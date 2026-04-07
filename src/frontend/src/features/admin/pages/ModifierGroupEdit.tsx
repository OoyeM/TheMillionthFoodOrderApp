import { useState, useEffect } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import {
  useModifierGroup,
  useUpdateModifierGroup,
  useDeleteModifierGroup,
} from '../hooks/useModifierGroups';
import { useBrandSettings } from '../hooks/useBrandSettings';
import { extractPrimaryLocale } from '../../../types/common';
import type { SupportedLocale } from '../../../types/common';

// ---------------------------------------------------------------------------
// Constants
// ---------------------------------------------------------------------------

const LANGUAGES: { code: SupportedLocale; label: string }[] = [
  { code: 'nl', label: 'NL' },
  { code: 'fr', label: 'FR' },
  { code: 'de', label: 'DE' },
];

interface ModifierTranslationState {
  name: string;
}

type ModifierTranslationsMap = Record<SupportedLocale, ModifierTranslationState>;

interface ModifierFormState {
  id?: string;
  translations: ModifierTranslationsMap;
  priceAdjustment: string;
}

function emptyModifier(): ModifierFormState {
  return {
    translations: { nl: { name: '' }, fr: { name: '' }, de: { name: '' } },
    priceAdjustment: '0',
  };
}

type GroupTranslationsMap = Record<SupportedLocale, ModifierTranslationState>;

interface FormErrors {
  primaryName?: string;
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
  const { data: brandSettings } = useBrandSettings(resolvedBrandSlug);
  const primaryLocale = extractPrimaryLocale(brandSettings?.defaultLanguage);

  const { data: group, isLoading, isError, error } = useModifierGroup(resolvedBrandSlug, resolvedId);
  const updateModifierGroup = useUpdateModifierGroup(resolvedBrandSlug, resolvedId);
  const deleteModifierGroup = useDeleteModifierGroup(resolvedBrandSlug);

  const [activeTab, setActiveTab] = useState<SupportedLocale>('nl');
  const [groupTranslations, setGroupTranslations] = useState<GroupTranslationsMap>({
    nl: { name: '' },
    fr: { name: '' },
    de: { name: '' },
  });
  const [modifiers, setModifiers] = useState<ModifierFormState[]>([]);
  const [errors, setErrors] = useState<FormErrors>({});
  const [formInitialized, setFormInitialized] = useState(false);

  // Populate form when group data arrives
  useEffect(() => {
    if (group !== undefined && !formInitialized) {
      const translationsMap: GroupTranslationsMap = {
        nl: { name: '' },
        fr: { name: '' },
        de: { name: '' },
      };
      for (const t of group.translations) {
        if (t.languageCode === 'nl' || t.languageCode === 'fr' || t.languageCode === 'de') {
          translationsMap[t.languageCode as SupportedLocale] = { name: t.name };
        }
      }
      setGroupTranslations(translationsMap);

      const modifierForms: ModifierFormState[] = group.modifiers
        .sort((a, b) => a.sortOrder - b.sortOrder)
        .map((m) => {
          const modTranslations: ModifierTranslationsMap = {
            nl: { name: '' },
            fr: { name: '' },
            de: { name: '' },
          };
          for (const tr of m.translations) {
            if (tr.languageCode === 'nl' || tr.languageCode === 'fr' || tr.languageCode === 'de') {
              modTranslations[tr.languageCode as SupportedLocale] = { name: tr.name };
            }
          }
          return {
            id: m.id,
            translations: modTranslations,
            priceAdjustment: m.priceAdjustment.toString(),
          };
        });

      setModifiers(modifierForms.length > 0 ? modifierForms : [emptyModifier()]);
      setFormInitialized(true);
    }
  }, [group, formInitialized]);

  function updateGroupTranslation(locale: SupportedLocale, value: string) {
    setGroupTranslations((prev) => ({
      ...prev,
      [locale]: { name: value },
    }));
  }

