import { useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useTranslation } from "react-i18next";
import { useNavigate, useParams } from "react-router-dom";
import { Cable, Hash, Wifi } from "lucide-react";

import { Button } from "@/components/ui/button";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { useToast } from "@/hooks/use-toast";
import i18nKeyContainer from "@/lib/i18n/keyContainer";
import attendanceMachineApi, {
  type MachineRecord,
  type UpdateMachineIpAddressRequest,
} from "./attendance-machine-api";

const isValidIpv4 = (value: string): boolean => {
  const parts = value.split(".");
  return (
    parts.length === 4 &&
    parts.every((part) => {
      if (!/^\d{1,3}$/.test(part)) return false;
      const octet = Number(part);
      return octet >= 0 && octet <= 255;
    })
  );
};

function EditMachineForm({ machine }: { machine: MachineRecord }) {
  const { t, i18n } = useTranslation();
  const isRtl = i18n.language === "ar";
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const { handleApiError, success, warning } = useToast();

  const [ipAddress, setIpAddress] = useState(machine.ipAddress);

  const mutation = useMutation({
    mutationFn: (request: UpdateMachineIpAddressRequest) =>
      attendanceMachineApi.updateMachineIpAddress(machine.machineId, request),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["machines"] });
      success(i18nKeyContainer.toast.machine.updated, {
        description: i18nKeyContainer.toast.machine.updatedDesc,
      });
      navigate("/machines");
    },
    onError: (error) => {
      handleApiError(error, i18nKeyContainer.machines.genericError);
    },
  });

  const handleSubmit = (event: React.FormEvent) => {
    event.preventDefault();

    if (!ipAddress.trim() || !isValidIpv4(ipAddress.trim())) {
      warning(i18nKeyContainer.machines.form.invalidError, {
        description: i18nKeyContainer.machines.form.invalidErrorDesc,
      });
      return;
    }

    mutation.mutate({ ipAddress: ipAddress.trim() });
  };

  return (
    <div dir={isRtl ? "rtl" : "ltr"}>
      <div className="mb-6">
        <h1 className="text-2xl font-bold text-slate-900">
          {t(i18nKeyContainer.machines.form.updateTitle)}
        </h1>
        <p className="text-slate-500">{t(i18nKeyContainer.machines.form.updateDescription)}</p>
      </div>

      <Card className="max-w-2xl border-slate-200 bg-white shadow-sm">
        <CardHeader>
          <CardTitle className="text-lg text-slate-900">
            {t(i18nKeyContainer.machines.editMachine)}
          </CardTitle>
          <CardDescription>
            {t(i18nKeyContainer.machines.form.updateDescription)}
          </CardDescription>
        </CardHeader>

        <CardContent>
          <form className="space-y-4" onSubmit={handleSubmit} noValidate>
            <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
              <div className="space-y-2">
                <Label htmlFor="machineNumber">
                  {t(i18nKeyContainer.machines.form.machineNumber)}
                </Label>
                <div className="relative">
                  <Hash className="absolute start-3 top-1/2 h-4 w-4 -translate-y-1/2 text-slate-400" />
                  <Input
                    id="machineNumber"
                    value={String(machine.machineNumber)}
                    readOnly
                    className="h-11 border-slate-200 bg-slate-100 ps-10 text-slate-500"
                  />
                </div>
              </div>

              <div className="space-y-2">
                <Label htmlFor="port">{t(i18nKeyContainer.machines.form.port)}</Label>
                <div className="relative">
                  <Cable className="absolute start-3 top-1/2 h-4 w-4 -translate-y-1/2 text-slate-400" />
                  <Input
                    id="port"
                    value={String(machine.port)}
                    readOnly
                    className="h-11 border-slate-200 bg-slate-100 ps-10 text-slate-500"
                  />
                </div>
              </div>
            </div>

            <div className="space-y-2">
              <Label htmlFor="ipAddress">{t(i18nKeyContainer.machines.form.ipAddress)}</Label>
              <div className="relative">
                <Wifi className="absolute start-3 top-1/2 h-4 w-4 -translate-y-1/2 text-slate-400" />
                <Input
                  id="ipAddress"
                  value={ipAddress}
                  onChange={(event) => setIpAddress(event.target.value)}
                  className="h-11 border-slate-200 bg-slate-50 ps-10 focus:bg-white"
                  placeholder={t(i18nKeyContainer.machines.form.ipAddressPlaceholder)}
                />
              </div>
            </div>

            <div className="flex gap-3 pt-2">
              <Button
                type="button"
                variant="outline"
                className="h-11 flex-1 cursor-pointer border-slate-200 bg-white hover:bg-slate-50"
                onClick={() => navigate("/machines")}
                disabled={mutation.isPending}
              >
                {t(i18nKeyContainer.common.cancel)}
              </Button>
              <Button
                type="submit"
                className="h-11 flex-1 cursor-pointer"
                disabled={mutation.isPending}
              >
                {mutation.isPending
                  ? t(i18nKeyContainer.common.saving)
                  : t(i18nKeyContainer.machines.form.updateAction)}
              </Button>
            </div>
          </form>
        </CardContent>
      </Card>
    </div>
  );
}

export default function EditMachinePage() {
  const { machineId = "" } = useParams<{ machineId: string }>();
  const { t, i18n } = useTranslation();
  const isRtl = i18n.language === "ar";

  const { data, isLoading } = useQuery({
    queryKey: ["machines"],
    queryFn: () => attendanceMachineApi.getAllMachines({ page: 1, pageSize: 1000 }),
  });

  const machine = data?.item.find((item) => item.machineId === machineId);

  if (isLoading) {
    return (
      <div dir={isRtl ? "rtl" : "ltr"}>
        <div className="mb-6">
          <h1 className="text-2xl font-bold text-slate-900">
            {t(i18nKeyContainer.machines.form.updateTitle)}
          </h1>
          <p className="text-slate-500">{t(i18nKeyContainer.machines.form.updateDescription)}</p>
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

  if (!machine) {
    return (
      <div dir={isRtl ? "rtl" : "ltr"}>
        <div className="rounded-lg border border-red-200 bg-red-50 p-4 text-sm text-red-700">
          <p className="font-medium">{t(i18nKeyContainer.machines.notFound)}</p>
          <p className="mt-1 text-red-600">{t(i18nKeyContainer.machines.notFoundDesc)}</p>
        </div>
      </div>
    );
  }

  return <EditMachineForm key={machine.machineId} machine={machine} />;
}