# Lifecycle Analysis — iLearn

วิเคราะห์ lifecycle (สถานะ + transition + guard) ของ entity หลักในระบบ — Course, Course Version, Content Item, Assignment, Enrollment, SCORM runtime

> เขียนโดย Claude Code (planner/reviewer) 2026-06-16 จากโค้ดจริง (enums, `Application/Common/*Policy`, services)

---

## 1. Course lifecycle

**สถานะ** (`CourseStatus`): `Draft(0) → Open(1) → Closed(2) → Retired(3)`

```
Draft ──(เปิด: ต้องมี active version ที่ ready)──> Open
Open  ──(ปิด)──> Closed ──(เปิดใหม่)──> Open
Open/Closed ──(ปลด: ต้องไม่มี open enrollment)──> Retired
```

**Guard ที่บังคับ** (`CourseService.UpdateCourseStatusAsync`):
- → **Open**: ต้องมี **active version** และ version นั้น **ready** (content item ครบ+published+มี launch) ไม่งั้น `InvalidOperationException` พร้อมรายการ issue (`CourseContentReadiness.BuildActivationErrorMessage`)
- → **Retired**: บล็อกถ้ายังมี enrollment ที่ `!IsCompleted` (ต้อง Close ก่อน แล้วรอจบ/ยกเลิก)
- ทุก transition: `IsActive = (status == Open)` + **log AdminActivity** (มี oldStatus→newStatus)
- `IsActive` (boolean) เป็น derived จาก status — Open เท่านั้นที่ active

**สังเกต:** ไม่มี guard ทิศทางอื่น (เช่น Retired→Draft ทำได้?) — transition เป็น set-status ตรง ไม่ใช่ state machine เข้ม; readiness/enrollment เป็น guard เฉพาะ Open/Retired

---

## 2. Course Version lifecycle

- Course มีได้หลาย `CourseVersion`; **1 version เป็น active** (`IsActive`) ต่อ course
- **Readiness** (`CourseContentReadiness.IsVersionReady`): version ready เมื่อมี content item ≥1 และ **ทุกตัว ready**
- Content item ready = `IsActive` (published) + มี `URL` + มี `LaunchHref` (หรือ URL เป็น direct launch)
- version readiness เป็นเงื่อนไขเปิด course (ข้อ 1) — `GetVersionReadinessAsync` คืน issues รายตัว
- learner ที่ enroll ผูกกับ `EnrolledCourseVersion` (เวอร์ชันตอน enroll)

---

## 3. Content Item lifecycle

**สถานะ:** `Draft (IsActive=false) ⇄ Published (IsActive=true)` (`ContentPublicationService`)

```
Draft ──(Publish)──> Published ──(Unpublish)──> Draft
```

**Guard:**
- **Publish**: ถ้า active อยู่แล้ว = no-op/guard
- **Unpublish**: ถ้าไม่ active = guard; มี **impact preview** (`PreviewBatchUnpublishAsync`) — `CanUnpublish=false` ถ้า content หาย/ไม่ active/ถูกอ้างโดย course ที่ active (กัน unpublish ของที่ course เปิดอยู่ใช้)
- ฝั่ง UI (ContentItemDetailPage): ต้อง unpublish ก่อนลบ (delete disabled ถ้า active)

**สังเกต:** content unpublish กระทบ version readiness → กระทบความสามารถเปิด course (chain: content → version → course)

---

## 4. Assignment lifecycle (computed status — ไม่เก็บใน DB)

สถานะคำนวณ runtime จาก date + progress (`AssignmentStatusKeys`) — ไม่ใช่ field เก็บ

**Batch status** (ทั้ง assignment): `Upcoming → InProgress → Expired` / `Completed`
- Completed = มี enrollment และ **ทุกคนจบ**
- Upcoming = `startDate > now`
- Expired = `dueDate < now`
- InProgress = ระหว่างนั้น

**Learner status** (รายคน): `NotStarted → InProgress → Completed`, + scheduled: `Upcoming`/`Overdue`
- Completed = `IsCompleted`
- Upcoming = `startDate > now`, Overdue = `dueDate < now` (ยังไม่จบ)
- InProgress = `progress > 0`, ไม่งั้น NotStarted
- **DueSoon** = due ภายใน **7 วัน** (`DueSoonWindowDays`) — ใช้ใน dashboard/report

**สังเกต:** สถานะเป็น pure function ของ (date, progress, isCompleted) — ไม่มี state เก็บ → คำนวณสม่ำเสมอทุกที่ (ดี ลด drift) แต่ต้องส่ง `currentDate` เข้าทุกครั้ง

---

## 5. Enrollment lifecycle

**fields:** `IsCompleted, Progress, TotalScore, TotalTimeSpent, StartDate/DueDate/CompletedDate, EnrolledCourseVersion, ResetAt, AssignmentLinks[]`

