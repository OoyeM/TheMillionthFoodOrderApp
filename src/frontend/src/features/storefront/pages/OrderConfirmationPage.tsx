import { useState } from 'react';
import { useParams, Link } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { useQuery } from '@tanstack/react-query';
import { ordersApi } from '@api/orders';
import type { OrderResponse } from '@api/orders';
import { useOrderUpdates } from '@api/useOrderUpdates';
import { useResolvedShop } from '../hooks/useResolvedShop';

// ---------------------------------------------------------------------------
// Query
// ---------------------------------------------------------------------------

function useOrderDetails(brandSlug: string, shopId: string, orderId: string) {
  return useQuery<OrderResponse>({
    queryKey: ['order', brandSlug, shopId, orderId],
    queryFn: () => ordersApi.getById(brandSlug, shopId, orderId),
    enabled: brandSlug.length > 0 && shopId.length > 0 && orderId.length > 0,
    staleTime: 0,
  });
}

// ---------------------------------------------------------------------------
// Order confirmation page
// ---------------------------------------------------------------------------

export function OrderConfirmationPage() {
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

  const { data: order, isLoading, isError } = useOrderDetails(
    resolvedBrandSlug,
    shop.id,
    resolvedOrderId,
  );

  // Live status tracking via SignalR
  const [liveStatus, setLiveStatus] = useState<string | null>(null);

  const { status: signalRStatus } = useOrderUpdates({
    orderId: resolvedOrderId,
    onStatusChange: (update) => {
      setLiveStatus(update.newStatus);
    },
  });

  const displayStatus = liveStatus ?? order?.statusName;

  function formatCurrency(amount: number): string {
    return new Intl.NumberFormat('nl-BE', { style: 'currency', currency: 'EUR' }).format(amount);
  }

  if (isLoading) {
    return (
      <main style={{ maxWidth: '40rem', margin: '0 auto', padding: '1.5rem 1rem' }}>
        <p style={{ color: '#6b7280' }}>{t('loading')}</p>
      </main>
    );
  }

  if (isError || !order) {
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

  return (
    <main style={{ maxWidth: '40rem', margin: '0 auto', padding: '1.5rem 1rem' }}>
      {/* Thank you header */}
      <div
        style={{
          textAlign: 'center',
          padding: '2rem 1rem',
          marginBottom: '2rem',
          background: '#f0fdf4',
          borderRadius: '0.75rem',
          border: '1px solid #bbf7d0',
        }}
      >
        <p style={{ fontSize: '2rem', margin: '0 0 0.5rem' }}>&#10003;</p>
        <h1
          style={{ fontSize: '1.5rem', fontWeight: 800, color: '#166534', margin: '0 0 0.5rem' }}
        >
          {t('storefront.order.confirmed')}
        </h1>
        <p style={{ color: '#166534', margin: 0, fontSize: '0.9375rem' }}>
          {t('storefront.order.preparing')}
        </p>
        <div style={{ marginTop: '1rem' }}>
          <Link
            to="./track"
            style={{
              display: 'inline-block',
              padding: '0.5rem 1.25rem',
              background: 'var(--brand-color-primary, #111827)',
              color: '#fff',
              borderRadius: '0.5rem',
              fontWeight: 700,
              fontSize: '0.9375rem',
              textDecoration: 'none',
            }}
          >
            {t('storefront.tracking.title')}
          </Link>
        </div>
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
        <InfoCard label={t('storefront.order.orderNumber')} value={`#${order.orderNumber}`} />
        <InfoCard label={t('storefront.order.orderType')} value={order.orderType} />
        {order.customerName && (
          <InfoCard label={t('storefront.order.customerName')} value={order.customerName} />
        )}
        <InfoCard
          label={t('storefront.order.status')}
          value={displayStatus ?? ''}
          highlight
          signalRStatus={signalRStatus}
        />
        <InfoCard
          label={t('storefront.checkout.payment.label')}
          value={
            order.paymentMethod === 'CreditCard' || order.paymentMethod === 'Bancontact'
              ? t('storefront.checkout.payment.statusPaid')
              : t('storefront.checkout.payment.statusPayAtPickup')
          }
        />
        {order.timeSlot && (
          <InfoCard
            label={t('storefront.checkout.timeSlotLegend')}
            value={t('storefront.order.timeSlot', { time: order.timeSlot })}
          />
        )}
      </div>

      {/* Items */}
      <div
        style={{
          border: '1px solid #e5e7eb',
          borderRadius: '0.5rem',
          overflow: 'hidden',
          marginBottom: '1.5rem',
        }}
      >
        <div
          style={{
            padding: '0.75rem 1rem',
            background: '#f9fafb',
            borderBottom: '1px solid #e5e7eb',
            fontWeight: 700,
            fontSize: '0.9375rem',
            color: '#111827',
          }}
        >
          {t('storefront.order.items')}
        </div>
        <div style={{ padding: '0.75rem 1rem' }}>
          {order.items.map((item, idx) => (
            <div
              key={`${item.productId}-${String(idx)}`}
              style={{
                display: 'flex',
                justifyContent: 'space-between',
                gap: '1rem',
                padding: '0.5rem 0',
                borderBottom: idx < order.items.length - 1 ? '1px solid #f3f4f6' : 'none',
              }}
            >
              <div style={{ flex: 1 }}>
                <p style={{ margin: 0, fontSize: '0.9375rem', color: '#111827', fontWeight: 600 }}>
                  {item.quantity}&times; {item.productName}
                </p>
                {item.selectedModifiers.length > 0 && (
                  <p style={{ margin: '0.25rem 0 0', fontSize: '0.8125rem', color: '#6b7280' }}>
                    {item.selectedModifiers.map((m) => m.modifierName).join(', ')}
                  </p>
                )}
                <p style={{ margin: '0.125rem 0 0', fontSize: '0.8125rem', color: '#6b7280' }}>
                  {formatCurrency(item.unitGrossPrice)} {t('storefront.order.each')}
                </p>
              </div>
              <span
                style={{
                  fontWeight: 700,
                  fontSize: '0.9375rem',
                  color: '#111827',
                  whiteSpace: 'nowrap',
                }}
              >
                {formatCurrency(item.lineTotal)}
              </span>
            </div>
          ))}
        </div>
      </div>

      {/* Totals */}
      <div
        style={{
          border: '1px solid #e5e7eb',
          borderRadius: '0.5rem',
          padding: '1rem',
          marginBottom: '2rem',
        }}
      >
        <TotalRow label={t('storefront.order.subtotal')} value={formatCurrency(order.subtotalGross)} />
        <TotalRow
          label={t('storefront.order.vat', { rate: order.vatRatePercent })}
          value={formatCurrency(order.totalVatAmount)}
        />
        <TotalRow
          label={t('storefront.order.total')}
          value={formatCurrency(order.totalGross)}
          bold
        />
      </div>

      {/* Back to home */}
      <div style={{ textAlign: 'center' }}>
        <Link
          to={`/${resolvedBrandSlug}/${String(lang)}`}
          style={{
            color: 'var(--brand-color-primary, #111827)',
            fontWeight: 700,
            fontSize: '0.9375rem',
          }}
        >
          {t('storefront.order.backToHome')}
        </Link>
      </div>
    </main>
  );
}

// ---------------------------------------------------------------------------
// Small reusable sub-components
// ---------------------------------------------------------------------------

interface InfoCardProps {
  label: string;
  value: string;
  highlight?: boolean;
  signalRStatus?: string;
}

function InfoCard({ label, value, highlight, signalRStatus }: InfoCardProps) {
  return (
    <div
      style={{
        padding: '0.875rem 1rem',
        borderRadius: '0.5rem',
        border: `1px solid ${highlight ? 'var(--brand-color-primary, #111827)' : '#e5e7eb'}`,
        background: highlight ? '#f9fafb' : '#fff',
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
        {highlight && signalRStatus === 'connected' && (
          <span
            style={{
              display: 'inline-block',
              width: '0.5rem',
              height: '0.5rem',
              borderRadius: '50%',
              background: '#22c55e',
              marginLeft: '0.375rem',
              verticalAlign: 'middle',
            }}
            title="Live updates active"
          />
        )}
      </p>
      <p style={{ margin: 0, fontSize: '1rem', fontWeight: 700, color: '#111827' }}>{value}</p>
    </div>
  );
}

interface TotalRowProps {
  label: string;
  value: string;
  bold?: boolean;
}

function TotalRow({ label, value, bold }: TotalRowProps) {
  return (
    <div
      style={{
        display: 'flex',
        justifyContent: 'space-between',
        padding: '0.375rem 0',
        borderTop: bold ? '1px solid #e5e7eb' : 'none',
        marginTop: bold ? '0.5rem' : 0,
        paddingTop: bold ? '0.625rem' : '0.375rem',
        fontSize: bold ? '1rem' : '0.9375rem',
        fontWeight: bold ? 700 : 400,
        color: '#111827',
      }}
    >
      <span>{label}</span>
      <span>{value}</span>
    </div>
  );
}
