import { useTranslation } from 'react-i18next';
import type { ProductListItem } from '../../../types/common';

// ---------------------------------------------------------------------------
// ComponentProductPicker
//
// Renders the scrollable list of available (unselected) simple products that
// can be added to a combo. When all products are selected it shows a message.
// When no simple products exist at all it shows an empty-state message.
// ---------------------------------------------------------------------------

export interface ComponentProductPickerProps {
  simpleProducts: ProductListItem[];
  selectedIds: string[];
  onAdd: (product: ProductListItem) => void;
}

export function ComponentProductPicker({
  simpleProducts,
  selectedIds,
  onAdd,
}: ComponentProductPickerProps): JSX.Element | null {
  const { t } = useTranslation();

  if (simpleProducts.length === 0) return null;

  const available = simpleProducts.filter((p) => !selectedIds.includes(p.id));

  return (
    <div
      style={{
        border: '1px solid #e5e7eb',
        borderRadius: '0.375rem',
        maxHeight: '12rem',
        overflowY: 'auto',
      }}
    >
      {available.map((product) => (
        <div
          key={product.id}
          onClick={() => onAdd(product)}
          style={{
            display: 'flex',
            alignItems: 'center',
            gap: '0.5rem',
            padding: '0.5rem 0.75rem',
            cursor: 'pointer',
            borderBottom: '1px solid #f3f4f6',
          }}
          onMouseEnter={(e) => {
            (e.currentTarget as HTMLElement).style.background = '#f9fafb';
          }}
          onMouseLeave={(e) => {
            (e.currentTarget as HTMLElement).style.background = 'transparent';
          }}
        >
          <span style={{ flex: 1, fontSize: '0.875rem' }}>{product.name}</span>
          <span style={{ fontSize: '0.75rem', color: '#6b7280', fontFamily: 'monospace' }}>
            {'€'} {product.basePrice.amount.toFixed(2)}
          </span>
          <span style={{ color: '#9ca3af', fontSize: '0.875rem' }}>+ Add</span>
        </div>
      ))}
      {available.length === 0 && (
        <p style={{ padding: '0.75rem', color: '#9ca3af', fontSize: '0.875rem' }}>
          {t('admin.comboProducts.allProductsSelected')}
        </p>
      )}
    </div>
  );
}
