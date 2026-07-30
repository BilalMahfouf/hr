using Identity.Abstracions;
using Identity.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Shared.Abstracions;
using VeterinaryApi.Common.Abstracions;
using VeterinaryApi.Domain.Notifications;
using VeterinaryApi.Infrastructure.Persistence.Configurations.Users;
using VeterinaryApi.Domain.Subscriptions;
using VeterinaryApi.Infrastructure.OutboxMessages;

namespace VeterinaryApi.Infrastructure.Persistence;

public class ApplicationDbContext : DbContext
    , IApplicationDbContext
    , IIdentityApplicationDbContext
{
    public ApplicationDbContext(DbContextOptions options) : base(options)
    {
    }

    public DbSet<User> Users { get; set; } = null!;

    public DbSet<UserSession> UserSessions { get; set; } = null!;

    public DbSet<OutboxMessage> OutboxMessages { get; set; } = null!;

    public DbSet<Notification> Notifications { get; set; } = null!;

    public DbSet<NotificationPushSubscription> NotificationPushSubscriptions { get; set; } = null!;

    public DbSet<Subscription> Subscriptions { get; set; }
    public DbSet<SubscriptionPlan> SubscriptionPlans { get; set; }
    public DbSet<Payment> SubscriptionPayments { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(UserConfiguration).Assembly);
    }

}
