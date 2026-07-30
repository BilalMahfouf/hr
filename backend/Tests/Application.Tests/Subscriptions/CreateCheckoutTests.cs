using Application.Tests.Helpers;
using Chargily.Pay.Abstractions;
using Chargily.Pay.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Moq;
using Modules.Identity.Abstracions;
using Modules.Shared.Abstracions;
using PublicApi.Common.Abstracions;
using Modules.Shared.Domain.Common;
using PublicApi.Domain.Common;
using PublicApi.Domain.Subscriptions;
using Modules.Identity.Domain.Users;
using PublicApi.Features.Subscriptions.Endpoints;
using PublicApi.Infrastructure.Payments;

namespace Application.Tests.Subscriptions;

using ChargilyCheckoutResponse = Chargily.Pay.Models.Response<CheckoutResponse>;

public class CreateCheckoutTests
{
    private readonly Mock<IApplicationDbContext> _dbMock = new();
    private readonly Mock<IIdentityApplicationDbContext> _identityDbMock = new();
    private readonly Mock<IChargilyPayClient> _chargilyPayClientMock = new();
    private readonly IOptions<ChargilyOptions> _options = Options.Create(new ChargilyOptions
    {
        ApiSecretKey = "secret",
        IsLiveMode = false,
        WebhookUrl = "https://example.com/webhook",
        SuccessUrl = "https://example.com/success",
        FailureUrl = "https://example.com/failure"
    });

    private readonly List<User> _users = [];
    private readonly List<SubscriptionPlan> _plans = [];
    private readonly List<Subscription> _subscriptions = [];
    private readonly List<Payment> _payments = [];

    private Payment? _addedPayment;
    private Subscription? _addedSubscription;

    private CreateCheckout.Handler CreateHandler()
    {
        SetupDbSets();

        return new CreateCheckout.Handler(
            _dbMock.Object,
            _identityDbMock.Object,
            _chargilyPayClientMock.Object,
            _options);
    }

