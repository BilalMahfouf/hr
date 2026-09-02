using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Modules.Employees.Application.Abstractions;
using Modules.Employees.Domain.EmployeeGroups;
using Modules.Shared.CQRS;
using Modules.Shared.Endpoints;
using Modules.Shared.Results;

namespace Modules.Employees.Application.EmployeeGroups.Rotations;

public static class GetAllRotations
{
    public sealed class Handler(IEmployeeDbContext dbContext)
        : IQueryHandler<GetAllRotationsQuery, IReadOnlyList<RotationEntryResponse>>
    {
        public async Task<Result<IReadOnlyList<RotationEntryResponse>>> Handle(
            GetAllRotationsQuery query,
            CancellationToken cancellationToken = default)
        {
            var group = await dbContext.EmployeeGroups
                .Include(g => g.RotationEntries)
                    .ThenInclude(re => re.WorkSchedule)
                .FirstOrDefaultAsync(g => g.Id == new EmployeeGroupId(query.EmployeeGroupId), cancellationToken);
            if (group is null)
            {
                return Result<IReadOnlyList<RotationEntryResponse>>.Failure(EmployeeGroupErrors.NotFound);
            }

            var response = group.RotationEntries
                .OrderBy(re => re.Position)
                .Select(EmployeeGroupMapper.ToResponse)
                .ToList();

            return Result<IReadOnlyList<RotationEntryResponse>>.Success(response);
        }
    }

    public sealed class Endpoint : IEndpoint
    {
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            app.MapGet("employee-groups/{groupId:guid}/rotations", async (
                Guid groupId,
                IQueryHandler<GetAllRotationsQuery, IReadOnlyList<RotationEntryResponse>> handler,
                CancellationToken ct) =>
            {
                var result = await handler.Handle(new GetAllRotationsQuery(groupId), ct);
                return result.IsSuccess ? Results.Ok(result.Value) : result.Problem();
            })
            .RequireAuthorization()
            .WithTags("EmployeeGroups")
            .WithSummary("Get all rotations for employee group")
            .WithDescription("Retrieves all rotation entries for an employee group, ordered by position.")
            .Produces<IReadOnlyList<RotationEntryResponse>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .WithName("GetAllRotations");
        }
    }
}