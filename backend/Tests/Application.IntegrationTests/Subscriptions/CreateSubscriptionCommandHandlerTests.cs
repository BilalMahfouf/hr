using Application.IntegrationTests.Infrastructure;
using Application.IntegrationTests.TestBases;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Modules.Identity.Abstracions;
using PublicApi.Domain.Subscriptions;
using PublicApi.Domain.Subscriptions.Errors;
using PublicApi.Features.Subscriptions.Endpoints;
using PublicApi.Infrastructure.Persistence;

namespace Application.IntegrationTests.Subscriptions;

public sealed class CreateSubscriptionCommandHandlerTests : SubscriptionsTestBase
{
    public CreateSubscriptionCommandHandlerTests(PostgresFixture fixture) : base(fixture)
    {
    }

    [Fact]
    public async Task Handle_WhenCommandInvalid_ThrowsValidationException()
    {
        using var scope = CreateScope();
        var handler = CreateCreateSubscriptionHandler(scope.ServiceProvider);

        await Assert.ThrowsAsync<ValidationException>(() => handler.Handle(
            new CreateSubscirption.Command(Guid.Empty, Guid.NewGuid()),
            CancellationToken.None));
    }

    [Fact]
    public async Task Handle_WhenActiveSubscriptionExists_ReturnsFailure()
    {
        using var scope = CreateScope();
        var appDb = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var identityDb = scope.ServiceProvider.GetRequiredService<IIdentityApplicationDbContext>();
        var doctor = await SeedDoctorAsync(identityDb);
        SetCurrentUser(doctor.Id);
        var plan = await SeedPlanAsync(appDb);
        await SeedSubscriptionAsync(appDb, doctor.Id, plan, SubscriptionStatus.Active);
        var handler = CreateCreateSubscriptionHandler(scope.ServiceProvider);

        var result = await handler.Handle(
            new CreateSubscirption.Command(doctor.Id, plan.Id),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(SubscriptionErrors.AlreadyExistAcitveSubscription.Code, result.Error.Code);
    }

    [Fact]
    public async Task Handle_WhenPlanMissing_ReturnsFailure()
    {
        using var scope = CreateScope();
        var appDb = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var identityDb = scope.ServiceProvider.GetRequiredService<IIdentityApplicationDbContext>();
        var doctor = await SeedDoctorAsync(identityDb);
        SetCurrentUser(doctor.Id);
        var handler = CreateCreateSubscriptionHandler(scope.ServiceProvider);
        var planId = Guid.NewGuid();

        var result = await handler.Handle(
            new CreateSubscirption.Command(doctor.Id, planId),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(SubscriptionPlanErrors.SubscriptionPlanNotFound(planId).Code, result.Error.Code);
    }

    [Fact]
    public async Task Handle_WhenValid_CreatesSubscription()
    {
        using var scope = CreateScope();
        var appDb = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var identityDb = scope.ServiceProvider.GetRequiredService<IIdentityApplicationDbContext>();
        var doctor = await SeedDoctorAsync(identityDb);
        SetCurrentUser(doctor.Id);
        var plan = await SeedPlanAsync(appDb, trialDays: 0);
        var handler = CreateCreateSubscriptionHandler(scope.ServiceProvider);

        var result = await handler.Handle(
            new CreateSubscirption.Command(doctor.Id, plan.Id),
            CancellationToken.None);

        Assert.True(result.IsSuccess);

        var subscription = await appDb.Subscriptions.SingleAsync(s => s.DoctorId == doctor.Id);
        Assert.Equal(SubscriptionStatus.Pending, subscription.Status);
        Assert.Equal(plan.Id, subscription.PlanId);
    }
}
