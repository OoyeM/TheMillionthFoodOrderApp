import { describe, it, expect } from 'vitest';
import { server } from '../../test/msw/server';
import { brandsApi } from '../brands';
import { mockEndpoint } from '../../test/mswHelpers';
import { expectAuthSessionExpired } from '../../test/authExpiredHarness';

/**
 * Tests for src/api/brands.ts
 */
describe('brandsApi', () => {
  describe('list', () => {
    it('returns a list of brands', async () => {
      const brands = await brandsApi.list();

      expect(brands).toHaveLength(1);
      expect(brands[0]).toMatchObject({
        id: 'brand-1',
        slug: 'frietjes',
        name: 'Frietjes?',
        isActive: true,
      });
    });

    it('dispatches auth:session-expired on 401', async () => {
      server.use(mockEndpoint('get', '/api/brands', 401));
      await expectAuthSessionExpired(() => brandsApi.list());
    });
  });

  describe('get', () => {
    it('returns a single brand by id', async () => {
      const brand = await brandsApi.get('brand-1');
      expect(brand).toMatchObject({ id: 'brand-1', slug: 'frietjes' });
    });
  });

  describe('create', () => {
    it('creates a brand and returns the created entity', async () => {
      const brand = await brandsApi.create({
        name: 'New Brand',
        slug: 'new-brand',
        contactEmail: 'new@brand.com',
      });

      expect(brand).toMatchObject({
        slug: 'new-brand',
        name: 'New Brand',
        contactEmail: 'new@brand.com',
      });
    });
  });

  describe('update', () => {
    it('updates a brand and returns the updated entity', async () => {
      const brand = await brandsApi.update('brand-1', {
        name: 'Updated Name',
        contactEmail: 'updated@frietjes.be',
      });

      expect(brand).toMatchObject({ name: 'Updated Name' });
    });
  });

  describe('deactivate / activate', () => {
    it('deactivates a brand without throwing', async () => {
      await expect(brandsApi.deactivate('brand-1')).resolves.toBeUndefined();
    });

    it('activates a brand without throwing', async () => {
      await expect(brandsApi.activate('brand-1')).resolves.toBeUndefined();
    });
  });

  describe('configureStaffAuth', () => {
    it('updates the staff auth method', async () => {
      const brand = await brandsApi.configureStaffAuth('frietjes', 'GoogleSso');
      expect(brand).toMatchObject({ staffAuthMethod: 'GoogleSso' });
    });
  });
});
