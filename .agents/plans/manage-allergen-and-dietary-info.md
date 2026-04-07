# US-FP-008: Manage Allergen and Dietary Information

## Context

Belgian food regulations (EU Regulation 1169/2011) require restaurants to display the 14 EU allergens on all food products. This story adds allergen and dietary tag support to the Product aggregate, exposes it through the API, and provides admin UI for managing tags and storefront display for customers.

**Prerequisites:** US-FP-005 (Simple Products) — completed.
**Blocks:** US-FP-064 (Browse menu with allergen/dietary filters).

## Approach

Store allergens and dietary tags as **two enum collections** on the Product entity, mapped by EF Core 8's primitive collection support (JSON columns in SQL Server). This avoids join table complexity for small fixed-size sets while still supporting LINQ querying for future filtering (US-FP-064).

**Key decision:** Use `IReadOnlyList<string>` in DTOs (not int) — send enum names as strings for API readability. The domain uses typed enums internally. This matches how the frontend already handles `StaffAuthMethod` (string values in the API, enum type internally).

Actually, looking at the existing codebase more carefully: `StaffRole` uses numeric values in the API (see `StaffRoleValue` in common.ts and the `role: number` in `StaffMember`). So we should **use int values** in the API to stay consistent with the existing pattern, and map to/from enums in the service layer.

## Implementation Phases

### Phase 1: Backend Domain

**Files to create:**
- `src/backend/TheMillionthFoodOrderApp.Domain/Products/Allergen.cs` — enum with 14 values (0-13)
- `src/backend/TheMillionthFoodOrderApp.Domain/Products/DietaryTag.cs` — enum with 4 values (0-3)

**Files to modify:**
- `src/backend/TheMillionthFoodOrderApp.Domain/Products/Product.cs`
  - Add `List<Allergen> _allergens` backing field + `IReadOnlyCollection<Allergen> Allergens` property
  - Add `List<DietaryTag> _dietaryTags` backing field + `IReadOnlyCollection<DietaryTag> DietaryTags` property
  - Update `Create()` factory: add optional `IEnumerable<Allergen>? allergens = null, IEnumerable<DietaryTag>? dietaryTags = null` params
  - Update `Update()` method: same params, clear+re-add pattern (like translations)

**Pattern to follow:** Same as `_translations` / `Translations` — private backing list, public read-only accessor.

### Phase 2: Backend Infrastructure

**Files to modify:**
- `src/backend/TheMillionthFoodOrderApp.Infrastructure/Products/ProductConfiguration.cs`
  - Add primitive collection mapping for `Allergens` and `DietaryTags`
  - EF Core 8+ stores these as JSON columns automatically
  - May need `.HasField("_allergens")` if convention doesn't pick up backing fields

**Migration command:**
```bash
cd src/backend && dotnet ef migrations add AddAllergenAndDietaryTags --project TheMillionthFoodOrderApp.Infrastructure --startup-project TheMillionthFoodOrderApp.Api --context BrandDbContext --output-dir Persistence/Migrations/Brand
```

Expected result: Two `nvarchar(max)` JSON columns on `Products` table, defaulting to `[]`.

### Phase 3: Backend Application

**Files to modify:**
- `src/backend/TheMillionthFoodOrderApp.Application/Products/ProductDtos.cs`
  - Add `IReadOnlyList<int>? Allergens = null` and `IReadOnlyList<int>? DietaryTags = null` to `CreateProductRequest` and `UpdateProductRequest`
  - Add `IReadOnlyList<int> Allergens` and `IReadOnlyList<int> DietaryTags` to `ProductResponse` and `ProductListItemResponse`

- `src/backend/TheMillionthFoodOrderApp.Application/Products/ProductService.cs`
  - `CreateProductAsync`: cast `request.Allergens` int list to `Allergen` enum, pass to `Product.Create()`
  - `UpdateProductAsync`: same casting, pass to `Product.Update()` via the lambda
  - `MapToResponse`: add `.Allergens.Select(a => (int)a)` and `.DietaryTags.Select(d => (int)d)`
  - `MapToListItem`: same additions

