# Plan: US-FP-015 — Order Products Within Categories

**Branch:** `feat/us-fp-015-order-products-in-categories`
**Depends on:** US-FP-014 (Define menu categories) ✅

## Goal

Allow Brand Admins to control the display order of products within a menu category. Products get a `SortOrderInCategory` field; a new endpoint accepts an ordered list of product IDs and assigns sequential positions. Newly assigned products default to the end.

## Acceptance Criteria

- [x] Brand Admin can reorder products within a category via position number (ordered list endpoint)
- [x] The configured order is reflected on the customer-facing storefront (list-products-by-category endpoint returns sorted)
- [x] Newly added products default to the end of the category

---

## Phase 1: Backend — Domain & Infrastructure

### 1.1 Domain: Add `SortOrderInCategory` to Product

**File:** `src/backend/TheMillionthFoodOrderApp.Domain/Products/Product.cs`

- Add `public int SortOrderInCategory { get; private set; }` (default 0)
- Update `AssignCategory(Guid menuCategoryId, int sortOrder)` — accept sort order, set both fields
- Update `RemoveCategory()` — reset `SortOrderInCategory = 0`
- Add `ReorderInCategory(int sortOrder)` — sets `SortOrderInCategory` and `UpdatedAt`

### 1.2 Infrastructure: EF Core Configuration

**File:** `src/backend/TheMillionthFoodOrderApp.Infrastructure/Products/ProductConfiguration.cs`

- Map `SortOrderInCategory` as required int, default 0
- Add composite index: `(MenuCategoryId, SortOrderInCategory)` for efficient sorted queries

### 1.3 Repository: Add query methods

**File:** `src/backend/TheMillionthFoodOrderApp.Domain/Products/IProductRepository.cs`

- Add `Task<IReadOnlyList<Product>> GetByCategoryAsync(Guid categoryId, CancellationToken ct)` — returns products ordered by `SortOrderInCategory`
- Add `Task<int> GetMaxSortOrderInCategoryAsync(Guid categoryId, CancellationToken ct)` — for appending
- Add `Task<IReadOnlyList<Product>> GetByIdsAsync(IEnumerable<Guid> ids, CancellationToken ct)` — for bulk reorder
- Add `UpdateScalarAsync(Guid id, Action<Product> mutate, CancellationToken ct)` — like MenuCategoryRepository pattern, for non-translation mutations

**File:** `src/backend/TheMillionthFoodOrderApp.Infrastructure/Products/ProductRepository.cs`

- Implement all new methods

### 1.4 EF Core Migration

```bash
dotnet ef migrations add AddProductSortOrderInCategory \
  --project TheMillionthFoodOrderApp.Infrastructure \
  --startup-project TheMillionthFoodOrderApp.Api \
  --context BrandDbContext \
  --output-dir Persistence/Migrations/Brand
```

---

## Phase 2: Backend — Application & API

### 2.1 Application Layer DTOs

**File:** `src/backend/TheMillionthFoodOrderApp.Application/Products/ProductDtos.cs`

- Add `SortOrderInCategory` to `ProductResponse` and `ProductListItemResponse`

**File:** `src/backend/TheMillionthFoodOrderApp.Application/MenuCategories/MenuCategoryDtos.cs`

- Add `ReorderProductsInCategoryRequest(IReadOnlyList<Guid> ProductIds)` — ordered list of product IDs

### 2.2 Application Layer Service

**File:** `src/backend/TheMillionthFoodOrderApp.Application/MenuCategories/IMenuCategoryService.cs`

- Add `Task<IReadOnlyList<ProductListItemResponse>> GetCategoryProductsAsync(Guid categoryId, CancellationToken ct)`
- Add `Task ReorderProductsInCategoryAsync(Guid categoryId, ReorderProductsInCategoryRequest request, CancellationToken ct)`

**File:** `src/backend/TheMillionthFoodOrderApp.Application/MenuCategories/MenuCategoryService.cs`

- Implement `GetCategoryProductsAsync` — delegates to `productRepository.GetByCategoryAsync`
- Implement `ReorderProductsInCategoryAsync`:
  1. Verify category exists
  2. Load products by IDs
  3. Validate all products belong to this category
  4. Assign `SortOrderInCategory = index` for each product (0..n-1)
  5. Save all changes
- Update `AssignProductCategoryAsync` — auto-assign `SortOrderInCategory = max + 1` so new products go to the end

### 2.3 API Endpoints

