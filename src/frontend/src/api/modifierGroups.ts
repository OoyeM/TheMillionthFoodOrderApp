import { apiClient } from './client';
import type { ModifierGroupListItem, ModifierGroupResponse, ProductModifierGroupResponse } from '../types/common';

// ---------------------------------------------------------------------------
// Request types
// ---------------------------------------------------------------------------

export interface ModifierTranslationInput {
  languageCode: string;
  name: string;
}

export interface ModifierInput {
  translations: ModifierTranslationInput[];
  priceAdjustment: number;
  sortOrder: number;
}

export interface CreateModifierGroupRequest {
  translations: ModifierTranslationInput[];
  modifiers: ModifierInput[];
}

export interface UpdateModifierGroupRequest {
  translations: ModifierTranslationInput[];
  modifiers: ModifierInput[];
}

export interface ProductModifierGroupInput {
  modifierGroupId: string;
  sortOrder: number;
}

export interface SetProductModifierGroupsRequest {
  modifierGroups: ProductModifierGroupInput[];
}

/**
 * API functions for modifier group management (brand admin).
 * All routes are brand-scoped: /brands/{brandSlug}/modifier-groups/...
 * Uses the shared apiClient which injects X-Brand-Slug and credentials.
 */
export const modifierGroupsApi = {
  list: (brandSlug: string): Promise<ModifierGroupListItem[]> =>
    apiClient
      .get<ModifierGroupListItem[]>(`/brands/${brandSlug}/modifier-groups`)
      .then((r) => r.data),

  get: (brandSlug: string, id: string): Promise<ModifierGroupResponse> =>
    apiClient
      .get<ModifierGroupResponse>(`/brands/${brandSlug}/modifier-groups/${id}`)
      .then((r) => r.data),

  create: (brandSlug: string, data: CreateModifierGroupRequest): Promise<ModifierGroupResponse> =>
    apiClient
      .post<ModifierGroupResponse>(`/brands/${brandSlug}/modifier-groups`, data)
      .then((r) => r.data),

  update: (
    brandSlug: string,
    id: string,
    data: UpdateModifierGroupRequest,
  ): Promise<ModifierGroupResponse> =>
    apiClient
      .put<ModifierGroupResponse>(`/brands/${brandSlug}/modifier-groups/${id}`, data)
      .then((r) => r.data),

  remove: (brandSlug: string, id: string): Promise<void> =>
    apiClient
      .delete(`/brands/${brandSlug}/modifier-groups/${id}`)
      .then(() => undefined),

  getProductModifierGroups: (
    brandSlug: string,
    productId: string,
  ): Promise<ProductModifierGroupResponse[]> =>
    apiClient
      .get<ProductModifierGroupResponse[]>(
        `/brands/${brandSlug}/products/${productId}/modifier-groups`,
      )
      .then((r) => r.data),

  setProductModifierGroups: (
    brandSlug: string,
    productId: string,
    data: SetProductModifierGroupsRequest,
  ): Promise<void> =>
    apiClient
      .put(
        `/brands/${brandSlug}/products/${productId}/modifier-groups`,
        data,
      )
      .then(() => undefined),
};
