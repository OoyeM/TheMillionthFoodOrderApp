# Frontend Cleanup — Design Spec

**Date:** 2026-04-30
**Author:** Matthias Ooye
**Status:** Draft (awaiting review)

## Context

A `fallow` analysis of `src/frontend` (108 files, 1,406 functions, 17,230 LOC) produced a health grade of **B (83.2)** with three meaningful issue clusters:

- **39 dead-code findings** — 8 unused files, 20 unused exports, 8 unused types, 2 unused devDependencies, 1 unresolved test import.
- **35.9% code duplication** — concentrated in tests (auth-expired event harness + MSW response handlers repeated across `client.test.ts`, `brands.test.ts`, `BffAuthProvider.test.tsx`, `useSessionKeepalive.test.tsx`).
- **75 functions above complexity threshold (28 critical)** — clustered in 9 admin Edit/Create pages: ComboProductEdit (cyclomatic 39, 750 LOC), MenuCategoryEdit (36 / 503), ProductEdit (33 / 652), and six others.

This spec covers cleanup of all three clusters in a single PR with twelve focused commits.

## Goals

- Health score from **B (83.2) → A (≥92)**.
- Zero error-severity dead-code findings.
- Duplication from **35.9% → < 15%**.
- 9 admin pages from **cyclomatic 19–39** to **≤10**, LOC **≤200** each.
- Behavior of every admin page unchanged — pure refactor.

## Non-Goals

- Backend changes (this PR is frontend-only).
- Refactoring the remaining 19 critical-complexity functions outside the 9 admin pages — most are tests or one-off helpers; defer.
- The 8-line clone shared between `api/menuCategories.ts`, `api/modifierGroups.ts`, and `api/products.ts` (production-code; defer to a separate cleanup).
- Adopting react-hook-form on storefront/POS pages — out of scope.

## Branching

- Branch: `chore/frontend-cleanup`, cut from `main`.
- After branch creation, cherry-pick `942fc2b` (latest local commit on `tests/migrate-to-tunit`) so the cleanup branch inherits the `.gitignore` / `.claude/settings.json` / `.mcp.json` scaffolding from that wip commit.
- One PR back to `main` containing twelve commits (below).

## Commit Plan

### Commit 1 — `chore(fe): remove unused devDeps, fix broken test import, untrack fallow caches`

- Remove `@typescript-eslint/eslint-plugin` and `@typescript-eslint/parser` from `package.json` devDependencies; run `pnpm install` to update `pnpm-lock.yaml`. Verify before removing that `eslint.config.js` does not reference them — if it does, switch to the modern `typescript-eslint` umbrella package.
- Fix `src/App.test.tsx:7` import: change `../i18n/config` to `./i18n/config` (verify by reading the file before editing).
- Add `.fallow/` to root `.gitignore` (matches at any depth, no need for a frontend-specific entry).
- `git rm --cached .fallow/cache.bin src/frontend/.fallow/cache.bin src/frontend/.fallow/churn.bin` — untrack the binaries the cherry-picked wip commit accidentally committed.
- Verify: `pnpm tsc --noEmit && pnpm test`.

### Commit 2 — `chore(fe): mark deferred-feature exports as @expected-unused`

Suppression mechanism: `/** @expected-unused — <reason> */` JSDoc tags inline with each export, OR `// fallow-ignore-file unused-export` at the top of files where every export is unused. Fallow tracks staleness automatically — when an export gets imported, the suppression appears in `stale_suppressions` so we know to remove it.

Story mapping (verified against `docs/dependency-tree.md`):

