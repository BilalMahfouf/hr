namespace VeterinaryApi.Infrastructure.OutboxMessages;

/// <summary>
/// Represents a serialized domain event stored in the outbox table.
/// The outbox pattern guarantees at-least-once delivery of domain events
/// by persisting them in the same database transaction as the entity changes,
/// and processing them asynchronously via <c>ProcessOutboxMessagesJob</c>.
/// </summary>
/// <remarks>
/// The <c>Name</c> field stores the assembly-qualified type name to enable
/// polymorphic deserialization via Newtonsoft.Json's <c>TypeNameHandling.All</c>.
/// Messages are processed in ascending <c>Id</c> order (effectively creation order).
/// A <c>null</c> <see cref="ProcessedOnUtc"/> means the message is still pending.
/// </remarks>
public class OutboxMessage
{
    /// <summary>Gets or sets the unique identifier of the outbox message.</summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the assembly-qualified type name of the serialized domain event.
    /// Used by <c>ProcessOutboxMessagesJob</c> to deserialize back to the correct type.
    /// Example: <c>"VeterinaryApi.Domain.Appointments.AppointmentCancelledDomainEvent, VeterinaryApi"</c>
    /// </summary>
    public string Name { get; set; } = null!;

    /// <summary>
    /// Gets or sets the JSON-serialized content of the domain event.
    /// Serialized with <c>TypeNameHandling.All</c> to preserve the concrete type.
    /// </summary>
    public string Content { get; set; } = null!;

    /// <summary>Gets or sets the UTC timestamp at which this message was inserted into the outbox.</summary>
    public DateTime CreatedOnUtc { get; set; }

    /// <summary>
    /// Gets or sets the UTC timestamp at which this message was successfully processed.
    /// A <c>null</c> value indicates the message is still pending processing.
    /// </summary>
    public DateTime? ProcessedOnUtc { get; set; } = null;

    /// <summary>
    /// Gets or sets any serialized error information if processing failed.
    /// Reserved for future retry/dead-letter logic. Currently not actively used.
    /// </summary>
    public string? Errors { get; set; } = null;

}
