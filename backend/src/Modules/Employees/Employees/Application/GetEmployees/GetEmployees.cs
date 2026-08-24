using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Modules.Shared.CQRS;
using Modules.Shared.Endpoints;
using Modules.Shared.Paginations.OffSet;
using Modules.Shared.Results;

namespace Modules.Employees.Application.GetEmployees;

public static class GetEmployees
{
    public sealed record Query(
        int Page,
        int PageSize,
        string? Search,
        string? SortColumn,
        string? SortOrder)
        : IQuery<OffSetPagedList<Response>>;

    public sealed record Response(
        string Matricule,
        string? Bdg,
        string FirstName,
        string LastName,
        string? Group,
        string? Department,
        string? Phone);

    public sealed class Endpoint : IEndpoint
    {
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            app.MapGet("employees", async (
                [FromQuery] int? page,
                [FromQuery] int? pageSize,
                [FromQuery] string? search,
                [FromQuery] string? sortColumn,
                [FromQuery] string? sortOrder,
                IQueryHandler<Query, OffSetPagedList<Response>> handler,
                CancellationToken cancellationToken) =>
            {
                var query = new Query(
                    page ?? 1,
                    pageSize ?? 10,
                    search,
                    sortColumn,
                    sortOrder);

                var result = await handler.Handle(query, cancellationToken);
                return result.IsSuccess ? Results.Ok(result.Value)
                     : result.Problem();
            })
            .RequireAuthorization()
            .WithTags("Employees")
            .WithSummary("Get all employees with pagination, search and sorting");
        }
    }
}
