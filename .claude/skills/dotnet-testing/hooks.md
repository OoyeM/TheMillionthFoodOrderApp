# Hooks & Lifecycle Reference

## Hook Attributes

| Attribute | Method type | Runs |
|-----------|------------|------|
| `[Before(Test)]` | instance | Before each test in the declaring class |
| `[After(Test)]` | instance | After each test in the declaring class |
| `[Before(Class)]` | **static** | Once before the first test in the class |
| `[After(Class)]` | **static** | Once after the last test in the class |
| `[Before(Assembly)]` | **static** | Once before the first test in the assembly |
| `[After(Assembly)]` | **static** | Once after the last test in the assembly |
| `[Before(TestSession)]` | **static** | Once before the first test in the session |
| `[After(TestSession)]` | **static** | Once after the last test in the session |
| `[BeforeEvery(Test)]` | **static** | Before every test in the entire session |
| `[AfterEvery(Test)]` | **static** | After every test in the entire session |
| `[BeforeEvery(Class)]` | **static** | Before each class's first test |
| `[AfterEvery(Class)]` | **static** | After each class's last test |

Key rules:
- `[Before(Test)]` / `[After(Test)]` — **instance** methods
- `[Before(Class)]` and above — **static** methods (compiler error if instance)
- `async void` not allowed anywhere — use `async Task` or `void`
- Multiple `[After(Test)]` methods are all guaranteed to run even if earlier ones throw; exceptions aggregate

## Context Types by Scope

```csharp
[Before(Test)]    public async Task Setup(TestContext context) { }
[Before(Class)]   public static async Task Setup(ClassHookContext context) { }
[Before(Assembly)] public static async Task Setup(AssemblyHookContext context) { }
[Before(TestSession)] public static async Task Setup(TestSessionContext context) { }
```

All hooks can also accept `CancellationToken` as a parameter (alone or alongside context):
```csharp
[Before(Test)]
public async Task Setup(TestContext context, CancellationToken ct)
{
    await SomeLongOperation(ct);
}
```

## Test-Level Hooks

```csharp
public class MyTests
{
    private HttpClient _client = null!;

    [Before(Test)]
    public async Task Setup()
    {
        _client = new HttpClient();
        await _client.GetAsync("https://localhost/ping");
    }

    [After(Test)]
    public async Task Cleanup()
    {
        _client.Dispose();
    }

    [After(Test)]
    public async Task CaptureOnFailure(TestContext context)
    {
        if (context.Execution.Result?.State == TestState.Failed)
            await TakeScreenshotAsync();
    }

    [Test]
    public async Task MyTest() { }
}
```

## Class-Level Hooks (once per class)

```csharp
public class DatabaseTests
{
    private static HttpResponseMessage? _healthCheck;

    [Before(Class)]
    public static async Task PingServer(ClassHookContext context)
    {
        _healthCheck = await new HttpClient().GetAsync("https://localhost/health");
    }

    [After(Class)]
    public static async Task KillChromeDrivers(ClassHookContext context)
    {
        foreach (var p in Process.GetProcessesByName("chromedriver"))
            p.Kill();
    }
}
```

## Global Hooks (BeforeEvery / AfterEvery)

Put in a dedicated file (e.g., `GlobalHooks.cs`). Applies to every test in the session:

```csharp
public static class GlobalHooks
{
    [BeforeEvery(Test)]
    public static void SetupTest(TestContext context)
    {
        Console.WriteLine($"Starting: {context.Metadata.TestName}");
    }

    [AfterEvery(Test)]
    public static async Task TeardownTest(TestContext context)
    {
        if (context.Execution.Result?.State == TestState.Failed)
            await LogFailureAsync(context);
    }
}
```

## Execution Order

For a single test:
1. `[BeforeEvery(Assembly)]` (once per assembly)
2. `[Before(Assembly)]` (once per assembly)
3. `[BeforeEvery(Class)]` (once per class)
4. `[Before(Class)]` (once per class)
5. `[BeforeEvery(Test)]`
6. `[Before(Test)]` — base class first, then derived
7. **Test runs**
8. `[After(Test)]` — derived first, then base
9. `[AfterEvery(Test)]`
10. `[After(Class)]` (last test in class)
11. `[AfterEvery(Class)]`
12. `[After(Assembly)]`
13. `[AfterEvery(Assembly)]`

## TestContext Properties

Available in `[Before(Test)]` / `[After(Test)]` hooks and inside test methods via `TestContext.Current`:

```csharp
TestContext.Current!.Metadata.TestName           // test method name
TestContext.Current!.Metadata.TestDetails        // full test metadata
TestContext.Current!.Execution.Result?.State     // TestState.Passed/Failed/Skipped (After only)
TestContext.Current!.Isolation.UniqueId          // int unique per test instance
TestContext.Current!.Isolation.GetIsolatedName("todos")   // "Test_42_todos"
TestContext.Current!.Isolation.GetIsolatedPrefix()        // "test_42_"
TestContext.Current!.Output.WriteLine("debug")
TestContext.Current!.Output.WriteError("warning")
TestContext.Current!.Output.AttachArtifact(new Artifact { ... })
```

## AsyncLocal Values

Set `AsyncLocal` in hooks and propagate to the test framework:

```csharp
private static readonly AsyncLocal<string> _tenantId = new();

[BeforeEvery(Class)]
public static void SetTenant(ClassHookContext context)
{
    _tenantId.Value = "test-tenant";
    context.AddAsyncLocalValues();   // propagates into test execution
}
```

## Common Mistakes

| Mistake | Fix |
|---------|-----|
| `[Before(Class)]` as instance method | Make it `static` |
| `[Before(Test)]` as static method | Make it instance |
| `async void` hook | Use `async Task` |
| `.Wait()` / `.Result` inside hook | Use `async Task` |
| Expensive setup in `[Before(Test)]` | Move to `[Before(Class)]` or `[ClassDataSource<T>]` |
