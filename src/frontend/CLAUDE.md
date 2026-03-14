# Frontend — CLAUDE.md

## Tech Stack

- React + TypeScript, Vite, pnpm
- TanStack Query for server state
- PWA (service worker, installable)

## Architecture

- Three apps from one codebase: customer storefront, in-store POS (touch-friendly), CMS admin panel
- App variant detected from hostname (`pos.`, `admin.`, or default storefront)
- URL structure: `/{brand}/{lang}/...` — brand and language resolved by AppShell
- Feature-based folder structure: `src/features/{storefront,pos,admin}/`
- Strict TypeScript — no `any`
- i18n from day one: NL, FR, DE (react-i18next)

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
