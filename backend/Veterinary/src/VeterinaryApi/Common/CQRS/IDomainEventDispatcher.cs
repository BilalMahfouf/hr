using VeterinaryApi.Domain.Common;

namespace VeterinaryApi.Common.CQRS;

/// <summary>
/// Defines the contract for dispatching collections of domain events to their handlers.
/// The dispatcher iterates through a set of domain events and invokes the appropriate
/// <see cref="IDomainEventHandler{T}"/> registered in the DI container.
/// </summary>
/// <remarks>
/// This interface is primarily used by the Outbox processing pipeline
/// in <c>ProcessOutboxMessagesJob</c> for sequential event-by-event dispatch.
/// Registered as a transient service to ensure a fresh dispatcher per dispatch cycle.
/// </remarks>
public interface IDomainEventDispatcher
{
    /// <summary>
    /// Dispatches all provided domain events to their registered handlers in sequence.
    /// Creates a new DI scope for each event to ensure proper handler lifecycle management.
    /// </summary>
    /// <param name="domainEvents">The collection of domain events to dispatch.</param>
    /// <param name="cancellationToken">Token to observe for async cancellation.</param>
    /// <returns>A <see cref="Task"/> that completes when all events have been dispatched.</returns>
    Task DispatchAsync(IEnumerable<IDomainEvent> domainEvents, CancellationToken cancellationToken = default);
}
