# Data-Driven Tests Reference

## [Arguments] — inline data

Repeats the test once per attribute:

```csharp
[Test]
[Arguments(1, 1, 2)]
[Arguments(2, 3, 5)]
[Arguments(0, 0, 0)]
public async Task Add_ReturnsExpected(int a, int b, int expected)
{
    await Assert.That(a + b).IsEqualTo(expected);
}
```

### Metadata on test cases

```csharp
[Test]
[Arguments("admin", true,  DisplayName = "Admin gets access")]
[Arguments("guest", false, DisplayName = "Guest is denied")]
[Arguments("",      false, Skip = "Empty username edge case not implemented")]
public async Task AuthCheck(string role, bool expected) { }

// Parameter substitution in DisplayName
[Test]
[Arguments(2, 3, 5, DisplayName = "Adding $a + $b = $expected")]
public async Task AddWithSubstitution(int a, int b, int expected) { }

// Categories for filtering
[Test]
[Arguments("Chrome", Categories = new[] { "Browser", "Smoke" })]
[Arguments("Firefox", Categories = new[] { "Browser" })]
public async Task BrowserTest(string browser) { }
```

## [Matrix] — combinatorial data

Generates all combinations of the provided values per parameter:

```csharp
[Test]
[Matrix("nl", "fr", "de")]                  // languages
[Matrix(true, false)]                        // active
public async Task TranslationTest(string lang, bool active)
{
    // runs 3 × 2 = 6 test cases
}
```

`[MatrixRange<T>]` for numeric ranges:
```csharp
[Test]
[MatrixRange<int>(1, 10)]       // 1, 2, 3, ..., 10
[MatrixRange<int>(1, 10, 2)]    // 1, 3, 5, 7, 9  (step = 2)
public async Task RangeTest(int value) { }
```

## [MethodDataSource] — dynamic data from a static method

```csharp
public class ProductTests
{
    public static IEnumerable<(decimal price, bool valid)> PriceTestCases()
    {
        yield return (3.50m, true);
        yield return (0m, false);
        yield return (-1m, false);
    }

    [Test]
    [MethodDataSource(nameof(PriceTestCases))]
    public async Task ValidatePrice(decimal price, bool expectedValid)
    {
        var result = PriceValidator.IsValid(price);
        await Assert.That(result).IsEqualTo(expectedValid);
    }
}

// Cross-class data source
[Test]
[MethodDataSource(typeof(SharedTestData), nameof(SharedTestData.BrandSlugs))]
public async Task BrandTest(string slug) { }
```

Return `IEnumerable<Func<T>>` for objects that need deferred construction:
```csharp
public static IEnumerable<Func<CreateProductRequest>> CreateRequests()
{
    yield return () => new CreateProductRequest { BasePrice = 3.50m, ... };
    yield return () => new CreateProductRequest { BasePrice = 10.00m, ... };
}
```

## [ClassDataSource] — injected objects

Inject an instance of a class into test constructor or as a test method parameter.

```csharp
// New instance per test (default)
[Test]
[ClassDataSource<DatabaseSetup>]
public async Task TestWithDatabase(DatabaseSetup db) { }

// Shared across tests in the same class
[ClassDataSource<ExpensiveResource>(Shared = SharedType.PerClass)]
public class MyTests(ExpensiveResource resource) { }

// Shared across entire test session (use for containers / factories)
[ClassDataSource<SqlServerContainer>(Shared = SharedType.PerTestSession)]
public class MyTests(SqlServerContainer sql) { }
```

### SharedType values

| Value | Scope |
|-------|-------|
| `SharedType.None` | New instance per injection point (default) |
| `SharedType.PerClass` | One instance per test class |
| `SharedType.PerAssembly` | One instance per assembly |
| `SharedType.PerTestSession` | One instance for the entire run (all assemblies) |
| `SharedType.Keyed` | Shared by explicit string key |

### IAsyncInitializer + IAsyncDisposable for lifecycle management

```csharp
public sealed class SqlServerContainer : IAsyncInitializer, IAsyncDisposable
{
    private readonly MsSqlContainer _container = new MsSqlBuilder().Build();

    public string ConnectionString => _container.GetConnectionString();

    public Task InitializeAsync() => _container.StartAsync();
    public ValueTask DisposeAsync() => _container.DisposeAsync();
}

// Usage — one container for all tests
[ClassDataSource<SqlServerContainer>(Shared = SharedType.PerTestSession)]
public class IntegrationTestBase(SqlServerContainer sql)
{
    [Test]
    public async Task MyTest()
    {
        // sql.ConnectionString available
    }
}
```

## Multiple ClassDataSource (multi-type injection)

```csharp
[Test]
[ClassDataSource<TypeA, TypeB, TypeC>(
    Shared = [SharedType.PerTestSession, SharedType.PerClass, SharedType.None],
    Keys   = ["",                        "",                   ""]
)]
public async Task MultiSourceTest(TypeA a, TypeB b, TypeC c) { }
```

`Keys` is positional — only needed for `SharedType.Keyed` entries.

## Combining Sources

You can combine `[Arguments]` with `[ClassDataSource]`:

```csharp
[Test]
[Arguments("alpha")]
[Arguments("beta")]
[ClassDataSource<SqlServerContainer>(Shared = SharedType.PerTestSession)]
public async Task TestWithBrand(string slug, SqlServerContainer sql) { }
```

## Filtering by Category (CLI)

```bash
# Run only tests tagged with "Integration"
dotnet run -- --treenode-filter "/*/*/*/*[Category=Integration]"

# Exclude "Performance" tests
dotnet run -- --treenode-filter "/*/*/*/*[Category!=Performance]"

# OR-combine filters
dotnet run -- --treenode-filter "/*/*/ClassA/*|/*/*/ClassB/*"
```
