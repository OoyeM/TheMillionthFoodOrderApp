import { useEffect, useRef } from 'react';
import { keepalive } from '@api/auth';

const KEEPALIVE_INTERVAL_MS = 15 * 60 * 1000; // 15 minutes
const ACTIVITY_DEBOUNCE_MS = 500;

/**
 * Sends a session keepalive request every 15 minutes while the user is
 * authenticated and has been active (mouse/keyboard).
 *
 * Only runs when:
 * 1. The user is authenticated
 * 2. VITE_MOCK_AUTH is NOT 'true' (no-op in dev mock mode)
 *
 * If the keepalive returns false (401), it dispatches `auth:session-expired`
 * so the BffAuthProvider can invalidate the user query.
 */
export function useSessionKeepalive(isAuthenticated: boolean): void {
  const isMock = import.meta.env.VITE_MOCK_AUTH === 'true';
  const lastActivityRef = useRef<number>(Date.now());
  const debounceTimerRef = useRef<ReturnType<typeof setTimeout> | null>(null);

  // Track user activity — debounced to avoid excessive writes
  useEffect(() => {
    if (!isAuthenticated || isMock) return;

    function handleActivity() {
      if (debounceTimerRef.current !== null) {
        clearTimeout(debounceTimerRef.current);
      }
      debounceTimerRef.current = setTimeout(() => {
        lastActivityRef.current = Date.now();
      }, ACTIVITY_DEBOUNCE_MS);
    }

    window.addEventListener('mousemove', handleActivity, { passive: true });
    window.addEventListener('keydown', handleActivity, { passive: true });
    window.addEventListener('click', handleActivity, { passive: true });

    return () => {
      window.removeEventListener('mousemove', handleActivity);
      window.removeEventListener('keydown', handleActivity);
      window.removeEventListener('click', handleActivity);

      if (debounceTimerRef.current !== null) {
        clearTimeout(debounceTimerRef.current);
      }
    };
  }, [isAuthenticated, isMock]);

  // Send keepalive on interval when user has been active
  useEffect(() => {
    if (!isAuthenticated || isMock) return;

    const intervalId = setInterval(() => {
      const timeSinceActivity = Date.now() - lastActivityRef.current;

      // Only keepalive if there was activity within the last interval period
      if (timeSinceActivity <= KEEPALIVE_INTERVAL_MS) {
        void keepalive().then((alive) => {
          if (!alive) {
            window.dispatchEvent(new CustomEvent('auth:session-expired'));
          }
        });
      }
    }, KEEPALIVE_INTERVAL_MS);

    return () => {
      clearInterval(intervalId);
    };
  }, [isAuthenticated, isMock]);
}
