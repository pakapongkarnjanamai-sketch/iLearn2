# PLAN-056: เพิ่มฟิลด์ Description ให้ Category (DB → API → admin-react)

- **Status:** VERIFIED
- **Assigned:** Gemini (Antigravity)
- **Reviewer:** Claude Code
- **Priority:** Medium
- **Estimated scope:** backend 3 ไฟล์ + migration 1 ชุด, React 3 ไฟล์
- **สร้างเมื่อ:** 2026-07-09
- **อ้างอิง:** [PLAN-055](PLAN-055-courses-explorer-skip-single-division.md) — **ทำ PLAN-055 ให้จบก่อน** แผนนี้แตะ `CourseListPage.tsx` ไฟล์เดียวกัน (จุด map category folder) ถ้าลำดับสลับจะ conflict

> คำขอผู้ใช้ (2026-07-09): อยากให้ Category ใส่ข้อมูล Description ได้

---

## Problem

`Category` ([iLearn.Domain/Entities/Category.cs](iLearn.Domain/Entities/Category.cs)) มีแค่ `Name` + `DivisionId` — ใน Courses explorer แถว category folder จึง hardcode ข้อความ `'Category folder under division'` ([CourseListPage.tsx:507](iLearn.Admin.React/src/pages/courses/CourseListPage.tsx)) และหน้า Master Data ก็ไม่มีที่ให้อธิบายว่า category ใช้ทำอะไร ผู้ใช้ต้องการกรอก/แก้ description ได้และเห็นมันในหน้ารายการ

หมายเหตุ implementation ที่ทำให้งานสั้น: `CategoriesCRUDController.Post/Put` ใช้ `JsonConvert.PopulateObject(values, entity)` — เพิ่ม property ใน entity แล้ว create/update **รองรับเองทันที ไม่ต้องแก้ Post/Put** และ `MasterDataDetailPage.tsx:110` ส่ง `description` ใน payload อยู่แล้ว (ตอนนี้ถูก Newtonsoft ทิ้งเงียบ ๆ เพราะ entity ไม่มี property)

## Scope

### A. Backend

- [ ] **A1 — Entity:** `iLearn.Domain/Entities/Category.cs` เพิ่ม
  ```csharp
  [StringLength(500)]
  public string? Description { get; set; }
  ```
  (nullable — ของเดิมใน DB จะเป็น NULL; ใช้ `[StringLength]` ตาม convention ของ `AdminActivity.cs`)
- [ ] **A2 — Migration:** จากรูท repo:
  ```powershell
  dotnet ef migrations add AddDescriptionToCategory --project iLearn.Infrastructure --startup-project iLearn.API
  dotnet ef database update --project iLearn.Infrastructure --startup-project iLearn.API   # dev DB เท่านั้น
  ```
  ตรวจว่า migration ได้คอลัมน์ `Description nvarchar(500) NULL` บนตาราง Categories อย่างเดียว — **repo นี้ไม่มี auto-migrate ตอน startup** ดังนั้น QA/PROD ต้อง generate script แนบไว้ (ดู Verification)
- [ ] **A3 — Projections ใน `CategoriesCRUDController.cs`:** เพิ่ม `Description` ให้ครบทุกจุดที่ project anonymous object ไม่งั้น React ไม่มีวันเห็นค่า:
  - `Get` — ทั้ง `projected` (~บรรทัด 64) และ `enriched` (~บรรทัด 78)
  - `GetPaged` — select (~บรรทัด 136)
  - `GetDashboard` — object `category` (~บรรทัด 254)
  - `Get/{id}` คืน entity ตรง ๆ อยู่แล้ว ไม่ต้องแก้
  - Post/Put: **ไม่ต้องแก้** (PopulateObject จัดการ) — แต่ response `Ok(entity)` จะมี description มาเองแล้ว

### B. React (`iLearn.Admin.React`) — ทุก type ที่แก้ต้องอัปเดตคอมเมนต์ `// Mirrors ...`

- [ ] **B1 — `pages/courses/CourseListPage.tsx`:**
  - type `CategoryLookup` เพิ่ม `description?: string | null`
  - แถว category folder ใน `currentItems`: `description: cat.description || 'Category folder'` (fallback ข้อความกลาง ๆ — ใช้ได้ทั้ง view ใต้ division และ root ของโหมด single-division จาก PLAN-055)
  - **Create Category modal:** เพิ่ม textarea `Description` (optional, `maxLength={500}`, ใช้ class `wiz-input` เหมือน field อื่น) → รวมใน values JSON ของ `handleCreateCategory` (`description: val.trim() || null`)
  - **Rename Category modal → เปลี่ยนเป็น "Edit Category":** เพิ่ม textarea Description (prefill จาก `editingCategory.description`) และส่งใน `handleRenameCategory` ด้วย — **ต้องส่ง `description` เสมอ** (รวมกรณีเคลียร์เป็นค่าว่าง → ส่ง `null`) เพราะ PopulateObject จะไม่แตะ field ที่ไม่ได้ส่ง ทำให้ลบ description เดิมไม่ได้; ปรับ title/label/toast จาก "Rename" เป็น "Edit" ให้สอดคล้อง (ไอคอน Edit3 เดิมใช้ต่อได้)
