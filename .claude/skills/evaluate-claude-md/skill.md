---
name: evaluate-claude-md
description: "Evaluate CLAUDE.md files for quality before commits. Checks for contradictions, bloat, vague instructions, stale paths, and progressive disclosure violations. Run automatically before any commit."
---

# Evaluate CLAUDE.md Quality

Audit all CLAUDE.md files in the repository for quality issues. Based on progressive disclosure principles from aihero.dev's AGENTS.md guide.

---

## Step 1: Discover All CLAUDE.md Files

```bash
find . -name "CLAUDE.md" -not -path "./.git/*" -not -path "*/node_modules/*"
```

Read every CLAUDE.md found. Also read `.claude/docs/` files if they exist.

---

## Step 2: Evaluate Each File

Score each CLAUDE.md against these criteria. For each issue found, note the file, line, and problem.

### 2.1 Contradictions (CRITICAL)

Scan across ALL CLAUDE.md files (root + nested) for instructions that conflict with each other.

Examples:
- Different approaches for the same task
- Conflicting tech choices or conventions
- Parent file says X, child file says Y without clear override intent

### 2.2 Instruction Budget (HIGH)

Frontier LLMs follow ~150-200 instructions with reasonable consistency. Every token loads on every request.

- Count actionable instructions per file
- Flag files over 50 lines (root) or 40 lines (nested)
- Flag total instruction count across all files if approaching 150

### 2.3 Obvious / Redundant Rules (HIGH)

Flag instructions the model already knows or that add no value:

- "Write clean code", "follow best practices"
- "Use async/await" (default in modern .NET/TS)
- "Functional components only" (React default)
- "Use TypeScript types" (redundant if TS is the stack)
- Anything that restates framework defaults

### 2.4 Vague / Non-Actionable (MEDIUM)

Flag instructions that aren't specific enough to act on:

- No concrete guidance ("handle errors properly")
- Missing context for when it applies
- Subjective without criteria ("keep it simple")

### 2.5 Progressive Disclosure Violations (MEDIUM)

- Root file contains detailed patterns that belong in `.claude/docs/`
- Nested files repeat information from root
- No links to detailed docs when file is getting long

### 2.6 Stale File Paths (LOW)

- References to specific file paths that may not exist
- Prefer documenting domain concepts and capabilities over paths

### 2.7 Missing Essentials (LOW)

Root CLAUDE.md must have:
- One-sentence project description
- Package manager (if not npm)
- Non-standard build/typecheck commands
- Monorepo navigation (if applicable)

Nested CLAUDE.md should have:
- Tech stack (only non-obvious choices)
- Architecture decisions that deviate from convention
- Domain constraints the model can't infer from code

---

## Step 3: Generate Report

Present findings as a table grouped by severity:

```
## CLAUDE.md Evaluation Report

### CRITICAL
| File | Issue | Line(s) | Recommendation |
|------|-------|---------|----------------|

### HIGH
| File | Issue | Line(s) | Recommendation |
|------|-------|---------|----------------|

### MEDIUM
| File | Issue | Line(s) | Recommendation |
|------|-------|---------|----------------|

### LOW
| File | Issue | Line(s) | Recommendation |
|------|-------|---------|----------------|

### Summary
- Files scanned: X
- Total instructions: X / 150 budget
- Issues: X critical, X high, X medium, X low
- Verdict: PASS / NEEDS ATTENTION / FAIL
```

**Verdict rules:**
- **PASS**: No critical, ≤2 high issues
- **NEEDS ATTENTION**: No critical, >2 high issues — present to user, don't block
- **FAIL**: Any critical issues — must be resolved before commit

---

## Step 4: Auto-Fix (with confirmation)

If issues are found:

1. **CRITICAL (contradictions)**: Ask the user which version to keep
2. **HIGH (redundant/obvious)**: Propose deletions, ask for confirmation
3. **MEDIUM (vague, disclosure violations)**: Suggest improvements, apply if user agrees
4. **LOW**: Note for future cleanup, don't block

Never auto-fix without user confirmation.

---

## Step 5: Refactor If Needed

If the evaluation reveals structural problems (file too long, poor organization), offer to run the full refactoring process:

1. Extract essentials for root file
2. Group detailed instructions into `.claude/docs/` topic files
3. Replace verbose sections with links
4. Remove flagged deletions

Present the proposed changes and wait for user approval.
