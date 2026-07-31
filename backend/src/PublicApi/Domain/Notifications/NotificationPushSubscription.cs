using Modules.Shared.Domain.Common;

namespace PublicApi.Domain.Notifications;

/// <summary>
/// Represents a Web Push subscription for a user's browser.
/// Each row corresponds to one browser/device that has subscribed to push notifications.
/// </summary>
public sealed class NotificationPushSubscription : Entity
{
    /// <summary>The identifier of the user this push subscription belongs to.</summary>
    public Guid UserId { get; private set; }

    /// <summary>The push endpoint URL provided by the browser's push service.</summary>
    public string Endpoint { get; private set; } = null!;

    /// <summary>The P-256 DH public key used for payload encryption.</summary>
    public string P256dh { get; private set; } = null!;

    /// <summary>The authentication secret used to validate the push message.</summary>
    public string Auth { get; private set; } = null!;

    /// <summary>Optional User-Agent string to help identify the browser/device.</summary>
    public string? UserAgent { get; private set; }

    /// <summary>
    /// Creates a new push subscription record.
    /// </summary>
    /// <param name="userId">The identifier of the user this push subscription belongs to.</param>
    /// <param name="endpoint">The push endpoint URL from the browser.</param>
    /// <param name="p256dh">The P-256 DH public key from the browser subscription.</param>
    /// <param name="auth">The authentication secret from the browser subscription.</param>
    /// <param name="userAgent">Optional User-Agent of the subscribing browser.</param>
    public static NotificationPushSubscription Create(
        Guid userId,
        string endpoint,
        string p256dh,
        string auth,
        string? userAgent = null)
    {
        return new NotificationPushSubscription
        {
            UserId = userId,
            Endpoint = endpoint,
            P256dh = p256dh,
            Auth = auth,
            UserAgent = userAgent,
        };
    }

    /// <summary>
    /// Updates the encryption keys for an existing subscription.
    /// Called when the browser re-subscribes on the same endpoint.
    /// </summary>
    public void UpdateKeys(string p256dh, string auth)
    {
        P256dh = p256dh;
        Auth = auth;
    }
}
