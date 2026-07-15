# PLAN-088: Notifications Phase 1 — Backend (Notification entity + per-user SignalR + 4 event hooks)

- **Status:** DONE → VERIFIED — Finding 1+2 FIXED (Claude Code 2026-07-14: ReadAt ใช้ IDateTime + ย้าย migration เข้า convention)
- **Assigned:** GitHub Copilot
- **Reviewer:** Claude Code
- **สร้างเมื่อ:** 2026-07-14
- **คู่ขนานกับ:** [PLAN-089](PLAN-089-notifications-frontend.md) (Gemini ทำ React จาก contract ในแผนนี้) — **API contract §2 ถูก freeze แล้ว** ห้ามเปลี่ยน shape โดยไม่อัปเดตทั้ง 2 แผน + AGENT_LOG (กติกาเดียวกับ PLAN-086/087 ที่ได้ผลดี)

> ผู้ใช้สั่ง (2026-07-14): ทำระบบ Notifications — Phase 1 เลือก **Admin bell อย่างเดียว** + event เฉพาะ **"งานของฉันเสร็จ/พัง"** (event-driven ล้วน ไม่ต้องมี scheduler/email)

---

## บริบท (ยืนยันจากโค้ดจริงแล้ว)

| มีอยู่แล้ว | สถานะ |
|---|---|
| SignalR hub `AdminActivityHub` (`[Authorize(AdminOnly)]`) + `MapHub("/hubs/admin-activity")` | ใช้ได้ — แต่ notifier ปัจจุบันส่ง **`Clients.All`** (ไม่ target ผู้รับ) |
| `AdminActivity` log (9 action types) + `IAdminActivityRealtimeNotifier` | เป็น "log ว่าใครทำอะไร" ไม่มี read-state/targeting → **ไม่ใช่ notification** |
| `AppDbContext.SaveChangesAsync` override (บรรทัด ~319) เซ็ต `BaseEntity.CreatedBy = _currentUserService.UserId` อัตโนมัติ | ✅ ทำให้รู้ว่า "งานของใคร" |
| `ICurrentUserService.UserId` = Nid ล้วน เช่น `N4734` (ไม่มี domain prefix) | ✅ ใช้เป็น recipient key |
| bell icon ใน `Header.tsx` | เป็นปุ่มตาย ไม่มี onClick |

**ไม่มี:** Notification table, read-state, UserIdProvider ของ SignalR, email infra, background scheduler → สโคปนี้จึงไม่แตะเรื่องพวกนั้นเลย

> **คุณค่าที่แท้จริงของ Phase 1** (เขียนไว้กันหลงทาง): งานพวกนี้เป็น sync request ที่ admin เห็น toast อยู่แล้ว — notification จึงมีค่าตรง (1) งานยาว เช่น อัป SCORM 1GB ที่ admin อาจปิดแท็บ/หลุดก่อนรู้ผล และ (2) เป็น **ประวัติงานของฉัน** ย้อนดูได้ว่าเมื่อวานอันไหนพัง. อย่าทำให้มันซ้ำ toast โดยไม่จำเป็น — ห้าม notify งานสั้นที่ไม่มีคุณค่าย้อนดู

## Scope

### 1. Entity + migration

`iLearn.Domain/Entities/Notification.cs` (สืบทอด `BaseEntity` → ได้ `Id/CreatedAt/CreatedBy/IsDeleted` ฟรี):

```csharp
public class Notification : BaseEntity
{
    public string RecipientUserId { get; set; } = string.Empty; // Nid ล้วน เช่น "N4734" — ตรงกับ ICurrentUserService.UserId
    public string Type { get; set; } = string.Empty;            // NotificationTypes.* (ข้อ 3)
    public string Level { get; set; } = string.Empty;           // "success" | "error" | "info"
    public string Title { get; set; } = string.Empty;
    public string? Message { get; set; }
    public string? LinkPath { get; set; }                       // path ฝั่ง React เช่น "/courses/123" (null = ไม่มี deep link)
    public string? EntityType { get; set; }
    public int? EntityId { get; set; }
    public bool IsRead { get; set; }
    public DateTime? ReadAt { get; set; }
}
```

