using Modules.Shared.Domain.Common;

namespace Modules.Attendence.Domain.PunchPolling;

public sealed class PunchPollingSettings : Entity
{
    public new PunchPollingSettingsId Id { get; private set; }

    public bool IsEnabled { get; private set; }

    public int IntervalMinutes { get; private set; }

    public DateTime UpdatedAt { get; private set; }

    private PunchPollingSettings()
    {
    }

    public static PunchPollingSettings Create(
        PunchPollingSettingsId id,
        bool isEnabled,
        int intervalMinutes)
    {
        return new PunchPollingSettings
        {
            Id = id,
            IsEnabled = isEnabled,
            IntervalMinutes = intervalMinutes,
            UpdatedAt = DateTime.UtcNow
        };
    }

    public void Update(bool isEnabled, int intervalMinutes)
    {
        IsEnabled = isEnabled;
        IntervalMinutes = intervalMinutes;
        UpdatedAt = DateTime.UtcNow;
    }
}
