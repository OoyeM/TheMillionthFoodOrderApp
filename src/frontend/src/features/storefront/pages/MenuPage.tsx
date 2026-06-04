import { useCallback, useEffect, useMemo, useState } from 'react';
import { useParams } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import type { ProductListItem } from '@/types/common';
import { CartProvider, useCart, type CartModifier } from '../context/CartContext';
import { useResolvedShop } from '../hooks/useResolvedShop';
import { ProductCard } from '../components/ProductCard';
import { ModifierModal } from '../components/ModifierModal';
import { CartDrawer } from '../components/CartDrawer';
import { MenuFilters } from '../components/MenuFilters';
import { useStorefrontCategories, useStorefrontCategoryProducts } from '../hooks/useStorefrontMenu';
import {
  EMPTY_FILTERS,
  isFilterActive,
  matchesFilters,
  type MenuFilterState,
} from '../utils/menuFilters';
import { modifierGroupsApi } from '@api/modifierGroups';

// ---------------------------------------------------------------------------
// Category section — fetches its own products and reports filtered match count
// ---------------------------------------------------------------------------

interface CategorySectionProps {
  brandSlug: string;
  categoryId: string;
  categoryName: string;
  filters: MenuFilterState;
  onAddProduct: (product: ProductListItem) => void;
  onMatchCountChange: (categoryId: string, count: number) => void;
}

function CategorySection({
  brandSlug,
  categoryId,
  categoryName,
  filters,
  onAddProduct,
  onMatchCountChange,
}: CategorySectionProps) {
  const { t } = useTranslation('common');
  const { data: products, isLoading, isError } = useStorefrontCategoryProducts(brandSlug, categoryId);

  const filteredProducts = useMemo(
    () => (products ?? []).filter((p) => matchesFilters(p, filters)),
    [products, filters],
  );

  useEffect(() => {
    if (products) onMatchCountChange(categoryId, filteredProducts.length);
  }, [products, filteredProducts.length, categoryId, onMatchCountChange]);

  if (isLoading) {
    return (
      <section style={{ marginBottom: '2rem' }}>
        <h2 style={{ fontSize: '1.25rem', fontWeight: 700, color: '#111827', marginBottom: '0.75rem' }}>
          {categoryName}
        </h2>
        <p style={{ color: '#6b7280' }}>{t('loading')}</p>
      </section>
    );
  }

  if (isError) {
    return (
      <section style={{ marginBottom: '2rem' }}>
        <h2 style={{ fontSize: '1.25rem', fontWeight: 700, color: '#111827', marginBottom: '0.75rem' }}>
          {categoryName}
        </h2>
        <p style={{ color: '#ef4444', fontSize: '0.875rem' }}>{t('error')}</p>
      </section>
    );
  }

  if (filteredProducts.length === 0) return null;

  return (
    <section style={{ marginBottom: '2.5rem' }}>
      <h2
        style={{
          fontSize: '1.25rem',
          fontWeight: 700,
          color: '#111827',
          marginBottom: '0.75rem',
          paddingBottom: '0.5rem',
          borderBottom: '2px solid var(--brand-color-primary, #111827)',
        }}
      >
        {categoryName}
      </h2>
      {filteredProducts.map((product) => (
        <ProductCard key={product.id} product={product} onAdd={onAddProduct} />
      ))}
    </section>
  );
}

// ---------------------------------------------------------------------------
// Menu content (inner — needs cart context and brandSlug)
// ---------------------------------------------------------------------------

interface MenuContentProps {
  brandSlug: string;
}

