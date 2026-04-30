# Generic CRUD Lookup Audit

Last updated: 2026-04-30

## Completed In This Round

- `Courses` lookup already separated previously:
  - `api/Assignments/lookup-courses` for assignable course selectors
  - `api/Courses/lookup` for general course selectors
- `CourseTypes` lookup separated from `admin/CourseTypesCRUD`:
  - Reused existing `api/Courses/course-types-lookup`
  - Rewired shared course-filter helpers, assignment course filters, and Courses quick-filter loading
- `Content library` lookup separated from `admin/ContentItemsCRUD`:
  - Added `api/ContentLibrary/lookup` with `AdminOnly` authorization
  - Rewired `Courses/Editor` and `Courses/VersionForm` content pickers
  - Picker grids now read server-provided `typeName` and `courseIdsCount`
- `Divisions` lookup separated from `admin/DivisionsCRUD`:
  - Added `api/Divisions/lookup`
  - Rewired shared course-filter helpers and lookup-only editor/filter consumers
- `Categories` lookup separated from `admin/CategoriesCRUD`:
  - Added `api/Categories/lookup`
  - Added `api/Categories/{id}` for category-to-division resolution in course forms
  - Rewired shared course-filter helpers and course editor/detail consumers
- `Roles` lookup separated from `admin/RolesCRUD`:
  - Added `api/Roles/lookup`
  - Rewired user-role tag box and role-division lookup consumer

## Remaining Lookup-Only Usage

None found in active Admin source for the audited `admin/*CRUD` selectors.

## Remaining Follow-Up (Non-CRUD)

| Area | Current Source | Remaining Consumer | Notes |
| --- | --- | --- | --- |
| Learner org filters | Shared helper + local fallback wrappers | `iLearn.Admin/Views/Assignments/Detail.cshtml` | Not a generic CRUD dependency anymore; fallback kept intentionally for deployment resilience |
| Learner org filters | Shared helper + local fallback wrappers | `iLearn.Admin/Views/LearnerGroups/AddMembers.cshtml` | Same resilient-wrapper pattern; optional future cleanup only if deploy/runtime stability is proven |
| Learner org filters | Shared helper + local fallback wrappers | `iLearn.Admin/Views/LearnerGroups/Editor.cshtml` | Same as above |

## Remaining Generic CRUD Usage That Is Acceptable

These are still on `admin/*CRUD`, but they are the actual CRUD or dashboard surfaces for that resource, so they are not lookup-only debt:

- `iLearn.Admin/Views/Divisions/Index.cshtml`
- `iLearn.Admin/Views/Categories/Index.cshtml`
- `iLearn.Admin/Views/Categories/Detail.cshtml`
- `iLearn.Admin/Views/Categories/Report.cshtml`
- `iLearn.Admin/Views/CourseTypes/Index.cshtml`
- `iLearn.Admin/Views/ContentItems/Index.cshtml`
- `iLearn.Admin/Views/Roles/Index.cshtml` main grid data source

## Recommended Next Order

1. Keep future selector work on dedicated `api/*/lookup` routes only
2. Treat learner-organization fallback wrapper cleanup as optional hardening, not CRUD debt

## Validation Notes

- `dotnet build iLearn.API/iLearn.API.csproj -c Debug --artifacts-path .\artifacts\validate\iLearn.API.lookup-rerun` passed
- `dotnet build iLearn.Admin/iLearn.Admin.csproj -c Debug --artifacts-path .\artifacts\validate\iLearn.Admin.lookup` passed
- Active-source grep checks now leave:
  - `admin/CourseTypesCRUD` only on `CourseTypes/Index` and generated publish artifacts
  - `admin/ContentItemsCRUD` only on `ContentItems/Index`
