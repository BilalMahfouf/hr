using VeterinaryApi.Common.Abstracions.Payments;
using VeterinaryApi.Common.Errors;
using VeterinaryApi.Common.Results;

namespace Application.IntegrationTests.Infrastructure;

public sealed class TestPaymentService : IPaymentService
{
    public Result<CreateCheckout>? CreateCheckoutResult { get; set; }
    public Result<GetCheckoutResponse>? GetCheckoutResult { get; set; }
    public List<(decimal Amount, Currency Currency, Guid PaymentId, List<string>? MetaData)> CreateCheckoutCalls { get; } = new();
    public List<string> GetCheckoutCalls { get; } = new();

    public Task<Result<CreateCheckout>> CreateCheckoutAsync(
        decimal amount,
        Currency currency,
        Guid paymentId,
        List<string>? metaData,
        CancellationToken cancellationToken = default)
    {
        CreateCheckoutCalls.Add((amount, currency, paymentId, metaData));
        return Task.FromResult(CreateCheckoutResult ?? Result<CreateCheckout>.Failure(
            Error.Failure("Test.Payment.NotConfigured", "CreateCheckoutAsync result not configured.")));
    }

    public Task<Result<GetCheckoutResponse>> GetCheckoutAsync(
        string id,
        CancellationToken cancellation = default)
    {
        GetCheckoutCalls.Add(id);
        return Task.FromResult(GetCheckoutResult ?? Result<GetCheckoutResponse>.Failure(
            Error.Failure("Test.Payment.NotConfigured", "GetCheckoutAsync result not configured.")));
    }
}
