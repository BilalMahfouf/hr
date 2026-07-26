using VeterinaryApi.Domain.Common;

namespace VeterinaryApi.Common.CQRS;

public interface IDomainEventPublisher
{
    Task PublishAsync(IDomainEvent domainEvent, CancellationToken ct = default);
}
