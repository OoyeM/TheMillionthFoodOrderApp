// Back-compat redirect: old GUID-based route `shops/:shopId/menu`
// → slug-based route `/:brandSlug/:lang/:shopSlug/menu`.
// Part of US-FP-071: keeps any saved/bookmarked old URLs working.

import { useParams, Navigate } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { useActiveShops } from '../hooks/useActiveShops';

/**
 * Resolves an old :shopId (GUID) to the corresponding slug and redirects.
 * If the shop is not found (e.g. deactivated), falls back to the shop chooser.
 */
export function LegacyShopIdRedirect() {
  const { brandSlug, lang, shopId } = useParams<{
    brandSlug: string;
    lang: string;
    shopId: string;
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
    return <Navigate to={`/${resolvedBrandSlug}/${resolvedLang}/shops`} replace />;
  }

  const shop = shops?.find((s) => s.id === shopId);

  if (!shop) {
    // Unknown GUID — fall back to chooser
    return <Navigate to={`/${resolvedBrandSlug}/${resolvedLang}/shops`} replace />;
  }

  return <Navigate to={`/${resolvedBrandSlug}/${resolvedLang}/${shop.slug}/menu`} replace />;
}
