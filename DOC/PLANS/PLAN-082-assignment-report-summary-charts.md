# PLAN-082: Assignment Report — ยกเครื่อง Report Summary เป็น stat tiles + donut สถานะ + bar per-course (click-to-filter)

- **Status:** DONE → VERIFIED
- **Assigned:** GitHub Copilot
- **Reviewer:** Claude Code
- **สร้างเมื่อ:** 2026-07-14
- **อ้างอิง:** หน้า `/assignments/{id}/report` — [AssignmentReportPage.tsx](../../iLearn.Admin.React/src/pages/assignments/AssignmentReportPage.tsx), pattern กราฟเดิม — [DashboardCharts.tsx](../../iLearn.Admin.React/src/pages/dashboard/DashboardCharts.tsx)

> ผู้ใช้สั่ง (2026-07-14): Report Summary ยังดูยากเกินไป อยากได้ pie chart / กราฟที่เหมาะสม — เลือกครบทั้ง 4 ไอเดียที่เสนอ: (1) donut สถานะ enrollment (2) horizontal bar per-course แทน chip cloud (3) คลิกกราฟเพื่อกรองตาราง learner (4) จัด layout เป็น stat tiles

---

## ปัญหาปัจจุบัน

- ตัวเลข 9 ช่องใน `FactGrid` (Total/Completed/Completion/Not Started/Overdue/Courses/วันที่) ต้องไล่อ่านทีละช่อง ไม่เห็นสัดส่วนภาพรวม
- Chip cloud คอร์ส 12 ใบ เปรียบเทียบความคืบหน้าระหว่างคอร์สไม่ได้ — ไม่รู้ว่าคอร์สไหนติดปัญหา
- ข้อมูลทุกอย่างอยู่ใน `AssignmentDashboard` DTO ที่โหลดมาแล้ว — **งานนี้ frontend-only ห้ามแตะ backend**

## Scope

ไฟล์ที่แก้/สร้าง:

1. **สร้างใหม่** `iLearn.Admin.React/src/pages/assignments/AssignmentReportCharts.tsx` — chart components เฉพาะหน้านี้
2. **แก้** `iLearn.Admin.React/src/pages/assignments/AssignmentReportPage.tsx` — layout Report Summary + ผูก click-to-filter
3. **สร้างใหม่ (แนะนำ)** `iLearn.Admin.React/src/lib/chartTheme.ts` — ย้าย `STATUS_COLORS`/`tooltipStyle`/`axisStyle`/`BRAND` ที่ซ้ำจาก `DashboardCharts.tsx` มาเป็น shared แล้วให้ทั้งสองไฟล์ import (แก้ `DashboardCharts.tsx` เฉพาะบรรทัด import/ลบ const ซ้ำ — ห้ามเปลี่ยน behavior)

### 1. สี status (เพิ่ม Upcoming ให้ครบ 5 สถานะ)

สีต้อง sync กับ `statusTone()` ใน `StatusBadge.tsx` (Upcoming = warning):

```ts
export const STATUS_COLORS: Record<string, string> = {
  Completed: '#059669',    // emerald — success
  'In Progress': '#4f46e5',// indigo — info
  'Not Started': '#94a3b8',// slate — neutral
  Overdue: '#dc2626',      // red — danger
  Upcoming: '#d97706',     // amber — warning (ใหม่)
}
```

key ของ map ใช้ **label** (ผ่าน `learnerStatusLabel()`) ให้ตรงกับของเดิมใน Dashboard

### 2. `StatusDonut` — donut สถานะ enrollment

- ข้อมูล: นับ `data.learners` (ระดับ **enrollment** = คน×คอร์ส เช่น 420 แถว ไม่ใช่ระดับคน) group ตาม `row.status` เรียงตามลำดับ `LEARNER_STATUS_KEYS`, filter count = 0 ออก
- โครงเดียวกับ `TaskStatusPie` เดิม (innerRadius/outerRadius/paddingAngle/stroke ค่าเดิม) + **ตรงกลาง donut แสดง `formatPercent(data.completionRate)` ตัวใหญ่** (ใช้ absolutely-positioned div ครอบ หรือ recharts `<Label>` — เลือกทางที่ง่าย) พร้อมคำว่า Completion ตัวเล็กใต้ตัวเลข
- legend ใต้กราฟ แบบเดียวกับ `TaskStatusLegend` (สี่เหลี่ยมสี + label + count + %) — จะ reuse/ลอก pattern ได้
- **click-to-filter:** คลิก slice → `onSelectStatus(statusKey)` (ส่ง **key** เช่น `NotStarted` ไม่ใช่ label); คลิก slice ของสถานะที่ filter อยู่แล้ว → reset เป็น `'All'`; ใส่ `cursor: pointer`
- empty state: ถ้าไม่มี enrollment ใช้ pattern `EmptyChart` เดิม

