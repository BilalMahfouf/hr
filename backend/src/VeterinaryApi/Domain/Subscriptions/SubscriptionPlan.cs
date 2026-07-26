using VeterinaryApi.Domain.Common;
using VeterinaryApi.Domain.Subscriptions.Errors;

namespace VeterinaryApi.Domain.Subscriptions;

public sealed class SubscriptionPlan : Entity
{
    public string Name { get; private set; } = null!;
    public string Slug { get; private set; } = null!;
    public Money Price { get; private set; } = null!;
    public string BillingInterval { get; private set; } = null!;  // "month" | "year"
    public int IntervalCount { get; private set; }
    public int TrialDays { get; private set; }
    public bool IsActive { get; private set; }

    private SubscriptionPlan() { } // EF Core

    public static SubscriptionPlan Create(
        string name,
        string slug,
        Money price,
        string billingInterval,
        int intervalCount = 1,
        int trialDays = 0)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Name is required.");
        if (string.IsNullOrWhiteSpace(slug)) throw new ArgumentException("Slug is required.");
        if (billingInterval is not "month" and not "year")
            throw new ArgumentException("Billing interval must be 'month' or 'year'.");

        return new SubscriptionPlan
        {
            Name = name,
            Slug = slug.ToLowerInvariant(),
            Price = price,
            BillingInterval = billingInterval,
            IntervalCount = intervalCount,
            TrialDays = trialDays,
            IsActive = true,
        };
    }

    public void Update(
        string Name,
        Money Price,
        string BillingInterval,
        int IntervalCount,
        int TrialDays)
    {
        if (string.IsNullOrWhiteSpace(Name)) throw new ArgumentException("Name is required.");
        if (BillingInterval is not "month" and not "year")
            throw new ArgumentException("Billing interval must be 'month' or 'year'.");
        this.Name = Name;
        this.Price = Price;
        this.BillingInterval = BillingInterval;
        this.IntervalCount = IntervalCount;
        this.TrialDays = TrialDays;
    }

    public void Deactivate()
    {
        if (!IsActive)
        {
            throw new DomainException(SubscriptionPlanErrors.PlanAlreadyNotActive);
        }
        IsActive = false;
    }
    public void Activate()
    {
        if (IsActive)
        {
            throw new DomainException(SubscriptionPlanErrors.PlanAlreadyActive);
        }
        IsActive = true;
    }
}
