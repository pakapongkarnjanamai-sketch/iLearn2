# PLAN-114: Assignment detail/report ปรับ Overview — ตัด caption chart, Created By เป็นชื่อ, stat cards แทน Fact

- **Status:** READY
- **Assigned:** GitHub Copilot (backend เล็ก + React หน้าเดียว)
- **Reviewer:** Claude Code
- **สร้างเมื่อ:** 2026-07-22
- **ที่มา:** ผู้ใช้รีวิวหน้า `/assignments/275` + `/assignments/275/report` บน QA (หลัง PLAN-112 ขึ้น) ขอ 4 ข้อ:
  1. เอาข้อความ hint `Click a segment to filter the table` / `Click a bar to filter the table` ออก (ทั้ง 2 หน้า)
  2. `Created By` แสดงแค่ Nid (`j2818`) — ให้เพิ่มการแสดงชื่อ
  3. ตัด Fact `Completed` กับ `Completion Rate` ออกจาก Overview (ดูจาก donut ได้)
  4. `Learners` / `Courses` / `Status` แสดงเป็น **stat card** แบบเดียวกับหน้า Report Summary
- **อ่าน `iLearn.Admin.React/README.md` (UI Conventions + API Contract Sync) ก่อนเริ่ม**

---

## วินิจฉัย (ยืนยันจากโค้ด)

- **ข้อ 1:** caption อยู่ใน `AssignmentReportCharts.tsx` บรรทัด **93** (`StatusDonut`) และ **166** (`CourseCompletionBars`) — component เดียวใช้ทั้งหน้า Report และ Overview (PLAN-112) ⇒ ลบ 2 บรรทัดจบทั้งสองหน้า
- **ข้อ 2:** `AssignmentDashboardDto.CreatedBy` = Nid ดิบจาก `Assignment.CreatedBy` (`AssignmentDashboardService` บรรทัด ~136). มี **`ILearnerApiService.GetEmployeesByNidsAsync(nids)`** ให้ resolve Nid→ชื่อได้อยู่แล้ว (pattern เดียวกับ `LookupLearnerNamesAsync` ที่มี try/catch ในไฟล์เดียวกัน)
- **ข้อ 3/4:** Overview ของ `AssignmentDetailPage.tsx` เป็น `FactGrid` (Learners/Completed/Completion Rate/Status/Start/Due/Created By/Learner Group) + donut (PLAN-112). Stat tile ต้นแบบอยู่ `AssignmentReportPage.tsx` บรรทัด ~316-320 (`rounded-lg border border-slate-200 bg-slate-50/50 px-3 py-2.5 text-center` + label uppercase + ตัวเลข `tabular-nums`). `totalCourses` มีใน DTO อยู่แล้ว (ตอนนี้ Overview ไม่ได้โชว์)

## Scope

### §1 ตัด caption (React — `AssignmentReportCharts.tsx` เท่านั้น)

- ลบ `<p>Click a segment to filter the table</p>` (บรรทัด ~93) และ `<p>Click a bar to filter the table</p>` (~166)
- **คง behavior คลิก filter ไว้เหมือนเดิม** — ตัดเฉพาะข้อความ hint
- นี่คือข้อยกเว้นที่อนุญาตให้แตะไฟล์ Report (PLAN-112 เคยห้าม) — จำกัดแค่ลบ 2 บรรทัดนี้

### §2 Created By แสดงชื่อ (backend + React) **[CONTRACT — additive]**

- `AssignmentDashboardDto` เพิ่ม `public string? CreatedByName { get; set; }`
- `AssignmentDashboardService.GetDashboardAsync` (จุดสร้าง DTO ~136): resolve ผ่าน `GetEmployeesByNidsAsync([mainRule.CreatedBy])` → `CreatedByName = ชื่อ ?? null`
  - **ห่อ try/catch** — lookup พังต้องไม่ทำ endpoint ล้ม (กติกา side-effect CLAUDE.md); fail → `CreatedByName = null`
  - อย่าแตะ `AssignmentHistoryDto` (~314 หน้า list) — นอก scope