function MenuContent({ brandSlug }: MenuContentProps) {
  const { t } = useTranslation('common');
  const { addItem, cartItemCount } = useCart();

  const [cartOpen, setCartOpen] = useState(false);
  const [modifierProduct, setModifierProduct] = useState<ProductListItem | null>(null);
  const [filters, setFilters] = useState<MenuFilterState>(EMPTY_FILTERS);
  const [matchCounts, setMatchCounts] = useState<Record<string, number>>({});

  const { data: categories, isLoading, isError } = useStorefrontCategories(brandSlug);

  const handleMatchCountChange = useCallback((categoryId: string, count: number) => {
    setMatchCounts((prev) => (prev[categoryId] === count ? prev : { ...prev, [categoryId]: count }));
  }, []);

  async function checkProductHasModifiers(product: ProductListItem): Promise<boolean> {
    try {
      const groups = await modifierGroupsApi.getProductModifierGroups(brandSlug, product.id);
      return groups.length > 0;
    } catch {
      return false;
    }
  }

  async function handleAddProduct(product: ProductListItem) {
    const hasModifiers = await checkProductHasModifiers(product);
    if (hasModifiers) {
      setModifierProduct(product);
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

  function handleModifierConfirm(selectedModifiers: CartModifier[]) {
    if (!modifierProduct) return;
    addItem({
      productId: modifierProduct.id,
      productName: modifierProduct.name,
      quantity: 1,
      unitGrossPrice: modifierProduct.basePrice.amount,
      selectedModifiers,
    });
    setModifierProduct(null);
  }

  const totalMatches = Object.values(matchCounts).reduce((sum, n) => sum + n, 0);
  const filterActive = isFilterActive(filters);
  const showNoMatches =
    filterActive && categories && categories.length > 0 && totalMatches === 0;

  return (
    <main style={{ maxWidth: '48rem', margin: '0 auto', padding: '1.5rem 1rem' }}>
      {/* Sticky cart button */}
      <div
        style={{
          position: 'sticky',
          top: '1rem',
          zIndex: 50,
          display: 'flex',
          justifyContent: 'flex-end',
          marginBottom: '1.5rem',
        }}
      >
        <button
          type="button"
          onClick={() => { setCartOpen(true); }}
          style={{
            display: 'flex',
            alignItems: 'center',
            gap: '0.5rem',
            padding: '0.625rem 1.25rem',
            borderRadius: '9999px',
            border: 'none',
            background: 'var(--brand-color-primary, #111827)',
            color: '#fff',
            fontWeight: 700,
            fontSize: '0.9375rem',
            cursor: 'pointer',
            boxShadow: '0 4px 12px rgba(0,0,0,0.15)',
          }}
        >
          <span>{t('storefront.cart.title')}</span>
          {cartItemCount > 0 && (
            <span
              style={{
                background: 'var(--brand-color-accent, #2563eb)',
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
        </button>
      </div>

      <h1 style={{ fontSize: '1.75rem', fontWeight: 800, color: '#111827', marginBottom: '1.5rem' }}>
        {t('storefront.menu.title')}
      </h1>

      <MenuFilters filters={filters} onChange={setFilters} />

      {isLoading && <p style={{ color: '#6b7280' }}>{t('loading')}</p>}
      {isError && <p style={{ color: '#ef4444' }}>{t('error')}</p>}

      {categories?.length === 0 && (
        <p style={{ color: '#6b7280' }}>{t('storefront.menu.noCategories')}</p>
      )}

      {categories?.map((category) => (
        <CategorySection
          key={category.id}
          brandSlug={brandSlug}
          categoryId={category.id}
          categoryName={category.name}
          filters={filters}
          onAddProduct={(product) => { void handleAddProduct(product); }}
          onMatchCountChange={handleMatchCountChange}
        />
      ))}

      {showNoMatches && (
        <p style={{ color: '#6b7280', textAlign: 'center', padding: '2rem 0' }}>
          {t('storefront.menu.filters.noMatches')}
        </p>
      )}

      {/* Modifier modal */}
      {modifierProduct && (
        <ModifierModal
          brandSlug={brandSlug}
          product={modifierProduct}
          onConfirm={handleModifierConfirm}
          onClose={() => { setModifierProduct(null); }}
        />
      )}

      {/* Cart drawer */}
      <CartDrawer isOpen={cartOpen} onClose={() => { setCartOpen(false); }} />
    </main>
  );
}

// ---------------------------------------------------------------------------
// MenuPage — wraps content in CartProvider, reads shop from ShopContext
// ---------------------------------------------------------------------------

export function MenuPage() {
  const { brandSlug } = useParams<{ brandSlug: string }>();
  // shopId comes from the resolved ShopContext (set by ShopResolver layout route).
  const shop = useResolvedShop();

  if (!brandSlug) {
    return <p>Invalid route: brandSlug is required.</p>;
  }

  return (
    <CartProvider brandSlug={brandSlug} shopId={shop.id}>
      <MenuContent brandSlug={brandSlug} />
    </CartProvider>
  );
}
