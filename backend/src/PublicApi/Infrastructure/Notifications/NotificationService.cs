using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Modules.Shared.Abstracions;
using PublicApi.Common.Abstracions;
using PublicApi.Domain.Notifications;
using PublicApi.Infrastructure.Persistence;
using WebPush;


namespace PublicApi.Infrastructure.Notifications;

/// <summary>
/// Concrete implementation of <see cref="INotificatioService"/> that delivers real-time
/// notifications via SignalR <b>and</b> Web Push (VAPID).
/// </summary>
/// <remarks>
/// SignalR is sent first (in-app, instant). Web Push follows for background/offline delivery.
/// Web Push is silently skipped when VAPID configuration is absent (dev environments without push).
/// Stale subscriptions (HTTP 410 Gone / 404) are automatically removed after a failed send.
/// </remarks>
public class NotificationService : INotificatioService
{
    private readonly IHubContext<NotificationHub> _hubContext;
    private readonly ICurrentTenant _currentUser;
    private readonly IApplicationDbContext _db;
    private readonly ILogger<NotificationService> _logger;

    /// <summary>
    /// Initializes the service with required infrastructure dependencies.
    /// </summary>
    /// <param name="hubContext">The SignalR hub context for real-time in-app delivery.</param>
    /// <param name="currentUser">The ambient current-user context.</param>
    /// <param name="db">The application database context for loading push subscriptions.</param>
    /// <param name="logger">Logger for diagnosing push delivery issues.</param>
    public NotificationService(
        IHubContext<NotificationHub> hubContext,
        ICurrentTenant currentUser,
        IApplicationDbContext db,
        ILogger<NotificationService> logger)
    {
        _hubContext = hubContext;
        _currentUser = currentUser;
        _db = db;
        _logger = logger;
    }

    /// <summary>
    /// Sends the notification via both SignalR (real-time, in-app) and Web Push (background/desktop).
    /// </summary>
    /// <param name="notification">The notification payload.</param>
    /// <param name="UserId">The target user's unique identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task SendNotificationAsync(
        NotificationResponse notification,
        Guid UserId,
        CancellationToken cancellationToken = default)
    {
        // ── 1. SignalR — existing real-time delivery ─────────────────────────
        await _hubContext.Clients
            .User(UserId.ToString())
            .SendAsync("ReceiveNotification", new { notification }, cancellationToken);

        // ── 2. Web Push — desktop/background delivery ────────────────────────
        await SendWebPushAsync(notification, UserId, cancellationToken);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Private helpers
    // ─────────────────────────────────────────────────────────────────────────

    private async Task SendWebPushAsync(
        NotificationResponse notification,
        Guid userId,
        CancellationToken cancellationToken)
    {
        var vapidPublicKey = Environment.GetEnvironmentVariable("WEBPUSH_VAPID_PUBLIC_KEY");
        var vapidPrivateKey = Environment.GetEnvironmentVariable("WEBPUSH_VAPID_PRIVATE_KEY");
        var vapidSubject = Environment.GetEnvironmentVariable("WEBPUSH_SUBJECT");

        // Skip silently when VAPID is not configured (local dev without push).
        if (string.IsNullOrWhiteSpace(vapidPublicKey)
            || string.IsNullOrWhiteSpace(vapidPrivateKey)
            || string.IsNullOrWhiteSpace(vapidSubject))
        {
            _logger.LogWarning("Web Push skipped for user {UserId}: VAPID keys not configured.", userId);
            return;
        }

        var subscriptions = await _db.NotificationPushSubscriptions
            .ForTenant(userId)
            .ToListAsync(cancellationToken);

        if (subscriptions.Count == 0)
        {
            _logger.LogDebug("Web Push skipped for user {UserId}: no registered browser subscriptions.", userId);
            return;
        }

        _logger.LogDebug("Sending Web Push to {Count} subscription(s) for user {UserId}.", subscriptions.Count, userId);

        var vapidDetails = new VapidDetails(vapidSubject, vapidPublicKey, vapidPrivateKey);
        var client = new WebPushClient();

        // Minimal payload — title and body are included because VAPID encrypts end-to-end.
        // Sensitive clinical data (full visit records, prescriptions, etc.) is NOT sent here;
        // the SPA fetches full details when the user opens the app after clicking the notification.
        var payload = JsonSerializer.Serialize(new
        {
            notificationId = notification.Id,
            title = notification.Title,
            body = notification.Body,
            createdOnUtc = notification.CreatedOnUtc,
            url = "/notifications",
        });

        var stale = new List<NotificationPushSubscription>();

        foreach (var sub in subscriptions)
        {
            try
            {
                var pushSubscription = new PushSubscription(sub.Endpoint, sub.P256dh, sub.Auth);
                await client.SendNotificationAsync(pushSubscription, payload, vapidDetails);
            }
            catch (WebPushException ex)
                when (ex.StatusCode == HttpStatusCode.Gone
                   || ex.StatusCode == HttpStatusCode.NotFound)
            {
                // Push service says the subscription is no longer valid — queue for cleanup.
                _logger.LogInformation("Removing stale push subscription {Endpoint} for user {UserId} (HTTP {Status}).",
                    sub.Endpoint, userId, ex.StatusCode);
                stale.Add(sub);
            }
            catch (Exception ex)
            {
                // Network errors, rate limits, etc. — keep subscription and retry next time.
                _logger.LogWarning(ex, "Web Push failed for subscription {Endpoint} (user {UserId}).",
                    sub.Endpoint, userId);
            }
        }

        if (stale.Count > 0)
        {
            _db.NotificationPushSubscriptions.RemoveRange(stale);
            await _db.SaveChangesAsync(cancellationToken);
        }
    }
}
