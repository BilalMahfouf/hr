import { useQuery } from "@tanstack/react-query";
import {
  ArrowLeft,
  Calendar,
  Clock,
  FileText,
  Hash,
  Pencil,
  RotateCcw,
  Shield,
  ShieldCheck,
  ShieldOff,
} from "lucide-react";
import { useTranslation } from "react-i18next";
import { useNavigate, useParams } from "react-router-dom";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { Skeleton } from "@/components/ui/skeleton";
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs";
import i18nKeyContainer from "@/lib/i18n/keyContainer";
import employeeGroupApi from "./employee-group-api";

function InfoRow({ label, value }: { label: string; value: string }) {
  return (
    <div className="flex items-center justify-between gap-4 rounded-lg border border-slate-200 bg-slate-50 px-4 py-3">
      <span className="text-sm text-slate-600">{label}</span>
      <span className="text-sm font-medium text-slate-900 text-right break-words">{value}</span>
    </div>
  );
}

function ReadOnlyField({ label, value }: { label: string; value: string }) {
  return (
    <div className="space-y-1.5">
      <p className="text-xs font-medium uppercase tracking-wide text-slate-500">{label}</p>
      <div className="rounded-lg border border-slate-200 bg-slate-50 px-3.5 py-2.5 text-sm text-slate-900">
        {value || "—"}
      </div>
    </div>
  );
}

