import { useState } from "react";
import { useMutation, useQueryClient } from "@tanstack/react-query";
import { useTranslation } from "react-i18next";
import { useNavigate } from "react-router-dom";
import { Cable, Hash, Wifi } from "lucide-react";

import { Button } from "@/components/ui/button";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { useToast } from "@/hooks/use-toast";
import i18nKeyContainer from "@/lib/i18n/keyContainer";
import attendanceMachineApi from "./attendance-machine-api";

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

export default function CreateMachinePage() {
  const { t, i18n } = useTranslation();
  const isRtl = i18n.language === "ar";
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const { handleApiError, success, warning } = useToast();

  const [machineNumber, setMachineNumber] = useState("");
  const [ipAddress, setIpAddress] = useState("");
  const [port, setPort] = useState("4370");

  const mutation = useMutation({
    mutationFn: attendanceMachineApi.createMachine,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["machines"] });
      success(i18nKeyContainer.toast.machine.added, {
        description: i18nKeyContainer.toast.machine.addedDesc,
      });
      navigate("/machines");
    },
    onError: (error) => {
      handleApiError(error, i18nKeyContainer.machines.genericError);
    },
  });

  const handleSubmit = (event: React.FormEvent) => {
    event.preventDefault();

    const parsedMachineNumber = Number(machineNumber);
    const parsedPort = port.trim() === "" ? undefined : Number(port);

    if (
      !ipAddress.trim() ||
      !isValidIpv4(ipAddress.trim()) ||
      !Number.isInteger(parsedMachineNumber) ||
      parsedMachineNumber <= 0 ||
      (parsedPort !== undefined && (!Number.isInteger(parsedPort) || parsedPort <= 0))
    ) {
      warning(i18nKeyContainer.machines.form.invalidError, {
        description: i18nKeyContainer.machines.form.invalidErrorDesc,
      });
      return;
    }

    mutation.mutate({
      ipAddress: ipAddress.trim(),
      machineNumber: parsedMachineNumber,
      port: parsedPort,
    });
  };

  return (
    <div dir={isRtl ? "rtl" : "ltr"}>
      <div className="mb-6">
        <h1 className="text-2xl font-bold text-slate-900">
          {t(i18nKeyContainer.machines.form.addTitle)}
        </h1>
        <p className="text-slate-500">{t(i18nKeyContainer.machines.form.addDescription)}</p>
      </div>

      <Card className="max-w-2xl border-slate-200 bg-white shadow-sm">
        <CardHeader>
          <CardTitle className="text-lg text-slate-900">
            {t(i18nKeyContainer.machines.form.addTitle)}
          </CardTitle>
          <CardDescription>
            {t(i18nKeyContainer.machines.form.addDescription)}
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
                    type="number"
                    step="1"
                    min="1"
                    value={machineNumber}
                    onChange={(event) => setMachineNumber(event.target.value)}
                    className="h-11 border-slate-200 bg-slate-50 ps-10 focus:bg-white"
                    placeholder={t(i18nKeyContainer.machines.form.machineNumberPlaceholder)}
                  />
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
            </div>

            <div className="space-y-2 sm:max-w-[calc(50%-0.5rem)]">
              <Label htmlFor="port">
                {t(i18nKeyContainer.machines.form.port)}
                <span className="ms-1 text-xs font-normal text-slate-400">
                  ({t(i18nKeyContainer.machines.form.portOptional)})
                </span>
              </Label>
              <div className="relative">
                <Cable className="absolute start-3 top-1/2 h-4 w-4 -translate-y-1/2 text-slate-400" />
                <Input
                  id="port"
                  type="number"
                  step="1"
                  min="1"
                  value={port}
                  onChange={(event) => setPort(event.target.value)}
                  className="h-11 border-slate-200 bg-slate-50 ps-10 focus:bg-white"
                  placeholder={t(i18nKeyContainer.machines.form.portPlaceholder)}
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
                  : t(i18nKeyContainer.machines.form.addAction)}
              </Button>
            </div>
          </form>
        </CardContent>
      </Card>
    </div>
  );
}