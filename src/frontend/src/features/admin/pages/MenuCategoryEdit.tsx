import { useState, useEffect } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import {
  useMenuCategory,
  useUpdateMenuCategory,
  useDeleteMenuCategory,
  useCategoryProducts,
  useReorderCategoryProducts,
} from '../hooks/useMenuCategories';
import { useBrandSettings } from '../hooks/useBrandSettings';
import { extractPrimaryLocale } from '../../../types/common';
import type { ProductListItem, SupportedLocale } from '../../../types/common';

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

export function MenuCategoryEdit() {
  const navigate = useNavigate();
  const { t } = useTranslation();
  const { brandSlug, lang, categoryId } = useParams<{
    brandSlug: string;
    lang: string;
    categoryId: string;
  }>();

  const resolvedBrandSlug = brandSlug ?? '';
  const resolvedCategoryId = categoryId ?? '';
  const { data: brandSettings } = useBrandSettings(resolvedBrandSlug);
  const primaryLocale = extractPrimaryLocale(brandSettings?.defaultLanguage);

  const {
    data: category,
    isLoading,
    isError,
    error,
  } = useMenuCategory(resolvedBrandSlug, resolvedCategoryId);
  const updateCategory = useUpdateMenuCategory(resolvedBrandSlug, resolvedCategoryId);
  const deleteCategory = useDeleteMenuCategory(resolvedBrandSlug);

  const {
    data: categoryProducts,
    isLoading: isLoadingProducts,
    isError: isErrorProducts,
  } = useCategoryProducts(resolvedBrandSlug, resolvedCategoryId);
  const reorderProducts = useReorderCategoryProducts(resolvedBrandSlug, resolvedCategoryId);

  const [activeTab, setActiveTab] = useState<SupportedLocale>('nl');
  const [translations, setTranslations] = useState<TranslationsMap>({ ...emptyTranslations });
  const [sortOrder, setSortOrder] = useState('0');
  const [imageUrl, setImageUrl] = useState('');
  const [errors, setErrors] = useState<FormErrors>({});
  const [formInitialized, setFormInitialized] = useState(false);

  // Local ordered list of products — populated from server data, then mutated by move up/down
  const [orderedProducts, setOrderedProducts] = useState<ProductListItem[]>([]);
  const [productOrderDirty, setProductOrderDirty] = useState(false);

  // Sync orderedProducts when server data arrives (and when not dirty)
  useEffect(() => {
    if (categoryProducts !== undefined && !productOrderDirty) {
      const sorted = [...categoryProducts].sort(
        (a, b) => a.sortOrderInCategory - b.sortOrderInCategory,
      );
      setOrderedProducts(sorted);
    }
  }, [categoryProducts, productOrderDirty]);

  // Populate form when category data arrives
  useEffect(() => {
    if (category !== undefined && !formInitialized) {
      setSortOrder(category.sortOrder.toString());
      setImageUrl(category.imageUrl ?? '');

      const translationsMap: TranslationsMap = { ...emptyTranslations };
      for (const t of category.translations) {
        if (t.languageCode in translationsMap) {
          translationsMap[t.languageCode as SupportedLocale] = { name: t.name };
        }
      }
      setTranslations(translationsMap);
      setFormInitialized(true);
    }
  }, [category, formInitialized]);

  function updateTranslation(locale: SupportedLocale, value: string) {
    setTranslations((prev) => ({
      ...prev,
      [locale]: { name: value },
    }));
  }

  function moveProduct(index: number, direction: 'up' | 'down') {
    const swapIndex = direction === 'up' ? index - 1 : index + 1;
    if (swapIndex < 0 || swapIndex >= orderedProducts.length) return;
    const next = [...orderedProducts];
    // Bounds are validated above; non-null assertions are safe here.
    // eslint-disable-next-line @typescript-eslint/no-non-null-assertion
    const a = next[index]!;
    // eslint-disable-next-line @typescript-eslint/no-non-null-assertion
    const b = next[swapIndex]!;
    next[index] = b;
    next[swapIndex] = a;
    setOrderedProducts(next);
    setProductOrderDirty(true);
  }

  function handleSaveOrder() {
    reorderProducts.mutate(
      orderedProducts.map((p) => p.id),
      {
        onSuccess: () => {
          setProductOrderDirty(false);
        },
      },
    );
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

    updateCategory.mutate(
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

  function handleDelete() {
    const name = translations.nl.name || '(unnamed)';
    if (
      window.confirm(
        `Delete "${name}"? This will remove the category. Products assigned to it will be unassigned.`,
      )
    ) {
      deleteCategory.mutate(resolvedCategoryId, {
        onSuccess: () => {
          navigate(`/${brandSlug}/${lang}/admin/menu-categories`);
        },
      });
    }
  }

  function handleCancel() {
    navigate(`/${brandSlug}/${lang}/admin/menu-categories`);
  }

  // ---------------------------------------------------------------------------
  // Loading / error states
  // ---------------------------------------------------------------------------

  if (isLoading) {
    return (
      <main style={{ padding: '1.5rem' }}>
        <p style={{ color: '#6b7280' }}>Loading menu category...</p>
      </main>
    );
  }

  if (isError) {
    return (
      <main style={{ padding: '1.5rem' }}>
        <p style={{ color: '#dc2626' }}>
          Failed to load menu category:{' '}
          {error instanceof Error ? error.message : 'Unknown error'}
        </p>
        <button onClick={handleCancel} style={secondaryButtonStyle}>
          Back to list
        </button>
      </main>
    );
  }

  if (category === undefined) {
    return (
      <main style={{ padding: '1.5rem' }}>
        <p style={{ color: '#6b7280' }}>Menu category not found.</p>
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
    <main style={{ padding: '1.5rem', maxWidth: '48rem' }}>
      <h1 style={{ fontSize: '1.5rem', fontWeight: 700, marginBottom: '1.5rem' }}>
        Edit Menu Category
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
        <div style={{ marginBottom: '0.5rem' }}>
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

        {/* Metadata */}
        <p style={{ fontSize: '0.75rem', color: '#9ca3af', marginBottom: '1.5rem' }}>
          Created: {new Date(category.createdAt).toLocaleString()} &mdash; Last updated:{' '}
          {new Date(category.updatedAt).toLocaleString()}
        </p>

        {/* API error */}
        {updateCategory.isError && (
          <p style={{ color: '#dc2626', marginBottom: '1rem', fontSize: '0.875rem' }}>
            {updateCategory.error instanceof Error
              ? updateCategory.error.message
              : 'Failed to save changes. Please try again.'}
          </p>
        )}

        <div style={{ display: 'flex', gap: '0.75rem' }}>
          <button
            type="submit"
            disabled={updateCategory.isPending}
            style={{
              padding: '0.5rem 1.25rem',
              background: '#111827',
              color: '#fff',
              border: 'none',
              borderRadius: '0.375rem',
              cursor: updateCategory.isPending ? 'not-allowed' : 'pointer',
              fontWeight: 600,
              opacity: updateCategory.isPending ? 0.6 : 1,
            }}
          >
            {updateCategory.isPending ? 'Saving...' : 'Save Changes'}
          </button>
          <button type="button" onClick={handleCancel} style={secondaryButtonStyle}>
            Cancel
          </button>
          <button
            type="button"
            onClick={handleDelete}
            disabled={deleteCategory.isPending}
            style={{
              padding: '0.5rem 1.25rem',
              background: '#fff',
              color: '#dc2626',
              border: '1px solid #fca5a5',
              borderRadius: '0.375rem',
              cursor: deleteCategory.isPending ? 'not-allowed' : 'pointer',
              opacity: deleteCategory.isPending ? 0.6 : 1,
              marginLeft: 'auto',
            }}
          >
            {deleteCategory.isPending ? 'Deleting...' : 'Delete Category'}
          </button>
        </div>
      </form>

      {/* ----------------------------------------------------------------- */}
      {/* Products in this Category                                          */}
      {/* ----------------------------------------------------------------- */}
      <section style={{ marginTop: '2.5rem' }}>
        <div
          style={{
            display: 'flex',
            justifyContent: 'space-between',
            alignItems: 'center',
            marginBottom: '1rem',
          }}
        >
          <h2 style={{ fontSize: '1.125rem', fontWeight: 700, margin: 0 }}>
            {t('admin.menuCategories.productsSection.title')}
          </h2>
          {productOrderDirty && (
            <button
              type="button"
              onClick={handleSaveOrder}
              disabled={reorderProducts.isPending}
              style={{
                padding: '0.375rem 1rem',
                background: '#111827',
                color: '#fff',
                border: 'none',
                borderRadius: '0.375rem',
                cursor: reorderProducts.isPending ? 'not-allowed' : 'pointer',
                fontWeight: 600,
                fontSize: '0.875rem',
                opacity: reorderProducts.isPending ? 0.6 : 1,
              }}
            >
              {reorderProducts.isPending
                ? t('admin.menuCategories.productsSection.savingOrder')
                : t('admin.menuCategories.productsSection.saveOrder')}
            </button>
          )}
        </div>

        {reorderProducts.isError && (
          <p style={{ color: '#dc2626', fontSize: '0.875rem', marginBottom: '0.75rem' }}>
            {reorderProducts.error instanceof Error
              ? reorderProducts.error.message
              : t('admin.menuCategories.productsSection.saveOrderError')}
          </p>
        )}

        {isLoadingProducts && (
          <p style={{ color: '#6b7280' }}>
            {t('admin.menuCategories.productsSection.loading')}
          </p>
        )}

        {isErrorProducts && (
          <p style={{ color: '#dc2626' }}>
            {t('admin.menuCategories.productsSection.loadError')}
          </p>
        )}

        {!isLoadingProducts && !isErrorProducts && orderedProducts.length === 0 && (
          <p style={{ color: '#6b7280' }}>
            {t('admin.menuCategories.productsSection.empty')}
          </p>
        )}

        {!isLoadingProducts && !isErrorProducts && orderedProducts.length > 0 && (
          <table
            style={{
              width: '100%',
              borderCollapse: 'collapse',
              fontSize: '0.9rem',
            }}
          >
            <thead>
              <tr style={{ borderBottom: '2px solid #e5e7eb', textAlign: 'left' }}>
                <th style={{ padding: '0.5rem 0.75rem', fontWeight: 600, width: '3rem' }}>
                  {t('admin.menuCategories.productsSection.position')}
                </th>
                <th style={{ padding: '0.5rem 0.75rem', fontWeight: 600 }}>
                  {t('admin.menuCategories.productsSection.productName')}
                </th>
                <th style={{ padding: '0.5rem 0.75rem', fontWeight: 600 }}>
                  {t('admin.menuCategories.productsSection.basePrice')}
                </th>
                <th style={{ padding: '0.5rem 0.75rem', fontWeight: 600, width: '6rem' }}>
                  {t('admin.menuCategories.productsSection.order')}
                </th>
              </tr>
            </thead>
            <tbody>
              {orderedProducts.map((product, index) => (
                <tr
                  key={product.id}
                  style={{ borderBottom: '1px solid #e5e7eb' }}
                >
                  <td
                    style={{
                      padding: '0.75rem',
                      color: '#6b7280',
                      fontVariantNumeric: 'tabular-nums',
                    }}
                  >
                    {index + 1}
                  </td>
                  <td style={{ padding: '0.75rem' }}>{product.name}</td>
                  <td style={{ padding: '0.75rem', fontFamily: 'monospace' }}>
                    {'\u20AC'} {product.basePrice.amount.toFixed(2)}
                  </td>
                  <td style={{ padding: '0.75rem' }}>
                    <div style={{ display: 'flex', gap: '0.25rem' }}>
                      <button
                        type="button"
                        onClick={() => moveProduct(index, 'up')}
                        disabled={index === 0}
                        aria-label={t('admin.menuCategories.productsSection.moveUp')}
                        style={moveButtonStyle(index === 0)}
                      >
                        ↑
                      </button>
                      <button
                        type="button"
                        onClick={() => moveProduct(index, 'down')}
                        disabled={index === orderedProducts.length - 1}
                        aria-label={t('admin.menuCategories.productsSection.moveDown')}
                        style={moveButtonStyle(index === orderedProducts.length - 1)}
                      >
                        ↓
                      </button>
                    </div>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        )}
      </section>
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

function moveButtonStyle(disabled: boolean): React.CSSProperties {
  return {
    padding: '0.25rem 0.5rem',
    fontSize: '0.875rem',
    background: '#fff',
    border: '1px solid #d1d5db',
    borderRadius: '0.25rem',
    cursor: disabled ? 'not-allowed' : 'pointer',
    opacity: disabled ? 0.3 : 1,
    lineHeight: 1,
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
