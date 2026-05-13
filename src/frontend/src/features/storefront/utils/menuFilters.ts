import type { ProductListItem } from '@/types/common';

/**
 * Active storefront menu filters. Both sets are keyed by the numeric enum value
 * (matching `Allergen` and `DietaryTag` in `@/types/common`).
 *
 * Semantics:
 * - `excludedAllergens` removes products that contain any of the listed allergens.
 * - `requiredDietaryTags` keeps only products that carry every listed dietary tag.
 */
export interface MenuFilterState {
  excludedAllergens: ReadonlySet<number>;
  requiredDietaryTags: ReadonlySet<number>;
}

export const EMPTY_FILTERS: MenuFilterState = {
  excludedAllergens: new Set<number>(),
  requiredDietaryTags: new Set<number>(),
};

export function isFilterActive(filters: MenuFilterState): boolean {
  return filters.excludedAllergens.size > 0 || filters.requiredDietaryTags.size > 0;
}

export function activeFilterCount(filters: MenuFilterState): number {
  return filters.excludedAllergens.size + filters.requiredDietaryTags.size;
}

export function matchesFilters(
  product: Pick<ProductListItem, 'allergens' | 'dietaryTags'>,
  filters: MenuFilterState,
): boolean {
  for (const allergen of product.allergens) {
    if (filters.excludedAllergens.has(allergen)) return false;
  }
  for (const tag of filters.requiredDietaryTags) {
    if (!product.dietaryTags.includes(tag)) return false;
  }
  return true;
}

export function toggleInSet(set: ReadonlySet<number>, value: number): Set<number> {
  const next = new Set(set);
  if (next.has(value)) next.delete(value);
  else next.add(value);
  return next;
}
