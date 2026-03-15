# Plan: Azure Entra External ID Integration

## Overview

Integrate Azure Entra External ID (formerly Azure AD B2C) as the identity provider. Single Entra tenant for the entire platform. Supports local username/password + Microsoft/Google SSO. BFF handles all OIDC flows.

## Architecture Decision: Single Tenant

One Entra External ID tenant, not one per brand. Per-brand branding achieved via separate app registrations or custom policies. User email uniqueness is tenant-wide (desirable for account linking).

## Roles and Permissions

```
PlatformAdmin                     — manages all brands
  BrandAdmin (scoped to brand)    — manages one brand
    ShopManager (scoped to shop)  — manages one shop
    CounterStaff (scoped to shop) — POS order taking
    KitchenStaff (scoped to shop) — kitchen display
    FloorStaff (scoped to shop)   — floor operations
  Customer (scoped to brand)      — places orders
```

Roles stored in PlatformDb (not Entra custom attributes) — allows real-time updates without Graph API calls.

## Token Claims Strategy

**From Entra**: `sub`, `email`, `name`, `idp`
**Enriched by BFF** (from PlatformDb lookup): `platform_role`, `brand_roles` (JSON array), `active_brand`, `active_shop`

## Domain Model (Platform DB)

- `PlatformUser` — Id, EntraObjectId (unique), Email, DisplayName, IsPlatformAdmin, CreatedAt, UpdatedAt
- `BrandUserRole` — Id, PlatformUserId (FK), BrandId (FK), ShopId? (FK), Role (enum), CreatedAt
- `StaffRole` enum — BrandAdmin, ShopManager, CounterStaff, KitchenStaff, FloorStaff, Customer

## Phases

### Phase 1: Azure Setup Documentation (no Azure needed)
- Create `docs/guides/azure-entra-external-id-setup.md` with step-by-step:
  - Create Entra External ID tenant
  - Register BFF app (confidential client, web platform)
  - Register API app (expose `access_as_user` scope)
  - User flows: sign-up/sign-in, password reset, profile edit
  - Social IdPs: Google, Microsoft configuration
  - Per-brand branding options (separate app registrations vs custom policies)
  - Custom attribute: `preferred_language` (nl/fr/de)

### Phase 2: Domain Model (no Azure needed)
- `PlatformUser.cs` — aggregate root with factory method
- `BrandUserRole.cs` — entity with role enum
- `StaffRole.cs` — enum
- `IPlatformUserRepository.cs` — repository interface

### Phase 3: Infrastructure (no Azure needed)
- EF Core configurations for PlatformUser + BrandUserRole
- PlatformUserRepository implementation
- Update PlatformDbContext with new DbSets
- Register in DI

### Phase 4: BFF Authentication (partially needs Azure)
- Add NuGet: `Microsoft.Identity.Web`, `Yarp.ReverseProxy`
- BFF appsettings with OIDC config (placeholder values)
- BFF Program.cs: OIDC middleware, cookie auth, YARP proxy
- Auth endpoints: `/bff/login`, `/bff/logout`, `/bff/me`, `/bff/switch-brand`
- ClaimsEnrichmentMiddleware: lookup user roles in PlatformDb after OIDC auth
- **DevAuthenticationHandler**: mock auth for local dev (bypasses OIDC)

### Phase 5: API Authorization (no Azure needed)
- Authorization policies: PlatformAdmin, BrandAdmin, BrandStaff, ShopStaff
- BrandAuthorizationHandler: reads `X-Brand-Slug` + `brand_roles` claim
- Wire into Api Program.cs

### Phase 6: Frontend Auth (no Azure needed for structure)
- AuthContext.tsx + useAuth() hook
- ProtectedRoute.tsx wrapper
- Update router with guards (admin = brand-admin, POS = staff)
- Update Vite proxy for `/bff/*`
- Login/logout UI in AppShell

### Phase 7: Aspire Orchestration
- Update AppHost to pass Entra config as parameters/env vars
- BFF marked as external-facing endpoint

### Phase 8: Application Layer
- IIdentityService interface (provision user, assign/remove roles)
- IdentityService implementation

### Phase 9: Brand Auth Config (extends Brand aggregate)
- `StaffAuthMethod` enum (EmailPassword, GoogleSso, MicrosoftSso)
- `Brand.ConfigureStaffAuth()` method
- `PUT /api/brands/{slug}/staff-auth` endpoint (US-FP-003)

## What Can Be Built Without Azure

| Phase | Azure Required? |
|-------|----------------|
| Phase 1: Setup docs | No |
| Phase 2: Domain model | No |
| Phase 3: Infrastructure | No |
| Phase 4: BFF auth | Partially (DevAuthHandler enables dev) |
| Phase 5: API authorization | No |
| Phase 6: Frontend auth | No (mock provider) |
| Phase 7-9 | No |

## Azure Portal Checklist (when subscription available)
- [ ] Create Entra External ID tenant
- [ ] Register BFF app + API app
- [ ] Create user flows
- [ ] Register Google + Microsoft as social IdPs
- [ ] Configure per-brand branding
- [ ] Store client secret in Key Vault / user-secrets
- [ ] Test end-to-end login flow
