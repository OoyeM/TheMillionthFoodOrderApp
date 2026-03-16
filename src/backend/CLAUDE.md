# Backend — CLAUDE.md

## Tech Stack

- .NET (C#), ASP.NET Web API, .NET Aspire
- FastEndpoints (not controllers) with FluentValidation
- EF Core, database-per-brand (multi-tenant isolation)
- Swagger/OpenAPI via FastEndpoints
- Wolverine for async messaging and mediator (in-memory locally, RabbitMQ/Azure Service Bus in prod)

## Architecture

- BFF layer (.NET) between frontend and API — handles auth/session management
- Clean Architecture: Api → Application → Domain → Infrastructure
- DDD: aggregates, entities, value objects, domain events
- One endpoint per class — no controllers, no MediatR

## Solution Structure

```
TheMillionthFoodOrderApp.AppHost/        — .NET Aspire orchestrator (run this)
TheMillionthFoodOrderApp.Api/            — FastEndpoints API host (http://localhost:5102, Swagger at /swagger)
TheMillionthFoodOrderApp.Bff/            — Backend-for-frontend (auth/session, YARP proxy to API)
TheMillionthFoodOrderApp.Application/    — Use cases, services, DTOs
TheMillionthFoodOrderApp.Domain/         — DDD aggregates, entities, value objects, domain events
TheMillionthFoodOrderApp.Infrastructure/ — EF Core (PlatformDbContext, BrandDbContext), repositories
TheMillionthFoodOrderApp.ServiceDefaults/ — Aspire shared config (telemetry, health)
```

## Local Development

- **Requires Docker Desktop** — Aspire runs SQL Server and Keycloak in containers with persistent volumes
- **SQL Server password** is set via `sql-password` Aspire parameter (prompted on first run). Required because `WithDataVolume` persists the password in the volume — random passwords desync on restart.
- **Keycloak** runs as Aspire container with realm auto-imported from `AppHost/keycloak/themillionfoodorderapp-realm.json`. Admin UI available at Aspire-assigned port. Persistent volume preserves data across restarts.
- API runs on `http://localhost:5102` — Swagger UI is the default launch page
- BFF runs on `http://localhost:5261` — auth endpoints + YARP proxy to API
- Frontend proxies `/api/*` and `/bff/*` to the BFF (not directly to API)
- Database: SQL Server container via Aspire, platform DB auto-migrated on startup. In dev, `BrandDatabaseProvisioner` is called explicitly at startup to create + migrate the brand DB before seeding (Wolverine isn't running yet at that point).
- Auth: mock auth enabled by default in dev (`Authentication:UseMockAuth=true`). Set to `false` to use Keycloak OIDC. Personas: `platform-admin`, `brand-admin@frietjes`, `counter-staff@frietjes`, `customer`. Test user passwords: `P@ssw0rd!`

## Aspire Integration (important — affects DI patterns)

This project uses **.NET Aspire** as the orchestrator. Aspire's `Add*` extension methods replace standard `AddDbContext`/`AddSqlServer` calls and bring **different defaults** than vanilla EF Core registration:

- **`builder.AddSqlServerDbContext<T>()`** registers the DbContext with **pooling enabled** by default.
- **Never use `OnConfiguring`** in pooled DbContexts — options are shared across pooled instances. Interceptors, query tracking, etc. must be configured via the `configureDbContextOptions` callback at registration time in `Program.cs`.
- **Connection strings are injected by Aspire** via the resource name (e.g., `"platform"`). Don't hardcode or read them from `appsettings.json`.
- **Service discovery** uses Aspire naming (e.g., `https+http://api` in YARP config). Don't use `localhost` URLs between services.
- **Health checks, retries, and telemetry** are wired automatically by Aspire — don't add them manually.

## Database Architecture

- **PlatformDbContext** — shared platform DB: brands, users (PlatformUser), roles (BrandUserRole), platform config. Registered via `AddSqlServerDbContext` (pooled).
- **BrandDbContext** — one DB per brand, created dynamically at runtime by `BrandDatabaseProvisioner`. Registered as **scoped** in DI via `BrandDbContextFactory` — inject directly into brand-scoped repositories.
- **BrandDbContextFactory** — resolves correct connection string based on brand slug (same SQL Server instance, different `Database=brand_{slug}`). Returns a placeholder context when no brand slug is set (happens at startup when FastEndpoints builds the route map).
- **BrandContextMiddleware** — validates brand slug from route/header against platform DB (cached 30s), returns 404/403 for invalid/inactive brands.
- **BrandScopedPreProcessor** — FastEndpoints pre-processor; add to all brand-scoped endpoints to guard against missing brand context.

## BFF Endpoints

- `GET /bff/login?mock=<persona>` — mock sign-in (dev) or OIDC challenge via Keycloak
- `POST /bff/logout` — sign out (cookie + federated OIDC logout when not using mock)
- `GET /bff/user` — current user info (always 200, returns `{ isAuthenticated: false }` if anonymous)
- `POST /bff/session/keepalive` — slide session expiration
- `/api/*` — YARP reverse proxy to API with bearer token forwarding (access token from OIDC stored via `SaveTokens=true`)

## Commands

- `dotnet run --project TheMillionthFoodOrderApp.AppHost` — start everything via Aspire
- `dotnet build TheMillionthFoodOrderApp.slnx` — build all projects
- `dotnet test` — run tests (xUnit)
- `dotnet ef migrations add <Name> --project TheMillionthFoodOrderApp.Infrastructure --startup-project TheMillionthFoodOrderApp.Api --context PlatformDbContext --output-dir Persistence/Migrations/Platform` — add platform migration
- `dotnet ef migrations add <Name> --project TheMillionthFoodOrderApp.Infrastructure --startup-project TheMillionthFoodOrderApp.Api --context BrandDbContext --output-dir Persistence/Migrations/Brand` — add brand migration

## Domain Patterns

- **Soft-delete:** Implement `ISoftDeletable` (IsDeleted + DeletedAt). Add a global query filter on `BrandDbContext`: `HasQueryFilter(e => !e.IsDeleted)`. Use `IgnoreQueryFilters()` only when historical data is needed.
- **Translations:** Use a child entity (e.g. `ProductTranslation`) with composite unique index on `(ParentId, LanguageCode)`. Load eagerly with `Include()`. On update, clear the collection and re-add — avoids EF Core orphan tracking issues.
- **Money:** `Money` value object (Amount + Currency), mapped as EF owned entity with explicit column names (`BasePrice_Amount`, `BasePrice_Currency`).

## Domain Constraints

- Belgian VAT: 6% takeaway, 21% eat-in
- Multi-language: NL, FR, DE

## Code Conventions

- **Always use `DateTimeOffset`** — never `DateTime`. DateTimeOffset is timezone-aware and avoids subtle bugs with UTC conversions and comparisons.
- **Always use `Guid.CreateVersion7()`** — never `Guid.NewGuid()`. UUIDv7 embeds a timestamp, producing time-ordered IDs that are better for database index performance and natural sort order.

## Testing

- xUnit + FluentAssertions, integration tests hit a real database (not mocks)
- **Testcontainers.MsSql** for integration tests — spins up SQL Server in Docker automatically
- `IntegrationTestWebAppFactory` replaces Aspire's pooled PlatformDbContext with a standard registration pointing at the test container
- `IntegrationTestBase` provisions multiple brand databases (alpha, beta, gamma) on the same container to verify cross-brand isolation
- Use `IClassFixture<IntegrationTestBase>` to share the container across tests in a class

For detailed patterns, see `.claude/skills/backend-dotnet/docs/`.
