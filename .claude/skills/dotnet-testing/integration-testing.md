# Integration Testing with TUnit

Tests hit a real SQL Server (via Testcontainers), a real ASP.NET Core host, and real EF Core migrations. No mocking of infrastructure.

## Packages Required

```xml
<PackageReference Include="TUnit" Version="*" />
<PackageReference Include="TUnit.AspNetCore" Version="*" />
<PackageReference Include="Testcontainers.MsSql" Version="4.*" />
<PackageReference Include="Microsoft.AspNetCore.Mvc.Testing" Version="10.*" />
```

## Core Architecture

```
SqlServerContainer          ← IAsyncInitializer + IAsyncDisposable
    ↓ SharedType.PerTestSession
IntegrationTestWebAppFactory ← TestWebApplicationFactory<Program>
    ↓ shared
IntegrationTestBase         ← abstract base class all tests inherit
    ↓
ProductCrudTests : IntegrationTestBase
```

One SQL Server container starts once per test session. All test classes share it.

## Container Wrapper

```csharp
public sealed class SqlServerContainer : IAsyncInitializer, IAsyncDisposable
{
    private readonly MsSqlContainer _container = new MsSqlBuilder()
        .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
        .Build();

    public string ConnectionString => _container.GetConnectionString();

    public Task InitializeAsync() => _container.StartAsync();
    public ValueTask DisposeAsync() => _container.DisposeAsync();
}
```

`IAsyncInitializer.InitializeAsync()` is called before the first test that uses this type. `IAsyncDisposable.DisposeAsync()` is called when the shared scope ends.

## WebApplicationFactory

Use `TestWebApplicationFactory<Program>` (from `TUnit.AspNetCore`) instead of the vanilla `WebApplicationFactory<Program>`. The TUnit version enables trace correlation, per-test logging, and `TestContext.Current` in request handlers.

Override `ConfigureStartupConfiguration` (not `ConfigureAppConfiguration`) when the connection string must be available **before** `Program.cs` runs:

```csharp
public sealed class IntegrationTestWebAppFactory
    : TestWebApplicationFactory<Program>, IAsyncInitializer
{
    [ClassDataSource<SqlServerContainer>(Shared = SharedType.PerTestSession)]
    public SqlServerContainer SqlServer { get; init; } = null!;

    public async Task InitializeAsync()
    {
        // Migrations and seeding run once at session start
        await using var scope = Services.CreateAsyncScope();
        var platformDb = scope.ServiceProvider.GetRequiredService<PlatformDbContext>();
        await platformDb.Database.MigrateAsync();
        await SeedPlatformBrandsAsync(platformDb);
        await ProvisionBrandDatabaseAsync("alpha");
        await ProvisionBrandDatabaseAsync("beta");
        await ProvisionBrandDatabaseAsync("gamma");
    }

    // ConfigureStartupConfiguration runs BEFORE Program.cs — use this for connection strings
    protected override void ConfigureStartupConfiguration(
        IConfigurationBuilder config)
    {
        config.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:platform"] = SqlServer.ConnectionString,
        });
    }

    protected override void ConfigureTestServices(IServiceCollection services)
    {
        // Remove Aspire's pooled PlatformDbContext and replace with standard registration
        services.RemoveAll<DbContextOptions<PlatformDbContext>>();
        services.RemoveAll<PlatformDbContext>();
        for (var i = services.Count - 1; i >= 0; i--)
        {
            var d = services[i];
            if (d.ServiceType.IsGenericType &&
                d.ServiceType.GenericTypeArguments is [var t] &&
                t == typeof(PlatformDbContext))
                services.RemoveAt(i);
        }
        services.AddDbContext<PlatformDbContext>((sp, opts) =>
        {
            opts.UseSqlServer(SqlServer.ConnectionString);
            opts.AddInterceptors(sp.GetRequiredService<AuditSaveChangesInterceptor>());
        });
    }
}
```

## IntegrationTestBase

All integration test classes inherit from this:

```csharp
public abstract class IntegrationTestBase
    : WebApplicationTest<IntegrationTestWebAppFactory, Program>
{
    public const string AlphaSlug = "alpha";
    public const string BetaSlug = "beta";
    public const string GammaSlug = "gamma";   // never written to — use for empty-state assertions

    protected HttpClient CreateClient() => Factory.CreateClient();
}
```

