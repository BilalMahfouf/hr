/**
 * API Error Types and Utilities
 * Maps backend ProblemDetails errors to frontend-friendly structures
 */

/**
 * Backend ProblemDetails structure (RFC 7807)
 */
export interface ProblemDetails {
    type: string;
    title: string; // This contains the error code (e.g., "Client.ClientNotFound")
    status: number;
    errors?: [string, string]; // [errorCode, errorDescription]
}

/**
 * Axios error response structure
 */
export interface ApiErrorResponse {
    response?: {
        data?: ProblemDetails;
        status?: number;
    };
    message?: string;
}

/**
 * Error types based on HTTP status codes
 */
export type ErrorType =
    | "validation"    // 400
    | "unauthorized"  // 401
    | "notFound"      // 404
    | "conflict"      // 409
    | "server";       // 500+

/**
 * Parsed error structure for frontend use
 */
export interface ParsedApiError {
    type: ErrorType;
    code: string;
    status: number;
    description: string;
}

/**
 * Backend error codes - mapped from EntityErrors
 */
export const ErrorCodes = {
    // User/Auth errors
    USER_NOT_FOUND: "User.NotFound",
    USER_INVALID_CREDENTIALS: "User.InvalidCredentials",
    USER_EXPIRED_REFRESH_TOKEN: "User.ExpiredRefreshToken",
    USER_INVALID_PASSWORD: "User.InvalidPassword",
    USER_INVALID_PASSWORD_LENGTH: "User.InvalidPasswordLength",
    USER_EMAIL_ALREADY_IN_USE: "User.EmailAlreadyInUse",
    USER_INVALID_RESET_TOKEN: "User.InvalidResetToken",

    // Subscription errors
    SUBSCRIPTION_NOT_FOUND: "Subscription.NotFound",
    SUBSCRIPTION_ACTIVE_ALREADY_EXISTS: "Subscription.ActiveSubscriptionAlreadyExist",
    SUBSCRIPTION_ALREADY_CANCELLED: "Subscription.SubscriptionAlreadyCancelled",
    SUBSCRIPTION_NOT_ACTIVE: "Subscription.SubscriptionNotActive",
    SUBSCRIPTION_FAILED_CHECKOUT: "Subscription.FailedToRetrieveCheckout",
    SUBSCRIPTION_PLAN_NOT_FOUND: "SubscriptionPlan.NotFound",
    SUBSCRIPTION_PLAN_ALREADY_EXISTS: "SubscriptionPlan.AlreadyExists",
    SUBSCRIPTION_PLAN_ALREADY_ACTIVE: "SubscriptionPlan.AlreadyActive",
    SUBSCRIPTION_PLAN_ALREADY_NOT_ACTIVE: "SubscriptionPlan.AlreadyNotActive",

    // Generic errors
    VALIDATION_ERROR: "Validation.Error",
    SERVER_ERROR: "Server.Error",
    NETWORK_ERROR: "Network.Error",
    UNKNOWN_ERROR: "Unknown.Error",
} as const;

export type ErrorCode = (typeof ErrorCodes)[keyof typeof ErrorCodes];

/**
 * Maps HTTP status code to ErrorType
 */
function getErrorType(status: number): ErrorType {
    switch (status) {
        case 400:
            return "validation";
        case 401:
            return "unauthorized";
        case 404:
            return "notFound";
        case 409:
            return "conflict";
        default:
            return "server";
    }
}

/**
 * Parses an API error response into a structured format
 */
export function parseApiError(error: unknown): ParsedApiError {
    const apiError = error as ApiErrorResponse;

    // Check if it's an Axios error with response data
    if (apiError?.response?.data) {
        const problemDetails = apiError.response.data;
        const status = problemDetails.status || apiError.response.status || 500;

        return {
            type: getErrorType(status),
            code: problemDetails.title || ErrorCodes.UNKNOWN_ERROR,
            status,
            description: problemDetails.errors?.[1] || problemDetails.title || "An error occurred",
        };
    }

    // Check if it's a network error
    if (apiError?.message === "Network Error" || !apiError?.response) {
        return {
            type: "server",
            code: ErrorCodes.NETWORK_ERROR,
            status: 0,
            description: "Network error - please check your connection",
        };
    }

    // Fallback for unknown error structures
    return {
        type: "server",
        code: ErrorCodes.UNKNOWN_ERROR,
        status: 500,
        description: "An unexpected error occurred",
    };
}

/**
 * Check if error is a specific error code
 */
export function isErrorCode(error: ParsedApiError, code: ErrorCode): boolean {
    return error.code === code;
}

/**
 * Check if an error is a 404 Not Found error
 * Useful for handling cases where a resource was already deleted
 */
export function isNotFoundError(error: unknown): boolean {
    const parsedError = parseApiError(error);
    return parsedError.type === "notFound" || parsedError.status === 404;
}

/**
 * Get the i18n key for an error code
 * Maps backend error codes to frontend i18n keys
 */
export function getErrorI18nKey(errorCode: string): string {
    // Map error codes to i18n keys
    const errorKeyMap: Record<string, string> = {
        // User/Auth errors
        [ErrorCodes.USER_NOT_FOUND]: "errors.user.notFound",
        [ErrorCodes.USER_INVALID_CREDENTIALS]: "errors.user.invalidCredentials",
        [ErrorCodes.USER_EXPIRED_REFRESH_TOKEN]: "errors.user.expiredToken",

        // Subscription errors
        [ErrorCodes.SUBSCRIPTION_NOT_FOUND]: "errors.subscription.notFound",
        [ErrorCodes.SUBSCRIPTION_ACTIVE_ALREADY_EXISTS]: "errors.subscription.alreadyActive",
        [ErrorCodes.SUBSCRIPTION_ALREADY_CANCELLED]: "errors.subscription.alreadyCancelled",
        [ErrorCodes.SUBSCRIPTION_NOT_ACTIVE]: "errors.subscription.notActive",
        [ErrorCodes.SUBSCRIPTION_FAILED_CHECKOUT]: "errors.subscription.checkoutFailed",
        [ErrorCodes.SUBSCRIPTION_PLAN_NOT_FOUND]: "errors.subscription.planNotFound",
        [ErrorCodes.SUBSCRIPTION_PLAN_ALREADY_EXISTS]: "errors.subscriptionPlan.alreadyExists",
        [ErrorCodes.SUBSCRIPTION_PLAN_ALREADY_ACTIVE]: "errors.subscriptionPlan.alreadyActive",
        [ErrorCodes.SUBSCRIPTION_PLAN_ALREADY_NOT_ACTIVE]: "errors.subscriptionPlan.alreadyNotActive",

        // Generic errors
        [ErrorCodes.VALIDATION_ERROR]: "errors.validation",
        [ErrorCodes.SERVER_ERROR]: "errors.server",
        [ErrorCodes.NETWORK_ERROR]: "errors.network",
        [ErrorCodes.UNKNOWN_ERROR]: "errors.unknown",
    };

    return errorKeyMap[errorCode] || "errors.unknown";
}
