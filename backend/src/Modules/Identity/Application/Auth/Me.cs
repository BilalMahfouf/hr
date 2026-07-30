using Identity.Application.Users;
using Shared.Abstracions;
using Shared.CQRS;
using Shared.Endpoints;
using Shared.Results;

namespace Identity.Application.Auth;

public static class Me
{
    public sealed class Endpoint : IEndpoint
    {
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            app.MapGet("/auth/me", async (
                ICurrentTenant currentTenant,
                IQueryHandler<GetUserById.GetUserByIdQuery, Identity.Application.Users.Shared.Response> handler,
                CancellationToken ct = default) =>
            {
                var userId = currentTenant.UserId!.Value;
                var query = new GetUserById.GetUserByIdQuery(userId);
                var result = await handler.Handle(query, ct);
                return result.IsSuccess ? Results.Ok(result.Value)
                 : result.Problem();

            }).RequireAuthorization()
            .WithTags("Auth")
            .WithSummary("Get current user info")
            .WithDescription("Retrieves information about the currently authenticated user, including their unique identifier.");
        }
    }
}
