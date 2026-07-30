using Modules.Shared.Domain.Common;

namespace Modules.Shared.CQRS;

public interface IDomainEventPublisher
{
    Task PublishAsync(IDomainEvent domainEvent, CancellationToken ct = default);
}
