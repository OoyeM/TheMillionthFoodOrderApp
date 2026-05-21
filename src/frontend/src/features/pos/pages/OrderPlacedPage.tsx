import { useEffect } from 'react';
import { useParams, useNavigate, Link } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { useQuery } from '@tanstack/react-query';
import { ordersApi } from '@api/orders';

const AUTO_REDIRECT_SECONDS = 10;

/**
 * POS order confirmation page — shown after a successful order submission.
 * Shows the order number prominently, order type, table number (if eat-in),
 * and item totals. Auto-redirects to new order after 10 seconds.
 */
export function OrderPlacedPage() {
  const { t } = useTranslation('common');
  const navigate = useNavigate();
  const { brandSlug, lang, shopId, orderId } = useParams<{
    brandSlug: string;
    lang: string;
    shopId: string;
    orderId: string;
  }>();

  const resolvedBrand = brandSlug ?? '';
  const resolvedShop = shopId ?? '';
  const resolvedLang = lang ?? 'nl';
  const resolvedOrderId = orderId ?? '';

  const { data: order } = useQuery({
    queryKey: ['pos', 'order', resolvedOrderId],
    queryFn: () => ordersApi.getById(resolvedBrand, resolvedShop, resolvedOrderId),
    enabled: resolvedOrderId.length > 0,
  });

  const newOrderPath = `/${resolvedBrand}/${resolvedLang}/pos/shops/${resolvedShop}/order`;

  // Auto-redirect after 10 seconds
  useEffect(() => {
    const timer = setTimeout(() => {
      navigate(newOrderPath);
    }, AUTO_REDIRECT_SECONDS * 1000);
    return () => clearTimeout(timer);
  }, [navigate, newOrderPath]);

  const formattedTotal = order
    ? new Intl.NumberFormat(`${resolvedLang}-BE`, { style: 'currency', currency: 'EUR' }).format(
        order.totalGross,
      )
    : null;

  return (
    <main
      style={{
        minHeight: '100dvh',
        display: 'flex',
        flexDirection: 'column',
        alignItems: 'center',
        justifyContent: 'center',
        padding: '2rem',
        background: '#f0fdf4',
        gap: '1.5rem',
        textAlign: 'center',
      }}
    >
      {/* Check mark */}
      <div
        style={{
          width: '5rem',
          height: '5rem',
          borderRadius: '50%',
          background: '#22c55e',
          display: 'flex',
          alignItems: 'center',
          justifyContent: 'center',
          fontSize: '2.5rem',
          color: '#fff',
        }}
      >
        ✓
      </div>

      <h1 style={{ margin: 0, fontSize: '1.5rem', fontWeight: 800, color: '#166534' }}>
        {t('pos.confirmation.title')}
      </h1>

      {/* Order number — prominent display */}
      {order && (
        <div
          data-testid="pos-order-number"
          style={{
            background: '#fff',
            border: '2px solid #22c55e',
            borderRadius: '1rem',
            padding: '1.5rem 3rem',
          }}
        >
          <p style={{ margin: '0 0 0.25rem', fontSize: '0.875rem', color: '#6b7280', fontWeight: 600 }}>
            {t('pos.confirmation.orderNumber')}
          </p>
          <p style={{ margin: 0, fontSize: '3rem', fontWeight: 900, color: '#111827', letterSpacing: '0.05em' }}>
            #{order.orderNumber}
          </p>
        </div>
      )}

      {/* Order details */}
      {order && (
        <div
          style={{
            background: '#fff',
            borderRadius: '0.75rem',
            padding: '1.25rem 1.5rem',
            minWidth: '18rem',
            textAlign: 'left',
            border: '1px solid #e5e7eb',
          }}
        >
          <dl style={{ margin: 0, display: 'flex', flexDirection: 'column', gap: '0.625rem' }}>
            <div style={{ display: 'flex', justifyContent: 'space-between', gap: '1rem' }}>
              <dt style={{ color: '#6b7280', fontSize: '0.875rem' }}>{t('pos.confirmation.orderType')}</dt>
              <dd style={{ margin: 0, fontWeight: 600, fontSize: '0.9375rem', color: '#111827' }}>
                {t(`pos.kitchen.orderType.${order.orderType}`)}
              </dd>
            </div>

            {order.tableNumber && (
              <div style={{ display: 'flex', justifyContent: 'space-between', gap: '1rem' }}>
                <dt style={{ color: '#6b7280', fontSize: '0.875rem' }}>{t('pos.confirmation.tableNumber')}</dt>
                <dd style={{ margin: 0, fontWeight: 600, fontSize: '0.9375rem', color: '#111827' }}>
                  {order.tableNumber}
                </dd>
              </div>
            )}

            {order.customerName && (
              <div style={{ display: 'flex', justifyContent: 'space-between', gap: '1rem' }}>
                <dt style={{ color: '#6b7280', fontSize: '0.875rem' }}>{t('pos.confirmation.customerName')}</dt>
                <dd style={{ margin: 0, fontWeight: 600, fontSize: '0.9375rem', color: '#111827' }}>
                  {order.customerName}
                </dd>
              </div>
            )}

            <div style={{ display: 'flex', justifyContent: 'space-between', gap: '1rem', borderTop: '1px solid #f3f4f6', paddingTop: '0.625rem' }}>
              <dt style={{ color: '#6b7280', fontSize: '0.875rem' }}>{t('pos.confirmation.total')}</dt>
              <dd style={{ margin: 0, fontWeight: 700, fontSize: '1.0625rem', color: '#111827' }}>
                {formattedTotal}
              </dd>
            </div>
          </dl>
        </div>
      )}

      {/* Action buttons */}
      <div style={{ display: 'flex', gap: '1rem', flexWrap: 'wrap', justifyContent: 'center' }}>
        <Link
          to={newOrderPath}
          data-testid="pos-new-order-btn"
          style={{
            display: 'inline-flex',
            alignItems: 'center',
            justifyContent: 'center',
            minHeight: '3rem',
            padding: '0.75rem 2rem',
            background: '#111827',
            color: '#fff',
            fontWeight: 700,
            fontSize: '1rem',
            borderRadius: '0.75rem',
            textDecoration: 'none',
          }}
        >
          {t('pos.confirmation.newOrder')}
        </Link>

        <button
          type="button"
          disabled
          title={t('pos.confirmation.printReceiptTooltip')}
          style={{
            minHeight: '3rem',
            padding: '0.75rem 2rem',
            background: '#f3f4f6',
            color: '#9ca3af',
            fontWeight: 600,
            fontSize: '1rem',
            borderRadius: '0.75rem',
            border: '1px solid #e5e7eb',
            cursor: 'not-allowed',
          }}
        >
          {t('pos.confirmation.printReceipt')}
        </button>
      </div>

      {/* Auto-redirect notice */}
      <p style={{ color: '#9ca3af', fontSize: '0.8125rem', margin: 0 }}>
        {t('pos.confirmation.autoRedirect', { seconds: AUTO_REDIRECT_SECONDS })}
      </p>
    </main>
  );
}
