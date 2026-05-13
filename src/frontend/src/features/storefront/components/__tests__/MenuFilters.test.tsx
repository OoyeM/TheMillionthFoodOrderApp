import { beforeAll, describe, it, expect, vi } from 'vitest';
import { screen, render } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import i18next from 'i18next';
import { Allergen, DietaryTag, ALLERGEN_KEYS, DIETARY_TAG_KEYS } from '@/types/common';
import { MenuFilters } from '../MenuFilters';
import { EMPTY_FILTERS, type MenuFilterState } from '../../utils/menuFilters';
import '../../../../i18n/config';

beforeAll(async () => {
  await i18next.changeLanguage('nl');
});

function t(key: string): string {
  return i18next.t(key);
}

function allergenLabel(key: keyof typeof Allergen): string {
  return t(`allergens.${key}`);
}
function dietaryLabel(key: keyof typeof DietaryTag): string {
  return t(`dietaryTags.${key}`);
}

function setup(initial: MenuFilterState = EMPTY_FILTERS) {
  const onChange = vi.fn<(next: MenuFilterState) => void>();
  const utils = render(<MenuFilters filters={initial} onChange={onChange} />);
  return { ...utils, onChange };
}

describe('MenuFilters', () => {
  it('renders every allergen and dietary tag as a toggle button', () => {
    setup();
    for (const key of ALLERGEN_KEYS) {
      expect(screen.getByRole('button', { name: allergenLabel(key) })).toBeInTheDocument();
    }
    for (const key of DIETARY_TAG_KEYS) {
      expect(screen.getByRole('button', { name: dietaryLabel(key) })).toBeInTheDocument();
    }
  });

  it('emits an updated set when an allergen chip is toggled on', async () => {
    const user = userEvent.setup();
    const { onChange } = setup();
    await user.click(screen.getByRole('button', { name: allergenLabel('Nuts') }));

    expect(onChange).toHaveBeenCalledTimes(1);
    const next = onChange.mock.calls[0]?.[0];
    expect(next?.excludedAllergens.has(Allergen.Nuts)).toBe(true);
    expect(next?.requiredDietaryTags.size).toBe(0);
  });

  it('removes an allergen when toggled off', async () => {
    const user = userEvent.setup();
    const initial: MenuFilterState = {
      excludedAllergens: new Set([Allergen.Nuts]),
      requiredDietaryTags: new Set(),
    };
    const { onChange } = setup(initial);
    await user.click(screen.getByRole('button', { name: allergenLabel('Nuts') }));

    const next = onChange.mock.calls[0]?.[0];
    expect(next?.excludedAllergens.has(Allergen.Nuts)).toBe(false);
  });

  it('emits an updated set when a dietary chip is toggled on', async () => {
    const user = userEvent.setup();
    const { onChange } = setup();
    await user.click(screen.getByRole('button', { name: dietaryLabel('Vegan') }));

    const next = onChange.mock.calls[0]?.[0];
    expect(next?.requiredDietaryTags.has(DietaryTag.Vegan)).toBe(true);
  });

  it('reflects selection state via aria-pressed', () => {
    setup({
      excludedAllergens: new Set([Allergen.Gluten]),
      requiredDietaryTags: new Set([DietaryTag.Vegan]),
    });
    expect(screen.getByRole('button', { name: allergenLabel('Gluten') })).toHaveAttribute(
      'aria-pressed',
      'true',
    );
    expect(screen.getByRole('button', { name: dietaryLabel('Vegan') })).toHaveAttribute(
      'aria-pressed',
      'true',
    );
    expect(screen.getByRole('button', { name: dietaryLabel('Halal') })).toHaveAttribute(
      'aria-pressed',
      'false',
    );
  });

  it('hides "Clear all" when no filter is active', () => {
    setup();
    const clearLabel = t('storefront.menu.filters.clearAll');
    expect(screen.queryByRole('button', { name: clearLabel })).toBeNull();
  });

  it('emits empty filters when "Clear all" is clicked', async () => {
    const user = userEvent.setup();
    const initial: MenuFilterState = {
      excludedAllergens: new Set([Allergen.Nuts]),
      requiredDietaryTags: new Set([DietaryTag.Vegan]),
    };
    const { onChange } = setup(initial);
    const clearLabel = t('storefront.menu.filters.clearAll');
    await user.click(screen.getByRole('button', { name: clearLabel }));

    const next = onChange.mock.calls[0]?.[0];
    expect(next?.excludedAllergens.size).toBe(0);
    expect(next?.requiredDietaryTags.size).toBe(0);
  });
});
