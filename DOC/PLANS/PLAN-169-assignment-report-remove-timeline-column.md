# PLAN-169: Assignment report remove Timeline column

- **Status:** VERIFIED
- **Assigned:** GitHub Copilot
- **Created:** 2026-07-30

## Context

On the assignment report learner table, the `Timeline` column repeats similar scheduling information across rows and was requested to be removed to reduce visual noise.

## Scope

- Remove the `Timeline` column from the learner detail table in assignment report page.
- Keep all existing filters, status/progress/completed date behavior, exports, charts, and backend contracts unchanged.

## Implementation

Updated `AssignmentReportPage.tsx`:

- Removed `Timeline` header cell from learner table.
- Removed row cell that rendered `Start Date` / `Due Date` timeline text.
- Updated empty-state table `colSpan` from `6` to `5`.
- Removed now-unused `tf` import from labels.

## Verification

- `get_errors` on `AssignmentReportPage.tsx` passed.
- `npm run lint` passed.
- `npm run build` passed.

## Notes

This is frontend presentation-only refinement. No API/DTO/DB changes.

## Reviewer Notes

**Claude Code, 2026-07-31 — VERIFIED.**

grep `ASSIGNMENT_LABELS.timeline` ใน `AssignmentReportPage.tsx` = **0 ครั้ง** ⇒ คอลัมน์ Timeline ถูกถอดจริง (label ยังอยู่ใน `labels.ts` เพราะหน้าอื่นใช้ — ไม่ใช่ dead code)

`npm run lint` / `npm run build` รันซ้ำวันนี้ผ่าน — ยืนยันว่าการปรับ `colSpan` 6→5 และการถอด import `tf` ไม่ทิ้ง TS error ค้าง
