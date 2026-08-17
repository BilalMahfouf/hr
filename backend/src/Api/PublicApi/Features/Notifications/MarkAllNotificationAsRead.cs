using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Modules.Shared.Abstracions;
using PublicApi.Common.Abstracions;
using Modules.Shared.CQRS;
using Modules.Shared.Endpoints;
using Modules.Shared.Results;
using PublicApi.Domain.Notifications;
using PublicApi.Infrastructure.Persistence;

namespace PublicApi.Features.Notifications;

/// <summary>
/// Vertical slice for bulk-marking all unread notifications as read for the current user.
/// </summary>
public static class MarkAllNotificationAsRead
{
    /// <summary>Command with no input — targets all unread notifications of the authenticated user.</summary>
    public sealed record Command() : ICommand;

    /// <summary>Handles the <see cref="Command"/> by iterating and marking all unread notifications.</summary>
    public sealed class MarkAllNotificationAsReadCommandHandler : ICommandHandler<Command>
    {
        private readonly IApplicationDbContext _db;
        private readonly ICurrentUser _currentUser;

        /// <summary>Initializes the handler with database and current-user context services.</summary>
        public MarkAllNotificationAsReadCommandHandler(
            IApplicationDbContext db,
            ICurrentUser currentUser)
        {
            _db = db;
            _currentUser = currentUser;
        }

        /// <summary>
        /// Loads all unread notifications for the tenant, calls <c>Notification.MarkAsRead()</c> on each,
        /// and persists. Returns success even if there are no unread notifications.
        /// </summary>
        public async Task<Result> Handle(
            Command command,
            CancellationToken cancellationToken)
        {
            var notifications = await _db.Notifications
                .Where(e => e.UserId == _currentUser.UserId!.Value)
                .Where(n => n.IsRead == false)
                .ToListAsync(cancellationToken);
            if (notifications is null || !notifications.Any())
            {
                return Result.Success;
            }

            foreach (var notification in notifications)
            {
                notification.MarkAsRead();
            }
            await _db.SaveChangesAsync(cancellationToken);
            return Result.Success;
        }
    }
    /// <summary>Carter endpoint that maps <c>PATCH /notifications/mark-all-as-read</c>. Requires authorization.</summary>
    public sealed class Endpoint : IEndpoint
    {
        /// <summary>Registers the bulk mark-all-as-read route.</summary>
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            app.MapPatch("/notifications/mark-all-as-read", [Authorize] async (
                 ICommandHandler<Command> handler,
                 CancellationToken ct) =>
            {
                var command = new Command();
                var result = await handler.Handle(command, ct);
                return result.IsSuccess ? Results.NoContent() : result.Problem();
            })
            .WithTags($"{nameof(Notification)}s")
            .WithSummary("Mark all notifications as read")
            .WithDescription("Marks all unread notifications for the current authenticated user as read. Returns success even if no unread notifications exist.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status401Unauthorized);
        }
    }

}
