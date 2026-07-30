using Shared.Domain.Common;

namespace VeterinaryApi.Domain.Notifications;

/// <summary>
/// Aggregate root representing a user notification message in the veterinary system.
/// Notifications are delivered via SignalR and stored in the database.
/// </summary>
public sealed class Notification : Entity
{
    /// <summary>Short heading text for the notification.</summary>
    public string Title { get; private set; } = null!;

    /// <summary>Full notification message body.</summary>
    public string Body { get; private set; } = null!;

    /// <summary>Indicates whether the notification has been read by the recipient.</summary>
    public bool IsRead { get; private set; }

    /// <summary>Factory method that creates a new unread notification.</summary>
    /// <param name="title">The notification title.</param>
    /// <param name="body">The notification body text.</param>
    /// <returns>A new <see cref="Notification"/> with <see cref="IsRead"/> set to <c>false</c>.</returns>
    public static Notification Create(string title, string body)
    {
        var notficaiton = new Notification();
        notficaiton.Title = title;
        notficaiton.Body = body;
        notficaiton.IsRead = false;
        return notficaiton;
    }

    /// <summary>Marks this notification as read.</summary>
    public void MarkAsRead()
    {
        this.IsRead = true;
    }

}
