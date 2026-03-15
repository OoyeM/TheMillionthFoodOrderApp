# Plan: BFF Session/Token Management

## Overview

Wire up the BFF as a secure gateway between React frontend and .NET API. BFF handles OIDC auth with Entra External ID, stores tokens server-side (frontend only sees encrypted session cookie), and proxies API calls via YARP with bearer tokens attached. Mock auth for local dev without Azure.

## Key Design Decisions

- **YARP** for reverse proxy (not manual HttpClient) — first-class ASP.NET Core + Aspire integration
- **Session storage**: in-memory for dev, Redis for prod
- **Token management**: MSAL distributed token cache holds access/refresh tokens; session cookie is just an identifier
- **Cookie config**: HttpOnly, SameSite=Strict, sliding expiration (8 hours)

## BFF Endpoints

| Endpoint | Method | Auth | Purpose |
|----------|--------|------|---------|
| `/bff/login` | GET | Anonymous | OIDC challenge → Entra redirect. `?mock=persona` in dev |
| `/bff/logout` | POST | Authenticated | Sign out cookie + Entra federated sign-out |
| `/bff/user` | GET | Anonymous | Returns user info JSON or `{ isAuthenticated: false }` |
| `/bff/silent-login` | GET | Anonymous | Check session validity without visible redirect |
| `/api/{**catch-all}` | * | Pass-through | YARP proxy to API with bearer token injected |

## Phases

### Phase 1: NuGet + Project Structure
- Add to BFF: `Microsoft.Identity.Web`, `Microsoft.Identity.Web.TokenCache`, `Yarp.ReverseProxy`
- Create folders: `Auth/`, `Endpoints/`, `Proxy/`

### Phase 2: Authentication Middleware (no Azure needed)
- `AuthConstants.cs` — scheme names, role constants, policy names, claim types
- `MockAuthHandler.cs` — dev-only auth handler with predefined personas (platform-admin, brand-admin@frietjes, counter-staff@frietjes, customer, anonymous)
- `ClaimsTransformer.cs` — normalize Entra claims to application claims
- `Program.cs` auth config: mock auth (dev) vs real OIDC (prod), cookie settings

### Phase 3: BFF Endpoints (no Azure needed)
- `GET /bff/login` — OIDC challenge or mock sign-in. Validates `returnUrl` against allowlist (open redirect prevention)
- `POST /bff/logout` — cookie + federated sign-out
- `GET /bff/user` — returns 200 with user info (never 401, storefront needs this)
- `GET /bff/silent-login` — session check

### Phase 4: YARP Proxy
- `TokenTransformProvider.cs` — acquires access token from MSAL cache, attaches as `Authorization: Bearer`. Forwards `X-Brand-Slug` header. Anonymous requests forwarded without token
- YARP config in appsettings: route `/api/{**catch-all}` → cluster `api` (Aspire service discovery `https+http://api`)
- `AddReverseProxy().LoadFromConfig().AddServiceDiscoveryDestinationResolver()`

### Phase 5: Authorization + Brand Context
- Authorization policies on BFF: `/api/admin/*` → RequireBrandAdmin, `/api/pos/*` → RequireStaff, `/api/storefront/*` → anonymous
- `BrandContextMiddleware` — validates authenticated user has access to requested brand

### Phase 6: AppHost + Frontend Integration
- AppHost: `WithExternalHttpEndpoints()` on BFF, pass `Authentication__UseMockAuth=true` env var
- Vite proxy: change `/api` target from API (5102) to BFF (5261), add `/bff` proxy
- Frontend auth utilities: `fetchUser()`, `login()`, `logout()`, `keepalive()`

### Phase 7: Config Files
- `appsettings.json` — OIDC config (placeholder values), YARP routes, session settings
- `appsettings.Development.json` — `UseMockAuth: true`

### Phase 8: API Bearer Validation (coordination)
- API adds `Microsoft.Identity.Web` for JWT bearer validation
- In dev: trust all BFF requests or skip validation (gated on environment)

## Mock Auth Personas

| Persona | Claims |
|---------|--------|
| `platform-admin` | PlatformAdmin role, all brands |
| `brand-admin@frietjes` | BrandAdmin for frietjes |
| `counter-staff@frietjes` | CounterStaff for frietjes |
| `customer` | Customer role |
| `anonymous` | No auth |

## Safety Guards
- Mock auth gated on `IHostEnvironment.IsDevelopment() && config flag`
- CRITICAL log warning if mock auth active
- Startup health check fails in Production + MockAuth
- `returnUrl` validated against allowlist (open redirect prevention)
- MSAL token cache (not session) holds tokens — session cookie is just an ID

## New Files
- `Bff/Auth/AuthConstants.cs`
- `Bff/Auth/MockAuthHandler.cs`
- `Bff/Auth/ClaimsTransformer.cs`
- `Bff/Auth/BrandContextMiddleware.cs`
- `Bff/Endpoints/BffEndpoints.cs`
- `Bff/Proxy/TokenTransformProvider.cs`
