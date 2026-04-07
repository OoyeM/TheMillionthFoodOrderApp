# 016 — US-FP-008: Manage Allergen and Dietary Information

**Date:** 2026-04-07

---

## What Was Built

Allergen and dietary tag management for products — Brand Admins can tag products with the 14 EU-regulated allergens and 4 dietary labels (vegan, vegetarian, gluten-free, halal). Full-stack: domain enums, API endpoints with validation, admin form UI with checkbox grids, and i18n support in NL/FR/DE.

## Key Design Decisions

### Enums + EF Core Primitive Collections (not join tables)

Allergens and dietary tags are stored as `List<Allergen>` and `List<DietaryTag>` on the Product entity, mapped by EF Core 8's primitive collection support as JSON columns (`nvarchar(max)`) in SQL Server.

- **Why not join tables?** The sets are small and fixed (14 allergens, 4 dietary tags). JSON columns avoid the overhead of extra tables, migrations, and FK management for what is essentially a set of flags.
- **Why not a [Flags] bitmask?** JSON arrays are human-readable in the database, queryable via LINQ (for future US-FP-064 allergen filtering), and don't break when new values are added in between existing ones.
- **Why not strings?** Typed enums catch invalid values at compile time. The domain validates with `Enum.IsDefined` at runtime for defense-in-depth against invalid casts.

### Int Values in API (not string names)

The API sends allergen/dietary tag values as integers matching the backend enum values (e.g., `0` = Gluten, `1` = Crustaceans). This follows the existing pattern used by `StaffRole` in the codebase. The frontend maps these via const objects with named keys for readability.

### Domain-Level Enum Validation

`Product.Create` and `Product.Update` validate every allergen/dietary tag value with `Enum.IsDefined`, throwing `ArgumentException` for invalid values. This protects against invalid integer casts (e.g., `(Allergen)999`) that C# allows silently. API validators also check using `Enum.IsDefined` rather than hardcoded ranges, so they stay correct when enums are extended.

### Storefront Display Deferred

The storefront has no product detail page yet. API responses include allergen/dietary data, so it will be available when a product detail page is built (US-FP-064 depends on this story).

## What Went Well

- Clean vertical slice: domain → infrastructure → application → API → frontend in one pass
- EF Core primitive collections worked with minimal configuration (just `HasField` + `HasColumnType`)
- Existing product form pattern (translations, modifier groups) made the frontend changes straightforward
- Review caught important issues: missing domain validation, hardcoded magic numbers, and i18n gaps

## Lessons Learned

- **Always validate enum casts at the domain boundary.** C# allows casting any integer to an enum without error. API validators are a first line of defense, but domain guards are essential for internal callers.
- **Use `Enum.IsDefined` in validators instead of hardcoded ranges.** `InclusiveBetween(0, 13)` silently breaks when new enum values are added. `Enum.IsDefined` stays correct automatically.
- **i18n from day one means all strings, not just new ones.** The product forms had pre-existing hardcoded English strings. Adding i18n for allergen labels made the inconsistency visible, so we fixed it for all product form strings.

## Files Changed

### New
- `Domain/Products/Allergen.cs` — 14 EU allergens enum
- `Domain/Products/DietaryTag.cs` — 4 dietary tags enum

### Modified (Backend)
- `Domain/Products/Product.cs` — allergen/dietary collections, Create/Update params, validation helpers
- `Infrastructure/Products/ProductConfiguration.cs` — primitive collection mapping with explicit `nvarchar(max)`
- `Application/Products/ProductDtos.cs` — allergen/dietary fields on all request/response records
- `Application/Products/ProductService.cs` — enum casting and mapping
- `Api/Endpoints/Products/CreateProductEndpoint.cs` — request model, validator, handler
- `Api/Endpoints/Products/UpdateProductEndpoint.cs` — same changes

### Modified (Frontend)
- `types/common.ts` — Allergen/DietaryTag const objects, ALLERGEN_KEYS/DIETARY_TAG_KEYS, Product interface
- `api/products.ts` — allergens/dietaryTags on request interfaces
- `features/admin/pages/ProductCreate.tsx` — checkbox UI, i18n for all strings
- `features/admin/pages/ProductEdit.tsx` — checkbox UI, populate from product, i18n for all strings
- `i18n/locales/{nl,fr,de}/common.json` — allergen names, dietary tag names, product form labels

### Tests
- `Tests.Unit/Products/ProductTests.cs` — 11 new tests for allergen/dietary tag behavior
