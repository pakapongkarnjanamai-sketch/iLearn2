# PLAN-101: HOTFIX ต่อ — reset ทุก path ต้องล้าง ScormRuntimeState (099 แก้แค่ path เดียว)

- **Status:** DONE → REVIEWED (code+tests ผ่าน — รอ manual admin-reset smoke ก่อน VERIFIED)
- **Assigned:** GitHub Copilot
- **Reviewer:** Claude Code
- **สร้างเมื่อ:** 2026-07-21
- **ความรุนแรง:** 🔴 CRITICAL — บั๊กเดิมของ PLAN-099 ยังเกิดเมื่อ reset ผ่าน admin (ผู้ใช้ยืนยันบน QA)
- **ต่อจาก:** [PLAN-099](PLAN-099-reset-scorm-state-lifecycle-hotfix.md) (VERIFIED — เพิ่ม `ClearForEnrollmentAsync`)
- **อ่าน CLAUDE.md หัวข้อ Backend ก่อนเริ่ม**

> 099 เพิ่ม `ClearForEnrollmentAsync` แต่เรียกแค่ที่ `LearningLogsController.ResetProgress` (learner). ผู้ใช้กด reset ผ่าน **Admin** → คนละ path → stale state ไม่ถูกล้าง → สถานะ/คะแนนเก่าฟื้นเหมือนเดิม

## หลักฐาน (QA DB, enrollment 18201 — reset admin 09:33:14 วันนี้)

- `Enrollment.ResetAt=09:33:14` ตั้งแล้ว แต่ `ScormRuntimeState` 4 rows ยัง **`IsDeleted=0`** (Created 13 ก.ค., RawScore=100 / SuccessStatus=passed)
- `UpdatedAt` ของ exam rows = 09:32:10 / 08:28:22 (**ก่อน** reset) ⇒ `ClearForEnrollmentAsync` ไม่เคยแตะ = reset path นี้ไม่เรียก clear
- post-reset commit ปลุก learn rows กลับ (RawScore=100 ค้าง) → progress เพี้ยน

## ResetAt setter ทั้งหมด (grep ยืนยัน 5 จุด — 099 แก้ 1)

| path | ใช้ตอน | scope | สถานะ |
| --- | --- | --- | --- |
| `LearningLogsController.ResetProgress` | learner reset | single | ✅ 099 |
| **`EnrollmentService.ResetStatusAsync`** (บรรทัด 50) | **admin manual reset** | single | ❌ §1 |
| `CourseAssignmentService` (บรรทัด ~197) | re-assign/version change | single (ในลูปใหญ่) | ❌ §2 |
| `CourseVersionService` (บรรทัด ~424 loop) | เปลี่ยน version | **bulk** | ❌ §2 |
| `AssignmentService` (บรรทัด ~282 loop) | bulk re-assign | **bulk** | ❌ §2 |

## Scope

### §1 (CRITICAL — path ที่ผู้ใช้ใช้) — `EnrollmentService.ResetStatusAsync`

- inject `IScormRuntimeStateService` เข้า `EnrollmentService` (constructor + field)
- ใน `ResetStatusAsync` หลัง set `ResetAt`/Progress: `await _scormRuntimeStateService.ClearForEnrollmentAsync(enrollment.Id);` (ของ 099 — single, save เอง) **ใน main flow** (ล้มได้ = reset ล้ม ไม่ใช่ side-effect เงียบ)
- **หมายเหตุ (align กับ learner path):** ResetStatusAsync ปัจจุบัน**ไม่ reset `EnrollmentAssignment` snapshot** (SnapshotCompleted/Progress) ต่างจาก `LearningLogsController.ResetProgress` ที่ reset — ทำให้ report ยังเห็น completed ค้างหลัง admin reset. ถ้า inject `IGenericRepository<EnrollmentAssignment>` แล้ว reset snapshot ด้วยได้ (low-risk) ให้ทำ; ถ้าจะขยาย scope เกินไปให้จดใน Implementer Notes เป็นหนี้ (แต่ **การล้าง runtime state คือส่วนบังคับ**)

### §2 (HIGH) — อีก 3 path (bulk-safe, กัน N+1)

**เพิ่ม bulk method ใน `IScormRuntimeStateService` + impl** (refactor ให้ single เดิมเรียกตัวนี้):

