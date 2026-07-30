using Modules.Shared.Domain.Common;

namespace Modules.Shared.CQRS;

public interface IDomainEventHandler<in T> where T : IDomainEvent
{
    Task Handle(T domainEvent, CancellationToken cancellationToken);
}
