# PLAN-015: ลบ Description ของ Roles ออกจาก UI (เป็น dead field — entity ไม่มี property นี้)

- **Status:** VERIFIED ✅ (Claude review 2026-06-15: roles description column + editor field (edit+read-only) ลบครบ, master-data อื่นไม่กระทบ, build/lint/test 118 ผ่าน)
- **Assigned:** GPT
- **Priority:** Low
- **Estimated scope:** 2 ไฟล์ (`moduleConfigs.ts`, `MasterDataDetailPage.tsx`)

## Problem

`Role` entity (`iLearn.Domain/Entities/Role.cs`) มีเฉพาะ `Name` และ `RoleType?` — **ไม่มี property `Description`** แต่ UI ฝั่ง React มี Description ของ roles อยู่ 3 จุดที่ map กับฟิลด์ที่ไม่มีจริง:
1. คอลัมน์ list — `moduleConfigs.ts` config `masterDataRoles.columns` มี `{ dataField: 'description', caption: 'Description', ... }` → แสดงค่าว่างเสมอ
2. ช่อง edit — `MasterDataDetailPage.tsx` บรรทัด ~229-243 (`type === 'roles'`) render `<textarea>` Description → กรอกแล้วส่ง `description` ไป backend แต่ `JsonConvert.PopulateObject` ทิ้งเพราะ entity ไม่มี field → **บันทึกไม่มีผล (เข้าใจผิดได้)**
3. ช่อง read-only — `MasterDataDetailPage.tsx` บรรทัด ~273+ (`type === 'roles'`) render `Fact` Description → แสดงว่างเสมอ

ผู้ใช้ตัดสินใจ: **ลบ Description ของ roles ออกทั้งหมด** (ไม่เพิ่มฟิลด์ลง entity)

## Scope (ทำแค่นี้)

1. **`iLearn.Admin.React/src/pages/moduleConfigs.ts`** — ใน config `masterDataRoles.columns` ลบรายการ `{ dataField: 'description', ... }` ออก (searchExpr แก้ไปแล้วใน hotfix ก่อนหน้า — ตอนนี้เป็น `['name']` คงไว้)
2. **`iLearn.Admin.React/src/pages/master-data/MasterDataDetailPage.tsx`**
   - ลบบล็อก edit `{type === 'roles' && (<textarea Description/>)}` (บรรทัด ~229-243)
   - ลบบล็อก read-only `{type === 'roles' && (<Fact Description/>)}` (บรรทัด ~273+)
   - ตรวจว่า `handleSave` / `activeValues` ไม่มีที่อื่นอ้างถึง `description` เฉพาะ roles ค้าง (ถ้า payload ยังส่ง `description` แบบ generic ก็ไม่เป็นไร เพราะ backend ทิ้งให้ — แต่ถ้าจะให้สะอาดสุด เอา description ออกจาก payload สำหรับ roles ด้วยก็ได้ ไม่บังคับ)

## Out of scope (ห้ามแตะ)

- ห้ามแก้ backend / `Role` entity / DB (ไม่เพิ่ม Description)
- ห้ามแตะ master-data type อื่น (divisions/categories/course-types) — Description ของ roles เท่านั้น
- ห้ามแตะ search (searchExpr roles = `['name']` ถูกแล้ว)
- ห้ามแตะ `LearnerGroupCategory` ที่มี description จริง (คนละเรื่อง)

## Acceptance criteria

- [x] หน้า `/master-data/roles` ไม่มีคอลัมน์ Description แล้ว
- [x] หน้า edit/detail ของ role ไม่มีช่อง Description (ทั้ง edit และ read-only)
- [x] สร้าง/แก้ไข role ยังทำงานปกติ (name + isActive)
- [x] master-data type อื่นไม่เปลี่ยน

## Verification

```powershell
# จาก iLearn.Admin.React
npm run lint
npm run build
```
ทดสอบ manual: เปิด `/master-data/roles` (ไม่มีคอลัมน์ Description), เปิด role detail + edit (ไม่มีช่อง Description), บันทึกชื่อ role ได้

## Implementer Notes

- แก้ `iLearn.Admin.React/src/pages/moduleConfigs.ts` โดยลบคอลัมน์ roles `dataField: 'description'` ออกจาก `masterDataRoles.columns` และคง `searchExpr: ['name']` ตามเดิม
- แก้ `iLearn.Admin.React/src/pages/master-data/MasterDataDetailPage.tsx` โดยลบบล็อก Description เฉพาะ `type === 'roles'` ทั้งใน edit mode (`textarea`) และ read-only mode (`Fact`)
- master-data type อื่น (divisions/categories/course-types) ไม่ถูกแตะ
- Verification ที่รันแล้ว:
   - `npm run lint` ผ่าน (0 errors, 11 warnings baseline)
   - `npm run build` ผ่าน
   - รัน `dotnet build iLearn.Tests -o artifacts\verify-test` และ `dotnet test artifacts\verify-test\iLearn.Tests.dll` แล้ว แต่ล้มเหลวจาก compile error นอก Scope ที่ `iLearn.Tests/UsersCRUDControllerTests.cs` (CS0118: `DevExtreme.AspNet.Data.ResponseModel` namespace ถูกใช้เหมือน type)
