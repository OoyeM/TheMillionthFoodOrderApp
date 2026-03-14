import type { RouteObject } from 'react-router-dom';
import { AdminDashboard } from './pages/Dashboard';

/**
 * Route configuration for the CMS admin panel.
 * These routes are nested under /:brandSlug/:lang/admin/ in the main router.
 */
export const adminRoutes: RouteObject[] = [
  {
    index: true,
    element: <AdminDashboard />,
  },
];
