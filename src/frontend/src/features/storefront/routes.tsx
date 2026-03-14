import type { RouteObject } from 'react-router-dom';
import { Home } from './pages/Home';

/**
 * Route configuration for the customer-facing storefront.
 * These routes are nested under /:brandSlug/:lang in the main router.
 */
export const storefrontRoutes: RouteObject[] = [
  {
    index: true,
    element: <Home />,
  },
];
