# Implementation Plan: US-FP-002 — Create and Manage Shops Within a Brand

## Overview

Add Shop as the first brand-scoped entity in the BrandDbContext. A Brand Admin can create, edit, list, and deactivate shops belonging to their brand. Shops are stored in the brand's isolated database (database-per-brand). Each shop has a name, address, contact details, a URL-safe slug for customer-facing URLs, and an active/inactive status. This feature follows the exact same layered pattern established by the Brand entity: Domain aggregate, Application service + DTOs, Infrastructure (EF config + repository), API endpoints (FastEndpoints), and frontend admin pages with TanStack Query hooks.

## Requirements (from Acceptance Criteria)

- Brand Admin can create a shop with name, address, and contact details
- Each shop inherits the brand's full product catalog upon creation (deferred — no products exist yet; structure the domain event for future use)
- Brand Admin can edit shop metadata
- Brand Admin can deactivate a shop, hiding it from customers
- Each shop gets a unique customer-facing URL (via slug, unique within brand)
- Shop data is stored in the brand's isolated database (BrandDbContext)

## Key Design Decisions

1. **Shop lives in BrandDbContext, not PlatformDbContext.** First entity in the brand-scoped database, fulfilling database-per-brand isolation.
2. **Shop is an AggregateRoot.** Owns its own lifecycle (create, update, deactivate, activate) and will later own opening hours, stock, orders, etc.
3. **Slug is scoped to brand.** Uniqueness enforced at DB level within the brand database. Customer-facing URL pattern: `/{brandSlug}/{lang}/shops/{shopSlug}`.
4. **Address as a ValueObject.** Belgian addresses have structured components (street, number, city, postal code, country). Using a ValueObject enables validation and future geocoding.
5. **API routes are brand-scoped:** `/api/brands/{brandSlug}/shops/...` — uses the existing `BrandContextMiddleware` to set the brand context, then resolves `BrandDbContext` via `BrandDbContextFactory`.
6. **Product catalog inheritance is a placeholder.** We raise a `ShopCreatedEvent` domain event that a future Wolverine handler can listen to for catalog cloning.

---

## Architecture Changes

### New Files

| Layer | File | Purpose |
|-------|------|---------|
| Domain | `Domain/Shops/Shop.cs` | Shop aggregate root |
| Domain | `Domain/Shops/Address.cs` | Address value object |
| Domain | `Domain/Shops/ShopCreatedEvent.cs` | Domain event for future catalog inheritance |
| Domain | `Domain/Shops/ShopDeactivatedEvent.cs` | Domain event for downstream reactions |
| Domain | `Domain/Shops/IShopRepository.cs` | Repository interface |
| Application | `Application/Shops/ShopDtos.cs` | Request/response DTOs |
| Application | `Application/Shops/IShopService.cs` | Service interface |
| Application | `Application/Shops/ShopService.cs` | Service implementation |
| Infrastructure | `Infrastructure/Shops/ShopConfiguration.cs` | EF Core entity configuration |
| Infrastructure | `Infrastructure/Shops/ShopRepository.cs` | Repository implementation |
| API | `Api/Endpoints/Shops/CreateShopEndpoint.cs` | POST /api/brands/{brandSlug}/shops |
| API | `Api/Endpoints/Shops/UpdateShopEndpoint.cs` | PUT /api/brands/{brandSlug}/shops/{id} |
| API | `Api/Endpoints/Shops/GetShopEndpoint.cs` | GET /api/brands/{brandSlug}/shops/{id} |
| API | `Api/Endpoints/Shops/ListShopsEndpoint.cs` | GET /api/brands/{brandSlug}/shops |
| API | `Api/Endpoints/Shops/DeactivateShopEndpoint.cs` | POST /api/brands/{brandSlug}/shops/{id}/deactivate |
| API | `Api/Endpoints/Shops/ActivateShopEndpoint.cs` | POST /api/brands/{brandSlug}/shops/{id}/activate |
| Frontend | `src/api/shops.ts` | Axios API client for shops |
| Frontend | `src/features/admin/hooks/useShops.ts` | TanStack Query hooks |
| Frontend | `src/features/admin/pages/ShopList.tsx` | Shop list page |
| Frontend | `src/features/admin/pages/ShopCreate.tsx` | Create shop form |
| Frontend | `src/features/admin/pages/ShopEdit.tsx` | Edit shop form |

### Modified Files

| File | Change |
|------|--------|
| `Infrastructure/Persistence/BrandDbContext.cs` | Add `DbSet<Shop>` and apply `ShopConfiguration` |
| `Infrastructure/DependencyInjection.cs` | Register `IShopRepository` and scoped `BrandDbContext` |
| `Application/DependencyInjection.cs` | Register `IShopService` |
| `Infrastructure/Persistence/Seeding/BrandDbSeeder.cs` | Seed sample shops for "Frietjes?" brand |
| `Frontend: src/types/common.ts` | Expand the `Shop` stub interface with all fields |
| `Frontend: src/features/admin/routes.tsx` | Add shop routes |

