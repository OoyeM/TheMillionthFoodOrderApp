# 001 — Project Kickoff: PRD, Structure, Tech Decisions, and Skills Setup

**Date:** 2026-03-14

## What happened

Started the project from zero. Used Claude Code to generate the PRD and user stories, set up the monorepo structure, made key technology decisions iteratively, created project-specific skills with progressive disclosure, installed DDD plugins, and established a dev journal workflow.

## Steps taken with Claude

1. **Generated a PRD** — described the app concept and had Claude extract a full PRD covering all features, roles, and domain concepts.

2. **Generated user stories** — Claude produced a complete set of user stories from the PRD, organized by epic.

3. **Set up monorepo folder structure** — `src/backend/`, `src/frontend/`, `docs/`, `infra/`, `.claude/docs/`. Kept it shallow.

4. **Created nested CLAUDE.md files** — root (project overview + domain concepts), backend (tech stack + conventions), frontend (tech stack + constraints). Applied progressive disclosure principles from aihero.dev's AGENTS.md guide.

5. **Made technology decisions** iteratively — one at a time through conversation (see table below).

6. **Set up dev journal** (`docs/journey/`) — chronological record for teaching colleagues.

7. **Created `/evaluate-claude-md` skill** — quality gate for CLAUDE.md files (contradictions, bloat, vague rules, progressive disclosure violations). Integrated into `/commit` workflow as Step 3.

8. **Created `/backend-dotnet` skill** with progressive disclosure — one main skill.md routing table + 9 separate doc files for each stack component. Empty Patterns/Gotchas scaffolds to fill as we build.

9. **Installed DDD plugins** — `dotnet-claude-code-skills` (nesbo) for aggregates/handlers/repos and `dotnet-claude-kit` (codewithmukesh) for comprehensive .NET DDD tooling.

10. **Populated DDD doc** — mapped 9 bounded contexts to the restaurant domain, defined aggregate rules, entity vs value object decisions, domain events, approval workflow, multi-tenant considerations, Belgian constraints.

11. **Copied skills into repo** (`.claude/skills/`) — repo is source of truth for project-specific skills. After editing a skill in the repo, Claude asks "Promote to global?" to sync to `~/.claude/skills/`.

12. **Set up persistent memory** for cross-session continuity:
    - Update dev journal before context clears
    - Keep CLAUDE.md files current with significant decisions
    - Include skills/agents/tooling in journal entries
    - Remind user to update journal before `/clear`
    - Edit skills in repo first, promote to global on request

## Technology decisions

| Decision | Choice |
|----------|--------|
| Package manager (frontend) | pnpm |
| Backend orchestration | .NET Aspire |
| API-to-frontend layer | BFF (.NET) — auth/session management |
| API framework | FastEndpoints |
| API documentation | Swagger/OpenAPI |
| Domain modeling | DDD (aggregates, entities, value objects, domain events) |
| Frontend framework | React + TypeScript |
| Build tool | Vite |
| Server state | TanStack Query |
| Database | EF Core, database-per-brand |
| Messaging | MassTransit (in-memory locally, RabbitMQ/Azure SB in prod) |
| Testing (backend) | xUnit |
| Testing (frontend) | Vitest + Playwright |

## Skills & tooling created

| Skill/Plugin | Location | Purpose |
|-------------|----------|---------|
| `/backend-dotnet` | `.claude/skills/backend-dotnet/` | Stack reference with progressive disclosure docs |
| `/evaluate-claude-md` | `.claude/skills/evaluate-claude-md/` | CLAUDE.md quality gate before commits |
| `/commit` (updated) | `~/.claude/skills/commit/` | Added CLAUDE.md eval step + renumbered |
| `dotnet-claude-code-skills` | plugin (nesbo) | DDD patterns for .NET |
| `dotnet-claude-kit` | plugin (codewithmukesh) | Comprehensive .NET dev tooling |

## DDD bounded contexts identified

Tenant Management, Catalog, Ordering, Pricing, Identity, Fulfillment, Scheduling, Loyalty, Analytics

## How Claude was used

- **PRD + user story generation**: Conversational extraction from app concept
- **Iterative tech decisions**: One choice at a time, Claude updated relevant CLAUDE.md after each
- **Best practices research**: Fetched aihero.dev article, applied progressive disclosure principles
- **Skill creation**: Built evaluate-claude-md and backend-dotnet skills from scratch
- **Plugin research**: Agent searched GitHub for DDD skills, found and installed two plugins
- **DDD domain mapping**: Claude mapped PRD features to bounded contexts, aggregates, and events
- **Memory setup**: Configured persistent memory for cross-session workflow continuity

## Lessons learned

1. **Start with minimum structure** — shallow folders, no deep scaffolding. Structure emerges as you build.
2. **Iterative decisions > big upfront design** — each tech choice was a short exchange.
3. **CLAUDE.md quality matters** — treat it like code: keep it small, evaluate before committing.
4. **Progressive disclosure for skills** — one routing table + separate docs per component. Only load what's relevant.
5. **Search before building** — found good DDD plugins instead of writing everything from scratch.
6. **Skills in the repo** — version-controlled, visible in git history, part of the teaching story.
7. **No hook for /clear** — must manually update journal before clearing context. Routine: commit → update journal → clear.
