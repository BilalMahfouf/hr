using Modules.Shared.Errors;

namespace Modules.Attendence.Domain.Machines;

public static class MachineErrors
{
    public static Error MachineNotFound(Guid id) =>
        Error.NotFound(
            $"{nameof(AttendenceMachine)}.NotFound",
            $"Attendance machine with id {id} is not found");
}
