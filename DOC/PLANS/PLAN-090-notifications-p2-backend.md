# PLAN-090: Notifications Phase 2 — Backend (deadline digest scheduler ตัวแรกของระบบ + retention + paging)

- **Status:** DONE → VERIFIED (no findings)
- **Assigned:** GitHub Copilot
- **Reviewer:** Claude Code
- **สร้างเมื่อ:** 2026-07-15
- **ต่อยอดจาก:** [PLAN-088](PLAN-088-notifications-backend.md) (VERIFIED แล้ว)
- **คู่ขนานกับ:** [PLAN-091](PLAN-091-notifications-p2-frontend.md) (Gemini) — **contract §1 freeze** กติกาเดิม: เบี่ยง shape ต้องอัปเดตทั้ง 2 แผน + AGENT_LOG ก่อน Gemini ปิดงาน
- **อ่าน CLAUDE.md หัวข้อ "กติกาสำคัญฝั่ง Backend" ก่อนเริ่ม** — โดยเฉพาะข้อ IDateTime, effective dates, side-effect ห้ามทำ request หลักล้ม

> ผู้ใช้เลือก (2026-07-15): Phase 2 = A (หน้าเต็ม+retention) + B (แจ้งเตือนใกล้ครบกำหนด/overdue ทุกเช้า) — **recipient = ทุก admin ใน division ของ assignment**

---

## บริบท (ยืนยันจากโค้ดแล้ว)

- **ระบบยังไม่มี `BackgroundService`/scheduler ใด ๆ เลย — งานนี้คือตัวแรก** ออกแบบให้เป็นแบบอย่างที่ดี
- Admin ↔ division: `User(Nid)` → `UserRole` → `Role(DivisionId, RoleType)` — DivisionId claim มาจาก role แรกที่มีค่า (`ApiClaimsEnrichMiddleware`); `RoleType` มีแค่ `Admin`/`SuperAdmin`
- Due-soon มี helper อยู่แล้ว: `AssignmentStatusKeys.IsDueSoon(isCompleted, dueDate, now)` + `GetDueSoonCutoff` (**window = 7 วัน**)
- `Assignment.DueDate` ถูกอัปเดตโดย ExtendDueDate อยู่แล้ว (rule-level ใช้ raw ได้) แต่ **overdue ของ learner ต้องนับจาก `EnrollmentAssignment.DueDate` (link)** ตามกติกา effective dates ใน CLAUDE.md
- ตาราง `Notifications` ตอนนี้**ไม่มี retention** — โตไม่จำกัด
- `NotificationService.NotifyAsync` กลืน error เองอยู่แล้ว (ปลอดภัยต่อการเรียกเป็น batch)

## Scope

### 1. Contract เพิ่มเติม (FREEZE — additive ไม่กระทบของเดิม)

- `NotificationListDto` เพิ่ม field: `public int TotalCount { get; set; }` (จำนวนทั้งหมดหลัง filter ของ user — สำหรับ "Showing X of Y")
- `GET api/Notifications` เพิ่ม query param: `skip` (default 0, clamp ≥ 0) — เรียง CreatedAt DESC เหมือนเดิมแล้วค่อย Skip/Take
- `NotificationTypes` เพิ่ม const: `DeadlineDigest`
- Endpoint อื่น/DTO อื่นเดิมทุกตัว **ห้ามเปลี่ยน**

### 2. Deadline digest — `IDeadlineDigestService` + hosted wrapper

แยกเป็น 2 ชั้นเพื่อ testability (**logic ทั้งหมดอยู่ในชั้นที่ unit test ได้ — hosted wrapper บางที่สุด**):

**(ก) `iLearn.API/Services/DeadlineDigestService : IDeadlineDigestService`** (ข้าง NotificationService — pattern เดียวกับ PLAN-088):

```csharp
public interface IDeadlineDigestService
{
    /// <summary>รัน digest 1 รอบ — idempotent ต่อวัน (เรียกซ้ำวันเดียวกัน = no-op)</summary>
    Task<int> RunOnceAsync(CancellationToken ct = default); // คืนจำนวน notification ที่สร้าง
}
```

Logic ใน `RunOnceAsync`:

