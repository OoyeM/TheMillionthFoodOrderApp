import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { productsApi } from '@api/products';

export const productKeys = {
  all: (brandSlug: string) => ['products', brandSlug] as const,
  detail: (brandSlug: string, id: string) => ['products', brandSlug, id] as const,
};

// ---------------------------------------------------------------------------
// Queries
// ---------------------------------------------------------------------------

/** Fetch all products for a brand. */
export function useProducts(brandSlug: string) {
  return useQuery({
    queryKey: productKeys.all(brandSlug),
    queryFn: () => productsApi.list(brandSlug),
    enabled: brandSlug.length > 0,
  });
}

// ---------------------------------------------------------------------------
// Mutations
// ---------------------------------------------------------------------------

/** Delete (soft-delete) a product. Invalidates list on success. */
export function useDeleteProduct(brandSlug: string) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (id: string) => productsApi.remove(brandSlug, id),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: productKeys.all(brandSlug) });
    },
  });
}

