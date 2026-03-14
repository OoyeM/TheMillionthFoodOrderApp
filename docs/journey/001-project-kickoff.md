# 001 — Project Kickoff: PRD, Structure, and Tech Decisions

**Date:** 2026-03-14

## What happened

Started the project from zero. Used Claude Code to generate the product requirements document and user stories, set up the repository structure, made key technology decisions, and configured Claude Code's knowledge and skills for the project.

## Steps taken with Claude

1. **Generated a PRD** — described the app concept (multi-tenant restaurant CMS and ordering platform, first customer: Frietjes?) and had Claude extract a full PRD covering all features, roles, and domain concepts.

2. **Generated user stories** — Claude produced a complete set of user stories from the PRD, organized by epic.

3. **Set up monorepo folder structure** — decided on a monorepo with `src/backend/`, `src/frontend/`, `docs/`, `infra/`, and `.claude/docs/`. Kept it shallow — no deep nesting yet.

4. **Created nested CLAUDE.md files** — three levels of context:
   - Root `CLAUDE.md` — project overview, monorepo navigation, domain concepts
   - `src/backend/CLAUDE.md` — backend-specific tech stack and conventions
   - `src/frontend/CLAUDE.md` — frontend-specific tech stack and conventions

5. **Made technology decisions** iteratively through conversation (see table below).

6. **Set up dev journal** (`docs/journey/`) — a chronological record of how the app is built with Claude, intended as a teaching resource for colleagues.

7. **Applied CLAUDE.md best practices** from aihero.dev's AGENTS.md guide:
   - Trimmed all CLAUDE.md files to be concise (progressive disclosure)
   - Moved detailed patterns to `.claude/docs/` (as they emerge)
   - Removed obvious/redundant rules
   - Documented domain concepts instead of file paths

8. **Created `/evaluate-claude-md` skill** — evaluates CLAUDE.md files for quality before commits. Checks for contradictions, bloat, vague instructions, stale paths, and progressive disclosure violations. Produces a severity-based report with PASS/NEEDS ATTENTION/FAIL verdict.

9. **Updated `/commit` skill** — added Step 3 that runs `/evaluate-claude-md` when CLAUDE.md files are in the changeset. FAIL blocks the commit.

10. **Set up persistent memory** — created memory entries so future sessions know to:
    - Update the dev journal before context clears
    - Keep CLAUDE.md files current with significant decisions
    - Include skills/agents/tooling in journal entries

## Technology decisions

| Decision | Choice | Why |
|----------|--------|-----|
| Package manager (frontend) | **pnpm** | User preference |
| Backend orchestration | **.NET Aspire** | Service defaults, orchestration, local dev experience |
| API-to-frontend layer | **BFF pattern (.NET)** | Handles auth/session management |
| API framework | **FastEndpoints** | One endpoint per class, built-in validation, no controller bloat |
| API documentation | **Swagger/OpenAPI** | Via FastEndpoints Swagger support |
| Domain modeling | **DDD** | Aggregates, entities, value objects, domain events — natural fit for Brand > Shop > Product hierarchy |
| Frontend framework | **React + TypeScript** | Learning project for the developer |
| Build tool | **Vite** | Fast dev server |
| Server state | **TanStack Query** | Cache management, background refetching |
| Database | **EF Core, database-per-brand** | Multi-tenant isolation |
| Testing (backend) | **xUnit** | Standard .NET testing |
| Testing (frontend) | **Vitest + Playwright** | Unit + E2E |

## Skills & tooling created

| Skill | Purpose |
|-------|---------|
| `/evaluate-claude-md` | Quality gate for CLAUDE.md files — runs before commits |
| `/commit` (updated) | Added CLAUDE.md evaluation as Step 3 in commit workflow |

## How Claude was used

- **PRD + user story generation**: Conversational extraction from app concept
- **Folder structure**: Asked for monorepo layout, then scoped it down — minimum structure
- **CLAUDE.md files**: Generated starters, refined through conversation (one tech choice at a time)
- **Best practices research**: Fetched aihero.dev article on AGENTS.md, applied principles to trim CLAUDE.md files
- **Skill creation**: Built evaluate-claude-md skill from article's evaluation criteria, integrated into commit workflow
- **Memory setup**: Configured persistent memory for cross-session continuity

## Lessons learned

1. **Start with minimum structure** — shallow folders, no deep scaffolding. Structure emerges as you build.
2. **Iterative decisions > big upfront design** — each tech choice was a short exchange, not a design doc.
3. **CLAUDE.md quality matters** — treat it like code: keep it small, remove bloat, evaluate before committing.
4. **Record everything** — the dev journal captures not just code decisions but tooling and workflow setup too.
