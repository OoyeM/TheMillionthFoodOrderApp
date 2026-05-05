import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { modifierGroupsApi } from '@api/modifierGroups';
import type {
  CreateModifierGroupRequest,
  SetProductModifierGroupsRequest,
} from '@api/modifierGroups';

export const modifierGroupKeys = {
  all: (brandSlug: string) => ['modifier-groups', brandSlug] as const,
  detail: (brandSlug: string, id: string) => ['modifier-groups', brandSlug, id] as const,
  productGroups: (brandSlug: string, productId: string) =>
    ['modifier-groups', brandSlug, 'product', productId] as const,
};

// ---------------------------------------------------------------------------
// Queries
// ---------------------------------------------------------------------------

/** Fetch all modifier groups for a brand. */
export function useModifierGroups(brandSlug: string) {
  return useQuery({
    queryKey: modifierGroupKeys.all(brandSlug),
    queryFn: () => modifierGroupsApi.list(brandSlug),
    enabled: brandSlug.length > 0,
  });
}

/** Fetch modifier groups assigned to a product. */
export function useProductModifierGroups(brandSlug: string, productId: string) {
  return useQuery({
    queryKey: modifierGroupKeys.productGroups(brandSlug, productId),
    queryFn: () => modifierGroupsApi.getProductModifierGroups(brandSlug, productId),
    enabled: brandSlug.length > 0 && productId.length > 0,
  });
}

// ---------------------------------------------------------------------------
// Mutations
// ---------------------------------------------------------------------------

/** Create a new modifier group under a brand. Invalidates the list on success. */
export function useCreateModifierGroup(brandSlug: string) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (data: CreateModifierGroupRequest) =>
      modifierGroupsApi.create(brandSlug, data),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: modifierGroupKeys.all(brandSlug) });
    },
  });
}

/** Delete (soft-delete) a modifier group. Invalidates list on success. */
export function useDeleteModifierGroup(brandSlug: string) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (id: string) => modifierGroupsApi.remove(brandSlug, id),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: modifierGroupKeys.all(brandSlug) });
    },
  });
}

/** Set the modifier groups assigned to a product, replacing existing assignments. */
export function useSetProductModifierGroups(brandSlug: string, productId: string) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (data: SetProductModifierGroupsRequest) =>
      modifierGroupsApi.setProductModifierGroups(brandSlug, productId, data),
    onSuccess: () => {
      void queryClient.invalidateQueries({
        queryKey: modifierGroupKeys.productGroups(brandSlug, productId),
      });
    },
  });
}
