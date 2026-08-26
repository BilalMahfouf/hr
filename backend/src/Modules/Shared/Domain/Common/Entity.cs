using Modules.Shared.Errors;

namespace Modules.Shared.Domain.Common;

public class Entity : IEntity, ISoftDelete
{
    public Guid Id { get; protected set; }

    public DateTime CreatedOnUtc { get; set; }

    public bool IsDeleted { get; private set; }

    public DateTime? DeletedOnUtc { get; private set; }

    private List<IDomainEvent> _events = new();

    public IReadOnlyCollection<IDomainEvent> DomainEvents => _events.AsReadOnly();

    protected void RaiseDomainEvent(IDomainEvent @event)
    {
        _events.Add(@event);
    }

    public void ClearDomainEvent()
    {
        _events.Clear();
    }

    public void Delete()
    {
        if (IsDeleted)
        {
            var error = Error.Conflict($"{this.GetType().Name}.AlreadyDelete",
                $"{this.GetType().Name} already deleted");

            throw new DomainException(error);
        }
        IsDeleted = true;
        DeletedOnUtc = DateTime.UtcNow;
    }

    public Entity()
    {
        Id = Guid.CreateVersion7();
        CreatedOnUtc = DateTime.UtcNow;
        IsDeleted = false;
    }
}

public interface IEntity
{
    public Guid Id { get; }

    public DateTime CreatedOnUtc { get; }
}
