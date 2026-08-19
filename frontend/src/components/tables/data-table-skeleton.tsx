import { Skeleton } from "@/components/ui/skeleton";
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table";

interface DataTableSkeletonProps {
  columnCount: number;
  rowCount?: number;
  hasActions?: boolean;
}

export function DataTableSkeleton({
  columnCount,
  rowCount = 5,
  hasActions = false,
}: DataTableSkeletonProps) {
  return (
    <div className="rounded-lg border border-slate-200 bg-white">
      <Table>
        <TableHeader>
          <TableRow className="bg-slate-50/50 hover:bg-slate-50/50">
            {Array.from({ length: columnCount }).map((_, index) => (
              <TableHead key={index} className="h-12">
                <Skeleton className="h-4 w-24" />
              </TableHead>
            ))}
            {hasActions && (
              <TableHead className="h-12 w-25">
                <Skeleton className="h-4 w-16 ml-auto" />
              </TableHead>
            )}
          </TableRow>
        </TableHeader>
        <TableBody>
          {Array.from({ length: rowCount }).map((_, rowIndex) => (
            <TableRow key={rowIndex}>
              {Array.from({ length: columnCount }).map((_, cellIndex) => (
                <TableCell key={cellIndex} className="py-4">
                  <div className="space-y-2">
                    <Skeleton className="h-4 w-32" />
                    {cellIndex === 0 && <Skeleton className="h-3 w-20" />}
                  </div>
                </TableCell>
              ))}
              {hasActions && (
                <TableCell className="py-4">
                  <Skeleton className="h-8 w-8 rounded-md ml-auto" />
                </TableCell>
              )}
            </TableRow>
          ))}
        </TableBody>
      </Table>
    </div>
  );
}
