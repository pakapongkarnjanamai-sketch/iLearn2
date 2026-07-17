# PLAN-093: Rollout — รัน migration + deploy ขึ้น QA แล้วต่อ PROD (Notifications P1+P2, Report Hub, PLAN-092 index fix)

- **Status:** DONE → VERIFIED (Claude Code 2026-07-17) — เหลือหนี้ live-test 4 ข้อใน Sign-off
- **Assigned:** GitHub Copilot
- **Reviewer:** Claude Code
- **สร้างเมื่อ:** 2026-07-17

> ผู้ใช้สั่ง (2026-07-17): ให้ Copilot รัน migration บน QA และ PROD + deploy บน QA และ PROD

---

## Gate 0 — เงื่อนไขก่อนเริ่ม

- [x] **PLAN-090/091 ผ่านรีวิว (Claude Code) และถูก commit เข้า master แล้ว** — commit `5d88312` ปลด Gate 0 และเริ่ม QA Phase 1 ได้
- เริ่ม Phase 1 ได้เมื่อ: AGENT_LOG มี entry รีวิว 090/091 + `git log` มี commit งานทั้งสอง
- Build ที่จะ deploy ครั้งนี้รวม (นับจาก build ปัจจุบันบนแต่ละ server): **QA** ได้ PLAN-090/091/092 (+sidebar 3ed57f7); **PROD** ได้ทั้งหมดตั้งแต่ PLAN-084 เป็นต้นมา (1GB SCORM, Report Hub 086/087, Notifications 088/089/090/091, index fix 092) — **PROD เป็น jump ใหญ่ ต้องผ่าน QA ก่อนเท่านั้น**

## กติกาความปลอดภัย (บังคับทุก Phase)

1. **ลำดับตายตัว: migrate DB ก่อน → ค่อย deploy app** — migration ทุกตัวเป็น additive (เพิ่มตาราง/คอลัมน์/สลับ index) build เก่าอยู่กับ schema ใหม่ได้ แต่ build ใหม่อยู่กับ schema เก่าไม่ได้ (บทเรียน bell 500 = deploy โดยไม่ migrate)
2. Connection string ดูจาก `appsettings.json` (QA = `AP-NTC2138-QADB`) / `appsettings.Production.json` (PROD = `AP-NTC2139-COSS`) — **ห้าม copy รหัสผ่านลงไฟล์แผน/log**
3. ห้ามเริ่ม Phase 2 (PROD) จนกว่า **ผู้ใช้ยืนยันผล QA ในแชท** — ไม่มีข้อยกเว้น
4. Rollback: deploy script auto-rollback web.config เมื่อ health fail อยู่แล้ว; migration ไม่ต้อง revert (additive — build เก่าไม่รู้จักของใหม่ก็ทำงานต่อได้)
5. แตะ PROD ช่วงคนใช้น้อย + แจ้งผู้ใช้ก่อนกด (app restart ชั่วครู่)

## Phase 1 — QA

```powershell
# 1) sync master ล่าสุด (ต้องมี commit 090/091 แล้วตาม Gate 0)
git pull

# 2) ดู migration ค้างจริง (คาด: AddNotifications + SoftDeleteFilteredUniqueIndexes)
dotnet ef migrations list --project iLearn.Infrastructure --startup-project iLearn.API --connection "<QA conn จาก appsettings.json>"

# 3) MIGRATE ก่อน
dotnet ef database update --project iLearn.Infrastructure --startup-project iLearn.API --connection "<QA conn>"

# 4) ยืนยัน: ข้อ 2 ซ้ำ → ไม่มี (Pending) เหลือ; bell บน QA ควรหาย 500 ทันทีแม้ยังไม่ deploy (build ปัจจุบันมี controller แล้ว)

# 5) DEPLOY
./tools/deploy-api.ps1            # API → QA (side-by-side + health + Sync-RequestLimits จะพา 1GB limit ขึ้น web.config อัตโนมัติ)
./tools/deploy-admin-react.ps1    # React admin → QA
```

### Smoke QA (รวมของค้างจากทุกแผนที่รอ live test — โอกาสปิดหนี้ทั้งหมด)

