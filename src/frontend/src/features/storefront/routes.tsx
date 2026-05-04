// fallow-ignore-file unused-file
//
// Storefront route module. Wired up by US-FP-016 + US-FP-017 (online + guest ordering).

import type { RouteObject } from 'react-router-dom';
import { Home } from './pages/Home';

/**
 * Route configuration for the customer-facing storefront.
 * These routes are nested under /:brandSlug/:lang in the main router.
 *
 * ThemeProvider is applied as a layout route in router.tsx — do NOT wrap again here.
 */
export const storefrontRoutes: RouteObject[] = [
  {
    index: true,
    element: <Home />,
  },
];
