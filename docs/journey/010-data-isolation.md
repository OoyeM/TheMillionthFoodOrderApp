# 010 — US-FP-004: Data Isolation Between Brands

**Date:** 2026-03-15
**Branch:** `feat/us-fp-4-usfp004-data-isolation-between-brands`
**User Story:** US-FP-004 — Each brand's data lives in its own database; no API endpoint leaks data across brand boundaries.

---

## What Was Built

### Multi-Tenant Pipeline Strengthened

The `BrandContextMiddleware` previously extracted the brand slug from the request route or header but never validated whether the slug referred to a real, active brand. This opened the door to requests hitting non-existent brand databases.

**Added:**
- `IBrandContextValidator` (Application layer) — interface for brand slug validation
- `BrandContextValidator` (Infrastructure layer) — validates against `PlatformDbContext` with a 30-second `IMemoryCache` TTL to avoid N+1 per-request DB hits
- Updated `BrandContextMiddleware` to call the validator:
  - Unknown slug → `404 Not Found`
  - Inactive brand → `403 Forbidden`
  - Valid, active brand → slug stored in `BrandContextAccessor`, pipeline continues

### BrandDbContext Registered as Scoped Service

Previously `BrandDbContext` was not in the DI container — code had to manually call `BrandDbContextFactory.CreateDbContext()`. Now it is registered as `AddScoped<BrandDbContext>(sp => factory.CreateDbContext())`, making it injectable like any other DbContext.

The `AuditSaveChangesInterceptor` (singleton) is now passed into `BrandDbContextFactory` and wired into every `BrandDbContext` instance, ensuring `CreatedAt`/`UpdatedAt` are auto-populated for brand-scoped entities.

### First Brand-Scoped Entity: BrandSettings

**Domain layer** (`TheMillionthFoodOrderApp.Domain/BrandSettings/`):
- `BrandSettings` — aggregate root with `DefaultLanguage`, `Timezone`, `Currency`, `CreatedAt`, `UpdatedAt`
- `IBrandSettingsRepository` — repository interface

**Infrastructure layer** (`TheMillionthFoodOrderApp.Infrastructure/BrandSettings/`):
- `BrandSettingsConfiguration` — EF Core `IEntityTypeConfiguration<BrandSettings>` (maps to `dbo.BrandSettings`)
- `BrandSettingsRepository` — implementation against `BrandDbContext`
- `BrandDbContext` updated with `DbSet<BrandSettings>` and configuration applied

**Application layer** (`TheMillionthFoodOrderApp.Application/BrandSettings/`):
- `BrandSettingsDtos` — `BrandSettingsResponse` and `UpdateBrandSettingsRequest`
- `IBrandSettingsService` / `BrandSettingsService` — get + upsert logic

### Brand-Scoped Endpoint Pattern

**`BrandScopedPreProcessor<TRequest>`** — FastEndpoints pre-processor that guards brand-scoped endpoints. Returns `400 Bad Request` if no brand context is active (defence against misconfiguration). All brand-scoped endpoints attach it via `PreProcessor<BrandScopedPreProcessor<TRequest>>()`.

**New endpoints:**
- `GET /api/brands/{brandSlug}/settings` — returns brand settings or 404
- `PUT /api/brands/{brandSlug}/settings` — creates or updates brand settings

### Dev Seed Data

`BrandDbSeeder` now seeds default `BrandSettings` (Belgian defaults: `nl-BE`, `Europe/Brussels`, `EUR`) for the Frietjes? brand on startup. `Program.cs` sets the brand accessor slug before calling the seeder.

### Integration Tests

New project: `TheMillionthFoodOrderApp.Tests.Integration`

Uses **Testcontainers.MsSql** (SQL Server 2022) and **Microsoft.AspNetCore.Mvc.Testing** `WebApplicationFactory`. The factory overrides Aspire's `PlatformDbContext` registration with a standard EF Core registration pointing at the container.

Test setup:
1. Spins up one SQL Server container per test run (shared via `IClassFixture`)
2. Migrates the platform database
3. Seeds `alpha` and `beta` brands
4. Provisions and migrates `brand_alpha` and `brand_beta` databases

**Isolation tests** (`BrandSettingsIsolationTests`):
- Settings written to Alpha are not visible from Beta
- Both brands can have independent settings simultaneously
- Upsert updates existing records correctly

**Middleware tests** (`BrandContextMiddlewareTests`):
- Unknown slug → 404
- Inactive brand → 403
- Valid brand → 404 from endpoint (not from middleware)
- Platform endpoints without brand slug are unaffected

---

## Architecture Notes

### Database-Per-Brand Pattern

```
SQL Server instance (Aspire container)
├── platform           ← PlatformDbContext: Brand registry, users, roles
├── brand_frietjes     ← BrandDbContext: Frietjes? settings, (future: shops, products, orders)
├── brand_alpha        ← BrandDbContext: Alpha settings (test)
└── brand_beta         ← BrandDbContext: Beta settings (test)
```

Connection strings are derived at runtime by swapping the `Initial Catalog` in the platform connection string. This is done in `BrandDbContextFactory` using `SqlConnectionStringBuilder` — never with string replacement or regex.

### Guard: Brand Validation TTL Cache

The 30-second TTL ensures middleware doesn't hammer the platform DB but does pick up brand activation/deactivation changes within 30 seconds. This is acceptable for the current scale. If lower latency is needed, the cache can be bypassed on explicit brand state change events.

### Why Upsert for BrandSettings

Each brand has exactly one `BrandSettings` row (singleton pattern). Using upsert (`GetOrCreate`) simplifies both the seeding path and the API — callers don't need to know whether settings have been provisioned yet.

---

## Files Changed

### New Files
- `Application/Multitenancy/IBrandContextValidator.cs`
- `Application/BrandSettings/BrandSettingsDtos.cs`
- `Application/BrandSettings/IBrandSettingsService.cs`
- `Application/BrandSettings/BrandSettingsService.cs`
- `Domain/BrandSettings/BrandSettings.cs`
- `Domain/BrandSettings/IBrandSettingsRepository.cs`
- `Infrastructure/Multitenancy/BrandContextValidator.cs`
- `Infrastructure/BrandSettings/BrandSettingsConfiguration.cs`
- `Infrastructure/BrandSettings/BrandSettingsRepository.cs`
- `Api/Endpoints/BrandScopedPreProcessor.cs`
- `Api/Endpoints/BrandSettings/GetBrandSettingsEndpoint.cs`
- `Api/Endpoints/BrandSettings/UpdateBrandSettingsEndpoint.cs`
- `Tests.Integration/` (new project)

### Modified Files
- `Api/Middleware/BrandContextMiddleware.cs` — added validator call, 404/403 responses
- `Api/Program.cs` — brand DB seeding, `public partial class Program`
- `Infrastructure/DependencyInjection.cs` — IMemoryCache, IBrandContextValidator, BrandDbContext scoped, IBrandSettingsRepository
- `Infrastructure/Persistence/BrandDbContext.cs` — added BrandSettings DbSet + configuration
- `Infrastructure/Persistence/BrandDbContextFactory.cs` — added AuditSaveChangesInterceptor
- `Infrastructure/Persistence/Seeding/BrandDbSeeder.cs` — seeds default BrandSettings
- `Application/DependencyInjection.cs` — IBrandSettingsService registration
