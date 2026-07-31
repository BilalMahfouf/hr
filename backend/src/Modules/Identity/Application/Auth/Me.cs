using Modules.Identity.Application.Users;
using Modules.Shared.Abstracions;
using Modules.Shared.CQRS;
using Modules.Shared.Endpoints;
using Modules.Shared.Results;

namespace Modules.Identity.Application.Auth;

public static class Me
{
    public sealed class Endpoint : IEndpoint
    {
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            app.MapGet("/auth/me", async (
                ICurrentUser currentUser,
                IQueryHandler<GetUserById.GetUserByIdQuery, Modules.Identity.Application.Users.Shared.Response> handler,
                CancellationToken ct = default) =>
            {
                var userId = currentUser.UserId!.Value;
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
