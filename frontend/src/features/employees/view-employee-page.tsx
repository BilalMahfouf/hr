import { useQuery } from "@tanstack/react-query";
import {
  ArrowLeft,
  BadgeCheck,
  Briefcase,
  CalendarDays,
  CreditCard,
  Globe,
  IdCard,
  Layers,
  MapPin,
  Phone,
  Ruler,
  User,
  type LucideIcon,
} from "lucide-react";
import { useTranslation } from "react-i18next";
import { useNavigate, useParams } from "react-router-dom";
import { Avatar, AvatarFallback, AvatarImage } from "@/components/ui/avatar";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import {
  Card,
  CardContent,
  CardHeader,
  CardTitle,
} from "@/components/ui/card";
import { Separator } from "@/components/ui/separator";
import { Skeleton } from "@/components/ui/skeleton";
import i18nKeyContainer from "@/lib/i18n/keyContainer";
import employeesApi from "./employees-api";

interface DetailItemProps {
  icon: LucideIcon;
  label: string;
  value: React.ReactNode;
  mono?: boolean;
}

function DetailItem({ icon: Icon, label, value, mono }: DetailItemProps) {
  return (
    <div className="flex items-start gap-3 rounded-lg border border-slate-100 bg-slate-50/50 p-3">
      <div className="flex h-9 w-9 shrink-0 items-center justify-center rounded-lg bg-white text-slate-500 shadow-sm">
        <Icon className="h-4 w-4" />
      </div>
      <div className="min-w-0 space-y-0.5">
        <p className="text-xs font-medium uppercase tracking-wide text-slate-500">
          {label}
        </p>
        <p
          className={`break-words text-sm font-semibold text-slate-800 ${mono ? "font-mono" : ""}`}
        >
          {value}
        </p>
      </div>
    </div>
  );
}

interface DetailSectionProps {
  title: string;
  children: React.ReactNode;
}

function DetailSection({ title, children }: DetailSectionProps) {
  return (
    <div className="space-y-4">
      <h3 className="text-sm font-semibold uppercase tracking-wide text-slate-400">
        {title}
      </h3>
      <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">{children}</div>
    </div>
  );
}

