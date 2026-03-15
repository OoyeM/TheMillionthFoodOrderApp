# Implementation Plan: US-FP-004 -- Data Isolation Between Brands

## Overview

Implement database-per-brand data isolation so that each brand's products, orders, customers, staff, and settings live in a dedicated SQL Server database (`brand_{slug}`), while platform-level data (brand registry, platform admin accounts) remains in the shared platform database. This story introduces the first brand-scoped entity (BrandSettings), a validation layer that verifies brand existence before resolving brand contexts, a `BrandScopedPreProcessor` that wires up brand context validation for FastEndpoints, and integration tests that prove cross-brand data leakage is impossible.

## Requirements

From the acceptance criteria:
1. Each brand's products, orders, customers, staff, and settings reside in its own database
2. Platform-level data (brand registry, platform admin accounts) lives in a shared platform database
3. No API endpoint leaks data across brand boundaries
4. A shop in Brand A cannot access data from Brand B

## Current State Analysis

### What exists
- **PlatformDbContext**: Stores `Brand`, `PlatformUser`, `BrandUserRole` in the `platform` schema
- **BrandDbContext**: Empty shell -- no DbSets, no entity configurations
- **BrandDbContextFactory**: Creates `BrandDbContext` from `IBrandContextAccessor.BrandSlug`
- **BrandDatabaseProvisioner**: Wolverine handler for `BrandCreatedEvent` -- creates DB and applies migrations
- **BrandContextMiddleware**: Extracts brand slug from route `{brandSlug}` or `X-Brand-Slug` header
- **BrandContextAccessor**: Scoped holder for `string? BrandSlug`
- **No test projects exist**

### What is missing
- No brand-scoped entities in `BrandDbContext`
- No validation in middleware that the brand slug corresponds to a real, active brand
- No `BrandDbContext` DI registration per-request
- No base class or convention for brand-scoped endpoints
- No integration tests proving isolation
- `AuditSaveChangesInterceptor` not added to `BrandDbContext`

## Implementation Phases

### Phase 1: Strengthen the Multi-Tenant Pipeline (3 steps)

#### Step 1.1: Add brand validation to BrandContextMiddleware

**New files:**
- `src/backend/TheMillionthFoodOrderApp.Application/Multitenancy/IBrandContextValidator.cs`
- `src/backend/TheMillionthFoodOrderApp.Infrastructure/Multitenancy/BrandContextValidator.cs`

**Modified files:**
- `src/backend/TheMillionthFoodOrderApp.Api/Middleware/BrandContextMiddleware.cs`
- `src/backend/TheMillionthFoodOrderApp.Infrastructure/DependencyInjection.cs`

After resolving the slug from route/header, look up the brand in PlatformDbContext via `IBrandContextValidator`. Return 404 for unknown brands, 403 for inactive. Use `IMemoryCache` with 30-second TTL to avoid N+1 queries.

#### Step 1.2: Register BrandDbContext as scoped service via factory

**Modified files:**
- `src/backend/TheMillionthFoodOrderApp.Infrastructure/DependencyInjection.cs`
- `src/backend/TheMillionthFoodOrderApp.Infrastructure/Persistence/BrandDbContextFactory.cs`

Register `BrandDbContext` as scoped via `services.AddScoped<BrandDbContext>(sp => ...)`. Add `AuditSaveChangesInterceptor` to BrandDbContextFactory.

#### Step 1.3: Verify middleware ordering in Program.cs

Ensure `BrandContextMiddleware` runs after auth but before FastEndpoints.

### Phase 2: First Brand-Scoped Entity -- BrandSettings (6 steps)

#### Step 2.1: Create BrandSettings domain entity
`src/backend/TheMillionthFoodOrderApp.Domain/BrandSettings/BrandSettings.cs`

Simple aggregate root: DefaultLanguage, Timezone, Currency, CreatedAt/UpdatedAt (DateTimeOffset).

#### Step 2.2: Create EF Core configuration
`src/backend/TheMillionthFoodOrderApp.Infrastructure/BrandSettings/BrandSettingsConfiguration.cs`

#### Step 2.3: Register in BrandDbContext
Add `DbSet<BrandSettings>` and apply configuration.

#### Step 2.4: Generate migration
`dotnet ef migrations add AddBrandSettings --context BrandDbContext`

