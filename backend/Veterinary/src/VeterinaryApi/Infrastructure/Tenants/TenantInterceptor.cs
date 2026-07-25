using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using VeterinaryApi.Common.Abstracions;
using VeterinaryApi.Domain.Common;
using VeterinaryApi.Domain.Users;

namespace VeterinaryApi.Infrastructure.Tenants;

/// <summary>
/// An EF Core <see cref="SaveChangesInterceptor"/> that implements per-user multi-tenancy by
/// automatically stamping the <c>TenantId</c> on all newly added <see cref="ITenantOwned"/> entities
/// before the database INSERT is executed.
/// </summary>
/// <remarks>
/// This interceptor fires during <c>SavingChangesAsync</c> (before the SQL write), ensuring that
/// every tenant-owned row is always associated with the currently authenticated user's identifier.
///
/// <b>Exclusion rule:</b> <see cref="User"/> entities are explicitly skipped because a user's
/// own <c>TenantId</c> is set manually to <c>Id</c> within the <c>User.Create</c> factory method,
/// and auto-stamping would overwrite that value with the admin's user ID.
///
/// If no user is authenticated (<see cref="ICurrentTenant.UserId"/> is <c>null</c>), the entity
/// is skipped silently (i.e., <c>TenantId</c> remains its default <see cref="Guid.Empty"/>).
/// </remarks>
public class TenantInterceptor : SaveChangesInterceptor
{
    private readonly ICurrentTenant _currentUser;

    /// <summary>
    /// Initializes the interceptor with the current-user/tenant accessor.
    /// </summary>
    /// <param name="currentUser">Provides the authenticated user's ID which doubles as the tenant key.</param>
    public TenantInterceptor(ICurrentTenant currentUser)
    {
        _currentUser = currentUser;
    }

    /// <summary>
    /// Called by EF Core immediately before executing the SQL <c>SaveChanges</c>.
    /// Iterates over all newly-added <see cref="ITenantOwned"/> entities (except <see cref="User"/>)
    /// and sets their <c>TenantId</c> to the authenticated user's <see cref="Guid"/>.
    /// </summary>
    /// <param name="eventData">EF Core event data providing access to the active <see cref="DbContext"/>.</param>
    /// <param name="result">The current interception result (passed through unchanged).</param>
    /// <param name="cancellationToken">A token to observe for cooperative cancellation.</param>
    /// <returns>The base interception result, allowing the save operation to proceed.</returns>
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        if (eventData.Context is not null)
        {
            var addedEntities = eventData.Context.ChangeTracker
               .Entries()
               .Where(e => e.Entity is ITenantOwned && e.State == EntityState.Added)
               .Select(e => e.Entity as ITenantOwned);

            foreach (var entity in addedEntities)
            {
                if (entity is null || entity is User)
                {
                    continue;
                }
                if (_currentUser.UserId is null)
                {
                    continue;
                }
                entity.TenantId = _currentUser.UserId.Value;
            }
        }
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }
}
