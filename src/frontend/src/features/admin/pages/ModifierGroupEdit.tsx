import { useState } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { useFieldArray, type UseFormRegister } from 'react-hook-form';
import { useDeleteModifierGroup, modifierGroupKeys } from '../hooks/useModifierGroups';
import { useResourceForm } from '../forms/useResourceForm';
import { modifierGroupsApi } from '@api/modifierGroups';
import { modifierGroupEditSchema, type ModifierGroupEditFormValues } from './schemas/modifierGroupEditSchema';
import type { SupportedLocale, ModifierGroupResponse } from '../../../types/common';
import type { UpdateModifierGroupRequest } from '@api/modifierGroups';
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
  apiTranslations: { languageCode: string; name: string }[],
): { nl: { name: string }; fr: { name: string }; de: { name: string } } {
  const map = { nl: { name: '' }, fr: { name: '' }, de: { name: '' } };
  for (const t of apiTranslations) {
    if (t.languageCode === 'nl' || t.languageCode === 'fr' || t.languageCode === 'de') {
      map[t.languageCode].name = t.name;
    }
  }
  return map;
}

// ---------------------------------------------------------------------------
// Page component
// ---------------------------------------------------------------------------

export function ModifierGroupEdit() {
  const navigate = useNavigate();
  const { t } = useTranslation();
  const { brandSlug, lang, modifierGroupId } = useParams<{
    brandSlug: string;
    lang: string;
    modifierGroupId: string;
  }>();

  const resolvedBrandSlug = brandSlug ?? '';
  const resolvedId = modifierGroupId ?? '';

  const deleteModifierGroup = useDeleteModifierGroup(resolvedBrandSlug);

  // Active translation tab (UI-only state — not part of the form schema)
  const [activeTab, setActiveTab] = useState<SupportedLocale>('nl');

  // ---------------------------------------------------------------------------
  // Main form via useResourceForm
  // NL is hard-coded as the required locale (matches ProductEdit / ComboProductEdit).
  // ---------------------------------------------------------------------------

  const { form, submit, isSubmitting, isFetching, fetchError, submitError } = useResourceForm<
    ModifierGroupResponse,
    ModifierGroupEditFormValues,
    UpdateModifierGroupRequest
  >({
    queryKey: modifierGroupKeys.detail(resolvedBrandSlug, resolvedId),
    fetch: () => modifierGroupsApi.get(resolvedBrandSlug, resolvedId),
    update: (payload) => modifierGroupsApi.update(resolvedBrandSlug, resolvedId, payload),
    schema: modifierGroupEditSchema,
    defaultValues: {
      translations: { nl: { name: '' }, fr: { name: '' }, de: { name: '' } },
      modifiers: [
        {
          translations: { nl: { name: '' }, fr: { name: '' }, de: { name: '' } },
          priceAdjustment: 0,
        },
      ],
    },
    toFormValues: (group) => ({
      translations: buildTranslationsMap(group.translations),
      modifiers: group.modifiers
        .sort((a, b) => a.sortOrder - b.sortOrder)
        .map((m) => ({
          id: m.id,
          translations: buildTranslationsMap(m.translations),
          priceAdjustment: m.priceAdjustment,
        })),
    }),
    toUpdatePayload: (values) => ({
      translations: (['nl', 'fr', 'de'] as const)
        .filter((loc) => values.translations[loc].name.trim().length > 0)
        .map((loc) => ({ languageCode: loc, name: values.translations[loc].name.trim() })),
      modifiers: values.modifiers.map((m, index) => ({
        translations: (['nl', 'fr', 'de'] as const)
          .filter((loc) => m.translations[loc].name.trim().length > 0)
          .map((loc) => ({ languageCode: loc, name: m.translations[loc].name.trim() })),
        priceAdjustment: m.priceAdjustment,
        sortOrder: index,
      })),
    }),
    invalidate: [
      modifierGroupKeys.all(resolvedBrandSlug),
      modifierGroupKeys.detail(resolvedBrandSlug, resolvedId),
    ],
    onSuccess: () => { navigate(`/${brandSlug}/${lang}/admin/modifier-groups`); },
  });

  const {
    register,
    control,
    formState: { errors },
  } = form;

  // useFieldArray for the modifiers nested list
  const { fields, append, remove } = useFieldArray({
    control,
    name: 'modifiers',
  });

  // Pre-compute group name error message to avoid complex type inference in JSX
  const nlNameError = (
    errors.translations?.nl?.name as { message?: string } | undefined
  )?.message;

  // ---------------------------------------------------------------------------
  // Handlers
  // ---------------------------------------------------------------------------

  function handleDelete() {
    const name = form.getValues('translations.nl.name') || '(unnamed)';
    const message = t('admin.modifierGroups.confirmDelete', { name });
    if (window.confirm(message)) {
      deleteModifierGroup.mutate(resolvedId, {
        onSuccess: () => {
          navigate(`/${brandSlug}/${lang}/admin/modifier-groups`);
        },
      });
    }
  }

  function handleCancel() {
    navigate(`/${brandSlug}/${lang}/admin/modifier-groups`);
  }

  // ---------------------------------------------------------------------------
  // Form
  // ---------------------------------------------------------------------------

  return (
    <ResourceFormShell
      isFetching={isFetching}
      fetchError={fetchError}
      resourceName="modifier group"
      onCancel={handleCancel}
    >
    <main style={{ padding: '1.5rem', maxWidth: '48rem' }}>
      <h1 style={{ fontSize: '1.5rem', fontWeight: 700, marginBottom: '1.5rem' }}>
        {t('admin.modifierGroups.edit')}
      </h1>

      <form onSubmit={submit} noValidate>
        {/* Group name — Translation Tabs */}
        <p style={{ fontWeight: 600, fontSize: '0.875rem', marginBottom: '0.5rem' }}>
          {t('admin.modifierGroups.name')} <RequiredMark />
        </p>
        <TabBar activeTab={activeTab} onTabChange={setActiveTab} />

        <div style={{ marginBottom: '1.5rem' }}>
          <GroupTranslationFields
            activeTab={activeTab}
            register={register}
            nlNameError={nlNameError}
          />
        </div>

        {/* Modifiers section */}
        <div
          style={{
            borderTop: '1px solid #e5e7eb',
            paddingTop: '1.5rem',
            marginBottom: '1.5rem',
          }}
        >
          <div
            style={{
              display: 'flex',
              justifyContent: 'space-between',
              alignItems: 'center',
              marginBottom: '1rem',
            }}
          >
            <p style={{ fontWeight: 600, fontSize: '0.875rem', margin: 0 }}>
              {t('admin.modifierGroups.modifiers')}
            </p>
            <button
              type="button"
              onClick={() =>
                { append({
                  translations: { nl: { name: '' }, fr: { name: '' }, de: { name: '' } },
                  priceAdjustment: 0,
                }); }
              }
              style={{
                padding: '0.25rem 0.75rem',
                fontSize: '0.875rem',
                background: '#f9fafb',
                border: '1px solid #d1d5db',
                borderRadius: '0.375rem',
                cursor: 'pointer',
              }}
            >
              + {t('admin.modifierGroups.addModifier')}
            </button>
          </div>

          {fields.map((field, index) => (
            <ModifierRow
              key={field.id}
              index={index}
              register={register}
              activeTab={activeTab}
              onRemove={remove}
            />
          ))}
        </div>

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
            disabled={deleteModifierGroup.isPending}
            style={{
              padding: '0.5rem 1.25rem',
              background: '#fff',
              color: '#dc2626',
              border: '1px solid #fca5a5',
              borderRadius: '0.375rem',
              cursor: deleteModifierGroup.isPending ? 'not-allowed' : 'pointer',
              opacity: deleteModifierGroup.isPending ? 0.6 : 1,
              marginLeft: 'auto',
            }}
          >
            {deleteModifierGroup.isPending ? 'Deleting...' : t('admin.modifierGroups.delete')}
          </button>
        </div>
      </form>
    </main>
    </ResourceFormShell>
  );
}

