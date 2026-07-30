using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Quartz;
using Shared.Abstracions;
using VeterinaryApi.Common.Abstracions;
using VeterinaryApi.Domain.Subscriptions;

namespace VeterinaryApi.Features.Subscriptions.BackgroundJobs;

internal sealed class MarkPastDueSubscriptionDailyJob(
    IApplicationDbContext db,
    ILogger<MarkPastDueSubscriptionDailyJob> logger) : IJob
{
    public async Task Execute(IJobExecutionContext context)
    {
        var ct = context.CancellationToken;
        var now = DateTime.UtcNow;

        logger.LogInformation("{JobName} started at {TimeUtc}", nameof(MarkPastDueSubscriptionDailyJob), now);

        try
        {
            var subscriptions = await db.Subscriptions
                .Where(e => e.Status == SubscriptionStatus.Active
                && e.CurrentPeriodEnd < now)
                .ToListAsync(ct);

            if (subscriptions.Count == 0)
            {
                logger.LogInformation("{JobName} found no active subscriptions to mark past due", nameof(MarkPastDueSubscriptionDailyJob));
                return;
            }

            foreach (var subscription in subscriptions)
            {
                subscription.MarkPastDue();
            }

            db.Subscriptions.UpdateRange(subscriptions);
            await db.SaveChangesAsync(ct);

            logger.LogInformation("{JobName} finished successfully. Marked {Count} subscriptions as past due", nameof(MarkPastDueSubscriptionDailyJob), subscriptions.Count);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            logger.LogWarning("{JobName} was cancelled", nameof(MarkPastDueSubscriptionDailyJob));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "{JobName} failed unexpectedly", nameof(MarkPastDueSubscriptionDailyJob));
        }
    }
}
