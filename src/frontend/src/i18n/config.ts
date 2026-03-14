import i18next from 'i18next';
import { initReactI18next } from 'react-i18next';
import LanguageDetector from 'i18next-browser-languagedetector';

import nlCommon from './locales/nl/common.json';
import frCommon from './locales/fr/common.json';
import deCommon from './locales/de/common.json';

void i18next
  .use(LanguageDetector)
  .use(initReactI18next)
  .init({
    // Fallback to Dutch (primary Belgian market language)
    fallbackLng: 'nl',
    supportedLngs: ['nl', 'fr', 'de'],
    defaultNS: 'common',
    ns: ['common'],

    // Translations are bundled — no backend fetch needed
    resources: {
      nl: { common: nlCommon },
      fr: { common: frCommon },
      de: { common: deCommon },
    },

    interpolation: {
      // React already escapes output; no need for i18next to escape again
      escapeValue: false,
    },

    detection: {
      // Prefer URL path param (set programmatically in AppShell) over browser settings
      order: ['htmlTag', 'navigator'],
      caches: [],
    },
  });

export default i18next;
