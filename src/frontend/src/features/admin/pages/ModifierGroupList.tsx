import { useNavigate, useParams } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { useModifierGroups, useDeleteModifierGroup } from '../hooks/useModifierGroups';
import type { ModifierGroupListItem } from '../../../types/common';

// ---------------------------------------------------------------------------
// Sub-component: a single row
// ---------------------------------------------------------------------------

interface ModifierGroupRowProps {
  group: ModifierGroupListItem;
  onRowClick: (id: string) => void;
  onDelete: (id: string) => void;
  isDeleting: boolean;
}

function ModifierGroupRow({ group, onRowClick, onDelete, isDeleting }: ModifierGroupRowProps) {
  const { t } = useTranslation();

  function handleDelete(e: React.MouseEvent) {
    e.stopPropagation();
    const message = t('admin.modifierGroups.confirmDelete', { name: group.name });
    if (window.confirm(message)) {
      onDelete(group.id);
    }
  }

  return (
    <tr
      onClick={() => { onRowClick(group.id); }}
      style={{ cursor: 'pointer', borderBottom: '1px solid #e5e7eb' }}
    >
      <td style={{ padding: '0.75rem 1rem' }}>{group.name}</td>
      <td style={{ padding: '0.75rem 1rem' }}>{group.modifierCount}</td>
      <td style={{ padding: '0.75rem 1rem' }}>{group.productCount}</td>
      <td style={{ padding: '0.75rem 1rem', color: '#6b7280' }}>
        {new Date(group.createdAt).toLocaleDateString()}
      </td>
      <td style={{ padding: '0.75rem 1rem' }}>
        <button
          onClick={handleDelete}
          disabled={isDeleting}
          style={{
            padding: '0.25rem 0.75rem',
            fontSize: '0.875rem',
            borderRadius: '0.25rem',
            border: '1px solid #fca5a5',
            background: '#fff',
            color: '#dc2626',
            cursor: isDeleting ? 'not-allowed' : 'pointer',
            opacity: isDeleting ? 0.6 : 1,
          }}
        >
          {t('admin.modifierGroups.delete')}
        </button>
      </td>
    </tr>
  );
}

// ---------------------------------------------------------------------------
// Main page
// ---------------------------------------------------------------------------

export function ModifierGroupList() {
  const navigate = useNavigate();
  const { t } = useTranslation();
  const { brandSlug, lang } = useParams<{ brandSlug: string; lang: string }>();

  const resolvedBrandSlug = brandSlug ?? '';
  const { data: groups, isLoading, isError, error } = useModifierGroups(resolvedBrandSlug);
  const deleteModifierGroup = useDeleteModifierGroup(resolvedBrandSlug);

  function handleCreateClick() {
    navigate(`/${String(brandSlug)}/${String(lang)}/admin/modifier-groups/new`);
  }

  function handleRowClick(id: string) {
    navigate(`/${String(brandSlug)}/${String(lang)}/admin/modifier-groups/${id}`);
  }

  function handleDelete(id: string) {
    deleteModifierGroup.mutate(id);
  }

  return (
    <main style={{ padding: '1.5rem' }}>
      <div
        style={{
          display: 'flex',
          justifyContent: 'space-between',
          alignItems: 'center',
          marginBottom: '1.5rem',
        }}
      >
        <h1 style={{ margin: 0, fontSize: '1.5rem', fontWeight: 700 }}>
          {t('admin.modifierGroups.title')}
        </h1>
        <button
          onClick={handleCreateClick}
          style={{
            padding: '0.5rem 1rem',
            background: '#111827',
            color: '#fff',
            border: 'none',
            borderRadius: '0.375rem',
            cursor: 'pointer',
            fontWeight: 600,
          }}
        >
          + {t('admin.modifierGroups.create')}
        </button>
      </div>

      {isLoading && <p style={{ color: '#6b7280' }}>Loading...</p>}

      {isError && (
        <p style={{ color: '#dc2626' }}>
          {error instanceof Error ? error.message : 'Unknown error'}
        </p>
      )}

      {!isLoading && !isError && groups?.length === 0 && (
        <p style={{ color: '#6b7280' }}>{t('admin.modifierGroups.noModifierGroups')}</p>
      )}

      {!isLoading && !isError && groups !== undefined && groups.length > 0 && (
        <div style={{ overflowX: 'auto' }}>
          <table
            style={{
              width: '100%',
              borderCollapse: 'collapse',
              fontSize: '0.9rem',
            }}
          >
            <thead>
              <tr style={{ borderBottom: '2px solid #e5e7eb', textAlign: 'left' }}>
                <th style={{ padding: '0.5rem 1rem', fontWeight: 600 }}>
                  {t('admin.modifierGroups.name')}
                </th>
                <th style={{ padding: '0.5rem 1rem', fontWeight: 600 }}>
                  {t('admin.modifierGroups.modifierCount')}
                </th>
                <th style={{ padding: '0.5rem 1rem', fontWeight: 600 }}>
                  {t('admin.modifierGroups.productCount')}
                </th>
                <th style={{ padding: '0.5rem 1rem', fontWeight: 600 }}>Created</th>
                <th style={{ padding: '0.5rem 1rem', fontWeight: 600 }}>Actions</th>
              </tr>
            </thead>
            <tbody>
              {groups.map((group) => (
                <ModifierGroupRow
                  key={group.id}
                  group={group}
                  onRowClick={handleRowClick}
                  onDelete={handleDelete}
                  isDeleting={deleteModifierGroup.isPending}
                />
              ))}
            </tbody>
          </table>
        </div>
      )}
    </main>
  );
}
