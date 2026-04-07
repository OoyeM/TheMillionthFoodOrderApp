# US-FP-030: Provide Translations for Product Catalog — Implementation Plan

## Current State

The translation infrastructure is **already fully implemented**:
- Domain entities (`Product`, `MenuCategory`, `ModifierGroup`, `Modifier`) all have child translation entities
- EF Core configs enforce `(ParentId, LanguageCode)` unique indexes
- API endpoints accept/return translation arrays with `languageCode`, `name`, `description`
- Validators restrict to `"nl" | "fr" | "de"`, require at least one translation
- Frontend admin UI has tabbed NL/FR/DE input (NL required, FR/DE optional)
- `BrandSettings.DefaultLanguage` exists (defaults to `"nl-BE"`)
- Seed data includes NL + FR translations (DE partial)

**No database migrations needed.**

## Gaps to Close

### Gap 1: Primary Language Fallback in List Endpoints
List-item mappers use `Translations.FirstOrDefault()?.Name` (insertion order) instead of resolving by brand's primary language.

**Fix:** Create a `TranslationResolver` helper and wire it into list-item mappers.

**Files:**
- `src/backend/TheMillionthFoodOrderApp.Application/Common/TranslationResolver.cs` (new)
- `src/backend/TheMillionthFoodOrderApp.Application/Products/ProductService.cs`
- `src/backend/TheMillionthFoodOrderApp.Application/MenuCategories/MenuCategoryService.cs`
- `src/backend/TheMillionthFoodOrderApp.Application/ModifierGroups/ModifierGroupService.cs`

### Gap 2: Primary Language Required Validation
Backend only checks "at least one translation" — doesn't enforce that the brand's primary language is included.

**Fix:** Add validation in service Create/Update methods that the primary language translation is present.

**Files:** Same service files as Gap 1.

### Gap 3: DE Translations in Seed Data
Products and categories only seeded with NL + FR. DE is missing.

**Fix:** Add DE translations to seed data.

**File:** `src/backend/TheMillionthFoodOrderApp.Infrastructure/Persistence/Seeding/BrandDbSeeder.cs`

### Gap 4: Frontend Dynamic Primary Language
Frontend hardcodes NL as required tab. Should read from brand settings.

**Files:**
- `src/frontend/src/features/admin/pages/ProductCreate.tsx`
- `src/frontend/src/features/admin/pages/ProductEdit.tsx`
- Equivalent category/modifier group create/edit pages

## Implementation Steps

1. **TranslationResolver helper** — static helper that resolves display name from translations collection given a preferred language, with fallback chain: preferred → primary → first → "(unnamed)"
2. **Wire into list-item mappers** — inject `IBrandSettingsRepository` into services, use resolver for list responses
3. **Primary-language-required validation** — in service Create/Update, validate primary language is present; throw validation exception if not
4. **Update seed data** — add DE translations to products and categories
5. **Update tests** — unit tests for resolver, integration tests for validation and fallback
6. **Frontend dynamic required language** — read brand's `defaultLanguage` from settings API, use it to determine which tab is required

## Testing

- Unit: `TranslationResolver` fallback logic
- Integration: create product without primary language → 400
- Integration: list endpoint returns name in primary language
- Frontend: required tab matches brand settings

## Risk

Low — core infrastructure exists, changes are incremental, no migrations needed, backward compatible.
