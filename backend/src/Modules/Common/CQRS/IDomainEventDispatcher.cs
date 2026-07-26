using VeterinaryApi.Domain.Common;

namespace VeterinaryApi.Common.CQRS;

public interface IDomainEventDispatcher
{
    Task DispatchAsync(IEnumerable<IDomainEvent> domainEvents, CancellationToken cancellationToken = default);
}
