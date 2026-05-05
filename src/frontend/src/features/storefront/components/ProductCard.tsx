import { useTranslation } from 'react-i18next';
import type { ProductListItem } from '@/types/common';

interface ProductCardProps {
  product: ProductListItem;
  onAdd: (product: ProductListItem) => void;
}

/**
 * Displays a single product in the storefront menu.
 * Shows the product name, formatted price, and an "Add" button.
 *
 * For products with modifier groups, the parent (MenuPage) opens a ModifierModal
 * when the add button is clicked.
 */
export function ProductCard({ product, onAdd }: ProductCardProps) {
  const { t } = useTranslation('common');

  const formattedPrice = new Intl.NumberFormat('nl-BE', {
    style: 'currency',
    currency: product.basePrice.currency || 'EUR',
  }).format(product.basePrice.amount);

  return (
    <div
      style={{
        display: 'flex',
        alignItems: 'center',
        gap: '1rem',
        padding: '1rem',
        borderRadius: '0.5rem',
        border: '1px solid #e5e7eb',
        background: '#fff',
        marginBottom: '0.75rem',
      }}
    >
      {product.imageUrl && (
        <img
          src={product.imageUrl}
          alt={product.name}
          style={{
            width: '4.5rem',
            height: '4.5rem',
            objectFit: 'cover',
            borderRadius: '0.375rem',
            flexShrink: 0,
          }}
        />
      )}

      <div style={{ flex: 1, minWidth: 0 }}>
        <p
          style={{
            margin: 0,
            fontWeight: 600,
            fontSize: '0.9375rem',
            color: '#111827',
            whiteSpace: 'nowrap',
            overflow: 'hidden',
            textOverflow: 'ellipsis',
          }}
        >
          {product.name}
        </p>
        <p
          style={{
            margin: '0.25rem 0 0',
            fontSize: '0.875rem',
            color: '#6b7280',
          }}
        >
          {formattedPrice}
        </p>
      </div>

      <button
        type="button"
        onClick={() => onAdd(product)}
        aria-label={t('storefront.menu.addItem', { name: product.name })}
        style={{
          flexShrink: 0,
          padding: '0.5rem 1rem',
          borderRadius: '0.375rem',
          border: 'none',
          background: 'var(--brand-color-primary, #111827)',
          color: '#fff',
          fontWeight: 600,
          fontSize: '0.875rem',
          cursor: 'pointer',
        }}
      >
        {t('storefront.menu.add')}
      </button>
    </div>
  );
}
