/**
 * All roles a user can hold in the platform.
 */
export type UserRole =
  | 'platform-admin'
  | 'brand-admin'
  | 'shop-manager'
  | 'counter-staff'
  | 'kitchen-staff'
  | 'floor-staff'
  | 'customer';

/**
 * Authenticated user as known by the frontend after the BFF /bff/user response
 * has been mapped to a domain entity.
 */
export interface AuthUser {
  userId: string;
  displayName: string;
  email: string;
  roles: UserRole[];
  brandSlug: string | null;
}

/**
 * Top-level auth state held by the auth context / providers.
 */
export interface AuthState {
  isAuthenticated: boolean;
  user: AuthUser | null;
  isLoading: boolean;
}

/**
 * Raw shape of the JSON returned by GET /bff/user.
 * Matches the C# anonymous object returned by HandleUser in BffEndpoints.cs.
 * When the user is not authenticated the server returns { isAuthenticated: false }.
 */
export type BffUserResponse =
  | { isAuthenticated: false }
  | {
      isAuthenticated: true;
      userId: string;
      displayName: string;
      email: string;
      roles: string[];
      brandSlug: string | null;
    };
