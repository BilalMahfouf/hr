import { useState } from "react";
import { useMutation, useQueryClient } from "@tanstack/react-query";
import { useTranslation } from "react-i18next";
import { useNavigate } from "react-router-dom";
import { Plus, Trash2, Calendar, Clock, Shield, RotateCcw } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { Label } from "@/components/ui/label";
import { Checkbox } from "@/components/ui/checkbox";
import { Separator } from "@/components/ui/separator";
import { useToast } from "@/hooks/use-toast";
import i18nKeyContainer from "@/lib/i18n/keyContainer";
import employeeGroupApi, {
  type CreateEmployeeGroupRequest,
  type CreateWorkScheduleRequest,
  type CreateRotationEntryRequest,
} from "./employee-group-api";
import DateInput from "./DateInput";
import TimeInput from "./TimeInput";
import NumberInput from "./NumberInput";
import TextArea from "./TextArea";
import SelectInput from "./SelectInput";
import { cn } from "@/lib/utils";

interface WorkScheduleForm {
  shiftStartTime: string;
  shiftEndTime: string;
  breakStartTime: string;
  breakEndTime: string;
  endDayOffset: string;
  allowedCheckInLatenessMinutes: string;
  allowedCheckOutEarlinessMinutes: string;
}

interface RotationForm {
  position: string;
  type: "Work" | "Rest";
  workScheduleId: string;
}

function generateScheduleId() {
  return `schedule-${Date.now()}-${Math.random().toString(36).substr(2, 9)}`;
}

function generateRotationId() {
  return `rotation-${Date.now()}-${Math.random().toString(36).substr(2, 9)}`;
}

function getDefaultSchedule(): WorkScheduleForm {
  return {
    shiftStartTime: "07:00",
    shiftEndTime: "15:00",
    breakStartTime: "11:00",
    breakEndTime: "11:30",
    endDayOffset: "0",
    allowedCheckInLatenessMinutes: "15",
    allowedCheckOutEarlinessMinutes: "10",
  };
}

function getDefaultRotation(schedules: WorkScheduleForm[]): RotationForm {
  return {
    position: String(schedules.length + 1),
    type: "Work",
    workScheduleId: "",
  };
}

