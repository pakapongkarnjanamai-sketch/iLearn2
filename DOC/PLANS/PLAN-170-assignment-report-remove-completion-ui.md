# PLAN-170: Assignment report remove completion UI

- **Status:** VERIFIED
- **Assigned:** GitHub Copilot
- **Created:** 2026-07-30

## Context

After removing completion-focused fields from workbook export, the next request was to remove completion-focused elements from the on-screen Assignment Report UI as well.

## Scope

- Remove completion-oriented UI elements from Assignment Report page.
- Keep report data loading, filters, export action, and backend contracts unchanged.
- Preserve shared chart component compatibility for other pages.

## Implementation

Updated `AssignmentReportPage.tsx`:

- Removed completion KPI tile from summary cards.
- Removed print-only `Completion` row.
- Removed `Completion by Course` chart section from report UI.
- Removed completion columns from learner group table (`Completed`, `Completion`).
- Replaced learner table right-most column from `Completed Date` to `Due Date`.
- Cleaned imports and removed unused completion-chart dependencies.

Updated `AssignmentReportCharts.tsx`:

- Removed `CourseCompletionBars` and `buildCourseBarData` exports (no longer needed by report page).
- Simplified chart module to status-donut only for this page path.
- Kept `StatusDonut` backward compatible by making `completionRate` optional:
  - when provided (other pages), center displays completion percent
  - when omitted (report page), center displays total enrollments

## Verification

- `get_errors` on touched files passed.
- `npm run lint` passed.
- `npm run build` passed.

## Notes

This is frontend presentation-only behavior. No API/DTO/DB changes.

## Reviewer Notes

**Claude Code, 2026-07-31 — VERIFIED.**

- grep `CourseCompletionBars` = **0 ครั้ง** ทั้งใน `AssignmentReportCharts.tsx` และ `AssignmentReportPage.tsx` ⇒ ลบครบ ไม่เหลือ export ที่ไม่มีคนเรียก
- ข้อที่ตั้งใจดีที่สุดของแผนนี้ตรวจแล้วทำจริง: `StatusDonut` รักษา backward compatibility ด้วย `completionRate?: number` (บรรทัด 36) แล้วเลือกสิ่งที่แสดงตรงกลางโดนัทจาก `completionRate !== undefined` (บรรทัด 87) ⇒ หน้าอื่นที่ยังส่ง prop นี้ไม่พัง
- `npm run lint` / `npm run build` รันซ้ำวันนี้ผ่าน