---

## Implementation Steps

### Phase 1: Domain Layer (4 files)

**1.1 Create Address value object**
- File: `Domain/Shops/Address.cs`
- `Address` extending `ValueObject` with: Street, Number, City, PostalCode, Country (default "BE")

**1.2 Create Shop aggregate root**
- File: `Domain/Shops/Shop.cs`
- `Shop : AggregateRoot<Guid>, IAuditable` following Brand pattern
- Properties: Name, Slug (immutable), Address, ContactEmail, ContactPhone, IsActive, CreatedAt, UpdatedAt
- Factory method `Create(...)` raises `ShopCreatedEvent`
- Methods: `UpdateMetadata(...)`, `Deactivate()`, `Activate()`

**1.3 Create domain events**
- `ShopCreatedEvent(Guid ShopId, string Name, string Slug)`
- `ShopDeactivatedEvent(Guid ShopId, string Slug)`

**1.4 Create IShopRepository interface**
- Mirrors `IBrandRepository`: GetByIdAsync, GetBySlugAsync, GetAllAsync, AddAsync, SaveChangesAsync

### Phase 2: Application Layer (3 files)

**2.1 Create Shop DTOs** — CreateShopRequest, UpdateShopRequest, AddressResponse, ShopResponse
**2.2 Create IShopService interface** — CRUD + activate/deactivate
**2.3 Create ShopService implementation** — follows BrandService pattern
**2.4 Register ShopService in DI**

### Phase 3: Infrastructure Layer (4 files + migration)

**3.1 Create ShopConfiguration** — EF Core config, Address as owned entity, unique slug index
**3.2 Register Shop in BrandDbContext** — Add DbSet<Shop>
**3.3 Create ShopRepository** — **HIGH RISK**: First brand-scoped repository, needs BrandDbContext DI pattern
**3.4 Register in DI** — Add scoped BrandDbContext factory registration + ShopRepository
**3.5 Generate EF Core migration** — First real BrandDbContext migration
**3.6 Seed sample shops** — 2-3 shops for "Frietjes?" dev data

### Phase 4: API Endpoints (6 files)

- `POST /api/brands/{brandSlug}/shops` — CreateShop (201, 409 on duplicate slug)
- `PUT /api/brands/{brandSlug}/shops/{id}` — UpdateShop (200, 404)
- `GET /api/brands/{brandSlug}/shops/{id}` — GetShop (200, 404)
- `GET /api/brands/{brandSlug}/shops` — ListShops (200)
- `POST /api/brands/{brandSlug}/shops/{id}/deactivate` — Deactivate (204, 404)
- `POST /api/brands/{brandSlug}/shops/{id}/activate` — Activate (204, 404)

### Phase 5: Frontend (6 files)

**5.1** Expand Shop type in `types/common.ts`
**5.2** Create `api/shops.ts` API client
**5.3** Create `useShops.ts` TanStack Query hooks
**5.4** Create ShopList page
**5.5** Create ShopCreate page
**5.6** Create ShopEdit page
**5.7** Add shop routes
**5.8** Add navigation link from Dashboard

---

## Risks & Mitigations

| Risk | Severity | Mitigation |
|------|----------|------------|
| BrandDbContext DI registration pattern | **HIGH** | Register as scoped factory delegate, shared per request. This sets the pattern for all future brand-scoped repos. |
| Brand context middleware for shop endpoints | MEDIUM | Middleware already supports `{brandSlug}` route values. Add integration tests. |
| First BrandDbContext migration with real tables | MEDIUM | Run migration early, verify generated SQL. |
| Address owned entity column naming | LOW | Explicitly configure column names in ShopConfiguration. |
| Product catalog inheritance not implementable | LOW | Raise `ShopCreatedEvent`; future handler will clone catalog. |

## Dependencies

**Prerequisites (complete):** US-FP-001 (Brand), US-FP-070 (BrandDatabaseProvisioner), BrandDbContext/Factory
**Unblocks:** US-FP-005 (Products), US-FP-040 (Opening hours), US-FP-020 (Time slots), all of Stream C

## Success Criteria

- [ ] CRUD + activate/deactivate endpoints working against brand-scoped database
- [ ] Slug unique within brand (409 on duplicate), immutable after creation
- [ ] Admin UI: list, create, edit, and toggle shop status
- [ ] `ShopCreatedEvent` raised for future catalog inheritance
- [ ] BrandDbContext migration applies correctly
- [ ] Dev seeder creates sample shops
- [ ] All tests pass with 80%+ coverage on new code
