---
name: backend-dotnet
description: ".NET backend patterns and conventions for TheMillionthFoodOrderApp. Covers Aspire, FastEndpoints, EF Core, DDD, Clean Architecture, BFF, FluentValidation, Swagger, and xUnit. Use when working in src/backend/."
---

# Backend .NET Skill

Reference for the .NET backend stack. Each component has its own doc — read only what's relevant to the current task.

## Stack Overview

| Component | Purpose | Docs |
|-----------|---------|------|
| .NET Aspire | Orchestration, service defaults, local dev | [aspire.md](docs/aspire.md) |
| FastEndpoints | API endpoints (replaces controllers) | [fast-endpoints.md](docs/fast-endpoints.md) |
| EF Core | Data access, database-per-brand | [ef-core.md](docs/ef-core.md) |
| DDD | Domain modeling — aggregates, entities, value objects | [ddd.md](docs/ddd.md) |
| Clean Architecture | Project structure and dependency flow | [clean-architecture.md](docs/clean-architecture.md) |
| BFF | Backend-for-Frontend — auth/session management | [bff.md](docs/bff.md) |
| FluentValidation | Request validation | [fluent-validation.md](docs/fluent-validation.md) |
| Swagger/OpenAPI | API documentation | [swagger.md](docs/swagger.md) |
| MassTransit | Async messaging, domain events, sagas | [masstransit.md](docs/masstransit.md) |
| xUnit | Testing — unit and integration | Use `/dotnet-unit-testing` skill |

## Code Conventions

- **Always use `DateTimeOffset`** — never `DateTime`. DateTime lacks timezone awareness and causes subtle bugs. Use DateTimeOffset for all entity properties, DTOs, API contracts, and EF Core mappings.

## When to Read Which Doc

- **Adding a new endpoint?** → fast-endpoints.md + fluent-validation.md + swagger.md
- **Modeling a domain concept?** → ddd.md + clean-architecture.md
- **Database work?** → ef-core.md
- **Auth or frontend-facing API?** → bff.md
- **Setting up services or infrastructure?** → aspire.md
- **Domain events or async workflows?** → masstransit.md + ddd.md
- **Writing tests?** → use `/dotnet-unit-testing` skill
