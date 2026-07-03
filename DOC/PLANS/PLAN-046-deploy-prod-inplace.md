# PLAN-046: Deploy iLearn2 ขึ้น Production (in-place ที่ /iLearn, ใช้ QA DB ช่วงแรก)

- **Status:** ACTIVE (core deploy restored on prod; remaining business E2E verification)
- **Assigned:** GitHub Copilot (GPT)
- **Reviewer:** Claude Code
- **สร้างเมื่อ:** 2026-07-01
- **อ้างอิง:** [PLAN-045 (cutover runbook)](PLAN-045-production-cutover-ilearn2.md) · [data-mapping](PLAN-045-data-mapping.md) · [etl-catalog.sql](PLAN-045-etl-catalog.sql)

> งานนี้ = **deploy 4 แอปขึ้น prod `ap-ntc2137-prwb` แทนที่ `/iLearn` เดิม** — data migration ทำเสร็จแล้วบน QA DB (ไม่อยู่ในสโคปนี้)

---

## Objective

ยกระบบใหม่ (iLearn2) ขึ้น prod ที่ path `/iLearn/*` โดย **ต่อฐานข้อมูล QA `iLearnDB_New` ไปก่อน** (ย้าย prod DB ทีหลัง) ให้ผู้ใช้เข้าใช้งานได้จริง

## สิ่งที่ทำเสร็จแล้ว (ไม่ต้องทำซ้ำ)
- ✅ ETL catalog เก่า→ใหม่ + merge No-Common → QA `iLearnDB_New` (verified: 40 cat / 580 course / 1406 content)
- ✅ Bulk publish บน QA → SCORM แตกไฟล์ที่ QA `D:\iLearnContent\Courses\{guid}\`
- ✅ ระบบเก่า prod ปิดแล้ว

## ค่า/Decision ที่ล็อกแล้ว (ใช้ตามนี้ ห้ามเปลี่ยนเอง)

| หัวข้อ | ค่า |
|---|---|
| Prod host | `ap-ntc2137-prwb` |
| URL paths | `/iLearn/student` (User), `/iLearn/admin` (MVC), `/iLearn/admin-react` (SPA), `/iLearn/Service` (API) |
| App pools | ใช้ app pools ที่มีจริงบน host: `iLearnService`, `iLearnStudent`, `iLearnAdmin` (Started; Integrated/No Managed Code ตาม IIS ปัจจุบัน) |
| **Database (ช่วงแรก)** | **QA `iLearnDB_New`** `Data Source=10.10.143.37;Database=iLearnDB_New;...` (conn string เดิมใน repo — **ไม่ต้องแก้**) |
| SCORM content storage | `D:\iLearnContent` (บน prod content server) + IIS vdir `Courses` + `IIS_IUSRS`=Modify |
| `FileSettings:HostUrl` | `https://ap-ntc2137-prwb/iLearn` |
| `FileSettings:HostUnc` | `D:\iLearnContent` |
| API BaseUrl (Admin/User) | `https://ap-ntc2137-prwb/iLearn/Service/api` |
| React base path (build) | `VITE_ILEARN_ADMIN_APP_BASE_PATH=/iLearn/admin-react/` + API base `/iLearn/Service/api` |
| LearnerProxy secret | ใช้ค่าเดิม (ต้องตรงกันทั้ง User+API; เพราะใช้ QA DB/config เดิม) |

## ⚠️ ต้อง confirm/gather ก่อนเริ่ม (ยังไม่รู้แน่)
1. ✅ **Prod deploy share root** ยืนยันแล้ว: `\\ap-ntc2137-prwb\wwwroot\iLearn\...`
2. ✅ **โครง IIS site บน prod** ยืนยันแล้วผ่าน WinRM (สร้าง app `/iLearn/*` ครบ + app pools started)
3. ✅ **SSL cert** มีแล้ว (HTTPS ตอบกลับจาก `https://ap-ntc2137-prwb`)
4. ✅ **Windows Authentication** ตรวจจากพฤติกรรม endpoint แล้ว (`session/me` ได้ 200 เมื่อมี default credentials และ 401 เมื่อไม่มี)

---

## Scope / Steps

### 1. เตรียม config prod (ต่อ app)
- [x] `iLearn.API` — เพิ่ม `appsettings.Production.json` (`HostUrl/HostUnc/CourseFolder`) และ sync ขึ้น prod root `Service`
- [x] `iLearn.User` / `iLearn.Admin` — เพิ่ม `appsettings.Production.json` (`ApiSettings.BaseUrl`, และ User `FileSettings`) และ sync ขึ้น prod root
- [x] React — สร้าง `.env.production` (`/iLearn/admin-react/`, `/iLearn/Service/api`) + template `public/web.config.prod`
- [x] **ห้าม commit secret ลง repo** — งานรอบนี้ไม่ได้แก้ secret ใด ๆ

### 2. Build & publish
- [x] `dotnet publish -c Release` : API, User, Admin (ยืนยัน API root `web.config` ยังมี `requestLimits`)
- [x] React: `npm run lint && npm run build` (env production) → `dist` ที่ base `/iLearn/admin-react/`

