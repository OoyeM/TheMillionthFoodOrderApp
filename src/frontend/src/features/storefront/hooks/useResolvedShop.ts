import { useContext } from 'react';
import { ShopContext } from '../context/shopContextValue';
import type { ResolvedShop } from '../context/shopContextValue';

// Re-export the type for consumers that only need it
export type { ResolvedShop };

/**
 * Returns the resolved shop from the nearest ShopResolver ancestor.
 * Throws if called outside a ShopResolver — callers within :shopSlug routes
 * are always inside one.
 */
export function useResolvedShop(): ResolvedShop {
  const ctx = useContext(ShopContext);
  if (ctx === null) {
    throw new Error('useResolvedShop must be used within a ShopResolver route.');
  }
  return ctx;
}
