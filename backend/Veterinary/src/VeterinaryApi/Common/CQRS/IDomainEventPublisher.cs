using VeterinaryApi.Domain.Common;

namespace VeterinaryApi.Common.CQRS;

/// <summary>
/// Defines the contract for publishing a single domain event to all of its registered handlers.
/// Unlike <see cref="IDomainEventDispatcher"/> (which dispatches a batch sequentially),
/// this publisher resolves and invokes all handlers for a single event in parallel using
/// <c>Task.WhenAll</c> for improved throughput.
/// </summary>
/// <remarks>
/// This is the primary event publishing mechanism used by the Outbox job
/// (<c>ProcessOutboxMessagesJob</c>). Registered as a transient service.
/// </remarks>
public interface IDomainEventPublisher
{
    /// <summary>
    /// Publishes the specified domain event to all registered
    /// <see cref="IDomainEventHandler{T}"/> implementations in parallel.
    /// </summary>
    /// <param name="domainEvent">The domain event to publish.</param>
    /// <param name="ct">Token to observe for async cancellation.</param>
    /// <returns>A <see cref="Task"/> that completes when all handlers have been invoked.</returns>
    Task PublishAsync(IDomainEvent domainEvent, CancellationToken ct = default);
}
