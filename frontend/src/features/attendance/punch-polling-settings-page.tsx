import { useState } from "react";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { useTranslation } from "react-i18next";
import { Clock, Loader2, Save } from "lucide-react";

import { Button } from "@/components/ui/button";
import { Skeleton } from "@/components/ui/skeleton";
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from "@/components/ui/card";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import { Switch } from "@/components/ui/switch";
import { Label } from "@/components/ui/label";
import { Separator } from "@/components/ui/separator";

import attendanceApi from "./attendance-api";
import i18nKeyContainer from "@/lib/i18n/keyContainer";
import { useToast } from "@/hooks/use-toast";

const INTERVAL_OPTIONS = [10, 15, 20, 25, 30, 35, 40, 45, 50, 55, 60];

export default function PunchPollingSettingsPage() {
  const { t, i18n } = useTranslation();
  const isRtl = i18n.language === "ar";
  const queryClient = useQueryClient();
  const { handleApiError, success } = useToast();

  const { data: settings, isLoading } = useQuery({
    queryKey: ["punchPollingSettings"],
    queryFn: attendanceApi.getPunchPollingSettings,
  });

  const [isEnabled, setIsEnabled] = useState(false);
  const [intervalMinutes, setIntervalMinutes] = useState(30);
  const [isInitialized, setIsInitialized] = useState(false);

  // Initialize form state from query data
  if (settings && !isInitialized) {
    setIsEnabled(settings.isEnabled);
    setIntervalMinutes(settings.intervalMinutes);
    setIsInitialized(true);
  }

  const updateMutation = useMutation({
    mutationFn: attendanceApi.updatePunchPollingSettings,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["punchPollingSettings"] });
      success(i18nKeyContainer.toast.punchPolling.updated, {
        description: i18nKeyContainer.toast.punchPolling.updatedDesc,
      });
    },
    onError: (error) => {
      handleApiError(error, i18nKeyContainer.attendance.punchPolling.genericError);
    },
  });

  const handleSave = () => {
    updateMutation.mutate({ isEnabled, intervalMinutes });
  };

  if (isLoading) {
    return (
      <div dir={isRtl ? "rtl" : "ltr"}>
        <div className="mb-6">
          <div className="flex items-center gap-3">
            <div className="flex h-12 w-12 items-center justify-center rounded-xl bg-primary/10">
              <Skeleton className="h-6 w-6" />
            </div>
            <div>
              <Skeleton className="h-8 w-64" />
              <Skeleton className="mt-2 h-4 w-96" />
            </div>
          </div>
        </div>
        <Card className="border-slate-200 bg-white shadow-sm">
          <CardHeader>
            <Skeleton className="h-6 w-48" />
          </CardHeader>
          <CardContent className="space-y-4">
            <Skeleton className="h-10 w-full" />
            <Skeleton className="h-10 w-full" />
          </CardContent>
        </Card>
      </div>
    );
  }

  return (
    <div dir={isRtl ? "rtl" : "ltr"}>
      <div className="mb-6">
        <div className="flex items-center gap-3">
          <div className="flex h-12 w-12 items-center justify-center rounded-xl bg-primary/10">
            <Clock className="h-6 w-6 text-primary" />
          </div>
          <div>
            <h1 className="text-2xl font-bold text-slate-900">
              {t(i18nKeyContainer.attendance.punchPolling.title)}
            </h1>
            <p className="text-slate-500">
              {t(i18nKeyContainer.attendance.punchPolling.description)}
            </p>
          </div>
        </div>
      </div>

      <Card className="border-slate-200 bg-white shadow-sm">
        <CardHeader>
          <CardTitle>{t(i18nKeyContainer.attendance.punchPolling.cardTitle)}</CardTitle>
          <CardDescription>
            {t(i18nKeyContainer.attendance.punchPolling.cardDescription)}
          </CardDescription>
        </CardHeader>
        <CardContent className="space-y-6">
          <div className="flex items-center justify-between">
            <div className="space-y-0.5">
              <Label htmlFor="polling-enabled" className="text-base">
                {t(i18nKeyContainer.attendance.punchPolling.enabled)}
              </Label>
              <p className="text-sm text-slate-500">
                {t(i18nKeyContainer.attendance.punchPolling.enabledDescription)}
              </p>
            </div>
            <Switch
              id="polling-enabled"
              checked={isEnabled}
              onCheckedChange={setIsEnabled}
            />
          </div>

          <Separator />

          <div className="space-y-2">
            <Label htmlFor="interval" className="text-base">
              {t(i18nKeyContainer.attendance.punchPolling.interval)}
            </Label>
            <p className="text-sm text-slate-500">
              {t(i18nKeyContainer.attendance.punchPolling.intervalDescription)}
            </p>
            <Select
              value={intervalMinutes.toString()}
              onValueChange={(value) => setIntervalMinutes(parseInt(value))}
            >
              <SelectTrigger id="interval" className="w-[200px] border-slate-200 bg-white">
                <SelectValue />
              </SelectTrigger>
              <SelectContent className="border-slate-200 bg-white">
                {INTERVAL_OPTIONS.map((minutes) => (
                  <SelectItem key={minutes} value={minutes.toString()}>
                    {minutes} {t(i18nKeyContainer.attendance.punchPolling.minutes)}
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>
            <p className="text-sm text-slate-400">
              {t(i18nKeyContainer.attendance.punchPolling.allowedRange)}
            </p>
          </div>

          <Separator />

          <div className="flex items-center gap-3">
            <Button
              onClick={handleSave}
              disabled={updateMutation.isPending}
              className=""
            >
              {updateMutation.isPending ? (
                <Loader2 className="mr-2 h-4 w-4 animate-spin" />
              ) : (
                <Save className="mr-2 h-4 w-4" />
              )}
              {t(i18nKeyContainer.common.save)}
            </Button>
          </div>
        </CardContent>
      </Card>
    </div>
  );
}
