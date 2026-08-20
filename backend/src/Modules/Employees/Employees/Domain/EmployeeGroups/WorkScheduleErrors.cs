using Modules.Shared.Errors;

namespace Modules.Employees.Domain.EmployeeGroups;

public static class WorkScheduleErrors
{
    public static Error InvalidShiftRange =>
        Error.Conflict(
            $"{nameof(WorkSchedule)}.{nameof(InvalidShiftRange)}",
            "Shift start time must be before shift end time.");

    public static Error InvalidBreakRange =>
        Error.Conflict(
            $"{nameof(WorkSchedule)}.{nameof(InvalidBreakRange)}",
            "Break start time must be before break end time.");

    public static Error BreakOutsideShift =>
        Error.Conflict(
            $"{nameof(WorkSchedule)}.{nameof(BreakOutsideShift)}",
            "Break must be completely inside the shift.");

    public static Error InvalidCheckInLateness =>
        Error.Validation(
            $"{nameof(WorkSchedule)}.{nameof(InvalidCheckInLateness)}",
            "Allowed check-in lateness minutes cannot be negative.");

    public static Error InvalidCheckOutEarliness =>
        Error.Validation(
            $"{nameof(WorkSchedule)}.{nameof(InvalidCheckOutEarliness)}",
            "Allowed check-out earliness minutes cannot be negative.");
}