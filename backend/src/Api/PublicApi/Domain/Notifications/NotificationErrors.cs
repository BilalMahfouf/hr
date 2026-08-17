using Modules.Shared.Errors;

namespace PublicApi.Domain.Notifications;

/// <summary>Defines domain error codes and messages for notification-related operations.</summary>
public static class NotificationErrors
{
    /// <summary>Returned when no notifications are found for the current tenant, or the result set is empty.</summary>
    public static Error NotFound
        => Error.NotFound($"{nameof(Notification)}s.{nameof(NotFound)}",
            "Notifcations are not found or emtpy");
}
