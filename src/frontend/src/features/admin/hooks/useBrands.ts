import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { brandsApi } from '@api/brands';
import type { CreateBrandRequest } from '@api/brands';
import type { StaffAuthMethod } from '../../../types/common';

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
