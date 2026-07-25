namespace VeterinaryApi.Domain.Common;

/// <summary>
/// Defines the multi-tenant ownership contract for domain entities.
/// Every entity that belongs to a specific clinic/doctor (tenant) must implement
/// this interface so that the <c>TenantInterceptor</c> can automatically stamp
/// the correct <c>TenantId</c> before persisting changes.
/// </summary>
/// <remarks>
/// In this system, the tenant is the individual doctor/user. Their <c>UserId</c>
/// serves as the <c>TenantId</c> for all data they own. All read queries must filter
/// by <c>TenantId</c> using the <c>.ForTenant(userId)</c> extension to ensure
/// strict data isolation between different clinic users.
/// </remarks>
public interface ITenantOwned
{
    /// <summary>
    /// Gets or sets the identifier of the tenant (doctor/user) that owns this entity.
    /// For <see cref="VeterinaryApi.Domain.Users.User"/> entities, this equals the user's own <c>Id</c>.
    /// For all other entities, this is automatically set by <c>TenantInterceptor</c>.
    /// </summary>
    public Guid TenantId { get; set; }
}
