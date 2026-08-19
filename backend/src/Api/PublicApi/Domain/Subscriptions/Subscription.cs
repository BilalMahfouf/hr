using Modules.Shared.Domain.Common;
using PublicApi.Domain.Subscriptions.Errors;

namespace PublicApi.Domain.Subscriptions;

public sealed class Subscription : Entity
{
    public Guid DoctorId { get; private set; }
    public Guid PlanId { get; private set; }
    public Guid? PreviousSubscriptionId { get; private set; }
    public SubscriptionStatus Status { get; private set; }
    public DateTime CurrentPeriodStart { get; private set; }
    public DateTime CurrentPeriodEnd { get; private set; }
    public DateTime? TrialEndsAt { get; private set; }
    public DateTime? CancelledAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    public SubscriptionPlan Plan { get; private set; } = null!;
    public Subscription? PreviousSubscription { get; private set; }
    private readonly List<Payment> _payments = [];
    public IReadOnlyCollection<Payment> Payments => _payments.AsReadOnly();

    private Subscription() { }

    /// <summary>
    /// To use this method you need to include
    /// the <see cref="SubscriptionPlan"/> entity, which contains the billing interval and trial period information.
    /// </summary>
    /// <param name="doctorId"></param>
    /// <param name="plan"></param>
    /// <returns></returns>
    public static Subscription Create(Guid doctorId, SubscriptionPlan plan)
    {
        var now = DateTime.UtcNow;
        var hasTrial = plan.TrialDays > 0;

        return new Subscription
        {
            DoctorId = doctorId,
            PlanId = plan.Id,
            Plan = plan,
            Status = hasTrial ? SubscriptionStatus.Trialing : SubscriptionStatus.Pending,
            CurrentPeriodStart = now,
            CurrentPeriodEnd = AddInterval(now, plan.BillingInterval, plan.IntervalCount),
            TrialEndsAt = hasTrial ? now.AddDays(plan.TrialDays) : null,
        };
    }

    public void Activate()
    {
        Status = SubscriptionStatus.Active;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkPastDue()
    {
        Status = SubscriptionStatus.PastDue;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Cancel()
    {
        if (Status == SubscriptionStatus.Cancelled)
        {
            throw new DomainException(SubscriptionErrors.SubscriptionAlreadyCancelled);
        }

        Status = SubscriptionStatus.Cancelled;
        CancelledAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// </summary>
    /// <param name="previousSubscription"></param>
    /// <returns></returns>
    /// <returns></returns>
    public static Subscription Renew(
                 Subscription previousSubscription,
                 SubscriptionPlan plan)
    {
        if (previousSubscription.Status is SubscriptionStatus.Active)
        {
            throw new DomainException(
                SubscriptionErrors.ActiveSubscriptionAlreadyExist);
        }
        if (previousSubscription.Status is not (SubscriptionStatus.PastDue
                                 or SubscriptionStatus.Expired))
        {
            throw new DomainException(SubscriptionErrors
                .SubscriptionNotInRenewableState);
        }

        var start = DateTime.UtcNow;
        var endOfSubscription = AddInterval(
                start,
                plan.BillingInterval,
                plan.IntervalCount);

        return new Subscription
        {
            DoctorId = previousSubscription.DoctorId,
            PlanId = plan.Id,
            PreviousSubscriptionId = previousSubscription.Id,
            Status = SubscriptionStatus.Pending,
            CurrentPeriodStart = start,
            CurrentPeriodEnd = endOfSubscription,
            TrialEndsAt = null, // no trial on renewal
        };
    }


    public bool IsAccessGranted()
    {
        if (Status == SubscriptionStatus.Cancelled
            || Status == SubscriptionStatus.Expired)
        {
            return false;
        }
        return true;
    }

    public void PaymentFailed()
    {
        Status = SubscriptionStatus.PaymentFailed;
        UpdatedAt = DateTime.UtcNow;
    }
    public void PaymentExipred()
    {
        Status = SubscriptionStatus.PaymentExpired;
        UpdatedAt = DateTime.UtcNow;
    }
    public void MarkExpired()
    {
        Status = SubscriptionStatus.Expired;
        UpdatedAt = DateTime.UtcNow;
    }
    private static DateTime AddInterval(DateTime from, string interval, int count) => interval switch
    {
        "month" => from.AddMonths(count),
        "year" => from.AddYears(count),
        _ => throw new ArgumentException($"Unknown interval: {interval}")
    };
}
