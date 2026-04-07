import { useState, useEffect, useRef } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { useCreateProduct } from '../hooks/useProducts';
import { useBrandSettings } from '../hooks/useBrandSettings';
import { Allergen, DietaryTag, ALLERGEN_KEYS, DIETARY_TAG_KEYS, extractPrimaryLocale } from '../../../types/common';
import type { SupportedLocale } from '../../../types/common';

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
  description: string;
}

type TranslationsMap = Record<SupportedLocale, TranslationState>;

const emptyTranslations: TranslationsMap = {
  nl: { name: '', description: '' },
  fr: { name: '', description: '' },
  de: { name: '', description: '' },
};

interface FormErrors {
  basePrice?: string;
  primaryName?: string;
}

// ---------------------------------------------------------------------------
// Page component
// ---------------------------------------------------------------------------

export function ProductCreate() {
  const navigate = useNavigate();
  const { t } = useTranslation();
  const { brandSlug, lang } = useParams<{ brandSlug: string; lang: string }>();
  const resolvedBrandSlug = brandSlug ?? '';
  const createProduct = useCreateProduct(resolvedBrandSlug);
  const { data: brandSettings } = useBrandSettings(resolvedBrandSlug);
  const primaryLocale = extractPrimaryLocale(brandSettings?.defaultLanguage);

  const [activeTab, setActiveTab] = useState<SupportedLocale>(primaryLocale);
  const [translations, setTranslations] = useState<TranslationsMap>({
    ...emptyTranslations,
  });
  const [basePrice, setBasePrice] = useState('');
  const [imageUrl, setImageUrl] = useState('');
  const [errors, setErrors] = useState<FormErrors>({});
  const [selectedAllergens, setSelectedAllergens] = useState<Set<number>>(new Set());
  const [selectedDietaryTags, setSelectedDietaryTags] = useState<Set<number>>(new Set());

  // Sync active tab when brand settings load (useState ignores updates to its initializer)
  const tabSynced = useRef(false);
  useEffect(() => {
    if (brandSettings && !tabSynced.current) {
      setActiveTab(extractPrimaryLocale(brandSettings.defaultLanguage));
      tabSynced.current = true;
    }
  }, [brandSettings]);

  function updateTranslation(
    locale: SupportedLocale,
    field: keyof TranslationState,
    value: string,
  ) {
    setTranslations((prev) => ({
      ...prev,
      [locale]: { ...prev[locale], [field]: value },
    }));
  }

  function toggleAllergen(value: number) {
    setSelectedAllergens((prev) => {
      const next = new Set(prev);
      if (next.has(value)) next.delete(value);
      else next.add(value);
      return next;
    });
  }

  function toggleDietaryTag(value: number) {
    setSelectedDietaryTags((prev) => {
      const next = new Set(prev);
      if (next.has(value)) next.delete(value);
      else next.add(value);
      return next;
    });
  }

  function validate(): FormErrors {
    const next: FormErrors = {};
    const price = parseFloat(basePrice);
    if (!basePrice.trim() || isNaN(price) || price <= 0) {
      next.basePrice = t('admin.products.validation.basePriceRequired');
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
      description: translations[l.code].description.trim() || null,
    }));

    createProduct.mutate(
      {
        basePrice: parseFloat(basePrice),
        imageUrl: imageUrl.trim() || null,
        translations: translationInputs,
        allergens: [...selectedAllergens],
        dietaryTags: [...selectedDietaryTags],
      },
      {
        onSuccess: () => {
          navigate(`/${brandSlug}/${lang}/admin/products`);
        },
      },
    );
  }

  function handleCancel() {
    navigate(`/${brandSlug}/${lang}/admin/products`);
  }

  return (
    <main style={{ padding: '1.5rem', maxWidth: '40rem' }}>
      <h1 style={{ fontSize: '1.5rem', fontWeight: 700, marginBottom: '1.5rem' }}>
        {t('admin.products.create')}
      </h1>

      <form onSubmit={handleSubmit} noValidate>
        {/* Base Price */}
        <div style={{ marginBottom: '1rem' }}>
          <label style={labelStyle} htmlFor="basePrice">
            {t('admin.products.basePrice')} (EUR) <RequiredMark />
          </label>
          <input
            id="basePrice"
            type="number"
            min="0.01"
            step="0.01"
            value={basePrice}
            onChange={(e) => setBasePrice(e.target.value)}
            style={inputStyle(!!errors.basePrice)}
            placeholder={t('admin.products.pricePlaceholder')}
          />
          {errors.basePrice && <FieldError message={errors.basePrice} />}
        </div>

        {/* Image URL */}
        <div style={{ marginBottom: '1.5rem' }}>
          <label style={labelStyle} htmlFor="imageUrl">
            {t('admin.products.imageUrl')}{' '}
            <span style={{ color: '#9ca3af', fontWeight: 400 }}>{t('admin.products.optional')}</span>
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

        {/* Allergens */}
        <div style={{ marginBottom: '1.5rem' }}>
          <p style={{ fontWeight: 600, fontSize: '0.875rem', marginBottom: '0.5rem' }}>
            {t('admin.products.allergens')}
          </p>
          <div style={{ display: 'flex', flexWrap: 'wrap', gap: '0.5rem' }}>
            {ALLERGEN_KEYS.map((key) => (
              <label
                key={key}
                style={{
                  display: 'flex',
                  alignItems: 'center',
                  gap: '0.25rem',
                  fontSize: '0.875rem',
                  cursor: 'pointer',
                }}
              >
                <input
                  type="checkbox"
                  checked={selectedAllergens.has(Allergen[key])}
                  onChange={() => toggleAllergen(Allergen[key])}
                />
                {t(`allergens.${key}`)}
              </label>
            ))}
          </div>
        </div>

        {/* Dietary Tags */}
        <div style={{ marginBottom: '1.5rem' }}>
          <p style={{ fontWeight: 600, fontSize: '0.875rem', marginBottom: '0.5rem' }}>
            {t('admin.products.dietaryTags')}
          </p>
          <div style={{ display: 'flex', flexWrap: 'wrap', gap: '0.5rem' }}>
            {DIETARY_TAG_KEYS.map((key) => (
              <label
                key={key}
                style={{
                  display: 'flex',
                  alignItems: 'center',
                  gap: '0.25rem',
                  fontSize: '0.875rem',
                  cursor: 'pointer',
                }}
              >
                <input
                  type="checkbox"
                  checked={selectedDietaryTags.has(DietaryTag[key])}
                  onChange={() => toggleDietaryTag(DietaryTag[key])}
                />
                {t(`dietaryTags.${key}`)}
              </label>
            ))}
          </div>
        </div>

        {/* Translation Tabs */}
        <p style={{ fontWeight: 600, fontSize: '0.875rem', marginBottom: '0.5rem' }}>
          {t('admin.products.translations')} <RequiredMark />
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
        <div style={{ marginBottom: '1rem' }}>
          <label style={labelStyle} htmlFor={`name-${activeTab}`}>
            Name {activeTab === primaryLocale && <RequiredMark />}
          </label>
          <input
            id={`name-${activeTab}`}
            type="text"
            value={translations[activeTab].name}
            onChange={(e) => updateTranslation(activeTab, 'name', e.target.value)}
            style={inputStyle(activeTab === primaryLocale && !!errors.primaryName)}
            placeholder={`Product name in ${activeTab.toUpperCase()}`}
          />
          {activeTab === primaryLocale && errors.primaryName && <FieldError message={errors.primaryName} />}
        </div>

        <div style={{ marginBottom: '1.5rem' }}>
          <label style={labelStyle} htmlFor={`desc-${activeTab}`}>
            {t('admin.products.description')}{' '}
            <span style={{ color: '#9ca3af', fontWeight: 400 }}>{t('admin.products.optional')}</span>
          </label>
          <textarea
            id={`desc-${activeTab}`}
            value={translations[activeTab].description}
            onChange={(e) => updateTranslation(activeTab, 'description', e.target.value)}
            rows={3}
            style={{ ...inputStyle(false), resize: 'vertical' }}
            placeholder={t('admin.products.descriptionPlaceholder', { lang: activeTab.toUpperCase() })}
          />
        </div>

        {/* API error */}
        {createProduct.isError && (
          <p style={{ color: '#dc2626', marginBottom: '1rem', fontSize: '0.875rem' }}>
            {createProduct.error instanceof Error
              ? createProduct.error.message
              : t('admin.products.createError')}
          </p>
        )}

        <div style={{ display: 'flex', gap: '0.75rem' }}>
          <button
            type="submit"
            disabled={createProduct.isPending}
            style={{
              padding: '0.5rem 1.25rem',
              background: '#111827',
              color: '#fff',
              border: 'none',
              borderRadius: '0.375rem',
              cursor: createProduct.isPending ? 'not-allowed' : 'pointer',
              fontWeight: 600,
              opacity: createProduct.isPending ? 0.6 : 1,
            }}
          >
            {createProduct.isPending ? t('admin.products.creating') : t('admin.products.create')}
          </button>
          <button type="button" onClick={handleCancel} style={secondaryButtonStyle}>
            {t('admin.products.cancel')}
          </button>
        </div>
      </form>
    </main>
  );
}

// ---------------------------------------------------------------------------
// Small style helpers
// ---------------------------------------------------------------------------

const labelStyle: React.CSSProperties = {
  display: 'block',
  fontWeight: 600,
  fontSize: '0.875rem',
  marginBottom: '0.25rem',
};

const secondaryButtonStyle: React.CSSProperties = {
  padding: '0.5rem 1.25rem',
  background: '#fff',
  color: '#374151',
  border: '1px solid #d1d5db',
  borderRadius: '0.375rem',
  cursor: 'pointer',
};

function inputStyle(hasError: boolean): React.CSSProperties {
  return {
    width: '100%',
    padding: '0.5rem 0.75rem',
    border: `1px solid ${hasError ? '#dc2626' : '#d1d5db'}`,
    borderRadius: '0.375rem',
    fontSize: '1rem',
    boxSizing: 'border-box',
  };
}

function RequiredMark() {
  return <span style={{ color: '#dc2626' }}>*</span>;
}

function FieldError({ message }: { message: string }) {
  return (
    <p style={{ color: '#dc2626', fontSize: '0.75rem', marginTop: '0.25rem' }}>{message}</p>
  );
}
