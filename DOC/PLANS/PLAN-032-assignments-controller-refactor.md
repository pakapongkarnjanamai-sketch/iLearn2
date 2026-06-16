# PLAN-032: Refactor AssignmentsController → service + anonymous→DTO (ตาม API style guide)

- **Status:** READY
- **Assigned:** GPT (GPT-5.3 Codex)
- **Priority:** Medium
- **Estimated scope:** `AssignmentsController.cs` (1316 บรรทัด) + service ใหม่/ขยาย + DTO — backend ล้วน
- **มาตรฐานเป้าหมาย:** `DOC/api_style_guide.md` (§3 response, §5 DTO, §6 controller thinness)
- **ต่อจาก:** PLAN-017 (Enrollments pilot), PLAN-027 (Courses pilot) — controller ใหญ่ตัวถัดไป

## Problem

`iLearn.API/Controllers/AssignmentsController.cs` = **1316 บรรทัด** (ใหญ่สุดในระบบ):
- inject `IGenericRepository<T>` ดิบ **4 ตัว** (`Assignment`, `EnrollmentAssignment`, `Course`, `Enrollment`) → business logic/query อยู่ใน controller จำนวนมาก (มี service บางส่วนแล้ว: `AssignmentBatchService`, `AssignmentDashboardService`, `CourseAssignmentService` แต่ยังเหลือ logic inline เยอะ)
- คืน **anonymous object 9 จุด** (`Ok(new { ... })`) — ผิด style guide §3 (OpenAPI gen ไม่ได้, React เดา shape)

## Scope (ทำแค่นี้ — pure refactor, ไม่เปลี่ยน shape/พฤติกรรม)

### A. ดึง logic inline ลง service
1. สร้าง `IAssignmentService` + `AssignmentService` (`iLearn.Application/`) **หรือ** ขยาย service ที่มีอยู่ ตามที่เหมาะ — ย้าย business logic/query ที่ใช้ raw repo ใน controller ลงไป (controller เหลือ orchestration + auth + map ตาม guide §6)
2. register DI; คง `[Authorize]` + division isolation (`_currentUser`) **ที่ controller** (guide §2/§7)
3. คง endpoint ฝั่ง learner/HMAC (ถ้ามี) ไว้ที่ controller — อย่าย้าย auth/HttpContext ลง service

### B. anonymous → DTO (9 จุด)
4. grep `Ok(new {` ใน AssignmentsController → แปลงทุกจุดเป็น **`ApiResponse<T>`** (`iLearn.Domain/Common/ApiResponse.cs`) หรือ DTO record ใน `iLearn.Application/DTOs/`
5. **shape ที่ React อ่านต้องไม่เปลี่ยน** — grep endpoint assignments ใน `iLearn.Admin.React/src` (เช่น `Assignments`, `/bulk`, `/gantt`, `/{id}`, `/report`, `/history`, `/conflict`) เทียบ field ให้ตรงเป๊ะก่อนแก้ (API Contract Sync) + เพิ่มคอมเมนต์ `// Mirrors <Dto>` ฝั่ง React type
6. ⚠️ EF: ถ้า project เข้า DTO record ใน `GroupBy/.Select` บน IQueryable → ต้อง materialize (`ToListAsync`) ก่อน map (guide §5 — บทเรียน Dashboard)

### C. pure refactor — พฤติกรรม/shape/status เดิมทั้งหมด (`dotnet test` เป็นตาข่าย)

> **ถ้า controller ใหญ่เกินทำจบรอบเดียวอย่างปลอดภัย** ให้ทำ **B (anonymous→DTO) ให้ครบก่อน** (contract-preserving ชัด) แล้วทำ A เท่าที่ปลอดภัย จดส่วนที่เหลือใน Implementer Notes เป็น follow-up — อย่าฝืน refactor ก้อนใหญ่จน regression

## Out of scope (ห้ามแตะ)

- ห้ามเปลี่ยน endpoint path / shape / status / พฤติกรรม (pure refactor)
- ห้าม refactor controller อื่น (ContentItems ฯลฯ = increment ต่อไป แยกแผน)
- ห้ามแตะ division isolation จนผลเปลี่ยน
- ห้ามแตะ frontend assignments pages นอกจากเพิ่มคอมเมนต์ Mirrors (ไม่เปลี่ยน type shape)
- ห้ามแตะ `AssignmentBatch/Dashboard/CourseAssignment` service ที่มีอยู่จนพฤติกรรมเปลี่ยน (เรียกใช้ได้)

## Acceptance criteria

- [ ] `AssignmentsController` ไม่มี `Ok(new {` เหลือ (= 0) — ใช้ ApiResponse<T>/DTO แทน
- [ ] shape ที่ React ได้รับไม่เปลี่ยน — หน้า `/assignments` (list/gantt/bulk) + assignment detail/report ยังทำงานครบ
- [ ] logic inline ที่ใช้ raw repo ย้ายลง service (controller สั้นลง/บางลง) — ถ้าทำบางส่วน จดที่เหลือใน Notes
- [ ] division isolation ของ assignments ยังถูก
- [ ] `dotnet test` ผ่านครบ (118) + `npm run build`/`lint` (0/0) ผ่าน

## Verification

```powershell
dotnet build iLearn.Tests -o artifacts/verify-test
dotnet test artifacts/verify-test/iLearn.Tests.dll
Remove-Item -Recurse -Force artifacts/verify-test
npm run lint; npm run build
```
ทดสอบ manual: `/assignments` (list + chip + scroll), `/assignments/gantt`, `/assignments/bulk`, assignment detail (แท็บ/modal) + report — ดูว่าโหลด/แสดง/บันทึกครบเหมือนเดิม

## Implementer Notes

(เติมหลังทำเสร็จ — service ที่สร้าง/ขยาย + 9 endpoint ที่แปลงเป็น typed + ส่วนที่เหลือเป็น follow-up + ผล grep contract sync)