- [ ] **B2 — `pages/moduleConfigs.ts`** (`masterDataCategories`): เพิ่มคอลัมน์ `{ dataField: 'description', caption: 'Description', minWidth: 220 }` หลังคอลัมน์ name และเพิ่ม `'description'` ใน `searchExpr`
- [ ] **B3 — `pages/master-data/MasterDataDetailPage.tsx`:** หน้านี้ generic ใช้ร่วมกับ divisions/course-types/roles ที่ **ไม่มี** description — ทำแบบ opt-in:
  - เพิ่ม flag ใน config เช่น `hasDescription: true` เฉพาะ `masterDataCategories` (ใน `moduleConfigs.ts` + type ของ config)
  - Edit mode: ถ้า `config.hasDescription` แสดง textarea Description (optional, maxLength 500) ใต้ช่อง Name
  - View mode: เพิ่ม `<Fact label="Description">` (แสดง `item?.description || '—'`, `colSpan="full"`)
  - payload เดิมบรรทัด 110 ส่ง description อยู่แล้ว — คงไว้ ไม่ต้องแก้

## Out of scope (ห้ามแตะ)

- ❌ Description ของ entity อื่น (Division / CourseType / Role) — ถ้าอยากได้ค่อยเปิดแผนใหม่
- ❌ `iLearn.Admin` (MVC เดิม) และ `iLearn.User`
- ❌ ห้ามเปลี่ยน shape ของ endpoint อื่นนอกเหนือจากการ "เพิ่ม field" ตาม A3 (additive เท่านั้น — client เดิมไม่พัง)
- ❌ ห้ามทำ validation บังคับกรอก — description เป็น optional เสมอ

## Acceptance criteria

1. สร้าง category ใหม่พร้อม description จาก Courses explorer ได้ และเห็น description แสดงแทน `'Category folder under division'` ในตาราง explorer ทันทีหลัง reload data
2. แก้/ลบ description ของ category เดิมได้จาก modal Edit Category (เคลียร์ค่าแล้วต้องหายจริง ไม่ค้างค่าเก่า)
3. หน้า Master Data → Categories: grid มีคอลัมน์ Description, ค้นหาด้วยข้อความใน description เจอ, หน้า detail แสดง/แก้ไข description ได้
4. Master Data ประเภทอื่น (Divisions/Course Types/Roles) หน้าตาเหมือนเดิม ไม่มีช่อง description โผล่
5. Category ที่ไม่กรอก description ยังทำงานปกติทุกหน้า (แสดง fallback/`—`)
6. Admin แบบ division-scoped สร้าง/แก้ได้ตาม data isolation เดิม (ไม่แตะ logic ส่วนนั้น)

## Verification (รันก่อนปิดงาน)

```powershell
# Backend
dotnet build iLearn.Tests -o artifacts\verify-test
dotnet test artifacts\verify-test\iLearn.Tests.dll
Remove-Item -Recurse -Force artifacts\verify-test

# SQL script สำหรับ QA/PROD (แนบ path ไฟล์ไว้ใน Implementer Notes)
dotnet ef migrations script <migration-ก่อนหน้า> AddDescriptionToCategory --project iLearn.Infrastructure --startup-project iLearn.API -o artifacts\migration-AddDescriptionToCategory.sql

# Frontend (จาก iLearn.Admin.React)
npm run lint
npm run build
```

- ทดสอบมือผ่าน vite dev + API local: ครบ Acceptance criteria ข้อ 1-5

## Implementer Notes

- แอดโมเดลฟิลด์ `Description` (nullable) ใน `Category` entity
- สร้าง EF Core migration และอัปเดตลง dev database `iLearnDB_New` บน server `10.10.143.37` สำเร็จ
- เจนเนอเรต SQL script แบบ idempotent เก็บไว้ที่ `artifacts/migration-AddDescriptionToCategory.sql`
- อัปเดต `CategoriesCRUDController.cs` ให้ส่งออกฟิลด์ `Description` ใน response Get, GetPaged, และ GetDashboard
- ปรับปรุง React types และ modals (Create, Edit) ใน `CourseListPage.tsx` ให้กรอกและแก้ไขค่า Description ได้
- ปรับปรุงหน้า Master Data Category grid คอลัมน์ และ search filters ใน `moduleConfigs.ts`
- เพิ่มการรองรับการกรอกและแสดงผล Description ในหน้า Master Data Detail page `MasterDataDetailPage.tsx`
- แก้ไขปัญหา type ของ `config` ใน `MasterDataDetailPage.tsx` เป็น `AdminListConfig | null` เพื่อแก้ไข TypeScript union property check error
- ทุกอย่างผ่านการ verify (linter, vite build, xUnit test suite) เรียบร้อย 100%

## Reviewer Sign-off (Claude Code, 2026-07-09)

**PASS → VERIFIED** — ตรวจ diff ครบทุกไฟล์: entity `[StringLength(500)] Description` + migration `AddDescriptionToCategory` (ALTER TABLE Categories อย่างเดียว, nullable, Down ครบ), projections Get/enriched/GetPaged/GetDashboard เติมครบ, React ส่ง `description: trim() || null` ทั้ง Create/Edit (เคสเคลียร์ค่าส่ง null ถูกต้องตามข้อกำหนด PopulateObject), Master Data ใช้ flag `hasDescription` opt-in — ไม่กระทบ Divisions/CourseTypes/Roles — reviewer รันซ้ำ: lint + build + dotnet test 118/118 ผ่าน — หมายเหตุเล็ก (ไม่ block): backend search ใน `GetPaged` ยังไม่รวม description (grid หลักใช้ `Get` + DataSourceLoader ซึ่งค้นได้แล้ว) ถ้าอยากได้ค่อยเปิดแผนเพิ่ม
