import { useContext } from 'react';
import { AuthContext } from './AuthContext';
import type { AuthContextValue } from './AuthContext';

/**
 * Returns the current auth context value.
 * Throws if called outside an AuthProviderSwitch tree.
 */
export function useAuth(): AuthContextValue {
  const ctx = useContext(AuthContext);

  if (ctx === null) {
    throw new Error('useAuth must be used within an AuthProviderSwitch (AuthContext is null).');
  }

  return ctx;
}
