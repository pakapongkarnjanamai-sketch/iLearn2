# SCORM Runtime Lifecycle Rules

Last updated: 2026-04-30

## Purpose

SCORM lifecycle covers package import, runtime commit, status normalization, player display, learning logs, and enrollment rollup. The system supports SCORM 1.2 and SCORM 2004.

## Package Import Lifecycle

| Step | Rule | Output |
| --- | --- | --- |
| Upload validation | File must be a valid ZIP package within configured limits | Stored in `FileStorage` |
| Manifest discovery | Package must contain exactly one `imsmanifest.xml` | Manifest path |
| Version detection | Manifest must resolve to SCORM 1.2 or SCORM 2004 | `SchemaVersion` |
| Launch discovery | Manifest must identify a launchable webcontent/SCO resource | `LaunchHref` |
| Extraction | Archive entries must remain under package root and stay within size limits | Extracted folder |
| Publish metadata | Content item receives folder URL, launch href, schema version, and active flag | Ready content item |

Unsupported packages, missing manifests, multiple manifests, unsafe paths, missing launch files, and unsupported SCORM versions must fail before content becomes ready.

## Runtime Commit Lifecycle

1. Learner UI requests player info for an enrollment/course.
2. API resolves the trusted learner code from internal signed headers.
3. API validates that the enrollment belongs to the learner.
4. Runtime commit validates content item ids against the enrolled course version.
5. Runtime state is upserted by enrollment and content item.
6. Runtime fields are normalized and evaluated by `ScormContentStatusPolicy` into legacy learning-log status/progress.
7. Enrollment rollup recalculates progress and completion.
8. Assignment snapshots synchronize from the enrollment after rollup.
9. Admin/Learner caches are invalidated.

Completed enrollments can still accept final runtime commits when the endpoint explicitly allows completed enrollment sync. This protects final session time, score, and runtime state from being lost.

## Runtime State Fields

| Field | SCORM Source | Meaning |
| --- | --- | --- |
| ScormVersion | Package/runtime version | Normalized to `1.2` or `2004` when recognized |
| LessonLocation | `cmi.core.lesson_location` or `cmi.location` | Resume bookmark |
| SuspendData | `cmi.suspend_data` | Resume state payload |
| LessonStatus | `cmi.core.lesson_status` | SCORM 1.2 lesson status |
| CompletionStatus | `cmi.completion_status` | SCORM 2004 completion status |
| SuccessStatus | `cmi.success_status` | SCORM 2004 pass/fail status |
| RawScore | `cmi.core.score.raw` or `cmi.score.raw` | Raw score from package |
| SessionTime | `cmi.core.session_time` or `cmi.session_time` | Current session duration |
| TotalTime | `cmi.core.total_time` or `cmi.total_time` | Package-reported total duration |
| Entry | `cmi.core.entry` or `cmi.entry` | Resume/ab-initio entry mode |
| Exit | `cmi.core.exit` or `cmi.exit` | Exit mode such as suspend/logout/normal |
| CmiSnapshotJson | Full CMI snapshot | Diagnostic persisted input, not returned to player responses |

## SCORM Status Meanings

| Status | Applies To | Meaning | Completion Impact |
| --- | --- | --- | --- |
| passed | Lesson/success/log/player | Learner satisfied the content success criteria | Complete/pass |
| completed | Lesson/completion/log/player | Learner completed required activity | Complete for Learn content; Exam may still require passed |
| failed | Lesson/success/log/player | Learner failed success criteria | Not passed; takes precedence over completed |
| incomplete | Lesson/completion/log/player | Activity is not complete | Not complete |
| not attempted | Lesson/completion | Activity has not started | Not complete |
| browsed | Lesson status | Learner browsed content; treated like completed by legacy mapping | Complete for Learn content |
| unknown | Success status | Success is not known | Does not override passed/failed |

`ScormContentStatusPolicy` is the shared rule owner for Learn vs Exam completion evaluation. Exam content with `completed` but without `passed` remains `incomplete` for player display and enrollment rollup.

## Normalization Rules

### SCORM 1.2

- `cmi.core.lesson_status` is authoritative.
- `passed` maps to success passed and completion completed.
- `failed` maps to success failed and completion completed.
- `completed` and `browsed` map to completed.
- `incomplete` and `not attempted` remain incomplete/not attempted.

### SCORM 2004

- `completion_status` and `success_status` are evaluated separately.
- `success_status=failed` takes precedence over `completion_status=completed`.
- `success_status=passed` marks success.
- `completion_status=completed` marks completion when success is not failed.

## Player Status Rules

Player content item status and runtime-to-log mapping now use the same `ScormContentStatusPolicy` priority:

1. `failed` when success, lesson, or log status is failed.
2. `passed` when success, lesson, or log status is passed.
3. `completed` when completion/lesson/log says completed or browsed, except Exam content without passed is shown as incomplete.
4. `incomplete` otherwise.

Completion progress is 100 only for `passed` or `completed`. Activity progress can show partial activity for incomplete Learn content when score/progress exists.

## Runtime State Preservation Rules

- Placeholder incomplete/not attempted commits should not overwrite terminal passed/completed/failed status.
- Unknown success should not overwrite final passed/failed success.
- Zero-like duration should not overwrite meaningful duration.
- `ab-initio` should not overwrite `resume` when bookmark or suspend data exists.
- Zero score should not overwrite meaningful score when the commit looks like a placeholder outcome.

## Enrollment Rollup Rules

- Runtime commits are mapped to legacy learning log statuses: `passed`, `completed`, `failed`, or `incomplete`.
- Enrollment progress counts content item logs whose status is `passed` or `completed`.
- Enrollment becomes completed only when all content items in the enrolled version have complete logs.
- Logs before `Enrollment.ResetAt` are ignored for active progress.

## Recommendations

Completed on 2026-04-30: Exam completion evaluation is centralized in `ScormContentStatusPolicy` and is now shared by `LearningLogsController` and `EnrollmentsController`.

1. Keep SCORM 1.2 `lesson_status` precedence covered by regression tests.
2. Keep CMI snapshots out of player responses; use them only for diagnostics and persisted commit input.
3. Add documentation or UI copy for packages that report `completed` with `success_status=unknown`, especially for exams.
4. Keep package import errors user-actionable: missing manifest, unsupported version, unsafe path, missing launch file, oversized archive.
