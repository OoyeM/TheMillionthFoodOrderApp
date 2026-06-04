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
import { labelStyle, inputStyle, RequiredMark, FieldError, FormError } from '../forms/adminFormStyles';
import { FormActions } from '../forms/FormActions';
import { ComboTranslationFields } from '../forms/ComboTranslationFields';
import { SelectedComponentsList } from '../forms/SelectedComponentsList';
import { ComponentProductPicker } from '../forms/ComponentProductPicker';

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

  const onSubmit = handleSubmit((values) => { void mutation.mutateAsync(toCreatePayload(values)); });

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
    // eslint-disable-next-line @typescript-eslint/no-non-null-assertion -- index is in-bounds (index > 0) and next is a copy of currentComponentIds
    [next[index - 1], next[index]] = [next[index]!, next[index - 1]!];
    setValue('componentProductIds', next);
  }

  function handleMoveDown(index: number) {
    if (index >= currentComponentIds.length - 1) return;
    const next = [...currentComponentIds];
    // eslint-disable-next-line @typescript-eslint/no-non-null-assertion -- index and index+1 are in-bounds (index < length-1) and next is a copy of currentComponentIds
    [next[index], next[index + 1]] = [next[index + 1]!, next[index]!];
    setValue('componentProductIds', next);
  }

  const watchedImageUrl = watch('imageUrl');
  const nlNameError = (errors.translations?.nl?.name as { message?: string } | undefined)?.message;
  const componentIdsError = (errors.componentProductIds as { message?: string } | undefined)?.message;
  const selectedProducts = currentComponentIds
    .map((id) => allProducts?.find((p) => p.id === id))
    .filter((p): p is ProductListItem => p !== undefined);

  return (
    <main style={{ padding: '1.5rem', maxWidth: '40rem' }}>
      <h1 style={{ fontSize: '1.5rem', fontWeight: 700, marginBottom: '1.5rem' }}>
        {t('admin.comboProducts.create')}
      </h1>
      <form onSubmit={(e) => { e.preventDefault(); void onSubmit(); }} noValidate>
        <BasePriceField register={register} error={errors.basePrice?.message} bundlePriceLabel={t('admin.comboProducts.bundlePrice')} />
        <ImageUrlField register={register} watchedImageUrl={watchedImageUrl} />
        <p style={{ fontWeight: 600, fontSize: '0.875rem', marginBottom: '0.5rem' }}>
          {t('admin.comboProducts.translations')} <RequiredMark />
        </p>
        <LanguageTabBar activeTab={activeTab} onTabChange={setActiveTab} />
        <ComboTranslationFields activeTab={activeTab} register={register} nlNameError={nlNameError} />
        <ComponentProductsSection
          simpleProducts={simpleProducts}
          selectedProducts={selectedProducts}
          currentComponentIds={currentComponentIds}
          componentIdsError={componentIdsError}
          onMoveUp={handleMoveUp}
          onMoveDown={handleMoveDown}
          onToggle={handleToggleComponent}
        />
        <FormError error={mutation.error} fallback="Failed to create combo product. Please try again." />
        <FormActions
          isPending={mutation.isPending}
          isSubmitting={isSubmitting}
          onCancel={handleCancel}
          submitLabel={t('admin.comboProducts.createButton')}
          pendingLabel={t('admin.comboProducts.creating')}
        />
      </form>
    </main>
  );
}

// ---------------------------------------------------------------------------
// Module-level sub-components (same file — no new dependencies)
// ---------------------------------------------------------------------------

interface BasePriceFieldProps {
  register: UseFormRegister<ComboProductEditFormValues>;
  error: string | undefined;
  bundlePriceLabel: string;
}

function BasePriceField({ register, error, bundlePriceLabel }: BasePriceFieldProps) {
  return (
    <div style={{ marginBottom: '1rem' }}>
      <label style={labelStyle} htmlFor="basePrice">
        {bundlePriceLabel} (EUR) <RequiredMark />
      </label>
      <input
        id="basePrice"
        type="number"
        min="0.01"
        step="0.01"
        {...register('basePrice', { valueAsNumber: true })}
        style={inputStyle(!!error)}
        placeholder="e.g. 3.50"
      />
      {error && <FieldError message={error} />}
    </div>
  );
}

interface ImageUrlFieldProps {
  register: UseFormRegister<ComboProductEditFormValues>;
  watchedImageUrl: string;
}

function ImageUrlField({ register, watchedImageUrl }: ImageUrlFieldProps) {
  return (
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
          style={{ marginTop: '0.5rem', maxWidth: 120, maxHeight: 120, objectFit: 'cover', borderRadius: '0.25rem' }}
          onError={(e) => { (e.target as HTMLImageElement).style.display = 'none'; }}
        />
      ) : null}
    </div>
  );
}

interface LanguageTabBarProps {
  activeTab: SupportedLocale;
  onTabChange: (tab: SupportedLocale) => void;
}

function LanguageTabBar({ activeTab, onTabChange }: LanguageTabBarProps) {
  return (
    <div style={{ display: 'flex', marginBottom: '1rem', borderBottom: '2px solid #e5e7eb' }}>
      {LANGUAGES.map((l) => (
        <button
          key={l.code}
          type="button"
          onClick={() => { onTabChange(l.code); }}
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
  );
}

interface ComponentProductsSectionProps {
  simpleProducts: ProductListItem[];
  selectedProducts: ProductListItem[];
  currentComponentIds: string[];
  componentIdsError: string | undefined;
  onMoveUp: (index: number) => void;
  onMoveDown: (index: number) => void;
  onToggle: (product: ProductListItem) => void;
}

function ComponentProductsSection({
  simpleProducts,
  selectedProducts,
  currentComponentIds,
  componentIdsError,
  onMoveUp,
  onMoveDown,
  onToggle,
}: ComponentProductsSectionProps) {
  const { t } = useTranslation();
  return (
    <section style={{ marginBottom: '1.5rem' }}>
      <p style={{ fontWeight: 600, fontSize: '0.875rem', marginBottom: '0.5rem' }}>
        {t('admin.comboProducts.componentProducts')} <RequiredMark />
      </p>
      <p style={{ fontSize: '0.75rem', color: '#6b7280', marginBottom: '0.75rem' }}>
        {t('admin.comboProducts.componentProductsHint')}
      </p>
      {componentIdsError && <FieldError message={componentIdsError} />}
      <SelectedComponentsList
        selectedProducts={selectedProducts}
        onMoveUp={onMoveUp}
        onMoveDown={onMoveDown}
        onRemove={onToggle}
      />
      {simpleProducts.length === 0 && selectedProducts.length === 0 && (
        <p style={{ color: '#6b7280', fontSize: '0.875rem', marginBottom: '0.75rem' }}>
          {t('admin.comboProducts.noSimpleProducts')}
        </p>
      )}
      <ComponentProductPicker simpleProducts={simpleProducts} selectedIds={currentComponentIds} onAdd={onToggle} />
    </section>
  );
}
