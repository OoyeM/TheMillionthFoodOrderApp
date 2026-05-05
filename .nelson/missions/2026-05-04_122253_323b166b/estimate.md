# The Estimate — US-FP-016: Place an Online Order

## 1. Reconnaissance

Two Explore agents were dispatched simultaneously — one into the backend, one into the frontend.

**Backend** is a clean greenfield build on proven infrastructure. The `Order` aggregate, `IOrderRepository`, `OrderService`, DTOs, and `CreateOrderEndpoint` do not exist. Everything they depend on is already battle-tested: `TaxCalculator.CalculateFromGross(gross, rate)` returns a full `TaxBreakdown`; `TaxConfiguration.GetRateForMode(ConsumptionMode)` gives the rate (6% Pickup/Delivery, 21% EatIn); `OrderLifecycleConfig.Statuses` provides the opening status per shop; and the Wolverine/SignalR pipeline (`OrderHub`, `OrderStatusChangedHandler`) is ready to receive a new `OrderCreatedEvent`. The repository pattern (explicit per-aggregate interface + EF Core implementation), FastEndpoints one-class-per-endpoint convention, and `BrandScopedPreProcessor` are all clear and consistent. `BrandDbContext` needs two new `DbSet<>` registrations and EF `IEntityTypeConfiguration` classes; the DI container needs `IOrderService` and `IOrderRepository` registrations. The only existing order-adjacent endpoint is `SimulateOrderStatusChangeEndpoint` — dev tooling only, not a foundation.

**Frontend** is a skeleton storefront waiting to be populated. The router has a brand-scoped shell (`/:brandSlug/:lang`) but only a Home page under the storefront feature. Cart state does not exist; the app has no Zustand or equivalent. The SignalR client and `useSignalR` hook are fully implemented and ready to consume. The API pattern (Axios modules → TanStack Query hooks) is clear; admin hooks already cover products and categories; storefront variants need creating. i18n is wired with NL/FR/DE locale files but has no ordering keys. The `AuthContext` provides user identity for the checkout form.

No surprises requiring mission reframing. The only design decision — cart state strategy — is resolved: React Context + `useReducer` backed by localStorage, consistent with the app's "deliberately simple" stance and introducing no new dependency.

## 2. Intent

We are establishing the `Order` aggregate as a first-class citizen of the platform: the moment a customer commits to a transaction, a durable record appears in the brand database with correct VAT applied, correct lifecycle entry, and a domain event on the wire. Every Layer-3 story — kitchen display, customer tracking, analytics, loyalty, receipts — depends on a well-formed `Order` existing and being queryable. The storefront must be honest about the experience: a real menu organised by category, a cart that survives page refresh, and a checkout that correctly maps the customer's intent (pickup vs eat-in vs delivery) to the backend's consumption-mode model. We are not building a demo — we are building the foundation the next twenty stories will stand on.

## 3. Effects

### Effect 1: Order aggregate and persistence

The `Order` aggregate root with a `List<OrderItem>` child collection, EF configuration, and brand-DB persistence that turn a checkout submission into a durable record.

**Commander's guidance:** `OrderItem` is not a separate aggregate — it is a child entity owned by `Order`. Each item carries `ProductId`, `ProductName` (denormalised — product names change over time), `Quantity`, `UnitGrossPrice`, `TaxBreakdown` per item, and an optional `List<SelectedModifier>` value object. The `Order` aggregate carries `OrderType` (enum: `Pickup`, `EatIn`, `Delivery`), `ShopId`, the opening lifecycle status (fetched from `OrderLifecycleConfig` — the status with lowest `SortOrder`), a human-readable `OrderNumber` (short UUID prefix, shop-scoped), and timestamps. Raise an `OrderCreatedEvent` domain event on creation so the Wolverine/SignalR pipeline notifies the `shop:{brandSlug}:{shopId}` group. Follow the existing `ProductRepository` pattern exactly for `IOrderRepository` + `OrderRepository`. Add `DbSet<Order>` to `BrandDbContext` and wire EF configurations in `OnModelCreating`.

**Acceptance criteria:**
- `Order` persists to the brand DB with all fields; `OrderItem` rows created as children with correct denormalised product names
- `OrderNumber` is unique within a shop
- `OrderCreatedEvent` is raised and dispatched via the Wolverine/SignalR pipeline to `shop:{brandSlug}:{shopId}`
- EF migration runs cleanly against a fresh brand DB (verified in Testcontainers)

---

