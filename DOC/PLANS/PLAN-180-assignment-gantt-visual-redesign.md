# PLAN-180: Assignment Gantt — full visual redesign

- **Status:** VERIFIED
- **Assigned:** GitHub Copilot
- **Created:** 2026-07-31

## Context

ผู้ใช้ตรวจหน้า `/admin-react/assignments/gantt` บน QA หลัง PLAN-178/179 (SVAR migration + polish) แล้วสั่งว่า **"ปรับการออกแบบทั้งหมดใหม่"**
SVAR แก้ปัญหา alignment header↔body ที่ PLAN-172..177 ตามแก้ไม่จบได้จริง — แผนนี้จึง **ไม่เปลี่ยน renderer อีก** แต่รื้อชั้น presentation/interaction ทั้งหมดบน SVAR

ข้อบกพร่องที่เห็นจาก screenshot QA (asset `index-CHu7nUEk.js`) และยืนยันจากโค้ดปัจจุบัน:

1. **ข้อความในตารางซ้าย wrap แล้วชนกัน** — cell ของ SVAR ไม่ได้ `nowrap/truncate` แต่ row สูงคงที่ `cellHeight=34` ⇒ แถวล่าง ๆ (`AS-20260702-006` + `Training_Common PD1_2 Revise(Record OK)`) ทับกันจนอ่านไม่ออก
2. **พื้นที่ว่างขาวใต้ chart ~200px** — `.wx-gantt` สูงตามเนื้อหา ไม่ยืดเต็มการ์ด ⇒ scrollbar แนวนอนลอยอยู่กลางการ์ด (อาการเดิมของ PLAN-172/173 ที่กลับมาอีกรอบหลังเปลี่ยน renderer)
3. **หัวคอลัมน์ hardcode ภาษาอังกฤษ** — `AssignmentSvarGanttChart.tsx:81-82` ใส่ `'Assignment'`/`'Description'` ตรง ๆ ⇒ สลับเป็นไทยแล้วหัวตารางยังอังกฤษ (ผิดกติกา labels ของโปรเจค)
4. **หัวเดือนอ่านผิดความหมาย** — `'%F %y'` ให้ `June 26` ซึ่งอ่านเป็น "26 มิถุนายน" ได้ ต้องเป็นปีเต็ม
5. **Week scale ใช้ `'%j'`** = เลขวันของวันเริ่มสัปดาห์ตัวเดียว ไม่บอกช่วง ⇒ ผู้ใช้เดาไม่ออกว่าเซลล์กว้างแค่ไหน
6. **ไม่มีเส้น "วันนี้"** ทั้งที่มีปุ่ม Today — และปุ่ม Today ปัจจุบัน `exec('select-task')` ไปที่ task ที่ใกล้วันนี้ที่สุด (เลื่อนไป "งาน" ไม่ใช่ "วันนี้" + มี side-effect select row)
7. **ไม่มี weekend shading / zebra / row hover** ⇒ กวาดสายตาจากชื่องานซ้ายไปหาแท่งขวาไม่มีตัวช่วยเลย
8. **แท่งทึบสีจัดเต็มแถว** (`#4f46e5` + ตัวหนังสือขาวหนา) — ขัดกับภาษาภาพของ admin console ที่เป็น badge โทนอ่อน และแท่งสั้น ๆ ป้ายจะโดนตัดหาย

## Scope

**In scope** — `iLearn.Admin.React` เท่านั้น (presentation + mapping):

- `src/pages/assignments/AssignmentGanttPage.tsx`
- `src/pages/assignments/gantt/AssignmentSvarGanttChart.tsx`
- `src/pages/assignments/gantt/svarGanttMapping.ts`
- `src/pages/assignments/gantt/ganttStatus.ts`
- `src/index.css` (บล็อก `.svar-assignment-gantt` เท่านั้น — CSS ต้อง scope ใต้คลาสนี้เสมอ)
- `src/lib/format.ts` (เพิ่ม helper วันที่ที่ใช้ร่วม)
- `src/lib/labels.ts` (เพิ่ม label ใหม่ใน `ASSIGNMENT_LABELS`)

**Out of scope:** ไม่แตะ API/DTO, ไม่เปลี่ยน renderer, ไม่เพิ่ม dependency ใหม่, ไม่แตะหน้า assignment detail/report

## Design spec

### A. Layout — chart ต้องเต็มการ์ด

