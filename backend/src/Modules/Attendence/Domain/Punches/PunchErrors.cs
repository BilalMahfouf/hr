using Modules.Shared.Errors;

namespace Modules.Attendence.Domain.Punches;

public static class PunchErrors
{
    public static Error PunchNotFound(Guid id) =>
        Error.NotFound(
            $"{nameof(Punch)}.NotFound",
            $"Punch with id {id} is not found");

    public static Error PunchesNotFound =>
        Error.NotFound(
            $"{nameof(Punch)}.PunchesNotFound",
            "No punches were found");
}