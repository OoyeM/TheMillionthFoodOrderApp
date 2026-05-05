# Battle Plan — US-FP-016: Place an Online Order

## Commander's Intent

We are establishing the `Order` aggregate as a first-class citizen of the platform: the moment a customer commits to a transaction, a durable record appears in the brand database with correct VAT applied, correct lifecycle entry, and a domain event on the wire. Every Layer-3 story — kitchen display, customer tracking, analytics, loyalty, receipts — depends on a well-formed `Order` existing and being queryable. The storefront must be honest about the experience: a real menu organised by category, a cart that survives page refresh, and a checkout that correctly maps the customer's intent (pickup vs eat-in vs delivery) to the backend's consumption-mode model. We are not building a demo — we are building the foundation the next twenty stories will stand on.

---

## Battle Plan Gate — Standing Order Verification

| Standing Order | Question | Answer |
|---|---|---|
| `becalmed-fleet` | Single-session instead? | No — backend and frontend tracks are genuinely independent with zero shared files. Parallel execution reduces wall-clock time. |
| `light-squadron` | Task count equals independent units? | Yes — Effects 1+2 sequentially dependent → HMS Duncan. Effects 3+4 sequentially dependent → HMS Portland. Two independent tracks = two captains. |
| `split-keel` | Exclusive file ownership? | Yes — HMS Duncan owns all of `src/backend/`; HMS Portland owns all of `src/frontend/`. No overlap. |
| `unclassified-engagement` | Every task has a risk tier? | Yes — Task 1: Station 2; Task 2: Station 1; Task 3 (red-cell): Station 2. |
| `all-hands-on-deck` | Only roles the work demands? | Yes — both captains implement directly (0 crew). Tasks are well-scoped sequential builds with no parallel sub-task branches. Red-cell is a separate squadron-level agent. |
| `skeleton-crew` | Single crew member for atomic task? | N/A — no crew on either ship. |
| `crew-without-canvas` | Every agent justified? | Yes — Duncan (substantial greenfield backend), Portland (substantial greenfield frontend), Vigilant (Station 2 adversarial review). |
| `captain-at-the-capstan` | Captain coordinating, not implementing? | N/A — 0 crew. Captains implement directly. |
| `press-ganged-navigator` | Red-cell not implementing? | Confirmed — HMS Vigilant reviews only. |
| `admiral-at-the-helm` | Admiral not implementing? | Confirmed — admiral reviews plan, coordinates, runs golden-path. No file edits. |
| `wrong-ensign` | Tools match subagents mode? | Yes — admiral uses `Agent(subagent_type)` to spawn; `TaskCreate/TaskUpdate` for visibility only; captains don't use task tools. |

---

## Task 1 — Backend: Order aggregate, repository, service, API endpoint, and tests

**Owner:** HMS Duncan (destroyer)
**Crew:** Captain implements directly (0 crew)
**Station tier:** 2 — financial data integrity; domain model is the foundation for all L3 stories; EF migration touches brand DB schema
**Execution:** Spawned in `mode: "plan"` first. Captain explores, then submits plan via ExitPlanMode. Admiral reviews. On approval, captain is respawned in `mode: "acceptEdits"` to execute.

**Deliverable:** Fully working `POST /api/brands/{brandSlug}/shops/{shopId}/orders` endpoint with Order aggregate in brand DB, correct VAT, domain event dispatched, and TUnit integration tests covering both VAT modes.

**Dependencies:** None (greenfield build on existing infrastructure).

