# US-FP-018 — `/workflows` arm output summary

**Story:** US-FP-018 — *Place an in-store order (counter staff)*: a touch-friendly POS interface so walk-in customers are served efficiently.
**Branch:** `tryWithClaudeWorkflows` (no merge/push).
**Orchestration backbone:** Claude Code's built-in `Workflow` tool (`/workflows`) — used in place of an external planning skill for decomposition, fan-out, pipelining, and adversarial quality gates.
**Final status:** Understand ✅ · Design ✅ · Backend implement ✅ (`1ddb0a4`) · Frontend implement ✅ (`9589791`) · Final review + gap-closing ✅ (commit pending). **Feature complete and green end-to-end.**

This run was **manual and interactive** — the user drove, approving each phase boundary and making the consequential calls. The notes below mark who decided what.

---

## 1. Architectural decisions (and who made them)

| # | Decision | Outcome | Decided by |
|---|---|---|---|
| D1 | In-store submit path | **New authenticated endpoint** `POST .../orders/in-store` (`Roles("CounterStaff")`) reusing `OrderService`; public `POST .../orders` stays anonymous and unchanged | **User** — chose from 3 options the Understand workflow surfaced with evidence |
| D2 | "Same fulfillment flow" scope | **SignalR-only** — reuse the existing `OrderCreatedEvent → OrderHub` fan-out; defer real KDS screen + ticket printer to US-FP-027 | **User** — workflow showed online orders already do exactly this |
| D3 | Table number storage | **Persisted on the `Order` aggregate** (field + EF mapping + BrandDb migration), required only for in-store eat-in | **User** — kitchen ticket needs it |
| D4 | Staff tracking | **Persist `CreatedByStaffId`** (same migration), captured server-side from the auth principal | **User confirmed** — flagged by Claude as a consequence of choosing an authenticated endpoint |
| D5 | Implementation cadence | **Backend first → checkpoint → frontend + tests → final review** | **User** |
| D6 | Pre-existing `OrderTrackingMapper` bug fix | **Keep it** in the backend slice | **User** — surfaced by Claude as a scope deviation |
| D7 | Residual test gaps (R1/R2) | **Close them** in the review pass (write the two missing tests) | **User** |
| — | ~13 implementation micro-decisions (separate service method, numeric table input, inline error panel, minimal confirmation screen, separate validator class, auto-gen migration, hardcoded EUR, table range 1–999, return new fields on `OrderResponse`, etc.) | Workflow-recommended defaults | **Workflow-suggested, Claude accepted & flagged** |

**Mechanical decisions Claude made directly** (not escalated): reader/probe/designer counts and slicing, reader tooling (codebase-memory-graph-first with Read/Grep fallback), and — most consequentially — **keeping all code-writing single-threaded** rather than fanning out parallel writers (see §2.3).

---

## 2. How `/workflows` shaped the plan

Five workflows ran in sequence, each its own phase, with a user checkpoint at every boundary. Totals: **5 workflows, 27 subagents, ~2.52M subagent tokens.**

### 2.1 `understand-us-fp-018` (5 agents, ~427k tok)
4 parallel `Explore` readers (POS app · Order backend · storefront checkout · fulfillment) → **barrier** → 1 synthesizer. Barrier justified: synthesis cross-references all four reads. Produced a reuse map showing the **backend is ~90% reusable**, an explicit **endpoint-vs-param evidence table**, and established that **no channel/source/table-number concept exists in `src/`** today — so D1–D4 were genuine design choices, not discoveries.

### 2.2 `design-us-fp-018` (7 agents, ~613k tok)
Probe (3 parallel) → **barrier** → Design (3 parallel: backend/frontend/tests) → **barrier** → 1 consistency critic. Produced a ~30-task ordered spec, an **AC-coverage matrix** (all 6 ACs → tasks + tests), `reuseViolations: []`. **Adversarial verify earned its keep:** the critic returned `contractConsistency: false` — `OrderResponse` was missing `tableNumber`/`createdByStaffId` — adding a task *before* any code was written.

### 2.3 `implement-us-fp-018-backend` (5 agents, ~448k tok)
**Deliberate orchestration choice:** the backend slice is one tightly-coupled contract, so **fanning out parallel writers would produce inconsistent, conflicting code**. The workflow kept *writing* coherent (1 implementer) and spent parallelism on **review**: Build (1) → Review (3 parallel lenses, barrier) → Repair (1). The contract reviewer (HIGH) caught `CreatedByStaffId` leaking into the app-layer request DTO → repair made it a **server-side-only parameter**. Repair added 2 tests and **correctly dismissed a false positive** (asserting `OrderCreatedEvent` is in `DomainEvents` would always fail — `CollectAndClear()` runs before `SaveChanges`). Also fixed a **pre-existing `OrderTrackingMapper` bug**.

