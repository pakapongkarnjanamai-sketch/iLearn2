# PLAN-184: MyLearning hides course when soft-deleted content link remains

- **Status:** DONE
- **Assigned:** GitHub Copilot
- **Created:** 2026-07-31

## Context

ผู้ใช้รายงานว่า learner `430263` ไม่เห็น course `KSN` ในหน้า learner `หลักสูตรของฉัน` หลัง Admin แก้ชื่อใน content library แต่ assignment detail ยังมี learner และ course นี้อยู่

## Findings

- Production assignment `509` / `AS-20260721-019` has course `KSN` and learner `430263` with status `NotStarted`
- Production learner `my-courses` originally returned 2 courses and did not include `KSN`
- Production `player-info/973` for learner `430263` returned 200 and included KSN content items, so course/content/player readiness was actually valid
- Difference was specific to `Enrollments/my-courses`: it loads enrollments with `ignoreQueryFilters: true`, then checks version readiness from the loaded navigation graph

## Root Cause

`CourseContentReadiness.IsVersionReady(...)` considered every `CourseContentItem` in the collection. When `my-courses` loads with `ignoreQueryFilters: true`, soft-deleted course-content links can be present and can make an otherwise ready version look not ready, causing the course to be hidden from `หลักสูตรของฉัน`

`player-info` uses a normal filtered version query, so it did not see the stale soft-deleted link and could still launch KSN

## Changes

- `CourseContentReadiness.GetContentItemIssue(...)` now treats soft-deleted `ContentItem` records as not ready
- `CourseContentReadiness.IsVersionReady(...)` ignores soft-deleted `CourseContentItem` links before checking readiness
- Added focused regression tests for:
  - soft-deleted unready links should not make a version not ready
  - active links to deleted content items should still make a version not ready

## Contract Changes

No API shape / DTO / DB changes. Readiness semantics now ignore soft-deleted course-content links consistently when a caller loads with ignored query filters

## Verification

- `dotnet test .\iLearn.Tests\iLearn.Tests.csproj -c Debug --artifacts-path .\artifacts\validate\course-content-readiness --filter "FullyQualifiedName~CourseContentReadinessTests"` ✓ 2/2
- QA API deploy: `20260731130531`; smoke `admin/session/me` = 200
- PROD API deploy: `20260731130652`; health check HTTP 401 as expected for unauthenticated Windows-auth endpoint
- Production learner proxy verification after deploy: learner `430263` `my-courses` returned 3 rows and `HasKSN=True`; KSN row = `CourseId=973`, `Code=KSN`, `Version=593`

## Implementer Notes

- No React/User deploy was required; fix is in shared Application readiness policy used by the API
- Direct player verification before the fix already proved KSN content was launchable; this fix aligns `my-courses` readiness with player-info readiness when soft-deleted links are present in ignored-query-filter loads