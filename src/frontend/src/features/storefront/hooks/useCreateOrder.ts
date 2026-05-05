import { useMutation } from '@tanstack/react-query';
import { ordersApi } from '@api/orders';
import type { CreateOrderRequest, OrderResponse } from '@api/orders';

/**
 * TanStack Query mutation hook for creating an order.
 * Called from CheckoutPage after form validation.
 *
 * On success, the caller should navigate to /order/:orderId using the returned id.
 */
export function useCreateOrder(brandSlug: string, shopId: string) {
  return useMutation<OrderResponse, Error, CreateOrderRequest>({
    mutationFn: (data: CreateOrderRequest) => ordersApi.create(brandSlug, shopId, data),
  });
}
