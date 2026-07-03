# PLAN-045: Production Cutover — iLearn2 แทนที่ระบบเดิม iLearn

- **Status:** DRAFT (รอค่า config prod + old-schema discovery ก่อนลงมือ)
- **Owner (planning):** Claude Code
- **Execution:** ผู้ใช้ + implementer (งาน ETL/data-map เหมาะกับ GPT/Gemini, ส่วน deploy/verify ทำร่วมกัน)
- **สร้างเมื่อ:** 2026-07-01
- **ประเภท:** Production deployment runbook (ไม่ใช่ feature plan) — high risk, มี downtime, ย้ายข้อมูลจริง

> ⚠️ นี่คือการ **แทนที่ระบบ production ที่ใช้งานจริงในที่เดิม พร้อมย้ายข้อมูล** ห้ามเริ่ม Phase 5+ จนกว่าจะปิด Phase 0–4 ครบและมี maintenance window ที่ตกลงแล้ว

---

## Objective

ยกระบบ iLearn2 (ใหม่: .NET 9 Clean Architecture API + React/MVC admin + learner portal) ขึ้น production เครื่อง `ap-ntc2137-prwb` **แทนที่ระบบเดิม `iLearn` ในที่เดิม** (URL `/iLearn/student`, `/iLearn/admin` คงเดิม) พร้อม **ย้ายข้อมูลจากฐานข้อมูลเก่า `AP-NTC2139-COSS/iLearn` เข้า schema ใหม่**

---

## Decisions (ล็อคแล้ว — 2026-07-01)

| # | ประเด็น | คำตอบ |
|---|---|---|
| 1 | Database | **สร้าง DB ใหม่ + ย้ายข้อมูลเก่า** (ETL `iLearn` เดิม → schema ใหม่) — **scope = เฉพาะสื่อการเรียน/catalog** (Categories, Courses, CourseResources, Resources, FileStorage); ไม่ย้าย learner/enrollment/history/admin/Divisions (ดู [data-mapping](PLAN-045-data-mapping.md)) |
| 2 | Cutover | **แทนที่ในที่เดิม** ที่ `/iLearn` (ไม่ใช่วางคู่ /iLearnNew) |
| 3 | Admin | **deploy ทั้งคู่** — React (SPA) + MVC |
| 4 | Config prod | **ผู้ใช้จะบอกค่าจริง** → ดู "ค่าที่ต้องได้จากผู้ใช้" |

---

## ผลการสำรวจ repo (ข้อเท็จจริง — ฐานของแผนนี้)

