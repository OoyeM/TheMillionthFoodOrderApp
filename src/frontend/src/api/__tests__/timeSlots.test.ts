/**
 * Smoke test for the timeSlots API module (US-FP-019).
 * Verifies that timeSlotsApi.get returns the correct shape via MSW.
 */
import { describe, it, expect } from 'vitest';
import { timeSlotsApi } from '../timeSlots';
import type { TimeSlotAvailabilityResponse } from '../timeSlots';
import { http, HttpResponse } from 'msw';
import { server } from '../../test/msw/server';

describe('timeSlotsApi', () => {
  it('returns the disabled-state response shape from the default MSW handler', async () => {
    const result = await timeSlotsApi.get('frietjes', 'shop-1');

    expect(result.isEnabled).toBe(false);
    expect(result.intervalMinutes).toBeNull();
    expect(result.slots).toEqual([]);
    expect(result.activeOrderCount).toBe(0);
  });

  it('returns slots when the shop has time-slot ordering enabled', async () => {
    const fixture: TimeSlotAvailabilityResponse = {
      isEnabled: true,
      intervalMinutes: 15,
      slots: [
        { slotStart: '2026-06-10T08:00:00Z', label: '10:00', isAvailable: true },
        { slotStart: '2026-06-10T08:15:00Z', label: '10:15', isAvailable: false },
      ],
      activeOrderCount: null,
    };

    server.use(
      http.get('/api/brands/:slug/shops/:shopId/time-slots', () =>
        HttpResponse.json(fixture),
      ),
    );

    const result = await timeSlotsApi.get('frietjes', 'shop-1');

    expect(result.isEnabled).toBe(true);
    expect(result.intervalMinutes).toBe(15);
    expect(result.slots).toHaveLength(2);
    expect(result.slots[0]).toMatchObject({ label: '10:00', isAvailable: true });
    expect(result.slots[1]).toMatchObject({ label: '10:15', isAvailable: false });
    expect(result.activeOrderCount).toBeNull();
  });
});
