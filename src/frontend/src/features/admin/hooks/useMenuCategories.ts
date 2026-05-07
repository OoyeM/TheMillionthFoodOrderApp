import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { menuCategoriesApi } from '@api/menuCategories';
import type {
  CreateMenuCategoryRequest,
  ReorderMenuCategoryRequest,
  ReorderProductsRequest,
} from '@api/menuCategories';

export const menuCategoryKeys = {
  all: (brandSlug: string) => ['menuCategories', brandSlug] as const,
  detail: (brandSlug: string, id: string) => ['menuCategories', brandSlug, id] as const,
  products: (brandSlug: string, id: string) =>
    ['menuCategories', brandSlug, id, 'products'] as const,
};

// ---------------------------------------------------------------------------
// Queries
// ---------------------------------------------------------------------------

/** Fetch all menu categories for a brand. */
export function useMenuCategories(brandSlug: string) {
  return useQuery({
    queryKey: menuCategoryKeys.all(brandSlug),
    queryFn: () => menuCategoriesApi.list(brandSlug),
    enabled: brandSlug.length > 0,
  });
}

// ---------------------------------------------------------------------------
// Mutations
// ---------------------------------------------------------------------------

/** Create a new menu category under a brand. Invalidates the list on success. */
export function useCreateMenuCategory(brandSlug: string) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (data: CreateMenuCategoryRequest) =>
      menuCategoriesApi.create(brandSlug, data),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: menuCategoryKeys.all(brandSlug) });
    },
  });
}

/** Delete a menu category. Invalidates list on success. */
export function useDeleteMenuCategory(brandSlug: string) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (id: string) => menuCategoriesApi.remove(brandSlug, id),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: menuCategoryKeys.all(brandSlug) });
    },
  });
}

/** Set the sort order of a menu category to a specific value. Invalidates list on success. */
export function useReorderMenuCategory(brandSlug: string) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ id, sortOrder }: { id: string } & ReorderMenuCategoryRequest) =>
      menuCategoriesApi.reorder(brandSlug, id, { sortOrder }),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: menuCategoryKeys.all(brandSlug) });
    },
  });
}

/**
 * Assign a product to a menu category. Invalidates list and detail on success.
 *
 * @expected-unused — US-FP-022 (Menu category assignment) — deferred to admin UX
 */
export function useAssignProductToCategory(brandSlug: string, categoryId: string) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (productId: string) =>
      menuCategoriesApi.assignProduct(brandSlug, { productId, categoryId }),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: menuCategoryKeys.all(brandSlug) });
      void queryClient.invalidateQueries({
        queryKey: menuCategoryKeys.detail(brandSlug, categoryId),
      });
    },
  });
}

/** Fetch all products assigned to a menu category, ordered by sortOrderInCategory. */
export function useCategoryProducts(brandSlug: string, categoryId: string) {
  return useQuery({
    queryKey: menuCategoryKeys.products(brandSlug, categoryId),
    queryFn: () => menuCategoriesApi.listProducts(brandSlug, categoryId),
    enabled: brandSlug.length > 0 && categoryId.length > 0,
  });
}

/** Reorder products within a menu category. Invalidates the products list on success. */
export function useReorderCategoryProducts(brandSlug: string, categoryId: string) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (productIds: string[]) => {
      const data: ReorderProductsRequest = { productIds };
      return menuCategoriesApi.reorderProducts(brandSlug, categoryId, data);
    },
    onSuccess: () => {
      void queryClient.invalidateQueries({
        queryKey: menuCategoryKeys.products(brandSlug, categoryId),
      });
    },
  });
}
