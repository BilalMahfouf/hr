import { useState } from 'react';
import { useNavigate, Link } from 'react-router-dom';
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Card, CardContent, CardHeader, CardTitle, CardDescription } from "@/components/ui/card";
// import { toast } from "sonner";
import { Lock, Mail, Loader2 } from "lucide-react";
import { useTranslation } from "react-i18next";
import i18nKeyContainer from "@/lib/i18n/keyContainer";
import { authApi } from '@/lib/api/auth';
import api from '@/lib/api/api';
import { useMutation } from '@tanstack/react-query';
import { useToast } from "@/hooks/use-toast";
import LanguageSwitcher from "@/components/ui/language-switcher";
import type { CurrentUser } from '@/features/auth/types';
import {
  isExpiredStatus,
  isPastDueStatus,
} from '@/features/subscriptions/subscription-status';

export default function Login() {
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const navigate = useNavigate();
  const { t, i18n } = useTranslation();
  const { handleApiError } = useToast();
  const isRtl = i18n.language === 'ar';


  const mutation = useMutation({
    mutationFn: authApi.login,
    onSuccess: async () => {
      try {
        const response = await api.get<CurrentUser>('/auth/me');
        const subscriptionStatus = response.data?.subscriptionStatus;

        if (isPastDueStatus(subscriptionStatus) || isExpiredStatus(subscriptionStatus)) {
          navigate('/onboarding/renew');
          return;
        }
      } catch {
        // If status fetch fails, fallback to default app entry.
      }

        navigate('/dashboard');
    },
    onError: (error) => {
      handleApiError(error);
    },
  });



  const handleLogin = async (e: React.FormEvent) => {
    e.preventDefault();
    if (email && password) {
      mutation.mutate({ email, password });
    } else {
        return;
    }
  };

  return (
    <div className="min-h-screen flex items-center justify-center bg-slate-50 relative overflow-hidden px-4" dir={isRtl ? "rtl" : "ltr"}>
      {/* Background Decorative Elements */}

      {/* Language Switcher */}
      <div className="absolute top-6 end-6 z-20">
        <LanguageSwitcher />
      </div>

      <div className="w-full max-w-md z-10">
        <div className="flex flex-col items-center mb-8">
          <img src="/logo.jpg" alt="HREnap" className="w-16 h-16 rounded-2xl object-cover shadow-xl shadow-primary/20 mb-4" />
          <h1 className="text-3xl font-black text-slate-900 tracking-tight">HREnap</h1>
          <p className="text-slate-500 font-medium mt-1">HR management, redefined.</p>
        </div>
<div className='bg-white'>
        <Card className="border-slate-200 shadow-2xl shadow-slate-200/50 overflow-hidden ">
          <CardHeader className="space-y-1 pb-6 pt-8 text-center border-b border-slate-50">
            <CardTitle className="text-2xl font-bold">{t(i18nKeyContainer.login)}</CardTitle>
            <CardDescription className="text-slate-500">
              {t(i18nKeyContainer.loginDescription)}
            </CardDescription>
          </CardHeader>
          <CardContent className="pt-8 pb-8 px-8">
            <form onSubmit={handleLogin} className="space-y-5">
              <div className="space-y-2">
                <Label htmlFor="email">{t(i18nKeyContainer.email)}</Label>
                <div className="relative">
                  <Mail className="absolute start-3 top-1/2 -translate-y-1/2 w-4 h-4 text-slate-400" />
                  <Input 
                    id="email" 
                    type="email" 
                    placeholder="name@company.com" 
                    className="bg-slate-50 border-slate-200 h-11 ps-10 transition-all focus:bg-white"
                    value={email}
                    onChange={(e) => setEmail(e.target.value)}
                  />
                </div>
              </div>
              <div className="space-y-2">
                <div className="flex items-center justify-between">
                  <Label htmlFor="password">{t(i18nKeyContainer.password)}</Label>
                  <Link to="/forgot-password" className="text-xs font-semibold text-primary hover:underline hover:text-primary/80">
                    {t(i18nKeyContainer.forgotPassword)}
                  </Link>
                </div>
                <div className="relative">
                  <Lock className="absolute start-3 top-1/2 -translate-y-1/2 w-4 h-4 text-slate-400" />
                  <Input 
                    id="password" 
                    type="password" 
                    className="bg-slate-50 border-slate-200 h-11 ps-10 transition-all focus:bg-white"
                    value={password}
                    onChange={(e) => setPassword(e.target.value)}
                  />
                </div>
              </div>
              <Button 
                type="submit" 
                className="w-full cursor-pointer bg-primary text-white h-11 font-bold text-lg shadow-lg shadow-primary/20 hover:scale-[1.02] active:scale-[0.98] transition-all"
                disabled={mutation.isPending}
              >
                {mutation.isPending && <Loader2 className="me-2 h-4 w-4 animate-spin" />}
                {t(i18nKeyContainer.signIn)}
              </Button>
            </form>
            <div className="mt-4 text-center text-sm">
              <span className="text-muted-foreground">
                {t(i18nKeyContainer.noAccount)}{" "}
              </span>
              <Link to="/register" className="font-medium text-primary hover:underline cursor-pointer">
                {t(i18nKeyContainer.signUp)}
              </Link>
            </div>
          </CardContent>
        </Card>


</div>
        <p className="mt-8 text-center text-sm text-slate-400 font-medium">
          © 2026 HREnap. Built for professionals.
        </p>
      </div>
    </div>
  );
}