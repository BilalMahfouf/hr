import { useState, useEffect } from "react";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { useTranslation } from "react-i18next";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { toast } from "sonner";

// UI Components
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Skeleton } from "@/components/ui/skeleton";
import { Separator } from "@/components/ui/separator";
import { Badge } from "@/components/ui/badge";
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from "@/components/ui/card";
import {
  Form,
  FormControl,
  FormDescription,
  FormField,
  FormItem,
  FormLabel,
  FormMessage,
} from "@/components/ui/form";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import {
  Sheet,
  SheetContent,
  SheetHeader,
  SheetTitle,
  SheetTrigger,
} from "@/components/ui/sheet";

// Icons
import {
  User,
  Globe,
  Menu,
  Loader2,
  Check,
  Bell,
  Monitor,
  CreditCard,
  CalendarDays,
  RefreshCcw,
} from "lucide-react";

// Local imports
import i18nKeyContainer from "@/lib/i18n/keyContainer";
import {
  settingsApi,
  type UserProfile,
  type UpdateProfileRequest,
} from "./settings-api";
import {
  profileFormSchema,
  type ProfileFormValues,
} from "./schemas";
import {
  getPushStatus,
  ensurePushSubscription,
  unsubscribeFromPush,
  type PushStatus,
} from "../notifications/push-notifications";
import notificationApi from "../notifications/notification-api";
import subscriptionApi, {
  type MySubscriptionResponse,
} from "../subscriptions/api/subscription-api";
import { parseApiError, ErrorCodes } from "@/lib/api/error-types";

// ============================================================================
// Constants
// ============================================================================

const SUPPORTED_LANGUAGES = [
  { code: "en", label: "English" },
  { code: "fr", label: "Français" },
  { code: "ar", label: "العربية" },
] as const;

type SettingsSection =
  | "profile"
  | "notifications"
  | "subscriptions";

// ============================================================================
// Settings Navigation Item
// ============================================================================

interface NavItemProps {
  icon: React.ReactNode;
  label: string;
  isActive: boolean;
  onClick: () => void;
}

function NavItem({ icon, label, isActive, onClick }: NavItemProps) {
  return (
    <button
      type="button"
      onClick={onClick}
      className={`
        flex w-full items-center gap-3 rounded-lg px-3 py-2.5 text-sm font-medium
        cursor-pointer transition-colors
        focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2
        ${
          isActive
            ? "bg-primary/10 text-primary"
            : "text-muted-foreground hover:bg-muted hover:text-foreground"
        }
      `}
    >
      {icon}
      {label}
    </button>
  );
}

// ============================================================================
// Settings Sidebar (Desktop)
// ============================================================================

interface SettingsSidebarProps {
  activeSection: SettingsSection;
  onSectionChange: (section: SettingsSection) => void;
  t: (key: string) => string;
  canViewSubscriptions: boolean;
}

function SettingsSidebar({
  activeSection,
  onSectionChange,
  t,
  canViewSubscriptions,
}: SettingsSidebarProps) {
  return (
    <nav className="flex flex-col gap-1">
      <NavItem
        icon={<User className="h-4 w-4" />}
        label={t(i18nKeyContainer.settingsPage.tabs.profile)}
        isActive={activeSection === "profile"}
        onClick={() => onSectionChange("profile")}
      />
      <NavItem
        icon={<Bell className="h-4 w-4" />}
        label={t(i18nKeyContainer.settingsPage.tabs.notifications)}
        isActive={activeSection === "notifications"}
        onClick={() => onSectionChange("notifications")}
      />
      {canViewSubscriptions && (
        <NavItem
          icon={<CreditCard className="h-4 w-4" />}
          label={t(i18nKeyContainer.settingsPage.tabs.subscriptions)}
          isActive={activeSection === "subscriptions"}
          onClick={() => onSectionChange("subscriptions")}
        />
      )}
    </nav>
  );
}

// ============================================================================
// Mobile Navigation Drawer
// ============================================================================

interface MobileNavProps {
  activeSection: SettingsSection;
  onSectionChange: (section: SettingsSection) => void;
  t: (key: string) => string;
  canViewSubscriptions: boolean;
}

