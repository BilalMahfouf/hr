using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Modules.Shared.Abstracions;
using Modules.Shared.Domain.Common;

namespace PublicApi.Infrastructure.Interceptors;

/// <summary>
/// An EF Core <see cref="SaveChangesInterceptor"/> that automatically stamps
/// the <c>CreatedByUserId</c> field on newly added entities that implement <see cref="ICreatedBy"/>.
/// </summary>
/// <remarks>
/// This interceptor fires <b>after</b> the database write completes (<c>SavedChangesAsync</c>)
/// and sets the audit field on entities whose state was <see cref="EntityState.Added"/>.
///
/// <b>Note:</b> Because this runs post-save, the <c>CreatedByUserId</c> value is set in memory
/// but is not yet persisted. For it to reach the database a subsequent <c>SaveChanges</c> call
/// must be triggered or the entity must be modified further. Consider moving this logic to
/// <c>SavingChangesAsync</c> (pre-save) to guarantee the field is included in the initial INSERT.
///
/// The dependency on <see cref="ICurrentTenant"/> is named <c>_currentUser</c> to reflect its
/// semantic role as "current user context", even though the interface is typed as the tenant accessor.
/// </remarks>
public class AuditInterceptor : SaveChangesInterceptor
{
    private readonly ICurrentTenant _currentUser;

    /// <summary>
    /// Initializes the interceptor with the current-user/tenant accessor.
    /// </summary>
    /// <param name="currentUser">
    /// The ambient user context used to retrieve the ID of the user performing the write.
    /// </param>
    public AuditInterceptor(ICurrentTenant currentUser)
    {
        _currentUser = currentUser;
    }

    /// <summary>
    /// Called by EF Core immediately after a successful <c>SaveChanges</c> database call.
    /// Iterates over all newly-added entities that implement <see cref="ICreatedBy"/> and
    /// sets their <c>CreatedByUserId</c> to the currently-authenticated user's identifier.
    /// </summary>
    /// <param name="eventData">EF Core event data exposing the <see cref="DbContext"/> and result.</param>
    /// <param name="result">The number of state entries written to the database.</param>
    /// <param name="cancellationToken">A token to observe for cooperative cancellation.</param>
    /// <returns>The original <paramref name="result"/> value, forwarded from the base implementation.</returns>
    public override ValueTask<int> SavedChangesAsync(
        SaveChangesCompletedEventData eventData,
        int result,
        CancellationToken cancellationToken = default)
    {
        if (eventData.Context is not null)
        {
            var addedEntities = eventData.Context.ChangeTracker
               .Entries()
               .Where(e => e.Entity is ICreatedBy && e.State == EntityState.Added)
               .Select(e => e.Entity as ICreatedBy);

            foreach (var entity in addedEntities)
            {
                if (entity is null)
                {
                    continue;
                }
                if (_currentUser.UserId is null)
                {
                    continue;
                }
                entity.CreatedByUserId = _currentUser.UserId.Value;
            }
        }
        return base.SavedChangesAsync(eventData, result, cancellationToken);
    }
}
