import { useEffect, useRef } from 'react';
import { useSignalR, type ConnectionStatus } from './useSignalR';

export interface OrderStatusUpdate {
  orderId: string;
  shopId: string;
  brandSlug: string;
  previousStatus: string;
  newStatus: string;
  customerName: string | null;
  timestamp: string;
}

interface UseOrderUpdatesOptions {
  /** Shop group to join (for kitchen/POS). */
  shopGroup?: { brandSlug: string; shopId: string };
  /** Order ID to track (for customer tracking). */
  orderId?: string;
  /** Callback fired on each status change event. */
  onStatusChange?: (update: OrderStatusUpdate) => void;
}

/**
 * Hook for components that need real-time order status updates.
 * Connects to SignalR, joins the appropriate group, and invokes
 * the callback on each status change event.
 */
export function useOrderUpdates(
  options: UseOrderUpdatesOptions,
): { status: ConnectionStatus } {
  const { shopGroup, orderId, onStatusChange } = options;
  const { connection, status } = useSignalR({ shopGroup, orderId });
  const callbackRef = useRef(onStatusChange);
  callbackRef.current = onStatusChange;

  useEffect(() => {
    if (!connection) return;

    const handler = (update: OrderStatusUpdate) => {
      callbackRef.current?.(update);
    };

    connection.on('OrderStatusChanged', handler);
    return () => {
      connection.off('OrderStatusChanged', handler);
    };
  }, [connection]);

  return { status };
}
