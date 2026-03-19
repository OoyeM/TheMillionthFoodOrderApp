# Implementation Plan: US-FP-040 — Set Shop Opening Hours

## Overview

Add weekly opening hours to shops, enabling automatic online ordering control based on business hours. A Shop Manager can configure multiple time blocks per day of the week (e.g., lunch and dinner service). The storefront shows real-time open/closed status with the next opening time. Opening hours are modeled as child entities of the Shop aggregate, stored in the brand-scoped database, following existing DDD and Clean Architecture patterns exactly.

## Requirements (from Acceptance Criteria)

1. Shop Manager can set opening and closing times for each day of the week
2. A shop can have multiple time blocks per day (e.g., 11:00-14:00 and 17:00-22:00)
3. Online ordering is automatically disabled outside opening hours
4. The storefront shows the shop's current status (open/closed) and next opening time

## Key Design Decisions

1. **OpeningHoursTimeBlock as a child entity of Shop.** Opening hours are always accessed in Shop context. The Shop aggregate is not overly large. No separate repository needed.
2. **`TimeOnly` for open/close times.** These are time-of-day values, not instants. `TimeOnly` maps natively to SQL Server `time(7)` in EF Core 8+. `DateTimeOffset` is not appropriate here because these are recurring weekly schedules, not specific moments.
3. **`DayOfWeek` enum (System.DayOfWeek)** stored as `int` (0=Sunday through 6=Saturday).
4. **Replace-all strategy for updates.** Following the existing translation pattern (clear collection, re-add), the set endpoint accepts the full weekly schedule and replaces all existing time blocks atomically.
5. **Shop status is computed, not stored.** `IsOpenAt()` is a pure domain method. The API exposes a dedicated status endpoint.
6. **Timezone: "Europe/Brussels" default.** `TimeZoneId` property on Shop for future-proofing. Times stored as local Belgian time, converted at query time.
7. **No overnight blocks for MVP.** Validation requires `openTime < closeTime`. Overnight spans (e.g., 22:00-02:00) are a US-FP-041 concern.

---

## Implementation Phases

### Phase 1: Domain Layer

**1.1 Create OpeningHoursTimeBlock entity**
- File: `Domain/Shops/OpeningHoursTimeBlock.cs`
- `OpeningHoursTimeBlock : Entity<Guid>` (child entity, not aggregate root)
- Properties: `Guid ShopId`, `DayOfWeek DayOfWeek`, `TimeOnly OpenTime`, `TimeOnly CloseTime`
- Private constructor for EF Core. Factory method with validation (close > open). Uses `Guid.CreateVersion7()`.

**1.2 Add opening hours to Shop aggregate**
- File: `Domain/Shops/Shop.cs`
- Add `_openingHours` collection and public `IReadOnlyCollection` accessor
- Add `string TimeZoneId` property (default "Europe/Brussels")
- `SetOpeningHours(IEnumerable<OpeningHoursTimeBlock> blocks)` — clear + add, validate no overlaps
- `IsOpenAt(DateTimeOffset now)` — convert to local time, check blocks for current day
- `GetNextOpeningTime(DateTimeOffset now)` — find next block start (wraps around week)

### Phase 2: Application Layer

**2.1 Opening Hours DTOs** — `Application/Shops/OpeningHoursDtos.cs`
**2.2 IOpeningHoursService + OpeningHoursService** — service interface and implementation
**2.3 Register in DI** — `Application/DependencyInjection.cs`

### Phase 3: Infrastructure Layer

**3.1 OpeningHoursTimeBlockConfiguration** — EF Core entity config
**3.2 Update BrandDbContext** — add DbSet
**3.3 Update ShopConfiguration** — add HasMany + TimeZoneId
**3.4 Update ShopRepository** — add `.Include(s => s.OpeningHours)`
**3.5 Generate EF migration**

### Phase 4: API Endpoints

**4.1 SetOpeningHoursEndpoint** — `PUT /api/brands/{brandSlug}/shops/{id}/opening-hours`
**4.2 GetOpeningHoursEndpoint** — `GET /api/brands/{brandSlug}/shops/{id}/opening-hours`
**4.3 GetShopStatusEndpoint** — `GET /api/brands/{brandSlug}/shops/{id}/status`

### Phase 5: Frontend Admin UI

**5.1** Types + API client
**5.2** TanStack Query hooks
**5.3** ShopOpeningHours page — day-by-day grid, time inputs, add/remove blocks
**5.4** Routes + navigation from ShopEdit
**5.5** i18n translations (NL, FR, DE)

### Phase 6: Frontend Storefront

**6.1** ShopStatusBadge component — open/closed badge with next opening time, 60s auto-refresh
**6.2** Integrate into storefront Home page

### Phase 7: Integration Tests

**7.1 OpeningHoursCrudTests** — set, replace, multi-block, clear, validation, not-found
**7.2 ShopStatusTests** — no-hours, within-hours, outside-hours, not-found

---

## Risks & Mitigations

| Risk | Severity | Mitigation |
|------|----------|------------|
| Overlap validation in domain | Medium | Sort blocks by OpenTime per day, check pairwise |
| EF Core replace-all for child entities | Medium | Follow translation pattern: clear + AddRange, cascade delete |
| Time zone handling | Medium | Default "Europe/Brussels", use TimeZoneInfo.FindSystemTimeZoneById() |
| Overnight blocks (22:00-02:00) | Low | Explicitly disallowed for MVP, deferred to US-FP-041 |

## Dependencies

- **Prerequisites (complete):** US-FP-002 (Shop Management)
- **Unblocks:** US-FP-041 (Special hours and holiday overrides)
