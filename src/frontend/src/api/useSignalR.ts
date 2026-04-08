import { useEffect, useRef, useState, useCallback } from 'react';
import { HubConnection, HubConnectionState } from '@microsoft/signalr';
import { getOrderHubConnection } from './signalr';

export type ConnectionStatus =
  | 'disconnected'
  | 'connecting'
  | 'connected'
  | 'reconnecting';

interface UseSignalROptions {
  /** Shop group to join (for kitchen/POS). */
  shopGroup?: { brandSlug: string; shopId: string };
  /** Order ID to track (for customer tracking). */
  orderId?: string;
}

/**
 * React hook that manages a SignalR connection to the order hub.
 * Handles connection lifecycle, group membership, and auto-reconnect.
 */
export function useSignalR(options: UseSignalROptions = {}) {
  const { shopGroup, orderId } = options;
  const [status, setStatus] = useState<ConnectionStatus>('disconnected');
  const connectionRef = useRef<HubConnection | null>(null);
  const groupsRef = useRef<{ shopGroup?: string; orderGroup?: string }>({});

  const updateStatus = useCallback((conn: HubConnection) => {
    switch (conn.state) {
      case HubConnectionState.Connected:
        setStatus('connected');
        break;
      case HubConnectionState.Connecting:
      case HubConnectionState.Reconnecting:
        setStatus(
          conn.state === HubConnectionState.Reconnecting
            ? 'reconnecting'
            : 'connecting',
        );
        break;
      default:
        setStatus('disconnected');
    }
  }, []);

  // Join groups after connection is established
  const joinGroups = useCallback(
    async (conn: HubConnection) => {
      if (conn.state !== HubConnectionState.Connected) return;

      if (shopGroup) {
        const groupName = `shop:${shopGroup.brandSlug}:${shopGroup.shopId}`;
        if (groupsRef.current.shopGroup !== groupName) {
          await conn.invoke('JoinShopGroup', shopGroup.brandSlug, shopGroup.shopId);
          groupsRef.current.shopGroup = groupName;
        }
      }

      if (orderId) {
        const groupName = `order:${orderId}`;
        if (groupsRef.current.orderGroup !== groupName) {
          await conn.invoke('JoinOrderGroup', orderId);
          groupsRef.current.orderGroup = groupName;
        }
      }
    },
    [shopGroup, orderId],
  );

  useEffect(() => {
    const conn = getOrderHubConnection();
    connectionRef.current = conn;

    conn.onreconnecting(() => setStatus('reconnecting'));
    conn.onreconnected(() => {
      setStatus('connected');
      // Re-join groups after reconnect
      groupsRef.current = {};
      void joinGroups(conn);
    });
    conn.onclose(() => setStatus('disconnected'));

    if (conn.state === HubConnectionState.Disconnected) {
      setStatus('connecting');
      conn
        .start()
        .then(() => {
          setStatus('connected');
          void joinGroups(conn);
        })
        .catch(() => setStatus('disconnected'));
    } else {
      updateStatus(conn);
      void joinGroups(conn);
    }

    return () => {
      // Leave groups on unmount but don't stop the shared connection
      const current = groupsRef.current;
      if (conn.state === HubConnectionState.Connected) {
        if (current.shopGroup && shopGroup) {
          void conn.invoke('LeaveShopGroup', shopGroup.brandSlug, shopGroup.shopId);
        }
        if (current.orderGroup && orderId) {
          void conn.invoke('LeaveOrderGroup', orderId);
        }
      }
      groupsRef.current = {};
    };
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [shopGroup?.brandSlug, shopGroup?.shopId, orderId]);

  return {
    connection: connectionRef.current,
    status,
  };
}
