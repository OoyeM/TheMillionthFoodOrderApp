import { apiClient } from './client';
import type {
  TaxConfigurationResponse,
  UpdateTaxConfigurationRequest,
  TaxBreakdownResponse,
  ConsumptionMode,
} from '../types/common';

/**
 * API functions for brand-level tax configuration.
 * Routes are brand-scoped: /brands/{brandSlug}/tax-configuration
 */
export const taxConfigurationApi = {
  /**
   * Fetch the tax configuration for a brand.
   */
  get: (brandSlug: string): Promise<TaxConfigurationResponse> =>
    apiClient
      .get<TaxConfigurationResponse>(`/brands/${brandSlug}/tax-configuration`)
      .then((r) => r.data),

  /**
   * Update the tax configuration for a brand (replaces all VAT rates).
   */
  update: (
    brandSlug: string,
    data: UpdateTaxConfigurationRequest,
  ): Promise<TaxConfigurationResponse> =>
    apiClient
      .put<TaxConfigurationResponse>(`/brands/${brandSlug}/tax-configuration`, data)
      .then((r) => r.data),

  /**
   * Calculate tax breakdown for a given gross amount and consumption mode.
   */
  calculate: (
    brandSlug: string,
    grossAmount: number,
    consumptionMode: ConsumptionMode,
  ): Promise<TaxBreakdownResponse> =>
    apiClient
      .post<TaxBreakdownResponse>(
        `/brands/${brandSlug}/tax-configuration/calculate`,
        { grossAmount, consumptionMode },
      )
      .then((r) => r.data),
};
