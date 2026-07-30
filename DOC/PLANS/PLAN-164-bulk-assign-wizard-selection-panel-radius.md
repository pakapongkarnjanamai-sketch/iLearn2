# PLAN-164: Bulk Assign wizard selection panel radius hardening

- **Status:** DONE
- **Assigned:** GitHub Copilot
- **Created:** 2026-07-30

## Context

QA showed a visual radius defect in the `Selected Courses` panel on `/admin-react/assignments/bulk`.

The local root cause was not data or browser rendering. The panel hand-rolled a rounded bordered container with a colored header, but the pattern was not owned by a shared primitive. That allowed two drift risks:

1. A child header background can visually square off or bleed past rounded corners unless the outer frame clips children with `overflow-hidden`.
2. The paired `Syllabus Catalog` and `Selected Courses` panels used slightly different radius classes (`rounded` vs `rounded-lg`).

`Card` already encodes this rule with `overflow-hidden`, but the Bulk Assign wizard did not use a shared panel primitive for this selection layout.

## Scope

- Add a small shared `WizardSelectionPanel` component for wizard selection/list panels.
- Move both Bulk Assign course panels (`Syllabus Catalog`, `Selected Courses`) to the shared component.
- Keep behavior unchanged: course filtering, search, add/remove, selected count, and clear action stay in `BulkAssignPage`.

## Implementation

- Added `iLearn.Admin.React/src/components/ui/WizardSelectionPanel.tsx`.
- The component owns the durable visual contract:
  - `rounded-lg`
  - `border border-slate-200`
  - `bg-white`
  - `overflow-hidden`
  - shared header structure
  - count badge via `Badge`
  - optional toolbar and actions
  - scrollable body with `min-h-0`
- Refactored `BulkAssignPage` to use this component for both course-selection columns.
- Replaced `sm:max-w-[180px]` with equivalent `sm:max-w-45` to clear the editor diagnostic in the touched file.

## Verification

- `get_errors` on `BulkAssignPage.tsx` and `WizardSelectionPanel.tsx` passed.
- `npm run lint` passed.
- `npm run build` passed.

## Follow-Up

- If another wizard needs a selectable left/right list panel, use `WizardSelectionPanel` instead of copying rounded-container/header markup.