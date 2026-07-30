namespace PublicApi.Domain.Notifications;

/// <summary>
/// Service contract for delivering real-time notifications to connected clients (e.g., via SignalR).
/// Note: interface name contains a typo (‘Notificatio’ → ‘Notification’).
/// </summary>
public interface INotificatioService
{
    /// <summary>Sends a notification to the specified user asynchronously.</summary>
    /// <param name="notification">The notification payload to deliver.</param>
    /// <param name="UserId">The unique identifier of the target user.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    Task SendNotificationAsync(
        NotificationResponse notification,
        Guid UserId,
        CancellationToken cancellationToken = default);
}

/// <summary>DTO payload sent over the real-time notification channel.</summary>
public sealed record NotificationResponse(
    Guid Id,
    string Title,
    string Body,
    bool IsRead,
    DateTime CreatedOnUtc);
