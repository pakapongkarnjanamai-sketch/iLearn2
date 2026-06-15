# Division Isolation Analysis — iLearn

วิเคราะห์การแบ่งข้อมูลตาม **Division** (data isolation) ทั้งระบบ — กลไก, สถานะรายโมดูล, และพฤติกรรมของ Learner Group / Learner Group Categories

> เขียนโดย Claude Code (planner/reviewer) 2026-06-15 จากการอ่านโค้ดจริง — ชื่อไฟล์/บรรทัดอาจขยับ ให้ยืนยันก่อนใช้

---

## 1. กลไก (mechanism)

ยึดที่ `ICurrentUserService.DivisionId` (+ `IsSuperAdmin`):
- **SuperAdmin** → `DivisionId = null` → **เห็นทุก division** (ข้าม filter ทั้งหมด)
- **Division-admin** → `DivisionId = X` → เห็นเฉพาะของ division ตัวเอง

3 จุดบังคับใน service/controller:
1. **List:** `if (_currentUser.DivisionId.HasValue) query = query.Where(x => x.DivisionId == value)`
2. **Create:** `entity.DivisionId = _currentUser.DivisionId` (auto จากผู้สร้าง — **ปัจจุบันไม่รับค่าจาก client**)
3. **Get/Update/Delete:** ownership check — `if (HasValue && entity.DivisionId != value)` → 403 / NotFound

ระดับ host: `FallbackPolicy = DefaultPolicy` → ทุก endpoint ต้อง auth โดยปริยาย (secure by default)

---

## 2. Entity ที่มี Division dimension

มี `DivisionId` โดยตรง: `LearnerGroup`, `LearnerGroupCategory`, `Assignment`, `AssignmentListRow`, `Category`, `Role`, `AdminActivity`

ได้ division ผ่านความสัมพันธ์: `Course` (→ Category.DivisionId), `User` (→ UserRole→Role.DivisionId), `Learner` (external, ฟิลด์ Division)

**ไม่มี** division: `ContentItem` (คลังกลาง), `Enrollment`, `LearningLog`, `CourseType` (lookup กลาง), `Division` (ตัวมิติเอง)

---

## 3. สถานะ Isolation รายโมดูล

| Module | มี division? | Isolation | สถานะ |
|---|---|---|---|
| Courses | ผ่าน Category | `CoursesController.GetAll` กรอง `_currentUser.DivisionId` | ✅ |
| Assignments | ✓ | controller + service กรอง | ✅ |
| Learner Groups | ✓ | `LearnerGroupService` กรอง + ownership | ✅ |
| Learner Group Categories | ✓ | `LearnerGroupCategoryService` กรอง + ownership | ✅ |
| Learners | external | inject division filter (`InjectDivisionFilter`) | ✅ |
| Admin Users | ผ่าน Role | `UsersCRUDController.Get` กรอง (enrich+filter) | ✅ |
| Dashboard | — | scope ตาม division (KPI/charts) | ✅ |
| Master Data (Div/Cat/Type/Role) | บางตัว | SuperAdminOnly | ✅ ไม่ต้อง isolate |
| Enrollments | ✗ | **SuperAdminOnly** (API + route) | ✅ เห็นทั้งหมดโดยตั้งใจ |
| Content Library | ✗ (ContentItem ไม่มี division) | ไม่มี | ⚠️ คลังกลาง — ดู §5.2 |
| Learning Logs | ✗ | API = **SuperAdminOnly** แต่ UI ไม่ตรง | ⚠️ ดู §5.1 |

**ข้อสรุปด้านความปลอดภัย: ไม่มีข้อมูลรั่วข้าม division** — entity ที่มี division ถูก isolate ครบ; ตัวที่ไม่มี ถูกกั้นด้วย SuperAdminOnly

---

## 4. Learner Group & Learner Group Categories กับ Division

ทั้งสอง entity มี `DivisionId` (nullable):
- `LearnerGroupCategory` = โฟลเดอร์จัดกลุ่ม **แบบ hierarchy** (`ParentId`/`Path`/`Depth`) + DivisionId
- `LearnerGroup` = กลุ่มผู้เรียนจริง (มีสมาชิก) + DivisionId + CategoryId

**พฤติกรรมตามบทบาท:**
| | SuperAdmin | Division-admin |
|---|---|---|
| เห็น | ทุก group/category (ทุก division + global) | เฉพาะ `DivisionId == ตัวเอง` |
| สร้างแล้วได้ division | `null` (global) | division ตัวเอง (auto) |
| แก้/ลบของ division อื่น | ได้ | ถูกบล็อก (403/NotFound) |

**ข้อจำกัดของ design ปัจจุบัน (ดู §5.3):**
- group/category **global (division=null)** ที่ SuperAdmin สร้าง → **division-admin มองไม่เห็น** (filter `== value` ไม่ match null)
- `CreateAsync` **ไม่รับ divisionId จาก client** (auto จากผู้สร้างเสมอ — `LearnerGroupService.cs:214`, `LearnerGroupCategoryService.cs:128`) → SuperAdmin สร้างให้ "แผนก X โดยเฉพาะ" ไม่ได้

---

## 5. ประเด็นที่ต้องตัดสินใจ / แผนที่เกี่ยวข้อง

### 5.1 Learning Logs — UI ไม่ตรงกับสิทธิ์ API → **PLAN-021**
`LearningLogsCRUDController` = `SuperAdminOnly` แต่ route React `/learning-logs` ไม่ครอบ `RequireRole superAdminOnly` และเมนูอยู่ใน "Operations" (division-admin เห็น) → คลิกแล้วโดน 403 (ไม่รั่วข้อมูล แต่ UX สับสน)
→ แก้: ทำให้ SuperAdmin-only สม่ำเสมอ (gate route + ย้ายเมนู)

### 5.2 Content Library — คลังกลาง (ยังไม่ออกแผน)
`ContentItem` ไม่มี division → admin ทุกคนเห็น content ทั้งหมด ถ้าตั้งใจให้เป็นคลังกลางแชร์กัน = OK; ถ้าต้องการแยกตาม division ต้องเพิ่ม division dimension (งานใหญ่ — รอการตัดสินใจ)

### 5.3 SuperAdmin สร้าง group/category ระบุ division ไม่ได้ → **PLAN-022**
ให้ `CreateAsync` รับ `DivisionId` จาก DTO **เฉพาะเมื่อผู้สร้างเป็น SuperAdmin**; division-admin ยัง auto เป็น division ตัวเอง (ไม่ให้ override)

---

## 6. แฟ้มอ้างอิง

- `iLearn.Application/Services/LearnerGroupService.cs`, `LearnerGroupCategoryService.cs` — isolation logic
- `iLearn.API/Extensions/AuthorizationExtensions.cs` — policies
- `iLearn.API/Controllers/Base/UsersCRUDController.cs`, `LearnersController.cs`, `CoursesController.cs` — ตัวอย่าง isolation
- `DOC/api_analysis.md` — วิเคราะห์ API ภาพรวม
