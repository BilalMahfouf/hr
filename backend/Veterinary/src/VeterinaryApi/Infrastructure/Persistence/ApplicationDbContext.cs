using Microsoft.EntityFrameworkCore;
using VeterinaryApi.Common.Abstracions;
using VeterinaryApi.Domain.Notifications;
using VeterinaryApi.Domain.Subscriptions;
using VeterinaryApi.Domain.Users;
using VeterinaryApi.Infrastructure.OutboxMessages;
using VeterinaryApi.Infrastructure.Persistence.Configurations.Users;

namespace VeterinaryApi.Infrastructure.Persistence;

/// <summary>
/// The EF Core database context for the VeterinaryApi system.
/// Acts as the Unit of Work and serves as the single entry point for all
/// database interactions. Implements <see cref="IApplicationDbContext"/> to
/// support testability and layer decoupling.
/// </summary>
/// <remarks>
/// The context is configured to use PostgreSQL via Npgsql.
/// Two EF Core interceptors are registered:
/// <list type="bullet">
///   <item><description><c>InsertOutboxMessagesInterceptors</c> — captures domain events before save.</description></item>
///   <item><description><c>TenantInterceptor</c> — stamps <c>TenantId</c> on new entities before save.</description></item>
/// </list>
/// Entity configurations are applied automatically from all <c>IEntityTypeConfiguration&lt;T&gt;</c>
/// implementations in the assembly, following the EF Core convention-based configuration approach.
/// Registered as a scoped service so each HTTP request gets its own context instance.
/// </remarks>
public class ApplicationDbContext : DbContext
    , IApplicationDbContext
{
    /// <summary>
    /// Initializes the <see cref="ApplicationDbContext"/> with the provided options.
    /// Interceptors (Outbox, Tenant) are injected via the options builder in <c>DependencyInjection.cs</c>.
    /// </summary>
    public ApplicationDbContext(DbContextOptions options) : base(options)
    {
    }

    /// <inheritdoc />
    public DbSet<User> Users { get; set; } = null!;

    /// <inheritdoc />
    public DbSet<UserSession> UserSessions { get; set; } = null!;

    /// <inheritdoc />

    /// <inheritdoc />

    /// <inheritdoc />

    /// <inheritdoc />

    /// <inheritdoc />

    /// <inheritdoc />
    public DbSet<OutboxMessage> OutboxMessages { get; set; } = null!;

    /// <inheritdoc />
    public DbSet<Notification> Notifications { get; set; } = null!;

    /// <inheritdoc />
    public DbSet<NotificationPushSubscription> NotificationPushSubscriptions { get; set; } = null!;

    /// <inheritdoc />


    public DbSet<Subscription> Subscriptions { get; set; }
    public DbSet<SubscriptionPlan> SubscriptionPlans { get; set; }
    public DbSet<Payment> SubscriptionPayments { get; set; }


    /// <summary>
    /// Configures the EF Core model by applying all entity configurations found in the assembly.
    /// Configurations are scanned from the same assembly as <c>UserConfiguration</c>.
    /// </summary>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(UserConfiguration).Assembly);
    }

}
