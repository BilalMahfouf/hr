/**
 * push-notifications.ts
 *
 * Helper module for managing Web Push subscriptions in HREnap.
 * All interactions with the native browser Push API are isolated here
 * so the rest of the codebase only calls these three exported functions.
 *
 * Registration only happens on HTTPS (or localhost for dev). On plain HTTP
 * the functions return "unsupported" gracefully.
 */

import notificationApi from "./notification-api";

// ─── Types ──────────────────────────────────────────────────────────────────

/**
 * Current push notification status for the current browser.
 *
 * - `unsupported` — browser does not support the Web Push API or is on HTTP.
 * - `denied`      — user has blocked notifications in browser settings.
 * - `enabled`     — an active push subscription exists for this browser.
 * - `disabled`    — supported and not denied, but no active subscription.
 */
export type PushStatus = "unsupported" | "denied" | "enabled" | "disabled";

// ─── Internal helpers ────────────────────────────────────────────────────────

/** Returns true when the current environment supports service workers and push. */
function isPushSupported(): boolean {
  return (
    typeof window !== "undefined" &&
    "Notification" in window &&
    "serviceWorker" in navigator &&
    "PushManager" in window &&
    // Web Push requires HTTPS (service workers on localhost are exempt).
    (location.protocol === "https:" || location.hostname === "localhost")
  );
}

/**
 * Converts a URL-safe Base64 string (VAPID public key) into the Uint8Array
 * format required by `PushManager.subscribe({ applicationServerKey })`.
 */
function urlBase64ToUint8Array(base64String: string): Uint8Array {
  const padding = "=".repeat((4 - (base64String.length % 4)) % 4);
  const base64 = (base64String + padding).replace(/-/g, "+").replace(/_/g, "/");
  const rawData = atob(base64);
  return Uint8Array.from(rawData, (c) => c.charCodeAt(0));
}

/**
 * Registers /sw-push.js (idempotent) then waits for it to reach the
 * "activated" state via `navigator.serviceWorker.ready`.
 * Calling `pushManager.subscribe()` on an installing/waiting SW throws, so
 * we MUST await `.ready` before handing the registration to the push API.
 */
async function getRegistration(): Promise<ServiceWorkerRegistration> {
  // register() is a no-op when the same URL+scope is already registered.
  await navigator.serviceWorker.register("/sw-push.js", { scope: "/" });
  // .ready resolves only once there is an *active* service worker controlling this page.
  const registration = await navigator.serviceWorker.ready;
  console.debug("[Push] Service worker active:", registration.active?.state);
  return registration;
}

// ─── Public API ──────────────────────────────────────────────────────────────

/**
 * Returns the current push notification status for this browser.
 * Safe to call at any time — does NOT prompt the user for permission.
 */
export async function getPushStatus(): Promise<PushStatus> {
  if (!isPushSupported()) return "unsupported";
  if (Notification.permission === "denied") return "denied";

  try {
    // Use the current page URL — finds any SW registration whose scope covers it.
    const registration = await navigator.serviceWorker.getRegistration(window.location.href);
    if (!registration?.active) return "disabled";

    const subscription = await registration.pushManager.getSubscription();
    console.debug("[Push] Current subscription:", subscription?.endpoint ?? "none");
    return subscription ? "enabled" : "disabled";
  } catch (err) {
    console.warn("[Push] getPushStatus error:", err);
    return "disabled";
  }
}

/**
 * Requests permission, creates a push subscription, then registers it on the server.
 * Must be called from a direct user gesture (button click) to satisfy browser policy.
 *
 * @throws {Error} when the user denies permission or subscription creation fails.
 */
export async function ensurePushSubscription(): Promise<void> {
  if (!isPushSupported()) {
    throw new Error("push_unsupported");
  }

  // Request permission — only works from a user gesture.
  const permission = await Notification.requestPermission();
  if (permission !== "granted") {
    throw new Error("push_permission_denied");
  }

  // Fetch the VAPID public key — server endpoint requires auth (JWT in header).
  const vapidPublicKey = await notificationApi.getVapidPublicKey();
  console.debug("[Push] VAPID public key fetched, length:", vapidPublicKey.length);

  const registration = await getRegistration();

  // Subscribe.  `userVisibleOnly: true` is required by Chrome.
  let subscription = await registration.pushManager.getSubscription();
  if (!subscription) {
    console.debug("[Push] No existing subscription — creating new one.");
    subscription = await registration.pushManager.subscribe({
      userVisibleOnly: true,
      applicationServerKey: urlBase64ToUint8Array(vapidPublicKey).buffer as ArrayBuffer,
    });
  } else {
    console.debug("[Push] Re-using existing browser subscription.");
  }

  const json = subscription.toJSON();
  console.debug("[Push] Subscription endpoint:", subscription.endpoint);

  await notificationApi.registerPushSubscription({
    endpoint: subscription.endpoint,
    p256dh:   json.keys?.p256dh   ?? "",
    auth:     json.keys?.auth     ?? "",
    userAgent: navigator.userAgent,
  });
  console.debug("[Push] Subscription saved to server.");
}

/**
 * Cancels the active push subscription (browser-side) and removes it from the server.
 * A no-op when there is no active subscription.
 */
export async function unsubscribeFromPush(): Promise<void> {
  if (!isPushSupported()) return;

  const registration = await navigator.serviceWorker.getRegistration("/sw-push.js");
  if (!registration) return;

  const subscription = await registration.pushManager.getSubscription();
  if (!subscription) return;

  const endpoint = subscription.endpoint;

  // Unsubscribe in the browser first, then remove from server.
  await subscription.unsubscribe();
  await notificationApi.unregisterPushSubscription(endpoint);
}
