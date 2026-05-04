# 020 — Frontend Form Refactor + Integration Test Reliability

**Date:** 2026-05-04

---

## What Was Built

Two independent improvements shipped together:

1. **`useResourceForm` hook + shared form primitives** — eliminates ~1 500 lines of duplicated form/mutation boilerplate across `ProductEdit`, `MenuCategoryEdit`, and `ComboProductEdit`.
2. **Integration test reliability** — `BrandStaffEndpointTests` was flaky due to TUnit running tests in parallel against shared brand databases. Fixed by applying `[NotInParallel("brand-staff")]` to every test in the class.

---

## Frontend Form Refactor

### The Problem

`ProductEdit`, `MenuCategoryEdit`, and `ComboProductEdit` each contained the same structure: a `useForm` call, TanStack Query `useMutation`, manual `isLoading`/`isError` state, and identical error-handling boilerplate — duplicated three times with minor variations. Adding or fixing any of this pattern required touching three files.

### useResourceForm

A generic hook that wraps the mutation + form lifecycle into one call:

```ts
const { form, onSubmit, isPending, serverError } =
  useResourceForm({ schema, defaultValues, mutationFn, onSuccess });
```

Internally it calls `useForm` (react-hook-form + Zod resolver), `useMutation`, and wires `onError` to surface API error messages without the caller doing anything extra. The caller provides the Zod schema, default values, and a mutation function — nothing else.

### Extracted Zod Schemas

Each page's validation schema moved to `pages/schemas/`:

```
pages/schemas/productEditSchema.ts
pages/schemas/menuCategoryEditSchema.ts
pages/schemas/comboProductEditSchema.ts
```

Keeping schemas in separate files makes them testable in isolation and prevents the page component from growing into a mixed concerns file.

### FormSection and NestedItemList

Two small presentational primitives extracted from the duplicated layout in each edit page:

- **`FormSection`** — titled card wrapper with consistent padding and heading style. Used wherever a form is split into logical groups (Basic Info, Translations, Modifiers).
- **`NestedItemList`** — renders an ordered list of child items (translations, combo components) with Add/Remove controls. Previously each page had its own version of this with slightly different spacing.

### Shared Test Harnesses

MSW setup helpers and the auth-expired test pattern were duplicated across test files. Extracted to:

```
src/test/mswHelpers.ts          — server setup / request interception helpers
src/test/authExpiredHarness.ts  — simulates 401 and asserts redirect
src/test/eventListenerHarness.ts — wraps addEventListener/removeEventListener spying
```

### What Changed

| Area | Files |
|------|-------|
| Hook | `features/admin/forms/useResourceForm.ts` |
| Primitives | `features/admin/forms/FormSection.tsx`, `NestedItemList.tsx` |
| Schemas | `pages/schemas/productEditSchema.ts`, `menuCategoryEditSchema.ts`, `comboProductEditSchema.ts` |
| Pages refactored | `ProductEdit.tsx`, `MenuCategoryEdit.tsx`, `ComboProductEdit.tsx` |
| Test harnesses | `test/mswHelpers.ts`, `test/authExpiredHarness.ts`, `test/eventListenerHarness.ts` |
| Smoke tests | `pages/__tests__/ProductEdit.test.tsx`, `MenuCategoryEdit.test.tsx`, `ComboProductEdit.test.tsx` |

---

## Integration Test Reliability Fix

### The Problem

`DeactivateBrandStaff_LastBrandAdmin_Returns409` was failing intermittently with `NoContent` instead of `Conflict`. The test:

1. Invites an anchor brand admin.
2. Lists all brand admins and deactivates every one except the anchor.
3. Deactivates the anchor — expects 409 because it is now the last.

Step 2 races against other tests in the class (`InviteBrandStaff_BrandAdmin_Returns201`, `InviteBrandStaff_ExistingUserNewRole_Returns201`, etc.) that also add brand admins to the same `alpha` brand. New admins arrive between the list fetch and the individual deactivate calls, so the drain never fully completes.

The existing `[NotInParallel("brand-staff-last-admin")]` attribute only serialised the test against other tests sharing that exact key — no other test had it, so it was effectively a no-op.

`ListBrandStaff_EmptyBrand_Returns200WithEmptyList` (which asserts that the `gamma` brand has zero staff) was also implicitly at risk — currently safe because nothing else writes to `gamma`, but fragile by design.

### TUnit's [NotInParallel]

`[NotInParallel("key")]` prevents a test from running simultaneously with any other test sharing the same key, regardless of class. Tests with different keys (or no attribute) still run in parallel — so other test classes (products, shops, etc.) are unaffected.

Adding `[NotInParallel("brand-staff")]` to **every test** in `BrandStaffEndpointTests` means the 10 staff tests run sequentially with each other while the rest of the suite stays parallel. The drain loop in the last-admin test also simplified back to a single pass — with serialisation guaranteed, no concurrent test can inject a new admin mid-drain.

### What Didn't Work First

A retry loop (keep fetching and draining until the list is empty) was tried first. It reduces the race window but does not close it: a concurrent test can still add an admin in the gap between "list is empty" and the final deactivate call. It treats the symptom, not the cause.