```
Created ──> In Progress (progress>0) ──> Completed (IsCompleted, progress=100)
   ^                                          │
   └──────────── Reset (ResetAt=now) ─────────┘
```

**Transition:**
- **Progress/Complete** (SCORM): `LearningLogsController.CommitRuntime/UpdateProgress` → upsert learning logs → `UpdateEnrollmentRollup` (rollup progress/score/time จาก logs)
- **Reset** (`EnrollmentService.ResetStatusAsync`): `IsCompleted=false, CompletedDate=null, Progress=0, ResetAt=now` — **เก็บ log เก่าไว้** (log ที่ `CreatedAt < ResetAt` = รอบก่อน ไม่แสดงใน player) → reset แบบไม่ลบประวัติ
- **Manual complete** (`UpdateCompletionAsync`): admin force complete/uncomplete
- **Validation:** `StartDate ≤ DueDate`
- **Bulk assign:** สร้าง enrollment; ถ้า learner มี assignment in-progress → ต้อง confirm ก่อน reset (`BulkAssignResultDto` conflict)

**สังเกตเด่น:** `ResetAt` เป็นกลไกฉลาด — reset progress โดยรักษา audit trail (logs) ไว้ทั้งหมด

---

## 6. SCORM runtime lifecycle (learner play)

```
Launch (player-info) ──> Play ──> commit-runtime/update-progress (HMAC) ──> resolve status ──> rollup enrollment
```

**Status resolution** (`ScormContentStatusPolicy.ResolveStatus`) ลำดับความสำคัญ:
1. `failed` (success/lesson/persisted = failed)
2. `passed` (success/lesson/persisted = passed)
3. `completed` — completion/lesson = completed/browsed หรือ isDone → **แต่ Exam type (typeId=2) คืน `incomplete`** (exam ต้อง pass ไม่ใช่แค่ complete)
4. `incomplete` (default)
- progress: passed/completed → 100, อื่น → 0

**จุดสำคัญ:** Exam content (typeId=2) จบ ≠ ผ่าน — ต้อง `passed` ถึงนับ 100; Learn content (typeId=1) แค่ completed ก็ 100

---

## 7. Cross-cutting

- **AdminActivity log**: transition สำคัญ (course status ฯลฯ) ถูก log + ส่ง SignalR live feed
- **Division isolation**: ทุก lifecycle action ผ่าน service ที่ isolate division (ดู `division_isolation_analysis.md`)
- **Soft delete**: entity ใช้ `IsDeleted` (BaseEntity) — lifecycle ไม่ลบจริง

---

## 8. ข้อสังเกต / ความเสี่ยง

| # | ประเด็น | ระดับ |
|---|---|---|
| 1 | Course transition เป็น **set-status** ไม่ใช่ state machine เข้ม — ทิศทางแปลก ๆ (เช่น Retired→Draft) อาจทำได้โดยไม่มี guard | 🟡 ตรวจว่ามี guard ครบตามที่ตั้งใจ |
| 2 | Assignment status คำนวณ runtime (ดี) แต่ **ต้องส่ง `currentDate`/`_dateTime.Now` สม่ำเสมอ** — ถ้าบางที่ใช้ `DateTime.Now` ตรง ๆ อาจ timezone/test ไม่ตรง | 🟡 |
| 3 | Content unpublish → กระทบ version readiness → course เปิดไม่ได้ (chain) — impact preview มีแล้ว แต่ผู้ใช้ต้องเข้าใจ chain | 🟢 มี preview |
| 4 | Enrollment reset เก็บ log ไว้ด้วย `ResetAt` — player/report ต้อง filter `CreatedAt >= ResetAt` ทุกที่ ไม่งั้นเห็นรอบเก่าปน | 🟡 ตรวจว่าทุก query เคารพ ResetAt |
| 5 | Exam completion logic อยู่ใน `ScormContentStatusPolicy` ที่เดียว (ดี) — แต่ขึ้นกับ `typeId` ถูกตั้งถูก | 🟢 |

---

## 9. แฟ้มอ้างอิง

- `iLearn.Domain/Enums/CourseStatus.cs`
- `iLearn.Application/Common/`: `AssignmentStatusKeys.cs`, `ScormContentStatusPolicy.cs`, `CourseContentReadiness.cs`, `EnrollmentVisibilityPolicy.cs`
- `iLearn.Application/Services/`: `CourseService.cs`, `ContentPublicationService.cs`, `CourseVersionService.cs`, `EnrollmentService.cs`
- `iLearn.API/Controllers/LearningLogsController.cs` (SCORM commit → rollup)
- `DOC/system_analysis.md`, `DOC/api_analysis.md`
