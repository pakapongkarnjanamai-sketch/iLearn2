# PLAN-175: Assignment Gantt spacing audit

- **Status:** VERIFIED
- **Assigned:** GitHub Copilot
- **Created:** 2026-07-30

## Context

After the Month scrollbar and weekend-band fixes were deployed to QA, the Gantt page still needed a broader visual pass for spacing and alignment. Playwright measurements on QA at `1429x768` found:

- The timeline scroller is correctly anchored to the bottom, but the grid content stops above it, leaving about `70px` of blank space in Day/Week and about `98px` in Month between the last row and the scrollbar.
- Weekend background shading is now computed from the right timeline model, but `buildWeekendBackground()` returns many comma-separated background layers. In Week zoom, `background-position` only declares two positions, so the browser repeats them across all layers and can shift alternating weekend layers.

## Scope

- Change only the React Admin assignment Gantt layout.
- Fill the empty vertical space below task rows with a grid filler row that continues the same timeline background to the scrollbar.
- Collapse weekend shading into a single percentage-based gradient layer so background positioning cannot alternate across bands.
- Keep scrollbar placement, sticky header/name column, task row height, and API behavior unchanged.

## Out of scope

- API/DTO/backend changes.
- New Gantt features.
- Assignment detail/report pages.

## Contract changes

None.

## Verification

```powershell
cd iLearn.Admin.React
npm run lint
npm run build
```

Manual QA measurement target:

- Day/Week/Month have horizontal scroll when expected.
- Blank space below the final task row is replaced by continued timeline background, with weekend bands/grid guides aligned through to the scrollbar.

## Implementer Notes

- Collapsed weekend shading into one percentage-based gradient layer instead of many comma-separated gradient layers. This prevents Week zoom's `background-position` from being repeated/alternated across individual weekend bands.
- Added a final filler grid row below the task rows. The timeline filler uses the same `rowsBackground` as task rows and the left frozen filler keeps the name column white, so the space down to the bottom scrollbar is intentional chart surface instead of a blank gap.
- Set the timeline inner wrapper and grid to a definite `height: 100%` so the filler row's `minmax(0, 1fr)` track expands. A first attempt with `minHeight: 100%` measured as `0px` filler height and was corrected before final deploy.
- Extended Month boundary guide lines and the Today marker through the filler area.
- Verification run: `npm run lint`, `npm run build`.

## Deployment Notes

- Deployed `iLearn.Admin.React` to QA via `tools/deploy-admin-react.ps1`.
- Deploy output: `CopySucceeded=True`, `RobocopyExitCode=3`.
- Smoke checks after deploy:
	- `https://ap-ntc2138-qawb.nikonoa.net/iLearn/admin-react/` = HTTP 200
	- `https://ap-ntc2138-qawb.nikonoa.net/iLearn/admin-react/assignments/gantt` = HTTP 200
	- QA HTML references `assets/index-BcQrGzLp.js` and `assets/index-_WMth2Ta.css`.
- Playwright measurement at `1429x768` after deploy:
	- Day: filler `60px`, `gapLastRowToFiller=0`, `fillerToScrollerBottom=10`, horizontal scroll yes, vertical scroll no, filler background matches row background.
	- Week: filler `60px`, `gapLastRowToFiller=0`, `fillerToScrollerBottom=10`, horizontal scroll yes, vertical scroll no, filler background matches row background.
	- Month: filler `88px`, `gapLastRowToFiller=0`, `fillerToScrollerBottom=10`, horizontal scroll yes, vertical scroll no, filler background matches row background.

## Reviewer Notes

**Claude Code, 2026-07-31 — VERIFIED (ปิดในฐานะ superseded).**

โค้ดที่แผนนี้แก้ (`GanttChart.tsx` / `GanttBar.tsx` / `ganttScale.ts`) ถูกลบทั้งหมดโดย [PLAN-178](./PLAN-178-assignment-gantt-svar-replacement.md) ที่ย้ายไปใช้ `@svar-ui/react-gantt` ⇒ **รีวิว diff ย้อนหลังไม่ได้แล้ว** — ยืนยันจาก `src/pages/assignments/gantt/` ที่เหลือเพียง `AssignmentSvarGanttChart.tsx`, `svarGanttMapping.ts`, `ganttStatus.ts`

หลักฐานเท่าที่มี: lint/build ผ่าน + deploy QA + smoke 200 ตามที่บันทึกไว้ใน Verification ของแผนนี้และใน `DOC/AGENT_LOG.md`; งานขึ้น QA ใช้จริงอยู่ประมาณ 1 วันก่อนถูกแทนที่

ปิดสถานะเพื่อล้างหนี้รีวิว **ไม่ใช่การรับรองรายบรรทัด** — บทเรียนของ track นี้ (172–177 ไล่แก้ alignment 6 รอบไม่จบเพราะ hand-build layout engine เอง) สรุปไว้ใน Context ของ PLAN-178 แล้ว
