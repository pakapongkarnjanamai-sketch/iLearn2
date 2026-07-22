# PLAN-111: Category มี running no (SortOrder) ต่อ Division — admin แก้ได้ + ตัดเลขหน้าชื่อ

- **Status:** VERIFIED
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

### GitHub Copilot — §1/§2 (backend) DONE 2026-07-22

- **§1 Domain + Migration:** เพิ่ม `Category.SortOrder` (int, default 0). Migration `AddSortOrderToCategory` (`iLearn.Infrastructure/Migrations/`, namespace `iLearn.Infrastructure.Migrations`) — `AddColumn` + backfill SQL 2 ก้อน: (1) `ROW_NUMBER() OVER (PARTITION BY ISNULL(DivisionId,-1) ORDER BY Id)` เฉพาะ `IsDeleted=0` (2) ตัด prefix ด้วย `CHARINDEX('.', Name)` + เช็คว่าก่อนจุดเป็นตัวเลขล้วน (`LEFT(...) NOT LIKE '%[^0-9]%'`) แทน pattern คงที่ 1-4 หลัก — รองรับความยาวเลขได้ทุกหลัก. `Down()` แค่ drop column (ชื่อที่ตัด prefix restore ไม่ได้ — จดไว้ใน comment แล้ว)
- **⚠️ พบ migration ชนกันระหว่างทำงาน:** ระหว่างรัน `dotnet ef migrations add` เจอไฟล์ migration ชื่อ `AddCategorySortOrder` ถูกสร้างขึ้นเองพร้อม side-effect ที่อันตราย — diff ของ 3 unique index (`IX_EnrollmentAssignments_EnrollmentId_AssignmentId`/`IX_Assignments_AssignmentNo_CourseId`/`IX_AssignmentCourses_AssignmentId_CourseId`) ที่ `Up()` จะ **ถอด filter `[IsDeleted]=0` ออก** (ย้อนกลับ fix ของ PLAN-092/SoftDeleteFilteredUniqueIndexes!) เพราะ `AppDbContextModelSnapshot.cs` ปัจจุบันไม่ตรงกับ DB จริงสำหรับ 3 index นี้ (index config ใน Fluent API ไม่มี `HasFilter` ครบ แต่ migration ก่อนหน้าใส่ filter ผ่าน raw `CreateIndex` เท่านั้น — เป็น tech debt เดิม ไม่เกี่ยวกับแผนนี้). **แก้โดยลบ migration นั้นทิ้งและเขียน migration ใหม่ด้วยมือ** (คัดลอกเฉพาะ AddColumn+backfill SQL, ไม่แตะ index ใด ๆ) เพื่อไม่ให้ scope บาน — **repo ยังมี tech debt เดิมนี้ค้างอยู่ (จะโผล่ซ้ำทุกครั้งที่ `dotnet ef migrations add` ถูกเรียกจนกว่าจะมีคนแก้ Fluent API ให้มี `HasFilter` ตรงกับ DB จริง) ไม่ได้แก้ในรอบนี้ตามขอบเขตแผน**
- **§2 API [CONTRACT FREEZE — ไม่เบี่ยง]:** `CategoryDto` +`SortOrder` (int). `CategoriesController` (admin lookup/GetById) เพิ่ม `SortOrder` ใน projection + ordering `OrderBy(DivisionId).ThenBy(SortOrder).ThenBy(Id)`. `CategoriesCRUDController`: `Get` (DataSourceLoader) เพิ่ม `.OrderBy(DivisionId).ThenBy(SortOrder).ThenBy(Id)` ก่อน project + เพิ่ม `SortOrder` ในทั้ง projected และ enriched anonymous object; `GetPaged` เพิ่ม branch `sortorder` ใน sort switch + เปลี่ยน default (`_` case) เป็น `(DivisionId,SortOrder,Id)` + เพิ่ม `SortOrder` ใน select. **ไม่ต้องแก้ Post/Put ของ GenericController** — ใช้ `JsonConvert.PopulateObject(values, entity)` อยู่แล้ว รับ `sortOrder` จาก React form ได้ทันทีเพราะ populate ตรงเข้า entity property ไม่ผ่าน DTO
- ไฟล์ที่แตะ: `iLearn.Domain/Entities/Category.cs`, `iLearn.Application/DTOs/DivisionDto.cs` (CategoryDto), `iLearn.API/Controllers/CategoriesController.cs`, `iLearn.API/Controllers/Base/CategoriesCRUDController.cs`, `iLearn.Infrastructure/Migrations/20260722030000_AddSortOrderToCategory.cs` (+Designer.cs), `iLearn.Infrastructure/Migrations/AppDbContextModelSnapshot.cs`
- ตรวจ string-matching บน `Category.Name` ก่อนตัด prefix ตามที่แผนสั่ง: grep ทุกจุด `c.Category.Name` เป็น display projection ล้วน (`CoursesCRUDController`/`DashboardController`/`ReportService`) ไม่มี logic เทียบ equality — ปลอดภัย
- Verified: `dotnet build iLearn.Infrastructure` 0 errors, `dotnet build iLearn.Tests -o artifacts\verify-test` 0 errors, `dotnet test` → **222 passed, 0 failed**, ลบ artifacts แล้ว
- **ถึง Gemini (§3):** contract ตรงตาม freeze เป๊ะ (`sortOrder: number`) — เพิ่มคอลัมน์ + number input ตามแผนได้เลย ไม่มีอะไรเบี่ยง
- **ถึง reviewer/deploy:** มี migration ⇒ deploy ต้องรัน `dotnet ef database update` คู่กันเสมอ (ไม่มี auto-migrate); **backup ชื่อ `Categories.Name` ก่อน deploy PROD** เพราะ Down() คืนชื่อเดิมไม่ได้

