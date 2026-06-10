# US-FP-019 — Select time slot at checkout

GitHub issue: [#19](https://github.com/OoyeM/TheMillionthFoodOrderApp/issues/19)
Branch: `feat/us-fp-019-time-slot-checkout`
Prereqs: US-FP-016 ✅ (checkout), US-FP-020 ✅ (time-slot settings on Shop)

## Acceptance criteria

1. When time-slot ordering is enabled, checkout shows available slots based on the shop's configured interval and max orders per interval.
2. Full slots are greyed out and not selectable.
3. "As soon as possible" is always available as an option.
4. Selected time slot is stored with the order and visible to kitchen staff.
5. When time-slot ordering is disabled, customer sees estimated wait time (or place in line) instead.

## Verified state of the codebase

- `Shop.TimeSlotOrdering` (owned `TimeSlotOrderingSettings` VO: `IsEnabled`, `TimeSlotInterval?` enum 5/10/15, `MaxOrdersPerInterval?`) exists with columns `TimeSlotOrdering_IsEnabled/_Interval/_MaxOrdersPerInterval` — `src/backend/TheMillionthFoodOrderApp.Domain/Shops/TimeSlotOrderingSettings.cs`, `src/backend/TheMillionthFoodOrderApp.Infrastructure/Shops/ShopConfiguration.cs`.
- **`Order` has NO time-slot field** — US-FP-028's "time slot" was speculative. The frontend `OrderResponse` type already declares `timeSlot?: string | null` (`src/frontend/src/api/orders.ts:83`), and `KitchenOrderCard.tsx` (line 143) + `printTicket.ts` (line 59) already render it when present, but the backend never sends it. Domain + migration + DTO work is required.
- Order creation funnels through the private `CreateOrderCoreAsync` in `src/backend/TheMillionthFoodOrderApp.Application/Orders/OrderService.cs` (~line 210), with per-path flags (`enforceOpeningHours: true` only for the online path) — the exact pattern to follow for slot enforcement.
- `Shop` has `TimeZoneId` ("Europe/Brussels"), `OpeningHours` (local-time `OpeningHoursTimeBlock`s), `IsOpenAt()`, `GetNextOpeningTime()` with established UTC-conversion patterns. `ShopRepository.GetByIdAsync` already `Include`s `OpeningHours`.
- Storefront checkout resolves the shop via `ShopResolver` → `ResolvedShop` context (`id`, `name`, `slug`, `isOpen`, `eatIn`) backed by `GET /api/brands/{brandSlug}/shops/active` (`StorefrontShopResponse`) — **which currently does NOT include `TimeSlotOrdering`**; it must be extended.
- US-FP-021 (estimated wait times) is not started — AC5 needs an MVP fallback.

## Design decisions

| Decision | Choice | Rationale |
|---|---|---|
| Where slots are computed | **New server endpoint** `GET /api/brands/{brandSlug}/shops/{shopId}/time-slots` | Capacity counting needs DB order counts; client can't know them. Server is also the enforcement point, so one shared generator avoids drift. |
| Slot anchor & grid | Slots anchored at each opening block's `OpenTime`, stepping by `interval`; a slot `[start, start+interval)` is offered only if it fits fully inside the block | Cleaner than clock-hour anchoring with odd opening times (e.g. 11:30 open + 15 min → 11:30, 11:45 …). |
| Horizon | **Remainder of today only** (shop-local day) — no artificial slot cap; the day itself bounds generation (worst case 24h open ÷ 5 min = 288 slots) | Same-day food pickup; no pre-order story exists. An arbitrary cap (e.g. 48) would silently truncate the day for long-open shops. Stated as scope decision. |
| Lead time | First offered slot starts ≥ `now + interval` (rounded up to grid) | Kitchen needs at least one interval of prep runway. |
| ASAP representation | `TimeSlotStart == null` on the Order (and absent from the request) | AC3: ASAP is always valid, including when slots are enabled. No sentinel values. |
| Persisted shape | `TimeSlotStart` + `TimeSlotEnd` (`DateTimeOffset?`, UTC) on `Order` | Capacity counting groups by exact `TimeSlotStart`; storing `End` denormalises the interval so later admin config changes don't corrupt history (same denormalisation philosophy as `ProductName` on order items). |
| Capacity counting | Count **all** orders with the same `TimeSlotStart`, regardless of status | No cancellation concept exists yet; slots are in the future so completed-before-slot is not a real case. Stated as scope decision. |
| Concurrency on capacity | Accept the small check-then-insert race for MVP (two simultaneous orders could overbook a slot by 1) | Fixing it needs a serializable transaction or slot-counter table; low traffic at MVP. Documented below. |
| Validation on create | Online path only: recompute valid slots via the shared generator and require the requested `TimeSlotStart` to be a member, then check capacity | One check enforces alignment + opening hours + future-ness + enabled-flag. In-store/POS path never passes a slot. |
| Full-slot error | `InvalidOperationException("TIME_SLOT_FULL")` → endpoint maps to **409**; frontend refreshes slot list on 409 | Distinguishable from generic 400s so the picker can self-heal. |
| Timezone | Generate in shop-local time from `OpeningHours`, convert boundaries to UTC via `TimeZoneInfo.ConvertTimeToUtc` (same pattern as `Shop.GetNextOpeningTime`); API returns ISO-8601 with offset; frontend formats with `Intl` (devices/customers are in the shop's timezone — consistent with how `createdAt` is already rendered) | Matches existing conventions; `DateTimeOffset` everywhere per backend CLAUDE.md. |
| AC5 fallback (US-FP-021 not done) | When `isEnabled == false`, checkout shows a static notice: "Your order will be prepared as soon as possible." No backend work. When US-FP-021 lands, that notice is replaced with the configured wait time. | Explicit scope decision — no wait-time config exists to read. |
| POS | **Stays ASAP-only.** No slot picker in POS; `CreateInStoreOrderRequest` unchanged | AC only covers customer checkout; staff at the counter take orders for "now". |
| Order type scope | Slot picker shown for all online order types (Pickup/EatIn/Delivery) | US-FP-020's stated intent is matching kitchen capacity for all online orders. |

## Backend

### 1. Domain — Order gets a time slot

**File:** `src/backend/TheMillionthFoodOrderApp.Domain/Orders/Order.cs`
- Add `public DateTimeOffset? TimeSlotStart { get; private set; }` and `public DateTimeOffset? TimeSlotEnd { get; private set; }` (null = ASAP).
- Extend `Order.Create(...)` with optional params `DateTimeOffset? timeSlotStart = null, DateTimeOffset? timeSlotEnd = null` (appended after `languageCode`). Invariants guarded in the factory: both-or-neither set; `timeSlotEnd > timeSlotStart`; throw `ArgumentException` otherwise.

**File (new):** `src/backend/TheMillionthFoodOrderApp.Domain/Shops/TimeSlotGenerator.cs`
- Pure, static domain service:
```csharp
public static IReadOnlyList<(DateTimeOffset Start, DateTimeOffset End)> GenerateSlotsForToday(
    Shop shop, DateTimeOffset now)
```
- No artificial slot cap — the today-only horizon bounds output naturally (≤ 288 slots for a 24h shop at 5-min intervals).
- Returns `[]` when `!shop.TimeSlotOrdering.IsEnabled`, no opening hours, or unknown `TimeZoneId` (mirror `IsOpenAt`'s defensive try/catch).
- Algorithm: convert `now` to shop-local; for each of today's blocks (ordered by `OpenTime`): anchor at `OpenTime`, step `interval` minutes; include slot if `slotEnd <= CloseTime` and `slotStart >= localNow + interval`; convert boundaries to UTC `DateTimeOffset`.

### 2. EF configuration + migration

**File:** `src/backend/TheMillionthFoodOrderApp.Infrastructure/Orders/OrderConfiguration.cs`
- `builder.Property(o => o.TimeSlotStart).IsRequired(false);` and same for `TimeSlotEnd` (DateTimeOffsetConvention gives `datetimeoffset(7)`).
- Filtered index for capacity counting:
```csharp
builder.HasIndex(o => new { o.ShopId, o.TimeSlotStart })
    .HasDatabaseName("IX_Orders_ShopId_TimeSlotStart")
    .HasFilter("[TimeSlotStart] IS NOT NULL");
```

**Migration (Brand context):**
```
dotnet ef migrations add AddOrderTimeSlot --project TheMillionthFoodOrderApp.Infrastructure --startup-project TheMillionthFoodOrderApp.Api --context BrandDbContext --output-dir Persistence/Migrations/Brand
```

### 3. Repository — slot occupancy counts

**File:** `src/backend/TheMillionthFoodOrderApp.Domain/Orders/IOrderRepository.cs`
```csharp
Task<IReadOnlyDictionary<DateTimeOffset, int>> GetTimeSlotOrderCountsAsync(
    Guid shopId, DateTimeOffset fromInclusive, DateTimeOffset toExclusive,
    CancellationToken cancellationToken = default);
```

**File:** `src/backend/TheMillionthFoodOrderApp.Infrastructure/Orders/OrderRepository.cs`
- `GroupBy(o => o.TimeSlotStart!.Value)` over `Orders.Where(o => o.ShopId == shopId && o.TimeSlotStart >= from && o.TimeSlotStart < to)` → `ToDictionaryAsync`.

### 4. Application — availability service + DTOs

**Files (new):** `src/backend/TheMillionthFoodOrderApp.Application/Orders/ITimeSlotAvailabilityService.cs`, `TimeSlotAvailabilityService.cs`
- `Task<AvailableTimeSlotsResponse> GetAvailableSlotsAsync(Guid shopId, CancellationToken ct)`:
  1. Load shop (404 if missing). If disabled → `new AvailableTimeSlotsResponse(false, null, null, [])`.
  2. `TimeSlotGenerator.GenerateSlotsForToday(shop, DateTimeOffset.UtcNow)`.
  3. One `GetTimeSlotOrderCountsAsync` call spanning first slot start → last slot end.
  4. Map each slot to `remainingCapacity = max - count`, `isAvailable = remaining > 0`.
- Register in `src/backend/TheMillionthFoodOrderApp.Application/DependencyInjection.cs`: `services.AddScoped<ITimeSlotAvailabilityService, TimeSlotAvailabilityService>();`

**File:** `src/backend/TheMillionthFoodOrderApp.Application/Orders/OrderDtos.cs` — add:
```csharp
public sealed record TimeSlotDto(
    DateTimeOffset Start, DateTimeOffset End, bool IsAvailable, int RemainingCapacity);

public sealed record AvailableTimeSlotsResponse(
    bool IsEnabled, int? IntervalMinutes, int? MaxOrdersPerInterval,
    IReadOnlyList<TimeSlotDto> Slots);
```
- `CreateOrderRequest`: append `DateTimeOffset? TimeSlotStart = null`.
- `OrderResponse`: append `DateTimeOffset? TimeSlotStart = null, DateTimeOffset? TimeSlotEnd = null` (optional positional params at the end — existing call sites unaffected).
- `CreateInStoreOrderRequest`: **unchanged** (POS = ASAP-only).

### 5. New endpoint — available slots

**File (new):** `src/backend/TheMillionthFoodOrderApp.Api/Endpoints/Orders/GetAvailableTimeSlotsEndpoint.cs`
- `GET /api/brands/{brandSlug}/shops/{shopId}/time-slots`
- Request record `(string BrandSlug, Guid ShopId)` route params; `AllowAnonymous()`; `PreProcessor<BrandScopedPreProcessor<…>>` (mirror `ListActiveShopsEndpoint`).
- Response shape (200):
```json
{
  "isEnabled": true,
  "intervalMinutes": 10,
  "maxOrdersPerInterval": 4,
  "slots": [
    { "start": "2026-06-10T15:10:00+00:00", "end": "2026-06-10T15:20:00+00:00",
      "isAvailable": true, "remainingCapacity": 3 }
  ]
}
```
- 404 when shop unknown. Swagger summary documenting the disabled shape `{ "isEnabled": false, "slots": [] }`.

### 6. OrderService — server-side enforcement (follows the eat-in gating pattern)

**File:** `src/backend/TheMillionthFoodOrderApp.Application/Orders/OrderService.cs`
- `CreateOrderCoreAsync`: new param `DateTimeOffset? timeSlotStart = null`. After the opening-hours check (step 3b), add step 3c:
  - If `timeSlotStart is null` → nothing (ASAP, always allowed — AC3).
  - Else:
    - `!shop.TimeSlotOrdering.IsEnabled` → `ArgumentException("This shop does not accept time-slot orders.")` (400).
    - Recompute `TimeSlotGenerator.GenerateSlotsForToday(shop, DateTimeOffset.UtcNow)`; requested start must match a generated slot's `Start` (compare UTC instants) → else `ArgumentException("The requested time slot is not available...")`.
    - Capacity: `GetTimeSlotOrderCountsAsync(shopId, slot.Start, slot.End)`; if `count >= MaxOrdersPerInterval` → `throw new InvalidOperationException("TIME_SLOT_FULL")`.
    - Pass `slot.Start`/`slot.End` to `Order.Create`.
- `CreateOrderAsync` forwards `request.TimeSlotStart`; `CreateInStoreOrderAsync` passes `null`.
- `MapToResponse`: append `order.TimeSlotStart, order.TimeSlotEnd`.

**File:** `src/backend/TheMillionthFoodOrderApp.Api/Endpoints/Orders/OrderTrackingMapper.cs` — same two fields appended (feeds tracking, by-number, list-active → kitchen display).

### 7. CreateOrderEndpoint — request field + 409 mapping

**File:** `src/backend/TheMillionthFoodOrderApp.Api/Endpoints/Orders/CreateOrderEndpoint.cs`
- `CreateOrderApiRequest`: append `DateTimeOffset? TimeSlotStart = null`; forward to the app request.
- Validator: light shape check only — `TimeSlotStart` must be in the future when supplied (`.GreaterThan(DateTimeOffset.UtcNow)` evaluated lazily via `.Must(...)`); authoritative validation stays in `OrderService` (comment referencing US-FP-019, same style as the table-number comment).
- Add a specific catch **before** the generic `InvalidOperationException` catch:
```csharp
catch (InvalidOperationException ex) when (ex.Message == "TIME_SLOT_FULL")
{
    var failures = new List<ValidationFailure>
        { new("timeSlotStart", "The selected time slot is full. Please pick another slot.") };
    await HttpContext.Response.SendErrorsAsync(failures, statusCode: 409, cancellation: ct);
}
```
- Swagger: document 409.

### 8. Storefront shop DTO carries the settings

**File:** `src/backend/TheMillionthFoodOrderApp.Application/Shops/ShopDtos.cs`
- `StorefrontShopResponse`: append `TimeSlotOrderingSettingsDto TimeSlotOrdering`.

**File:** `src/backend/TheMillionthFoodOrderApp.Application/Shops/ShopService.cs`
- `MapToStorefrontResponse`: map `shop.TimeSlotOrdering` exactly like `MapToResponse` does.

## Frontend

### 9. Types + API clients

**File:** `src/frontend/src/api/shops.ts`
- `StorefrontShop`: add `timeSlotOrdering: TimeSlotOrderingSettings;` (type already exists in `src/frontend/src/types/common.ts:76`).

**File:** `src/frontend/src/api/orders.ts`
- `CreateOrderRequest`: add `timeSlotStart?: string | null;`
- `OrderResponse`: **replace** the speculative `timeSlot?: string | null` with `timeSlotStart?: string | null; timeSlotEnd?: string | null;`
- New types + function:
```ts
export interface TimeSlotResponse {
  start: string; end: string; isAvailable: boolean; remainingCapacity: number;
}
export interface AvailableTimeSlotsResponse {
  isEnabled: boolean; intervalMinutes: number | null;
  maxOrdersPerInterval: number | null; slots: TimeSlotResponse[];
}
// in ordersApi:
getTimeSlots: (brandSlug: string, shopId: string): Promise<AvailableTimeSlotsResponse> =>
  apiClient.get<AvailableTimeSlotsResponse>(`/brands/${brandSlug}/shops/${shopId}/time-slots`)
    .then((r) => r.data),
```

**File (new):** `src/frontend/src/utils/timeSlot.ts`
- `formatTimeSlot(startIso: string, endIso: string): string` → `"17:10–17:20"` using `Intl.DateTimeFormat('nl-BE', { hour: '2-digit', minute: '2-digit' })` (same convention as `KitchenOrderCard.formatTime`).

### 10. ShopResolver context

**Files:** `src/frontend/src/features/storefront/context/shopContextValue.ts`, `ShopContext.tsx`
- `ResolvedShop`: add `timeSlotOrdering: TimeSlotOrderingSettings;`; `ShopResolver` copies `shop.timeSlotOrdering` into the context.

### 11. Hook + picker component

**File (new):** `src/frontend/src/features/storefront/hooks/useAvailableTimeSlots.ts`
```ts
useQuery({
  queryKey: ['timeSlots', brandSlug, shopId],
  queryFn: () => ordersApi.getTimeSlots(brandSlug, shopId),
  enabled,                 // only when shop.timeSlotOrdering.isEnabled
  staleTime: 15_000,
  refetchInterval: 30_000, // slots fill up in real time
});
```

**File (new):** `src/frontend/src/features/storefront/components/TimeSlotPicker.tsx`
- Props: `{ slots: TimeSlotResponse[]; value: string; onChange: (startIso: string) => void; isLoading: boolean; isError: boolean }` where `value === ''` means ASAP.
- Renders a radio group styled like the existing order-type cards:
  - First option always **ASAP** (`storefront.checkout.timeSlot.asap`) — AC3.
  - One option per slot, label from `formatTimeSlot`; when `!isAvailable`: `disabled`, `opacity 0.5`, `cursor: not-allowed`, suffixed badge `storefront.checkout.timeSlot.full` — AC2.
  - Empty `slots` (e.g. near closing): ASAP option + `storefront.checkout.timeSlot.noneAvailable` notice.
  - Error state: ASAP option + load-error notice (ordering must still work).
  - Slot count can be large (a long-open shop at 5-min intervals yields hundreds of slots): render the slot options in a max-height scrollable container (ASAP pinned above it) rather than an unbounded list.
  - **Stale-slot guard:** the server only generates future slots (start ≥ now + interval), but the fetched list ages between refetches — at render time, filter out slots whose `start` is no longer in the future (`new Date(slot.start) > new Date()`). If the currently selected slot drops out of the filtered list, reset the selection to ASAP. Defense-in-depth: server still rejects past/misaligned slots with 400 on submit.

### 12. CheckoutPage integration

**File:** `src/frontend/src/features/storefront/pages/CheckoutPage.tsx`
- `CheckoutForm` gets `timeSlotOrdering` prop from `useResolvedShop()`.
- Form: add `timeSlotStart: string` to `CheckoutFormValues` + zod schema (plain `z.string()`, default `''`); ASAP needs no validation.
- Render between the order-type fieldset and the VAT notice:
  - `timeSlotOrdering.isEnabled` → `useAvailableTimeSlots(...)` + `<TimeSlotPicker>` (Controller-wrapped) — AC1.
  - else → static info banner `storefront.checkout.timeSlot.asapNotice` (reuse the VAT-notice styling) — AC5 MVP fallback.
- `onSubmit`: `timeSlotStart: values.timeSlotStart || null` in the mutate payload.
- Error handling: if `createOrder` fails with HTTP 409 (axios `error.response?.status === 409`), show `storefront.checkout.timeSlot.slotFull`, reset `timeSlotStart` to `''`, and `queryClient.invalidateQueries({ queryKey: ['timeSlots', brandSlug, shopId] })`.

### 13. Display surfaces (AC4)

**File:** `src/frontend/src/features/pos/components/KitchenOrderCard.tsx`
- Replace the `order.timeSlot` condition (line 143) with `order.timeSlotStart != null && order.timeSlotEnd != null`, rendering `t('pos.kitchen.timeSlot', { value: formatTimeSlot(order.timeSlotStart, order.timeSlotEnd) })`. Existing i18n key + `data-testid="kitchen-order-timeslot"` are kept.

**File:** `src/frontend/src/features/pos/utils/printTicket.ts`
- Same swap: build the meta row from `timeSlotStart`/`timeSlotEnd` via `formatTimeSlot` (the `labels.timeSlot` label param stays).

**File:** `src/frontend/src/features/storefront/pages/OrderConfirmationPage.tsx`
- Add a "Time slot" row when `timeSlotStart` is present (key `storefront.order.timeSlot`), so the customer sees what they picked.

### 14. i18n (NL / FR / DE)

**Files:** `src/frontend/src/i18n/locales/{nl,fr,de}/common.json` — under `storefront.checkout`, new `timeSlot` object; plus one key under `storefront.order`:

| Key | NL | FR | DE |
|---|---|---|---|
| `checkout.timeSlot.label` | Tijdslot | Créneau horaire | Zeitfenster |
| `checkout.timeSlot.asap` | Zo snel mogelijk | Dès que possible | So schnell wie möglich |
| `checkout.timeSlot.full` | Vol | Complet | Voll |
| `checkout.timeSlot.noneAvailable` | Geen tijdsloten meer beschikbaar vandaag. Uw bestelling wordt zo snel mogelijk bereid. | Plus de créneaux disponibles aujourd'hui. Votre commande sera préparée dès que possible. | Heute sind keine Zeitfenster mehr verfügbar. Ihre Bestellung wird so schnell wie möglich zubereitet. |
| `checkout.timeSlot.asapNotice` | Uw bestelling wordt zo snel mogelijk bereid. | Votre commande sera préparée dès que possible. | Ihre Bestellung wird so schnell wie möglich zubereitet. |
| `checkout.timeSlot.slotFull` | Dit tijdslot is net volgeboekt. Kies een ander tijdslot. | Ce créneau vient d'être complet. Veuillez en choisir un autre. | Dieses Zeitfenster ist soeben ausgebucht. Bitte wählen Sie ein anderes. |
| `checkout.timeSlot.loadError` | Tijdsloten konden niet geladen worden. U kunt wel "zo snel mogelijk" bestellen. | Impossible de charger les créneaux. Vous pouvez commander "dès que possible". | Zeitfenster konnten nicht geladen werden. Sie können "so schnell wie möglich" bestellen. |
| `order.timeSlot` | Tijdslot | Créneau horaire | Zeitfenster |

(`pos.kitchen.timeSlot` already exists in all three locales.)

### 15. MSW handlers / fixtures

**File:** `src/frontend/src/test/msw/handlers.ts`
- Add `timeSlotOrdering` to the active-shops fixture (line ~270 already has it on the admin shop fixture — the storefront one will now need it too).
- New handler: `GET */brands/:brandSlug/shops/:shopId/time-slots`.

## Tests

### Backend — TUnit (per dotnet-testing skill: `[Test]`, awaited `Assert.That`, `dotnet run -c Release`; **run integration classes individually — parallel SQL containers OOM**)

**Unit — `src/backend/TheMillionthFoodOrderApp.Tests.Unit/Shops/TimeSlotGeneratorTests.cs`** (new)
- Disabled settings → empty. No opening hours → empty. Invalid TimeZoneId → empty.
- Slots anchor at block `OpenTime` and step by interval (11:30 open + 15 min → 11:30, 11:45 …).
- Slot whose end would exceed `CloseTime` is excluded.
- Lead time: slots starting before `now + interval` excluded; mid-day "now" yields only remaining slots.
- Two blocks in one day (lunch + dinner) → both covered, gap excluded.
- UTC conversion correct for Europe/Brussels (+02:00 in June).
- 24h-open shop at 5-min interval from start of day → 288 slots (full day, no truncation).

**Unit — `src/backend/TheMillionthFoodOrderApp.Tests.Unit/Orders/OrderTests.cs`** (new or extend)
- `Order.Create` with start-only / end-only throws; `end <= start` throws; both-null OK; both-set persists.

**Integration — `src/backend/TheMillionthFoodOrderApp.Tests.Integration/Orders/GetAvailableTimeSlotsTests.cs`** (new) — `[ClassDataSource<IntegrationTestBase>(Shared = SharedType.PerClass)]`, seed via HTTP like `PlaceOrderTests` (`PUT shops/{id}` to enable `timeSlotOrdering`, always-open hours helper):
1. Disabled shop → `{ isEnabled: false, slots: [] }`.
2. Enabled (10 min, max 2) → slots aligned to interval, all `isAvailable: true`, `remainingCapacity: 2`.
3. Place 2 orders into one slot → that slot `isAvailable: false`, `remainingCapacity: 0`; neighbours unaffected.
4. Unknown shop id → 404.

**Integration — `src/backend/TheMillionthFoodOrderApp.Tests.Integration/Orders/PlaceOrderTimeSlotTests.cs`** (new)
1. Valid slot → 201; `timeSlotStart`/`timeSlotEnd` echoed; persisted (fetch tracking endpoint shows them).
2. No slot (ASAP) with slots enabled → 201, nulls.
3. Slot supplied while shop has slots disabled → 400.
4. Misaligned/past slot → 400.
5. Slot at capacity → **409** with `timeSlotStart` failure.
6. Kitchen path: `GET orders/active` includes `timeSlotStart`/`timeSlotEnd` (AC4).
7. In-store endpoint ignores slots entirely (existing `CreateInStoreOrderServiceTests` untouched; add one assertion that in-store orders have null slots).

### Frontend — vitest

- **`src/frontend/src/features/storefront/components/__tests__/TimeSlotPicker.test.tsx`** (new): renders ASAP first; renders formatted slot labels; full slots disabled (AC2); selecting fires `onChange`; empty-slots and error states still offer ASAP; slots with a `start` in the past are not rendered (use fake timers), and a selected slot that ages out resets the selection to ASAP.
- **`src/frontend/src/features/storefront/pages/__tests__/CheckoutPage.test.tsx`** (extend): picker visible when enabled (AC1); ASAP notice when disabled (AC5); submit payload contains `timeSlotStart` (and `null` for ASAP); 409 → slot-full message shown + slots refetched.
- **`src/frontend/src/features/pos/components/__tests__/KitchenOrderCard…`** + **`printTicket.test.ts`** (update): time-slot badge/row renders from `timeSlotStart`/`timeSlotEnd`; absent when null.
- **`src/frontend/src/utils/__tests__/timeSlot.test.ts`** (new): formatting.
- Update any fixture using the removed `timeSlot` string field.

## Verification

- `dotnet build TheMillionthFoodOrderApp.slnx` in `src/backend/` → 0 errors
- `dotnet run -c Release` in `TheMillionthFoodOrderApp.Tests.Unit/`
- `dotnet run -c Release` in `TheMillionthFoodOrderApp.Tests.Integration/` — run the new classes individually (OOM constraint): `--treenode-filter "/*/*/GetAvailableTimeSlotsTests/*"` etc.
- `pnpm test` and `pnpm build` in `src/frontend/`
- Manual: enable slots on a seeded shop via admin (US-FP-020 UI), place an online order with a slot, confirm the kitchen display badge and ticket print show it.

## Scope decisions & open questions (explicit)

1. **AC5 / wait time:** US-FP-021 not started → static "as soon as possible" notice; no per-shop configured wait minutes, no place-in-line. Revisit in US-FP-021.
2. **Capacity counts all statuses:** no cancelled/refused order concept exists; when one lands, the count query must exclude it.
3. **Overbooking race:** check-then-insert is not transactionally guarded; worst case one extra order in a slot under concurrent submits. Acceptable for MVP; fix later with a slot-counter row + unique constraint if needed.
4. **Today-only horizon:** no advance-day ordering; aligns with same-day fast-food flow. Special hours/holidays (US-FP-041) not done — slots follow weekly opening hours only, consistent with `IsOpenAt`.
5. **POS stays ASAP-only**; kitchen display continues to sort by `CreatedAt` (sorting by slot is a possible follow-up, not in the AC).
6. **Frontend `timeSlot` string field is replaced** by `timeSlotStart`/`timeSlotEnd` — it was speculative and never populated, so this is not a breaking change for real data.

## File summary

**Backend (modified):** `Domain/Orders/Order.cs`, `Domain/Orders/IOrderRepository.cs`, `Infrastructure/Orders/OrderConfiguration.cs`, `Infrastructure/Orders/OrderRepository.cs`, `Application/Orders/OrderDtos.cs`, `Application/Orders/OrderService.cs`, `Application/DependencyInjection.cs`, `Application/Shops/ShopDtos.cs`, `Application/Shops/ShopService.cs`, `Api/Endpoints/Orders/CreateOrderEndpoint.cs`, `Api/Endpoints/Orders/OrderTrackingMapper.cs`
**Backend (new):** `Domain/Shops/TimeSlotGenerator.cs`, `Application/Orders/ITimeSlotAvailabilityService.cs`, `Application/Orders/TimeSlotAvailabilityService.cs`, `Api/Endpoints/Orders/GetAvailableTimeSlotsEndpoint.cs`, `Infrastructure/Persistence/Migrations/Brand/<ts>_AddOrderTimeSlot.cs`, `Tests.Unit/Shops/TimeSlotGeneratorTests.cs`, `Tests.Unit/Orders/OrderTests.cs`, `Tests.Integration/Orders/GetAvailableTimeSlotsTests.cs`, `Tests.Integration/Orders/PlaceOrderTimeSlotTests.cs`
**Frontend (modified):** `api/orders.ts`, `api/shops.ts`, `features/storefront/context/shopContextValue.ts`, `features/storefront/context/ShopContext.tsx`, `features/storefront/pages/CheckoutPage.tsx`, `features/storefront/pages/OrderConfirmationPage.tsx`, `features/pos/components/KitchenOrderCard.tsx`, `features/pos/utils/printTicket.ts`, `i18n/locales/{nl,fr,de}/common.json`, `test/msw/handlers.ts`
**Frontend (new):** `features/storefront/hooks/useAvailableTimeSlots.ts`, `features/storefront/components/TimeSlotPicker.tsx`, `utils/timeSlot.ts`, plus the test files above
**Docs:** `docs/dependency-tree.md` — mark US-FP-019 ✅ after merge
