using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Quartz;
using System.Diagnostics.CodeAnalysis;
using Shared.Abstracions;
using VeterinaryApi.Common.Abstracions;
using VeterinaryApi.Domain.Subscriptions;

namespace VeterinaryApi.Features.Subscriptions.BackgroundJobs;

internal sealed class MarkExpiredSubscriptionDailyJob(
    IApplicationDbContext db,
    ILogger<MarkExpiredSubscriptionDailyJob> logger) : IJob
{
    public async Task Execute(IJobExecutionContext context)
    {
        var ct = context.CancellationToken;
        var now = DateTime.UtcNow;

        logger.LogInformation("{JobName} started at {TimeUtc}", nameof(MarkExpiredSubscriptionDailyJob), now);

        try
        {
            var subscriptions = await db.Subscriptions
                .Where(e => e.Status == SubscriptionStatus.PastDue &&
                e.CurrentPeriodEnd.AddDays(1) < now)
                .ToListAsync(ct);

            if (subscriptions.Count == 0)
            {
                logger.LogInformation("{JobName} found no past-due subscriptions to mark expired", nameof(MarkExpiredSubscriptionDailyJob));
                return;
            }

            foreach (var subscription in subscriptions)
            {
                subscription.MarkExpired();
            }

            db.Subscriptions.UpdateRange(subscriptions);
            await db.SaveChangesAsync(ct);

            logger.LogInformation("{JobName} finished successfully. Marked {Count} subscriptions as expired", nameof(MarkExpiredSubscriptionDailyJob), subscriptions.Count);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            logger.LogWarning("{JobName} was cancelled", nameof(MarkExpiredSubscriptionDailyJob));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "{JobName} failed unexpectedly", nameof(MarkExpiredSubscriptionDailyJob));
        }
    }
}