1. **Deploy scripts ปัจจุบันชี้ QA ทั้งหมด** — `tools/deploy-*.ps1` hardcode `AP-NTC2138-QAWB` + share `\\10.10.143.39\wwwroot\iLearnNew`. Prod เป็นคนละ host/share/app-pool → ต้องมีชุดพารามิเตอร์ prod แยก (ผ่าน param ที่ script รองรับอยู่แล้ว: `-DeployRoot`, `-IisHost`, `-AppPoolName`, `-HealthCheckUrl`)
2. **ไม่มี `appsettings.Production.json`** ⚠️ — `appsettings.json` ปัจจุบันคือค่า QA (DB `iLearnDB_New@10.10.143.37`, share iLearnNew, HostUrl QA). และ **deploy script ก็อป `appsettings*.json` ทับ root ปลายทางเสมอ** (backup ไป `_deploy_*/_prev-root-config` ก่อนทับ) → ถ้า publish ด้วยค่า QA แล้ว deploy ขึ้น prod = prod จะวิ่งชน DB/ไฟล์ QA ทันที → **ต้องเตรียมค่า prod ให้ถูกก่อน publish**
3. **ไม่มี auto-migrate ในโค้ด** — `iLearn.API/Program.cs` ไม่มี `db.Database.Migrate()` → migrate DB เป็นขั้นตอน manual แยก (ปลอดภัยกว่าแต่ต้องทำเอง)
4. **ไม่มี migration `InitialCreate`** ⚠️ — migration แรกสุด (`AddSoftDeleteToBaseEntity`) เป็นการ *เพิ่มคอลัมน์เข้าตารางที่มีอยู่แล้ว* → **รัน `dotnet ef database update` บน DB เปล่าไม่ได้ มันจะ error** ต้องสร้าง baseline schema ก่อน (script schema ออกจาก QA `iLearnDB_New` หรือ generate จาก model) แล้วค่อย mark migrations ว่า applied
5. **ไม่มี `UsePathBase` ในโค้ด** — prefix path มาจาก IIS virtual directory ล้วน → ย้าย `/iLearnNew` → `/iLearn` เป็นเรื่อง IIS config + แก้ URL ใน config **ไม่ต้องแก้โค้ด**
6. **มี 4 web component** (ไม่ใช่ 3): `iLearn.API`→`/Service`, `iLearn.User`→student portal, `iLearn.Admin` MVC→`/admin`, `iLearn.Admin.React` SPA→`/admin-react`. **React admin ไม่มี deploy script** — ต้อง `npm run build` (ตั้ง `VITE_ILEARN_ADMIN_APP_BASE_PATH` ให้ตรง path prod ก่อน) แล้ว copy `dist` เอง + มี `public/web.config` สำหรับ SPA fallback
7. **โครงสร้าง secret validation** — `Program.cs` บังคับต้องมี `ConnectionStrings:DefaultConnection` และ `LearnerProxyAuth:SharedSecret` เป็นค่าจริงตอน boot ไม่งั้นแอปไม่ขึ้น
8. **ข้อดีที่มีอยู่** — deploy script เป็น side-by-side + `app_offline.htm` drain + health-check + auto-rollback + `-Rollback` flip web.config (ฐาน rollback ดีมากอยู่แล้ว)
9. **schema ใหม่ 21 ตาราง:** AdminActivities, Assignments, AssignmentCourses, Categories, ContentItems, Courses, CourseContentItems, CourseTypes, CourseVersions, Divisions, Enrollments, EnrollmentAssignments, FileStorages, LearnerGroups, LearnerGroupCategories, LearnerGroupMembers, LearningLogs, Roles, ScormRuntimeStates, Users, UserRoles
10. **โค้ดเก่าเข้าถึงได้** — `C:\Users\n4734\source\repos\iLearn\iLearn` (มี `iLearn`, `iLearnAdmin`, `iLearnAuth`, `iLearnService`, `iLearn.sln`) → วิเคราะห์ schema เก่าเพื่อทำ ETL ได้จริง
11. **เนื้อหา SCORM เก็บ 2 ที่** — `FileStorages.Data` เป็น `byte[]` ใน DB **และ** ไฟล์ course บน share (`FileSettings:HostUnc\Courses`) → การย้ายข้อมูลต้องย้าย **ทั้ง DB และ folder Courses บน share**

---

## ค่าที่ต้องได้จากผู้ใช้ (คุณเลือก "บอกค่าตอนนี้เลย")

กรอกค่าจริงให้ครบก่อนเริ่ม Phase 1 (ค่า secret ไม่ commit ลง repo — ใส่บนไฟล์ปลายทาง prod เท่านั้น หรือผ่าน environment variable)

