import { createContext } from 'react';
import type { EatInSettings } from '../../../types/common';

// ---------------------------------------------------------------------------
// ResolvedShop type and context
// ---------------------------------------------------------------------------

/**
 * The resolved shop available to all children of a :shopSlug route.
 * Provides the stable id needed for API calls and the cart.
 */
export interface ResolvedShop {
  id: string;
  name: string;
  slug: string;
  isOpen: boolean;
  /** Eat-in ordering configuration — gates the eat-in option at checkout (US-FP-066). */
  eatIn: EatInSettings;
}

/**
 * Context value set by ShopResolver and consumed by useResolvedShop.
 * Separated from the layout component to keep react-refresh happy.
 */
export const ShopContext = createContext<ResolvedShop | null>(null);
