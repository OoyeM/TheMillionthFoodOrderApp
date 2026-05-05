import { useState } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { useForm, type UseFormRegister } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import { productsApi } from '@api/products';
import type { CreateComboProductRequest } from '@api/products';
import { comboProductEditSchema, type ComboProductEditFormValues } from './schemas/comboProductEditSchema';
import { productKeys, useProducts } from '../hooks/useProducts';
import type { SupportedLocale, ProductListItem } from '../../../types/common';

// ---------------------------------------------------------------------------
// Constants
// ---------------------------------------------------------------------------

const LANGUAGES: { code: SupportedLocale; label: string }[] = [
  { code: 'nl', label: 'NL' },
  { code: 'fr', label: 'FR' },
  { code: 'de', label: 'DE' },
];

// ---------------------------------------------------------------------------
// Payload mapper
// ---------------------------------------------------------------------------

function toCreatePayload(values: ComboProductEditFormValues): CreateComboProductRequest {
  return {
    basePrice: values.basePrice,
    imageUrl: values.imageUrl.trim() || null,
    translations: (['nl', 'fr', 'de'] as const)
      .filter((loc) => values.translations[loc].name.trim().length > 0)
      .map((loc) => ({
        languageCode: loc,
        name: values.translations[loc].name.trim(),
        description: values.translations[loc].description.trim() || null,
      })),
    componentProductIds: values.componentProductIds,
  };
}

// ---------------------------------------------------------------------------
// Page component
// ---------------------------------------------------------------------------

