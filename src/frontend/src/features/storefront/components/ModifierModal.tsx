import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import type { ProductListItem, ModifierResponse } from '@/types/common';
import type { CartModifier } from '../context/CartContext';
import { useProductModifierGroups } from '../hooks/useStorefrontMenu';

interface ModifierModalProps {
  brandSlug: string;
  product: ProductListItem;
  onConfirm: (selectedModifiers: CartModifier[]) => void;
  onClose: () => void;
}

/**
 * Modal for selecting modifier options before adding a product to the cart.
 * Fetches modifier groups for the product and renders them as a list of checkboxes.
 *
 * Each modifier group allows multiple selections (no single-choice enforcement in MVP).
 */
export function ModifierModal({ brandSlug, product, onConfirm, onClose }: ModifierModalProps) {
  const { t } = useTranslation('common');
  const { data: modifierGroups, isLoading } = useProductModifierGroups(brandSlug, product.id);
  const [selectedModifierIds, setSelectedModifierIds] = useState<Set<string>>(new Set());

  function toggleModifier(modifier: ModifierResponse) {
    setSelectedModifierIds((prev) => {
      const next = new Set(prev);
      if (next.has(modifier.id)) {
        next.delete(modifier.id);
      } else {
        next.add(modifier.id);
      }
      return next;
    });
  }

  function handleConfirm() {
    if (!modifierGroups) {
      onConfirm([]);
      return;
    }

    const selected: CartModifier[] = [];
    for (const group of modifierGroups) {
      for (const modifier of group.modifiers ?? []) {
        if (selectedModifierIds.has(modifier.id)) {
          const modifierName = modifier.translations[0]?.name ?? '';
          selected.push({
            modifierId: modifier.id,
            modifierName,
            priceAdjustment: modifier.priceAdjustment,
          });
        }
      }
    }
    onConfirm(selected);
  }

  const formattedBasePrice = new Intl.NumberFormat('nl-BE', {
    style: 'currency',
    currency: product.basePrice.currency || 'EUR',
  }).format(product.basePrice.amount);

  return (
    <>
      {/* Backdrop */}
      <div
        onClick={onClose}
        style={{
          position: 'fixed',
          inset: 0,
          background: 'rgba(0,0,0,0.4)',
          zIndex: 100,
        }}
      />

      {/* Modal panel */}
      <div
        role="dialog"
        aria-modal="true"
        aria-label={t('storefront.menu.customise')}
        style={{
          position: 'fixed',
          inset: 0,
          margin: 'auto',
          width: 'min(28rem, calc(100vw - 2rem))',
          maxHeight: 'calc(100vh - 4rem)',
          background: '#fff',
          borderRadius: '0.75rem',
          boxShadow: '0 20px 60px rgba(0,0,0,0.25)',
          zIndex: 101,
          display: 'flex',
          flexDirection: 'column',
          overflow: 'hidden',
        }}
      >
        {/* Header */}
        <div
          style={{
            padding: '1.25rem 1.5rem',
            borderBottom: '1px solid #e5e7eb',
            display: 'flex',
            justifyContent: 'space-between',
            alignItems: 'flex-start',
          }}
        >
          <div>
            <h2 style={{ margin: 0, fontSize: '1.125rem', fontWeight: 700, color: '#111827' }}>
              {product.name}
            </h2>
            <p style={{ margin: '0.25rem 0 0', fontSize: '0.875rem', color: '#6b7280' }}>
              {formattedBasePrice}
            </p>
          </div>
          <button
            type="button"
            onClick={onClose}
            aria-label={t('storefront.menu.closeModal')}
            style={{
              background: 'none',
              border: 'none',
              cursor: 'pointer',
              color: '#6b7280',
              fontSize: '1.5rem',
              lineHeight: 1,
              padding: '0.25rem',
            }}
          >
            &times;
          </button>
        </div>

        {/* Body */}
        <div style={{ flex: 1, overflowY: 'auto', padding: '1rem 1.5rem' }}>
          {isLoading && (
            <p style={{ color: '#6b7280', fontSize: '0.875rem' }}>
              {t('storefront.menu.loadingModifiers')}
            </p>
          )}

          {!isLoading && modifierGroups?.length === 0 && (
            <p style={{ color: '#6b7280', fontSize: '0.875rem' }}>
              {t('storefront.menu.noModifiers')}
            </p>
          )}

          {!isLoading &&
            modifierGroups?.map((group) => {
              return (
                <div key={group.modifierGroupId} style={{ marginBottom: '1.25rem' }}>
                  <h3
                    style={{
                      margin: '0 0 0.5rem',
                      fontSize: '0.9375rem',
                      fontWeight: 600,
                      color: '#374151',
                    }}
                  >
                    {group.name}
                  </h3>
                  {(group.modifiers ?? []).map((modifier) => {
                    const modifierName = modifier.translations[0]?.name ?? '';
                    const isChecked = selectedModifierIds.has(modifier.id);
                    const adjustmentLabel =
                      modifier.priceAdjustment !== 0
                        ? ` (+${new Intl.NumberFormat('nl-BE', {
                            style: 'currency',
                            currency: 'EUR',
                          }).format(modifier.priceAdjustment)})`
                        : '';

                    return (
                      <label
                        key={modifier.id}
                        style={{
                          display: 'flex',
                          alignItems: 'center',
                          gap: '0.75rem',
                          padding: '0.5rem 0',
                          cursor: 'pointer',
                          fontSize: '0.9375rem',
                          color: '#111827',
                          borderBottom: '1px solid #f3f4f6',
                        }}
                      >
                        <input
                          type="checkbox"
                          checked={isChecked}
                          onChange={() => { toggleModifier(modifier); }}
                          style={{ width: '1.125rem', height: '1.125rem', cursor: 'pointer' }}
                        />
                        <span style={{ flex: 1 }}>{modifierName}</span>
                        {adjustmentLabel && (
                          <span style={{ color: '#6b7280', fontSize: '0.875rem' }}>
                            {adjustmentLabel}
                          </span>
                        )}
                      </label>
                    );
                  })}
                </div>
              );
            })}
        </div>

        {/* Footer */}
        <div
          style={{
            padding: '1rem 1.5rem',
            borderTop: '1px solid #e5e7eb',
            display: 'flex',
            gap: '0.75rem',
            justifyContent: 'flex-end',
          }}
        >
          <button
            type="button"
            onClick={onClose}
            style={{
              padding: '0.625rem 1.25rem',
              borderRadius: '0.375rem',
              border: '1px solid #d1d5db',
              background: '#fff',
              color: '#374151',
              fontWeight: 600,
              fontSize: '0.875rem',
              cursor: 'pointer',
            }}
          >
            {t('storefront.menu.cancel')}
          </button>
          <button
            type="button"
            onClick={handleConfirm}
            disabled={isLoading}
            style={{
              padding: '0.625rem 1.25rem',
              borderRadius: '0.375rem',
              border: 'none',
              background: 'var(--brand-color-primary, #111827)',
              color: '#fff',
              fontWeight: 600,
              fontSize: '0.875rem',
              cursor: isLoading ? 'not-allowed' : 'pointer',
              opacity: isLoading ? 0.6 : 1,
            }}
          >
            {t('storefront.menu.addToCart')}
          </button>
        </div>
      </div>
    </>
  );
}
