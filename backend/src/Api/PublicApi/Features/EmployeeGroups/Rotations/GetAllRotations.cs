using Modules.Employees.Application.Abstractions;
using Modules.Employees.Domain.EmployeeGroups;
using Modules.Employees.Domain.EmployeeGroups.Rotation;
using Modules.Shared.CQRS;
using Modules.Shared.Endpoints;
using Modules.Shared.Results;
using PublicApi.Features.EmployeeGroups;

namespace PublicApi.Features.EmployeeGroups.Rotations;

public static class GetAllRotations
{
    public sealed class Handler(IEmployeeGroupRepository repository)
        : IQueryHandler<GetAllRotationsQuery, IReadOnlyList<RotationEntryResponse>>
    {
        public async Task<Result<IReadOnlyList<RotationEntryResponse>>> Handle(
            GetAllRotationsQuery query,
            CancellationToken cancellationToken = default)
        {
            var group = await repository.GetByIdAsync(new EmployeeGroupId(query.EmployeeGroupId), cancellationToken);
            if (group is null)
            {
                return Result<IReadOnlyList<RotationEntryResponse>>.Failure(EmployeeGroupErrors.NotFound);
            }

            var rotations = await repository.GetRotationEntriesByGroupIdAsync(new EmployeeGroupId(query.EmployeeGroupId), cancellationToken);

            var response = rotations.Select(re => new RotationEntryResponse(
                re.Id.Value,
                re.EmployeeGroupId.Value,
                re.Position,
                re.WorkScheduleId?.Value,
                re.Status.ToString())).ToList();

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