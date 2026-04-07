# User Story Dependency Tree & Progress Tracker

> Generated from [frietjes-platform.md](./extract-prd/user-stories/frietjes-platform.md)
> Last updated: 2026-03-15

## Progress Legend

| Symbol | Meaning |
|--------|---------|
| ✅ | Done |
| 🚧 | Partial / In Progress |
| ⬜ | Not Started |

---

## Layer 0 — Foundation (sequential)

These are prerequisites for everything else. Must be done in order.

| Status | Story | Description | Depends On |
|--------|-------|-------------|------------|
| ✅ | **US-FP-069** | REST API backbone (FastEndpoints, BFF, YARP) | — |
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
| ⬜ | **US-FP-039** | Staff login with configured auth method | 003, 032 |

**Notes:**
- 003: Full-stack complete — domain, endpoint, frontend config UI with confirmation dialog and i18n
- 037: Mock auth complete; real Entra External ID / SSO is TODO
- 032: Complete — full-stack CRUD: invite by email with role+shop, list, deactivate, shop-filtered view, last-admin guard, 10 integration tests

### Stream C: Shop Configuration

| Status | Story | Description | Depends On |
|--------|-------|-------------|------------|
| ✅ | **US-FP-040** | Set shop opening hours | 002 |
| ⬜ | **US-FP-041** | Set special hours and holiday overrides | 040 |
| ⬜ | **US-FP-020** | Configure time slot settings | 002 |
| ⬜ | **US-FP-021** | Configure estimated wait times | 002 |
| ✅ | **US-FP-022** | Configure order lifecycle statuses | 002 |
| ⬜ | **US-FP-066** | Enable or disable eat-in ordering | 002 |
| ⬜ | **US-FP-065** | Generate QR codes for tables | 002 |

### Stream D: Branding & i18n

| Status | Story | Description | Depends On |
|--------|-------|-------------|------------|
| ✅ | **US-FP-029** | Configure brand theming | 001 |
| ✅ | **US-FP-030** | Provide translations for product catalog | 005 |
| ⬜ | **US-FP-031** | Select language on the storefront | 030 |
| ⬜ | **US-FP-067** | Configure custom domain for brand | 029 |

> **Note:** 030 and 031 depend on products (Stream A), so they can start once 005 is done.

---

## Layer 2 — Ordering Core (needs Streams A + C)

| Status | Story | Description | Depends On |
|--------|-------|-------------|------------|
| ⬜ | **US-FP-046** | Apply Belgian VAT rates | 022 |
| ⬜ | **US-FP-068** | Real-time updates (SignalR/SSE) | — (infra, can start in L1) |
| ⬜ | **US-FP-016** | Place an online order | 005, 006, 007, 014, 022, 046 |
| ⬜ | **US-FP-058** | Complete payment flow (mocked) | 016 |
| ⬜ | **US-FP-017** | Place an order as a guest | 016 |
| ⬜ | **US-FP-018** | Place an in-store order (POS) | 016 |

---

## Layer 3 — Post-Ordering Features (5 parallel streams)

Once ordering works, these streams are **all independent**.

### Stream E: Kitchen & Fulfillment

| Status | Story | Description | Depends On |
|--------|-------|-------------|------------|
| ⬜ | **US-FP-027** | View order on kitchen display | 022, 068 |
| ⬜ | **US-FP-023** | Update order status (kitchen) | 022, 027 |
| ⬜ | **US-FP-028** | Print order ticket | 022 |
| ⬜ | **US-FP-026** | Configure order notifications | 022 |

### Stream F: Customer Experience

| Status | Story | Description | Depends On |
|--------|-------|-------------|------------|
| ⬜ | **US-FP-063** | Track current order status | 022, 068 |
| ⬜ | **US-FP-062** | View order history | 016, 037 |
| ⬜ | **US-FP-024** | Eat-in ordering with table number | 016, 066 |
| ⬜ | **US-FP-025** | QR code table ordering | 024, 065 |
| ⬜ | **US-FP-019** | Select time slot at checkout | 016, 020 |
| ⬜ | **US-FP-064** | Browse menu with allergen/dietary filters | 008 |
| ⬜ | **US-FP-059** | Place a delivery order (POC) | 016 |

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
| ⬜ | **US-FP-051** | Generate digital receipt | 016, 046 |
| ⬜ | **US-FP-052** | Print receipt at point of sale | 016, 046 |

### Stream K: Offline Mode

| Status | Story | Description | Depends On |
|--------|-------|-------------|------------|
| ⬜ | **US-FP-053** | In-store interface works offline | 018 |
| ⬜ | **US-FP-054** | Kitchen display works offline | 027 |

### Stream L: PWA

| Status | Story | Description | Depends On |
|--------|-------|-------------|------------|
| ⬜ | **US-FP-055** | Install storefront as PWA | 029 |
| ⬜ | **US-FP-056** | Offline caching of menu data | 055 |
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
| ⬜ | **US-FP-060** | Platform admin dashboard | 001, reporting |
| ⬜ | **US-FP-045** | View inventory signals | 012, 013 |
| ⬜ | **US-FP-038** | Guest checkout (refinement) | 017 |

---

## Visual Summary

```
L0: [069✅] → [061🚧] → [001🚧] → [070🚧] → [004✅] → [002]

L1: [A: Products]   [B: Auth/Staff]   [C: Shop Config]   [D: Branding]     ← 4 parallel
         │                │                  │
L2: [046 VAT] [068 Realtime] → [016 Ordering] → [058/017/018]              ← ordering core
                                     │
L3: [E: Kitchen] [F: Customer] [G: Shop Mgmt] [H: Loyalty] [I: Reports]    ← 5 parallel
         │            │
L4: [J: Receipts] [K: Offline] [L: PWA] [M: Scheduling]                    ← 4 parallel
         │
L5: [060 Dashboard] [045 Inventory] [038 Guest polish]                      ← polish
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
