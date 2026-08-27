using FluentValidation;
using Modules.Employees.Domain.EmployeeGroups;
using Modules.Employees.Domain.EmployeeGroups.WorkSchedules;

namespace Modules.Employees.Application.EmployeeGroups;

public interface IWorkSchedulePayload
{
    TimeOnly ShiftStartTime { get; }
    TimeOnly ShiftEndTime { get; }
    TimeOnly BreakStartTime { get; }
    TimeOnly BreakEndTime { get; }
    int EndDayOffset { get; }
    int AllowedCheckInLatenessMinutes { get; }
    int AllowedCheckOutEarlinessMinutes { get; }
}

public abstract class WorkSchedulePayloadValidator<T> : AbstractValidator<T>
    where T : IWorkSchedulePayload
{
    protected WorkSchedulePayloadValidator()
    {
        RuleFor(x => x.EndDayOffset)
            .GreaterThanOrEqualTo(0);

        RuleFor(x => x.AllowedCheckInLatenessMinutes)
            .GreaterThanOrEqualTo(0);

        RuleFor(x => x.AllowedCheckOutEarlinessMinutes)
            .GreaterThanOrEqualTo(0);

        RuleFor(x => x)
            .Must(r => r.EndDayOffset > 0 || r.ShiftStartTime < r.ShiftEndTime)
            .WithMessage("Shift start must be before shift end (unless endDayOffset > 0).");

        RuleFor(x => x)
            .Must(r => r.EndDayOffset > 0 || r.BreakStartTime < r.BreakEndTime)
            .WithMessage("Break start must be before break end (unless endDayOffset > 0).");

        RuleFor(x => x)
            .Must(x => ValidateBreakWithinShift(x))
            .WithMessage("Break must be within shift hours.");
    }

    private static bool ValidateBreakWithinShift(IWorkSchedulePayload r)
    {
        if (r.EndDayOffset > 0) return true;
        return r.BreakStartTime >= r.ShiftStartTime && r.BreakEndTime <= r.ShiftEndTime;
    }
}

public sealed class WorkScheduleRequestValidator : WorkSchedulePayloadValidator<CreateWorkScheduleRequest>
{
}

public static class WorkScheduleRequestExtensions
{
    public static CreateWorkScheduleDto ToDto(
        this CreateWorkScheduleRequest request,
        EmployeeGroupId groupId) =>
        new(
            groupId,
            request.ShiftStartTime,
            request.ShiftEndTime,
            request.EndDayOffset,
            request.BreakStartTime,
            request.BreakEndTime,
            request.AllowedCheckInLatenessMinutes,
            request.AllowedCheckOutEarlinessMinutes);
}