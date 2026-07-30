# PLAN-165: Assignment batch report export actions

- **Status:** DONE
- **Assigned:** GitHub Copilot
- **Created:** 2026-07-30

## Context

The QA assignment batch report page at `/admin-react/assignments/{id}/report` needed an obvious way to export the report data.

The page already had client-side row export logic, but the actions were rendered through a generic `ExportMenu` inside the detail controls sidebar. That made the export affordance less consistent with the page's persistent controls pattern.

## Scope

- Keep the export data source client-side from the loaded `AssignmentDashboard` payload.
- Keep both full export and filtered export behavior.
- Support CSV and XLSX.
- Do not change API response shape, database schema, or backend endpoints.

## Implementation

- Replaced the generic `ExportMenu` instances in `AssignmentReportPage` with explicit `ControlAction` rows:
  - Export Excel (All)
  - Export CSV (All)
  - Export Excel (Filtered)
  - Export CSV (Filtered)
- Added a visible `Data Export` section label in the controls sidebar.
- Added per-action loading state with `exportingKey`.
- Disabled filtered exports until a filter/search is applied, preserving the existing `filterBeforeExport` guidance.
- Added the `exportData` label to `ASSIGNMENT_LABELS`.

## Verification

- `get_errors` on `AssignmentReportPage.tsx` and `labels.ts` passed.
- `npm run lint` passed.
- `npm run build` passed.

## Notes

No API contract changed. This is a presentation/control-surface improvement over existing client-side export behavior.