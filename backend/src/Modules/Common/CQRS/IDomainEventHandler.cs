using VeterinaryApi.Domain.Common;

namespace VeterinaryApi.Common.CQRS;

public interface IDomainEventHandler<in T> where T : IDomainEvent
{
    Task Handle(T domainEvent, CancellationToken cancellationToken);
}
