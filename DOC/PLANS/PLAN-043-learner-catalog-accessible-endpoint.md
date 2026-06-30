# PLAN-043: แก้ "ไม่สามารถโหลดข้อมูลหลักสูตรได้" — เพิ่ม learner-accessible catalog endpoint

- **Status:** VERIFIED (รีวิวโดย Claude Code 2026-06-30 — ดู Review Notes ท้ายไฟล์)
- **Assigned:** GPT (GPT-5.3-Codex)
- **Priority:** High — บั๊กผู้ใช้จริงบน production (learner เปิด "คลังหลักสูตร" ไม่ได้เลย)
- **Estimated scope:** เพิ่ม learner-accessible catalog endpoint บน API (proxy auth) + proxy action ใน `MyLearningController` + แก้ JS ใน `Index.cshtml` ให้เรียกผ่าน proxy

## Problem

ผู้ใช้ (learner 500816, division PD2) เปิดหน้า learner portal (`iLearn.User`) — section "หลักสูตรของฉัน" โหลดได้ แต่ section **"คลังหลักสูตร" ขึ้น error แดง "ไม่สามารถโหลดข้อมูลหลักสูตรได้"** และแสดง 0 รายการ

## Evidence และ Root Cause

flow ที่ทำงาน vs ที่พัง ต่างกันที่ **เรียกผ่าน proxy หรือยิง API ตรง:**

| Section | เรียกอะไร | ผ่าน proxy? | ผล |
|---|---|---|---|
| หลักสูตรของฉัน | `loadMyCourses()` → `@Url.Action("GetMyCourses","MyLearning")` → proxy → API `Enrollments/my-courses` (`[AllowAnonymous]` + learner-proxy signature) | ✅ | โหลดได้ |
| คลังหลักสูตร | `loadAllCourses()` → `${serviceUrl}/courses` **ตรงจาก browser** → API `CoursesController.GetAll` | ❌ | **413/403 → error** |

