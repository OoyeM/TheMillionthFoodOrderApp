# Battle Plan — US-FP-058: Complete Payment Flow (Mocked)

## Commander's Intent

The ordering flow from US-FP-016 is functionally complete but experientially incomplete — a customer can build a cart and submit, but has never been asked how they intend to pay. This story closes that gap. The intent is to give the checkout a payment step that feels deliberate: the customer picks a method, online methods briefly simulate processing before succeeding, and the confirmation page reflects what happened. The structural requirement — that the UI can be replaced by a real gateway without redesign — means we are building a seam, not a stub. Every naming decision and component boundary should assume Mollie or Stripe will one day sit behind it.

---

## Battle Plan Gate — Standing Order Verification

- **becalmed-fleet**: Two fully independent tasks (backend + frontend) — multi-agent is correct, not single-session.
- **light-squadron**: 2 captains for 2 independent work units — not under-split.
- **split-keel**: HMS Kent owns all `src/backend/` files; HMS Lancaster owns all `src/frontend/` files — no overlap.
- **unclassified-engagement**: Both tasks Station 1 — classified.
- **all-hands-on-deck**: Both captains implement directly — no unjustified crew.
- **skeleton-crew**: N/A — 0 crew.
- **crew-without-canvas**: N/A — no crew.
- **captain-at-the-capstan**: N/A — no crew.
- **press-ganged-navigator**: No red-cell navigator — Station 1 work.
- **admiral-at-the-helm**: Admiral coordinates only, no implementation.
- **wrong-ensign**: subagents mode — captains report via Agent return value; admiral uses TaskCreate/TaskUpdate for visibility only.

---

## Task 1 — PaymentMethod on Order Domain and API

**Ship:** HMS Kent (frigate)
**Owner:** assigned at formation
**Station tier:** 1 (Caution — user-visible API change)
**admiralty-action-required:** no

**File ownership:**
- `src/backend/TheMillionthFoodOrderApp.Domain/Orders/PaymentMethod.cs` (new)
- `src/backend/TheMillionthFoodOrderApp.Domain/Orders/Order.cs`
- `src/backend/TheMillionthFoodOrderApp.Application/Orders/Dtos/CreateOrderRequest.cs`
- `src/backend/TheMillionthFoodOrderApp.Application/Orders/Dtos/OrderResponse.cs`
- `src/backend/TheMillionthFoodOrderApp.Application/Orders/OrderService.cs`
- `src/backend/TheMillionthFoodOrderApp.Api/Endpoints/Orders/CreateOrderEndpoint.cs`
- `src/backend/TheMillionthFoodOrderApp.Infrastructure/Orders/OrderConfiguration.cs`
- `src/backend/TheMillionthFoodOrderApp.Infrastructure/Persistence/Migrations/Brand/[timestamp]_AddPaymentMethod.cs` (new)
- `src/backend/TheMillionthFoodOrderApp.Tests.Integration/Orders/PlaceOrderTests.cs`

**Modification targets:**
- `Order.Create()` factory method — add `PaymentMethod paymentMethod` parameter
- `CreateOrderRequest` record — add `string PaymentMethod` field
- `OrderResponse` record — add `string PaymentMethod` field
- `OrderService.CreateOrderAsync()` — pass `PaymentMethod` through to `Order.Create()`
- `CreateOrderEndpoint` ApiRequest model — add `PaymentMethod` field; map to application DTO
- `OrderConfiguration` — add `.Property(o => o.PaymentMethod)` mapping
- `PlaceOrderTests` — add `PaymentMethod = "CashAtPickup"` to all 11 existing request bodies

**Deliverable:** PaymentMethod flows end-to-end: enum in Domain, column in DB via migration, accepted in POST /orders request (string, case-insensitive), returned in OrderResponse, integration tests all green.

**Acceptance criteria:**
- `PaymentMethod` enum in `Domain/Orders/` with `CashAtPickup = 0`, `CreditCard = 1`, `Bancontact = 2` — verified by build
- `Order.Create()` accepts and stores `PaymentMethod` — verified by build
- `POST /orders` accepts `paymentMethod` string (case-insensitive) — verified by integration test
- `OrderResponse` includes `paymentMethod` as string — verified by integration test
- Migration adds `PaymentMethod INT NOT NULL DEFAULT 0` — verified by reviewing generated SQL
- All 11 existing integration tests pass with `paymentMethod` added — verified by `dotnet test`
- New test `PlaceOrder_WithCreditCard_StoresPaymentMethod` passes — verified by `dotnet test`

