# 011 — Startup Fixes, UUIDv7, and Aspire SQL Password

**Date:** 2026-03-16

---

## What Was Fixed

### Brand Database Not Created Before Seeding

The dev startup path in `Program.cs` called `PlatformDbSeeder` (which inserts the Frietjes? brand record) and then immediately called `BrandDbSeeder` — but the brand database `brand_frietjes` didn't exist yet.

In production, `BrandDatabaseProvisioner` handles the `BrandCreatedEvent` via Wolverine and creates the database asynchronously. But during startup seeding, Wolverine hasn't started processing messages yet, so the event never fires.

**Fix:** Explicitly call `BrandDatabaseProvisioner.HandleAsync()` in the dev seeding block before running `BrandDbSeeder`. This creates the database and applies migrations synchronously during startup, matching what Wolverine would do asynchronously in production.

### Aspire SQL Server Password Mismatch

With `WithLifetime(ContainerLifetime.Persistent)` and `WithDataVolume("sql-data")`, Aspire generates a random SA password on each run. The persistent volume retains the original password, so subsequent runs fail with "Login failed for user 'sa'".

**Fix:** Added `builder.AddParameter("sql-password", secret: true)` and passed it to `AddSqlServer()`. The password is now set once by the developer and stays consistent across restarts.

### Missing Brand Migration for Shop Entity

The `Shop` entity was added to `BrandDbContext` but no migration existed for it, causing `PendingModelChangesWarning` on startup.

**Fix:** Added `AddShops` brand migration.

### FastEndpoints Startup Resolution of BrandDbContext

FastEndpoints instantiates all endpoints at startup to build the route map. Brand-scoped endpoints depend on `BrandDbContext`, which requires an active brand slug — unavailable during startup.

**Fix:** `BrandDbContextFactory.CreateDbContext()` now returns a placeholder context (with a dummy connection string) when no brand slug is set. This satisfies DI during startup but will fail loudly if actually queried outside a brand-scoped request.

### Guid.NewGuid() → Guid.CreateVersion7()

Replaced all `Guid.NewGuid()` calls with `Guid.CreateVersion7()` across all domain entity factory methods (Brand, Shop, PlatformUser, BrandUserRole, BrandSettings).

UUIDv7 (.NET 9+) embeds a timestamp prefix, producing time-ordered identifiers that:
- Improve clustered index insert performance (no random page splits)
- Provide natural chronological ordering without needing a separate `CreatedAt` sort
- Are still globally unique like v4 GUIDs

This is now a documented convention in the backend CLAUDE.md and DDD skill.

---

## Files Changed

### Modified
- `AppHost/Program.cs` — added `sql-password` parameter for stable SA password
- `Api/Program.cs` — added explicit brand DB provisioning before seeding
- `Infrastructure/Persistence/BrandDbContextFactory.cs` — placeholder context when no brand slug
- `Infrastructure/Persistence/Migrations/Brand/` — new `AddShops` migration
- `Domain/Brands/Brand.cs` — `Guid.CreateVersion7()`
- `Domain/Shops/Shop.cs` — `Guid.CreateVersion7()`
- `Domain/Identity/PlatformUser.cs` — `Guid.CreateVersion7()`
- `Domain/Identity/BrandUserRole.cs` — `Guid.CreateVersion7()`
- `Domain/BrandSettings/BrandSettings.cs` — `Guid.CreateVersion7()`
- `src/backend/CLAUDE.md` — added UUIDv7 convention
- `.claude/skills/backend-dotnet/docs/ddd.md` — added UUIDv7 to aggregate design rules