**New file:** `src/backend/TheMillionthFoodOrderApp.Api/Endpoints/MenuCategories/ListCategoryProductsEndpoint.cs`

- `GET /api/brands/{brandSlug}/menu-categories/{id}/products`
- Returns `IReadOnlyList<ProductListItemResponse>` sorted by `SortOrderInCategory`
- Used by both admin (reorder UI) and storefront (display)

**New file:** `src/backend/TheMillionthFoodOrderApp.Api/Endpoints/MenuCategories/ReorderCategoryProductsEndpoint.cs`

- `PUT /api/brands/{brandSlug}/menu-categories/{id}/products/order`
- Body: `{ productIds: ["guid1", "guid2", ...] }` — full ordered list
- Assigns 0..n-1 sequentially (last-write-wins per CLAUDE.md convention)
- Returns 204 No Content

---

## Phase 3: Frontend

### 3.1 API Client

**File:** `src/frontend/src/api/menuCategories.ts`

- Add `listProducts(brandSlug, categoryId)` → `GET .../menu-categories/{id}/products`
- Add `reorderProducts(brandSlug, categoryId, productIds)` → `PUT .../menu-categories/{id}/products/order`

### 3.2 Types

**File:** `src/frontend/src/types/common.ts`

- Add `sortOrderInCategory` to Product types (if not already present)

### 3.3 TanStack Query Hooks

**File:** `src/frontend/src/features/admin/hooks/useMenuCategories.ts`

- Add `menuCategoryKeys.products(brandSlug, id)` query key
- Add `useCategoryProducts(brandSlug, categoryId)` query hook
- Add `useReorderCategoryProducts(brandSlug, categoryId)` mutation hook (invalidates products query)

### 3.4 Admin UI — Product Reorder in Category Edit

**File:** `src/frontend/src/features/admin/pages/MenuCategoryEdit.tsx`

- Add a "Products" section showing products in the category, sorted by `sortOrderInCategory`
- Move up / move down buttons for each product row
- "Save Order" button triggers the reorder mutation
- Alternatively: simple number input per product for position

---

## Phase 4: Tests

### 4.1 Unit Tests

**New file:** `src/backend/TheMillionthFoodOrderApp.Tests.Unit/Products/ProductSortOrderTests.cs`

- Test `ReorderInCategory` sets correct values
- Test `AssignCategory` with sort order
- Test `RemoveCategory` resets sort order

### 4.2 Integration Tests

**New file:** `src/backend/TheMillionthFoodOrderApp.Tests.Integration/MenuCategories/CategoryProductOrderTests.cs`

- Test listing products by category returns sorted order
- Test reorder endpoint persists new order
- Test assigning product to category defaults to end
- Test reorder validates products belong to category

---

## File Summary

| Action | File |
|--------|------|
| Modify | `Domain/Products/Product.cs` |
| Modify | `Domain/Products/IProductRepository.cs` |
| Modify | `Infrastructure/Products/ProductConfiguration.cs` |
| Modify | `Infrastructure/Products/ProductRepository.cs` |
| Modify | `Application/Products/ProductDtos.cs` |
| Modify | `Application/Products/ProductService.cs` |
| Modify | `Application/MenuCategories/MenuCategoryDtos.cs` |
| Modify | `Application/MenuCategories/IMenuCategoryService.cs` |
| Modify | `Application/MenuCategories/MenuCategoryService.cs` |
| Create | `Api/Endpoints/MenuCategories/ListCategoryProductsEndpoint.cs` |
| Create | `Api/Endpoints/MenuCategories/ReorderCategoryProductsEndpoint.cs` |
| Create | EF Core migration (auto-generated) |
| Modify | `frontend/src/api/menuCategories.ts` |
| Modify | `frontend/src/types/common.ts` |
| Modify | `frontend/src/features/admin/hooks/useMenuCategories.ts` |
| Modify | `frontend/src/features/admin/pages/MenuCategoryEdit.tsx` |
| Create | `Tests.Unit/Products/ProductSortOrderTests.cs` |
| Create | `Tests.Integration/MenuCategories/CategoryProductOrderTests.cs` |

## Risks & Notes

- The `ProductRepository.UpdateAsync` method uses transactions to handle translations. For `ReorderInCategory`, we only need scalar updates — use `UpdateScalarAsync` (no translation involved).
- Bulk reorder: load all products by IDs, set sort order in-memory, save in a single `SaveChangesAsync` call. No need for individual `UpdateScalarAsync` calls.
- Last-write-wins for MVP (per CLAUDE.md sort ordering convention).
