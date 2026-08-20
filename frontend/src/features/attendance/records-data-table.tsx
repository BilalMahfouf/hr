import {
  DataTable,
  DataTableColumnHeader,
  DataTableRowActions,
  StatusBadge,
  TextCell,
  type DataTableColumn,
  type RowAction,
} from "@/components/tables";
import { Eye } from "lucide-react";
import { useTranslation } from "react-i18next";
import i18nKeyContainer from "@/lib/i18n/keyContainer";
import attendanceApi, { type AttendanceRecord } from "./attendance-api";

interface AttendanceRecordsDataTableProps {
  onView?: (record: AttendanceRecord) => void;
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

export default function AttendanceRecordsDataTable({
  onView,
}: AttendanceRecordsDataTableProps) {
  const { t, i18n } = useTranslation();
  const isRtl = i18n.language === "ar";

  const recordsColumns: DataTableColumn<AttendanceRecord>[] = [
    {
      accessorKey: "employeeFullName",
      header: ({ column }) => (
        <DataTableColumnHeader
          column={column}
          title={t(i18nKeyContainer.attendance.records.table.employee)}
        />
      ),
      cell: ({ row }) => (
        <TextCell
          primary={row.original.employeeFullName}
          secondary={row.original.employeeId}
        />
      ),
    },
    {
      accessorKey: "checkInAt",
      header: ({ column }) => (
        <DataTableColumnHeader
          column={column}
          title={t(i18nKeyContainer.attendance.records.table.checkIn)}
        />
      ),
      cell: ({ row }) => (
        <span className="text-slate-700">
          {formatDateTime(row.original.checkInAt, i18n.language)}
        </span>
      ),
    },
    {
      accessorKey: "checkOutAt",
      header: ({ column }) => (
        <DataTableColumnHeader
          column={column}
          title={t(i18nKeyContainer.attendance.records.table.checkOut)}
        />
      ),
      cell: ({ row }) =>
        row.original.checkOutAt ? (
          <span className="text-slate-700">
            {formatDateTime(row.original.checkOutAt, i18n.language)}
          </span>
        ) : (
          <StatusBadge
            status={t(i18nKeyContainer.attendance.status.open)}
            variant="info"
          />
        ),
    },
    {
      accessorKey: "workedTime",
      header: ({ column }) => (
        <DataTableColumnHeader
          column={column}
          title={t(i18nKeyContainer.attendance.records.table.workedTime)}
        />
      ),
      cell: ({ row }) => (
        <span className="font-mono text-sm text-slate-700">
          {row.original.workedTime}
        </span>
      ),
    },
    {
      accessorKey: "isAbsent",
      header: ({ column }) => (
        <DataTableColumnHeader
          column={column}
          title={t(i18nKeyContainer.attendance.records.table.status)}
          enableSorting={false}
        />
      ),
      cell: ({ row }) => {
        const statusLabel = row.original.isAbsent
          ? t(i18nKeyContainer.attendance.status.absent)
          : t(i18nKeyContainer.attendance.status.present);

        return (
          <StatusBadge
            status={statusLabel}
            variant={row.original.isAbsent ? "error" : "success"}
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
        const actions: RowAction<AttendanceRecord>[] = [
          {
            label: t(i18nKeyContainer.table.viewDetails),
            onClick: (record) => onView?.(record),
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
        columns={recordsColumns}
        queryFn={attendanceApi.getAllAttendanceRecords}
        queryKey="attendance-records"
        searchPlaceholder={t(i18nKeyContainer.table.search)}
        defaultPageSize={10}
        enableSearch={true}
        emptyMessage={t(i18nKeyContainer.attendance.records.empty)}
        searchDebounceMs={1000}
      />
    </div>
  );
}