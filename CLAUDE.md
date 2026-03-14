# CLAUDE.md

## Project

TheMillionthFoodOrderApp — a multi-tenant restaurant CMS and ordering platform. First customer: Frietjes? (Belgian fries chain).

## Monorepo Layout

- `src/backend/` — .NET API + BFF, Aspire orchestrator (see its CLAUDE.md)
- `src/frontend/` — React PWA: storefront, POS, admin — one codebase, three apps (see its CLAUDE.md)
- `infra/` — deployment and infrastructure (not yet scaffolded)
- `docs/` — PRD, user stories, dev journal
- `.claude/docs/` — detailed patterns and conventions

## Quick Start

```bash
# Backend (requires .NET 10+ SDK) — API runs on http://localhost:5102
cd src/backend && dotnet run --project TheMillionthFoodOrderApp.AppHost

# Frontend (requires Node 20+, pnpm) — dev server on http://localhost:5173
cd src/frontend && pnpm install && pnpm dev
```

Frontend Vite dev server proxies `/api/*` to `http://localhost:5102` (the API). Start both for full-stack development.

## Domain Concepts

- **Platform > Brand > Shop** hierarchy with database-per-brand isolation
- Brands define product catalog, pricing, theming; shops inherit and can customize (with approval)
- Products: simple, with modifiers, or combos — all support 14 EU allergens and dietary tags
- Orders: online + in-store channels, configurable lifecycle per shop
- Belgian market: VAT (6% takeaway / 21% eat-in), languages (NL, FR, DE)

## Repository

https://github.com/OoyeM/TheMillionthFoodOrderApp.git