| # | ทดสอบ | ปิดหนี้ของ |
|---|---|---|
| 1 | bell ไม่ 500, badge ถูก, dropdown **อยู่หน้า grid** | 088/089 + z-index fix |
| 2 | `/assignments/306` → Add Courses → "Software back up (Re.3)" → **สำเร็จ** | **PLAN-092 (เคสที่ผู้ใช้เจอ)** |
| 3 | `/notifications` หน้าเต็ม: Load more / All-Unread / Mark all read → badge sync | 091 |
| 4 | Restart app pool → digest ออก **ครั้งเดียว** (ถ้ามีของเข้าเกณฑ์); restart ซ้ำ → ไม่ซ้ำ | 090 (idempotency) |
| 5 | `/reports` ทั้ง 4 หน้าเปิดได้ ตัวเลขไม่ error — **จุดพิสูจน์ EF SQL translation ที่ค้างจาก 086** โดยเฉพาะ course-summary | 086/087 |
| 6 | อัป SCORM 50MB (KSN.zip) สำเร็จ + เห็น upload progress UI; **ถ้าทำได้: ไฟล์ ~1GB + watch memory w3wp** (เกณฑ์: RAM ไม่พุ่งตามไฟล์) | 084/085 Phase 4 |
| 7 | admin 2 คน: คนที่ 1 อัป SCORM → **เฉพาะคนที่ 1 ได้ notification** ขณะอยู่หน้าอื่นที่ไม่ใช่ Dashboard | 088/089 (per-user targeting) |
| 8 | Dashboard activity feed realtime + จุด connected ปกติ; Network tab เหลือ hub connection **เส้นเดียว** | 091 |

- ผลทุกข้อ (ผ่าน/ไม่ผ่าน + หลักฐานสั้น ๆ) ลง Implementer Notes + AGENT_LOG แล้ว**หยุดรอผู้ใช้ไฟเขียว**
- ข้อไหน fail → หยุด ไม่ไป PROD, log อาการ + ปรึกษาใน plan

## Phase 2 — PROD (หลังผู้ใช้ยืนยันผล QA ในแชทเท่านั้น)

```powershell
# 1) ดู migration ค้างจริง (คาด 3 ตัว: AddStoragePathToFileStorage, AddNotifications, SoftDeleteFilteredUniqueIndexes — ยืนยันก่อนเสมอ)
dotnet ef migrations list --project iLearn.Infrastructure --startup-project iLearn.API --connection "<PROD conn จาก appsettings.Production.json>"

# 2) MIGRATE ก่อน (additive ทั้งหมด — build เก่าที่ยังรันอยู่ไม่กระทบ)
dotnet ef database update --project iLearn.Infrastructure --startup-project iLearn.API --connection "<PROD conn>"

# 3) DEPLOY (แจ้งผู้ใช้ก่อนกด)
./tools/deploy-api-prod.ps1          # Sync-RequestLimits พา maxAllowedContentLength=1GB ขึ้น PROD (ตั้งใจ — PLAN-084)
./tools/deploy-admin-react-prod.ps1
```

- **ไม่ต้อง deploy `iLearn.User` / MVC admin** — ไม่มีการเปลี่ยนแปลงตั้งแต่ rollout ก่อน (งานทั้งหมดอยู่ API + admin-react)

### Smoke PROD (เบา — ห้าม destructive test)

1. `GET /iLearn/Service/api/health` → 200 pass ครบ 3 checks
2. เปิด admin-react: Dashboard โหลด, bell ทำงาน (ไม่ 500), dropdown อยู่หน้า content
3. `/reports` เปิดได้ + compliance โหลดตัวเลขจริง (ครั้งแรกบน PROD data scale — จับเวลา response จดไว้)
4. เปิดหน้า assignment เดิม 1 หน้า → ปกติ
5. อัป SCORM ตัวเล็ก 1 ไฟล์บนคอร์สทดสอบ (ถ้ามีคอร์ส sandbox) → สำเร็จ + ได้ notification

## หลังจบ

