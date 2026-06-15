# API Analysis: iLearn.API

วิเคราะห์ backend (.NET 9, Clean Architecture) ฉบับรวม — สถาปัตยกรรม, surface ของ endpoint, โมเดล auth, pattern ที่ใช้ซ้ำ, และ **ความเสี่ยง/ข้อสังเกตเรียงตามลำดับความสำคัญ** สำหรับใช้อ้างอิงและออกแผนแก้ไข

> เขียนโดย Claude Code (planner/reviewer) 2026-06-15 จากการอ่านโค้ดจริง — ตัวเลขบรรทัด/ชื่อไฟล์อาจขยับได้ ให้ยืนยันก่อนใช้

---

## 1. สถาปัตยกรรม (Clean Architecture)

| Layer | โปรเจกต์ | หน้าที่ |
|---|---|---|
| Presentation | `iLearn.API` | Controllers, SignalR hub, middleware, auth, DI composition |
| Application | `iLearn.Application` | Services (business logic), DTOs, interfaces, common policies |
| Domain | `iLearn.Domain` | Entities, enums, `BaseEntity`, `ApiResponse<T>` |
| Infrastructure | `iLearn.Infrastructure` | EF Core, repositories, external service clients (`LearnerApiService`) |

**จุดแข็งของ host setup** (`iLearn.API/Program.cs`):
- แยก concern เป็น extension methods (`AddPresentation`, `AddApiAuthentication/Authorization`, `AddApiSwagger`, `AddApplication`, `AddInfrastructure`, `AddApiCors`) — อ่านง่าย เทสง่าย
- `ValidateRequiredSecrets` ตรวจ connection string + learner proxy secret ตอน boot (fail fast)
- `GlobalExceptionMiddleware` แปลง exception → `ProblemDetails` + กัน log-forging (CWE-117) + ซ่อน detail นอก Development
- `app.ValidateExplicitControllerAuthorizationPolicies()` ตรวจว่า policy ที่ controller อ้างถึงมีจริงตอน startup
- `FallbackPolicy = DefaultPolicy` → **secure by default** (ทุก endpoint ต้อง auth เว้นแต่ `[AllowAnonymous]`)

---

## 2. โมเดล Authentication / Authorization

**Authentication:** Windows Authentication (Negotiate/NTLM) + claims enrichment (`ApiClaimsEnrichMiddleware`)

