import { describe, it, expect } from 'vitest';
import { render, screen } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';
import { AuthContext } from '../AuthContext';
import { RequireAuth } from '../RequireAuth';
import type { AuthContextValue } from '../AuthContext';
import type { UserRole } from '@/types/auth';

/**
 * Factory for AuthContextValue to reduce boilerplate in tests.
 */
function makeAuthContext(overrides: Partial<AuthContextValue>): AuthContextValue {
  return {
    isAuthenticated: false,
    user: null,
    isLoading: false,
    login: () => undefined,
    logout: () => Promise.resolve(),
    hasRole: () => false,
    hasAnyRole: () => false,
    ...overrides,
  };
}

function renderRequireAuth(ctx: AuthContextValue, roles?: UserRole[]) {
  const guarded = roles ? (
    <RequireAuth roles={roles}>
      <div data-testid="protected-content">Protected</div>
    </RequireAuth>
  ) : (
    <RequireAuth>
      <div data-testid="protected-content">Protected</div>
    </RequireAuth>
  );

  return render(
    <MemoryRouter>
      <AuthContext.Provider value={ctx}>{guarded}</AuthContext.Provider>
    </MemoryRouter>,
  );
}

/**
 * Tests for src/auth/RequireAuth.tsx
 *
 * Four states:
 * 1. isLoading → spinner
 * 2. not authenticated → login button
 * 3. authenticated but missing required role → access denied
 * 4. authenticated + role matched → renders children
 */
describe('RequireAuth', () => {
  it('renders a spinner while loading', () => {
    const { container } = renderRequireAuth(makeAuthContext({ isLoading: true }));

    // The spinner is the element with animate-spin class
    const spinner = container.querySelector('.animate-spin');
    expect(spinner).toBeInTheDocument();
    expect(screen.queryByTestId('protected-content')).not.toBeInTheDocument();
  });

  it('renders a login button when not authenticated', () => {
    renderRequireAuth(makeAuthContext({ isAuthenticated: false, isLoading: false }));

    expect(screen.getByRole('button', { name: 'Sign in' })).toBeInTheDocument();
    expect(screen.queryByTestId('protected-content')).not.toBeInTheDocument();
  });

  it('renders access denied when authenticated but missing required role', () => {
    renderRequireAuth(
      makeAuthContext({
        isAuthenticated: true,
        user: {
          userId: 'user-1',
          displayName: 'User',
          email: 'user@test.com',
          roles: ['counter-staff'],
          brandSlug: 'frietjes',
        },
        hasAnyRole: () => false,
      }),
      ['brand-admin', 'platform-admin'],
    );

    expect(screen.getByText('Access denied.')).toBeInTheDocument();
    expect(screen.queryByTestId('protected-content')).not.toBeInTheDocument();
  });

  it('renders children when authenticated and role matches', () => {
    renderRequireAuth(
      makeAuthContext({
        isAuthenticated: true,
        user: {
          userId: 'user-1',
          displayName: 'Brand Admin',
          email: 'admin@frietjes.be',
          roles: ['brand-admin'],
          brandSlug: 'frietjes',
        },
        hasAnyRole: (roles) => roles.includes('brand-admin'),
      }),
      ['brand-admin'],
    );

    expect(screen.getByTestId('protected-content')).toBeInTheDocument();
    expect(screen.queryByText('Access denied.')).not.toBeInTheDocument();
  });

  it('renders children when authenticated and no roles are required', () => {
    renderRequireAuth(
      makeAuthContext({
        isAuthenticated: true,
        user: {
          userId: 'user-1',
          displayName: 'Any User',
          email: 'user@test.com',
          roles: ['customer'],
          brandSlug: null,
        },
        hasAnyRole: () => false,
      }),
      // No roles argument — public route
    );

    expect(screen.getByTestId('protected-content')).toBeInTheDocument();
  });
});