// ---------------------------------------------------------------------------
// GroupTranslationFields — extracted so `register` call uses literal path strings,
// which TypeScript resolves correctly (dynamic template-literal paths produce
// unknown spreads in strict TSX).
// ---------------------------------------------------------------------------

interface GroupTranslationFieldsProps {
  activeTab: SupportedLocale;
  register: UseFormRegister<ModifierGroupEditFormValues>;
  nlNameError: string | undefined;
}

function GroupTranslationFields({ activeTab, register, nlNameError }: GroupTranslationFieldsProps) {
  if (activeTab === 'nl') {
    return (
      <>
        <input
          id="group-name-nl"
          type="text"
          {...register('translations.nl.name')}
          style={inputStyle(!!nlNameError)}
          placeholder="Group name in NL"
        />
        {nlNameError && <FieldError message={nlNameError} />}
      </>
    );
  }
  if (activeTab === 'fr') {
    return (
      <input
        id="group-name-fr"
        type="text"
        {...register('translations.fr.name')}
        style={inputStyle(false)}
        placeholder="Group name in FR"
      />
    );
  }
  return (
    <input
      id="group-name-de"
      type="text"
      {...register('translations.de.name')}
      style={inputStyle(false)}
      placeholder="Group name in DE"
    />
  );
}

