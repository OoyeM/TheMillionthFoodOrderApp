import { useCallback, useState } from 'react';
import { AuthContext } from './AuthContext';
import type { AuthUser, UserRole } from '@/types/auth';

const ALL_ROLES: UserRole[] = [
  'platform-admin',
  'brand-admin',
  'shop-manager',
  'counter-staff',
  'kitchen-staff',
  'floor-staff',
  'customer',
];

function buildMockUser(role: UserRole, displayName: string): AuthUser {
  return {
    userId: 'mock-user-id',
    displayName,
    email: `${role}@mock.dev`,
    roles: [role],
    brandSlug: role === 'platform-admin' ? null : 'demo',
  };
}

interface MockAuthProviderProps {
  children: React.ReactNode;
}

/**
 * Auth provider for local development.
 * Reads initial role and display name from Vite env vars; no HTTP calls.
 * Renders a fixed dev toolbar (bottom-right) with a role switcher dropdown.
 */
export function MockAuthProvider({ children }: MockAuthProviderProps) {
  const initialRole = (import.meta.env.VITE_MOCK_ROLE as UserRole | undefined) ?? 'platform-admin';
  const initialDisplayName = import.meta.env.VITE_MOCK_DISPLAY_NAME ?? 'Dev User';

  const [currentRole, setCurrentRole] = useState<UserRole>(initialRole);
  const [user, setUser] = useState<AuthUser>(() => buildMockUser(initialRole, initialDisplayName));

  const handleRoleChange = (role: UserRole) => {
    setCurrentRole(role);
    setUser(buildMockUser(role, initialDisplayName));
  };

  // Mock login is a no-op — user is always "logged in" in dev
  const handleLogin = useCallback(() => {
    // No-op in mock mode: user is always authenticated
  }, []);

  const handleLogout = useCallback(async () => {
    // No-op in mock mode
    return Promise.resolve();
  }, []);

  const hasRole = useCallback((role: UserRole): boolean => user.roles.includes(role), [user]);

  const hasAnyRole = useCallback(
    (roles: UserRole[]): boolean => roles.some((r) => user.roles.includes(r)),
    [user],
  );

  return (
    <AuthContext.Provider
      value={{
        isAuthenticated: true,
        user,
        isLoading: false,
        login: handleLogin,
        logout: handleLogout,
        hasRole,
        hasAnyRole,
      }}
    >
      {children}
      <MockDevToolbar currentRole={currentRole} onRoleChange={handleRoleChange} />
    </AuthContext.Provider>
  );
}

// ---------------------------------------------------------------------------
// Dev toolbar — only rendered in MockAuthProvider (never shipped in production)
// ---------------------------------------------------------------------------

interface MockDevToolbarProps {
  currentRole: UserRole;
  onRoleChange: (role: UserRole) => void;
}

function MockDevToolbar({ currentRole, onRoleChange }: MockDevToolbarProps) {
  return (
    <div
      style={{
        position: 'fixed',
        bottom: '16px',
        right: '16px',
        zIndex: 9999,
        background: '#1e1e2e',
        color: '#cdd6f4',
        borderRadius: '8px',
        padding: '8px 12px',
        fontSize: '12px',
        display: 'flex',
        alignItems: 'center',
        gap: '8px',
        boxShadow: '0 4px 12px rgba(0,0,0,0.4)',
        fontFamily: 'monospace',
      }}
    >
      <span style={{ color: '#a6e3a1', fontWeight: 'bold' }}>MOCK AUTH</span>
      <select
        value={currentRole}
        onChange={(e) => onRoleChange(e.target.value as UserRole)}
        style={{
          background: '#313244',
          color: '#cdd6f4',
          border: '1px solid #45475a',
          borderRadius: '4px',
          padding: '2px 6px',
          fontSize: '12px',
          fontFamily: 'monospace',
          cursor: 'pointer',
        }}
      >
        {ALL_ROLES.map((role) => (
          <option key={role} value={role}>
            {role}
          </option>
        ))}
      </select>
    </div>
  );
}
