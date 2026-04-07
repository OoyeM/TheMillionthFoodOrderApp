# 017 — US-FP-030: Provide Translations for Product Catalog

**Date:** 2026-04-07

---

## What Was Built

Translation fallback and primary-language validation for the entire product catalog — products, menu categories, and modifier groups. The core translation infrastructure (child entities, EF configs, API endpoints, frontend tabs) already existed from prior stories. This story closed the gaps: correct name resolution in list endpoints, backend enforcement of the brand's primary language, German (DE) seed data, and dynamic required-language detection in the admin UI.

## Key Design Decisions

### TranslationResolver — shared static helper

A generic `TranslationResolver` class in `Application/Common/` handles two responsibilities:
1. **Name resolution** with fallback chain: brand's primary language → first available → "(unnamed)"
2. **Primary-language validation** — throws `InvalidOperationException` if the primary language translation is missing

Using generic `Func<T, string>` selectors avoids coupling to any specific translation entity type. All three services (Product, MenuCategory, ModifierGroup) reuse the same helper.

### Per-instance caching of brand settings

Each service fetches brand settings via `IBrandSettingsRepository.GetAsync()` for validation and name resolution. To avoid repeated DB hits within the same request scope, a `GetPrimaryLanguageAsync()` helper caches the result in a `_cachedPrimaryLanguage` field. Services are scoped (one instance per request), so this is safe.

### Frontend: `extractPrimaryLocale` + useEffect sync

The brand's `DefaultLanguage` is a BCP-47 tag (e.g. `"nl-BE"`). `extractPrimaryLocale()` strips it to a two-letter code and validates it against the supported set, falling back to `'nl'` for safety.

Create pages use a `useRef`-guarded `useEffect` to sync the active translation tab once when brand settings load, since `useState` only uses its initializer on the first render.

### InvalidOperationException for validation

Following the existing codebase pattern (used by brand creation, staff management), services throw `InvalidOperationException` for business rule violations. Endpoints catch this and return 400 with a `ValidationFailure` pointing to the `translations` field.

## What Already Existed (No Changes Needed)

- Domain entities with `List<XTranslation>` child collections
- EF Core composite unique index on `(ParentId, LanguageCode)`
- API endpoints accepting/returning translation arrays
- FluentValidation restricting language codes to `nl | fr | de`
- Frontend tabbed NL/FR/DE input UI
- `BrandSettings.DefaultLanguage` field

## Files Changed

**Backend — new:**
- `Application/Common/TranslationResolver.cs`

**Backend — modified:**
- `ProductService.cs`, `MenuCategoryService.cs`, `ModifierGroupService.cs` — primary-language validation + fallback name resolution
- 6 endpoint files — `InvalidOperationException` → 400 catch blocks
- `BrandDbSeeder.cs` — DE translations for products and categories

**Frontend — modified:**
- `types/common.ts` — `extractPrimaryLocale()` utility
- 6 admin pages — dynamic required language from brand settings

## Lessons Learned

- **Most of the work was already done.** The translation infrastructure was solid from prior stories. This story was about closing gaps (fallback logic, validation, DE data) rather than building from scratch. Always check what exists before planning.
- **`useState` initial values are traps with async data.** When the initial value depends on data that arrives after first render, you need an explicit sync effect — `useState` won't re-initialize.
- **Generic helpers with selectors scale well.** `TranslationResolver` handles Products, Categories, and ModifierGroups without knowing about any of them. The `Func<T, string>` pattern avoids creating adapter interfaces.
