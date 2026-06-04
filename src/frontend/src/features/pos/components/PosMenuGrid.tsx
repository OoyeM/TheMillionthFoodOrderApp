import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import type { ProductListItem } from '@/types/common';
import { useStorefrontCategories, useStorefrontCategoryProducts, useProductModifierGroups } from '@features/storefront/hooks/useStorefrontMenu';
import type { CartItem } from '../context/PosOrderContext';
import { useOrderState } from '../context/PosOrderContext';
import { PosModifierModal } from './PosModifierModal';

interface PosMenuGridProps {
  brandSlug: string;
}

/**
 * Touch-first product grid for the POS interface.
 *
 * Layout: CSS grid with 2-4 responsive columns (auto-fill, min 160px).
 * All tap targets are at least 48 × 48 px.
 * No hover-only affordances — interactions are via tap/click.
 *
 * Products with modifier groups open PosModifierModal before being added.
 * Products without modifiers are added directly to the order.
 */
export function PosMenuGrid({ brandSlug }: PosMenuGridProps) {
  const { t } = useTranslation('common');
  const { data: categories, isLoading: categoriesLoading } = useStorefrontCategories(brandSlug);
  const { addItem } = useOrderState();

  const [modalProduct, setModalProduct] = useState<ProductListItem | null>(null);

  function handleProductTap(product: ProductListItem, hasModifiers: boolean) {
    if (hasModifiers) {
      setModalProduct(product);
    } else {
      addItem({
        productId: product.id,
        productName: product.name,
        quantity: 1,
        unitGrossPrice: product.basePrice.amount,
        selectedModifiers: [],
      });
    }
  }

  function handleModalConfirm(selectedModifiers: CartItem['selectedModifiers']) {
    if (!modalProduct) return;
    addItem({
      productId: modalProduct.id,
      productName: modalProduct.name,
      quantity: 1,
      unitGrossPrice: modalProduct.basePrice.amount,
      selectedModifiers,
    });
    setModalProduct(null);
  }

  if (categoriesLoading) {
    return (
      <div style={{ padding: '2rem', color: '#6b7280', textAlign: 'center' }}>
        {t('loading')}
      </div>
    );
  }

  if (!categories || categories.length === 0) {
    return (
      <div style={{ padding: '2rem', color: '#6b7280', textAlign: 'center' }}>
        {t('storefront.menu.noCategories')}
      </div>
    );
  }

  return (
    <>
      <div
        style={{ overflowY: 'auto', flex: 1, padding: '1rem' }}
        data-testid="pos-menu-grid"
      >
        <h2 style={{ margin: '0 0 1rem', fontSize: '1.25rem', fontWeight: 700, color: '#111827' }}>
          {t('pos.menu.title')}
        </h2>

        {categories.map((category) => (
          <CategorySection
            key={category.id}
            brandSlug={brandSlug}
            categoryId={category.id}
            categoryName={category.name}
            onProductTap={handleProductTap}
          />
        ))}
      </div>

      {modalProduct && (
        <PosModifierModal
          brandSlug={brandSlug}
          product={modalProduct}
          onConfirm={handleModalConfirm}
          onClose={() => { setModalProduct(null); }}
        />
      )}
    </>
  );
}

// ---------------------------------------------------------------------------
// Category section sub-component
// ---------------------------------------------------------------------------

interface CategorySectionProps {
  brandSlug: string;
  categoryId: string;
  categoryName: string;
  onProductTap: (product: ProductListItem, hasModifiers: boolean) => void;
}

function CategorySection({
  brandSlug,
  categoryId,
  categoryName,
  onProductTap,
}: CategorySectionProps) {
  const { data: products } = useStorefrontCategoryProducts(brandSlug, categoryId);

  if (!products || products.length === 0) return null;

  return (
    <section style={{ marginBottom: '2rem' }}>
      <h3
        style={{
          margin: '0 0 0.75rem',
          fontSize: '1rem',
          fontWeight: 600,
          color: '#374151',
          paddingBottom: '0.375rem',
          borderBottom: '2px solid var(--brand-color-primary, #111827)',
          display: 'inline-block',
        }}
      >
        {categoryName}
      </h3>

      {/* Responsive grid: 2-4 cols based on viewport, min tap target 48px */}
      <div
        style={{
          display: 'grid',
          gridTemplateColumns: 'repeat(auto-fill, minmax(160px, 1fr))',
          gap: '0.75rem',
        }}
      >
        {products.map((product) => (
          <ProductTile
            key={product.id}
            brandSlug={brandSlug}
            product={product}
            onTap={onProductTap}
          />
        ))}
      </div>
    </section>
  );
}

// ---------------------------------------------------------------------------
// Product tile sub-component
// ---------------------------------------------------------------------------

interface ProductTileProps {
  brandSlug: string;
  product: ProductListItem;
  onTap: (product: ProductListItem, hasModifiers: boolean) => void;
}

function ProductTile({ brandSlug, product, onTap }: ProductTileProps) {
  const { t } = useTranslation('common');
  const { data: modifierGroups } = useProductModifierGroups(brandSlug, product.id);
  const hasModifiers = modifierGroups !== undefined && modifierGroups.length > 0;

  const formattedPrice = new Intl.NumberFormat('nl-BE', {
    style: 'currency',
    currency: product.basePrice.currency || 'EUR',
  }).format(product.basePrice.amount);

  return (
    <button
      type="button"
      onClick={() => { onTap(product, hasModifiers); }}
      aria-label={`${product.name} — ${formattedPrice}`}
      style={{
        display: 'flex',
        flexDirection: 'column',
        alignItems: 'flex-start',
        gap: '0.5rem',
        padding: '0.875rem',
        borderRadius: '0.75rem',
        border: '2px solid #e5e7eb',
        background: '#fff',
        cursor: 'pointer',
        textAlign: 'left',
        width: '100%',
        /* Touch target: always at least 48px tall */
        minHeight: '4rem',
        transition: 'border-color 0.15s',
      }}
      onFocus={(e) => {
        e.currentTarget.style.borderColor = 'var(--brand-color-primary, #111827)';
      }}
      onBlur={(e) => {
        e.currentTarget.style.borderColor = '#e5e7eb';
      }}
      onPointerDown={(e) => {
        e.currentTarget.style.borderColor = 'var(--brand-color-primary, #111827)';
        e.currentTarget.style.background = '#f9fafb';
      }}
      onPointerUp={(e) => {
        e.currentTarget.style.borderColor = '#e5e7eb';
        e.currentTarget.style.background = '#fff';
      }}
    >
      {product.imageUrl && (
        <img
          src={product.imageUrl}
          alt=""
          aria-hidden="true"
          style={{
            width: '100%',
            height: '6rem',
            objectFit: 'cover',
            borderRadius: '0.375rem',
          }}
        />
      )}
      <span
        style={{
          fontWeight: 600,
          fontSize: '0.9375rem',
          color: '#111827',
          lineHeight: 1.3,
        }}
      >
        {product.name}
      </span>
      <span
        style={{
          fontSize: '0.875rem',
          color: 'var(--brand-color-primary, #374151)',
          fontWeight: 700,
        }}
      >
        {formattedPrice}
      </span>
      {hasModifiers && (
        <span
          style={{
            fontSize: '0.75rem',
            color: '#6b7280',
            fontStyle: 'italic',
          }}
        >
          {t('pos.menu.modifierIndicator')}
        </span>
      )}
    </button>
  );
}
