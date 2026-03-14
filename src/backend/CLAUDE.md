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

## Domain Constraints

- Belgian VAT: 6% takeaway, 21% eat-in
- Multi-language: NL, FR, DE

## Testing

- xUnit, integration tests hit a real database (not mocks)

For detailed patterns, see `.claude/docs/`.
