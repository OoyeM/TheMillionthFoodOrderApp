import { useEffect } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { useTranslation } from 'react-i18next';

interface PosOrderConfirmationProps {
  orderNumber: string;
  onBackToMenu: () => void;
}

/**
 * Minimal POS-specific order confirmation page.
 * Shows the order number, a success indicator, and a "Back to Menu" button.
 *
 * This is intentionally simple — no storefront OrderConfirmationPage complexity
 * (no SignalR tracking, no payment status) since the staff already knows the order
 * was placed successfully.
 */
export function PosOrderConfirmationInner({ orderNumber, onBackToMenu }: PosOrderConfirmationProps) {
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

  // Redirect to /pos dashboard if the orderNumber param is missing (e.g. direct navigation)
  useEffect(() => {
    if (!orderNumber) {
      void navigate(`/${brandSlug}/${lang}/pos`, { replace: true });
    }
  }, [orderNumber, navigate, brandSlug, lang]);

  function handleBackToMenu() {
    void navigate(`/${brandSlug}/${lang}/pos`);
  }

  if (!orderNumber) {
    return null;
  }

  return (
    <PosOrderConfirmationInner
      orderNumber={orderNumber}
      onBackToMenu={handleBackToMenu}
    />
  );
}