| Files / exports | Story | Reason |
|---|---|---|
| `src/api/signalr.ts`, `useSignalR.ts`, `useOrderUpdates.ts`, `getOrderHubConnection`, `resetOrderHubConnection` | US-FP-068 | Real-time order updates infrastructure |
| `src/features/pos/routes.tsx` | US-FP-018 | POS routes — wired in POS ordering story |
| `src/features/storefront/routes.tsx` | US-FP-016, US-FP-017 | Storefront routes — wired in online + guest ordering |
| `src/auth/index.ts` | — | Barrel export for `src/auth`, kept for downstream consumers |
| `src/test/setup.ts`, `src/test/testUtils.tsx` | — | Test infrastructure used once suites converge on shared setup |
| `*Keys` exports (12 of them) | various | TanStack Query key constants used by future mutations for cache invalidation |
| `useShopStaff`, `useShopStatus` | US-FP-024, US-FP-007 | Shop-level hooks consumed by upcoming features |
| `useAssignProductToCategory` | US-FP-022 | Menu category assignment, deferred |
| `bffClient` | — | Lower-level BFF client — public API surface for future consumers |
| 8 unused types | various | DTO/response shapes; tag at the type alias |

After this commit, `fallow dead-code --format json --quiet` shows **0 unused-files / unused-exports / unused-types** findings.

### Commit 3 — `refactor(fe): extract shared MSW + auth-expired test helpers`

Two new files in `src/test/`:

**`mswHelpers.ts`** — `mockEndpoint(method, path, status, body?)` returns an MSW handler:
```ts
server.use(mockEndpoint('get', '/api/brands', 401));
```

**`authExpiredHarness.ts`** — `expectAuthSessionExpired(action: () => Promise<unknown>)`:
- Adds `auth:session-expired` window listener
- Awaits the action (rejection acceptable)
- Removes listener
- Asserts listener was called once

Replaces clones at: `api/__tests__/brands.test.ts:23-32`, `api/__tests__/client.test.ts:67-76 + 88-102 + 109-123 + 128-142`, `auth/__tests__/BffAuthProvider.test.tsx:62-73 + 95-107`, `auth/__tests__/useSessionKeepalive.test.tsx` (four clone pairs at 33-45/73-88, 41-52/61-72, 52-61/72-81, 78-95/108-121).

Validation: `pnpm test` passes with identical assertion count. Re-run `fallow dupes` — duplication should drop from 35.9% to ~12-15%.

### Commit 4 — `refactor(fe): extract useResourceForm hook and form primitives`

Install dependencies (latest majors at install time; verify via `pnpm view <pkg> version`):
```bash
pnpm add react-hook-form zod @hookform/resolvers
```

Create `src/features/admin/forms/`:

**`useResourceForm<TResource, TUpdate>`** — generic hook that bundles:
- RHF's `useForm({ resolver: zodResolver(schema) })`
- TanStack Query's `useQuery` for initial data → calls `form.reset(data)` once loaded
- TanStack Query's `useMutation` for submit
- Cache invalidation on success
- Optional `onSuccess` callback (typically navigation)

API:
```ts
const { form, submit, isSubmitting, isFetching, error } = useResourceForm({
  queryKey: productKeys.detail(id),
  fetch: () => productsApi.get(id),
  update: (patch) => productsApi.update(id, patch),
  invalidate: [productKeys.lists()],
  onSuccess: () => navigate('../'),
  schema: productEditSchema,
});
```

**`<FormSection>`** — collapsible labelled card for grouping fields. Props: `title`, `description?`, `defaultOpen?`, children.

**`<NestedItemList>`** — wraps RHF's `useFieldArray`, renders rows with add/remove/reorder controls. Generic over the row component. Used by ComboProductEdit (combo items), ModifierGroupEdit (options), and ShopOpeningHours (slots).

**Tests** in `src/features/admin/forms/__tests__/`:
- `useResourceForm.test.tsx` — happy path, validation failure, mutation error, cache invalidation, navigation on success.
- `FormSection.test.tsx` — render, collapse/expand.
- `NestedItemList.test.tsx` — add, remove, reorder.

This commit is purely additive — no consumer changes. Verify: `pnpm tsc --noEmit && pnpm test && pnpm build`.

### Commits 5–12 — Per-page refactors

