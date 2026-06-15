# Division Isolation Analysis — iLearn (Current)

วิเคราะห์การแบ่งข้อมูลตาม **Division** (data isolation) ทั้งระบบ — กลไก, สถานะรายโมดูล, และพฤติกรรมของ Learner Group / Learner Group Categories

> อัปเดตล่าสุด: 2026-06-15 (หลัง implement PLAN-021/022/023)

---

## 1. กลไก (mechanism)

ยึดที่ `ICurrentUserService.DivisionId` (+ `IsSuperAdmin`):
- **SuperAdmin** → `DivisionId = null` → **เห็นทุก division** (ข้าม filter ทั้งหมด)
- **Division-admin** → `DivisionId = X` → เห็นเฉพาะของ division ตัวเอง

3 จุดบังคับใน service/controller:
1. **List:** `if (_currentUser.DivisionId.HasValue) query = query.Where(x => x.DivisionId == value)`
2. **Create:** โดยหลักเป็น auto จาก current user; แต่บาง flow รองรับ SuperAdmin ระบุ `DivisionId` ผ่าน DTO ได้ (พร้อม guard)
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
| Learning Logs | ✗ | API + UI = **SuperAdminOnly** | ✅ |

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
| สร้างแล้วได้ division | เลือกได้ (specific division หรือ `null`/global ตาม flow) | division ตัวเอง (auto) |
| แก้/ลบของ division อื่น | ได้ | ถูกบล็อก (403/NotFound) |

**พฤติกรรมที่อัปเดตแล้ว:**
- `LearnerGroupService.CreateAsync` และ `LearnerGroupCategoryService.CreateAsync` รองรับการกำหนด division สำหรับ SuperAdmin โดยยังคงป้องกัน escalation ฝั่ง division-admin
- `LearnerGroupCategoryService.UpdateAsync` รองรับการเปลี่ยน division ฝั่ง SuperAdmin พร้อม guard ไม่ให้เกิด tree inconsistency ข้าม division

---

## 5. ประเด็นที่ต้องติดตามต่อ

### 5.1 Learning Logs — ปิดช่องว่าง UI/API แล้ว
`LearningLogsCRUDController` เป็น `SuperAdminOnly` และฝั่ง React route/menu ถูกจัดให้เป็น SuperAdmin-only สอดคล้องกันแล้ว

### 5.2 Content Library — คลังกลาง (ยัง open decision)
`ContentItem` ไม่มี division → admin ทุกคนเห็น content ทั้งหมด ถ้าตั้งใจให้เป็นคลังกลางแชร์กัน = OK; ถ้าต้องการแยกตาม division ต้องเพิ่ม division dimension (งานใหญ่ — รอการตัดสินใจ)

### 5.3 Global records visibility policy
ข้อมูลที่เป็น global (`DivisionId = null`) ยังมีพฤติกรรมที่ division-admin อาจไม่เห็นตาม filter แบบ strict equality ซึ่งเป็น policy decision มากกว่าบั๊กความปลอดภัย

---

## 6. สถานะแผนที่เกี่ยวข้อง

- PLAN-021: DONE (Learning Logs UI/API consistency)
- PLAN-022: DONE (SuperAdmin select division on create)
- PLAN-023: DONE (SuperAdmin select division on category edit + explorer flow)

---

## 7. แฟ้มอ้างอิง

- `iLearn.Application/Services/LearnerGroupService.cs`, `LearnerGroupCategoryService.cs` — isolation logic
- `iLearn.API/Extensions/AuthorizationExtensions.cs` — policies
- `iLearn.API/Controllers/Base/UsersCRUDController.cs`, `LearnersController.cs`, `CoursesController.cs` — ตัวอย่าง isolation
- `DOC/api_analysis.md` — วิเคราะห์ API ภาพรวม
- `DOC/PLANS/PLAN-021-learning-logs-superadmin-consistency.md`
- `DOC/PLANS/PLAN-022-superadmin-assign-division-on-create.md`
- `DOC/PLANS/PLAN-023-superadmin-division-category-edit-and-explorer.md`
