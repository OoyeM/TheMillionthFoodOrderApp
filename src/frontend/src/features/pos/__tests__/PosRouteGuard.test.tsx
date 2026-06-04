/**
 * t-14 — /pos requires a staff role [AC6 precondition]
 * - Staff renders the dashboard
 * - Anonymous / customer is blocked
 *
 * We provide AuthContext directly to control the role returned.
 */
import { describe, it, expect, vi } from 'vitest';
import { screen } from '@testing-library/react';
import { render } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { MemoryRouter } from 'react-router-dom';
import type { AuthContextValue } from '@/auth/AuthContext';
import { AuthContext } from '@/auth/AuthContext';
import { RequireAuth } from '@/auth/RequireAuth';
import type { UserRole } from '@/types/auth';

// ── Helpers ──────────────────────────────────────────────────────────────────

function createAuthContext(overrides: Partial<AuthContextValue> = {}): AuthContextValue {
  return {
    isAuthenticated: true,
    isLoading: false,
    user: {
      userId: 'user-1',
      displayName: 'Test User',
      email: 'test@example.com',
      roles: ['counter-staff'],
      brandSlug: 'frietjes',
      firstName: null,
      lastName: null,
      phoneNumber: null,
    },
    login: vi.fn(),
    logout: vi.fn().mockResolvedValue(undefined) as () => Promise<void>,
    hasRole: (role: UserRole) => overrides.user?.roles.includes(role) ?? false,
    hasAnyRole: (roles: UserRole[]) =>
      roles.some((r) => overrides.user?.roles.includes(r) ?? false),
    ...overrides,
  };
}

function renderWithAuth(
  authCtx: AuthContextValue,
  children: React.ReactNode,
) {
  const client = new QueryClient({
    defaultOptions: { queries: { retry: false }, mutations: { retry: false } },
  });

  return render(
    <QueryClientProvider client={client}>
      <MemoryRouter initialEntries={['/frietjes/nl/pos']}>
        <AuthContext.Provider value={authCtx}>
          {children}
        </AuthContext.Provider>
      </MemoryRouter>
    </QueryClientProvider>,
  );
}

const POS_STAFF_ROLES: UserRole[] = [
  'counter-staff',
  'floor-staff',
  'kitchen-staff',
  'shop-manager',
];

// ── Tests ─────────────────────────────────────────────────────────────────────

describe('POS route guard (t-14)', () => {
  it('renders children for counter-staff role', () => {
    const authCtx = createAuthContext({
      isAuthenticated: true,
      user: {
        userId: 'u1',
        displayName: 'Counter Staff',
        email: 'staff@test.com',
        roles: ['counter-staff'],
        brandSlug: 'frietjes',
        firstName: null,
        lastName: null,
        phoneNumber: null,
      },
      hasAnyRole: (roles) => roles.some((r) => r === 'counter-staff'),
    });

    renderWithAuth(
      authCtx,
      <RequireAuth roles={POS_STAFF_ROLES}>
        <div data-testid="pos-content">POS Dashboard</div>
      </RequireAuth>,
    );

    expect(screen.getByTestId('pos-content')).toBeInTheDocument();
  });

  it('renders children for shop-manager role', () => {
    const authCtx = createAuthContext({
      isAuthenticated: true,
      user: {
        userId: 'u2',
        displayName: 'Shop Manager',
        email: 'manager@test.com',
        roles: ['shop-manager'],
        brandSlug: 'frietjes',
        firstName: null,
        lastName: null,
        phoneNumber: null,
      },
      hasAnyRole: (roles) => roles.some((r) => r === 'shop-manager'),
    });

    renderWithAuth(
      authCtx,
      <RequireAuth roles={POS_STAFF_ROLES}>
        <div data-testid="pos-content">POS Dashboard</div>
      </RequireAuth>,
    );

    expect(screen.getByTestId('pos-content')).toBeInTheDocument();
  });

  it('blocks anonymous users (not authenticated)', () => {
    const authCtx: AuthContextValue = {
      isAuthenticated: false,
      isLoading: false,
      user: null,
      login: vi.fn(),
      logout: vi.fn().mockResolvedValue(undefined) as () => Promise<void>,
      hasRole: () => false,
      hasAnyRole: () => false,
    };

    renderWithAuth(
      authCtx,
      <RequireAuth roles={POS_STAFF_ROLES}>
        <div data-testid="pos-content">POS Dashboard</div>
      </RequireAuth>,
    );

    expect(screen.queryByTestId('pos-content')).not.toBeInTheDocument();
    // RequireAuth shows a sign-in prompt for unauthenticated users
    expect(screen.getByText(/sign in/i)).toBeInTheDocument();
  });

  it('blocks customer role', () => {
    const authCtx = createAuthContext({
      isAuthenticated: true,
      user: {
        userId: 'u3',
        displayName: 'Customer',
        email: 'customer@test.com',
        roles: ['customer'],
        brandSlug: 'frietjes',
        firstName: null,
        lastName: null,
        phoneNumber: null,
      },
      hasAnyRole: (roles) => roles.some((r) => r === 'customer'),
    });

    renderWithAuth(
      authCtx,
      <RequireAuth roles={POS_STAFF_ROLES}>
        <div data-testid="pos-content">POS Dashboard</div>
      </RequireAuth>,
    );

    expect(screen.queryByTestId('pos-content')).not.toBeInTheDocument();
    expect(screen.getByText(/access denied/i)).toBeInTheDocument();
  });

  it('blocks brand-admin role (not a POS staff role)', () => {
    const authCtx = createAuthContext({
      isAuthenticated: true,
      user: {
        userId: 'u4',
        displayName: 'Brand Admin',
        email: 'admin@test.com',
        roles: ['brand-admin'],
        brandSlug: 'frietjes',
        firstName: null,
        lastName: null,
        phoneNumber: null,
      },
      hasAnyRole: (roles) => roles.some((r) => r === 'brand-admin'),
    });

    renderWithAuth(
      authCtx,
      <RequireAuth roles={POS_STAFF_ROLES}>
        <div data-testid="pos-content">POS Dashboard</div>
      </RequireAuth>,
    );

    expect(screen.queryByTestId('pos-content')).not.toBeInTheDocument();
    expect(screen.getByText(/access denied/i)).toBeInTheDocument();
  });
});
