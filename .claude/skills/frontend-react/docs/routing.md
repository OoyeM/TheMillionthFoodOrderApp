# Routing — React Router

## Three Apps, One Router

The three app variants share a single React Router config with top-level route splitting:

```
/{brand-slug}/{lang}/              → Storefront routes
/{brand-slug}/{lang}/pos/          → POS routes
/{brand-slug}/{lang}/admin/        → Admin routes
```

## Route Structure

```tsx
// src/router.tsx
import { createBrowserRouter } from 'react-router-dom';

export const router = createBrowserRouter([
  {
    path: '/:brandSlug/:lang',
    element: <AppShell />,       // shared layout: brand context + i18n init
    children: [
      // Storefront
      {
        path: '',
        lazy: () => import('./features/storefront/routes'),
        children: [
          { index: true, lazy: () => import('./features/storefront/pages/Home') },
          { path: 'menu', lazy: () => import('./features/storefront/pages/Menu') },
          { path: 'menu/:categorySlug', lazy: () => import('./features/storefront/pages/Category') },
          { path: 'cart', lazy: () => import('./features/storefront/pages/Cart') },
          { path: 'checkout', lazy: () => import('./features/storefront/pages/Checkout') },
          { path: 'order/:orderId', lazy: () => import('./features/storefront/pages/OrderStatus') },
        ],
      },
      // POS
      {
        path: 'pos',
        lazy: () => import('./features/pos/routes'),
        children: [
          { index: true, lazy: () => import('./features/pos/pages/Dashboard') },
          { path: 'order', lazy: () => import('./features/pos/pages/NewOrder') },
          { path: 'orders', lazy: () => import('./features/pos/pages/OrderList') },
          { path: 'orders/:orderId', lazy: () => import('./features/pos/pages/OrderDetail') },
        ],
      },
      // Admin
      {
        path: 'admin',
        lazy: () => import('./features/admin/routes'),
        children: [
          { index: true, lazy: () => import('./features/admin/pages/Dashboard') },
          { path: 'products', lazy: () => import('./features/admin/pages/Products') },
          { path: 'products/:productId', lazy: () => import('./features/admin/pages/ProductEdit') },
          { path: 'orders', lazy: () => import('./features/admin/pages/Orders') },
          { path: 'settings', lazy: () => import('./features/admin/pages/Settings') },
        ],
      },
    ],
  },
]);
```

## AppShell — Shared Layout

The `AppShell` component handles cross-cutting concerns:

1. **Brand resolution** — reads `:brandSlug` from URL, fetches brand config (theme, logo, features)
2. **Language init** — sets i18n language from `:lang` param
3. **Auth context** — checks session via BFF
4. **Layout switching** — renders different shell (nav, header) based on active app variant

```tsx
function AppShell() {
  const { brandSlug, lang } = useParams();
  const brand = useBrandQuery(brandSlug!);
  const appVariant = useAppVariant(); // 'storefront' | 'pos' | 'admin'

  return (
    <BrandProvider brand={brand.data}>
      <AppVariantLayout variant={appVariant}>
        <Outlet />
      </AppVariantLayout>
    </BrandProvider>
  );
}
```

## App Variant Detection

```tsx
function useAppVariant(): 'storefront' | 'pos' | 'admin' {
  const location = useLocation();
  if (location.pathname.includes('/pos')) return 'pos';
  if (location.pathname.includes('/admin')) return 'admin';
  return 'storefront';
}
```

## Code Splitting

Each app variant is lazy-loaded — visiting the storefront doesn't download POS or admin code:

- `storefront/routes.tsx` → ~50KB (menu, cart, checkout)
- `pos/routes.tsx` → ~40KB (order taking, touch-optimized)
- `admin/routes.tsx` → ~80KB (CRUD, forms, data tables)

## Route Guards

```tsx
// Admin requires authentication
{ path: 'admin', element: <RequireAuth role="admin"><Outlet /></RequireAuth> }

// POS requires authentication + shop context
{ path: 'pos', element: <RequireAuth role="pos"><RequireShop><Outlet /></RequireShop></RequireAuth> }

// Storefront is public
```

## Navigation Helpers

```tsx
// Type-safe route building
export const routes = {
  storefront: {
    home: (brand: string, lang: string) => `/${brand}/${lang}`,
    menu: (brand: string, lang: string) => `/${brand}/${lang}/menu`,
    cart: (brand: string, lang: string) => `/${brand}/${lang}/cart`,
  },
  pos: {
    dashboard: (brand: string, lang: string) => `/${brand}/${lang}/pos`,
    newOrder: (brand: string, lang: string) => `/${brand}/${lang}/pos/order`,
  },
  admin: {
    dashboard: (brand: string, lang: string) => `/${brand}/${lang}/admin`,
    products: (brand: string, lang: string) => `/${brand}/${lang}/admin/products`,
  },
} as const;
```
