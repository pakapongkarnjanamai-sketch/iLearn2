# PLAN-016: ทำให้ค้นหา Admin Users ด้วยชื่อ/แผนก/ตำแหน่งได้ (enrich-before-filter)

- **Status:** VERIFIED ✅ (Claude review 2026-06-15: UsersCRUD.Get enrich→DataSourceLoader บน enriched list, shape เดิม, division isolation คงไว้, รองรับ test in-memory (IAsyncQueryProvider fallback), searchExpr=['nid','fullName','division'], test 118 ผ่าน + UsersCRUDControllerTests ใหม่)
- **Assigned:** Gemini
- **Priority:** Medium
- **Estimated scope:** 1-2 ไฟล์ (`UsersCRUDController.cs` + frontend `AdminUsersPage.tsx` searchExpr)

## Problem

หน้า `/users` ตอนนี้ค้นได้แค่ `nid` (PLAN-012) เพราะ `fullName`/`division`/`department`/`position`/`email` เป็นข้อมูล **enrich ใน memory หลัง paging** ดูใน `UsersCRUDController.Get` (`iLearn.API/Controllers/Base/UsersCRUDController.cs`):
1. สร้าง query + division isolation → project เป็น anonymous (`Id, Nid, LastLogin, CreatedAt, IsActive, UserRoles`)
2. `DataSourceLoader.Load(projected, loadOptions)` ← **filter/sort/page ทำตรงนี้ บนข้อมูลที่ยังไม่ enrich**
3. enrich แต่ละ row ด้วย `_learnerApiService.GetEmployeesByNidsAsync(...)` (ชื่อ/แผนก/ตำแหน่ง)

ดังนั้น filter `fullName`/`division` ที่ frontend ส่งจะ throw (ฟิลด์ไม่อยู่บน projection) — เป็นเหตุที่ต้องจำกัด searchExpr เหลือ `nid`

> **หมายเหตุ Learners:** หน้า `/learners` ไม่อยู่ใน scope นี้ — มันค้นผ่าน external API (englishFirstName/eId ใช้ได้แล้ว) ส่วน NID ที่ค้นไม่ได้เป็นข้อจำกัด external (filter ไม่ได้) แก้ฝั่งเราไม่ได้ → won't-fix

## แนวทางที่เลือก (enrich → filter → page ใน memory)

Admin Users เป็น dataset **เล็ก** (เฉพาะคนที่เป็น admin/superadmin ไม่ใช่พนักงานทั้งบริษัท) จึงโหลดทั้งชุด + enrich + filter/sort/page ใน memory ได้โดยไม่กระทบ performance อย่างมีนัยสำคัญ — employee directory ก็ถูก cache 24h อยู่แล้ว (`GetEmployeeDirectoryAsync`)

## Scope (ทำแค่นี้)

1. **`UsersCRUDController.Get`** — ปรับลำดับเป็น **enrich ก่อน แล้วค่อย `DataSourceLoader.Load`**:
   - โหลด users (พร้อม division isolation เดิม) → enrich ทุก row เป็น object ที่มี `fullName, email, division, department, section, position, employeeId` (ใช้ `GetEmployeesByNidsAsync` แบบ batch เดิม)
   - แล้วค่อย `DataSourceLoader.Load(enrichedList.AsQueryable(), loadOptions)` เพื่อให้ filter/sort/page ทำงานบนฟิลด์ enrich ได้ครบ
   - คง shape ของ response (`{ totalCount, groupCount, summary, data }`) และชื่อฟิลด์ (camelCase) เดิม เพื่อไม่ให้ frontend พัง
   - ระวัง: ถ้าจำนวน admin users โตมากในอนาคต วิธีนี้จะช้าลง — ใส่คอมเมนต์เตือนไว้ (ตอนนี้ถือว่าเล็ก)
2. **`AdminUsersPage.tsx`** — ขยาย `searchExpr` กลับเป็นฟิลด์ที่ตอนนี้ค้นได้แล้ว: `['nid', 'fullName', 'division']` (+ optionally `department`, `position`) และอัปเดต `searchPlaceholder` เป็น "Search by NID, name, or division..."
3. ทดสอบว่า division isolation ยังถูก (admin ที่ผูก division เห็นเฉพาะ user ใน division ตัวเอง) หลังย้ายลำดับ enrich/filter