| Key | ใช้ที่ | ค่า QA ปัจจุบัน (อ้างอิง) | ค่า Prod ที่ต้องการ |
|---|---|---|---|
| Prod SQL server | connection string | `10.10.143.37` | ✅ `10.10.154.119` (= AP-NTC2139-COSS) |
| Prod DB name (ใหม่) | connection string | `iLearnDB_New` | ⚠️ **รอยืนยัน** — เสนอ `iLearnDB_New` (ห้ามใช้ `iLearn` = DB เก่า!) |
| SQL auth (prod) | connection string | `sa` / รหัส… | ✅ `sa` + password (provided) |
| Old DB (ETL source) | data migration | — | ✅ `10.10.154.119 / iLearn` (เก่า, **อ่านอย่างเดียว** ห้ามเขียน/migrate) |
| Prod file share UNC | `FileSettings:HostUnc` | `\\10.10.143.39\wwwroot\iLearnNew` | ⟨UNC ของ prod เช่น `\\<ip>\wwwroot\iLearn`⟩ |
| Prod HostUrl | `FileSettings:HostUrl` | `https://ap-ntc2138-qawb/iLearnNew` | `https://ap-ntc2137-prwb/iLearn` (ยืนยัน) |
| Old content share | file migration | — | ✅ `\\ap-ntc2137-prwb\wwwroot\iLearn\course` (จาก `PathConst.cs`) |
| API BaseUrl | Admin/User `ApiSettings:BaseUrl` | `…/iLearnNew/Service/api` | `https://ap-ntc2137-prwb/iLearn/Service/api` |
| EmployeeService (CSV) | `EmployeeServiceSettings:BaseEmployeeCsvUrl` | ชี้ QA | ⟨URL prod⟩ |
| EmployeeService (lookup/student) | `EmployeeServiceSettings` | ชี้ PRWB อยู่แล้ว | ยืนยันว่าใช้ค่าเดิมได้ |
| LearnerProxyAuth SharedSecret | User↔API | (secret QA) | ⟨สร้าง secret ใหม่สำหรับ prod — ต้องตรงกันทั้ง User+API⟩ |
| IIS app pool names (prod) | offline strategy/verify | `iLearnNew.*` | ✅ **prod ใช้ `iLearn.*`** (เลิก iLearnNew): `iLearn.Service` / `iLearn.User` / `iLearn.Admin` (No Managed Code, Integrated, AppPoolIdentity) |
| DB (ช่วงแรก) | connection | — | ✅ ช่วงแรก prod app ต่อ **QA `iLearnDB_New`** (10.10.143.37) — ไม่แก้ conn string; ย้าย prod DB ทีหลัง. **freeze iLearnDB_New อย่าให้ ETL/ล้างซ้ำ** |
| Deploy account | เขียน prod share | — | ⟨บัญชีที่มีสิทธิ์เขียน prod share⟩ |
| Maintenance window | cutover | — | ⟨วัน/เวลา + ระยะ downtime ที่ยอมรับได้⟩ |

**IIS layout เป้าหมายบน prod** (เพื่อคง URL เดิม — โค้ดไม่มี UsePathBase จึงพึ่ง virtual dir):

| Component | Prod path | หมายเหตุ |
|---|---|---|
| iLearn.User (student) | `/iLearn/student` | คง URL เดิมของผู้เรียน |
| iLearn.Admin (MVC) | `/iLearn/admin` | คง URL เดิมของแอดมิน |
| iLearn.Admin.React (SPA) | `/iLearn/admin-react` | ของใหม่ (build ด้วย base path นี้) |
| iLearn.API | `/iLearn/Service` | backend |

> ❓ **ต้องยืนยัน:** student อยู่ที่ `/iLearn/student` หรือย้ายเป็น `/iLearn` (root)? — แผนตั้งสมมติฐาน `/iLearn/student` เพื่อให้ URL เดิมไม่พัง

---

## Phases

### Phase 0 — Pre-flight (ปิดให้ครบก่อนแตะ prod)
- [ ] เก็บค่าทุกช่องในตาราง "ค่าที่ต้องได้จากผู้ใช้" ครบ
- [ ] ยืนยัน maintenance window + แจ้งผู้ใช้งาน
- [ ] ยืนยันสิทธิ์: เขียน prod share, สร้าง DB บน `AP-NTC2139-COSS`, อ่าน old DB `iLearn`, (ถ้าใช้) IIS admin บน prod host
- [ ] **Full backup** old DB `iLearn` (`.bak`) + snapshot old content share ก่อนแตะอะไร
- [ ] `git status` clean + build ผ่านทั้ง 4 โปรเจกต์ (คำสั่งใน CLAUDE.md)

