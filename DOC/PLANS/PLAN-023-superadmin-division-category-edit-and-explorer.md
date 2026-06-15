# PLAN-023: SuperAdmin เลือก Division ได้ตอน Edit Category + ตอนสร้าง folder ในหน้า Explorer

- **Status:** VERIFIED ✅ (Claude review 2026-06-15: commit 3373581 — UpdateLearnerGroupCategoryDto+DivisionId, UpdateAsync parent-inherit + SuperAdmin-only + empty-check guard (กันเปลี่ยน division ของ category ที่มีลูก/group), frontend edit selector + explorer folder selector (useSession), dotnet test 118 + build/lint ผ่าน)
- **Assigned:** Gemini
- **Priority:** Medium
- **Estimated scope:** backend 1 DTO + 1 service (UpdateAsync) + frontend 2 ไฟล์ (`LearnerGroupCategoryEditorPage.tsx`, `LearnerGroupListPage.tsx`)
- **ขึ้นกับ:** PLAN-022 (create-side ทำแล้ว — DivisionId อยู่ใน `CreateLearnerGroupCategoryDto` + `LearnerGroupCategoryService.CreateAsync` ใช้ `IsSuperAdmin ? dto.DivisionId : currentUser.DivisionId` + parent inheritance แล้ว)

## Problem

ผู้ใช้ต้องการ: ถ้าเป็น **SuperAdmin** ตอน **New หรือ Edit Category** ให้เลือก Division ได้ ทั้ง 2 หน้า:
- `/master-data/learner-group-categories` (`LearnerGroupCategoryEditorPage`)
- `/learner-groups` (explorer `LearnerGroupListPage`)

สถานะหลัง PLAN-022:
- ✅ **New** category ที่ `/master-data/...` มี division selector แล้ว (SuperAdmin)
- ❌ **Edit** category ที่ `/master-data/...` ยังไม่มี — เพราะ PLAN-022 กัน `UpdateAsync` ออก (`UpdateLearnerGroupCategoryDto` ไม่มี `DivisionId`)
- ❌ **New folder** ในหน้า explorer `/learner-groups` ยังไม่มี — `handleCreateFolder` (`LearnerGroupListPage.tsx:382-418`) POST `LearnerGroupCategories` โดย**ไม่ส่ง divisionId** (backend create รับได้แล้วจาก PLAN-022 แต่ frontend ไม่ส่ง + ไม่มี selector)

> หมายเหตุ: หน้า explorer มีแค่ **สร้าง** folder (modal) + **ลบ** (`handleDeleteFolder`) — **ไม่มี edit/rename category** ในหน้านี้ ดังนั้น "Edit Category" ใช้กับหน้า `/master-data/...` เท่านั้น

## Scope (ทำแค่นี้)

### Backend (เปิดให้แก้ division ตอน edit)
1. **`UpdateLearnerGroupCategoryDto`** (`iLearn.Application/DTOs/LearnerGroupCategoryDto.cs`) — เพิ่ม `public int? DivisionId { get; set; }`
2. **`LearnerGroupCategoryService.UpdateAsync`** — ใช้กฎเดียวกับ CreateAsync (PLAN-022):
   - `DivisionId = _currentUser.IsSuperAdmin ? dto.DivisionId : category.DivisionId` (division-admin **ห้ามเปลี่ยน** — คงค่าเดิม/ของตัวเอง กัน escalation)
   - ถ้า category มี `ParentId` ให้ division สอดคล้องกับ parent (inherit จาก parent เหมือน create) — ไม่ให้ tree ข้าม division
   - **ระวัง:** ถ้าเปลี่ยน division ของ category ที่มีลูก/มี group อยู่ข้างใน — พิจารณาว่าจะ cascade ลง descendant หรือ block ถ้าไม่ว่าง; ถ้า logic ซับซ้อนเกิน ให้**อนุญาตเปลี่ยน division เฉพาะ category ที่ยังไม่มีลูกและไม่มี group** แล้วจดเงื่อนไขใน Implementer Notes (เลือกทางที่ปลอดภัยต่อ data isolation)
   - คง ownership check เดิม (division-admin แก้ของ division อื่นไม่ได้)

