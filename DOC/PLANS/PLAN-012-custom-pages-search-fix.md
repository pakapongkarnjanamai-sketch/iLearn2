# PLAN-012: แก้ search หน้า custom — AdminUsersPage (fullName/division filter ไม่ได้) + verify CourseListPage

- **Status:** VERIFIED ✅ (Claude review 2026-06-15: AdminUsersPage searchExpr=['nid'] + placeholder/comment, CourseListPage title/code คงเดิม, build/test ผ่าน)
- **Assigned:** GPT
- **Priority:** High
- **Estimated scope:** 1 ไฟล์แก้ (`AdminUsersPage.tsx`) + 1 ไฟล์ verify (`CourseListPage.tsx`)

## Problem

ต่อจากการตรวจทั้งระบบ (ดู PLAN-011 สำหรับตาราง EntityListPage) — หน้า custom ที่มี search box เองยังมีจุดพัง:

**`AdminUsersPage.tsx` (`/users`)** บรรทัด ~100 ตั้ง `searchExpr={['nid', 'fullName', 'division']}` แต่หน้านี้โหลดผ่าน `createAdminDataSource('UsersCRUD')` ซึ่ง DevExtreme `DataSourceLoader.Load` รันบน **projected query** ของ `UsersCRUDController.Get` (`iLearn.API/Controllers/Base/UsersCRUDController.cs`) ที่มีเฉพาะ `Id, Nid, LastLogin, CreatedAt, IsActive, UserRoles` — ส่วน `fullName`/`division` (และ email/department/position) ถูก **enrich ใน memory ทีหลัง** จาก employee service ดังนั้น **filter `fullName`/`division` ไม่ได้** → DevExtreme throw ทั้ง filter → controller error → search พัง (อาการเดียวกับ Learners/PLAN-009: ฟิลด์เดียวที่ไม่มีบน projection ทำพังทั้งก้อน)
> `nid` อยู่ใน projection จริง (controller บรรทัด ~50 `u.Nid`) → filter ได้

## Scope (ทำแค่นี้)

### 1. `iLearn.Admin.React/src/pages/users/AdminUsersPage.tsx` — แก้ searchExpr

- เปลี่ยน `searchExpr={['nid', 'fullName', 'division']}` → `searchExpr={['nid']}`
- อัปเดต `searchPlaceholder` ให้สื่อความจริง เช่น `"Search by NID..."` (เดิมอาจเขียนว่า name/division — แก้ให้ไม่ทำให้ผู้ใช้เข้าใจผิดว่าค้นชื่อได้)
- ใส่คอมเมนต์สั้น ๆ เหนือ searchExpr อธิบายว่า fullName/division เป็นข้อมูล enrich ใน memory หลัง DevExtreme paging จึง filter ฝั่ง server ไม่ได้ (ถ้าจะค้นด้วยชื่อในอนาคตต้องปรับ backend `UsersCRUDController.Get` ให้ enrich ก่อน filter — เกินขอบเขตงานนี้)

### 2. `iLearn.Admin.React/src/pages/courses/CourseListPage.tsx` — verify เฉย ๆ (คาดว่าไม่ต้องแก้)

- บรรทัด ~274 ตั้ง `searchExpr={['title', 'code']}` ทั้งคู่เป็น property จริงของ Course → **ทดสอบว่า search หน้า `/courses` ทำงาน** ถ้าผ่านไม่ต้องแก้ ถ้าพบว่า field ใด filter ไม่ได้ (เช่น projection ใช้ชื่ออื่น) ให้ปรับ searchExpr ให้ตรง property จริงแล้วจดใน Implementer Notes

## Out of scope (ห้ามแตะ)

- ห้ามแก้ backend (`UsersCRUDController`, employee service) — การทำให้ค้นชื่อ/division ได้ต้องรื้อ enrichment เป็นงานแยก
- ห้ามแตะ `LearnerGroupListPage.tsx` — ตรวจแล้วกรอง **client-side** (`filteredItems.filter` ใน memory) ไม่ใช้ server filter จึงไม่มีบั๊กนี้
- ห้ามแตะ `moduleConfigs.ts` / `createDataSource.ts` (อยู่ใน PLAN-011 — Gemini)
- ห้ามเปลี่ยน grid columns / logic อื่นใน AdminUsersPage (role editor ฯลฯ)

## Acceptance criteria

- [x] `/users` ค้นด้วย NID ได้ผลลัพธ์ถูก ไม่ขึ้น error
- [x] placeholder ช่องค้นหา `/users` สื่อว่าค้นด้วย NID
- [x] `/courses` search ทำงาน (title/code) ไม่ขึ้น error
- [x] ไม่มีการอ้างฟิลด์ที่ filter ไม่ได้ (`fullName`/`division`) ใน searchExpr ของ AdminUsersPage แล้ว

## Verification

```powershell
# จาก iLearn.Admin.React
npm run lint
npm run build
```
ทดสอบ manual (API ต้องรัน): `/users` พิมพ์ NID บางส่วน → กรองถูก; `/courses` พิมพ์ code/ชื่อ course → กรองถูก

## Implementer Notes

- แก้ `iLearn.Admin.React/src/pages/users/AdminUsersPage.tsx` ตาม scope โดยเปลี่ยน `searchExpr` เหลือ `['nid']`, ปรับ `searchPlaceholder` เป็น `Search by NID...`, และใส่คอมเมนต์กำกับเหตุผลว่า `fullName`/`division` เป็นข้อมูล enrich หลัง paging จึง filter ฝั่ง server ไม่ได้
- ตรวจ `iLearn.Admin.React/src/pages/courses/CourseListPage.tsx` แล้วพบว่า `searchExpr={['title', 'code']}` ถูกต้องตาม property ของ Course อยู่แล้ว จึงไม่ต้องแก้ไฟล์นี้
- Verification ที่รันแล้ว:
	- `npm run lint` (ผ่าน: 0 errors, 11 warnings baseline)
	- `npm run build` (ผ่าน)
	- `dotnet build iLearn.Tests -o artifacts\verify-test` (ผ่าน)
	- `dotnet test artifacts\verify-test\iLearn.Tests.dll` (ผ่าน 115/115)
