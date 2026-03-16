import { apiClient } from './client';
import type { Product, ProductListItem } from '../types/common';

export interface TranslationInput {
  languageCode: string;
  name: string;
  description?: string | null;
}

export interface CreateProductRequest {
  basePrice: number;
  imageUrl?: string | null;
  translations: TranslationInput[];
}

export interface UpdateProductRequest {
  basePrice: number;
  imageUrl?: string | null;
  translations: TranslationInput[];
}

/**
 * API functions for product management (brand admin).
 * All routes are brand-scoped: /brands/{brandSlug}/products/...
 * Uses the shared apiClient which injects X-Brand-Slug and credentials.
 */
export const productsApi = {
  list: (brandSlug: string): Promise<ProductListItem[]> =>
    apiClient.get<ProductListItem[]>(`/brands/${brandSlug}/products`).then((r) => r.data),

  get: (brandSlug: string, id: string): Promise<Product> =>
    apiClient.get<Product>(`/brands/${brandSlug}/products/${id}`).then((r) => r.data),

  create: (brandSlug: string, data: CreateProductRequest): Promise<Product> =>
    apiClient.post<Product>(`/brands/${brandSlug}/products`, data).then((r) => r.data),

  update: (brandSlug: string, id: string, data: UpdateProductRequest): Promise<Product> =>
    apiClient.put<Product>(`/brands/${brandSlug}/products/${id}`, data).then((r) => r.data),

  remove: (brandSlug: string, id: string): Promise<void> =>
    apiClient.delete(`/brands/${brandSlug}/products/${id}`).then(() => undefined),
};
