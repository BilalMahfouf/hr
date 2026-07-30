using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Quartz;
using Shared.Abstracions;
using VeterinaryApi.Common.Abstracions;
using VeterinaryApi.Domain.Subscriptions;

namespace VeterinaryApi.Features.Subscriptions.BackgroundJobs
{
    internal sealed class CleanPendingSubscriptionsDailyJob(
        IApplicationDbContext db,
        ILogger<CleanPendingSubscriptionsDailyJob> logger) : IJob
    {
        public async Task Execute(IJobExecutionContext context)
        {
            var ct = context.CancellationToken;
            var now = DateTime.UtcNow;

            logger.LogInformation("{JobName} started at {TimeUtc}", nameof(CleanPendingSubscriptionsDailyJob), now);

            try
            {
                var pendingSubscriptions = await db.Subscriptions
                    .Where(s => s.Status == SubscriptionStatus.Pending &&
                            s.CreatedOnUtc.AddDays(1) < now)
                    .OrderBy(s => s.CreatedOnUtc)
                    .Take(20)
                    .Include(s => s.Payments)
                    .ToListAsync(ct);

                if (pendingSubscriptions.Count == 0)
                {
                    logger.LogInformation("{JobName} found no stale pending subscriptions to clean", nameof(CleanPendingSubscriptionsDailyJob));
                    return;
                }

                foreach (var subscription in pendingSubscriptions)
                {
                    subscription.Delete();
                    foreach (var payment in subscription.Payments)
                    {
                        payment.Delete();
                    }
                }

                db.Subscriptions.UpdateRange(pendingSubscriptions);
                await db.SaveChangesAsync(ct);

                logger.LogInformation("{JobName} finished successfully. Cleaned {Count} pending subscriptions", nameof(CleanPendingSubscriptionsDailyJob), pendingSubscriptions.Count);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                logger.LogWarning("{JobName} was cancelled", nameof(CleanPendingSubscriptionsDailyJob));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "{JobName} failed unexpectedly", nameof(CleanPendingSubscriptionsDailyJob));
            }
        }
    }
}
