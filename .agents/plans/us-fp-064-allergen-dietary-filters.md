# US-FP-064 — Browse menu with allergen and dietary filters

## Story
As a Registered Customer, I want to filter the menu by allergens and dietary preferences,
so that I can quickly find safe food options.

**Acceptance criteria:**
- Filter UI for **allergens** — selecting an allergen *excludes* products containing it.
- Filter UI for **dietary tags** — selecting a tag *requires* products to have it.
- Filters combine (e.g. Vegetarian AND no Nuts).
- Product cards display allergen icons + dietary tags.

## Scope
Frontend-only. `ProductListItem` already carries `allergens: number[]` and
`dietaryTags: number[]` from the API, and i18n entries for every allergen/tag
already exist (`allergens.{Key}`, `dietaryTags.{Key}`) in NL/FR/DE.

No backend, API, or DB changes. No new dependencies.

## Files

### New
| Path | Purpose |
| --- | --- |
| `src/frontend/src/features/storefront/components/MenuFilters.tsx` | Filter UI: collapsible panel with allergen exclusion chips + dietary tag chips + "Clear all" |
| `src/frontend/src/features/storefront/utils/menuFilters.ts` | Pure helper `matchesFilters(product, filters)` and `MenuFilterState` type |
| `src/frontend/src/features/storefront/components/__tests__/MenuFilters.test.tsx` | Component tests (toggling, clear-all) |
| `src/frontend/src/features/storefront/components/__tests__/ProductCard.test.tsx` | Asserts allergen/dietary chips render |
| `src/frontend/src/features/storefront/utils/__tests__/menuFilters.test.ts` | Pure-function tests for filter logic (combinations, empty case) |

### Modified
| Path | Change |
| --- | --- |
| `src/frontend/src/features/storefront/pages/MenuPage.tsx` | Hold filter state, render `<MenuFilters>`, pass filters to `CategorySection`, show "no matches" copy when all sections are filtered out |
| `src/frontend/src/features/storefront/components/ProductCard.tsx` | Render allergen icons + dietary tag chips below the price line |
| `src/frontend/src/i18n/locales/{nl,fr,de}/common.json` | Add `storefront.menu.filters.*` keys |

## Design notes

**Filter semantics** (matches AC):
```ts
function matchesFilters(p: ProductListItem, f: MenuFilterState): boolean {
  if (p.allergens.some(a => f.excludedAllergens.has(a))) return false;
  for (const tag of f.requiredDietaryTags) {
    if (!p.dietaryTags.includes(tag)) return false;
  }
  return true;
}
```

**State shape** (lifted into `MenuContent`):
```ts
interface MenuFilterState {
  excludedAllergens: Set<number>;     // Allergen enum values
  requiredDietaryTags: Set<number>;   // DietaryTag enum values
}
```
Create new Sets on every toggle so React re-renders. Filters are session-only.

**MenuFilters UI:**
- Sticky panel below page title, collapsible via `<details>` (accessible, no extra JS).
- Two sections: "Exclude allergens" + "Dietary requirements".
- Each option is a toggle chip (`<button type="button"` with `aria-pressed`).
- Footer row: "X active" + "Clear all" (hidden when no filters active).

**ProductCard chips:**
- Below price, render allergen chips then dietary chips.
- Each chip has `title` + `aria-label` for screen readers.
- Skip rendering the row when both arrays empty.

**MenuContent / "no matches":**
- Each `CategorySection` reports its filtered match count back via a callback.
- `MenuContent` aggregates per-category counts in `useRef`-backed state; if filters are active AND total matches === 0, show `storefront.menu.filters.noMatches`.

## i18n additions

```json
"filters": {
  "title": "Filters",
  "excludeAllergens": "Exclude allergens",
  "requireDietary": "Dietary requirements",
  "clearAll": "Clear all",
  "active": "{{count}} active",
  "noMatches": "No products match your filters.",
  "open": "Open filters",
  "close": "Close filters"
}
```
Translations: NL, FR, DE.

## Tests

**`menuFilters.test.ts`** (pure):
- Empty filters → all products match.
- Excluded allergen filters out products containing it.
- Required dietary tag keeps only products with that tag.
- Combined (AND) — excludes allergen even when dietary tag matches.
- Product with no allergens passes any allergen filter.

**`MenuFilters.test.tsx`**:
- Renders all 14 allergens + 4 dietary tags as buttons.
- Clicking toggles `aria-pressed` and emits new Set.
- "Clear all" emits empty Sets and is hidden when no filters active.

**`ProductCard.test.tsx`**:
- Renders allergen chip with the right i18n label.
- Renders dietary chip with the right i18n label.
- No chips row when both arrays empty.

No new MSW handlers needed — filtering is client-side.

## Out of scope
- Persisting filters across sessions / URL sync — not in AC.
- Backend-side filtering — catalogs are small and already loaded.
- "May contain traces" warnings — not in domain model.
- Combo component-level filtering — combos already carry aggregated allergens.

## Verification
1. `pnpm --dir src/frontend lint`
2. `pnpm --dir src/frontend typecheck`
3. `pnpm --dir src/frontend test`
