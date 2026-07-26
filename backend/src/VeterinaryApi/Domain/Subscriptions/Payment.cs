using Microsoft.EntityFrameworkCore.Query.Internal;
using VeterinaryApi.Domain.Common;

namespace VeterinaryApi.Domain.Subscriptions;


public sealed class Payment : Entity
{
    public Guid SubscriptionId { get; private set; }
    public Guid DoctorId { get; private set; }
    public Money Amount { get; private set; } = null!;
    public PaymentStatus Status { get; private set; }
    public string Provider { get; private set; } = null!;
    public string? ProviderPaymentId { get; private set; }
    public string IdempotencyKey { get; private set; } = null!;
    public string? ProviderMetadata { get; private set; }
    public string? FailureReason { get; private set; }
    public DateTime? PaidAt { get; private set; }

    public Subscription Subscription { get; private set; } = null!;

    private Payment() { }

    public static Payment CreatePending(
        Guid subscriptionId,
        Guid doctorId,
        Money amount,
        string provider,
        string idempotencyKey)
    {
        if (string.IsNullOrWhiteSpace(idempotencyKey))
            throw new ArgumentException("Idempotency key is required.");

        return new Payment
        {
            SubscriptionId = subscriptionId,
            DoctorId = doctorId,
            Amount = new Money(amount.Amount, amount.Currency),
            Status = PaymentStatus.Pending,
            Provider = provider,
            IdempotencyKey = idempotencyKey,
        };
    }

    public void MarkPaid(string? metadata = null)
    {
        Status = PaymentStatus.Paid;
        ProviderMetadata = metadata;
        PaidAt = DateTime.UtcNow;
    }

    public void MarkFailed(string reason, string? metadata = null)
    {
        Status = PaymentStatus.Failed;
        FailureReason = reason;
        ProviderMetadata = metadata;
    }
    public void MarkExpired(string reason, string? metaData = null)
    {
        Status = PaymentStatus.Expired;
        FailureReason = reason;
        ProviderMetadata = metaData;
    }


    public void MarkRefunded() => Status = PaymentStatus.Refunded;

    public void SetProviderPaymentId(string? providerPaymentId)
    {
        ProviderPaymentId = providerPaymentId;
    }
}
