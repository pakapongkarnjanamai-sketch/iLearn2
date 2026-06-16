# iLearn.API — Style Guide (มาตรฐานกลาง)

มาตรฐานการเขียน API ฝั่ง backend เพื่อลด fragmentation (หลาย route convention + response shape ปนกัน) — ใช้เป็นเกณฑ์เป้าหมายของงาน refactor ทุกตัว (ของใหม่ทำตามนี้, ของเก่าทยอย migrate)

> เขียนโดย Claude Code (planner) 2026-06-15 จาก pattern ที่มีอยู่จริง — เป็น **convention เป้าหมาย** ไม่ใช่สั่งให้ rewrite ทั้งหมดทันที

---

## 1. Routing

| ประเภท controller | route | ตัวอย่าง |
|---|---|---|
| Feature / REST | `api/[controller]` | `api/Courses`, `api/Assignments`, `api/Learners` |
| Admin CRUD (สืบทอด `GenericController<T>`) | `api/admin/[controller]` (ชื่อลงท้าย `CRUD`) + verb `Get`/`Post`/`Put`/`Delete` (form-encoded) | `api/admin/CoursesCRUD/Get` |
| Admin-only feature | `api/admin/[controller]` | `api/admin/Dashboard`, `api/admin/SystemConfig` |
| Sub-resource | `api/[controller]/{id}/<sub>` | `api/Courses/{id}/versions` |
| Action/command | `api/[controller]/<verb>` | `api/Assignments/bulk`, `api/Enrollments/ResetStatus` |
| Learner-facing (HMAC) | `api/[controller]/<verb>` + `[AllowAnonymous]` + resolver | `api/LearningLogs/commit-runtime` |

**กฎ:** path ใช้ noun พหูพจน์ (controller name); action เป็น kebab/verb ชัดเจน; อย่าผสม REST item route กับ DevExtreme CRUD ใน controller เดียวกันโดยไม่จำเป็น

---

## 2. Authentication / Authorization

- ทุก controller/action มี `[Authorize(Policy = "...")]` ชัดเจน — policy: `AdminOnly`, `SuperAdminOnly`, `ManagerOrAbove`, `UserOrAbove`, `DomainUser`
- `FallbackPolicy = DefaultPolicy` → secure-by-default (ไม่ใส่ = ยังต้อง auth)
- Learner endpoint: `[AllowAnonymous]` ต่อ Windows auth **แต่ต้องเรียก `ILearnerProxyIdentityResolver` (HMAC) เสมอ** ก่อนทำงาน — ห้ามมี anonymous endpoint ที่ไม่ verify HMAC
- auth อยู่ที่ **controller** เท่านั้น — อย่าย้าย auth check ลง service

---

## 3. Response envelope (มาตรฐาน)

| กรณี | ใช้อะไร |
|---|---|
| REST/feature endpoint คืน object/collection | **`ApiResponse<T>`** (`iLearn.Domain/Common/ApiResponse.cs`) = `{ success, message, data, errorCode }` |
| DevExtreme grid (`Get` ของ CRUD) | ผล `DataSourceLoader.Load(...)` = `{ data, totalCount, summary, groupCount }` (อย่าห่อ ApiResponse ทับ) |
| ไม่มี payload (delete/command สำเร็จ) | `Ok()` หรือ `ApiResponse<bool>{ success = true }` |

**ห้าม:** `return Ok(new { success = true, data = ... })` (anonymous object) → OpenAPI generate type ไม่ได้ + React ต้องเดา shape → **ใช้ `ApiResponse<T>` หรือ DTO record แทน**

ทุก field serialize เป็น **camelCase** (default System.Text.Json) — DTO ตั้งชื่อ PascalCase ฝั่ง C#, React อ่าน camelCase

---

## 4. Error handling

- **โยน exception** ให้ `GlobalExceptionMiddleware` แปลงเป็น `ProblemDetails` — อย่า catch แล้วคืน error object เอง, อย่า `catch { return null }` (กลืน)
- mapping ปัจจุบัน: `KeyNotFound`→404, `UnauthorizedAccess`→403, `ArgumentException`→400, `InvalidOperationException`→409, `HttpRequestException`→502, อื่น→500
- ถ้าต้อง error เฉพาะทาง → throw typed exception (เพิ่ม mapping ใน middleware) ไม่ใช่ status code ดิบใน controller

---

## 5. DTO / Contract

- response ใช้ **typed DTO/record** ใน `iLearn.Application/DTOs/` — ไม่มี anonymous projection ใน response สุดท้าย (projection ใน LINQ ระหว่างทางได้ แต่ map เป็น DTO ก่อนคืน)
- ⚠️ EF: **อย่า project เข้า DTO record constructor ใน IQueryable** (โดยเฉพาะ `GroupBy().Select(new Dto(...))`) — SQL แปลงไม่ได้ ให้ project เป็น anonymous ใน SQL แล้ว map เป็น DTO **หลัง** `ToListAsync` (บทเรียนจาก Dashboard)
- React mirror type ต้องมีคอมเมนต์ `// Mirrors <DtoName> (<path>)`

---

## 6. Controller thinness

- controller = **orchestration**: รับ input → เรียก Application service → map → คืน response + auth
- business logic / query / กฎ อยู่ใน `iLearn.Application/Services/` (มี interface + register DI)
- หลีกเลี่ยง inject `IGenericRepository<T>` ดิบหลายตัวใน controller (สัญญาณว่า logic ควรอยู่ใน service)

---

## 7. Data isolation (Division)

- **List:** `if (_currentUser.DivisionId.HasValue) query.Where(x => x.DivisionId == value)`
- **Create:** `DivisionId = IsSuperAdmin ? dto.DivisionId : _currentUser.DivisionId` (กัน escalation)
- **Get/Update/Delete:** ownership check → 403/404 ถ้า division ไม่ตรง
- (ดู `DOC/division_isolation_analysis.md`)

---

## 8. สถานะปัจจุบัน (gap)

- response: ~8 controller ยังใช้ `Ok(new {...})` anonymous, ~5 ใช้ ApiResponse → ทยอย migrate
- controller ใหญ่ (Assignments 1316, ContentItems 1179) logic ยังปนใน controller → ทยอย refactor (ดู PLAN-032+)
- route: ส่วนใหญ่ตาม §1 แล้ว มีบาง special case (`api/admin/session`)

> งาน refactor แต่ละตัวให้ยึด guide นี้ + เป็น **pure refactor** (ไม่เปลี่ยน shape ที่ React อ่าน) ใช้ `dotnet test` + contract grep เป็นตาข่าย
