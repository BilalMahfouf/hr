using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VeterinaryApi.Common.Abstracions;
using VeterinaryApi.Common.CQRS;
using VeterinaryApi.Common.Endpoints;
using VeterinaryApi.Common.Paginations.Cursor;
using VeterinaryApi.Common.Paginations.OffSet;
using VeterinaryApi.Common.Results;
using VeterinaryApi.Domain.Notifications;
using VeterinaryApi.Infrastructure.Persistence;

namespace VeterinaryApi.Features.Notifications;

/// <summary>
/// Vertical slice for retrieving a tenant-scoped list of notifications with cursor-based pagination.
/// Supports filtering by read/unread status and bidirectional navigation (next/prev).
/// </summary>
public static class GetAllNotifications
{
    /// <summary>Read-model DTO for a single notification row.</summary>
    public sealed record Response(
        Guid Id,
        string Title,
        string Body,
        bool IsRead,
        DateTime CreatedOnUtc);

    /// <summary>Filter type for the notifications query.</summary>
    public enum Type
    {
        /// <summary>Return all notifications regardless of read status.</summary>
        All = 1,
        /// <summary>Return only unread notifications.</summary>
        NotReaded = 2,
    }

    /// <summary>Query carrying cursor pagination parameters and an optional read/unread filter.</summary>
    /// <param name="cursorRequest">Cursor pagination parameters (size, cursor token, direction).</param>
    /// <param name="type">Optional filter: <see cref="Type.All"/> or <see cref="Type.NotReaded"/>.</param>
    public sealed record Query(
        CursorRequest<Response> cursorRequest,
        Type? type
        ) : IQuery<CursorPagedList<Response>>;

