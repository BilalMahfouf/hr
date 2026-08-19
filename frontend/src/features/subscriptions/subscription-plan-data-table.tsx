import { useMemo } from "react";
import { useTranslation } from "react-i18next";
import { Eye, Pencil, ToggleLeft, ToggleRight } from "lucide-react";

import {
  DataTableRowActions,
  DateCell,
  StatusBadge,
  type RowAction,
} from "@/components/tables";
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table";
import { Card, CardContent } from "@/components/ui/card";
import i18nKeyContainer from "@/lib/i18n/keyContainer";
import type { SubscriptionPlan } from "./api/subscription-plan-api";

interface SubscriptionPlanDataTableProps {
  plans: SubscriptionPlan[];
  isLoading: boolean;
  isError: boolean;
  onView: (plan: SubscriptionPlan) => void;
  onEdit: (plan: SubscriptionPlan) => void;
  onActivate: (plan: SubscriptionPlan) => void;
  onDeactivate: (plan: SubscriptionPlan) => void;
}

export default function SubscriptionPlanDataTable({
  plans,
  isLoading,
  isError,
  onView,
  onEdit,
  onActivate,
  onDeactivate,
}: SubscriptionPlanDataTableProps) {
  const { t, i18n } = useTranslation();
  const isRtl = i18n.language === "ar";

  const sortedPlans = useMemo(() => {
    return [...plans].sort((a, b) => {
      return new Date(b.createdOnUtc).getTime() - new Date(a.createdOnUtc).getTime();
    });
  }, [plans]);

  const formatCurrency = (amount: number, currency: string) => {
    return new Intl.NumberFormat(i18n.language, {
      style: "currency",
      currency,
      minimumFractionDigits: 0,
      maximumFractionDigits: 2,
    }).format(amount);
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

  if (isLoading) {
    return (
      <Card className="border border-slate-200 bg-white shadow-sm">
        <CardContent className="space-y-3 p-6">
          <div className="h-10 rounded-lg bg-slate-100 animate-pulse" />
          <div className="h-10 rounded-lg bg-slate-100 animate-pulse" />
          <div className="h-10 rounded-lg bg-slate-100 animate-pulse" />
        </CardContent>
      </Card>
    );
  }

  if (isError) {
    return (
      <div className="rounded-lg border border-red-200 bg-red-50 p-4 text-sm text-red-700">
        {t(i18nKeyContainer.subscriptionPlans.loadError)}
      </div>
    );
  }

  if (sortedPlans.length === 0) {
    return (
      <div className="rounded-lg border border-slate-200 bg-slate-50 p-6 text-center">
        <p className="text-sm text-slate-600">{t(i18nKeyContainer.subscriptionPlans.empty)}</p>
      </div>
    );
  }

  return (
    <div dir={isRtl ? "rtl" : "ltr"}>
      <Card className="border border-slate-200 bg-white shadow-sm">
        <CardContent className="p-0">
          <Table>
            <TableHeader className="[&_tr]:border-slate-200">
              <TableRow className="border-slate-200 bg-slate-50 hover:bg-slate-50">
                <TableHead>{t(i18nKeyContainer.subscriptionPlans.table.name)}</TableHead>
                <TableHead>{t(i18nKeyContainer.subscriptionPlans.table.slug)}</TableHead>
                <TableHead>{t(i18nKeyContainer.subscriptionPlans.table.price)}</TableHead>
                <TableHead>{t(i18nKeyContainer.subscriptionPlans.table.interval)}</TableHead>
                <TableHead>{t(i18nKeyContainer.subscriptionPlans.table.trialDays)}</TableHead>
                <TableHead>{t(i18nKeyContainer.subscriptionPlans.table.status)}</TableHead>
                <TableHead>{t(i18nKeyContainer.subscriptionPlans.table.createdOn)}</TableHead>
                <TableHead className="text-right">{t(i18nKeyContainer.table.openMenu)}</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {sortedPlans.map((plan) => {
                const actions: RowAction<SubscriptionPlan>[] = [
                  {
                    label: t(i18nKeyContainer.table.viewDetails),
                    onClick: onView,
                    icon: Eye,
                  },
                  {
                    label: t(i18nKeyContainer.table.edit),
                    onClick: onEdit,
                    icon: Pencil,
                  },
                  {
                    label: plan.isActive
                      ? t(i18nKeyContainer.subscriptionPlans.actions.deactivate)
                      : t(i18nKeyContainer.subscriptionPlans.actions.activate),
                    onClick: plan.isActive ? onDeactivate : onActivate,
                    icon: plan.isActive ? ToggleLeft : ToggleRight,
                  },
                ];

                return (
                  <TableRow key={plan.id} className="border-slate-100 hover:bg-slate-50">
                    <TableCell className="font-medium text-slate-900">{plan.name}</TableCell>
                    <TableCell className="text-slate-600">{plan.slug}</TableCell>
                    <TableCell className="text-slate-700">
                      {formatCurrency(plan.amount, plan.currency)}
                    </TableCell>
                    <TableCell className="text-slate-700">
                      {formatInterval(plan.billingInterval, plan.intervalCount)}
                    </TableCell>
                    <TableCell className="text-slate-700">{plan.trialDays}</TableCell>
                    <TableCell>
                      <StatusBadge
                        status={
                          plan.isActive
                            ? t(i18nKeyContainer.subscriptionPlans.status.active)
                            : t(i18nKeyContainer.subscriptionPlans.status.inactive)
                        }
                        variant={plan.isActive ? "success" : "secondary"}
                      />
                    </TableCell>
                    <TableCell>
                      <DateCell date={plan.createdOnUtc} showIcon={false} />
                    </TableCell>
                    <TableCell className="text-right">
                      <DataTableRowActions row={plan} actions={actions} />
                    </TableCell>
                  </TableRow>
                );
              })}
            </TableBody>
          </Table>
        </CardContent>
      </Card>
    </div>
  );
}