export function ComboProductCreate() {
  const navigate = useNavigate();
  const { t } = useTranslation();
  const queryClient = useQueryClient();
  const { brandSlug = '', lang = '' } = useParams<{ brandSlug: string; lang: string }>();

  const [activeTab, setActiveTab] = useState<SupportedLocale>('nl');

  const { data: allProducts } = useProducts(brandSlug);
  const simpleProducts = allProducts?.filter((p) => p.productType === 'Simple') ?? [];

  const form = useForm<ComboProductEditFormValues>({
    resolver: zodResolver(comboProductEditSchema),
    defaultValues: {
      basePrice: 0,
      imageUrl: '',
      translations: {
        nl: { name: '', description: '' },
        fr: { name: '', description: '' },
        de: { name: '', description: '' },
      },
      componentProductIds: [],
    },
  });

  const {
    register,
    handleSubmit,
    watch,
    setValue,
    formState: { errors, isSubmitting },
  } = form;

  const mutation = useMutation({
    mutationFn: (payload: CreateComboProductRequest) =>
      productsApi.createCombo(brandSlug, payload),
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: productKeys.all(brandSlug) });
      navigate('..');
    },
  });

  const onSubmit = handleSubmit((values) => {
    void mutation.mutateAsync(toCreatePayload(values));
  });

  function handleCancel() {
    navigate(`/${brandSlug}/${lang}/admin/products`);
  }

  // Component product selection/reorder — using form.watch + form.setValue
  const currentComponentIds = watch('componentProductIds');

  function handleToggleComponent(product: ProductListItem) {
    const exists = currentComponentIds.includes(product.id);
    if (exists) {
      setValue('componentProductIds', currentComponentIds.filter((id) => id !== product.id));
    } else {
      setValue('componentProductIds', [...currentComponentIds, product.id]);
    }
  }

  function handleMoveUp(index: number) {
    if (index === 0) return;
    const next = [...currentComponentIds];
    [next[index - 1], next[index]] = [next[index]!, next[index - 1]!];
    setValue('componentProductIds', next);
  }

  function handleMoveDown(index: number) {
    if (index >= currentComponentIds.length - 1) return;
    const next = [...currentComponentIds];
    [next[index], next[index + 1]] = [next[index + 1]!, next[index]!];
    setValue('componentProductIds', next);
  }

  const watchedImageUrl = watch('imageUrl');
  // Pre-compute error messages to avoid complex type inference inside JSX
  const nlNameError = (errors.translations?.nl?.name as { message?: string } | undefined)?.message;
  const componentIdsError = (errors.componentProductIds as { message?: string } | undefined)?.message;

  // Derive the ordered list of selected product objects from the ID list
  const selectedProducts = currentComponentIds
    .map((id) => allProducts?.find((p) => p.id === id))
    .filter((p): p is ProductListItem => p !== undefined);

  return (
    <main style={{ padding: '1.5rem', maxWidth: '40rem' }}>
      <h1 style={{ fontSize: '1.5rem', fontWeight: 700, marginBottom: '1.5rem' }}>
        {t('admin.comboProducts.create')}
      </h1>

      <form onSubmit={(e) => { e.preventDefault(); void onSubmit(); }} noValidate>
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
            {...register('basePrice', { valueAsNumber: true })}
            style={inputStyle(!!errors.basePrice)}
            placeholder="e.g. 3.50"
          />
          {errors.basePrice?.message && <FieldError message={errors.basePrice.message} />}
        </div>

        {/* Image URL */}
        <div style={{ marginBottom: '1.5rem' }}>
          <label style={labelStyle} htmlFor="imageUrl">
            Image URL <span style={{ color: '#9ca3af', fontWeight: 400 }}>(optional)</span>
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
              {l.code === 'nl' ? ' *' : null}
            </button>
          ))}
        </div>

        {/* Translation fields — rendered for all tabs, only the active one is visible */}
        <TranslationFields
          activeTab={activeTab}
          register={register}
          nlNameError={nlNameError}
        />

        {/* Component Products */}
        <section style={{ marginBottom: '1.5rem' }}>
          <p style={{ fontWeight: 600, fontSize: '0.875rem', marginBottom: '0.5rem' }}>
            {t('admin.comboProducts.componentProducts')} <RequiredMark />
          </p>
          <p style={{ fontSize: '0.75rem', color: '#6b7280', marginBottom: '0.75rem' }}>
            {t('admin.comboProducts.componentProductsHint')}
          </p>

          {componentIdsError && <FieldError message={componentIdsError} />}

          {/* Selected components with reorder controls */}
          {selectedProducts.length > 0 && (
            <div style={{ marginBottom: '0.75rem' }}>
              {selectedProducts.map((product, index) => (
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
                    {'€'} {product.basePrice.amount.toFixed(2)}
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
                    disabled={index === selectedProducts.length - 1}
                    style={{
                      ...reorderButtonStyle,
                      opacity: index === selectedProducts.length - 1 ? 0.3 : 1,
                      cursor: index === selectedProducts.length - 1 ? 'not-allowed' : 'pointer',
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
          {simpleProducts.length === 0 && selectedProducts.length === 0 && (
            <p style={{ color: '#6b7280', fontSize: '0.875rem', marginBottom: '0.75rem' }}>
              {t('admin.comboProducts.noSimpleProducts')}
            </p>
          )}
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
                .filter((p) => !currentComponentIds.includes(p.id))
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
                      {'€'} {product.basePrice.amount.toFixed(2)}
                    </span>
                    <span style={{ color: '#9ca3af', fontSize: '0.875rem' }}>+ Add</span>
                  </div>
                ))}
              {simpleProducts.filter((p) => !currentComponentIds.includes(p.id)).length === 0 && (
                <p style={{ padding: '0.75rem', color: '#9ca3af', fontSize: '0.875rem' }}>
                  {t('admin.comboProducts.allProductsSelected')}
                </p>
              )}
            </div>
          )}
        </section>

        {/* API error */}
        {mutation.error != null && (
          <p style={{ color: '#dc2626', marginBottom: '1rem', fontSize: '0.875rem' }}>
            {mutation.error instanceof Error
              ? mutation.error.message
              : 'Failed to create combo product. Please try again.'}
          </p>
        )}

        <div style={{ display: 'flex', gap: '0.75rem' }}>
          <button
            type="submit"
            disabled={isSubmitting || mutation.isPending}
            style={{
              padding: '0.5rem 1.25rem',
              background: '#111827',
              color: '#fff',
              border: 'none',
              borderRadius: '0.375rem',
              cursor: isSubmitting || mutation.isPending ? 'not-allowed' : 'pointer',
              fontWeight: 600,
              opacity: isSubmitting || mutation.isPending ? 0.6 : 1,
            }}
          >
            {mutation.isPending ? t('admin.comboProducts.creating') : t('admin.comboProducts.createButton')}
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
// TranslationFields — extracted so `register` call uses literal path strings,
// which TypeScript resolves correctly (dynamic template-literal paths produce
// unknown spreads in strict TSX).
// ---------------------------------------------------------------------------

interface TranslationFieldsProps {
  activeTab: SupportedLocale;
  register: UseFormRegister<ComboProductEditFormValues>;
  nlNameError: string | undefined;
}

function TranslationFields({ activeTab, register, nlNameError }: TranslationFieldsProps) {
  if (activeTab === 'nl') {
    return (
      <>
        <div style={{ marginBottom: '1rem' }}>
          <label style={labelStyle} htmlFor="name-nl">
            Name <RequiredMark />
          </label>
          <input
            id="name-nl"
            type="text"
            {...register('translations.nl.name')}
            style={inputStyle(!!nlNameError)}
            placeholder="Combo name in NL"
          />
          {nlNameError && <FieldError message={nlNameError} />}
        </div>
        <div style={{ marginBottom: '0.5rem' }}>
          <label style={labelStyle} htmlFor="desc-nl">
            Description <span style={{ color: '#9ca3af', fontWeight: 400 }}>(optional)</span>
          </label>
          <textarea
            id="desc-nl"
            {...register('translations.nl.description')}
            rows={3}
            style={{ ...inputStyle(false), resize: 'vertical' }}
            placeholder="Combo description in NL"
          />
        </div>
      </>
    );
  }
  if (activeTab === 'fr') {
    return (
      <>
        <div style={{ marginBottom: '1rem' }}>
          <label style={labelStyle} htmlFor="name-fr">
            Name
          </label>
          <input
            id="name-fr"
            type="text"
            {...register('translations.fr.name')}
            style={inputStyle(false)}
            placeholder="Combo name in FR"
          />
        </div>
        <div style={{ marginBottom: '0.5rem' }}>
          <label style={labelStyle} htmlFor="desc-fr">
            Description <span style={{ color: '#9ca3af', fontWeight: 400 }}>(optional)</span>
          </label>
          <textarea
            id="desc-fr"
            {...register('translations.fr.description')}
            rows={3}
            style={{ ...inputStyle(false), resize: 'vertical' }}
            placeholder="Combo description in FR"
          />
        </div>
      </>
    );
  }
  return (
    <>
      <div style={{ marginBottom: '1rem' }}>
        <label style={labelStyle} htmlFor="name-de">
          Name
        </label>
        <input
          id="name-de"
          type="text"
          {...register('translations.de.name')}
          style={inputStyle(false)}
          placeholder="Combo name in DE"
        />
      </div>
      <div style={{ marginBottom: '0.5rem' }}>
        <label style={labelStyle} htmlFor="desc-de">
          Description <span style={{ color: '#9ca3af', fontWeight: 400 }}>(optional)</span>
        </label>
        <textarea
          id="desc-de"
          {...register('translations.de.description')}
          rows={3}
          style={{ ...inputStyle(false), resize: 'vertical' }}
          placeholder="Combo description in DE"
        />
      </div>
    </>
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