1. `now = _dateTime.Now` (**ห้าม DateTime.Now/UtcNow ดิบ — กติกา CLAUDE.md**)
2. **Dedupe รายวัน:** ถ้ามี `Notification` ที่ `Type == DeadlineDigest && CreatedAt >= now.Date` อยู่แล้ว → return 0 ทันที (ทำให้เรียกกี่ครั้งก็ปลอดภัย รวมถึงตอน app restart)
3. รวบรวม assignment ที่เข้าเกณฑ์ (query เดียว, projection, ไม่โหลด entity เต็ม, **ห้ามแตะ FileStorage**):
   - ไม่ถูกลบ + `DueDate != null`
   - **due-soon:** `IsDueSoon(false, a.DueDate, now)` (ครบกำหนดภายใน 7 วันข้างหน้า)
   - **มี overdue learner:** นับจาก `EnrollmentAssignment` links ของ assignment ที่ `!SnapshotCompleted && Enrollment != null && !Enrollment.IsCompleted && link.DueDate < now` (นับ **link-level ตาม effective dates** — ห้ามใช้ Enrollment.DueDate ดิบ)
   - assignment เดียวเข้าได้ทั้ง 2 เกณฑ์
4. Group ตาม `Assignment.DivisionId` → หา recipients:
   - division X → ทุก `User.Nid` ที่มี role `DivisionId == X` (ผ่าน UserRole join, filter soft-delete ตามปกติ)
   - **SuperAdmin** (role `RoleType == SuperAdmin`) → ได้ digest **org-wide 1 ฉบับ** (รวมทุก division + assignment ที่ `DivisionId == null`)
   - assignment `DivisionId == null` → อยู่เฉพาะใน digest ของ SuperAdmin (ไม่ spam admin division อื่น)
5. **1 notification ต่อ recipient ต่อวัน** (สรุปยอด ไม่ยิงรายตัว — กัน spam):
   - Type `DeadlineDigest`, Level = `error` ถ้ามี overdue > 0, ไม่งั้น `info`
   - Title: `สรุปงานใกล้ครบกำหนดประจำวัน`
   - Message เช่น: `ครบกำหนดใน 7 วัน: 3 งาน · มีผู้เรียนเกินกำหนด: 2 งาน (15 คน)` (ตัวเลขจริงจากข้อ 3)
   - LinkPath: `/assignments`
   - ส่งผ่าน `INotificationService.NotifyAsync` เดิม (ได้ SignalR push + error-swallow ฟรี)
6. **ไม่มีอะไรเข้าเกณฑ์ = ไม่ส่ง** (ห้ามส่ง digest ว่าง)
7. **Retention (ข้อ 3 ของ scope) รันต่อท้ายในรอบเดียวกัน**

**(ข) `iLearn.API/Services/DeadlineDigestHostedService : BackgroundService`** — wrapper บาง:

- Loop: คำนวณเวลารันถัดไป = 08:00 (เวลาไทยจาก `IDateTime.Now`) ของวันนี้ถ้ายังไม่ถึง / พรุ่งนี้ถ้าเลยแล้ว → `Task.Delay` ถึงเวลานั้น → สร้าง scope (`IServiceScopeFactory` — DbContext เป็น scoped ห้าม inject ตรง) → เรียก `RunOnceAsync`
- **ตอน start app:** เรียก `RunOnceAsync` ทันที 1 ครั้ง (dedupe ข้อ 2 กันซ้ำเอง — ครอบเคส app recycle หลัง 08:00 แล้ววันนั้นยังไม่ได้ส่ง)
- ทุก iteration ห่อ try/catch + `ILogger.LogError` — **ห้าม exception หลุดจน host ตาย** และห้าม loop แตก (delay สั้นสุด 1 นาทีก่อน retry)
- ลงทะเบียน: `services.AddScoped<IDeadlineDigestService, DeadlineDigestService>()` + `services.AddHostedService<DeadlineDigestHostedService>()` ใน `PresentationExtensions`

### 3. Retention

