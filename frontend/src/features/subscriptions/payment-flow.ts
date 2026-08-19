export const PAYMENT_FLOW = {
  SUBSCRIBE: 'subscribe',
  RENEW: 'renew',
} as const;

type PaymentFlow = (typeof PAYMENT_FLOW)[keyof typeof PAYMENT_FLOW];

const PAYMENT_FLOW_KEY = 'subscription-payment-flow';

export const setPaymentFlow = (flow: PaymentFlow): void => {
  sessionStorage.setItem(PAYMENT_FLOW_KEY, flow);
};

export const getPaymentFlow = (): PaymentFlow | null => {
  const value = sessionStorage.getItem(PAYMENT_FLOW_KEY);

  if (value === PAYMENT_FLOW.SUBSCRIBE || value === PAYMENT_FLOW.RENEW) {
    return value;
  }

  return null;
};

export const clearPaymentFlow = (): void => {
  sessionStorage.removeItem(PAYMENT_FLOW_KEY);
};
