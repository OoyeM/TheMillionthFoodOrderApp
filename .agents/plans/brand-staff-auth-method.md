# Plan: US-FP-003 — Assign brand-level staff authentication method

## Issue
**As a** Brand Admin, **I want** to configure whether staff authenticate via email/password or SSO (Google/Microsoft), **so that** authentication fits the brand's operational style.

## Existing Backend (already done)
- `StaffAuthMethod` enum: `EmailPassword=0`, `GoogleSso=1`, `MicrosoftSso=2`
- `Brand.StaffAuthMethod` property with default `EmailPassword`
- `Brand.ConfigureStaffAuth(method)` domain method
- `BrandService.ConfigureStaffAuthAsync()` in Application layer
- `ConfigureStaffAuthEndpoint` — `PUT /api/brands/{slug}/staff-auth`
- `BrandResponse` DTO already includes `StaffAuthMethod`
- EF Core column mapping + migration in place

**Backend is complete. No backend changes needed.**

## What's Missing (all frontend)

### Phase 1: Frontend Types & API Client
1. **Add `staffAuthMethod` to `Brand` interface** in `src/frontend/src/types/common.ts`
2. **Add `StaffAuthMethod` type** — `'EmailPassword' | 'GoogleSso' | 'MicrosoftSso'`
3. **Add `configureStaffAuth` API function** in `src/frontend/src/api/brands.ts`
   - `PUT /api/brands/{slug}/staff-auth` with `{ method: number }`

### Phase 2: TanStack Query Hook
4. **Add `useConfigureStaffAuth` mutation hook** in `src/frontend/src/features/admin/hooks/useBrands.ts`
   - Invalidates brand query on success
   - Returns mutation for the PUT endpoint

### Phase 3: Admin UI
5. **Add staff auth configuration section to `BrandEdit.tsx`**
   - Radio group: Email/Password, Google SSO, Microsoft SSO
   - Shows current method on load
   - Switching triggers a confirmation dialog ("Changing authentication method will affect all staff. Continue?")
   - On confirm → calls mutation → shows success toast

6. **Create `ConfirmDialog` component** (if one doesn't exist)
   - Reusable dialog with title, message, confirm/cancel buttons

### Phase 4: i18n
7. **Add translation keys** in NL, FR, DE:
   - `staffAuth.title`, `staffAuth.description`
   - `staffAuth.emailPassword`, `staffAuth.googleSso`, `staffAuth.microsoftSso`
   - `staffAuth.changeWarning`, `staffAuth.confirm`, `staffAuth.cancel`
   - `staffAuth.updated`

### Phase 5: Tests
8. **Unit test** for the `useConfigureStaffAuth` hook
9. **Component test** for the staff auth section in BrandEdit

## Files to Modify/Create
| File | Action |
|------|--------|
| `src/frontend/src/types/common.ts` | Add `staffAuthMethod` to `Brand`, add `StaffAuthMethod` type |
| `src/frontend/src/api/brands.ts` | Add `configureStaffAuth()` function |
| `src/frontend/src/features/admin/hooks/useBrands.ts` | Add `useConfigureStaffAuth` hook |
| `src/frontend/src/features/admin/pages/BrandEdit.tsx` | Add staff auth config section |
| `src/frontend/src/components/ConfirmDialog.tsx` | Create (if needed) |
| `src/frontend/src/i18n/locales/{nl,fr,de}/common.json` | Add translation keys |

## Scope Boundary
- This story is **configuration only** — saving the preference to the database
- Actual enforcement on login screens is **US-FP-039** (separate story)
- No Keycloak/Entra integration needed here
