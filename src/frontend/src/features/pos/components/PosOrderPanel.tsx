import { useTranslation } from 'react-i18next';
import { useOrderState } from '../context/PosOrderContext';
import type { CartItem, CartModifier } from '../context/PosOrderContext';
import type { EatInSettings } from '@/types/common';

interface PosOrderPanelProps {
  /** Shop eat-in configuration — gates the eat-in toggle and table-number requirement (US-FP-066). */
  eatIn: EatInSettings;
}

/**
 * Sidebar / bottom panel showing the running POS order.
 *
 * - Lists all items with per-line qty +/- controls and line totals
 * - OrderType toggle (Pickup / EatIn) — large buttons for touch
 * - Conditional table-number input (numeric, only when EatIn)
 * - Subtotal + VAT (colour-coded by order type) + total
 * - All amounts formatted via Intl.NumberFormat nl-BE
 */
export function PosOrderPanel({ eatIn }: PosOrderPanelProps) {
  const { t } = useTranslation('common');
  const {
    state,
    totals,
    updateQuantity,
    removeItem,
    setOrderType,
    setTableNumber,
    getModifierKey,
  } = useOrderState();

  const formatCurrency = (amount: number) =>
    new Intl.NumberFormat('nl-BE', { style: 'currency', currency: 'EUR' }).format(amount);

  const vatColor = state.orderType === 'EatIn' ? '#7c3aed' : '#059669';

  // Eat-in is only offered when the shop accepts it (US-FP-066).
  const orderTypes: readonly ('Pickup' | 'EatIn')[] = eatIn.isEnabled
    ? ['Pickup', 'EatIn']
    : ['Pickup'];

  return (
    <div
      style={{
        display: 'flex',
        flexDirection: 'column',
        height: '100%',
        background: '#f9fafb',
        borderLeft: '1px solid #e5e7eb',
      }}
    >
      {/* Header */}
      <div
        style={{
          padding: '1rem 1.25rem',
          borderBottom: '1px solid #e5e7eb',
          background: '#fff',
        }}
      >
        <h2 style={{ margin: 0, fontSize: '1.125rem', fontWeight: 700, color: '#111827' }}>
          {t('pos.order.title')}
        </h2>
      </div>

      {/* Order type toggle */}
      <div style={{ padding: '0.875rem 1.25rem', borderBottom: '1px solid #e5e7eb', background: '#fff' }}>
        <p style={{ margin: '0 0 0.5rem', fontSize: '0.875rem', fontWeight: 600, color: '#374151' }}>
          {t('pos.order.orderType')}
        </p>
        <div style={{ display: 'flex', gap: '0.5rem' }}>
          {orderTypes.map((type) => {
            const isActive = state.orderType === type;
            return (
              <button
                key={type}
                type="button"
                data-testid={`order-type-${type.toLowerCase()}`}
                onClick={() => setOrderType(type)}
                aria-pressed={isActive}
                style={{
                  flex: 1,
                  padding: '0.75rem',
                  borderRadius: '0.5rem',
                  border: `2px solid ${isActive ? 'var(--brand-color-primary, #111827)' : '#e5e7eb'}`,
                  background: isActive ? 'var(--brand-color-primary, #111827)' : '#fff',
                  color: isActive ? '#fff' : '#374151',
                  fontWeight: 600,
                  fontSize: '0.9375rem',
                  cursor: 'pointer',
                  minHeight: '3rem',
                }}
              >
                {type === 'Pickup' ? t('pos.order.pickup') : t('pos.order.eatIn')}
              </button>
            );
          })}
        </div>

        {/* Table number — only shown when EatIn */}
        {state.orderType === 'EatIn' && (
          <div style={{ marginTop: '0.75rem' }}>
            <label
              htmlFor="pos-table-number"
              style={{
                display: 'block',
                fontSize: '0.875rem',
                fontWeight: 600,
                color: '#374151',
                marginBottom: '0.375rem',
              }}
            >
              {t('pos.order.tableNumber')}
              {eatIn.requiresTableNumber && (
                <span style={{ color: '#ef4444', marginLeft: '0.25rem' }}>*</span>
              )}
            </label>
            <input
              id="pos-table-number"
              data-testid="table-number-input"
              type="number"
              inputMode="numeric"
              min={1}
              value={state.tableNumber ?? ''}
              onChange={(e) => {
                const val = e.target.value;
                setTableNumber(val === '' ? undefined : parseInt(val, 10));
              }}
              placeholder={t('pos.order.tableNumberPlaceholder')}
              style={{
                width: '100%',
                padding: '0.625rem 0.875rem',
                borderRadius: '0.375rem',
                border: '1px solid #d1d5db',
                fontSize: '1rem',
                color: '#111827',
                boxSizing: 'border-box',
                minHeight: '3rem',
              }}
            />
          </div>
        )}
      </div>

      {/* Order items list */}
      <div style={{ flex: 1, overflowY: 'auto', padding: '0.75rem 1.25rem' }}>
        {state.items.length === 0 && (
          <p style={{ color: '#9ca3af', fontSize: '0.9375rem', textAlign: 'center', marginTop: '2rem' }}>
            {t('storefront.cart.empty')}
          </p>
        )}

        {state.items.map((item) => (
          <OrderLine
            key={`${item.productId}-${getModifierKey(item.selectedModifiers)}`}
            item={item}
            onIncrease={() =>
              updateQuantity(item.productId, item.selectedModifiers, item.quantity + 1)
            }
            onDecrease={() =>
              item.quantity <= 1
                ? removeItem(item.productId, item.selectedModifiers)
                : updateQuantity(item.productId, item.selectedModifiers, item.quantity - 1)
            }
            formatCurrency={formatCurrency}
          />
        ))}
      </div>

      {/* Totals */}
      <div
        style={{
          padding: '1rem 1.25rem',
          borderTop: '2px solid #e5e7eb',
          background: '#fff',
        }}
      >
        <div style={{ display: 'flex', justifyContent: 'space-between', marginBottom: '0.375rem' }}>
          <span style={{ fontSize: '0.9375rem', color: '#374151' }}>{t('pos.order.subtotal')}</span>
          <span style={{ fontSize: '0.9375rem', fontWeight: 600, color: '#111827' }}>
            {formatCurrency(totals.subtotalGross)}
          </span>
        </div>
        <div style={{ display: 'flex', justifyContent: 'space-between', marginBottom: '0.75rem' }}>
          <span style={{ fontSize: '0.9375rem', color: vatColor }}>
            {t('pos.order.vat', { rate: totals.vatPercent })}
          </span>
          <span style={{ fontSize: '0.9375rem', fontWeight: 600, color: vatColor }}>
            {formatCurrency(totals.vatAmount)}
          </span>
        </div>
        <div
          style={{
            display: 'flex',
            justifyContent: 'space-between',
            paddingTop: '0.75rem',
            borderTop: '1px solid #e5e7eb',
          }}
        >
          <span style={{ fontSize: '1.125rem', fontWeight: 700, color: '#111827' }}>
            {t('pos.order.total')}
          </span>
          <span style={{ fontSize: '1.25rem', fontWeight: 800, color: '#111827' }}>
            {formatCurrency(totals.totalGross)}
          </span>
        </div>
      </div>
    </div>
  );
}

