# User Story Dependency Tree & Progress Tracker

> Generated from [frietjes-platform.md](./extract-prd/user-stories/frietjes-platform.md)
> Last updated: 2026-06-10 (US-FP-019 time-slot picker at checkout done; earlier: US-FP-066 eat-in toggle + US-FP-020 time-slot config, US-FP-051 digital receipt, US-FP-052 print receipt, US-FP-026 order notifications, US-FP-023 + US-FP-071 — see "Status Accuracy" below)

## Progress Legend

| Symbol | Meaning |
|--------|---------|
| ✅ | Done |
| 🚧 | Partial / In Progress |
| ⬜ | Not Started |

---

## Status Accuracy & AC Verification (2026-06-04)

> **Tracking caveat:** GitHub issues are *not* closed when work merges, and this tree had drifted. Statuses below were re-verified by checking each open issue's **acceptance-criteria checklist** against the actual code (backend + frontend). Treat the issue tracker's open/closed state as unreliable on its own — verify against ACs.

**✅ Online-ordering entry point RESOLVED (2026-06-04).** US-FP-071 (shop selection + shop-slug-scoped storefront routes) merged via **PR #128** and was runtime-verified. The storefront now has a real customer journey: `/{brand}/{lang}/shops` chooser → `{shopSlug}/menu` → checkout → payment → order tracking, with the localStorage `shopId` hack removed. This unblocks the cluster that was previously "component-complete but NOT user-reachable" — **US-FP-016 / 017 / 038 / 058 / 063 restored to ✅.** (Historical note: this gap was found 2026-06-03 — `Home.tsx` was a stub and the menu only rendered at a GUID-based route nothing linked to.)

