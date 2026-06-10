# US-FP-019 ultracode session — cost & outcome record (2026-06-10)

Ultracode (full multi-agent orchestration) re-run of US-FP-019 "Select time slot at
checkout", for comparison against the baseline run recorded in
`us-fp-019-baseline.md` (branch `feat/us-fp-019-time-slot-checkout`, PR #138, $51.32).
Result: PR #139, commits `dd2fa28` (feature) + `435dc21` (bonus fix), branched from
`chore/frontend-create-page-dedup` (d1cd886) — same base as the baseline ✓.

## Approach used (ultracode)

Main conversation orchestrating three Workflow runs plus main-loop glue:

1. **Explore workflow** (7 agents) — 6 parallel Explore readers over subsystems
   (config exposure, checkout frontend, order backend, kitchen display, hours/wait-time,
   tests/plan conventions) + 1 completeness critic that answered the gaps itself
2. **Main loop** — authored the plan from the structured findings
3. **Plan-review workflow** (3 agents) — adversarial reviewers (backend correctness,
   frontend correctness, product/AC completeness) against the actual code; main loop
   folded 4 substantive findings + 1 scope cut back into the plan **before approval**
4. **Implement workflow** (6 agents) — 2 general-implementers on disjoint tracks
   (backend §1–§5, frontend §6–§10) in parallel → 2 independent verifiers re-running
   all builds/suites from scratch → 2 adversarial diff reviewers (plan conformance,
   bug hunt)
5. **Main loop** — applied the 3 confirmed review defects + regression tests, re-ran
   all suites, **live happy-path verification in Chrome** (found a shipped pre-existing
   bug → EF fix + migration + regression test), 2 commits, PR

Key structural differences vs baseline: planning itself was fanned out (explore +
gap-critic), the plan got an adversarial review **before** implementation,
implementation was split across 2 parallel implementers, verification was done by
independent agents rather than trusted from the implementer, and a live browser
verification pass was added after the automated suites.

## Token usage (as reported by the harness, subagents only)

| Workflow / agents | Agents | Tokens | Tool uses | Wall duration |
|---|---:|---:|---:|---:|
| Explore (6 readers + gap critic) | 7 | 628,761 | 259 | 13m 38s |
| Plan review (3 adversarial lenses) | 3 | 403,750 | 123 | 11m 19s |
| Implement (2 impl + 2 verify + 2 review) | 6 | 705,796 | 279 | 30m 25s |
| **Subagent total** | **16** | **1,738,307** | **661** | sequential workflows |

Comparability note: the baseline's 717,267 subagent tokens **excluded** its Plan
agent; this run's planning was subagent-based, so the totals are not
phase-equivalent. Main-conversation tokens (plan authoring, review-fix edits, live
browser verification, EF bug diagnosis/fix, commit/PR) are NOT included — not
visible from inside the session. Main-loop share was substantially larger than the
baseline's because the live verification + bonus-bug fix were done inline.

## Authoritative session totals (from /usage, recorded 2026-06-10)

- **Total cost: $98.64** (baseline: $51.32 → **1.92×**)
- Total duration (API): 1h 17m 11s (baseline: 1h 21m 17s — *less* API time than baseline)
- Total duration (wall): 3h 7m 22s (includes the PC-reboot/approval gap between
  planning and implementation, plus this benchmark-writing tail)
- Code changes (as counted by the session): 2,796 lines added, 123 removed

Usage by model:

| Model | Input | Output | Cache read | Cache write | Cost |
|---|---:|---:|---:|---:|---:|
| claude-fable-5 | 90.7k | 188.1k | 58.6m | 1.9m | $92.83 |
| claude-sonnet-4-6 | 699 | 68.9k | 12.5m | 272.7k | $5.80 |

Cost-shape note: fable **output** tokens were *lower* than the baseline's (188.1k vs
218.5k) — the 1.9× cost comes almost entirely from **cache reads** (58.6m vs 21.3m),
i.e. many agents re-reading large contexts plus the browser-MCP results staying in
the main context during live verification.

Session-attributed characteristics (from /usage, last-24h heuristics — caveat: the
24h window also contains the baseline session, so these mix both runs):
- 100% of usage from subagent-heavy sessions; 59% at >150k context
- Subagent share: workflow-subagent 15%, code-review 7%, general-implementer 6%,
  git-story 5%
- Skill share: /verify 18%, /frontend-react 4%, /evaluate-claude-md 3%, /git-story 2%
- MCP: claude-in-chrome 20% — the live-verification pass is a large cost slice, and
  also the phase that caught the shipped EF bug

## Scope delivered

- `dd2fa28` feat: 31 files, 4,031 insertions, 66 deletions
- `435dc21` fix: 6 files, 1,312 insertions, 12 deletions (bonus — see below)
- Insertion counts include the committed plan doc (~300 lines) and 2 migrations with
  generated Designer/snapshot files, same as the baseline's counts did
- Backend: 1 new domain service (`TimeSlotCalculator`: generation **and** create-time
  validation), Order + IOrderRepository extensions, new `TimeSlotService` + endpoint,
  server-side enforcement in OrderService (step 3c), migration `AddOrderTimeSlot`
