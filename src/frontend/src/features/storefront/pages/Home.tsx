import { useTranslation } from 'react-i18next';

export function Home() {
  const { t } = useTranslation('common');

  return (
    <main>
      <h1>{t('welcome')}</h1>
      <p>{t('storefront.description')}</p>
    </main>
  );
}
