import { useEffect } from 'react';
import { Outlet, useParams } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import type { SupportedLocale } from '@/types/common';
import { useAppVariant } from './useAppVariant';

const SUPPORTED_LOCALES: SupportedLocale[] = ['nl', 'fr', 'de'];

function isSupportedLocale(value: string): value is SupportedLocale {
  return (SUPPORTED_LOCALES as string[]).includes(value);
}

/**
 * Root layout for all app variants.
 * Reads brandSlug and lang from URL params, syncs the language to i18next,
 * and provides the brand context via outlet context.
 *
 * Route shape: /:brandSlug/:lang/*
 */
export function AppShell() {
  const { brandSlug, lang } = useParams<{ brandSlug: string; lang: string }>();
  const { i18n } = useTranslation();
  const variant = useAppVariant();

  // Sync URL language param to i18next
  useEffect(() => {
    if (!lang) return;

    const locale = isSupportedLocale(lang) ? lang : 'nl';
    void i18n.changeLanguage(locale);
  }, [lang, i18n]);

  if (!brandSlug || !lang) {
    return <p>Invalid route: brandSlug and lang are required.</p>;
  }

  return <Outlet context={{ brandSlug, lang, variant }} />;
}