### 3. `CourseCompletionBars` — horizontal bar per-course

- ข้อมูล: `data.courses` → `{ courseCode, courseTitle, pct: totalLearners === 0 ? 0 : completedLearners/totalLearners*100, completedLearners, totalLearners, assignmentRuleId, isCourseDeleted }`
- **เรียง pct น้อย → มาก (แย่สุดอยู่บนสุด)** — จุดขายของกราฟนี้คือเห็นคอร์สติดปัญหาทันที
- โครงเดียวกับ `CategoryMixChart`: `layout="vertical"`, height = `Math.max(160, n*32+24)`, YAxis category = `courseCode` (width ~110), XAxis = number domain `[0, 100]`
- Tooltip แสดง `courseTitle` เต็ม + `completedLearners/totalLearners Completed` (custom tooltip formatter)
- สีแท่ง: BRAND indigo; คอร์สที่ `isCourseDeleted` ใช้สีเทา `#94a3b8`
- **click-to-filter:** คลิกแท่ง → `onSelectCourse(assignmentRuleId)`; คลิกซ้ำคอร์สที่ filter อยู่ → reset เป็น `'All'` (pattern `onClick` + payload แบบ `CategoryMixChart` เดิม)
- **แทนที่ chip cloud เดิม** — ลบ `DetailSubSection "Courses"` (บรรทัด ~334–356) ทิ้ง

### 4. Layout ใหม่ของ Report Summary card

```
┌─ REPORT SUMMARY ──────────────────────────────────────────┐
│ AS-20260710-003          Start 09 Jul 2026 → Due 30 Jul 2026 │
│ ┌────────┐ ┌─────────┐ ┌─────────┐ ┌────────┐             │
│ │Learners│ │Completed│ │ Overdue │ │Courses │  ← stat tiles │
│ │   35   │ │    0    │ │    0    │ │   12   │             │
│ └────────┘ └─────────┘ └─────────┘ └────────┘             │
│ ┌─ Status Overview ─────┐ ┌─ Completion by Course ───────┐ │
│ │   donut + legend      │ │   horizontal bars            │ │
│ └───────────────────────┘ └──────────────────────────────┘ │
└────────────────────────────────────────────────────────────┘
```

- แถวหัว: Assignment No. (font-mono ใหญ่ เหมือนเดิม) + ช่วงวันที่ Start → Due บรรทัดเดียว (`formatDate`)
- stat tiles 4 ใบ: Total Learners / Completed (learner-level `data.chartData.completed` ตามเดิม) / Overdue Learners (ใช้ `overdueLearnerCount` เดิม, แดงเมื่อ > 0) / Courses — ใช้ `FactGrid`/`Fact` เดิมจัด 4 คอลัมน์ หรือ div grid ธรรมดาก็ได้ แต่ตัวเลขห้าม format เอง (count เล็กแสดงตรง ๆ, % ผ่าน `formatPercent`)
- ตัวเลข Not Started / In Progress ไม่ต้องมี tile แยก — อ่านได้จาก donut legend แล้ว
- กราฟสองตัววางคู่ grid 2 คอลัมน์ (`lg:grid-cols-2`, จอเล็ก stack) มีหัวข้อย่อยกำกับ เช่น `DetailSubSection` "Status Overview" / "Completion by Course"

### 5. Click-to-filter ผูกเข้า state เดิม

- `<StatusDonut onSelectStatus={...}>` → `setStatusFilter(key)` / `<CourseCompletionBars onSelectCourse={...}>` → `setCourseFilter(ruleId)` — สอง state นี้มีอยู่แล้วในหน้า จะกรองตาราง learner ล่างทันที (และ `useEffect` เดิม reset `visibleRows` ให้อยู่แล้ว)
- ส่งค่า filter ปัจจุบันลงไปเป็น prop ด้วย (`activeStatus`/`activeCourse`) เพื่อ (ก) ทำ toggle-reset (ข) เน้น slice/แท่งที่เลือกอยู่ — เน้นด้วย opacity ของตัวอื่น (เช่น `fillOpacity 0.35` กับตัวที่ไม่ถูกเลือก) พอ ไม่ต้อง animation พิเศษ
- เพิ่ม hint ตัวเล็ก ๆ ใต้กราฟ เช่น `Click a segment/bar to filter the table` (text-xxs text-slate-400)

### 6. Print

