namespace VeterinaryApi.Features.SubscriptionPlans;

public static class Shared
{
    public sealed record Response(
        Guid Id,
       string Name,
       string Slug,
       decimal Amount,
       string Currency,
       string BillingInterval,
       int IntervalCount,
       int TrialDays,
       bool IsActive,
       DateTime CreatedOnUtc);

  
}
