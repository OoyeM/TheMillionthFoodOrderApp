import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { brandsApi } from '@api/brands';
import type { CreateBrandRequest, UpdateBrandRequest } from '@api/brands';
import type { StaffAuthMethod } from '../../../types/common';

/**
 * Centralized query key factory — keeps cache invalidation consistent.
 *
 * @expected-unused — US-FP-002 (Brand CRUD) — used by mutations for cache invalidation
 */
export const brandKeys = {
  all: ['brands'] as const,
  detail: (id: string) => ['brands', id] as const,
};

// ---------------------------------------------------------------------------
// Queries
// ---------------------------------------------------------------------------

/** Fetch all brands. */
export function useBrands() {
  return useQuery({
    queryKey: brandKeys.all,
    queryFn: () => brandsApi.list(),
  });
}

/** Fetch a single brand by id. */
export function useBrand(id: string) {
  return useQuery({
    queryKey: brandKeys.detail(id),
    queryFn: () => brandsApi.get(id),
    enabled: id.length > 0,
  });
}

// ---------------------------------------------------------------------------
// Mutations
// ---------------------------------------------------------------------------

/** Create a new brand. Invalidates the list on success. */
export function useCreateBrand() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (data: CreateBrandRequest) => brandsApi.create(data),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: brandKeys.all });
    },
  });
}

/** Update brand metadata. Invalidates list and detail on success. */
export function useUpdateBrand(id: string) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (data: UpdateBrandRequest) => brandsApi.update(id, data),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: brandKeys.all });
      void queryClient.invalidateQueries({ queryKey: brandKeys.detail(id) });
    },
  });
}

/** Deactivate a brand. Invalidates list and detail on success. */
export function useDeactivateBrand(id: string) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: () => brandsApi.deactivate(id),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: brandKeys.all });
      void queryClient.invalidateQueries({ queryKey: brandKeys.detail(id) });
    },
  });
}

/** Activate a brand. Invalidates list and detail on success. */
export function useActivateBrand(id: string) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: () => brandsApi.activate(id),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: brandKeys.all });
      void queryClient.invalidateQueries({ queryKey: brandKeys.detail(id) });
    },
  });
}

/** Configure staff authentication method. Invalidates list and detail on success. */
export function useConfigureStaffAuth(id: string, slug: string) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (method: StaffAuthMethod) => brandsApi.configureStaffAuth(slug, method),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: brandKeys.all });
      void queryClient.invalidateQueries({ queryKey: brandKeys.detail(id) });
    },
  });
}
