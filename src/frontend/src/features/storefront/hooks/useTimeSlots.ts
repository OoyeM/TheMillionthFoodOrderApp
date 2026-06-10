import { useQuery } from '@tanstack/react-query';
import { timeSlotsApi } from '@api/timeSlots';
import type { TimeSlotAvailabilityResponse } from '@api/timeSlots';

/**
 * Fetches time-slot availability for a shop with 60-second polling.
 *
 * - refetchInterval: 60_000 — same polling cadence as ShopStatusBadge
 * - staleTime: 30_000 — avoids unnecessary re-renders between refetches
 *
 * Called inside CheckoutForm (design decision 8 — single source of truth).
 */
export function useTimeSlots(brandSlug: string, shopId: string) {
  return useQuery<TimeSlotAvailabilityResponse>({
    queryKey: ['timeSlots', brandSlug, shopId],
    queryFn: () => timeSlotsApi.get(brandSlug, shopId),
    refetchInterval: 60_000,
    staleTime: 30_000,
    enabled: brandSlug.length > 0 && shopId.length > 0,
  });
}
