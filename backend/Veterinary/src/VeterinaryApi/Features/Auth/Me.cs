using VeterinaryApi.Common.Abstracions;
using VeterinaryApi.Common.CQRS;
using VeterinaryApi.Common.Endpoints;
using VeterinaryApi.Common.Results;
using VeterinaryApi.Features.Users;

namespace VeterinaryApi.Features.Auth;

/// <summary>
/// Vertical slice for the "current user" endpoint.
/// Delegates to <see cref="GetUserById.GetUserByIdQuery"/> using the authenticated user's tenant ID.
/// </summary>
public static class Me
{
    /// <summary>
    /// Endpoint-only slice — no command/query class defined here.
    /// Maps <c>GET /auth/me</c> to <see cref="GetUserById"/> with the current user's ID.
    /// </summary>
    public sealed class Endpoint : IEndpoint
    {
        /// <summary>Registers the /auth/me route with authorization requirement.</summary>
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            app.MapGet("/auth/me", async (
                ICurrentTenant currentTenant,
                IQueryHandler<GetUserById.GetUserByIdQuery, Shared.Response> handler,
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
