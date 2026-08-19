import { useTranslation } from "react-i18next";
import { toast } from "sonner";
import { parseApiError, type ParsedApiError } from "@/lib/api/error-types";
import i18nKeyContainer from "@/lib/i18n/keyContainer";

/**
 * Notification-specific toast hook
 * Handles all notification-related toast notifications
 */
export function useNotificationToast() {
    const { t } = useTranslation();

    /**
     * Show success toast when a single notification is marked as read
     */
    const markedAsRead = () => {
        toast.success(t(i18nKeyContainer.toast.notification.markedAsRead), {
            description: t(i18nKeyContainer.toast.notification.markedAsReadDesc),
        });
    };

    /**
     * Show success toast when all notifications are marked as read
     */
    const markedAllAsRead = () => {
        toast.success(t(i18nKeyContainer.toast.notification.markedAllAsRead), {
            description: t(i18nKeyContainer.toast.notification.markedAllAsReadDesc),
        });
    };

    /**
     * Show error toast for mark as read operations
     */
    const error = (apiError: unknown): ParsedApiError => {
        const parsedError = parseApiError(apiError);

        toast.error(t(i18nKeyContainer.errors.notification.markReadFailed), {
            description: t(i18nKeyContainer.errors.notification.markReadFailedDesc),
        });

        return parsedError;
    };

    /**
     * Show error toast for fetch operations
     */
    const fetchError = (apiError: unknown): ParsedApiError => {
        const parsedError = parseApiError(apiError);

        toast.error(t(i18nKeyContainer.errors.notification.fetchFailed), {
            description: t(i18nKeyContainer.errors.notification.fetchFailedDesc),
        });

        return parsedError;
    };

    return {
        markedAsRead,
        markedAllAsRead,
        error,
        fetchError,
    };
}
