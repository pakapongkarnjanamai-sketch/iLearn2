# Master Data And Learner Group Lifecycle Rules

Last updated: 2026-04-30

## Purpose

Master data lifecycle covers objects that support course assignment and reporting but do not have a rich domain-specific status enum. These objects mostly use generic active/delete flags from `BaseEntity`.

## Common BaseEntity Lifecycle

| State/Field | Meaning | Recommended UI Behavior |
| --- | --- | --- |
| Created | Record exists with `CreatedAt` and optional `CreatedBy` | Show in normal lists if not deleted |
| Updated | Record has `UpdatedAt` and optional `UpdatedBy` | Show last modified metadata where useful |
| Active | `IsActive = true` | Selectable/usable unless a domain rule says otherwise |
| Inactive | `IsActive = false` | Not selectable for new work, but historical references remain |
| Soft Deleted | `IsDeleted = true`, `DeletedAt`, `DeletedBy` | Hide from normal lists; keep history/report references |

`IsActive` is a generic support flag for master data. It must not replace richer business statuses such as `Course.Status` or content readiness.

## Division Lifecycle

| State | Meaning | Impact |
| --- | --- | --- |
| Active Division | Division can own categories, groups, roles, assignments, and data scopes | Available in admin scoping and lookup flows |
| Inactive Division | Division should not be used for new setup | Existing historical data remains scoped |
| Deleted Division | Hidden from normal management | Should be blocked or impact-reviewed if referenced |

Recommendations:

- Do not delete referenced divisions without an impact review.
- If division is inactive, keep reports readable and keep existing admins' historical data understandable.

## Category Lifecycle

Categories organize courses under divisions.

| State | Meaning | Impact |
| --- | --- | --- |
| Active Category | Can be selected for new/edit course forms | Courses can be organized here |
| Inactive Category | Should not be selected for new courses | Existing courses remain visible |
| Deleted Category | Hidden from normal selectors | Historical course records should preserve display context |

Recommendations:

- Block deleting categories with active courses, or show an explicit reassignment flow.
- Keep category-to-division resolution available for course forms.

## Course Type Lifecycle

Course Types classify courses, such as Special and General.

| State | Meaning | Impact |
| --- | --- | --- |
| Active Course Type | Available in course forms and filters | Can drive assignment policy such as General auto-assign |
| Inactive Course Type | Not selectable for new courses | Existing courses remain classified |
| Deleted Course Type | Hidden from management | Should be avoided when courses reference it |

Recommendations:

- Treat Course Type names as business configuration, not free-form labels when logic depends on them.
- If General auto-assignment depends on the name `General`, consider a stable key/code field in the future.

## Role And User Lifecycle

| Object | State | Meaning |
| --- | --- | --- |
| Role | Admin | Administrative role, optionally division scoped |
| Role | SuperAdmin | Highest-level administrative role |
| Role | Inactive/deleted | Should not be assigned to new users |
| User | Active | Windows-authenticated user known to Admin UI |
| User | LastLogin updated | User has accessed the system |
| UserRole | Active link | User has a role assignment |

Recommendations:

- Keep RoleType as the authorization source when possible.
- If role names are editable, avoid making authorization depend on display names.
- Define whether inactive users should keep role history but lose menu access.

## Learner Group Lifecycle

Learner Groups are assignment targets. Group membership context uses `Member`; individual people outside the group context use `Learner`.

| State | Meaning | Assignment Impact |
| --- | --- | --- |
| Active Group | Can be selected as assignment target | Members can receive assignments |
| Inactive Group | Should not be selected for new assignment | Existing assignments remain historical |
| Deleted Group | Hidden from normal flows | Related assignments should remain reportable |
| Empty Group | Group exists with no members | Assignment validation should block or warn |
| Has Members | Group contains learner codes | Can be used for bulk assignment |

## Learner Group Member Lifecycle

| Transition | Meaning | Notes |
| --- | --- | --- |
| Add Member | LearnerCode is linked to group | Duplicate members should be prevented |
| Remove Member | LearnerCode is removed from group | Should not remove historical enrollment records |
| Add Members To Existing Assignments | Members may be added to selected related assignments | Allowed assignment statuses are Completed, InProgress, Upcoming, Expired |

Recommendations:

- When adding members to existing assignments, explain whether Completed assignments create new enrollments, reset, or skip already completed learners.
- Keep selection trays visible in group member workflows to prevent lost selections across paged grids.

## Learner Group Category Lifecycle

Learner Group Categories organize groups in a tree.

| State/Field | Meaning |
| --- | --- |
| ParentId | Parent category in the tree |
| Children | Child categories |
| Path | Materialized ancestor path, for example `/12/45/` |
| Depth | Tree level, root starts at 0 |
| Active | Category can be used for grouping |
| Deleted | Hidden from normal tree |

Recommendations:

- Validate moves so categories cannot become their own ancestor.
- Keep maximum depth rules in one service-level constant.
- Decide whether deleting a category moves groups to parent, blocks deletion, or deletes descendants.

## FileStorage Lifecycle

| State | Meaning | Notes |
| --- | --- | --- |
| Stored | Original uploaded file bytes and metadata exist | Used for publish and auto-prepare |
| Referenced | ContentItem points to FileStorageId | Do not hard-delete while referenced |
| Hard Deleted | Binary data is removed | Allowed when content item is deleted and no other reference needs it |

Recommendations:

- Keep package bytes for content that may need auto-prepare.
- Add operational monitoring for large stored packages and orphaned file storage records.

## Admin Activity Lifecycle

Admin Activity is an append-only audit-style record.

| State | Meaning |
| --- | --- |
| Created Activity | Admin action was recorded |
| Historical Activity | Activity remains for reporting and traceability |

Recommendations:

- Do not treat Admin Activity as normal mutable CRUD data.
- Prefer append-only behavior; corrections should be new records or metadata, not destructive edits.

## Recommendations Summary

1. Add consistent impact checks before deleting or deactivating referenced master data.
2. Expose `IsActive` in grids, but pair it with domain-specific helper text for objects where active has consequences.
3. Keep lookup endpoints filtered to active/selectable records, while report/detail endpoints can include inactive/deleted references when needed.
4. Avoid using display names as business keys for Course Type and Role.
5. Define deletion behavior for Learner Group Category trees before adding bulk category maintenance tools.
