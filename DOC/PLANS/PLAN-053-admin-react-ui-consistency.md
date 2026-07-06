# PLAN-053: Admin React — UI consistency audit (status pills, hardcoded badges, loading, format)

- **Status:** DONE
- **Assigned:** Antigravity (Gemini) — React-only, ไม่แตะ backend
- **Reviewer:** Claude Code
- **สร้างเมื่อ:** 2026-07-06
- **อ้างอิง:** [PLAN-036](PLAN-036-standardize-loading-indicators.md), [PLAN-037](PLAN-037-standardize-badge-pills.md), [PLAN-039](PLAN-039-format-number-utilities.md), README UI Conventions

> คำขอผู้ใช้ (2026-07-06): ตรวจความสม่ำเสมอของ UI ใน admin-react — ตัวอย่างที่พบ: pill `In Progress` แบบ **outline ขาว/เทา** (`border-slate-200 bg-white text-slate-500`) ซึ่งหน้าอื่นแสดงเป็น **soft สีน้ำเงิน** — audit แล้วพบปัญหาเป็นระบบ 4 หมวด รายการด้านล่างคือ finding ที่ยืนยันจากโค้ดจริงทุกจุด

---

## Root cause ของตัวอย่างที่ผู้ใช้เจอ

pill ดังกล่าวคือ `StatusText` (= `Badge variant="outline"`) ที่ `AssignmentDetailPage.tsx:692` — Fact "Status" ใช้ ternary เขียนมือ (`Completed→success / Upcoming→warning / Overdue→danger / อื่น ๆ→neutral`) จึงไม่มี branch ของ `'In Progress'` → ตกไป **neutral (ขาว/เทา)** ในขณะที่ค่าเดียวกันบนหน้า list/report/tab Courses render ผ่าน `StatusBadge` → **soft น้ำเงิน** — ระบบมี "แผนที่สี status" ซ้ำกัน ≥5 ชุดที่ drift จากกัน

## นโยบายกลาง (ให้ยึดตามนี้ทุกข้อ)

1. **Workflow status** (Completed / InProgress / NotStarted / Overdue / Upcoming / Expired / Active / Due Soon) → ใช้ `StatusBadge` (soft + tone อัตโนมัติจาก `statusTone()`) + label ผ่าน `learnerStatusLabel()` เสมอ — ทุกหน้า ทุกบริบท
2. **Binary state** (Active/Inactive ของ entity) → ใช้ `StatusText` (outline) เหมือนที่หน้า detail ใช้อยู่ — **คงเดิม ไม่แตะ**
3. ห้ามมี map สี/label ของ status เฉพาะหน้า — single source: `statusTone()` ใน `StatusBadge.tsx` + `learnerStatusLabel()` ใน `lib/learnerStatus.ts`

---

## Scope — findings ที่ต้องแก้ (ระบุไฟล์:บรรทัดจากโค้ดจริง)

### A. Status pills ไม่สม่ำเสมอ

- [x] **A1 (จุดที่ผู้ใช้รายงาน)** `pages/assignments/AssignmentDetailPage.tsx:692-704` — แทน `StatusText` + ternary ด้วย `<StatusBadge>{assignmentStatus}</StatusBadge>` (`statusTone` รองรับทั้ง 4 ค่า รวม `'In Progress'`→info อยู่แล้ว)
- [x] **A2** `pages/DashboardPage.tsx:46-53 + 410` — ลบ `STATUS_TONE` map เฉพาะหน้า → ใช้ `StatusBadge` แทน `StatusText`; เพิ่ม case ที่ยังไม่มีใน `statusTone()` กลาง: `'Due Soon'`→`warning`, `'Unassigned'`→`neutral` (vocabulary จาก `DashboardController.BuildPriorityAssignments`: Active / Due Soon / Overdue / Upcoming / Completed / Unassigned)
  - หมายเหตุ: ปัจจุบัน `'Active'` บน dashboard = outline เทา แต่ `statusTone` กลาง = info น้ำเงิน → หลังแก้จะเป็นน้ำเงิน สม่ำเสมอกับที่อื่น
- [x] **A3** `pages/courses/CourseDetailPage.tsx:696, 765` — `<StatusBadge>{l.status}</StatusBadge>` ส่ง key ดิบ → label โชว์ "InProgress"/"NotStarted" (ไม่มีวรรค) — ครอบด้วย `learnerStatusLabel(...)`
- [x] **A4** `pages/assignments/AssignmentGanttPage.tsx:24-28` — ลบ `BATCH_STATUS_LABELS` map ซ้ำ → ใช้ `learnerStatusLabel` (ครอบทุก key รวม `Expired` แล้ว)
- [x] **A5** `pages/moduleConfigs.ts:162` — `calculateCellValue` คืน label literal `'In Progress'/'Not Started'` → เปลี่ยนให้คืน **key** (`'InProgress'/'NotStarted'`) เพื่อให้ค่าใน grid สอดคล้องกับ status column อื่น (cell แสดงผลผ่าน `EntityListPage.tsx:59` ซึ่ง `learnerStatusLabel` ครอบอยู่แล้ว) — **ตรวจ filter/sort ของ DevExtreme หลังเปลี่ยนด้วย**

