import { useState, useEffect, useRef, useCallback } from "react";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { z } from "zod";
import { Link } from "react-router-dom";
import { useMutation } from "@tanstack/react-query";
import { useTranslation } from "react-i18next";
import { Mail, ArrowLeft, CheckCircle } from "lucide-react";

import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Card, CardContent, CardHeader, CardTitle, CardDescription, CardFooter } from "@/components/ui/card";
import { authApi } from "@/lib/api/auth";
import i18nKeyContainer from "@/lib/i18n/keyContainer";
import { useAuthToast } from "../use-auth-toast";
import LanguageSwitcher from "@/components/ui/language-switcher";

const forgotPasswordSchema = z.object({
  email: z.string().email(),
});

type ForgotPasswordFormValues = z.infer<typeof forgotPasswordSchema>;

export default function ForgotPasswordPage() {
  const { t, i18n } = useTranslation();
  const isRtl = i18n.language === 'ar';
  const authToast = useAuthToast();
  const [isEmailSent, setIsEmailSent] = useState(false);
  const [submittedEmail, setSubmittedEmail] = useState("");
  const [cooldown, setCooldown] = useState(0);
  const timerRef = useRef<ReturnType<typeof setInterval> | null>(null);

  const startCooldown = useCallback(() => {
    setCooldown(60);
    timerRef.current = setInterval(() => {
      setCooldown((prev) => {
        if (prev <= 1) {
          if (timerRef.current) clearInterval(timerRef.current);
          timerRef.current = null;
          return 0;
        }
        return prev - 1;
      });
    }, 1000);
  }, []);

  useEffect(() => {
    return () => {
      if (timerRef.current) clearInterval(timerRef.current);
    };
  }, []);

  const { register, handleSubmit, formState: { errors } } = useForm<ForgotPasswordFormValues>({
    resolver: zodResolver(forgotPasswordSchema),
  });

  const mutation = useMutation({
    mutationFn: (email: string) => authApi.forgotPassword({
      email,
      clientUri: `${window.location.origin}/reset-password`
    }),
    onSuccess: () => {
      authToast.resetLinkSent();
      setIsEmailSent(true);
      startCooldown();
    },
    onError: (err) => {
      authToast.error(err);
    }
  });

  const resendMutation = useMutation({
    mutationFn: () => authApi.forgotPassword({
      email: submittedEmail,
      clientUri: `${window.location.origin}/reset-password`
    }),
    onSuccess: () => {
      authToast.resendSuccess();
      startCooldown();
    },
    onError: (err) => {
      authToast.error(err);
    }
  });

  const onSubmit = (data: ForgotPasswordFormValues) => {
    setSubmittedEmail(data.email);
    mutation.mutate(data.email);
  };

  const isResendDisabled = cooldown > 0 || resendMutation.isPending;

  return (
    <div className="min-h-screen flex items-center justify-center bg-slate-50 relative overflow-hidden px-4" dir={isRtl ? "rtl" : "ltr"}>
      <div className="absolute top-6 end-6 z-20">
        <LanguageSwitcher />
      </div>

      <div className="w-full max-w-md z-10">
        {/* Logo Section */}
        <div className="flex flex-col items-center mb-8">
          <img src="/logo.jpg" alt="HREnap" className="w-16 h-16 rounded-2xl object-cover shadow-xl shadow-primary/20 mb-4" />
          <h1 className="text-3xl font-black text-slate-900 tracking-tight">HREnap</h1>
          <p className="text-slate-500 font-medium mt-1">HR management, redefined.</p>
        </div>

        <div className="bg-white">
          <Card className="border-slate-200 shadow-2xl shadow-slate-200/50 overflow-hidden">
            {isEmailSent ? (
              <>
                <CardHeader className="text-center pb-4 pt-8">
                  <div className="mx-auto w-12 h-12 bg-green-100 rounded-full flex items-center justify-center mb-4">
                    <CheckCircle className="w-6 h-6 text-green-600" />
                  </div>
                  <CardTitle className="text-xl font-bold">
                    {t(i18nKeyContainer.auth.forgotPassword.successTitle)}
                  </CardTitle>
                  <CardDescription className="text-slate-500 mt-2">
                    {t(i18nKeyContainer.auth.forgotPassword.successMessage)}
                  </CardDescription>
                </CardHeader>
                <CardContent className="px-8 pb-6">
                  <Button
                    type="button"
                    className="w-full bg-primary text-white h-11 font-semibold shadow-lg shadow-primary/20 hover:scale-[1.02] active:scale-[0.98] transition-all"
                    disabled={isResendDisabled}
                    onClick={() => resendMutation.mutate()}
                  >
                    {resendMutation.isPending
                      ? t(i18nKeyContainer.auth.forgotPassword.resending)
                      : cooldown > 0
                        ? t(i18nKeyContainer.auth.forgotPassword.resendIn, { seconds: cooldown })
                        : t(i18nKeyContainer.auth.forgotPassword.resendEmail)
                    }
                  </Button>
                </CardContent>
                <CardFooter className="bg-slate-50/50 flex justify-center py-4 border-t border-slate-100">
                  <Link to="/login" className="flex items-center text-sm font-semibold text-slate-600 hover:text-primary transition-colors">
                    <ArrowLeft className={`w-4 h-4 ${isRtl ? "ms-1 rotate-180" : "me-1"}`} />
                    {t(i18nKeyContainer.auth.forgotPassword.backToLogin)}
                  </Link>
                </CardFooter>
              </>
            ) : (
              <>
                <CardHeader className="space-y-1 pb-6 pt-8 text-center border-b border-slate-50">
                  <CardTitle className="text-2xl font-bold">{t(i18nKeyContainer.auth.forgotPassword.title)}</CardTitle>
                  <CardDescription className="text-slate-500">
                    {t(i18nKeyContainer.auth.forgotPassword.description)}
                  </CardDescription>
                </CardHeader>
                <CardContent className="pt-8 pb-8 px-8">
                  <form onSubmit={handleSubmit(onSubmit)} className="space-y-5">
                    <div className="space-y-2">
                      <Label htmlFor="email">{t(i18nKeyContainer.email)}</Label>
                      <div className="relative">
                        <Mail className="absolute start-3 top-1/2 -translate-y-1/2 w-4 h-4 text-slate-400" />
                        <Input 
                          id="email" 
                          type="email" 
                          placeholder="name@company.com"
                          className={`bg-slate-50 border-slate-200 h-11 ps-10 transition-all focus:bg-white ${errors.email ? "border-red-500" : ""}`}
                          {...register("email")}
                          disabled={mutation.isPending}
                        />
                      </div>
                      {errors.email && (
                        <p className="text-sm text-red-500">
                          {t(i18nKeyContainer.settingsPage.validation.invalidEmail)}
                        </p>
                      )}
                    </div>

                    <Button 
                      type="submit" 
                      className="w-full bg-primary text-white h-11 font-bold text-lg shadow-lg shadow-primary/20 hover:scale-[1.02] active:scale-[0.98] transition-all"
                      disabled={mutation.isPending}
                    >
                      {mutation.isPending 
                        ? t(i18nKeyContainer.auth.forgotPassword.sending) 
                        : t(i18nKeyContainer.auth.forgotPassword.sendLink)
                      }
                    </Button>
                  </form>
                </CardContent>
                <CardFooter className="bg-slate-50/50 flex justify-center py-4 border-t border-slate-100">
                  <Link to="/login" className="flex items-center text-sm font-semibold text-slate-600 hover:text-primary transition-colors">
                    <ArrowLeft className={`w-4 h-4 ${isRtl ? "ms-1 rotate-180" : "me-1"}`} />
                    {t(i18nKeyContainer.auth.forgotPassword.backToLogin)}
                  </Link>
                </CardFooter>
              </>
            )}
          </Card>
        </div>

        <p className="mt-8 text-center text-sm text-slate-400 font-medium">
          © 2026 HREnap. Built for professionals.
        </p>
      </div>
    </div>
  );
}
