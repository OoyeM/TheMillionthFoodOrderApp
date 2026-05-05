# The Estimate — US-FP-058: Complete Payment Flow (Mocked)

## 1. Reconnaissance

Two scouts were dispatched in parallel — one into the backend Order domain, one into the frontend checkout flow.

**Backend terrain is a clean slate.** The Order aggregate (`Domain/Orders/Order.cs`) has no payment fields. The last migration (`20260504131405_AddOrders.cs`) created Orders, OrderItems, and OrderItemSelectedModifiers with no payment column. The `CreateOrderRequest` DTO carries ShopId, BrandSlug, OrderType, CustomerName, and Items only. `OrderResponse` mirrors it with price/VAT decomposition added. All price resolution happens server-side in `OrderService.cs` — nothing is trusted from the client. The endpoint is `POST /api/brands/{brandSlug}/shops/{shopId}/orders` via `CreateOrderEndpoint.cs`. Eleven integration tests in `PlaceOrderTests.cs` exercise happy paths (VAT rates, multi-item aggregation, modifier pricing) and error cases — all will need `PaymentMethod` added to their request bodies.

**Frontend terrain is more prepared than expected.** `CheckoutPage.tsx` already contains a payment placeholder block (marked `{/* Payment placeholder */}`) with a `paymentNotice` variable that branches by order type: EatIn → "pay at counter", Pickup → "pay at pickup", Delivery → "coming soon". The `CheckoutFormValues` Zod schema has only `orderType` and `customerName` — no payment field yet. `CreateOrderRequest` in `api/orders.ts` matches the backend exactly, missing only payment method. The confirmation page (`OrderConfirmationPage.tsx`) displays four info cards; a fifth payment status card slots in cleanly. i18n keys live in `storefront.checkout.*` across nl/fr/de `common.json`; NL already has `paymentAtCounter`, `paymentAtPickup`, and `paymentOnline` keys.

No surprises. The placeholder UI, clean backend slate, and symmetric DTO structure make this an extension rather than a retrofit.

---

## 2. Intent

The ordering flow from US-FP-016 is functionally complete but experientially incomplete — a customer can build a cart and submit, but has never been asked how they intend to pay. This story closes that gap. The intent is to give the checkout a payment step that feels deliberate: the customer picks a method, online methods briefly simulate processing before succeeding, and the confirmation page reflects what happened. The structural requirement — that the UI can be replaced by a real gateway without redesign — means we are building a seam, not a stub. Every naming decision and component boundary should assume Mollie or Stripe will one day sit behind it.

---

## 3. Effects

### Effect 1: PaymentMethod on the Order domain and API

The Order aggregate gains a `PaymentMethod` property captured at order creation time. A new `PaymentMethod` enum in the Domain carries three values: `CashAtPickup`, `CreditCard`, `Bancontact`. The property flows through every layer — `CreateOrderRequest` DTO, `OrderResponse` DTO, `OrderService.CreateOrderAsync`, `CreateOrderEndpoint`, `OrderConfiguration` EF mapping, and a new migration. The eleven integration tests in `PlaceOrderTests.cs` all need the new field added to their request bodies.

**Commander's guidance:** `PaymentMethod` is an `int` column, stored as enum ordinal, `NOT NULL`. Keep the enum in `Domain/Orders/` alongside `OrderType`. No domain events for payment in this story. Default in migration: `0` (CashAtPickup) for any pre-existing rows. Accept the field as a string in the API request (parsed case-insensitively to enum, consistent with how `OrderType` is handled). Serialize it back as a string in `OrderResponse`.

**Acceptance criteria:**
- `PaymentMethod` enum exists in `Domain/Orders/` with `CashAtPickup = 0`, `CreditCard = 1`, `Bancontact = 2`
- `Order.Create()` accepts and stores `PaymentMethod`
- `POST /orders` request accepts `paymentMethod` string, parsed to enum (case-insensitive)
- `OrderResponse` includes `paymentMethod` as string
- Migration adds `PaymentMethod INT NOT NULL DEFAULT 0` to Orders table
- All 11 existing integration tests pass with `paymentMethod` added to request bodies
- New test `PlaceOrder_WithCreditCard_StoresPaymentMethod` verifies the field round-trips correctly

