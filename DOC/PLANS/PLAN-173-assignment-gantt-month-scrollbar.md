# PLAN-173: Assignment Gantt month scrollbar

- **Status:** VERIFIED
- **Assigned:** GitHub Copilot
- **Created:** 2026-07-30

## Context

After PLAN-172 was deployed to QA, Month zoom still showed no horizontal scrollbar on `/admin-react/assignments/gantt`. The root cause is that Month zoom is still treated as a fit-to-width timeline (`fitsWidth: true`), so the chart has no horizontal overflow even though the scroller itself now fills the bottom of the card.

## Scope

- Change only the React Admin assignment Gantt layout.
- Make Month zoom use a scrollable fixed/minimum timeline width instead of fitting exactly to the card width.
- Keep bar/header/month-guide alignment based on shared percentage positions.
- Keep the scrollbar anchored at the bottom of the chart viewport from PLAN-172.

## Out of scope

- API/DTO/backend changes.
- New Gantt features or behavior outside scrollbar/layout.
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

- Removed Month zoom's fit-to-width behavior from the timeline model. Month now gets a scrollable fixed/minimum `widthPx` (`max(totalDays * 6px, monthSegments * 220px, 1280px)`).
- `GanttChart` now always renders against the fixed timeline width and uses `overflow-x-scroll` so the horizontal scrollbar is present at the bottom of the filled chart viewport.
- Month body guide rendering now keys off `zoom === 'month'` rather than the removed fit-width mode, keeping month boundaries aligned with the header and bars.
- Verification run: `npm run lint`, `npm run build`.

## Deployment Notes

- Deployed `iLearn.Admin.React` to QA via `tools/deploy-admin-react.ps1`.
- Deploy output: `CopySucceeded=True`, `RobocopyExitCode=3`.
- Smoke checks after deploy:
	- `https://ap-ntc2138-qawb.nikonoa.net/iLearn/admin-react/` = HTTP 200
	- `https://ap-ntc2138-qawb.nikonoa.net/iLearn/admin-react/assignments/gantt` = HTTP 200
	- QA HTML references `assets/index-CYKbRfZ1.js` and `assets/index-_WMth2Ta.css`.

## Reviewer Notes

**Claude Code, 2026-07-31 — VERIFIED (ปิดในฐานะ superseded).**

โค้ดที่แผนนี้แก้ (`GanttChart.tsx` / `GanttBar.tsx` / `ganttScale.ts`) ถูกลบทั้งหมดโดย [PLAN-178](./PLAN-178-assignment-gantt-svar-replacement.md) ที่ย้ายไปใช้ `@svar-ui/react-gantt` ⇒ **รีวิว diff ย้อนหลังไม่ได้แล้ว** — ยืนยันจาก `src/pages/assignments/gantt/` ที่เหลือเพียง `AssignmentSvarGanttChart.tsx`, `svarGanttMapping.ts`, `ganttStatus.ts`

หลักฐานเท่าที่มี: lint/build ผ่าน + deploy QA + smoke 200 ตามที่บันทึกไว้ใน Verification ของแผนนี้และใน `DOC/AGENT_LOG.md`; งานขึ้น QA ใช้จริงอยู่ประมาณ 1 วันก่อนถูกแทนที่

ปิดสถานะเพื่อล้างหนี้รีวิว **ไม่ใช่การรับรองรายบรรทัด** — บทเรียนของ track นี้ (172–177 ไล่แก้ alignment 6 รอบไม่จบเพราะ hand-build layout engine เอง) สรุปไว้ใน Context ของ PLAN-178 แล้ว
