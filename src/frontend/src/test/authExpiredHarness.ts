import { expect } from 'vitest';
import { runWithEventListener } from './eventListenerHarness';

/**
 * Runs action, asserts that exactly one auth:session-expired window event was dispatched.
 *
 * The action is allowed to reject — most callers are testing that a 401 from the API client
 * causes both the rejection and the dispatch.
 */
export async function expectAuthSessionExpired(
  action: () => Promise<unknown>,
): Promise<void> {
  const { listener } = await runWithEventListener('auth:session-expired', action);
  expect(listener).toHaveBeenCalledOnce();
}

/**
 * Inverse of expectAuthSessionExpired: asserts that NO auth:session-expired event
 * was dispatched while running action.
 */
export async function expectNoAuthSessionExpired(
  action: () => Promise<unknown>,
): Promise<void> {
  const { listener } = await runWithEventListener('auth:session-expired', action);
  expect(listener).not.toHaveBeenCalled();
}
