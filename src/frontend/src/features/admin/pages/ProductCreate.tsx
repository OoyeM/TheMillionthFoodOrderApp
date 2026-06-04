import { useState } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { useForm, Controller, type UseFormRegister } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import type { TFunction } from 'i18next';
import { productsApi } from '@api/products';
import type { CreateProductRequest } from '@api/products';
import { productEditSchema, type ProductEditFormValues } from './schemas/productEditSchema';
import { productKeys } from '../hooks/useProducts';
import {
  Allergen,
  DietaryTag,
  ALLERGEN_KEYS,
  DIETARY_TAG_KEYS,
} from '../../../types/common';
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

// ---------------------------------------------------------------------------
// Payload mapper
// ---------------------------------------------------------------------------

function toCreatePayload(values: ProductEditFormValues): CreateProductRequest {
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
    allergens: values.allergens,
    dietaryTags: values.dietaryTags,
  };
}

// ---------------------------------------------------------------------------
// Page component
// ---------------------------------------------------------------------------

export function ProductCreate() {
  const navigate = useNavigate();
  const { t } = useTranslation();
  const queryClient = useQueryClient();
  const { brandSlug = '', lang = '' } = useParams<{ brandSlug: string; lang: string }>();

  const [activeTab, setActiveTab] = useState<SupportedLocale>('nl');

  const form = useForm<ProductEditFormValues>({
    resolver: zodResolver(productEditSchema),
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
  });

  const {
    register,
    handleSubmit,
    watch,
    control,
    formState: { errors, isSubmitting },
  } = form;

  const mutation = useMutation({
    mutationFn: (payload: CreateProductRequest) => productsApi.create(brandSlug, payload),
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

  const watchedImageUrl = watch('imageUrl');
  // Pre-compute error message to avoid complex type inference inside JSX
  const nlNameError = (errors.translations?.nl?.name as { message?: string } | undefined)?.message;

  return (
    <main style={{ padding: '1.5rem', maxWidth: '40rem' }}>
      <h1 style={{ fontSize: '1.5rem', fontWeight: 700, marginBottom: '1.5rem' }}>
        {t('admin.products.create')}
      </h1>

      <form onSubmit={(e) => { e.preventDefault(); void onSubmit(); }} noValidate>
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

        {/* Allergens */}
        <div style={{ marginBottom: '1.5rem' }}>
          <p style={{ fontWeight: 600, fontSize: '0.875rem', marginBottom: '0.5rem' }}>
            {t('admin.products.allergens')}
          </p>
          <Controller
            name="allergens"
            control={control}
            render={({ field }) => (
              <div style={{ display: 'flex', flexWrap: 'wrap', gap: '0.5rem' }}>
                {ALLERGEN_KEYS.map((key) => {
                  const val = Allergen[key];
                  const checked = (field.value ?? []).includes(val);
                  return (
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
                        checked={checked}
                        onChange={() => {
                          if (checked) {
                            field.onChange((field.value ?? []).filter((v) => v !== val));
                          } else {
                            field.onChange([...(field.value ?? []), val]);
                          }
                        }}
                      />
                      {t(`allergens.${key}`)}
                    </label>
                  );
                })}
              </div>
            )}
          />
        </div>

        {/* Dietary Tags */}
        <div style={{ marginBottom: '1.5rem' }}>
          <p style={{ fontWeight: 600, fontSize: '0.875rem', marginBottom: '0.5rem' }}>
            {t('admin.products.dietaryTags')}
          </p>
          <Controller
            name="dietaryTags"
            control={control}
            render={({ field }) => (
              <div style={{ display: 'flex', flexWrap: 'wrap', gap: '0.5rem' }}>
                {DIETARY_TAG_KEYS.map((key) => {
                  const val = DietaryTag[key];
                  const checked = (field.value ?? []).includes(val);
                  return (
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
                        checked={checked}
                        onChange={() => {
                          if (checked) {
                            field.onChange((field.value ?? []).filter((v) => v !== val));
                          } else {
                            field.onChange([...(field.value ?? []), val]);
                          }
                        }}
                      />
                      {t(`dietaryTags.${key}`)}
                    </label>
                  );
                })}
              </div>
            )}
          />
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
        <TranslationFields
          activeTab={activeTab}
          register={register}
          nlNameError={nlNameError}
          t={t}
        />

        {/* API error */}
        {mutation.error != null && (
          <p style={{ color: '#dc2626', marginBottom: '1rem', fontSize: '0.875rem' }}>
            {mutation.error instanceof Error
              ? mutation.error.message
              : t('admin.products.createError')}
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
            {mutation.isPending ? t('admin.products.creating') : t('admin.products.create')}
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
// TranslationFields — extracted so `register` call uses literal path strings,
// which TypeScript resolves correctly (dynamic template-literal paths produce
// unknown spreads in strict TSX).
// ---------------------------------------------------------------------------

interface TranslationFieldsProps {
  activeTab: SupportedLocale;
  register: UseFormRegister<ProductEditFormValues>;
  nlNameError: string | undefined;
  t: TFunction;
}

function TranslationFields({ activeTab, register, nlNameError, t }: TranslationFieldsProps) {
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
            placeholder={t('admin.products.namePlaceholder', { lang: 'NL' })}
          />
          {nlNameError && <FieldError message={nlNameError} />}
        </div>
        <div style={{ marginBottom: '1.5rem' }}>
          <label style={labelStyle} htmlFor="desc-nl">
            {t('admin.products.description')}{' '}
            <span style={{ color: '#9ca3af', fontWeight: 400 }}>{t('admin.products.optional')}</span>
          </label>
          <textarea
            id="desc-nl"
            {...register('translations.nl.description')}
            rows={3}
            style={{ ...inputStyle(false), resize: 'vertical' }}
            placeholder={t('admin.products.descriptionPlaceholder', { lang: 'NL' })}
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
            placeholder={t('admin.products.namePlaceholder', { lang: 'FR' })}
          />
        </div>
        <div style={{ marginBottom: '1.5rem' }}>
          <label style={labelStyle} htmlFor="desc-fr">
            {t('admin.products.description')}{' '}
            <span style={{ color: '#9ca3af', fontWeight: 400 }}>{t('admin.products.optional')}</span>
          </label>
          <textarea
            id="desc-fr"
            {...register('translations.fr.description')}
            rows={3}
            style={{ ...inputStyle(false), resize: 'vertical' }}
            placeholder={t('admin.products.descriptionPlaceholder', { lang: 'FR' })}
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
          placeholder={t('admin.products.namePlaceholder', { lang: 'DE' })}
        />
      </div>
      <div style={{ marginBottom: '1.5rem' }}>
        <label style={labelStyle} htmlFor="desc-de">
          {t('admin.products.description')}{' '}
          <span style={{ color: '#9ca3af', fontWeight: 400 }}>{t('admin.products.optional')}</span>
        </label>
        <textarea
          id="desc-de"
          {...register('translations.de.description')}
          rows={3}
          style={{ ...inputStyle(false), resize: 'vertical' }}
          placeholder={t('admin.products.descriptionPlaceholder', { lang: 'DE' })}
        />
      </div>
    </>
  );
}

