# US-FP-019: Select time slot at checkout

GitHub issue: [#19](https://github.com/OoyeM/TheMillionthFoodOrderApp/issues/19) · Branch: `feat/us-fp-019-time-slot-fable`
Prerequisites: US-FP-016 (place online order) ✅ · US-FP-020 (time-slot config) ✅

**As a** Registered Customer, **I want** to pick a time slot for my order when the shop has time-slot ordering enabled, **so that** I know when my food will be ready.

## Acceptance criteria

1. When time-slot ordering is enabled, checkout shows available slots based on the shop's configured interval and max orders per interval
2. Full slots are greyed out and not selectable
3. "As soon as possible" is always available as an option
4. Selected time slot is stored with the order and visible to kitchen staff
5. When time-slot ordering is disabled, customer sees estimated wait time (or place in line) instead

**Scope notes:**
- AC5 is implemented as **place-in-line** (queue position from active-order count). Configurable wait-time *estimates* are US-FP-021 (separate Should-Have story, still ⬜); its AC3 explicitly says "if no estimates are configured, show place in line only" — so place-in-line is the correct US-FP-019 baseline.
- AC4 "visible to kitchen staff" needs **zero kitchen-display work**: `KitchenOrderCard.tsx` (lines 143–156) and `printTicket.ts` (lines 59–63) already render `order.timeSlot` as a badge/ticket line when non-empty, with i18n keys (`pos.kitchen.timeSlot`, `pos.kitchen.ticket.timeSlot`) present in nl/fr/de. Only the backend needs to start populating the field (plus one new frontend test proving the badge renders — see §10).
- In-store (POS) orders **bypass time slots entirely**, mirroring their opening-hours bypass (staff are on-site; `TimeSlotOrderingSettings` doc comment says "online orders are placed into fixed-length slots"). `CreateInStoreOrderRequest` is not touched.

## Key design decisions

1. **Slot semantics — future pickup times, not placement windows.** Slots are wall-clock interval boundaries in the *shop's timezone* (10:00, 10:15… per the US-FP-020 AC example), generated from the first boundary strictly after "now" through the end of **today's** remaining opening-hours blocks (a slot is offered when `OpenTime <= slotStart < CloseTime`; multi-block days yield slots in every remaining block). Same-local-day only — same-visit ordering. Capacity is counted by the *stored slot*, never by `Order.CreatedAt` windows (placement time ≠ pickup time).
2. **Slots apply to all online order types** (Pickup, EatIn, Delivery). Capacity models *kitchen* throughput, and the VO doc comment scopes it to "online orders" generically. The picker label is therefore type-neutral ("Tijdslot" / "Créneau horaire" / "Zeitfenster") and the confirmation line says "ready at", which reads correctly for all three types.
3. **Closed shop ⇒ no slots, but `IsEnabled` always reflects configuration only.** When `!shop.IsOpenAt(now)` the availability endpoint returns `IsEnabled` per config with an **empty** slot list (online ordering is rejected at create time anyway — step 3b — so advertising slots a customer cannot book, e.g. during a midday gap between blocks, would be a dead end). The checkout additionally hides the picker when `!shopIsOpen`. The AC5 place-in-line notice keys off `isEnabled === false` only, so a slots-enabled-but-closed shop never shows the wrong message.
4. **Two columns on Orders.** `TimeSlotStart` (`datetimeoffset NULL`, canonical UTC, used for counting/validation) + `TimeSlot` (`nvarchar(16) NULL`, denormalized shop-local `"HH:mm"` label, computed once at creation). Both `NULL` = ASAP / no slot. The label format matches the de-facto contract already asserted in `printTicket.test.ts:70` (`timeSlot: '18:30'`), and denormalizing is required because the kitchen list maps orders without loading the shop (`OrderTrackingMapper.MapOrder(order, shop: null)` cannot do TZ formatting). Matches existing denormalization precedent (`ProductName`, `StatusName`).
5. **Capacity check is a best-effort count at create time.** A `COUNT(*)` on `(ShopId, TimeSlotStart)` in `CreateOrderCoreAsync` before persisting, rejecting with marker exception `TIME_SLOT_FULL` when `count >= MaxOrdersPerInterval`. Under truly concurrent submits the slot can overshoot by 1 — accepted for MVP (house precedent: "last-write-wins for MVP" sort ordering; order-number generation uses the same check-then-retry philosophy). Upgrade path documented in Risks.
6. **Which orders consume capacity: all of them.** No cancel flow exists (`Order` has only `AdvanceTo`), so count every order with a matching `(ShopId, TimeSlotStart)` regardless of status. Terminal-status filtering would wrongly free slots when kitchens advance orders early.
7. **Server computes everything; the client never does slot math.** The availability endpoint returns ready-to-render slots (UTC start + local label + availability flag). Checkout sends back the chosen `slotStart` verbatim (or null for ASAP). Server re-validates alignment, same-local-day, opening block, window, and capacity at create time — the slot list is a stale snapshot by definition (AC2 greying is UX, the create-time check is the gate).
8. **Single source of truth on the frontend: the live availability response.** `StorefrontShopResponse` / `ResolvedShop` are **not** extended with `timeSlotOrdering` — the live `GET /time-slots` response already carries `isEnabled` + `intervalMinutes`, and dual-sourcing the flag (60s-stale shop context vs live query) invites contradictory UI states. Cost: the picker appears one query-tick after page load (the picker needs the slot data anyway; ASAP submits work regardless). This also keeps the public shop DTO and all its fixtures untouched.
9. **Pure slot math lives in the Domain.** Static `TimeSlotCalculator` (Domain/Shops) handles both generation and validation from explicit inputs including `nowUtc` — fully unit-testable without a clock abstraction (no `TimeProvider` exists in this codebase; `UtcNow` stays at the Application call sites).
10. **Forgiving validity window at create.** A slot is accepted while its window has not fully elapsed (`slotStart + interval > now`), so a customer who picked 17:15 at 17:10 and submits at 17:16 is not rejected. Generation only offers boundaries strictly after now.

## Backend

### 1. Domain — `TimeSlotCalculator` (new) + `Order` extension

**New file:** `src/backend/TheMillionthFoodOrderApp.Domain/Shops/TimeSlotCalculator.cs`

```csharp
public static class TimeSlotCalculator
{
    /// <summary>Slot starts for the shop-local day containing nowUtc: boundaries aligned to
    /// multiples of the interval from the hour (shop-local), strictly after now, inside an
    /// opening block of that day.</summary>
    public static IReadOnlyList<TimeSlotCandidate> GenerateSlots(
        IReadOnlyCollection<OpeningHoursTimeBlock> openingHours,
        string timeZoneId,
        TimeSlotInterval interval,
        DateTimeOffset nowUtc);

    /// <summary>Create-time gate for a client-submitted slot: aligned to a shop-local interval
    /// boundary, inside an opening block, on the same shop-local day as nowUtc, and its window
    /// not yet fully elapsed (slotStartUtc + interval > nowUtc — design decision 10).</summary>
    public static bool IsValidSlotStart(
        IReadOnlyCollection<OpeningHoursTimeBlock> openingHours,
        string timeZoneId,
        TimeSlotInterval interval,
        DateTimeOffset slotStartUtc,
        DateTimeOffset nowUtc);
}
public readonly record struct TimeSlotCandidate(DateTimeOffset SlotStartUtc, string LocalLabel);
```

- TZ conversion mirrors `Shop.IsOpenAt` / `Shop.GetNextOpeningTime`: `TimeZoneInfo.ConvertTime` in, `DateTime.SpecifyKind(…, Unspecified)` + `ConvertTimeToUtc` out (DST-safe; convert each boundary individually, never cache an offset). Unknown TZ → empty list / false (mirrors `IsOpenAt`).
- Alignment is defined in **shop-local** minutes-since-midnight (`% (int)interval == 0`), anchored at the hour (15-min interval → :00/:15/:30/:45) — matters for non-whole-hour-offset zones.
- The **same-local-day** check in `IsValidSlotStart` is what stops aligned slots on future dates: opening blocks are weekday-keyed (`DayOfWeek` + `TimeOnly`, no dates), so block containment alone would accept "next Tuesday 17:15".
- `LocalLabel` = `localStart.ToString("HH\\:mm")`.

**Modify:** `src/backend/TheMillionthFoodOrderApp.Domain/Orders/Order.cs`
- Two new properties: `DateTimeOffset? TimeSlotStart`, `string? TimeSlot` (label).
- `Order.Create(...)`: two new optional tail params `DateTimeOffset? timeSlotStart = null, string? timeSlot = null` (after `languageCode`). **Call sites must use named arguments** — the optional tail already has 5 params.

**Modify:** `src/backend/TheMillionthFoodOrderApp.Domain/Orders/IOrderRepository.cs` (interface lives in **Domain**, not Infrastructure):

```csharp
Task<int> CountByTimeSlotAsync(Guid shopId, DateTimeOffset slotStartUtc, CancellationToken ct);
Task<IReadOnlyDictionary<DateTimeOffset, int>> GetTimeSlotCountsAsync(
    Guid shopId, DateTimeOffset fromUtc, DateTimeOffset toUtc, CancellationToken ct); // GROUP BY TimeSlotStart
Task<int> CountActiveByShopAsync(Guid shopId, CancellationToken ct); // same terminal-name filter as GetActiveByShopAsync
```

### 2. Application — slot availability service + create-order enforcement

**New file:** `src/backend/TheMillionthFoodOrderApp.Application/Orders/TimeSlotService.cs` (+ `ITimeSlotService`)

```csharp
public sealed record TimeSlotDto(DateTimeOffset SlotStart, string Label, bool IsAvailable);
public sealed record TimeSlotAvailabilityResponse(
    bool IsEnabled,            // reflects configuration ONLY, never open/closed state
    int? IntervalMinutes,
    IReadOnlyList<TimeSlotDto> Slots,
    /// <summary>Active (non-terminal) order count for place-in-line display; only when disabled (AC5).</summary>
    int? ActiveOrderCount);

Task<TimeSlotAvailabilityResponse> GetAvailabilityAsync(Guid shopId, CancellationToken ct);
```

- Disabled: `IsEnabled=false, Slots=[], ActiveOrderCount = await orderRepository.CountActiveByShopAsync(...)` (do **not** have the storefront call `ListActiveOrders` — it returns customer PII).
- Enabled but `!shop.IsOpenAt(UtcNow)`: `IsEnabled=true, Slots=[], ActiveOrderCount=null` (design decision 3).
- Enabled and open: `TimeSlotCalculator.GenerateSlots(shop.OpeningHours, shop.TimeZoneId, interval, DateTimeOffset.UtcNow)`, then one `GetTimeSlotCountsAsync` query spanning the day's slot range; `IsAvailable = count < MaxOrdersPerInterval`. (`ShopRepository.GetByIdAsync` already `.Include`s `OpeningHours`.) Unknown shop → `KeyNotFoundException` → 404.
- Register in DI next to `IOrderService` (`Application/DependencyInjection.cs:32`).

**Modify:** `src/backend/TheMillionthFoodOrderApp.Application/Orders/OrderDtos.cs`
- `CreateOrderRequest`: add optional tail param `DateTimeOffset? TimeSlotStart = null`.
- `OrderResponse`: add optional tail param `string? TimeSlot = null` (the local label — the shape the frontend's forward-compat field already expects).

**Modify:** `src/backend/TheMillionthFoodOrderApp.Application/Orders/OrderService.cs` — in `CreateOrderCoreAsync`, new step **3c** between the opening-hours check (3b) and lifecycle load (4). Thread `TimeSlotStart` from `CreateOrderAsync` only; `CreateInStoreOrderAsync` passes null (bypass):

```csharp
// 3c. Time-slot gating (US-FP-019) — online orders only; in-store bypasses like opening hours.
string? timeSlotLabel = null;
if (timeSlotStart is { } slotStart)
{
    var settings = shop.TimeSlotOrdering;
    if (!settings.IsEnabled)
        throw new InvalidOperationException("This shop does not use time-slot ordering.");

    if (!TimeSlotCalculator.IsValidSlotStart(
            shop.OpeningHours, shop.TimeZoneId, settings.Interval!.Value, slotStart, DateTimeOffset.UtcNow))
        throw new ArgumentException("The selected time slot is not valid for this shop.");

    // Capacity (best-effort; see design decision 5):
    var taken = await orderRepository.CountByTimeSlotAsync(shopId, slotStart, cancellationToken);
    if (taken >= settings.MaxOrdersPerInterval!.Value)
        throw new InvalidOperationException("TIME_SLOT_FULL");

    timeSlotLabel = /* shop-local "HH:mm" via shop.TimeZoneId (helper or inline TimeZoneInfo.ConvertTime) */;
}
```

- Pass `timeSlotStart: timeSlotStart, timeSlot: timeSlotLabel` (named args) to `Order.Create` in the retry loop.
- `MapToResponse`: include `TimeSlot: order.TimeSlot`.

### 3. Infrastructure — repository impl + EF config + migration

**Modify:** `src/backend/TheMillionthFoodOrderApp.Infrastructure/Orders/OrderRepository.cs` — implement the three new `IOrderRepository` methods (§1).

**Modify:** `src/backend/TheMillionthFoodOrderApp.Infrastructure/Orders/OrderConfiguration.cs`
- `TimeSlot` → `HasMaxLength(16).IsRequired(false)`; `TimeSlotStart` → `IsRequired(false)` (datetimeoffset(7) via convention).
- New index: `HasIndex(o => new { o.ShopId, o.TimeSlotStart }).HasDatabaseName("IX_Orders_ShopId_TimeSlotStart")`.

**Migration:** `dotnet ef migrations add AddOrderTimeSlot --project TheMillionthFoodOrderApp.Infrastructure --startup-project TheMillionthFoodOrderApp.Api --context BrandDbContext --output-dir Persistence/Migrations/Brand`
⚠️ Stop the Aspire AppHost first (running Api/Bff lock `bin/` DLLs).

### 4. API — new endpoint + create-order plumbing

**New file:** `src/backend/TheMillionthFoodOrderApp.Api/Endpoints/Orders/GetTimeSlotsEndpoint.cs`
- `GET /api/brands/{brandSlug}/shops/{shopId}/time-slots` — modeled on `GetShopStatusEndpoint`: `AllowAnonymous()`, `PreProcessor<BrandScopedPreProcessor<…>>()`, returns `TimeSlotAvailabilityResponse`, 404 on unknown shop. (Literal segment coexists safely with `{orderId}` routes — precedent `/orders/active`.)

**Modify:** `src/backend/TheMillionthFoodOrderApp.Api/Endpoints/Orders/CreateOrderEndpoint.cs`
- `CreateOrderApiRequest`: add `DateTimeOffset? TimeSlotStart = null` tail param; pass through to `CreateOrderRequest`.
- Catch block: special-case the marker **before** the generic `InvalidOperationException` handler (exception filters make this valid alongside the existing catches):

```csharp
catch (InvalidOperationException ex) when (ex.Message == "TIME_SLOT_FULL")
{
    var failures = new List<ValidationFailure>
        { new(nameof(req.TimeSlotStart), "The selected time slot is full. Please pick another slot.") };
    await HttpContext.Response.SendErrorsAsync(failures, statusCode: 400, cancellation: ct);
}
```

(FastEndpoints applies the camelCase naming policy to error keys, so the body carries `errors.timeSlotStart` — verified against FastEndpoints 8.0.1 + this API's default JSON options. That key is what the frontend uses to show the slot-specific message and refetch.)

**Modify:** `src/backend/TheMillionthFoodOrderApp.Api/Endpoints/Orders/OrderTrackingMapper.cs` — map `TimeSlot: order.TimeSlot` so tracking + kitchen `listActive` responses carry it.

### 5. Backend tests

**Unit — new:** `Tests.Unit/Shops/TimeSlotCalculatorTests.cs` (deterministic `nowUtc` parameter; Brussels TZ):
- `GenerateSlots`: alignment to interval boundaries anchored at the hour, for 5/10/15-minute intervals
- First slot is strictly after now (now exactly on a boundary → next boundary)
- Slots clamp to block close (`slotStart < CloseTime`); none before block open
- Multiple blocks on the same day each produce slots; the gap between blocks produces none
- No opening hours / closed day / unknown timezone → empty list
- Labels are shop-local `"HH:mm"` (pick a UTC `now` where Brussels local differs from UTC)
- `IsValidSlotStart`: accepts an offered slot; accepts a started-but-not-elapsed slot (picked 17:15, now 17:16 — decision 10); rejects misaligned; rejects outside any opening block; rejects fully elapsed; **rejects an aligned slot on a future day with the same weekday**; rejects on unknown TZ
- A DST-transition date for both methods

**Unit — extend:** `Tests.Unit/Orders/OrderTests.cs` (if present; else create): `Order.Create` with slot params persists both; defaults are null.

**Integration — new:** `Tests.Integration/Orders/TimeSlotTests.cs` (`[ClassDataSource<IntegrationTestBase>(Shared = SharedType.PerClass)]`; always-open shops via existing helper; unique shop slugs per test):
- GET time-slots: disabled shop → `isEnabled=false`, empty slots, `activeOrderCount` present and correct after placing an order
- GET time-slots: enabled shop → `isEnabled=true`, every slot aligned to interval / in the future / `isAvailable=true` (structural assertions, not exact lists — keeps tests time-independent)
- GET time-slots: enabled shop with **no opening hours** (closed now) → `isEnabled=true`, empty slots (decision 3)
- Capacity: enable slots with `maxOrdersPerInterval=2`, place 2 orders into the first returned slot → GET shows that slot `isAvailable=false`; 3rd create into it → 400 with `errors.timeSlotStart`
- Create with valid slot → 201, `timeSlot` label `"HH:mm"` in response; GET order returns it; `listActive` returns it (kitchen path, AC4)
- Create with `timeSlotStart` when shop has slots disabled → 400
- Create with misaligned `timeSlotStart` → 400
- Create with an aligned `timeSlotStart` **7 days in the future** → 400 (same-local-day gate)
- ASAP: create with null `timeSlotStart` on an enabled shop always succeeds, response `timeSlot` null (AC3)
- Guard against the near-midnight empty-slot edge: tests that need a slot take `slots.First()` and early-return (with a logged skip) if the list is empty — only possible in the final interval before midnight on an always-open shop

⚠️ Run with `dotnet run -c Release` inside the test project (TUnit), one class at a time via `--treenode-filter` (parallel SQL containers OOM).

## Frontend

### 6. API client + types

**New file:** `src/frontend/src/api/timeSlots.ts` (module-per-resource precedent: `openingHours.ts`):

```typescript
export interface TimeSlotDto { slotStart: string; label: string; isAvailable: boolean; }
export interface TimeSlotAvailabilityResponse {
  isEnabled: boolean;
  intervalMinutes: number | null;
  slots: TimeSlotDto[];
  activeOrderCount: number | null;
}
export const timeSlotsApi = {
  get: (brandSlug: string, shopId: string): Promise<TimeSlotAvailabilityResponse> =>
    apiClient.get(`/brands/${brandSlug}/shops/${shopId}/time-slots`).then((r) => r.data),
};
```

**Modify:** `src/frontend/src/api/orders.ts`
- `CreateOrderRequest`: add `timeSlotStart?: string | null` (ISO from the slots endpoint, null/omitted = ASAP).
- `OrderResponse.timeSlot`: update the comment — no longer forward-compat.

*(No changes to `shops.ts`, `ShopContext.tsx`, or `shopContextValue.ts` — design decision 8.)*

### 7. Hook — slot availability with freshness

**New file:** `src/frontend/src/features/storefront/hooks/useTimeSlots.ts`
- `useQuery({ queryKey: ['timeSlots', brandSlug, shopId], queryFn: …, refetchInterval: 60_000, staleTime: 30_000 })` (60s polling precedent: `ShopStatusBadge`). Called **inside `CheckoutForm`** (which already receives `brandSlug` + `shopId` and owns the schema memo, the mutation, and the picker — no prop-threading). The response drives both the picker (enabled) and the place-in-line notice (disabled). No SignalR (status payload carries no slot data).

### 8. Checkout page — picker, validation, AC5 notice, error handling

**Modify:** `src/frontend/src/features/storefront/pages/CheckoutPage.tsx` (all inside `CheckoutForm`)

- Form value: `timeSlotStart: string` — `'asap'` sentinel default, otherwise a `slotStart` ISO string. Add `setValue` to the `useForm` destructuring (needed for auto-reset).
- **Gating (single source — decision 8):** `const { data: slotData, refetch } = useTimeSlots(brandSlug, shopId)`. Picker renders when `shopIsOpen && slotData?.isEnabled`; AC5 notice renders when `slotData && !slotData.isEnabled`; while loading **or on query error**, render neither (ordering still works as ASAP; explicit decision — see Risks).
- **Picker (AC1/AC2/AC3)** — new `<fieldset>` after the VAT notice (line ~341), styled like the order-type radio group (inline styles, `--brand-color-primary` selection border), type-neutral legend (decision 2):
  - First option: "As soon as possible" (always enabled, default checked).
  - Then one radio per slot showing `label`; full slots: `disabled` input, `opacity: 0.45`, `cursor: not-allowed`, suffix from `storefront.checkout.slotFullSuffix` — greyed out and not selectable.
  - Many-slot days (5-min interval ⇒ ~100+ slots): wrap slot options in a `display:grid; gridTemplateColumns: repeat(auto-fill, minmax(5.5rem, 1fr)); maxHeight: 14rem; overflowY: auto` container; ASAP stays outside the scroll area.
  - If the selected slot disappears or becomes full on a refetch, auto-reset selection to `'asap'` (`useEffect` over `slotData` + watched value, via `setValue`).
- **Schema:** extend to `makeCheckoutSchema(isAuthenticated, t, eatIn, slotData)` with a refine: when `slotData?.isEnabled`, `timeSlotStart` must be `'asap'` or match an *available* fetched slot (backstop for the auto-reset). Memo deps gain `slotData`.
- **Submit:** `timeSlotStart: values.timeSlotStart === 'asap' ? null : values.timeSlotStart` in the `createOrder.mutateAsync` payload.
- **Error handling:** on mutation error, if the axios error body has `errors.timeSlotStart` → show `storefront.checkout.timeSlotFull` message and `refetch()` so the now-full slot greys out (cart is already preserved on failure — `clearCart()` only runs on success). Otherwise keep the generic `submitError` banner.
- **AC5 notice:** info banner (blue, same style as the VAT notice): `activeOrderCount === 0` → `storefront.checkout.queueEmpty` ("Your order goes straight to the kitchen"), else `storefront.checkout.queueNotice` with `{{count}}` ("There are {{count}} orders ahead of you").

**Modify:** `src/frontend/src/features/storefront/pages/OrderConfirmationPage.tsx` — when `order.timeSlot` is non-empty, show a "Ready at {{time}}" row (`storefront.confirmation.timeSlot`) in the existing info grid. One conditional row; the field already flows through `ordersApi`.

### 9. i18n (nl / fr / de `common.json`)

New keys (kitchen/ticket/receipt keys already exist):

```
storefront.checkout.timeSlotLegend      "Tijdslot" / "Créneau horaire" / "Zeitfenster"   (type-neutral — decision 2)
storefront.checkout.asap                "Zo snel mogelijk" / "Dès que possible" / "So schnell wie möglich"
storefront.checkout.slotFullSuffix      "(volzet)" / "(complet)" / "(voll)"
storefront.checkout.timeSlotInvalid     validation message (schema refine)
storefront.checkout.timeSlotFull        submit-error message ("Dit tijdslot is net volgeboekt…")
storefront.checkout.queueNotice         "Er zijn {{count}} bestellingen voor jou." (+ _one variant)
storefront.checkout.queueEmpty          "Je bestelling gaat direct naar de keuken."
storefront.confirmation.timeSlot        "Klaar om {{time}}"
```

### 10. Frontend tests

- **Modify** `src/frontend/src/test/msw/handlers.ts`: new default handler for `GET /api/brands/:brandSlug/shops/:shopId/time-slots` returning `{ isEnabled: false, intervalMinutes: null, slots: [], activeOrderCount: 0 }`. **Load-bearing:** MSW runs with `onUnhandledRequest: 'error'` (`test/setup.ts:10`), so once `useTimeSlots` is wired in, *every existing* CheckoutPage test fires this request — the handler must land in the same change and its default must keep those tests passing unchanged. *(No `StorefrontShop`/`ResolvedShop` fixture changes anywhere — decision 8 keeps both types untouched.)*
- **Extend** `src/frontend/src/features/storefront/pages/__tests__/CheckoutPage.test.tsx` (override the time-slots handler per test via `server.use(...)`):
  - Slots enabled → picker renders ASAP + slot labels; ASAP pre-selected
  - Full slot rendered disabled (AC2); clicking it does not change selection
  - Picker also renders for a non-Pickup order type (EatIn selected) — decision 2
  - Submitting with a slot selected sends `timeSlotStart` ISO; ASAP sends null
  - Slots disabled → no picker; queue notice shows `activeOrderCount` (and `queueEmpty` at 0)
  - Slots query errors → no picker, no notice, checkout still submits as ASAP
  - `400 errors.timeSlotStart` response → slot-full message shown, cart intact
- **Extend** `src/frontend/src/features/pos/pages/__tests__/KitchenDisplay.test.tsx`: an order with `timeSlot: '18:30'` in the listActive fixture renders the time-slot badge (proves AC4's UI half — currently untested).
- **New** `src/frontend/src/api/__tests__/timeSlots.test.ts`: response shape smoke test against MSW.
- OrderConfirmationPage has no test file today; the new row is covered by the live happy path only (explicit decision, not an oversight).

## Verification

```bash
# Backend (stop Aspire AppHost first)
cd src/backend && dotnet build TheMillionthFoodOrderApp.slnx
cd TheMillionthFoodOrderApp.Tests.Unit && dotnet run -c Release
cd ../TheMillionthFoodOrderApp.Tests.Integration && dotnet run -c Release --treenode-filter "/*/*/TimeSlotTests/*"
# (also run PlaceOrderTests — same create path touched)

# Frontend
cd src/frontend && pnpm build && pnpm test && pnpm lint
```

Live happy-path (per project memory: AC-passing ≠ user-reachable):
1. Start AppHost + `pnpm dev`; sign in `/bff/login?mock=brand-admin@frietjes`; in admin ShopEdit enable time slots (15 min, max 2) and set the shop always-open.
2. Storefront checkout → picker shows aligned future slots + ASAP; place 2 orders into one slot → slot greys out (after refetch) and a 3rd attempt shows the slot-full error.
3. Order confirmation shows "Ready at HH:mm"; kitchen display (counter-staff persona, switch role in place — it resets on navigation) → order cards show the time-slot badge.
4. Disable time slots in admin → checkout shows the place-in-line notice instead.

## Out of scope (deferred)

- **US-FP-021** — configurable wait-time estimates by order size; the disabled-state banner upgrades from place-in-line to estimates there.
- **Serializable/constraint-based capacity enforcement** — see Risks; upgrade only if real-world overshoot is observed.
- **Kitchen display sorting/grouping by slot** — kitchen currently sorts by `createdAt`; revisit with kitchen feedback (relates to US-FP-024 group-by-table work).
- **Email receipt time-slot line** (`ReceiptComposer`) — receipts render fine without it; trivial follow-up if requested.
- **Multi-day pre-ordering / ordering while closed** — slots are same-local-day and only offered while the shop is open (create-time 3b rejects closed-shop online orders regardless of slot; relaxing that is a product decision for a future story).
- **Storefront exposure of `timeSlotOrdering` on shop DTOs** — deliberately cut (decision 8); revisit if the shop chooser ever needs to badge slot-enabled shops.

## Risks & mitigations

| Risk | Severity | Mitigation |
|---|---|---|
| Best-effort capacity check can overshoot max by 1 under concurrent submits | Low | Accepted for MVP (fries-shop traffic; window is ms). Upgrade paths: serializable COUNT+INSERT wrapped in `CreateExecutionStrategy().ExecuteAsync` (precedent: `ShopRepository.ReplaceOpeningHoursAsync`) with SQL 1205 retry, or unique `(ShopId, TimeSlotStart, SlotPosition)` index mirroring the ORDER_NUMBER_CONFLICT pattern |
| Slot list staleness (AC2 greying is a snapshot) | Med | 60s `refetchInterval`, refetch on slot-full 400, auto-reset stale selection to ASAP, server is authoritative |
| Aligned slot on a future date passes weekday-keyed opening-hours containment | Med | Same-local-day check in `IsValidSlotStart` (§1) + dedicated unit and integration tests (7-days-ahead → 400) |
| DST transition days: local boundary → UTC conversion invalid/ambiguous | Low | Follow `GetNextOpeningTime` approach (`DateTimeKind.Unspecified` + per-boundary `ConvertTimeToUtc`); unit-test a DST date |
| Checkout minutes before close → empty slot list while shop still open | Med | ASAP is always present and default — picker degrades to "ASAP only"; no dead-end |
| Slots-query error leaves AC5 notice unshown for disabled shops | Low | Explicit MVP decision: omit the notice on error (ordering still works); covered by a dedicated test so the degradation is intentional |
| Wiring `useTimeSlots` into checkout breaks existing tests via MSW `onUnhandledRequest:'error'` | Med | Default time-slots MSW handler with disabled-state response ships in the same change (§10) |
| Integration-test flakiness near interval/midnight boundaries | Low | Structural assertions + first-returned-slot pattern + empty-list early-return guard |
| `Order.Create` optional-tail growth (7 params) | Low | Named arguments at all call sites (spec'd in §1/§2) |

## File summary

**Backend (new):** `Domain/Shops/TimeSlotCalculator.cs` · `Application/Orders/TimeSlotService.cs` (+ interface) · `Api/Endpoints/Orders/GetTimeSlotsEndpoint.cs` · migration `AddOrderTimeSlot` · `Tests.Unit/Shops/TimeSlotCalculatorTests.cs` · `Tests.Integration/Orders/TimeSlotTests.cs`
**Backend (modified):** `Domain/Orders/Order.cs` · `Domain/Orders/IOrderRepository.cs` · `Application/Orders/{OrderDtos,OrderService}.cs` · `Application/DependencyInjection.cs` · `Infrastructure/Orders/{OrderRepository,OrderConfiguration}.cs` · `Api/Endpoints/Orders/{CreateOrderEndpoint,OrderTrackingMapper}.cs` · `Tests.Unit/Orders/OrderTests.cs` (if present)
**Frontend (new):** `api/timeSlots.ts` · `features/storefront/hooks/useTimeSlots.ts` · `api/__tests__/timeSlots.test.ts`
**Frontend (modified):** `api/orders.ts` · `features/storefront/pages/{CheckoutPage,OrderConfirmationPage}.tsx` · `i18n/locales/{nl,fr,de}/common.json` · `test/msw/handlers.ts` · `__tests__/{CheckoutPage,KitchenDisplay}.test.tsx`
**Docs:** `docs/dependency-tree.md` (US-FP-019 ⬜→✅ + changelog entry)
