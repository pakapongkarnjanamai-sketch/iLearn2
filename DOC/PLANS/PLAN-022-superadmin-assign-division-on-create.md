# PLAN-022: ให้ SuperAdmin ระบุ Division ตอนสร้าง Learner Group / Learner Group Category ได้

- **Status:** DONE
- **Assigned:** Gemini
- **Priority:** Medium
- **Estimated scope:** backend 2 service + 2 DTO + 2 controller (เล็กน้อย) + frontend 2 editor page

## Problem

`CreateAsync` ของทั้ง LearnerGroup และ LearnerGroupCategory กำหนด `DivisionId = _currentUser.DivisionId` เสมอ (auto จากผู้สร้าง):
- `iLearn.Application/Services/LearnerGroupService.cs:214`
- `iLearn.Application/Services/LearnerGroupCategoryService.cs:128`

ผลคือ **SuperAdmin (DivisionId = null) สร้าง group/category ได้แค่แบบ global** (division = null) ซึ่ง division-admin มองไม่เห็น → SuperAdmin **สร้างกลุ่ม/หมวดให้ "แผนก X โดยเฉพาะ" ไม่ได้** (ดู `DOC/division_isolation_analysis.md` §5.3)

เป้าหมาย: ให้ **SuperAdmin เลือก division** ตอนสร้างได้; **division-admin คงพฤติกรรมเดิม** (auto = division ตัวเอง, ห้าม override — กัน privilege escalation)

## Scope (ทำแค่นี้)

### Backend
1. **DTO** — เพิ่ม `public int? DivisionId { get; set; }` ใน:
   - `CreateLearnerGroupDto` (`iLearn.Application/DTOs/LearnerGroupDto.cs`)
   - `CreateLearnerGroupCategoryDto` (`iLearn.Application/DTOs/LearnerGroupCategoryDto.cs`)
2. **Service** — แก้ `CreateAsync` ทั้งสอง ให้กำหนด DivisionId ตามกฎ:
   ```csharp
   // SuperAdmin (DivisionId == null) → ใช้ค่าที่เลือกจาก dto; ถ้าไม่เลือกก็ null (global)
   // Division-admin → บังคับเป็น division ตัวเองเสมอ (เมิน dto.DivisionId กัน escalation)
   DivisionId = _currentUser.IsSuperAdmin ? dto.DivisionId : _currentUser.DivisionId;
   ```
   (`ICurrentUserService.IsSuperAdmin` มีอยู่แล้ว)
   - **LearnerGroupCategory เพิ่มเติม:** ถ้ามี `ParentId` ให้ DivisionId สอดคล้องกับ parent (category ลูกควรอยู่ division เดียวกับ parent) — ถ้า SuperAdmin เลือก divisionId ขัดกับ parent.DivisionId ให้ยึด parent (หรือ reject + จดใน Notes) เพื่อไม่ให้ tree ข้าม division
3. **Controller** — ตรวจว่า create endpoint ส่ง dto เข้า service ครบ (ไม่ต้องเพิ่ม logic auth — service จัดการ IsSuperAdmin แล้ว); คง `[Authorize]` เดิม

### Frontend
4. **`LearnerGroupEditorPage.tsx`** + **`LearnerGroupCategoryEditorPage.tsx`** — แสดง **Division selector เฉพาะเมื่อผู้ใช้เป็น SuperAdmin**:
   - โหลด divisions (`admin/DivisionsCRUD/Get`)
   - ถ้า SuperAdmin: แสดง dropdown เลือก division (optional — "Global / ไม่ระบุ" = null), ส่ง `divisionId` ไปกับ create payload
   - ถ้าไม่ใช่ SuperAdmin: ซ่อน selector, ไม่ส่ง divisionId (backend auto เป็น division ตัวเอง)
   - หาวิธีรู้บทบาท: ใช้ context/hook สิทธิ์ที่มีอยู่ (ดูที่ `RequireRole`/auth context ฝั่ง React ใช้ตรวจ superAdmin อยู่แล้ว — reuse ตัวเดียวกัน)
   - `LearnerGroupEditorPage` มี form field `division` อยู่แล้ว (บรรทัด ~268) — wire ให้ส่งจริงเฉพาะ SuperAdmin

## Out of scope (ห้ามแตะ)

- ห้ามเปลี่ยนพฤติกรรม division-admin (ยัง auto division ตัวเอง ห้าม override)
- ห้ามแตะ list filter / ownership check เดิม (get/update/delete)
- ห้ามแก้เรื่อง "global category มองไม่เห็นโดย division-admin" (เป็นคนละประเด็น §5.3 ย่อย — ไม่อยู่ใน scope นี้)
- ห้ามแตะ UpdateAsync (เปลี่ยน division หลังสร้าง = อีกเรื่อง ถ้าต้องการทำแผนแยก)

## Acceptance criteria

- [x] SuperAdmin: ตอนสร้าง group/category เลือก division ได้ → record ถูก tag division นั้น → division-admin ของแผนกนั้นเห็น
- [x] SuperAdmin ไม่เลือก division → เป็น global (null) เหมือนเดิม
- [x] Division-admin: ไม่เห็น selector, สร้างแล้ว division = ตัวเองเสมอ (ส่ง divisionId ปลอมมาก็ถูกเมิน)
- [x] category ลูกไม่ข้าม division กับ parent
- [x] `dotnet test` ผ่าน + `npm run build`/`lint` ผ่าน

## Verification

```powershell
dotnet build iLearn.Tests -o artifacts/verify-test
dotnet test artifacts/verify-test/iLearn.Tests.dll
Remove-Item -Recurse -Force artifacts/verify-test
npm run lint; npm run build
```
ทดสอบ manual: SuperAdmin สร้าง group ระบุ division A → login division-admin A เห็น; ส่ง divisionId ปลอมจาก division-admin (ผ่าน devtools) → backend เมิน

## Implementer Notes

- Backend changes for role-based `DivisionId` auto-assignment/inheritance have been confirmed in `LearnerGroupService.cs` and `LearnerGroupCategoryService.cs`.
- Frontend `LearnerGroupEditorPage.tsx` was already wired with division selector for SuperAdmin.
- Completed frontend implementation in `LearnerGroupCategoryEditorPage.tsx` with:
  - `useSession` check to conditionally render a Division selector dropdown only for SuperAdmin on creation.
  - Automatic `divisionId` inheritance from the parent category when a parent is selected, disabling direct division selection with a warning message.
  - Showing effective division in the review step for SuperAdmins.
  - `npm run lint` and `npm run build` ran and completed successfully.
  - All 118 backend tests build and pass successfully.
  - Fixed an unused import regression (`Sliders` in `CourseListPage.tsx`) to restore the React production build.
