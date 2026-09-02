import { useMemo, useCallback, useState, useEffect } from "react";
import {
  useReactTable,
  getCoreRowModel,
  flexRender,
  type ColumnDef,
  type SortingState,
} from "@tanstack/react-table";
import { useTranslation } from "react-i18next";
import { Loader2 } from "lucide-react";
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table";
import { cn } from "@/lib/utils";
import { useTableQuery } from "./use-table-query";
import { DataTableSearch } from "./data-table-search";
import { DataTablePagination } from "./data-table-pagination";
import { DataTableSkeleton } from "./data-table-skeleton";
import type { TableRequest, PagedList, DataTableColumn } from "./types";
import i18nKeyContainer from "@/lib/i18n/keyContainer";

// ==================== Component Props ====================

interface DataTableProps<TData> {
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
  /** Enable/disable global search */
  enableSearch?: boolean;
  /** Custom empty state message */
  emptyMessage?: string;
  /** Callback when row is clicked */
  onRowClick?: (row: TData) => void;
  /** Action handlers */
  onView?: (row: TData) => void;
  onEdit?: (row: TData) => void;
  onDelete?: (row: TData) => void;
  /** Additional CSS class for the table container */
  className?: string;
  /** Debounce time for search (ms) */
  searchDebounceMs?: number;
}

// ==================== Debounce Hook ====================

function useDebounce<T>(value: T, delay: number): T {
  const [debouncedValue, setDebouncedValue] = useState<T>(value);

  useEffect(() => {
    const handler = setTimeout(() => {
      setDebouncedValue(value);
    }, delay);

    return () => {
      clearTimeout(handler);
    };
  }, [value, delay]);

  return debouncedValue;
}

// ==================== DataTable Component ====================

