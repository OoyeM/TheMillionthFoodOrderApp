import { useMutation } from '@tanstack/react-query';
import { ordersApi } from '@api/orders';
import type { CreateInStoreOrderRequest, OrderResponse } from '@api/orders';
import { useOrderState } from '../context/PosOrderContext';

/**
 * TanStack Query mutation hook for creating an in-store (POS) order.
 *
 * - Accepts brandSlug + shopId as route params (not in the body)
 * - Builds the request payload from current POS order state
 * - Clears the order on success so the terminal is ready for the next customer
 * - Error handling mirrors useCreateOrder in the storefront feature
 */
export function useCreateInStoreOrder(brandSlug: string, shopId: string) {
  const { state, clearOrder } = useOrderState();

  return useMutation<OrderResponse, Error, { customerName?: string }>({
    mutationFn: ({ customerName }) => {
      const payload: CreateInStoreOrderRequest = {
        orderType: state.orderType,
        paymentMethod: state.paymentMethod,
        items: state.items.map((item) => ({
          productId: item.productId,
          quantity: item.quantity,
          selectedModifierIds: item.selectedModifiers.map((m) => m.modifierId),
        })),
        ...(customerName ? { customerName } : {}),
        ...(state.orderType === 'EatIn' && state.tableNumber !== undefined
          ? { tableNumber: state.tableNumber }
          : {}),
      };
      return ordersApi.createInStore(brandSlug, shopId, payload);
    },
    onSuccess: () => {
      clearOrder();
    },
  });
}
