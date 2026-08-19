import { useMutation, useQueryClient } from "@tanstack/react-query";
import notificationApi from "./notification-api";
import { useNotificationToast } from "./use-notification-toast";

/**
 * Hook for marking a single notification as read
 */
export function useMarkAsRead() {
    const queryClient = useQueryClient();
    const notificationToast = useNotificationToast();

    const mutation = useMutation({
        mutationFn: (notificationId: string) =>
            notificationApi.markAsRead(notificationId),
        onSuccess: () => {
            queryClient.invalidateQueries({ queryKey: ["notifications"] });
            notificationToast.markedAsRead();
        },
        onError: (error) => {
            notificationToast.error(error);
        },
    });

    return {
        markAsRead: mutation.mutate,
        isMarking: mutation.isPending,
    };
}

/**
 * Hook for marking all notifications as read
 */
export function useMarkAllAsRead() {
    const queryClient = useQueryClient();
    const notificationToast = useNotificationToast();

    const mutation = useMutation({
        mutationFn: () => notificationApi.markAllAsRead(),
        onSuccess: () => {
            queryClient.invalidateQueries({ queryKey: ["notifications"] });
            notificationToast.markedAllAsRead();
        },
        onError: (error) => {
            notificationToast.error(error);
        },
    });

    return {
        markAllAsRead: mutation.mutate,
        isMarkingAll: mutation.isPending,
    };
}
