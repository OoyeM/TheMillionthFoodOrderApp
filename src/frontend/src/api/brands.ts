import { apiClient } from './client';
import type { Brand, StaffAuthMethod } from '../types/common';

export interface CreateBrandRequest {
  name: string;
  slug: string;
  contactEmail: string;
  contactPhone?: string;
}

export interface UpdateBrandRequest {
  name: string;
  contactEmail: string;
  contactPhone?: string;
}

/**
 * API functions for brand management (platform admin only).
 * Uses the shared apiClient which injects X-Brand-Slug and credentials.
 */
export const brandsApi = {
  list: (): Promise<Brand[]> =>
    apiClient.get<Brand[]>('/brands').then((r) => r.data),

  get: (id: string): Promise<Brand> =>
    apiClient.get<Brand>(`/brands/${id}`).then((r) => r.data),

  create: (data: CreateBrandRequest): Promise<Brand> =>
    apiClient.post<Brand>('/brands', data).then((r) => r.data),

  update: (id: string, data: UpdateBrandRequest): Promise<Brand> =>
    apiClient.put<Brand>(`/brands/${id}`, data).then((r) => r.data),

  deactivate: (id: string): Promise<void> =>
    apiClient.post<void>(`/brands/${id}/deactivate`).then(() => undefined),

  activate: (id: string): Promise<void> =>
    apiClient.post<void>(`/brands/${id}/activate`).then(() => undefined),

  configureStaffAuth: (slug: string, method: StaffAuthMethod): Promise<Brand> => {
    const methodMap: Record<StaffAuthMethod, number> = {
      EmailPassword: 0,
      GoogleSso: 1,
      MicrosoftSso: 2,
    };
    return apiClient
      .put<Brand>(`/brands/${slug}/staff-auth`, { method: methodMap[method] })
      .then((r) => r.data);
  },
};
