using Identity.Abstracions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shared.Abstracions;
using VeterinaryApi.Common.Abstracions;
using VeterinaryApi.Domain.Subscriptions;

namespace Application.IntegrationTests.Infrastructure;

public sealed class TestSubscriptionStatusQuery(
    IServiceProvider serviceProvider) : IUserSubscriptionStatusQuery
{
    public async Task<(string? Status, bool Exists)> GetSubscriptionStatusAsync(Guid userId)
    {
        using var scope = serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
        var status = await db.Subscriptions
            .AsNoTracking()
            .Where(e => e.DoctorId == userId)
            .OrderByDescending(e => e.CreatedOnUtc)
            .Select(e => e.Status.ToString())
            .FirstOrDefaultAsync();

        return (status, status is not null);
    }
}
