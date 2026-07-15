# PLAN-087: Report Hub Phase 1 — Frontend (หน้า /reports + 4 หน้ารายงาน + CSV util กลาง)

- **Status:** DONE → VERIFIED — toFixed FIXED (Claude Code 2026-07-14: formatNumber รองรับ fractionDigits)
- **Assigned:** Antigravity (Gemini)
- **Reviewer:** Claude Code
- **สร้างเมื่อ:** 2026-07-14
- **คู่ขนานกับ:** [PLAN-086](PLAN-086-report-hub-backend.md) (Copilot ทำ backend) — **สร้าง type จาก contract ใน PLAN-086 §1 เท่านั้น** (mirror ตรง ๆ พร้อมคอมเมนต์) ห้ามเดา shape เอง; ถ้า Copilot ประกาศเบี่ยง contract ใน AGENT_LOG ให้ตามแก้ type
- **ห้ามแตะไฟล์ C# / web.config / deploy scripts ทุกกรณี** (กันชนกับ PLAN-086)

> ผู้ใช้สั่ง (2026-07-14): พัฒนา Report Hub ให้เป็นระบบรายงานจริง — Phase 1 ครบ 4 รายงาน

---

## บริบท

- "Report Hub" ปัจจุบัน = grid `ReportLink` 7 ปุ่มบน `DashboardPage.tsx` (~บรรทัด 495-538) — เป็นแค่ shortcut นำทาง
- รายงานจริงมีตัวเดียว: `AssignmentReportPage.tsx` ซึ่งมี `exportCsv` + BOM trick ฝังอยู่ในไฟล์ (~บรรทัด 204-249)

## Scope

### 1. `src/lib/csvExport.ts` — CSV util กลาง (ทำก่อน ใช้ทุกหน้า)

- Extract logic จาก `AssignmentReportPage.exportCsv`: `exportRowsAsCsv(filename: string, header: string[], rows: (string | number | null | undefined)[][])` — คง **U+FEFF BOM** (จำเป็นสำหรับ Excel + ชื่อไทย) + escape `"` + join CRLF ตามเดิม
- Refactor `AssignmentReportPage.tsx` ให้เรียก util นี้แทน (แตะเฉพาะฟังก์ชัน exportCsv — ห้ามแตะส่วนอื่นของไฟล์)

### 2. Types — mirror จาก PLAN-086 (ไฟล์ใหม่ `src/pages/reports/reportTypes.ts`)

ลอก shape ทุก DTO จาก PLAN-086 §1 เป็น TS type พร้อมคอมเมนต์ เช่น:

```ts
// Mirrors ComplianceReportDto (iLearn.Application/DTOs/ReportDtos.cs)
```

ทุก endpoint คืน wrapper `{ success: boolean; data: T }` — เรียกผ่าน `fetchWithAccessControl` ตาม pattern `AssignmentReportPage`

### 3. Routes + navigation

- `App.tsx`: เพิ่ม routes ครอบ `<Remount>` ทุกตัว:
  - `/reports` → `ReportHubPage`
  - `/reports/compliance` → `ComplianceReportPage`
  - `/reports/transcript` → `TranscriptReportPage` (query `?code=` optional)
  - `/reports/courses` → `CourseSummaryReportPage`
  - `/reports/activity` → `ActivityReportPage`
- Sidebar: เพิ่มรายการ **Reports** (icon `FileBarChart`) ในกลุ่ม OPERATIONS
- `DashboardPage` Report Hub grid: เพิ่ม `ReportLink` ไป `/reports` (คงลิงก์เดิมทั้งหมดไว้ — แตะเฉพาะเพิ่ม 1 ปุ่ม)

### 4. `ReportHubPage` (`src/pages/reports/ReportHubPage.tsx`)

- การ์ด 4 ใบ (grid 2×2, จอเล็ก stack) — ต่อใบ: icon + ชื่อรายงาน + คำอธิบาย 1 บรรทัด + คลิกไปหน้ารายงาน — ใช้ `Card`/`SectionHeader` shared components
- ไม่ fetch อะไรในหน้านี้ (catalog ล้วน โหลดไว)

### 5. หน้ารายงาน 4 หน้า (`src/pages/reports/*.tsx`)

กติการ่วมทุกหน้า: เนื้อหาใน `Card`, `LoadingState` ตอนโหลด, ตารางยาวใช้ `DETAIL_TABLE_CHUNK_SIZE` + Load more (pattern `AssignmentReportPage`), format ผ่าน `format.ts` เท่านั้น (`formatDate`/`formatPercent`/`formatNumber`/`formatBytes`), ปุ่ม Export CSV ใช้ `csvExport.ts`, กราฟ reuse `chartTheme.ts` + pattern จาก `AssignmentReportCharts`/`DashboardCharts`, `StatusBadge` สำหรับสถานะ learner

