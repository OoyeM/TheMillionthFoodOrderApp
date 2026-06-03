import { apiClient } from './client';
import type { Shop, ShopAddress } from '../types/common';

// ---------------------------------------------------------------------------
// Storefront-only shop DTO (returned by GET /brands/:brandSlug/shops/active)
// ---------------------------------------------------------------------------

/**
 * Lightweight shop response used by the storefront chooser.
 * Mirrors the backend StorefrontShopResponse DTO.
 */
export interface StorefrontShop {
  id: string;
  name: string;
  slug: string;
  address: ShopAddress;
  isOpen: boolean;
}

export interface CreateShopRequest {
  name: string;
  slug: string;
  address: {
    street: string;
    number: string;
    city: string;
    postalCode: string;
    country: string;
  };
  contactEmail: string;
  contactPhone?: string;
}

export interface UpdateShopRequest {
  name: string;
  address: {
    street: string;
    number: string;
    city: string;
    postalCode: string;
    country: string;
  };
  contactEmail: string;
  contactPhone?: string;
}

/**
 * API functions for shop management (brand admin).
 * All routes are brand-scoped: /brands/{brandSlug}/shops/...
 * Uses the shared apiClient which injects X-Brand-Slug and credentials.
 */
export const shopsApi = {
  list: (brandSlug: string): Promise<Shop[]> =>
    apiClient.get<Shop[]>(`/brands/${brandSlug}/shops`).then((r) => r.data),

  get: (brandSlug: string, id: string): Promise<Shop> =>
    apiClient.get<Shop>(`/brands/${brandSlug}/shops/${id}`).then((r) => r.data),

  create: (brandSlug: string, data: CreateShopRequest): Promise<Shop> =>
    apiClient.post<Shop>(`/brands/${brandSlug}/shops`, data).then((r) => r.data),

  update: (brandSlug: string, id: string, data: UpdateShopRequest): Promise<Shop> =>
    apiClient.put<Shop>(`/brands/${brandSlug}/shops/${id}`, data).then((r) => r.data),

  deactivate: (brandSlug: string, id: string): Promise<void> =>
    apiClient
      .post<void>(`/brands/${brandSlug}/shops/${id}/deactivate`)
      .then(() => undefined),

  activate: (brandSlug: string, id: string): Promise<void> =>
    apiClient
      .post<void>(`/brands/${brandSlug}/shops/${id}/activate`)
      .then(() => undefined),

  /**
   * Lists all active shops for a brand, including real-time isOpen status.
   * Used by the storefront shop chooser (US-FP-071).
   * Endpoint: GET /api/brands/:brandSlug/shops/active
   */
  listActive: (brandSlug: string): Promise<StorefrontShop[]> =>
    apiClient
      .get<StorefrontShop[]>(`/brands/${brandSlug}/shops/active`)
      .then((r) => r.data),
};
