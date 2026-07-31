# PLAN-172: Assignment Gantt scrollbar at timeline bottom

- **Status:** VERIFIED
- **Assigned:** GitHub Copilot
- **Created:** 2026-07-30

## Context

QA requested that the horizontal scrollbar on `/admin-react/assignments/gantt` sit at the bottom of the timeline area for every timeline zoom. The current `GanttChart` scroller is sized to content height, so when only a few rows are visible the scrollbar sits directly below the last batch row instead of at the bottom of the chart card.

## Scope

- Change only the React Admin assignment Gantt layout.
- Make the `GanttChart` scroll container fill the available chart card height so the browser's horizontal scrollbar is anchored at the bottom of that timeline viewport.
- Preserve sticky header, sticky assignment column, month fit behavior, and existing zoom scale semantics.

## Out of scope

- API/DTO/backend changes.
- New Gantt features such as paging, date filters, dependency arrows, export, or drag scheduling.
- Assignment detail/report pages.

## Contract changes

None.

## Verification

```powershell
cd iLearn.Admin.React
npm run lint
npm run build
```

## Implementer Notes

- Updated `GanttChart` so the timeline scroller is `flex-1`, filling the available chart card height and placing the horizontal scrollbar at the bottom of the timeline viewport instead of directly below the last row.
- Kept the existing sticky header/name column, percentage-based month layout, and zoom scale logic unchanged.
- Verification run: `npm run lint`, `npm run build`.

## Deployment Notes

- Deployed `iLearn.Admin.React` to QA via `tools/deploy-admin-react.ps1`.
- Deploy output: `CopySucceeded=True`, `RobocopyExitCode=3`.
- Smoke checks after deploy:
	- `https://ap-ntc2138-qawb.nikonoa.net/iLearn/admin-react/` = HTTP 200
	- `https://ap-ntc2138-qawb.nikonoa.net/iLearn/admin-react/assignments/gantt` = HTTP 200

## Reviewer Notes

**Claude Code, 2026-07-31 — VERIFIED (ปิดในฐานะ superseded).**

โค้ดที่แผนนี้แก้ (`GanttChart.tsx` / `GanttBar.tsx` / `ganttScale.ts`) ถูกลบทั้งหมดโดย [PLAN-178](./PLAN-178-assignment-gantt-svar-replacement.md) ที่ย้ายไปใช้ `@svar-ui/react-gantt` ⇒ **รีวิว diff ย้อนหลังไม่ได้แล้ว** — ยืนยันจาก `src/pages/assignments/gantt/` ที่เหลือเพียง `AssignmentSvarGanttChart.tsx`, `svarGanttMapping.ts`, `ganttStatus.ts`

หลักฐานเท่าที่มี: lint/build ผ่าน + deploy QA + smoke 200 ตามที่บันทึกไว้ใน Verification ของแผนนี้และใน `DOC/AGENT_LOG.md`; งานขึ้น QA ใช้จริงอยู่ประมาณ 1 วันก่อนถูกแทนที่

ปิดสถานะเพื่อล้างหนี้รีวิว **ไม่ใช่การรับรองรายบรรทัด** — บทเรียนของ track นี้ (172–177 ไล่แก้ alignment 6 รอบไม่จบเพราะ hand-build layout engine เอง) สรุปไว้ใน Context ของ PLAN-178 แล้ว
