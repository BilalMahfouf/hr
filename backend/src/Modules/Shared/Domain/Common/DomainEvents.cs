namespace Modules.Shared.Domain.Common;

public interface IDomainEvent;

public abstract record DomainEvent : IDomainEvent
{
    public Guid Id { get; init; } = Guid.NewGuid();
}
