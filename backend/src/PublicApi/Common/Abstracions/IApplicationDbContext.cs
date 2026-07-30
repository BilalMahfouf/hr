using Microsoft.EntityFrameworkCore;
using PublicApi.Domain.Notifications;
using PublicApi.Domain.Subscriptions;
using PublicApi.Infrastructure.OutboxMessages;

namespace PublicApi.Common.Abstracions;

public interface IApplicationDbContext
{
    public DbSet<OutboxMessage> OutboxMessages { get; }

    public DbSet<Notification> Notifications { get; }

    public DbSet<NotificationPushSubscription> NotificationPushSubscriptions { get; }

    public DbSet<Subscription> Subscriptions { get; }
    public DbSet<SubscriptionPlan> SubscriptionPlans { get; }
    public DbSet<Payment> SubscriptionPayments { get; }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
