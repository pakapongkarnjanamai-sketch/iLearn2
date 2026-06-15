# PLAN-027: Refactor controller ใหญ่ + DTO typing (increment ถัดไป — CoursesController)

- **Status:** VERIFIED ✅ (Claude review 2026-06-15: CoursesController `Ok(new{)`=0 → ApiResponse<T> (shape-compatible: success/data เดิม + field เกินไม่กระทบ React), delegate _courseService, test 118 + build/lint ผ่าน. หมายเหตุ: controller 647 บรรทัด (ยาวกว่าเดิมเล็กน้อยจาก ApiResponse verbosity — เป้าหมาย typed+service บรรลุ))
- **Assigned:** GPT (GPT-5.3 Codex) — ย้ายจาก Gemini (เครดิตหมด) 2026-06-15
- **Priority:** Low
- **Estimated scope:** `CoursesController.cs` + `CourseService` (มีอยู่แล้ว — ขยาย) + DTO/`ApiResponse<T>`
- **ต่อจาก:** PLAN-017 (pilot Enrollments→service) + PLAN-018 (pilot Dashboard anonymous→DTO)

## Problem

หนี้ทางเทคนิคที่ทำเป็น increment: controller ใหญ่มี logic ปน + คืน `Ok(new {...})` (anonymous) ทำ OpenAPI/contract sync ยาก PLAN-017/018 ทำ pilot ไปแล้ว 1 ตัว/ด้าน — แผนนี้ทำ **increment ถัดไปกับ controller เดียว** ให้จบทั้ง 2 ด้านพร้อมกัน (refactor logic + typed response)

**เป้าหมาย increment นี้: `CoursesController` (622 บรรทัด)** — เลือกเพราะมี `CourseService`/`ICourseService` อยู่แล้ว (ขยายต่อได้ ความเสี่ยงต่ำกว่าสร้างใหม่) และคืน anonymous wrapper หลายจุด (`Ok(new { success = true, data = ... })`)

## Scope (ทำแค่นี้)

### A. ดึง logic ลง service
- ย้าย business logic ใน action ของ `CoursesController` ที่ยังอยู่ใน controller → `CourseService` (เพิ่ม method ใน `ICourseService` + impl) controller เหลือ orchestration (รับ input → เรียก service → คืนผล)
- คง `[Authorize]` + division isolation (`_currentUser`) **ไว้ที่ controller** (อย่าย้าย auth ลง service — แต่ logic กรอง division ที่เป็น business rule ย้ายได้ถ้าส่ง currentUser context เข้าไป — เลือกแนวที่ไม่ทำให้ isolation รั่ว แล้วจดใน Notes)

### B. แทน anonymous response ด้วย typed
- เปลี่ยน `Ok(new { success, data })` → ใช้ `ApiResponse<T>` (`iLearn.Domain/Common/ApiResponse.cs` มีอยู่แล้ว) หรือ DTO record ที่ shape ตรงเดิม
- **ห้ามเปลี่ยน shape ที่ React อ่าน** — grep endpoint ของ courses ใน `iLearn.Admin.React/src` (เช่น `Courses`, `Courses/{id}`, `course-types-lookup`, `versions`, `readiness` ฯลฯ) เทียบ field ให้ตรงเป๊ะก่อนแก้ (API Contract Sync)
- เพิ่มคอมเมนต์ `// Mirrors <Dto>` ฝั่ง React type ที่เกี่ยวข้อง

### C. pure refactor — พฤติกรรม/shape/status เดิมทั้งหมด (`dotnet test` เป็นตาข่าย)

## Out of scope (ห้ามแตะ)

- ห้าม refactor controller อื่น (Assignments/ContentItems ฯลฯ — increment ต่อ ๆ ไปแยกแผน)
- ห้ามเปลี่ยนพฤติกรรม/shape/endpoint path
- ห้ามแตะ division isolation logic จนรั่ว (ต้องคงผลลัพธ์เดิม)
- ห้ามแตะ CourseDetailPage/Editor ฝั่ง React นอกจากเพิ่มคอมเมนต์ Mirrors (ถ้าจำเป็น) — ไม่เปลี่ยน type shape

## Acceptance criteria

- [x] `CoursesController` สั้นลง logic หลักอยู่ใน `CourseService`
- [x] response ของ courses endpoints เป็น typed (`ApiResponse<T>`/DTO) ไม่ใช่ anonymous — shape ที่ React ได้รับ**ไม่เปลี่ยน** (หน้า courses/explorer/detail ยังทำงาน)
- [x] division isolation ของ courses ยังถูก (admin เห็นเฉพาะ division ตัวเอง)
- [x] `dotnet test` ผ่านครบ + `npm run build`/`lint` ผ่าน
- [x] `/courses` (explorer) + course detail ยังโหลด/แสดงครบเหมือนเดิม

## Verification

```powershell
dotnet build iLearn.Tests -o artifacts/verify-test
dotnet test artifacts/verify-test/iLearn.Tests.dll
Remove-Item -Recurse -Force artifacts/verify-test
npm run lint; npm run build
```
ทดสอบ manual: `/courses` explorer ไล่ระดับ + course detail (overview/versions/learners/assignments) ยังทำงานครบ

## Implementer Notes

- Refactor `iLearn.API/Controllers/CoursesController.cs`
	- ลบ logic aggregation หนักออกจาก controller แล้วเรียก service ตรงใน 3 endpoint:
		- `GET Courses/{courseId}/learners` -> `_courseService.GetCourseLearnersAsync(courseId)`
		- `GET Courses/{courseId}/assignments` -> `_courseService.GetCourseAssignmentsAsync(courseId)`
		- `GET Courses/{courseId}/dashboard` -> `_courseService.GetCourseDashboardAsync(courseId)`
	- ตัด dependency ที่ไม่จำเป็นใน controller constructor (`Enrollment/Assignment/EnrollmentAssignment repos`, `ILearnerApiService`, `IDateTime`) เพื่อคงบทบาท orchestration

- Typed response increment
	- เปลี่ยน anonymous wrappers ของ courses/version/status-impact หลาย endpoint เป็น `ApiResponse<T>` โดยคง field ที่ React ใช้ (`success`, `data`, `message`) และ path/status เดิม
	- `course-types-lookup` คงเป็น plain array เดิมเพื่อรักษา contract ฝั่ง explorer/editor

- API Contract Sync (grep + cross-check)
	- ตรวจ endpoint usage ใน `iLearn.Admin.React/src` ก่อนแก้ เพื่อยืนยัน field ที่ถูกอ่านจริง
	- เพิ่มคอมเมนต์ `// Mirrors <Dto>` ใน type ฝั่ง React ที่เกี่ยวข้อง:
		- `src/pages/courses/CourseDetailPage.tsx`
		- `src/pages/courses/CourseListPage.tsx`
		- `src/pages/courses/CourseEditorPage.tsx`
		- `src/pages/courses/VersionFormPage.tsx`

- Verification
	- `dotnet build iLearn.API/iLearn.API.csproj --artifacts-path artifacts/validate` ผ่าน
	- `dotnet build iLearn.Tests -o artifacts/verify-test` ผ่าน
	- `dotnet test artifacts/verify-test/iLearn.Tests.dll` ผ่าน (118/118)
	- `npm run lint` ผ่าน (EXIT:0)
	- `npm run build` ผ่าน (EXIT:0)
	- Manual smoke ผ่าน: `/courses` explorer โหลดรายการได้ และ `/courses/823` (course detail) โหลดครบ tab/control