### Phase 1 — เตรียม Production configuration
- [ ] สร้าง `appsettings.Production.json` ให้ `iLearn.API`, `iLearn.User`, `iLearn.Admin` (ค่า prod จากตารางด้านบน) — **หรือ** เตรียมไฟล์ค่า prod ที่จะวางบน root share หลัง deploy (ดู Phase 6 เรื่อง script ก็อปทับ)
- [ ] ตัดสินใจกลไกจ่ายค่า: (ก) `ASPNETCORE_ENVIRONMENT=Production` + `appsettings.Production.json` publish ไปด้วย, หรือ (ข) แก้ root `appsettings.json` บน prod share โดยตรง + กัน deploy script ทับ. **ข้อควรระวัง:** deploy script sync `appsettings*.json` จาก publish → prod root เสมอ; ถ้าเลือก (ข) ต้องปรับ flow ไม่ให้ทับ (ดู Risk R2)
- [ ] React admin: สร้าง `.env.production` ตั้ง `VITE_ILEARN_ADMIN_APP_BASE_PATH=/iLearn/admin-react/`, `VITE_ILEARN_ADMIN_API_BASE_URL=/iLearn/Service/api` (+ SignalR ถ้าเปิด)
- [ ] สร้าง prod deploy wrapper (คัดลอกจาก `tools/deploy-*.ps1` เป็น `deploy-*-prod.ps1` หรือ pass param prod) — อย่าแก้ default ของ QA script

### Phase 2 — Provision Production database (schema เปล่า)
- [ ] สร้าง DB ใหม่บน `AP-NTC2139-COSS`
- [ ] สร้าง baseline schema (เพราะไม่มี InitialCreate): **แนะนำ** — script schema-only ออกจาก QA `iLearnDB_New` (SSMS Generate Scripts / `mssql-scripter`) แล้ว apply บน prod DB ใหม่
- [ ] เติมตาราง `__EFMigrationsHistory` ให้มี migration ทุกตัวถึง HEAD (`20260612020041_AddCachedFileLengthToContentItem`) เพื่อให้ EF ถือว่า schema ตรง HEAD แล้ว (ไม่รัน migration ซ้ำ)
- [ ] ยืนยัน `dotnet ef migrations list` เทียบกับ prod = ตรงกัน / ไม่มี pending
- [ ] seed master data ขั้นต่ำ (CourseTypes เริ่มต้น ฯลฯ) ถ้า ETL ไม่ครอบคลุม

### Phase 3 — Data migration ETL (old `iLearn` → schema ใหม่) — scope = catalog เท่านั้น
> 📄 **Mapping ระดับตาราง/คอลัมน์ทำแล้ว → [PLAN-045-data-mapping.md](PLAN-045-data-mapping.md)** (5 ตารางในสโคป → 6 ปลายทาง + decisions D0–D4 + ลำดับ ETL)
- [x] **Discovery:** วิเคราะห์ schema เก่า (`iLearnService` EF Core 8) → mapping เสร็จ. **สโคปลดเหลือเฉพาะสื่อการเรียน** (Categories, Courses, CourseResources, Resources, FileStorage) → ตัด 🔴 Enrollments/LearningLogs/admin ออกหมด
- [ ] dump DB จริง (`AP-NTC2139-COSS/iLearn`) schema+row counts เทียบ snapshot; dump Divisions ทั้งสอง DB → สร้าง crosswalk (D0)
- [ ] ปิด D0–D4 (division crosswalk, course category/type/status default, expired date, empty version)
- [ ] เขียนสคริปต์ ETL ตามลำดับ FK-safe (Categories→FileStorages→ContentItems→Courses→CourseVersions→CourseContentItems) ด้วย `IDENTITY_INSERT` คง Id เดิม
- [ ] จุดที่ต้องระวัง: **FileStorages.Data (byte[] SCORM zip)** batch กัน timeout, สร้าง **CourseVersion v1** ต่อคอร์ส, map `Category.DivisionId` ผ่าน crosswalk
- [ ] **Dry-run ETL บน DB ทดสอบ** + reconciliation (count ต้นทาง↔ปลายทาง) + **เปิด SCORM course ตัวอย่างเล่นได้จริง**