- const `NotificationRetentionDays = 90` (ไว้ที่เดียวกับ `NotificationTypes`)
- ใน `RunOnceAsync` ท้ายรอบ: ลบ `Notifications` ที่ `CreatedAt < now.AddDays(-90)` — ใช้ fetch-ids-then-RemoveRange เป็น batch (`Take(500)` วนจนหมด) **ไม่ใช้ `ExecuteDeleteAsync`** (InMemory test ไม่รองรับ + volume รายวันเล็ก)
- ลบ = hard delete (notification เป็นข้อมูล transient ไม่ใช่ audit record)

### 4. Endpoint paging (contract §1)

- `NotificationsController.GetNotifications` เพิ่ม `[FromQuery] int skip = 0` → ส่งต่อ service
- `NotificationService.GetForUserAsync(userId, unreadOnly, take, skip)` — เพิ่ม `TotalCount` (COUNT หลัง filter เดียวกับ items ก่อน Skip/Take) — index เดิม `(RecipientUserId, IsRead, CreatedAt DESC)` ครอบอยู่แล้ว ไม่ต้องแตะ schema

### 5. Tests (`iLearn.Tests` — ต่อไฟล์ NotificationServiceTests หรือไฟล์ใหม่ DeadlineDigestServiceTests)

- digest: รันซ้ำวันเดียวกัน → ครั้งที่ 2 คืน 0, ไม่มี row เพิ่ม (**เคสสำคัญสุด**)
- recipient: admin division A ได้เฉพาะ assignment division A; SuperAdmin ได้ org-wide รวม division null; admin division B ที่ไม่มีอะไรเข้าเกณฑ์ → ไม่ได้ digest
- overdue นับจาก link.DueDate ไม่ใช่ Enrollment.DueDate (สร้างเคส link future + enrollment past → ไม่นับ)
- ไม่มี assignment เข้าเกณฑ์ → 0 notifications
- retention: row อายุ 91 วันถูกลบ, 89 วันอยู่ (ใช้ FakeDateTime fixed)
- paging: skip/take + TotalCount ถูก (ต่อจาก test เดิม)

## Contract ที่เปลี่ยน

- `NotificationListDto` **+TotalCount** (additive), `GET Notifications` **+skip** — PLAN-091 ต้องอัปเดต mirror
- ใหม่: `NotificationTypes.DeadlineDigest`, `IDeadlineDigestService`, hosted service
- DB schema: **ไม่เปลี่ยน** (ไม่มี migration)

## นอก Scope (ห้ามทำ)

- ห้ามแตะ React (PLAN-091 คู่ขนาน)
- ห้ามทำ email / learner-side / preferences-mute
- ห้ามแก้ endpoint/hook เดิมของ PLAN-088 นอกจาก signature `GetForUserAsync` ตาม §4
- ห้ามทำ per-assignment notification รายตัว (digest สรุปเท่านั้น — กัน spam)
- config เวลารัน (08:00) เป็น const พอ ไม่ต้องทำ appsettings (ยังไม่มีเหตุต้องเปลี่ยนต่อ environment)

## Verification

```powershell
dotnet build iLearn.Tests -o artifacts\verify-test
dotnet test artifacts\verify-test\iLearn.Tests.dll
Remove-Item -Recurse -Force artifacts\verify-test
```

ทดสอบมือ (API local):

1. Start API → log แสดง digest รอบ startup (ถ้ามีของเข้าเกณฑ์วันนี้และยังไม่เคยส่ง) → bell มี digest
2. Restart API ซ้ำทันที → **ไม่มี digest ใหม่** (dedupe)
3. สร้าง assignment ที่ DueDate ภายใน 7 วัน → รุ่งขึ้น (หรือ mock) digest มีเลขถูก
4. `GET Notifications?skip=20&take=20` → หน้า 2 ถูก + TotalCount คงที่
5. ตรวจว่า endpoint เดิม (ไม่ส่ง skip) พฤติกรรมเดิมเป๊ะ
6. w3wp ไม่ตาย/ไม่ log error รัว ๆ หลังทิ้งไว้ข้ามชั่วโมง

## Implementer Notes

