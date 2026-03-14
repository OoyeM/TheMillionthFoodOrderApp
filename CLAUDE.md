# CLAUDE.md

## Project

TheMillionthFoodOrderApp — a multi-tenant restaurant CMS and ordering platform. First customer: Frietjes? (Belgian fries chain).

## Monorepo Layout

- `src/backend/` — .NET API + BFF (see its CLAUDE.md)
- `src/frontend/` — React PWA: storefront, in-store POS, CMS admin (see its CLAUDE.md)
- `infra/` — deployment and infrastructure
- `docs/` — PRD, user stories, dev journal
- `.claude/docs/` — detailed patterns and conventions

## Domain Concepts

- **Platform > Brand > Shop** hierarchy with database-per-brand isolation
- Brands define product catalog, pricing, theming; shops inherit and can customize (with approval)
- Products: simple, with modifiers, or combos — all support 14 EU allergens and dietary tags
- Orders: online + in-store channels, configurable lifecycle per shop
- Belgian market: VAT (6% takeaway / 21% eat-in), languages (NL, FR, DE)

## Repository

https://github.com/OoyeM/TheMillionthFoodOrderApp.git
