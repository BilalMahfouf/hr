using Modules.Shared.Errors;

namespace Modules.Employees.Domain.EmployeeGroups;

public static class EmployeeGroupErrors
{
    public static Error NotFound =>
        Error.NotFound(
            code: $"{nameof(EmployeeGroup)}.{nameof(NotFound)}",
            description: "Employee group not found.");

    public static Error InvalidName =>
        Error.Validation(
            code: $"{nameof(EmployeeGroup)}.{nameof(InvalidName)}",
            description: "Employee group name cannot be null, empty or whitespace.");

    public static Error InvalidNumberOfRotations =>
        Error.Validation(
            code: $"{nameof(EmployeeGroup)}.{nameof(InvalidNumberOfRotations)}",
            description: "Employee group number of rotations must be greater than zero.");

    public static Error WorkScheduleBelongsToAnotherGroup =>
        Error.Conflict(
            code: $"{nameof(EmployeeGroup)}.{nameof(WorkScheduleBelongsToAnotherGroup)}",
            description: "Work schedule belongs to another employee group.");

    public static Error ActiveWorkScheduleAlreadyExists =>
        Error.Conflict(
            code: $"{nameof(EmployeeGroup)}.{nameof(ActiveWorkScheduleAlreadyExists)}",
            description: "An active work schedule already exists for this employee group.");

    public static Error RotationNotFound =>
        Error.NotFound(
            code: $"{nameof(EmployeeGroup)}.{nameof(RotationNotFound)}",
            description: "Rotation entry not found.");

    public static Error RotationPositionAlreadyExists =>
        Error.Conflict(
            code: $"{nameof(EmployeeGroup)}.{nameof(RotationPositionAlreadyExists)}",
            description: "A rotation entry with this position already exists.");
    public static Error WorkScheduleUsedByRotation =>
        Error.Conflict(
            code: $"{nameof(EmployeeGroup)}.{nameof(WorkScheduleUsedByRotation)}",
            description: "Work schedule cannot be removed because it is used by a rotation.");

}