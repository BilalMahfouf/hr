import { zodResolver } from "@hookform/resolvers/zod";
import { useForm } from "react-hook-form";
import { z } from "zod";
import { Button } from "@/components/ui/button";
import {
  Form,
  FormControl,
  FormField,
  FormItem,
  FormLabel,
  FormMessage,
} from "@/components/ui/form";
import { Input } from "@/components/ui/input";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { useTranslation } from "react-i18next";
import LanguageSwitcher from "@/components/ui/language-switcher";
import i18nKeyContainer from "@/lib/i18n/keyContainer";
import { useRegister } from "@/features/auth/api/register";
import { useMutation } from "@tanstack/react-query";
import { authApi } from "@/lib/api/auth";
import { useNavigate } from "react-router-dom";
import { toast } from "sonner";
import { Loader2 } from "lucide-react";
import { Link } from "react-router-dom";

export default function RegisterPage() {
  const { t, i18n } = useTranslation();
  const navigate = useNavigate();
  const isRtl = i18n.language === "ar";
  
  const registerMutation = useRegister();
  const loginMutation = useMutation({
    mutationFn: authApi.login,
  });

  const formSchema = z.object({
    firstName: z.string().min(2, { message: t(i18nKeyContainer.register.firstName) + " is too short" }), // Ideally use separate error keys
    lastName: z.string().min(2, { message: t(i18nKeyContainer.register.lastName) + " is too short" }),
    userName: z.string().min(3, { message: t(i18nKeyContainer.register.userName) + " is too short" }),
    email: z.string().email(),
    password: z.string().min(6, { message: t(i18nKeyContainer.register.password) + " is too short" }),
    confirmPassword: z.string(),
  }).refine((data) => data.password === data.confirmPassword, {
    message: "Passwords don't match",
    path: ["confirmPassword"],
  });

  const form = useForm<z.infer<typeof formSchema>>({
    resolver: zodResolver(formSchema),
    defaultValues: {
      firstName: "",
      lastName: "",
      userName: "",
      email: "",
      password: "",
      confirmPassword: "",
    },
  });

  async function onSubmit(values: z.infer<typeof formSchema>) {
    try {
      // 1. Register
      await registerMutation.mutateAsync({
        firstName: values.firstName,
        lastName: values.lastName,
        userName: values.userName,
        email: values.email,
        password: values.password,
      });

      // 2. Login (Auto-login)
      const loginSuccess = await loginMutation.mutateAsync({
        email: values.email,
        password: values.password,
      });

      if (loginSuccess) {
          toast.success(t(i18nKeyContainer.register.successMessage));
          navigate("/onboarding/subscribe");
      } else {
        // Fallback if login fails (e.g., verify email required)
        navigate("/login");
        toast.info("Registration successful. Please login.");
      }

    } catch (error: unknown) {
        // Handle Global Errors
        const axiosError = error as { response?: { data?: { errors?: Array<string | { description: string }> } } };
        const errors = axiosError.response?.data?.errors;
        if (Array.isArray(errors)) {
             toast.error(errors.map((e) => typeof e === 'string' ? e : e.description).join(", "));
        } else {
            toast.error("An unexpected error occurred.");
        }
    }
  }

  return (
    <div 
        className="min-h-screen flex items-center justify-center bg-slate-50 relative overflow-hidden px-4"
        dir={isRtl ? "rtl" : "ltr"}
    >
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
            <CardTitle className="text-2xl font-bold">{t(i18nKeyContainer.register.title)}</CardTitle>
            <CardDescription className="text-slate-500">
              {t(i18nKeyContainer.register.description)}
            </CardDescription>
          </CardHeader>
        <CardContent className="pt-8 pb-8 px-8">
          <Form {...form}>
            <form onSubmit={form.handleSubmit(onSubmit)} className="space-y-5">
              <div className="grid grid-cols-2 gap-4">
                <FormField
                    control={form.control}
                    name="firstName"
                    render={({ field }) => (
                    <FormItem>
                        <FormLabel>{t(i18nKeyContainer.register.firstName)}</FormLabel>
                        <FormControl>
                        <Input placeholder="" {...field} className="bg-slate-50 border-slate-200 h-11 transition-all focus:bg-white" />
                        </FormControl>
                        <FormMessage />
                    </FormItem>
                    )}
                />
                <FormField
                    control={form.control}
                    name="lastName"
                    render={({ field }) => (
                    <FormItem>
                        <FormLabel>{t(i18nKeyContainer.register.lastName)}</FormLabel>
                        <FormControl>
                        <Input placeholder="" {...field} className="bg-slate-50 border-slate-200 h-11 transition-all focus:bg-white" />
                        </FormControl>
                        <FormMessage />
                    </FormItem>
                    )}
                />
              </div>

              <FormField
                control={form.control}
                name="userName"
                render={({ field }) => (
                  <FormItem>
                    <FormLabel>{t(i18nKeyContainer.register.userName)}</FormLabel>
                    <FormControl>
                      <Input placeholder="" {...field} className="bg-slate-50 border-slate-200 h-11 transition-all focus:bg-white" />
                    </FormControl>
                    <FormMessage />
                  </FormItem>
                )}
              />

              <FormField
                control={form.control}
                name="email"
                render={({ field }) => (
                  <FormItem>
                    <FormLabel>{t(i18nKeyContainer.register.email)}</FormLabel>
                    <FormControl>
                      <Input placeholder="" {...field} type="email" className="bg-slate-50 border-slate-200 h-11 transition-all focus:bg-white" />
                    </FormControl>
                    <FormMessage />
                  </FormItem>
                )}
              />

              <FormField
                control={form.control}
                name="password"
                render={({ field }) => (
                  <FormItem>
                    <FormLabel>{t(i18nKeyContainer.register.password)}</FormLabel>
                    <FormControl>
                      <Input type="password" placeholder="" {...field} className="bg-slate-50 border-slate-200 h-11 transition-all focus:bg-white" />
                    </FormControl>
                    <FormMessage />
                  </FormItem>
                )}
              />
               <FormField
                control={form.control}
                name="confirmPassword"
                render={({ field }) => (
                  <FormItem>
                    <FormLabel>{t(i18nKeyContainer.register.confirmPassword)}</FormLabel>
                    <FormControl>
                      <Input type="password" placeholder="" {...field} className="bg-slate-50 border-slate-200 h-11 transition-all focus:bg-white" />
                    </FormControl>
                    <FormMessage />
                  </FormItem>
                )}
              />

              <Button type="submit" className="w-full cursor-pointer bg-primary text-white h-11 font-bold text-lg shadow-lg shadow-primary/20 hover:scale-[1.02] active:scale-[0.98] transition-all" disabled={registerMutation.isPending || loginMutation.isPending}>
                {(registerMutation.isPending || loginMutation.isPending) && <Loader2 className="me-2 h-4 w-4 animate-spin" />}
                {t(i18nKeyContainer.register.submit)}
              </Button>
            </form>
          </Form>
           <div className="mt-4 text-center text-sm">
            <span className="text-muted-foreground">
                {t(i18nKeyContainer.register.alreadyHaveAccount)}{" "}
            </span>
            <Link to="/login" className="font-medium text-primary hover:underline cursor-pointer">
                {t(i18nKeyContainer.register.loginLink)}
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