- Fluent API ใน `AppDbContext`: `RecipientUserId` HasMaxLength(100) **Required**, `Type`/`Level` HasMaxLength(50), `Title` HasMaxLength(200), `Message` HasMaxLength(1000), `LinkPath` HasMaxLength(300), `EntityType` HasMaxLength(100)
- **Index (สำคัญต่อ performance ของ bell):** composite `(RecipientUserId, IsRead, CreatedAt DESC)` — ทุก query กรองด้วย 3 คอลัมน์นี้
- Migration `AddNotifications`
- `NotificationTypes`/`NotificationLevels` เป็น `static class` const ใน `iLearn.Application/Common/` (แนวเดียวกับ `AssignmentStatusKeys`) — **ห้าม hardcode string ที่ call site**

### 2. API contract (FREEZE — PLAN-089 mirror จากตรงนี้)

`iLearn.Application/DTOs/NotificationDtos.cs`:

```csharp
public class NotificationDto
{
    public int Id { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Level { get; set; } = string.Empty;   // success | error | info
    public string Title { get; set; } = string.Empty;
    public string? Message { get; set; }
    public string? LinkPath { get; set; }
    public string? EntityType { get; set; }
    public int? EntityId { get; set; }
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class NotificationListDto
{
    public int UnreadCount { get; set; }               // ของ recipient ทั้งหมด (ไม่ใช่แค่ในหน้านี้)
    public List<NotificationDto> Items { get; set; } = new();
}
```

`iLearn.API/Controllers/NotificationsController.cs` — `[Authorize(Policy = "AdminOnly")]`, route `api/Notifications`, wrapper `Ok(new { success = true, data = ... })` ทุกตัว:

| Endpoint | คืน | หมายเหตุ |
|---|---|---|
| `GET api/Notifications?unreadOnly=false&take=20` | `NotificationListDto` | take clamp 1–50; เรียง `CreatedAt DESC`; **เฉพาะของ current user เสมอ** |
| `GET api/Notifications/unread-count` | `{ unreadCount: int }` | ใช้กับ badge (เบาสุด — COUNT อย่างเดียว) |
| `POST api/Notifications/{id}/read` | `{ unreadCount: int }` | 404 ถ้า id ไม่ใช่ของ current user (**ห้ามให้อ่าน/แก้ของคนอื่นเด็ดขาด**) |
| `POST api/Notifications/read-all` | `{ unreadCount: 0 }` | mark ทุกอันของ current user |

**SignalR event (contract ด้วย):** `"NotificationCreated"` payload = `NotificationDto` ตัวเดียว (shape เดียวกับ REST เป๊ะ)

### 3. Service + event types

`INotificationService` / `NotificationService` (`iLearn.Infrastructure/Services/` — วางที่เดียวกับ `AdminActivityService` เพราะต้องใช้ hub):

```csharp
Task NotifyAsync(string recipientUserId, string type, string level, string title,
                 string? message = null, string? linkPath = null,
                 string? entityType = null, int? entityId = null);
Task<NotificationListDto> GetForUserAsync(string userId, bool unreadOnly, int take);
Task<int> GetUnreadCountAsync(string userId);
Task<int> MarkReadAsync(string userId, int notificationId);   // คืน unreadCount ใหม่; throw KeyNotFound ถ้าไม่ใช่ของ user
Task<int> MarkAllReadAsync(string userId);
```

**`NotifyAsync` ต้องไม่ทำให้ request หลักพังเด็ดขาด** — ห่อ try/catch ทั้งก้อน (รวม INSERT + SignalR push) แล้ว `_logger.LogWarning` เมื่อพัง (pattern เดียวกับ `AdminActivityService` ที่ catch `SqlException 208` ตอนตารางยังไม่มี) — งานหลักสำเร็จแล้วห้าม rollback เพราะแจ้งเตือนพัง

`NotificationTypes` (const):
- `ScormUploadSucceeded` / `ScormUploadFailed`
- `ContentPublishSucceeded` / `ContentPublishFailed`
- `BatchPublishCompleted`
- `BulkAssignCompleted`

### 4. SignalR targeting (จุดเสี่ยงที่สุดของแผนนี้ — อ่านให้ครบ)