### Phase 4 — Content/file migration ⚠️ (ETL อย่างเดียวเล่น SCORM ไม่ได้)
> **สำคัญ:** แอปเสิร์ฟ SCORM จาก **ไฟล์ที่แตกไว้บน share** ไม่ใช่จาก `FileStorage.Data` (byte[]) ใน DB — `ScormService.ExtractAndParseScorm` แตกตอน **upload/publish** เท่านั้น, ตอนเล่น `GetScormUrl` แค่ชี้ URL `{HostUrl}/Courses/{ContentItem.URL}/{LaunchHref}`
> - **เก่า:** ไฟล์อยู่ `\\ap-ntc2137-prwb\wwwroot\iLearn\course\{URL}\...` (โฟลเดอร์ `course` เอกพจน์, จาก `PathConst.cs`)
> - **ใหม่:** `{HostUnc}\Courses\{URL}\...` (โฟลเดอร์ `Courses` พหูพจน์)

**2 ทางเลือกทำให้เล่นได้:**
- **A. Copy ไฟล์** — copy `…\iLearn\course\{URL}\*` → `…\Courses\{URL}\*` (ETL คง `URL`+`LaunchHref` ให้แล้ว → subfolder/launch ตรง เล่นได้ทันที). ตรงไปตรงมา, เทสต์ copy แค่ตัวอย่างก็พอ
- **B. Re-publish** — migrate ContentItem เป็น `IsActive=0` แล้วสั่ง **Publish** ในแอป → `ContentPublicationService.PublishAsync` อ่าน byte[] จาก DB แล้ว re-extract ไป share เอง (ไม่ต้อง copy) — **แต่** ต้อง (i) `Name` ลงท้าย `.zip`, (ii) มี bulk-publish (ตอนนี้เห็นแต่ publish ทีละตัว), (iii) มันจะ **สร้าง `URL`/folder ใหม่เป็น GUID + เขียนทับ LaunchHref**

- [x] **เลือก B (Re-publish)** (ยืนยัน 2026-07-01) — ETL migrate ContentItem เป็น `IsActive=0` แล้ว bulk publish
- [x] **bulk publish มีจริง** — `POST ContentItems/Admin/BulkSetPublic` (ทั้งหมด, streaming+progress) หรือ `Admin/BatchPublishStream` (ตาม ids); publish ทีละตัวจริงคือ `Admin/Publish?key=`
- [ ] **⚠️ .zip Name gate** — `PublishAsync` extract เฉพาะ `Name` ที่ลงท้าย `.zip` (ไม่งั้น set IsActive=true แต่ไม่แตกไฟล์ = เล่นไม่ได้). ETL ใส่ guard เติม `.zip` ให้ zip-backed แล้ว — **ยืนยันด้วย query เช็คชื่อไฟล์เก่าก่อน** (ดู data-mapping §3.5)
- [ ] flow: รัน ETL (IsActive=0) → เรียก BulkSetPublic → แอป re-extract SCORM ไป share (GUID folder ใหม่ + เขียน URL/LaunchHref ใหม่) → ทดสอบเปิด player
- [ ] หมายเหตุ: publish ~1400 ตัว = re-extract zip ทุกก้อน (หนัก/ใช้เวลา) ใช้ streaming endpoint ดู progress
- [x] **✅ พิสูจน์บน QA แล้ว (2026-07-01):** ETL+merge+republish ครบ loop, เล่นได้ (รอ E2E play ยืนยันสุดท้าย)
- [ ] **storage แยกไดร์ฟ (pattern ที่ต้องทำซ้ำบน prod):** เก็บไฟล์ที่แตกบนไดร์ฟข้อมูล (เลี่ยงเต็ม C:) ผ่าน **IIS Virtual Directory** `Courses` → physical บนไดร์ฟใหญ่ + `FileSettings:HostUnc` = path ไดร์ฟนั้น + สิทธิ์ **`IIS_IUSRS` = Modify** (QA ใช้ `D:\iLearnContent`)

