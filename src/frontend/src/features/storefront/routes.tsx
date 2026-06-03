// Storefront route module. Updated by US-FP-071 to add slug-based shop routing.

import { lazy } from 'react';
import { Navigate } from 'react-router-dom';
import type { RouteObject } from 'react-router-dom';
import { SuspenseWrapper } from '@components/SuspenseWrapper';
import { ShopChooserPage } from './pages/ShopChooserPage';
import { LegacyShopIdRedirect } from './pages/LegacyShopIdRedirect';
import { ShopResolver } from './context/ShopContext';

// Lazy-load heavy storefront pages for code-splitting
const LazyMenuPage = lazy(() =>
  import('./pages/MenuPage').then((m) => ({ default: m.MenuPage })),
);

const LazyCheckoutPage = lazy(() =>
  import('./pages/CheckoutPage').then((m) => ({ default: m.CheckoutPage })),
);

const LazyOrderConfirmationPage = lazy(() =>
  import('./pages/OrderConfirmationPage').then((m) => ({ default: m.OrderConfirmationPage })),
);

const LazyOrderTrackingPage = lazy(() =>
  import('./pages/OrderTrackingPage').then((m) => ({ default: m.OrderTrackingPage })),
);

/**
 * Route configuration for the customer-facing storefront.
 * These routes are nested under /:brandSlug/:lang in the main router,
 * inside the ThemeProvider layout route.
 *
 * ThemeProvider is applied as a layout route in router.tsx — do NOT wrap again here.
 *
 * Route tree:
 *   index                         → redirect to shops
 *   shops                         → ShopChooserPage (lists active shops)
 *   shops/:shopId/menu            → LegacyShopIdRedirect (back-compat GUID route)
 *   :shopSlug                     → ShopResolver (layout route, resolves slug → shop)
 *     :shopSlug/menu              → MenuPage
 *     :shopSlug/checkout          → CheckoutPage
 *     :shopSlug/order/:orderId    → OrderConfirmationPage
 *     :shopSlug/order/:orderId/track → OrderTrackingPage
 */
export const storefrontRoutes: RouteObject[] = [
  {
    // Index: redirect to the shop chooser.
    // ShopChooserPage will auto-redirect to the menu if only one shop exists.
    index: true,
    element: <Navigate to="shops" replace />,
  },
  {
    // Shop chooser: display all active shops as selectable cards.
    path: 'shops',
    element: <ShopChooserPage />,
  },
  {
    // Back-compat: old GUID-based route used before US-FP-071.
    // Looks up the shop slug by id and redirects to the slug route.
    path: 'shops/:shopId/menu',
    element: <LegacyShopIdRedirect />,
  },
  {
    // ShopResolver layout route: resolves :shopSlug param to a shop object
    // and makes it available to all children via ShopContext.
    path: ':shopSlug',
    element: <ShopResolver />,
    children: [
      {
        // Menu page: browse categories and products, add to cart
        path: 'menu',
        element: (
          <SuspenseWrapper>
            <LazyMenuPage />
          </SuspenseWrapper>
        ),
      },
      {
        // Checkout: review cart, select order type, submit
        path: 'checkout',
        element: (
          <SuspenseWrapper>
            <LazyCheckoutPage />
          </SuspenseWrapper>
        ),
      },
      {
        // Order confirmation: show order details and real-time status via SignalR
        path: 'order/:orderId',
        element: (
          <SuspenseWrapper>
            <LazyOrderConfirmationPage />
          </SuspenseWrapper>
        ),
      },
      {
        // Order tracking: visual lifecycle stepper with real-time SignalR updates
        path: 'order/:orderId/track',
        element: (
          <SuspenseWrapper>
            <LazyOrderTrackingPage />
          </SuspenseWrapper>
        ),
      },
    ],
  },
];
