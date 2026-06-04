import { useEffect } from 'react';
import { useTranslation } from 'react-i18next';

// ---------------------------------------------------------------------------
// Constants
// ---------------------------------------------------------------------------

const MOCK_PAYMENT_DELAY_MS = 1500;

// ---------------------------------------------------------------------------
// Props
// ---------------------------------------------------------------------------

interface MockPaymentScreenProps {
  orderId: string;
  onComplete: () => void;
}

// ---------------------------------------------------------------------------
// MockPaymentScreen
//
// Seam component: shows a brief "processing" screen for online payment methods.
// Replace this component with a real gateway integration (Mollie, Stripe) by
// swapping onComplete to fire after the real redirect/callback completes.
// No API calls, no side effects beyond the timer.
// ---------------------------------------------------------------------------

export function MockPaymentScreen({ onComplete }: MockPaymentScreenProps) {
  const { t } = useTranslation('common');

  useEffect(() => {
    const timer = setTimeout(() => {
      onComplete();
    }, MOCK_PAYMENT_DELAY_MS);

    return () => {
      clearTimeout(timer);
    };
  }, [onComplete]);

  return (
    <main
      style={{
        display: 'flex',
        alignItems: 'center',
        justifyContent: 'center',
        minHeight: '60vh',
        padding: '1.5rem 1rem',
      }}
    >
      <div
        style={{
          maxWidth: '24rem',
          width: '100%',
          padding: '2.5rem 2rem',
          borderRadius: '0.75rem',
          border: '1px solid #e5e7eb',
          background: '#fff',
          textAlign: 'center',
          boxShadow: '0 4px 24px rgba(0,0,0,0.07)',
        }}
      >
        {/* Spinner */}
        <div
          style={{
            display: 'inline-block',
            width: '3rem',
            height: '3rem',
            borderRadius: '50%',
            border: '4px solid #e5e7eb',
            borderTopColor: 'var(--brand-color-primary, #111827)',
            animation: 'spin 0.8s linear infinite',
            marginBottom: '1.5rem',
          }}
        />
        <style>{`@keyframes spin { to { transform: rotate(360deg); } }`}</style>

        <h1
          style={{
            fontSize: '1.25rem',
            fontWeight: 800,
            color: '#111827',
            margin: '0 0 0.5rem',
          }}
        >
          {t('storefront.checkout.payment.processing')}
        </h1>
        <p
          style={{
            fontSize: '0.9375rem',
            color: '#6b7280',
            margin: 0,
          }}
        >
          {t('storefront.checkout.payment.pleaseWait')}
        </p>
      </div>
    </main>
  );
}