`WebApplicationTest<TFactory, TProgram>` (from `TUnit.AspNetCore`) exposes:
- `Factory` — the per-test isolated factory instance
- `GlobalFactory` — the shared factory instance
- `Services` — DI container
- `UniqueId` — integer unique per test
- `GetIsolatedName(baseName)` → `"Test_42_baseName"` 
- `GetIsolatedPrefix(sep)` → `"test_42_"`

## Writing Integration Tests

```csharp
public sealed class ProductCrudTests : IntegrationTestBase
{
    [Test]
    public async Task CreateProduct_Returns201_WithTranslations()
    {
        var client = CreateClient();

        var response = await client.PostAsJsonAsync(
            $"/api/brands/{AlphaSlug}/products",
            new
            {
                BasePrice = 3.50m,
                Translations = new[]
                {
                    new { LanguageCode = "nl", Name = "Frietje Speciaal", Description = (string?)null }
                }
            });

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Created);

        var product = await response.Content.ReadFromJsonAsync<ProductResponse>();
        await Assert.That(product).IsNotNull();
        await Assert.That(product!.Id).IsNotEqualTo(Guid.Empty);
        await Assert.That(product.Translations).HasCount().EqualTo(1);
    }
}
```

## Per-Test Isolation

TUnit runs tests in parallel. Brand-slug isolation (alpha/beta/gamma) is the primary safety net — write tests so they only touch their designated brand. When two tests must truly not overlap:

```csharp
[NotInParallel]
public sealed class SignalRHubTests : IntegrationTestBase { }
```

For finer resource isolation within a test, use `GetIsolatedName`:

```csharp
protected override async Task SetupAsync()
{
    var tableName = GetIsolatedName("products"); // "Test_42_products"
    await CreateTableAsync(tableName);
}
```

## ConfigureTestServices (per-test DI overrides)

Override per test class when you need to replace a service:

```csharp
public sealed class EmailNotificationTests : IntegrationTestBase
{
    private readonly FakeEmailService _emailSpy = new();

    protected override void ConfigureTestServices(IServiceCollection services)
    {
        services.ReplaceService<IEmailService>(_emailSpy);
    }

    [Test]
    public async Task PlaceOrder_SendsConfirmationEmail()
    {
        // act...
        await Assert.That(_emailSpy.SentEmails).HasCount().EqualTo(1);
    }
}
```

## HTTP Exchange Capture

Enable for tests that need to inspect raw HTTP exchanges:

```csharp
public class AuditTests : IntegrationTestBase
{
    protected override WebApplicationTestOptions Options => new()
    {
        EnableHttpExchangeCapture = true
    };

    [Test]
    public async Task CreateProduct_LogsRequestBody()
    {
        var client = CreateClient();
        await client.PostAsJsonAsync("/api/brands/alpha/products", new { ... });

        await Assert.That(HttpCapture!.Last!.Response.StatusCode)
            .IsEqualTo(HttpStatusCode.Created);
        await Assert.That(HttpCapture.Last.Request.Body)
            .Contains("\"basePrice\"");
    }
}
```

## Lifecycle Order (TUnit.AspNetCore)

Understanding this prevents "connection string not available" bugs:

1. `ConfigureStartupConfiguration` — runs before `Program.cs`; use for connection strings
2. `Program.cs` runs
3. `ConfigureWebHost` → `ConfigureTestConfiguration` → `ConfigureTestServices`
4. App starts
5. `SetupAsync` (per test)
6. Test executes
7. `[After(Test)]` hooks
8. App tears down

## Brand DB Isolation (Project-Specific)

- `AlphaSlug` / `BetaSlug` — for CRUD tests; writes to both prove cross-brand isolation
- `GammaSlug` — never written to; use for "list returns empty" or "resource not found" assertions
- Each brand has its own SQL database (`brand_alpha`, `brand_beta`, `brand_gamma`) on the same SQL Server container

## Test Organization

```
Tests.Integration/
  Fixtures/
    SqlServerContainer.cs
    IntegrationTestWebAppFactory.cs
    IntegrationTestBase.cs
  Products/
    ProductCrudTests.cs
    ProductIsolationTests.cs
  MenuCategories/
    MenuCategoryCrudTests.cs
  ...
```
