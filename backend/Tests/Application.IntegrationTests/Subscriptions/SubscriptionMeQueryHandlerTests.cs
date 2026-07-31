using Application.IntegrationTests.Infrastructure;
using Application.IntegrationTests.TestBases;
using Microsoft.Extensions.DependencyInjection;
using Modules.Identity.Abstracions;
using PublicApi.Domain.Subscriptions;
using PublicApi.Domain.Subscriptions.Errors;
using MeEndpoint = PublicApi.Features.Subscriptions.Endpoints.Me;
using PublicApi.Features.Subscriptions.Endpoints;
using PublicApi.Infrastructure.Persistence;

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
        var appDb = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var identityDb = scope.ServiceProvider.GetRequiredService<IIdentityApplicationDbContext>();
        var doctor = await SeedDoctorAsync(identityDb);
        SetCurrentUser(doctor.Id);
        var plan = await SeedPlanAsync(appDb);

        var oldSubscription = await SeedSubscriptionAsync(appDb, doctor.Id, plan, SubscriptionStatus.PastDue);
        oldSubscription.CreatedOnUtc = DateTime.UtcNow.AddMinutes(-10);
        appDb.Subscriptions.Update(oldSubscription);
        await appDb.SaveChangesAsync();

        var latestSubscription = await SeedSubscriptionAsync(appDb, doctor.Id, plan, SubscriptionStatus.Active);

        var handler = CreateSubscriptionMeHandler(scope.ServiceProvider);
        var result = await handler.Handle(
            new MeEndpoint.Query(doctor.Id),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(latestSubscription.Id, result.Value.Id);
    }
}
