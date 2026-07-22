# PLAN-117: Course explorer — โชว์ลำดับในชื่อ Category folder + แก้ลำดับได้จาก modal Edit

- **Status:** VERIFIED — QA smoke (folder numbering + edit sortOrder + create without sortOrder) + deploy PROD สำเร็จผ่าน PLAN-118
- **Assigned:** Antigravity Gemini (React ไฟล์เดียว — `CourseListPage.tsx`)
- **Reviewer:** Claude Code
- **สร้างเมื่อ:** 2026-07-22
- **ที่มา:** ผู้ใช้รีวิว `/admin-react/courses?divisionId=1` บน QA: (1) อยากเห็น **ลำดับ** ของ Category แสดงในชื่อ folder (2) ปุ่ม **Edit** ของ category folder ให้แก้ **ลำดับ** ได้ด้วย
- **อ่าน `iLearn.Admin.React/README.md` (API Contract Sync) ก่อนเริ่ม**

---

## วินิจฉัย (ยืนยันจากโค้ด — `CourseListPage.tsx`)

- categories โหลดจาก `admin/CategoriesCRUD/Get` (บรรทัด ~315) ซึ่ง**ส่ง `sortOrder` มาแล้ว**ตั้งแต่ PLAN-111 — แต่ mirror type `CategoryLookup` (~47-53) **ยังไม่มี field นี้** ⇒ แค่เติม type ก็ใช้ได้ ไม่ต้องแตะ backend
- การเรียง category folder ใช้ `sortByNameAsc` (ชื่อ A-Z) — 2 จุดที่เป็น **รายการ category**: `categoriesByDivision` (~160) และสาขา division view (~579); ส่วน `items.sort` ที่ ~533/~609 เป็นรายการ division/root — **ห้ามแตะ**
- ปุ่ม Edit ของ folder เปิด modal (`editingCategory` state, ~886) มีช่อง name + description แล้ว PUT `admin/CategoriesCRUD/Put` ด้วย `values` JSON — backend รับ `sortOrder` ผ่าน `PopulateObject` ได้อยู่แล้ว (PLAN-111 §2)
- pseudo folder `Uncategorized` (id 0) ถูก push ต่อท้ายหลัง sort — ไม่กระทบ

## Scope (React ล้วน — ไม่มี backend/migration)

### §1 Mirror type + การเรียง

- `CategoryLookup` เพิ่ม `sortOrder: number` (คอมเมนต์ mirror อ้าง `CategoryDto` เดิม)
- เพิ่ม comparator `sortCategoriesByOrder = (a, b) => (a.sortOrder - b.sortOrder) || (a.id - b.id)` แล้วใช้แทน `sortByNameAsc` เฉพาะ **2 จุดที่ sort รายการ category**: `categoriesByDivision` (~160) และ list ใน division view (~579)
- `sortByNameAsc` ของ division/root/course ไม่แตะ

### §2 แสดงลำดับในชื่อ folder

- ตอนสร้าง `ExplorerItem` ของ category folder: `name: category.sortOrder > 0 ? `${category.sortOrder}. ${category.name}` : category.name`
- เฉพาะ **folder row** — breadcrumb / modal / ที่อื่นที่ใช้ `category.name` คงชื่อสะอาดไม่มีเลข
- `Uncategorized` ไม่มีเลข

### §3 Edit modal แก้ลำดับได้

- state `editCategorySortOrder` — ตั้งค่าเริ่มจาก `editingCategory.sortOrder` ตอนเปิด modal
- เพิ่ม **number input** `Sort Order (ลำดับ)` (`min={1}`, `required`, pattern เดียวกับฟอร์ม `MasterDataDetailPage` ของ PLAN-111) ใน modal Edit Category
- `handleRenameCategory`: เพิ่ม `sortOrder: Number(editCategorySortOrder)` ลง `values` JSON — ส่งเฉพาะเมื่อเป็นเลขถูกต้อง (form `required` + native submit กันค่าว่างแล้ว — modal นี้เป็น `as="form"` `onSubmit` อยู่แล้ว ✓)
- **modal New Category** เพิ่ม input เดียวกันแบบ **optional** (ว่างได้ → ไม่ส่ง field → backend default 0): กัน category ใหม่ที่ไม่ตั้งลำดับไปลอยบนสุดแบบเงียบ ๆ — ถ้าเว้นว่าง folder จะแสดงชื่อไม่มีเลข (ตาม §2 กติกา >0) จน admin มาตั้ง

### นอก Scope (ห้ามทำ)

