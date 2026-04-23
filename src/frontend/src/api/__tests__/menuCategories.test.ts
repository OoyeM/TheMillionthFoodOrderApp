import { describe, it, expect, vi } from 'vitest';
import { http, HttpResponse } from 'msw';
import { server } from '../../test/msw/server';
import { menuCategoriesApi } from '../menuCategories';

const BRAND_SLUG = 'frietjes';

/**
 * Tests for src/api/menuCategories.ts
 */
describe('menuCategoriesApi', () => {
  describe('list', () => {
    it('returns a list of menu category list items', async () => {
      const categories = await menuCategoriesApi.list(BRAND_SLUG);

      expect(categories).toHaveLength(1);
      expect(categories[0]).toMatchObject({
        id: 'cat-1',
        name: 'Frietjes',
        sortOrder: 1,
      });
    });

    it('dispatches auth:session-expired on 401', async () => {
      server.use(
        http.get('/api/brands/:slug/menu-categories', () =>
          new HttpResponse(null, { status: 401 }),
        ),
      );

      const listener = vi.fn();
      window.addEventListener('auth:session-expired', listener);

      try {
        await menuCategoriesApi.list(BRAND_SLUG);
      } catch {
        // Expected
      } finally {
        window.removeEventListener('auth:session-expired', listener);
      }

      expect(listener).toHaveBeenCalledOnce();
    });
  });

  describe('get', () => {
    it('returns a single menu category with translations', async () => {
      const category = await menuCategoriesApi.get(BRAND_SLUG, 'cat-1');
      expect(category).toMatchObject({ id: 'cat-1', sortOrder: 1 });
      expect(category.translations).toHaveLength(1);
    });
  });

  describe('create', () => {
    it('creates a category and returns the created entity', async () => {
      const category = await menuCategoriesApi.create(BRAND_SLUG, {
        sortOrder: 2,
        imageUrl: null,
        translations: [{ languageCode: 'nl', name: 'Sauzen' }],
      });

      expect(category).toMatchObject({ sortOrder: 2, productCount: 0 });
    });
  });

  describe('update', () => {
    it('updates a category and returns the updated entity', async () => {
      const category = await menuCategoriesApi.update(BRAND_SLUG, 'cat-1', {
        sortOrder: 1,
        imageUrl: null,
        translations: [{ languageCode: 'nl', name: 'Frietjes Updated' }],
      });

      expect(category).toMatchObject({ id: 'cat-1' });
    });
  });

  describe('remove', () => {
    it('deletes a category without throwing', async () => {
      await expect(menuCategoriesApi.remove(BRAND_SLUG, 'cat-1')).resolves.toBeUndefined();
    });
  });

  describe('reorder', () => {
    it('reorders a category without throwing', async () => {
      await expect(
        menuCategoriesApi.reorder(BRAND_SLUG, 'cat-1', { sortOrder: 2 }),
      ).resolves.toBeUndefined();
    });
  });

  describe('assignProduct', () => {
    it('assigns a product to a category without throwing', async () => {
      await expect(
        menuCategoriesApi.assignProduct(BRAND_SLUG, {
          productId: 'prod-1',
          categoryId: 'cat-1',
        }),
      ).resolves.toBeUndefined();
    });
  });

  describe('listProducts', () => {
    it('returns products for a category', async () => {
      const products = await menuCategoriesApi.listProducts(BRAND_SLUG, 'cat-1');
      expect(Array.isArray(products)).toBe(true);
    });
  });

  describe('reorderProducts', () => {
    it('reorders products in a category without throwing', async () => {
      await expect(
        menuCategoriesApi.reorderProducts(BRAND_SLUG, 'cat-1', {
          productIds: ['prod-1', 'prod-2'],
        }),
      ).resolves.toBeUndefined();
    });
  });
});
