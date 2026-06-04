import { useState, useEffect } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { Controller, type UseFormRegister } from 'react-hook-form';
import type { TFunction } from 'i18next';
import { useDeleteProduct, productKeys } from '../hooks/useProducts';
import {
  useProductModifierGroups,
  useModifierGroups,
  useSetProductModifierGroups,
} from '../hooks/useModifierGroups';
import { useResourceForm } from '../forms/useResourceForm';
import { productsApi } from '../../../api/products';
import type { UpdateProductRequest } from '../../../api/products';
import { productEditSchema, type ProductEditFormValues } from './schemas/productEditSchema';
import {
  Allergen,
  DietaryTag,
  ALLERGEN_KEYS,
  DIETARY_TAG_KEYS,
} from '../../../types/common';
import type { SupportedLocale, ProductModifierGroupResponse, Product } from '../../../types/common';
import { labelStyle, inputStyle, secondaryButtonStyle, RequiredMark, FieldError } from '../forms/adminFormStyles';
import { ResourceFormShell } from '../forms/ResourceFormShell';

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

  // Modifier groups (kept imperative — separate resource / mutation)
  const { data: productModifierGroups } = useProductModifierGroups(resolvedBrandSlug, resolvedProductId);
  const { data: allModifierGroups } = useModifierGroups(resolvedBrandSlug);
  const setProductModifierGroups = useSetProductModifierGroups(resolvedBrandSlug, resolvedProductId);
  const [assignedGroups, setAssignedGroups] = useState<ProductModifierGroupResponse[]>([]);
  const [assignmentsInitialized, setAssignmentsInitialized] = useState(false);
  const [selectedGroupToAdd, setSelectedGroupToAdd] = useState('');

  // Populate assigned modifier groups when data arrives
  useEffect(() => {
    if (productModifierGroups !== undefined && !assignmentsInitialized) {
      setAssignedGroups(productModifierGroups);
      setAssignmentsInitialized(true);
    }
  }, [productModifierGroups, assignmentsInitialized]);

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
  // Modifier group assignment handlers (imperative — separate resource)
  // ---------------------------------------------------------------------------

  function handleAddModifierGroup() {
    if (!selectedGroupToAdd) return;
    const alreadyAssigned = assignedGroups.some((g) => g.modifierGroupId === selectedGroupToAdd);
    if (alreadyAssigned) return;

    const groupInfo = allModifierGroups?.find((g) => g.id === selectedGroupToAdd);
    if (!groupInfo) return;

    const newGroup: ProductModifierGroupResponse = {
      modifierGroupId: groupInfo.id,
      name: groupInfo.name,
      sortOrder: assignedGroups.length,
      modifiers: [],
    };
    setAssignedGroups((prev) => [...prev, newGroup]);
    setSelectedGroupToAdd('');
  }

  function handleRemoveAssignedGroup(modifierGroupId: string) {
    setAssignedGroups((prev) =>
      prev
        .filter((g) => g.modifierGroupId !== modifierGroupId)
        .map((g, index) => ({ ...g, sortOrder: index })),
    );
  }

  function handleMoveGroupUp(index: number) {
    if (index === 0) return;
    setAssignedGroups((prev) => {
      const next = [...prev];
      const current = next[index];
      const above = next[index - 1];
      if (current === undefined || above === undefined) return prev;
      next[index - 1] = current;
      next[index] = above;
      return next.map((g, i) => ({ ...g, sortOrder: i }));
    });
  }

  function handleMoveGroupDown(index: number) {
    setAssignedGroups((prev) => {
      if (index >= prev.length - 1) return prev;
      const next = [...prev];
      const current = next[index];
      const below = next[index + 1];
      if (current === undefined || below === undefined) return prev;
      next[index] = below;
      next[index + 1] = current;
      return next.map((g, i) => ({ ...g, sortOrder: i }));
    });
  }

  function handleSaveAssignments() {
    setProductModifierGroups.mutate({
      assignments: assignedGroups.map((g) => ({
        modifierGroupId: g.modifierGroupId,
        sortOrder: g.sortOrder,
      })),
    });
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
                  // eslint-disable-next-line @typescript-eslint/no-unnecessary-condition -- field.value is react-hook-form Controller state that can be undefined before defaults apply
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
                            // eslint-disable-next-line @typescript-eslint/no-unnecessary-condition -- field.value is react-hook-form Controller state that can be undefined before defaults apply
                            field.onChange((field.value ?? []).filter((v) => v !== val));
                          } else {
                            // eslint-disable-next-line @typescript-eslint/no-unnecessary-condition -- field.value is react-hook-form Controller state that can be undefined before defaults apply
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
                  // eslint-disable-next-line @typescript-eslint/no-unnecessary-condition -- field.value is react-hook-form Controller state that can be undefined before defaults apply
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
                            // eslint-disable-next-line @typescript-eslint/no-unnecessary-condition -- field.value is react-hook-form Controller state that can be undefined before defaults apply
                            field.onChange((field.value ?? []).filter((v) => v !== val));
                          } else {
                            // eslint-disable-next-line @typescript-eslint/no-unnecessary-condition -- field.value is react-hook-form Controller state that can be undefined before defaults apply
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

      {/* Modifier Groups Section */}
      <section
        style={{
          marginTop: '2.5rem',
          borderTop: '2px solid #e5e7eb',
          paddingTop: '1.5rem',
        }}
      >
        <h2 style={{ fontSize: '1.125rem', fontWeight: 700, marginBottom: '1rem' }}>
          {t('admin.modifierGroups.title')}
        </h2>

        {/* Assigned groups list */}
        {assignedGroups.length === 0 ? (
          <p style={{ color: '#6b7280', marginBottom: '1rem' }}>
            {t('admin.modifierGroups.noAssignedGroups')}
          </p>
        ) : (
          <div style={{ marginBottom: '1rem' }}>
            {assignedGroups.map((group, index) => {
              // The assignment endpoint returns only { modifierGroupId, sortOrder };
              // resolve the display name + modifier count from the full group list.
              const info = allModifierGroups?.find((g) => g.id === group.modifierGroupId);
              // eslint-disable-next-line @typescript-eslint/no-unnecessary-condition -- group is API/JSON data; name may be absent at runtime despite the type
              const groupName = info?.name ?? group.name ?? '';
              // eslint-disable-next-line @typescript-eslint/no-unnecessary-condition -- group is API/JSON data; modifiers may be absent at runtime despite the type
              const modifierCount = info?.modifierCount ?? group.modifiers?.length ?? 0;
              return (
              <div
                key={group.modifierGroupId}
                style={{
                  display: 'flex',
                  alignItems: 'center',
                  gap: '0.5rem',
                  padding: '0.625rem 0.75rem',
                  border: '1px solid #e5e7eb',
                  borderRadius: '0.375rem',
                  marginBottom: '0.5rem',
                  background: '#f9fafb',
                }}
              >
                <div style={{ flex: 1 }}>
                  <span style={{ fontWeight: 600, fontSize: '0.9rem' }}>{groupName}</span>
                  {modifierCount > 0 && (
                    <span style={{ color: '#6b7280', fontSize: '0.75rem', marginLeft: '0.5rem' }}>
                      {modifierCount} {t('admin.modifierGroups.modifiers').toLowerCase()}
                    </span>
                  )}
                </div>
                <button
                  type="button"
                  onClick={() => { handleMoveGroupUp(index); }}
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
                  onClick={() => { handleMoveGroupDown(index); }}
                  disabled={index === assignedGroups.length - 1}
                  style={{
                    ...reorderButtonStyle,
                    opacity: index === assignedGroups.length - 1 ? 0.3 : 1,
                    cursor: index === assignedGroups.length - 1 ? 'not-allowed' : 'pointer',
                  }}
                >
                  &#9660;
                </button>
                <button
                  type="button"
                  onClick={() => { handleRemoveAssignedGroup(group.modifierGroupId); }}
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
                  {t('admin.modifierGroups.removeModifier')}
                </button>
              </div>
              );
            })}
          </div>
        )}

        {/* Add group dropdown */}
        {allModifierGroups !== undefined && allModifierGroups.length > 0 && (
          <div style={{ display: 'flex', gap: '0.5rem', marginBottom: '1rem', alignItems: 'center' }}>
            <select
              value={selectedGroupToAdd}
              onChange={(e) => { setSelectedGroupToAdd(e.target.value); }}
              style={{
                padding: '0.5rem 0.75rem',
                border: '1px solid #d1d5db',
                borderRadius: '0.375rem',
                fontSize: '0.9rem',
                flex: 1,
                maxWidth: '20rem',
              }}
            >
              <option value="">-- Select a modifier group --</option>
              {allModifierGroups
                .filter((g) => !assignedGroups.some((a) => a.modifierGroupId === g.id))
                .map((g) => (
                  <option key={g.id} value={g.id}>
                    {g.name}
                  </option>
                ))}
            </select>
            <button
              type="button"
              onClick={handleAddModifierGroup}
              disabled={!selectedGroupToAdd}
              style={{
                padding: '0.5rem 1rem',
                background: '#f9fafb',
                border: '1px solid #d1d5db',
                borderRadius: '0.375rem',
                cursor: selectedGroupToAdd ? 'pointer' : 'not-allowed',
                opacity: selectedGroupToAdd ? 1 : 0.5,
                fontSize: '0.875rem',
              }}
            >
              + Add
            </button>
          </div>
        )}

        {/* Save assignments */}
        {setProductModifierGroups.isError && (
          <p style={{ color: '#dc2626', marginBottom: '0.75rem', fontSize: '0.875rem' }}>
            {setProductModifierGroups.error instanceof Error
              ? setProductModifierGroups.error.message
              : 'Failed to save assignments. Please try again.'}
          </p>
        )}

        {setProductModifierGroups.isSuccess && (
          <p style={{ color: '#16a34a', marginBottom: '0.75rem', fontSize: '0.875rem' }}>
            Assignments saved.
          </p>
        )}

        <button
          type="button"
          onClick={handleSaveAssignments}
          disabled={setProductModifierGroups.isPending}
          style={{
            padding: '0.5rem 1.25rem',
            background: '#111827',
            color: '#fff',
            border: 'none',
            borderRadius: '0.375rem',
            cursor: setProductModifierGroups.isPending ? 'not-allowed' : 'pointer',
            fontWeight: 600,
            opacity: setProductModifierGroups.isPending ? 0.6 : 1,
          }}
        >
          {setProductModifierGroups.isPending ? 'Saving...' : t('admin.modifierGroups.saveAssignments')}
        </button>
      </section>
    </main>
    </ResourceFormShell>
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

// ---------------------------------------------------------------------------
// Local style helpers (reorderButtonStyle is unique to this page)
// ---------------------------------------------------------------------------

const reorderButtonStyle: React.CSSProperties = {
  padding: '0.125rem 0.4rem',
  fontSize: '0.75rem',
  background: '#fff',
  border: '1px solid #d1d5db',
  borderRadius: '0.25rem',
  lineHeight: 1,
};

