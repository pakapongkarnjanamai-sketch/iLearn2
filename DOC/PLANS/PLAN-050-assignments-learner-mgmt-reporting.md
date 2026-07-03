# PLAN-050: ปรับปรุงหน้า Assignments — จัดการนักเรียน + Report สำหรับผู้ดูแลระบบ

- **Status:** DONE (2026-07-03)
- **Assigned:** ~~Part A + C → GPT · Part B → Gemini~~ → **ผู้ใช้สั่งให้ Claude Code implement ทั้งหมดเอง** (ทำครบ A+B+C ในรอบเดียว)
- **Reviewer:** Claude Code
- **สร้างเมื่อ:** 2026-07-03
- **อ้างอิง:** [PLAN-032](PLAN-032-assignments-controller-refactor.md)

> จากการวิเคราะห์หน้า `admin-react/assignments/:id` (Detail), `:id/report` (Report), list (`AssignmentsCRUD` / `vw_AssignmentList`) และ backend (`AssignmentsController`, `AssignmentService`, `AssignmentStatusKeys`) — พบบั๊กจริง 3 จุด + ช่องว่าง UX ด้านจัดการนักเรียนและ report หลายจุด แผนนี้แบ่ง 3 Part เรียงตาม priority

---

## สรุปข้อค้นพบ (Claude Code วิเคราะห์ 2026-07-03)

### บั๊กที่ยืนยันแล้ว (ต้องแก้)

1. **Status filter หน้า Report ใช้งานไม่ได้ 3 ใน 4 ปุ่ม** — `AssignmentReportPage.tsx` ประกาศ bucket `['Completed', 'In Progress', 'Not Started', 'Overdue']` แล้วเทียบ `row.status !== statusFilter` แบบตรงตัว แต่ backend ส่ง `InProgress` / `NotStarted` (ไม่มีเว้นวรรค — ดู `AssignmentStatusKeys.Learner`) → กด "In Progress"/"Not Started" ได้ตารางว่างเสมอ
2. **สถานะ `Overdue` ไม่เคยถูกคำนวณ** — `BuildAssignmentDashboardAsync` ใช้ `AssignmentStatusKeys.GetLearnerStatus(isCompleted, progress)` ซึ่งคืนได้แค่ Completed/InProgress/NotStarted ทั้งที่มี `GetScheduledLearnerStatus(...)` ที่รองรับ Overdue/Upcoming อยู่แล้ว และ per-link `StartDate`/`DueDate` ก็ select มาแล้วใน query เดียวกัน → ปุ่ม filter "Overdue" ว่างเสมอ และ admin มองไม่เห็นว่าใครเลยกำหนด
3. **Export CSV ชื่อไทยเพี้ยนใน Excel** — `exportCsv` สร้าง Blob UTF-8 โดยไม่มี BOM (`﻿`) → เปิดใน Excel (ผู้ใช้หลักของ admin) ชื่อนักเรียนภาษาไทยเป็น mojibake

### ข้อดีของหน้าปัจจุบัน (คงไว้ อย่า regress)

- โครง batch ถูกต้อง: หนึ่ง AssignmentNo หลาย course rule, dashboard รวม per-course + per-learner ใน endpoint เดียว (`Assignments/dashboard/{id}`) — โหลดครั้งเดียวใช้ได้ทั้ง 2 หน้า
- Snapshot pattern (`SnapshotCompleted/Progress/CompletedDate` บน link) ทำให้ history ของ batch เก่าไม่เพี้ยนเมื่อ learner ถูก reset ในบริบทอื่น
- Add learners มี 2 ทาง (Directory picker + bulk paste EIds) พร้อม dedupe อัตโนมัติ; reset/remove ใช้ `useConfirm` ตาม convention
- Soft delete ทั้งระบบ + division scoping (`EnsureDivisionAccess`) ครบทุก mutation
- List ใช้ `vw_AssignmentList` (SQL paging จริง) — เร็ว
- Learner name enrichment ผ่าน `/api/Learner/all` (server cache 24h) — ไม่มี N+1

### ข้อเสีย / ช่องว่าง

