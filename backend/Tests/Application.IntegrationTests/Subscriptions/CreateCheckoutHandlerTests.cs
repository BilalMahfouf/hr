using Application.IntegrationTests.Infrastructure;
using Application.IntegrationTests.TestBases;
using Chargily.Pay;
using Chargily.Pay.Models;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using PublicApi.Domain.Subscriptions;
using PublicApi.Domain.Subscriptions.Errors;
using Modules.Identity.Domain.Users;
using PublicApi.Features.Subscriptions.Endpoints;
using PublicApi.Infrastructure.Persistence;

namespace Application.IntegrationTests.Subscriptions;

public sealed class CreateCheckoutHandlerTests : SubscriptionsTestBase
{
    public CreateCheckoutHandlerTests(PostgresFixture fixture) : base(fixture)
    {
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
        var handler = CreateCreateCheckoutHandler(scope.ServiceProvider);

        var result = await handler.Handle(
            new CreateCheckout.CreateSubscriptionCheckoutCommand(doctor.Id, plan.Id, "key-1"),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(SubscriptionErrors.ActiveSubscriptionAlreadyExist.Code, result.Error.Code);
    }

    [Fact]
    public async Task Handle_WhenExistingPaymentHasCheckout_ReturnsExistingCheckout()
    {
        using var scope = CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var doctor = await SeedDoctorAsync(db);
        SetCurrentTenant(doctor.Id);
        var plan = await SeedPlanAsync(db);
        var subscription = await SeedSubscriptionAsync(db, doctor.Id, plan, SubscriptionStatus.Pending);
        var payment = await SeedPaymentAsync(
            db,
            subscription,
            doctor.Id,
            "idempotency-1",
            providerPaymentId: "provider-123",
            status: PaymentStatus.Pending);

        var checkout = BuildCheckoutResponse("provider-123", new Uri("https://checkout.test/existing"));
        ChargilyClientMock
            .Setup(c => c.GetCheckout("provider-123"))
            .ReturnsAsync(checkout);

        var handler = CreateCreateCheckoutHandler(scope.ServiceProvider);
        var result = await handler.Handle(
            new CreateCheckout.CreateSubscriptionCheckoutCommand(doctor.Id, plan.Id, "idempotency-1"),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(checkout.Value.CheckoutUrl.ToString(), result.Value.CheckoutUrl);
        Assert.Equal(payment.SubscriptionId, result.Value.SubscriptionId);
        Assert.Null(result.Value.SubscriptionStatus);
    }

    [Fact]
    public async Task Handle_WhenExistingPaymentCheckoutMissing_ReturnsFailure()
    {
        using var scope = CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var doctor = await SeedDoctorAsync(db);
        SetCurrentTenant(doctor.Id);
        var plan = await SeedPlanAsync(db);
        var subscription = await SeedSubscriptionAsync(db, doctor.Id, plan, SubscriptionStatus.Pending);
        var payment = await SeedPaymentAsync(
            db,
            subscription,
            doctor.Id,
            "idempotency-1",
            providerPaymentId: "provider-123",
            status: PaymentStatus.Pending);

        ChargilyClientMock
            .Setup(c => c.GetCheckout("provider-123"))
            .ReturnsAsync((Response<CheckoutResponse>?)null);

        var handler = CreateCreateCheckoutHandler(scope.ServiceProvider);
        var result = await handler.Handle(
            new CreateCheckout.CreateSubscriptionCheckoutCommand(doctor.Id, plan.Id, "idempotency-1"),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(SubscriptionErrors.FailedToRetrieveCheckout(payment.Id).Code, result.Error.Code);
    }

    [Fact]
    public async Task Handle_WhenDoctorMissing_ReturnsFailure()
    {
        using var scope = CreateScope();
        var handler = CreateCreateCheckoutHandler(scope.ServiceProvider);

        var result = await handler.Handle(
            new CreateCheckout.CreateSubscriptionCheckoutCommand(Guid.NewGuid(), Guid.NewGuid(), "key-1"),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(UserErrors.NotFound.Code, result.Error.Code);
    }

    [Fact]
    public async Task Handle_WhenPlanMissing_ReturnsFailure()
    {
        using var scope = CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var doctor = await SeedDoctorAsync(db);
        SetCurrentTenant(doctor.Id);
        var handler = CreateCreateCheckoutHandler(scope.ServiceProvider);
        var planId = Guid.NewGuid();

        var result = await handler.Handle(
            new CreateCheckout.CreateSubscriptionCheckoutCommand(doctor.Id, planId, "key-1"),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(SubscriptionPlanErrors.SubscriptionPlanNotFound(planId).Code, result.Error.Code);
    }

    [Fact]
    public async Task Handle_WhenPlanHasTrial_ReturnsTrialingResponse()
    {
        using var scope = CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var doctor = await SeedDoctorAsync(db);
        SetCurrentTenant(doctor.Id);
        var plan = await SeedPlanAsync(db, trialDays: 7);
        var handler = CreateCreateCheckoutHandler(scope.ServiceProvider);

        var result = await handler.Handle(
            new CreateCheckout.CreateSubscriptionCheckoutCommand(doctor.Id, plan.Id, "key-1"),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Null(result.Value.CheckoutUrl);
        Assert.Equal(SubscriptionStatus.Trialing.ToString(), result.Value.SubscriptionStatus);

        var subscription = await db.Subscriptions.SingleAsync(s => s.DoctorId == doctor.Id);
        Assert.Equal(SubscriptionStatus.Trialing, subscription.Status);
        Assert.Empty(db.SubscriptionPayments);
    }

    [Fact]
    public async Task Handle_WhenCheckoutCreationFails_ReturnsFailure()
    {
        using var scope = CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var doctor = await SeedDoctorAsync(db);
        SetCurrentTenant(doctor.Id);
        var plan = await SeedPlanAsync(db, trialDays: 0);
        ChargilyClientMock
            .Setup(c => c.CreateCheckout(It.IsAny<Checkout>()))
            .ReturnsAsync((Response<CheckoutResponse>?)null);
        var handler = CreateCreateCheckoutHandler(scope.ServiceProvider);

        var result = await handler.Handle(
            new CreateCheckout.CreateSubscriptionCheckoutCommand(doctor.Id, plan.Id, "key-1"),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(SubscriptionErrors.FailedToRetrieveCheckout(Guid.Empty).Code, result.Error.Code);
    }

    [Fact]
    public async Task Handle_WhenCheckoutCreated_ReturnsCheckoutUrl()
    {
        using var scope = CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var doctor = await SeedDoctorAsync(db);
        SetCurrentTenant(doctor.Id);
        var plan = await SeedPlanAsync(db, trialDays: 0);
        var checkout = BuildCheckoutResponse("provider-123", new Uri("https://checkout.test/new"));
        ChargilyClientMock
            .Setup(c => c.CreateCheckout(It.IsAny<Checkout>()))
            .ReturnsAsync(checkout);
        var handler = CreateCreateCheckoutHandler(scope.ServiceProvider);

        var result = await handler.Handle(
            new CreateCheckout.CreateSubscriptionCheckoutCommand(doctor.Id, plan.Id, "key-1"),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(checkout.Value.CheckoutUrl.ToString(), result.Value.CheckoutUrl);
        Assert.Equal(SubscriptionStatus.Pending.ToString(), result.Value.SubscriptionStatus);

        var payment = await db.SubscriptionPayments.SingleAsync();
        Assert.Equal("provider-123", payment.ProviderPaymentId);
    }
}
