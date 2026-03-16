# 014 — US-FP-015: Order Products Within Categories

**Date:** 2026-03-16

---

## What Was Built

Product display ordering within menu categories — Brand Admins can control the sequence products appear in each category. Two new API endpoints (`GET` list products by category, `PUT` reorder), a `SortOrderInCategory` field on Product, and an admin UI section on the category edit page with move up/down buttons. Newly assigned products auto-append to the end of the category.

## Key Design Decisions

### SortOrderInCategory on Product (not a join table)

`Product.SortOrderInCategory` is an `int` field on the Product entity itself, not on a separate join table, because:

- Products already have a nullable FK `MenuCategoryId` (from US-FP-014)
- The sort order is scoped to a single category (a product belongs to at most one)
- Adding a field is simpler than introducing a join table for a 1:N relationship
- Composite index `(MenuCategoryId, SortOrderInCategory)` enables efficient sorted queries

### Reorder Endpoint Accepts Full Ordered List

`PUT .../products/order` takes a complete list of product IDs rather than pairwise swap or position-per-item:

- Consistent with the existing category reorder pattern (assigns 0..n-1 sequentially)
- Last-write-wins for MVP — no optimistic concurrency needed
- Frontend sends the full list after local move operations; one round-trip per save

### Auto-Append on Category Assignment

`AssignProductCategoryAsync` queries `GetMaxSortOrderInCategoryAsync` and assigns `max + 1`. This has a known race condition (concurrent assigns can produce duplicate positions), documented with a TODO. Acceptable for MVP since the reorder endpoint can fix any duplicates.

### Validation: Products Must Belong to Category

The reorder endpoint validates:
1. All submitted product IDs exist in the database (404 if any missing)
2. All products belong to the specified category (400 if any don't)
3. No duplicate IDs in the request (400 via FluentValidation)

## Architecture

### Backend

| Layer | Changes |
|-------|---------|
| **Domain** | `Product.SortOrderInCategory`, `ReorderInCategory()`, updated `AssignCategory(id, sortOrder)`, `RemoveCategory()` resets to 0 |
| **Infrastructure** | `ProductConfiguration` maps column + composite index, `ProductRepository` adds `GetByCategoryAsync`, `GetMaxSortOrderInCategoryAsync`, `GetByIdsAsync`, `UpdateScalarAsync` |
| **Application** | `MenuCategoryService` adds `GetCategoryProductsAsync`, `ReorderProductsInCategoryAsync`, updates `AssignProductCategoryAsync` for auto-append |
| **API** | `ListCategoryProductsEndpoint` (`GET .../products`), `ReorderCategoryProductsEndpoint` (`PUT .../products/order`) |

### Frontend

| Area | Changes |
|------|---------|
| **API Client** | `listProducts`, `reorderProducts` in `menuCategories.ts` |
| **Hooks** | `useCategoryProducts`, `useReorderCategoryProducts` with query key `['menuCategories', brandSlug, id, 'products']` |
| **UI** | Products section in `MenuCategoryEdit.tsx` — sorted table with move up/down buttons, "Save Order" button (appears when dirty), loading/error/empty states |
| **i18n** | `admin.menuCategories.productsSection.*` keys in NL, FR, DE |

## Testing

- **14 new unit tests** — `ProductSortOrderTests`: `ReorderInCategory`, `AssignCategory` with sort order, `RemoveCategory` resets, default values
- **11 new integration tests** — `CategoryProductOrderTests`: sorted retrieval, auto-append, reorder persistence, 404/400 error cases, duplicate/wrong-category validation
- All 108 tests green (50 unit + 58 integration), zero regressions

## Code Review Findings Addressed

- **Silent skip on missing product IDs** — added count validation to reject requests where any submitted ID wasn't found in the DB
- **Race condition on auto-append** — documented with TODO comment, acceptable for MVP
- **Unreachable null guard in validator** — removed

## What This Unblocks

- US-FP-016 (Place an online order) — menu now has both categories and ordered products for storefront display
- Storefront menu rendering can query products by category in the correct display order

## Lessons Learned

1. **Code review caught a real data integrity bug.** The reorder service silently skipped product IDs not found in the database, meaning a request with a typo'd GUID would succeed but produce an incomplete reorder. Always validate that all input IDs resolve to actual entities.

2. **`UpdateScalarAsync` pattern is essential for non-translation mutations.** Both `MenuCategoryRepository` and now `ProductRepository` have this method. It avoids the transaction overhead of the translation-aware `UpdateAsync`. Any new repository that follows the translation pattern should include both methods from the start.

3. **The sort ordering convention (ordered ID list → 0..n-1) is now proven across two use cases.** Categories and products-within-categories both use the same approach. This is the canonical pattern for any future orderable entity.
