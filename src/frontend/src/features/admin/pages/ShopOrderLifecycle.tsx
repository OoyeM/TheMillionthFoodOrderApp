import { useState, useEffect } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import {
  useOrderLifecycle,
  useConfigureOrderLifecycle,
  useResetOrderLifecycle,
} from '../hooks/useOrderLifecycle';
import type {
  OrderStatusRequest,
  OrderStatusTransitionRequest,
} from '../../../types/common';

// ---------------------------------------------------------------------------
// Local state shape
// ---------------------------------------------------------------------------

interface LocalStatus {
  localId: string;
  name: string;
  systemKey: string | null;
  sortOrder: number;
  isTerminal: boolean;
  colorHex: string;
}

interface LocalTransition {
  localId: string;
  fromSortOrder: number;
  toSortOrder: number;
}

let _localIdCounter = 0;
function nextLocalId(): string {
  _localIdCounter += 1;
  return `local-${_localIdCounter}`;
}

// ---------------------------------------------------------------------------
// Component
// ---------------------------------------------------------------------------

export function ShopOrderLifecycle() {
  const { brandSlug: rawBrand, lang, shopId: rawShopId } = useParams();
  const brandSlug = rawBrand ?? '';
  const shopId = rawShopId ?? '';
  const navigate = useNavigate();
  const { t } = useTranslation('common');

  const lifecycle = useOrderLifecycle(brandSlug, shopId);
  const configureMutation = useConfigureOrderLifecycle(brandSlug, shopId);
  const resetMutation = useResetOrderLifecycle(brandSlug, shopId);

  const [statuses, setStatuses] = useState<LocalStatus[]>([]);
  const [transitions, setTransitions] = useState<LocalTransition[]>([]);
  const [formInitialized, setFormInitialized] = useState(false);
  const [successMessage, setSuccessMessage] = useState('');
  const [showResetConfirm, setShowResetConfirm] = useState(false);

  // Initialize form from fetched data
  useEffect(() => {
    if (lifecycle.data && !formInitialized) {
      setStatuses(
        lifecycle.data.statuses.map((s) => ({
          localId: nextLocalId(),
          name: s.name,
          systemKey: s.systemKey,
          sortOrder: s.sortOrder,
          isTerminal: s.isTerminal,
          colorHex: s.colorHex ?? '',
        })),
      );

      // Build transitions from response — map status IDs to sort orders
      const idToSort = new Map(
        lifecycle.data.statuses.map((s) => [s.id, s.sortOrder]),
      );
      setTransitions(
        lifecycle.data.transitions.map((tr) => ({
          localId: nextLocalId(),
          fromSortOrder: idToSort.get(tr.fromStatusId) ?? 0,
          toSortOrder: idToSort.get(tr.toStatusId) ?? 0,
        })),
      );

      setFormInitialized(true);
    }
  }, [lifecycle.data, formInitialized]);

  // Auto-dismiss success message
  useEffect(() => {
    if (!successMessage) return;
    const timer = setTimeout(() => setSuccessMessage(''), 3000);
    return () => clearTimeout(timer);
  }, [successMessage]);

  // ---------------------------------------------------------------------------
  // Validation
  // ---------------------------------------------------------------------------

  function validate(): string | null {
    if (statuses.length < 2) return t('admin.shops.orderLifecycle.minTwoStatuses');
    if (!statuses.some((s) => s.isTerminal))
      return t('admin.shops.orderLifecycle.needTerminal');
    if (statuses.some((s) => !s.name.trim()))
      return t('admin.shops.orderLifecycle.nameRequired');
    return null;
  }

  // ---------------------------------------------------------------------------
  // Handlers
  // ---------------------------------------------------------------------------

  function handleAddStatus() {
    const newSortOrder = statuses.length;
    setStatuses((prev) => [
      ...prev,
      {
        localId: nextLocalId(),
        name: '',
        systemKey: null,
        sortOrder: newSortOrder,
        isTerminal: false,
        colorHex: '',
      },
    ]);
  }

  function handleRemoveStatus(index: number) {
    const removing = statuses[index] as LocalStatus | undefined;
    if (!removing) return;
    // Don't allow removing if only 2 left
    if (statuses.length <= 2) return;

    const removedSortOrder = removing.sortOrder;
    const updated: LocalStatus[] = statuses
      .filter((_, i) => i !== index)
      .map((s, i) => ({ ...s, sortOrder: i }));
    setStatuses(updated);

    // Remove transitions referencing the removed sort order and remap
    setTransitions((prev) =>
      prev
        .filter(
          (tr) =>
            tr.fromSortOrder !== removedSortOrder &&
            tr.toSortOrder !== removedSortOrder,
        )
        .map((tr) => ({
          ...tr,
          fromSortOrder:
            tr.fromSortOrder > removedSortOrder
              ? tr.fromSortOrder - 1
              : tr.fromSortOrder,
          toSortOrder:
            tr.toSortOrder > removedSortOrder
              ? tr.toSortOrder - 1
              : tr.toSortOrder,
        })),
    );
  }

  function handleMoveStatus(index: number, direction: 'up' | 'down') {
    const swapIdx = direction === 'up' ? index - 1 : index + 1;
    if (swapIdx < 0 || swapIdx >= statuses.length) return;

    const current = statuses[index];
    const swap = statuses[swapIdx];
    if (!current || !swap) return;

    const updated: LocalStatus[] = [...statuses];
    const oldSort = current.sortOrder;
    const newSort = swap.sortOrder;
    updated[index] = { ...current, sortOrder: newSort };
    updated[swapIdx] = { ...swap, sortOrder: oldSort };
    updated.sort((a, b) => a.sortOrder - b.sortOrder);
    setStatuses(updated);

    // Remap transitions
    setTransitions((prev) =>
      prev.map((tr) => ({
        ...tr,
        fromSortOrder:
          tr.fromSortOrder === oldSort
            ? newSort
            : tr.fromSortOrder === newSort
              ? oldSort
              : tr.fromSortOrder,
        toSortOrder:
          tr.toSortOrder === oldSort
            ? newSort
            : tr.toSortOrder === newSort
              ? oldSort
              : tr.toSortOrder,
      })),
    );
  }

  function handleAddTransition() {
    if (statuses.length < 2) return;
    setTransitions((prev) => [
      ...prev,
      { localId: nextLocalId(), fromSortOrder: 0, toSortOrder: 1 },
    ]);
  }

  function handleRemoveTransition(index: number) {
    setTransitions((prev) => prev.filter((_, i) => i !== index));
  }

  function handleSave() {
    const error = validate();
    if (error) {
      setSuccessMessage('');
      return;
    }

    const apiStatuses: OrderStatusRequest[] = statuses.map((s) => ({
      name: s.name.trim(),
      systemKey: s.systemKey,
      sortOrder: s.sortOrder,
      isTerminal: s.isTerminal,
      colorHex: s.colorHex || null,
    }));

    const apiTransitions: OrderStatusTransitionRequest[] = transitions.map((tr) => ({
      fromSortOrder: tr.fromSortOrder,
      toSortOrder: tr.toSortOrder,
    }));

    configureMutation.mutate(
      { statuses: apiStatuses, transitions: apiTransitions },
      {
        onSuccess: () => {
          setSuccessMessage(t('admin.shops.orderLifecycle.saved'));
          setFormInitialized(false);
        },
      },
    );
  }

  function handleReset() {
    resetMutation.mutate(undefined, {
      onSuccess: () => {
        setFormInitialized(false);
        setShowResetConfirm(false);
        setSuccessMessage(t('admin.shops.orderLifecycle.saved'));
      },
    });
  }

  // ---------------------------------------------------------------------------
  // Render
  // ---------------------------------------------------------------------------

  if (lifecycle.isLoading) {
    return (
      <p style={{ padding: '1.5rem', color: '#6b7280' }}>
        {t('admin.shops.orderLifecycle.loading', 'Loading...')}
      </p>
    );
  }

  if (lifecycle.isError) {
    return (
      <p style={{ padding: '1.5rem', color: '#dc2626' }}>
        {t('admin.shops.orderLifecycle.loadError', 'Failed to load order lifecycle.')}
      </p>
    );
  }

  const validationError = validate();

  return (
    <div style={{ maxWidth: '56rem', margin: '0 auto', padding: '1.5rem' }}>
      {/* Header */}
      <div
        style={{
          display: 'flex',
          alignItems: 'center',
          gap: '1rem',
          marginBottom: '1.5rem',
        }}
      >
        <button
          type="button"
          onClick={() => navigate(`/${brandSlug}/${lang}/admin/shops/${shopId}`)}
          style={{
            background: 'none',
            border: 'none',
            cursor: 'pointer',
            fontSize: '1.25rem',
            color: '#6b7280',
          }}
        >
          &larr;
        </button>
        <h2 style={{ margin: 0, fontSize: '1.5rem', fontWeight: 600 }}>
          {t('admin.shops.orderLifecycle.title')}
        </h2>
      </div>

      {/* Success message */}
      {successMessage && (
        <p
          style={{
            color: '#16a34a',
            marginBottom: '1rem',
            fontSize: '0.875rem',
            fontWeight: 500,
          }}
        >
          {successMessage}
        </p>
      )}

      {/* Validation error */}
      {validationError && (
        <p
          style={{
            color: '#dc2626',
            marginBottom: '1rem',
            fontSize: '0.875rem',
          }}
        >
          {validationError}
        </p>
      )}

      {/* Visual flow */}
      <div
        style={{
          display: 'flex',
          flexWrap: 'wrap',
          gap: '0.5rem',
          alignItems: 'center',
          marginBottom: '1.5rem',
          padding: '1rem',
          background: '#f9fafb',
          borderRadius: '0.5rem',
          border: '1px solid #e5e7eb',
        }}
      >
        {statuses.map((s, i) => (
          <div key={s.localId} style={{ display: 'flex', alignItems: 'center', gap: '0.5rem' }}>
            <span
              style={{
                padding: '0.375rem 0.75rem',
                borderRadius: '9999px',
                fontSize: '0.8125rem',
                fontWeight: 500,
                background: s.colorHex || (s.isTerminal ? '#dcfce7' : '#dbeafe'),
                color: s.isTerminal ? '#166534' : '#1e40af',
                border: `1px solid ${s.isTerminal ? '#bbf7d0' : '#bfdbfe'}`,
              }}
            >
              {s.name || `Status ${s.sortOrder}`}
              {s.isTerminal ? ' *' : ''}
            </span>
            {i < statuses.length - 1 && (
              <span style={{ color: '#9ca3af', fontSize: '1.25rem' }}>&rarr;</span>
            )}
          </div>
        ))}
      </div>

      {/* Statuses list */}
      <h3
        style={{
          fontSize: '1.125rem',
          fontWeight: 600,
          marginBottom: '0.75rem',
        }}
      >
        {t('admin.shops.orderLifecycle.statuses')}
      </h3>

      {statuses.map((status, index) => (
        <div
          key={status.localId}
          style={{
            display: 'flex',
            gap: '0.75rem',
            alignItems: 'center',
            marginBottom: '0.5rem',
            padding: '0.75rem',
            border: '1px solid #e5e7eb',
            borderRadius: '0.375rem',
            background: '#fff',
          }}
        >
          {/* Sort order */}
          <span
            style={{
              fontSize: '0.75rem',
              color: '#9ca3af',
              minWidth: '1.5rem',
              textAlign: 'center',
            }}
          >
            {status.sortOrder}
          </span>

          {/* Name */}
          <input
            type="text"
            value={status.name}
            placeholder={t('admin.shops.orderLifecycle.statusName')}
            onChange={(e) => {
              setStatuses((prev) =>
                prev.map((s, i) => (i === index ? { ...s, name: e.target.value } : s)),
              );
            }}
            disabled={status.systemKey !== null}
            style={{
              flex: 1,
              padding: '0.375rem 0.5rem',
              border: '1px solid #d1d5db',
              borderRadius: '0.25rem',
              fontSize: '0.875rem',
              background: status.systemKey !== null ? '#f3f4f6' : '#fff',
            }}
          />

          {/* Color */}
          <input
            type="color"
            value={status.colorHex || '#3b82f6'}
            onChange={(e) => {
              setStatuses((prev) =>
                prev.map((s, i) => (i === index ? { ...s, colorHex: e.target.value } : s)),
              );
            }}
            title={t('admin.shops.orderLifecycle.color')}
            style={{
              width: '2rem',
              height: '2rem',
              border: '1px solid #d1d5db',
              borderRadius: '0.25rem',
              cursor: 'pointer',
              padding: 0,
            }}
          />

          {/* Terminal toggle */}
          <label
            style={{
              display: 'flex',
              alignItems: 'center',
              gap: '0.25rem',
              fontSize: '0.75rem',
              color: '#6b7280',
              whiteSpace: 'nowrap',
            }}
          >
            <input
              type="checkbox"
              checked={status.isTerminal}
              onChange={(e) => {
                setStatuses((prev) =>
                  prev.map((s, i) => (i === index ? { ...s, isTerminal: e.target.checked } : s)),
                );
              }}
            />
            {t('admin.shops.orderLifecycle.terminal')}
          </label>

          {/* Move buttons */}
          <button
            type="button"
            onClick={() => handleMoveStatus(index, 'up')}
            disabled={index === 0}
            style={{
              padding: '0.25rem 0.5rem',
              border: '1px solid #d1d5db',
              borderRadius: '0.25rem',
              background: '#fff',
              cursor: index === 0 ? 'not-allowed' : 'pointer',
              opacity: index === 0 ? 0.4 : 1,
              fontSize: '0.75rem',
            }}
          >
            &uarr;
          </button>
          <button
            type="button"
            onClick={() => handleMoveStatus(index, 'down')}
            disabled={index === statuses.length - 1}
            style={{
              padding: '0.25rem 0.5rem',
              border: '1px solid #d1d5db',
              borderRadius: '0.25rem',
              background: '#fff',
              cursor: index === statuses.length - 1 ? 'not-allowed' : 'pointer',
              opacity: index === statuses.length - 1 ? 0.4 : 1,
              fontSize: '0.75rem',
            }}
          >
            &darr;
          </button>

          {/* Remove (only custom statuses, only if > 2) */}
          <button
            type="button"
            onClick={() => handleRemoveStatus(index)}
            disabled={statuses.length <= 2}
            style={{
              padding: '0.25rem 0.5rem',
              border: '1px solid #fca5a5',
              borderRadius: '0.25rem',
              background: '#fff',
              color: '#dc2626',
              cursor: statuses.length <= 2 ? 'not-allowed' : 'pointer',
              opacity: statuses.length <= 2 ? 0.4 : 1,
              fontSize: '0.75rem',
            }}
          >
            {t('admin.shops.orderLifecycle.remove')}
          </button>
        </div>
      ))}

      <button
        type="button"
        onClick={handleAddStatus}
        style={{
          marginTop: '0.5rem',
          marginBottom: '1.5rem',
          padding: '0.375rem 1rem',
          border: '1px dashed #d1d5db',
          borderRadius: '0.375rem',
          background: '#fff',
          color: '#374151',
          cursor: 'pointer',
          fontSize: '0.875rem',
        }}
      >
        + {t('admin.shops.orderLifecycle.addStatus')}
      </button>

      {/* Transitions */}
      <h3
        style={{
          fontSize: '1.125rem',
          fontWeight: 600,
          marginBottom: '0.75rem',
        }}
      >
        {t('admin.shops.orderLifecycle.transitions')}
      </h3>

      {transitions.map((tr, index) => (
        <div
          key={tr.localId}
          style={{
            display: 'flex',
            gap: '0.75rem',
            alignItems: 'center',
            marginBottom: '0.5rem',
          }}
        >
          <select
            value={tr.fromSortOrder}
            onChange={(e) => {
              setTransitions((prev) =>
                prev.map((t, i) =>
                  i === index ? { ...t, fromSortOrder: Number(e.target.value) } : t,
                ),
              );
            }}
            style={{
              padding: '0.375rem 0.5rem',
              border: '1px solid #d1d5db',
              borderRadius: '0.25rem',
              fontSize: '0.875rem',
            }}
          >
            {statuses.map((s) => (
              <option key={s.sortOrder} value={s.sortOrder}>
                {s.name || `Status ${s.sortOrder}`}
              </option>
            ))}
          </select>

          <span style={{ color: '#9ca3af' }}>&rarr;</span>

          <select
            value={tr.toSortOrder}
            onChange={(e) => {
              setTransitions((prev) =>
                prev.map((t, i) =>
                  i === index ? { ...t, toSortOrder: Number(e.target.value) } : t,
                ),
              );
            }}
            style={{
              padding: '0.375rem 0.5rem',
              border: '1px solid #d1d5db',
              borderRadius: '0.25rem',
              fontSize: '0.875rem',
            }}
          >
            {statuses.map((s) => (
              <option key={s.sortOrder} value={s.sortOrder}>
                {s.name || `Status ${s.sortOrder}`}
              </option>
            ))}
          </select>

          <button
            type="button"
            onClick={() => handleRemoveTransition(index)}
            style={{
              padding: '0.25rem 0.5rem',
              border: '1px solid #fca5a5',
              borderRadius: '0.25rem',
              background: '#fff',
              color: '#dc2626',
              cursor: 'pointer',
              fontSize: '0.75rem',
            }}
          >
            {t('admin.shops.orderLifecycle.remove')}
          </button>
        </div>
      ))}

      <button
        type="button"
        onClick={handleAddTransition}
        style={{
          marginTop: '0.5rem',
          marginBottom: '1.5rem',
          padding: '0.375rem 1rem',
          border: '1px dashed #d1d5db',
          borderRadius: '0.375rem',
          background: '#fff',
          color: '#374151',
          cursor: 'pointer',
          fontSize: '0.875rem',
        }}
      >
        + {t('admin.shops.orderLifecycle.addTransition')}
      </button>

      {/* Action buttons */}
      <div style={{ display: 'flex', gap: '0.75rem', marginTop: '1rem' }}>
        <button
          type="button"
          onClick={handleSave}
          disabled={configureMutation.isPending || !!validationError}
          style={{
            padding: '0.5rem 1.5rem',
            background: configureMutation.isPending ? '#9ca3af' : '#2563eb',
            color: '#fff',
            border: 'none',
            borderRadius: '0.375rem',
            cursor: configureMutation.isPending ? 'not-allowed' : 'pointer',
            fontSize: '0.875rem',
            fontWeight: 500,
          }}
        >
          {configureMutation.isPending
            ? t('admin.shops.orderLifecycle.saving')
            : t('admin.shops.orderLifecycle.save')}
        </button>

        <button
          type="button"
          onClick={() => setShowResetConfirm(true)}
          disabled={resetMutation.isPending}
          style={{
            padding: '0.5rem 1.5rem',
            background: '#fff',
            color: '#dc2626',
            border: '1px solid #fca5a5',
            borderRadius: '0.375rem',
            cursor: 'pointer',
            fontSize: '0.875rem',
          }}
        >
          {t('admin.shops.orderLifecycle.resetToDefault')}
        </button>
      </div>

      {/* API error */}
      {configureMutation.isError && (
        <p style={{ color: '#dc2626', marginTop: '0.75rem', fontSize: '0.875rem' }}>
          {configureMutation.error instanceof Error
            ? configureMutation.error.message
            : t('admin.shops.orderLifecycle.saveError', 'Failed to save. Please try again.')}
        </p>
      )}

      {/* Reset confirmation dialog */}
      {showResetConfirm && (
        <div
          style={{
            position: 'fixed',
            inset: 0,
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'center',
            background: 'rgba(0,0,0,0.4)',
            zIndex: 50,
          }}
        >
          <div
            style={{
              background: '#fff',
              borderRadius: '0.5rem',
              padding: '1.5rem',
              maxWidth: '24rem',
              width: '90%',
              boxShadow: '0 10px 25px rgba(0,0,0,0.15)',
            }}
          >
            <h3 style={{ margin: '0 0 0.75rem', fontSize: '1.125rem', fontWeight: 600 }}>
              {t('admin.shops.orderLifecycle.resetConfirmTitle')}
            </h3>
            <p style={{ margin: '0 0 1.25rem', fontSize: '0.875rem', color: '#6b7280' }}>
              {t('admin.shops.orderLifecycle.resetConfirmMessage')}
            </p>
            <div style={{ display: 'flex', gap: '0.75rem', justifyContent: 'flex-end' }}>
              <button
                type="button"
                onClick={() => setShowResetConfirm(false)}
                style={{
                  padding: '0.375rem 1rem',
                  border: '1px solid #d1d5db',
                  borderRadius: '0.375rem',
                  background: '#fff',
                  cursor: 'pointer',
                  fontSize: '0.875rem',
                }}
              >
                {t('admin.shops.orderLifecycle.cancel')}
              </button>
              <button
                type="button"
                onClick={handleReset}
                disabled={resetMutation.isPending}
                style={{
                  padding: '0.375rem 1rem',
                  border: 'none',
                  borderRadius: '0.375rem',
                  background: '#dc2626',
                  color: '#fff',
                  cursor: 'pointer',
                  fontSize: '0.875rem',
                }}
              >
                {resetMutation.isPending
                  ? t('admin.shops.orderLifecycle.resetting', 'Resetting...')
                  : t('admin.shops.orderLifecycle.confirmReset')}
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
