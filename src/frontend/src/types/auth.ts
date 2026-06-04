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
  /** Given name from the user's profile (US-FP-051). Null when not provided. */
  firstName: string | null;
  /** Family name from the user's profile (US-FP-051). Null when not provided. */
  lastName: string | null;
  /** Phone number from the user's profile (US-FP-051). Null when not provided. */
  phoneNumber: string | null;
}

/**
 * Top-level auth state held by the auth context / providers.
 *
 * @expected-unused — DTO/response shape used once auth context consumers are wired up
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
      /** Given name (US-FP-051). */
      firstName: string | null;
      /** Family name (US-FP-051). */
      lastName: string | null;
      /** Phone number (US-FP-051). */
      phoneNumber: string | null;
    };
