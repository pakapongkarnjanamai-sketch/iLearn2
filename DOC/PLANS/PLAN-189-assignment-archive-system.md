# PLAN-189: Assignment archive system

- **Status:** READY
- **Assigned:** GPT
- **Reviewer:** Claude Code
- **Priority:** Medium

## Request

ต้องการระบบจัดเก็บ assignments ที่ไม่ใช้งานแล้ว หรือไม่ต้องการติดตามในหน้าทำงานประจำแล้ว เพื่อแยกออกจาก assignments ปัจจุบันเมื่อเวลาผ่านไปและจำนวนรายการมากขึ้น

## Core Decision

เพิ่มสถานะ `Archived` สำหรับ assignment batch เป็นระบบแยกจาก `Deleted`

- `Archive` = ไม่แสดงใน operational tracking surfaces โดย default แต่ยังดู history/report/audit ได้
- `Delete` = soft delete เดิม ใช้เมื่อไม่ต้องการให้ assignment อยู่ในระบบปกติแล้ว
- ไม่ย้ายหรือลบ `Enrollment` / `EnrollmentAssignment` เพราะ snapshot fields เป็น historical truth ตาม lifecycle rules
- ไม่ใช้ global query filter สำหรับ archive เพื่อหลีกเลี่ยงปัญหาแบบ ignored-query-filter current-state leak; ให้แต่ละ current surface filter `!IsArchived` ชัดเจน

เหตุผล: ถ้าย้าย physical row ออกจาก `Assignments` ตั้งแต่ phase แรก จะกระทบ FK ของ `EnrollmentAssignment`, assignment detail/report, learner history, audit, และ reassign/reset semantics มากเกินความจำเป็น

## Proposed User Behavior

### Default operational view

- Assignment list, dashboard widgets, Gantt, learner group related assignments, and active monitoring views show only non-archived assignments by default
- Archived assignments are still searchable from a dedicated archive tab/page
- Direct URL `/assignments/{id}` remains accessible for archived assignments, but page shows an archived banner and disables mutation actions that would change active assignment tracking

### Archive action

- Admin can archive an assignment batch from Assignment detail or assignment list row actions
- Batch-level operation archives every `Assignment` row sharing the same `AssignmentNo` inside one transaction
- Store who/when/why archived it
- Restore action is available to move a batch back to operational tracking

### Eligibility policy

Default safe eligibility:

- `Completed` assignment batches can be archived
- `Expired` assignment batches can be archived when due date is older than a configurable retention threshold, proposed default 90 days
- `Upcoming` and `InProgress` are not eligible by default
- Manual force archive for `InProgress` should require SuperAdmin and an explicit confirmation that learner access/progress is not changed

Archive does not stop learner access by itself. If the business wants to stop learner access, use existing assignment removal/unenroll/course lifecycle actions rather than archive

## Backend Scope

### 1. Persistence

Add fields to `Assignment`:

```csharp
public bool IsArchived { get; set; }
public DateTime? ArchivedAt { get; set; }
public string? ArchivedBy { get; set; }
public string? ArchiveReason { get; set; }
```

EF migration:

- Add nullable archive metadata columns to `Assignments`
- Default `IsArchived = 0`
- Add filtered/nonclustered index for current assignment queries, e.g. `(IsDeleted, IsArchived, DivisionId, AssignmentNo, DueDate, CreatedAt)` according to SQL Server plan result
- Migration must live under `iLearn.Infrastructure/Migrations/` with namespace `iLearn.Infrastructure.Migrations`

### 2. Domain/application policy

Add an application-level archive policy/service, for example:

- `AssignmentArchivePolicy` computes eligibility from current computed assignment status, dates, enrollment completion/snapshots, and current date
- `AssignmentArchiveService` or methods on `IAssignmentService` handle archive/restore preview and commit
- Archive and restore must be transaction-safe across all rules in the batch
- Division-scoped admins can archive only assignments in their division; SuperAdmin can archive across divisions

### 3. API endpoints

Add explicit endpoints under `AssignmentsController`:

- `POST /api/Assignments/{id}/archive`
- `POST /api/Assignments/{id}/restore`
- `POST /api/Assignments/archive/preview`
- `POST /api/Assignments/archive/bulk`

DTOs:

- `ArchiveAssignmentRequestDto { string? Reason, bool Force }`
- `ArchiveAssignmentPreviewDto { AssignmentId, AssignmentNo, Status, CourseCount, LearnerCount, Eligible, Blockers[] }`
- `ArchiveAssignmentResultDto { Success, ArchivedBatchCount, AssignmentNo, Message }`

