using Application.IntegrationTests.Infrastructure;
using Application.IntegrationTests.TestBases;
using Microsoft.Extensions.DependencyInjection;
using VeterinaryApi.Domain.Subscriptions;
using VeterinaryApi.Domain.Subscriptions.Errors;
using MeEndpoint = VeterinaryApi.Features.Subscriptions.Endpoints.Me;
using VeterinaryApi.Features.Subscriptions.Endpoints;
using VeterinaryApi.Infrastructure.Persistence;

namespace Application.IntegrationTests.Subscriptions;

public sealed class SubscriptionMeQueryHandlerTests : SubscriptionsTestBase
{
    public SubscriptionMeQueryHandlerTests(PostgresFixture fixture) : base(fixture)
    {
    }

    [Fact]
    public async Task Handle_WhenNoSubscription_ReturnsFailure()
    {
        using var scope = CreateScope();
        var handler = CreateSubscriptionMeHandler(scope.ServiceProvider);

        var result = await handler.Handle(
            new MeEndpoint.Query(Guid.NewGuid()),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(SubscriptionErrors.NotFound.Code, result.Error.Code);
    }

    [Fact]
    public async Task Handle_WhenSubscriptionsExist_ReturnsLatest()
    {
        using var scope = CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var doctor = await SeedDoctorAsync(db);
        SetCurrentTenant(doctor.Id);
        var plan = await SeedPlanAsync(db);

        var oldSubscription = await SeedSubscriptionAsync(db, doctor.Id, plan, SubscriptionStatus.PastDue);
        oldSubscription.CreatedOnUtc = DateTime.UtcNow.AddMinutes(-10);
        db.Subscriptions.Update(oldSubscription);
        await db.SaveChangesAsync();

        var latestSubscription = await SeedSubscriptionAsync(db, doctor.Id, plan, SubscriptionStatus.Active);

        var handler = CreateSubscriptionMeHandler(scope.ServiceProvider);
        var result = await handler.Handle(
            new MeEndpoint.Query(doctor.Id),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(latestSubscription.Id, result.Value.Id);
    }
}
