import { useNavigate, useParams } from 'react-router-dom';
import { useBrands, useDeactivateBrand, useActivateBrand } from '../hooks/useBrands';
import type { Brand } from '../../../types/common';

// ---------------------------------------------------------------------------
// Sub-component: a single row that owns its own activate/deactivate mutations
// ---------------------------------------------------------------------------

interface BrandRowProps {
  brand: Brand;
  onRowClick: (id: string) => void;
}

function BrandRow({ brand, onRowClick }: BrandRowProps) {
  const deactivate = useDeactivateBrand(brand.id);
  const activate = useActivateBrand(brand.id);

  const isBusy = deactivate.isPending || activate.isPending;

  function handleToggle(e: React.MouseEvent) {
    // Prevent the row click handler from firing
    e.stopPropagation();
    if (brand.isActive) {
      deactivate.mutate();
    } else {
      activate.mutate();
    }
  }

  return (
    <tr
      onClick={() => { onRowClick(brand.id); }}
      style={{
        cursor: 'pointer',
        borderBottom: '1px solid #e5e7eb',
      }}
    >
      <td style={{ padding: '0.75rem 1rem' }}>{brand.name}</td>
      <td style={{ padding: '0.75rem 1rem', color: '#6b7280', fontFamily: 'monospace', fontSize: '0.875rem' }}>
        {brand.slug}
      </td>
      <td style={{ padding: '0.75rem 1rem' }}>
        <span
          style={{
            display: 'inline-block',
            padding: '0.125rem 0.5rem',
            borderRadius: '9999px',
            fontSize: '0.75rem',
            fontWeight: 600,
            background: brand.isActive ? '#d1fae5' : '#fee2e2',
            color: brand.isActive ? '#065f46' : '#991b1b',
          }}
        >
          {brand.isActive ? 'Active' : 'Inactive'}
        </span>
      </td>
      <td style={{ padding: '0.75rem 1rem', color: '#6b7280' }}>{brand.contactEmail}</td>
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
          {brand.isActive ? 'Deactivate' : 'Activate'}
        </button>
      </td>
    </tr>
  );
}

// ---------------------------------------------------------------------------
// Main page
// ---------------------------------------------------------------------------

export function BrandList() {
  const navigate = useNavigate();
  const { brandSlug, lang } = useParams<{ brandSlug: string; lang: string }>();
  const { data: brands, isLoading, isError, error } = useBrands();

  function handleCreateClick() {
    navigate(`/${String(brandSlug)}/${String(lang)}/admin/brands/new`);
  }

  function handleRowClick(id: string) {
    navigate(`/${String(brandSlug)}/${String(lang)}/admin/brands/${id}`);
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
        <h1 style={{ margin: 0, fontSize: '1.5rem', fontWeight: 700 }}>Brands</h1>
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
          + Create Brand
        </button>
      </div>

      {isLoading && <p style={{ color: '#6b7280' }}>Loading brands…</p>}

      {isError && (
        <p style={{ color: '#dc2626' }}>
          Failed to load brands:{' '}
          {error instanceof Error ? error.message : 'Unknown error'}
        </p>
      )}

      {!isLoading && !isError && brands?.length === 0 && (
        <p style={{ color: '#6b7280' }}>No brands yet. Create the first one.</p>
      )}

      {!isLoading && !isError && brands !== undefined && brands.length > 0 && (
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
                <th style={{ padding: '0.5rem 1rem', fontWeight: 600 }}>Status</th>
                <th style={{ padding: '0.5rem 1rem', fontWeight: 600 }}>Contact Email</th>
                <th style={{ padding: '0.5rem 1rem', fontWeight: 600 }}>Actions</th>
              </tr>
            </thead>
            <tbody>
              {brands.map((brand) => (
                <BrandRow key={brand.id} brand={brand} onRowClick={handleRowClick} />
              ))}
            </tbody>
          </table>
        </div>
      )}
    </main>
  );
}
