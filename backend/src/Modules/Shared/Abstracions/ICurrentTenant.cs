namespace Shared.Abstracions;

public interface ICurrentTenant
{
    public Guid? UserId { get; }
}
