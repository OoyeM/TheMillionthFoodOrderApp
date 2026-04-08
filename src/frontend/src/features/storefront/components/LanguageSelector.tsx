import { useNavigate, useParams, useLocation } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import type { SupportedLocale } from '@/types/common';

const LOCALES: SupportedLocale[] = ['nl', 'fr', 'de'];

const LOCALE_LABELS: Record<SupportedLocale, string> = {
  nl: 'NL',
  fr: 'FR',
  de: 'DE',
};

/** localStorage key for the user's explicit language preference. */
export const LANGUAGE_PREF_KEY = 'language-preference';

/**
 * NL / FR / DE toggle rendered in the storefront header.
 *
 * On selection:
 * - Saves the chosen locale to localStorage so the root redirect can restore it on the next visit.
 * - Navigates to the same URL path but with the new lang segment.
 */
export function LanguageSelector() {
  const { brandSlug, lang } = useParams<{ brandSlug: string; lang: string }>();
  const navigate = useNavigate();
  const location = useLocation();
  const { t } = useTranslation('common');

  function handleSelect(locale: SupportedLocale) {
    if (!brandSlug || !lang || locale === lang) return;

    localStorage.setItem(LANGUAGE_PREF_KEY, locale);

    // Replace only the lang segment in the current path so sub-routes are preserved.
    const newPathname = location.pathname.replace(
      `/${brandSlug}/${lang}`,
      `/${brandSlug}/${locale}`,
    );
    navigate({ pathname: newPathname, search: location.search }, { replace: false });
  }

  return (
    <nav
      aria-label={t('storefront.languageSelector.ariaLabel')}
      style={{ display: 'flex', gap: '0.25rem', alignItems: 'center' }}
    >
      {LOCALES.map((locale) => {
        const isActive = lang === locale;
        return (
          <button
            key={locale}
            onClick={() => handleSelect(locale)}
            aria-current={isActive ? 'true' : undefined}
            disabled={isActive}
            style={{
              padding: '0.25rem 0.5rem',
              fontSize: '0.75rem',
              fontWeight: isActive ? 700 : 400,
              background: isActive ? '#111827' : 'transparent',
              color: isActive ? '#fff' : '#374151',
              border: '1px solid #e5e7eb',
              borderRadius: '0.25rem',
              cursor: isActive ? 'default' : 'pointer',
              lineHeight: 1.4,
            }}
          >
            {LOCALE_LABELS[locale]}
          </button>
        );
      })}
    </nav>
  );
}
