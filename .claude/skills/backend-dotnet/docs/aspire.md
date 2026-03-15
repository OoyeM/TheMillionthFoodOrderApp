# .NET Aspire

## Purpose

Orchestration, service defaults, and local development environment. **Aspire changes how DI registration, connection strings, and service-to-service communication work** compared to vanilla .NET — read this doc before touching infrastructure code.

## What Aspire Provides

- **Single-command startup** — `dotnet run --project AppHost` starts Api + Bff + SQL Server, manages ports, ordering (`WaitFor`)
- **Aspire Dashboard** — local dashboard with real-time logs, distributed traces, and metrics across all services
- **Service discovery** — services reference each other by name, no hardcoded URLs
- **OpenTelemetry** — tracing (cross-service requests), metrics (ASP.NET Core, HttpClient, runtime), OTLP export via env var
- **Health checks** — `/health` (all checks) and `/alive` (liveness) on every service
- **HTTP resilience** — standard resilience handler (retries, circuit breaker, timeouts) on all HttpClient calls
- **Cloud deployment** — `azd up` generates Azure Container Apps infra from the AppHost definition

## AppHost Resource Registration

The AppHost (`Program.cs`) declares all resources using a fluent API:

```csharp
var builder = DistributedApplication.CreateBuilder(args);

// SQL Server container with persistent data
var sql = builder.AddSqlServer("sql")
    .WithLifetime(ContainerLifetime.Persistent)   // survives AppHost restart
    .WithDataVolume("sql-data");                  // named Docker volume
var platformDb = sql.AddDatabase("platform");     // logical database

var api = builder.AddProject<Projects.Api>("api")
    .WithReference(platformDb)                    // injects ConnectionStrings__platform
    .WaitFor(platformDb);                         // delays start until healthy

var bff = builder.AddProject<Projects.Bff>("bff")
    .WithReference(api)                           // enables service discovery
    .WaitFor(api)
    .WithExternalHttpEndpoints();                 // marks as externally accessible

builder.Build().Run();
```

### Key AppHost APIs

| API | Purpose |
|-----|---------|
| `AddSqlServer("name")` | SQL Server container |
| `.AddDatabase("name")` | Logical database on a SQL Server |
| `AddRedis("name")` | Redis container |
| `AddRabbitMQ("name")` | RabbitMQ container |
| `AddProject<T>("name")` | .NET project reference |
| `AddConnectionString("name")` | Reference to an existing external resource |
| `.WithReference(resource)` | Injects connection string or enables service discovery |
| `.WaitFor(resource)` | Delays startup until resource is healthy |
| `.WithEnvironment("key", "value")` | Injects environment variable |
| `.WithEndpoint("https", e => e.Port = 5001)` | Override endpoint port |
| `.WithLifetime(ContainerLifetime.Persistent)` | Container survives AppHost restart |
| `.WithDataVolume("name")` | Named Docker volume for data persistence |
| `.WithHttpHealthCheck("/health")` | Custom health check path |

### Parameters and secrets

```csharp
var adminPassword = builder.AddParameter("sql-password", secret: true);
var sql = builder.AddSqlServer("sql", password: adminPassword);
```

### Connecting to existing external resources

```csharp
// Reads from ConnectionStrings:existing-db in config/user-secrets
var existingDb = builder.AddConnectionString("existing-db");
builder.AddProject<Projects.Api>("api").WithReference(existingDb);
```

## Service Defaults

The `ServiceDefaults` project is shared by every service. `AddServiceDefaults()` registers:

- **OpenTelemetry** — logging (`IncludeFormattedMessage`, `IncludeScopes`), metrics (`AddAspNetCoreInstrumentation`, `AddHttpClientInstrumentation`, `AddRuntimeInstrumentation`), tracing (`AddAspNetCoreInstrumentation`, `AddHttpClientInstrumentation`), OTLP exporter when `OTEL_EXPORTER_OTLP_ENDPOINT` is set
- **Health checks** — self-check tagged with `"live"`
- **Service discovery** — `AddServiceDiscovery()` resolves service names to URLs
- **HTTP resilience** — Polly standard resilience handler (retries, circuit breaker, timeouts) on all `HttpClient` calls

