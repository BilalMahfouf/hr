import { useState } from "react";
import { useMutation, useQueryClient } from "@tanstack/react-query";
import { useTranslation } from "react-i18next";
import { useNavigate } from "react-router-dom";
import { Button } from "@/components/ui/button";
import ConfirmActionDialog from "@/components/ui/confirm-action-dialog";
import { useToast } from "@/hooks/use-toast";
import i18nKeyContainer from "@/lib/i18n/keyContainer";
import attendanceMachineApi, { type MachineRecord } from "./attendance-machine-api";
import MachinesDataTable from "./machines-data-table";

interface ConfirmTarget {
  machine: MachineRecord;
  action: "activate" | "deactivate";
}

export default function MachinesPage() {
  const { t, i18n } = useTranslation();
  const isRtl = i18n.language === "ar";
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const { handleApiError, success } = useToast();

  const [confirmTarget, setConfirmTarget] = useState<ConfirmTarget | null>(null);

  const activateMutation = useMutation({
    mutationFn: (machineId: string) => attendanceMachineApi.activateMachine(machineId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["machines"] });
      success(i18nKeyContainer.toast.machine.activated, {
        description: i18nKeyContainer.toast.machine.activatedDesc,
      });
      setConfirmTarget(null);
    },
    onError: (error) => {
      handleApiError(error, i18nKeyContainer.machines.genericError);
      setConfirmTarget(null);
    },
  });

  const deactivateMutation = useMutation({
    mutationFn: (machineId: string) => attendanceMachineApi.deactivateMachine(machineId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["machines"] });
      success(i18nKeyContainer.toast.machine.deactivated, {
        description: i18nKeyContainer.toast.machine.deactivatedDesc,
      });
      setConfirmTarget(null);
    },
    onError: (error) => {
      handleApiError(error, i18nKeyContainer.machines.genericError);
      setConfirmTarget(null);
    },
  });

  const handleEdit = (machine: MachineRecord) => {
    navigate(`/machines/${machine.machineId}/edit`);
  };

  const handleActivate = (machine: MachineRecord) => {
    setConfirmTarget({ machine, action: "activate" });
  };

  const handleDeactivate = (machine: MachineRecord) => {
    setConfirmTarget({ machine, action: "deactivate" });
  };

  const isActivating = confirmTarget?.action === "activate" && activateMutation.isPending;
  const isDeactivating = confirmTarget?.action === "deactivate" && deactivateMutation.isPending;

  const handleConfirm = () => {
    if (!confirmTarget) return;

    if (confirmTarget.action === "activate") {
      activateMutation.mutate(confirmTarget.machine.machineId);
    } else {
      deactivateMutation.mutate(confirmTarget.machine.machineId);
    }
  };

  const isConfirming = isActivating || isDeactivating;

  return (
    <div dir={isRtl ? "rtl" : "ltr"}>
      <div className="mb-6 flex flex-col items-start justify-between gap-3 sm:flex-row sm:items-center">
        <div>
          <h1 className="text-2xl font-bold text-slate-900">
            {t(i18nKeyContainer.machines.title)}
          </h1>
          <p className="text-slate-500">{t(i18nKeyContainer.machines.description)}</p>
        </div>

        <Button onClick={() => navigate("/machines/create")} className="cursor-pointer">
          {t(i18nKeyContainer.machines.addMachine)}
        </Button>
      </div>

      <MachinesDataTable
        onEdit={handleEdit}
        onActivate={handleActivate}
        onDeactivate={handleDeactivate}
      />

      <ConfirmActionDialog
        open={confirmTarget !== null}
        onClose={() => {
          if (!isConfirming) setConfirmTarget(null);
        }}
        onConfirm={handleConfirm}
        title={t(
          confirmTarget?.action === "activate"
            ? i18nKeyContainer.machines.confirm.activateTitle
            : i18nKeyContainer.machines.confirm.deactivateTitle,
        )}
        description={t(
          confirmTarget?.action === "activate"
            ? i18nKeyContainer.machines.confirm.activateDescription
            : i18nKeyContainer.machines.confirm.deactivateDescription,
        )}
        itemName={confirmTarget?.machine.ipAddress}
        isLoading={isConfirming}
        confirmAction={t(
          confirmTarget?.action === "activate"
            ? i18nKeyContainer.machines.confirm.activateConfirm
            : i18nKeyContainer.machines.confirm.deactivateConfirm,
        )}
      />
    </div>
  );
}