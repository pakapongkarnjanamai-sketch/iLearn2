# PLAN-183: Apply active course version to existing learners

- **Status:** DONE
- **Assigned:** GitHub Copilot
- **Created:** 2026-07-31

## Context

ผู้ใช้พบว่า learner player ของ course `893` ยังแสดง content จาก version เก่า แม้ Admin course detail มี active version ใหม่แล้ว

ตรวจ production แล้วพบว่า learner `431420` มี `Enrollment.EnrolledCourseVersion = 370` และ player ใช้ `CourseVersionId = 370` จึงแสดง Rev.11 ในขณะที่ active version ของ course คือ `612` / v2 / Rev.12

## Root Cause

ระบบ learner player ตั้งใจใช้ `Enrollment.EnrolledCourseVersion` สำหรับผู้เรียนที่มี enrollment อยู่แล้ว เพื่อรักษาประวัติ assignment/progress เดิม

การ activate version ใหม่ด้วย policy `NewLearnersOnly` ไม่ย้าย learner เดิม และหลัง version นั้น active แล้ว endpoint `set-active` ไม่ apply policy ซ้ำ เพราะ service guard เฉพาะตอนเปลี่ยน inactive -> active

## Changes

- Added service method `ApplyLearnerPolicyToActiveVersionAsync(courseId, versionId, policy)`
- Added API endpoint `POST /api/Courses/{courseId}/versions/{versionId}/apply-learner-policy`
- Endpoint validates that target version belongs to the course, is currently active, and is ready before applying policy
- Added Course detail Admin action `Apply Active Version`
- Added modal with learner impact counts and two explicit policies:
  - `MoveNotStarted`: move only not-started eligible learners
  - `ResetInProgress`: move not-started and in-progress eligible learners, clearing active progress/runtime state
- Added regression test that already-active version can move not-started eligible learners without moving in-progress/completed/upcoming/unassigned learners

## Contract Changes

New API endpoint only; no DTO/DB shape changes. Existing learner player behavior remains unchanged until an Admin explicitly applies a learner version policy.

## Verification

- `dotnet test .\iLearn.Tests\iLearn.Tests.csproj -c Debug --artifacts-path .\artifacts\validate\active-version-policy --filter "FullyQualifiedName~CourseVersionLearnerPolicyTests"` ✓ 9/9
- `npm run lint` ✓
- `npm run build` ✓ (existing Vite chunk-size warning)
- QA API deploy: `20260731125309`; QA React deploy: `index-T-I3M-Xy.js`, robocopy 3
- QA no-op endpoint smoke: `POST Courses/893/versions/370/apply-learner-policy` with `NewLearnersOnly` = 200
- PROD API deploy: `20260731125509`; PROD React deploy: `index-T-I3M-Xy.js`, robocopy 3
- PROD no-op endpoint smoke: `POST Courses/893/versions/612/apply-learner-policy` with `NewLearnersOnly` = 200
- PROD Course detail `/iLearn/admin-react/courses/893` = 200 and serves `index-T-I3M-Xy.js`
- Playwright assertion: course page contains `Apply Active Version` and `NTC-WI-CAS-2343`

## Implementer Notes

- Smoke tests intentionally used `NewLearnersOnly`, which is a no-op, so production learner `431420` was not moved during deployment validation
- After no-op smoke, learner `431420` still had player `CourseVersionId = 370` and content Rev.11, confirming no accidental data movement
- To move that learner to Rev.12, an Admin must use the new Course detail action and choose `Move not-started learners only` for course `893`