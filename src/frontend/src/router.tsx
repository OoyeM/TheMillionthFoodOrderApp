import { lazy } from 'react';
import { createBrowserRouter, Navigate } from 'react-router-dom';
import { extractPrimaryLocale } from '@/types/common';
import { LANGUAGE_PREF_KEY } from '@features/storefront/components/LanguageSelector';
import { AppShell } from '@components/AppShell';
import { AppVariantLayout } from '@components/AppVariantLayout';
import { SuspenseWrapper } from '@components/SuspenseWrapper';
import { RequireAuth } from '@/auth/RequireAuth';
import { adminRoutes } from '@features/admin/routes';
import { storefrontRoutes } from '@features/storefront/routes';
import { ThemeProvider } from '@features/storefront/components/ThemeProvider';

// ---------------------------------------------------------------------------
// Lazy-loaded feature pages — split into separate chunks by Vite/Rollup
// ---------------------------------------------------------------------------

const LazyPosDashboard = lazy(() =>
  import('@features/pos/pages/Dashboard').then((m) => ({ default: m.PosDashboard })),
);

/**
 * Resolves the initial locale for the root redirect.
 * Priority: explicit user preference (localStorage) → browser language → 'nl' (fallback).
 */
function resolveInitialLocale(): string {
  const saved = localStorage.getItem(LANGUAGE_PREF_KEY);
  if (saved) return saved;
  return extractPrimaryLocale(navigator.language);
}

/**
 * Root redirect component — re-evaluates on every render so that changes to
 * the stored language preference are picked up when the user returns to "/".
 */
function RootRedirect() {
  return <Navigate to={`/demo/${resolveInitialLocale()}`} replace />;
}

export const router = createBrowserRouter([
  {
    // Root redirect: send bare "/" to the default brand/locale.
    // Uses the saved language preference or browser locale; falls back to Dutch.
    path: '/',
    element: <RootRedirect />,
  },
  {
    // Layout route shared by all three app variants
    // Matches: /:brandSlug/:lang  and all child paths
    path: '/:brandSlug/:lang',
    element: <AppShell />,
    children: [
      // ── Storefront (public — no auth required) ───────────────────────────
      // ThemeProvider fetches the brand theme and injects CSS custom properties.
      {
        element: <AppVariantLayout variant="storefront" />,
        children: [
          {
            // ThemeProvider as layout: fetches brand theme, applies CSS custom properties,
            // then renders child routes via <Outlet />.
            element: <ThemeProvider />,
            children: storefrontRoutes,
          },
        ],
      },
      // ── POS ─────────────────────────────────────────────────────────────
      {
        path: 'pos',
        element: (
          <RequireAuth roles={['counter-staff', 'floor-staff', 'kitchen-staff', 'shop-manager']}>
            <AppVariantLayout variant="pos" />
          </RequireAuth>
        ),
        children: [
          {
            index: true,
            element: (
              <SuspenseWrapper>
                <LazyPosDashboard />
              </SuspenseWrapper>
            ),
          },
        ],
      },
      // ── Admin ────────────────────────────────────────────────────────────
      {
        path: 'admin',
        element: (
          <RequireAuth roles={['brand-admin', 'platform-admin']}>
            <AppVariantLayout variant="admin" />
          </RequireAuth>
        ),
        children: adminRoutes,
      },
    ],
  },
]);
