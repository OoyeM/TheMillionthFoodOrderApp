# 013 — US-FP-014: Menu Categories

**Date:** 2026-03-16

---

## What Was Built

Full-stack CRUD for menu categories — the organizational layer for products (Layer 1, Stream A). Brand Admins can create, edit, reorder, list, and soft-delete categories with multilingual names (NL, FR, DE) and optional image. Products can be assigned to categories. Shops inherit the brand's category structure automatically (same brand DB).

## Key Design Decisions

### MenuCategory as a Separate Aggregate (not a Product child)

Categories could have been modeled as a value object or child entity of Product, but they are a separate aggregate root because:

- Categories have their own lifecycle (created before products are assigned)
- They need independent CRUD operations and their own API routes
- Reordering is a category-level concern, not a product concern
- Future features (category images, visibility rules, nesting) will expand the entity

### Nullable FK on Product (not a join table)

`Product.MenuCategoryId` is a nullable `Guid?` rather than a many-to-many join table because:

- A product belongs to at most one category (business rule for Frietjes?)
- Uncategorized products are valid (they just don't appear in category-grouped views)
- `SetNull` on category delete — products become uncategorized, not deleted
- If multi-category assignment is needed later, a join table can replace the FK

### SortOrder as Simple Integer

Categories use an `int SortOrder` field rather than fractional positioning or linked lists:

- The reorder endpoint accepts an ordered list of category IDs and assigns 0..n-1 sequentially
- Last-write-wins is acceptable for MVP (small number of categories per brand, typically <20)
- Move up/down buttons in the UI for Phase 1; drag-and-drop deferred to US-FP-015

### Translation Pattern Reuse

`MenuCategoryTranslation` follows the exact same pattern as `ProductTranslation`: child entity with composite unique index on `(MenuCategoryId, LanguageCode)`, clear-and-re-add on update. This confirms the pattern is reusable across all multilingual entities.

## Architecture

### Backend (Clean Architecture layers)

| Layer | Files |
|-------|-------|
| **Domain** | `MenuCategory` aggregate root, `MenuCategoryTranslation` child entity, domain events |
| **Infrastructure** | EF Core configurations, `MenuCategoryRepository`, migration (`AddMenuCategories`), seeder with 4 Belgian fries categories |
| **Application** | `MenuCategoryService`, DTOs (separate list/detail responses) |
| **API** | 7 FastEndpoints: `POST/GET/GET-list/PUT/DELETE` + `PATCH reorder` + `POST assign-product` under `/api/brands/{brandSlug}/menu-categories/` |

### Frontend (React + TypeScript)

| Area | Files |
|------|-------|
| **API Client** | `menuCategories.ts` — axios CRUD + reorder + assign |
| **Hooks** | `useMenuCategories.ts` — TanStack Query with key factory |
| **Pages** | `MenuCategoryList` (table + move up/down), `MenuCategoryCreate` (form + tabs), `MenuCategoryEdit` (form + tabs + delete) |
| **UX** | Translation tabs (NL/FR/DE) — NL required, others optional. Image URL with preview. Move up/down buttons for reorder. |

### Modified Existing Files

- `Product` entity: added `MenuCategoryId` nullable FK + `AssignCategory()`/`RemoveCategory()` methods
- `ProductConfiguration`: added FK relationship with `SetNull` on delete
- `ProductDtos`/`ProductService`: added `MenuCategoryId` to response DTOs
- `BrandDbSeeder`: seeds 4 categories (Frieten, Snacks, Sauzen, Burgers) with NL+FR translations

## Testing

- **14 unit tests** — domain entity behavior (create, update, soft-delete, validation, events, UUIDv7)
- **21 integration tests** — full HTTP round-trip via Testcontainers (CRUD + reorder + assign product + cascade SetNull)
- All 83 tests green (36 unit + 47 integration), zero regressions

## What This Unblocks

- US-FP-015 (Order products within categories) — now has categories to order within
- US-FP-016 (Place an online order) — needs categories for menu display

## Lessons Learned

1. **Parallel backend + frontend implementation works well.** Two agents implemented backend and frontend simultaneously with no conflicts. The API contract (route patterns, DTO shapes) was defined in the plan, so both sides agreed without coordination.

2. **Migration must be created before integration tests pass.** EF Core's `PendingModelChangesWarning` causes all integration tests to fail until a migration exists. Always create the migration before running tests.

3. **The translation + soft-delete pattern is now battle-tested across two entities.** Product and MenuCategory both use the same approach — this is the canonical pattern for any new brand-scoped, multilingual, soft-deletable entity.