- **จัดการนักเรียน:** tab Learners ใน Detail **ไม่มี search/filter** (มีแค่ Load more ทีละ chunk — batch หลักร้อยคนหาคนแทบไม่ได้), ไม่มี bulk select → reset/remove ทีละคนเท่านั้น (backend `ResetEnrollmentsAsync` รับ `LearnerCodes` หลายคน + `RuleIds` per-course อยู่แล้วแต่ UI ไม่ใช้), reset ได้แค่ "ทุกคอร์สของคนนั้น" ทั้งที่ backend รองรับ per-course, bulk paste EIds **ไม่ validate กับ directory** (พิมพ์ผิดก็สร้าง enrollment ขยะ — `AssignCoursesToEmployees` ไม่เช็ค), ไม่แสดง Division/Department (ข้อมูล fetch มาแล้วแต่ทิ้ง ใช้แค่ Name)
- **Endpoint `POST Assignments/{id}/courses` (AddCourses) มีอยู่แต่ไม่มี UI เรียก** — เพิ่มคอร์สเข้า batch เดิมไม่ได้จาก UI (ลบได้อย่างเดียว)
- **Report:** ตาราง render ทุก row ไม่แบ่งหน้า (ผิด convention `DETAIL_TABLE_CHUNK_SIZE`), ไม่มีมิติ Division/Department (ทั้งบนจอและใน CSV), ไม่มี summary Overdue, ไม่มี print stylesheet (`window.print()` ติด sidebar/toolbar มาด้วย), CSV ไม่มีคอลัมน์ Start/Due
- **List:** view มี `Status`, `LearnerCount`, `CourseCount`, `HasDeletedCourse` คำนวณไว้แล้วแต่ config ไม่แสดง; คอลัมน์ Division แสดง `divisionId` เป็นตัวเลขดิบ
- เล็กน้อย: `Math.round(...)%` inline แทน `formatPercent` (ผิดกติกา format.ts), StatusBadge โชว์ raw `InProgress`/`NotStarted` ไม่มีเว้นวรรค, ข้อความ confirm ตอน remove learner บอกว่า "Enrollment will be deleted" แต่จริง ๆ soft-delete แค่ link (progress ยังอยู่), Detail ไม่แสดง `createdBy` ทั้งที่ DTO มี

---

## Part A — แก้บั๊ก + contract ฝั่ง backend (GPT — ทำก่อน)

### A1. คำนวณ learner status แบบมีกำหนดเวลา
- [ ] `AssignmentService.BuildAssignmentDashboardAsync`: เปลี่ยนจาก `GetLearnerStatus(row.IsCompleted, row.Progress)` → `GetScheduledLearnerStatus(row.IsCompleted, row.Progress, row.StartDate, row.DueDate, _dateTime.Now)` (ข้อมูล start/due อยู่ใน `DashboardLearnerRow` แล้ว)
- [ ] **Contract change:** field `status` ใน `LearnerProgressDto` จะมีค่า `Overdue`/`Upcoming` เพิ่ม — ฝั่ง React `statusTone()` ใน `StatusBadge.tsx` รองรับ `Overdue` อยู่แล้ว แต่ต้อง grep การใช้ `status` ทั้ง 2 หน้า (`AssignmentDetailPage`, `AssignmentReportPage`) แล้วอัปเดต type comment ให้ตรง
- [ ] เพิ่ม/อัปเดต xUnit test ของ dashboard status (มี test โปรเจค `iLearn.Tests` อยู่แล้ว)

### A2. เติมมิติองค์กรใน LearnerProgressDto
- [ ] `LearnerProgressDto`: เพิ่ม `Division`, `Department` (nullable string) — map จาก `ExternalLearnerDto` ที่ `GetLearnersByCodesAsync` คืนอยู่แล้ว (ตอนนี้ใช้แค่ `Name`) → **ไม่มี HTTP call เพิ่ม**
- [ ] Sync type ฝั่ง React ทั้ง 2 หน้า พร้อมคอมเมนต์ `// Mirrors LearnerProgressDto (...)` ตามกติกา API Contract Sync