function MobileNav({
  activeSection,
  onSectionChange,
  t,
  canViewSubscriptions,
}: MobileNavProps) {
  const [open, setOpen] = useState(false);

  const handleSelect = (section: SettingsSection) => {
    onSectionChange(section);
    setOpen(false);
  };

  return (
    <Sheet open={open} onOpenChange={setOpen}>
      <SheetTrigger asChild>
        <Button
          variant="outline"
          size="icon"
          className="md:hidden border-transparent bg-transparent hover:bg-slate-100"
        >
          <Menu className="h-5 w-5" />
          <span className="sr-only">
            {t(i18nKeyContainer.settingsPage.openMenu)}
          </span>
        </Button>
      </SheetTrigger>
      <SheetContent side="left" className="w-80 bg-white border-e border-slate-200 p-0 sm:max-w-sm">
        <div className="flex flex-col h-full bg-white">
          <SheetHeader className="p-6 bg-white border-b-0 text-left">
            <SheetTitle className="text-xl font-bold text-slate-900 border-none">
              {t(i18nKeyContainer.settingsPage.title)}
            </SheetTitle>
          </SheetHeader>
          <div className="flex-1 overflow-y-auto p-4 bg-white">
            <SettingsSidebar
              activeSection={activeSection}
              onSectionChange={handleSelect}
              t={t}
              canViewSubscriptions={canViewSubscriptions}
            />
          </div>
        </div>
      </SheetContent>
    </Sheet>
  );
}

// ============================================================================
// Profile Section
// ============================================================================

interface ProfileSectionProps {
  userProfile: UserProfile | undefined;
  isLoading: boolean;
  t: (key: string) => string;
  i18n: ReturnType<typeof useTranslation>["i18n"];
}