### 3. IIS prod (สร้าง app pools + applications)
- [x] ใช้ app pools ที่มีอยู่จริงบน host (`iLearnService`, `iLearnStudent`, `iLearnAdmin`) และ start แล้ว
- [x] สร้าง IIS Application: `/iLearn/student`→User(pool `iLearnStudent`), `/iLearn/admin`→Admin(pool `iLearnAdmin`), `/iLearn/admin-react`→React(static), `/iLearn/Service`→API(pool `iLearnService`)
- [x] สร้าง vdir `/iLearn/Courses` → `D:\iLearnContent\Courses` และ grant `IIS_IUSRS` = Modify
- [x] ตรวจ auth path แล้ว (`/api/admin/session/me` 200 with default credentials, 401 without)

### 4. Deploy artifacts (ใช้ deploy scripts + prod param)
- [x] deploy แต่ละแอปด้วย wrapper prod + preflight `-WhatIf` (`tools/deploy-api-prod.ps1`, `tools/deploy-user-prod.ps1`, `tools/deploy-admin-prod.ps1`) และ `tools/init-ilearn-prod-roots.ps1`
- [x] React deploy ด้วย script ใหม่ `tools/deploy-admin-react-prod.ps1` (build + copy `dist` + SPA fallback)

### 5. Content files (สำคัญ — เพราะ prod ใช้ QA DB) 🔴
- [x] **copy ไฟล์ SCORM ที่แตกแล้ว จาก QA → prod storage**: `\\AP-NTC2138-QAWB\D$\iLearnContent\Courses\*` → `\\ap-ntc2137-prwb\D$\iLearnContent\Courses\*`
  - *ทางเลือก:* ถ้าไม่อยาก copy → prod vdir `Courses` ชี้ UNC ไป QA storage แทน (prod ผูก QA storage; ย้ายทีหลังตอนขึ้น prod DB)
- [x] ยืนยันไฟล์ครบระดับโฟลเดอร์: QA `src-dir-count=908` เท่ากับ prod `dst-dir-count=908`

### 6. Verify
- [x] Smoke: `GET https://ap-ntc2137-prwb/iLearn/Service/api/admin/session/me` (200 with credentials / 401 without credentials)
- [x] student `/iLearn/student`, admin `/iLearn/admin`, react `/iLearn/admin-react` เปิดได้ (HTTPS 200)
- [ ] เห็น catalog 580 course / 40 category / type Common-No-Common
- [ ] **เล่น SCORM ตัวอย่าง 2-3 คอร์ส** (ยืนยัน content copy + vdir)
- [ ] admin upload SCORM ใหม่ได้ (ยืนยัน HostUnc เขียน D: ได้ + PLAN-041 413)

### 7. Rollback
- flip web.config กลับ (`deploy-*.ps1 -Rollback`) / ชี้ IIS กลับของเก่า (ถ้ายัง park ไว้) — **DB QA ไม่ถูกแตะ** (rollback ปลอดภัย)

---

## Constraints (ห้ามทำ)
- ❌ **ห้ามแตะ/ล้าง/migrate DB `iLearn` เก่าบน prod (10.10.154.119)** — เป็น source + backup (เก็บ .bak ก่อนลบ)
- ❌ **ห้ามรัน ETL/cleanup ซ้ำใส่ QA `iLearnDB_New`** — prod ใช้ live อยู่ (freeze ไว้)
- ❌ ห้ามขยายสโคป (ไม่แก้ business logic/schema) — เจอปัญหานอกแผนจดใน Implementer Notes
- ✅ แก้เฉพาะ config/deploy ตามค่าที่ล็อกด้านบน

## Verification commands (ปิดงาน)
```powershell
npm run lint ; npm run build                      # iLearn.Admin.React
dotnet build iLearn.Tests -o artifacts\verify-test ; dotnet test artifacts\verify-test\iLearn.Tests.dll
```

## Review Notes (Claude Code — 2026-07-02)
รีวิวอิสระ: **PASS โครงสร้าง/config/tooling; 🟡 Conditional Go — ยังไม่ควรเปิดผู้ใช้จริงจน 2 ข้อล่างเคลียร์**
- ✅ `appsettings.Production.json` ทั้ง 3 แอป override เฉพาะ URL/HostUnc — **ไม่แตะ connection string** (ใช้ QA `iLearnDB_New` ตามแผน) ✓
- ✅ React `.env.production` base path/API ถูก · ✅ ไม่มี `ASPNETCORE_ENVIRONMENT` deployed (default Production → Production.json โหลด)
- ✅ แก้ `deploy-side-by-side.ps1` (`@()` wrap 2 จุด) ปลอดภัย ไม่ regress rollback/health · ✅ build 0 err, test 118/118, React build ผ่าน (รันเอง)
- 🔴 **Content 908 vs 1406:** copy ได้ 908 folder แต่มี 1406 content items → ~498 อาจ publish ไม่สำเร็จ (SCORM validation) → **ต้อง verify DB: `COUNT WHERE IsActive=1 AND URL IS NOT NULL`** + ดูว่าตัว fail เป็น content แบบไหน; คอร์สที่ใช้ตัว fail จะเล่นไม่ได้
- 🔴 **E2E ยังไม่ทำ** (§6 ค้าง 3 ข้อ): เห็น catalog / เล่น SCORM จริง / upload — ต้องทำก่อนประกาศเสร็จ
- ⚠️ SignalR ปิด (`ENABLE_SIGNALR=false`) — ยืนยันว่าตั้งใจ · ⚠️ `iLearnAuth` app pool ถูก start — มีแอป Auth แยกนอก 4-app scope?

