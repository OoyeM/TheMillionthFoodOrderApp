import { describe, it, expect, vi } from 'vitest';
import { http, HttpResponse } from 'msw';
import { server } from '../../test/msw/server';
import { shopsApi } from '../shops';

const BRAND_SLUG = 'frietjes';
const SHOP_ADDRESS = {
  street: 'Veldstraat',
  number: '1',
  city: 'Gent',
  postalCode: '9000',
  country: 'BE',
};

/**
 * Tests for src/api/shops.ts
 */
describe('shopsApi', () => {
  describe('list', () => {
    it('returns a list of shops', async () => {
      const shops = await shopsApi.list(BRAND_SLUG);

      expect(shops).toHaveLength(1);
      expect(shops[0]).toMatchObject({
        id: 'shop-1',
        name: 'Gent Centrum',
        slug: 'gent-centrum',
      });
    });

    it('dispatches auth:session-expired on 401', async () => {
      server.use(
        http.get('/api/brands/:slug/shops', () => new HttpResponse(null, { status: 401 })),
      );

      const listener = vi.fn();
      window.addEventListener('auth:session-expired', listener);

      try {
        await shopsApi.list(BRAND_SLUG);
      } catch {
        // Expected
      } finally {
        window.removeEventListener('auth:session-expired', listener);
      }

      expect(listener).toHaveBeenCalledOnce();
    });
  });

  describe('get', () => {
    it('returns a single shop by id', async () => {
      const shop = await shopsApi.get(BRAND_SLUG, 'shop-1');
      expect(shop).toMatchObject({ id: 'shop-1', name: 'Gent Centrum' });
    });
  });

  describe('create', () => {
    it('creates a shop and returns the created entity', async () => {
      const shop = await shopsApi.create(BRAND_SLUG, {
        name: 'Brussel Noord',
        slug: 'brussel-noord',
        address: SHOP_ADDRESS,
        contactEmail: 'brussel@frietjes.be',
      });

      expect(shop).toMatchObject({
        name: 'Brussel Noord',
        slug: 'brussel-noord',
      });
    });
  });

  describe('update', () => {
    it('updates a shop and returns the updated entity', async () => {
      const shop = await shopsApi.update(BRAND_SLUG, 'shop-1', {
        name: 'Gent Centrum Updated',
        address: SHOP_ADDRESS,
        contactEmail: 'gent-updated@frietjes.be',
        kitchenDisplayEnabled: false,
        ticketPrinterEnabled: false,
        pushNotificationEnabled: false,
        soundAlertEnabled: false,
        eatIn: { isEnabled: true, requiresTableNumber: true },
        timeSlotOrdering: { isEnabled: false, intervalMinutes: null, maxOrdersPerInterval: null },
      });

      expect(shop).toMatchObject({ name: 'Gent Centrum Updated' });
    });
  });

  describe('deactivate / activate', () => {
    it('deactivates a shop without throwing', async () => {
      await expect(shopsApi.deactivate(BRAND_SLUG, 'shop-1')).resolves.toBeUndefined();
    });

    it('activates a shop without throwing', async () => {
      await expect(shopsApi.activate(BRAND_SLUG, 'shop-1')).resolves.toBeUndefined();
    });
  });
});
