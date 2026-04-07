import { useState } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { useProducts, useCreateComboProduct } from '../hooks/useProducts';
import type { SupportedLocale, ProductListItem } from '../../../types/common';

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
  nlName?: string;
  components?: string;
}

// ---------------------------------------------------------------------------
// Page component
// ---------------------------------------------------------------------------

export function ComboProductCreate() {
  const navigate = useNavigate();
  const { t } = useTranslation();
  const { brandSlug, lang } = useParams<{ brandSlug: string; lang: string }>();
  const resolvedBrandSlug = brandSlug ?? '';
  const createCombo = useCreateComboProduct(resolvedBrandSlug);
  const { data: allProducts } = useProducts(resolvedBrandSlug);

  const [activeTab, setActiveTab] = useState<SupportedLocale>('nl');
  const [translations, setTranslations] = useState<TranslationsMap>({
    ...emptyTranslations,
  });
  const [basePrice, setBasePrice] = useState('');
  const [imageUrl, setImageUrl] = useState('');
  const [selectedComponents, setSelectedComponents] = useState<ProductListItem[]>([]);
  const [errors, setErrors] = useState<FormErrors>({});

  const simpleProducts = allProducts?.filter((p) => p.productType === 'Simple') ?? [];

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

  function validate(): FormErrors {
    const next: FormErrors = {};
    const price = parseFloat(basePrice);
    if (!basePrice.trim() || isNaN(price) || price <= 0) {
      next.basePrice = t('admin.comboProducts.errors.priceRequired');
    }
    if (translations.nl.name.trim().length === 0) {
      next.nlName = t('admin.comboProducts.errors.nlNameRequired');
    }
    if (selectedComponents.length < 2) {
      next.components = t('admin.comboProducts.errors.minComponents');
    }
    return next;
  }

  function handleToggleComponent(product: ProductListItem) {
    setSelectedComponents((prev) => {
      const exists = prev.some((p) => p.id === product.id);
      if (exists) {
        return prev.filter((p) => p.id !== product.id);
      }
      return [...prev, product];
    });
  }

  function handleMoveUp(index: number) {
    if (index === 0) return;
    setSelectedComponents((prev) => {
      const next = [...prev];
      [next[index - 1], next[index]] = [next[index]!, next[index - 1]!];
      return next;
    });
  }

  function handleMoveDown(index: number) {
    setSelectedComponents((prev) => {
      if (index >= prev.length - 1) return prev;
      const next = [...prev];
      [next[index], next[index + 1]] = [next[index + 1]!, next[index]!];
      return next;
    });
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

    createCombo.mutate(
      {
        basePrice: parseFloat(basePrice),
        imageUrl: imageUrl.trim() || null,
        translations: translationInputs,
        componentProductIds: selectedComponents.map((p) => p.id),
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
        {t('admin.comboProducts.create')}
      </h1>

      <form onSubmit={handleSubmit} noValidate>
        {/* Base Price */}
        <div style={{ marginBottom: '1rem' }}>
          <label style={labelStyle} htmlFor="basePrice">
            {t('admin.comboProducts.bundlePrice')} (EUR) <RequiredMark />
          </label>
          <input
            id="basePrice"
            type="number"
            min="0.01"
            step="0.01"
            value={basePrice}
            onChange={(e) => setBasePrice(e.target.value)}
            style={inputStyle(!!errors.basePrice)}
            placeholder="e.g. 3.50"
          />
          {errors.basePrice && <FieldError message={errors.basePrice} />}
        </div>

        {/* Image URL */}
        <div style={{ marginBottom: '1.5rem' }}>
          <label style={labelStyle} htmlFor="imageUrl">
            Image URL <span style={{ color: '#9ca3af', fontWeight: 400 }}>(optional)</span>
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
          {t('admin.comboProducts.translations')} <RequiredMark />
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
              {l.code === 'nl' && ' *'}
            </button>
          ))}
        </div>

        {/* Active tab content */}
        <div style={{ marginBottom: '1rem' }}>
          <label style={labelStyle} htmlFor={`name-${activeTab}`}>
            Name {activeTab === 'nl' && <RequiredMark />}
          </label>
          <input
            id={`name-${activeTab}`}
            type="text"
            value={translations[activeTab].name}
            onChange={(e) => updateTranslation(activeTab, 'name', e.target.value)}
            style={inputStyle(activeTab === 'nl' && !!errors.nlName)}
            placeholder={`Combo name in ${activeTab.toUpperCase()}`}
          />
          {activeTab === 'nl' && errors.nlName && <FieldError message={errors.nlName} />}
        </div>

        <div style={{ marginBottom: '1.5rem' }}>
          <label style={labelStyle} htmlFor={`desc-${activeTab}`}>
            Description <span style={{ color: '#9ca3af', fontWeight: 400 }}>(optional)</span>
          </label>
          <textarea
            id={`desc-${activeTab}`}
            value={translations[activeTab].description}
            onChange={(e) => updateTranslation(activeTab, 'description', e.target.value)}
            rows={3}
            style={{ ...inputStyle(false), resize: 'vertical' }}
            placeholder={`Combo description in ${activeTab.toUpperCase()}`}
          />
        </div>

        {/* Component Products */}
        <section style={{ marginBottom: '1.5rem' }}>
          <p style={{ fontWeight: 600, fontSize: '0.875rem', marginBottom: '0.5rem' }}>
            {t('admin.comboProducts.componentProducts')} <RequiredMark />
          </p>
          <p style={{ fontSize: '0.75rem', color: '#6b7280', marginBottom: '0.75rem' }}>
            {t('admin.comboProducts.componentProductsHint')}
          </p>

          {errors.components && <FieldError message={errors.components} />}

          {/* Selected components with reorder controls */}
          {selectedComponents.length > 0 && (
            <div style={{ marginBottom: '0.75rem' }}>
              {selectedComponents.map((product, index) => (
                <div
                  key={product.id}
                  style={{
                    display: 'flex',
                    alignItems: 'center',
                    gap: '0.5rem',
                    padding: '0.5rem 0.75rem',
                    border: '1px solid #e5e7eb',
                    borderRadius: '0.375rem',
                    marginBottom: '0.375rem',
                    background: '#f9fafb',
                  }}
                >
                  <span style={{ flex: 1, fontSize: '0.9rem', fontWeight: 500 }}>
                    {product.name}
                  </span>
                  <span style={{ fontSize: '0.75rem', color: '#6b7280', fontFamily: 'monospace' }}>
                    {'\u20AC'} {product.basePrice.amount.toFixed(2)}
                  </span>
                  <button
                    type="button"
                    onClick={() => handleMoveUp(index)}
                    disabled={index === 0}
                    style={{
                      ...reorderButtonStyle,
                      opacity: index === 0 ? 0.3 : 1,
                      cursor: index === 0 ? 'not-allowed' : 'pointer',
                    }}
                  >
                    &#9650;
                  </button>
                  <button
                    type="button"
                    onClick={() => handleMoveDown(index)}
                    disabled={index === selectedComponents.length - 1}
                    style={{
                      ...reorderButtonStyle,
                      opacity: index === selectedComponents.length - 1 ? 0.3 : 1,
                      cursor: index === selectedComponents.length - 1 ? 'not-allowed' : 'pointer',
                    }}
                  >
                    &#9660;
                  </button>
                  <button
                    type="button"
                    onClick={() => handleToggleComponent(product)}
                    style={{
                      padding: '0.125rem 0.5rem',
                      fontSize: '0.75rem',
                      background: '#fff',
                      border: '1px solid #fca5a5',
                      borderRadius: '0.25rem',
                      color: '#dc2626',
                      cursor: 'pointer',
                    }}
                  >
                    {t('admin.comboProducts.remove')}
                  </button>
                </div>
              ))}
            </div>
          )}

          {/* Available products to add */}
          {simpleProducts.length > 0 && (
            <div
              style={{
                border: '1px solid #e5e7eb',
                borderRadius: '0.375rem',
                maxHeight: '12rem',
                overflowY: 'auto',
              }}
            >
              {simpleProducts
                .filter((p) => !selectedComponents.some((s) => s.id === p.id))
                .map((product) => (
                  <div
                    key={product.id}
                    onClick={() => handleToggleComponent(product)}
                    style={{
                      display: 'flex',
                      alignItems: 'center',
                      gap: '0.5rem',
                      padding: '0.5rem 0.75rem',
                      cursor: 'pointer',
                      borderBottom: '1px solid #f3f4f6',
                    }}
                    onMouseEnter={(e) => {
                      (e.currentTarget as HTMLElement).style.background = '#f9fafb';
                    }}
                    onMouseLeave={(e) => {
                      (e.currentTarget as HTMLElement).style.background = 'transparent';
                    }}
                  >
                    <span style={{ flex: 1, fontSize: '0.875rem' }}>{product.name}</span>
                    <span
                      style={{ fontSize: '0.75rem', color: '#6b7280', fontFamily: 'monospace' }}
                    >
                      {'\u20AC'} {product.basePrice.amount.toFixed(2)}
                    </span>
                    <span style={{ color: '#9ca3af', fontSize: '0.875rem' }}>+ Add</span>
                  </div>
                ))}
              {simpleProducts.filter((p) => !selectedComponents.some((s) => s.id === p.id))
                .length === 0 && (
                <p style={{ padding: '0.75rem', color: '#9ca3af', fontSize: '0.875rem' }}>
                  {t('admin.comboProducts.allProductsSelected')}
                </p>
              )}
            </div>
          )}
        </section>

        {/* API error */}
        {createCombo.isError && (
          <p style={{ color: '#dc2626', marginBottom: '1rem', fontSize: '0.875rem' }}>
            {createCombo.error instanceof Error
              ? createCombo.error.message
              : 'Failed to create combo product. Please try again.'}
          </p>
        )}

        <div style={{ display: 'flex', gap: '0.75rem' }}>
          <button
            type="submit"
            disabled={createCombo.isPending}
            style={{
              padding: '0.5rem 1.25rem',
              background: '#111827',
              color: '#fff',
              border: 'none',
              borderRadius: '0.375rem',
              cursor: createCombo.isPending ? 'not-allowed' : 'pointer',
              fontWeight: 600,
              opacity: createCombo.isPending ? 0.6 : 1,
            }}
          >
            {createCombo.isPending ? 'Creating...' : t('admin.comboProducts.createButton')}
          </button>
          <button type="button" onClick={handleCancel} style={secondaryButtonStyle}>
            Cancel
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

const reorderButtonStyle: React.CSSProperties = {
  padding: '0.125rem 0.4rem',
  fontSize: '0.75rem',
  background: '#fff',
  border: '1px solid #d1d5db',
  borderRadius: '0.25rem',
  lineHeight: 1,
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
