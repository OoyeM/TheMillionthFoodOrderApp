import { useState } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { useCreateModifierGroup } from '../hooks/useModifierGroups';
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
  modifiers?: string;
}

// ---------------------------------------------------------------------------
// Page component
// ---------------------------------------------------------------------------

export function ModifierGroupCreate() {
  const navigate = useNavigate();
  const { t } = useTranslation();
  const { brandSlug, lang } = useParams<{ brandSlug: string; lang: string }>();
  const resolvedBrandSlug = brandSlug ?? '';
  const createModifierGroup = useCreateModifierGroup(resolvedBrandSlug);
  const { data: brandSettings } = useBrandSettings(resolvedBrandSlug);
  const primaryLocale = extractPrimaryLocale(brandSettings?.defaultLanguage);

  const [activeTab, setActiveTab] = useState<SupportedLocale>(primaryLocale);
  const [groupTranslations, setGroupTranslations] = useState<GroupTranslationsMap>({
    nl: { name: '' },
    fr: { name: '' },
    de: { name: '' },
  });
  const [modifiers, setModifiers] = useState<ModifierFormState[]>([emptyModifier()]);
  const [errors, setErrors] = useState<FormErrors>({});

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

    createModifierGroup.mutate(
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

  function handleCancel() {
    navigate(`/${brandSlug}/${lang}/admin/modifier-groups`);
  }

  return (
    <main style={{ padding: '1.5rem', maxWidth: '48rem' }}>
      <h1 style={{ fontSize: '1.5rem', fontWeight: 700, marginBottom: '1.5rem' }}>
        {t('admin.modifierGroups.create')}
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
              key={index}
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

        {/* API error */}
        {createModifierGroup.isError && (
          <p style={{ color: '#dc2626', marginBottom: '1rem', fontSize: '0.875rem' }}>
            {createModifierGroup.error instanceof Error
              ? createModifierGroup.error.message
              : 'Failed to create modifier group. Please try again.'}
          </p>
        )}

        <div style={{ display: 'flex', gap: '0.75rem' }}>
          <button
            type="submit"
            disabled={createModifierGroup.isPending}
            style={{
              padding: '0.5rem 1.25rem',
              background: '#111827',
              color: '#fff',
              border: 'none',
              borderRadius: '0.375rem',
              cursor: createModifierGroup.isPending ? 'not-allowed' : 'pointer',
              fontWeight: 600,
              opacity: createModifierGroup.isPending ? 0.6 : 1,
            }}
          >
            {createModifierGroup.isPending ? 'Creating...' : t('admin.modifierGroups.create')}
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
