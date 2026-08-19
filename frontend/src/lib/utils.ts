import type { TableRequest } from "@/components/tables/types";
import { clsx, type ClassValue } from "clsx"
import { twMerge } from "tailwind-merge"

export function cn(...inputs: ClassValue[]) {
  return twMerge(clsx(inputs))
}
export function getTableRequsestParams(request: TableRequest): URLSearchParams {
    const params = new URLSearchParams();
    params.append('page', request.page.toString());
    params.append('pageSize', request.pageSize.toString());
    if(request.search)    params.append('search', request.search);
    if(request.sortColumn)    params.append('sortColumn', request.sortColumn.toLowerCase());
    if(request.sortOrder)    params.append('sortOrder', request.sortOrder);
    return params;
}