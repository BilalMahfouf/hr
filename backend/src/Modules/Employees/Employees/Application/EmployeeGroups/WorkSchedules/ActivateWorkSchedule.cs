using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Modules.Employees.Application.Abstractions;
using Modules.Employees.Domain.EmployeeGroups;
using Modules.Employees.Domain.EmployeeGroups.WorkSchedules;
using Modules.Shared.CQRS;
using Modules.Shared.Endpoints;
using Modules.Shared.Results;

namespace Modules.Employees.Application.EmployeeGroups.WorkSchedules;

public static class ActivateWorkSchedule
{
    public sealed class Handler(IEmployeeDbContext dbContext)
        : ICommandHandler<ActivateWorkScheduleCommand, WorkScheduleResponse>
    {
        public async Task<Result<WorkScheduleResponse>> Handle(
            ActivateWorkScheduleCommand command,
            CancellationToken cancellationToken = default)
        {
            var group = await dbContext.EmployeeGroups
                .Include(g => g.WorkSchedules)
                .FirstOrDefaultAsync(g => g.Id == new EmployeeGroupId(command.EmployeeGroupId), cancellationToken);
            if (group is null)
            {
                return Result<WorkScheduleResponse>.Failure(EmployeeGroupErrors.NotFound);
            }

            var schedule = group.WorkSchedules
                .FirstOrDefault(ws => ws.Id == new WorkScheduleId(command.ScheduleId));
            if (schedule is null)
            {
                return Result<WorkScheduleResponse>.Failure(EmployeeGroupErrors.WorkScheduleNotFound);
            }

            group.ActivateWorkSchedule(schedule.Id);

            await dbContext.SaveChangesAsync(cancellationToken);

            return Result<WorkScheduleResponse>.Success(EmployeeGroupMapper.ToResponse(schedule));
        }
    }

    public sealed class Endpoint : IEndpoint
    {
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            app.MapPost("employee-groups/{groupId:guid}/work-schedules/{scheduleId:guid}/activate", async (
                Guid groupId,
                Guid scheduleId,
                ICommandHandler<ActivateWorkScheduleCommand, WorkScheduleResponse> handler,
                CancellationToken ct) =>
            {
                var result = await handler.Handle(new ActivateWorkScheduleCommand(groupId, scheduleId), ct);
                return result.IsSuccess ? Results.Ok(result.Value) : result.Problem();
            })
            .RequireAuthorization()
            .WithTags("EmployeeGroups")
            .WithSummary("Activate work schedule")
            .WithDescription("Activates a work schedule for an employee group.")
            .Produces<WorkScheduleResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .WithName("ActivateWorkSchedule");
        }
    }
}