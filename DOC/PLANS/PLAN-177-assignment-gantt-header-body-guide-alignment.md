# PLAN-177: Assignment Gantt header/body guide alignment

- **Status:** VERIFIED
- **Assigned:** GitHub Copilot
- **Created:** 2026-07-30

## Context

QA reported that Day zoom date columns do not line up with the table header on `/admin-react/assignments/gantt`. Playwright measurement on QA showed the header day cells rendering at about `21.984px` while the body grid still used a fixed `22px` repeating gradient. The difference accumulates across many days and becomes visible after horizontal scrolling.

## Scope

- Change only the React Admin assignment Gantt layout.
- Build body guide lines from the same `timeline.ticks[].widthPct` cumulative positions that render the header cells.
- Keep Day weekend shading, remove weekend shading from Week/Month as in PLAN-176, and preserve scrollbar/filler behavior.

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

Manual QA measurement target:

- Day visible header tick offsets match body guide offsets without cumulative pixel drift.
- Week guide lines still align to week tick boundaries.
- Month remains unchanged: no row background, month boundary overlay only.

## Implementer Notes

- Replaced fixed-pixel Day/Week body guide gradients with `buildGuideBackground()`, which derives cumulative guide stops from `timeline.ticks[].widthPct` — the same model used by the header cells.
- Day now layers the percentage guide background over the existing Day-only weekend shading.
- Week now uses percentage guide lines only, with no weekend shading and no pixel phase offset.
- Month remains unchanged: no row background, month boundary overlay only.
- Verification run: `npm run lint`, `npm run build`.

## Deployment Notes

- Deployed `iLearn.Admin.React` to QA via `tools/deploy-admin-react.ps1`.
- Deploy output: `CopySucceeded=True`, `RobocopyExitCode=3`.
- Smoke checks after deploy:
	- `https://ap-ntc2138-qawb.nikonoa.net/iLearn/admin-react/` = HTTP 200
	- `https://ap-ntc2138-qawb.nikonoa.net/iLearn/admin-react/assignments/gantt` = HTTP 200
	- QA HTML references `assets/index-Bafo0cnS.js` and `assets/index-_WMth2Ta.css`.
- Playwright measurement after deploy:
	- Day: `usesFixedPxGuide=false`, `usesPercentCalcGuide=true`, weekend color present = `true`, horizontal scroll = `true`.
	- Week: `usesFixedPxGuide=false`, `usesPercentCalcGuide=true`, weekend color present = `false`, horizontal scroll = `true`.
	- Month: row background layers = `0`, weekend color present = `false`, horizontal scroll = `true`.

## Reviewer Notes

**Claude Code, 2026-07-31 — VERIFIED (ปิดในฐานะ superseded).**

โค้ดที่แผนนี้แก้ (`GanttChart.tsx` / `GanttBar.tsx` / `ganttScale.ts`) ถูกลบทั้งหมดโดย [PLAN-178](./PLAN-178-assignment-gantt-svar-replacement.md) ที่ย้ายไปใช้ `@svar-ui/react-gantt` ⇒ **รีวิว diff ย้อนหลังไม่ได้แล้ว** — ยืนยันจาก `src/pages/assignments/gantt/` ที่เหลือเพียง `AssignmentSvarGanttChart.tsx`, `svarGanttMapping.ts`, `ganttStatus.ts`

หลักฐานเท่าที่มี: lint/build ผ่าน + deploy QA + smoke 200 ตามที่บันทึกไว้ใน Verification ของแผนนี้และใน `DOC/AGENT_LOG.md`; งานขึ้น QA ใช้จริงอยู่ประมาณ 1 วันก่อนถูกแทนที่

ปิดสถานะเพื่อล้างหนี้รีวิว **ไม่ใช่การรับรองรายบรรทัด** — บทเรียนของ track นี้ (172–177 ไล่แก้ alignment 6 รอบไม่จบเพราะ hand-build layout engine เอง) สรุปไว้ใน Context ของ PLAN-178 แล้ว
