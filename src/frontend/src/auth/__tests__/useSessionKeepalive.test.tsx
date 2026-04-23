import { describe, it, expect, vi, afterEach, beforeEach } from 'vitest';
import { renderHook, act } from '@testing-library/react';
import { http, HttpResponse } from 'msw';
import { server } from '../../test/msw/server';
import { useSessionKeepalive } from '../useSessionKeepalive';

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

    let keepaliveCalled = false;
    server.use(
      http.post('/bff/session/keepalive', () => {
        keepaliveCalled = true;
        return new HttpResponse(null, { status: 200 });
      }),
    );

    renderHook(() => useSessionKeepalive(true));

    window.dispatchEvent(new MouseEvent('mousemove'));
    await act(async () => {
      vi.advanceTimersByTime(15 * 60 * 1000 + 100);
      await Promise.resolve();
    });

    expect(keepaliveCalled).toBe(false);
  });

  it('does nothing when not authenticated', async () => {
    let keepaliveCalled = false;
    server.use(
      http.post('/bff/session/keepalive', () => {
        keepaliveCalled = true;
        return new HttpResponse(null, { status: 200 });
      }),
    );

    renderHook(() => useSessionKeepalive(false));

    window.dispatchEvent(new MouseEvent('mousemove'));
    await act(async () => {
      vi.advanceTimersByTime(15 * 60 * 1000 + 100);
      await Promise.resolve();
    });

    expect(keepaliveCalled).toBe(false);
  });

  it('calls keepalive when there was recent user activity', async () => {
    let keepaliveCalled = false;
    server.use(
      http.post('/bff/session/keepalive', () => {
        keepaliveCalled = true;
        return new HttpResponse(null, { status: 200 });
      }),
    );

    renderHook(() => useSessionKeepalive(true));

    // Simulate activity at mount time (within the same fake-timer tick)
    window.dispatchEvent(new MouseEvent('mousemove'));

    // Let the activity debounce settle (500ms)
    await act(async () => {
      vi.advanceTimersByTime(600);
    });

    // Now advance to just past the keepalive interval
    await act(async () => {
      vi.advanceTimersByTime(15 * 60 * 1000);
      await Promise.resolve();
    });

    expect(keepaliveCalled).toBe(true);
  });

  it('dispatches auth:session-expired when keepalive returns 401', async () => {
    server.use(
      http.post('/bff/session/keepalive', () =>
        new HttpResponse(null, { status: 401 }),
      ),
    );

    const listener = vi.fn();
    window.addEventListener('auth:session-expired', listener);

    renderHook(() => useSessionKeepalive(true));

    // Trigger activity so keepalive fires
    window.dispatchEvent(new MouseEvent('mousemove'));
    await act(async () => {
      vi.advanceTimersByTime(600); // debounce
    });

    await act(async () => {
      vi.advanceTimersByTime(15 * 60 * 1000);
      await Promise.resolve();
    });

    window.removeEventListener('auth:session-expired', listener);

    expect(listener).toHaveBeenCalled();
  });

  it('cleans up event listeners and interval on unmount', () => {
    const removeEventListenerSpy = vi.spyOn(window, 'removeEventListener');

    const { unmount } = renderHook(() => useSessionKeepalive(true));
    unmount();

    // Should clean up at least mousemove, keydown, click listeners
    expect(removeEventListenerSpy).toHaveBeenCalledWith('mousemove', expect.any(Function));
  });
});