- เพิ่ม `NotificationListDto.TotalCount`, `skip` ให้ `GET api/Notifications` และ clamp `skip` เป็นศูนย์เมื่อส่งค่าติดลบ; response เดิมยังเป็น `{ success, data }` และ endpoint ที่ไม่ส่ง `skip` ยังคงพฤติกรรมเดิม
- เพิ่ม `NotificationTypes.DeadlineDigest` และ `NotificationRetentionDays = 90`; ไม่มี migration เพราะ schema เดิมรองรับอยู่แล้ว
- เพิ่ม `IDeadlineDigestService` และ `DeadlineDigestService`: dedupe ต่อวัน, ใช้ `IDateTime.Now`, อ่าน overdue จาก `EnrollmentAssignment.DueDate`, รวม digest หนึ่งฉบับต่อ recipient ต่อวัน, จำกัด division admin ตาม role และให้ SuperAdmin เห็นทุก division รวม assignment ที่ไม่มี division
- เพิ่ม `DeadlineDigestHostedService`: run ทันทีเมื่อ API start, แล้ว schedule เวลา 08:00 ตามเวลาไทย; ทุก run สร้าง DI scope ใหม่, ส่ง cancellation token, log/retry ทุก 1 นาทีเมื่อ error เพื่อไม่ให้ host ตาย
- เพิ่ม hard-delete retention เป็น batch ละ 500 รายการสำหรับ notification ที่เก่ากว่า 90 วัน
- เพิ่ม `DeadlineDigestServiceTests` 5 cases: daily dedupe, division/SuperAdmin recipient scope, effective link due date, no empty digest และ retention; เพิ่ม paging/TotalCount regression test ใน `NotificationServiceTests`
- Verification: `dotnet build iLearn.Tests -o artifacts\\verify-plan090-final` ผ่าน และ `dotnet test artifacts\\verify-plan090-final\\iLearn.Tests.dll` ผ่าน **203/203**; ลบ artifacts หลังจบแล้ว
- Manual verification ที่ยังต้องทำบน environment จริง: start/restart API เพื่อตรวจ digest startup/dedupe, ตรวจ run เวลา 08:00 จาก log, และ smoke `GET Notifications?skip=20&take=20` กับ Windows-auth user จริง

## Reviewer Sign-off (Claude Code, 2026-07-17)

ตรวจ diff เต็ม + build/test อิสระ (`dotnet test` **203 passed** — 6 DeadlineDigestServiceTests ใหม่ครบทุกเคสในแผน §5 รวม idempotency/link-DueDate/retention):

- **DeadlineDigestService:** `IDateTime.Now` ทุกจุด ✅ dedupe รายวันด้วย `CreatedAt >= now.Date` (source เดียวกับที่ SaveChanges ใช้ — timezone ตรง) ✅ overdue นับจาก `link.DueDate` + `!SnapshotCompleted && !IsCompleted` ตาม effective-dates ✅ `IsDueSoon` ไม่นับวันเลยกำหนด (>= today) — ไม่นับซ้อนกับ overdue ✅ SuperAdmin ได้ org-wide + ไม่โดนส่งซ้ำจาก division role ✅ digest ว่างไม่ส่ง ✅ retention batch RemoveRange 500 รันแม้วันที่ dedupe-skip ✅ query 2 ก้อนไม่มี N+1, AsNoTracking, global soft-delete filter ครอบ ✅
- **HostedService:** wrapper บางตามสเปค — startup run ทันที (dedupe กันซ้ำ), scope ต่อรอบ, try/catch ครอบ iteration + retry 1 นาที, next run 08:00 จาก IDateTime, host ไม่มีทางตาย ✅
- **Contract §1:** `TotalCount` (COUNT ก่อน Skip/Take ตาม filter เดียวกัน) + `skip` clamp ≥0 + `DeadlineDigest`/`NotificationRetentionDays` const — ตรง freeze; endpoint เดิมพฤติกรรมเดิม ✅
- Observation (ยอมรับได้): dedupe เป็น global รายวัน — ถ้า run ตายกลางคัน recipient ที่เหลือจะไม่ได้รับจนวันถัดไป (ตามสเปคแผนเป๊ะ; โอกาสต่ำ ไม่บล็อก)
- **CLAUDE.md กติกาใหม่ได้ผลจริง:** รอบนี้ไม่มี finding เรื่อง DateTime/effective dates/migration path เลย — ครั้งแรกที่ backend ผ่านสะอาด

**สรุป: ผ่านรีวิว ไม่มี finding ต้องแก้ — scheduler ตัวแรกของระบบเขียนได้มาตรฐานดี**
