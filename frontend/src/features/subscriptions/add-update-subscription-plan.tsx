import { useState } from "react";
import { useMutation, useQueryClient } from "@tanstack/react-query";
import { useTranslation } from "react-i18next";
import { DollarSign, Hash } from "lucide-react";

import { Dialog, DialogContent } from "@/components/ui/dialog";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import { useToast } from "@/hooks/use-toast";
import i18nKeyContainer from "@/lib/i18n/keyContainer";
import subscriptionPlanApi, {
  type BillingInterval,
  billingIntervals,
  type CreateSubscriptionPlanRequest,
  type SubscriptionPlan,
} from "./api/subscription-plan-api";

const MODE_ADD_NEW = "addnew";
const MODE_UPDATE = "update";

interface AddUpdateSubscriptionPlanProps {
  open: boolean;
  onClose: () => void;
  planId?: string;
  initialPlan?: SubscriptionPlan;
}

export default function AddUpdateSubscriptionPlan({
  open,
  onClose,
  planId,
  initialPlan,
}: AddUpdateSubscriptionPlanProps) {
  const normalizeBillingInterval = (value?: string): BillingInterval => {
    return value?.toLowerCase() === "year" ? "year" : "month";
  };

  const mode = planId ? MODE_UPDATE : MODE_ADD_NEW;
  const [name, setName] = useState(initialPlan?.name ?? "");
  const [amount, setAmount] = useState(initialPlan ? String(initialPlan.amount) : "");
  const [currency, setCurrency] = useState(initialPlan?.currency ?? "DZD");
  const [billingInterval, setBillingInterval] = useState<BillingInterval>(
    normalizeBillingInterval(initialPlan?.billingInterval)
  );
  const [intervalCount, setIntervalCount] = useState(
    initialPlan ? String(initialPlan.intervalCount) : "1"
  );
  const [trialDays, setTrialDays] = useState(initialPlan ? String(initialPlan.trialDays) : "0");

  const { t, i18n } = useTranslation();
  const isRtl = i18n.language === "ar";
  const queryClient = useQueryClient();
  const { handleApiError, success, warning } = useToast();

  const getIntervalLabel = (value: string) => {
    switch (value) {
      case "month":
        return t(i18nKeyContainer.subscriptionPlans.intervals.month);
      case "year":
        return t(i18nKeyContainer.subscriptionPlans.intervals.year);
      default:
        return value;
    }
  };

  const mutation = useMutation({
    mutationFn: (request: CreateSubscriptionPlanRequest) => {
      if (mode === MODE_UPDATE && planId) {
        return subscriptionPlanApi.updateSubscriptionPlan(planId, request);
      }

      return subscriptionPlanApi.createSubscriptionPlan(request);
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["subscription-plans"] });
      if (mode === MODE_UPDATE && planId) {
        queryClient.invalidateQueries({ queryKey: ["subscription-plan", planId] });
      }

      if (mode === MODE_UPDATE) {
        success(i18nKeyContainer.toast.subscriptionPlan.updated, {
          description: i18nKeyContainer.toast.subscriptionPlan.updatedDesc,
        });
      } else {
        success(i18nKeyContainer.toast.subscriptionPlan.added, {
          description: i18nKeyContainer.toast.subscriptionPlan.addedDesc,
        });
      }

      onClose();
    },
    onError: (error) => {
      handleApiError(error, i18nKeyContainer.subscriptionPlans.genericError);
    },
  });

  const handleClose = () => {
    onClose();
  };

  const handleSubmit = (event: React.FormEvent) => {
    event.preventDefault();

    const parsedAmount = Number(amount);
    const parsedIntervalCount = Number(intervalCount);
    const parsedTrialDays = Number(trialDays);

    if (
      !name.trim() ||
      Number.isNaN(parsedAmount) ||
      Number.isNaN(parsedIntervalCount) ||
      Number.isNaN(parsedTrialDays) ||
      parsedAmount < 0 ||
      parsedIntervalCount <= 0 ||
      parsedTrialDays < 0 ||
      !billingIntervals.includes(billingInterval)
    ) {
      warning(i18nKeyContainer.subscriptionPlans.form.invalidError, {
        description: i18nKeyContainer.subscriptionPlans.form.invalidErrorDesc,
      });
      return;
    }

    if (parsedAmount <= 50) {
      warning(i18nKeyContainer.subscriptionPlans.form.amountMinError, {
        description: i18nKeyContainer.subscriptionPlans.form.amountMinErrorDesc,
      });
      return;
    }

    mutation.mutate({
      name: name.trim(),
      amount: parsedAmount,
      currency: currency.trim().toUpperCase(),
      billingInterval,
      intervalCount: parsedIntervalCount,
      trialDays: parsedTrialDays,
    });
  };

  return (
    <Dialog open={open} onOpenChange={handleClose}>
      <DialogContent
        className="max-w-2xl border-0 bg-white p-0"
        dir={isRtl ? "rtl" : "ltr"}
        onInteractOutside={(event) => event.preventDefault()}
      >
        <div className="border-b border-slate-200 px-6 py-6">
          <h2 className="text-xl font-semibold text-slate-900">
            {t(
              mode === MODE_UPDATE
                ? i18nKeyContainer.subscriptionPlans.form.updateTitle
                : i18nKeyContainer.subscriptionPlans.form.addTitle
            )}
          </h2>
          <p className="mt-1 text-sm text-slate-500">
            {t(
              mode === MODE_UPDATE
                ? i18nKeyContainer.subscriptionPlans.form.updateDescription
                : i18nKeyContainer.subscriptionPlans.form.addDescription
            )}
          </p>
        </div>

        <div className="px-6 py-6">
          <form className="space-y-4" onSubmit={handleSubmit} noValidate>
              {mode === MODE_UPDATE && planId && (
                <div className="space-y-2">
                  <Label htmlFor="subscriptionPlanId">
                    {t(i18nKeyContainer.subscriptionPlans.form.id)}
                  </Label>
                  <div className="relative">
                    <Hash className="absolute start-3 top-1/2 h-4 w-4 -translate-y-1/2 text-slate-400" />
                    <Input
                      id="subscriptionPlanId"
                      value={planId}
                      readOnly
                      className="h-11 border-slate-200 bg-slate-100 ps-10 text-slate-500"
                    />
                  </div>
                </div>
              )}

              <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
                <div className="space-y-2">
                  <Label htmlFor="planName">{t(i18nKeyContainer.subscriptionPlans.form.name)}</Label>
                  <Input
                    id="planName"
                    value={name}
                    onChange={(event) => setName(event.target.value)}
                    className="h-11 border-slate-200 bg-slate-50 focus:bg-white"
                    placeholder={t(i18nKeyContainer.subscriptionPlans.form.namePlaceholder)}
                  />
                </div>

                <div className="space-y-2">
                  <Label htmlFor="planCurrency">{t(i18nKeyContainer.subscriptionPlans.form.currency)}</Label>
                  <Input
                    id="planCurrency"
                    value={currency}
                    onChange={(event) => setCurrency(event.target.value.toUpperCase())}
                    className="h-11 border-slate-200 bg-slate-50 focus:bg-white"
                    maxLength={3}
                  />
                </div>
              </div>

              <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
                <div className="space-y-2">
                  <Label htmlFor="planAmount">{t(i18nKeyContainer.subscriptionPlans.form.amount)}</Label>
                  <div className="relative">
                    <DollarSign className="absolute start-3 top-1/2 h-4 w-4 -translate-y-1/2 text-slate-400" />
                    <Input
                      id="planAmount"
                      type="number"
                      step="0.01"
                      value={amount}
                      onChange={(event) => setAmount(event.target.value)}
                      className="h-11 border-slate-200 bg-slate-50 ps-10 focus:bg-white"
                    />
                  </div>
                </div>

                <div className="space-y-2">
                  <Label htmlFor="planInterval">{t(i18nKeyContainer.subscriptionPlans.form.billingInterval)}</Label>
                  <Select
                    value={billingInterval}
                    onValueChange={(value) => setBillingInterval(normalizeBillingInterval(value))}
                  >
                    <SelectTrigger
                      id="planInterval"
                      className="h-11 border-slate-200 bg-slate-50 focus:bg-white focus:ring-2 focus:ring-slate-300 focus:ring-offset-0 data-[state=open]:ring-2 data-[state=open]:ring-slate-300 data-[state=open]:ring-offset-0"
                    >
                      <SelectValue placeholder={t(i18nKeyContainer.subscriptionPlans.form.selectInterval)} />
                    </SelectTrigger>
                    <SelectContent className="border-slate-200 bg-white shadow-lg">
                      {billingIntervals.map((interval) => (
                        <SelectItem
                          key={interval}
                          value={interval}
                          className="focus:bg-slate-100 focus:text-slate-900"
                        >
                          {getIntervalLabel(interval)}
                        </SelectItem>
                      ))}
                    </SelectContent>
                  </Select>
                </div>
              </div>

              <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
                <div className="space-y-2">
                  <Label htmlFor="planIntervalCount">
                    {t(i18nKeyContainer.subscriptionPlans.form.intervalCount)}
                  </Label>
                  <Input
                    id="planIntervalCount"
                    type="number"
                    step="1"
                    value={intervalCount}
                    onChange={(event) => setIntervalCount(event.target.value)}
                    className="h-11 border-slate-200 bg-slate-50 focus:bg-white"
                  />
                </div>

                <div className="space-y-2">
                  <Label htmlFor="planTrialDays">{t(i18nKeyContainer.subscriptionPlans.form.trialDays)}</Label>
                  <Input
                    id="planTrialDays"
                    type="number"
                    step="1"
                    value={trialDays}
                    onChange={(event) => setTrialDays(event.target.value)}
                    className="h-11 border-slate-200 bg-slate-50 focus:bg-white"
                  />
                </div>
              </div>

              <div className="flex gap-3 pt-2">
                <Button
                  type="button"
                  variant="white"
                  className="h-11 flex-1"
                  onClick={handleClose}
                  disabled={mutation.isPending}
                >
                  {t(i18nKeyContainer.common.cancel)}
                </Button>
                <Button
                  type="submit"
                  className="h-11 flex-1"
                  disabled={mutation.isPending}
                >
                  {mutation.isPending
                    ? t(i18nKeyContainer.common.saving)
                    : t(
                        mode === MODE_UPDATE
                          ? i18nKeyContainer.subscriptionPlans.form.updateAction
                          : i18nKeyContainer.subscriptionPlans.form.addAction
                      )}
                </Button>
              </div>
          </form>
        </div>
      </DialogContent>
    </Dialog>
  );
}
