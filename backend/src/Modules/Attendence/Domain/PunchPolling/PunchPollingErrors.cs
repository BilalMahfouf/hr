using Modules.Shared.Errors;

namespace Modules.Attendence.Domain.PunchPolling;

public static class PunchPollingErrors
{
    public static Error InvalidIntervalMinutes(int interval) =>
        Error.Validation(
            $"{nameof(PunchPollingSettings)}.InvalidIntervalMinutes",
            $"Interval must be between 10 and 60 minutes. Got {interval}.");

    public static Error SettingsNotFound =>
        Error.NotFound(
            $"{nameof(PunchPollingSettings)}.NotFound",
            "Punch polling settings not found.");
}
