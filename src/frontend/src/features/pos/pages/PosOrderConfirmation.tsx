import { useEffect } from 'react';
import { useLocation, useNavigate, useParams } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import type { TFunction } from 'i18next';
import type { OrderResponse } from '@api/orders';
import { printReceipt, type ReceiptLabels } from '../utils/printReceipt';

interface PosOrderConfirmationProps {
  orderNumber: string;
  onBackToMenu: () => void;
  /**
   * The placed order, when available. Present immediately after placing an order (carried
   * via router state); absent after a refresh or direct navigation. The receipt button is
   * only shown when this is present.
   */
  order?: OrderResponse | null;
  onPrintReceipt?: (() => void) | undefined;
}

/**
 * Resolves the localised, pre-resolved receipt labels for an order.
 */
function buildReceiptLabels(order: OrderResponse, t: TFunction): ReceiptLabels {
  return {
    heading: t('pos.receipt.heading'),
    vatNumber: t('pos.receipt.vatNumber'),
    orderType: t(`pos.receipt.orderType.${order.orderType}`, { defaultValue: order.orderType }),
    table: t('pos.receipt.table'),
    timeSlot: t('pos.receipt.timeSlot'),
    customer: t('pos.receipt.customer'),
    placedAt: t('pos.receipt.placedAt'),
    subtotalNet: t('pos.receipt.subtotalNet'),
    vat: t('pos.receipt.vat', { rate: order.vatRatePercent }),
    total: t('pos.receipt.total'),
    paymentMethod: t('pos.receipt.paymentMethod'),
    paymentMethodValue: t(`pos.receipt.payment.${order.paymentMethod}`, {
      defaultValue: order.paymentMethod,
    }),
  };
}

/**
 * Minimal POS-specific order confirmation page.
 * Shows the order number, a success indicator, a "Print receipt" action (US-FP-052),
 * and a "Back to Menu" button.
 *
 * The print action doubles as the reprint trigger — counter staff can click it as many
 * times as needed to reprint the customer receipt.
 */
export function PosOrderConfirmationInner({
  orderNumber,
  onBackToMenu,
  order,
  onPrintReceipt,
}: PosOrderConfirmationProps) {
  const { t } = useTranslation('common');

  return (
    <main
      style={{
        display: 'flex',
        flexDirection: 'column',
        alignItems: 'center',
        justifyContent: 'center',
        minHeight: '60vh',
        padding: '2rem',
        textAlign: 'center',
      }}
    >
      {/* Success indicator */}
      <div
        style={{
          width: '5rem',
          height: '5rem',
          borderRadius: '50%',
          background: '#d1fae5',
          display: 'flex',
          alignItems: 'center',
          justifyContent: 'center',
          marginBottom: '1.5rem',
        }}
      >
        <span style={{ fontSize: '2.5rem', lineHeight: 1 }}>✓</span>
      </div>

      <h1 style={{ fontSize: '1.75rem', fontWeight: 800, color: '#111827', margin: '0 0 0.75rem' }}>
        {t('pos.confirmation.title')}
      </h1>

      <p
        style={{ fontSize: '1.25rem', fontWeight: 600, color: '#374151', margin: '0 0 2rem' }}
        data-testid="pos-order-number"
      >
        {t('pos.confirmation.orderNumber', { number: orderNumber })}
      </p>

      <div style={{ display: 'flex', gap: '0.75rem', flexWrap: 'wrap', justifyContent: 'center' }}>
        {order != null && onPrintReceipt != null && (
          <button
            type="button"
            onClick={onPrintReceipt}
            data-testid="pos-print-receipt"
            style={{
              padding: '0.875rem 2rem',
              borderRadius: '0.5rem',
              border: '2px solid var(--brand-color-primary, #111827)',
              background: '#fff',
              color: 'var(--brand-color-primary, #111827)',
              fontWeight: 700,
              fontSize: '1.0625rem',
              cursor: 'pointer',
              minHeight: '3rem',
            }}
          >
            {t('pos.receipt.print')}
          </button>
        )}

        <button
          type="button"
          onClick={onBackToMenu}
          style={{
            padding: '0.875rem 2.5rem',
            borderRadius: '0.5rem',
            border: 'none',
            background: 'var(--brand-color-primary, #111827)',
            color: '#fff',
            fontWeight: 700,
            fontSize: '1.0625rem',
            cursor: 'pointer',
            minHeight: '3rem',
          }}
        >
          {t('pos.confirmation.back')}
        </button>
      </div>
    </main>
  );
}

/**
 * Route component for /pos/confirmation/:orderNumber
 */
export function PosOrderConfirmation() {
  const { brandSlug, lang, orderNumber } = useParams<{
    brandSlug: string;
    lang: string;
    orderNumber: string;
  }>();
  const navigate = useNavigate();
  const location = useLocation();
  const { t } = useTranslation('common');

  // The full order is carried via router state from the POS dashboard when an order is
  // placed. It is intentionally not re-fetched on refresh: the receipt button simply
  // hides when the order is unavailable.
  const order = (location.state as { order?: OrderResponse } | null)?.order ?? null;

  // Redirect to /pos dashboard if the orderNumber param is missing (e.g. direct navigation)
  useEffect(() => {
    if (!orderNumber) {
      void navigate(`/${brandSlug}/${lang}/pos`, { replace: true });
    }
  }, [orderNumber, navigate, brandSlug, lang]);

  function handleBackToMenu() {
    void navigate(`/${brandSlug}/${lang}/pos`);
  }

  function handlePrintReceipt() {
    if (order == null) return;
    printReceipt(order, buildReceiptLabels(order, t));
  }

  if (!orderNumber) {
    return null;
  }

  return (
    <PosOrderConfirmationInner
      orderNumber={orderNumber}
      onBackToMenu={handleBackToMenu}
      order={order}
      onPrintReceipt={order != null ? handlePrintReceipt : undefined}
    />
  );
}
