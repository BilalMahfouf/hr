using Application.IntegrationTests.Infrastructure;
using Application.IntegrationTests.TestBases;
using Microsoft.Extensions.DependencyInjection;
using VeterinaryApi.Common.Abstracions.Payments;
using Shared.Errors;
using Shared.Results;
using PaymentCreateCheckout = VeterinaryApi.Common.Abstracions.Payments.CreateCheckout;
using VeterinaryApi.Domain.Subscriptions;
using VeterinaryApi.Domain.Subscriptions.Errors;
using VeterinaryApi.Features.Subscriptions.Endpoints;
using VeterinaryApi.Infrastructure.Persistence;

namespace Application.IntegrationTests.Subscriptions;

public sealed class RenewSubscriptionCommandHandlerTests : SubscriptionsTestBase
{
    public RenewSubscriptionCommandHandlerTests(PostgresFixture fixture) : base(fixture)
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
        var handler = CreateRenewSubscriptionHandler(scope.ServiceProvider);

        var result = await handler.Handle(
            new RenewSubscription.Command(doctor.Id, plan.Id, "key-1"),
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
        var subscription = await SeedSubscriptionAsync(db, doctor.Id, plan, SubscriptionStatus.PastDue);
        var payment = await SeedPaymentAsync(
            db,
            subscription,
            doctor.Id,
            "idempotency-1",
            providerPaymentId: "provider-123",
            status: PaymentStatus.Pending);

        PaymentService.GetCheckoutResult = Result<GetCheckoutResponse>.Success(
            new GetCheckoutResponse(new Uri("https://checkout.test/existing")));

        var handler = CreateRenewSubscriptionHandler(scope.ServiceProvider);
        var result = await handler.Handle(
            new RenewSubscription.Command(doctor.Id, plan.Id, "idempotency-1"),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("https://checkout.test/existing", result.Value.CheckoutUrl);
        Assert.Equal(payment.SubscriptionId, result.Value.SubscriptionId);
        Assert.Null(result.Value.SubscriptionStatus);
    }

    [Fact]
    public async Task Handle_WhenExistingPaymentCheckoutFails_ReturnsFailure()
    {
        using var scope = CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var doctor = await SeedDoctorAsync(db);
        SetCurrentTenant(doctor.Id);
        var plan = await SeedPlanAsync(db);
        var subscription = await SeedSubscriptionAsync(db, doctor.Id, plan, SubscriptionStatus.PastDue);
        var payment = await SeedPaymentAsync(
            db,
            subscription,
            doctor.Id,
            "idempotency-1",
            providerPaymentId: "provider-123",
            status: PaymentStatus.Pending);

        PaymentService.GetCheckoutResult = Result<GetCheckoutResponse>.Failure(
            Error.Failure("Test.Fail", "failed"));

        var handler = CreateRenewSubscriptionHandler(scope.ServiceProvider);
        var result = await handler.Handle(
            new RenewSubscription.Command(doctor.Id, plan.Id, "idempotency-1"),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(SubscriptionErrors.FailedToRetrieveCheckout(payment.Id).Code, result.Error.Code);
    }

    [Fact]
    public async Task Handle_WhenPlanMissing_ReturnsFailure()
    {
        using var scope = CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var doctor = await SeedDoctorAsync(db);
        SetCurrentTenant(doctor.Id);
        var handler = CreateRenewSubscriptionHandler(scope.ServiceProvider);
        var planId = Guid.NewGuid();

        var result = await handler.Handle(
            new RenewSubscription.Command(doctor.Id, planId, "key-1"),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(SubscriptionPlanErrors.SubscriptionPlanNotFound(planId).Code, result.Error.Code);
    }

    [Fact]
    public async Task Handle_WhenOldSubscriptionMissing_ReturnsFailure()
    {
        using var scope = CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var doctor = await SeedDoctorAsync(db);
        SetCurrentTenant(doctor.Id);
        var plan = await SeedPlanAsync(db);
        var handler = CreateRenewSubscriptionHandler(scope.ServiceProvider);

        var result = await handler.Handle(
            new RenewSubscription.Command(doctor.Id, plan.Id, "key-1"),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(SubscriptionErrors.NotFound.Code, result.Error.Code);
    }

    [Fact]
    public async Task Handle_WhenCheckoutCreationFails_ReturnsFailure()
    {
        using var scope = CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var doctor = await SeedDoctorAsync(db);
        SetCurrentTenant(doctor.Id);
        var plan = await SeedPlanAsync(db);
        await SeedSubscriptionAsync(db, doctor.Id, plan, SubscriptionStatus.PastDue);
        var handler = CreateRenewSubscriptionHandler(scope.ServiceProvider);

        var result = await handler.Handle(
            new RenewSubscription.Command(doctor.Id, plan.Id, "key-1"),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(SubscriptionErrors.FailedToCreateCheckout.Code, result.Error.Code);
    }

    [Fact]
    public async Task Handle_WhenCheckoutCreated_ReturnsCheckoutUrl()
    {
        using var scope = CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var doctor = await SeedDoctorAsync(db);
        SetCurrentTenant(doctor.Id);
        var plan = await SeedPlanAsync(db);
        var oldSubscription = await SeedSubscriptionAsync(db, doctor.Id, plan, SubscriptionStatus.PastDue);

        PaymentService.CreateCheckoutResult = Result<PaymentCreateCheckout>.Success(
            new PaymentCreateCheckout(new Uri("https://checkout.test/new"), "provider-123"));

        var handler = CreateRenewSubscriptionHandler(scope.ServiceProvider);
        var result = await handler.Handle(
            new RenewSubscription.Command(doctor.Id, plan.Id, "key-1"),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("https://checkout.test/new", result.Value.CheckoutUrl);

        var renewed = await db.Subscriptions
            .OrderByDescending(s => s.CreatedOnUtc)
            .FirstAsync(s => s.DoctorId == doctor.Id);
        Assert.Equal(oldSubscription.Id, renewed.PreviousSubscriptionId);

        var payment = await db.SubscriptionPayments.SingleAsync(p => p.SubscriptionId == renewed.Id);
        Assert.Equal("provider-123", payment.ProviderPaymentId);
    }
}
