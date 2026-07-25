using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using VeterinaryApi.Common.Errors;

namespace VeterinaryApi.Domain.Common;

/// <summary>
/// Abstract base class for all domain aggregate roots in the VeterinaryApi system.
/// Provides a unique identity, UTC creation timestamp, soft-delete support,
/// multi-tenant ownership stamping, and an internal domain event collection
/// that participates in the Outbox pattern.
/// </summary>
/// <remarks>
/// Every persistent domain entity must inherit from this class.
/// The <c>Id</c> and <c>CreatedOnUtc</c> are set automatically in the constructor.
/// Domain events raised via <see cref="RaiseDomainEvent"/> are harvested by
/// <c>InsertOutboxMessagesInterceptors</c> just before EF Core persists changes.
/// </remarks>
public class Entity : IEntity, ISoftDelete, ITenantOwned
{
    /// <summary>
    /// Gets the unique identifier of the entity.
    /// Automatically generated as a new <see cref="Guid"/> upon construction.
    /// </summary>
    public Guid Id { get; protected set; }

    /// <summary>
    /// Gets the UTC timestamp at which this entity was first created and persisted.
    /// </summary>
    public DateTime CreatedOnUtc { get; set; }

    /// <summary>
    /// Gets a value indicating whether this entity has been soft-deleted.
    /// Soft-deleted entities are not physically removed from the database.
    /// </summary>
    public bool IsDeleted { get; private set; }

    /// <summary>
    /// Gets the UTC timestamp at which this entity was soft-deleted,
    /// or <c>null</c> if the entity is still active.
    /// </summary>
    public DateTime? DeletedOnUtc { get; private set; }

    private List<IDomainEvent> _events = new();

    /// <summary>
    /// Gets a read-only snapshot of domain events raised by this entity
    /// during the current unit of work.
    /// These events are serialized and stored in the <c>OutboxMessages</c> table
    /// within the same database transaction as the entity changes.
    /// </summary>
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _events.AsReadOnly();

    /// <summary>
    /// Gets or sets the identifier of the tenant that owns this entity.
    /// For <c>User</c> entities, <c>TenantId == Id</c> (the doctor is their own tenant).
    /// For all other entities, this is stamped automatically by <c>TenantInterceptor</c>
    /// using the current authenticated user's ID.
    /// </summary>
    public Guid TenantId { get; set; }

    /// <summary>
    /// Adds a domain event to this entity's internal event collection.
    /// Should be called from entity business methods when a meaningful
    /// domain action occurs (e.g., appointment cancelled).
    /// </summary>
    /// <param name="event">The domain event instance to enqueue.</param>
    protected void RaiseDomainEvent(IDomainEvent @event)
    {
        _events.Add(@event);
    }

    /// <summary>
    /// Removes all domain events from this entity's collection.
    /// Called by <c>InsertOutboxMessagesInterceptors</c> after all events have been
    /// serialized into the outbox table, preventing them from being processed twice.
    /// </summary>
    public void ClearDomainEvent()
    {
        _events.Clear();
    }

    /// <summary>
    /// Marks this entity as soft-deleted by setting <see cref="IsDeleted"/> to <c>true</c>
    /// and recording <see cref="DeletedOnUtc"/>.
    /// The entity is retained in the database for audit and referential integrity purposes.
    /// </summary>
    /// <exception cref="DomainException">
    /// Thrown with a <c>Conflict</c> error when the entity has already been deleted,
    /// preventing double-deletion.
    /// </exception>
    public void Delete()
    {
        if (IsDeleted)
        {
            var error = Error.Conflict($"{this.GetType().Name}.AlreadyDelete",
                $"{this.GetType().Name} already deleted");

            throw new DomainException(error);
        }
        IsDeleted = true;
        DeletedOnUtc = DateTime.UtcNow;
    }

    /// <summary>
    /// Initializes a new <see cref="Entity"/> instance with a generated <see cref="Guid"/> ID,
    /// the current UTC timestamp, and <c>IsDeleted = false</c>.
    /// </summary>
    public Entity()
    {
        Id = Guid.NewGuid();
        CreatedOnUtc = DateTime.UtcNow;
        IsDeleted = false;
    }
}

/// <summary>
/// Defines the minimal read-only identity contract for all domain entities.
/// Exposes the unique identifier and the creation timestamp.
/// </summary>
public interface IEntity
{
    /// <summary>Gets the unique identifier of the entity.</summary>
    public Guid Id { get; }

    /// <summary>Gets the UTC timestamp at which the entity was created.</summary>
    public DateTime CreatedOnUtc { get; }
}
