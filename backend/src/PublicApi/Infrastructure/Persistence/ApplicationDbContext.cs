using Microsoft.EntityFrameworkCore;
using Modules.Shared.Infrastructure.Outbox;
using PublicApi.Common.Abstracions;
using PublicApi.Domain.Notifications;
using PublicApi.Domain.Subscriptions;

namespace PublicApi.Infrastructure.Persistence;

public class ApplicationDbContext : DbContext, IApplicationDbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options) { }

    public DbSet<OutboxMessage> OutboxMessages { get; set; } = null!;

    public DbSet<Notification> Notifications { get; set; } = null!;

    public DbSet<NotificationPushSubscription> NotificationPushSubscriptions { get; set; } = null!;

    public DbSet<Subscription> Subscriptions { get; set; }
    public DbSet<SubscriptionPlan> SubscriptionPlans { get; set; }
    public DbSet<Payment> SubscriptionPayments { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ConfigureOutboxMessage();
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(Program).Assembly);
    }
}
