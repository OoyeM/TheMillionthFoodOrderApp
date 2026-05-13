import { useTranslation } from 'react-i18next';
import {
  Allergen,
  DietaryTag,
  ALLERGEN_KEYS,
  DIETARY_TAG_KEYS,
} from '@/types/common';
import {
  EMPTY_FILTERS,
  activeFilterCount,
  isFilterActive,
  toggleInSet,
  type MenuFilterState,
} from '../utils/menuFilters';

interface MenuFiltersProps {
  filters: MenuFilterState;
  onChange: (next: MenuFilterState) => void;
}

const containerStyle: React.CSSProperties = {
  marginBottom: '1.5rem',
  border: '1px solid #e5e7eb',
  borderRadius: '0.5rem',
  background: '#fff',
};

const summaryStyle: React.CSSProperties = {
  display: 'flex',
  alignItems: 'center',
  justifyContent: 'space-between',
  gap: '0.5rem',
  padding: '0.75rem 1rem',
  fontWeight: 600,
  fontSize: '0.9375rem',
  color: '#111827',
  cursor: 'pointer',
  listStyle: 'none',
};

const sectionTitleStyle: React.CSSProperties = {
  fontSize: '0.8125rem',
  fontWeight: 700,
  color: '#374151',
  textTransform: 'uppercase',
  letterSpacing: '0.05em',
  margin: '0 0 0.5rem',
};

const chipRowStyle: React.CSSProperties = {
  display: 'flex',
  flexWrap: 'wrap',
  gap: '0.375rem',
};

function chipStyle(selected: boolean, tone: 'allergen' | 'dietary'): React.CSSProperties {
  const selectedBg =
    tone === 'allergen' ? 'var(--brand-color-primary, #111827)' : '#15803d';
  return {
    padding: '0.3125rem 0.75rem',
    borderRadius: '9999px',
    border: `1px solid ${selected ? selectedBg : '#d1d5db'}`,
    background: selected ? selectedBg : '#fff',
    color: selected ? '#fff' : '#374151',
    fontSize: '0.8125rem',
    fontWeight: 500,
    cursor: 'pointer',
    transition: 'background 0.12s, border-color 0.12s, color 0.12s',
  };
}

export function MenuFilters({ filters, onChange }: MenuFiltersProps) {
  const { t } = useTranslation('common');
  const active = isFilterActive(filters);
  const count = activeFilterCount(filters);

  function toggleAllergen(value: number) {
    onChange({
      ...filters,
      excludedAllergens: toggleInSet(filters.excludedAllergens, value),
    });
  }

  function toggleDietary(value: number) {
    onChange({
      ...filters,
      requiredDietaryTags: toggleInSet(filters.requiredDietaryTags, value),
    });
  }

  function clearAll() {
    onChange(EMPTY_FILTERS);
  }

  return (
    <details style={containerStyle}>
      <summary style={summaryStyle} aria-label={t('storefront.menu.filters.title')}>
        <span>{t('storefront.menu.filters.title')}</span>
        {active && (
          <span
            style={{
              fontSize: '0.75rem',
              fontWeight: 600,
              color: '#fff',
              background: 'var(--brand-color-accent, #2563eb)',
              padding: '0.125rem 0.5rem',
              borderRadius: '9999px',
            }}
            aria-live="polite"
          >
            {t('storefront.menu.filters.active', { count })}
          </span>
        )}
      </summary>

      <div style={{ padding: '0 1rem 1rem' }}>
        <section style={{ marginTop: '0.75rem' }}>
          <p style={sectionTitleStyle}>{t('storefront.menu.filters.excludeAllergens')}</p>
          <div style={chipRowStyle} role="group" aria-label={t('storefront.menu.filters.excludeAllergens')}>
            {ALLERGEN_KEYS.map((key) => {
              const value = Allergen[key];
              const selected = filters.excludedAllergens.has(value);
              const label = t(`allergens.${key}`);
              return (
                <button
                  key={key}
                  type="button"
                  aria-pressed={selected}
                  onClick={() => { toggleAllergen(value); }}
                  style={chipStyle(selected, 'allergen')}
                >
                  <span aria-hidden="true" style={{ marginRight: '0.25rem' }}>⚠</span>
                  {label}
                </button>
              );
            })}
          </div>
        </section>

        <section style={{ marginTop: '1rem' }}>
          <p style={sectionTitleStyle}>{t('storefront.menu.filters.requireDietary')}</p>
          <div style={chipRowStyle} role="group" aria-label={t('storefront.menu.filters.requireDietary')}>
            {DIETARY_TAG_KEYS.map((key) => {
              const value = DietaryTag[key];
              const selected = filters.requiredDietaryTags.has(value);
              const label = t(`dietaryTags.${key}`);
              return (
                <button
                  key={key}
                  type="button"
                  aria-pressed={selected}
                  onClick={() => { toggleDietary(value); }}
                  style={chipStyle(selected, 'dietary')}
                >
                  <span aria-hidden="true" style={{ marginRight: '0.25rem' }}>✓</span>
                  {label}
                </button>
              );
            })}
          </div>
        </section>

        {active && (
          <div style={{ marginTop: '1rem', display: 'flex', justifyContent: 'flex-end' }}>
            <button
              type="button"
              onClick={clearAll}
              style={{
                padding: '0.375rem 0.875rem',
                borderRadius: '0.375rem',
                border: '1px solid #d1d5db',
                background: '#fff',
                color: '#374151',
                fontSize: '0.8125rem',
                fontWeight: 600,
                cursor: 'pointer',
              }}
            >
              {t('storefront.menu.filters.clearAll')}
            </button>
          </div>
        )}
      </div>
    </details>
  );
}
