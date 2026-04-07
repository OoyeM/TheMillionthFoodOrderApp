# 018 — US-FP-007: Create and Manage Combo Products

**Date:** 2026-04-07

---

## What Was Built

Combo (bundle) products — Brand Admins can create products that group two or more existing simple products at a fixed bundle price. Example: "Small Fry Special" = small fries + special sauce = EUR 3.50. Combos have their own name, description, image, and translations, and can also have modifier groups (e.g., a size variant for the whole combo). The customer experience is "quick select" — pick the combo, not the individual items.

Full-stack implementation: domain model extension, EF Core migration, application services, two new API endpoints, admin UI (create/edit pages with a product picker), and updated product list with type badges.

## Key Design Decisions

### Extending Product Instead of a Separate Aggregate

Combos share every base property with simple products: translations, price, image, categories, modifier groups, soft-delete. Rather than creating a separate `ComboProduct` aggregate (which would duplicate ~80% of the code), we added a `ProductType` discriminator enum (`Simple`/`Combo`) and a `ComboItem` child entity collection to the existing `Product` aggregate.

Benefits:
- Combos appear naturally in product listings alongside simple products
- Existing modifier group and category assignment works unchanged
- Single repository, single service, shared mapping logic
- EF migration is minimal: one column + one table

Trade-off: the Product aggregate now has conditional logic (combo items only populated when `ProductType == Combo`). This is acceptable at the current scale and avoids the maintenance burden of parallel aggregates.

### Dedicated Combo Endpoints (Not Shared)

Create and update use dedicated routes (`POST/PUT /api/brands/{brandSlug}/combo-products`) rather than overloading the existing product endpoints. This keeps validators clean (combo-specific rules like "min 2 components"), makes the API self-documenting, and avoids a polymorphic request body. List, get, and delete reuse the existing product endpoints — the response now includes `productType` and `comboItems` for all products.

### Delete Protection for Component Products

A simple product that is part of an active combo cannot be soft-deleted. The service layer checks `IsComponentOfAnyComboAsync` before deletion, returning 409 Conflict with a clear message. The check joins with the Products table so the global soft-delete query filter excludes deleted combos — if a combo is already deleted, its former components can be freely deleted.

The `ComboItems` FK uses `Restrict` on `ComponentProductId` as a database-level safety net.

### No Nested Combos

The service validates that all component products are `ProductType.Simple`. Combos cannot contain other combos. This avoids recursive pricing complexity and keeps the domain model flat. The constraint is enforced at both the API validator level (input validation) and the application service level (data validation after loading).

## What Changed

### Backend

| Layer | Files | What |
|-------|-------|------|
| Domain | `ProductType.cs`, `ComboItem.cs`, `Product.cs`, `IProductRepository.cs` | Enum, child entity, `CreateCombo()` factory, `UpdateComboItems()` method, `IsComponentOfAnyComboAsync` contract |
| Infrastructure | `ComboItemConfiguration.cs`, `ProductConfiguration.cs`, `ProductRepository.cs`, `BrandDbContext.cs` | EF config (unique index, cascade/restrict FKs), ComboItems includes on all queries, delete/re-add in transaction |
| Application | `ProductDtos.cs`, `IProductService.cs`, `ProductService.cs`, `MenuCategoryService.cs` | Combo DTOs, create/update/validate methods, delete protection, updated response mapping |
| API | `CreateComboProductEndpoint.cs`, `UpdateComboProductEndpoint.cs`, `DeleteProductEndpoint.cs` | Two new endpoints with FluentValidation, 409 handler on delete |
| Migration | `AddComboProducts.cs` | Adds `ProductType` int column (default 0), creates `ComboItems` table |

### Frontend

| Area | Files | What |
|------|-------|------|
| Types | `common.ts` | `ProductType`, `ComboItemResponse`, extended `Product` and `ProductListItem` |
| API | `products.ts` | `createCombo()`, `updateCombo()` functions |
| Hooks | `useProducts.ts` | `useCreateComboProduct`, `useUpdateComboProduct` |
| Pages | `ComboProductCreate.tsx`, `ComboProductEdit.tsx` | Full create/edit forms with product picker, reorder controls, modifier groups |
| List | `ProductList.tsx` | Type column with badge (Simple/Combo), "+ Create Combo" button, type-aware routing |
| Routes | `routes.tsx` | `combo-products/new`, `combo-products/:productId` |
| i18n | `nl/common.json`, `fr/common.json`, `de/common.json` | `admin.comboProducts.*` keys |

## Bugs Caught in Review

1. **UpdateComboProductAsync didn't validate product type** — calling the update-combo endpoint on a simple product would throw `InvalidOperationException` inside the repository transaction after translations were already deleted. Fixed by pre-checking `ProductType == Combo` before entering the transaction.

2. **IsComponentOfAnyComboAsync counted soft-deleted combos** — querying `ComboItems` directly bypassed the Product soft-delete filter, so a component used only in deleted combos was still blocked from deletion. Fixed by joining with `Products` (which applies the global query filter).

3. **MenuCategoryService build break** — `ProductListItemResponse` gained a new `ProductType` parameter, but `MenuCategoryService.MapProductToListItem` wasn't updated. Build caught it immediately once the SDK was available.

4. **FastEndpoints `SendErrorsAsync` pattern** — used `AddError` + `SendErrorsAsync` (non-existent in this version). The codebase uses `new List<ValidationFailure>` + `HttpContext.Response.SendErrorsAsync`. Fixed to match existing pattern.

## Remaining Items

Tracked in `docs/007_review_todo.md`:
- Keyboard accessibility on component picker (medium)
- `aria-label` on reorder buttons (medium)
- Domain-level duplicate component validation (medium)
- Standardize error codes between create/update (low)
- Handle soft-deleted components in combo responses (low)
