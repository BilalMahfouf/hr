using Microsoft.EntityFrameworkCore;
using VeterinaryApi.Domain.Notifications;
using VeterinaryApi.Domain.Subscriptions;
using VeterinaryApi.Domain.Users;
using VeterinaryApi.Infrastructure.OutboxMessages;

namespace VeterinaryApi.Common.Abstracions;

/// <summary>
/// Defines the abstraction layer over the EF Core <c>ApplicationDbContext</c>.
/// Feature handlers depend on this interface rather than the concrete DbContext,
/// enabling testability (mock or in-memory implementations) and decoupling
/// the application logic from the infrastructure persistence layer.
/// </summary>
/// <remarks>
/// Registered in DI with scoped lifetime. Handlers receive this interface via constructor injection.
/// All write operations must call <see cref="SaveChangesAsync"/> to persist changes.
/// The concrete <c>ApplicationDbContext</c> implements this interface and applies
/// EF Core interceptors for tenant stamping, audit tracking, and outbox message insertion.
/// </remarks>
public interface IApplicationDbContext
{
    /// <summary>Gets the EF Core <see cref="DbSet{T}"/> for <see cref="User"/> entities.</summary>
    public DbSet<User> Users { get; }

    /// <summary>Gets the EF Core <see cref="DbSet{T}"/> for <see cref="UserSession"/> entities (refresh tokens).</summary>
    public DbSet<UserSession> UserSessions { get; }

    /// Gets the EF Core <see cref="DbSet{T}"/> for <see cref="OutboxMessage"/> entities.
    /// Used by the <c>InsertOutboxMessagesInterceptors</c> and <c>ProcessOutboxMessagesJob</c>.
    /// </summary>
    public DbSet<OutboxMessage> OutboxMessages { get; }

    /// <summary>Gets the EF Core <see cref="DbSet{T}"/> for <see cref="Notification"/> entities.</summary>
    public DbSet<Notification> Notifications { get; }

    /// <summary>Gets the EF Core <see cref="DbSet{T}"/> for <see cref="NotificationPushSubscription"/> entities.</summary>
    public DbSet<NotificationPushSubscription> NotificationPushSubscriptions { get; }

    /// <summary>Gets the EF Core <see cref="DbSet{T}"/> for <see cref="Vaccination"/> entities.</summary>

    public DbSet<Subscription> Subscriptions { get; }
    public DbSet<SubscriptionPlan> SubscriptionPlans { get; }
    public DbSet<Payment> SubscriptionPayments { get; }

    /// <summary>
    /// Asynchronously saves all pending changes in this unit of work to the database.
    /// This also triggers the EF Core interceptors (tenant stamping, outbox insertion, audit).
    /// </summary>
    /// <param name="cancellationToken">Token to observe for async cancellation.</param>
    /// <returns>The number of state entries written to the database.</returns>
    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
