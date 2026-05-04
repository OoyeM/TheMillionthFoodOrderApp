# 019 — Domain Event Dispatch via Wolverine

**Date:** 2026-05-04

---

## What Was Built

Aggregates have always raised domain events (e.g. `BrandCreatedEvent`, `OrderStatusChangedEvent`) via `AddDomainEvent()` on `Entity<TId>`, but the repositories never dispatched them — they called `dbContext.SaveChangesAsync()` and returned. This entry wires up the missing link: every repository now collects domain events from EF Core's change tracker and publishes them via Wolverine's `IMessageBus` after each successful persist.

The `BrandCreatedEvent → BrandDatabaseProvisioner` path (brand DB provisioning on first brand creation) is the first live consumer. Any future event-driven workflow (notifications, cache invalidation, projections) can now register a Wolverine handler without touching the repositories.

## Key Design Decisions

### Why Not an EF Core SaveChangesInterceptor

The obvious place for this logic is a `SaveChangesInterceptor` that fires after every save. It works for the `AuditSaveChangesInterceptor` (which has no DI dependencies), but it cannot work for event dispatch:

- Both `PlatformDbContext` and `BrandDbContext` are registered via `builder.AddSqlServerDbContext<T>()` (Aspire's extension), which enables **DbContext pooling** by default.
- Pooled contexts cannot accept **scoped** constructor dependencies. `IMessageBus` is scoped (one per request/message).
- The `configureDbContextOptions` callback in Aspire only provides `DbContextOptionsBuilder` — there is no `IServiceProvider` to manually resolve `IMessageBus` at registration time.

### IHasDomainEvents Interface

`Entity<TId>` already holds `DomainEvents` and `ClearDomainEvents()`, but it is generic. EF Core's `ChangeTracker.Entries<T>()` requires a concrete non-generic type to enumerate across all tracked entities — `Entries<Entity<Guid>>()` misses entities with other ID types. A non-generic `IHasDomainEvents` interface fixes this cleanly:

```csharp
public interface IHasDomainEvents
{
    IReadOnlyCollection<IDomainEvent> DomainEvents { get; }
    void ClearDomainEvents();
}
```

`Entity<TId>` implements it; `ChangeTracker.Entries<IHasDomainEvents>()` now enumerates every aggregate regardless of its ID type.

### Repository-Based Dispatch

Repositories are scoped services — they can freely inject `IMessageBus`. A static `DomainEventDispatcher` helper centralises the two-step pattern so no repository duplicates it:

```csharp
// 1. Collect and clear BEFORE SaveChangesAsync (entities still in tracker)
var events = DomainEventDispatcher.CollectAndClear(dbContext);

// 2. Save
await dbContext.SaveChangesAsync(cancellationToken);

// 3. Publish AFTER save (or AFTER CommitAsync for transactional methods)
await DomainEventDispatcher.PublishAsync(events, messageBus);
```

### (dynamic) Dispatch

`IMessageBus.PublishAsync<T>()` is generic and Wolverine routes by the **concrete message type**. If we pass `IDomainEvent` at compile time, Wolverine sees `IDomainEvent` — no handler is matched. Using `(dynamic)@event` forces runtime type resolution so `BrandCreatedEvent` is dispatched as `BrandCreatedEvent`, not as `IDomainEvent`.

### Transaction Ordering Rule

For repository methods that use an explicit `BeginTransactionAsync`:

- **Collect** events after the last mutation (entities are in the tracker).
- **Publish** only after `CommitAsync` — never after `SaveChangesAsync` alone.

Publishing before commit would fire Wolverine handlers for a database change that was never committed. If the transaction rolls back, the handlers would have already run on stale data.

For `ChangeTracker.Clear()` methods (e.g. `ReplaceOpeningHoursAsync`): the tracker is cleared, the entity is re-loaded fresh, and then mutated — so `CollectAndClear` runs on the re-loaded entity after mutation, not before the clear.

### Outbox Deferred

An outbox guarantees at-least-once delivery even if the process crashes between commit and publish. Wolverine has first-class outbox support via `WolverineFx.SqlServer`. Deferred until we switch to a real message broker (RabbitMQ / Azure Service Bus) — at that point the change is additive config in `Program.cs` without touching repositories.

## What Changed

| Layer | File | What |
|-------|------|------|
| Domain | `Common/IHasDomainEvents.cs` | New non-generic interface |
| Domain | `Common/Entity.cs` | Implements `IHasDomainEvents` |
| Infrastructure | `Persistence/DomainEventDispatcher.cs` | New static helper (CollectAndClear + PublishAsync) |
| Infrastructure | `Brands/BrandRepository.cs` | Inject `IMessageBus`, dispatch in `SaveChangesAsync` |
| Infrastructure | `BrandSettings/BrandSettingsRepository.cs` | Same pattern |
| Infrastructure | `Identity/PlatformUserRepository.cs` | Same pattern; inline save in `AddOrGetExistingAsync` dispatches in `try` block only (not `catch`) |
| Infrastructure | `Shops/ShopRepository.cs` | `UpdateAsync` (no transaction) + `ReplaceOpeningHoursAsync` (transactional) |
| Infrastructure | `Products/ProductRepository.cs` | `UpdateScalarAsync` (no transaction) + `UpdateAsync` (transactional) |
| Infrastructure | `MenuCategories/MenuCategoryRepository.cs` | Same two-pattern structure as Products |
| Infrastructure | `ModifierGroups/ModifierGroupRepository.cs` | Four save sites: `SaveChangesAsync`, `SoftDeleteAsync`, `UpdateAsync`, `SetProductModifierGroupsAsync` |
| Infrastructure | `OrderLifecycle/OrderLifecycleConfigRepository.cs` | `SaveChangesAsync` + transactional `ReplaceAsync` |
| Infrastructure | `TaxConfiguration/TaxConfigurationRepository.cs` | `SaveChangesAsync` + transactional `ReplaceRatesAsync` |
| Tests | `DomainEvents/BrandEventDispatchTests.cs` | TDD integration test — `SpyMessageBus` + direct repository instantiation |

## Writing the SpyMessageBus

The plan called for using an IDE "implement interface" action on `SpyMessageBus`. Without an IDE in the agent loop, we compiled against `IMessageBus` and read the compiler errors to discover the actual interface shape:

- `PreviewSubscriptions` returns `IReadOnlyList<Envelope>`, not `IReadOnlyList<ISubscriberAddress>` (wrong guess from XML docs)
- `IMessageBus` extends `ICommandBus`, which adds two `InvokeAsync` overloads with a `DeliveryOptions` parameter
- `TenantId` is a read/write property (`{ get; set; }`), not read-only
- `PublishAsync<T>` and `SendAsync<T>` have constraints that differ from `where T : class` — explicit interface implementation was needed to avoid CS0425

Lesson: for third-party interfaces with non-obvious shapes, a compile-and-fix cycle is faster than guessing from docs.