`MapDefaultEndpoints()` maps:
- `/health` — all health checks (readiness)
- `/alive` — only `"live"` tagged checks (liveness)

**Don't duplicate these registrations manually** — they're already wired.

## Aspire Component Packages (`Aspire.*` NuGet)

### Hosting vs. Client integrations

| Package pattern | Installed in | Purpose |
|---|---|---|
| `Aspire.Hosting.*` | AppHost | Provisions/configures the resource |
| `Aspire.*` (no Hosting) | Service project | Client-side DI with health checks, telemetry, retries |

### `Aspire.Microsoft.EntityFrameworkCore.SqlServer`

This is what `builder.AddSqlServerDbContext<T>()` comes from. It replaces `services.AddDbContext<T>()`.

**What it does differently from vanilla EF Core:**
- Registers DbContext with **pooling enabled** (uses `AddDbContextPool` internally)
- Adds **health check** that calls `CanConnectAsync`
- Adds **OpenTelemetry tracing** for EF Core
- Adds **connection retry** (SqlClient resiliency)
- Reads connection string from `ConnectionStrings:{connectionName}` (injected by Aspire)

**Full signature:**
```csharp
builder.AddSqlServerDbContext<PlatformDbContext>("platform",
    configureSettings: settings =>
    {
        settings.DisableHealthChecks = false;   // default: false
        settings.DisableTracing = false;        // default: false
        settings.DisableMetrics = false;        // default: false
        settings.CommandTimeout = 30;           // seconds
    },
    configureDbContextOptions: options =>
    {
        options.AddInterceptors(new AuditSaveChangesInterceptor());
    });
```

### `EnrichSqlServerDbContext` — for manually registered DbContexts

When you register a DbContext yourself (e.g., factory pattern for multi-tenant), use Enrich to add Aspire features:

```csharp
// Register yourself (non-pooled)
builder.Services.AddDbContext<BrandDbContext>(options => { /* ... */ });

// Then add Aspire health checks, telemetry, retries
builder.EnrichSqlServerDbContext<BrandDbContext>();
```

## Connection String Management

### How injection works

1. AppHost resource named `"platform"` → env var `ConnectionStrings__platform`
2. .NET config binds `ConnectionStrings__platform` → `ConnectionStrings:platform`
3. Aspire components read via `connectionName` parameter match

**The name must match** between AppHost and service:
```csharp
// AppHost:
var db = sql.AddDatabase("platform");       // name = "platform"
// Service:
builder.AddSqlServerDbContext<T>("platform"); // must match
```

**Don't put connection strings in `appsettings.json`** for Aspire-managed resources — Aspire injects them via env vars.

**Exception:** EF Core migrations (`dotnet ef`) run outside Aspire, so use a design-time factory with a fallback connection string.

## Service Discovery

### How it works

`WithReference(projectB)` injects configuration that maps the name `"projectB"` to its actual endpoint URLs. `AddServiceDiscovery()` in ServiceDefaults enables resolution.

### The `https+http://servicename` URI scheme

```
https+http://api
```

Means: **try HTTPS first, fall back to HTTP**. Service discovery resolves `api` to the actual `host:port`.

Use in YARP cluster config:
```json
{
  "ReverseProxy": {
    "Clusters": {
      "api-cluster": {
        "Destinations": {
          "primary": { "Address": "https+http://api" }
        }
      }
    }
  }
}
```

**Never use `localhost` URLs between services** — always use service discovery names.

## Multi-Tenant Database Strategy

