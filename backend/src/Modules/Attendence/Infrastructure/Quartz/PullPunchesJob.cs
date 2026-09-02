using Microsoft.Extensions.Logging;
using Modules.Attendence.Application.Importer;
using Modules.Shared.CQRS;
using Quartz;

namespace Modules.Attendence.Infrastructure.Quartz;

[DisallowConcurrentExecution]
public sealed class PullPunchesJob : IJob
{
    private readonly ICommandHandler<ImportAttendanceLogs.Command, ImportAttendanceLogs.Response> _importHandler;
    private readonly ILogger<PullPunchesJob> _logger;

    public PullPunchesJob(
        ICommandHandler<ImportAttendanceLogs.Command, ImportAttendanceLogs.Response> importHandler,
        ILogger<PullPunchesJob> logger)
    {
        _importHandler = importHandler;
        _logger = logger;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        _logger.LogInformation("PullPunchesJob: Starting punch poll");

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var command = new ImportAttendanceLogs.Command(today, today);

        var result = await _importHandler.Handle(command, context.CancellationToken);

        if (result.IsSuccess)
        {
            _logger.LogInformation(
                "PullPunchesJob: Poll completed. Machines: {MachineCount}, Punches: {PunchCount}",
                result.Value.MachineCount,
                result.Value.PunchCount);
        }
        else
        {
            _logger.LogError(
                "PullPunchesJob: Poll failed with error: {Error}",
                result.Error.Description);
        }
    }
}
