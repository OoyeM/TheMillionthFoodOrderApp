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

/**
 * Maps a frontend role to the BFF mock persona accepted by `/bff/login?mock=`.
 * Only these four personas exist server-side (see MockAuthHandler.MockPersonas);
 * roles without an entry cannot establish a real BFF session, so the toolbar's
 * "BFF login" button is disabled for them.
 */
const BFF_PERSONA: Partial<Record<UserRole, string>> = {
  'platform-admin': 'platform-admin',
  'brand-admin': 'brand-admin@frietjes',
  'counter-staff': 'counter-staff@frietjes',
  customer: 'customer',
};

function buildMockUser(role: UserRole, displayName: string): AuthUser {
  // Role-specific profile fields for US-FP-051 (digital receipt prefill).
  const profileByRole: Partial<Record<UserRole, { firstName: string; lastName: string; phoneNumber: string }>> = {
    customer: { firstName: 'Test', lastName: 'Customer', phoneNumber: '+32470000004' },
    'counter-staff': { firstName: 'Counter', lastName: 'Staff', phoneNumber: '+32470000001' },
    'brand-admin': { firstName: 'Brand', lastName: 'Admin', phoneNumber: '+32470000002' },
    'platform-admin': { firstName: 'Platform', lastName: 'Admin', phoneNumber: '+32470000003' },
  };
  const profile = profileByRole[role] ?? null;

  return {
    userId: 'mock-user-id',
    displayName,
    email: `${role}@mock.dev`,
    roles: [role],
    brandSlug: role === 'platform-admin' ? null : 'demo',
    firstName: profile?.firstName ?? null,
    lastName: profile?.lastName ?? null,
    phoneNumber: profile?.phoneNumber ?? null,
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
  // eslint-disable-next-line @typescript-eslint/no-unnecessary-condition -- env var is typed string but is undefined at runtime when unset; fallback is load-bearing
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
  const persona = BFF_PERSONA[currentRole];

  // Full-page navigation to the BFF login endpoint: it sets the session cookie and
  // redirects back to `returnUrl`, so the proxied /api/* calls become authorized.
  // (The client-side mock above only fakes the frontend's auth UI — real API calls
  // still need a BFF session, which is wiped whenever the AppHost restarts.)
  const handleBffLogin = () => {
    if (!persona) return;
    const returnUrl = window.location.pathname + window.location.search;
    window.location.assign(
      `/bff/login?mock=${encodeURIComponent(persona)}&returnUrl=${encodeURIComponent(returnUrl)}`,
    );
  };

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
        onChange={(e) => { onRoleChange(e.target.value as UserRole); }}
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
      <button
        type="button"
        onClick={handleBffLogin}
        disabled={!persona}
        title={
          persona
            ? `Sign in to the BFF as "${persona}" so /api calls are authorized, then return to this page`
            : `No BFF mock persona for "${currentRole}" — BFF supports: platform-admin, brand-admin, counter-staff, customer`
        }
        style={{
          background: persona ? '#a6e3a1' : '#313244',
          color: persona ? '#1e1e2e' : '#6c7086',
          border: persona ? 'none' : '1px solid #45475a',
          borderRadius: '4px',
          padding: '3px 8px',
          fontSize: '12px',
          fontFamily: 'monospace',
          fontWeight: 'bold',
          cursor: persona ? 'pointer' : 'not-allowed',
        }}
      >
        BFF login
      </button>
    </div>
  );
}
