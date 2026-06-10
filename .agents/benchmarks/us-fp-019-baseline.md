# US-FP-019 baseline session — cost & outcome record (2026-06-10)

Baseline run of US-FP-019 "Select time slot at checkout" for comparison against a
planned ultracode (full multi-agent orchestration) re-run of the same story.
Result: PR #138, commit `3df4a0e`, branched from `chore/frontend-create-page-dedup` (d1cd886).

## Approach used (baseline)

Single main conversation orchestrating individual subagents:
1. **Plan** agent (1×) — codebase exploration + full implementation plan
2. **general-implementer** agent (1×) — entire backend + frontend implementation
3. Main loop — verification, debugging 3 test failures, review fixes, commit, PR
4. **Review fan-out** (7× parallel finder agents) — high-effort /code-review; verification done inline by main loop instead of separate verifier agents

So the baseline was NOT purely single-agent: the review phase already fanned out.
The ultracode run differs mainly in orchestrating understand/design/implement/verify
phases through workflows too.

## Token usage (as reported by the harness, subagents only)

| Agent | Tokens | Tool uses | Duration |
|---|---:|---:|---:|
| Plan (us-fp-019 plan) | not reported | — | — |
| general-implementer (full story) | 141,407 | 181 | 24m 57s |
| Review finder: line-by-line | 94,154 | 21 | 5m 35s |
| Review finder: removed-behavior | 98,921 | 24 | 5m 54s |
| Review finder: cross-file tracer | 109,986 | 32 | 6m 46s |
| Review finder: reuse | 67,753 | 16 | 3m 05s |
| Review finder: simplification | 62,214 | 13 | 3m 16s |
| Review finder: efficiency | 68,122 | 13 | 3m 35s |
| Review finder: altitude | 74,710 | 17 | 3m 12s |
| **Subagent total (excl. Plan agent)** | **717,267** | **317** | finders ran in parallel (~7m wall) |

Main-conversation tokens (planning prompts, debugging, review verification, fixes,
commit/PR) are NOT included above — not visible from inside the session.

## Authoritative session totals (from /usage, recorded 2026-06-10)

- **Total cost: $51.32**
- Total duration (API): 1h 21m 17s
- Total duration (wall): 1h 36m 27s
- Code changes (as counted by the session): 2,689 lines added, 203 removed

Usage by model:

| Model | Input | Output | Cache read | Cache write | Cost |
|---|---:|---:|---:|---:|---:|
| claude-fable-5 | 134.0k | 218.5k | 21.3m | 837.7k | $44.07 |
| claude-sonnet-4-6 | 3.4k | 75.3k | 16.7m | 296.6k | $7.25 |
| claude-haiku-4-5 | 443 | 16 | 0 | 0 | $0.0005 |

Session-attributed characteristics (from /usage, last-24h heuristics):
- 95% of usage came from subagent-heavy sessions
- 33% of usage was at >150k context
- Subagent share: code-review 27%, general-implementer 14%, git-story 8%
- Skill share: /code-review 5%, /git-story 3%, /commit 1%

Note: the session ran ~15 min past the PR (baseline doc writing + this recording),
but virtually all cost is from the story itself.

## Scope delivered

- 47 files changed, 3,658 insertions, 62 deletions (single feature commit)
- Backend: 2 domain changes + 1 new domain service, 1 EF migration, 1 new endpoint,
  1 new app service, 1 typed exception, server-side enforcement in OrderService
- Frontend: 3 new modules (picker, hook, time utils), checkout integration with 409
  recovery, kitchen/ticket/confirmation display, i18n NL/FR/DE, MSW handlers
- Tests added: ~9 unit (TimeSlotGenerator) + Order invariants, 11 integration
  (2 new classes), ~15 vitest (picker/utils + updated fixtures)
- Final state: 347 unit / 11 integration / 357 vitest green; build + type-check clean

## Quality events (for comparing defect-catch rates)

Defects that escaped the implementer agent and were caught later:
1. **Infinite loop / DateTime overflow** in TimeSlotGenerator when slot grid touched
   midnight (TimeOnly wraparound) — caught by integration tests the implementer never
   ran (Docker was down during its session)
2. **In-store test used HTTP endpoint without CounterStaff auth** → 401 — caught by
   integration test run, fixed via service-layer DI pattern
3. **DST ambiguous-time offset inverted** (picked DST offset, comment claimed standard)
   — caught by review fan-out
4. **Stale-slot rejection = unrecoverable 400** (frontend only handled 409) — caught by
   review fan-out; fixed with typed TimeSlotUnavailableException → 409
5. **Silent ASAP downgrade** when selected slot aged out — caught by review fan-out
6. **InvalidTimeZoneException uncaught** → 500 on public endpoint — caught by review
7. Minor: weakened test assertion, per-call Intl.DateTimeFormat, duplicated formatter,
   query-key literal duplication, polling behind payment screen — caught by review

Known accepted gaps (same for both runs, by design): capacity check-then-insert race,
cancelled orders consume capacity, today-only horizon, POS ASAP-only.

## Wall-clock phase estimates (from tool durations)

- Planning (Plan agent): ~5–10 min
- Implementation (implementer agent): ~25 min
- Verification + debugging (integration runs at 3m29s/22s/25s/21s/17s/14s, unit runs,
  frontend test+build ×2): ~25 min
- Review fan-out: ~7 min parallel + fixes ~20 min
- Commit + PR: ~5 min

## Comparison checklist for the ultracode re-run

Record the same for the re-run:
- [ ] Total subagent tokens + agent count
- [ ] `/usage` session total (baseline: **$51.32**, 1h 21m API / 1h 36m wall)
- [ ] Wall-clock time to PR
- [ ] Defects remaining after implementation phase (run the same integration/unit/vitest suites)
- [ ] Defects found by review phase(s)
- [ ] Scope drift vs the approved plan (.agents/plans/us-fp-019-time-slot-checkout.md)
- [ ] Re-run setup: branch fresh from d1cd886, revert/ignore this baseline's branch,
      keep issue #19 open (or compare against PR #138 diff instead of merging twice)
