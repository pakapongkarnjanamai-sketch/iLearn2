# PLAN-185: Audit ignored-query-filter current-state decisions

- **Status:** VERIFIED
- **Assigned:** GPT
- **Reviewer:** Claude Code
- **Priority:** Medium
- **Estimated scope:** 8 source locations + focused tests where current-state logic is affected

## Problem

PLAN-184 found a production bug where `Enrollments/my-courses` hid an otherwise launchable assigned course because the endpoint loaded enrollment graphs with `ignoreQueryFilters: true`, then used soft-deleted `CourseContentItem` links in a current-state readiness decision.

Soft-delete history is valid and required for audit/reporting, but ignored query filters become dangerous when their results feed current operational decisions such as:

- learner visibility
- launch/readiness checks
- status/KPI counts
- active assignment/course/version decisions
- linked-content summaries shown as current state

This plan audits all remaining ignored-query-filter usages and classifies whether each one is historical/audit-safe or needs explicit filtering before current-state decisions.

## Scope

Audit these current code locations:

1. `iLearn.API/Controllers/DashboardController.cs`
   - `.IgnoreQueryFilters()` near dashboard aggregation
   - Determine whether deleted courses/content/users are counted as current dashboard state

2. `iLearn.API/Controllers/EnrollmentsController.cs`
   - `GetMyCourses()` uses `ignoreQueryFilters: true`
   - Confirm PLAN-184 readiness fix is enough and check `GetEffectiveSchedule(...)` / learner visibility for other soft-deleted navigation data

3. `iLearn.API/Controllers/LearnersController.cs`
   - enrollment loading with `ignoreQueryFilters: true`
   - Decide whether learner profile/history is intentionally historical or mixes current visibility/status counts

4. `iLearn.Application/Services/AssignmentDashboardService.cs`
   - `LoadBatchAsync(... ignoreQueryFilters: true)` and repository `ignoreQueryFilters: true`
   - Determine whether this legacy/internal dashboard service is still used in flows and whether deleted assignment/course rows affect current dashboard decisions

5. `iLearn.Application/Services/AssignmentService.cs`
   - `.IgnoreQueryFilters()` around assignment history/course mapping and dashboard course map
   - Classify historical deleted-course display vs current dashboard/learner status behavior

6. `iLearn.Application/Services/CourseService.cs`
   - `GetCourseLearnersAsync(...)` uses `ignoreQueryFilters: true`
   - enrollment detail and status impact paths use ignored filters
   - Confirm current learner counts/statuses exclude deleted-only assignment links but keep intended history

7. `iLearn.Application/Services/ReportService.cs`
   - `.IgnoreQueryFilters()` in report projection
   - Confirm reports are historical where intended, and current compliance/status reports do not count deleted-only active records incorrectly

8. `iLearn.Infrastructure/Repositories/GenericRepository.cs`
   - Repository-level `ignoreQueryFilters` option
   - No behavior change expected, but document call-site rules for future use

## Out of scope

- Do not remove soft-delete globally
- Do not rewrite repository abstractions
- Do not change historical report semantics unless a concrete current-state bug is found
- Do not clean production data manually as part of this audit
- Do not broaden learner retention policy beyond the ignored-filter issue

## Acceptance Criteria

- Produce a table of every ignored-query-filter usage with:
  - file/member
  - why ignored filters are used
  - whether the result is historical/audit or current-state
  - risk level: `Safe`, `Needs guard`, or `Bug`
  - required code/test action
- For every `Bug` or `Needs guard` item, either:
  - implement a narrowly scoped guard/filter plus focused tests, or
  - create a follow-up PLAN if the change is too broad for this audit
- Add at least one regression test for any fixed current-state decision
- Update docs/memory with a rule of thumb for future `ignoreQueryFilters: true` use
- Leave historical report behavior intact unless explicitly justified

## Verification

Required if code changes are made:

