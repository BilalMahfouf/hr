import { useQuery, keepPreviousData } from "@tanstack/react-query";
import { useState, useCallback, useMemo } from "react";
import type { SortingState } from "@tanstack/react-table";
import type { TableRequest, PagedList, TableState } from "./types";
import { isNotFoundError } from "@/lib/api/error-types";

interface UseTableQueryOptions<TData> {
  /** Unique query key for caching */
  queryKey: string;
  /** Function to fetch data from the server */
  queryFn: (params: TableRequest) => Promise<PagedList<TData>>;
  /** Default page size */
  defaultPageSize?: number;
  /** Debounce time for search in milliseconds */
  searchDebounceMs?: number;
}

interface UseTableQueryReturn<TData> {
  /** Current data from the server */
  data: PagedList<TData> | undefined;
  /** Whether the initial data is loading */
  isLoading: boolean;
  /** Whether data is being refetched */
  isFetching: boolean;
  /** Error from the query */
  error: Error | null;
  /** Current table state */
  state: TableState;
  /** Set the current page (0-indexed) */
  setPageIndex: (pageIndex: number) => void;
  /** Set the page size */
  setPageSize: (pageSize: number) => void;
  /** Set the sorting state */
  setSorting: (sorting: SortingState | ((prev: SortingState) => SortingState)) => void;
  /** Set the global filter/search */
  setGlobalFilter: (filter: string) => void;
  /** Reset all table state to defaults */
  resetState: () => void;
  /** Refetch data manually */
  refetch: () => void;
}

/**
 * Custom hook for managing server-side table state with TanStack Query.
 * Handles pagination, sorting, and searching with proper caching.
 */
export function useTableQuery<TData>({
  queryKey,
  queryFn,
  defaultPageSize = 10,
}: UseTableQueryOptions<TData>): UseTableQueryReturn<TData> {
  // Table state
  const [pageIndex, setPageIndex] = useState(0);
  const [pageSize, setPageSize] = useState(defaultPageSize);
  const [sorting, setSorting] = useState<SortingState>([]);
  const [globalFilter, setGlobalFilter] = useState("");

  // Build request parameters
  const requestParams = useMemo((): TableRequest => {
    const sortColumn = sorting.length > 0 ? sorting[0].id : undefined;
    const sortOrder = sorting.length > 0 
      ? (sorting[0].desc ? "desc" : "asc") 
      : undefined;

    return {
      page: pageIndex + 1, // Backend uses 1-indexed pages
      pageSize,
      search: globalFilter || undefined,
      sortColumn,
      sortOrder,
    };
  }, [pageIndex, pageSize, sorting, globalFilter]);

  // Query with automatic caching and background updates
  const { data: queryData, isLoading, isFetching, error, refetch } = useQuery({
    queryKey: [queryKey, requestParams],
    queryFn: () => queryFn(requestParams),
    placeholderData: keepPreviousData,
    staleTime: 10000, // Consider data fresh for 10 seconds
    retry: (count: number, err: unknown) => {
      // Don't retry on 404
      if (isNotFoundError(err)) return false;
      return count < 2;
    },
  });

  // Return empty data on 404 instead of keeping previous data
  const data = useMemo(() => {
    if (error && isNotFoundError(error)) {
      return { 
        item: [], 
        totalCount: 0, 
        page: 1, 
        pageSize, 
        hasNextPage: false, 
        hasPreviousPage: false 
      } as PagedList<TData>;
    }
    return queryData;
  }, [queryData, error, pageSize]);

  // State object for TanStack Table
  const state = useMemo((): TableState => ({
    pagination: {
      pageIndex,
      pageSize,
    },
    sorting,
    globalFilter,
  }), [pageIndex, pageSize, sorting, globalFilter]);

  // Reset page when search or sorting changes
  const handleSetGlobalFilter = useCallback((filter: string) => {
    setGlobalFilter(filter);
    setPageIndex(0); // Reset to first page on search
  }, []);

  const handleSetSorting = useCallback((
    sortingOrUpdater: SortingState | ((prev: SortingState) => SortingState)
  ) => {
    setSorting(sortingOrUpdater);
    setPageIndex(0); // Reset to first page on sort change
  }, []);

  const handleSetPageSize = useCallback((size: number) => {
    setPageSize(size);
    setPageIndex(0); // Reset to first page on page size change
  }, []);

  const resetState = useCallback(() => {
    setPageIndex(0);
    setPageSize(defaultPageSize);
    setSorting([]);
    setGlobalFilter("");
  }, [defaultPageSize]);

  return {
    data,
    isLoading,
    isFetching,
    error: error as Error | null,
    state,
    setPageIndex,
    setPageSize: handleSetPageSize,
    setSorting: handleSetSorting,
    setGlobalFilter: handleSetGlobalFilter,
    resetState,
    refetch,
  };
}
