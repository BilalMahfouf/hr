import {
  DataTable,
  DataTableColumnHeader,
  DataTableRowActions,
  StatusBadge,
  TextCell,
  type DataTableColumn,
  type RowAction,
} from "@/components/tables";
import { Eye, Pencil, ToggleLeft, ToggleRight, Wifi } from "lucide-react";
import { useTranslation } from "react-i18next";
import i18nKeyContainer from "@/lib/i18n/keyContainer";
import attendanceMachineApi, { type MachineRecord } from "./attendance-machine-api";

interface MachinesDataTableProps {
  onView?: (machine: MachineRecord) => void;
  onEdit?: (machine: MachineRecord) => void;
  onActivate?: (machine: MachineRecord) => void;
  onDeactivate?: (machine: MachineRecord) => void;
}

export default function MachinesDataTable({
  onView,
  onEdit,
  onActivate,
  onDeactivate,
}: MachinesDataTableProps) {
  const { t, i18n } = useTranslation();
  const isRtl = i18n.language === "ar";

  const machinesColumns: DataTableColumn<MachineRecord>[] = [
    {
      accessorKey: "machineNumber",
      header: ({ column }) => (
        <DataTableColumnHeader
          column={column}
          title={t(i18nKeyContainer.machines.table.machineNumber)}
        />
      ),
      cell: ({ row }) => <TextCell primary={String(row.original.machineNumber)} />,
    },
    {
      accessorKey: "ipAddress",
      header: ({ column }) => (
        <DataTableColumnHeader
          column={column}
          title={t(i18nKeyContainer.machines.table.ipAddress)}
        />
      ),
      cell: ({ row }) => (
        <div className="flex items-center gap-2">
          <Wifi className="h-4 w-4 text-slate-400" />
          <span className="font-medium text-slate-700">{row.original.ipAddress}</span>
        </div>
      ),
    },
    {
      accessorKey: "port",
      header: ({ column }) => (
        <DataTableColumnHeader
          column={column}
          title={t(i18nKeyContainer.machines.table.port)}
        />
      ),
      cell: ({ row }) => <TextCell primary={String(row.original.port)} />,
    },
    {
      accessorKey: "isActive",
      header: ({ column }) => (
        <DataTableColumnHeader
          column={column}
          title={t(i18nKeyContainer.machines.table.status)}
          enableSorting={false}
        />
      ),
      cell: ({ row }) => {
        const statusLabel = row.original.isActive
          ? t(i18nKeyContainer.machines.status.active)
          : t(i18nKeyContainer.machines.status.inactive);

        return (
          <StatusBadge
            status={statusLabel}
            variant={row.original.isActive ? "success" : "secondary"}
          />
        );
      },
    },
    {
      id: "actions",
      header: () => (
        <span className="text-xs font-medium uppercase tracking-wide text-slate-500">
          {t(i18nKeyContainer.table.openMenu)}
        </span>
      ),
      cell: ({ row }) => {
        const actions: RowAction<MachineRecord>[] = [
          {
            label: t(i18nKeyContainer.table.viewDetails),
            onClick: (machine) => onView?.(machine),
            icon: Eye,
          },
          {
            label: t(i18nKeyContainer.table.edit),
            onClick: (machine) => onEdit?.(machine),
            icon: Pencil,
          },
          {
            label: row.original.isActive
              ? t(i18nKeyContainer.machines.actions.deactivate)
              : t(i18nKeyContainer.machines.actions.activate),
            onClick: (machine) =>
              machine.isActive ? onDeactivate?.(machine) : onActivate?.(machine),
            icon: row.original.isActive ? ToggleLeft : ToggleRight,
            separator: true,
          },
        ];

        return <DataTableRowActions row={row.original} actions={actions} />;
      },
    },
  ];

  return (
    <div dir={isRtl ? "rtl" : "ltr"}>
      <DataTable
        columns={machinesColumns}
        queryFn={attendanceMachineApi.getAllMachines}
        queryKey="machines"
        searchPlaceholder={t(i18nKeyContainer.table.search)}
        defaultPageSize={10}
        enableSearch={true}
        emptyMessage={t(i18nKeyContainer.machines.empty)}
        searchDebounceMs={1000}
      />
    </div>
  );
}