# 015 — US-FP-022: Configure Order Lifecycle Statuses

**Date:** 2026-04-07

---

## What Was Built

Shop-level order lifecycle configuration — Shop Managers can define which order statuses their shop uses and the allowed transitions between them. Default lifecycle: Placed → Confirmed → Preparing → Ready → Picked Up / Delivered. Custom statuses can be added, reordered, and removed. Minimum constraint: 2 statuses with at least one terminal status.

## Key Design Decisions

### Separate Aggregate Instead of Extending Shop

`OrderLifecycleConfig` is its own aggregate root (shop-scoped via `ShopId` FK) rather than a child collection on `Shop`:

- The Shop aggregate already has `OpeningHours` — adding more child collections bloats it
- Order lifecycle is a distinct bounded context concern: future Order, Kitchen Display, and Customer Tracking features all reference it
- Separate aggregate allows independent querying without loading the full Shop
- Still lives in BrandDbContext (database-per-brand isolation preserved)

### Lazy Initialization on First Access

Instead of eagerly creating lifecycle configs when a shop is created, the default config is created on first GET request:

- Avoids coupling shop creation to lifecycle management
- No domain event or handler needed — simple null check in the service
- Unique index on `ShopId` prevents race condition duplicates
- Seeder also creates defaults for dev data shops

### Transitions Reference Sort Orders (Not IDs) in Requests

The PUT request uses `FromSortOrder`/`ToSortOrder` instead of status IDs because:

- When creating a new lifecycle, statuses don't have IDs yet (they're server-generated)
- Sort orders are the stable identifier during editing — they're what the user sees
- The service resolves sort orders to IDs server-side after creating the status entities
- Response DTOs return actual IDs for stable reference

### Atomic Replace Pattern

`ConfigureLifecycle()` clears all statuses and transitions, then re-adds the new set. Same pattern as `Shop.SetOpeningHours()`:

- Simpler than diffing old vs new entities
- EF Core handles orphan deletion via cascade delete from the tracked collection
- Validates the entire new configuration as a unit (min 2, terminal exists, sequential sort orders, valid transition refs)

### Restrict Delete on Transition FKs

`OrderStatusTransition` has two FKs to `OrderStatus` (`FromStatusId`, `ToStatusId`). Both use `DeleteBehavior.Restrict` instead of Cascade to avoid SQL Server's "multiple cascade paths" error. The parent cascade from `OrderLifecycleConfig → OrderStatus` handles cleanup — when a config is deleted, its statuses are cascade-deleted, which removes the transition references first via the parent `OrderLifecycleConfig → OrderStatusTransition` cascade.

## Architecture

### Backend

| Layer | Changes |
|-------|---------|
| **Domain** | `OrderLifecycleConfig` (aggregate root), `OrderStatus` (entity), `OrderStatusTransition` (entity), `IOrderLifecycleConfigRepository` |
| **Application** | `OrderLifecycleService` (get with lazy-init, configure, reset), DTOs with sort-order-based transition mapping |
| **Infrastructure** | 3 EF configurations, repository, BrandDbContext registration, migration `AddOrderLifecycleConfig` |
| **API** | `GetOrderLifecycleEndpoint` (GET), `ConfigureOrderLifecycleEndpoint` (PUT with FluentValidation), `ResetOrderLifecycleEndpoint` (POST) |
| **Seeding** | `SeedOrderLifecycleConfigsAsync` creates defaults for all seeded shops |

### Frontend

| Area | Changes |
|------|---------|
| **Types** | `OrderStatusResponse`, `OrderStatusTransitionResponse`, `OrderLifecycleResponse`, request types in `common.ts` |
| **API Client** | `orderLifecycle.ts` — get, configure, reset |
| **Hooks** | `useOrderLifecycle`, `useConfigureOrderLifecycle`, `useResetOrderLifecycle` with cache invalidation |
| **UI** | `ShopOrderLifecycle.tsx` — visual flow diagram, status editor (add/remove/reorder/color/terminal toggle), transition editor, reset-to-default with confirmation dialog |
| **Routes** | `shops/:shopId/order-lifecycle` under admin routes |
| **ShopEdit** | "Manage Order Lifecycle" button alongside "Manage Opening Hours" |
| **i18n** | `admin.shops.orderLifecycle.*` keys in NL, FR, DE (25 keys each) |

### API Endpoints

| Method | Route | Purpose |
|--------|-------|---------|
| GET | `/api/brands/{slug}/shops/{id}/order-lifecycle` | Get config (lazy-inits default) |
| PUT | `/api/brands/{slug}/shops/{id}/order-lifecycle` | Replace entire lifecycle |
| POST | `/api/brands/{slug}/shops/{id}/order-lifecycle/reset` | Reset to default |

## Testing

- **10 integration tests** — `OrderLifecycleCrudTests`: default lazy-init (6 statuses, 5 transitions), minimal valid config (2 statuses), replace existing, validation errors (< 2 statuses, no terminal, duplicate sort orders, invalid transition refs), reset to default, 404 for non-existent shop/brand
- Frontend TypeScript type-check passes (0 new errors)

## Code Review Findings Addressed

- **Missing i18n keys** — `loading`, `loadError`, `saveError` were used with inline fallbacks; added proper translations to all 3 locale files
- **Unnecessary re-fetch after create** — `ConfigureLifecycleAsync` re-fetched the entity after `AddAsync` + `SaveChangesAsync`; removed since EF Core already tracks it
- **`IsEnabled` on OrderStatus is unused** — kept as future-proofing for when orders reference statuses (disabling vs deleting in-use statuses)

## What This Unblocks

This is a key dependency for the ordering core (Layer 2) and post-ordering features (Layer 3):

- **US-FP-046** — Apply Belgian VAT rates (depends on lifecycle config)
- **US-FP-016** — Place an online order (needs lifecycle to track order state)
- **US-FP-027** — Kitchen display (renders configured statuses)
- **US-FP-023** — Update order status (transitions constrained by config)
- **US-FP-063** — Customer order tracking (shows configured statuses)
- **US-FP-028** — Print order ticket
- **US-FP-026** — Order notifications

## Lessons Learned

1. **Separate aggregates for distinct concerns pay off early.** Even though `OrderLifecycleConfig` is shop-scoped, making it its own aggregate root kept the Shop aggregate clean and made the repository/service boundary clear. The cost is one extra DbSet and configuration class — trivial compared to the coupling avoided.

2. **Sort-order-based references in APIs solve the "new entity has no ID" problem.** When the client sends a batch of new entities with cross-references, using positional identifiers (sort orders) instead of GUIDs avoids the need for client-generated IDs or two-phase create-then-link flows.

3. **EF Core's cascade delete with multiple FK paths requires careful thought on SQL Server.** The Restrict + parent cascade pattern works, but it's not obvious why. A comment in the configuration explaining the cascade strategy saved future confusion during code review.

4. **Manual migration creation works when the SDK isn't available**, but it's fragile — the Designer file and snapshot must be kept in sync manually. Recommend regenerating the migration with `dotnet ef` when the SDK is available to catch any discrepancies.
