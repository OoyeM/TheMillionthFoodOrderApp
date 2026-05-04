# Frontend Cleanup Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Clean up the React frontend in `src/frontend/` — eliminate dead-code findings via deferred-feature suppressions, deduplicate test infrastructure, and refactor the 9 highest-complexity admin Edit/Create pages onto a shared `react-hook-form` + `zod` pattern.

**Architecture:** Twelve focused commits on a single `chore/frontend-cleanup` branch. Each per-page commit is independently revertable thanks to shared primitives that ship in commit 4.

**Tech Stack:** React 18, TypeScript 5, Vite, TanStack Query 5, react-hook-form 7+, zod 3.x or 4.x (latest), `@hookform/resolvers/zod`, MSW 2, Vitest, fallow CLI 2.56+.

**Spec:** `docs/superpowers/specs/2026-04-30-frontend-cleanup-design.md`.

---

## File Structure Map

### Files created

- `src/frontend/.fallowrc.json` — fallow config for false-positive suppressions
- `src/frontend/src/test/mswHelpers.ts` — `mockEndpoint(method, path, status, body?)` MSW handler factory
- `src/frontend/src/test/authExpiredHarness.ts` — `expectAuthSessionExpired(action)` helper
- `src/frontend/src/test/eventListenerHarness.ts` — `expectWindowEvent(eventName, action)` generic event-assertion helper (used for `auth:access-denied` plus the negative-case 401-vs-403 assertions)
- `src/frontend/src/features/admin/forms/useResourceForm.ts` — generic form hook combining RHF + TanStack Query
- `src/frontend/src/features/admin/forms/FormSection.tsx` — collapsible section primitive
- `src/frontend/src/features/admin/forms/NestedItemList.tsx` — `useFieldArray` wrapper
- `src/frontend/src/features/admin/forms/__tests__/useResourceForm.test.tsx`
- `src/frontend/src/features/admin/forms/__tests__/FormSection.test.tsx`
- `src/frontend/src/features/admin/forms/__tests__/NestedItemList.test.tsx`
- `src/frontend/src/features/admin/pages/schemas/<page>Schema.ts` (one per refactored page, 9 total)

### Files modified

- `.gitignore` (root) — add `.fallow/`
- `src/frontend/package.json` — remove 2 unused devDeps; add `react-hook-form`, `zod`, `@hookform/resolvers`
- `src/frontend/pnpm-lock.yaml` (regenerated)
- `src/frontend/src/App.test.tsx` — fix `../i18n/config` → `./i18n/config`
- 8 unused-file headers — add `// fallow-ignore-file unused-file` (setup.ts, testUtils.tsx) or `@expected-unused` JSDoc per export
- 20 unused-export sites + 8 unused-type sites — add `@expected-unused` JSDoc tags
- `src/frontend/src/api/__tests__/brands.test.ts` — adopt helpers
- `src/frontend/src/api/__tests__/client.test.ts` — adopt helpers
- `src/frontend/src/auth/__tests__/BffAuthProvider.test.tsx` — adopt helpers
- `src/frontend/src/auth/__tests__/useSessionKeepalive.test.tsx` — adopt helpers
- 9 admin page files (per the table in tasks 5–12)

### Files untracked from git

- `.fallow/cache.bin` (root)
- `src/frontend/.fallow/cache.bin`
- `src/frontend/.fallow/churn.bin`

---

## Pre-flight: branch setup

### Task 0: Create the cleanup branch

**Files:** none (git operations only)

- [ ] **Step 1: Verify clean working tree on `tests/migrate-to-tunit`**

```bash
cd C:/Users/MatthiasOoye/Git/TheMillionthFoodOrderApp
git status
```

Expected: only the unrelated `M src/frontend/tsconfig.app.json` change. If anything else, stop and ask the user.

- [ ] **Step 2: Stash any modifications and switch to main**

```bash
git stash push -m "frontend-cleanup-temp" --include-untracked
git checkout main
git pull --ff-only
```

Expected: clean fast-forward on `main`. If `git pull` is non-FF, stop.

- [ ] **Step 3: Create the cleanup branch**

```bash
git checkout -b chore/frontend-cleanup
```

- [ ] **Step 4: Cherry-pick `942fc2b` (wip — gitignore + .claude + .mcp scaffolding) and `bed2a44` (the design spec)**

```bash
git cherry-pick 942fc2b bed2a44
```

Expected: both commits land cleanly. If conflicts, abort and reconcile.

- [ ] **Step 5: Verify**

```bash
git log --oneline -5
```

Expected output (top 3):
```
<sha> docs: add frontend cleanup design spec
<sha> wip
<sha> <main-tip>
```

---

## Commit 1 — devDeps, broken import, untrack fallow caches

### Task 1: Remove unused devDeps + fix App.test.tsx + clean up .fallow tracking

**Files:**
- Modify: `src/frontend/package.json`
- Modify: `src/frontend/pnpm-lock.yaml` (regenerated)
- Modify: `src/frontend/src/App.test.tsx:7`
- Modify: `.gitignore` (root)
- Untrack: `.fallow/cache.bin`, `src/frontend/.fallow/cache.bin`, `src/frontend/.fallow/churn.bin`

- [ ] **Step 1: Confirm the unused devDeps are not referenced**

```bash
cd C:/Users/MatthiasOoye/Git/TheMillionthFoodOrderApp
grep -rn "@typescript-eslint/eslint-plugin\|@typescript-eslint/parser" src/frontend --include="*.ts" --include="*.tsx" --include="*.js" --include="*.json" --include="*.cjs" --include="*.mjs"
```

Expected: only matches in `src/frontend/package.json`. (The actual ESLint config at `src/frontend/eslint.config.js` uses `typescript-eslint` — the umbrella package, which is a different dependency that stays.)

If a match shows up in `eslint.config.js` or any other source file, **stop** and report — the spec assumed they were unreferenced.

- [ ] **Step 2: Remove the two devDeps from `package.json`**

In `src/frontend/package.json`, delete lines 36–37:

```diff
-    "@typescript-eslint/eslint-plugin": "^8.19.1",
-    "@typescript-eslint/parser": "^8.19.1",
```

(Lines 36 and 37 in the current file. Verify by reading the file before editing.)

- [ ] **Step 3: Update lockfile**

```bash
cd C:/Users/MatthiasOoye/Git/TheMillionthFoodOrderApp/src/frontend
pnpm install
```

Expected: lockfile updates, removes the two packages and their unique transitive deps.

- [ ] **Step 4: Verify lint still works**

```bash
pnpm lint
```

Expected: lint runs to completion (warnings/errors about source code OK, but no "Cannot find module" or "plugin not found" errors related to the removed packages).

- [ ] **Step 5: Fix the broken test import in `src/App.test.tsx`**

Change line 7 from:

```ts
import '../i18n/config';
```

to:

```ts
import './i18n/config';
```

(Reasoning: `App.test.tsx` lives at `src/App.test.tsx`; `i18n/config.ts` lives at `src/i18n/config.ts`. The relative path is `./i18n/config`.)

- [ ] **Step 6: Add `.fallow/` to root `.gitignore`**

Open `C:/Users/MatthiasOoye/Git/TheMillionthFoodOrderApp/.gitignore` and append:

```gitignore

# Fallow caches (any depth)
.fallow/
```

A single root-level entry matches `.fallow/` at any nested level — no per-workspace entry needed.

- [ ] **Step 7: Untrack the fallow cache binaries that the wip commit accidentally committed**

```bash
cd C:/Users/MatthiasOoye/Git/TheMillionthFoodOrderApp
git rm --cached .fallow/cache.bin src/frontend/.fallow/cache.bin src/frontend/.fallow/churn.bin
```

Expected: three "rm" lines printed.

- [ ] **Step 8: Verify type-check + tests pass**

```bash
cd src/frontend
pnpm type-check && pnpm test
```

Expected: both succeed. The `App.test.tsx` import fix should make the previously-failing import resolve.

- [ ] **Step 9: Commit**

```bash
cd C:/Users/MatthiasOoye/Git/TheMillionthFoodOrderApp
git add .gitignore src/frontend/package.json src/frontend/pnpm-lock.yaml src/frontend/src/App.test.tsx
git commit -m "$(cat <<'EOF'
chore(fe): remove unused devDeps, fix broken test import, untrack fallow caches

- Remove @typescript-eslint/eslint-plugin and @typescript-eslint/parser
  from devDependencies; eslint.config.js uses the typescript-eslint
  umbrella package instead.
- Fix App.test.tsx import: '../i18n/config' -> './i18n/config'.
- Add .fallow/ to root .gitignore (covers nested workspace caches).
- Untrack accidentally-committed fallow cache binaries.

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>
EOF
)"
```

