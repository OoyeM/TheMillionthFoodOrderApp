# US-FP-018 — Place an in-store order (counter staff)

GitHub issue: [#18](https://github.com/OoyeM/TheMillionthFoodOrderApp/issues/18)
Branch: `feat/us-fp-18-usfp018-place-an-instore-order-counter-s` (already checked out)
Prereqs: US-FP-016 ✅ (place online order), US-FP-022 ✅ (order lifecycle), US-FP-027 ✅ (kitchen display), US-FP-068 ✅ (SignalR), US-FP-058 ✅ (mock payments)

## 1. Summary

Build a touch-friendly POS screen at `/:brandSlug/:lang/pos/shops/:shopId/order` that lets counter staff:

- Browse the brand's menu (categories + product tiles) with large, finger-sized buttons sized for a tablet in landscape.
- Add products, including modifier selections and combos, to an in-memory ticket.
- Pick order type (Pickup / Eat-In) and, when Eat-In, enter a table number.
- Submit the order to the existing `POST /api/brands/{slug}/shops/{shopId}/orders` endpoint with `PaymentMethod=CashAtPickup` ("pay at pickup"). The order flows through the same `Order.Create` → `OrderCreatedEvent` → Wolverine → `OrderCreatedHandler` → SignalR pipeline, so the existing Kitchen Display (US-FP-027) picks it up automatically.

**Architecture approach — reuse, don't fork.** Almost all backend plumbing already exists. The only backend change required by US-FP-018 itself is **adding `TableNumber` to the `Order` aggregate** (already declared on the frontend `OrderResponse` as `tableNumber?` and already rendered by `KitchenOrderCard`). The frontend work is the substantive deliverable: a new POS feature module mirroring the storefront menu + cart pattern but optimised for touch.

**Scope decisions (recommended — surface to user):**

1. **Eat-in / table number**: implement **option (a)** — capture `tableNumber` as part of this story because AC explicitly requires it. The field becomes a permanent column on `Order` (nullable, only meaningful when `OrderType=EatIn`). US-FP-024 will later add the customer-facing equivalent and pair this with the US-FP-066 enable/disable flag; until then, the POS always allows Eat-In.
2. **Ticket printer**: explicitly **out of scope**. The AC mentions it; we satisfy that AC by re-using the same `OrderCreatedEvent` Wolverine pipe the kitchen display already listens on. US-FP-028 will plug a printer handler into the same event later — no change to this story.
3. **Payment**: every POS order is created with `PaymentMethod = CashAtPickup` for both Pickup and Eat-In. US-FP-058 mock-payment screen is not invoked. This matches the AC ("pickup = pay at pickup") and treats Eat-In the same way (settle in person). Card/Bancontact at counter is a separate concern.
4. **Auth**: route already guarded with `RequireAuth roles={['counter-staff', 'floor-staff', 'kitchen-staff', 'shop-manager']}`. Mock auth has a `counter-staff` persona. Real Keycloak staff login (US-FP-039 ⬜) is *not* blocking — mock-auth is sufficient to demonstrate this story end-to-end, and the BFF endpoint already accepts an authenticated counter-staff bearer token (the endpoint itself is `AllowAnonymous` per US-FP-016 conventions, with auth gating deferred to US-FP-039). Surface as a risk.
5. **Shop selection**: the POS user lands on a dashboard and must pick a shop before ordering. We can either (a) read `brandSlug + shopId` from the staff user's `BrandUserRole` assignment (once US-FP-039 ships) or (b) for this story show a simple shop picker on the `/pos` dashboard. Recommend (b) for now: a `ShopPicker` page listing the brand's shops, persisting the choice to `localStorage` (key `pos:active-shop:{brandSlug}`).

## 2. Backend changes

### 2.1 Domain — add `TableNumber` to `Order`

**File:** `src/backend/TheMillionthFoodOrderApp.Domain/Orders/Order.cs`

- Add nullable `public string? TableNumber { get; private set; }`. Use `string` not `int` to allow alphanumeric labels (e.g. "T-12", "Bar-3"); matches FE typing `tableNumber: string | null`.
- Extend `Order.Create(...)` factory with a `string? tableNumber` parameter (place after `customerName`).
- Add an invariant: when `orderType == OrderType.EatIn`, `tableNumber` may be null OR non-empty (do **not** require it at the domain level — the API/validation layer will gate this. Rationale: US-FP-024 + US-FP-066 will later allow eat-in without a table when the shop opts out, and we want to model that without breaking the aggregate).
- Optionally trim and length-cap in the factory: `tableNumber?.Trim()` with max 20 chars (throw `ArgumentException` if too long).

### 2.2 Domain — extend `OrderCreatedEvent`

**File:** `src/backend/TheMillionthFoodOrderApp.Domain/Orders/OrderCreatedEvent.cs`

- Add `string? TableNumber` to the record.
- Update `Order.Create` to pass `tableNumber` when raising the event. This isn't strictly required (the handler currently only forwards status), but it's cheap and lets the future printer handler render the table number without a re-fetch.

### 2.3 Infrastructure — EF configuration + migration

**File:** `src/backend/TheMillionthFoodOrderApp.Infrastructure/Orders/OrderConfiguration.cs`

```csharp
builder.Property(o => o.TableNumber).HasMaxLength(20);  // nullable, no IsRequired
```

**New migration:** `dotnet ef migrations add AddOrderTableNumber --context BrandDbContext --output-dir Persistence/Migrations/Brand` from inside `src/backend/TheMillionthFoodOrderApp.Infrastructure` with the standard startup-project flag. Generates `*_AddOrderTableNumber.cs` adding `TableNumber NVARCHAR(20) NULL` to the `Orders` table.

### 2.4 Application — DTO and request plumbing

**File:** `src/backend/TheMillionthFoodOrderApp.Application/Orders/Dtos/CreateOrderRequest.cs`

- Add `string? TableNumber` to the record (after `CustomerName`).

**File:** `src/backend/TheMillionthFoodOrderApp.Application/Orders/Dtos/OrderResponse.cs`

- Add `string? TableNumber` to the response record. **Frontend already expects this field as `tableNumber`** — wiring it through completes the contract.

**File:** `src/backend/TheMillionthFoodOrderApp.Application/Orders/OrderService.cs`

- Pass `request.TableNumber` into `Order.Create(...)`.
- Include `order.TableNumber` in `MapToResponse(...)`.
- No new business rule: do **not** require eat-in to have a table number at this layer (deferred to US-FP-066).

### 2.5 API — endpoint, validation, response mapping

**File:** `src/backend/TheMillionthFoodOrderApp.Api/Endpoints/Orders/CreateOrderEndpoint.cs`

- Add `string? TableNumber` to `CreateOrderApiRequest`.
- Add to `CreateOrderRequestValidator`:
  ```csharp
  RuleFor(x => x.TableNumber).MaximumLength(20).When(x => x.TableNumber is not null);
  // The story AC says table number is required for eat-in — apply at the API layer:
  RuleFor(x => x.TableNumber)
      .NotEmpty()
      .When(x => string.Equals(x.OrderType, "EatIn", StringComparison.OrdinalIgnoreCase))
      .WithMessage("TableNumber is required for eat-in orders.");
  ```
- Pass `req.TableNumber` to the new `CreateOrderRequest` constructor.

**File:** `src/backend/TheMillionthFoodOrderApp.Api/Endpoints/Orders/OrderTrackingMapper.cs`

- Map `Order.TableNumber` → `OrderResponse.TableNumber` so the kitchen display's `GET /orders/active` response and the tracking response include it for newly placed POS orders.

### 2.6 Integration tests

**File:** `src/backend/TheMillionthFoodOrderApp.Tests.Integration/Orders/PlaceOrderTests.cs` — extend existing class.

Add scenarios:
- `PlaceOrder_EatIn_WithTableNumber_PersistsAndReturnsIt` — POST with `OrderType=EatIn`, `TableNumber="T-12"`; assert 201 + response includes `tableNumber == "T-12"`.
- `PlaceOrder_EatIn_WithoutTableNumber_Returns400` — POST with `OrderType=EatIn`, no table number; assert 400.
- `PlaceOrder_Pickup_WithoutTableNumber_Returns201` — table number not required for pickup.
- `PlaceOrder_EatIn_TableNumberTooLong_Returns400` — 21 chars.

**File:** `src/backend/TheMillionthFoodOrderApp.Tests.Integration/Orders/ListActiveOrdersTests.cs` — extend to assert `tableNumber` is non-null in the response when an eat-in order with a table number was placed (proves the kitchen display will see it).

These follow the **TUnit** pattern (`[Test]`, `await Assert.That(...)`, `[ClassDataSource<IntegrationTestBase>(Shared = SharedType.PerClass)]`) per `src/backend/CLAUDE.md`.

### 2.7 Notification handler — no change required

`OrderCreatedHandler` already broadcasts every new order to the shop's SignalR group via the existing `status-changed` pipeline. POS orders use the same `OrderService.CreateOrderAsync` path, so the kitchen display will receive them with zero new code. This satisfies AC "Order is submitted and triggers the same fulfillment flow as online orders".

## 3. Frontend changes

All changes land under `src/frontend/src/features/pos/`. The pattern mirrors the storefront module but is purpose-built for touch — tile-grid layout, large buttons, no over-the-cart drawer (left/right two-pane layout fits a 10" tablet in landscape).

### 3.1 Routes — POS shell + new screens

**File:** `src/frontend/src/router.tsx` — replace the inline `posRoutes` with the module's export and add the new routes:

```tsx
import { posRoutes } from '@features/pos/routes';
// ...
{
  path: 'pos',
  element: (
    <RequireAuth roles={['counter-staff', 'floor-staff', 'kitchen-staff', 'shop-manager']}>
      <AppVariantLayout variant="pos" />
    </RequireAuth>
  ),
  children: posRoutes,
}
```

**File:** `src/frontend/src/features/pos/routes.tsx` — replace the placeholder. Wire:
- `index` → `PosDashboard` (now a shop picker)
- `shops/:shopId/kitchen` → `KitchenDisplay` (existing, just relocated from `router.tsx` into the feature module so all POS routes live together)
- `shops/:shopId/order` → `NewOrderPage` (new, the main deliverable)
- `shops/:shopId/order/confirmation/:orderId` → `OrderPlacedPage` (new, terse confirmation tuned for staff)

Lazy-load via React.lazy where appropriate, following the existing kitchen-display pattern.

### 3.2 Dashboard / shop picker

**File:** `src/frontend/src/features/pos/pages/Dashboard.tsx` — replace placeholder with a shop picker.

- Use `shopsApi.list(brandSlug)` (already in `src/frontend/src/api/shops.ts`).
- Render shop cards as full-width touch tiles ("Open ordering" + "Open kitchen display" buttons per shop).
- Persist the last-used shop in `localStorage` (`pos:last-shop:{brandSlug}`) and auto-redirect to `/pos/shops/:shopId/order` on next visit.

### 3.3 New-order page (the heart of this story)

**File:** `src/frontend/src/features/pos/pages/NewOrderPage.tsx`

Two-pane layout (CSS grid `grid-template-columns: 1fr 22rem`):

- **Left pane — menu**:
  - Category tabs across the top (horizontal scroll if many).
  - Product grid: 3–4 columns of `ProductTile` components sized ≥ 80×80mm equivalent (≈ `min-height: 7rem`, `padding: 1.25rem`, `font-size: 1rem`+).
  - Tap a tile → if product has modifier groups, open `PosModifierModal` (touch-friendly variant of the existing `ModifierModal`); otherwise add directly to the in-memory order.
  - Reuse `useStorefrontCategories` + `useStorefrontCategoryProducts` from `@features/storefront/hooks/useStorefrontMenu.ts`. Same data, different rendering — no duplication of fetching logic.
- **Right pane — ticket**:
  - Live list of items with `+ / –` qty buttons (44px hit targets).
  - Order-type toggle: large pickup/eat-in segmented control.
  - Table-number input, conditionally rendered only when EatIn is selected — numeric keyboard hint (`inputMode="numeric"` but `type="text"` to permit "T-12").
  - Optional customer-name input (matches existing endpoint contract).
  - Subtotal display.
  - "Place order" primary button (full-width, 56px tall).

### 3.4 Components

**File:** `src/frontend/src/features/pos/components/ProductTile.tsx`
- Large square card, product name top, price bottom, optional image. Tap area = entire tile.

**File:** `src/frontend/src/features/pos/components/PosTicket.tsx`
- Renders the in-memory order: items, modifiers, qty steppers, line totals, subtotal.
- Receives the order state via props (lift state to `NewOrderPage` to keep the ticket and menu in sync).

**File:** `src/frontend/src/features/pos/components/PosModifierModal.tsx`
- Touch-friendly modifier picker. May be implemented as a slim wrapper around the existing `ModifierModal` from storefront with `posMode` prop, **or** a fresh component if the storefront modal's styling can't be easily scaled. Decide during execution; recommend wrapper first.

**File:** `src/frontend/src/features/pos/components/OrderTypeSelector.tsx`
- Segmented control: Pickup / Eat-In. Delivery omitted from POS for now.

### 3.5 In-memory order state

**File:** `src/frontend/src/features/pos/hooks/usePosOrder.ts`

A simple `useReducer` (action types `ADD_ITEM`, `REMOVE_ITEM`, `UPDATE_QUANTITY`, `SET_ORDER_TYPE`, `SET_TABLE_NUMBER`, `SET_CUSTOMER_NAME`, `RESET`). **Not** persisted to localStorage — counter staff move fast and a stale ticket on reload is worse than starting fresh. Different from the storefront `CartContext`, which intentionally persists.

### 3.6 Mutation — create order

**File:** `src/frontend/src/features/pos/hooks/useCreatePosOrder.ts`

Thin wrapper around `ordersApi.create` (existing in `src/frontend/src/api/orders.ts`). On success, reset the ticket and navigate to `OrderPlacedPage`.

**File:** `src/frontend/src/api/orders.ts` — extend `CreateOrderRequest` interface to include `tableNumber?: string | null`. `OrderResponse.tableNumber` already exists.

### 3.7 Confirmation page

**File:** `src/frontend/src/features/pos/pages/OrderPlacedPage.tsx`
- Massive order number ("# ABC123"), order type, table number (if any), totals.
- Two big buttons: "New order" → back to `NewOrderPage`; "Print receipt" disabled with tooltip "Coming in US-FP-028".
- Auto-redirect to new order after 10s.

### 3.8 i18n keys

**Files:** `src/frontend/src/i18n/locales/{nl,fr,de}/common.json` — extend the existing `pos.*` namespace with `pos.dashboard`, `pos.order` (ticket, orderType, tableNumber, customerName, submitError), and `pos.confirmation` groups. Mirror NL across FR and DE.

### 3.9 Tests

- **Component (Vitest):** `ProductTile`, `PosTicket`, `OrderTypeSelector` rendering + interaction tests.
- **Hook (Vitest):** `usePosOrder` reducer transitions.
- **Page integration (Vitest + RTL):** `NewOrderPage` — selecting EatIn surfaces the table-number input; submit posts with `tableNumber`.
- **E2E (Playwright, optional follow-up):** smoke test placing a POS order and seeing it in the kitchen display.

## 4. Auth

- The route is already guarded for staff roles; `MockAuthProvider` exposes a `counter-staff` persona out of the box.
- Real Keycloak staff login (**US-FP-039**) is **not blocking** this story end-to-end via mock auth, but production sign-in is incomplete. Surface as an open question — the user may want US-FP-039 first.
- The `POST /orders` endpoint is currently `AllowAnonymous()` (deliberate, per US-FP-016, with a comment pointing to US-FP-039). Don't tighten it in this PR; that's US-FP-039's job.

## 5. Fulfillment flow integration

End-to-end after this story:

1. Counter staff submits → `POST /api/brands/{slug}/shops/{shopId}/orders` with `paymentMethod=CashAtPickup`, `tableNumber` populated when EatIn.
2. `OrderService.CreateOrderAsync` builds the `Order` aggregate identically to online orders (same VAT logic, same lifecycle opening status, same denormalisation rules, same `ORDER_NUMBER_CONFLICT` retry loop).
3. `Order.Create` raises `OrderCreatedEvent`. `OrderRepository.SaveChangesAsync` collects domain events, calls `DomainEventDispatcher.PublishAsync(events, messageBus)`.
4. **Wolverine** routes the event to `OrderCreatedHandler` (Infrastructure/Notifications), which calls `IOrderNotificationService.NotifyOrderStatusChangedAsync` with `previousStatus=""` (signalling a brand-new order).
5. `SignalROrderNotificationService` pushes to the shop group; `KitchenDisplay` (US-FP-027) is already subscribed and refreshes via `useActiveOrders` → `ordersApi.listActive` → cards rerender with the new POS order.
6. `KitchenOrderCard` already renders `order.tableNumber` and `order.orderType` badge — once the backend sends `tableNumber` in the active-orders response, eat-in POS orders show the table chip with zero kitchen-display code changes.

Printer integration (US-FP-028) will plug a second handler into the same `OrderCreatedEvent`. No changes needed in this PR.

## 6. Test plan

**Backend (TUnit):**
- New tests in `PlaceOrderTests.cs` and `ListActiveOrdersTests.cs` (see §2.6).
- Run all existing order integration tests to confirm no regression.

**Frontend (Vitest):**
- `pnpm test` in `src/frontend` — new component/hook/page tests pass.
- `pnpm build` — type-check passes (especially the new `tableNumber` field on `CreateOrderRequest`).

**Manual verification (Aspire up, mock auth):**
1. `cd src/backend && dotnet run --project TheMillionthFoodOrderApp.AppHost`
2. `cd src/frontend && pnpm dev`
3. Visit `http://localhost:5173/bff/login?mock=counter-staff@frietjes`.
4. Navigate to `/frietjes/nl/pos` → pick a seeded shop.
5. Tap several products, add modifiers, switch order type to Eat-In, enter `T-12`, submit.
6. Open `/frietjes/nl/pos/shops/{id}/kitchen` in another tab — the POS order appears in real time with the table-number chip and Eat-In badge.
7. Open `/frietjes/nl/shops/{id}/menu` (storefront) in a third tab and place an online order — confirm POS-tab kitchen display also sees it (proves both channels share the pipe).
8. Visit Swagger → `POST /orders` → verify `tableNumber` appears in request and response schemas.

## 7. Open questions / risks

1. **(must decide)** Eat-in scope: implement here vs defer to US-FP-024 + US-FP-066. **Recommend implement here** because the AC is explicit; defer the *enable/disable* and *customer-side* flows.
2. **(must decide)** Staff login: are we OK relying on mock auth for the demo, with real Keycloak staff login coming in US-FP-039? If not, US-FP-039 should land first.
3. **(should decide)** Should the POS allow the counter staff to override `customerName` ("Jan", "table 5", etc.) the same way the storefront does? Recommend **yes** — keep the field, optional, same as online.
4. **(should decide)** Should we surface a "Quick combo / favourites" row at the top of the menu pane for the top-selling products? **Out of scope** for MVP, but worth a follow-up issue.
5. **Risk — touch testing without a tablet**: AC requires tablet-sized screens. Mitigate by viewport CSS media queries (`@media (min-width: 768px) and (pointer: coarse)`) and a Playwright test pinning the viewport to 1024×768. Manual verification on a real tablet should still be requested from the user.
6. **Risk — shop selection without staff–shop binding**: until US-FP-032/039 are fully wired, any staff member can pick any shop. Acceptable for MVP; document the gap.
7. **Risk — `tableNumber` as a string vs int**: keeping it `string?` allows for "T-12" style labels but means the storefront/kitchen display can't sort numerically. Recommend `string?` (matches existing FE typing).

## 8. File-by-file change list

### Backend — modify

- `src/backend/TheMillionthFoodOrderApp.Domain/Orders/Order.cs` — add `TableNumber`, extend `Create` factory.
- `src/backend/TheMillionthFoodOrderApp.Domain/Orders/OrderCreatedEvent.cs` — add `TableNumber`.
- `src/backend/TheMillionthFoodOrderApp.Infrastructure/Orders/OrderConfiguration.cs` — map `TableNumber` column (NVARCHAR(20) NULL).
- `src/backend/TheMillionthFoodOrderApp.Application/Orders/Dtos/CreateOrderRequest.cs` — add `TableNumber`.
- `src/backend/TheMillionthFoodOrderApp.Application/Orders/Dtos/OrderResponse.cs` — add `TableNumber`.
- `src/backend/TheMillionthFoodOrderApp.Application/Orders/OrderService.cs` — thread `TableNumber` through factory + mapper.
- `src/backend/TheMillionthFoodOrderApp.Api/Endpoints/Orders/CreateOrderEndpoint.cs` — request field, validator rule (`NotEmpty` when EatIn), pass-through.
- `src/backend/TheMillionthFoodOrderApp.Api/Endpoints/Orders/OrderTrackingMapper.cs` — map `TableNumber` into responses for `GET /orders/{id}` and `GET /orders/active`.
- `src/backend/TheMillionthFoodOrderApp.Tests.Integration/Orders/PlaceOrderTests.cs` — new tests.
- `src/backend/TheMillionthFoodOrderApp.Tests.Integration/Orders/ListActiveOrdersTests.cs` — assert `tableNumber` propagation.

### Backend — create

- `src/backend/TheMillionthFoodOrderApp.Infrastructure/Persistence/Migrations/Brand/{timestamp}_AddOrderTableNumber.cs` (+ Designer + snapshot updates) via `dotnet ef migrations add`.

### Frontend — modify

- `src/frontend/src/router.tsx` — use `posRoutes` from feature module; remove the inline kitchen-display registration (relocated).
- `src/frontend/src/features/pos/routes.tsx` — full routes object: dashboard, kitchen, new-order, confirmation.
- `src/frontend/src/features/pos/pages/Dashboard.tsx` — replace placeholder with shop picker.
- `src/frontend/src/api/orders.ts` — add `tableNumber?: string | null` to `CreateOrderRequest`.
- `src/frontend/src/i18n/locales/{nl,fr,de}/common.json` — extend `pos.*`.

### Frontend — create

- `src/frontend/src/features/pos/pages/NewOrderPage.tsx`
- `src/frontend/src/features/pos/pages/OrderPlacedPage.tsx`
- `src/frontend/src/features/pos/components/ProductTile.tsx`
- `src/frontend/src/features/pos/components/PosTicket.tsx`
- `src/frontend/src/features/pos/components/PosModifierModal.tsx`
- `src/frontend/src/features/pos/components/OrderTypeSelector.tsx`
- `src/frontend/src/features/pos/hooks/usePosOrder.ts`
- `src/frontend/src/features/pos/hooks/useCreatePosOrder.ts`
- Tests under `src/frontend/src/features/pos/**/__tests__/*.test.{ts,tsx}` mirroring §3.9.

### Documentation

- Update `docs/dependency-tree.md` row for US-FP-018 from ⬜ to ✅ on PR.
