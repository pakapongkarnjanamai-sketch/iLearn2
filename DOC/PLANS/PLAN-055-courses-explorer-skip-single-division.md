# PLAN-055: Courses Explorer — ข้าม Division level เมื่อผู้ใช้เห็นแค่ 1 Division

- **Status:** VERIFIED
- **Assigned:** Gemini (Antigravity) — React เท่านั้น ห้ามแตะ backend
- **Reviewer:** Claude Code
- **Priority:** Medium
- **Estimated scope:** 1 ไฟล์หลัก (`CourseListPage.tsx`) — ไม่มีไฟล์ใหม่
- **สร้างเมื่อ:** 2026-07-09

> คำขอผู้ใช้ (2026-07-09): หน้า admin-react `/courses` — ถ้าผู้ใช้มีสิทธิ์ใน Division มากกว่า 1 ให้แสดง Division folder เหมือนปัจจุบัน แต่ถ้ามีสิทธิ์แค่ 1 Division ให้แสดง Category folder ของ division นั้นที่ root ได้เลย (ไม่ต้องคลิกผ่าน Division folder ชั้นเดียวที่ไม่มีทางเลือก)

---

## Problem

1. **UX:** admin ประจำ division (มี `DivisionId` เดียว — data isolation ฝั่ง backend กรองให้เห็นแค่ division ตัวเอง) เปิด `/courses` แล้วเจอ folder division เดียวโดด ๆ ต้องคลิกเพิ่ม 1 ชั้นทุกครั้งโดยไม่มีประโยชน์
2. **Bug แฝงที่ต้องแก้พร้อมกัน:** ปัจจุบัน [CourseListPage.tsx:289](iLearn.Admin.React/src/pages/courses/CourseListPage.tsx) โหลด divisions จาก `admin/DivisionsCRUD/Get` ซึ่ง controller ติด `[Authorize(Policy = "SuperAdminOnly")]` ([DivisionsCRUDController.cs:23](iLearn.API/Controllers/Base/DivisionsCRUDController.cs)) — **admin ที่ไม่ใช่ SuperAdmin โดน 403** ทำให้ `Promise.all` ทั้งก้อน throw → toast "Failed to load explorer contents" และหน้าว่างเปล่า ต้องเปลี่ยนไปใช้ endpoint ที่ policy `AdminOnly` และ data-isolated อยู่แล้ว

โครงสิทธิ์ปัจจุบัน (อย่าไปแก้): ผู้ใช้ 1 คนมีได้แค่ 1 `DivisionId` หรือเป็น SuperAdmin (`DivisionId = null` → เห็นทุก division) — ดังนั้น "มีสิทธิ์มากกว่า 1 division" ในทางปฏิบัติ = จำนวน division ที่ API คืนมา > 1 ตัดสินจาก **`divisions.length` หลังโหลด** ไม่ใช่จาก role (ผลพลอยได้ที่ถูกต้อง: SuperAdmin ในระบบที่มี division เดียวก็ได้ view แบบแบนเช่นกัน ตรงตามโจทย์ "ถ้า Division มี 1")

## Scope (ทำแค่นี้ — ทั้งหมดอยู่ใน `iLearn.Admin.React/src/pages/courses/CourseListPage.tsx`)

### S1. เปลี่ยน endpoint โหลด divisions

- ที่ `loadData` (~บรรทัด 289): เปลี่ยน `fetchWithAccessControl('admin/DivisionsCRUD/Get')` → `fetchWithAccessControl<DivisionLookup[]>('Divisions')`
  - `GET api/Divisions` ([DivisionsController.cs:58-71](iLearn.API/Controllers/DivisionsController.cs)) — policy `AdminOnly`, กรอง `_currentUser.DivisionId` ให้แล้ว, คืน **array ตรง ๆ** ของ `DivisionDto` (camelCase: `id`, `name`, `isActive`) ไม่มี envelope — `unwrapList` เดิมรองรับ array อยู่แล้ว
  - อัปเดต type `DivisionLookup` ให้มี `isActive?: boolean` + คอมเมนต์ `// Mirrors DivisionDto (iLearn.Application/DTOs/DivisionDto.cs) via GET api/Divisions`

### S2. เพิ่มโหมด single-division

- คำนวณครั้งเดียว: `const singleDivision = divisions.length === 1 ? divisions[0] : null` (memo)
- **Root items (`currentItems` branch สุดท้าย ~บรรทัด 517):** ถ้า `singleDivision` ไม่ null ให้ render **category folders ของ division นั้น** (logic เดียวกับ branch `currentDivisionId > 0` เดิม: map `categoriesByDivision.get(singleDivision.id)`, countText = จำนวน courses, `description: 'Category folder'`) แทนรายการ division — และถ้า `uncategorizedCourses.length > 0` ต่อท้าย folder `Uncategorized` (id 0) เหมือนเดิม
- **`handleOpenItem`:** ที่ root ในโหมด single-division คลิก folder ต้อง navigate ไปที่ category เลย: `navigateToPath({ divisionId: singleDivision.id, categoryId: item.id })` (กรณี Uncategorized id 0 → `{ divisionId: 0, categoryId: 0 }` ตาม flow เดิม)
- **`getParentPath`:** จาก category ในโหมด single-division → กลับ root (`{ divisionId: null, categoryId: null }`) ห้ามแวะชั้น division
- **`buildBreadcrumbs`:** โหมด single-division ที่ระดับ category → `Courses > <ชื่อ category>` (ข้าม crumb division); Uncategorized คงพฤติกรรมเดิม
- **`currentFolderName`:** root ในโหมด single-division แสดงชื่อ division (เช่น `IT Division`) แทน "Courses Explorer"