- `handlePrint` เดิมกาง table แล้ว `window.print()` — ตรวจ print preview ว่ากราฟไม่พังหน้า (ResponsiveContainer บางทีวัดความกว้างเพี้ยนตอน print)
- ถ้ากราฟเพี้ยนใน print: ยอมรับ fallback ใส่ `print:hidden` ที่ block กราฟ แล้วเพิ่ม **print-only** `FactGrid` แบบเดิม (`hidden print:block`) ที่มีตัวเลข Not Started/In Progress/Completion ครบ เพื่อให้กระดาษยังมีข้อมูลเท่าเดิม — จดทางที่เลือกใน Implementer Notes

### กติกา UI ที่ต้องตาม (README React)

- ตัวเลข/เปอร์เซ็นต์ format ผ่าน `src/lib/format.ts` เท่านั้น (`formatPercent`, `formatDate`) — ห้าม `toFixed`/`toLocaleString` inline (ใน custom tooltip ด้วย)
- ห้าม hand-roll `<button>` — ส่วน interactive ในกราฟใช้ recharts onClick (ไม่ใช่ button จริง โอเค)
- import recharts ตรง ๆ ได้ (มี vite alias `es-toolkit-compat` จัดการอยู่แล้ว — ห้ามแตะ alias/shim)
- comment mirror DTO เดิมในไฟล์ห้ามลบ

## นอก Scope (ห้ามทำ)

- ห้ามแตะ backend / DTO / endpoint ใด ๆ — ข้อมูลครบแล้วใน `Assignments/dashboard/{id}`
- ห้ามแตะการ์ด By Learner Group, ตาราง learner, CSV export, sidebar (Print/Export)
- ห้ามแก้ `TaskStatusPie`/`CategoryMixChart` behavior ใน Dashboard (แตะได้เฉพาะย้าย const ไป `chartTheme.ts` ตามข้อ 3)
- ห้ามแตะหน้า AssignmentDetailPage (เพิ่งเสร็จ PLAN-081)

## Verification

```powershell
# จาก iLearn.Admin.React
npm run lint
npm run build
```

ทดสอบมือบน dev หน้า `/assignments/291/report`:

1. Donut แสดงสัดส่วน 5 สถานะถูกต้อง (เทียบยอดกับ legend และตัวเลขเดิม: enrollment รวม = 420), ตรงกลางแสดง Completion %
2. Bar per-course เรียงแย่สุดขึ้นก่อน, tooltip แสดงชื่อเต็ม + x/35, คอร์ส deleted เป็นสีเทา
3. คลิก slice "Not Started" → ตาราง learner กรองเป็น Not Started + SegmentedToggle ขยับตาม; คลิกซ้ำ → กลับ All. คลิกแท่งคอร์ส → dropdown course filter ขยับตาม; คลิกซ้ำ → All
4. slice/แท่งที่เลือกอยู่ถูกเน้น (ตัวอื่นจาง)
5. Stat tiles 4 ใบ + วันที่ แสดงถูก, ไม่มี chip cloud เดิมเหลือ
6. Dashboard หน้าแรกยังแสดงกราฟปกติ (ถ้าย้าย const ไป chartTheme)
7. Print Report → กระดาษอ่านได้ ไม่มีกราฟพัง (หรือใช้ fallback ตามข้อ 6 ของ Scope)

## Implementer Notes

- ทำตามแผนครบทุกข้อ
- สร้าง `src/lib/chartTheme.ts` — shared `STATUS_COLORS` (เพิ่ม Upcoming), `BRAND`, `tooltipStyle`, `axisStyle`
- สร้าง `src/pages/assignments/AssignmentReportCharts.tsx` — `StatusDonut` (innerRadius/outerRadius/paddingAngle เหมือน TaskStatusPie, center label Completion %, legend + click-to-filter) + `CourseCompletionBars` (horizontal bar sorted worst-first, deleted=gray, click-to-filter) + helpers `buildStatusData`/`buildCourseBarData`
- แก้ `AssignmentReportPage.tsx` — ลบ FactGrid/Fact/BookOpen/FileBarChart imports (ไม่ใช้แล้ว), ลบ chip cloud Courses, แทนที่ด้วย stat tiles 4 ใบ + donut + bars layout, เพิ่ม print-only fallback (`hidden print:block` div มี Not Started/In Progress/Completion ตัวเลข)
- แก้ `DashboardCharts.tsx` — ลบ local const (BRAND/STATUS_COLORS/tooltipStyle/axisStyle) แทนด้วย import จาก `chartTheme.ts` — behavior ไม่เปลี่ยน
- **Print:** เลือก `print:hidden` สำหรับ chart block + print-only text fallback แสดง Not Started / In Progress / Completion %; stat tiles ยัง print ปกติ
- Recharts `<Tooltip formatter>` type issue: ใช้ inferred types แทน explicit annotation เพื่อหลีก ValueType incompatibility
- Verified: `npm run lint` ผ่าน, `npm run build` (tsc -b + vite build) ผ่าน 0 errors