#### Step 2.5: Create repository
- `src/backend/TheMillionthFoodOrderApp.Domain/BrandSettings/IBrandSettingsRepository.cs`
- `src/backend/TheMillionthFoodOrderApp.Infrastructure/BrandSettings/BrandSettingsRepository.cs`

#### Step 2.6: Create application service + DTOs
- `src/backend/TheMillionthFoodOrderApp.Application/BrandSettings/IBrandSettingsService.cs`
- `src/backend/TheMillionthFoodOrderApp.Application/BrandSettings/BrandSettingsService.cs`
- `src/backend/TheMillionthFoodOrderApp.Application/BrandSettings/BrandSettingsDtos.cs`

### Phase 3: Brand-Scoped Endpoint Pattern (3 steps)

#### Step 3.1: Create BrandScopedPreProcessor
`src/backend/TheMillionthFoodOrderApp.Api/Endpoints/BrandScopedPreProcessor.cs`

FastEndpoints pre-processor that validates brand context is set. All brand-scoped endpoints add `.PreProcessor<BrandContextPreProcessor>()`.

#### Step 3.2: Create GET/PUT BrandSettings endpoints
- `GET /api/brands/{brandSlug}/settings`
- `PUT /api/brands/{brandSlug}/settings`

#### Step 3.3: Register new services in DI

### Phase 4: Seed Data for Dev (2 steps)

#### Step 4.1: Update BrandDbSeeder
Seed default BrandSettings for "Frietjes?" brand.

#### Step 4.2: Wire into startup

### Phase 5: Integration Tests (4 steps)

#### Step 5.1: Create test project
`src/backend/TheMillionthFoodOrderApp.Tests.Integration` with xUnit, Testcontainers.MsSql, FluentAssertions.

#### Step 5.2: Create test fixtures
`IntegrationTestWebAppFactory` + `IntegrationTestBase` with two brand databases (alpha, beta).

#### Step 5.3: Write isolation tests
- Settings written to Brand Alpha not visible to Brand Beta
- Unknown brand slug returns 404
- Inactive brand returns 403
- Missing brand context returns 400
- Platform endpoints still work

#### Step 5.4: Write middleware tests

### Phase 6: Documentation (2 steps)

#### Step 6.1: Add journey entry
`docs/journey/010-data-isolation.md`

#### Step 6.2: Add guard comments

## File Change Summary

### New files (13+)
1. `IBrandContextValidator.cs` -- validate brand slug
2. `BrandContextValidator.cs` -- implementation
3. `BrandSettings.cs` -- domain entity
4. `BrandSettingsConfiguration.cs` -- EF Core config
5. `IBrandSettingsRepository.cs` -- repository interface
6. `BrandSettingsRepository.cs` -- repository impl
7. `IBrandSettingsService.cs` -- service interface
8. `BrandSettingsService.cs` -- service impl
9. `BrandSettingsDtos.cs` -- DTOs
10. `BrandScopedPreProcessor.cs` -- FastEndpoints pre-processor
11. `GetBrandSettingsEndpoint.cs` -- GET endpoint
12. `UpdateBrandSettingsEndpoint.cs` -- PUT endpoint
13. Test project + fixtures + tests

### Modified files (6)
1. `BrandContextMiddleware.cs` -- add validation
2. `BrandDbContext.cs` -- add BrandSettings DbSet
3. `BrandDbContextFactory.cs` -- add AuditSaveChangesInterceptor
4. `Infrastructure/DependencyInjection.cs` -- register services
5. `Application/DependencyInjection.cs` -- register services
6. `BrandDbSeeder.cs` -- seed BrandSettings

## Risks and Mitigations

| Risk | Mitigation |
|------|------------|
| Middleware hits platform DB per request | IMemoryCache with 30s TTL |
| Brand DB not yet provisioned | Return 503 "being provisioned" |
| Slow CI with Testcontainers | Class fixture shares container |

## Success Criteria

- [ ] BrandDbContext contains BrandSettings entity with migration
- [ ] BrandContextMiddleware validates brand slug (404/403)
- [ ] BrandDbContext registered as scoped in DI
- [ ] Brand-scoped endpoints use `/api/brands/{brandSlug}/...` pattern
- [ ] Integration tests prove cross-brand isolation
- [ ] Platform endpoints continue working
- [ ] Build passes
- [ ] Dev seeding populates BrandSettings