---

## Commit 2 — mark deferred-feature exports as @expected-unused

### Task 2: Suppress dead-code findings for deferred features

This commit applies fallow suppressions to all 39 dead-code findings the user wants to keep. Approach:
- **Files with no exports** (`src/test/setup.ts`) → file-level `// fallow-ignore-file unused-file` comment.
- **Files where every export is unused** → file-level `// fallow-ignore-file unused-export` comment with a one-line reason at the top.
- **Files with mixed used/unused exports** → per-export `/** @expected-unused — <reason> */` JSDoc on each unused symbol.
- **Setup.ts false positive** (loaded via `vite.config.ts setupFiles`) → handled with `// fallow-ignore-file unused-file`. (Alternative: add to `.fallowrc.json` `dynamicallyLoaded`, but inline is closer to the code.)

**Files:**
- Modify: `src/frontend/src/api/signalr.ts` (file-level)
- Modify: `src/frontend/src/api/useSignalR.ts` (file-level)
- Modify: `src/frontend/src/api/useOrderUpdates.ts` (file-level)
- Modify: `src/frontend/src/auth/index.ts` (file-level)
- Modify: `src/frontend/src/features/pos/routes.tsx` (file-level)
- Modify: `src/frontend/src/features/storefront/routes.tsx` (file-level)
- Modify: `src/frontend/src/test/setup.ts` (file-level — false positive)
- Modify: `src/frontend/src/test/testUtils.tsx` (file-level)
- Modify: 12 `*Keys` query-key exports in `src/frontend/src/api/*.ts` and `src/frontend/src/features/admin/hooks/*.ts` (per-export)
- Modify: 4 hook exports (`useShopStaff`, `useShopStatus`, `useAssignProductToCategory`, `bffClient`) (per-export)
- Modify: 8 type aliases (per-export)

- [ ] **Step 1: Generate authoritative list of dead-code findings to suppress**

```bash
cd C:/Users/MatthiasOoye/Git/TheMillionthFoodOrderApp/src/frontend
fallow dead-code --format json --quiet > /tmp/fallow-deadcode-pre.json
```

Then enumerate findings:

```bash
python -c "
import json
with open('/tmp/fallow-deadcode-pre.json') as f:
    d = json.load(f)
print('UNUSED FILES:')
for x in d['unused_files']:
    print(' ', x.get('path'))
print('UNUSED EXPORTS:')
for x in d['unused_exports']:
    print(f\"  {x.get('file','?')}:{x.get('line','?')} -> {x.get('export_name','?')}\")
print('UNUSED TYPES:')
for x in d['unused_types']:
    print(f\"  {x.get('file','?')}:{x.get('line','?')} -> {x.get('type_name','?')}\")
print('UNRESOLVED IMPORTS:')
for x in d['unresolved_imports']:
    print(' ', x)
"
```

Use this list as the source of truth for the next steps.

- [ ] **Step 2: Add file-level suppression to fully-dead files**

For each of the 8 unused files, add a header comment immediately after any existing header comments. Use these exact headers:

**`src/frontend/src/api/signalr.ts`** — top of file:
```ts
// fallow-ignore-file unused-export
// fallow-ignore-file unused-file
//
// Real-time SignalR client + hooks. Wired up by US-FP-068
// (Real-time order updates infrastructure).
```

**`src/frontend/src/api/useSignalR.ts`** — same pattern with reason: "Real-time SignalR React hook. Consumed by US-FP-068."

**`src/frontend/src/api/useOrderUpdates.ts`** — reason: "Hook subscribing to OrderHub events. Consumed by US-FP-068."

**`src/frontend/src/features/pos/routes.tsx`** — reason: "POS route module. Wired up by US-FP-018 (POS ordering)."

**`src/frontend/src/features/storefront/routes.tsx`** — reason: "Storefront route module. Wired up by US-FP-016 + US-FP-017 (online + guest ordering)."

**`src/frontend/src/auth/index.ts`** — reason: "Public barrel for src/auth; kept for downstream consumers."

**`src/frontend/src/test/setup.ts`** — top of file (only `unused-file`, no exports):
```ts
// fallow-ignore-file unused-file
//
// Vitest setup file — loaded by vite.config.ts via setupFiles option.
// Fallow doesn't statically resolve string-config references.
```

**`src/frontend/src/test/testUtils.tsx`** — reason: "Generic render-with-providers helpers; consumed once test suites converge on shared setup."

- [ ] **Step 3: Add per-export `@expected-unused` JSDoc to each unused export**

For each unused export not covered by step 2, add a JSDoc immediately above the export. Pattern:

```ts
/** @expected-unused — <reason: typically a story ID or "future feature"> */
export const productKeys = { ... };
```

Story-ID mapping for the known cases (use these as the reason text):

| Export | Reason text |
|---|---|
| `brandKeys` | `US-FP-002 (Brand CRUD) — used by mutations for cache invalidation` |
| `productKeys` | `US-FP-005 (Product CRUD) — used by mutations for cache invalidation` |
| `shopKeys` | `US-FP-007 (Shop CRUD) — used by mutations for cache invalidation` |
| `menuCategoryKeys` | `US-FP-022 (Menu categories) — used by mutations for cache invalidation` |
| `modifierGroupKeys` | `US-FP-008 (Modifier groups) — used by mutations for cache invalidation` |
| `openingHoursKeys` | `US-FP-009 (Opening hours) — used by mutations for cache invalidation` |
| `orderLifecycleKeys` | `US-FP-024 (Order lifecycle) — used by mutations for cache invalidation` |
| `platformAdminKeys` | `US-FP-001 (Platform admin) — used by mutations for cache invalidation` |
| `taxConfigurationKeys` | `US-FP-046 (VAT) — used by mutations for cache invalidation` |
| `brandThemeKeys` | `US-FP-003 (Brand theming) — used by mutations for cache invalidation` |
| `brandSettingsKeys` | `US-FP-002 (Brand settings) — used by mutations for cache invalidation` |
| `brandStaffKeys` | `US-FP-007 (Brand staff) — used by mutations for cache invalidation` |
| `useShopStaff` | `US-FP-007 (Shop staff) — wired up when staff list page lands` |
| `useShopStatus` | `US-FP-024 (Shop status badge) — wired up when storefront ships` |
| `useAssignProductToCategory` | `US-FP-022 (Menu category assignment) — deferred to admin UX` |
| `bffClient` | `Lower-level BFF client — public API surface for future consumers` |

For unused types listed in the fallow output, add the same JSDoc immediately above each `type` or `interface` declaration. The reason text can simply state "DTO shape used once <related feature> ships" with the closest story ID if knowable; otherwise just "future feature".

- [ ] **Step 4: Verify suppressions clear all findings**

```bash
cd C:/Users/MatthiasOoye/Git/TheMillionthFoodOrderApp/src/frontend
fallow dead-code --format json --quiet > /tmp/fallow-deadcode-post.json
python -c "
import json
with open('/tmp/fallow-deadcode-post.json') as f:
    d = json.load(f)
print('total_issues:', d['total_issues'])
print('unused_files:', len(d['unused_files']))
print('unused_exports:', len(d['unused_exports']))
print('unused_types:', len(d['unused_types']))
print('stale_suppressions:', len(d['stale_suppressions']))
"
```

