import api from '@/lib/api/api';
import subscriptionPlanApi, { type SubscriptionPlan } from './subscription-plan-api';

export interface CheckoutResponse {
  checkoutUrl: string | null;
  subscriptionStatus: string | null;
  subscriptionId: string;
}

export interface MySubscriptionResponse {
  id: string;
  doctorId: string;
  planId: string;
  planName: string;
  planDisplayName: string;
  planPrice: number;
  planCurrency: string;
  subscriptionStatus: string;
  currentPeriodStart: string;
  currentPeriodEnd: string;
  trialEndsAt: string | null;
  cancelledAt: string | null;
  updatedAt: string | null;
  previousSubscriptionId: string | null;
}

interface CreateCheckoutRequest {
  planId: string;
}

const generateIdempotencyKey = (): string => {
  return `${Date.now()}-${Math.random().toString(36).substring(2, 11)}`;
};

const subscriptionApi = {
  getSubscriptionPlans: async (): Promise<SubscriptionPlan[]> => {
    return subscriptionPlanApi.getAllSubscriptionPlans();
  },

  getMySubscription: async (): Promise<MySubscriptionResponse> => {
    const response = await api.get<MySubscriptionResponse>('/subscriptions/me');
    if (response.status !== 200) {
      throw new Error('Failed to fetch subscription details');
    }
    return response.data;
  },

  createCheckout: async (planId: string): Promise<CheckoutResponse> => {
    const payload: CreateCheckoutRequest = { planId };

    const response = await api.post<CheckoutResponse>('/subscriptions', payload, {
      headers: {
        'Idempotency-Key': generateIdempotencyKey(),
      },
    });
    
    if (response.status !== 200 && response.status !== 201) {
      throw new Error('Failed to create checkout');
    }
    console.log('Checkout response:', response.data);
    return response.data;
  },

  renewSubscription: async (planId: string): Promise<CheckoutResponse> => {
    const response = await api.post<CheckoutResponse>('/subscriptions/renew', planId, {
      headers: {
        'Idempotency-Key': generateIdempotencyKey(),
      },
    });

    if (response.status !== 200 && response.status !== 201) {
      throw new Error('Failed to renew subscription');
    }

    return response.data;
  },
};

export default subscriptionApi;
