import api from './api';
import i18n from '../i18n';
import i18nKeyContainer from '../i18n/keyContainer';
import { toast } from 'sonner';
import type { AxiosError, InternalAxiosRequestConfig } from 'axios';

// ── State ────────────────────────────────────────────────
let accessToken: string | null = null;
let refreshPromise: Promise<string | null> | null = null;

// Listeners for auth-state changes (used by AuthProvider)
type AuthListener = (token: string | null) => void;
const listeners: Set<AuthListener> = new Set();

const notifyListeners = (token: string | null) => {
  listeners.forEach((fn) => fn(token));
};

// ── Token Manager ────────────────────────────────────────
export const tokenManager = {
  getAccessToken: () => accessToken,

  setAccessToken: (token: string | null) => {
    accessToken = token;
    notifyListeners(token);
  },

  clearTokens: () => {
    accessToken = null;
    notifyListeners(null);
  },

  /** Subscribe to token changes. Returns an unsubscribe function. */
  subscribe: (listener: AuthListener) => {
    listeners.add(listener);
    listener(accessToken); // fire immediately with current value
    return () => {
      listeners.delete(listener);
    };
  },

  /**
   * Singleton refresh — no matter how many callers invoke this concurrently,
   * only ONE network request is made. Every caller shares the same promise.
   */
  refreshAccessToken: (): Promise<string | null> => {
    if (refreshPromise) return refreshPromise;

    refreshPromise = (async () => {
      try {
        const response = await api.post(
          '/auth/refresh-token',
          {},
          { skipAuthRefresh: true } as any, // httpOnly cookie sent automatically
        );

        if (response.status === 200 && response.data?.token) {
          const newToken: string = response.data.token;
          tokenManager.setAccessToken(newToken);
          return newToken;
        }

        // 200 but no token — treat as failure
        return null;
      } catch (error) {
        tokenManager.clearTokens();
        throw error;
      } finally {
        refreshPromise = null; // allow future refreshes
      }
    })();

    return refreshPromise;
  },
};

// ── Helpers ──────────────────────────────────────────────
let isRedirecting = false;

function redirectToLogin() {
  if (isRedirecting) return;
  isRedirecting = true;
  const message = i18n.t(i18nKeyContainer.sessionExpiredMessage);
  toast.error(message);
  setTimeout(() => {
    isRedirecting = false;
    window.location.href = '/login';
  }, 600);
}

// ── Request interceptor — attach access token ────────────
api.interceptors.request.use(
  (config) => {
    if ((config as any).skipAuthRefresh) return config;

    const token = tokenManager.getAccessToken();
    if (token) {
      config.headers.Authorization = `Bearer ${token}`;
    }
    return config;
  },
  (error) => Promise.reject(error),
);

// ── Response interceptor — handle 401 with refresh ───────
api.interceptors.response.use(
  (response) => response,
  async (error: AxiosError) => {
    const originalRequest = error.config as
      | (InternalAxiosRequestConfig & { _retry?: boolean; skipAuthRefresh?: boolean })
      | undefined;

    if (!originalRequest || originalRequest.skipAuthRefresh) {
      return Promise.reject(error);
    }

    if (error.response?.status === 401 && !originalRequest._retry) {
      originalRequest._retry = true;

      try {
        // All concurrent 401s share the same singleton refresh promise
        const newToken = await tokenManager.refreshAccessToken();

        if (newToken) {
          originalRequest.headers.Authorization = `Bearer ${newToken}`;
          return api(originalRequest);
        }

        // No token returned — session gone
        tokenManager.clearTokens();
        redirectToLogin();
        return Promise.reject(error);
      } catch (refreshError) {
        // Refresh failed (expired cookie, network, etc.)
        tokenManager.clearTokens();
        redirectToLogin();
        return Promise.reject(refreshError);
      }
    }

    return Promise.reject(error);
  },
);