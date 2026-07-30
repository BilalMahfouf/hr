using System.Collections.Concurrent;
using Shared.CQRS;
using Shared.Domain.Common;

namespace VeterinaryApi.Infrastructure.CQRS;

/// <summary>
/// Sequential implementation of <see cref="IDomainEventDispatcher"/> that dispatches a
/// collection of domain events one at a time, each within its own DI service scope.
/// </summary>
/// <remarks>
/// Handlers for each event are resolved from a fresh child scope to support scoped services
/// (e.g., <c>DbContext</c>) as handler dependencies. Resolution uses a type-cached
/// <c>IDomainEventHandler&lt;T&gt;</c> open-generic lookup to avoid repeated reflection.
///
/// The <see cref="HandlerWrapper"/> pattern provides compile-time generic type safety when
/// invoking <see cref="IDomainEventHandler{T}.Handle"/> without resorting to <c>dynamic</c>
/// dispatch — the concrete wrapper type is also cached in <see cref="WrapperTypeDictionary"/>.
///
/// This dispatcher is typically used within a Quartz job (<see cref="ProcessOutboxMessagesJob"/>)
/// or directly in unit/integration tests. For fire-and-forget parallel dispatch see
/// <see cref="DomainEventPublisher"/>.
/// </remarks>
internal sealed class DomainEventsDispatcher(IServiceProvider serviceProvider) : IDomainEventDispatcher
{
    /// <summary>Cache mapping a domain event type to its closed <c>IDomainEventHandler&lt;T&gt;</c> type.</summary>
    private static readonly ConcurrentDictionary<Type, Type> HandlerTypeDictionary = new();

    /// <summary>Cache mapping a domain event type to its closed <c>HandlerWrapper&lt;T&gt;</c> type.</summary>
    private static readonly ConcurrentDictionary<Type, Type> WrapperTypeDictionary = new();

    /// <summary>
    /// Dispatches each domain event in <paramref name="domainEvents"/> sequentially,
    /// invoking every registered handler for that event type in its own DI scope.
    /// </summary>
    /// <param name="domainEvents">The ordered collection of domain events to dispatch.</param>
    /// <param name="cancellationToken">A token to observe for cooperative cancellation.</param>
    public async Task DispatchAsync(
        IEnumerable<IDomainEvent> domainEvents,
        CancellationToken cancellationToken = default)
    {
        foreach (IDomainEvent domainEvent in domainEvents)
        {
            using IServiceScope scope = serviceProvider.CreateScope();

            Type domainEventType = domainEvent.GetType();
            Type handlerType = HandlerTypeDictionary.GetOrAdd(
                domainEventType,
                et => typeof(IDomainEventHandler<>).MakeGenericType(et));

            IEnumerable<object?> handlers = scope.ServiceProvider.GetServices(handlerType);

            foreach (object? handler in handlers)
            {
                if (handler is null)
                {
                    continue;
                }

                var handlerWrapper = HandlerWrapper.Create(handler, domainEventType);

                await handlerWrapper.Handle(domainEvent, cancellationToken);
            }
        }
    }

    /// <summary>
    /// Abstract base for a type-safe handler invocation wrapper.
    /// Avoids <c>dynamic</c> dispatch while supporting arbitrary concrete event types at runtime.
    /// </summary>
    private abstract class HandlerWrapper
    {
        /// <summary>Invokes the wrapped handler with the given domain event.</summary>
        /// <param name="domainEvent">The domain event to pass to the handler.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        public abstract Task Handle(IDomainEvent domainEvent, CancellationToken cancellationToken);

        /// <summary>
        /// Factory method: creates a <see cref="HandlerWrapper{T}"/> whose generic parameter
        /// matches <paramref name="domainEventType"/>, using a cached <see cref="Activator"/> call.
        /// </summary>
        /// <param name="handler">The resolved <c>IDomainEventHandler&lt;T&gt;</c> service instance.</param>
        /// <param name="domainEventType">The concrete domain event type.</param>
        /// <returns>A <see cref="HandlerWrapper"/> bound to the event type.</returns>
        public static HandlerWrapper Create(object handler, Type domainEventType)
        {
            Type wrapperType = WrapperTypeDictionary.GetOrAdd(
                domainEventType,
                et => typeof(HandlerWrapper<>).MakeGenericType(et));

            return (HandlerWrapper)Activator.CreateInstance(wrapperType, handler)!;
        }
    }

    /// <summary>
    /// Closed generic wrapper that casts both the handler and the event to their concrete
    /// types, enabling a direct strongly-typed <see cref="IDomainEventHandler{T}.Handle"/> call.
    /// </summary>
    /// <typeparam name="T">The specific domain event type being handled.</typeparam>
    private sealed class HandlerWrapper<T>(object handler) : HandlerWrapper where T : IDomainEvent
    {
        private readonly IDomainEventHandler<T> _handler = (IDomainEventHandler<T>)handler;

        /// <inheritdoc/>
        public override async Task Handle(IDomainEvent domainEvent, CancellationToken cancellationToken)
        {
            await _handler.Handle((T)domainEvent, cancellationToken);
        }
    }
}
