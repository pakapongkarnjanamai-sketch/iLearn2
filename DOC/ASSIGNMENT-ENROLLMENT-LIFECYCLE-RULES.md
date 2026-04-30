# Assignment And Enrollment Lifecycle Rules

Last updated: 2026-04-30

## Purpose

Assignment lifecycle controls distribution of courses to learners. Enrollment lifecycle controls each learner's active progress, completion, reset, and historical assignment snapshots.

## Assignment Model

An assignment batch is represented by one or more `Assignment` rows sharing an `AssignmentNo`. Each row can represent one course rule in the batch. Learner progress is linked through `EnrollmentAssignment` rows.

Assignments do not store a single status field. Assignment status is computed from schedule and related enrollment completion.

## Assignment Status Model

| Status | Computed Rule | Meaning | Notes |
| --- | --- | --- | --- |
| Completed | Has enrollments and all related enrollments or snapshots are completed | The assignment batch is finished | Takes priority over schedule |
| Upcoming | StartDate is in the future | Assignment is scheduled but not started | Takes priority over Expired in contradictory date cases |
| Expired | DueDate is in the past and not completed | Assignment period is over with incomplete work | UI may display this as Overdue if mapped intentionally |
| InProgress | Not completed, not future, not expired | Assignment is currently actionable or has no schedule boundary | Current default when no dates are set |

Priority is: Completed, then Upcoming, then Expired, then InProgress.

## Assignment Creation Rules

1. Courses must be Open and accessible in the current admin division.
2. Courses must have a ready active version.
3. Target learners can come from selected learner codes or a learner group.
4. StartDate must be on or before DueDate when both exist.
5. Assignment rows are created inside a transaction with enrollment creation.
6. Existing in-progress or completed enrollments require explicit confirmation before reset/reassign.

## Conflict Types

| Conflict | Rule | Default Behavior |
| --- | --- | --- |
| InProgressConflict | Existing enrollment/link is not completed and has progress | Require confirmation before resetting |
| CompletedConflict | Existing enrollment/link is completed | Require confirmation before reassigning |

When confirmed, reassignment can reset the active `Enrollment`. Completed history is preserved through `EnrollmentAssignment` snapshot fields before reset.

## Assignment Update Rules

### Extend Due Date

- New due date must be after assignment start date when a start date exists.
- All rules in the assignment batch receive the new due date.
- Active incomplete enrollment links receive the new due date.
- Completed or snapshot-completed links keep their completion history.

### Deleted Course Awareness

- Assignment history can include soft-deleted courses.
- Admin reports should show deleted course names with a clear marker, not hide the historical assignment.

## Enrollment Lifecycle

| State | Stored/Derived From | Meaning | Learner Access |
| --- | --- | --- | --- |
| NotStarted | `IsCompleted = false`, progress 0, no started signal | Learner has not begun the enrolled version | Yes when course/version/schedule allow |
| Pending | Legacy assignment-detail label for not started | Same practical meaning as NotStarted in assignment learner rows | Yes when allowed |
| InProgress | `IsCompleted = false` and progress/log/started signal exists | Learner has begun but not completed | Yes when allowed |
| Completed | `IsCompleted = true` | Learner completed the enrolled version | Visible in recent history; launch behavior depends on UI/policy |
| Upcoming | Effective start date is in the future | Enrollment exists but is not yet visible/actionable to learner | No normal learner card visibility |
| Overdue | Effective due date is in the past and not completed | Learning window has closed | No normal learner card visibility |
| Reset | `ResetAt` set and progress cleared | New attempt boundary; older logs ignored for active progress | Yes when allowed |

## Enrollment Creation Rules

1. Enrollment key is learner plus course.
2. New enrollment uses the course's active version as `EnrolledCourseVersion`.
3. New enrollment starts with `IsCompleted = false`, progress 0, and assignment start/due dates when provided.
4. If an existing enrollment is reassigned or version changes, it is reset.
5. Reset clears progress, score, total time, completion date, and sets `ResetAt`.

## Enrollment Completion Rollup

1. Runtime or progress updates write content item learning logs.
2. Logs before `ResetAt` are ignored for active progress.
3. A content item counts as complete when its log status is `passed` or `completed`.
4. Enrollment progress is completed count divided by content item count.
5. When every content item is complete, enrollment becomes completed, progress becomes 100, and CompletedDate is set.
6. Assignment snapshot fields are synchronized from the current enrollment after rollup.

## Snapshot Rules

`EnrollmentAssignment` snapshots protect assignment history from later resets.

| Field | Meaning |
| --- | --- |
| SnapshotCompleted | Whether the assignment link was completed at snapshot time |
| SnapshotCompletedDate | Completion date captured before reset/reassign |
| SnapshotProgress | Progress captured before reset/reassign |

Snapshots should be treated as historical assignment truth. Current enrollment state is the active learner attempt.

## Learner UI Visibility Rules

A learner course appears only when all of these are true:

1. Enrollment belongs to the trusted learner code.
2. Course status is Open or Closed.
3. Enrolled version is ready.
4. Effective schedule is visible.
5. Incomplete enrollments are within start/due window.
6. Completed enrollments remain visible only within the current recent-history retention window.

Current retention behavior keeps completed enrollments visible when `CompletedDate` is within the one-month window defined by `EnrollmentVisibilityPolicy.CompletedHistoryRetentionMonths`.

## Recommendations

1. Normalize assignment batch API keys to `Completed`, `Upcoming`, `Expired`, and `InProgress`; normalize learner/enrollment API keys to `NotStarted`, `InProgress`, `Completed`, `Upcoming`, and `Overdue`.
2. Treat `Pending` and learner-side `Expired` only as legacy aliases in shared UI mapping while older clients or cached payloads are still being phased out.
3. Keep completed learner history retention owned by `EnrollmentVisibilityPolicy`, and move that same owner to configuration if the retention window ever needs to vary by tenant or environment.
4. Consider a computed `NoLearners` display bucket for assignment batches with no enrollments and no schedule boundary, while keeping `InProgress` as the contract value until migration.
5. Add tests for reassigning completed learners to ensure snapshots remain stable after later progress changes.
