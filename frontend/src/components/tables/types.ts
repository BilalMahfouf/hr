import type { ColumnDef, SortingState } from "@tanstack/react-table";

// ==================== Backend Contract Types ====================

/**
 * Request parameters for server-side table operations.
 * Maps to the backend TableRequest model.
 */
export interface TableRequest {
  page: number;
  pageSize: number;
  search?: string;
  sortColumn?: string;
  sortOrder?: "asc" | "desc";
}

/**
 * Response structure for paginated data.
 * Maps to the backend PagedList<T> model.
 */
export interface PagedList<T> {
  item: T[];
  totalCount: number;
  pageSize: number;
  page: number;
  hasNextPage: boolean;
  hasPreviousPage: boolean;
}

/**
 * Response structure for cursor-based paginated data.
 * Maps to the backend CursorPagedList<T> model.
 */
export interface CursorPagedList<T> {
  items: T[];
  pageSize: number;
  hasNextPage: boolean;
  hasPreviousPage: boolean;
  nextCursor: string | null;
  previousCursor: string | null;
}

// ==================== Table Configuration Types ====================

/**
 * Extended column definition with additional metadata for server operations.
 * Uses ColumnDef directly for compatibility with TanStack Table.
 */
export type DataTableColumn<TData, TValue = unknown> = ColumnDef<TData, TValue> & {
  /** Column key used for server-side sorting */
  sortKey?: string;
  /** Whether this column is sortable server-side */
  enableServerSorting?: boolean;
};

/**
 * Table state managed by the DataTable component.
 */
export interface TableState {
  pagination: {
    pageIndex: number;
    pageSize: number;
  };
  sorting: SortingState;
  globalFilter: string;
}

/**
 * Filter option for dropdown filters.
 */
export interface FilterOption {
  label: string;
  value: string;
  icon?: React.ComponentType<{ className?: string }>;
}

/**
 * Filter configuration for a column.
 */
export interface ColumnFilter {
  id: string;
  title: string;
  options: FilterOption[];
}

// ==================== DataTable Props ====================

/**
 * Props for the generic DataTable component.
 */
export interface DataTableProps<TData> {
  /** Column definitions for the table */
  columns: DataTableColumn<TData>[];
  /** Data fetching function that returns a PagedList */
  queryFn: (params: TableRequest) => Promise<PagedList<TData>>;
  /** Unique query key for TanStack Query caching */
  queryKey: string;
  /** Search placeholder text */
  searchPlaceholder?: string;
  /** Available page sizes for the user to choose from */
  pageSizes?: number[];
  /** Default page size */
  defaultPageSize?: number;
  /** Column filters configuration */
  filters?: ColumnFilter[];
  /** Actions button configuration - render custom actions dropdown */
  renderActions?: () => React.ReactNode;
  /** Enable/disable global search */
  enableSearch?: boolean;
  /** Custom empty state message */
  emptyMessage?: string;
  /** Callback when row is clicked */
  onRowClick?: (row: TData) => void;
  /** Row action menu renderer */
  renderRowActions?: (row: TData) => React.ReactNode;
  /** Action handlers */
  onView?: (row: TData) => void;
  onEdit?: (row: TData) => void;
  onDelete?: (row: TData) => void;
  /** Additional CSS class for the table container */
  className?: string;
}

// ==================== Pagination Props ====================

export interface DataTablePaginationProps {
  pageIndex: number;
  pageSize: number;
  totalCount: number;
  hasNextPage: boolean;
  hasPreviousPage: boolean;
  onPageChange: (page: number) => void;
  onPageSizeChange: (size: number) => void;
  pageSizes: number[];
}

// ==================== Search Props ====================

export interface DataTableSearchProps {
  value: string;
  onChange: (value: string) => void;
  placeholder?: string;
}

// ==================== Status Badge Types ====================

export type StatusVariant = 
  | "default"
  | "success"
  | "warning"
  | "error"
  | "info"
  | "secondary";

export interface StatusConfig {
  variant: StatusVariant;
  label: string;
}
