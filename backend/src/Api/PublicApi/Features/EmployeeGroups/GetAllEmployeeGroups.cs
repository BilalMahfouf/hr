using Modules.Employees.Application.Abstractions;
using Modules.Employees.Domain.EmployeeGroups;
using Modules.Shared.CQRS;
using Modules.Shared.Endpoints;
using Modules.Shared.Results;
using PublicApi.Features.EmployeeGroups;

namespace PublicApi.Features.EmployeeGroups;

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

            var response = groups.Select(MapToResponse).ToList();
            return Result<IReadOnlyList<EmployeeGroupResponse>>.Success(response);
        }

        private static EmployeeGroupResponse MapToResponse(EmployeeGroup group)
        {
            var workSchedules = group.WorkSchedules.Select(ws => new WorkScheduleResponse(
                ws.Id.Value,
                ws.EmployeeGroupId.Value,
                ws.ShiftStartTime,
                ws.ShiftEndTime,
                ws.BreakStartTime,
                ws.BreakEndTime,
                ws.EndDayOffset,
                ws.AllowedCheckInLatenessMinutes,
                ws.AllowedCheckOutEarlinessMinutes,
                ws.IsActive,
                ws.CreatedOnUtc)).ToList();

            var rotationEntries = group.RotationEntries.Select(re => new RotationEntryResponse(
                re.Id.Value,
                re.EmployeeGroupId.Value,
                re.Position,
                re.WorkScheduleId?.Value,
                re.Status.ToString())).ToList();

            return new EmployeeGroupResponse(
                group.Id.Value,
                group.Name,
                group.IsSecurity,
                group.Description,
                group.RotationStartDate,
                group.NumberOfRotations,
                workSchedules,
                rotationEntries,
                group.CreatedOnUtc);
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