import { apiClient } from './client';
import type { StaffMember } from '../types/common';

export interface InviteBrandStaffRequest {
  email: string;
  displayName: string;
  /** Numeric role value matching the backend StaffRole enum. */
  role: number;
  shopId?: string | null;
}

/**
 * API functions for brand-scoped staff account management.
 * Uses the shared apiClient which injects credentials.
 */
export const brandStaffApi = {
  list: (brandSlug: string): Promise<StaffMember[]> =>
    apiClient
      .get<StaffMember[]>(`/brands/${brandSlug}/staff`)
      .then((r) => r.data),

  listByShop: (brandSlug: string, shopId: string): Promise<StaffMember[]> =>
    apiClient
      .get<StaffMember[]>(`/brands/${brandSlug}/shops/${shopId}/staff`)
      .then((r) => r.data),

  invite: (brandSlug: string, data: InviteBrandStaffRequest): Promise<StaffMember> =>
    apiClient
      .post<StaffMember>(`/brands/${brandSlug}/staff`, data)
      .then((r) => r.data),

  deactivate: (brandSlug: string, roleId: string): Promise<void> =>
    apiClient
      .post<void>(`/brands/${brandSlug}/staff/${roleId}/deactivate`)
      .then(() => undefined),
};
