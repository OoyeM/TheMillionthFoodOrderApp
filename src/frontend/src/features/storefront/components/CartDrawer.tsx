import { useTranslation } from 'react-i18next';
import { useNavigate, useParams } from 'react-router-dom';
import { useCart, type CartItem } from '../context/CartContext';

interface CartDrawerProps {
  isOpen: boolean;
  onClose: () => void;
}

/**
 * Fixed-position right-side panel showing the current cart contents.
 * Provides quantity +/- controls and line totals, with a "Go to Checkout" button.
 */
export function CartDrawer({ isOpen, onClose }: CartDrawerProps) {
  const { t } = useTranslation('common');
  const { brandSlug, lang } = useParams<{ brandSlug: string; lang: string }>();
  const navigate = useNavigate();
  const { state, updateQuantity, removeItem, cartTotal, cartItemCount, getModifierKey } = useCart();

  function handleGoToCheckout() {
    onClose();
    void navigate(`/${brandSlug}/${lang}/checkout`);
  }

  function formatCurrency(amount: number): string {
    return new Intl.NumberFormat('nl-BE', { style: 'currency', currency: 'EUR' }).format(amount);
  }

  function getLineTotal(item: CartItem): number {
    const modifierTotal = item.selectedModifiers.reduce((s, m) => s + m.priceAdjustment, 0);
    return item.quantity * (item.unitGrossPrice + modifierTotal);
  }

  return (
    <>
      {/* Backdrop — only when open */}
      {isOpen && (
        <div
          onClick={onClose}
          style={{
            position: 'fixed',
            inset: 0,
            background: 'rgba(0,0,0,0.3)',
            zIndex: 90,
          }}
        />
      )}

      {/* Drawer panel */}
      <div
        role="dialog"
        aria-modal="true"
        aria-label={t('storefront.cart.title')}
        style={{
          position: 'fixed',
          top: 0,
          right: 0,
          bottom: 0,
          width: 'min(24rem, 100vw)',
          background: '#fff',
          boxShadow: '-4px 0 24px rgba(0,0,0,0.15)',
          zIndex: 91,
          transform: isOpen ? 'translateX(0)' : 'translateX(100%)',
          transition: 'transform 0.25s ease',
          display: 'flex',
          flexDirection: 'column',
        }}
      >
        {/* Header */}
        <div
          style={{
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'space-between',
            padding: '1.25rem 1.5rem',
            borderBottom: '1px solid #e5e7eb',
          }}
        >
          <h2 style={{ margin: 0, fontSize: '1.125rem', fontWeight: 700, color: '#111827' }}>
            {t('storefront.cart.title')}
            {cartItemCount > 0 && (
              <span
                style={{
                  marginLeft: '0.5rem',
                  background: 'var(--brand-color-primary, #111827)',
                  color: '#fff',
                  borderRadius: '9999px',
                  fontSize: '0.75rem',
                  fontWeight: 700,
                  padding: '0.125rem 0.5rem',
                }}
              >
                {cartItemCount}
              </span>
            )}
          </h2>
          <button
            type="button"
            onClick={onClose}
            aria-label={t('storefront.cart.close')}
            style={{
              background: 'none',
              border: 'none',
              cursor: 'pointer',
              color: '#6b7280',
              fontSize: '1.5rem',
              lineHeight: 1,
            }}
          >
            &times;
          </button>
        </div>

        {/* Item list */}
        <div style={{ flex: 1, overflowY: 'auto', padding: '1rem 1.5rem' }}>
          {state.items.length === 0 ? (
            <p style={{ color: '#6b7280', fontSize: '0.9375rem', textAlign: 'center', marginTop: '2rem' }}>
              {t('storefront.cart.empty')}
            </p>
          ) : (
            state.items.map((item) => {
              const key = `${item.productId}-${getModifierKey(item.selectedModifiers)}`;
              const lineTotal = getLineTotal(item);

              return (
                <div
                  key={key}
                  style={{
                    paddingBottom: '1rem',
                    marginBottom: '1rem',
                    borderBottom: '1px solid #f3f4f6',
                  }}
                >
                  <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start', gap: '0.5rem' }}>
                    <span style={{ fontWeight: 600, fontSize: '0.9375rem', color: '#111827', flex: 1 }}>
                      {item.productName}
                    </span>
                    <span style={{ fontWeight: 600, fontSize: '0.9375rem', color: '#111827', whiteSpace: 'nowrap' }}>
                      {formatCurrency(lineTotal)}
                    </span>
                  </div>

                  {item.selectedModifiers.length > 0 && (
                    <p style={{ margin: '0.25rem 0 0', fontSize: '0.8125rem', color: '#6b7280' }}>
                      {item.selectedModifiers.map((m) => m.modifierName).join(', ')}
                    </p>
                  )}

                  {/* Quantity controls */}
                  <div style={{ display: 'flex', alignItems: 'center', gap: '0.5rem', marginTop: '0.5rem' }}>
                    <button
                      type="button"
                      onClick={() =>
                        updateQuantity(item.productId, item.selectedModifiers, item.quantity - 1)
                      }
                      aria-label={t('storefront.cart.decreaseQuantity')}
                      style={{
                        width: '2rem',
                        height: '2rem',
                        borderRadius: '50%',
                        border: '1px solid #d1d5db',
                        background: '#fff',
                        cursor: 'pointer',
                        fontSize: '1.125rem',
                        lineHeight: 1,
                        display: 'flex',
                        alignItems: 'center',
                        justifyContent: 'center',
                        color: '#374151',
                      }}
                    >
                      &minus;
                    </button>
                    <span style={{ fontWeight: 600, minWidth: '1.5rem', textAlign: 'center', fontSize: '0.9375rem' }}>
                      {item.quantity}
                    </span>
                    <button
                      type="button"
                      onClick={() =>
                        updateQuantity(item.productId, item.selectedModifiers, item.quantity + 1)
                      }
                      aria-label={t('storefront.cart.increaseQuantity')}
                      style={{
                        width: '2rem',
                        height: '2rem',
                        borderRadius: '50%',
                        border: '1px solid #d1d5db',
                        background: '#fff',
                        cursor: 'pointer',
                        fontSize: '1.125rem',
                        lineHeight: 1,
                        display: 'flex',
                        alignItems: 'center',
                        justifyContent: 'center',
                        color: '#374151',
                      }}
                    >
                      +
                    </button>
                    <button
                      type="button"
                      onClick={() => removeItem(item.productId, item.selectedModifiers)}
                      aria-label={t('storefront.cart.remove')}
                      style={{
                        marginLeft: 'auto',
                        background: 'none',
                        border: 'none',
                        cursor: 'pointer',
                        color: '#ef4444',
                        fontSize: '0.8125rem',
                        fontWeight: 600,
                      }}
                    >
                      {t('storefront.cart.remove')}
                    </button>
                  </div>
                </div>
              );
            })
          )}
        </div>

        {/* Footer */}
        {state.items.length > 0 && (
          <div
            style={{
              padding: '1rem 1.5rem',
              borderTop: '1px solid #e5e7eb',
            }}
          >
            <div
              style={{
                display: 'flex',
                justifyContent: 'space-between',
                marginBottom: '1rem',
                fontWeight: 700,
                fontSize: '1rem',
              }}
            >
              <span>{t('storefront.cart.subtotal')}</span>
              <span>{formatCurrency(cartTotal)}</span>
            </div>
            <button
              type="button"
              onClick={handleGoToCheckout}
              style={{
                width: '100%',
                padding: '0.75rem',
                borderRadius: '0.5rem',
                border: 'none',
                background: 'var(--brand-color-primary, #111827)',
                color: '#fff',
                fontWeight: 700,
                fontSize: '1rem',
                cursor: 'pointer',
              }}
            >
              {t('storefront.cart.goToCheckout')}
            </button>
          </div>
        )}
      </div>
    </>
  );
}
