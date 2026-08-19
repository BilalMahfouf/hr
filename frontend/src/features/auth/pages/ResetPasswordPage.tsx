import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { z } from "zod";
import { useNavigate, useSearchParams, Link } from "react-router-dom";
import { useMutation } from "@tanstack/react-query";
import { useTranslation } from "react-i18next";
import { Lock, ArrowLeft, AlertCircle } from "lucide-react";

import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import LanguageSwitcher from "@/components/ui/language-switcher";
import { Label } from "@/components/ui/label";
import { Card, CardContent, CardHeader, CardTitle, CardDescription, CardFooter } from "@/components/ui/card";
import { authApi } from "@/lib/api/auth";
import i18nKeyContainer from "@/lib/i18n/keyContainer";
import { useAuthToast } from "../use-auth-toast";

const resetPasswordSchema = z.object({
  password: z.string().min(6),
  confirmPassword: z.string()
}).refine((data) => data.password === data.confirmPassword, {
  path: ["confirmPassword"],
});

type ResetPasswordFormValues = z.infer<typeof resetPasswordSchema>;

export default function ResetPasswordPage() {
  const { t, i18n } = useTranslation();
  const isRtl = i18n.language === 'ar';
  const navigate = useNavigate();
  const [searchParams] = useSearchParams();
  const authToast = useAuthToast();
  
  const token = searchParams.get("token");
  const email = searchParams.get("email");

  const { register, handleSubmit, formState: { errors } } = useForm<ResetPasswordFormValues>({
    resolver: zodResolver(resetPasswordSchema),
  });

  const mutation = useMutation({
    mutationFn: (values: ResetPasswordFormValues) => authApi.resetPassword({
      email: email || "",
      token: token || "",
      password: values.password,
      confirmPassword: values.confirmPassword
    }),
    onSuccess: () => {
      authToast.passwordReset();
      navigate("/login");
    },
    onError: (err) => {
      authToast.error(err);
    }
  });

  const onSubmit = (data: ResetPasswordFormValues) => {
    if (!token || !email) {
      authToast.warning(
        i18nKeyContainer.errors.user.invalidResetToken,
        i18nKeyContainer.errors.user.invalidResetTokenDesc
      );
      return;
    }
    mutation.mutate(data);
  };

  // Invalid link state
  if (!token || !email) {
    return (
      <div className="min-h-screen flex items-center justify-center bg-slate-50 px-4" dir={isRtl ? "rtl" : "ltr"}>
        <div className="absolute top-6 end-6 z-20">
          <LanguageSwitcher />
        </div>

        <div className="w-full max-w-md">
          <div className="flex flex-col items-center mb-8">
            <img src="/logo.jpg" alt="HREnap" className="w-16 h-16 rounded-2xl object-cover shadow-xl shadow-primary/20 mb-4" />
            <h1 className="text-3xl font-black text-slate-900 tracking-tight">HREnap</h1>
          </div>

          <Card className="border-slate-200 shadow-2xl shadow-slate-200/50">
            <CardHeader className="text-center pb-4">
              <div className="mx-auto w-12 h-12 bg-red-100 rounded-full flex items-center justify-center mb-4">
                <AlertCircle className="w-6 h-6 text-red-600" />
              </div>
              <CardTitle className="text-xl font-bold text-red-600">
                {t(i18nKeyContainer.errors.user.invalidResetToken)}
              </CardTitle>
              <CardDescription className="text-slate-500 mt-2">
                {t(i18nKeyContainer.errors.user.invalidResetTokenDesc)}
              </CardDescription>
            </CardHeader>
            <CardFooter className="flex justify-center pb-6">
              <Link 
                to="/login" 
                className="flex items-center text-sm font-semibold text-primary hover:underline"
              >
                <ArrowLeft className={`w-4 h-4 ${isRtl ? "ms-1 rotate-180" : "me-1"}`} />
                {t(i18nKeyContainer.auth.forgotPassword.backToLogin)}
              </Link>
            </CardFooter>
          </Card>
        </div>
      </div>
    );
  }

  return (
    <div className="min-h-screen flex items-center justify-center bg-slate-50 relative overflow-hidden px-4" dir={isRtl ? "rtl" : "ltr"}>
      <div className="absolute top-6 end-6 z-20">
        <LanguageSwitcher />
      </div>

      <div className="w-full max-w-md z-10">
        <div className="flex flex-col items-center mb-8">
          <img src="/logo.jpg" alt="HREnap" className="w-16 h-16 rounded-2xl object-cover shadow-xl shadow-primary/20 mb-4" />
          <h1 className="text-3xl font-black text-slate-900 tracking-tight">HREnap</h1>
          <p className="text-slate-500 font-medium mt-1">HR management, redefined.</p>
        </div>

        <div className="bg-white">
          <Card className="border-slate-200 shadow-2xl shadow-slate-200/50 overflow-hidden">
            <CardHeader className="space-y-1 pb-6 pt-8 text-center border-b border-slate-50">
              <CardTitle className="text-2xl font-bold">{t(i18nKeyContainer.auth.resetPassword.title)}</CardTitle>
              <CardDescription className="text-slate-500">
                {t(i18nKeyContainer.auth.resetPassword.description)}
              </CardDescription>
            </CardHeader>
            <CardContent className="pt-8 pb-8 px-8">
              <form onSubmit={handleSubmit(onSubmit)} className="space-y-5">
                <div className="space-y-2">
                  <Label htmlFor="password">{t(i18nKeyContainer.auth.resetPassword.newPassword)}</Label>
                  <div className="relative">
                    <Lock className="absolute start-3 top-1/2 -translate-y-1/2 w-4 h-4 text-slate-400" />
                    <Input 
                      id="password" 
                      type="password" 
                      className={`bg-slate-50 border-slate-200 h-11 ps-10 transition-all focus:bg-white ${errors.password ? "border-red-500" : ""}`}
                      {...register("password")}
                      disabled={mutation.isPending}
                    />
                  </div>
                  {errors.password && (
                    <p className="text-sm text-red-500">
                      {t(i18nKeyContainer.auth.resetPassword.minLength)}
                    </p>
                  )}
                </div>

                <div className="space-y-2">
                  <Label htmlFor="confirmPassword">{t(i18nKeyContainer.auth.resetPassword.confirmPassword)}</Label>
                  <div className="relative">
                    <Lock className="absolute start-3 top-1/2 -translate-y-1/2 w-4 h-4 text-slate-400" />
                    <Input 
                      id="confirmPassword" 
                      type="password" 
                      className={`bg-slate-50 border-slate-200 h-11 ps-10 transition-all focus:bg-white ${errors.confirmPassword ? "border-red-500" : ""}`}
                      {...register("confirmPassword")}
                      disabled={mutation.isPending}
                    />
                  </div>
                  {errors.confirmPassword && (
                    <p className="text-sm text-red-500">
                      {t(i18nKeyContainer.auth.resetPassword.passwordsDoNotMatch)}
                    </p>
                  )}
                </div>

                <Button 
                  type="submit" 
                  className="w-full cursor-pointer bg-primary text-white h-11 font-bold text-lg shadow-lg shadow-primary/20 hover:scale-[1.02] active:scale-[0.98] transition-all"
                  disabled={mutation.isPending}
                >
                  {mutation.isPending 
                    ? t(i18nKeyContainer.auth.resetPassword.submitting) 
                    : t(i18nKeyContainer.auth.resetPassword.submit)
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
          </Card>
        </div>

        <p className="mt-8 text-center text-sm text-slate-400 font-medium">
          © 2026 HREnap. Built for professionals.
        </p>
      </div>
    </div>
  );
}
