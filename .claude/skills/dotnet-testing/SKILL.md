---
name: dotnet-testing
description: Use when writing, fixing, or reviewing any tests in the .NET project. Covers TUnit for both unit and integration tests, replacing xUnit entirely.
---

# .NET Testing Skill

**TUnit** is the single test framework for this project — unit tests, integration tests, everything.

## Reference Docs

- **[Unit Testing](./unit-testing.md)** — test class structure, mocking, parameterized tests, conventions
- **[Integration Testing](./integration-testing.md)** — ASP.NET Core, Testcontainers, per-test isolation
- **[Assertions](./assertions.md)** — full `Assert.That()` API reference
- **[Mocking](./mocking.md)** — TUnit.Mocks setup, verification, argument matchers
- **[Hooks & Lifecycle](./hooks.md)** — `[Before]`/`[After]` at every scope
- **[Data-Driven Tests](./data-driven.md)** — `[Arguments]`, `[Matrix]`, `[MethodDataSource]`, `[ClassDataSource]`

## Project Setup

### .csproj

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="TUnit" Version="*" />
    <PackageReference Include="TUnit.Mocks" Version="*" Prerelease="true" />
  </ItemGroup>
</Project>
```

**Integration test project also needs:**
```xml
<PackageReference Include="TUnit.AspNetCore" Version="*" />
<PackageReference Include="Testcontainers.MsSql" Version="4.*" />
<PackageReference Include="Microsoft.AspNetCore.Mvc.Testing" Version="10.*" />
```

> **Never add** `Microsoft.NET.Test.Sdk` or `coverlet.collector` — they conflict with TUnit.  
> **Never add** `xunit`, `xunit.runner.visualstudio`, or any xUnit package.  
> TUnit includes `Microsoft.Testing.Extensions.CodeCoverage` built-in.

### Global Usings (auto-injected by TUnit)

TUnit automatically provides `TUnit.Core`, `TUnit.Assertions`, and `TUnit.Assertions.Extensions` as global usings — no explicit imports needed.

## Running Tests

```bash
# Preferred — direct execution, all flags available
dotnet run -c Release

# Via dotnet test (flags go after --)
dotnet test -c Release -- --report-trx --coverage

# Filter to a class or method
dotnet run -- --treenode-filter "/*/*/MyTestClass/*"
dotnet run -- --treenode-filter "/*/*/MyTestClass/MyMethod"

# Filter by category
dotnet run -- --treenode-filter "/*/*/*/*[Category=Integration]"

# With coverage and TRX report
dotnet run -c Release --coverage --report-trx
```

## IDE Setup

- **Visual Studio**: Tools → Options → Preview Features → "Use testing platform server mode" → restart
- **Rider**: Settings → Build, Execution, Deployment → Unit Testing → Testing Platform → enable → restart
- **VS Code**: Install C# Dev Kit → enable "Dotnet > Test Window > Use Testing Platform Protocol"

## Critical Rules

| Rule | Why |
|------|-----|
| All `Assert.That(...)` calls **must be awaited** | Assertions return awaitable objects; un-awaited assertions silently pass |
| Test methods must be `async Task` if they use assertions | Compiler error `TUnit0031` if `async void` |
| No `[TestClass]` on the class | TUnit doesn't require it |
| `[Before(Class)]` and higher must be **static** | Instance methods at class/assembly scope are a compile error |
| `[Before(Test)]` must be an **instance** method | Static test-level hooks don't work |
| Do **not** mutate shared `ClassDataSource` state in tests | Tests run in parallel; shared mutable state causes flakiness |

## Quick xUnit → TUnit Migration Reference

| xUnit | TUnit |
|-------|-------|
| `[Fact]` | `[Test]` |
| `[Theory]` + `[InlineData(...)]` | `[Test]` + `[Arguments(...)]` |
| `IClassFixture<T>` | `[ClassDataSource<T>(Shared = SharedType.PerClass)]` |
| `ICollectionFixture<T>` | `[ClassDataSource<T>(Shared = SharedType.PerTestSession)]` |
| `IAsyncLifetime` | `IAsyncInitializer` + `IAsyncDisposable` |
| Constructor injection | Constructor params or `[ClassDataSource<T>]` |
| `Assert.Equal(expected, actual)` | `await Assert.That(actual).IsEqualTo(expected)` |
| `Assert.NotNull(x)` | `await Assert.That(x).IsNotNull()` |
| `Assert.Throws<T>(...)` | `await Assert.ThrowsAsync<T>(...)` |
