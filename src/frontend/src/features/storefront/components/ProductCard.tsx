import { useTranslation } from 'react-i18next';
import {
  Allergen,
  DietaryTag,
  ALLERGEN_KEYS,
  DIETARY_TAG_KEYS,
} from '@/types/common';
import type { ProductListItem } from '@/types/common';

interface ProductCardProps {
  product: ProductListItem;
  onAdd: (product: ProductListItem) => void;
}

const ALLERGEN_KEY_BY_VALUE = new Map<number, (typeof ALLERGEN_KEYS)[number]>(
  ALLERGEN_KEYS.map((key) => [Allergen[key], key] as const),
);

const DIETARY_KEY_BY_VALUE = new Map<number, (typeof DIETARY_TAG_KEYS)[number]>(
  DIETARY_TAG_KEYS.map((key) => [DietaryTag[key], key] as const),
);

const chipBase: React.CSSProperties = {
  display: 'inline-flex',
  alignItems: 'center',
  gap: '0.1875rem',
  padding: '0.125rem 0.5rem',
  borderRadius: '9999px',
  fontSize: '0.6875rem',
  fontWeight: 600,
  lineHeight: 1.3,
};

const allergenChipStyle: React.CSSProperties = {
  ...chipBase,
  background: '#fef3c7',
  color: '#92400e',
  border: '1px solid #fde68a',
};

const dietaryChipStyle: React.CSSProperties = {
  ...chipBase,
  background: '#dcfce7',
  color: '#166534',
  border: '1px solid #bbf7d0',
};

/**
 * Displays a single product in the storefront menu.
 * Shows the product name, formatted price, allergen and dietary chips, and an "Add" button.
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

  const hasTags = product.allergens.length > 0 || product.dietaryTags.length > 0;

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

        {hasTags && (
          <div
            style={{
              marginTop: '0.375rem',
              display: 'flex',
              flexWrap: 'wrap',
              gap: '0.25rem',
            }}
            data-testid="product-tags"
          >
            {product.dietaryTags.map((value) => {
              const key = DIETARY_KEY_BY_VALUE.get(value);
              if (!key) return null;
              const label = t(`dietaryTags.${key}`);
              return (
                <span
                  key={`dietary-${String(value)}`}
                  style={dietaryChipStyle}
                  title={label}
                  aria-label={label}
                >
                  <span aria-hidden="true">✓</span>
                  {label}
                </span>
              );
            })}
            {product.allergens.map((value) => {
              const key = ALLERGEN_KEY_BY_VALUE.get(value);
              if (!key) return null;
              const label = t(`allergens.${key}`);
              const containsLabel = t('storefront.menu.contains', { name: label });
              return (
                <span
                  key={`allergen-${String(value)}`}
                  style={allergenChipStyle}
                  title={containsLabel}
                  aria-label={containsLabel}
                >
                  <span aria-hidden="true">⚠</span>
                  {label}
                </span>
              );
            })}
          </div>
        )}
      </div>

      <button
        type="button"
        onClick={() => { onAdd(product); }}
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
