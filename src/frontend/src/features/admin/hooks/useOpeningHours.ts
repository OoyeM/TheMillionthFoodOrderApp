import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { openingHoursApi } from '@api/openingHours';
import type { SetOpeningHoursRequest } from '../../../types/common';

/**
 * Centralized query key factory — scoped by brandSlug + shopId for proper cache isolation.
 *
 * @expected-unused — US-FP-009 (Opening hours) — used by mutations for cache invalidation
 */
export const openingHoursKeys = {
  all: (brandSlug: string, shopId: string) =>
    ['openingHours', brandSlug, shopId] as const,
  status: (brandSlug: string, shopId: string) =>
    ['shopStatus', brandSlug, shopId] as const,
};

// ---------------------------------------------------------------------------
// Queries
// ---------------------------------------------------------------------------

/** Fetch the full weekly opening hours schedule for a shop. */
export function useOpeningHours(brandSlug: string, shopId: string) {
  return useQuery({
    queryKey: openingHoursKeys.all(brandSlug, shopId),
    queryFn: () => openingHoursApi.get(brandSlug, shopId),
    enabled: brandSlug.length > 0 && shopId.length > 0,
  });
}

/**
 * Fetch real-time open/closed status for a shop.
 *
 * @expected-unused — US-FP-024 (Shop status badge) — wired up when storefront ships
 */
export function useShopStatus(brandSlug: string, shopId: string) {
  return useQuery({
    queryKey: openingHoursKeys.status(brandSlug, shopId),
    queryFn: () => openingHoursApi.getStatus(brandSlug, shopId),
    enabled: brandSlug.length > 0 && shopId.length > 0,
  });
}

// ---------------------------------------------------------------------------
// Mutations
// ---------------------------------------------------------------------------

/** Replace the full weekly schedule for a shop. Invalidates the schedule cache on success. */
export function useSetOpeningHours(brandSlug: string, shopId: string) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (data: SetOpeningHoursRequest) =>
      openingHoursApi.set(brandSlug, shopId, data),
    onSuccess: () => {
      void queryClient.invalidateQueries({
        queryKey: openingHoursKeys.all(brandSlug, shopId),
      });
      // Also invalidate status since the schedule change may affect open/closed state
      void queryClient.invalidateQueries({
        queryKey: openingHoursKeys.status(brandSlug, shopId),
      });
    },
  });
}
