using System.Collections.Concurrent;
using Modules.Shared.CQRS;
using Modules.Shared.Domain.Common;

namespace PublicApi.Infrastructure.CQRS;

/// <summary>
/// Parallel implementation of <see cref="IDomainEventPublisher"/> that resolves all registered
/// handlers for a given domain event and invokes them concurrently via <c>Task.WhenAll</c>.
/// </summary>
/// <remarks>
/// Unlike <see cref="DomainEventsDispatcher"/>, this publisher does <b>not</b> create a child
/// DI scope per event. Handlers are resolved from the root <see cref="IServiceProvider"/>
/// passed at construction time. Callers (e.g., <see cref="ProcessOutboxMessagesJob"/>) must
/// ensure the provider/scope lifetime is appropriate.
///
/// Handler type resolution is cached in <see cref="_handlerTypeCache"/> keyed by event type.
/// Actual handler invocation uses <c>dynamic</c> dispatch to avoid reflection overhead at
/// the cost of compile-time safety. Consider migrating to the <see cref="HandlerWrapper"/>
/// approach used in <see cref="DomainEventsDispatcher"/> for better debuggability.
///
/// If no handlers are registered for an event type the method returns immediately without error.
/// </remarks>
public class DomainEventPublisher : IDomainEventPublisher
{
    private readonly IServiceProvider _serviceProvider;

    /// <summary>Cache mapping domain event type → closed <c>IDomainEventHandler&lt;T&gt;</c> type.</summary>
    private static readonly ConcurrentDictionary<Type, Type> _handlerTypeCache = new();

    /// <summary>
    /// Initializes the publisher with the root service provider.
    /// </summary>
    /// <param name="serviceProvider">The DI container used to resolve event handlers.</param>
    public DomainEventPublisher(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    /// <summary>
    /// Resolves all registered handlers for <paramref name="domainEvent"/>'s concrete type
    /// and invokes them in parallel. Returns only after all handlers complete.
    /// </summary>
    /// <param name="domainEvent">The domain event to publish.</param>
    /// <param name="ct">A token to observe for cooperative cancellation.</param>
    public async Task PublishAsync(
        IDomainEvent domainEvent,
        CancellationToken ct = default)
    {
        var eventType = domainEvent.GetType();
        var handlerType = _handlerTypeCache.GetOrAdd(
            eventType,
            t => typeof(IDomainEventHandler<>).MakeGenericType(t));

        // Resolve ALL handlers registered for this event type.
        var handlers = _serviceProvider.GetServices(handlerType);

        if (!handlers.Any()) return;

        // Execute all handlers in parallel and await all completions.
        var tasks = handlers.Select(handler =>
            InvokeHandlerAsync(handler!, domainEvent, ct));

        await Task.WhenAll(tasks);
    }

    /// <summary>
    /// Invokes a single handler using <c>dynamic</c> dispatch, avoiding reflection while
    /// preserving the ability to call any closed <c>IDomainEventHandler&lt;T&gt;.Handle</c>
    /// at runtime without knowing <c>T</c> statically.
    /// </summary>
    /// <param name="handler">The resolved handler instance.</param>
    /// <param name="domainEvent">The domain event to pass to the handler.</param>
    /// <param name="ct">The cancellation token.</param>
    private static async Task InvokeHandlerAsync(object handler, IDomainEvent domainEvent, CancellationToken ct)
    {
        // dynamic dispatch: compiles to a DLR call-site with internal caching.
        await ((dynamic)handler).Handle((dynamic)domainEvent, ct);
    }
}
