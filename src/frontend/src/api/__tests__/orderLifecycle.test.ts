import { describe, it, expect, vi } from 'vitest';
import { http, HttpResponse } from 'msw';
import { server } from '../../test/msw/server';
import { orderLifecycleApi } from '../orderLifecycle';

const BRAND_SLUG = 'frietjes';
const SHOP_ID = 'shop-1';

/**
 * Tests for src/api/orderLifecycle.ts
 */
describe('orderLifecycleApi', () => {
  describe('get', () => {
    it('returns the order lifecycle configuration for a shop', async () => {
      const lifecycle = await orderLifecycleApi.get(BRAND_SLUG, SHOP_ID);

      expect(lifecycle).toMatchObject({ shopId: SHOP_ID });
      expect(lifecycle.statuses).toHaveLength(1);
      expect(lifecycle.statuses[0]).toMatchObject({
        id: 'status-1',
        name: 'New',
        systemKey: 'new',
      });
    });

    it('dispatches auth:session-expired on 401', async () => {
      server.use(
        http.get('/api/brands/:slug/shops/:shopId/order-lifecycle', () =>
          new HttpResponse(null, { status: 401 }),
        ),
      );

      const listener = vi.fn();
      window.addEventListener('auth:session-expired', listener);

      try {
        await orderLifecycleApi.get(BRAND_SLUG, SHOP_ID);
      } catch {
        // Expected
      } finally {
        window.removeEventListener('auth:session-expired', listener);
      }

      expect(listener).toHaveBeenCalledOnce();
    });
  });

  describe('configure', () => {
    it('updates the lifecycle and returns the updated config', async () => {
      const config = {
        statuses: [
          {
            name: 'Pending',
            systemKey: 'pending',
            sortOrder: 1,
            isTerminal: false,
            colorHex: '#f59e0b',
          },
          {
            name: 'Done',
            systemKey: 'done',
            sortOrder: 2,
            isTerminal: true,
            colorHex: '#22c55e',
          },
        ],
        transitions: [{ fromSortOrder: 1, toSortOrder: 2 }],
      };

      const result = await orderLifecycleApi.configure(BRAND_SLUG, SHOP_ID, config);

      expect(result.shopId).toBe(SHOP_ID);
      expect(result.statuses).toHaveLength(2);
    });
  });

  describe('reset', () => {
    it('resets the lifecycle to defaults and returns the reset config', async () => {
      const result = await orderLifecycleApi.reset(BRAND_SLUG, SHOP_ID);
      expect(result.shopId).toBe(SHOP_ID);
    });
  });
});