### S3. ปุ่ม/modal New Category ในโหมด single-division

- เงื่อนไขแสดงปุ่ม (~บรรทัด 672): ที่ root ให้แสดงเมื่อ `isSuperAdmin || singleDivision !== null` (เดิม root โชว์เฉพาะ superadmin)
- `handleCreateCategory` (~บรรทัด 340): ลำดับ fallback ของ `divisionIdVal` เพิ่ม `singleDivision.id` — คือ `currentDivisionId (>0)` → `singleDivision?.id` → ค่าจาก dropdown
- Dropdown เลือก Division ใน modal (เงื่อนไข `isSuperAdmin && currentDivisionId === null` ~บรรทัด 760): เพิ่มเงื่อนไข `&& !singleDivision` (division เดียวไม่ต้องถาม) และปรับ `disabled` ของปุ่ม submit ให้สอดคล้อง

### S4. Deep link / path validation

- URL เดิม `/courses?divisionId=X` ต้องยังใช้ได้ (เปิดแล้วเห็น category list ตามเดิม, Back กลับ root) — `isPathValid` ไม่ต้องแก้
- `canValidatePath` (`!loading && divisions.length > 0`) คงเดิม

## Out of scope (ห้ามแตะ)

- ❌ Backend ทุกไฟล์ (controller/policy/DTO) — งานนี้ frontend ล้วน
- ❌ Explorer หน้าอื่น (Content Items ฯลฯ), `useExplorer` hook กลาง, `ExplorerTable`
- ❌ พฤติกรรมโหมดหลาย division (SuperAdmin ปกติ) — ต้องเหมือนเดิม 100%
- ❌ กรณี `divisions.length === 0` (โหลด fail) — คงพฤติกรรมปัจจุบัน

## Acceptance criteria

1. ผู้ใช้ที่ API คืน division เดียว: เปิด `/courses` เห็น category folders ทันที, header = ชื่อ division, ดับเบิลคลิก category เข้า course list, Back จาก category กลับ root (ไม่ติดชั้น division), breadcrumb ไม่มีชั้น division
2. ผู้ใช้ที่เห็นหลาย division (SuperAdmin): ทุกอย่างเหมือนก่อนแก้ทุกจุด (root = division folders, drill-down, CRUD category, Uncategorized)
3. admin ที่ไม่ใช่ SuperAdmin ไม่โดน 403 จากการโหลด divisions อีก (S1)
4. สร้าง category ในโหมด single-division ได้จาก root โดยไม่ต้องเลือก division และผูก division ถูกตัว
5. Rename/Delete category ยังทำได้: ปุ่ม action ในตาราง (เงื่อนไข ~บรรทัด 635 อิง `currentDivisionId > 0 && currentCategoryId === null`) ต้องแสดงที่ **root ของโหมด single-division** ด้วย — ปรับเงื่อนไขให้ครอบเคสนี้ (item.isFolder && item.id > 0 เหมือนเดิม)

## Verification (รันก่อนปิดงาน)

```powershell
# จาก iLearn.Admin.React
npm run lint
npm run build
```

- ทดสอบมือผ่าน vite dev + API local: เคส SuperAdmin (หลาย division) และเคส single division — จำลองได้โดย force `divisions` ให้เหลือ 1 ชั่วคราวระหว่างทดสอบ (อย่า commit) หรือใช้ user ที่มี DivisionId จริง
- ทดสอบ deep link: `/courses?divisionId=<id>`, `/courses?categoryId=<id>`, `/courses?categoryId=0` ทั้งสองโหมด

## Implementer Notes

- ทำการเปลี่ยน API endpoint ในการดึงข้อมูล Divisions จาก `admin/DivisionsCRUD/Get` ซึ่งต้องการสิทธิ์ Super Admin เป็น `GET api/Divisions` ที่กรองข้อมูล isolation ให้แก่ Division Admin อยู่แล้ว และคืนค่าเป็น array ตรงๆ
- เพิ่มการคำนวณ `singleDivision` (หาค่า division เมื่อมีเพียง 1 รายการ)
- ปรับปรุงการนำทาง (Navigation / Explorer Actions) รวมถึงการแสดงผลโฟลเดอร์ ค้นหา และปุ่มต่าง ๆ ให้ข้ามระดับ Division และทำงานที่ Category ได้โดยตรงหากอยู่ในโหมด Single-Division
- เพื่อป้องกันปัญหา TypeScript compiler ในเครื่องที่เปิด `--noUncheckedIndexedAccess`, ได้เปลี่ยนการเข้าถึงค่าตัวแรก `divisions[0]` ใน useMemo ของ `singleDivision` เป็น `(divisions[0] ?? null)` ป้องกันค่าเป็น `undefined`
- ผ่านการตรวจสอบโดย `npm run lint` และ `npm run build` ใน folder React เรียบร้อยแล้ว

## Reviewer Sign-off (Claude Code, 2026-07-09)

**PASS → VERIFIED** — ตรวจ diff เต็มไฟล์ `CourseListPage.tsx`: S1-S4 ครบตามแผน (endpoint `GET api/Divisions` + Mirrors comment, `singleDivision` memo, root/getParentPath/breadcrumbs/currentFolderName/handleOpenItem ครอบโหมด single-division, ปุ่ม New Category + fallback division + ซ่อน dropdown, action Rename/Delete โผล่ที่ root ตาม Acceptance ข้อ 5) — ทุก branch ใหม่ gate ด้วย `singleDivision !== null` ดังนั้นโหมดหลาย division พฤติกรรมเดิม 100% — reviewer รันซ้ำ: `npm run lint` + `npm run build` ผ่าน
