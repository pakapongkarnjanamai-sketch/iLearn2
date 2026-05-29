# iLearn Lifecycle Overview

Last updated: 2026-04-30

## Purpose

เอกสารชุดนี้เป็นแผนที่ lifecycle กลางของ iLearn2 สำหรับใช้คุยงาน ออกแบบ UI/API และตรวจสอบ business rule ให้ตรงกันทั้ง Admin UI, Learner UI, API และ Domain model

ไฟล์นี้สรุปภาพรวมและข้อวิเคราะห์ ส่วนรายละเอียดรายกลุ่มอยู่ในไฟล์แยกตามตารางด้านล่าง

## Document Map

| Area | Document | Scope |
| --- | --- | --- |
| Course | `COURSE-LIFECYCLE-RULES.md` | Course status: Draft, Open, Closed, Retired |
| Content + Course Version | `CONTENT-LIFECYCLE-RULES.md` | Content item publish/readiness, CourseVersion activation, learner version policy |
| Assignment + Enrollment | `ASSIGNMENT-ENROLLMENT-LIFECYCLE-RULES.md` | Bulk assign, assignment status, enrollment progress/reset/snapshot |
| SCORM Runtime | `SCORM-RUNTIME-LIFECYCLE-RULES.md` | SCORM import/runtime status normalization, commit, rollup |
| Master Data + Learner Groups | `MASTER-DATA-LIFECYCLE-RULES.md` | Division, Category, Course Type, Role, User, Learner Group, FileStorage, audit |
| All Status Meanings | `STATUS-DEFINITIONS.md` | Central status and state flag dictionary |
| System Terms | `SYSTEM-DICTIONARY.md` | Product glossary and naming rules |
| Improvement Plan | `LIFECYCLE-IMPROVEMENT-PLAN.md` | Prioritized roadmap from open lifecycle recommendations |

## Lifecycle Coverage Map

| Object | Stored State | Computed State | Primary Rule Owner |
| --- | --- | --- | --- |
| Course | `Course.Status`, legacy `IsActive` | `CanAssign`, `CanLearnerAccess` | `CourseService`, `CourseVersionService` |
| Course Version | `CourseVersion.IsActive` | readiness from linked content items | `CourseVersionService`, `CourseContentReadiness` |
| Content Item | `ContentItem.IsActive`, `URL`, `LaunchHref`, `SchemaVersion` | Published, Not Ready, Ready, unused/needed | `ContentItemsController`, `CourseContentReadiness` |
| Assignment | dates, soft-delete, assignment batch number | Completed, Upcoming, Expired, InProgress | `AssignmentDashboardService.CalculateStatus` |
| Enrollment | `IsCompleted`, `Progress`, `ResetAt`, enrolled version | NotStarted, InProgress, Completed, Upcoming, Overdue | `LearningLogsController`, `EnrollmentsController`, `CourseService` |
| EnrollmentAssignment | schedule snapshot, completion snapshot | assignment historical progress | `CourseAssignmentService`, `LearningLogsController` |
| SCORM Runtime State | SCORM status fields and CMI snapshot | player status, legacy log status, enrollment rollup | `ScormRuntimeStateService`, `LearningLogsController` |
| Learner Group | generic active/delete fields, member rows | related assignment preview status | `LearnerGroupService` |
| Master Data | generic `IsActive`, soft-delete fields | selectable/not selectable | generic CRUD controllers/services |

## Cross-Cutting Rules

1. Course availability must use `Course.Status`, not only `IsActive`.
2. New assignments must use Open courses with a ready active version.
3. Learner access is enrollment-based: assigned learners can continue Open or Closed courses when their enrolled version is ready.
4. Content readiness is derived, not a single stored status. A content item is ready only when it is published and has launch metadata.
5. Assignment status is computed from completion and schedule. It should not be manually edited.
6. Enrollment completion is a rollup from content item logs/runtime state for the enrolled version.
7. Reset keeps history but moves active progress to a new attempt boundary using `ResetAt`.
8. Soft-deleted records remain important for history, reports, and audit.

## Analysis And Recommendations

### 1. Retired Course Rule Finalized as Hard Block

The Retired Course Policy has been finalized as a strict **Hard Block** to preserve learner progress and data integrity. If open enrollments exist, the system blocks retiring the course. Admins must close the course first, then wait for completion or cancel/delete the related active enrollments. The documentation, API responses, and Admin confirmation flows are aligned to this safe default.

