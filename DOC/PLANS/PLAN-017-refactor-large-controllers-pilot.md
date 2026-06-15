# PLAN-017: ลดขนาด controller ใหญ่ → ดึง logic ลง Application service (pilot: Enrollments)

- **Status:** VERIFIED ✅ (Claude review 2026-06-15: EnrollmentsController 624→491, สร้าง IEnrollmentService/EnrollmentService (Reset/GetById/UpdateCompletion/BulkAssign) + register DI, controller delegate, learner anonymous+HMAC คงใน controller, pure refactor test 118 ผ่าน)
- **Assigned:** Gemini
- **Priority:** Low
- **Estimated scope:** pilot 1 controller (`EnrollmentsController` → `EnrollmentService` ใหม่) — controller อื่นเป็นแผนต่อ ๆ ไป

## Problem

Controller หลายตัวมี business logic ปนอยู่จำนวนมาก เทส unit ยาก/อ่านยาก:
`AssignmentsController` 1316, `ContentItemsController` 1179, `DashboardController` 766, `EnrollmentsController` 624, `CoursesController` 622 บรรทัด

นี่เป็นหนี้ทางเทคนิคที่ควรทยอยลด **ทีละ controller** (ไม่ทำรวดเดียว เสี่ยง) — แผนนี้เป็น **pilot ตัวแรก + วางมาตรฐานวิธีทำ** ให้ controller ที่เหลือทำตามในแผนถัด ๆ ไป

## วิธีมาตรฐาน (ใช้ซ้ำกับ controller อื่นในอนาคต)

1. สร้าง `I<Name>Service` ใน `iLearn.Application/Interfaces/Services/` + implementation ใน `iLearn.Application/Services/`
2. ย้าย business logic (query/คำนวณ/กฎ) จาก action ลง service — controller เหลือแค่: รับ input → เรียก service → คืนผล/แปลง error
3. register service ใน `iLearn.Application/DependencyInjection.cs`
4. **พฤติกรรมต้องเหมือนเดิมเป๊ะ** (pure refactor) — response shape, status, side-effect เดิมทั้งหมด
5. คง `[AllowAnonymous]` + HMAC + `[Authorize]` ไว้ที่ controller (อย่าย้าย auth ลง service)

## Scope (ทำแค่นี้ — pilot)

**`EnrollmentsController` (624 บรรทัด) — ดึง logic ฝั่ง admin ลง `EnrollmentService` ใหม่**
- เลือกเฉพาะส่วนที่ isolate ชัด: admin operations (เช่น reset enrollments, ledger query, lifecycle inspection — ดู action ที่ `[Authorize(Policy="AdminOnly")]`)
- **คงไว้ที่ controller:** endpoint ฝั่งผู้เรียน `[AllowAnonymous]` + การ resolve HMAC learner identity (อย่าแตะ flow นี้ — ย้ายแค่ logic admin ที่ไม่พึ่ง HttpContext)
- ถ้าส่วนใดผูกกับ `HttpContext`/auth จนแยกยาก ให้คงไว้ที่ controller แล้วจดใน Implementer Notes (อย่าฝืน)

> หมายเหตุ: ไม่มี `EnrollmentService` เดิมอยู่ (ต่างจาก Courses/Assignments ที่มี service บางส่วนแล้ว) — pilot นี้จึงสร้างใหม่

## Out of scope (ห้ามแตะ)

- ห้ามเปลี่ยนพฤติกรรม/response shape/status code ใด ๆ (pure refactor เท่านั้น)
- ห้าม refactor controller อื่น (Assignments/ContentItems/Dashboard/Courses) — เป็นแผนแยก
- ห้ามแตะ HMAC/learner endpoint flow
- ห้ามเปลี่ยน DB/entity/migration

## Acceptance criteria

- [x] มี `IEnrollmentService` + implementation + register ใน DI
- [x] `EnrollmentsController` สั้นลงอย่างมีนัยสำคัญ (logic admin ย้ายลง service) controller เหลือ orchestration
- [x] พฤติกรรมเดิมทุกอย่างไม่เปลี่ยน — `dotnet test` ผ่านครบเท่าเดิม (เป็นหลักฐานหลักว่าไม่ regression)
- [x] endpoint ผู้เรียน (anonymous + HMAC) ยังทำงานเหมือนเดิม

## Verification

```powershell
dotnet build iLearn.Tests -o artifacts/verify-test
dotnet test artifacts/verify-test/iLearn.Tests.dll
Remove-Item -Recurse -Force artifacts/verify-test
```
ทดสอบ manual (ถ้ารัน API ได้): `/enrollments` โหลด/รีเซ็ตปกติ; SCORM player commit progress (learner) ยังทำงาน

## Implementer Notes

- สร้าง `BulkAssignResultDto` เพื่อส่งต่อข้อมูลผลลัพธ์การ assign รวมถึงข้อมูล conflicts จาก Service กลับมายัง API Controller ในรูปแบบที่คง behavior และ API payload shape ดั่งเดิม
- ย้าย logic ของ Admin-only endpoints (`ResetStatus`, `GetById`, `UpdateCompletion`, `BulkAssign`) และ private helper methods ต่างๆ ที่เกี่ยวโยงเฉพาะกับฝั่ง Admin ลงไปอยู่บน `EnrollmentService` ทั้งหมด
- คง endpoints ผู้เรียน (`[AllowAnonymous]`) และ logic ที่ผูกกับ user identity resolution, HttpContext (เช่น `TryGetTrustedLearnerLearnerCode`) และ helper functions สำหรับ learner scheduler (`GetActiveLinks`, `GetEffectiveSchedule`, `EnrollmentSchedule`) ไว้ที่ controller ดั่งเดิมตาม Scope ของแผน
- ลงทะเบียน `IEnrollmentService` ใน `iLearn.Application/DependencyInjection.cs`
- ปรับปรุง unit test `EnrollmentsPlayerInfoTests.cs` ในส่วน `CreateController` เพื่อรองรับ dynamic injection/constructor parameters list ของ `EnrollmentsController` ที่ถูกปรับปรุงให้สั้นลง
- ทำการ clean build และ run unit tests backend ผ่านสำเร็จครบ 118/118 tests 

