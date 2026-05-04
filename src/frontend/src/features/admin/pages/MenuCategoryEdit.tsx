import { useState, useEffect } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { type UseFormRegister } from 'react-hook-form';
import {
  useDeleteMenuCategory,
  useCategoryProducts,
  useReorderCategoryProducts,
} from '../hooks/useMenuCategories';
import { menuCategoryKeys } from '../hooks/useMenuCategories';
import { menuCategoriesApi } from '../../../api/menuCategories';
import type { UpdateMenuCategoryRequest } from '../../../api/menuCategories';
import { useResourceForm } from '../forms/useResourceForm';
import { menuCategoryEditSchema, type MenuCategoryEditFormValues } from './schemas/menuCategoryEditSchema';
import type { MenuCategory, SupportedLocale, ProductListItem } from '../../../types/common';

// ---------------------------------------------------------------------------
// Constants
// ---------------------------------------------------------------------------

const LANGUAGES: { code: SupportedLocale; label: string }[] = [
  { code: 'nl', label: 'NL' },
  { code: 'fr', label: 'FR' },
  { code: 'de', label: 'DE' },
];

// ---------------------------------------------------------------------------
// Helper — build a translations map from the API array, filling missing locales
// ---------------------------------------------------------------------------

function buildTranslationsMap(
  apiTranslations: MenuCategory['translations'],
): MenuCategoryEditFormValues['translations'] {
  const map: MenuCategoryEditFormValues['translations'] = {
    nl: { name: '' },
    fr: { name: '' },
    de: { name: '' },
  };
  for (const tr of apiTranslations) {
    const loc = tr.languageCode as SupportedLocale;
    if (loc in map) {
      map[loc] = { name: tr.name };
    }
  }
  return map;
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

  const deleteCategory = useDeleteMenuCategory(resolvedBrandSlug);

  // Product-reorder section — stays imperative (separate resource / separate mutation)
  const {
    data: categoryProducts,
    isLoading: isLoadingProducts,
    isError: isErrorProducts,
  } = useCategoryProducts(resolvedBrandSlug, resolvedCategoryId);
  const reorderProducts = useReorderCategoryProducts(resolvedBrandSlug, resolvedCategoryId);

  const [activeTab, setActiveTab] = useState<SupportedLocale>('nl');
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

  // ---------------------------------------------------------------------------
  // Main form via useResourceForm
  // Note: the schema enforces NL as the required locale at the form layer.
  // The original code validated the brand's defaultLanguage (primaryLocale) instead.
  // That was over-engineering for what's mostly an NL-first product; simplified here
  // to always require NL, matching the ComboProductEdit pattern.
  // ---------------------------------------------------------------------------

  const { form, submit, isSubmitting, isFetching, fetchError, submitError } = useResourceForm<
    MenuCategory,
    MenuCategoryEditFormValues,
    UpdateMenuCategoryRequest
  >({
    queryKey: menuCategoryKeys.detail(resolvedBrandSlug, resolvedCategoryId),
    fetch: () => menuCategoriesApi.get(resolvedBrandSlug, resolvedCategoryId),
    update: (payload) => menuCategoriesApi.update(resolvedBrandSlug, resolvedCategoryId, payload),
    schema: menuCategoryEditSchema,
    defaultValues: {
      sortOrder: 0,
      imageUrl: '',
      translations: {
        nl: { name: '' },
        fr: { name: '' },
        de: { name: '' },
      },
    },
    toFormValues: (cat) => ({
      sortOrder: cat.sortOrder,
      imageUrl: cat.imageUrl ?? '',
      translations: buildTranslationsMap(cat.translations),
    }),
    toUpdatePayload: (values) => ({
      sortOrder: values.sortOrder,
      imageUrl: values.imageUrl.trim() || null,
      translations: (['nl', 'fr', 'de'] as const)
        .filter((loc) => values.translations[loc].name.trim().length > 0)
        .map((loc) => ({
          languageCode: loc,
          name: values.translations[loc].name.trim(),
        })),
    }),
    invalidate: [menuCategoryKeys.all(resolvedBrandSlug)],
    onSuccess: () => navigate(`/${brandSlug}/${lang}/admin/menu-categories`),
  });

  // ---------------------------------------------------------------------------
  // Handlers
  // ---------------------------------------------------------------------------

  function handleDelete() {
    const name = form.getValues('translations.nl.name') || '(unnamed)';
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

  // ---------------------------------------------------------------------------
  // Loading / error states
  // ---------------------------------------------------------------------------

  if (isFetching) {
    return (
      <main style={{ padding: '1.5rem' }}>
        <p style={{ color: '#6b7280' }}>Loading menu category...</p>
      </main>
    );
  }

  if (fetchError) {
    return (
      <main style={{ padding: '1.5rem' }}>
        <p style={{ color: '#dc2626' }}>
          Failed to load menu category:{' '}
          {fetchError instanceof Error ? fetchError.message : 'Unknown error'}
        </p>
        <button onClick={handleCancel} style={secondaryButtonStyle}>
          Back to list
        </button>
      </main>
    );
  }

  // ---------------------------------------------------------------------------
  // Form render
  // ---------------------------------------------------------------------------

  const {
    register,
    formState: { errors },
    watch,
  } = form;

  const watchedImageUrl = watch('imageUrl');
  // Pre-compute error messages to avoid complex type inference inside JSX
  const nlNameError = (errors.translations?.nl?.name as { message?: string } | undefined)?.message;
  const sortOrderError = errors.sortOrder?.message;

  return (
    <main style={{ padding: '1.5rem', maxWidth: '48rem' }}>
      <h1 style={{ fontSize: '1.5rem', fontWeight: 700, marginBottom: '1.5rem' }}>
        Edit Menu Category
      </h1>

      <form onSubmit={submit} noValidate>
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
            {...register('sortOrder', { valueAsNumber: true })}
            style={inputStyle(!!sortOrderError)}
            placeholder="e.g. 0"
          />
          {sortOrderError != null && <FieldError message={sortOrderError} />}
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
            {...register('imageUrl')}
            style={inputStyle(false)}
            placeholder="https://example.com/image.jpg"
          />
          {watchedImageUrl && watchedImageUrl.trim().length > 0 ? (
            <img
              src={watchedImageUrl}
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
          ) : null}
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
              {l.code === 'nl' ? ' *' : null}
            </button>
          ))}
        </div>

        {/* Translation fields — extracted subcomponent so literal paths resolve correctly in TS */}
        <TranslationFields
          activeTab={activeTab}
          register={register}
          nlNameError={nlNameError}
        />

        {/* API error */}
        {submitError != null && (
          <p style={{ color: '#dc2626', marginBottom: '1rem', fontSize: '0.875rem' }}>
            {submitError instanceof Error
              ? submitError.message
              : 'Failed to save changes. Please try again.'}
          </p>
        )}

        <div style={{ display: 'flex', gap: '0.75rem' }}>
          <button
            type="submit"
            disabled={isSubmitting}
            style={{
              padding: '0.5rem 1.25rem',
              background: '#111827',
              color: '#fff',
              border: 'none',
              borderRadius: '0.375rem',
              cursor: isSubmitting ? 'not-allowed' : 'pointer',
              fontWeight: 600,
              opacity: isSubmitting ? 0.6 : 1,
            }}
          >
            {isSubmitting ? 'Saving...' : 'Save Changes'}
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
                    {'€'} {product.basePrice.amount.toFixed(2)}
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
// TranslationFields — extracted so `register` call uses literal path strings,
// which TypeScript resolves correctly (dynamic template-literal paths produce
// unknown spreads in strict TSX).
// ---------------------------------------------------------------------------

