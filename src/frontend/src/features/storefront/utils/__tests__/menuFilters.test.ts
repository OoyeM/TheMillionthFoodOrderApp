import { describe, expect, it } from 'vitest';
import { Allergen, DietaryTag } from '@/types/common';
import {
  EMPTY_FILTERS,
  activeFilterCount,
  isFilterActive,
  matchesFilters,
  toggleInSet,
  type MenuFilterState,
} from '../menuFilters';

function product(allergens: number[], dietaryTags: number[]) {
  return { allergens, dietaryTags };
}

function filters(
  excludedAllergens: number[] = [],
  requiredDietaryTags: number[] = [],
): MenuFilterState {
  return {
    excludedAllergens: new Set(excludedAllergens),
    requiredDietaryTags: new Set(requiredDietaryTags),
  };
}

describe('matchesFilters', () => {
  it('returns true for any product when no filters are active', () => {
    const p = product([Allergen.Gluten, Allergen.Nuts], []);
    expect(matchesFilters(p, EMPTY_FILTERS)).toBe(true);
  });

  it('excludes products containing an excluded allergen', () => {
    const p = product([Allergen.Gluten], []);
    expect(matchesFilters(p, filters([Allergen.Gluten]))).toBe(false);
  });

  it('keeps products that do not contain any excluded allergen', () => {
    const p = product([Allergen.Milk], []);
    expect(matchesFilters(p, filters([Allergen.Nuts]))).toBe(true);
  });

  it('keeps a product with no allergens regardless of allergen filters', () => {
    const p = product([], []);
    expect(matchesFilters(p, filters([Allergen.Nuts, Allergen.Gluten]))).toBe(true);
  });

  it('keeps only products carrying every required dietary tag (AND semantics)', () => {
    const veganGf = product([], [DietaryTag.Vegan, DietaryTag.GlutenFree]);
    const veganOnly = product([], [DietaryTag.Vegan]);
    const required = filters([], [DietaryTag.Vegan, DietaryTag.GlutenFree]);
    expect(matchesFilters(veganGf, required)).toBe(true);
    expect(matchesFilters(veganOnly, required)).toBe(false);
  });

  it('combines allergen exclusion and dietary requirement', () => {
    const p = product([Allergen.Nuts], [DietaryTag.Vegetarian]);
    const f = filters([Allergen.Nuts], [DietaryTag.Vegetarian]);
    // Excluded allergen wins → product is hidden even though dietary matches.
    expect(matchesFilters(p, f)).toBe(false);
  });

  it('returns true when both filter dimensions match', () => {
    const p = product([Allergen.Milk], [DietaryTag.Vegetarian]);
    const f = filters([Allergen.Nuts], [DietaryTag.Vegetarian]);
    expect(matchesFilters(p, f)).toBe(true);
  });
});

describe('isFilterActive', () => {
  it('is false for the empty state', () => {
    expect(isFilterActive(EMPTY_FILTERS)).toBe(false);
  });

  it('is true when at least one allergen or tag is selected', () => {
    expect(isFilterActive(filters([Allergen.Gluten]))).toBe(true);
    expect(isFilterActive(filters([], [DietaryTag.Vegan]))).toBe(true);
  });
});

describe('activeFilterCount', () => {
  it('sums excluded allergens and required dietary tags', () => {
    expect(activeFilterCount(EMPTY_FILTERS)).toBe(0);
    expect(activeFilterCount(filters([Allergen.Gluten, Allergen.Nuts]))).toBe(2);
    expect(
      activeFilterCount(filters([Allergen.Gluten], [DietaryTag.Vegan, DietaryTag.Halal])),
    ).toBe(3);
  });
});

describe('toggleInSet', () => {
  it('adds the value when absent', () => {
    const result = toggleInSet(new Set([1]), 2);
    expect([...result].sort()).toEqual([1, 2]);
  });

  it('removes the value when present', () => {
    const result = toggleInSet(new Set([1, 2]), 2);
    expect([...result]).toEqual([1]);
  });

  it('does not mutate the input set', () => {
    const input = new Set([1]);
    toggleInSet(input, 2);
    expect([...input]).toEqual([1]);
  });
});