**File ownership (exclusive — HMS Duncan):**
- `src/backend/Domain/Orders/Order.cs` — NEW
- `src/backend/Domain/Orders/OrderItem.cs` — NEW
- `src/backend/Domain/Orders/SelectedModifier.cs` — NEW (value object)
- `src/backend/Domain/Orders/OrderType.cs` — NEW (enum: Pickup, EatIn, Delivery)
- `src/backend/Domain/Orders/ConsumptionMode.cs` — NEW if not already mapped from OrderType (check existing enum usage in TaxConfiguration first)
- `src/backend/Domain/Orders/OrderCreatedEvent.cs` — NEW
- `src/backend/Domain/Orders/IOrderRepository.cs` — NEW
- `src/backend/Infrastructure/Orders/OrderRepository.cs` — NEW
- `src/backend/Infrastructure/Orders/OrderConfiguration.cs` — NEW (EF IEntityTypeConfiguration)
- `src/backend/Infrastructure/Orders/OrderItemConfiguration.cs` — NEW
- `src/backend/Application/Orders/IOrderService.cs` — NEW
- `src/backend/Application/Orders/OrderService.cs` — NEW
- `src/backend/Application/Orders/Dtos/CreateOrderRequest.cs` — NEW
- `src/backend/Application/Orders/Dtos/OrderResponse.cs` — NEW
- `src/backend/Api/Endpoints/Orders/CreateOrderEndpoint.cs` — NEW
- `src/backend/Infrastructure/Persistence/BrandDbContext.cs` — MODIFY (add DbSet<Order>)
- `src/backend/Application/DependencyInjection.cs` — MODIFY (register IOrderService)
- `src/backend/Infrastructure/DependencyInjection.cs` — MODIFY (register IOrderRepository)
- `src/backend/Tests.Integration/Orders/PlaceOrderTests.cs` — NEW
- EF migration file — NEW

**Modification targets:**
- `BrandDbContext.cs`: add `public DbSet<Order> Orders { get; }` and `modelBuilder.ApplyConfiguration(new OrderConfiguration())` / `ApplyConfiguration(new OrderItemConfiguration())`
- `Application/DependencyInjection.cs`: add `services.AddScoped<IOrderService, OrderService>()`
- `Infrastructure/DependencyInjection.cs`: add `services.AddScoped<IOrderRepository, OrderRepository>()`

**Commander's guidance:**
- Model `Order` as aggregate root with `List<OrderItem>` child (not separate aggregate). Follow `ProductRepository` pattern exactly for `IOrderRepository` / `OrderRepository`.
- Each `OrderItem` carries: `ProductId`, `ProductName` (denormalised), `Quantity`, `UnitGrossPrice`, `TaxBreakdown` per item, optional `List<SelectedModifier>`.
- `Order` carries: `OrderType`, `ShopId`, opening lifecycle status (lowest `SortOrder` from `OrderLifecycleConfig`), `OrderNumber` (short prefix UUID, shop-scoped unique), timestamps.
- Resolve product prices from DB — **never trust client-submitted prices**.
- VAT: `taxConfig.GetRateForMode(consumptionMode)` → `TaxCalculator.CalculateFromGross(unitGross, rate)` per item.
- Raise `OrderCreatedEvent` domain event on creation; repository's `SaveChangesAsync` dispatches via Wolverine/SignalR pipeline to `shop:{brandSlug}:{shopId}` group.
- Check whether `ConsumptionMode` enum already exists (used by TaxConfiguration). If so, derive from `OrderType` in the service layer rather than duplicating the enum.
- Use `BrandScopedPreProcessor` on the endpoint.
- API contract (share with frontend):
  - Route: `POST /api/brands/{brandSlug}/shops/{shopId}/orders`
  - Request: `{ orderType: "Pickup"|"EatIn"|"Delivery", customerName?: string, items: [{ productId, quantity, selectedModifierIds? }] }`
  - Response: `{ id, orderNumber, shopId, brandSlug, orderType, statusName, customerName?, items: [...], vatRatePercent, subtotalGross, totalVatAmount, totalNet, totalGross, createdAt }`
- Integration tests: `[ClassDataSource<IntegrationTestBase>(Shared = SharedType.PerClass)]`, `await Assert.That(...)` pattern. Cover: 201 happy path (Pickup), 201 happy path (EatIn with 21% VAT), 400 unknown ProductId, 400 missing required fields.
- Run `dotnet run` (not `dotnet test`) from test project to verify.

