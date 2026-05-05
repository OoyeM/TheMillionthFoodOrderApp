import { useState, useEffect, useRef } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { useCreateMenuCategory } from '../hooks/useMenuCategories';
import { useBrandSettings } from '../hooks/useBrandSettings';
import { extractPrimaryLocale } from '../../../types/common';
import type { SupportedLocale } from '../../../types/common';
import { labelStyle, inputStyle, secondaryButtonStyle, RequiredMark, FieldError } from '../forms/adminFormStyles';

// ---------------------------------------------------------------------------
// Constants
// ---------------------------------------------------------------------------

const LANGUAGES: { code: SupportedLocale; label: string }[] = [
  { code: 'nl', label: 'NL' },
  { code: 'fr', label: 'FR' },
  { code: 'de', label: 'DE' },
];

interface TranslationState {
  name: string;
}

type TranslationsMap = Record<SupportedLocale, TranslationState>;

const emptyTranslations: TranslationsMap = {
  nl: { name: '' },
  fr: { name: '' },
  de: { name: '' },
};

interface FormErrors {
  sortOrder?: string;
  primaryName?: string;
}

// ---------------------------------------------------------------------------
// Page component
// ---------------------------------------------------------------------------

export function MenuCategoryCreate() {
  const navigate = useNavigate();
  const { brandSlug, lang } = useParams<{ brandSlug: string; lang: string }>();
  const resolvedBrandSlug = brandSlug ?? '';
  const createCategory = useCreateMenuCategory(resolvedBrandSlug);
  const { data: brandSettings } = useBrandSettings(resolvedBrandSlug);
  const primaryLocale = extractPrimaryLocale(brandSettings?.defaultLanguage);

  const [activeTab, setActiveTab] = useState<SupportedLocale>(primaryLocale);
  const [translations, setTranslations] = useState<TranslationsMap>({
    ...emptyTranslations,
  });
  const [sortOrder, setSortOrder] = useState('0');
  const [imageUrl, setImageUrl] = useState('');
  const [errors, setErrors] = useState<FormErrors>({});

  // Sync active tab when brand settings load (useState ignores updates to its initializer)
  const tabSynced = useRef(false);
  useEffect(() => {
    if (brandSettings && !tabSynced.current) {
      setActiveTab(extractPrimaryLocale(brandSettings.defaultLanguage));
      tabSynced.current = true;
    }
  }, [brandSettings]);

  function updateTranslation(locale: SupportedLocale, value: string) {
    setTranslations((prev) => ({
      ...prev,
      [locale]: { name: value },
    }));
  }

  function validate(): FormErrors {
    const next: FormErrors = {};
    const order = parseInt(sortOrder, 10);
    if (!sortOrder.trim() || isNaN(order) || order < 0) {
      next.sortOrder = 'Sort order must be a non-negative integer.';
    }
    if (translations[primaryLocale].name.trim().length === 0) {
      next.primaryName = `${primaryLocale.toUpperCase()} name is required.`;
    }
    return next;
  }

  function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    const validationErrors = validate();
    if (Object.keys(validationErrors).length > 0) {
      setErrors(validationErrors);
      return;
    }
    setErrors({});

    const translationInputs = LANGUAGES.filter(
      (l) => translations[l.code].name.trim().length > 0,
    ).map((l) => ({
      languageCode: l.code,
      name: translations[l.code].name.trim(),
    }));

    createCategory.mutate(
      {
        sortOrder: parseInt(sortOrder, 10),
        imageUrl: imageUrl.trim() || null,
        translations: translationInputs,
      },
      {
        onSuccess: () => {
          navigate(`/${brandSlug}/${lang}/admin/menu-categories`);
        },
      },
    );
  }

  function handleCancel() {
    navigate(`/${brandSlug}/${lang}/admin/menu-categories`);
  }

  return (
    <main style={{ padding: '1.5rem', maxWidth: '40rem' }}>
      <h1 style={{ fontSize: '1.5rem', fontWeight: 700, marginBottom: '1.5rem' }}>
        Create Menu Category
      </h1>

      <form onSubmit={handleSubmit} noValidate>
        {/* Sort Order */}
        <div style={{ marginBottom: '1rem' }}>
          <label style={labelStyle} htmlFor="sortOrder">
            Sort Order <RequiredMark />
          </label>
          <input
            id="sortOrder"
            type="number"
            min="0"
            step="1"
            value={sortOrder}
            onChange={(e) => setSortOrder(e.target.value)}
            style={inputStyle(!!errors.sortOrder)}
            placeholder="e.g. 0"
          />
          {errors.sortOrder && <FieldError message={errors.sortOrder} />}
        </div>

        {/* Image URL */}
        <div style={{ marginBottom: '1.5rem' }}>
          <label style={labelStyle} htmlFor="imageUrl">
            Image URL{' '}
            <span style={{ color: '#9ca3af', fontWeight: 400 }}>(optional)</span>
          </label>
          <input
            id="imageUrl"
            type="url"
            value={imageUrl}
            onChange={(e) => setImageUrl(e.target.value)}
            style={inputStyle(false)}
            placeholder="https://example.com/image.jpg"
          />
          {imageUrl.trim() && (
            <img
              src={imageUrl}
              alt="Preview"
              style={{
                marginTop: '0.5rem',
                maxWidth: 120,
                maxHeight: 120,
                objectFit: 'cover',
                borderRadius: '0.25rem',
              }}
              onError={(e) => {
                (e.target as HTMLImageElement).style.display = 'none';
              }}
            />
          )}
        </div>

        {/* Translation Tabs */}
        <p style={{ fontWeight: 600, fontSize: '0.875rem', marginBottom: '0.5rem' }}>
          Translations <RequiredMark />
        </p>
        <div
          style={{
            display: 'flex',
            marginBottom: '1rem',
            borderBottom: '2px solid #e5e7eb',
          }}
        >
          {LANGUAGES.map((l) => (
            <button
              key={l.code}
              type="button"
              onClick={() => setActiveTab(l.code)}
              style={{
                padding: '0.5rem 1rem',
                fontWeight: activeTab === l.code ? 700 : 400,
                background: 'none',
                border: 'none',
                borderBottom: `2px solid ${activeTab === l.code ? '#111827' : 'transparent'}`,
                cursor: 'pointer',
                marginBottom: '-2px',
                color: activeTab === l.code ? '#111827' : '#6b7280',
              }}
            >
              {l.label}
              {l.code === primaryLocale && ' *'}
            </button>
          ))}
        </div>

        {/* Active tab content */}
        <div style={{ marginBottom: '1.5rem' }}>
          <label style={labelStyle} htmlFor={`name-${activeTab}`}>
            Name {activeTab === primaryLocale && <RequiredMark />}
          </label>
          <input
            id={`name-${activeTab}`}
            type="text"
            value={translations[activeTab].name}
            onChange={(e) => updateTranslation(activeTab, e.target.value)}
            style={inputStyle(activeTab === primaryLocale && !!errors.primaryName)}
            placeholder={`Category name in ${activeTab.toUpperCase()}`}
          />
          {activeTab === primaryLocale && errors.primaryName && <FieldError message={errors.primaryName} />}
        </div>

        {/* API error */}
        {createCategory.isError && (
          <p style={{ color: '#dc2626', marginBottom: '1rem', fontSize: '0.875rem' }}>
            {createCategory.error instanceof Error
              ? createCategory.error.message
              : 'Failed to create menu category. Please try again.'}
          </p>
        )}

        <div style={{ display: 'flex', gap: '0.75rem' }}>
          <button
            type="submit"
            disabled={createCategory.isPending}
            style={{
              padding: '0.5rem 1.25rem',
              background: '#111827',
              color: '#fff',
              border: 'none',
              borderRadius: '0.375rem',
              cursor: createCategory.isPending ? 'not-allowed' : 'pointer',
              fontWeight: 600,
              opacity: createCategory.isPending ? 0.6 : 1,
            }}
          >
            {createCategory.isPending ? 'Creating...' : 'Create Category'}
          </button>
          <button type="button" onClick={handleCancel} style={secondaryButtonStyle}>
            Cancel
          </button>
        </div>
      </form>
    </main>
  );
}

