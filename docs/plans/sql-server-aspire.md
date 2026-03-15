# Plan: Add SQL Server to .NET Aspire

## Overview

Replace EF Core InMemory database with a containerized SQL Server instance managed by Aspire. One SQL Server instance, one platform database registered in AppHost, brand databases created dynamically at runtime.

## Prerequisites

- Docker Desktop (for SQL Server container)
- Fix all `DateTime` → `DateTimeOffset` violations first

## Phases

### Phase 1: Fix DateTimeOffset Violations
- `IDomainEvent.cs` — `DateTime OccurredOn` → `DateTimeOffset`
- `Brand.cs` — `DateTime CreatedAt/UpdatedAt` → `DateTimeOffset`, `DateTime.UtcNow` → `DateTimeOffset.UtcNow`
- `BrandCreatedEvent.cs`, `BrandDeactivatedEvent.cs` — same fix
- `BrandDtos.cs` — `BrandResponse` uses `DateTime` → `DateTimeOffset`

### Phase 2: AppHost SQL Server Resource
- Add `Aspire.Hosting.SqlServer` NuGet to AppHost
- Register SQL Server container + platform database in `Program.cs`:
  ```csharp
  var sql = builder.AddSqlServer("sql")
      .AddDatabase("platform");
  var api = builder.AddProject<Projects.TheMillionthFoodOrderApp_Api>("api")
      .WithReference(sql).WaitFor(sql);
  ```

### Phase 3: EF Core Infrastructure
- Add `Aspire.Microsoft.EntityFrameworkCore.SqlServer` to Api project
- Remove `Microsoft.EntityFrameworkCore.InMemory` from Infrastructure
- Create `DateTimeOffsetConvention` (IModelFinalizingConvention)
- Create `AuditSaveChangesInterceptor` (auto-set CreatedAt/UpdatedAt)
- Create `IAuditable` interface in Domain
- Update `PlatformDbContext` with conventions + schema

### Phase 4: BrandDbContext and Multi-Tenancy
- Create `IBrandContextAccessor` in Application layer
- Create `BrandContextAccessor` (reads brand slug from HttpContext)
- Create `BrandContextMiddleware` (extracts brand slug from route/header)
- Create `BrandDbContext` (brand-scoped entities — initially empty shell)
- Create `BrandDbContextFactory` (derives connection string: same server, different `Database=brand_{slug}`)
- Create `BrandDatabaseProvisioner` (creates brand DB + applies migrations on BrandCreatedEvent)

### Phase 5: DI Registration
- Update `DependencyInjection.cs` — remove InMemory, register brand context services
- Update Api `Program.cs` — `builder.AddSqlServerDbContext<PlatformDbContext>("platform")`

### Phase 6: Migrations
- Create initial Platform migration (`--context PlatformDbContext --output-dir Persistence/Migrations/Platform`)
- Create initial Brand migration (`--context BrandDbContext --output-dir Persistence/Migrations/Brand`)
- Create `BrandDbContextDesignTimeFactory` for CLI tooling
- Add auto-migration on startup for Platform DB

### Phase 7: Seeding
- `PlatformDbSeeder` — seeds "Frietjes?" brand in dev mode
- `BrandDbSeeder` — stub for future brand data
- Wire seeding into startup (dev only)

### Phase 8: Documentation
- Update backend CLAUDE.md (Docker requirement, migration commands)
- Update aspire.md and ef-core.md skill docs
- Add journal entry

## Key Design Decisions

- **Connection string derivation**: Use `SqlConnectionStringBuilder` to parse Aspire-injected connection string and swap `Database=platform` → `Database=brand_{slug}`
- **Brand DB provisioning**: Async via Wolverine handler on `BrandCreatedEvent`, not synchronous in the API request
- **Design-time factory**: Hardcoded local connection string for `dotnet ef migrations` CLI only

## Risks
- Docker Desktop must be running (document clearly)
- Connection string derivation — use `SqlConnectionStringBuilder`, never regex
- Separate migration histories need `--context` flag on every `dotnet ef` command

## New Files
- `Domain/Common/IAuditable.cs`
- `Application/Multitenancy/IBrandContextAccessor.cs`
- `Infrastructure/Multitenancy/BrandContextAccessor.cs`
- `Api/Middleware/BrandContextMiddleware.cs`
- `Infrastructure/Persistence/BrandDbContext.cs`
- `Infrastructure/Persistence/BrandDbContextFactory.cs`
- `Infrastructure/Persistence/BrandDbContextDesignTimeFactory.cs`
- `Infrastructure/Persistence/BrandDatabaseProvisioner.cs`
- `Infrastructure/Persistence/Conventions/DateTimeOffsetConvention.cs`
- `Infrastructure/Persistence/Interceptors/AuditSaveChangesInterceptor.cs`
- `Infrastructure/Persistence/Seeding/PlatformDbSeeder.cs`
- `Infrastructure/Persistence/Seeding/BrandDbSeeder.cs`