```powershell
dotnet test .\iLearn.Tests\iLearn.Tests.csproj -c Debug --artifacts-path .\artifacts\validate\ignore-query-filter-audit --filter "FullyQualifiedName~<focused-test-filter>"
```

Run broader tests only if a shared policy or report projection changes:

```powershell
dotnet test .\iLearn.Tests\iLearn.Tests.csproj -c Debug --artifacts-path .\artifacts\validate\ignore-query-filter-audit
```

If React/API contract is affected, also run the relevant React validation:

```powershell
cd iLearn.Admin.React
npm run lint
npm run build
```

## Implementation Notes

## Audit Result

| Location | Why ignored filters are used | Classification | Risk | Action |
| --- | --- | --- | --- | --- |
| `iLearn.API/Controllers/DashboardController.cs` / `BuildCourseAttentionAsync` | Fetch course labels for task rows even when the course was later soft-deleted. Dashboard counts/tasks come from normally filtered assignment/link queries. | Display label for current task aggregation | Safe | No code change. Ignored course map only changes label fallback, not task inclusion. |
| `iLearn.API/Controllers/EnrollmentsController.cs` / `GetMyCourses` | Load assignment/content history needed to hide deleted-only assignment enrollments while keeping historical data available. | Current learner visibility/readiness | Safe | Existing guards remain: `CourseContentReadiness` ignores soft-deleted course-content links and `GetEffectiveSchedule` uses only active assignment links. |
| `iLearn.API/Controllers/LearnersController.cs` / `GetProfile` | Load deleted courses/links so learner profile can show historical deleted-course rows. | Mixed history plus current assignment flags | Bug fixed | Now includes `AssignmentLinks.Assignment` and derives `hasActiveAssignment` / `isAssignmentCancelled` from links where both link and assignment are not deleted. Added regression test. |
| `iLearn.Application/Services/AssignmentDashboardService.cs` / `GetDashboardAsync` | Legacy/internal dashboard loaded batch rules with deleted-course support. | Current dashboard counts | Bug fixed | Filters loaded batch to `activeRules` before rule ids, course summaries, learner rows, and total course count. Added regression test. |
| `iLearn.Application/Services/AssignmentDashboardService.cs` / `GetCoursesIncludingDeletedAsync` | Assignment history needs deleted course names and `[Deleted]` markers. | Historical/audit display | Safe | No code change; caller starts from normally filtered assignments and uses deleted courses only for display. |
| `iLearn.Application/Services/AssignmentService.cs` / `BuildAssignmentHistoryAsync` course map | Assignment history keeps deleted course labels. | Historical/audit display | Safe | No code change; assignment rows and links come from normally filtered queries. |
| `iLearn.Application/Services/AssignmentService.cs` / `BuildAssignmentDashboardAsync` course map | Dashboard needs deleted-course label/flag for active assignment rows that reference a deleted course. | Current dashboard with display-only deleted-course metadata | Safe | No code change; current rows/links come from normally filtered assignment/link queries, ignored course map only supplies label and `IsCourseDeleted`. |
| `iLearn.Application/Services/CourseService.cs` / `GetCourseLearnersAsync` | Course learner list needs to distinguish deleted-only links from enrollments that never had assignment links. | Current learner list/status | Safe | Existing guard excludes deleted-only assignment links and computes dates from active links only; covered by `CourseServiceVisibilityTests`. |
| `iLearn.Application/Services/CourseService.cs` / `GetCourseDashboardAsync` | Course dashboard KPI needs the same deleted-only link distinction. | Current course KPI | Safe | Existing visible-enrollment guard excludes deleted-only links; covered by `CourseServiceVisibilityTests`. |
| `iLearn.Application/Services/ReportService.cs` / `ApplyVisibleEnrollmentFilter` | Reports must tell historical links from never-linked legacy enrollments. | Current compliance/transcript/course report visibility | Safe | Existing `allLinks` uses ignored filters, but `activeLinks` requires active link + active assignment before inclusion; covered by `ReportServiceTests`. |
| `iLearn.Infrastructure/Repositories/GenericRepository.cs` / `GetAsync(ignoreQueryFilters)` | Repository escape hatch for audit/history call sites. | Infrastructure helper | Safe | No code change; documented call-site rule in repo memory. |