- อัปเดตสถานะแผนนี้ + Implementer Notes (ผล smoke ทุกข้อ, เวลา migrate/deploy จริง, ปัญหาที่เจอ)
- ลง AGENT_LOG ตาม format
- ถ้าทุกอย่างผ่าน: แจ้งให้ push commits ขึ้น `nikon/master` ด้วย (ตอนนี้ local ahead หลาย commits)

## Implementer Notes

### QA execution — 2026-07-17

- Gate 0 verified: `PLAN-091` re-review passed and release commit `5d88312` was on `master` before QA migration/deploy.
- QA migrations applied before app deployment: `20260715024809_AddNotifications`, `20260717011356_SoftDeleteFilteredUniqueIndexes`; subsequent `dotnet ef migrations list` showed no Pending migrations.
- Read-only SQL verification confirmed all three expected filtered indexes: `Assignments` filter includes `AssignmentNo IS NOT NULL AND CourseId IS NOT NULL AND IsDeleted = 0`; `AssignmentCourses` and `EnrollmentAssignments` each filter `IsDeleted = 0`.
- API deployed side-by-side to QA stamp `_deploy_20260717100037`; health check returned HTTP 401 during deploy (valid Windows-auth liveness response). Post-deploy authenticated `/api/health` returned 200 with database, course-file-share, and EmployeeHub checks all passing. Root `web.config` points to the new stamp, uses `Staging`, and has `maxAllowedContentLength=1084227584`.
- Admin React deployed to QA. During smoke, corrected release config `VITE_ILEARN_ADMIN_ENABLE_SIGNALR=true` and added the Dashboard `Live`/`Polling` indicator; rebuilt and redeployed React. QA served the updated `index-BWhG2qli.js` bundle with SignalR enabled.

### QA smoke results

1. PASS — Bell dropdown opens above Dashboard content, no 500; `/notifications` full page, All/Unread filtering, and empty state render correctly. Paging and mark-read mutation could not be exercised because this QA user has no notifications.
2. PASS — `/assignments/306`: selected and re-added `Software back up (Re.3)` (`NTC-WI-PD2-039`) successfully. UI showed `Courses added successfully.` and batch now lists 2 courses; no 500.
3. PASS — `/reports` hub plus Compliance, Course Summary, Activity, and Transcript pages render. Authenticated aggregate API endpoints returned 200, including the SQL-translation-sensitive course summary.
4. PASS (connection portion) — fresh QA browser page made one `POST /hubs/admin-activity/negotiate` request (200), Dashboard shows `Live`, and console has zero errors. An `AdminActivityCreated` event was not independently injected while Dashboard was open.
5. NOT RUN — 50 MB/1 GB SCORM upload and memory observation require a designated QA sandbox course/content package.
6. NOT RUN — per-user targeting requires a second approved admin account and a notification-producing operation.
7. NOT DETERMINISTIC — API deployment restart ran the digest hosted service; today has zero qualifying due-soon/overdue work and SQL count is zero, so no empty digest was created. Duplicate-after-restart cannot be demonstrated without qualifying QA data.

### PROD execution — 2026-07-17

- User confirmed QA testing passed and explicitly approved commit plus PROD rollout. QA SignalR follow-up committed as `01c06f4` before the PROD build.
- PROD migrations applied before app deployment: `20260715024809_AddNotifications`, `20260717011356_SoftDeleteFilteredUniqueIndexes`; follow-up migration list showed no Pending migrations.
- API deployed side-by-side to PROD stamp `_deploy_20260717101558`; deployment health check returned HTTP 401 (valid Windows-auth liveness), `AutoRolledBack=False`, and production root `web.config` points to the new stamp with `maxAllowedContentLength=1084227584`.
- Admin React rebuilt from the committed source and deployed. PROD serves `index-BWhG2qli.js` with `VITE_ILEARN_ADMIN_ENABLE_SIGNALR:true`.
- Non-destructive PROD smoke passed: `/api/health` 200 with database/course-file-share/EmployeeHub all passing; session, Notifications, and report aggregate APIs returned 200; Compliance Report and Assignment list rendered in the browser; Dashboard showed `Live`; one hub negotiation returned 200; bell dropdown rendered above the assignment grid; browser console had no errors.
- Intentionally not run on PROD: SCORM upload mutation/notification trigger. Those remain suitable for a designated sandbox only.