### Effect 2: Place-order API endpoint

`POST /api/brands/{brandSlug}/shops/{shopId}/orders` — validates request, resolves product prices from DB, applies VAT, creates the `Order` aggregate, and returns the persisted order.

**Commander's guidance:** One `CreateOrderEndpoint` class in `/Api/Endpoints/Orders/`. Request body carries `OrderType` (enum string), `CustomerName` (optional string), and `Items` (array of `{ ProductId, Quantity, SelectedModifierIds? }`). The endpoint resolves each product from the brand DB to get current `BasePrice` — client-submitted prices are ignored. VAT is applied per item via `TaxCalculator.CalculateFromGross(unitGross, rate)` where `rate = taxConfig.GetRateForMode(consumptionMode)`. The opening lifecycle status is the first status from `OrderLifecycleConfig` (lowest `SortOrder`). Use `BrandScopedPreProcessor`. Response is `OrderResponse` with order number, status, itemised breakdown (net + VAT + gross per item), and order-level totals. Cover with TUnit integration tests using `IntegrationTestBase` / `ClassDataSource<>` pattern.

**API contract** (shared with frontend captain):
- Route: `POST /api/brands/{brandSlug}/shops/{shopId}/orders`
- Request: `{ orderType: "Pickup"|"EatIn"|"Delivery", customerName?: string, items: [{ productId: string, quantity: number, selectedModifierIds?: string[] }] }`
- Response: `{ id, orderNumber, shopId, brandSlug, orderType, statusName, customerName?, items: [{ productId, productName, quantity, unitGrossPrice, unitNetPrice, unitVatAmount, lineTotal }], vatRatePercent, subtotalGross, totalVatAmount, totalNet, totalGross, createdAt }`

**Acceptance criteria:**
- `POST /orders` with valid body returns `201` with populated `OrderResponse`
- VAT is 6% for `Pickup`/`Delivery` and 21% for `EatIn` — both modes verified in integration tests
- Client-submitted prices are ignored; server resolves current product prices
- Unknown `ProductId` returns `400`
- Missing required fields return `400` with validation errors
- Integration tests pass under TUnit (`dotnet run` from test project)

---

### Effect 3: Storefront menu page

New storefront routes and a `MenuPage` that renders the shop's menu by category, allows adding items (including modifier selection) to a persistent cart, and shows a cart summary.

**Commander's guidance:** Add routes to `features/storefront/routes.tsx`: `/shops/:shopId/menu`, `/checkout`, `/order/:orderId`. `CartContext` (React Context + `useReducer`) backed by `localStorage` under key `cart:{brandSlug}:{shopId}` — enforces shop isolation. Cart items carry `productId`, `productName`, `quantity`, `unitGrossPrice`, `selectedModifiers[]`. Create storefront-flavoured TanStack Query hooks (`useStorefrontMenuCategories`, `useStorefrontCategoryProducts`) as thin wrappers over existing `menuCategoriesApi` — no new API layer. Modifier selection: inline expander or modal when product has modifier groups. Add i18n keys under `storefront.menu.*` and `storefront.cart.*` in all three locale files.

**Acceptance criteria:**
- Menu renders categories in `sortOrder` with products
- Adding a simple product increments quantity correctly
- Adding a product with modifiers prompts modifier selection first
- Cart persists across page refresh (localStorage)
- Cart is scoped to current shop — navigating to another shop shows empty cart
- Cart shows itemised list with unit prices and line totals

---

### Effect 4: Checkout flow and order confirmation

`/checkout` route collecting order type and customer name, submitting to `POST /orders`, followed by `/order/:orderId` confirmation/tracking page with real-time status via SignalR.

**Commander's guidance:** `CheckoutPage` uses `react-hook-form` + `zod`. Collect `OrderType` (radio: Pickup / EatIn / Delivery) and `CustomerName` (optional text). Show VAT rate notice based on selection (6% vs 21%). Payment is deferred (US-FP-058) — render a clear "Pay at pickup" / "Pay at counter" placeholder. New `useCreateOrder` mutation hook calling `ordersApi.create()`. On success, navigate to `/order/:orderId`. `OrderConfirmationPage` shows order number, status, and itemised summary; wires `useSignalR({ orderId })` for real-time status updates. Add i18n keys under `storefront.checkout.*` and `storefront.order.*` in all three locales.

