using FluentValidation;
using Modules.Employees.Application.Abstractions;
using Modules.Employees.Domain.EmployeeGroups;
using Modules.Employees.Domain.EmployeeGroups.Rotation;
using Modules.Employees.Domain.EmployeeGroups.WorkSchedules;
using Modules.Shared.CQRS;
using Modules.Shared.Endpoints;
using Modules.Shared.Results;
using PublicApi.Features.EmployeeGroups;

namespace PublicApi.Features.EmployeeGroups;

public static class ReplaceSchedulesAndRotations
{
    public sealed class Validator : AbstractValidator<ReplaceSchedulesAndRotationsCommand>
    {
        public Validator()
        {
            RuleForEach(x => x.WorkSchedules).SetValidator(new WorkScheduleValidator());

            RuleForEach(x => x.RotationEntries).SetValidator(new RotationEntryValidator());

            RuleFor(x => x.RotationEntries)
                .Must(entries => entries.Select(e => e.Position).Distinct().Count() == entries.Count)
                .WithMessage("Rotation positions must be unique.")
                .When(x => x.RotationEntries != null && x.RotationEntries.Count > 0);
        }
    }

    private sealed class WorkScheduleValidator : AbstractValidator<CreateWorkScheduleRequest>
    {
        public WorkScheduleValidator()
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

        private static bool ValidateShiftRange(CreateWorkScheduleRequest r)
        {
            if (r.EndDayOffset > 0) return true;
            return r.ShiftStartTime < r.ShiftEndTime;
        }

        private static bool ValidateBreakRange(CreateWorkScheduleRequest r)
        {
            if (r.EndDayOffset > 0) return true;
            return r.BreakStartTime < r.BreakEndTime;
        }

        private static bool ValidateBreakWithinShift(CreateWorkScheduleRequest r)
        {
            if (r.EndDayOffset > 0) return true;
            return r.BreakStartTime >= r.ShiftStartTime && r.BreakEndTime <= r.ShiftEndTime;
        }
    }

    private sealed class RotationEntryValidator : AbstractValidator<CreateRotationEntryRequest>
    {
        public RotationEntryValidator()
        {
            RuleFor(x => x.Position).GreaterThanOrEqualTo(1);
        }
    }

    public sealed class Handler(
        IEmployeeGroupRepository repository,
        IEmployeeDbContext dbContext,
        IValidator<ReplaceSchedulesAndRotationsCommand> validator)
        : ICommandHandler<ReplaceSchedulesAndRotationsCommand, EmployeeGroupResponse>
    {
        public async Task<Result<EmployeeGroupResponse>> Handle(
            ReplaceSchedulesAndRotationsCommand command,
            CancellationToken cancellationToken = default)
        {
            validator.ValidateAndThrow(command);

            var group = await repository.GetByIdWithDetailsAsync(new EmployeeGroupId(command.GroupId), cancellationToken);
            if (group is null)
            {
                return Result<EmployeeGroupResponse>.Failure(EmployeeGroupErrors.NotFound);
            }

            // Remove all rotation entries first (to avoid FK issues when removing schedules)
            group.ReplaceRotationEntries([]);

            // Remove all work schedules
            foreach (var schedule in group.WorkSchedules.ToList())
            {
                group.RemoveWorkSchedule(schedule.Id);
            }

            // Add new work schedules
            var createdSchedules = new List<WorkSchedule>();
            foreach (var wsReq in command.WorkSchedules ?? [])
            {
                var dto = new CreateWorkScheduleDto(
                    group.Id,
                    wsReq.ShiftStartTime,
                    wsReq.ShiftEndTime,
                    wsReq.EndDayOffset,
                    wsReq.BreakStartTime,
                    wsReq.BreakEndTime,
                    wsReq.AllowedCheckInLatenessMinutes,
                    wsReq.AllowedCheckOutEarlinessMinutes);
                group.AddWorkSchedule(dto);
                createdSchedules.Add(group.WorkSchedules.Last());
            }

            // Add new rotation entries - validate workScheduleIds reference created schedules
            var rotationEntries = new List<(int Position, WorkScheduleId? WorkScheduleId)>();
            foreach (var rotReq in command.RotationEntries ?? [])
            {
                WorkScheduleId? wsId = null;
                if (rotReq.WorkScheduleId.HasValue)
                {
                    var schedule = createdSchedules.FirstOrDefault(s => s.Id == rotReq.WorkScheduleId.Value);
                    if (schedule is null)
                    {
                        return Result<EmployeeGroupResponse>.Failure(EmployeeGroupErrors.WorkScheduleNotFound);
                    }
                    wsId = schedule.Id;
                }
                rotationEntries.Add((rotReq.Position, wsId));
            }

            group.ReplaceRotationEntries(rotationEntries);

            await dbContext.SaveChangesAsync(cancellationToken);

            var response = MapToResponse(group);
            return Result<EmployeeGroupResponse>.Success(response);
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
            app.MapPut("employee-groups/{groupId:guid}/schedules-and-rotations", async (
                Guid groupId,
                ReplaceSchedulesAndRotationsRequest request,
                ICommandHandler<ReplaceSchedulesAndRotationsCommand, EmployeeGroupResponse> handler,
                CancellationToken ct) =>
            {
                var command = new ReplaceSchedulesAndRotationsCommand(groupId, request.WorkSchedules, request.RotationEntries);
                var result = await handler.Handle(command, ct);
                return result.IsSuccess ? Results.Ok(result.Value) : result.Problem();
            })
            .RequireAuthorization()
            .WithTags("EmployeeGroups")
            .WithSummary("Replace all schedules and rotations for a group")
            .WithDescription("Atomically replaces all work schedules and rotation entries for an employee group.")
            .Produces<EmployeeGroupResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .WithName("ReplaceSchedulesAndRotations");
        }
    }
}