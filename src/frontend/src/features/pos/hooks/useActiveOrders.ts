import { useQuery, useQueryClient } from '@tanstack/react-query';
import { ordersApi, type OrderResponse } from '@api/orders';
import { useOrderUpdates } from '@api/useOrderUpdates';
import type { ConnectionStatus } from '@api/useSignalR';

export const activeOrdersQueryKey = (brandSlug: string, shopId: string) =>
  ['orders', 'active', brandSlug, shopId] as const;

/**
 * Subscribes to the shop's active orders for the kitchen display.
 *
 * Real-time invalidation: any `OrderStatusChanged` event for this shop refetches the
 * list — covers new orders, status changes, and completions in a single code path.
 * The list is small (active orders for one shop) so the refetch cost is trivial.
 */
export function useActiveOrders(brandSlug: string, shopId: string): {
  orders: OrderResponse[] | undefined;
  isLoading: boolean;
  isError: boolean;
  connectionStatus: ConnectionStatus;
} {
  const queryClient = useQueryClient();

  const query = useQuery<OrderResponse[]>({
    queryKey: activeOrdersQueryKey(brandSlug, shopId),
    queryFn: () => ordersApi.listActive(brandSlug, shopId),
    enabled: brandSlug.length > 0 && shopId.length > 0,
    staleTime: 0,
    refetchOnWindowFocus: true,
  });

  const hasShop = brandSlug.length > 0 && shopId.length > 0;
  const { status: connectionStatus } = useOrderUpdates({
    ...(hasShop ? { shopGroup: { brandSlug, shopId } } : {}),
    onStatusChange: () => {
      void queryClient.invalidateQueries({
        queryKey: activeOrdersQueryKey(brandSlug, shopId),
      });
    },
  });

  return {
    orders: query.data,
    isLoading: query.isLoading,
    isError: query.isError,
    connectionStatus,
  };
}
