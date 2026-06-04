/**
 * Browser push-notification helpers for new kitchen orders (US-FP-026).
 *
 * Uses the basic Notification API (no service worker), so it works in dev and
 * surfaces a desktop notification while the kitchen page is open — even when the
 * tab is in the background. No-ops when the API is unavailable (tests/SSR) or
 * permission hasn't been granted, and never throws.
 */

export function notificationsSupported(): boolean {
  return typeof Notification !== 'undefined';
}

/** Current permission, or 'denied' when notifications are unsupported. */
export function notificationPermission(): NotificationPermission {
  return notificationsSupported() ? Notification.permission : 'denied';
}

/**
 * Requests notification permission. Must be called from a user gesture — browsers
 * ignore it otherwise. Resolves to the resulting permission, or 'denied' when
 * unsupported.
 */
export async function requestNotificationPermission(): Promise<NotificationPermission> {
  if (!notificationsSupported()) return 'denied';
  try {
    return await Notification.requestPermission();
  } catch {
    return notificationPermission();
  }
}

/**
 * Shows a desktop notification for a new order. No-ops unless notifications are
 * supported and permission has been granted. `tag` (the order id) dedupes repeat
 * notifications for the same order.
 */
export function showNewOrderNotification(title: string, body: string, tag: string): void {
  if (!notificationsSupported() || Notification.permission !== 'granted') return;
  try {
    void new Notification(title, { body, tag });
  } catch {
    // Ignore — a failed notification must never break the order board.
  }
}
