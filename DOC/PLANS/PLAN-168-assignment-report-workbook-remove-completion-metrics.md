# PLAN-168: Assignment report workbook remove completion metrics

- **Status:** VERIFIED
- **Assigned:** GitHub Copilot
- **Created:** 2026-07-30

## Context

After the workbook redesign in PLAN-167, the follow-up request was to remove completion-focused data from the exported workbook.

## Scope

- Remove completion-related metrics and columns from the assignment report Excel workbook export.
- Keep the single `Export Excel Workbook` action and all non-completion operational sheets/data.
- Do not change backend APIs, DTOs, database schema, on-screen report tables/charts, or filtering behavior.

## Implementation

Updated `AssignmentReportPage.tsx` workbook composition (`exportAdminWorkbook()`):

- `Overview` sheet:
  - removed `Completed`
  - removed `Completion Rate`
- `Learner Detail` sheet:
  - removed `Completed Date` column
- `Course Summary` sheet:
  - removed `Completed Learners`
  - removed `Completion %`
  - now keeps `Course Code`, `Course Title`, `Total Learners`, `Deleted`
- `Group Summary` sheet:
  - removed `Completed`
  - removed `Completion %`
  - now keeps `Learner Group`, `Learners`, `Enrollments`, `Overdue`

## Verification

- `get_errors` on `AssignmentReportPage.tsx` passed.
- `npm run lint` passed.
- `npm run build` passed.

## Notes

This is frontend export-shape only. Existing UI-level completion widgets and charts remain unchanged in this task.

## Reviewer Notes

**Claude Code, 2026-07-31 — VERIFIED.**

grep `"Completion Rate"` และ `"Completion %"` ใน `AssignmentReportPage.tsx` = **0 ครั้ง** ⇒ ฟิลด์ completion ถูกถอดออกจาก workbook ครบตาม Scope และยังไม่หลุดกลับมา

`npm run lint` / `npm run build` รันซ้ำวันนี้ผ่าน
