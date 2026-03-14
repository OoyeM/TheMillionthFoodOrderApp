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
