using Microsoft.Extensions.Logging;
using Modules.Attendence.Application.PunchPolling;
using Quartz;

namespace Modules.Attendence.Infrastructure.Quartz;

public sealed class QuartzPunchPollingScheduler : IPunchPollingScheduler
{
    private readonly ISchedulerFactory _schedulerFactory;
    private readonly ILogger<QuartzPunchPollingScheduler> _logger;

    private static readonly JobKey JobKey = new(nameof(PullPunchesJob));

    public QuartzPunchPollingScheduler(
        ISchedulerFactory schedulerFactory,
        ILogger<QuartzPunchPollingScheduler> logger)
    {
        _schedulerFactory = schedulerFactory;
        _logger = logger;
    }

    public async Task ScheduleAsync(
        bool isEnabled,
        int intervalMinutes,
        CancellationToken ct = default)
    {
        var scheduler = await _schedulerFactory.GetScheduler(ct);

        if (!isEnabled)
        {
            if (scheduler.CheckExists(JobKey, ct).GetAwaiter().GetResult())
            {
                await scheduler.DeleteJob(JobKey, ct);
                _logger.LogInformation(
                    "Punch polling disabled. Removed Quartz job {JobKey}", JobKey);
            }
            return;
        }

        var triggerKey = new TriggerKey($"{JobKey.Name}_trigger");

        var trigger = TriggerBuilder.Create()
            .WithIdentity(triggerKey)
            .ForJob(JobKey)
            .WithSimpleSchedule(schedule => schedule
                .WithIntervalInMinutes(intervalMinutes)
                .RepeatForever())
            .StartNow()
            .Build();

        if (scheduler.CheckExists(JobKey, ct).GetAwaiter().GetResult())
        {
            await scheduler.RescheduleJob(triggerKey, trigger, ct);
            _logger.LogInformation(
                "Punch polling rescheduled to every {Interval} minutes", intervalMinutes);
        }
        else
        {
            var job = JobBuilder.Create<PullPunchesJob>()
                .WithIdentity(JobKey)
                .Build();

            await scheduler.ScheduleJob(job, trigger, ct);
            _logger.LogInformation(
                "Punch polling scheduled every {Interval} minutes", intervalMinutes);
        }
    }
}
