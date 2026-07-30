namespace Modules.Shared.Abstracions;

public interface ICurrentTenant
{
    public Guid? UserId { get; }
}