---

### Effect 2: Payment selection and mock processing in checkout

The payment placeholder in `CheckoutPage.tsx` is replaced with a real selection step: three radio options (Credit card, Bancontact, Cash at pickup). The `CheckoutFormValues` Zod schema gains a required `paymentMethod` field. On form submit, the selected method is sent in the API request. For `CashAtPickup`, the order is created and the user navigates directly to confirmation. For `CreditCard` or `Bancontact`, a mock payment screen is shown in-place after order creation that displays a processing animation for ~1.5 seconds, then auto-navigates to confirmation.

**Commander's guidance:** The mock screen should live in a new component `MockPaymentScreen` in `src/features/storefront/components/`. It receives the `orderId` and a callback (or simply navigates internally after the delay). Isolate it so a real gateway can replace it by swapping this single component. Use a named constant for the delay (`MOCK_PAYMENT_DELAY_MS = 1500`). The radio group should use the existing form/input patterns. Pass `paymentMethod` as a string matching backend enum names (`CashAtPickup`, `CreditCard`, `Bancontact`). Update `CreateOrderRequest` type in `api/orders.ts` to include `paymentMethod: string`. The existing `paymentNotice` variable can be removed; its job is superseded by the selection UI.

**Acceptance criteria:**
- Checkout form shows three payment method radio buttons with translated labels
- `paymentMethod` is required; form cannot submit without selection
- `CashAtPickup` creates order and navigates immediately to confirmation
- `CreditCard` / `Bancontact` show `MockPaymentScreen` for ~1.5s then navigate to confirmation
- `createOrder` request body includes `paymentMethod` field
- `CreateOrderRequest` TypeScript type updated to include `paymentMethod: string`
- i18n keys present in nl, fr, de for all new strings (method labels, processing screen copy)

---

### Effect 3: Payment status on order confirmation

The confirmation page gains a fifth info card showing payment status. The display value derives from `paymentMethod` on `OrderResponse`: online methods (`CreditCard`, `Bancontact`) show a "Paid" label; `CashAtPickup` shows "Pay at pickup". No live-update behaviour needed — the value is static from the order response.

**Commander's guidance:** Add the card to the existing four-card info grid in `OrderConfirmationPage.tsx`. Reuse the `InfoCard` component already present. Derive the display string from `paymentMethod` using a dedicated translation lookup (e.g., `storefront.checkout.payment.status.*`). Keep it a pure display concern — no new API calls or state.

**Acceptance criteria:**
- Confirmation page shows a payment status info card
- Online methods (`CreditCard`, `Bancontact`) display a translated "paid" label
- `CashAtPickup` displays a translated "pay at pickup" label
- i18n keys present in nl, fr, de
- `OrderResponse` TypeScript type updated to include `paymentMethod: string`

---

## 4. Terrain

**Backend — Effect 1:**

| File | Action |
|------|--------|
| `Domain/Orders/PaymentMethod.cs` | New file — enum |
| `Domain/Orders/Order.cs` | Add `PaymentMethod` property; update `Create()` signature |
| `Application/Orders/Dtos/CreateOrderRequest.cs` | Add `PaymentMethod` field |
| `Application/Orders/Dtos/OrderResponse.cs` | Add `PaymentMethod` field |
| `Application/Orders/OrderService.cs` | Pass `PaymentMethod` through to `Order.Create()` |
| `Api/Endpoints/Orders/CreateOrderEndpoint.cs` | Add to `CreateOrderApiRequest`; map to application DTO |
| `Infrastructure/Orders/OrderConfiguration.cs` | Add EF column mapping |
| `Infrastructure/Persistence/Migrations/Brand/[timestamp]_AddPaymentMethod.cs` | New migration |
| `Tests.Integration/Orders/PlaceOrderTests.cs` | Update 11 tests; add 1 new test |

