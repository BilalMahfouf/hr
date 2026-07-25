namespace VeterinaryApi.Common.Abstracions;

/// <summary>
/// Provides the identity of the currently authenticated user (tenant) for the current HTTP request.
/// In this system, each doctor is their own tenant, so the <see cref="UserId"/> serves
/// as both the user identifier and the tenant identifier for data isolation.
/// </summary>
/// <remarks>
/// Implemented by <c>CurrentUserService</c> in the Infrastructure layer, which reads
/// the <c>ClaimTypes.NameIdentifier</c> claim from the current HTTP context's user principal.
/// Returns <c>null</c> when no authenticated user is present (e.g., anonymous requests).
/// Registered as a scoped service so it reflects the user for each individual request.
/// </remarks>
public interface ICurrentTenant
{
    /// <summary>
    /// Gets the unique identifier of the currently authenticated user,
    /// or <c>null</c> if the request is unauthenticated.
    /// This value is also used as the <c>TenantId</c> for all data owned by this user.
    /// </summary>
    public Guid? UserId { get; }
}
