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
 * - X-Brand-Slug header: injected automatically on every request
 *   so the BFF can resolve the correct tenant database.
 */
export const apiClient = axios.create({
  baseURL: '/api',
  withCredentials: true,
  headers: {
    'Content-Type': 'application/json',
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
    return Promise.reject(error);
  },
);
