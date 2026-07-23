# PLAN-132: ซ่อน Enrollment กำพร้าหลังลบ Assignment ให้สอดคล้องทุกมุมมอง (admin + reports)

- **Status**: VERIFIED
- **Assigned**: GitHub Copilot (GPT)
- **Created**: 2026-07-23

## Overview

**ปัญหาที่ผู้ใช้รายงาน:** ลบ Assignment แล้ว แต่ learner ยังโผล่อยู่ในหน้า Course → tab Learners (ฝั่ง admin) ทำให้สับสน

**พฤติกรรมปัจจุบัน:** `DeleteAssignmentAsync` (`iLearn.Application/Services/AssignmentService.cs:160`) soft-delete ตัว `Assignment` (ทั้ง batch) + `EnrollmentAssignment` links แต่**ไม่แตะ `Enrollment`** (ถูกต้องแล้ว — ต้องเก็บประวัติการเรียน ห้ามเปลี่ยนข้อนี้) ผลคือแต่ละมุมมองไม่สอดคล้องกัน:

| มุมมอง | หลังลบ assignment | สาเหตุ |
|---|---|---|
| Learner (my-courses) | ซ่อน ✓ (พฤติกรรมอ้างอิง) | `GetEffectiveSchedule` ใน `EnrollmentsController` โหลด links ด้วย `ignoreQueryFilters: true` แล้วเช็ค `hadDeletedAssignmentOnly` → `ShouldBeVisible = false` |
| Course detail → tab Learners | **ยังแสดงเป็น active/overdue** ✗ | `GetCourseLearnersAsync` (`CourseService.cs:564`) — global soft-delete query filter ซ่อน link ที่ลบไปจาก navigation → `AssignmentLinks` ว่าง → fallback วันที่ดิบใน enrollment |
| Course detail → KPI "Active Learners" | **ยังนับ** ✗ | `GetCourseDashboardAsync` (`CourseService.cs:707`) — `LearnerCount = enrollments.Count` นับทุก enrollment ของคอร์ส |
| Reports (Compliance/Transcript/Activity) | **ยังนับ** ✗ (bug แฝง) | `VisibleEnrollmentPredicate` (`ReportService.cs:424`) พังเงียบ — ดูรายละเอียดด้านล่าง |

**การตัดสินใจของผู้ใช้ (ยืนยันแล้ว):** enrollment ที่ assignment ถูกลบหมด = **ซ่อนทั้งหมด** ทุกมุมมอง admin + reports ให้ตรงกับฝั่ง learner (รวมถึงคนที่เรียนจบแล้วก็ซ่อนด้วย — ยอมรับผลข้างเคียงว่า transcript report จะไม่แสดงแถวนั้น; ข้อมูลใน DB ยังอยู่ครบ ไม่ได้ลบ)

## นิยามกลาง (visibility rule เดียว ใช้ทุกจุด)

Enrollment **มองเห็นได้** ก็ต่อเมื่อ (ประเมินโดย**เห็น link ที่ถูก soft-delete ด้วย** — ต้อง ignore query filter):

- มี link ≥ 1 ตัวที่ active (`!link.IsDeleted && link.Assignment != null && !link.Assignment.IsDeleted`), **หรือ**
- ไม่เคยมี link เลย (legacy enrollment ที่สร้างก่อนมีระบบ link — ต้องยังแสดง)

Enrollment ที่ "เคยมี link แต่ตอนนี้ถูกลบหมด" = ซ่อน — นี่คือกติกาเดียวกับ `hadDeletedAssignmentOnly` ใน `EnrollmentsController.GetEffectiveSchedule` (บรรทัด ~525)

> ⚠️ กับดักหลักของงานนี้: **global soft-delete query filter** (`AppDbContext.ApplySoftDeleteFilters`) ใส่ `!IsDeleted` ให้ทุก entity รวม `EnrollmentAssignment` — ดังนั้นใน query ปกติ `e.AssignmentLinks` จะ**ไม่มีทางเห็น link ที่ลบแล้ว** แยก "ไม่เคยมี link" กับ "link ถูกลบหมด" ไม่ได้ ต้อง query ตาราง link แบบ `IgnoreQueryFilters()` แยกต่างหาก (หรือโหลด entity ด้วย `ignoreQueryFilters: true` แล้วกรองมือแบบที่ EnrollmentsController ทำ)

## Scope of Changes

### §1 ReportService — แก้ `VisibleEnrollmentPredicate` ที่พังเงียบ

`iLearn.Application/Services/ReportService.cs:424`:

```csharp
e => !e.AssignmentLinks.Any()
  || e.AssignmentLinks.Any(al => !al.IsDeleted && al.Assignment != null && !al.Assignment.IsDeleted);
```

