import { useEffect, useState, useRef } from 'react';
import { useNavigate } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { useQuery } from '@tanstack/react-query';
import { CheckCircle2, Loader2 } from 'lucide-react';

import { Button } from '@/components/ui/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import LanguageSwitcher from '@/components/ui/language-switcher';
import i18nKeyContainer from '@/lib/i18n/keyContainer';
import api from '@/lib/api/api';
import type { CurrentUser } from '@/features/auth/types';
import { clearPaymentFlow } from '@/features/subscriptions/payment-flow';

type VerificationState = 'verifying' | 'verified' | 'timeout';

export default function PaymentSuccessPage() {
  const { t, i18n } = useTranslation();
  const navigate = useNavigate();
  const isRtl = i18n.language === 'ar';

  const [state, setState] = useState<VerificationState>('verifying');
  const [countdown, setCountdown] = useState(3);
  const pollCountRef = useRef(0);
  const maxPolls = 7;

  const { data: user } = useQuery({
    queryKey: ['payment-verification'],
    queryFn: async () => {
      const response = await api.get<CurrentUser>('/auth/me');
      if (response.status !== 200) {
        throw new Error('Failed to fetch user');
      }
      return response.data;
    },
    enabled: state === 'verifying',
    refetchInterval: state === 'verifying' ? 2000 : false,
    retry: false,
  });

  useEffect(() => {
    clearPaymentFlow();
  }, []);

  useEffect(() => {
const foo=()=>{
    console.log("state",state)
    console.log('Polling for subscription status...', pollCountRef.current + 1);
    if (state !== 'verifying') return;

    if (user?.subscriptionStatus === 'Active' || user?.subscriptionStatus === 'Trialing') {
      setState('verified');
      return;
    }

    pollCountRef.current += 1;
    if (pollCountRef.current >= maxPolls) {
      setState('timeout');
    }
}
foo();

  }, [user, state]);

  useEffect(() => {
    if (state !== 'verified') return;

    if (countdown <= 0) {
      navigate('/dashboard');
      return;
    }

    const timer = setTimeout(() => {
      setCountdown((prev) => prev - 1);
    }, 1000);

    return () => clearTimeout(timer);
  }, [state, countdown, navigate]);

  const handleGoToDashboard = () => {
    navigate('/dashboard');
  };

  return (
    <div
      className="min-h-screen flex items-center justify-center bg-linear-to-b from-slate-50 via-slate-50 to-slate-100/60 relative overflow-hidden px-4 py-8 sm:py-12"
      dir={isRtl ? 'rtl' : 'ltr'}
    >
      <div className="absolute top-6 end-6 z-20">
        <LanguageSwitcher />
      </div>

      <div className="w-full max-w-md z-10 px-2 sm:px-0">
        <div className="flex flex-col items-center mb-6 sm:mb-8">
          <img
            src="/logo.jpg"
            alt="HREnap"
            className="w-16 h-16 rounded-2xl object-cover shadow-xl shadow-primary/20 mb-4"
          />
          <h1 className="text-3xl font-black text-slate-900 tracking-tight">HREnap</h1>
          <p className="text-slate-500 font-medium mt-1">{t('app.name')}</p>
        </div>

        <Card className="border-slate-200 shadow-2xl shadow-slate-200/50 overflow-hidden">
          <CardHeader className="space-y-1 pb-6 pt-8 text-center border-b border-slate-50">
            <CardTitle className="text-2xl font-bold">
              {t(i18nKeyContainer.onboarding.payment.success.title)}
            </CardTitle>
            <CardDescription className="text-slate-500">
              {t(i18nKeyContainer.onboarding.payment.success.subtitle)}
            </CardDescription>
          </CardHeader>

          <CardContent className="pt-8 pb-8 px-6 sm:px-8">
            <div className="flex flex-col items-center text-center space-y-6">
              {state === 'verifying' && (
                <>
                  <div className="w-16 h-16 rounded-full bg-primary/10 flex items-center justify-center">
                    <Loader2 className="w-8 h-8 text-primary animate-spin" />
                  </div>
                  <p className="text-slate-600">
                    {t(i18nKeyContainer.onboarding.payment.success.verifying)}
                  </p>
                </>
              )}

              {state === 'verified' && (
                <>
                  <div className="w-16 h-16 rounded-full bg-green-100 flex items-center justify-center">
                    <CheckCircle2 className="w-8 h-8 text-green-600" />
                  </div>
                  <div className="space-y-2">
                    <p className="text-green-700 font-medium">
                      {t(i18nKeyContainer.onboarding.payment.success.verified)}
                    </p>
                    <p className="text-slate-500 text-sm">
                      {t(i18nKeyContainer.onboarding.payment.success.redirecting, {
                        seconds: countdown,
                      })}
                    </p>
                  </div>
                </>
              )}

              {state === 'timeout' && (
                <>
                  <div className="w-16 h-16 rounded-full bg-amber-100 flex items-center justify-center">
                    <Loader2 className="w-8 h-8 text-amber-600" />
                  </div>
                  <div className="space-y-2">
                    <p className="text-amber-700 font-medium">
                      {t(i18nKeyContainer.onboarding.payment.success.timeout)}
                    </p>
                    <p className="text-slate-500 text-sm">
                      {t(i18nKeyContainer.onboarding.payment.success.timeoutDesc)}
                    </p>
                  </div>
                  <Button
                    onClick={handleGoToDashboard}
                    className="w-full h-12 text-base font-semibold shadow-lg shadow-primary/20 hover:shadow-xl hover:shadow-primary/30 transition-all"
                  >
                    {t(i18nKeyContainer.onboarding.payment.success.goToDashboard)}
                  </Button>
                </>
              )}
            </div>
          </CardContent>
        </Card>

        <p className="text-center text-xs text-slate-500 mt-8">
          &copy; 2026 HREnap. Built for professionals.
        </p>
      </div>
    </div>
  );
}
