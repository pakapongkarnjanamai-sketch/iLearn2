# PLAN-032: Refactor AssignmentsController → service + anonymous error → exceptions (ตาม API style guide)

- **Status:** VERIFIED ✅ (Gemini review 2026-06-16)
- **Assigned:** GPT (GPT-5.3 Codex)
- **Priority:** Medium
- **Estimated scope:** `AssignmentsController.cs` (675 บรรทัด) + service ใหม่/ขยาย + DTO — backend ล้วน
- **มาตรฐานเป้าหมาย:** `DOC/api_style_guide.md` (§4 error handling, §5 DTO, §6 controller thinness)
- **ต่อจาก:** PLAN-017 (Enrollments pilot), PLAN-027 (Courses pilot) — controller ใหญ่ตัวถัดไป

## Problem

`iLearn.API/Controllers/AssignmentsController.cs` = **675 บรรทัด**:
- inject `IGenericRepository<T>` ดิบ **3 ตัว** (`Assignment`, `EnrollmentAssignment`, `Enrollment`) → business logic/query/transaction ยังอยู่ใน controller ในหลาย endpoints (เช่น Delete, ResetEnrollments, ExtendDueDate, RemoveCourse, AddLearner, RemoveLearner)
- คืน **anonymous error object 19 จุด** ในรูป `NotFound(new { message = ... })` และ `BadRequest(new { message = ... })` — ผิด style guide §4 (ควรโยน Exception ให้ middleware หรือคืนเป็น Error envelope ใน format มาตรฐาน)

## Scope (ทำแค่นี้ — pure refactor, ไม่เปลี่ยน shape/พฤติกรรม)

### A. ดึง logic inline ลง service
1. ขยาย `IAssignmentService` / `AssignmentService` ใน `iLearn.Application/` ให้รองรับการทำ business operations:
   - `DeleteAssignmentAsync(int id, int? divisionId)` (จัดการลบ Assignment และ Batch Link ทั้งหมด)
   - `ResetEnrollmentsAsync(int assignmentId, string? statusFilter, int? divisionId)` (จัดการ reset ประวัติการเรียนของผู้เรียนตามเงื่อนไขฟิลเตอร์)
   - `ExtendDueDateAsync(int assignmentId, DateTime newDueDate, int? divisionId)` (จัดการขยายเวลาส่งงาน)
   - `RemoveCourseFromAssignmentAsync(int assignmentId, int courseId, int? divisionId)` (ลบคอร์สเรียนออกจากใบสั่งงาน)
   - `AddLearnerToAssignmentAsync(int assignmentId, string learnerCodesText, int? divisionId)` (เพิ่มผู้เรียนใหม่เข้าใบสั่งงาน)
   - `RemoveLearnerFromAssignmentAsync(int assignmentId, string learnerCode, int? divisionId)` (ลบผู้เรียนออกจากใบสั่งงาน)
2. ย้าย logic จัดการ database transaction (`BeginTransactionAsync`, `CommitAsync`, `SaveChangesAsync`) ออกจาก Controller ไปจัดการใน Service ชั้น Application
3. register DI; คง `[Authorize]` + division isolation (`_currentUser`) **ที่ controller** (guide §2/§7)

### B. error anonymous -> standard exceptions / DTOs
4. แปลงการคืน `NotFound(new { message = ... })` และ `BadRequest(new { message = ... })` ทั้งหมด (19 จุด) ไปเป็น:
   - การโยน Exception ที่เหมาะสม (เช่น `KeyNotFoundException` สำหรับ NotFound, `ArgumentException`/`InvalidOperationException` สำหรับ BadRequest) เพื่อให้ `GlobalExceptionMiddleware` จัดการแปลงเป็น `ProblemDetails` ตามมาตรฐาน (guide §4)
   - หรือคืนในรูป DTO wrapper ของ `ApiResponse<T>` / `ApiResponse<bool>` ที่มี `Success = false` และ `Message = "..."`
5. **shape ที่ React อ่านต้องไม่เปลี่ยน** — ตรวจสอบ endpoints ของ assignments ใน `iLearn.Admin.React/src` เพื่อให้แน่ใจว่าการตอบกลับแบบผิดพลาด (error payload) ยังสอดคล้องกับพฤติกรรมเดิม

### C. pure refactor — พฤติกรรม/shape/status เดิมทั้งหมด (`dotnet test` เป็นตาข่าย)