export default function CreateEmployeeGroupPage() {
  const { t, i18n } = useTranslation();
  const isRtl = i18n.language === "ar";
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const { handleApiError, success, warning } = useToast();

  const [name, setName] = useState("");
  const [isSecurity, setIsSecurity] = useState(false);
  const [description, setDescription] = useState("");
  const [rotationStartDate, setRotationStartDate] = useState("");
  const [workSchedules, setWorkSchedules] = useState<WorkScheduleForm[]>([getDefaultSchedule()]);
  const [rotations, setRotations] = useState<RotationForm[]>([]);
  const [errors, setErrors] = useState<Record<string, string>>({});

  const mutation = useMutation({
    mutationFn: (request: CreateEmployeeGroupRequest) => employeeGroupApi.createEmployeeGroup(request),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["employee-groups"] });
      success(i18nKeyContainer.employeeGroups.toast.added, {
        description: i18nKeyContainer.employeeGroups.toast.addedDesc,
      });
      navigate("/employee-groups");
    },
    onError: (error) => {
      handleApiError(error, i18nKeyContainer.employeeGroups.genericError);
    },
  });

  const validateForm = (): boolean => {
    const newErrors: Record<string, string> = {};

    if (!name.trim()) {
      newErrors.name = t(i18nKeyContainer.employeeGroups.form.validation.nameRequired);
    } else if (name.length > 100) {
      newErrors.name = t(i18nKeyContainer.employeeGroups.form.validation.nameMaxLength);
    }

    if (!rotationStartDate) {
      newErrors.rotationStartDate = t(i18nKeyContainer.employeeGroups.form.validation.rotationStartDateRequired);
    } else {
      const today = new Date().toISOString().split("T")[0];
      if (rotationStartDate < today) {
        newErrors.rotationStartDate = t(i18nKeyContainer.employeeGroups.form.validation.rotationStartDatePast);
      }
    }

    if (workSchedules.length === 0) {
      newErrors.workSchedules = t(i18nKeyContainer.employeeGroups.form.validation.atLeastOneSchedule);
    } else {
      workSchedules.forEach((schedule, index) => {
        if (schedule.shiftStartTime >= schedule.shiftEndTime && schedule.endDayOffset === "0") {
          newErrors[`schedule-${index}-shiftStartTime`] = t(i18nKeyContainer.employeeGroups.form.validation.shiftStartBeforeEnd);
        }
        if (schedule.breakStartTime >= schedule.breakEndTime && schedule.endDayOffset === "0") {
          newErrors[`schedule-${index}-breakStartTime`] = t(i18nKeyContainer.employeeGroups.form.validation.breakStartBeforeEnd);
        }
        if (
          schedule.breakStartTime < schedule.shiftStartTime ||
          schedule.breakEndTime > schedule.shiftEndTime
        ) {
          newErrors[`schedule-${index}-breakWithinShift`] = t(i18nKeyContainer.employeeGroups.form.validation.breakWithinShift);
        }
        if (Number(schedule.endDayOffset) < 0) {
          newErrors[`schedule-${index}-endDayOffset`] = t(i18nKeyContainer.employeeGroups.form.validation.endDayOffsetMin);
        }
        if (Number(schedule.allowedCheckInLatenessMinutes) < 0) {
          newErrors[`schedule-${index}-lateness`] = t(i18nKeyContainer.employeeGroups.form.validation.latenessMin);
        }
        if (Number(schedule.allowedCheckOutEarlinessMinutes) < 0) {
          newErrors[`schedule-${index}-earliness`] = t(i18nKeyContainer.employeeGroups.form.validation.earlinessMin);
        }
      });
    }

    if (rotations.length === 0) {
      newErrors.rotations = t(i18nKeyContainer.employeeGroups.form.validation.atLeastOneRotation);
    } else {
      const positions = new Set<string>();
      rotations.forEach((rotation, index) => {
        if (positions.has(rotation.position)) {
          newErrors[`rotation-${index}-position`] = t(i18nKeyContainer.employeeGroups.form.validation.uniquePosition);
        }
        positions.add(rotation.position);
        if (rotation.type === "Work" && !rotation.workScheduleId) {
          newErrors[`rotation-${index}-workScheduleId`] = t(i18nKeyContainer.employeeGroups.form.validation.scheduleReference);
        }
      });
    }

    setErrors(newErrors);
    return Object.keys(newErrors).length === 0;
  };

  const handleSubmit = (event: React.FormEvent) => {
    event.preventDefault();

    if (!validateForm()) {
      warning(i18nKeyContainer.common.error, {
        description: i18nKeyContainer.errors.validationDesc,
      });
      return;
    }

    const request: CreateEmployeeGroupRequest = {
      name: name.trim(),
      isSecurity,
      description: description.trim() || null,
      rotationStartDate,
      workSchedules: workSchedules.map((s) => ({
        shiftStartTime: s.shiftStartTime + ":00",
        shiftEndTime: s.shiftEndTime + ":00",
        breakStartTime: s.breakStartTime + ":00",
        breakEndTime: s.breakEndTime + ":00",
        endDayOffset: Number(s.endDayOffset),
        allowedCheckInLatenessMinutes: Number(s.allowedCheckInLatenessMinutes),
        allowedCheckOutEarlinessMinutes: Number(s.allowedCheckOutEarlinessMinutes),
      })),
      rotationEntries: rotations.map((r) => ({
        position: Number(r.position),
        workScheduleId: r.type === "Work" ? r.workScheduleId : null,
      })),
    };

    mutation.mutate(request);
  };

  const addSchedule = () => {
    setWorkSchedules([...workSchedules, getDefaultSchedule()]);
  };

  const removeSchedule = (index: number) => {
    if (workSchedules.length <= 1) return;
    setWorkSchedules(workSchedules.filter((_, i) => i !== index));
  };

  const updateSchedule = (index: number, field: keyof WorkScheduleForm, value: string) => {
    setWorkSchedules(
      workSchedules.map((s, i) => (i === index ? { ...s, [field]: value } : s))
    );
  };

  const addRotation = (type: "Work" | "Rest") => {
    setRotations([...rotations, { ...getDefaultRotation(workSchedules), type }]);
  };

  const removeRotation = (index: number) => {
    setRotations(rotations.filter((_, i) => i !== index));
  };

  const updateRotation = (index: number, field: keyof RotationForm, value: string) => {
    setRotations(
      rotations.map((r, i) => (i === index ? { ...r, [field]: value } : r))
    );
  };

  const scheduleOptions = workSchedules.map((s, index) => ({
    value: index.toString(),
    label: `${t(i18nKeyContainer.employeeGroups.form.schedule.shiftStartTime)}: ${s.shiftStartTime} - ${t(i18nKeyContainer.employeeGroups.form.schedule.shiftEndTime)}: ${s.shiftEndTime}`,
  }));

  return (
    <div dir={isRtl ? "rtl" : "ltr"}>
      <div className="mb-6">
        <h1 className="text-2xl font-bold text-slate-900">
          {t(i18nKeyContainer.employeeGroups.addTitle)}
        </h1>
        <p className="text-slate-500">{t(i18nKeyContainer.employeeGroups.addDescription)}</p>
      </div>

      <form className="space-y-6" onSubmit={handleSubmit} noValidate>
        {/* Group Info Section */}
        <Card className="border-slate-200 bg-white shadow-sm">
          <CardHeader>
            <CardTitle className="text-lg text-slate-900 flex items-center gap-2">
              <Shield className="h-5 w-5 text-primary" />
              {t(i18nKeyContainer.employeeGroups.form.name)}
            </CardTitle>
            <CardDescription>{t(i18nKeyContainer.employeeGroups.addDescription)}</CardDescription>
          </CardHeader>
          <CardContent className="space-y-4">
            <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
              <div className="space-y-2">
                <Label htmlFor="name" className="text-sm font-medium text-slate-700">
                  {t(i18nKeyContainer.employeeGroups.form.name)}
                  <span className="text-red-500 ml-1">*</span>
                </Label>
                <input
                  id="name"
                  type="text"
                  value={name}
                  onChange={(e) => setName(e.target.value)}
                  placeholder={t(i18nKeyContainer.employeeGroups.form.namePlaceholder)}
                  className={cn(
                    "w-full h-11 rounded-lg border border-slate-200 bg-slate-50 px-4 py-2.5 text-sm placeholder:text-slate-400 focus:bg-white focus:border-primary focus:ring-1 focus:ring-primary",
                    errors.name && "border-red-300 focus:border-red-500 focus:ring-red-500"
                  )}
                  maxLength={100}
                  required
                />
                {errors.name && (
                  <p className="text-sm text-red-600" role="alert">{errors.name}</p>
                )}
              </div>

              <div className="space-y-2">
                <Label htmlFor="isSecurity" className="flex items-center gap-2 text-sm font-medium text-slate-700 cursor-pointer">
                  <Checkbox
                    id="isSecurity"
                    checked={isSecurity}
                    onCheckedChange={setIsSecurity}
                    className="h-4 w-4 rounded border-slate-300 text-primary focus:ring-primary"
                  />
                  {t(i18nKeyContainer.employeeGroups.form.isSecurity)}
                </Label>
              </div>
            </div>

            <TextArea
              id="description"
              label={t(i18nKeyContainer.employeeGroups.form.description)}
              value={description}
              onChange={setDescription}
              placeholder={t(i18nKeyContainer.employeeGroups.form.descriptionPlaceholder)}
              rows={3}
            />

            <DateInput
              id="rotationStartDate"
              label={t(i18nKeyContainer.employeeGroups.form.rotationStartDate)}
              value={rotationStartDate}
              onChange={setRotationStartDate}
              placeholder={t(i18nKeyContainer.employeeGroups.form.rotationStartDatePlaceholder)}
              required
              min={new Date().toISOString().split("T")[0]}
              error={errors.rotationStartDate}
            />
          </CardContent>
        </Card>

        {/* Work Schedules Section */}
        <Card className="border-slate-200 bg-white shadow-sm">
          <CardHeader>
            <div className="flex items-center justify-between">
              <div>
                <CardTitle className="text-lg text-slate-900 flex items-center gap-2">
                  <Clock className="h-5 w-5 text-primary" />
                  {t(i18nKeyContainer.employeeGroups.form.workSchedules)}
                </CardTitle>
                <CardDescription>{t(i18nKeyContainer.employeeGroups.form.workSchedules)}</CardDescription>
              </div>
              <Button
                type="button"
                variant="outline"
                size="sm"
                onClick={addSchedule}
                className="cursor-pointer gap-1 h-9"
              >
                <Plus className="h-4 w-4" />
                {t(i18nKeyContainer.employeeGroups.form.addSchedule)}
              </Button>
            </div>
          </CardHeader>
          <CardContent className="space-y-4">
            {workSchedules.map((schedule, index) => (
              <div
                key={index}
                className="border border-slate-200 rounded-lg p-4 space-y-4"
              >
                <div className="flex items-center justify-between">
                  <span className="font-medium text-slate-900">
                    {t(i18nKeyContainer.employeeGroups.form.workSchedules)} {index + 1}
                  </span>
                  {workSchedules.length > 1 && (
                    <Button
                      type="button"
                      variant="ghost"
                      size="icon"
                      onClick={() => removeSchedule(index)}
                      className="text-red-600 hover:text-red-700 hover:bg-red-50 cursor-pointer"
                    >
                      <Trash2 className="h-4 w-4" />
                    </Button>
                  )}
                </div>

                <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-4">
                  <TimeInput
                    id={`schedule-${index}-shiftStartTime`}
                    label={t(i18nKeyContainer.employeeGroups.form.schedule.shiftStartTime)}
                    value={schedule.shiftStartTime}
                    onChange={(v) => updateSchedule(index, "shiftStartTime", v)}
                    required
                    error={errors[`schedule-${index}-shiftStartTime`]}
                  />
                  <TimeInput
                    id={`schedule-${index}-shiftEndTime`}
                    label={t(i18nKeyContainer.employeeGroups.form.schedule.shiftEndTime)}
                    value={schedule.shiftEndTime}
                    onChange={(v) => updateSchedule(index, "shiftEndTime", v)}
                    required
                    error={errors[`schedule-${index}-shiftEndTime`]}
                  />
                  <TimeInput
                    id={`schedule-${index}-breakStartTime`}
                    label={t(i18nKeyContainer.employeeGroups.form.schedule.breakStartTime)}
                    value={schedule.breakStartTime}
                    onChange={(v) => updateSchedule(index, "breakStartTime", v)}
                    required
                    error={errors[`schedule-${index}-breakStartTime`]}
                  />
                  <TimeInput
                    id={`schedule-${index}-breakEndTime`}
                    label={t(i18nKeyContainer.employeeGroups.form.schedule.breakEndTime)}
                    value={schedule.breakEndTime}
                    onChange={(v) => updateSchedule(index, "breakEndTime", v)}
                    required
                    error={errors[`schedule-${index}-breakEndTime`]}
                  />
                </div>

                <div className="grid grid-cols-1 gap-4 sm:grid-cols-3">
                  <NumberInput
                    id={`schedule-${index}-endDayOffset`}
                    label={t(i18nKeyContainer.employeeGroups.form.schedule.endDayOffset)}
                    value={schedule.endDayOffset}
                    onChange={(v) => updateSchedule(index, "endDayOffset", v)}
                    min={0}
                    required
                    error={errors[`schedule-${index}-endDayOffset`]}
                  />
                  <NumberInput
                    id={`schedule-${index}-allowedCheckInLatenessMinutes`}
                    label={t(i18nKeyContainer.employeeGroups.form.schedule.allowedCheckInLatenessMinutes)}
                    value={schedule.allowedCheckInLatenessMinutes}
                    onChange={(v) => updateSchedule(index, "allowedCheckInLatenessMinutes", v)}
                    min={0}
                    required
                    error={errors[`schedule-${index}-lateness`]}
                  />
                  <NumberInput
                    id={`schedule-${index}-allowedCheckOutEarlinessMinutes`}
                    label={t(i18nKeyContainer.employeeGroups.form.schedule.allowedCheckOutEarlinessMinutes)}
                    value={schedule.allowedCheckOutEarlinessMinutes}
                    onChange={(v) => updateSchedule(index, "allowedCheckOutEarlinessMinutes", v)}
                    min={0}
                    required
                    error={errors[`schedule-${index}-earliness`]}
                  />
                </div>
              </div>
            ))}

            {errors.workSchedules && (
              <p className="text-sm text-red-600" role="alert">{errors.workSchedules}</p>
            )}
          </CardContent>
        </Card>

        {/* Rotations Section */}
        <Card className="border-slate-200 bg-white shadow-sm">
          <CardHeader>
            <div className="flex items-center justify-between">
              <div>
                <CardTitle className="text-lg text-slate-900 flex items-center gap-2">
                  <RotateCcw className="h-5 w-5 text-primary" />
                  {t(i18nKeyContainer.employeeGroups.form.rotations)}
                </CardTitle>
                <CardDescription>{t(i18nKeyContainer.employeeGroups.form.rotations)}</CardDescription>
              </div>
              <div className="flex gap-2">
                <Button
                  type="button"
                  variant="outline"
                  size="sm"
                  onClick={() => addRotation("Work")}
                  className="cursor-pointer gap-1 h-9"
                >
                  <Plus className="h-4 w-4" />
                  {t(i18nKeyContainer.employeeGroups.form.addWorkRotation)}
                </Button>
                <Button
                  type="button"
                  variant="outline"
                  size="sm"
                  onClick={() => addRotation("Rest")}
                  className="cursor-pointer gap-1 h-9"
                >
                  <Plus className="h-4 w-4" />
                  {t(i18nKeyContainer.employeeGroups.form.addRestRotation)}
                </Button>
              </div>
            </div>
          </CardHeader>
          <CardContent className="space-y-3">
            {rotations.map((rotation, index) => (
              <div
                key={index}
                className="flex flex-col gap-3 sm:flex-row sm:items-center border border-slate-200 rounded-lg p-4"
              >
                <div className="flex items-center gap-3 flex-1">
                  <span className="w-8 text-center font-medium text-slate-900 bg-slate-100 rounded px-2 py-1">
                    {index + 1}
                  </span>
                  <SelectInput
                    id={`rotation-${index}-type`}
                    label=""
                    value={rotation.type}
                    onChange={(v) => updateRotation(index, "type", v as "Work" | "Rest")}
                    options={[
                      { value: "Work", label: t(i18nKeyContainer.employeeGroups.form.rotation.work) },
                      { value: "Rest", label: t(i18nKeyContainer.employeeGroups.form.rotation.rest) },
                    ]}
                    className="w-full sm:w-40"
                  />
                  {rotation.type === "Work" && (
                    <SelectInput
                      id={`rotation-${index}-workScheduleId`}
                      label=""
                      value={rotation.workScheduleId}
                      onChange={(v) => updateRotation(index, "workScheduleId", v)}
                      options={scheduleOptions}
                      placeholder={t(i18nKeyContainer.employeeGroups.form.rotation.selectSchedule)}
                      error={errors[`rotation-${index}-workScheduleId`]}
                      className="w-full sm:w-64"
                    />
                  )}
                  <Button
                    type="button"
                    variant="ghost"
                    size="icon"
                    onClick={() => removeRotation(index)}
                    className="text-red-600 hover:text-red-700 hover:bg-red-50 cursor-pointer"
                  >
                    <Trash2 className="h-4 w-4" />
                  </Button>
                </div>
                {errors[`rotation-${index}-position`] && (
                  <p className="text-sm text-red-600 sm:hidden">{errors[`rotation-${index}-position`]}</p>
                )}
                {errors[`rotation-${index}-workScheduleId`] && rotation.type === "Work" && (
                  <p className="text-sm text-red-600 sm:hidden">
                    {errors[`rotation-${index}-workScheduleId`]}
                  </p>
                )}
              </div>
            ))}

            {rotations.length === 0 && (
              <div className="text-center py-8 text-slate-500">
                <p>{t(i18nKeyContainer.employeeGroups.form.rotations)}</p>
              </div>
            )}

            {errors.rotations && (
              <p className="text-sm text-red-600" role="alert">{errors.rotations}</p>
            )}
          </CardContent>
        </Card>

        {/* Actions */}
        <div className="flex gap-3 justify-end">
          <Button
            type="button"
            variant="outline"
            className="h-11 px-6 cursor-pointer border-slate-200 bg-white hover:bg-slate-50"
            onClick={() => navigate("/employee-groups")}
            disabled={mutation.isPending}
          >
            {t(i18nKeyContainer.common.cancel)}
          </Button>
          <Button
            type="submit"
            className="h-11 px-6 cursor-pointer"
            disabled={mutation.isPending}
          >
            {mutation.isPending
              ? t(i18nKeyContainer.common.saving)
              : t(i18nKeyContainer.employeeGroups.addTitle)}
          </Button>
        </div>
      </form>
    </div>
  );
}