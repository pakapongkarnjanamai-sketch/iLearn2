# iLearn Lifecycle Improvement Plan

Last updated: 2026-04-30

## Purpose

ไฟล์นี้สรุป recommendation ที่ยังเปิดอยู่จากเอกสาร lifecycle ทั้งชุด และเรียงลำดับจากสำคัญมากไปน้อยตามผลกระทบต่อ data integrity, contract consistency, learner/admin behavior, และความเสี่ยงในการทำ regression

Progress update: priorities 1, 2, and 5 were completed on 2026-04-30 by centralizing SCORM Exam/Learn outcome evaluation in `ScormContentStatusPolicy`, making `LearningLog` defaults safe, and unifying the shared `Due Soon` threshold. Priority 4 is now partially completed because `CourseVersionsCRUDController.Get/{id}` no longer returns a raw entity payload. Priority 6 is also completed in code by moving completed-history retention into `EnrollmentVisibilityPolicy`.

## Prioritization Rules

1. สิ่งที่ทำให้ player, progress, completion, หรือ learner access คลาดเคลื่อนกัน ถือว่าสำคัญสูงสุด
2. สิ่งที่เสี่ยงสร้างข้อมูลผิดแบบเงียบ ๆ หรือทำให้ state เพี้ยนย้อนหลัง อยู่ลำดับถัดมา
3. สิ่งที่ทำให้ API contract ไม่ชัดหรือ response หลุด raw entity อยู่ระดับกลาง
4. สิ่งที่เป็น UX/report consistency แต่ยังไม่ทำให้ business state ผิด อยู่ระดับกลางถึงล่าง
5. Cleanup หรือ governance ของ master data อยู่ลำดับท้าย เว้นแต่ผูกกับ destructive action โดยตรง

## Priority Order

### 1. Centralized SCORM Exam Completion Rule

Status: Completed on 2026-04-30

Outcome:
- Added shared `ScormContentStatusPolicy` as the single owner for Learn vs Exam completion evaluation
- `LearningLogsController` runtime-to-log mapping and `EnrollmentsController` player status now use the same rule
- Regression coverage now includes exam `completed + unknown` and keeps player/runtime behavior aligned

Follow-up:
- Keep SCORM 1.2 precedence regression-covered
- Improve user-facing copy for packages that report `completed` with `success_status=unknown`

### 2. Remove Dangerous LearningLog Completion Defaults

Status: Completed on 2026-04-30

Outcome:
- `LearningLog.Status` now defaults to `incomplete`
- `LearningLog.Progress` now defaults to `0`
- Focused regression coverage now guards the constructor defaults and the active runtime creation path

Follow-up:
- Keep future learning-log creation paths explicit
- Revisit EF/database defaults only if a later migration introduces server-side defaults for this entity

### 3. Finalize Retired Course Policy

Priority: High

Why now:
- เอกสารยังพูดได้ทั้ง “resolve enrollments first” และ “explicitly accept impact”
- service ปัจจุบัน hard-block retire เมื่อมี open enrollments อยู่จริง
- ถ้าไม่ปิด decision นี้ UI copy, API message, และ product expectation จะยังไม่ตรงกัน

Evidence:
- `iLearn.Application/Services/CourseService.cs` hard-block เมื่อ `openEnrollmentCount > 0`
- `DOC/COURSE-LIFECYCLE-RULES.md` กับ `DOC/LIFECYCLE-OVERVIEW.md` ยังเก็บ decision gate นี้ไว้

Goal:
- มีนโยบายเดียวสำหรับ Retired: hard block หรือ explicit force-retire
- Impact preview, confirmation copy, API result, และ docs ตรงกันหมด

Implementation plan:
1. ตัดสิน product policy ว่าจะคง hard block หรือรองรับ force flow
2. ถ้าคง hard block: lock docs/UI/API message ให้ตรงกับ behavior ปัจจุบัน
3. ถ้ารองรับ force flow: เพิ่ม explicit flag, confirmation UX, audit trail, และ learner impact messaging
4. อัปเดต `CourseStatusImpactDto`, `Courses/Detail`, `COURSE-LIFECYCLE-RULES.md`, `STATUS-DEFINITIONS.md`

Validation:
- tests สำหรับ retire with open enrollments, retire when safe, และ impact preview contract

### 4. Eliminate Remaining Raw Entity Or Ambiguous Lifecycle Payloads

Status: Completed on 2026-04-30

Outcome:
- `CourseVersionsCRUDController.Get/{id}` returns `CourseVersionDto` with semantic version/content lifecycle fields
- `CoursesCRUDController.Get/{id}` returns `CourseDetailDto` and `GetActive` returns semantic projected rows instead of raw `Course` entities
- `ContentItemsCRUDController.Get/{id}` now returns `ContentItemDto` instead of a raw `ContentItem` entity
- Focused controller tests now lock the content/version/course lifecycle detail contracts that were previously leaking raw entities

Priority: High

