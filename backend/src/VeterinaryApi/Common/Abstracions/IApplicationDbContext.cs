using Identity.Domain.Users;
using Microsoft.EntityFrameworkCore;
using VeterinaryApi.Domain.Notifications;
using VeterinaryApi.Domain.Subscriptions;
using VeterinaryApi.Infrastructure.OutboxMessages;

namespace VeterinaryApi.Common.Abstracions;

public interface IApplicationDbContext
{
    public DbSet<User> Users { get; }

    public DbSet<UserSession> UserSessions { get; }

    public DbSet<OutboxMessage> OutboxMessages { get; }

    public DbSet<Notification> Notifications { get; }

    public DbSet<NotificationPushSubscription> NotificationPushSubscriptions { get; }

    public DbSet<Subscription> Subscriptions { get; }
    public DbSet<SubscriptionPlan> SubscriptionPlans { get; }
    public DbSet<Payment> SubscriptionPayments { get; }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
