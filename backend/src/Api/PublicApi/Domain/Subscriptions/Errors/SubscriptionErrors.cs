using Modules.Shared.Errors;

namespace PublicApi.Domain.Subscriptions.Errors;

public static class SubscriptionErrors
{
    public static Error NotFound
        => Error.NotFound(
            $"{nameof(Subscription)}.{nameof(NotFound)}",
            "The subscription was not found.");

    public static Error SubscriptionAlreadyCancelled
        => Error.Conflict(
            $"{nameof(Subscription)}.{nameof(SubscriptionAlreadyCancelled)}",
            "The subscription is already cancelled.");
    public static Error SubscriptionNotActive
       => Error.Conflict(
           $"{nameof(Subscription)}.{nameof(SubscriptionNotActive)}",
           "The subscription is not active");
    public static Error SubscriptionNotInRenewableState
        => Error.Conflict(
            $"{nameof(Subscription)}.{nameof(SubscriptionNotInRenewableState)}",
            "Only an active, past due, or expired subscription can be renewed.");
    public static Error AlreadyExistAcitveSubscription
        => Error.Conflict($"{nameof(Subscription)}." +
            $"{nameof(AlreadyExistAcitveSubscription)}",
            "This User Already have a an active subscription");
    public static Error FailedToRetrieveCheckout(Guid paymentId)
        => Error.Failure(
            $"{nameof(Subscription)}.{nameof(FailedToRetrieveCheckout)}",
            $"Failed to retrieve checkout for payment with ID: {paymentId}");
    public static Error FailedToRetrieveCheckout()
        => Error.Failure(
            $"{nameof(Subscription)}.{nameof(FailedToRetrieveCheckout)}",
            $"Failed to retrieve checkout ");

    public static Error FailedToProcessWebhook(Guid paymentId)
        => Error.Failure(
            $"{nameof(Subscription)}.{nameof(FailedToProcessWebhook)}",
            $"Failed to process webhook for payment with ID: {paymentId}");

    public static Error ActiveSubscriptionAlreadyExist =>
        Error.Conflict(
            $"{nameof(Subscription)}.{nameof(ActiveSubscriptionAlreadyExist)}",
            "An active subscription already exists for this doctor.");
    public static Error FailedToCreateCheckout =>
        Error.Failure(
            $"{nameof(Subscription)}.{nameof(FailedToCreateCheckout)}",
            "Failed to create checkout for the subscription.");

}
