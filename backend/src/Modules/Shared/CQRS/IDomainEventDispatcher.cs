using Modules.Shared.Domain.Common;

namespace Modules.Shared.CQRS;

public interface IDomainEventDispatcher
{
    Task DispatchAsync(IEnumerable<IDomainEvent> domainEvents, CancellationToken cancellationToken = default);
}