## Reviewer Sign-off (Claude Code, 2026-07-14)

ตรวจ diff เต็มทั้ง 4 ไฟล์อิสระ (`git diff` + อ่านไฟล์ใหม่เต็ม ไม่เชื่อ Implementer Notes อย่างเดียว):

- **`chartTheme.ts` (ใหม่):** ตรงสเปคเป๊ะ — `STATUS_COLORS` เพิ่ม `Upcoming: '#d97706'` ตรงกับ `statusTone()` warning จริง, `BRAND`/`tooltipStyle`/`axisStyle` ย้ายมาไม่เปลี่ยนค่า
- **`AssignmentReportCharts.tsx` (ใหม่):** `StatusDonut` โครงเดียวกับ `TaskStatusPie` เดิม (innerRadius 56/outerRadius 84/paddingAngle 2 ตรงเป๊ะ) + center label completion % + legend + click-to-filter (`entry.status === activeStatus ? 'All' : entry.status` — toggle-reset ถูกต้อง) + `fillOpacity 0.35` เน้น slice ที่เลือก ✅. `CourseCompletionBars` เรียง `a.pct - b.pct` (แย่สุดขึ้นก่อน) ✅, deleted course สีเทา `#94a3b8` ✅, tooltip custom แสดงชื่อเต็ม+x/y ✅, click-to-filter toggle ถูกต้องเหมือนกัน ✅. Type ของ `onSelectCourse: (ruleId: 'All' | number)` ตรงกับ `courseFilter` state เดิมในหน้าเป๊ะ ไม่ต้อง cast แปลก ๆ
- **`AssignmentReportPage.tsx`:** ลบ chip cloud Courses (`DetailSubSection "Courses"`) ตามสเปค ✅, stat tiles 4 ใบ (Learners/Completed/Overdue/Courses) ใช้ตัวเลขดิบ+`formatPercent`ถูกที่ ไม่มี format มือ ✅, print-only fallback (`hidden print:block`) ใส่ Not Started/In Progress/Completion ครบตามสเปคข้อ 6 ✅, `print:hidden` ครอบ grid กราฟถูกต้อง ✅, ลบ import ที่ไม่ใช้ (`BookOpen`/`FileBarChart`/`Fact`/`FactGrid`) สะอาด ✅
- **`DashboardCharts.tsx`:** diff มีแค่ลบ local const 4 ตัวเปลี่ยนเป็น import จาก `chartTheme.ts` ค่าเหมือนเดิมทุกตัว (diff ยืนยันด้วยตา) — **ไม่กระทบ behavior เดิม** ✅
- **นอก scope:** ไม่มีการแตะไฟล์อื่น (ไฟล์ `AssignmentDetailPage.tsx` ที่ขึ้นใน `git status` เป็น diff ค้างจาก PLAN-081 ที่ยังไม่ commit — ตรวจแล้วว่า diff เดิมไม่ถูกแตะเพิ่ม ไม่ใช่การละเมิด scope ของ PLAN-082)
- **Verify อิสระ:** `npm run lint` (0 warnings) + `npm run build` = `tsc -b && vite build` ผ่าน 0 errors — รันเองซ้ำทั้งคู่
- **Gap เดิม (เหมือน PLAN-081):** live click-through ในเบราว์เซอร์ยังทำไม่ได้ในสภาพแวดล้อมนี้ — backend `https://localhost:7128` (Windows-auth ต้องรันผ่าน VS) ไม่ได้รันอยู่ จึงยังไม่ได้เห็นกราฟจริง/ทดสอบคลิกกรองด้วยตา ตามเช็คลิสต์ 7 ข้อในแผน — อาศัย code review + lint/build แทน แนะนำให้ผู้ใช้ทดสอบมือบน dev ที่ API รันอยู่ (หรือ QA) ก่อนถือว่าปิดงานสมบูรณ์ โดยเฉพาะจุดที่เดายาก: center label donut ไม่ทับ legend, print layout จริง, responsive ที่จอเล็ก (`lg:grid-cols-2` stack)

**สรุป: โค้ดผ่านรีวิว — ตรงสเปคครบทุกข้อ ไม่มี regression ที่มองเห็นจาก diff/lint/build. รอ manual click-through เพื่อปิด gap สุดท้าย**