```csharp
/// <summary>Soft-delete runtime states ของหลาย enrollment; saveChanges=false = ให้ caller commit เอง (ร่วม transaction/SaveChanges เดิม กัน N+1)</summary>
Task<int> ClearForEnrollmentsAsync(IReadOnlyCollection<int> enrollmentIds, bool saveChanges = true, CancellationToken ct = default);
```

- impl: query states `WHERE EnrollmentId IN (ids)` **ครั้งเดียว** (ไม่ใช่ต่อ enrollment) → `DeleteWithoutSave` ทุกตัว → `if (saveChanges) SaveChangesAsync` → คืน count
- `ClearForEnrollmentAsync(id)` เดิม = `ClearForEnrollmentsAsync([id], saveChanges: true)` (ไม่เปลี่ยน behavior 099)

**wire เข้า 3 path — เรียก `ClearForEnrollmentsAsync(ids, saveChanges: false)` ก่อน `SaveChanges`/commit เดิมของ path นั้น** (repo ทั้งหมด scoped ใช้ DbContext เดียว → soft-delete ที่ยังไม่ save จะถูก commit พร้อมกัน):
- **CourseVersionService** (~424): เก็บ `targetEnrollments.Select(e => e.Id)` → เรียก clear (save:false) ก่อน `await _unitOfWork.SaveChangesAsync()` (บรรทัด 438)
- **AssignmentService** (~282): ใน transaction เดิม เก็บ `enrollments.Select(e=>e.Id)` (หรือ `enrollmentIdsToReset`) → clear (save:false) ก่อน `SaveChangesAsync` (บรรทัด 292) — อยู่ใน try/transaction เดิม rollback ครอบให้แล้ว
- **CourseAssignmentService** (~197): single enrollment ในลูปใหญ่ — ถ้าเป็นจุดที่ save รวมทีเดียวตอนท้าย ใช้ clear (save:false) + เก็บ id ไว้ clear รวม; ถ้า save ต่อ enrollment อยู่แล้วใช้ single `ClearForEnrollmentAsync`. **อ่าน flow จริงก่อนเลือก** (จดวิธีที่เลือกใน Notes)

## Contract ที่เปลี่ยน

- **ใหม่:** `IScormRuntimeStateService.ClearForEnrollmentsAsync` (additive); `ClearForEnrollmentAsync` เดิม behavior คงเดิม
- `EnrollmentService` เพิ่ม dependency `IScormRuntimeStateService` (+ อาจ `IGenericRepository<EnrollmentAssignment>` ตาม §1)
- DB schema / migration: **ไม่มี**; API shape: **ไม่เปลี่ยน**

## นอก Scope

- ห้ามแตะ merge policy / player-info resolve / client (เหมือน 099)
- ห้ามเปลี่ยน behavior ของ `ClearForEnrollmentAsync` (099 VERIFIED แล้ว)
- ห้าม migration

## Verification

```powershell
dotnet build iLearn.Tests -o artifacts\verify-test
dotnet test artifacts\verify-test\iLearn.Tests.dll
Remove-Item -Recurse -Force artifacts\verify-test
```

Tests ที่ต้องเพิ่ม:
1. `EnrollmentService.ResetStatusAsync` → `ClearForEnrollmentAsync` ถูกเรียกด้วย enrollmentId ถูกตัว (fake service assert ClearedEnrollmentId — pattern เดียวกับ 099)
2. `ClearForEnrollmentsAsync([a,b], saveChanges:false)` → states ของ a,b ถูก mark deleted, **ยังไม่ save** (SaveCallCount=0); ของ enrollment c ไม่โดน
3. `ClearForEnrollmentsAsync([a], saveChanges:true)` → save 1 ครั้ง (เทียบเท่าของเดิม)
4. (ถ้าทำ §1 snapshot) ResetStatusAsync reset SnapshotCompleted/Progress

Manual (QA — ต้อง deploy API):
1. **admin reset enrollment 18201** → เปิด player: ทุก item ว่าง/ไม่ผ่าน ไม่มีติ๊กเขียวค้าง บาร์ Learn ไม่เต็ม
2. ตรวจ DB: `ScormRuntimeStates` ของ 18201 rows เก่า `IsDeleted=1`
3. (ถ้ามี) เปลี่ยน version คอร์สที่มีคนเรียนค้าง → states ของทุกคนถูกล้าง, ไม่มี error/timeout (กัน N+1)

**Remediation 18201 ที่เพี้ยนอยู่:** deploy §1 → admin reset ซ้ำ 1 ครั้ง = สะอาด (ไม่ต้องแก้ DB มือ)