### 4. Query behavior updates

Filter archived assignments out of current surfaces:

- `AssignmentsCRUDController.Get` / `vw_AssignmentList` or equivalent mapped row query
- `AssignmentService.GetHistoryAsync` default response and summary counts
- `AssignmentService.GetGanttTasksAsync`
- Dashboard active/priority assignment queries
- Course detail assignment history unless `includeArchived=true`
- Learner group related assignments added in PLAN-188 unless `includeArchived=true`
- Reports that represent current operational monitoring

Keep archived assignments available in historical/audit surfaces:

- Assignment detail by id
- Assignment report by id
- Archive tab/page search
- Optional `includeArchived=true` for history/report endpoints where useful

## React Admin Scope

### 1. Assignment list

- Add `Active` / `Archived` segmented filter or tabs on `/assignments`
- Default to `Active`
- Show archived metadata columns only in Archived view: `ArchivedAt`, `ArchivedBy`, `ArchiveReason`
- Add row action `Archive` for active rows and `Restore` for archived rows

### 2. Assignment detail

- Show archived banner with archived metadata
- Disable mutation actions while archived:
  - add/remove learners
  - add/remove courses
  - extend due date
  - reset enrollments
  - update description, unless product decides description edits are allowed for audit correction
- Keep report/export/read-only tables available
- Show `Restore Assignment` action in Controls

### 3. Bulk archive workspace

- Add a maintenance flow under Assignments, e.g. `/assignments/archive`
- Filters: status, due date before, created before, division, search
- Preview first, then confirm
- Show blockers for ineligible assignments

## Data / Audit Semantics

- Do not change `Enrollment.IsCompleted`, `Enrollment.Progress`, `Enrollment.DueDate`, or `EnrollmentAssignment` snapshot fields during archive
- Archive must not reset learners, unenroll learners, or change course/version access
- Archive metadata is audit data and should not be overwritten except by restore/re-archive cycles
- Restore clears `IsArchived`, `ArchivedAt`, `ArchivedBy`, and `ArchiveReason` or records a separate restore audit event if audit table support is added

## Physical Storage Phase

Phase 1 should be logical archive only. If row volume later proves too large, create a separate follow-up plan for physical cold storage:

- Keep original rows in `Assignments` for FK integrity until every read path supports archive lookup
- Option A: table partitioning or filtered indexes by `IsArchived`
- Option B: append-only archive mirror tables such as `ArchivedAssignments` and `ArchivedEnrollmentAssignments`, populated by copy jobs while original rows remain for FK references
- Do not hard-move assignment rows out of `Assignments` while `EnrollmentAssignment.AssignmentId` still references them

## Testing Scope

Backend tests:

- Completed batch archives all rules sharing `AssignmentNo`
- Expired old batch archives when beyond retention threshold
- Upcoming/InProgress batch is blocked without force
- Division-scoped admin cannot archive another division's assignment
- Archived assignment is excluded from current list/history/gantt/dashboard by default
- Archived assignment detail/report by id remains readable
- Restore makes the assignment visible in current surfaces again
- Archive does not mutate enrollment progress or snapshot values

Frontend tests/build checks:

- Assignment list default active filter hides archived rows
- Archived tab shows archived rows and metadata
- Detail page renders archived banner and disables mutation actions
- React `npm run lint` and `npm run build`

## Deployment Notes

- Requires EF migration and `dotnet ef database update --connection <env>` during deploy; there is no auto-migrate
- Deploy API before React so the new contract exists before UI calls archive endpoints
- No data backfill should auto-archive production rows in the first deploy; initial deploy only adds capability
- First production archive run should be manual and reviewed with a small batch

## Acceptance Criteria

- Admin can archive and restore assignment batches without losing learner history
- Active assignment pages no longer show archived rows by default
- Archive page/tab can search archived assignments
- Direct detail/report links for archived assignments still work
- Assignment counts/KPIs clearly distinguish active vs archived
- Existing assignment lifecycle rules, snapshots, learner access, and reports remain consistent

## Open Decisions Before Implementation

- Default retention threshold for Expired assignments: proposed 90 days
- Required role for normal archive: proposed Admin within division; force archive proposed SuperAdmin only
- Whether description edits are allowed while archived
- Whether archive should be visible in learner-facing history (proposed no learner UI change)

## Verification Commands

```powershell
dotnet build iLearn.Tests -o artifacts\verify-plan189
dotnet test artifacts\verify-plan189\iLearn.Tests.dll
Remove-Item -Recurse -Force artifacts\verify-plan189

cd iLearn.Admin.React
npm run lint
npm run build
```

## Implementer Notes