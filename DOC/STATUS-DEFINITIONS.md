# iLearn Status Definitions

Last updated: 2026-04-30

## Purpose

ไฟล์นี้เป็นทะเบียนกลางของ status, state flag, และ computed status ที่ใช้ใน iLearn2 เพื่อให้ทีมใช้คำเดียวกันทั้งเอกสาร, UI, API, service, report, และ test

## Status Principles

1. Use exact API keys in code and payloads.
2. Use readable labels in UI, but keep mappings explicit.
3. Do not infer rich business meaning from `IsActive` alone.
4. Separate persisted state from computed display state.
5. Keep historical states reportable even after soft delete or reset.

## Course Status

Source: `CourseStatus` enum on `Course.Status`

| Status | Value | Meaning | New Assignments | Existing Learner Access | Notes |
| --- | --- | --- | --- | --- | --- |
| Draft | 0 | Course is being prepared and is not ready for assignment | No | No normal learner access | Requires ready active version before Open |
| Open | 1 | Course is available for assignment and learning | Yes | Yes | `Course.IsActive` should be true for compatibility |
| Closed | 2 | Course is closed to new assignments | No | Yes, existing assigned learners can continue | Soft close |
| Retired | 3 | Course is permanently withdrawn from learning access | No | No active learner launch | Current service blocks retire while open enrollments exist |

Derived course capabilities:

| Capability | Rule | Meaning |
| --- | --- | --- |
| CanAssign | `Status == Open` | Course can appear in new assignment pickers |
| CanLearnerAccess | `Status == Open || Status == Closed` | Existing assigned learners can launch when version is ready |
| IsRetired | `Status == Retired` | Course is withdrawn from active learning |

## Course Version States

| State | Source | Meaning |
| --- | --- | --- |
| Active Version | `CourseVersion.IsActive = true` | Current version for course launch/assignment |
| Inactive Version | `IsActive = false` | Saved version not currently active |
| Ready Version | All linked content items are ready and count > 0 | Can be activated and used by learners |
| Not Ready Version | No content or at least one readiness issue | Cannot be activated safely |
| No Active Version | No version has `IsActive = true` | Course cannot be opened |

Course version contract fields:

| Field | Meaning |
| --- | --- |
| IsActive | Compatibility active-version flag |
| VersionState | Readable version lifecycle label: `Active` or `Inactive` |

## Course Version Learner Policy

Source: `CourseVersionLearnerPolicy`

| Policy | Value | Meaning |
| --- | --- | --- |
| NewLearnersOnly | 0 | Existing learners stay on their enrolled version; new learners use the new active version |
| MoveNotStarted | 1 | Not-started eligible learners move to the new version |
| ResetInProgress | 2 | Eligible open learners move to the new version and progress is reset |

Policy impact labels:

| Label | Meaning |
| --- | --- |
| Pending | UI is checking learner impact or waiting for policy selection |
| No Impact | No eligible open learners will be moved/reset |
| Action Required | Eligible open learners exist and admin must choose a policy |

## Content Item Status And Readiness

| Status/State | Source | Meaning |
| --- | --- | --- |
| Queued Upload | UI-only local state | File selected but not saved/processed yet |
| Draft / Unpublished | `ContentItem.IsActive = false` | Content exists but is not published |
| Published | `ContentItem.IsActive = true` | Content has been made public/launchable at least at publish level |
| Ready | Derived by `CourseContentReadiness` | Published plus launch URL and SCORM launch metadata are present |
| Not Ready | Derived by readiness check | Missing record, unpublished, missing URL, or missing SCORM launch file |
| Unused Published | Maintenance analysis | Published but not linked to active course versions |
| Should Publish | Maintenance analysis | Unpublished but linked to active course versions |
| Deleted | `IsDeleted = true` | Hidden from normal content library; history may still reference it |

Content item contract fields:

| Field | Meaning |
| --- | --- |
| IsActive | Compatibility publish flag used in older clients |
| IsPublished | Semantic alias for publish state |
| PublishState | Readable content publish label: `Published` or `Unpublished` |

Content readiness issue values:

| Reason | Meaning |
| --- | --- |
| content item record is missing | Linked record cannot be found |
| content item is not published | `IsActive = false` |
| launch URL is missing | No launch storage URL/path |
| SCORM launch file is missing | Missing `LaunchHref` and URL is not a direct launch URL |
| original SCORM package is missing | Auto-prepare cannot find stored package bytes |
| automatic content preparation failed | Server attempted recovery but failed |

## Assignment Status

Source: `AssignmentDashboardService.CalculateStatus`

| Status | Meaning | Computed Rule | UI Notes |
| --- | --- | --- | --- |
| Completed | Assignment batch is finished | Has enrollments and all are completed/snapshot-completed | Highest priority |
| Upcoming | Assignment starts in the future | `StartDate > currentDate` and not completed | Scheduled |
| Expired | Due date is past and assignment is not completed | `DueDate < currentDate` and not completed | May be displayed as Overdue if intentionally mapped |
| InProgress | Current/default assignment state | Not Completed, not Upcoming, not Expired | Also used when no dates are set |

Related non-status filters:

| Term | Meaning |
| --- | --- |
| Due Soon | Not completed and due within the shared 7-day window defined by `AssignmentStatusKeys.DueSoonWindowDays` |
| Overdue | Learner/enrollment past-due contract value; do not reuse for assignment batch status |

## Assignment Learner Progress Status

Source: `AssignmentDashboardDto.LearnerProgressDto.Status`

