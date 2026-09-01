import { useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useTranslation } from "react-i18next";

import { Button } from "@/components/ui/button";
import { useToast } from "@/hooks/use-toast";
import i18nKeyContainer from "@/lib/i18n/keyContainer";

import subscriptionPlanApi, { type SubscriptionPlan } from "../api/subscription-plan-api";
import SubscriptionPlanDataTable from "../subscription-plan-data-table";
import AddUpdateSubscriptionPlan from "../add-update-subscription-plan";
import ViewSubscriptionPlan from "../view-subscription-plan";

export default function SubscriptionPlansPage() {
  const [isFormOpen, setIsFormOpen] = useState(false);
  const [isViewOpen, setIsViewOpen] = useState(false);
  const [selectedPlanId, setSelectedPlanId] = useState<string | null>(null);
  const [editPlan, setEditPlan] = useState<SubscriptionPlan | null>(null);

  const queryClient = useQueryClient();
  const { t, i18n } = useTranslation();
  const { handleApiError, success } = useToast();
  const isRtl = i18n.language === "ar";

  const plansQuery = useQuery({
    queryKey: ["subscription-plans"],
    queryFn: subscriptionPlanApi.getAllSubscriptionPlans,
  });

  const activateMutation = useMutation({
    mutationFn: (planId: string) => subscriptionPlanApi.activateSubscriptionPlan(planId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["subscription-plans"] });
      success(i18nKeyContainer.toast.subscriptionPlan.activated, {
        description: i18nKeyContainer.toast.subscriptionPlan.activatedDesc,
      });
    },
    onError: (error) => {
      handleApiError(error, i18nKeyContainer.subscriptionPlans.genericError);
    },
  });

  const deactivateMutation = useMutation({
    mutationFn: (planId: string) => subscriptionPlanApi.deactivateSubscriptionPlan(planId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["subscription-plans"] });
      success(i18nKeyContainer.toast.subscriptionPlan.deactivated, {
        description: i18nKeyContainer.toast.subscriptionPlan.deactivatedDesc,
      });
    },
    onError: (error) => {
      handleApiError(error, i18nKeyContainer.subscriptionPlans.genericError);
    },
  });

  const handleAddPlan = () => {
    setEditPlan(null);
    setIsFormOpen(true);
  };

  const handleView = (plan: SubscriptionPlan) => {
    setSelectedPlanId(plan.id);
    setIsViewOpen(true);
  };

  const handleEdit = (plan: SubscriptionPlan) => {
    setEditPlan(plan);
    setIsFormOpen(true);
  };

  const handleActivate = (plan: SubscriptionPlan) => {
    activateMutation.mutate(plan.id);
  };

  const handleDeactivate = (plan: SubscriptionPlan) => {
    deactivateMutation.mutate(plan.id);
  };

  return (
    <div dir={isRtl ? "rtl" : "ltr"}>
      <div className="mb-6 flex flex-col items-start justify-between gap-3 sm:flex-row sm:items-center">
        <div>
          <h1 className="text-2xl font-bold text-slate-900">
            {t(i18nKeyContainer.subscriptionPlans.title)}
          </h1>
          <p className="text-slate-500">{t(i18nKeyContainer.subscriptionPlans.description)}</p>
        </div>

        <Button onClick={handleAddPlan}>
          {t(i18nKeyContainer.subscriptionPlans.addPlan)}
        </Button>
      </div>

      <SubscriptionPlanDataTable
        plans={plansQuery.data ?? []}
        isLoading={plansQuery.isLoading}
        isError={plansQuery.isError}
        onView={handleView}
        onEdit={handleEdit}
        onActivate={handleActivate}
        onDeactivate={handleDeactivate}
      />

      <AddUpdateSubscriptionPlan
        key={`${editPlan?.id ?? "new"}-${isFormOpen ? "open" : "closed"}`}
        open={isFormOpen}
        onClose={() => {
          setIsFormOpen(false);
          setEditPlan(null);
        }}
        planId={editPlan?.id}
        initialPlan={editPlan || undefined}
      />

      {selectedPlanId && (
        <ViewSubscriptionPlan
          open={isViewOpen}
          onClose={() => {
            setIsViewOpen(false);
            setSelectedPlanId(null);
          }}
          planId={selectedPlanId}
        />
      )}
    </div>
  );
}
