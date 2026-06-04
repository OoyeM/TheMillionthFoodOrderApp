import { describe, it, expect } from 'vitest';
import { renderHook } from '@testing-library/react';
import { AuthContext } from '../AuthContext';
import { useAuth } from '../useAuth';
import type { AuthContextValue } from '../AuthContext';

/**
 * Tests for src/auth/useAuth.ts
 *
 * - Throws when called outside an AuthContext provider (context is null)
 * - Returns context value when called inside a provider
 */
describe('useAuth', () => {
  it('throws when called outside an AuthContext provider', () => {
    // renderHook without a wrapper — context defaults to null
    expect(() => {
      renderHook(() => useAuth());
    }).toThrow('useAuth must be used within an AuthProviderSwitch (AuthContext is null).');
  });

  it('returns the context value when called inside a provider', () => {
    const mockContextValue: AuthContextValue = {
      isAuthenticated: true,
      user: {
        userId: 'user-1',
        displayName: 'Test User',
        email: 'test@example.com',
        roles: ['brand-admin'],
        brandSlug: 'frietjes',
        firstName: null,
        lastName: null,
        phoneNumber: null,
      },
      isLoading: false,
      login: () => undefined,
      logout: () => Promise.resolve(),
      hasRole: (role) => role === 'brand-admin',
      hasAnyRole: (roles) => roles.includes('brand-admin'),
    };

    const wrapper = ({ children }: { children: React.ReactNode }) => (
      <AuthContext.Provider value={mockContextValue}>{children}</AuthContext.Provider>
    );

    const { result } = renderHook(() => useAuth(), { wrapper });

    expect(result.current.isAuthenticated).toBe(true);
    expect(result.current.user?.displayName).toBe('Test User');
    expect(result.current.hasRole('brand-admin')).toBe(true);
    expect(result.current.hasRole('platform-admin')).toBe(false);
    expect(result.current.hasAnyRole(['platform-admin', 'brand-admin'])).toBe(true);
  });

  it('returns isLoading state from context', () => {
    const loadingContextValue: AuthContextValue = {
      isAuthenticated: false,
      user: null,
      isLoading: true,
      login: () => undefined,
      logout: () => Promise.resolve(),
      hasRole: () => false,
      hasAnyRole: () => false,
    };

    const wrapper = ({ children }: { children: React.ReactNode }) => (
      <AuthContext.Provider value={loadingContextValue}>{children}</AuthContext.Provider>
    );

    const { result } = renderHook(() => useAuth(), { wrapper });

    expect(result.current.isLoading).toBe(true);
    expect(result.current.isAuthenticated).toBe(false);
    expect(result.current.user).toBeNull();
  });
});