### Phase 5 — Build & publish artifacts (ด้วยค่า prod)
- [ ] `dotnet publish -c Release` แต่ละตัว (API, User, Admin) โดยค่า config = prod
- [ ] React: `npm run lint` + `npm run build` (env production) → ได้ `dist` ที่ base path `/iLearn/admin-react/`
- [ ] ตรวจ published `web.config` ของ API มี `requestLimits maxAllowedContentLength` (PLAN-041) ครบ

### Phase 6 — เตรียม IIS บน prod (`ap-ntc2137-prwb`)
- [ ] **Park ระบบเก่า** เพื่อ rollback: rename/สำรอง physical folder + web.config ของ `/iLearn` เดิม (อย่าลบ)
- [ ] สร้าง/ปรับ IIS application + app pool สำหรับ 4 path (`/iLearn/student`, `/iLearn/admin`, `/iLearn/admin-react`, `/iLearn/Service`) — .NET 9 hosting bundle ติดตั้งบน prod host
- [ ] วาง `web.config` (ANCM) ที่ deploy root แต่ละตัว ให้ side-by-side script flip ได้
- [ ] ยืนยัน prod share เขียนได้จากเครื่อง deploy

### Phase 7 — Cutover execution (ใน maintenance window)
- [ ] ประกาศ downtime / วาง app_offline
- [ ] **Backup ซ้ำ** (old DB + old files + สถานะ IIS ปัจจุบัน) จุดสุดท้ายก่อนตัด
- [ ] รัน ETL รอบจริง (Phase 3) เข้าฐาน prod ใหม่ + content copy (Phase 4)
- [ ] Deploy 4 component ด้วย prod wrapper (side-by-side): API → Service → Admin(MVC) → Admin(React) → User; ใช้ `-WhatIf` ก่อนทุกตัว
- [ ] ชี้ IIS `/iLearn/*` ไปแอปใหม่ (flip web.config / virtual dir)

### Phase 8 — Verify
- [ ] Smoke: `GET /iLearn/Service/api/admin/session/me` (คาด 200/401 = up), หน้า student `/iLearn/student`, admin `/iLearn/admin`, react `/iLearn/admin-react`
- [ ] ทดสอบ flow จริง: learner login เห็นคอร์ส+ประวัติ (ยืนยัน ETL), เปิด SCORM เล่นได้, admin สร้าง/แก้คอร์ส + upload SCORM (ยืนยัน PLAN-041/413), รายงาน export
- [ ] ตรวจ console + server log ไม่มี error config/DB

### Phase 9 — Rollback plan
- **App:** flip web.config กลับ folder เดิม (`tools/deploy-*.ps1 -Rollback`) หรือชี้ IIS กลับ physical folder ระบบเก่าที่ park ไว้ (Phase 6)
- **DB:** ระบบเก่าใช้ DB เก่า `iLearn` (แยกคนละฐานกับ prod ใหม่) → rollback = ชี้ IIS กลับแอปเก่าที่ยังต่อ `iLearn` เดิม; **DB ใหม่ไม่แตะ DB เก่า** (ETL เป็น read-only ต่อ source) จึง rollback ปลอดภัย ตราบใดที่ไม่ลบ/แก้ old DB
- **เงื่อนไข rollback:** smoke fail, ETL reconciliation ไม่ผ่าน, หรือ error กระทบผู้ใช้ภายใน ⟨เกณฑ์เวลา⟩ นาที
- [ ] ซ้อม rollback (flip กลับ-กลับ) ก่อน window จริง

### Phase 10 — Post-deploy
- [ ] เก็บ stamp/DeployPath/WebConfigArguments ทุกตัว (ตาม DEPLOY-CHECKLIST §4)
- [ ] เฝ้าดู log 24–48 ชม. ก่อนลบ folder เก่า/DB เก่า
- [ ] อัปเดต `DOC/DEPLOY-CHECKLIST.md` ให้มี target prod (ตอนนี้เขียนเฉพาะ QA)
- [ ] ลง `DOC/AGENT_LOG.md`

