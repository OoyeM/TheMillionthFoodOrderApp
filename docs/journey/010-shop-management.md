# 010 — US-FP-002: Shop Management (Full Stack)

**Date:** 2026-03-15

## What happened

Implemented the second user story end-to-end: Brand Admins can create, edit, activate, and deactivate shops within their brand. This is the **first brand-scoped entity** — shops are stored in the brand's isolated database (`BrandDbContext`), not the platform database.

## Backend

### Domain layer
- `Shop` aggregate root with factory method (`Create`), `UpdateMetadata`, `Deactivate`, `Activate`
- `Address` value object (Street, Number, City, PostalCode, Country — Belgian address structure)
- Domain events: `ShopCreatedEvent` (future hook for product catalog inheritance), `ShopDeactivatedEvent`
- `IShopRepository` interface — no `SaveChangesAsync` (brand-scoped repos use per-method unit-of-work)

### Application layer
- `ShopService` with full CRUD + activate/deactivate
- DTOs: `CreateShopRequest`, `UpdateShopRequest`, `AddressRequest`, `AddressResponse`, `ShopResponse`

### Infrastructure layer
- `ShopConfiguration` — Address as EF Core owned entity, unique index on Slug
- `ShopRepository` — **new pattern**: per-method unit-of-work via `BrandDbContextFactory`. Each method creates its own `await using BrandDbContext`. Mutations use `UpdateAsync(Guid id, Action<Shop> mutate)` to load-mutate-save in one context.
- `AuditSaveChangesInterceptor` wired to `BrandDbContextFactory.CreateDbContext()` — ensures audit fields are set for all brand-scoped entities
- `BrandDbSeeder` updated with 3 sample shops for "Frietjes?" (Bruxelles, Antwerpen, Gent)

### API layer (FastEndpoints)
| Endpoint | Verb | Route |
|----------|------|-------|
| CreateShop | POST | `/api/brands/{brandSlug}/shops` |
| UpdateShop | PUT | `/api/brands/{brandSlug}/shops/{id}` |
| GetShop | GET | `/api/brands/{brandSlug}/shops/{id}` |
| ListShops | GET | `/api/brands/{brandSlug}/shops` |
| DeactivateShop | POST | `/api/brands/{brandSlug}/shops/{id}/deactivate` |
| ActivateShop | POST | `/api/brands/{brandSlug}/shops/{id}/activate` |

All endpoints use `{brandSlug}` in the route, which triggers `BrandContextMiddleware` to set the brand context before the endpoint executes.

## Frontend

- `shopsApi` — API client with all CRUD + activate/deactivate operations
- TanStack Query hooks: `useShops`, `useShop`, `useCreateShop`, `useUpdateShop`, `useDeactivateShop`, `useActivateShop`
- Admin pages: `ShopList` (table with status toggle), `ShopCreate` (form with slug auto-derivation), `ShopEdit` (pre-populated form, slug read-only)
- Dashboard updated with navigation cards for Brands and Shops
- Routes added under `shops/*`

## Key decisions

1. **Brand-scoped repository pattern established.** Each method owns its unit-of-work (`await using var db = factory.CreateDbContext()`). This is the canonical pattern for all future brand-scoped entities.
2. **No `SaveChangesAsync` on brand-scoped repos.** Unlike `BrandRepository` (platform-scoped, DI-registered context), brand-scoped repos save atomically within each method. Exposing `SaveChangesAsync` would be a misleading no-op.
3. **`UpdateAsync` delegate pattern.** Because two `CreateDbContext()` calls produce independent contexts, the standard get-then-save pattern doesn't work. `UpdateAsync(id, shop => shop.UpdateMetadata(...))` keeps load-mutate-save in one context.
4. **Product catalog inheritance deferred.** `ShopCreatedEvent` is raised but no handler exists yet — products (US-FP-005) aren't implemented. The event is the hook for future catalog cloning.
5. **Address as ValueObject / EF owned entity.** Maps to columns in the Shops table (Address_Street, etc.), not a separate table. Structured for future geocoding.

## Code review findings (fixed)

- **AuditSaveChangesInterceptor** was not wired to `BrandDbContextFactory` — fixed by adding `.AddInterceptors(new AuditSaveChangesInterceptor())` to the options builder.
- **`SaveChangesAsync` no-op trap** — removed from `IShopRepository` and `ShopRepository` to avoid misleading callers.

## KB updates

- `.claude/skills/backend-dotnet/docs/ef-core.md` — documented the brand-scoped repository pattern with code example and key rules.

## What's next

- EF Core migration for BrandDbContext (first real brand migration with tables)
- Wire up `BrandDbSeeder` call in startup
- US-FP-005: Products (first entity that depends on shops existing)
- Auth on mutation endpoints (currently `AllowAnonymous`, matching Brand pattern)