### 2.4 `implement-us-fp-018-frontend` (5 agents, ~511k tok)
Same shape (Build → 3-lens Review → Repair), same single-threaded-writing discipline. The Build agent wrote 4 test files; **review caught coverage holes** and repair closed several (added the `PosOrderConfirmation` spec, made the AC2 test assert the *correct* product's modifiers, added combo-re-add and exact-key payload assertions, added MSW payload validation) and fixed a genuine **React Router anti-pattern** (render-time `navigate()` → `useEffect`).

### 2.5 `review-us-fp-018` (5 agents, ~524k tok) — final cross-cutting pass
4 parallel lenses over the whole feature (e2e+contract · security/auth · AC+tests · quality/reuse) → **barrier** → 1 close+repair agent. Per D7 it wrote the two mandated tests — **`PosModifierFlow.test.tsx` (t-16, AC5 end-to-end UI modifier flow)** and **`PosDashboardGuard.test.tsx` (t-17, AC4 real-`Dashboard` guard)** — and **principled-dismissed** the "remove redundant validation" suggestions (kept DDD defense-in-depth), deferring shared-code refactors as documented follow-ups.

### 2.6 Independent verification (Claude, outside the workflows) — where it mattered most
After **every** implement/review workflow, Claude re-ran the real gates rather than trusting self-reports. This was not ceremony:
- **Backend phase:** the LSP flagged the new test file as uncompilable; a real `dotnet build` proved it compiled (0 errors) — the LSP was **stale** (unrestored `.csproj` refs).
- **Review phase:** the workflow self-reported "typecheck 0 errors," but an independent `tsc --noEmit` caught a real **`TS6133` unused-variable error** (`addItemToOrder`) in the generated `PosDashboardGuard.test.tsx`. Vitest had passed because esbuild strips types and does not enforce `tsc`. Claude removed the dead helper; typecheck then passed. **Lesson: a workflow agent's claim of "green" is only trustworthy for gates it actually ran — independent gate-running is essential.**
- A reported `MSB3492` solution-build file-lock turned out **transient** (a process holding `obj` files during concurrent builds); it did not recur on a clean re-run.

---

## 3. Final state — verified green

- **Backend:** `dotnet build` → 0 errors; **268 TUnit unit tests** pass (incl. the new in-store endpoint/validator tests) + **6 integration tests** (real SQL via Testcontainers). Commit `1ddb0a4`.
- **Frontend:** `tsc --noEmit` → 0 errors; **255 Vitest tests across 52 files** pass (incl. 7 POS spec files). Commit `9589791` + a pending commit for the review-pass tests/fixes.
- **API contract:** `POST /api/brands/{brandSlug}/shops/{shopId}/orders/in-store` — request carries `tableNumber` (required for eat-in); server forces `CashAtPickup` and sets `CreatedByStaffId` from the token; response returns both new fields. Public endpoint untouched.
- **ACs:** AC1–AC6 each covered by implementation **and** test (AC5's UI modifier path and AC4's real-`Dashboard` guard closed in the review pass).

---

## 4. Compromises, open questions, and follow-ups

**Closed during the run:** R1 (AC5 end-to-end UI modifier flow) and R2 (AC4 real-`Dashboard` guard) — both now have dedicated tests.

**Documented follow-ups (deliberately out of scope):**
- **Staff-id claim name** — code uses fallback `NameIdentifier → "sub" → "userId"`; confirm against what the BFF actually forwards in a running environment.
- **Role-gated HTTP backend test** — integration tests drive the service via DI (Testing env uses JWT Bearer with no mock tokens); `Roles("CounterStaff")` is proven by an anonymous→401 HTTP test + the `Configure()` declaration, not a role-bearing HTTP test.
- **Touch-target / tablet-viewport tests (AC1/AC6)** and **SignalR fan-out (AC5)** need Playwright / a running hub — not expressible in JSDOM/Vitest; deferred.
- **Refactor duplication:** `PosModifierModal` vs storefront `ModifierModal`, `PosOrderContext` vs `CartContext` reducer, `OrderTrackingMapper` vs `OrderService` private mapper, and the two FluentValidation validators — all candidates for shared extraction, deferred as separate stories.
- **Known limitation:** a combo is a **single order line** (no per-component customization) — matches today's order flow.
- POS order state is **transient per-terminal** (lost on reload, intentional).

---

## 5. Process note for the comparison

`/workflows` was a strong fit for the **read / design / review** phases — genuine parallel fan-out with barriers placed only where cross-item synthesis required them, and adversarial critics that twice changed the outcome before it could ship (the design critic adding the `OrderResponse` contract task; the implement reviewer de-leaking `CreatedByStaffId`). Its main limit showed at the **writing** step: parallel writers across a coupled contract are counterproductive, so the high-value pattern was *single-threaded writing + fan-out verification*. Two recurring caveats: workflow agents' **self-reported "green" was not always real** (a `tsc` error slipped through a Vitest-only check), and **stale tooling/transient build locks** produced false alarms — both caught only by Claude independently re-running the actual build/typecheck/test gates after each phase.
