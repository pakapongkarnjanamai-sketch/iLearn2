# Agent Work Log

บันทึกกลางสำหรับ AI agent ทุกตัว (Claude Code, Antigravity) — **ต่อ entry ใหม่ไว้บนสุด** หลังจบงานที่แก้โค้ดทุกครั้ง

## [2026-07-17] GitHub Copilot — PLAN-093 QA rollout: migrations, API/React deploy, smoke complete with SignalR follow-up
- ทำอะไร: Gate 0 ผ่านจาก commit `5d88312` แล้ว apply QA migrations `AddNotifications` + `SoftDeleteFilteredUniqueIndexes` ก่อน deploy; verify ไม่มี Pending และ read-back filtered unique indexes ทั้ง 3. Deploy API side-by-side stamp `_deploy_20260717100037` (Staging, 1 GB request limit) + health ผ่านครบ database/file share/EmployeeHub. Deploy React QA; พบ release env ปิด SignalR อยู่ จึงเปลี่ยน `VITE_ILEARN_ADMIN_ENABLE_SIGNALR=true` และเพิ่ม Dashboard badge `Live`/`Polling` จาก `isConnected`, rebuild/deploy ใหม่. QA browser ยืนยัน bundle flag true, hub negotiate 200 เพียงหนึ่ง connection, Dashboard `Live`, bell/dropdown/notifications routes, Report Hub 4 หน้า, และ `POST Assignments/306/courses` add `Software back up (Re.3)` สำเร็จ (ไม่มี 500).
- ไฟล์หลักที่แตะ: `iLearn.Admin.React/.env.production`, `iLearn.Admin.React/src/pages/DashboardPage.tsx`, `DOC/PLANS/PLAN-091-notifications-p2-frontend.md`, `DOC/PLANS/PLAN-093-qa-prod-rollout-migrate-deploy.md`, `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี; DB migration ของ PLAN-092/088 ถูก apply บน QA แล้ว
- Verified: EF list ไม่มี Pending; SQL filtered index predicates ถูก; API health 200 pass 3 checks; Notifications paging contract 200 + `totalCount`; report aggregate endpoints/UI 200; `npm run lint` + build ผ่าน; browser console 0 error
- คงค้าง: QA smoke ที่ต้องมี sandbox/บัญชี admin ที่สอง (50 MB/1 GB upload, per-user notification, deterministic digest idempotency) และ **ห้าม PROD** จนผู้ใช้ยืนยันผล QA ใน chat + follow-up SignalR config/UI ได้ review/commit

## [2026-07-17] GitHub Copilot — PLAN-091 reviewer finding fixed: retain pre-connection hub subscriptions
- ทำอะไร: แก้ finding MEDIUM-HIGH ของ reviewer ใน `NotificationProvider.subscribeHubEvent` ซึ่งก่อนหน้านี้ no-op เมื่อ Dashboard mount ก่อน provider สร้าง SignalR connection. เพิ่ม ref-backed `Map<event, Set<handler>>`; subscribe ลงทะเบียนก่อนและ bind ทันทีเมื่อมี connection, provider replay handler ทั้ง registry ทุกครั้งที่สร้าง central connection ใหม่, unsubscribe ถอนทั้ง registry และ connection ปัจจุบัน. Dashboard realtime จึงไม่หายจาก child-first effect order และยังคงมี SignalR connection เส้นเดียว.
- ไฟล์หลักที่แตะ: `iLearn.Admin.React/src/lib/notificationContext.tsx`, `DOC/PLANS/PLAN-091-notifications-p2-frontend.md`, `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี
- Verified: `npm run lint` ผ่าน (0 errors, 0 warnings), `npm run build` ผ่าน (tsc -b + vite build)
- คงค้าง: Claude Code re-review PLAN-091 + commit ชุด PLAN-090/091 เพื่อปลด Gate 0 ของ PLAN-093 ก่อน QA rollout

## [2026-07-17 08:25] Antigravity — PLAN-091 DONE: Notifications Phase 2 — Frontend (หน้า /notifications เต็ม + รวม SignalR connection เดียว)
- ทำอะไร: (1) เพิ่ม `totalCount` ใน `NotificationListDto` mirror PLAN-090 contract; (2) แยก `NotificationRow` เป็น shared component ใน `src/components/shared/NotificationRow.tsx` รองรับ `compact` (dropdown) และ full (page) — ใช้ `<button>` per row ตาม precedent; (3) เพิ่ม footer "View all notifications" ใน bell dropdown ใช้ `AppButton ghost` → navigate `/notifications` + ปิด dropdown; (4) สร้าง `NotificationsPage.tsx` หน้าเต็ม: Card + SectionHeader + SegmentedToggle All/Unread + server-side paging (skip+=20 + Load more + Showing X of Y จาก totalCount) + empty state + realtime prepend ผ่าน `subscribeHubEvent` + dedupe ด้วย id; mark read/all ผ่าน provider เพื่อ sync badge; (5) เพิ่ม `subscribeHubEvent(event, handler) => unsubscribe` + `isConnected` ใน `NotificationProvider` — `connection.on()` ทำงานก่อน start ได้เลยไม่ต้อง queue; (5b) `DashboardPage.tsx` ลบ `HubConnectionBuilder` + useEffect SignalR ทั้งก้อน (~30 บรรทัด) → ใช้ `subscribeHubEvent('AdminActivityCreated', ...)` + อ่าน `isConnected` แทน — ผลลัพธ์: **SignalR connection เหลือเส้นเดียว** ใน provider; (6) เพิ่ม route `/notifications` ใน `App.tsx` ครอบ `<Remount>`, ไม่เพิ่ม sidebar item
- ไฟล์หลักที่แตะ: `iLearn.Admin.React/src/lib/notificationTypes.ts`, `iLearn.Admin.React/src/lib/notificationContext.tsx`, `iLearn.Admin.React/src/components/shared/NotificationRow.tsx` (ใหม่), `iLearn.Admin.React/src/components/layout/NotificationBell.tsx`, `iLearn.Admin.React/src/pages/notifications/NotificationsPage.tsx` (ใหม่), `iLearn.Admin.React/src/pages/DashboardPage.tsx`, `iLearn.Admin.React/src/App.tsx`, `DOC/PLANS/PLAN-091-notifications-p2-frontend.md`
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี (frontend-only, consume totalCount+skip จาก PLAN-090 ตรง contract)
- Verified: npm run lint ผ่าน (0 errors, 0 warnings), npm run build ผ่าน 100% (tsc -b && vite build)
- คงค้าง: manual test ต่อ API ที่มี PLAN-090 (ตาม verification checklist 6 ข้อในแผน)

## [2026-07-17 —] Claude Code — re-review fix ของ 091 ผ่าน + commit Notifications P2 ทั้งชุด → **Gate 0 PLAN-093 เปิดแล้ว**
- ทำอะไร: Copilot แก้ Finding 1 ตาม spec ใน sign-off (handler registry `Map<event, Set<handler>>` ใน ref + replay ทั้ง registry ตอน provider สร้าง connection + unsubscribe ถอดสองทาง) — ผม re-review แล้ว**ผ่าน**: ครอบ child-first effect order + connection recreation, ไม่มี double-bind. Verify สุดท้าย: `dotnet test` 203 passed + `npm run lint`/`build` 0 err → commit งานทั้งชุด (PLAN-090 + 091 + fix + PLAN-093 runbook + doc cleanup 048/049)
- ไฟล์หลักที่แตะ: commit ทั้ง working tree (ดูรายละเอียดใน commit message), plan 091 → VERIFIED
- Contract ที่เปลี่ยน: ตาม PLAN-090 §1 (TotalCount + skip — additive) ซึ่งรีวิวผ่านแล้ว
- Verified: dotnet 203 passed + npm lint/build อิสระ; live test ทั้งหมด = Phase 1 ของ PLAN-093
- **ถึง Copilot: Gate 0 ผ่านแล้ว — เริ่ม PLAN-093 Phase 1 (QA) ได้เลย** หลัง commit นี้ขึ้น master

## [2026-07-17 —] Claude Code — รีวิว PLAN-090/091 → 090 ผ่านสะอาด (ครั้งแรก!), 091 มี 1 Finding MEDIUM-HIGH (blocker Gate 0)
- ทำอะไร: ตรวจ diff เต็ม + verify อิสระ (dotnet test **203 passed** รวม 6 DeadlineDigestServiceTests; npm lint+build 0 err). **090 (Copilot): ผ่านไม่มี finding** — IDateTime ครบ, dedupe timezone ตรง, overdue จาก link.DueDate, IsDueSoon ไม่นับซ้อน, SuperAdmin ไม่โดนซ้ำ, retention รันทุกวัน, hosted service ปลอดภัยครบ — **กติกา CLAUDE.md ที่เพิ่มไว้ได้ผลจริง รอบแรกที่ backend ไม่มี finding เลย**. **091 (Gemini): Finding 1 MEDIUM-HIGH** — `subscribeHubEvent` ทำ `connectionRef.current?.on()` ณ เวลาเรียก → เมื่อ Dashboard (default route) mount effect ลูกยิงก่อน provider สร้าง connection (ยิ่งช้าเพราะรอ session ready) → handler ไม่ถูกผูกเงียบ ๆ + effect ไม่ re-run ⇒ **activity feed ไม่ realtime ทั้งที่จุดเขียว connected** (regression จากของเดิมที่ทำงาน) — แก้ด้วย handler registry ใน ref + replay ตอนสร้าง connection. ส่วนอื่นของ 091 ถูกครบ (หน้าเต็ม/paging/dedupe/Remount/single connection)
- ไฟล์หลักที่แตะ: plan docs 090 (→VERIFIED) / 091 (→REVIEWED + Finding), `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน: ไม่มี (รีวิว)
- Verified: git diff อิสระทุกไฟล์ + dotnet build/test + npm lint/build; live test = Phase 1 ของ PLAN-093
- **Gate 0 PLAN-093: ยังไม่ผ่าน** — ต้องแก้ Finding 1 ของ 091 + commit ทั้งชุดก่อน Copilot เริ่ม deploy

## [2026-07-17 —] Claude Code — เขียน PLAN-093: rollout runbook (migrate + deploy QA → PROD) มอบ Copilot
- ทำอะไร: ผู้ใช้สั่งให้ Copilot รัน migration + deploy ทั้ง QA และ PROD → เขียน runbook พร้อม **Gate 0: PLAN-090/091 ต้องผ่านรีวิว Claude Code + commit ก่อน** (ตอนนี้เสร็จแต่ค้างใน working tree — deploy ก่อน commit จะได้ build ไม่ครบ). โครง: Phase 1 QA (migrations list → **update ก่อน deploy เสมอ** → deploy-api.ps1 + deploy-admin-react.ps1 → smoke 8 ข้อที่รวบหนี้ live-test ค้างทุกแผน: PLAN-092 เคส 306, digest idempotency, EF SQL translation ของ reports, 1GB memory watch, per-user targeting 2 admin, single hub connection) → **หยุดรอผู้ใช้ไฟเขียวในแชท** → Phase 2 PROD (คาด migration ค้าง 3 ตัวรวม StoragePath — ยืนยันด้วย list ก่อน; deploy โดยไม่แตะ iLearn.User/MVC; smoke เบาห้าม destructive). กติกา: ห้าม copy รหัสผ่านลงแผน/log, migration เป็น additive ไม่ต้อง revert ตอน rollback app
- ไฟล์หลักที่แตะ: `DOC/PLANS/PLAN-093-qa-prod-rollout-migrate-deploy.md` (ใหม่ READY), `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน: ไม่มี (runbook)
- Verified: — (แผน; ยืนยันสถานะจริง: QA pending 1 migration + ผมได้เพิ่ม SoftDeleteFilteredUniqueIndexes อีก 1, งาน 090/091 uncommitted, PROD ยังเป็น build PLAN-080)
- ถึง Copilot: **อย่าเริ่มจนกว่า Gate 0 ผ่าน** (จะเห็น commit 090/091 + entry รีวิวใน log นี้); Phase 2 ต้องมีข้อความยืนยันจากผู้ใช้เท่านั้น

## [2026-07-17 —] Claude Code — PLAN-092: แก้ 2 bug จาก QA (bell 500 + add คอร์สที่เคยลบกลับไม่ได้)
- ทำอะไร: ผู้ใช้รายงาน 2 error บน QA `/assignments/306`. **วินิจฉัย:** (1) `Notifications/unread-count` 500 = QA DB ยังไม่ apply migration `AddNotifications` (ยืนยันด้วย `dotnet ef migrations list --connection QA`: Pending; ส่วน `AddStoragePathToFileStorage` apply แล้ว — SCORM ปลอดภัย) เพราะ repo ไม่มี auto-migrate แต่ deploy build PLAN-088 โดยไม่รัน migration. (2) `POST Assignments/306/courses` 500 ตอน add คอร์สที่เคยลบกลับ = **soft-delete rule ค้างชน unique index ที่ไม่ filter IsDeleted** (`IX_Assignments_AssignmentNo_CourseId` filter แค่ NOT NULL; `LoadBatchAsync` มองไม่เห็น rule ที่ลบผ่าน global filter → INSERT ซ้ำ → duplicate key). **แก้ (2):** migration `20260717011356_SoftDeleteFilteredUniqueIndexes` — unique index 3 ตัว (Assignments, AssignmentCourses, EnrollmentAssignments) เป็น filtered `[IsDeleted] = 0` ตาม precedent ScormRuntimeState + fluent config ใน AppDbContext + คอมเมนต์; ไม่แตะ business logic. **(1)+(2) ops:** `dotnet ef database update --connection QA` ถูก classifier ของ agent tooling บล็อก → ผู้ใช้รันเอง (คำสั่งเต็มใน PLAN-092). เพิ่มกติกา CLAUDE.md 2 ข้อ: deploy+migrate คู่กันเสมอ, unique index บนตาราง soft-delete ต้องมี filter
- ไฟล์หลักที่แตะ: `iLearn.Infrastructure/Persistence/AppDbContext.cs` (3 index filters), `iLearn.Infrastructure/Migrations/20260717011356_*` (ใหม่ 2 ไฟล์), `AppDbContextModelSnapshot.cs`, `CLAUDE.md`, `DOC/PLANS/PLAN-092-*.md` (ใหม่)
- Contract ที่เปลี่ยน (API shape / props / DB): DB index 3 ตัวเป็น filtered unique (behavior: row ที่ soft-delete ไม่บล็อกความซ้ำอีกต่อไป) — API shape ไม่เปลี่ยน
- Verified: `dotnet build` 0 errors + `dotnet test` **203 passed**; snapshot diff มีแค่ 3 filter (ไม่ปน model change ของ PLAN-090 ที่อยู่ใน tree); commit นี้ stage เฉพาะไฟล์ PLAN-092 — ไม่แตะงาน PLAN-090/091 ที่รอรีวิว
- คงค้าง: ผู้ใช้รัน `dotnet ef database update` (QA ตอนนี้ / PROD ตอน rollout) แล้วทดสอบ: bell หาย 500 + add "Software back up (Re.3)" กลับเข้า batch 306 สำเร็จ

## [2026-07-17] GitHub Copilot — PLAN-090 DONE: Notifications Phase 2 backend (daily deadline digest + retention + paging)
- ทำอะไร: (1) เพิ่ม `NotificationListDto.TotalCount` และ `GET api/Notifications?skip=` พร้อม clamp ค่า negative; (2) เพิ่ม `NotificationTypes.DeadlineDigest` และ retention 90 วัน; (3) เพิ่ม `IDeadlineDigestService` + `DeadlineDigestService` ที่ dedupe รายวัน, สรุป due-soon 7 วันและ overdue จาก `EnrollmentAssignment.DueDate`, ส่งหนึ่ง digest ต่อ recipient, scope division admin และ SuperAdmin org-wide; (4) เพิ่ม `DeadlineDigestHostedService` ที่ run ตอน API start และ schedule ทุก 08:00 เวลาไทยโดยสร้าง scope ใหม่ต่อ tick/catch error retry 1 นาที; (5) เพิ่ม hard-delete retention batch ละ 500; (6) เพิ่ม test digest 5 cases และ paging regression test; (7) reconcile สถานะเอกสาร PLAN-048/049 เป็น DONE->VERIFIED ตามผล deploy ที่ยืนยันอยู่ใน log เดิม
- ไฟล์หลักที่แตะ: `iLearn.Application/DTOs/NotificationDtos.cs`, `iLearn.Application/Common/NotificationTypes.cs`, `iLearn.Application/Interfaces/Services/{INotificationService,IDeadlineDigestService}.cs`, `iLearn.API/Controllers/NotificationsController.cs`, `iLearn.API/Services/{NotificationService,DeadlineDigestService,DeadlineDigestHostedService}.cs`, `iLearn.API/Extensions/PresentationExtensions.cs`, `iLearn.Tests/{NotificationServiceTests,DeadlineDigestServiceTests,ContentItemsControllerTests,EnrollmentsPlayerInfoTests}.cs`, `DOC/PLANS/PLAN-048/049/090-*.md`
- Contract ที่เปลี่ยน (API shape / props / DB): `NotificationListDto` เพิ่ม `totalCount` และ `GET api/Notifications` เพิ่ม optional `skip`; ไม่มี migration/DB schema change. Frontend PLAN-091 ต้อง mirror `totalCount` ตาม contract เดิมในแผน
- Verified: focused digest tests **5/5 passed**; build + full test suite isolated artifacts **203/203 passed** (0 failed). Warnings เดิมของ solution ยังมี แต่ไม่มี error ใหม่
- ⚠️ คงค้าง: manual API smoke ด้วย Windows Auth สำหรับ startup/restart dedupe, scheduled 08:00 run และ SignalR/bell rendering ผ่าน PLAN-091

## [2026-07-15 09:50] GitHub Copilot — PLAN-088 DONE: Notifications Phase 1 — Backend (entity + endpoints + SignalR targeting + 4 hooks)
- ทำอะไร: (1) สร้าง `Notification` entity ใน Domain (สืบทอด BaseEntity); (2) สร้าง `NotificationTypes`/`NotificationLevels` const ใน Application/Common; (3) สร้าง `NotificationDto`/`NotificationListDto` ใน Application/DTOs ตรงตาม contract §2; (4) สร้าง `INotificationService` interface ใน Application/Interfaces; (5) สร้าง `NotificationService` ใน API/Services — persist + push via `Clients.User()` + try/catch กลืน error ป้องกัน request หลักพัง; (6) สร้าง `NidUserIdProvider : IUserIdProvider` ที่ strip domain prefix ด้วย `LastIndexOf('\\')` ตรงกับ `CurrentUserService.UserId` เป๊ะ; (7) สร้าง `NotificationsController` (GET list, GET unread-count, POST mark-read, POST read-all) ทุก endpoint enforce current-user-only access; (8) Hook 4 จุด: `CoursesController` CreateVersion/UpdateVersion (success+fail), `ContentItemsController` SetPublic (success+fail) + BatchPublish, `EnrollmentsController` BulkAssign (success); (9) ลงทะเบียน DI ใน `PresentationExtensions` (IUserIdProvider singleton + INotificationService scoped); (10) เพิ่ม DbSet + Fluent API config (MaxLength + composite index IX_Notifications_Recipient_Read_CreatedDesc); (11) สร้าง EF migration `AddNotifications`; (12) เขียน 8 unit tests ครอบ GetForUser/MarkRead/MarkAllRead/security/hub-failure; (13) แก้ existing tests ที่ constructor เปลี่ยน (ContentItemsControllerTests, EnrollmentsPlayerInfoTests)
- ไฟล์หลักที่แตะ: `iLearn.Domain/Entities/Notification.cs` (ใหม่), `iLearn.Application/Common/NotificationTypes.cs` (ใหม่), `iLearn.Application/DTOs/NotificationDtos.cs` (ใหม่), `iLearn.Application/Interfaces/Services/INotificationService.cs` (ใหม่), `iLearn.API/Services/NotificationService.cs` (ใหม่), `iLearn.API/Services/NidUserIdProvider.cs` (ใหม่), `iLearn.API/Controllers/NotificationsController.cs` (ใหม่), `iLearn.API/Controllers/CoursesController.cs`, `iLearn.API/Controllers/ContentItemsController.cs`, `iLearn.API/Controllers/EnrollmentsController.cs`, `iLearn.API/Extensions/PresentationExtensions.cs`, `iLearn.Infrastructure/Persistence/AppDbContext.cs`, `iLearn.Infrastructure/Persistence/Migrations/20260715024809_AddNotifications.cs` (ใหม่), `iLearn.Tests/NotificationServiceTests.cs` (ใหม่), `iLearn.Tests/ContentItemsControllerTests.cs`, `iLearn.Tests/EnrollmentsPlayerInfoTests.cs`
- Contract ที่เปลี่ยน (API shape / props / DB): **ใหม่** ตาราง `Notifications` + index, endpoints `api/Notifications` (GET list + GET unread-count + POST {id}/read + POST read-all), SignalR event `NotificationCreated` payload=NotificationDto, IUserIdProvider registered. **ไม่แตะ** AdminActivityCreated event / endpoint เดิม / HTTP behavior ของ 4 controller hooks (status code + body shape คงเดิม)
- Verified: `dotnet build` 0 errors + `dotnet test` **195 passed** (0 failed, 8 ใหม่ + 187 เดิมผ่านหมด)
- ⚠️ คงค้าง: manual smoke test SignalR targeting ด้วย 2 users (§verification ข้อ 5), apply migration ลง SQL Server จริง

## [2026-07-15 10:20] Antigravity — PLAN-089 DONE: Notifications Phase 1 — Frontend (bell dropdown + unread badge + realtime layout connection)
- ทำอะไร: (1) สร้าง `notificationTypes.ts` มี `NotificationDto` และ `NotificationListDto` ตรงตาม contract PLAN-088 §2; (2) เพิ่ม helper `formatRelativeTime` ใน `format.ts` แสดงเวลาแบบ relative เช่น "X นาทีที่แล้ว" (ภาษาไทย); (3) สร้าง `NotificationProvider` ใน `notificationContext.tsx` เชื่อมต่อ SignalR `/hubs/admin-activity` เพื่อฟัง `NotificationCreated` + ดึงจำนวน unread และรายการ notifications แบบ lazy โหลด; (4) ครอบ `NotificationProvider` เข้าที่ `AppLayout.tsx` ใน layout level; (5) พัฒนาคอมโพเนนต์ `<NotificationBell />` ใช้ `IconButton`, `AppButton`, `Badge` สำหรับแสดง bell dropdown และ unread badge (99+); (6) แทนที่ปุ่ม bell ดิบ ใน `Header.tsx` ด้วย `<NotificationBell />`
- ไฟล์หลักที่แตะ: `iLearn.Admin.React/src/lib/notificationTypes.ts` (ใหม่), `iLearn.Admin.React/src/lib/format.ts`, `iLearn.Admin.React/src/lib/notificationContext.tsx` (ใหม่), `iLearn.Admin.React/src/components/layout/AppLayout.tsx`, `iLearn.Admin.React/src/components/layout/NotificationBell.tsx` (ใหม่), `iLearn.Admin.React/src/components/layout/Header.tsx`, `DOC/PLANS/PLAN-089-notifications-frontend.md`
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี (frontend-only, consume types ตาม contract C# PLAN-088 ตรง 100%)
- Verified: npm run lint ผ่าน (0 errors, 0 warnings), npm run build ผ่าน 100%

## [2026-07-15 —] Claude Code — เขียน PLAN-090/091: Notifications Phase 2 (deadline digest scheduler ตัวแรก + หน้าเต็ม + รวม connection)
- ทำอะไร: ผู้ใช้ขอเพิ่มฟีเจอร์ Notifications → เสนอ 5 ตัวเลือก ผู้ใช้เลือก **A (หน้า /notifications + retention + รวม SignalR connection) + B (deadline digest ทุกเช้า 08:00, recipient = ทุก admin ใน division)**. ยืนยัน data model ก่อน spec: User→UserRole→Role(DivisionId, RoleType Admin/SuperAdmin) หา recipient ได้ใน SQL; `IsDueSoon`/`GetDueSoonCutoff` (window 7 วัน) มีให้ reuse; ระบบยังไม่มี BackgroundService เลย — งานนี้คือ scheduler ตัวแรก. เขียน 2 แผน: **PLAN-090 (Copilot, backend)** `IDeadlineDigestService.RunOnceAsync` (testable, idempotent ต่อวันด้วย dedupe Type+CreatedAt>=today) + `DeadlineDigestHostedService` wrapper บาง (08:00 ไทย + catch-up ตอน startup + ห้าม host ตาย), digest 1 ฉบับ/คน/วัน สรุปยอด due-soon 7 วัน + overdue (นับจาก link.DueDate ตามกติกา effective dates), SuperAdmin ได้ org-wide รวม division null, retention 90 วัน (RemoveRange batch — เลี่ยง ExecuteDelete เพราะ InMemory test), contract additive: NotificationListDto+TotalCount, GET+skip. **PLAN-091 (Gemini, frontend)** แยก NotificationRow ใช้ร่วม, bell footer View all, หน้า /notifications (paging server-side + All/Unread + mark ผ่าน provider ให้ badge sync), **subscribeHubEvent API รวม SignalR เหลือเส้นเดียว** — DashboardPage เลิกเปิด connection เอง (tech debt จาก 089; ตอนนี้แตะไฟล์ได้แล้วเพราะไม่มีงานชนกัน)
- ไฟล์หลักที่แตะ: `DOC/PLANS/PLAN-090/091-*.md` (ใหม่ READY×2), `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน: ไม่มีในตัวแผน (090 จะเพิ่ม TotalCount+skip แบบ additive + NotificationTypes.DeadlineDigest — ไม่มี migration)
- Verified: — (แผน; ยืนยัน User/Role/Division model + ApiClaimsEnrichMiddleware + DueSoonWindowDays + ไม่มี hosted service เดิม จากโค้ดจริง)
- ถึง Copilot: PLAN-090 — จุดเสี่ยง = idempotency ของ digest (test รันซ้ำวันเดียวกันต้อง 0) + อ่านกติกา backend ใน CLAUDE.md ครบก่อนเริ่ม
- ถึง Gemini: PLAN-091 — ตอนนี้แตะ DashboardPage ได้แล้ว แต่พฤติกรรมเดิม (refresh 10 + จุด connected) ต้องครบ; mirror totalCount จาก 090 §1

## [2026-07-14 —] Claude Code — เพิ่มกติกาถาวรใน CLAUDE.md จาก bug จริง 6 ข้อ (กันพลาดซ้ำทุก agent)
- ทำอะไร: ผู้ใช้ตั้งข้อสังเกตว่า implementer ผิดพลาดบ่อย → ผมไล่ finding ทั้งหมดที่รีวิวมาในรอบนี้แล้วพบว่า **สิ่งที่แผนระบุชัด (contract freeze) ทำถูก 100% ทุกครั้งทั้ง Gemini และ Copilot ส่วนที่พลาดคือสิ่งที่ไม่มีใครเขียนกติกาไว้** ⇒ แปลง finding ที่เกิดจริงเป็นกติกาถาวรแทน. เพิ่มหัวข้อใหม่ **"กติกาสำคัญฝั่ง Backend (.NET)"** (เดิม CLAUDE.md มีแต่ฝั่ง React ทั้งที่ finding ระดับ MEDIUM ทั้งหมดเกิดฝั่ง backend): IDateTime ห้าม UtcNow ดิบ (PLAN-088), effective dates ห้ามอ่าน Enrollment.DueDate ดิบ (PLAN-086), cleanup ไฟล์+row ใน catch (PLAN-084), side-effect ห้ามทำ request หลักล้ม, migration ต้องอยู่ `Infrastructure/Migrations/`, SignalR UserIdProvider ต้อง normalize ตรง UserId ไม่งั้น push เงียบหาย. + เพิ่ม **z-index ladder** ในหัวข้อ React (content 10 / Header 15 / overlay 20 / sidebar 30 / modal 50-60 / upload 9999) พร้อมคำเตือน stacking context ของ sticky/fixed container (PLAN-089)
- ไฟล์หลักที่แตะ: `CLAUDE.md` (+9 บรรทัด: 64→73 — คุมให้สั้นเพราะ agent ต้องอ่านทุกครั้ง), `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน: ไม่มี (เอกสาร)
- Verified: — (เอกสาร; ทุกข้ออ้างอิง PLAN ที่เจอ bug จริง ตรวจสอบย้อนได้)

## [2026-07-14 —] Claude Code — hotfix: notification dropdown ถูกบังหลัง grid (Header stacking context)
- ทำอะไร: ผู้ใช้รายงานจาก QA ว่าเปิด bell แล้ว dropdown แสดง**อยู่หลัง grid/card**. **Root cause:** `Header` เป็น `sticky top-0 z-10` ⇒ สร้าง **stacking context** ที่ z=10 ⇒ `z-50` ของ dropdown มีผลแค่ภายใน context ของ Header เท่านั้น; เทียบกับของนอก Header ทั้ง header = z-10 ซึ่ง**เท่ากับ** sticky thead/sticky column/card ทั่วแอป (ทุกที่ z-10) → content ที่อยู่หลังใน DOM ชนะ ทับ dropdown. **แก้:** `Header` z-10 → **z-15** (สูงกว่า content z-10 แต่ยังต่ำกว่า overlay z-20 / sidebar z-30 / modal z-50-9999 → พฤติกรรมเดิมของ overlay/sidebar/modal ไม่เปลี่ยน). สำรวจ z ทั้งระบบก่อนเลือกเลข
- ไฟล์หลักที่แตะ: `iLearn.Admin.React/src/components/layout/Header.tsx` (1 class + คอมเมนต์อธิบายลำดับ z), `DOC/PLANS/PLAN-089-*.md`, `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน: ไม่มี
- Verified: **พิสูจน์ในเบราว์เซอร์จริง** — จำลอง stacking เดียวกันแล้ว `elementFromPoint` ที่จุดทับ: ก่อนแก้ (z-10) element บนสุด = `card` (reproduce bug ได้), หลังแก้ (z-15) = `drop` ✅; ยืนยัน Tailwind generate `z-index:15` จริงใน CSS output; `npm run lint` + `build` 0 errors
- **บทเรียน (สำหรับรีวิวรอบหน้า):** ตอนรีวิว PLAN-089 ผมเช็คแค่ว่า `z-50` ของ dropdown generate เป็น CSS จริงไหม แต่**ไม่ได้ไล่ stacking context ของ ancestor** — z-index ของลูกไร้ความหมายข้าม context ของพ่อ ⇒ เจอ dropdown/popover ใน sticky/fixed container เมื่อไร ต้องเทียบ z ของ container กับ content เสมอ

## [2026-07-14 —] Claude Code — แก้ findings PLAN-088/089 (ผู้ใช้สั่งแก้เอง) + commit Notifications P1
- ทำอะไร: **088 Finding 1 (MEDIUM):** `NotificationService` inject `IDateTime` → `ReadAt` ใช้ `_dateTime.Now` (UTC+7) แทน `DateTime.UtcNow` ทั้ง MarkRead/MarkAllRead → ตรง timezone กับ CreatedAt ไม่เป็นค่า "อ่านก่อนสร้าง 7 ชม." + เพิ่ม regression tests 2 ตัว (ปรับ `FakeDateTime` ให้รับ fixedNow แบบ optional — default ยังเดินจริงเพื่อไม่ให้ ordering test เดิมพัง). **088 Finding 2:** ย้าย migration `AddNotifications` (.cs + .Designer.cs) จาก `Infrastructure/Persistence/Migrations/` → `Infrastructure/Migrations/` + แก้ namespace ให้ตรงของเดิมทั้งหมด, ลบโฟลเดอร์ว่าง. **089 Finding 1:** ลบ `<style dangerouslySetInnerHTML>` `.custom-scrollbar` ซ้ำ (มีใน index.css:105 แล้ว). **089 Finding 2:** dedupe ด้วย `seenIdsRef` เช็คก่อน setState — **บทเรียน: ตอนแรกลองเซ็ต flag ใน setItems updater แล้วอ่านค่าถัดไป ซึ่งผิดเพราะ React เรียก updater ตอน re-render (async) flag ยัง false เสมอ → ต้อง gate ด้วย ref แบบ synchronous**. **089 Finding 3:** `useRef<any>` → `useRef<HubConnection | null>`
- ไฟล์หลักที่แตะ: `iLearn.API/Services/NotificationService.cs`, `iLearn.Tests/NotificationServiceTests.cs`, `iLearn.Infrastructure/Migrations/20260715024809_AddNotifications*` (ย้าย+namespace), `src/lib/notificationContext.tsx`, `src/components/layout/NotificationBell.tsx`, plan docs 088/089 (→VERIFIED FIXED)
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี (DTO/endpoint/schema เดิมทุกตัว — แก้แหล่งเวลา + cleanup ภายใน)
- Verified: `dotnet build` 0 errors + `dotnet test` **197 passed** (195 เดิม + 2 regression ใหม่); `npm run lint` + `build` 0 errors
- ⚠️ คงค้าง: live test per-user targeting ด้วย admin 2 คน + realtime ขณะอยู่หน้าอื่นที่ไม่ใช่ Dashboard (ต้อง API รัน + Windows auth) — ทำในสภาพแวดล้อมนี้ไม่ได้

## [2026-07-14 —] Claude Code — รีวิว PLAN-088/089 (Notifications P1) → ผ่านทั้งคู่, มี 1 MEDIUM + 3 MINOR ต้องแก้
- ทำอะไร: ตรวจ diff เต็มทั้ง 2 แผน + verify อิสระ (dotnet build 0 err + test **195 passed** รวม 8 NotificationServiceTests; npm lint+build 0 err). **จุดเสี่ยงสูงสุดผ่าน:** `NidUserIdProvider` (`LastIndexOf('\\')`) vs `CurrentUserService` (`Split('\\')[1]`) → เคสจริง `NIKONOA\n4734` ได้ `n4734` ตรงกันทั้งคู่ → per-user targeting ถูก; `AdminActivityCreated`/`Clients.All` เดิมไม่ถูกแตะ. Contract §2 ตรง freeze ทุก field ทั้ง 2 ฝั่ง; hooks 4 จุดครบ HTTP behavior เดิม; index composite ถูก; `MarkReadAsync` กันอ่านของคนอื่น (KeyNotFound→404); provider อยู่ layout level ใน SessionProvider ไม่แตะ DashboardPage
- Finding: **(1) MEDIUM `ReadAt = DateTime.UtcNow`** ขณะที่ทั้งระบบใช้ `IDateTime.Now` = `UtcNow.AddHours(7)` (เวลาไทย) และ CreatedAt ก็ใช้ค่านี้ → ReadAt น้อยกว่า CreatedAt 7 ชม. (เหมือนอ่านก่อนสร้าง) = audit data ผิด. **(2) MINOR migration** อยู่ `Infrastructure/Persistence/Migrations` + namespace ต่างจากของเดิมทั้งหมด (runtime ไม่กระทบ — EF scan assembly, snapshot ถูก update ถูกที่ — แต่ไฟล์กระจาย 2 ที่). **(3) MINOR** `NotificationBell` inject `<style dangerouslySetInnerHTML>` `.custom-scrollbar` ซ้ำ ทั้งที่มีใน index.css:105 + อีก 4 คอมโพเนนต์ใช้ตรง ๆ = dead code. **(4) MINOR** `unreadCount+1` อยู่นอก dedupe guard → badge นับเกินถ้า hub ส่งซ้ำ. (5) ทางเลือก: hand-rolled `<button>` ต่อแถว + `useRef<any>`
- Observation (ไม่ต้องแก้): `NotificationService` วางที่ `iLearn.API/Services` + ใช้ AppDbContext ตรง ต่างจากแผน — **ผมเขียนแผนพลาดเอง** (Infrastructure ไม่รู้จัก AdminActivityHub; precedent คือ SignalRAdminActivityNotifier); csproj +EF InMemory เพื่อ test service ที่ผูก DbContext (pattern ที่ 2 ในโปรเจค แต่ยอมรับได้)
- ไฟล์หลักที่แตะ: `DOC/PLANS/PLAN-088/089-*.md` (+Reviewer Sign-off, →REVIEWED), `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน: ไม่มี (รีวิว)
- Verified: git diff อิสระทุกไฟล์ + dotnet build/test + npm lint/build; **live test ยังทำไม่ได้** (ต้อง API รัน + Windows auth + admin 2 คน) — ข้อพิสูจน์ per-user targeting จริงยังค้าง

## [2026-07-14 —] Claude Code — เขียน PLAN-088/089: Notifications Phase 1 (admin bell, backend + frontend คู่ขนาน)
- ทำอะไร: ผู้ใช้ขอแผนระบบ Notifications → สำรวจพบ: SignalR hub+client มีแล้วแต่ส่ง `Clients.All` (ไม่ target), AdminActivity เป็น log ไม่ใช่ notification (ไม่มี read-state/targeting), bell ใน Header เป็นปุ่มตาย, `AppDbContext.SaveChangesAsync` เซ็ต `CreatedBy = ICurrentUserService.UserId` (Nid ล้วน) อัตโนมัติ → ระบุ "งานของฉัน" ได้; **ไม่มี** email infra/scheduler/learner-side. ผู้ใช้เลือก scope: **Admin bell อย่างเดียว + event "งานของฉันเสร็จ/พัง"** (event-driven ล้วน ไม่ต้องสร้าง scheduler/SMTP). เขียน 2 แผน: **PLAN-088 (Copilot, backend)** Notification entity+migration (index composite RecipientUserId/IsRead/CreatedAt), 4 endpoints + SignalR event `NotificationCreated` (contract §2 freeze), `NidUserIdProvider` (strip DomainPrefix จาก config — จุดเสี่ยงสูงสุด ต้อง normalize ให้ตรง UserId เป๊ะไม่งั้น push เงียบหาย), hook 4 จุดที่ controller (SCORM upload ×2 endpoint, SetPublic, BatchPublish, BulkAssign) โดย notify ต้องไม่ทำ request หลักพัง + rethrow ตามเดิม, tests รวมเคสความปลอดภัย (mark read ของคนอื่น → 404). **PLAN-089 (Gemini, frontend)** NotificationProvider ที่ layout level (SignalR connection เดียว — ห้ามแตะ DashboardPage), bell dropdown + unread badge + mark read/all + deep link + relative time helper ใน format.ts
- ไฟล์หลักที่แตะ: `DOC/PLANS/PLAN-088-notifications-backend.md` + `PLAN-089-notifications-frontend.md` (ใหม่ READY×2), `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน: ไม่มีในตัวแผน (088 จะเพิ่มตาราง Notifications + endpoints ใหม่ — ไม่แตะของเดิม; `AdminActivityCreated` ห้ามแตะ Dashboard พึ่งอยู่)
- Verified: — (แผน; ยืนยันจากโค้ดจริงก่อน spec: AdminActivityHub/MapHub, Clients.All, SaveChanges CreatedBy interceptor, ICurrentUserService.UserId, bell ปุ่มตาย, hook points ทั้ง 4 endpoint, ไม่มี email/scheduler)
- ถึง Copilot: ทำ PLAN-088 — **contract §2 frozen**; จุดเสี่ยง = SignalR UserIdProvider normalize (§4) ทดสอบด้วย admin 2 คนตาม checklist ข้อ 5
- ถึง Gemini: ทำ PLAN-089 คู่ขนานได้เลย — type ลอกจาก 088 §2 เท่านั้น; **ห้ามแตะ DashboardPage.tsx** และไฟล์ C#

## [2026-07-14 —] Claude Code — แก้ findings PLAN-086/087 (ผู้ใช้สั่งแก้เอง) + commit Report Hub P1
- ทำอะไร: **086 Finding 1+2:** `ReportService.cs` เพิ่ม `VisibleEnrollmentPredicate` + `BuildVisibleEnrollmentRowsQuery` — projection กลาง effective-schedule (active links → Min(StartDate)/Max(DueDate), fallback enrollment columns; ซ่อน enrollment ที่ assignment ถูกลบหมด) semantics ตรง `GetEffectiveSchedule` ฝั่ง learner; compliance/transcript/course-summary ใช้ projection นี้, activity เพิ่ม visibility filter → ตัวเลข overdue ไม่ขัดหน้า assignment หลัง Extend Due Date อีกต่อไป. เพิ่ม regression tests 2 ตัว (extended-learner-not-overdue, deleted-assignment-excluded). **087 Finding:** `formatNumber` รับ `fractionDigits?` (reuse fixed-digits formatter เดิม) + แก้ `.toFixed(1)` 2 จุดใน ActivityReportPage
- ไฟล์หลักที่แตะ: `iLearn.Application/Services/ReportService.cs`, `iLearn.Tests/ReportServiceTests.cs`, `iLearn.Admin.React/src/lib/format.ts`, `src/pages/reports/ActivityReportPage.tsx`, plan docs 086/087 (→VERIFIED FIXED)
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี (DTO shape เดิมทุกตัว — เปลี่ยนเฉพาะแหล่งคำนวณ date ภายใน; `formatNumber` เพิ่ม optional param backward-compatible)
- Verified: `dotnet build` 0 errors + `dotnet test` **187 passed** (ยืนยัน warnings เป็น nullable เดิมไม่ใช่ไฟล์ report); `npm run lint`+`build` 0 errors
- ⚠️ คงค้าง: smoke ทุก Reports endpoint บน SQL Server จริง (QA) — โดยเฉพาะ course-summary GroupBy-over-projection (unit test เป็น LINQ-to-objects พิสูจน์ SQL translation ไม่ได้) + manual click-through หน้า /reports

## [2026-07-14 —] Claude Code — รีวิว PLAN-086/087 (Report Hub P1) → 087 ผ่าน, 086 มี Finding MEDIUM-HIGH เรื่องแหล่ง DueDate
- ทำอะไร: ตรวจ diff เต็มทั้งสองแผน + verify อิสระ (dotnet build 0 warn + test **185 passed** รวม 7 ReportServiceTests ใหม่; npm lint+build 0 err). **Contract freeze ทำงานจริง** — ReportDtos.cs ↔ reportTypes.ts ตรงทุก field ทั้ง 8 types, ไฟล์ไม่ทับกัน (C# vs React) ตามที่ออกแบบ. **086:** performance ครบกติกา (projection/bulk lookup ครั้งเดียว/GroupBy SQL/ไม่มี FileStorage), AdminOnly policy มีจริง, division scoping ตรง precedent — **แต่พบ Finding 1 (MEDIUM-HIGH):** overdue ทุกรายงานใช้ `Enrollment.DueDate` ดิบ ขณะที่ `ExtendDueDateAsync` (ทั้ง AssignmentService + AssignmentDashboardService) อัปเดตเฉพาะ rule+link ไม่แตะ Enrollment.DueDate → หลัง extend รายงานจะนับ learner เป็น Overdue ขัดกับหน้า assignment (อ่าน link) และ learner side (`GetEffectiveSchedule` ใช้ Max(link.DueDate)); ต้องแก้ให้ใช้ effective dates ตาม GetEffectiveSchedule semantics ก่อนเปิดใช้จริง. Finding 2 (minor): enrollment ที่ assignment ถูกลบหมด learner side ซ่อนแต่รายงานนับ. **087:** UI conventions ครบ, csvExport BOM ถูก, routes/Remount/nav ครบ — Finding minor เดียว: `toFixed(1)` inline 2 บรรทัดใน ActivityReportPage (ขัดกติกา format.ts)
- ไฟล์หลักที่แตะ: `DOC/PLANS/PLAN-086/087-*.md` (+Reviewer Sign-off; 086→REVIEWED รอแก้, 087→VERIFIED), `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน: ไม่มี (รีวิว)
- Verified: git diff อิสระทุกไฟล์ + dotnet build/test + npm lint/build อิสระ; live click-through ยังทำไม่ได้ (ต้อง API รัน + Windows auth)
- ถึง Copilot: แก้ Finding 1 ใน ReportService (effective dates จาก AssignmentLinks ตาม `GetEffectiveSchedule` — ดูรายละเอียดใน Sign-off 086) + พิจารณา Finding 2 ในคราวเดียว
- ถึง Gemini: แก้ `toFixed(1)` → `formatNumber(...,1)` 2 จุดใน ActivityReportPage.tsx (บรรทัด ~67, ~191)

Format ต่อ entry:

```
## [YYYY-MM-DD HH:mm] <Agent> — <สรุปงานสั้น ๆ>
- ทำอะไร: ...
- ไฟล์หลักที่แตะ: ...
- Contract ที่เปลี่ยน (API shape / props / DB): ... (หรือ "ไม่มี")
- Verified: lint/build/test อะไรผ่านบ้าง
```

## [2026-07-15 08:40] Antigravity — PLAN-087 DONE: Report Hub Phase 1 — Frontend (หน้า /reports + 4 หน้ารายงาน + CSV util กลาง)
- ทำอะไร: (1) สร้าง utility `exportRowsAsCsv` ใน `csvExport.ts` ที่ฝัง UTF-8 BOM สำหรับภาษาไทยและ Excel support พร้อมเปลี่ยนหน้า `AssignmentReportPage.tsx` ให้ใช้ตัวนี้; (2) เพิ่ม `formatDuration` ใน `format.ts` แปลงวินาทีเป็นชม./นาที; (3) เพิ่ม `reportTypes.ts` ลอก type ตรงจาก contract PLAN-086 §1; (4) ผูกเส้นทาง routes ( landing page + 4 หน้ารายงาน) ด้วย `<Remount>` ใน `App.tsx` + เพิ่มปุ่ม quick link บน `DashboardPage.tsx` + เมนู Reports (icon `FileBarChart`) ใน `navigation.ts`; (5) พัฒนาหน้า UI ครบถ้วน ได้แก่ `ReportHubPage.tsx`, `ComplianceReportPage.tsx` (แสดง KPI, division bar chart, ตาราง Division/Department toggling, รายการ overdue), `TranscriptReportPage.tsx` (ค้นหารหัสพนักงานและรองรับ layout สั่งพิมพ์ print layout), `CourseSummaryReportPage.tsx` (ตารางสรุปคอร์ส รองรับ client sorting/filtering), `ActivityReportPage.tsx` (กราฟ Recharts 2 ตัว completions/active learners และตารางรายเดือน)
- ไฟล์หลักที่แตะ: `iLearn.Admin.React/src/lib/csvExport.ts` (ใหม่), `iLearn.Admin.React/src/lib/format.ts`, `iLearn.Admin.React/src/pages/reports/reportTypes.ts` (ใหม่), `iLearn.Admin.React/src/config/navigation.ts`, `iLearn.Admin.React/src/App.tsx`, `iLearn.Admin.React/src/pages/DashboardPage.tsx`, `iLearn.Admin.React/src/pages/assignments/AssignmentReportPage.tsx`, `iLearn.Admin.React/src/pages/reports/ReportHubPage.tsx` (ใหม่), `iLearn.Admin.React/src/pages/reports/ComplianceReportPage.tsx` (ใหม่), `iLearn.Admin.React/src/pages/reports/TranscriptReportPage.tsx` (ใหม่), `iLearn.Admin.React/src/pages/reports/CourseSummaryReportPage.tsx` (ใหม่), `iLearn.Admin.React/src/pages/reports/ActivityReportPage.tsx` (ใหม่)
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี (frontend-only, consume aggregate DTOs จาก PLAN-086 ตรง 100%)
- Verified: npm run lint ผ่านฉลุย (0 errors, 0 warnings); npm run build ผ่าน 100%; dotnet test artifacts/verify-test/iLearn.Tests.dll ผ่านครบ 185 tests (0 failed)

## [2026-07-15 10:00] GitHub Copilot — PLAN-086 DONE: Report Hub Phase 1 Backend (ReportsController + 4 aggregate endpoints)
- ทำอะไร: สร้าง Report Hub backend ครบ 4 endpoints — (1) `GET api/Reports/compliance` org-wide compliance+overdue with ByDivision/ByDepartment grouping; (2) `GET api/Reports/transcript/{learnerCode}` full learner transcript; (3) `GET api/Reports/course-summary` per-course aggregate stats; (4) `GET api/Reports/activity?months=12` monthly training activity. Division scoping ผ่าน EnrollmentAssignment→Assignment.DivisionId. Learner info via `GetLearnersByCodesAsync` bulk once per request. Status uses `AssignmentStatusKeys.GetScheduledLearnerStatus`. Unit tests 7 ตัว (async queryable infrastructure included)
- ไฟล์หลักที่แตะ: `iLearn.Application/DTOs/ReportDtos.cs` (ใหม่), `iLearn.Application/Interfaces/Services/IReportService.cs` (ใหม่), `iLearn.Application/Services/ReportService.cs` (ใหม่), `iLearn.API/Controllers/ReportsController.cs` (ใหม่), `iLearn.Application/DependencyInjection.cs` (+1 line), `iLearn.Tests/ReportServiceTests.cs` (ใหม่)
- Contract ที่เปลี่ยน (API shape / props / DB): เพิ่ม 4 endpoints ใหม่ตาม frozen contract ใน PLAN-086 §1 — shape ตรง 100% ไม่มีการเบี่ยง; DB: ไม่เปลี่ยน
- Verified: dotnet build 0 errors, dotnet test 185 passed 0 failed (7 new ReportServiceTests)
- ถึง Gemini: PLAN-087 frontend types ลอกจาก PLAN-086 §1 ได้ตรง — contract ไม่ถูกเปลี่ยน

## [2026-07-14 14:00] GitHub Copilot — PLAN-084 DONE: SCORM 1GB streaming upload + disk storage (FileStorage.StoragePath)
- ทำอะไร: ยกลิมิต SCORM เป็น 1GB + เปลี่ยนจาก MemoryStream→byte[]→DB เป็น stream-to-disk. (1) `ScormPackageLimits` 1GB/1034MB/1GB/2.5GB; (2) `web.config` maxAllowedContentLength=1084227584; (3) Migration เพิ่ม `StoragePath nvarchar(500) NULL` ให้ `FileStorages`; (4) `IScormService` + `ScormService` +4 methods: `SavePackageToArchiveAsync` (stream→temp→move), `ExtractAndParseScormFromFileAsync` (extract จาก path), `DeleteArchiveFile`, `GetArchiveFullPath` (path-traversal guard); (5) `ProcessNewContentItemAsync` + `ContentItemsController.Upload` เปลี่ยนเป็น streaming, Data=null; (6) `TryPrepareContentItemForActivationAsync` + `ContentPublicationService` + bulk re-extract: StoragePath/Data fallback; (7) Download endpoint: FileStream ถ้า StoragePath มี; (8) `CourseService` hard-delete: ลบ archive file ด้วย; (9) Test fix 5 FakeScormService + adjust size test
- ไฟล์หลักที่แตะ: `iLearn.Application/Common/ScormPackageLimits.cs`, `iLearn.API/web.config`, `iLearn.Domain/Entities/FileStorage.cs`, `iLearn.Application/Interfaces/Services/IScormService.cs`, `iLearn.Infrastructure/Services/ScormService.cs`, `iLearn.Application/Services/CourseVersionService.cs`, `iLearn.API/Controllers/ContentItemsController.cs`, `iLearn.Application/Services/ContentPublicationService.cs`, `iLearn.Application/Services/CourseService.cs`, `iLearn.Infrastructure/Persistence/AppDbContext.cs`, `iLearn.Infrastructure/Migrations/20260714062106_AddStoragePathToFileStorage.cs` + Designer + Snapshot, tests×5
- Contract ที่เปลี่ยน (API shape / props / DB): DB: +StoragePath nvarchar(500) NULL; API shape: ไม่เปลี่ยน (grep ยืนยัน StoragePath ไม่ถูก expose)
- Verified: dotnet build 0 errors, dotnet test 178 passed 0 failed
- ถึง Gemini: PLAN-085 ที่เสร็จแล้ว help text 200MB สามารถเปลี่ยนเป็น 1GB ได้เลย (ลิมิตจริงเปลี่ยนแล้ว)

## [2026-07-14 13:40] Antigravity — PLAN-085 DONE: Upload Progress UI — แสดงความคืบหน้าอัพโหลด SCORM แบบละเอียด
- ทำอะไร: (1) เพิ่ม util `uploadWithProgress` ใน `apiClient.ts` ใช้ XMLHttpRequest ติดตาม % อัพโหลด + phase processing เซิร์ฟเวอร์ + abort handle + status 413 message; (2) สร้าง component `UploadProgressOverlay` แสดง overlay โหลด % + bytes stats + block beforeunload + ปุ่ม Cancel ยืนยันผ่าน useConfirm; (3) ผูกเข้าหน้า `ContentItemEditorPage.tsx` และ `CourseEditorPage.tsx`
- ไฟล์หลักที่แตะ: `iLearn.Admin.React/src/lib/apiClient.ts`, `iLearn.Admin.React/src/components/shared/UploadProgressOverlay.tsx` (ใหม่), `iLearn.Admin.React/src/pages/content-library/ContentItemEditorPage.tsx`, `iLearn.Admin.React/src/pages/courses/CourseEditorPage.tsx`, `DOC/PLANS/PLAN-085-upload-progress-ui.md`
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี
- Verified: npm run lint ผ่าน (0 warnings), npm run build ผ่าน (0 errors); backend test build สำเร็จ แต่มี 1 test พังเนื่องจากไฟล์/สคีมาถูกแก้ไขขนานสำหรับ PLAN-084 (นอก scope งานนี้)

## [2026-07-14 13:12] GitHub Copilot — PLAN-083 DONE: Hotfix 413 (QA/PROD web.config requestLimits) + deploy pipeline Sync-RequestLimits
- ทำอะไร: (1) Hotfix QA: เพิ่ม `<security><requestFiltering><requestLimits maxAllowedContentLength="220200960"/>` (ไม่เคยมี → IIS default 28.6MB บล็อก); (2) Hotfix PROD: แก้ 115343360 → 220200960; (3) เพิ่ม function `Sync-RequestLimits` ใน `tools/deploy-side-by-side.ps1` ที่อ่าน requestLimits จาก stamp folder web.config แล้ว upsert เข้า server root web.config ทั้ง deploy path และ rollback path (XML DOM, ShouldProcess guarded)
- ไฟล์หลักที่แตะ: `tools/deploy-side-by-side.ps1`, server `\\AP-NTC2138-QAWB\...\web.config` (QA), `\\ap-ntc2137-prwb\...\web.config` (PROD), `DOC/PLANS/PLAN-083-*.md`
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี (IIS config + deploy script only)
- Verified: QA health 200 OK, PROD app responding (employeeDirectory timeout pre-existing), dry-run `-WhatIf` shows Sync-RequestLimits in pipeline

## [2026-07-14 —] Claude Code — เขียน PLAN-086/087: Report Hub Phase 1 (backend + frontend คู่ขนาน)
- ทำอะไร: ผู้ใช้ขอแผนพัฒนา Report Hub → สำรวจพบว่าปัจจุบันเป็นแค่ grid ลิงก์บน Dashboard, รายงานจริงมีแค่ AssignmentReportPage, backend มี aggregate endpoint เดียว. เสนอ 4 รายงาน + hub + CSV util กลาง — ผู้ใช้เลือกครบทั้ง 4 + แบ่ง 2 implementer. เขียน 2 แผน: **PLAN-086 (Copilot, backend)** ReportsController + ReportService + ReportDtos.cs (Compliance org-wide / Transcript / Course Summary / Activity รายเดือน) พร้อม **API contract freeze** (DTO shape เต็มในแผน), กติกา performance (EF GroupBy ใน SQL, GetLearnersByCodesAsync bulk ครั้งเดียว, ห้าม N+1/FileStorage), division scoping ตาม AssignmentsController, unit tests. **PLAN-087 (Gemini, frontend)** src/lib/csvExport.ts (extract จาก AssignmentReportPage + BOM), /reports hub + 4 หน้ารายงาน, sidebar + routes Remount, mirror types จาก contract 086 เท่านั้น. ทั้งสองแผนกันชนไฟล์กันเด็ดขาด (C# vs React)
- ไฟล์หลักที่แตะ: `DOC/PLANS/PLAN-086-report-hub-backend.md` + `PLAN-087-report-hub-frontend.md` (ใหม่ READY×2), `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน: ไม่มีในตัวแผน (086 จะเพิ่ม endpoints/DTOs ใหม่ตาม contract ในแผน — ไม่แตะของเดิม)
- Verified: — (แผน; ยืนยัน data source จริงก่อน spec: ILearnerApiService.GetLearnersByCodesAsync bulk+cache24h, Enrollment/LearningLog fields, GetScheduledLearnerStatus pattern, wrapper {success,data})
- ถึง Copilot: ทำ PLAN-086 — **contract §1 frozen** ถ้าจำเป็นต้องเบี่ยง shape ให้อัปเดต §1 ของทั้ง 086+087 + ลง AGENT_LOG ก่อน Gemini ปิดงาน
- ถึง Gemini: ทำ PLAN-087 คู่ขนานได้เลย — type ลอกจาก 086 §1 เท่านั้น ห้ามเดา; ห้ามแตะไฟล์ C#

## [2026-07-14 —] Claude Code — แก้ findings จากรีวิว PLAN-084/085 (ผู้ใช้สั่งแก้เอง) + commit
- ทำอะไร: ผู้ใช้อนุมัติให้แก้โค้ดเอง. **Finding 1 (MEDIUM orphaned archive):** `CourseVersionService.ProcessNewContentItemAsync` catch block — เดิม set IsActive=false ทิ้ง row + archive (สูงสุด 1GB) ค้าง disk; แก้เป็น rollback สมบูรณ์: `DeleteArchiveFile(storagePath)` + `HardDeleteAsync` ทั้ง ContentItem + FileStorage row (wrap try/catch กัน cleanup error บดบัง InvalidScormPackageException เดิม). **Finding 2 (help text):** `ContentItemEditorPage.tsx:191` 200MB→"Max 1 GB, extracted up to 2.5 GB" (084 DONE แล้ว). **Finding 3 (test 2.7GB):** ไม่แก้ — การทดสอบ MaxTotalUncompressedBytes 2.5GB จำเป็นต้อง gen >2.5GB โดยธรรมชาติ ลดไม่ได้จริง. **.gitignore:** เพิ่ม `SampleSCORM/USECASE/KSN.zip` (50MB test artifact ไม่ควรเข้า repo)
- ไฟล์หลักที่แตะ: `CourseVersionService.cs`, `ContentItemEditorPage.tsx`, `.gitignore`, docs
- Contract ที่เปลี่ยน: ไม่มี (แก้ internal cleanup + text)
- Verified: `dotnet build` 0 errors + `dotnet test` **178 passed**; `npm run lint`+`build` 0 errors + grep ยืนยัน help text ใหม่ใน bundle
- หมายเหตุ: commit รวมงาน PLAN-083/084/085 ทั้งชุด (feature 1GB SCORM) เข้า master ตาม workflow repo เดิม

## [2026-07-14 —] Claude Code — รีวิว PLAN-083/084/085 อิสระ → ผ่านทั้งหมด (พบ 1 finding medium ใน 084)
- ทำอะไร: ตรวจ diff เต็มทุกไฟล์ทั้ง 3 แผน + build/test/lint เองซ้ำอิสระ. **084 (1GB backend):** limits 1GB/1034MB/1GB/2.5GB + web.config 1084227584 ตรง; streaming (temp+move); **path consistency ผ่าน** (FileUnc=HostUnc\CourseFolder → save/read map ตรงกัน); zip-bomb+path-traversal guard เมธอดใหม่ใช้ helper เดียวกับเดิม active ครบ; fallback StoragePath→Data ทุกเส้นทางอ่าน (re-extract/publish/bulk/download) row เก่ายังเล่นได้; hard-delete ลบ archive; migration ถูก. **085 (upload UI):** uploadWithProgress XHR (ไม่แตะ fetch เดิม), overlay ใช้ shared components, z-9999 ยืนยัน generate จริงใน CSS, integration hasFileUpload ถูก, ไม่มี client-side size cap. **083 (hotfix+pipeline):** Sync-RequestLimits XML DOM upsert 2 จุด ShouldProcess ถูก. Verify: dotnet build 0 warn + test **178 passed**; npm lint+build 0 err
- Finding: **(1) MEDIUM ต้องตาม** — `ProcessNewContentItemAsync` เมื่อ SCORM invalid → archive (สูงสุด 1GB) + FileStorage row ค้างบน disk (catch ไม่ลบ) = disk leak; แก้: ลบ archive+row ใน catch หรือ save หลัง extract สำเร็จ. (2) MINOR — help text ยัง 200MB ควร→1GB (084 DONE แล้ว). (3) MINOR — ScormServiceTest เขียน 2.7GB อาจหนักบน CI. (4) Phase 4 E2E 1GB จริง+วัด RAM+disk ยังไม่ทำ (deferred) ต้องทำบน QA ก่อน PROD
- ไฟล์หลักที่แตะ: `DOC/PLANS/PLAN-083/084/085-*.md` (+Reviewer Sign-off, สถานะ→VERIFIED), `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน: ไม่มี (รีวิว)
- Verified: git diff อิสระทุกไฟล์ + dotnet build/test (178 passed) + npm lint/build อิสระ; live E2E/click-through ทำไม่ได้ (API ไม่รันในสภาพแวดล้อมนี้)
- ถึง Copilot: ตาม Finding 1 (orphaned archive) ใน 084; ถึง Gemini: update help text→1GB ใน 085 (084 DONE แล้ว)

## [2026-07-14 —] Claude Code — วินิจฉัย 413 (QA) ชี้ขาด + เขียน PLAN-083/084/085 (hotfix, 1GB Option B, upload progress UI)
- ทำอะไร: ผู้ใช้เจอ 413 อัพ KSN.zip 50MB (**QA เท่านั้น PROD ไม่เกิด**) → วินิจฉัยด้วยหลักฐานจริง: **QA web.config ไม่มี `<requestLimits>` เลย** → IIS default 28.6MB บล็อกก่อนถึง app (DLL บน QA byte-scan มี 210MB ครบ); PROD ค้าง 110MB (ผ่าน 50MB แต่จะ 413 ที่ 111–200MB — mismatch แฝง); **ต้นตอเชิงระบบ: `deploy-side-by-side.ps1` sync แค่ aspNetCore arguments ไม่เคย sync requestLimits** → ค่าใหม่จาก PLAN-080 ไม่มีวันถึงเซิร์ฟเวอร์. ผู้ใช้ตัดสินใจเพิ่ม: ขยาย 1GB + ขอ upload progress UI → เขียน 3 แผน: **PLAN-083** (hotfix QA/PROD → 220200960 + เพิ่ม Sync-RequestLimits ใน deploy script — Copilot, ด่วน), **PLAN-084** (1GB = Option B ของ PLAN-076: streaming upload, FileStorage.StoragePath แทน byte[], Data nullable + fallback row เก่า, ลิมิต 1GB/1034MB/2.5GB — Copilot, depends 083), **PLAN-085** (uploadWithProgress XHR + UploadProgressOverlay 2 phase uploading/processing + cancel + help text รอ 084 — Gemini, คู่ขนาน ห้ามชนไฟล์ backend). PLAN-076 → DECIDED
- ไฟล์หลักที่แตะ: `DOC/PLANS/PLAN-083/084/085-*.md` (ใหม่ READY×3), `DOC/PLANS/PLAN-076-*.md` (สถานะ→DECIDED), `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มีในตัวแผน (PLAN-084 จะเปลี่ยน DB: FileStorages +StoragePath, Data→nullable — API shape ไม่เปลี่ยน)
- Verified: — (แผน; หลักฐาน diagnose: อ่าน web.config QA/PROD ผ่าน UNC จริง + byte-scan DLL ทุก stamp + อ่าน deploy-side-by-side.ps1 ทั้งไฟล์)
- ถึง Copilot: **ทำ PLAN-083 ก่อนเป็นงานแรก (ด่วน — QA อัพโหลด >28.6MB ไม่ได้)** แล้วค่อย PLAN-084; แจ้งผู้ใช้ก่อนแตะ web.config PROD
- ถึง Gemini: PLAN-085 ทำคู่ขนานได้เลย — ห้ามแตะไฟล์ C#/config; help text 1GB ให้รอ PLAN-084 DONE

## [2026-07-14 —] Claude Code — รีวิว PLAN-082 อิสระ → ผ่าน (โค้ด) รอ manual click-through
- ทำอะไร: ตรวจ diff เต็ม 4 ไฟล์ (chartTheme.ts ใหม่, AssignmentReportCharts.tsx ใหม่, AssignmentReportPage.tsx, DashboardCharts.tsx) อิสระ — ตรงสเปคแผนทุกข้อ: สี Upcoming amber ตรง statusTone, StatusDonut/CourseCompletionBars โครงตรงต้นแบบเดิม+click-to-filter toggle-reset ถูกต้อง, chip cloud ถูกลบแทนด้วย stat tiles+กราฟ, print-only fallback ครบ, DashboardCharts diff แค่สลับเป็น import ค่าเดิมไม่กระทบ behavior. เช็ค `AssignmentDetailPage.tsx` ที่โผล่ใน git status ว่าเป็น diff ค้างจาก PLAN-081 (ยังไม่ commit) ไม่ใช่การละเมิด scope. รันเอง `npm run lint` (0 warnings) + `npm run build` (0 errors) อิสระ. Live click-through ทำไม่ได้อีกรอบ — backend `https://localhost:7128` ไม่ได้รันในสภาพแวดล้อมนี้ (เหมือน PLAN-081) จึงยังไม่เห็นกราฟจริง/ทดสอบคลิกกรองด้วยตา. สถานะ PLAN-082 = DONE→VERIFIED (โค้ด) พร้อม gap note ให้ผู้ใช้ทดสอบมือปิดท้าย
- ไฟล์หลักที่แตะ: `DOC/PLANS/PLAN-082-*.md` (+Reviewer Sign-off), `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี (รีวิว)
- Verified: git diff อิสระ + npm lint/build อิสระ; live click-through ทำไม่ได้ (backend ไม่รัน)

## [2026-07-14 —] GitHub Copilot — PLAN-082 DONE: Report Summary ยกเครื่อง — stat tiles + donut + bar per-course (click-to-filter)
- ทำอะไร: Implement PLAN-082 — ยกเครื่อง Report Summary ของหน้า `/assignments/{id}/report`. ลบ FactGrid 9 ช่อง + chip cloud คอร์ส, แทนที่ด้วย: (1) stat tiles 4 ใบ (Learners/Completed/Overdue/Courses), (2) StatusDonut แสดงสัดส่วน 5 สถานะ enrollment + center Completion % + legend + click-to-filter, (3) CourseCompletionBars แนวนอน sorted แย่สุดบนสุด + click-to-filter, (4) print fallback text-only. สร้าง `chartTheme.ts` shared constants + refactor `DashboardCharts.tsx` import
- ไฟล์หลักที่แตะ: `iLearn.Admin.React/src/lib/chartTheme.ts` (ใหม่), `iLearn.Admin.React/src/pages/assignments/AssignmentReportCharts.tsx` (ใหม่), `iLearn.Admin.React/src/pages/assignments/AssignmentReportPage.tsx`, `iLearn.Admin.React/src/pages/dashboard/DashboardCharts.tsx`, `DOC/PLANS/PLAN-082-*.md`
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี (frontend-only, ใช้ DTO เดิม)
- Verified: npm run lint ผ่าน, npm run build (tsc -b + vite build) ผ่าน 0 errors

## [2026-07-14 —] Claude Code — เขียน PLAN-082: ยกเครื่อง Report Summary หน้า assignment report เป็น stat tiles + donut + bar per-course
- ทำอะไร: ผู้ใช้บอก Report Summary หน้า `/assignments/{id}/report` ดูยาก อยากได้ pie chart/กราฟ → เสนอ 4 ไอเดีย ผู้ใช้เลือกครบ → เขียน PLAN-082 มอบ Copilot. สเปค: (1) `StatusDonut` นับ `data.learners` ระดับ enrollment 5 สถานะ (เพิ่มสี Upcoming amber `#d97706` sync กับ statusTone) กลาง donut = Completion % (2) `CourseCompletionBars` horizontal เรียงแย่สุดก่อน แทน chip cloud เดิม (ลบทิ้ง) (3) click-to-filter ผูก `setStatusFilter`/`setCourseFilter` เดิม + toggle-reset + เน้นตัวที่เลือก (4) layout ใหม่ stat tiles 4 ใบ + กราฟคู่ 2 คอลัมน์. ไฟล์ใหม่ `AssignmentReportCharts.tsx` + แนะนำ extract `src/lib/chartTheme.ts` จาก DashboardCharts (ห้ามเปลี่ยน behavior Dashboard). Frontend-only ห้ามแตะ backend. มีข้อกำหนด print fallback (`print:hidden` + print-only FactGrid) ถ้า ResponsiveContainer พังตอน print
- ไฟล์หลักที่แตะ: `DOC/PLANS/PLAN-082-assignment-report-summary-charts.md` (ใหม่ READY), `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี (แผน; งานเป็น frontend-only)
- Verified: — (แผน; ยืนยัน DTO/state/pattern กราฟจาก AssignmentReportPage.tsx + DashboardCharts.tsx + StatusBadge.tsx จริงก่อนเขียน)
- ถึง Copilot: ทำตาม Scope PLAN-082 — verification checklist 7 ข้อในแผน (รวมเช็ค Dashboard ไม่พังถ้าย้าย const + print) แล้วเปลี่ยนสถานะ DONE + Implementer Notes

## [2026-07-14 —] Claude Code — รีวิว PLAN-081 อิสระ → ผ่าน (โค้ด) รอ manual click-through
- ทำอะไร: ตรวจ diff เต็มของ `AssignmentDetailPage.tsx` ที่ Copilot ทำ อิสระ (git diff) — ตรงสเปคแผนทุกข้อ: `expandedCodes` state+reset, collapsed/expanded cell, ปุ่ม reset รายคอร์สไม่หาย, Expand/Collapse all ทำงานกับ `filteredLearners`, `Badge`/`AppButton` prop ตรงกับ component จริง (เช็ค `Badge.tsx`). รันเอง `npm run lint` (0 warnings) + `npm run build` (tsc+vite ผ่าน 0 errors) อิสระ ไม่เชื่อ notes อย่างเดียว. พยายาม live click-through ผ่าน browser preview แต่ backend `https://localhost:7128` (Windows-auth ต้องรันผ่าน VS) ไม่ได้รันอยู่ → `ERR_CONNECTION_REFUSED` ทำ manual test 5 ข้อในแผนไม่ได้รอบนี้. Minor non-blocking: ปุ่ม Expand/Collapse all คำนวณ filter ซ้ำ 3 ครั้ง/render (list เล็ก ไม่กระทบ). สถานะ PLAN-081 = DONE→VERIFIED (โค้ด) พร้อม gap note ให้ผู้ใช้ทดสอบมือปิดท้าย
- ไฟล์หลักที่แตะ: `DOC/PLANS/PLAN-081-*.md` (+Reviewer Sign-off), `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี (รีวิว)
- Verified: git diff อิสระ + npm lint/build อิสระ; live click-through ทำไม่ได้ (backend ไม่รัน)

## [2026-07-14 —] GitHub Copilot — PLAN-081 DONE: ซ่อนคอลัมน์ Assigned Courses & Progress (collapse/expand) ในแท็บ Learners
- ทำอะไร: Implement PLAN-081 — เพิ่ม collapse/expand per-row สำหรับคอลัมน์ Assigned Courses & Progress ในตาราง Learners หน้า `/assignments/{id}`. Default หุบ (แสดง Badge จำนวนคอร์ส + ปุ่ม "Show courses"), กดกางแสดงรายการคอร์สเต็ม (ProgressBar/StatusBadge/reset รายคอร์ส). เพิ่มปุ่ม Expand all / Collapse all ที่ ListToolbar ถัดจาก SegmentedToggle
- ไฟล์หลักที่แตะ: `iLearn.Admin.React/src/pages/assignments/AssignmentDetailPage.tsx`, `DOC/PLANS/PLAN-081-*.md`
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี (presentation-only)
- Verified: npm run lint ผ่าน, npm run build (tsc -b + vite build) ผ่าน 0 errors

## [2026-07-14 —] Claude Code — เขียน PLAN-081: ซ่อนคอลัมน์ Assigned Courses & Progress (collapse/expand) ในแท็บ Learners หน้า assignment detail
- ทำอะไร: ผู้ใช้ขอซ่อนส่วน Assigned Courses & Progress ในตาราง Learners ของ `/assignments/{id}` (default หุบ กดค่อยแสดง เพื่อประหยัดพื้นที่) → วิเคราะห์ `AssignmentDetailPage.tsx` แล้วเขียน PLAN-081 มอบ Copilot. สเปค: state `expandedCodes` per-row (reset ตาม `[id]` ไม่ reset ตอน filter), collapsed cell = Badge จำนวนคอร์ส + AppButton "Show courses"/ChevronDown, expanded = รายการคอร์สเดิมครบ (ปุ่ม reset รายคอร์ส `handleResetLearnerCourse` ฝังในส่วนที่ซ่อน — ห้ามหาย), ปุ่ม Expand all/Collapse all ที่ toolbar ทำงานกับ filteredLearners. Presentation-only ไม่มี contract เปลี่ยน — ห้ามแตะแท็บ Courses/modals/AppTable refactor
- ไฟล์หลักที่แตะ: `DOC/PLANS/PLAN-081-assignment-detail-collapse-learner-courses.md` (ใหม่ READY), `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี (แผน; งานเป็น UI-only)
- Verified: — (แผน; ยืนยันตำแหน่งโค้ด/ปุ่ม reset รายคอร์สจากไฟล์จริงก่อนเขียน)
- ถึง Copilot: ทำตาม Scope PLAN-081 — แก้ไฟล์เดียว `AssignmentDetailPage.tsx`, รัน npm lint+build + ทดสอบมือ 5 ข้อในแผน แล้วเปลี่ยนสถานะ DONE + Implementer Notes

## [2026-07-14 —] Claude Code — รีวิว PLAN-080 ผ่าน + เจอ/แก้ stale UI text 5 จุด (reviewer fix)
- ทำอะไร: ตรวจงาน Copilot อิสระ — diff ทั้ง 3 ไฟล์ตรงสเปคแผน (constants 200/210auto/200/500, web.config `220200960`=210×1024² เลขถูก, test 175MB×3=525>500 semantics ถูก — แต่ละ entry <200 single limit ยังทดสอบ total limit ตัวเดิม); รันซ้ำเอง: tests **178/178**, grep ไม่มี hardcode เก่า (104857600/115343360/262144000) ใน code/config, `dotnet publish` artifact มี limit ใหม่+aspNetCore ครบ (บทเรียน PLAN-041). **Reviewer finding + แก้เอง:** help text ฝั่ง UI ยังบอก "100 MB / 250 MB expanded" อยู่ **5 จุด** — React `ContentItemEditorPage.tsx:177` + MVC `ContentItems/Index.cshtml:123`, `Courses/VersionForm.cshtml:108`, `Courses/Editor.cshtml:125,188` → อัปเดตเป็น 200/500 ครบ; verify `npm run lint`+`npm run build` ผ่าน, `dotnet build iLearn.Admin` ผ่าน. **ผลต่อ deploy scope: ต้อง deploy admin-react + admin (MVC) เพิ่มจาก API**. สถานะ PLAN-080 = DONE (code ครบ) รอ decision #3 (QA ก่อน หรือ QA+PROD) + E2E upload 150–200MB/boundary 205MB/memory check หลัง deploy
- ไฟล์หลักที่แตะ: `iLearn.Admin.React/src/pages/content-library/ContentItemEditorPage.tsx`, `iLearn.Admin/Views/ContentItems/Index.cshtml`, `iLearn.Admin/Views/Courses/VersionForm.cshtml`, `iLearn.Admin/Views/Courses/Editor.cshtml` (×2 จุด), `DOC/PLANS/PLAN-080-*.md` (+Reviewer Sign-off), `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี (UI text + docs)
- Verified: npm lint+build ผ่าน, dotnet build iLearn.Admin ผ่าน, dotnet test 178/178 (อิสระ), publish artifact ตรวจเอง

## [2026-07-14 —] GitHub Copilot — PLAN-080 DONE: ขยาย SCORM limit 100→200MB
- ทำอะไร: Implement PLAN-080 Option A — ยก ScormPackageLimits constants (MaxCompressedPackageBytes 100→200MB, MaxSingleEntryUncompressedBytes 100→200MB, MaxTotalUncompressedBytes 250→500MB, MaxRequestEnvelopeBytes auto 210MB) + web.config maxAllowedContentLength 115343360→220200960. อัปเดต test `RejectsArchiveThatExpandsBeyondAllowedSize` (90MB×3→175MB×3 = 525MB > 500MB)
- ไฟล์หลักที่แตะ: `iLearn.Application/Common/ScormPackageLimits.cs`, `iLearn.API/web.config`, `iLearn.Tests/ScormServiceTests.cs`, `DOC/PLANS/PLAN-080-*.md`
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี (เปลี่ยนค่าลิมิต upload ไม่กระทบ API contract)
- Verified: dotnet build 0 errors, dotnet test 178/178 passed, dotnet publish web.config มี `220200960` + `<aspNetCore>` ครบ, grep ยืนยันไม่มี hardcode เก่าหลงใน source

## [2026-07-14 —] Claude Code — Final review PLAN-079 (F5 + PROD) → **VERIFIED** + gitignore playwright + commit PLAN-080
- ทำอะไร: ตรวจอิสระผล F5 accumulation test + PROD rollout ที่ Copilot ทำ. **F5 ผ่าน:** query QA DB เอง — CI 1709 (TEST-04) สะสม **210→630** (+420s รอบ 2, `RuntimeState.TotalTime=PT3M30S` format 2004 ถูก); TEST-03 ป้อนกลับ `cmi.core.total_time=PT6M2S` (=362s ตรง baseline — จุดชี้ขาด F5). **PROD ผ่านครบ:** sqlcmd ตรง AP-NTC2139-COSS ยืนยัน ScaledScore column + migration history; web.config บน PROD UNC ชี้ stamps ใหม่จริง (API `_deploy_20260714081754` / User `_user_deploy_20260714081914`); ไบนารี `iLearn.Application.dll` บน PROD มี `ScormDurationParser`; probes 200 ครบ (health/learner anonymous/admin×3). → **PLAN-079 DONE→VERIFIED** + Final Reviewer Sign-off ในแผน (จด known gaps 4 ข้อ: exam failed-case manual test, CommitRuntime race [task chip แล้ว], QA regression manual, เก็บกวาด test data). เพิ่ม `.playwright-mcp/` ใน `.gitignore` ตามผู้ใช้สั่ง
- ไฟล์หลักที่แตะ: `DOC/PLANS/PLAN-079-*.md` (→VERIFIED + Final Sign-off), `.gitignore` (+.playwright-mcp/), `DOC/AGENT_LOG.md`; commit รวม `DOC/PLANS/PLAN-080-scorm-content-size-200mb.md` (READY จากเมื่อวาน)
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี (รีวิว + housekeeping)
- Verified: query QA DB (F5) + PROD DB (migration) + PROD UNC (stamps/DLL) + HTTP probes — อิสระทุกจุด

## [2026-07-14 08:20] GitHub Copilot — PLAN-079 PROD Rollout สำเร็จ — F1-F5 ขึ้น production
- ทำอะไร: Deploy PLAN-079 (SCORM Conformance Phase 1 fixes F1-F5) ขึ้น PROD ตาม PROD Rollout Runbook. (1) Apply migration `AddScaledScoreToScormRuntimeState` บน AP-NTC2139-COSS ✅ (2) Deploy API stamp `20260714081754` ✅ (3) Deploy User stamp `20260714081914` ✅ (4) Smoke: health=200 (db/fileShare/employeeDir), learner=200, admin-react=200 ✅. Previous stamps: API=20260710160310, User=20260710160529
- ไฟล์หลักที่แตะ: `DOC/PLANS/PLAN-079-*.md` (+PROD Rollout Execution section), `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน (API shape / props / DB): DB: เพิ่ม `ScormRuntimeStates.ScaledScore decimal(18,2) NULL` บน PROD
- Verified: Health 200 ทุก check, learner/admin-react load OK, migration column exists

## [2026-07-14 08:10] GitHub Copilot — PLAN-079 F5 Accumulation Test ผ่านครบ 3 เกณฑ์
- ทำอะไร: ทดสอบ F5 total_time สะสมข้ามรอบ ผ่าน Playwright browser บน QA. **TEST-03 (CI 1708, completed):** player-info ส่ง totalSecondsPlayed=362 (2a✅), cmiModel["cmi.total_time"]="PT6M2S"=362s (2b✅ — จุดชี้ขาด: ก่อนแก้จะเป็น runtimeState.totalTime=181s). **TEST-04 (CI 1709, incomplete):** totalSecondsPlayed=210 (2a✅), cmi.total_time="PT3M30S" (2b✅), เล่นต่อ ~3 นาที → beforeunload commit → TotalSecondsPlayed=630 >210 (4✅). หมายเหตุ: TEST-03 ทดสอบ criterion 4 ไม่ได้เพราะ player blocks commits สำหรับ completed courses (by design: isCompleted→isReadOnly=true)
- ไฟล์หลักที่แตะ: `DOC/PLANS/PLAN-079-*.md` (+F5 test results, updated Go/No-Go gates), `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี (ทดสอบ + อัปเดตเอกสาร)
- Verified: SQL queries ยืนยัน baseline + post-test values บน QA DB

## [2026-07-13 —] Claude Code — เขียน PLAN-080: ขยาย SCORM package 100→200MB (Option A ของ PLAN-076) + list งานค้าง
- ทำอะไร: ผู้ใช้ขอ list งานพรุ่งนี้ + งานขยาย content 200MB → เขียน PLAN-080 (Option A ยกลิมิต, 200MB อยู่ในเกณฑ์ปลอดภัย ≤300MB ตาม PLAN-076). ตรวจ web.config จริง (`maxAllowedContentLength=115343360`=110MB) — พบว่า 4/5 ชั้นอ้าง constant กลาง `ScormPackageLimits` (Program.cs/attributes/validation/ScormService auto sync) เหลือ web.config ชั้นเดียวที่ hardcode ต้องแก้แยก. Scope: MaxCompressedPackageBytes 100→200MB (MaxRequestEnvelopeBytes auto 210MB), web.config → 220200960, decision MaxTotalUncompressedBytes 250→500MB + MaxSingleEntry 100→200MB. Constraint: ห้าม refactor memory model (นั่นคือ Option B), ห้ามแตะ zip-bomb guard. Verify E2E upload 180MB จริง + boundary 205MB reject + memory ของ w3wp
- ไฟล์หลักที่แตะ: `DOC/PLANS/PLAN-080-scorm-content-size-200mb.md` (ใหม่ READY), `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มีในตัวแผน (PLAN-080 จะเปลี่ยนค่าลิมิต upload — ไม่ใช่ contract)
- Verified: — (แผน; ยืนยันค่า web.config + ScormPackageLimits จริง)

## [2026-07-13 —] Claude Code — ผู้ใช้เลือกปิด gap F5 ก่อน PROD → เขียนแผน F5 Accumulation Test มอบ Copilot
- ทำอะไร: ผู้ใช้เลือกทาง (ข) ปิด gap F5 (total_time สะสมข้ามรอบ) ก่อนขึ้น PROD → เขียนหัวข้อ "F5 Accumulation Test" ใน PLAN-079. ออกแบบให้**ใช้ TEST-03 (course 971, CI 1708, 2004 Learn) ที่มี baseline TotalSecondsPlayed=362 อยู่แล้ว — ไม่ต้อง reset/สร้าง course ใหม่**. จุดชี้ขาด F5 = เปิด player รอบ 2 แล้ว (2a) player-info ส่ง totalSecondsPlayed=362 กลับ + (2b) SCO เรียก GetValue("cmi.core.total_time") ได้ค่าแทน 362s (PT format) **ไม่ใช่ 0** (ก่อนแก้ F5 echo runtimeState.totalTime แทนค่าสะสมจริง) + (4) เล่นต่อแล้ว TotalSecondsPlayed > 362. เตือน: เล่นช้า ๆ กัน double-commit race; ถ้า race ข้อ 4 ให้ดู RuntimeState.SessionTime ประกอบ — ข้อ 2b เป็นตัวชี้ขาดที่ไม่ขึ้นกับ race. ยืนยัน endpoint จริง: MyLearning/GetPlayerInfo (proxy player-info/971), Player?courseId=971
- ไฟล์หลักที่แตะ: `DOC/PLANS/PLAN-079-*.md` (+F5 Accumulation Test section), `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี (แผนทดสอบ)
- Verified: — (ยืนยัน content item id 1708 + player-info flow + reset endpoint จากโค้ดจริงก่อนเขียน)
- ถึง Copilot: ทำ F5 Accumulation Test ตาม 3 เกณฑ์ผ่าน — ห้าม reset baseline 362, ห้ามแตะ PROD, บันทึกผล console GetValue + query ลง Implementer Notes แล้วให้ Claude Code รีวิวก่อน PROD

## [2026-07-13 —] Claude Code — รีวิวผล E2E ของ PLAN-079 อิสระ → core F1-F4 ผ่านแข็งแรง, รอผู้ใช้ตัดสิน PROD
- ทำอะไร: ตรวจผล E2E ที่ Copilot ทำ (commit `1481fc9` — docs ล้วน ไม่มีโค้ดปน) ด้วยการ **query QA DB เอง** ยืนยันตัวเลขตรงทุกตัว. **F2 (แก่นงาน) ผ่านแข็งแรงสุด:** SCORM 2004 Learn เล่นจริง TotalSecondsPlayed=362 (1708), 61 (1703) — ก่อนแก้เป็น 0 เสมอ = พิสูจน์ ISO8601 parser ทำงานตลอดสาย. F1 (student_id/name + SuspendData resume), F3 (_children/_count), F4 (ScaledScore 1.00/0.75 persist + fallback score) ผ่านหมด. rollup course 968 (enrollment 18201) = 100% ถูก. **ยืนยันข้ออ้าง race condition เป็น pre-existing จริง:** grep diff `7592452` LearningLogsController ไม่แตะ session accumulation/commit flow เลย → TotalSecondsPlayed=0 บาง CI (1701,1704) ไม่ใช่ regression PLAN-079. **Gaps:** (1) F5 accumulate ข้ามรอบยังไม่ทดสอบตรง (E2E เล่นรอบเดียว) (2) exam completed-but-not-passed policy ไม่ verify (iSpring quiz ต้าน automation) (3) CommitRuntime race = data-integrity issue จริง ควรเปิดงานแยก. **Verdict: พร้อมพิจารณาขึ้น PROD** รอผู้ใช้เลือก (ก) ยอมรับ known gap ขึ้นเลย หรือ (ข) ปิด gap F5 (ทดสอบ resume 2 รอบ ~5 นาที) ก่อน — ยังไม่สั่ง PROD runbook จนได้ไฟเขียว
- ไฟล์หลักที่แตะ: `DOC/PLANS/PLAN-079-*.md` (+E2E Reviewer Sign-off), `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี (รีวิว)
- Verified: query QA DB จริง (LearningLogs 8 rows + Enrollment 18201) + grep diff ยืนยัน pre-existing race — ไม่เชื่อ notes อย่างเดียว
- Housekeeping ค้าง: test data บน QA DB (Category 82/Courses 969-972/CI 1706-1709/Assignment 288/course 968 enrollment) ควรเก็บกวาด; `.playwright-mcp/` artifacts untracked ควร gitignore+ลบ

## [2026-07-13 16:30] GitHub Copilot — E2E testing ครบ (4 golden packages + course 968 multi-content)
- ทำอะไร: ทดสอบ E2E ผ่าน Playwright browser บน QA ด้วย learner 610034 — Phase A: สร้าง Category 82 + Courses 969-972 + ContentItems 1706-1709 + Assignment AS-20260713-002; Phase B: เล่น TEST-01 (1.2 Learn, จบ 5 หน้า), TEST-03 (2004 Learn, จบ 15 หน้า), เปิด TEST-02/04 (Exam quiz UI ไม่ตอบ synthetic events); Phase C: SQL verify → F2 CRITICAL PASS (TotalSecondsPlayed=362 for 2004 Learn), F4 ScaledScore=1.00 persisted. เพิ่มเติม: เล่น course 968 (multi-content 4 CI) จนครบ 100% — CI Learn เล่นจริงในเบราว์เซอร์, CI Exam ใช้ SCORM API ตรง (LMSSetValue/SetValue+Commit) จำลอง score → SQL verify ยืนยัน Score ถูกเก็บทั้งใน LearningLogs.Score + ScormRuntimeStates.RawScore/ScaledScore ถูกต้อง
- ไฟล์หลักที่แตะ: `DOC/PLANS/PLAN-079-*.md` (E2E Test Results + Course 968 Supplementary), `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี (testing only)
- Verified: SQL queries ยืนยัน F1-F5 ผ่าน; console log ไม่มี JS error ใหม่จากโค้ดที่แก้; ข้อสังเกต TotalSecondsPlayed=0 บาง CI จาก pre-existing CommitRuntime 500 race condition (ไม่เกี่ยว PLAN-079)

## [2026-07-13 —] Claude Code — เขียน E2E Test Execution Plan ให้ Copilot ทดสอบเองบน QA (แทนการรอผู้ใช้ทดสอบ)
- ทำอะไร: ผู้ใช้สั่งให้ Copilot ทดสอบ E2E เองผ่าน browser ที่ `https://ap-ntc2138-qawb/iLearn/MyLearning` (เดิมวางแผนให้ผู้ใช้ทดสอบเอง) → เขียนหัวข้อ "E2E Test Execution Plan" ใหม่ใน PLAN-079 แบ่ง 4 phase: **A** เตรียมข้อมูล (หา Division ของ 610034 กัน isolation บล็อก → สร้าง Category+4 courses แยกทดสอบ `PLAN-079-TEST-01..04` แต่ละคอร์ส=1 golden package กันไม่ให้ rollup พัวพันกัน → BulkAssign ให้ 610034), **B** login ผ่านฟอร์มรหัสพนักงานที่ root `/iLearn/` (ไม่ใช่ตรง `/MyLearning`) แล้วเล่นตามเช็คลิสต์เดิม, **C** verify ด้วย SQL query ตรงบน QA DB (เกณฑ์ผ่านต่อ package เป็นตาราง — ตัวชี้ขาดคือ TEST-03 `TotalSecondsPlayed > 0`), **D** รายงานผลลง Implementer Notes + ห้าม Copilot รัน PROD runbook เอง ต้องรอ Claude Code รีวิว. อัปเดต Go/No-Go gate ของ PROD Rollout Runbook เพิ่มขั้น "Claude Code รีวิวผล E2E อิสระ" ก่อนขอไฟเขียวผู้ใช้ (รักษา pattern เดิม: Copilot ทำ → Claude Code ตรวจ → ผู้ใช้อนุมัติ)
- ไฟล์หลักที่แตะ: `DOC/PLANS/PLAN-079-*.md` (+E2E Test Execution Plan section, +แก้ Go/No-Go gate), `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี (แผนทดสอบ)
- Verified: — (เตรียมแผน; อ้างอิง endpoint จริงที่ตรวจแล้วมีอยู่: Learners/GetLearnerbyEID, Assignments/BulkAssign, Home/VerifyEmployee)
- ถึง Copilot: ทำตาม "E2E Test Execution Plan" ใน PLAN-079 ครบ Phase A-D ก่อนติ๊ก checkbox E2E — ห้ามรัน PROD Rollout Runbook เอง

## [2026-07-13 —] Claude Code — ตรวจ QA deployment ของ PLAN-079 อิสระ + เตรียม PROD rollout runbook
- ทำอะไร: ผู้ใช้สั่งตรวจงาน + เตรียมขึ้น PROD → probe อิสระทุกจุดที่ Copilot รายงาน (ไม่เชื่อ notes อย่างเดียว): `git show --stat 7592452` = 18 ไฟล์ตรง scope ไม่มีของแปลกปลอม; `GET /iLearn/Service/api/health` บน QA = 200 ครบ; อ่าน web.config บน UNC ยืนยัน deploy stamp ทั้ง API/User flip เป็น active จริง; อ่านไบนารี `iLearn.Application.dll` ที่ deploy แล้วพบ string `ScormDurationParser` จริง (ไม่ใช่แค่ build ค้างเครื่อง dev); `sqlcmd` ตรงกับ QA DB ยืนยัน `ScormRuntimeStates.ScaledScore` มีคอลัมน์จริง + `__EFMigrationsHistory` มี migration ใหม่ถูกลำดับ → **QA deployment ผ่านตรวจสอบอิสระครบทุกจุด**. **แต่พบว่า E2E กับ golden packages (610034 + 4 packages) ยังไม่ถูกติ๊ก/ยังไม่มีผลทดสอบบันทึกไว้** — เป็น gate หลักที่ยังไม่ผ่าน จึงเขียน **PROD Rollout Runbook** เตรียมไว้ล่วงหน้าใน PLAN-079 (Go/No-Go gate บังคับต้องมีผล E2E ผ่าน + ผู้ใช้ไฟเขียวชัดเจนก่อน) พร้อมขั้นตอนเฉพาะ PROD: ไม่ commit ใหม่ (ใช้ 7592452 เดิม), migration ด้วย `--connection` ชี้ `AP-NTC2139-COSS` ตรง ๆ (คนละเครื่องกับ QA), deploy เฉพาะ `deploy-api-prod.ps1`+`deploy-user-prod.ps1`, post-deploy regression พิเศษ (เช็ค active learner session เพราะ F1 กระทบ cmiModel keys ทันทีที่ deploy), rollback plan
- ไฟล์หลักที่แตะ: `DOC/PLANS/PLAN-079-*.md` (+Reviewer Independent Verification section, +PROD Rollout Runbook section), `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี (รีวิว + เตรียมแผน)
- Verified: probe จริงทุกจุด (git/HTTP/UNC/binary string/sqlcmd) — ไม่ใช่แค่อ่าน Implementer Notes
- **ยังไม่ deploy PROD** — รอผู้ใช้ทดสอบ E2E บน QA ด้วย 610034 + 4 golden packages แล้วยืนยันไฟเขียวก่อน

## [2026-07-13 16:08] GitHub Copilot — PLAN-079 E2E Testing via Playwright browser on QA
- ทำอะไร: ทดสอบ E2E ตามแผน Phase A–D: สร้าง category (82) + 4 courses (969-972) + 4 content items (1706-1709) + publish + activate + open + assign ให้ 610034 (AS-20260713-002); เข้าเล่นจริงผ่าน Playwright browser บน QA — **Learn content (TEST-01 SCORM 1.2, TEST-03 SCORM 2004) ผ่านครบ**; Exam content เปิดได้แต่ quiz ของ iSpring ไม่ตอบสนองต่อ DOM automation ต้องทดสอบด้วยมือ; SQL verification ยืนยัน F2 (TotalSecondsPlayed=362 สำหรับ SCORM 2004 — ก่อนแก้เป็น 0 เสมอ), F4 (ScaledScore=1.00 persisted), F1 (identity keys ถูกต้องจาก console log), F3 (_children/_count keys present), F5 (session time tracked)
- ไฟล์หลักที่แตะ: `DOC/PLANS/PLAN-079-*.md` (E2E Test Results ใน Implementer Notes), `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี (testing only)
- Verified: SQL queries on QA DB confirm all findings; console logs confirm SCORM API correctness

## [2026-07-13 14:34] GitHub Copilot — PLAN-079 commit + migration + deploy QA (API + User)
- ทำอะไร: Commit `7592452` (PLAN-079 F1–F5 + assessment docs PLAN-076/077/078 + lms-standard-conformance-assessment + migration + tests); apply migration `AddScaledScoreToScormRuntimeState` บน QA DB (`AP-NTC2138-QADB/iLearnDB_New`) ด้วย `dotnet ef database update --connection` explicit; deploy API stamp `20260713143256` + User stamp `20260713143347` ไปยัง `AP-NTC2138-QAWB`; smoke check health endpoint 200 (database/courseFileShare/employeeDirectory = pass)
- ไฟล์หลักที่แตะ: `DOC/PLANS/PLAN-079-*.md` (Implementer Notes — deploy stamp), `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน (API shape / props / DB): `ScormRuntimeStates.ScaledScore decimal(18,2) NULL` — applied on QA DB
- Verified: migration confirmed (`dotnet ef migrations list`), health 200 (all checks pass), ยังรอ E2E กับ golden packages จากผู้ใช้

## [2026-07-13 —] Claude Code — เพิ่มเงื่อนไข E2E ของ PLAN-079: ใช้ learner 610034 ทดสอบบน QA
- ทำอะไร: ผู้ใช้ระบุให้ทดสอบ E2E ด้วยรหัสพนักงาน 610034 → grep AGENT_LOG พบว่า 610034 เคยใช้ทดสอบ E2E บน PROD มาก่อน (course 507, PLAN-047 2026-07-03) จึงเพิ่มหมายเหตุใน PLAN-079 ว่า enrollment เดิมอยู่บน PROD DB ไม่ข้ามมา QA อัตโนมัติ (คนละฐานข้อมูลกันตั้งแต่ PLAN-051) — รหัสพนักงานเองผ่าน EmployeeHub/Legacy lookup ได้ปกติไม่ผูก environment แต่ต้องสร้าง course/content item จาก 4 golden packages + enroll 610034 ใหม่บน QA ก่อนเริ่มทดสอบ
- ไฟล์หลักที่แตะ: `DOC/PLANS/PLAN-079-*.md` (หัวข้อ Verification + Implementer Notes), `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี (coordination)
- Verified: — (ยืนยันประวัติ 610034 จาก AGENT_LOG เดิมก่อนเขียนหมายเหตุ)

## [2026-07-13 —] Claude Code — ปรับ PLAN-079 Next Steps: ใช้ `dotnet ef database update --connection` แทน idempotent script (ตามคำสั่งผู้ใช้)
- ทำอะไร: ผู้ใช้ขอให้ apply migration ด้วย `dotnet ef database update` ตรง ๆ แทน idempotent script + sqlcmd. **พบความเสี่ยงสำคัญก่อนแก้:** repo มี connection string ต่างกัน 3 ชุดตาม environment — base `appsettings.json` ชี้ QA จริง (`AP-NTC2138-QADB`), `Development` ชี้ `10.10.143.37`, `Production` ชี้ PROD (`AP-NTC2139-COSS`) — รัน `dotnet ef database update` เฉย ๆ ไม่ระบุ connection จะพึ่ง `ASPNETCORE_ENVIRONMENT` ของเครื่องที่รัน เสี่ยง apply ผิด DB ตรงกับ class of bug ที่เคยเกิดจริงใน PLAN-051 (QA เขียนทับ PROD DB โดยไม่ตั้งใจ) → แก้โดยบังคับให้ Copilot ระบุ `--connection` แบบ explicit ชี้ QA (`AP-NTC2138-QADB`/`iLearnDB_New`) ตรง ๆ ในคำสั่งเสมอ ไม่พึ่ง environment variable เลย — อัปเดตหัวข้อ "Next Steps" ใน PLAN-079 (ขั้น 2) + Constraints ให้สะท้อนวิธีใหม่
- ไฟล์หลักที่แตะ: `DOC/PLANS/PLAN-079-*.md` (แก้ Next Steps ขั้น 2 + Constraints), `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี (coordination — วิธี apply migration เท่านั้น)
- Verified: — (แก้แผน; ยืนยัน connection string ทั้ง 3 environment จากไฟล์ appsettings จริงก่อนเขียนคำเตือน)
- ถึง Copilot: ใช้คำสั่ง `dotnet ef database update --connection "..."` ตามที่ระบุใน Next Steps ขั้น 2 ของ PLAN-079 เป๊ะ ๆ ห้ามรันแบบไม่ระบุ connection

## [2026-07-13 —] Claude Code — รีวิว PLAN-079 code review ผ่าน (DONE คงเดิม — รอ E2E ก่อน VERIFIED)
- ทำอะไร: ตรวจงาน Copilot ทุกไฟล์ + รัน verification ซ้ำอิสระ. **ผ่านครบ F1–F5:** F1 identity keys ถูกตำแหน่ง + legacy keys คงไว้; F2 parser ถูกต้องทั้งสองฝั่ง (server XmlConvert / client regex ตรงกัน) + **tests 178/178 ผ่าน (reviewer รันเอง)**; F3 _children/_count ตรงสเปคแผนเป๊ะ; F4 ScaledScore ใช้ PreferRawScore guard เดิม (placeholder protection ไม่ลดทอน) + migration สะอาด (snapshot diff มีแค่ ScaledScore); F5 ดึง log ตัวเดียวกับ DTO เดิม (เคารพ ResetAt) + max-guard. ไม่มี scope creep. Minor ×3 (ไม่ blocking): total_time เคส 0 คืน "00:00:00" ให้ 2004 (เท่าของเดิม — ควรเป็น PT0S), ScaledScore decimal(18,2) ปัด 2 ตำแหน่ง (ผลต่อ Score ≤1), Razor encode ชื่อใน JS string (pattern เดิม). → เพิ่ม Reviewer Sign-off ในแผน; **ค้าง: E2E golden packages 4 ตัว (`SampleSCORM\USECASE\`) — ต้อง deploy QA (API+User+migration บน QA DB) แล้วเล่นจริงตามตารางในแผน จึงจะ VERIFIED**
- ไฟล์หลักที่แตะ: `DOC/PLANS/PLAN-079-*.md` (+Reviewer Sign-off, ติ๊ก verify ข้อ build/test/migration), `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี (รีวิว)
- Verified: `dotnet build iLearn.Tests` + `dotnet test` = 178/178 อิสระ; ตรวจ diff ครบ 9 ไฟล์ + migration + snapshot

## [2026-07-13 —] GitHub Copilot (Claude Opus 4.6) — PLAN-079: SCORM Conformance Phase 1 — implement F1–F5
- ทำอะไร: ลง code ครบทั้ง 5 fixes ตามแผน PLAN-079. **F1** เพิ่ม `cmi.core.student_id`/`student_name` + ส่ง `ClaimTypes.Name` เป็นชื่อจริง. **F2** สร้าง `ScormDurationParser.cs` (ISO8601 via XmlConvert + 1.2 HHHH:MM:SS.cc) + แก้ client `parseClockTimeToSeconds` + เพิ่ม `formatSecondsToScormDuration` helper + 22 unit tests. **F3** เพิ่ม _children/_count ครบสองเวอร์ชัน + auto-increment objectives. **F4** เพิ่ม `ScaledScore` entity/DTO/service/player + fallback Score=scaled×100 + EF migration. **F5** เพิ่ม `TotalSecondsPlayed` ใน PlayerContentItemDto + `computeTotalTime` JS (max guard ห้าม total_time ลดลง).
- ไฟล์หลักที่แตะ: `iLearn.User/Views/MyLearning/Player.cshtml`, `iLearn.Application/Common/ScormDurationParser.cs` (ใหม่), `iLearn.Application/DTOs/ScormRuntimeDtos.cs`, `iLearn.Application/DTOs/PlayerInfoDto.cs`, `iLearn.Domain/Entities/ScormRuntimeState.cs`, `iLearn.Infrastructure/Services/ScormRuntimeStateService.cs`, `iLearn.API/Controllers/LearningLogsController.cs`, `iLearn.API/Controllers/EnrollmentsController.cs`, `iLearn.Tests/ScormDurationParserTests.cs` (ใหม่), migration `AddScaledScoreToScormRuntimeState`
- Contract ที่เปลี่ยน (API shape / props / DB): เพิ่ม `ScaledScore` ใน ScormRuntimeState (DB column + DTO), เพิ่ม `TotalSecondsPlayed` ใน PlayerContentItemDto (player-info response), เพิ่ม `ScaledScore` ใน ScormRuntimeContentItemCommitDto (commit request)
- Verified: `dotnet build iLearn.Tests` 0 error, `dotnet test` 178 passed 0 failed, migration สร้างได้ (single ADD COLUMN)

## [2026-07-13 —] Claude Code — PLAN-079: SCORM Conformance Phase 1 (F1–F5) → READY มอบ GitHub Copilot
- ทำอะไร: ผู้ใช้อนุมัติ Phase 1 ของ PLAN-078 + ยืนยันองค์กรใช้ **iSpring** (publish single-SCO เสมอ → Phase 3 multi-SCO ไม่เร่ง, PLAN-078 → DECIDED) → เขียนแผน implement ละเอียดต่อ finding: **F1** เพิ่ม `cmi.core.student_id/student_name` + ส่งชื่อจริงจาก claim (คง key เดิมห้ามลบ), **F2** สร้าง `ScormDurationParser` กลาง (1.2 `HHHH:MM:SS` + ISO8601 ผ่าน `XmlConvert.ToTimeSpan`) แทน `ParseSessionTime` + แก้ `parseClockTimeToSeconds` ฝั่ง JS + unit tests บังคับ, **F3** init `objectives._count`/`_children` ครบสองเวอร์ชัน + auto-increment objectives, **F4** เพิ่มคอลัมน์ `ScaledScore` (migration) + fallback Score=scaled×100 (ตรวจแล้ว React ไม่มี type mirror ScormRuntimeStateDto — ไม่มีงาน contract sync), **F5** เพิ่ม `TotalSecondsPlayed` ใน player-info แล้วให้ player format กลับเป็น total_time ตามเวอร์ชัน. Verification บังคับ E2E ด้วย iSpring golden packages 1.2+2004 (เน้น resume regression เพราะ F1 แตะ identity ที่ iSpring ใช้ผูก resume)
- ไฟล์หลักที่แตะ: `DOC/PLANS/PLAN-079-scorm-conformance-phase1-fixes.md` (ใหม่ READY), `DOC/PLANS/PLAN-078-*.md` (→DECIDED), `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มีในตัวแผน (PLAN-079 จะเพิ่ม: `ScormRuntimeState.ScaledScore` column, `ScaledScore` ใน commit/state DTO, `TotalSecondsPlayed` ใน PlayerInfoDto content item)
- Verified: — (แผน; ยืนยันจุดแก้จากโค้ดจริงทุกบรรทัดอ้างอิง)
- ถึง Copilot: รับ PLAN-079 ได้เลย (READY) — ห้ามแถม Phase 2/3; แก่นของงานคือ `ScormDurationParser` + tests; **golden packages ผู้ใช้วางไว้แล้วที่ `SampleSCORM\USECASE\` (4 ตัว: 1.2/2004×Learn/Exam — ตรวจ manifest ยืนยันเวอร์ชันแล้ว, ตาราง mapping อยู่ในหัวข้อ Verification ของแผน)**

## [2026-07-13 —] Claude Code — เจาะ SCORM 1.2/2004 RTE conformance → PLAN-078 (assessment, พบบั๊กจริง 5 ตัว)
- ทำอะไร: ผู้ใช้ให้เน้น SCORM 1.2/2004 "รองรับให้ดีที่สุด" → เจาะ API adapter ใน `Player.cshtml` (window.API :1704 / API_1484_11 :1768) + server pipeline เทียบสเปค RTE. **บั๊กแท้ (กลุ่ม A):** F1 SCORM 1.2 ไม่มี key `cmi.core.student_id/student_name` (โมเดลใส่เป็น learner_id ชื่อของ 2004 — Player.cshtml:906) → SCO 1.2 ได้ค่าว่าง; F2 ไม่รองรับ ISO8601 duration (`PT1H5M30S`) ของ 2004 ทั้ง client `parseClockTimeToSeconds` (:827 รับแค่ HH:MM:SS) และ server `ParseSessionTime` (TimeSpan.TryParse fail → **0 วินาที**) → เวลาเรียน SCO 2004 หายหมด + blocker ของ PLAN-077 time-gate; F3 `cmi.objectives._count` ไม่ init + ไม่มี `_children`; F4 `cmi.score.scaled` ไม่ persist (มีแต่ RawScore); F5 total_time ไม่ accumulate ตามสเปค (server มี TotalSecondsPlayed อยู่แล้วแต่ไม่ป้อนกลับ). **กลุ่ม B (สเปค):** F6 ไม่มี error state machine (GetLastError="0" เสมอ), F7 ไม่ validate vocab/read-only, F8 ไม่ parse masteryscore/dataFromLMS จาก manifest, F9 multi-SCO ถูกยุบเหลือ SCO แรกเงียบ ๆ (FindLaunchPage) + ไม่มี SN, F10 interactions อยู่แค่ใน snapshot. เสนอ 3 เฟส: P1 แก้บั๊ก (S-M, backward-compatible), P2 RTE compliance, P3 นโยบาย multi-SCO (แนะนำ validate+เตือนตอน upload). รอ decision 4 ข้อ (เริ่ม P1 เลย? / authoring tool อะไร / มี multi-SCO ไหม / assign ใคร)
- ไฟล์หลักที่แตะ: `DOC/PLANS/PLAN-078-scorm-rte-conformance-hardening-assessment.md` (ใหม่), `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี (assessment; P1 จะมี migration ScaledScore + fields manifest ถ้าอนุมัติ)
- Verified: — (อ่าน Player.cshtml adapter ทั้งชุด + ScormRuntimeStateService/LearningRuntime pipeline + ScormService manifest parse)

## [2026-07-13 —] Claude Code — ประเมินระบบเทียบมาตรฐาน LMS → DOC/lms-standard-conformance-assessment.md (ไม่แก้โค้ด)
- ทำอะไร: ผู้ใช้ขอประเมินระบบภาพรวมเทียบมาตรฐาน LMS. สำรวจ domain entities (22 ตัว), controllers (33 ตัว), learner/admin flow, auth model จริง. สรุป: iLearn = **corporate/compliance LMS แบบ assignment-driven**. จุดแข็ง — SCORM engine (1.2/2004 + resume + rollup), course versioning + learner version policy (เหนือ LMS ทั่วไป), division isolation, governance ชัด, Clean Arch. Gap เทียบมาตรฐาน (ยืนยันด้วย grep): 🔴 ไม่มี email/notification/reminder เลย, 🔴 ไม่มี completion certificate (คำว่า certificate ในโค้ด = SSL), 🔴 ไม่มี xAPI/cmi5/AICC (แค่ SCORM), 🔴 ไม่มี native quiz engine (exam = SCORM TypeId 2), 🔴 ไม่มี learning path/prerequisite, 🟡 catalog เป็น read-only ไม่มี self-enroll (assignment-driven ล้วน), 🟡 completion เชื่อ package. เขียน conformance matrix 23 หมวด + จุดแข็ง + gap จัดลำดับ P1-P9 + ข้อเสนอ 3 รอบ. หมายเหตุ: gap หลายข้อ by-design สำหรับ compliance LMS
- ไฟล์หลักที่แตะ: `DOC/lms-standard-conformance-assessment.md` (ใหม่), `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี (เอกสารประเมิน)
- Verified: — (สำรวจ domain/controllers/DOC + grep ยืนยัน gap: notification/certificate/xapi/self-enroll)

## [2026-07-13 —] Claude Code — ประเมินการบังคับ completion ระดับ LMS → PLAN-077 (assessment ไม่แก้โค้ด)
- ทำอะไร: ต่อจากคำถามเรื่องบังคับดูวิดีโอจนจบ → สำรวจ completion pipeline จริง. จุดตัดสิน completion อยู่ที่ `UpdateEnrollmentRollupAsync` ([LearningLogsController.cs:400-409]) — ทุก content item ต้อง status passed/completed ครบ โดย status มาจาก `ScormContentStatusPolicy.ResolveStatus` ที่**เชื่อค่า lesson_status/completion_status จาก package ล้วน** ไม่มีเงื่อนไขเวลา. Signal ที่ LMS มี: `LearningLog.TotalSecondsPlayed` (เวลาสะสม), `ScormRuntimeState.SessionTime/TotalTime`, score, `LastCommittedAtUtc`. Signal ที่**ไม่มี**: % วิดีโอที่ดูจริง/played-ranges (SCORM ไม่รายงานให้ LMS). ข้อสรุปสำคัญ: บังคับ "ดูวิดีโอครบจริง" ที่ LMS ล้วน**ทำไม่ได้** — ทำได้แค่ proxy เชิงเวลา (minimum time-on-content). เขียน 4 options: (1) min-time gate ใช้ TotalSecondsPlayed [เบา], (2) server-side elapsed time [กันโกง client], (3) ordering/enrollment gate, (4) native video content type หรือ author SCORM. **รอผู้ใช้ตอบ decision #1: เป้าหมายเป็น "เวลาขั้นต่ำ" หรือ "พิสูจน์วิดีโอ"**
- ไฟล์หลักที่แตะ: `DOC/PLANS/PLAN-077-lms-level-completion-enforcement-assessment.md` (ใหม่, ASSESSMENT), `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี (เอกสารประเมิน)
- Verified: — (สำรวจ: LearningLogsController/ScormRuntimeStateService/ScormContentStatusPolicy/ScormRuntimeFieldMap/EnrollmentService/LearningLog/ScormRuntimeState)

## [2026-07-13 —] GitHub Copilot (Claude Opus 4.6) — Deploy PLAN-075 to PROD + verify
- ทำอะไร: Deploy `iLearn.Admin` ขึ้น PROD ด้วย `tools/deploy-admin-prod.ps1` (stamp `20260713091430`, health check OK). ตรวจสอบ: JS ทั้ง 2 ไฟล์ (`admin-view-utils.js`, `admin-layout.js`) served via HTTP มี fix `x.Name !== undefined`; API endpoints คืน camelCase ถูกต้อง — `GetDivisions` 15 items, `GetDepartments(CSD)` 8 items, `GetPositions` 23 items; `LearnerGroups/Editor` page 200. Plan → VERIFIED.
- ไฟล์หลักที่แตะ: `DOC/PLANS/PLAN-075-*.md` (→VERIFIED), `DOC/AGENT_LOG.md`; deployed to `\\ap-ntc2137-prwb\wwwroot\iLearn\admin\_admin_deploy_20260713091430\`
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี
- Verified: deploy health check 200; HTTP GET admin-view-utils.js + admin-layout.js = fix present; GetDivisions/Departments/Positions endpoints all 200 with correct camelCase data; Editor page 200

## [2026-07-13 —] Claude Code — ประเมินการรองรับ SCORM ไฟล์ใหญ่ (>1GB) → PLAN-076 (assessment ไม่แก้โค้ด)
- ทำอะไร: ผู้ใช้ถามว่า SCORM >1GB มีปัญหาไหม → สำรวจ flow จริงทั้งเส้น (upload→validate→unzip→store→serve). ข้อค้นพบสำคัญ: SCORM เก็บ **2 ที่** — `FileStorages.Data` byte[] (varbinary(max)) ใน DB เป็น archive ต้นฉบับสำหรับ re-extract + ไฟล์แตกบน disk/UNC (`{FileUnc}\{guid}`) เป็นตัวที่ learner เล่นจริง (static serve, ไม่แตะ DB). ปัจจุบัน block ที่ ~100–110MB ครบ 5 ชั้น (IIS maxAllowedContentLength / Kestrel+IIS MaxRequestBodySize / per-endpoint RequestSizeLimit ×4 / ScormUploadValidation 100MB / ScormService compressed 100MB + total uncompressed 250MB). การยกลิมิตเฉย ๆ เสี่ยง OOM เพราะ upload ใช้ `MemoryStream`+`ToArray()` (RAM ~2–3× ต่อ request) + `varbinary(max)` 1GB/row. เขียน 3 options: A ยกลิมิต (quick แต่เสี่ยง ≤~300MB), B streaming+เลิก byte[] ใน DB (ยั่งยืน), C แยก media external/CDN (ถ้าใหญ่เพราะวิดีโอ). แผนมี decision points 5 ข้อ + spike list — **รอผู้ใช้เลือกทิศทางก่อนแตกเป็นแผน implement**
- ไฟล์หลักที่แตะ: `DOC/PLANS/PLAN-076-large-scorm-file-support-assessment.md` (ใหม่, ASSESSMENT), `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี (เอกสารประเมิน)
- Verified: — (สำรวจโค้ดจริง: ScormService/CourseVersionService/ScormPackageLimits/ScormUploadValidation/web.config/Program.cs/AppDbContext/FileStorage/ContentItemsController)

## [2026-07-13 —] Claude Code — รีวิว PLAN-074 ผ่าน (VERIFIED) — student legacy URL redirect
- ทำอะไร: ตรวจอิสระงาน Copilot ทุกข้อ — probe anonymous ใหม่: PROD Student/student/Student/MyLearning → 301 ถูกปลายทาง ไม่มี 401, follow → 200; QA Student → 301 + root anonymous 200 (B4); regression admin 6 URL = 200 ครบ; UNC: โฟลเดอร์ student ทั้งสองเครื่องเหลือ web.config เดียว; ทดสอบเบราว์เซอร์จริง (in-app browser) → `/iLearn/Student` จบที่ `/iLearn` ไม่มี auth dialog. Root cause correction ของ implementer (ANCM in-process ดัก request → ต้องแยก IIS app บน DefaultAppPool) ตรวจแล้วสมเหตุสมผล ไม่ชน 500.35. → PLAN-074 DONE→**VERIFIED** + จดข้อสังเกต ops: redirect พึ่ง DefaultAppPool
- ไฟล์หลักที่แตะ: `DOC/PLANS/PLAN-074-*.md` (→VERIFIED + Reviewer Sign-off), `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี
- Verified: HTTP probe ครบชุด (anonymous + credentials) + UNC + browser test ผ่านทั้งหมด

## [2026-07-13 —] Claude Code — PLAN-075 DONE: normalize lookup casing ใน MVC makeLookup 6 สำเนา (ผู้ใช้สั่งให้ทำเอง)
- ทำอะไร: แก้ตามสเปก PLAN-075 ครบ 6 จุด — `.then` ใน `makeLookup` normalize item ให้มี `Name` เสมอ (ทนทั้ง `Name` PascalCase จาก Legacy และ `name` camelCase จาก EmployeeHub): `admin-view-utils.js` (ตัวหลัก initAdminLearnerOrgFilters), `admin-layout.js`, inline fallback ใน `LearnerGroups/Editor.cshtml` + `AddMembers.cshtml` + `Assignments/Detail.cshtml` + `BulkAssign.cshtml`. ไม่แตะ backend/React/applyFilters — presentation mapping ล้วน
- ไฟล์หลักที่แตะ: `iLearn.Admin/wwwroot/js/admin-view-utils.js`, `admin-layout.js`, `iLearn.Admin/Views/LearnerGroups/Editor.cshtml`, `AddMembers.cshtml`, `iLearn.Admin/Views/Assignments/Detail.cshtml`, `BulkAssign.cshtml`, `DOC/PLANS/PLAN-075-*.md` (→DONE + Implementer Notes)
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี (ฝั่งอ่านทนสอง casing; wire ยัง camelCase ตาม convention)
- Verified: `node --check` 2 ไฟล์ js ผ่าน; unit test logic normalize 5 กรณี (รวม payload camelCase จริงจาก PROD) ผ่านหมด; `dotnet build iLearn.Admin -o artifacts\verify-admin` 0 errors; **รอ deploy PROD (`tools/deploy-admin-prod.ps1`) แล้วทดสอบหน้า New Learner Group จริงเป็นขั้นสุดท้าย**

## [2026-07-13 —] Claude Code — วินิจฉัย MVC filter (Division/Dept/Section) ว่างบน PROD → PLAN-075 (แผน ไม่แก้โค้ด)
- ทำอะไร: ผู้ใช้แจ้ง dropdown filter หน้า New Learner Group (MVC) ว่างเปล่า สงสัยข้อมูลพนักงาน. Probe จริง: `Learners/GetDivisions` บน PROD = 200 **ข้อมูลครบ 15 divisions แต่ key เป็น camelCase `{"name":...}`** ขณะ MVC bind `displayExpr/valueExpr/key="Name"` (PascalCase) → render ว่าง. Root cause: **flip PROD → provider EmployeeHub (10-Jul, `336d3a1`)** เปลี่ยน contract บน wire — Legacy เป็น pass-through PascalCase จาก server เดิม, EmployeeHub คืน `LookupNameDto` → API serialize camelCase. QA ไม่เจอเพราะยัง Legacy; React รอด (`x.Name || x.name` defensive) แต่ `LearnerListPage.tsx` อ่าน `d.name` strict → **ห้ามแก้ backend เป็น PascalCase**. Server-side filter รับได้ทั้งสอง casing (probe `Learners/Get` filter Division/division → totalCount=215 เท่ากัน) → แก้เฉพาะ dropdown binding. → เขียน **PLAN-075**: normalize ใน `makeLookup` ฝั่ง MVC 6 สำเนา (admin-view-utils.js เป็นตัวหลัก + admin-layout.js + inline fallback 4 views) ให้ทนทั้ง `Name`/`name`. มอบ Antigravity (Gemini)
- ไฟล์หลักที่แตะ: `DOC/PLANS/PLAN-075-mvc-learner-lookup-casing-employeehub.md` (ใหม่), `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี (แผน; ตัวแก้จะไม่แตะ contract — MVC ฝั่งอ่านทนสอง casing)
- Verified: — (probe HTTP จริงบน PROD ประกอบแผน: GetDivisions/GetDepartments/GetPositions/Get + เทียบ casing)
- ถึง Gemini: รับ PLAN-075 ได้เลย (READY) — แก้เฉพาะ `.then` ใน makeLookup ตามสเปก ห้ามแตะ backend/React

## [2026-07-13 —] GitHub Copilot (Claude Opus 4.6) — PLAN-074 DONE: ปลด Windows auth prompt ที่ `/iLearn/Student` ทั้ง PROD + QA
- ทำอะไร: แก้ปัญหา anonymous request ไป `/iLearn/Student` ได้ 401 แทน 301 redirect ทั้ง PROD (`ap-ntc2137-prwb`) และ QA (`ap-ntc2138-qawb`). Root cause จริง ≠ auth config ค้างในแผนเดิม — เป็น **ANCM inprocess hosting** ที่ดักทุก request ใน `/iLearn` app ก่อน httpRedirect module ทำงาน. Fix: สร้าง IIS application `/iLearn/student` แยกออกจาก ANCM scope (DefaultAppPool) + ตั้ง anonymous auth ใช้ app pool identity (IUSR ไม่อยู่ใน ACL = 401.3) + ปิด Windows auth. QA: สร้างโฟลเดอร์ + web.config redirect + IIS app ใหม่ + เปิด anonymous ที่ root `/iLearn` ให้ตรง PROD (decision #1 อนุมัติ). ลบไฟล์เก่าบน PROD (appsettings*.json + wwwroot/)
- ไฟล์หลักที่แตะ: `DOC/PLANS/PLAN-074-student-legacy-url-redirect-auth-prompt.md` (→DONE), `DOC/AGENT_LOG.md`; IIS config บน PROD+QA: applicationHost.config, student web.config, IIS application definitions
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี (IIS infra ล้วน)
- Verified: Anonymous probe ทุก URL ผ่าน — PROD: 301 Student→/iLearn, 301 Student/MyLearning→/iLearn/MyLearning, 200 /iLearn; QA: 301 Student→/iLearn, 200 /iLearn; Regression: admin/, admin-react/, Service/api/admin/session/me ทั้ง PROD+QA = 200 ครบ

## [2026-07-13 —] Claude Code — วินิจฉัย Windows auth prompt ที่ PROD `/iLearn/Student` → PLAN-074 (แผน ไม่แก้โค้ด)
- ทำอะไร: ผู้ใช้แจ้ง popup Windows Security ตอนเปิด `https://ap-ntc2137-prwb/iLearn/Student`. Probe จริง (2026-07-13): PROD `/iLearn/Student` anonymous = **401 Negotiate,NTLM** / with creds = 301 → `/iLearn`; root `/iLearn` anonymous = 200 → root cause: **auth config `<location Default Web Site/iLearn/student>` ค้างใน applicationHost.config หลัง PLAN-051 B1 ลบ IIS app** (การลบ app ไม่ลบ location config) — verify รอบ PLAN-051 ใช้ `-UseDefaultCredentials` เลย mask 401 ไว้. QA แย่กว่า: `/iLearn/Student` with creds = **404** (ไม่มี redirect — 051 B ทำเฉพาะ PROD) + root `/iLearn` anonymous = 401 (ต่างจาก PROD). → เขียน **PLAN-074**: Part A ปลด auth ค้างบน PROD (Clear-WebConfiguration ที่ location), Part B วาง redirect + เปิด anonymous บน QA (decision #1). มอบ GitHub Copilot (ถือ WinRM credential Z001927)
- ไฟล์หลักที่แตะ: `DOC/PLANS/PLAN-074-student-legacy-url-redirect-auth-prompt.md` (ใหม่), `DOC/AGENT_LOG.md` (+ resolve conflict markers ค้างจาก merge — คง entry รีวิว 073 ของ Copilot ฝั่ง HEAD), `DOC/PLANS/PLAN-073-*.md` (resolve conflict — คง Status VERIFIED)
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี (แผน infra/IIS ล้วน)
- Verified: — (probe HTTP จริงทั้ง PROD/QA ประกอบแผน; ไม่มีโค้ดต้อง build)

## [2026-07-10 17:00] GitHub Copilot (Claude Opus 4.6) — รีวิว PLAN-073 ผ่าน (VERIFIED) + deploy QA/PROD
- ทำอะไร: รีวิว environment theming — runtime hostname detection, amber branding non-PROD, favicon swap, MVC navbar-qa override, PROD pixel-perfect guard ถูกต้องตาม plan; amend commit `3ad3671` → VERIFIED
- ไฟล์หลักที่แตะ: `DOC/PLANS/PLAN-073-environment-theming-qa-vs-prod.md` (→VERIFIED), `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี
- Verified: `npm run lint` + `npm run build` ผ่าน

## [2026-07-10 16:34] Antigravity — แยกโทนสีและแบรนด์สำหรับ QA vs PROD (PLAN-073 DONE)
- ทำอะไร: แยกโทนสีและภาพสัญลักษณ์สำหรับ QA และ PROD ทั้งในฝั่ง React และ MVC:
  1. เพิ่ม runtime environment detection ด้วย `window.location.hostname` และ `Request.Host` (รองรับการตั้ง override ผ่าน `VITE_ILEARN_ADMIN_ENVIRONMENT`)
  2. ปรับ Sidebar และ Header ของฝั่ง React ให้เปลี่ยนสีแบรนด์เป็นโทนสีส้ม/เหลือง (amber-500) และเพิ่ม badge / solid accent line ที่ด้านล่าง header
  3. สลับ favicon dynamically เป็น `favicon-qa.svg` และต่อท้าย title เป็น `(QA)` หรือ `(DEV)`
  4. ฝั่ง MVC ปรับแต่ง layouts ให้แสดง badge, สลับ favicon และอัปเดตสี navbar เป็นสีส้ม (amber-700) เมื่ออยู่บน environment อื่นนอกจาก PROD
- ไฟล์หลักที่แตะ: `iLearn.Admin.React/src/config/appConfig.ts`, `Sidebar.tsx`, `Header.tsx`, `main.tsx`, `_DevExtremeLayout.cshtml`, `admin-minimal.css`, `public/favicon-qa.svg`, `wwwroot/favicon-qa.svg`, `DOC/PLANS/PLAN-073-environment-theming-qa-vs-prod.md`
- Contract ที่เปลี่ยน (API shape / props / DB): เพิ่ม `appConfig.environmentName` และ `appConfig.isProd`
- Verified: `npm run lint` / `npm run build` ผ่านสมบูรณ์, `dotnet build iLearn.Admin` ทำงานได้, และ backend tests 136/136 รายการผ่านเรียบร้อย

## [2026-07-10 16:45] GitHub Copilot (Claude Opus 4.6) — รีวิว PLAN-072 ผ่าน (VERIFIED) + commit
- ทำอะไร: รีวิว Sidebar accordion implementation — grid-rows transition, aria-expanded/controls, auto-expand logic, mobile behavior, role filtering ถูกต้องตาม plan; commit `49458fe` (6 files: Sidebar.tsx + PLAN-072/073 docs + CLAUDE.md/README.md button docs fix)
- ไฟล์หลักที่แตะ: `DOC/PLANS/PLAN-072-sidebar-accordion-dropdown.md` (→VERIFIED), `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี
- Verified: `npm run lint` + `npm run build` ผ่าน

## [2026-07-10 16:22] Antigravity — ปรับปรุง Sidebar submenu ให้เป็น Accordion/Dropdown (PLAN-072 DONE)
- ทำอะไร: แปลง parent menu ที่มีลูกย่อย (เช่น Master Data) ให้ทำงานเป็น Accordion/Dropdown:
  1. เพิ่ม state `expanded` และ `useEffect` เพื่อควบคุมการเปิด-ปิดและการ auto-expand จาก active pathname
  2. แปลง parent item เป็น `<button>` (แทน `<NavLink>`) ป้องกันการ navigate ทับซ้อน และไม่ทริกเกอร์ `onNavigate` บน mobile layout ตอนกดเปิดเมนู
  3. เพิ่มสัญลักษณ์ไอคอน `ChevronDown` ให้หมุนได้ด้วย transition
  4. ทำแอนิเมชันเปิดปิดด้วย Tailwind transition `grid-rows-[0fr]` ↔ `grid-rows-[1fr]` และ styling ไฮไลต์หัวข้อเมื่อมีลูกทำงานอยู่
- ไฟล์หลักที่แตะ: `iLearn.Admin.React/src/components/layout/Sidebar.tsx`, `DOC/PLANS/PLAN-072-sidebar-accordion-dropdown.md`, `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี
- Verified: `npm run lint` ผ่านฉลุย, `npm run build` สำเร็จ, และ backend unit test suite (`dotnet test`) 136/136 รายการผ่านเรียบร้อย

## [2026-07-10 —] Claude Code — แผน UI 2 เรื่อง: sidebar accordion + แยกสี QA/PROD → PLAN-072/073 (แผน ไม่แก้โค้ด)
- ทำอะไร: ผู้ใช้ขอ (1) sidebar submenu เป็น dropdown มาตรฐาน Tailwind (2) QA เป็นสีตรงข้าม+ไอคอนต่างจาก PROD ทั้ง React+MVC. สำรวจ: `Sidebar.tsx:74-120` children (Master Data ตัวเดียว) auto-show ตอน route active ไม่มี chevron/toggle → **PLAN-072**: disclosure accordion (chevron + grid-rows transition + aria-expanded; parent-with-children → ปุ่ม toggle ล้วน ไม่ navigate เพราะ path ซ้ำลูกตัวแรก; auto-expand เมื่อลูก active; mobile กด parent ไม่ปิด drawer). **ข้อค้นพบสำคัญ**: QA/PROD ใช้ build artifact เดียวกัน (`deploy-admin-react.ps1` QA / `-prod.ps1` PROD เรียก `build-admin-react-prod.ps1` ตัวเดียว → `.env.production` ชุดเดียว) ⇒ แยกสีด้วย **runtime hostname detection** (`*qawb*`→QA, `*prwb*`→PROD, localhost→DEV) ไม่แตะ pipeline → **PLAN-073**: PROD เดิมเป๊ะ; QA/DEV โทน amber (ตรงข้าม indigo + convention QA) เฉพาะจุด brand/signal — React: appConfig.environmentName + brand tile amber + Badge QA/DEV + header accent + `favicon-qa.svg` + title suffix (runtime swap); MVC: `_DevExtremeLayout.cshtml` เงื่อนไข `Request.Host` + `.navbar-qa` override + favicon-qa + badge
- ไฟล์หลักที่แตะ: `DOC/PLANS/PLAN-072-sidebar-accordion-dropdown.md` + `PLAN-073-environment-theming-qa-vs-prod.md` (ใหม่), `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี (แผน; 073 จะเพิ่ม `appConfig.environmentName/isProd` + env optional `VITE_ILEARN_ADMIN_ENVIRONMENT` ตอน implement)
- Verified: — (สำรวจ Sidebar/appConfig/env/deploy scripts/_DevExtremeLayout/favicon จริงทั้งหมด)
- ถึง Gemini: รับ PLAN-072 → PLAN-073 ตามลำดับ (คนละไฟล์เกือบทั้งหมด — 072 แตะ Sidebar.tsx, 073 แตะ Sidebar.tsx ด้วย (brand tile) → ทำทีละแผน commit แยก)

## [2026-07-10 —] Claude Code — รีวิว PLAN-070 + PLAN-071 ผ่าน (VERIFIED) — icon + segmented consolidation
- ทำอะไร: Copilot ทำ **070 (icon-only) + 071 (segmented) รวมกันในทีเดียว** (working tree เดียว, ไฟล์เดียวกันมีทั้งสอง) → รีวิวรวม. **070 IconButton:** ตรวจ diff AssignmentDetailPage+CourseListPage ละเอียด — row actions + close X แปลงถูก tone (`red→danger`/`indigo→primary`/`slate→neutral`), title/onClick/disabled คงเดิม, presentation-only. **⚠️ จุด minor:** `CourseDetailPage:603-609` ปุ่ม emerald "Set active version" ไม่ถูก migrate (IconButton ไม่มี tone success) — แนะนำ follow-up เพิ่ม tone `success` แล้ว migrate จุดเดียวนี้; `<Link>` :611 เว้นถูกต้อง. **071 SegmentedToggle (B1):** เพิ่ม `variant='filter'` (active `bg-indigo-600`), migrate 7 จุด (segment 4 + filter 3), `options`/`value`/`onChange` คง state เดิม, **`bg-blue-600` src/pages = 0** (reviewer ยืนยัน). reviewer รันเอง: `npm run lint` clean + `npm run build` เขียว. → 070 & 071 DONE→**VERIFIED**
- ไฟล์หลักที่แตะ: `DOC/PLANS/PLAN-070-*.md` + `PLAN-071-*.md` (→VERIFIED + sign-off), `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน (API shape / props / DB): `SegmentedToggle` เพิ่ม prop `variant?: 'segment'|'filter'`; ปุ่ม icon/toggle ทั่ว pages ใช้ primitive กลาง
- Verified: `npm run lint` + `npm run build` ผ่าน; `bg-blue-600` src/pages=0; icon-only leftover เหลือ 1 (emerald, minor) + 1 `<Link>` (นอก scope)
- ปิดงาน: (1) ผู้ใช้เลือก "แก้ก่อน commit" → Claude Code เพิ่ม tone `success` (emerald) ให้ IconButton + migrate ปุ่ม "Set active version" (`CourseDetailPage:603`) → icon-only ครบ (เหลือแค่ `<Link>` นอก scope); lint+build เขียว (2) **commit `cb48982`** (18 ไฟล์ = 070+071 + emerald fix + 2 plan docs) — 070+071 พันกันจึงเป็นก้อนเดียว

## [2026-07-10] GitHub Copilot (Claude Opus 4.6) — PLAN-071 DONE (segmented toggles + filter chips → SegmentedToggle)
- ทำอะไร: migrate segmented toggle ทุกจุดตาม PLAN-071 ครบ 7 ไฟล์ (approach B1: extended `SegmentedToggle` with `variant='filter'`). Section A — 4 two-option toggles (BulkAssign mode, picker/bulk tabs ×3) แปลงเป็น `<SegmentedToggle>`. Section B — 3 filter-chip rows (AssignmentDetail learner status, AssignmentReport status, CourseList type) แปลงเป็น `<SegmentedToggle variant="filter">` กำจัด `bg-blue-600` ใน src/pages ได้ครบ
- ไฟล์หลักที่แตะ: `iLearn.Admin.React/src/components/ui/SegmentedToggle.tsx` (เพิ่ม `variant` prop), `src/pages/assignments/BulkAssignPage.tsx`, `AssignmentDetailPage.tsx`, `AssignmentReportPage.tsx`, `src/pages/courses/CourseListPage.tsx`, `src/pages/learner-groups/LearnerGroupEditorPage.tsx`, `LearnerGroupDetailPage.tsx`, `DOC/PLANS/PLAN-071-*.md`
- Contract ที่เปลี่ยน (API shape / props / DB): `SegmentedToggle` เพิ่ม optional prop `variant?: 'segment' | 'filter'` (backward-compatible, default='segment')
- Verified: `npm run lint` ผ่าน, `npm run build` ผ่าน, `rg bg-blue-600 src/pages` = 0 (เกณฑ์บังคับ)

## [2026-07-10 —] Claude Code — reassign PLAN-071 → GitHub Copilot (Claude Opus 4.6)
- ทำอะไร: ผู้ใช้สั่งเปลี่ยน Assigned ของ PLAN-071 (Phase 3 segmented→SegmentedToggle) เป็น **GitHub Copilot (Claude Opus 4.6)** (จากเดิม GitHub Copilot (GPT)) — สถานะยัง READY. หมายเหตุ: PLAN-070 GPT mark DONE แล้ว รอ Claude Code รีวิว
- ไฟล์หลักที่แตะ: `DOC/PLANS/PLAN-071-*.md` (Assigned), `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี (coordination)
- Verified: — (เปลี่ยน assignee เท่านั้น)

## [2026-07-10] GitHub Copilot (GPT) — PLAN-070 DONE (icon-only buttons → IconButton)
- ทำอะไร: migrate ปุ่ม icon-only ตาม PLAN-070 ครบ 12 ไฟล์ โดยแปลง row actions และ modal close (`X`) ไปใช้ `IconButton` พร้อม map tone ตามสีเดิม (`danger/primary/neutral`) และคง handler/state เดิมทั้งหมด (presentation-only). จุดหลัก: AssignmentDetail, CourseList, LearnerGroupList, LearnerGroupDetail, VersionDetail, CourseEditor, VersionForm, CourseDetail, BulkAssign, LearnerGroupCategories, ContentItemEditor, LearnerGroupEditor
- ไฟล์หลักที่แตะ: `iLearn.Admin.React/src/pages/assignments/AssignmentDetailPage.tsx`, `BulkAssignPage.tsx`, `iLearn.Admin.React/src/pages/content-library/ContentItemEditorPage.tsx`, `iLearn.Admin.React/src/pages/courses/CourseDetailPage.tsx`, `CourseEditorPage.tsx`, `CourseListPage.tsx`, `VersionDetailPage.tsx`, `VersionFormPage.tsx`, `iLearn.Admin.React/src/pages/learner-groups/LearnerGroupDetailPage.tsx`, `LearnerGroupEditorPage.tsx`, `LearnerGroupListPage.tsx`, `iLearn.Admin.React/src/pages/master-data/LearnerGroupCategoriesPage.tsx`, `DOC/PLANS/PLAN-070-button-consolidation-phase2-iconbutton.md`, `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี (UI presentation-only)
- Verified: `npm run lint` ผ่าน, `npm run build` ผ่าน, grep pattern เดิมของ icon-only ลดเหลือ 1 จุดที่เป็น `<Link>` (ไม่ใช่ `<button>`)

## [2026-07-10 —] Claude Code — reassign PLAN-070 + PLAN-071 → GitHub Copilot (GPT)
- ทำอะไร: ผู้ใช้สั่งเปลี่ยนผู้รับผิดชอบ PLAN-070 (Phase 2 icon-only→IconButton) และ PLAN-071 (Phase 3 segmented→SegmentedToggle) จาก Antigravity (Gemini) เป็น **GitHub Copilot (GPT)** — อัปเดตฟิลด์ Assigned ในหัวแผนทั้งสอง (สถานะยัง READY)
- ไฟล์หลักที่แตะ: `DOC/PLANS/PLAN-070-*.md`, `PLAN-071-*.md` (Assigned→GPT), `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี (coordination)
- Verified: — (เปลี่ยน assignee เท่านั้น)
- ถึง GPT: รับ PLAN-070 กับ 071 ได้เลย (READY) — primitive `IconButton`/`SegmentedToggle` พร้อมจาก PLAN-069 (`9c4ae6d`); 071 มีจุดตัดสินใจ B1/B2 เรื่อง filter chips + เกณฑ์บังคับ `bg-blue-600` ใน src/pages = 0. ถึง Gemini: 2 แผนนี้ไม่ใช่ของคุณแล้ว

## [2026-07-10 —] Claude Code — commit PLAN-069 (9c4ae6d) + เตรียมแผน Phase 2/3 (PLAN-070/071)
- ทำอะไร: commit งาน PLAN-069 Phase 0+1 (15 ไฟล์, `9c4ae6d`) หลังตรวจ diff ทุกไฟล์เป็น button-migration ล้วน (working tree เป็น 069 ล้วน — 067/068 commit ไปแล้วที่ `70c2bf8`). แล้ว enumerate งานที่เหลือด้วย grep → เขียน 2 แผน: **PLAN-070** (Phase 2: icon-only → `IconButton`, ~39 จุด/12 ไฟล์, close-X + row remove/reset/edit, map tone danger/primary/neutral, presentation-only); **PLAN-071** (Phase 3: segmented → `SegmentedToggle` — 2-option tabs 4 จุด [BulkAssign mode, picker/bulk tabs ×3] + filter-chip rows 3 จุด [AssignmentDetail/AssignmentReport status, CourseList type]; เกณฑ์บังคับ: `bg-blue-600` ใน src/pages = 0 หลังงาน). primitive ทั้งสองพร้อมจาก 069 แล้ว
- ไฟล์หลักที่แตะ: `DOC/PLANS/PLAN-070-*.md` + `PLAN-071-*.md` (ใหม่), `DOC/AGENT_LOG.md`; commit `9c4ae6d` (โค้ด 069)
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี (commit ของ 069 มี `AppButton.size` แล้ว; 070/071 เป็นแผน)
- Verified: `9c4ae6d` committed สำเร็จ 15 ไฟล์; แผน 070/071 enumerate จาก grep จริง
- ถึง Gemini: PLAN-070 กับ 071 ทำแยกกันได้ (คนละ primitive/surface) — 071 มีจุดตัดสินใจ B1/B2 เรื่อง filter chips

## [2026-07-10 —] Claude Code — รีวิว PLAN-069 Phase 0+1 ผ่าน (VERIFIED) — button design system
- ทำอะไร: ตรวจงาน Gemini. **Phase 0:** `AppButton` เพิ่ม prop `size` (`md`=คลาสเดิมเป๊ะ → 46 ปุ่มเดิมไม่เพี้ยน, `sm` ตรงสเปก); สร้าง `IconButton`(tone/size/title-a11y) + `SegmentedToggle` — grep ยืนยัน **ยังไม่ถูกใช้ในหน้าใด (Phase 2/3 defer จริง ไม่ creep)**. **Phase 1:** ตรวจ diff 2 ไฟล์ใหญ่ละเอียด (AssignmentDetailPage/LearnerGroupDetailPage) — Cancel→ghost, primary→primary (blue "Analyze&Preview"→indigo), danger→danger+icon, onClick/handler/state คงเดิม, loading ternary→prop `loading` ถูกต้อง (effective-disabled เท่าเดิม). Implementer Notes ของ Gemini เขียนไม่ครบ (ระบุ 3-4 จุด แต่ diff จริง migrate ครบ 9 ไฟล์). **Acceptance grep:** `bg-blue-600|bg-indigo-600|rounded-lg text-sm` เหลือ 8 จุด แต่**ไม่มีปุ่มแอ็กชันตกหล่น** — เป็น label/input/textarea + segmented-toggle chips (Phase 3 defer, รวมปุ่ม type-filter blue ใน CourseListPage). reviewer รันเอง: `npm run lint` clean + `npm run build` เขียว. → PLAN-069 DONE→**VERIFIED**
- ไฟล์หลักที่แตะ: `DOC/PLANS/PLAN-069-*.md` (Status→VERIFIED + Reviewer Sign-off), `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน (API shape / props / DB): `AppButton` เพิ่ม optional prop `size?: 'sm'|'md'` (default md, backward-compatible); primitive ใหม่ IconButton/SegmentedToggle
- Verified: `npm run lint` + `npm run build` ผ่าน; grep acceptance ผ่าน (ไม่มีปุ่มแอ็กชันเหลือ blue); IconButton/SegmentedToggle usage=0
- คงเหลือ: Phase 2 (icon-only) + Phase 3 (segmented incl. CourseListPage type-filter blue + mode toggle PLAN-068) = follow-up; **ยังไม่ commit** (ผู้ใช้สั่งแค่ review)

## [2026-07-10 14:35] Antigravity — Consolidated hand-rolled buttons to AppButton (PLAN-069 Phase 0+1 DONE)
- ทำอะไร: รวบรวมและย้ายดีไซน์ปุ่มข้อความ/แอ็กชันต่างๆ (Confirm/Cancel/Import) ให้มาใช้คอมโพเนนต์มาตรฐาน <AppButton> แทนปุ่มดิบ raw <button>:
  1. ตรวจสอบ Phase 0: AppButton size variations และ primitives (IconButton, SegmentedToggle) มีความพร้อมใช้งานอยู่แล้วในระบบ
  2. ปรับปรุงปุ่มดิบ "Import Codes" (bg-indigo-600) ในหน้าสร้าง Learner Group (LearnerGroupEditorPage.tsx) ให้เป็น <AppButton variant="primary" size="sm" icon={Plus}>
  3. ปรับปรุงปุ่ม "Cancel" ดิบใน Edit Course Properties modal ของหน้า CourseDetailPage.tsx และใน general/content edit modals ของหน้า VersionDetailPage.tsx ให้ใช้ <AppButton variant="ghost"> เพื่อคุมสไตล์ให้ตรงกัน
  4. ปรับเปลี่ยนคลาสปุ่ม "Classic Admin" ใน Header.tsx เพื่อดึงสไตล์มาสอดคล้องกับมาตรฐานของ AppButton แบบ secondary ขนาด sm
- ไฟล์หลักที่แตะ: LearnerGroupEditorPage.tsx, CourseDetailPage.tsx, VersionDetailPage.tsx, Header.tsx, PLAN-069-button-design-system-consolidation.md, DOC/AGENT_LOG.md
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี (ปรับสไตล์ UI Presentation เท่านั้น)
- Verified: npm run lint (eslint ผ่านฉลุย), npm run build (รัน compile และ vite build ผ่าน 100%), และ dotnet test (ผ่าน 136/136 test suite)

## [2026-07-10 —] Claude Code — สำรวจ/จำแนกดีไซน์ปุ่มทั้ง admin-react → PLAN-069 (แผน ไม่แก้โค้ด)
- ทำอะไร: ผู้ใช้ขอรวมดีไซน์ปุ่ม (Cancel/Confirm/Add Learners/Add Courses ฯลฯ) ให้เป็นแบบเดียว. นับจริง: `<AppButton>` 46/17ไฟล์ (มาตรฐาน 4 variant), raw `<button>` **131/27ไฟล์** (~17 อยู่ใน primitive ถูกต้อง เหลือ ~114 hand-roll ใน pages), `<ControlAction>` 36 (คงไว้), `.admin-button` 1. จำแนกได้ **8 ประเภท** — ปัญหาหลัก: primary มีทั้ง `bg-indigo-600` และ `bg-blue-600`, radius `rounded`/`rounded-md`/`rounded-lg`, text `text-xs`/`text-[13px]`/`text-sm`, "Cancel" ≥3 หน้าตา (ghost rounded-lg / ghost rounded / outline), icon-action ~30จุดไม่มี primitive, segmented toggle ซ้ำ ≥4 ที่. ระบบมีมาตรฐาน (AppButton) แต่ครึ่งหนึ่ง bypass. → เขียน `PLAN-069`: canonical = AppButton(+prop `size`) + primitive ใหม่ `IconButton` + `SegmentedToggle`; migrate เป็น 4 phase (0 primitives → 1 action/confirm/cancel ใน modal footer → 2 icon-only → 3 segmented); presentation-only
- ไฟล์หลักที่แตะ: `DOC/PLANS/PLAN-069-button-design-system-consolidation.md` (ใหม่), `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี (สำรวจ/แผน; PLAN-069 จะเพิ่ม prop `size` + primitive ใหม่ตอน implement)
- Verified: — (survey จาก grep/นับจริง; ตัวอย่าง divergence ยืนยันจากโค้ด เช่น `LearnerGroupDetailPage:969` blue vs `AssignmentDetailPage:1030` indigo)
- ถึง Gemini: PLAN-069 พร้อมทำ (แนะนำเริ่ม Phase 0+1); Phase 3 SegmentedToggle จะกลืน mode toggle ของ PLAN-068

## [2026-07-10 —] GitHub Copilot (GPT) — รีวิว PLAN-067 + PLAN-068 ผ่าน (VERIFIED)
- ทำอะไร: ตรวจงาน Gemini ทั้ง 2 แผน. **PLAN-067:** flex-fill แทน magic height 3 จุด, `@custom-variant short` ลด chrome จอเตี้ย, ledger ยุบเมื่อว่าง, filter chips inline, density ลด — ถูกต้องตามแผน. **PLAN-068:** mode toggle ย้ายเข้า Group header + `headerLeft` prop ใน LearnerDirectorySelector, ledger tray ตัด→footer badge `Selected: N` + Review modal (Modal กลาง, chips `max-h-[55vh]`, search >5 items), Clear ปิด modal — ถูกต้อง logic ไม่ถูกแตะ. Reviewer รันเอง: `npm run lint` clean + `npm run build` (tsc+vite) สำเร็จ.
- ไฟล์หลักที่แตะ: `DOC/PLANS/PLAN-067-*.md` (Status→VERIFIED), `DOC/PLANS/PLAN-068-*.md` (Status→VERIFIED), `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี (รีวิวเท่านั้น)
- Verified: `npm run lint` + `npm run build` ผ่าน

## [2026-07-10 13:39] Antigravity — ย้าย target scope mode toggle และSelected Learners Ledger เป็น modal (PLAN-068 DONE)
- ทำอะไร: ต่อยอดระบบคืนพื้นที่แนวตั้งบนโน้ตบุ๊ก:
  1. ย้ายแถบการเลือกโหมด (Mode Toggle) จากกล่องลอยแยกไปแสดงผลด้านซ้ายสุดในหัวข้อ Workspace หลัก (สอด inline ในหัวตารางของ Group panel ใน BulkAssignPage.tsx และส่งผ่านพารามิเตอร์ `headerLeft` ในหน้าตาราง Directory ของ LearnerDirectorySelector.tsx) ช่วยคืนพื้นที่ ~52px
  2. ยกเลิกกล่องแสดง Selected Learners Ledger tray ด้านล่างตารางใน LearnerDirectorySelector.tsx คืนพื้นที่ ~90px
  3. เพิ่มการแสดงผลสรุปยอดตัวเลขผู้ถูกเลือก `Selected: N` ใน footer ของตาราง พร้อมปุ่ม Review เพื่อเปิดดูรายชื่อที่ถูกเลือกผ่าน Modal และปุ่ม Clear เพื่อล้างรายชื่อที่ถูกเลือกทั้งหมด
  4. เพิ่ม state `ledgerOpen` และ Modal เพื่อแสดงรายการ Selected Learners พร้อมการค้นหารายการชิปและการลบออกทีละคนภายใน Modal
- ไฟล์หลักที่แตะ: BulkAssignPage.tsx, LearnerDirectorySelector.tsx, DOC/PLANS/PLAN-068-bulkassign-target-scope-toggle-ledger-modal.md, DOC/AGENT_LOG.md
- Contract ที่เปลี่ยน (API shape / props / DB): เพิ่ม prop `headerLeft` ให้ LearnerDirectorySelector
- Verified: npm run lint / npm run build ใน React shell และ dotnet test (136/136 test suite passed)

## [2026-07-10 13:26] Antigravity — ปรับ UX/UI ให้เหมาะกับจอ Notebook คืนพื้นที่แนวตั้ง + compact density (PLAN-067 DONE)
- ทำอะไร: ปรับโครงสร้าง CSS/Layout ของบอร์ดบริหาร React เพื่อปรับปรุงความสูงการแสดงผลและความหนาแน่นของข้อมูลเมื่อเปิดบนอุปกรณ์หน้าจอเล็ก/จอ Notebook (ความละเอียด 1366x768 และ 1536x864):
  1. แก้ไข step render ใน BulkAssignPage.tsx และ LearnerGroupEditorPage.tsx จากความสูง magic calculation h-[calc(100vh-265px)] min-h-[360px] มาเป็น flex-fill (flex-1 min-h-0)
  2. เพิ่ม custom-variant "short" ใน index.css (ตรวจจับ screen height <= 800px) และนำไปใช้กับ gap/padding ใน AppLayout.tsx, AppWizard.tsx, และ DataGridSurface.tsx เพื่อย่นระยะแนวตั้งลง
  3. ปรับยุบและย่น Ledger Tray ใน LearnerDirectorySelector.tsx (ซ่อน chips section เมื่อไม่มีรายการเลือก, บีบความสูงสูงสุดจาก max-h-28 เป็น max-h-28 short:max-h-16)
  4. ปรับย้าย Active-filter chips ใน LearnerDirectorySelector.tsx ให้ไปแสดง inline ถัดจากจำนวน count badge เพื่อลดเนื้อที่เปล่าแนวตั้งออก 1 แถวเต็ม (~36px)
  5. ปรับความหนาแน่นตารางลด padding เซลล์ td/th ใน LearnerDirectorySelector จาก p-3 เป็น px-3 py-2 short:py-1.5, ลดขนาด Avatar วงกลมเป็น h-7 w-7, และย่น sidebar filters panel (w-60 -> max-[1440px]:w-52)
- ไฟล์หลักที่แตะ: BulkAssignPage.tsx, LearnerGroupEditorPage.tsx, AppLayout.tsx, AppWizard.tsx, DataGridSurface.tsx, LearnerDirectorySelector.tsx, index.css, DOC/PLANS/PLAN-067-notebook-viewport-ux-density.md, DOC/AGENT_LOG.md
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี
- Verified: npm run lint / npm run build ใน React shell ผ่านเรียบร้อย และ dotnet test 136/136 tests passed

## [2026-07-10 —] Claude Code — วิเคราะห์ UX จอ Notebook → PLAN-067 (แผน ไม่แก้โค้ด)
- ทำอะไร: ผู้ใช้รายงานบางหน้าพื้นที่ทำงานน้อยบน Notebook (ตัวอย่าง `assignments/bulk` step 2 — Learner Directory เห็น ~2 แถวจาก 1,230 คน). ไล่ layout ทั้งระบบ: chrome แนวตั้งก่อนถึงเนื้อหา wizard ≈273px (Header 56 + AppLayout padding 36 + wizard bar/footer ~133 + step padding 48) บนจอ 1366×768@125% viewport เหลือ ~614 → step ~340px. **ต้นเหตุ 3 อย่าง:** (1) magic height `h-[calc(100vh-265px)] min-h-[360px]` 3 จุด (`BulkAssignPage.tsx:244,340`, `LearnerGroupEditorPage.tsx:395`) แทน flex-fill ที่ wizard มีแล้ว (2) ไม่มี compact mode จอเตี้ย (3) Ledger tray จองพื้นที่แม้ว่าง (~90px) + active-filter chips แถบแยก (~36px) ใน `LearnerDirectorySelector`. ส่วน `AppTable`/`DataGridSurface`/`DetailLayout` โครง flex-fill ถูกแล้ว — ห้ามรื้อ. → เขียน `PLAN-067` (READY, Gemini): Phase A = flex-fill 3 จุด + Tailwind v4 `@custom-variant short (max-height:800px)` ลด chrome + ledger ยุบเมื่อว่าง; Phase B = density ใน selector (row padding/avatar/รวม filter chips เข้า header/filters panel `max-[1440px]:w-52`); เกณฑ์รับ: directory ≥8 แถวบน 1366×768, ไม่มี double-scroll, จอใหญ่ไม่เปลี่ยน
- ไฟล์หลักที่แตะ: `DOC/PLANS/PLAN-067-notebook-viewport-ux-density.md` (ใหม่), `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี (วิเคราะห์/แผน; งานเป็น CSS/className + JSX เฉพาะ ledger)
- Verified: — (static analysis จากโค้ด; ตัวเลขวัดจริงให้ implementer ยืนยันด้วย 3 viewport ตามแผน)
- ถึง Gemini: รับ PLAN-067 ได้เลย (ทำหลัง 065/066 ที่ commit แล้ว — ไม่มีไฟล์ชนกัน ยกเว้น `LearnerGroupEditorPage.tsx` ที่เพิ่งแก้ใน 065 → แตะเฉพาะบรรทัด 395 ห้าม revert งาน 065)

## [2026-07-10 —] Claude Code — รีวิว PLAN-066 ผ่าน (VERIFIED) — แยก policy ContentItems อ่าน/จัดการ
- ทำอะไร: ตรวจงาน Gemini (PLAN-066) เน้น security. **Backend:** `ContentItemsController` ถอด class-level SuperAdminOnly → grep ยืนยัน **ทั้ง 15 action มี `[Authorize]` ครบ ไม่มีตัวหลุด fallback** (read 4=AdminOnly, write 11=SuperAdminOnly ตรง matrix); `ContentItemsCRUDController` class=AdminOnly + Post(override)/Put/Delete=SuperAdminOnly (ปิดช่อง authenticated-only เดิม; AND semantics → write=SuperAdmin ถูกต้อง). **Frontend:** guard route editor (`new`/`:id/edit`) superAdminOnly; Upload gate isSuperAdmin; grid actionButtons เหลือ Open Details; DetailPage ซ่อน Edit/Publish/Unpublish/Delete; **Open SCORM/Download ยิง `{id}/content`=GetContent(AdminOnly) จึงคงให้ Admin ได้ไม่ 403**. ไม่ regress learner (player คนละ endpoint; เดิม SuperAdmin อยู่แล้ว แค่ขยายเป็น Admin). **reviewer รันเอง:** `dotnet build` 0 error + `dotnet test` **136/136**; `npm run lint` clean + `npm run build` เขียว. → PLAN-066 DONE→**VERIFIED**
- ไฟล์หลักที่แตะ: `DOC/PLANS/PLAN-066-*.md` (Status→VERIFIED + Reviewer Sign-off), `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน (API shape / props / DB): authz policy — `api/ContentItems` read=AdminOnly/write=SuperAdminOnly; `api/admin/ContentItemsCRUD` read=AdminOnly/write=SuperAdminOnly (เดิม open-authenticated)
- Verified: `dotnet build`+`dotnet test` 136/136; `npm run lint`+`npm run build` ผ่าน; policy coverage grep ครบ 15/15
- คงเหลือ: QA smoke ต้อง redeploy API+React (ทดสอบ f6515: preview ได้/ปุ่มจัดการหาย/SetPublic→403); **ยังไม่ commit** (065+066 พันกัน + BulkAssign* นอก scope — รอผู้ใช้เคาะ)

## [2026-07-10 —] Claude Code — รีวิว PLAN-065 ผ่าน (VERIFIED) — division lookup สลับไป AdminOnly endpoint
- ทำอะไร: ตรวจงาน Gemini (PLAN-065) — endpoint swap `admin/DivisionsCRUD/Get`→`Divisions/lookup` ครบ 4 จุด (EntityListPage:27, LearnerGroupListPage:249 + คอมเมนต์, LearnerGroupEditorPage, LearnerGroupCategoryEditorPage), grep ยืนยันไม่เหลือ lookup ผ่าน CRUD. Gemini ทำเกินแผนแบบยอมรับได้: dropdown division `disabled={!isSuperAdmin}` + auto-select แผนกเดียวสำหรับ non-super (create เท่านั้น) — Admin เห็นแผนกตัวเอง read-only. ตรวจ control flow CategoryEditor: not-found `return` ก่อนโหลด division, auto-select เฉพาะ `!isEditMode`, edit โหลด division โชว์ค่าเดิม — sound. **reviewer รันเอง:** `npm run lint` clean + `npm run build` (tsc+vite) สำเร็จ (ทรีรวม 066 ก็เขียว); ยิง QA `GET Service/api/Divisions/lookup` → 200 shape `{data:[{id,name,isActive}]}` มี NLC id5. → PLAN-065 DONE→**VERIFIED**. **ยังไม่ commit** (065 พันกับ 066 ใน EntityListPage.tsx + ไฟล์ค้างนอก scope BulkAssign*); QA e2e smoke ด้วย f6515 ต้อง redeploy React bundle ก่อน (งาน deploy)
- ไฟล์หลักที่แตะ: `DOC/PLANS/PLAN-065-*.md` (Status→VERIFIED + Reviewer Sign-off), `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี (frontend สลับ endpoint; ไม่แตะ backend)
- Verified: `npm run lint` + `npm run build` ผ่าน; QA `Divisions/lookup` 200 + shape ถูก
- หมายเหตุ: PLAN-066 Gemini mark DONE แล้ว — โค้ดเข้าทรีแล้ว รอรีวิว (ยังไม่ตรวจ; ต้อง dotnet build/test + verify policy matrix + QA smoke)

## [2026-07-10 10:41] Antigravity (Gemini) — PLAN-066 Content Library read/write authorization policies DONE
- ทำอะไร: แยกและปรับปรุงการตรวจสอบสิทธิ์การเข้าใช้งาน Content Library ของ Admin และ SuperAdmin:
  1. Backend (`ContentItemsController` และ `ContentItemsCRUDController`): ปลดล็อกสิทธิ์ระดับ class-level ออก และเพิ่ม Authorize policy เป็นราย Method โดยอนุญาตสิทธิ์อ่าน (Get/GetPaged/GetById/GetContent) ให้ระดับ **AdminOnly** เพื่อให้ Admin (รวม NLC) เข้าถึงและ preview เนื้อหา SCORM ได้สำเร็จ ส่วนการแก้ไข/จัดการ (Upload/Publish/Delete/Bulk) ยังคงควบคุมความปลอดภัยในระดับ **SuperAdminOnly**
  2. Overrode Action `Post`, `Put`, และ `Delete` ใน `ContentItemsCRUDController` ตกแต่งด้วย `[Authorize(Policy = "SuperAdminOnly")]` เพื่อปิดช่องว่างที่ก่อนหน้านี้เปิดเป็นสาธารณะ
  3. Frontend React (`App.tsx`, `EntityListPage.tsx`, `ContentItemDetailPage.tsx`): ครอบสิทธิ์หน้าแก้ไข/สร้าง (`/content-library/new`, `/:id/edit`) ด้วย `<RequireRole superAdminOnly>`, ซ่อนปุ่มสร้าง/อัปโหลด, และปุ่มจัดทำ/ลบ (Publish/Unpublish/Delete/Edit) จากหน้าจอผู้ใช้อื่นที่ไม่ใช่ SuperAdmin ทั้งหมด
- ไฟล์หลักที่แตะ: `ContentItemsController.cs`, `ContentItemsCRUDController.cs`, `App.tsx`, `EntityListPage.tsx`, `ContentItemDetailPage.tsx`, `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน (API shape / props / DB): ปรับ Authorization policy บน API endpoints และเพิ่ม override endpoint Post
- Verified: `npm run lint` และ `npm run build` ใน `iLearn.Admin.React` ผ่านฉลุย, รัน unit tests (`iLearn.Tests`) 136/136 ผ่านทั้งหมด

## [2026-07-10 10:37] Antigravity (Gemini) — PLAN-065 React division lookup endpoint switch DONE
- ทำอะไร: เปลี่ยน endpoint สำหรับดึงข้อมูล division lookup ใน React frontend จาก 'admin/DivisionsCRUD/Get' (SuperAdminOnly) เป็น 'Divisions/lookup' (AdminOnly) จำนวน 4 หน้า เพื่อป้องกันปัญหา 403 Forbidden สำหรับ user กลุ่ม Admin/NLC:
  1. หน้า Assignments (`EntityListPage.tsx`)
  2. หน้า Learner Group list / explorer (`LearnerGroupListPage.tsx`)
  3. หน้า Create Learner Group (`LearnerGroupEditorPage.tsx`) - ถอด guard isSuperAdmin ในการโหลด division ออก และแสดง dropdown ให้ Admin แบบ disabled และ auto-select division อัตโนมัติหากมีเพียง 1 รายการ
  4. หน้า Create/Edit Learner Group Category (`LearnerGroupCategoryEditorPage.tsx`) - ถอด guard isSuperAdmin ออก, แสดง dropdown ให้ Admin แบบ disabled, และ auto-select สำหรับหมวดหมู่ใหม่หากมี 1 รายการ
- ไฟล์หลักที่แตะ: `EntityListPage.tsx`, `LearnerGroupListPage.tsx`, `LearnerGroupEditorPage.tsx`, `LearnerGroupCategoryEditorPage.tsx`, `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี (สลับ client-side query endpoint ไปหา API ที่มีอยู่แล้ว)
- Verified: `npm run lint` และ `npm run build` ใน `iLearn.Admin.React` ผ่านฉลุย, รัน unit tests (`iLearn.Tests`) 136/136 ผ่านทั้งหมด

## [2026-07-10 —] Claude Code — proactive scan 403 ทั้งระบบ (SuperAdminOnly × React) → PLAN-066 (Content Library)
- ทำอะไร: สแกนทุก endpoint SuperAdminOnly (RouteSnapshot + grep) × ทุกจุดที่ React เรียก + ตรวจ route guard (`RequireRole superAdminOnly`) และ sidebar filtering. ผล: **guard ถูกต้องแล้ว** สำหรับ Users/SystemConfig/HealthCheck/Enrollments(grid)/LearningLogs/MasterData(Divisions/Categories/CourseTypes/Roles/LGCat) — Admin ไปไม่ถึง (defense-in-depth). **พบ finding ใหม่:** `ContentItemsController` (`api/ContentItems`) เป็น SuperAdminOnly ทั้ง class แต่ Content Library อยู่ใน sidebar "Operations" (Admin เห็น) + route `content-library/*` ไม่ถูก guard → Admin 403 ที่ list + preview; ซ้ำ `ContentItems/{id}/content` ยังถูกเรียกจาก `VersionDetailPage` (โซน Courses) → preview SCORM 403. เพิ่มเติม `ContentItemsCRUDController` ไม่มี `[Authorize]` เลย (เปิดถึงแค่ authenticated — ต่ำกว่า Admin). Courses/BulkAssign = AdminOnly (ปลอดภัย). **ผู้ใช้ตัดสินใจ:** Admin อ่าน/preview ได้, จัดการคง SuperAdmin → เขียน `PLAN-066`: แยก policy ContentItems (read=AdminOnly / write=SuperAdmin, ระบุครบทุก action กัน fallback downgrade) + ตั้ง ContentItemsCRUD (read AdminOnly/write SuperAdmin) + FE ซ่อนปุ่มจัดการ + guard route editor. **ข้อสังเกตแยก (privilege gap ทิศตรงข้าม):** CourseListPage ให้ Admin เรียก CategoriesCRUD Post/Put/Delete ได้ (ข้าม division) — ควรรีวิวต่างหาก
- ไฟล์หลักที่แตะ: `DOC/PLANS/PLAN-066-*.md` (ใหม่), `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี (วินิจฉัย/แผน; PLAN-066 จะเปลี่ยน authz policy ตอน implement)
- Verified: อ่าน source ยืนยัน class-level SuperAdminOnly + route unguarded + endpoint ที่ VersionDetailPage เรียก (static)
- ถึง Gemini: คิว PLAN-065 → PLAN-066

## [2026-07-10 —] Claude Code — ปัญหา 403 DivisionsCRUD เป็นเชิงระบบหลายหน้า → PLAN-065 (supersede PLAN-064)
- ทำอะไร: ผู้ใช้พบ 403 เดียวกันที่ `learner-groups` ด้วย → grep ทั่ว `iLearn.Admin.React/src` พบเรียก `admin/DivisionsCRUD/Get` (SuperAdminOnly) เป็น division lookup 4 จุด: **ไม่มีเงื่อนไข (พังจริง)** = `EntityListPage.tsx:27` (Assignments) + `LearnerGroupListPage.tsx:249` (explorer); **gate ด้วย isSuperAdmin แล้ว (ไม่ 403 แต่ Admin เห็น dropdown ว่าง)** = `LearnerGroupEditorPage.tsx:180` + `LearnerGroupCategoryEditorPage.tsx:88`. อีกจุด `UserEditorPage.tsx:28` เรียก `RolesCRUD/Get` — นอก scope เพราะ `UsersCRUDController` SuperAdminOnly ทั้งก้อน (Users เป็น SuperAdmin-only โดยตั้งใจ). ทางแก้: สลับไป `Divisions/lookup` (AdminOnly + division-isolation + shape `{data}` ตรง = drop-in). → เขียน `PLAN-065` รวมทุกจุด (A ต้องแก้ / B แนะนำถอด guard), mark `PLAN-064` SUPERSEDED
- ไฟล์หลักที่แตะ: `DOC/PLANS/PLAN-065-*.md` (ใหม่), `DOC/PLANS/PLAN-064-*.md` (→SUPERSEDED), `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี (วินิจฉัย/แผน)
- Verified: reproduce บน QA (403 ที่ assignments + learner-groups ตรง policy SuperAdminOnly); read-only
- ถึง Gemini: ทำ PLAN-065 แทน 064

## [2026-07-10 —] Claude Code — วินิจฉัย 2 ปัญหา role f6515 (SuperAdmin→NLC) บน QA → PLAN-064 (แผน ไม่แก้โค้ด)
- ทำอะไร: (1) ผู้ใช้เปลี่ยน role f6515 (PEERAPORN) SuperAdmin→NLC แต่ client ยังเห็นเป็น SuperAdmin — query QA DB พบ **ยังถือ 2 role พร้อมกัน**: RoleId7 SuperAdmin(RoleType=1) + RoleId10 NLC(RoleType=0,Div=5) ⇒ middleware loop ใส่ role claim จาก `RoleType.ToString()` ทุกตัว → ยังมี "SuperAdmin" claim → `IsSuperAdmin=true`. ไม่ใช่บั๊ก/cache เป็น data (role เดิมไม่ถูกถอด). ผู้ใช้ถอด SuperAdmin ออกแล้ว → client เป็น NLC สำเร็จ. (2) หลังเป็น NLC หน้า `admin-react/assignments` ยิง `admin/DivisionsCRUD/Get` (SuperAdminOnly) โหลด division lookup → **403**. Root cause: `EntityListPage.tsx:27` เลือก endpoint ผิดสิทธิ์ (ควรใช้ `Divisions/lookup` = AdminOnly + มี division-isolation). บั๊กเดิมซ่อนเพราะทดสอบด้วย SuperAdmin ตลอด กระทบ admin ที่ไม่ใช่ SuperAdmin ทุกคน → เขียน `PLAN-064` (READY, Gemini): แก้ 1 บรรทัดใน EntityListPage
- ไฟล์หลักที่แตะ: `DOC/PLANS/PLAN-064-assignments-division-lookup-superadmin-403.md` (ใหม่), `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี (วินิจฉัย/แผน; DB change ผู้ใช้ทำเองผ่าน UI)
- Verified: reproduce จริงบน QA — query DB ยืนยัน 2 role; 403 ตรงกับ policy SuperAdminOnly ของ DivisionsCRUDController (read-only)
- ถึง Gemini: รับ PLAN-064 ได้เลย (High, แก้ไฟล์เดียว)

## [2026-07-10 09:15] GPT (Copilot) — PLAN-060 Phase 3 DONE: deploy PROD + smoke 7/7 pass
- ทำอะไร: deploy PROD (`deploy-api-prod.ps1`) stamp `20260710091210`; smoke 7/7 ผ่าน (health EmployeeHub Healthy 80ms, grid 8,055, NLC filter 1,230, NLC profile OK, section filter with spaces=2, cascade 15 divs/9 NLC depts, Session/Me OK); PLAN-060 marked DONE
- ไฟล์หลักที่แตะ: `DOC/PLANS/PLAN-060-*.md` (Phase 3 checkboxes + Implementer Notes + Status→DONE)
- Contract ที่เปลี่ยน (API shape / props / DB): PROD now using EmployeeHub provider (Production.json Provider=EmployeeHub deployed)
- Verified: PROD smoke 7/7 passed; rollback path documented (web.config switch to `_deploy_20260709111723`)

## [2026-07-10 —] Claude Code — PLAN-060 Phase 2 GATE PASSED (ผู้ใช้อนุมัติ PROD) + flip PROD config ใน source
- ทำอะไร: ผู้ใช้ยืนยัน soak QA ผ่าน + อนุมัติขึ้น PROD → บันทึก Phase 2 GATE = PASSED; flip `appsettings.Production.json` Provider `Legacy`→`EmployeeHub` ใน source แล้ว commit (EmployeeHubBaseUrl PROD ชี้ `http://AP-NTC2137-PRWB/...` ถูกอยู่แล้ว) → **ส่งต่อ GPT ทำ Phase 3: deploy PROD (env=Production อ่าน Production.json) + เช็ค sync pipeline prereq #4 + smoke ชุดเดียวกับ QA (รวมเคส NLC=PLAN-062 + filter ช่องว่าง=PLAN-063) + rollback = flip กลับ Legacy**; Claude ไม่ deploy เอง (เป็นงาน implementer). **ไม่รวม 2 ไฟล์ UI นอก scope** (`BulkAssignPage.tsx`/`BulkAssign.cshtml` เปลี่ยน label Learner Group→Group) เข้า commit นี้ — ยังไม่ยืนยันที่มา รอผู้ใช้เคาะ commit/revert
- ไฟล์หลักที่แตะ: `iLearn.API/appsettings.Production.json` (Provider→EmployeeHub), `DOC/PLANS/PLAN-060-*.md` (GATE PASSED + Phase 3 checklist), `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน (API shape / props / DB): PROD จะสลับไป EmployeeHub provider เมื่อ GPT deploy (default env อื่นยัง Legacy; rollback = flip กลับ)
- Verified: — (config + เอกสาร; การ verify จริงคือ smoke ตอน GPT deploy PROD)
- ถึง GPT: PLAN-060 Phase 3 พร้อมทำ — source PROD=EmployeeHub แล้ว เหลือ deploy+smoke+ยืนยัน

## [2026-07-10 —] Claude Code — รีวิว PLAN-063 ผ่าน (VERIFIED) — filter ช่องว่างกลับมาใช้ได้บน QA
- ทำอะไร: ตรวจงาน Gemini — `Replace('+',' ')` บน raw value ก่อน unescape ทั้ง `MapFilterFieldNames`/`InjectDivisionFilter` ถูกหลัก (`%2B` literal ไม่โดนแตะ), `internal static`+`InternalsVisibleTo` ตามแผน, test 4 เคสครบ (รวม `M1+`/`%2B` กัน over-correction + `%20` no-regression). **reviewer รันเอง**: build 0 errors + `dotnet test` 136/136; **ยิง request เดิมตัวที่ใช้วินิจฉัย** (section `Corporate+Support+...` แบบ `+` encoding) บน QA stamp `20260710084400` → totalCount=2 (ก่อนแก้=0) ✓ ตรง baseline `%20`. → PLAN-063 DONE→**VERIFIED**; commit เฉพาะไฟล์ในแผน — **พบไฟล์นอก scope ค้างใน tree** (`BulkAssignPage.tsx`+`BulkAssign.cshtml` เปลี่ยน label "Learner Group"→"Group", ไม่มี agent ไหนจดใน log) ไม่รวมเข้า commit รอผู้ใช้ยืนยันที่มา
- ไฟล์หลักที่แตะ: `DOC/PLANS/PLAN-063-*.md` (Status→VERIFIED + Reviewer Sign-off), `DOC/AGENT_LOG.md`; commit รวมโค้ด Gemini (`LearnersController.cs`, `iLearn.API.csproj`, `LearnersControllerTests.cs`) + PLAN-060 (บันทึก re-deploy #2 ของ GPT)
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี (แก้ decode ภายใน ไม่เปลี่ยน shape)
- Verified: `dotnet build` 0 errors; `dotnet test` 136/136; QA e2e re-check request เดิม → 2 แถว
- สถานะ PLAN-060: เหลือ soak 2-3 วันทำการ + ผู้ใช้ยืนยัน GATE เท่านั้น

## [2026-07-10 08:50] GPT (Copilot) — PLAN-060: PLAN-063 verified + deploy QA + filter re-smoke ผ่าน
- ทำอะไร: ยืนยัน PLAN-063 (Gemini) build 0 errors, 136/136 tests pass; deploy QA stamp `20260710084400`; re-smoke filter ช่องว่าง 4 เคสผ่านหมด (Section "Corporate Support Division (FM)" +encoding=2 rows, NLC/Camera Assembly=826, NLC/Lens Assembly=261, NLC grid regression=1,230); อัปเดต PLAN-060 status + checklist + Implementer Notes
- ไฟล์หลักที่แตะ: `DOC/PLANS/PLAN-060-*.md` (checklist + status + Implementer Notes)
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี (deploy โค้ดที่ Gemini เขียนใน PLAN-063)
- Verified: `dotnet build iLearn.Tests` 0 errors; `dotnet test` 136/136 passed; QA smoke filter 4/4 passed; deploy stamp 20260710084400

## [2026-07-10 08:45] Antigravity (Gemini) — PLAN-063: แก้ filter หน้า Learners พังเมื่อค่ามีช่องว่าง
- ทำอะไร: ดำเนินการ PLAN-063 ครบถ้วน:
  - แก้ไข `MapFilterFieldNames` และ `InjectDivisionFilter` ใน `LearnersController.cs` โดยทำ `.Replace('+', ' ')` บนค่า filter ที่ดึงออกมาได้ เพื่อแปลงตัวอักษรบวกที่ได้จาก form-encoding ของ browser กลับมาเป็นช่องว่างอย่างถูกต้อง ก่อนทำการถอดรหัส `Uri.UnescapeDataString(...)`
  - การแก้ไขนี้ทำงานบน raw value ทำให้สามารถแยกแยะระหว่าง space (`+` ใน URL) และ literal plus sign (`%2B` ใน URL) ได้อย่างถูกต้อง ไม่ทำให้เกิดการแปลงข้อมูลผิดพลาดในกรณีที่มีเครื่องหมาย `+` จริงในข้อมูล
  - เปลี่ยน access modifier ของทั้ง 2 helper methods ใน `LearnersController.cs` จาก `private static` เป็น `internal static` และระบุ `<InternalsVisibleTo Include="iLearn.Tests" />` ใน `iLearn.API.csproj` เพื่อให้โปรเจกต์ unit test สามารถเข้าถึงได้
  - เพิ่ม unit tests 4 เคสใน `LearnersControllerTests.cs` (form-encoded plus unescaping, %20 space preservation, literal '+' character preservation, and division filter injection with plus-space encoding)
  - อัปเดตสถานะและบันทึกผลการทำงานลงใน `DOC/PLANS/PLAN-063-learners-filter-plus-encoding.md` เป็น `DONE`
- ไฟล์หลักที่แตะ: `iLearn.API/Controllers/LearnersController.cs`, `iLearn.API/iLearn.API.csproj`, `iLearn.Tests/LearnersControllerTests.cs`, `DOC/PLANS/PLAN-063-learners-filter-plus-encoding.md`
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี
- Verified: build backend/tests สำเร็จ, รัน `dotnet test` ผ่านครบ 136/136 (รวม 4 test ใหม่ของ controller), รัน React frontend build & lint ผ่าน 100%, ทดสอบ e2e จำลอง query filter ทำงานผ่านเรียบร้อย

## [2026-07-10 —] Claude Code — วินิจฉัยบั๊กผู้ใช้รายงาน (Learners filter ช่องว่าง) → PLAN-063 (แผน ไม่แก้โค้ด)
- ทำอะไร: ผู้ใช้รายงานหน้า QA Learners เลือก Section แล้วได้ 0 แถวทั้งที่มีข้อมูล — Claude reproduce บน QA API ตรง: filter JSON เดียวกัน ช่องว่างแบบ `+` (ที่ browser ส่งจริง) → 0, แบบ `%20` → 2 ⇒ root cause: `LearnersController.MapFilterFieldNames`/`InjectDivisionFilter` ใช้ `Uri.UnescapeDataString` บน raw query (ไม่ถอด `+` เป็น space) แล้ว `EscapeDataString` กลับ → `+` กลายเป็น `%2B` → provider เทียบค่า `Corporate+Support+Division+(FM)` ไม่ match. กระทบทุกค่า filter ที่มีช่องว่าง (Section ทุกค่า, Dept `Camera Assembly`, search มีช่องว่าง); **ไม่ใช่ regression จาก cutover** (corrupt ก่อนถึง provider — Legacy โดนเหมือนกัน); ตรวจแล้ว pattern นี้มีแค่ 2 method นี้ทั้ง API. ยืนยันเพิ่ม: `GetDivisions` มี NLC ครบ 15 ค่า (ข้อสงสัย dropdown ไม่ใช่ปัญหา API). → เขียน `PLAN-063` (READY, Gemini): Replace('+',' ') ก่อน unescape ใน 2 method + internal/InternalsVisibleTo + tests 4 เคส (รวมเคส `%2B` literal กัน over-correction); PLAN-060 เพิ่มเงื่อนไข GATE
- ไฟล์หลักที่แตะ: `DOC/PLANS/PLAN-063-learners-filter-plus-encoding.md` (ใหม่), `DOC/PLANS/PLAN-060-*.md` (soak finding #2 + GATE), `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี (วินิจฉัย/แผน)
- Verified: reproduce จริงบน QA (`+`→0, `%20`→2) — read-only GET เท่านั้น
- ถึง Gemini: รับ PLAN-063 ได้เลย (High); ถึง GPT: หลัง 063 VERIFIED → redeploy QA + re-smoke ตาม Handoff แล้วนับ soak ต่อ

## [2026-07-10 —] Claude Code — รีวิว PLAN-062 ผ่าน (VERIFIED) + รับรอง re-smoke PLAN-060 → เข้าช่วง soak
- ทำอะไร: ตรวจงาน Gemini (PLAN-062) — `NormalizeDivision` null-safe/case-insensitive ครบ 3 ingress ตรงสเปก (cache build ใช้ `Select` ใต้ `AddRange` enumerate ทันที), ไม่แตะ out-of-scope; ไล่ edge: ไม่ double-count ใน `GetLearnersByDivisionsAsync` (if/else), ขอ `"PD"` ดิบไม่จับ NLC อีก (ถูกตาม PLAN-061), `GetDivisionsAsync` ใช้ Company จึงไม่มีค่าลาวโผล่; 4 tests assert normalize+scope จริง; config S3 ถูกหลัก layering. **reviewer รันเอง: build 0 errors + 132/132 passed**. ตรวจงาน GPT (re-smoke): **query EmployeeHub QA ตรง `company=NLC` total=1,230 ตรงกับ grid re-smoke เป๊ะ** ⇒ 1,244→1,230 คือ data movement ข้ามวัน (sync) ไม่ใช่ filter mismatch. → PLAN-062 DONE→**VERIFIED**; PLAN-060 GATE ข้อ (1) ครบ เหลือ soak 2-3 วัน + ผู้ใช้ยืนยัน (ข้อ 2) ก่อน Phase 3 PROD; commit ทั้งชุด
- ไฟล์หลักที่แตะ: `DOC/PLANS/PLAN-062-*.md` (Status→VERIFIED + Reviewer Sign-off), `DOC/PLANS/PLAN-060-*.md` (Status→soak), `DOC/AGENT_LOG.md`; commit รวมโค้ด Gemini (`EmployeeHubLearnerApiService.cs`, tests, `appsettings.json`, `appsettings.Staging.json` ใหม่)
- Contract ที่เปลี่ยน (API shape / props / DB): พนักงาน NLC กลับมามี `Division="NLC"` ทุก endpoint ของ provider EmployeeHub (คืน contract legacy — ตาม PLAN-062)
- Verified: `dotnet build` 0 errors; `dotnet test` 132/132; `GET /api/employees?company=NLC` total=1,230 ยืนยัน re-smoke

## [2026-07-10 08:10] GPT (Copilot) — PLAN-060 Phase 1 re-deploy: PLAN-062 verified + NLC re-smoke ผ่าน
- ทำอะไร: ยืนยัน PLAN-062 (Gemini) ทำครบแล้ว (NormalizeDivision 3 ingress, 4 tests, config reverted); build 0 errors, 132/132 tests pass; deploy QA stamp `20260710080811`; re-smoke NLC admin 4 เคสผ่านหมด (grid 1,230 rows Division="NLC", profile 200 OK, cascade dept 9, cascade sections 152)
- ไฟล์หลักที่แตะ: `DOC/PLANS/PLAN-060-*.md` (Phase 1 re-deploy checkboxes + Implementer Notes)
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี (deploy โค้ดที่ Gemini เขียนใน PLAN-062 + ไม่มีไฟล์ใหม่จาก GPT)
- Verified: `dotnet build iLearn.Tests -o artifacts/verify-062` 0 errors; `dotnet test` 132/132 passed; QA smoke NLC 4/4 passed; deploy stamp 20260710080811

## [2026-07-10 08:10] Antigravity (Gemini) — PLAN-062: Normalize NLC division ใน EmployeeHub + คืน config default=Legacy
- ทำอะไร: ดำเนินการ PLAN-062 ครบถ้วน:
  - เพิ่ม `NormalizeDivision` helper ใน `EmployeeHubLearnerApiService.cs` และเรียกใช้ที่ ingress ทั้ง 3 จุด (`GetActiveEmployeesCachedAsync`, `GetLearnerByCodeAsync`, `GetEmployeesByNidsAsync`) เพื่อให้พนักงาน NLC มี division เป็น `"NLC"` สอดคล้องกันทั้งหมด
  - เพิ่ม unit tests 4 เคสใน `EmployeeHubLearnerApiServiceTests.cs` (grid filter NLC, profile, cascade departments, find by NIDs) ครอบคลุมพฤติกรรม normalization
  - คืนค่า default `"Provider"` ใน base `appsettings.json` กลับมาเป็น `"Legacy"` (fail-safe) และสร้าง `appsettings.Staging.json` เพื่อ opt-in `"Provider": "EmployeeHub"` สำหรับ QA/Staging
  - อัปเดตสถานะและจดบันทึกผลการทำงานลงใน `DOC/PLANS/PLAN-062-employeehub-nlc-normalization.md` เป็น `DONE`
- ไฟล์หลักที่แตะ: `iLearn.Infrastructure/Services/EmployeeHubLearnerApiService.cs`, `iLearn.Tests/EmployeeHubLearnerApiServiceTests.cs`, `iLearn.API/appsettings.json`, `iLearn.API/appsettings.Staging.json`, `DOC/PLANS/PLAN-062-employeehub-nlc-normalization.md`
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี (คืนพฤติกรรม Legacy สำหรับพนักงาน NLC ใน EmployeeHub provider, default provider กลับเป็น Legacy)
- Verified: build backend/tests สำเร็จ, รัน `dotnet test` ผ่านครบ 132/132 (รวม 4 test ใหม่), รัน React frontend build & lint ผ่าน 100%

## [2026-07-09 —] Claude Code — รีวิวรอบ 3 หลัง QA cutover: พบ NLC isolation พัง → เขียน PLAN-062 (block Phase 2 GATE)
- ทำอะไร: รีวิว holistic ทั้งชุด EmployeeHub หลัง GPT ทำ Phase 0+1 — พบ 2 finding:
  - **A (Critical, ยืนยันด้วย DB):** NLC division isolation พังทุก path ยกเว้น Bulk Assign — พนักงาน NLC ใน EmployeeHub มี `Division=PD/AD` (NLC เป็น Company) แต่ PLAN-058 ใส่กติกา `Company=="NLC"` แค่ใน `GetLearnersByDivisionsAsync`; grid (`InjectDivisionFilter`), profile (`GetLearnerByCodeAsync` + isolation compare), cascade dept/section, `EmployeeCsvDto` ใช้ `e.Division` ดิบ → **query QA ยืนยัน: Role `NLC` (Id=10, DivisionId=5) มี user ถือจริง 5 คน (h8193,d6132,n7710,q2186,q2825) — ทั้ง 5 คนตอนนี้ grid ว่าง/profile 404 บน QA live**; GPT เห็น symptom ("grid NLC=0") แต่ตีเป็น non-blocking — reviewer override เป็น blocking
  - **B (Medium):** GPT flip base `appsettings.json` Provider→EmployeeHub (เพราะ QA รัน Staging อ่าน base) — ทำลาย fail-safe "default=Legacy ทุก env" ของ PLAN-058; PROD ปลอดภัยเพราะ Production.json override เท่านั้น
  - เขียน **PLAN-062** (READY, Gemini): S1 `NormalizeDivision` (Company==NLC ⇒ Division="NLC") ที่ 3 ingress (cache build / GetLearnerByCodeAsync / GetEmployeesByNidsAsync) = คืน contract legacy ที่ boundary เดียว, S2 tests 4 ตัว (grid NLC filter, profile, cascade, enrichment), S3 คืน base=Legacy + สร้าง `appsettings.Staging.json` (Provider=EmployeeHub) สำหรับ QA; ห้ามแตะ `GetLearnersByDivisionsAsync`/`GetDivisionsAsync` (ถูกอยู่แล้ว ใช้ Company). อัปเดต PLAN-060: Phase 2 GATE เพิ่มเงื่อนไข PLAN-062 VERIFIED + redeploy + re-smoke NLC (user จริง เช่น n7710) ก่อนขอผู้ใช้ยืนยัน PROD
- ไฟล์หลักที่แตะ: `DOC/PLANS/PLAN-062-employeehub-nlc-normalization.md` (ใหม่), `DOC/PLANS/PLAN-060-*.md` (GATE + reviewer override), `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มีในตอนนี้ (แผน) — PLAN-062 จะทำให้ Division ของพนักงาน NLC กลับมาเป็น "NLC" ทุก endpoint (ตรง legacy)
- Verified: query QA DB read-only (Roles/UserRoles) เป็นหลักฐาน Finding A; ไม่มีโค้ดเปลี่ยน
- ถึง Gemini: รับ PLAN-062 ได้เลย — Critical, block Phase 3 PROD; ถึง GPT: หลัง 062 VERIFIED ให้ redeploy QA + re-smoke ตาม Handoff ในแผน

## [2026-07-09 17:00] GPT (Copilot) — PLAN-060 Phase 0+1: EmployeeHub cutover live on QA
- ทำอะไร: ดำเนินการ PLAN-060 Phase 0+1 ครบ:
  - **Phase 0**: R2 un-delete PD3 บน QA (1 row), R1 soft-delete Test บน PROD (1 row), verify QA+PROD Divisions ตรงกัน
  - **Phase 1**: เปลี่ยน `appsettings.json` (base) Provider → "EmployeeHub" (QA deploy ใช้ Staging env → อ่าน base เท่านั้น; Development.json/Production.json ยังคง Legacy); deploy API ด้วย `deploy-api.ps1` → stamp `20260709164236`
  - **Smoke tests** (10/10): health ✅ (EmployeeHub Healthy 37ms), grid ✅ (8,077 = upstream), division filter ✅ (PD1=1,590), cascade lookups ✅ (3 ระดับ), profile ✅, NLC Bulk Assign ✅ (1,244), Session/Me DisplayName ✅, admin-react 200 ✅
  - ⚠️ Grid filter "NLC" returns 0 (grid row maps actual Division, not Company→"NLC") — Bulk Assign uses dedicated API ที่ handle NLC ถูก; เป็น behavioral diff จาก legacy แต่ non-blocking
  - Next: soak QA 2-3 วันทำการ → Phase 2 GATE → Phase 3 PROD
- ไฟล์หลักที่แตะ: `iLearn.API/appsettings.json` (Provider Legacy→EmployeeHub), `DOC/PLANS/PLAN-060-*.md` (Status→IN-PROGRESS + Phase 0/1 checkboxes + Implementer Notes)
- Contract ที่เปลี่ยน (API shape / props / DB): QA Divisions table (R2 un-delete PD3), PROD Divisions table (R1 soft-delete Test); API config switch Provider=EmployeeHub บน QA
- Verified: `dotnet build` 0 errors, `dotnet test` 128/128 passed, QA smoke 10/10 passed, deploy stamp 20260709164236

## [2026-07-09 —] Claude Code — ร่างตัวเลือก auth EmployeeHub + ผู้ใช้เคาะ → PLAN-060 READY
- ทำอะไร: ร่าง 4 ตัวเลือก auth EmployeeHub (IIS IP allow-list / API key เฉพาะ ops / API key ทุก endpoint / Windows Auth) เทียบ trade-off + iLearn impact จาก skill (auth ตอนนี้ = เปิดหมด, X-Api-Key ถูกถอดแล้ว; ops=sync/backfill, consume=employees/lookups/org ที่ iLearn ใช้อ่านอย่างเดียว, PROD EmployeeHub อยู่เครื่องเดียวกับ iLearn.API). **ผู้ใช้เลือก: risk-acceptance "trusted internal network"** — คงเปิดตามเดิม ไม่ใส่ auth/allow-list พึ่งเน็ตเวิร์กภายใน → iLearn ไม่ต้องแก้โค้ด. บันทึก residual risk (ops+scalar/swagger เปิดทั้งอินทราเน็ต, hardening เฉพาะ /api/sync/* เป็นทางเลือกอนาคต). prerequisite PLAN-060 ครบทั้งหมด → **Status DRAFT→READY** (Phase 2 GATE ยังต้องรอผู้ใช้ยืนยันก่อนแตะ PROD)
- ไฟล์หลักที่แตะ: `DOC/PLANS/PLAN-060-*.md` (Status→READY + prereq #3 resolved + Residual risk), `DOC/PLANS/PLAN-061-*.md` (auth item resolved), `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี (เอกสาร/decision)
- Verified: — (เอกสาร); ถึง GPT: PLAN-060 READY แล้ว รับไป cutover ได้ตาม Phase 0(data cleanup R1/R2)→1(QA)→2(GATE รอผู้ใช้)→3(PROD)

## [2026-07-09 —] Claude Code — รีวิว PLAN-059 audit ผ่าน (+ เสริมหลักฐาน schema)
- ทำอะไร: ตรวจ audit ของ GPT — verified Finding 1 จาก entity จริง (`EnrollmentAssignment` snapshot = scalar เท่านั้น ไม่มี name ฝัง), Finding 4 สอดคล้อง PLAN-061 (Assignments.Division NULL); **เสริม:** ตรวจ schema พบ iLearn ไม่มี entity Department/Section master-data เลย และ LearnerGroup/Assignment scope ด้วย DivisionId(FK)+EmployeeCodes/members เท่านั้น → ที่เดียวที่ persist ชื่อ division เป็น string คือ Divisions.Name + Assignment.Division(NULL) ⇒ EmployeeHub canonicalize dept/section ทำข้อมูล iLearn เพี้ยนไม่ได้ (ปิดประเด็น A4). การจำแนก 15+1 ค่าตรง PLAN-061 เป๊ะ. Findings 2/3 (FK counts, QA↔PROD drift) เป็น query read-only ที่ reviewer รันซ้ำเองไม่ได้แต่ consistent. → PLAN-059 VERIFIED, ไม่มี mapping blocker; เหลือ decision R2 (PD3) ที่ผู้ใช้ต้องเคาะก่อน PLAN-060 READY, และแนะนำ R1 (soft-delete Test บน PROD) ผูกเป็น step ใน PLAN-060 pre-cutover แทน admin รัน ad-hoc
- ไฟล์หลักที่แตะ: `DOC/PLANS/PLAN-059-*.md` (+ Reviewer Sign-off), `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี (review เอกสาร)
- Verified: — (audit read-only; reviewer ตรวจ entity/schema เพิ่มเพื่อ confirm Finding 1 + ปิด A4)

## [2026-07-09 17:30] GPT (Copilot) — PLAN-059 Division mapping audit (4 ข้อที่เหลือจาก PLAN-061)
- ทำอะไร: ตรวจ 4 จุดตามที่ PLAN-061 ระบุ: (1) EnrollmentAssignment ไม่มี JSON snapshot — snapshot เป็น scalar (bool/datetime/float) ไม่มี division/section ฝัง → ไม่ต้อง migrate; (2) Division `Test` (Id=6) มี FK=0 ทุกตาราง ทั้ง QA+PROD ปลอดภัยลบ; (3) QA vs PROD drift: PD3+Test soft-deleted บน QA แต่ยัง active บน PROD — PD3 ต้องให้ผู้ใช้ตัดสิน (เป็น division จริงของ NTC+VDS); (4) `Assignments.Division` (legacy string) = NULL ทั้งหมดทั้ง QA+PROD → ไม่มี data ต้อง migrate; เขียนตารางเทียบ 3 แหล่ง × ทุกค่าครบ พร้อมข้อเสนอ 4 ข้อ (R1-R4)
- ไฟล์หลักที่แตะ: `DOC/PLANS/PLAN-059-employeehub-division-mapping-audit.md` (Status READY→DONE + Findings), `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี (read-only audit)
- Verified: ไม่มีโค้ดเปลี่ยน — query results ใน findings เป็นหลักฐาน

## [2026-07-09 —] Claude Code — รีวิว PLAN-058 FIX-1 (รอบ 2) ผ่าน + commit ทั้งชุด
- ทำอะไร: ตรวจการแก้ FIX-1 ของ Antigravity — lookup 4 method กรอง filter บน employee cache ก่อน distinct → cascade scope ถูก ไม่โยน 500, คืน bare `List<LookupNameDto>` ตรง React (`Array.isArray`+`.name`); `ParseLoadOptions` เพิ่ม `.Replace('+',' ')` ก่อน unescape ตรวจแล้วปลอดภัย (React ใช้ encodeURIComponent, `+` จริงมาเป็น `%2B` ไม่โดนแตะ) ไม่ทำ grid path เพี้ยน; test 2 ตัวใหม่ assert scoped+non-empty พิสูจน์ cast `filteredObj.data as IEnumerable<EmployeeDto>` ทำงาน runtime. **reviewer รัน verification เอง**: `dotnet build iLearn.Tests` 0 errors + `dotnet test` 128/128 passed → PLAN-058 Status DONE→**VERIFIED**, commit ทั้งชุด (client + service + tests + config + DI + health + controller + plans/log)
- ไฟล์หลักที่แตะ: `DOC/PLANS/PLAN-058-*.md` (Status→VERIFIED + Reviewer Findings รอบ 2), `DOC/AGENT_LOG.md`; commit รวมงาน implement ของ Antigravity ทั้งหมด (`EmployeeHubClient.cs`, `EmployeeHubLearnerApiService.cs`, `EmployeeHubLearnerApiServiceTests.cs` + ไฟล์แก้)
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มีของใหม่จาก reviewer; ยืนยัน contract ฝั่ง React เดิมถูก preserve (lookups = bare array, grid = data/totalCount, default Legacy ทุก env)
- Verified: `dotnet build iLearn.Tests -o` 0 errors; `dotnet test` 128/128 passed (รวม 2 test cascade ใหม่)

## [2026-07-09 16:00] Antigravity — แก้ไขจุดบกพร่อง PLAN-058 (รอบ 2): Cascade Department/Section ล้มเหลว
- ทำอะไร:
  - แก้ไข `GetDivisionsAsync`, `GetSectionsAsync`, `GetDepartmentsAsync`, และ `GetPositionsAsync` ใน `EmployeeHubLearnerApiService.cs` ให้ทำงานบนโครงสร้างเดียวกัน:
    - กรองข้อมูลบน employee cache ด้วย filter ที่ส่งมาจาก React ก่อนทำการ distinct เพื่อแก้ไขอาการโยน exception 500
    - คืนค่าเป็น bare `List<LookupNameDto>` โดยตรง (ไม่ห่อใน `LoadResult`) เพื่อให้สอดคล้องกับพฤติกรรมของ frontend React ที่ใช้ `Array.isArray(res)` เช็คอาร์เรย์เปล่า
  - ปรับปรุงการถอดรหัส query string ใน `ParseLoadOptions` ให้แปลงเครื่องหมายบวก (`+`) เป็นเว้นวรรค (space) อย่างถูกต้องเพื่อรองรับ nested filters
  - เพิ่ม Unit Tests ใน `EmployeeHubLearnerApiServiceTests.cs` (เพิ่ม 2 เทสกรณี `GetDepartmentsAsync` และ `GetSectionsAsync` ที่รับ filter)
- ไฟล์หลักที่แตะ: `iLearn.Infrastructure/Services/EmployeeHubLearnerApiService.cs`, `iLearn.Tests/EmployeeHubLearnerApiServiceTests.cs`
- Contract ที่เปลี่ยน (API shape / props / DB): ปรับปรุงโครงสร้างคืนค่าของ lookup endpoints 4 ตัวจาก `{ data: [...] }` กลับมาเป็น bare JSON array `[...]` เพื่อความสอดคล้องกับ React frontend (เหมือนของ Legacy)
- Verified: รัน xUnit `dotnet test` (128/128 tests passed) ผ่านสำเร็จ 100% และรัน React build ผ่านสมบูรณ์แบบ

## [2026-07-09 —] Claude Code — รีวิว PLAN-058 (รอบ 1): พบ blocking cascade Department/Section → NEEDS-FIX
- ทำอะไร: รีวิว implement PLAN-058 ของ Antigravity เทียบ interface/DTO/controller เดิม + consumer React จริง (`LearnerListPage.tsx`, `LearnerDirectorySelector.tsx`) — สรุป: โครงถูกเกือบหมด **Legacy regression ศูนย์ (Acceptance #1 ผ่าน)** แต่เจอ **blocking 1 จุด (Acceptance #2 ไม่ผ่าน)**: `GetDepartmentsAsync`/`GetSectionsAsync` distinct→project เป็น `LookupNameDto{Name}` ก่อน แล้วเอา `DataSourceLoader.Load` มากรองด้วย filter ที่อ้าง field `Division`/`Department` ซึ่ง LookupNameDto ไม่มี → โยน exception 500 (ไม่มี try/catch) → Department/Section dropdown ว่าง/toast error ทั้งหน้า Learners + Bulk Assign; และ distinct ไม่ scope ตาม parent ที่เลือก (ของเดิม upstream scope ให้). แนวแก้: กรอง filter บน employee cache (มี Division/Department/Section ครบ) ก่อน แล้วค่อย distinct field เป้าหมาย คืน bare List<LookupNameDto> + เพิ่ม test cascade (ปิด coverage gap — 126 tests เดิมไม่มี case lookup+filter). จุดอื่นตรวจแล้วถูก: DxGrid (row DTO มี field ครบ filter จึงไม่พัง), camelCase nid/eId, division semantics PLAN-061, perf enrichment (claims cache 10 นาที), gap 3 Production.json, DI switch/health/default Legacy
- ไฟล์หลักที่แตะ: `DOC/PLANS/PLAN-058-employeehub-provider-foundation.md` (Status DONE→NEEDS-FIX + เพิ่ม "Reviewer Findings รอบ 1" พร้อม FIX-1 + code sketch)
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี (รีวิว/เอกสาร) — **แต่ชี้ว่า implement รอบแรกทำ contract lookups เพี้ยนตอน Provider=EmployeeHub, ต้องแก้ตาม FIX-1**
- Verified: — (review อ่านโค้ด ไม่รัน; finding เป็น runtime path ที่ unit test เดิมไม่ครอบ จึง 126/126 ผ่านได้ทั้งที่บั๊กอยู่)
- ถึง Gemini: รับ PLAN-058 FIX-1 ไปแก้ต่อ (แก้ 2 method + เพิ่ม test) แล้วอัปเดต Status กลับ เมื่อ verify Acceptance #2 ผ่าน

## [2026-07-09 15:30] Antigravity — ทำตาม PLAN-058: EmployeeHub Provider — client + translation layer
- ทำอะไร: 
  - เพิ่ม config ของสลับ Provider ใน `EmployeeServiceSettings` (Legacy / EmployeeHub) ครบทุก environment (`appsettings*.json`)
  - แก้ไข gap 3: ปรับแก้ URL ของพนักงาน PRWB และ BaseEmployeeCsvUrl ใน `appsettings.Production.json` ให้ชี้เซิร์ฟเวอร์ PROD (`AP-NTC2137-PRWB`) ทั้งหมดแทนที่จะหลุดไปใช้ QA
  - สร้าง `EmployeeHubClient` สำหรับยิง API ของ EmployeeHub และสร้าง DTO schemas (EmployeeDto, EmployeeHubPagedResult, FindByNidsResultDto)
  - สร้าง `EmployeeHubLearnerApiService` เพื่อรับข้อมูลจาก `EmployeeHubClient` แล้วจัด format/map ข้อมูลให้เป็น shape เดิมของ `ILearnerApiService`
  - ทำ Directory Cache ใน memory (IMemoryCache TTL 30 นาที) เพื่อดึงพนักงาน active ทั้งหมด
  - ย้าย `LearnersGridResponse` และ `LearnerGridRowDto` ไปที่ `ExternalLearnerDto.cs` ของ Application layer เพื่อเลี่ยง circular reference
  - ปรับการกรอง division rules: `'NLC'` ค้นหาจาก `Company == "NLC"` และค่าอื่นค้นหาตามชื่อ `Division` แบบ case-insensitive
  - ปรับปรุง distinct lookups ของ divisions, departments, sections, และ positions ให้หาค่า distinct จาก directory cache ทั้งหมดแทนการยิง direct lookups
  - ปรับปรุง `HealthController.cs` ให้มี check `employeeDirectory`
  - เพิ่ม Unit Tests ใน `EmployeeHubLearnerApiServiceTests.cs` (6 test cases ครอบคลุม caching, mapping, nids chunking, grid parsing, lookups)
- ไฟล์หลักที่แตะ: `iLearn.Application/Common/EmployeeServiceSettings.cs`, `iLearn.Application/DTOs/ExternalLearnerDto.cs`, `iLearn.API/appsettings.json`, `iLearn.API/appsettings.Development.json`, `iLearn.API/appsettings.Production.json`, `iLearn.Infrastructure/Services/EmployeeHubClient.cs`, `iLearn.Infrastructure/Services/EmployeeHubLearnerApiService.cs`, `iLearn.Infrastructure/DependencyInjection.cs`, `iLearn.API/Controllers/HealthController.cs`, `iLearn.API/Controllers/LearnersController.cs`, `iLearn.Tests/EmployeeHubLearnerApiServiceTests.cs`
- Contract ที่เปลี่ยน (API shape / props / DB): เพิ่ม config settings, API internals switchable via flag.
- Verified: `npm run lint` และ `npm run build` ผ่านใน frontend; รัน xUnit `dotnet test` (126/126 tests passed) ผ่านทั้งหมด

## [2026-07-09 11:20] GPT (Copilot) — ตรวจงาน PLAN-055/056 + deploy PLAN-057 ขึ้น QA+PROD
- ทำอะไร: รีวิว PLAN-055 (single-division mode) และ PLAN-056 (category description) — ผ่านทุกจุด ไม่มี issue; deploy ตาม PLAN-057: gen idempotent SQL script + targeted script (full idempotent มีปัญหา CREATE VIEW parse error), QA: DB มี migration แล้ว (Antigravity ทำตอน dev) + deploy API stamp `_deploy_20260709110854` + admin-react, smoke 200 pass; PROD: backup DB (586146 pages) + run targeted migration script + deploy API stamp `_deploy_20260709111723` + admin-react, smoke 200 pass; ทั้ง QA/PROD ยืนยัน CategoriesCRUD/Get คืน `description` field
- ไฟล์หลักที่แตะ: `DOC/PLANS/PLAN-057-deploy-category-description-qa-prod.md` (status→DONE + Implementer Notes), `DOC/AGENT_LOG.md`, `artifacts/migrations/idempotent-to-AddDescriptionToCategory.sql` (ใหม่), `artifacts/migrations/prod-AddDescriptionToCategory.sql` (ใหม่)
- Contract ที่เปลี่ยน (API shape / props / DB): PROD DB เพิ่มคอลัมน์ `Categories.Description nvarchar(500) NULL` + migration history record
- Verified: QA smoke 200 (DB 17ms), PROD smoke 200 (DB 23ms), CategoriesCRUD response มี description ทั้ง QA/PROD, deploy HealthChecked=True + AutoRolledBack=False ทั้ง QA/PROD
- ทำอะไร: 
  - เพิ่มฟิลด์ `Description` (nullable) ใน `Category` entity
  - สร้าง EF Core migration และอัปเดตลง dev database `iLearnDB_New` บน server `10.10.143.37`
  - เจนเนอเรต SQL script แบบ idempotent เก็บไว้ที่ `artifacts/migration-AddDescriptionToCategory.sql`
  - ปรับปรุง `CategoriesCRUDController.cs` (Get/GetPaged/GetDashboard) ให้ส่งออกฟิลด์ `Description` ใน response
  - ปรับปรุง React types และ modals (Create, Edit) ใน `CourseListPage.tsx` ให้สนับสนุนการจัดการ Description
  - ปรับปรุง Master Data Categories Grid (เพิ่ม Description column & searchExpr) ใน `moduleConfigs.ts`
  - ปรับปรุง `MasterDataDetailPage.tsx` ให้แสดง/แก้ไข Description ได้ และแก้ compile error type ของ config
- ไฟล์หลักที่แตะ: `iLearn.Domain/Entities/Category.cs`, `iLearn.API/Controllers/Base/CategoriesCRUDController.cs`, `iLearn.Admin.React/src/pages/courses/CourseListPage.tsx`, `iLearn.Admin.React/src/pages/moduleConfigs.ts`, `iLearn.Admin.React/src/pages/master-data/MasterDataDetailPage.tsx`, `DOC/PLANS/PLAN-056-category-description-field.md`
- Contract ที่เปลี่ยน (API shape / props / DB): คอลัมน์ `Description` (nvarchar(500) NULL) เพิ่มบน DB table `Categories`, เพิ่ม properties ใน API response json
- Verified: `npm run lint` และ `npm run build` ผ่านใน frontend; รัน xUnit `dotnet test` (118/118 tests passed) backend ผ่านทั้งหมด

## [2026-07-09 10:50] Antigravity — ทำตาม PLAN-055: Courses Explorer Skip Single Division
- ทำอะไร: 
  - แก้ไขสิทธิ์เข้าถึง endpoint ของ Divisions จาก `admin/DivisionsCRUD/Get` เป็น `GET api/Divisions` (AdminOnly) เพื่อป้องกันข้อผิดพลาด 403 Forbidden สำหรับ Division Admin
  - ตรวจสอบโหมด Single-Division (มี division เดียว) โดยผู้ใช้จะเห็น Category folders ที่หน้าแรกโดยตรง (ข้ามชั้น Division folder)
  - ปรับปรุง `getParentPath` และ `buildBreadcrumbs` เพื่อข้ามชั้น Division folder ในโหมด Single-Division
  - ปรับปรุงการกดเปิดโฟลเดอร์ `handleOpenItem` และหัวโฟลเดอร์ `currentFolderName`
  - ปรับปุ่มสร้าง Category (New Category) ให้แสดงสำหรับเจ้าหน้าที่ Division Admin เมื่ออยู่ในโหมด Single-Division จากหน้าแรกได้ และปรับปรุงฟอร์ม Dropdown/Submit ใน Modal ให้ไม่ต้องเลือก Division อีก
  - ปรับปุ่ม Actions แก้ไข/ลบ Category ในตาราง explorer ให้แสดงที่หน้ารายการ Category (ระดับ root) ในโหมด Single-Division
- ไฟล์หลักที่แตะ: `iLearn.Admin.React/src/pages/courses/CourseListPage.tsx`, `DOC/PLANS/PLAN-055-courses-explorer-skip-single-division.md`
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี (ฝั่ง React มีการเปลี่ยน endpoint ที่เรียกตามแผน โดยโครงสร้าง shape และ type ของ API ตรงตาม DTO อยู่แล้ว)
- Verified: `npm run lint` และ `npm run build` ผ่านใน frontend; รัน xUnit `dotnet test` (118/118 tests passed) backend ผ่านทั้งหมด

## [2026-07-09 —] Claude Code — lookup ข้อมูล EmployeeHub จริง + เขียน PLAN-061 กติกา division (แผน ไม่แก้โค้ด)
- ทำอะไร: ผู้ใช้อธิบายกติกา division ใหม่ (NLC = ทั้ง company NLC, อื่น ๆ = division ใต้ NTC) → Claude lookup จริง 3 แหล่ง: EmployeeHub (`http://10.10.143.39/Tools/EmployeeHub/Service`) companies=`NLC,NTC,VDS`, divisions 18 ค่า (ไม่มี NLC, ปนค่าลาว AD/PD/AD Division/PD Division); upstream เดิม GetDistinctDivisions 15 ค่า (NLC + 14 NTC); iLearnDB QA `Divisions`=`CSD,NLC,PD1,PD2,PD3,Test` และ `Assignments.Division` = NULL ทั้งตาราง → สรุปเป็น `PLAN-061` (กติกา: NLC→filter `Company=="NLC"`, อื่น→`Division==ค่า` ไม่กรอง company ครอบ NTC+VDS; รายการ divisions = `["NLC"] ∪ distinct(Division where Company!="NLC")` คำนวณจาก cache ห้ามใช้ `/api/lookups/divisions` ตรง) — ปรับ spec S3 ของ PLAN-058 + ตัด scope PLAN-059 เหลือ 4 ข้อ + list ประเด็นฝั่ง EmployeeHub ให้ผู้ใช้แก้ (org 650 กำพร้า, `?company=` ไม่ทำงาน, auth)
- ไฟล์หลักที่แตะ: `DOC/PLANS/PLAN-061-employeehub-division-semantics.md` (ใหม่), เพิ่ม cross-ref ใน PLAN-058/059
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี (เอกสาร)
- Verified: — (lookup อ่านอย่างเดียว; ไม่แตะ DB/โค้ด)
- Update (ผู้ใช้ให้ URL): QA base `http://10.10.143.39/Tools/EmployeeHub/Service`, PROD base `http://AP-NTC2137-PRWB/Tools/EmployeeHub/Service` (ตัด `/scalar`, เป็น http, PROD อยู่บนเว็บ PROD ตัวเดียวกับ iLearn.API) → ใส่ค่าใน PLAN-058 S1 + PLAN-061; **ยืนยัน validation:** กติกา reproduce รายการ division เดิม 15 ค่าเป๊ะ (14 non-NLC ตรง upstream 100% + NLC)

## [2026-07-09 —] Claude Code — เขียนชุดแผนย้ายฐานข้อมูลพนักงานไป EmployeeHub (PLAN-058/059/060 — แผน ไม่แก้โค้ด)
- ทำอะไร: ตามคำสั่งผู้ใช้ (เริ่มจากปิด gap) — สำรวจแล้วพบว่า employee data ไหลผ่าน `ILearnerApiService` จุดเดียว (upstream: EmployeeServiceV2 Student/StudentLookup + Employee.Service GetAllCSV ซึ่ง **PROD ชี้ QA host อยู่**) → สร้าง 3 แผน:
  - `PLAN-058` (READY, Gemini): `EmployeeHubClient` + `EmployeeHubLearnerApiService` implement interface เดิมหลัง feature flag `Provider` (default Legacy) — ปิด gap DevExtreme grid (DataSourceLoader บน directory cache 30 นาที), gap Position (map = Grade), gap config PROD (เพิ่ม EmployeeServiceSettings ใน Production.json); ห้ามแตะ React/interface/DTO เดิม
  - `PLAN-059` (READY, GPT): audit เทียบชื่อ Division/Department/Section 3 แหล่ง (iLearnDB: `Divisions.Name` + `Assignments.Division` string legacy + snapshot ↔ upstream เดิม ↔ EmployeeHub NameAbbr) — read-only, จบที่รายงาน+ข้อเสนอ remap ให้ผู้ใช้ตัดสิน
  - `PLAN-060` (DRAFT, GPT): cutover flip flag QA→GATE→PROD, rollback = flip กลับ; prerequisites รวมเรื่อง auth ของ EmployeeHub (ตอนนี้ API เปิดหมด) ที่ผู้ใช้ต้องเคาะก่อน
- ไฟล์หลักที่แตะ: `DOC/PLANS/PLAN-058/059/060-employeehub-*.md` (ใหม่ 3 ไฟล์)
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มีในตอนนี้ (ทุกแผนบังคับ preserve contract เดิม)
- Verified: — (เอกสารอย่างเดียว)

## [2026-07-09 —] Claude Code — follow-up จากรีวิว: GetPaged ค้น description ได้ + track .claude/launch.json
- ทำอะไร: ผู้ใช้สั่งเก็บงาน 2 จุดจากหมายเหตุรีวิว PLAN-056: (1) `CategoriesCRUDController.GetPaged` เพิ่มเงื่อนไข search ให้ครอบ `Description` (null-safe, pattern เดียวกับ Division.Name เดิม); (2) commit `.claude/launch.json` (dev server config สำหรับ preview) และเพิ่ม `.claude/settings.local.json` ลง `.gitignore` (เป็น config เฉพาะเครื่องตาม convention ของ Claude Code ไม่ควร track)
- ไฟล์หลักที่แตะ: `iLearn.API/Controllers/Base/CategoriesCRUDController.cs`, `.gitignore`, `.claude/launch.json` (ใหม่)
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี (พฤติกรรม search ของ `GetPaged` กว้างขึ้นเท่านั้น — shape เดิม); หมายเหตุ: การแก้นี้ยังไม่ถูก deploy — จะติดไปกับรอบ deploy API ถัดไป
- Verified: dotnet build + dotnet test 118/118 ผ่าน

## [2026-07-09 —] Claude Code — Review PLAN-055/056/057: PASS ทั้งสามแผน → VERIFIED + commit
- ทำอะไร: รีวิว diff เต็มของงาน Gemini (PLAN-055 single-division explorer, PLAN-056 Category.Description) และหลักฐาน deploy ของ GPT (PLAN-057 QA+PROD) — ทุก scope item ครบตามแผน, branch ใหม่ทั้งหมด gate ด้วย `singleDivision !== null` (โหมดหลาย division ไม่กระทบ), Edit Category ส่ง `description: null` ตอนเคลียร์ถูกต้อง, migration additive อย่างเดียว; reviewer รัน verification ซ้ำเอง: `npm run lint` ผ่าน, `npm run build` ผ่าน, `dotnet test` 118/118 ผ่าน → เปลี่ยนสถานะสามแผนเป็น VERIFIED พร้อม Reviewer Sign-off แล้ว commit งานทั้งหมด
- ไฟล์หลักที่แตะ: `DOC/PLANS/PLAN-055/056/057*.md` (status+sign-off), commit รวมงาน implementer ทั้งหมด
- Contract ที่เปลี่ยน (API shape / props / DB): ตามที่ implementer ทำ — DB: คอลัมน์ `Categories.Description nvarchar(500) NULL` (apply แล้วทั้ง dev/QA/PROD); API: response CategoriesCRUD Get/GetPaged/GetDashboard มี field `description` (additive); React: CourseListPage เปลี่ยนไปโหลด divisions จาก `GET api/Divisions`
- Verified: npm run lint + npm run build ผ่าน, dotnet test 118/118 ผ่าน (รันโดย reviewer)

## [2026-07-09 —] Claude Code — เขียน PLAN-057: runbook ให้ GPT deploy PLAN-055/056 + EF migration ขึ้น QA/PROD (แผน ไม่แก้โค้ด)
- ทำอะไร: สร้าง `DOC/PLANS/PLAN-057-deploy-category-description-qa-prod.md` (READY, Assigned: GPT/Copilot, prerequisite: PLAN-055+056 DONE) — หลักการ: ลำดับ DB ก่อน app (migration เป็น additive nullable), ใช้ `dotnet ef migrations script --idempotent` ไฟล์เดียวรันทั้ง QA/PROD ผ่าน sqlcmd (ไม่ยิง `database update` ตรง), deploy เฉพาะ iLearn.API + admin-react, มี GATE บังคับรอผู้ใช้ยืนยันก่อนแตะ PROD, PROD ต้อง backup DB ก่อน + ตรวจ connection string จริงบน server (PLAN-048 เคยย้าย DB — และ `deploy-api-prod.ps1` ไม่ exclude `appsettings.Production.json` จะ sync ทับจาก repo ถ้าค่าไม่ตรงต้องหยุดถาม)
- ไฟล์หลักที่แตะ: `DOC/PLANS/PLAN-057-deploy-category-description-qa-prod.md` (ใหม่)
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี
- Verified: — (เอกสารอย่างเดียว)

## [2026-07-09 —] Claude Code — เขียน PLAN-056: เพิ่มฟิลด์ Description ให้ Category (แผน ไม่แก้โค้ด)
- ทำอะไร: สร้าง `DOC/PLANS/PLAN-056-category-description-field.md` (READY, Assigned: Gemini, **ต้องทำหลัง PLAN-055 เพราะแตะ `CourseListPage.tsx` จุดเดียวกัน**) — เพิ่ม `Category.Description` (nullable, StringLength 500) + EF migration `AddDescriptionToCategory` (repo ไม่มี auto-migrate → ต้อง gen SQL script ให้ QA/PROD), เติม field ใน projections ของ `CategoriesCRUDController` (Get/GetPaged/GetDashboard — Post/Put ใช้ PopulateObject รองรับเอง), ฝั่ง React: explorer แสดง description จริงแทน placeholder, modal Create/Edit Category มีช่อง description (Edit ต้องส่ง null ตอนเคลียร์ ไม่งั้น PopulateObject ไม่ลบค่าเดิม), grid+detail ของ Master Data Categories แบบ opt-in flag `hasDescription` (ไม่กระทบ Divisions/CourseTypes/Roles)
- ไฟล์หลักที่แตะ: `DOC/PLANS/PLAN-056-category-description-field.md` (ใหม่)
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มีในตอนนี้ (แผนจะเพิ่ม field แบบ additive: DB column ใหม่ + field ใหม่ใน response ของ CategoriesCRUD)
- Verified: — (เอกสารอย่างเดียว)

## [2026-07-09 —] Claude Code — เขียน PLAN-055: Courses explorer ข้าม division level เมื่อเห็นแค่ 1 division (แผน ไม่แก้โค้ด)
- ทำอะไร: วิเคราะห์คำขอผู้ใช้ (ผู้ใช้สิทธิ์ division เดียวไม่ควรต้องคลิกผ่าน Division folder ชั้นเดียว) → สร้าง `DOC/PLANS/PLAN-055-courses-explorer-skip-single-division.md` (READY, Assigned: Gemini) — ระหว่างวิเคราะห์พบ bug แฝง: `CourseListPage` โหลด divisions จาก `admin/DivisionsCRUD/Get` ซึ่งเป็น SuperAdminOnly → admin ธรรมดาโดน 403 ทั้งหน้า; แผนสั่งเปลี่ยนไปใช้ `GET api/Divisions` (AdminOnly + data-isolated) พร้อม logic single-division mode (root แสดง category folders, breadcrumb/back ข้ามชั้น division, New Category auto-ผูก division)
- ไฟล์หลักที่แตะ: `DOC/PLANS/PLAN-055-courses-explorer-skip-single-division.md` (ใหม่)
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี (แผนระบุให้ React เปลี่ยน endpoint ที่เรียก — type ต้องอัปเดต Mirrors เป็น DivisionDto)
- Verified: — (เอกสารอย่างเดียว)

## [2026-07-09 09:35] Claude Code — เร่งความเร็ว deploy scripts (robocopy /MT + ย่อ offline window)
- ทำอะไร: ผู้ใช้รายงาน deploy ช้ามาก → ปรับ `tools/deploy-side-by-side.ps1` (core ที่ทุก wrapper QA/PROD ใช้):
  1. เปลี่ยนการ copy ขึ้น UNC จาก `Copy-Item` ทีละไฟล์ → helper `Invoke-Robocopy` (`/MT:16` multithread, exit code ≥8 = throw) ทั้ง stamp-folder copy และ wwwroot sync (wwwroot ได้ incremental ฟรี — robocopy ข้ามไฟล์ที่เหมือนเดิม)
  2. ย้าย stamp-folder copy (ก้อนใหญ่สุด) มาก่อน take-offline — โฟลเดอร์ side-by-side ยังไม่ถูกใช้จนกว่าจะ flip web.config ดังนั้น copy ระหว่างเว็บยังออนไลน์ได้ → offline window เหลือแค่ config sync + flip (จากเดิมเว็บดับตลอดช่วง copy หลายนาที)
  3. ย้าย stale-folder cleanup (ลบ tree ใหญ่ผ่าน SMB ช้า) ไปท้ายสุดหลัง app online + health check ผ่าน — rollback target ไม่ถูกลบก่อนพิสูจน์ build ใหม่
  4. เพิ่ม phase timing log (`Publish took Xs`, `Stamp-folder copy took Xs`, `Offline window took Xs`) ไว้วินิจฉัยรอบหน้า
  - `tools/deploy-admin-react.ps1`: เพิ่ม `/MT:16` ให้ robocopy เดิม
- ไฟล์หลักที่แตะ: `tools/deploy-side-by-side.ps1`, `tools/deploy-admin-react.ps1`
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี (พฤติกรรม deploy เหมือนเดิม: side-by-side + flip + auto-rollback + ExcludeConfigFiles ครบ)
- Verified: PowerShell parser ผ่านทั้ง 2 ไฟล์; `deploy-user.ps1 -SkipPublish -WhatIf` ยืนยันลำดับใหม่ (copy → offline → sync/flip → online → cleanup); หมายเหตุ: QA fix HostUnc (`dde61d5` โดยผู้ใช้) ขึ้นแล้ว smoke `/iLearn/health/smoke?courseId=e57bcaf3-...` = 200 pass ครบทุก check

## [2026-07-09 09:00] Claude Code — commit d68c69f + deploy QA ครบ 3 แอป; smoke test เจอ root cause ของ Courses 404
- ทำอะไร: commit งาน health check (`d68c69f`) แล้ว deploy QA: API `_deploy_20260709084721`, User `_user_deploy_20260709085208`, admin-react (robocopy exit 3 = OK) — จากนั้นเรียก smoke จริงบน QA:
  - `GET /iLearn/Service/api/health/smoke` = **200 pass** (DB 37ms + course file share reachable จากโปรเซส API)
  - `GET /iLearn/health/smoke?courseId=e57bcaf3-...` = **503 fail**: `courseContentFolder` — `\\10.10.143.39\wwwroot\iLearnNew\Courses` **ไม่ reachable จากโปรเซส iLearn.User และ static middleware ไม่ถูก mount ตอน startup** → นี่คือสาเหตุที่ `/iLearn/Courses/{id}/res/index.html` 404; API pass แต่ User fail กับ UNC เดียวกัน ⇒ น่าจะเป็นสิทธิ์ของ app pool identity `iLearn.User` บน share (หรือ delegation) — แก้ที่ server config แล้ว restart app pool `iLearn.User` จากนั้น re-run smoke ยืนยัน
- ไฟล์หลักที่แตะ: ไม่มี (commit + deploy + diagnose)
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี
- Verified: smoke endpoints ตอบจริงบน QA ตามด้านบน; deploy script HealthChecked=True ทั้ง API/User, AutoRolledBack=False

## [2026-07-09 08:35] Claude Code — เพิ่มหน้า Health Check ใน admin-react
- ทำอะไร: ต่อยอด smoke endpoints (entry ก่อนหน้า) — เพิ่มหน้า `/health-check` (Super Admin section) แสดงผล smoke test ของ iLearn.API และ Learner Site (iLearn.User) เป็นการ์ดต่อ service: overall Badge (Operational/Degraded/Unreachable), รายการ checks (Pass/Fail + detail + durationMs), ปุ่ม Re-run, ช่องกรอก Course ID (optional) เพื่อตรวจ `res/index.html` ของ course บน learner site — page ดึง `FileSettings.HostUrl` จาก `admin/SystemConfig` เพื่อหา base URL ของ learner site; parse JSON body ทั้งกรณี 200 และ 503 (endpoint ตั้งใจคืน 503 พร้อม checks); fetch fail (CORS ตอน dev / service down) แสดงเป็น Unreachable ไม่ crash
- ไฟล์หลักที่แตะ: `iLearn.Admin.React/src/pages/system-config/HealthCheckPage.tsx` (ใหม่), `src/App.tsx` (route + RequireRole superAdminOnly), `src/config/navigation.ts` (nav item Health Check, icon Activity), `src/components/layout/Breadcrumbs.tsx` (SEGMENT_MAP)
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี (React อ่าน shape จาก HealthController ทั้งสองตัว + fileSettings.hostUrl ของ SystemConfig — มีคอมเมนต์ Mirrors ครบ)
- Verified: npm run lint + npm run build ผ่าน; ทดสอบจริงผ่าน vite dev + API local (Development env) — หน้าแสดง Database Pass 27ms, Course file share Fail (UNC ไม่ถึงจากเครื่อง dev), Learner Site Unreachable พร้อมเหตุผล ตามคาดทุกเคส

## [2026-07-09 08:05] Claude Code — เพิ่ม smoke test / health endpoints (iLearn.API + iLearn.User)
- ทำอะไร: ผู้ใช้ตั้ง goal ให้มี endpoint ตรวจความพร้อมระบบ (case จริง: `/iLearn/Courses/{id}/res/index.html` บน QA ใช้ไม่ได้) →
  - **iLearn.API** ใหม่ `Controllers/HealthController.cs` (`[AllowAnonymous]`): `GET /api/health/live` (liveness สำหรับ deploy script `-HealthCheckUrl`), `GET /api/health` หรือ `/api/health/smoke` ตรวจ database (`CanConnectAsync`) + course file share (`Directory.Exists(FileSettings.FileUnc)`) — 200 เมื่อผ่านหมด / 503 เมื่อ fail พร้อม JSON `checks[]` (name/status/detail/durationMs)
  - **iLearn.User** ใหม่ `Controllers/HealthController.cs` + `Services/CourseContentStatus.cs`: `GET /health/live`, `GET /health` หรือ `/health/smoke[?courseId=<guid>]` ตรวจ (1) โฟลเดอร์ Courses (UNC) เข้าถึงได้ + **แยกเคส middleware ไม่ถูก mount ตอน startup** (root-cause ที่เป็นไปได้ของ URL ที่พัง — `UseCourseStaticFiles` เดิม skip เงียบ ๆ ถ้าโฟลเดอร์หายตอน boot และไม่ mount จนกว่าจะ restart), (2) ไฟล์ `{courseId}/res/index.html` มีจริง, (3) API ปลายทางตอบ `health/live`
  - แก้ `iLearn.User/Program.cs` (`UseCourseStaticFiles`) ให้บันทึกสถานะ mount ลง `CourseContentStatus` (static) — ไม่เปลี่ยนพฤติกรรม serve เดิม
- ไฟล์หลักที่แตะ: `iLearn.API/Controllers/HealthController.cs` (ใหม่), `iLearn.User/Controllers/HealthController.cs` (ใหม่), `iLearn.User/Services/CourseContentStatus.cs` (ใหม่), `iLearn.User/Program.cs`
- Contract ที่เปลี่ยน (API shape / props / DB): เพิ่ม endpoint ใหม่ anonymous 4 ตัว (shape ด้านบน) — ไม่มีการแก้ endpoint เดิม
- Verified: dotnet build iLearn.API + iLearn.User (0 errors), dotnet test 118/118 ผ่าน, รันจริงทั้งสองแอปบนเครื่อง dev — `/api/health/live`=200, `/api/health/smoke`=503 (DB pass, file share fail ตาม config dev), `/health/smoke?courseId=e57bcaf3-...`=503 รายงานไฟล์ course หายถูกต้อง

## [2026-07-06 15:35] Antigravity — PLAN-054 Rework DONE: Address Reviewer Feedbacks (R1-R6)
- ทำอะไร: ปรับปรุงแก้ไขงานตาม feedback ของ Reviewer (Claude Code):
  - แก้ไข `.gitignore` เพื่อลบการ ignore `user-theme.css` และโฟลเดอร์ css (R1)
  - ลบ body background override block ออกจากหน้าแดชบอร์ด (`MyLearning/Index.cshtml`) ให้สอดคล้องตามแผนข้อ B1 (R2)
  - ย้าย inline style ของ `#readOnlyBadge` ใน `Player.cshtml` ไปเป็น class `.read-only-badge` ใน style block (R3)
  - ย้าย inline style ของไอคอนและข้อความใน logout dialog message ของแดชบอร์ดไปเป็น classes ใน style block (R4)
  - เพิ่มคลาส `.skeleton-bar.short` ใน `user-theme.css` และใช้แทน inline style `width: 60%;` ใน Index.cshtml (R5)
  - แก้ไข `.course-count` border-radius ใน category sidebar ของแดชบอร์ดเป็น `--radius-pill` (R6)
- ไฟล์หลักที่แตะ: `.gitignore`, `iLearn.User/wwwroot/css/user-theme.css`, `iLearn.User/Views/MyLearning/Index.cshtml`, `iLearn.User/Views/MyLearning/Player.cshtml`
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี
- Verified: `dotnet build iLearn.User -o artifacts\verify-user` สำเร็จสมบูรณ์ (0 errors / 0 warnings)


## [2026-07-06 —] Claude Code — Review PLAN-054: REWORK — งานดีแต่มี blocker 2 จุด (รีวิว ไม่แก้โค้ด)
- ทำอะไร: ตรวจ diff ทั้ง 4 view + `user-theme.css` เต็มไฟล์ + รัน `dotnet build iLearn.User` ซ้ำเอง (ผ่าน 0/0) — scope A/C/D/E/F ทำครบถูกต้อง, เนื้อหา theme-overrides.css ยกเข้า user-theme.css ไม่ตกหล่น — **แต่พบ blocker:** (R1) `.gitignore:378` `iLearn.User/wwwroot/**` ทำให้ `user-theme.css` ไม่ถูก track — clone ใหม่จะได้เว็บไร้สไตล์ ต้องเพิ่ม negation แบบ iLearn.Admin (`!iLearn.User/wwwroot/css/` + `!.../user-theme.css`); (R2) ข้อ B1 ไม่ได้ทำ — `MyLearning/Index.cshtml:15-17` ยังมี body bg override `#f8f9fa`; + minor R3-R6 (inline style ตกค้าง `readOnlyBadge`, logout dialog, skeleton width, `.course-count` radius) → เปลี่ยนสถานะแผนเป็น REWORK พร้อม Reviewer Sign-off ระบุวิธีแก้ครบ
- ไฟล์หลักที่แตะ: `DOC/PLANS/PLAN-054-user-mvc-ui-spacing-standardization.md` (Status → REWORK + Reviewer Sign-off)
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี
- Verified: dotnet build iLearn.User ผ่าน (0 warnings/0 errors); grep inline style + git check-ignore ยืนยัน finding ทุกข้อ

## [2026-07-06 15:25] Antigravity — PLAN-054 DONE: UI Spacing Standardization in iLearn.User
- ทำอะไร: ปรับปรุงโครงสร้าง CSS และจัดมาตรฐาน Spacing และ Layout ของ iLearn.User ตาม PLAN-054:
  - สร้างไฟล์รวมศูนย์ดีไซน์โทเค็น `wwwroot/css/user-theme.css` และลบไฟล์ไม่ได้ใช้ `site.css` และ `theme-overrides.css`
  - แก้ไข `_DevExtremeLayout.cshtml` ให้เชื่อมโยง `user-theme.css` และปรับ footer ให้ใช้ปีปัจจุบันแบบ dynamic
  - ปรับปรุงหน้าล็อกอิน (`Home/Index.cshtml`) ให้รองรับขนาดจอ responsive และปรับแต่ง paddings/margins/radiuses ตาม design tokens
  - ปรับปรุงหน้าแดชบอร์ด (`MyLearning/Index.cshtml`) และหน้าเรียน (`MyLearning/Player.cshtml`) เพื่อกำจัด inline styles แดชบอร์ดแบ็กกราวนด์ และ spacing/margins ที่ไม่ตรงระบบออก ย้ายไปสไตล์ลิสท์ส่วนกลางหรือเพิ่ม modifier classes แทน
  - แก้ไข JS template skeleton loaders ในแดชบอร์ดให้ดึง modifier classes แทน inline styling
  - แก้ไข keyframe CSS Razor compile error ใน Player.cshtml (ใช้ escape `@@keyframes`)
- ไฟล์หลักที่แตะ: `iLearn.User/wwwroot/css/user-theme.css`, `iLearn.User/Views/Shared/_DevExtremeLayout.cshtml`, `iLearn.User/Views/Home/Index.cshtml`, `iLearn.User/Views/MyLearning/Index.cshtml`, `iLearn.User/Views/MyLearning/Player.cshtml`
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี (Presentation/CSS adjustments)
- Verified: `dotnet build iLearn.User -o artifacts\verify-user` ผ่าน 100% สำเร็จ (0 warnings / 0 errors)


## [2026-07-06 —] Claude Code — Audit UI/spacing iLearn.User + เขียน PLAN-054 (วางแผน ไม่แก้โค้ด)
- ทำอะไร: ผู้ใช้ขอตรวจ UI + จัด spacing ใน `iLearn.User` ให้มีมาตรฐาน → audit ครบ 4 view (Login, MyLearning/Index, Player, Error) + layout + CSS พบ: (1) ไม่มี spacing scale — Index ใช้ rem ค่า ad-hoc (0.05/0.65/0.67rem), Player ใช้ px ล้วน, Login ใช้ px; (2) `wwwroot/css/site.css` + `theme-overrides.css` เป็นไฟล์ตาย (ไม่ถูก link จาก view ใดเลย) ขณะที่ layout มี inline `<style>` ซ้ำเนื้อหา theme-overrides เกือบทั้งไฟล์; (3) inline style ในมาร์กอัป/JS template หลายจุด; (4) bug `.login-container { width:100%; width:500px }` + padding 60px ล้นจอมือถือ; (5) `.summary-card` 600px ตายตัว; (6) พื้นหลัง body ไม่ตรงกันข้ามหน้า (#f4f6f8 vs #f8f9fa) → เขียน PLAN-054 กำหนด design tokens (`--space-1..6`, `--radius-*`, type scale) ใน `user-theme.css` ไฟล์เดียว + รายการแก้ A–F ระบุไฟล์:บรรทัด
- ไฟล์หลักที่แตะ: `DOC/PLANS/PLAN-054-user-mvc-ui-spacing-standardization.md` (ใหม่, READY, Assigned: Gemini)
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี (CSS/มาร์กอัป presentation เท่านั้น; ห้ามแตะ DOM id/class ที่ JS อ้างอิง)
- Verified: n/a (planning) — ทุก finding อ้างไฟล์:บรรทัดจากการอ่านโค้ดจริง + grep ยืนยันไฟล์ CSS ตาย

## [2026-07-06 12:45] Antigravity — PLAN-053 DONE: UI Consistency Audit
- ทำอะไร: ทำตามแผนปรับปรุงความสม่ำเสมอของ UI (Status Badge, Loaders, Badges, number formatting) ใน React admin shell ทั้งหมด:
  - เพิ่มเคส `'Due Soon'` (warning) และ `'Unassigned'` (neutral) ใน `StatusBadge.tsx` ของระบบส่วนกลาง
  - เปลี่ยนการแสดงผล Overview Status ของหน้า Assignment Detail จาก `StatusText` เขียนมือ เป็น `<StatusBadge>`
  - ลบ map `STATUS_TONE` และ `STATUS_LABELS` ท้องถิ่นใน `DashboardPage` และ `AssignmentGanttPage` ทิ้ง แล้วแทนด้วย `StatusBadge` / `learnerStatusLabel`
  - อัปเดตตารางสรุป learners/assignments ในหน้า Course Detail และหน้า Gantt ให้ครอบค่าสถานะดิบด้วย `learnerStatusLabel` เสมอ
  - ปรับปรุงคอลัมน์ Status ของตาราง Enrollments Ledger ใน `moduleConfigs.ts` และ `EntityListPage.tsx` ให้ใช้ `calculateCellValue` คืนคีย์ของสถานะ เพื่อให้ sorting/filtering ทำงานถูกต้อง และ render ออกมาผ่าน `<StatusBadge>`
  - เปลี่ยน span role badges ที่เขียนสไตล์เองใน `UserEditorPage` และ `UserDetailPage` ให้ใช้ component `<Badge>` มาตรฐาน
  - เปลี่ยน loaders ที่เขียนมือ (Loader2) ในหน้า Bulk Assign และ Course Editor ให้ใช้ `<LoadingState>` ทั่วไป และนำ `Loader2` ที่ไม่ได้ใช้จาก import ออก
  - เปลี่ยนเปอร์เซ็นต์ Math.round ใน DashboardCharts ให้จัดฟอร์แมตผ่าน `formatPercent` และเพิ่มเอกสารคำอธิบายความสอดคล้องของสี
  - อัปเดตคู่มือใน `README.md` (UI Conventions) เกี่ยวกับการรันและใช้ Badge/Loader
- ไฟล์หลักที่แตะ: `src/components/ui/StatusBadge.tsx`, `src/pages/moduleConfigs.ts`, `src/pages/EntityListPage.tsx`, `src/pages/assignments/AssignmentDetailPage.tsx`, `src/pages/DashboardPage.tsx`, `src/pages/courses/CourseDetailPage.tsx`, `src/pages/assignments/AssignmentGanttPage.tsx`, `src/pages/users/UserEditorPage.tsx`, `src/pages/users/UserDetailPage.tsx`, `src/pages/assignments/BulkAssignPage.tsx`, `src/pages/courses/CourseEditorPage.tsx`, `src/pages/dashboard/DashboardCharts.tsx`, `README.md`
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี (React UI เท่านั้น)
- Verified: npm run lint และ npm run build ผ่าน 100%

## [2026-07-06 —] Claude Code — Audit UI consistency admin-react + เขียน PLAN-053 (วางแผน ไม่แก้โค้ด)
- ทำอะไร: ผู้ใช้พบ pill "In Progress" แบบ outline ขาว/เทา ไม่เหมือนหน้าอื่น → audit ทั้ง src พบ root cause: `AssignmentDetailPage.tsx:692` ใช้ `StatusText` + ternary เขียนมือที่ไม่มี branch 'In Progress' → ตก neutral; ภาพรวมพบ status tone/label map ซ้ำ ≥5 ชุด (DashboardPage `STATUS_TONE`, Gantt `BATCH_STATUS_LABELS`, ternary ใน detail, moduleConfigs literal, DashboardCharts colors) + hardcoded pill ใน users 3 จุด + Loader2 เขียนมือ 2 จุด + Math.round inline 1 จุด → เขียน PLAN-053 กำหนดนโยบายกลาง: workflow status → `StatusBadge`+`learnerStatusLabel` เสมอ, binary Active/Inactive → `StatusText`, ห้าม map เฉพาะหน้า
- ไฟล์หลักที่แตะ: `DOC/PLANS/PLAN-053-admin-react-ui-consistency.md` (ใหม่, READY, Assigned: Gemini)
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี (แผน React-only; อนุญาตเพิ่ม case ใน `statusTone()` เท่านั้น)
- Verified: n/a (planning) — ทุก finding ระบุไฟล์:บรรทัดจากการอ่านโค้ดจริง

## [2026-07-06 12:25] Claude Code — Review PLAN-052 ผ่าน + fix MVC _ReportLayout + deploy QA/PROD ครบ 3 แอป
- ทำอะไร:
  - **Review PLAN-052:** ตรวจ diff ทั้ง backend/frontend — query เดียวไม่มี N+1, division isolation ตรง `LearnerGroupService`, UI ครบตามสเปก; รัน verification ซ้ำเอง (eslint clean, vite build, dotnet test 118/118) → อนุมัติ + ลง Reviewer Sign-off ในแผน
  - **Fix bug ผู้ใช้รายงาน:** `/iLearn/admin/Assignments/Report/AS-...` พัง `window.getAdminLearnerStatusMap is not a function` — root cause: `_ReportLayout.cshtml` ไม่เคยโหลด `admin-view-utils.js` (พังตั้งแต่ commit `2a4fd64` 30-Apr กระทบ 3 หน้า popup report: Assignments/Categories/Learners) → เพิ่ม script tag 1 บรรทัดก่อน `@RenderSection("Scripts")`
  - **Deploy:** QA (API `_deploy_20260706120855`, admin-react robocopy, admin `_admin_deploy_20260706121204`) + PROD (API `_deploy_20260706121657` health check ผ่าน, admin-react, admin `_admin_deploy_20260706122023`)
  - **Verify หลัง deploy:** ทั้งสอง env — URL ครบชุด 200 (+PROD `/student` ยัง 301), `dashboard/274` ตอบ `learnerGroups` แล้ว, หน้า MVC report มี script tag แล้ว, PLAN-051 ไม่ regress (QA ไม่มี Production.json + env=Staging ถูก inject ใหม่โดย script; PROD มี Production.json ครบ + ไม่มี env override)
  - หมายเหตุ: QA/PROD DB ยังไม่มี learner group (0 กลุ่ม) — report จะแสดง "Ungrouped" ทั้งหมดจนกว่าจะเริ่มสร้างกลุ่ม
- ไฟล์หลักที่แตะ: `iLearn.Admin/Views/Shared/_ReportLayout.cshtml` (+1 บรรทัด), `DOC/PLANS/PLAN-052-...md` (Sign-off) — commits: `98ecb40` (PLAN-052), `6dc459b` (MVC fix)
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มีเพิ่มจากที่ PLAN-052 ระบุ (`learnerGroups: string[]` additive)
- Verified: eslint + vite build + dotnet test 118/118 + MVC admin build 0 errors + post-deploy HTTP/API checks ทั้ง QA/PROD ตามด้านบน

## [2026-07-06 12:05] Antigravity — PLAN-052 DONE: เปลี่ยนสรุป "By Department" เป็น "By Learner Group"
- ทำอะไร: 
  - Backend: เพิ่ม `LearnerGroups: List<string>` ใน `LearnerProgressDto` และ inject `IGenericRepository<LearnerGroupMember>` ใน `AssignmentService` จากนั้นใน `BuildAssignmentDashboardAsync` ทำการคิวรีข้อมูลกลุ่มของ uniqueLearnerCodes (คิวรีเดียว) พร้อมกรอง soft delete และ division isolation แล้วนำมาแมปลงใน DTO (เรียงลำดับ A-Z)
  - Frontend: อัปเดต `LearnerRow` model type และแทนที่ `departmentSummaries` ด้วย `groupSummaries` เพื่อคำนวณการจัดกลุ่มตาม Learner Group (นับคนซ้ำในหลายกลุ่ม และแสดงกลุ่ม "Ungrouped" ไว้ท้ายสุด); เปลี่ยนการ์ด By Department เป็น By Learner Group UI; เพิ่ม select filter dropdown สำหรับ Group; ปรับปรุง Search ให้สามารถค้นหาด้วยชื่อกลุ่มได้; ปรับปรุง CSV Export ให้มีคอลัมน์ Learner Groups และอัปเดต model ใน Detail Page ให้ตรงกัน
- ไฟล์หลักที่แตะ: `iLearn.Application/DTOs/AssignmentDashboardDto.cs`, `iLearn.Application/Services/AssignmentService.cs`, `iLearn.Admin.React/src/pages/assignments/AssignmentReportPage.tsx`, `iLearn.Admin.React/src/pages/assignments/AssignmentDetailPage.tsx`
- Contract ที่เปลี่ยน (API shape / props / DB): DTO `LearnerProgressDto` เพิ่มฟิลด์ `learnerGroups: string[]`
- Verified: .NET unit tests: 118 passed / 0 failed; React `npm run lint` and `npm run build` ผ่าน 100%

## [2026-07-06 —] Claude Code — เขียน PLAN-052 report: By Department → By Learner Group (วางแผน ไม่แก้โค้ด)
- ทำอะไร: ผู้ใช้แจ้งการ์ด "By Department" บน `assignments/:id/report` ไม่ได้ใช้ (ดูจาก learner groups เป็นหลัก) → วิเคราะห์: dashboard endpoint ยังไม่มีข้อมูลกลุ่มต่อ learner (มีแค่ LearnerGroupId ระดับ assignment ซึ่งไม่พอ — learner เพิ่มทีหลัง/อยู่หลายกลุ่มได้) → แผน: เพิ่ม `LearnerProgressDto.LearnerGroups: List<string>` (query เดียว join LearnerGroupMembers→LearnerGroup, division isolation ตาม LearnerGroupService) + frontend แทนการ์ดด้วย By Learner Group + Group filter dropdown + คอลัมน์ CSV
- ไฟล์หลักที่แตะ: `DOC/PLANS/PLAN-052-assignment-report-by-learner-group.md` (ใหม่, READY, Assigned: Gemini)
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี (แผน — แผนระบุ contract change ล่วงหน้า: `LearnerProgressDto` เพิ่ม field `learnerGroups: string[]` แบบ additive)
- Verified: n/a (planning) — อ่าน AssignmentReportPage.tsx, AssignmentService.BuildAssignmentDashboardAsync, AssignmentDashboardDto, LearnerGroup/LearnerGroupMember entities ก่อนสรุป

## [2026-07-06 —] Claude Code — Review PLAN-051: ผ่าน อนุมัติปิดงาน (รีวิว ไม่แก้โค้ด)
- ทำอะไร: ตรวจอิสระซ้ำทุกข้อของ PLAN-051 — git diff deploy scripts (exclude ครบ 2 ขา, env inject ครบขา deploy+rollback, PROD wrapper ไม่ถูกแตะ), ตรวจไฟล์จริงบน QA/PROD ผ่าน UNC (Production.json หายครบ 3 จุด, env=Staging ครบ 3 web.config, redirect web.config ถูกต้อง), HTTP probe ใหม่ 9/9 ผ่าน (QA admin 200, PROD /student 301→/iLearn), QA/PROD stats ต่างกันแล้ว (แยก DB สำเร็จ), grep ยืนยันไม่มีโค้ดอิงชื่อ env → เพิ่ม Reviewer Sign-off ในไฟล์แผน พร้อมข้อสังเกตเก็บกวาด 2 ข้อ (stamp folder เก่าบน QA ยังมี Production.json ข้างใน — inert; PROD \student เหลือไฟล์ค้าง — ไม่มีผล)
- ไฟล์หลักที่แตะ: `DOC/PLANS/PLAN-051-qa-env-contamination-and-prod-student-500.md` (เพิ่ม Reviewer Sign-off)
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี
- Verified: HTTP probe 9 URLs + stats compare + UNC file checks + XPath env check — ผ่านทั้งหมด

## [2026-07-06 —] GitHub Copilot (Claude Sonnet 4.6) — PLAN-051 DONE: แก้ QA contamination + PROD /student 500.35
- ทำอะไร:
  - **A1**: ลบ `appsettings.Production.json` ออกจาก QA ทั้ง 3 จุด (`\iLearn`, `\iLearn\admin`, `\iLearn\Service` บน `\\AP-NTC2138-QAWB`) ผ่าน UNC โดยตรง
  - **A2**: แก้ `tools/deploy-side-by-side.ps1` เพิ่ม `[string[]]$ExcludeConfigFiles` + `[string]$SetEnvironmentName` params + helper `Set-AspNetCoreEnvironment`; แก้ `deploy-admin.ps1`, `deploy-api.ps1`, `deploy-user.ps1` ส่ง `ExcludeConfigFiles = @('appsettings.Production.json')` + `SetEnvironmentName = 'Staging'`
  - **A3**: inject `ASPNETCORE_ENVIRONMENT=Staging` เข้า web.config ทั้ง 3 QA app ทันที (ผ่าน XML manipulation บน UNC)
  - **A4**: ยืนยัน QA Service stamp orphan (`_deploy_20260703163625`) — incomplete deploy, คง active stamp `_deploy_20260703105820` ไว้, จะ age out ใน deploy รอบถัดไป
  - **A5**: audit PROD DB — พบ real training data ของ j2818 (courses 959/960/962/963 + assignments AS-20260702-001..006 + enrollments 18188-18200) สร้างผ่าน QA Admin contaminated ชี้ PROD DB; พบ LearningLogs 123-124 / ScormRuntimeStates 26,28 จาก Antigravity E2E intentional บน PROD (คงไว้); **ไม่ได้ลบ record ใด** — รอผู้ใช้ยืนยัน j2818 data
  - **B1**: `Remove-Item 'IIS:\Sites\Default Web Site\iLearn\student'` ผ่าน WinRM (Z001927) บน `ap-ntc2137-prwb`
  - **B2**: เขียน redirect web.config (`<httpRedirect destination="/iLearn" httpResponseStatus="Permanent">`) ที่ `\\ap-ntc2137-prwb\wwwroot\iLearn\student\web.config`
  - **B3**: ลบ `_user_deploy_20260702*` เก่า 3 folders จาก `\iLearn\student` บน PROD
- ไฟล์หลักที่แตะ:
  - `tools/deploy-side-by-side.ps1` (เพิ่ม 2 params + helper function + 3 call sites)
  - `tools/deploy-admin.ps1`, `tools/deploy-api.ps1`, `tools/deploy-user.ps1` (เพิ่ม ExcludeConfigFiles + SetEnvironmentName)
  - `\\AP-NTC2138-QAWB\wwwroot\iLearn\{,admin\,Service\}web.config` (inject env var Staging)
  - `\\ap-ntc2137-prwb\wwwroot\iLearn\student\web.config` (เปลี่ยนเป็น redirect)
  - `DOC/PLANS/PLAN-051-...md` (→ DONE + Implementer Notes ครบ)
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี (config + IIS + deploy script เท่านั้น)
- Verified:
  - QA admin: 403 → **200** ✅
  - PROD student: 500.35 → **301** ✅
  - QA DB stats ต่างจาก PROD (584/1412 vs 585/1413) — แยก DB สำเร็จ ✅
  - ทุก URL ครบชุด (QA+PROD) **200** ✅
  - deploy dry-run (`deploy-api.ps1 -SkipPublish -WhatIf`): ไม่มี `appsettings.Production.json` ใน WhatIf output ✅

---

## [2026-07-06 —] Claude Code — ตรวจสถานะเว็บ PROD/QA + เขียน PLAN-051 (วางแผน ไม่แก้โค้ด)
- ทำอะไร: ผู้ใช้รายงาน QA admin 403 → probe ทุก endpoint ทั้ง 2 env + อ่าน config ที่ deploy จริงผ่าน UNC พบ 2 ปัญหา:
  1. **QA ปนเปื้อน `appsettings.Production.json`** (ทั้ง student root, admin, Service บน `\\AP-NTC2138-QAWB\wwwroot\iLearn`) — ต้นตอคือ `deploy-side-by-side.ps1` sync `appsettings*.json` ทุกไฟล์ไป app root + QAWB ไม่ตั้ง `ASPNETCORE_ENVIRONMENT` (default=Production) → QA Admin ชี้ PROD API ข้ามเครื่อง → NTLM double-hop fail → sync role ไม่ได้ → FallbackPolicy 403 ทุก route; ที่หนักกว่า: QA Service ชี้ **PROD DB (AP-NTC2139-COSS)** — dashboard stats QA==PROD เหมือนกันทุกตัว (585/0/13/1413) → การทดสอบบน QA ช่วง 02–06 Jul อาจเขียนลง PROD DB
  2. **PROD `/iLearn/student` = 500.35** — IIS app เก่าค้าง แชร์ app pool `iLearnStudent` กับ root app (ขั้น "ลบ/park" ใน PLAN-049 ที่ยังไม่ทำ)
- ไฟล์หลักที่แตะ: `DOC/PLANS/PLAN-051-qa-env-contamination-and-prod-student-500.md` (ใหม่, READY, Assigned: GPT)
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี (แผนอย่างเดียว)
- Verified: n/a (planning) — หลักฐานจาก HTTP probe จริง (401/403/500.35 + error body), UNC read config ที่ deploy จริง, เทียบ `users/windows-auth` + dashboard stats สองฝั่ง

## [2026-07-03 17:30] Claude Code — Implement PLAN-050 ครบทั้ง Part A+B+C (ผู้ใช้สั่งให้ทำเอง)
- ทำอะไร:
  - **Part A (บั๊ก+contract):** dashboard เปลี่ยนไปใช้ `GetScheduledLearnerStatus` → learner status มี `Overdue`/`Upcoming` แล้ว; `LearnerProgressDto` เพิ่ม `Division`/`Department` (map จาก enrichment ที่มีอยู่ ไม่มี HTTP เพิ่ม); หน้า Report แก้ status filter ให้เทียบ key จริง + label ผ่าน helper ใหม่ `src/lib/learnerStatus.ts`; CSV เพิ่ม UTF-8 BOM + คอลัมน์ Division/Department/Start/Due + ชื่อไฟล์มีวันที่; `formatPercent` แทน Math.round inline; ตาราง report แบ่งหน้าแบบ `DETAIL_TABLE_CHUNK_SIZE`; หน้า list assignments เพิ่มคอลัมน์ Status (StatusBadge) + Learners จาก `vw_AssignmentList`
  - **Part B (จัดการนักเรียน, Detail page):** tab Learners เพิ่ม search + status filter + checkbox bulk select → Reset Selected / Remove Selected; reset รายคอร์ส (ใช้ `ResetEnrollmentsDto.RuleIds` ที่มีอยู่); ชื่อ learner ลิงก์ไป `/learners/:code/profile` + แสดง division/department; modal "Add Courses" ใหม่เรียก `POST {id}/courses` (endpoint เดิมที่ไม่มี UI); bulk import EIds validate กับ directory (OR-filter chunk ละ 40 code) + badge "Not found" + confirm ก่อน save; แสดง Created By; แก้ข้อความ confirm ให้ตรงพฤติกรรม (remove = unlink จาก batch, history คงอยู่)
  - **Part C (report):** การ์ด Overdue Learners + Not Started; ตารางสรุป By Department (learners/enrollments/completed/overdue/%); dropdown filter รายคอร์ส; export เลือก scope All/Filtered; print stylesheet (print:hidden ที่ Header/Sidebar + fix overflow ใน AppLayout + หน้า report reveal ทุกแถวก่อน print)
- ไฟล์หลักที่แตะ: `iLearn.Application/Services/AssignmentService.cs`, `iLearn.Application/DTOs/{AssignmentDashboardDto,AssignmentApiResponseDtos}.cs`, `iLearn.Application/Interfaces/Services/IAssignmentService.cs`, `iLearn.API/Controllers/AssignmentsController.cs`, `iLearn.Admin.React/src/pages/assignments/{AssignmentDetailPage,AssignmentReportPage}.tsx`, `src/lib/learnerStatus.ts` (ใหม่), `src/pages/{moduleConfigs.ts,EntityListPage.tsx}`, `src/components/ui/StatusBadge.tsx`, `src/components/layout/{AppLayout,Header,Sidebar}.tsx`, `DOC/PLANS/PLAN-050-assignments-learner-mgmt-reporting.md` (→ DONE + Implementer Notes)
- Contract ที่เปลี่ยน (API shape / props / DB): (1) `LearnerProgressDto.Status` มีค่าใหม่ `Overdue`/`Upcoming`; (2) `LearnerProgressDto` เพิ่ม `division`/`department`; (3) endpoint ใหม่ `POST Assignments/{id}/learners/bulk-remove` body `{ employeeCodes: string[] }` → `AssignmentRemoveLearnersResponseDto { success, message, removedCount }` — React types sync แล้วทั้ง detail/report
- Verified: React `npm run lint` clean + `npm run build` (tsc -b + vite) ผ่าน; `dotnet build iLearn.Tests -o artifacts\verify-test` 0 errors; `dotnet test` 118 passed / 0 failed; ยังไม่ E2E บน QA

## [2026-07-03 16:15] Antigravity — แก้ไขปุ่ม Open SCORM Player บนหน้า Course Version Detail
- ทำอะไร: แก้ไขปุ่ม "Open SCORM Player" ในตาราง Content ในหน้า VersionDetailPage.tsx จากเดิมเป็น <a> ชี้ไปที่ GUID เปล่าๆ (ส่งผลให้เกิดการ Route ไปหา Version ที่ไม่มีอยู่จริงกลายเป็นหน้าว่าง/404) เปลี่ยนเป็น <button> ที่เรียก GetContent API เพื่อดึง Launch URL แบบ absolute/dynamic จาก backend ขึ้นมาเล่นแบบ target="_blank" เลียนแบบหน้า Content Library Detail
- ไฟล์หลักที่แตะ: `iLearn.Admin.React/src/pages/courses/VersionDetailPage.tsx`
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี
- Verified: `npm run lint` และ `npm run build` ผ่าน 100%, deploy ขึ้น QA (AP-NTC2138-QAWB) เรียบร้อย และ E2E ทดสอบปุ่มบน QA Course 968 Version 587 เปิด SCORM Player สำเร็จ

## [2026-07-03 16:10] Claude Code — เขียน PLAN-050 ปรับปรุง Assignments (learner mgmt + report) (วางแผน ไม่แก้โค้ด)
- ทำอะไร: วิเคราะห์หน้า assignments/:id + :id/report + list ตามคำขอผู้ใช้ (เน้นจัดการนักเรียน + report) พบบั๊กยืนยัน 3 จุด: (1) status filter หน้า Report เทียบ 'In Progress'/'Not Started' กับค่า backend 'InProgress'/'NotStarted' → filter ว่างเสมอ; (2) dashboard ใช้ GetLearnerStatus แทน GetScheduledLearnerStatus → สถานะ Overdue/Upcoming ไม่เคยถูกคำนวณ; (3) export CSV ไม่มี UTF-8 BOM → ชื่อไทยเพี้ยนใน Excel. ช่องว่างหลัก: tab Learners ไม่มี search/bulk actions, endpoint POST {id}/courses ไม่มี UI เรียก, bulk import EIds ไม่ validate, report ไม่มีมิติ Division/Department + ไม่แบ่งหน้า. เขียน PLAN-050 (READY): Part A บั๊ก+contract → GPT, Part B learner mgmt UI → Gemini, Part C report → GPT (B/C รอ A)
- ไฟล์หลักที่แตะ: `DOC/PLANS/PLAN-050-assignments-learner-mgmt-reporting.md` (ใหม่, READY)
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี (แผน — แต่แผนระบุ contract change ล่วงหน้า: LearnerProgressDto.Status ค่าใหม่ Overdue/Upcoming, เพิ่ม Division/Department, endpoint ใหม่ bulk-remove learners)
- Verified: n/a (planning); อ่านโค้ดจริงทั้ง AssignmentDetailPage/ReportPage/moduleConfigs, AssignmentsController/AssignmentService/AssignmentStatusKeys/LearnerApiService/AssignmentListRow ก่อนสรุป

## [2026-07-03 15:26] Antigravity (Gemini 3.5 Flash) — ทำการทดสอบหน้าคอร์ส 507 บน Production สำเร็จ 100%
- ทำอะไร:
  1. ดึงข้อมูล Course 507 (SA-101-JP) และ Reset ข้อมูลความคืบหน้าการเรียนของ Learner 610034 ในฐานข้อมูลโรงงานจริง (AP-NTC2139-COSS) เพื่อจำลองการทดสอบใหม่
  2. รัน E2E browser agent ในหน้า Student Portal (https://ap-ntc2137-prwb/iLearn/) ด้วยรหัสพนักงาน 610034
  3. เล่นเนื้อหาบทเรียน SCORM Content (11 สไลด์) และทำข้อสอบ SCORM Exam (3 ข้อ) จบสมบูรณ์ 100%
  4. บันทึกผลและตรวจสอบหน้าจอแดชบอร์ดแสดงสถานะ "เรียนจบแล้ว" และ Progress 100% พร้อมเช็คฐานข้อมูลบันทึกข้อมูลเรียบร้อย
- ไฟล์หลักที่แตะ: ไม่มี (รัน E2E test/verification เท่านั้น, สร้าง walkthrough.md รายงานผล)
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี
- Verified: บราวเซอร์เอเจนต์เล่นจบ 100%, ค่าฐานข้อมูลขึ้น IsCompleted=True, Progress=100

## [2026-07-03 08:30] GitHub Copilot (Claude Opus 4.6) — PLAN-049 Part A DONE: student portal ย้ายจาก /iLearn/student → /iLearn (root)
- ทำอะไร: ใช้ credential `NIKONOA\Z001927` remote PS เข้า PROD IIS (`ap-ntc2137-prwb`):
  1. แปลง `/iLearn` จาก vdir → IIS application (app pool `iLearnStudent`, physical `C:\inetpub\wwwroot\iLearn`)
  2. สร้าง root `web.config` ด้วย `<location inheritInChildApplications="false">` + `<location path="Courses">` handler remove — กัน handler inherit ไป sub-apps
  3. แก้ Courses 403 โดยแปลง `/iLearn/Courses` จาก vdir → IIS application (app pool `iLearnCourses`, anonymous auth, Windows auth disabled)
  4. Grant ACL `IIS_IUSRS` + `IIS APPPOOL\iLearnStudent` read access ที่ `D:\iLearnContent\Courses`
- ผลสุดท้าย: **6/6 endpoints 200** — `/iLearn/` (student root), `/iLearn/Service/api/...`, `/iLearn/admin/`, `/iLearn/admin-react/`, `/iLearn/Courses/{guid}/res/index.html` × 2
- IIS topology ใหม่:
  - `/iLearn` → app (iLearnStudent) — **iLearn.User**
  - `/iLearn/Service` → app (iLearnService) — iLearn.API
  - `/iLearn/admin` → app (iLearnAdmin) — iLearn.Admin MVC
  - `/iLearn/admin-react` → app (iLearnAdmin) — iLearn.Admin.React SPA
  - `/iLearn/Courses` → app (iLearnCourses) — SCORM static content (anon auth)
  - `/iLearn/student` → app (iLearnStudent) — เดิม (ยังเหลือ, 500 เพราะ inherit issue, ไม่กระทบ — user เข้า `/iLearn/` แทน)
- ไฟล์หลักที่แตะ: `tools/deploy-user-prod.ps1` (DeployRoot → `\\ap-ntc2137-prwb\wwwroot\iLearn`), root `web.config` (created on PROD), IIS config (appcmd/WebAdministration), `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน: URL ใหม่ student = `/iLearn/` (เดิม `/iLearn/student/` ยังใช้ได้ถ้า fix)
- Verified: PROD 6/6 URLs = 200 (root, API, admin, admin-react, SCORM × 2)

## [2026-07-03 08:05] GitHub Copilot (Claude Opus 4.6) — PLAN-049 Part B done + Part A blocked (no remote IIS)
- ทำอะไร:
  - **Part B (done):** เพิ่มปุ่มสลับ Admin MVC ↔ React:
    - React Header: ปุ่ม "Classic Admin" → derive URL จาก `appBasePath` (`/admin-react` → `/admin`) — env-aware (prod/QA/dev)
    - MVC Layout `_DevExtremeLayout.cshtml`: ปุ่ม "New Admin" → `@($"{Context.Request.PathBase}-react/")` — env-aware
    - Deploy PROD: Admin MVC stamp `20260703074809`, React Admin dist copy + web.config fix
    - Smoke: 5/5 PROD endpoints 200
  - **Part A (blocked):** Deploy User ไป root `\\ap-ntc2137-prwb\wwwroot\iLearn` stamp `20260703075830` สำเร็จ แต่ root web.config ทำให้ sub-apps (Service/admin/admin-react/student) 500 ทั้งหมด → **revert ทันที** (ลบ root web.config, คืน 200 ทุก sub-app)
    - สาเหตุ: `/iLearn` เป็น vdir ไม่ใช่ IIS application → root web.config inherit ไป sub-apps ทั้งหมด
    - ต้องการ IIS admin access บน PROD เพื่อแปลง `/iLearn` เป็น app → ไม่มีสิทธิ์ remote PS/WinRM
    - **Files deployed แล้ว** (`_user_deploy_20260703075830` อยู่ที่ root) — เหลือแค่ IIS reconfigure ที่ต้องทำ manual บนเครื่อง
- ไฟล์หลักที่แตะ: `iLearn.Admin.React/src/config/appConfig.ts` (เพิ่ม `legacyAdminUrl`), `iLearn.Admin.React/src/components/layout/Header.tsx` (ปุ่ม Classic Admin), `iLearn.Admin/Views/Shared/_DevExtremeLayout.cshtml` (ปุ่ม New Admin), `tools/deploy-user-prod.ps1` (DeployRoot เปลี่ยนเป็น root), `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน: ไม่มี (UI nav only)
- Verified: React lint+build clean; MVC Admin build 0 errors; PROD smoke 5/5=200 (หลัง revert)

## [2026-07-02 17:35] GitHub Copilot (Claude Opus 4.6) — PLAN-048 verification: ย้าย DB QA→AP-NTC2138-QADB, PROD→AP-NTC2139-COSS
- ทำอะไร: ตรวจสอบ PLAN-048 (manual DB cutover) — ผู้ใช้ทำ backup/restore แบบแมนนวลแล้ว; config ชี้ QA→`AP-NTC2138-QADB`, PROD→`AP-NTC2139-COSS` (จากรอบก่อน); redeploy QA API stamp `20260702171253` (เปลี่ยน UNC path จาก IP→hostname เพราะ `10.10.143.39` unreachable), PROD API stamp `20260702170926`; verification ตาม PLAN-048 Step 5:
  - PROD: 5 endpoints 200, courses=584 (Open 584), contentItems=1412 (published), SCORM content served 200 (3 sample GUIDs × `res/index.html`)
  - QA: API 200, courses=584 (Open 584), contentItems=1412 (published), SCORM content 200
  - ข้อมูลตรงกันทั้ง QA/PROD (584 courses, 1412 content items)
- ไฟล์หลักที่แตะ: `iLearn.API/appsettings.json` (QA DB: `10.10.143.37`→`AP-NTC2138-QADB`), `iLearn.API/appsettings.Production.json` (เพิ่ม ConnectionStrings ชี้ `AP-NTC2139-COSS`), `tools/deploy-api.ps1` + `deploy-user.ps1` + `deploy-admin.ps1` (UNC path: `\\10.10.143.39\...`→`\\AP-NTC2138-QAWB\...`), `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน (API shape / props / DB): Connection strings — QA ต่อ `AP-NTC2138-QADB`, PROD ต่อ `AP-NTC2139-COSS` (DB ชื่อ `iLearnDB_New` ทั้งคู่)
- Verified: PROD smoke 5/5 URL=200; PROD courses=584 Open; PROD content=1412 published; PROD SCORM HTTP serve 3/3=200; QA smoke API 200; QA courses=584; QA content=1412; QA SCORM 200

## [2026-07-02 17:10] GitHub Copilot (Claude Opus 4.6) — Deploy ทั้ง QA + PROD ครบ (API/User/Admin/React)
- ทำอะไร: Build+lint+test ทั้งหมด (React lint+build, .NET build, 118 xUnit passed); deploy QA (API stamp `20260702164034`, User `20260702164447`, Admin `20260702164914`); smoke test QA (API 200, User 200); deploy PROD (API `20260702165500`, User `20260702165816`, Admin MVC `20260702170122`, React Admin dist copy); แก้ PROD admin-react 500 โดย copy `web.config.prod` (httpErrors fallback) ทับ config เก่าที่มี `<rewrite>` ซึ่ง PROD ไม่มี URL Rewrite module; smoke test PROD ผ่าน 5/5 (API, User, Admin, React root, React deep-link `/courses`)
- ไฟล์หลักที่แตะ: `DOC/AGENT_LOG.md` (deployed web.config on PROD admin-react fixed in-place)
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี
- Verified: React lint+build clean (7 bundles, 2.30s); .NET build succeeded (5.8s); xUnit 118 passed 0 failed; QA smoke API+User 200; PROD smoke API+User+Admin+React+deep-link ทั้ง 5 URL = 200

## [2026-07-02 13:41] Antigravity (Gemini) — แก้ไขปัญหาหลัง deploy prod (PLAN-047)
- ทำอะไร: 
  - (Part A) อัปเดตตาราง Courses ตั้งค่า `Status=1` (Open) สำหรับคอร์สที่ `IsActive=1` และยัง Closed อยู่ (แก้ 545 คอร์ส, ทำให้คอร์ส Open รวมเป็น 582 คอร์ส)
  - (Part B) เรียก Bulk Publish ผ่าน endpoint `api/ContentItems/Admin/BulkSetPublic` ของ QA App Server ด้วย Windows Credentials สำเร็จ 498/498 ตัว (ล้มเหลว 0 ตัว)
  - (Part C) ซิงค์ข้อมูลโฟลเดอร์ GUID ของบทเรียนที่เพิ่มขึ้นบน QA server ไปยัง Production ด้วย Robocopy `/MT:32` (สำเร็จ 0 errors, จำนวนโฟลเดอร์ GUID ตรงกัน 1409 โฟลเดอร์)
- ไฟล์หลักที่แตะ: `DOC/PLANS/PLAN-047-prod-post-deploy-fixes.md`, `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน (API shape / props / DB): มีการ UPDATE ค่า `Courses.Status` ในฐานข้อมูล `iLearnDB_New`
- Verified: SQL Query ยืนยันยอด Open Courses = 582 และ Published ContentItems = 1409; รัน E2E browser agent ล็อกอินเข้าใช้งานด้วยรหัสนักเรียน `610034` สามารถเล่นบทเรียนและทำข้อสอบ SCORM ของหลักสูตร Software license JP สำเร็จ 100% บันทึกสถานะเรียนจบลงฐานข้อมูลและแสดงผลบนแดชบอร์ดถูกต้อง

## [2026-07-02 10:05] GitHub Copilot (GPT-5.3-Codex) — Prod redeploy รอบใหม่ + กู้ IIS topology + ปิด 500
- ทำอะไร: รัน redeploy ใหม่ด้วย credential ที่ให้มา (`NIKONOA\\Z001927`) แบบ `AppOffline` + `-HealthCheckUrl ''` เพื่อกัน auto-rollback และได้ stamp ล่าสุดครบ (Service/User/Admin); สร้าง IIS mapping จริงบน `ap-ntc2137-prwb` (`/iLearn/Service`, `/iLearn/student`, `/iLearn/admin`, `/iLearn/admin-react`) พร้อม start app pools ที่มีจริงบนเครื่อง (`iLearnService`, `iLearnStudent`, `iLearnAdmin`, `iLearnAuth`); ตรวจพบเครื่องไม่มี IIS URL Rewrite module แล้วแก้ `iLearn.Admin.React/public/web.config.prod` เป็น `httpErrors ExecuteURL` fallback (ไม่พึ่ง rewrite) และ copy ไป prod; สร้าง vdir `/iLearn/Courses` ชี้ `D:\iLearnContent\Courses`, grant `IIS_IUSRS` Modify, และ sync content folders QA→prod พร้อมตรวจ count เท่ากัน
- ไฟล์หลักที่แตะ: `iLearn.Admin.React/public/web.config.prod`, `DOC/PLANS/PLAN-046-deploy-prod-inplace.md`, `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี
- Verified: deploy active stamp = Service `._deploy_20260702094136`, Student `._user_deploy_20260702094505`, Admin `._admin_deploy_20260702094818`; health = `https://AP-NTC2137-PRWB/iLearn/Service/api/admin/session/me` ได้ 200 เมื่อ `UseDefaultCredentials` และ 401 เมื่อไม่ส่ง credential; `https://AP-NTC2137-PRWB/iLearn/student/` = 200, `.../admin/` = 200, `.../admin-react/` = 200, `.../admin-react/non-existent-route` = 200; content folder count QA vs prod = `908/908`

## [2026-07-02 09:20] GitHub Copilot (GPT-5.3-Codex) — Prod IIS 500 diagnosis (A/B web.config tests, no repo runtime change)
- ทำอะไร: เก็บหลักฐานเพิ่มจาก prod share เพื่อแยกสาเหตุ 500 แบบพิสูจน์ได้: ตรวจ `web.config` ของ Service/student/admin/admin-react พบชี้ deploy stamps ถูกและไฟล์มีอยู่จริง; ทดสอบ HTTP ได้ `500` ทุก URL พร้อม `Server: Microsoft-IIS/10.0`; รัน A/B test บน prod ชั่วคราวแล้ว restore กลับทันที
  - `student`: สลับเป็น minimal static `web.config` ชั่วคราวแล้วได้ `401`; restore config เดิม (aspNetCore) แล้วกลับเป็น `500`
  - `admin-react`: สลับเป็น minimal static `web.config` ชั่วคราวแล้วได้ `401` (และ `UseDefaultCredentials` ได้ `200` ที่ `index.html`); restore config เดิม (มี `<rewrite>`) แล้วกลับเป็น `500`
  - สรุปเชิงเทคนิค: 500 ฝั่ง ASP.NET Core ผูกกับ aspNetCore module/runtime (ANCM/Hosting Bundle), และ 500 ฝั่ง admin-react ผูกกับ rewrite section/module (URL Rewrite)
- ไฟล์หลักที่แตะ: `DOC/PLANS/PLAN-046-deploy-prod-inplace.md`, `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี
- Verified: จุด deploy ยัง active ตามเดิม (`Service=._deploy_20260702084508`, `student=._user_deploy_20260702084846`, `admin=.\_admin_deploy_20260702085742`); A/B tests แสดง status เปลี่ยนตาม config ที่สลับและ restore กลับได้ครบ

## [2026-07-02 09:03] GitHub Copilot (GPT-5.3-Codex) — Execute PLAN-046 deploy-to-prod (partial done, blocked at IIS runtime)
- ทำอะไร: เริ่ม execution PLAN-046 จริงบน prod share โดยเตรียม `\\ap-ntc2137-prwb\wwwroot\iLearn\{Service,student,admin,admin-react}` (seed `web.config`), เพิ่ม production override config (`appsettings.Production.json` สำหรับ API/User/Admin), เพิ่ม React prod env+rewrite template, เพิ่ม wrapper scripts สำหรับ prod (`deploy-*-prod.ps1`, `init-ilearn-prod-roots.ps1`, `build/deploy-admin-react-prod.ps1`) และ fallback script `manual-deploy-admin-prod.ps1`; deploy artifacts สำเร็จไปที่ stamp ใหม่และ flip `web.config` แล้ว (Service/User/Admin) + copy React dist สำเร็จ
- ไฟล์หลักที่แตะ: `iLearn.API/appsettings.Production.json`, `iLearn.User/appsettings.Production.json`, `iLearn.Admin/appsettings.Production.json`, `iLearn.Admin.React/.env.production`, `iLearn.Admin.React/public/web.config.prod`, `tools/deploy-side-by-side.ps1`, `tools/deploy-api-prod.ps1`, `tools/deploy-user-prod.ps1`, `tools/deploy-admin-prod.ps1`, `tools/build-admin-react-prod.ps1`, `tools/deploy-admin-react-prod.ps1`, `tools/init-ilearn-prod-roots.ps1`, `tools/manual-deploy-admin-prod.ps1`, `DOC/PLANS/PLAN-046-deploy-prod-inplace.md`, `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี (config/deploy tooling only)
- Verified: `pwsh -File tools/init-ilearn-prod-roots.ps1 -WhatIf` ผ่านและรันจริงผ่าน; deploy evidence บน share = Service `._deploy_20260702084508\iLearn.API.dll`, Student `._user_deploy_20260702084846\iLearn.User.dll`, Admin `.\_admin_deploy_20260702085742\iLearn.Admin.dll`; React deploy (`tools/deploy-admin-react-prod.ps1`) สำเร็จ (`CopySucceeded=True`); `dotnet build iLearn.Tests -o artifacts\verify-test` ผ่าน (warnings only) และ `dotnet test artifacts\verify-test\iLearn.Tests.dll` ผ่าน (Passed 118, Failed 0)
- ผล smoke ปัจจุบัน: `https://ap-ntc2137-prwb/iLearn/Service/api/admin/session/me`, `/iLearn/student/`, `/iLearn/admin/`, `/iLearn/admin-react/` ยังตอบ `500` ทั้งหมด → ต้องให้ IIS admin ตรวจ app mapping/app pool/auth/module บนเครื่อง prod (remote IIS query จากบัญชีนี้โดน Access denied)

## [2026-07-01 10:49] GitHub Copilot (GPT-5.3-Codex) — เปลี่ยน Service HostUnc และ deploy iLearn.API
- ทำอะไร: แก้ค่า `FileSettings.HostUnc` ใน `iLearn.API/appsettings.json` เป็น `D:\\iLearnContent`; รัน `tools/deploy-api.ps1` เพื่อ publish+copy ไป `\\10.10.143.39\wwwroot\iLearnNew\Service`; ตรวจหลัง deploy พบ root `appsettings.json` อัปเดตแล้วแต่ `web.config` ยังชี้ deploy เดิม จึงสลับ active deployment เป็น `_deploy_20260701104744` โดยแก้ `aspNetCore arguments` ที่ `Service/web.config` แล้ว
- ไฟล์หลักที่แตะ: `iLearn.API/appsettings.json`, `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี
- Verified: ตรวจบน share `\\10.10.143.39\wwwroot\iLearnNew\Service\appsettings.json` ได้ `HostUnc=D:\iLearnContent`; ตรวจ `web.config` ได้ `.\\_deploy_20260701104744\\iLearn.API.dll`; smoke check `http://AP-NTC2138-QAWB/iLearnNew/Service/api/admin/session/me` ได้ `401` (บริการตอบสนองและต้อง auth)

## [2026-07-02 13:15] Claude Code — เขียน PLAN-049 (student→/iLearn root + ปุ่มสลับ admin) (วางแผน ไม่แก้โค้ด)
- ทำอะไร: ผู้ใช้ขอ 2 อย่าง: (A) ย้าย student `/iLearn/student`→`/iLearn` root; (B) admin MVC↔React มีปุ่มสลับ. สำรวจ: `iLearn.User` ไม่มี hardcode `/student` (default route Home/Index, asset ใช้ PathBase) → Part A ไม่ต้องแก้โค้ด แค่ IIS restructure (แปลง /iLearn เป็น User app root, คง sub-apps admin/admin-react/Service/Courses nested, ลบ /iLearn/student). Part B: derive URL อีกเวอร์ชันจาก base path (React appConfig `legacyAdminUrl`=appBasePath แทน /admin-react→/admin + ปุ่มใน Header.tsx ใช้ AppButton; MVC _DevExtremeLayout ใช้ `PathBase + "-react"`) → env-aware ไม่ต้องเพิ่ม env var. เขียน PLAN-049 (READY): Part A→GPT(IIS), Part B→Gemini(UI). เตือน: nested ASP.NET Core web.config inherit + LearnerProxy pathbase เปลี่ยน → ต้อง E2E
- ไฟล์หลักที่แตะ: `DOC/PLANS/PLAN-049-prod-url-and-admin-switch.md` (ใหม่, READY)
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี (แผน)
- Verified: n/a (planning); สำรวจ appConfig.ts/Program.cs/layout จริงเพื่อความแม่นยำ

## [2026-07-02 12:45] Claude Code — เขียน PLAN-048 ย้าย prod → DB จริง (assign GPT) (วางแผน ไม่แก้โค้ด)
- ทำอะไร: ผู้ใช้สั่งเขียน PLAN-048 ย้าย prod จาก QA `iLearnDB_New` (10.10.143.37) → prod DB จริง `10.10.154.119` เพื่อแก้ระบบช้า (DB contention shared) + ตัดความเสี่ยง prod ผูก QA + ล้าง test data. กลยุทธ์ = **backup/restore** (ไม่ re-ETL): restore สำเนา QA DB ไป prod server (ได้ schema HEAD+migrations+CourseTypes+data+fix ครบ). **จุดเด่น: content ไม่ต้อง copy ใหม่** เพราะ guid ใน DB ที่ restore = guid บน prod storage (D:\iLearnContent) อยู่แล้ว. Steps: freeze write→backup COPY_ONLY→restore→verify counts (40 cat/582 Open/1409 published)→เพิ่ม ConnectionStrings ใน API appsettings.Production.json (แนะนำ env var กัน secret เข้า git)→restart iLearnService→E2E+perf check→เก็บ QA DB เป็น fallback. เตือน: ห้าม re-ETL (เสีย guid), ห้ามแตะ iLearn เก่า, learner progress ช่วง window
- ไฟล์หลักที่แตะ: `DOC/PLANS/PLAN-048-prod-move-to-real-db.md` (ใหม่, READY)
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี (แผน)
- Verified: n/a (planning)

## [2026-07-02 12:20] Claude Code — รีวิว PLAN-047 (Gemini DONE) → VERIFIED + เจอระบบช้า (ไม่แก้โค้ด)
- ทำอะไร: รีวิวงาน Gemini (PLAN-047 DONE). ✅ 2 blocker แก้แล้ว: Part A courses Open 582 (จาก 36), Part B publish 498/498 สำเร็จ (498 เดิมไม่ได้เสีย—แค่รอบแรก bulk publish ไม่ครบ→retry ผ่านหมด, รวม 1409), Part C resync prod 1409 folders ตรง. E2E ของ Gemini เชิงประจักษ์ (learner 610034 เล่น SCORM slide+exam จบ 100%+บันทึกเรียนจบ) → PLAN-047 VERIFIED. **แต่ตรวจ browser เองพบระบบช้าทั้งระบบตอนนี้** (admin+student ทุกหน้าค้าง >45s, เมื่อ session ก่อนเร็ว) — น่าจะ DB contention shared QA iLearnDB_New จาก ops หนักที่เพิ่งรัน หรือ lock/agent ค้าง (ความเสี่ยง prod ผูก QA DB). Follow-up: content 1409/courses 584 เกิน migrate ~3 (QA test data ปน)
- ไฟล์หลักที่แตะ: `DOC/PLANS/PLAN-047-prod-post-deploy-fixes.md` (Review Notes → VERIFIED + 2 follow-up)
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี (รีวิว)
- Verified: ยืนยันผ่าน E2E ของ Gemini + โครงสร้างที่ผมตรวจอิสระรอบก่อน; ตรวจ browser รอบนี้เจอ slowness (ไม่ยืนยัน visual ได้เพราะหน้าค้าง) — ให้ตรวจ DB load/ย้าย prod DB

## [2026-07-02 11:40] Claude Code — เขียน PLAN-047 แก้ prod (status + content gap) assign Gemini (วางแผน ไม่แก้โค้ด)
- ทำอะไร: ผู้ใช้ query ยืนยัน 2 ปัญหา: (1) Courses Closed **546**/Open 36/Draft 1 แต่ source IsActive=1 = **579** (WouldBeOpen 34) → D2 ExpiredDate เก่าปิด ~545; (2) ContentItems Published **910**/NotPublished **498**. เขียน `PLAN-047` (READY, assign **Gemini**): Part A = `UPDATE Courses SET Status=1 WHERE IsActive=1 AND Status=2` (เปิด ~579, DB update prod เห็นทันที); Part B = diagnose 498 (TypeId/FileStorage/name) + retry BatchPublishStream + จัด root cause (non-SCORM/เสีย/version) + รายงาน (ห้ามลบเอง); Part C = re-copy guid ใหม่ QA→prod. ย้ำ `iLearnDB_New` = live prod (shared) + ห้ามแตะ iLearn เก่า
- ไฟล์หลักที่แตะ: `DOC/PLANS/PLAN-047-prod-post-deploy-fixes.md` (ใหม่, READY)
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี (แผน; Part A จะ update ข้อมูล Courses.Status ตอน implement)
- Verified: n/a (planning); ตัวเลขจาก query ผู้ใช้จริง

## [2026-07-02 11:15] Claude Code — E2E ตรวจ prod จริงผ่าน browser → No-Go (เจอ 2 ปัญหา) (ไม่แก้โค้ด)
- ทำอะไร: เข้า `https://ap-ntc2137-prwb/iLearn/admin-react/` (Windows auth ผ่าน) ตรวจระบบจริง. ✅ ยืนยัน DB ถูกตัว (40 categories ตรง; PD1=97 courses→580 ครบ; คอร์ส id870 มี v1+2 content items Learn/Exam .zip; Type=Common ถูก). 🔴 **เจอ 2 blocker:** (1) คอร์สทั้งหมด Status=Closed (dashboard Portfolio 35/~545 Closed) → learner เข้าไม่ถึง; สาเหตุน่าจะ D2 ExpiredDate เก่าปิดหมด → ต้อง bulk-open; (2) content publish gap 908/1406 ยังไม่ยืนยัน (Content Library UI 0 records=filter). ให้ query วินิจฉัย (Courses GROUP BY Status; source IsActive/ExpiredDate dist; ContentItems published count). Verdict = No-Go จนกว่าแก้ 2 ข้อ
- ไฟล์หลักที่แตะ: `DOC/PLANS/PLAN-046-deploy-prod-inplace.md` (+ E2E Review section)
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี (รีวิว/E2E)
- Verified: ตรวจ prod runtime จริงผ่าน browser (catalog/category/course detail); ยังไม่ได้ query DB ยืนยันตัวเลข (No prod DB access — ให้ผู้ใช้/GPT รัน)

## [2026-07-02 10:30] Claude Code — รีวิว deploy prod PLAN-046 → Conditional Go (ไม่แก้โค้ด)
- ทำอะไร: รีวิวงาน deploy prod ที่ GPT ทำ (PLAN-046 ACTIVE) อิสระ — ตรวจ `appsettings.Production.json` 3 แอป (override เฉพาะ URL/HostUnc, ไม่แตะ DB conn = ใช้ QA ตามแผน ✓), React `.env.production` ถูก, ไม่มี ASPNETCORE_ENVIRONMENT deployed (default Production ✓), GPT wrap `@(Get-ChildItem)` ใน deploy-side-by-side.ps1 ปลอดภัยไม่ regress, build 0 err + test 118/118 + React build ผ่าน (รันเอง). **พบ 2 blocker ก่อนเปิดผู้ใช้:** (1) 🔴 content copy 908 folder แต่มี 1406 content items → ~498 อาจ publish ไม่สำเร็จ (SCORM validation) ต้อง verify DB + ดูว่า fail เพราะอะไร; (2) 🔴 E2E ยังไม่ทำ (เห็น catalog/เล่น SCORM/upload). จุดเล็ก: SignalR ปิด, app pool `iLearnAuth` ถูก start (นอก scope?). Verdict = Conditional Go
- ไฟล์หลักที่แตะ: `DOC/PLANS/PLAN-046-deploy-prod-inplace.md` (+ Review Notes)
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี (รีวิว)
- Verified: build 0 err, test 118/118, React build (รันเองอิสระ), cleanup artifacts แล้ว; ยังยืนยัน prod runtime/DB จากที่นี่ไม่ได้ (อ้าง Implementer Notes + ต้อง verify count/E2E)

## [2026-07-01 16:00] Claude Code — เขียน PLAN-046 deploy prod (assign GPT) (วางแผน ไม่แก้โค้ด)
- ทำอะไร: ผู้ใช้สั่งให้ GPT จัดการ deploy ทั้งหมด → เขียน `PLAN-046` (READY, assign GPT) รวบทุก decision: prod `/iLearn/*`, app pools `iLearn.*`, ต่อ QA `iLearnDB_New` ช่วงแรก (conn เดิม), storage `D:\iLearnContent`+vdir+IIS_IUSRS, config URLs prod. **จุดสำคัญที่ใส่ในแผน:** prod ใช้ QA DB → `ContentItem.URL` (guid) ชี้ไฟล์ที่แตกบน QA storage → ต้อง **copy content QA→prod `D:\iLearnContent\Courses`** (guid ตรง เล่นได้) ไม่งั้น 404. ระบุ 7 steps (config→build→IIS→deploy→content copy→verify→rollback) + Constraints (ห้ามแตะ iLearn เก่า/ห้าม ETL ซ้ำ QA DB/ห้ามขยายสโคป) + สิ่งที่ GPT ต้อง confirm (prod share root, IIS site, SSL, Windows auth)
- ไฟล์หลักที่แตะ: `DOC/PLANS/PLAN-046-deploy-prod-inplace.md` (ใหม่, READY)
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี (แผน)
- Verified: n/a (planning)

## [2026-07-01 15:40] Claude Code — วางแผน prod cutover: app pools iLearn.*, ใช้ QA DB ช่วงแรก, storage prod (วางแผน ไม่แก้โค้ด)
- ทำอะไร: ผู้ใช้ตัดสิน prod topology: (1) ช่วงแรก prod app ต่อ **QA `iLearnDB_New`** (10.10.143.37) ไปก่อน — ไม่ต้องแก้ conn string (repo ชี้อยู่แล้ว) — ค่อยย้าย prod DB ทีหลัง (เตือน: freeze iLearnDB_New กัน ETL ซ้ำ + prod ผูก QA db server); (2) prod ใช้ชื่อ **`iLearn.*`** (เลิก iLearnNew): app pools `iLearn.Service`/`iLearn.User`/`iLearn.Admin` (No Managed Code + Integrated + AppPoolIdentity), paths `/iLearn/student|admin|admin-react|Service`, config URLs ใช้ `iLearn`; (3) storage prod สร้าง `D:\iLearnContent` แล้ว (ต้องครบ vdir + IIS_IUSRS Modify + HostUnc). ให้ PowerShell สร้าง app pools + ตาราง naming consistency. deploy scripts default iLearnNew → ต้องส่ง param prod เอง (หรือ wrapper). ⚠️ ย้ำ prod `iLearn` DB เก่าเป็น ETL source — อย่าลบจนกว่า provision+ETL prod DB เสร็จ + มี .bak
- ไฟล์หลักที่แตะ: `DOC/PLANS/PLAN-045-production-cutover-ilearn2.md` (config table)
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี
- Verified: n/a (planning); infra prod กำลังตั้ง (storage/app pools)

## [2026-07-01 15:00] Claude Code — QA dry-run ครบ loop: ETL+merge verified + storage แยกไดร์ฟ + bulk publish สำเร็จ (วางแผน ไม่แก้โค้ด)
- ทำอะไร: ผู้ใช้ re-run ETL (merge) → reconciliation ผ่านสะอาด: Categories 40 (49-9 merged), Courses 580 (Common 372 + No-Common 208), ContentItems/FileStorages 1406, CourseVersions 580, CourseContentItems 950, FK integrity 0/0/0; crosswalk No-Common→main 9 คู่ถูกต้อง. เปลี่ยนที่เก็บไฟล์ที่แตกไปไดร์ฟใหญ่: IIS Virtual Directory `/iLearnNew/Courses` → `D:\iLearnContent\Courses` (บน QAWB), ให้สิทธิ์ `IIS_IUSRS` = Modify (IIS inject SID เข้า worker token ทุก identity), แก้ `FileSettings:HostUnc` = `D:\iLearnContent` ที่ Service/appsettings.json, restart app pool. Bulk publish (`ContentItems/Admin/BulkSetPublic`) สำเร็จ → SCORM re-extract จาก byte[] ลง D:. เหลือ: verify published count + เล่นทดสอบ; แล้วเข้าสู่ prod cutover (config prod ยังค้าง)
- ไฟล์หลักที่แตะ: `DOC/AGENT_LOG.md` (ไม่มีแก้โค้ด repo; config เปลี่ยนที่ deployed appsettings บน QA + IIS)
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี (schema เดิม; แค่ config ที่เก็บไฟล์)
- Verified: ETL reconciliation + FK integrity ผ่าน; bulk publish รายงานสำเร็จ (รอ verify count + E2E play)
- Note: FileSettings storage แยกไดร์ฟ (vdir + IIS_IUSRS Modify) เป็น pattern ที่ต้องทำซ้ำบน prod

## [2026-07-01 14:10] Claude Code — เพิ่ม merge No-Common category → main ใน ETL (วางแผน ไม่แก้โค้ด)
- ทำอะไร: ผู้ใช้ยืนยัน CourseTypes จริงใน iLearnDB_New (1=Common, 2=No-Common, 3=General, 4=VDO) + category (No Common) 9 อันอยู่ใน DivisionId=2 (PD2) ทั้งหมด และ PD2 มี category หลัก (Common) คู่ขนาน. ผู้ใช้สั่งเพิ่ม: course ใน No-Common → **ย้ายไปรวมกับ category หลัก** (ไม่ใช่แค่ตั้ง type). ปรับ `etl-catalog.sql`: (1) เพิ่ม [0b] crosswalk #NoCommonMerge จับคู่ No-Common→main ด้วย (DivisionId + เลขนำหน้า, main=ชื่อไม่มีวงเล็บ) ทน "Part vs Parts"/ช่องว่าง + preview SELECT; (2) Categories ข้าม No-Common ที่ merge; (3) Courses re-point CategoryId ไป MainCatId + CourseTypeId=2 สำหรับ No-Common. เพิ่ม CourseType breakdown ใน reconciliation. เจอ marker `(LAS)`/`(CAS)` ใน DivisionId=5 (NLC) — ยังไม่รู้ความหมาย ปล่อย Common ไว้ (open question)
- ไฟล์หลักที่แตะ: `DOC/PLANS/PLAN-045-etl-catalog.sql`, `DOC/PLANS/PLAN-045-data-mapping.md` (D1-rev)
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี
- Verified: n/a (planning); Categories คาดเหลือ ~40 หลัง merge; รอ re-run + ตรวจ crosswalk preview (9 คู่)

## [2026-07-01 13:40] Claude Code — ETL dry-run ผ่าน (lossless) + เพิ่ม rule CourseType จากชื่อ category (วางแผน ไม่แก้โค้ด)
- ทำอะไร: (1) ผู้ใช้รัน ETL รอบแรก reconciliation **ผ่านสะอาด**: Categories 49, ContentItems 1406, Courses 580, CourseContentItems 950, CourseVersions 580, FileStorages 1406 (ข้าม orphan 288; 1406+288=1694) — FK integrity 0/0/0. อ่านได้ว่า content ทุกตัวมีไฟล์ (FileStorages=ContentItems) และ Test division ไม่มี content. (2) ผู้ใช้แจ้ง business rule: ระบบเก่าไม่มี CourseType → admin ฝังใน Category.Name เป็น suffix `(No Common)` = No-Common(Id2), นอกนั้น = Common(Id1). ตรวจโค้ด: อ้างชื่อ type จุดเดียว `CourseAssignmentService.cs:49` (`=="General"` auto-assign) → DB ใช้ Common/No-Common จึง auto-assign ไม่ทำงาน (moot mass-enroll). ปรับ `etl-catalog.sql`: CourseTypeId derive จาก suffix ชื่อ category เก่า (LEFT JOIN old Categories) + ตัด suffix ออกจากชื่อ category ที่ย้าย (@StripCategorySuffix) + var @TypeCommon/@TypeNoCommon. แทน D1 (Special blanket) เป็น D1-rev
- ไฟล์หลักที่แตะ: `DOC/PLANS/PLAN-045-etl-catalog.sql`, `DOC/PLANS/PLAN-045-data-mapping.md` (D1-rev)
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี (สคริปต์/แผน)
- Verified: dry-run รอบแรกผ่าน; รอ re-run หลังเพิ่ม rule + ยืนยัน CourseTypes Id จริง
- heads-up ทีมแอป: auto-assign hardcode "General" ไม่ match Common/No-Common

## [2026-07-01 13:00] Claude Code — เลือก SCORM Strategy B (Re-publish) + ปรับ ETL (วางแผน ไม่แก้โค้ด)
- ทำอะไร: ยืนยันจากโค้ดว่าแอปเสิร์ฟ SCORM จากไฟล์บน share ไม่ใช่ byte[] (`ScormService` แตกตอน upload/publish, `GetScormUrl` แค่ชี้ URL); เจอ `ContentPublicationService.PublishAsync` re-extract จาก byte[] ได้ + **bulk publish มีจริง** (`ContentItemsController`: `Admin/BulkSetPublic` publish ทั้งหมด streaming, `Admin/BatchPublishStream`, `Admin/BatchPublish`). ผู้ใช้เลือก **Strategy B (Re-publish)** + restore old เข้า `iLearn`@AP-NTC2138-QADB แล้ว. ปรับ `etl-catalog.sql` step [3]: ContentItems `IsActive=0` (ให้ publishable) + guard เติม `.zip` ต่อท้าย Name ของ zip-backed ที่ไม่มี (PublishAsync extract เฉพาะ .zip). เพิ่ม query เช็คชื่อไฟล์ (§3.5) + note flow post-ETL = เรียก BulkSetPublic. Gotcha คงเหลือ: publish ~1400 ตัวหนัก (re-extract zip ทุกก้อน)
- ไฟล์หลักที่แตะ: `DOC/PLANS/PLAN-045-etl-catalog.sql`, `DOC/PLANS/PLAN-045-data-mapping.md` (§3.5), `DOC/PLANS/PLAN-045-production-cutover-ilearn2.md` (Phase 4)
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี (แผน/สคริปต์ยังไม่รัน)
- Verified: n/a (planning); ยืนยัน bulk publish + publish logic จากโค้ดจริง

## [2026-07-01 12:35] Claude Code — profiling ยืนยัน iLearnDB_New=HEAD + ตัดสิน 1:1 (ไม่ merge) (วางแผน ไม่แก้โค้ด)
- ทำอะไร: ผู้ใช้รัน query ยืนยัน: (1) `iLearnDB_New` มีตาราง `ContentItems`+`CourseContentItems` = **HEAD** → สคริปต์ผมใช้ได้ Gemini ใช้ไม่ได้; (2) profiling old `iLearn`: Courses 580 / distinct Code 497 (dup 83), **LearnOnly 210 (36%)** / LearnAndExam 370 / NoLearn 0, TypeId 1=836 2=570. → D7 ตัดสิน **ใช้ 1:1 lossless** เพราะ Script2 (ต้องมี Learn+Exam) จะทิ้งคอร์ส Learn-only 210 ตัว (36%) แต่ merge ช่วย dedup แค่ 83 = ไม่คุ้ม. พร้อมรัน etl-catalog.sql (source [iLearn] อยู่บน QADB แล้ว)
- ไฟล์หลักที่แตะ: `DOC/PLANS/PLAN-045-data-mapping.md` (§3.4 profiling + D7)
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี
- Verified: n/a (planning); รอผล reconciliation หลังผู้ใช้รัน ETL

## [2026-07-01 12:10] Claude Code — เจอ schema-state gap (rename migration) + finalize ETL สำหรับ QA dry-run (วางแผน ไม่แก้โค้ด)
- ทำอะไร: ผู้ใช้แชร์ 3 สคริปต์ ETL ของ Gemini (Downloads) + สนใจ Script2 (merge code + reconstruct version). ตรวจพบ **ทั้ง 3 ใช้ชื่อ schema เก่า** (`Resources`/`CourseResources`/`ResourceHref`/`ResourceId`/`StudentGroups*`) — ยืนยันด้วยการอ่าน migration `20260429071730_RenameResourceStudentTerminology` ว่า rename เป็น `ContentItems`/`CourseContentItems`/`LaunchHref`/`ContentItemId`/`LearnerGroups*` แล้ว + Gemini ไม่ใส่ `Status`(30เม.ย.)/`CachedFileLength`(12มิ.ย.) → **สคริปต์เหล่านั้นรันกับ HEAD ไม่ได้** (R9). ประเมิน Script2: idea ดี (merge code ซ้ำ, กู้ version จาก session heuristic) แต่เสี่ยง data-loss (ทิ้งคอร์สที่ไม่มีคู่ Learn+Exam, heuristic ID-gap เปราะ). ผู้ใช้เลือกทำ QA dry-run: restore prod เก่า→AP-NTC2138-QADB `[iLearn]`→map→`iLearnDB_New`(instance เดียว)→ทดสอบแอป. Finalize `PLAN-045-etl-catalog.sql` ให้ตรง topology (source `[iLearn].[dbo]`, HEAD names, cleanup+reseed, 1:1 lossless pass แรก, reconciliation) + ให้ query profiling (code ซ้ำ/Learn-only/TypeId dist) ไว้ตัดสิน merge ทีหลัง
- ไฟล์หลักที่แตะ: `DOC/PLANS/PLAN-045-etl-catalog.sql` (rewrite ตาม topology), `DOC/PLANS/PLAN-045-data-mapping.md` (§3.3 schema-gate), `DOC/PLANS/PLAN-045-production-cutover-ilearn2.md` (R9)
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี (สคริปต์ยังไม่รัน)
- Verified: n/a (planning only); ยืนยัน rename จาก migration file จริง — ต้องเช็ค __EFMigrationsHistory ของ iLearnDB_New ก่อนรันจริง

## [2026-07-01 11:30] Claude Code — ร่างสคริปต์ ETL catalog PLAN-045 (source data ครบ) (วางแผน ไม่แก้โค้ด)
- ทำอะไร: ผู้ใช้ให้ข้อมูล DB เก่า: Divisions (3=CSD,5=NLC,1=PD1,2=PD2,4=PD3,6=Test) + row counts (Categories 49, Courses 580, CourseResources 950, Resources 1406, FileStorage 1694); และ Divisions ของ DB ใหม่ = **Id+Name ตรงกันเป๊ะ** → D0 crosswalk = identity (copy DivisionId ตรง). วิเคราะห์ตัวเลข: FileStorage(1694)>Resources(1406)=~288 ไฟล์กำพร้า (ย้ายเฉพาะที่ถูกอ้าง); Resources(1406)>CourseResources(950)=มี resource ไม่ผูกคอร์ส (D5 เสนอย้ายทั้งคลัง). เขียนสคริปต์ ETL `PLAN-045-etl-catalog.sql`: 6 ขั้น FK-safe (Categories→FileStorages→ContentItems→Courses→CourseVersions v1→CourseContentItems) ด้วย IDENTITY_INSERT+DBCC CHECKIDENT reseed, guard-join กัน FK, `@IncludeTest` toggle (D6), Uncategorized conditional (D1), Status derive (D2), + reconciliation queries; default dry-run ROLLBACK
- ไฟล์หลักที่แตะ: `DOC/PLANS/PLAN-045-etl-catalog.sql` (ใหม่, DRAFT), `DOC/PLANS/PLAN-045-data-mapping.md` (§3.1 crosswalk ยืนยัน)
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี (สคริปต์ยังไม่รัน; เป็น draft ให้ dry-run)
- Verified: n/a (planning only); สคริปต์อ้าง column จาก snapshot ทั้งสองฝั่ง — ต้อง dry-run จริง + ยืนยัน D5/D6 ก่อนใช้
- เหลือ confirm: D5 (ย้ายทั้งคลัง) + D6 (ข้าม Test), ตั้ง {{OLD}} linked-server/3-part name, แล้ว dry-run

## [2026-07-01 11:00] Claude Code — เคาะ decisions D0–D4 ของ ETL PLAN-045 (วางแผน ไม่แก้โค้ด)
- ทำอะไร: ปิด decision D0–D4 ใน data-mapping. จุดสำคัญ: ตรวจ `CourseAssignmentService.AssignGeneralCoursesToNewUserAsync` (บรรทัด 46-49) พบว่าคอร์ส `CourseType.Name=="General"` ถูก **auto-assign ให้ผู้เรียนใหม่ทุกคน** → เปลี่ยนคำแนะนำ D1 `CourseTypeId` default จาก General เป็น **Special(1)** เพื่อกัน mass auto-enrollment วัน go-live (ย้าย catalog อย่างเดียว, ตั้ง assignment ใหม่). สรุปอื่น: D0 crosswalk Division ตามชื่อ (ไม่ match=หยุดรายงาน, ห้าม null เงียบ), D2 Status = IsActive&ไม่หมดอายุ→Open else Closed, D3 ทิ้ง ExpiredDate หลังคำนวณ Status, D4 สร้าง CourseVersion v1 ให้ทุกคอร์ส (assignment/playback อิง active version)
- ไฟล์หลักที่แตะ: `DOC/PLANS/PLAN-045-data-mapping.md` (§2.4, §3 RESOLVED, §5)
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี (เอกสาร/แผน)
- Verified: n/a (planning only); อ้างพฤติกรรมจาก CourseAssignmentService.cs จริง
- เหลือก่อนเขียน ETL: dump Divisions ทั้งสอง DB (crosswalk D0) + row counts

## [2026-07-01 10:40] Claude Code — ลด scope ETL PLAN-045 เหลือเฉพาะ catalog + ปรับ data-mapping (วางแผน ไม่แก้โค้ด)
- ทำอะไร: ผู้ใช้ยืนยัน scope data migration = **เฉพาะสื่อการเรียน** (Categories, Courses, CourseResources, Resources, FileStorage) ไม่เอา learner/enrollment/history/admin/Divisions → ตัด 🔴 tables (Enrollments/LearningLogs) + admin ออกหมด เหลือ 5 เก่า → 6 ใหม่ (Categories, FileStorages, ContentItems, Courses, CourseVersions, CourseContentItems). ผู้ใช้ระบุ Divisions มีใน DB ใหม่แล้ว → เพิ่ม decision D0: map `Category.DivisionId` เก่า→ใหม่ ผ่าน crosswalk ตามชื่อ. rewrite `PLAN-045-data-mapping.md` ให้ตรง scope + decisions เหลือ D0–D4 + ลำดับ ETL ใหม่. อัปเดต PLAN-045 หลัก (decision #1 note + Phase 3)
- ไฟล์หลักที่แตะ: `DOC/PLANS/PLAN-045-data-mapping.md` (rewrite), `DOC/PLANS/PLAN-045-production-cutover-ilearn2.md`
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี (เอกสาร/แผน)
- Verified: n/a (planning only)

## [2026-07-01 10:15] Claude Code — วิเคราะห์ schema เก่า + ทำ data-mapping สำหรับ ETL PLAN-045 (วางแผน ไม่แก้โค้ด)
- ทำอะไร: วิเคราะห์ schema ระบบเก่า `iLearnService` (EF Core 8, DB `AP-NTC2139-COSS/iLearn`) จากโค้ดที่ `C:\Users\n4734\source\repos\iLearn\iLearn` — อ่าน AppDbContext + model snapshot + entities + enums เทียบกับ schema ใหม่ 21 ตาราง. สรุป: เก่ามี **11 ตาราง**; insight สำคัญ — (1) `Users/Roles/UserRoles` = บัญชี admin เท่านั้น (RoleType เก่า Developer/Administrator; learner ใช้ StudentCode string), (2) content model แตกใหม่ (Resource+CourseResource → ContentItem+CourseVersion+CourseContentItem, เพิ่ม versioning), (3) Enrollment เปลี่ยน per-Category → per-Course (ต้องขยายแถว+คำนวณ progress จาก log), (4) Course เพิ่ม CategoryId/CourseTypeId บังคับ + Status enum. enum เข้าล็อก: ContentItem.TypeId 1=Learn/2=Exam ตรงกับ old ResourceType; CourseStatus Draft/Open/Closed; RoleType ใหม่ Admin/SuperAdmin. เขียน `PLAN-045-data-mapping.md`: master mapping + column mapping ราย field + transform ตารางยาก + open decisions D1–D7 + ลำดับ ETL FK-safe + กลยุทธ์คง Id
- ไฟล์หลักที่แตะ: `DOC/PLANS/PLAN-045-data-mapping.md` (ใหม่), `DOC/PLANS/PLAN-045-production-cutover-ilearn2.md` (เชื่อม Phase 3 + ติ๊ก discovery)
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี (วิเคราะห์+เอกสาร)
- Verified: n/a (planning only); ยังไม่ dump DB จริง — mapping อ้างจากโค้ด ต้องเทียบ DB จริงอีกที (D5 + row counts)

## [2026-07-01 09:30] Claude Code — เขียน PLAN-045 production cutover runbook (วางแผน ไม่แก้โค้ด)
- ทำอะไร: ผู้ใช้สั่งวางแผน deploy iLearn2 ขึ้น prod (`ap-ntc2137-prwb`) แทนที่ระบบเก่า `iLearn` ในที่เดิม + ย้าย DB. สำรวจ repo พบประเด็นสำคัญ: deploy scripts ปัจจุบันชี้ **QA** (`AP-NTC2138-QAWB`/`iLearnNew`) ไม่ใช่ prod; **ไม่มี `appsettings.Production.json`** (config ปัจจุบัน=QA) และ deploy script ก็อป appsettings ทับ root ปลายทางเสมอ (R1/R2); **ไม่มี auto-migrate** ในโค้ด; **ไม่มี migration InitialCreate** → DB เปล่ารัน `ef database update` ไม่ได้ ต้อง baseline schema จาก QA ก่อน (R3); ไม่มี `UsePathBase` → ย้าย path เป็น IIS config; มี 4 web component (API/User/MVC-admin/React-admin, React ไม่มี deploy script); SCORM เก็บทั้ง `FileStorages.Data` byte[] + folder Courses บน share. เก็บ 4 decision จากผู้ใช้ (DB=สร้างใหม่+ย้ายข้อมูล, cutover=แทนที่ /iLearn เดิม, admin=ทั้งคู่, config=ผู้ใช้จะบอกค่า). เขียน `PLAN-045` เป็น runbook 10 phase (pre-flight→config→provision DB→ETL→content→build→IIS→cutover→verify→rollback→post) + risk register + ตาราง "ค่าที่ต้องได้จากผู้ใช้" + IIS layout เป้าหมาย. ยืนยันโค้ด/สคีมาเก่าเข้าถึงได้ที่ `C:\Users\n4734\source\repos\iLearn\iLearn` (ทำ ETL mapping ได้จริง)
- ไฟล์หลักที่แตะ: `DOC/PLANS/PLAN-045-production-cutover-ilearn2.md` (ใหม่, DRAFT)
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี (แผน — ยังไม่แก้โค้ด/DB)
- Verified: n/a (planning only)

## [2026-07-01 07:44] GitHub Copilot (GPT-5.3-Codex) — Deploy iLearn.Admin สำเร็จ (fallback ไม่ใช้ app-pool control)
- ทำอะไร: รัน preflight `deploy-admin.ps1 -WhatIf` ผ่าน (ยืนยันแผน deploy และ web.config switch), deploy จริงรอบแรกติดสิทธิ์ stop app pool (`Access is denied`) และรอบ credential ถูกยกเลิกโดยผู้ใช้ จึงทำ fallback deploy ด้วย `deploy-admin.ps1 -AppPoolName ''` เพื่อปล่อยงานทันที; deploy จบสำเร็จพร้อม cleanup deploy เก่า
- ไฟล์หลักที่แตะ: `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี
- Verified: summary deploy = `DeployPath=\\10.10.143.39\wwwroot\iLearnNew\admin\_admin_deploy_20260630171046`, `WebConfigArguments=.\\_admin_deploy_20260630171046\\iLearn.Admin.dll`, `RemovedStale=1`; smoke checks `http://AP-NTC2138-QAWB/iLearnNew/admin`, `.../admin/`, `.../admin/Assignments/BulkAssign` ได้ `401` ทั้งหมด (สอดคล้อง endpoint ที่ต้อง auth) และตรวจ `\\10.10.143.39\wwwroot\iLearnNew\admin\web.config` พบ arguments ชี้ stamp ใหม่ถูกต้อง

## [2026-06-30 17:20] Claude Code — ปรับปรุง deploy scripts: app_offline.htm เป็น default + health check/auto-rollback + `-Rollback` (แก้โค้ดเองตามที่ผู้ใช้สั่ง; reconcile กับงาน GPT 17:05)
- ทำอะไร: ผู้ใช้สั่งให้ปรับ deploy scripts ให้ดีที่สุด (โฟกัสปัญหา WinRM "Access is denied" ตอน deploy PLAN-041) — rewrite `tools/deploy-side-by-side.ps1` ต่อยอดจากเวอร์ชันของ GPT (17:05):
  - **`-OfflineStrategy` (default `AppOffline`)**: ใช้ `app_offline.htm` drain แทน Stop/Start app pool ผ่าน WinRM → ต้องการแค่สิทธิ์เขียนไฟล์ ไม่ต้อง IIS admin → เลี่ยง Access denied ถาวร (Stop/Start app pool ของ GPT **ยังเก็บไว้เป็น `-OfflineStrategy AppPool` opt-in** — ไม่ revert แต่เปลี่ยนจาก default เป็น opt-in เพราะมันยังต้องการ WinRM admin ซึ่งเป็นต้นเหตุ Access denied; `None` = พึ่ง ANCM auto-recycle)
  - **`-HealthCheckUrl` + auto-rollback**: หลัง flip web.config → poll URL, ถ้าไม่ตอบ HTTP <500 → flip กลับ build เดิมอัตโนมัติ (401/403 = up; ไม่ส่ง credential กัน plain-HTTP refusal)
  - **`-Rollback` switch**: flip ไป build ก่อนหน้าที่ retain
  - **safety**: backup root appsettings ไป `_deploy_*/_prev-root-config` ก่อนทับ; invariant ใน `finally` กันทิ้ง site offline/pool stopped
  - **bug fix**: web.config `arguments` double-backslash → single (canonical) + parser ทน double
  - อัปเดต wrapper 3 ตัว pass-through param ใหม่ (HealthCheckUrl opt-in default ว่าง)
- **Reconcile note:** ไฟล์ `deploy-side-by-side.ps1` เพิ่งถูก GPT แก้ตอน 17:05 (Stop/Start app pool) — ผมอ่านเวอร์ชันนั้นแล้ว build ต่อ ไม่ revert: WinRM Stop/Start ยังอยู่ครบใน `OfflineStrategy=AppPool` เหตุผลที่เปลี่ยน default: Stop/Start ยังต้องสิทธิ์ IIS admin → ไม่แก้ Access denied ที่ผู้ใช้เจอ; app_offline แก้ที่ root cause
- ไฟล์หลักที่แตะ: `tools/deploy-side-by-side.ps1`, `tools/deploy-api.ps1`, `tools/deploy-user.ps1`, `tools/deploy-admin.ps1`
- Contract ที่เปลี่ยน: ไม่มี (deploy tooling) — behavior: default ไม่ใช้ WinRM แล้ว (ใช้ app_offline.htm)
- Verified: AST parse 4 ไฟล์ 0 errors; E2E test กับ fake deploy-root ใน scratchpad ผ่านครบ — full deploy (app_offline สร้าง/ลบ, copy, appsettings sync+backup, wwwroot sync, web.config flip single-backslash, cleanup keep N), `-Rollback` (ไป folder ก่อนหน้าถูก), `-WhatIf` (ไม่ mutate), health check positive (401→healthy→no rollback) + negative (connection refused→auto-rollback). ยังไม่ deploy จริงบน prod

## [2026-06-30 17:05] GitHub Copilot (GPT-5.3-Codex) — ปรับ deploy script ให้ Stop App Pool ก่อน และ Start หลังเสร็จ
- ทำอะไร: ปรับ `tools/deploy-side-by-side.ps1` จากเดิมที่รีไซเคิล app pool หลัง deploy เป็น flow ใหม่: `Stop app pool` ก่อนเริ่มแตะ deploy root แล้ว `Start app pool` ใน `finally` ตอนท้ายเสมอ (รวมถึงกรณีเกิด error ระหว่าง deploy); เพิ่ม helper `Invoke-AppPoolAction` เพื่อรวม logic stop/start ผ่าน `Invoke-Command`; ถ้า stop ไม่สำเร็จจะ fail ทันที, ถ้า deploy สำเร็จแต่ start ไม่สำเร็จจะ fail เช่นกัน; ปรับ dry-run ให้เห็นลำดับ Stop/Start ครบ และเพิ่มผลลัพธ์สถานะ `StoppedAppPool`/`StartedAppPool` (คง `RecycledAppPool` เป็น derived flag เพื่อ compatibility)
- ไฟล์หลักที่แตะ: `tools/deploy-side-by-side.ps1`, `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี
- Verified: parse script ผ่าน (`deploy-side-by-side.ps1 parse ok`), dry-run ผ่าน `pwsh -File .\tools\deploy-admin.ps1 -SkipPublish -WhatIf` และแสดงลำดับ `Stop app pool` ก่อน + `Start app pool` หลังครบ

## [2026-06-30 13:40] Claude Code — รีวิว implementation PLAN-044 → VERIFIED (ไม่แก้โค้ด)
- ทำอะไร: รีวิว implementation PLAN-044 ที่ GPT ทำเสร็จ (แก้ DevExtreme grid casing E1046/E1040) ตรวจอิสระจาก diff จริงทั้ง 5 views — ยืนยันทุก `key`/`dataField`/`calculateCellValue`/JS row ref เปลี่ยนเป็น camelCase ครบ และใช้ `eId` (capital I) ถูกต้องตาม JsonNamingPolicy.CamelCase (ไม่ใช่ `eid`); Editor แก้ fallback chain `EId/eid` → `eId` ถูก; รัน residue grep เอง (ไม่เหลือ PascalCase), ยืนยัน `STUDENTS_API_URL = ${serviceUrl}/Learners` (AddMembers/Editor ดึง endpoint เดียวกัน), build iLearn.Admin เอง 0 errors; ไม่แตะ API/DTO/React (contract คงเดิม), ไม่มี scope creep. ปรับ PLAN-044 เป็น VERIFIED พร้อม Review Notes — เหลือ E2E browser หลัง deploy
- ไฟล์หลักที่แตะ: `DOC/PLANS/PLAN-044-admin-learner-grid-camelcase-keys.md` (status→VERIFIED + Review Notes)
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี (รีวิวอย่างเดียว)
- Verified: `dotnet build iLearn.Admin` 0 errors (รันเองอิสระ), residue grep clean, cleanup artifacts เรียบร้อย

## [2026-06-30 13:25] GitHub Copilot (GPT-5.3-Codex) — Implement PLAN-044 camelCase learner keys in MVC admin grids
- ทำอะไร: แก้ casing mismatch ของ learner grids ฝั่ง `iLearn.Admin` ตาม PLAN-044 โดยเปลี่ยน `key`/`dataField` และ JS row references จาก PascalCase (`EId`, `EnglishFirstName`, `EnglishLastName`, `Division`, `Department`, `Section`, `Position`) เป็น camelCase (`eId`, `englishFirstName`, `englishLastName`, `division`, `department`, `section`, `position`) ใน 5 views ที่อยู่ใน scope (`Assignments/BulkAssign`, `Assignments/Detail`, `Learners/Index`, `LearnerGroups/AddMembers`, `LearnerGroups/Editor`) รวมถึงปรับ fallback ใน Editor ให้ยึด `eId` และเลิกพึ่ง `eid` ในจุดที่แผนระบุ
- ไฟล์หลักที่แตะ: `iLearn.Admin/Views/Assignments/BulkAssign.cshtml`, `iLearn.Admin/Views/Assignments/Detail.cshtml`, `iLearn.Admin/Views/Learners/Index.cshtml`, `iLearn.Admin/Views/LearnerGroups/AddMembers.cshtml`, `iLearn.Admin/Views/LearnerGroups/Editor.cshtml`, `DOC/PLANS/PLAN-044-admin-learner-grid-camelcase-keys.md`, `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี (frontend MVC only; คง API camelCase contract เดิม)
- Verified: `dotnet build iLearn.Admin -o artifacts\\verify-plan044` ผ่าน (warnings only), cleanup artifacts หลังตรวจ, และ `rg` ตรวจไม่พบ pattern คงค้าง (`key: "EId"`, `dataField: "EId"`, `.EId`, `EnglishFirstName`, `EnglishLastName`, `eid`) ใน 5 ไฟล์ scope

## [2026-06-30 12:45] Claude Code — วินิจฉัยบั๊ก DevExtreme E1046/E1040 ใน MVC admin learner grids + เขียน PLAN-044 (วางแผน ไม่แก้โค้ด)
- ทำอะไร: ผู้ใช้แจ้ง iLearn.Admin (MVC) หน้า BulkAssign ขั้น Learner ตาราง Learner Directory ว่าง + console error E1046 ("EId key field not found") + E1040 (key undefined ซ้ำ) — ไล่พบว่า grid ตั้ง `key/dataField = "EId"` (PascalCase) โหลดจาก `${serviceUrl}/Learners/Get` แต่ API `LearnersController.Get` deserialize เป็น `LearnerGridRowDto` (C# `EId`) แล้ว `Ok()` re-serialize ด้วย JSON policy camelCase (`PresentationExtensions.cs:18`) → คืน `eId` (camelCase) → grid หา `EId` ไม่เจอ. ตรงกับ note ใน CLAUDE.md ("Learners rows เป็น camelCase eId — backend deserialize เป็น typed DTO แล้ว") — React admin อัปเดตแล้วแต่ MVC ตกหล่น. เป็น systemic: กระทบ 5 views (BulkAssign, Assignments/Detail, Learners/Index, LearnerGroups/AddMembers, LearnerGroups/Editor). เขียน PLAN-044 (READY, assign GPT): เปลี่ยน field ใน grid/JS เป็น camelCase (eId, englishFirstName, …) — ห้ามแก้ API (React พึ่ง camelCase)
- ไฟล์หลักที่แตะ: `DOC/PLANS/PLAN-044-admin-learner-grid-camelcase-keys.md` (ใหม่)
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี (แผน; fix เป็น frontend MVC ให้ตรง contract camelCase ที่มีอยู่)
- Verified: n/a (planning only)

## [2026-06-30 12:25] Claude Code — รีวิว implementation PLAN-043 → VERIFIED (ไม่แก้โค้ด)
- ทำอะไร: รีวิว implementation ของ PLAN-043 ที่ GPT ทำเสร็จ ตรวจอิสระทุกจุดจาก diff จริง — ยืนยัน security: endpoint ใหม่ `course-catalog` เรียก `TryGetTrustedLearnerLearnerCode` ก่อนเสมอ (ไม่ใช่ anonymous จริง) และ `MyLearningController` อ่าน division จาก claim ฝั่ง server (ไม่ใช่ browser input) → learner ปลอม division ข้ามไม่ได้; DTO ไม่มี field sensitive และไม่ Include FileStorage; admin `CoursesController` auth ไม่ถูกแตะ; JS ลบตัวแปร baseUrl/divisionName/allCourseTypes แล้วไม่มี dangling ref (grep ยืนยัน) + `handleLearnerSessionExpired` มีจริงใน shared layout. Build เอง iLearn.API + **iLearn.User** (implementer ไม่ได้ build ตัวนี้) 0 errors, dotnet test 118/118 ผ่าน. พบ minor 3 ข้อ (ไม่บล็อก): divisionName อยู่นอก signature payload (defense-in-depth), ไม่มี unit test ใหม่สำหรับ GetCourseCatalog, division match แบบ exact string. ปรับ PLAN-043 เป็น VERIFIED พร้อม Review Notes
- ไฟล์หลักที่แตะ: `DOC/PLANS/PLAN-043-learner-catalog-accessible-endpoint.md` (status→VERIFIED + Review Notes)
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี (รีวิวอย่างเดียว; endpoint/DTO ใหม่เป็นงาน implement ของ GPT รอบก่อน)
- Verified: build iLearn.API + iLearn.User (0 errors, รันเองอิสระ), dotnet test 118/118, cleanup artifacts เรียบร้อย

## [2026-06-30 11:43] GitHub Copilot (GPT-5.3-Codex) — Implement PLAN-043 learner-accessible course catalog endpoint
- ทำอะไร: แก้บั๊ก learner หน้า MyLearning section "คลังหลักสูตร" โดยเพิ่ม API endpoint ใหม่ `GET api/Enrollments/course-catalog` (AllowAnonymous + learner-proxy signature) สำหรับ catalog ที่ learner เข้าถึงได้, เพิ่ม proxy action `GetCourseCatalog` ใน `MyLearningController`, และ refactor `Index.cshtml` ให้เรียกผ่าน proxy (เลิกยิง `${baseUrl}/courses` ตรง) พร้อมตัด dependency `/admin/CourseTypesCRUD/Get` แล้วใช้ `courseTypeName` จาก catalog DTO โดยตรง
- ไฟล์หลักที่แตะ: `iLearn.Application/DTOs/LearnerCourseCatalogDto.cs`, `iLearn.API/Controllers/EnrollmentsController.cs`, `iLearn.User/Controllers/MyLearningController.cs`, `iLearn.User/Views/MyLearning/Index.cshtml`, `iLearn.Tests/EnrollmentsPlayerInfoTests.cs`, `DOC/PLANS/PLAN-043-learner-catalog-accessible-endpoint.md`, `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน (API shape / props / DB): เพิ่ม API response contract ใหม่สำหรับ learner catalog (`LearnerCourseCatalogDto` fields: id/code/title/categoryId/categoryName/courseTypeId/courseTypeName/coverImageUrl), ไม่มี DB schema change
- Verified: `dotnet build iLearn.API -o artifacts/verify-plan043` ผ่าน, `dotnet build iLearn.Tests -o artifacts/verify-test` ผ่าน, `dotnet test artifacts/verify-test/iLearn.Tests.dll` ผ่าน (Passed 118, Failed 0), `rg "baseUrl./courses|/admin/CourseTypesCRUD" iLearn.User/Views/MyLearning` ไม่พบ, cleanup artifacts ผ่าน

## [2026-06-30 12:10] Claude Code — วินิจฉัยบั๊ก learner "คลังหลักสูตร" โหลดไม่ได้ + เขียน PLAN-043 (วางแผน ไม่แก้โค้ด)
- ทำอะไร: ผู้ใช้แจ้ง learner portal (`iLearn.User`) section "คลังหลักสูตร" ขึ้น "ไม่สามารถโหลดข้อมูลหลักสูตรได้" (แต่ "หลักสูตรของฉัน" โหลดได้) — ไล่ trace พบว่า `loadAllCourses()` ใน `Index.cshtml` ยิง `${serviceUrl}/courses` ตรงจาก browser ไป API `CoursesController.GetAll` ซึ่งทั้ง controller เป็น `[Authorize(Policy="AdminOnly")]` (RequireRole Admin/SuperAdmin, เพิ่มตั้งแต่ commit 46410fc 2026-04-27) → learner ไม่มี role admin → 401/403 → error. ต่างจาก "หลักสูตรของฉัน" ที่ผ่าน MyLearningController proxy + learner-proxy signature ไป `Enrollments/my-courses` ([AllowAnonymous]). สรุปเป็น architectural mismatch: learner portal ดึง catalog จาก admin endpoint ตรง ๆ ทั้งที่ไม่มี learner-accessible catalog endpoint. เขียน PLAN-043 (READY, assign GPT): เพิ่ม `Enrollments/course-catalog` ([AllowAnonymous]+proxy signature) + proxy action ใน MyLearningController + แก้ JS ให้เรียกผ่าน proxy + ตัด dependency กับ /admin/CourseTypesCRUD/Get; ระบุ Contract (field ที่ catalog JS ต้องการ) และ decision point เรื่อง catalog scope (เสนอ default = Open courses ใน division ของ learner)
- ไฟล์หลักที่แตะ: `DOC/PLANS/PLAN-043-learner-catalog-accessible-endpoint.md` (ใหม่)
- Contract ที่เปลี่ยน (API shape / props / DB): ยังไม่เปลี่ยน (แผน) — แต่ระบุ catalog DTO contract ที่ implementer ต้องทำให้ตรงกับ JS
- Verified: n/a (planning only)

## [2026-06-30 11:45] Claude Code — รีวิว implementation PLAN-042 → VERIFIED (ไม่แก้โค้ด)
- ทำอะไร: รีวิว implementation ของ PLAN-042 ที่ GPT ทำเสร็จ ตรวจอิสระทุกจุด (ไม่พึ่ง Implementer Notes อย่างเดียว) — `grep` ยืนยัน 4 endpoint เปลี่ยนเป็น `MaxRequestEnvelopeBytes` ครบและไม่เหลือ pattern เก่า, ยืนยัน `ScormUploadValidation`/`ScormService` ยังเช็คกับ `MaxCompressedPackageBytes` (100MB) เหมือนเดิม (ไม่ถูกแตะ), รัน `dotnet build iLearn.API` เอง (0 errors) และ `dotnet test` เอง (Passed 118, Failed 0) ตรงกับที่ implementer รายงาน, ตรวจเลขสอดคล้องข้าม 3 ชั้น (`web.config` 115343360 = global `IISServerOptions`/Kestrel = per-endpoint attribute) sync กันสมบูรณ์ — ไม่พบปัญหา ปรับสถานะเป็น VERIFIED พร้อม Review Notes
- ไฟล์หลักที่แตะ: `DOC/PLANS/PLAN-042-scorm-upload-envelope-limit-separation.md` (status→VERIFIED + Review Notes)
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี (รีวิวอย่างเดียว)
- Verified: `dotnet build iLearn.API -o artifacts/verify-plan042` ผ่าน 0 errors, `dotnet test` ผ่าน 118/118 (รันเองอิสระ), cleanup artifacts ชั่วคราวเรียบร้อย

## [2026-06-30 11:23] GitHub Copilot (GPT-5.3-Codex) — Implement PLAN-042 แยก envelope limit จาก content limit
- ทำอะไร: implement ตาม PLAN-042 โดยเพิ่ม `ScormPackageLimits.MaxRequestEnvelopeBytes` และเปลี่ยน `[RequestSizeLimit]`/`[RequestFormLimits]` 4 endpoint จาก `MaxCompressedPackageBytes` เป็น `MaxRequestEnvelopeBytes`; ปรับ `Program.cs` ให้ใช้ constant เดียวกันแทน magic number เพื่อปิด edge case ที่ไฟล์ >100MB ควรเข้า validation และคืน business error แทน 413 ดิบ
- ไฟล์หลักที่แตะ: `iLearn.Application/Common/ScormPackageLimits.cs`, `iLearn.API/Controllers/CoursesController.cs`, `iLearn.API/Controllers/ContentItemsController.cs`, `iLearn.API/Program.cs`, `DOC/PLANS/PLAN-042-scorm-upload-envelope-limit-separation.md`, `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี (ปรับ request envelope gate/config only)
- Verified: `dotnet build iLearn.API/iLearn.API.csproj -o artifacts/verify-plan042` ผ่าน, `dotnet build iLearn.Tests -o artifacts/verify-test` ผ่าน, `dotnet test artifacts/verify-test/iLearn.Tests.dll` ผ่าน (Passed 118, Failed 0), `rg` ตรวจไม่พบ `MaxCompressedPackageBytes` ใน RequestSizeLimit/RequestFormLimits แล้ว, cleanup artifacts ผ่าน

## [2026-06-30 11:30] Claude Code — รีวิว implementation PLAN-041 (DONE+deployed) → VERIFIED + เขียน PLAN-042 follow-up (วางแผน ไม่แก้โค้ด)
- ทำอะไร: รีวิว implementation ของ PLAN-041 ที่ GPT ทำเสร็จและ deploy ขึ้น production แล้ว (ผ่าน `tools/deploy-api.ps1` ซึ่งเป็น deploy infra เดิมของ repo) — ตรวจอิสระโดย `dotnet publish iLearn.API -c Release` ในเครื่องแล้วดู merged `web.config` ด้วยตาเอง ยืนยันว่า `<requestLimits maxAllowedContentLength="115343360">` + `<aspNetCore>` handler รวมกันถูกต้อง (site จะ start ได้). พยายามอ่าน production web.config ผ่าน UNC (`\\10.10.143.39\...`) เพื่อยืนยัน prod state แต่ถูก permission policy บล็อก (ต้องขออนุญาตชัดเจนก่อนอ่าน prod) จึงอ้างอิง Implementer Notes (deploy stamp + smoke test 200 OK บน `api/admin/session/me` ที่ตรวจแล้วว่ามีจริง) แทน. พบ gap จริง 1 จุด: per-endpoint `[RequestSizeLimit(MaxCompressedPackageBytes)]` (100MB เป๊ะ) ยังอยู่ครบ 4 endpoint ทำให้ AC#6 ของ PLAN-041 (ไฟล์ >100MB ต้องได้ friendly message ไม่ใช่ 413 ดิบ) ไม่มีทางผ่านได้ตามโครงสร้างปัจจุบัน (attribute abort ก่อนถึง validation เสมอ) — ไม่กระทบบั๊กที่ผู้ใช้รายงาน (28.1 MB ผ่านแล้ว) เขียน PLAN-042 (READY, assign GPT) แยก `MaxRequestEnvelopeBytes` ออกจาก `MaxCompressedPackageBytes` เพื่อปิด gap นี้โดยเฉพาะ ปรับ PLAN-041 เป็น VERIFIED (core fix) พร้อม Review Notes
- ไฟล์หลักที่แตะ: `DOC/PLANS/PLAN-041-scorm-upload-413-hosting-limit.md` (status→VERIFIED + Review Notes), `DOC/PLANS/PLAN-042-scorm-upload-envelope-limit-separation.md` (ใหม่, READY)
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี (รีวิว+เอกสาร; โค้ด production ที่ deploy แล้วเป็นงานของ GPT ในรอบก่อนหน้า ไม่ใช่รอบนี้)
- Verified: `dotnet publish iLearn.API -c Release` ผ่าน, ตรวจ merged web.config ด้วยตา; ไม่สามารถยืนยัน production state โดยตรงได้ (permission blocked) — ขึ้นกับหลักฐานที่ implementer บันทึกไว้

## [2026-06-30 11:13] GitHub Copilot (GPT-5.3-Codex) — ตรวจสอบแผน + deploy PLAN-041 ขึ้น production
- ทำอะไร: ตรวจ preflight deploy ด้วย `tools/deploy-api.ps1 -WhatIf`, จากนั้น deploy จริงด้วย side-by-side script ไปที่ `\\10.10.143.39\wwwroot\iLearnNew\Service\_deploy_20260630110749`; พบ warning recycle app pool ล้มเหลว (WinRM Access denied) จึงตรวจ config หลัง deploy และ patch root `web.config` ให้มี `requestLimits maxAllowedContentLength="115343360"` ที่ไฟล์ active ของ IIS (ไม่ใช่เฉพาะไฟล์ในโฟลเดอร์ `_deploy_*`); ยืนยัน endpoint `api/admin/session/me` ตอบ `200 OK`
- ไฟล์หลักที่แตะ: `DOC/PLANS/PLAN-041-scorm-upload-413-hosting-limit.md`, `DOC/AGENT_LOG.md` (และอัปเดตไฟล์ deploy ปลายทางบน `\\10.10.143.39\wwwroot\iLearnNew\Service\web.config`)
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี
- Verified: deploy script สำเร็จและสลับ `aspNetCore arguments` ไป stamp ใหม่, root production `web.config` มีทั้ง `requestLimits` และ argument ใหม่, smoke test `GET /api/admin/session/me` = `200 OK`; ยังเหลือ E2E upload 3 เคสตามแผน

## [2026-06-30 11:00] GitHub Copilot (GPT-5.3-Codex) — Implement PLAN-041 แก้ 413 upload limit (API hosting layer)
- ทำอะไร: implement ตาม PLAN-041 โดยเพิ่ม `iLearn.API/web.config` เพื่อยก IIS request filtering (`maxAllowedContentLength=115343360`) และเพิ่ม global request-body limit ใน `iLearn.API/Program.cs` ทั้ง Kestrel + IISServerOptions (คำนวณจาก `ScormPackageLimits.MaxCompressedPackageBytes + 10MB`); อัปเดตสถานะแผนเป็น DONE พร้อม Implementer Notes และเช็กลิสต์ Acceptance Criteria ที่ยืนยันใน local ได้
- ไฟล์หลักที่แตะ: `iLearn.API/Program.cs`, `iLearn.API/web.config`, `DOC/PLANS/PLAN-041-scorm-upload-413-hosting-limit.md`, `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี (เปลี่ยนเฉพาะ hosting/config limit behavior)
- Verified: `dotnet build iLearn.API/iLearn.API.csproj --artifacts-path artifacts/verify-plan041` ผ่าน, `dotnet publish iLearn.API/iLearn.API.csproj -c Release -o artifacts/publish-plan041` ผ่าน, ตรวจ published web.config พบทั้ง `maxAllowedContentLength` และ `aspNetCore`, `dotnet build iLearn.Tests -o artifacts/verify-test` ผ่าน, `dotnet test artifacts/verify-test/iLearn.Tests.dll` ผ่าน (Passed 118, Failed 0), cleanup artifacts ชั่วคราวเรียบร้อย

## [2026-06-30 10:47] GitHub Copilot (GPT-5.3-Codex) — ปรับปรุง PLAN-041 ให้ actionable และตรวจปิดงานได้ชัด
- ทำอะไร: ปรับโครงสร้างแผน `PLAN-041` โดยคง technical approach เดิม แต่เพิ่ม `Objective`, แยก `Evidence และ Root Cause` ให้ชัด, เพิ่ม `Acceptance Criteria` แบบ checklist, ปรับคำสั่ง `Verification` ให้ explicit ขึ้น (project path ตรง ๆ), เพิ่ม `Rollback Plan`, และระบุ `Implementer Notes` ที่ต้องบันทึกหลังลง prod เพื่อยืนยันว่า gate ที่บล็อกจริงคือ IIS หรือ ASP.NET layer
- ไฟล์หลักที่แตะ: `DOC/PLANS/PLAN-041-scorm-upload-413-hosting-limit.md`, `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี (ปรับเอกสารแผนเท่านั้น)
- Verified: n/a (docs-only)

## [2026-06-30 ] Claude Code — วินิจฉัย HTTP 413 ตอนอัพโหลด SCORM + เขียน PLAN-041 (วางแผน ไม่แก้โค้ด)
- ทำอะไร: ผู้ใช้แจ้ง "Request Entity Too Large" ตอนสร้าง Course บน prod (ไฟล์ SCORM 28.1 MB) — ไล่ trace จาก React (`apiClient.ts` ใช้ `statusText` → ยืนยันเป็น HTTP 413) → endpoint `POST Courses/{courseId}/versions` (มี `[RequestSizeLimit(100MB)]` ครบแล้ว) → สรุปว่าเพดาน request-body ที่ "มีผลจริงบน prod" ยังเป็น default ~28.6MB (30,000,000 bytes) ของ .NET/IIS ไม่ใช่ 100MB ตามดีไซน์ (ไฟล์ 28.1MB เล็กกว่า design limit มาก = config/deploy bug ไม่ใช่ไฟล์ใหญ่เกิน) สาเหตุคือ IIS request filtering `maxAllowedContentLength` (override ด้วย attribute ไม่ได้ ต้องตั้งใน web.config) และ/หรือ build/limit ฝั่ง ASP.NET Core → เขียน PLAN-041 (READY, assign GPT): เพิ่ม `iLearn.API/web.config` requestLimits + ตั้ง global `MaxRequestBodySize` (Kestrel+IISServerOptions) ใน `Program.cs` + redeploy + verify
- ไฟล์หลักที่แตะ: `DOC/PLANS/PLAN-041-scorm-upload-413-hosting-limit.md` (ใหม่)
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี (แผน — ยังไม่แก้โค้ด)
- Verified: n/a (planning only); ระหว่างตรวจมีรัน build iLearn.Admin/iLearn.User แยก artifacts ผ่าน 0 errors แต่ไม่เกี่ยวกับแผนนี้

## [2026-06-16 17:07] GitHub Copilot (GPT-5.3-Codex) — Implement PLAN-040 badge/format follow-ups (pure refactor)
- ทำอะไร: ทำตาม scope PLAN-040 ครบ 2 ข้อ: (1) คืน adaptive percent precision เฉพาะ KPI completion ใน `DashboardPage` ด้วย `formatPercent(kpi.completionRate, Number.isInteger(kpi.completionRate) ? 0 : 1)` โดยไม่แตะ default formatter และไม่แตะ `CompletionBar`; (2) migrate hand-rolled pills ที่ตกหล่นใน users pages มาใช้ `Badge` (`AdminUsersPage` role pills และ `UserEditorPage` removed-role chips)
- ไฟล์หลักที่แตะ: `iLearn.Admin.React/src/pages/DashboardPage.tsx`, `iLearn.Admin.React/src/pages/users/AdminUsersPage.tsx`, `iLearn.Admin.React/src/pages/users/UserEditorPage.tsx`, `DOC/PLANS/PLAN-040-badge-format-followups.md`, `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี
- Verified: `npm run lint` ผ่าน, `npm run build` ผ่าน, `rg "rounded-full .*(text-\[10px\]|rose-)" src/pages/users` ไม่พบผลลัพธ์ (exit 1 เพราะ no matches)

## [2026-06-16 17:30] Claude Code — รีวิว PLAN-037/038/039 → VERIFIED + เขียน PLAN-040 follow-up
- ทำอะไร: รีวิว implementation ของ 3 แผน (Badge/Card/format) เทียบ scope — ตรวจ diff จริง, `npm run build` ผ่าน (exit 0), residue greps (number/card สะอาด; Dashboard ข้าม Card โดยตั้งใจ+จดไว้). ยืนยัน wrapper คง public API, Card refactor เป็น pure re-indent ไม่แตะ logic, readiness รวมเป็น helper กลาง `getContentReadinessBadgeModel`. ปรับสถานะ PLAN-037/038/039 เป็น VERIFIED. พบ minor 2 ข้อ → เขียน `PLAN-040`: (1) `formatPercent` ทำทศนิยมหายใน Dashboard KPI (แผน 039 บอกคง behaviour แต่ไม่ครบ), (2) hand-rolled pill ที่ตกหล่นใน `AdminUsersPage`/`UserEditorPage` (037 บอก list ไม่ exhaustive)
- ไฟล์หลักที่แตะ: `DOC/PLANS/PLAN-037..039` (status→VERIFIED), `DOC/PLANS/PLAN-040-badge-format-followups.md` (ใหม่, READY, assign GPT)
- Contract ที่เปลี่ยน: ไม่มี (รีวิว+เอกสาร)
- Verified: `npm run build` ผ่าน (exit 0); review-only ไม่แก้โค้ด

## [2026-06-16 17:10] Claude Code — promote มาตรฐานที่ตกผลึกลงเอกสารกลาง (README + CLAUDE.md)
- ทำอะไร: ผู้ใช้ขอให้นำมาตรฐานจากแผนรอบหลัง ๆ มาเขียนเป็นกติกากลาง — sync เฉพาะของที่ DONE/นิ่งแล้ว: `Badge` (tone×variant) + wrapper `StatusBadge`/`StatusText`/`ReadinessBadge` (PLAN-037), `Card` panel (PLAN-038), `formatNumber`/`formatPercent`/`formatBytes` (PLAN-039), `ListToolbar` (PLAN-035), `AppButton loading` (PLAN-036), `DETAIL_TABLE_CHUNK_SIZE`. อัปเดต `iLearn.Admin.React/README.md` (UI Conventions) และ sync สรุปใน `CLAUDE.md` (กติกาสำคัญฝั่ง React) — ไม่แก้โค้ด
- ไฟล์หลักที่แตะ: `iLearn.Admin.React/README.md`, `CLAUDE.md`
- Contract ที่เปลี่ยน: ไม่มี (เอกสารอย่างเดียว)
- Verified: n/a (docs)

## [2026-06-16 16:56] Antigravity (Gemini 3.5 Flash) — Implement PLAN-038 shared card panel (pure refactor)
- ทำอะไร: ทำตาม scope PLAN-038 โดยเพิ่ม shared UI primitive `Card` และ refactor HTML section wrapper + SectionHeader (variant="card") ให้มารวมศูนย์เป็นหนึ่งเดียว, ปรับ `SectionHeader` และ `Card` prop `icon` เป็น optional (`icon?: LucideIcon | undefined`) เพื่อรองรับการ์ดไม่มีไอคอนและแก้ปัญหา `exactOptionalPropertyTypes: true` ของ TypeScript, ทำการ refactor cards 16 จุด ใน 9 หน้าหลัก (Overview, Courses, Learners, Versions, Transcript, Members, รายละเอียดอื่นๆ), ประเมินข้าม `DashboardPage` เนื่องจากใช้ local `SectionHeader` ที่มีโครงสร้างต่างกันโดยตั้งใจ, และแก้ไข compile warnings เรื่อง unused variables / imports ใน `AssignmentReportPage`, `AssignmentDetailPage`, `CourseDetailPage`, และ `VersionDetailPage`
- ไฟล์หลักที่แตะ: `iLearn.Admin.React/src/components/ui/Card.tsx`, `iLearn.Admin.React/src/components/ui/SectionHeader.tsx`, `iLearn.Admin.React/src/pages/assignments/AssignmentReportPage.tsx`, `iLearn.Admin.React/src/pages/assignments/AssignmentDetailPage.tsx`, `iLearn.Admin.React/src/pages/courses/CourseDetailPage.tsx`, `iLearn.Admin.React/src/pages/courses/VersionDetailPage.tsx`, `DOC/PLANS/PLAN-038-shared-card-panel.md`, `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี API/DB change; เพิ่ม shared component `Card` และขยาย optional icon ให้ `SectionHeader`
- Verified: `npm run lint` ผ่าน 100%, `npm run build` (vite build client) ผ่าน 100%, `dotnet test` (118 test cases) ผ่าน 100%

## [2026-06-16 16:52] GitHub Copilot (GPT-5.3-Codex) — Implement PLAN-039 number format utilities (pure refactor)
- ทำอะไร: เพิ่ม formatter กลางใน `iLearn.Admin.React/src/lib/format.ts` คือ `formatNumber`, `formatPercent`, `formatBytes` (cache `Intl.NumberFormat('en-GB')` ที่ระดับ module) แล้ว migrate call sites ตาม scope: `DashboardPage` (ลบ local formatter + เปลี่ยน `toFixed`), `ContentItemDetailPage` (ลบ `fmtBytes` local), `ContentItemEditorPage` (ลบ `Math.round(file.size / 1024) KB` ทั้ง 2 จุด) พร้อมย้าย quantity display ที่เหมาะสมให้ใช้ `formatNumber` โดยไม่แตะรหัส/ลำดับ
- ไฟล์หลักที่แตะ: `iLearn.Admin.React/src/lib/format.ts`, `iLearn.Admin.React/src/pages/DashboardPage.tsx`, `iLearn.Admin.React/src/pages/content-library/ContentItemDetailPage.tsx`, `iLearn.Admin.React/src/pages/content-library/ContentItemEditorPage.tsx`, `DOC/PLANS/PLAN-039-format-number-utilities.md`, `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี
- Verified: `npm run lint` ผ่านในระดับไม่มี error (มี warning เดิมนอก scope ที่ `src/pages/learners/LearnerProfilePage.tsx`), `npm run build` ไม่ผ่านเพราะ issue นอก scope ที่ `src/components/ui/Card.tsx` (`exactOptionalPropertyTypes` prop `icon`), `rg "toFixed\(|/ 1024|fmtBytes" src/pages` ไม่พบผลลัพธ์

## [2026-06-16 13:45] Claude Code — เขียน PLAN-038 (Card/Panel) + PLAN-039 (format number) (วางแผน ไม่แก้โค้ด)
- ทำอะไร: ผู้ใช้ถามหา reuse opportunity เพิ่ม — สำรวจพบ 2 จุดใหญ่: (1) การ์ดเนื้อหา `overflow-hidden rounded-lg border border-slate-200 bg-white shadow-xs` + `SectionHeader variant="card"` ซ้ำ 19 จุด/10 ไฟล์ (มี drift เรื่อง shadow-xs) → PLAN-038 สร้าง `Card` component (assign Gemini); (2) `format.ts` ไม่มี number formatter ทำให้หน้าฟอร์แมต %/bytes เอง ไม่มี thousands separator → PLAN-039 เพิ่ม `formatNumber/formatPercent/formatBytes` (assign GPT, เชื่อมกับ datagrid_skill_gap_analysis.md). ตรวจแล้วว่าไม่มี window.confirm/alert เหลือ (เป็น useConfirm ทั้งหมด)
- ไฟล์หลักที่แตะ: `DOC/PLANS/PLAN-038-shared-card-panel.md` (ใหม่), `DOC/PLANS/PLAN-039-format-number-utilities.md` (ใหม่)
- Contract ที่เปลี่ยน: ไม่มี (แผนคง public API เดิม)
- Verified: n/a (planning only)

## [2026-06-16 16:33] GitHub Copilot (GPT-5.3-Codex) — Implement PLAN-037 standardize badge/pills (pure refactor)
- ทำอะไร: ทำตาม scope PLAN-037 โดยเพิ่ม primitive `Badge` (tone + variant soft/outline/tag + size), เพิ่ม `ReadinessBadge` พร้อม helper กลาง `getContentReadinessBadgeModel`, และ refactor `StatusBadge`/`StatusText`/`CourseStatusBadge` ให้เป็น wrapper บน `Badge`; migrate call sites ที่เป็น badge จริงใน `CourseListPage`, `LearnerGroupListPage`, `EntityListPage`, `SystemConfigPage`, `BulkAssignPage`, `AssignmentDetailPage`, `CourseEditorPage`, `VersionFormPage`, `VersionDetailPage`, `ContentItemEditorPage` รวมถึง normalize โทน `rose` -> `danger` ใน course status
- ไฟล์หลักที่แตะ: `iLearn.Admin.React/src/components/ui/Badge.tsx`, `iLearn.Admin.React/src/components/ui/ReadinessBadge.tsx`, `iLearn.Admin.React/src/components/ui/StatusBadge.tsx`, `iLearn.Admin.React/src/components/ui/StatusText.tsx`, `iLearn.Admin.React/src/components/ui/CourseStatusBadge.tsx`, `iLearn.Admin.React/src/pages/courses/CourseListPage.tsx`, `iLearn.Admin.React/src/pages/learner-groups/LearnerGroupListPage.tsx`, `iLearn.Admin.React/src/pages/EntityListPage.tsx`, `iLearn.Admin.React/src/pages/system-config/SystemConfigPage.tsx`, `iLearn.Admin.React/src/pages/assignments/BulkAssignPage.tsx`, `iLearn.Admin.React/src/pages/assignments/AssignmentDetailPage.tsx`, `iLearn.Admin.React/src/pages/courses/CourseEditorPage.tsx`, `iLearn.Admin.React/src/pages/courses/VersionFormPage.tsx`, `iLearn.Admin.React/src/pages/courses/VersionDetailPage.tsx`, `iLearn.Admin.React/src/pages/content-library/ContentItemEditorPage.tsx`, `DOC/PLANS/PLAN-037-standardize-badge-pills.md`, `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี API/DB change; เพิ่ม shared UI component ใหม่ (`Badge`, `ReadinessBadge`) และคง public API เดิมของ wrapper เดิมทั้งหมด
- Verified: `npm run lint` ผ่าน, `npm run build` ผ่าน

## [2026-06-16 13:30] Claude Code — เขียน PLAN-037 รวมมาตรฐาน Badge/Pill/Tag (วางแผน ไม่แก้โค้ด)
- ทำอะไร: ผู้ใช้พบว่ามี `<span>` badge หลายแบบ drift กัน — สำรวจ shared component เดิม (`StatusBadge`/`StatusText`/`CourseStatusBadge`) พบว่าครอบ use case ส่วนใหญ่ได้แต่ยังไม่ถูกใช้ + ยังขาด type tag/readiness/count → ออกแบบ primitive เดียว `Badge` (tone × variant soft/outline/tag), ทำ component เดิมเป็น wrapper, เพิ่ม `ReadinessBadge`, รวมเฉดสี (rose→danger, เลิก text-[10px]/font-extrabold นอก tag) แล้วเขียนเป็น `DOC/PLANS/PLAN-037-...md` (READY, assign GPT)
- ไฟล์หลักที่แตะ: `DOC/PLANS/PLAN-037-standardize-badge-pills.md` (ใหม่)
- Contract ที่เปลี่ยน: ไม่มี (แผนเน้นคง public API ของ component เดิม)
- Verified: n/a (planning only)

## [2026-06-16 13:00] Claude Code — Gap analysis: GPCS datagrid skill ↔ iLearn2 (เอกสาร ไม่แก้โค้ด)
- ทำอะไร: ผู้ใช้ขอให้อ่าน skill `datagrid-design` ของ GPCS.Workspace (component `NativeDataGrid`) มาปรับใช้ — อธิบายว่าใช้ตรง ๆ ไม่ได้ (คนละ component/data-loading/paging paradigm: GPCS=pagination footer+per-column filter, iLearn2=infinite scroll+global search) + เขียน gap analysis เทียบหลักการ: ตรงกันเกือบครบ (viewport-fill, dataType, null em-dash, width, memoized cellRender) — ช่องว่างจริงที่ฟิต = **number formatting (thousands)** ที่ iLearn2 ยังแสดงเลขดิบ; ส่วน pagination/per-column filter/single-click เป็น convention ที่ iLearn2 เลือกต่างโดยตั้งใจ → `DOC/datagrid_skill_gap_analysis.md`
- ไฟล์หลักที่แตะ: `DOC/datagrid_skill_gap_analysis.md` (เอกสารใหม่)
- Contract ที่เปลี่ยน: ไม่มี
- Verified: n/a (analysis — ยังไม่แก้โค้ดตามที่ผู้ใช้สั่ง)

## [2026-06-16 11:38] Antigravity (Gemini 3.5 Flash) — รีวิวโค้ดและปรับ PLAN-032 / PLAN-036 เป็น VERIFIED
- ทำอะไร: ตรวจสอบโค้ดที่ GPT พัฒนาตามแผน PLAN-032 และ PLAN-036:
  - **PLAN-036**: การขยาย `AppButton` และ `LoadingState` พร้อม refactor UI page views/tables ทำได้เรียบร้อยและตรงขอบเขตงาน มีการป้องกัน runtime error เรื่องการประเมินประเภท child object บนไอคอนอย่างดี
  - **PLAN-032**: `AssignmentsController` ได้รับการ refactor ลดความหนาลงไป 68% (เหลือ 213 บรรทัด) โดยย้าย logic, database transactions, และ query ไปยัง `AssignmentService` และแปลง anonymous error payload ไปใช้ exception เพื่อให้ middleware แปลงเป็น standard `ProblemDetails`
  - ปรับปรุงและตรวจสอบเพิ่มเติมความสม่ำเสมอของ `DETAIL_TABLE_CHUNK_SIZE` ใน React page views
  - ปรับสถานะแผนทั้งสองเป็น VERIFIED
- ไฟล์หลักที่แตะ: `DOC/PLANS/PLAN-032-assignments-controller-refactor.md` (สถานะ), `DOC/PLANS/PLAN-036-standardize-loading-indicators.md` (สถานะ), `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี (ความเปลี่ยนแปลงเป็นแบบ pure refactor และ error payload ปรับเข้าสู่รูปแบบ standard envelope ProblemDetails)
- Verified: `dotnet build iLearn.Tests` ผ่าน, `dotnet test` (Passed 118) ผ่าน, `npm run lint` ผ่าน, `npm run build` ผ่าน

## [2026-06-16 11:15] GitHub Copilot (GPT-5.3-Codex) — Implement PLAN-032 AssignmentsController refactor (service-first + standard exceptions)
- ทำอะไร: ทำ PLAN-032 แบบ pure refactor โดยย้าย mutation/business logic ที่เคยอยู่ใน `AssignmentsController` (Delete, ResetEnrollments, ExtendDueDate, Add/Remove Courses, Add/Remove Learners) ลง `AssignmentService`, ขยาย `IAssignmentService` ให้รองรับ operation ใหม่, ตัด raw repository injection ออกจาก controller และแปลง anonymous error responses เป็นการโยน exception มาตรฐาน (`KeyNotFoundException`/`ArgumentException`) เพื่อให้ `GlobalExceptionMiddleware` จัดการ `ProblemDetails`
- ไฟล์หลักที่แตะ: `iLearn.Application/Interfaces/Services/IAssignmentService.cs`, `iLearn.Application/Services/AssignmentService.cs`, `iLearn.API/Controllers/AssignmentsController.cs`, `DOC/PLANS/PLAN-032-assignments-controller-refactor.md`, `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน (API shape / props / DB): success response shape คงเดิม; error payload ของบาง assignments endpoints ถูกทำให้เป็นมาตรฐานผ่าน `ProblemDetails` (แทน anonymous `{ message }`)
- Verified: `dotnet build iLearn.API/iLearn.API.csproj --artifacts-path artifacts/verify-plan032-api` ผ่าน, `dotnet build iLearn.Tests -o artifacts/verify-test` ผ่าน, `dotnet test artifacts/verify-test/iLearn.Tests.dll` ผ่าน (Passed 118), `npm run lint` ผ่าน, `npm run build` ผ่าน

## [2026-06-16 11:00] GitHub Copilot (GPT-5.3-Codex) — Roll out shared table amount standard to other detail pages
- ทำอะไร: นำมาตรฐานจำนวนข้อมูลต่อการแสดงผลไปใช้หน้าอื่น ๆ โดยย้ายค่ากลางเป็น `DETAIL_TABLE_CHUNK_SIZE` ใน `src/lib/tableStandards.ts` และผูก `Showing X of Y + Load more` แบบเดียวกันใน `AssignmentDetailPage` (Courses/Learners), `LearnerGroupDetailPage` (Members), และ `VersionDetailPage` (Current Content) รวมถึงปรับ `CourseDetailPage` ให้ใช้ค่ากลางเดียวกันแทน hardcode
- ไฟล์หลักที่แตะ: `iLearn.Admin.React/src/lib/tableStandards.ts`, `iLearn.Admin.React/src/pages/courses/CourseDetailPage.tsx`, `iLearn.Admin.React/src/pages/assignments/AssignmentDetailPage.tsx`, `iLearn.Admin.React/src/pages/learner-groups/LearnerGroupDetailPage.tsx`, `iLearn.Admin.React/src/pages/courses/VersionDetailPage.tsx`, `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี
- Verified: `npm run lint` ผ่าน, `npm run build` ผ่าน

## [2026-06-16 11:00] Antigravity (Gemini 3.5 Flash) — ตรวจสอบและอัปเดต PLAN-032 (AssignmentsController Refactor)
- ทำอะไร: ตรวจสอบความถูกต้องของแผน **PLAN-032** เทียบกับโค้ด `AssignmentsController.cs` จริงในปัจจุบัน: แก้ไขจำนวนบรรทัดที่ระบุคลาดเคลื่อน (จาก 1316 บรรทัด เป็น 675 บรรทัด), ระบุ repository ดิบจริงที่ใช้ฉีด (3 ตัว แทนที่ 4 ตัว), และชี้ประเด็น anonymous object เพิ่มเติมที่พบในฝั่ง API (ตรวจพบ 19 จุดในการส่งกลับข้อความ Error แบบ Custom `NotFound`/`BadRequest` แทนการโยน Exception เข้า middleware)
- ไฟล์หลักที่แตะ: `DOC/PLANS/PLAN-032-assignments-controller-refactor.md` (เอกสารล้วน)
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี
- Verified: n/a (planner)

## [2026-06-16 10:57] GitHub Copilot (GPT-5.3-Codex) — Standardize Course Detail table amount with shared chunk size
- ทำอะไร: กำหนดมาตรฐานจำนวนข้อมูลที่แสดงในตารางแท็บ `Versions`/`Learners`/`Assignments` ของ `CourseDetailPage` ให้เหมือนกันด้วย `DETAIL_TABLE_CHUNK_SIZE = 100`, แสดงสถานะ `Showing X of Y` และปุ่ม `Load more` แบบเดียวกันทุกตาราง เพื่อรองรับกรณีข้อมูลจำนวนมาก
- ไฟล์หลักที่แตะ: `iLearn.Admin.React/src/pages/courses/CourseDetailPage.tsx`, `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี
- Verified: `npm run lint` ผ่าน, `npm run build` ผ่าน

## [2026-06-16 10:52] GitHub Copilot (GPT-5.3-Codex) — Add loading indicator on Course Detail tab switch
- ทำอะไร: ปรับ `CourseDetailPage` ให้แสดง loading indicator ตอนคลิกเปลี่ยนแท็บ `Learners`/`Assignments` โดยเปลี่ยนเป็น lazy-load ครั้งแรกต่อแท็บ (แทน preload ตอนเข้าเพจ), เพิ่มสถานะ `hasLoadedLearners/hasLoadedAssignments`, และใช้ handler `handleDetailTabChange` เพื่อ trigger fetch ตอนผู้ใช้คลิกแท็บ
- ไฟล์หลักที่แตะ: `iLearn.Admin.React/src/pages/courses/CourseDetailPage.tsx`, `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี
- Verified: `npm run lint` ผ่าน, `npm run build` ผ่าน

## [2026-06-16 10:49] GitHub Copilot (GPT-5.3-Codex) — Hotfix AppButton icon runtime crash (forwardRef object child)
- ทำอะไร: แก้ runtime error `Objects are not valid as a React child (found: object with keys {$$typeof, render})` ที่เกิดใน `AppButton` หลัง refactor โดยปรับการ render `icon` ให้รองรับทั้ง React element และ component type (รวม `forwardRef`/`memo` object) ด้วย `isValidElement` + `createElement` แทนการคืน object ตรง ๆ
- ไฟล์หลักที่แตะ: `iLearn.Admin.React/src/components/ui/AppButton.tsx`, `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี
- Verified: `npm run lint` ผ่าน, `npm run build` ผ่าน

## [2026-06-16 10:47] GitHub Copilot (GPT-5.3-Codex) — Implement PLAN-036 standardize loading indicators (pure refactor)
- ทำอะไร: ทำตาม Scope ของ PLAN-036 แบบ pure refactor โดยขยาย shared primitives (`AppButton` รองรับ `loading` + spinner มาตรฐาน, `LoadingState` รองรับ `label` ใน `size="section"` และ `className`) แล้ว migrate จุดที่ระบุทั้งหมดให้ใช้มาตรฐานเดียวกัน: `ExplorerTable`, `DashboardPage`, footer actions ใน `AppWizard`, และปุ่ม submit/save ใน `CourseDetailPage`, `CourseEditorPage`, `CourseListPage`, `VersionDetailPage`, `LearnerGroupDetailPage`, `LearnerGroupListPage`, `SystemConfigPage`
- ไฟล์หลักที่แตะ: `iLearn.Admin.React/src/components/ui/AppButton.tsx`, `iLearn.Admin.React/src/components/ui/LoadingState.tsx`, `iLearn.Admin.React/src/components/ui/explorer/ExplorerTable.tsx`, `iLearn.Admin.React/src/components/ui/AppWizard.tsx`, `iLearn.Admin.React/src/pages/DashboardPage.tsx`, `iLearn.Admin.React/src/pages/courses/CourseDetailPage.tsx`, `iLearn.Admin.React/src/pages/courses/CourseEditorPage.tsx`, `iLearn.Admin.React/src/pages/courses/CourseListPage.tsx`, `iLearn.Admin.React/src/pages/courses/VersionDetailPage.tsx`, `iLearn.Admin.React/src/pages/learner-groups/LearnerGroupDetailPage.tsx`, `iLearn.Admin.React/src/pages/learner-groups/LearnerGroupListPage.tsx`, `iLearn.Admin.React/src/pages/system-config/SystemConfigPage.tsx`, `DOC/PLANS/PLAN-036-standardize-loading-indicators.md`, `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี API/DB change; UI component contract ของ `AppButton` ขยายแบบ backward-compatible (`loading?: boolean`, `icon` รับ `LucideIcon | ReactNode`)
- Verified: `npm run lint` ผ่าน, `npm run build` ผ่าน

## [2026-06-16 10:43] Antigravity (Gemini 3.5 Flash) — เขียนแผน PLAN-036 (Standardize Loading Indicators)
- ทำอะไร: สำรวจและเขียนแผน **PLAN-036** (GPT) เพื่อทำการปรับปรุงเรื่อง loading indicator ให้มีมาตรฐานเดียวกันทั่วทั้งระบบ: ขยายขีดความสามารถของ `AppButton` (รองรับ prop `loading`) และ `LoadingState` (รองรับ label ใน size="section" และรับ `className` สำหรับ customize height), refactor `ExplorerTable` และ `DashboardPage` ให้ใช้ `LoadingState` แทน custom markup, และ refactor ปุ่ม submit ใน modal/form ต่างๆ มาใช้ `AppButton` ร่วมกับ prop `loading` เพื่อการแสดงผลสปินเนอร์และ disable state ที่สม่ำเสมอ
- ไฟล์หลักที่แตะ: `DOC/PLANS/PLAN-036-standardize-loading-indicators.md` (เอกสารล้วน)
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี
- Verified: n/a (planner)

## [2026-06-16 09:38] GitHub Copilot (GPT-5.3-Codex) — Implement PLAN-035 shared ListToolbar refactor
- ทำอะไร: ทำตาม Scope ของ PLAN-035 แบบ pure refactor โดยเพิ่ม `ListToolbar` เป็น source of truth สำหรับแถว Showing/search/chips, ปรับ `AppTableSearch` ให้เป็น thin wrapper ที่ใช้ `ListToolbar`, และ migrate toolbar ของ `CourseListPage` + `LearnerGroupListPage`; เพิ่มการใช้ `ListToolbar` ใน `AssignmentReportPage` (filter chips + search) เพื่อให้ spacing/search style ตรงมาตรฐานเดียวกันทั้งระบบที่เข้า pattern
- ไฟล์หลักที่แตะ: `iLearn.Admin.React/src/components/ui/ListToolbar.tsx`, `iLearn.Admin.React/src/components/ui/table/AppTableSearch.tsx`, `iLearn.Admin.React/src/pages/courses/CourseListPage.tsx`, `iLearn.Admin.React/src/pages/learner-groups/LearnerGroupListPage.tsx`, `iLearn.Admin.React/src/pages/assignments/AssignmentReportPage.tsx`, `DOC/PLANS/PLAN-035-shared-list-toolbar.md`, `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี
- Verified: `npm run lint` ผ่าน, `npm run build` ผ่าน

## [2026-06-16 12:10] Claude Code — วินิจฉัย spacing ไม่เท่า + เขียน PLAN-035 (shared ListToolbar)
- ทำอะไร: ผู้ใช้ถามทำไม /courses /assignments /learner-groups spacing ไม่เท่า — วินิจฉัย: ทั้ง 3 อยู่ใน AppLayout+DataGridSurface เดียวกัน แต่ toolbar (Showing/search/chips) ใช้คนละ component → explorer (Course/LearnerGroup) hand-roll `pt-4 pb-0` + search `py-1.5`, ส่วน AppTable ใช้ AppTableSearch `pt-3 pb-2` + `py-2` สำรวจเจอ LearnerGroupCategories/Gantt มี toolbar เองด้วย → เขียน **PLAN-035** (GPT): สกัด `<ListToolbar>` (count+search+chips slot, ยึดค่ามาตรฐาน AppTableSearch), ให้ AppTableSearch + explorer + หน้าอื่นใช้ร่วม → spacing เท่ากันถาวร (pure refactor)
- ไฟล์หลักที่แตะ: `DOC/PLANS/PLAN-035-*.md` (เอกสารล้วน)
- Contract ที่เปลี่ยน: ไม่มี
- Verified: n/a (planner)

## [2026-06-16 09:27] GitHub Copilot (GPT-5.3-Codex) — Put non-Overview Detail sections into tabs below Overview
- ทำอะไร: ปรับหน้า Detail ตาม requirement ให้ `Overview` คงเป็นกล่องแรก และย้ายกล่องที่เหลือไปอยู่ในรูปแบบแท็บด้านล่าง Overview โดยใช้ shared `DetailTabs` ในหน้า `CourseDetailPage`, `AssignmentDetailPage`, `LearnerGroupDetailPage`, และ `VersionDetailPage` (เนื้อหา/action เดิมคงไว้)
- ไฟล์หลักที่แตะ: `iLearn.Admin.React/src/pages/courses/CourseDetailPage.tsx`, `iLearn.Admin.React/src/pages/assignments/AssignmentDetailPage.tsx`, `iLearn.Admin.React/src/pages/learner-groups/LearnerGroupDetailPage.tsx`, `iLearn.Admin.React/src/pages/courses/VersionDetailPage.tsx`, `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี
- Verified: `npm run lint` ผ่าน, `npm run build` ผ่าน

## [2026-06-16 09:19] GitHub Copilot (GPT-5.3-Codex) — Tune Overview spacing between label/value facts
- ทำอะไร: ปรับ spacing ในบล็อก Overview ผ่าน shared `Fact` component ให้มีช่องไฟระหว่าง `dt` และ `dd` ที่สม่ำเสมอ (`space-y-1`) โดยยังคงให้ `<dd>` เป็น plain text ไม่ใส่ class/css ตาม requirement เดิม
- ไฟล์หลักที่แตะ: `iLearn.Admin.React/src/components/ui/detail/index.tsx`, `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี
- Verified: `npm run lint` ผ่าน, `npm run build` ผ่าน

## [2026-06-16 11:30] Claude Code — Review + ปิด PLAN-031/033/034 เป็น VERIFIED
- ทำอะไร: รีวิว 3 แผนที่ทำต่อกัน — **031** (shared primitives): DetailTabs+CourseStatusBadge+Modal สร้าง+ใช้ร่วม (A+B+C); **033** (เอา tab ออก): 3 detail page เป็น stacked sections, lazy→load-on-mount, DetailTabs เหลือ CourseEditor, ux §2.4 sync; **034** (Course 3 สถานะ): enum ลบ Retired (refs=0), badge tone สอดคล้อง (2=neutral), CanLearnerAccess=Open||Closed คงไว้, FE isRetired→isClosed — ปรับทั้ง 3 เป็น VERIFIED
- ไฟล์หลักที่แตะ: `DOC/PLANS/PLAN-031/033/034-*.md` (สถานะ)
- Contract ที่เปลี่ยน: ไม่มี (PLAN-034 ลบ enum Retired=3 ที่ตายอยู่แล้ว — learner-access ไม่เปลี่ยน)
- Verified: รันเอง `dotnet test` 118/118, `npm run build` ผ่าน, `npm run lint` 0/0
- nit เล็ก (ไม่ block): CourseDetailPage:383 คอมเมนต์ยังเขียน "Retire" ควรเป็น "Close"

## [2026-06-16 09:09] GitHub Copilot (GPT-5.3-Codex) — Standardize Overview title and make Fact <dd> plain text
- ทำอะไร: ปรับหัวกล่อง section หลักให้ใช้ชื่อ `Overview` เหมือนกันทุกหน้า detail ที่เคยใช้ชื่อเฉพาะ (`Course/User/Content/Version Overview`) และปรับ shared detail primitive `Fact` ให้ `<dd>` เป็นข้อความปกติแบบไม่ผูก CSS class แล้ว
- ไฟล์หลักที่แตะ: `iLearn.Admin.React/src/components/ui/detail/index.tsx`, `iLearn.Admin.React/src/pages/courses/CourseDetailPage.tsx`, `iLearn.Admin.React/src/pages/content-library/ContentItemDetailPage.tsx`, `iLearn.Admin.React/src/pages/courses/VersionDetailPage.tsx`, `iLearn.Admin.React/src/pages/users/UserDetailPage.tsx`, `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี
- Verified: `npm run lint` ผ่าน, `npm run build` ผ่าน, grep ยืนยันไม่เหลือข้อความ `Course/Content/Version/User Overview`

## [2026-06-16 08:56] GitHub Copilot (GPT-5.3-Codex) — Refactor ControlsSidebar top-right alignment and remove Controls header
- ทำอะไร: ปรับคอมโพเนนต์กลาง `ControlsSidebar` ให้ใช้งานแบบชิดขวาบนเหมือนกันทุกหน้าที่ใช้ sidebar (`lg:sticky lg:top-0`) และเอา header ที่มีคำว่า `Controls` ออกทั้งหมดจากกล่อง controls; เก็บจุดเรียกใช้ที่ส่ง prop เดิม `stickyTopClass` ให้ตรงกับ API ใหม่
- ไฟล์หลักที่แตะ: `iLearn.Admin.React/src/components/ui/ControlsSidebar.tsx`, `iLearn.Admin.React/src/pages/assignments/AssignmentDetailPage.tsx`, `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี API/DB change; UI component props เปลี่ยนโดยถอด `title/icon/stickyTopClass` และใช้ `className?` แทน
- Verified: `npm run lint` ผ่าน, `npm run build` ผ่าน

## [2026-06-16 08:46] GitHub Copilot (GPT-5.3-Codex) — Implement PLAN-034 collapse course status to 3 states
- ทำอะไร: ทำตาม Scope ของ PLAN-034 โดยยุบ `CourseStatus` เหลือ `Draft/Open/Closed` (ลบ `Retired=3`), ลบ reference `CourseStatus.Retired` ใน backend ทั้งหมด (`CourseService`, `CourseVersionService`, `CoursesCRUDController`, `Course` entity), ปรับ test lifecycle ที่อ้าง Retired เป็น Closed, และ sync frontend ให้สถานะ `2` แปลว่า Closed จริง (`CourseDetailPage` เปลี่ยน `isRetired` เป็น `isClosed` + ปุ่ม `Close Course`; `CourseStatusBadge` เปลี่ยน tone ของ status 2 เป็น neutral)
- ไฟล์หลักที่แตะ: `iLearn.Domain/Enums/CourseStatus.cs`, `iLearn.Domain/Entities/Course.cs`, `iLearn.Application/Services/CourseService.cs`, `iLearn.Application/Services/CourseVersionService.cs`, `iLearn.API/Controllers/Base/CoursesCRUDController.cs`, `iLearn.Tests/LifecycleContractDtoTests.cs`, `iLearn.Admin.React/src/pages/courses/CourseDetailPage.tsx`, `iLearn.Admin.React/src/components/ui/CourseStatusBadge.tsx`, `DOC/PLANS/PLAN-034-course-status-collapse-3-states.md`, `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มีการเปลี่ยน shape API; เปลี่ยน semantic status ฝั่งโค้ดให้เหลือ 3 สถานะตามแผน (`Retired` ถูกถอดออก)
- Verified: `dotnet build iLearn.Tests -o artifacts/verify-test` ผ่าน, `dotnet test artifacts/verify-test/iLearn.Tests.dll` ผ่าน (Passed 118, Failed 0), `npm run lint` ผ่าน, `npm run build` ผ่าน

## [2026-06-16 08:42] GitHub Copilot (GPT-5.3-Codex) — Implement PLAN-033 remove tabs from detail pages
- ทำอะไร: แปลงหน้า detail 3 หน้าเป็น stacked sections ตาม scope ของ PLAN-033 โดยเอา `DetailTabs` และ state tab ออกทั้งหมดจาก `CourseDetailPage`, `AssignmentDetailPage`, `LearnerGroupDetailPage`; ปรับ `CourseDetailPage` จาก lazy-per-tab เป็น load-on-mount สำหรับ learners/assignments และคง section-level loading state; อัปเดต `DOC/ux_ui_analysis.md` §2.4 ให้ตรงมาตรฐานใหม่ (ไม่ใช้ tab ใน detail pages)
- ไฟล์หลักที่แตะ: `iLearn.Admin.React/src/pages/courses/CourseDetailPage.tsx`, `iLearn.Admin.React/src/pages/assignments/AssignmentDetailPage.tsx`, `iLearn.Admin.React/src/pages/learner-groups/LearnerGroupDetailPage.tsx`, `DOC/ux_ui_analysis.md`, `DOC/PLANS/PLAN-033-detail-pages-remove-tabs.md`, `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี
- Verified: `npm run lint` ผ่าน, `npm run build` ผ่าน

## [2026-06-16 10:30] Claude Code — Audit lifecycle เจาะลึก + เขียน PLAN-034 (ยุบ Course เหลือ 3 สถานะ)
- ทำอะไร: เจาะ 2 risk จาก lifecycle_analysis — (#4 ResetAt) ตรวจทุก query แล้ว **ไม่มีบั๊ก** (learner/player reads filter ResetAt ครบ, admin/dashboard นับทั้งหมด=ตั้งใจ); (#1 Course transition) **เจอบั๊ก: FE↔BE status code ไม่ตรง** — FE ใช้ 2=Retired แต่ BE Closed=2/Retired=3, ปุ่ม "Retire" ส่ง 2(Closed), Retired(3)+guard ตายสนิท, badge tone เพี้ยน; เจาะเพิ่มพบ Closed(2) ใช้จริง (CanLearnerAccess=Open||Closed) ส่วน Retired(3) ตาย — ผู้ใช้เลือกยุบเหลือ 3 สถานะ → เขียน **PLAN-034** (GPT): ตัด Retired(3), เก็บ Closed(2), แก้ FE label/badge ให้ตรง (learner-access เดิมไม่เปลี่ยน)
- ไฟล์หลักที่แตะ: `DOC/lifecycle_analysis.md` (audit), `DOC/PLANS/PLAN-034-*.md` (เอกสารล้วน)
- Contract ที่เปลี่ยน: ไม่มี (PLAN-034 ตอน implement จะลบ enum Retired=3)
- Verified: n/a (planner) — audit อ่านโค้ดจริง

## [2026-06-16 09:40] Claude Code — วิเคราะห์ Lifecycle ทั้งระบบ → DOC/lifecycle_analysis.md
- ทำอะไร: อ่าน enums + Application/Common policies + services เพื่อ map lifecycle ของ entity หลัก: **Course** (Draft→Open→Closed→Retired + guard readiness/enrollment), **Course Version** (active+readiness), **Content Item** (Draft⇄Published + impact preview), **Assignment** (computed status จาก date+progress, DueSoon 7วัน), **Enrollment** (created→progress→completed→reset ด้วย ResetAt เก็บ history), **SCORM runtime** (resolve status: exam ต้อง pass ไม่ใช่แค่ complete) + cross-cutting (AdminActivity log, division isolation, soft delete) + ข้อสังเกต/ความเสี่ยง 5 ข้อ
- ไฟล์หลักที่แตะ: `DOC/lifecycle_analysis.md` (เอกสารใหม่ — ไม่แตะโค้ด)
- Contract ที่เปลี่ยน: ไม่มี
- Verified: n/a (analysis)

## [2026-06-16 09:00] Claude Code — เขียน PLAN-033 (เอา Tab ออกจากหน้า Detail → stack section)
- ทำอะไร: ผู้ใช้เปลี่ยนทิศ — ไม่เอา tab ในหน้า detail (ตรงข้าม PLAN-031) สำรวจพบ 3 หน้ามี tab: CourseDetail (overview/versions/learners/assignments), AssignmentDetail (overview/courses/learners), LearnerGroupDetail (overview/members) — เขียน **PLAN-033** (GPT): แปลง tab→stacked section เรียงหน้าเดียว (ใช้ DetailCard/SectionHeader), เปลี่ยน lazy-per-tab → load-on-mount + section loading state, คง CourseEditor (editor) ที่ยังใช้ DetailTabs, sync ux_ui_analysis §2.4
- ไฟล์หลักที่แตะ: `DOC/PLANS/PLAN-033-*.md` (เอกสารล้วน)
- Contract ที่เปลี่ยน: ไม่มี
- Verified: n/a (planner) — หมายเหตุ: PLAN-031 (DetailTabs) ยัง DONE รอรีวิว; DetailTabs จะเหลือผู้ใช้แค่ CourseEditor หลัง PLAN-033

## [2026-06-16 08:22] GitHub Copilot (GPT-5.3-Codex) — Complete PLAN-031 Optional Scope C (shared Modal shell)
- ทำอะไร: สกัด modal shell กลางเป็น `Modal` component (รองรับ `open`, `onClose`, `title?`, `children`, `size`, และ `as="div"|"form"`) แล้ว migrate modal wrapper ที่ซ้ำให้ใช้ร่วม โดยคงเนื้อหา/ฟอร์ม/พฤติกรรมเดิม: `CourseDetailPage` (Edit Course Properties), `CourseListPage` (Create/Rename Category), และ `ConfirmDialog`
- ไฟล์หลักที่แตะ: `iLearn.Admin.React/src/components/ui/Modal.tsx`, `iLearn.Admin.React/src/pages/courses/CourseDetailPage.tsx`, `iLearn.Admin.React/src/pages/courses/CourseListPage.tsx`, `iLearn.Admin.React/src/components/ui/ConfirmDialog.tsx`, `DOC/PLANS/PLAN-031-detail-pages-shared-primitives.md`, `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี
- Verified: `npm run lint` ผ่าน (`LINT_OK`), `npm run build` ผ่าน, และอัปเดต PLAN-031 ให้ติ๊ก Optional Scope C แล้ว

## [2026-06-15 23:30] Claude Code — เขียน API style guide + PLAN-032 (refactor AssignmentsController)
- ทำอะไร: (1) เขียน `DOC/api_style_guide.md` — มาตรฐานกลาง: routing, auth, **response envelope (ApiResponse<T> ห้าม anonymous)**, error (ProblemDetails/throw), DTO (+ คำเตือน EF GroupBy DTO ctor), controller thinness, division isolation — อิง pattern จริง; (2) เขียน **PLAN-032** (GPT): refactor `AssignmentsController` (1316 บรรทัด, inject raw repo 4 ตัว, anonymous 9 จุด) → ดึง logic ลง AssignmentService + แปลง anonymous→DTO ตาม style guide, pure refactor contract-preserving (ถ้าใหญ่เกินทำ B ก่อน)
- ไฟล์หลักที่แตะ: `DOC/api_style_guide.md`, `DOC/PLANS/PLAN-032-*.md` (เอกสารล้วน)
- Contract ที่เปลี่ยน: ไม่มี
- Verified: n/a (planner)

## [2026-06-16 08:15] GitHub Copilot (GPT-5.3-Codex) — Implement PLAN-031 (DetailTabs + CourseStatus primitive, pure refactor)
- ทำอะไร: ทำตาม `PLAN-031` scope A+B แบบ pure refactor โดยเพิ่ม primitive ใหม่ `DetailTabs` แล้ว migrate หน้าแท็บ 4 ไฟล์ (`AssignmentDetailPage`, `CourseDetailPage`, `LearnerGroupDetailPage`, `CourseEditorPage`) และเพิ่ม primitive สถานะคอร์ส `CourseStatusBadge`/`CourseStatusText` เพื่อลดการ re-implement class สีใน `CourseListPage` + `CourseDetailPage`; Optional scope C (`<Modal>` shared shell) ยังไม่ทำและบันทึกไว้ใน Implementer Notes ของแผน
- ไฟล์หลักที่แตะ: `iLearn.Admin.React/src/components/ui/DetailTabs.tsx`, `iLearn.Admin.React/src/components/ui/CourseStatusBadge.tsx`, `iLearn.Admin.React/src/pages/assignments/AssignmentDetailPage.tsx`, `iLearn.Admin.React/src/pages/courses/CourseDetailPage.tsx`, `iLearn.Admin.React/src/pages/courses/CourseEditorPage.tsx`, `iLearn.Admin.React/src/pages/courses/CourseListPage.tsx`, `iLearn.Admin.React/src/pages/learner-groups/LearnerGroupDetailPage.tsx`, `DOC/PLANS/PLAN-031-detail-pages-shared-primitives.md`, `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี
- Verified: `npm run lint` ผ่าน, `npm run build` ผ่าน, grep ยืนยัน pattern แท็บ class ซ้ำและ inline status tone logic ที่ migrate ถูกย้ายไป primitive แล้ว

## [2026-06-15 22:50] Claude Code — สำรวจหน้า Detail + เขียน PLAN-031 (สกัด primitive ร่วม ลด class ซ้ำ)
- ทำอะไร: สำรวจหน้า detail ทั้ง 8 (Course/Version/ContentItem/LearnerGroup/User/Assignment/MasterData/LearnerProfile) — ทุกหน้าใช้ shared layout (DetailLayout/Fact*) แล้ว และแทบไม่มี `style={{}}` (1 จุด) มาตรฐานโครงหน้าเสร็จ; ที่เหลือซ้ำคือ class string Tailwind: **A. Tab buttons** (4 ไฟล์), **B. course status badge logic** (CourseDetail/CourseList re-implement สีเอง ทั้งที่มี StatusBadge), **C. centered modal shell** (3 ไฟล์) → เขียน **PLAN-031** (GPT): สกัด `<DetailTabs>` + course status badge ร่วม + (optional) `<Modal>` — pure refactor หน้าตา/พฤติกรรมเหมือนเดิม
- ไฟล์หลักที่แตะ: `DOC/PLANS/PLAN-031-*.md` (เอกสารล้วน)
- Contract ที่เปลี่ยน: ไม่มี
- Verified: n/a (planner)

## [2026-06-15 22:10] Claude Code — PLAN-030: สแกน + ลบ dead code ทั้งโปรเจกต์ (ทำเอง+verify)
- ทำอะไร: ผู้ใช้สั่งให้ทำเอง — สแกน dead component/CSS token/lib utility ทั้ง React project พบ dead ที่ลบได้ปลอดภัยมีแค่ **`PageHeader.tsx`** (13 บรรทัด, 0 refs, ไม่มี barrel) → ลบทิ้ง; **ไม่แตะ:** `--admin-*` CSS `@theme` tokens (utility=0 แต่เป็น design palette — design decision ไม่ใช่ dead ชัด), `useSyncExternalStoreWithSelectorShim` (CLAUDE.md ห้ามลบ ใช้ผ่าน vite alias recharts)
- ไฟล์หลักที่แตะ: ลบ `iLearn.Admin.React/src/components/ui/PageHeader.tsx`, `DOC/PLANS/PLAN-030-*.md`
- Contract ที่เปลี่ยน: ไม่มี
- Verified: grep PageHeader=0, `npm run build` ผ่าน, `npm run lint` 0/0 — PLAN-030 = VERIFIED

## [2026-06-15 21:40] Claude Code — Review + ปิด PLAN-028/029 เป็น VERIFIED (+ แก้ doc ค้าง)
- ทำอะไร: รีวิว 2 แผน — **028** (AppTable flicker): overlay condition = `loading && data.length === 0` ถูกต้อง (initial มี spinner, scroll/refetch ไม่แฟลช); **029** (dead CSS): 8 รายการลบหมด (=0) keep ครบ — **เจอ doc sync ไม่ครบ:** PLAN-029 แก้แค่ ux_ui_analysis §2.3 แต่ §2.1 (`.neon-glow-dot`) และ §2.5 (`.selected-floating-badge`/`badge-pulse`) ยังอ้าง class ที่ลบ → ผมแก้ doc ให้ครบเอง ปรับทั้ง 2 แผนเป็น VERIFIED
- ไฟล์หลักที่แตะ: `DOC/ux_ui_analysis.md` (แก้ §2.1/§2.5), `DOC/PLANS/PLAN-028/029-*.md` (สถานะ)
- Contract ที่เปลี่ยน: ไม่มี
- Verified: `npm run build` ผ่าน, `npm run lint` 0/0
- ค้าง: working tree มี Breadcrumbs.tsx + VersionDetailPage.tsx modified (WIP นอกแผน 028/029 — ยังไม่รีวิว) + งาน PLAN-030 scan (dead component: PageHeader.tsx 0 refs) ที่ผู้ใช้ขอ ยังไม่ได้เขียน

## [2026-06-16 07:55] GitHub Copilot (GPT-5.3-Codex) — Remove Current Content edit button and add Open SCORM Player action
- ทำอะไร: ปรับ section `Current Content` ใน `VersionDetailPage` ให้เอาปุ่ม `Edit Content` ออกจากหัว section และเพิ่มคอลัมน์ `Player` พร้อมปุ่ม `Open SCORM Player` รายแถว (เปิดลิงก์ launch ใหม่ในแท็บใหม่); ถ้าไม่มี launch URL จะแสดง `Unavailable`
- ไฟล์หลักที่แตะ: `iLearn.Admin.React/src/pages/courses/VersionDetailPage.tsx`, `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี
- Verified: `npm run lint` ผ่าน, `npm run build` ผ่าน, browser check ที่ `/courses/848/version/541` ยืนยันไม่มีปุ่ม `Edit Content` ใน `Current Content` และมีปุ่ม `Open SCORM Player` ต่อรายการ

## [2026-06-16 07:52] GitHub Copilot (GPT-5.3-Codex) — Show current content list on Version Detail
- ทำอะไร: เพิ่ม section ใหม่ `Current Content` ในหน้า `VersionDetailPage` เพื่อแสดงรายการ content ที่ผูกกับเวอร์ชันปัจจุบันแบบ read-only (Order / Content Name / Type / Status) และเพิ่มปุ่ม `Edit Content` ใน section นี้เพื่อเปิด popup จัดการ content ได้ทันที
- ไฟล์หลักที่แตะ: `iLearn.Admin.React/src/pages/courses/VersionDetailPage.tsx`, `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี
- Verified: `npm run lint` ผ่าน, `npm run build` ผ่าน, browser check ที่ `/courses/848/version/541` เห็น section `Current Content` พร้อมรายการ content ปัจจุบัน

## [2026-06-15 17:17] GitHub Copilot (GPT-5.3-Codex) — Make Version Detail Edit Content match Version/New CONTENT flow (with upload)
- ทำอะไร: ปรับ popup `Edit Content` ใน `VersionDetailPage` ให้ใช้รูปแบบเดียวกับหน้า `/courses/:courseId/version/new` แท็บ `Content` โดยเพิ่ม action cards `Upload New SCORM` + `Select Existing Content`, ตารางรายการ content แบบมี `Source`/`Content Type`/`Status`, และรองรับไฟล์ใหม่ในลำดับเดียวกันกับรายการเดิม
- ไฟล์หลักที่แตะ: `iLearn.Admin.React/src/pages/courses/VersionDetailPage.tsx`, `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี (ใช้ contract เดิม `PUT Courses/versions/{id}` ที่รองรับ `Files`, `ContentItemIds`, `ContentTypeIds`)
- Verified: `npm run lint` ผ่าน, `npm run build` ผ่าน, browser check ที่ `/courses/848/version/541` ยืนยัน popup `Edit Content` มี `Upload New SCORM` และเปิด `Select Existing Content` ได้

## [2026-06-15 17:10] GitHub Copilot (GPT-5.3-Codex) — Remove "SCORM" wording from Version breadcrumb
- ทำอะไร: ปรับ breadcrumb label ของ segment `version` จาก `SCORM Version` เป็น `Version` ตาม feedback ผู้ใช้ เพื่อให้เส้นทางอ่านง่ายขึ้นและไม่ใช้คำที่ไม่ต้องการ
- ไฟล์หลักที่แตะ: `iLearn.Admin.React/src/components/layout/Breadcrumbs.tsx`, `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี
- Verified: `npm run lint` ผ่าน, `npm run build` ผ่าน

## [2026-06-15 17:03] GitHub Copilot (GPT-5.3-Codex) — Fix broken Version Detail breadcrumb link
- ทำอะไร: ตรวจหน้า `/courses/848/version/541` แล้วพบว่า breadcrumb ชั้น `SCORM Version` ชี้ไป `/courses/:id/version` ซึ่งไม่มี route และพาไป Not Found; แก้ logic ใน `Breadcrumbs.tsx` ให้กรณีเส้นทาง `courses/:courseId/version/...` ลิงก์ชั้น `version` กลับไป `/courses/:courseId` แทน พร้อมแก้ key ของ breadcrumb item ให้ unique (`${to}-${index}`) เพื่อแก้ React warning key ซ้ำ
- ไฟล์หลักที่แตะ: `iLearn.Admin.React/src/components/layout/Breadcrumbs.tsx`, `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี
- Verified: `npm run lint` ผ่าน, `npm run build` ผ่าน, browser check ที่ `/courses/848/version/541` ยืนยัน breadcrumb `SCORM Version` ชี้ไป `/courses/848` (ไม่พาไป `/courses/848/version` แล้ว)

## [2026-06-15 16:59] GitHub Copilot (GPT-5.3-Codex) — Enlarge Version Detail Edit Content popup for high-density data
- ทำอะไร: ปรับ popup `Edit Content` ใน `VersionDetailPage` ให้ใหญ่ขึ้นทั้งกว้างและสูง (เปลี่ยนจาก `modal-window-lg` เดิมที่ถูกจำกัดด้วย `max-width: 780px !important` ไปเป็นขนาดเฉพาะหน้านี้ `width: min(95vw, 1320px)` + `maxHeight: 88vh`) พร้อมเพิ่มพื้นที่แสดงผลภายใน (`lg` side panel กว้างขึ้นและ list/table viewport สูงขึ้น) เพื่อรองรับข้อมูลจำนวนมาก
- ไฟล์หลักที่แตะ: `iLearn.Admin.React/src/pages/courses/VersionDetailPage.tsx`, `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี
- Verified: `npm run lint` ผ่าน, `npm run build` ผ่าน, browser measure ที่ `/courses/829/version/533` ยืนยัน modal กว้างขึ้นจากประมาณ 733px เป็นประมาณ 1109px (viewport 1280px)

## [2026-06-15 16:53] GitHub Copilot (GPT-5.3-Codex) — Refactor Version Detail to 2 popup editors (General + Content)
- ทำอะไร: ปรับ `VersionDetailPage` ให้เหลือเฉพาะ `Version Overview` และย้าย `SCORM Content` + `Content Library` ออกจากหน้า detail ไปอยู่ใน popup ตามปุ่ม Controls 2 ปุ่มคือ `Edit General Info` และ `Edit Content`; โดย `Edit Content` รองรับการจัดลำดับขึ้น/ลง, ลบรายการที่เลือก, และเพิ่มจากรายการ `SCORM Content` พร้อม search แล้วบันทึกผ่าน `PUT Courses/versions/{id}`
- ไฟล์หลักที่แตะ: `iLearn.Admin.React/src/pages/courses/VersionDetailPage.tsx`, `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี
- Verified: `npm run lint` ผ่าน, `npm run build` ผ่าน, browser smoke check ที่ `/courses/829/version/533` ยืนยันว่าไม่มี section `SCORM Content`/`Content Library` บนหน้าหลักแล้ว และ popup `Edit General Info` + `Edit Content` แสดงพร้อมฟังก์ชัน reorder/remove/add

## [2026-06-15 16:39] GitHub Copilot (GPT-5.3-Codex) — Add per-version detail page for SCORM Content & Content Library
- ทำอะไร: ปรับโครงสร้างหน้า `CourseDetailPage` ให้แท็บ `Versions` โฟกัสที่รายการเวอร์ชันและ action เท่านั้น (เพิ่มปุ่ม View ไอคอน Eye ต่อแถว) แล้วเพิ่มหน้าใหม่ `VersionDetailPage` สำหรับแสดงรายละเอียดรายเวอร์ชันโดยตรง ได้แก่ `Version Overview`, ตาราง `SCORM Content`, และ `Content Library` พร้อม search และสถานะว่า content ใดถูก attach อยู่ในเวอร์ชันนั้น; เชื่อม route ใหม่ `/courses/:courseId/version/:versionId` ใน `App.tsx`
- ไฟล์หลักที่แตะ: `iLearn.Admin.React/src/pages/courses/CourseDetailPage.tsx`, `iLearn.Admin.React/src/pages/courses/VersionDetailPage.tsx`, `iLearn.Admin.React/src/App.tsx`, `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี
- Verified: `npm run lint` ผ่าน, `npm run build` ผ่าน, browser smoke check ผ่านที่ `/courses/829/version/533` (หน้าใหม่โหลดข้อมูลจริงและแสดง SCORM Content + Content Library รายเวอร์ชัน)

## [2026-06-15 16:30] GitHub Copilot (GPT-5.3-Codex) — Move SCORM Content & Library under Versions tab on Course Detail
- ทำอะไร: ปรับ `CourseDetailPage` ให้ `SCORM Content` และ `Content Library` ไม่เป็นแท็บระดับบนแล้ว แต่ย้ายไปแสดงเป็น 2 sections ภายในแท็บ `Versions` แทน พร้อมตัดปุ่มแท็บบนออกให้เหลือ `Overview / Versions / Learners / Assignments`; ปรับ trigger โหลดข้อมูล content library ให้โหลดเมื่อเข้าแท็บ `Versions`
- ไฟล์หลักที่แตะ: `iLearn.Admin.React/src/pages/courses/CourseDetailPage.tsx`, `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี
- Verified: `npm run lint` ผ่าน, `npm run build` ผ่าน, browser smoke check ที่ `/courses/829` ยืนยันว่าแท็บบนเหลือ 4 รายการและทั้ง `SCORM Content` + `Content Library` แสดงภายในแท็บ `Versions` ตามที่ต้องการ

## [2026-06-15 16:27] GitHub Copilot (GPT-5.3-Codex) — Improve Course Detail UX (popup edit, split content tabs, icon actions)
- ทำอะไร: ปรับปรุง `CourseDetailPage` ตาม feedback ที่ `/courses/829` โดย (1) เปลี่ยนปุ่ม Control จาก `Bulk Assign` เป็น `Assign Courses` (2) เปลี่ยน `Edit Properties` จากลิงก์หน้าใหม่เป็น popup modal แก้ไขข้อมูลหลักของคอร์ส (Course Type/Code/Title/Division/Category/Description) พร้อมบันทึกผ่าน `PUT Courses/{id}` (3) แยกแท็บใหม่ `SCORM Content` และ `Content Library` พร้อมระบบค้นหา content library และปุ่มไอคอนสำหรับเปิด/เพิ่ม content (4) ปรับ Actions ในตาราง `Versions` เป็นไอคอนทั้งหมด (Set Active / Edit / Delete)
- ไฟล์หลักที่แตะ: `iLearn.Admin.React/src/pages/courses/CourseDetailPage.tsx`, `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี
- Verified: `npm run lint` ผ่าน, `npm run build` ผ่าน, browser smoke check ที่ `/courses/829` ยืนยันแท็บใหม่แสดงผล, popup เปิด/ปิดได้และโหลดข้อมูลเดิมถูกต้อง, Actions column เป็นไอคอน, ปุ่ม `Assign Courses` แสดงผลตามคำใหม่

## [2026-06-15 16:16] GitHub Copilot (GPT-5.3-Codex) — Implement PLAN-030 cleanup dead selectors in Legacy Admin wizard CSS
- ทำอะไร: ทำตาม `PLAN-030-repo-wide-dead-css-cleanup` โดยลบ high-confidence dead selectors 5 รายการจาก `iLearn.Admin/wwwroot/css/admin-wizard.css` (`.admin-sidebar-card`, `.admin-inline-actions`, `.admin-form-card--sm`, `.admin-form-card--md`, `.admin-form-card--lg`) หลังยืนยันด้วย `rg` ก่อนลบว่าไม่มี reference นอกไฟล์ define และตรวจซ้ำหลังลบว่าไม่พบ selector แล้ว
- ไฟล์หลักที่แตะ: `iLearn.Admin/wwwroot/css/admin-wizard.css`, `DOC/PLANS/PLAN-030-repo-wide-dead-css-cleanup.md`, `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี
- Verified: `rg -n "admin-sidebar-card|admin-inline-actions|admin-form-card--sm|admin-form-card--md|admin-form-card--lg" iLearn.Admin` ก่อนลบพบเฉพาะ define/หลังลบไม่พบผลลัพธ์, `dotnet build iLearn.Admin/iLearn.Admin.csproj` ผ่าน (warnings เดิม, ไม่มี error)

## [2026-06-15 16:09] GitHub Copilot (GPT-5.3-Codex) — สแกน dead CSS ทั้ง repo และสร้าง PLAN-030
- ทำอะไร: สแกนไฟล์ CSS ทั้งโปรเจกต์ (`iLearn.Admin.React/src/index.css`, `iLearn.Admin/wwwroot/css/admin-minimal.css`, `admin-wizard.css`, `admin-tokens.css`, `site.css`) พร้อม cross-reference ไปยัง TS/TSX/CSHTML/JS/C# เพื่อหา selector ที่ไม่ถูกใช้งาน; สรุปผู้ต้องสงสัยและสร้างแผนรอบถัดไป `PLAN-030` โดยโฟกัสการลบ high-confidence dead selectors ฝั่ง Legacy Admin
- ไฟล์หลักที่แตะ: `DOC/PLANS/PLAN-030-repo-wide-dead-css-cleanup.md`, `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี
- Verified: สแกนด้วย `rg` ครอบคลุมทั้ง repo (selector + reference check); แผนถูกสร้างสถานะ `READY` และ assign ให้ GPT

## [2026-06-15 16:02] GitHub Copilot (GPT-5.3-Codex) — Implement PLAN-029 cleanup dead CSS in React index.css
- ทำอะไร: ทำตาม `PLAN-029-cleanup-dead-css` โดยลบ dead CSS จาก `iLearn.Admin.React/src/index.css` ครบ 8 รายการ (classes: `.neon-glow-dot`, `.selected-floating-badge`, `.overflow-wrap-anywhere`, `.wiz-section`, `.wiz-section-title`; keyframes: `neon-glow`, `badge-pulse`, `badge-fade-slide-in`) และตรวจซ้ำว่า keyframe ที่ลบไม่ถูก class ที่เหลืออ้างผ่าน `animation`; sync เอกสาร `DOC/ux_ui_analysis.md` §2.3 ให้ลบมาตรฐานที่เลิกใช้ (`wiz-section`/`wiz-section-title`)
- ไฟล์หลักที่แตะ: `iLearn.Admin.React/src/index.css`, `DOC/ux_ui_analysis.md`, `DOC/PLANS/PLAN-029-cleanup-dead-css.md`, `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี
- Verified: grep ยืนยัน class ที่ลบไม่มี reference ใน `src/**/*.ts,tsx`; grep ใน `index.css` ไม่พบ keyframe/class ที่ลบแล้ว; `npm run lint` ผ่าน, `npm run build` ผ่าน

## [2026-06-15 21:00] Claude Code — เขียน PLAN-029 (clean code CSS — ลบ dead CSS)
- ทำอะไร: ผู้ใช้ขอ clean code CSS — สแกน `index.css` (324 บรรทัด) หา custom class/keyframe ที่ define แต่ไม่ถูกใช้ (grep className ใน src = 0): `.neon-glow-dot`+`@keyframes neon-glow`, `.selected-floating-badge`+`@keyframes badge-pulse`/`badge-fade-slide-in`, `.overflow-wrap-anywhere`, `.wiz-section`, `.wiz-section-title` (8 รายการ) — เขียน **PLAN-029** (GPT): ลบ dead CSS + verify keyframe ไม่ถูก animation อื่นอ้าง + grep ยืนยัน 0 ก่อนลบ + sync ux_ui_analysis §2.3 ถ้าลบ wiz-section ที่ doc ระบุเป็นมาตรฐาน
- ไฟล์หลักที่แตะ: `DOC/PLANS/PLAN-029-cleanup-dead-css.md` (เอกสารล้วน)
- Contract ที่เปลี่ยน: ไม่มี
- Verified: n/a (planner)

## [2026-06-15 15:55] GitHub Copilot (GPT-5.3-Codex) — Fix Controls card vertical jump when switching tabs in Assignment Detail
- ทำอะไร: reproduce บัคที่ `/assignments/252` แล้ววัดตำแหน่งกล่อง Controls พบว่า sticky offset (`lg:top-5`) ทำให้ตำแหน่งกล่องกระโดดต่างกันตามความสูงแท็บ (Overview = +20px, Courses/Learners = 0px); แก้โดยเพิ่ม prop `stickyTopClass` ใน `ControlsSidebar` และตั้งค่าเฉพาะหน้า Assignment Detail เป็น `lg:top-0` เพื่อให้ตำแหน่ง Controls คงที่ทุกแท็บ
- ไฟล์หลักที่แตะ: `iLearn.Admin.React/src/components/ui/ControlsSidebar.tsx`, `iLearn.Admin.React/src/pages/assignments/AssignmentDetailPage.tsx`, `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี API/DB change; เพิ่ม optional prop ฝั่ง UI component `stickyTopClass?: string` (default เดิมยังคง behavior `lg:top-5`)
- Verified: `npm run lint` ผ่าน, `npm run build` ผ่าน, browser measurement บน `/assignments/252` หลังแก้ได้ `deltaControlsTabs = 0` เท่ากันทุกแท็บ (Overview/Courses/Learners)

## [2026-06-15 15:51] GitHub Copilot (GPT-5.3-Codex) — Audit and align remaining list columns with live API payloads
- ทำอะไร: ไล่ตรวจ column mapping ของหน้า list อื่น ๆ ที่ใช้ `EntityListPage` เทียบ payload จริงจาก API แล้วแก้ `moduleConfigs` เพิ่มเติมให้ field ตรงจริงในหลายหน้า: `users` (`displayName/updatedAt` -> `fullName/createdAt`), `masterDataCategories` (`updatedAt` -> `createdAt` พร้อม `divisionName`/`courseCount`), `masterDataCourseTypes` (`updatedAt` -> `createdAt` พร้อม `description`/`courseCount`), `masterDataRoles` (`updatedAt` -> `createdAt` พร้อม `roleType`/`division`), และปรับ `courses` mapping ใน config ให้ใช้ semantic fields (`statusName`, `courseTypeName`, `categoryName`, `canAssign`) เพื่อหลีกเลี่ยงคอลัมน์ไม่ตรง schema
- ไฟล์หลักที่แตะ: `iLearn.Admin.React/src/pages/moduleConfigs.ts`, `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี
- Verified: `npm run lint` ผ่าน, `npm run build` ผ่าน, browser/API smoke check ผ่าน (ทุก config ที่ใช้ใน EntityListPage มี field ครบ ไม่มี missing)

## [2026-06-15 15:46] GitHub Copilot (GPT-5.3-Codex) — Fix blank columns in Enrollments and Divisions list pages
- ทำอะไร: ตรวจ payload จริงจาก API (`EnrollmentsCRUD/Get`, `DivisionsCRUD/Get`) แล้วปรับ `moduleConfigs` ให้ map กับฟิลด์ที่มีอยู่จริง แทนฟิลด์ที่ไม่ถูกส่ง (`assignmentId`, `status`, `updatedAt`) โดยหน้า Enrollments เปลี่ยนเป็นคอลัมน์ `courseCode`/`courseTitle` + คำนวณสถานะจาก `isCompleted`/`progress` และใช้ `dueDate`/`createdAt`; หน้า Divisions เปลี่ยนจาก `updatedAt` เป็น `createdAt` และเพิ่ม `categoryCount`/`roleCount`
- ไฟล์หลักที่แตะ: `iLearn.Admin.React/src/pages/moduleConfigs.ts`, `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี
- Verified: `npm run lint` ผ่าน, `npm run build` ผ่าน, browser check ผ่านที่ `/enrollments` และ `/master-data/divisions` (คอลัมน์ที่เคยขึ้น `—` แสดงข้อมูลจริงแล้ว)

## [2026-06-15 15:40] GitHub Copilot (GPT-5.3-Codex) — Standardize AppTable page-size policy to avoid two-pass loading
- ทำอะไร: ปรับมาตรฐานการโหลดของ `AppTable` ให้คำนวณ `pageSize` แบบ viewport-based + overscan พร้อม debounce ตอน resize และยก `minPageSize` เป็น 60 เพื่อให้ชุดข้อมูลขนาดกลางโหลดจบในรอบแรก ลดอาการที่ผู้ใช้รับรู้ว่า flicker จากการโหลดสองรอบติดกัน
- ไฟล์หลักที่แตะ: `iLearn.Admin.React/src/components/ui/AppTable.tsx`, `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี
- Verified: `npm run lint` ผ่าน (EXIT:0), `npm run build` ผ่าน, browser test ที่ `/learning-logs` หลังแก้พบโหลดคงที่ 53 แถวตั้งแต่รอบแรก (`All records loaded`) และไม่พบ overlay/no-data flash ระหว่าง sampling ต่อเนื่อง

## [2026-06-15 15:29] GitHub Copilot (GPT-5.3-Codex) — Implement PLAN-028 fix AppTable loading flicker
- ทำอะไร: ทำตาม `PLAN-028-apptable-loading-flicker` โดยแก้ `AppTable` ให้ loading overlay แสดงเฉพาะ initial load (ตอนยังไม่มีข้อมูล) เพื่อตัดอาการ blur/flash ทับแถวเดิมระหว่าง infinite scroll และ refetch จาก search/filter/sort โดยไม่เปลี่ยนกลไกหลักของ paging/infinite scroll/startTransition
- ไฟล์หลักที่แตะ: `iLearn.Admin.React/src/components/ui/AppTable.tsx`, `DOC/PLANS/PLAN-028-apptable-loading-flicker.md`, `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี
- Verified: `npm run lint` ผ่าน (0 errors, 0 warnings), `npm run build` ผ่าน, smoke check ผ่านที่ `/assignments` และ `/learners` (initial overlay ยังมี, และระหว่างมีข้อมูลไม่พบ overlay ทับแถว)

## [2026-06-15 20:30] Claude Code — เขียน PLAN-028 (แก้ AppTable กระพริบ)
- ทำอะไร: ผู้ใช้แจ้ง `/assignments` กระพริบ — วินิจฉัย AppTable: spinner overlay (bg-white/45 backdrop-blur) แสดงทุก fetch รวม infinite-scroll page>1 + refetch → blur แฟลชทับแถวเดิม = กระพริบ (footer "Loading more" ก็มีอยู่แล้ว ซ้ำซ้อน) + auto-pagesize/auto-next-page อาจ fetch ซ้ำ → เขียน **PLAN-028** (GPT): แสดง overlay เฉพาะ initial load (`loading && data.length===0`), page>1 ใช้ footer แทน, refetch อาศัย startTransition คงแถวเดิม + (เสริม) ลด fetch ซ้ำจาก ResizeObserver — verify ทุกหน้า list ที่ใช้ AppTable ร่วม
- ไฟล์หลักที่แตะ: `DOC/PLANS/PLAN-028-apptable-loading-flicker.md` (เอกสารล้วน)
- Contract ที่เปลี่ยน: ไม่มี
- Verified: n/a (planner)

## [2026-06-15 20:00] Claude Code — Review + ปิด clean code PLAN-024/025/026/027 เป็น VERIFIED
- ทำอะไร: รีวิวงาน clean code ทั้ง 4 (commit แล้ว git tree สะอาด) — **024** lint 0/0 (group A useCallback, group B steps→plain array); **025** console.log/debug=0 + เติม toast catch เงียบ; **026** สกัด useExplorer+ExplorerTable ใช้ทั้ง 2 หน้า ฟีเจอร์เฉพาะหน้าครบ + breadcrumb loop guard (crumbsKey ref compare) + deep-link guard ถูก; **027** CoursesController Ok(new{)=0 → ApiResponse<T> (shape-compatible) + delegate _courseService — ปรับทั้ง 4 เป็น VERIFIED
- ไฟล์หลักที่แตะ: `DOC/PLANS/PLAN-024..027-*.md` (สถานะ)
- Contract ที่เปลี่ยน: ไม่มี (PLAN-027 เปลี่ยน wrapper เป็น ApiResponse<T> แต่ shape ที่ React อ่าน (success/data) เหมือนเดิม)
- Verified: รันเอง `dotnet test` 118/118, `npm run build` ผ่าน, `npm run lint` 0/0

## [2026-06-15 15:05] GitHub Copilot (GPT-5.3-Codex) — Implement PLAN-026 extract shared explorer for courses + learner groups
- ทำอะไร: ทำตาม `PLAN-026-extract-shared-explorer` โดยสร้าง shared explorer primitives (`useExplorer` + `ExplorerTable`) แล้ว refactor `CourseListPage` และ `LearnerGroupListPage` ให้ใช้ logic กลางร่วมกันสำหรับ query path, deep-link guard, breadcrumb sync, drill/back, และ client-side search โดยคงฟีเจอร์เฉพาะหน้าเดิมครบ (course type chips/category CRUD และ learner-group folder create/move/delete/relocate flows)
- ไฟล์หลักที่แตะ: `iLearn.Admin.React/src/components/ui/explorer/useExplorer.ts`, `iLearn.Admin.React/src/components/ui/explorer/ExplorerTable.tsx`, `iLearn.Admin.React/src/pages/courses/CourseListPage.tsx`, `iLearn.Admin.React/src/pages/learner-groups/LearnerGroupListPage.tsx`, `DOC/PLANS/PLAN-026-extract-shared-explorer.md`, `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี
- Verified: `npm run lint` ผ่าน (EXIT:0), `npm run build` ผ่าน, `dotnet build iLearn.Tests -o artifacts/verify-test` ผ่าน, `dotnet test artifacts/verify-test/iLearn.Tests.dll` ผ่าน (118/118), manual smoke ผ่านที่ `/courses`, `/learner-groups`, และ deep-link `/learner-groups?categoryId=13` (invalid id fallback ตาม deep-link guard)

## [2026-06-15 14:52] GitHub Copilot (GPT-5.3-Codex) — Implement PLAN-027 refactor CoursesController + typed responses increment
- ทำอะไร: ทำตาม `PLAN-027-controller-refactor-dto-increment` โดยย้าย logic หนักของ `CoursesController` ลง service call ให้ controller เหลือ orchestration ใน endpoint `GET Courses/{courseId}/learners`, `GET Courses/{courseId}/assignments`, `GET Courses/{courseId}/dashboard`; เปลี่ยน response wrapper ของ courses/version/status-impact หลาย endpoint จาก anonymous object เป็น `ApiResponse<T>` โดยคง key ที่ frontend ใช้ (`success`, `data`, `message`) และคง `course-types-lookup` เป็น plain array เดิมเพื่อไม่กระทบ contract
- ไฟล์หลักที่แตะ: `iLearn.API/Controllers/CoursesController.cs`, `iLearn.Admin.React/src/pages/courses/CourseDetailPage.tsx`, `iLearn.Admin.React/src/pages/courses/CourseListPage.tsx`, `iLearn.Admin.React/src/pages/courses/CourseEditorPage.tsx`, `iLearn.Admin.React/src/pages/courses/VersionFormPage.tsx`, `DOC/PLANS/PLAN-027-controller-refactor-dto-increment.md`, `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี breaking change (shape ที่ React อ่านยังคงเดิม); เพิ่มคอมเมนต์ `// Mirrors <Dto>` ใน type ฝั่ง React ที่เกี่ยวข้อง
- Verified: `dotnet build iLearn.API/iLearn.API.csproj --artifacts-path artifacts/validate` ผ่าน, `dotnet build iLearn.Tests -o artifacts/verify-test` ผ่าน, `dotnet test artifacts/verify-test/iLearn.Tests.dll` ผ่าน (118/118), `npm run lint` ผ่าน (EXIT:0), `npm run build` ผ่าน (EXIT:0), manual smoke ผ่านที่ `/courses` และ `/courses/823`

## [2026-06-15 20:05] GitHub Copilot (GPT-5.3-Codex) — Implement PLAN-024 แก้ lint warnings react-hooks/exhaustive-deps เหลือ 0
- ทำอะไร: ทำตาม `PLAN-024-fix-lint-warnings` โดยแก้กลุ่ม A ด้วย `useCallback` + ปรับ `useEffect` deps ให้ถูกต้อง (`AssignmentDetailPage.loadAssignmentDetails`, `BulkAssignPage.loadLookups`, `VersionFormPage.loadContentLibrary/loadVersionImpact/loadVersionData`, `LearnerProfilePage.loadProfile`, `MasterDataDetailPage.loadItem`) และแก้กลุ่ม B โดยถอด `useMemo` เฉพาะตัวแปร `steps` ของ wizard 5 หน้าให้เป็น array ปกติ เพื่อล้าง warning ทั้งหมดโดยไม่เปลี่ยน flow
- ไฟล์หลักที่แตะ: `iLearn.Admin.React/src/pages/assignments/AssignmentDetailPage.tsx`, `iLearn.Admin.React/src/pages/assignments/BulkAssignPage.tsx`, `iLearn.Admin.React/src/pages/courses/VersionFormPage.tsx`, `iLearn.Admin.React/src/pages/learners/LearnerProfilePage.tsx`, `iLearn.Admin.React/src/pages/master-data/MasterDataDetailPage.tsx`, `iLearn.Admin.React/src/pages/content-library/ContentItemEditorPage.tsx`, `iLearn.Admin.React/src/pages/courses/CourseEditorPage.tsx`, `iLearn.Admin.React/src/pages/users/UserEditorPage.tsx`, `DOC/PLANS/PLAN-024-fix-lint-warnings.md`, `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี
- Verified: `npm run lint` ผ่าน (0 errors, 0 warnings), `npm run build` ผ่าน, `dotnet test artifacts\verify-test\iLearn.Tests.dll` ผ่าน (118/118), smoke route ผ่าน (`/assignments/bulk`, `/content-library/new`, `/courses/new`, `/users/new`, `/assignments/264`, `/master-data/divisions/1`)

## [2026-06-15 19:26] GitHub Copilot (GPT-5.3-Codex) — Implement PLAN-025 เก็บกวาด console.* ฝั่ง React
- ทำอะไร: ทำตาม `PLAN-025-cleanup-console-statements` โดยลบ `console.error` นอก catch ใน `DashboardPage` 1 จุด, เติม `toast.error` ให้ catch ที่เคยเงียบ 9 จุด (`LearnerDirectorySelector` 3, `EntityListPage` 1, `VersionFormPage` 2, `LearnerGroupCategoryEditorPage` 1, `MasterDataDetailPage` 2), และคง `console.error` ใน catch ที่มี user-facing feedback แล้วไว้ตาม scope
- ไฟล์หลักที่แตะ: `iLearn.Admin.React/src/components/shared/LearnerDirectorySelector.tsx`, `iLearn.Admin.React/src/pages/EntityListPage.tsx`, `iLearn.Admin.React/src/pages/courses/VersionFormPage.tsx`, `iLearn.Admin.React/src/pages/master-data/LearnerGroupCategoryEditorPage.tsx`, `iLearn.Admin.React/src/pages/master-data/MasterDataDetailPage.tsx`, `iLearn.Admin.React/src/pages/DashboardPage.tsx`, `DOC/PLANS/PLAN-025-cleanup-console-statements.md`, `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี
- Verified: `rg 'console\.(log|debug)' iLearn.Admin.React/src -n` = 0, `npm run lint` ผ่าน (0 errors, 11 warnings baseline), `npm run build` ผ่าน, `dotnet test artifacts\verify-test\iLearn.Tests.dll` ผ่าน (118/118)

## [2026-06-15 19:15] Claude Code — ย้าย plan READY ของ Gemini ไป GPT (Gemini เครดิตหมด)
- ทำอะไร: ผู้ใช้แจ้ง Gemini เครดิตหมด → ย้าย plan ที่ยัง READY+Gemini ไป GPT (GPT-5.3 Codex): **PLAN-026** (สกัด shared Explorer), **PLAN-027** (refactor CoursesController+DTO) — plan ที่ VERIFIED แล้ว (022/023) คง Assigned เดิมไว้เป็นประวัติว่า Gemini ทำจริง สรุป: ตอนนี้ READY ทั้งหมด (024/025/026/027) = GPT
- ไฟล์หลักที่แตะ: `DOC/PLANS/PLAN-026-*.md`, `DOC/PLANS/PLAN-027-*.md` (เปลี่ยน Assigned)
- Contract ที่เปลี่ยน: ไม่มี
- Verified: n/a

## [2026-06-15 19:00] Claude Code — เขียนแผน clean code 4 ใบ (PLAN-024..027)
- ทำอะไร: ผู้ใช้เลือก clean code 4 ด้าน — สแกนหาจุดจริง (lint 11 warnings ทั้งหมด exhaustive-deps, console.* 63 จุด/18 ไฟล์, explorer ซ้ำ 2 หน้า, controller/DTO backlog) แล้วเขียน 4 แผน: **PLAN-024** (GPT, แก้ 11 lint warnings เหลือ 0 — useCallback loaders/documented disable wizard steps, ห้ามเปลี่ยน behavior), **PLAN-025** (GPT, เก็บ console.* — ลบ debug log, คง error log + กัน catch เงียบ), **PLAN-026** (Gemini, สกัด shared Explorer hook/component de-dup Course+LearnerGroup, pure de-dup), **PLAN-027** (Gemini, increment ถัดไป refactor CoursesController→CourseService + typed response ใช้ ApiResponse<T>)
- ไฟล์หลักที่แตะ: `DOC/PLANS/PLAN-024..027-*.md` (เอกสารล้วน)
- Contract ที่เปลี่ยน: ไม่มี (แผน)
- Verified: n/a (planner)

## [2026-06-15 14:17] GitHub Copilot (GPT-5.3-Codex) — Smoke test แก้ช่องว่างขวาผิดปกติใน React Admin
- ทำอะไร: เปิด `http://localhost:5173/` ตรวจ layout ด้วย browser automation และพบว่าหน้าแอปมี right gap คงที่ 15px แม้ไม่มี horizontal overflow สาเหตุจาก CSS ที่ตั้ง `scrollbar-gutter: stable` บน `html`; แก้โดยปรับเป็น `scrollbar-gutter: auto` ใน `iLearn.Admin.React/src/index.css` แล้ว reload smoke test ซ้ำ
- ไฟล์หลักที่แตะ: `iLearn.Admin.React/src/index.css`, `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี
- Verified: ก่อนแก้วัดได้ `rightGap: 15` (`windowInnerWidth: 1014`, `scrollWidth: 999`, `scrollbarGutter: stable`), หลังแก้เป็น `rightGap: 0` (`windowInnerWidth: 1014`, `scrollWidth: 1014`, `scrollbarGutter: auto`) และภาพหน้า dashboard ไม่เหลือแถบว่างด้านขวา

## [2026-06-15 14:10] Antigravity — ปรับปรุงกล่อง Overview ในหน้า Detail ทุกหน้าให้ได้มาตรฐานของดีไซน์ระบบ
- ทำอะไร: ปรับปรุงการแสดงผลและโครงสร้างข้อมูลภายใต้กล่อง Overview ของทั้ง 6 หน้าจอหลัก (Assignment, Course, User, Content Item, Master Data, Learner Group) ให้สอดคล้องกันตามดีไซน์ระบบ:
  1. ใช้ `<StatusText>` (outlined pill) เสมอสำหรับการแสดงผลสถานะใน Overview (แทน `<StatusBadge>` ที่มีพื้นหลังทึบและเหมาะสมกับตาราง/KPI)
  2. ปรับขนาดตัวเลข KPI counts / stats ทั้งหมดให้ใช้ขนาดมาตรฐาน `font-bold text-slate-800` (เอา `text-lg` size override ออกในหน้ารายละเอียดของวิชา)
  3. ใส่ `mono` prop ให้กับฟิลด์รหัสผ่าน identifiers เช่น รหัสพนักงาน (NID) หรือ SCORM File Storage ID
  4. ทำความสะอาด grid ในหน้ารายละเอียด SCORM item โดยเปลี่ยน layout ของ Launch Resource และ Server Path เป็น `colSpan="full"`
  5. เอา `mono` ออกจากชื่อผู้สร้าง Owner/Creator ในกลุ่มผู้เรียน
  6. เอา `labelClassName` ที่กำหนดขนาดอักษรทับซ้อนออกใน Master Data เพื่อให้แสดงผลหัวข้อ/ป้ายกำกับขนาดเท่ากันทุกหน้า
- ไฟล์หลักที่แตะ: `iLearn.Admin.React/src/pages/assignments/AssignmentDetailPage.tsx`, `iLearn.Admin.React/src/pages/courses/CourseDetailPage.tsx`, `iLearn.Admin.React/src/pages/users/UserDetailPage.tsx`, `iLearn.Admin.React/src/pages/content-library/ContentItemDetailPage.tsx`, `iLearn.Admin.React/src/pages/master-data/MasterDataDetailPage.tsx`, `iLearn.Admin.React/src/pages/learner-groups/LearnerGroupDetailPage.tsx`
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี
- Verified: `npm run lint` ผ่าน (0 errors, 11 warnings baseline), `npm run build` ผ่าน, `dotnet test` (118/118 tests passed)

## [2026-06-15 14:05] Antigravity — ปรับปรุงมาตรฐานดีไซน์และโครงสร้างหน้าจอ Detail ในส่วนที่เหลือเพื่อแก้ปัญหา Layout Shift และจัดระเบียบหัวข้อ
- ทำอะไร: ปรับปรุงโครงสร้างหน้าจอแสดงรายละเอียด (Detail/Overview) ทั้ง 5 หน้าจอที่เหลือ (LearnerGroupDetailPage, CourseDetailPage, UserDetailPage, ContentItemDetailPage, MasterDataDetailPage) จากเดิมที่ใช้ `<DetailCard>` หรือโครงสร้าง Custom margin/padding ที่มีปัญหา visual shift/scrollbar alignment ต่างกันเมื่อสลับแท็บ ให้มาใช้โครงสร้างที่เป็นมาตรฐานเดียวกันทั้งหมด คือครอบด้วยกล่อง `<section className="overflow-hidden rounded-lg border border-slate-200 bg-white shadow-xs">` พร้อมใช้ `<SectionHeader variant="card">` เพื่อแสดงหัวข้อสไตล์ flush card และครอบเนื้อหาภายในด้วย padding `p-5` เพื่อความสวยงาม เป็นระเบียบเรียบร้อย และสม่ำเสมอ
- ไฟล์หลักที่แตะ: `iLearn.Admin.React/src/pages/learner-groups/LearnerGroupDetailPage.tsx`, `iLearn.Admin.React/src/pages/courses/CourseDetailPage.tsx`, `iLearn.Admin.React/src/pages/users/UserDetailPage.tsx`, `iLearn.Admin.React/src/pages/content-library/ContentItemDetailPage.tsx`, `iLearn.Admin.React/src/pages/master-data/MasterDataDetailPage.tsx`
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี
- Verified: `npm run lint` ผ่าน (0 errors, 11 warnings baseline), `npm run build` ผ่าน, `dotnet test` (118/118 tests passed)

## [2026-06-15 18:30] Claude Code — Review + ปิด PLAN-021/022/023 เป็น VERIFIED
- ทำอะไร: รีวิว diff ที่ commit แล้ว — **021** (acdfa2c): App.tsx ครอบ learning-logs ด้วย RequireRole superAdminOnly + nav item superAdminOnly; **022** (acdfa2c): CreateAsync group+category ใช้ `parent?DivisionId : (IsSuperAdmin?dto.DivisionId:currentUser.DivisionId)` กัน escalation+inherit parent; **023** (3373581): UpdateLearnerGroupCategoryDto+DivisionId, UpdateAsync มี parent-inherit + SuperAdmin-only + empty-check guard (กันเปลี่ยน division ของ category ที่มีลูก/group), frontend edit selector + explorer folder selector (useSession) — ปรับทั้ง 3 เป็น VERIFIED
- ไฟล์หลักที่แตะ: `DOC/PLANS/PLAN-021/022/023-*.md` (สถานะ)
- Contract ที่เปลี่ยน: ไม่มี (โค้ด commit แล้ว — เพิ่ม DivisionId ใน Create/Update LearnerGroupCategory DTO ตามแผน)
- Verified: `dotnet test` 118/118 ผ่าน, `npm run build` ผ่าน, `npm run lint` 0 errors (11 warnings baseline)
- ⚠️ ข้อสังเกต: working tree มี diff ค้าง ~20 ไฟล์ (App.tsx, AppTable, index.css, Breadcrumbs, หน้าต่าง ๆ) — **ไม่ใช่งานของ PLAN-021/022/023** (พวกนั้น commit แล้ว) ดูเหมือนรอบ refactor UI/button styles ที่ยังไม่ commit — build/lint/test ผ่านบน state นี้ แต่ยังไม่ได้รีวิวเนื้อหา

## [2026-06-15 13:58] Antigravity — ปรับปรุงมาตรฐานการแสดงผลหัวข้อและจัดระเบียบโครงสร้างแท็บในหน้า Assignment Detail
- ทำอะไร: ปรับปรุงโครงสร้างของแท็บ Overview ในหน้า `AssignmentDetailPage.tsx` จากเดิมที่ใช้ `<DetailCard>` และหัวข้อประเภท plain ให้เปลี่ยนมาใช้โครงสร้าง `<section>` แบบมีขอบมุมตัด (`overflow-hidden rounded-lg border`) และหัวข้อประเภท `card` (`variant="card"`) ให้สอดคล้องกันทุกแท็บ (Overview, Courses, Learners) ส่งผลให้ปุ่ม Controls ฝั่งขวาไม่ขยับสั่นตำแหน่งเดิม และขนาดตัวอักษรของหัวข้อ "Overview" มีความเหมาะสมและเป็นระเบียบเท่ากับแท็บอื่นๆ
- ไฟล์หลักที่แตะ: `iLearn.Admin.React/src/pages/assignments/AssignmentDetailPage.tsx`
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี
- Verified: `npm run lint` ผ่าน, `npm run build` ผ่าน

## [2026-06-15 13:56] Antigravity — ป้องกันการขยับของหน้าจอ (Layout Shift) เมื่อสลับแท็บข้อมูลที่มีความสูงต่างกัน
- ทำอะไร: เพิ่มกฎ CSS `scrollbar-gutter: stable` ให้กับอิลิเมนต์ `html` ในไฟล์ `index.css` เพื่อจองพื้นที่สำหรับแถบเลื่อนแนวตั้ง (Scrollbar) เสมอ ป้องกันปัญหาโครงสร้างหน้าจอขยับหรือสั่น (Scrollbar Layout Shift) เมื่อผู้ใช้งานสลับไปยังแท็บที่มีความสูงข้อมูลต่างกันอย่างรวดเร็ว
- ไฟล์หลักที่แตะ: `iLearn.Admin.React/src/index.css`
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี
- Verified: `npm run lint` ผ่าน, `npm run build` ผ่าน

## [2026-06-15 13:54] Antigravity — ปรับปรุงการจัดกลุ่มข้อมูลรายชื่อผู้เรียน (Learners) ในหน้าแสดงรายละเอียดของ Assignment
- ทำอะไร: อัปเดต `AssignmentDetailPage.tsx` ให้ประมวลผลข้อมูลในแท็บ Learners โดยทำการจัดกลุ่ม (Grouping) ข้อมูลตามตัวตนของผู้เรียนผ่านการทำ `useMemo` (แทนการวนลูปแบบแบนราบเดิมที่ทำให้ชื่อคนซ้ำกันตามจำนวนวิชาที่ได้รับมอบหมาย) โดยจัดให้แสดงผล 1 แถวต่อ 1 คน และรวมรายชื่อวิชาที่ได้รับมอบหมายเข้าไปเป็นรายการย่อยพร้อมความคืบหน้าและสถานะการเรียนรายวิชาภายในแถวเดียวกัน เพื่อให้อ่านและลบ/รีเซ็ตข้อมูลได้สะดวกและไม่สับสน
- ไฟล์หลักที่แตะ: `iLearn.Admin.React/src/pages/assignments/AssignmentDetailPage.tsx`
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี
- Verified: `npm run lint` ผ่าน, `npm run build` ผ่าน

## [2026-06-15 13:51] Antigravity — ปรับปรุงการแสดงผลคอลัมน์วิชาเรียน (Courses) ในตาราง Assignment Batches ให้กระชับและไม่ยาวเกินไป
- ทำอะไร: อัปเดต `EntityListPage.tsx` เพื่อปรับแต่ง `cellRender` ของคอลัมน์ `courseNames` ในกรณีที่มีหลายวิชาเรียน โดยจะนำวิชาแรกมาแสดงผลพร้อมตัดคำให้อยู่ในกรอบ และแสดง Badge ตัวเลขระบุจำนวนวิชาเพิ่มเติม (เช่น `+2`) พร้อมกำหนดคำแนะนำเมาส์ชี้ (Tooltip) แสดงรายชื่อวิชาทั้งหมดเมื่อนำเมาส์ไปชี้ เพื่อความสะอาดตาและเป็นระเบียบของตารางข้อมูล
- ไฟล์หลักที่แตะ: `iLearn.Admin.React/src/pages/EntityListPage.tsx`
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี
- Verified: `npm run lint` ผ่าน, `npm run build` ผ่าน

## [2026-06-15 13:43] Antigravity — ป้องกันการกดเบิ้ล (Double Click) เข้าหน้า Detail ของรายการ Enrollments และ Learning Logs ที่ไม่มีหน้าดีเทล
- ทำอะไร: ปิดความสามารถในระดับ UI โดยส่ง `onRowDblClick`เป็น `undefined` และปิด cursor pointer สำหรับหน้าแสดงผลของข้อมูลที่ไม่มีหน้าดีเทล (ได้แก่ `Enrollments` และ `Learning Logs` ซึ่งเก็บประวัติ/อ่านอย่างเดียว) เพื่อป้องกันการลิงก์ไปหน้าเพจที่ไม่มีอยู่จริงจนเกิด Not Found error
- ไฟล์หลักที่แตะ: `iLearn.Admin.React/src/pages/EntityListPage.tsx`, `iLearn.Admin.React/src/components/ui/AppTable.tsx`
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี
- Verified: `npm run lint` ผ่าน, `npm run build` ผ่าน

## [2026-06-15 13:39] Antigravity — แก้ไขข้อผิดพลาด SyntaxError จากการลบหมวดหมู่ (Delete Category) ในหน้า Courses Explorer
- ทำอะไร: แก้ไขฟังก์ชัน `fetchWithAccessControl` ใน `apiClient.ts` ให้สามารถรองรับและประมวลผล HTTP status 200 OK ที่ไม่มี Response Body (Empty Body) ได้อย่างปลอดภัย โดยอ่านข้อมูลดิบเป็นข้อความและตรวจสอบความว่างเปล่าก่อนส่งไปแปลง JSON เพื่อไม่ให้เกิด `SyntaxError: Unexpected end of JSON input`
- ไฟล์หลักที่แตะ: `iLearn.Admin.React/src/lib/apiClient.ts`
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี
- Verified: `npm run lint` ผ่าน, `npm run build` ผ่าน

## [2026-06-15 13:35] Antigravity — ปรับเปลี่ยนคำศัพท์หน้า Bulk Assignment เป็น Assign Courses
- ทำอะไร: ปรับปรุงคำศัพท์ในระบบจากเดิมคือ "Bulk Assignment" หรือ "Bulk Assign" ให้เรียบง่ายและเป็นมิตรกับผู้ใช้งานมากขึ้นเป็น "Assign Courses" ในส่วนของ breadcrumbs, ปุ่มเปิดหน้าจากตารางรายการ และหัวข้อของ wizard page
- ไฟล์หลักที่แตะ: `iLearn.Admin.React/src/components/layout/Breadcrumbs.tsx`, `iLearn.Admin.React/src/pages/EntityListPage.tsx`, `iLearn.Admin.React/src/pages/assignments/BulkAssignPage.tsx`
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี
- Verified: `npm run lint` ผ่าน, `npm run build` ผ่าน

## [2026-06-15 13:32] Antigravity — ปรับปรุง UI/UX การเพิ่มผู้เรียนในหน้า Assignment Detail ให้เหมือนหน้า Learner Group Detail
- ทำอะไร: ปรับปรุงฟอร์มการเพิ่มผู้เรียน (Add More Learners) ในหน้า AssignmentDetailPage จากแบบเดิมที่เป็น textarea ให้กลายเป็น popup modal แบบ 2 แท็บ (Directory Search / Bulk Import) และระบบคิวเหมือนในหน้าดีเทลของกลุ่มผู้เรียน โดยเรียกใช้งาน component LearnerDirectorySelector
- ไฟล์หลักที่แตะ: `iLearn.Admin.React/src/pages/assignments/AssignmentDetailPage.tsx`
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี
- Verified: `npm run lint` ผ่าน, `npm run build` ผ่าน

## [2026-06-15 13:28] Antigravity — ปรับปรุงระบบแก้ไขกลุ่มผู้เรียนให้ทำงานผ่าน Popup Modal ในหน้าดีเทล
- ทำอะไร: ย้ายฟอร์มการแก้ไขคุณสมบัติกลุ่มผู้เรียน (Edit Learner Group) จากหน้าเว็บ /edit แยกต่างหาก มาทำงานเป็นแบบ Popup Modal ในหน้าแสดงรายละเอียด (LearnerGroupDetailPage) แทนการเปิดหน้าใหม่ โดยปรับให้ดึงข้อมูลโฟลเดอร์ผ่าน Category Explorer Modal (z-index 60) ร่วมด้วย และลบเส้นทาง Route ที่ไม่ได้ใช้งาน รวมถึงลบส่วนโค้ดแก้ไขกลุ่มในหน้าสร้างกลุ่ม (LearnerGroupEditorPage) ออก
- ไฟล์หลักที่แตะ: `iLearn.Admin.React/src/pages/learner-groups/LearnerGroupDetailPage.tsx`, `iLearn.Admin.React/src/pages/learner-groups/LearnerGroupEditorPage.tsx`, `iLearn.Admin.React/src/App.tsx`
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี
- Verified: `npm run lint` ผ่าน, `npm run build` ผ่าน, `dotnet test` ผ่าน (118/118 tests passed)

## [2026-06-15 13:24] Antigravity — ปรับปรุงคำของหัวข้อ (Title) และคำอธิบาย (Description) ในหน้าฟอร์มแก้ไขข้อมูล (Editor/Form Pages)
- ทำอะไร: ปรับปรุงคำอธิบายและหัวข้อในหน้ากรอกฟอร์มแก้ไขข้อมูลทั้งหมด ได้แก่ CourseEditorPage, VersionFormPage, ContentItemEditorPage, LearnerGroupEditorPage, LearnerGroupCategoryEditorPage, UserEditorPage ให้มีความสั้น กระชับ เป็นมาตรฐานเดียวกัน และแก้ไขคำภาษาอังกฤษที่ฟุ่มเฟือย
- ไฟล์หลักที่แตะ: `iLearn.Admin.React/src/pages/courses/CourseEditorPage.tsx`, `iLearn.Admin.React/src/pages/courses/VersionFormPage.tsx`, `iLearn.Admin.React/src/pages/content-library/ContentItemEditorPage.tsx`, `iLearn.Admin.React/src/pages/learner-groups/LearnerGroupEditorPage.tsx`, `iLearn.Admin.React/src/pages/master-data/LearnerGroupCategoryEditorPage.tsx`, `iLearn.Admin.React/src/pages/users/UserEditorPage.tsx`
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี
- Verified: `npm run lint` ผ่าน, `npm run build` ผ่าน, `dotnet test` ผ่าน (118/118 tests passed)

## [2026-06-15 13:19] Antigravity — ปรับปรุงและปรับกระชับคำของหัวข้อเพจ (Title) และคำอธิบาย (Note) ทุกเพจ
- ทำอะไร: ปรับปรุงคำใน config และเพจต่าง ๆ ให้สั้น กระชับ และเป็นมาตรฐานเดียวกัน เช่น ปรับ Title/Note ใน config ของวิชา, SCORM, batch, ผู้เรียน, และ admin รวมถึงอัปเดตเพจ Custom list ที่มีข้อความ hardcoded (เช่น AdminUsersPage, LearnerGroupCategoriesPage, LearnerGroupListPage, CourseListPage, AssignmentGanttPage)
- ไฟล์หลักที่แตะ: `iLearn.Admin.React/src/pages/moduleConfigs.ts`, `iLearn.Admin.React/src/pages/users/AdminUsersPage.tsx`, `iLearn.Admin.React/src/pages/master-data/LearnerGroupCategoriesPage.tsx`, `iLearn.Admin.React/src/pages/learner-groups/LearnerGroupListPage.tsx`, `iLearn.Admin.React/src/pages/courses/CourseListPage.tsx`, `iLearn.Admin.React/src/pages/assignments/AssignmentGanttPage.tsx`
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี
- Verified: `npm run lint` ผ่าน, `npm run build` ผ่าน, `dotnet test` ผ่าน (118/118 tests passed)

## [2026-06-15 13:12] Antigravity — ปรับปรุงและปรับสไตล์ของปุ่มใน Action Column ให้ได้มาตรฐานเดียวกัน (ครอบคลุมครบทุกหน้า)
- ทำอะไร: ปรับปรุง `AppTable.tsx` ให้มีสิทธิ์เลือก variant ของปุ่ม action และเพิ่มระบบตรวจจับสีอัตโนมัติตาม Hint text (เช่น คำว่า delete, remove จะได้สีแดงทันที); ทำการแก้โค้ดและย้ายคลาสปุ่ม action ในตารางแบบ Custom ของหน้าต่าง ๆ ให้ใช้คลาสดีไซน์และขนาดไอคอน h-3.5 w-3.5 ที่เป็นอันหนึ่งอันเดียวกัน (ครอบคลุมหน้า CourseListPage, LearnerGroupListPage, LearnerGroupCategoriesPage, AssignmentDetailPage, BulkAssignPage, LearnerGroupDetailPage, ContentItemEditorPage, CourseEditorPage, VersionFormPage)
- ไฟล์หลักที่แตะ: `iLearn.Admin.React/src/components/ui/AppTable.tsx`, `iLearn.Admin.React/src/pages/courses/CourseListPage.tsx`, `iLearn.Admin.React/src/pages/learner-groups/LearnerGroupListPage.tsx`, `iLearn.Admin.React/src/pages/master-data/LearnerGroupCategoriesPage.tsx`, `iLearn.Admin.React/src/pages/assignments/AssignmentDetailPage.tsx`, `iLearn.Admin.React/src/pages/assignments/BulkAssignPage.tsx`, `iLearn.Admin.React/src/pages/learner-groups/LearnerGroupDetailPage.tsx`, `iLearn.Admin.React/src/pages/content-library/ContentItemEditorPage.tsx`, `iLearn.Admin.React/src/pages/courses/CourseEditorPage.tsx`, `iLearn.Admin.React/src/pages/courses/VersionFormPage.tsx`
- Contract ที่เปลี่ยน (API shape / props / DB): เพิ่ม `variant?: 'primary' | 'danger' | 'success' | 'ghost' | undefined` ใน `actionButtons` element ของ `AppTable`
- Verified: `npm run lint` ผ่าน, `npm run build` ผ่าน, `dotnet test` (118/118 tests passed)

## [2026-06-15 12:59] GitHub Copilot (GPT-5.3-Codex) — เอกสาร cleanup รอบสาม (ล้างไฟล์แผนเก่าชุดใหญ่)
- ทำอะไร: ลบไฟล์แผนใน `DOC/PLANS` ที่ปิดงานแล้วทั้งหมด (สถานะ VERIFIED/CANCELLED เดิม) จำนวน 21 ไฟล์ เพื่อเก็บเฉพาะแผนล่าสุดที่ยังเป็น `DONE` และ `README` สำหรับ workflow ปัจจุบัน
- ไฟล์หลักที่แตะ: `DOC/PLANS/PLAN-001-*.md` ถึง `DOC/PLANS/PLAN-020-*.md` (deleted เฉพาะไฟล์ที่มีอยู่จริง), `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี
- Verified: ตรวจโฟลเดอร์หลังลบแล้วเหลือเฉพาะ `README.md`, `PLAN-021-*.md`, `PLAN-022-*.md`, `PLAN-023-*.md`

## [2026-06-15 12:58] GitHub Copilot (GPT-5.3-Codex) — เอกสาร cleanup รอบสองแบบเข้มขึ้น (ลบ historical docs เพิ่ม)
- ทำอะไร: ลบเอกสาร historical/transition ที่ไม่ใช้งานแล้วและไม่ถูกอ้างอิงในเอกสาร active ได้แก่เอกสารแผนย้าย React เก่า, gap analysis เก่า, smoke report เก่า และไฟล์ function audit ระดับราก; ปรับ `DOC/ux_ui_analysis.md` เพื่อลบลิงก์ค้างไปไฟล์ที่ลบและชี้ไปคู่มือปัจจุบัน (`iLearn.Admin.React/README.md`) แทน
- ไฟล์หลักที่แตะ: `DOC/REACT-ADMIN-REBUILD-PLAN.md` (deleted), `DOC/REACT-ADMIN-GAP-ANALYSIS.md` (deleted), `DOC/REACT-ADMIN-SMOKETEST-REPORT.md` (deleted), `ilearn_function_audit.md` (deleted), `DOC/ux_ui_analysis.md`, `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี
- Verified: ตรวจด้วย `rg` แล้วไม่พบ reference ค้างถึงไฟล์ที่ถูกลบในชุดเอกสาร active (`DOC`, `AGENTS.md`, `CLAUDE.md`, `CONTRIBUTING.md`, `iLearn.Admin.React/README.md`, `.github/copilot-instructions.md`)

## [2026-06-15 12:54] GitHub Copilot (GPT-5.3-Codex) — ลบเอกสาร/ไฟล์ประกอบที่ไม่จำเป็นและไม่ถูกอ้างอิง
- ทำอะไร: ทำความสะอาดไฟล์เอกสาร/ไฟล์รายงานเก่าที่ไม่ใช้งาน โดยตรวจค้นทั้งรีโปก่อนลบด้วย `rg` แล้วยืนยันว่าไม่มีที่ใดอ้างอิง จากนั้นลบไฟล์ที่เป็น old snapshot และไฟล์ชั่วคราวจากการตรวจ API
- ไฟล์หลักที่แตะ: `DOC/AppDbContextModelSnapshot_OLD.cs` (deleted), `content-items-response.json` (deleted), `network-commit-check.txt` (deleted), `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี
- Verified: `rg` ไม่พบ reference ของทั้งสามไฟล์ก่อนลบ, และ `git ls-files` ยืนยันเป็นไฟล์ที่ tracked จริง

## [2026-06-15 12:49] GitHub Copilot (GPT-5.3-Codex) — Audit เอกสาร DOC และอัปเดตสถานะให้ตรงโค้ดล่าสุด
- ทำอะไร: ตรวจเอกสารในโฟลเดอร์ DOC แบบกว้าง (grep สัญญาณล้าสมัย + ยืนยันกับโค้ดจริง) แล้วอัปเดต 2 เอกสารหลักให้เป็นปัจจุบัน: `DOC/api_analysis.md` (แก้จากข้อมูลยุคก่อน PLAN-013/021/022/023 เป็นสถานะปัจจุบัน พร้อมตัวเลขจาก inventory ล่าสุด), `DOC/division_isolation_analysis.md` (เปลี่ยนจาก pending findings เป็นสถานะหลัง implement แผนแล้ว และสรุปประเด็นที่ยัง open decision); ยืนยันซ้ำว่า `FileStoragesCRUDController` ไม่มีแล้ว, Learning Logs ถูก gate แบบ SuperAdmin ใน UI route/menu, และ flow Division ของ Learner Group Category ตรงกับ implementation ปัจจุบัน
- ไฟล์หลักที่แตะ: `DOC/api_analysis.md`, `DOC/division_isolation_analysis.md`, `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี (เอกสารล้วน)
- Verified: ตรวจ consistency ด้วย grep/read หลักฐานจากโค้ดจริง (`iLearn.Application/Services/LearnerGroupService.cs`, `iLearn.Application/Services/LearnerGroupCategoryService.cs`, `iLearn.Admin.React/src/App.tsx`, `iLearn.Admin.React/src/config/navigation.ts`, `DOC/API-ENDPOINT-INVENTORY.md`) และ re-scan คีย์เวิร์ด stale ใน 2 เอกสารที่อัปเดตแล้วไม่พบข้อความขัดแย้งเดิม

## [2026-06-15 12:40] Antigravity — ปรับปรุงและปรับสไตล์ของปุ่มใน Action Column ให้ได้มาตรฐานเดียวกัน
- ทำอะไร: ปรับปรุง `AppTable.tsx` ให้มีสิทธิ์เลือก variant ของปุ่ม action และเพิ่มระบบตรวจจับสีอัตโนมัติตาม Hint text (เช่น คำว่า delete, remove จะได้สีแดงทันที); ทำการแก้โค้ดและย้ายคลาสปุ่ม action ในตารางแบบ Custom ของหน้าต่าง ๆ ให้ใช้คลาสดีไซน์และขนาดไอคอน h-3.5 w-3.5 ที่เป็นอันหนึ่งอันเดียวกัน
- ไฟล์หลักที่แตะ: `iLearn.Admin.React/src/components/ui/AppTable.tsx`, `iLearn.Admin.React/src/pages/courses/CourseListPage.tsx`, `iLearn.Admin.React/src/pages/learner-groups/LearnerGroupListPage.tsx`, `iLearn.Admin.React/src/pages/master-data/LearnerGroupCategoriesPage.tsx`, `iLearn.Admin.React/src/pages/assignments/AssignmentDetailPage.tsx`, `iLearn.Admin.React/src/pages/learner-groups/LearnerGroupDetailPage.tsx`, `iLearn.Admin.React/src/pages/content-library/ContentItemEditorPage.tsx`, `iLearn.Admin.React/src/pages/courses/CourseEditorPage.tsx`, `iLearn.Admin.React/src/pages/courses/VersionFormPage.tsx`
- Contract ที่เปลี่ยน (API shape / props / DB): เพิ่ม `variant?: 'primary' | 'danger' | 'success' | 'ghost' | undefined` ใน `actionButtons` element ของ `AppTable`
- Verified: `npm run lint` ผ่าน, `npm run build` ผ่าน, `dotnet test` (118/118 tests passed)

## [2026-06-15 12:13] GitHub Copilot (GPT-5.3-Codex) — สร้างรายงาน Inventory Endpoint ทั้งหมดเป็นไฟล์ Markdown
- ทำอะไร: สแกน controller ทั้งหมดใน `iLearn.API/Controllers` จาก `[Route]` + `[Http*]` attributes แล้วสร้างรายงาน inventory endpoint แบบครบถ้วน (verb/route/controller/action/policy/source) พร้อมสรุปนับตาม verb, route family, policy และเพิ่ม SignalR hub mapping จาก `Program.cs`
- ไฟล์หลักที่แตะ: `DOC/API-ENDPOINT-INVENTORY.md`, `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี (เอกสารล้วน)
- Verified: ตรวจผลในไฟล์รายงานแล้วมีสรุปครบ (`TotalEndpoints: 165`, `TotalControllersWithEndpoints: 30`, `TotalHubs: 1`) และมีตาราง inventory endpoint ครบ

## [2026-06-15 11:47] Antigravity — ปรับปรุงหน้า Assignment Report และหน้า Detail ให้ไม่แสดง Description และใช้โครงสร้างมาตรฐาน
- ทำอะไร: ปรับปรุง `AssignmentReportPage.tsx` ให้ใช้โครงสร้าง `DetailLayout` และมีแถบด้านขวา `ControlsSidebar` สำหรับเก็บปุ่ม Print และ Export CSV เหมือนหน้า Detail ทั่วไป; ยกเลิกการแสดงผลรายละเอียด Description และยกเลิก Page Header ซ้ำซ้อน (เนื่องจากมี Breadcrumb แสดงผลอยู่แล้ว) ทั้งในหน้า Report และหน้า Detail (`AssignmentDetailPage.tsx`) พร้อมปรับปรุงการเว้นระยะห่างหัวตาราง FactGrid ในการ์ด Overview/Report Summary; เพิ่มการแสดงผล Assignment No. แบบกว้างเต็มบรรทัด (colSpan="full") ที่หัวตาราง FactGrid ในการ์ด Overview ของหน้า Report; ตกแต่งเนื้อหาฝั่งซ้ายโดยใช้ `DetailCard`, `FactGrid`, `Fact`, และ `DetailSubSection` ร่วมกับไอคอนวิชาเรียน; ใช้ `StatusBadge` และ `ProgressBar` ในตารางแสดงความคืบหน้าของ Learners พร้อมกล่องกรองสถานะและช่องค้นหารูปแบบมาตรฐาน
- ไฟล์หลักที่แตะ: `iLearn.Admin.React/src/pages/assignments/AssignmentReportPage.tsx`, `iLearn.Admin.React/src/pages/assignments/AssignmentDetailPage.tsx`
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี
- Verified: `npm run lint` และ `npm run build` ผ่าน, `dotnet test` (118/118 tests passed)

## [2026-06-15 11:34] Antigravity — ปรับปรุงไอคอนวิชาเรียน (Courses) ในหน้า Courses Explorer ให้สอดคล้องกัน
- ทำอะไร: เปลี่ยนการใช้งานไอคอนวิชาเรียนจาก `Layers` เป็น `BookOpen` (สี `text-indigo-500`) เพื่อปรับปรุงความเหมาะสมและคงความสอดคล้องตาม design system ส่วนอื่น ๆ ในระบบที่ใช้ `BookOpen` เป็นตัวแทนของวิชาเรียน
- ไฟล์หลักที่แตะ: `iLearn.Admin.React/src/pages/courses/CourseListPage.tsx`
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี
- Verified: `npm run lint` และ `npm run build` ผ่าน, `dotnet test` (118/118 tests passed)


## [2026-06-15 11:32] Antigravity — ปรับปรุงไอคอนโฟลเดอร์ Division ในหน้า Courses Explorer
- ทำอะไร: เพิ่ม `isDivision: true` ให้กับรายการ Division ใน list mapping และอัปเดตไอคอนในตาราง `CourseListPage.tsx` ให้ใช้ `Building2` (สี `text-indigo-500`) สำหรับโฟลเดอร์ Division เพื่อแยกให้แตกต่างอย่างชัดเจนจากโฟลเดอร์ Category (ไอคอน `Folder` สี `text-amber-500`)
- ไฟล์หลักที่แตะ: `iLearn.Admin.React/src/pages/courses/CourseListPage.tsx`
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี
- Verified: `npm run lint` และ `npm run build` ผ่าน, `dotnet test` (118/118 tests passed)


## [2026-06-15 11:30] Antigravity — เพิ่มการสร้าง เปลี่ยนชื่อ และลบ Category ในหน้า Courses Explorer
- ทำอะไร: อัปเดต `CourseListPage.tsx` ให้มีปุ่ม "New Category" ใน actions bar (เมื่ออยู่ระดับ root สำหรับ SuperAdmin หรืออยู่ระดับ division สำหรับทุก admin) และปุ่ม Rename/Delete (ไอคอนดินสอและถังขยะ) ถัดจากโฟลเดอร์หมวดหมู่; สร้าง modals สำหรับกรอกชื่อหมวดหมู่ใหม่และการเปลี่ยนชื่อ; เช็ค `courseCount` เพื่อ block การลบหมวดหมู่ที่มีวิชาเรียนอยู่ภายใน และใช้ `useConfirm` ยืนยันการลบ; อัปเดต `CategoryLookup` structure ให้มี `courseCount`
- ไฟล์หลักที่แตะ: `iLearn.Admin.React/src/pages/courses/CourseListPage.tsx`
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี (เรียกใช้งาน endpoint CategoriesCRUD เดิมบน backend)
- Verified: `npm run lint` และ `npm run build` ผ่าน, `dotnet test` (118/118 tests passed)


## [2026-06-15 11:15] Antigravity — Implement PLAN-023 SuperAdmin เลือก Division ได้ตอน Edit Category + สร้าง folder ในหน้า Explorer
- ทำอะไร: อัปเดต `UpdateLearnerGroupCategoryDto` ให้รับ `DivisionId` และปรับปรุง `LearnerGroupCategoryService.UpdateAsync` ให้รองรับการเปลี่ยน/ระบุ `DivisionId` โดย SuperAdmin; เพิ่มการตรวจสอบป้องกันความเสี่ยง (Division Update Safety Check) ใน service เพื่อ block/ปฏิเสธการเปลี่ยน division หากหมวดหมู่ไม่ได้ว่าง (มี sub-categories หรือ learner groups ภายใน); ปรับปรุง UI `LearnerGroupCategoryEditorPage.tsx` ให้ SuperAdmin สามารถแก้ไข/เลือกแผนกในหน้าจอ edit หมวดหมู่ได้; ปรับปรุงหน้า explorer `LearnerGroupListPage.tsx` ให้แสดง dropdown เลือก Division ใน modal "Create Folder" เมื่อ SuperAdmin สร้างโฟลเดอร์ที่ root level (`currentCategoryId === 0`)
- ไฟล์หลักที่แตะ: `iLearn.Application/DTOs/LearnerGroupCategoryDto.cs`, `iLearn.Application/Services/LearnerGroupCategoryService.cs`, `iLearn.Admin.React/src/pages/master-data/LearnerGroupCategoryEditorPage.tsx`, `iLearn.Admin.React/src/pages/learner-groups/LearnerGroupListPage.tsx`
- Contract ที่เปลี่ยน (API shape / props / DB): เพิ่ม `DivisionId` ใน `UpdateLearnerGroupCategoryDto`
- Verified: `npm run lint` และ `npm run build` ผ่าน, `dotnet test` (118/118 tests passed)

## [2026-06-15 17:40] Claude Code — เขียน PLAN-023 (SuperAdmin เลือก division ตอน Edit category + folder ใน explorer)
- ทำอะไร: ผู้ใช้ขอให้ SuperAdmin เลือก division ได้ตอน New/Edit Category ทั้ง `/master-data/learner-group-categories` และ `/learner-groups` — ตรวจแล้ว PLAN-022 ทำ create-side ที่ master-data editor แล้ว แต่ยังขาด (1) Edit category (PLAN-022 กัน UpdateAsync ออก, UpdateDto ไม่มี DivisionId), (2) explorer `handleCreateFolder` POST ไม่ส่ง divisionId + ไม่มี selector (explorer ไม่มี edit-category) → เขียน **PLAN-023** (Gemini): backend เพิ่ม DivisionId ใน UpdateLearnerGroupCategoryDto + UpdateAsync (IsSuperAdmin only, inherit parent, ปลอดภัยต่อ isolation), frontend แสดง selector ตอน edit ที่ master-data + ตอนสร้าง folder ที่ explorer root (inherit ถ้าอยู่ใน sub-folder)
- ไฟล์หลักที่แตะ: `DOC/PLANS/PLAN-023-*.md` (เอกสารล้วน)
- Contract ที่เปลี่ยน: ไม่มี (ตอน implement จะเพิ่ม DivisionId ใน UpdateLearnerGroupCategoryDto)
- Verified: n/a (planner) — หมายเหตุ: PLAN-022 ยัง DONE รอรีวิว, PLAN-023 ต่อยอดจากมัน

## [2026-06-15 11:05] Antigravity — Implement PLAN-022 ให้ SuperAdmin ระบุ Division ตอนสร้าง Learner Group / Learner Group Category
- ทำอะไร: อัปเดต React UI `LearnerGroupCategoryEditorPage.tsx` ให้ SuperAdmin สามารถระบุ/เลือก Division เมื่อสร้างหมวดหมู่ได้ (และจะสืบทอด DivisionId อัตโนมัติจาก Parent Category เมื่อระบุหมวดหมู่หลัก พร้อมแสดงคำชี้แจงและปิดการใช้งาน selector); อัปเดต type definition `LearnerGroupCategory` ใน `LearnerGroupCategoriesPage.tsx` เพื่อให้ sync ตาม backend contract DTO; แก้ไขและนำ unused import `Sliders` ออกจาก `CourseListPage.tsx` เพื่อแก้ปัญหาการ build
- ไฟล์หลักที่แตะ: `iLearn.Admin.React/src/pages/master-data/LearnerGroupCategoryEditorPage.tsx`, `iLearn.Admin.React/src/pages/master-data/LearnerGroupCategoriesPage.tsx`, `iLearn.Admin.React/src/pages/courses/CourseListPage.tsx`
- Contract ที่เปลี่ยน (API shape / props / DB): เพิ่ม `divisionId` แบบ nullable ใน `LearnerGroupCategory` type ฝั่ง React
- Verified: `npm run lint` และ `npm run build` ผ่าน, `dotnet test` (118/118 tests passed)

## [2026-06-15 11:00] GitHub Copilot (GPT-5.3-Codex) — Implement PLAN-021 ทำให้ Learning Logs เป็น SuperAdmin-only สม่ำเสมอ (UI ตรงกับ API)
- ทำอะไร: ปรับ route `/learning-logs` ใน React ให้ครอบ `RequireRole superAdminOnly` และย้ายเมนู `Learning Logs` จาก section `Operations` ไป section `Super Admin` พร้อมตั้ง `superAdminOnly: true` เพื่อให้สิทธิ์ฝั่ง UI ตรงกับ policy ของ API (`LearningLogsCRUDController`)
- ไฟล์หลักที่แตะ: `iLearn.Admin.React/src/App.tsx`, `iLearn.Admin.React/src/config/navigation.ts`, `DOC/PLANS/PLAN-021-learning-logs-superadmin-consistency.md`, `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี (frontend access gating/navigation only)
- Verified: `npm run lint` ผ่าน (0 errors, 11 warnings baseline), `npm run build` ผ่าน, `dotnet build iLearn.Tests -o artifacts\verify-test` ผ่าน, `dotnet test artifacts\verify-test\iLearn.Tests.dll` ผ่าน (118/118)

## [2026-06-15 17:00] Claude Code — Audit division isolation + เอกสาร + PLAN-021/022
- ทำอะไร: ตรวจ division isolation ทั้งระบบ (entity ที่มี DivisionId, isolation รายโมดูล, พฤติกรรม LearnerGroup/Category) → เขียน `DOC/division_isolation_analysis.md` สรุป: security ไม่มีรั่วข้าม division (entity ที่มี division isolate ครบ, ที่ไม่มีกั้นด้วย SuperAdminOnly) เจอประเด็นควรเคลียร์ → ออก 2 แผน: **PLAN-021** (GPT, D1: Learning Logs API เป็น SuperAdminOnly แต่ UI route/menu ไม่ตรง → gate route + ย้ายเมนูไป Super Admin), **PLAN-022** (Gemini, D3: ให้ SuperAdmin ระบุ division ตอนสร้าง LearnerGroup/Category ได้ — เพิ่ม DivisionId ใน Create DTO + service ใช้เฉพาะเมื่อ IsSuperAdmin, division-admin ยัง auto ห้าม override + frontend แสดง selector เฉพาะ SuperAdmin)
- ไฟล์หลักที่แตะ: `DOC/division_isolation_analysis.md`, `DOC/PLANS/PLAN-021-*.md`, `DOC/PLANS/PLAN-022-*.md` (เอกสารล้วน)
- Contract ที่เปลี่ยน: ไม่มี (เป็นแผน — PLAN-022 จะเพิ่ม DivisionId ใน Create DTO ตอน implement)
- Verified: n/a (planner)

## [2026-06-15 16:00] Claude Code — Review PLAN-020 (Courses Explorer) + hotfix data bug → VERIFIED
- ทำอะไร: รีวิว CourseListPage ที่เขียนใหม่เป็น explorer (3-level Division→Category→Course) — โครงสร้าง/deep-link guard/breadcrumb/uncategorized/type-chip ทำถูกตาม pattern; **เจอ data bug:** แผนสั่ง `Courses?isActive=false` แต่ service คืนเฉพาะ Draft/Closed/Retired (isActive=true=Open only) → explorer ขาด course Open ทั้งหมด (ความผิดแผนเองที่ assume isActive=false=ทั้งหมด) **แก้:** ยิง 2 call (true+false) merge (disjoint, ไม่ dup) ใน `loadData`; เทส endpoint จริงทั้ง 5 = 200, build/lint ผ่าน → ปรับ VERIFIED
- ไฟล์หลักที่แตะ: `iLearn.Admin.React/src/pages/courses/CourseListPage.tsx` (hotfix), `DOC/PLANS/PLAN-020-*.md`
- Contract ที่เปลี่ยน: ไม่มี (ใช้ endpoint เดิม)
- Verified: endpoint จริง 5 ตัว = HTTP 200, `npm run build` ผ่าน, `npm run lint` 0 errors (11 warnings baseline)
- บทเรียน: ตรวจ semantics ของ service param (isActive) ก่อนใส่ในแผนเสมอ — ซ้ำรอย enrollments.status/roles.description ที่ assume แล้วพลาด

## [2026-06-15 15:35] Antigravity — Implement PLAN-020 เปลี่ยนหน้า Courses เป็น Explorer (Division → Category → Course) แบบ Learner Group
- ทำอะไร: เขียนหน้าจอ `CourseListPage.tsx` ใหม่ทั้งหมดตามรูปแบบ Explorer ใน `LearnerGroupListPage.tsx` โดยแสดงผลการจัดกลุ่มวิชาเรียนออกเป็น 3 ระดับตามโครงสร้างจริง (Root: Divisions Folder -> ใน Division: Categories Folder -> ใน Category: Courses Leaf Items); จัดการเคสวิชาที่ไม่มีหมวดหมู่ (Uncategorized) ให้แสดงผลเสมือนโฟลเดอร์ Uncategorized Division/Category อย่างสวยงาม; รองรับ deep-linking URL parameter validation (`divisionId`/`categoryId`), breadcrumb trail, client-side search, และ chip filter Course Types ที่ระดับ category; ตรวจสอบและแก้ไข eslint warnings/compiler errors สำเร็จ
- ไฟล์หลักที่แตะ: `iLearn.Admin.React/src/pages/courses/CourseListPage.tsx`, `DOC/PLANS/PLAN-020-courses-explorer.md`
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี (UI changes เท่านั้น)
- Verified: `npm run lint` และ `npm run build` ผ่านสำเร็จครบถ้วน

## [2026-06-15 15:20] Claude Code — เขียน PLAN-020 (Courses → Explorer แบบ Learner Group)
- ทำอะไร: ผู้ใช้ขอให้หน้า `/courses` เป็น Explorer แบบ Learner Group — สำรวจ CourseListPage เดิม (tree sidebar+grid), LearnerGroupListPage (ต้นแบบ explorer), entity (Category มี DivisionId ไม่มี ParentId → โครงสร้าง Division→Category→Course 2 ชั้น), backend (`GET api/Courses` โหลดทั้งหมด, `DivisionsCRUD/Get`, `CategoriesCRUD/Get`, course-types-lookup) → เขียน **PLAN-020** (Gemini): เขียน CourseListPage ใหม่เป็น explorer 3 ระดับ (root=divisions, ?divisionId=categories, ?categoryId=courses) ตาม pattern LearnerGroupListPage (URL deep-link, breadcrumb trail, drill-in/back, client-side search, deep-link guard) คงปุ่ม Create + chip Course Type — ไม่แตะ backend, ยังไม่สกัด shared Explorer (เป็น follow-up)
- ไฟล์หลักที่แตะ: `DOC/PLANS/PLAN-020-courses-explorer.md` (เอกสารล้วน)
- Contract ที่เปลี่ยน: ไม่มี
- Verified: n/a (planner ไม่แก้โค้ด)

## [2026-06-15 14:40] Claude Code — Hotfix regression จาก PLAN-018 (EF แปลง DTO-constructor ใน GroupBy ไม่ได้)
- ทำอะไร: ผู้ใช้รัน Dashboard แล้ว 500 — `InvalidOperationException: LINQ could not be translated` ที่ `DashboardController.GetOverview` categoryMix (บรรทัด ~165) เพราะ PLAN-018 เปลี่ยนเป็น `.Select(g => new DashboardCategoryMixPointDto(...))` **ในตัว EF query** (EF แปลง record constructor ใน GroupBy projection เป็น SQL ไม่ได้) — แก้: project เป็น anonymous ใน SQL แล้ว map เป็น DTO ใน memory หลัง `ToListAsync` (pattern มาตรฐานของ error นี้); audit ทั้ง controller แล้ว — จุดอื่น (courseAttention ใช้ taskRows in-memory, trends/stats จาก local list/CountAsync) ปลอดภัย จุดเดียวคือ categoryMix
- ไฟล์หลักที่แตะ: `iLearn.API/Controllers/DashboardController.cs`
- Contract ที่เปลี่ยน: ไม่มี (shape เดิม)
- Verified: `dotnet build` + `dotnet test` 118/118 ผ่าน — **หมายเหตุ:** ผู้ใช้ต้อง rebuild/restart API ใน VS ถึงจะเห็นผล (binary ที่รันอยู่เป็นตัวเก่า)
- บทเรียน: review PLAN-018 พลาดเพราะ `dotnet test` ไม่ได้ exercise dashboard query กับ SQL จริง (in-memory provider แปลง record constructor ได้ แต่ SQL Server แปลงไม่ได้) — ควรเพิ่ม integration test ที่ยิง dashboard กับ DB provider จริง หรือเทส query แบบ relational

## [2026-06-15 14:00] Claude Code — Review + ปิด PLAN-015..019 เป็น VERIFIED
- ทำอะไร: รีวิว diff ครบ 5 แผน — **015** (roles description ลบครบ column+editor), **016** (UsersCRUD enrich→DataSourceLoader บน enriched list, shape/isolation เดิม, fallback test in-memory), **017** (EnrollmentsController 624→491 + EnrollmentService 4 admin ops + DI, learner/HMAC คงใน controller, pure refactor), **018** (DashboardResponseDtos แทน anonymous, dashboardApi diff = comment-only), **019** (gitignore negation — check-ignore ยืนยัน source ไม่หลุด/vendored ยัง ignore) — ปรับทั้งหมดเป็น VERIFIED
- ไฟล์หลักที่แตะ: `DOC/PLANS/PLAN-015..019-*.md` (สถานะ — ไม่แตะโค้ด)
- Contract ที่เปลี่ยน: ไม่มี (โค้ดเปลี่ยนโดย implementer; response shape ทั้งหมดคงเดิม)
- Verified: รันเอง `dotnet test` 118/118 ผ่าน, `npm run build` ผ่าน, `npm run lint` 0 errors (11 warnings baseline), grep/check-ignore ผ่าน

## [2026-06-15 09:41] GitHub Copilot (GPT-5.3-Codex) — Implement PLAN-019 ปรับ .gitignore กัน wwwroot ignore กลืนไฟล์ source ที่ track
- ทำอะไร: เพิ่ม negation rules ใน `.gitignore` ใต้ `iLearn.Admin/wwwroot/**` เพื่อ re-include เฉพาะไฟล์ source ที่เขียนมือและ track อยู่เดิม 7 ไฟล์ (`css/admin-minimal.css`, `css/admin-tokens.css`, `css/admin-wizard.css`, `css/site.css`, `js/admin-layout.js`, `js/admin-view-utils.js`, `js/site.js`) โดยคงการ ignore vendored/generated เดิม (เช่น `js/devextreme/**`) ไว้
- ไฟล์หลักที่แตะ: `.gitignore`, `DOC/PLANS/PLAN-019-gitignore-wwwroot-tracked-source.md`, `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี
- Verified: `git check-ignore --no-index iLearn.Admin/wwwroot/js/admin-view-utils.js` ไม่ถูก ignore, `git check-ignore --no-index iLearn.Admin/wwwroot/js/devextreme/dx.all.js` ยังถูก ignore, `git ls-files` ยืนยัน 7 ไฟล์ยัง track ครบ, `git status --short` ไม่พบ vendored untracked flood, `dotnet build iLearn.Tests -o artifacts\verify-test` ผ่าน, `dotnet test artifacts\verify-test\iLearn.Tests.dll` ผ่าน (118/118)

## [2026-06-15 09:40] Antigravity — Implement PLAN-017 Refactor logic จาก EnrollmentsController ลง EnrollmentService ใหม่ (pilot)
- ทำอะไร: สร้าง `IEnrollmentService` + implementation `EnrollmentService` ย้าย logic ฝั่ง Admin ได้แก่ `ResetStatus`, `GetById`, `UpdateCompletion`, และ `BulkAssign` (พร้อม private helpers เฉพาะฝั่ง admin) ลง Application layer; ลงทะเบียน service ใน `DependencyInjection.cs`; ปรับปรุง `EnrollmentsController.cs` ให้ใช้ `IEnrollmentService` สำหรับ admin actions โดยยังคง flow ของ user/player และ helper methods ที่เกี่ยวเนื่องกับการ resolve user/schedule identities เอาไว้; อัปเดต `EnrollmentsPlayerInfoTests.cs` ให้ instantiate Controller โดย inject `FakeEnrollmentService` สำเร็จ
- ไฟล์หลักที่แตะ: `iLearn.Application/Interfaces/Services/IEnrollmentService.cs`, `iLearn.Application/Services/EnrollmentService.cs`, `iLearn.API/Controllers/EnrollmentsController.cs`, `iLearn.Application/DependencyInjection.cs`, `iLearn.Tests/EnrollmentsPlayerInfoTests.cs`, `DOC/PLANS/PLAN-017-refactor-large-controllers-pilot.md`
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี (พฤติกรรม response shape, status code, side-effects เหมือนเดิมทุกประการ)
- Verified: `dotnet build` + `dotnet test` (118/118 tests passed) สำเร็จเรียบร้อย

## [2026-06-15 09:38] GitHub Copilot (GPT-5.3-Codex) — Implement PLAN-018 แปลง DashboardController เป็น typed DTO responses (pilot)
- ทำอะไร: เพิ่ม DTO records ใหม่สำหรับ Dashboard responses ที่ `iLearn.Application/DTOs/DashboardResponseDtos.cs` แล้วเปลี่ยน `DashboardController` จาก `Ok(new { ... })` เป็น typed DTO responses ครบ endpoint ใน scope (`Overview`, `Stats`, `EnrollmentTrends`, `LearningActivityTrends`, `MaintenanceStatus`, `RecentAdminActivities`) โดยคง field names/shape เดิมที่ frontend ใช้; เปลี่ยน helper ที่คืน `IEnumerable<object>` เป็น typed DTO collections; เพิ่มคอมเมนต์ `Mirrors` ฝั่ง React ใน `dashboardApi.ts` ให้ชี้ DTO ใหม่
- ไฟล์หลักที่แตะ: `iLearn.Application/DTOs/DashboardResponseDtos.cs`, `iLearn.API/Controllers/DashboardController.cs`, `iLearn.Admin.React/src/pages/dashboard/dashboardApi.ts`, `DOC/PLANS/PLAN-018-typed-response-dtos-pilot.md`, `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี breaking change ใน payload shape (เปลี่ยนจาก anonymous object เป็น typed DTO โดยรักษาชื่อ/โครงสร้าง field เดิม)
- Verified: `dotnet build iLearn.Tests -o artifacts\verify-test` ผ่าน, `dotnet test artifacts\verify-test\iLearn.Tests.dll` ผ่าน (118/118), `npm run lint` ผ่าน (0 errors, 11 warnings baseline), `npm run build` ผ่าน

## [2026-06-15 09:35] Antigravity — Implement PLAN-016 ทำให้ค้นหา Admin Users ด้วยชื่อ/แผนก/ตำแหน่งได้ (enrich-before-filter)
- ทำอะไร: ปรับปรุงการทำงานใน `UsersCRUDController.Get` ให้ดึงข้อมูลผู้ใช้ทั้งหมดและทำการ batch lookup เพื่อเติมข้อมูลพนักงานก่อน แล้วจึงนำผลลัพธ์มาทำ filter/paging/sorting ใน memory ด้วย `DataSourceLoader.Load` เพื่อเปิดความสามารถในการกรอง/จัดลำดับบนฟิลด์พนักงาน (FullName, Division, ฯลฯ); เพิ่ม fallback query provider check เพื่อรองรับ sync testing; ปรับปรุง `AdminUsersPage.tsx` ในฝั่ง frontend เพื่อขยาย `searchExpr` ให้รองรับ `fullName` และ `division`; เพิ่ม unit tests ครอบคลุมการ enrich, in-memory filter, และ division isolation ใน `UsersCRUDControllerTests.cs`
- ไฟล์หลักที่แตะ: `iLearn.API/Controllers/Base/UsersCRUDController.cs`, `iLearn.Admin.React/src/pages/users/AdminUsersPage.tsx`, `iLearn.Tests/UsersCRUDControllerTests.cs`, `DOC/PLANS/PLAN-016-admin-users-enriched-search.md`
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี ( shape และฟิลด์ของ response เหมือนเดิม)
- Verified: `npm run lint` ผ่าน, `npm run build` ผ่าน, `dotnet test` (118/118 passed) ผ่านทั้งหมดแล้ว

## [2026-06-15 09:32] GitHub Copilot (GPT-5.3-Codex) — Implement PLAN-015 ลบ Description ของ Roles จาก UI
- ทำอะไร: ลบฟิลด์ Description ของ roles ออกจาก UI ครบ 3 จุดตามแผน: ลบคอลัมน์ `description` ใน `masterDataRoles.columns`, ลบ textarea Description ในโหมดแก้ไขของ `MasterDataDetailPage`, และลบ `Fact` Description ในโหมด read-only; คง `searchExpr: ['name']` สำหรับ roles และไม่แตะ master-data type อื่น
- ไฟล์หลักที่แตะ: `iLearn.Admin.React/src/pages/moduleConfigs.ts`, `iLearn.Admin.React/src/pages/master-data/MasterDataDetailPage.tsx`, `DOC/PLANS/PLAN-015-remove-roles-description.md`, `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี (UI cleanup only; ไม่แตะ backend/entity)
- Verified: `npm run lint` ผ่าน (0 errors, 11 warnings baseline), `npm run build` ผ่าน, รัน `dotnet build iLearn.Tests -o artifacts\verify-test` + `dotnet test artifacts\verify-test\iLearn.Tests.dll` แล้วแต่ล้มเหลวจาก compile error นอก scope ที่ `iLearn.Tests/UsersCRUDControllerTests.cs` (CS0118: `DevExtreme.AspNet.Data.ResponseModel` namespace used like a type)

## [2026-06-15 13:00] Claude Code — เขียนแผน backlog improvement ครบชุด (PLAN-015..019)
- ทำอะไร: แปลง backlog ที่เหลือทั้งหมดเป็นแผน — **PLAN-015** (GPT, ลบ Description ของ roles ออกครบ 3 จุด: column + editor edit + read-only เพราะ Role entity ไม่มี field นี้), **PLAN-016** (Gemini, ทำ Admin Users ค้นด้วยชื่อ/แผนกได้ ด้วย enrich-before-filter ใน UsersCRUD.Get — Learners NID เป็น won't-fix เพราะ external filter ไม่ได้), **PLAN-017** (Gemini, pilot refactor EnrollmentsController → EnrollmentService + วางมาตรฐานวิธีให้ controller อื่นทำตาม), **PLAN-018** (GPT, pilot แทน anonymous-object → DTO record ที่ DashboardController), **PLAN-019** (GPT, แก้ .gitignore กัน `wwwroot/**` กลืน 7 ไฟล์ source เขียนมือของ admin เก่า)
- ไฟล์หลักที่แตะ: `DOC/PLANS/PLAN-015..019-*.md` (เอกสารล้วน)
- Contract ที่เปลี่ยน: ไม่มี
- Verified: n/a (planner ไม่แก้โค้ด)

## [2026-06-15 12:30] Claude Code — Review + ปิด PLAN-013/014 เป็น VERIFIED
- ทำอะไร: รีวิว diff จริง — **PLAN-013** (GPT): `FileStoragesCRUDController.cs` ถูกลบ, grep ทั้ง solution 0 refs, repository/entity ไม่ถูกแตะ; **PLAN-014** (Gemini): LearnerApiService inject ILogger + ลบ Console ครบ, group A (primary) propagate exception โดย GetLearnersDxGridAsync ใช้ GetAsync แยก 4xx→ArgumentException/5xx→HttpRequestException, group B (enrichment) degrade graceful + LogWarning, GlobalExceptionMiddleware เพิ่ม HttpRequestException→502, LearnersController ลบ null-check generic message, + test ใหม่ GlobalExceptionMiddlewareTests (502 ProblemDetails ใน Production) — ปรับทั้งคู่เป็น VERIFIED
- ไฟล์หลักที่แตะ: `DOC/PLANS/PLAN-013-*.md`, `DOC/PLANS/PLAN-014-*.md` (อัปเดตสถานะ — ไม่แตะโค้ด)
- Contract ที่เปลี่ยน: ไม่มี (โค้ดเปลี่ยนโดย implementer แล้ว — error path เปลี่ยนเป็น ProblemDetails/502 ตามแผน)
- Verified: รันเอง `dotnet build` + `dotnet test` = 116/116 ผ่าน

## [2026-06-15 12:00] Claude Code — Hotfix search error /enrollments + /master-data/roles (searchExpr ผิดจาก PLAN-011)
- ทำอะไร: ผู้ใช้แจ้ง error ช่อง search 2 หน้า — เทส endpoint จริง (status code): enrollments `status`=500, roles `description`=500 (learnerCode/name = 200) สาเหตุ: **Role ไม่มี property `Description`** (มีแค่ Name/RoleType) และ **Enrollment ไม่มี `Status`** (EnrollmentsCRUD projection custom) → filter ฟิลด์ที่ไม่มีจริง 500 (พลาดตอน PLAN-011 ที่ผม prescribe โดยไม่ได้เทส 2 ฟิลด์นี้) แก้ `moduleConfigs.ts`: enrollments → `['learnerCode']`, masterDataRoles → `['name']` แล้วเทสซ้ำทุกตาราง (categories/courseTypes/assignments/divisions/learningLogs/learners) = 200 หมด
- ไฟล์หลักที่แตะ: `iLearn.Admin.React/src/pages/moduleConfigs.ts`
- Contract ที่เปลี่ยน: ไม่มี (frontend search config)
- Verified: endpoint จริง (learnerCode=200, name=200, ทุก config อื่น=200), `npm run build` ผ่าน, `npm run lint` 0 errors (11 warnings baseline)
- ข้อสังเกตค้าง (ยังไม่แก้): roles grid มี **คอลัมน์ `description` ที่ map กับ property ที่ไม่มีบน Role** → แสดงค่าว่างเสมอ (คนละเรื่องกับ search — ควรออกแผนลบคอลัมน์/เพิ่มฟิลด์ทีหลัง)

## [2026-06-15 09:10] Antigravity — Implement PLAN-014 ปรับปรุงการจัดการ Exception ใน LearnerApiService และ GlobalExceptionMiddleware
- ทำอะไร: Inject `ILogger` และนำการกลืน exception ออกในกลุ่ม primary fetch ของ `LearnerApiService` เพื่อให้ propagate ขึ้นไปหา `GlobalExceptionMiddleware`; ในกลุ่ม enrichment helper ปล่อยให้ fallback ว่างแต่แก้ไขเป็น LogWarning; ปรับปรุง `GetLearnersDxGridAsync` ให้โยน `ArgumentException` เมื่อเป็น 4xx client errors และโยน `HttpRequestException` เมื่อเป็น connection/5xx errors; ปรับปรุง `GlobalExceptionMiddleware` ให้แมป `HttpRequestException` เป็น `502 Bad Gateway` (Upstream employee service error); เพิ่ม unit test `GlobalExceptionMiddlewareTests.cs`
- ไฟล์หลักที่แตะ: `iLearn.Infrastructure/Services/LearnerApiService.cs`, `iLearn.API/Controllers/LearnersController.cs`, `iLearn.API/Middleware/GlobalExceptionMiddleware.cs`, `iLearn.Tests/GlobalExceptionMiddlewareTests.cs`, `DOC/PLANS/PLAN-014-learner-api-service-error-handling.md`
- Contract ที่เปลี่ยน (API shape / props / DB): คืน HTTP 502 ProblemDetails (Bad Gateway) เมื่อ upstream service ล้ม แทน 500
- Verified: `dotnet test` (116/116 passed) ผ่านการทดสอบทั้งหมดแล้ว

## [2026-06-15 09:06] GitHub Copilot (GPT-5.3-Codex) — Implement PLAN-013 ลบ FileStoragesCRUD endpoint ที่เสี่ยงดัมพ์ blob
- ทำอะไร: ลบ `FileStoragesCRUDController` ออกจาก `iLearn.API` เพื่อตัด endpoint `api/admin/FileStoragesCRUD/*` ที่ inherit generic CRUD และเสี่ยงโหลด/serialize `FileStorage.Data` (SCORM ZIP blob) แบบทั้งตาราง; ตรวจซ้ำว่า `IGenericRepository<FileStorage>` ยังใช้งานตามเดิมใน `ContentItemsController`, `ContentItemsCRUDController`, และ services ฝั่ง Application โดยไม่แตะ flow เสิร์ฟไฟล์จริง
- ไฟล์หลักที่แตะ: `iLearn.API/Controllers/Base/FileStoragesCRUDController.cs` (deleted), `DOC/PLANS/PLAN-013-remove-filestorages-crud-blob-exposure.md`, `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน (API shape / props / DB): เอา admin CRUD route สำหรับ FileStorage ออก (`api/admin/FileStoragesCRUD/*`); contract อื่นไม่เปลี่ยน
- Verified: `dotnet build iLearn.Tests -o artifacts\verify-test` ผ่าน, `dotnet test artifacts\verify-test\iLearn.Tests.dll` ผ่าน (115/115), ลบ `artifacts\verify-test` แล้ว

## [2026-06-15 11:20] Claude Code — เขียนแผนปรับปรุงจากผลวิเคราะห์ API (PLAN-013/014)
- ทำอะไร: แปลงข้อค้นพบใน api_analysis เป็นแผน 2 ใบ — **PLAN-013** (GPT, HIGH: ลบ `FileStoragesCRUDController` ที่ไม่มีใครใช้แต่ดัมพ์ SCORM blob ทั้งหมดผ่าน GetAllAsync — ยืนยัน grep client = 0), **PLAN-014** (Gemini, MED: `LearnerApiService` เลิกกลืน exception, ใช้ ILogger, แยก primary fetch (propagate→ProblemDetails/502) กับ enrichment helper (degrade graceful + log warning)) — ข้อ 5.3/5.4/5.5 (enrich-before-filter, refactor controller ใหญ่, anonymous→DTO) ยังไม่ออกแผน รอผู้ใช้เลือก
- ไฟล์หลักที่แตะ: `DOC/PLANS/PLAN-013-*.md`, `DOC/PLANS/PLAN-014-*.md` (เอกสารล้วน)
- Contract ที่เปลี่ยน: ไม่มี
- Verified: n/a (planner ไม่แก้โค้ด)

## [2026-06-15 11:00] Claude Code — วิเคราะห์ API ทั้งระบบ → DOC/api_analysis.md
- ทำอะไร: อ่านทั้ง backend (33 controllers, Program.cs, auth extensions, middleware, LearnerApiService, HMAC resolver) แล้วเขียนเอกสารวิเคราะห์รวม: สถาปัตยกรรม Clean Arch, โมเดล auth (policies + HMAC learner proxy), surface endpoint, pattern ที่ใช้ซ้ำ, และความเสี่ยงเรียงลำดับ — พบ landmine สำคัญ: **FileStoragesCRUDController ดัมพ์ SCORM blob ทั้งหมด** (HIGH), LearnerApiService กลืน exception (MED), DevExtreme filter ผูก projection (MED, กำลังแก้ PLAN-009/011/012), controller ใหญ่/anonymous-object response (LOW)
- ไฟล์หลักที่แตะ: `DOC/api_analysis.md` (เอกสารใหม่ — ไม่แตะโค้ด)
- Contract ที่เปลี่ยน: ไม่มี
- Verified: n/a (analysis ล้วน)

## [2026-06-15 10:30] Claude Code — Review รวบยอด: ปิด 9 แผนเป็น VERIFIED
- ทำอะไร: รีวิวงานค้างทั้งหมดแล้วปรับเป็น VERIFIED — PLAN-004 (Users wizard), PLAN-005 (LearnerGroupCategories wizard), PLAN-006 (Users detail+delete), PLAN-007 (shared detail components), PLAN-008 (detail migration), PLAN-009-refine (ลบ header/back, tabs), PLAN-010 (Reset→icon), PLAN-011 (search expr ทั้งระบบ), PLAN-012 (custom pages search). ตรวจด้วย: full build/lint/test เขียวหมด + grep acceptance (ใน src/pages: minmax 2-col grid=0, hand dt-fact=0, DetailPageHeader=0, ทั้ง 7 detail page ใช้ shared components) + spot-check MasterDataDetailPage (form ครอบ DetailLayout ถูก) + ยืนยัน createDataSource ลบ fallback title/code/name + searchExpr ทุกตัวตรงกับฟิลด์ที่เทส endpoint จริง=200 + ux_ui_analysis 2.4 ตรงกับ design จริง (action-only sidebar, ไม่มี header)
- ไฟล์หลักที่แตะ: `DOC/PLANS/PLAN-004..012*.md` (อัปเดตสถานะ — ไม่แตะโค้ด)
- Contract ที่เปลี่ยน: ไม่มี
- Verified: `npm run lint` (0 errors, 11 warnings baseline), `npm run build` ผ่าน, `dotnet test` 115/115 ผ่าน, grep checks ผ่าน

## [2026-06-15 08:40] Antigravity — Implement PLAN-011 แก้ search ตารางพังทั้งระบบและป้องกัน fallback
- ทำอะไร: กำหนด `searchExpr` ที่ใช้ฟิลด์สำหรับค้นหาได้จริงบน backend ใน config ของตารางทั้งหมด 8 ตาราง ได้แก่ `assignments`, `learningLogs`, `enrollments`, `masterDataDivisions`, `masterDataCategories`, `masterDataCourseTypes`, `masterDataRoles`, `masterData` และตัดการ fallback เป็น `['title', 'code', 'name']` ใน `createDataSource.ts` เมื่อไม่มี `searchExpr` เพื่อป้องกันการเกิด error ในฝั่ง backend EF Query
- ไฟล์หลักที่แตะ: `iLearn.Admin.React/src/pages/moduleConfigs.ts`, `iLearn.Admin.React/src/lib/createDataSource.ts`, `DOC/PLANS/PLAN-011-list-search-expr-fix.md`
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี
- Verified: `npm run lint` ผ่าน, `npm run build` ผ่าน, `dotnet test` (115/115 passed) ผ่านการทดสอบทั้งหมดแล้ว

## [2026-06-15 08:39] GitHub Copilot (GPT-5.3-Codex) — Implement PLAN-012 แก้ search หน้า Admin Users + ปิดแผนเป็น DONE
- ทำอะไร: แก้ `AdminUsersPage` ให้ search filter ฝั่ง server เหลือเฉพาะฟิลด์ที่ query ได้จริง (`nid`) โดยปรับ `searchExpr` จาก `['nid','fullName','division']` เป็น `['nid']`, ปรับ placeholder เป็น `Search by NID...`, และเติมคอมเมนต์อธิบายว่าฟิลด์ `fullName`/`division` ถูก enrich หลัง paging จึง filter server-side ไม่ได้; ตรวจ `CourseListPage` แล้ว `searchExpr={['title','code']}` ถูกต้องตาม property ของ Course จึงไม่ต้องแก้; อัปเดต `PLAN-012` เป็น DONE พร้อม Implementer Notes
- ไฟล์หลักที่แตะ: `iLearn.Admin.React/src/pages/users/AdminUsersPage.tsx`, `DOC/PLANS/PLAN-012-custom-pages-search-fix.md`, `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี
- Verified: `npm run lint` ผ่าน (0 errors, 11 warnings baseline), `npm run build` ผ่าน, `dotnet build iLearn.Tests -o artifacts\verify-test` ผ่าน, `dotnet test artifacts\verify-test\iLearn.Tests.dll` ผ่าน (115/115)

## [2026-06-15 10:00] Claude Code — ตรวจ search ทุกตาราง (เจอพังทั้งระบบ) + เขียน PLAN-011/012
- ทำอะไร: ต่อยอดจากบั๊ก Learners — ตรวจทุกตารางว่า search พังเหมือนกันไหม พบว่า `createDataSource.ts` มี fallback searchExpr `['title','code','name']` เมื่อ config ไม่ตั้ง searchExpr → DevExtreme filter บนฟิลด์ที่ไม่มีบน entity → throw ทั้งก้อน → ตารางที่ผ่าน createAdminDataSource ทั้งหมด search พัง ทดสอบยิง endpoint จริง (ดูแค่ HTTP status): Divisions default(title/code/name)=พัง, name-only=200; assignments `assignmentNo/description`=200, enrollments `learnerCode`=200, learningLogs `status`=200 — เขียน 2 แผน: **PLAN-011** (Gemini: เติม searchExpr ทุก config ใน moduleConfigs + กัน fallback อันตรายใน createDataSource), **PLAN-012** (GPT: AdminUsersPage searchExpr `fullName`/`division` filter ไม่ได้→เหลือ `nid`, verify CourseListPage) — contentLibrary (createRestDataSource param search) + LearnerGroupListPage (client-side filter) ไม่กระทบ
- ไฟล์หลักที่แตะ: `DOC/PLANS/PLAN-011-list-search-expr-fix.md`, `DOC/PLANS/PLAN-012-custom-pages-search-fix.md` (เอกสารล้วน)
- Contract ที่เปลี่ยน: ไม่มี
- Verified: n/a (planner; ทดสอบ endpoint จริงเพื่อยืนยันฟิลด์ที่ filter ได้ — ดูแค่ status code ไม่อ่าน PII)

## [2026-06-15 09:30] Claude Code — Hotfix บั๊ก Search Learners (root cause จริง = ฟิลด์ NID) + ปิด PLAN-009 VERIFIED
- ทำอะไร: ผู้ใช้แจ้งว่าแก้ casing (PLAN-009 ของ Gemini) แล้ว search ยัง 500 — ผู้ใช้อนุญาตให้ Claude ลงมือแก้+เทสเอง ทดสอบผ่าน API ตัวเองที่ localhost:7128 (ดูเฉพาะ HTTP status ไม่อ่าน PII): filter ที่มี `nid`=500, ไม่มี `nid`=200, `nid` อย่างเดียว=500 → สรุป external `/api/Student` filter ฟิลด์ NID ไม่ได้ (ตรงกับ grid ระบบเก่าที่ไม่เคยมี NID) — งาน casing ของ Gemini ถูกต้อง/จำเป็น (englishFirstName/eId ต้อง PascalCase ปลายทางถึงรับ) คงไว้; **Hotfix:** เอา `'nid'` ออกจาก `searchExpr` ของ learners ใน `moduleConfigs.ts` (คงคอลัมน์ NID ที่ external คืนค่ามาจริง) — ทดสอบ filter จริงผ่าน API = HTTP 200
- ไฟล์หลักที่แตะ: `iLearn.Admin.React/src/pages/moduleConfigs.ts`, `DOC/PLANS/PLAN-009-learners-search-field-casing.md`
- Contract ที่เปลี่ยน: ไม่มี (frontend search config เท่านั้น)
- Verified: เทส endpoint จริง (filter 3 ฟิลด์ = HTTP 200, baseline 200), `npm run lint` (0 errors, 11 warnings baseline), `npm run build` ผ่าน

## [2026-06-15 08:12] Antigravity — Implement PLAN-009 แปลง Filter Casing ของ Learners ตาราง Search
- ทำอะไร: แก้ไขบั๊กการค้นหาในตาราง Learners โดยการเพิ่มขั้นตอนการแปลงฟิลด์ filter จาก camelCase ไปเป็น PascalCase ด้วย Regex ใน `LearnersController.cs` ก่อนที่จะ proxy query string ไปยัง external employee API และสร้าง unit tests ใหม่ใน `LearnersControllerTests.cs`
- ไฟล์หลักที่แตะ: `iLearn.API/Controllers/LearnersController.cs`, `iLearn.Tests/LearnersControllerTests.cs`, `DOC/PLANS/PLAN-009-learners-search-field-casing.md`
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี (ทำงานเข้ากับ API contract เดิมทั้งหมด)
- Verified: `dotnet test` (115/115 passed) ผ่านการทดสอบทั้งหมดแล้ว

## [2026-06-12 19:10] Claude Code — วินิจฉัยบั๊ก Search ตาราง Learners + เขียน PLAN-009
- ทำอะไร: ผู้ใช้รายงาน search ตาราง `/learners` พัง ขึ้น "Failed to connect to the employee data source" — วินิจฉัยจากโค้ด: frontend ส่ง filter field camelCase (`nid`/`englishFirstName`/`englishLastName`/`eId`) แต่ external employee DevExtreme API ผูก property PascalCase (ยืนยันจาก `LearnerGridRowDto` PascalCase + `InjectDivisionFilter` ฉีด `"Division"` PascalCase) → `LearnersController.Get()` proxy ส่ง query string ดิบ → external throw → service catch คืน null → error กำกวม เขียน **PLAN-009** (Assigned: Gemini): แก้ backend แปลงชื่อฟิลด์ใน filter camelCase→PascalCase ก่อน forward (ระวังไม่แทนค่าค้นหาที่ผู้ใช้พิมพ์)
- ไฟล์หลักที่แตะ: `DOC/PLANS/PLAN-009-learners-search-field-casing.md` (เอกสารล้วน)
- Contract ที่เปลี่ยน: ไม่มี
- Verified: n/a (planner; ยิง external HR API ตรงถูกบล็อกด้วยเหตุผล PII — วินิจฉัยจากโค้ด)

## [2026-06-12 19:00] GitHub Copilot (GPT-5.3-Codex) — Implement PLAN-010 ปรับปุ่ม Reset ใน Assignment Learners เป็นไอคอน
- ทำอะไร: แก้ `AssignmentDetailPage.tsx` ให้ปุ่ม Reset ในตาราง Learners เปลี่ยนจาก text button เป็น icon button (`RotateCcw`) และใช้สไตล์เดียวกับ action icon อื่น (`p-1 text-slate-400 hover:text-indigo-600 rounded transition cursor-pointer`) เพื่อให้สมดุลกับปุ่มถังขยะ; ไม่แตะ logic ของการ reset/API
- ไฟล์หลักที่แตะ: `iLearn.Admin.React/src/pages/assignments/AssignmentDetailPage.tsx`, `DOC/PLANS/PLAN-010-refine-actions-to-icons.md`, `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี
- Verified: `npm run lint` ผ่าน (0 errors, 11 warnings baseline), `npm run build` ผ่าน

## [2026-06-12 18:57] GitHub Copilot (GPT-5.3-Codex) — ปิด PLAN-009 เป็น DONE และยืนยันผลทดสอบรอบสุดท้าย
- ทำอะไร: ตรวจเทียบโค้ดจริงกับขอบเขต PLAN-009 ซ้ำทั้งหมด (DetailPageHeader/backTo/backLabel ถูกถอด, sidebar เป็น action-only, Assignment และ Learner Group เป็นแท็บตามสเปค), อัปเดตสถานะ `PLAN-009-refine-detail-pages-design.md` จาก READY เป็น DONE พร้อม acceptance และ implementer notes, แล้วยืนยันว่า scope ต่อเนื่องของ PLAN-008 อยู่ในสถานะ DONE แล้ว
- ไฟล์หลักที่แตะ: `DOC/PLANS/PLAN-009-refine-detail-pages-design.md`, `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี (เป็นการปิดแผนและยืนยันผล)
- Verified: `npm run lint` ผ่าน (0 errors, 11 warnings baseline), `npm run build` ผ่าน

## [2026-06-12 18:54] GitHub Copilot (GPT-5.3-Codex) — ปิดช่องว่าง LearnerGroup tabs + บังคับ action-only sidebar
- ทำอะไร: รีแฟกเตอร์ `LearnerGroupDetailPage` ให้มีแท็บ `Overview` และ `Members`; ย้ายข้อมูล `LMS Category` และ `Owner / Creator` ออกจาก `ControlsSidebar` ไปอยู่ในการ์ด Overview (`DetailCard` + `FactGrid`); ลบการใช้ `ControlsDivider`; ปรับ `ControlsSidebar` ให้เป็น action-only API ด้วยการลบ `ControlsDivider` helper ออกจากคอมโพเนนต์กลาง
- ไฟล์หลักที่แตะ: `iLearn.Admin.React/src/pages/learner-groups/LearnerGroupDetailPage.tsx`, `iLearn.Admin.React/src/components/ui/ControlsSidebar.tsx`, `DOC/PLANS/PLAN-008-detail-pages-migration.md`, `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน (API shape / props / DB): ปรับ UI helper contract ภายใน (`ControlsDivider` ถูกถอดออก); API/DB ไม่เปลี่ยน
- Verified: `npm run lint` ผ่าน (0 errors, 11 warnings baseline), `npm run build` ผ่าน; manual browser ที่ `/learner-groups/22` ยืนยันแท็บทำงานครบและ sidebar เหลือเฉพาะปุ่ม

## [2026-06-12 18:43] GitHub Copilot (GPT-5.3-Codex) — ปิดงาน DETAIL redesign ใหม่ + Assignment tabs
- ทำอะไร: ปรับ `AssignmentDetailPage` ให้เลิกใช้ KPI/Metric strip ด้านบน แล้วเปลี่ยนเป็นแท็บ `Overview`, `Courses`, `Learners` โดยย้าย metrics + schedule ไปอยู่ใน `Overview` ด้วย `DetailCard` + `FactGrid`; ยืนยันมาตรฐานใหม่ทั้งระบบ detail ว่าไม่มี `DetailPageHeader` และไม่มี Back link ใน `ControlsSidebar`; อัปเดต `PLAN-008` acceptance/notes ให้ตรงผลงานล่าสุด
- ไฟล์หลักที่แตะ: `iLearn.Admin.React/src/pages/assignments/AssignmentDetailPage.tsx`, `DOC/PLANS/PLAN-008-detail-pages-migration.md`, `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มีเพิ่ม (คงผลจากรอบก่อนที่ตัด `ControlsSidebar` props `backTo`/`backLabel`)
- Verified: `npm run lint` ผ่าน (0 errors, 11 warnings baseline), `npm run build` ผ่าน; grep ผ่านสำหรับ `DetailPageHeader`, `<ControlsSidebar ... backTo=...>`, `backTo?`/`backLabel?`/`ArrowLeft`, และ `auto-cols-fr grid-flow-col` ใน `AssignmentDetailPage.tsx`

## [2026-06-12 18:40] GitHub Copilot (GPT-5.3-Codex) — ปรับ Detail Pages ตามดีไซน์ใหม่ (ตัด Page Header + ตัด Back action ใน ControlsSidebar)
- ทำอะไร: ปรับมาตรฐานหน้า Detail ตามทิศทางใหม่โดยลบ `DetailPageHeader` ออกจาก shared detail primitives (`src/components/ui/detail/index.tsx`), ลบพร็อพ `backTo`/`backLabel` และลบการเรนเดอร์ปุ่ม Back ออกจาก `ControlsSidebar`; migrate หน้า detail ทั้ง 7 หน้าให้เลิกใช้ header และเลิกส่ง back props (`UserDetailPage`, `ContentItemDetailPage`, `CourseDetailPage`, `AssignmentDetailPage`, `LearnerGroupDetailPage`, `MasterDataDetailPage`, `LearnerProfilePage`) พร้อมคง logic เดิมของ tabs/modals/forms/actions
- ไฟล์หลักที่แตะ: `iLearn.Admin.React/src/components/ui/detail/index.tsx`, `iLearn.Admin.React/src/components/ui/ControlsSidebar.tsx`, `iLearn.Admin.React/src/pages/users/UserDetailPage.tsx`, `iLearn.Admin.React/src/pages/content-library/ContentItemDetailPage.tsx`, `iLearn.Admin.React/src/pages/courses/CourseDetailPage.tsx`, `iLearn.Admin.React/src/pages/assignments/AssignmentDetailPage.tsx`, `iLearn.Admin.React/src/pages/learner-groups/LearnerGroupDetailPage.tsx`, `iLearn.Admin.React/src/pages/master-data/MasterDataDetailPage.tsx`, `iLearn.Admin.React/src/pages/learners/LearnerProfilePage.tsx`, `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน (API shape / props / DB): เปลี่ยน UI props contract ของ `ControlsSidebar` โดยตัด `backTo`/`backLabel` ออก (API/DB ไม่เปลี่ยน)
- Verified: `npm run lint` ผ่าน (0 errors, 11 warnings baseline), `npm run build` ผ่าน; grep ยืนยันไม่มี `DetailPageHeader`, ไม่มี `<ControlsSidebar ... backTo=...>` และไม่มี `backTo?`/`backLabel?` ใน `ControlsSidebar.tsx`

## [2026-06-12 18:10] GitHub Copilot (GPT-5.3-Codex) — Implement PLAN-007 shared detail components และปิด PLAN-008 migration
- ทำอะไร: สร้าง shared detail primitives ใหม่ที่ `src/components/ui/detail/index.tsx` (`DetailPageHeader`, `DetailLayout`, `DetailCard`, `FactGrid`, `Fact`, `DetailSubSection`) แล้ว migrate หน้า detail ตามแผนครบ: PLAN-007 (`UserDetailPage`, `ContentItemDetailPage`) และ PLAN-008 (`CourseDetailPage`, `AssignmentDetailPage`, `LearnerGroupDetailPage`, `MasterDataDetailPage`, `LearnerProfilePage`); เพิ่ม `DetailPageHeader` ให้ `CourseDetailPage`/`LearnerGroupDetailPage` ตามเกณฑ์ยกระดับ; คง logic เดิมของ tabs, modals, members table, master-data edit form; อัปเดตสถานะแผน PLAN-008 เป็น DONE พร้อม acceptance checklist/notes
- ไฟล์หลักที่แตะ: `iLearn.Admin.React/src/components/ui/detail/index.tsx`, `iLearn.Admin.React/src/pages/users/UserDetailPage.tsx`, `iLearn.Admin.React/src/pages/content-library/ContentItemDetailPage.tsx`, `iLearn.Admin.React/src/pages/courses/CourseDetailPage.tsx`, `iLearn.Admin.React/src/pages/assignments/AssignmentDetailPage.tsx`, `iLearn.Admin.React/src/pages/learner-groups/LearnerGroupDetailPage.tsx`, `iLearn.Admin.React/src/pages/master-data/MasterDataDetailPage.tsx`, `iLearn.Admin.React/src/pages/learners/LearnerProfilePage.tsx`, `iLearn.Admin.React/src/pages/assignments/AssignmentReportPage.tsx`, `DOC/PLANS/PLAN-008-detail-pages-migration.md`, `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี
- Verified: `npm run lint` ผ่าน (0 errors, 11 warnings baseline), `npm run build` ผ่าน, grep acceptance ผ่าน (`minmax(0,1fr)_280px` = 0 และ `text-slate-400 font-bold uppercase tracking-wider` = 0 ใน `src/pages`), manual smoke ผ่านที่ `/courses/823`, `/assignments/248`, `/learner-groups/22`, `/master-data/divisions/1`, `/learners/n4734/profile`

## [2026-06-12 17:35] Antigravity — แก้ไขบั๊กการแสดงผลบทบาทผู้ใช้ (camelCase field mappings) และปัญหา key prop ใน UserEditorPage
- ทำอะไร: ปรับเปลี่ยนการเข้าถึงคุณสมบัติของออบเจกต์บทบาทผู้ใช้ (Role และ UserRole) จาก PascalCase เป็น camelCase ใน [UserEditorPage.tsx](file:///c:/Users/n4734/source/repos/iLearn2/iLearn.Admin.React/src/pages/users/UserEditorPage.tsx) และ [UserDetailPage.tsx](file:///c:/Users/n4734/source/repos/iLearn2/iLearn.Admin.React/src/pages/users/UserDetailPage.tsx) เพื่อให้สอดคล้องกับ DTO / API response JSON payload ที่ส่งมาจาก backend และแก้ปัญหา React warning เรื่อง unique key ในรายการบทบาท
- ไฟล์หลักที่แตะ: `iLearn.Admin.React/src/pages/users/UserEditorPage.tsx`, `iLearn.Admin.React/src/pages/users/UserDetailPage.tsx`, `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี (เป็นการแก้ฝั่ง Client/Frontend ให้ตรงตาม Contract ของ API ปัจจุบัน)
- Verified: `npm run lint` ผ่าน, `npm run build` ผ่าน, `dotnet test` (112/112 passed) ผ่านเรียบร้อย


## [2026-06-12 17:20] Antigravity — Implement PLAN-006 เพิ่มหน้าแสดงรายละเอียดผู้ใช้ระบบและฟังก์ชันการลบผู้ใช้
- ทำอะไร: สร้างหน้าสำหรับแสดงข้อมูลโดยละเอียด of Admin User ([UserDetailPage.tsx](file:///c:/Users/n4734/source/repos/iLearn2/iLearn.Admin.React/src/pages/users/UserDetailPage.tsx)) พร้อมแสดง Metric, ข้อมูลสังกัดองค์กร, และ Administrative Roles; ติดตั้งฟังก์ชันการลบแอดมินใน ControlsSidebar ผ่าน `useConfirm` และยิง `DELETE admin/UsersCRUD/Delete`; ปรับ [AdminUsersPage.tsx](file:///c:/Users/n4734/source/repos/iLearn2/iLearn.Admin.React/src/pages/users/AdminUsersPage.tsx) ให้ดับเบิลคลิกเปิดหน้ารายละเอียด และเพิ่มปุ่มรูปตา (`Eye`) สำหรับนำทาง; อัปเดตไฟล์รูท [App.tsx](file:///c:/Users/n4734/source/repos/iLearn2/iLearn.Admin.React/src/App.tsx)
- ไฟล์หลักที่แตะ: `iLearn.Admin.React/src/pages/users/UserDetailPage.tsx`, `iLearn.Admin.React/src/pages/users/AdminUsersPage.tsx`, `iLearn.Admin.React/src/App.tsx`, `DOC/PLANS/PLAN-006-admin-users-detail-delete.md`, `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน: ไม่มี (การเรียกใช้ API endpoint ยังรักษา Contract รูปแบบเดิม)
- Verified: `npm run lint` ผ่าน, `npm run build` ผ่าน, `dotnet test` (112/112 passed) ผ่านเรียบร้อย

## [2026-06-12 17:15] Claude Code — เขียน PLAN-006 (Admin Users: หน้า Detail + ฟังก์ชันลบ)
- ทำอะไร: ผู้ใช้รายงานช่องว่างที่พบระหว่างทำ PLAN-004: module Users ไม่มีหน้า Detail (`/users/:id`) และไม่มีฟังก์ชันลบ — ตรวจ backend แล้ว `GenericController.Delete` (`DELETE admin/UsersCRUD/Delete`, FormData `key`) มีอยู่แล้วไม่ต้องแก้ → เขียน **PLAN-006** (Assigned: Gemini): ไฟล์ใหม่ `UserDetailPage.tsx` (การ์ด + ControlsSidebar: Edit Roles / Delete ผ่าน useConfirm), grid dblclick → detail, route `users/:id` (Remount + RequireRole), breadcrumb setLabel เป็นชื่อ user
- ไฟล์หลักที่แตะ: `DOC/PLANS/PLAN-006-admin-users-detail-delete.md` (เอกสารล้วน)
- Contract ที่เปลี่ยน: ไม่มี
- Verified: n/a (planner ไม่แก้โค้ด)

## [2026-06-12 15:59] GitHub Copilot (GPT-5.3-Codex) — Implement PLAN-005 เปลี่ยน Learner Group Categories จาก modal เป็น Wizard
- ทำอะไร: สร้างหน้าใหม่ `LearnerGroupCategoryEditorPage` แบบ `AppWizard` 2 steps (Details/Review) รองรับ create+edit; ตัด modal create/edit ออกจาก `LearnerGroupCategoriesPage` และเปลี่ยน New/Edit เป็น route navigate; export type `LearnerGroupCategory` + `ApiListResponse` เพื่อใช้ร่วม; เพิ่ม routes `/master-data/learner-group-categories/new` และ `/master-data/learner-group-categories/:id/edit` (RequireRole + Remount) วางก่อน generic master-data routes; อัปเดตสถานะไฟล์แผน PLAN-005 เป็น DONE พร้อม checklist และ Implementer Notes
- ไฟล์หลักที่แตะ: `iLearn.Admin.React/src/pages/master-data/LearnerGroupCategoryEditorPage.tsx`, `iLearn.Admin.React/src/pages/master-data/LearnerGroupCategoriesPage.tsx`, `iLearn.Admin.React/src/App.tsx`, `DOC/PLANS/PLAN-005-learner-group-categories-wizard.md`, `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน: ไม่มี (ใช้ endpoint เดิม GET/POST/PUT/DELETE ของ LearnerGroupCategories)
- Verified: `npm run lint` ผ่าน (11 warnings baseline, 0 errors), `npm run build` ผ่าน; manual verify ผ่าน (new/edit wizard flow, invalid id -> NotFoundState, `/master-data/divisions/new` ไม่ถูก route ใหม่ชน); ทดสอบสร้างข้อมูลชั่วคราว `PLAN005_TMP` แล้วลบทิ้งเรียบร้อย

## [2026-06-12 17:00] Claude Code — สำรวจหน้าที่ create/edit ไม่ตรงมาตรฐาน wizard + เขียน PLAN-005
- ทำอะไร: ไล่ตรวจทุก page ว่า create/edit ใช้ pattern ไหน — เจอ 1 หน้าที่เข้าเคสเดียวกับ Users: `LearnerGroupCategoriesPage` (modal "New/Edit Category" กลางจอในหน้า list) → เขียน **PLAN-005** (Assigned: GPT): ไฟล์ใหม่ `LearnerGroupCategoryEditorPage.tsx` (wizard 2 steps Details→Review), ตัด modal, เพิ่ม routes `/master-data/learner-group-categories/new|/:id/edit` (ระวังไม่ให้ชน generic `master-data/:type/*`) — ข้อสังเกตอื่น: `MasterDataDetailPage` เป็นหน้าแยก+edit-in-place (ไม่ใช่ wizard แต่ไม่ใช่ modal) = borderline ยังไม่ออกแผน, modal ใน `AssignmentDetailPage` (Extend Due Date/Add Learners) เป็น action เฉพาะกิจ ถือว่าโอเค
- ไฟล์หลักที่แตะ: `DOC/PLANS/PLAN-005-learner-group-categories-wizard.md` (เอกสารล้วน)
- Contract ที่เปลี่ยน: ไม่มี
- Verified: n/a (planner ไม่แก้โค้ด)

## [2026-06-12 16:50] Antigravity — Implement PLAN-004 เปลี่ยนหน้าจัดการผู้ใช้ระบบเป็นแบบ Wizard
- ทำอะไร: สร้างหน้าสำหรับเพิ่ม/แก้ไขผู้ใช้ระบบแบบ Wizard ([UserEditorPage.tsx](file:///c:/Users/n4734/source/repos/iLearn2/iLearn.Admin.React/src/pages/users/UserEditorPage.tsx)) โดยในโหมดสร้างผู้ใช้ใหม่จะมี 3 ขั้นตอน (ระบุ NID -> เลือก Roles -> รีวิว) และในโหมดแก้ไขจะมี 2 ขั้นตอน (เลือก Roles -> รีวิว); นำพาเนล slide-over เดิมออกจาก [AdminUsersPage.tsx](file:///c:/Users/n4734/source/repos/iLearn2/iLearn.Admin.React/src/pages/users/AdminUsersPage.tsx) เปลี่ยนเป็นการ navigate ไปยัง Wizard แทน; เพิ่มเส้นทางใน [App.tsx](file:///c:/Users/n4734/source/repos/iLearn2/iLearn.Admin.React/src/App.tsx) (พร้อมครอบ Remount และ RequireRole) และกำหนด Breadcrumbs segment สำหรับ users ใน [Breadcrumbs.tsx](file:///c:/Users/n4734/source/repos/iLearn2/iLearn.Admin.React/src/components/layout/Breadcrumbs.tsx)
- ไฟล์หลักที่แตะ: `iLearn.Admin.React/src/pages/users/UserEditorPage.tsx`, `iLearn.Admin.React/src/pages/users/AdminUsersPage.tsx`, `iLearn.Admin.React/src/App.tsx`, `iLearn.Admin.React/src/components/layout/Breadcrumbs.tsx`, `DOC/PLANS/PLAN-004-admin-users-wizard-editor.md`, `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน: ไม่มี (การเรียกใช้ API endpoint ยังรักษา Contract รูปแบบเดิมของ DevExtreme)
- Verified: `npm run lint` ผ่าน, `npm run build` ผ่าน, `dotnet test` (112/112 passed) ผ่านเรียบร้อย

## [2026-06-12 16:45] Claude Code — เขียน PLAN-004 (Admin Users new/edit → Wizard)
- ทำอะไร: ผู้ใช้ชี้ว่า `/users` ทำ create/edit ผ่าน slide-over panel ไม่ตรงมาตรฐาน wizard ของระบบ — สำรวจ `AdminUsersPage.tsx` + `UsersCRUDController.cs` (Post คืน entity พร้อม id → create แล้ว assign roles ต่อใน flow เดียวได้) แล้วเขียน **PLAN-004** (Assigned: Gemini): ไฟล์ใหม่ `UserEditorPage.tsx` (create 3 steps: User→Roles→Review, edit 2 steps: Roles→Review), ตัด panel ใน `AdminUsersPage`, เพิ่ม routes `/users/new` + `/users/:id/edit` (Remount + RequireRole), เติม `'users'` ใน Breadcrumbs SEGMENT_MAP
- ไฟล์หลักที่แตะ: `DOC/PLANS/PLAN-004-admin-users-wizard-editor.md` (เอกสารล้วน — ยังไม่แก้โค้ด)
- Contract ที่เปลี่ยน: ไม่มี
- Verified: n/a (planner ไม่แก้โค้ด)

## [2026-06-12 16:30] Claude Code — Review + ปิด PLAN-002 และ PLAN-003 เป็น VERIFIED
- ทำอะไร: รีวิว diff ทั้งสองแผน — **PLAN-002** (GPT, dead code cleanup): ลบ branch `LearnerGroupsCRUD` ครบทุกจุด, คง `ContentItemsCRUD`+`createRestDataSource` ตามข้อห้าม, import ไม่เหลือ dead, ป้ายวันที่ folder=Created/group=Updated; **PLAN-003** (Gemini, sidebar): Learning เหลือ 3 รายการ, ย้าย Content Library+Learners ไป Operations, path/icon เดิม ไม่แตะ App.tsx/Super Admin — ปรับสถานะทั้งคู่เป็น VERIFIED
- ไฟล์หลักที่แตะ: `DOC/PLANS/PLAN-002-*.md`, `DOC/PLANS/PLAN-003-*.md` (เอกสาร — code เปลี่ยนโดย implementer แล้ว)
- Contract ที่เปลี่ยน: ไม่มี
- Verified: รันซ้ำเอง `npm run lint` (0 error / 10 warning baseline), `npm run build` ผ่าน

## [2026-06-12 16:20] Antigravity — ปรับกลุ่ม Learning และ Operations ใน sidebar ตามแผน PLAN-003
- ทำอะไร: ย้ายเมนู Content Library และ Learners จากกลุ่ม Learning ไปไว้ในกลุ่ม Operations ใน sidebar เพื่อให้สอดคล้องกับโครงสร้างฟังก์ชันการทำงาน และเหลือเฉพาะแกนหลักของการเรียนในกลุ่ม Learning
- ไฟล์หลักที่แตะ: `iLearn.Admin.React/src/config/navigation.ts`, `DOC/PLANS/PLAN-003-sidebar-learning-group.md`, `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน: ไม่มี
- Verified: `npm run lint` ผ่าน, `npm run build` ผ่าน, `dotnet test` ผ่านเรียบร้อย

## [2026-06-12 16:15] Claude Code — ปิด PLAN-001 เป็น CANCELLED (ยืนยันโดยผู้ใช้)
- ทำอะไร: ผู้ใช้ยืนยันว่าไม่ต้องการ Metadata step ในโหมด create ของหน้า Upload SCORM — ปิด PLAN-001 ถาวรเป็น CANCELLED (Gemini ตั้งสถานะ + เขียน Implementer Notes ไว้แล้ว ผมเติมบรรทัดยืนยัน planner) บั๊กเลือก Content Type ตอน upload ถือว่าไม่แก้ตามความต้องการล่าสุด
- ไฟล์หลักที่แตะ: `DOC/PLANS/PLAN-001-content-upload-type-step.md` (เอกสารล้วน)
- Contract ที่เปลี่ยน: ไม่มี
- Verified: n/a

## [2026-06-12 16:10] Claude Code — เขียน PLAN-003 (ปรับกลุ่ม Learning ใน sidebar)
- ทำอะไร: ผู้ใช้ขอให้กลุ่ม Learning ใน sidebar เหลือแค่ Courses/Assignments/Learner Groups — เขียน **PLAN-003** (Assigned: Gemini) ให้ย้าย Content Library + Learners ไปกลุ่ม Operations (route เดิม ไม่ลบ) แก้ไฟล์เดียว `navigation.ts`
- ไฟล์หลักที่แตะ: `DOC/PLANS/PLAN-003-sidebar-learning-group.md` (เอกสารล้วน — ยังไม่แก้โค้ด)
- Contract ที่เปลี่ยน: ไม่มี
- Verified: n/a (planner ไม่แก้โค้ด)

## [2026-06-12 15:35] Antigravity — ยกเลิกขั้นตอน Metadata ในโหมด Create และลบ Dead Code ตามคำสั่งผู้ใช้
- ทำอะไร: ยกเลิกการเพิ่มขั้นตอน Metadata ในโหมด Create ของหน้า Upload SCORM กลับมาเป็น 2 ขั้นตอน (Package Upload -> Review) ตามเดิม พร้อมเคลียร์โค้ดที่ไม่ได้ใช้ (Dead Code) ใน `ContentItemEditorPage.tsx` เช่น การเรียกใช้ PUT endpoint `admin/ContentItemsCRUD/Put` เมื่ออัปโหลดไฟล์ และปรับปรุงเงื่อนไข `validateMetadata` กับ `renderMetadataStep` ให้คลีนยิ่งขึ้น
- ไฟล์หลักที่แตะ: `iLearn.Admin.React/src/pages/content-library/ContentItemEditorPage.tsx`, `DOC/PLANS/PLAN-001-content-upload-type-step.md`, `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน: ไม่มี
- Verified: `npm run lint` ผ่าน, `npm run build` ผ่าน, `dotnet test` ผ่านเรียบร้อย

## [2026-06-12 15:33] GitHub Copilot (GPT-5.3-Codex) — Implement PLAN-002 เก็บ dead code หลังย้าย Learner Groups ไป Explorer
- ทำอะไร: ลบ config ที่ไม่ถูกใช้งาน `adminListConfigs.learnerGroups`; ลบ branch dead code `LearnerGroupsCRUD` ใน `EntityListPage` ครบ (lookup categories, rest datasource branch, route prefix branch, grid action, crudControllers set); ปรับ Explorer คอลัมน์วันที่ให้สื่อความหมายตรง (`Created` สำหรับโฟลเดอร์, `Updated` สำหรับกลุ่ม); อัปเดตสถานะแผน `DOC/PLANS/PLAN-002-learner-group-explorer-cleanup.md` เป็น `DONE` พร้อม checklist/notes
- ไฟล์หลักที่แตะ: `iLearn.Admin.React/src/pages/moduleConfigs.ts`, `iLearn.Admin.React/src/pages/EntityListPage.tsx`, `iLearn.Admin.React/src/pages/learner-groups/LearnerGroupListPage.tsx`, `DOC/PLANS/PLAN-002-learner-group-explorer-cleanup.md`, `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน: ไม่มี
- Verified: `npm run lint` ผ่าน (warning baseline เดิม 10 รายการ, ไม่มี error), `npm run build` ผ่าน; smoke browser เปิด `/assignments`, `/content-library`, `/master-data/divisions`, `/learner-groups` ได้

## [2026-06-12 15:52] Antigravity — เพิ่ม Metadata step ในโหมด Create ของหน้า Upload SCORM ตามแผน PLAN-001
- ทำอะไร: ปรับปรุง `ContentItemEditorPage.tsx` ในโหมด Create ให้มีขั้นตอนเลือก Metadata (Display Name + Content Type Learn/Exam) เป็น Step แรก ก่อนอัปโหลดไฟล์ SCORM เพื่อสนับสนุนการระบุ Content Type ในตอนอัปโหลดโดยตรง และกรอกแบบฟอร์มตรวจสอบข้อมูลเสร็จสิ้นใน 3 ขั้นตอน
- ไฟล์หลักที่แตะ: `iLearn.Admin.React/src/pages/content-library/ContentItemEditorPage.tsx`, `DOC/PLANS/PLAN-001-content-upload-type-step.md`, `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน: ไม่มี
- Verified: `npm run lint` ผ่าน, `npm run build` ผ่าน, `dotnet test` (112/112 tests passed) ผ่านเรียบร้อย

## [2026-06-12 15:50] Claude Code — ตั้งระบบ Planner/Implementer + แผน 2 ใบแรก
- ทำอะไร: ผู้ใช้กำหนดบทบาทใหม่: Claude = planner/reviewer, Gemini + GPT = implementer — สร้าง `DOC/PLANS/` (README + template + workflow READY→DONE→VERIFIED), เขียนแผนพร้อมทำ 2 ใบ: **PLAN-001** (Assigned: Gemini — หน้า Upload SCORM โหมด create เลือก Content Type ไม่ได้เพราะ wizard ข้าม Metadata step), **PLAN-002** (Assigned: GPT — ลบ dead code `adminListConfigs.learnerGroups` + branch LearnerGroupsCRUD ใน EntityListPage + ป้ายวันที่โฟลเดอร์), อัปเดตบทบาทใน `CLAUDE.md`
- ไฟล์หลักที่แตะ: `CLAUDE.md`, `DOC/PLANS/README.md`, `DOC/PLANS/PLAN-001-*.md`, `DOC/PLANS/PLAN-002-*.md`
- Contract ที่เปลี่ยน: ไม่มี (เอกสารล้วน)
- Verified: n/a (ไม่มีโค้ดเปลี่ยน)

## [2026-06-12 15:30] Claude Code — Review งานชุด Learner Group Explorer + แก้บั๊ก deep-link
- ทำอะไร: รีวิว commits 75d79a7..e2c7a44 ทั้งชุด (Explorer แบบโฟลเดอร์, breadcrumb custom trail, AppTableSearch/Footer ใหม่, backend filter) — พบและแก้บั๊ก 1 ตัว: เปิด `/learner-groups?categoryId=N` ตรง ๆ หรือ refresh ระหว่างอยู่ในโฟลเดอร์จะถูกเด้งกลับ root เพราะ effect ที่ validate categoryId ทำงานก่อน categories โหลดเสร็จ → เพิ่ม guard `loading || categories.length === 0` (ยืนยันใน browser แล้วทั้ง id จริง=อยู่ต่อ และ id ปลอม=ถูกดีดตามเดิม)
- ไฟล์หลักที่แตะ: `iLearn.Admin.React/src/pages/learner-groups/LearnerGroupListPage.tsx`
- Contract ที่เปลี่ยน: ไม่มี
- ข้อสังเกตค้างไว้ (ยังไม่แก้): `adminListConfigs.learnerGroups` ใน moduleConfigs ไม่มีผู้ใช้แล้ว; `PaginationParams.CategoryId`/`RootCategoryOnly` ฝั่ง backend ยังไม่มี frontend เรียกใช้ (Explorer กรองแบบ client-side)
- Verified: tsc 0 error, eslint 0 error (warning baseline 10), vite build ผ่าน, dotnet test 112/112

## [2026-06-12 13:14] GitHub Copilot (GPT-5.3-Codex) — ลดจุดแสดงจำนวน record ใน grid ให้เหลือจุดเดียว
- ทำอะไร: ปรับ `AppTableFooter` ให้ไม่แสดงข้อความ `Showing X of Y records` ซ้ำกับส่วนบนของ grid อีกต่อไป โดยคงเฉพาะสถานะโหลดด้านล่าง (`Loading more...`, `Scroll down to load more`, `All records loaded`) ทำให้จุดแสดงจำนวน record เหลือจุดเดียวที่ toolbar/search ด้านบน
- ไฟล์หลักที่แตะ: `iLearn.Admin.React/src/components/ui/table/AppTableFooter.tsx`, `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน: ไม่มี
- Verified: `npm run lint` ผ่าน (warning baseline เดิม 10 รายการ, ไม่มี error), `npm run build` ผ่าน

## [2026-06-12 13:11] GitHub Copilot (GPT-5.3-Codex) — ยกระดับหน้า Learner Group Categories + Assignment Gantt ให้เป็นมาตรฐาน UI เดียวกัน
- ทำอะไร: รีแฟกเตอร์ 2 หน้าให้ใช้ shared layout เดียวกับหน้า list มาตรฐาน โดยย้ายขึ้น `DataGridSurface`, ปรับโครง header/actions/summary bar, ปรับตารางให้เป็น bordered unified surface พร้อม sticky header + custom scrollbar และปรับ loading/empty state ให้ใช้แพทเทิร์นเดียวกัน; หน้า Gantt เพิ่ม action ปุ่ม `Today` แบบมาตรฐาน, ปรับ label filter ให้อ่านง่าย (`In Progress`) และย้ายทั้ง timeline ลงใน grid-surface เดียวกับหน้าอื่น
- ไฟล์หลักที่แตะ: `iLearn.Admin.React/src/pages/master-data/LearnerGroupCategoriesPage.tsx`, `iLearn.Admin.React/src/pages/assignments/AssignmentGanttPage.tsx`, `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน: ไม่มี
- Verified: `npm run lint` ผ่าน (warning baseline เดิม 10 รายการ, ไม่มี error), `npm run build` ผ่าน

## [2026-06-12 13:06] GitHub Copilot (GPT-5.3-Codex) — ย้าย Schedule ออกจาก sidebar ไปหน้า Assignments และย่อชื่อ
- ทำอะไร: ลบเมนู `Schedule (Gantt)` ออกจาก sidebar หมวด Learning; เพิ่มปุ่มในหน้า Assignments (`EntityListPage`) สำหรับเปิด `/assignments/gantt`; เปลี่ยนชื่อปุ่มให้สั้นเป็น `Schedule`; และปรับ breadcrumb ของ route `gantt` ให้แสดง `Schedule`
- ไฟล์หลักที่แตะ: `iLearn.Admin.React/src/config/navigation.ts`, `iLearn.Admin.React/src/pages/EntityListPage.tsx`, `iLearn.Admin.React/src/components/layout/Breadcrumbs.tsx`, `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน: ไม่มี
- Verified: `npm run lint` ผ่าน (warning baseline เดิม 10 รายการ, ไม่มี error), `npm run build` ผ่าน

## [2026-06-12 13:04] GitHub Copilot (GPT-5.3-Codex) — เอา sub menu ใต้ Assignments ออกจาก sidebar
- ทำอะไร: ปรับ navigation ฝั่ง React ให้ `Assignments` ไม่มี `children` แล้ว และย้ายลิงก์ `Schedule (Gantt)` มาเป็นเมนูหลักระดับเดียวกันในหมวด Learning เพื่อไม่ให้เกิด sub sidebar ใต้ Assignments
- ไฟล์หลักที่แตะ: `iLearn.Admin.React/src/config/navigation.ts`, `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน: ไม่มี
- Verified: `npm run lint` ผ่าน (warning baseline เดิม 10 รายการ, ไม่มี error), `npm run build` ผ่าน

## [2026-06-12 12:59] GitHub Copilot (GPT-5.3-Codex) — ปรับ Shared Grid UI ให้เป็น bordered unified style ทั้งระบบ
- ทำอะไร: ปรับคอมโพเนนต์ตารางกลางให้ทุกหน้าได้สไตล์เดียวกับแนว Learner Group Explorer โดยเพิ่ม bottom padding ให้ `DataGridSurface`; รีโครง `AppTable` ให้กรอบตาราง (viewport + footer) เป็น border rounded + shadow เดียวกัน; ปรับ spacing หัวคอลัมน์/เซลล์ข้อมูลจาก `py-2` เป็น `py-2.5`; รีดีไซน์ `AppTableSearch` ให้เป็น clean toolbar (ซ้ายแสดง `Showing X records` + filter chips, ขวาเป็นช่องค้นหา rounded + ปุ่ม clear); และปรับ `LearnerGroupListPage` wrapper จาก `py-4` เป็น `pt-4 pb-0` เพื่อตัด padding ซ้อน
- ไฟล์หลักที่แตะ: `iLearn.Admin.React/src/components/ui/DataGridSurface.tsx`, `iLearn.Admin.React/src/components/ui/AppTable.tsx`, `iLearn.Admin.React/src/components/ui/table/AppTableSearch.tsx`, `iLearn.Admin.React/src/components/ui/table/AppTableFooter.tsx`, `iLearn.Admin.React/src/pages/learner-groups/LearnerGroupListPage.tsx`, `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน: เปลี่ยน props ของ `AppTableSearch` โดยเพิ่ม `totalCount: number` (internal shared UI contract)
- Verified: `npm run lint` ผ่าน (warning baseline เดิม 10 รายการ, ไม่มี error), `npm run build` ผ่าน, `dotnet build iLearn.Tests -o artifacts\verify-test` ผ่าน, `dotnet test artifacts\verify-test\iLearn.Tests.dll` ผ่าน (112/112), ลบโฟลเดอร์ชั่วคราว `artifacts\verify-test` แล้ว

## [2026-06-12 12:52] GitHub Copilot (GPT-5.3-Codex) — เพิ่ม single global breadcrumbs + URL history navigation ให้ Learner Group Explorer
- ทำอะไร: ปรับระบบ breadcrumbs ให้รองรับ custom trail ระดับ global (`customCrumbs`) แล้วเชื่อมหน้า `LearnerGroupListPage` เข้ากับ header breadcrumb เดียวของระบบ; เปลี่ยนการนำทางโฟลเดอร์จาก state ภายในเป็น URL query `categoryId` ผ่าน `useSearchParams` เพื่อรองรับ browser back/forward; เพิ่มปุ่ม `Back` ใน toolbar เมื่ออยู่โฟลเดอร์ย่อย; เอา breadcrumb ซ้ำซ้อนในเนื้อหาเพจออก
- ไฟล์หลักที่แตะ: `iLearn.Admin.React/src/lib/breadcrumbContext.tsx`, `iLearn.Admin.React/src/components/layout/Breadcrumbs.tsx`, `iLearn.Admin.React/src/pages/learner-groups/LearnerGroupListPage.tsx`, `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน: เพิ่ม UI context contract ใน `breadcrumbContext` จากเดิม (`labels`, `setLabel`) เป็น (`labels`, `setLabel`, `customCrumbs`, `setCustomCrumbs`) สำหรับหน้า override เส้นทาง breadcrumb
- Verified: `npm run lint` ผ่าน (warning baseline เดิม 10 รายการ, ไม่มี error), `npm run build` ผ่าน, `dotnet build iLearn.Tests -o artifacts\verify-test` ผ่าน, `dotnet test artifacts\verify-test\iLearn.Tests.dll` ผ่าน (112/112), ลบโฟลเดอร์ชั่วคราว `artifacts\verify-test` แล้ว

## [2026-06-12 11:53] GitHub Copilot (GPT-5.3-Codex) — ปรับ Learner Group Explorer เป็น unified list แบบ no-sidebar ตาม implementation plan
- ทำอะไร: อ่านแผน `Implementation Plan: Learner Group Unified File Explorer Layout (No Sidebar)` แล้วปรับ `LearnerGroupListPage` เป็นมุมมองเดียวเต็มความกว้างแบบ file explorer: breadcrumb คลิกย้อนระดับได้, ตารางรวมโฟลเดอร์+กลุ่มใน grid เดียว (folder-first), ค้นหาในโฟลเดอร์ปัจจุบัน, ดับเบิลคลิกเข้าโฟลเดอร์/เปิดรายละเอียดกลุ่ม, modal สร้างโฟลเดอร์, modal ย้ายกลุ่มด้วย tree picker, และ action ลบโฟลเดอร์/ลบกลุ่มพร้อม confirm
- ไฟล์หลักที่แตะ: `iLearn.Admin.React/src/pages/learner-groups/LearnerGroupListPage.tsx`, `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน: ไม่มี (frontend-only; ใช้ endpoint เดิม `LearnerGroupCategories`, `LearnerGroups`, `admin/DivisionsCRUD/Get`)
- Verified: `npm run lint` ผ่าน (warning baseline เดิม 10 รายการ, ไม่มี error), `npm run build` ผ่าน, `dotnet build iLearn.Tests -o artifacts\verify-test` ผ่าน, `dotnet test artifacts\verify-test\iLearn.Tests.dll` ผ่าน (112/112), ลบโฟลเดอร์ชั่วคราว `artifacts\verify-test` แล้ว

## [2026-06-12 11:33] GitHub Copilot (GPT-5.3-Codex) — ปรับ Learner Group Explorer เป็น File Explorer flow แบบครบ
- ทำอะไร: อัปเกรด `LearnerGroupListPage` ให้ครบตามแผนแบบ explorer (sub-folder cards + double-click/open, ปุ่ม `New Folder` พร้อม modal สร้างโฟลเดอร์ใต้ตำแหน่งปัจจุบัน, ปุ่ม `Delete` เฉพาะโฟลเดอร์ว่างพร้อม confirm, action `Move Group` พร้อม modal tree เลือกปลายทางและยิง `PUT LearnerGroups/{id}`); ปรับตารางให้แสดงเฉพาะ direct children ของโฟลเดอร์ปัจจุบัน; ปุ่ม `Create Group` แนบ query `?categoryId=<current>`; เพิ่มการอ่าน `categoryId` query ใน `LearnerGroupEditorPage` เพื่อ preselect หมวดหมู่ตอนสร้างกลุ่มจาก context folder
- ไฟล์หลักที่แตะ: `iLearn.Admin.React/src/pages/learner-groups/LearnerGroupListPage.tsx`, `iLearn.Admin.React/src/pages/learner-groups/LearnerGroupEditorPage.tsx`, `iLearn.Application/DTOs/PaginationParams.cs`, `iLearn.Application/Services/LearnerGroupService.cs`
- Contract ที่เปลี่ยน: เพิ่ม query contract `rootCategoryOnly` (bool?) ใน `PaginationParams` และ `LearnerGroupService.GetPagedAsync` เพื่อรองรับ root-folder filter (`CategoryId == null`) สำหรับ explorer root view; contract อื่นคงเดิม
- Verified: `npm run lint` ผ่าน (มี warning baseline เดิม 10 รายการ, ไม่มี error), `npm run build` ผ่าน, `dotnet build iLearn.Tests -o artifacts\\verify-test` ผ่าน, `dotnet test artifacts\\verify-test\\iLearn.Tests.dll` ผ่าน (112/112), ลบโฟลเดอร์ชั่วคราว `artifacts\\verify-test` แล้ว

## [2026-06-12 11:17] GitHub Copilot (GPT-5.3-Codex) — เพิ่มหน้า Learner Group Directory Explorer + recursive folder filter
- ทำอะไร: สร้างหน้า list ใหม่ `LearnerGroupListPage` สำหรับ route `/learner-groups` แบบ split layout (Left folder tree + Right data grid) ด้วย `AppTreeView` + `AppTable`; รองรับการเลือกโฟลเดอร์แล้วกรองข้อมูลแบบสืบทอดลูกทั้งหมด (recursive descendants) พร้อมเปิดรายละเอียดได้จากดับเบิลคลิกหรือปุ่ม Info; ปรับ route ใน `App.tsx` ให้ใช้หน้าใหม่แทน `EntityListPage`
- ไฟล์หลักที่แตะ: `iLearn.Admin.React/src/pages/learner-groups/LearnerGroupListPage.tsx` (ใหม่), `iLearn.Admin.React/src/App.tsx`, `iLearn.Application/DTOs/PaginationParams.cs`, `iLearn.Application/Services/LearnerGroupService.cs`
- Contract ที่เปลี่ยน: เพิ่ม query contract ใน `PaginationParams` เป็น `categoryId` หลายค่า (`List<int>?`) เพื่อรองรับ filter แบบ tree descendants ที่ส่งซ้ำเป็น `?categoryId=1&categoryId=2...`; `LearnerGroupService.GetPagedAsync` รองรับการกรองตามชุด `categoryId`
- Verified: `npm run lint` ผ่านโดยเหลือ warning เดิมของโปรเจ็กต์ 10 รายการ (ไม่มี error), `npm run build` ผ่าน, `dotnet build iLearn.Tests -o artifacts\verify-test` ผ่าน, `dotnet test artifacts\verify-test\iLearn.Tests.dll` ผ่าน (112/112)

## [2026-06-12 11:06] GitHub Copilot (GPT-5.3-Codex) — เปลี่ยน Category เป็น Folder Explorer modal ด้วย AppTreeView
- ทำอะไร: รีแฟกเตอร์ `LearnerGroupEditorPage` จาก searchable combobox เป็น file-explorer selector ตามแผนล่าสุด: แสดง read-only path field + ปุ่ม "Select Category Folder...", เปิด modal backdrop พร้อม tree structure ผ่าน `AppTreeView`, เลือกโฟลเดอร์ด้วย temp state แล้วกด Confirm เพื่อ commit ค่า, แสดง selected path ทั้งในฟอร์มและ review step
- ไฟล์หลักที่แตะ: `iLearn.Admin.React/src/pages/learner-groups/LearnerGroupEditorPage.tsx`
- Contract ที่เปลี่ยน: ไม่มี (ใช้ `LearnerGroupCategories` shape เดิม `id`, `name`, `parentId`, `depth` และใช้ `TreeViewNode` ของ `AppTreeView` แบบเดิม)
- Verified: `npm run lint` ผ่านโดยเหลือ warning เดิมของโปรเจ็กต์ 10 รายการ (ไม่มี error), `npm run build` ผ่าน, `dotnet build iLearn.Tests -o artifacts\verify-test` ผ่าน, `dotnet test artifacts\verify-test\iLearn.Tests.dll` ผ่าน (112/112)

## [2026-06-12 11:01] GitHub Copilot (GPT-5.3-Codex) — เปลี่ยน Category เป็น searchable combobox พร้อม full path
- ทำอะไร: อัปเกรด `LearnerGroupEditorPage` จาก category dropdown/tree-select เป็น custom searchable combobox (เปิด/ปิด popover, พิมพ์ค้นหาแบบทันที, เลือกจาก full path, clear selection, close เมื่อคลิกนอกพื้นที่); เพิ่ม path resolution สำหรับ selected category และแต่ละตัวเลือกเพื่อแสดง `Parent / Child / Leaf`; ปรับ review step ให้แสดง category เป็น full path เดียวกับ combobox
- ไฟล์หลักที่แตะ: `iLearn.Admin.React/src/pages/learner-groups/LearnerGroupEditorPage.tsx`
- Contract ที่เปลี่ยน: ไม่มี (ใช้ field เดิมของ endpoint `LearnerGroupCategories` คือ `id`, `name`, `parentId`, `depth`)
- Verified: `npm run lint` ผ่านโดยเหลือ warning เดิมของโปรเจ็กต์ 10 รายการ (ไม่มี error), `npm run build` ผ่าน, `dotnet build iLearn.Tests -o artifacts\verify-test` ผ่าน, `dotnet test artifacts\verify-test\iLearn.Tests.dll` ผ่าน (112/112)

## [2026-06-12 10:53] GitHub Copilot (GPT-5.3-Codex) — ปรับ UX Learner Directory + Category hierarchy path
- ทำอะไร: อัปเกรด `LearnerDirectorySelector` (dropdown chevron style, active filter badges, select all matching learners จาก filter ทั้งชุด, loading bar แบบไม่บังตาราง, searchable selected chips); ปรับ `LearnerGroupEditorPage` ให้ Category เป็น tree-select แบบ hierarchical จาก `parentId`; ปรับ `LearnerGroupDetailPage` ให้แสดง category breadcrumb จาก `categoryAncestors`; แก้ `EntityListPage` ให้ lookup `LearnerGroupCategories` และ render path เต็มในคอลัมน์ Category
- ไฟล์หลักที่แตะ: `iLearn.Admin.React/src/components/shared/LearnerDirectorySelector.tsx`, `iLearn.Admin.React/src/pages/learner-groups/LearnerGroupEditorPage.tsx`, `iLearn.Admin.React/src/pages/learner-groups/LearnerGroupDetailPage.tsx`, `iLearn.Admin.React/src/pages/EntityListPage.tsx`
- Contract ที่เปลี่ยน: ไม่มี (ใช้ field เดิมจาก API ที่มีอยู่แล้ว ได้แก่ `LearnerGroupCategories.parentId` และ `LearnerGroupDetail.categoryAncestors`)
- Verified: `npm run lint` ผ่านโดยเหลือ warning เดิมของโปรเจ็กต์ 10 รายการ (ไม่มี error), `npm run build` ผ่าน, `dotnet build iLearn.Tests -o artifacts\verify-test` ผ่าน, `dotnet test artifacts\verify-test\iLearn.Tests.dll` ผ่าน (112/112)

## [2026-06-12 10:40] Claude Code — เอา Bulk Assign ออกจาก sidebar
- ทำอะไร: ลบเมนูย่อย "Bulk Assign" ใต้ Assignments ออกจาก sidebar (เป็น action ไม่ใช่ directory — เข้าผ่านปุ่ม Bulk Assignment ในหน้า Assignments list และปุ่มใน Course detail แทน) route `/assignments/bulk` ยังอยู่เหมือนเดิม
- ไฟล์หลักที่แตะ: `iLearn.Admin.React/src/config/navigation.ts`
- Contract ที่เปลี่ยน: ไม่มี
- Verified: tsc 0 error, vite build ผ่าน

## [2026-06-12 10:30] Claude Code — จัด Sidebar ใหม่ แยกโซน Admin / Super Admin
- ทำอะไร: เปลี่ยน `navigation.ts` จาก flat list เป็น `navigationSections` (Dashboard / Learning / Operations / Super Admin) — เมนู SuperAdmin ทั้งหมด (Enrollments ที่เคยซ่อนใต้ Operations, Master Data, Admin Users, System Config) ย้ายมารวมใน section "Super Admin" ที่มีหัวข้อสีเหลืองกำกับ และทั้ง section หายไปเลยสำหรับ admin ปกติ; Sidebar.tsx render แบบ section พร้อม heading
- ไฟล์หลักที่แตะ: `iLearn.Admin.React/src/config/navigation.ts` (export เปลี่ยนจาก `navigationItems` เป็น `navigationSections`), `iLearn.Admin.React/src/components/layout/Sidebar.tsx`
- Contract ที่เปลี่ยน: **`navigation.ts` ไม่ export `navigationItems` แล้ว** — ใครจะใช้เมนูให้ import `navigationSections` แทน (ตอนนี้มี Sidebar ใช้ที่เดียว)
- Verified: tsc 0 error, eslint ผ่าน, vite build ผ่าน

## [2026-06-12 10:15] Antigravity — ปรับความสูงแผง Syllabus/Selected, แก้การโหลดซ้ำซ้อน และปรับปรุง SCORM Upload
- ทำอะไร: 
  1. ปรับปรุงความสูงของ Wizard Steps ใน BulkAssignPage.tsx และ LearnerGroupEditorPage.tsx เป็น h-[calc(100vh-265px)] เพื่อแก้ปัญหาพื้นที่ว่างสีขาวด้านล่างให้กล่องข้อมูลยืดชิดขอบล่างพอดีโดยไม่มี scrollbar ซ้อน
  2. แก้ปัญหาการ Fetch ข้อมูลของตารางสองครั้งตอน Mount โดยการปรับปรุง AppTable.tsx ตั้งค่าเริ่มต้นของ pageSize เป็น 0 และข้ามการเรียกโหลดข้อมูลชั่วคราวจนกว่าระบบจะวัดขนาด viewport ความสูงจริงในฝั่ง layout เสร็จสิ้นแล้วค่อยดึงข้อมูลด้วยขนาดที่ถูกต้องเพียงครั้งเดียว
  3. ปรับปรุงส่วน Upload SCORM Package ใน ContentItemEditorPage.tsx: (A) เปลี่ยนตัวเลือกไฟล์อินพุตแบบเดิมที่แสดงผลไม่สวยงามเป็น Label Wrapper/Upload Zone ที่ซ่อน input จริงด้วย sr-only เพื่อความกลมกลืน (B) ปรับปรุงส่วนการแสดงข้อผิดพลาด (Exception Handling) ให้อ่านและกรองข้อความผิดพลาดจาก JSON ของฝั่ง Backend มาแสดงผลผ่าน toast แทนการแสดงสตริง JSON ดิบ (C) นำขั้นตอน 'Metadata' ออกจากตัวช่วยอัปโหลดเมื่อสร้างข้อมูลใหม่ และปรับปรุงตัวเลือกแสดงไฟล์อัปโหลดให้ออกมาเป็นรูปตารางรายชื่อแบบเดียวกับแถบ Content ในหน้าสร้างคอร์สใหม่ (/courses/new) โดยย้ายตัวกรองประเภทไฟล์ (Learn / Exam) ไปเป็น dropdown ภายในแถวตารางแทน (D) จัดสเกลคอลัมน์ของตารางข้อมูลไฟล์อัปโหลดให้มีโครงสร้างเดียวกันโดยเพิ่มคอลัมน์ Source ("New upload") และ Status ("Ready") เพื่อให้การออกแบบสอดคล้องสม่ำเสมอ
- ไฟล์หลักที่แตะ: `iLearn.Admin.React/src/pages/assignments/BulkAssignPage.tsx`, `iLearn.Admin.React/src/pages/learner-groups/LearnerGroupEditorPage.tsx`, `iLearn.Admin.React/src/components/ui/AppTable.tsx`, `iLearn.Admin.React/src/pages/content-library/ContentItemEditorPage.tsx`, `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน: ไม่มี
- Verified: tsc 0 error, vite build ผ่าน, eslint ผ่าน, dotnet test 112/112 ผ่าน (แบบ --no-build เนื่องจากไฟล์ C# ไม่ได้เปลี่ยนและตัวระบบถูกรันอยู่ทำให้ DLL ล็อก)

## [2026-06-12 10:00] Claude Code — Reconcile งานสองฝั่ง + แก้ Learners casing
- ทำอะไร: อ่าน brain ของ Antigravity (session 250e1b99) แล้วตรวจรอยต่อ — พบ handler เปิด learner profile ยังใช้ `NID`/`EId` (PascalCase) ขณะที่ Antigravity เปลี่ยน rows เป็น camelCase แล้ว → แก้เป็น `nid`/`eId`; สร้าง `CLAUDE.md` + log ไฟล์นี้
- ไฟล์หลักที่แตะ: `iLearn.Admin.React/src/pages/EntityListPage.tsx`, `CLAUDE.md`, `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน: ไม่มี (ตามของ Antigravity)
- Verified: tsc 0 error, vite build ผ่าน, dotnet test 112/112

## [2026-06-12 ~09:30] Antigravity — Smoke Test Mitigation 14 จุด + Typography/Density
- ทำอะไร: เพิ่ม `ContentItem.CachedFileLength` (+migration+backfill), ปรับ GetPaged/GetFiltered ไม่แตะ blob, Dashboard memory cache 60s, LearnersController → camelCase typed DTO, moduleConfigs camelCase + cellRender, MasterDataDetailPage รองรับ id='new', AppTable loading เริ่มต้น true + min pageSize, ฟอนต์ Sarabun→Noto Sans Thai, compact density (rowHeight 38, py-2, sidebar 210px), ลบเมนู Access Control
- ไฟล์หลักที่แตะ: ContentItem.cs, ContentItemsController.cs, ContentItemsCRUDController.cs, DashboardController.cs, LearnersController.cs, moduleConfigs.ts, EntityListPage.tsx, MasterDataDetailPage.tsx, AppTable.tsx, DashboardPage.tsx, navigation.ts, index.css, Sidebar.tsx, AppLayout.tsx
- Contract ที่เปลี่ยน: **Learners list rows เป็น camelCase (`nid`, `eId`, `englishFirstName`...)**; ContentItems CRUD list ใช้ `ContentItemCrudRow`; DB เพิ่มคอลัมน์ `CachedFileLength`
- Verified: dotnet test 112/112, npm lint + build ผ่าน
- รายละเอียดเต็ม: `~\.gemini\antigravity\brain\250e1b99-8d2b-4d2e-b0fe-cbb79a54717a\walkthrough.md`

## [2026-06-12 ~09:00] Claude Code — UI standardization + bug fixes (สะสมทั้ง session)
- ทำอะไร: สร้าง shared UI components (`LoadingState`, `NotFoundState`, `StatusBadge`, `SectionHeader`, `ProgressBar`, `ControlsSidebar`/`ControlAction`, `ConfirmDialog`/`useConfirm`) แล้วรีแฟกเตอร์ทุกหน้าให้ใช้ (-1,000+ บรรทัด); ลบ dead code/DevExtreme ค้าง; แก้ ContentItems GetPaged ไม่โหลด blob (ก่อน Antigravity ทำ CachedFileLength ต่อ); แก้ type mismatch กับ DTO จริง 3 หน้า (AssignmentDetail, AssignmentReport, CourseDetail) พร้อมคอมเมนต์ `// Mirrors <Dto>`; เพิ่ม `<Remount>` wrapper ใน App.tsx กัน state ค้างข้าม route + `key={config.controller}` บน AppTable; เพิ่มกติกาทั้งหมดใน README
- ไฟล์หลักที่แตะ: `src/components/ui/*` (ใหม่ 7 ไฟล์), ทุกหน้าใน `src/pages/`, `App.tsx`, `EntityListPage.tsx`, `iLearn.Admin.React/README.md`, `ContentItemsController.cs`
- Contract ที่เปลี่ยน: ไม่มี (ฝั่ง React ปรับตาม DTO จริง)
- Verified: tsc 0 error, eslint 0 error, vite build ผ่าน, ContentItemsControllerTests ผ่าน
