namespace Modules.Attendence.Application.PunchPolling;

public interface IPunchPollingScheduler
{
    Task ScheduleAsync(bool isEnabled, int intervalMinutes, CancellationToken ct = default);
}
