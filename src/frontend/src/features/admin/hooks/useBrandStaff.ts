import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { brandStaffApi } from '@api/brandStaff';
import type { InviteBrandStaffRequest } from '@api/brandStaff';

/** Centralized query key factory — keeps cache invalidation consistent. */
export const brandStaffKeys = {
  all: (brandSlug: string) => ['brand-staff', brandSlug] as const,
  byShop: (brandSlug: string, shopId: string) =>
    ['brand-staff', brandSlug, 'shop', shopId] as const,
};

// ---------------------------------------------------------------------------
// Queries
// ---------------------------------------------------------------------------

/** Fetch all staff for a brand. */
export function useBrandStaff(brandSlug: string) {
  return useQuery({
    queryKey: brandStaffKeys.all(brandSlug),
    queryFn: () => brandStaffApi.list(brandSlug),
    enabled: brandSlug.length > 0,
  });
}

/** Fetch staff for a specific shop within a brand. */
export function useShopStaff(brandSlug: string, shopId: string) {
  return useQuery({
    queryKey: brandStaffKeys.byShop(brandSlug, shopId),
    queryFn: () => brandStaffApi.listByShop(brandSlug, shopId),
    enabled: brandSlug.length > 0 && shopId.length > 0,
  });
}

// ---------------------------------------------------------------------------
// Mutations
// ---------------------------------------------------------------------------

/** Invite a new staff member to the brand. Invalidates the list on success. */
export function useInviteBrandStaff(brandSlug: string) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (data: InviteBrandStaffRequest) =>
      brandStaffApi.invite(brandSlug, data),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: brandStaffKeys.all(brandSlug) });
    },
  });
}

/** Deactivate (remove role assignment from) a brand staff member. Invalidates the list on success. */
export function useDeactivateBrandStaff(brandSlug: string) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (roleId: string) => brandStaffApi.deactivate(brandSlug, roleId),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: brandStaffKeys.all(brandSlug) });
    },
  });
}
