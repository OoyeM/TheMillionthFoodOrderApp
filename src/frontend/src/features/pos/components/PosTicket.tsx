import { useTranslation } from 'react-i18next';
import type { PosOrderAction, PosOrderState, PosTicketItem } from '../hooks/usePosOrder';
import { OrderTypeSelector } from './OrderTypeSelector';
import type { OrderType } from '@api/orders';

interface PosTicketProps {
  state: PosOrderState;
  subtotal: number;
  dispatch: React.Dispatch<PosOrderAction>;
  onPlaceOrder: () => void;
  isSubmitting: boolean;
  submitError: string | null;
}

/**
 * Right-pane ticket panel for the POS new-order page.
 * Displays the current order items with +/- controls, order type selector,
 * conditional table-number input (eat-in only), optional customer name,
 * subtotal, and the primary "Place order" button.
 */
export function PosTicket({
  state,
  subtotal,
  dispatch,
  onPlaceOrder,
  isSubmitting,
  submitError,
}: PosTicketProps) {
  const { t } = useTranslation('common');

  const formattedSubtotal = new Intl.NumberFormat('nl-BE', {
    style: 'currency',
    currency: 'EUR',
  }).format(subtotal);

  const canSubmit =
    state.items.length > 0 &&
    !isSubmitting &&
    (state.orderType !== 'EatIn' || state.tableNumber.trim().length > 0);

  return (
    <aside
      data-testid="pos-ticket"
      style={{
        display: 'flex',
        flexDirection: 'column',
        gap: '1rem',
        background: '#f9fafb',
        borderLeft: '1px solid #e5e7eb',
        padding: '1rem',
        overflowY: 'auto',
      }}
    >
      {/* Header */}
      <h2 style={{ margin: 0, fontSize: '1.125rem', fontWeight: 700, color: '#111827' }}>
        {t('pos.order.ticket')}
      </h2>

      {/* Items */}
      {state.items.length === 0 ? (
        <p style={{ color: '#9ca3af', fontSize: '0.875rem', margin: 0 }}>
          {t('pos.order.empty')}
        </p>
      ) : (
        <ul style={{ listStyle: 'none', margin: 0, padding: 0, display: 'flex', flexDirection: 'column', gap: '0.5rem' }}>
          {state.items.map((item) => (
            <TicketLineItem key={item.key} item={item} dispatch={dispatch} />
          ))}
        </ul>
      )}

      <hr style={{ border: 'none', borderTop: '1px solid #e5e7eb', margin: '0.25rem 0' }} />

      {/* Order type */}
      <div>
        <label
          style={{ display: 'block', fontWeight: 600, fontSize: '0.875rem', color: '#374151', marginBottom: '0.5rem' }}
        >
          {t('pos.order.orderType')}
        </label>
        <OrderTypeSelector
          value={state.orderType as OrderType}
          onChange={(type) =>
            dispatch({ type: 'SET_ORDER_TYPE', payload: { orderType: type } })
          }
        />
      </div>

      {/* Table number — eat-in only */}
      {state.orderType === 'EatIn' && (
        <div>
          <label
            htmlFor="pos-table-number"
            style={{ display: 'block', fontWeight: 600, fontSize: '0.875rem', color: '#374151', marginBottom: '0.375rem' }}
          >
            {t('pos.order.tableNumberLabel')}
            <span style={{ color: '#dc2626', marginLeft: '0.25rem' }}>*</span>
          </label>
          <input
            id="pos-table-number"
            data-testid="pos-table-number-input"
            type="text"
            inputMode="numeric"
            value={state.tableNumber}
            onChange={(e) =>
              dispatch({
                type: 'SET_TABLE_NUMBER',
                payload: { tableNumber: e.target.value.slice(0, 20) },
              })
            }
            placeholder={t('pos.order.tableNumberPlaceholder')}
            style={{
              width: '100%',
              padding: '0.625rem 0.75rem',
              border: '1px solid #d1d5db',
              borderRadius: '0.5rem',
              fontSize: '1rem',
              background: '#fff',
              color: '#111827',
              minHeight: '2.75rem',
              boxSizing: 'border-box',
            }}
          />
        </div>
      )}

      {/* Customer name (optional) */}
      <div>
        <label
          htmlFor="pos-customer-name"
          style={{ display: 'block', fontWeight: 600, fontSize: '0.875rem', color: '#374151', marginBottom: '0.375rem' }}
        >
          {t('pos.order.customerNameLabel')}
          <span style={{ color: '#9ca3af', marginLeft: '0.25rem', fontWeight: 400 }}>
            ({t('storefront.checkout.optional')})
          </span>
        </label>
        <input
          id="pos-customer-name"
          type="text"
          value={state.customerName}
          onChange={(e) =>
            dispatch({ type: 'SET_CUSTOMER_NAME', payload: { customerName: e.target.value } })
          }
          placeholder={t('pos.order.customerNamePlaceholder')}
          style={{
            width: '100%',
            padding: '0.625rem 0.75rem',
            border: '1px solid #d1d5db',
            borderRadius: '0.5rem',
            fontSize: '1rem',
            background: '#fff',
            color: '#111827',
            minHeight: '2.75rem',
            boxSizing: 'border-box',
          }}
        />
      </div>

      <hr style={{ border: 'none', borderTop: '1px solid #e5e7eb', margin: '0.25rem 0' }} />

      {/* Subtotal */}
      <div
        style={{
          display: 'flex',
          justifyContent: 'space-between',
          alignItems: 'center',
          fontWeight: 700,
          fontSize: '1.0625rem',
          color: '#111827',
        }}
      >
        <span>{t('pos.order.subtotal')}</span>
        <span data-testid="pos-subtotal">{formattedSubtotal}</span>
      </div>

      {/* Submit error */}
      {submitError && (
        <p style={{ color: '#dc2626', fontSize: '0.875rem', margin: 0 }}>{submitError}</p>
      )}

      {/* Place order button — full width, 56px tall */}
      <button
        type="button"
        data-testid="pos-place-order-btn"
        disabled={!canSubmit}
        onClick={onPlaceOrder}
        style={{
          width: '100%',
          minHeight: '3.5rem', // 56px
          padding: '0.75rem 1rem',
          borderRadius: '0.75rem',
          border: 'none',
          background: canSubmit ? '#111827' : '#d1d5db',
          color: canSubmit ? '#fff' : '#9ca3af',
          fontWeight: 700,
          fontSize: '1.0625rem',
          cursor: canSubmit ? 'pointer' : 'not-allowed',
          transition: 'background 0.15s ease',
          marginTop: 'auto',
        }}
      >
        {isSubmitting ? t('pos.order.placing') : t('pos.order.placeOrder')}
      </button>
    </aside>
  );
}