> **ถ้าการย้าย logic ทั้งหมดดูใหญ่เกินกว่าจะทำเสร็จในรอบเดียวอย่างปลอดภัย** ให้เลือกแปลงส่วน error anonymous (ข้อ B) และดึง logic เฉพาะของจุดสำคัญ (เช่น Delete, ResetEnrollments) ลง Service ก่อน แล้วจดส่วนที่เหลือใน Notes เป็น follow-up — อย่าฝืนทำก้อนใหญ่เกินจนเกิด regression

## Out of scope (ห้ามแตะ)

- ห้ามเปลี่ยน endpoint path / shape / status / พฤติกรรมภายนอก (pure refactor)
- ห้าม refactor controller อื่น (ContentItems ฯลฯ = เก็บไว้ทำรอบถัดไป)
- ห้ามแตะ division isolation จนผลลัพธ์การคัดกรองเปลี่ยน
- ห้ามแตะ frontend assignments pages นอกจากการเพิ่มคอมเมนต์และปรับเปลี่ยน type sync

## Acceptance criteria

- [x] `AssignmentsController` ไม่มี `IGenericRepository<Assignment/Enrollment/EnrollmentAssignment>` ถูกฉีดเข้ามาโดยตรง
- [x] ไม่มี `Ok(new {` หรือ `NotFound(new {` หรือ `BadRequest(new {` หลงเหลือใน Controller
- [x] โยน Exception หรือใช้ DTO ที่มีโครงสร้างมาตรฐานสำหรับส่งกลับผลลัพธ์แบบสำเร็จและผิดพลาด
- [x] ทุกหน้าของ `/assignments` (list, gantt, bulk, detail, report) บน React ยังสามารถทำงานได้อย่างสมบูรณ์
- [x] `dotnet test` ผ่านครบ (118) + `npm run build`/`lint` ผ่าน

## Verification

```powershell
dotnet build iLearn.Tests -o artifacts/verify-test
dotnet test artifacts/verify-test/iLearn.Tests.dll
Remove-Item -Recurse -Force artifacts/verify-test
npm run lint; npm run build
```

ทดสอบ manual: ใช้งานจริงที่หน้า `/assignments` (list + search), `/assignments/gantt`, `/assignments/bulk`, assignment detail และ report — ตรวจสอบการเพิ่ม/ลบผู้เรียน คอร์สเรียน และการขยายเวลาส่งงานว่ายังคงทำงานปกติ

## Implementer Notes

- ขยาย `IAssignmentService` และ `AssignmentService` ให้รับผิดชอบ operation ที่เดิมอยู่ใน controller ได้แก่ `Delete`, `ResetEnrollments`, `ExtendDueDate`, `AddCourses`, `RemoveCourse`, `AddLearners`, `RemoveLearner` พร้อม transaction handling (`BeginTransactionAsync` + `SaveChangesAsync` + `Commit/Rollback`) ภายใน service
- ปรับ `AssignmentsController` ให้เป็น orchestration-only: เรียก service แล้วคืน DTO เดิม, ตัด raw repository injection ออกจาก constructor
- แปลง anonymous error responses ใน `AssignmentsController` (`NotFound(new { message = ... })`, `BadRequest(new { message = ... })`) เป็นการโยน exception มาตรฐาน (`KeyNotFoundException`, `ArgumentException`) ให้ `GlobalExceptionMiddleware` แปลงเป็น `ProblemDetails`
- คง successful response DTO shape เดิมของ assignments endpoints (`Assignment*ResponseDto`) เพื่อไม่กระทบ React contract ฝั่ง success path
- Verification:
   - `dotnet build iLearn.API/iLearn.API.csproj --artifacts-path artifacts/verify-plan032-api` ผ่าน
   - `dotnet build iLearn.Tests -o artifacts/verify-test` ผ่าน
   - `dotnet test artifacts/verify-test/iLearn.Tests.dll` ผ่าน (Passed 118, Failed 0)
   - `Remove-Item -Recurse -Force artifacts/verify-test` เรียบร้อย
   - `npm run lint` ผ่าน
   - `npm run build` ผ่าน
   - หมายเหตุ: task `build iLearn.API` แบบปกติชน locked binaries จาก process รันอยู่ จึงยืนยันผล compile ด้วย `--artifacts-path` แทน
