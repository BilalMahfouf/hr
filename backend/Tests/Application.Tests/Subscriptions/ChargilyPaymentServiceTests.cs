using Chargily.Pay.Abstractions;
using Chargily.Pay.Models;
using Microsoft.Extensions.Options;
using Moq;
using PublicApi.Common.Abstracions.Payments;
using PublicApi.Infrastructure.Payments;

namespace Application.Tests.Subscriptions;

using ChargilyCheckoutResponse = Chargily.Pay.Models.Response<CheckoutResponse>;
using PaymentCurrency = PublicApi.Common.Abstracions.Payments.Currency;

public class ChargilyPaymentServiceTests
{
    private readonly Mock<IChargilyPayClient> _chargilyPayClientMock = new();

    private readonly IOptions<ChargilyOptions> _options = Options.Create(new ChargilyOptions
    {
        ApiSecretKey = "secret",
        IsLiveMode = false,
        WebhookUrl = "https://example.com/webhook",
        SuccessUrl = "https://example.com/success",
        FailureUrl = "https://example.com/failure"
    });

    private ChargilyPaymentService CreateService()
    {
        return new ChargilyPaymentService(_chargilyPayClientMock.Object, _options);
    }

    [Fact]
    public async Task CreateCheckoutAsync_WhenProviderReturnsValidCheckout_ShouldReturnSuccess()
    {
        // Arrange
        var paymentId = Guid.NewGuid();
        var metaData = new List<string> { "source:web" };
        Checkout? capturedCheckout = null;

        _chargilyPayClientMock
            .Setup(c => c.CreateCheckout(It.IsAny<Checkout>()))
            .Callback<Checkout>(checkout => capturedCheckout = checkout)
            .ReturnsAsync(new ChargilyCheckoutResponse
            {
                Id = "provider-1",
                Value = new CheckoutResponse
                {
                    Id = "provider-1",
                    CheckoutUrl = new Uri("https://checkout.example.com/ok")
                }
            });

        var service = CreateService();

        // Act
        var result = await service.CreateCheckoutAsync(
            1500,
            PaymentCurrency.DZD,
            paymentId,
            metaData,
            CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("provider-1", result.Value.ProviderPaymentId);
        Assert.Equal("https://checkout.example.com/ok", result.Value.CheckoutUrl.ToString());

        Assert.NotNull(capturedCheckout);
        Assert.Equal(LocaleType.Arabic, capturedCheckout!.Language);
        Assert.Equal(PaymentMethod.EDAHABIA, capturedCheckout.PaymentMethod);
        Assert.False(capturedCheckout.PassFeesToCustomer);
        Assert.False(capturedCheckout.CollectShippingAddress);
        Assert.Equal(new Uri("https://example.com/webhook"), capturedCheckout.WebhookEndpointUrl);
        Assert.Equal(new Uri("https://example.com/success"), capturedCheckout.OnSuccessRedirectUrl);
        Assert.Equal(new Uri("https://example.com/failure"), capturedCheckout.OnFailureRedirectUrl);
        Assert.Contains("source:web", capturedCheckout.Metadata!);
        Assert.Contains($"paymentId:{paymentId}", capturedCheckout.Metadata!);

        _chargilyPayClientMock.Verify(c => c.CreateCheckout(It.IsAny<Checkout>()), Times.Once);
    }

    [Fact]
    public async Task CreateCheckoutAsync_WhenMetaDataIsNull_ShouldIncludeOnlyPaymentIdMetadata()
    {
        // Arrange
        var paymentId = Guid.NewGuid();
        Checkout? capturedCheckout = null;

        _chargilyPayClientMock
            .Setup(c => c.CreateCheckout(It.IsAny<Checkout>()))
            .Callback<Checkout>(checkout => capturedCheckout = checkout)
            .ReturnsAsync(new ChargilyCheckoutResponse
            {
                Id = "provider-2",
                Value = new CheckoutResponse
                {
                    Id = "provider-2",
                    CheckoutUrl = new Uri("https://checkout.example.com/created")
                }
            });

        var service = CreateService();

        // Act
        var result = await service.CreateCheckoutAsync(
            2000,
            PaymentCurrency.DZD,
            paymentId,
            null,
            CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(capturedCheckout);
        Assert.Single(capturedCheckout!.Metadata!);
        Assert.Equal($"paymentId:{paymentId}", capturedCheckout.Metadata![0]);
    }

    [Fact]
    public async Task CreateCheckoutAsync_WhenProviderResponseIsNull_ShouldReturnFailure()
    {
        // Arrange
        var paymentId = Guid.NewGuid();

        _chargilyPayClientMock
            .Setup(c => c.CreateCheckout(It.IsAny<Checkout>()))
            .ReturnsAsync((ChargilyCheckoutResponse?)null);

        var service = CreateService();

        // Act
        var result = await service.CreateCheckoutAsync(
            1000,
            PaymentCurrency.DZD,
            paymentId,
            new List<string>(),
            CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("Subscription.FailedToRetrieveCheckout", result.Error.Code);
        Assert.Contains(paymentId.ToString(), result.Error.Description);
    }

    [Fact]
    public async Task CreateCheckoutAsync_WhenProviderResponseIdIsNull_ShouldReturnFailure()
    {
        // Arrange
        var paymentId = Guid.NewGuid();

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

        var service = CreateService();

        // Act
        var result = await service.CreateCheckoutAsync(
            1000,
            PaymentCurrency.DZD,
            paymentId,
            new List<string>(),
            CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("Subscription.FailedToRetrieveCheckout", result.Error.Code);
        Assert.Contains(paymentId.ToString(), result.Error.Description);
    }

    [Fact]
    public async Task CreateCheckoutAsync_WhenCheckoutUrlIsNull_ShouldReturnFailure()
    {
        // Arrange
        var paymentId = Guid.NewGuid();

        _chargilyPayClientMock
            .Setup(c => c.CreateCheckout(It.IsAny<Checkout>()))
            .ReturnsAsync(new ChargilyCheckoutResponse
            {
                Id = "provider-3",
                Value = new CheckoutResponse
                {
                    Id = "provider-3",
                    CheckoutUrl = null
                }
            });

        var service = CreateService();

        // Act
        var result = await service.CreateCheckoutAsync(
            1000,
            PaymentCurrency.DZD,
            paymentId,
            new List<string>(),
            CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("Subscription.FailedToRetrieveCheckout", result.Error.Code);
        Assert.Contains(paymentId.ToString(), result.Error.Description);
    }

    [Fact]
    public async Task GetCheckoutAsync_WhenProviderReturnsCheckoutUrl_ShouldReturnSuccess()
    {
        // Arrange
        _chargilyPayClientMock
            .Setup(c => c.GetCheckout("provider-10"))
            .ReturnsAsync(new ChargilyCheckoutResponse
            {
                Value = new CheckoutResponse
                {
                    CheckoutUrl = new Uri("https://checkout.example.com/existing")
                }
            });

        var service = CreateService();

        // Act
        var result = await service.GetCheckoutAsync("provider-10", CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(
            "https://checkout.example.com/existing",
            result.Value.CheckoutUrl.ToString());
    }

    [Fact]
    public async Task GetCheckoutAsync_WhenProviderResponseIsNull_ShouldReturnFailure()
    {
        // Arrange
        _chargilyPayClientMock
            .Setup(c => c.GetCheckout("provider-11"))
            .ReturnsAsync((ChargilyCheckoutResponse?)null);

        var service = CreateService();

        // Act
        var result = await service.GetCheckoutAsync("provider-11", CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("Subscription.FailedToRetrieveCheckout", result.Error.Code);
    }

    [Fact]
    public async Task GetCheckoutAsync_WhenCheckoutUrlIsNull_ShouldReturnFailure()
    {
        // Arrange
        _chargilyPayClientMock
            .Setup(c => c.GetCheckout("provider-12"))
            .ReturnsAsync(new ChargilyCheckoutResponse
            {
                Value = new CheckoutResponse
                {
                    CheckoutUrl = null
                }
            });

        var service = CreateService();

        // Act
        var result = await service.GetCheckoutAsync("provider-12", CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("Subscription.FailedToRetrieveCheckout", result.Error.Code);
    }
}