function ProfileSection({
  userProfile,
  isLoading,
  t,
  i18n,
}: ProfileSectionProps) {
  const queryClient = useQueryClient();

  const form = useForm<ProfileFormValues>({
    resolver: zodResolver(profileFormSchema),
    defaultValues: {
      firstName: "",
      lastName: "",
      userName: "",
      email: "",
    },
  });

  // Populate form when data loads
  useEffect(() => {
    if (userProfile) {
      form.reset({
        firstName: userProfile.firstName,
        lastName: userProfile.lastName,
        userName: userProfile.userName,
        email: userProfile.email,
      });
    }
  }, [userProfile, form]);

  // Profile update mutation
  const updateProfileMutation = useMutation({
    mutationFn: (data: UpdateProfileRequest) => settingsApi.updateProfile(data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["userProfile"] });
      toast.success(t(i18nKeyContainer.settingsPage.profile.updateSuccess), {
        description: t(i18nKeyContainer.settingsPage.profile.updateSuccessDesc),
      });
    },
    onError: () => {
      toast.error(t(i18nKeyContainer.settingsPage.profile.updateError), {
        description: t(i18nKeyContainer.settingsPage.profile.updateErrorDesc),
      });
    },
  });

  // Email change mutation
  const changeEmailMutation = useMutation({
    mutationFn: (email: string) => settingsApi.changeEmail({ email }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["userProfile"] });
      toast.success(t(i18nKeyContainer.settingsPage.profile.emailUpdateSuccess), {
        description: t(i18nKeyContainer.settingsPage.profile.emailUpdateSuccessDesc),
      });
    },
    onError: () => {
      toast.error(t(i18nKeyContainer.settingsPage.profile.updateError), {
        description: t(i18nKeyContainer.settingsPage.profile.updateErrorDesc),
      });
    },
  });

  const handleLanguageChange = (lang: string) => {
    i18n.changeLanguage(lang);
    document.documentElement.dir = lang === "ar" ? "rtl" : "ltr";
  };

  const onSubmit = async (data: ProfileFormValues) => {
    const profileChanged =
      data.firstName !== userProfile?.firstName ||
      data.lastName !== userProfile?.lastName ||
      data.userName !== userProfile?.userName;

    const emailChanged = data.email !== userProfile?.email;

    try {
      if (profileChanged) {
        await updateProfileMutation.mutateAsync({
          userName: data.userName,
          firstName: data.firstName,
          lastName: data.lastName,
        });
      }
      if (emailChanged) {
        await changeEmailMutation.mutateAsync(data.email);
      }
    } catch {
      // Error handling is done in mutation callbacks
    }
  };

  const isSaving =
    updateProfileMutation.isPending || changeEmailMutation.isPending;
  const isSuccess =
    updateProfileMutation.isSuccess || changeEmailMutation.isSuccess;

  if (isLoading) {
    return <ProfileSectionSkeleton />;
  }

  return (
    <div className="space-y-6">
      {/* Section Header */}
      <div>
        <h2 className="text-xl font-semibold tracking-tight">
          {t(i18nKeyContainer.settingsPage.profile.header)}
        </h2>
        <p className="text-sm text-muted-foreground mt-1">
          {t(i18nKeyContainer.settingsPage.profile.description)}
        </p>
      </div>

      <Separator className="bg-border/20" />

      {/* Personal Information Card */}
      <Card className="border border-slate-200 bg-white shadow-sm">
        <CardHeader className="pb-4">
          <CardTitle className="text-base font-medium">
            {t(i18nKeyContainer.settingsPage.profile.personalInfo.title)}
          </CardTitle>
          <CardDescription>
            {t(i18nKeyContainer.settingsPage.profile.personalInfo.description)}
          </CardDescription>
        </CardHeader>
        <CardContent>
          <Form {...form}>
            <form onSubmit={form.handleSubmit(onSubmit)} className="space-y-5">
              <div className="grid gap-5 sm:grid-cols-2">
                <FormField
                  control={form.control}
                  name="firstName"
                  render={({ field }) => (
                    <FormItem>
                      <FormLabel>
                        {t(i18nKeyContainer.settingsPage.profile.firstName)}
                      </FormLabel>
                      <FormControl>
                        <Input
                          {...field}
                          placeholder={t(
                            i18nKeyContainer.settingsPage.profile
                              .firstNamePlaceholder
                          )}
                          className="rounded-md border border-slate-200 bg-white px-3 py-2.5 text-sm placeholder:text-slate-400  focus:outline-none focus:ring-1  resize-none"
                        />
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
                      <FormLabel>
                        {t(i18nKeyContainer.settingsPage.profile.lastName)}
                      </FormLabel>
                      <FormControl>
                        <Input
                          {...field}
                          placeholder={t(
                            i18nKeyContainer.settingsPage.profile
                              .lastNamePlaceholder
                          )}
                          className="rounded-md border border-slate-200 bg-white px-3 py-2.5 text-sm placeholder:text-slate-400  focus:outline-none focus:ring-1  resize-none"
                        />
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
                    <FormLabel>
                      {t(i18nKeyContainer.settingsPage.profile.username)}
                    </FormLabel>
                    <FormControl>
                      <Input
                        {...field}
                        placeholder={t(
                          i18nKeyContainer.settingsPage.profile.usernamePlaceholder
                        )}
                        className="rounded-md border border-slate-200 bg-white px-3 py-2.5 text-sm placeholder:text-slate-400  focus:outline-none focus:ring-1  resize-none"
                      />
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
                    <FormLabel>
                      {t(i18nKeyContainer.settingsPage.profile.email)}
                    </FormLabel>
                    <FormControl>
                      <Input
                        {...field}
                        type="email"
                        placeholder={t(
                          i18nKeyContainer.settingsPage.profile.emailPlaceholder
                        )}
                        className="rounded-md border border-slate-200 bg-white px-3 py-2.5 text-sm placeholder:text-slate-400  focus:outline-none focus:ring-1  resize-none"
                      />
                    </FormControl>
                    <FormDescription>
                      {t(i18nKeyContainer.settingsPage.profile.emailNote)}
                    </FormDescription>
                    <FormMessage />
                  </FormItem>
                )}
              />

              <div className="flex items-center gap-3 pt-2">
                <Button
                  type="submit"
                  disabled={!form.formState.isDirty || isSaving}
                  className=""
                >
                  {isSaving ? (
                    <>
                      <Loader2 className="mr-2 h-4 w-4 animate-spin" />
                      {t(i18nKeyContainer.settingsPage.profile.saving)}
                    </>
                  ) : isSuccess && !form.formState.isDirty ? (
                    <>
                      <Check className="mr-2 h-4 w-4" />
                      {t(i18nKeyContainer.settingsPage.profile.saved)}
                    </>
                  ) : (
                    t(i18nKeyContainer.settingsPage.profile.saveChanges)
                  )}
                </Button>
              </div>
            </form>
          </Form>
        </CardContent>
      </Card>

      {/* Language & Preferences Card */}
      <Card className="border border-slate-200 bg-white shadow-sm">
        <CardHeader className="pb-4">
          <CardTitle className="text-base font-medium">
            {t(i18nKeyContainer.settingsPage.profile.preferences.title)}
          </CardTitle>
          <CardDescription>
            {t(i18nKeyContainer.settingsPage.profile.preferences.description)}
          </CardDescription>
        </CardHeader>
        <CardContent>
          <div className="max-w-xs">
            <label
              htmlFor="language-select"
              className="flex items-center gap-2 text-sm font-medium mb-2"
            >
              <Globe className="h-4 w-4 text-muted-foreground" />
              {t(i18nKeyContainer.settingsPage.profile.language)}
            </label>
            <Select value={i18n.language} onValueChange={handleLanguageChange}>
              <SelectTrigger
                id="language-select"
                className=" rounded-md border border-slate-200  px-3 py-2.5 text-sm placeholder:text-slate-400  focus:outline-none focus:ring-1  resize-none bg-white"
              >
                <SelectValue />
              </SelectTrigger>
              <SelectContent className="rounded-md border border-slate-200 bg-white px-3 py-2.5 text-sm placeholder:text-slate-400  focus:outline-none  resize-none">
                {SUPPORTED_LANGUAGES.map((lang) => (
                  <SelectItem
                    key={lang.code}
                    value={lang.code}
                    className="cursor-pointer hover:bg-slate-100 focus:bg-slate-100"
                  >
                    {lang.label}
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>
          </div>
        </CardContent>
      </Card>
    </div>
  );
}

function ProfileSectionSkeleton() {
  return (
    <div className="space-y-6">
      <div>
        <Skeleton className="h-7 w-40" />
        <Skeleton className="h-4 w-64 mt-2" />
      </div>
      <Separator className="bg-border/20" />
      <Card className="border border-slate-200 bg-white shadow-sm">
        <CardHeader className="pb-4">
          <Skeleton className="h-5 w-36" />
          <Skeleton className="h-4 w-56 mt-1" />
        </CardHeader>
        <CardContent className="space-y-5">
          <div className="grid gap-5 sm:grid-cols-2">
            <Skeleton className="h-10 w-full" />
            <Skeleton className="h-10 w-full" />
          </div>
          <Skeleton className="h-10 w-full" />
          <Skeleton className="h-10 w-full" />
          <Skeleton className="h-10 w-32" />
        </CardContent>
      </Card>
      <Card className="border border-slate-200 bg-white shadow-sm">
        <CardHeader className="pb-4">
          <Skeleton className="h-5 w-40" />
          <Skeleton className="h-4 w-56 mt-1" />
        </CardHeader>
        <CardContent>
          <Skeleton className="h-10 w-48" />
        </CardContent>
      </Card>
    </div>
  );
}

// ============================================================================
// Notifications Section
// ============================================================================

function NotificationsSection({ t, i18n }: { t: (key: string) => string; i18n: { language: string } }) {
  const isRtl = i18n.language === "ar";
  const [pushStatus, setPushStatus] = useState<PushStatus>("disabled");
  const [isPushLoading, setIsPushLoading] = useState(false);
  const [isTestLoading, setIsTestLoading] = useState(false);

  useEffect(() => {
    getPushStatus().then(setPushStatus);
  }, []);

  const handlePushToggle = async () => {
    setIsPushLoading(true);
    try {
      if (pushStatus === "enabled") {
        await unsubscribeFromPush();
        toast.success(t(i18nKeyContainer.toast.notification.pushDisabled));
      } else {
        await ensurePushSubscription();
        toast.success(t(i18nKeyContainer.toast.notification.pushEnabled));
      }
      setPushStatus(await getPushStatus());
    } catch (err: unknown) {
      const message = err instanceof Error ? err.message : "";
      if (message === "push_permission_denied") {
        setPushStatus("denied");
        toast.error(t(i18nKeyContainer.errors.notification.pushBlocked));
      } else {
        toast.error(t(i18nKeyContainer.errors.notification.pushFailed));
      }
    } finally {
      setIsPushLoading(false);
    }
  };

  const handleTestPush = async () => {
    setIsTestLoading(true);
    try {
      await notificationApi.sendTestPush();
      toast.success("Test push sent — check your desktop notifications!");
    } catch {
      toast.error("Failed to send test push. Check the browser console for details.");
    } finally {
      setIsTestLoading(false);
    }
  };

  const statusLabel = (() => {
    if (pushStatus === "enabled")     return t(i18nKeyContainer.notification.pushEnabled);
    if (pushStatus === "denied")      return t(i18nKeyContainer.notification.pushBlocked);
    if (pushStatus === "unsupported") return t(i18nKeyContainer.notification.pushUnsupported);
    return t(i18nKeyContainer.notification.pushDisabled);
  })();

  const statusColor = (() => {
    if (pushStatus === "enabled")     return "text-green-600 bg-green-50 ring-green-200";
    if (pushStatus === "denied")      return "text-red-600 bg-red-50 ring-red-200";
    if (pushStatus === "unsupported") return "text-slate-500 bg-slate-50 ring-slate-200";
    return "text-amber-600 bg-amber-50 ring-amber-200";
  })();

  return (
    <div className="space-y-6" dir={isRtl ? "rtl" : "ltr"}>
      {/* Section Header */}
      <div>
        <h2 className="text-xl font-semibold tracking-tight">
          {t(i18nKeyContainer.settingsPage.notifications.header)}
        </h2>
        <p className="text-sm text-muted-foreground mt-1">
          {t(i18nKeyContainer.settingsPage.notifications.description)}
        </p>
      </div>

      <Separator className="bg-border/20" />

      {/* Push Notifications Card */}
      <Card className="border border-slate-200 bg-white shadow-sm">
        <CardHeader className="pb-4">
          <CardTitle className="flex items-center gap-2 text-base font-medium">
            <Monitor className="h-4 w-4 text-muted-foreground" />
            {t(i18nKeyContainer.notification.pushTitle)}
          </CardTitle>
          <CardDescription>
            {pushStatus === "denied"
              ? t(i18nKeyContainer.notification.pushBlockedDesc)
              : pushStatus === "unsupported"
              ? t(i18nKeyContainer.notification.pushUnsupportedDesc)
              : t(i18nKeyContainer.settingsPage.notifications.pushDesc)}
          </CardDescription>
        </CardHeader>
        <CardContent>
          <div className="flex items-center justify-between gap-4">
            <span
              className={`inline-flex items-center px-2.5 py-1 rounded-full text-xs font-medium ring-1 ring-inset ${statusColor}`}
            >
              {statusLabel}
            </span>
            {pushStatus !== "unsupported" && (
              <Button
                variant={pushStatus === "enabled" ? "secondary" : "default"}
                size="sm"
                disabled={isPushLoading || pushStatus === "denied"}
                onClick={handlePushToggle}
                className="shrink-0"
              >
                {isPushLoading ? (
                  <Loader2 className="h-4 w-4 animate-spin" />
                ) : pushStatus === "enabled" ? (
                  t(i18nKeyContainer.notification.pushDisable)
                ) : (
                  t(i18nKeyContainer.notification.pushEnable)
                )}
              </Button>
            )}
          </div>

          {/* Test push — only shown when subscribed */}
          {pushStatus === "enabled" && (
            <div className="mt-4 pt-4 border-t border-slate-100 flex items-center justify-between">
              <div className="space-y-0.5">
                <p className="text-sm font-medium">Test Push Notification</p>
                <p className="text-xs text-muted-foreground">
                  Send a test push to verify your browser receives it correctly.
                </p>
              </div>
              <Button
                variant="white"
                size="sm"
                disabled={isTestLoading}
                onClick={handleTestPush}
                className="shrink-0"
              >
                {isTestLoading ? (
                  <Loader2 className="h-4 w-4 animate-spin" />
                ) : (
                  "Send Test"
                )}
              </Button>
            </div>
          )}
        </CardContent>
      </Card>
    </div>
  );
}

// ============================================================================
// Subscriptions Section
// ============================================================================

function formatSubscriptionCurrency(amount: number, currency: string) {
  return new Intl.NumberFormat(undefined, {
    style: "currency",
    currency,
    minimumFractionDigits: 0,
    maximumFractionDigits: 2,
  }).format(amount);
}

function formatSubscriptionDate(value: string | null) {
  if (!value) {
    return null;
  }

  const date = new Date(value);
  if (Number.isNaN(date.getTime())) {
    return null;
  }

  return new Intl.DateTimeFormat(undefined, { dateStyle: "medium" }).format(
    date
  );
}

function getStatusBadgeVariant(status: string) {
  switch (status.toLowerCase()) {
    case "active":
      return "success" as const;
    case "trialing":
      return "info" as const;
    case "pending":
      return "warning" as const;
    case "paymentfailed":
    case "pastdue":
    case "cancelled":
    case "expired":
      return "error" as const;
    default:
      return "secondary" as const;
  }
}

function getStatusTranslationKey(status: string) {
  switch (status.toLowerCase()) {
    case "active":
      return i18nKeyContainer.settingsPage.subscriptions.statusValues.active;
    case "trialing":
      return i18nKeyContainer.settingsPage.subscriptions.statusValues.trialing;
    case "pending":
      return i18nKeyContainer.settingsPage.subscriptions.statusValues.pending;
    case "paymentfailed":
      return i18nKeyContainer.settingsPage.subscriptions.statusValues.paymentFailed;
    case "pastdue":
      return i18nKeyContainer.settingsPage.subscriptions.statusValues.pastDue;
    case "cancelled":
      return i18nKeyContainer.settingsPage.subscriptions.statusValues.cancelled;
    case "expired":
      return i18nKeyContainer.settingsPage.subscriptions.statusValues.expired;
    default:
      return i18nKeyContainer.settingsPage.subscriptions.statusValues.unknown;
  }
}

function SubscriptionInfoRow({ label, value }: { label: string; value: string }) {
  return (
    <div className="flex items-center justify-between gap-4 rounded-lg border border-slate-200 bg-white px-4 py-3">
      <span className="text-sm text-slate-600">{label}</span>
      <span className="text-sm font-medium text-slate-900 text-right">{value}</span>
    </div>
  );
}

function SubscriptionsSection({ t }: { t: (key: string) => string }) {
  const subscriptionQuery = useQuery({
    queryKey: ["subscription", "me"],
    queryFn: subscriptionApi.getMySubscription,
    retry: false,
  });

  if (subscriptionQuery.isLoading) {
    return <SubscriptionsSectionSkeleton />;
  }

  const parsedError = subscriptionQuery.error
    ? parseApiError(subscriptionQuery.error)
    : null;
  const isNotFound =
    parsedError?.status === 404 ||
    parsedError?.code === ErrorCodes.SUBSCRIPTION_NOT_FOUND;

  if (subscriptionQuery.isError && !isNotFound) {
    return (
      <div className="space-y-6">
        <div>
          <h2 className="text-xl font-semibold tracking-tight">
            {t(i18nKeyContainer.settingsPage.subscriptions.header)}
          </h2>
          <p className="text-sm text-muted-foreground mt-1">
            {t(i18nKeyContainer.settingsPage.subscriptions.description)}
          </p>
        </div>

        <Separator className="bg-border/20" />

        <Card className="border border-slate-200 bg-white shadow-sm">
          <CardContent className="py-8 space-y-4 text-center">
            <p className="text-sm text-slate-700">
              {t(i18nKeyContainer.settingsPage.subscriptions.loadError)}
            </p>
            <Button
              type="button"
              variant="white"
              onClick={() => subscriptionQuery.refetch()}
            >
              <RefreshCcw className="me-2 h-4 w-4" />
              {t(i18nKeyContainer.settingsPage.subscriptions.retry)}
            </Button>
          </CardContent>
        </Card>
      </div>
    );
  }

  if (isNotFound || !subscriptionQuery.data) {
    return (
      <div className="space-y-6">
        <div>
          <h2 className="text-xl font-semibold tracking-tight">
            {t(i18nKeyContainer.settingsPage.subscriptions.header)}
          </h2>
          <p className="text-sm text-muted-foreground mt-1">
            {t(i18nKeyContainer.settingsPage.subscriptions.description)}
          </p>
        </div>

        <Separator className="bg-border/20" />

        <Card className="border border-slate-200 bg-white shadow-sm">
          <CardContent className="py-8 text-center">
            <p className="text-sm text-slate-700">
              {t(i18nKeyContainer.settingsPage.subscriptions.emptyTitle)}
            </p>
            <p className="text-xs text-slate-500 mt-1">
              {t(i18nKeyContainer.settingsPage.subscriptions.emptyDescription)}
            </p>
          </CardContent>
        </Card>
      </div>
    );
  }

  const subscription: MySubscriptionResponse = subscriptionQuery.data;
  const formattedStart =
    formatSubscriptionDate(subscription.currentPeriodStart) ??
    t(i18nKeyContainer.settingsPage.subscriptions.notAvailable);
  const formattedEnd =
    formatSubscriptionDate(subscription.currentPeriodEnd) ??
    t(i18nKeyContainer.settingsPage.subscriptions.notAvailable);
  const formattedTrialEnd =
    formatSubscriptionDate(subscription.trialEndsAt) ??
    t(i18nKeyContainer.settingsPage.subscriptions.notAvailable);
  const formattedCancelledAt =
    formatSubscriptionDate(subscription.cancelledAt) ??
    t(i18nKeyContainer.settingsPage.subscriptions.notAvailable);
  const formattedUpdatedAt =
    formatSubscriptionDate(subscription.updatedAt) ??
    t(i18nKeyContainer.settingsPage.subscriptions.notAvailable);

  const intervalText = subscription.planDisplayName
    ? subscription.planDisplayName
    : t(i18nKeyContainer.settingsPage.subscriptions.notAvailable);

  return (
    <div className="space-y-6">
      <div>
        <h2 className="text-xl font-semibold tracking-tight">
          {t(i18nKeyContainer.settingsPage.subscriptions.header)}
        </h2>
        <p className="text-sm text-muted-foreground mt-1">
          {t(i18nKeyContainer.settingsPage.subscriptions.description)}
        </p>
      </div>

      <Separator className="bg-border/20" />

      <Card className="border border-slate-200 bg-white shadow-sm">
        <CardHeader className="pb-4">
          <CardTitle className="flex items-center gap-2 text-base font-medium">
            <CreditCard className="h-4 w-4 text-muted-foreground" />
            {t(i18nKeyContainer.settingsPage.subscriptions.overview.title)}
          </CardTitle>
          <CardDescription>
            {t(i18nKeyContainer.settingsPage.subscriptions.overview.description)}
          </CardDescription>
        </CardHeader>
        <CardContent className="space-y-3">
          <SubscriptionInfoRow
            label={t(i18nKeyContainer.settingsPage.subscriptions.planName)}
            value={subscription.planName}
          />
          <SubscriptionInfoRow
            label={t(i18nKeyContainer.settingsPage.subscriptions.price)}
            value={formatSubscriptionCurrency(
              subscription.planPrice,
              subscription.planCurrency
            )}
          />
          <SubscriptionInfoRow
            label={t(i18nKeyContainer.settingsPage.subscriptions.interval)}
            value={intervalText}
          />
          <div className="flex items-center justify-between gap-4 rounded-lg border border-slate-200 bg-white px-4 py-3">
            <span className="text-sm text-slate-600">
              {t(i18nKeyContainer.settingsPage.subscriptions.status)}
            </span>
            <Badge variant={getStatusBadgeVariant(subscription.subscriptionStatus)}>
              {t(getStatusTranslationKey(subscription.subscriptionStatus))}
            </Badge>
          </div>
        </CardContent>
      </Card>

      <Card className="border border-slate-200 bg-white shadow-sm">
        <CardHeader className="pb-4">
          <CardTitle className="flex items-center gap-2 text-base font-medium">
            <CalendarDays className="h-4 w-4 text-muted-foreground" />
            {t(i18nKeyContainer.settingsPage.subscriptions.billingCycle.title)}
          </CardTitle>
          <CardDescription>
            {t(i18nKeyContainer.settingsPage.subscriptions.billingCycle.description)}
          </CardDescription>
        </CardHeader>
        <CardContent className="space-y-3">
          <SubscriptionInfoRow
            label={t(i18nKeyContainer.settingsPage.subscriptions.currentPeriodStart)}
            value={formattedStart}
          />
          <SubscriptionInfoRow
            label={t(i18nKeyContainer.settingsPage.subscriptions.currentPeriodEnd)}
            value={formattedEnd}
          />
          <SubscriptionInfoRow
            label={t(i18nKeyContainer.settingsPage.subscriptions.trialEndsAt)}
            value={formattedTrialEnd}
          />
          <SubscriptionInfoRow
            label={t(i18nKeyContainer.settingsPage.subscriptions.cancelledAt)}
            value={formattedCancelledAt}
          />
          <SubscriptionInfoRow
            label={t(i18nKeyContainer.settingsPage.subscriptions.updatedAt)}
            value={formattedUpdatedAt}
          />
        </CardContent>
      </Card>

      <Card className="border border-slate-200 bg-white shadow-sm">
        <CardHeader className="pb-4">
          <CardTitle className="text-base font-medium">
            {t(i18nKeyContainer.settingsPage.subscriptions.identifiers.title)}
          </CardTitle>
          <CardDescription>
            {t(i18nKeyContainer.settingsPage.subscriptions.identifiers.description)}
          </CardDescription>
        </CardHeader>
        <CardContent className="space-y-3">
          <SubscriptionInfoRow
            label={t(i18nKeyContainer.settingsPage.subscriptions.subscriptionId)}
            value={subscription.id}
          />
          <SubscriptionInfoRow
            label={t(i18nKeyContainer.settingsPage.subscriptions.previousSubscriptionId)}
            value={
              subscription.previousSubscriptionId ??
              t(i18nKeyContainer.settingsPage.subscriptions.notAvailable)
            }
          />
        </CardContent>
      </Card>

    </div>
  );
}

function SubscriptionsSectionSkeleton() {
  return (
    <div className="space-y-6">
      <div>
        <Skeleton className="h-7 w-40" />
        <Skeleton className="h-4 w-64 mt-2" />
      </div>
      <Separator className="bg-border/20" />
      {Array.from({ length: 3 }).map((_, idx) => (
        <Card key={idx} className="border border-slate-200 bg-white shadow-sm">
          <CardHeader className="pb-4">
            <Skeleton className="h-5 w-44" />
            <Skeleton className="h-4 w-60 mt-1" />
          </CardHeader>
          <CardContent className="space-y-3">
            <Skeleton className="h-12 w-full" />
            <Skeleton className="h-12 w-full" />
            <Skeleton className="h-12 w-full" />
          </CardContent>
        </Card>
      ))}
    </div>
  );
}

// ============================================================================
// Main Settings Page
// ============================================================================

export default function SettingPage() {
  const { t, i18n } = useTranslation();
  const isRtl = i18n.language === "ar";
  const [activeSection, setActiveSection] = useState<SettingsSection>("profile");

  // Fetch user profile
  const {
    data: userProfile,
    isLoading: isLoadingProfile,
    isError: isProfileError,
  } = useQuery({
    queryKey: ["userProfile"],
    queryFn: settingsApi.getMe,
  });

  const isDoctor = userProfile?.role?.toLowerCase() === "doctor";

  useEffect(() => {
   const foo=()=>{
    if (activeSection === "subscriptions" && !isDoctor) {
      setActiveSection("profile");
    }
   } 
   foo();
  }, [activeSection, isDoctor]);

  if (isProfileError) {
    return (
      <div className="p-6" dir={isRtl ? "rtl" : "ltr"}>
        <p className="text-destructive">
          {t(i18nKeyContainer.settingsPage.loadError)}
        </p>
      </div>
    );
  }

  return (
    <div
      className="min-h-screen bg-background"
      dir={isRtl ? "rtl" : "ltr"}
    >
      {/* Page Container */}
      <div className="mx-auto max-w-6xl px-4 py-6 sm:px-6 lg:px-8">
        {/* Header */}
        <div className="flex items-center gap-4 mb-8">
          <MobileNav
            activeSection={activeSection}
            onSectionChange={setActiveSection}
            t={t}
            canViewSubscriptions={isDoctor}
          />
          <div>
            <h1 className="text-2xl font-bold tracking-tight">
              {t(i18nKeyContainer.settingsPage.title)}
            </h1>
            <p className="text-sm text-muted-foreground mt-1">
              {t(i18nKeyContainer.settingsPage.description)}
            </p>
          </div>
        </div>

        {/* Layout: Sidebar + Content */}
        <div className="flex flex-col gap-8 md:flex-row">
          {/* Desktop Sidebar */}
          <aside className="hidden md:block w-56 shrink-0">
            <div className="sticky top-6">
              <SettingsSidebar
                activeSection={activeSection}
                onSectionChange={setActiveSection}
                t={t}
                canViewSubscriptions={isDoctor}
              />
            </div>
          </aside>

          {/* Content Panel */}
          <main className="flex-1 min-w-0">
            {activeSection === "profile" && (
              <ProfileSection
                userProfile={userProfile}
                isLoading={isLoadingProfile}
                t={t}
                i18n={i18n}
              />
            )}
            {activeSection === "notifications" && (
              <NotificationsSection t={t} i18n={i18n} />
            )}
            {activeSection === "subscriptions" && isDoctor && (
              <SubscriptionsSection t={t} />
            )}
          </main>
        </div>
      </div>
    </div>
  );
}