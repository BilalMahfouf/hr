using FluentValidation;
using Modules.Employees.Application.Abstractions;
using Modules.Employees.Domain.EmployeeGroups;
using Modules.Employees.Domain.EmployeeGroups.WorkSchedules;
using Modules.Shared.CQRS;
using Modules.Shared.Endpoints;
using Modules.Shared.Results;
using PublicApi.Features.EmployeeGroups;

namespace PublicApi.Features.EmployeeGroups.WorkSchedules;

public static class UpdateWorkSchedule
{
    public sealed class Validator : AbstractValidator<UpdateWorkScheduleCommand>
    {
        public Validator()
        {
            RuleFor(x => x.ShiftStartTime).NotEmpty();
            RuleFor(x => x.ShiftEndTime).NotEmpty();
            RuleFor(x => x.BreakStartTime).NotEmpty();
            RuleFor(x => x.BreakEndTime).NotEmpty();
            RuleFor(x => x.EndDayOffset).GreaterThanOrEqualTo(0);
            RuleFor(x => x.AllowedCheckInLatenessMinutes).GreaterThanOrEqualTo(0);
            RuleFor(x => x.AllowedCheckOutEarlinessMinutes).GreaterThanOrEqualTo(0);

            RuleFor(x => x)
                .Must(ValidateShiftRange)
                .WithMessage("Shift start must be before shift end (unless endDayOffset > 0).");

            RuleFor(x => x)
                .Must(ValidateBreakRange)
                .WithMessage("Break start must be before break end (unless endDayOffset > 0).");

            RuleFor(x => x)
                .Must(ValidateBreakWithinShift)
                .WithMessage("Break must be within shift hours.");
        }

        private static bool ValidateShiftRange(UpdateWorkScheduleCommand r)
        {
            if (r.EndDayOffset > 0) return true;
            return r.ShiftStartTime < r.ShiftEndTime;
        }

        private static bool ValidateBreakRange(UpdateWorkScheduleCommand r)
        {
            if (r.EndDayOffset > 0) return true;
            return r.BreakStartTime < r.BreakEndTime;
        }

        private static bool ValidateBreakWithinShift(UpdateWorkScheduleCommand r)
        {
            if (r.EndDayOffset > 0) return true;
            return r.BreakStartTime >= r.ShiftStartTime && r.BreakEndTime <= r.ShiftEndTime;
        }
    }

    public sealed class Handler(
        IEmployeeGroupRepository repository,
        IEmployeeDbContext dbContext,
        IValidator<UpdateWorkScheduleCommand> validator)
        : ICommandHandler<UpdateWorkScheduleCommand, WorkScheduleResponse>
    {
        public async Task<Result<WorkScheduleResponse>> Handle(
            UpdateWorkScheduleCommand command,
            CancellationToken cancellationToken = default)
        {
            validator.ValidateAndThrow(command);

            var group = await repository.GetByIdWithDetailsAsync(new EmployeeGroupId(command.EmployeeGroupId), cancellationToken);
            if (group is null)
            {
                return Result<WorkScheduleResponse>.Failure(EmployeeGroupErrors.NotFound);
            }

            var schedule = group.WorkSchedules.FirstOrDefault(ws => ws.Id == new WorkScheduleId(command.ScheduleId));
            if (schedule is null)
            {
                return Result<WorkScheduleResponse>.Failure(EmployeeGroupErrors.WorkScheduleNotFound);
            }

            // Check if schedule is referenced by rotation
            var isReferenced = group.RotationEntries.Any(re => re.WorkScheduleId == schedule.Id);
            if (isReferenced)
            {
                return Result<WorkScheduleResponse>.Failure(EmployeeGroupErrors.WorkScheduleInUse);
            }

            var dto = new UpdateWorkScheduleDto(
                schedule.Id,
                group.Id,
                command.ShiftStartTime,
                command.ShiftEndTime,
                command.EndDayOffset,
                command.BreakStartTime,
                command.BreakEndTime,
                command.AllowedCheckInLatenessMinutes,
                command.AllowedCheckOutEarlinessMinutes);

            group.UpdateWorkSchedule(dto);
            var updatedSchedule = group.WorkSchedules.First(ws => ws.Id == schedule.Id);

            await dbContext.SaveChangesAsync(cancellationToken);

            var response = MapToResponse(updatedSchedule);
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
            .WithDescription("Updates a work schedule. Cannot update if schedule is referenced by rotation entries.")
            .Produces<WorkScheduleResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .WithName("UpdateWorkSchedule");
        }
    }
}