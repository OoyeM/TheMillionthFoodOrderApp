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

const LazyMenuCategoryList = lazy(() =>
  import('./pages/MenuCategoryList').then((m) => ({ default: m.MenuCategoryList })),
);

const LazyMenuCategoryCreate = lazy(() =>
  import('./pages/MenuCategoryCreate').then((m) => ({ default: m.MenuCategoryCreate })),
);

const LazyMenuCategoryEdit = lazy(() =>
  import('./pages/MenuCategoryEdit').then((m) => ({ default: m.MenuCategoryEdit })),
);

const LazyShopOpeningHours = lazy(() =>
  import('./pages/ShopOpeningHours').then((m) => ({ default: m.ShopOpeningHours })),
);

const LazyShopOrderLifecycle = lazy(() =>
  import('./pages/ShopOrderLifecycle').then((m) => ({ default: m.ShopOrderLifecycle })),
);

const LazyPlatformAdminList = lazy(() =>
  import('./pages/PlatformAdminList').then((m) => ({ default: m.PlatformAdminList })),
);

const LazyStaffList = lazy(() =>
  import('./pages/StaffList').then((m) => ({ default: m.StaffList })),
);

const LazyBrandTheming = lazy(() =>
  import('./pages/BrandTheming').then((m) => ({ default: m.BrandTheming })),
);

const LazyModifierGroupList = lazy(() =>
  import('./pages/ModifierGroupList').then((m) => ({ default: m.ModifierGroupList })),
);

const LazyModifierGroupCreate = lazy(() =>
  import('./pages/ModifierGroupCreate').then((m) => ({ default: m.ModifierGroupCreate })),
);

const LazyModifierGroupEdit = lazy(() =>
  import('./pages/ModifierGroupEdit').then((m) => ({ default: m.ModifierGroupEdit })),
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
        children: [
          {
            index: true,
            element: (
              <Suspense fallback={<p style={{ padding: '1.5rem', color: '#6b7280' }}>Loading…</p>}>
                <LazyShopEdit />
              </Suspense>
            ),
          },
          {
            path: 'opening-hours',
            element: (
              <Suspense fallback={<p style={{ padding: '1.5rem', color: '#6b7280' }}>Loading…</p>}>
                <LazyShopOpeningHours />
              </Suspense>
            ),
          },
          {
            path: 'order-lifecycle',
            element: (
              <Suspense fallback={<p style={{ padding: '1.5rem', color: '#6b7280' }}>Loading…</p>}>
                <LazyShopOrderLifecycle />
              </Suspense>
            ),
          },
        ],
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
  // Menu category management (brand-scoped)
  {
    path: 'menu-categories',
    children: [
      {
        index: true,
        element: (
          <Suspense fallback={<p style={{ padding: '1.5rem', color: '#6b7280' }}>Loading…</p>}>
            <LazyMenuCategoryList />
          </Suspense>
        ),
      },
      {
        path: 'new',
        element: (
          <Suspense fallback={<p style={{ padding: '1.5rem', color: '#6b7280' }}>Loading…</p>}>
            <LazyMenuCategoryCreate />
          </Suspense>
        ),
      },
      {
        path: ':categoryId',
        element: (
          <Suspense fallback={<p style={{ padding: '1.5rem', color: '#6b7280' }}>Loading…</p>}>
            <LazyMenuCategoryEdit />
          </Suspense>
        ),
      },
    ],
  },
  // Modifier group management (brand-scoped)
  {
    path: 'modifier-groups',
    children: [
      {
        index: true,
        element: (
          <Suspense fallback={<p style={{ padding: '1.5rem', color: '#6b7280' }}>Loading…</p>}>
            <LazyModifierGroupList />
          </Suspense>
        ),
      },
      {
        path: 'new',
        element: (
          <Suspense fallback={<p style={{ padding: '1.5rem', color: '#6b7280' }}>Loading…</p>}>
            <LazyModifierGroupCreate />
          </Suspense>
        ),
      },
      {
        path: ':modifierGroupId',
        element: (
          <Suspense fallback={<p style={{ padding: '1.5rem', color: '#6b7280' }}>Loading…</p>}>
            <LazyModifierGroupEdit />
          </Suspense>
        ),
      },
    ],
  },
  // Platform admin management
  {
    path: 'platform-admins',
    element: (
      <Suspense fallback={<p style={{ padding: '1.5rem', color: '#6b7280' }}>Loading…</p>}>
        <LazyPlatformAdminList />
      </Suspense>
    ),
  },
  // Brand staff management
  {
    path: 'staff',
    element: (
      <Suspense fallback={<p style={{ padding: '1.5rem', color: '#6b7280' }}>Loading…</p>}>
        <LazyStaffList />
      </Suspense>
    ),
  },
  // Brand theming
  {
    path: 'theming',
    element: (
      <Suspense fallback={<p style={{ padding: '1.5rem', color: '#6b7280' }}>Loading…</p>}>
        <LazyBrandTheming />
      </Suspense>
    ),
  },
];
