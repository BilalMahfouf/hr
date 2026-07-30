using Microsoft.EntityFrameworkCore;
using Shared.Domain.Common;

namespace VeterinaryApi.Infrastructure.Persistence;

/// <summary>Extension methods for <see cref="DbSet{T}"/> that enforce multi-tenant data isolation.</summary>
public static class QueryableExtensions
{
    /// <summary>
    /// Filters a <see cref="DbSet{T}"/> to return only entities belonging to the specified tenant.
    /// All read queries must call this extension to enforce per-clinic data isolation.
    /// </summary>
    /// <typeparam name="T">An entity type that implements <see cref="ITenantOwned"/>.</typeparam>
    /// <param name="dbSet">The EF Core <see cref="DbSet{T}"/> to filter.</param>
    /// <param name="TenantId">The tenant (doctor/user) identifier to filter by.</param>
    /// <returns>An <see cref="IQueryable{T}"/> scoped to the specified tenant.</returns>
    public static IQueryable<T> ForTenant<T>(this DbSet<T> dbSet, Guid TenantId)
        where T : class, ITenantOwned
    {
        return dbSet.Where(e => e.TenantId == TenantId);
    }
}