Rule of thumb added to repo memory: ignored filters may load audit/history data, but current flags/counts/readiness must first derive active rows with explicit `!IsDeleted` checks on both the link and target entity, or use ignored data only for display labels/history markers.

## Implementer Notes

- Fixed learner profile history flags so deleted-only assignment links are reported as cancelled, not active.
- Hardened legacy `AssignmentDashboardService.GetDashboardAsync` so deleted rules from an ignored-filter batch do not affect current dashboard counts/learner rows.
- No API response shape or DB contract changed.

## Verification Run

```powershell
dotnet test .\iLearn.Tests\iLearn.Tests.csproj -c Debug --artifacts-path .\artifacts\validate\ignore-query-filter-audit --filter "FullyQualifiedName~LearnersControllerTests.GetProfile_DeletedOnlyAssignmentLink_IsCancelledNotActive"
dotnet test .\iLearn.Tests\iLearn.Tests.csproj -c Debug --no-restore --no-build --artifacts-path .\artifacts\validate\ignore-query-filter-audit --filter "FullyQualifiedName~AssignmentDashboardService_GetDashboardAsync_ExcludesDeletedRulesFromCurrentCounts" --logger "console;verbosity=normal"
dotnet test .\iLearn.Tests\iLearn.Tests.csproj -c Debug --artifacts-path .\artifacts\validate\ignore-query-filter-audit --filter "FullyQualifiedName~LearnersControllerTests|FullyQualifiedName~AssignmentFlowTests|FullyQualifiedName~CourseServiceVisibilityTests|FullyQualifiedName~ReportServiceTests|FullyQualifiedName~CourseContentReadinessTests"
```

Result: focused learner profile test 1/1 passed; focused assignment dashboard test 1/1 passed; regression set 39/39 passed. Build emitted existing NU1903 warnings for `Microsoft.AspNetCore.Authentication.Negotiate`.

Seed finding from PLAN-184:

- `Enrollments/my-courses` needs ignored filters for deleted-assignment visibility decisions, but current content readiness must ignore soft-deleted `CourseContentItem` links. This is now guarded in `CourseContentReadiness.IsVersionReady(...)`.

Known search command:

```powershell
rg "ignoreQueryFilters\s*:\s*true|IgnoreQueryFilters\(" -g "*.cs"
```

## Reviewer Notes (Claude Code, 2026-07-31)

**ผลรีวิว: VERIFIED** — โค้ดถูกต้อง, audit table ที่ระบุ `Safe` ตรวจสอบแล้วตรงกับโค้ดจริงทุกจุด, full test suite **294/294 ผ่าน** (implementer รันแค่ 39 ตัวใน regression set) มีข้อสังเกต 4 ข้อดังนี้ — ไม่มีข้อไหนบล็อก แต่ข้อ 2 ควรแก้ในไฟล์แผน/log ให้ตรงความจริง

1. **`LearnersController.GetProfile` — ถูกต้องและตรง convention ของ repo แล้ว**
   predicate ใหม่ (`hasAnyLinks && !hasActiveLinks` = cancelled) ตรงกับ guard ที่มีอยู่แล้วอีก 3 จุดเป๊ะ ๆ:
   `CourseService.GetCourseLearnersAsync` / `GetCourseDashboardAsync` (`hasActiveLinks || !hasAnyLinks`) และ
   `EnrollmentsController.GetEffectiveSchedule` (`hadDeletedAssignmentOnly = AssignmentLinks.Any() && activeLinks.Count == 0`)
   include path `"AssignmentLinks.Assignment"` valid (Enrollment.AssignmentLinks → EnrollmentAssignment.Assignment) และ `GenericRepository` เรียก `IgnoreQueryFilters()` บน root query จึงโหลด link/assignment ที่ถูกลบมาให้ตรวจได้จริง

