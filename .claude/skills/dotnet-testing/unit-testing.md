# Unit Testing with TUnit

## Test Class Structure

No `[TestClass]` attribute needed. Just `[Test]` on the method.

```csharp
public class CalculatorTests
{
    [Test]
    public async Task Add_TwoPositiveNumbers_ReturnsSum()
    {
        var calculator = new Calculator();
        var result = calculator.Add(2, 3);
        await Assert.That(result).IsEqualTo(5);
    }
}
```

**Method signatures:**
- `public async Task TestName()` — required when using `Assert.That()`
- `public void TestName()` — allowed for pure side-effect tests with no assertions
- `async void` → compile error `TUnit0031`

## Constructor Setup

TUnit creates a fresh class instance per test. Use the constructor for synchronous setup:

```csharp
public class OrderServiceTests
{
    private readonly IOrderRepository.Mock _repository;
    private readonly OrderService _sut;

    public OrderServiceTests()
    {
        _repository = IOrderRepository.Mock();
        _sut = new OrderService(_repository);
    }

    [Test]
    public async Task CreateOrder_ValidRequest_SavesOrder()
    {
        _repository.Save(Any<Order>()).Returns(Task.CompletedTask);

        await _sut.CreateOrderAsync(new CreateOrderRequest { ... });

        _repository.Save(Any<Order>()).WasCalled(Times.Once);
    }
}
```

For async setup, use `[Before(Test)]` — see [Hooks & Lifecycle](./hooks.md).

## Mocking with TUnit.Mocks

See [Mocking](./mocking.md) for the full reference. Quick patterns:

```csharp
// Create mock — mock IS the interface, no .Object needed
var mock = IMyService.Mock();

// Setup return value
mock.GetById(Any<Guid>()).Returns(new Entity { Id = Guid.NewGuid() });

// Setup async return
mock.GetByIdAsync(Any<Guid>()).Returns(new Entity { Id = Guid.NewGuid() });

// Setup throw
mock.Delete(Any<Guid>()).Throws<NotFoundException>();

// Verify call count
mock.GetById(someId).WasCalled(Times.Once);
mock.Delete(Any<Guid>()).WasNeverCalled();
```

**Requires C# 14 / .NET 10** for the `IMyInterface.Mock()` static extension syntax. Uses `Mock.Of<T>()` for older projects.

**TUnit.Mocks is beta** — add with `--prerelease`:
```xml
<PackageReference Include="TUnit.Mocks" Version="*" Prerelease="true" />
<LangVersion>14</LangVersion>
```

## Parameterized Tests

Use `[Arguments]` for inline data, `[MethodDataSource]` for complex data:

```csharp
[Test]
[Arguments(1, 1, 2)]
[Arguments(2, 3, 5)]
[Arguments(0, 0, 0, DisplayName = "Zero plus zero")]
public async Task Add_ProducesExpectedResult(int a, int b, int expected)
{
    await Assert.That(a + b).IsEqualTo(expected);
}
```

See [Data-Driven Tests](./data-driven.md) for `[Matrix]`, `[MethodDataSource]`, and `[ClassDataSource]`.

## Testing Validators (FluentValidation)

No mocks needed — instantiate directly:

```csharp
public class CreateProductValidatorTests
{
    private readonly CreateProductValidator _validator = new();

    [Test]
    public async Task Validate_EmptyTranslations_Fails()
    {
        var request = new CreateProductRequest { Translations = [] };
        var result = _validator.Validate(request);

        await Assert.That(result.IsValid).IsFalse();
        await Assert.That(result.Errors)
            .Contains(e => e.PropertyName == "Translations");
    }

    [Test]
    public async Task Validate_ValidRequest_Passes()
    {
        var request = new CreateProductRequest
        {
            BasePrice = 3.50m,
            Translations = [new("nl", "Frietjes", null)]
        };

        var result = _validator.Validate(request);
        await Assert.That(result.IsValid).IsTrue();
    }
}
```

## Testing Domain Logic / Pure Functions

```csharp
public class TaxCalculatorTests
{
    [Test]
    [Arguments(ConsumptionMode.TakeAway, 10.00, 0.06, DisplayName = "Takeaway: 6% VAT")]
    [Arguments(ConsumptionMode.EatIn, 10.00, 0.21, DisplayName = "Eat-in: 21% VAT")]
    public async Task Calculate_AppliesCorrectVatRate(
        ConsumptionMode mode, decimal net, decimal expectedRate)
    {
        var result = TaxCalculator.Calculate(net, mode);

        await Assert.That(result.VatRate).IsEqualTo(expectedRate);
        await Assert.That(result.NetAmount).IsEqualTo(net);
    }
}
```

## Naming Conventions

- **Class**: `{TypeUnderTest}Tests`
- **Method**: `{MethodOrScenario}_{Condition}_{ExpectedResult}`
- **Arrange / Act / Assert** structure in every test
- No logic in tests — no if/else, loops, or try/catch

## Organization

```
Tests.Unit/
  Products/
    CreateProductHandlerTests.cs
    CreateProductValidatorTests.cs
  TaxConfiguration/
    TaxCalculatorTests.cs
  Common/
    MoneyTests.cs
```
