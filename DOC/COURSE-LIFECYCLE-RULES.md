# Course Lifecycle Rules

## Purpose

Course lifecycle status must separate assignment policy from learner access. In an internal LMS, closing a course should stop new assignments without interrupting learners who already received the learning task.

## Status Model

| Status | Meaning | New Assignments | Existing Enrollments | Learner History | Admin Reporting |
| --- | --- | --- | --- | --- | --- |
| Draft | Course is being prepared and is not ready for assignment. | No | No normal learner access | Not applicable | Yes |
| Open | Course is available for assignment and learning. | Yes | Yes | Yes | Yes |
| Closed | Course no longer accepts new learners. | No | Yes, existing assigned learners continue until completion or due date. | Yes | Yes |
| Retired | Course is permanently withdrawn from learning access. | No | No active learning access. Historical records remain. | Yes | Yes |

## Transition Rules

### Draft to Open
- Requires one active course version.
- Active version must contain at least one ready content item.
- Ready content requires a published content item, launch URL, and SCORM launch file when applicable.

### Open to Closed
- Allowed as a soft close.
- Stops new manual, group, bulk, and automatic assignments.
- Existing enrollments remain visible to learners and can continue launching content.
- Admin confirmation must show learner impact counts before applying the status.

### Closed to Open
- Requires the same readiness checks as Draft to Open.
- Restores course availability for new assignments.
- Does not change existing learner progress.

### Open or Closed to Retired
- Blocks new assignments and active learner launch access.
- Should be used only when the course is no longer valid.
- If open enrollments exist, admins should resolve them before retiring or explicitly accept that active learning access will stop.
- Reports, logs, enrollments, and learning history must remain available.

## Learner Access Rules

- Learners can see and launch assigned enrollments when the course status is Open or Closed.
- Learners cannot launch Draft or Retired courses.
- Completed learners may still see recent history according to existing learner dashboard retention rules.
- Player access should be based on the learner's enrollment and enrolled version, not only on whether the course accepts new assignments.

## Assignment Rules

- Only Open courses can be assigned to learners.
- Assignment course pickers, bulk assignment validation, automatic general-course assignment, and group assignment flows must filter to Open courses.
- Closed courses remain visible to admins in course management and reports, but not in new-assignment pickers.

## Admin UI Rules

- Primary action labels:
  - Open course: show `Close Course`.
  - Closed or Draft course: show `Open Course` when readiness allows.
  - Retired course: show `Retired` state and require a separate restore flow if supported.
- Close confirmation must say: `Close Course will stop new assignments. Existing assigned learners can continue learning until completed or due date.`
- Impact preview should include Not Started, In Progress, Completed, Active Assignments, and Future Assignments.
- Use `Open`, `Closed`, `Draft`, and `Retired` consistently instead of ambiguous `Active`/`Inactive` copy for courses.

## Compatibility Rule

`Course.IsActive` is legacy compatibility only. New business decisions should use `Course.Status` and status-derived capabilities such as `CanAssign` and `CanLearnerAccess`.
