import { apiClient } from './client';
import type { Product, ProductListItem } from '../types/common';
import { extractData, toVoid } from './utils';

export interface TranslationInput {
  languageCode: string;
  name: string;
  description?: string | null;
}

export interface CreateProductRequest {
  basePrice: number;
  imageUrl?: string | null;
  translations: TranslationInput[];
  allergens?: number[];
  dietaryTags?: number[];
}

export interface UpdateProductRequest {
  basePrice: number;
  imageUrl?: string | null;
  translations: TranslationInput[];
  allergens?: number[];
  dietaryTags?: number[];
}

export interface CreateComboProductRequest {
  basePrice: number;
  imageUrl?: string | null;
  translations: TranslationInput[];
  componentProductIds: string[];
}

export interface UpdateComboProductRequest {
  basePrice: number;
  imageUrl?: string | null;
  translations: TranslationInput[];
  componentProductIds: string[];
}

/**
 * API functions for product management (brand admin).
 * All routes are brand-scoped: /brands/{brandSlug}/products/...
 * Uses the shared apiClient which injects X-Brand-Slug and credentials.
 */
export const productsApi = {
  list: (brandSlug: string): Promise<ProductListItem[]> =>
    apiClient.get<ProductListItem[]>(`/brands/${brandSlug}/products`).then(extractData),

  get: (brandSlug: string, id: string): Promise<Product> =>
    apiClient.get<Product>(`/brands/${brandSlug}/products/${id}`).then(extractData),

  create: (brandSlug: string, data: CreateProductRequest): Promise<Product> =>
    apiClient.post<Product>(`/brands/${brandSlug}/products`, data).then(extractData),

  update: (brandSlug: string, id: string, data: UpdateProductRequest): Promise<Product> =>
    apiClient.put<Product>(`/brands/${brandSlug}/products/${id}`, data).then(extractData),

  remove: (brandSlug: string, id: string): Promise<void> =>
    apiClient.delete(`/brands/${brandSlug}/products/${id}`).then(toVoid),

  createCombo: (brandSlug: string, data: CreateComboProductRequest): Promise<Product> =>
    apiClient.post<Product>(`/brands/${brandSlug}/combo-products`, data).then(extractData),

  updateCombo: (brandSlug: string, id: string, data: UpdateComboProductRequest): Promise<Product> =>
    apiClient.put<Product>(`/brands/${brandSlug}/combo-products/${id}`, data).then(extractData),
};
