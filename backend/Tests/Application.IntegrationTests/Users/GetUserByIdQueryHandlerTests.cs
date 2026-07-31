using Application.IntegrationTests.Infrastructure;
using Application.IntegrationTests.TestBases;
using Microsoft.Extensions.DependencyInjection;
using Modules.Identity.Abstracions;
using Modules.Shared.Domain.Common;
using PublicApi.Domain.Common;
using PublicApi.Domain.Subscriptions;
using Modules.Identity.Domain.Users;
using Modules.Identity.Application.Users;
using PublicApi.Infrastructure.Persistence;

namespace Application.IntegrationTests.Users;

public sealed class GetUserByIdQueryHandlerTests : UsersTestBase
{
    public GetUserByIdQueryHandlerTests(PostgresFixture fixture) : base(fixture)
    {
    }

    [Fact]
    public async Task Handle_WhenUserMissing_ReturnsFailure()
    {
        using var scope = CreateScope();
        var handler = CreateGetUserByIdHandler(scope.ServiceProvider);
        var missingUserId = Guid.NewGuid();

        var result = await handler.Handle(
            new GetUserById.GetUserByIdQuery(missingUserId),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(UserErrors.UserNotFound(missingUserId).Code, result.Error.Code);
    }

    [Fact]
    public async Task Handle_WhenUserHasNoSubscription_ReturnsUserWithEmptySubscriptionFields()
    {
        using var scope = CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IIdentityApplicationDbContext>();
        var user = await SeedUserAsync(db, email: "user@test.local", userName: "user");
        var handler = CreateGetUserByIdHandler(scope.ServiceProvider);

        var result = await handler.Handle(
            new GetUserById.GetUserByIdQuery(user.Id),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Null(result.Value.SubscriptionStatus);
        Assert.False(result.Value.IsSubscriptionExist ?? true);
    }

    [Fact]
    public async Task Handle_WhenUserHasSubscription_ReturnsSubscriptionStatus()
    {
        using var scope = CreateScope();
        var identityDb = scope.ServiceProvider.GetRequiredService<IIdentityApplicationDbContext>();
        var appDb = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var user = await SeedUserAsync(identityDb, email: "user@test.local", userName: "user");
        SetCurrentUser(user.Id);
        var plan = SubscriptionPlan.Create(
            "Basic",
            "basic",
            Money.InDzd(1500m),
            "month",
            1,
            0);
        appDb.SubscriptionPlans.Add(plan);
        var subscription = Subscription.Create(user.Id, plan);
        subscription.Activate();
        appDb.Subscriptions.Add(subscription);
        await appDb.SaveChangesAsync();

        var handler = CreateGetUserByIdHandler(scope.ServiceProvider);

        var result = await handler.Handle(
            new GetUserById.GetUserByIdQuery(user.Id),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(SubscriptionStatus.Active.ToString(), result.Value.SubscriptionStatus);
        Assert.True(result.Value.IsSubscriptionExist);
    }
}
