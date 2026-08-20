import { useTranslation } from "react-i18next";
import { toast } from "sonner";
import {
    parseApiError,
    getErrorI18nKey,
    getErrorDescriptionKey,
    isKnownApiError,
    type ParsedApiError,
} from "@/lib/api/error-types";

type ToastType = "success" | "error" | "warning" | "info";

interface ToastOptions {
    description?: string;
    duration?: number;
}

/**
 * Centralized toast hook with i18n support
 * Provides consistent toast notifications throughout the application
 */
export function useToast() {
    const { t } = useTranslation();

    const showToast = (
        type: ToastType,
        titleKey: string,
        options?: ToastOptions
    ) => {
        const title = t(titleKey);
        const description = options?.description ? t(options.description) : undefined;

        switch (type) {
            case "success":
                toast.success(title, { description, duration: options?.duration });
                break;
            case "error":
                toast.error(title, { description, duration: options?.duration });
                break;
            case "warning":
                toast.warning(title, { description, duration: options?.duration });
                break;
            case "info":
                toast.info(title, { description, duration: options?.duration });
                break;
        }
    };

    /**
     * Handle API error and show appropriate toast
     * Maps backend error codes to localized messages
     */
    const handleApiError = (error: unknown, fallbackKey?: string): ParsedApiError => {
        const parsedError = parseApiError(error);
        const errorTitleKey = getErrorI18nKey(parsedError);
        const descriptionKey = isKnownApiError(parsedError)
            ? getErrorDescriptionKey(parsedError)
            : fallbackKey;

        // Determine toast type based on error type
        let toastType: ToastType = "error";
        if (parsedError.type === "validation") {
            toastType = "warning";
        } else if (parsedError.type === "notFound") {
            toastType = "warning";
        }

        showToast(toastType, errorTitleKey, {
            description: descriptionKey,
        });

        return parsedError;
    };

    // Predefined toast methods for common actions
    return {
        // Generic methods
        success: (titleKey: string, options?: ToastOptions) =>
            showToast("success", titleKey, options),
        error: (titleKey: string, options?: ToastOptions) =>
            showToast("error", titleKey, options),
        warning: (titleKey: string, options?: ToastOptions) =>
            showToast("warning", titleKey, options),
        info: (titleKey: string, options?: ToastOptions) =>
            showToast("info", titleKey, options),

        // API error handler
        handleApiError,
    };
}

// Export toast directly for use outside of React components
export { toast };
