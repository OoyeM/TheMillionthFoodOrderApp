import { apiClient } from './client';
import type { PlatformAdmin } from '../types/common';

export interface InvitePlatformAdminRequest {
  email: string;
  displayName: string;
}

/**
 * API functions for platform admin account management.
 * Uses the shared apiClient which injects credentials.
 */
export const platformAdminsApi = {
  list: (): Promise<PlatformAdmin[]> =>
    apiClient.get<PlatformAdmin[]>('/platform-admins').then((r) => r.data),

  invite: (data: InvitePlatformAdminRequest): Promise<PlatformAdmin> =>
    apiClient.post<PlatformAdmin>('/platform-admins', data).then((r) => r.data),

  deactivate: (id: string): Promise<void> =>
    apiClient.post(`/platform-admins/${id}/deactivate`).then(() => undefined),
};
