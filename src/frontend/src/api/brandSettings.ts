import { apiClient } from './client';
import type { BrandSettings, BrandColors, BrandTypography, BrandTheme } from '../types/common';

export interface UpdateBrandThemingRequest {
  colors?: BrandColors | null;
  typography?: BrandTypography | null;
  customDomain?: string | null;
}

export interface UploadBrandLogoResponse {
  logoUrl: string;
}

/**
 * API functions for brand settings and theming.
 * Routes are brand-scoped: /brands/{brandSlug}/settings/...
 */
export const brandSettingsApi = {
  /**
   * Fetch the full brand settings (locale, theming, etc.).
   */
  get: (brandSlug: string): Promise<BrandSettings> =>
    apiClient.get<BrandSettings>(`/brands/${brandSlug}/settings`).then((r) => r.data),

  /**
   * Update the visual theming configuration (colors, typography, custom domain).
   */
  updateTheming: (
    brandSlug: string,
    data: UpdateBrandThemingRequest,
  ): Promise<BrandSettings> =>
    apiClient
      .put<BrandSettings>(`/brands/${brandSlug}/settings/theming`, data)
      .then((r) => r.data),

  /**
   * Upload a new brand logo. Sends as multipart/form-data.
   * Replaces the previous logo if one already exists.
   */
  uploadLogo: (brandSlug: string, file: File): Promise<UploadBrandLogoResponse> => {
    const form = new FormData();
    form.append('logo', file);
    // Do NOT set Content-Type manually — Axios sets it automatically with the correct
    // multipart boundary when the request body is a FormData instance.
    return apiClient
      .post<UploadBrandLogoResponse>(`/brands/${brandSlug}/settings/logo`, form)
      .then((r) => r.data);
  },

  /**
   * Fetch the lightweight public theme for the storefront.
   * No auth required — safe to call from the storefront.
   */
  getTheme: (brandSlug: string): Promise<BrandTheme> =>
    apiClient.get<BrandTheme>(`/brands/${brandSlug}/theme`).then((r) => r.data),
};
