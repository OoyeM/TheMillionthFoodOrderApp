# PR Review — Unresolved Issues

## Medium: `extractPrimaryLocale` silently falls back on invalid config

**File:** `src/frontend/src/types/common.ts:15`

If `brandSettings.defaultLanguage` is an unexpected value like `"en-US"`, `extractPrimaryLocale` silently returns `'nl'`. This is safe defensive behavior but could mask a misconfigured brand — the admin would see NL marked as required with no indication that the configured primary language is unsupported.

**Suggestion:** Log a `console.warn` in dev when the extracted code is not in `SUPPORTED_LOCALES`, or surface a UI hint.

---

## Low: Duplicate `TabBar` and `ModifierFormRow` components

**Files:**
- `src/frontend/src/features/admin/pages/ModifierGroupCreate.tsx`
- `src/frontend/src/features/admin/pages/ModifierGroupEdit.tsx`

Both files define their own copies of `TabBar` and `ModifierFormRow` with identical interfaces (including the `primaryLocale` prop added in this PR). Pre-existing duplication, but this PR widened both copies.

**Suggestion:** Extract `TabBar` and `ModifierFormRow` into shared components under `src/frontend/src/features/admin/components/`.
