import type { ProductListItem } from '@/types/common';

interface ProductTileProps {
  product: ProductListItem;
  onTap: (product: ProductListItem) => void;
}

/**
 * Large square tile representing a single product on the POS menu grid.
 * Meets touch UX spec: min-height 7rem, padding 1.25rem, entire tile is tappable.
 */
export function ProductTile({ product, onTap }: ProductTileProps) {
  const formattedPrice = new Intl.NumberFormat('nl-BE', {
    style: 'currency',
    currency: product.basePrice.currency || 'EUR',
  }).format(product.basePrice.amount);

  return (
    <button
      type="button"
      onClick={() => onTap(product)}
      data-testid={`product-tile-${product.id}`}
      style={{
        display: 'flex',
        flexDirection: 'column',
        justifyContent: 'space-between',
        minHeight: '7rem',
        padding: '1.25rem',
        background: '#fff',
        border: '1px solid #e5e7eb',
        borderRadius: '0.75rem',
        cursor: 'pointer',
        textAlign: 'left',
        width: '100%',
        transition: 'box-shadow 0.15s ease, border-color 0.15s ease',
        boxShadow: '0 1px 3px rgba(0,0,0,0.06)',
      }}
      onMouseDown={(e) => {
        (e.currentTarget as HTMLButtonElement).style.boxShadow =
          '0 0 0 2px #111827 inset';
      }}
      onMouseUp={(e) => {
        (e.currentTarget as HTMLButtonElement).style.boxShadow =
          '0 1px 3px rgba(0,0,0,0.06)';
      }}
      onMouseLeave={(e) => {
        (e.currentTarget as HTMLButtonElement).style.boxShadow =
          '0 1px 3px rgba(0,0,0,0.06)';
      }}
    >
      <span
        style={{
          fontSize: '1rem',
          fontWeight: 600,
          color: '#111827',
          lineHeight: 1.3,
          overflowWrap: 'break-word',
        }}
      >
        {product.name}
      </span>
      <span
        style={{
          fontSize: '1rem',
          fontWeight: 700,
          color: '#374151',
          marginTop: '0.5rem',
        }}
      >
        {formattedPrice}
      </span>
    </button>
  );
}