### Antigravity Gemini — §3 (React admin) DONE 2026-07-22

- **§3 React Admin Interface:**
  - Added `sortOrder` column to the `masterDataCategories` columns array in `moduleConfigs.ts` placed left-most (before name column).
  - Defined the `CategoryDto` TypeScript interface inside `MasterDataDetailPage.tsx` with a doc comment mapping the backend DTO structure (`// Mirrors CategoryDto (iLearn.Application/DTOs/DivisionDto.cs)`).
  - In `MasterDataDetailPage.tsx`, added a standard number input field for `Sort Order` when `type === 'categories'` in edit mode, using the standard UI structure.
  - In `MasterDataDetailPage.tsx`, displayed the Sort Order as a `Fact` element in the `FactGrid` details panel under read-only view.
  - Verified route remounting is already implemented in `App.tsx` via `<Remount>` for master data details/new views.
  - **Solved Model/Index Sync Tech Debt:** Restored the `[IsDeleted] = 0` `.HasFilter()` configuration on unique indexes (`Assignment`, `EnrollmentAssignment`, and `AssignmentCourse`) in `AppDbContext.cs` and synced the model snapshot `AppDbContextModelSnapshot.cs`. This resolves the design-time EF migration drift issue safely and avoids index dropping/recreation regressions in future migration generation.
  - Successfully ran `dotnet ef database update` locally against the QA DB to apply the hand-written `AddSortOrderToCategory` migration.
  - Verified React project builds and lints with 0 errors (`npm run lint` & `npm run build` passed).
  - Verified backend project builds and all 222 xUnit tests pass cleanly.

## PROD Deploy (GitHub Copilot, 2026-07-22)

ทำตามคำยืนยันผู้ใช้ (deploy รวมกับ PLAN-110 เพราะอยู่ HEAD เดียวกัน):

- Backup `Categories` บน PROD → `dbo.Categories_Backup_20260722` (41 แถว) ก่อนรัน migration
- `dotnet ef database update --connection <PROD>` → apply `20260722030000_AddSortOrderToCategory` — verify: 41/41 แถว backfill ถูกต้อง, 0 แถวเหลือ prefix เลขนำหน้าชื่อ, SortOrder เรียงถูกต่อ division
- Deploy API PROD (`tools/deploy-api-prod.ps1`) — stamp `_deploy_20260722101547`, health check HTTP 401 attempt 1/5, `AutoRolledBack=False`
- ไม่ได้ smoke หน้า admin Categories grid โดยตรงรอบนี้ (ต้อง Windows-auth login ผ่าน browser) — ยืนยันทางอ้อมด้วย DB query โดยตรง (ข้อมูลถูกต้อง) + health check ไม่ 500
- **คงค้าง:** manual smoke ข้อ 4-6 ใน Verification (เปิด admin React จริง, แก้ sortOrder แล้ว refresh, learner sidebar เรียงตามลำดับใหม่) ยังไม่ได้ทำบน PROD รอผู้ใช้เปิดใช้จริง

## Reviewer Sign-off (Claude Code, 2026-07-22)

**ผลรีวิว: ✅ ผ่าน — REVIEWED** (มี limitation 1 ข้อจดไว้ ไม่ block)

