# Frontend — CLAUDE.md

## Tech Stack

- React + TypeScript, Vite, pnpm
- TanStack Query for server state
- PWA (service worker, installable)

## Architecture

- Three apps from one codebase: customer storefront, in-store POS (touch-friendly), CMS admin panel
- App variant detected from hostname (`pos.`, `admin.`, or default storefront)
- URL structure: `/{brand}/{lang}/...` — brand and language resolved by AppShell
- Admin panel: `/{brand}/{lang}/admin/brands` for brand management
- Feature-based folder structure: `src/features/{storefront,pos,admin}/`
- API client modules in `src/api/` — axios with Vite proxy to BFF (`/api/*` and `/bff/*` → `http://localhost:5261`)
- Auth module in `src/auth/` — AuthContext, providers, route guards, session keepalive
- TanStack Query hooks per feature in `src/features/*/hooks/`
- Strict TypeScript — no `any`
- i18n from day one: NL, FR, DE (react-i18next)

## Authentication

- **MockAuthProvider** (dev default, `VITE_MOCK_AUTH=true`): immediate mock user, role switcher toolbar (bottom-right corner)
- **BffAuthProvider** (real): fetches `/bff/user` via TanStack Query, listens for `auth:session-expired` events
- **AuthProviderSwitch** selects mock vs real based on `VITE_MOCK_AUTH` env var
- **RequireAuth** component guards routes: admin requires `brand-admin`/`platform-admin`, POS requires staff roles, storefront is public
- **useSessionKeepalive** hook pings `/bff/session/keepalive` every 15 min when user is active
- 401 from any API call dispatches `auth:session-expired` window event → auth state resets

## Environment Variables

- `VITE_MOCK_AUTH` — `true` for mock auth (default in dev), `false` for real BFF auth
- `VITE_MOCK_ROLE` — default role for mock auth (`platform-admin`, `brand-admin`, `counter-staff`, `customer`)
- `VITE_MOCK_DISPLAY_NAME` — display name for mock user

## Commands

- `pnpm dev` — start dev server
- `pnpm build` — type-check + production build
- `pnpm test` — run Vitest unit tests
- `pnpm test:e2e` — run Playwright E2E tests
- `pnpm lint` / `pnpm format` — ESLint + Prettier

## Key Constraints

- Offline mode required for in-store interface
- PWA is the mobile strategy (no native apps)

## Testing

- Vitest for unit tests, Playwright for E2E

For detailed patterns, see `.claude/docs/`.