**Acceptance criteria:**
- Checkout form validates required fields before submit
- `EatIn` shows 21% VAT notice; `Pickup`/`Delivery` shows 6%
- Successful submit navigates to order confirmation with correct order number
- Order confirmation page receives and displays real-time status updates via SignalR
- Payment UI shows clear deferred placeholder — no broken payment state

---

## 4. Terrain

**Effect 1** lands entirely in the backend domain and infrastructure layers. New files only, except for two modifications: `BrandDbContext.cs` (add `DbSet<Order>`) and the two DI registration files. The EF migration touches the brand DB schema — low risk given per-brand isolation and Testcontainers coverage.

**Effect 2** lands in the application and API layers. New files only except the DI registrations (shared with Effect 1). The integration test file is net-new. No existing endpoints modified.

**Effect 3** lands in the frontend storefront feature and i18n files. `routes.tsx` is modified (additive). All other changes are new files. The existing `menuCategoriesApi` is called but not modified.

**Effect 4** continues in the storefront feature. New pages and hooks only. Routes file modified again (additive).

**Blast radius:** Effects 1+2 are contained to the backend with no modification of existing domain aggregates. Effects 3+4 are purely additive on the frontend. The i18n locale files are append-only. No existing pages or endpoints are altered.

## 5. Forces

**Two captains**, running their tracks in parallel:

- **HMS Resolute** — frigate, Sonnet — Effects 1+2 (backend: aggregate → repository → endpoint → tests). Captain implements directly; no crew needed. Station 2 — red-cell navigator reviews the Order aggregate design and `CreateOrderEndpoint` VAT logic before integration tests run. Spawned in `mode: "plan"` so the admiral reviews the Order aggregate shape before implementation begins — this model is the foundation for twenty downstream stories.

- **HMS Swift** — frigate, Sonnet — Effects 3+4 (frontend: routes → cart context → menu page → checkout → confirmation). Captain implements directly; no crew needed. Station 1 — independent review at completion (admiral visual inspection).

- **HMS Vigilant** — red-cell navigator — reviews HMS Resolute's plan (aggregate design + VAT application) before execution begins, and reviews the completed `CreateOrderEndpoint` + integration tests before the backend track closes.

Three squadron-level agents total (admiral + 2 captains + red-cell). No marines required.

## 6. Coordination

**Two parallel tracks:**

```
Track A (Backend — HMS Resolute):   Effect 1 ──► Effect 2 ──► red-cell review
Track B (Frontend — HMS Swift):     Effect 3 ──► Effect 4

                                    ├── Both complete ──► Admiral golden-path run
```

Effect 1 must precede Effect 2 within Track A (endpoint needs the aggregate). Effects 3 and 4 are sequential within Track B (checkout needs cart context). The two tracks are fully independent during build.

**Shared artifact — API contract** is defined in full in Effect 2's commander's guidance. HMS Swift builds against this contract from the estimate. No inter-captain coordination file needed; if the contract changes during HMS Resolute's work, the admiral amends this estimate with a dated addendum and notifies HMS Swift via `SendMessage`.

**Coordination surface:** when both tracks complete, the admiral runs the full stack in dev and walks the golden path. This is the single integration point.

## 7. Control

**Action station tiers:**
- Effect 1 (Order aggregate + EF migration): **Station 2** — financial data integrity; domain model is the foundation for all L3 stories; EF migration modifies brand DB schema.
- Effect 2 (API endpoint + tests): **Station 2** — VAT calculation touches financial figures; red-cell review required.
- Effect 3 (Storefront menu + cart): **Station 1** — user-visible; coupled to cart context.
- Effect 4 (Checkout + confirmation): **Station 1** — user-visible form submission; SignalR wiring.

**Quality gates:**
1. **HMS Resolute exits plan mode** → admiral reviews Order aggregate shape and VAT application design before a line is written. This is the highest-leverage review point — the model must be right.
2. **Integration tests pass** → gate for Track A completion. Both VAT modes (Pickup and EatIn) must be covered.
3. **Red-cell review (HMS Vigilant)** → reviews aggregate + endpoint after Track A completes.
4. **Admiral visual inspection** → walks the storefront golden path in dev browser after both tracks complete: menu renders → add to cart → cart persists on refresh → checkout → confirmation shows order number → SignalR status update appears.

**Rollback:**
- EF migration affects only brand DBs (per-brand isolation; Testcontainers creates fresh instances for tests — no prod risk during this build).
- Frontend changes are purely additive. No existing routes are modified in a breaking way.
- If the domain model proves wrong during integration, Effect 1 is self-contained enough to amend without touching the rest of the codebase.
