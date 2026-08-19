import { useTranslation } from "react-i18next";
import { toast } from "sonner";
import { parseApiError, ErrorCodes, type ParsedApiError } from "@/lib/api/error-types";
import i18nKeyContainer from "@/lib/i18n/keyContainer";

/**
 * Auth-specific toast hook
 * Handles all auth-related toast notifications with domain-aware error mapping
 */
export function useAuthToast() {
  const { t } = useTranslation();

  /**
   * Show success toast for forgot password (reset link sent)
   * Note: Always shows success message for security (don't reveal if email exists)
   */
  const resetLinkSent = () => {
    toast.success(t(i18nKeyContainer.toast.auth.resetLinkSent), {
      description: t(i18nKeyContainer.toast.auth.resetLinkSentDesc),
    });
  };

  /**
   * Show success toast when resending the reset email
   */
  const resendSuccess = () => {
    toast.success(t(i18nKeyContainer.toast.auth.resendSuccess), {
      description: t(i18nKeyContainer.toast.auth.resendSuccessDesc),
    });
  };

  /**
   * Show success toast for password reset
   */
  const passwordReset = () => {
    toast.success(t(i18nKeyContainer.toast.auth.passwordReset), {
      description: t(i18nKeyContainer.toast.auth.passwordResetDesc),
    });
  };

  /**
   * Show error toast with domain-specific message
   * Maps backend error codes to localized auth error messages
   */
  const error = (apiError: unknown): ParsedApiError => {
    const parsedError = parseApiError(apiError);
    
    // Map auth-specific error codes to i18n keys
    let titleKey: string;
    let descKey: string;
    
    switch (parsedError.code) {
      case ErrorCodes.USER_NOT_FOUND:
        titleKey = i18nKeyContainer.errors.user.notFound;
        descKey = i18nKeyContainer.errors.user.notFoundDesc;
        break;
      case ErrorCodes.USER_INVALID_CREDENTIALS:
        titleKey = i18nKeyContainer.errors.user.invalidCredentials;
        descKey = i18nKeyContainer.errors.user.invalidCredentialsDesc;
        break;
      case ErrorCodes.USER_EXPIRED_REFRESH_TOKEN:
        titleKey = i18nKeyContainer.errors.user.expiredToken;
        descKey = i18nKeyContainer.errors.user.expiredTokenDesc;
        break;
      case ErrorCodes.USER_INVALID_PASSWORD:
        titleKey = i18nKeyContainer.errors.user.invalidPassword;
        descKey = i18nKeyContainer.errors.user.invalidPasswordDesc;
        break;
      case ErrorCodes.USER_INVALID_PASSWORD_LENGTH:
        titleKey = i18nKeyContainer.errors.user.invalidPasswordLength;
        descKey = i18nKeyContainer.errors.user.invalidPasswordLengthDesc;
        break;
      case ErrorCodes.USER_EMAIL_ALREADY_IN_USE:
        titleKey = i18nKeyContainer.errors.user.emailAlreadyInUse;
        descKey = i18nKeyContainer.errors.user.emailAlreadyInUseDesc;
        toast.warning(t(titleKey), { description: t(descKey) });
        return parsedError;
      case ErrorCodes.USER_INVALID_RESET_TOKEN:
        titleKey = i18nKeyContainer.errors.user.invalidResetToken;
        descKey = i18nKeyContainer.errors.user.invalidResetTokenDesc;
        break;
      case ErrorCodes.VALIDATION_ERROR:
        titleKey = i18nKeyContainer.errors.validation;
        descKey = i18nKeyContainer.errors.validationDesc;
        toast.warning(t(titleKey), { description: t(descKey) });
        return parsedError;
      case ErrorCodes.NETWORK_ERROR:
        titleKey = i18nKeyContainer.errors.network;
        descKey = i18nKeyContainer.errors.networkDesc;
        break;
      default:
        // Check if the error code contains "User." for unmapped user errors
        if (parsedError.code.startsWith("User.")) {
          titleKey = i18nKeyContainer.errors.user.invalidResetToken;
          descKey = i18nKeyContainer.errors.user.invalidResetTokenDesc;
        } else {
          // Generic server error
          titleKey = i18nKeyContainer.errors.server;
          descKey = i18nKeyContainer.errors.serverDesc;
        }
    }
    
    toast.error(t(titleKey), {
      description: t(descKey),
    });
    
    return parsedError;
  };

  /**
   * Show warning toast for auth operations
   */
  const warning = (
    titleKey: string,
    descriptionKey?: string
  ) => {
    toast.warning(t(titleKey), {
      description: descriptionKey ? t(descriptionKey) : undefined,
    });
  };

  return {
    // Success operations
    resetLinkSent,
    resendSuccess,
    passwordReset,
    
    // Error handling
    error,
    
    // Warning
    warning,
  };
}
