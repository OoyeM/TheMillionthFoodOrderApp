import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { shopsApi } from '@api/shops';

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

// ---------------------------------------------------------------------------
// Mutations
// ---------------------------------------------------------------------------

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