  function updateModifierTranslation(
    index: number,
    locale: SupportedLocale,
    value: string,
  ) {
    setModifiers((prev) =>
      prev.map((m, i) =>
        i === index
          ? { ...m, translations: { ...m.translations, [locale]: { name: value } } }
          : m,
      ),
    );
  }

  function updateModifierPrice(index: number, value: string) {
    setModifiers((prev) =>
      prev.map((m, i) => (i === index ? { ...m, priceAdjustment: value } : m)),
    );
  }

  function addModifier() {
    setModifiers((prev) => [...prev, emptyModifier()]);
  }

  function removeModifier(index: number) {
    setModifiers((prev) => prev.filter((_, i) => i !== index));
  }

  function validate(): FormErrors {
    const next: FormErrors = {};
    if (groupTranslations[primaryLocale].name.trim().length === 0) {
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

    const groupTranslationInputs = LANGUAGES.filter(
      (l) => groupTranslations[l.code].name.trim().length > 0,
    ).map((l) => ({
      languageCode: l.code,
      name: groupTranslations[l.code].name.trim(),
    }));

    const modifierInputs = modifiers.map((m, index) => ({
      translations: LANGUAGES.filter(
        (l) => m.translations[l.code].name.trim().length > 0,
      ).map((l) => ({
        languageCode: l.code,
        name: m.translations[l.code].name.trim(),
      })),
      priceAdjustment: parseFloat(m.priceAdjustment) || 0,
      sortOrder: index,
    }));

    updateModifierGroup.mutate(
      {
        translations: groupTranslationInputs,
        modifiers: modifierInputs,
      },
      {
        onSuccess: () => {
          navigate(`/${brandSlug}/${lang}/admin/modifier-groups`);
        },
      },
    );
  }

  function handleDelete() {
    const name = groupTranslations.nl.name || '(unnamed)';
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
  // Loading / error states
  // ---------------------------------------------------------------------------

  if (isLoading) {
    return (
      <main style={{ padding: '1.5rem' }}>
        <p style={{ color: '#6b7280' }}>Loading...</p>
      </main>
    );
  }

  if (isError) {
    return (
      <main style={{ padding: '1.5rem' }}>
        <p style={{ color: '#dc2626' }}>
          {error instanceof Error ? error.message : 'Unknown error'}
        </p>
        <button onClick={handleCancel} style={secondaryButtonStyle}>
          Back to list
        </button>
      </main>
    );
  }

  if (group === undefined) {
    return (
      <main style={{ padding: '1.5rem' }}>
        <p style={{ color: '#6b7280' }}>Modifier group not found.</p>
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
        {t('admin.modifierGroups.edit')}
      </h1>

      <form onSubmit={handleSubmit} noValidate>
        {/* Group name — Translation Tabs */}
        <p style={{ fontWeight: 600, fontSize: '0.875rem', marginBottom: '0.5rem' }}>
          {t('admin.modifierGroups.name')} <RequiredMark />
        </p>
        <TabBar
          activeTab={activeTab}
          primaryLocale={primaryLocale}
          onTabChange={setActiveTab}
          languages={LANGUAGES}
        />

        <div style={{ marginBottom: '1.5rem' }}>
          <input
            id={`group-name-${activeTab}`}
            type="text"
            value={groupTranslations[activeTab].name}
            onChange={(e) => updateGroupTranslation(activeTab, e.target.value)}
            style={inputStyle(activeTab === primaryLocale && !!errors.primaryName)}
            placeholder={`Group name in ${activeTab.toUpperCase()}`}
          />
          {activeTab === primaryLocale && errors.primaryName && <FieldError message={errors.primaryName} />}
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
              onClick={addModifier}
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

          {modifiers.map((modifier, index) => (
            <ModifierFormRow
              key={modifier.id ?? `new-${index}`}
              index={index}
              modifier={modifier}
              activeTab={activeTab}
              primaryLocale={primaryLocale}
              onUpdateTranslation={updateModifierTranslation}
              onUpdatePrice={updateModifierPrice}
              onRemove={removeModifier}
              languages={LANGUAGES}
            />
          ))}
        </div>

        {/* Metadata */}
        <p style={{ fontSize: '0.75rem', color: '#9ca3af', marginBottom: '1.5rem' }}>
          Created: {new Date(group.createdAt).toLocaleString()} &mdash; Last updated:{' '}
          {new Date(group.updatedAt).toLocaleString()}
        </p>

        {/* API error */}
        {updateModifierGroup.isError && (
          <p style={{ color: '#dc2626', marginBottom: '1rem', fontSize: '0.875rem' }}>
            {updateModifierGroup.error instanceof Error
              ? updateModifierGroup.error.message
              : 'Failed to save changes. Please try again.'}
          </p>
        )}

        <div style={{ display: 'flex', gap: '0.75rem' }}>
          <button
            type="submit"
            disabled={updateModifierGroup.isPending}
            style={{
              padding: '0.5rem 1.25rem',
              background: '#111827',
              color: '#fff',
              border: 'none',
              borderRadius: '0.375rem',
              cursor: updateModifierGroup.isPending ? 'not-allowed' : 'pointer',
              fontWeight: 600,
              opacity: updateModifierGroup.isPending ? 0.6 : 1,
            }}
          >
            {updateModifierGroup.isPending ? 'Saving...' : 'Save Changes'}
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
  );
}

// ---------------------------------------------------------------------------
// Modifier form row sub-component
// ---------------------------------------------------------------------------

interface ModifierFormRowProps {
  index: number;
  modifier: ModifierFormState;
  activeTab: SupportedLocale;
  primaryLocale: SupportedLocale;
  onUpdateTranslation: (index: number, locale: SupportedLocale, value: string) => void;
  onUpdatePrice: (index: number, value: string) => void;
  onRemove: (index: number) => void;
  languages: { code: SupportedLocale; label: string }[];
}

function ModifierFormRow({
  index,
  modifier,
  activeTab,
  primaryLocale,
  onUpdateTranslation,
  onUpdatePrice,
  onRemove,
  languages,
}: ModifierFormRowProps) {
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
          onClick={() => onRemove(index)}
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
          {activeTab === primaryLocale && ' *'}
        </label>
        <input
          id={`modifier-${index}-name-${activeTab}`}
          type="text"
          value={modifier.translations[activeTab].name}
          onChange={(e) => onUpdateTranslation(index, activeTab, e.target.value)}
          style={inputStyle(false)}
          placeholder={`Modifier name in ${activeTab.toUpperCase()}`}
        />
        <p style={{ fontSize: '0.75rem', color: '#9ca3af', marginTop: '0.25rem' }}>
          {languages
            .filter((l) => l.code !== activeTab && modifier.translations[l.code].name)
            .map((l) => `${l.label}: ${modifier.translations[l.code].name}`)
            .join(' | ')}
        </p>
      </div>

      <div>
        <label style={labelStyle} htmlFor={`modifier-${index}-price`}>
          {t('admin.modifierGroups.priceAdjustment')}
        </label>
        <input
          id={`modifier-${index}-price`}
          type="number"
          step="0.01"
          value={modifier.priceAdjustment}
          onChange={(e) => onUpdatePrice(index, e.target.value)}
          style={{ ...inputStyle(false), maxWidth: '12rem' }}
        />
      </div>
    </div>
  );
}

// ---------------------------------------------------------------------------
// Tab bar sub-component
// ---------------------------------------------------------------------------

interface TabBarProps {
  activeTab: SupportedLocale;
  primaryLocale: SupportedLocale;
  onTabChange: (tab: SupportedLocale) => void;
  languages: { code: SupportedLocale; label: string }[];
}

function TabBar({ activeTab, primaryLocale, onTabChange, languages }: TabBarProps) {
  return (
    <div
      style={{
        display: 'flex',
        marginBottom: '1rem',
        borderBottom: '2px solid #e5e7eb',
      }}
    >
      {languages.map((l) => (
        <button
          key={l.code}
          type="button"
          onClick={() => onTabChange(l.code)}
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
