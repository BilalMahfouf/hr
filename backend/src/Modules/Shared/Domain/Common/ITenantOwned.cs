namespace Shared.Domain.Common;

public interface ITenantOwned
{
    public Guid TenantId { get; set; }
}