// ---------------------------------------------------------------------------
// ModifierRow — one entry in the useFieldArray; extracted so `register` call
// uses literal-ish path strings. Index is passed and used via a switch on
// `activeTab` to produce typed register paths.
// ---------------------------------------------------------------------------

interface ModifierRowProps {
  index: number;
  register: UseFormRegister<ModifierGroupEditFormValues>;
  activeTab: SupportedLocale;
  onRemove: (index: number) => void;
}

function ModifierRow({ index, register, activeTab, onRemove }: ModifierRowProps) {
  const { t } = useTranslation();

  return (
    <div
      style={{
        border: '1px solid #e5e7eb',
        borderRadius: '0.375rem',
        padding: '1rem',
        marginBottom: '0.75rem',
        background: '#fafafa',
      }}
    >
      <div
        style={{
          display: 'flex',
          justifyContent: 'space-between',
          alignItems: 'center',
          marginBottom: '0.75rem',
        }}
      >
        <span style={{ fontWeight: 600, fontSize: '0.875rem', color: '#374151' }}>
          {t('admin.modifierGroups.modifiers')} #{index + 1}
        </span>
        <button
          type="button"
          onClick={() => { onRemove(index); }}
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

      <div style={{ marginBottom: '0.5rem' }}>
        <label style={labelStyle} htmlFor={`modifier-${index}-name-${activeTab}`}>
          {t('admin.modifierGroups.modifierName')} ({activeTab.toUpperCase()})
          {activeTab === 'nl' ? ' *' : null}
        </label>
        <ModifierTranslationFields index={index} register={register} activeTab={activeTab} />
      </div>

      <div>
        <label style={labelStyle} htmlFor={`modifier-${index}-price`}>
          {t('admin.modifierGroups.priceAdjustment')}
        </label>
        <input
          id={`modifier-${index}-price`}
          type="number"
          step="0.01"
          // eslint-disable-next-line @typescript-eslint/no-explicit-any
          {...register(`modifiers.${index}.priceAdjustment` as any, { valueAsNumber: true })}
          style={{ ...inputStyle(false), maxWidth: '12rem' }}
        />
      </div>
    </div>
  );
}

// ---------------------------------------------------------------------------
// ModifierTranslationFields — renders the name input for the active locale tab.
// Uses a switch on activeTab so each branch has a literal register path.
// The index is used via type assertion since template literal paths can't be
// statically verified for deeply nested dynamic arrays.
// ---------------------------------------------------------------------------

interface ModifierTranslationFieldsProps {
  index: number;
  register: UseFormRegister<ModifierGroupEditFormValues>;
  activeTab: SupportedLocale;
}

function ModifierTranslationFields({ index, register, activeTab }: ModifierTranslationFieldsProps) {
  if (activeTab === 'nl') {
    return (
      <input
        id={`modifier-${index}-name-nl`}
        type="text"
        // eslint-disable-next-line @typescript-eslint/no-explicit-any
        {...register(`modifiers.${index}.translations.nl.name` as any)}
        style={inputStyle(false)}
        placeholder="Modifier name in NL"
      />
    );
  }
  if (activeTab === 'fr') {
    return (
      <input
        id={`modifier-${index}-name-fr`}
        type="text"
        // eslint-disable-next-line @typescript-eslint/no-explicit-any
        {...register(`modifiers.${index}.translations.fr.name` as any)}
        style={inputStyle(false)}
        placeholder="Modifier name in FR"
      />
    );
  }
  return (
    <input
      id={`modifier-${index}-name-de`}
      type="text"
      // eslint-disable-next-line @typescript-eslint/no-explicit-any
      {...register(`modifiers.${index}.translations.de.name` as any)}
      style={inputStyle(false)}
      placeholder="Modifier name in DE"
    />
  );
}

// ---------------------------------------------------------------------------
// Tab bar sub-component
// ---------------------------------------------------------------------------

interface TabBarProps {
  activeTab: SupportedLocale;
  onTabChange: (tab: SupportedLocale) => void;
}

function TabBar({ activeTab, onTabChange }: TabBarProps) {
  return (
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