**Acceptance criteria:**
- Order persists to brand DB with all fields; OrderItem rows created as children with correct denormalised product names — *verify: integration test, read back from DB*
- OrderNumber is unique within a shop — *verify: integration test, two orders same shop*
- OrderCreatedEvent dispatched via Wolverine/SignalR pipeline to shop group — *verify: review handler wiring*
- EF migration runs cleanly against fresh brand DB — *verify: Testcontainers in integration tests*
- POST /orders returns 201 with populated OrderResponse — *verify: integration test*
- VAT is 6% for Pickup/Delivery and 21% for EatIn — *verify: integration tests, both modes*
- Client-submitted prices ignored; server resolves current product prices — *verify: integration test (send wrong price, check response)*
- Unknown ProductId returns 400 — *verify: integration test*
- Missing required fields return 400 with validation errors — *verify: integration test*

**Rollback note:** EF migration only touches brand DBs (per-brand isolation; fresh Testcontainers in tests — no prod risk). If migration fails, drop the migration file and revert BrandDbContext changes.

**admiralty-action-required: yes**
- action: Review HMS Duncan's plan output (Order aggregate shape, VAT wiring, EF config approach) and approve or redirect before execution begins.
- timing: after HMS Duncan plan phase completes (before execute phase is spawned)
- blocks: Task 1 execution phase

---

## Task 2 — Frontend: Storefront menu, cart context, checkout, and order confirmation

**Owner:** HMS Portland (frigate)
**Crew:** Captain implements directly (0 crew)
**Station tier:** 1 — user-visible; couples to new cart context and SignalR

**Deliverable:** `/shops/:shopId/menu` route with category-organised menu and cart; `/checkout` route with order type selection and form submission; `/order/:orderId` confirmation page with real-time SignalR status updates.

**Dependencies:** None during build (builds against API contract defined in estimate). Wires to live backend endpoint after Task 1 completes.

**File ownership (exclusive — HMS Portland):**
- `src/frontend/src/features/storefront/routes.tsx` — MODIFY (add new routes)
- `src/frontend/src/features/storefront/context/CartContext.tsx` — NEW
- `src/frontend/src/features/storefront/pages/MenuPage.tsx` — NEW
- `src/frontend/src/features/storefront/pages/CheckoutPage.tsx` — NEW
- `src/frontend/src/features/storefront/pages/OrderConfirmationPage.tsx` — NEW
- `src/frontend/src/features/storefront/components/ProductCard.tsx` — NEW
- `src/frontend/src/features/storefront/components/ModifierModal.tsx` — NEW
- `src/frontend/src/features/storefront/components/CartDrawer.tsx` — NEW
- `src/frontend/src/features/storefront/hooks/useStorefrontMenu.ts` — NEW
- `src/frontend/src/features/storefront/hooks/useCreateOrder.ts` — NEW
- `src/frontend/src/api/orders.ts` — NEW
- `src/frontend/src/i18n/locales/nl/common.json` — MODIFY (append ordering keys)
- `src/frontend/src/i18n/locales/fr/common.json` — MODIFY (append ordering keys)
- `src/frontend/src/i18n/locales/de/common.json` — MODIFY (append ordering keys)

**Commander's guidance:**
- Add routes under the existing `/:brandSlug/:lang` shell: `/shops/:shopId/menu`, `/checkout`, `/order/:orderId`.
- `CartContext`: React Context + `useReducer`. Persist to localStorage under key `cart:{brandSlug}:{shopId}` — enforces shop isolation. Cart items: `{ productId, productName, quantity, unitGrossPrice, selectedModifiers[] }`. Actions: `ADD_ITEM`, `REMOVE_ITEM`, `UPDATE_QUANTITY`, `CLEAR_CART`.
- `useStorefrontMenu`: thin TanStack Query wrapper over existing `menuCategoriesApi.list()` and `menuCategoriesApi.listProducts()`. No new API module — reuse existing.
- `MenuPage`: fetch categories in sortOrder, render products per category. `ProductCard` shows name, price (gross), allergen icons. Products with modifier groups open `ModifierModal` before adding to cart. `CartDrawer` slides in from the right, shows itemised cart with line totals, Checkout button.
- `CheckoutPage`: `react-hook-form` + `zod`. Fields: `orderType` (radio: Pickup / EatIn / Delivery), `customerName` (optional text). Show VAT notice: 6% for Pickup/Delivery, 21% for EatIn. Payment placeholder ("Pay at pickup" / "Pay at counter / Pay at cashier") — no payment form. `useCreateOrder` mutation calls `ordersApi.create()`. On success navigate to `/order/:orderId`.
- `OrderConfirmationPage`: display order number, current status, itemised summary. Wire `useSignalR({ orderId })` — display status updates as they arrive. Use existing `useOrderUpdates` hook from `src/frontend/src/api/useOrderUpdates.ts` (marked `@expected-unused`, ready to consume).
- `ordersApi.create()`: POST to `/api/brands/{brandSlug}/shops/{shopId}/orders` (see contract in Task 1).
- i18n: add keys under `storefront.menu.*`, `storefront.cart.*`, `storefront.checkout.*`, `storefront.order.*` in all three locale files. NL as primary; FR and DE can be NL values for now (marked TODO for translation).
- Follow inline-styles pattern (no new CSS library). Reuse existing form field and section patterns from admin forms where applicable.

