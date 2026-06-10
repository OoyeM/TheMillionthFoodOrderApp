import { useState } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { useDeleteProduct, productKeys } from '../hooks/useProducts';
import { useResourceForm } from '../forms/useResourceForm';
import { productsApi } from '../../../api/products';
import type { UpdateProductRequest } from '../../../api/products';
import { productEditSchema, type ProductEditFormValues } from './schemas/productEditSchema';
import type { SupportedLocale, Product } from '../../../types/common';
import { labelStyle, inputStyle, secondaryButtonStyle, RequiredMark, FieldError } from '../forms/adminFormStyles';
import { ResourceFormShell } from '../forms/ResourceFormShell';
import { ProductTranslationFields } from '../forms/ProductTranslationFields';
import { ModifierGroupAssignments } from '../forms/ModifierGroupAssignments';
import { AllergenDietaryFields } from '../forms/AllergenDietaryFields';

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
  apiTranslations: Product['translations'],
): ProductEditFormValues['translations'] {
  const map: ProductEditFormValues['translations'] = {
    nl: { name: '', description: '' },
    fr: { name: '', description: '' },
    de: { name: '', description: '' },
  };
  for (const tr of apiTranslations) {
    const loc = tr.languageCode;
    if (loc in map) {
      map[loc] = { name: tr.name, description: tr.description ?? '' };
    }
  }
  return map;
}

// ---------------------------------------------------------------------------
// Page component
// ---------------------------------------------------------------------------

export function ProductEdit() {
  const navigate = useNavigate();
  const { t } = useTranslation();
  const { brandSlug, lang, productId } = useParams<{
    brandSlug: string;
    lang: string;
    productId: string;
  }>();

  const resolvedBrandSlug = brandSlug ?? '';
  const resolvedProductId = productId ?? '';

  const deleteProduct = useDeleteProduct(resolvedBrandSlug);

  // Active translation tab (UI-only state — not part of the form schema)
  const [activeTab, setActiveTab] = useState<SupportedLocale>('nl');

  // ---------------------------------------------------------------------------
  // Main form via useResourceForm
  // Note: the schema enforces NL as the required locale at the form layer.
  // The original code validated the brand's defaultLanguage (primaryLocale) instead.
  // Simplified here to always require NL, matching ComboProductEdit/MenuCategoryEdit.
  // ---------------------------------------------------------------------------

  const { form, submit, isSubmitting, isFetching, fetchError, submitError } = useResourceForm<
    Product,
    ProductEditFormValues,
    UpdateProductRequest
  >({
    queryKey: productKeys.detail(resolvedBrandSlug, resolvedProductId),
    fetch: () => productsApi.get(resolvedBrandSlug, resolvedProductId),
    update: (payload) => productsApi.update(resolvedBrandSlug, resolvedProductId, payload),
    schema: productEditSchema,
    defaultValues: {
      basePrice: 0,
      imageUrl: '',
      translations: {
        nl: { name: '', description: '' },
        fr: { name: '', description: '' },
        de: { name: '', description: '' },
      },
      allergens: [],
      dietaryTags: [],
    },
    toFormValues: (product) => ({
      basePrice: product.basePrice.amount,
      imageUrl: product.imageUrl ?? '',
      translations: buildTranslationsMap(product.translations),
      // allergens/dietaryTags stored as number[] at form layer (was Set<number>)
      // eslint-disable-next-line @typescript-eslint/no-unnecessary-condition -- product is API/JSON data; arrays may be absent at runtime despite the type
      allergens: product.allergens ?? [],
      // eslint-disable-next-line @typescript-eslint/no-unnecessary-condition -- product is API/JSON data; arrays may be absent at runtime despite the type
      dietaryTags: product.dietaryTags ?? [],
    }),
    toUpdatePayload: (values) => ({
      basePrice: values.basePrice,
      imageUrl: values.imageUrl.trim() || null,
      translations: (['nl', 'fr', 'de'] as const)
        .filter((loc) => values.translations[loc].name.trim().length > 0)
        .map((loc) => ({
          languageCode: loc,
          name: values.translations[loc].name.trim(),
          description: values.translations[loc].description.trim() || null,
        })),
      allergens: values.allergens,
      dietaryTags: values.dietaryTags,
    }),
    invalidate: [productKeys.all(resolvedBrandSlug)],
    onSuccess: () => { navigate(`/${String(brandSlug)}/${String(lang)}/admin/products`); },
  });

  // ---------------------------------------------------------------------------
  // Handlers
  // ---------------------------------------------------------------------------

  function handleDelete() {
    const nlName = form.getValues('translations.nl.name') || '(unnamed)';
    if (window.confirm(t('admin.products.confirmDelete', { name: nlName }))) {
      deleteProduct.mutate(resolvedProductId, {
        onSuccess: () => {
          navigate(`/${String(brandSlug)}/${String(lang)}/admin/products`);
        },
      });
    }
  }

  function handleCancel() {
    navigate(`/${String(brandSlug)}/${String(lang)}/admin/products`);
  }

  // ---------------------------------------------------------------------------
  // Form render
  // ---------------------------------------------------------------------------

  const {
    register,
    formState: { errors },
    watch,
    control,
  } = form;

  const watchedImageUrl = watch('imageUrl');
  // Pre-compute error messages to avoid complex type inference inside JSX
  const nlNameError = (errors.translations?.nl?.name as { message?: string } | undefined)?.message;

  return (
    <ResourceFormShell
      isFetching={isFetching}
      fetchError={fetchError}
      resourceName="product"
      onCancel={handleCancel}
    >
    <main style={{ padding: '1.5rem', maxWidth: '40rem' }}>
      <h1 style={{ fontSize: '1.5rem', fontWeight: 700, marginBottom: '1.5rem' }}>
        {t('admin.products.edit')}
      </h1>

      <form onSubmit={(e) => { e.preventDefault(); void submit(); }} noValidate>
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
            {...register('basePrice', { valueAsNumber: true })}
            style={inputStyle(!!errors.basePrice)}
            placeholder={t('admin.products.pricePlaceholder')}
          />
          {errors.basePrice?.message && <FieldError message={errors.basePrice.message} />}
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

        <AllergenDietaryFields mode="edit" control={control} />

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
              onClick={() => { setActiveTab(l.code); }}
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

        {/* Translation fields — rendered for all tabs, only the active one is visible */}
        <ProductTranslationFields
          activeTab={activeTab}
          register={register}
          nlNameError={nlNameError}
          t={t}
        />

        {/* API error */}
        {submitError != null && (
          <p style={{ color: '#dc2626', marginBottom: '1rem', fontSize: '0.875rem' }}>
            {submitError instanceof Error
              ? submitError.message
              : t('admin.products.updateError')}
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
            {isSubmitting ? t('admin.products.saving') : t('admin.products.saveChanges')}
          </button>
          <button type="button" onClick={handleCancel} style={secondaryButtonStyle}>
            {t('admin.products.cancel')}
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
            {deleteProduct.isPending ? t('admin.products.deleting') : t('admin.products.delete')}
          </button>
        </div>
      </form>

      {/* Modifier Groups */}
      <ModifierGroupAssignments brandSlug={resolvedBrandSlug} productId={resolvedProductId} />
    </main>
    </ResourceFormShell>
  );
}

