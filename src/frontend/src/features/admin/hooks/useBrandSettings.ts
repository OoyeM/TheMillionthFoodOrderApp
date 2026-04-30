import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { brandSettingsApi } from '@api/brandSettings';
import type { UpdateBrandThemingRequest } from '@api/brandSettings';

/**
 * Centralized query key factory for brand settings queries.
 *
 * @expected-unused — US-FP-002 (Brand settings) — used by mutations for cache invalidation
 */
export const brandSettingsKeys = {
  settings: (brandSlug: string) => ['brandSettings', brandSlug] as const,
  theme: (brandSlug: string) => ['brandTheme', brandSlug] as const,
};

// ---------------------------------------------------------------------------
// Queries
// ---------------------------------------------------------------------------

/** Fetch the full brand settings (includes theming). */
export function useBrandSettings(brandSlug: string) {
  return useQuery({
    queryKey: brandSettingsKeys.settings(brandSlug),
    queryFn: () => brandSettingsApi.get(brandSlug),
    enabled: brandSlug.length > 0,
  });
}

// ---------------------------------------------------------------------------
// Mutations
// ---------------------------------------------------------------------------

/** Update the brand's visual theming configuration. */
export function useUpdateBrandTheming(brandSlug: string) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (data: UpdateBrandThemingRequest) =>
      brandSettingsApi.updateTheming(brandSlug, data),
    onSuccess: () => {
      // Invalidate both the full settings and the public theme cache
      void queryClient.invalidateQueries({
        queryKey: brandSettingsKeys.settings(brandSlug),
      });
      void queryClient.invalidateQueries({
        queryKey: brandSettingsKeys.theme(brandSlug),
      });
    },
  });
}

/** Upload a new brand logo. Invalidates settings on success. */
export function useUploadBrandLogo(brandSlug: string) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (file: File) => brandSettingsApi.uploadLogo(brandSlug, file),
    onSuccess: () => {
      void queryClient.invalidateQueries({
        queryKey: brandSettingsKeys.settings(brandSlug),
      });
      void queryClient.invalidateQueries({
        queryKey: brandSettingsKeys.theme(brandSlug),
      });
    },
  });
}