### A3. แก้หน้า Report (บั๊ก + convention)
- [ ] Filter bucket ใช้ค่า key จริง (`Completed`/`InProgress`/`NotStarted`/`Overdue`) + สร้าง label map แสดงผล (`InProgress` → "In Progress") — แนะนำทำ helper `learnerStatusLabel()` ใน `src/lib/` ให้ Detail page ใช้ด้วย
- [ ] `exportCsv`: prepend `﻿` BOM + เพิ่มคอลัมน์ Division, Department, Start Date, Due Date + ตั้งชื่อไฟล์มีวันที่ (`assignment-{no}-report-YYYYMMDD.csv`)
- [ ] แทน `Math.round(...)%` ด้วย `formatPercent` จาก `src/lib/format.ts` (ทั้ง Report และจุดที่แตะใน scope)
- [ ] ตาราง learner ใช้ pattern `DETAIL_TABLE_CHUNK_SIZE` (Showing X of Y + Load more) ตาม `src/lib/tableStandards.ts`

### A4. หน้า List — โชว์ข้อมูลที่ view มีอยู่แล้ว
- [ ] `moduleConfigs.ts` → `assignments.columns`: เพิ่ม `status` (ใช้ cellRender + Badge tone ตาม `statusTone`) และ `learnerCount` (ค่าอยู่ใน `vw_AssignmentList` แล้ว — เช็คว่า CRUD Get ส่ง field เหล่านี้จริงก่อนเพิ่ม)

**Verify A:** `npm run lint && npm run build`; `dotnet build iLearn.Tests -o artifacts\verify-test && dotnet test ...`; เปิดหน้า report ของ batch ที่เลย due แล้ว → filter Overdue มีข้อมูล; export CSV เปิดใน Excel ชื่อไทยถูก

---

## Part B — จัดการนักเรียนในหน้า Detail (Gemini — หลัง A merge)

> ไฟล์หลัก: `AssignmentDetailPage.tsx` (Gemini เป็นเจ้าของไฟล์นี้ใน scope นี้ — GPT ห้ามแตะระหว่างทำ Part B)

### B1. ค้นหา + filter ใน tab Learners
- [ ] ช่อง search (code/name) + ปุ่ม filter ตาม status (ใช้ helper label จาก A3) — filter client-side จาก data ที่โหลดแล้วพอ (payload มีครบ)
- [ ] แสดง Division/Department ใต้ชื่อ (จาก A2)
- [ ] ชื่อ learner ลิงก์ไปหน้า Learner Profile (route มีอยู่แล้ว — ดู `App.tsx` learners route)

### B2. Bulk operations
- [ ] Checkbox เลือกหลายคน + action bar: **Reset selected** (ยิง `POST {id}/reset-enrollments` ด้วย `learnerCodes` หลายตัว — backend รองรับแล้ว ไม่ต้องแก้)
- [ ] **Remove selected:** เพิ่ม endpoint ฝั่ง backend `POST Assignments/{id}/learners/bulk-remove` (body: `{ learnerCodes: string[] }`) — ลอก logic จาก `RemoveLearnerFromAssignmentAsync` ให้รับหลาย code ใน transaction เดียว (อย่า loop ยิง DELETE ทีละคนจาก UI) + สร้าง response DTO จริงตาม convention (ไม่ใช้ anonymous object) + sync type ฝั่ง React
- [ ] ทั้งสอง action ผ่าน `useConfirm` แสดงจำนวนคนที่จะโดน

### B3. Reset ราย course
- [ ] ในแถว course ย่อยของ learner เพิ่มปุ่ม reset เฉพาะคอร์สนั้น → ยิง `reset-enrollments` ด้วย `learnerCodes: [code], ruleIds: [assignmentRuleId]` (backend `ResetEnrollmentsDto.RuleIds` รองรับแล้ว — **ต้องส่ง `assignmentRuleId` ลงมาใน grouped course object ด้วย** ตอนนี้ทิ้งไประหว่าง group)
- [ ] แก้ข้อความ confirm ของ reset/remove ให้ตรงพฤติกรรมจริง (reset = ล้าง progress ทุกคอร์สที่เลือกใน batch นี้; remove = ถอด link ออกจาก batch, ประวัติ enrollment ไม่หาย)

### B4. เพิ่มคอร์สเข้า batch (UI สำหรับ endpoint ที่มีอยู่แล้ว)
- [ ] ControlAction "Add Courses" → modal เลือกคอร์สจาก `Assignments/lookup-courses` (กรองตัวที่อยู่ใน batch แล้วออก) → `POST Assignments/{id}/courses` (`{ courseIds: [...] }` — mirror `ManageAssignmentCoursesDto`)

