# 007 — Infrastructure, Auth & Full-Stack Wiring

**Date:** 2026-03-15

## What Was Done

Major infrastructure session: replaced the InMemory database with real SQL Server, added identity/auth model, wired BFF with mock auth, and connected the frontend through the BFF.

### 1. SQL Server via Aspire
- Replaced EF Core InMemory with SQL Server container managed by Aspire
- AppHost registers SQL Server + platform database; connection strings injected automatically
- **PlatformDbContext**: shared platform DB (brands, users, roles, config) with `platform` schema
- **BrandDbContext**: per-brand DB, created dynamically at runtime
- **BrandDbContextFactory**: derives connection string by swapping `Database=brand_{slug}` via `SqlConnectionStringBuilder`
- **BrandDatabaseProvisioner**: Wolverine handler for `BrandCreatedEvent` — creates brand DB + applies migrations
- Auto-migration on startup for platform DB; "Frietjes?" brand seeded in dev mode
- Design-time factory for `BrandDbContext` enables `dotnet ef migrations` CLI

### 2. DateTimeOffset Cleanup
- Fixed all `DateTime` → `DateTimeOffset` violations across Domain, Application, and Infrastructure
- Added `DateTimeOffsetConvention` (EF Core model-building convention) as safety net
- Added `AuditSaveChangesInterceptor` to auto-populate `CreatedAt`/`UpdatedAt` on `IAuditable` entities

### 3. Identity Domain Model
- **PlatformUser** aggregate root: EntraObjectId, Email, DisplayName, IsPlatformAdmin
- **BrandUserRole** entity: maps users to roles within Platform > Brand > Shop hierarchy
- **StaffRole** enum: BrandAdmin, ShopManager, CounterStaff, KitchenStaff, FloorStaff, Customer
- **IdentityService**: user provisioning (idempotent), role assignment, brand staff queries
- **StaffAuthMethod** on Brand entity: configurable per-brand staff auth (EmailPassword, GoogleSso, MicrosoftSso)
- New endpoint: `PUT /api/brands/{slug}/staff-auth`

### 4. BFF with Mock Auth + YARP
- BFF fully wired: cookie auth, mock auth handler, YARP reverse proxy to API
- Mock personas: `platform-admin`, `brand-admin@frietjes`, `counter-staff@frietjes`, `customer`
- BFF endpoints: `/bff/login`, `/bff/logout`, `/bff/user`, `/bff/session/keepalive`
- YARP proxies `/api/*` to API using Aspire service discovery (`https+http://api`)
- Authorization policies: RequirePlatformAdmin, RequireBrandAdmin, RequireStaff, RequireAuthenticated
- CRITICAL log warning if mock auth is active (safety guard)

### 5. Frontend Auth Wiring
- Vite proxy updated: `/api/*` and `/bff/*` → BFF (port 5261), no longer directly to API
- **AuthContext** + **useAuth** hook for global auth state
- **MockAuthProvider**: immediate mock user from env vars, role switcher dev toolbar (bottom-right)
- **BffAuthProvider**: TanStack Query for `/bff/user`, listens for `auth:session-expired` events
- **AuthProviderSwitch**: selects mock vs real based on `VITE_MOCK_AUTH` env var
- **RequireAuth** component: guards POS (staff roles) and admin (brand-admin/platform-admin) routes
- **useSessionKeepalive**: pings BFF every 15 min if user is active
- Axios interceptor: 401 dispatches `auth:session-expired`, 403 dispatches `auth:access-denied`

### 6. Azure Setup Guide
- Created `docs/guides/azure-entra-external-id-setup.md` — step-by-step guide for when Azure subscription arrives
- Covers: tenant creation, app registrations, user flows, social IdPs, per-brand branding, connecting to .NET

## Key Decisions

1. **Single Entra tenant** for the entire platform (not per-brand). Per-brand branding via separate app registrations.
2. **Roles in PlatformDb**, not Entra custom attributes — allows real-time updates without Graph API calls.
3. **YARP** for BFF-to-API proxy (not manual HttpClient) — native Aspire service discovery integration.
4. **Mock auth as default** in dev — full-stack development without Azure dependency.
5. **Frontend talks exclusively to BFF** — never directly to API. BFF is the single entry point.

## What's Blocked on Azure

- Real OIDC login flow (flip `Authentication:UseMockAuth=false`)
- Social SSO (Google, Microsoft)
- Per-brand login branding
- Real JWT token acquisition and forwarding from BFF to API
- ClaimsEnrichmentMiddleware (enriches Entra claims with PlatformDb roles)

## Files Changed

### New files (backend)
- `Domain/Common/IAuditable.cs`
- `Domain/Identity/PlatformUser.cs`, `BrandUserRole.cs`, `StaffRole.cs`, `IPlatformUserRepository.cs`
- `Domain/Brands/StaffAuthMethod.cs`
- `Application/Identity/IIdentityService.cs`, `IdentityService.cs`
- `Application/Multitenancy/IBrandContextAccessor.cs`
- `Infrastructure/Identity/PlatformUserConfiguration.cs`, `BrandUserRoleConfiguration.cs`, `PlatformUserRepository.cs`
- `Infrastructure/Multitenancy/BrandContextAccessor.cs`
- `Infrastructure/Persistence/BrandDbContext.cs`, `BrandDbContextFactory.cs`, `BrandDbContextDesignTimeFactory.cs`, `BrandDatabaseProvisioner.cs`
- `Infrastructure/Persistence/Conventions/DateTimeOffsetConvention.cs`
- `Infrastructure/Persistence/Interceptors/AuditSaveChangesInterceptor.cs`
- `Infrastructure/Persistence/Seeding/PlatformDbSeeder.cs`, `BrandDbSeeder.cs`
- `Api/Middleware/BrandContextMiddleware.cs`
- `Api/Auth/DevPassThroughHandler.cs`
- `Api/Endpoints/Brands/ConfigureStaffAuthEndpoint.cs`
- `Bff/Auth/AuthConstants.cs`, `MockAuthHandler.cs`
- `Bff/Endpoints/BffEndpoints.cs`

### New files (frontend)
- `src/types/auth.ts`
- `src/api/auth.ts`
- `src/auth/AuthContext.tsx`, `BffAuthProvider.tsx`, `MockAuthProvider.tsx`, `AuthProviderSwitch.tsx`, `RequireAuth.tsx`, `useAuth.ts`, `useSessionKeepalive.ts`, `index.ts`
- `.env.development`

### New files (docs)
- `docs/plans/sql-server-aspire.md`, `azure-entra-external-id.md`, `bff-session-management.md`, `frontend-bff-wiring.md`
- `docs/guides/azure-entra-external-id-setup.md`

## Lessons Learned

1. **Mock auth is essential** for frontend-heavy development — role switcher toolbar saves significant time testing different personas.
2. **Aspire service discovery + YARP** is a natural fit — `https+http://api` just works when the AppHost wires `WithReference(api)`.
3. **Database-per-brand** works cleanly with Aspire — register one SQL Server instance, derive brand connection strings at runtime.
4. **Planning all four workstreams upfront** revealed shared concerns (BFF + frontend overlap) and the right execution order.