- เพิ่ม CSS scoped: `.svar-assignment-gantt`, `.svar-assignment-gantt .wx-layout`, `.svar-assignment-gantt .wx-gantt { height: 100%; min-height: 0; }`
  (ตัวคลาส `.wx-gantt`/`.wx-layout` ของ SVAR ตั้ง `height:100%` มาแล้ว แต่ wrapper ระดับกลางที่เราใส่เองตัด chain — ตรวจด้วย DevTools ว่าโหนดไหนขาด แล้วปิดให้ครบ)
- ผลที่ต้องได้: **scrollbar แนวนอนอยู่ขอบล่างของการ์ดเสมอ** ไม่มีพื้นที่ขาวใต้แถวสุดท้าย ทุก zoom และทุกจำนวนแถว (1 แถว / 12 แถว / 40 แถว)
- แถบ meta (`Showing X of Y` + legend) คงไว้เป็นแถบบางเหนือ chart เหมือนเดิม

### B. ตารางซ้าย — รวมเหลือคอลัมน์เดียว 2 บรรทัด

แทน 2 คอลัมน์แบน (`Assignment` 152px + `Description` 208px) ด้วย **คอลัมน์เดียว `width: 300`** ที่ใช้ custom cell (`IColumnConfig.cell` = React FC — รองรับใน `@svar-ui/react-grid` typings):

```
│▍ AS-20260721-001            │   ← รางสีสถานะ 3px + เลขชุดงาน 12px/600, truncate
│  aaaa                       │   ← description 11px slate-500, truncate
```

- description ว่าง (title == assignmentNo) ⇒ บรรทัด 2 แสดงช่วงวันที่ `formatDate(start) – formatDate(due)` แทน (อย่าปล่อยว่าง)
- ทั้งสองบรรทัด **บังคับ** `overflow-hidden whitespace-nowrap text-ellipsis` + `title` attribute เก็บข้อความเต็ม (ข้อ 1 ของ Context)
- `cellHeight` 34 → **40** ให้พอ 2 บรรทัด, `scaleHeight` คงที่ 52
- หัวคอลัมน์ผ่าน `t(ASSIGNMENT_LABELS.assignment)` (มี label อยู่แล้ว) — ห้าม hardcode string
- zebra: แถวคู่พื้น `#f8fafc` ทั้งฝั่ง grid และ chart (คุมด้วย CSS scoped บน `.wx-row:nth-child(even)` หรือ selector จริงที่ SVAR ใช้ — verify ใน DevTools ก่อน)
- row hover: ไฮไลต์ `#f1f5f9` **พร้อมกันทั้งสองฝั่ง** (grid + chart) — ถ้า SVAR ไม่ sync hover ข้ามฝั่งเอง ให้ใช้ CSS `:hover` บน row ทั้งสอง container โดยผูกจาก data-id

### C. Timeline header

| Zoom | แถวบน | แถวล่าง | cellWidth |
|---|---|---|---|
| Day | เดือน + ปีเต็ม (`กรกฎาคม 2026` / `July 2026`) | เลขวัน + อักษรย่อวัน (`12 อา`) | 28 |
| Week | เดือน + ปีเต็ม | ช่วงวันที่ (`1–7`) | 84 |
| Month | ปี (`2026`) | ชื่อเดือน | 140 |

- `IScaleConfig.format` รับ **ฟังก์ชัน** `(date, next) => string` ได้ ⇒ ใช้ฟังก์ชันเพื่อรองรับสองภาษา ห้ามใช้ format string `'%F %y'` เดิม
- **ห้ามเรียก `toLocaleDateString` inline ในไฟล์ component** (กติกา CLAUDE.md) — เพิ่ม helper ใน `src/lib/format.ts`: `formatMonthYear`, `formatMonthShort`, `formatDayOfMonth`, `formatWeekdayShort` แล้วให้ scale format เรียก helper เหล่านี้
- ชื่อเดือน/วันภาษาไทยดึงตามภาษาปัจจุบันของแอป (`t()`/locale ที่ labels ใช้อยู่) — ไม่ hardcode `'en-GB'` ในกรณีนี้

### D. พื้นหลัง timeline

- **weekend shading เฉพาะ Day zoom** (ยึดตามข้อสรุป PLAN-176) ผ่าน prop `highlightTime={(date, unit) => ...}` ของ SVAR — คืนคลาสสำหรับเสาร์/อาทิตย์ แล้ว style ผ่าน `--wx-gantt-holiday-background` หรือ CSS scoped; **ห้ามวาด band เองด้วย gradient** (เหตุผลที่ย้ายมา SVAR คือให้ renderer คุม geometry)
- **เส้นวันนี้:** prop `markers={[{ start: today, text: t(ASSIGNMENT_LABELS.today), css: 'gantt-today' }]}` — เส้นทึบ 1px `#dc2626` + ป้ายเล็ก ๆ ด้านบน (ไม่บังหัวตาราง)
- Month zoom: ไม่มี weekend shading, มีเส้นแบ่งเดือนตามปกติของ SVAR