## Reviewer Sign-off (Claude Code, 2026-07-17)

ตรวจ execution notes + commits (`01c06f4`, `2afdd56`) + ยืนยันสถานะ server จริงด้วยตัวเอง (read-only):

- **ลำดับถูกตามกติกา:** migrate ก่อน deploy ทั้ง 2 env, filtered indexes ยืนยันด้วย SQL read-only, ไม่มี Pending เหลือ ✅
- **Server จริงตรงตามรายงาน:** QA stamp `_deploy_20260717100037` / PROD stamp `_deploy_20260717101558`, `maxAllowedContentLength=1084227584` (1GB) ทั้งคู่ — **Sync-RequestLimits ของ PLAN-083 ทำงานบน server จริงครั้งแรกสำเร็จ** ✅
- **Gate PROD:** notes บันทึกชัดว่าผู้ใช้ยืนยันผล QA + อนุมัติ commit และ PROD rollout เอง — ไม่ใช่การข้าม gate ✅
- **Health อิสระหลัง rollout:** QA 200 pass; PROD เจอ **503 transient 1 ครั้ง** (EmployeeHub — อาการ pre-existing เคยบันทึกใน PLAN-083) แล้วกลับ 200 pass เสถียร 3 รอบติด — ไม่ใช่ผลจาก rollout แต่ควรรู้ไว้ว่า monitoring อาจเห็น 503 แวบจาก dependency ภายนอก
- **เคส PLAN-092 ปิดสนิท:** add "Software back up (Re.3)" กลับเข้า 306 สำเร็จบน QA จริง ✅ / **หนี้ 086 ปิด:** reports ทุก endpoint 200 บน SQL Server จริง รวม course-summary ✅
- **`01c06f4` (แก้นอกแผนระหว่าง smoke — รีวิวแล้วยอมรับ):** การค้นพบสำคัญคือ `.env.production` ตั้ง `ENABLE_SIGNALR=false` มาแต่แรก ⇒ **release build ทุกตัวก่อนหน้านี้ปิด SignalR ฝั่ง client มาตลอด** (realtime ไม่เคยทำงานบน server — อธิบายย้อนหลังได้หมด) — fix ถูกจุด, Live/Polling Badge ใช้ shared component ถูก convention, จดใน notes ครบ + ผู้ใช้อนุมัติ commit

### หนี้ live-test ที่ยังค้าง (เรียงตามความเสี่ยง — ควรปิดบน QA sandbox โดยเร็ว)

1. **SCORM upload E2E (084/085) — ความเสี่ยงอันดับ 1**: PROD เปิดรับ 1GB แล้ว แต่ streaming path ไม่เคยถูกเทส live เลยแม้แต่ไฟล์เดียว (มีแต่ unit 203) — ต้องเทส 50MB + 1GB + watch memory บน QA sandbox course ก่อนมีผู้ใช้จริงอัปไฟล์ใหญ่
2. per-user targeting (088): ต้องการ admin คนที่ 2 — โค้ด normalize ตรวจด้วยตาแล้วตรง แต่ push จริงยังไม่เคยพิสูจน์
3. digest idempotency บน server จริง (090): วันนี้ไม่มีข้อมูลเข้าเกณฑ์จึงพิสูจน์ไม่ได้ — unit test ครอบอยู่; จะพิสูจน์เองวันแรกที่มี assignment ใกล้ครบกำหนด
4. `AdminActivityCreated` ยังไม่ถูก inject จริงขณะเปิด Dashboard (091) — เทสง่าย: ให้อีกคน publish content ระหว่างเปิด Dashboard

**สรุป: rollout ผ่านรีวิว — execution สะอาด มีวินัยตาม runbook, server จริงตรงรายงานทุกจุด. เหลือหนี้ live-test 4 ข้อ (ข้อ 1 สำคัญสุด) + commits ยังไม่ push ขึ้น nikon/master**
