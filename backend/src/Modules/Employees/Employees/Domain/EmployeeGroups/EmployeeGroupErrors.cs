using Modules.Shared.Errors;

namespace Modules.Employees.Domain.EmployeeGroups;

public static class EmployeeGroupErrors
{
    public static Error NotFound =>
        Error.NotFound(
            $"{nameof(EmployeeGroup)}.{nameof(NotFound)}",
            "Employee group not found.");
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

    public static Error RotationStartDateRequired =>
        Error.Validation(
            $"{nameof(EmployeeGroup)}.{nameof(RotationStartDateRequired)}",
            "Rotation start date is required.");

    public static Error RotationEntryNotFound =>
        Error.NotFound(
            $"{nameof(EmployeeGroup)}.{nameof(RotationEntryNotFound)}",
            "Rotation entry not found.");

    public static Error WorkScheduleInUse =>
        Error.Conflict(
            $"{nameof(EmployeeGroup)}.{nameof(WorkScheduleInUse)}",
            "Cannot delete work schedule: it is referenced by one or more rotation entries.");

    public static Error InvalidRotationCount =>
        Error.Validation(
            $"{nameof(EmployeeGroup)}.{nameof(InvalidRotationCount)}",
            "Number of rotation entries must be greater than zero.");

    public static Error DuplicateRotationPosition =>
        Error.Conflict(
            $"{nameof(EmployeeGroup)}.{nameof(DuplicateRotationPosition)}",
            "Rotation position already exists.");

    public static Error WorkScheduleNotFound =>
        Error.NotFound(
            $"{nameof(EmployeeGroup)}.{nameof(WorkScheduleNotFound)}",
            "Work schedule not found in this group.");
}