Why it mattered:
- DTO response หลักของ course/content/version เคยยังมีบาง endpoint ที่คืน raw entity หรือ local payload ที่ยังพึ่ง `IsActive` มากเกินไป
- จุดที่เห็นชัดที่สุดคือ detail endpoints ของ course version และ content item ซึ่งเคยตกกลับไป raw entity จาก `GenericController`

Evidence now addressed:
- `iLearn.API/Controllers/Base/CourseVersionsCRUDController.cs` returns `CourseVersionDto` instead of a raw `CourseVersion` entity
- `iLearn.API/Controllers/Base/ContentItemsCRUDController.cs` returns `ContentItemDto` instead of a raw `ContentItem` entity for `Get/{id}`
- generic master-data endpoints remain out of scope because they do not have richer domain lifecycle semantics to expose

Goal:
- response ฝั่ง domain-specific lifecycle ต้องไม่คืน raw entity ถ้าความหมายจริงต้องมี semantic state เช่น `VersionState`, `PublishState`, `ReadinessStatus`

Implementation summary:
1. Keep `CourseVersionsCRUDController.Get` on shaped DTO response with contract tests
2. Shape `ContentItemsCRUDController.Get/{id}` to `ContentItemDto`
3. Keep generic master-data endpoints on raw `IsActive` where no richer lifecycle exists

Validation:
- focused endpoint tests for course/content/version detail slices
- API compile via targeted test builds

### 5. Unify Due Soon Threshold Across Dashboard And Admin Filters

Status: Completed on 2026-04-30

Outcome:
- Added shared `AssignmentStatusKeys.DueSoonWindowDays` and `IsDueSoon(...)` as the single owner for `Due Soon` bucketing
- `DashboardController` KPI/priority assignment calculations now use the shared owner instead of `today.AddDays(14)`
- Admin assignment list and Gantt quick filters now render and apply the same seven-day window as the backend
- Focused policy tests now lock the due-soon boundary behavior

Priority: High

Why now:
- Admin assignment quick filters ใช้ `Due in 7 days`
- Dashboard API ใช้ `today.AddDays(14)`
- ผู้ใช้จะเห็น KPI และ list filter ไม่ตรงกันในระบบเดียวกัน

Evidence:
- `iLearn.Admin/Views/Assignments/Index.cshtml` และ `Gantt.cshtml` ใช้ 7 วัน
- `iLearn.API/Controllers/DashboardController.cs` ใช้ 14 วัน

Goal:
- มี threshold กลางเพียงค่าเดียวสำหรับ `Due Soon`
- Dashboard, report, list filter, และ docs ใช้ข้อความเดียวกัน

Implementation summary:
1. ใช้ shared owner กลางที่ `AssignmentStatusKeys`
2. เปลี่ยน `DashboardController` และ Admin labels/filter logic ให้ใช้ค่าเดียวกัน
3. อัปเดต docs และเพิ่ม focused tests สำหรับ due-soon boundary

Validation:
- tests สำหรับ due soon bucketing
- smoke check dashboard KPI กับ assignments list filter

### 6. Externalize Completed Learner History Retention

Status: Completed on 2026-04-30

Outcome:
- Added shared `EnrollmentVisibilityPolicy` as the named owner for learner completed-history visibility
- `EnrollmentsController.GetMyCourses` no longer embeds `currentDate.AddMonths(-1)` directly
- Focused tests now lock the one-month retention boundary

Priority: Medium

Why now:
- learner dashboard visibility ของ completed enrollments ยังพึ่ง rule แบบ implicit หนึ่งเดือนใน controller
- เป็น business rule จริง แต่ตอนนี้ยังไม่มี named constant/config ทำให้ drift ง่าย

Evidence:
- `iLearn.API/Controllers/EnrollmentsController.cs` ใช้ `currentDate.AddMonths(-1)`

Goal:
- retention window มี owner ชัด, เปลี่ยนได้, test ได้, และอธิบายได้ใน docs

Implementation summary:
1. ย้าย one-month retention ไปเป็น named owner ที่ `EnrollmentVisibilityPolicy`
2. ใช้ owner กลางใน learner-visible enrollment queries
3. เพิ่ม tests สำหรับขอบเขตวันครบกำหนดการแสดงผล completed history
4. อัปเดต `ASSIGNMENT-ENROLLMENT-LIFECYCLE-RULES.md`

Validation:
- tests ที่ boundary ของ retention window

### 7. Extend Shared Content Publication Policy With Impact Preview

Status: Completed on 2026-04-30

Outcome:
- Added shared unpublish impact preview in `IContentPublicationService` / `ContentPublicationService`
- `ContentItemsController` bulk unpublish paths now use the same shared policy instead of bypassing it with controller-local state changes
- Admin `Content Library` bulk-unpublish confirmations now show eligible versus blocked counts and mention linked course references before running maintenance
- Focused service tests now lock eligible-versus-blocked preview behavior

Priority: Medium

Why it mattered:
- single/bulk publish-unpublish ถูก centralize แล้ว แต่ blocked bulk maintenance เคยตอบกลับแบบ hard-stop หรือ bypass policy ไปเลย
- ฝั่ง Admin ยังไม่มี impact preview ที่บอกว่าอะไร block อยู่และเพราะอะไร ก่อนผู้ใช้กด maintenance

