import axios from 'axios';
import type { BffUserResponse, AuthUser, UserRole } from '@/types/auth';

/**
 * Separate axios instance for BFF session/auth endpoints.
 * - baseURL: '/bff' — Vite proxies this to the BFF host (http://localhost:5261)
 * - withCredentials: true — sends session cookie
 * - No X-Brand-Slug interceptor: BFF auth is brand-agnostic at this layer
 *
 * @expected-unused — Lower-level BFF client — public API surface for future consumers
 */
export const bffClient = axios.create({
  baseURL: '/bff',
  withCredentials: true,
  headers: {
    'Content-Type': 'application/json',
  },
});

// ---------------------------------------------------------------------------
// Mappers
// ---------------------------------------------------------------------------

/**
 * Maps the raw BFF /bff/user response to an AuthUser domain entity.
 * Filters out any unknown role strings so the rest of the app only sees
 * strongly-typed UserRole values.
 */
function mapBffUserToAuthUser(response: BffUserResponse): AuthUser | null {
  if (!response.isAuthenticated) return null;

  const knownRoles: UserRole[] = [
    'platform-admin',
    'brand-admin',
    'shop-manager',
    'counter-staff',
    'kitchen-staff',
    'floor-staff',
    'customer',
  ];

  const roles = response.roles.filter((r): r is UserRole => knownRoles.includes(r as UserRole));

  return {
    userId: response.userId,
    displayName: response.displayName,
    email: response.email,
    roles,
    brandSlug: response.brandSlug,
  };
}

// ---------------------------------------------------------------------------
// API functions
// ---------------------------------------------------------------------------

/**
 * Fetches the current authenticated user from the BFF.
 * Returns null when the user is not authenticated (server returns { isAuthenticated: false }).
 * Never throws on 401 — the BFF always returns 200 for this endpoint.
 */
export async function getUser(): Promise<AuthUser | null> {
  const { data } = await bffClient.get<BffUserResponse>('/user');
  return mapBffUserToAuthUser(data);
}

/**
 * Redirects the browser to the BFF login flow.
 * In development with mock auth enabled this triggers the mock persona sign-in.
 *
 * @param persona - Mock persona to sign in as (only used in dev mock mode)
 * @param returnUrl - URL to redirect to after successful login
 */
export function login(persona?: string, returnUrl?: string): void {
  const params = new URLSearchParams();
  if (persona) params.set('mock', persona);
  if (returnUrl) params.set('returnUrl', returnUrl);

  const qs = params.toString();
  window.location.href = `/bff/login${qs ? `?${qs}` : ''}`;
}

/**
 * Signs the current user out by posting to the BFF logout endpoint,
 * then reloads the page so all in-memory state is cleared.
 */
export async function logout(): Promise<void> {
  await bffClient.post('/logout');
  window.location.reload();
}

/**
 * Extends the session window. Call this periodically while the user is active.
 * Returns false if the session has expired (401).
 */
export async function keepalive(): Promise<boolean> {
  try {
    await bffClient.post('/session/keepalive');
    return true;
  } catch {
    return false;
  }
}
