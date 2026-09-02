import { useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useTranslation } from "react-i18next";
import { useNavigate, useParams } from "react-router-dom";
import { Plus, Trash2, Clock, RotateCcw, ShieldCheck, ShieldOff, Save } from "lucide-react";

import { Button } from "@/components/ui/button";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs";
import { Checkbox } from "@/components/ui/checkbox";
import { Label } from "@/components/ui/label";
import ConfirmActionDialog from "@/components/ui/confirm-action-dialog";
import ConfirmDeleteDialog from "@/components/ui/confirm-delete-dialog";
import { Skeleton } from "@/components/ui/skeleton";
import { useToast } from "@/hooks/use-toast";
import i18nKeyContainer from "@/lib/i18n/keyContainer";
import employeeGroupApi, { type WorkScheduleResponse } from "./employee-group-api";
import TimeInput from "./TimeInput";
import NumberInput from "./NumberInput";
import SelectInput from "./SelectInput";

type ConfirmTarget =
  | { kind: "delete-group"; id: string; name: string }
  | { kind: "delete-schedule"; scheduleId: string }
  | { kind: "toggle-schedule"; schedule: WorkScheduleResponse };

interface ScheduleForm {
  key: string;
  id?: string;
  shiftStartTime: string;
  shiftEndTime: string;
  hasBreak: boolean;
  breakStartTime: string;
  breakEndTime: string;
  endDayOffset: string;
  allowedCheckInLatenessMinutes: string;
  allowedCheckOutEarlinessMinutes: string;
}

interface RotationForm {
  key: string;
  position: number;
  type: "Work" | "Rest";
  workScheduleId: string;
}

function toTime(value: string): string {
  return value.length === 5 ? `${value}:00` : value;
}

function fromTime(value: string): string {
  return value.length === 8 ? value.slice(0, 5) : value;
}

function toForm(schedule: WorkScheduleResponse): ScheduleForm {
  const hasBreak =
    schedule.breakStartTime !== null &&
    schedule.breakEndTime !== null &&
    fromTime(schedule.breakStartTime) !== "00:00" &&
    fromTime(schedule.breakEndTime) !== "00:00";
  return {
    key: schedule.id,
    id: schedule.id,
    shiftStartTime: fromTime(schedule.shiftStartTime),
    shiftEndTime: fromTime(schedule.shiftEndTime),
    hasBreak,
    breakStartTime: hasBreak ? fromTime(schedule.breakStartTime) : "",
    breakEndTime: hasBreak ? fromTime(schedule.breakEndTime) : "",
    endDayOffset: String(schedule.endDayOffset),
    allowedCheckInLatenessMinutes: String(schedule.allowedCheckInLatenessMinutes),
    allowedCheckOutEarlinessMinutes: String(schedule.allowedCheckOutEarlinessMinutes),
  };
}

