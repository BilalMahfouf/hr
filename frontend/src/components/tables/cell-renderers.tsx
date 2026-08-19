import { Badge } from "@/components/ui/badge";
import { Calendar, Phone, Mail, MapPin, Clock, User } from "lucide-react";
import { cn } from "@/lib/utils";
import type { StatusVariant } from "./types";

// ==================== Status Badge Cell ====================

interface StatusBadgeProps {
  status: string;
  variant?: StatusVariant;
  className?: string;
}

const statusVariantMap: Record<string, StatusVariant> = {
  // Common status mappings
  active: "success",
  inactive: "secondary",
  stable: "success",
  "under observation": "warning",
  critical: "error",
  finalized: "success",
  draft: "success",
  pending: "warning",
  cancelled: "error",
  completed: "success",
  scheduled: "info",
};

export function StatusBadge({ status, variant, className }: StatusBadgeProps) {
  const normalizedStatus = status.toLowerCase();
  const resolvedVariant = variant || statusVariantMap[normalizedStatus] || "secondary";

  return (
    <Badge variant={resolvedVariant} className={cn("capitalize", className)}>
      {status}
    </Badge>
  );
}

// ==================== Status Indicator (Dot + Text) ====================

interface StatusIndicatorProps {
  status: string;
  variant?: StatusVariant;
  className?: string;
}

const dotColorMap: Record<StatusVariant, string> = {
  default: "bg-slate-400",
  success: "bg-green-500",
  warning: "bg-amber-500",
  error: "bg-red-500",
  info: "bg-blue-500",
  secondary: "bg-slate-400",
};

export function StatusIndicator({ status, variant, className }: StatusIndicatorProps) {
  const normalizedStatus = status.toLowerCase();
  const resolvedVariant = variant || statusVariantMap[normalizedStatus] || "secondary";

  return (
    <div className={cn("flex items-center gap-2", className)}>
      <span
        className={cn(
          "h-2 w-2 rounded-full",
          dotColorMap[resolvedVariant]
        )}
      />
      <span className="text-sm text-slate-700">{status}</span>
    </div>
  );
}

// ==================== Primary + Secondary Text Cell ====================

interface TextCellProps {
  primary: string;
  secondary?: string;
  className?: string;
}

export function TextCell({ primary, secondary, className }: TextCellProps) {
  return (
    <div className={cn("space-y-0.5", className)}>
      <div className="font-medium text-slate-900">{primary}</div>
      {secondary && (
        <div className="text-sm text-slate-500">{secondary}</div>
      )}
    </div>
  );
}

// ==================== Icon + Text Cell ====================

interface IconTextCellProps {
  icon?: React.ComponentType<{ className?: string }>;
  primary: string;
  secondary?: string;
  className?: string;
}

export function IconTextCell({
  icon: Icon,
  primary,
  secondary,
  className,
}: IconTextCellProps) {
  return (
    <div className={cn("flex items-start gap-3", className)}>
      {Icon && (
        <div className="flex h-10 w-10 shrink-0 items-center justify-center rounded-lg bg-primary/10">
          <Icon className="h-5 w-5 text-primary" />
        </div>
      )}
      <div className="space-y-0.5">
        <div className="font-medium text-primary">{primary}</div>
        {secondary && (
          <div className="text-sm text-slate-500">{secondary}</div>
        )}
      </div>
    </div>
  );
}

// ==================== Date Cell ====================

interface DateCellProps {
  date: string | Date;
  showIcon?: boolean;
  format?: "short" | "long";
  className?: string;
}

export function DateCell({
  date,
  showIcon = true,
  format = "short",
  className,
}: DateCellProps) {
  const dateObj = typeof date === "string" ? new Date(date) : date;
  
  const formattedDate = dateObj.toLocaleDateString("en-US", {
    year: "numeric",
    month: format === "short" ? "2-digit" : "long",
    day: "2-digit",
  });

  return (
    <div className={cn("flex items-center gap-2 text-slate-700", className)}>
      {showIcon && <Calendar className="h-4 w-4 text-slate-400" />}
      <span>{formattedDate}</span>
    </div>
  );
}

// ==================== Contact Cell ====================

interface ContactCellProps {
  name: string;
  phone?: string;
  email?: string;
  className?: string;
}

export function ContactCell({ name, phone, email, className }: ContactCellProps) {
  return (
    <div className={cn("space-y-1", className)}>
      <div className="font-medium text-slate-900">{name}</div>
      {phone && (
        <div className="flex items-center gap-1.5 text-sm text-slate-500">
          <Phone className="h-3.5 w-3.5" />
          <span>{phone}</span>
        </div>
      )}
      {email && (
        <div className="flex items-center gap-1.5 text-sm text-slate-500">
          <Mail className="h-3.5 w-3.5" />
          <span>{email}</span>
        </div>
      )}
    </div>
  );
}

// ==================== Location Cell ====================

interface LocationCellProps {
  address: string;
  city?: string;
  className?: string;
}

export function LocationCell({ address, city, className }: LocationCellProps) {
  return (
    <div className={cn("space-y-0.5", className)}>
      <div className="flex items-center gap-1.5 text-slate-700">
        <MapPin className="h-4 w-4 text-slate-400" />
        <span>{address}</span>
      </div>
      {city && <div className="pl-5.5 text-sm text-slate-500">{city}</div>}
    </div>
  );
}

// ==================== Time Range Cell ====================

interface TimeRangeCellProps {
  start: string;
  end: string;
  className?: string;
}

export function TimeRangeCell({ start, end, className }: TimeRangeCellProps) {
  return (
    <div className={cn("flex items-center gap-1.5 text-slate-500", className)}>
      <Clock className="h-4 w-4 text-slate-400" />
      <span>{start} - {end}</span>
    </div>
  );
}

// ==================== Staff Count Cell ====================

interface StaffCountCellProps {
  count: number;
  label?: string;
  className?: string;
}

export function StaffCountCell({
  count,
  label = "members",
  className,
}: StaffCountCellProps) {
  return (
    <div className={cn("flex items-center gap-1", className)}>
      <span className="font-semibold text-red-600">{count}</span>
      <span className="text-slate-500">{label}</span>
    </div>
  );
}

// ==================== User Cell ====================

interface UserCellProps {
  name: string;
  subtitle?: string;
  avatarUrl?: string;
  className?: string;
}

export function UserCell({ name, subtitle, avatarUrl, className }: UserCellProps) {
  return (
    <div className={cn("flex items-center gap-3", className)}>
      {avatarUrl ? (
        <img
          src={avatarUrl}
          alt={name}
          className="h-8 w-8 rounded-full object-cover"
        />
      ) : (
        <div className="flex h-8 w-8 items-center justify-center rounded-full bg-slate-100">
          <User className="h-4 w-4 text-slate-500" />
        </div>
      )}
      <div className="space-y-0.5">
        <div className="font-medium text-slate-900">{name}</div>
        {subtitle && <div className="text-sm text-slate-500">{subtitle}</div>}
      </div>
    </div>
  );
}

// ==================== Tag/Type Badge Cell ====================

interface TypeBadgeProps {
  type: string;
  className?: string;
}

export function TypeBadge({ type, className }: TypeBadgeProps) {
  return (
    <span
      className={cn(
        "inline-flex items-center rounded-md border border-slate-200 bg-white px-2.5 py-0.5 text-xs font-medium text-slate-700",
        className
      )}
    >
      {type}
    </span>
  );
}
