# TUnit Assertions Reference

All assertions use `await Assert.That(value).AssertionMethod()`.

**Critical: forgetting `await` causes a silent pass.** The TUnit Roslyn analyzer catches this at build time.

## Equality & Comparison

```csharp
await Assert.That(value).IsEqualTo(expected);
await Assert.That(value).IsNotEqualTo(other);
await Assert.That(value).IsGreaterThan(min);
await Assert.That(value).IsGreaterThanOrEqualTo(min);
await Assert.That(value).IsLessThan(max);
await Assert.That(value).IsLessThanOrEqualTo(max);
await Assert.That(value).IsBetween(min, max);
await Assert.That(a).IsSameReferenceAs(b);            // reference equality

// Floating-point tolerance
await Assert.That(3.14159).IsEqualTo(Math.PI).Within(0.001);

// Custom equality comparer
await Assert.That(obj).IsEqualTo(other, new MyComparer());
```

## Null & Default

```csharp
await Assert.That(obj).IsNull();
await Assert.That(obj).IsNotNull();
await Assert.That(value).IsDefault();      // == default(T)
await Assert.That(value).IsNotDefault();
```

## Boolean

```csharp
await Assert.That(condition).IsTrue();
await Assert.That(condition).IsFalse();
```

## Strings

```csharp
await Assert.That(str).IsEqualTo("expected");
await Assert.That(str).Contains("substring");
await Assert.That(str).DoesNotContain("substring");
await Assert.That(str).StartsWith("prefix");
await Assert.That(str).EndsWith("suffix");
await Assert.That(str).Matches(@"^\d+$");              // regex
await Assert.That(str).IsNotEmpty();
await Assert.That(str).IsEmpty();
await Assert.That(str).HasLength().EqualTo(10);
```

## Numbers

```csharp
await Assert.That(n).IsPositive();
await Assert.That(n).IsNegative();
await Assert.That(n).IsZero();
await Assert.That(n).IsNotZero();
```

## Collections

```csharp
await Assert.That(list).IsNotEmpty();
await Assert.That(list).IsEmpty();
await Assert.That(list).HasCount().EqualTo(3);
await Assert.That(list).HasSingleItem();
await Assert.That(list).Contains(item);
await Assert.That(list).DoesNotContain(item);
await Assert.That(list).Contains(x => x.Id == id);    // predicate
await Assert.That(list).All(x => x.IsActive);          // every element
await Assert.That(list).Any(x => x.Name == "Alice");   // at least one
await Assert.That(list).IsEquivalentTo(other);          // same items, any order
await Assert.That(list).HasDistinctItems();
await Assert.That(list).IsInOrder();
await Assert.That(list).IsOrderedBy(x => x.Name);
```

## Exceptions

```csharp
// Async method throws
await Assert.ThrowsAsync<InvalidOperationException>(
    async () => await service.DoSomethingAsync());

// Sync method throws
await Assert.Throws<ArgumentNullException>(() => sut.Method(null));

// Chain on message
await Assert.ThrowsAsync<ValidationException>(
    async () => await service.CreateAsync(request))
    .WithMessage("*Translations*");       // wildcard
    // OR:
    .WithMessageContaining("Translations");

// Nothing throws
await Assert.ThrowsNothingAsync(async () => await service.DoSomethingAsync());
```

## Type Checking

```csharp
await Assert.That(obj).IsTypeOf<MyClass>();
await Assert.That(obj).IsAssignableTo<IMyInterface>();
```

## DateTime

```csharp
await Assert.That(date).IsAfter(other);
await Assert.That(date).IsBefore(other);
await Assert.That(date).IsEqualTo(expected).Within(TimeSpan.FromSeconds(1));
```

## Chaining

### .And — all conditions must pass

```csharp
await Assert.That(product.Id).IsNotEqualTo(Guid.Empty)
    .And.IsNotDefault();

await Assert.That(str)
    .IsNotNull()
    .And.IsNotEmpty()
    .And.StartsWith("prefix");
```

### .Or — at least one must pass

```csharp
await Assert.That(statusCode)
    .IsEqualTo(HttpStatusCode.OK)
    .Or.IsEqualTo(HttpStatusCode.Created);
```

> Do not mix `.And` and `.Or` in a single chain — precedence is undefined.

## Multiple Assertions (report all failures)

Use `Assert.Multiple()` to run all assertions even when earlier ones fail:

```csharp
using (Assert.Multiple())
{
    await Assert.That(user.FirstName).IsEqualTo("John");
    await Assert.That(user.LastName).IsEqualTo("Doe");
    await Assert.That(user.Age).IsGreaterThan(18);
}
// All three are evaluated; failures are aggregated and thrown together
```

## Member Assertions

Assert on object properties while keeping the parent in context:

```csharp
await Assert.That(product)
    .Member(p => p.Id, id => id.IsNotEqualTo(Guid.Empty))
    .And.Member(p => p.BasePrice, price => price.IsGreaterThan(0m));

// Nested
await Assert.That(order)
    .Member(o => o.Customer,
        customer => customer
            .Member(c => c.Name, name => name.IsEqualTo("Alice"))
            .And.Member(c => c.Age, age => age.IsGreaterThan(18)));
```

## HTTP Response Shorthand (integration tests)

```csharp
await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Created);
await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.NoContent);
await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.NotFound);
await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.BadRequest);
await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Unauthorized);
await Assert.That(response.IsSuccessStatusCode).IsTrue();
```
