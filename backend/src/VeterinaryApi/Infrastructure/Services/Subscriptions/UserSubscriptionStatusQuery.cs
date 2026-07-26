using Identity.Abstracions;
using Microsoft.EntityFrameworkCore;
using VeterinaryApi.Common.Abstracions;

namespace VeterinaryApi.Infrastructure.Services.Subscriptions;

internal sealed class UserSubscriptionStatusQuery(
    IApplicationDbContext db) : IUserSubscriptionStatusQuery
{
    public async Task<(string? Status, bool Exists)> GetSubscriptionStatusAsync(Guid userId)
    {
        var status = await db.Subscriptions
            .AsNoTracking()
            .Where(e => e.DoctorId == userId)
            .OrderByDescending(e => e.CreatedOnUtc)
            .Select(e => e.Status.ToString())
            .FirstOrDefaultAsync();

        return (status, status is not null);
    }
}
