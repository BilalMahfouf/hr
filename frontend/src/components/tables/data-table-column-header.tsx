import { ArrowDown, ArrowUp, ArrowUpDown } from "lucide-react";
import type { Column } from "@tanstack/react-table";
import { cn } from "@/lib/utils";
import { Button } from "@/components/ui/button";

interface DataTableColumnHeaderProps<TData, TValue>
  extends React.HTMLAttributes<HTMLDivElement> {
  column: Column<TData, TValue>;
  title: string;
  enableSorting?: boolean;
}

export function DataTableColumnHeader<TData, TValue>({
  column,
  title,
  className,
  enableSorting = true,
}: DataTableColumnHeaderProps<TData, TValue>) {
  if (!enableSorting || !column.getCanSort()) {
    return (
      <div className={cn("text-xs font-medium uppercase tracking-wide text-slate-500", className)}>
        {title}
      </div>
    );
  }

  return (
    <div className={cn("flex items-center space-x-2", className)}>
      <Button
        variant="ghost"
        size="sm"
        className="-ml-3 h-8 data-[state=open]:bg-accent hover:bg-transparent"
        onClick={() => column.toggleSorting(column.getIsSorted() === "asc")}
      >
        <span className="text-xs font-medium uppercase tracking-wide text-slate-500">
          {title}
        </span>
        {column.getIsSorted() === "desc" ? (
          <ArrowDown className="ml-2 h-3.5 w-3.5 text-slate-500" />
        ) : column.getIsSorted() === "asc" ? (
          <ArrowUp className="ml-2 h-3.5 w-3.5 text-slate-500" />
        ) : (
          <ArrowUpDown className="ml-2 h-3.5 w-3.5 text-slate-400" />
        )}
      </Button>
    </div>
  );
}
