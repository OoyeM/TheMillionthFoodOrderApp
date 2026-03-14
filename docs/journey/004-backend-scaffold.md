# 004 — Backend Scaffold: .NET + Aspire + Clean Architecture

**Date:** 2026-03-14

## What happened

Scaffolded the full .NET backend from the patterns defined in the `/backend-dotnet` skill. The solution is runnable with `dotnet run` from the AppHost project.

## Structure created

```
src/backend/
├── TheMillionthFoodOrderApp.Api/          — FastEndpoints API host
├── TheMillionthFoodOrderApp.AppHost/      — .NET Aspire orchestrator
├── TheMillionthFoodOrderApp.Application/  — Use cases, DI registration
├── TheMillionthFoodOrderApp.Bff/          — Backend-for-frontend (auth/session)
├── TheMillionthFoodOrderApp.Domain/       — DDD building blocks
├── TheMillionthFoodOrderApp.Infrastructure/ — EF Core, external services
├── TheMillionthFoodOrderApp.ServiceDefaults/ — Aspire shared config
└── TheMillionthFoodOrderApp.slnx          — Solution file
```

## Key decisions

- **Aspire AppHost as entry point** — orchestrates Api, Bff, and future services. Service discovery and health checks come free.
- **Clean Architecture layers** — Domain has zero dependencies; Infrastructure references Domain + Application; Api references everything.
- **DDD base classes in Domain** — `Entity<TId>`, `AggregateRoot<TId>`, `ValueObject`, `IDomainEvent` ready for first aggregate.
- **BFF separate from API** — the BFF handles frontend auth/session concerns; the API stays pure resource endpoints.
- **slnx format** — uses the new XML-based solution format instead of classic .sln.

## What's not yet wired

- No FastEndpoints registered yet (no domain entities to expose)
- No EF Core DbContext configured (waiting for first aggregate)
- No MassTransit messaging
- No authentication/authorization middleware

## Lessons

1. **.NET Aspire ServiceDefaults** centralizes OpenTelemetry, health checks, and service discovery config — avoids duplicating this across Api and Bff.
2. **Starting with DDD base classes** before any entities ensures consistent patterns when the first aggregate lands.