Expected: `total_issues: 0` (or only the 1 unresolved-import flag if that wasn't fixed in commit 1 — it should have been). `unused_files`, `unused_exports`, `unused_types` all 0. `stale_suppressions: 0`.

If `stale_suppressions > 0`, a tag was applied to an export that's actually used — remove that tag and re-run.

- [ ] **Step 5: Re-run type-check and tests to confirm no regressions**

```bash
pnpm type-check && pnpm test
```

Expected: pass.

- [ ] **Step 6: Commit**

```bash
cd C:/Users/MatthiasOoye/Git/TheMillionthFoodOrderApp
git add src/frontend/src
git commit -m "$(cat <<'EOF'
chore(fe): mark deferred-feature exports as @expected-unused

Apply fallow suppressions to 39 dead-code findings that are intentionally
unused now but consumed by upcoming user stories (US-FP-002 brands,
US-FP-005 products, US-FP-068 real-time, US-FP-016/017/018 ordering,
etc.). Fallow tracks staleness — suppressions auto-warn the day the
export gets imported.

setup.ts marked at file-level; loaded by vite config setupFiles.

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>
EOF
)"
```

---

## Commit 3 — shared MSW + auth-event test helpers

### Task 3: Extract `mockEndpoint`, `expectAuthSessionExpired`, and `expectWindowEvent`

**Files:**
- Create: `src/frontend/src/test/mswHelpers.ts`
- Create: `src/frontend/src/test/authExpiredHarness.ts`
- Create: `src/frontend/src/test/eventListenerHarness.ts`
- Modify: `src/frontend/src/api/__tests__/brands.test.ts`
- Modify: `src/frontend/src/api/__tests__/client.test.ts`
- Modify: `src/frontend/src/auth/__tests__/BffAuthProvider.test.tsx`
- Modify: `src/frontend/src/auth/__tests__/useSessionKeepalive.test.tsx`

- [ ] **Step 1: Create `mswHelpers.ts`**

Write `src/frontend/src/test/mswHelpers.ts`:

```ts
import { http, HttpResponse, type HttpHandler } from 'msw';

type Method = 'get' | 'post' | 'put' | 'delete' | 'patch';

/**
 * Returns an MSW handler that responds with the given status (and optional body)
 * to a single request to the given path.
 *
 * Use this to replace inline `server.use(http.get(path, () => new HttpResponse(null, { status })))`
 * blocks in tests.
 */
export function mockEndpoint<TBody = unknown>(
  method: Method,
  path: string,
  status: number,
  body?: TBody,
): HttpHandler {
  const responder = () => {
    if (body === undefined) {
      return new HttpResponse(null, { status });
    }
    return HttpResponse.json(body, { status });
  };
  return http[method](path, responder);
}
```

- [ ] **Step 2: Create `eventListenerHarness.ts`**

Write `src/frontend/src/test/eventListenerHarness.ts`:

```ts
import { vi, type Mock } from 'vitest';

interface RunWithEventListenerResult {
  /** vitest mock that captures every dispatch. Assert via `expect(listener).toHaveBeenCalled[…]`. */
  listener: Mock;
  /** Whatever `action` returned (or threw — caught and stored as `error`). */
  result: unknown;
  /** If `action` rejected, the rejection is captured here. Otherwise `null`. */
  error: unknown;
}

/**
 * Adds a window event listener for `eventName`, runs `action`, removes the listener,
 * and returns the captured listener mock plus the action's result/error.
 *
 * Test code asserts on `listener` directly — `expect(listener).toHaveBeenCalledOnce()`
 * or `expect(listener).not.toHaveBeenCalled()` etc.
 *
 * Errors from `action` are caught (most callers test that an axios call rejects with 401
 * and don't care about the rejection itself).
 */
export async function runWithEventListener(
  eventName: string,
  action: () => Promise<unknown> | unknown,
): Promise<RunWithEventListenerResult> {
  const listener = vi.fn();
  window.addEventListener(eventName, listener);
  let result: unknown = undefined;
  let error: unknown = null;
  try {
    result = await action();
  } catch (e) {
    error = e;
  } finally {
    window.removeEventListener(eventName, listener);
  }
  return { listener, result, error };
}
```

- [ ] **Step 3: Create `authExpiredHarness.ts`**

Write `src/frontend/src/test/authExpiredHarness.ts`:

```ts
import { expect } from 'vitest';
import { runWithEventListener } from './eventListenerHarness';

/**
 * Runs `action`, asserts that exactly one `auth:session-expired` window event was dispatched.
 *
 * The action is allowed to reject — most callers are testing that a 401 from the API client
 * causes both the rejection and the dispatch.
 */
export async function expectAuthSessionExpired(
  action: () => Promise<unknown>,
): Promise<void> {
  const { listener } = await runWithEventListener('auth:session-expired', action);
  expect(listener).toHaveBeenCalledOnce();
}

/**
 * Inverse of `expectAuthSessionExpired`: asserts that NO `auth:session-expired` event
 * was dispatched while running `action`.
 */
export async function expectNoAuthSessionExpired(
  action: () => Promise<unknown>,
): Promise<void> {
  const { listener } = await runWithEventListener('auth:session-expired', action);
  expect(listener).not.toHaveBeenCalled();
}
```

- [ ] **Step 4: Run a quick smoke test on the new helpers**

```bash
cd C:/Users/MatthiasOoye/Git/TheMillionthFoodOrderApp/src/frontend
pnpm type-check
```

Expected: clean. The helpers don't have their own tests yet — they get exercised by the call sites in the next steps.

- [ ] **Step 5: Refactor `src/api/__tests__/brands.test.ts`**

Replace the body of the `dispatches auth:session-expired on 401` test (currently lines 23–40):

```ts
    it('dispatches auth:session-expired on 401', async () => {
      server.use(mockEndpoint('get', '/api/brands', 401));
      await expectAuthSessionExpired(() => brandsApi.list());
    });
```

Add the imports at the top:

```ts
import { mockEndpoint } from '../../test/mswHelpers';
import { expectAuthSessionExpired } from '../../test/authExpiredHarness';
```

Remove the now-unused `vi`, `http`, `HttpResponse` imports IF this is the only test in the file using them. If other tests still use them, leave them.

- [ ] **Step 6: Refactor `src/api/__tests__/client.test.ts`**

Two adjustments per test pair (401 and 403):

For the `401 → auth:session-expired event` block (lines 66–104), rewrite as:

```ts
  describe('401 → auth:session-expired event', () => {
    it('dispatches auth:session-expired on 401 response', async () => {
      server.use(mockEndpoint('get', '/api/brands', 401));
      await expectAuthSessionExpired(() => apiClient.get('/brands'));
    });

    it('does NOT dispatch auth:session-expired on 403 response', async () => {
      server.use(mockEndpoint('get', '/api/brands', 403));
      await expectNoAuthSessionExpired(() => apiClient.get('/brands'));
    });
  });
```

For the `403 → auth:access-denied event` block (lines 106–144), rewrite using `runWithEventListener` directly (since `auth:access-denied` doesn't have a dedicated harness):

```ts
  describe('403 → auth:access-denied event', () => {
    it('dispatches auth:access-denied on 403 response', async () => {
      server.use(mockEndpoint('get', '/api/brands', 403));
      const { listener } = await runWithEventListener(
        'auth:access-denied',
        () => apiClient.get('/brands'),
      );
      expect(listener).toHaveBeenCalledOnce();
    });

    it('does NOT dispatch auth:access-denied on 401 response', async () => {
      server.use(mockEndpoint('get', '/api/brands', 401));
      const { listener } = await runWithEventListener(
        'auth:access-denied',
        () => apiClient.get('/brands'),
      );
      expect(listener).not.toHaveBeenCalled();
    });
  });
```

For the `successful responses` block (lines 146–164), rewrite the both-listeners variant as:

```ts
  describe('successful responses', () => {
    it('returns 2xx responses normally without dispatching events', async () => {
      const sessionExpired = await runWithEventListener(
        'auth:session-expired',
        async () => {
          const acc = await runWithEventListener(
            'auth:access-denied',
            () => apiClient.get('/brands'),
          );
          return acc;
        },
      );
      // The inner runWithEventListener returns its own listener result via .result
      const accessDenied = (sessionExpired.result as { listener: Mock }).listener;
      expect(sessionExpired.listener).not.toHaveBeenCalled();
      expect(accessDenied).not.toHaveBeenCalled();
    });
  });
```

(Add `import { runWithEventListener } from '../../test/eventListenerHarness';` and `import type { Mock } from 'vitest';` to the imports.)

Update the imports section at the top of the file:

```ts
import { describe, it, expect, afterEach } from 'vitest';
import type { Mock } from 'vitest';
import { server } from '../../test/msw/server';
import { apiClient, setActiveBrandSlug, getActiveBrandSlug } from '../client';
import { mockEndpoint } from '../../test/mswHelpers';
import {
  expectAuthSessionExpired,
  expectNoAuthSessionExpired,
} from '../../test/authExpiredHarness';
import { runWithEventListener } from '../../test/eventListenerHarness';
```

(Remove `vi`, `http`, `HttpResponse` imports — no longer needed.)

The `X-Brand-Slug` tests (lines 32–63) keep using the inline `http.get(...)` form because they need to capture the request, not just respond — leave those as-is.

- [ ] **Step 7: Refactor `src/auth/__tests__/BffAuthProvider.test.tsx`**

This file's only auth-event-related test is the `invalidates user query when auth:session-expired event fires` test (lines 95–120). It's not asserting on the listener mock — it's *dispatching* the event itself. Leave that test as-is (different pattern).

The file otherwise uses bespoke MSW patterns that aren't worth de-duping. **No changes needed in this file.** Skip to step 8.

- [ ] **Step 8: Refactor `src/auth/__tests__/useSessionKeepalive.test.tsx`**

The keepalive-call detection pattern (`let keepaliveCalled = false; server.use(http.post(...))`) is repeated three times (tests at lines 30, 52, 72). Extract a tiny helper inline at the top of this file (test-file-local, not exported globally):

Add immediately after the imports:

```ts
function trackKeepaliveCall(): { wasCalled: () => boolean } {
  const state = { called: false };
  server.use(
    http.post('/bff/session/keepalive', () => {
      state.called = true;
      return new HttpResponse(null, { status: 200 });
    }),
  );
  return { wasCalled: () => state.called };
}
```

Then rewrite each of the three tests to use it:

```ts
  it('does nothing when VITE_MOCK_AUTH=true (mock mode)', async () => {
    vi.stubEnv('VITE_MOCK_AUTH', 'true');
    const tracker = trackKeepaliveCall();

    renderHook(() => useSessionKeepalive(true));

    window.dispatchEvent(new MouseEvent('mousemove'));
    await act(async () => {
      vi.advanceTimersByTime(15 * 60 * 1000 + 100);
      await Promise.resolve();
    });

    expect(tracker.wasCalled()).toBe(false);
  });

  it('does nothing when not authenticated', async () => {
    const tracker = trackKeepaliveCall();

    renderHook(() => useSessionKeepalive(false));

    window.dispatchEvent(new MouseEvent('mousemove'));
    await act(async () => {
      vi.advanceTimersByTime(15 * 60 * 1000 + 100);
      await Promise.resolve();
    });

    expect(tracker.wasCalled()).toBe(false);
  });

  it('calls keepalive when there was recent user activity', async () => {
    const tracker = trackKeepaliveCall();

    renderHook(() => useSessionKeepalive(true));
    window.dispatchEvent(new MouseEvent('mousemove'));
    await act(async () => {
      vi.advanceTimersByTime(600);
    });
    await act(async () => {
      vi.advanceTimersByTime(15 * 60 * 1000);
      await Promise.resolve();
    });

    expect(tracker.wasCalled()).toBe(true);
  });
```

For the `dispatches auth:session-expired when keepalive returns 401` test (line 100), use the harness:

```ts
  it('dispatches auth:session-expired when keepalive returns 401', async () => {
    server.use(mockEndpoint('post', '/bff/session/keepalive', 401));

    await expectAuthSessionExpired(async () => {
      renderHook(() => useSessionKeepalive(true));
      window.dispatchEvent(new MouseEvent('mousemove'));
      await act(async () => {
        vi.advanceTimersByTime(600);
      });
      await act(async () => {
        vi.advanceTimersByTime(15 * 60 * 1000);
        await Promise.resolve();
      });
    });
  });
```

Add the imports at the top:

```ts
import { mockEndpoint } from '../../test/mswHelpers';
import { expectAuthSessionExpired } from '../../test/authExpiredHarness';
```

- [ ] **Step 9: Run the full test suite**

```bash
cd C:/Users/MatthiasOoye/Git/TheMillionthFoodOrderApp/src/frontend
pnpm type-check && pnpm test
```

Expected: all tests pass with the same number of assertions as before. If a test fails because mock setup timing changed, debug — the helpers must be behaviorally identical to the inline code they replaced.

- [ ] **Step 10: Re-run dupes analysis**

```bash
fallow dupes --format json --quiet > /tmp/fallow-dupes-post.json
python -c "
import json
with open('/tmp/fallow-dupes-post.json') as f:
    d = json.load(f)
print('duplication_pct:', d['stats']['duplication_percentage'])
print('clone_groups:', d['stats']['clone_groups'])
print('duplicated_lines:', d['stats']['duplicated_lines'])
"
```

Expected: `duplication_pct` drops from 35.9 to roughly 12–18%. Capture this number for the eventual PR description.

- [ ] **Step 11: Commit**

```bash
cd C:/Users/MatthiasOoye/Git/TheMillionthFoodOrderApp
git add src/frontend/src
git commit -m "$(cat <<'EOF'
refactor(fe): extract shared MSW + auth-expired test helpers

Three new helpers in src/test/:
- mswHelpers.ts: mockEndpoint(method, path, status, body?) handler factory
- eventListenerHarness.ts: runWithEventListener(eventName, action)
- authExpiredHarness.ts: expectAuthSessionExpired / expectNoAuthSessionExpired

Adopted by brands.test.ts, client.test.ts, useSessionKeepalive.test.tsx.
BffAuthProvider.test.tsx left unchanged (its event tests use a different
pattern — dispatching, not listening).

Cuts repeated MSW + listener boilerplate across the auth-related test
suites; duplication-percentage drops from ~36% toward target <15%.

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>
EOF
)"
```

---

## Commit 4 — `useResourceForm` hook + form primitives

### Task 4: Install RHF + zod, create primitives, write tests

**Files:**
- Modify: `src/frontend/package.json`, `src/frontend/pnpm-lock.yaml`
- Create: `src/frontend/src/features/admin/forms/useResourceForm.ts`
- Create: `src/frontend/src/features/admin/forms/FormSection.tsx`
- Create: `src/frontend/src/features/admin/forms/NestedItemList.tsx`
- Create: `src/frontend/src/features/admin/forms/index.ts` (barrel)
- Create: `src/frontend/src/features/admin/forms/__tests__/useResourceForm.test.tsx`
- Create: `src/frontend/src/features/admin/forms/__tests__/FormSection.test.tsx`
- Create: `src/frontend/src/features/admin/forms/__tests__/NestedItemList.test.tsx`

- [ ] **Step 1: Install dependencies**

```bash
cd C:/Users/MatthiasOoye/Git/TheMillionthFoodOrderApp/src/frontend
pnpm view react-hook-form version
pnpm view zod version
pnpm view @hookform/resolvers version
```

Note the versions printed. Then install (let pnpm pick latest majors):

```bash
pnpm add react-hook-form zod @hookform/resolvers
```

Expected: three new entries in `package.json` `dependencies`. Lockfile updated.

- [ ] **Step 2: Create `useResourceForm.ts`**

Write `src/frontend/src/features/admin/forms/useResourceForm.ts`:

```ts
import { useEffect, useRef } from 'react';
import { useForm, type UseFormReturn, type DefaultValues } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { useQuery, useMutation, useQueryClient, type QueryKey } from '@tanstack/react-query';
import type { ZodType } from 'zod';

interface UseResourceFormParams<TResource, TFormValues extends Record<string, unknown>, TUpdateInput> {
  /** TanStack Query key used to fetch the resource. */
  queryKey: QueryKey;
  /** Loads the resource from the server (typically `() => api.get(id)`). */
  fetch: () => Promise<TResource>;
  /** Submits the form's update payload (typically `(payload) => api.update(id, payload)`). */
  update: (payload: TUpdateInput) => Promise<TResource>;
  /** zod schema for validation. The schema's inferred type is the form's value type. */
  schema: ZodType<TFormValues>;
  /** Maps the loaded resource onto the form's initial values. */
  toFormValues: (resource: TResource) => TFormValues;
  /** Maps form values onto the API update payload. Defaults to identity. */
  toUpdatePayload?: (values: TFormValues) => TUpdateInput;
  /** Query keys to invalidate after a successful submit. */
  invalidate?: QueryKey[];
  /** Called after a successful submit (typically navigation). */
  onSuccess?: (updated: TResource) => void;
  /** Default form values used before the resource has loaded. */
  defaultValues: DefaultValues<TFormValues>;
}

interface UseResourceFormResult<TFormValues extends Record<string, unknown>> {
  form: UseFormReturn<TFormValues>;
  submit: () => Promise<void>;
  isSubmitting: boolean;
  isFetching: boolean;
  fetchError: unknown;
  submitError: unknown;
}

/**
 * Combines TanStack Query (resource fetch + mutation + cache invalidation)
 * with react-hook-form (form state, validation via zod).
 *
 * The form is initialized with `defaultValues`, then `form.reset(toFormValues(data))`
 * runs exactly once after the first successful fetch. Subsequent re-fetches do NOT
 * reset — that would discard in-progress edits.
 */
export function useResourceForm<TResource, TFormValues extends Record<string, unknown>, TUpdateInput = TFormValues>(
  params: UseResourceFormParams<TResource, TFormValues, TUpdateInput>,
): UseResourceFormResult<TFormValues> {
  const {
    queryKey,
    fetch,
    update,
    schema,
    toFormValues,
    toUpdatePayload,
    invalidate = [],
    onSuccess,
    defaultValues,
  } = params;

  const queryClient = useQueryClient();
  const form = useForm<TFormValues>({
    resolver: zodResolver(schema),
    defaultValues,
  });

  const fetchQuery = useQuery<TResource>({
    queryKey,
    queryFn: fetch,
  });

  const hasResetRef = useRef(false);
  useEffect(() => {
    if (fetchQuery.data !== undefined && !hasResetRef.current) {
      form.reset(toFormValues(fetchQuery.data));
      hasResetRef.current = true;
    }
  }, [fetchQuery.data, form, toFormValues]);

  const mutation = useMutation<TResource, unknown, TUpdateInput>({
    mutationFn: update,
    onSuccess: async (updated) => {
      await Promise.all(invalidate.map((key) => queryClient.invalidateQueries({ queryKey: key })));
      onSuccess?.(updated);
    },
  });

  const submit = form.handleSubmit(async (values) => {
    const payload = (toUpdatePayload ? toUpdatePayload(values) : (values as unknown as TUpdateInput));
    await mutation.mutateAsync(payload);
  });

  return {
    form,
    submit,
    isSubmitting: mutation.isPending,
    isFetching: fetchQuery.isLoading,
    fetchError: fetchQuery.error,
    submitError: mutation.error,
  };
}
```

- [ ] **Step 3: Create `FormSection.tsx`**

Write `src/frontend/src/features/admin/forms/FormSection.tsx`:

```tsx
import { type ReactNode, useState } from 'react';

interface FormSectionProps {
  title: string;
  description?: string;
  defaultOpen?: boolean;
  children: ReactNode;
}

/**
 * A collapsible labelled card grouping related form fields.
 * Used by the admin Edit/Create pages to break long forms into chunks.
 */
export function FormSection({
  title,
  description,
  defaultOpen = true,
  children,
}: FormSectionProps): JSX.Element {
  const [isOpen, setIsOpen] = useState(defaultOpen);

  return (
    <section
      className="form-section"
      style={{
        border: '1px solid #e5e7eb',
        borderRadius: 8,
        padding: 16,
        marginBottom: 16,
      }}
    >
      <header
        style={{
          display: 'flex',
          alignItems: 'center',
          justifyContent: 'space-between',
          cursor: 'pointer',
        }}
        onClick={() => setIsOpen((v) => !v)}
        role="button"
        aria-expanded={isOpen}
        tabIndex={0}
        onKeyDown={(e) => {
          if (e.key === 'Enter' || e.key === ' ') {
            e.preventDefault();
            setIsOpen((v) => !v);
          }
        }}
      >
        <div>
          <h3 style={{ margin: 0 }}>{title}</h3>
          {description !== undefined && (
            <p style={{ margin: '4px 0 0 0', color: '#6b7280', fontSize: 14 }}>{description}</p>
          )}
        </div>
        <span aria-hidden="true">{isOpen ? '▼' : '▶'}</span>
      </header>
      {isOpen && <div style={{ marginTop: 12 }}>{children}</div>}
    </section>
  );
}
```

- [ ] **Step 4: Create `NestedItemList.tsx`**

Write `src/frontend/src/features/admin/forms/NestedItemList.tsx`:

```tsx
import { type ReactNode } from 'react';
import {
  useFieldArray,
  type ArrayPath,
  type FieldValues,
  type UseFormReturn,
} from 'react-hook-form';

interface NestedItemListProps<TFormValues extends FieldValues, TItem> {
  form: UseFormReturn<TFormValues>;
  /** Dotted path into the form values pointing at an array (e.g. `'componentProductIds'`). */
  name: ArrayPath<TFormValues>;
  /** Renders a single row. Receives the field, its index, and a remove callback. */
  renderRow: (field: TItem & { id: string }, index: number, remove: () => void) => ReactNode;
  /** Returns the value to append when "Add" is clicked. */
  newItem: () => TItem;
  /** Optional label for the add button. Defaults to "Add". */
  addLabel?: string;
}

/**
 * Wraps RHF's useFieldArray to render a dynamic list of rows with
 * standardized add/remove controls. Used by combo product items,
 * modifier group options, and opening-hour slots.
 */
export function NestedItemList<TFormValues extends FieldValues, TItem>(
  props: NestedItemListProps<TFormValues, TItem>,
): JSX.Element {
  const { form, name, renderRow, newItem, addLabel = 'Add' } = props;
  const { fields, append, remove } = useFieldArray<TFormValues>({
    control: form.control,
    name,
  });

  return (
    <div className="nested-item-list">
      {fields.map((field, index) =>
        renderRow(field as TItem & { id: string }, index, () => { remove(index); }),
      )}
      <button
        type="button"
        onClick={() => { append(newItem() as never); }}
      >
        {addLabel}
      </button>
    </div>
  );
}
```

- [ ] **Step 5: Create the barrel `index.ts`**

Write `src/frontend/src/features/admin/forms/index.ts`:

```ts
export { useResourceForm } from './useResourceForm';
export { FormSection } from './FormSection';
export { NestedItemList } from './NestedItemList';
```

- [ ] **Step 6: Type-check after primitives are in place**

```bash
cd C:/Users/MatthiasOoye/Git/TheMillionthFoodOrderApp/src/frontend
pnpm type-check
```

Expected: clean. If TS complains about `JSX.Element` in `.tsx` files, switch to `ReactElement` from `react` and use `import type { ReactElement } from 'react'`.

- [ ] **Step 7: Write tests for `useResourceForm` (TDD — failing first)**

Write `src/frontend/src/features/admin/forms/__tests__/useResourceForm.test.tsx`:

```tsx
import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { renderHook, act, waitFor } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { z } from 'zod';
import { useResourceForm } from '../useResourceForm';
import type { ReactNode } from 'react';

const schema = z.object({
  name: z.string().min(1),
  count: z.number().int().nonnegative(),
});

type FormValues = z.infer<typeof schema>;
interface Resource { id: string; name: string; count: number; }

function makeWrapper(client?: QueryClient) {
  const queryClient = client ?? new QueryClient({
    defaultOptions: { queries: { retry: false }, mutations: { retry: false } },
  });
  return ({ children }: { children: ReactNode }) => (
    <QueryClientProvider client={queryClient}>{children}</QueryClientProvider>
  );
}

describe('useResourceForm', () => {
  beforeEach(() => {
    vi.useRealTimers();
  });

  afterEach(() => {
    vi.restoreAllMocks();
  });

  it('seeds form values from fetched resource', async () => {
    const fetch = vi.fn<[], Promise<Resource>>().mockResolvedValue({
      id: 'r1',
      name: 'hello',
      count: 3,
    });

    const { result } = renderHook(
      () => useResourceForm<Resource, FormValues>({
        queryKey: ['resource', 'r1'],
        fetch,
        update: vi.fn(),
        schema,
        toFormValues: (r) => ({ name: r.name, count: r.count }),
        defaultValues: { name: '', count: 0 },
      }),
      { wrapper: makeWrapper() },
    );

    await waitFor(() => {
      expect(result.current.form.getValues()).toEqual({ name: 'hello', count: 3 });
    });
  });

  it('rejects submit when validation fails', async () => {
    const update = vi.fn();
    const { result } = renderHook(
      () => useResourceForm<Resource, FormValues>({
        queryKey: ['r2'],
        fetch: () => Promise.resolve({ id: 'r2', name: 'x', count: 0 }),
        update,
        schema,
        toFormValues: (r) => ({ name: r.name, count: r.count }),
        defaultValues: { name: '', count: 0 },
      }),
      { wrapper: makeWrapper() },
    );

    // Empty out the name field to trigger validation failure
    await act(async () => {
      result.current.form.setValue('name', '');
      await result.current.submit();
    });

    expect(update).not.toHaveBeenCalled();
    expect(result.current.form.formState.errors.name).toBeDefined();
  });

  it('calls update + invalidates + onSuccess on valid submit', async () => {
    const update = vi.fn().mockResolvedValue({ id: 'r3', name: 'new', count: 5 });
    const onSuccess = vi.fn();
    const queryClient = new QueryClient({
      defaultOptions: { queries: { retry: false }, mutations: { retry: false } },
    });
    const invalidateSpy = vi.spyOn(queryClient, 'invalidateQueries');

    const { result } = renderHook(
      () => useResourceForm<Resource, FormValues>({
        queryKey: ['r3'],
        fetch: () => Promise.resolve({ id: 'r3', name: 'old', count: 1 }),
        update,
        schema,
        toFormValues: (r) => ({ name: r.name, count: r.count }),
        invalidate: [['resources', 'list']],
        onSuccess,
        defaultValues: { name: '', count: 0 },
      }),
      { wrapper: makeWrapper(queryClient) },
    );

    await waitFor(() => {
      expect(result.current.form.getValues().name).toBe('old');
    });

    await act(async () => {
      result.current.form.setValue('name', 'new');
      result.current.form.setValue('count', 5);
      await result.current.submit();
    });

    expect(update).toHaveBeenCalledWith({ name: 'new', count: 5 });
    expect(invalidateSpy).toHaveBeenCalledWith({ queryKey: ['resources', 'list'] });
    expect(onSuccess).toHaveBeenCalledWith({ id: 'r3', name: 'new', count: 5 });
  });

  it('exposes submitError when mutation rejects', async () => {
    const err = new Error('boom');
    const update = vi.fn().mockRejectedValue(err);

    const { result } = renderHook(
      () => useResourceForm<Resource, FormValues>({
        queryKey: ['r4'],
        fetch: () => Promise.resolve({ id: 'r4', name: 'x', count: 0 }),
        update,
        schema,
        toFormValues: (r) => ({ name: r.name, count: r.count }),
        defaultValues: { name: '', count: 0 },
      }),
      { wrapper: makeWrapper() },
    );

    await waitFor(() => {
      expect(result.current.form.getValues().name).toBe('x');
    });

    await act(async () => {
      result.current.form.setValue('name', 'valid');
      await result.current.submit();
    });

    await waitFor(() => {
      expect(result.current.submitError).toBe(err);
    });
  });
});
```

- [ ] **Step 8: Run the new tests**

```bash
pnpm test useResourceForm
```

Expected: 4 tests pass.

- [ ] **Step 9: Write tests for `FormSection`**

Write `src/frontend/src/features/admin/forms/__tests__/FormSection.test.tsx`:

```tsx
import { describe, it, expect } from 'vitest';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { FormSection } from '../FormSection';

describe('FormSection', () => {
  it('renders title, description, and children when open', () => {
    render(
      <FormSection title="Basic info" description="Set the basics">
        <div>Inner content</div>
      </FormSection>,
    );

    expect(screen.getByText('Basic info')).toBeInTheDocument();
    expect(screen.getByText('Set the basics')).toBeInTheDocument();
    expect(screen.getByText('Inner content')).toBeInTheDocument();
  });

  it('hides children when collapsed', async () => {
    const user = userEvent.setup();
    render(
      <FormSection title="Group" defaultOpen>
        <div data-testid="inner">Inner</div>
      </FormSection>,
    );

    expect(screen.getByTestId('inner')).toBeInTheDocument();
    await user.click(screen.getByRole('button', { name: /Group/ }));
    expect(screen.queryByTestId('inner')).not.toBeInTheDocument();
  });

  it('starts collapsed when defaultOpen is false', () => {
    render(
      <FormSection title="Group" defaultOpen={false}>
        <div data-testid="inner">Inner</div>
      </FormSection>,
    );

    expect(screen.queryByTestId('inner')).not.toBeInTheDocument();
  });
});
```

- [ ] **Step 10: Run FormSection tests**

```bash
pnpm test FormSection
```

Expected: 3 tests pass.

- [ ] **Step 11: Write tests for `NestedItemList`**

Write `src/frontend/src/features/admin/forms/__tests__/NestedItemList.test.tsx`:

```tsx
import { describe, it, expect } from 'vitest';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { useForm } from 'react-hook-form';
import { NestedItemList } from '../NestedItemList';

interface ItemForm {
  items: { value: string }[];
}

function Harness({ initial = [] }: { initial?: { value: string }[] }) {
  const form = useForm<ItemForm>({ defaultValues: { items: initial } });
  return (
    <NestedItemList<ItemForm, { value: string }>
      form={form}
      name="items"
      renderRow={(field, index, remove) => (
        <div key={field.id} data-testid={`row-${String(index)}`}>
          <span>{field.value}</span>
          <button type="button" onClick={remove} data-testid={`remove-${String(index)}`}>
            Remove
          </button>
        </div>
      )}
      newItem={() => ({ value: 'new' })}
      addLabel="Add item"
    />
  );
}

describe('NestedItemList', () => {
  it('renders existing rows', () => {
    render(<Harness initial={[{ value: 'a' }, { value: 'b' }]} />);
    expect(screen.getByTestId('row-0')).toHaveTextContent('a');
    expect(screen.getByTestId('row-1')).toHaveTextContent('b');
  });

  it('appends a new row when add is clicked', async () => {
    const user = userEvent.setup();
    render(<Harness />);

    expect(screen.queryByTestId('row-0')).not.toBeInTheDocument();
    await user.click(screen.getByRole('button', { name: 'Add item' }));
    expect(screen.getByTestId('row-0')).toHaveTextContent('new');
  });

  it('removes a row when remove is clicked', async () => {
    const user = userEvent.setup();
    render(<Harness initial={[{ value: 'a' }, { value: 'b' }]} />);

    expect(screen.getByTestId('row-1')).toBeInTheDocument();
    await user.click(screen.getByTestId('remove-0'));
    // After removal, the second row reindexes to 0
    expect(screen.getByTestId('row-0')).toHaveTextContent('b');
    expect(screen.queryByTestId('row-1')).not.toBeInTheDocument();
  });
});
```

- [ ] **Step 12: Run NestedItemList tests**

```bash
pnpm test NestedItemList
```

Expected: 3 tests pass.

- [ ] **Step 13: Run the full suite + type-check + build**

```bash
pnpm type-check && pnpm test && pnpm build
```

Expected: all green.

- [ ] **Step 14: Commit**

```bash
cd C:/Users/MatthiasOoye/Git/TheMillionthFoodOrderApp
git add src/frontend/package.json src/frontend/pnpm-lock.yaml src/frontend/src/features/admin/forms
git commit -m "$(cat <<'EOF'
refactor(fe): extract useResourceForm hook and form primitives

Add react-hook-form + zod + @hookform/resolvers as dependencies.

Three new primitives in src/features/admin/forms/:
- useResourceForm<TResource, TFormValues, TUpdate>: combines TanStack
  Query (fetch + mutation + cache invalidation) with RHF (form state +
  zod validation). Form resets exactly once after first successful fetch.
- FormSection: collapsible labelled card grouping related fields.
- NestedItemList: useFieldArray wrapper for dynamic row lists.

Tests in src/features/admin/forms/__tests__/ cover happy path, validation
failure, mutation error, cache invalidation, navigation on success,
section collapse/expand, and item add/remove.

Pure addition — no consumer changes in this commit.

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>
EOF
)"
```

---

## Commits 5–12 — Per-page refactors

The next 8 commits follow a repeating template. **Read this template once before starting any per-page commit**, then apply it page by page in the order below (highest CRAP first).

### Per-page refactor template (apply to each commit 5–12)

For each target page `Foo.tsx`:

1. **Read the current page file in full.** Identify:
   - Form fields (the `useState` calls or any controlled inputs).
   - Validation logic (any inline `if (!x)` checks or schema-like validation).
   - The submit handler (typically calls a mutation hook then navigates).
   - Any nested item lists (combo items, modifier options, hour slots, etc.).
   - Query-key invalidation calls (`queryClient.invalidateQueries({ queryKey: ... })`).

2. **Create the page-specific zod schema** at `src/frontend/src/features/admin/pages/schemas/<page>Schema.ts`. The schema must:
   - Mirror the existing form-state shape exactly.
   - Encode every validation rule the page currently performs (required-string, positive-number, regex, etc.).
   - Export both the schema and the inferred TS type via `z.infer`.

   Skeleton:

   ```ts
   import { z } from 'zod';

   export const fooSchema = z.object({
     // ... fields matching the page's form state
   });

   export type FooFormValues = z.infer<typeof fooSchema>;
   ```

3. **Replace the page body** following this shape:

   ```tsx
   import { useNavigate, useParams } from 'react-router-dom';
   import { useForm } from 'react-hook-form';
   import { useResourceForm, FormSection, NestedItemList } from '../forms';
   import { fooSchema, type FooFormValues } from './schemas/fooSchema';
   import { fooApi } from '../../../api/foo';
   import { fooKeys } from '../hooks/useFoo';

   export function FooEdit(): JSX.Element {
     const navigate = useNavigate();
     const { brandSlug = '', fooId = '' } = useParams<{ brandSlug: string; fooId: string }>();

     const { form, submit, isSubmitting, isFetching, fetchError, submitError } = useResourceForm({
       queryKey: fooKeys.detail(brandSlug, fooId),
       fetch: () => fooApi.get(brandSlug, fooId),
       update: (payload) => fooApi.update(brandSlug, fooId, payload),
       schema: fooSchema,
       toFormValues: (resource) => ({
         // ... map from resource to form values
       }),
       toUpdatePayload: (values) => ({
         // ... map from form values to API payload (or omit if identical)
       }),
       invalidate: [fooKeys.lists(brandSlug), fooKeys.detail(brandSlug, fooId)],
       onSuccess: () => navigate('../'),
       defaultValues: {
         // ... empty form skeleton
       },
     });

     if (isFetching) return <div>Loading…</div>;
     if (fetchError !== null) return <div>Error: {String(fetchError)}</div>;

     return (
       <form onSubmit={(e) => { e.preventDefault(); void submit(); }}>
         <FormSection title="Basics">
           {/* form fields using form.register('field'), form.formState.errors */}
         </FormSection>
         {/* additional sections, NestedItemList for arrays */}
         <button type="submit" disabled={isSubmitting}>
           {isSubmitting ? 'Saving…' : 'Save'}
         </button>
         {submitError !== null && <div role="alert">{String(submitError)}</div>}
       </form>
     );
   }
   ```

4. **Behavior must be identical:**
   - Same submit produces same API request.
   - Same query-key invalidations after success.
   - Same redirect on success.
   - Same loading/error UI states.
   - Same field-level validation errors render in the same places.

5. **Run page-scoped tests + type-check:**

   ```bash
   pnpm test <page-test-pattern> && pnpm type-check
   ```

   If a test asserts on internal state shape (e.g., reads internal `useState` values), rewrite the assertion against observable behavior (DOM, network calls, navigation). Note this in the commit message.

6. **Manual smoke (single command):** start the dev server, load the page, edit a field, submit, verify list refreshes and stale validation errors clear. Trigger validation deliberately by submitting empty / invalid input.

7. **Verify per-page metrics:**

   ```bash
   fallow health --format json --quiet --top 50 > /tmp/fallow-health-page.json
   python -c "
   import json
   with open('/tmp/fallow-health-page.json') as f:
     d = json.load(f)
   for fn in d.get('findings', []):
     if fn.get('name') == '<PageName>':
       print(fn)
   "
   ```

   Expected: cyclomatic ≤10, cognitive ≤8 for the refactored function. (If still above, the page likely has more nested logic that should be extracted into helper functions before committing — typically `<FieldGroup>` sub-components.)

8. **Commit** with a message like:

   ```
   refactor(fe): apply useResourceForm to FooEdit

   Replaces inline useState form state with react-hook-form + zod schema.
   Behavior unchanged — same API requests, same invalidations, same nav.

   Cyclomatic: 33 -> 8. LOC: 652 -> 184.

   Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>
   ```

---

### Task 5 — Commit 5: ComboProductEdit

**Files:**
- Read: `src/frontend/src/features/admin/pages/ComboProductEdit.tsx` (currently 850 LOC, cyclomatic 39)
- Create: `src/frontend/src/features/admin/pages/schemas/comboProductEditSchema.ts`
- Modify: `src/frontend/src/features/admin/pages/ComboProductEdit.tsx`

- [ ] **Step 1: Apply the per-page template (above) to ComboProductEdit**

Specifics for this page:
- The combo-items list (component products selected) is a `NestedItemList` candidate.
- Schema fields: `basePrice` (number, ≥0), `imageUrl` (string, optional URL), `translations` (object keyed by locale: `nl/fr/de` → `{ name: string min(1), description: string optional }`), `componentProductIds` (string array, min length 1), `allergens` (number array), `dietaryTags` (number array).
- Mutation: `productsApi.updateCombo(brandSlug, productId, payload)`.
- Invalidate: `productKeys.lists(brandSlug)`, `productKeys.detail(brandSlug, productId)`.

- [ ] **Step 2: Run page tests + type-check + build**

```bash
pnpm test ComboProductEdit && pnpm type-check && pnpm build
```

- [ ] **Step 3: Verify metrics**

```bash
fallow health --format json --quiet --top 30 | python -c "
import json, sys
d = json.load(sys.stdin)
for fn in d.get('findings', []):
  if fn.get('name') == 'ComboProductEdit':
    print(fn)
    sys.exit(0)
print('ComboProductEdit no longer above threshold (good).')
"
```

Expected: either no entry (function dropped below all thresholds) or cyclomatic ≤10.

- [ ] **Step 4: Commit**

Use the template commit-message form with the page name and before/after metric numbers.

---

### Task 6 — Commit 6: MenuCategoryEdit

**Files:**
- Read: `src/frontend/src/features/admin/pages/MenuCategoryEdit.tsx` (currently 603 LOC, cyclomatic 36)
- Create: `src/frontend/src/features/admin/pages/schemas/menuCategoryEditSchema.ts`
- Modify: `src/frontend/src/features/admin/pages/MenuCategoryEdit.tsx`

- [ ] **Step 1: Apply the per-page template**

Specifics:
- Schema fields: `displayOrder` (number int ≥0), `translations` (locale-keyed `name`/`description`), `assignedProductIds` (string array — if the page assigns products inline).
- Mutation: `menuCategoriesApi.update(brandSlug, categoryId, payload)`.
- Invalidate: `menuCategoryKeys.lists(brandSlug)`, `menuCategoryKeys.detail(brandSlug, categoryId)`.

- [ ] **Step 2: Run tests + type-check + build, verify metrics, commit**

(See template steps 2–4 of Task 5.)

---

### Task 7 — Commit 7: ProductEdit

**Files:**
- Read: `src/frontend/src/features/admin/pages/ProductEdit.tsx` (currently 754 LOC, cyclomatic 33)
- Create: `src/frontend/src/features/admin/pages/schemas/productEditSchema.ts`
- Modify: `src/frontend/src/features/admin/pages/ProductEdit.tsx`

- [ ] **Step 1: Apply the per-page template**

Specifics:
- The page also manages product↔modifier-group assignment via separate hooks (`useProductModifierGroups`, `useSetProductModifierGroups`). Treat that as a **second resource** — do NOT roll it into `useResourceForm`. Instead keep a separate `useMutation` for assignments and call it after the main submit succeeds, OR leave it as-is in a sibling component below the form. (Pick the simpler option that preserves current UX.)
- Schema fields: `basePrice` (number ≥0), `imageUrl` (URL optional), `translations` (locale-keyed name+description, NL name required), `allergens` (number set / array), `dietaryTags` (number set / array).
- Mutation: `productsApi.update(brandSlug, productId, payload)`.
- Invalidate: `productKeys.lists(brandSlug)`, `productKeys.detail(brandSlug, productId)`.

- [ ] **Step 2: Run tests + type-check + build, verify metrics, commit**

---

### Task 8 — Commit 8: BrandEdit

**Files:**
- Read: `src/frontend/src/features/admin/pages/BrandEdit.tsx` (currently 385 LOC, cyclomatic 24)
- Create: `src/frontend/src/features/admin/pages/schemas/brandEditSchema.ts`
- Modify: `src/frontend/src/features/admin/pages/BrandEdit.tsx`

- [ ] **Step 1: Apply the per-page template**

Specifics:
- Schema: `name` (string min 1), `slug` (string regex `/^[a-z0-9-]+$/`), `contactEmail` (email), `defaultLanguage` (enum 'nl'|'fr'|'de'), `staffAuthMethod` (enum), `isActive` (bool).
- Mutation: `brandsApi.update(slug, payload)`.
- Invalidate: `brandKeys.lists()`, `brandKeys.detail(slug)`.

- [ ] **Step 2: Run tests + type-check + build, verify metrics, commit**

---

### Task 9 — Commit 9: BrandTheming

**Files:**
- Read: `src/frontend/src/features/admin/pages/BrandTheming.tsx` (currently 402 LOC, cyclomatic 23)
- Create: `src/frontend/src/features/admin/pages/schemas/brandThemingSchema.ts`
- Modify: `src/frontend/src/features/admin/pages/BrandTheming.tsx`

- [ ] **Step 1: Apply the per-page template**

Specifics:
- Schema: nested `colors` (primary, secondary, accent, background, text — each hex regex `/^#[0-9a-f]{6}$/i`), nested `typography` (headingFont, bodyFont strings).
- Mutation: `brandThemeApi.update(slug, payload)` (verify exact name in `src/api/`).
- Invalidate: `brandThemeKeys.detail(slug)`.

- [ ] **Step 2: Run tests + type-check + build, verify metrics, commit**

---

### Task 10 — Commit 10: ShopEdit

**Files:**
- Read: `src/frontend/src/features/admin/pages/ShopEdit.tsx` (currently 401 LOC, cyclomatic 21)
- Create: `src/frontend/src/features/admin/pages/schemas/shopEditSchema.ts`
- Modify: `src/frontend/src/features/admin/pages/ShopEdit.tsx`

- [ ] **Step 1: Apply the per-page template**

Specifics:
- Schema: `name`, `slug`, `address` (street/city/postalCode/country), `vatRateTakeaway`, `vatRateEatIn`, `currency`.
- Mutation: `shopsApi.update(brandSlug, shopId, payload)`.
- Invalidate: `shopKeys.lists(brandSlug)`, `shopKeys.detail(brandSlug, shopId)`.

- [ ] **Step 2: Run tests + type-check + build, verify metrics, commit**

---

### Task 11 — Commit 11: Three Create pages

**Files:**
- Read + modify: `ComboProductCreate.tsx`, `ProductCreate.tsx`, `ShopCreate.tsx`
- Create: matching schemas in `src/features/admin/pages/schemas/`

The Create pages are structurally similar to their Edit siblings but call `api.create(...)` rather than `api.update(...)`, and don't have an existing-resource fetch step. **`useResourceForm` is fetch-oriented — for Create, use a thinner pattern:**

For each Create page, replace the local form state with this skeleton:

```tsx
import { useNavigate, useParams } from 'react-router-dom';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import { FormSection } from '../forms';
import { fooCreateSchema, type FooCreateValues } from './schemas/fooCreateSchema';
import { fooApi } from '../../../api/foo';
import { fooKeys } from '../hooks/useFoo';

export function FooCreate(): JSX.Element {
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const { brandSlug = '' } = useParams<{ brandSlug: string }>();

  const form = useForm<FooCreateValues>({
    resolver: zodResolver(fooCreateSchema),
    defaultValues: { /* empty skeleton */ },
  });

  const mutation = useMutation({
    mutationFn: (payload: FooCreateValues) => fooApi.create(brandSlug, payload),
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: fooKeys.lists(brandSlug) });
      navigate('../');
    },
  });

  const onSubmit = form.handleSubmit((values) => mutation.mutateAsync(values));

  return (
    <form onSubmit={(e) => { e.preventDefault(); void onSubmit(); }}>
      <FormSection title="Basics">
        {/* form.register fields */}
      </FormSection>
      <button type="submit" disabled={mutation.isPending}>Create</button>
      {mutation.error !== null && <div role="alert">{String(mutation.error)}</div>}
    </form>
  );
}
```

If a Create page has the same shape as its Edit sibling, **import and reuse the Edit page's schema directly** rather than duplicating it. Only diverge if Create has fields Edit doesn't (e.g., a slug that's only set at creation time).

- [ ] **Step 1: Apply the Create template to all three pages**
- [ ] **Step 2: Run tests + type-check + build, verify metrics, commit**

Commit message body:

```
Apply RHF + zod to ComboProductCreate, ProductCreate, ShopCreate.
Schemas reused from corresponding Edit pages where shape matches;
ShopCreate has its own schema (slug-required-at-create field).
```

---

### Task 12 — Commit 12: ModifierGroupEdit + ShopOrderLifecycle

**Files:**
- Read + modify: `ModifierGroupEdit.tsx` (cyclomatic 19), `ShopOrderLifecycle.tsx` (cyclomatic 15, 721 LOC)
- Create: matching schemas

`ModifierGroupEdit` follows the standard Edit template. The modifier-options array uses `NestedItemList`.

`ShopOrderLifecycle` is the trickiest: it edits the order-status state machine for a shop (which statuses are enabled, which transitions are allowed). The "form" is a 2D matrix of allowed transitions. Approach:
- Schema: `enabledStatuses` (string array of status names), `transitions` (record of `from -> to[]`).
- Render the matrix as a `<table>` of checkboxes; bind each cell to `form.register('transitions.<from>.<to>')` or use `form.watch` + `form.setValue` for the dynamic shape.
- The page may already have helper functions for "is this transition reachable?" — keep those as pure functions outside the component.

- [ ] **Step 1: Apply the per-page template to both pages**
- [ ] **Step 2: Run tests + type-check + build, verify metrics, commit**

---

## Final Verification — pre-PR

### Task 13: Capture before/after metrics and prepare PR description

**Files:** none (analysis only)

- [ ] **Step 1: Run final fallow analyses**

```bash
cd C:/Users/MatthiasOoye/Git/TheMillionthFoodOrderApp/src/frontend
fallow dead-code --format json --quiet > /tmp/fallow-deadcode-final.json
fallow dupes --format json --quiet > /tmp/fallow-dupes-final.json
fallow health --format json --quiet --score > /tmp/fallow-health-final.json
```

- [ ] **Step 2: Verify thresholds met**

```bash
python -c "
import json
with open('/tmp/fallow-deadcode-final.json') as f:
    dc = json.load(f)
with open('/tmp/fallow-dupes-final.json') as f:
    du = json.load(f)
with open('/tmp/fallow-health-final.json') as f:
    he = json.load(f)

print('=== DEAD CODE ===')
print(f\"  total_issues: {dc['total_issues']} (target 0)\")
print(f\"  unused_files: {len(dc['unused_files'])}\")
print(f\"  unused_exports: {len(dc['unused_exports'])}\")
print(f\"  unresolved_imports: {len(dc['unresolved_imports'])}\")
print(f\"  stale_suppressions: {len(dc['stale_suppressions'])}\")
print()
print('=== DUPES ===')
print(f\"  duplication_pct: {du['stats']['duplication_percentage']:.1f} (target <15)\")
print()
print('=== HEALTH ===')
print(f\"  score: {he['health_score']['score']} (target >=92)\")
print(f\"  grade: {he['health_score']['grade']}\")
print(f\"  functions_above_threshold: {he['summary']['functions_above_threshold']}\")
print(f\"  severity_critical_count: {he['summary']['severity_critical_count']}\")
"
```

Expected:
- `total_issues: 0` (or only fallow-suppressed unused-file-style false positives)
- `duplication_pct < 15`
- `health score >= 92`, grade A
- `severity_critical_count` reduced from 28 (will not necessarily reach 0; the remaining criticals should be outside the 9 admin pages)

If any threshold misses, identify which page or finding is the cause and either fix in a follow-up commit on the branch or document in the PR as a known follow-up.

- [ ] **Step 3: Run full pre-flight checks**

```bash
pnpm type-check && pnpm test && pnpm build && pnpm test:e2e
```

Expected: all pass. If `pnpm test:e2e` requires the backend to be running and the agent doesn't have it, document this in the PR description as "manual e2e run pending."

- [ ] **Step 4: Push branch and open PR**

```bash
cd C:/Users/MatthiasOoye/Git/TheMillionthFoodOrderApp
git push -u origin chore/frontend-cleanup
gh pr create --base main --title "chore(fe): cleanup — dead-code suppressions, test de-dup, admin-page refactor" --body "$(cat <<'EOF'
## Summary

Cleans up `src/frontend/` per the design spec at
`docs/superpowers/specs/2026-04-30-frontend-cleanup-design.md`.

### Before / after (fallow)

| Metric | Before | After |
|---|---|---|
| Health score | 83.2 (B) | <fill> |
| Dead-code findings | 39 | <fill> |
| Code duplication | 35.9% | <fill> |
| Critical-complexity functions | 28 | <fill> |
| 9 admin Edit/Create pages — cyclomatic | 14–39 | ≤10 each |

### Approach

- **A.** Removed two unused devDependencies, fixed a broken test import,
  marked 39 deferred-feature findings with `@expected-unused` so fallow
  stops flagging them but tracks staleness when they finally get used.
- **B.** Extracted three test helpers (`mockEndpoint`,
  `expectAuthSessionExpired`, `runWithEventListener`) — cuts MSW + window-event
  boilerplate from four test files.
- **C.** Adopted react-hook-form + zod + `useResourceForm` across 9 admin
  Edit/Create pages. Shared `<FormSection>` and `<NestedItemList>` primitives
  ship in commit 4. Each per-page commit is independently revertable.

## Test plan

- [ ] CI passes (type-check, vitest, build).
- [ ] Manual smoke on each admin Edit page: load → edit → submit → list refreshes.
- [ ] Manual smoke on each Create page: empty submit shows validation errors;
      valid submit creates and navigates back.
- [ ] Run `fallow dead-code`, `fallow dupes`, `fallow health` locally and
      paste numbers into the table above before requesting review.

🤖 Generated with [Claude Code](https://claude.com/claude-code)
EOF
)"
```

Expected: PR opens. Print the URL.

---

## Self-review notes

- **Spec coverage.** Every commit in the spec maps to a task: Commits 1–4 → Tasks 1–4; Commits 5–12 → Tasks 5–12; final verification → Task 13.
- **Type consistency.** `useResourceForm` API stays consistent between definition (Task 4) and consumers (Tasks 5–10, 12). Create-page template (Task 11) uses RHF directly, not `useResourceForm`, because there's no fetch step — this is documented.
- **Risk handling for ProductEdit.** The product↔modifier-groups assignment hooks are explicitly called out as a separate concern that should NOT be rolled into `useResourceForm`. This avoids a common refactor pitfall.
- **Per-page tasks are template-driven, not boilerplate-replicated.** Each Task 5–12 lists the exact schema fields and API mutation specific to that page. The transformation pattern is shown once in the template.
- **Open items from the spec are addressed in the plan:** ESLint config check (Task 1, Step 1), zod/RHF version pinning (Task 4, Step 1), `App.test.tsx` import path (Task 1, Step 5).
