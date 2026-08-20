import {
  DataTable,
  DataTableColumnHeader,
  DataTableRowActions,
  TextCell,
  type DataTableColumn,
  type RowAction,
} from "@/components/tables";
import { Eye, Wifi } from "lucide-react";
import { useTranslation } from "react-i18next";
import i18nKeyContainer from "@/lib/i18n/keyContainer";
import attendanceApi, { type PunchRecord } from "./attendance-api";

interface PunchesDataTableProps {
  onView?: (punch: PunchRecord) => void;
}

function formatDateTime(iso: string, language: string): string {
  return new Date(iso).toLocaleString(language, {
    year: "numeric",
    month: "short",
    day: "numeric",
    hour: "2-digit",
    minute: "2-digit",
  });
}

export default function PunchesDataTable({ onView }: PunchesDataTableProps) {
  const { t, i18n } = useTranslation();
  const isRtl = i18n.language === "ar";

  const punchesColumns: DataTableColumn<PunchRecord>[] = [
    {
      accessorKey: "employeeFullName",
      header: ({ column }) => (
        <DataTableColumnHeader
          column={column}
          title={t(i18nKeyContainer.attendance.punches.table.employee)}
        />
      ),
      cell: ({ row }) => (
        <TextCell
          primary={row.original.employeeFullName ?? "—"}
          secondary={row.original.employeeId}
        />
      ),
    },
    {
      accessorKey: "machineIp",
      header: ({ column }) => (
        <DataTableColumnHeader
          column={column}
          title={t(i18nKeyContainer.attendance.punches.table.machineIp)}
          enableSorting={false}
        />
      ),
      cell: ({ row }) =>
        row.original.machineIp ? (
          <div className="flex items-center gap-2">
            <Wifi className="h-4 w-4 text-slate-400" />
            <span className="font-mono text-sm text-slate-700">{row.original.machineIp}</span>
          </div>
        ) : (
          <span className="text-slate-400">—</span>
        ),
    },
    {
      accessorKey: "punchOccurredOnUtc",
      header: ({ column }) => (
        <DataTableColumnHeader
          column={column}
          title={t(i18nKeyContainer.attendance.punches.table.punchOccurredOn)}
        />
      ),
      cell: ({ row }) => (
        <span className="text-slate-700">
          {formatDateTime(row.original.punchOccurredOnUtc, i18n.language)}
        </span>
      ),
    },
    {
      id: "actions",
      header: () => (
        <span className="text-xs font-medium uppercase tracking-wide text-slate-500">
          {t(i18nKeyContainer.table.openMenu)}
        </span>
      ),
      cell: ({ row }) => {
        const actions: RowAction<PunchRecord>[] = [
          {
            label: t(i18nKeyContainer.table.viewDetails),
            onClick: (punch) => onView?.(punch),
            icon: Eye,
          },
        ];

        return <DataTableRowActions row={row.original} actions={actions} />;
      },
    },
  ];

  return (
    <div dir={isRtl ? "rtl" : "ltr"}>
      <DataTable
        columns={punchesColumns}
        queryFn={attendanceApi.getAllPunches}
        queryKey="attendance-punches"
        searchPlaceholder={t(i18nKeyContainer.table.search)}
        defaultPageSize={10}
        enableSearch={true}
        emptyMessage={t(i18nKeyContainer.attendance.punches.empty)}
        searchDebounceMs={1000}
      />
    </div>
  );
}