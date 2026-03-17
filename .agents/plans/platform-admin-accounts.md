# Plan: US-FP-061 — Platform Admin Accounts

## Overview

Add management endpoints and frontend UI for platform admin accounts. The domain (`PlatformUser` with `IsPlatformAdmin`, `PromoteToPlatformAdmin()`, `RevokePlatformAdmin()`) and repository already exist. Missing: application service, API endpoints, frontend page.

## Current State

- `PlatformUser` entity has `IsPlatformAdmin`, `PromoteToPlatformAdmin()`, `RevokePlatformAdmin()`
- `IPlatformUserRepository` + implementation exists (CRUD, lookup by external ID)
- `IdentityService` handles provisioning and role management, no platform admin-specific methods
- No repository method to list platform admins or count them
- No API endpoints, no frontend page

## Phase 1: Domain + Repository Extensions

### Step 1: Check/add `IsActive` on `PlatformUser`
- If missing: add `bool IsActive` (default true), `Deactivate()`, `Reactivate()` methods
- Alternative: skip `IsActive`, use `RevokePlatformAdmin()` as deactivation (simpler for MVP)
- Add migration if schema changes needed

### Step 2: Add repository methods
- `GetAllPlatformAdminsAsync()` — all users where `IsPlatformAdmin == true`
- `GetByEmailAsync(email)` — lookup for invite flow
- `CountActivePlatformAdminsAsync()` — for "last admin" guard

### Step 3: Implement repository methods
- EF Core LINQ queries in `PlatformUserRepository`

## Phase 2: Application Service

### Step 4: Create `IPlatformAdminService` + `PlatformAdminService`
- **Files:** `Application/Identity/IPlatformAdminService.cs`, `PlatformAdminService.cs` (new)
- Methods:
  - `ListAsync()` → `IReadOnlyList<PlatformAdminDto>`
  - `InviteAsync(email, displayName)` → creates user or promotes existing → `PlatformAdminDto`
  - `DeactivateAsync(id)` → revokes admin, guards against last admin
- DTO: `PlatformAdminDto(Guid Id, string Email, string DisplayName, bool IsActive, DateTimeOffset CreatedAt)`

### Step 5: Register in DI
- `services.AddScoped<IPlatformAdminService, PlatformAdminService>()`

## Phase 3: API Endpoints

### Step 6: `ListPlatformAdminsEndpoint`
- `GET /api/platform-admins` → returns list of `PlatformAdminDto`

### Step 7: `InvitePlatformAdminEndpoint`
- `POST /api/platform-admins` → `{ email, displayName }` → 201 with DTO
- Validator: email required + valid format, displayName required
- Handle duplicate (409)

### Step 8: `DeactivatePlatformAdminEndpoint`
- `POST /api/platform-admins/{id}/deactivate` → 204
- Handle last admin (409), not found (404)

## Phase 4: Frontend

### Step 9: API client
- **File:** `src/frontend/src/api/platformAdmins.ts` (new)
- `list()`, `invite(data)`, `deactivate(id)`
- Type: `PlatformAdmin { id, email, displayName, isActive, createdAt }`

### Step 10: TanStack Query hooks
- **File:** `src/frontend/src/features/admin/hooks/usePlatformAdmins.ts` (new)
- `usePlatformAdmins()`, `useInvitePlatformAdmin()`, `useDeactivatePlatformAdmin()`

### Step 11: `PlatformAdminList` page
- **File:** `src/frontend/src/features/admin/pages/PlatformAdminList.tsx` (new)
- Table: Display Name, Email, Status badge, Actions (deactivate)
- "Invite Admin" button → inline form or modal (email + display name)
- Deactivate with confirmation dialog
- Follow `BrandList.tsx` patterns

### Step 12: Add route
- In admin routes: `{ path: 'platform-admins', element: <PlatformAdminList /> }`

### Step 13: i18n translations (NL, FR, DE)
- Keys: `admin.platformAdmins.title`, `.invite`, `.email`, `.displayName`, `.deactivate`, `.confirmDeactivate`, `.lastAdminError`, `.empty`

## Phase 5: Integration Tests

### Step 14: `PlatformAdminEndpointTests`
- **File:** `Tests.Integration/PlatformAdmins/PlatformAdminEndpointTests.cs` (new)
- Tests:
  1. `ListPlatformAdmins_ReturnsSeededAdmin`
  2. `InvitePlatformAdmin_Returns201_CreatesUser`
  3. `InvitePlatformAdmin_ExistingUser_PromotesToAdmin`
  4. `InvitePlatformAdmin_InvalidEmail_Returns400`
  5. `DeactivatePlatformAdmin_Returns204`
  6. `DeactivatePlatformAdmin_LastAdmin_Returns409`
  7. `DeactivatePlatformAdmin_NotFound_Returns404`

## Files Summary

| File | Action |
|------|--------|
| `Domain/Identity/PlatformUser.cs` | Modify (if adding IsActive) |
| `Domain/Identity/IPlatformUserRepository.cs` | Modify — add 3 methods |
| `Infrastructure/Identity/PlatformUserRepository.cs` | Modify — implement 3 methods |
| `Application/Identity/IPlatformAdminService.cs` | Create |
| `Application/Identity/PlatformAdminService.cs` | Create |
| `Api/Endpoints/PlatformAdmins/ListPlatformAdminsEndpoint.cs` | Create |
| `Api/Endpoints/PlatformAdmins/InvitePlatformAdminEndpoint.cs` | Create |
| `Api/Endpoints/PlatformAdmins/DeactivatePlatformAdminEndpoint.cs` | Create |
| `frontend/src/api/platformAdmins.ts` | Create |
| `frontend/src/features/admin/hooks/usePlatformAdmins.ts` | Create |
| `frontend/src/features/admin/pages/PlatformAdminList.tsx` | Create |
| `frontend/src/features/admin/routes.tsx` | Modify — add route |
| `frontend/src/i18n/locales/{nl,fr,de}/common.json` | Modify — add translations |
| `Tests.Integration/PlatformAdmins/PlatformAdminEndpointTests.cs` | Create |

## Key Design Decisions

- **Invite flow:** Creates `PlatformUser` with placeholder `externalIdentityId` (`pending:{email}`). Linked to real identity on first OIDC login.
- **Deactivation:** Use `RevokePlatformAdmin()` (already exists). If `IsActive` isn't worth adding, revoking admin role is sufficient for MVP.
- **Auth:** `AllowAnonymous` for now — consistent with all existing endpoints. Auth gating is US-FP-039.
- **URL:** `/api/platform-admins` (platform-level, not brand-scoped)

## Success Criteria
- [ ] `GET /api/platform-admins` returns admin list
- [ ] `POST /api/platform-admins` invites new admin by email
- [ ] `POST /api/platform-admins/{id}/deactivate` revokes admin
- [ ] Cannot deactivate the last platform admin
- [ ] Frontend page with list, invite, and deactivate
- [ ] i18n translations for NL, FR, DE
- [ ] 7 integration tests pass
- [ ] All existing tests still pass
