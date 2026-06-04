import { useCallback, useEffect, useMemo, useRef, useState } from 'react';
import { useParams } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { useActiveOrders, activeOrdersQueryKey } from '../hooks/useActiveOrders';
import { KitchenOrderCard } from '../components/KitchenOrderCard';
import { printTicket } from '../utils/printTicket';
import { playNewOrderSound, primeAudioAlerts } from '../utils/playNewOrderSound';
import {
  notificationPermission,
  requestNotificationPermission,
  showNewOrderNotification,
} from '../utils/notifyNewOrder';
import { orderLifecycleApi } from '@api/orderLifecycle';
import { ordersApi, type OrderResponse } from '@api/orders';
import type { OrderStatusResponse } from '@/types/common';
import { shopsApi } from '@api/shops';
import type { ConnectionStatus } from '@api/useSignalR';

const connectionColor: Record<ConnectionStatus, string> = {
  connected: '#16a34a',
  connecting: '#d97706',
  reconnecting: '#d97706',
  disconnected: '#dc2626',
};

// How long a newly-arrived order stays highlighted on the board (US-FP-026).
const HIGHLIGHT_MS = 8000;

export function KitchenDisplay() {
  const { t } = useTranslation('common');
  const { brandSlug, shopId } = useParams<{
    brandSlug: string;
    lang: string;
    shopId: string;
  }>();

  const resolvedBrand = brandSlug ?? '';
  const resolvedShop = shopId ?? '';
  const hasShop = resolvedBrand.length > 0 && resolvedShop.length > 0;

  const queryClient = useQueryClient();

  const { orders, isLoading, isError, connectionStatus } = useActiveOrders(
    resolvedBrand,
    resolvedShop,
  );

  // The shop's lifecycle drives which "advance" buttons each card shows. It rarely
  // changes, so fetch it once and keep it warm for the kitchen session.
  const lifecycleQuery = useQuery({
    queryKey: ['order-lifecycle', resolvedBrand, resolvedShop],
    queryFn: () => orderLifecycleApi.get(resolvedBrand, resolvedShop),
    enabled: hasShop,
    staleTime: 5 * 60_000,
  });

  // The shop's notification settings gate each new-order channel (US-FP-026 /
  // US-FP-028). Each is independent and any combination can be active at once.
  const shopQuery = useQuery({
    queryKey: ['shop', resolvedBrand, resolvedShop],
    queryFn: () => shopsApi.get(resolvedBrand, resolvedShop),
    enabled: hasShop,
    staleTime: 5 * 60_000,
  });
  const highlightEnabled = shopQuery.data?.kitchenDisplayEnabled === true;
  const autoPrintEnabled = shopQuery.data?.ticketPrinterEnabled === true;
  const pushEnabled = shopQuery.data?.pushNotificationEnabled === true;
  const soundEnabled = shopQuery.data?.soundAlertEnabled === true;

  // Reprints (and auto-prints) a single order's kitchen ticket via a hidden iframe.
  const printOrder = useCallback(
    (order: OrderResponse) => {
      printTicket(order, {
        heading: t('pos.kitchen.ticket.heading'),
        orderType: t(`pos.kitchen.orderType.${order.orderType}`),
        table: t('pos.kitchen.ticket.table'),
        timeSlot: t('pos.kitchen.ticket.timeSlot'),
        placedAt: t('pos.kitchen.ticket.placedAt'),
        customer: t('pos.kitchen.ticket.customer'),
      });
    },
    [t],
  );

  const notifyOrder = useCallback(
    (order: OrderResponse) => {
      showNewOrderNotification(
        t('pos.kitchen.newOrderTitle'),
        t('pos.kitchen.newOrderBody', { number: order.orderNumber }),
        order.id,
      );
    },
    [t],
  );

  // Newly-arrived order ids currently highlighted on the board (US-FP-026); each
  // clears itself after HIGHLIGHT_MS. Timers are tracked so we can cancel on unmount.
  const [highlightedIds, setHighlightedIds] = useState<Set<string>>(new Set());
  const highlightTimers = useRef<Map<string, number>>(new Map());
  useEffect(() => {
    const timers = highlightTimers.current;
    return () => {
      for (const timer of timers.values()) window.clearTimeout(timer);
      timers.clear();
    };
  }, []);

  // React to orders that appear after the initial load. The first successful fetch
  // only seeds the "seen" set so the existing backlog never triggers a reaction;
  // thereafter each newly-arriving order fires every enabled channel exactly once.
  const seenOrderIds = useRef<Set<string>>(new Set());
  const seeded = useRef(false);
  useEffect(() => {
    if (orders === undefined) return;
    if (!seeded.current) {
      seenOrderIds.current = new Set(orders.map((o) => o.id));
      seeded.current = true;
      return;
    }
    const arrived: OrderResponse[] = [];
    for (const order of orders) {
      if (seenOrderIds.current.has(order.id)) continue;
      seenOrderIds.current.add(order.id);
      arrived.push(order);
      if (autoPrintEnabled) printOrder(order);
      if (pushEnabled) notifyOrder(order);
    }
    if (arrived.length === 0) return;
    // One chime per batch — several orders landing together shouldn't stack alarms.
    if (soundEnabled) playNewOrderSound();
    if (highlightEnabled) {
      setHighlightedIds((prev) => {
        const next = new Set(prev);
        for (const order of arrived) next.add(order.id);
        return next;
      });
      for (const order of arrived) {
        const existing = highlightTimers.current.get(order.id);
        if (existing !== undefined) window.clearTimeout(existing);
        const timer = window.setTimeout(() => {
          setHighlightedIds((prev) => {
            if (!prev.has(order.id)) return prev;
            const next = new Set(prev);
            next.delete(order.id);
            return next;
          });
          highlightTimers.current.delete(order.id);
        }, HIGHLIGHT_MS);
        highlightTimers.current.set(order.id, timer);
      }
    }
  }, [orders, autoPrintEnabled, pushEnabled, soundEnabled, highlightEnabled, printOrder, notifyOrder]);

  // Sound and push both need a user gesture (autoplay + permission policies), so
  // staff arm them once via a header control before any auto-fired alert.
  const [alertsArmed, setAlertsArmed] = useState(false);
  const armAlerts = useCallback(async () => {
    if (soundEnabled) primeAudioAlerts();
    if (pushEnabled) await requestNotificationPermission();
    setAlertsArmed(true);
  }, [soundEnabled, pushEnabled]);
  const needsArming =
    !alertsArmed && (soundEnabled || (pushEnabled && notificationPermission() !== 'granted'));

  // Map each status name → the statuses reachable from it (sorted by lifecycle order).
  // Orders carry only the denormalised status name, so we key by name.
  const nextStatusesByName = useMemo(() => {
    const map = new Map<string, OrderStatusResponse[]>();
    const lifecycle = lifecycleQuery.data;
    if (!lifecycle) return map;

    const byId = new Map(lifecycle.statuses.map((s) => [s.id, s]));
    for (const status of lifecycle.statuses) {
      const next = lifecycle.transitions
        .filter((tr) => tr.fromStatusId === status.id)
        .map((tr) => byId.get(tr.toStatusId))
        .filter((s): s is OrderStatusResponse => s !== undefined)
        .sort((a, b) => a.sortOrder - b.sortOrder);
      map.set(status.name, next);
    }
    return map;
  }, [lifecycleQuery.data]);

  // Tracks which order's last advance attempt failed, so we surface the error on
  // that specific card rather than globally.
  const [failedOrderId, setFailedOrderId] = useState<string | null>(null);

  const advanceMutation = useMutation({
    mutationFn: ({ orderId, toStatusId }: { orderId: string; toStatusId: string }) =>
      ordersApi.advanceStatus(resolvedBrand, resolvedShop, orderId, toStatusId),
    onMutate: () => { setFailedOrderId(null); },
    onSuccess: () => {
      void queryClient.invalidateQueries({
        queryKey: activeOrdersQueryKey(resolvedBrand, resolvedShop),
      });
    },
    onError: (_error, variables) => { setFailedOrderId(variables.orderId); },
  });

  return (
    <main
      style={{
        maxWidth: '90rem',
        margin: '0 auto',
        padding: '1rem',
        display: 'flex',
        flexDirection: 'column',
        gap: '1rem',
      }}
    >
      <header
        style={{
          display: 'flex',
          justifyContent: 'space-between',
          alignItems: 'center',
          gap: '1rem',
          flexWrap: 'wrap',
        }}
      >
        <h1 style={{ fontSize: '1.75rem', fontWeight: 800, margin: 0 }}>
          {t('pos.kitchen.title')}
        </h1>
        <div style={{ display: 'inline-flex', alignItems: 'center', gap: '0.75rem' }}>
          {needsArming && (
            <button
              type="button"
              data-testid="kitchen-enable-alerts"
              onClick={() => void armAlerts()}
              style={{
                padding: '0.375rem 0.875rem',
                background: '#2563eb',
                color: '#ffffff',
                border: 'none',
                borderRadius: '999px',
                fontSize: '0.8125rem',
                fontWeight: 700,
                cursor: 'pointer',
              }}
            >
              🔔 {t('pos.kitchen.enableAlerts')}
            </button>
          )}
          <div
            data-testid="kitchen-connection-status"
            style={{
              display: 'inline-flex',
              alignItems: 'center',
              gap: '0.5rem',
              padding: '0.375rem 0.75rem',
              background: '#f3f4f6',
              borderRadius: '999px',
              fontSize: '0.8125rem',
              fontWeight: 600,
              color: '#374151',
            }}
          >
            <span
              style={{
                width: '0.625rem',
                height: '0.625rem',
                borderRadius: '50%',
                background: connectionColor[connectionStatus],
                display: 'inline-block',
              }}
            />
            {t(`pos.kitchen.connection.${connectionStatus}`)}
          </div>
        </div>
      </header>

      {isLoading && (
        <p style={{ color: '#6b7280', margin: 0 }} data-testid="kitchen-loading">
          {t('loading')}
        </p>
      )}

      {isError && (
        <p style={{ color: '#dc2626', margin: 0 }} data-testid="kitchen-error">
          {t('error')}
        </p>
      )}

      {!isLoading && !isError && orders?.length === 0 && (
        <p
          style={{
            color: '#6b7280',
            margin: 0,
            padding: '2rem',
            textAlign: 'center',
            background: '#f9fafb',
            borderRadius: '0.75rem',
          }}
          data-testid="kitchen-empty"
        >
          {t('pos.kitchen.empty')}
        </p>
      )}

      {orders !== undefined && orders.length > 0 && (
        <section
          style={{
            display: 'grid',
            gridTemplateColumns: 'repeat(auto-fill, minmax(18rem, 1fr))',
            gap: '0.75rem',
          }}
          data-testid="kitchen-order-grid"
        >
          {orders.map((order) => (
            <KitchenOrderCard
              key={order.id}
              order={order}
              nextStatuses={nextStatusesByName.get(order.statusName) ?? []}
              onAdvance={(toStatusId) =>
                { advanceMutation.mutate({ orderId: order.id, toStatusId }); }
              }
              isAdvancing={
                advanceMutation.isPending &&
                advanceMutation.variables.orderId === order.id
              }
              advanceError={failedOrderId === order.id}
              onReprint={() => { printOrder(order); }}
              highlight={highlightedIds.has(order.id)}
            />
          ))}
        </section>
      )}
    </main>
  );
}