- **One SQL Server instance** managed by Aspire
- **Platform database** registered in AppHost — shared data (brands, users, platform config)
- **Brand databases** created dynamically at runtime — Aspire doesn't know about them
- `BrandDbContextFactory` resolves the correct connection string by deriving from the platform connection string

## Testing Aspire Apps

Package: `Aspire.Hosting.Testing`

```csharp
public class ApiTests
{
    [Fact]
    public async Task GetBrands_ReturnsOk()
    {
        var appHost = await DistributedApplicationTestingBuilder
            .CreateAsync<Projects.MyApp_AppHost>();

        await using var app = await appHost.BuildAsync();
        await app.StartAsync();

        // Wait for resource to be healthy
        var notifications = app.Services.GetRequiredService<ResourceNotificationService>();
        await notifications.WaitForResourceHealthyAsync("api");

        var httpClient = app.CreateHttpClient("api");
        var response = await httpClient.GetAsync("/api/brands");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
```

- Dashboard is disabled in tests by default
- Ports are randomized for parallel execution
- `CreateHttpClient("name")` creates a client pointed at the named resource

## Deployment

### `azd` integration

```bash
azd init       # detects Aspire AppHost
azd provision  # generates Bicep, deploys Azure resources
azd deploy     # builds container images, pushes to ACR, updates ACA
```

### Aspire 9.2+ publish model

```csharp
// AppHost — add a compute environment for publishing
builder.AddDockerComposeEnvironment("compose");
// OR
builder.AddKubernetesEnvironment("k8s");
```

| Publisher package | Target |
|---|---|
| `Aspire.Hosting.Azure` | Azure (Bicep) |
| `Aspire.Hosting.Docker` | Docker Compose |
| `Aspire.Hosting.Kubernetes` | Kubernetes manifests |

## Gotchas

### DbContext pooling is ON by default (the #1 pitfall)

`AddSqlServerDbContext<T>()` uses `AddDbContextPool` internally.

- **Never use `OnConfiguring`** — options are shared across pooled instances. Will throw `InvalidOperationException` at runtime.
- **Interceptors, seeding, query tracking** — all must go in `configureDbContextOptions` callback at registration time.
- **No per-request state in DbContext constructors** — pooled contexts are reused.

```csharp
// WRONG — throws at runtime with pooling
protected override void OnConfiguring(DbContextOptionsBuilder options)
{
    options.AddInterceptors(myInterceptor);
}

// RIGHT — configure at registration time
builder.AddSqlServerDbContext<PlatformDbContext>("platform",
    configureDbContextOptions: options =>
    {
        options.AddInterceptors(new AuditSaveChangesInterceptor());
    });
```

### BrandDbContext is NOT in DI

It's multi-tenant — created on-demand via `BrandDbContextFactory`. Never inject `BrandDbContext` directly. If a class needs brand data, inject `BrandDbContextFactory`.

### Container data persistence

Without `WithLifetime(ContainerLifetime.Persistent)` + `WithDataVolume()`, SQL Server containers restart on every AppHost restart and **lose all data**. Always use both for database containers.

### Startup ordering

- `WaitFor()` only delays startup — it doesn't guarantee the resource is fully ready
- Use `WithHttpHealthCheck("/health")` on projects so `WaitFor` knows when they're truly ready
- Without health checks, `WaitFor` relies on container status (running), not application readiness

### EF Core migrations run outside Aspire

`dotnet ef` doesn't go through the AppHost, so connection strings aren't injected. Provide a design-time factory with a fallback connection string.

### Health checks are automatic

Aspire component packages register health checks automatically. Don't add duplicate health checks for resources already managed by Aspire.

### Running without Aspire

Services can run standalone but need manual configuration — provide connection strings via `appsettings.Development.json` or user-secrets, use direct URLs instead of service discovery.

### CI/CD

- Install Aspire workload: `dotnet workload install aspire`
- Container resources need Docker in CI
- Podman can be problematic — Docker is more reliable with Aspire
