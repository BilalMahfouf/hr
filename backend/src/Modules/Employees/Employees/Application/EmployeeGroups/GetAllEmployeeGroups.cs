using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Modules.Employees.Application.Abstractions;
using Modules.Shared.CQRS;
using Modules.Shared.Endpoints;
using Modules.Shared.Results;

namespace Modules.Employees.Application.EmployeeGroups;

public static class GetAllEmployeeGroups
{
    public sealed class Handler(IEmployeeGroupRepository repository)
        : IQueryHandler<GetAllEmployeeGroupsQuery, IReadOnlyList<EmployeeGroupResponse>>
    {
        public async Task<Result<IReadOnlyList<EmployeeGroupResponse>>> Handle(
            GetAllEmployeeGroupsQuery query,
            CancellationToken cancellationToken = default)
        {
            var groups = await repository.GetAllAsync(cancellationToken);

            var response = groups.Select(EmployeeGroupMapper.ToResponse).ToList();
            return Result<IReadOnlyList<EmployeeGroupResponse>>.Success(response);
        }
    }

    public sealed class Endpoint : IEndpoint
    {
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            app.MapGet("employee-groups", async (
                IQueryHandler<GetAllEmployeeGroupsQuery, IReadOnlyList<EmployeeGroupResponse>> handler,
                CancellationToken ct) =>
            {
                var result = await handler.Handle(new GetAllEmployeeGroupsQuery(), ct);
                return result.IsSuccess ? Results.Ok(result.Value) : result.Problem();
            })
            .RequireAuthorization()
            .WithTags("EmployeeGroups")
            .WithSummary("Get all employee groups")
            .WithDescription("Retrieves all employee groups with their work schedules and rotation entries.")
            .Produces<IReadOnlyList<EmployeeGroupResponse>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .WithName("GetAllEmployeeGroups");
        }
    }
}