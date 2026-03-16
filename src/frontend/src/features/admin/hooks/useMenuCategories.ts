import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { menuCategoriesApi } from '@api/menuCategories';
import type {
  CreateMenuCategoryRequest,
  UpdateMenuCategoryRequest,
  ReorderMenuCategoryRequest,
} from '@api/menuCategories';

/** Centralized query key factory — scoped by brandSlug for proper cache isolation. */
export const menuCategoryKeys = {
  all: (brandSlug: string) => ['menuCategories', brandSlug] as const,
  detail: (brandSlug: string, id: string) => ['menuCategories', brandSlug, id] as const,
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

/** Fetch a single menu category by id. */
export function useMenuCategory(brandSlug: string, id: string) {
  return useQuery({
    queryKey: menuCategoryKeys.detail(brandSlug, id),
    queryFn: () => menuCategoriesApi.get(brandSlug, id),
    enabled: brandSlug.length > 0 && id.length > 0,
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

/** Update menu category details. Invalidates list and detail on success. */
export function useUpdateMenuCategory(brandSlug: string, id: string) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (data: UpdateMenuCategoryRequest) =>
      menuCategoriesApi.update(brandSlug, id, data),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: menuCategoryKeys.all(brandSlug) });
      void queryClient.invalidateQueries({
        queryKey: menuCategoryKeys.detail(brandSlug, id),
      });
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

/** Assign a product to a menu category. Invalidates list and detail on success. */
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
