import { useNavigate, useParams } from 'react-router-dom';
import { useMenuCategories, useDeleteMenuCategory, useReorderMenuCategory } from '../hooks/useMenuCategories';
import type { MenuCategoryListItem } from '../../../types/common';

// ---------------------------------------------------------------------------
// Sub-component: a single row
// ---------------------------------------------------------------------------

interface MenuCategoryRowProps {
  category: MenuCategoryListItem;
  isFirst: boolean;
  isLast: boolean;
  onRowClick: (id: string) => void;
  onDelete: (id: string) => void;
  onMoveUp: (id: string) => void;
  onMoveDown: (id: string) => void;
  isDeleting: boolean;
  isReordering: boolean;
}

function MenuCategoryRow({
  category,
  isFirst,
  isLast,
  onRowClick,
  onDelete,
  onMoveUp,
  onMoveDown,
  isDeleting,
  isReordering,
}: MenuCategoryRowProps) {
  function handleDelete(e: React.MouseEvent) {
    e.stopPropagation();
    if (
      window.confirm(
        `Delete "${category.name}"? This will remove the category. Products assigned to it will be unassigned.`,
      )
    ) {
      onDelete(category.id);
    }
  }

  function handleMoveUp(e: React.MouseEvent) {
    e.stopPropagation();
    onMoveUp(category.id);
  }

  function handleMoveDown(e: React.MouseEvent) {
    e.stopPropagation();
    onMoveDown(category.id);
  }

  return (
    <tr
      onClick={() => { onRowClick(category.id); }}
      style={{ cursor: 'pointer', borderBottom: '1px solid #e5e7eb' }}
    >
      <td style={{ padding: '0.75rem 1rem', fontFamily: 'monospace', color: '#6b7280' }}>
        {category.sortOrder}
      </td>
      <td style={{ padding: '0.75rem 1rem', fontWeight: 500 }}>{category.name}</td>
      <td style={{ padding: '0.75rem 1rem' }}>
        {category.imageUrl ? (
          <img
            src={category.imageUrl}
            alt={category.name}
            style={{ width: 40, height: 40, objectFit: 'cover', borderRadius: '0.25rem' }}
          />
        ) : (
          <span style={{ color: '#9ca3af' }}>&mdash;</span>
        )}
      </td>
      <td style={{ padding: '0.75rem 1rem', textAlign: 'center' }}>
        {category.productCount}
      </td>
      <td style={{ padding: '0.75rem 1rem' }}>
        <div style={{ display: 'flex', gap: '0.375rem', alignItems: 'center' }}>
          {/* Reorder buttons */}
          <button
            onClick={handleMoveUp}
            disabled={isFirst || isReordering}
            title="Move up"
            style={{
              padding: '0.25rem 0.5rem',
              fontSize: '0.75rem',
              borderRadius: '0.25rem',
              border: '1px solid #d1d5db',
              background: '#fff',
              color: '#374151',
              cursor: isFirst || isReordering ? 'not-allowed' : 'pointer',
              opacity: isFirst || isReordering ? 0.4 : 1,
            }}
          >
            &#9650;
          </button>
          <button
            onClick={handleMoveDown}
            disabled={isLast || isReordering}
            title="Move down"
            style={{
              padding: '0.25rem 0.5rem',
              fontSize: '0.75rem',
              borderRadius: '0.25rem',
              border: '1px solid #d1d5db',
              background: '#fff',
              color: '#374151',
              cursor: isLast || isReordering ? 'not-allowed' : 'pointer',
              opacity: isLast || isReordering ? 0.4 : 1,
            }}
          >
            &#9660;
          </button>
          {/* Delete button */}
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
              marginLeft: '0.25rem',
            }}
          >
            Delete
          </button>
        </div>
      </td>
    </tr>
  );
}

// ---------------------------------------------------------------------------
// Main page
// ---------------------------------------------------------------------------

export function MenuCategoryList() {
  const navigate = useNavigate();
  const { brandSlug, lang } = useParams<{ brandSlug: string; lang: string }>();

  const resolvedBrandSlug = brandSlug ?? '';
  const { data: categories, isLoading, isError, error } = useMenuCategories(resolvedBrandSlug);
  const deleteCategory = useDeleteMenuCategory(resolvedBrandSlug);
  const reorderCategory = useReorderMenuCategory(resolvedBrandSlug);

  function handleCreateClick() {
    navigate(`/${brandSlug}/${lang}/admin/menu-categories/new`);
  }

  function handleRowClick(id: string) {
    navigate(`/${brandSlug}/${lang}/admin/menu-categories/${id}`);
  }

  function handleDelete(id: string) {
    deleteCategory.mutate(id);
  }

  function handleMoveUp(id: string) {
    if (!categories) return;
    const index = categories.findIndex((c) => c.id === id);
    if (index <= 0) return;
    const current = categories[index];
    const above = categories[index - 1];
    if (!current || !above) return;
    reorderCategory.mutate({ id: current.id, sortOrder: above.sortOrder });
    reorderCategory.mutate({ id: above.id, sortOrder: current.sortOrder });
  }

  function handleMoveDown(id: string) {
    if (!categories) return;
    const index = categories.findIndex((c) => c.id === id);
    if (index < 0 || index >= categories.length - 1) return;
    const current = categories[index];
    const below = categories[index + 1];
    if (!current || !below) return;
    reorderCategory.mutate({ id: current.id, sortOrder: below.sortOrder });
    reorderCategory.mutate({ id: below.id, sortOrder: current.sortOrder });
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
        <h1 style={{ margin: 0, fontSize: '1.5rem', fontWeight: 700 }}>Menu Categories</h1>
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
          + Create Category
        </button>
      </div>

      {isLoading && <p style={{ color: '#6b7280' }}>Loading menu categories...</p>}

      {isError && (
        <p style={{ color: '#dc2626' }}>
          Failed to load menu categories:{' '}
          {error instanceof Error ? error.message : 'Unknown error'}
        </p>
      )}

      {!isLoading && !isError && categories?.length === 0 && (
        <p style={{ color: '#6b7280' }}>No menu categories yet. Create the first one.</p>
      )}

      {!isLoading && !isError && categories !== undefined && categories.length > 0 && (
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
                <th style={{ padding: '0.5rem 1rem', fontWeight: 600 }}>Sort Order</th>
                <th style={{ padding: '0.5rem 1rem', fontWeight: 600 }}>Name</th>
                <th style={{ padding: '0.5rem 1rem', fontWeight: 600 }}>Image</th>
                <th style={{ padding: '0.5rem 1rem', fontWeight: 600, textAlign: 'center' }}>
                  Products
                </th>
                <th style={{ padding: '0.5rem 1rem', fontWeight: 600 }}>Actions</th>
              </tr>
            </thead>
            <tbody>
              {categories.map((category, index) => (
                <MenuCategoryRow
                  key={category.id}
                  category={category}
                  isFirst={index === 0}
                  isLast={index === categories.length - 1}
                  onRowClick={handleRowClick}
                  onDelete={handleDelete}
                  onMoveUp={handleMoveUp}
                  onMoveDown={handleMoveDown}
                  isDeleting={deleteCategory.isPending}
                  isReordering={reorderCategory.isPending}
                />
              ))}
            </tbody>
          </table>
        </div>
      )}
    </main>
  );
}
