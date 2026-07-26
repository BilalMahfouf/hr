namespace VeterinaryApi.Domain.Common;

public interface ISoftDelete
{
    public bool IsDeleted { get; }

    public DateTime? DeletedOnUtc { get; }

    public void Delete();
}