2. **แก้ bug เพิ่มอีก 1 ตัวโดยไม่ได้บันทึกไว้ (positive แต่ควรจด)**
   เงื่อนไข `hasAnyAssignmentLinks` ที่เพิ่มเข้ามา ไม่ได้แก้แค่เคส deleted-only link — มันแก้เคส **enrollment ที่ไม่เคยมี link เลย (self-enroll / legacy)** ด้วย
   ของเดิม: ไม่มี link + ยังไม่จบ + มี StartDate/DueDate ⇒ `isAssignmentCancelled = true` ⇒ หน้า `LearnerProfilePage.tsx:182` ขึ้น badge **Cancelled**
   ของใหม่: ⇒ `false` ⇒ ตกไปที่ branch `selfEnroll` (`LearnerProfilePage.tsx:192`) ซึ่งเป็นพฤติกรรมที่ถูก
   **ยังไม่มี test คุมเคสนี้** — ควรเพิ่ม test "enrollment ไม่มี link เลย ⇒ ไม่ cancelled + ไม่ active"

3. **`AssignmentDashboardService.GetDashboardAsync` เป็น dead code — การแก้ไม่มีผลกับ production**
   `AssignmentsController.cs:77` เรียก `_assignmentService.GetDashboardAsync(id, divisionId, ct)` = `AssignmentService` (ตัวใหม่) ไม่ใช่ตัวนี้ grep ทั้ง solution ไม่พบ call site ของ `IAssignmentDashboardService.GetDashboardAsync` เลย (มีแต่ `ValidateBeforeAssignAsync` / `GetGroupHistoryAsync` ที่ยังถูกเรียก)
   ⇒ ตาราง audit ที่เขียนว่า "Bug fixed / Current dashboard counts" **เกินจริง** — ที่ถูกคือ "dead code hardening" ส่วน dashboard ที่ใช้จริงคือ `AssignmentService.BuildAssignmentDashboardAsync` ซึ่ง audit จัดเป็น `Safe` และตรวจแล้วถูกต้อง (rows/links มาจาก query ที่ filter ปกติ, ignored course map ใช้แค่ label + `IsCourseDeleted`)
   ⇒ แนะนำเปิดแผนใหม่เพื่อลบ method + interface member + test ที่คุม dead code นี้ทิ้ง (นอกขอบเขต PLAN-185)

4. **`if (activeRules.Count == 0) return null;` แทบไม่มีทางเข้าถึง**
   `mainRule` มาจาก `GetByIdAsync` → `FindAsync` ซึ่ง apply query filter (deleted rule = null → return ที่บรรทัด 48 ก่อนแล้ว) และ `LoadBatchAsync` filter ด้วย `AssignmentNo` + division ที่ผ่าน `IsAccessibleToCurrentDivision` มาแล้ว ⇒ `mainRule` อยู่ใน batch เสมอและไม่ deleted เป็น defensive guard ที่ไม่มีผลเสีย เก็บไว้ได้

**หมายเหตุคุณภาพ test:** `InMemoryGenericRepository<T>` ที่เพิ่มใน `LearnersControllerTests.cs` เป็น private nested class ซ้ำกับ helper ที่ `AssignmentFlowTests` ใช้อยู่ และ **ไม่ประมวลผล `includeProperties`** ⇒ การเปลี่ยน include path ไม่ถูก test คุม (ถ้าพิมพ์ path ผิดจะพังตอน runtime เท่านั้น) — รอบนี้ตรวจ path ด้วยมือแล้วว่าถูก แต่ควรรวม helper เป็นตัวเดียวในงานหน้า

**Verification ที่ reviewer รันเอง:**

```powershell
dotnet build iLearn.Tests -o artifacts\verify-plan185   # 0 Error(s), 100 warnings เดิม
dotnet test artifacts\verify-plan185\iLearn.Tests.dll    # Passed! 294/294, 7s
```