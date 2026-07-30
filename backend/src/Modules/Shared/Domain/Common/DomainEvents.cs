namespace Shared.Domain.Common;

public interface IDomainEvent : ITenantOwned;

public abstract record DomainEvent : IDomainEvent
{
    public Guid Id { get; init; } = Guid.NewGuid();

    public Guid TenantId { get; set; }
}
