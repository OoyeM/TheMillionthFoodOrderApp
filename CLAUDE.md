# CLAUDE.md

## Project

TheMillionthFoodOrderApp — a multi-tenant restaurant CMS and ordering platform. First customer: Frietjes? (Belgian fries chain).

## Monorepo Layout

- `src/backend/` — .NET API + BFF, Aspire orchestrator (see its CLAUDE.md)
- `src/frontend/` — React PWA: storefront, POS, admin — one codebase, three apps (see its CLAUDE.md)
- `infra/` — deployment and infrastructure (not yet scaffolded)
- `docs/` — PRD, user stories, dev journal, [dependency tree](docs/dependency-tree.md)
- `.claude/docs/` — detailed patterns and conventions

## Quick Start

```bash
# Backend (requires .NET 10+ SDK + Docker Desktop for SQL Server container)
cd src/backend && dotnet run --project TheMillionthFoodOrderApp.AppHost

# Frontend (requires Node 20+, pnpm) — dev server on http://localhost:5173
cd src/frontend && pnpm install && pnpm dev
```

Backend starts API (http://localhost:5102), BFF (http://localhost:5261), SQL Server, and Keycloak via Aspire. Frontend proxies `/api/*` and `/bff/*` to the BFF. Start both for full-stack development.

Mock auth is enabled by default in dev — no external services needed. Visit `/bff/login?mock=brand-admin@frietjes` to sign in with a test persona. Set `Authentication:UseMockAuth=false` to use Keycloak.

## Key Architecture Decisions

- **Database:** Azure SQL with database-per-brand isolation — one SQL Server instance, platform DB for shared data, brand DBs created dynamically at runtime
- **Identity:** Keycloak (self-hosted, Docker) — standard OIDC, runs as Aspire container. Mock auth for local dev. Migrating to Azure Entra External ID or any OIDC provider is a config-only change.
- **Auth pattern:** BFF handles all auth (OIDC, cookies, sessions) — frontend never touches tokens. YARP proxies API calls with bearer tokens.
- **Architecture:** DDD with Clean Architecture, bounded contexts per domain area

## Domain Concepts

- **Platform > Brand > Shop** hierarchy with database-per-brand isolation
- Brands define product catalog, pricing, theming; shops inherit and can customize (with approval)
- Products: simple, with modifiers, or combos — all support 14 EU allergens and dietary tags
- Menu categories: brand-scoped, multilingual, ordered — products assigned to at most one category with configurable display order within each category
- Orders: online + in-store channels, configurable lifecycle per shop
- Belgian market: VAT (6% takeaway / 21% eat-in), languages (NL, FR, DE)

## Planning & User Stories

The [dependency tree](docs/dependency-tree.md) is the **source of truth** for what to build next. It tracks all 70 user stories (US-FP-001 through US-FP-070) across 5 layers with dependency chains and completion status. Always consult it when:
- Deciding which story to work on next (pick stories whose dependencies are all ✅)
- Understanding what a story unlocks downstream
- Planning parallel work across worktrees (4-worktree strategy documented there)

Full user stories: `docs/extract-prd/user-stories/frietjes-platform.md`
PRD: `docs/extract-prd/new-app/frietjes-platform.md`

## Repository

https://github.com/OoyeM/TheMillionthFoodOrderApp.git