// ---------------------------------------------------------------------------
// Single order line sub-component
// ---------------------------------------------------------------------------

interface OrderLineProps {
  item: CartItem;
  onIncrease: () => void;
  onDecrease: () => void;
  formatCurrency: (amount: number) => string;
}

function OrderLine({ item, onIncrease, onDecrease, formatCurrency }: OrderLineProps) {
  const modifierTotal = item.selectedModifiers.reduce(
    (s: number, m: CartModifier) => s + m.priceAdjustment,
    0,
  );
  const lineTotal = item.quantity * (item.unitGrossPrice + modifierTotal);

  return (
    <div
      style={{
        display: 'flex',
        gap: '0.75rem',
        alignItems: 'flex-start',
        paddingBottom: '0.75rem',
        marginBottom: '0.75rem',
        borderBottom: '1px solid #f3f4f6',
      }}
    >
      {/* Qty controls */}
      <div style={{ display: 'flex', alignItems: 'center', gap: '0.375rem', flexShrink: 0 }}>
        <button
          type="button"
          onClick={onDecrease}
          aria-label={`Decrease ${item.productName}`}
          style={{
            width: '2.25rem',
            height: '2.25rem',
            borderRadius: '0.375rem',
            border: '1px solid #d1d5db',
            background: '#fff',
            cursor: 'pointer',
            fontSize: '1.125rem',
            fontWeight: 700,
            color: '#374151',
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'center',
          }}
        >
          −
        </button>
        <span
          style={{
            minWidth: '1.75rem',
            textAlign: 'center',
            fontWeight: 700,
            fontSize: '1rem',
            color: '#111827',
          }}
        >
          {item.quantity}
        </span>
        <button
          type="button"
          onClick={onIncrease}
          aria-label={`Increase ${item.productName}`}
          style={{
            width: '2.25rem',
            height: '2.25rem',
            borderRadius: '0.375rem',
            border: '1px solid #d1d5db',
            background: '#fff',
            cursor: 'pointer',
            fontSize: '1.125rem',
            fontWeight: 700,
            color: '#374151',
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'center',
          }}
        >
          +
        </button>
      </div>

      {/* Name + modifiers */}
      <div style={{ flex: 1, minWidth: 0 }}>
        <p style={{ margin: 0, fontWeight: 600, fontSize: '0.9375rem', color: '#111827' }}>
          {item.productName}
        </p>
        {item.selectedModifiers.length > 0 && (
          <p style={{ margin: '0.125rem 0 0', fontSize: '0.8125rem', color: '#6b7280' }}>
            {item.selectedModifiers.map((m) => m.modifierName).join(', ')}
          </p>
        )}
      </div>

      {/* Line total */}
      <span
        style={{
          fontWeight: 700,
          fontSize: '0.9375rem',
          color: '#111827',
          whiteSpace: 'nowrap',
          flexShrink: 0,
        }}
      >
        {formatCurrency(lineTotal)}
      </span>
    </div>
  );
}
