namespace Shared.Domain.Common;

public interface ICreatedBy
{
    public Guid CreatedByUserId { get; set; }
}