**Validation required:** `dotnet build` output + `dotnet test --filter PlaceOrderTests` output included in report.
**Rollback note required:** yes — `dotnet ef migrations remove` removes column; revert DTOs and enum.

---

## Task 2 — Payment Selection, Mock Screen, and Confirmation Status (Frontend)

**Ship:** HMS Lancaster (frigate)
**Owner:** assigned at formation
**Station tier:** 1 (Caution — user-facing checkout flow)
**admiralty-action-required:** no

**File ownership:**
- `src/frontend/src/features/storefront/pages/CheckoutPage.tsx`
- `src/frontend/src/features/storefront/components/MockPaymentScreen.tsx` (new)
- `src/frontend/src/api/orders.ts`
- `src/frontend/src/features/storefront/pages/OrderConfirmationPage.tsx`
- `src/frontend/src/i18n/locales/nl/common.json`
- `src/frontend/src/i18n/locales/fr/common.json`
- `src/frontend/src/i18n/locales/de/common.json`

**Modification targets:**
- `CheckoutFormValues` Zod schema — add required `paymentMethod` field
- `CheckoutForm` component — replace `{/* Payment placeholder */}` block with radio group; add mock screen trigger logic on submit
- `paymentNotice` variable — remove (superseded by selection UI)
- `CreateOrderRequest` TypeScript type — add `paymentMethod: string`
- `OrderResponse` TypeScript type — add `paymentMethod: string`
- `ordersApi.create()` call — pass `paymentMethod` from form values
- `OrderConfirmationPage` info grid — add fifth `InfoCard` for payment status

**Deliverable:** Three payment method radio buttons in checkout; `MockPaymentScreen` component shown for ~1.5s on online methods before navigating to confirmation; confirmation page shows payment status card; all i18n keys present in nl/fr/de; TypeScript clean.

**API contract (established in Estimate — do not deviate):**
- Send: `paymentMethod: "CashAtPickup" | "CreditCard" | "Bancontact"` in POST /orders body
- Receive: `paymentMethod: string` in OrderResponse

**Acceptance criteria:**
- Checkout shows three payment method radio buttons — verified visually
- `paymentMethod` required; form cannot submit without selection — verified by type-check + visual
- `CashAtPickup` navigates directly to confirmation — verified visually
- `CreditCard`/`Bancontact` show `MockPaymentScreen` for ~1.5s then navigate to confirmation — verified visually
- `CreateOrderRequest` type includes `paymentMethod: string` — verified by `pnpm build`
- `OrderResponse` type includes `paymentMethod: string` — verified by `pnpm build`
- Confirmation page shows payment status info card — verified visually
- Online methods show "paid" label, CashAtPickup shows "pay at pickup" — verified visually
- i18n keys present in all three locales — verified by review

**Validation required:** `pnpm build` output included in report. Visual description of each AC in the report.
**Rollback note required:** yes — revert CheckoutPage.tsx, OrderConfirmationPage.tsx, api/orders.ts; delete MockPaymentScreen.tsx; revert i18n files.

---

## Ship Manifests

### HMS Kent

```
Ship:     HMS Kent
Captain:  TBD (assigned at formation)
Task:     Task 1 — PaymentMethod on Order Domain and API
Crew:     Captain implements directly
Marines:  0
```

### HMS Lancaster

```
Ship:     HMS Lancaster
Captain:  TBD (assigned at formation)
Task:     Task 2 — Payment Selection, Mock Screen, and Confirmation Status
Crew:     Captain implements directly
Marines:  0
```

---

## Execution Mode

**Mode:** subagents

Both tasks are fully independent — no shared files, no sequencing dependency, API contract established in this estimate. Captains report to the admiral via Agent return value only. Admiral tracks progress via TaskCreate/TaskUpdate/TaskList for user visibility (Ctrl+T).
