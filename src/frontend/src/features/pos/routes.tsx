// POS route module. Wired up by US-FP-018 (POS ordering).

import type { RouteObject } from 'react-router-dom';
import { PosDashboard } from './pages/Dashboard';
import { PosOrderConfirmation } from './pages/PosOrderConfirmation';

/**
 * Route configuration for the in-store POS interface.
 * These routes are nested under /:brandSlug/:lang/pos/ in the main router.
 *
 * Auth guard (RequireAuth with staff roles) is applied in router.tsx — not duplicated here.
 */
export const posRoutes: RouteObject[] = [
  {
    index: true,
    element: <PosDashboard />,
  },
  {
    path: 'confirmation/:orderNumber',
    element: <PosOrderConfirmation />,
  },
];
