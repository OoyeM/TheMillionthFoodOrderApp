// Order tracking page — shows the shop's configured lifecycle as a visual
// stepper and subscribes to real-time status updates via SignalR.
// Accessible to guests (no RequireAuth) via /:brandSlug/:lang/:shopSlug/order/:orderId/track.

import { useState } from 'react';
import { useParams, Link } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { useQuery } from '@tanstack/react-query';
import { ordersApi } from '@api/orders';
import type { OrderTrackingResponse } from '@api/orders';
import { useOrderUpdates } from '@api/useOrderUpdates';
import { OrderStatusStepper } from '../components/OrderStatusStepper';
import { useResolvedShop } from '../hooks/useResolvedShop';

// ---------------------------------------------------------------------------
// Query hook
// ---------------------------------------------------------------------------

function useOrderTracking(brandSlug: string, shopId: string, orderId: string) {
  return useQuery<OrderTrackingResponse>({
    queryKey: ['order-tracking', brandSlug, shopId, orderId],
    queryFn: () => ordersApi.getTracking(brandSlug, shopId, orderId),
    enabled: brandSlug.length > 0 && shopId.length > 0 && orderId.length > 0,
    staleTime: 0,
  });
}

// ---------------------------------------------------------------------------
// OrderTrackingPage
// ---------------------------------------------------------------------------

export function OrderTrackingPage() {
  const { t } = useTranslation('common');
  const { brandSlug, lang, orderId } = useParams<{
    brandSlug: string;
    lang: string;
    orderId: string;
  }>();

  // shopId comes from the resolved ShopContext (set by ShopResolver layout route).
  const shop = useResolvedShop();

  const resolvedBrandSlug = brandSlug ?? '';
  const resolvedOrderId = orderId ?? '';

  const { data, isLoading, isError } = useOrderTracking(
    resolvedBrandSlug,
    shop.id,
    resolvedOrderId,
  );

  // Seed live status from the initial fetch; updated by SignalR events.
  const [currentStatusName, setCurrentStatusName] = useState<string | null>(null);

  const { status: signalRStatus } = useOrderUpdates({
    orderId: resolvedOrderId,
    onStatusChange: (update) => {
      setCurrentStatusName(update.newStatus);
    },
  });

  const displayStatus = currentStatusName ?? data?.order.statusName ?? '';
  const isConnected = signalRStatus === 'connected';

  // ---------------------------------------------------------------------------
  // Loading state
  // ---------------------------------------------------------------------------

  if (isLoading) {
    return (
      <main style={{ maxWidth: '40rem', margin: '0 auto', padding: '1.5rem 1rem' }}>
        <p style={{ color: '#6b7280' }}>{t('loading')}</p>
      </main>
    );
  }

  // ---------------------------------------------------------------------------
  // Error state
  // ---------------------------------------------------------------------------

  if (isError || !data) {
    return (
      <main style={{ maxWidth: '40rem', margin: '0 auto', padding: '1.5rem 1rem' }}>
        <p style={{ color: '#ef4444' }}>{t('error')}</p>
        <Link
          to={`/${resolvedBrandSlug}/${String(lang)}`}
          style={{ color: 'var(--brand-color-primary, #111827)', fontWeight: 600 }}
        >
          {t('storefront.order.backToHome')}
        </Link>
      </main>
    );
  }

  const { order, lifecycle } = data;

  // Sort lifecycle statuses by sortOrder for the stepper.
  const sortedStatuses = [...lifecycle.statuses].sort((a, b) => a.sortOrder - b.sortOrder);

  // ---------------------------------------------------------------------------
  // Render
  // ---------------------------------------------------------------------------

  return (
    <main style={{ maxWidth: '40rem', margin: '0 auto', padding: '1.5rem 1rem' }}>
      {/* Page header */}
      <div style={{ marginBottom: '1.5rem' }}>
        <Link
          to=".."
          relative="path"
          style={{
            display: 'inline-flex',
            alignItems: 'center',
            gap: '0.25rem',
            color: 'var(--brand-color-primary, #111827)',
            fontSize: '0.875rem',
            fontWeight: 600,
            textDecoration: 'none',
            marginBottom: '1rem',
          }}
        >
          &#8592; {t('storefront.tracking.backToConfirmation')}
        </Link>

        <h1
          style={{
            fontSize: '1.5rem',
            fontWeight: 800,
            color: '#111827',
            margin: '0 0 0.25rem',
          }}
        >
          {t('storefront.tracking.title')}
        </h1>

        <p style={{ margin: 0, fontSize: '0.9375rem', color: '#6b7280' }}>
          {t('storefront.tracking.orderNumber')}
          {order.orderNumber}
        </p>
      </div>

      {/* SignalR connection indicator + current status */}
      <div
        style={{
          display: 'flex',
          alignItems: 'center',
          justifyContent: 'space-between',
          padding: '0.75rem 1rem',
          background: '#f9fafb',
          borderRadius: '0.5rem',
          border: '1px solid #e5e7eb',
          marginBottom: '1.5rem',
        }}
      >
        <span style={{ fontSize: '0.9375rem', fontWeight: 700, color: '#111827' }}>
          {displayStatus}
        </span>

        <span
          style={{
            display: 'flex',
            alignItems: 'center',
            gap: '0.375rem',
            fontSize: '0.75rem',
            fontWeight: 600,
            color: isConnected ? '#16a34a' : '#6b7280',
          }}
        >
          {/* Live indicator dot */}
          <span
            style={{
              display: 'inline-block',
              width: '0.5rem',
              height: '0.5rem',
              borderRadius: '50%',
              background: isConnected ? '#22c55e' : '#9ca3af',
            }}
            aria-hidden="true"
          />
          {isConnected
            ? t('storefront.tracking.statusLive')
            : t('storefront.tracking.connecting')}
        </span>
      </div>

      {/* Order lifecycle stepper */}
      <div
        style={{
          border: '1px solid #e5e7eb',
          borderRadius: '0.75rem',
          padding: '1.25rem 1rem 0.5rem',
          marginBottom: '1.5rem',
          overflowX: 'hidden',
        }}
      >
        <OrderStatusStepper statuses={sortedStatuses} currentStatusName={displayStatus} />
      </div>

      {/* Order meta */}
      <div
        style={{
          display: 'grid',
          gridTemplateColumns: '1fr 1fr',
          gap: '0.75rem',
          marginBottom: '1.5rem',
        }}
      >
        <MetaCard label={t('storefront.order.orderType')} value={order.orderType} />
        {order.customerName && (
          <MetaCard label={t('storefront.order.customerName')} value={order.customerName} />
        )}
      </div>
    </main>
  );
}

// ---------------------------------------------------------------------------
// Small reusable sub-component
// ---------------------------------------------------------------------------

interface MetaCardProps {
  label: string;
  value: string;
}

function MetaCard({ label, value }: MetaCardProps) {
  return (
    <div
      style={{
        padding: '0.875rem 1rem',
        borderRadius: '0.5rem',
        border: '1px solid #e5e7eb',
        background: '#fff',
      }}
    >
      <p
        style={{
          margin: '0 0 0.25rem',
          fontSize: '0.75rem',
          fontWeight: 600,
          color: '#6b7280',
          textTransform: 'uppercase',
          letterSpacing: '0.05em',
        }}
      >
        {label}
      </p>
      <p style={{ margin: 0, fontSize: '1rem', fontWeight: 700, color: '#111827' }}>{value}</p>
    </div>
  );
}
