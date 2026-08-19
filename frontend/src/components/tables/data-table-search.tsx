import { Search } from "lucide-react";
import { Input } from "@/components/ui/input";
import { cn } from "@/lib/utils";

interface DataTableSearchProps {
  value: string;
  onChange: (value: string) => void;
  placeholder?: string;
  className?: string;
}

export function DataTableSearch({
  value,
  onChange,
  placeholder = "Search...",
  className,
}: DataTableSearchProps) {
  return (
    <div className={cn("relative flex-1", className)}>
      <Search className="absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
      <Input
        type="text"
        placeholder={placeholder}
        value={value}
        onChange={(e) => onChange(e.target.value)}
        className="pl-9 h-10 border-slate-200 bg-white focus-visible:ring-primary"
      />
    </div>
  );
}
