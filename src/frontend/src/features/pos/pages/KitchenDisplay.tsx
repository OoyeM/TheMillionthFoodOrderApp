import { useCallback, useEffect, useMemo, useRef, useState } from 'react';
import { useParams } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { useActiveOrders, activeOrdersQueryKey } from '../hooks/useActiveOrders';
import { KitchenOrderCard } from '../components/KitchenOrderCard';
import { printTicket } from '../utils/printTicket';
import { orderLifecycleApi } from '@api/orderLifecycle';
import { ordersApi, type OrderResponse, type OrderStatusResponse } from '@api/orders';
import { shopsApi } from '@api/shops';
import type { ConnectionStatus } from '@api/useSignalR';

const connectionColor: Record<ConnectionStatus, string> = {
  connected: '#16a34a',
  connecting: '#d97706',
  reconnecting: '#d97706',
  disconnected: '#dc2626',
};

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

  // The shop's ticket-printer setting gates auto-printing (US-FP-028).
  const shopQuery = useQuery({
    queryKey: ['shop', resolvedBrand, resolvedShop],
    queryFn: () => shopsApi.get(resolvedBrand, resolvedShop),
    enabled: hasShop,
    staleTime: 5 * 60_000,
  });
  const autoPrintEnabled = shopQuery.data?.ticketPrinterEnabled === true;

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

  // Auto-print orders that appear after the initial load. The first successful
  // fetch only seeds the "seen" set so we never reprint the existing backlog;
  // thereafter any newly-arriving order prints once when the setting is enabled.
  const seenOrderIds = useRef<Set<string>>(new Set());
  const seeded = useRef(false);
  useEffect(() => {
    if (orders === undefined) return;
    if (!seeded.current) {
      seenOrderIds.current = new Set(orders.map((o) => o.id));
      seeded.current = true;
      return;
    }
    for (const order of orders) {
      if (seenOrderIds.current.has(order.id)) continue;
      seenOrderIds.current.add(order.id);
      if (autoPrintEnabled) printOrder(order);
    }
  }, [orders, autoPrintEnabled, printOrder]);

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
    onMutate: () => setFailedOrderId(null),
    onSuccess: () => {
      void queryClient.invalidateQueries({
        queryKey: activeOrdersQueryKey(resolvedBrand, resolvedShop),
      });
    },
    onError: (_error, variables) => setFailedOrderId(variables.orderId),
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

      {!isLoading && !isError && orders !== undefined && orders.length === 0 && (
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
                advanceMutation.mutate({ orderId: order.id, toStatusId })
              }
              isAdvancing={
                advanceMutation.isPending &&
                advanceMutation.variables?.orderId === order.id
              }
              advanceError={failedOrderId === order.id}
              onReprint={() => printOrder(order)}
            />
          ))}
        </section>
      )}
    </main>
  );
}
