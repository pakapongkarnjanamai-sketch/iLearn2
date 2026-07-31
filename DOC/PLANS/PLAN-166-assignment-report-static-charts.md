# PLAN-166: Assignment report static charts

- **Status:** VERIFIED
- **Assigned:** GitHub Copilot
- **Created:** 2026-07-30

## Context

The QA assignment batch report page at `/admin-react/assignments/{id}/report` showed interactive chart behavior on the `Status Overview` donut and `Completion by Course` bar chart. The requested behavior is no render animation and no black focus frame after click. A tooltip is acceptable as long as it is readable and does not add a heavy hover overlay.

## Scope

- Disable chart render animation for the two assignment report charts.
- Remove the black focus outline after clicking the same charts.
- Keep a readable tooltip with no animated tooltip transition and no hover cursor overlay.
- Do not change report data, filters, table behavior, exports, API contracts, or backend code.

## Implementation

- Updated `AssignmentReportCharts.tsx`:
  - restored Recharts `Tooltip` with local light/readable styles for `StatusDonut`
  - restored Recharts `Tooltip` with local light/readable styles for `CourseCompletionBars`
  - set `cursor={false}` on both tooltips to avoid hover overlays
  - set `isAnimationActive={false}` on both tooltips
  - set `accessibilityLayer={false}` on both Recharts chart roots to prevent click focus frames
  - set `rootTabIndex={-1}` on `Pie` because Recharts defaults the pie-sector wrapper to `tabIndex=0`
  - scoped focus outline suppression to the chart wrappers for defensive coverage
  - set `isAnimationActive={false}` on `Pie`
  - set `isAnimationActive={false}` on `Bar`
  - removed the shared dark `tooltipStyle` usage for these report charts
- Cleaned the touched file's Tailwind diagnostic by replacing `h-[200px]` with equivalent `h-50`.

## Verification

- `get_errors` on `AssignmentReportCharts.tsx` passed.
- `npm run lint` passed.
- `npm run build` passed.

## Notes

This is frontend presentation only. Active filter dimming remains data-driven by the selected filter. Tooltips are intentionally retained, but chart animation and click focus frames are disabled.

## Follow-Up Fix

QA still showed a black focus rectangle after the initial tooltip follow-up. Recharts `Pie` owns a separate `rootTabIndex` prop that defaults to `0`, so the sector wrapper could still receive focus even with chart-level accessibility disabled. The final fix sets `rootTabIndex={-1}` and extends scoped outline suppression to all focused SVG descendants inside the chart wrapper.

## Reviewer Notes

**Claude Code, 2026-07-31 — VERIFIED.**

ตรวจ `AssignmentReportCharts.tsx` พบครบตามที่ Implementation ระบุ:

- `accessibilityLayer={false}` บน chart root (บรรทัด 52)
- `isAnimationActive={false}` สองจุด (บรรทัด 56, 72)
- `rootTabIndex={-1}` บน `Pie` (บรรทัด 73) — จุดนี้คือ root cause จริงของกรอบโฟกัสสีดำที่ QA ยังเจอหลังรอบแรก และถูกบันทึกไว้ในหัวข้อ Follow-Up Fix ของแผนอย่างถูกต้อง

`npm run lint` / `npm run build` รันซ้ำวันนี้ผ่าน. หมายเหตุ: `CourseCompletionBars` ที่แผนนี้แก้ ถูกลบทิ้งภายหลังโดย PLAN-170 ⇒ การแก้ที่ยังมีผลจริงตอนนี้เหลือเฉพาะฝั่ง `StatusDonut`
