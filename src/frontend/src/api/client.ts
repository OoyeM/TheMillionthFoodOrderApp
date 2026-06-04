import axios from 'axios';

/**
 * Module-level store for the active brand slug.
 * This is set by AppShell when the brand slug is read from the URL.
 * It is deliberately simple (not Pinia/Zustand) because the slug is
 * authoritative in the URL; no persistent client state is needed.
 */
let activeBrandSlug: string | null = null;

export function setActiveBrandSlug(slug: string): void {
  activeBrandSlug = slug;
}

export function getActiveBrandSlug(): string | null {
  return activeBrandSlug;
}

/**
 * Shared axios instance for all API calls.
 *
 * - baseURL: '/api' — proxied to the .NET BFF by Vite in dev,
 *   and by the reverse proxy (nginx / Azure Front Door) in production.
 * - withCredentials: true — sends cookies for authentication.
 * - X-CSRF: 1 — required by the BFF on every state-changing call.
 *   Browsers cannot send custom headers cross-origin without a CORS
 *   preflight, so its presence proves same-origin intent.
 * - X-Brand-Slug header: legacy hint for non-route paths. The BFF strips
 *   client-supplied values and re-derives the slug from the user's claims.
 */
export const apiClient = axios.create({
  baseURL: '/api',
  withCredentials: true,
  headers: {
    'Content-Type': 'application/json',
    'X-CSRF': '1',
  },
});

// Request interceptor: attach the active brand slug as a header
apiClient.interceptors.request.use((config) => {
  const slug = getActiveBrandSlug();
  if (slug) {
    config.headers['X-Brand-Slug'] = slug;
  }
  return config;
});

// Response interceptor: translate HTTP auth errors into window events.
// Components and providers listen for these to update auth state without
// having to inspect every API response individually.
apiClient.interceptors.response.use(
  (response) => response,
  (error: unknown) => {
    if (axios.isAxiosError(error)) {
      if (error.response?.status === 401) {
        window.dispatchEvent(new CustomEvent('auth:session-expired'));
      } else if (error.response?.status === 403) {
        window.dispatchEvent(new CustomEvent('auth:access-denied'));
      }
    }
    // eslint-disable-next-line @typescript-eslint/prefer-promise-reject-errors -- must re-reject the original axios error unchanged so downstream axios.isAxiosError checks and callers see the real rejection value
    return Promise.reject(error);
  },
);