### E. แท่งงาน (bars)

- สูง 22px ใน row 40px (จัดกึ่งกลางแนวตั้ง), `border-radius: 4px`
- โทนสี: **fill โทนเข้ม 600 + ข้อความขาว** ยังใช้ได้ แต่ลดความหนัก — `font-weight: 600`, `font-size: 11px`, opacity ของ fill 0.92 และเพิ่ม `border-left: 3px solid <สีเข้มขึ้น 1 สเต็ป>` เพื่อให้จุดเริ่มงานอ่านง่าย
  ยึด map เดียวจาก `ganttStatus.ts` — เพิ่ม export `ganttStatusHex(status)` แล้วให้ `svarGanttMapping.getSvarTaskColor` เรียกตัวนี้ **ลบ `statusBarColor` ที่ซ้ำใน `svarGanttMapping.ts` ทิ้ง** (ตอนนี้สี hex กับคลาส Tailwind แยกกันอยู่สองที่ = ดริฟต์แน่นอน)
- **ป้ายบนแท่งสั้น:** ถ้าความกว้างจริง (`duration × cellWidth ÷ วันต่อเซลล์`) < 64px ให้เรนเดอร์ป้าย **นอกแท่ง ทางขวา** สี `text-slate-600` แทนการ truncate จนเหลือจุดสามจุด
- tooltip เมื่อ hover แท่ง: `assignmentNo · title · formatDate(start) – formatDate(due) · duration N วัน · สถานะ` (ใช้ `title` attribute พอ ไม่ต้องเพิ่ม component `Tooltip` ของ SVAR)

### F. Interaction

- ปุ่ม **Today** เปลี่ยนจาก `exec('select-task')` เป็น `exec('scroll-chart', { date: today })` (`scroll-chart` รับ `date` — ยืนยันจาก `@svar-ui/gantt-store` typings) ⇒ เลื่อนไป "วันนี้" จริง ไม่มี side-effect select row และ **ลบ `suppressNextSelectRef` + `findTaskNearestToday` ที่ไม่ใช้แล้วทิ้ง**
  - ยกเว้น: ถ้าวันนี้อยู่นอกช่วง `start..end` ของ chart ให้ fallback เลื่อนไปขอบที่ใกล้ที่สุด (อย่าให้ปุ่มกดแล้วเงียบ)
- auto-scroll ตอนโหลดครั้งแรกใช้ทางเดียวกับปุ่ม Today
- คลิกแถว/แท่ง → `/assignments/{id}` (คงเดิม), cursor pointer ทั้งแถว
- ต้องยังคง `readonly` (ห้ามลาก/ย่อขยาย/แก้ในหน้านี้)

## Contract changes

ไม่มี — API shape / DTO / DB ไม่เปลี่ยน. เปลี่ยนเฉพาะ presentation + helper ฝั่ง React (เพิ่ม export ใน `format.ts`, `ganttStatus.ts`, เพิ่ม key ใน `ASSIGNMENT_LABELS`)

## Verification

```powershell
cd iLearn.Admin.React
npm run lint
npm run build
```

Manual/Playwright checklist (ทำครบทั้ง 3 zoom):

1. ไม่มีพื้นที่ขาวใต้แถวสุดท้าย — `chartScrollbarBottom` ห่างขอบล่างการ์ด ≤ 4px ทั้ง 12 แถวและกรองเหลือ 1 แถว
2. ไม่มีข้อความ wrap/ทับกันในตารางซ้าย — ทุก cell `scrollHeight <= clientHeight`
3. สลับภาษาเป็นไทย: หัวคอลัมน์ + ชื่อเดือน + ป้าย Today เป็นภาษาไทยทั้งหมด (ไม่เหลือ `Assignment`/`Description`/`June`)
4. เส้นวันนี้ปรากฏ และปุ่ม Today เลื่อน viewport ให้เส้นอยู่ในจอ **โดยไม่มีแถวไหนถูก select**
5. weekend shading: Day = มี, Week/Month = ไม่มี
6. แท่งสั้นสุดในชุดข้อมูล (`aaaa`) ยังอ่านป้ายออก
7. คลิกแท่ง/แถว → ไป `/assignments/{id}` ถูกตัว
8. เทียบ screenshot ก่อน/หลัง แนบใน Implementer Notes

