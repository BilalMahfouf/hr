namespace VeterinaryApi.Domain.Common;

public interface ICreatedBy
{
    public Guid CreatedByUserId { get; set; }
}
