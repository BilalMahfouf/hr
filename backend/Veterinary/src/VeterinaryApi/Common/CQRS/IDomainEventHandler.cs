using VeterinaryApi.Domain.Common;

namespace VeterinaryApi.Common.CQRS;

/// <summary>
/// Defines the contract for handling a specific type of domain event.
/// Domain event handlers are responsible for executing side effects in response
/// to something that has happened in the domain (e.g., sending notifications,
/// creating follow-up records, updating projections).
/// </summary>
/// <remarks>
/// Implementations are auto-discovered and registered as scoped services via Scrutor.
/// They are invoked asynchronously by <c>DomainEventPublisher</c> during Outbox message processing.
/// Multiple handlers can be registered for the same event type and are executed in parallel.
/// </remarks>
/// <typeparam name="T">The domain event type this handler processes.</typeparam>
public interface IDomainEventHandler<in T> where T : IDomainEvent
{
    /// <summary>
    /// Handles the specified domain event and performs the associated side effect.
    /// </summary>
    /// <param name="domainEvent">The domain event instance to process.</param>
    /// <param name="cancellationToken">Token to observe for async cancellation.</param>
    /// <returns>A <see cref="Task"/> that completes when the handler has finished.</returns>
    Task Handle(T domainEvent, CancellationToken cancellationToken);
}
