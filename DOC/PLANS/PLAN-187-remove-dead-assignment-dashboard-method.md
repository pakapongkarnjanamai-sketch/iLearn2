# PLAN-187: ลบ dead code `AssignmentDashboardService.GetDashboardAsync` + เติม test เคส never-linked enrollment

- **Status:** READY
- **Assigned:** GPT
- **Reviewer:** Claude Code
- **Priority:** Low
- **Estimated scope:** 4 ไฟล์ (1 service, 1 interface, 2 test files) — ลบโค้ดเป็นหลัก + เพิ่ม test 1 ตัว

## Problem

มาจาก Reviewer Notes ของ [PLAN-185](./PLAN-185-audit-ignore-query-filter-current-state.md) 2 ข้อ:

**1. `AssignmentDashboardService.GetDashboardAsync` เป็น dead code**

`AssignmentsController.cs:77` (endpoint `GET /api/Assignments/{id}/dashboard`) เรียก `_assignmentService.GetDashboardAsync(id, divisionId, ct)` = `AssignmentService.GetDashboardAsync` (3 args, ตัวใหม่) — grep ทั้ง solution **ไม่พบ call site ของ `IAssignmentDashboardService.GetDashboardAsync` (1 arg) เลย** method อื่นบน interface เดียวกัน (`ValidateBeforeAssignAsync`, `GetAssignmentHistoryPagedAsync`, `GetGroupHistoryAsync`, `ExtendDueDateAsync`, `GetLookupCoursesAsync`) ยังถูกเรียกอยู่ปกติ ⇒ ตัดทิ้งได้เฉพาะ method นี้ ไม่ใช่ทั้ง service

ผลเสียของการปล่อยไว้: PLAN-185 เพิ่ง "แก้ bug" ในนี้ไป 1 รอบและเขียน regression test คุมไว้ — โค้ดที่ไม่มีใครเรียกแต่มี test เขียว ทำให้ agent รอบถัดไปเข้าใจผิดว่านี่คือ dashboard ที่ใช้จริง แล้วแก้ผิดที่ (dashboard ตัวจริงคือ `AssignmentService.BuildAssignmentDashboardAsync`)

**2. เคส never-linked enrollment ยังไม่มี test คุม**

fix ของ PLAN-185 ใน `LearnersController.GetProfile` เพิ่มเงื่อนไข `hasAnyAssignmentLinks` เข้ามา ซึ่งนอกจากแก้เคส deleted-only link แล้ว ยังแก้เคส **enrollment ที่ไม่เคยมี assignment link เลย (self-enroll / legacy)** ที่ของเดิมขึ้น badge `Cancelled` ผิด ๆ ให้กลับไปขึ้น `Self Enroll` ตามที่ควรเป็น ([LearnerProfilePage.tsx:182-192](../../iLearn.Admin.React/src/pages/learners/LearnerProfilePage.tsx)) แต่ PLAN-185 ไม่ได้เขียน test คุมเคสนี้ ⇒ regression กลับมาได้เงียบ ๆ

## Scope (ทำแค่นี้)

### 1. ลบ dead method + สมาชิกที่ตายตาม

`iLearn.Application/Interfaces/Services/IAssignmentDashboardService.cs`
- ลบบรรทัด 11-12 (xml doc + `Task<AssignmentDashboardDto?> GetDashboardAsync(int assignmentId);`)

`iLearn.Application/Services/AssignmentDashboardService.cs`
- ลบ `GetDashboardAsync` ทั้ง method (บรรทัด 45-158 ของไฟล์ปัจจุบัน)
- ลบ private member ที่ไม่มีใครเรียกต่อ **หลังลบ method ข้างบนแล้ว** — ตรวจซ้ำด้วย grep ก่อนลบทุกตัว:
  - `LookupCreatedByNameAsync` (บรรทัด 335)
  - `LookupLearnerNamesAsync` (บรรทัด 355)
  - `private sealed class EnrollmentProjection` (บรรทัด 383)
- `ILearnerApiService _learnerApiService` จะกลายเป็น dependency ที่ไม่มีใครใช้ (ถูกใช้แค่ใน 2 lookup ข้างบน) ⇒ ลบทั้ง field + constructor parameter
  - **ไม่ต้องแก้ `iLearn.Application/DependencyInjection.cs`** — DI container resolve ctor param เอง ไม่มี manual `new`
  - construction site แบบ manual มีที่เดียวคือ `iLearn.Tests/AssignmentFlowTests.cs:351` ซึ่งจะถูกลบอยู่แล้วในข้อ 2

### 2. ลบ test ที่คุม dead code

`iLearn.Tests/AssignmentFlowTests.cs`
- ลบ `AssignmentDashboardService_GetDashboardAsync_ExcludesDeletedRulesFromCurrentCounts` ทั้ง test (เพิ่งเพิ่มใน PLAN-185, เป็น test เดียวที่ construct `AssignmentDashboardService` ตรง ๆ)
- ถ้า `using` / helper / fake ตัวไหนกลายเป็น unused หลังลบ ให้เก็บกวาดตาม (แต่ **ห้ามลบ fake ที่ test อื่นในไฟล์ยังใช้**)

