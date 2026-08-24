using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Modules.Shared.CQRS;
using Modules.Shared.Endpoints;
using Modules.Shared.Results;

namespace Modules.Employees.Application.GetEmployeeById;

public static class GetEmployeeById
{
    public sealed record Query(string Id) : IQuery<Response>;

    public sealed record Response(
        string Matricule,
        string? Bdg,
        string FirstName,
        string LastName,
        DateTime? BirthDate,
        string? BirthPlace,
        string? Phone,
        string? Sex,
        string? Address,
        string? Nationality,
        string? Group,
        string? Department,
        string? CodeNiv,
        string? Spec,
        string? PhotoBase64);

    public sealed class Endpoint : IEndpoint
    {
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            app.MapGet("employees/{id}", async (
                string id,
                IQueryHandler<Query, Response> handler,
                CancellationToken cancellationToken) =>
            {
                var result = await handler.Handle(new Query(id), cancellationToken);
                return result.IsSuccess ? Results.Ok(result.Value)
                     : result.Problem();
            })
            .RequireAuthorization()
            .WithTags("Employees")
            .WithSummary("Get employee details by id")
            .WithDescription("Retrieves employee details from the legacy SYSGRH database by matricule.");
        }
    }
}
