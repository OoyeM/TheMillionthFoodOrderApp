import type React from 'react';
import { Controller, type Control } from 'react-hook-form';
import { useTranslation } from 'react-i18next';
import { Allergen, DietaryTag, ALLERGEN_KEYS, DIETARY_TAG_KEYS } from '../../../types/common';
import type { ProductEditFormValues } from '../pages/schemas/productEditSchema';

// ---------------------------------------------------------------------------
// AllergenDietaryFields — the allergen + dietary-tag checkbox grids shared by
// the product editor.
//
//  - `mode: 'edit'`     — interactive, Controller-bound (simple products).
//  - `mode: 'readonly'` — disabled grids that display a computed roll-up
//                         (combo products inherit their components' values).
// ---------------------------------------------------------------------------

const sectionStyle: React.CSSProperties = { marginBottom: '1.5rem' };
const headingStyle: React.CSSProperties = {
  fontWeight: 600,
  fontSize: '0.875rem',
  marginBottom: '0.5rem',
};
const gridStyle: React.CSSProperties = { display: 'flex', flexWrap: 'wrap', gap: '0.5rem' };
const hintStyle: React.CSSProperties = {
  fontSize: '0.75rem',
  color: '#6b7280',
  marginTop: '0.375rem',
};

interface ToggleItem {
  value: number;
  label: string;
}

interface ToggleGridProps {
  items: ToggleItem[];
  selected: number[];
  /** When omitted the grid is read-only (disabled checkboxes). */
  onToggle?: (value: number, currentlyChecked: boolean) => void;
}

function ToggleGrid({ items, selected, onToggle }: ToggleGridProps): React.JSX.Element {
  const readonly = onToggle === undefined;
  return (
    <div style={gridStyle}>
      {items.map((item) => {
        const checked = selected.includes(item.value);
        return (
          <label
            key={item.value}
            style={{
              display: 'flex',
              alignItems: 'center',
              gap: '0.25rem',
              fontSize: '0.875rem',
              cursor: readonly ? 'default' : 'pointer',
              color: readonly && !checked ? '#9ca3af' : '#374151',
            }}
          >
            <input
              type="checkbox"
              checked={checked}
              disabled={readonly}
              onChange={readonly ? undefined : () => { onToggle(item.value, checked); }}
            />
            {item.label}
          </label>
        );
      })}
    </div>
  );
}

type AllergenDietaryFieldsProps =
  | { mode: 'edit'; control: Control<ProductEditFormValues> }
  | { mode: 'readonly'; allergens: number[]; dietaryTags: number[] };

export function AllergenDietaryFields(props: AllergenDietaryFieldsProps): React.JSX.Element {
  const { t } = useTranslation();

  const allergenItems: ToggleItem[] = ALLERGEN_KEYS.map((key) => ({
    value: Allergen[key],
    label: t(`allergens.${key}`),
  }));
  const dietaryItems: ToggleItem[] = DIETARY_TAG_KEYS.map((key) => ({
    value: DietaryTag[key],
    label: t(`dietaryTags.${key}`),
  }));

  if (props.mode === 'readonly') {
    return (
      <>
        <div style={sectionStyle}>
          <p style={headingStyle}>{t('admin.products.allergens')}</p>
          <ToggleGrid items={allergenItems} selected={props.allergens} />
          <p style={hintStyle}>
            Combined from the selected component products (union) — not editable for combos.
          </p>
        </div>
        <div style={sectionStyle}>
          <p style={headingStyle}>{t('admin.products.dietaryTags')}</p>
          <ToggleGrid items={dietaryItems} selected={props.dietaryTags} />
          <p style={hintStyle}>
            Applies only when every component product shares the tag (intersection).
          </p>
        </div>
      </>
    );
  }

  const { control } = props;
  return (
    <>
      <div style={sectionStyle}>
        <p style={headingStyle}>{t('admin.products.allergens')}</p>
        <Controller
          name="allergens"
          control={control}
          render={({ field }) => {
            // eslint-disable-next-line @typescript-eslint/no-unnecessary-condition -- RHF field.value can be undefined before the form resets to fetched values
            const current = field.value ?? [];
            return (
              <ToggleGrid
                items={allergenItems}
                selected={current}
                onToggle={(value, checked) => {
                  field.onChange(checked ? current.filter((v) => v !== value) : [...current, value]);
                }}
              />
            );
          }}
        />
      </div>
      <div style={sectionStyle}>
        <p style={headingStyle}>{t('admin.products.dietaryTags')}</p>
        <Controller
          name="dietaryTags"
          control={control}
          render={({ field }) => {
            // eslint-disable-next-line @typescript-eslint/no-unnecessary-condition -- RHF field.value can be undefined before the form resets to fetched values
            const current = field.value ?? [];
            return (
              <ToggleGrid
                items={dietaryItems}
                selected={current}
                onToggle={(value, checked) => {
                  field.onChange(checked ? current.filter((v) => v !== value) : [...current, value]);
                }}
              />
            );
          }}
        />
      </div>
    </>
  );
}