Comment บอกว่าตั้งใจซ่อน enrollment ที่ assignment ถูกลบหมด แต่เพราะ query filter กรอง link ที่ลบออกจาก navigation ไปก่อนแล้ว เงื่อนไขแรก `!e.AssignmentLinks.Any()` จึงเป็น **true** สำหรับ enrollment กำพร้า → หลุดเข้ารายงานทุกตัวที่ใช้ predicate นี้ (Compliance, Transcript, Activity monthly trends)

**วิธีแก้ (แนะนำ):** เปลี่ยนจาก expression บน navigation เป็น subquery จากตาราง link ตรง ๆ (pattern เดียวกับ `BuildDivisionScopedEnrollmentQuery` ในไฟล์เดียวกัน):

```csharp
// enrollment ids ที่ "เคยมี link" (รวมที่ลบแล้ว) — ต้อง IgnoreQueryFilters
var everLinkedIds = _enrollmentAssignmentRepo.GetQuery().IgnoreQueryFilters()
    .Select(ea => ea.EnrollmentId).Distinct();

// enrollment ids ที่มี link active (query filter กรอง link ที่ลบให้แล้ว แต่ต้องเช็ค assignment เอง)
var activeLinkedIds = _enrollmentAssignmentRepo.GetQuery()
    .Where(ea => ea.Assignment != null && !ea.Assignment.IsDeleted)
    .Select(ea => ea.EnrollmentId).Distinct();

query.Where(e => activeLinkedIds.Contains(e.Id) || !everLinkedIds.Contains(e.Id));
```

- แปลง `VisibleEnrollmentPredicate` (static field) เป็น instance method เช่น `ApplyVisibleEnrollmentFilter(IQueryable<Enrollment>)` เพราะต้องใช้ repo — ปรับผู้เรียกทั้ง 2 จุด (บรรทัด ~330 monthly trends และ ~446 `BuildVisibleEnrollmentRowsQuery`) — คง XML doc comment เดิมไว้และอัปเดตให้ตรงความจริง
- ถ้า `IRepository.GetQuery()` ยังไม่มีทางเลือก ignore filters: ใช้ `.IgnoreQueryFilters()` ต่อท้าย IQueryable ได้เลย (เป็น EF extension) — **ห้าม**เพิ่ม overload ใหม่ลง interface ถ้าไม่จำเป็น
- หมายเหตุ: หลังแก้ ตัวเลขรายงาน (compliance rate, overdue, completions) จะ**ลดลง**ได้ในข้อมูลจริง เพราะ enrollment กำพร้าที่เคยถูกนับผิดจะหายไป — นี่คือ intended behavior

### §2 CourseService.GetCourseLearnersAsync — tab Learners (จุดที่ผู้ใช้เห็นปัญหา)

`iLearn.Application/Services/CourseService.cs:564`:

- โหลด enrollment ด้วย `ignoreQueryFilters: true` + `includeProperties: "AssignmentLinks.Assignment"` (ตอนนี้ include แค่ `AssignmentLinks` และไม่ ignore filters)
- เพราะ ignore filters แล้ว ต้องกรองมือเพิ่ม: `!e.IsDeleted` ที่ตัว enrollment
- ใช้กติกากลาง: ถ้า `e.AssignmentLinks.Any()` (รวม deleted) แต่ไม่มี link active → **ข้าม enrollment นั้นไปเลย** (ไม่ใส่ในผลลัพธ์)
- effective dates ต้องคิดจาก **active links เท่านั้น** (ตอนนี้บรรทัด 590–591 ใช้ทุก link — หลัง ignore filters จะรวม link ที่ลบแล้วเข้ามาด้วย ถ้าไม่กรองจะผิด): มี active link → `Min(StartDate)`/`Max(DueDate)` จาก active links; ไม่มี link เลย → fallback คอลัมน์ enrollment (ตรงกับกติกา effective dates ใน CLAUDE.md / PLAN-086)

### §3 CourseService.GetCourseDashboardAsync — KPI "Active Learners"

`iLearn.Application/Services/CourseService.cs:700–711`: `LearnerCount` / `CompletedCount` นับจากทุก enrollment ของคอร์ส — ปรับให้นับเฉพาะ enrollment ที่มองเห็นได้ตามกติกากลาง (ใช้ subquery pattern จาก §1 หรือโหลด link แล้วกรองมือ — เลือกทางที่ไม่ N+1 และไม่โหลด `FileStorage.Data`)

### §4 Unit tests (เพิ่มใน `iLearn.Tests`)

มี `ReportServiceTests.cs` อยู่แล้ว (in-memory pattern) — เพิ่มเคสอย่างน้อย:

