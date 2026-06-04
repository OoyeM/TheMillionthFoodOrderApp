import { useAuth } from './useAuth';
import { login } from '@api/auth';
import type { UserRole } from '@/types/auth';

interface RequireAuthProps {
  /** If provided, at least one of these roles must be present on the user. */
  roles?: UserRole[];
  children: React.ReactNode;
}

/**
 * Route guard component.
 *
 * Rendering behaviour:
 * 1. isLoading   → spinner / skeleton
 * 2. not authenticated → login prompt (calls BFF login)
 * 3. authenticated but missing required role → "Access denied" message
 * 4. authenticated and role check passes → renders children
 */
export function RequireAuth({ roles, children }: RequireAuthProps) {
  const { isAuthenticated, isLoading, hasAnyRole } = useAuth();

  if (isLoading) {
    return (
      <div className="flex min-h-screen items-center justify-center">
        <div className="h-8 w-8 animate-spin rounded-full border-4 border-blue-500 border-t-transparent" />
      </div>
    );
  }

  if (!isAuthenticated) {
    return (
      <div className="flex min-h-screen flex-col items-center justify-center gap-4">
        <p className="text-lg font-medium">You must be signed in to access this page.</p>
        <button
          type="button"
          onClick={() => { login(undefined, window.location.pathname); }}
          className="rounded-md bg-blue-600 px-4 py-2 text-white hover:bg-blue-700"
        >
          Sign in
        </button>
      </div>
    );
  }

  if (roles !== undefined && roles.length > 0 && !hasAnyRole(roles)) {
    return (
      <div className="flex min-h-screen flex-col items-center justify-center gap-2">
        <p className="text-lg font-medium">Access denied.</p>
        <p className="text-sm text-gray-500">You do not have permission to view this page.</p>
      </div>
    );
  }

  return <>{children}</>;
}
