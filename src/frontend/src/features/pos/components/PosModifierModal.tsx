import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import type { ProductListItem, ModifierResponse } from '@/types/common';
import { useProductModifierGroups } from '@features/storefront/hooks/useStorefrontMenu';
import type { PosTicketModifier } from '../hooks/usePosOrder';

interface PosModifierModalProps {
  brandSlug: string;
  product: ProductListItem;
  onConfirm: (selectedModifiers: PosTicketModifier[]) => void;
  onClose: () => void;
}

/**
 * Touch-friendly modifier picker for the POS new-order page.
 * Reuses the same modifier data as the storefront (useProductModifierGroups)
 * but renders with larger touch targets suited to a 10" landscape tablet.
 */
export function PosModifierModal({ brandSlug, product, onConfirm, onClose }: PosModifierModalProps) {
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

    const selected: PosTicketModifier[] = [];
    for (const group of modifierGroups) {
      for (const modifier of group.modifiers) {
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
        role="presentation"
        onClick={onClose}
        style={{
          position: 'fixed',
          inset: 0,
          background: 'rgba(0,0,0,0.45)',
          zIndex: 200,
        }}
      />

      {/* Modal panel — larger than storefront version for tablet */}
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
          boxShadow: '0 24px 80px rgba(0,0,0,0.3)',
          zIndex: 201,
          display: 'flex',
          flexDirection: 'column',
          overflow: 'hidden',
        }}
      >
        {/* Header */}
        <div
          style={{
            padding: '1.5rem 1.75rem',
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
          <button
            type="button"
            onClick={onClose}
            aria-label={t('storefront.menu.closeModal')}
            style={{
              background: 'none',
              border: 'none',
              cursor: 'pointer',
              color: '#6b7280',
              fontSize: '1.75rem',
              lineHeight: 1,
              padding: '0.25rem',
              minWidth: '2.75rem',
              minHeight: '2.75rem',
              display: 'flex',
              alignItems: 'center',
              justifyContent: 'center',
            }}
          >
            &times;
          </button>
        </div>

        {/* Body */}
        <div style={{ flex: 1, overflowY: 'auto', padding: '1.25rem 1.75rem' }}>
          {isLoading && (
            <p style={{ color: '#6b7280', fontSize: '1rem' }}>
              {t('storefront.menu.loadingModifiers')}
            </p>
          )}

          {!isLoading && modifierGroups && modifierGroups.length === 0 && (
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
                {group.modifiers.map((modifier) => {
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
                        padding: '0.75rem 0',
                        cursor: 'pointer',
                        fontSize: '1rem',
                        color: '#111827',
                        borderBottom: '1px solid #f3f4f6',
                        minHeight: '2.75rem', // 44px touch target
                      }}
                    >
                      <input
                        type="checkbox"
                        checked={isChecked}
                        onChange={() => toggleModifier(modifier)}
                        style={{ width: '1.375rem', height: '1.375rem', cursor: 'pointer' }}
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

        {/* Footer */}
        <div
          style={{
            padding: '1.25rem 1.75rem',
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
              padding: '0.75rem 1.5rem',
              minHeight: '2.75rem',
              borderRadius: '0.5rem',
              border: '1px solid #d1d5db',
              background: '#fff',
              color: '#374151',
              fontWeight: 600,
              fontSize: '1rem',
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
              padding: '0.75rem 1.5rem',
              minHeight: '2.75rem',
              borderRadius: '0.5rem',
              border: 'none',
              background: '#111827',
              color: '#fff',
              fontWeight: 600,
              fontSize: '1rem',
              cursor: isLoading ? 'not-allowed' : 'pointer',
              opacity: isLoading ? 0.6 : 1,
            }}
          >
            {t('pos.order.addToOrder')}
          </button>
        </div>
      </div>
    </>
  );
}
