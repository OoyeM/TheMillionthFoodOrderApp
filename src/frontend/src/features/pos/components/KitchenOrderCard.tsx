import { useTranslation } from 'react-i18next';
import type { OrderResponse, OrderType } from '@api/orders';

interface KitchenOrderCardProps {
  order: OrderResponse;
}

const orderTypeColor: Record<OrderType, string> = {
  Pickup: '#2563eb',
  EatIn: '#9333ea',
  Delivery: '#059669',
};

function formatTime(iso: string): string {
  const d = new Date(iso);
  return new Intl.DateTimeFormat('nl-BE', { hour: '2-digit', minute: '2-digit' }).format(d);
}

function formatRelative(iso: string, now: number): string {
  const elapsedMs = now - new Date(iso).getTime();
  const minutes = Math.max(0, Math.floor(elapsedMs / 60_000));
  if (minutes < 1) return '<1m';
  if (minutes < 60) return `${minutes}m`;
  const hours = Math.floor(minutes / 60);
  return `${hours}h ${minutes % 60}m`;
}

export function KitchenOrderCard({ order }: KitchenOrderCardProps) {
  const { t } = useTranslation('common');
  const typeLabel = t(`pos.kitchen.orderType.${order.orderType}`);
  const typeColor = orderTypeColor[order.orderType];
  // Capturing now once at render keeps re-renders deterministic; the page itself
  // re-renders on every SignalR invalidation, so the relative time stays fresh enough.
  const now = Date.now();

  return (
    <article
      data-testid="kitchen-order-card"
      style={{
        display: 'flex',
        flexDirection: 'column',
        gap: '0.75rem',
        padding: '1rem',
        background: '#ffffff',
        borderRadius: '0.75rem',
        border: '1px solid #e5e7eb',
        boxShadow: '0 1px 2px rgba(0,0,0,0.05)',
        minHeight: '12rem',
      }}
    >
      <header
        style={{
          display: 'flex',
          justifyContent: 'space-between',
          alignItems: 'baseline',
          gap: '0.5rem',
        }}
      >
        <h2 style={{ fontSize: '1.75rem', fontWeight: 800, margin: 0 }}>#{order.orderNumber}</h2>
        <div style={{ fontSize: '0.875rem', color: '#6b7280', textAlign: 'right' }}>
          <div style={{ fontWeight: 600 }}>{formatRelative(order.createdAt, now)}</div>
          <div>{formatTime(order.createdAt)}</div>
        </div>
      </header>

      <div style={{ display: 'flex', flexWrap: 'wrap', gap: '0.5rem', alignItems: 'center' }}>
        <span
          style={{
            display: 'inline-block',
            padding: '0.25rem 0.625rem',
            background: typeColor,
            color: '#ffffff',
            borderRadius: '999px',
            fontSize: '0.8125rem',
            fontWeight: 700,
          }}
        >
          {typeLabel}
        </span>
        {order.tableNumber != null && order.tableNumber.length > 0 && (
          <span
            data-testid="kitchen-order-table"
            style={{
              padding: '0.25rem 0.625rem',
              background: '#fef3c7',
              color: '#92400e',
              borderRadius: '999px',
              fontSize: '0.8125rem',
              fontWeight: 700,
            }}
          >
            {t('pos.kitchen.table', { number: order.tableNumber })}
          </span>
        )}
        {order.timeSlot != null && order.timeSlot.length > 0 && (
          <span
            data-testid="kitchen-order-timeslot"
            style={{
              padding: '0.25rem 0.625rem',
              background: '#e0e7ff',
              color: '#3730a3',
              borderRadius: '999px',
              fontSize: '0.8125rem',
              fontWeight: 700,
            }}
          >
            {t('pos.kitchen.timeSlot', { value: order.timeSlot })}
          </span>
        )}
        {order.customerName != null && order.customerName.length > 0 && (
          <span style={{ fontSize: '0.875rem', color: '#374151' }}>
            {t('pos.kitchen.customer', { name: order.customerName })}
          </span>
        )}
      </div>

      <ul style={{ listStyle: 'none', padding: 0, margin: 0, display: 'grid', gap: '0.375rem' }}>
        {order.items.map((item, idx) => (
          <li key={`${order.id}-${item.productId}-${idx}`}>
            <div style={{ display: 'flex', alignItems: 'baseline', gap: '0.5rem' }}>
              <span style={{ fontWeight: 700, fontSize: '1rem', minWidth: '2rem' }}>
                {item.quantity}×
              </span>
              <span style={{ fontSize: '1rem' }}>{item.productName}</span>
            </div>
            {item.selectedModifiers.length > 0 && (
              <ul
                style={{
                  listStyle: 'none',
                  margin: '0.125rem 0 0 2.5rem',
                  padding: 0,
                  display: 'grid',
                  gap: '0.125rem',
                }}
              >
                {item.selectedModifiers.map((mod) => (
                  <li key={mod.modifierId} style={{ fontSize: '0.875rem', color: '#6b7280' }}>
                    + {mod.modifierName}
                  </li>
                ))}
              </ul>
            )}
          </li>
        ))}
      </ul>
    </article>
  );
}
