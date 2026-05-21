import { lazy } from 'react';
import type { RouteObject } from 'react-router-dom';
import { SuspenseWrapper } from '@components/SuspenseWrapper';
import { PosDashboard } from './pages/Dashboard';

// Lazy-load heavier POS pages — split into separate chunks
const LazyKitchenDisplay = lazy(() =>
  import('./pages/KitchenDisplay').then((m) => ({ default: m.KitchenDisplay })),
);

const LazyNewOrderPage = lazy(() =>
  import('./pages/NewOrderPage').then((m) => ({ default: m.NewOrderPage })),
);

const LazyOrderPlacedPage = lazy(() =>
  import('./pages/OrderPlacedPage').then((m) => ({ default: m.OrderPlacedPage })),
);

/**
 * Route configuration for the in-store POS interface.
 * Nested under /:brandSlug/:lang/pos/ in the main router.
 *
 * Routes:
 *   index                                      → PosDashboard (shop picker)
 *   shops/:shopId/kitchen                      → KitchenDisplay (US-FP-027)
 *   shops/:shopId/order                        → NewOrderPage (US-FP-018)
 *   shops/:shopId/order/confirmation/:orderId  → OrderPlacedPage (US-FP-018)
 */
export const posRoutes: RouteObject[] = [
  {
    index: true,
    element: (
      <SuspenseWrapper>
        <PosDashboard />
      </SuspenseWrapper>
    ),
  },
  {
    path: 'shops/:shopId/kitchen',
    element: (
      <SuspenseWrapper>
        <LazyKitchenDisplay />
      </SuspenseWrapper>
    ),
  },
  {
    path: 'shops/:shopId/order',
    element: (
      <SuspenseWrapper>
        <LazyNewOrderPage />
      </SuspenseWrapper>
    ),
  },
  {
    path: 'shops/:shopId/order/confirmation/:orderId',
    element: (
      <SuspenseWrapper>
        <LazyOrderPlacedPage />
      </SuspenseWrapper>
    ),
  },
];