### Phase 4: Backend API

**Files to modify:**
- `src/backend/TheMillionthFoodOrderApp.Api/Endpoints/Products/CreateProductEndpoint.cs`
  - Add `List<int>? Allergens` and `List<int>? DietaryTags` to `CreateProductApiRequest` record
  - Add validation in `CreateProductRequestValidator`:
    - Each allergen value must be 0-13
    - Each dietary tag value must be 0-3
    - No duplicates in either list
  - Update `HandleAsync` mapping to pass allergens/dietary tags to `CreateProductRequest`

- `src/backend/TheMillionthFoodOrderApp.Api/Endpoints/Products/UpdateProductEndpoint.cs`
  - Same changes as CreateProductEndpoint

### Phase 5: Frontend

**Files to modify:**
- `src/frontend/src/types/common.ts`
  - Add `Allergen` const object + type (values 0-13, keys matching enum names)
  - Add `DietaryTag` const object + type (values 0-3)
  - Add `ALLERGEN_KEYS` and `DIETARY_TAG_KEYS` helper arrays
  - Add `allergens: number[]` and `dietaryTags: number[]` to `Product` and `ProductListItem` interfaces

- `src/frontend/src/api/products.ts`
  - Add `allergens?: number[]` and `dietaryTags?: number[]` to `CreateProductRequest` and `UpdateProductRequest`

- `src/frontend/src/features/admin/pages/ProductCreate.tsx`
  - Add `selectedAllergens` and `selectedDietaryTags` state (Set<number>)
  - Add checkbox grid UI for allergens (14 checkboxes) between Image URL and Translations
  - Add checkbox grid UI for dietary tags (4 checkboxes) after allergens
  - Include in mutation data: `allergens: [...selectedAllergens], dietaryTags: [...selectedDietaryTags]`
  - Add `useTranslation` import for i18n labels

- `src/frontend/src/features/admin/pages/ProductEdit.tsx`
  - Same checkbox UI as ProductCreate
  - Populate from loaded product in the `useEffect` that initializes the form
  - Include in update mutation data

- `src/frontend/src/i18n/locales/nl/common.json` — add allergen names (NL), dietary tag names, and admin labels
- `src/frontend/src/i18n/locales/fr/common.json` — add allergen names (FR), dietary tag names, and admin labels
- `src/frontend/src/i18n/locales/de/common.json` — add allergen names (DE), dietary tag names, and admin labels

### Phase 6: Storefront Display

The storefront currently has no product detail page. The API responses will include allergen/dietary data. For now, **defer storefront display** — the data flows end-to-end and will be available when a product detail page is built. This is acceptable because US-FP-064 (Browse with allergen filters) is a separate story that depends on this one.

## Verification

```bash
# Backend build
cd src/backend && dotnet build TheMillionthFoodOrderApp.slnx

# Backend tests
cd src/backend && dotnet test

# Frontend build
cd src/frontend && pnpm build

# Frontend lint
cd src/frontend && pnpm lint
```

## Acceptance Criteria Mapping

| AC | Implementation |
|---|---|
| 14 EU allergens available as tags | `Allergen` enum with all 14 values |
| Dietary tags available (vegan, vegetarian, gluten-free, halal) | `DietaryTag` enum with 4 values |
| Brand Admin can assign tags to brand products | Create/Update endpoints accept allergen/dietary arrays; admin form checkboxes |
| Allergen/dietary info displayed on storefront | API responses include data; storefront page deferred (no product detail page exists yet) |
| Shop Managers can assign to shop-level custom products | Same endpoints — products are brand-scoped, both roles have access |

## Confidence: 9/10

Low risk — small, well-scoped domain change following established patterns. The only uncertainty is EF Core primitive collection mapping for enum lists (may need explicit backing field config).
