import { describe, it, expect, vi } from 'vitest';
import { http, HttpResponse } from 'msw';
import { server } from '../../test/msw/server';
import { platformAdminsApi } from '../platformAdmins';

/**
 * Tests for src/api/platformAdmins.ts
 */
describe('platformAdminsApi', () => {
  describe('list', () => {
    it('returns a list of platform admins', async () => {
      const admins = await platformAdminsApi.list();

      expect(admins).toHaveLength(1);
      expect(admins[0]).toMatchObject({
        id: 'pa-1',
        email: 'admin@platform.dev',
        isPlatformAdmin: true,
      });
    });

    it('dispatches auth:session-expired on 401', async () => {
      server.use(
        http.get('/api/platform-admins', () => new HttpResponse(null, { status: 401 })),
      );

      const listener = vi.fn();
      window.addEventListener('auth:session-expired', listener);

      try {
        await platformAdminsApi.list();
      } catch {
        // Expected
      } finally {
        window.removeEventListener('auth:session-expired', listener);
      }

      expect(listener).toHaveBeenCalledOnce();
    });
  });

  describe('invite', () => {
    it('invites a platform admin and returns the created entity', async () => {
      const admin = await platformAdminsApi.invite({
        email: 'new-admin@platform.dev',
        displayName: 'New Admin',
      });

      expect(admin).toMatchObject({
        email: 'new-admin@platform.dev',
        displayName: 'New Admin',
        isPlatformAdmin: true,
      });
    });
  });

  describe('deactivate', () => {
    it('deactivates a platform admin without throwing', async () => {
      await expect(platformAdminsApi.deactivate('pa-1')).resolves.toBeUndefined();
    });

    it('dispatches auth:session-expired on 401', async () => {
      server.use(
        http.post('/api/platform-admins/:id/deactivate', () =>
          new HttpResponse(null, { status: 401 }),
        ),
      );

      const listener = vi.fn();
      window.addEventListener('auth:session-expired', listener);

      try {
        await platformAdminsApi.deactivate('pa-1');
      } catch {
        // Expected
      } finally {
        window.removeEventListener('auth:session-expired', listener);
      }

      expect(listener).toHaveBeenCalledOnce();
    });
  });
});
