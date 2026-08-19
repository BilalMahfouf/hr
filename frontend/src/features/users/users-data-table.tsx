import {
  DataTable,
  DataTableColumnHeader,
  DataTableRowActions,
  DateCell,
  StatusBadge,
  TextCell,
  type DataTableColumn,
  type RowAction,
} from "@/components/tables";
import { Eye, Mail, Shield, User } from "lucide-react";
import { useTranslation } from "react-i18next";
import i18nKeyContainer from "@/lib/i18n/keyContainer";
import usersApi, { type UserRecord } from "./users-api";

interface UsersDataTableProps {
  onView?: (user: UserRecord) => void;
}

export default function UsersDataTable({ onView }: UsersDataTableProps) {
  const { t, i18n } = useTranslation();
  const isRtl = i18n.language === "ar";

  const usersColumns: DataTableColumn<UserRecord>[] = [
    {
      accessorKey: "fullName",
      header: ({ column }) => (
        <DataTableColumnHeader
          column={column}
          title={t(i18nKeyContainer.user.fullName)}
          enableSorting={false}
        />
      ),
      cell: ({ row }) => (
        <div className="flex items-start gap-3">
          <div className="flex h-10 w-10 shrink-0 items-center justify-center rounded-lg bg-primary/10">
            <User className="h-5 w-5 text-primary" />
          </div>
          <div className="space-y-0.5">
            <div className="font-medium text-slate-900">{row.original.fullName}</div>
            <div className="flex items-center gap-1.5 text-sm text-slate-500">
              <Mail className="h-3.5 w-3.5" />
              <span>{row.original.email}</span>
            </div>
          </div>
        </div>
      ),
    },
    {
      accessorKey: "userName",
      header: ({ column }) => (
        <DataTableColumnHeader
          column={column}
          title={t(i18nKeyContainer.settingsPage.profile.username)}
          enableSorting={false}
        />
      ),
      cell: ({ row }) => <TextCell primary={row.original.userName} />,
    },
    {
      accessorKey: "role",
      header: ({ column }) => (
        <DataTableColumnHeader
          column={column}
          title={t(i18nKeyContainer.staff.role)}
          enableSorting={false}
        />
      ),
      cell: ({ row }) => (
        <div className="flex items-center gap-2">
          <Shield className="h-4 w-4 text-slate-400" />
          <span className="font-medium text-slate-700">{row.original.role}</span>
        </div>
      ),
    },
    {
      accessorKey: "isActive",
      header: ({ column }) => (
        <DataTableColumnHeader
          column={column}
          title={t(i18nKeyContainer.staff.status)}
          enableSorting={false}
        />
      ),
      cell: ({ row }) => {
        const statusLabel = row.original.isActive
          ? t(i18nKeyContainer.staff.active)
          : t(i18nKeyContainer.staff.inactive);

        return (
          <StatusBadge
            status={statusLabel}
            variant={row.original.isActive ? "success" : "secondary"}
          />
        );
      },
    },
    {
      accessorKey: "createdOnUtc",
      header: ({ column }) => (
        <DataTableColumnHeader
          column={column}
          title={t(i18nKeyContainer.user.tableRegistered)}
          enableSorting={false}
        />
      ),
      cell: ({ row }) => <DateCell date={row.original.createdOnUtc} />,
    },
    {
      id: "actions",
      header: () => (
        <span className="text-xs font-medium uppercase tracking-wide text-slate-500">
          {t(i18nKeyContainer.table.openMenu)}
        </span>
      ),
      cell: ({ row }) => {
        const actions: RowAction<UserRecord>[] = [
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
        columns={usersColumns}
        queryFn={usersApi.getAllUsers}
        queryKey="users"
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
