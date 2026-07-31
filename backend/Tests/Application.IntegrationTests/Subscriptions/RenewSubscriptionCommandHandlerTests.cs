using Application.IntegrationTests.Infrastructure;
using Application.IntegrationTests.TestBases;
using Microsoft.Extensions.DependencyInjection;
using Modules.Identity.Abstracions;
using PublicApi.Common.Abstracions.Payments;
using Modules.Shared.Errors;
using Modules.Shared.Results;
using PaymentCreateCheckout = PublicApi.Common.Abstracions.Payments.CreateCheckout;
using PublicApi.Domain.Subscriptions;
using PublicApi.Domain.Subscriptions.Errors;
using PublicApi.Features.Subscriptions.Endpoints;
using PublicApi.Infrastructure.Persistence;

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
        var appDb = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var identityDb = scope.ServiceProvider.GetRequiredService<IIdentityApplicationDbContext>();
        var doctor = await SeedDoctorAsync(identityDb);
        SetCurrentTenant(doctor.Id);
        var plan = await SeedPlanAsync(appDb);
        await SeedSubscriptionAsync(appDb, doctor.Id, plan, SubscriptionStatus.Active);
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
        var appDb = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var identityDb = scope.ServiceProvider.GetRequiredService<IIdentityApplicationDbContext>();
        var doctor = await SeedDoctorAsync(identityDb);
        SetCurrentTenant(doctor.Id);
        var plan = await SeedPlanAsync(appDb);
        var subscription = await SeedSubscriptionAsync(appDb, doctor.Id, plan, SubscriptionStatus.PastDue);
        var payment = await SeedPaymentAsync(
            appDb,
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
        var appDb = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var identityDb = scope.ServiceProvider.GetRequiredService<IIdentityApplicationDbContext>();
        var doctor = await SeedDoctorAsync(identityDb);
        SetCurrentTenant(doctor.Id);
        var plan = await SeedPlanAsync(appDb);
        var subscription = await SeedSubscriptionAsync(appDb, doctor.Id, plan, SubscriptionStatus.PastDue);
        var payment = await SeedPaymentAsync(
            appDb,
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
        var appDb = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var identityDb = scope.ServiceProvider.GetRequiredService<IIdentityApplicationDbContext>();
        var doctor = await SeedDoctorAsync(identityDb);
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
        var appDb = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var identityDb = scope.ServiceProvider.GetRequiredService<IIdentityApplicationDbContext>();
        var doctor = await SeedDoctorAsync(identityDb);
        SetCurrentTenant(doctor.Id);
        var plan = await SeedPlanAsync(appDb);
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
        var appDb = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var identityDb = scope.ServiceProvider.GetRequiredService<IIdentityApplicationDbContext>();
        var doctor = await SeedDoctorAsync(identityDb);
        SetCurrentTenant(doctor.Id);
        var plan = await SeedPlanAsync(appDb);
        await SeedSubscriptionAsync(appDb, doctor.Id, plan, SubscriptionStatus.PastDue);
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
        var appDb = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var identityDb = scope.ServiceProvider.GetRequiredService<IIdentityApplicationDbContext>();
        var doctor = await SeedDoctorAsync(identityDb);
        SetCurrentTenant(doctor.Id);
        var plan = await SeedPlanAsync(appDb);
        var oldSubscription = await SeedSubscriptionAsync(appDb, doctor.Id, plan, SubscriptionStatus.PastDue);

        PaymentService.CreateCheckoutResult = Result<PaymentCreateCheckout>.Success(
            new PaymentCreateCheckout(new Uri("https://checkout.test/new"), "provider-123"));

        var handler = CreateRenewSubscriptionHandler(scope.ServiceProvider);
        var result = await handler.Handle(
            new RenewSubscription.Command(doctor.Id, plan.Id, "key-1"),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("https://checkout.test/new", result.Value.CheckoutUrl);

        var renewed = await appDb.Subscriptions
            .OrderByDescending(s => s.CreatedOnUtc)
            .FirstAsync(s => s.DoctorId == doctor.Id);
        Assert.Equal(oldSubscription.Id, renewed.PreviousSubscriptionId);

        var payment = await appDb.SubscriptionPayments.SingleAsync(p => p.SubscriptionId == renewed.Id);
        Assert.Equal("provider-123", payment.ProviderPaymentId);
    }
}
