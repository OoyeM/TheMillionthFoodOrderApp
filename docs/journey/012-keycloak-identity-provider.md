# 012 — Keycloak Identity Provider Integration

**Date:** 2026-03-16

---

## Context

The project was designed around Azure Entra External ID for authentication, but the user's Azure subscription (MSDN/Visual Studio Ultimate) doesn't support creating Entra External ID tenants. We evaluated alternatives and chose Keycloak — free, self-hosted, Docker-based, and uses standard OIDC so migration to any provider later is a config-only change.

## What Changed

### Phase 1: Provider-Neutral Domain Model

Renamed `EntraObjectId` to `ExternalIdentityId` across the entire backend:

- **Domain:** `PlatformUser.EntraObjectId` → `ExternalIdentityId`, factory parameter renamed
- **Repository:** `GetByEntraObjectIdAsync` → `GetByExternalIdentityIdAsync`
- **Application:** `IIdentityService.ProvisionUserAsync` parameter renamed, `UserWithRolesDto` field renamed
- **Infrastructure:** EF configuration updated, max length increased from 36 to 128 (Keycloak UUIDs are 36 chars but other providers may differ)
- **Migration:** Non-destructive `RenameColumn` + `RenameIndex` + `AlterColumn` (manually edited from EF's destructive scaffold)

### Phase 2: Keycloak in Aspire

- Added `Aspire.Hosting.Keycloak` (preview 13.1.2) to AppHost
- Keycloak container runs with persistent volume and realm import
- Realm file: `keycloak/themillionfoodorderapp-realm.json` containing:
  - `bff-client` confidential client (authorization code + PKCE)
  - `PlatformAdmin` realm role
  - 4 test users with deterministic UUIDs matching the PlatformDbSeeder

### Phase 3: BFF OIDC Integration

- Added `AddOpenIdConnect()` with Keycloak configuration (authority, client ID/secret, PKCE)
- Created `ClaimsEnrichmentService` — runs on `OnTokenValidated` to provision user in PlatformDb and enrich claims with roles
- BFF now references Application + Infrastructure layers (for claims enrichment)
- YARP proxy forwards access tokens as Bearer headers via `GetTokenAsync("access_token")`
- Login endpoint issues OIDC challenge when mock auth is disabled
- Logout endpoint performs federated sign-out with Keycloak

### Phase 4: API JWT Bearer Validation

- Added `Microsoft.AspNetCore.Authentication.JwtBearer` to API
- New `Authentication:UseDevPassThrough` toggle (replaces hardcoded `IsDevelopment()` check)
- JWT bearer validates against Keycloak authority when dev pass-through is disabled

### Phase 5: Dev User Seeding

- `PlatformDbSeeder` now seeds 4 test users matching Keycloak realm personas
- Deterministic external identity IDs (`00000000-0000-0000-0000-00000000000{1-4}`) match Keycloak user IDs
- Brand admin gets `BrandAdmin` role for Frietjes

## Auth Toggle Summary

| Setting | Mock Auth | Keycloak |
|---------|-----------|----------|
| `Authentication:UseMockAuth` (BFF) | `true` (default) | `false` |
| `Authentication:UseDevPassThrough` (API) | `true` (default) | `false` |

Both default to dev-friendly mode. Set both to `false` to use real Keycloak auth.

## Keycloak Realm Import Gotchas

Two issues discovered during testing:

1. **`postLogoutRedirectUris` is not a valid field** in Keycloak's `ClientRepresentation`. Post-logout URIs go in `attributes.post.logout.redirect.uris` (pipe-separated or `##`-separated).
2. **Filename must match `{realmname}-realm.json`**. Keycloak enforces this naming convention for directory-based imports. `dev-realm.json` fails; `themillionfoodorderapp-realm.json` works.

## Files Changed (22 files)

**Domain/Application/Infrastructure (rename):** PlatformUser, IPlatformUserRepository, PlatformUserRepository, PlatformUserConfiguration, IIdentityService, IdentityService, PlatformDbSeeder, + EF migration

**Aspire:** AppHost Program.cs, AppHost csproj, keycloak/themillionfoodorderapp-realm.json (new)

**BFF:** Program.cs, csproj, AuthConstants, BffEndpoints, ClaimsEnrichmentService (new), appsettings.json, appsettings.Development.json

**API:** Program.cs, csproj, appsettings.json, appsettings.Development.json

## Next Steps

- Test real OIDC login flow by setting `UseMockAuth=false`
- Keycloak admin UI available at the Aspire-assigned port for manual realm management
- Future: per-brand Keycloak realms, token refresh in YARP, social SSO (Google/Microsoft)
