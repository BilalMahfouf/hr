import { Label } from "@/components/ui/label";
import { Input } from "@/components/ui/input";
import { Calendar } from "lucide-react";
import { cn } from "@/lib/utils";

interface DateInputProps {
  label: string;
  value: string;
  onChange: (value: string) => void;
  placeholder?: string;
  error?: string;
  required?: boolean;
  disabled?: boolean;
  min?: string;
  max?: string;
  id: string;
  className?: string;
}

export default function DateInput({
  label,
  value,
  onChange,
  placeholder,
  error,
  required,
  disabled,
  min,
  max,
  id,
  className,
}: DateInputProps) {
  return (
    <div className={cn("space-y-1.5", className)}>
      <Label htmlFor={id} className="text-sm font-medium text-slate-700">
        {label}
        {required && <span className="text-red-500 ml-1">*</span>}
      </Label>
      <div className="relative">
        <Calendar className="absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-slate-400 pointer-events-none" />
        <Input
          id={id}
          type="date"
          value={value}
          onChange={(e) => onChange(e.target.value)}
          placeholder={placeholder}
          disabled={disabled}
          min={min}
          max={max}
          className={cn(
            "h-11 border-slate-200 bg-slate-50 pl-10 focus:bg-white focus:border-primary focus:ring-1 focus:ring-primary",
            error && "border-red-300 focus:border-red-500 focus:ring-red-500",
            disabled && "bg-slate-100 text-slate-500 cursor-not-allowed"
          )}
          aria-invalid={error ? "true" : "false"}
          aria-describedby={error ? `${id}-error` : undefined}
        />
      </div>
      {error && (
        <p id={`${id}-error`} className="text-sm text-red-600" role="alert">
          {error}
        </p>
      )}
    </div>
  );
}