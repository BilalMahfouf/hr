export type SubscriptionStatus =
  | "Pending"
  | "Trialing"
  | "Active"
  | "PaymentFailed"
  | "PaymentExpired"
  | "PastDue"
  | "Cancelled"
  | "Expired"
  | "Inactive";

export type CurrentUser = {
  id: string;
  userName: string;
  fullName: string;
  email: string;
  role: string;
  isActive: boolean;
  subscriptionStatus: SubscriptionStatus | null;
  isSubscriptionExist: boolean ;
  createdOnUtc: string;
};

