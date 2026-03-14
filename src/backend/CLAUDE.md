# Backend — CLAUDE.md

## Tech Stack

- .NET (C#), ASP.NET Web API, .NET Aspire
- FastEndpoints (not controllers) with FluentValidation
- EF Core, database-per-brand (multi-tenant isolation)
- Swagger/OpenAPI via FastEndpoints
- MassTransit for async messaging (in-memory locally, RabbitMQ/Azure Service Bus in prod)

## Architecture

- BFF layer (.NET) between frontend and API — handles auth/session management
- Clean Architecture: Api → Application → Domain → Infrastructure
- DDD: aggregates, entities, value objects, domain events
- One endpoint per class — no controllers, no MediatR

## Solution Structure

```
TheMillionthFoodOrderApp.AppHost/        — .NET Aspire orchestrator (run this)
TheMillionthFoodOrderApp.Api/            — FastEndpoints API host
TheMillionthFoodOrderApp.Bff/            — Backend-for-frontend (auth/session)
TheMillionthFoodOrderApp.Application/    — Use cases, DI registration
TheMillionthFoodOrderApp.Domain/         — DDD base classes (Entity, AggregateRoot, ValueObject)
TheMillionthFoodOrderApp.Infrastructure/ — EF Core, external services
TheMillionthFoodOrderApp.ServiceDefaults/ — Aspire shared config (telemetry, health)
```

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
