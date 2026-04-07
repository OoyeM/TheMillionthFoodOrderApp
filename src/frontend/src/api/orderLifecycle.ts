import { apiClient } from './client';
import type {
  ConfigureOrderLifecycleRequest,
  OrderLifecycleResponse,
} from '../types/common';

/**
 * API functions for shop order lifecycle management (brand admin).
 * All routes are brand-scoped: /brands/{brandSlug}/shops/{shopId}/order-lifecycle
 */
export const orderLifecycleApi = {
  get: (brandSlug: string, shopId: string): Promise<OrderLifecycleResponse> =>
    apiClient
      .get<OrderLifecycleResponse>(
        `/brands/${brandSlug}/shops/${shopId}/order-lifecycle`,
      )
      .then((r) => r.data),

  configure: (
    brandSlug: string,
    shopId: string,
    data: ConfigureOrderLifecycleRequest,
  ): Promise<OrderLifecycleResponse> =>
    apiClient
      .put<OrderLifecycleResponse>(
        `/brands/${brandSlug}/shops/${shopId}/order-lifecycle`,
        data,
      )
      .then((r) => r.data),

  reset: (brandSlug: string, shopId: string): Promise<OrderLifecycleResponse> =>
    apiClient
      .post<OrderLifecycleResponse>(
        `/brands/${brandSlug}/shops/${shopId}/order-lifecycle/reset`,
      )
      .then((r) => r.data),
};
