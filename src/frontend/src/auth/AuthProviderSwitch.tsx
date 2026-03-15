import { BffAuthProvider } from './BffAuthProvider';
import { MockAuthProvider } from './MockAuthProvider';

interface AuthProviderSwitchProps {
  children: React.ReactNode;
}

/**
 * Renders either the mock or the real BFF auth provider based on the
 * VITE_MOCK_AUTH environment variable.
 *
 * - VITE_MOCK_AUTH=true  → MockAuthProvider (local dev, no HTTP calls)
 * - anything else        → BffAuthProvider  (staging / production)
 */
export function AuthProviderSwitch({ children }: AuthProviderSwitchProps) {
  const useMock = import.meta.env.VITE_MOCK_AUTH === 'true';

  if (useMock) {
    return <MockAuthProvider>{children}</MockAuthProvider>;
  }

  return <BffAuthProvider>{children}</BffAuthProvider>;
}
