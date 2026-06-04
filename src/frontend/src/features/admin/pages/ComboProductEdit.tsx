import { useState, useEffect } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { Controller } from 'react-hook-form';
import { useProducts, useDeleteProduct } from '../hooks/useProducts';
import {
  useProductModifierGroups,
  useModifierGroups,
  useSetProductModifierGroups,
} from '../hooks/useModifierGroups';
import { useResourceForm } from '../forms/useResourceForm';
import { productKeys } from '../hooks/useProducts';
import { productsApi } from '../../../api/products';
import { comboProductEditSchema, type ComboProductEditFormValues } from './schemas/comboProductEditSchema';
import type {
  SupportedLocale,
  ProductListItem,
  ProductModifierGroupResponse,
  Product,
} from '../../../types/common';
import type { UpdateComboProductRequest } from '../../../api/products';
import { labelStyle, inputStyle, secondaryButtonStyle, RequiredMark, FieldError } from '../forms/adminFormStyles';
import { ComboTranslationFields } from '../forms/ComboTranslationFields';

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
): ComboProductEditFormValues['translations'] {
  const map: ComboProductEditFormValues['translations'] = {
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

export function ComboProductEdit() {
  const navigate = useNavigate();
  const { t } = useTranslation();
  const { brandSlug, lang, productId } = useParams<{
    brandSlug: string;
    lang: string;
    productId: string;
  }>();

  const resolvedBrandSlug = brandSlug ?? '';
  const resolvedProductId = productId ?? '';

  const { data: allProducts } = useProducts(resolvedBrandSlug);
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
  // ---------------------------------------------------------------------------

  const { form, submit, isSubmitting, isFetching, fetchError, submitError } = useResourceForm<
    Product,
    ComboProductEditFormValues,
    UpdateComboProductRequest
  >({
    queryKey: productKeys.detail(resolvedBrandSlug, resolvedProductId),
    fetch: () => productsApi.get(resolvedBrandSlug, resolvedProductId),
    update: (payload) => productsApi.updateCombo(resolvedBrandSlug, resolvedProductId, payload),
    schema: comboProductEditSchema,
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
    toFormValues: (product) => ({
      basePrice: product.basePrice.amount,
      imageUrl: product.imageUrl ?? '',
      translations: buildTranslationsMap(product.translations),
      componentProductIds: (product.comboItems ?? [])
        .slice()
        .sort((a, b) => a.sortOrder - b.sortOrder)
        .map((c) => c.componentProductId),
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
      componentProductIds: values.componentProductIds,
    }),
    invalidate: [productKeys.all(resolvedBrandSlug)],
    onSuccess: () => { navigate(`/${String(brandSlug)}/${String(lang)}/admin/products`); },
  });

  // ---------------------------------------------------------------------------
  // Derived data
  // ---------------------------------------------------------------------------

  const simpleProducts = allProducts?.filter((p) => p.productType === 'Simple') ?? [];

  // ---------------------------------------------------------------------------
  // Handlers — combo component list (Controller-based, not useFieldArray)
  // ---------------------------------------------------------------------------

  function handleToggleComponent(product: ProductListItem, currentIds: string[], onChange: (ids: string[]) => void) {
    const exists = currentIds.includes(product.id);
    if (exists) {
      onChange(currentIds.filter((id) => id !== product.id));
    } else {
      onChange([...currentIds, product.id]);
    }
  }

  function handleMoveUp(index: number, currentIds: string[], onChange: (ids: string[]) => void) {
    if (index === 0) return;
    const next = [...currentIds];
    // eslint-disable-next-line @typescript-eslint/no-non-null-assertion -- index is in-bounds (index > 0) and next is a copy of currentIds
    [next[index - 1], next[index]] = [next[index]!, next[index - 1]!];
    onChange(next);
  }

  function handleMoveDown(index: number, currentIds: string[], onChange: (ids: string[]) => void) {
    if (index >= currentIds.length - 1) return;
    const next = [...currentIds];
    // eslint-disable-next-line @typescript-eslint/no-non-null-assertion -- index and index+1 are in-bounds (index < length-1) and next is a copy of currentIds
    [next[index], next[index + 1]] = [next[index + 1]!, next[index]!];
    onChange(next);
  }

  // ---------------------------------------------------------------------------
  // Delete handler
  // ---------------------------------------------------------------------------

  function handleDelete() {
    const nlName = form.getValues('translations.nl.name') || '(unnamed)';
    if (window.confirm(t('admin.comboProducts.confirmDelete', { name: nlName }))) {
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
      // eslint-disable-next-line @typescript-eslint/no-non-null-assertion -- index is in-bounds (index > 0) and next is a copy of prev
      [next[index - 1], next[index]] = [next[index]!, next[index - 1]!];
      return next.map((g, i) => ({ ...g, sortOrder: i }));
    });
  }

  function handleMoveGroupDown(index: number) {
    setAssignedGroups((prev) => {
      if (index >= prev.length - 1) return prev;
      const next = [...prev];
      // eslint-disable-next-line @typescript-eslint/no-non-null-assertion -- index and index+1 are in-bounds (index < length-1) and next is a copy of prev
      [next[index], next[index + 1]] = [next[index + 1]!, next[index]!];
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
  // Loading / error states
  // ---------------------------------------------------------------------------

  if (isFetching) {
    return (
      <main style={{ padding: '1.5rem' }}>
        <p style={{ color: '#6b7280' }}>Loading combo product...</p>
      </main>
    );
  }

  if (fetchError) {
    return (
      <main style={{ padding: '1.5rem' }}>
        <p style={{ color: '#dc2626' }}>
          Failed to load combo product:{' '}
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
  const componentIdsError = (errors.componentProductIds as { message?: string } | undefined)?.message;

  return (
    <main style={{ padding: '1.5rem', maxWidth: '40rem' }}>
      <h1 style={{ fontSize: '1.5rem', fontWeight: 700, marginBottom: '1.5rem' }}>
        {t('admin.comboProducts.edit')}
      </h1>

      <form onSubmit={(e) => { e.preventDefault(); void submit(); }} noValidate>
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
        <ComboTranslationFields
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

          <Controller
            name="componentProductIds"
            control={form.control}
            render={({ field }) => {
              // eslint-disable-next-line @typescript-eslint/no-unnecessary-condition -- RHF Controller field.value can be undefined at runtime before the form resets to fetched values, despite the non-nullable schema type
              const currentIds: string[] = field.value ?? [];
              const selectedProducts = currentIds
                .map((id) => allProducts?.find((p) => p.id === id))
                .filter((p): p is ProductListItem => p !== undefined);

              return (
                <>
                  {/* Selected components with reorder controls */}
                  {selectedProducts.length > 0 && (
                    <div style={{ marginBottom: '0.75rem' }}>
                      {selectedProducts.map((comp, index) => (
                        <div
                          key={comp.id}
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
                            {comp.name}
                          </span>
                          <span
                            style={{ fontSize: '0.75rem', color: '#6b7280', fontFamily: 'monospace' }}
                          >
                            {'€'} {comp.basePrice.amount.toFixed(2)}
                          </span>
                          <button
                            type="button"
                            onClick={() => { handleMoveUp(index, currentIds, field.onChange); }}
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
                            onClick={() => { handleMoveDown(index, currentIds, field.onChange); }}
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
                            onClick={() => { handleToggleComponent(comp, currentIds, field.onChange); }}
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
                        .filter((p) => !currentIds.includes(p.id))
                        .map((p) => (
                          <div
                            key={p.id}
                            onClick={() => { handleToggleComponent(p, currentIds, field.onChange); }}
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
                            <span style={{ flex: 1, fontSize: '0.875rem' }}>{p.name}</span>
                            <span
                              style={{
                                fontSize: '0.75rem',
                                color: '#6b7280',
                                fontFamily: 'monospace',
                              }}
                            >
                              {'€'} {p.basePrice.amount.toFixed(2)}
                            </span>
                            <span style={{ color: '#9ca3af', fontSize: '0.875rem' }}>+ Add</span>
                          </div>
                        ))}
                      {simpleProducts.filter((p) => !currentIds.includes(p.id)).length === 0 && (
                        <p style={{ padding: '0.75rem', color: '#9ca3af', fontSize: '0.875rem' }}>
                          {t('admin.comboProducts.allProductsSelected')}
                        </p>
                      )}
                    </div>
                  )}
                </>
              );
            }}
          />
        </section>

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
            {isSubmitting ? t('admin.comboProducts.saving') : t('admin.comboProducts.saveChanges')}
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
            {deleteProduct.isPending ? t('admin.comboProducts.deleting') : t('admin.comboProducts.delete')}
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
            {assignedGroups.map((group, index) => (
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
                  <span style={{ fontWeight: 600, fontSize: '0.9rem' }}>{group.name}</span>
                  {group.modifiers.length > 0 && (
                    <span style={{ color: '#6b7280', fontSize: '0.75rem', marginLeft: '0.5rem' }}>
                      {group.modifiers.length} {t('admin.modifierGroups.modifiers').toLowerCase()}
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
            ))}
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
              <option value="">{t('admin.comboProducts.selectModifierGroup')}</option>
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
  );
}

// ---------------------------------------------------------------------------
// Local style helpers not covered by adminFormStyles
// ---------------------------------------------------------------------------

const reorderButtonStyle: React.CSSProperties = {
  padding: '0.125rem 0.4rem',
  fontSize: '0.75rem',
  background: '#fff',
  border: '1px solid #d1d5db',
  borderRadius: '0.25rem',
  lineHeight: 1,
};