- ห้ามแตะ backend — `CategoriesCRUD/Get`/`Put` รองรับครบแล้ว
- ห้ามแตะการเรียง division / course rows / root items
- ห้าม drag-drop
- ห้ามใส่เลขใน breadcrumb หรือหน้าจออื่นนอก folder list ของหน้านี้

## Contract ที่เปลี่ยน

ไม่มี — mirror type ตามของจริงที่ backend ส่งอยู่แล้ว

## Verification

```powershell
cd iLearn.Admin.React; npm run lint; npm run build
```

Manual (QA — `/admin-react/courses?divisionId=1`):
1. Category folders แสดง `1. <ชื่อ>`, `2. <ชื่อ>` … เรียงตามลำดับ (ตรงกับหน้า Master Data Categories); `Uncategorized` อยู่ท้าย ไม่มีเลข
2. กด Edit ที่ folder → modal มีช่อง Sort Order ค่าปัจจุบัน → แก้เลข → Save → folder เรียงใหม่ + เลขหน้าชื่อเปลี่ยน; ชื่อ/description เดิมไม่พัง (แก้กลับหลังทดสอบ)
3. New Category: สร้างโดยไม่กรอกลำดับ → โผล่ไม่มีเลขนำหน้า; สร้างพร้อมลำดับ → แสดงเลขถูก (ลบ category ทดสอบทิ้งหลังเสร็จ)
4. คลิกเข้า folder → breadcrumb แสดงชื่อ**ไม่มีเลข**; course list ข้างในปกติ
5. console 0 error

## Deploy note

- **Admin React เท่านั้น** — QA → verify → PROD (รอผู้ใช้ยืนยัน)
- ระวังชนกับ PLAN-116 §1 ที่ Copilot อาจกำลังแก้ `AssignmentReportCharts`/หน้า assignments — คนละไฟล์กัน ไม่ควรชน แต่เช็ค AGENT_LOG ก่อนเริ่มตามกติกา

## Implementer Notes

- **§1**: อัปเดต `CategoryLookup` เพิ่ม `sortOrder: number` และสร้าง `sortCategoriesByOrder = (a, b) => ((a.sortOrder ?? 0) - (b.sortOrder ?? 0)) || (a.id - b.id)` สลับใช้แทน `sortByNameAsc` สำหรับ `categoriesByDivision` และ `singleDivision` list (ไม่แตะ division/course/root)
- **§2**: จัดฟอร์แมตชื่อ category folder row ให้แสดงเลขลำดับแบบ `${cat.sortOrder}. ${cat.name}` เมื่อ `sortOrder > 0` โดยชื่อใน breadcrumb / modals ยังเป็นชื่อสะอาดตามเดิม
- **§3**: เพิ่ม input `Sort Order (ลำดับ)` ใน Edit Category Modal (required, min=1) และ Create Category Modal (optional) พร้อมทั้งส่ง `sortOrder` ใน JSON payload ของ `handleRenameCategory` และ `handleCreateCategory`
- **Verification**: `npm run lint` ผ่าน 0 errors และ `npm run build` ผ่าน 0 errors (built in 1.68s)

## Reviewer Sign-off (Claude Code, 2026-07-22)

**ผลรีวิว: ✅ ผ่าน — REVIEWED**

1. **§1:** `CategoryLookup` +`sortOrder`; comparator ใช้ `?? 0` กัน undefined ดี; สลับ sort เฉพาะ `categoriesByDivision` + division-view list (ผ่าน `a.original as CategoryLookup` — list ณ จุดนั้นมีแต่ category folder จริง, `Uncategorized` push หลัง sort พร้อม `sortOrder: 0` ใน original ครบ type); division/root/course ยัง `sortByNameAsc` เดิม ✓
2. **§2:** prefix `${sortOrder}. ${name}` เฉพาะ 2 จุดสร้าง ExplorerItem folder row; breadcrumb ใช้ `category.name` ตรงจาก lookup — ยืนยันไม่มีเลข ✓
3. **§3:** Edit modal — ค่าเริ่มจาก `category.sortOrder`, `required` + `disabled` guard (`editCategorySortOrder === ''`) สองชั้น, ส่งเฉพาะเมื่อไม่ว่าง; Create modal — optional ตามแผน, payload ใส่ `sortOrder` เฉพาะเมื่อกรอก (ว่าง → backend default 0 → folder ไม่มีเลขจน admin ตั้ง) ✓
4. **Reviewer รัน verify เอง:** `npm run lint`/`npm run build` 0 errors

**คงค้าง: deploy Admin React ขึ้น QA → manual 1-5 → PROD รอผู้ใช้ยืนยัน** (รวม deploy กับ PLAN-116 ได้)

