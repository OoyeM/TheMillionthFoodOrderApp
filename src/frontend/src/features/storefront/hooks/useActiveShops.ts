import { useQuery } from '@tanstack/react-query';
import { shopsApi } from '@api/shops';
import type { StorefrontShop } from '@api/shops';

/**
 * Fetches the list of active shops for a brand.
 * Used by the storefront shop chooser page (US-FP-071).
 *
 * Cache key: ['activeShops', brandSlug]
 * Stale after 60 s — isOpen is real-time but re-fetching every minute is enough
 * for the chooser; MenuPage's ShopStatusBadge handles per-shop live refresh.
 */
export function useActiveShops(brandSlug: string) {
  return useQuery<StorefrontShop[]>({
    queryKey: ['activeShops', brandSlug],
    queryFn: () => shopsApi.listActive(brandSlug),
    enabled: brandSlug.length > 0,
    staleTime: 60_000,
  });
}
