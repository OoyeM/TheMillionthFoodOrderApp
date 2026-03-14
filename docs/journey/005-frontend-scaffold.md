# 005 — Frontend Scaffold: React + Multi-App Routing

**Date:** 2026-03-14

## What happened

Scaffolded the React frontend from the patterns defined in the `/frontend-react` skill. The app runs with `pnpm dev` and serves three app variants from one codebase.

## Structure created

```
src/frontend/
├── src/
│   ├── api/client.ts              — Axios instance with brand header injection
│   ├── components/
│   │   ├── AppShell.tsx            — Brand resolution + i18n init wrapper
│   │   ├── AppVariantLayout.tsx    — Routes to storefront/POS/admin by subdomain
│   │   ├── ErrorBoundary.tsx       — Catch-all error UI
│   │   ├── SuspenseWrapper.tsx     — Loading state for lazy routes
│   │   └── useAppVariant.ts       — Detects app variant from hostname
│   ├── features/
│   │   ├── storefront/             — Customer-facing ordering
│   │   ├── pos/                    — In-store touch interface
│   │   └── admin/                  — CMS management panel
│   ├── i18n/
│   │   ├── config.ts              — i18next setup with NL/FR/DE
│   │   └── locales/{nl,fr,de}/    — Translation files
│   ├── router.tsx                  — /{brand}/{lang}/ URL structure
│   ├── types/common.ts            — Shared domain types
│   └── App.tsx / main.tsx         — Entry points
├── vite.config.ts                 — Path aliases, PWA plugin, proxy
├── playwright.config.ts           — E2E test configuration
└── package.json                   — pnpm, scripts, dependencies
```

## Key decisions

- **Three apps, one codebase** — `useAppVariant` hook detects `pos.`, `admin.`, or default (storefront) from `window.location.hostname`. Each variant lazy-loads its own route tree.
- **URL structure: `/{brand}/{lang}/`** — brand and language in the URL path, resolved by AppShell before any routes render.
- **i18n from day one** — NL, FR, DE translation files with `react-i18next`. Language detected from URL, then browser, then default (NL).
- **TanStack Query + Axios** — API client injects `X-Brand` header automatically from the resolved brand context.
- **Vitest + Playwright** — unit tests already pass (`App.test.tsx` covers route rendering and app variant detection).

## Dependencies installed

React 18, react-router-dom 6, TanStack Query 5, axios, i18next, vite 6, vitest, Playwright, vite-plugin-pwa.

## What's not yet wired

- No actual API calls (backend endpoints don't exist yet)
- PWA service worker registered but no caching strategies configured
- No auth/protected routes
- No UI component library chosen yet

## Lessons

1. **Multi-app from one codebase works cleanly** — hostname-based variant detection at the router level keeps feature folders independent.
2. **Scaffold tests with scaffold code** — having `App.test.tsx` pass from the start means CI can enforce tests immediately.
3. **pnpm strict mode** catches phantom dependencies early — worth the occasional `onlyBuiltDependencies` config.
