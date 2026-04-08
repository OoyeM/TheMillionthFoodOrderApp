import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { taxConfigurationApi } from '../../../api/taxConfiguration';
import type { UpdateTaxConfigurationRequest } from '../../../types/common';

/** Centralized query key factory for tax configuration queries. */
export const taxConfigurationKeys = {
  config: (brandSlug: string) => ['taxConfiguration', brandSlug] as const,
};

// ---------------------------------------------------------------------------
// Queries
// ---------------------------------------------------------------------------

/** Fetch the brand-level tax configuration. */
export function useTaxConfiguration(brandSlug: string) {
  return useQuery({
    queryKey: taxConfigurationKeys.config(brandSlug),
    queryFn: () => taxConfigurationApi.get(brandSlug),
    enabled: brandSlug.length > 0,
  });
}

// ---------------------------------------------------------------------------
// Mutations
// ---------------------------------------------------------------------------

/** Update the brand's tax configuration (replaces all VAT rates). */
export function useUpdateTaxConfiguration(brandSlug: string) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (data: UpdateTaxConfigurationRequest) =>
      taxConfigurationApi.update(brandSlug, data),
    onSuccess: () => {
      void queryClient.invalidateQueries({
        queryKey: taxConfigurationKeys.config(brandSlug),
      });
    },
  });
}
