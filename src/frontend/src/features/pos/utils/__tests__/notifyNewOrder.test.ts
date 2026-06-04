import { describe, it, expect } from 'vitest';
import {
  notificationsSupported,
  notificationPermission,
  requestNotificationPermission,
  showNewOrderNotification,
} from '../notifyNewOrder';

const PERMISSIONS = ['granted', 'denied', 'default'];

// jsdom provides no Notification API. The invariants under test: the helpers
// never throw and always return a sane permission value regardless of support.
describe('notifyNewOrder', () => {
  it('never throws and reports a valid permission', async () => {
    expect(typeof notificationsSupported()).toBe('boolean');
    expect(PERMISSIONS).toContain(notificationPermission());
    const requested = await requestNotificationPermission();
    expect(PERMISSIONS).toContain(requested);
    expect(() => showNewOrderNotification('New order', '#0042', 'order-1')).not.toThrow();
  });
});
