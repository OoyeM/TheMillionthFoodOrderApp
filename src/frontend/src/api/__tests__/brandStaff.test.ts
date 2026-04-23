import { describe, it, expect, vi } from 'vitest';
import { http, HttpResponse } from 'msw';
import { server } from '../../test/msw/server';
import { brandStaffApi } from '../brandStaff';

/**
 * Tests for src/api/brandStaff.ts
 */
describe('brandStaffApi', () => {
  describe('list', () => {
    it('returns brand staff members', async () => {
      const staff = await brandStaffApi.list('frietjes');

      expect(staff).toHaveLength(1);
      expect(staff[0]).toMatchObject({
        id: 'staff-1',
        email: 'staff@frietjes.be',
        displayName: 'Staff Member',
      });
    });

    it('dispatches auth:session-expired on 401', async () => {
      server.use(
        http.get('/api/brands/:slug/staff', () => new HttpResponse(null, { status: 401 })),
      );

      const listener = vi.fn();
      window.addEventListener('auth:session-expired', listener);

      try {
        await brandStaffApi.list('frietjes');
      } catch {
        // Expected
      } finally {
        window.removeEventListener('auth:session-expired', listener);
      }

      expect(listener).toHaveBeenCalledOnce();
    });
  });

  describe('listByShop', () => {
    it('returns staff members for a specific shop', async () => {
      const staff = await brandStaffApi.listByShop('frietjes', 'shop-1');
      expect(Array.isArray(staff)).toBe(true);
    });
  });

  describe('invite', () => {
    it('invites a staff member and returns the created entity', async () => {
      const member = await brandStaffApi.invite('frietjes', {
        email: 'newstaff@frietjes.be',
        displayName: 'New Staff',
        role: 2,
        shopId: 'shop-1',
      });

      expect(member).toMatchObject({
        email: 'newstaff@frietjes.be',
        displayName: 'New Staff',
      });
    });
  });

  describe('deactivate', () => {
    it('deactivates a staff role assignment without throwing', async () => {
      await expect(brandStaffApi.deactivate('frietjes', 'role-1')).resolves.toBeUndefined();
    });

    it('dispatches auth:session-expired on 401', async () => {
      server.use(
        http.post('/api/brands/:slug/staff/:roleId/deactivate', () =>
          new HttpResponse(null, { status: 401 }),
        ),
      );

      const listener = vi.fn();
      window.addEventListener('auth:session-expired', listener);

      try {
        await brandStaffApi.deactivate('frietjes', 'role-1');
      } catch {
        // Expected
      } finally {
        window.removeEventListener('auth:session-expired', listener);
      }

      expect(listener).toHaveBeenCalledOnce();
    });
  });
});
