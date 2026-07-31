# PLAN-181: Assignment report learner group scope

- **Status:** DONE
- **Assigned:** GitHub Copilot
- **Created:** 2026-07-31

## Context

ผู้ใช้พบว่า production หน้า `/iLearn/admin-react/assignments/280/report` แสดง `By Learner Group` ไม่ถูกต้อง มี learner groups ที่ไม่เกี่ยวข้องกับ assignment นี้ และขอให้ตรวจสอบข้อมูล export ที่เกี่ยวกับ learner group ด้วย

## Root Cause

`Assignments/dashboard/{id}` เติม `LearnerProgressDto.LearnerGroups` จาก `LearnerGroupMember` ปัจจุบันของ learner ทุกคนใน batch แทนที่จะใช้ learner group ที่เป็น target ของ `Assignment` row นั้น ๆ

ผลคือ direct assignment ที่ไม่มี `Assignment.LearnerGroupId` แต่ learner เป็นสมาชิกกลุ่มอื่นอยู่ จะถูกสรุปใน `By Learner Group` และ client-side workbook export ผิดตาม payload เดียวกัน

## Scope

- Backend: `iLearn.Application/Services/AssignmentService.cs`
- Frontend: `iLearn.Admin.React/src/pages/assignments/AssignmentReportPage.tsx`
- Tests: `iLearn.Tests/AssignmentFlowTests.cs`

## Changes

- Dashboard learner rows now use `Assignment.LearnerGroupId/LearnerGroupName` as the assignment target group truth
- Dashboard no longer joins current `LearnerGroupMember` membership for report row group labels
- Direct assignments now return empty `learnerGroups` even if the learner currently belongs to unrelated learner groups
- React report group summary no longer creates synthetic `Ungrouped` rows, so `By Learner Group` and workbook `Group Summary` only include real assignment target groups

## Contract Changes

API shape unchanged. Semantics changed for `AssignmentDashboardDto.Learners[].LearnerGroups`: values now represent assignment target groups, not current learner memberships.

## Verification

- `dotnet test .\iLearn.Tests\iLearn.Tests.csproj -c Debug --artifacts-path .\artifacts\validate\assignment-group-dashboard-tests --filter "FullyQualifiedName~AssignmentService_GetDashboardAsync"` ✓ 2/2
- `npm run lint` ✓
- `npm run build` ✓ (existing Vite chunk-size warning)
- QA API deploy: `20260731112144`; QA React deploy: `index-CmNTH0OX.js`, robocopy 3
- PROD API deploy: `20260731112350`; PROD React deploy: `index-CmNTH0OX.js`, robocopy 3
- PROD smoke `Assignments/dashboard/280`: `TargetGroupId=null`, `DistinctGroupCount=0`, `ExportGroupSummaryRows=0`, `LearnerRows=6`
- PROD report route `/iLearn/admin-react/assignments/280/report` = 200 and serves new bundle

## Implementer Notes

- `By Learner Group`, group filter options derived from real groups, and client-side workbook export all read from the same dashboard payload, so the backend semantic fix plus frontend summary skip handles both screen and export behavior
- Production app-pool topology audit warned `Access is denied` during API deploy, but deploy continued per script behavior and post-deploy health check passed HTTP 401 as expected for unauthenticated Windows-auth endpoint