---

## Risk Register

| ID | ความเสี่ยง | ผลกระทบ | การป้องกัน |
|---|---|---|---|
| R1 | Publish ด้วยค่า QA แล้ว deploy ขึ้น prod | prod วิ่งชน DB/ไฟล์ QA | บังคับ Phase 1 เสร็จก่อน publish; ตรวจ appsettings ปลายทางหลัง deploy |
| R2 | deploy script ก็อป appsettings ทับ config prod | prod config หาย | เตรียม prod config ใน publish, หรือ patch script ไม่ให้ทับ + verify root หลัง flip |
| R3 | DB เปล่ารัน migration ไม่ได้ (ไม่มี InitialCreate) | schema สร้างไม่ได้ | baseline จาก QA schema-script + seed `__EFMigrationsHistory` (Phase 2) |
| R4 | ETL map ผิด/ข้อมูลหาย | ประวัติเรียน/enrollment เสีย | dry-run + reconciliation + ตรวจ sample จริงก่อน cutover; source read-only |
| R5 | เนื้อหา SCORM (byte[] + share) ไม่ครบ | เล่นคอร์สไม่ได้ | ย้ายทั้ง DB + folder Courses; ทดสอบเปิด player |
| R6 | URL prod ต่างจาก QA (path base) | link/asset พัง | React build ด้วย base path prod; config BaseUrl `/iLearn/...`; ยืนยัน student path |
| R7 | LearnerProxy secret ไม่ตรง User↔API | learner proxy 401 | secret prod ใหม่ ตั้งให้ตรงทั้งสองฝั่ง |
| R8 | ไม่มี maintenance window / downtime ยาว | กระทบผู้ใช้ | ETL/content copy ให้ได้ส่วนใหญ่ก่อน window; delta ตอน cutover |
| R9 | **schema state ไม่ตรง HEAD** — migration `RenameResourceStudentTerminology` (29 เม.ย.) เปลี่ยน `Resources→ContentItems`, `CourseResources→CourseContentItems`, `ResourceHref→LaunchHref` ฯลฯ; สคริปต์ ETL ที่เขียนบน schema เก่า (เช่น 3 สคริปต์ Gemini ใน Downloads) จะ **error กับ DB ที่ HEAD** | ETL ล้มทั้งยวง | **ยืนยัน `__EFMigrationsHistory`/`sys.tables` ของ DB เป้าหมายก่อนรัน ETL เสมอ**; ใช้ชื่อ HEAD (`ContentItems`/`CourseContentItems`) ตาม [etl-catalog.sql](PLAN-045-etl-catalog.sql); DB เป้าหมายต้อง migrate ถึง HEAD (แอปที่ deploy ต้องการ) |

---

## Verification (คำสั่ง build — จาก CLAUDE.md)
```powershell
# React (จาก iLearn.Admin.React)
npm run lint ; npm run build
# Backend
dotnet build iLearn.API/iLearn.API.csproj -c Release --artifacts-path artifacts/verify-api
dotnet build iLearn.User/iLearn.User.csproj -c Release --artifacts-path artifacts/verify-user
dotnet build iLearn.Admin/iLearn.Admin.csproj -c Release --artifacts-path artifacts/verify-admin
dotnet build iLearn.Tests -o artifacts/verify-test ; dotnet test artifacts/verify-test/iLearn.Tests.dll
```

## Open questions (ต้องปิดก่อน execute)
1. ค่าทั้งหมดในตาราง "ค่าที่ต้องได้จากผู้ใช้"
2. student อยู่ `/iLearn/student` หรือ `/iLearn` (root)?
3. ETL: ต้องการย้าย **ทั้งหมด** หรือเฉพาะข้อมูล active (ตัด log เก่ากว่า X ปี)?
4. DB name prod ใหม่ (`iLearnDB_New` เหมือน QA หรือชื่ออื่น)?
5. maintenance window + เกณฑ์ rollback (นาที)
