import { useInfiniteQuery } from "@tanstack/react-query";
import notificationApi, {
    NotificationType,
    type GetNotificationsParams,
} from "./notification-api";
import { parseApiError } from "@/lib/api/error-types";

const PAGE_SIZE = 15;

/**
 * Hook for fetching notifications with cursor-based infinite pagination
 */
export function useNotifications(filterType: "all" | "unread" = "all") {
    const type =
        filterType === "unread" ? NotificationType.NotReaded : NotificationType.All;

    return useInfiniteQuery({
        queryKey: ["notifications", filterType],
        refetchInterval: 30 * 1000, // Refetch every 30 seconds to get new notifications
        queryFn: async ({ pageParam }) => {
            try {
                const params: GetNotificationsParams = {
                    pageSize: PAGE_SIZE,
                    type,
                    ...(pageParam && { cursor: pageParam, direction: "next" as const }),
                };
                return await notificationApi.getAllNotifications(params);
            } catch (error: unknown) {
                const parsedError = parseApiError(error);
                
                // If it's a 404 or empty response, return empty result instead of error
                if (parsedError.status === 404 || parsedError.type === "notFound") {
                    return {
                        items: [],
                        pageSize: PAGE_SIZE,
                        hasNextPage: false,
                        hasPreviousPage: false,
                        nextCursor: null,
                        previousCursor: null,
                    };
                }
                
                // For other errors, re-throw
                throw error;
            }
        },
        initialPageParam: undefined as string | undefined,
        getNextPageParam: (lastPage) =>
            lastPage.hasNextPage ? lastPage.nextCursor : undefined,
        staleTime: 30 * 1000, // 30 seconds
    });
}
