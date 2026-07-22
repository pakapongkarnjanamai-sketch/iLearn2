# PLAN-111: Category มี running no (SortOrder) ต่อ Division — admin แก้ได้ + ตัดเลขหน้าชื่อ

- **Status:** READY
- **Assigned:** GitHub Copilot (§1/§2 backend) + Antigravity Gemini (§3 React) — **contract §2 FREEZE**
- **Reviewer:** Claude Code
- **สร้างเมื่อ:** 2026-07-22
- **ที่มา:** ผู้ใช้อยากให้ Categories มีเลขลำดับ (running no) **แยกออกจากชื่อ** — ตอนแรกเรียงตาม id ต่อ Division แล้ว admin ปรับเองได้
- **อ่าน CLAUDE.md หัวข้อ Migration + React (AppTable/config, Route remount) ก่อนเริ่ม**

> **ผู้ใช้ตัดสินแล้ว (2026-07-22):** (1) admin แก้ลำดับด้วย **ช่องกรอกตัวเลข** (ไม่ใช่ drag-drop) · (2) **ตัดเลขหน้าชื่อออก** (`"1. Environment & Safety"` → `"Environment & Safety"`, เลขไปอยู่คอลัมน์ running no)

---

## สภาพปัจจุบัน (ยืนยันจากโค้ด)

- `Category` (`iLearn.Domain/Entities/Category.cs`): `Name, DivisionId, Division, Description, Courses` — **ไม่มี field ลำดับ**; เลข "1." ฝังใน `Name`
- Admin React: table แบบ config — `moduleConfigs.ts` → `masterDataCategories.columns` (controller `CategoriesCRUD`)
- ordering ปัจจุบัน: `CategoriesController` (learner) `.OrderBy(c => c.Name)` (บรรทัด 40); `CategoriesCRUDController` เรียง name/isactive/id
- **ต้องตรวจ:** โค้ดที่ match ชื่อ category แบบ string (จะพังถ้าตัด prefix) — grep `Category.Name ==` / `.Name.Contains` ก่อนแก้ (ตรวจแล้วรอบแรก: learner catalog match ที่ **divisionName** ไม่ใช่ category name — แต่ verify ซ้ำ)

## Scope

### §1 (backend) — Domain + Migration

**1.1 เพิ่ม field**
```csharp
public int SortOrder { get; set; }   // running no ต่อ Division (1-based)
```

**1.2 Migration** (ต้องอยู่ `iLearn.Infrastructure/Migrations/` + namespace `iLearn.Infrastructure.Migrations` เท่านั้น — กติกา CLAUDE.md)
- add column `SortOrder int NOT NULL DEFAULT 0`
- **backfill ใน `Up()`** (raw SQL — InMemory test ไม่รัน migration อยู่แล้ว):
  1. **SortOrder:** ต่อ `DivisionId` เรียงตาม `Id` → ใส่ 1,2,3… (row_number() over partition by DivisionId order by Id); `DivisionId IS NULL` เป็นกลุ่มของตัวเอง; เฉพาะ `IsDeleted=0`
  2. **ตัดเลขหน้าชื่อ:** `Name` ที่ขึ้นต้นด้วย `^\d+\.\s*` → ตัด prefix ออก (เช่น `"10. Parts system control"` → `"Parts system control"`) — ทำใน SQL ด้วย pattern/PATINDEX หรือ loop; ระวังชื่อที่ไม่มี prefix (คงเดิม)
- **Down():** drop column (ชื่อที่ตัด prefix ไปแล้ว restore ไม่ได้ — จดใน Notes ว่า down ไม่คืนชื่อเดิม; backup ก่อน deploy PROD)

### §2 (backend) — API **[CONTRACT FREEZE]**

- `CategoryDto` / response ของ `CategoriesCRUDController` (+ learner `CategoriesController`) เพิ่ม **`sortOrder` (int)**
- **รับแก้ `sortOrder`** ผ่าน PUT เดิมของ GenericController (ให้ DTO/update รวม field นี้) — admin กรอกเลขแล้ว save ได้
- **default ordering เปลี่ยนเป็น `(DivisionId, SortOrder, Id)`** ทั้ง:
  - `CategoriesCRUDController` (สาขา default — บรรทัด 128-129) 
  - `CategoriesController` learner (บรรทัด 40: `OrderBy(Name)` → `OrderBy(DivisionId).ThenBy(SortOrder)`)
  - จุดที่ learner catalog/sidebar เรียง category (ตรวจ `EnrollmentsController` — ถ้าเรียงตาม name ให้เปลี่ยน)
- **ไม่บังคับ unique** `(DivisionId, SortOrder)` — admin อาจกรอกเลขซ้ำชั่วคราวระหว่างจัด; ใช้ `Id` เป็น tiebreak

