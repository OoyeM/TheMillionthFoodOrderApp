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
TheMillionthFoodOrderApp.Bff/            — Backend-for-frontend (auth/session)
TheMillionthFoodOrderApp.Application/    — Use cases, services, DTOs
TheMillionthFoodOrderApp.Domain/         — DDD aggregates, entities, value objects, domain events
TheMillionthFoodOrderApp.Infrastructure/ — EF Core (PlatformDbContext), repositories
TheMillionthFoodOrderApp.ServiceDefaults/ — Aspire shared config (telemetry, health)
```

## Local Development

- API runs on `http://localhost:5102` — Swagger UI is the default launch page
- Frontend Vite dev server (`http://localhost:5173`) proxies `/api/*` to the API
- Database: EF Core InMemory (`PlatformDb`) for local dev — no SQL Server required

## Commands

- `dotnet run --project TheMillionthFoodOrderApp.AppHost` — start everything via Aspire
- `dotnet build TheMillionthFoodOrderApp.slnx` — build all projects
- `dotnet test` — run tests (xUnit)

## Domain Constraints

- Belgian VAT: 6% takeaway, 21% eat-in
- Multi-language: NL, FR, DE

## Testing

- xUnit, integration tests hit a real database (not mocks)

For detailed patterns, see `.claude/docs/`.