## Deploy note

แตะ iLearn.Application + iLearn.Infrastructure → **deploy API** (ไม่มี migration). PROD รอผู้ใช้ยืนยัน QA

## Implementer Notes

- Added `ClearForEnrollmentsAsync` to `IScormRuntimeStateService`; the implementation queries all requested enrollment IDs once, soft-deletes the active states, and optionally participates in the caller's existing save boundary. `ClearForEnrollmentAsync` now delegates to it with `saveChanges: true`, preserving PLAN-099 behavior.
- `EnrollmentService.ResetStatusAsync` now resets related `EnrollmentAssignment` snapshot completion/progress values and clears runtime state in the main reset flow.
- Wired bulk clear with `saveChanges: false` before the existing save/transaction boundary in `CourseVersionService`, `AssignmentService`, and `CourseAssignmentService`. `CourseAssignmentService` collects only existing enrollment IDs that are actually reset, then clears them once per public assignment operation.
- Added regression tests for bulk clear without saving, bulk clear with one save, and admin reset clearing runtime state plus snapshots.
- Validation: `dotnet build iLearn.Tests -o artifacts\verify-plan101 --no-restore -v:q` compiles all production projects successfully. The test project is blocked by a pre-existing concurrent-worktree error in `EnrollmentsPlayerInfoTests.NullNotificationService`: it does not implement the current `INotificationService.GetForUserAsync(string, bool, int, int)` signature. PLAN-101 test doubles and constructor calls compile cleanly before that unrelated error.
- QA deployment: API deployed to `\\AP-NTC2138-QAWB\wwwroot\iLearn\Service\_deploy_20260721121253`; active `web.config` argument is `.\_deploy_20260721121253\iLearn.API.dll`. Previous API stamp was `20260721092410`. The session endpoint returned expected HTTP 401 on the first health-check attempt and `AutoRolledBack=False`. No migration was run.
- The QA learner app was also deployed for the associated PLAN-102/103 diagnostic and same-origin verification: `\\AP-NTC2138-QAWB\wwwroot\iLearn\_user_deploy_20260721121441`, previous stamp `20260721092529`; learner root returned HTTP 200 on the first attempt and `AutoRolledBack=False`.
- Remaining QA manual check: reset enrollment 18201 through Admin, confirm its old `ScormRuntimeStates` are soft-deleted and the player has no revived completion/score. Do not deploy PROD until this is confirmed.

## Reviewer Sign-off (Claude Code, 2026-07-21)

- **§1:** `EnrollmentService.ResetStatusAsync` inject `IScormRuntimeStateService` + `IGenericRepository<EnrollmentAssignment>`; เรียก `ClearForEnrollmentAsync` ใน main flow (ไม่ห่อ try/catch) **และทำ snapshot reset ให้ด้วย** (ข้อที่แผนบอกว่า "ถ้าทำได้ให้ทำ") ✅
- **§2:** `ClearForEnrollmentsAsync(ids, saveChanges, ct)` — query เดียวด้วย `Contains(ids)` + `Distinct()` **ไม่มี N+1**; `ClearForEnrollmentAsync` delegate มาที่ตัวนี้ (behavior 099 คงเดิม) ✅ wire ครบ 3 path: `CourseVersionService`/`AssignmentService` เรียก `saveChanges:false` ก่อน `SaveChangesAsync` เดิม (AssignmentService อยู่ใน transaction เดิม rollback ครอบให้) และ `CourseAssignmentService` สะสม `resetEnrollmentIds` แล้ว clear ครั้งเดียวต่อ public operation ✅
- **Tests:** 3 เคสตามแผน (`ClearForEnrollmentsAsync_SoftDeletes...WithoutSaving` assert SaveCallCount=0, `..._SavesOnceWhenRequested`, `ResetStatusAsync_ClearsRuntimeStateAndAssignmentSnapshots`) ✅
- **Verify อิสระ:** build 0 errors, `dotnet test` **214/214** — และ **blocker เดิมที่ทำให้ test project build ไม่ผ่านถูกแก้แล้ว** (`EnrollmentsPlayerInfoTests`/`ContentItemsControllerTests` test double อัปเดตตาม `INotificationService` signature)
- **คงค้าง:** manual admin-reset enrollment 18201 บน QA + ยืนยัน rows เก่า `IsDeleted=1`

**สรุป: ผ่านรีวิว ไม่มี finding**