### 3. เพิ่ม regression test เคส never-linked enrollment

`iLearn.Tests/LearnersControllerTests.cs` — เพิ่ม test ข้าง ๆ `GetProfile_DeletedOnlyAssignmentLink_IsCancelledNotActive` (ใช้ pattern + `InMemoryGenericRepository<Enrollment>` เดิมในไฟล์ได้เลย):

```
GetProfile_EnrollmentWithoutAssignmentLinks_IsNeitherActiveNorCancelled
```

- setup: `Enrollment` ที่ **ไม่มี `AssignmentLinks` เลย**, `IsCompleted = false`, มี `StartDate` และ/หรือ `DueDate` (เงื่อนไขที่ของเดิมทำให้ขึ้น Cancelled ผิด)
- assert: `hasActiveAssignment == false` **และ** `isAssignmentCancelled == false`
- ใส่คอมเมนต์สั้น ๆ ว่าเคสนี้ = self-enroll/legacy enrollment ⇒ UI ต้องขึ้น badge `Self Enroll` ไม่ใช่ `Cancelled` (อ้าง PLAN-185/PLAN-187)

## Out of scope (ห้ามแตะ)

- **ห้ามลบ `AssignmentDashboardService` ทั้ง class หรือ interface** — method ที่เหลืออีก 5 ตัวยังถูกเรียกจาก `AssignmentsController` / `EnrollmentService` / `CourseAssignmentService`
- **ห้ามลบ DTO** `AssignmentDashboardDto` / `CourseSummaryDto` / `LearnerProgressDto` / `DashboardChartDto` — `AssignmentService` (dashboard ตัวจริง) ยังใช้ทั้งหมด
- **ห้ามลบ `AssignmentStatusKeys.GetLearnerStatus`** ถึงแม้หลังงานนี้จะเหลือแค่ `AssignmentStatusKeysTests` ที่เรียก — เป็น public helper ใน `Common` ที่มี test คุมอยู่แล้ว (ตัวที่ใช้จริงใน `AssignmentService` คือ `GetScheduledLearnerStatus` คนละตัว) การตัดสินใจลบ/รวม 2 helper นี้เป็นงานคนละเรื่อง ให้จดใน Implementer Notes ถ้าคิดว่าควรทำ
- **ห้ามแตะ `AssignmentService.GetDashboardAsync` / `BuildAssignmentDashboardAsync`** — คือ path ที่ production ใช้จริง ผ่านรีวิว PLAN-185 มาแล้วว่า `Safe`
- ไม่ต้องรวม `InMemoryGenericRepository<T>` ที่ซ้ำกันระหว่าง `LearnersControllerTests` กับ helper ของ `AssignmentFlowTests` (Reviewer Notes ข้อ 3 ของ PLAN-185) — เป็นงาน test refactor แยก ถ้าจะทำให้เปิดแผนใหม่
- ไม่ต้อง deploy — งานนี้ไม่เปลี่ยนพฤติกรรม runtime ใด ๆ

## Acceptance criteria

- [ ] `rg "GetDashboardAsync" --glob "*.cs"` เหลือเฉพาะ `AssignmentService` / `IAssignmentService` / `AssignmentsController.cs:77`
- [ ] `rg "_learnerApiService" iLearn.Application/Services/AssignmentDashboardService.cs` ไม่พบผลลัพธ์
- [ ] build ผ่านโดยไม่มี warning ใหม่ (โดยเฉพาะ CS0169 unused field / CS0414)
- [ ] test เดิมทั้งหมดยังเขียว และจำนวน test ลดลง 1 (ลบ) + เพิ่ม 1 (ใหม่) = **294 เท่าเดิม**
- [ ] test ใหม่ `GetProfile_EnrollmentWithoutAssignmentLinks_IsNeitherActiveNorCancelled` **fail ถ้า revert เงื่อนไข `hasAnyAssignmentLinks` ออกจาก `LearnersController.cs:243`** (ยืนยันว่า test คุมของจริง — ลอง revert ชั่วคราวแล้วรันดู ก่อนใส่กลับ)
- [ ] endpoint `GET /api/Assignments/{id}/dashboard` ยังคืน response เดิม (ไม่ได้แตะ path ที่ใช้จริง)

## Verification

```powershell
# API รันใน VS อยู่ bin จะถูกล็อก ให้ build ออก artifacts
dotnet build iLearn.Tests -o artifacts\verify-plan187
dotnet test artifacts\verify-plan187\iLearn.Tests.dll
Remove-Item -Recurse -Force artifacts\verify-plan187
```

ไม่ต้องรัน React lint/build (ไม่แตะฝั่ง React) และไม่ต้อง deploy

## Implementer Notes