ปัจจุบัน `SignalRAdminActivityNotifier` ส่ง `Clients.All` และ **ไม่มี `IUserIdProvider`** ⇒ `Clients.User(...)` จะยังใช้ไม่ได้จนกว่าจะทำข้อนี้

- เพิ่ม `iLearn.API/Services/NidUserIdProvider : IUserIdProvider` — คืน **Nid ล้วน** ให้ตรงกับ `ICurrentUserService.UserId` เป๊ะ:
  - Windows auth ⇒ `Context.User?.Identity?.Name` = `"NIKONOA\n4734"` → ต้อง strip domain prefix
  - **ห้าม hardcode `"NIKONOA\"`** — อ่านจาก config `Authentication:DomainPrefix` (มีอยู่แล้วใน appsettings) และ strip แบบ case-insensitive; ถ้าไม่มี prefix ให้ใช้ค่าเดิม; fallback = ตัดทุกอย่างก่อน `\` ตัวสุดท้าย
  - **ต้อง lowercase/normalize ให้ตรงกับที่ `ICurrentUserService.UserId` คืน** — ไปดูโค้ด `CurrentUserService` จริงก่อน แล้วทำให้ normalize เหมือนกันเป๊ะ (ถ้ามันคืน `N4734` ตัวใหญ่ ก็ต้องตรงแบบนั้น) มิฉะนั้น push จะเงียบหาย (ไม่ error — หาบั๊กยากมาก)
  - ลงทะเบียน `services.AddSingleton<IUserIdProvider, NidUserIdProvider>()` ใน `PresentationExtensions`
- ส่ง notification ด้วย `_hubContext.Clients.User(recipientUserId).SendAsync("NotificationCreated", dto)`
- **reuse `AdminActivityHub` เดิม** (client เชื่อมอยู่แล้ว, `[Authorize(AdminOnly)]` ครบ) — ห้ามสร้าง hub ใหม่
- **ห้ามแตะ** `NotifyCreatedAsync`/`AdminActivityCreated` เดิม (Dashboard พึ่งอยู่) — เพิ่ม path ใหม่ข้าง ๆ เท่านั้น

### 5. Event hooks (4 จุด — "งานของฉัน" = `_currentUser.UserId` ของ request นั้น)

ทุกจุด: **notify หลังงานสำเร็จ/ล้มเหลวจริงเท่านั้น** และ recipient = `_currentUser.UserId` เสมอ

| # | จุด hook | สำเร็จ | ล้มเหลว |
|---|---|---|---|
| 1 | `CoursesController` `POST {courseId}/versions` + `PUT versions/{versionId}` (บรรทัด ~358/416 — เส้นทางอัป SCORM จาก CourseEditor) | `ScormUploadSucceeded` — title เช่น `อัปโหลด SCORM สำเร็จ`, message = ชื่อคอร์ส/ไฟล์, `LinkPath = "/courses/{courseId}"` | catch `InvalidScormPackageException` → `ScormUploadFailed` level `error`, message = `ex.Message` แล้ว **rethrow ตามเดิม** (พฤติกรรม HTTP ห้ามเปลี่ยน) |
| 2 | `ContentItemsController` `POST SetPublic` (~บรรทัด 292) | `ContentPublishSucceeded`, `LinkPath = "/content-library/{id}"` | catch `InvalidScormPackageException` → `ContentPublishFailed` (ยังคืน 400 เหมือนเดิม) |
| 3 | `ContentItemsController` `POST Admin/BatchPublish` (~บรรทัด 570) | `BatchPublishCompleted` — message สรุป `สำเร็จ X รายการ, ล้มเหลว Y รายการ`; level = `success` เมื่อ Y=0, ไม่งั้น `info` | — (มันรวบผลอยู่แล้ว ไม่ throw) |
| 4 | `EnrollmentsController` `POST BulkAssign` (บรรทัด ~463) | `BulkAssignCompleted` — message = จำนวน learner/course + assignmentNo, `LinkPath = "/assignments/{assignmentId}"` | ไม่ต้อง notify กรณี validation fail (admin เห็น error ทันทีในฟอร์ม ไม่มีคุณค่าย้อนดู) |

- **`Admin/BatchPublishStream` (~บรรทัด 619) ไม่ต้องแตะ** — มัน stream ผลทีละรายการให้ client อยู่แล้ว
- Hook ที่ **controller** (จุดที่รู้ผลรวมของ request) ไม่ใช่ใน service ลึก ๆ — กัน notify ซ้ำเมื่อ service ถูกเรียกจากหลายที่

### 6. Tests (`iLearn.Tests/NotificationServiceTests.cs`)

- `GetForUserAsync` คืนเฉพาะของ user นั้น (ใส่ noise ของ user อื่นแล้วต้องไม่หลุด) + เรียง CreatedAt DESC + take clamp
- `MarkReadAsync` ของ user อื่น → `KeyNotFoundException` (**เคสความปลอดภัย ห้ามขาด**)
- `MarkAllReadAsync` → unreadCount = 0 และไม่แตะของ user อื่น
- `unreadOnly=true` คืนเฉพาะที่ยังไม่อ่าน แต่ `UnreadCount` ยังเป็นยอดรวมทั้งหมดของ user
- `NotifyAsync` เมื่อ repo/hub โยน exception → **ไม่ throw ออกมา** (กลืน + log)

## Contract ที่เปลี่ยน

- **ใหม่:** ตาราง `Notifications` (+index), DTOs/endpoints ตาม §2, SignalR event `NotificationCreated`, `IUserIdProvider`
- **ไม่แตะ:** endpoint/DTO เดิมทุกตัว, `AdminActivityCreated` event, พฤติกรรม HTTP ของ 4 endpoint ที่ hook (status code/body เดิมเป๊ะ)

## นอก Scope (ห้ามทำ)

- ห้ามแตะ React (PLAN-089 ของ Gemini ทำคู่ขนาน)
- ห้ามทำ email / scheduler / digest / learner-side notification (Phase ถัดไป — ระบบยังไม่มี infra พวกนี้เลย)
- ห้ามเปลี่ยน `AdminActivity` / เอา notification ไปผูกกับ activity log (คนละความหมาย: log = ใครทำอะไร, notification = สิ่งที่ฉันต้องรู้ + read-state)
- ห้ามแตะ MVC admin เดิม
- ไม่ต้องทำ retention/cleanup job รอบนี้ (จดเป็น Phase 2 — ตารางจะโตเรื่อย ๆ)

## Verification

```powershell
dotnet build iLearn.Tests -o artifacts\verify-test
dotnet test artifacts\verify-test\iLearn.Tests.dll
Remove-Item -Recurse -Force artifacts\verify-test
```

ทดสอบมือ (API local + Swagger/curl):

1. อัป SCORM ผ่าน CourseEditor (ไฟล์ปกติ) → `GET api/Notifications` มี `ScormUploadSucceeded` ของ user ตัวเอง + `LinkPath` ถูก
2. อัป ZIP เสีย → ได้ `ScormUploadFailed` level `error` **และ HTTP response ยังเป็น 400 เหมือนเดิม**
3. `unread-count` ขยับถูก → `POST {id}/read` → count ลด 1 → `read-all` → 0
4. ลอง `POST {id}/read` ด้วย id ของ user อื่น → **404** (ไม่ใช่ 200)
5. **SignalR targeting (จุดเสี่ยงสูงสุด):** เปิด admin 2 คน (2 เครื่อง/2 บัญชี) → คนที่ 1 อัป SCORM → **คนที่ 1 เท่านั้นได้ `NotificationCreated`**, คนที่ 2 ต้องไม่ได้ — ถ้า push ไม่ถึงใครเลย ให้ตรวจ `NidUserIdProvider` ว่า normalize ตรงกับ `ICurrentUserService.UserId` (ดู §4)
6. BatchPublish หลายรายการ (ผสมดี/พัง) → ได้ 1 notification สรุปยอดถูก
7. BulkAssign → ได้ notification + `LinkPath` เปิดหน้า assignment ได้จริง
8. Dashboard เดิม: activity feed realtime **ยังทำงานเหมือนเดิม** (ห้าม regress)

## Implementer Notes

- ใช้ `LastIndexOf('\\')` ใน `NidUserIdProvider` แทน config-based domain prefix strip — เพราะ `CurrentUserService` ก็ใช้ `Split('\\')` ไม่ได้อ่าน config เลย ทำให้ behavior ตรงกัน 100% โดยไม่ต้องพึ่ง config
- `NotificationService` วางใน `iLearn.API/Services/` (ไม่ใช่ Infrastructure) เพราะต้อง inject `IHubContext<AdminActivityHub>` ซึ่งอยู่ใน API layer เท่านั้น — แนวเดียวกับ `SignalRAdminActivityNotifier`
- เพิ่ม `Microsoft.EntityFrameworkCore.InMemory` package ใน iLearn.Tests สำหรับ test NotificationService (ต้องใช้ real DbContext เพราะ service ใช้ LINQ queries ตรง)
- existing tests 2 ไฟล์ (`ContentItemsControllerTests`, `EnrollmentsPlayerInfoTests`) ต้องเพิ่ม constructor args — ใส่ `NullNotificationService` + `FakeCurrentUserService` fakes
- Migration `20260715024809_AddNotifications` สร้างตาราง + composite index สำเร็จ

## Reviewer Sign-off (Claude Code, 2026-07-14)

ตรวจ diff เต็มทุกไฟล์ + build/test อิสระ: `dotnet build` 0 errors, `dotnet test` **195 passed** (8 NotificationServiceTests ใหม่ครบทุกเคสในแผน รวมเคสความปลอดภัย)

- **จุดเสี่ยงสูงสุด (§4 SignalR targeting) — ผ่าน:** `NidUserIdProvider` strip ด้วย `LastIndexOf('\')` vs `CurrentUserService` ที่ใช้ `Split('\')[1]` → เคสจริง `NIKONOA\n4734` ได้ `n4734` **ตรงกันทั้งคู่** ✅ (ต่างกันเฉพาะเคสหลาย backslash ซึ่ง Windows domain\user ไม่มี — ไม่ใช่บั๊กจริง) ลงทะเบียน `IUserIdProvider` + `Clients.User(...)` ถูกต้อง; `AdminActivityCreated`/`Clients.All` เดิมไม่ถูกแตะ ✅
- **Contract §2:** DTOs + 4 endpoints + wrapper + 404 ตรง freeze ทุก field ✅
- **Service:** `NotifyAsync` ห่อ try/catch ทั้งก้อน + LogWarning → งานหลักไม่พังแม้ notify ล้ม ✅; `MarkReadAsync` filter `RecipientUserId` → KeyNotFound (กันอ่านของคนอื่น) ✅; unreadCount = ยอดรวมทั้งหมด ✅; clamp 1–50 ✅
- **Hooks 4 จุดครบ** (CoursesController ×2 endpoint, SetPublic, BatchPublish, BulkAssign) — success/fail ตามตาราง, **HTTP status/body เดิมทุกจุด** ✅; `NotificationTypes`/`NotificationLevels` เป็น const ไม่มี hardcode ✅
- **Schema:** fluent config + index composite `IX_Notifications_Recipient_Read_CreatedDesc` (`RecipientUserId, IsRead, CreatedAt DESC`) ตรงสเปค ✅

### ⚠️ Finding 1 (MEDIUM — ต้องแก้): `ReadAt` ใช้ `DateTime.UtcNow` → เพี้ยน 7 ชั่วโมง
`NotificationService.MarkReadAsync`/`MarkAllReadAsync` เซ็ต `ReadAt = DateTime.UtcNow` แต่ทั้งระบบใช้ `IDateTime.Now` ซึ่ง `DateTimeService.Now => DateTime.UtcNow.AddHours(7)` (**เวลาไทย**) และ `CreatedAt` ก็ถูกเซ็ตด้วยค่านี้ผ่าน `SaveChanges` interceptor
⇒ ในแถวเดียวกัน `ReadAt` จะ **น้อยกว่า** `CreatedAt` เสมอ (ดูเหมือนอ่านก่อนถูกสร้าง 7 ชม.) — ตอนนี้ยังไม่พังหน้าจอเพราะ `ReadAt` ไม่ได้อยู่ใน DTO แต่เป็นข้อมูล audit ที่ผิดฝังใน DB และผิด convention ทั้งระบบ
**แก้:** inject `IDateTime` เข้า `NotificationService` แล้วใช้ `_dateTime.Now` แทน `DateTime.UtcNow` (2 จุด)

### Finding 2 (MINOR/convention): migration อยู่คนละโฟลเดอร์+namespace กับของเดิม
migration ใหม่อยู่ `iLearn.Infrastructure/Persistence/Migrations/` namespace `iLearn.Infrastructure.Persistence.Migrations` ขณะที่ของเดิมทั้งหมด (รวม `AppDbContextModelSnapshot` ที่ถูก update ถูกต้อง) อยู่ `iLearn.Infrastructure/Migrations/` namespace `iLearn.Infrastructure.Migrations`
**ไม่กระทบ runtime** (EF scan migration จาก assembly ไม่ใช่ path — ยืนยันว่าไม่มี `MigrationsAssembly` override) แต่ทำให้ไฟล์กระจาย 2 ที่ และ `dotnet ef migrations add` ครั้งหน้าจะไปลงอีกที่ ⇒ ควรย้ายไฟล์ 2 ตัว + แก้ namespace ให้ตรงของเดิม

### Observation (ยอมรับได้ — ไม่ต้องแก้)
- **`NotificationService` วางที่ `iLearn.API/Services/` + ใช้ `AppDbContext` ตรง** ต่างจากที่แผนเขียน (`Infrastructure` + repo pattern) — **ผมเขียนแผนพลาดเอง**: Infrastructure ไม่รู้จัก `AdminActivityHub` (อยู่ใน API) การวางที่ API จึงถูกกว่า และ precedent มีอยู่แล้ว (`SignalRAdminActivityNotifier` ก็อยู่ที่นี่)
- **`iLearn.Tests.csproj` +`Microsoft.EntityFrameworkCore.InMemory`** — test ใช้ EF InMemory แทน hand-rolled `InMemoryGenericRepository` แบบ `ReportServiceTests` เพราะ service ผูกกับ `AppDbContext` ตรง; ยอมรับได้ (test เขียว 195) แต่เป็น pattern ที่ 2 ในโปรเจค
- `NotifyAsync` เรียก `_db.SaveChangesAsync()` บน scoped DbContext ร่วม — ปลอดภัยเพราะ repo pattern เดิม SaveChanges ทุก operation อยู่แล้ว (ไม่มี pending ค้างตอน hook ทำงาน) และถ้าพังก็ถูกกลืนใน try/catch

**สรุป: ผ่านรีวิว — สถาปัตยกรรม/contract/ความปลอดภัย/targeting ถูกครบ. ต้องแก้ Finding 1 (ReadAt timezone) + ย้าย migration ตาม Finding 2 ก่อนปิด**

## Fix Findings (Claude Code, 2026-07-14 — ผู้ใช้สั่งแก้เอง)

- **Finding 1 (MEDIUM) FIXED:** `NotificationService` inject `IDateTime` แล้วใช้ `_dateTime.Now` แทน `DateTime.UtcNow` ทั้ง `MarkReadAsync`/`MarkAllReadAsync` → `ReadAt` อยู่ timezone เดียวกับ `CreatedAt` (UTC+7) ไม่เป็นค่าที่ดูเหมือน "อ่านก่อนถูกสร้าง 7 ชม." อีก
- **Finding 2 (convention) FIXED:** ย้าย `20260715024809_AddNotifications.cs` + `.Designer.cs` จาก `iLearn.Infrastructure/Persistence/Migrations/` → `iLearn.Infrastructure/Migrations/` (ที่เดียวกับ migration อื่นทั้งหมด + snapshot) และแก้ namespace → `iLearn.Infrastructure.Migrations`; ลบโฟลเดอร์ว่างทิ้ง
- **Regression tests เพิ่ม 2 ตัว:** `MarkReadAsync_SetsReadAtFromIDateTime_NotRawUtc`, `MarkAllReadAsync_SetsReadAtFromIDateTime` (ใช้ `FakeDateTime(fixedNow)` — ปรับ `FakeDateTime` ให้รับเวลาคงที่แบบ optional โดยค่า default ยังเดินจริงเพื่อไม่ให้ ordering test เดิมพัง)
- Verified: `dotnet build` 0 errors, `dotnet test` **197 passed**
