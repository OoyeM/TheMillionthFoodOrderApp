import { describe, it, expect, vi } from 'vitest';
import { http, HttpResponse } from 'msw';
import { server } from '../../test/msw/server';
import { openingHoursApi } from '../openingHours';

const BRAND_SLUG = 'frietjes';
const SHOP_ID = 'shop-1';

/**
 * Tests for src/api/openingHours.ts
 */
describe('openingHoursApi', () => {
  describe('get', () => {
    it('returns opening hours for a shop', async () => {
      const hours = await openingHoursApi.get(BRAND_SLUG, SHOP_ID);

      expect(hours.timeBlocks).toHaveLength(1);
      expect(hours.timeBlocks[0]).toMatchObject({
        dayOfWeek: 1,
        openTime: '09:00',
        closeTime: '18:00',
      });
    });

    it('dispatches auth:session-expired on 401', async () => {
      server.use(
        http.get('/api/brands/:slug/shops/:shopId/opening-hours', () =>
          new HttpResponse(null, { status: 401 }),
        ),
      );

      const listener = vi.fn();
      window.addEventListener('auth:session-expired', listener);

      try {
        await openingHoursApi.get(BRAND_SLUG, SHOP_ID);
      } catch {
        // Expected
      } finally {
        window.removeEventListener('auth:session-expired', listener);
      }

      expect(listener).toHaveBeenCalledOnce();
    });
  });

  describe('set', () => {
    it('sets opening hours and returns the updated schedule', async () => {
      const result = await openingHoursApi.set(BRAND_SLUG, SHOP_ID, {
        timeBlocks: [
          { dayOfWeek: 1, openTime: '10:00', closeTime: '20:00' },
          { dayOfWeek: 2, openTime: '10:00', closeTime: '20:00' },
        ],
      });

      expect(result.timeBlocks).toHaveLength(2);
    });
  });

  describe('getStatus', () => {
    it('returns the real-time open/closed status', async () => {
      const status = await openingHoursApi.getStatus(BRAND_SLUG, SHOP_ID);

      expect(status).toMatchObject({
        isOpen: true,
        nextOpeningTime: null,
        timeZoneId: 'Europe/Brussels',
      });
    });

    it('reports closed status correctly', async () => {
      server.use(
        http.get('/api/brands/:slug/shops/:shopId/status', () =>
          HttpResponse.json({
            isOpen: false,
            nextOpeningTime: '2024-06-01T10:00:00Z',
            timeZoneId: 'Europe/Brussels',
          }),
        ),
      );

      const status = await openingHoursApi.getStatus(BRAND_SLUG, SHOP_ID);
      expect(status.isOpen).toBe(false);
      expect(status.nextOpeningTime).toBeDefined();
    });
  });
});
