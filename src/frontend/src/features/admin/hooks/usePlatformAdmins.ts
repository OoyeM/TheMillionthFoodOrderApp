import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { platformAdminsApi } from '@api/platformAdmins';
import type { InvitePlatformAdminRequest } from '@api/platformAdmins';

/**
 * Centralized query key factory — keeps cache invalidation consistent.
 *
 * @expected-unused — US-FP-001 (Platform admin) — used by mutations for cache invalidation
 */
export const platformAdminKeys = {
  all: ['platform-admins'] as const,
};

// ---------------------------------------------------------------------------
// Queries
// ---------------------------------------------------------------------------

/** Fetch all platform admins. */
export function usePlatformAdmins() {
  return useQuery({
    queryKey: platformAdminKeys.all,
    queryFn: () => platformAdminsApi.list(),
  });
}

// ---------------------------------------------------------------------------
// Mutations
// ---------------------------------------------------------------------------

/** Invite a new platform admin. Invalidates the list on success. */
export function useInvitePlatformAdmin() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (data: InvitePlatformAdminRequest) => platformAdminsApi.invite(data),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: platformAdminKeys.all });
    },
  });
}

/** Deactivate (revoke admin privileges from) a platform admin. Invalidates the list on success. */
export function useDeactivatePlatformAdmin() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (id: string) => platformAdminsApi.deactivate(id),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: platformAdminKeys.all });
    },
  });
}
