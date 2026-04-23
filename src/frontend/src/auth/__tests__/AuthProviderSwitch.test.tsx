import { describe, it, expect, vi, afterEach } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { AuthProviderSwitch } from '../AuthProviderSwitch';

/**
 * Helper that renders AuthProviderSwitch in a QueryClientProvider.
 * BffAuthProvider requires TanStack Query; MockAuthProvider does not.
 */
function renderSwitch(children: React.ReactNode) {
  const client = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(
    <QueryClientProvider client={client}>
      <AuthProviderSwitch>{children}</AuthProviderSwitch>
    </QueryClientProvider>,
  );
}

/**
 * Tests for src/auth/AuthProviderSwitch.tsx
 *
 * - Renders MockAuthProvider when VITE_MOCK_AUTH === 'true'
 * - Renders BffAuthProvider otherwise (fetches /bff/user)
 */
describe('AuthProviderSwitch', () => {
  afterEach(() => {
    vi.unstubAllEnvs();
  });

  it('renders MockAuthProvider (shows MOCK AUTH toolbar) when VITE_MOCK_AUTH=true', () => {
    vi.stubEnv('VITE_MOCK_AUTH', 'true');

    renderSwitch(<div data-testid="child">Child content</div>);

    expect(screen.getByText('MOCK AUTH')).toBeInTheDocument();
    expect(screen.getByTestId('child')).toBeInTheDocument();
  });

  it('does NOT show MOCK AUTH toolbar when VITE_MOCK_AUTH=false', async () => {
    vi.stubEnv('VITE_MOCK_AUTH', 'false');

    // BffAuthProvider fetches /bff/user — MSW handler returns authenticated user
    renderSwitch(<div data-testid="child">Child content</div>);

    // Wait for BffAuthProvider to settle
    await waitFor(() => {
      expect(screen.getByTestId('child')).toBeInTheDocument();
    });

    expect(screen.queryByText('MOCK AUTH')).not.toBeInTheDocument();
  });

  it('renders BffAuthProvider when VITE_MOCK_AUTH is unset', async () => {
    vi.stubEnv('VITE_MOCK_AUTH', '');

    renderSwitch(<div data-testid="bff-child">BFF child</div>);

    await waitFor(() => {
      expect(screen.getByTestId('bff-child')).toBeInTheDocument();
    });

    expect(screen.queryByText('MOCK AUTH')).not.toBeInTheDocument();
  });
});
