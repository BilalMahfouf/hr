using FluentValidation;
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

public static class UpdateWorkSchedule
{
    public sealed class Validator : WorkSchedulePayloadValidator<UpdateWorkScheduleCommand>
    {
    }

    public sealed class Handler(
        IEmployeeDbContext dbContext,
        IValidator<UpdateWorkScheduleCommand> validator)
        : ICommandHandler<UpdateWorkScheduleCommand, WorkScheduleResponse>
    {
        public async Task<Result<WorkScheduleResponse>> Handle(
            UpdateWorkScheduleCommand command,
            CancellationToken cancellationToken = default)
        {
            validator.ValidateAndThrow(command);

            var group = await dbContext.EmployeeGroups
                .Include(g => g.WorkSchedules)
                .Include(g => g.RotationEntries)
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

            var isReferenced = group.RotationEntries.Any(re => re.WorkScheduleId == schedule.Id);
            if (isReferenced)
            {
                return Result<WorkScheduleResponse>.Failure(EmployeeGroupErrors.WorkScheduleInUse);
            }

            var updated = group.UpdateWorkSchedule(new UpdateWorkScheduleDto(
                schedule.Id,
                group.Id,
                command.ShiftStartTime,
                command.ShiftEndTime,
                command.EndDayOffset,
                command.BreakStartTime,
                command.BreakEndTime,
                command.AllowedCheckInLatenessMinutes,
                command.AllowedCheckOutEarlinessMinutes));

            await dbContext.SaveChangesAsync(cancellationToken);

            return Result<WorkScheduleResponse>.Success(EmployeeGroupMapper.ToResponse(updated));
        }
    }

    public sealed class Endpoint : IEndpoint
    {
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            app.MapPut("employee-groups/{groupId:guid}/work-schedules/{scheduleId:guid}", async (
                Guid groupId,
                Guid scheduleId,
                UpdateWorkScheduleRequest request,
                ICommandHandler<UpdateWorkScheduleCommand, WorkScheduleResponse> handler,
                CancellationToken ct) =>
            {
                var command = new UpdateWorkScheduleCommand(
                    groupId,
                    scheduleId,
                    request.ShiftStartTime,
                    request.ShiftEndTime,
                    request.BreakStartTime,
                    request.BreakEndTime,
                    request.EndDayOffset,
                    request.AllowedCheckInLatenessMinutes,
                    request.AllowedCheckOutEarlinessMinutes);

                var result = await handler.Handle(command, ct);
                return result.IsSuccess ? Results.Ok(result.Value) : result.Problem();
            })
            .RequireAuthorization()
            .WithTags("EmployeeGroups")
            .WithSummary("Update work schedule")
            .WithDescription("Updates a work schedule. Cannot update if the schedule is referenced by rotation entries.")
            .Produces<WorkScheduleResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .WithName("UpdateWorkSchedule");
        }
    }
}