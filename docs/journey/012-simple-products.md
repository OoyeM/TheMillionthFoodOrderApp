# 012 — US-FP-005: Simple Product Management

**Date:** 2026-03-16

---

## What Was Built

Full-stack CRUD for simple products — the first entity in the Product domain stream (Layer 1, Stream A). Brand Admins can create, edit, list, and soft-delete products with multilingual names/descriptions and a base price.

## Key Design Decisions

### Translations as a Child Entity (not JSON)

Product names and descriptions support NL, FR, and DE. We chose a `ProductTranslations` table with a composite unique index `(ProductId, LanguageCode)` rather than a JSON column because:

- SQL-level querying and filtering by language (needed for the storefront later)
- Referential integrity enforced at the DB level
- Pattern reusable for categories, modifiers, combos, and any future multilingual entity
- Three rows per product is trivial cost

On **update**, the domain method clears the translation collection and re-adds all translations. This avoids EF Core orphan tracking complexity — simpler and idempotent.

### ISoftDeletable Interface + Global Query Filter

This is the first soft-deletable entity in the codebase. We introduced:

- `ISoftDeletable` marker interface (`IsDeleted`, `DeletedAt`) in `Domain/Common/`
- A global query filter on `BrandDbContext`: `HasQueryFilter(p => !p.IsDeleted)`
- Soft-deleted products are excluded from all queries by default
- Use `IgnoreQueryFilters()` only when historical data is needed (e.g., order history)

This pattern is ready to be reused by categories, modifiers, and any entity that needs soft-delete.

### Money Value Object

`Money` encapsulates `Amount` (decimal) and `Currency` (string, default "EUR"). Stored as an EF owned entity with explicit column names (`BasePrice_Amount`, `BasePrice_Currency`). Prevents primitive obsession and future-proofs for multi-currency.

### Image Upload Deferred

The acceptance criteria mention "image upload" but actual file storage (Azure Blob) is infrastructure that can be added later. For now, `ImageUrl` is a nullable string. The frontend form accepts a URL input with a preview.

## Architecture

### Backend (Clean Architecture layers)

| Layer | Files |
|-------|-------|
| **Domain** | `Product` aggregate root, `ProductTranslation` child entity, `Money` value object, `ISoftDeletable` interface, domain events |
| **Infrastructure** | EF Core configurations, `ProductRepository`, migration (`AddProducts`), seeder with 5 Belgian fries products |
| **Application** | `ProductService`, DTOs (separate list/detail responses to avoid over-fetching) |
| **API** | 5 FastEndpoints: `POST/GET/GET-list/PUT/DELETE` under `/api/brands/{brandSlug}/products/` |

### Frontend (React + TypeScript)

| Area | Files |
|------|-------|
| **API Client** | `products.ts` — axios CRUD |
| **Hooks** | `useProducts.ts` — TanStack Query with key factory |
| **Pages** | `ProductList` (table), `ProductCreate` (form + tabs), `ProductEdit` (form + tabs + delete) |
| **UX** | Translation tabs (NL/FR/DE) — NL required, others optional. Price input, image URL with preview |

## What This Unblocks

US-FP-005 is on the critical path. It now unblocks:
- US-FP-006 (Modifier groups)
- US-FP-007 (Combo products)
- US-FP-008 (Allergen/dietary info)
- US-FP-014 (Menu categories)
- US-FP-030 (Catalog translations)
- US-FP-009/010/011 (Shop product management)

## Lessons Learned

1. **EF Core child collections on Update require care.** The clear-and-re-add pattern is simpler than trying to diff translations. EF Core tracks the removals and additions automatically when the parent is loaded with `Include()`.

2. **Global query filters are powerful but need testing.** The soft-delete filter applies to all queries on `Product` — verify that `Include()` on translations still works correctly and that existing entities (BrandSettings, Shops) are unaffected.

3. **Parallel agent execution hit permission issues.** Background agents couldn't get Write tool permissions. For future sessions: implement directly in the main thread for reliability, or ensure permissions are pre-granted.
