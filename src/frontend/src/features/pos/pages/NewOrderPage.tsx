import { useState } from 'react';
import { useParams, Link } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { useStorefrontCategories, useStorefrontCategoryProducts } from '@features/storefront/hooks/useStorefrontMenu';
import { ProductTile } from '../components/ProductTile';
import { PosTicket } from '../components/PosTicket';
import { PosModifierModal } from '../components/PosModifierModal';
import { usePosOrder } from '../hooks/usePosOrder';
import { useCreatePosOrder } from '../hooks/useCreatePosOrder';
import type { ProductListItem } from '@/types/common';
import type { PosTicketModifier } from '../hooks/usePosOrder';

/**
 * POS new-order page — the heart of US-FP-018.
 * Two-pane layout (CSS grid 1fr 22rem) optimised for 10" landscape tablet.
 * Left pane: category tabs + product grid.
 * Right pane: live ticket with qty controls, order type selector, table number, submit.
 */
export function NewOrderPage() {
  const { t } = useTranslation('common');
  const { brandSlug, lang } = useParams<{
    brandSlug: string;
    lang: string;
    shopId: string;
  }>();

  const resolvedBrand = brandSlug ?? '';
  const resolvedLang = lang ?? 'nl';

  const { state, dispatch, subtotal } = usePosOrder();
  const [activeCategoryId, setActiveCategoryId] = useState<string | null>(null);
  const [pendingProduct, setPendingProduct] = useState<ProductListItem | null>(null);

  // Data hooks — reuse storefront queries (same cache, no duplication)
  const { data: categories, isLoading: categoriesLoading } = useStorefrontCategories(resolvedBrand);
  const firstCategoryId = categories?.[0]?.id ?? null;
  const resolvedCategoryId = activeCategoryId ?? firstCategoryId;

  const { data: products, isLoading: productsLoading } = useStorefrontCategoryProducts(
    resolvedBrand,
    resolvedCategoryId ?? '',
  );

  // Mutation
  const createOrder = useCreatePosOrder(dispatch);

  function handleProductTap(product: ProductListItem) {
    // If product has no modifier groups we know about at tile level, just add it.
    // The PosModifierModal will check and show "no modifiers" if none exist.
    // To avoid an extra round-trip, we open the modal for all products so staff
    // can confirm. Products with no modifiers get a quick "Add" button.
    setPendingProduct(product);
  }

  function handleModifierConfirm(modifiers: PosTicketModifier[]) {
    if (!pendingProduct) return;
    dispatch({
      type: 'ADD_ITEM',
      payload: {
        productId: pendingProduct.id,
        productName: pendingProduct.name,
        unitGrossPrice: pendingProduct.basePrice.amount,
        selectedModifiers: modifiers,
      },
    });
    setPendingProduct(null);
  }

  function handlePlaceOrder() {
    if (state.items.length === 0) return;

    createOrder.mutate({
      orderType: state.orderType,
      paymentMethod: 'CashAtPickup',
      customerName: state.customerName.trim() || null,
      tableNumber: state.orderType === 'EatIn' ? state.tableNumber.trim() : null,
      items: state.items.map((item) => ({
        productId: item.productId,
        quantity: item.quantity,
        selectedModifierIds: item.selectedModifiers.map((m) => m.modifierId),
      })),
    });
  }

  const submitError = createOrder.isError
    ? (createOrder.error?.message ?? t('pos.order.submitError'))
    : null;

  return (
    <div
      style={{
        height: '100dvh',
        display: 'grid',
        gridTemplateColumns: '1fr 22rem',
        overflow: 'hidden',
      }}
    >
      {/* ── Left pane — menu ── */}
      <main
        style={{
          display: 'flex',
          flexDirection: 'column',
          overflow: 'hidden',
        }}
      >
        {/* Top bar */}
        <header
          style={{
            padding: '0.75rem 1rem',
            borderBottom: '1px solid #e5e7eb',
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'space-between',
            gap: '0.75rem',
            flexShrink: 0,
          }}
        >
          <h1 style={{ margin: 0, fontSize: '1.25rem', fontWeight: 800, color: '#111827' }}>
            {t('pos.order.title')}
          </h1>
          <Link
            to={`/${resolvedBrand}/${resolvedLang}/pos`}
            style={{
              fontSize: '0.875rem',
              color: '#6b7280',
              textDecoration: 'none',
              padding: '0.375rem 0.75rem',
              border: '1px solid #d1d5db',
              borderRadius: '0.375rem',
            }}
          >
            ← {t('pos.dashboard.backToDashboard')}
          </Link>
        </header>

        {/* Category tabs */}
        {categoriesLoading ? (
          <p style={{ padding: '1rem', color: '#9ca3af' }}>{t('loading')}</p>
        ) : (
          <nav
            style={{
              display: 'flex',
              gap: '0.5rem',
              padding: '0.75rem 1rem',
              overflowX: 'auto',
              flexShrink: 0,
              borderBottom: '1px solid #f3f4f6',
            }}
            aria-label="Menu categorieën"
          >
            {categories?.map((cat) => {
              const isActive = (resolvedCategoryId === cat.id);
              return (
                <button
                  key={cat.id}
                  type="button"
                  onClick={() => setActiveCategoryId(cat.id)}
                  style={{
                    padding: '0.5rem 1rem',
                    minHeight: '2.75rem',
                    borderRadius: '2rem',
                    border: isActive ? '2px solid #111827' : '2px solid #e5e7eb',
                    background: isActive ? '#111827' : '#fff',
                    color: isActive ? '#fff' : '#374151',
                    fontWeight: 600,
                    fontSize: '0.9375rem',
                    cursor: 'pointer',
                    whiteSpace: 'nowrap',
                    flexShrink: 0,
                  }}
                >
                  {cat.name}
                </button>
              );
            })}
          </nav>
        )}

        {/* Product grid */}
        <div
          style={{
            flex: 1,
            overflowY: 'auto',
            padding: '1rem',
          }}
        >
          {productsLoading ? (
            <p style={{ color: '#9ca3af' }}>{t('loading')}</p>
          ) : products && products.length > 0 ? (
            <div
              style={{
                display: 'grid',
                gridTemplateColumns: 'repeat(auto-fill, minmax(8rem, 1fr))',
                gap: '0.75rem',
              }}
            >
              {products.map((product) => (
                <ProductTile key={product.id} product={product} onTap={handleProductTap} />
              ))}
            </div>
          ) : (
            <p style={{ color: '#9ca3af' }}>{t('storefront.menu.noCategories')}</p>
          )}
        </div>
      </main>

      {/* ── Right pane — ticket ── */}
      <PosTicket
        state={state}
        subtotal={subtotal}
        dispatch={dispatch}
        onPlaceOrder={handlePlaceOrder}
        isSubmitting={createOrder.isPending}
        submitError={submitError}
      />

      {/* Modifier modal */}
      {pendingProduct && (
        <PosModifierModal
          brandSlug={resolvedBrand}
          product={pendingProduct}
          onConfirm={handleModifierConfirm}
          onClose={() => setPendingProduct(null)}
        />
      )}
    </div>
  );
}
