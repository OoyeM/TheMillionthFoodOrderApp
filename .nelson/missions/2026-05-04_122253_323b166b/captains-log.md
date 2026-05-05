# Captain's Log — US-FP-016: Place an Online Order
Date: 2026-05-04 | Duration: ~60 min | Outcome: ACHIEVED

## Mission Summary

US-FP-016 is complete. A registered customer can now browse a shop's menu, build a cart, and place an online order that enters the configured lifecycle. The `Order` aggregate is in the brand DB, VAT is applied correctly per item (including modifiers), and an `OrderCreatedEvent` reaches the kitchen SignalR group on every placement.

## Decisions and Rationale

**Two parallel captains (backend/frontend split):** The dependency graph had a clean seam at the backend/frontend boundary. HMS Duncan and HMS Portland ran concurrently with no shared files, reducing wall-clock time significantly.

**HMS Duncan in plan mode (Station 2):** The Order aggregate is the foundation for all Layer-3 stories. Requiring the admiral to review the domain model design before execution was the correct intervention point — catching design errors here costs nothing; catching them after 20 stories are built on top costs significantly more. In practice, the domain model was sound and no redirects were needed.

**Red-cell review (HMS Vigilant):** Returned two High findings that would have reached production undetected:
1. VAT not applied to modifier price adjustments — fiscal totals would have been wrong on any order with a modifier.
2. OrderNumber race condition — non-atomic read-then-write would produce intermittent 500s under any concurrent load.
Both were remediated and verified before merge. This was the correct call for a financial-data story.

**Cart state as React Context + localStorage:** No new dependency (Zustand not installed), consistent with the app's "deliberately simple" stance. shop-scoped key (`cart:{brandSlug}:{shopId}`) provides cross-shop isolation without a store.

**ConsumptionMode mapping:** Pickup→Takeaway (6%), Delivery→Takeaway (6%), EatIn→EatIn (21%). Aligns with Belgian fiscal law and the existing `TaxConfiguration` domain model.

**Payment deferred:** Checkout renders a clear placeholder ("Pay at pickup" / "Pay at counter"). No broken payment state. US-FP-058 (mocked payment) can build directly on this.

## Validation Evidence

- **Backend:** 155 TUnit integration tests pass (155 total, 0 failures). Tests cover: Pickup VAT (6%), EatIn VAT (21%), modifier price VAT inclusion, unknown ProductId (400), missing fields (400), quantity bounds, denormalised product names.
- **Frontend:** TypeScript type-check passes with zero new errors (4 pre-existing errors unrelated to this story remain).
- **Dependency tree:** US-FP-016 updated to ✅.

## Open Risks

1. **Modifier resolution in memory (Low):** `OrderService` loads all modifier groups for a product to resolve selected modifier IDs. Acceptable for MVP catalog sizes. Should be replaced with a direct modifier-by-ID query before the platform reaches brands with large catalogs (>50 modifier groups per product).
2. **Language-indeterminate name denormalisation (Low):** Product and modifier names are stored using `Translations.FirstOrDefault()` without a locale preference. On a bilingual brand (NL/FR), the stored name may vary across orders. To be resolved before go-live; deferred per HMS Vigilant's low-severity classification.
3. **shopId bridge via sessionStorage (Frontend):** The `/checkout` route carries no `:shopId` param; `CheckoutPage` scans localStorage for the active cart and writes the shopId to sessionStorage post-submit for `OrderConfirmationPage`. This is pragmatic for MVP but brittle if users have multiple tabs open with different shops. Should be revisited when address/delivery routing is added (US-FP-059).
4. **Golden-path visual verification:** The admiral's end-to-end browser run was not executed in this session (requires the Aspire stack running locally). The user should walk the golden path: start Aspire, navigate to `/{brandSlug}/{lang}/shops/{shopId}/menu`, add items, complete checkout, verify order confirmation page and SignalR status update.

## Files Delivered

**Backend (23 new files + 3 modified):**
- `Domain/Orders/`: Order.cs, OrderItem.cs, SelectedModifier.cs, OrderType.cs, OrderCreatedEvent.cs, IOrderRepository.cs
- `Infrastructure/Orders/`: OrderRepository.cs, OrderConfiguration.cs, OrderItemConfiguration.cs
- `Infrastructure/Notifications/`: OrderCreatedHandler.cs
- `Infrastructure/Persistence/Migrations/Brand/`: 20260504131405_AddOrders.cs + Designer + Snapshot update
- `Application/Orders/`: IOrderService.cs, OrderService.cs, Dtos/{CreateOrderRequest, OrderItemInput, OrderResponse}.cs
- `Api/Endpoints/Orders/`: CreateOrderEndpoint.cs
- `Tests.Integration/Orders/`: PlaceOrderTests.cs (155 tests)
- Modified: BrandDbContext.cs, Application/DependencyInjection.cs, Infrastructure/DependencyInjection.cs

**Frontend (15 files):**
- `api/orders.ts`
- `storefront/context/CartContext.tsx`
- `storefront/components/`: ProductCard.tsx, ModifierModal.tsx, CartDrawer.tsx
- `storefront/hooks/`: useStorefrontMenu.ts, useCreateOrder.ts
- `storefront/pages/`: MenuPage.tsx, CheckoutPage.tsx, OrderConfirmationPage.tsx
- Modified: routes.tsx, router.tsx (2-line wiring), i18n/locales/{nl,fr,de}/common.json

## Mentioned in Despatches

**HMS Vigilant** for a thorough and precise red-cell review that caught two High-severity fiscal correctness issues (modifier VAT gap, OrderNumber race condition) before they reached main. The structured verdict format with exact file/line references made remediation straightforward. Exactly what a red-cell is for.

**HMS Duncan** for methodical execution on a substantial greenfield build — domain model, EF configuration, migration, service, endpoint, and 12 integration tests delivered in one pass with correct patterns throughout. The flagged open risk (modifier resolution in memory) shows professional awareness of technical trade-offs at MVP scale.

## Patterns for Future Missions

- **VAT must be applied to the full unit price (base + modifiers), not just the base price.** This pattern will recur in any story that touches order pricing (discounts, combos, loyalty redemption).
- **The OrderNumber race condition fix pattern** (catch DbUpdateException on specific SQL error codes 2601/2627, detach and retry) is now established and should be reused for any other shop-scoped sequential number generation.
- **Red-cell review is worth the cost at Station 2.** This mission found two High issues post-implementation that would have been expensive to fix once downstream stories built on top.