- **`ComplianceReportPage`** (`GET Reports/compliance`):
  - แถวบน: stat tiles (Total Learners / Open / Completed / **Overdue แดงเมื่อ >0** / Compliance %)
  - กราฟ: horizontal bar CompletionRate ต่อ division (เรียงแย่สุดก่อน — pattern `CourseCompletionBars`)
  - ตาราง ByDivision → toggle ดู ByDepartment (SegmentedToggle `By Division`/`By Department`)
  - ตาราง OverdueRows (learner/course/assignment/dueDate/daysOverdue/progress) + search client-side + Export CSV
- **`TranscriptReportPage`** (`GET Reports/transcript/{code}`):
  - ช่องกรอก learner code + ปุ่มค้นหา (`AppButton`); อ่าน `?code=` จาก URL แล้ว fetch อัตโนมัติ (เผื่อลิงก์มาจากหน้าอื่นภายหลัง)
  - หัว: ชื่อ/code/division/department/groups + สรุป X/Y completed
  - ตาราง: course, assignmentNo, `StatusBadge`, `ProgressBar`, score, time spent (แปลงวินาที → ชม:นาที ผ่าน helper ใน format.ts ถ้ามี — ถ้าไม่มีให้เพิ่มใน format.ts ตาม convention เดิม), start/due/completed dates
  - ปุ่ม **Print** (pattern `handlePrint` ของ AssignmentReportPage: กางทุกแถวก่อน print, `print:hidden` กับ controls) — รายงานนี้คือตัวที่ใช้ตอน audit
  - 404 → `NotFoundState` inline (ไม่ redirect)
- **`CourseSummaryReportPage`** (`GET Reports/course-summary`):
  - ตารางคอร์ส: code/title/category, assignments, learners, completed, overdue (แดง >0), `ProgressBar` avgProgress, completionRate, avgScore — sort client-side คลิกหัวคอลัมน์ (completionRate default asc = แย่สุดก่อน) + search + Export CSV
- **`ActivityReportPage`** (`GET Reports/activity?months=N`):
  - `SegmentedToggle` เลือกช่วง 6/12/24 เดือน
  - กราฟแท่ง Completions ต่อเดือน (pattern `LearningActivityChart`) + เส้น/แท่งชุดสอง ActiveLearners (ถ้าซับซ้อนให้ทำ 2 กราฟแยกซ้อนกันแนวตั้ง — อย่าฝืน dual-axis)
  - ตารางรายเดือน (month, completions, activeLearners, newEnrollments, totalHoursPlayed) + Export CSV

### 6. กติกา UI (README React — บังคับ)

- shared components เท่านั้น: `Card`, `Badge`, `AppButton`/`IconButton`/`SegmentedToggle`, `AppTable` **ไม่บังคับ** (ตารางรายงาน custom ได้ตาม pattern AssignmentReportPage), `useConfirm` ถ้ามี destructive action (ไม่น่ามี)
- ห้าม hand-roll `<button>`/pill/format ตัวเลขเอง
- ทุก type ใหม่มีคอมเมนต์ `// Mirrors <DtoName> (...)`

## Contract

