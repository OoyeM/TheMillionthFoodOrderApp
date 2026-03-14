# Plan: Frontend Skill Setup

**Date:** 2026-03-14
**Status:** Pending

## Goal
Create the same progressive disclosure skill structure for the frontend that we built for the backend.

## Steps

1. **Create `/frontend-react` skill** with progressive disclosure
   - Main `skill.md` with stack overview table + "When to read which doc" routing
   - Separate doc files:
     - `react.md` — React patterns, component conventions
     - `typescript.md` — strict TS rules, project-specific types
     - `tanstack-query.md` — server state, cache management
     - `vite.md` — build config, dev server
     - `pwa.md` — service worker, offline mode, installability
     - `i18n.md` — NL/FR/DE translation approach
     - `routing.md` — React Router, three-app routing strategy
   - Skip testing doc — use existing `/e2e-testing` skill for Playwright, find or create Vitest skill

2. **Search for existing React/frontend plugins** — check for React + TS Claude Code plugins (like dotnet-claude-kit but for frontend)

3. **Populate key docs** with project-specific content:
   - `pwa.md` — three apps (storefront, in-store POS, CMS admin), offline requirements for in-store
   - `i18n.md` — Belgian languages, translation value objects from API, day-one requirement
   - `routing.md` — how three apps coexist in one codebase

4. **Copy skills to repo** (`.claude/skills/frontend-react/`)

5. **Commit** via `/commit`

6. **Update journal** — append frontend skill setup to session entry
