import { useNavigate, useParams } from 'react-router-dom';
import { useShops, useDeactivateShop, useActivateShop } from '../hooks/useShops';
import type { Shop } from '../../../types/common';

// ---------------------------------------------------------------------------
// Sub-component: a single row that owns its own activate/deactivate mutations
// ---------------------------------------------------------------------------

interface ShopRowProps {
  shop: Shop;
  brandSlug: string;
  onRowClick: (id: string) => void;
}

function ShopRow({ shop, brandSlug, onRowClick }: ShopRowProps) {
  const deactivate = useDeactivateShop(brandSlug, shop.id);
  const activate = useActivateShop(brandSlug, shop.id);

  const isBusy = deactivate.isPending || activate.isPending;

  function handleToggle(e: React.MouseEvent) {
    // Prevent the row click handler from firing
    e.stopPropagation();
    if (shop.isActive) {
      deactivate.mutate();
    } else {
      activate.mutate();
    }
  }

  return (
    <tr
      onClick={() => { onRowClick(shop.id); }}
      style={{
        cursor: 'pointer',
        borderBottom: '1px solid #e5e7eb',
      }}
    >
      <td style={{ padding: '0.75rem 1rem' }}>{shop.name}</td>
      <td style={{ padding: '0.75rem 1rem', color: '#6b7280', fontFamily: 'monospace', fontSize: '0.875rem' }}>
        {shop.slug}
      </td>
      <td style={{ padding: '0.75rem 1rem' }}>{shop.address.city}</td>
      <td style={{ padding: '0.75rem 1rem' }}>
        <span
          style={{
            display: 'inline-block',
            padding: '0.125rem 0.5rem',
            borderRadius: '9999px',
            fontSize: '0.75rem',
            fontWeight: 600,
            background: shop.isActive ? '#d1fae5' : '#fee2e2',
            color: shop.isActive ? '#065f46' : '#991b1b',
          }}
        >
          {shop.isActive ? 'Active' : 'Inactive'}
        </span>
      </td>
      <td style={{ padding: '0.75rem 1rem', color: '#6b7280' }}>{shop.contactEmail}</td>
      <td style={{ padding: '0.75rem 1rem' }}>
        <button
          onClick={handleToggle}
          disabled={isBusy}
          style={{
            padding: '0.25rem 0.75rem',
            fontSize: '0.875rem',
            borderRadius: '0.25rem',
            border: '1px solid #d1d5db',
            background: '#fff',
            cursor: isBusy ? 'not-allowed' : 'pointer',
            opacity: isBusy ? 0.6 : 1,
          }}
        >
          {shop.isActive ? 'Deactivate' : 'Activate'}
        </button>
      </td>
    </tr>
  );
}

// ---------------------------------------------------------------------------
// Main page
// ---------------------------------------------------------------------------

export function ShopList() {
  const navigate = useNavigate();
  const { brandSlug, lang } = useParams<{ brandSlug: string; lang: string }>();

  const resolvedBrandSlug = brandSlug ?? '';
  const { data: shops, isLoading, isError, error } = useShops(resolvedBrandSlug);

  function handleCreateClick() {
    navigate(`/${brandSlug}/${lang}/admin/shops/new`);
  }

  function handleRowClick(id: string) {
    navigate(`/${brandSlug}/${lang}/admin/shops/${id}`);
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
        <h1 style={{ margin: 0, fontSize: '1.5rem', fontWeight: 700 }}>Shops</h1>
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
          + Create Shop
        </button>
      </div>

      {isLoading && <p style={{ color: '#6b7280' }}>Loading shops…</p>}

      {isError && (
        <p style={{ color: '#dc2626' }}>
          Failed to load shops:{' '}
          {error instanceof Error ? error.message : 'Unknown error'}
        </p>
      )}

      {!isLoading && !isError && shops?.length === 0 && (
        <p style={{ color: '#6b7280' }}>No shops yet. Create the first one.</p>
      )}

      {!isLoading && !isError && shops !== undefined && shops.length > 0 && (
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
                <th style={{ padding: '0.5rem 1rem', fontWeight: 600 }}>Name</th>
                <th style={{ padding: '0.5rem 1rem', fontWeight: 600 }}>Slug</th>
                <th style={{ padding: '0.5rem 1rem', fontWeight: 600 }}>City</th>
                <th style={{ padding: '0.5rem 1rem', fontWeight: 600 }}>Status</th>
                <th style={{ padding: '0.5rem 1rem', fontWeight: 600 }}>Contact Email</th>
                <th style={{ padding: '0.5rem 1rem', fontWeight: 600 }}>Actions</th>
              </tr>
            </thead>
            <tbody>
              {shops.map((shop) => (
                <ShopRow
                  key={shop.id}
                  shop={shop}
                  brandSlug={resolvedBrandSlug}
                  onRowClick={handleRowClick}
                />
              ))}
            </tbody>
          </table>
        </div>
      )}
    </main>
  );
}
