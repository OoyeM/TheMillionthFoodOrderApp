import { useEffect } from 'react';
import { useParams, Link, useNavigate } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { useQuery } from '@tanstack/react-query';
import { shopsApi } from '@api/shops';
import type { Shop } from '@/types/common';

const LAST_SHOP_KEY = (brandSlug: string) => `pos:last-shop:${brandSlug}`;

/**
 * POS dashboard — shop picker.
 * Lists all active shops for the brand. Staff tap a shop to start ordering.
 * Persists the last-used shop to localStorage and auto-redirects on next visit.
 */
export function PosDashboard() {
  const { t } = useTranslation('common');
  const navigate = useNavigate();
  const { brandSlug, lang } = useParams<{ brandSlug: string; lang: string }>();

  const resolvedBrand = brandSlug ?? '';
  const resolvedLang = lang ?? 'nl';

  const { data: shops, isLoading, isError } = useQuery<Shop[]>({
    queryKey: ['pos', 'shops', resolvedBrand],
    queryFn: () => shopsApi.list(resolvedBrand),
    enabled: resolvedBrand.length > 0,
  });

  // Auto-redirect to the last-used shop's order page
  useEffect(() => {
    const lastShop = localStorage.getItem(LAST_SHOP_KEY(resolvedBrand));
    if (lastShop && shops) {
      const exists = shops.some((s) => s.id === lastShop);
      if (exists) {
        navigate(`/${resolvedBrand}/${resolvedLang}/pos/shops/${lastShop}/order`, {
          replace: true,
        });
      }
    }
  }, [shops, resolvedBrand, resolvedLang, navigate]);

  function handleSelectShop(shopId: string) {
    localStorage.setItem(LAST_SHOP_KEY(resolvedBrand), shopId);
    navigate(`/${resolvedBrand}/${resolvedLang}/pos/shops/${shopId}/order`);
  }

  return (
    <main
      style={{
        maxWidth: '56rem',
        margin: '0 auto',
        padding: '2rem 1rem',
        display: 'flex',
        flexDirection: 'column',
        gap: '1.5rem',
      }}
    >
      <h1 style={{ margin: 0, fontSize: '1.75rem', fontWeight: 800, color: '#111827' }}>
        {t('pos.dashboard.title')}
      </h1>
      <p style={{ margin: 0, color: '#6b7280' }}>{t('pos.dashboard.selectShop')}</p>

      {isLoading && <p style={{ color: '#9ca3af' }}>{t('loading')}</p>}
      {isError && <p style={{ color: '#dc2626' }}>{t('error')}</p>}

      {shops && shops.length === 0 && (
        <p style={{ color: '#9ca3af' }}>{t('pos.dashboard.noShops')}</p>
      )}

      <ul style={{ listStyle: 'none', margin: 0, padding: 0, display: 'flex', flexDirection: 'column', gap: '1rem' }}>
        {shops
          ?.filter((shop) => shop.isActive)
          .map((shop) => (
            <li
              key={shop.id}
              style={{
                background: '#fff',
                border: '1px solid #e5e7eb',
                borderRadius: '0.75rem',
                padding: '1.25rem 1.5rem',
                boxShadow: '0 1px 3px rgba(0,0,0,0.06)',
              }}
            >
              <div
                style={{
                  display: 'flex',
                  justifyContent: 'space-between',
                  alignItems: 'center',
                  flexWrap: 'wrap',
                  gap: '1rem',
                }}
              >
                <div>
                  <h2 style={{ margin: 0, fontSize: '1.125rem', fontWeight: 700, color: '#111827' }}>
                    {shop.name}
                  </h2>
                  <p style={{ margin: '0.25rem 0 0', fontSize: '0.875rem', color: '#6b7280' }}>
                    {shop.address.street} {shop.address.number}, {shop.address.city}
                  </p>
                </div>
                <div style={{ display: 'flex', gap: '0.75rem', flexWrap: 'wrap' }}>
                  <button
                    type="button"
                    onClick={() => handleSelectShop(shop.id)}
                    style={{
                      minHeight: '2.75rem',
                      padding: '0.625rem 1.25rem',
                      background: '#111827',
                      color: '#fff',
                      fontWeight: 600,
                      fontSize: '0.9375rem',
                      border: 'none',
                      borderRadius: '0.5rem',
                      cursor: 'pointer',
                      whiteSpace: 'nowrap',
                    }}
                  >
                    {t('pos.dashboard.openOrdering')}
                  </button>
                  <Link
                    to={`/${resolvedBrand}/${resolvedLang}/pos/shops/${shop.id}/kitchen`}
                    style={{
                      display: 'inline-flex',
                      alignItems: 'center',
                      minHeight: '2.75rem',
                      padding: '0.625rem 1.25rem',
                      background: '#f3f4f6',
                      color: '#374151',
                      fontWeight: 600,
                      fontSize: '0.9375rem',
                      border: '1px solid #e5e7eb',
                      borderRadius: '0.5rem',
                      textDecoration: 'none',
                      whiteSpace: 'nowrap',
                    }}
                  >
                    {t('pos.dashboard.openKitchen')}
                  </Link>
                </div>
              </div>
            </li>
          ))}
      </ul>
    </main>
  );
}
