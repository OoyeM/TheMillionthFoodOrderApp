import { useNavigate, useParams } from 'react-router-dom';
import { useProducts, useDeleteProduct } from '../hooks/useProducts';
import type { ProductListItem } from '../../../types/common';

// ---------------------------------------------------------------------------
// Sub-component: a single row
// ---------------------------------------------------------------------------

interface ProductRowProps {
  product: ProductListItem;
  onRowClick: (id: string) => void;
  onDelete: (id: string) => void;
  isDeleting: boolean;
}

function ProductRow({ product, onRowClick, onDelete, isDeleting }: ProductRowProps) {
  function handleDelete(e: React.MouseEvent) {
    e.stopPropagation();
    if (window.confirm(`Delete "${product.name}"? This product will be hidden from the storefront.`)) {
      onDelete(product.id);
    }
  }

  const isCombo = product.productType === 'Combo';

  return (
    <tr
      onClick={() => { onRowClick(product.id); }}
      style={{ cursor: 'pointer', borderBottom: '1px solid #e5e7eb' }}
    >
      <td style={{ padding: '0.75rem 1rem' }}>{product.name}</td>
      <td style={{ padding: '0.75rem 1rem' }}>
        <span
          style={{
            display: 'inline-block',
            padding: '0.125rem 0.5rem',
            fontSize: '0.7rem',
            fontWeight: 600,
            borderRadius: '9999px',
            background: isCombo ? '#dbeafe' : '#f3f4f6',
            color: isCombo ? '#1d4ed8' : '#6b7280',
          }}
        >
          {isCombo ? 'Combo' : 'Simple'}
        </span>
      </td>
      <td style={{ padding: '0.75rem 1rem', fontFamily: 'monospace' }}>
        {'\u20AC'} {product.basePrice.amount.toFixed(2)}
      </td>
      <td style={{ padding: '0.75rem 1rem' }}>
        {product.imageUrl ? (
          <img
            src={product.imageUrl}
            alt={product.name}
            style={{ width: 40, height: 40, objectFit: 'cover', borderRadius: '0.25rem' }}
          />
        ) : (
          <span style={{ color: '#9ca3af' }}>&mdash;</span>
        )}
      </td>
      <td style={{ padding: '0.75rem 1rem', color: '#6b7280' }}>
        {new Date(product.createdAt).toLocaleDateString()}
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
          Delete
        </button>
      </td>
    </tr>
  );
}

// ---------------------------------------------------------------------------
// Main page
// ---------------------------------------------------------------------------

export function ProductList() {
  const navigate = useNavigate();
  const { brandSlug, lang } = useParams<{ brandSlug: string; lang: string }>();

  const resolvedBrandSlug = brandSlug ?? '';
  const { data: products, isLoading, isError, error } = useProducts(resolvedBrandSlug);
  const deleteProduct = useDeleteProduct(resolvedBrandSlug);

  function handleCreateClick() {
    navigate(`/${String(brandSlug)}/${String(lang)}/admin/products/new`);
  }

  function handleCreateComboClick() {
    navigate(`/${String(brandSlug)}/${String(lang)}/admin/combo-products/new`);
  }

  function handleRowClick(id: string) {
    const product = products?.find((p) => p.id === id);
    if (product?.productType === 'Combo') {
      navigate(`/${String(brandSlug)}/${String(lang)}/admin/combo-products/${id}`);
    } else {
      navigate(`/${String(brandSlug)}/${String(lang)}/admin/products/${id}`);
    }
  }

  function handleDelete(id: string) {
    deleteProduct.mutate(id);
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
        <h1 style={{ margin: 0, fontSize: '1.5rem', fontWeight: 700 }}>Products</h1>
        <div style={{ display: 'flex', gap: '0.5rem' }}>
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
            + Create Product
          </button>
          <button
            onClick={handleCreateComboClick}
            style={{
              padding: '0.5rem 1rem',
              background: '#1d4ed8',
              color: '#fff',
              border: 'none',
              borderRadius: '0.375rem',
              cursor: 'pointer',
              fontWeight: 600,
            }}
          >
            + Create Combo
          </button>
        </div>
      </div>

      {isLoading && <p style={{ color: '#6b7280' }}>Loading products...</p>}

      {isError && (
        <p style={{ color: '#dc2626' }}>
          Failed to load products:{' '}
          {error instanceof Error ? error.message : 'Unknown error'}
        </p>
      )}

      {!isLoading && !isError && products?.length === 0 && (
        <p style={{ color: '#6b7280' }}>No products yet. Create the first one.</p>
      )}

      {!isLoading && !isError && products !== undefined && products.length > 0 && (
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
                <th style={{ padding: '0.5rem 1rem', fontWeight: 600 }}>Type</th>
                <th style={{ padding: '0.5rem 1rem', fontWeight: 600 }}>Price</th>
                <th style={{ padding: '0.5rem 1rem', fontWeight: 600 }}>Image</th>
                <th style={{ padding: '0.5rem 1rem', fontWeight: 600 }}>Created</th>
                <th style={{ padding: '0.5rem 1rem', fontWeight: 600 }}>Actions</th>
              </tr>
            </thead>
            <tbody>
              {products.map((product) => (
                <ProductRow
                  key={product.id}
                  product={product}
                  onRowClick={handleRowClick}
                  onDelete={handleDelete}
                  isDeleting={deleteProduct.isPending}
                />
              ))}
            </tbody>
          </table>
        </div>
      )}
    </main>
  );
}