**Policies** (`iLearn.API/Extensions/AuthorizationExtensions.cs`):
| Policy | เงื่อนไข |
|---|---|
| `AdminOnly` | role Admin หรือ SuperAdmin |
| `SuperAdminOnly` | role SuperAdmin |
| `ManagerOrAbove` | Manager/Admin/SuperAdmin |
| `UserOrAbove` | User/Manager/Admin/SuperAdmin |
| `DomainUser` | `Identity.Name` ขึ้นต้นด้วย domain prefix (config `Authentication:DomainPrefix`, default `NIKONOA\`) |

**Learner proxy (HMAC):** endpoint ฝั่งผู้เรียน (`[AllowAnonymous]` ต่อ Windows auth) ป้องกันด้วยลายเซ็น HMAC-SHA256 ผ่าน `LearnerProxyIdentityResolver` — ตรวจ header `X-iLearn-Learner-{Code,Timestamp,Signature}`, ลงนาม `code\ntimestamp\nMETHOD\npath`, ใช้ `CryptographicOperations.FixedTimeEquals` (constant-time), timestamp tolerance 300s ยืนยันแล้วว่า endpoint anonymous ทุกตัวเรียก resolver จริงก่อนทำงาน (เช่น `LearningLogsController.UpdateProgress/CommitRuntime`) — **ออกแบบดี**

---

## 3. Surface ของ Endpoint (33 controllers)

### 3.1 Generic CRUD (`Controllers/Base/`, สืบทอด `GenericController<T>`)
route `api/admin/{Controller}` — verb: `Get`, `Get/{id}`, `Post`, `Put`, `Delete` (FromForm `values`/`key`), policy default `AdminOnly`

CoursesCRUD, ContentItemsCRUD, AssignmentsCRUD, CategoriesCRUD, CourseTypesCRUD, DivisionsCRUD, RolesCRUD, UsersCRUD (override `SuperAdminOnly` + custom Get/Put), UserRolesCRUD, EnrollmentsCRUD, LearningLogsCRUD, LearnerGroupsCRUD, CourseVersionsCRUD, CourseContentItemsCRUD, **FileStoragesCRUD (⚠ ดูข้อ 5.1)**

### 3.2 Admin feature controllers (`api/[controller]` หรือ `api/admin/[controller]`)
- **AssignmentsController** (1316 บรรทัด) — batch assign, gantt, report, conflict, history
- **ContentItemsController** (1179) — SCORM upload/extract, publish/unpublish, player launch, runtime
- **DashboardController** (766) — KPI + charts + live activity (SignalR)
- **CoursesController** (622) — catalog, version lifecycle, readiness
- **EnrollmentsController** (624) — ledger, reset, learner-facing progress
- LearnerGroupsController, LearnerGroupCategoriesController, ContentLibraryController, CategoriesController, DivisionsController, RolesController, UsersController, LearnersController, SessionController, CacheController, SystemConfigController

### 3.3 Learner-facing (`[AllowAnonymous]` + HMAC)
- `LearningLogsController` — `update-progress`, `commit-runtime`, runtime read (ทั้ง controller anonymous + HMAC)
- `EnrollmentsController` — progress endpoints บางตัว
- `LearnersController.GetLearnerbyEID`, `DivisionsController` (lookup ตัวหนึ่ง)

---

## 4. Pattern ที่ใช้ซ้ำ (cross-cutting)

1. **DevExtreme `DataSourceLoader`** — CRUD `Get` คืน `DataSourceLoader.Load(data, loadOptions)` รับ skip/take/filter/sort จาก query string ฝั่ง React จำลองผ่าน `createAdminDataSource`
2. **External employee proxy** — `LearnerApiService` forward query string ดิบไป external HR API (`EmployeeServiceV2`)
3. **Anonymous-object responses** — controller หลายตัวคืน `Ok(new {...})` → OpenAPI generate type ไม่ได้ (React ต้องลอก shape มือ — ตาม CLAUDE.md)
4. **MemoryCache** — ใช้ cache employee directory (24h) + dashboard
5. **SignalR** `AdminActivityHub` (`/hubs/admin-activity`) — live activity feed

---

## 5. ความเสี่ยง / ข้อสังเกต (เรียงตามความสำคัญ)

### 5.1 🔴 HIGH — `FileStoragesCRUDController` เปิด endpoint ที่ดัมพ์ SCORM blob ทั้งหมด
`Controllers/Base/FileStoragesCRUDController.cs` สืบทอด `GenericController<FileStorage>` **โดยไม่ override อะไรเลย** → `GET api/admin/FileStoragesCRUD/Get` เรียก `_repository.GetAllAsync()` ที่โหลด **ทุกแถวรวมคอลัมน์ `Data` (byte[] = ZIP SCORM ทั้งก้อน)** เข้า memory แล้ว serialize เป็น JSON (base64)
- ขัด CLAUDE.md โดยตรง ("ห้าม Include/โหลด entity นี้ใน query รายการเด็ดขาด ใช้ `CachedFileLength`")
- ผลกระทบ: admin คนเดียวกดเรียก = โหลด ZIP ทุกไฟล์พร้อมกัน → memory spike / timeout / DoS เชิงปฏิบัติ
- **แนะนำ:** ลบ controller นี้ถ้าไม่มีใครใช้ หรือ override `Get`/`Post`/`Put`/`Delete` ให้ปิด หรือ project เฉพาะ metadata (ไม่รวม `Data`) — ควรออกเป็น PLAN

### 5.2 🟠 MEDIUM — `LearnerApiService` กลืน exception บดบัง root cause
`Infrastructure/Services/LearnerApiService.cs` ทุก method `catch (Exception) { return null; }` แล้ว controller แปลงเป็นข้อความเดียว "Failed to connect to the employee data source." (ดูบั๊ก search ที่เพิ่งแก้ — PLAN-009)
- ปัญหา: bypass `GlobalExceptionMiddleware` ที่ทำ ProblemDetails สวยงามอยู่แล้ว, ข้อความ error กำกวม (filter ผิด vs ต่อไม่ติด แยกไม่ออก), debug ยาก
- **แนะนำ:** ให้ propagate exception (หรือ throw แบบ typed) แล้วปล่อย middleware จัดการ + แยก `HttpRequestException` (เชื่อมต่อ) ออกจาก non-success status (filter/4xx) — ควรออกเป็น PLAN

### 5.3 🟠 MEDIUM — DevExtreme filter พังเมื่อฟิลด์ไม่อยู่ใน projection (ระบบ search)
อาการเดียวกันลามทั้งระบบ — ฟิลด์ที่ไม่มีบน entity/projection ทำ `DataSourceLoader.Load` throw ทั้งก้อน (Learners NID, Users fullName/division, fallback title/code/name) **กำลังแก้ฝั่ง frontend** ใน PLAN-009/011/012 (จำกัด searchExpr ให้ตรง projection)
- ข้อสังเกตเชิงสถาปัตยกรรม: การกรองถูกผูกกับ "ชื่อ property บน projection" โดยไม่มี guard ฝั่ง backend — ฟิลด์ enrich-in-memory (UsersCRUD: fullName/division/position) จะ filter ไม่ได้ตลอด ถ้าต้องการค้นด้วยฟิลด์เหล่านี้ ต้องปรับ controller ให้ enrich ก่อน filter (งานใหญ่ — ยังไม่ทำ)

### 5.4 🟡 LOW — Controller ขนาดใหญ่ (business logic ปนใน controller)
Assignments 1316, ContentItems 1179, Dashboard 766, Enrollments 624, Courses 622 บรรทัด — แม้มี service layer แต่ logic จำนวนมากยังอยู่ใน controller ทำให้เทส unit ยาก/อ่านยาก
- **แนะนำ (ไม่เร่ง):** ทยอยดึง logic ลง Application services เพิ่ม ออกเป็น PLAN refactor รายตัวเมื่อมีโอกาส

### 5.5 🟡 LOW — Anonymous-object responses ทำ contract sync เปราะ
controller คืน `Ok(new {...})` จำนวนมาก → ไม่มี type กลาง, React ต้องลอก shape มือ (เสี่ยง drift เวลา backend เปลี่ยน) มี CLAUDE.md กติกา "Mirrors <Dto>" ช่วยอยู่ แต่ยังพึ่งวินัยคน
- **แนะนำ (ไม่เร่ง):** ทยอยแปลง response เป็น DTO record ที่ใช้ร่วมได้ เริ่มจาก endpoint ที่ React ใช้บ่อย

### 5.6 ℹ️ ตรวจแล้วว่า "ดี" (ไม่ต้องแก้)
- secure-by-default (FallbackPolicy), HMAC learner proxy (constant-time), GlobalExceptionMiddleware, secret validation ตอน boot, policy audit ตอน startup, anonymous endpoints บังคับ HMAC จริงครบ

---

## 6. ข้อเสนอเป็นแผนงาน (ถ้าผู้ใช้อนุมัติ)

| ลำดับ | งาน | อ้างอิง |
|---|---|---|
| 1 | ปิด/แก้ `FileStoragesCRUDController` ไม่ให้ดัมพ์ blob | 5.1 |
| 2 | ให้ `LearnerApiService` หยุดกลืน exception + แยกชนิด error | 5.2 |
| 3 | (พิจารณา) ค้นหา Users/Learners ด้วยฟิลด์ enrich → ปรับ backend enrich-before-filter | 5.3 |
| 4 | (ทยอย) refactor controller ใหญ่ → service | 5.4 |

> ผมยังไม่ออกแผนพวกนี้จนกว่าผู้ใช้จะเลือก — แจ้งได้ว่าจะให้ทำข้อไหนก่อน