Blast radius: contained to the Orders vertical. No shared domain contracts touched.

**Frontend — Effects 2 & 3:**

| File | Action |
|------|--------|
| `features/storefront/pages/CheckoutPage.tsx` | Replace placeholder; add payment radio group; trigger `MockPaymentScreen` for online methods |
| `features/storefront/components/MockPaymentScreen.tsx` | New file — processing animation + auto-navigate |
| `api/orders.ts` | Add `paymentMethod: string` to `CreateOrderRequest` type and `OrderResponse` type |
| `features/storefront/pages/OrderConfirmationPage.tsx` | Add fifth info card for payment status |
| `i18n/locales/nl/common.json` | Add payment method labels + processing screen + status keys |
| `i18n/locales/fr/common.json` | Same |
| `i18n/locales/de/common.json` | Same |

Blast radius: storefront checkout and confirmation pages only. Cart context, routing, and menu pages are untouched.

---

## 5. Forces

Two captains, fully parallel. No crew needed — each task is a bounded implementation on a single subsystem by one skilled engineer.

**HMS Resolute** (frigate) — Backend captain. Owns all backend files for Effect 1. Captain implements directly.

**HMS Swift** (frigate) — Frontend captain. Owns all frontend files for Effects 2 and 3. Captain implements directly.

No red-cell navigator at station-formation time. Both tasks are **Station 1** (user-visible API/UI changes, moderate coupling, reversible). Admiral provides independent review at quarterdeck checkpoint per Station 1 controls.

---

## 6. Coordination

The two captains are fully independent. The coordination surface is the API contract — the `paymentMethod` string values (`CashAtPickup`, `CreditCard`, `Bancontact`) — which is established in this estimate and requires no runtime handoff.

```
HMS Resolute (Backend, Effect 1)   ──────────────────────────────► Admiral review
HMS Swift    (Frontend, Effects 2+3) ──────────────────────────────► Admiral review
                                                                         │
                                                                   Stand-down
```

Both captains start from the same API contract. No blocking dependency between them. The backend captain does not need to complete before the frontend captain begins.

Within HMS Swift's work, Effects 2 and 3 share the `OrderResponse` type update (`paymentMethod` field) as a common foundation — the captain should update `api/orders.ts` first, then build the checkout UI and confirmation card.

---

## 7. Control

**Quality gates:**

| Ship | Gate | Tool |
|------|------|------|
| HMS Resolute | Build passes | `dotnet build` |
| HMS Resolute | All integration tests pass (11 existing + 1 new) | `dotnet test --filter PlaceOrderTests` |
| HMS Swift | TypeScript compilation clean | `pnpm build` |
| HMS Swift | Visual: payment selection renders, mock screen shows, confirmation card present | Manual / dev server |

**Station 1 controls (both tasks):**
- Validation evidence: test output / build output included in report
- Failure case noted: what breaks if `paymentMethod` is omitted from request
- Rollback: backend — `dotnet ef migrations remove`; frontend — git revert of CheckoutPage + MockPaymentScreen + OrderConfirmationPage
- Admiral reviews both outputs before stand-down

**Intervention points:**
- After HMS Resolute delivers: verify migration SQL before frontend integrates (single review moment)
- After HMS Swift delivers: spot-check mock screen timing and i18n coverage in all three locales

**Rollback plan:**
- Backend: `dotnet ef migrations remove` removes the column; `PaymentMethod` property and enum deleted; DTOs reverted. No data loss — default was `0`.
- Frontend: Three files reverted (`CheckoutPage.tsx`, `OrderConfirmationPage.tsx`, `api/orders.ts`); `MockPaymentScreen.tsx` deleted. Cart and routing unchanged.
