# PLAN-174: Assignment Gantt weekend band alignment

- **Status:** VERIFIED
- **Assigned:** GitHub Copilot
- **Created:** 2026-07-30

## Context

QA found that the Saturday/Sunday background bands on `/admin-react/assignments/gantt` do not line up with the date columns after the recent timeline scrollbar changes. The body rows currently use a pixel-based repeating CSS gradient for weekend shading while the header and bars are positioned from the shared percentage timeline model, so the visual bands can drift from the actual date columns.

## Scope

- Change only the React Admin assignment Gantt layout.
- Replace the pixel-based weekend shading with explicit weekend band overlays computed from `rangeStart` and `totalDays`.
- Keep grid lines, month boundaries, sticky header/name column, and scrollbar behavior unchanged.

## Out of scope

- API/DTO/backend changes.
- New timeline features outside weekend background alignment.
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

- Added `weekendBands` to the Gantt timeline model. Bands are computed from `rangeStart` and `totalDays` as `leftPct`/`widthPct`, so they share the exact same coordinate system as header date cells and bars.
- Replaced the old day-view pixel-phase weekend gradient with percentage-based row background gradients. Grid guide lines remain unchanged, and week/month views can also render aligned weekend bands from the same model.
- Verification run: `npm run lint`, `npm run build`.

## Deployment Notes

- Deployed `iLearn.Admin.React` to QA via `tools/deploy-admin-react.ps1`.
- Deploy output: `CopySucceeded=True`, `RobocopyExitCode=3`.
- Smoke checks after deploy:
	- `https://ap-ntc2138-qawb.nikonoa.net/iLearn/admin-react/` = HTTP 200
	- `https://ap-ntc2138-qawb.nikonoa.net/iLearn/admin-react/assignments/gantt` = HTTP 200
	- QA HTML references `assets/index-BF5mUMMA.js` and `assets/index-_WMth2Ta.css`.

## Reviewer Notes

**Claude Code, 2026-07-31 — VERIFIED (ปิดในฐานะ superseded).**

โค้ดที่แผนนี้แก้ (`GanttChart.tsx` / `GanttBar.tsx` / `ganttScale.ts`) ถูกลบทั้งหมดโดย [PLAN-178](./PLAN-178-assignment-gantt-svar-replacement.md) ที่ย้ายไปใช้ `@svar-ui/react-gantt` ⇒ **รีวิว diff ย้อนหลังไม่ได้แล้ว** — ยืนยันจาก `src/pages/assignments/gantt/` ที่เหลือเพียง `AssignmentSvarGanttChart.tsx`, `svarGanttMapping.ts`, `ganttStatus.ts`

หลักฐานเท่าที่มี: lint/build ผ่าน + deploy QA + smoke 200 ตามที่บันทึกไว้ใน Verification ของแผนนี้และใน `DOC/AGENT_LOG.md`; งานขึ้น QA ใช้จริงอยู่ประมาณ 1 วันก่อนถูกแทนที่

ปิดสถานะเพื่อล้างหนี้รีวิว **ไม่ใช่การรับรองรายบรรทัด** — บทเรียนของ track นี้ (172–177 ไล่แก้ alignment 6 รอบไม่จบเพราะ hand-build layout engine เอง) สรุปไว้ใน Context ของ PLAN-178 แล้ว
