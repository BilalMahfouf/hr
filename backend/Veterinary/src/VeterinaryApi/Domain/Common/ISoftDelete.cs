namespace VeterinaryApi.Domain.Common;

/// <summary>
/// Defines the soft-delete contract for domain entities that must not be physically removed
/// from the database. Instead of deletion, entities are flagged as deleted and excluded
/// from normal query results.
/// </summary>
/// <remarks>
/// All aggregate roots that need soft-delete behavior inherit from <see cref="Entity"/>,
/// which implements this interface. A global EF Core query filter should be configured
/// to automatically exclude <c>IsDeleted = true</c> records from queries.
/// </remarks>
public interface ISoftDelete
{
    /// <summary>Gets a value indicating whether this entity has been soft-deleted.</summary>
    public bool IsDeleted { get; }

    /// <summary>Gets the UTC timestamp at which this entity was deleted, or <c>null</c> if active.</summary>
    public DateTime? DeletedOnUtc { get; }

    /// <summary>
    /// Marks this entity as deleted.
    /// Implementations should throw a <see cref="DomainException"/> if the entity
    /// is already in a deleted state to prevent idempotency issues.
    /// </summary>
    public void Delete();
}