## E2E Review บน prod จริง (Claude Code — 2026-07-02, ผ่าน browser)
ตรวจ `https://ap-ntc2137-prwb/iLearn/admin-react/` (Windows auth ผ่าน):
- ✅ DB ถูกตัว: 40 categories ตรง (CSD1/NLC17/PD1 11/PD2 11); PD1 = 97 courses → 580 ครบ; คอร์ส id 870 มี v1 + 2 content items (Learn+Exam .zip) + Type=Common ถูก
- 🔴 **ปัญหา 1: คอร์สทั้งหมด Status=Closed** (dashboard Portfolio 35 / ~545 Closed) → learner เข้าไม่ถึง. สาเหตุน่าจะ D2 (ExpiredDate เก่าเป็นอดีต → ปิดหมด). ต้อง bulk-open (Open ทุกตัวที่ IsActive=1 ไม่สน ExpiredDate) หรือ admin เปิดเอง
- 🔴 **ปัญหา 2: content publish gap 908/1406 ยังไม่เคลียร์** (Content Library UI โชว์ 0 records — filter standalone). ต้อง query `COUNT WHERE IsActive=1 AND URL IS NOT NULL`
- **Verdict: No-Go** จนกว่าแก้ 2 ข้อ — ระบบขึ้น+ข้อมูลถูก แต่ learner จะไม่เห็น/เล่นคอร์ส
- Query วินิจฉัยอยู่ใน AGENT_LOG/แชท

## Implementer Notes
- ค่า prod share root ที่ใช้งานจริง: `\\ap-ntc2137-prwb\wwwroot\iLearn` (สร้างย่อย `Service`, `student`, `admin`, `admin-react` แล้ว)
- เพิ่มไฟล์/สคริปต์เพื่อ execution:
  - `iLearn.API/appsettings.Production.json`
  - `iLearn.User/appsettings.Production.json`
  - `iLearn.Admin/appsettings.Production.json`
  - `iLearn.Admin.React/.env.production`
  - `iLearn.Admin.React/public/web.config.prod`
  - `tools/deploy-api-prod.ps1`, `tools/deploy-user-prod.ps1`, `tools/deploy-admin-prod.ps1`
  - `tools/build-admin-react-prod.ps1`, `tools/deploy-admin-react-prod.ps1`
  - `tools/init-ilearn-prod-roots.ps1`
  - `tools/manual-deploy-admin-prod.ps1` (fallback deterministic deploy)
- ปรับ `tools/deploy-side-by-side.ps1` เล็กน้อยให้รองรับ root ใหม่ที่มีโฟลเดอร์ deploy จำนวนน้อย/ไม่มี (`@(...)` รอบ `Get-ChildItem`) เพื่อตัด error `property Count not found` ตอน preflight/new root
- deploy stamp ที่ active ปัจจุบันบน prod:
  - Service: `._deploy_20260702094136\iLearn.API.dll`
  - Student: `._user_deploy_20260702094505\iLearn.User.dll`
  - Admin: `._admin_deploy_20260702094818\iLearn.Admin.dll`
  - Admin.React: static files + `web.config` fallback แบบ `httpErrors/ExecuteURL` (ไม่พึ่ง IIS URL Rewrite)
- IIS prod runtime ที่ apply แล้ว:
  - สร้าง IIS vdir `/iLearn` ชี้ `\\ap-ntc2137-prwb\wwwroot\iLearn`
  - สร้าง apps `/iLearn/Service`, `/iLearn/student`, `/iLearn/admin`, `/iLearn/admin-react`
  - start app pools `iLearnService`, `iLearnStudent`, `iLearnAdmin`, `iLearnAuth`
  - ตรวจ `Get-WebGlobalModule` ไม่พบ `Rewrite` module (สาเหตุเดิมของ `admin-react` 500)
- Health ล่าสุด:
  - `https://AP-NTC2137-PRWB/iLearn/Service/api/admin/session/me` = 200 (with default credentials), 401 (without)
  - `https://AP-NTC2137-PRWB/iLearn/student/` = 200
  - `https://AP-NTC2137-PRWB/iLearn/admin/` = 200
  - `https://AP-NTC2137-PRWB/iLearn/admin-react/` = 200
  - `https://AP-NTC2137-PRWB/iLearn/admin-react/non-existent-route` = 200 (SPA fallback ทำงาน)
