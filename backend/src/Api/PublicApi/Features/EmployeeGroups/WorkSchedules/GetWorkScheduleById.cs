using Modules.Employees.Application.Abstractions;
using Modules.Employees.Domain.EmployeeGroups;
using Modules.Employees.Domain.EmployeeGroups.WorkSchedules;
using Modules.Shared.CQRS;
using Modules.Shared.Endpoints;
using Modules.Shared.Results;
using PublicApi.Features.EmployeeGroups;

namespace PublicApi.Features.EmployeeGroups.WorkSchedules;

public static class GetWorkScheduleById
{
    public sealed class Handler(IEmployeeGroupRepository repository)
        : IQueryHandler<GetWorkScheduleByIdQuery, WorkScheduleResponse>
    {
        public async Task<Result<WorkScheduleResponse>> Handle(
            GetWorkScheduleByIdQuery query,
            CancellationToken cancellationToken = default)
        {
            var group = await repository.GetByIdAsync(new EmployeeGroupId(query.EmployeeGroupId), cancellationToken);
            if (group is null)
            {
                return Result<WorkScheduleResponse>.Failure(EmployeeGroupErrors.NotFound);
            }

            var schedule = group.WorkSchedules.FirstOrDefault(ws => ws.Id == new WorkScheduleId(query.ScheduleId));
            if (schedule is null)
            {
                return Result<WorkScheduleResponse>.Failure(EmployeeGroupErrors.WorkScheduleNotFound);
            }

            var response = MapToResponse(schedule);
            return Result<WorkScheduleResponse>.Success(response);
        }

        private static WorkScheduleResponse MapToResponse(WorkSchedule ws)
        {
            return new WorkScheduleResponse(
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
                ws.CreatedOnUtc);
        }
    }

    public sealed class Endpoint : IEndpoint
    {
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            app.MapGet("employee-groups/{groupId:guid}/work-schedules/{scheduleId:guid}", async (
                Guid groupId,
                Guid scheduleId,
                IQueryHandler<GetWorkScheduleByIdQuery, WorkScheduleResponse> handler,
                CancellationToken ct) =>
            {
                var result = await handler.Handle(new GetWorkScheduleByIdQuery(groupId, scheduleId), ct);
                return result.IsSuccess ? Results.Ok(result.Value) : result.Problem();
            })
            .RequireAuthorization()
            .WithTags("EmployeeGroups")
            .WithSummary("Get work schedule by ID")
            .WithDescription("Retrieves a specific work schedule for an employee group.")
            .Produces<WorkScheduleResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .WithName("GetWorkScheduleById");
        }
    }
}