---
name: frontend-react
description: "React frontend patterns and conventions for TheMillionthFoodOrderApp. Covers React, TypeScript, TanStack Query, Vite, PWA, i18n, and routing. Use when working in src/frontend/."
---

# Frontend React Skill

Reference for the React frontend stack. Each component has its own doc — read only what's relevant to the current task.

## Stack Overview

| Component | Purpose | Docs |
|-----------|---------|------|
| React | UI components, hooks, state | [react.md](docs/react.md) |
| TypeScript | Strict typing, project types | [typescript.md](docs/typescript.md) |
| TanStack Query | Server state, cache management | [tanstack-query.md](docs/tanstack-query.md) |
| Vite | Build tool, dev server, config | [vite.md](docs/vite.md) |
| PWA | Service worker, offline, installability | [pwa.md](docs/pwa.md) |
| i18n | NL/FR/DE translations | [i18n.md](docs/i18n.md) |
| Routing | React Router, three-app strategy | [routing.md](docs/routing.md) |
| Vitest | Unit testing | Use `/tdd-workflow` skill |
| Playwright | E2E testing | Use `/e2e-testing` skill |

## When to Read Which Doc

- **Building a new component?** → react.md + typescript.md
- **Fetching or mutating data?** → tanstack-query.md
- **Build config or dev tooling?** → vite.md
- **Offline mode or mobile install?** → pwa.md
- **Adding/editing translations?** → i18n.md
- **Adding routes or new app section?** → routing.md
- **Writing unit tests?** → use `/tdd-workflow` skill
- **Writing E2E tests?** → use `/e2e-testing` skill
