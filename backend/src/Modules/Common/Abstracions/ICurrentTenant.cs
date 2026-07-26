namespace VeterinaryApi.Common.Abstracions;

public interface ICurrentTenant
{
    public Guid? UserId { get; }
}
