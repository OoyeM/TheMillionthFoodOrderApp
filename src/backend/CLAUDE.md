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

- **Requires Docker Desktop** — Aspire runs SQL Server in a container
- API runs on `http://localhost:5102` — Swagger UI is the default launch page
- BFF runs on `http://localhost:5261` — auth endpoints + YARP proxy to API
- Frontend proxies `/api/*` and `/bff/*` to the BFF (not directly to API)
- Database: SQL Server container via Aspire, platform DB auto-migrated on startup, "Frietjes?" brand seeded in dev
- Auth: mock auth enabled by default in dev (`Authentication:UseMockAuth=true`). Personas: `platform-admin`, `brand-admin@frietjes`, `counter-staff@frietjes`, `customer`

## Database Architecture

- **PlatformDbContext** — shared platform DB: brands, users (PlatformUser), roles (BrandUserRole), platform config
- **BrandDbContext** — one DB per brand, created dynamically at runtime by `BrandDatabaseProvisioner`
- **BrandDbContextFactory** — resolves correct connection string based on brand slug (same SQL Server instance, different `Database=brand_{slug}`)

## BFF Endpoints

- `GET /bff/login?mock=<persona>` — mock sign-in (dev), OIDC challenge (prod)
- `POST /bff/logout` — sign out
- `GET /bff/user` — current user info (always 200, returns `{ isAuthenticated: false }` if anonymous)
- `POST /bff/session/keepalive` — slide session expiration
- `/api/*` — YARP reverse proxy to API with bearer token injection

## Commands

- `dotnet run --project TheMillionthFoodOrderApp.AppHost` — start everything via Aspire
- `dotnet build TheMillionthFoodOrderApp.slnx` — build all projects
- `dotnet test` — run tests (xUnit)
- `dotnet ef migrations add <Name> --project TheMillionthFoodOrderApp.Infrastructure --startup-project TheMillionthFoodOrderApp.Api --context PlatformDbContext --output-dir Persistence/Migrations/Platform` — add platform migration
- `dotnet ef migrations add <Name> --project TheMillionthFoodOrderApp.Infrastructure --startup-project TheMillionthFoodOrderApp.Api --context BrandDbContext --output-dir Persistence/Migrations/Brand` — add brand migration

## Domain Constraints

- Belgian VAT: 6% takeaway, 21% eat-in
- Multi-language: NL, FR, DE

## Code Conventions

- **Always use `DateTimeOffset`** — never `DateTime`. DateTimeOffset is timezone-aware and avoids subtle bugs with UTC conversions and comparisons.

## Testing

- xUnit, integration tests hit a real database (not mocks)

For detailed patterns, see `.claude/docs/`.
