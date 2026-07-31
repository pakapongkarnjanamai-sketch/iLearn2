# PLAN-176: Assignment Gantt weekend background only on Day zoom

- **Status:** VERIFIED
- **Assigned:** GitHub Copilot
- **Created:** 2026-07-30

## Context

QA requested that Week and Month zooms should not show Saturday/Sunday background shading on `/admin-react/assignments/gantt`. Day zoom should keep weekend shading because it has individual day columns.

## Scope

- Change only the React Admin assignment Gantt layout.
- Keep weekend background shading on Day zoom.
- Remove weekend background shading from Week and Month zoom.
- Preserve Week guide lines, Month boundary guide lines, sticky header/name column, scrollbar behavior, and task bars.

## Out of scope

- API/DTO/backend changes.
- Assignment detail/report pages.
- New Gantt features.

## Contract changes

None.

## Verification

```powershell
cd iLearn.Admin.React
npm run lint
npm run build
```

## Implementer Notes

- Updated `buildRowsBackground()` so weekend shading is built only for Day zoom.
- Week zoom now renders only weekly guide lines with the existing phase alignment.
- Month zoom now renders no row background; its month boundary guide overlay remains unchanged.
- Verification run: `npm run lint`, `npm run build`.

## Deployment Notes

- Deployed `iLearn.Admin.React` to QA via `tools/deploy-admin-react.ps1`.
- Deploy output: `CopySucceeded=True`, `RobocopyExitCode=3`.
- Smoke checks after deploy:
	- `https://ap-ntc2138-qawb.nikonoa.net/iLearn/admin-react/` = HTTP 200
	- `https://ap-ntc2138-qawb.nikonoa.net/iLearn/admin-react/assignments/gantt` = HTTP 200
	- QA HTML references `assets/index-pgVtkzUg.js` and `assets/index-_WMth2Ta.css`.
- Playwright measurement after deploy:
	- Day: row background layers = `2`, weekend color present = `true`.
	- Week: row background layers = `1`, weekend color present = `false`.
	- Month: row background layers = `0`, weekend color present = `false`.

## Reviewer Notes

**Claude Code, 2026-07-31 — VERIFIED (ปิดในฐานะ superseded).**

โค้ดที่แผนนี้แก้ (`GanttChart.tsx` / `GanttBar.tsx` / `ganttScale.ts`) ถูกลบทั้งหมดโดย [PLAN-178](./PLAN-178-assignment-gantt-svar-replacement.md) ที่ย้ายไปใช้ `@svar-ui/react-gantt` ⇒ **รีวิว diff ย้อนหลังไม่ได้แล้ว** — ยืนยันจาก `src/pages/assignments/gantt/` ที่เหลือเพียง `AssignmentSvarGanttChart.tsx`, `svarGanttMapping.ts`, `ganttStatus.ts`

หลักฐานเท่าที่มี: lint/build ผ่าน + deploy QA + smoke 200 ตามที่บันทึกไว้ใน Verification ของแผนนี้และใน `DOC/AGENT_LOG.md`; งานขึ้น QA ใช้จริงอยู่ประมาณ 1 วันก่อนถูกแทนที่

ปิดสถานะเพื่อล้างหนี้รีวิว **ไม่ใช่การรับรองรายบรรทัด** — บทเรียนของ track นี้ (172–177 ไล่แก้ alignment 6 รอบไม่จบเพราะ hand-build layout engine เอง) สรุปไว้ใน Context ของ PLAN-178 แล้ว
