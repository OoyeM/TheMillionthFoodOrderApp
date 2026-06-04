import { vi, type Mock } from 'vitest';

interface RunWithEventListenerResult {
  /** vitest mock that captures every dispatch. Assert via expect(listener).toHaveBeenCalled[…]. */
  listener: Mock;
  /** Whatever action returned (or threw — caught and stored as error). */
  result: unknown;
  /** If action rejected, the rejection is captured here. Otherwise null. */
  error: unknown;
}

/**
 * Adds a window event listener for eventName, runs action, removes the listener,
 * and returns the captured listener mock plus the action's result/error.
 *
 * Test code asserts on listener directly — expect(listener).toHaveBeenCalledOnce()
 * or expect(listener).not.toHaveBeenCalled() etc.
 *
 * Errors from action are caught (most callers test that an axios call rejects with 401
 * and don't care about the rejection itself).
 */
export async function runWithEventListener(
  eventName: string,
  action: () => unknown,
): Promise<RunWithEventListenerResult> {
  const listener = vi.fn();
  window.addEventListener(eventName, listener);
  let result: unknown = undefined;
  let error: unknown = null;
  try {
    result = await action();
  } catch (e) {
    error = e;
  } finally {
    window.removeEventListener(eventName, listener);
  }
  return { listener, result, error };
}
