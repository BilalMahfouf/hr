import { useQuery } from "@tanstack/react-query";
import {
  Activity,
  ArrowLeft,
  Cable,
  Calendar,
  Fingerprint,
  Hash,
  type LucideIcon,
  Wifi,
} from "lucide-react";
import { useTranslation } from "react-i18next";
import { useNavigate, useParams } from "react-router-dom";
import { StatusBadge } from "@/components/tables";
import { Button } from "@/components/ui/button";
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from "@/components/ui/card";
import i18nKeyContainer from "@/lib/i18n/keyContainer";
import attendanceMachineApi from "./attendance-machine-api";

interface DetailRowProps {
  icon: LucideIcon;
  label: string;
  value: React.ReactNode;
  mono?: boolean;
}

function DetailRow({ icon: Icon, label, value, mono }: DetailRowProps) {
  return (
    <div className="flex items-center justify-between gap-4 py-4">
      <div className="flex items-center gap-3">
        <div className="flex h-9 w-9 items-center justify-center rounded-lg bg-slate-100 text-slate-500">
          <Icon className="h-4 w-4" />
        </div>
        <span className="text-sm font-medium text-slate-500">{label}</span>
      </div>
      <span
        className={`text-sm font-semibold text-slate-800 ${mono ? "font-mono" : ""}`}
      >
        {value}
      </span>
    </div>
  );
}

export default function ViewMachinePage() {
  const { machineId = "" } = useParams<{ machineId: string }>();
  const { t, i18n } = useTranslation();
  const isRtl = i18n.language === "ar";
  const navigate = useNavigate();

  const { data: machine, isLoading, isError } = useQuery({
    queryKey: ["machine", machineId],
    queryFn: () => attendanceMachineApi.getMachineById(machineId),
    enabled: machineId !== "",
  });

  if (isLoading) {
    return (
      <div dir={isRtl ? "rtl" : "ltr"}>
        <div className="mb-6">
          <h1 className="text-2xl font-bold text-slate-900">
            {t(i18nKeyContainer.machines.view.title)}
          </h1>
          <p className="text-slate-500">{t(i18nKeyContainer.machines.view.description)}</p>
        </div>
        <Card className="mx-auto max-w-2xl border-slate-200 bg-white shadow-sm">
          <CardContent className="space-y-3 p-6">
            <div className="h-11 rounded-lg bg-slate-100 animate-pulse" />
            <div className="h-11 rounded-lg bg-slate-100 animate-pulse" />
          </CardContent>
        </Card>
      </div>
    );
  }

  if (isError || !machine) {
    return (
      <div dir={isRtl ? "rtl" : "ltr"}>
        <div className="rounded-lg border border-red-200 bg-red-50 p-4 text-sm text-red-700">
          <p className="font-medium">{t(i18nKeyContainer.machines.notFound)}</p>
          <p className="mt-1 text-red-600">{t(i18nKeyContainer.machines.notFoundDesc)}</p>
        </div>
      </div>
    );
  }

  const statusLabel = machine.isActive
    ? t(i18nKeyContainer.machines.status.active)
    : t(i18nKeyContainer.machines.status.inactive);

  const createdOn = machine.createdOnUtc
    ? new Date(machine.createdOnUtc).toLocaleString(i18n.language, {
        year: "numeric",
        month: "long",
        day: "numeric",
        hour: "2-digit",
        minute: "2-digit",
      })
    : "—";

  return (
    <div dir={isRtl ? "rtl" : "ltr"}>
      <div className="mb-6 flex flex-col items-start justify-between gap-3 sm:flex-row sm:items-center">
        <div>
          <h1 className="text-2xl font-bold text-slate-900">
            {t(i18nKeyContainer.machines.view.title)}
          </h1>
          <p className="text-slate-500">{t(i18nKeyContainer.machines.view.description)}</p>
        </div>

        <Button
          variant="white"
          className="h-10"
          onClick={() => navigate("/machines")}
        >
          <ArrowLeft className="h-4 w-4 rtl:rotate-180" />
          {t(i18nKeyContainer.common.back)}
        </Button>
      </div>

      <Card className="mx-auto max-w-2xl border-slate-200 bg-white shadow-sm">
        <CardHeader className="border-b border-slate-100">
          <CardTitle className="text-lg text-slate-900">
            {t(i18nKeyContainer.machines.view.title)}
          </CardTitle>
          <CardDescription className="text-sm text-slate-500">
            {machine.ipAddress}
          </CardDescription>
        </CardHeader>
        <CardContent className="divide-y divide-slate-100 px-6">
          <DetailRow
            icon={Fingerprint}
            label={t(i18nKeyContainer.machines.view.machineId)}
            value={machine.machineId}
            mono
          />
          <DetailRow
            icon={Hash}
            label={t(i18nKeyContainer.machines.view.machineNumber)}
            value={machine.machineNumber}
          />
          <DetailRow
            icon={Wifi}
            label={t(i18nKeyContainer.machines.view.ipAddress)}
            value={machine.ipAddress}
            mono
          />
          <DetailRow
            icon={Cable}
            label={t(i18nKeyContainer.machines.view.port)}
            value={machine.port}
          />
          <DetailRow
            icon={Activity}
            label={t(i18nKeyContainer.machines.view.status)}
            value={
              <StatusBadge
                status={statusLabel}
                variant={machine.isActive ? "success" : "secondary"}
              />
            }
          />
          <DetailRow
            icon={Calendar}
            label={t(i18nKeyContainer.machines.view.createdOn)}
            value={createdOn}
          />
        </CardContent>
      </Card>
    </div>
  );
}