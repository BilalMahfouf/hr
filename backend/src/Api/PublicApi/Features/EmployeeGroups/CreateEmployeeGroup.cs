using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Modules.Employees.Application.Abstractions;
using Modules.Employees.Domain.EmployeeGroups;
using Modules.Employees.Domain.EmployeeGroups.Rotation;
using Modules.Employees.Domain.EmployeeGroups.WorkSchedules;
using Modules.Shared.CQRS;
using Modules.Shared.Endpoints;
using Modules.Shared.Results;
using PublicApi.Features.EmployeeGroups;

namespace PublicApi.Features.EmployeeGroups;

public static class CreateEmployeeGroup
{
    public sealed class Validator : AbstractValidator<CreateEmployeeGroupCommand>
    {
        public Validator()
        {
            RuleFor(x => x.Name)
                .NotEmpty()
                .MaximumLength(100);

            RuleFor(x => x.RotationStartDate)
                .NotEqual(default(DateOnly))
                .WithMessage("Rotation start date is required.");

            RuleForEach(x => x.WorkSchedules).SetValidator(new WorkScheduleValidator());

            RuleForEach(x => x.RotationEntries).SetValidator(new RotationEntryValidator());

            RuleFor(x => x.RotationEntries)
                .Must(entries => entries.Select(e => e.Position).Distinct().Count() == entries.Count)
                .WithMessage("Rotation positions must be unique.")
                .When(x => x.RotationEntries != null && x.RotationEntries.Count > 0);

            RuleFor(x => x)
                .Must(ValidateWorkScheduleReferences)
                .WithMessage("Rotation entries reference work schedules that don't exist in the request.")
                .When(x => x.WorkSchedules != null && x.RotationEntries != null);
        }

        private static bool ValidateWorkScheduleReferences(CreateEmployeeGroupCommand cmd)
        {
            if (cmd.WorkSchedules == null || cmd.RotationEntries == null)
                return true;

            var scheduleIds = new HashSet<Guid>();
            // We can't know the IDs beforehand since they're generated server-side
            // This validation will be done in the handler after schedules are created
            return true;
        }
    }

    private sealed class WorkScheduleValidator : AbstractValidator<CreateWorkScheduleRequest>
    {
        public WorkScheduleValidator()
        {
            RuleFor(x => x.ShiftStartTime)
                .NotEmpty();

            RuleFor(x => x.ShiftEndTime)
                .NotEmpty();

            RuleFor(x => x.BreakStartTime)
                .NotEmpty();

            RuleFor(x => x.BreakEndTime)
                .NotEmpty();

            RuleFor(x => x.EndDayOffset)
                .GreaterThanOrEqualTo(0);

            RuleFor(x => x.AllowedCheckInLatenessMinutes)
                .GreaterThanOrEqualTo(0);

            RuleFor(x => x.AllowedCheckOutEarlinessMinutes)
                .GreaterThanOrEqualTo(0);

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
            RuleFor(x => x.Position)
                .GreaterThanOrEqualTo(1);
        }
    }

    public sealed class Handler(
        IEmployeeGroupRepository repository,
        IEmployeeDbContext dbContext,
        IValidator<CreateEmployeeGroupCommand> validator)
        : ICommandHandler<CreateEmployeeGroupCommand, EmployeeGroupResponse>
    {
        public async Task<Result<EmployeeGroupResponse>> Handle(
            CreateEmployeeGroupCommand command,
            CancellationToken cancellationToken = default)
        {
            validator.ValidateAndThrow(command);

            var nameExists = await repository.ExistsByNameAsync(command.Name, cancellationToken);
            if (nameExists)
            {
                return Result<EmployeeGroupResponse>.Failure(EmployeeGroupErrors.InvalidName);
            }

            var group = EmployeeGroup.Create(
                command.Name,
                command.IsSecurity,
                command.RotationStartDate,
                command.Description);

            // Add work schedules
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

            // Add rotation entries - validate workScheduleIds reference created schedules
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

            repository.Add(group);
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
            app.MapPost("employee-groups", async (
                CreateEmployeeGroupRequest request,
                ICommandHandler<CreateEmployeeGroupCommand, EmployeeGroupResponse> handler,
                CancellationToken ct) =>
            {
                var command = new CreateEmployeeGroupCommand(
                    request.Name,
                    request.IsSecurity,
                    request.Description,
                    request.RotationStartDate,
                    request.WorkSchedules,
                    request.RotationEntries);

                var result = await handler.Handle(command, ct);
                return result.IsSuccess
                    ? Results.Created($"/api/v1/employee-groups/{result.Value.Id}", result.Value)
                    : result.Problem();
            })
            .RequireAuthorization()
            .WithTags("EmployeeGroups")
            .WithSummary("Create employee group with schedules and rotations")
            .WithDescription("Creates a new employee group with work schedules and rotation entries in a single atomic transaction.")
            .Produces<EmployeeGroupResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .WithName("CreateEmployeeGroup");
        }
    }
}