**Changed 2026-06-10:**
- **US-FP-019** → ✅ done. Customer-facing time-slot picker at checkout (PR `feat/us-fp-019-time-slot-fable`, closes #19). New anonymous `GET /brands/{slug}/shops/{shopId}/time-slots` returns server-computed same-day slots (UTC start + shop-local `"HH:mm"` label + availability) from a pure-domain `TimeSlotCalculator`; `Order` gains nullable `TimeSlotStart` (UTC, capacity counting on a new `(ShopId, TimeSlotStart)` index) + denormalised `TimeSlot` label (brand migration `AddOrderTimeSlot`). Create-order step 3c validates tick-precision alignment / opening block / same-local-day / not-elapsed and enforces `MaxOrdersPerInterval` via a best-effort count (`TIME_SLOT_FULL` → 400 `errors.timeSlotStart`; overshoot-by-1 accepted for MVP). Slots apply to all **online** order types; in-store POS bypasses (like opening hours). Checkout: ASAP-default radio picker (full slots greyed/disabled), slot-full error + auto-refetch, stale-selection auto-reset; when slots are disabled the checkout shows a **place-in-line** notice from `activeOrderCount` (configurable wait estimates remain US-FP-021). Kitchen badge + printed ticket already rendered `timeSlot` — now populated. Confirmation page shows "Ready at HH:mm".
- **US-FP-066** → ✅ done. Per-shop eat-in toggle + a "require table number" sub-toggle, modelled as an owned `EatInSettings` value object on `Shop` (defaults: eat-in **on**, table **required** — preserves prior behaviour). Admin ShopEdit checkboxes; storefront + POS hide the eat-in order type when disabled and gate the table-number field (required/optional) by the flag; **server-side enforcement** in the shared `OrderService` create-core rejects eat-in when disabled and requires a table number when mandated, so online + in-store are gated identically. Closed US-FP-024's `IsEatInEnabled` gating gap and added **online table-number capture** (the public order path/`CreateOrderRequest` now accepts `tableNumber`). Brand migration `AddShopEatInAndTimeSlotSettings`.
- **US-FP-020** → ✅ done. Per-shop time-slot ordering config — owned `TimeSlotOrderingSettings` VO (enabled + `TimeSlotInterval` enum 5/10/15 + max-orders-per-slot), admin UI (watch-gated interval `<select>` + max input) + FluentValidation. Config only; the customer-facing slot picker is **US-FP-019** (still ⬜, now unblocked since 020 is done).
- **US-FP-051** → ✅ done. Digital receipt emailed when an **online** order reaches a terminal lifecycle status. New email infra: `IEmailSender` (Application) → MailKit `SmtpEmailSender` (Infrastructure) + a **mailpit** dev container in Aspire (SMTP 1025 / UI 8025); prod is a config-only SMTP swap. `ReceiptComposer` renders a localized HTML receipt (NL/FR/DE) mirroring the US-FP-052 printed layout, reusing the denormalised `OrderResponse` receipt data. Send is **synchronous** in `OrderService.AdvanceOrderStatusAsync` (request scope — avoids the Wolverine-handler tenant-context pitfall), online-only (`CreatedByStaffId is null`), idempotent via a persisted `Order.ReceiptEmailSent` flag, best-effort (try/catch, never fails the status advance). Scope expansions: customer name **split into first/last**; per-order `LanguageCode` captured at checkout (frontend sends the route lang); **guest checkout now requires first+last+email+phone** (enforced in `CreateOrderEndpoint` after merging claim-or-body), while logged-in customers get those from their **profile** — added an OIDC `phone` scope + Keycloak mapper + seed attributes, surfaced `firstName`/`lastName`/`phoneNumber` on `/bff/user`, with mock-auth parity. One brand migration `SplitOrderNameAndAddReceiptFields` (data-preserving). Tests: 314 unit + Bff + new receipt-email/claims integration tests green; frontend 342 vitest + type-check + build green.
- **US-FP-052** → ✅ done. POS customer receipt: thermal-format `buildReceiptHtml` (seller legal block — shop name, address, VAT number — plus per-line prices, Belgian VAT breakdown net/VAT/gross, payment method, date) printed via the shared hidden-iframe `printDocument` helper (extracted from US-FP-028's `printTicket`). A "Print receipt" action on the POS confirmation screen triggers print and doubles as reprint (AC3). New nullable `Shop.VatNumber` (domain + EF + brand migration `AddShopVatNumber` + shop create/update DTOs/endpoints/validators + admin ShopEdit field). The seller legal block is denormalised onto `OrderResponse` (populated on the create + GET-order paths; null on the kitchen status-advance/list paths) — this also lays the groundwork for US-FP-051 (digital receipt). Note: the receipt button is fed the order via router state, so reprint is available on the confirmation screen but not after a hard refresh (acceptable for MVP). 284 unit + 45 integration + 84 frontend tests green.
- **US-FP-071** → ✅ done. Shop chooser + shop-slug routes; FE/BE verified at runtime. PR #128 merged.
- **US-FP-023** → ✅ done. Status-advance endpoint + kitchen-card button shipped (commit `adf18f2`); SignalR negotiate CSRF fix (`7fb5299`). The keystone is complete.
- **US-FP-016 / 017 / 038 / 058 / 063** → ✅ — entry-point blocker resolved by 071; these were already component-complete.
- **US-FP-028** → ✅ done. Per-shop `TicketPrinterEnabled` flag (admin toggle on ShopEdit); kitchen display auto-prints new orders via a hidden print iframe when enabled, plus a manual reprint button on every order card. Ticket carries order number, items+modifiers, type, table (eat-in), time slot (when present), timestamp.
- **US-FP-026** → ✅ done. Per-shop notification settings extended to four independent channels — `KitchenDisplayEnabled` (new-order highlight on the board), `TicketPrinterEnabled` (US-FP-028), `PushNotificationEnabled` (browser Notification API), `SoundAlertEnabled` (Web Audio chime). Admin toggles on ShopEdit; all channels hook the kitchen display's single new-order detection loop, so both online and in-store orders fire them (AC4 — shared `OrderService.CreateOrderCoreAsync` → `OrderCreatedHandler` → SignalR). Sound/push are armed once via an "enable alerts" control (browser gesture/permission policy).

**Changed 2026-06-03:**
- **US-FP-018** → ✅ done. Functionally complete; ticket-printer scope reclassified to US-FP-028. GitHub #18 closed.

**Partial (🚧) — specific remaining gaps:**
- **US-FP-069** — per-endpoint RBAC not enforced (62 endpoints `AllowAnonymous`; roles only checked at the BFF proxy). ⚠ security hardening needed before production.
- **US-FP-024** — eat-in gating (`Shop.EatIn` settings, US-FP-066) + table-number capture (online **and** POS) + 21% VAT done; only group-kitchen-cards-by-table remains.
- **US-FP-037** — mock auth + BFF login work; customer self-registration not implemented.
- **US-FP-039** — session timeout configurable; missing dynamic per-brand login UI, role-based redirect, failed-login errors.
- **US-FP-055** — static manifest only; needs per-brand manifest (logo/colors/icons).
- **US-FP-056** — app shell cached; needs menu-data caching + stale-data banner.
- **US-FP-059** — order type + 6% VAT done; missing delivery address capture/storage + per-shop delivery fee.
- **US-FP-060** — brand CRUD done; missing shop-count metrics + true platform-level dashboard.
- **US-FP-067** — custom-domain field stored; missing DNS instructions, SSL provisioning, host→brand routing.

**Everything else not marked ✅/🚧 below is not started** (32 stories).

---

## Layer 0 — Foundation (sequential)

These are prerequisites for everything else. Must be done in order.

| Status | Story | Description | Depends On |
|--------|-------|-------------|------------|
| 🚧 | **US-FP-069** | REST API backbone (FastEndpoints, BFF, YARP) — ⚠ per-endpoint RBAC not enforced | — |
| ✅ | **US-FP-061** | Platform admin accounts | — |
| ✅ | **US-FP-001** | Create and manage brands | 069 |
| ✅ | **US-FP-070** | Database-per-brand provisioning | 001 |
| ✅ | **US-FP-004** | Data isolation between brands | 001, 070 |
| ✅ | **US-FP-002** | Create and manage shops | 001 |

**Notes:**
- 069: FastEndpoints + Swagger + BFF + YARP proxy all configured and working
- 001: Complete — full CRUD, database provisioning, activate/deactivate, frontend UI
- 070: Complete — provisioner with retry, verification, health check, and integration tests
- 004: BrandSettings entity in BrandDbContext, middleware validates slugs (404/403), BrandScopedPreProcessor, integration tests with Testcontainers prove cross-brand isolation
- 002: Shop CRUD + activate/deactivate, full-stack implementation with brand-scoped database
- 061: Complete — full-stack CRUD with list, invite, deactivate, last-admin guard, 7 integration tests

---

## Layer 1 — Core Domains (4 parallel streams)

Once brands + shops exist, these streams are **independent of each other**.

### Stream A: Products

| Status | Story | Description | Depends On |
|--------|-------|-------------|------------|
| ✅ | **US-FP-005** | Create and manage simple products | 001, 002 |
| ✅ | **US-FP-006** | Add modifier groups to products | 005 |
| ✅ | **US-FP-007** | Create and manage combo products | 005 |
| ✅ | **US-FP-008** | Manage allergen and dietary information | 005 |
| ✅ | **US-FP-014** | Define menu categories | 005 |
| ✅ | **US-FP-015** | Order products within categories | 014 |

### Stream B: Auth & Staff

| Status | Story | Description | Depends On |
|--------|-------|-------------|------------|
| ✅ | **US-FP-003** | Assign brand-level staff auth method | 001 |
| 🚧 | **US-FP-037** | Customer registration and login | 001 |
| ✅ | **US-FP-032** | Manage staff accounts | 001, 003 |
| 🚧 | **US-FP-039** | Staff login with configured auth method | 003, 032 |

**Notes:**
- 003: Full-stack complete — domain, endpoint, frontend config UI with confirmation dialog and i18n
- 037: Mock auth complete; real Entra External ID / SSO is TODO
- 032: Complete — full-stack CRUD: invite by email with role+shop, list, deactivate, shop-filtered view, last-admin guard, 10 integration tests

### Stream C: Shop Configuration

| Status | Story | Description | Depends On |
|--------|-------|-------------|------------|
| ✅ | **US-FP-040** | Set shop opening hours | 002 |
| ⬜ | **US-FP-041** | Set special hours and holiday overrides | 040 |
| ✅ | **US-FP-020** | Configure time slot settings | 002 |
| ⬜ | **US-FP-021** | Configure estimated wait times | 002 |
| ✅ | **US-FP-022** | Configure order lifecycle statuses | 002 |
| ✅ | **US-FP-066** | Enable or disable eat-in ordering | 002 |
| ⬜ | **US-FP-065** | Generate QR codes for tables | 002 |

### Stream D: Branding & i18n

| Status | Story | Description | Depends On |
|--------|-------|-------------|------------|
| ✅ | **US-FP-029** | Configure brand theming | 001 |
| ✅ | **US-FP-030** | Provide translations for product catalog | 005 |
| ✅ | **US-FP-031** | Select language on the storefront | 030 |
| 🚧 | **US-FP-067** | Configure custom domain for brand | 029 |

> **Note:** 030 and 031 depend on products (Stream A), so they can start once 005 is done.

---

## Layer 2 — Ordering Core (needs Streams A + C)

| Status | Story | Description | Depends On |
|--------|-------|-------------|------------|
| ✅ | **US-FP-046** | Apply Belgian VAT rates | 022 |
| ✅ | **US-FP-068** | Real-time updates (SignalR/SSE) | — (infra, can start in L1) |
| ✅ | **US-FP-016** | Place an online order | 005, 006, 007, 014, 022, 046 |
| ✅ | **US-FP-071** | Shop selection + shop-scoped routes (`/shops` chooser → `{shopSlug}/menu` → checkout/order) — #127 (PR #128), runtime-verified | 002, 005 |
| ✅ | **US-FP-058** | Complete payment flow (mocked) | 016 |
| ✅ | **US-FP-017** | Place an order as a guest | 016 |
| ✅ | **US-FP-018** | Place an in-store order (POS) | 016 |

---

## Layer 3 — Post-Ordering Features (5 parallel streams)

Once ordering works, these streams are **all independent**.

### Stream E: Kitchen & Fulfillment

| Status | Story | Description | Depends On |
|--------|-------|-------------|------------|
| ✅ | **US-FP-027** | View order on kitchen display | 022, 068 |
| ✅ | **US-FP-023** | Update order status (kitchen) | 022, 027 |
| ✅ | **US-FP-028** | Print order ticket (incl. in-store POS ticket, reclassified from US-FP-018) | 022 |
| ✅ | **US-FP-026** | Configure order notifications | 022 |

### Stream F: Customer Experience

| Status | Story | Description | Depends On |
|--------|-------|-------------|------------|
| ✅ | **US-FP-063** | Track current order status | 022, 068 |
| ⬜ | **US-FP-062** | View order history | 016, 037 |
| 🚧 | **US-FP-024** | Eat-in ordering with table number | 016, 066 |
| ⬜ | **US-FP-025** | QR code table ordering | 024, 065 |
| ✅ | **US-FP-019** | Select time slot at checkout | 016, 020 |
| ✅ | **US-FP-064** | Browse menu with allergen/dietary filters | 008 |
| 🚧 | **US-FP-059** | Place a delivery order (POC) | 016 |

### Stream G: Shop Product Management

| Status | Story | Description | Depends On |
|--------|-------|-------------|------------|
| ⬜ | **US-FP-009** | Shop adds custom products (with approval) | 005, 002 |
| ⬜ | **US-FP-010** | Shop requests price override | 005, 002 |
| ⬜ | **US-FP-011** | Brand admin reviews approval requests | 009, 010 |
| ⬜ | **US-FP-012** | Set daily stock count per product | 005, 002 |
| ⬜ | **US-FP-013** | Toggle product availability in real-time | 005, 002 |

### Stream H: Loyalty & Promotions

| Status | Story | Description | Depends On |
|--------|-------|-------------|------------|
| ⬜ | **US-FP-047** | Configure stamp/loyalty program | 001, 037 |
| ⬜ | **US-FP-048** | View and redeem loyalty points | 047, 016 |
| ⬜ | **US-FP-049** | Create multi-use discount codes | 001, 016 |
| ⬜ | **US-FP-050** | Create single-use discount codes | 049 |

### Stream I: Reporting & Analytics

| Status | Story | Description | Depends On |
|--------|-------|-------------|------------|
| ⬜ | **US-FP-042** | View sales dashboard | 016 |
| ⬜ | **US-FP-043** | View product performance report | 016 |
| ⬜ | **US-FP-044** | View order analytics | 016 |

---

## Layer 4 — Advanced Features (4 parallel streams)

### Stream J: Receipts

| Status | Story | Description | Depends On |
|--------|-------|-------------|------------|
| ✅ | **US-FP-051** | Generate digital receipt (email on online order completion) | 016, 046 |
| ✅ | **US-FP-052** | Print receipt at point of sale (POS thermal receipt + reprint) | 016, 046 |

### Stream K: Offline Mode

| Status | Story | Description | Depends On |
|--------|-------|-------------|------------|
| ⬜ | **US-FP-053** | In-store interface works offline | 018 |
| ⬜ | **US-FP-054** | Kitchen display works offline | 027 |

### Stream L: PWA

| Status | Story | Description | Depends On |
|--------|-------|-------------|------------|
| 🚧 | **US-FP-055** | Install storefront as PWA | 029 |
| 🚧 | **US-FP-056** | Offline caching of menu data | 055 |
| ⬜ | **US-FP-057** | Push notifications for order status | 055, 063 |

### Stream M: Staff Scheduling

| Status | Story | Description | Depends On |
|--------|-------|-------------|------------|
| ⬜ | **US-FP-033** | Create and assign shifts | 032, 002 |
| ⬜ | **US-FP-034** | Manage staff availability | 033 |
| ⬜ | **US-FP-035** | Request shift swap | 033 |
| ⬜ | **US-FP-036** | Track staff hours | 033 |

---

## Layer 5 — Platform Polish

| Status | Story | Description | Depends On |
|--------|-------|-------------|------------|
| 🚧 | **US-FP-060** | Platform admin dashboard | 001, reporting |
| ⬜ | **US-FP-045** | View inventory signals | 012, 013 |
| ✅ | **US-FP-038** | Guest checkout (refinement) | 017 |

---

## Visual Summary

```
L0: [069🚧] → [061✅] → [001✅] → [070✅] → [004✅] → [002✅]

L1: [A: Products✅]  [B: Auth/Staff🚧]  [C: Shop Config🚧]  [D: Branding✅]  ← 4 parallel
         │                │                  │
L2: [046✅ VAT] [068✅ Realtime] → [071✅ entry] → [016✅ → 058✅ 018✅ 017✅]   ← ordering core ✅
                                        │
L3: [E: Kitchen] [F: Customer] [G: Shop Mgmt] [H: Loyalty] [I: Reports]      ← 5 parallel
         │            │
L4: [J: Receipts] [K: Offline] [L: PWA] [M: Scheduling]                      ← 4 parallel
         │
L5: [060 Dashboard] [045 Inventory] [038 Guest polish]                        ← polish
```

## Worktree Strategy (4 worktrees)

| Phase | WT-1 | WT-2 | WT-3 | WT-4 |
|-------|------|------|------|------|
| **Layer 0** | Foundation (sequential) | — | — | — |
| **Layer 1** | Stream A: Products | Stream B: Auth | Stream C: Shop Config | Stream D: Branding |
| **Layer 2** | 068 Real-time | 046 VAT | 016 + 058 Ordering | 017 + 018 Guest/POS |
| **Layer 3** | Stream E: Kitchen | Stream F: Customer | Stream G: Shop Mgmt | Stream H+I: Loyalty+Reports |
| **Layer 4** | Stream J: Receipts | Stream K: Offline | Stream L: PWA | Stream M: Scheduling |
| **Layer 5** | 060 Dashboard | 045 Inventory | 038 Guest polish | — |

**Critical path:** `001 → 002 → 005 → 014 → 016 → downstream features` — keep Stream A moving fast.
