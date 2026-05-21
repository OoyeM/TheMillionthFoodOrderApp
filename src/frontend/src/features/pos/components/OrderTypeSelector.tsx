import { useTranslation } from 'react-i18next';
import type { OrderType } from '@api/orders';

interface OrderTypeSelectorProps {
  value: OrderType;
  onChange: (type: OrderType) => void;
}

const POS_ORDER_TYPES: OrderType[] = ['Pickup', 'EatIn'];

/**
 * Segmented control for choosing Pickup or Eat-In on the POS ticket.
 * Delivery is intentionally omitted from POS (counter staff only handle in-store).
 * Buttons meet the 44px minimum hit-target spec.
 */
export function OrderTypeSelector({ value, onChange }: OrderTypeSelectorProps) {
  const { t } = useTranslation('common');

  return (
    <div
      role="group"
      aria-label={t('pos.order.orderType')}
      style={{
        display: 'flex',
        gap: '0.5rem',
      }}
    >
      {POS_ORDER_TYPES.map((type) => {
        const isSelected = value === type;
        return (
          <button
            key={type}
            type="button"
            aria-pressed={isSelected}
            onClick={() => onChange(type)}
            style={{
              flex: 1,
              minHeight: '2.75rem', // 44px
              padding: '0.5rem 0.75rem',
              borderRadius: '0.5rem',
              border: isSelected ? '2px solid #111827' : '2px solid #d1d5db',
              background: isSelected ? '#111827' : '#f9fafb',
              color: isSelected ? '#fff' : '#374151',
              fontWeight: 600,
              fontSize: '0.9375rem',
              cursor: 'pointer',
              transition: 'all 0.15s ease',
            }}
          >
            {t(`pos.kitchen.orderType.${type}`)}
          </button>
        );
      })}
    </div>
  );
}
