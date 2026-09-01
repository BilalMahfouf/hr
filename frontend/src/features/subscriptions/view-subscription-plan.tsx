import { useQuery } from "@tanstack/react-query";
import { useTranslation } from "react-i18next";
import { Calendar, Hash, Loader2 } from "lucide-react";

import { Dialog, DialogContent } from "@/components/ui/dialog";
import { Button } from "@/components/ui/button";
import { StatusBadge } from "@/components/tables";
import i18nKeyContainer from "@/lib/i18n/keyContainer";
import subscriptionPlanApi from "./api/subscription-plan-api";

interface ViewSubscriptionPlanProps {
  open: boolean;
  onClose: () => void;
  planId: string;
}

export default function ViewSubscriptionPlan({ open, onClose, planId }: ViewSubscriptionPlanProps) {
  const { t, i18n } = useTranslation();
  const isRtl = i18n.language === "ar";

  const { data: plan, isLoading, isError } = useQuery({
    queryKey: ["subscription-plan", planId],
    queryFn: () => subscriptionPlanApi.getSubscriptionPlanById(planId),
    enabled: open && !!planId,
  });

  const formatDate = (value: string) => {
    return new Date(value).toLocaleDateString(i18n.language, {
      year: "numeric",
      month: "long",
      day: "numeric",
    });
  };

  const getIntervalLabel = (value: string) => {
    switch (value.toLowerCase()) {
      case "day":
        return t(i18nKeyContainer.subscriptionPlans.intervals.day);
      case "week":
        return t(i18nKeyContainer.subscriptionPlans.intervals.week);
      case "month":
        return t(i18nKeyContainer.subscriptionPlans.intervals.month);
      case "year":
        return t(i18nKeyContainer.subscriptionPlans.intervals.year);
      default:
        return value;
    }
  };

  const formatInterval = (billingInterval: string, intervalCount: number) => {
    const intervalLabel = getIntervalLabel(billingInterval);
    if (intervalCount <= 1) {
      return t(i18nKeyContainer.subscriptionPlans.intervalSingle, {
        interval: intervalLabel,
      });
    }

    return t(i18nKeyContainer.subscriptionPlans.intervalMultiple, {
      count: intervalCount,
      interval: intervalLabel,
    });
  };

  return (
    <Dialog open={open} onOpenChange={onClose}>
      <DialogContent
        className="max-w-2xl border-0 bg-white p-0"
        dir={isRtl ? "rtl" : "ltr"}
        onInteractOutside={(event) => event.preventDefault()}
      >
        <div className="border-b border-slate-200 px-6 py-6">
          <h2 className="text-xl font-semibold text-slate-900">
            {t(i18nKeyContainer.subscriptionPlans.viewTitle)}
          </h2>
          <p className="mt-1 text-sm text-slate-500">
            {t(i18nKeyContainer.subscriptionPlans.viewDescription)}
          </p>
        </div>

        <div className="px-6 py-6">
          {isLoading ? (
            <div className="flex items-center justify-center py-12">
              <Loader2 className="h-6 w-6 animate-spin text-primary" />
            </div>
          ) : isError || !plan ? (
            <div className="rounded-lg border border-slate-200 bg-slate-50 p-4 text-sm text-slate-600">
              {t(i18nKeyContainer.subscriptionPlans.genericError)}
            </div>
          ) : (
            <div className="space-y-4">
              <div className="space-y-1.5">
                <label className="text-sm font-medium text-slate-700">
                  {t(i18nKeyContainer.subscriptionPlans.form.id)}
                </label>
                <div className="flex items-center gap-2 rounded-lg border border-slate-200 bg-slate-50 p-3">
                  <Hash className="h-4 w-4 text-slate-400" />
                  <span className="text-sm text-slate-900">{plan.id}</span>
                </div>
              </div>

              <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
                <div className="space-y-1.5">
                  <label className="text-sm font-medium text-slate-700">
                    {t(i18nKeyContainer.subscriptionPlans.form.name)}
                  </label>
                  <div className="rounded-lg border border-slate-200 bg-slate-50 p-3 text-slate-900">
                    {plan.name}
                  </div>
                </div>

                <div className="space-y-1.5">
                  <label className="text-sm font-medium text-slate-700">
                    {t(i18nKeyContainer.subscriptionPlans.table.slug)}
                  </label>
                  <div className="rounded-lg border border-slate-200 bg-slate-50 p-3 text-slate-900">
                    {plan.slug}
                  </div>
                </div>
              </div>

              <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
                <div className="space-y-1.5">
                  <label className="text-sm font-medium text-slate-700">
                    {t(i18nKeyContainer.subscriptionPlans.form.amount)}
                  </label>
                  <div className="rounded-lg border border-slate-200 bg-slate-50 p-3 text-slate-900">
                    {new Intl.NumberFormat(i18n.language, {
                      style: "currency",
                      currency: plan.currency,
                      minimumFractionDigits: 0,
                      maximumFractionDigits: 2,
                    }).format(plan.amount)}
                  </div>
                </div>

                <div className="space-y-1.5">
                  <label className="text-sm font-medium text-slate-700">
                    {t(i18nKeyContainer.subscriptionPlans.form.currency)}
                  </label>
                  <div className="rounded-lg border border-slate-200 bg-slate-50 p-3 text-slate-900">
                    {plan.currency}
                  </div>
                </div>
              </div>

              <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
                <div className="space-y-1.5">
                  <label className="text-sm font-medium text-slate-700">
                    {t(i18nKeyContainer.subscriptionPlans.form.billingInterval)}
                  </label>
                  <div className="rounded-lg border border-slate-200 bg-slate-50 p-3 text-slate-900">
                    {formatInterval(plan.billingInterval, plan.intervalCount)}
                  </div>
                </div>

                <div className="space-y-1.5">
                  <label className="text-sm font-medium text-slate-700">
                    {t(i18nKeyContainer.subscriptionPlans.form.trialDays)}
                  </label>
                  <div className="rounded-lg border border-slate-200 bg-slate-50 p-3 text-slate-900">
                    {plan.trialDays}
                  </div>
                </div>
              </div>

              <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
                <div className="space-y-1.5">
                  <label className="text-sm font-medium text-slate-700">
                    {t(i18nKeyContainer.subscriptionPlans.table.status)}
                  </label>
                  <div className="rounded-lg border border-slate-200 bg-slate-50 p-3">
                    <StatusBadge
                      status={
                        plan.isActive
                          ? t(i18nKeyContainer.subscriptionPlans.status.active)
                          : t(i18nKeyContainer.subscriptionPlans.status.inactive)
                      }
                      variant={plan.isActive ? "success" : "secondary"}
                    />
                  </div>
                </div>

                <div className="space-y-1.5">
                  <label className="text-sm font-medium text-slate-700">
                    {t(i18nKeyContainer.subscriptionPlans.table.createdOn)}
                  </label>
                  <div className="flex items-center gap-2 rounded-lg border border-slate-200 bg-slate-50 p-3 text-slate-900">
                    <Calendar className="h-4 w-4 text-slate-400" />
                    <span>{formatDate(plan.createdOnUtc)}</span>
                  </div>
                </div>
              </div>

              <div className="pt-2">
                <Button onClick={onClose} className="w-full">
                  {t(i18nKeyContainer.common.close)}
                </Button>
              </div>
            </div>
          )}
        </div>
      </DialogContent>
    </Dialog>
  );
}
