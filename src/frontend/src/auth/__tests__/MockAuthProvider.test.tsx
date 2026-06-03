import { describe, it, expect, vi, afterEach } from 'vitest';
import { render, screen, fireEvent } from '@testing-library/react';
import { MockAuthProvider } from '../MockAuthProvider';
import { useAuth } from '../useAuth';

/**
 * Helper component that exposes auth context values via accessible text.
 */
function AuthDisplay() {
  const { user, isAuthenticated, isLoading } = useAuth();
  return (
    <div>
      <span data-testid="is-authenticated">{String(isAuthenticated)}</span>
      <span data-testid="is-loading">{String(isLoading)}</span>
      <span data-testid="display-name">{user?.displayName ?? 'null'}</span>
      <span data-testid="role">{user?.roles[0] ?? 'null'}</span>
    </div>
  );
}

/**
 * Tests for src/auth/MockAuthProvider.tsx
 *
 * - Reads VITE_MOCK_ROLE / VITE_MOCK_DISPLAY_NAME via mocked import.meta.env
 * - Role-switcher toolbar updates context
 * - Always authenticated (isLoading=false, isAuthenticated=true)
 */
describe('MockAuthProvider', () => {
  afterEach(() => {
    vi.unstubAllEnvs();
  });

  it('is always authenticated with isLoading=false', () => {
    render(
      <MockAuthProvider>
        <AuthDisplay />
      </MockAuthProvider>,
    );

    expect(screen.getByTestId('is-authenticated').textContent).toBe('true');
    expect(screen.getByTestId('is-loading').textContent).toBe('false');
  });

  it('uses VITE_MOCK_ROLE env var for the initial role', () => {
    vi.stubEnv('VITE_MOCK_ROLE', 'platform-admin');

    render(
      <MockAuthProvider>
        <AuthDisplay />
      </MockAuthProvider>,
    );

    expect(screen.getByTestId('role').textContent).toBe('platform-admin');
  });

  it('uses VITE_MOCK_DISPLAY_NAME env var for the display name', () => {
    vi.stubEnv('VITE_MOCK_ROLE', 'brand-admin');
    vi.stubEnv('VITE_MOCK_DISPLAY_NAME', 'Friet Baron');

    render(
      <MockAuthProvider>
        <AuthDisplay />
      </MockAuthProvider>,
    );

    expect(screen.getByTestId('display-name').textContent).toBe('Friet Baron');
  });

  it('falls back to platform-admin role when VITE_MOCK_ROLE is not set', () => {
    // Do NOT stub VITE_MOCK_ROLE — leave it undefined so the ?? fallback triggers.
    // When neither env var is stubbed, import.meta.env.VITE_MOCK_ROLE is undefined.
    vi.stubEnv('VITE_MOCK_DISPLAY_NAME', 'Dev User');

    render(
      <MockAuthProvider>
        <AuthDisplay />
      </MockAuthProvider>,
    );

    // undefined ?? 'platform-admin' → 'platform-admin'
    expect(screen.getByTestId('role').textContent).toBe('platform-admin');
    expect(screen.getByTestId('display-name').textContent).toBe('Dev User');
  });

  it('renders the role-switcher toolbar', () => {
    render(
      <MockAuthProvider>
        <div />
      </MockAuthProvider>,
    );

    expect(screen.getByText('MOCK AUTH')).toBeInTheDocument();
    expect(screen.getByRole('combobox')).toBeInTheDocument();
  });

  it('updates auth context when the role switcher changes', () => {
    vi.stubEnv('VITE_MOCK_ROLE', 'brand-admin');

    render(
      <MockAuthProvider>
        <AuthDisplay />
      </MockAuthProvider>,
    );

    const select = screen.getByRole('combobox');
    fireEvent.change(select, { target: { value: 'counter-staff' } });

    expect(screen.getByTestId('role').textContent).toBe('counter-staff');
  });

  it('enables the BFF login button for a role with a server persona', () => {
    vi.stubEnv('VITE_MOCK_ROLE', 'platform-admin');

    render(
      <MockAuthProvider>
        <div />
      </MockAuthProvider>,
    );

    expect(screen.getByRole('button', { name: /bff login/i })).toBeEnabled();
  });

  it('disables the BFF login button for roles without a server persona', () => {
    vi.stubEnv('VITE_MOCK_ROLE', 'shop-manager');

    render(
      <MockAuthProvider>
        <div />
      </MockAuthProvider>,
    );

    expect(screen.getByRole('button', { name: /bff login/i })).toBeDisabled();
  });

  it('navigates to /bff/login with the mapped persona and current page as returnUrl', () => {
    vi.stubEnv('VITE_MOCK_ROLE', 'brand-admin');

    const assign = vi.fn();
    const originalLocation = window.location;
    Object.defineProperty(window, 'location', {
      configurable: true,
      value: { assign, pathname: '/frietjes/nl/admin/brands', search: '' },
    });

    try {
      render(
        <MockAuthProvider>
          <div />
        </MockAuthProvider>,
      );

      fireEvent.click(screen.getByRole('button', { name: /bff login/i }));

      expect(assign).toHaveBeenCalledTimes(1);
      const url = String(assign.mock.calls[0]?.[0]);
      // brand-admin → brand-admin@frietjes (encoded), returnUrl = current path (encoded)
      expect(url).toContain('/bff/login?mock=brand-admin%40frietjes');
      expect(url).toContain('returnUrl=%2Ffrietjes%2Fnl%2Fadmin%2Fbrands');
    } finally {
      Object.defineProperty(window, 'location', {
        configurable: true,
        value: originalLocation,
      });
    }
  });
});
