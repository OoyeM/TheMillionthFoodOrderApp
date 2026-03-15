# .NET Aspire

## Purpose
Orchestration, service defaults, and local development environment.

## What Aspire Provides

- **Single-command startup** — `dotnet run --project AppHost` starts Api + Bff, manages ports, ordering (`WaitFor`)
- **Aspire Dashboard** — local dashboard with real-time logs, distributed traces, and metrics across all services
- **Service discovery** — services reference each other by name, no hardcoded URLs
- **OpenTelemetry** — tracing (cross-service requests), metrics (ASP.NET Core, HttpClient, runtime), OTLP export via env var
- **Health checks** — `/health` (all checks) and `/alive` (liveness) on every service
- **HTTP resilience** — standard resilience handler (retries, circuit breaker, timeouts) on all HttpClient calls
- **Cloud deployment** — `azd up` generates Azure Container Apps infra from the AppHost definition

## AppHost Resource Registration

```csharp
var sql = builder.AddSqlServer("sql")
    .AddDatabase("platform");  // shared platform DB (brands, users, config)

var api = builder.AddProject<Projects.Api>("api")
    .WithReference(sql);

var bff = builder.AddProject<Projects.Bff>("bff")
    .WithReference(api)
    .WaitFor(api);
```

Aspire wires connection strings automatically — no `appsettings.json` juggling.

## Multi-Tenant Database Strategy

- **One SQL Server instance** managed by Aspire
- **Platform database** registered in AppHost — shared data (brands, users, platform config)
- **Brand databases** created dynamically at runtime when a brand is onboarded — Aspire doesn't need to know about them
- Application code (EF Core `BrandDbContextFactory`) resolves the correct connection string based on brand context

## Gotchas

- Aspire is an orchestration/dev tool, not a runtime — deployed apps are plain .NET apps
- Magic around connection strings and service discovery can be confusing to debug — check Aspire Dashboard traces first
- Azure-leaning deployment story — other clouds need more manual setup
- Still evolving — watch for breaking changes between versions
