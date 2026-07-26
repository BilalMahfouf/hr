using Identity.Abstracions;
using Identity.Domain.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using VeterinaryApi.Common.CQRS;
using VeterinaryApi.Common.Endpoints;
using VeterinaryApi.Common.Results;
using static Identity.Application.Users.Shared;

namespace Identity.Application.Users;

public static class GetUserById
{
    public record GetUserByIdQuery(Guid UserId) : IQuery<Response>;

    public class GetUserByIdQueryHandler
        : IQueryHandler<GetUserByIdQuery, Response>
    {
        private readonly IIdentityApplicationDbContext _db;
        private readonly IUserSubscriptionStatusQuery _subscriptionQuery;

        public GetUserByIdQueryHandler(
            IIdentityApplicationDbContext db,
            IUserSubscriptionStatusQuery subscriptionQuery)
        {
            _db = db;
            _subscriptionQuery = subscriptionQuery;
        }

        public async Task<Result<Response>> Handle(
            GetUserByIdQuery query,
            CancellationToken cancellationToken = default)
        {
            var (subscriptionStatus, isSubscriptionExist) = await _subscriptionQuery
                .GetSubscriptionStatusAsync(query.UserId);

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

    public class Endpoint : IEndpoint
    {
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