### B5. Validate EIds ตอน bulk import
- [ ] ก่อน "Add to Queue" ตรวจ code กับ directory (มี `LearnerDirectorySelector` ใช้ endpoint Learners อยู่แล้ว — ใช้ endpoint เดิม lookup แบบ batch/ทีละหน้า) → code ที่ไม่พบให้ขึ้นรายการ "ไม่พบในระบบ" ให้ admin ตัดสินใจ (เอาออก/ยืนยันเพิ่มทั้งที่ไม่พบ) — **ห้าม block ทั้งชุด** เพราะ directory อาจตามหลังพนักงานใหม่
- [ ] แสดง `createdBy` ใน Overview card (DTO มีแล้ว)

**Verify B:** lint/build ผ่าน; ทดสอบกับ batch จริงบน QA: search เจอคน, bulk reset 2 คน, reset รายคอร์ส 1 คอร์ส, เพิ่มคอร์สใหม่เข้า batch แล้ว learner เดิมได้ enrollment คอร์สใหม่, paste EId ผิด 1 ตัวเห็น warning

---

## Part C — ยกระดับ Report (GPT — หลัง A merge, ไฟล์หลัก `AssignmentReportPage.tsx`)

- [ ] **Summary การ์ดเพิ่ม:** Overdue count + Not Started count (นับจาก learners หลัง A1) — ใช้ `Badge`/`Fact` ตาม convention
- [ ] **Group-by Department view:** ตารางสรุป per-department (client-side group จาก field ใหม่ A2): Department | Learners | Completed | Overdue | % — ช่วยผู้ดูแลไล่ตามหัวหน้าแผนก
- [ ] **Filter ตาม course:** dropdown เลือกคอร์ส (batch หลายคอร์สข้อมูลปนกันใน table เดียว)
- [ ] **Print stylesheet:** `@media print` ซ่อน sidebar/toolbar/ปุ่ม ให้เหลือ summary + ตาราง (เช็คว่ามี global css print อยู่แล้วหรือไม่ก่อนเพิ่ม)
- [ ] **Export ตัวเลือก scope:** ปุ่ม export ให้เลือก "ทั้งหมด" หรือ "ตาม filter ปัจจุบัน" (ตอนนี้ export เฉพาะ filtered โดยไม่บอกผู้ใช้)

**Verify C:** lint/build; report ของ batch >1 คอร์ส filter per-course ถูก; print preview ไม่มี sidebar; CSV ครบคอลัมน์

---

## นอก scope (บันทึกไว้ ไม่ทำรอบนี้)

- **แจ้งเตือน/ส่งอีเมลตาม learner ที่ Overdue** — ต้องมี SMTP/notification infra ก่อน (ยังไม่มีในระบบ) → แผนแยกถ้าผู้ใช้ต้องการ
- **Excel (.xlsx) export** — CSV+BOM แก้ปัญหา Excel ได้แล้ว; xlsx ต้องเพิ่ม dependency ฝั่ง client → รอ feedback
- **Server-side paging ของ dashboard endpoint** — ตอนนี้ payload ทั้ง batch โหลดทีเดียว รับได้ที่สเกลปัจจุบัน (หลักร้อย–พันคน) ถ้า batch >5,000 enrollment ค่อยทำ endpoint แบบ paged
- แก้หน้า Gantt (`assignments/gantt`) — ไม่อยู่ใน scope คำขอ

## Constraints

- ❌ ห้ามแตะ `iLearn.Admin` (MVC เดิม)
- ❌ ห้ามเปลี่ยน semantics ของ snapshot fields (`SnapshotCompleted/Progress/CompletedDate`) — report ประวัติศาสตร์พึ่งมัน
- ✅ ทุก contract change (A1 status, A2 fields, B2 endpoint) ต้อง sync type React + คอมเมนต์ Mirrors ในงานเดียวกัน
- ✅ UI ใหม่ทุกจุดใช้ shared components (`Badge`, `AppButton`, `useConfirm`, `ListToolbar`, `Card`) + format ผ่าน `src/lib/format.ts`
- ⚠️ Part B กับ C แตะคนละไฟล์หลัก แต่ทั้งคู่พึ่ง A — ห้ามเริ่มก่อน A DONE; ถ้าทำพร้อมกันห้ามข้ามไฟล์กัน