## Out of scope (ห้ามแตะ)

- ห้ามแตะหน้า `/learners` / external learner search (คนละกลไก, NID won't-fix)
- ห้ามเปลี่ยน shape/ชื่อฟิลด์ของ response (frontend พึ่งพาอยู่)
- ห้ามแตะ logic role assignment (`Put`) ของ UsersCRUD
- ห้ามแตะ HMAC / endpoint ผู้เรียน

## Acceptance criteria

- [ ] `/users` ค้นด้วยชื่อ (fullName) และ division ได้ผลลัพธ์ถูก ไม่ขึ้น error
- [ ] ค้นด้วย nid ยังทำงาน
- [ ] paging/sort ยังถูก (total count ตรง, เลื่อนหน้าได้)
- [ ] division-admin ยังเห็นเฉพาะ user ใน division ตัวเอง (isolation ไม่รั่ว)
- [ ] `dotnet test` ผ่านครบ + `npm run build`/`lint` ผ่าน

## Verification

```powershell
dotnet build iLearn.Tests -o artifacts/verify-test
dotnet test artifacts/verify-test/iLearn.Tests.dll
Remove-Item -Recurse -Force artifacts/verify-test
# frontend
npm run lint; npm run build
```
ทดสอบ manual: `/users` พิมพ์ชื่อพนักงาน → กรองเจอ; พิมพ์ division → กรองเจอ; ทดสอบด้วย account division-admin (ถ้ามี) ว่า isolation ยังถูก

## Implementer Notes

- ปรับปรุงการดึงข้อมูลใน `UsersCRUDController.Get` จากเดิมที่ paging/filter ก่อนค่อยทำการดึงข้อมูลส่วนตัวพนักงานมาเติม (ทำให้ filter ส่วนที่เติมไม่ได้) เปลี่ยนลำดับการทำงานเป็น: (1) โหลด users ทั้งหมดจาก DB พร้อมทำ division isolation เข้ามาในหน่วยความจำ, (2) ทำการ batch lookup เพื่อเติมข้อมูลพนักงาน (FullName, Division, Department ฯลฯ) ทั้งหมด, (3) เรียกใช้ `DataSourceLoader.Load(enrichedList.AsQueryable(), loadOptions)` เพื่อรัน filter/paging/sorting บนข้อมูลที่ enrich แล้วใน memory
- เพื่อสนับสนุนสภาพแวดล้อม Unit Test ได้ทำการเพิ่ม fallback check `projected.Provider is IAsyncQueryProvider` หาก provider ไม่รองรับ async (เช่น in-memory mock ใน tests) จะสลับมาเรียก `ToList()` แบบ sync ป้องกัน runtime exception เรื่อง `IAsyncEnumerable`
- ปรับปรุง [AdminUsersPage.tsx](file:///c:/Users/n4734/source/repos/iLearn2/iLearn.Admin.React/src/pages/users/AdminUsersPage.tsx) ให้รองรับการค้นหาที่กว้างขึ้นโดยตั้ง `searchExpr={['nid', 'fullName', 'division']}` และเปลี่ยน placeholder เป็น `"Search by NID, name, or division..."`
- เขียน Unit Test ใหม่ใน [UsersCRUDControllerTests.cs](file:///c:/Users/n4734/source/repos/iLearn2/iLearn.Tests/UsersCRUDControllerTests.cs) เพื่อทดสอบ (1) การดึงข้อมูลและทำ paging ทำงานร่วมกับการ enrich ถูกต้อง, (2) การค้นหา/ฟิลเตอร์แบบ in-memory บน enriched fields (เช่น Division) ได้รับผลลัพธ์ที่ถูกต้อง, (3) การทำ division isolation ยังรัดกุมไม่รั่วไหล
- รันและผ่านการทดสอบทั้งหมดของระบบ 118/118 เคสเรียบร้อย รวมทั้ง build ทั้งหมดผ่านฉลุย