    private void SetupDbSets()
    {
        var usersSet = DbSetMockHelper.CreateMockDbSet(_users);
        _identityDbMock.Setup(db => db.Users).Returns(usersSet.Object);

        var plansSet = DbSetMockHelper.CreateMockDbSet(_plans);
        _dbMock.Setup(db => db.SubscriptionPlans).Returns(plansSet.Object);

        var subscriptionsSet = DbSetMockHelper.CreateMockDbSet(_subscriptions);
        subscriptionsSet
            .Setup(db => db.Add(It.IsAny<Subscription>()))
            .Callback<Subscription>(subscription =>
            {
                _addedSubscription = subscription;
                _subscriptions.Add(subscription);
            });
        _dbMock.Setup(db => db.Subscriptions).Returns(subscriptionsSet.Object);

        var paymentsSet = DbSetMockHelper.CreateMockDbSet(_payments);
        paymentsSet
            .Setup(db => db.Add(It.IsAny<Payment>()))
            .Callback<Payment>(payment =>
            {
                _addedPayment = payment;
                _payments.Add(payment);
            });
        _dbMock.Setup(db => db.SubscriptionPayments).Returns(paymentsSet.Object);

        _dbMock
            .Setup(db => db.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
    }

    private static SubscriptionPlan CreatePlan(decimal amount = 1000)
    {
        return SubscriptionPlan.Create(
            "Standard",
            "standard",
            new Money(amount, "DZD"),
            "month");
    }

    private static User CreateDoctor()
    {
        return User.Create("Doc", "Tor", "doc@example.com", "hash", UserRoles.Doctor);
    }

    [Fact]
    public async Task Handle_WhenPaymentAlreadyExistsAndCheckoutExists_ShouldReturnExistingCheckoutUrl()
    {
        // Arrange
        var idempotencyKey = "idem-1";
        var existingPayment = Payment.CreatePending(
            Guid.NewGuid(),
            Guid.NewGuid(),
            new Money(1200, "DZD"),
            "ChargilyPay",
            idempotencyKey);

        existingPayment.SetProviderPaymentId("provider-1");
        _payments.Add(existingPayment);

        _chargilyPayClientMock
            .Setup(c => c.GetCheckout("provider-1"))
            .ReturnsAsync(new ChargilyCheckoutResponse
            {
                Value = new CheckoutResponse
                {
                    CheckoutUrl = new Uri("https://checkout.example.com/existing")
                }
            });

        var handler = CreateHandler();
        var command = new CreateCheckout.CreateSubscriptionCheckoutCommand(
            Guid.NewGuid(),
            Guid.NewGuid(),
            idempotencyKey);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("https://checkout.example.com/existing", result.Value.CheckoutUrl);

        _chargilyPayClientMock.Verify(c => c.GetCheckout("provider-1"), Times.Once);
        _dbMock.Verify(db => db.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenPaymentAlreadyExistsAndCheckoutCannotBeRetrieved_ShouldReturnFailure()
    {
        // Arrange
        var idempotencyKey = "idem-2";
        var existingPayment = Payment.CreatePending(
            Guid.NewGuid(),
            Guid.NewGuid(),
            new Money(1200, "DZD"),
            "ChargilyPay",
            idempotencyKey);

        existingPayment.SetProviderPaymentId("provider-2");
        _payments.Add(existingPayment);

        _chargilyPayClientMock
            .Setup(c => c.GetCheckout("provider-2"))
            .ReturnsAsync((ChargilyCheckoutResponse?)null);

        var handler = CreateHandler();
        var command = new CreateCheckout.CreateSubscriptionCheckoutCommand(
            Guid.NewGuid(),
            Guid.NewGuid(),
            idempotencyKey);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("Subscription.FailedToRetrieveCheckout", result.Error.Code);
        Assert.Contains(existingPayment.Id.ToString(), result.Error.Description);

        _dbMock.Verify(db => db.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenDoctorDoesNotExist_ShouldReturnUserNotFound()
    {
        // Arrange
        var plan = CreatePlan();
        _plans.Add(plan);

        var handler = CreateHandler();
        var command = new CreateCheckout.CreateSubscriptionCheckoutCommand(
            Guid.NewGuid(),
            plan.Id,
            "idem-3");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("User.NotFound", result.Error.Code);

        _chargilyPayClientMock.Verify(c => c.CreateCheckout(It.IsAny<Checkout>()), Times.Never);
        _dbMock.Verify(db => db.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenPlanDoesNotExist_ShouldReturnSubscriptionPlanNotFound()
    {
        // Arrange
        var doctor = CreateDoctor();
        _users.Add(doctor);

        var missingPlanId = Guid.NewGuid();
        var handler = CreateHandler();
        var command = new CreateCheckout.CreateSubscriptionCheckoutCommand(
            doctor.Id,
            missingPlanId,
            "idem-4");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("SubscriptionPlan.NotFound", result.Error.Code);
        Assert.Contains(missingPlanId.ToString(), result.Error.Description);

        _chargilyPayClientMock.Verify(c => c.CreateCheckout(It.IsAny<Checkout>()), Times.Never);
        _dbMock.Verify(db => db.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenProviderCheckoutCreationFails_ShouldReturnFailure()
    {
        // Arrange
        var doctor = CreateDoctor();
        var plan = CreatePlan(2500);
        _users.Add(doctor);
        _plans.Add(plan);

        _chargilyPayClientMock
            .Setup(c => c.CreateCheckout(It.IsAny<Checkout>()))
            .ReturnsAsync((ChargilyCheckoutResponse?)null);

        var handler = CreateHandler();
        var command = new CreateCheckout.CreateSubscriptionCheckoutCommand(
            doctor.Id,
            plan.Id,
            "idem-5");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("Subscription.FailedToRetrieveCheckout", result.Error.Code);
        Assert.Contains(Guid.Empty.ToString(), result.Error.Description);

        Assert.NotNull(_addedSubscription);
        Assert.NotNull(_addedPayment);

        _dbMock.Verify(db => db.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenCheckoutIsCreated_ShouldCreateSubscriptionPaymentAndReturnCheckoutUrl()
    {
        // Arrange
        var doctor = CreateDoctor();
        var plan = CreatePlan(3000);
        _users.Add(doctor);
        _plans.Add(plan);

        _chargilyPayClientMock
            .Setup(c => c.CreateCheckout(It.IsAny<Checkout>()))
            .ReturnsAsync(new ChargilyCheckoutResponse
            {
                Id = "provider-3",
                Value = new CheckoutResponse
                {
                    Id = "provider-3",
                    CheckoutUrl = new Uri("https://checkout.example.com/new")
                }
            });

        var handler = CreateHandler();
        var command = new CreateCheckout.CreateSubscriptionCheckoutCommand(
            doctor.Id,
            plan.Id,
            "idem-6");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("https://checkout.example.com/new", result.Value.CheckoutUrl);

        Assert.NotNull(_addedSubscription);
        Assert.NotNull(_addedPayment);
        Assert.Equal("provider-3", _addedPayment.ProviderPaymentId);
        Assert.Equal(plan.Price, _addedPayment.Amount);
        Assert.Equal(doctor.Id, _addedPayment.DoctorId);

        _chargilyPayClientMock.Verify(c => c.CreateCheckout(It.IsAny<Checkout>()), Times.Once);
        _dbMock.Verify(db => db.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenCheckoutCreatedWithNullId_ShouldReturnFailure()
    {
        // Arrange
        var doctor = CreateDoctor();
        var plan = CreatePlan(3500);
        _users.Add(doctor);
        _plans.Add(plan);

        _chargilyPayClientMock
            .Setup(c => c.CreateCheckout(It.IsAny<Checkout>()))
            .ReturnsAsync(new ChargilyCheckoutResponse
            {
                Id = null,
                Value = new CheckoutResponse
                {
                    CheckoutUrl = new Uri("https://checkout.example.com/no-id")
                }
            });

        var handler = CreateHandler();
        var command = new CreateCheckout.CreateSubscriptionCheckoutCommand(
            doctor.Id,
            plan.Id,
            "idem-7");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("Subscription.FailedToRetrieveCheckout", result.Error.Code);

        _dbMock.Verify(db => db.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenExistingPaymentHasNoProviderPaymentId_ShouldCreateNewCheckout()
    {
        // Arrange
        var doctor = CreateDoctor();
        var plan = CreatePlan(1500);
        _users.Add(doctor);
        _plans.Add(plan);

        var existingPendingPayment = Payment.CreatePending(
            Guid.NewGuid(),
            doctor.Id,
            new Money(1500, "DZD"),
            "ChargilyPay",
            "idem-8");

        _payments.Add(existingPendingPayment);

        _chargilyPayClientMock
            .Setup(c => c.CreateCheckout(It.IsAny<Checkout>()))
            .ReturnsAsync(new ChargilyCheckoutResponse
            {
                Id = "provider-8",
                Value = new CheckoutResponse
                {
                    CheckoutUrl = new Uri("https://checkout.example.com/fresh")
                }
            });

        var handler = CreateHandler();
        var command = new CreateCheckout.CreateSubscriptionCheckoutCommand(
            doctor.Id,
            plan.Id,
            "idem-8");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("https://checkout.example.com/fresh", result.Value.CheckoutUrl);

        _chargilyPayClientMock.Verify(c => c.GetCheckout(It.IsAny<string>()), Times.Never);
        _chargilyPayClientMock.Verify(c => c.CreateCheckout(It.IsAny<Checkout>()), Times.Once);
        _dbMock.Verify(db => db.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
