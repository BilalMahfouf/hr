using FluentValidation;
using Modules.Employees.Application.Abstractions;
using Modules.Employees.Domain.EmployeeGroups;
using Modules.Employees.Domain.EmployeeGroups.WorkSchedules;
using Modules.Shared.CQRS;
using Modules.Shared.Endpoints;
using Modules.Shared.Results;
using PublicApi.Features.EmployeeGroups;

namespace PublicApi.Features.EmployeeGroups.WorkSchedules;

public static class CreateWorkSchedule
{
    public sealed class Validator : AbstractValidator<CreateWorkScheduleCommand>
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

        private static bool ValidateShiftRange(CreateWorkScheduleCommand r)
        {
            if (r.EndDayOffset > 0) return true;
            return r.ShiftStartTime < r.ShiftEndTime;
        }

        private static bool ValidateBreakRange(CreateWorkScheduleCommand r)
        {
            if (r.EndDayOffset > 0) return true;
            return r.BreakStartTime < r.BreakEndTime;
        }

        private static bool ValidateBreakWithinShift(CreateWorkScheduleCommand r)
        {
            if (r.EndDayOffset > 0) return true;
            return r.BreakStartTime >= r.ShiftStartTime && r.BreakEndTime <= r.ShiftEndTime;
        }
    }

    public sealed class Handler(
        IEmployeeGroupRepository repository,
        IEmployeeDbContext dbContext,
        IValidator<CreateWorkScheduleCommand> validator)
        : ICommandHandler<CreateWorkScheduleCommand, WorkScheduleResponse>
    {
        public async Task<Result<WorkScheduleResponse>> Handle(
            CreateWorkScheduleCommand command,
            CancellationToken cancellationToken = default)
        {
            validator.ValidateAndThrow(command);

            var group = await repository.GetByIdAsync(new EmployeeGroupId(command.EmployeeGroupId), cancellationToken);
            if (group is null)
            {
                return Result<WorkScheduleResponse>.Failure(EmployeeGroupErrors.NotFound);
            }

            var dto = new CreateWorkScheduleDto(
                group.Id,
                command.ShiftStartTime,
                command.ShiftEndTime,
                command.EndDayOffset,
                command.BreakStartTime,
                command.BreakEndTime,
                command.AllowedCheckInLatenessMinutes,
                command.AllowedCheckOutEarlinessMinutes);

            group.AddWorkSchedule(dto);
            var schedule = group.WorkSchedules.Last();

            await dbContext.SaveChangesAsync(cancellationToken);

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
            app.MapPost("employee-groups/{groupId:guid}/work-schedules", async (
                Guid groupId,
                CreateWorkScheduleRequest request,
                ICommandHandler<CreateWorkScheduleCommand, WorkScheduleResponse> handler,
                CancellationToken ct) =>
            {
                var command = new CreateWorkScheduleCommand(
                    groupId,
                    request.ShiftStartTime,
                    request.ShiftEndTime,
                    request.BreakStartTime,
                    request.BreakEndTime,
                    request.EndDayOffset,
                    request.AllowedCheckInLatenessMinutes,
                    request.AllowedCheckOutEarlinessMinutes);

                var result = await handler.Handle(command, ct);
                return result.IsSuccess
                    ? Results.Created($"/api/v1/employee-groups/{groupId}/work-schedules/{result.Value.Id}", result.Value)
                    : result.Problem();
            })
            .RequireAuthorization()
            .WithTags("EmployeeGroups")
            .WithSummary("Create work schedule for employee group")
            .WithDescription("Adds a new work schedule to an employee group.")
            .Produces<WorkScheduleResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .WithName("CreateWorkSchedule");
        }
    }
}