# US-FP-007 Review TODO

Items identified during PR review to address later.

## Medium

- [ ] **Keyboard accessibility on component picker** — `ComboProductCreate.tsx` and `ComboProductEdit.tsx` use `<div onClick>` without `role="button"`, `tabIndex`, or keyboard handlers. Inaccessible for keyboard-only users.
- [ ] **Reorder buttons missing aria-labels** — Up/down arrow buttons in both combo pages have no `aria-label`, meaningless to screen readers.
- [ ] **Domain-level duplicate component validation** — `Product.CreateCombo()` and `UpdateComboItems()` don't reject duplicate component IDs. Relies on DB unique constraint as catch-all. Add `ArgumentException` if duplicates detected.

## Low

- [ ] **Inconsistent error codes between Create and Update** — `CreateComboProductEndpoint` returns 400 for missing components (`KeyNotFoundException`), while `UpdateComboProductEndpoint` returns 404. Standardize to 400 since missing components are validation errors.
- [ ] **Soft-deleted components show as "(unnamed)"** — `GetProductAsync` loads component names via `GetByIdsAsync` which applies the soft-delete filter. Deleted components silently fall back to "(unnamed)" instead of indicating they're unavailable.