- React (`AssignmentDetailPage.tsx`): mirror type +`createdByName?: string | null`; Fact แสดง `createdByName` เป็นหลัก + Nid เป็นบรรทัดรอง (`text-xxs font-mono text-slate-400` แบบเดียวกับ pattern code ใต้ชื่อที่ใช้ทั่วแอป); ไม่มีชื่อ → แสดง Nid เดิม
- หน้า Report แสดง Created By ไหมตรวจด้วย — ถ้ามีให้ใช้ field ใหม่เหมือนกัน

### §3 ตัด Fact Completed + Completion Rate (React)

- ลบ `<Fact label="Completed">` และ `<Fact label="Completion Rate">` ออกจาก Overview — donut มีครบแล้ว (center = completion %, legend มีจำนวน)

### §4 Learners / Courses / Status เป็น stat cards (React)

- แถว stat tiles บนสุดของ Overview: **Learners** (`totalEmployees`) / **Courses** (`totalCourses` — ใหม่ ไม่เคยโชว์) / **Status** (`<StatusBadge>{assignmentStatus}</StatusBadge>` แทนตัวเลข) — ลอก markup tile จาก `AssignmentReportPage.tsx:316` ให้หน้าตาเหมือน Report Summary (`grid grid-cols-3 gap-3`)
- Fact ที่เหลือ (Start Date / Due Date / Created By / Learner Group ถ้ามี) คงเป็น `FactGrid` ใต้ tiles
- donut ยังอยู่ฝั่งขวาตาม layout PLAN-112 (tiles + facts ฝั่งซ้าย)

### นอก Scope (ห้ามทำ)

- ห้ามแตะ logic คลิก filter ของ chart ทั้งสอง (ตัดแค่ข้อความ)
- ห้ามแตะ `AssignmentHistoryDto`/หน้า Assignments list
- ห้ามแตะ stat tiles ของหน้า Report (ของเดิมถูกอยู่แล้ว) นอกจากถ้าจะ extract tile เป็น shared component — **อนุญาต** แต่ต้อง render ผลเหมือนเดิมทั้งสองหน้า (ถ้า extract ให้วางใน `src/components/ui/`)
- ห้ามแตะ endpoint อื่น / ไม่มี migration

## Contract ที่เปลี่ยน

- `AssignmentDashboardDto` +`createdByName` (nullable, additive) — consumer: `AssignmentDetailPage.tsx` + `AssignmentReportPage.tsx` (mirror type ต้องอัปเดตทั้งคู่ถ้า declare แยก)

## Verification

```powershell
dotnet build iLearn.Tests -o artifacts\verify-test
dotnet test artifacts\verify-test\iLearn.Tests.dll
Remove-Item -Recurse -Force artifacts\verify-test
# React
cd iLearn.Admin.React; npm run lint; npm run build
```

Manual (QA — `/assignments/275` + `/assignments/275/report` + `/assignments/288`):
1. ไม่มีข้อความ "Click a segment/bar..." ทั้ง detail + report; คลิก segment/bar ยัง filter ได้เหมือนเดิม
2. Created By แสดงชื่อจริง + Nid รอง; batch ที่ resolve ชื่อไม่ได้ → แสดง Nid ไม่ error
3. Overview: มี stat cards Learners/Courses/Status หน้าตาเหมือน Report Summary; ไม่มี Fact Completed/Completion Rate แล้ว; Start/Due/Created By ยังอยู่; donut ปกติ
4. จอแคบ: tiles ไม่ล้น (grid ตกบรรทัดได้), donut stack ใต้เหมือนเดิม
5. console 0 error

## Deploy note

- แตะ **API + Admin React** (ไม่มี migration) — deploy API ก่อนหรือพร้อมกัน (React อ่าน field ใหม่แบบ optional → API เก่าก็ไม่พัง)
- QA → verify → PROD (รอผู้ใช้ยืนยัน)

## Implementer Notes

_(เติมโดย implementer)_
