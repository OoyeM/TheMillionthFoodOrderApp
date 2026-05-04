// Storefront route module. Wired up by US-FP-016 + US-FP-017 (online + guest ordering).

import { lazy } from 'react';
import type { RouteObject } from 'react-router-dom';
import { SuspenseWrapper } from '@components/SuspenseWrapper';
import { Home } from './pages/Home';

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

/**
 * Route configuration for the customer-facing storefront.
 * These routes are nested under /:brandSlug/:lang in the main router,
 * inside the ThemeProvider layout route.
 *
 * ThemeProvider is applied as a layout route in router.tsx — do NOT wrap again here.
 */
export const storefrontRoutes: RouteObject[] = [
  {
    index: true,
    element: <Home />,
  },
  {
    // Menu page for a specific shop: browse categories and products, add to cart
    path: 'shops/:shopId/menu',
    element: (
      <SuspenseWrapper>
        <LazyMenuPage />
      </SuspenseWrapper>
    ),
  },
  {
    // Checkout page: review cart, select order type, submit order
    path: 'checkout',
    element: (
      <SuspenseWrapper>
        <LazyCheckoutPage />
      </SuspenseWrapper>
    ),
  },
  {
    // Order confirmation page: show order details and real-time status via SignalR
    path: 'order/:orderId',
    element: (
      <SuspenseWrapper>
        <LazyOrderConfirmationPage />
      </SuspenseWrapper>
    ),
  },
];
