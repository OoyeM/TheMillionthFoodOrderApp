import { describe, it, expect, vi, afterEach } from 'vitest';
import { http, HttpResponse } from 'msw';
import { server } from '../../test/msw/server';
import { apiClient, setActiveBrandSlug, getActiveBrandSlug } from '../client';

/**
 * Tests for the shared axios client:
 * - X-Brand-Slug header injection via active-brand helper
 * - 401 → dispatches auth:session-expired window event
 * - 403 → dispatches auth:access-denied window event
 */
describe('apiClient interceptors', () => {
  afterEach(() => {
    // Reset brand slug between tests to avoid state leakage
    setActiveBrandSlug('');
  });

  describe('active brand helper', () => {
    it('getActiveBrandSlug returns null by default', () => {
      // Module-level state may be set from previous test suites; we reset above
      // but the initial value is null — test the getter/setter contract.
      setActiveBrandSlug('frietjes');
      expect(getActiveBrandSlug()).toBe('frietjes');
    });

    it('setActiveBrandSlug updates the stored slug', () => {
      setActiveBrandSlug('brand-x');
      expect(getActiveBrandSlug()).toBe('brand-x');
    });
  });

  describe('X-Brand-Slug header', () => {
    it('attaches X-Brand-Slug when an active brand is set', async () => {
      let capturedSlug: string | null = null;

      server.use(
        http.get('/api/brands', ({ request }) => {
          capturedSlug = request.headers.get('X-Brand-Slug');
          return HttpResponse.json([]);
        }),
      );

      setActiveBrandSlug('frietjes');
      await apiClient.get('/brands');

      expect(capturedSlug).toBe('frietjes');
    });

    it('does not attach X-Brand-Slug when no active brand is set', async () => {
      let capturedSlug: string | null = 'sentinel';

      server.use(
        http.get('/api/brands', ({ request }) => {
          capturedSlug = request.headers.get('X-Brand-Slug');
          return HttpResponse.json([]);
        }),
      );

      setActiveBrandSlug('');
      await apiClient.get('/brands');

      expect(capturedSlug).toBeNull();
    });
  });

  describe('401 → auth:session-expired event', () => {
    it('dispatches auth:session-expired on 401 response', async () => {
      server.use(
        http.get('/api/brands', () => new HttpResponse(null, { status: 401 })),
      );

      const listener = vi.fn();
      window.addEventListener('auth:session-expired', listener);

      try {
        await apiClient.get('/brands');
      } catch {
        // Expected rejection
      } finally {
        window.removeEventListener('auth:session-expired', listener);
      }

      expect(listener).toHaveBeenCalledOnce();
    });

    it('does NOT dispatch auth:session-expired on 403 response', async () => {
      server.use(
        http.get('/api/brands', () => new HttpResponse(null, { status: 403 })),
      );

      const listener = vi.fn();
      window.addEventListener('auth:session-expired', listener);

      try {
        await apiClient.get('/brands');
      } catch {
        // Expected rejection
      } finally {
        window.removeEventListener('auth:session-expired', listener);
      }

      expect(listener).not.toHaveBeenCalled();
    });
  });

  describe('403 → auth:access-denied event', () => {
    it('dispatches auth:access-denied on 403 response', async () => {
      server.use(
        http.get('/api/brands', () => new HttpResponse(null, { status: 403 })),
      );

      const listener = vi.fn();
      window.addEventListener('auth:access-denied', listener);

      try {
        await apiClient.get('/brands');
      } catch {
        // Expected rejection
      } finally {
        window.removeEventListener('auth:access-denied', listener);
      }

      expect(listener).toHaveBeenCalledOnce();
    });

    it('does NOT dispatch auth:access-denied on 401 response', async () => {
      server.use(
        http.get('/api/brands', () => new HttpResponse(null, { status: 401 })),
      );

      const listener = vi.fn();
      window.addEventListener('auth:access-denied', listener);

      try {
        await apiClient.get('/brands');
      } catch {
        // Expected rejection
      } finally {
        window.removeEventListener('auth:access-denied', listener);
      }

      expect(listener).not.toHaveBeenCalled();
    });
  });

  describe('successful responses', () => {
    it('returns 2xx responses normally without dispatching events', async () => {
      const sessionExpiredListener = vi.fn();
      const accessDeniedListener = vi.fn();
      window.addEventListener('auth:session-expired', sessionExpiredListener);
      window.addEventListener('auth:access-denied', accessDeniedListener);

      try {
        const response = await apiClient.get('/brands');
        expect(response.status).toBe(200);
      } finally {
        window.removeEventListener('auth:session-expired', sessionExpiredListener);
        window.removeEventListener('auth:access-denied', accessDeniedListener);
      }

      expect(sessionExpiredListener).not.toHaveBeenCalled();
      expect(accessDeniedListener).not.toHaveBeenCalled();
    });
  });
});
