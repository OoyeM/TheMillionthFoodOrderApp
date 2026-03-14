import { useLocation } from 'react-router-dom';

/**
 * The three app variants served from this single codebase.
 */
export type AppVariant = 'storefront' | 'pos' | 'admin';

/**
 * Detects which app variant is active based on the current URL path segments.
 * - Path contains /pos   → 'pos'
 * - Path contains /admin → 'admin'
 * - Otherwise            → 'storefront'
 */
export function useAppVariant(): AppVariant {
  const { pathname } = useLocation();

  if (pathname.includes('/pos')) {
    return 'pos';
  }
  if (pathname.includes('/admin')) {
    return 'admin';
  }
  return 'storefront';
}
