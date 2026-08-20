import { useQuery } from "@tanstack/react-query";
import {
  ArrowLeft,
  Calendar,
  Clock,
  Fingerprint,
  Hash,
  Server,
  type LucideIcon,
  User,
  Wifi,
} from "lucide-react";
import { useTranslation } from "react-i18next";
import { useNavigate, useParams } from "react-router-dom";
import { Button } from "@/components/ui/button";
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from "@/components/ui/card";
import i18nKeyContainer from "@/lib/i18n/keyContainer";
import attendanceApi from "./attendance-api";

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

function formatDateTime(iso: string, language: string): string {
  return new Date(iso).toLocaleString(language, {
    year: "numeric",
    month: "long",
    day: "numeric",
    hour: "2-digit",
    minute: "2-digit",
  });
}

export default function ViewPunchPage() {
  const { punchId = "" } = useParams<{ punchId: string }>();
  const { t, i18n } = useTranslation();
  const isRtl = i18n.language === "ar";
  const navigate = useNavigate();

  const { data: punch, isLoading, isError } = useQuery({
    queryKey: ["attendance-punch", punchId],
    queryFn: () => attendanceApi.getPunchById(punchId),
    enabled: punchId !== "",
  });

  if (isLoading) {
    return (
      <div dir={isRtl ? "rtl" : "ltr"}>
        <div className="mb-6">
          <h1 className="text-2xl font-bold text-slate-900">
            {t(i18nKeyContainer.attendance.punches.view.title)}
          </h1>
          <p className="text-slate-500">
            {t(i18nKeyContainer.attendance.punches.view.description)}
          </p>
        </div>
        <Card className="max-w-2xl border-slate-200 bg-white shadow-sm">
          <CardContent className="space-y-3 p-6">
            <div className="h-11 rounded-lg bg-slate-100 animate-pulse" />
            <div className="h-11 rounded-lg bg-slate-100 animate-pulse" />
          </CardContent>
        </Card>
      </div>
    );
  }

  if (isError || !punch) {
    return (
      <div dir={isRtl ? "rtl" : "ltr"}>
        <div className="rounded-lg border border-red-200 bg-red-50 p-4 text-sm text-red-700">
          <p className="font-medium">
            {t(i18nKeyContainer.attendance.punches.notFound)}
          </p>
          <p className="mt-1 text-red-600">
            {t(i18nKeyContainer.attendance.punches.notFoundDesc)}
          </p>
        </div>
      </div>
    );
  }

  return (
    <div dir={isRtl ? "rtl" : "ltr"}>
      <div className="mb-6 flex flex-col items-start justify-between gap-3 sm:flex-row sm:items-center">
        <div>
          <h1 className="text-2xl font-bold text-slate-900">
            {t(i18nKeyContainer.attendance.punches.view.title)}
          </h1>
          <p className="text-slate-500">
            {t(i18nKeyContainer.attendance.punches.view.description)}
          </p>
        </div>

        <Button
          variant="outline"
          className="h-10 cursor-pointer border-slate-200 bg-white hover:bg-slate-50"
          onClick={() => navigate("/attendance")}
        >
          <ArrowLeft className="h-4 w-4 rtl:rotate-180" />
          {t(i18nKeyContainer.common.back)}
        </Button>
      </div>

      <Card className="max-w-2xl border-slate-200 bg-white shadow-sm">
        <CardHeader className="border-b border-slate-100">
          <CardTitle className="text-lg text-slate-900">
            {t(i18nKeyContainer.attendance.punches.view.title)}
          </CardTitle>
          <CardDescription className="text-sm text-slate-500">
            {punch.employeeFullName ?? punch.employeeId}
          </CardDescription>
        </CardHeader>
        <CardContent className="divide-y divide-slate-100 px-6">
          <DetailRow
            icon={Fingerprint}
            label={t(i18nKeyContainer.attendance.punches.view.punchId)}
            value={punch.punchId}
            mono
          />
          <DetailRow
            icon={Server}
            label={t(i18nKeyContainer.attendance.punches.view.machineId)}
            value={punch.machineId}
            mono
          />
          <DetailRow
            icon={Wifi}
            label={t(i18nKeyContainer.attendance.punches.view.machineIp)}
            value={punch.machineIp ?? "—"}
            mono
          />
          <DetailRow
            icon={Hash}
            label={t(i18nKeyContainer.attendance.punches.view.employeeId)}
            value={punch.employeeId}
            mono
          />
          <DetailRow
            icon={User}
            label={t(i18nKeyContainer.attendance.punches.view.employeeFullName)}
            value={punch.employeeFullName ?? "—"}
          />
          <DetailRow
            icon={Clock}
            label={t(i18nKeyContainer.attendance.punches.view.punchOccurredOn)}
            value={formatDateTime(punch.punchOccurredOnUtc, i18n.language)}
          />
          <DetailRow
            icon={Calendar}
            label={t(i18nKeyContainer.attendance.punches.view.createdOn)}
            value={formatDateTime(punch.createdOnUtc, i18n.language)}
          />
        </CardContent>
      </Card>
    </div>
  );
}