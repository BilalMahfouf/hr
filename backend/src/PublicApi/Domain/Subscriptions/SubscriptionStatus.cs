namespace PublicApi.Domain.Subscriptions;

public enum SubscriptionStatus
{
    Pending = 1,
    Trialing,
    Active,
    PaymentFailed,
    PaymentExpired,
    PastDue,
    Cancelled,
    Expired,
}
