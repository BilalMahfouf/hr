import { useNavigate } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { XCircle } from 'lucide-react';

import { Button } from '@/components/ui/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import LanguageSwitcher from '@/components/ui/language-switcher';
import i18nKeyContainer from '@/lib/i18n/keyContainer';
import { PAYMENT_FLOW, clearPaymentFlow, getPaymentFlow } from '@/features/subscriptions/payment-flow';

export default function PaymentFailedPage() {
  const { t, i18n } = useTranslation();
  const navigate = useNavigate();
  const isRtl = i18n.language === 'ar';

  const handleTryAgain = () => {
    const flow = getPaymentFlow();
    clearPaymentFlow();

    if (flow === PAYMENT_FLOW.RENEW) {
      navigate('/onboarding/renew');
      return;
    }

    navigate('/onboarding/subscribe');
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
            <CardTitle className="text-2xl font-bold text-red-600">
              {t(i18nKeyContainer.onboarding.payment.failed.title)}
            </CardTitle>
            <CardDescription className="text-slate-500">
              {t(i18nKeyContainer.onboarding.payment.failed.subtitle)}
            </CardDescription>
          </CardHeader>

          <CardContent className="pt-8 pb-8 px-6 sm:px-8">
            <div className="flex flex-col items-center text-center space-y-6">
              <div className="w-16 h-16 rounded-full bg-red-100 flex items-center justify-center">
                <XCircle className="w-8 h-8 text-red-600" />
              </div>

              <Button
                onClick={handleTryAgain}
                className="w-full h-12 text-base font-semibold shadow-lg shadow-primary/20 hover:shadow-xl hover:shadow-primary/30 transition-all cursor-pointer"
              >
                {t(i18nKeyContainer.onboarding.payment.failed.tryAgain)}
              </Button>
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
