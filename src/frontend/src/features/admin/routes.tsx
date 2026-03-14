import { lazy, Suspense } from 'react';
import { Navigate } from 'react-router-dom';
import type { RouteObject } from 'react-router-dom';

// ---------------------------------------------------------------------------
// Lazy-loaded admin pages
// ---------------------------------------------------------------------------

const LazyBrandList = lazy(() =>
  import('./pages/BrandList').then((m) => ({ default: m.BrandList })),
);

const LazyBrandCreate = lazy(() =>
  import('./pages/BrandCreate').then((m) => ({ default: m.BrandCreate })),
);

const LazyBrandEdit = lazy(() =>
  import('./pages/BrandEdit').then((m) => ({ default: m.BrandEdit })),
);

/**
 * Route configuration for the CMS admin panel.
 * These routes are nested under /:brandSlug/:lang/admin/ in the main router.
 */
export const adminRoutes: RouteObject[] = [
  // Default: redirect /admin → /admin/brands
  {
    index: true,
    element: <Navigate to="brands" replace />,
  },
  // Brand management
  {
    path: 'brands',
    children: [
      {
        index: true,
        element: (
          <Suspense fallback={<p style={{ padding: '1.5rem', color: '#6b7280' }}>Loading…</p>}>
            <LazyBrandList />
          </Suspense>
        ),
      },
      {
        path: 'new',
        element: (
          <Suspense fallback={<p style={{ padding: '1.5rem', color: '#6b7280' }}>Loading…</p>}>
            <LazyBrandCreate />
          </Suspense>
        ),
      },
      {
        path: ':brandId',
        element: (
          <Suspense fallback={<p style={{ padding: '1.5rem', color: '#6b7280' }}>Loading…</p>}>
            <LazyBrandEdit />
          </Suspense>
        ),
      },
    ],
  },
];
