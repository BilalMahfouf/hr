import { ChevronLeft, ChevronRight } from "lucide-react";
import { useTranslation } from "react-i18next";
import { Button } from "@/components/ui/button";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import { cn } from "@/lib/utils";
import i18nKeyContainer from "@/lib/i18n/keyContainer";

interface DataTablePaginationProps {
  pageIndex: number;
  pageSize: number;
  totalCount: number;
  hasNextPage: boolean;
  hasPreviousPage: boolean;
  onPageChange: (page: number) => void;
  onPageSizeChange: (size: number) => void;
  pageSizes: number[];
  className?: string;
}

export function DataTablePagination({
  pageIndex,
  pageSize,
  totalCount,
  hasNextPage,
  hasPreviousPage,
  onPageChange,
  onPageSizeChange,
  pageSizes,
  className,
}: DataTablePaginationProps) {
  const { t } = useTranslation();
  const totalPages = Math.ceil(totalCount / pageSize);
  const currentPage = pageIndex + 1; // Display as 1-indexed

  // Generate page numbers to display
  const getPageNumbers = () => {
    const pages: (number | "ellipsis")[] = [];
    const showPages = 5; // Max number of page buttons to show
    
    if (totalPages <= showPages) {
      // Show all pages if total is small
      for (let i = 1; i <= totalPages; i++) {
        pages.push(i);
      }
    } else {
      // Always show first page
      pages.push(1);

      if (currentPage > 3) {
        pages.push("ellipsis");
      }

      // Show pages around current
      const start = Math.max(2, currentPage - 1);
      const end = Math.min(totalPages - 1, currentPage + 1);

      for (let i = start; i <= end; i++) {
        pages.push(i);
      }

      if (currentPage < totalPages - 2) {
        pages.push("ellipsis");
      }

      // Always show last page
      if (totalPages > 1) {
        pages.push(totalPages);
      }
    }

    return pages;
  };

  const pageNumbers = getPageNumbers();

  return (
    <div
      className={cn(
        "flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-center py-4",
        className
      )}
    >
      {/* Page size selector - just the select, white background */}
      <Select
        value={pageSize.toString()}
        onValueChange={(value) => onPageSizeChange(Number(value))}
      >
        <SelectTrigger className="h-8 w-16 bg-white border-slate-200">
          <SelectValue placeholder={pageSize} />
        </SelectTrigger>
        <SelectContent side="top" className="bg-white border-0 shadow-lg">
          {pageSizes.map((size) => (
            <SelectItem key={size} value={size.toString()}>
              {size}
            </SelectItem>
          ))}
        </SelectContent>
      </Select>

      {/* Pagination controls */}
      <div className="flex items-center justify-center gap-1">
        {/* Previous button */}
        <Button
          variant="ghost"
          size="sm"
          onClick={() => onPageChange(pageIndex - 1)}
          disabled={!hasPreviousPage}
          className="gap-1 text-slate-600 hover:text-slate-900"
        >
          <ChevronLeft className="h-4 w-4 rtl:rotate-180" />
          <span className="hidden sm:inline">{t(i18nKeyContainer.table.previous)}</span>
        </Button>

        {/* Page numbers */}
        <div className="flex items-center gap-1">
          {pageNumbers.map((page, index) => {
            if (page === "ellipsis") {
              return (
                <span
                  key={`ellipsis-${index}`}
                  className="px-2 text-muted-foreground"
                >
                  ...
                </span>
              );
            }

            const isActive = page === currentPage;
            return (
              <Button
                key={page}
                variant={isActive ? "outline" : "ghost"}
                size="sm"
                onClick={() => onPageChange(page - 1)}
                className={cn(
                  "h-8 w-8 p-0",
                  isActive &&
                    "border-slate-300 bg-white font-medium text-slate-900 hover:bg-slate-50"
                )}
              >
                {page}
              </Button>
            );
          })}
        </div>

        {/* Next button */}
        <Button
          variant="ghost"
          size="sm"
          onClick={() => onPageChange(pageIndex + 1)}
          disabled={!hasNextPage}
          className="gap-1 text-slate-600 hover:text-slate-900"
        >
          <span className="hidden sm:inline">{t(i18nKeyContainer.table.next)}</span>
          <ChevronRight className="h-4 w-4 rtl:rotate-180" />
        </Button>
      </div>
    </div>
  );
}
