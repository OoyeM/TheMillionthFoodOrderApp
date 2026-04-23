import { describe, it, expect, vi } from 'vitest';
import { http, HttpResponse } from 'msw';
import { server } from '../../test/msw/server';
import { productsApi } from '../products';

const BRAND_SLUG = 'frietjes';

const SIMPLE_PRODUCT_REQUEST = {
  basePrice: 3.5,
  imageUrl: null,
  translations: [{ languageCode: 'nl', name: 'Kleine friet', description: null }],
  allergens: [],
  dietaryTags: [],
};

/**
 * Tests for src/api/products.ts
 */
describe('productsApi', () => {
  describe('list', () => {
    it('returns a list of product list items', async () => {
      const products = await productsApi.list(BRAND_SLUG);

      expect(products).toHaveLength(1);
      expect(products[0]).toMatchObject({
        id: 'prod-1',
        productType: 'Simple',
        name: 'Kleine friet',
      });
    });

    it('dispatches auth:session-expired on 401', async () => {
      server.use(
        http.get('/api/brands/:slug/products', () => new HttpResponse(null, { status: 401 })),
      );

      const listener = vi.fn();
      window.addEventListener('auth:session-expired', listener);

      try {
        await productsApi.list(BRAND_SLUG);
      } catch {
        // Expected
      } finally {
        window.removeEventListener('auth:session-expired', listener);
      }

      expect(listener).toHaveBeenCalledOnce();
    });
  });

  describe('get', () => {
    it('returns a single product with full details', async () => {
      const product = await productsApi.get(BRAND_SLUG, 'prod-1');
      expect(product).toMatchObject({
        id: 'prod-1',
        productType: 'Simple',
        comboItems: null,
      });
      expect(product.translations).toHaveLength(1);
    });
  });

  describe('create', () => {
    it('creates a simple product and returns the created entity', async () => {
      const product = await productsApi.create(BRAND_SLUG, SIMPLE_PRODUCT_REQUEST);

      expect(product).toMatchObject({
        id: 'prod-new',
        productType: 'Simple',
      });
    });
  });

  describe('update', () => {
    it('updates a product and returns the updated entity', async () => {
      const product = await productsApi.update(BRAND_SLUG, 'prod-1', {
        ...SIMPLE_PRODUCT_REQUEST,
        basePrice: 4.0,
      });

      expect(product).toMatchObject({ id: 'prod-1' });
    });
  });

  describe('remove', () => {
    it('deletes a product without throwing', async () => {
      await expect(productsApi.remove(BRAND_SLUG, 'prod-1')).resolves.toBeUndefined();
    });
  });

  describe('createCombo', () => {
    it('creates a combo product and returns the created entity', async () => {
      const product = await productsApi.createCombo(BRAND_SLUG, {
        basePrice: 9.99,
        imageUrl: null,
        translations: [{ languageCode: 'nl', name: 'Frietjes menu', description: null }],
        componentProductIds: ['prod-1', 'prod-2'],
      });

      expect(product).toMatchObject({ id: 'combo-new', productType: 'Combo' });
      expect(product.comboItems).toHaveLength(2);
    });
  });

  describe('updateCombo', () => {
    it('updates a combo product and returns the updated entity', async () => {
      const product = await productsApi.updateCombo(BRAND_SLUG, 'combo-1', {
        basePrice: 10.99,
        imageUrl: null,
        translations: [{ languageCode: 'nl', name: 'Frietjes menu XL', description: null }],
        componentProductIds: ['prod-1', 'prod-2', 'prod-3'],
      });

      expect(product).toMatchObject({ productType: 'Combo' });
      expect(product.comboItems).toHaveLength(3);
    });
  });
});
