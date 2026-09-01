import { Link, useNavigate } from 'react-router-dom';
import { useState } from 'react';
import { useMutation, useQuery } from '@tanstack/react-query';
import { useTranslation } from 'react-i18next';
import { Check, Loader2 } from 'lucide-react';

import { Button } from '@/components/ui/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import LanguageSwitcher from '@/components/ui/language-switcher';
import { useToast } from '@/hooks/use-toast';
import i18nKeyContainer from '@/lib/i18n/keyContainer';
import { parseApiError, ErrorCodes } from '@/lib/api/error-types';
import subscriptionApi from '@/features/subscriptions/api/subscription-api';
import subscriptionPlanApi from '@/features/subscriptions/api/subscription-plan-api';
import { PAYMENT_FLOW, setPaymentFlow } from '@/features/subscriptions/payment-flow';

export default function SubscribePage() {
  const { t, i18n } = useTranslation();
  const { handleApiError, info } = useToast();
  const navigate = useNavigate();
  const isRtl = i18n.language === 'ar';

  const [selectedPlanId, setSelectedPlanId] = useState<string | null>(null);

  const plansQuery = useQuery({
    queryKey: ['subscription-plans'],
    queryFn: subscriptionPlanApi.getAllSubscriptionPlans,
  });

  const checkoutMutation = useMutation({
    mutationFn: (planId: string) => subscriptionApi.createCheckout(planId),
    onSuccess: (data) => {
      if (data.checkoutUrl) {
        setPaymentFlow(PAYMENT_FLOW.SUBSCRIBE);
        window.location.href = data.checkoutUrl;
      } else {
        navigate('/dashboard');
      }
    },
    onError: (error) => {
      setSelectedPlanId(null);
      const parsedError = parseApiError(error);

      if (parsedError.code === ErrorCodes.SUBSCRIPTION_ACTIVE_ALREADY_EXISTS) {
        info(
          i18nKeyContainer.errors.subscription.alreadyActive,
          { description: i18nKeyContainer.errors.subscription.alreadyActiveDesc }
        );
        navigate('/dashboard');
        return;
      }

      handleApiError(error, i18nKeyContainer.onboarding.subscribe.errorGeneric);
    },
  });

  const activePlans = (plansQuery.data ?? []).filter((plan) => plan.isActive);
  const trialDays = activePlans.find((plan) => plan.trialDays > 0)?.trialDays;

  const handleStartTrial = (planId: string) => {
    setSelectedPlanId(planId);
    checkoutMutation.mutate(planId);
  };

  const formatPrice = (amount: number, currency: string) => {
    return new Intl.NumberFormat(undefined, {
      style: 'currency',
      currency,
      minimumFractionDigits: 0,
      maximumFractionDigits: 2,
    }).format(amount);
  };

  const formatInterval = (billingInterval: string, intervalCount: number) => {
    const interval = billingInterval.toLowerCase();
    if (intervalCount <= 1) {
      return `/${interval}`;
    }

    return `/${intervalCount} ${interval}s`;
  };

  const featureKeys = [
    i18nKeyContainer.onboarding.subscribe.feature1,
    i18nKeyContainer.onboarding.subscribe.feature2,
    i18nKeyContainer.onboarding.subscribe.feature4,
    i18nKeyContainer.onboarding.subscribe.feature5,
  ];

  return (
    <div
      className="min-h-screen flex items-center justify-center bg-linear-to-b from-slate-50 via-slate-50 to-slate-100/60 relative overflow-hidden px-4 py-8 sm:py-12"
      dir={isRtl ? 'rtl' : 'ltr'}
    >
      <div className="absolute top-6 end-6 z-20">
        <LanguageSwitcher />
      </div>

      <div className="w-full max-w-4xl z-10 px-2 sm:px-0">
        <div className="flex flex-col items-center mb-6 sm:mb-8">
          <img
            src="/logo.jpg"
            alt="HREnap"
            className="w-16 h-16 rounded-2xl object-cover shadow-xl shadow-primary/20 mb-4"
          />
          <h1 className="text-3xl font-black text-slate-900 tracking-tight">HREnap</h1>
          <p className="text-slate-500 font-medium mt-1">{t('app.name')}</p>
        </div>

        <div className="bg-white">
          <Card className="border-slate-200 shadow-2xl shadow-slate-200/50 overflow-hidden">
            <CardHeader className="space-y-1 pb-6 pt-8 text-center border-b border-slate-50">
              <CardTitle className="text-2xl font-bold">
                {t(i18nKeyContainer.onboarding.subscribe.title)}
              </CardTitle>
              <CardDescription className="text-slate-500">
                {t(i18nKeyContainer.onboarding.subscribe.subtitle)}
              </CardDescription>
            </CardHeader>

            <CardContent className="pt-6 sm:pt-8 pb-6 sm:pb-8 px-5 sm:px-8">
              {plansQuery.isLoading && (
                <div className="flex items-center justify-center py-12">
                  <Loader2 className="w-5 h-5 text-primary animate-spin" />
                </div>
              )}

              {plansQuery.isError && (
                <div className="bg-red-50 border border-red-200 rounded-lg p-4 mb-6">
                  <p className="text-sm text-red-700 text-center">
                    {t(i18nKeyContainer.onboarding.subscribe.errorGeneric)}
                  </p>
                </div>
              )}

              {!plansQuery.isLoading && !plansQuery.isError && activePlans.length === 0 && (
                <div className="bg-amber-50 border border-amber-200 rounded-lg p-4 mb-6">
                  <p className="text-sm text-amber-700 text-center">
                    {t(i18nKeyContainer.onboarding.subscribe.errorGeneric)}
                  </p>
                </div>
              )}

              {!plansQuery.isLoading && !plansQuery.isError && activePlans.length > 0 && (
                <>
                  <div className="grid grid-cols-1 md:grid-cols-2 gap-6 md:gap-8 mb-6">
                    {activePlans.map((plan) => {
                      const isSubmittingCurrentPlan =
                        checkoutMutation.isPending && selectedPlanId === plan.id;

                      return (
                        <div
                          key={plan.id}
                          className="relative border-primary border-[1.5px] rounded-xl p-5 sm:p-6 bg-white flex flex-col h-full"
                        >
                          <div className="absolute -top-3 start-1/2 -translate-x-1/2">
                            <span className="bg-primary text-white text-xs font-bold px-4 py-1 rounded-full shadow-lg whitespace-nowrap">
                              {plan.name}
                            </span>
                          </div>

                          <h3 className="text-xl font-bold text-slate-900 mt-2 mb-4 text-center">
                            {plan.name}
                          </h3>

                          <div className="text-center mb-6">
                            <div className="flex items-baseline justify-center gap-1 flex-wrap">
                              <span className="text-4xl font-black text-primary">
                                {formatPrice(plan.amount, plan.currency)}
                              </span>
                              <span className="text-slate-500 whitespace-nowrap">
                                {formatInterval(plan.billingInterval, plan.intervalCount)}
                              </span>
                            </div>
                          </div>

                          <div className="h-px bg-slate-200 my-6" />

                          <ul className="space-y-3 mb-8 grow">
                            {featureKeys.map((featureKey) => (
                              <li key={featureKey} className="flex items-start gap-3">
                                <div className="shrink-0 w-5 h-5 rounded-full bg-primary/10 flex items-center justify-center mt-0.5">
                                  <Check className="w-3 h-3 text-primary" />
                                </div>
                                <span className="text-slate-700 text-sm">
                                  {t(featureKey)}
                                </span>
                              </li>
                            ))}
                          </ul>

                          <Button
                            onClick={() => handleStartTrial(plan.id)}
                            className="w-full h-12 text-base font-semibold shadow-lg shadow-primary/20 hover:shadow-xl hover:shadow-primary/30 transition-all hover:scale-[1.02] active:scale-[0.98] mt-auto"
                            disabled={checkoutMutation.isPending}
                          >
                            {isSubmittingCurrentPlan ? (
                              <>
                                <Loader2 className="w-4 h-4 me-2 animate-spin" />
                                {t(i18nKeyContainer.onboarding.subscribe.ctaLoading)}
                              </>
                            ) : (
                              t(i18nKeyContainer.onboarding.subscribe.cta)
                            )}
                          </Button>
                        </div>
                      );
                    })}
                  </div>

                  {trialDays !== undefined && (
                    <div className="bg-blue-50 border border-blue-200 rounded-lg p-4 mb-6">
                      <p className="text-sm text-blue-900 text-center">
                        {t(i18nKeyContainer.onboarding.subscribe.trialNote, {
                          days: trialDays,
                        })}
                      </p>
                    </div>
                  )}
                </>
              )}

              <p className="text-center text-sm text-slate-500 mt-6">
                {t(i18nKeyContainer.onboarding.subscribe.signinPrompt)}{' '}
                <Link to="/login" className="text-primary hover:underline font-medium">
                  {t(i18nKeyContainer.onboarding.subscribe.signinLink)}
                </Link>
              </p>
            </CardContent>
          </Card>
        </div>

        <p className="text-center text-xs text-slate-500 mt-8">
          © 2026 HREnap. Built for professionals.
        </p>
      </div>
    </div>
  );
}
