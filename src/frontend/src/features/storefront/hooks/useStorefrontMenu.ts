import { useQuery } from '@tanstack/react-query';
import { menuCategoriesApi } from '@api/menuCategories';
import { modifierGroupsApi } from '@api/modifierGroups';
import type { MenuCategoryListItem, ProductListItem, ProductModifierGroupResponse } from '@/types/common';

// ---------------------------------------------------------------------------
// Query key factories — storefront-scoped, separate from admin keys
// ---------------------------------------------------------------------------

const storefrontMenuKeys = {
  categories: (brandSlug: string) => ['storefront', 'menuCategories', brandSlug] as const,
  categoryProducts: (brandSlug: string, categoryId: string) =>
    ['storefront', 'menuCategories', brandSlug, categoryId, 'products'] as const,
  productModifiers: (brandSlug: string, productId: string) =>
    ['storefront', 'productModifiers', brandSlug, productId] as const,
};

// ---------------------------------------------------------------------------
// Queries
// ---------------------------------------------------------------------------

/**
 * Fetches all menu categories for a brand, sorted by sortOrder.
 * Used by the storefront menu page to render category sections.
 */
export function useStorefrontCategories(brandSlug: string) {
  return useQuery<MenuCategoryListItem[]>({
    queryKey: storefrontMenuKeys.categories(brandSlug),
    queryFn: async () => {
      const categories = await menuCategoriesApi.list(brandSlug);
      return [...categories].sort((a, b) => a.sortOrder - b.sortOrder);
    },
    enabled: brandSlug.length > 0,
    staleTime: 2 * 60 * 1000, // 2 minutes
  });
}

/**
 * Fetches all products for a menu category, sorted by sortOrderInCategory.
 * Used by the storefront menu page to render product cards within each section.
 */
export function useStorefrontCategoryProducts(brandSlug: string, categoryId: string) {
  return useQuery<ProductListItem[]>({
    queryKey: storefrontMenuKeys.categoryProducts(brandSlug, categoryId),
    queryFn: async () => {
      const products = await menuCategoriesApi.listProducts(brandSlug, categoryId);
      return [...products].sort((a, b) => a.sortOrderInCategory - b.sortOrderInCategory);
    },
    enabled: brandSlug.length > 0 && categoryId.length > 0,
    staleTime: 2 * 60 * 1000,
  });
}

/**
 * Fetches modifier groups assigned to a product.
 * Used by the ModifierModal to display customisation options before adding to cart.
 */
export function useProductModifierGroups(brandSlug: string, productId: string) {
  return useQuery<ProductModifierGroupResponse[]>({
    queryKey: storefrontMenuKeys.productModifiers(brandSlug, productId),
    queryFn: () => modifierGroupsApi.getProductModifierGroups(brandSlug, productId),
    enabled: brandSlug.length > 0 && productId.length > 0,
    staleTime: 5 * 60 * 1000,
  });
}
