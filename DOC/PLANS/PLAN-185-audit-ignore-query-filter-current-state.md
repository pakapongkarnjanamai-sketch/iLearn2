# PLAN-185: Audit ignored-query-filter current-state decisions

- **Status:** READY
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

Seed finding from PLAN-184:

- `Enrollments/my-courses` needs ignored filters for deleted-assignment visibility decisions, but current content readiness must ignore soft-deleted `CourseContentItem` links. This is now guarded in `CourseContentReadiness.IsVersionReady(...)`.

Known search command:

```powershell
rg "ignoreQueryFilters\s*:\s*true|IgnoreQueryFilters\(" -g "*.cs"
```