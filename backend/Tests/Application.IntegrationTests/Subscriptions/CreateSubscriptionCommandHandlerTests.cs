using Application.IntegrationTests.Infrastructure;
using Application.IntegrationTests.TestBases;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
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
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var doctor = await SeedDoctorAsync(db);
        SetCurrentTenant(doctor.Id);
        var plan = await SeedPlanAsync(db);
        await SeedSubscriptionAsync(db, doctor.Id, plan, SubscriptionStatus.Active);
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
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var doctor = await SeedDoctorAsync(db);
        SetCurrentTenant(doctor.Id);
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
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var doctor = await SeedDoctorAsync(db);
        SetCurrentTenant(doctor.Id);
        var plan = await SeedPlanAsync(db, trialDays: 0);
        var handler = CreateCreateSubscriptionHandler(scope.ServiceProvider);

        var result = await handler.Handle(
            new CreateSubscirption.Command(doctor.Id, plan.Id),
            CancellationToken.None);

        Assert.True(result.IsSuccess);

        var subscription = await db.Subscriptions.SingleAsync(s => s.DoctorId == doctor.Id);
        Assert.Equal(SubscriptionStatus.Pending, subscription.Status);
        Assert.Equal(plan.Id, subscription.PlanId);
    }
}
