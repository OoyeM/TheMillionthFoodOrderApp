// Shop chooser page — lists all active shops for the brand.
// If exactly one shop is active, auto-redirects straight to its menu.
// Part of US-FP-071: shop selection and slug-based routing.

import { useParams, Navigate, Link } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { useActiveShops } from '../hooks/useActiveShops';
import { ShopStatusBadge } from '../components/ShopStatusBadge';

/**
 * Renders a card for each active shop.
 * Tapping a card navigates to /:brandSlug/:lang/:shopSlug/menu.
 */
export function ShopChooserPage() {
  const { t } = useTranslation('common');
  const { brandSlug, lang } = useParams<{ brandSlug: string; lang: string }>();

  const resolvedBrandSlug = brandSlug ?? '';
  const resolvedLang = lang ?? 'nl';
  const { data: shops, isLoading, isError } = useActiveShops(resolvedBrandSlug);

  // Loading
  if (isLoading) {
    return (
      <main style={{ maxWidth: '40rem', margin: '0 auto', padding: '1.5rem 1rem' }}>
        <p style={{ color: '#6b7280' }}>{t('loading')}</p>
      </main>
    );
  }

  // Error
  if (isError) {
    return (
      <main style={{ maxWidth: '40rem', margin: '0 auto', padding: '1.5rem 1rem' }}>
        <p style={{ color: '#ef4444' }}>{t('error')}</p>
      </main>
    );
  }

  // Auto-redirect when exactly one shop is active
  const singleShop = shops?.length === 1 ? shops[0] : undefined;
  if (singleShop) {
    return (
      <Navigate
        to={`/${resolvedBrandSlug}/${resolvedLang}/${singleShop.slug}/menu`}
        replace
      />
    );
  }

  // At this point data is loaded: normalise to an array (undefined → empty)
  const activeShops = shops ?? [];

  // Empty state
  if (activeShops.length === 0) {
    return (
      <main style={{ maxWidth: '40rem', margin: '0 auto', padding: '1.5rem 1rem' }}>
        <h1
          style={{ fontSize: '1.75rem', fontWeight: 800, color: '#111827', marginBottom: '1.5rem' }}
        >
          {t('storefront.shopChooser.title')}
        </h1>
        <p style={{ color: '#6b7280' }}>{t('storefront.shopChooser.noShops')}</p>
      </main>
    );
  }

  // Multi-shop chooser
  return (
    <main style={{ maxWidth: '40rem', margin: '0 auto', padding: '1.5rem 1rem' }}>
      <h1
        style={{
          fontSize: '1.75rem',
          fontWeight: 800,
          color: '#111827',
          marginBottom: '0.5rem',
        }}
      >
        {t('storefront.shopChooser.title')}
      </h1>
      <p style={{ color: '#6b7280', marginBottom: '1.5rem', fontSize: '0.9375rem' }}>
        {t('storefront.shopChooser.subtitle')}
      </p>

      <div style={{ display: 'flex', flexDirection: 'column', gap: '1rem' }}>
        {activeShops.map((shop) => (
          <Link
            key={shop.id}
            to={`/${resolvedBrandSlug}/${resolvedLang}/${shop.slug}/menu`}
            style={{ textDecoration: 'none', color: 'inherit' }}
            aria-label={t('storefront.shopChooser.selectShop', { name: shop.name })}
          >
            <div
              style={{
                padding: '1.25rem 1.5rem',
                border: '2px solid #e5e7eb',
                borderRadius: '0.75rem',
                background: '#fff',
                cursor: 'pointer',
                transition: 'border-color 0.15s, box-shadow 0.15s',
              }}
              onMouseEnter={(e) => {
                (e.currentTarget as HTMLDivElement).style.borderColor =
                  'var(--brand-color-primary, #111827)';
                (e.currentTarget as HTMLDivElement).style.boxShadow =
                  '0 2px 8px rgba(0,0,0,0.08)';
              }}
              onMouseLeave={(e) => {
                (e.currentTarget as HTMLDivElement).style.borderColor = '#e5e7eb';
                (e.currentTarget as HTMLDivElement).style.boxShadow = 'none';
              }}
            >
              {/* Shop name + status badge */}
              <div
                style={{
                  display: 'flex',
                  alignItems: 'center',
                  justifyContent: 'space-between',
                  gap: '0.75rem',
                  marginBottom: '0.5rem',
                }}
              >
                <span style={{ fontSize: '1.0625rem', fontWeight: 700, color: '#111827' }}>
                  {shop.name}
                </span>
                <ShopStatusBadge brandSlug={resolvedBrandSlug} shopId={shop.id} />
              </div>

              {/* Address */}
              <p
                style={{
                  margin: 0,
                  fontSize: '0.875rem',
                  color: '#6b7280',
                  lineHeight: 1.5,
                }}
              >
                {shop.address.street} {shop.address.number}, {shop.address.postalCode}{' '}
                {shop.address.city}
              </p>
            </div>
          </Link>
        ))}
      </div>
    </main>
  );
}
