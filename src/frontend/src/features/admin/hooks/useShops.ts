import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { shopsApi } from '@api/shops';
import type { CreateShopRequest, UpdateShopRequest } from '@api/shops';

/**
 * Centralized query key factory — scoped by brandSlug for proper cache isolation.
 *
 * @expected-unused — US-FP-007 (Shop CRUD) — used by mutations for cache invalidation
 */
export const shopKeys = {
  all: (brandSlug: string) => ['shops', brandSlug] as const,
  detail: (brandSlug: string, id: string) => ['shops', brandSlug, id] as const,
};

// ---------------------------------------------------------------------------
// Queries
// ---------------------------------------------------------------------------

/** Fetch all shops for a brand. */
export function useShops(brandSlug: string) {
  return useQuery({
    queryKey: shopKeys.all(brandSlug),
    queryFn: () => shopsApi.list(brandSlug),
    enabled: brandSlug.length > 0,
  });
}

/** Fetch a single shop by id. */
export function useShop(brandSlug: string, id: string) {
  return useQuery({
    queryKey: shopKeys.detail(brandSlug, id),
    queryFn: () => shopsApi.get(brandSlug, id),
    enabled: brandSlug.length > 0 && id.length > 0,
  });
}

// ---------------------------------------------------------------------------
// Mutations
// ---------------------------------------------------------------------------

/** Create a new shop under a brand. Invalidates the list on success. */
export function useCreateShop(brandSlug: string) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (data: CreateShopRequest) => shopsApi.create(brandSlug, data),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: shopKeys.all(brandSlug) });
    },
  });
}

/** Update shop metadata. Invalidates list and detail on success. */
export function useUpdateShop(brandSlug: string, id: string) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (data: UpdateShopRequest) => shopsApi.update(brandSlug, id, data),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: shopKeys.all(brandSlug) });
      void queryClient.invalidateQueries({ queryKey: shopKeys.detail(brandSlug, id) });
    },
  });
}

/** Deactivate a shop. Invalidates list and detail on success. */
export function useDeactivateShop(brandSlug: string, id: string) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: () => shopsApi.deactivate(brandSlug, id),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: shopKeys.all(brandSlug) });
      void queryClient.invalidateQueries({ queryKey: shopKeys.detail(brandSlug, id) });
    },
  });
}

/** Activate a shop. Invalidates list and detail on success. */
export function useActivateShop(brandSlug: string, id: string) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: () => shopsApi.activate(brandSlug, id),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: shopKeys.all(brandSlug) });
      void queryClient.invalidateQueries({ queryKey: shopKeys.detail(brandSlug, id) });
    },
  });
}