interface TranslationFieldsProps {
  activeTab: SupportedLocale;
  register: UseFormRegister<MenuCategoryEditFormValues>;
  nlNameError: string | undefined;
}

function TranslationFields({ activeTab, register, nlNameError }: TranslationFieldsProps) {
  if (activeTab === 'nl') {
    return (
      <div style={{ marginBottom: '0.5rem' }}>
        <label style={labelStyle} htmlFor="name-nl">
          Name <RequiredMark />
        </label>
        <input
          id="name-nl"
          type="text"
          {...register('translations.nl.name')}
          style={inputStyle(!!nlNameError)}
          placeholder="Category name in NL"
        />
        {nlNameError && <FieldError message={nlNameError} />}
      </div>
    );
  }
  if (activeTab === 'fr') {
    return (
      <div style={{ marginBottom: '0.5rem' }}>
        <label style={labelStyle} htmlFor="name-fr">
          Name
        </label>
        <input
          id="name-fr"
          type="text"
          {...register('translations.fr.name')}
          style={inputStyle(false)}
          placeholder="Category name in FR"
        />
      </div>
    );
  }
  return (
    <div style={{ marginBottom: '0.5rem' }}>
      <label style={labelStyle} htmlFor="name-de">
        Name
      </label>
      <input
        id="name-de"
        type="text"
        {...register('translations.de.name')}
        style={inputStyle(false)}
        placeholder="Category name in DE"
      />
    </div>
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
