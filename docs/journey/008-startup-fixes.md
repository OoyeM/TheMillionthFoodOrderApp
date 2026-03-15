# 008 — Startup & DI Fixes

**Date:** 2026-03-15

## What Was Done

Fixed three startup errors preventing the Aspire-orchestrated backend from running.

### 1. BrandDbSeeder DI Resolution Failure

**Error:** `Unable to resolve service for type 'BrandDbContext' while attempting to activate 'BrandDbSeeder'`

**Cause:** `BrandDbSeeder` took `BrandDbContext` as a constructor parameter, but `BrandDbContext` is never registered in DI — it's multi-tenant and created on-demand via `BrandDbContextFactory`.

**Fix:** Changed `BrandDbSeeder` to accept `BrandDbContextFactory` instead, matching the database-per-brand pattern.

### 2. DbContext Pooling Conflict with OnConfiguring

**Error:** `'OnConfiguring' cannot be used to modify DbContextOptions when DbContext pooling is enabled`

**Cause:** Aspire's `AddSqlServerDbContext<T>()` enables DbContext pooling by default. `PlatformDbContext.OnConfiguring` added `AuditSaveChangesInterceptor` — which is forbidden with pooling because pooled contexts share options.

**Fix:** Removed `OnConfiguring` from `PlatformDbContext`. Moved interceptor registration to the `configureDbContextOptions` callback in `Program.cs` where it's applied at pool creation time. Since `AuditSaveChangesInterceptor` has no dependencies, it's instantiated directly.

### 3. Missing EF Core Migrations

**Error:** `Invalid object name 'platform.Brands'` — tables didn't exist despite `MigrateAsync()` running.

**Cause:** No migration files had ever been generated. `MigrateAsync()` ran successfully but had nothing to apply.

**Fix:** Added `Microsoft.EntityFrameworkCore.Design` package to the API project and generated the initial platform migration (`InitialPlatform`). Creates `platform.Brands`, `platform.PlatformUsers`, and `platform.BrandUserRoles` tables with proper schema, indexes, and foreign keys.

### 4. Documentation & Skill Updates

Updated docs to capture the Aspire-specific patterns learned from the fixes above:

- **`src/backend/CLAUDE.md`** — added "Aspire Integration" section documenting pooling constraints, connection string injection, service discovery naming, and auto-wired health checks
- **`.claude/skills/backend-dotnet/docs/aspire.md`** — major expansion: AppHost APIs (resource registration, parameters, secrets, external resources), Service Defaults internals, Aspire component packages (`AddSqlServerDbContext` full signature, `EnrichSqlServerDbContext`), connection string management, service discovery with `https+http://` scheme, YARP integration, testing with `DistributedApplicationTestingBuilder`, deployment (azd + Aspire 9.2+ publishers), and comprehensive gotchas
- **`.claude/skills/backend-dotnet/docs/ef-core.md`** — added pooling and BrandDbContext gotchas

### 5. PR Review Fixes

Code review of PR #76 surfaced two issues, both now resolved:

- **Auth middleware dev-only** — `UseAuthentication()`/`UseAuthorization()` were wrapped in `if (IsDevelopment())`, meaning production would skip auth entirely. Fix: auth scheme registration stays conditional (dev pass-through vs. future JWT bearer), but middleware and `AddAuthorization()` are now unconditional.
- **AppHost missing data persistence** — SQL Server container lacked `WithLifetime(ContainerLifetime.Persistent)` and `WithDataVolume("sql-data")`, causing data loss on every AppHost restart. Fix: added both to the `AddSqlServer` call.

### 6. Issue Triage & Parallel Work Planning

Reviewed issues #2–#5 for Azure dependency and parallelization:

| Issue | Azure needed? | Status |
|-------|--------------|--------|
| #2 Shop management | No | Ready — next up |
| #3 Staff auth config | Partially (backend done, SSO login needs Azure) | Frontend UI doable without Azure |
| #4 Data isolation | No | Mostly done, needs tests/verification |
| #5 Simple products | No | Ready — depends on #2 (shops) |

**Parallel work decision:** sequential foundation first (shops, then products), then parallel features once core entities are in place. 2 worktrees max during this phase — pick stories from different layers (one backend-heavy, one frontend-heavy). Full 4-worktree parallel viable once past product catalog into independent features (QR codes, order tracking, allergen filters).

## Key Takeaways

1. Aspire's `AddSqlServerDbContext` enables DbContext pooling — interceptors and other option modifications must be done at registration time, not in `OnConfiguring`. This is a common pitfall when migrating from non-pooled to pooled DbContext registration.
2. Auth middleware should always be in the pipeline unconditionally — only the scheme registration should be environment-specific.
3. Always use `WithLifetime(Persistent)` + `WithDataVolume()` for database containers in Aspire to avoid data loss.
4. For greenfield projects, parallel worktrees work best after the shared foundation (core domain entities) is established.

## Files Changed

### Code fixes
- `Infrastructure/Persistence/Seeding/BrandDbSeeder.cs` — constructor: `BrandDbContext` → `BrandDbContextFactory`
- `Infrastructure/Persistence/PlatformDbContext.cs` — removed `OnConfiguring` and `AuditSaveChangesInterceptor` ctor param
- `Api/Program.cs` — added interceptor via `configureDbContextOptions` callback; auth middleware now unconditional
- `Api/TheMillionthFoodOrderApp.Api.csproj` — added `Microsoft.EntityFrameworkCore.Design` package
- `AppHost/Program.cs` — added `WithLifetime(Persistent)` + `WithDataVolume("sql-data")`
- `Infrastructure/Persistence/Migrations/Platform/` — new: `InitialPlatform` migration + snapshot

### Documentation
- `src/backend/CLAUDE.md` — added Aspire Integration section
- `.claude/skills/backend-dotnet/docs/aspire.md` — expanded from 64 lines to comprehensive reference
- `.claude/skills/backend-dotnet/docs/ef-core.md` — added pooling/multi-tenant gotchas