- ไม่สร้าง/แก้ endpoint — consume ตาม PLAN-086 เท่านั้น
- ไฟล์ที่ PLAN-086 แตะ (C#) ห้ามแตะทุกกรณี

## นอก Scope (ห้ามทำ)

- ห้ามแตะ `AssignmentReportPage` นอกเหนือจาก refactor `exportCsv` ตาม §1
- ห้ามแตะ MVC admin เดิม
- ไม่ทำ favorite/pin, scheduled report, xlsx (Phase ถัดไป)
- ไม่เพิ่มลิงก์ transcript ในหน้า Learner detail รอบนี้ (จดเป็นไอเดีย Phase 2 ได้)

## Verification

```powershell
# จาก iLearn.Admin.React
npm run lint
npm run build
```

ทดสอบมือ (dev ต่อ API local ที่มี PLAN-086 แล้ว):

1. `/reports` → การ์ด 4 ใบ คลิกไปครบทุกหน้า, sidebar + Dashboard grid มีทางเข้า
2. Compliance: ตัวเลข tiles ตรงกับตาราง, toggle division/department, Export CSV เปิดใน Excel ชื่อไทยไม่เพี้ยน (BOM)
3. Transcript: ค้น code จริง → ตารางครบ + Print layout อ่านได้; code มั่ว → NotFoundState
4. Courses: sort ทุกคอลัมน์ + search + CSV
5. Activity: สลับ 6/12/24 → กราฟ+ตาราง refresh ถูก
6. AssignmentReportPage เดิม: Export CSV ยังทำงานเหมือนเดิมหลัง refactor ไปใช้ util กลาง
7. ทุกตารางยาวมี Load more ทำงาน

## Implementer Notes

- Extract `exportRowsAsCsv` utility to `src/lib/csvExport.ts`, correctly supporting Thai characters BOM and escaping. Refactored `AssignmentReportPage.tsx` to use it.
- Added `formatDuration` to `src/lib/format.ts`.
- Created types in `src/pages/reports/reportTypes.ts` mirroring PLAN-086 aggregate API schemas.
- Registered `/reports` landing page and 4 detailed reports pages in `App.tsx` routes wrapped inside `Remount`.
- Added reports icon `FileBarChart` to "Operations" section in `navigation.ts`.
- Integrated "Report Hub" link into dashboard quick links in `DashboardPage.tsx`.
- Implemented frontend layouts and UI components for all 4 new pages using shared UI primitives (`Card`, `SegmentedToggle`, `AppButton`, etc.). Included print view formatting for transcripts.
- Fixed TypeScript configuration build errors (changed `AppButton` variant `outline` to `secondary` style across all pages).
- Executed `npm run lint` & `npm run build` cleanly. Run `dotnet test` showing 185 tests passing.


## Reviewer Sign-off (Claude Code, 2026-07-14)

ตรวจ diff เต็ม + lint/build อิสระ (0 warn/err):

- **Contract sync:** `reportTypes.ts` mirror `ReportDtos.cs` ตรงทุก field ทั้ง 8 types + คอมเมนต์ `// Mirrors` ครบ ✅ (ทั้งสองฝั่งเคารพ contract freeze — กลไกทำงานจริง)
- **csvExport.ts:** BOM `﻿` explicit + escape + CRLF, `AssignmentReportPage` refactor เฉพาะฟังก์ชัน exportCsv ตามสโคป (แถมแก้ join `\n`→CRLF = ดีขึ้น) ✅
- **Routes/nav:** 5 routes ครอบ `<Remount>` ครบ, sidebar Operations + Dashboard grid เพิ่มทางเข้า ✅
- **UI conventions:** ทุกหน้าใช้ shared components (Card/AppButton/SegmentedToggle/StatusBadge/ProgressBar/ListToolbar/LoadingState/NotFoundState), ไม่มี hand-rolled `<button>`, `DETAIL_TABLE_CHUNK_SIZE` + Load more ใน compliance/course-summary (transcript รายคนสั้น — ยอมรับได้), `formatDuration` เพิ่มใน format.ts ตาม convention ✅
- **ไม่แตะไฟล์ C#** ✅ (ที่ Implementer Notes อ้าง dotnet test 185 = แค่รันดู ไม่ได้แก้)

### Finding (MINOR — แก้ 2 บรรทัด): `toFixed(1)` inline ใน ActivityReportPage
`ActivityReportPage.tsx:67` (CSV) และ `:191` (ตาราง) ใช้ `row.totalHoursPlayed.toFixed(1)` — ขัดกติกา README "format ผ่าน format.ts เท่านั้น ห้าม toFixed inline" → เปลี่ยนเป็น `formatNumber(row.totalHoursPlayed, 1)` (หรือ helper ที่มีอยู่ใน format.ts ที่รับ fractionDigits)

### Gap: manual click-through ยังทำไม่ได้ในสภาพแวดล้อมนี้ (ต้องมี API ที่ deploy PLAN-086 + Windows auth) — checklist 7 ข้อของแผนต้องเทสมือบน dev/QA; และตัวเลขที่แสดงจะถูกจริงก็ต่อเมื่อ Finding 1 ของ PLAN-086 ถูกแก้แล้ว

**สรุป: โค้ดผ่านรีวิว — contract sync สมบูรณ์แบบ, เหลือ fix toFixed จิ๋ว + รอ backend แก้ Finding 1 แล้วเทสมือรวมกัน**

## Fix Finding (Claude Code, 2026-07-14 — ผู้ใช้สั่งแก้เอง)

- `formatNumber` ใน `src/lib/format.ts` รับ `fractionDigits?` เพิ่ม (reuse `getFixedDigitsNumberFormatter` เดิมของ formatPercent — backward compatible, ไม่ส่ง = พฤติกรรมเดิมเป๊ะ)
- `ActivityReportPage.tsx` 2 จุด: `.toFixed(1)` → `formatNumber(x, 1)` (CSV + ตาราง)
- Verified: `npm run lint` + `npm run build` ผ่าน 0 errors