### Frontend
3. **`LearnerGroupCategoryEditorPage.tsx`** — แสดง Division selector ในโหมด **edit** ด้วย (ตอนนี้มีเฉพาะ create จาก PLAN-022):
   - เฉพาะ SuperAdmin, pre-fill ด้วย division ปัจจุบันของ category
   - ส่ง `divisionId` ไปกับ payload ตอน PUT
   - คงกฎ inherit-from-parent ที่ PLAN-022 ทำไว้ (ถ้าเลือก parent → division ตาม parent, disable selector + ข้อความเตือน)
4. **`LearnerGroupListPage.tsx` (explorer) — modal "New Folder"** (`isNewFolderOpen`, `handleCreateFolder`):
   - เฉพาะ SuperAdmin **และเฉพาะตอนสร้างที่ root** (`currentCategoryId === 0`, parentId = null) → แสดง Division selector ในฟอร์ม modal
   - ถ้าสร้างใน folder (parentId > 0) → ไม่ต้องเลือก (inherit จาก parent) ซ่อน selector
   - ส่ง `divisionId` เพิ่มใน body ของ `POST LearnerGroupCategories` (บรรทัด ~396) เฉพาะกรณี SuperAdmin+root
   - ตรวจบทบาท SuperAdmin: ใช้ `useSession` ตัวเดียวกับที่ PLAN-022 ใช้ใน `LearnerGroupCategoryEditorPage`

## Out of scope (ห้ามแตะ)

- ห้ามแตะ group create/edit (PLAN-022 ทำ group แล้ว — งานนี้เฉพาะ **Category**)
- ห้ามเพิ่ม edit/rename category ในหน้า explorer (ไม่มีของเดิม ไม่ต้องสร้างใหม่)
- ห้ามเปลี่ยนพฤติกรรม division-admin (ห้ามให้เปลี่ยน division)
- ห้ามแตะเรื่อง "global category มองไม่เห็นโดย division-admin" (คนละประเด็น)

## Acceptance criteria

- [x] `/master-data/learner-group-categories` **Edit** category: SuperAdmin เลือก/เปลี่ยน division ได้, pre-fill ค่าเดิม; division-admin ไม่เห็น selector + เปลี่ยนไม่ได้
- [x] `/learner-groups` explorer **New Folder** ที่ root: SuperAdmin เลือก division ได้ → folder ถูก tag division นั้น (division-admin ของแผนกนั้นเห็น); สร้างใน sub-folder = inherit parent (ไม่มี selector)
- [x] division-admin: ทุกที่ไม่เห็น selector, ส่ง divisionId ปลอมมา backend เมิน
- [x] category ที่มี parent ไม่ข้าม division กับ parent (ทั้ง create/edit)
- [x] `dotnet test` ผ่าน + `npm run build`/`lint` ผ่าน

## Verification

```powershell
dotnet build iLearn.Tests -o artifacts/verify-test
dotnet test artifacts/verify-test/iLearn.Tests.dll
Remove-Item -Recurse -Force artifacts/verify-test
npm run lint; npm run build
```
ทดสอบ manual: SuperAdmin — edit category เปลี่ยน division ที่หน้า master-data; สร้าง folder ระบุ division ที่ explorer root → login division-admin แผนกนั้นเห็น folder/category นั้น

## Implementer Notes

- **Backend**:
  - Added `DivisionId` to `UpdateLearnerGroupCategoryDto`.
  - In `LearnerGroupCategoryService.UpdateAsync`, implemented target `DivisionId` evaluation: evaluates to parent category's `DivisionId` if `parentId` is specified, otherwise to the SuperAdmin-selected `dto.DivisionId` (retains the existing division for division-admins to prevent escalation).
  - **Division Update Safety Check**: Implemented logic to check and reject division updates if the category is not empty (i.e. already has sub-categories or learner groups inside). This prevents cross-division tree inconsistencies without complex cascading.
- **Frontend**:
  - Updated `LearnerGroupCategoryEditorPage.tsx` to load divisions for SuperAdmins during edit mode as well as create mode, show the dropdown during edit mode, pre-fill it with the category's current division, and pass `divisionId` in the PUT request payload.
  - Updated `LearnerGroupListPage.tsx` to use `useSession` hook. Added division dropdown selector to the "Create Folder" modal when the user is a SuperAdmin and creating a folder at the explorer root (`currentCategoryId === 0`). Passed `divisionId` to the POST payload.
  - Cleaned up the modal closing logic by resetting division states.
  - Successfully verified linting, production building, and full unit test execution.
