import { useQuery } from '@tanstack/react-query';
import { ordersApi } from '../../../api/orders';
import type { AvailableTimeSlotsResponse } from '../../../api/orders';

/** Query-key factory — also used by CheckoutPage to invalidate after a 409. */
export const timeSlotKeys = {
  list: (brandSlug: string, shopId: string) => ['timeSlots', brandSlug, shopId] as const,
};

/**
 * Fetches available time slots for a shop for the remainder of today (US-FP-019).
 * Refreshes every 30 seconds so the picker stays in sync with orders being placed
 * concurrently by other customers.
 *
 * @param brandSlug - The brand slug from the URL.
 * @param shopId    - The shop id resolved by ShopResolver.
 * @param enabled   - Pass `false` to skip the query when time-slot ordering is disabled.
 */
export function useAvailableTimeSlots(
  brandSlug: string,
  shopId: string,
  enabled = true,
) {
  return useQuery<AvailableTimeSlotsResponse>({
    queryKey: timeSlotKeys.list(brandSlug, shopId),
    queryFn: () => ordersApi.getTimeSlots(brandSlug, shopId),
    staleTime: 15_000,
    refetchInterval: 30_000,
    enabled,
  });
}
