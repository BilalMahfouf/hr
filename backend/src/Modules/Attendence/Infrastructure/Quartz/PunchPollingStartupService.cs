using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Modules.Attendence.Application.PunchPolling;
using Modules.Attendence.Application.Shared;
using Microsoft.EntityFrameworkCore;

namespace Modules.Attendence.Infrastructure.Quartz;

public sealed class PunchPollingStartupService : IHostedService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IPunchPollingScheduler _scheduler;
    private readonly ILogger<PunchPollingStartupService> _logger;

    public PunchPollingStartupService(
        IServiceProvider serviceProvider,
        IPunchPollingScheduler scheduler,
        ILogger<PunchPollingStartupService> logger)
    {
        _serviceProvider = serviceProvider;
        _scheduler = scheduler;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IAttendanceDbContext>();

        var settings = await db.PunchPollingSettings
            .FirstOrDefaultAsync(cancellationToken);

        if (settings is null)
        {
            _logger.LogInformation(
                "No punch polling settings found. Polling is disabled by default.");
            return;
        }

        if (!settings.IsEnabled)
        {
            _logger.LogInformation("Punch polling is disabled.");
            return;
        }

        _logger.LogInformation(
            "Restoring punch polling schedule: enabled={IsEnabled}, interval={Interval} minutes",
            settings.IsEnabled,
            settings.IntervalMinutes);

        await _scheduler.ScheduleAsync(
            settings.IsEnabled,
            settings.IntervalMinutes,
            cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
