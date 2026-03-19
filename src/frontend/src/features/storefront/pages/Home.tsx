import { useTranslation } from 'react-i18next';
import { useParams } from 'react-router-dom';
import { ShopStatusBadge } from '../components/ShopStatusBadge';

export function Home() {
  const { t } = useTranslation('common');
  const { brandSlug } = useParams<{ brandSlug: string }>();

  return (
    <main>
      <div style={{ display: 'flex', alignItems: 'center', gap: '0.75rem' }}>
        <h1>{t('welcome')}</h1>
        {brandSlug !== undefined && (
          <ShopStatusBadge brandSlug={brandSlug} shopId="" />
        )}
      </div>
      <p>{t('storefront.description')}</p>
    </main>
  );
}
