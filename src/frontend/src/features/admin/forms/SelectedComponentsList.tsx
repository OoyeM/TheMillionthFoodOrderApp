import { useTranslation } from 'react-i18next';
import type { ProductListItem } from '../../../types/common';

// ---------------------------------------------------------------------------
// SelectedComponentsList
//
// Renders the ordered list of combo component products with up/down reorder
// buttons and a remove button for each entry.
// ---------------------------------------------------------------------------

const reorderButtonStyle: React.CSSProperties = {
  padding: '0.125rem 0.4rem',
  fontSize: '0.75rem',
  background: '#fff',
  border: '1px solid #d1d5db',
  borderRadius: '0.25rem',
  lineHeight: 1,
};

export interface SelectedComponentsListProps {
  selectedProducts: ProductListItem[];
  onMoveUp: (index: number) => void;
  onMoveDown: (index: number) => void;
  onRemove: (product: ProductListItem) => void;
}

export function SelectedComponentsList({
  selectedProducts,
  onMoveUp,
  onMoveDown,
  onRemove,
}: SelectedComponentsListProps): JSX.Element | null {
  const { t } = useTranslation();

  if (selectedProducts.length === 0) return null;

  return (
    <div style={{ marginBottom: '0.75rem' }}>
      {selectedProducts.map((product, index) => (
        <div
          key={product.id}
          style={{
            display: 'flex',
            alignItems: 'center',
            gap: '0.5rem',
            padding: '0.5rem 0.75rem',
            border: '1px solid #e5e7eb',
            borderRadius: '0.375rem',
            marginBottom: '0.375rem',
            background: '#f9fafb',
          }}
        >
          <span style={{ flex: 1, fontSize: '0.9rem', fontWeight: 500 }}>
            {product.name}
          </span>
          <span style={{ fontSize: '0.75rem', color: '#6b7280', fontFamily: 'monospace' }}>
            {'€'} {product.basePrice.amount.toFixed(2)}
          </span>
          <button
            type="button"
            onClick={() => onMoveUp(index)}
            disabled={index === 0}
            style={{
              ...reorderButtonStyle,
              opacity: index === 0 ? 0.3 : 1,
              cursor: index === 0 ? 'not-allowed' : 'pointer',
            }}
          >
            &#9650;
          </button>
          <button
            type="button"
            onClick={() => onMoveDown(index)}
            disabled={index === selectedProducts.length - 1}
            style={{
              ...reorderButtonStyle,
              opacity: index === selectedProducts.length - 1 ? 0.3 : 1,
              cursor: index === selectedProducts.length - 1 ? 'not-allowed' : 'pointer',
            }}
          >
            &#9660;
          </button>
          <button
            type="button"
            onClick={() => onRemove(product)}
            style={{
              padding: '0.125rem 0.5rem',
              fontSize: '0.75rem',
              background: '#fff',
              border: '1px solid #fca5a5',
              borderRadius: '0.25rem',
              color: '#dc2626',
              cursor: 'pointer',
            }}
          >
            {t('admin.comboProducts.remove')}
          </button>
        </div>
      ))}
    </div>
  );
}
