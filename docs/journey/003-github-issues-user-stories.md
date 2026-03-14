# 003 — GitHub Issues: User Stories

**Date:** 2026-03-14

## What happened

Created all 70 user stories from the Frietjes Platform PRD as GitHub issues on the repository.

## Process

1. **Reviewed user stories file** — `docs/extract-prd/user-stories/frietjes-platform.md` contains all stories organized by epic, not split by frontend/backend (they're role-based, as user stories should be)
2. **Installed GitHub CLI** — `gh` wasn't available; installed via `winget install GitHub.cli` and authenticated with `gh auth login --web`
3. **Created labels** — Set up a labeling taxonomy:
   - `user-story` type label
   - Priority labels: `priority: must-have`, `priority: should-have`, `priority: could-have` (MoSCoW)
   - 22 epic labels (e.g., `epic: ordering`, `epic: product-management`, `epic: multi-tenant-architecture`)
4. **Bulk-created 70 issues** — Each issue has title (`US-FP-XXX: ...`), full acceptance criteria as checkboxes, and correct labels

## Decisions

- **Stories stay feature-oriented, not split by frontend/backend.** The frontend/backend split happens at the task level during implementation planning, not at the story level. Stories describe user value.
- **MoSCoW priority labels** map directly to the PRD priorities (Must Have, Should Have, Could Have).
- **Epic labels** allow filtering issues by feature area in GitHub's issue list.

## Stats

- 70 issues created (#1–#70)
- 22 epics covered
- Largest epic: Ordering (13 stories), Product Management (9 stories)

## Lessons

- `gh` CLI wasn't on PATH in the bash shell even after install — needed to explicitly add `/c/Program Files/GitHub CLI` to PATH.
- Bulk issue creation benefits from batching to avoid GitHub API rate limits.
- Having a structured user stories markdown file made automated issue creation straightforward — the format was consistent enough to parse and map to labels programmatically.
