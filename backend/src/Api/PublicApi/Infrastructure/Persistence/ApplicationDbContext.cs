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

    public DbSet<Notification> Notifications { get; set; } = null!;

    public DbSet<NotificationPushSubscription> NotificationPushSubscriptions { get; set; } = null!;

    public DbSet<Subscription> Subscriptions { get; set; }
    public DbSet<SubscriptionPlan> SubscriptionPlans { get; set; }
    public DbSet<Payment> SubscriptionPayments { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        // The shared outbox table is mapped (so InsertOutboxMessagesInterceptors can write
        // domain events in this context's transaction) but its migrations are owned
        // by SharedDbContext in Modules.Shared.
        modelBuilder.ConfigureOutboxMessage(excludeFromMigrations: true);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(Program).Assembly);
    }
}
