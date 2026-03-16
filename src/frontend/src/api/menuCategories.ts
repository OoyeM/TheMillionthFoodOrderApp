import { apiClient } from './client';
import type { MenuCategory, MenuCategoryListItem, ProductListItem } from '../types/common';

export interface MenuCategoryTranslationInput {
  languageCode: string;
  name: string;
}

export interface CreateMenuCategoryRequest {
  sortOrder: number;
  imageUrl?: string | null;
  translations: MenuCategoryTranslationInput[];
}

export interface UpdateMenuCategoryRequest {
  sortOrder: number;
  imageUrl?: string | null;
  translations: MenuCategoryTranslationInput[];
}

export interface ReorderMenuCategoryRequest {
  sortOrder: number;
}

export interface AssignProductRequest {
  productId: string;
  categoryId: string;
}

export interface ReorderProductsRequest {
  productIds: string[];
}

/**
 * API functions for menu category management (brand admin).
 * All routes are brand-scoped: /brands/{brandSlug}/menu-categories/...
 * Uses the shared apiClient which injects X-Brand-Slug and credentials.
 */
export const menuCategoriesApi = {
  list: (brandSlug: string): Promise<MenuCategoryListItem[]> =>
    apiClient
      .get<MenuCategoryListItem[]>(`/brands/${brandSlug}/menu-categories`)
      .then((r) => r.data),

  get: (brandSlug: string, id: string): Promise<MenuCategory> =>
    apiClient
      .get<MenuCategory>(`/brands/${brandSlug}/menu-categories/${id}`)
      .then((r) => r.data),

  create: (brandSlug: string, data: CreateMenuCategoryRequest): Promise<MenuCategory> =>
    apiClient
      .post<MenuCategory>(`/brands/${brandSlug}/menu-categories`, data)
      .then((r) => r.data),

  update: (
    brandSlug: string,
    id: string,
    data: UpdateMenuCategoryRequest,
  ): Promise<MenuCategory> =>
    apiClient
      .put<MenuCategory>(`/brands/${brandSlug}/menu-categories/${id}`, data)
      .then((r) => r.data),

  remove: (brandSlug: string, id: string): Promise<void> =>
    apiClient
      .delete(`/brands/${brandSlug}/menu-categories/${id}`)
      .then(() => undefined),

  reorder: (
    brandSlug: string,
    id: string,
    data: ReorderMenuCategoryRequest,
  ): Promise<void> =>
    apiClient
      .patch<void>(`/brands/${brandSlug}/menu-categories/${id}/sort-order`, data)
      .then(() => undefined),

  assignProduct: (
    brandSlug: string,
    data: AssignProductRequest,
  ): Promise<void> =>
    apiClient
      .post<void>(`/brands/${brandSlug}/menu-categories/assign-product`, data)
      .then(() => undefined),

  listProducts: (brandSlug: string, categoryId: string): Promise<ProductListItem[]> =>
    apiClient
      .get<ProductListItem[]>(`/brands/${brandSlug}/menu-categories/${categoryId}/products`)
      .then((r) => r.data),

  reorderProducts: (
    brandSlug: string,
    categoryId: string,
    data: ReorderProductsRequest,
  ): Promise<void> =>
    apiClient
      .put<void>(
        `/brands/${brandSlug}/menu-categories/${categoryId}/products/order`,
        data,
      )
      .then(() => undefined),
};
