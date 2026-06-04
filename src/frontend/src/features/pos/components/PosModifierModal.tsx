import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import type { ProductListItem, ModifierResponse } from '@/types/common';
import type { CartModifier } from '../context/PosOrderContext';
import { useProductModifierGroups } from '@features/storefront/hooks/useStorefrontMenu';

interface PosModifierModalProps {
  brandSlug: string;
  product: ProductListItem;
  onConfirm: (selectedModifiers: CartModifier[]) => void;
  onClose: () => void;
}

/**
 * Touch-optimised modifier selection modal for the POS interface.
 *
 * Design choices vs storefront ModifierModal:
 * - Large (min 48px) checkbox targets for finger-first use
 * - Close button is explicit and large; backdrop tap does NOT close the modal
 *   (prevents accidental dismissal on a touch screen)
 * - Confirm and cancel buttons are full-width and tall
 */
export function PosModifierModal({
  brandSlug,
  product,
  onConfirm,
  onClose,
}: PosModifierModalProps) {
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
      // eslint-disable-next-line @typescript-eslint/no-unnecessary-condition -- modifiers comes from a network response; the field can be absent at runtime despite the non-nullable type
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
      {/*
        Backdrop: intentionally does NOT close on click (touch-safe).
        Staff must use the explicit close button.
      */}
      <div
        aria-hidden="true"
        style={{
          position: 'fixed',
          inset: 0,
          background: 'rgba(0,0,0,0.5)',
          zIndex: 200,
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
          width: 'min(36rem, calc(100vw - 2rem))',
          maxHeight: 'calc(100vh - 4rem)',
          background: '#fff',
          borderRadius: '1rem',
          boxShadow: '0 24px 64px rgba(0,0,0,0.3)',
          zIndex: 201,
          display: 'flex',
          flexDirection: 'column',
          overflow: 'hidden',
        }}
      >
        {/* Header */}
        <div
          style={{
            padding: '1.5rem',
            borderBottom: '1px solid #e5e7eb',
            display: 'flex',
            justifyContent: 'space-between',
            alignItems: 'flex-start',
          }}
        >
          <div>
            <h2 style={{ margin: 0, fontSize: '1.25rem', fontWeight: 700, color: '#111827' }}>
              {product.name}
            </h2>
            <p style={{ margin: '0.25rem 0 0', fontSize: '1rem', color: '#6b7280' }}>
              {formattedBasePrice}
            </p>
          </div>
          {/* Large explicit close button — no accidental backdrop dismiss */}
          <button
            type="button"
            onClick={onClose}
            aria-label={t('storefront.menu.closeModal')}
            style={{
              minWidth: '3rem',
              minHeight: '3rem',
              background: '#f3f4f6',
              border: 'none',
              borderRadius: '0.5rem',
              cursor: 'pointer',
              color: '#374151',
              fontSize: '1.5rem',
              lineHeight: 1,
              display: 'flex',
              alignItems: 'center',
              justifyContent: 'center',
              flexShrink: 0,
            }}
          >
            &times;
          </button>
        </div>

        {/* Body */}
        <div style={{ flex: 1, overflowY: 'auto', padding: '1rem 1.5rem' }}>
          {isLoading && (
            <p style={{ color: '#6b7280', fontSize: '1rem' }}>
              {t('storefront.menu.loadingModifiers')}
            </p>
          )}

          {!isLoading && modifierGroups?.length === 0 && (
            <p style={{ color: '#6b7280', fontSize: '1rem' }}>
              {t('storefront.menu.noModifiers')}
            </p>
          )}

          {!isLoading &&
            modifierGroups?.map((group) => (
              <div key={group.modifierGroupId} style={{ marginBottom: '1.5rem' }}>
                <h3
                  style={{
                    margin: '0 0 0.75rem',
                    fontSize: '1.0625rem',
                    fontWeight: 600,
                    color: '#374151',
                  }}
                >
                  {group.name}
                </h3>
                {/* eslint-disable-next-line @typescript-eslint/no-unnecessary-condition -- modifiers comes from a network response; the field can be absent at runtime despite the non-nullable type */}
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
                        gap: '1rem',
                        padding: '0.875rem 0.5rem',
                        cursor: 'pointer',
                        fontSize: '1rem',
                        color: '#111827',
                        borderBottom: '1px solid #f3f4f6',
                        minHeight: '3rem',
                      }}
                    >
                      {/* Large touch target checkbox */}
                      <input
                        type="checkbox"
                        checked={isChecked}
                        onChange={() => { toggleModifier(modifier); }}
                        style={{
                          width: '1.5rem',
                          height: '1.5rem',
                          cursor: 'pointer',
                          flexShrink: 0,
                        }}
                      />
                      <span style={{ flex: 1 }}>{modifierName}</span>
                      {adjustmentLabel && (
                        <span style={{ color: '#6b7280', fontSize: '0.9375rem' }}>
                          {adjustmentLabel}
                        </span>
                      )}
                    </label>
                  );
                })}
              </div>
            ))}
        </div>

        {/* Footer — full-width buttons for touch use */}
        <div
          style={{
            padding: '1rem 1.5rem',
            borderTop: '1px solid #e5e7eb',
            display: 'flex',
            gap: '0.75rem',
          }}
        >
          <button
            type="button"
            onClick={onClose}
            style={{
              flex: 1,
              padding: '0.875rem',
              borderRadius: '0.5rem',
              border: '1px solid #d1d5db',
              background: '#fff',
              color: '#374151',
              fontWeight: 600,
              fontSize: '1rem',
              cursor: 'pointer',
              minHeight: '3rem',
            }}
          >
            {t('storefront.menu.cancel')}
          </button>
          <button
            type="button"
            data-testid="pos-modifier-confirm"
            onClick={handleConfirm}
            disabled={isLoading}
            style={{
              flex: 2,
              padding: '0.875rem',
              borderRadius: '0.5rem',
              border: 'none',
              background: 'var(--brand-color-primary, #111827)',
              color: '#fff',
              fontWeight: 600,
              fontSize: '1rem',
              cursor: isLoading ? 'not-allowed' : 'pointer',
              opacity: isLoading ? 0.6 : 1,
              minHeight: '3rem',
            }}
          >
            {t('storefront.menu.addToCart')}
          </button>
        </div>
      </div>
    </>
  );
}
