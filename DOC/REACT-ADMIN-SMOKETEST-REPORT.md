# iLearn Admin React — Smoke Test Report & Improvement Plan

> **Date:** 2026-06-12  
> **Scope:** Full smoke test of `iLearn.Admin.React` (http://localhost:5173/) — all routes  
> **Method:** Code-level analysis + live API timing (curl + Playwright)

---

## Table of Contents

- [1. Executive Summary](#1-executive-summary)
- [2. Issues Found](#2-issues-found)
  - [2.1 Critical — Content Library Performance](#21-critical--content-library-performance)
  - [2.2 High — Dashboard API Performance](#22-high--dashboard-api-performance)
  - [2.3 High — Learners Page Data Mismatch](#23-high--learners-page-data-mismatch)
  - [2.4 High — Master Data CRUD Broken](#24-high--master-data-crud-broken)
  - [2.5 Medium — ID Columns Without Lookup Labels](#25-medium--id-columns-without-lookup-labels)
  - [2.6 Medium — Loading UX / Error Boundary](#26-medium--loading-ux--error-boundary)
  - [2.7 Low — Miscellaneous UI/UX](#27-low--miscellaneous-uiux)
- [3. Improvement Plan](#3-improvement-plan)
  - [Phase 1: Fix Critical & High Issues](#phase-1-fix-critical--high-issues)
  - [Phase 2: Fix Medium Issues](#phase-2-fix-medium-issues)
  - [Phase 3: Polish & Hardening](#phase-3-polish--hardening)
- [4. Detailed Implementation Notes](#4-detailed-implementation-notes)
- [5. Testing Checklist](#5-testing-checklist)

---

## 1. Executive Summary

Smoke test ของ iLearn Admin React พบ **14 ปัญหา** แบ่งเป็น:

| ระดับ | จำนวน | สถานะ |
|---|---|---|
| **วิกฤต (Critical)** | 1 | ต้องแก้ไขทันที |
| **สูง (High)** | 5 | ต้องแก้ไขก่อน production |
| **ปานกลาง (Medium)** | 5 | ควรแก้ไขภายใน sprint ถัดไป |
| **ต่ำ (Low)** | 3 | Nice-to-have / polish |

**ปัญหาวิกฤตที่สุด:** `GET /api/ContentItems/paged` ใช้เวลา **17–21 วินาที** ต่อ request ทำให้หน้า Content Library ใช้งานไม่ได้จริง

---

## 2. Issues Found

### 2.1 Critical — Content Library Performance

| ID | C1 |
|---|---|
| **Route** | `/content-library` |
| **API** | `GET /api/ContentItems/paged?page=1&pageSize=13` |
| **Measured** | **17–21 วินาที** (curl direct, ไม่ใช่ปัญหา frontend) |
| **เปรียบเทียบ** | `GET /api/ContentItems` (GetAll ไม่มี projection) = **63ms** |

**Root Cause — 2 จุดใน EF Core Projection:**

**จุดที่ 1: LEFT JOIN ไปตาราง blob**
```csharp
// ContentItemsController.cs line 139
FileLength = r.FileStorage != null ? r.FileStorage.Length : 0,
```
- `FileStorage` มี column `Data` (`varbinary(max)`) ซึ่งเก็บ SCORM ZIP ไฟล์
- แม้ projection เลือกแค่ `Length` (long) แต่ EF Core สร้าง LEFT JOIN ไปตาราง `FileStorages`
- SQL Server ต้องอ่าน data pages ของตารางที่ row ใหญ่มาก (blob) → I/O สูง

**จุดที่ 2: Correlated Subquery**
```csharp
// ContentItemsController.cs line 142-147
CourseIdsCount = r.CourseContentItems
    .Where(cr => cr.CourseVersion != null)
    .Select(cr => cr.CourseVersion!.CourseId)
    .Distinct()
    .Count(),
```
- EF Core translate เป็น correlated subquery ที่ JOIN `CourseContentItems` → `CourseVersions` → `COUNT(DISTINCT CourseId)` **ซ้ำทุก row**

**จุดที่ 3: Double round-trip**
- `CountAsync()` แยก 1 query + `ToListAsync()` อีก 1 query = **2 round-trip** ทั้งคู่ช้า

**จุดอื่นที่มีปัญหาเดียวกัน:**

| Endpoint | Location | ความเสี่ยง |
|---|---|---|
| `ContentItemsCRUD/Get` (Legacy MVC) | `ContentItemsCRUDController.GetFiltered()` | วิกฤตเท่ากัน |
| `ContentItemsCRUD/GetByCourse` | ผ่าน `GetFiltered()` + WHERE | สูง |
| `ContentItemsCRUD/Get/{id}` | Same pattern แต่ 1 row | ต่ำ |

---

### 2.2 High — Dashboard API Performance

| ID | D1, D2 |
|---|---|
| **Route** | `/` (Dashboard) |
| **API** | `GET /api/admin/Dashboard/Overview` |

**ปัญหา:**

1. **8+ sequential COUNT queries** — `DashboardController.GetOverview()` ทำ `CountAsync()` แยกกัน 7+ ครั้งติดต่อกัน:
   - `activeCourses`, `draftCourses`, `newCourses`, `contentItemCount`, `learnerGroupCount`, `learningSessionsLast30`, `learningSessionsPrevious30`
   - แต่ละครั้งเป็น round-trip ไป DB

2. **In-memory aggregation บน full dataset** — ดึง `assignmentRows` (ทุก assignment) + `taskRows` (ทุก enrollment-assignment link) มา memory ก่อน แล้ว group/count ใน C#
   - ถ้ามี 10K+ assignments + 50K+ tasks จะกินหน่วยความจำมาก
   - ไม่มี caching — ทุก request คำนวณใหม่ทั้งหมด

3. **ไม่ cancel polling เมื่อ SignalR ใช้ได้** — Dashboard poll `MaintenanceStatus` ทุก 15 วินาที + `RecentAdminActivities` ทุก 60 วินาที แม้ SignalR จะ connected อยู่

---

### 2.3 High — Learners Page Data Mismatch

| ID | L2, L3 |
|---|---|
| **Route** | `/learners` |
| **API** | `GET /api/Learners/Get` (proxy ไป external employee API) |

**ปัญหา:**

1. **Key field casing** — `moduleConfigs.ts` ตั้ง `key: 'Id'` (PascalCase) แต่ `LearnersController.Get()` ใช้ `Content(resultJson, "application/json")` ส่ง raw JSON จาก external service **โดยไม่ผ่าน ASP.NET JSON serializer** → field casing ขึ้นกับ external service

2. **Column field casing** — Columns ใช้ PascalCase: `EId`, `NID`, `EnglishFirstName`, `EnglishLastName`, `Division`, `Department`, `Section`, `Position` → ต้องตรงกับ external API response format

3. **ผลกระทบ:** ถ้า casing ไม่ตรง → ทุกคอลัมน์จะว่างเปล่า และ infinite scroll deduplication จะพัง (เพราะ key ไม่ match)

---

### 2.4 High — Master Data CRUD Broken

| ID | M1, M2 |
|---|---|
| **Routes** | `/master-data/divisions`, `/master-data/categories`, `/master-data/course-types`, `/master-data/roles` |

**ปัญหา 2 จุด:**

1. **Create button links ไปหน้าที่ไม่มี** — ปุ่ม "Create Division" link ไป `/master-data/divisions/new` แต่ `App.tsx` ไม่มี route สำหรับ `/master-data/*/new` → 404

2. **CRUD ไม่ enable** — `EntityListPage` มี `crudControllers` set ที่มีแค่ `'LearnerGroupsCRUD'`:
   ```tsx
   const crudControllers = new Set(['LearnerGroupsCRUD'])
   ```
   Master data controllers (`DivisionsCRUD`, `CategoriesCRUD`, `CourseTypesCRUD`, `RolesCRUD`) ไม่อยู่ใน set → `enableCrud: false` → ไม่มี insert/update/delete handlers → inline edit ไม่ทำงาน

---

### 2.5 Medium — ID Columns Without Lookup Labels

| ID | X1, C2, A2, CO1 |
|---|---|
| **Routes** | ทุกหน้า list ที่มี FK columns |

**ปัญหา:** คอลัมน์ที่เป็น foreign key ID แสดงเป็นตัวเลขดิบ:

| Column | แสดงเป็น | ควรแสดง |
|---|---|---|
| `typeId` (Content Library) | `1` / `2` | "Learn" / "Exam" |
| `courseId` (Assignments) | `42` | "SAFE-001 — Basic Safety" |
| `courseTypeId` (Courses) | `3` | "Mandatory" |
| `categoryId` (Courses) | `5` | "Safety & Environment" |
| `divisionId` (Learner Groups) | `1` | "Operations" |

ผู้ใช้ต้องจำ ID number ซึ่งใช้งานไม่ได้จริง

---

### 2.6 Medium — Loading UX / Error Boundary

| ID | D4, C3, X2 |
|---|---|

1. **ไม่มี Error Boundary** — ถ้า recharts ได้ data ผิดรูป (null value ใน pie chart) จะ crash ทั้ง Dashboard page โดยไม่มี fallback UI

2. **"No records found" flash** — `AppTable` แสดง "No records found" เมื่อ `data.length === 0 && !loading` แต่ initial render อาจ flash ข้อความนี้ก่อนที่ loading state จะ set เป็น true (race condition ใน `useEffect` timing)

3. **ไม่มี skeleton loader** — ระหว่างรอ API (โดยเฉพาะ Content Library 21 วินาที) ผู้ใช้เห็นแค่หน้าว่าง + spinner เล็ก ๆ

---

### 2.7 Low — Miscellaneous UI/UX

| ID | ปัญหา |
|---|---|
| **X3** | `AppTable` auto-pageSize อาจได้ค่าเล็กมาก (5) บน notebook → ทำให้ต้อง load หลาย page ติดต่อกัน |
| **X4** | Sidebar มี "Access Control" link ไปหน้า `/access-denied` (403 page) — ไม่ควรอยู่ใน nav |
| **D3** | Dashboard polling ไม่ cancel เมื่อ SignalR connected → request ซ้ำซ้อน |

---

## 3. Improvement Plan

### Phase 1: Fix Critical & High Issues

> **เป้าหมาย:** ทุกหน้าหลักใช้งานได้ ไม่มีปัญหา performance วิกฤต  
> **ขอบเขต:** Backend performance fix + Frontend data binding fix

#### Task 1.1 — Fix ContentItems/paged Performance (C1)

**Files:**
- `iLearn.API/Controllers/ContentItemsController.cs`
- `iLearn.API/Controllers/Base/ContentItemsCRUDController.cs`
- `iLearn.Domain/Entities/ContentItem.cs` (ถ้าเพิ่ม column)
- EF Migration (ถ้าเพิ่ม column)

**แนวทาง (เลือก 1):**

| Option | วิธี | ข้อดี | ข้อเสีย |
|---|---|---|---|
| **A: Denormalize `FileLength`** | เพิ่ม `CachedFileLength` column ลงใน `ContentItem` entity โดยตรง, populate ตอน upload/update | ลบ LEFT JOIN blob ได้เลย, query เร็วสุด | ต้อง migration + backfill data |
| **B: ลบ `FileLength` ออก** | ไม่แสดง file size ใน grid list (แสดงเฉพาะ detail page) | ไม่ต้อง migration | เสีย feature |
| **C: แยกตาราง blob** | สร้าง `FileStorageData` table แยก `Data` column ออก | แก้ root cause ของ table scan | Migration ซับซ้อน, กระทบ code เยอะ |

**แนะนำ: Option A** — เพิ่ม `CachedFileLength` column ลง `ContentItem`:
1. เพิ่ม `public long? CachedFileLength { get; set; }` ใน `ContentItem` entity
2. สร้าง EF Migration
3. Backfill: `UPDATE ContentItems SET CachedFileLength = fs.[Length] FROM FileStorages fs WHERE ContentItems.FileStorageId = fs.Id`
4. แก้ projection ใน `ContentItemsController.GetPaged()` เป็น `FileLength = r.CachedFileLength ?? 0`
5. แก้ `ContentItemsCRUDController.GetFiltered()` เหมือนกัน
6. แก้ upload flow ให้ set `CachedFileLength` ตอน save

**แก้ `CourseIdsCount`:**
1. เปลี่ยนจาก correlated subquery เป็น separate query:
   ```csharp
   // Query paged IDs first
   var pagedIds = await query.Skip(...).Take(...).Select(r => r.Id).ToListAsync();
   
   // Separate batch query for course counts
   var courseCountMap = await dbContext.CourseContentItems
       .Where(cr => pagedIds.Contains(cr.ContentItemId) && cr.CourseVersion != null)
       .Select(cr => new { cr.ContentItemId, cr.CourseVersion!.CourseId })
       .Distinct()
       .GroupBy(x => x.ContentItemId)
       .ToDictionaryAsync(g => g.Key, g => g.Count());
   ```
2. Map `CourseIdsCount` ใน memory หลัง query เสร็จ

**เป้าหมาย:** ลดจาก 17-21s → **< 500ms**

---

#### Task 1.2 — Add Dashboard Overview Caching (D1, D2)

**Files:**
- `iLearn.API/Controllers/DashboardController.cs`

**แนวทาง:**
1. เพิ่ม `IMemoryCache` injection (มีอยู่แล้วใน DI)
2. Cache `Overview` response ไว้ **60 วินาที** per division scope
3. Cache key: `$"dashboard:overview:{divisionId ?? "global"}"`
4. รวม multiple COUNT queries เป็น single query ด้วย conditional aggregation:
   ```csharp
   var stats = await coursesQuery
       .GroupBy(_ => 1)
       .Select(g => new {
           Active = g.Count(c => c.Status == CourseStatus.Open),
           Draft = g.Count(c => c.Status == CourseStatus.Draft),
           New = g.Count(c => c.CreatedAt >= recentWindowStart)
       })
       .FirstOrDefaultAsync(cancellationToken);
   ```
5. ย้าย task aggregation ไป SQL ด้วย `GROUP BY AssignmentId` แทนดึงทุก row มา memory

**เป้าหมาย:** ลด round-trip จาก 8+ → **2-3**, เพิ่ม cache 60s

---

#### Task 1.3 — Fix Learners Page Key/Column Casing (L2, L3)

**Files:**
- `iLearn.Admin.React/src/pages/moduleConfigs.ts`
- `iLearn.API/Controllers/LearnersController.cs`

**แนวทาง (เลือก 1):**

| Option | วิธี |
|---|---|
| **A: แก้ API** | Parse JSON จาก external service แล้ว re-serialize ด้วย ASP.NET camelCase policy ก่อน return |
| **B: แก้ Frontend** | ตรวจสอบ actual casing จาก external API response แล้วแก้ `moduleConfigs.ts` ให้ match |

**แนะนำ: Option A** — ให้ API normalize casing:
1. `LearnersController.Get()` → parse JSON → deserialize เป็น DTO array → return `Ok(data)`
2. ASP.NET JSON camelCase policy จะ normalize ให้อัตโนมัติ
3. แก้ `moduleConfigs.ts` ให้ใช้ camelCase: `eId`, `nid`, `englishFirstName`, etc.
4. แก้ `key: 'Id'` → `key: 'id'`

---

#### Task 1.4 — Fix Master Data CRUD (M1, M2)

**Files:**
- `iLearn.Admin.React/src/pages/EntityListPage.tsx`

**แนวทาง:**
1. เพิ่ม master data controllers เข้า `crudControllers` set:
   ```tsx
   const crudControllers = new Set([
     'LearnerGroupsCRUD',
     'DivisionsCRUD',
     'CategoriesCRUD',
     'CourseTypesCRUD',
     'RolesCRUD',
   ])
   ```
2. ลบ "Create" button/link สำหรับ master data (ใช้ inline create แทน — ไม่ต้องมี route `/new`)
3. หรือเปลี่ยน create button เป็น inline add-row trigger ของ `AppTable`

---

#### Task 1.5 — Verify Assignment Division Column (A1)

**Files:**
- `iLearn.Admin.React/src/pages/moduleConfigs.ts`

**แนวทาง:**
1. ตรวจสอบ actual field name จาก `vw_AssignmentList` SQL view
2. แก้ `dataField: 'division'` ให้ตรงกับ actual column name (อาจเป็น `divisionName` หรือ `division`)
3. ทดสอบว่าคอลัมน์แสดงข้อมูลถูกต้อง

---

### Phase 2: Fix Medium Issues

> **เป้าหมาย:** UX ที่ดีขึ้น — ผู้ใช้เข้าใจข้อมูลทุก column

#### Task 2.1 — Add Lookup Labels for FK Columns (X1, C2, A2)

**Files:**
- `iLearn.Admin.React/src/pages/moduleConfigs.ts`
- `iLearn.Admin.React/src/pages/EntityListPage.tsx`

**แนวทาง:**

**Content Library — `typeId`:**
```tsx
{
  dataField: 'typeId',
  caption: 'Content Type',
  cellRender: ({ value }) => value === 1 ? 'Learn' : value === 2 ? 'Exam' : '—'
}
```

**Courses — `courseTypeId` / `categoryId`:**
- Fetch lookup data จาก `admin/CourseTypesCRUD/Get` และ `admin/CategoriesCRUD/Get` ตอน page load
- Map ID → name ใน `cellRender`

**Assignments — `courseId`:**
- Fetch course lookup จาก `admin/CoursesCRUD/GetForLookup`
- Map ID → `code — title`

**Learner Groups — `divisionId` / `categoryId`:**
- เหมือนกัน — fetch lookup แล้ว map

---

#### Task 2.2 — Add Error Boundary for Dashboard Charts (D4)

**Files:**
- `iLearn.Admin.React/src/components/ui/ErrorBoundary.tsx` (สร้างใหม่)
- `iLearn.Admin.React/src/pages/DashboardPage.tsx`

**แนวทาง:**
1. สร้าง `ErrorBoundary` component (React class component with `componentDidCatch`)
2. Wrap แต่ละ chart section ด้วย `<ErrorBoundary fallback={<EmptyRow label="Chart failed to render" />}>`
3. Log error เพื่อ debug

---

#### Task 2.3 — Improve Loading UX (C3, X2)

**Files:**
- `iLearn.Admin.React/src/components/ui/AppTable.tsx`

**แนวทาง:**
1. แก้ initial state: ให้ `loading = true` ตั้งแต่ mount (ป้องกัน "No records" flash)
2. แสดง skeleton rows (3-5 animated rows) แทนข้อความ "No records found" ระหว่าง initial load
3. แสดง "No records found" **เฉพาะเมื่อ** loading เสร็จแล้วจริง ๆ และอยู่ที่ page 1:
   ```tsx
   {data.length === 0 && !loading && page === 1 ? (
     <tr>...</tr>  // "No records found"
   ) : null}
   ```

---

### Phase 3: Polish & Hardening

> **เป้าหมาย:** Production-ready polish

#### Task 3.1 — AppTable Minimum PageSize Guard (X3)

**Files:**
- `iLearn.Admin.React/src/components/ui/AppTable.tsx`

**แนวทาง:**
- เพิ่ม minimum pageSize guard: `Math.max(10, calculatedPageSize)`
- ป้องกัน pageSize=5 ที่ทำให้ auto-load หลาย page ติดต่อกัน

---

#### Task 3.2 — Remove "Access Control" from Sidebar (X4)

**Files:**
- `iLearn.Admin.React/src/config/navigation.ts`

**แนวทาง:**
- ลบ `{ label: 'Access Control', path: '/access-denied', icon: ShieldCheck, superAdminOnly: true }` ออกจาก `navigationItems`
- หน้า `/access-denied` ยังคงอยู่เป็น fallback route สำหรับ unauthorized redirect

---

#### Task 3.3 — Cancel Dashboard Polling When SignalR Connected (D3)

**Files:**
- `iLearn.Admin.React/src/pages/DashboardPage.tsx`

**แนวทาง:**
- Track SignalR connection state ใน `useRef`
- ใน polling interval callback ให้ check: ถ้า connected อยู่ → skip fetch

---

## 4. Detailed Implementation Notes

### ContentItem FileLength Denormalization (Task 1.1)

```
Migration steps:
1. เพิ่ม property ใน ContentItem.cs:
   public long? CachedFileLength { get; set; }

2. สร้าง migration:
   dotnet ef migrations add AddCachedFileLengthToContentItem -p iLearn.Infrastructure -s iLearn.API

3. Apply migration

4. Run backfill SQL:
   UPDATE ci SET ci.CachedFileLength = fs.[Length]
   FROM ContentItems ci
   INNER JOIN FileStorages fs ON ci.FileStorageId = fs.Id
   WHERE ci.FileStorageId IS NOT NULL

5. Update upload endpoints to set CachedFileLength on save:
   - ContentItemsController.Upload()
   - ContentItemsController.ReprocessScorm()

6. Update projections:
   - ContentItemsController.GetPaged() → FileLength = r.CachedFileLength ?? 0
   - ContentItemsCRUDController.GetFiltered() → fileLength = r.CachedFileLength ?? 0
   - ContentItemsCRUDController.Get(int id) → FileLength = r.CachedFileLength ?? 0

7. Update MappingExtensions.ToDto() → FileLength = entity.CachedFileLength ?? 0

8. Run existing tests:
   - ContentItemsControllerTests
   - ContentItemsCrudControllerTests
   - ContentPublicationServiceTests
```

### Dashboard Caching (Task 1.2)

```
Implementation:
1. DashboardController already has IMemoryCache available (via DI)
2. Wrap GetOverview body:
   var cacheKey = $"dashboard:overview:{_currentUser.DivisionId?.ToString() ?? "global"}";
   var result = await _cache.GetOrCreateAsync(cacheKey, async entry => {
       entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(60);
       // ... existing query logic ...
       return responseObject;
   });
   return Ok(result);

3. Consolidate COUNT queries:
   Replace 7 separate CountAsync() calls with 1-2 grouped queries

4. Manual invalidation (optional):
   POST admin/Dashboard/RefreshCache → _cache.Remove(cacheKey)
```

### Master Data Inline CRUD Fix (Task 1.4)

```
Current state:
  - EntityListPage checks crudControllers set
  - Only 'LearnerGroupsCRUD' is in the set
  - Master data controllers use createAdminDataSource (not REST)
  - createAdminDataSource supports CRUD via /Post, /Put, /Delete

Fix:
  - Add master data controller names to crudControllers set
  - Remove "Create X" button/link for master data
    (inline AppTable add-row is the correct UX per copilot-instructions.md Section 14)
  - Verify AppTable inline edit mode works with createAdminDataSource CRUD handlers
```

---

## 5. Testing Checklist

### Phase 1 Tests

- [ ] `GET /api/ContentItems/paged?page=1&pageSize=20` responds in < 500ms
- [ ] `GET /api/admin/ContentItemsCRUD/Get` responds in < 1s
- [ ] Content Library grid loads data within 2 seconds
- [ ] Dashboard loads within 3 seconds
- [ ] Dashboard refresh (F5) does not re-query DB within 60s cache window
- [ ] Learners page shows all columns with correct data
- [ ] Learners page infinite scroll works (no duplicate rows)
- [ ] Master Data Divisions page allows inline create/edit/delete
- [ ] Master Data Categories page allows inline create/edit/delete
- [ ] Master Data Course Types page allows inline create/edit/delete
- [ ] Master Data Roles page allows inline create/edit/delete
- [ ] Assignment list shows division name (not empty column)
- [ ] Existing tests pass: `ContentItemsControllerTests`, `ContentItemsCrudControllerTests`, `CoursesCrudControllerTests`

### Phase 2 Tests

- [ ] Content Library shows "Learn" / "Exam" instead of 1 / 2
- [ ] Courses list shows course type name and category name
- [ ] Assignments list shows course name instead of course ID
- [ ] Learner Groups list shows division name instead of division ID
- [ ] Dashboard chart with null/malformed data does not crash page
- [ ] Initial page load shows skeleton instead of "No records found" flash
- [ ] AppTable shows "No records found" only after load completes with 0 results

### Phase 3 Tests

- [ ] AppTable pageSize is never less than 10
- [ ] Sidebar does not show "Access Control" link
- [ ] Dashboard does not poll when SignalR is connected
- [ ] All routes in the Route Registry (Section 17) are reachable without 404

---

## Appendix: Measured API Response Times

| Endpoint | Method | Time | Notes |
|---|---|---|---|
| `/api/ContentItems/paged?page=1&pageSize=13` | GET | **17–21s** | วิกฤต — blob JOIN + correlated subquery |
| `/api/ContentItems` | GET | 63ms | GetAll ไม่มี projection — baseline |
| `/api/admin/session/me` | GET | ~100ms | ปกติ |
| `/api/admin/Dashboard/Overview` | GET | ยังไม่ได้วัด | คาดว่า 2-5s จาก 8+ COUNT queries |
| `/api/Learners/Get` | GET | ขึ้นกับ external API | ไม่ควบคุมได้ — proxy pattern |
