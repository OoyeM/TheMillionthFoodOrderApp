// fallow-ignore-file unused-file
//
// POS route module. Wired up by US-FP-018 (POS ordering).

import type { RouteObject } from 'react-router-dom';
import { PosDashboard } from './pages/Dashboard';

/**
 * Route configuration for the in-store POS interface.
 * These routes are nested under /:brandSlug/:lang/pos/ in the main router.
 */
export const posRoutes: RouteObject[] = [
  {
    index: true,
    element: <PosDashboard />,
  },
];
