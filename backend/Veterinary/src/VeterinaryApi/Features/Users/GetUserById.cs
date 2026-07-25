using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using VeterinaryApi.Common.Abstracions;
using VeterinaryApi.Common.CQRS;
using VeterinaryApi.Common.Endpoints;
using VeterinaryApi.Common.Results;
using VeterinaryApi.Domain.Users;
using static VeterinaryApi.Features.Users.Shared;

namespace VeterinaryApi.Features.Users;

/// <summary>
/// Vertical slice for retrieving a single user's public profile by their unique identifier.
/// </summary>
public static class GetUserById
{
    /// <summary>
    /// Query carrying the target user's identifier.
    /// Implements <see cref="IQuery{TResponse}"/> where the response is <see cref="Response"/>.
    /// </summary>
    /// <param name="UserId">The unique identifier of the user to retrieve.</param>
    public record GetUserByIdQuery(Guid UserId) : IQuery<Response>;

    /// <summary>
    /// Handles the <see cref="GetUserByIdQuery"/> with a projection query (no entity tracking).
    /// </summary>
    public class GetUserByIdQueryHandler
        : IQueryHandler<GetUserByIdQuery, Response>
    {
        private readonly IApplicationDbContext _db;

        /// <summary>Initializes the handler with the application database context.</summary>
        public GetUserByIdQueryHandler(IApplicationDbContext db)
        {
            _db = db;
        }

        /// <summary>
        /// Projects the user entity to a <see cref="Response"/> DTO.
        /// Uses <c>AsNoTracking</c> for read performance.
        /// </summary>
        /// <param name="query">The query with the target user ID.</param>
        /// <param name="cancellationToken">Token for cooperative cancellation.</param>
        /// <returns>A successful result with the user DTO, or <c>UserErrors.UserNotFound</c> if not found.</returns>
        public async Task<Result<Response>> Handle(
            GetUserByIdQuery query,
            CancellationToken cancellationToken = default)
        {
            var subscriptionStatus = await _db.Subscriptions.AsNoTracking()
                 .Where(e => e.DoctorId == query.UserId)
                 .OrderByDescending(e => e.CreatedOnUtc)
                 .Select(e => e.Status.ToString())
                 .FirstOrDefaultAsync(cancellationToken);
            var isSubscriptionExist = subscriptionStatus is not null;
            var response = await _db.Users
                .Where(u => u.Id == query.UserId)
                .Select(u => new Response(
                    u.Id,
                    u.UserName,
                    u.FullName,
                    u.Email,
                    u.Role.ToString(),
                    u.IsActive,
                    subscriptionStatus,
                    isSubscriptionExist,
                    u.CreatedOnUtc
                    ))
                .AsNoTracking().FirstOrDefaultAsync(cancellationToken);
            if (response is null)
            {
                return Result<Response>.Failure(
                    UserErrors.UserNotFound(query.UserId));
            }
            return Result<Response>.Success(response);
        }
    }

    /// <summary>
    /// Carter endpoint that maps <c>GET /users/{userId}</c>.
    /// Requires authorization via <c>[Authorize]</c> attribute.
    /// Returns <c>200 OK</c> with the user DTO, or Problem Details if not found.
    /// </summary>
    public class Endpoint : IEndpoint
    {
        /// <summary>Registers the get-user-by-id route.</summary>
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            app.MapGet("/users/{userId:guid}",
               [Authorize] async (Guid userId,
                IQueryHandler<GetUserByIdQuery, Response> handler,
                CancellationToken cancellationToken) =>
            {
                var query = new GetUserByIdQuery(userId);
                var result = await handler.Handle(
                        query, cancellationToken);
                return result.IsSuccess
                    ? Results.Ok(result.Value)
                    : result.Problem();
            })
            .WithTags($"{nameof(User)}s")
            .WithSummary("Get user by ID")
            .WithDescription("Retrieves user information by their unique identifier. Returns user ID, email, and full name.");
        }
    }
}