**Acceptance criteria:**
- Menu renders categories in sortOrder with products — *verify: visual in dev browser*
- Adding simple product to cart increments quantity correctly — *verify: visual*
- Adding product with modifiers prompts modifier selection first — *verify: visual*
- Cart persists across page refresh (localStorage) — *verify: refresh page, cart unchanged*
- Cart scoped to shop — navigating to another shop shows empty cart — *verify: visual*
- Cart shows itemised list with unit prices and line totals — *verify: visual*
- Checkout form validates required fields before submit — *verify: visual, try submitting empty*
- EatIn shows 21% VAT notice; Pickup/Delivery shows 6% — *verify: visual, toggle radio*
- Successful submit navigates to confirmation with correct order number — *verify: visual (may need mock or live backend)*
- Confirmation page receives and displays real-time SignalR status updates — *verify: trigger SimulateOrderStatusChangeEndpoint in dev, observe UI update*
- Payment shows clear deferred placeholder — *verify: visual*

**Rollback note:** All changes are additive (new pages + modified routes). No existing pages altered. Revert by removing new routes and files if needed.

**admiralty-action-required: no**

---

## Task 3 — Red-Cell Review: Backend domain model and endpoint

**Owner:** HMS Vigilant (red-cell navigator)
**Station tier:** 2 (inherits parent task tier)
**Dependencies:** Task 1 (execute phase) must complete first.

**Deliverable:** Adversarial review of the Order aggregate design, VAT application logic, and integration test coverage. Written verdict: pass or list of issues.

**Scope:** Review `Order.cs`, `OrderItem.cs`, `CreateOrderEndpoint.cs`, `OrderService.cs`, `PlaceOrderTests.cs`. Check:
- Denormalisation is complete (ProductName captured, price not re-fetched at display time)
- VAT applied per item not per order total
- No client-submitted prices trusted
- EF configuration correctly owns OrderItem (no orphaned rows possible)
- Integration tests cover both VAT modes and failure cases
- OrderCreatedEvent is raised and will reach the SignalR pipeline
- No obvious data integrity gaps (e.g., race condition on OrderNumber uniqueness)

**admiralty-action-required: no**

---

## Execution Sequence

```
[Admiral] Create TaskCreate entries (visibility)
    │
    ├── Spawn HMS Duncan (plan mode) ──────────────────────────────┐
    │                                                              │
    └── Spawn HMS Portland (acceptEdits) ──────────────────────── │ parallel
                                                                   │
    ◆ ADMIRALTY ACTION: Review HMS Duncan plan → approve/redirect  │
    │                                                              │
    Spawn HMS Duncan (execute, acceptEdits) ◄─────────────────────┘
    │                   (Portland may complete while Duncan executes)
    │
    ├── Await HMS Duncan complete
    └── Await HMS Portland complete
         │
    Spawn HMS Vigilant (red-cell review of backend)
         │
    Admiral: golden-path run in dev browser
    (menu → cart → checkout → confirmation + SignalR)
         │
    Stand down
```
