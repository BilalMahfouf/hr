import { MoreHorizontal } from "lucide-react";
import { useTranslation } from "react-i18next";
import { Button } from "@/components/ui/button";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu";
import i18nKeyContainer from "@/lib/i18n/keyContainer";

export interface RowAction<TData> {
  label: string;
  onClick: (row: TData) => void;
  icon?: React.ComponentType<{ className?: string }>;
  variant?: "default" | "destructive";
  separator?: boolean;
}

interface DataTableRowActionsProps<TData> {
  row: TData;
  actions: RowAction<TData>[];
}

export function DataTableRowActions<TData>({
  row,
  actions,
}: DataTableRowActionsProps<TData>) {
  const { t } = useTranslation();

  if (actions.length === 0) return null;

  return (
    <DropdownMenu>
      <DropdownMenuTrigger asChild>
        <Button
          variant="ghost"
          size="sm"
          className="h-8 w-8 p-0 bg-white hover:bg-slate-50 text-slate-400 hover:text-slate-600 focus-visible:ring-0"
        >
          <span className="sr-only">{t(i18nKeyContainer.table.openMenu)}</span>
          <MoreHorizontal className="h-4 w-4" />
        </Button>
      </DropdownMenuTrigger>
      <DropdownMenuContent align="end" className="w-40 bg-white border-0 shadow-lg">
        {actions.map((action, index) => (
          <div key={action.label}>
            {action.separator && index > 0 && <DropdownMenuSeparator />}
            <DropdownMenuItem
              onClick={() => action.onClick(row)}
              className={
                action.variant === "destructive"
                  ? "text-red-600 hover:text-red-600 hover:!bg-red-100 focus:text-red-600 focus:!bg-red-100 cursor-pointer"
                  : "hover:!bg-slate-200 focus:!bg-slate-200 cursor-pointer"
              }
            >
              {action.icon && <action.icon className="me-2 h-4 w-4" />}
              {action.label}
            </DropdownMenuItem>
          </div>
        ))}
      </DropdownMenuContent>
    </DropdownMenu>
  );
}