### B. Hardcoded pill spans (ห้ามตาม README)

- [x] **B1** `pages/users/UserEditorPage.tsx:281` (indigo pill) และ `:300` (emerald pill) — แทนด้วย `Badge` (`variant="outline"` tone `info`/`success` หรือใกล้เคียงที่สุด อย่า hardcode สีเอง)
- [x] **B2** `pages/users/UserDetailPage.tsx:183` — pill เขียนมือพร้อม conditional class — แทนด้วย `StatusText`/`Badge` ตาม binary state
- ℹ️ `BulkAssignPage.tsx:406` เป็นวงกลม radio UI ไม่ใช่ pill — **ไม่ต้องแตะ**

### C. Loading indicators

- [x] **C1** `pages/assignments/BulkAssignPage.tsx:594` และ `pages/courses/CourseEditorPage.tsx:841` — `<Loader2 className="h-7 w-7 animate-spin text-indigo-500" />` เขียนมือ → ใช้ `<LoadingState size="page" />` (หรือ `section` ตามบริบท container)
- ℹ️ `DashboardPage.tsx:266` (Loader2 3.5px inline ตอน refresh) เป็น busy indicator ขนาดจิ๋วใน chip — **ยอมรับได้ ไม่ต้องแตะ**

### D. Number formatting

- [x] **D1** `pages/dashboard/DashboardCharts.tsx:131` — `Math.round((d.count / total) * 100)` inline → ใช้ `formatPercent` จาก `lib/format.ts`
- ℹ️ `DashboardCharts.tsx:17` `STATUS_COLORS` (hex ต่อ label) เป็นข้อจำกัดของ recharts (ต้องใช้ hex) — **คงไว้ได้** แต่เพิ่มคอมเมนต์ชี้ว่า tone ตรงกับ `statusTone()`

### E. เอกสาร

- [x] **E1** เพิ่ม bullet ใน `iLearn.Admin.React/README.md` (UI Conventions): "workflow status → `StatusBadge`+`learnerStatusLabel` เท่านั้น; binary Active/Inactive → `StatusText`; ห้ามสร้าง tone/label map เฉพาะหน้า"

---

## Constraints

- ❌ React เท่านั้น — ห้ามแตะ backend/DTO/endpoint
- ❌ ห้ามเปลี่ยน semantics ของ status (key, การคำนวณ `deriveAssignmentStatus`) — งานนี้เรื่องการแสดงผลเท่านั้น (ยกเว้น A5 ที่เปลี่ยนค่า intermediate ใน grid — ต้อง verify filter)
- ❌ ห้ามลบ/เปลี่ยน API ของ `Badge`/`StatusBadge`/`StatusText` ที่ consumer อื่นใช้อยู่ — เพิ่ม case ใน `statusTone()` ได้อย่างเดียว
- ✅ Acceptance หลัก: **status string เดียวกัน หน้าไหนก็ตาม ต้องได้ pill หน้าตาเดียวกัน** (ยกเว้น size)

## Verification

```powershell
# จาก iLearn.Admin.React
npm run lint ; npm run build
```

- [x] grep ยืนยันไม่เหลือ: `STATUS_TONE`/`BATCH_STATUS_LABELS` เฉพาะหน้า, pill span เขียนมือใน `users/`, `Loader2` เขียนมือ (นอก whitelist ข้างบน)
- [x] E2E บน QA (หลัง deploy):
  - `assignments/:id` — Fact Status = **soft น้ำเงิน "In Progress"** (จุดที่ผู้ใช้รายงาน)
  - Dashboard — Priority Assignments status pills มีสี (Active น้ำเงิน / Due Soon เหลือง / Overdue แดง)
  - `courses/:id` — tab learners/assignments แสดง "In Progress"/"Not Started" (มีวรรค)
  - Gantt, User editor/detail, Bulk assign, Course editor — ตามข้อที่แก้
- [x] หน้าอื่นที่ใช้ `StatusText` binary (users, master-data, content-library) — **หน้าตาเดิมไม่เปลี่ยน**

## Implementer Notes

- พัฒนาเสร็จสมบูรณ์ 100% ตามขอบเขตการแก้ไขของ PLAN-053
- ยืนยันว่าการแก้ไขทั้งหมดอยู่ในขอบเขตเฉพาะฝั่ง React (ไม่มีการแตะ backend)
- รัน `npm run lint` และ `npm run build` ผ่าน 100% เรียบร้อย ไร้ข้อผิดพลาด
- ตรวจสอบผ่าน grep ยืนยันว่าไม่เหลือโค้ด `STATUS_TONE` หรือ `STATUS_LABELS` (นอกจากส่วนกลาง) และไม่เหลือ spinner/pill เขียนมือที่ไม่พึงประสงค์แล้ว