export function DataTable<TData>({
  columns,
  queryFn,
  queryKey,
  searchPlaceholder,
  pageSizes = [5, 10, 20, 50],
  defaultPageSize = 10,
  enableSearch = true,
  emptyMessage,
  onRowClick,
  // Action handlers are passed through columns, not used directly here
  onView: _onView,
  onEdit: _onEdit,
  onDelete: _onDelete,
  className,
  searchDebounceMs = 300,
}: DataTableProps<TData>) {
  const { t, i18n } = useTranslation();
  const isRtl = i18n.language === "ar";
  
  // Local search state for immediate UI feedback
  const [searchInput, setSearchInput] = useState("");
  const debouncedSearch = useDebounce(searchInput, searchDebounceMs);

  // Server-side table state management
  const {
    data,
    isLoading,
    isFetching,
    state,
    setPageIndex,
    setPageSize,
    setSorting,
    setGlobalFilter,
  } = useTableQuery<TData>({
    queryKey,
    queryFn,
    defaultPageSize,
  });

  // Sync debounced search with query
  useEffect(() => {
    setGlobalFilter(debouncedSearch);
  }, [debouncedSearch, setGlobalFilter]);

  // Memoize columns to prevent unnecessary re-renders
  const tableColumns = useMemo(
    () => columns as ColumnDef<TData>[],
    [columns]
  );

  // Memoize data
  const tableData = useMemo(() => data?.item ?? [], [data?.item]);

  // Handle sorting change
  const handleSortingChange = useCallback(
    (updaterOrValue: SortingState | ((old: SortingState) => SortingState)) => {
      setSorting(updaterOrValue);
    },
    [setSorting]
  );

  // TanStack Table instance
  const table = useReactTable({
    data: tableData,
    columns: tableColumns,
    pageCount: data ? Math.ceil(data.totalCount / data.pageSize) : -1,
    state: {
      sorting: state.sorting,
      pagination: state.pagination,
    },
    onSortingChange: handleSortingChange,
    getCoreRowModel: getCoreRowModel(),
    manualPagination: true,
    manualSorting: true,
    manualFiltering: true,
  });

  // Row click handler
  const handleRowClick = useCallback(
    (row: TData) => {
      if (onRowClick) {
        onRowClick(row);
      }
    },
    [onRowClick]
  );

  return (
    <div className={cn("space-y-4", className)} dir={isRtl ? "rtl" : "ltr"}>
      {/* Toolbar: Search only */}
      {enableSearch && (
        <div className="flex items-center">
          <DataTableSearch
            value={searchInput}
            onChange={setSearchInput}
            placeholder={searchPlaceholder || t(i18nKeyContainer.table.search)}
            className="max-w-md"
          />
        </div>
      )}

      {/* Table */}
      {isLoading ? (
        <DataTableSkeleton
          columnCount={columns.length}
          rowCount={defaultPageSize > 5 ? 5 : defaultPageSize}
          hasActions={true}
        />
      ) : (
        <div className="relative">
          {/* Loading overlay when refetching */}
          {isFetching && (
            <div className="absolute inset-0 z-10 flex items-center justify-center bg-white/70 rounded-lg">
              <div className="flex items-center gap-2 rounded-lg bg-white px-4 py-2 shadow-md">
                <Loader2 className="h-5 w-5 animate-spin text-primary" />
                <span className="text-sm font-medium text-slate-600">{t(i18nKeyContainer.common.loading)}</span>
              </div>
            </div>
          )}
          <div
            className={cn(
              "rounded-lg border border-slate-200 bg-white transition-opacity",
              isFetching && "opacity-50"
            )}
          >
          <Table>
            <TableHeader>
              {table.getHeaderGroups().map((headerGroup) => (
                <TableRow
                  key={headerGroup.id}
                  className="bg-slate-50/50 hover:bg-slate-50/50 border-b border-slate-200"
                >
                  {headerGroup.headers.map((header) => (
                    <TableHead
                      key={header.id}
                      className={cn(
                        "h-12 px-4 text-xs font-medium uppercase tracking-wide text-slate-500",
                        header.column.getCanSort() && "cursor-pointer select-none hover:bg-slate-100"
                      )}
                      onClick={header.column.getToggleSortingHandler()}
                    >
                      {header.isPlaceholder
                        ? null
                        : flexRender(
                            header.column.columnDef.header,
                            header.getContext()
                          )}
                    </TableHead>
                  ))}
                </TableRow>
              ))}
            </TableHeader>
            <TableBody>
              {table.getRowModel().rows?.length ? (
                table.getRowModel().rows.map((row) => (
                  <TableRow
                    key={row.id}
                    data-state={row.getIsSelected() && "selected"}
                    className={cn(
                      "border-b border-slate-100 transition-colors",
                      onRowClick && "cursor-pointer hover:bg-slate-50"
                    )}
                    onClick={() => handleRowClick(row.original)}
                  >
                    {row.getVisibleCells().map((cell) => (
                      <TableCell
                        key={cell.id}
                        className="px-4 py-4"
                      >
                        {flexRender(
                          cell.column.columnDef.cell,
                          cell.getContext()
                        )}
                      </TableCell>
                    ))}
                  </TableRow>
                ))
              ) : (
                <TableRow>
                  <TableCell
                    colSpan={columns.length}
                    className="h-24 text-center text-muted-foreground"
                  >
                    {emptyMessage || t(i18nKeyContainer.table.noResults)}
                  </TableCell>
                </TableRow>
              )}
            </TableBody>
          </Table>
        </div>
        </div>
      )}

      {/* Pagination */}
      {data && data.totalCount > 0 && (
        <DataTablePagination
          pageIndex={state.pagination.pageIndex}
          pageSize={state.pagination.pageSize}
          totalCount={data.totalCount}
          hasNextPage={data.hasNextPage}
          hasPreviousPage={data.hasPreviousPage}
          onPageChange={setPageIndex}
          onPageSizeChange={setPageSize}
          pageSizes={pageSizes}
        />
      )}
    </div>
  );
}