- Frontend: timeSlots api module + `useTimeSlots` hook, checkout picker with
  slot-full recovery (refetch + auto-reset + cart preserved), place-in-line notice
  (AC5), confirmation-page ready time, i18n NL/FR/DE, MSW handlers
- Tests added: 28 unit (26 calculator incl. DST + sub-minute, 2 Order), 12
  integration (11 TimeSlotTests + 1 toggle-back regression), ~13 vitest scenarios
- Final state: 358 unit / 16 integration / 356 vitest green; build + lint clean
- **Live-verified end-to-end in Chrome** (picker, grey-out, race UX, kitchen badge,
  place-in-line) — the baseline run had no live verification pass

## Quality events (for comparing defect-catch rates)

**Caught at the plan stage** (before any code — phase the baseline didn't have):
1. Create-time validation hole: weekday-keyed opening hours would accept aligned
   slots on *future days* — same-local-day gate added to the plan
2. Plan's test-fixture impact list was factually wrong (named already-passing files,
   missed real breaks)
3. Closed-shop behavior contradicted the plan's own slot-generation rule
4. Delivery/EatIn order-type semantics unaddressed (picker label was pickup-specific)
5. Scope cut: storefront shop-DTO extension was redundant dual-sourcing — removed,
   deleting a backend DTO change + context plumbing + fixture churn

**Escaped the implementers, caught by the diff-review fan-out:**
1. i18n key mismatch — confirmation page rendered the raw key
   `storefront.confirmation.timeSlot` (locales had `storefront.order.timeSlot`)
2. Sub-minute capacity bypass — `IsValidSlotStart` ignored seconds, so crafted
   `17:15:30Z` values formed private capacity buckets, defeating
   `MaxOrdersPerInterval` entirely via the anonymous endpoint
3. Mid-session disable dead-end — stale selected slot resubmitted forever with no
   on-screen control to clear it

**Escaped every automated phase, caught only by live verification:**
4. **Pre-existing EF bug (shipped before this story):** `HasDefaultValue` on owned
   value-object bools + Added-entity sentinel semantics made "disable time slots"
   (US-FP-020) and "re-enable eat-in" (US-FP-066) silent no-ops, leaving rows in the
   domain-unrepresentable state `(enabled, null interval)` that 500'd the new
   endpoint. Fixed (migration + data heal + regression test + CLAUDE.md gotcha) in
   `435dc21`. **The baseline implementation sits on the same broken toggle** — its
   PR #138 would hit the same corrupt state the first time a shop disables slots.

**Defects remaining after the implementation phase, by the suites' own measure:** 0 —
both implementers handed over green builds/suites on first run (verified
independently), vs the baseline's 3 test failures debugged in the main loop. The
real post-implementation defect count was 3 (review) + 1 (live), i.e. the suites
alone were insufficient in both runs.

Known accepted gaps (identical to baseline, by design): capacity check-then-insert
race (overshoot-by-1), all orders consume capacity (no cancel flow), today-only
horizon, POS bypasses slots.

## Scope drift vs the approved plan

Minimal. Implementer-recorded deviations: ITimeSlotService co-located with its
implementation; UTC-label fallback on unknown TZ; confirmation row uses the existing
InfoCard label+value shape; slot-full error intercepted via try/catch rather than
mutation state. One out-of-plan addition, deliberately accepted: the `435dc21` EF
persistence fix (load-bearing for AC5; would otherwise block "disable time slots").

## Wall-clock phase estimates

- Explore workflow: ~14 min · plan authoring (main loop): ~10 min
- Plan-review workflow: ~11 min · plan revision: ~5 min
- (user gap: approval + PC reboot — excluded)
- Implement workflow (impl ∥ → verify → review): ~30 min
- Review fixes + regression tests + full re-verification: ~15 min
- Live verification incl. bonus-bug diagnosis, fix, migration, re-tests: ~35 min
- Commit + PR + docs: ~10 min
- **Active total ≈ 2h 10m** (vs baseline ~1h 35m wall) — the delta is mostly the live
  verification pass and the bonus bug it caught, which the baseline didn't attempt

## Comparison checklist (from the baseline doc)

- [x] Total subagent tokens + agent count: **1,738,307 / 16 agents** (vs 717,267 / 8,
      excl. baseline's unreported Plan agent)
- [x] `/usage` session total: **$98.64**, 1h 17m API / 3h 7m wall incl. reboot gap
      (baseline: $51.32, 1h 21m API / 1h 36m wall)
- [x] Wall-clock time to PR: ~2h 10m active (reboot gap excluded)
- [x] Defects remaining after implementation phase: 0 by the suites; 3 real (review)
      + 1 pre-existing (live verification)
- [x] Defects found by review phase(s): 5 at plan stage, 3 in code review, 1 live
- [x] Scope drift vs approved plan: minimal; one deliberate out-of-plan fix (435dc21)
- [x] Re-run setup honoured: branched fresh from d1cd886; issue #19 left open
      (PR #139 carries `Closes #19`; PRs #138/#139 must not both be merged as-is —
      they implement the same story with different schemas)