1. [iLearn.User/Views/MyLearning/Index.cshtml:1225](file:///c:/Users/n4734/source/repos/iLearn2/iLearn.User/Views/MyLearning/Index.cshtml#L1225) — `loadAllCourses()` ยิง `${baseUrl}/courses` (`baseUrl` = `serviceUrl` = `ApiSettings:BaseUrl` จาก [_DevExtremeLayout.cshtml:299](file:///c:/Users/n4734/source/repos/iLearn2/iLearn.User/Views/Shared/_DevExtremeLayout.cshtml#L299))
2. API [CoursesController.cs:18](file:///c:/Users/n4734/source/repos/iLearn2/iLearn.API/Controllers/CoursesController.cs#L18) เป็น `[Authorize(Policy = "AdminOnly")]` ทั้ง controller — `AdminOnly = RequireRole("Admin","SuperAdmin")` ([AuthorizationExtensions.cs:25](file:///c:/Users/n4734/source/repos/iLearn2/iLearn.API/Extensions/AuthorizationExtensions.cs#L25))
3. learner ไม่มี role admin → เรียก `GET api/Courses` ตรง ๆ ได้ 401/403 → AJAX error handler ที่ [Index.cshtml:1244](file:///c:/Users/n4734/source/repos/iLearn2/iLearn.User/Views/MyLearning/Index.cshtml#L1244) แสดง "ไม่สามารถโหลดข้อมูลหลักสูตรได้"
4. `loadCourseTypes()` ([Index.cshtml:1137](file:///c:/Users/n4734/source/repos/iLearn2/iLearn.User/Views/MyLearning/Index.cshtml#L1137)) ยิง `/admin/CourseTypesCRUD/Get` ก็เป็น admin endpoint เช่นกัน (แต่ fail แล้ว fallthrough ไป loadAllCourses อยู่ดี)

**สรุป root cause:** learner portal ออกแบบให้ดึง catalog จาก **admin-only API endpoint ตรงจาก browser** ซึ่งผิด pattern — ของที่ถูกต้องคือผ่าน `MyLearningController` proxy + learner-proxy HMAC signature (เหมือน `GetMyCourses`) ไปยัง endpoint ที่ learner เข้าถึงได้ ปัจจุบัน **ไม่มี catalog endpoint สำหรับ learner** (proxy endpoints มีแค่ใน `EnrollmentsController`/`LearningLogsController`)

> หมายเหตุ: `AdminOnly` ถูกเพิ่มเข้า `CoursesController` ตั้งแต่ commit `46410fc` (2026-04-27) — feature นี้พังกับ learner ที่ไม่ใช่ admin ตั้งแต่ตอนนั้น

## Decision point (ต้องยืนยันก่อน/ระหว่างทำ)

**learner ควรเห็นหลักสูตรไหนใน "คลังหลักสูตร"?** — catalog นี้มีปุ่ม "ดูเนื้อหา" (`Player?courseId=...`) ให้ launch ได้ จึงเป็นเรื่อง visibility/security

- **ค่า default ที่เสนอ (mirror admin `GetAll`):** คอร์สที่ `Status == Open` และอยู่ใน **division ของ learner** (admin `GetAll` รับ `divisionName` filter อยู่แล้ว — learner มี division claim)
- ถ้า product ต้องการให้ browse ข้าม division หรือรวม Closed/อื่น ๆ ให้ปรับ filter ตามนั้น แล้วจดใน Implementer Notes

## Scope (ทำแค่นี้)

### 1. เพิ่ม learner-accessible catalog endpoint บน API
ใน `iLearn.API/Controllers/EnrollmentsController.cs` (หรือสร้าง learner-facing controller ใหม่ถ้าเหมาะกว่า) เพิ่ม action ตาม pattern ของ `GetMyCourses`/`GetPlayerInfoByCourse`:
- `[AllowAnonymous] [HttpGet("course-catalog")]` — ใช้ `TryGetTrustedLearnerLearnerCode(...)` ตรวจ learner-proxy signature ก่อน (เหมือน [EnrollmentsController.cs:71-77](file:///c:/Users/n4734/source/repos/iLearn2/iLearn.API/Controllers/EnrollmentsController.cs#L71))
- คืนรายการคอร์สตาม Decision point ข้างบน
- **DTO ต้องมี field ตรงกับที่ JS อ่าน** (ดู Contract ด้านล่าง) — สร้าง DTO ใหม่เฉพาะ catalog (อย่า reuse admin DTO ที่มี field เกินจำเป็น/sensitive) พร้อมคอมเมนต์ `// Mirrors <DtoName>`
- **ห้าม Include/โหลด `FileStorage.Data`** ใน query (กติกา repo — ใช้ `ContentItem.CachedFileLength` ถ้าต้องการขนาด)

**Contract (field ที่ catalog JS ต้องการ — จาก `renderCatalogCard`/`renderCatalogListItem`/`organizeCoursesByCategory`/`getCourseTypeId`):**
```
id            : number   // course id (ใช้ลิงก์ Player?courseId=)
code          : string   // course code (แสดงบน badge)
title         : string   // course title
categoryId    : number
categoryName  : string
courseTypeId  : number
courseTypeName: string   // ใส่มาด้วยเพื่อให้ type filter แสดงชื่อได้ (จะได้ไม่ต้องเรียก /admin/CourseTypesCRUD/Get)
coverImageUrl : string?  // optional (JS มี fallback image อยู่แล้ว)
```

### 2. เพิ่ม proxy action ใน `MyLearningController`
เพิ่ม `[HttpGet] GetCourseCatalog()` mirror `GetMyCourses` ([MyLearningController.cs:38-69](file:///c:/Users/n4734/source/repos/iLearn2/iLearn.User/Controllers/MyLearningController.cs#L38)) — ใช้ `SendLearnerProxyRequestAsync(HttpMethod.Get, "Enrollments/course-catalog", learnerCode)` + `CreateProxyResultAsync` (โครง try/catch เดียวกัน)

### 3. แก้ JS ใน `Index.cshtml`
- `loadAllCourses()` ([:1221](file:///c:/Users/n4734/source/repos/iLearn2/iLearn.User/Views/MyLearning/Index.cshtml#L1221)) — เปลี่ยน `url: ${baseUrl}/courses` เป็น `url: '@Url.Action("GetCourseCatalog","MyLearning")'` (เหมือน `myCoursesUrl`) และ map response shape ให้ตรง (`res.data`)
- `loadCourseTypes()` ([:1136](file:///c:/Users/n4734/source/repos/iLearn2/iLearn.User/Views/MyLearning/Index.cshtml#L1136)) — เนื่องจาก catalog DTO มี `courseTypeName` แล้ว ให้ **เลิกเรียก `/admin/CourseTypesCRUD/Get`** แล้วปรับ `getCatalogTypeName` ให้ใช้ `course.courseTypeName` โดยตรง (หรือ derive `allCourseTypes` จาก course list) — ตัด dependency กับ admin endpoint ออกให้หมด
- เช็คว่าไม่มีจุดอื่นใน `Index.cshtml`/`Player.cshtml` ที่ยังยิง `${baseUrl}/...` ไป admin/Courses โดยตรง (grep `${baseUrl}` / `serviceUrl`) ถ้าเจอให้ route ผ่าน proxy ด้วย หรือจดไว้ถ้าเกิน scope

### ขอบเขตที่ห้ามทำ
- **ห้ามลด authorization ของ `CoursesController`** (admin endpoint ต้องล็อกไว้เหมือนเดิม)
- ห้ามแตะ React admin (`iLearn.Admin.React`) — คนละ app
- ห้ามเปลี่ยน learner-proxy auth mechanism (แค่ reuse)
- ห้าม return field sensitive (เช่น internal flags, FileStorage) ใน catalog DTO

## Verification
```powershell
# Backend (จาก repo root — ถ้า API รันใน VS ให้ build แยก artifacts)
dotnet build iLearn.API -o artifacts\verify-plan043
dotnet build iLearn.Tests -o artifacts\verify-test
dotnet test artifacts\verify-test\iLearn.Tests.dll
Remove-Item -Recurse -Force artifacts\verify-plan043, artifacts\verify-test

# ยืนยันไม่มี catalog JS ยิง admin endpoint ตรงแล้ว
rg "baseUrl./courses|/admin/CourseTypesCRUD" iLearn.User/Views/MyLearning
```
- **E2E (จุดชี้ขาด):** login เป็น learner ที่ไม่ใช่ admin → เปิดหน้า MyLearning → "คลังหลักสูตร" ต้องโหลดรายการคอร์สได้ (ไม่ขึ้น error แดง), type filter แสดงชื่อประเภทถูก, ปุ่ม "ดูเนื้อหา" เปิด Player ได้
- ยืนยันว่า admin endpoint `GET api/Courses` ยังคง 401/403 เมื่อเรียกโดย learner ตรง ๆ (security ไม่ถูกลด)

## Implementer Notes

- Endpoint/DTO ที่เพิ่มจริง:
	- API learner catalog endpoint: `GET api/Enrollments/course-catalog` (AllowAnonymous + learner-proxy signature via `TryGetTrustedLearnerLearnerCode`)
	- DTO ใหม่: `iLearn.Application/DTOs/LearnerCourseCatalogDto.cs`
	- User proxy endpoint: `GET /MyLearning/GetCourseCatalog` ใน `MyLearningController`

- Catalog filter ที่ใช้จริง:
	- ฝั่ง `iLearn.User` ดึง division จาก claim `Division` แล้ว forward เป็น query `divisionName` ไป API proxy call (`Enrollments/course-catalog?divisionName=...`)
	- ฝั่ง API กรองเฉพาะคอร์ส `Status == Open` และ `Category.Division.Name == divisionName`
	- ถ้าไม่พบ `divisionName` (ว่าง/`-`) จะคืนรายการว่าง (`[]`) เพื่อคง data isolation (ไม่ fallback เป็นทุก division)

- JS/UI ที่แก้:
	- `loadAllCourses()` เปลี่ยนมาเรียก `@Url.Action("GetCourseCatalog", "MyLearning")` แทน `${baseUrl}/courses`
	- ตัด dependency `/admin/CourseTypesCRUD/Get` ออกทั้งหมด
	- `getCatalogTypeName` ใช้ `course.courseTypeName` (fallback `course.typeName`) จาก catalog DTO โดยตรง

- Verification ที่รันจริง (ผ่าน):
	- `dotnet build iLearn.API -o artifacts/verify-plan043`
	- `dotnet build iLearn.Tests -o artifacts/verify-test`
	- `dotnet test artifacts/verify-test/iLearn.Tests.dll` -> Passed 118, Failed 0
	- `rg "baseUrl./courses|/admin/CourseTypesCRUD" iLearn.User/Views/MyLearning` -> ไม่พบ
	- cleanup สำเร็จ: `artifacts/verify-plan043`, `artifacts/verify-test`

- E2E บน production:
	- ยังไม่ได้รันในรอบ implement นี้ (ต้องทดสอบด้วย learner account จริงหลัง deploy)

## Review Notes (Claude Code, 2026-06-30)

ตรวจอิสระทุกจุด (อ่าน diff จริง + build/test เอง):

**✅ ถูกต้อง/ปลอดภัย:**
- Security จุดสำคัญผ่าน: endpoint `[AllowAnonymous]` แต่เรียก `TryGetTrustedLearnerLearnerCode` ก่อนเสมอ → ต้องมี learner-proxy signature จริง (ไม่ได้เปิด anonymous)
- **division มาจาก claim ฝั่ง server** (`MyLearningController.GetAuthenticatedLearnerDivisionName` อ่าน `User.FindFirst("Division")`) ไม่ใช่ browser input → learner ปลอม division เพื่อดู catalog ข้าม division ไม่ได้
- DTO `LearnerCourseCatalogDto` มีเฉพาะ field ที่ JS ใช้ ไม่มี field sensitive; query ไม่ Include `Versions`/`FileStorage` (ตามกติกา repo)
- `CoursesController` (admin) authorization ไม่ถูกแตะ — security ไม่ถูกลด
- JS: ลบตัวแปร `baseUrl`/`divisionName`/`allCourseTypes` แล้วไม่มี dangling reference เหลือ (grep ยืนยัน), `handleLearnerSessionExpired` มีจริงใน `_DevExtremeLayout.cshtml:405`, ตัด dependency กับ `/admin/CourseTypesCRUD/Get` หมด
- test เดิม `EnrollmentsPlayerInfoTests` เพิ่ม `InMemoryGenericRepository<Course>` ให้ constructor ใหม่ — จำเป็นและถูกต้อง
- Build เอง: `iLearn.API` 0 errors, **`iLearn.User` 0 errors** (implementer ไม่ได้ build ตัวนี้ ผม build เพิ่ม), `dotnet test` 118/118 ผ่าน

**🟡 ข้อสังเกต minor (ไม่บล็อก — พิจารณาเป็น follow-up):**
1. `divisionName` ส่งผ่าน query string ซึ่ง **อยู่นอก payload ที่ sign** (signature คลุม path เท่านั้น) — ปลอดภัยภายใต้ trust model ปัจจุบัน (เฉพาะ User server ที่ถือ SharedSecret เรียกได้ และมัน set จาก claim) แต่ถ้าต้องการ defense-in-depth ให้ API derive division จาก `learnerCode` ที่ sign แล้วเอง (ตอนนี้ทิ้ง learnerCode ด้วย `out _`) จะ robust กว่า
2. **ไม่มี unit test ใหม่สำหรับ `GetCourseCatalog`** (signature rejection / division filter / empty-division → []) — endpoint นี้ learner-facing ควรมี test คุม regression ในอนาคต
3. division match แบบ exact string (`Name == divisionName`) — ถ้าชื่อ division มี casing/whitespace drift อาจคืนว่างเงียบ ๆ (แต่ consistent กับ admin `GetAll` เดิม)

**สรุป:** core fix ถูกต้องและปลอดภัย ปรับเป็น VERIFIED — เหลือ E2E จริงบน prod (ต้อง deploy `iLearn.API` + `iLearn.User` ก่อน) และแนะนำเพิ่ม unit test ตามข้อ 2
