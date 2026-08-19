namespace PublicApi.Features.Subscriptions;

public static class Shared
{
    public sealed record Response(
           string? CheckoutUrl,
           string? SubscriptionStatus,
           Guid SubscriptionId);

    public  const string IdempotencyKeyHeader = "Idempotency-Key";

}