    /// <summary>
    /// Handles the <see cref="Query"/> with cursor-based pagination on notifications,
    /// using <c>CreatedOnUtc</c> + <c>Id</c> as a stable composite cursor.
    /// </summary>
    public sealed class GetAllNotificationQueryHandler
        : IQueryHandler<Query, CursorPagedList<Response>>
    {
        private readonly IApplicationDbContext _db;
        private readonly ICurrentTenant _currentTenant;

        /// <summary>Initializes the handler with database and tenant context services.</summary>
        public GetAllNotificationQueryHandler(
            IApplicationDbContext db,
            ICurrentTenant currentTenant)
        {
            _db = db;
            _currentTenant = currentTenant;
        }

        /// <summary>
        /// Applies cursor filter, orders by <c>CreatedOnUtc DESC</c> (or <c>ASC</c> for prev),
        /// fetches <c>pageSize + 1</c> to detect page boundaries, and builds cursor tokens.
        /// </summary>
        /// <returns>A successful cursor-paginated result, or <c>NotificationErrors.NotFound</c>.</returns>
        public async Task<Result<CursorPagedList<Response>>> Handle(
            Query query,
            CancellationToken cancellationToken = default)
        {
            var cursorData = CursorHelper.Decode(query.cursorRequest.Cursor);

            var isAll = query.type switch
            {
                Type.All => true,
                Type.NotReaded => false,
                _ => false
            };


            var baseQuery = _db.Notifications
                .ForTenant(_currentTenant.UserId!.Value)
                .Where(e => e.IsRead == isAll);

            // Step 3: Apply cursor filter
            // For "next" (older items): CreatedOnUtc < cursor OR (same time AND Id > cursor)
            // We use Id > cursor for tie-breaking (arbitrary but consistent)
            if (cursorData is not null)
            {

                if (query.cursorRequest.Direction == CursorDirection.Next)
                {
                    // Get items OLDER than cursor (going forward in desc list)
                    baseQuery = baseQuery.Where(e =>
                        e.CreatedOnUtc < cursorData.CreatedOnUtc ||
                        (e.CreatedOnUtc == cursorData.CreatedOnUtc
                        && e.Id.CompareTo(cursorData.Id) > 0));
                }
                else // "prev"
                {
                    // Get items NEWER than cursor (going backward)
                    baseQuery = baseQuery.Where(e =>
                        e.CreatedOnUtc > cursorData.CreatedOnUtc ||
                        (e.CreatedOnUtc == cursorData.CreatedOnUtc
                        && e.Id.CompareTo(cursorData.Id) < 0));
                }
            }

            // Step 4: Order and fetch one extra item to detect hasNextPage
            IQueryable<Notification> orderedQuery;
            if (query.cursorRequest.Direction == CursorDirection.Next)
            {
                orderedQuery = baseQuery
                    .OrderByDescending(e => e.CreatedOnUtc)
                    .ThenBy(e => e.Id);
            }
            else
            {
                // For "prev", reverse order then flip results
                orderedQuery = baseQuery
                    .OrderBy(e => e.CreatedOnUtc)
                    .ThenByDescending(e => e.Id);
            }

            // Fetch pageSize + 1 to check if there are more items
            var notifications = await orderedQuery
                .Take(query.cursorRequest.PageSize + 1)
                .Select(e => new Response(e.Id, e.Title, e.Body, e.IsRead, e.CreatedOnUtc))
                .ToListAsync(cancellationToken);

            if (notifications.Count <= 0)
            {
                return Result<CursorPagedList<Response>>
                    .Failure(NotificationErrors.NotFound);
            }

            bool hasMore = notifications.Count > query.cursorRequest.PageSize;

            if (hasMore)
                notifications = notifications
                    .Take(query.cursorRequest.PageSize).ToList();

            // For "prev" direction, reverse to maintain newest-first order
            if (query.cursorRequest.Direction == CursorDirection.Prev)
                notifications.Reverse();

            // Step 6: Generate cursors
            string? nextCursor = null;
            string? previousCursor = null;

            if (notifications.Any())
            {
                var firstItem = notifications.First();
                var lastItem = notifications.Last();

                // NextCursor points to last item (to get older items)
                if (hasMore || query.cursorRequest.Direction == CursorDirection.Prev)
                    nextCursor = CursorHelper.Encode(lastItem.CreatedOnUtc, lastItem.Id);

                // PreviousCursor points to first item (to get newer items)
                // Only if we're not at the very beginning
                if (cursorData is not null)
                    previousCursor = CursorHelper.Encode(firstItem.CreatedOnUtc, firstItem.Id);
            }

            bool hasNextPage = query.cursorRequest.Direction
                == CursorDirection.Next ?
                hasMore : cursorData != null;
            bool hasPreviousPage = query.cursorRequest.Direction == CursorDirection.Prev ?
                hasMore : cursorData != null;

            var pageSize = notifications.Count < query.cursorRequest.PageSize
                ? notifications.Count : query.cursorRequest.PageSize;

            // Step 7: Build response
            var response = CursorPagedList<Response>.Create(
                notifications,
                pageSize,
                hasNextPage,
                hasPreviousPage,
                nextCursor,
                previousCursor);

            return Result<CursorPagedList<Response>>.Success(response);
        }
    }

    /// <summary>Carter endpoint that maps <c>GET /notifications</c> with cursor pagination query parameters. Requires authorization.</summary>
    public sealed class Endpoint : IEndpoint
    {
        /// <summary>Registers the get-all-notifications route.</summary>
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            app.MapGet("/notifications", [Authorize] async (
                [FromQuery] int? pageSize,
                [FromQuery] string? cursor,
                [FromQuery] string? direction,
                [FromQuery] Type? type,
                IQueryHandler<Query, CursorPagedList<Response>> handler,
                CancellationToken ct = default) =>
            {
                var query = new Query(
                    CursorRequest<Response>.Create(pageSize, cursor, direction),
                    type);
                var result = await handler.Handle(query, ct);
                return result.IsSuccess
                    ? Results.Ok(result.Value)
                    : result.Problem();
            })
            .WithTags($"{nameof(Notification)}s")
            .WithSummary("Get all notifications (cursor pagination)")
            .WithDescription("Retrieves notifications using cursor-based pagination. Pass 'cursor' from previous response to load more. Use 'type' to filter read/unread.")
            .Produces<CursorPagedList<Response>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound);
        }

    }
}
