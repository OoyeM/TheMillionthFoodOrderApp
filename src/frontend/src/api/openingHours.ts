import { apiClient } from './client';
import type {
  OpeningHoursResponse,
  SetOpeningHoursRequest,
  ShopStatusResponse,
} from '../types/common';

/**
 * API functions for shop opening hours management (brand admin).
 * All routes are brand-scoped: /brands/{brandSlug}/shops/{id}/...
 * Uses the shared apiClient which injects X-Brand-Slug and credentials.
 */
export const openingHoursApi = {
  get: (brandSlug: string, shopId: string): Promise<OpeningHoursResponse> =>
    apiClient
      .get<OpeningHoursResponse>(`/brands/${brandSlug}/shops/${shopId}/opening-hours`)
      .then((r) => r.data),

  set: (
    brandSlug: string,
    shopId: string,
    data: SetOpeningHoursRequest,
  ): Promise<OpeningHoursResponse> =>
    apiClient
      .put<OpeningHoursResponse>(`/brands/${brandSlug}/shops/${shopId}/opening-hours`, data)
      .then((r) => r.data),

  getStatus: (brandSlug: string, shopId: string): Promise<ShopStatusResponse> =>
    apiClient
      .get<ShopStatusResponse>(`/brands/${brandSlug}/shops/${shopId}/status`)
      .then((r) => r.data),
};
