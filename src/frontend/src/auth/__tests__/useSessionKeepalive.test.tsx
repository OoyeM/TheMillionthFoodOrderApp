import { describe, it, expect, vi, afterEach, beforeEach } from 'vitest';
import { renderHook, act } from '@testing-library/react';
import { http, HttpResponse } from 'msw';
import { server } from '../../test/msw/server';
import { useSessionKeepalive } from '../useSessionKeepalive';
import { mockEndpoint } from '../../test/mswHelpers';
import { expectAuthSessionExpired } from '../../test/authExpiredHarness';

function trackKeepaliveCall(): { wasCalled: () => boolean } {
  const state = { called: false };
  server.use(
    http.post('/bff/session/keepalive', () => {
      state.called = true;
      return new HttpResponse(null, { status: 200 });
    }),
  );
  return { wasCalled: () => state.called };
}

/**
 * Tests for src/auth/useSessionKeepalive.ts
 *
 * The hook uses:
 * - setInterval at 15 min to send keepalive (only when active)
 * - lastActivityRef updated on mousemove/keydown/click with debounce
 * - Skips when VITE_MOCK_AUTH=true or isAuthenticated=false
 *
 * NOTE: bffClient uses baseURL '/bff' which in jsdom resolves to
 * /bff. MSW node intercepts /bff/*.
 */
describe('useSessionKeepalive', () => {
  beforeEach(() => {
    vi.useFakeTimers();
    vi.stubEnv('VITE_MOCK_AUTH', 'false');
  });

  afterEach(() => {
    vi.useRealTimers();
    vi.unstubAllEnvs();
    vi.restoreAllMocks();
  });

  it('does nothing when VITE_MOCK_AUTH=true (mock mode)', async () => {
    vi.stubEnv('VITE_MOCK_AUTH', 'true');
    const tracker = trackKeepaliveCall();

    renderHook(() => { useSessionKeepalive(true); });

    window.dispatchEvent(new MouseEvent('mousemove'));
    await act(async () => {
      vi.advanceTimersByTime(15 * 60 * 1000 + 100);
      await Promise.resolve();
    });

    expect(tracker.wasCalled()).toBe(false);
  });

  it('does nothing when not authenticated', async () => {
    const tracker = trackKeepaliveCall();

    renderHook(() => { useSessionKeepalive(false); });

    window.dispatchEvent(new MouseEvent('mousemove'));
    await act(async () => {
      vi.advanceTimersByTime(15 * 60 * 1000 + 100);
      await Promise.resolve();
    });

    expect(tracker.wasCalled()).toBe(false);
  });

  it('calls keepalive when there was recent user activity', async () => {
    const tracker = trackKeepaliveCall();

    renderHook(() => { useSessionKeepalive(true); });
    window.dispatchEvent(new MouseEvent('mousemove'));
    await act(async () => {
      vi.advanceTimersByTime(600);
    });
    await act(async () => {
      vi.advanceTimersByTime(15 * 60 * 1000);
      await Promise.resolve();
    });

    expect(tracker.wasCalled()).toBe(true);
  });

  it('dispatches auth:session-expired when keepalive returns 401', async () => {
    server.use(mockEndpoint('post', '/bff/session/keepalive', 401));

    await expectAuthSessionExpired(async () => {
      renderHook(() => { useSessionKeepalive(true); });
      window.dispatchEvent(new MouseEvent('mousemove'));
      await act(async () => {
        vi.advanceTimersByTime(600);
      });
      await act(async () => {
        vi.advanceTimersByTime(15 * 60 * 1000);
        await Promise.resolve();
      });
    });
  });

  it('cleans up event listeners and interval on unmount', () => {
    const removeEventListenerSpy = vi.spyOn(window, 'removeEventListener');

    const { unmount } = renderHook(() => { useSessionKeepalive(true); });
    unmount();

    // Should clean up at least mousemove, keydown, click listeners
    expect(removeEventListenerSpy).toHaveBeenCalledWith('mousemove', expect.any(Function));
  });
});