// ---------------------------------------------------------------------------
// Ticket line item sub-component
// ---------------------------------------------------------------------------

function TicketLineItem({
  item,
  dispatch,
}: {
  item: PosTicketItem;
  dispatch: React.Dispatch<PosOrderAction>;
}) {
  const formattedLineTotal = new Intl.NumberFormat('nl-BE', {
    style: 'currency',
    currency: 'EUR',
  }).format(item.unitGrossPrice * item.quantity);

  return (
    <li
      style={{
        background: '#fff',
        border: '1px solid #e5e7eb',
        borderRadius: '0.5rem',
        padding: '0.625rem 0.75rem',
      }}
    >
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start', gap: '0.5rem' }}>
        <span style={{ fontSize: '0.9375rem', fontWeight: 600, color: '#111827', flex: 1 }}>
          {item.productName}
        </span>
        <span style={{ fontSize: '0.9375rem', fontWeight: 600, color: '#374151', whiteSpace: 'nowrap' }}>
          {formattedLineTotal}
        </span>
      </div>

      {/* Modifier list */}
      {item.selectedModifiers.length > 0 && (
        <ul style={{ listStyle: 'none', margin: '0.25rem 0 0', padding: 0 }}>
          {item.selectedModifiers.map((m) => (
            <li key={m.modifierId} style={{ fontSize: '0.8125rem', color: '#6b7280' }}>
              + {m.modifierName}
              {m.priceAdjustment !== 0 &&
                ` (+${new Intl.NumberFormat('nl-BE', { style: 'currency', currency: 'EUR' }).format(m.priceAdjustment)})`}
            </li>
          ))}
        </ul>
      )}

      {/* Quantity stepper */}
      <div
        style={{
          display: 'flex',
          alignItems: 'center',
          gap: '0.5rem',
          marginTop: '0.5rem',
        }}
      >
        <button
          type="button"
          aria-label="Hoeveelheid verlagen"
          onClick={() =>
            dispatch({
              type: 'UPDATE_QUANTITY',
              payload: { key: item.key, quantity: item.quantity - 1 },
            })
          }
          style={{
            width: '2.75rem', // 44px
            height: '2.75rem',
            borderRadius: '0.375rem',
            border: '1px solid #d1d5db',
            background: '#f3f4f6',
            color: '#374151',
            fontSize: '1.25rem',
            fontWeight: 700,
            cursor: 'pointer',
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'center',
          }}
        >
          −
        </button>
        <span
          style={{
            minWidth: '2rem',
            textAlign: 'center',
            fontSize: '1rem',
            fontWeight: 700,
            color: '#111827',
          }}
          data-testid={`qty-${item.key}`}
        >
          {item.quantity}
        </span>
        <button
          type="button"
          aria-label="Hoeveelheid verhogen"
          onClick={() =>
            dispatch({
              type: 'UPDATE_QUANTITY',
              payload: { key: item.key, quantity: item.quantity + 1 },
            })
          }
          style={{
            width: '2.75rem',
            height: '2.75rem',
            borderRadius: '0.375rem',
            border: '1px solid #d1d5db',
            background: '#f3f4f6',
            color: '#374151',
            fontSize: '1.25rem',
            fontWeight: 700,
            cursor: 'pointer',
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'center',
          }}
        >
          +
        </button>
      </div>
    </li>
  );
}