| Status | Meaning | Rule |
| --- | --- | --- |
| NotStarted | Learner has not started this assignment/course row | Not completed and progress is 0 |
| InProgress | Learner has started but not completed | Not completed and progress > 0 |
| Completed | Learner completed this assignment/course row | Completed or snapshot-completed |

Canonical API output should use `NotStarted`. Do not reintroduce `Pending` as a learner progress contract key.

## Course Learner Status

Source: `CourseLearnerDto.Status`

| Status | Meaning | Rule |
| --- | --- | --- |
| Completed | Learner completed the enrollment | `IsCompleted = true` |
| Upcoming | Effective start date is in the future | `StartDate > now` |
| Overdue | Effective due date is in the past | `DueDate < now` and not completed |
| InProgress | Learner has progress but is not completed | `Progress > 0` |
| NotStarted | Learner has not started | Not completed, not scheduled/expired, progress 0 |

Recommended UI labels:

| API Key | Display Label |
| --- | --- |
| NotStarted | Not Started |
| InProgress | In Progress |
| Completed | Completed |
| Upcoming | Upcoming |
| Overdue | Overdue |

## Enrollment State Flags

| Field/State | Meaning |
| --- | --- |
| IsCompleted | Enrollment has completed the enrolled version |
| CompletedDate | Completion timestamp for current enrollment attempt |
| Progress | Course/content completion progress, 0-100 |
| EnrolledCourseVersion | Version assigned to the learner enrollment |
| ResetAt | Attempt boundary; logs older than this are ignored for active progress |
| IsReadOnly | Player mode without enrollment or preview-only access |

## EnrollmentAssignment Snapshot Flags

| Field | Meaning |
| --- | --- |
| SnapshotCompleted | Historical completion state for an assignment link |
| SnapshotCompletedDate | Historical completion date for that link |
| SnapshotProgress | Historical progress for that link |

## SCORM Runtime Status

SCORM status values are normalized but still come from package runtime behavior.

| Status | Applies To | Meaning | Rollup Impact |
| --- | --- | --- | --- |
| passed | SCORM lesson/success, log, player | Learner passed success criteria | Complete/pass |
| completed | SCORM lesson/completion, log, player | Learner completed activity | Complete for Learn; Exam without `passed` remains incomplete under `ScormContentStatusPolicy` |
| failed | SCORM lesson/success, log, player | Learner failed success criteria | Not passed; takes precedence over completed |
| incomplete | SCORM lesson/completion, log, player | Activity is not complete | Not complete |
| not attempted | SCORM lesson/completion | Activity not started | Not complete |
| browsed | SCORM lesson status | Learner browsed content | Legacy mapping treats as completed |
| unknown | SCORM success status | Success is unknown | Does not override passed/failed |

SCORM version values:

| Value | Meaning |
| --- | --- |
| 1.2 | SCORM 1.2 runtime model |
| 2004 | SCORM 2004 runtime model |

Player content item status values:

| Status | Meaning |
| --- | --- |
| passed | Passed according to success/lesson/log status |
| completed | Completed according to completion/lesson/log status for Learn outcomes that count as complete |
| failed | Failed according to success/lesson/log status |
| incomplete | Not complete or not passed enough to count |

Legacy learning log status values:

| Status | Meaning |
| --- | --- |
| passed | Counts as complete in enrollment rollup |
| completed | Counts as complete in enrollment rollup for outcomes that `ScormContentStatusPolicy` allows to complete |
| failed | Does not count as complete |
| incomplete | Does not count as complete |

## Generic Master Data State Flags

Source: `BaseEntity`

| Field/State | Meaning | Notes |
| --- | --- | --- |
| IsActive | Generic active/selectable flag | Meaning varies by entity |
| IsDeleted | Soft delete flag | Hidden from normal lists but may remain in history |
| DeletedAt | Delete timestamp | Audit/support field |
| DeletedBy | User who deleted the record | Audit/support field |
| CreatedAt | Create timestamp | Audit/support field |
| UpdatedAt | Last update timestamp | Audit/support field |

## Role Type

Source: `RoleType`

| RoleType | Meaning |
| --- | --- |
| Admin | Administrative user, usually division-scoped |
| SuperAdmin | Highest administrative role, can access broader system management |

## Terms To Avoid Or Map Carefully

| Term | Risk | Recommended Handling |
| --- | --- | --- |
| Active Course | Ambiguous with `IsActive` | Use Open/Closed/Draft/Retired for course lifecycle |
| Inactive Course | Ambiguous | Use Draft, Closed, or Retired as applicable |
| Pending | Can mean not started or waiting for async work | Do not use as a learner progress API key; keep only where the domain truly means waiting |
| Overdue | Separate from assignment `Expired` | Use for learner/enrollment work that is past due |
| Ready | Derived from multiple fields | Do not persist unless a clear cache/invalidating strategy exists |
| Published | Content publish status, not course publish status | Use Open for course availability |

## Recommended Canonical Keys

| Domain | Canonical API Keys | Display Labels |
| --- | --- | --- |
| Course | Draft, Open, Closed, Retired | Draft, Open, Closed, Retired |
| Assignment | Completed, Upcoming, Expired, InProgress | Completed, Upcoming, Expired/Overdue, In Progress |
| Learner progress | NotStarted, InProgress, Completed, Upcoming, Overdue | Not Started, In Progress, Completed, Upcoming, Overdue |
| Content readiness | Published, NotReady, QueuedUpload, Ready | Published, Not Ready, Queued Upload, Ready |
| SCORM/player | passed, completed, failed, incomplete | Passed, Completed, Failed, Incomplete |
