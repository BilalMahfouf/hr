/**
 * HREnap — Web Push Service Worker
 *
 * Handles push events from the server and notification click actions.
 * Served as a static file at /sw-push.js (from frontend/public/).
 *
 * Security notes:
 *  - Contains NO secrets (VAPID private key stays on the server).
 *  - Only callable over HTTPS (or localhost); service workers are secure by spec.
 *  - Payload is end-to-end encrypted by the Web Push protocol (VAPID / RFC 8291).
 */

/* eslint-disable no-undef */

// Force the updated SW to activate immediately (no tab-reload required).
self.addEventListener("install", () => {
    console.log("[SW-Push] Installing — calling skipWaiting.");
    self.skipWaiting();
});

self.addEventListener("activate", (event) => {
    console.log("[SW-Push] Activated — claiming clients.");
    event.waitUntil(self.clients.claim());
});

// ─── Push event ────────────────────────────────────────────────────────────

self.addEventListener("push", (event) => {
    console.log("[SW-Push] Push event received.", event.data ? "Has data." : "No data.");

    let data = {
        title: "HREnap",
        body: "You have a new notification.",
        url: "/notifications",
        notificationId: null,
    };

    if (event.data) {
        try {
            data = { ...data, ...event.data.json() };
            console.log("[SW-Push] Parsed payload:", data.title, data.body);
        } catch (err) {
            console.warn("[SW-Push] Failed to parse push payload:", err);
        }
    }

    // Build an absolute icon URL so the SW can always resolve it regardless of scope.
    const iconUrl = self.registration.scope.replace(/\/$/, "") + "/favicon.ico";

    const showPromise = self.registration.showNotification(data.title, {
        body: data.body,
        icon: iconUrl,
        badge: iconUrl,
        tag: data.notificationId ?? "hrenap-notification",
        // requireInteraction keeps the Windows toast visible until the user
        // explicitly dismisses it.  Without this it auto-closes in ~5 s and
        // you miss it entirely when Chrome is not the foreground window.
        requireInteraction: true,
        renotify: true,
        silent: false,
        data: {
            url: data.url ?? "/notifications",
            notificationId: data.notificationId,
        },
    }).then(() => {
        console.log("[SW-Push] showNotification resolved successfully.");
    }).catch((err) => {
        console.error("[SW-Push] showNotification failed:", err);
    });

    event.waitUntil(showPromise);
});

// ─── Notification click event ───────────────────────────────────────────────

self.addEventListener("notificationclick", (event) => {
    event.notification.close();

    const targetUrl = event.notification.data?.url ?? "/notifications";
    const origin = self.location.origin;

    const focusPromise = clients
        .matchAll({ type: "window", includeUncontrolled: true })
        .then((clientList) => {
            // Try to focus an existing HREnap tab.
            const existing = clientList.find((c) =>
                c.url.startsWith(origin)
            );
            if (existing) {
                return existing.navigate(targetUrl).then((c) => c?.focus() ?? existing.focus());
            }
            // No open tab — open a new one.
            return clients.openWindow(targetUrl);
        });

    event.waitUntil(focusPromise);
});
