using Identity.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Shared.Abstracions;
using Shared.Domain.Common;

namespace VeterinaryApi.Infrastructure.Tenants;

public class TenantInterceptor : SaveChangesInterceptor
{
    private readonly ICurrentTenant _currentUser;

    public TenantInterceptor(ICurrentTenant currentUser)
    {
        _currentUser = currentUser;
    }

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
