import { useCallback, useEffect } from 'react';
import { useQuery, useQueryClient } from '@tanstack/react-query';
import { AuthContext } from './AuthContext';
import { getUser, login, logout } from '@api/auth';
import { useSessionKeepalive } from './useSessionKeepalive';
import type { UserRole } from '@/types/auth';

const USER_QUERY_KEY = ['bff', 'user'] as const;

interface BffAuthProviderProps {
  children: React.ReactNode;
}

/**
 * Auth provider that fetches session state from the BFF.
 * Used in non-mock environments (staging, production).
 *
 * - Fetches /bff/user on mount; never retries (avoid hammering on 401)
 * - Listens for the 'auth:session-expired' window event and invalidates
 *   the user query so the app reflects the logged-out state
 */
export function BffAuthProvider({ children }: BffAuthProviderProps) {
  const queryClient = useQueryClient();

  const { data: user = null, isLoading } = useQuery({
    queryKey: USER_QUERY_KEY,
    queryFn: getUser,
    retry: false,
    staleTime: 5 * 60 * 1000,
  });

  // When the API client fires a session-expired event, invalidate the user query
  useEffect(() => {
    function handleSessionExpired() {
      void queryClient.invalidateQueries({ queryKey: USER_QUERY_KEY });
    }

    window.addEventListener('auth:session-expired', handleSessionExpired);
    return () => {
      window.removeEventListener('auth:session-expired', handleSessionExpired);
    };
  }, [queryClient]);

  const isAuthenticated = user !== null;

  const handleLogin = useCallback((persona?: string, returnUrl?: string) => {
    login(persona, returnUrl);
  }, []);

  const handleLogout = useCallback(async () => {
    await logout();
  }, []);

  const hasRole = useCallback(
    (role: UserRole): boolean => user?.roles.includes(role) ?? false,
    [user],
  );

  const hasAnyRole = useCallback(
    (roles: UserRole[]): boolean => roles.some((r) => user?.roles.includes(r) ?? false),
    [user],
  );

  useSessionKeepalive(isAuthenticated);

  return (
    <AuthContext.Provider
      value={{
        isAuthenticated,
        user,
        isLoading,
        login: handleLogin,
        logout: handleLogout,
        hasRole,
        hasAnyRole,
      }}
    >
      {children}
    </AuthContext.Provider>
  );
}
