import { createContext } from 'react';
import type { AuthUser, UserRole } from '@/types/auth';

export interface AuthContextValue {
  isAuthenticated: boolean;
  user: AuthUser | null;
  isLoading: boolean;
  login: (persona?: string, returnUrl?: string) => void;
  logout: () => Promise<void>;
  hasRole: (role: UserRole) => boolean;
  hasAnyRole: (roles: UserRole[]) => boolean;
}

export const AuthContext = createContext<AuthContextValue | null>(null);
