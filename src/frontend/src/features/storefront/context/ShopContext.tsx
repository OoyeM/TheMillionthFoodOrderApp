import { useParams, Outlet, Navigate } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { useActiveShops } from '../hooks/useActiveShops';
import { ShopContext } from './shopContextValue';
import type { ResolvedShop } from './shopContextValue';

// ---------------------------------------------------------------------------
// ShopResolver — layout route component
// ---------------------------------------------------------------------------

/**
 * Layout route that resolves :shopSlug to a full shop object once.
 * Renders an <Outlet /> for children once the shop is found.
 *
 * - Shows a loading state while the shops list is fetching.
 * - Redirects to /shops if the slug cannot be matched (shop deactivated etc).
 */
export function ShopResolver() {
  const { brandSlug, lang, shopSlug } = useParams<{
    brandSlug: string;
    lang: string;
    shopSlug: string;
  }>();

  const { t } = useTranslation('common');
  const resolvedBrandSlug = brandSlug ?? '';
  const resolvedLang = lang ?? 'nl';
  const { data: shops, isLoading, isError } = useActiveShops(resolvedBrandSlug);

  if (isLoading) {
    return (
      <main style={{ maxWidth: '40rem', margin: '0 auto', padding: '1.5rem 1rem' }}>
        <p style={{ color: '#6b7280' }}>{t('loading')}</p>
      </main>
    );
  }

  if (isError) {
    return (
      <main style={{ maxWidth: '40rem', margin: '0 auto', padding: '1.5rem 1rem' }}>
        <p style={{ color: '#ef4444' }}>{t('error')}</p>
      </main>
    );
  }

  const shop = shops?.find((s) => s.slug === shopSlug);

  if (!shop) {
    // Slug not found — redirect back to shop chooser
    return <Navigate to={`/${resolvedBrandSlug}/${resolvedLang}/shops`} replace />;
  }

  const resolved: ResolvedShop = {
    id: shop.id,
    name: shop.name,
    slug: shop.slug,
    isOpen: shop.isOpen,
    eatIn: shop.eatIn,
  };

  return (
    <ShopContext.Provider value={resolved}>
      <Outlet />
    </ShopContext.Provider>
  );
}
