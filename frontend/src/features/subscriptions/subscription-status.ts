export const SUBSCRIPTION_STATUS = {
  PENDING: "Pending",
  TRIALING: "Trialing",
  ACTIVE: "Active",
  PAYMENT_FAILED: "PaymentFailed",
  PAYMENT_EXPIRED: "PaymentExpired",
  PAST_DUE: "PastDue",
  CANCELLED: "Cancelled",
  EXPIRED: "Expired",
} as const;

export type KnownSubscriptionStatus =
  (typeof SUBSCRIPTION_STATUS)[keyof typeof SUBSCRIPTION_STATUS];

export const isActiveOrTrialingStatus = (
  subscriptionStatus: string | null | undefined
): boolean => {
  return (
    subscriptionStatus === SUBSCRIPTION_STATUS.ACTIVE ||
    subscriptionStatus === SUBSCRIPTION_STATUS.TRIALING
  );
};

export const isRenewOnlyStatus = (
  subscriptionStatus: string | null | undefined
): boolean => {
  return (
    subscriptionStatus === SUBSCRIPTION_STATUS.EXPIRED ||
    subscriptionStatus === SUBSCRIPTION_STATUS.PAST_DUE
  );
};

export const isSubscribeOnlyStatus = (
  subscriptionStatus: string | null | undefined
): boolean => {
  return (
    subscriptionStatus === SUBSCRIPTION_STATUS.PENDING ||
    subscriptionStatus === SUBSCRIPTION_STATUS.PAYMENT_FAILED ||
    subscriptionStatus === SUBSCRIPTION_STATUS.PAYMENT_EXPIRED ||
    subscriptionStatus === SUBSCRIPTION_STATUS.CANCELLED
  );
};

export const isPastDueStatus = (
  subscriptionStatus: string | null | undefined
): boolean => {
  return subscriptionStatus === SUBSCRIPTION_STATUS.PAST_DUE;
};

export const isPaymentFailedStatus = (
  subscriptionStatus: string | null | undefined
): boolean => {
  return subscriptionStatus === SUBSCRIPTION_STATUS.PAYMENT_FAILED;
};

export const isExpiredStatus = (
  subscriptionStatus: string | null | undefined
): boolean => {
  return subscriptionStatus === SUBSCRIPTION_STATUS.EXPIRED;
};