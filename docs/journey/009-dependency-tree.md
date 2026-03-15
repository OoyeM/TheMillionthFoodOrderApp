# 009 — Dependency Tree & Parallel Development Plan

**Date:** 2026-03-15

## What Was Done

Analysed all 70 user stories from the Frietjes Platform user stories document to build a dependency tree that enables maximum parallel development across worktrees.

### Dependency Analysis

Mapped every US-FP story to its prerequisites and grouped them into 5 development layers with 13 parallel streams:

- **Layer 0 — Foundation (sequential):** API backbone, platform admin, brands, DB provisioning, data isolation, shops. Must be done in order since each depends on the previous.
- **Layer 1 — Core Domains (4 parallel streams):** Products, Auth/Staff, Shop Configuration, Branding/i18n. All independent once shops exist.
- **Layer 2 — Ordering Core:** VAT, real-time infra, online ordering, payments, guest/POS ordering. The convergence point where Streams A + C merge.
- **Layer 3 — Post-Ordering (5 parallel streams):** Kitchen/fulfillment, customer experience, shop product management, loyalty/promotions, reporting. All independent.
- **Layer 4 — Advanced (4 parallel streams):** Receipts, offline mode, PWA, staff scheduling. All independent.
- **Layer 5 — Polish:** Platform dashboard, inventory signals, guest checkout refinement.

### Progress Tracking

Added status checkmarks to every story based on codebase analysis:

| Status | Count |
|--------|-------|
| Done | 1 (US-FP-069: REST API backbone) |
| Partial | 6 (001, 003, 004, 032, 037, 061, 070) |
| Not Started | 63 |

### Worktree Strategy

Mapped the 13 streams to a 4-worktree rotation plan. Each layer phase assigns one stream per worktree, with the critical path (`001 → 002 → 005 → 014 → 016`) getting priority.

## Key Takeaways

1. The critical path runs through brand → shop → product → menu → ordering. Everything downstream (kitchen, receipts, loyalty, reporting) is blocked until ordering works.
2. Maximum parallelism (4-5 streams) is achievable from Layer 1 onward, matching the 4-worktree setup.
3. Layer 3 has the most parallel streams (5) — this is where the worktree approach pays off most.
4. Auth/Staff (Stream B) and Branding (Stream D) are fully independent from Products (Stream A), making them good candidates for parallel work during Layer 1.

## Files Changed

- `docs/dependency-tree.md` — new: full dependency tree with progress tracking, visual summary, and worktree strategy
