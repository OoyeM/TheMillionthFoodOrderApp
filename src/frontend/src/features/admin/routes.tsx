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

const LazyShopList = lazy(() =>
  import('./pages/ShopList').then((m) => ({ default: m.ShopList })),
);

const LazyShopCreate = lazy(() =>
  import('./pages/ShopCreate').then((m) => ({ default: m.ShopCreate })),
);

const LazyShopEdit = lazy(() =>
  import('./pages/ShopEdit').then((m) => ({ default: m.ShopEdit })),
);

const LazyProductList = lazy(() =>
  import('./pages/ProductList').then((m) => ({ default: m.ProductList })),
);

const LazyProductCreate = lazy(() =>
  import('./pages/ProductCreate').then((m) => ({ default: m.ProductCreate })),
);

const LazyProductEdit = lazy(() =>
  import('./pages/ProductEdit').then((m) => ({ default: m.ProductEdit })),
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
  // Shop management (brand-scoped)
  {
    path: 'shops',
    children: [
      {
        index: true,
        element: (
          <Suspense fallback={<p style={{ padding: '1.5rem', color: '#6b7280' }}>Loading…</p>}>
            <LazyShopList />
          </Suspense>
        ),
      },
      {
        path: 'new',
        element: (
          <Suspense fallback={<p style={{ padding: '1.5rem', color: '#6b7280' }}>Loading…</p>}>
            <LazyShopCreate />
          </Suspense>
        ),
      },
      {
        path: ':shopId',
        element: (
          <Suspense fallback={<p style={{ padding: '1.5rem', color: '#6b7280' }}>Loading…</p>}>
            <LazyShopEdit />
          </Suspense>
        ),
      },
    ],
  },
  // Product management (brand-scoped)
  {
    path: 'products',
    children: [
      {
        index: true,
        element: (
          <Suspense fallback={<p style={{ padding: '1.5rem', color: '#6b7280' }}>Loading…</p>}>
            <LazyProductList />
          </Suspense>
        ),
      },
      {
        path: 'new',
        element: (
          <Suspense fallback={<p style={{ padding: '1.5rem', color: '#6b7280' }}>Loading…</p>}>
            <LazyProductCreate />
          </Suspense>
        ),
      },
      {
        path: ':productId',
        element: (
          <Suspense fallback={<p style={{ padding: '1.5rem', color: '#6b7280' }}>Loading…</p>}>
            <LazyProductEdit />
          </Suspense>
        ),
      },
    ],
  },
];
