using Modules.Shared.Errors;

namespace PublicApi.Domain.Subscriptions.Errors;

public static class SubscriptionPlanErrors
{
    public static Error SubscriptionPlanNotFound(Guid planId)
        => Error.NotFound(
            code: $"{nameof(SubscriptionPlan)}.NotFound",
            description: $"Subscription plan with id {planId} was not found."
        );
    public static Error SubscriptionPlanNotFound()
      => Error.NotFound(
          code: $"{nameof(SubscriptionPlan)}.NotFound",
          description: $"Subscription plan  was not found."
      );
    public static Error SubscriptionPlansNotFound
            => Error.NotFound(
                code: $"{nameof(SubscriptionPlan)}.NotFound",
                description: $"Subscription plans  are not found."
            );
    public static Error SubscriptionPlanNameNotUnique(string name) =>
            Error.Conflict(
                code: $"{nameof(SubscriptionPlan)}.AlreadyExists",
                description: $"Subscription plan with name {name} already exists."
            );
    public static Error SubscriptionPlanSlugNotUnique(string slug) =>
            Error.Conflict(
                code: $"{nameof(SubscriptionPlan)}.AlreadyExists",
                description: $"Subscription plan with slug {slug} already exists."
            );
    public static Error PlanAlreadyNotActive =>
            Error.Conflict(
                code: $"{nameof(SubscriptionPlan)}.AlreadyNotActive",
                description: $"Subscription plan is already not active."
            );
    public static Error PlanAlreadyActive =>
            Error.Conflict(
                code: $"{nameof(SubscriptionPlan)}.AlreadyActive",
                description: $"Subscription plan  is already active."
            );


}
