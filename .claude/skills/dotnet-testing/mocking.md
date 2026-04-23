# TUnit.Mocks Reference

Source-generated, AOT-compatible mocking. Works with any test runner. Currently **beta**.

## Requirements

```xml
<PackageReference Include="TUnit.Mocks" Version="*" Prerelease="true" />
<LangVersion>14</LangVersion>   <!-- required for IMyInterface.Mock() syntax -->
```

Optional extensions:
```xml
<PackageReference Include="TUnit.Mocks.Http" Version="*" Prerelease="true" />
<PackageReference Include="TUnit.Mocks.Logging" Version="*" Prerelease="true" />
```

## Creating Mocks

```csharp
// Recommended — typed extension syntax (C# 14, .NET 10+)
var mock = IMyService.Mock();

// Fallback factory (works on all versions)
var mock = Mock.Of<IMyService>();

// Strict mode — throws on any unconfigured call
var strict = IMyService.Mock(MockBehavior.Strict);

// Partial mock — wraps a real instance
var partial = Mock.Wrap(new RealService());

// Delegate mocking
var funcMock = Mock.OfDelegate<Func<string, int>>();

// Multi-interface mock
var multi = Mock.Of<ILogger, IDisposable>();

// HTTP / Logging helpers
var httpClient = Mock.HttpClient();         // HttpClient with .Handler mock
var logger = Mock.Logger<MyService>();      // ILogger<MyService>
```

**`T.Mock()` returns a typed wrapper that IS the interface** — no `.Object` needed:
```csharp
var mock = IGreeter.Mock();
IGreeter greeter = mock;           // direct assignment
AcceptGreeter(mock);               // pass to methods directly
```

## Setup — Return Values

```csharp
// Fixed return
mock.GetUser(Any()).Returns(new User("Alice"));

// Computed return
mock.GetUser(Any()).Returns(() => new User(DateTime.Now.ToString()));

// Async methods — auto-wraps in Task<T>/ValueTask<T>
mock.GetUserAsync(Any()).Returns(new User("Alice"));

// Sequential returns
mock.GetValue(Any())
    .Throws<InvalidOperationException>()   // 1st call: throws
    .Then()
    .Returns("retry-ok")                   // 2nd call: returns
    .Then()
    .Returns("cached");                    // 3rd+ calls

// Shorthand for sequential values
mock.GetValue(Any()).ReturnsSequentially("first", "second", "third");
// Last value repeats for any remaining calls
```

## Setup — Void Methods & Callbacks

```csharp
// Callback (void or returning methods)
var callCount = 0;
mock.Process(Any()).Callback(() => callCount++);

// Callback with captured arguments
mock.Process(Any()).Callback((object?[] args) =>
    Console.WriteLine($"Called with: {args[0]}"));

// Throws
mock.Delete(Any()).Throws<NotFoundException>();
mock.Delete(Any()).Throws(new ArgumentException("bad id"));

// Void methods: Callback and Throws only (no Returns)
mock.Log(Any()).Callback(() => { });
```

## Properties

```csharp
// Getter (default)
mock.Name.Returns("Alice");
mock.Name.Getter.Returns("Alice");  // explicit, same as above

// Setter
mock.Count.Setter.Callback(() => Console.WriteLine("set!"));
mock.Count.Set(42).Callback(() => Console.WriteLine("set to 42"));
mock.Name.Setter.Throws<NotSupportedException>();

// Auto-tracking properties (setters store, getters return)
mock.SetupAllProperties();
mock.Object.Name = "Alice";
var name = mock.Object.Name;  // "Alice"
```

## Argument Matchers

No `Arg.` prefix needed — matchers are imported globally:

```csharp
// Any value of type T
mock.GetUser(Any<int>()).Returns(user);
mock.GetUser(Any()).Returns(user);          // type inferred

// Exact value (implicit)
mock.GetUser(42).Returns(alice);

// Predicate (inline lambda)
mock.GetUser(id => id > 0).Returns(validUser);
mock.Search(name => name.StartsWith("A"), Any()).Returns(results);

// Explicit predicate
mock.GetUser(Is<int>(id => id > 0)).Returns(validUser);

// Common matchers
Is<string>(s => s.Length > 3)
IsAny<T>()
IsNull<T>()
IsNotNull<T>()
```

## Verification

```csharp
// At least once (default)
mock.GetUser(42).WasCalled();

// Exact count
mock.GetUser(42).WasCalled(Times.Once);
mock.GetUser(42).WasCalled(Times.Exactly(3));

// Never called
mock.Delete(Any()).WasNeverCalled();
```

### Times values

| Expression | Matches |
|---|---|
| `Times.Once` | Exactly 1 |
| `Times.Never` | Exactly 0 |
| `Times.AtLeastOnce` | 1 or more |
| `Times.Exactly(n)` | Exactly n |
| `Times.AtLeast(n)` | n or more |
| `Times.AtMost(n)` | n or fewer |
| `Times.Between(min, max)` | Between min and max (inclusive) |

### TUnit assertion integration

```csharp
using TUnit.Mocks.Assertions;

await Assert.That(mock.GetUser(42)).WasCalled(Times.Once);
await Assert.That(mock.Delete(Any())).WasNeverCalled();
```

### Ordered verification

```csharp
Mock.VerifyInOrder(() =>
{
    mockLogger.Log("Starting").WasCalled();
    mockRepo.SaveAsync(Any()).WasCalled();
    mockLogger.Log("Done").WasCalled();
});
```

### VerifyAll / VerifyNoOtherCalls

```csharp
mock.VerifyAll();             // every setup was invoked at least once
mock.VerifyNoOtherCalls();    // all recorded calls have been verified
```

## Out / Ref Parameters

```csharp
// Out — excluded from setup signature, use SetsOut{Name}()
mock.TryGet("key")
    .Returns(true)
    .SetsOutValue("found-value");

// Ref — included in signature, use SetsRef{Name}()
mock.Swap(Any()).SetsRefValue(99);
```

## Pattern: Constructor Injection

```csharp
public class OrderServiceTests
{
    private readonly IOrderRepository.Mock _repo;
    private readonly OrderService _sut;

    public OrderServiceTests()
    {
        _repo = IOrderRepository.Mock();
        _sut = new OrderService(_repo);
    }

    [Test]
    public async Task CreateOrder_SavesToRepository()
    {
        _repo.SaveAsync(Any<Order>()).Returns(Task.CompletedTask);

        await _sut.CreateOrderAsync(new CreateOrderRequest());

        _repo.SaveAsync(Any<Order>()).WasCalled(Times.Once);
    }
}
```
