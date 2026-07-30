using Shared.Domain.Common;

namespace Shared.CQRS;

public interface IDomainEventPublisher
{
    Task PublishAsync(IDomainEvent domainEvent, CancellationToken ct = default);
}
