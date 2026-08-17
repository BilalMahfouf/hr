using Chargily.Pay.Abstractions;
using Chargily.Pay.Models;
using HandlebarsDotNet;
using Microsoft.Extensions.Options;
using PublicApi.Common.Abstracions.Payments;
using Modules.Shared.Results;
using PublicApi.Domain.Subscriptions.Errors;

namespace PublicApi.Infrastructure.Payments;

public sealed class ChargilyPaymentService(
    IChargilyPayClient chargilyPayClient,
    IOptions<ChargilyOptions> options)
    : IPaymentService
{
    private List<string> GenerateMetaData(Guid paymentId, List<string>? metaData)
    {
        if (metaData is not null)
        {

            var allMetaData = new List<string>()
            {
                $"paymentId:{paymentId.ToString()}",
            };
            allMetaData.AddRange(metaData);

            return allMetaData;
        }
        return new List<string>
        {
            $"paymentId:{paymentId.ToString()}"
        };
    }
    public async Task<Result<CreateCheckout>> CreateCheckoutAsync(
        decimal amount,
        PublicApi.Common.Abstracions.Payments.Currency currency,
        Guid paymentId,
        List<string>? metaData,
        CancellationToken cancellationToken = default)
    {
        var chargilyOptions = options.Value;

        var checkout = new Checkout(amount, (Chargily.Pay.Models.Currency)currency)
        {
            Language = LocaleType.Arabic,
            PaymentMethod = PaymentMethod.EDAHABIA,
            PassFeesToCustomer = false,
            WebhookEndpointUrl = new Uri(chargilyOptions.WebhookUrl),
            OnFailureRedirectUrl = new Uri(chargilyOptions.FailureUrl),
            OnSuccessRedirectUrl = new Uri(chargilyOptions.SuccessUrl),
            CollectShippingAddress = false,
            Metadata = GenerateMetaData(paymentId, metaData),
        };
        var checkoutResult = await chargilyPayClient.CreateCheckout(checkout);
        if (checkoutResult is null || checkoutResult?.Id is null)
        {
            return Result<CreateCheckout>.Failure(SubscriptionErrors
                .FailedToRetrieveCheckout(paymentId));
        }
        var checkoutUri = checkoutResult.Value.CheckoutUrl;
        if (checkoutUri is null)
        {
            return Result<CreateCheckout>.Failure(SubscriptionErrors
                .FailedToRetrieveCheckout(paymentId));
        }

        return Result<CreateCheckout>.Success(new CreateCheckout(
            checkoutUri,
            checkoutResult.Value.Id));
    }

    public async Task<Result<GetCheckoutResponse>> GetCheckoutAsync(
        string id,
        CancellationToken cancellation = default)
    {
        Response<CheckoutResponse>? existingCheckout = await chargilyPayClient
                   .GetCheckout(id);
        if (existingCheckout is null ||
            existingCheckout.Value.CheckoutUrl is null)
        {
            return Result<GetCheckoutResponse>.Failure(SubscriptionErrors
                .FailedToRetrieveCheckout());
        }

        return Result<GetCheckoutResponse>.Success(new GetCheckoutResponse(
            existingCheckout.Value.CheckoutUrl));
    }
}
