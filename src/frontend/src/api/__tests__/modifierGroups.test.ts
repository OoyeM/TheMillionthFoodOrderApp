import { describe, it, expect, vi } from 'vitest';
import { http, HttpResponse } from 'msw';
import { server } from '../../test/msw/server';
import { modifierGroupsApi } from '../modifierGroups';

const BRAND_SLUG = 'frietjes';

/**
 * Tests for src/api/modifierGroups.ts
 */
describe('modifierGroupsApi', () => {
  describe('list', () => {
    it('returns a list of modifier group list items', async () => {
      const groups = await modifierGroupsApi.list(BRAND_SLUG);

      expect(groups).toHaveLength(1);
      expect(groups[0]).toMatchObject({ id: 'mg-1', name: 'Sauzen', modifierCount: 3 });
    });

    it('dispatches auth:session-expired on 401', async () => {
      server.use(
        http.get('/api/brands/:slug/modifier-groups', () =>
          new HttpResponse(null, { status: 401 }),
        ),
      );

      const listener = vi.fn();
      window.addEventListener('auth:session-expired', listener);

      try {
        await modifierGroupsApi.list(BRAND_SLUG);
      } catch {
        // Expected
      } finally {
        window.removeEventListener('auth:session-expired', listener);
      }

      expect(listener).toHaveBeenCalledOnce();
    });
  });

  describe('get', () => {
    it('returns a single modifier group with modifiers', async () => {
      const group = await modifierGroupsApi.get(BRAND_SLUG, 'mg-1');
      expect(group).toMatchObject({ id: 'mg-1' });
      expect(group.modifiers).toHaveLength(1);
      expect(group.modifiers[0]).toMatchObject({ id: 'mod-1' });
    });
  });

  describe('create', () => {
    it('creates a modifier group and returns the created entity', async () => {
      const group = await modifierGroupsApi.create(BRAND_SLUG, {
        translations: [{ languageCode: 'nl', name: 'Extras' }],
        modifiers: [
          {
            translations: [{ languageCode: 'nl', name: 'Extra kaas' }],
            priceAdjustment: 0.5,
            sortOrder: 1,
          },
        ],
      });

      expect(group).toMatchObject({ id: 'mg-new' });
    });
  });

  describe('update', () => {
    it('updates a modifier group and returns the updated entity', async () => {
      const group = await modifierGroupsApi.update(BRAND_SLUG, 'mg-1', {
        translations: [{ languageCode: 'nl', name: 'Sauzen Updated' }],
        modifiers: [],
      });

      expect(group).toMatchObject({ id: 'mg-1' });
    });
  });

  describe('remove', () => {
    it('deletes a modifier group without throwing', async () => {
      await expect(modifierGroupsApi.remove(BRAND_SLUG, 'mg-1')).resolves.toBeUndefined();
    });
  });

  describe('getProductModifierGroups', () => {
    it('returns modifier groups for a product', async () => {
      const groups = await modifierGroupsApi.getProductModifierGroups(BRAND_SLUG, 'prod-1');
      expect(groups).toHaveLength(1);
      expect(groups[0]).toMatchObject({ modifierGroupId: 'mg-1' });
    });
  });

  describe('setProductModifierGroups', () => {
    it('sets modifier groups on a product without throwing', async () => {
      await expect(
        modifierGroupsApi.setProductModifierGroups(BRAND_SLUG, 'prod-1', {
          modifierGroups: [{ modifierGroupId: 'mg-1', sortOrder: 1 }],
        }),
      ).resolves.toBeUndefined();
    });
  });
});
