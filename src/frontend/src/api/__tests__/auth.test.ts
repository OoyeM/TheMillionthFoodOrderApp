import { describe, it, expect, vi, afterEach } from 'vitest';
import { http, HttpResponse } from 'msw';
import { server } from '../../test/msw/server';
import { getUser, login, logout, keepalive } from '../auth';

/**
 * Tests for src/api/auth.ts — BFF auth endpoints.
 *
 * bffClient uses baseURL '/bff'. MSW matches by relative path.
 */
describe('auth API', () => {
  const originalLocation = window.location;

  afterEach(() => {
    vi.restoreAllMocks();
    Object.defineProperty(window, 'location', {
      configurable: true,
      value: originalLocation,
    });
  });

  describe('getUser', () => {
    it('maps authenticated BFF response to AuthUser', async () => {
      const user = await getUser();

      expect(user).toEqual({
        userId: 'user-1',
        displayName: 'Test User',
        email: 'test@example.com',
        roles: ['brand-admin'],
        brandSlug: 'frietjes',
        firstName: 'Test',
        lastName: 'User',
        phoneNumber: '+32470000001',
      });
    });

    it('returns null when user is not authenticated', async () => {
      server.use(
        http.get('/bff/user', () =>
          HttpResponse.json({ isAuthenticated: false }),
        ),
      );

      const user = await getUser();
      expect(user).toBeNull();
    });

    it('filters out unknown role strings', async () => {
      server.use(
        http.get('/bff/user', () =>
          HttpResponse.json({
            isAuthenticated: true,
            userId: 'user-2',
            displayName: 'Test',
            email: 'test@test.com',
            roles: ['brand-admin', 'unknown-role', 'super-hacker'],
            brandSlug: null,
          }),
        ),
      );

      const user = await getUser();
      expect(user?.roles).toEqual(['brand-admin']);
    });
  });

  describe('login', () => {
    it('sets window.location.href to /bff/login without params', () => {
      let capturedHref = '';
      Object.defineProperty(window, 'location', {
        configurable: true,
        value: {
          // eslint-disable-next-line @typescript-eslint/no-misused-spread -- intentional shallow copy of Location's own enumerable props for a test stub; prototype not needed
          ...window.location,
          set href(url: string) {
            capturedHref = url;
          },
        },
      });

      login();
      expect(capturedHref).toBe('/bff/login');
    });

    it('includes mock persona in query string when provided', () => {
      let capturedHref = '';
      Object.defineProperty(window, 'location', {
        configurable: true,
        value: {
          // eslint-disable-next-line @typescript-eslint/no-misused-spread -- intentional shallow copy of Location's own enumerable props for a test stub; prototype not needed
          ...window.location,
          set href(url: string) {
            capturedHref = url;
          },
        },
      });

      login('brand-admin@frietjes');
      expect(capturedHref).toContain('mock=brand-admin%40frietjes');
    });

    it('includes returnUrl in query string when provided', () => {
      let capturedHref = '';
      Object.defineProperty(window, 'location', {
        configurable: true,
        value: {
          // eslint-disable-next-line @typescript-eslint/no-misused-spread -- intentional shallow copy of Location's own enumerable props for a test stub; prototype not needed
          ...window.location,
          set href(url: string) {
            capturedHref = url;
          },
        },
      });

      login(undefined, '/admin/brands');
      expect(capturedHref).toContain('returnUrl=%2Fadmin%2Fbrands');
    });
  });

  describe('logout', () => {
    it('posts to /bff/logout and reloads the page', async () => {
      let logoutCalled = false;
      let reloadCalled = false;
      server.use(
        http.post('/bff/logout', () => {
          logoutCalled = true;
          return new HttpResponse(null, { status: 200 });
        }),
      );

      Object.defineProperty(window, 'location', {
        configurable: true,
        value: {
          // eslint-disable-next-line @typescript-eslint/no-misused-spread -- intentional shallow copy of Location's own enumerable props for a test stub; prototype not needed
          ...window.location,
          reload: () => {
            reloadCalled = true;
          },
        },
      });

      await logout();

      expect(logoutCalled).toBe(true);
      expect(reloadCalled).toBe(true);
    });
  });

  describe('keepalive', () => {
    it('returns true on successful keepalive', async () => {
      const result = await keepalive();
      expect(result).toBe(true);
    });

    it('returns false on 401 (session expired)', async () => {
      server.use(
        http.post('/bff/session/keepalive', () =>
          new HttpResponse(null, { status: 401 }),
        ),
      );

      const result = await keepalive();
      expect(result).toBe(false);
    });

    it('returns false on network error', async () => {
      server.use(
        http.post('/bff/session/keepalive', () => HttpResponse.error()),
      );

      const result = await keepalive();
      expect(result).toBe(false);
    });
  });
});
