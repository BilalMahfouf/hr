import api from "@/lib/api/api";
import type { CursorPagedList } from "@/components/tables/types";

// ==================== Types ====================

/**
 * Notification filter type matching backend.
 */
export const NotificationType = {
  All: 1,
  NotReaded: 2,
} as const;

export type NotificationType =
  (typeof NotificationType)[keyof typeof NotificationType];

/**
 * Notification response from the backend.
 */
export interface Notification {
  id: string;
  title: string;
  body: string;
  isRead: boolean;
  createdOnUtc: string;
}

/**
 * Request parameters for fetching notifications.
 */
export interface GetNotificationsParams {
  pageSize?: number;
  cursor?: string;
  direction?: "next" | "prev";
  type?: NotificationType;
}

// ==================== API ====================

const notificationApi = {
  /**
   * Get all notifications with cursor-based pagination.
   */
  getAllNotifications: async (
    params?: GetNotificationsParams
  ): Promise<CursorPagedList<Notification>> => {
    const response = await api.get<CursorPagedList<Notification>>(
      "/notifications",
      { params }
    );
    if (response.status !== 200) {
      throw new Error("Failed to fetch notifications");
    }
    return response.data;
  },

  /**
   * Mark a single notification as read.
   */
  markAsRead: async (notificationId: string): Promise<void> => {
    const response = await api.patch<void>(
      `/notifications/${notificationId}/mark-as-read`
    );
    if (response.status !== 204) {
      throw new Error("Failed to mark notification as read");
    }
  },

  /**
   * Mark all notifications as read.
   */
  markAllAsRead: async (): Promise<void> => {
    const response = await api.patch<void>("/notifications/mark-all-as-read");
    if (response.status !== 204) {
      throw new Error("Failed to mark all notifications as read");
    }
  },

  // ==================== Web Push ====================

  /**
   * Fetch the VAPID public key from the server.
   * The browser needs this key to create a PushSubscription.
   */
  getVapidPublicKey: async (): Promise<string> => {
    const response = await api.get<{ publicKey: string }>("/push/vapid-public-key");
    return response.data.publicKey;
  },

  /**
   * Register (or refresh) a Web Push subscription on the server.
   * Upserts by endpoint so a re-subscription from the same browser does not create duplicates.
   */
  registerPushSubscription: async (subscription: {
    endpoint: string;
    p256dh: string;
    auth: string;
    userAgent?: string;
  }): Promise<void> => {
    await api.post("/push/subscriptions", subscription);
  },

  /**
   * Unregister a Web Push subscription on the server (user opted out).
   */
  unregisterPushSubscription: async (endpoint: string): Promise<void> => {
    await api.delete("/push/subscriptions", { data: { endpoint } });
  },

  /**
   * Send a test Web Push notification to all subscriptions of the current user.
   * Used to verify end-to-end push delivery without a real event.
   */
  sendTestPush: async (): Promise<void> => {
    await api.post("/push/test");
  },
};

export default notificationApi;