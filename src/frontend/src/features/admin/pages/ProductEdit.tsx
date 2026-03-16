import { useState, useEffect } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { useProduct, useUpdateProduct, useDeleteProduct } from '../hooks/useProducts';
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
  nlName?: string;
}

// ---------------------------------------------------------------------------
// Page component
// ---------------------------------------------------------------------------

export function ProductEdit() {
  const navigate = useNavigate();
  const { brandSlug, lang, productId } = useParams<{
    brandSlug: string;
    lang: string;
    productId: string;
  }>();

  const resolvedBrandSlug = brandSlug ?? '';
  const resolvedProductId = productId ?? '';

  const { data: product, isLoading, isError, error } = useProduct(resolvedBrandSlug, resolvedProductId);
  const updateProduct = useUpdateProduct(resolvedBrandSlug, resolvedProductId);
  const deleteProduct = useDeleteProduct(resolvedBrandSlug);

  const [activeTab, setActiveTab] = useState<SupportedLocale>('nl');
  const [translations, setTranslations] = useState<TranslationsMap>({ ...emptyTranslations });
  const [basePrice, setBasePrice] = useState('');
  const [imageUrl, setImageUrl] = useState('');
  const [errors, setErrors] = useState<FormErrors>({});
  const [formInitialized, setFormInitialized] = useState(false);

  // Populate form when product data arrives
  useEffect(() => {
    if (product !== undefined && !formInitialized) {
      setBasePrice(product.basePrice.amount.toString());
      setImageUrl(product.imageUrl ?? '');

      const translationsMap: TranslationsMap = { ...emptyTranslations };
      for (const t of product.translations) {
        if (t.languageCode in translationsMap) {
          translationsMap[t.languageCode as SupportedLocale] = {
            name: t.name,
            description: t.description ?? '',
          };
        }
      }
      setTranslations(translationsMap);
      setFormInitialized(true);
    }
  }, [product, formInitialized]);

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
      next.basePrice = 'Base price must be greater than zero.';
    }
    if (translations.nl.name.trim().length === 0) {
      next.nlName = 'Dutch (NL) name is required.';
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

    updateProduct.mutate(
      {
        basePrice: parseFloat(basePrice),
        imageUrl: imageUrl.trim() || null,
        translations: translationInputs,
      },
      {
        onSuccess: () => {
          navigate(`/${brandSlug}/${lang}/admin/products`);
        },
      },
    );
  }

  function handleDelete() {
    const name = translations.nl.name || '(unnamed)';
    if (window.confirm(`Delete "${name}"? This product will be hidden from the storefront.`)) {
      deleteProduct.mutate(resolvedProductId, {
        onSuccess: () => {
          navigate(`/${brandSlug}/${lang}/admin/products`);
        },
      });
    }
  }

  function handleCancel() {
    navigate(`/${brandSlug}/${lang}/admin/products`);
  }

  // ---------------------------------------------------------------------------
  // Loading / error states
  // ---------------------------------------------------------------------------

  if (isLoading) {
    return (
      <main style={{ padding: '1.5rem' }}>
        <p style={{ color: '#6b7280' }}>Loading product...</p>
      </main>
    );
  }

  if (isError) {
    return (
      <main style={{ padding: '1.5rem' }}>
        <p style={{ color: '#dc2626' }}>
          Failed to load product:{' '}
          {error instanceof Error ? error.message : 'Unknown error'}
        </p>
        <button onClick={handleCancel} style={secondaryButtonStyle}>
          Back to list
        </button>
      </main>
    );
  }

  if (product === undefined) {
    return (
      <main style={{ padding: '1.5rem' }}>
        <p style={{ color: '#6b7280' }}>Product not found.</p>
        <button onClick={handleCancel} style={secondaryButtonStyle}>
          Back to list
        </button>
      </main>
    );
  }

  // ---------------------------------------------------------------------------
  // Form
  // ---------------------------------------------------------------------------

  return (
    <main style={{ padding: '1.5rem', maxWidth: '40rem' }}>
      <h1 style={{ fontSize: '1.5rem', fontWeight: 700, marginBottom: '1.5rem' }}>
        Edit Product
      </h1>

      <form onSubmit={handleSubmit} noValidate>
        {/* Base Price */}
        <div style={{ marginBottom: '1rem' }}>
          <label style={labelStyle} htmlFor="basePrice">
            Base Price (EUR) <RequiredMark />
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
            placeholder={`Product name in ${activeTab.toUpperCase()}`}
          />
          {activeTab === 'nl' && errors.nlName && <FieldError message={errors.nlName} />}
        </div>

        <div style={{ marginBottom: '0.5rem' }}>
          <label style={labelStyle} htmlFor={`desc-${activeTab}`}>
            Description <span style={{ color: '#9ca3af', fontWeight: 400 }}>(optional)</span>
          </label>
          <textarea
            id={`desc-${activeTab}`}
            value={translations[activeTab].description}
            onChange={(e) => updateTranslation(activeTab, 'description', e.target.value)}
            rows={3}
            style={{ ...inputStyle(false), resize: 'vertical' }}
            placeholder={`Product description in ${activeTab.toUpperCase()}`}
          />
        </div>

        {/* Metadata */}
        <p style={{ fontSize: '0.75rem', color: '#9ca3af', marginBottom: '1.5rem' }}>
          Created: {new Date(product.createdAt).toLocaleString()} &mdash; Last updated:{' '}
          {new Date(product.updatedAt).toLocaleString()}
        </p>

        {/* API error */}
        {updateProduct.isError && (
          <p style={{ color: '#dc2626', marginBottom: '1rem', fontSize: '0.875rem' }}>
            {updateProduct.error instanceof Error
              ? updateProduct.error.message
              : 'Failed to save changes. Please try again.'}
          </p>
        )}

        <div style={{ display: 'flex', gap: '0.75rem' }}>
          <button
            type="submit"
            disabled={updateProduct.isPending}
            style={{
              padding: '0.5rem 1.25rem',
              background: '#111827',
              color: '#fff',
              border: 'none',
              borderRadius: '0.375rem',
              cursor: updateProduct.isPending ? 'not-allowed' : 'pointer',
              fontWeight: 600,
              opacity: updateProduct.isPending ? 0.6 : 1,
            }}
          >
            {updateProduct.isPending ? 'Saving...' : 'Save Changes'}
          </button>
          <button type="button" onClick={handleCancel} style={secondaryButtonStyle}>
            Cancel
          </button>
          <button
            type="button"
            onClick={handleDelete}
            disabled={deleteProduct.isPending}
            style={{
              padding: '0.5rem 1.25rem',
              background: '#fff',
              color: '#dc2626',
              border: '1px solid #fca5a5',
              borderRadius: '0.375rem',
              cursor: deleteProduct.isPending ? 'not-allowed' : 'pointer',
              opacity: deleteProduct.isPending ? 0.6 : 1,
              marginLeft: 'auto',
            }}
          >
            {deleteProduct.isPending ? 'Deleting...' : 'Delete Product'}
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