### 2. Separate Assignment Expired From Learner Overdue

The backend now treats these as different contract values instead of UI-only synonyms.

- Assignment batch status stays `Expired`.
- Learner and enrollment past-due status uses `Overdue`.
- Shared Admin helpers should now expect canonical learner payloads to emit `Overdue`, not learner-side `Expired`.

### 3. Standardize Not Started And Pending

Assignment learner rows use `Pending`; course learner rows use `NotStarted`; UI often displays `Not Started`. The difference is understandable but easy to misuse.

Current default: use `NotStarted` as the normalized API/computed key for learner progress and display `Not Started`. Keep `Pending` only in domains that truly mean waiting, such as policy evaluation.

### 4. Avoid Using Generic IsActive As A Business Status

`IsActive` has different meanings across entities: Open course compatibility, published content item, active course version, selectable master data, and generic enabled flag. This is the largest source of ambiguity.

Current direction: every API response that exposes `IsActive` should also expose a domain-specific label or capability when the domain has richer rules. Course DTOs already expose `StatusName`, `CanAssign`, and `CanLearnerAccess`; content/version DTOs now also expose `PublishState`, `IsPublished`, and `VersionState`.

### 5. Content Unpublish Paths Now Share One Guard And Preview

Single and bulk unpublish paths now run through the same shared publication policy. Bulk actions expose preview counts for eligible versus blocked items, and blocked content linked to course versions is skipped instead of being silently unpublished by a separate code path.

Recommended follow-up: if operations later need a destructive override, add an explicit force flow as a separate path rather than weakening the default safe behavior.

### 6. Course Version Readiness Should Stay Separate From Course Status

Some helpers answer "ready active Open course", while version activation and course opening need "version readiness" independent of current course status.

Recommended default: use `GetVersionReadinessAsync` for pre-open checks and reserve `HasReadyActiveVersion` for assignment/learner visibility checks where Open status is required.

### 7. Assignment With No Enrollments Can Look In Progress

`CalculateStatus` returns `InProgress` when there are no enrollments and no future/past schedule boundary. This is technically current behavior, but it may read as misleading in Admin history.

Recommended default: keep the current value for compatibility, but consider adding a separate dashboard count or label for `No Learners` if admins need to distinguish it.

### 8. SCORM Exam Success Now Uses One Rule Across Player And Rollup

Exam/Learn completion evaluation is now centralized in `ScormContentStatusPolicy` and shared by runtime-to-log mapping plus player status. Exam content with only `completed` but no `passed` now remains incomplete consistently across player display and enrollment rollup.

Recommended follow-up: keep regression coverage for `completed + unknown`, `completed + failed`, and SCORM 1.2 stale alias combinations.

### 9. Due Soon Now Uses One Shared Seven-Day Threshold

`Due Soon` is now centralized as a shared seven-day window in `AssignmentStatusKeys.DueSoonWindowDays`. `DashboardController` and the Admin assignment quick filters both use the same cutoff and exclude completed work the same way.

Recommended follow-up: if operations ever need tenant-specific behavior, move the same shared owner to configuration rather than reintroducing local literals.

### 10. LearningLog Defaults Are Now Safe By Default

`LearningLog.Status` now defaults to `incomplete` and `Progress` defaults to `0`. The active creation path in `LearningLogsController` still sets explicit values, and focused tests now guard the safer constructor defaults.

Recommended follow-up: keep new learning-log creation paths explicit and keep regression coverage around default constructor behavior.

### 11. Completed Learner History Retention Now Has A Named Owner

Completed learner history visibility is now owned by `EnrollmentVisibilityPolicy`. `EnrollmentsController.GetMyCourses` no longer uses an implicit `AddMonths(-1)` literal, and focused tests guard the one-month retention boundary for recently completed enrollments.

Recommended follow-up: if learner history retention becomes tenant-specific, lift the same policy into configuration instead of duplicating date math in query code.

## Recommended Next Work

1. Decide the Retired course policy and update Course Lifecycle plus Admin confirmation text if needed.
2. Add tests for status dictionary invariants that are not yet locked by the current DTO contract tests: course status transitions, assignment status priority, content readiness, and remaining SCORM precedence rules.