## Decision points (ผู้ใช้)

1. B2 bulk-remove: ยืนยันว่าต้องการ หรือ bulk reset อย่างเดียวพอ
2. C group-by Department: พอ client-side หรืออยากได้ระดับ report แยกหน้า (ตอนนี้เสนอ client-side ใน page เดิม)
3. ลำดับ: เสนอ A → B → C (A เป็นบั๊กล้วน ควรไปก่อนสุด)

## Verification commands

```powershell
# จาก iLearn.Admin.React
npm run lint ; npm run build

# Backend
dotnet build iLearn.Tests -o artifacts\verify-test
dotnet test artifacts\verify-test\iLearn.Tests.dll
Remove-Item -Recurse -Force artifacts\verify-test
```

## Implementer Notes

_(Claude Code, 2026-07-03 — ผู้ใช้สั่งให้ implement เองทั้งหมด)_

ทำครบทุกข้อใน Part A/B/C ยกเว้นรายการที่ระบุว่าปรับจากแผน:

**สิ่งที่ต่างจากแผน:**
1. **B2 bulk-remove:** endpoint เป็น `POST Assignments/{id}/learners/bulk-remove` body reuse `ManageAssignmentLearnersDto` → `{ employeeCodes: string[] }` (แผนร่างไว้ `learnerCodes` — เปลี่ยนให้ consistent กับ AddLearners) — response = `AssignmentRemoveLearnersResponseDto { success, message, removedCount }`; ตัวลบรายคนเดิม (`DELETE {id}/learners/{code}`) refactor ให้ delegate ไป bulk ภายใน service เดิม พฤติกรรมภายนอกไม่เปลี่ยน
2. **B5 validate EIds:** ทำ client-side ล้วน — ยิง `Learners/Get` ด้วย OR-filter (`EId = code1 or ...`) chunk ละ 40 code ต่อ request; code ที่พบจะ enrich ชื่อ/division/department เข้า queue, code ที่ไม่พบติด badge "Not found in directory" + ปุ่ม "Remove Not Found" + confirm ก่อน save ถ้ายังมีค้าง — ไม่ต้องเพิ่ม endpoint ใหม่
3. **A1:** dashboard status ตอนนี้มีค่า `Overdue`/`Upcoming` เพิ่ม — เพิ่ม `'Upcoming' → warning` ใน `statusTone()` (`StatusBadge.tsx`) และเพิ่ม `Upcoming` เข้า filter bucket ทั้ง 2 หน้า; ไม่เพิ่ม xUnit ใหม่เพราะ `GetScheduledLearnerStatus` มี coverage อยู่แล้วใน `AssignmentStatusKeysTests.cs` (การเปลี่ยนคือ wiring จุดเรียกเท่านั้น)
4. **C print:** ทำระดับ global — `print:hidden` ที่ `Header`/`Sidebar` (layout) + `print:h-auto print:overflow-visible` ที่ content wrapper ใน `AppLayout` (ไม่งั้น print ถูก clip เหลือหน้าเดียว) + `print:hidden` ที่ controls sidebar/toolbar/load-more ของหน้า report; ปุ่ม Print จะ setVisibleRows(ทั้งหมด) ก่อนค่อย `window.print()` เพื่อไม่ให้ตารางโดนตัดตาม chunk
5. **CSV BOM:** ใช้ตัวอักษร U+FEFF literal ใน source (มี comment กำกับ) — escape `﻿` ถูก normalize เป็นตัวจริงระหว่างเขียนไฟล์ ผลลัพธ์เท่ากัน
6. **helper ใหม่:** `src/lib/learnerStatus.ts` (`LEARNER_STATUS_KEYS`, `learnerStatusLabel`) ใช้ร่วม 3 จุด (report/detail/list)

**Verified:** `npm run lint` clean · `npm run build` (tsc -b + vite) ผ่าน · `dotnet build iLearn.Tests -o artifacts\verify-test` 0 errors · `dotnet test` 118 passed / 0 failed · ยังไม่ได้ E2E บน QA (แนะนำเปิด batch จริงที่เลย due ตรวจ filter Overdue + export CSV เปิดใน Excel)
