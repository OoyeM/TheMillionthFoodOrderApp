# US-FP-027 — View order on kitchen display

GitHub issue: [#27](https://github.com/OoyeM/TheMillionthFoodOrderApp/issues/27)
Branch: `feat/us-fp-027-kitchen-display`
Prereqs: US-FP-022 ✅ (order lifecycle), US-FP-068 ✅ (SignalR infrastructure)

## Acceptance criteria

1. Kitchen display shows orders sorted by time (oldest first).
2. Each order card shows: order number, items with modifiers, order type (Pickup/EatIn/Delivery), table number (if eat-in), time slot (if applicable), customer name.
3. New orders appear automatically without page refresh (SignalR).
4. Completed orders are removed from the active view (terminal statuses).

**Scope note — fields that don't exist yet:**
- `TableNumber` on `Order` lands with US-FP-024. The UI will render it *if* the response includes a non-null `tableNumber`; until then, eat-in orders simply show the "Eat-In" badge with no number. No domain or DB change in this PR.
- "Time slot" is not yet a concept on `Order` (no story has introduced scheduled-for orders). Same approach: the UI renders a `timeSlot` field if present. No domain or DB change in this PR.

This keeps US-FP-027 a pure read-side display + push feature and avoids coupling with US-FP-024.

## Backend

### 1. Repository — list active orders for a shop

**File:** `src/backend/TheMillionthFoodOrderApp.Application/Orders/IOrderRepository.cs`
Add:
```csharp
Task<IReadOnlyList<Order>> GetActiveByShopAsync(Guid shopId, CancellationToken ct);
```

**File:** `src/backend/TheMillionthFoodOrderApp.Infrastructure/Orders/OrderRepository.cs`
Implementation:
- Query `Orders` where `ShopId == shopId`
- Filter out orders whose `StatusName` matches an `OrderStatus` in the shop's lifecycle that has `IsTerminal == true`
- Order by `CreatedAt ASC`
- `.Include(o => o.Items).ThenInclude(i => i.SelectedModifiers)`

Because `OrderStatus` lives in `OrderLifecycleConfig` (per-shop) and an order only stores `StatusName`, the implementation joins to the shop's lifecycle config:
```csharp
var terminalStatusNames = await _db.OrderLifecycleConfigs
    .Where(c => c.ShopId == shopId)
    .SelectMany(c => c.Statuses)
    .Where(s => s.IsTerminal)
    .Select(s => s.Name)
    .ToListAsync(ct);

return await _db.Orders
    .Include(o => o.Items).ThenInclude(i => i.SelectedModifiers)
    .Where(o => o.ShopId == shopId && !terminalStatusNames.Contains(o.StatusName))
    .OrderBy(o => o.CreatedAt)
    .ToListAsync(ct);
```

### 2. Endpoint — list active orders

**File:** `src/backend/TheMillionthFoodOrderApp.Api/Endpoints/Orders/ListActiveOrdersEndpoint.cs`

- Route: `GET /api/brands/{brandSlug}/shops/{shopId}/orders/active`
- Request: `(string BrandSlug, Guid ShopId)` route params
- Response: `IReadOnlyList<OrderResponse>` (reuse existing `OrderResponse` + `OrderMapper`/`OrderTrackingMapper.MapOrder`)
- `PreProcessor<BrandScopedPreProcessor<…>>`
- `AllowAnonymous()` for parity with `GetOrderEndpoint` — auth gating across the order surface is owned by **US-FP-039**. Add a TODO comment referencing it.
- Swagger summary + 200 response shape

### 3. Tests

**Unit:** `src/backend/TheMillionthFoodOrderApp.Tests.Unit/Orders/OrderRepositoryQueryShapeTests.cs` — optional, covered by integration.

**Integration:** `src/backend/TheMillionthFoodOrderApp.Tests.Integration/Orders/ListActiveOrdersTests.cs`
- TUnit `[Test]`, `[ClassDataSource<IntegrationTestBase>(Shared = SharedType.PerClass)]`
- Setup: brand + shop + tax config + default lifecycle (Placed/Confirmed/Preparing/Ready/PickedUp/Delivered; PickedUp + Delivered terminal)
- Scenarios:
  1. Returns empty list when no orders exist
  2. Returns orders in non-terminal statuses, sorted ascending by `CreatedAt`
  3. Excludes orders in terminal statuses
  4. Does not return orders from other shops in the same brand
  5. Item modifiers are included in the response

## Frontend

### 4. API client

**File:** `src/frontend/src/api/orders.ts` (extend the existing `ordersApi`)
```ts
listActive: (brandSlug: string, shopId: string): Promise<OrderResponse[]> =>
  apiClient
    .get<OrderResponse[]>(`/brands/${brandSlug}/shops/${shopId}/orders/active`)
    .then(r => r.data),
```

Extend `OrderResponse` with optional fields to allow forward-compatibility without breaking now:
```ts
tableNumber?: string | null;   // future US-FP-024
timeSlot?: string | null;      // future
```

### 5. Hook — active orders with realtime updates

**File:** `src/frontend/src/features/pos/hooks/useActiveOrders.ts`
- `useQuery({ queryKey: ['orders', 'active', brandSlug, shopId], queryFn: () => ordersApi.listActive(...) })`
- `staleTime: 0`, `refetchOnWindowFocus: true`
- `useOrderUpdates({ shopGroup: { brandSlug, shopId }, onStatusChange })`
- `onStatusChange` logic:
  - Get current orders from cache
  - If the updated order isn't in the cache → invalidate the query (new order arrived)
  - If `update.newStatus` corresponds to a terminal status → optimistically remove from cache, then invalidate to reconcile
  - Else update the matching order's `statusName` in cache
- Return `{ orders, isLoading, connectionStatus }`

Use the shop's lifecycle (already fetched server-side, expose lifecycle terminal status names via a separate `useOrderLifecycle` query) to decide what is terminal client-side. **Alternative — simpler:** always invalidate on any status change, which is correct and minimizes complexity. Recommend this for v1; revisit if real-load testing shows excessive refetches.

> Decision: go with the simpler "always invalidate on any status change" approach. The hook just calls `queryClient.invalidateQueries(['orders', 'active', brandSlug, shopId])` on every `OrderStatusChanged` event. Simpler, fewer edge cases, and an invalidate on a small list (~tens of orders) is cheap.

### 6. Kitchen display page

**File:** `src/frontend/src/features/pos/pages/KitchenDisplay.tsx`

- `useParams` → `brandSlug` (from outer route), `shopId` (from search params or route param — we'll use a query param `?shopId=...` for v1 since POS routes don't yet have `shopId`; document this as a temporary mechanism)
- Renders:
  - Header with shop name placeholder, `ConnectionStatus` indicator pill
  - Grid of order cards (CSS grid, large touch-friendly tiles)
- Order card displays:
  - `#${orderNumber}` (large)
  - Relative time ("3m ago") + absolute time (`HH:mm`)
  - Order type badge with i18n label and color
  - Conditional `tableNumber` ("Table 5") when present
  - Conditional `timeSlot` when present
  - `customerName` if set
  - Item list — product name, quantity, modifier names indented under each item
- Empty state: "No active orders" with the i18n key
- Loading state: skeleton tiles
- Error state: error banner with retry

**File:** `src/frontend/src/features/pos/components/KitchenOrderCard.tsx`
- Extract the card to its own component for testability

### 7. Routing + auth

**File:** `src/frontend/src/features/pos/routes.tsx`
- Add `{ path: 'kitchen', element: <RequireAuth roles={['kitchen-staff', 'counter-staff', 'brand-admin']}><KitchenDisplay /></RequireAuth> }`
- Mounted under `/:brandSlug/:lang/pos/kitchen`

### 8. i18n

**Files:** `src/frontend/src/i18n/locales/{nl,fr,de}/common.json`
- `kitchen.title`
- `kitchen.empty`
- `kitchen.orderType.pickup` / `.eatIn` / `.delivery`
- `kitchen.customer`
- `kitchen.table`
- `kitchen.timeSlot`
- `kitchen.connection.connected` / `.connecting` / `.disconnected`

### 9. Frontend tests

**File:** `src/frontend/src/features/pos/pages/__tests__/KitchenDisplay.test.tsx`
- MSW handler for `GET /api/brands/:slug/shops/:shopId/orders/active`
- Renders cards in the order returned by the API
- Renders item modifiers under each item
- Renders empty state when list is empty
- Renders table number only when present

**File:** `src/frontend/src/features/pos/hooks/__tests__/useActiveOrders.test.ts`
- Mock `useOrderUpdates` and assert invalidation is called when `onStatusChange` fires
- Optional — light coverage; full SignalR plumbing is exercised in storefront tests already

## Verification

- `dotnet build TheMillionthFoodOrderApp.slnx` in `src/backend/` → 0 errors
- `dotnet run -c Release` in `TheMillionthFoodOrderApp.Tests.Integration/` (per backend CLAUDE.md — TUnit uses `dotnet run -c Release`, not `dotnet test`)
- `pnpm test` in `src/frontend/` → all green
- `pnpm build` in `src/frontend/` → 0 errors

## Out of scope (deferred)

- Table number persistence/entry (**US-FP-024**)
- Scheduled / time-slot orders (no story yet)
- Audio cue / pop notification for new orders
- Bumping orders to next status from the kitchen display itself (that's **US-FP-023** "Update order status (kitchen)")
- Hub authorization for non-anonymous access (**US-FP-039**)

## File summary

**Backend (added):**
- `Application/Orders/IOrderRepository.cs` (extended)
- `Infrastructure/Orders/OrderRepository.cs` (extended)
- `Api/Endpoints/Orders/ListActiveOrdersEndpoint.cs` (new)
- `Tests.Integration/Orders/ListActiveOrdersTests.cs` (new)

**Frontend (added):**
- `api/orders.ts` (extended)
- `features/pos/hooks/useActiveOrders.ts` (new)
- `features/pos/pages/KitchenDisplay.tsx` (new)
- `features/pos/components/KitchenOrderCard.tsx` (new)
- `features/pos/components/KitchenOrderCard.module.css` (new)
- `features/pos/pages/KitchenDisplay.module.css` (new)
- `features/pos/routes.tsx` (extended)
- `i18n/locales/{nl,fr,de}/common.json` (extended)
- `features/pos/pages/__tests__/KitchenDisplay.test.tsx` (new)
- `features/pos/hooks/__tests__/useActiveOrders.test.ts` (new)

**Docs:**
- `docs/dependency-tree.md` — mark US-FP-027 as ✅ after PR merges
