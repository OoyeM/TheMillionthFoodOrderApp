import { useQuery } from '@tanstack/react-query';
import { brandSettingsApi } from '@api/brandSettings';
import type { BrandTheme } from '../../../types/common';

/** Default theme values applied when the brand has not configured theming. */
export const defaultTheme: BrandTheme = {
  logoUrl: null,
  customDomain: null,
  primaryColor: '#111827',
  secondaryColor: '#6b7280',
  accentColor: '#2563eb',
  headingFontFamily: 'System Default',
  bodyFontFamily: 'System Default',
};

/** Query key factory for brand theme queries. */
export const brandThemeKeys = {
  theme: (brandSlug: string) => ['brandTheme', brandSlug] as const,
};

/**
 * Fetches the brand theme from the public theme endpoint.
 * - Cached for 5 minutes (staleTime) — refreshes in the background on next use.
 * - Falls back to `defaultTheme` when the brand has not configured theming or the
 *   request fails.
 * - No auth required; the endpoint is public.
 */
export function useBrandTheme(brandSlug: string) {
  return useQuery({
    queryKey: brandThemeKeys.theme(brandSlug),
    queryFn: () => brandSettingsApi.getTheme(brandSlug),
    staleTime: 5 * 60 * 1000, // 5 minutes
    enabled: brandSlug.length > 0,
    // Provide a placeholder so the storefront renders immediately with defaults
    placeholderData: defaultTheme,
  });
}