export default function ViewEmployeeGroupPage() {
  const { id = "" } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const { t, i18n } = useTranslation();
  const isRtl = i18n.language === "ar";

  const { data: group, isLoading, isError } = useQuery({
    queryKey: ["employee-groups", id],
    queryFn: () => employeeGroupApi.getEmployeeGroupById(id),
    enabled: id !== "",
  });

  if (isLoading) {
    return (
      <div dir={isRtl ? "rtl" : "ltr"} className="mx-auto w-full max-w-[95%] space-y-6 py-2">
        <Skeleton className="h-8 w-64" />
        <Card className="border-slate-200 bg-white shadow-sm">
          <CardContent className="space-y-3 p-6">
            <Skeleton className="h-11 w-full" />
            <Skeleton className="h-11 w-full" />
            <Skeleton className="h-11 w-2/3" />
          </CardContent>
        </Card>
      </div>
    );
  }

  if (isError || !group) {
    return (
      <div dir={isRtl ? "rtl" : "ltr"} className="mx-auto w-full max-w-[95%] py-2">
        <div className="rounded-lg border border-red-200 bg-red-50 p-4 text-sm text-red-700">
          <p className="font-medium">{t(i18nKeyContainer.employeeGroups.notFound)}</p>
          <p className="mt-1 text-red-600">{t(i18nKeyContainer.employeeGroups.notFoundDesc)}</p>
        </div>
      </div>
    );
  }

  const rotationsSorted = [...group.rotationEntries].sort((a, b) => a.position - b.position);

  return (
    <div dir={isRtl ? "rtl" : "ltr"} className="mx-auto w-full max-w-[95%] space-y-6 py-2">
      {/* Header */}
      <div className="flex flex-col items-start justify-between gap-3 sm:flex-row sm:items-center">
        <div>
          <h1 className="text-2xl font-bold text-slate-900">{group.name}</h1>
          <p className="text-slate-500">{group.groupNumber ? `#${group.groupNumber}` : t(i18nKeyContainer.employeeGroups.editDescription)}</p>
        </div>
        <div className="flex items-center gap-2">
          <Button variant="white" className="h-10" onClick={() => navigate("/employee-groups")}>
            <ArrowLeft className="h-4 w-4 rtl:rotate-180" />
            {t(i18nKeyContainer.common.back)}
          </Button>
          <Button className="h-10" onClick={() => navigate(`/employee-groups/${group.id}/edit`)}>
            <Pencil className="h-4 w-4" />
            {t(i18nKeyContainer.common.edit)}
          </Button>
        </div>
      </div>

      {/* Group Info — read only */}
      <Card className="border-slate-200 bg-white shadow-sm">
        <CardHeader>
          <CardTitle className="text-lg text-slate-900 flex items-center gap-2">
            <Shield className="h-5 w-5 text-primary" />
            {t(i18nKeyContainer.employeeGroups.form.name)}
          </CardTitle>
          <CardDescription>{group.description ?? "—"}</CardDescription>
        </CardHeader>
        <CardContent className="space-y-4">
          <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
            <ReadOnlyField label={t(i18nKeyContainer.employeeGroups.list.name)} value={group.name} />
            <div className="space-y-1.5">
              <p className="text-xs font-medium uppercase tracking-wide text-slate-500">
                {t(i18nKeyContainer.employeeGroups.form.isSecurity)}
              </p>
              <div>
                {group.isSecurity ? (
                  <Badge className="bg-amber-100 text-amber-800 hover:bg-amber-100 gap-1.5">
                    <ShieldCheck className="h-3.5 w-3.5" />
                    {t(i18nKeyContainer.common.active)}
                  </Badge>
                ) : (
                  <Badge variant="secondary" className="gap-1.5">
                    <ShieldOff className="h-3.5 w-3.5" />
                    {t(i18nKeyContainer.common.inactive)}
                  </Badge>
                )}
              </div>
            </div>
          </div>

          {group.description && (
            <ReadOnlyField label={t(i18nKeyContainer.employeeGroups.form.description)} value={group.description} />
          )}

          <div className="grid grid-cols-1 gap-4 sm:grid-cols-3">
            <InfoRow label={t(i18nKeyContainer.employeeGroups.list.rotationStartDate)} value={group.rotationStartDate} />
            <InfoRow label={t(i18nKeyContainer.employeeGroups.list.schedulesCount)} value={String(group.workSchedules.length)} />
            <InfoRow label={t(i18nKeyContainer.employeeGroups.list.rotationsCount)} value={String(group.numberOfRotations)} />
          </div>

          <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
            <InfoRow label={t(i18nKeyContainer.machines.view.createdOn)} value={new Date(group.createdOnUtc).toLocaleDateString()} />
            <InfoRow label="Group Number" value={group.groupNumber ?? "—"} />
          </div>
        </CardContent>
      </Card>

      {/* Schedules & Rotations — read only */}
      <Tabs defaultValue="schedules">
        <TabsList className="bg-slate-100">
          <TabsTrigger value="schedules" className="cursor-pointer gap-2">
            <Clock className="h-4 w-4" />
            {t(i18nKeyContainer.employeeGroups.tabs.schedules)} ({group.workSchedules.length})
          </TabsTrigger>
          <TabsTrigger value="rotations" className="cursor-pointer gap-2">
            <RotateCcw className="h-4 w-4" />
            {t(i18nKeyContainer.employeeGroups.tabs.rotations)} ({rotationsSorted.length})
          </TabsTrigger>
        </TabsList>

        <TabsContent value="schedules" className="space-y-4 pt-4">
          {group.workSchedules.map((ws, index) => (
            <div key={ws.id} className="rounded-lg border border-slate-200 bg-white p-4 space-y-4 shadow-sm">
              <div className="flex items-center justify-between">
                <span className="font-medium text-slate-900">
                  {t(i18nKeyContainer.employeeGroups.form.workSchedules)} #{index + 1}
                </span>
                {ws.isActive ? (
                  <Badge variant="success">{t(i18nKeyContainer.common.active)}</Badge>
                ) : (
                  <Badge variant="secondary">{t(i18nKeyContainer.common.inactive)}</Badge>
                )}
              </div>

              <div className="grid grid-cols-2 gap-4 sm:grid-cols-4">
                <ReadOnlyField label={t(i18nKeyContainer.employeeGroups.form.schedule.shiftStartTime)} value={ws.shiftStartTime.slice(0, 5)} />
                <ReadOnlyField label={t(i18nKeyContainer.employeeGroups.form.schedule.shiftEndTime)} value={ws.shiftEndTime.slice(0, 5)} />
                <ReadOnlyField label={t(i18nKeyContainer.employeeGroups.form.schedule.endDayOffset)} value={String(ws.endDayOffset)} />
                <ReadOnlyField
                  label={t(i18nKeyContainer.employeeGroups.form.schedule.hasBreak)}
                  value={ws.breakStartTime && ws.breakEndTime ? `${ws.breakStartTime.slice(0, 5)} - ${ws.breakEndTime.slice(0, 5)}` : "—"}
                />
              </div>

              <div className="grid grid-cols-1 gap-4 sm:grid-cols-3">
                <ReadOnlyField
                  label={t(i18nKeyContainer.employeeGroups.form.schedule.allowedCheckInLatenessMinutes)}
                  value={String(ws.allowedCheckInLatenessMinutes)}
                />
                <ReadOnlyField
                  label={t(i18nKeyContainer.employeeGroups.form.schedule.allowedCheckOutEarlinessMinutes)}
                  value={String(ws.allowedCheckOutEarlinessMinutes)}
                />
                <div className="space-y-1.5">
                  <p className="text-xs font-medium uppercase tracking-wide text-slate-500 flex items-center gap-1.5">
                    <Calendar className="h-3.5 w-3.5" />
                    {t(i18nKeyContainer.machines.view.createdOn)}
                  </p>
                  <div className="rounded-lg border border-slate-200 bg-slate-50 px-3.5 py-2.5 text-sm text-slate-900">
                    {new Date(ws.createdOnUtc).toLocaleDateString()}
                  </div>
                </div>
              </div>
            </div>
          ))}

          {group.workSchedules.length === 0 && (
            <div className="rounded-lg border border-dashed border-slate-300 py-10 text-center text-slate-500">
              {t(i18nKeyContainer.employeeGroups.empty)}
            </div>
          )}
        </TabsContent>

        <TabsContent value="rotations" className="space-y-4 pt-4">
          {rotationsSorted.map((rotation, index) => {
            const schedule = rotation.workScheduleId
              ? group.workSchedules.find((ws) => ws.id === rotation.workScheduleId)
              : null;
            return (
              <div
                key={rotation.id}
                className="flex flex-col gap-3 rounded-lg border border-slate-200 bg-white p-4 shadow-sm sm:flex-row sm:items-center"
              >
                <span className="inline-flex h-9 min-w-9 items-center justify-center rounded-md bg-slate-100 px-2 font-medium text-slate-900">
                  {index + 1}
                </span>

                <div className="flex items-center gap-2">
                  {rotation.status === "Work" ? (
                    <Badge className="bg-primary/10 text-primary hover:bg-primary/10 gap-1">
                      <Clock className="h-3 w-3" />
                      {t(i18nKeyContainer.employeeGroups.form.rotation.work)}
                    </Badge>
                  ) : (
                    <Badge variant="secondary" className="gap-1">
                      <FileText className="h-3 w-3" />
                      {t(i18nKeyContainer.employeeGroups.form.rotation.rest)}
                    </Badge>
                  )}
                </div>

                {rotation.status === "Work" && schedule && (
                  <span className="text-sm text-slate-700">
                    {schedule.shiftStartTime.slice(0, 5)} - {schedule.shiftEndTime.slice(0, 5)}
                    {schedule.endDayOffset !== 0 ? " (+1)" : ""}
                  </span>
                )}

                {rotation.status === "Work" && !schedule && (
                  <span className="text-sm text-amber-600">—</span>
                )}

                <div className="flex-1" />

                <span className="text-xs text-slate-400 flex items-center gap-1">
                  <Hash className="h-3 w-3" /> {rotation.position}
                </span>
              </div>
            );
          })}

          {rotationsSorted.length === 0 && (
            <div className="rounded-lg border border-dashed border-slate-300 py-10 text-center text-slate-500">
              {t(i18nKeyContainer.employeeGroups.empty)}
            </div>
          )}
        </TabsContent>
      </Tabs>
    </div>
  );
}
