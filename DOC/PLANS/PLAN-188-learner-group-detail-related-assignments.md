# PLAN-188: Show related assignments on learner group detail

- **Status:** DEPLOYED
- **Assigned:** GPT
- **Reviewer:** Claude Code
- **Priority:** Medium

## Request

ผู้ใช้ต้องการให้หน้า React Admin `/learner-groups/{id}` แสดง assignments ที่เกี่ยวข้องกับ learner group นั้นด้วย

## Scope

- Extend `LearnerGroupDetailDto` returned by `GET /api/LearnerGroups/{id}` with related assignment rows for assignments whose `LearnerGroupId` matches the group.
- Reuse the existing assignment-batch grouping/status logic in `LearnerGroupService` so detail, add-member preview, and remove-member preview agree on assignment grouping.
- Update `LearnerGroupDetailPage` to show an Assignments tab with assignment number, courses, dates, status, and current learner count, linking to `/assignments/{id}`.
- Keep division isolation in the service layer.

## Out of Scope

- Do not change assignment creation, assignment dashboard, or member add/remove behavior.
- Do not touch PLAN-187 files except shared files that are required for this request.
- No database or endpoint route changes.

## Verification

```powershell
dotnet build iLearn.Tests -o artifacts\verify-plan188-tests
Remove-Item -Recurse -Force artifacts\verify-plan188-tests

cd iLearn.Admin.React
npm run lint
npm run build
```

## Implementer Notes
- Added `Assignments` to `LearnerGroupDetailDto`; it reuses `LearnerGroupRelatedAssignmentPreviewDto` rows grouped by assignment batch.
- `LearnerGroupService.GetByIdAsync` now loads all related assignment contexts for the group without status filtering; existing add/remove member previews still pass explicit status filters and keep their behavior.
- `LearnerGroupDetailPage` now has a second tab, `Related Assignments`, with assignment links, courses, start/due dates, status badge, and current learner count.
- API contract changed only by adding `data.assignments` to `GET /api/LearnerGroups/{id}`; no route or DB change.
- Verification passed: `dotnet build .\iLearn.Tests\iLearn.Tests.csproj -o .\artifacts\verify-plan188-backend`; `dotnet test .\artifacts\verify-plan188-backend\iLearn.Tests.dll` (294/294); React `npm run build`; React `npm run lint`.
- PROD deploy: API stamp `20260731163725`; React asset `index-BMwSsivA.js` copied to `\\ap-ntc2137-prwb\wwwroot\iLearn\admin-react` with robocopy exit code 3.
- PROD smoke: `/iLearn/admin-react/` 200, `/iLearn/admin-react/learner-groups/32` 200, `/iLearn/Service/api/admin/session/me` 200 with credentials, `/iLearn/Service/api/LearnerGroups/32` 200.
- PROD data check: learner group 32 (`10. Parts system `) currently returns `AssignmentCount=0`; existing `/api/Assignments/group/32/history` also returns `data: []`, so there are no direct `LearnerGroupId=32` assignments in production at smoke time.