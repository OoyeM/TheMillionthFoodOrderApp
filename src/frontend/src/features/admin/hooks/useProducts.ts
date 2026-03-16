import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { productsApi } from '@api/products';
import type { CreateProductRequest, UpdateProductRequest } from '@api/products';

/** Centralized query key factory — scoped by brandSlug for proper cache isolation. */
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

/** Fetch a single product by id. */
export function useProduct(brandSlug: string, id: string) {
  return useQuery({
    queryKey: productKeys.detail(brandSlug, id),
    queryFn: () => productsApi.get(brandSlug, id),
    enabled: brandSlug.length > 0 && id.length > 0,
  });
}

// ---------------------------------------------------------------------------
// Mutations
// ---------------------------------------------------------------------------

/** Create a new product under a brand. Invalidates the list on success. */
export function useCreateProduct(brandSlug: string) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (data: CreateProductRequest) => productsApi.create(brandSlug, data),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: productKeys.all(brandSlug) });
    },
  });
}

/** Update product details. Invalidates list and detail on success. */
export function useUpdateProduct(brandSlug: string, id: string) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (data: UpdateProductRequest) => productsApi.update(brandSlug, id, data),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: productKeys.all(brandSlug) });
      void queryClient.invalidateQueries({ queryKey: productKeys.detail(brandSlug, id) });
    },
  });
}

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
