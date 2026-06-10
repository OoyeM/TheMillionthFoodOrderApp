import type React from 'react';
import { useState, useEffect } from 'react';
import { useTranslation } from 'react-i18next';
import {
  useProductModifierGroups,
  useModifierGroups,
  useSetProductModifierGroups,
} from '../hooks/useModifierGroups';
import type { ProductModifierGroupResponse } from '../../../types/common';

// ---------------------------------------------------------------------------
// ModifierGroupAssignments — the modifier-group assignment section shared by
// ProductEdit and ComboProductEdit. It owns its own data + mutation (modifier
// assignments are a separate resource from the product form) and the
// assigned-group ordering UI.
// ---------------------------------------------------------------------------

const reorderButtonStyle: React.CSSProperties = {
  padding: '0.125rem 0.4rem',
  fontSize: '0.75rem',
  background: '#fff',
  border: '1px solid #d1d5db',
  borderRadius: '0.25rem',
  lineHeight: 1,
};

interface ModifierGroupAssignmentsProps {
  brandSlug: string;
  productId: string;
}

export function ModifierGroupAssignments({
  brandSlug,
  productId,
}: ModifierGroupAssignmentsProps): React.JSX.Element {
  const { t } = useTranslation();

  const { data: productModifierGroups } = useProductModifierGroups(brandSlug, productId);
  const { data: allModifierGroups } = useModifierGroups(brandSlug);
  const setProductModifierGroups = useSetProductModifierGroups(brandSlug, productId);
  const [assignedGroups, setAssignedGroups] = useState<ProductModifierGroupResponse[]>([]);
  const [assignmentsInitialized, setAssignmentsInitialized] = useState(false);
  const [selectedGroupToAdd, setSelectedGroupToAdd] = useState('');

  // Populate assigned modifier groups when data arrives
  useEffect(() => {
    if (productModifierGroups !== undefined && !assignmentsInitialized) {
      setAssignedGroups(productModifierGroups);
      setAssignmentsInitialized(true);
    }
  }, [productModifierGroups, assignmentsInitialized]);

  function handleAddModifierGroup() {
    if (!selectedGroupToAdd) return;
    const alreadyAssigned = assignedGroups.some((g) => g.modifierGroupId === selectedGroupToAdd);
    if (alreadyAssigned) return;

    const groupInfo = allModifierGroups?.find((g) => g.id === selectedGroupToAdd);
    if (!groupInfo) return;

    const newGroup: ProductModifierGroupResponse = {
      modifierGroupId: groupInfo.id,
      name: groupInfo.name,
      sortOrder: assignedGroups.length,
      modifiers: [],
    };
    setAssignedGroups((prev) => [...prev, newGroup]);
    setSelectedGroupToAdd('');
  }

  function handleRemoveAssignedGroup(modifierGroupId: string) {
    setAssignedGroups((prev) =>
      prev
        .filter((g) => g.modifierGroupId !== modifierGroupId)
        .map((g, index) => ({ ...g, sortOrder: index })),
    );
  }

  function handleMoveGroupUp(index: number) {
    if (index === 0) return;
    setAssignedGroups((prev) => {
      const next = [...prev];
      const current = next[index];
      const above = next[index - 1];
      if (current === undefined || above === undefined) return prev;
      next[index - 1] = current;
      next[index] = above;
      return next.map((g, i) => ({ ...g, sortOrder: i }));
    });
  }

  function handleMoveGroupDown(index: number) {
    setAssignedGroups((prev) => {
      if (index >= prev.length - 1) return prev;
      const next = [...prev];
      const current = next[index];
      const below = next[index + 1];
      if (current === undefined || below === undefined) return prev;
      next[index] = below;
      next[index + 1] = current;
      return next.map((g, i) => ({ ...g, sortOrder: i }));
    });
  }

  function handleSaveAssignments() {
    setProductModifierGroups.mutate({
      assignments: assignedGroups.map((g) => ({
        modifierGroupId: g.modifierGroupId,
        sortOrder: g.sortOrder,
      })),
    });
  }

  return (
    <section
      style={{
        marginTop: '2.5rem',
        borderTop: '2px solid #e5e7eb',
        paddingTop: '1.5rem',
      }}
    >
      <h2 style={{ fontSize: '1.125rem', fontWeight: 700, marginBottom: '1rem' }}>
        {t('admin.modifierGroups.title')}
      </h2>

      {/* Assigned groups list */}
      {assignedGroups.length === 0 ? (
        <p style={{ color: '#6b7280', marginBottom: '1rem' }}>
          {t('admin.modifierGroups.noAssignedGroups')}
        </p>
      ) : (
        <div style={{ marginBottom: '1rem' }}>
          {assignedGroups.map((group, index) => {
            // The assignment endpoint returns only { modifierGroupId, sortOrder };
            // resolve the display name + modifier count from the full group list.
            const info = allModifierGroups?.find((g) => g.id === group.modifierGroupId);
            // eslint-disable-next-line @typescript-eslint/no-unnecessary-condition -- group is API/JSON data; name may be absent at runtime despite the type
            const groupName = info?.name ?? group.name ?? '';
            // eslint-disable-next-line @typescript-eslint/no-unnecessary-condition -- group is API/JSON data; modifiers may be absent at runtime despite the type
            const modifierCount = info?.modifierCount ?? group.modifiers?.length ?? 0;
            return (
            <div
              key={group.modifierGroupId}
              style={{
                display: 'flex',
                alignItems: 'center',
                gap: '0.5rem',
                padding: '0.625rem 0.75rem',
                border: '1px solid #e5e7eb',
                borderRadius: '0.375rem',
                marginBottom: '0.5rem',
                background: '#f9fafb',
              }}
            >
              <div style={{ flex: 1 }}>
                <span style={{ fontWeight: 600, fontSize: '0.9rem' }}>{groupName}</span>
                {modifierCount > 0 && (
                  <span style={{ color: '#6b7280', fontSize: '0.75rem', marginLeft: '0.5rem' }}>
                    {modifierCount} {t('admin.modifierGroups.modifiers').toLowerCase()}
                  </span>
                )}
              </div>
              <button
                type="button"
                onClick={() => { handleMoveGroupUp(index); }}
                disabled={index === 0}
                style={{
                  ...reorderButtonStyle,
                  opacity: index === 0 ? 0.3 : 1,
                  cursor: index === 0 ? 'not-allowed' : 'pointer',
                }}
              >
                &#9650;
              </button>
              <button
                type="button"
                onClick={() => { handleMoveGroupDown(index); }}
                disabled={index === assignedGroups.length - 1}
                style={{
                  ...reorderButtonStyle,
                  opacity: index === assignedGroups.length - 1 ? 0.3 : 1,
                  cursor: index === assignedGroups.length - 1 ? 'not-allowed' : 'pointer',
                }}
              >
                &#9660;
              </button>
              <button
                type="button"
                onClick={() => { handleRemoveAssignedGroup(group.modifierGroupId); }}
                style={{
                  padding: '0.125rem 0.5rem',
                  fontSize: '0.75rem',
                  background: '#fff',
                  border: '1px solid #fca5a5',
                  borderRadius: '0.25rem',
                  color: '#dc2626',
                  cursor: 'pointer',
                }}
              >
                {t('admin.modifierGroups.removeModifier')}
              </button>
            </div>
            );
          })}
        </div>
      )}

      {/* Add group dropdown */}
      {allModifierGroups !== undefined && allModifierGroups.length > 0 && (
        <div style={{ display: 'flex', gap: '0.5rem', marginBottom: '1rem', alignItems: 'center' }}>
          <select
            value={selectedGroupToAdd}
            onChange={(e) => { setSelectedGroupToAdd(e.target.value); }}
            style={{
              padding: '0.5rem 0.75rem',
              border: '1px solid #d1d5db',
              borderRadius: '0.375rem',
              fontSize: '0.9rem',
              flex: 1,
              maxWidth: '20rem',
            }}
          >
            <option value="">-- Select a modifier group --</option>
            {allModifierGroups
              .filter((g) => !assignedGroups.some((a) => a.modifierGroupId === g.id))
              .map((g) => (
                <option key={g.id} value={g.id}>
                  {g.name}
                </option>
              ))}
          </select>
          <button
            type="button"
            onClick={handleAddModifierGroup}
            disabled={!selectedGroupToAdd}
            style={{
              padding: '0.5rem 1rem',
              background: '#f9fafb',
              border: '1px solid #d1d5db',
              borderRadius: '0.375rem',
              cursor: selectedGroupToAdd ? 'pointer' : 'not-allowed',
              opacity: selectedGroupToAdd ? 1 : 0.5,
              fontSize: '0.875rem',
            }}
          >
            + Add
          </button>
        </div>
      )}

      {/* Save assignments */}
      {setProductModifierGroups.isError && (
        <p style={{ color: '#dc2626', marginBottom: '0.75rem', fontSize: '0.875rem' }}>
          {setProductModifierGroups.error instanceof Error
            ? setProductModifierGroups.error.message
            : 'Failed to save assignments. Please try again.'}
        </p>
      )}

      {setProductModifierGroups.isSuccess && (
        <p style={{ color: '#16a34a', marginBottom: '0.75rem', fontSize: '0.875rem' }}>
          Assignments saved.
        </p>
      )}

      <button
        type="button"
        onClick={handleSaveAssignments}
        disabled={setProductModifierGroups.isPending}
        style={{
          padding: '0.5rem 1.25rem',
          background: '#111827',
          color: '#fff',
          border: 'none',
          borderRadius: '0.375rem',
          cursor: setProductModifierGroups.isPending ? 'not-allowed' : 'pointer',
          fontWeight: 600,
          opacity: setProductModifierGroups.isPending ? 0.6 : 1,
        }}
      >
        {setProductModifierGroups.isPending ? 'Saving...' : t('admin.modifierGroups.saveAssignments')}
      </button>
    </section>
  );
}
