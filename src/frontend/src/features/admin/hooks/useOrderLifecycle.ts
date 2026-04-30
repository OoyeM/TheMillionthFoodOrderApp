import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { orderLifecycleApi } from '@api/orderLifecycle';
import type { ConfigureOrderLifecycleRequest } from '../../../types/common';

/**
 * Centralized query key factory — scoped by brandSlug + shopId for proper cache isolation.
 *
 * @expected-unused — US-FP-024 (Order lifecycle) — used by mutations for cache invalidation
 */
export const orderLifecycleKeys = {
  all: (brandSlug: string, shopId: string) =>
    ['orderLifecycle', brandSlug, shopId] as const,
};

// ---------------------------------------------------------------------------
// Queries
// ---------------------------------------------------------------------------

/** Fetch the order lifecycle configuration for a shop. */
export function useOrderLifecycle(brandSlug: string, shopId: string) {
  return useQuery({
    queryKey: orderLifecycleKeys.all(brandSlug, shopId),
    queryFn: () => orderLifecycleApi.get(brandSlug, shopId),
    enabled: brandSlug.length > 0 && shopId.length > 0,
  });
}

// ---------------------------------------------------------------------------
// Mutations
// ---------------------------------------------------------------------------

/** Replace the full lifecycle configuration for a shop. */
export function useConfigureOrderLifecycle(brandSlug: string, shopId: string) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (data: ConfigureOrderLifecycleRequest) =>
      orderLifecycleApi.configure(brandSlug, shopId, data),
    onSuccess: () => {
      void queryClient.invalidateQueries({
        queryKey: orderLifecycleKeys.all(brandSlug, shopId),
      });
    },
  });
}

/** Reset the lifecycle configuration to defaults. */
export function useResetOrderLifecycle(brandSlug: string, shopId: string) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: () => orderLifecycleApi.reset(brandSlug, shopId),
    onSuccess: () => {
      void queryClient.invalidateQueries({
        queryKey: orderLifecycleKeys.all(brandSlug, shopId),
      });
    },
  });
}