export default function EditEmployeeGroupPage() {
  const { id = "" } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const { t, i18n } = useTranslation();
  const isRtl = i18n.language === "ar";
  const { handleApiError, success, warning } = useToast();

  const [schedules, setSchedules] = useState<ScheduleForm[]>([]);
  const [rotations, setRotations] = useState<RotationForm[]>([]);
  const [hydrated, setHydrated] = useState(false);
  const [confirmTarget, setConfirmTarget] = useState<ConfirmTarget | null>(null);
  const [rotationToDelete, setRotationToDelete] = useState<number | null>(null);

  const { data: group, isLoading, isError } = useQuery({
    queryKey: ["employee-groups", id],
    queryFn: () => employeeGroupApi.getEmployeeGroupById(id),
  });

  if (group && !hydrated) {
    setSchedules(group.workSchedules.map(toForm));
    setRotations(
      [...group.rotationEntries]
        .sort((a, b) => a.position - b.position)
        .map((r) => {
          const matchedSchedule = r.workScheduleId
            ? group.workSchedules.find((ws) => ws.id === r.workScheduleId)
            : null;
          return {
            key: r.id,
            position: r.position,
            type: r.status,
            workScheduleId: matchedSchedule ? matchedSchedule.id : "",
          };
        }),
    );
    setHydrated(true);
  }

  const invalidateGroup = () => {
    queryClient.invalidateQueries({ queryKey: ["employee-groups", id] });
    queryClient.invalidateQueries({ queryKey: ["employee-groups"] });
  };

  const deleteGroupMutation = useMutation({
    mutationFn: () => employeeGroupApi.deleteEmployeeGroup(id),
    onSuccess: () => {
      invalidateGroup();
      success(i18nKeyContainer.employeeGroups.toast.deleted, {
        description: i18nKeyContainer.employeeGroups.toast.deletedDesc,
      });
      navigate("/employee-groups");
    },
    onError: (error) => handleApiError(error, i18nKeyContainer.employeeGroups.genericError),
  });

  const toggleScheduleMutation = useMutation({
    mutationFn: (schedule: WorkScheduleResponse) =>
      schedule.isActive
        ? employeeGroupApi.deactivateWorkSchedule(id, schedule.id)
        : employeeGroupApi.activateWorkSchedule(id, schedule.id),
    onSuccess: (_, schedule) => {
      invalidateGroup();
      success(
        schedule.isActive
          ? i18nKeyContainer.employeeGroups.toast.scheduleDeactivated
          : i18nKeyContainer.employeeGroups.toast.scheduleActivated,
      );
      setConfirmTarget(null);
    },
    onError: (error) => {
      handleApiError(error, i18nKeyContainer.employeeGroups.genericError);
      setConfirmTarget(null);
    },
  });

  const deleteScheduleMutation = useMutation({
    mutationFn: (scheduleId: string) => employeeGroupApi.deleteWorkSchedule(id, scheduleId),
    onSuccess: () => {
      invalidateGroup();
      success(i18nKeyContainer.employeeGroups.toast.scheduleDeleted);
      setConfirmTarget(null);
    },
    onError: (error) => {
      handleApiError(error, i18nKeyContainer.employeeGroups.scheduleInUse);
      setConfirmTarget(null);
    },
  });

  const saveMutation = useMutation({
    mutationFn: () =>
      employeeGroupApi.replaceSchedulesAndRotations(id, {
        workSchedules: schedules.map((s) => ({
          shiftStartTime: toTime(s.shiftStartTime),
          shiftEndTime: toTime(s.shiftEndTime),
          breakStartTime: s.hasBreak ? toTime(s.breakStartTime) : null,
          breakEndTime: s.hasBreak ? toTime(s.breakEndTime) : null,
          endDayOffset: Number(s.endDayOffset),
          allowedCheckInLatenessMinutes: Number(s.allowedCheckInLatenessMinutes),
          allowedCheckOutEarlinessMinutes: Number(s.allowedCheckOutEarlinessMinutes),
        })),
        rotationEntries: rotations.map((r, index) => ({
          position: index + 1,
          workScheduleIndex:
            r.type === "Work" && r.workScheduleId
              ? schedules.findIndex((s) => s.key === r.workScheduleId)
              : null,
        })),
      }),
    onSuccess: () => {
      invalidateGroup();
      success(i18nKeyContainer.employeeGroups.toast.updated, {
        description: i18nKeyContainer.employeeGroups.toast.updatedDesc,
      });
    },
    onError: (error) => handleApiError(error, i18nKeyContainer.employeeGroups.genericError),
  });

  const validateSchedules = (): boolean => {
    for (const s of schedules) {
      if (!s.shiftStartTime || !s.shiftEndTime) {
        warning(i18nKeyContainer.errors.validation, {
          description: i18nKeyContainer.errors.validationDesc,
        });
        return false;
      }
      if (
        s.endDayOffset === "0" &&
        s.shiftStartTime >= s.shiftEndTime
      ) {
        warning(i18nKeyContainer.errors.validation, {
          description: i18nKeyContainer.errors.validationDesc,
        });
        return false;
      }
      if (s.hasBreak) {
        if (!s.breakStartTime || !s.breakEndTime) {
          warning(i18nKeyContainer.errors.validation, {
            description: i18nKeyContainer.errors.validationDesc,
          });
          return false;
        }
        if (
          s.endDayOffset === "0" &&
          (s.breakStartTime >= s.breakEndTime)
        ) {
          warning(i18nKeyContainer.errors.validation, {
            description: i18nKeyContainer.errors.validationDesc,
          });
          return false;
        }
      }
    }
    return true;
  };

  const handleSave = () => {
    if (schedules.length === 0 || rotations.length === 0) {
      warning(i18nKeyContainer.common.warning, {
        description: i18nKeyContainer.errors.validationDesc,
      });
      return;
    }
    if (!validateSchedules()) return;
    saveMutation.mutate();
  };

  const addSchedule = () => {
    setSchedules((prev) => [
      ...prev,
      {
        key: `new-${Date.now()}`,
        shiftStartTime: "08:00",
        shiftEndTime: "16:00",
        hasBreak: true,
        breakStartTime: "12:00",
        breakEndTime: "12:30",
        endDayOffset: "0",
        allowedCheckInLatenessMinutes: "15",
        allowedCheckOutEarlinessMinutes: "10",
      },
    ]);
  };

  const removeSchedule = (key: string) => {
    setSchedules((prev) => prev.filter((s) => s.key !== key));
    setRotations((prev) =>
      prev.map((r) =>
        r.type === "Work" && r.workScheduleId === key
          ? { ...r, workScheduleId: "" }
          : r,
      ),
    );
  };

  const updateSchedule = (key: string, field: keyof ScheduleForm, value: string) => {
    setSchedules((prev) =>
      prev.map((s) => (s.key === key ? { ...s, [field]: value } : s)),
    );
  };

  const toggleBreak = (key: string, checked: boolean) => {
    setSchedules((prev) =>
      prev.map((s) =>
        s.key === key
          ? {
              ...s,
              hasBreak: checked,
              breakStartTime: checked ? "12:00" : "",
              breakEndTime: checked ? "12:30" : "",
            }
          : s,
      ),
    );
  };

  const addRotation = (type: "Work" | "Rest") => {
    setRotations((prev) => [
      ...prev,
      {
        key: `new-${Date.now()}-${Math.random()}`,
        position: prev.length + 1,
        type,
        workScheduleId: type === "Work" ? (schedules[0]?.key ?? "") : "",
      },
    ]);
  };

  const removeRotation = (position: number) => {
    setRotations((prev) => prev.filter((_, index) => index !== position - 1).map((r, index) => ({ ...r, position: index + 1 })));
  };

  const scheduleOptions = schedules.map((s) => ({
    value: s.key,
    label: `${s.shiftStartTime} - ${s.shiftEndTime}${s.endDayOffset !== "0" ? " (+1)" : ""}`,
  }));

  const isConfirmPending =
    toggleScheduleMutation.isPending ||
    deleteScheduleMutation.isPending ||
    deleteGroupMutation.isPending;

  if (isLoading || (isError && !group)) {
    return (
      <div dir={isRtl ? "rtl" : "ltr"} className="mx-auto w-full max-w-[95%] space-y-6">
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
      <div dir={isRtl ? "rtl" : "ltr"} className="mx-auto w-full max-w-[95%]">
        <div className="rounded-lg border border-red-200 bg-red-50 p-4 text-sm text-red-700">
          <p className="font-medium">{t(i18nKeyContainer.employeeGroups.notFound)}</p>
          <p className="mt-1 text-red-600">{t(i18nKeyContainer.employeeGroups.notFoundDesc)}</p>
        </div>
      </div>
    );
  }

  const scheduleInUse = (schedule: WorkScheduleResponse) =>
    group.rotationEntries.some((entry) => entry.workScheduleId === schedule.id);

  return (
    <div dir={isRtl ? "rtl" : "ltr"} className="mx-auto w-full max-w-[95%] space-y-6 py-2">
      {/* Header */}
      <div className="flex flex-col items-start justify-between gap-3 sm:flex-row sm:items-center">
        <div>
          <h1 className="text-2xl font-bold text-slate-900">{group.name}</h1>
          <p className="text-slate-500">
            {t(i18nKeyContainer.employeeGroups.editDescription)}
          </p>
        </div>
        <Button
          type="button"
          variant="outline"
          onClick={() =>
            setConfirmTarget({ kind: "delete-group", id: group.id, name: group.name })
          }
          className="border-red-200 text-red-600 hover:bg-red-50 hover:text-red-700"
        >
          <Trash2 className="h-4 w-4" />
          {t(i18nKeyContainer.common.delete)}
        </Button>
      </div>

      {/* Group Info — readonly */}
      <Card className="border-slate-200 bg-white shadow-sm">
        <CardHeader>
          <CardTitle className="text-lg text-slate-900 flex items-center gap-2">
            <ShieldCheck className="h-5 w-5 text-primary" />
            {t(i18nKeyContainer.employees.view.personalInfoSection)}
          </CardTitle>
          <CardDescription>{group.description ?? "—"}</CardDescription>
        </CardHeader>
        <CardContent className="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-4">
          <InfoRow label={t(i18nKeyContainer.employeeGroups.list.name)} value={group.name} />
          <InfoRow
            label={t(i18nKeyContainer.employeeGroups.form.isSecurity)}
            value={
              group.isSecurity
                ? t(i18nKeyContainer.common.active)
                : t(i18nKeyContainer.common.inactive)
            }
          />
          <InfoRow label={t(i18nKeyContainer.employeeGroups.list.rotationStartDate)} value={group.rotationStartDate} />
          <InfoRow label={t(i18nKeyContainer.employeeGroups.list.rotationsCount)} value={String(group.numberOfRotations)} />
        </CardContent>
      </Card>

      {/* Schedules & Rotations */}
      <Tabs defaultValue="schedules">
        <TabsList className="bg-slate-100">
          <TabsTrigger value="schedules" className="cursor-pointer gap-2">
            <Clock className="h-4 w-4" />
            {t(i18nKeyContainer.employeeGroups.tabs.schedules)} ({schedules.length})
          </TabsTrigger>
          <TabsTrigger value="rotations" className="cursor-pointer gap-2">
            <RotateCcw className="h-4 w-4" />
            {t(i18nKeyContainer.employeeGroups.tabs.rotations)} ({rotations.length})
          </TabsTrigger>
        </TabsList>

        {/* Schedules Tab */}
        <TabsContent value="schedules" className="space-y-4 pt-4">
          <div className="flex justify-end">
            <Button type="button" size="sm" onClick={addSchedule} className="gap-1">
              <Plus className="h-4 w-4" />
              {t(i18nKeyContainer.employeeGroups.form.addSchedule)}
            </Button>
          </div>

          {schedules.map((schedule, index) => {
            const serverSchedule =
              schedule.id != null
                ? group.workSchedules.find((ws) => ws.id === schedule.id)
                : undefined;

            return (
              <div key={schedule.key} className="rounded-lg border border-slate-200 bg-white p-4 space-y-4 shadow-sm">
                <div className="flex items-center justify-between">
                  <div className="flex items-center gap-3">
                    <span className="font-medium text-slate-900">
                      {t(i18nKeyContainer.employeeGroups.form.workSchedules)} #{index + 1}
                    </span>
                    {serverSchedule &&
                      (serverSchedule.isActive ? (
                        <Badge variant="success">{t(i18nKeyContainer.common.active)}</Badge>
                      ) : (
                        <Badge variant="secondary">{t(i18nKeyContainer.common.inactive)}</Badge>
                      ))}
                  </div>
                  <div className="flex items-center gap-1">
                    {serverSchedule && (
                      <>
                        <Button
                          type="button"
                          variant="ghost"
                          size="sm"
                          disabled={isConfirmPending}
                          onClick={() => setConfirmTarget({ kind: "toggle-schedule", schedule: serverSchedule })}
                          className="gap-1 text-slate-600"
                        >
                          {serverSchedule.isActive ? (
                            <>
                              <ShieldOff className="h-4 w-4" />
                              {t(i18nKeyContainer.employeeGroups.form.schedule.deactivate)}
                            </>
                          ) : (
                            <>
                              <ShieldCheck className="h-4 w-4" />
                              {t(i18nKeyContainer.employeeGroups.form.schedule.activate)}
                            </>
                          )}
                        </Button>
                        <Button
                          type="button"
                          variant="ghost"
                          size="icon"
                          title={
                            scheduleInUse(serverSchedule)
                              ? t(i18nKeyContainer.employeeGroups.scheduleInUse)
                              : undefined
                          }
                          disabled={isConfirmPending}
                          onClick={() => setConfirmTarget({ kind: "delete-schedule", scheduleId: serverSchedule.id })}
                          className="text-red-600 hover:text-red-700 hover:bg-red-50"
                        >
                          <Trash2 className="h-4 w-4" />
                        </Button>
                      </>
                    )}
                    {!serverSchedule && (
                      <Button
                        type="button"
                        variant="ghost"
                        size="icon"
                        onClick={() => removeSchedule(schedule.key)}
                        className="text-red-600 hover:text-red-700 hover:bg-red-50"
                      >
                        <Trash2 className="h-4 w-4" />
                      </Button>
                    )}
                  </div>
                </div>

                <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-4">
                  <TimeInput
                    id={`edit-schedule-${index}-shiftStart`}
                    label={t(i18nKeyContainer.employeeGroups.form.schedule.shiftStartTime)}
                    value={schedule.shiftStartTime}
                    onChange={(v) => updateSchedule(schedule.key, "shiftStartTime", v)}
                    required
                  />
                  <TimeInput
                    id={`edit-schedule-${index}-shiftEnd`}
                    label={t(i18nKeyContainer.employeeGroups.form.schedule.shiftEndTime)}
                    value={schedule.shiftEndTime}
                    onChange={(v) => updateSchedule(schedule.key, "shiftEndTime", v)}
                    required
                  />
                  <div className="space-y-1.5">
                    <Label className="text-sm font-medium text-slate-700 flex items-center gap-2 cursor-pointer">
                      <Checkbox
                        checked={schedule.hasBreak}
                        onCheckedChange={(checked) => toggleBreak(schedule.key, checked === true)}
                        className="h-4 w-4 rounded border-slate-300 text-primary focus:ring-primary"
                      />
                      {t(i18nKeyContainer.employeeGroups.form.schedule.hasBreak)}
                    </Label>
                  </div>
                </div>
                {schedule.hasBreak && (
                  <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
                    <TimeInput
                      id={`edit-schedule-${index}-breakStart`}
                      label={t(i18nKeyContainer.employeeGroups.form.schedule.breakStartTime)}
                      value={schedule.breakStartTime}
                      onChange={(v) => updateSchedule(schedule.key, "breakStartTime", v)}
                      required
                    />
                    <TimeInput
                      id={`edit-schedule-${index}-breakEnd`}
                      label={t(i18nKeyContainer.employeeGroups.form.schedule.breakEndTime)}
                      value={schedule.breakEndTime}
                      onChange={(v) => updateSchedule(schedule.key, "breakEndTime", v)}
                      required
                    />
                  </div>
                )}

                <div className="grid grid-cols-1 gap-4 sm:grid-cols-3">
                  <NumberInput
                    id={`edit-schedule-${index}-offset`}
                    label={t(i18nKeyContainer.employeeGroups.form.schedule.endDayOffset)}
                    value={schedule.endDayOffset}
                    onChange={(v) => updateSchedule(schedule.key, "endDayOffset", v)}
                    min={0}
                    required
                  />
                  <NumberInput
                    id={`edit-schedule-${index}-lateness`}
                    label={t(i18nKeyContainer.employeeGroups.form.schedule.allowedCheckInLatenessMinutes)}
                    value={schedule.allowedCheckInLatenessMinutes}
                    onChange={(v) => updateSchedule(schedule.key, "allowedCheckInLatenessMinutes", v)}
                    min={0}
                    required
                  />
                  <NumberInput
                    id={`edit-schedule-${index}-earliness`}
                    label={t(i18nKeyContainer.employeeGroups.form.schedule.allowedCheckOutEarlinessMinutes)}
                    value={schedule.allowedCheckOutEarlinessMinutes}
                    onChange={(v) => updateSchedule(schedule.key, "allowedCheckOutEarlinessMinutes", v)}
                    min={0}
                    required
                  />
                </div>
              </div>
            );
          })}

          {schedules.length === 0 && (
            <div className="rounded-lg border border-dashed border-slate-300 py-10 text-center text-slate-500">
              {t(i18nKeyContainer.employeeGroups.empty)}
            </div>
          )}
        </TabsContent>

        {/* Rotations Tab */}
        <TabsContent value="rotations" className="space-y-4 pt-4">
          <div className="flex justify-end gap-2">
            <Button type="button" size="sm" onClick={() => addRotation("Work")} className="gap-1">
              <Plus className="h-4 w-4" />
              {t(i18nKeyContainer.employeeGroups.form.addWorkRotation)}
            </Button>
            <Button type="button" size="sm" onClick={() => addRotation("Rest")} className="gap-1">
              <Plus className="h-4 w-4" />
              {t(i18nKeyContainer.employeeGroups.form.addRestRotation)}
            </Button>
          </div>

          {rotations.map((rotation, index) => (
            <div
              key={rotation.key}
              className="flex flex-col gap-3 rounded-lg border border-slate-200 bg-white p-4 shadow-sm sm:flex-row sm:items-center"
            >
              <span className="inline-flex h-9 min-w-9 items-center justify-center rounded-md bg-slate-100 px-2 font-medium text-slate-900">
                {index + 1}
              </span>

              <SelectInput
                id={`edit-rotation-${index}-type`}
                label=""
                value={rotation.type}
                onChange={(v) =>
                  setRotations((prev) =>
                    prev.map((r, i) =>
                      i === index
                        ? { ...r, type: v as "Work" | "Rest", workScheduleId: v === "Rest" ? "" : (schedules[0]?.key ?? "") }
                        : r,
                    ),
                  )
                }
                options={[
                  { value: "Work", label: t(i18nKeyContainer.employeeGroups.form.rotation.work) },
                  { value: "Rest", label: t(i18nKeyContainer.employeeGroups.form.rotation.rest) },
                ]}
                className="w-full sm:w-36"
              />

              {rotation.type === "Work" && (
                <SelectInput
                  id={`edit-rotation-${index}-schedule`}
                  label=""
                  value={rotation.workScheduleId}
                  onChange={(v) =>
                    setRotations((prev) =>
                      prev.map((r, i) => (i === index ? { ...r, workScheduleId: v } : r)),
                    )
                  }
                  options={scheduleOptions}
                  placeholder={t(i18nKeyContainer.employeeGroups.form.rotation.selectSchedule)}
                  className="w-full sm:flex-1"
                />
              )}

              <div className="flex-1" />

              <Button
                type="button"
                variant="ghost"
                size="icon"
                onClick={() => setRotationToDelete(index + 1)}
                className="text-red-600 hover:text-red-700 hover:bg-red-50"
              >
                <Trash2 className="h-4 w-4" />
              </Button>
            </div>
          ))}

          {rotations.length === 0 && (
            <div className="rounded-lg border border-dashed border-slate-300 py-10 text-center text-slate-500">
              {t(i18nKeyContainer.employeeGroups.empty)}
            </div>
          )}
        </TabsContent>
      </Tabs>

      {/* Actions */}
      <div className="flex gap-3 justify-end pb-4">
        <Button
          type="button"
          variant="white"
          className="h-11 px-6"
          onClick={() => navigate("/employee-groups")}
          disabled={saveMutation.isPending}
        >
          {t(i18nKeyContainer.common.cancel)}
        </Button>
        <Button
          type="button"
          className="h-11 px-6"
          onClick={handleSave}
          disabled={saveMutation.isPending}
        >
          <Save className="h-4 w-4" />
          {saveMutation.isPending
            ? t(i18nKeyContainer.common.saving)
            : t(i18nKeyContainer.common.save)}
        </Button>
      </div>

      {/* Delete group confirmation */}
      <ConfirmDeleteDialog
        open={confirmTarget?.kind === "delete-group"}
        onClose={() => !isConfirmPending && setConfirmTarget(null)}
        onConfirm={() => confirmTarget?.kind === "delete-group" && deleteGroupMutation.mutate()}
        title={t(i18nKeyContainer.employeeGroups.confirm.deleteTitle)}
        description={t(i18nKeyContainer.employeeGroups.confirm.deleteDescription)}
        itemName={confirmTarget?.kind === "delete-group" ? confirmTarget.name : undefined}
        isLoading={deleteGroupMutation.isPending}
      />

      {/* Delete schedule confirmation */}
      <ConfirmActionDialog
        open={confirmTarget?.kind === "delete-schedule"}
        onClose={() => !isConfirmPending && setConfirmTarget(null)}
        onConfirm={() => confirmTarget?.kind === "delete-schedule" && deleteScheduleMutation.mutate(confirmTarget.scheduleId)}
        title={t(i18nKeyContainer.employeeGroups.confirm.deleteScheduleTitle)}
        description={t(i18nKeyContainer.employeeGroups.confirm.deleteScheduleDescription)}
        isLoading={deleteScheduleMutation.isPending}
        confirmAction={t(i18nKeyContainer.common.delete)}
      />

      {/* Toggle schedule confirmation */}
      <ConfirmActionDialog
        open={confirmTarget?.kind === "toggle-schedule"}
        onClose={() => !isConfirmPending && setConfirmTarget(null)}
        onConfirm={() =>
          confirmTarget?.kind === "toggle-schedule" && toggleScheduleMutation.mutate(confirmTarget.schedule)
        }
        title={
          confirmTarget?.kind === "toggle-schedule" && confirmTarget.schedule.isActive
            ? t(i18nKeyContainer.employeeGroups.form.schedule.deactivate)
            : t(i18nKeyContainer.employeeGroups.form.schedule.activate)
        }
        description={
          confirmTarget?.kind === "toggle-schedule"
            ? `${confirmTarget.schedule.shiftStartTime} - ${confirmTarget.schedule.shiftEndTime}`
            : ""
        }
        isLoading={toggleScheduleMutation.isPending}
        confirmAction={t(i18nKeyContainer.common.confirm)}
      />

      {/* Delete rotation confirmation */}
      <ConfirmActionDialog
        open={rotationToDelete !== null}
        onClose={() => setRotationToDelete(null)}
        onConfirm={() => rotationToDelete !== null && removeRotation(rotationToDelete)}
        title={t(i18nKeyContainer.employeeGroups.confirm.deleteRotationTitle)}
        description={t(i18nKeyContainer.employeeGroups.confirm.deleteRotationDescription)}
        itemName={rotationToDelete !== null ? `#${rotationToDelete}` : undefined}
        confirmAction={t(i18nKeyContainer.common.delete)}
      />
    </div>
  );
}

function InfoRow({ label, value }: { label: string; value: string }) {
  return (
    <div className="flex items-center justify-between gap-4 rounded-lg border border-slate-200 bg-slate-50 px-4 py-3">
      <span className="text-sm text-slate-600">{label}</span>
      <span className="text-sm font-medium text-slate-900 text-right">{value}</span>
    </div>
  );
}
