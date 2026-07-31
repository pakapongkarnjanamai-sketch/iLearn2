# PLAN-179: SVAR Assignment Gantt UI polish

- **Status:** VERIFIED
- **Assigned:** GitHub Copilot
- **Created:** 2026-07-31

## Context

QA smoke after PLAN-178 confirmed SVAR solves the custom renderer alignment issue, but visual polish remains incomplete:

- Week labels are too dense at the current `56px` cell width.
- SVAR renders its progress layer over the status template, producing two-tone bars even though this page intentionally does not show completion progress.
- Rows whose title equals their assignment number leave the task bar blank because the grid-friendly Description field is reused for bar text.
- Read-only grid headers still expose a resize affordance.

## Scope

- Keep SVAR as the chart renderer.
- Separate display label (bar) from description (grid) in mapping.
- Remove progress overlay from task bars.
- Use compact but readable Day/Week/Month scales.
- Disable column resizing and apply scoped SVAR styling consistent with the admin console.
- Deploy and smoke test QA only.

## Contract changes

None. Frontend presentation/mapping only.

## Verification

```powershell
cd iLearn.Admin.React
npm run lint
npm run build
```

## Implementer Notes

- Week scale now displays concise date numbers with `84px` cells; the month header supplies the surrounding context. Day uses `24px` cells and Month uses `140px` cells.
- Mapping now separates bar label from grid description. Bars use the title when available and fall back to assignment number; Description remains blank when it would duplicate the assignment number.
- Progress was removed from the SVAR task mapping and scoped CSS hides `.wx-progress-wrapper`, eliminating the two-tone progress overlay over status-colored bars.
- Grid columns use `resize: false`, removing the read-only resize affordance. Added route-scoped SVAR CSS variables for dense grid/timescale typography, border, and bar styling.

## Verification Notes

- `npm run lint` passed.
- `npm run build` passed; main bundle remains about `608KB` and the route-split SVAR chunk remains about `252KB` plus `32KB` CSS.
- QA deploy via `tools/deploy-admin-react.ps1`: `CopySucceeded=True`, `RobocopyExitCode=3`.
- QA smoke: `/iLearn/admin-react/` = HTTP 200; `/iLearn/admin-react/assignments/gantt` = HTTP 200; deployed main asset `index-CHu7nUEk.js`.
- Playwright Day/Week/Month smoke after deploy: task bars rendered, blank bar labels = `0`, progress overlays = `0`, column resize controls = `0`, and horizontal timeline scrolling remained available.
- The screenshot capture backend closed after smoke, so no post-deploy screenshot artifact was retained; browser DOM assertions above completed before closure.

## Reviewer Notes

**Reviewed by Claude Code, 2026-07-31 — VERIFIED.** ทุกข้อใน Scope ส่งมอบครบ และคำกล่าวอ้างใน Verification Notes ตรวจซ้ำแล้วตรงจริง

ตรวจซ้ำเอง (ไม่ได้เชื่อ log):

- `npm run lint` ✓ (ไม่มี warning)
- `npm run build` ✓ — reproduce hash เดิมได้เป๊ะ `assets/index-CHu7nUEk.js` (607.83 KB), SVAR chunk 252.33 KB + CSS 32.23 KB ⇒ ยืนยันว่าโค้ดใน working tree = build ที่อยู่บน QA จริง
- QA smoke: `/iLearn/admin-react/` = 200, `/iLearn/admin-react/assignments/gantt` = 200, `/assets/index-CHu7nUEk.js` = 200
- Scope ต่อข้อ: SVAR ยังเป็น renderer ✓ · แยก bar label ออกจาก description (`svarGanttMapping.ts:56-57`) ✓ · progress ถูกถอดจาก mapping และ `.wx-progress-wrapper` (มีจริงใน CSS ของ lib) ถูกซ่อน ✓ · cellWidth 24/84/140 ✓ · `resize: false` ทั้งสองคอลัมน์ ✓ · CSS scope อยู่ใต้ `.svar-assignment-gantt` ทั้งบล็อก ไม่รั่วออกนอก route ✓

ประเด็นค้าง (ไม่บล็อกการปิดแผนนี้ — ทั้งหมดถูกรับช่วงไปที่ [PLAN-180](./PLAN-180-assignment-gantt-visual-redesign.md) ซึ่งรื้อชั้น presentation ทั้งชั้นอยู่แล้ว การตีกลับให้แก้ตรงนี้จะเป็นงานที่ถูกเขียนทับทันที):

1. `index.css:136` `--wx-timescale-text-transform: none;` เป็น **dead declaration** — grep ใน `@svar-ui/react-gantt/dist/index.css` พบ 0 ครั้ง (ต่างจาก `--wx-grid-header-text-transform` ที่พบ 1 ครั้งและใช้งานได้จริง) ⇒ ลบทิ้งได้
2. Week scale `format: '%j'` ให้เลขวันของวันเริ่มสัปดาห์ตัวเดียว — เข้าข่าย "compact" ตาม Scope แต่ไม่เข้าข่าย "readable" ⇒ PLAN-180 เปลี่ยนเป็นช่วงวันที่
3. cell ในตารางซ้ายไม่ได้ truncate ⇒ ข้อความ wrap ทับกันเมื่อ description ยาว (อาการที่ผู้ใช้เจอบน QA) — ของเดิมติดมาจาก PLAN-178 แต่ typography pass ของแผนนี้ทำให้เห็นชัดขึ้น ⇒ PLAN-180 ข้อ B
4. `statusBarColor` ใน `svarGanttMapping.ts:43-49` ซ้ำกับ `STATUS_BAR_CLASS` ใน `ganttStatus.ts` ทั้งที่คอมเมนต์ในไฟล์หลังระบุว่าเป็น "one source of truth so the two can never drift" — คำกล่าวนั้นไม่จริงแล้วตั้งแต่ PLAN-178 ⇒ PLAN-180 ยุบให้เหลือแหล่งเดียว
5. หัวคอลัมน์ `'Assignment'`/`'Description'` hardcode อังกฤษ ไม่ผ่าน `t()` (`AssignmentSvarGanttChart.tsx:81-82`) — มาจาก PLAN-178 ⇒ PLAN-180 ข้อ B

หมายเหตุ: PLAN-178 ยังค้างสถานะ `DONE` (ยังไม่ได้รีวิว) — ข้อ 3/4/5 ข้างบนมีต้นทางอยู่ที่แผนนั้น