| # | Commit subject | Page(s) | Current LOC / cyclo |
|---|---|---|---|
| 5 | `refactor(fe): apply useResourceForm to ComboProductEdit` | `features/admin/pages/ComboProductEdit.tsx` | 750 / 39 |
| 6 | `refactor(fe): apply useResourceForm to MenuCategoryEdit` | `features/admin/pages/MenuCategoryEdit.tsx` | 503 / 36 |
| 7 | `refactor(fe): apply useResourceForm to ProductEdit` | `features/admin/pages/ProductEdit.tsx` | 652 / 33 |
| 8 | `refactor(fe): apply useResourceForm to BrandEdit` | `features/admin/pages/BrandEdit.tsx` | 385 / 24 |
| 9 | `refactor(fe): apply useResourceForm to BrandTheming` | `features/admin/pages/BrandTheming.tsx` | 402 / 23 |
| 10 | `refactor(fe): apply useResourceForm to ShopEdit` | `features/admin/pages/ShopEdit.tsx` | 401 / 21 |
| 11 | `refactor(fe): apply useResourceForm to Create pages` | `ComboProductCreate`, `ProductCreate`, `ShopCreate` | combined ~1100 / 14-20 |
| 12 | `refactor(fe): apply useResourceForm to tail pages` | `ModifierGroupEdit`, `ShopOrderLifecycle` | ~900 / 15-19 |

**Per-commit pattern:**

1. Create `<page>Schema.ts` next to the page — single zod schema, source of truth for validation and TS shape.
2. Replace local `useState` form state with `useForm({ resolver: zodResolver(schema) })`.
3. Replace direct `useQuery` + `useMutation` calls with `useResourceForm` wiring.
4. Wrap field groups in `<FormSection>`. Replace nested array UIs with `<NestedItemList>` via `useFieldArray`.
5. Behavior must be identical — same submit semantics, same query invalidations, same redirects, same error toasts.

**Per-commit validation:**
- Existing tests for that page must pass unchanged. If a test breaks because it asserted on internal state shape (rather than observable behavior), fix the test to assert on observable behavior and call it out in the commit message.
- `pnpm tsc --noEmit && pnpm build` clean.
- Manual smoke: load page → edit → submit → list refreshes; trigger validation error and confirm error renders correctly.
- Target metrics post-commit: cyclomatic ≤10, cognitive ≤8, LOC ≤200.

**Rollback:** each per-page commit is independently revertable. Shared primitives (commit 4) keep working without consumers. If a per-page refactor regresses, `git revert <commit>` and ship the rest.

## Final Verification (pre-PR)

```bash
fallow dead-code --format json --quiet      # 0 error-severity issues
fallow health --format json --quiet --score # score ≥ 92 (A grade)
fallow dupes --format json --quiet          # < 15%
pnpm tsc --noEmit && pnpm test && pnpm build && pnpm test:e2e
```

Capture before/after metrics in the PR description.

## Risks and Mitigations

| Risk | Mitigation |
|---|---|
| Admin-page refactors silently change form behavior | Existing tests run unchanged per commit; observable-behavior smoke test before each commit lands |
| RHF + TanStack Query state divergence (form fields out of sync with cached data) | `useResourceForm` calls `form.reset(data)` exactly once on first successful fetch; subsequent invalidations don't re-reset unless the mutation succeeds |
| zod schema drift from TS types in `src/types/common.ts` | Each page-level schema infers its own type via `z.infer` — page-local source of truth; if a mismatch with `common.ts` appears, treat as a bug and reconcile in the same commit |
| Removing `@typescript-eslint/*` breaks lint | Verify `eslint.config.js` references and switch to the modern `typescript-eslint` package if needed |
| Cherry-picked wip commit `942fc2b` includes content unrelated to cleanup (e.g., `.claude/settings.json` changes) | Acceptable — it's small dev-environment scaffolding the cleanup branch genuinely needs; document in the PR |

## Open Items (resolve at execution time)

- Confirm the exact zod and react-hook-form major versions at `pnpm add` time.
- Confirm whether `eslint.config.js` references the soon-to-be-removed `@typescript-eslint/*` packages.
- Confirm exact import path needed for `App.test.tsx:7` once the file is read.