Evidence now addressed:
- `IContentPublicationService` และ `ContentPublicationService` now expose preview logic for batch unpublish
- `ContentItemsController.BatchUnpublish` and `BulkDeletePublished` now route through the shared policy/preview path

Goal:
- bulk maintenance บอก impacted course/version references ได้ก่อนทำจริง
- รองรับ UX ระดับ preview โดย default และเปิดทางให้มี explicit force flow ในอนาคตถ้าจำเป็น

Implementation summary:
1. เพิ่ม shared service method สำหรับ preview blocked items
2. แสดง impacted course/version references ใน Admin maintenance UI
3. ทำ bulk unpublish default path ให้ skip blocked items ตาม shared guard แทนการ bypass policy

Validation:
- tests สำหรับ preview payload
- Admin UX smoke check

### 8. Add NoLearners Display Bucket For Assignment Batches

Priority: Medium

Why now:
- contract ปัจจุบันยังคืน `InProgress` เมื่อ assignment ไม่มี enrollments และไม่มี boundary อื่น
- ไม่ผิดเชิง compatibility แต่ทำให้ admin history/monitoring อ่านยาก

Evidence:
- recommendation ใน `LIFECYCLE-OVERVIEW.md` และ `ASSIGNMENT-ENROLLMENT-LIFECYCLE-RULES.md`

Goal:
- คง contract เดิมถ้าจำเป็น แต่เพิ่ม display bucket หรือ KPI ที่แยก `No Learners` ให้ Admin เข้าใจได้ทันที

Implementation plan:
1. ตัดสินว่าจะเพิ่ม client-side display bucket หรือ backend computed helper field
2. แก้ dashboard/list/report ให้แยก `No Learners` จาก `InProgress`
3. เพิ่ม tests ที่ guard contract เดิมถ้ายังไม่ migrate

Validation:
- assignment status tests และ Admin UI smoke checks

### 9. Harden Master Data Impact Checks And Active Helper Text

Priority: Medium-Low

Why now:
- recommendation ของ master data ยังเปิดอยู่หลายข้อ แต่ส่วนใหญ่เป็น governance และ destructive-action safety มากกว่าความถูกต้องของ learner/runtime logic

Evidence:
- `DOC/MASTER-DATA-LIFECYCLE-RULES.md`

Goal:
- delete/deactivate master data ที่ถูกอ้างอิงต้องมี impact checks สม่ำเสมอ
- grid/detail views ต้องอธิบายความหมายของ inactive สำหรับ object ที่มีผลต่อ behavior

Implementation plan:
1. audit master data controllers ที่รองรับ deactivate/delete
2. ใส่ pre-check และ error contract ที่สม่ำเสมอ
3. ปรับ report/detail helper text และ lookup filtering ให้ชัด

Validation:
- endpoint tests สำหรับ deactivate/delete blocked cases

### 10. Remove Display Name As Business Key And Finalize Learner Group Category Deletion Rules

Priority: Low

Why now:
- เป็นเรื่อง data governance ระยะต่อไป ไม่ใช่ production correctness risk ระดับแรก

Evidence:
- `DOC/MASTER-DATA-LIFECYCLE-RULES.md`

Goal:
- `CourseType` และ `Role` ไม่ใช้ display name เป็น business key
- tree/category delete behavior มี rule ชัดก่อนขยาย bulk maintenance tools

Implementation plan:
1. ระบุ key owner ที่แท้จริงใน API/domain
2. audit query/filter ที่ยัง rely on names
3. ออกแบบ delete/move behavior สำหรับ learner group categories

Validation:
- integration tests สำหรับ rename/delete paths

## Suggested Execution Phases

### Phase 0: Decision Gates

1. ตัดสิน Retired course policy
2. ตัดสิน Due Soon threshold กลาง

### Phase 1: Critical Correctness

1. Remove dangerous LearningLog defaults

### Phase 2: Contract Hardening

1. Eliminate remaining raw entity / ambiguous lifecycle payloads
2. Externalize completed learner history retention

### Phase 3: Workflow Consistency

1. Unify Due Soon threshold
2. Extend shared content publication policy with impact preview
3. Add NoLearners display bucket

### Phase 4: Governance And Cleanup

1. Harden master data impact checks
2. Remove display-name-as-key dependencies
3. Finalize learner group category deletion behavior

## Recommended Next Implementation Slice

If only one slice is started next, start with:

1. `remaining response payload audit`

Why:
- เป็น next implementable high-priority item ที่ไม่ต้องรอ product decision
- ช่วยปิด contract ambiguity ต่อจาก `CourseVersionsCRUDController.Get/{id}` ที่เพิ่ง align แล้ว
- scope ยังพอแยกทำเป็น endpoint-by-endpoint พร้อม contract tests ได้

If one smaller but lower-risk slice is needed before that, use:

1. `CourseVersionsCRUDController` response shaping

Why:
- แก้ contract ambiguity ชัดเจนและ scope แคบ
- ไม่ต้องรอ product decision