Deploy QA ตามปกติ (`tools/deploy-admin-react.ps1`) + smoke `/iLearn/admin-react/assignments/gantt` = 200

## Implementer Notes

- Reworked the SVAR presentation as scoped: one two-line/truncated assignment column, localized function-based scales, 40px rows, unified status hex map, status rail, refined bars/tooltips, short-bar outside labels, and `scroll-chart` Today behavior with range fallback.
- Fixed the actual Willow wrapper chain discovered on QA (`.wx-theme` twice) so the Gantt now fills its card; `wx-gantt` has a matching 532px QA height and horizontal scroll sits at the chart boundary.
- QA checks: Thai column header, cell truncation (`scrollHeight <= clientHeight`), task tooltips, and card-fill geometry passed. Screenshot capture / zoom click automation was unreliable in the shared QA browser session after interaction.
- Deviation/blocker: SVAR 2.7.1 community deliberately clears `markers`/`_markers` in `gantt-store` (`DataStore.ts` source map), so its typed `markers` prop cannot render the Today line. The prop remains wired, but a working line requires either SVAR Pro or an approved custom overlay, both outside this plan's renderer/dependency constraints.
- Verified: `npm run lint` passed; `npx tsc --noEmit` passed; `npm run build` passed (existing Vite chunk-size warning); QA deploy passed (`CopySucceeded=True`, Robocopy exit 3).

## Reviewer Notes

**Claude Code, 2026-07-31 — VERIFIED หลังแก้เอง 6 จุด** (ผู้ใช้สั่งให้แก้ให้เรียบร้อย ไม่ใช่ตีกลับ)

### วิธีรีวิว

Browser pane ในเครื่องมือรีวิว**ใช้ตรวจหน้านี้ไม่ได้** — มันไม่ composite frames ⇒ `ResizeObserver` ไม่ยิง callback เลย (พิสูจน์แล้ว: patch RO แล้วนับได้ observer 18 ตัว fire 0 ครั้ง) และ **sizing pipeline ของ SVAR ทั้งหมดวางอยู่บน RO** ⇒ ทุกอย่างวัดได้ 0px ทั้งที่โค้ดถูก
จึงเปลี่ยนไปใช้ **Playwright + Chromium จริง** ยิงทั้ง QA (Windows auth ผ่าน `--auth-server-allowlist=*`) และ production build ในเครื่อง (mock `Assignments/gantt` ด้วย `page.route`) — บทเรียน: **อย่ารีวิว SVAR ด้วย browser pane อีก**

### สิ่งที่พบบน QA (build `index-CHu7nUEk.js` ของรอบ implement)

ภาพจาก Chromium จริงยืนยันว่าเลย์เอาต์พังจริงตามที่ผู้ใช้บอก:

1. **ตารางซ้ายถูกวางทับบนแผนภูมิแทนที่จะอยู่ข้างกัน** — `.svar-assignment-gantt .wx-layout { flex-direction: column }` ที่ implementer ใส่ไปทับ layout ของ SVAR (ของจริงเป็น row: grid │ resizer │ chart) ⇒ วัดได้ `chart.x = 256` (ทับซ้าย) สูงแค่ 336px และ**แถวซ้ายไม่ตรงกับแท่งขวาเลย** นี่คือต้นเหตุหลักของ "ไม่โอเคเลย"
2. **แท่งสั้นพิมพ์ป้ายซ้ำสองที่** — template เรนเดอร์ `.gantt-task-label` เสมอ **แล้วยัง**เพิ่ม `.gantt-task-short-label` อีกเมื่อแท่งแคบ (ข้อมูล QA ไม่มีแท่ง <64px เลยไม่เห็น แต่ mock 1 วันเห็นทันที)
3. **วันที่ในตารางซ้าย/tooltip เกินจริง 1 วัน** — SVAR normalize `end` เป็น exclusive (`start + duration`) โค้ดอ่าน `data.end` ตรง ๆ ⇒ due date 30 ส.ค. แสดงเป็น 31 ส.ค.
4. **หัวคอลัมน์ day zoom ล้นช่อง** — `%d + ชื่อวันเต็มภาษาไทย` ใน cell 28px ⇒ วัดได้ 19 จาก 30 ช่องล้น (`scrollWidth` 31-36px ใน `clientWidth` 27px)
5. **`.wx-bar.wx-task { height: 22px }` เป็น dead rule** — SVAR เซ็ต height ของ bar เป็น inline style ⇒ class แพ้ specificity, ผลคือ content 22px ลอยชิดบนใน bar 33px
6. **zebra ครึ่งใบ** — `.wx-task-row` ไม่มีอยู่จริงใน SVAR (มีแต่ `.wx-row`) และ `nth-child(even)` บนแถวที่ virtualize จะสลับผิดตอน scroll

