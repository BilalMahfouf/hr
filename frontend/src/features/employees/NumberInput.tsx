import { Label } from "@/components/ui/label";
import { Input } from "@/components/ui/input";
import { cn } from "@/lib/utils";

interface NumberInputProps {
  label: string;
  value: string;
  onChange: (value: string) => void;
  placeholder?: string;
  error?: string;
  required?: boolean;
  disabled?: boolean;
  id: string;
  min?: number;
  max?: number;
  step?: number;
  className?: string;
}

export default function NumberInput({
  label,
  value,
  onChange,
  placeholder,
  error,
  required,
  disabled,
  id,
  min,
  max,
  step = 1,
  className,
}: NumberInputProps) {
  return (
    <div className={cn("space-y-1.5", className)}>
      <Label htmlFor={id} className="text-sm font-medium text-slate-700">
        {label}
        {required && <span className="text-red-500 ml-1">*</span>}
      </Label>
      <Input
        id={id}
        type="number"
        value={value}
        onChange={(e) => onChange(e.target.value)}
        placeholder={placeholder}
        disabled={disabled}
        min={min}
        max={max}
        step={step}
        className={cn(
          "h-11 border-slate-200 bg-slate-50 focus:bg-white focus:border-primary focus:ring-1 focus:ring-primary",
          error && "border-red-300 focus:border-red-500 focus:ring-red-500",
          disabled && "bg-slate-100 text-slate-500 cursor-not-allowed"
        )}
        aria-invalid={error ? "true" : "false"}
        aria-describedby={error ? `${id}-error` : undefined}
      />
      {error && (
        <p id={`${id}-error`} className="text-sm text-red-600" role="alert">
          {error}
        </p>
      )}
    </div>
  );
}