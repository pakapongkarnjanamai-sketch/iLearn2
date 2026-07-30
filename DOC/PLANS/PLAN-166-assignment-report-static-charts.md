# PLAN-166: Assignment report static charts

- **Status:** DONE
- **Assigned:** GitHub Copilot
- **Created:** 2026-07-30

## Context

The QA assignment batch report page at `/admin-react/assignments/{id}/report` showed interactive chart behavior on the `Status Overview` donut and `Completion by Course` bar chart. The requested behavior is static charts with no animation and no hover/click feedback.

## Scope

- Disable chart render animation for the two assignment report charts.
- Remove hover tooltip/cursor behavior from the same charts.
- Do not change report data, filters, table behavior, exports, API contracts, or backend code.

## Implementation

- Updated `AssignmentReportCharts.tsx`:
  - removed Recharts `Tooltip` from `StatusDonut`
  - removed Recharts `Tooltip` from `CourseCompletionBars`
  - set `isAnimationActive={false}` on `Pie`
  - set `isAnimationActive={false}` on `Bar`
  - removed now-unused `tooltipStyle` and `tf` imports
- Cleaned the touched file's Tailwind diagnostic by replacing `h-[200px]` with equivalent `h-50`.

## Verification

- `get_errors` on `AssignmentReportCharts.tsx` passed.
- `npm run lint` passed.
- `npm run build` passed.

## Notes

This is frontend presentation only. Active filter dimming remains data-driven by the selected filter; hover/click tooltip feedback is removed.