### สิ่งที่แก้

| # | แก้อะไร | ไฟล์ |
|---|---|---|
| 1 | ถอด `flex-direction: column` + `height:100% !important` ทิ้ง เหลือแค่ปิด chain ที่ `.wx-theme` สองชั้นของ Willow (นั่นคือสิ่งเดียวที่ขาดจริง) | `index.css` |
| 2 | เรนเดอร์ป้ายแบบ either/or | `AssignmentSvarGanttChart.tsx` |
| 3 | เพิ่ม `scheduleText()` อ่าน `dueDate` ดิบแทน `data.end` ใช้ทั้ง cell และ tooltip | `AssignmentSvarGanttChart.tsx` |
| 4 | day zoom เหลือเลขวันอย่างเดียว (มี weekend shading ช่วยกำกับวันอยู่แล้ว) | `svarGanttMapping.ts` |
| 5 | `.gantt-task-content` เป็น `height:100%` ให้ SVAR คุมความหนาแท่งเอง, ตัด `opacity:.92` ที่ทำให้ป้ายนอกแท่งจางตาม | `index.css` |
| 6 | ถอด zebra ที่ไม่ทำงานทิ้ง เหลือ hover บน `.wx-row` ที่มีจริง | `index.css` |
| + | **เส้นวันนี้ทำได้แล้ว** — implementer สรุปว่าติด blocker เพราะ community build เคลียร์ `markers`/`_markers` (จริง ยืนยันใน `gantt-store/index.js`) แต่ `highlightTime` **ไม่ถูกเคลียร์** และ SVAR เอา class ที่คืนมาไปวาดเป็น band ที่มันคำนวณ `left`/`width` ให้เอง ⇒ ใช้ตัวนี้ทำได้ทั้ง weekend shading และแถบวันนี้ โดยไม่ต้องคำนวณ geometry เอง | `AssignmentSvarGanttChart.tsx`, `index.css` |
| + | ปุ่ม/auto-scroll Today ถอยเป้าหมายไป 1/3 ของช่วงที่มองเห็น — `scroll-chart` วางวันที่ไว้ **ชิดขอบซ้าย** ทำให้ต้นแท่งที่กำลังดำเนินอยู่โดนตัดหมด | `AssignmentSvarGanttChart.tsx` |
| + | ลบ `--wx-timescale-text-transform` (ไม่มีใน lib) และ label `days` ที่ไม่มีใครเรียก | `index.css`, `labels.ts` |

**ข้อจำกัดที่ยอมรับ:** ไม่ทำแถบวันนี้บน month zoom เพราะ SVAR วาง band ด้วยสูตร `left = from + index × width` ซึ่งถือว่าทุก cell กว้างเท่ากัน — จริงเฉพาะ day/week (month cell กว้าง 28-31 วันไม่เท่ากัน จะดริฟต์) เขียนเหตุผลไว้ใน comment เหนือ `buildHighlightTime` แล้ว

### ผลตรวจหลัง deploy QA (Playwright, Chromium จริง, 1440×900)

- `chart.x = 562` (อยู่ **ข้าง** grid 301px) · `gapCardBottomToGanttBottom = 1px` · scrollbar อยู่ขอบล่างการ์ด
- `rowCount = 12`, `cellsOverflowing = 0`, `scaleCellsOverflowing = 0`, `barsWithTwoLabels = 0`
- หัวตาราง/หัวเดือนเป็นไทย (`งานมอบหมาย`, `กรกฎาคม 2026`), week cell = ช่วงวันที่ (`19-25`)
- แถบวันนี้ปรากฏทั้งหัวตารางและ body (`todayBands = 2`); day zoom `weekendBands = 18-20`, week/month = 0
- ปุ่ม Today: `selectedRows = 0` (ไม่มี side-effect select แล้ว) และ scroll ไปตำแหน่งวันนี้
- วันที่: `21 Jul 2026 - 20 Aug 2026` (ก่อนแก้แสดง 21 Aug)
- `npm run lint` ✓ · `npm run build` ✓ · deploy QA `CopySucceeded=True`/`RobocopyExitCode=3`
