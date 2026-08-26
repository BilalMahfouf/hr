import {
  DataTable,
  DataTableColumnHeader,
  DataTableRowActions,
  TextCell,
  type DataTableColumn,
  type RowAction,
} from "@/components/tables";
import { Eye, Trash2, Shield } from "lucide-react";
import { useTranslation } from "react-i18next";
import i18nKeyContainer from "@/lib/i18n/keyContainer";
import employeeGroupApi, { type EmployeeGroupResponse } from "./employee-group-api";

interface EmployeeGroupsDataTableProps {
  onView?: (group: EmployeeGroupResponse) => void;
}

export default function EmployeeGroupsDataTable({ onView }: EmployeeGroupsDataTableProps) {
  const { t, i18n } = useTranslation();
  const isRtl = i18n.language === "ar";

  const employeeGroupsColumns: DataTableColumn<EmployeeGroupResponse>[] = [
    {
      accessorKey: "name",
      sortKey: "name",
      enableServerSorting: true,
      header: ({ column }) => (
        <DataTableColumnHeader
          column={column}
          title={t(i18nKeyContainer.employeeGroups.list.name)}
        />
      ),
      cell: ({ row }) => (
        <div className="flex items-center gap-3">
          <div className="flex h-10 w-10 shrink-0 items-center justify-center rounded-lg bg-primary/10">
            <Shield className="h-5 w-5 text-primary" />
          </div>
          <TextCell primary={row.original.name} />
        </div>
      ),
    },
    {
      accessorKey: "isSecurity",
      header: ({ column }) => (
        <DataTableColumnHeader
          column={column}
          title={t(i18nKeyContainer.employeeGroups.list.isSecurity)}
          enableSorting={false}
        />
      ),
      cell: ({ row }) => (
        <span
          className={cn(
            "inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium",
            row.original.isSecurity
              ? "bg-amber-100 text-amber-800"
              : "bg-green-100 text-green-800"
          )}
        >
          {row.original.isSecurity
            ? t(i18nKeyContainer.common.active)
            : t(i18nKeyContainer.common.inactive)}
        </span>
      ),
    },
    {
      accessorKey: "rotationStartDate",
      header: ({ column }) => (
        <DataTableColumnHeader
          column={column}
          title={t(i18nKeyContainer.employeeGroups.list.rotationStartDate)}
          enableSorting={false}
        />
      ),
      cell: ({ row }) => <TextCell primary={row.original.rotationStartDate} />,
    },
    {
      accessorKey: "workSchedules.length",
      header: ({ column }) => (
        <DataTableColumnHeader
          column={column}
          title={t(i18nKeyContainer.employeeGroups.list.schedulesCount)}
          enableSorting={false}
        />
      ),
      cell: ({ row }) => <TextCell primary={String(row.original.workSchedules.length)} />,
    },
    {
      accessorKey: "rotationEntries.length",
      header: ({ column }) => (
        <DataTableColumnHeader
          column={column}
          title={t(i18nKeyContainer.employeeGroups.list.rotationsCount)}
          enableSorting={false}
        />
      ),
      cell: ({ row }) => <TextCell primary={String(row.original.rotationEntries.length)} />,
    },
    {
      accessorKey: "createdOnUtc",
      header: ({ column }) => (
        <DataTableColumnHeader
          column={column}
          title={t(i18nKeyContainer.employeeGroups.list.created)}
          enableSorting={false}
        />
      ),
      cell: ({ row }) => {
        const date = new Date(row.original.createdOnUtc);
        return <TextCell primary={date.toLocaleDateString()} />;
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
        const actions: RowAction<EmployeeGroupResponse>[] = [
          {
            label: t(i18nKeyContainer.table.viewDetails),
            onClick: () => onView?.(row.original),
            icon: Eye,
          },
          {
            label: t(i18nKeyContainer.common.delete),
            onClick: () => {}, // Handled by parent with confirmation
            icon: Trash2,
            variant: "destructive",
          },
        ];

        return <DataTableRowActions row={row.original} actions={actions} />;
      },
    },
  ];

  return (
    <div dir={isRtl ? "rtl" : "ltr"}>
      <DataTable
        columns={employeeGroupsColumns}
        queryFn={employeeGroupApi.getAllEmployeeGroups}
        queryKey="employee-groups"
        searchPlaceholder={t(i18nKeyContainer.table.search)}
        defaultPageSize={10}
        enableSearch={true}
        emptyMessage={t(i18nKeyContainer.employeeGroups.empty)}
        searchDebounceMs={1000}
        onView={onView}
      />
    </div>
  );
}