namespace VeterinaryApi.Domain.Common;

/// <summary>
/// Marker interface for all domain events in the system.
/// Domain events represent something that has happened within the domain model
/// and require side-effect handling (e.g., notifications, projections).
/// Implements <see cref="ITenantOwned"/> so every event carries the tenant context
/// needed for correct routing in the Outbox processing pipeline.
/// </summary>
public interface IDomainEvent : ITenantOwned;

/// <summary>
/// Abstract base record for all domain events.
/// Provides a unique event identifier and tenant ownership.
/// Derive from this record when defining domain events in an aggregate.
/// </summary>
/// <example>
/// <code>
/// public sealed record AppointmentCancelledDomainEvent(Guid AppointmentId) : DomainEvent();
/// </code>
/// </example>
public abstract record DomainEvent : IDomainEvent
{
    /// <summary>
    /// Gets the unique identifier for this specific domain event instance.
    /// Used to deduplicate events during outbox processing.
    /// </summary>
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>
    /// Gets or sets the tenant identifier associated with this event.
    /// Stamped by <c>InsertOutboxMessagesInterceptors</c> from the current
    /// authenticated user before the event is persisted to the outbox table.
    /// </summary>
    public Guid TenantId { get; set; }
}
