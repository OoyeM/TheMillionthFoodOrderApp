# Domain Event Dispatch Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Wire up domain event dispatch so that events raised by aggregates are published via Wolverine's `IMessageBus` immediately after the repository persists changes to the database.

**Architecture:** Domain events sit on aggregate instances (via `Entity<TId>.DomainEvents`) but are never dispatched today — the repositories just call `dbContext.SaveChangesAsync()` and return. We cannot use an EF Core `SaveChangesInterceptor` because Aspire registers both DbContexts with pooling (pooled DbContexts can't take scoped constructor dependencies like `IMessageBus`). Instead, each repository injects `IMessageBus` and dispatches events after the save (and after the commit for transactional methods). A non-generic `IHasDomainEvents` interface lets the ChangeTracker enumerate events without knowing `TId`.

**Tech Stack:** C# 13, EF Core 9, Wolverine (IMessageBus), TUnit (tests), Testcontainers.MsSql (integration tests)

---

## File Map

| Action | Path |
|--------|------|
| **Create** | `src/backend/TheMillionthFoodOrderApp.Domain/Common/IHasDomainEvents.cs` |
| **Modify** | `src/backend/TheMillionthFoodOrderApp.Domain/Common/Entity.cs` |
| **Create** | `src/backend/TheMillionthFoodOrderApp.Infrastructure/Persistence/DomainEventDispatcher.cs` |
| **Create** | `src/backend/TheMillionthFoodOrderApp.Tests.Integration/DomainEvents/BrandEventDispatchTests.cs` |
| **Modify** | `src/backend/TheMillionthFoodOrderApp.Infrastructure/Brands/BrandRepository.cs` |
| **Modify** | `src/backend/TheMillionthFoodOrderApp.Infrastructure/BrandSettings/BrandSettingsRepository.cs` |
| **Modify** | `src/backend/TheMillionthFoodOrderApp.Infrastructure/Identity/PlatformUserRepository.cs` |
| **Modify** | `src/backend/TheMillionthFoodOrderApp.Infrastructure/Shops/ShopRepository.cs` |
| **Modify** | `src/backend/TheMillionthFoodOrderApp.Infrastructure/Products/ProductRepository.cs` |
| **Modify** | `src/backend/TheMillionthFoodOrderApp.Infrastructure/MenuCategories/MenuCategoryRepository.cs` |
| **Modify** | `src/backend/TheMillionthFoodOrderApp.Infrastructure/ModifierGroups/ModifierGroupRepository.cs` |
| **Modify** | `src/backend/TheMillionthFoodOrderApp.Infrastructure/OrderLifecycle/OrderLifecycleConfigRepository.cs` |
| **Modify** | `src/backend/TheMillionthFoodOrderApp.Infrastructure/TaxConfiguration/TaxConfigurationRepository.cs` |

---

## Task 1: Add `IHasDomainEvents` and update `Entity<TId>`

**Files:**
- Create: `src/backend/TheMillionthFoodOrderApp.Domain/Common/IHasDomainEvents.cs`
- Modify: `src/backend/TheMillionthFoodOrderApp.Domain/Common/Entity.cs`

`Entity<TId>` already has `DomainEvents` and `ClearDomainEvents()` but is generic — EF Core's ChangeTracker can't enumerate `Entries<Entity<TId>>()` across differing TId types. A non-generic interface fixes this.

- [ ] **Step 1: Create `IHasDomainEvents`**

Create `src/backend/TheMillionthFoodOrderApp.Domain/Common/IHasDomainEvents.cs`:

```csharp
namespace TheMillionthFoodOrderApp.Domain.Common;

public interface IHasDomainEvents
{
    IReadOnlyCollection<IDomainEvent> DomainEvents { get; }
    void ClearDomainEvents();
}
```

- [ ] **Step 2: Implement the interface on `Entity<TId>`**

Open `src/backend/TheMillionthFoodOrderApp.Domain/Common/Entity.cs`. Change:

```csharp
public abstract class Entity<TId> where TId : notnull
```

to:

```csharp
public abstract class Entity<TId> : IHasDomainEvents where TId : notnull
```

No other changes — the class already has `DomainEvents` and `ClearDomainEvents()`, which satisfy the interface.

- [ ] **Step 3: Verify build**

```bash
cd src/backend && dotnet build TheMillionthFoodOrderApp.slnx
```

Expected: build succeeds with no errors.

- [ ] **Step 4: Commit**

```bash
git add src/backend/TheMillionthFoodOrderApp.Domain/Common/IHasDomainEvents.cs \
        src/backend/TheMillionthFoodOrderApp.Domain/Common/Entity.cs
git commit -m "feat(domain): add IHasDomainEvents interface for non-generic event enumeration"
```

---

## Task 2: Create `DomainEventDispatcher` helper

**Files:**
- Create: `src/backend/TheMillionthFoodOrderApp.Infrastructure/Persistence/DomainEventDispatcher.cs`

This static helper centralises the two-step pattern: collect+clear events from tracked entities, then publish them. Keeping it static and internal means repositories call one method instead of duplicating the ChangeTracker query.

The dispatch uses `(dynamic)@event` to resolve the concrete type at runtime so Wolverine's generic `PublishAsync<T>` receives the actual event type (e.g. `BrandCreatedEvent`) rather than `IDomainEvent`. Wolverine routes based on the concrete type, so this is required for handlers to be invoked.

- [ ] **Step 1: Create `DomainEventDispatcher.cs`**

```csharp
using Microsoft.EntityFrameworkCore;
using TheMillionthFoodOrderApp.Domain.Common;
using Wolverine;

namespace TheMillionthFoodOrderApp.Infrastructure.Persistence;

internal static class DomainEventDispatcher
{
    /// <summary>
    /// Collects all domain events from EF Core-tracked entities and clears them
    /// from the aggregates. Call this AFTER the last mutation and BEFORE SaveChangesAsync
    /// so that entities are still in the change tracker.
    /// </summary>
    internal static List<IDomainEvent> CollectAndClear(DbContext context)
    {
        var events = new List<IDomainEvent>();

        foreach (var entry in context.ChangeTracker.Entries<IHasDomainEvents>())
        {
            events.AddRange(entry.Entity.DomainEvents);
            entry.Entity.ClearDomainEvents();
        }

        return events;
    }

    /// <summary>
    /// Publishes each collected event via Wolverine. Call this AFTER SaveChangesAsync
    /// (and after CommitAsync for transactional methods) so events are only dispatched
    /// once the database change is durable.
    /// </summary>
    internal static async Task PublishAsync(IEnumerable<IDomainEvent> events, IMessageBus bus)
    {
        foreach (var @event in events)
            await bus.PublishAsync((dynamic)@event);
    }
}
```

- [ ] **Step 2: Verify build**

```bash
cd src/backend && dotnet build TheMillionthFoodOrderApp.slnx
```

Expected: build succeeds.

- [ ] **Step 3: Commit**

```bash
git add src/backend/TheMillionthFoodOrderApp.Infrastructure/Persistence/DomainEventDispatcher.cs
git commit -m "feat(infra): add DomainEventDispatcher helper for collect-and-publish pattern"
```

---

## Task 3: Write the failing integration test (TDD)

**Files:**
- Create: `src/backend/TheMillionthFoodOrderApp.Tests.Integration/DomainEvents/BrandEventDispatchTests.cs`

This test directly instantiates `BrandRepository` with a real `PlatformDbContext` (pointing at the Testcontainer) and a spy `IMessageBus`. It verifies that saving a new brand dispatches `BrandCreatedEvent`. The test is written now, before the repository is updated — it must fail first.

- [ ] **Step 1: Create the spy `IMessageBus`**

Create `src/backend/TheMillionthFoodOrderApp.Tests.Integration/DomainEvents/BrandEventDispatchTests.cs`:

```csharp
using Microsoft.EntityFrameworkCore;
using TheMillionthFoodOrderApp.Domain.Brands;
using TheMillionthFoodOrderApp.Infrastructure.Brands;
using TheMillionthFoodOrderApp.Infrastructure.Persistence;
using TheMillionthFoodOrderApp.Tests.Integration.Fixtures;
using Wolverine;

namespace TheMillionthFoodOrderApp.Tests.Integration.DomainEvents;

[ClassDataSource<IntegrationTestBase>(Shared = SharedType.PerClass)]
public sealed class BrandEventDispatchTests(IntegrationTestBase fixture)
{
    private PlatformDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<PlatformDbContext>()
            .UseSqlServer(fixture.PlatformConnectionString)
            .Options;
        return new PlatformDbContext(options);
    }

    [Test]
    public async Task SaveChangesAsync_publishes_BrandCreatedEvent_after_brand_is_added()
    {
        // Arrange
        var spy = new SpyMessageBus();
        await using var dbContext = CreateDbContext();
        var repository = new BrandRepository(dbContext, spy);

        var brand = Brand.Create("Dispatch Test Brand", "dispatch-test", "dispatch@test.com", null);
        repository.Add(brand);

        // Act
        await repository.SaveChangesAsync(CancellationToken.None);

        // Assert — one BrandCreatedEvent published
        await Assert.That(spy.Published.Count).IsEqualTo(1);
        await Assert.That(spy.Published[0]).IsTypeOf<BrandCreatedEvent>();

        var published = (BrandCreatedEvent)spy.Published[0];
        await Assert.That(published.Slug).IsEqualTo("dispatch-test");
    }

    [Test]
    public async Task SaveChangesAsync_does_not_publish_events_when_no_events_raised()
    {
        // Arrange — load an existing brand and read it (no mutation = no events)
        var spy = new SpyMessageBus();
        await using var dbContext = CreateDbContext();
        var repository = new BrandRepository(dbContext, spy);

        // The alpha brand was seeded by IntegrationTestBase without going through the repository,
        // so no pending events exist on the tracked entity.
        var existing = await dbContext.Brands.FirstAsync(b => b.Slug == IntegrationTestBase.AlphaSlug);

        // Act — save with no domain events raised
        await repository.SaveChangesAsync(CancellationToken.None);

        // Assert
        await Assert.That(spy.Published.Count).IsEqualTo(0);
    }
}
```

Then add the spy class at the bottom of the same file (or in a separate file in the same folder):

```csharp
/// <summary>
/// Test spy for IMessageBus. Records all messages passed to PublishAsync.
/// Use your IDE to generate the remaining IMessageBus interface members
/// with `throw new NotImplementedException()`.
/// </summary>
internal sealed class SpyMessageBus : IMessageBus
{
    public List<object> Published { get; } = [];

    public ValueTask PublishAsync<T>(T message, DeliveryOptions? options = null) where T : class
    {
        Published.Add(message);
        return ValueTask.CompletedTask;
    }

    // Generate remaining IMessageBus members via IDE → throw new NotImplementedException()
    // (SendAsync, InvokeAsync, InvokeAsync<T>, PreviewSubscriptions, EndpointFor, etc.)
}
```

> **Note:** Use your IDE's "implement interface" action on `SpyMessageBus` to generate the remaining `IMessageBus` members. Set each generated body to `throw new NotImplementedException()`. Only `PublishAsync<T>` needs a real implementation.

- [ ] **Step 2: Verify the test file compiles**

```bash
cd src/backend && dotnet build TheMillionthFoodOrderApp.slnx
```

Expected: build succeeds. If `BrandRepository` constructor doesn't yet accept `IMessageBus`, you'll see a compile error — that's expected. Fix it by temporarily adding the parameter to `BrandRepository` with an empty implementation, run the test to confirm FAIL, then revert before Task 4.

Actually — since `BrandRepository` doesn't yet accept `IMessageBus`, the test won't compile. Add a temporary second constructor or add the parameter now with an empty body, just enough to compile:

Temporary change to `BrandRepository.cs` (revert after seeing the test fail):
```csharp
public sealed class BrandRepository(PlatformDbContext dbContext, IMessageBus messageBus) : IBrandRepository
{
    // ... existing methods unchanged ...

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
        => await dbContext.SaveChangesAsync(cancellationToken); // NOT YET dispatching
}
```

- [ ] **Step 3: Run the test and confirm it FAILS**

```bash
cd src/backend/TheMillionthFoodOrderApp.Tests.Integration && dotnet run -c Release
```

Expected: `SaveChangesAsync_publishes_BrandCreatedEvent_after_brand_is_added` **FAILS** because `spy.Published.Count` is 0 (no dispatch yet). The second test passes.

- [ ] **Step 4: Commit the failing test**

```bash
git add src/backend/TheMillionthFoodOrderApp.Tests.Integration/DomainEvents/BrandEventDispatchTests.cs
git commit -m "test(domain-events): add failing integration test for BrandCreatedEvent dispatch"
```

---

## Task 4: Update `BrandRepository` — make the test pass

**Files:**
- Modify: `src/backend/TheMillionthFoodOrderApp.Infrastructure/Brands/BrandRepository.cs`

The simplest dispatch case: no transactions, one `SaveChangesAsync`. Collect → save → publish.

- [ ] **Step 1: Add `IMessageBus` injection and update `SaveChangesAsync`**

Open `src/backend/TheMillionthFoodOrderApp.Infrastructure/Brands/BrandRepository.cs`.

Change the class declaration from:
```csharp
public sealed class BrandRepository(PlatformDbContext dbContext) : IBrandRepository
```

to:
```csharp
public sealed class BrandRepository(PlatformDbContext dbContext, IMessageBus messageBus) : IBrandRepository
```

Add the using at the top:
```csharp
using TheMillionthFoodOrderApp.Infrastructure.Persistence;
using Wolverine;
```

Change `SaveChangesAsync` from:
```csharp
public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    => await dbContext.SaveChangesAsync(cancellationToken);
```

to:
```csharp
public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
{
    var events = DomainEventDispatcher.CollectAndClear(dbContext);
    await dbContext.SaveChangesAsync(cancellationToken);
    await DomainEventDispatcher.PublishAsync(events, messageBus);
}
```

- [ ] **Step 2: Run the test and confirm it PASSES**

```bash
cd src/backend/TheMillionthFoodOrderApp.Tests.Integration && dotnet run -c Release
```

Expected: both `BrandEventDispatchTests` tests **PASS**. Run the full suite to check nothing regressed.

- [ ] **Step 3: Commit**

```bash
git add src/backend/TheMillionthFoodOrderApp.Infrastructure/Brands/BrandRepository.cs
git commit -m "feat(infra): dispatch domain events from BrandRepository.SaveChangesAsync"
```

---

## Task 5: Update `BrandSettingsRepository` and `PlatformUserRepository`

**Files:**
- Modify: `src/backend/TheMillionthFoodOrderApp.Infrastructure/BrandSettings/BrandSettingsRepository.cs`
- Modify: `src/backend/TheMillionthFoodOrderApp.Infrastructure/Identity/PlatformUserRepository.cs`

Both follow the simple pattern. `PlatformUserRepository` has an extra inline `SaveChangesAsync` call inside `AddOrGetExistingAsync` — events should only be dispatched when a new user is actually created (the `try` branch), not when the `catch` branch runs (concurrent insert, no domain events to dispatch).

- [ ] **Step 1: Update `BrandSettingsRepository`**

Open `src/backend/TheMillionthFoodOrderApp.Infrastructure/BrandSettings/BrandSettingsRepository.cs`.

Add to the class declaration: `IMessageBus messageBus` parameter.
Add usings for `Wolverine` and `TheMillionthFoodOrderApp.Infrastructure.Persistence`.

Change `SaveChangesAsync`:
```csharp
public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
{
    var events = DomainEventDispatcher.CollectAndClear(dbContext);
    await dbContext.SaveChangesAsync(cancellationToken);
    await DomainEventDispatcher.PublishAsync(events, messageBus);
}
```

- [ ] **Step 2: Update `PlatformUserRepository`**

Open `src/backend/TheMillionthFoodOrderApp.Infrastructure/Identity/PlatformUserRepository.cs`.

Add `IMessageBus messageBus` parameter to the class declaration.
Add usings for `Wolverine` and `TheMillionthFoodOrderApp.Infrastructure.Persistence`.

Change `SaveChangesAsync` (the public method):
```csharp
public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
{
    var events = DomainEventDispatcher.CollectAndClear(dbContext);
    await dbContext.SaveChangesAsync(cancellationToken);
    await DomainEventDispatcher.PublishAsync(events, messageBus);
}
```

Also update the inline call inside `AddOrGetExistingAsync`. The `try` block creates a new user — if it succeeds, dispatch the events. The `catch` block handles a concurrent insert race; no domain events to dispatch there.

Change the `try` block from:
```csharp
try
{
    await dbContext.PlatformUsers.AddAsync(user, cancellationToken);
    await dbContext.SaveChangesAsync(cancellationToken);
    return (user, true);
}
catch (DbUpdateException ex) when (...)
{
    ...
}
```

to:
```csharp
try
{
    await dbContext.PlatformUsers.AddAsync(user, cancellationToken);
    var events = DomainEventDispatcher.CollectAndClear(dbContext);
    await dbContext.SaveChangesAsync(cancellationToken);
    await DomainEventDispatcher.PublishAsync(events, messageBus);
    return (user, true);
}
catch (DbUpdateException ex) when (
    ex.InnerException is Microsoft.Data.SqlClient.SqlException { Number: 2601 or 2627 })
{
    dbContext.Entry(user).State = EntityState.Detached;
    var existing = await dbContext.PlatformUsers
        .FirstOrDefaultAsync(u => u.ExternalIdentityId == user.ExternalIdentityId, cancellationToken);
    return (existing!, false);
}
```

- [ ] **Step 3: Build and run tests**

```bash
cd src/backend && dotnet build TheMillionthFoodOrderApp.slnx
cd TheMillionthFoodOrderApp.Tests.Integration && dotnet run -c Release
```

Expected: build succeeds, all tests pass.

- [ ] **Step 4: Commit**

```bash
git add src/backend/TheMillionthFoodOrderApp.Infrastructure/BrandSettings/BrandSettingsRepository.cs \
        src/backend/TheMillionthFoodOrderApp.Infrastructure/Identity/PlatformUserRepository.cs
git commit -m "feat(infra): dispatch domain events from BrandSettings and PlatformUser repositories"
```

---

## Task 6: Update `ShopRepository`

**Files:**
- Modify: `src/backend/TheMillionthFoodOrderApp.Infrastructure/Shops/ShopRepository.cs`

`ShopRepository` has two methods that persist:
1. `UpdateAsync` — simple update, no explicit transaction
2. `ReplaceOpeningHoursAsync` — uses `BeginTransactionAsync`. Events must be dispatched **after** `CommitAsync`, not after `SaveChangesAsync`, to avoid publishing events for a transaction that wasn't committed.

For the transactional method, collect events **after** `mutate(shop)` (the mutation raises events) but **before** `SaveChangesAsync`, then publish only after `CommitAsync`.

- [ ] **Step 1: Add `IMessageBus` injection**

Open `src/backend/TheMillionthFoodOrderApp.Infrastructure/Shops/ShopRepository.cs`.

Add `IMessageBus messageBus` to the class declaration.
Add usings for `Wolverine` and `TheMillionthFoodOrderApp.Infrastructure.Persistence`.

- [ ] **Step 2: Update `SaveChangesAsync`**

```csharp
public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
{
    var events = DomainEventDispatcher.CollectAndClear(dbContext);
    await dbContext.SaveChangesAsync(cancellationToken);
    await DomainEventDispatcher.PublishAsync(events, messageBus);
}
```

- [ ] **Step 3: Update `UpdateAsync`**

`UpdateAsync` calls `SaveChangesAsync` directly (no transaction) — wrap the save:

```csharp
public async Task<Shop?> UpdateAsync(Guid id, Action<Shop> mutate, CancellationToken cancellationToken = default)
{
    var shop = await dbContext.Shops
        .Include(s => s.OpeningHours)
        .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

    if (shop is null)
        return null;

    mutate(shop);

    var events = DomainEventDispatcher.CollectAndClear(dbContext);
    await dbContext.SaveChangesAsync(cancellationToken);
    await DomainEventDispatcher.PublishAsync(events, messageBus);

    return shop;
}
```

- [ ] **Step 4: Update `ReplaceOpeningHoursAsync`**

Collect events after `mutate(shop)`, publish after `CommitAsync`:

```csharp
public async Task<Shop?> ReplaceOpeningHoursAsync(Guid shopId, Action<Shop> mutate, CancellationToken cancellationToken = default)
{
    await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

    await dbContext.OpeningHoursTimeBlocks
        .Where(b => b.ShopId == shopId)
        .ExecuteDeleteAsync(cancellationToken);

    dbContext.ChangeTracker.Clear();

    var shop = await dbContext.Shops
        .FirstOrDefaultAsync(s => s.Id == shopId, cancellationToken);

    if (shop is null)
    {
        await transaction.RollbackAsync(cancellationToken);
        return null;
    }

    mutate(shop);

    await dbContext.OpeningHoursTimeBlocks.AddRangeAsync(shop.OpeningHours, cancellationToken);

    var events = DomainEventDispatcher.CollectAndClear(dbContext);
    await dbContext.SaveChangesAsync(cancellationToken);
    await transaction.CommitAsync(cancellationToken);
    await DomainEventDispatcher.PublishAsync(events, messageBus);

    return shop;
}
```

> **Why collect before save?** After `ChangeTracker.Clear()` and `FirstAsync`, `shop` is tracked. `mutate(shop)` raises events on it. `CollectAndClear` enumerates the change tracker — `shop` is there. After `SaveChangesAsync` the entity state changes but the list `events` already holds the collected values.

- [ ] **Step 5: Build and run tests**

```bash
cd src/backend && dotnet build TheMillionthFoodOrderApp.slnx
cd TheMillionthFoodOrderApp.Tests.Integration && dotnet run -c Release
```

- [ ] **Step 6: Commit**

```bash
git add src/backend/TheMillionthFoodOrderApp.Infrastructure/Shops/ShopRepository.cs
git commit -m "feat(infra): dispatch domain events from ShopRepository"
```

---

## Task 7: Update `ProductRepository`

**Files:**
- Modify: `src/backend/TheMillionthFoodOrderApp.Infrastructure/Products/ProductRepository.cs`

Same pattern as `ShopRepository`: simple `UpdateScalarAsync` (no transaction) and transactional `UpdateAsync` (translations + combo items replacement).

- [ ] **Step 1: Add `IMessageBus messageBus` to class declaration**

Add usings for `Wolverine` and `TheMillionthFoodOrderApp.Infrastructure.Persistence`.

- [ ] **Step 2: Update `SaveChangesAsync`**

```csharp
public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
{
    var events = DomainEventDispatcher.CollectAndClear(dbContext);
    await dbContext.SaveChangesAsync(cancellationToken);
    await DomainEventDispatcher.PublishAsync(events, messageBus);
}
```

- [ ] **Step 3: Update `UpdateScalarAsync`**

```csharp
public async Task<Product?> UpdateScalarAsync(Guid id, Action<Product> mutate, CancellationToken cancellationToken = default)
{
    var product = await dbContext.Products
        .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    if (product is null)
        return null;

    mutate(product);

    var events = DomainEventDispatcher.CollectAndClear(dbContext);
    await dbContext.SaveChangesAsync(cancellationToken);
    await DomainEventDispatcher.PublishAsync(events, messageBus);

    return product;
}
```

- [ ] **Step 4: Update `UpdateAsync` (transactional)**

```csharp
public async Task<Product?> UpdateAsync(Guid id, Action<Product> mutate, CancellationToken cancellationToken = default)
{
    var product = await dbContext.Products
        .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    if (product is null)
        return null;

    await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

    await dbContext.ProductTranslations
        .Where(t => t.ProductId == id)
        .ExecuteDeleteAsync(cancellationToken);

    await dbContext.ComboItems
        .Where(ci => ci.ComboProductId == id)
        .ExecuteDeleteAsync(cancellationToken);

    mutate(product);

    dbContext.ProductTranslations.AddRange(product.Translations);

    if (product.ComboItems.Count > 0)
        dbContext.ComboItems.AddRange(product.ComboItems);

    var events = DomainEventDispatcher.CollectAndClear(dbContext);
    await dbContext.SaveChangesAsync(cancellationToken);
    await transaction.CommitAsync(cancellationToken);
    await DomainEventDispatcher.PublishAsync(events, messageBus);

    return product;
}
```

- [ ] **Step 5: Build and run tests**

```bash
cd src/backend && dotnet build TheMillionthFoodOrderApp.slnx
cd TheMillionthFoodOrderApp.Tests.Integration && dotnet run -c Release
```

- [ ] **Step 6: Commit**

```bash
git add src/backend/TheMillionthFoodOrderApp.Infrastructure/Products/ProductRepository.cs
git commit -m "feat(infra): dispatch domain events from ProductRepository"
```

---

## Task 8: Update `MenuCategoryRepository`

**Files:**
- Modify: `src/backend/TheMillionthFoodOrderApp.Infrastructure/MenuCategories/MenuCategoryRepository.cs`

Same two-pattern structure as ProductRepository: `UpdateScalarAsync` (no transaction) and `UpdateAsync` (transactional translation replacement).

- [ ] **Step 1: Add `IMessageBus messageBus` to class declaration**

Add usings for `Wolverine` and `TheMillionthFoodOrderApp.Infrastructure.Persistence`.

- [ ] **Step 2: Update `SaveChangesAsync`**

```csharp
public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
{
    var events = DomainEventDispatcher.CollectAndClear(dbContext);
    await dbContext.SaveChangesAsync(cancellationToken);
    await DomainEventDispatcher.PublishAsync(events, messageBus);
}
```

- [ ] **Step 3: Update `UpdateScalarAsync`**

```csharp
public async Task<MenuCategory?> UpdateScalarAsync(Guid id, Action<MenuCategory> mutate, CancellationToken cancellationToken = default)
{
    var category = await dbContext.MenuCategories
        .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

    if (category is null)
        return null;

    mutate(category);

    var events = DomainEventDispatcher.CollectAndClear(dbContext);
    await dbContext.SaveChangesAsync(cancellationToken);
    await DomainEventDispatcher.PublishAsync(events, messageBus);

    return category;
}
```

- [ ] **Step 4: Update `UpdateAsync` (transactional)**

```csharp
public async Task<MenuCategory?> UpdateAsync(Guid id, Action<MenuCategory> mutate, CancellationToken cancellationToken = default)
{
    var category = await dbContext.MenuCategories
        .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

    if (category is null)
        return null;

    await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

    await dbContext.MenuCategoryTranslations
        .Where(t => t.MenuCategoryId == id)
        .ExecuteDeleteAsync(cancellationToken);

    mutate(category);

    dbContext.MenuCategoryTranslations.AddRange(category.Translations);

    var events = DomainEventDispatcher.CollectAndClear(dbContext);
    await dbContext.SaveChangesAsync(cancellationToken);
    await transaction.CommitAsync(cancellationToken);
    await DomainEventDispatcher.PublishAsync(events, messageBus);

    return category;
}
```

- [ ] **Step 5: Build and run tests**

```bash
cd src/backend && dotnet build TheMillionthFoodOrderApp.slnx
cd TheMillionthFoodOrderApp.Tests.Integration && dotnet run -c Release
```

- [ ] **Step 6: Commit**

```bash
git add src/backend/TheMillionthFoodOrderApp.Infrastructure/MenuCategories/MenuCategoryRepository.cs
git commit -m "feat(infra): dispatch domain events from MenuCategoryRepository"
```

---

## Task 9: Update `ModifierGroupRepository`

**Files:**
- Modify: `src/backend/TheMillionthFoodOrderApp.Infrastructure/ModifierGroups/ModifierGroupRepository.cs`

The most complex repository — three save sites:
1. `SaveChangesAsync` (public) — simple case
2. `SoftDeleteAsync` — inline save, no transaction
3. `UpdateAsync` — deeply nested transactional replace (3-level child delete + re-insert)
4. `SetProductModifierGroupsAsync` — transactional join table replacement; `ProductModifierGroup` is a value-object-like entity that likely doesn't raise domain events, but include dispatch for consistency

- [ ] **Step 1: Add `IMessageBus messageBus` to class declaration**

Add usings for `Wolverine` and `TheMillionthFoodOrderApp.Infrastructure.Persistence`.

- [ ] **Step 2: Update `SaveChangesAsync`**

```csharp
public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
{
    var events = DomainEventDispatcher.CollectAndClear(dbContext);
    await dbContext.SaveChangesAsync(cancellationToken);
    await DomainEventDispatcher.PublishAsync(events, messageBus);
}
```

- [ ] **Step 3: Update `SoftDeleteAsync`**

```csharp
public async Task<bool> SoftDeleteAsync(Guid id, CancellationToken cancellationToken = default)
{
    var group = await dbContext.ModifierGroups
        .FirstOrDefaultAsync(g => g.Id == id, cancellationToken);

    if (group is null)
        return false;

    group.SoftDelete();

    var events = DomainEventDispatcher.CollectAndClear(dbContext);
    await dbContext.SaveChangesAsync(cancellationToken);
    await DomainEventDispatcher.PublishAsync(events, messageBus);

    return true;
}
```

- [ ] **Step 4: Update `UpdateAsync` (transactional)**

```csharp
public async Task<ModifierGroup?> UpdateAsync(Guid id, Action<ModifierGroup> mutate, CancellationToken cancellationToken = default)
{
    var group = await dbContext.ModifierGroups
        .FirstOrDefaultAsync(g => g.Id == id, cancellationToken);

    if (group is null)
        return null;

    await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

    var modifierIds = await dbContext.Modifiers
        .Where(m => EF.Property<Guid>(m, "ModifierGroupId") == id)
        .Select(m => m.Id)
        .ToListAsync(cancellationToken);

    if (modifierIds.Count > 0)
    {
        await dbContext.ModifierTranslations
            .Where(t => modifierIds.Contains(t.ModifierId))
            .ExecuteDeleteAsync(cancellationToken);
    }

    await dbContext.Modifiers
        .Where(m => EF.Property<Guid>(m, "ModifierGroupId") == id)
        .ExecuteDeleteAsync(cancellationToken);

    await dbContext.ModifierGroupTranslations
        .Where(t => t.ModifierGroupId == id)
        .ExecuteDeleteAsync(cancellationToken);

    mutate(group);

    dbContext.ModifierGroupTranslations.AddRange(group.Translations);
    foreach (var modifier in group.Modifiers)
    {
        dbContext.Modifiers.Add(modifier);
        dbContext.ModifierTranslations.AddRange(modifier.Translations);
    }

    var events = DomainEventDispatcher.CollectAndClear(dbContext);
    await dbContext.SaveChangesAsync(cancellationToken);
    await transaction.CommitAsync(cancellationToken);
    await DomainEventDispatcher.PublishAsync(events, messageBus);

    return group;
}
```

- [ ] **Step 5: Update `SetProductModifierGroupsAsync` (transactional)**

```csharp
public async Task SetProductModifierGroupsAsync(
    Guid productId,
    IEnumerable<(Guid modifierGroupId, int sortOrder)> assignments,
    CancellationToken cancellationToken = default)
{
    await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

    await dbContext.ProductModifierGroups
        .Where(pmg => pmg.ProductId == productId)
        .ExecuteDeleteAsync(cancellationToken);

    foreach (var (modifierGroupId, sortOrder) in assignments)
    {
        var pmg = ProductModifierGroup.Create(productId, modifierGroupId, sortOrder);
        await dbContext.ProductModifierGroups.AddAsync(pmg, cancellationToken);
    }

    var events = DomainEventDispatcher.CollectAndClear(dbContext);
    await dbContext.SaveChangesAsync(cancellationToken);
    await transaction.CommitAsync(cancellationToken);
    await DomainEventDispatcher.PublishAsync(events, messageBus);
}
```

- [ ] **Step 6: Build and run tests**

```bash
cd src/backend && dotnet build TheMillionthFoodOrderApp.slnx
cd TheMillionthFoodOrderApp.Tests.Integration && dotnet run -c Release
```

- [ ] **Step 7: Commit**

```bash
git add src/backend/TheMillionthFoodOrderApp.Infrastructure/ModifierGroups/ModifierGroupRepository.cs
git commit -m "feat(infra): dispatch domain events from ModifierGroupRepository"
```

---

## Task 10: Update `OrderLifecycleConfigRepository` and `TaxConfigurationRepository`

**Files:**
- Modify: `src/backend/TheMillionthFoodOrderApp.Infrastructure/OrderLifecycle/OrderLifecycleConfigRepository.cs`
- Modify: `src/backend/TheMillionthFoodOrderApp.Infrastructure/TaxConfiguration/TaxConfigurationRepository.cs`

Both follow the `ReplaceAsync` / `ReplaceRatesAsync` pattern from CLAUDE.md. Both use `ChangeTracker.Clear()` then reload, then mutate. Collect after mutation, publish after commit.

- [ ] **Step 1: Update `OrderLifecycleConfigRepository`**

Add `IMessageBus messageBus` to the class declaration.
Add usings for `Wolverine` and `TheMillionthFoodOrderApp.Infrastructure.Persistence`.

Update `SaveChangesAsync`:
```csharp
public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
{
    var events = DomainEventDispatcher.CollectAndClear(dbContext);
    await dbContext.SaveChangesAsync(cancellationToken);
    await DomainEventDispatcher.PublishAsync(events, messageBus);
}
```

Update `ReplaceAsync`:
```csharp
public async Task<OrderLifecycleConfig> ReplaceAsync(Guid configId, Action<OrderLifecycleConfig> mutate, CancellationToken cancellationToken = default)
{
    await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

    await dbContext.OrderStatusTransitions
        .Where(t => t.OrderLifecycleConfigId == configId)
        .ExecuteDeleteAsync(cancellationToken);

    await dbContext.OrderStatuses
        .Where(s => s.OrderLifecycleConfigId == configId)
        .ExecuteDeleteAsync(cancellationToken);

    dbContext.ChangeTracker.Clear();

    var config = await dbContext.OrderLifecycleConfigs
        .FirstAsync(c => c.Id == configId, cancellationToken);

    mutate(config);

    await dbContext.OrderStatuses.AddRangeAsync(config.Statuses, cancellationToken);
    dbContext.OrderStatusTransitions.AddRange(config.Transitions);

    var events = DomainEventDispatcher.CollectAndClear(dbContext);
    await dbContext.SaveChangesAsync(cancellationToken);
    await transaction.CommitAsync(cancellationToken);
    await DomainEventDispatcher.PublishAsync(events, messageBus);

    return config;
}
```

- [ ] **Step 2: Update `TaxConfigurationRepository`**

Add `IMessageBus messageBus` to the class declaration.
Add usings for `Wolverine` and `TheMillionthFoodOrderApp.Infrastructure.Persistence`.

Update `SaveChangesAsync`:
```csharp
public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
{
    var events = DomainEventDispatcher.CollectAndClear(dbContext);
    await dbContext.SaveChangesAsync(cancellationToken);
    await DomainEventDispatcher.PublishAsync(events, messageBus);
}
```

Update `ReplaceRatesAsync`:
```csharp
public async Task<Domain.TaxConfiguration.TaxConfiguration> ReplaceRatesAsync(
    Guid configId,
    Action<Domain.TaxConfiguration.TaxConfiguration> mutate,
    CancellationToken cancellationToken = default)
{
    await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

    await dbContext.VatRates
        .Where(v => v.TaxConfigurationId == configId)
        .ExecuteDeleteAsync(cancellationToken);

    dbContext.ChangeTracker.Clear();

    var config = await dbContext.TaxConfigurations
        .FirstAsync(c => c.Id == configId, cancellationToken);

    mutate(config);

    await dbContext.VatRates.AddRangeAsync(config.VatRates, cancellationToken);

    var events = DomainEventDispatcher.CollectAndClear(dbContext);
    await dbContext.SaveChangesAsync(cancellationToken);
    await transaction.CommitAsync(cancellationToken);
    await DomainEventDispatcher.PublishAsync(events, messageBus);

    return config;
}
```

- [ ] **Step 3: Build and run full test suite**

```bash
cd src/backend && dotnet build TheMillionthFoodOrderApp.slnx
cd TheMillionthFoodOrderApp.Tests.Integration && dotnet run -c Release
```

Expected: all tests pass, no regressions.

- [ ] **Step 4: Commit**

```bash
git add src/backend/TheMillionthFoodOrderApp.Infrastructure/OrderLifecycle/OrderLifecycleConfigRepository.cs \
        src/backend/TheMillionthFoodOrderApp.Infrastructure/TaxConfiguration/TaxConfigurationRepository.cs
git commit -m "feat(infra): dispatch domain events from OrderLifecycleConfig and TaxConfiguration repositories"
```

---

## Task 11: Final verification and wrap-up

- [ ] **Step 1: Run unit tests**

```bash
cd src/backend/TheMillionthFoodOrderApp.Tests.Unit && dotnet run -c Release
```

Expected: all pass.

- [ ] **Step 2: Run integration tests**

```bash
cd src/backend/TheMillionthFoodOrderApp.Tests.Integration && dotnet run -c Release
```

Expected: all pass, including the two new `BrandEventDispatchTests`.

- [ ] **Step 3: Build the full solution**

```bash
cd src/backend && dotnet build TheMillionthFoodOrderApp.slnx
```

Expected: no warnings about unused parameters, no errors.

- [ ] **Step 4: Manual smoke test**

Start the backend: `dotnet run --project TheMillionthFoodOrderApp.AppHost`

Create a brand via Swagger at `http://localhost:5102/swagger`. Observe in the Aspire dashboard logs that `BrandDatabaseProvisioner` receives the `BrandCreatedEvent` and provisions the brand database. This confirms the full Wolverine dispatch path is live.

---

## Summary of Dispatch Rules

| Situation | Where to collect | Where to publish |
|-----------|-----------------|-----------------|
| No transaction (`SaveChangesAsync`, `UpdateScalarAsync`, `SoftDeleteAsync`) | Before `SaveChangesAsync` | After `SaveChangesAsync` |
| With transaction (`UpdateAsync`, `ReplaceAsync`, etc.) | After last mutation, before `SaveChangesAsync` | After `CommitAsync` |
| Concurrent-insert try/catch (`AddOrGetExistingAsync`) | In `try` block before inline `SaveChangesAsync` | In `try` block after inline `SaveChangesAsync` |
