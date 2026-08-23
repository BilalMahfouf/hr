using Modules.Shared.Errors;

namespace Modules.Employees.Domain.EmployeeGroups;

public static class EmployeeGroupErrors
{
    public static Error InvalidName =>
        Error.Validation(
            $"{nameof(EmployeeGroup)}.{nameof(InvalidName)}",
            "Employee group name cannot be null, empty or whitespace.");

    public static Error InvalidNumberOfRotations =>
        Error.Validation(
            $"{nameof(EmployeeGroup)}.{nameof(InvalidNumberOfRotations)}",
            "Employee group number of rotations must be greater than zero.");

    public static Error WorkScheduleBelongsToAnotherGroup =>
        Error.Conflict(
            $"{nameof(EmployeeGroup)}.{nameof(WorkScheduleBelongsToAnotherGroup)}",
            "Work schedule belongs to another employee group.");

    public static Error ActiveWorkScheduleAlreadyExists =>
        Error.Conflict(
            $"{nameof(EmployeeGroup)}.{nameof(ActiveWorkScheduleAlreadyExists)}",
            "An active work schedule already exists for this employee group.");
}