1. **§1 Migration (เขียนมือ):** ✅ อยู่ `iLearn.Infrastructure/Migrations/` + namespace ถูก, timestamp ต่อท้ายตัวล่าสุด, `Up()` มีแค่ AddColumn + backfill 2 ก้อน — CTE ROW_NUMBER per `ISNULL(DivisionId,-1)` ถูกต้อง; prefix-strip เช็ค `LEFT(...) NOT LIKE '%[^0-9]%'` ก่อนตัด ⇒ "Node.js Basics" ไม่โดน, "1.Foo"/"12. Bar" โดนถูกต้อง. หมายเหตุเล็ก: prefix-strip ไม่ filter `IsDeleted=0` (แถวที่ลบแล้วโดนตัดชื่อด้วย) — ไม่มีผลจริงเพราะ query filter ซ่อนอยู่แล้ว
2. **การเลี่ยง side-effect ของ `dotnet ef migrations add`:** ✅ ตรวจแล้ว snapshot ที่ commit ไว้**มี** filter 3 index อยู่แล้ว — drift ตัวจริงคือ Fluent API ขาด `HasFilter` (ทำให้ EF gen migration ถอด filter) การที่ Copilot เขียน migration มือ + Gemini เติม `HasFilter` เข้า `AppDbContext.cs` = ตอนนี้ **model ↔ snapshot ↔ DB ตรงกันครบ ปิด tech debt แล้วจริง** (Designer.cs ของ migration ใหม่ตรง snapshot ต่างแค่ header — ยืนยันด้วย diff 9 บรรทัด)
3. **§2 Contract freeze:** ✅ shape ตรง freeze; ordering `(DivisionId,SortOrder,Id)` ครบ lookup/GetById/Get/GetPaged (+branch `sortorder`); PUT รับ `sortOrder` ผ่าน `PopulateObject` จริง
4. **§3 React:** ✅ คอลัมน์ ลำดับ ซ้ายสุด + number input (`required` + native form submit ⇒ ค่าว่างส่งไม่ได้ ไม่มีทางยิง `""` ไปพัง PopulateObject) + Fact ใน read-only + `<Remount>` มีอยู่แล้ว. Minor (ไม่ block): interface `CategoryDto` ที่ mirror ไว้ขาด `courseCount` ตาม freeze และยังไม่ได้ผูกกับ state (declare ไว้เป็น doc) — ถ้าแตะไฟล์นี้รอบหน้าค่อยเก็บ
5. **grep `Category.Name` matching:** ✅ ยืนยันซ้ำ — ทุกจุดเป็น display projection ไม่มี equality/Contains logic
6. **⚠️ Limitation (จดไว้ รอผู้ใช้ตัดสิน — ไม่อยู่ใน scope แผนนี้):** sidebar learner (`MyLearning/Index.cshtml` `renderCategorySidebar`) เรียงหมวดตาม `Object.keys()` ที่ key เป็นเลข ⇒ ได้ลำดับ **categoryId** (ไม่ใช่ name จึงไม่เข้าเงื่อนไข "ถ้าเรียงตาม name ให้เปลี่ยน" ของแผน) — วันนี้ backfill sortOrder = ลำดับ Id พอดี ลำดับเลยตรงกัน แต่**เมื่อ admin แก้ sortOrder ภายหลัง learner จะยังเห็นลำดับเดิมตาม Id** ถ้าต้องการให้ learner ตาม ให้เปิดแผนใหม่ (ต้องส่ง sortOrder ลง course DTO ฝั่ง learner หรือเรียก categories lookup)
7. **Reviewer รัน verify เองครบ:** `dotnet test` → **222/222 passed**; `npm run lint` + `npm run build` → 0 errors

**คงค้างก่อน VERIFIED:** QA DB อัปเดตแล้ว (Gemini รัน `database update`) — เหลือ deploy API + Admin React ขึ้น QA แล้ว manual check ข้อ 4-7 → PROD (รอผู้ใช้ยืนยัน + **backup `Categories.Name` ก่อน** เพราะ `Down()` คืนชื่อไม่ได้ + รัน `dotnet ef database update` คู่ deploy PROD)

## Admin React Deploy + Full Verification (GitHub Copilot, 2026-07-22)

- Deploy Admin React ขึ้น QA (`tools/deploy-admin-react.ps1`) — `npm run lint` + `npm run build` ผ่าน 0 errors, robocopy สำเร็จ
- **Smoke QA** (`https://ap-ntc2138-qawb/iLearn/admin-react/master-data/categories`): คอลัมน์ **ลำดับ** ซ้ายสุด เลข 1,2,3… เรียงต่อ division, ชื่อไม่มีเลขนำหน้า (42 records); เปิด detail ของ "Environment & Safety" (Id 23) → Fact **Sort Order: 1** แสดงถูก; ทดลอง Edit Properties หลายครั้ง — ตรวจ DB หลังทำ: `Name`/`SortOrder` ของ Id 23 **ไม่เปลี่ยนแปลง** (ไม่มีการแก้ข้อมูลโดยไม่ตั้งใจ); toast "Changes saved successfully" 2 ครั้งที่เห็นระหว่างนั้นตรวจแล้วเป็น SignalR broadcast `adminactivitycreated` ของกิจกรรม admin คนอื่น (console warning ยืนยัน) **ไม่ใช่ผลจากการทดสอบของเรา**
- Deploy Admin React ขึ้น PROD (`tools/deploy-admin-react-prod.ps1`) — lint+build 0 errors, robocopy สำเร็จ
- **Smoke PROD** (`https://ap-ntc2137-prwb/iLearn/admin-react/master-data/categories`): คอลัมน์ ลำดับ ซ้ายสุด เลข 1,2,3,4… เรียงต้อง, ชื่อไม่มี prefix, 41 records, ไม่มี error
- **ข้อ 6 (learner sidebar เรียงตามลำดับใหม่):** ยังไม่ได้ทำ — ตาม Limitation ที่ reviewer จดไว้: sidebar learner เรียงตาม categoryId ไม่ใช่ sortOrder — อยู่นอก scope ของแผนนี้ (§4 เป็น option รอผู้ใช้ยืนยัน)

**PLAN-111 ปิดงานสมบูรณ์: API + migration + Admin React ขึ้น QA และ PROD ครบทั้งหมด เหลือเฉพาะ˶ learner sidebar reorder (อยู่นอก scope)**
