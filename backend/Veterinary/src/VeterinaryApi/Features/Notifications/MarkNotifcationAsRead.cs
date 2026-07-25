using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using VeterinaryApi.Common.Abstracions;
using VeterinaryApi.Common.CQRS;
using VeterinaryApi.Common.Endpoints;
using VeterinaryApi.Common.Results;
using VeterinaryApi.Domain.Notifications;
using VeterinaryApi.Infrastructure.Persistence;

namespace VeterinaryApi.Features.Notifications;

/// <summary>
/// Vertical slice for marking a single notification as read by its identifier.
/// Note: the filename <c>MarkNotifcationAsRead.cs</c> contains a typo (‘Notifcation’ → ‘Notification’).
/// </summary>
public static class MarkNotifcationAsRead
{
    /// <summary>Command carrying the target notification identifier.</summary>
    /// <param name="NotificationId">The unique identifier of the notification to mark as read.</param>
    public sealed record Command(Guid NotificationId) : ICommand;

    /// <summary>Handles the <see cref="Command"/> by loading the notification and marking it as read.</summary>
    public sealed class MarkNotificationAsReadCommandHandler : ICommandHandler<Command>
    {
        private readonly IApplicationDbContext _db;
        private readonly ICurrentTenant _currentTenant;

        /// <summary>Initializes the handler with database and tenant context services.</summary>
        public MarkNotificationAsReadCommandHandler(
            IApplicationDbContext db,
            ICurrentTenant currentTenant)
        {
            _db = db;
            _currentTenant = currentTenant;
        }

        /// <summary>Loads the notification, calls <c>Notification.MarkAsRead()</c>, and persists.</summary>
        /// <returns>A successful result, or <c>NotificationErrors.NotFound</c>.</returns>
        public async Task<Result> Handle(
            Command command,
            CancellationToken cancellationToken = default)
        {
            var notification = await _db.Notifications
                .ForTenant(_currentTenant.UserId!.Value)
                .FirstOrDefaultAsync(
                e => e.Id == command.NotificationId
                , cancellationToken);
            if (notification is null)
            {
                return Result.Failure(NotificationErrors.NotFound);
            }
            notification.MarkAsRead();
            await _db.SaveChangesAsync(cancellationToken);
            return Result.Success;
        }
    }
    /// <summary>Carter endpoint that maps <c>PATCH /notifications/{notificationId}/mark-as-read</c>. Requires authorization.</summary>
    public sealed class Endpoint : IEndpoint
    {
        /// <summary>Registers the mark-single-notification-as-read route.</summary>
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            app.MapPatch("/notifications/{notificationId:guid}/mark-as-read", [Authorize] async (
                 Guid notificationId,
                 ICommandHandler<Command> handler,
                 CancellationToken ct) =>
            {
                var command = new Command(notificationId);
                var result = await handler.Handle(command, ct);
                return result.IsSuccess ? Results.NoContent() : result.Problem();
            })
            .WithTags($"{nameof(Notification)}s")
            .WithSummary("Mark notification as read")
            .WithDescription("Marks a specific notification as read by its unique identifier. The notification must belong to the current authenticated user.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status401Unauthorized);
        }
    }
}