export default function ViewEmployeePage() {
  const { id = "" } = useParams<{ id: string }>();
  const { t, i18n } = useTranslation();
  const isRtl = i18n.language === "ar";
  const navigate = useNavigate();

  const { data: employee, isLoading, isError } = useQuery({
    queryKey: ["employee", id],
    queryFn: () => employeesApi.getEmployeeById(id),
    enabled: id !== "",
  });

  if (isLoading) {
    return (
      <div dir={isRtl ? "rtl" : "ltr"}>
        <div className="mb-6">
          <Skeleton className="h-8 w-64" />
          <Skeleton className="mt-2 h-5 w-96" />
        </div>
        <Card className="mx-auto w-full max-w-3xl border-slate-200 bg-white shadow-sm">
          <CardContent className="space-y-3 p-6">
            <div className="h-11 animate-pulse rounded-lg bg-slate-100" />
            <div className="h-11 animate-pulse rounded-lg bg-slate-100" />
            <div className="h-11 animate-pulse rounded-lg bg-slate-100" />
          </CardContent>
        </Card>
      </div>
    );
  }

  if (isError || !employee) {
    return (
      <div dir={isRtl ? "rtl" : "ltr"}>
        <div className="rounded-lg border border-red-200 bg-red-50 p-4 text-sm text-red-700">
          <p className="font-medium">
            {t(i18nKeyContainer.errors.employee.notFound)}
          </p>
          <p className="mt-1 text-red-600">
            {t(i18nKeyContainer.errors.employee.notFoundDesc)}
          </p>
        </div>
      </div>
    );
  }

  const fullName = `${employee.firstName} ${employee.lastName}`;
  const initials = `${employee.firstName.charAt(0)}${employee.lastName.charAt(0)}`.toUpperCase();
  // TODO: wire photo rendering logic (photoBase64)
  const photoUrl: string | undefined = undefined;

  const birthDate = employee.birthDate
    ? new Date(employee.birthDate).toLocaleDateString(i18n.language, {
        year: "numeric",
        month: "long",
        day: "numeric",
      })
    : null;

  return (
    <div dir={isRtl ? "rtl" : "ltr"}>
      <div className="mb-6 flex flex-col items-start justify-between gap-3 sm:flex-row sm:items-center">
        <div>
          <h1 className="text-2xl font-bold text-slate-900">
            {t(i18nKeyContainer.employees.view.title)}
          </h1>
          <p className="text-slate-500">
            {t(i18nKeyContainer.employees.view.description)}
          </p>
        </div>

        <Button
          variant="outline"
          className="h-10 cursor-pointer border-slate-200 bg-white hover:bg-slate-50"
          onClick={() => navigate("/employees")}
        >
          <ArrowLeft className="h-4 w-4 rtl:rotate-180" />
          {t(i18nKeyContainer.common.back)}
        </Button>
      </div>

      <Card className="mx-auto w-full max-w-3xl border-slate-200 bg-white shadow-sm">
        <CardHeader className="border-b border-slate-100">
          <div className="flex items-center gap-4">
            <Avatar className="h-16 w-16 border border-slate-200">
              {photoUrl && <AvatarImage src={photoUrl} alt={fullName} />}
              <AvatarFallback className="bg-primary/10 text-lg font-semibold text-primary">
                {initials}
              </AvatarFallback>
            </Avatar>
            <div className="min-w-0 space-y-1">
              <CardTitle className="truncate text-xl text-slate-900">
                {fullName}
              </CardTitle>
              <div className="flex flex-wrap items-center gap-2">
                <Badge
                  variant="secondary"
                  className="gap-1.5 bg-slate-100 font-mono text-slate-700"
                >
                  <IdCard className="h-3.5 w-3.5" />
                  {employee.matricule}
                </Badge>
                {employee.department && (
                  <Badge
                    variant="secondary"
                    className="gap-1.5 bg-primary/10 text-primary"
                  >
                    <Briefcase className="h-3.5 w-3.5" />
                    {employee.department}
                  </Badge>
                )}
              </div>
            </div>
          </div>
        </CardHeader>
        <CardContent className="space-y-6 px-6 py-6">
          <DetailSection
            title={t(
              i18nKeyContainer.employees.view.personalInfoSection,
            )}
          >
            <DetailItem
              icon={CalendarDays}
              label={t(i18nKeyContainer.employees.view.birthDate)}
              value={birthDate ?? "—"}
            />
            <DetailItem
              icon={MapPin}
              label={t(i18nKeyContainer.employees.view.birthPlace)}
              value={employee.birthPlace ?? "—"}
            />
            <DetailItem
              icon={User}
              label={t(i18nKeyContainer.employees.view.sex)}
              value={employee.sex ?? "—"}
            />
            <DetailItem
              icon={Phone}
              label={t(i18nKeyContainer.employees.view.phone)}
              value={employee.phone ?? "—"}
              mono
            />
            <DetailItem
              icon={Globe}
              label={t(i18nKeyContainer.employees.view.nationality)}
              value={employee.nationality ?? "—"}
            />
            <DetailItem
              icon={MapPin}
              label={t(i18nKeyContainer.employees.view.address)}
              value={employee.address ?? "—"}
            />
          </DetailSection>

          <Separator />

          <DetailSection
            title={t(i18nKeyContainer.employees.view.jobInfoSection)}
          >
            <DetailItem
              icon={CreditCard}
              label={t(i18nKeyContainer.employees.view.bdg)}
              value={employee.bdg ?? "—"}
              mono
            />
            <DetailItem
              icon={Layers}
              label={t(i18nKeyContainer.employees.view.group)}
              value={employee.group ?? "—"}
            />
            <DetailItem
              icon={Briefcase}
              label={t(i18nKeyContainer.employees.view.department)}
              value={employee.department ?? "—"}
            />
            <DetailItem
              icon={Ruler}
              label={t(i18nKeyContainer.employees.view.codeNiv)}
              value={employee.codeNiv ?? "—"}
              mono
            />
            <DetailItem
              icon={BadgeCheck}
              label={t(i18nKeyContainer.employees.view.spec)}
              value={employee.spec ?? "—"}
            />
          </DetailSection>
        </CardContent>
      </Card>
    </div>
  );
}