1. enrollment ที่ link เดียวถูก soft-delete (ทั้ง link+assignment) → ไม่ปรากฏในผลรายงาน (`BuildVisibleEnrollmentRowsQuery` ผ่าน public method เช่น compliance)
2. enrollment ไม่มี link เลย (legacy) → ยังปรากฏ
3. enrollment มี link ลบแล้ว 1 + active 1 → ปรากฏ และใช้วันที่จาก active link เท่านั้น
4. `GetCourseLearnersAsync`: เคสเดียวกับข้อ 1–3
5. เคส `RemoveLearnerFromAssignmentAsync` แล้ว learner หายจาก `GetCourseLearnersAsync` (ใช้กลไก soft-delete link เดียวกัน — ควรถูกคลุมโดยกติกาเดียวกันอัตโนมัติ)

> หมายเหตุ in-memory DB กับ query filter: EF InMemory เคารพ `HasQueryFilter` + `IgnoreQueryFilters()` ปกติ — ถ้า test infra ของ repo mock `GetQuery()` เอง ให้ดูว่า filter ถูกจำลองไว้อย่างไรก่อนเขียน test (อย่า assert พฤติกรรมที่ infra ไม่ได้จำลอง)

## Out of Scope / ห้ามแตะ

- **ห้ามแก้ `DeleteAssignmentAsync` ให้ลบ `Enrollment`** — เก็บประวัติไว้ตามเดิม
- ห้ามแตะ `EnrollmentsController.GetEffectiveSchedule` / my-courses (ฝั่ง learner ถูกอยู่แล้ว — เป็น reference behavior)
- ไม่มี migration / ไม่มี schema change
- ไม่มี frontend change — response shape เดิมทุก endpoint (ค่าเปลี่ยน แต่ contract ไม่เปลี่ยน) — ไม่ต้องแก้ React types
- `GetCourseSummaryReportAsync` (เพิ่ง fix ใน PLAN-131 รอบ ReportService) นับ assignment ผ่าน link ภายใต้ query filter อยู่แล้ว = ไม่นับตัวที่ลบ → ไม่ต้องแก้ (แต่ถ้า implement §1 แล้วกระทบ ให้จดใน Implementer Notes)
- Division scoping (`BuildDivisionScopedEnrollmentQuery`) ใช้ link ภายใต้ filter — หลังลบ assignment enrollment จะหลุดจาก scope ของ admin ระดับ division ไปเอง = สอดคล้องกับการซ่อน ไม่ต้องแก้

## Verification (รันก่อนปิดงาน)

```powershell
dotnet build iLearn.Tests -o artifacts\verify-test
dotnet test artifacts\verify-test\iLearn.Tests.dll
Remove-Item -Recurse -Force artifacts\verify-test
```

Manual smoke (QA หลัง deploy): สร้าง assignment → learner โผล่ใน Course→Learners → ลบ assignment → learner หายจาก (1) tab Learners (2) KPI Active Learners (3) Compliance report — และ learner ที่ไม่เคยมี assignment (ถ้ามี legacy data) ยังแสดงปกติ

## Implementer Notes

- ดำเนินการครบ §1–§3:
  - `ReportService` แทน `VisibleEnrollmentPredicate` ด้วย `ApplyVisibleEnrollmentFilter(IQueryable<Enrollment>)` โดยใช้ subquery จาก `EnrollmentAssignment` + `IgnoreQueryFilters()` เพื่อตรวจแยก legacy (ไม่เคยมี link) ออกจาก orphaned (เคยมี link แต่ถูกลบหมด) ได้ถูกต้อง
  - `GetCourseLearnersAsync` โหลด enrollment แบบ `ignoreQueryFilters: true` + `AssignmentLinks.Assignment`, กรอง `!e.IsDeleted`, ตัด orphaned enrollment ออก, และคำนวณ effective dates จาก active links เท่านั้น
  - `GetCourseDashboardAsync` นับ `LearnerCount`/`CompletedCount` เฉพาะ visible enrollments ตามกติกาเดียวกัน
- เทสต์:
  - เพิ่มเคส report สำหรับ mixed deleted+active links ใน `ReportServiceTests`
  - เพิ่มไฟล์ `CourseServiceVisibilityTests` ครอบคลุม 4 เคส: soft-deleted-only link (remove-learner equivalent), legacy no links, mixed links effective dates, และ KPI นับเฉพาะ visible enrollments
- Verification:
  - `dotnet build iLearn.Tests -o artifacts/verify-test` ผ่าน
  - `dotnet test artifacts/verify-test/iLearn.Tests.dll` ผ่าน (Passed 247, Failed 0)
  - `Remove-Item -Recurse -Force artifacts/verify-test` เรียบร้อย
