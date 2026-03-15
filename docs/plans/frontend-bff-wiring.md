# Plan: Wire Frontend to BFF

## Overview

Connect the React frontend to the BFF for auth and API proxying. Frontend talks exclusively to the BFF — never directly to the API. Mock auth provider for dev, real BFF auth provider for production. Both implement the same interface.

## Current State
- Vite proxies `/api/*` → `http://localhost:5102` (API directly)
- Axios client has `withCredentials: true` and `X-Brand-Slug` interceptor
- No auth context, no route guards, no BFF endpoints in frontend

## Phases

### Phase 1: BFF Backend Endpoints (prerequisite)
- Add YARP to BFF for `/api/*` proxy to API
- BFF dev login: `POST /bff/login` accepts `{ role, displayName, brandSlug }`
- BFF endpoints: `/bff/user`, `/bff/logout`, `/bff/session/keepalive`
- Cookie auth with sliding expiration

### Phase 2: Vite Proxy + Environment Config
- Change `/api` proxy target: `http://localhost:5102` → `http://localhost:5261` (BFF)
- Add `/bff` proxy target: `http://localhost:5261`
- Create `.env.development`: `VITE_MOCK_AUTH=true`, `VITE_MOCK_ROLE=platform-admin`
- Update `env.d.ts` with new env var types

### Phase 3: Auth Types + API Module
- `types/auth.ts` — `UserRole`, `AuthUser`, `AuthState`, `DevLoginRequest`
- `api/auth.ts` — `getUser()`, `devLogin()`, `logout()`, `keepalive()` via separate `bffClient`

### Phase 4: Auth Context + Providers
- `AuthContext.tsx` — context with `user`, `isAuthenticated`, `isLoading`, `login()`, `logout()`, `hasRole()`, `hasAnyRole()`
- `BffAuthProvider.tsx` — real provider using TanStack Query to fetch `/bff/user`. Handles 401 as "not authenticated" (not error). Listens for `auth:session-expired` window events
- `MockAuthProvider.tsx` — immediate mock user from env vars. Includes dev toolbar (bottom-right) with role switcher dropdown
- `AuthProviderSwitch.tsx` — renders Mock or Bff provider based on `VITE_MOCK_AUTH`
- `useAuth.ts` — hook consuming context

### Phase 5: Route Protection
- `RequireAuth.tsx` — loading → login prompt → access denied → render children
  - POS: `roles={['counter-staff', 'floor-staff', 'kitchen-staff', 'shop-manager']}`
  - Admin: `roles={['brand-admin', 'platform-admin']}`
  - Storefront: no guard (public)
- Update `router.tsx` with guards on POS and admin route groups

### Phase 6: API Client Integration
- Add axios response interceptor: 401 → dispatch `auth:session-expired` event, 403 → dispatch `auth:access-denied`
- BffAuthProvider listens for `auth:session-expired` → invalidates user query

### Phase 7: Session Keepalive
- `useSessionKeepalive.ts` — sends `POST /bff/session/keepalive` every 15 min if user active (debounced mouse/key events). Only runs when authenticated and not in mock mode
- Wire into BffAuthProvider

### Phase 8: App.tsx Wiring
- Component tree: `ErrorBoundary > QueryClientProvider > AuthProviderSwitch > RouterProvider`
- Update AppShell: verify brand slug from auth matches URL brand slug

### Phase 9: Documentation
- Update frontend/backend/root CLAUDE.md files
- Add journal entry

## New Frontend Files
- `.env.development`
- `src/types/auth.ts`
- `src/api/auth.ts`
- `src/auth/AuthContext.tsx`
- `src/auth/BffAuthProvider.tsx`
- `src/auth/MockAuthProvider.tsx`
- `src/auth/AuthProviderSwitch.tsx`
- `src/auth/RequireAuth.tsx`
- `src/auth/useAuth.ts`
- `src/auth/useSessionKeepalive.ts`
- `src/auth/index.ts`

## Modified Frontend Files
- `vite.config.ts` — proxy targets
- `src/env.d.ts` — env var types
- `src/App.tsx` — wrap with AuthProviderSwitch
- `src/router.tsx` — RequireAuth guards
- `src/api/client.ts` — 401/403 interceptor
- `src/components/AppShell.tsx` — brand/auth validation

## Dependency Graph
```
Phase 1 (BFF backend) → Phase 2 (Vite proxy) → Phase 3 (types/API)
    → Phase 4 (providers) → Phase 5 (guards) → Phase 6 (interceptor)
    → Phase 7 (keepalive) → Phase 8 (wiring) → Phase 9 (docs)
```

## Risks
- Aspire dynamic ports for BFF — pin with `.WithEndpoint()` or use `VITE_BFF_URL` env var
- Cookie SameSite/Secure in dev — Vite proxy makes it same-origin, set `SameSite=Lax`, `Secure=false`
- TanStack Query retries on 401 — set `retry: false` on auth user query
- Mock auth divergence — both providers implement same `AuthContextValue` interface
