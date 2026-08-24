import {
  DataTable,
  DataTableColumnHeader,
  DataTableRowActions,
  TextCell,
  type DataTableColumn,
  type RowAction,
} from "@/components/tables";
import { Eye, IdCard, User } from "lucide-react";
import { useTranslation } from "react-i18next";
import i18nKeyContainer from "@/lib/i18n/keyContainer";
import employeesApi, { type EmployeeListItem } from "./employees-api";

interface EmployeesDataTableProps {
  onView?: (employee: EmployeeListItem) => void;
}

export default function EmployeesDataTable({ onView }: EmployeesDataTableProps) {
  const { t, i18n } = useTranslation();
  const isRtl = i18n.language === "ar";

  const employeesColumns: DataTableColumn<EmployeeListItem>[] = [
    {
      accessorKey: "lastName",
      sortKey: "lastName",
      enableServerSorting: true,
      header: ({ column }) => (
        <DataTableColumnHeader
          column={column}
          title={t(i18nKeyContainer.employees.table.name)}
        />
      ),
      cell: ({ row }) => (
        <div className="flex items-start gap-3">
          <div className="flex h-10 w-10 shrink-0 items-center justify-center rounded-lg bg-primary/10">
            <User className="h-5 w-5 text-primary" />
          </div>
          <TextCell
            primary={`${row.original.firstName} ${row.original.lastName}`}
          />
        </div>
      ),
    },
    {
      accessorKey: "matricule",
      header: ({ column }) => (
        <DataTableColumnHeader
          column={column}
          title={t(i18nKeyContainer.employees.table.matricule)}
          enableSorting={false}
        />
      ),
      cell: ({ row }) => (
        <div className="flex items-center gap-2">
          <IdCard className="h-4 w-4 text-slate-400" />
          <span className="font-mono text-sm font-medium text-slate-700">
            {row.original.matricule}
          </span>
        </div>
      ),
    },
    {
      accessorKey: "group",
      header: ({ column }) => (
        <DataTableColumnHeader
          column={column}
          title={t(i18nKeyContainer.employees.table.group)}
          enableSorting={false}
        />
      ),
      cell: ({ row }) => <TextCell primary={row.original.group ?? "—"} />,
    },
    {
      accessorKey: "department",
      header: ({ column }) => (
        <DataTableColumnHeader
          column={column}
          title={t(i18nKeyContainer.employees.table.department)}
          enableSorting={false}
        />
      ),
      cell: ({ row }) => <TextCell primary={row.original.department ?? "—"} />,
    },
    {
      accessorKey: "phone",
      header: ({ column }) => (
        <DataTableColumnHeader
          column={column}
          title={t(i18nKeyContainer.employees.table.phone)}
          enableSorting={false}
        />
      ),
      cell: ({ row }) => <TextCell primary={row.original.phone ?? "—"} />,
    },
    {
      id: "actions",
      header: () => (
        <span className="text-xs font-medium uppercase tracking-wide text-slate-500">
          {t(i18nKeyContainer.table.openMenu)}
        </span>
      ),
      cell: ({ row }) => {
        const actions: RowAction<EmployeeListItem>[] = [
          {
            label: t(i18nKeyContainer.table.viewDetails),
            onClick: () => onView?.(row.original),
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
        columns={employeesColumns}
        queryFn={employeesApi.getAllEmployees}
        queryKey="employees"
        searchPlaceholder={t(i18nKeyContainer.table.search)}
        defaultPageSize={10}
        enableSearch={true}
        emptyMessage={t(i18nKeyContainer.table.noResults)}
        searchDebounceMs={1000}
        onView={onView}
      />
    </div>
  );
}
