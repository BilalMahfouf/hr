// Main DataTable component
export { DataTable } from "./data-table";

// Sub-components
export { DataTableSearch } from "./data-table-search";
export { DataTablePagination } from "./data-table-pagination";
export { DataTableSkeleton } from "./data-table-skeleton";
export { DataTableColumnHeader } from "./data-table-column-header";
export { DataTableRowActions, type RowAction } from "./data-table-row-actions";

// Cell Renderers (for common UI patterns)
export {
  StatusBadge,
  StatusIndicator,
  TextCell,
  IconTextCell,
  DateCell,
  ContactCell,
  LocationCell,
  TimeRangeCell,
  StaffCountCell,
  UserCell,
  TypeBadge,
} from "./cell-renderers";

// Hooks
export { useTableQuery } from "./use-table-query";

// Types
export type {
  TableRequest,
  PagedList,
  DataTableColumn,
  TableState,
  FilterOption,
  ColumnFilter,
  DataTableProps,
  DataTablePaginationProps,
  DataTableSearchProps,
  StatusVariant,
  StatusConfig,
} from "./types";