**Contract ที่ freeze (React ลอกตาม):**
```
CategoryDto { id, name, description, divisionId, divisionName, sortOrder, courseCount, isActive, createdAt }
// sortOrder: int, 1-based ต่อ division; แก้ได้ผ่าน PUT
```

### §3 (React admin) — คอลัมน์ running no + แก้ได้ (หลัง §2 freeze)

- `moduleConfigs.ts` `masterDataCategories`:
  - เพิ่มคอลัมน์ **`{ dataField: 'sortOrder', caption: 'ลำดับ', dataType: 'number', width: 90, alignment: 'center' }`** ไว้**ซ้ายสุด** (ก่อน name)
  - default sort ของ grid = division + sortOrder (ถ้า config รองรับ)
- **ฟอร์ม create/edit category** เพิ่มช่อง **number input `sortOrder`** (ตาม UI Conventions — ใช้ shared component, ห้าม hand-roll)
  - lอก type จาก CategoryDto พร้อมคอมเมนต์ `// Mirrors CategoryDto (...)` ตามกติกา API Contract Sync
- **ไม่ทำ drag-drop** (ผู้ใช้เลือกช่องกรอกตัวเลข)
- ถ้ามี editor route detail ต้องครอบ `<Remount>` ตามกติกา (ตรวจว่ามีอยู่แล้วไหม)

### §4 — learner sidebar หลังตัด prefix

- หลัง §1 ตัดเลขหน้าชื่อ → sidebar หน้า learner (`MyLearning/Index.cshtml`) จะแสดงชื่อ**ไม่มีเลข** — ถูกตามเจตนา
- **ถ้าผู้ใช้อยากเห็นเลขนำหน้าใน learner ด้วย** ให้ประกอบ `sortOrder + ". " + name` ตอนแสดง (ยังไม่ทำในแผนนี้ — จดเป็น option รอผู้ใช้ยืนยัน)

## Contract ที่เปลี่ยน

- DB schema: **+column `Category.SortOrder`** ⇒ **มี migration** (deploy ต้อง `dotnet ef database update`)
- `CategoryDto` +`sortOrder` (additive) — PLAN นี้ React ต้อง mirror
- `Category.Name` ของข้อมูลเดิมถูกตัด prefix (backfill)
- ordering เปลี่ยน (name → sortOrder)

## นอก Scope (ห้ามทำ)

- ห้าม drag-drop / reorder endpoint (ผู้ใช้เลือกช่องกรอก)
- ห้าม unique constraint บน (DivisionId, SortOrder)
- ห้ามแตะ learner display เพิ่มเลขนำหน้า (§4 เป็น option รอยืนยัน)
- ห้ามวาง migration นอก `iLearn.Infrastructure/Migrations/`
- ห้ามแตะ Category ฟิลด์อื่น / entity อื่น

## Verification

```powershell
dotnet build iLearn.Tests -o artifacts\verify-test
dotnet test artifacts\verify-test\iLearn.Tests.dll
Remove-Item -Recurse -Force artifacts\verify-test
# React
cd iLearn.Admin.React; npm run lint; npm run build
```

Migration/data:
1. รัน migration บน QA → `SELECT DivisionId, SortOrder, Name FROM Categories WHERE IsDeleted=0 ORDER BY DivisionId, SortOrder` → แต่ละ division เลข 1,2,3… ต่อเนื่อง, ชื่อ**ไม่มี** prefix `N.`
2. ชื่อที่ไม่มี prefix เดิม → คงเดิม (ไม่โดนตัดผิด)
3. `Category.Name ==`/matching เดิมไม่พัง (grep + smoke)

Manual (QA):
4. Admin Categories: มีคอลัมน์ **ลำดับ** ซ้ายสุด, เรียงตาม division+ลำดับ, ชื่อไม่มีเลข
5. แก้ sortOrder ของ category หนึ่ง → save → refresh → ลำดับเปลี่ยนตาม
6. learner catalog/sidebar: หมวดหมู่เรียงตามลำดับใหม่, ชื่อไม่มีเลข, course ในหมวดถูกต้อง
7. React `npm run build` + lint ผ่าน; admin console 0 error

## Deploy note

- **มี migration** ⇒ deploy build ที่มี migration **ต้องรัน `dotnet ef database update --connection <env>` คู่กันเสมอ** (กติกา CLAUDE.md/PLAN-092) — ไม่มี auto-migrate
- แตะ **API + Admin React** (ไม่แตะ learner app ยกเว้น §4 ถ้าทำ — รอบนี้ไม่ทำ)
- ลำดับ: build+migration → deploy API + รัน `database update` QA → deploy Admin React → verify → PROD (รอผู้ใช้ยืนยัน + backup ชื่อ category ก่อน)

## Implementer Notes

_(เติมโดย implementer)_
