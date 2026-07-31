# PLAN-165: Assignment batch report export actions

- **Status:** VERIFIED
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

## Reviewer Notes

**Claude Code, 2026-07-31 — VERIFIED (ปิดในฐานะ superseded บางส่วน).**

- ส่วนที่ยังอยู่จริงในโค้ดปัจจุบัน: export ย้ายมาอยู่บน `ControlAction` ใน controls sidebar (`AssignmentReportPage.tsx:386-392`) พร้อมหัวข้อ `Data Export` จาก label `ASSIGNMENT_LABELS.exportData` ⇒ เจตนาหลักของแผน (เลิกใช้ `ExportMenu` generic) สำเร็จและคงอยู่
- ส่วนที่ถูกถอดออกภายหลัง: action `Export CSV (All)` / `Export Excel (Filtered)` / `Export CSV (Filtered)` ถูกลบโดย [PLAN-167](./PLAN-167-assignment-report-admin-workbook-export.md) ในวันเดียวกัน ⇒ **ไม่ต้องรีวิวย้อนหลัง** เพราะไม่มีโค้ดเหลือแล้ว

ข้อสังเกตกระบวนการ: 165 → 167 → 168 → 170 เป็นการกลับลำ 4 รอบภายในวันเดียวบนหน้าเดียว (เพิ่ม export 4 ปุ่ม → เหลือปุ่มเดียว → ถอด metric ออกจาก workbook → ถอด completion UI) — ตรงตามกติกา repo ที่ให้เปิดแผนใหม่ทุกรอบ feedback แต่ถ้ารอบหน้าเจอ pattern แบบนี้ ควรถามขอบเขตให้จบก่อนลงมือ
