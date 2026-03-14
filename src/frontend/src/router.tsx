import { lazy } from 'react';
import { createBrowserRouter, Navigate } from 'react-router-dom';
import { AppShell } from '@components/AppShell';
import { AppVariantLayout } from '@components/AppVariantLayout';
import { SuspenseWrapper } from '@components/SuspenseWrapper';
import { adminRoutes } from '@features/admin/routes';

// ---------------------------------------------------------------------------
// Lazy-loaded feature pages — split into separate chunks by Vite/Rollup
// ---------------------------------------------------------------------------

const LazyStorefrontHome = lazy(() =>
  import('@features/storefront/pages/Home').then((m) => ({ default: m.Home })),
);

const LazyPosDashboard = lazy(() =>
  import('@features/pos/pages/Dashboard').then((m) => ({ default: m.PosDashboard })),
);

export const router = createBrowserRouter([
  {
    // Root redirect: send bare "/" to the default brand/locale
    path: '/',
    element: <Navigate to="/demo/nl" replace />,
  },
  {
    // Layout route shared by all three app variants
    // Matches: /:brandSlug/:lang  and all child paths
    path: '/:brandSlug/:lang',
    element: <AppShell />,
    children: [
      // ── Storefront ──────────────────────────────────────────────────────
      {
        element: <AppVariantLayout variant="storefront" />,
        children: [
          {
            index: true,
            element: (
              <SuspenseWrapper>
                <LazyStorefrontHome />
              </SuspenseWrapper>
            ),
          },
        ],
      },
      // ── POS ─────────────────────────────────────────────────────────────
      {
        path: 'pos',
        element: <AppVariantLayout variant="pos" />,
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
        element: <AppVariantLayout variant="admin" />,
        children: adminRoutes,
      },
    ],
  },
]);
