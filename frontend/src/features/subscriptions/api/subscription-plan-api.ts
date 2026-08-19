import api from "@/lib/api/api";

export interface SubscriptionPlan {
  id: string;
  name: string;
  slug: string;
  amount: number;
  currency: string;
  billingInterval: string;
  intervalCount: number;
  trialDays: number;
  isActive: boolean;
  createdOnUtc: string;
}

export interface CreateSubscriptionPlanRequest {
  name: string;
  amount: number;
  currency: string;
  billingInterval: BillingInterval;
  intervalCount: number;
  trialDays: number;
}

export type UpdateSubscriptionPlanRequest = CreateSubscriptionPlanRequest;
export type BillingInterval = "month" | "year";

interface UpdateSubscriptionPlanResponse {
  id: string;
}

export const billingIntervals: readonly BillingInterval[] = ["month", "year"];

const subscriptionPlanApi = {
  getAllSubscriptionPlans: async (): Promise<SubscriptionPlan[]> => {
    const response = await api.get<SubscriptionPlan[]>("/subscription-plans");

    if (response.status !== 200) {
      throw new Error("Failed to fetch subscription plans");
    }

    return response.data;
  },

  getSubscriptionPlanById: async (planId: string): Promise<SubscriptionPlan> => {
    const response = await api.get<SubscriptionPlan>(`/subscription-plans/${planId}`);

    if (response.status !== 200) {
      throw new Error("Failed to fetch subscription plan details");
    }

    return response.data;
  },

  createSubscriptionPlan: async (request: CreateSubscriptionPlanRequest): Promise<string> => {
    const response = await api.post<string>("/subscription-plans", request);

    if (response.status !== 201) {
      throw new Error("Failed to create subscription plan");
    }

    return response.data;
  },

  updateSubscriptionPlan: async (
    planId: string,
    request: UpdateSubscriptionPlanRequest
  ): Promise<string> => {
    const response = await api.put<UpdateSubscriptionPlanResponse>(
      `/subscription-plans/${planId}`,
      request
    );

    if (response.status !== 200) {
      throw new Error("Failed to update subscription plan");
    }

    return response.data.id;
  },

  activateSubscriptionPlan: async (planId: string): Promise<void> => {
    const response = await api.patch(`/subscription-plans/${planId}/activate`);

    if (response.status !== 204) {
      throw new Error("Failed to activate subscription plan");
    }
  },

  deactivateSubscriptionPlan: async (planId: string): Promise<void> => {
    const response = await api.patch(`/subscription-plans/${planId}/deactivate`);

    if (response.status !== 204) {
      throw new Error("Failed to deactivate subscription plan");
    }
  },
};

export default subscriptionPlanApi;
