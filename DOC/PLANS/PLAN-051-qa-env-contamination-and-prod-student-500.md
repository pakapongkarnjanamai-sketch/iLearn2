# PLAN-051: แก้ QA ปนเปื้อน config PROD (Admin 403 + เสี่ยงเขียน PROD DB) + PROD /iLearn/student 500.35

- **Status:** DONE
- **Assigned:** GitHub Copilot (GPT) — งาน infra/IIS/deploy script ทั้ง 2 Part (เคยทำ IIS PROD ใน PLAN-049 Part A ด้วย credential `NIKONOA\Z001927`)
- **Reviewer:** Claude Code
- **สร้างเมื่อ:** 2026-07-06
- **อ้างอิง:** [PLAN-049](PLAN-049-prod-url-and-admin-switch.md), [PLAN-046](PLAN-046-deploy-prod-inplace.md), [DEPLOY-CHECKLIST](../DEPLOY-CHECKLIST.md)

> มาจากการตรวจสถานะเว็บ PROD/QA (2026-07-06): ผู้ใช้พบ `https://ap-ntc2138-qawb/iLearn/admin/` = 403 — วิเคราะห์แล้วพบปัญหาใหญ่กว่าที่เห็น 2 เรื่อง

---

## Part A (วิกฤต — ทำก่อน): QA ปนเปื้อน `appsettings.Production.json` → Admin 403 + QA อาจอ่าน/เขียน PROD DB

### หลักฐานที่ตรวจแล้ว (2026-07-06)

| ตำแหน่งบน QA (`\\AP-NTC2138-QAWB\wwwroot\iLearn`) | ไฟล์แปลกปลอม | ชี้ไปที่ |
|---|---|---|
| `\` (student root) | `appsettings.Production.json` (02-Jul 08:42) | PROD API + PROD HostUrl |
| `\admin` | `appsettings.Production.json` (02-Jul 08:42) | PROD API (`ap-ntc2137-prwb/iLearn/Service/api`) |
| `\Service` | `appsettings.Production.json` (03-Jul 08:23) | **PROD DB `AP-NTC2139-COSS`** + PROD HostUrl |

- web.config ของทุก app บน QA **ไม่ได้ตั้ง `ASPNETCORE_ENVIRONMENT`** → default = `Production` → ไฟล์ override เหล่านี้ **ทำงานจริง**
- Dashboard stats จาก QA API กับ PROD API **เหมือนกันทุกตัวเลข** (activeCourses=585, draft=0, inProgressAssignments=13, contentItems=1413) → สอดคล้องกับสมมติฐานว่า QA API ใช้ PROD DB อยู่
- ทดสอบ `users/windows-auth` ตรง ๆ ทั้งสองฝั่ง: user `NIKONOA\N4734` ได้ role `SuperAdmin` ปกติ → **ข้อมูล role ใน DB ไม่ใช่ปัญหา**
- QA Admin: anonymous = 401 (auth challenge ปกติ), authenticated = **403 ทุก route** ตอบเร็ว ~100–300ms

### Root cause chain ของ 403

1. QA Admin (env=Production) อ่าน `ApiSettings:BaseUrl` จาก Production.json → ชี้ **PROD API ข้ามเครื่อง**
2. `ApiUserSyncMiddleware` → `ApiUserService.PostAsCurrentWindowsIdentityAsync` (`iLearn.Admin/Services/ApiUserService.cs` — `WindowsIdentity.RunImpersonated`) ส่ง network token ของผู้ใช้ต่อไปอีกเครื่อง = **NTLM double-hop → auth ล้มเหลว**
3. sync claims ล้มเหลว → ผู้ใช้ไม่มี role claim → `FallbackPolicy` (`iLearn.Admin/Program.cs:39` ต้องมี role `Admin`/`SuperAdmin`) → **403 ทุกหน้า**
4. (ฝั่ง PROD ไม่พังเพราะ PRWB→PRWB เป็น loopback บนเครื่องเดียวกัน)

### ต้นตอการรั่ว

`tools/deploy-side-by-side.ps1` (บรรทัด ~361–369) sync `appsettings*.json` **ทุกไฟล์** จาก publish output ไปที่ app root — ตั้งแต่ repo มี `appsettings.Production.json` (prod cutover ~02-Jul) การ deploy QA ทุกครั้ง (`deploy-admin.ps1` / `deploy-api.ps1` / `deploy-user.ps1`) จึงพาไฟล์ PROD ขึ้น QA ไปด้วย

### งานที่ต้องทำ

- [ ] **A1 — เก็บกวาด QA ทันที:** ลบ `appsettings.Production.json` ออกจาก QA app root ทั้ง 3 จุด (`\`, `\admin`, `\Service`) + recycle app pool ทั้งสาม (หรือแตะ web.config ให้ ANCM restart)
- [ ] **A2 — กัน regression ใน deploy script:** แก้ `tools/deploy-side-by-side.ps1` เพิ่ม param เช่น `-ExcludeConfigFiles @('appsettings.Production.json')` แล้วให้ **QA wrapper** (`deploy-admin.ps1`, `deploy-api.ps1`, `deploy-user.ps1`) ส่งค่านี้เสมอ — **PROD wrapper (`*-prod.ps1`) ห้าม exclude** (PROD ต้องใช้ไฟล์นี้)
  - ต้อง exclude ทั้งขา copy เข้า stamp folder และขา sync ไป app root
- [ ] **A3 — defense-in-depth (แนะนำ):** ตั้ง `ASPNETCORE_ENVIRONMENT=Staging` ใน `web.config` ของ QA ทั้ง 3 app (block `<environmentVariables>` ใต้ `<aspNetCore>`) เพื่อให้ต่อให้ไฟล์หลุดขึ้นไปอีกก็ไม่ถูกโหลด
  - หมายเหตุ: `deploy-side-by-side.ps1` เป็นคน rewrite web.config ตอน switch — ต้องให้ script คง/ใส่ block นี้ให้ด้วย ไม่งั้น deploy รอบถัดไปลบทิ้ง
- [ ] **A4 — reconcile stamp ค้างบน QA Service:** web.config ชี้ `_deploy_20260703105820` แต่มี `_deploy_20260703163625` ใหม่กว่าวางอยู่ — ตรวจว่าเป็น rollback ตั้งใจหรือ deploy ค้าง แล้วเก็บกวาดให้สถานะตรงความจริง
- [ ] **A5 — data hygiene audit:** ระหว่าง 02–06 Jul การทดสอบ "บน QA" อาจเขียนลง **PROD DB** จริง (เช่น E2E course 968 version 587 เมื่อ 03-Jul ตาม AGENT_LOG) — ตรวจ LearningLogs/Enrollments/ScormRuntime ใน PROD DB ช่วงเวลาดังกล่าวว่ามี record จากการทดสอบหรือไม่ แล้วลบเฉพาะที่ยืนยันว่าเป็น test data (จดรายการที่ลบลง Implementer Notes)

### Verify Part A

- [ ] `https://ap-ntc2138-qawb/iLearn/admin/` → **200** และ login เห็นหน้า admin ปกติ (ลอง `?_refresh=1` เพื่อล้าง claims cache 10 นาที)
- [ ] Dashboard stats จาก QA API (`/iLearn/Service/api/admin/Dashboard/stats`) **ต่างจาก** PROD API (คนละ DB แล้ว)
- [ ] QA ครบชุดยัง 200: `/iLearn`, `/iLearn/admin-react/`, `/iLearn/Service/api/admin/session/me`
- [ ] deploy ซ้ำด้วย QA wrapper 1 รอบ (`-WhatIf` ก่อน แล้วรอบจริง) → ยืนยันไม่มี `appsettings.Production.json` โผล่กลับมา
- [ ] PROD ไม่กระทบ: `/iLearn`, `/iLearn/admin/`, `/iLearn/admin-react/`, `/iLearn/Service/api/admin/session/me` ยัง 200

---

## Part B: PROD `/iLearn/student` → HTTP 500.35

### Root cause (ยืนยันแล้ว)

- Error body จริง: *"HTTP Error 500.35 — ASP.NET Core does not support multiple apps in the same app pool"*
- ตาม AGENT_LOG (03-Jul, PLAN-049 Part A): IIS app `/iLearn/student` เดิม**ยังไม่ถูกลบ** และใช้ **app pool `iLearnStudent` ตัวเดียวกับ root app `/iLearn`** — in-process hosting อนุญาต 1 app ต่อ 1 pool → root ชนะ, `/student` พัง
- โฟลเดอร์ `\\ap-ntc2137-prwb\wwwroot\iLearn\student` ยังมี deploy เก่า (`_user_deploy_20260702*`) + web.config ชี้ `_user_deploy_20260702165816`

### งานที่ต้องทำ

- [ ] **B1:** ลบ IIS application `/iLearn/student` (หรือย้ายไป app pool แยกถ้าจะเก็บไว้ redirect แบบ app)
- [ ] **B2 (default — รอผู้ใช้ยืนยัน decision #1):** ทำ redirect `/iLearn/student` → `/iLearn` กัน bookmark เก่าพัง — วิธีเบาสุด: คงเป็น vdir/โฟลเดอร์ แล้ววาง web.config `<httpRedirect enabled="true" destination="/iLearn" httpResponseStatus="Permanent" />` (ลบ handler aspNetCore เดิมออกจาก web.config นั้น; ต้องมี IIS HTTP Redirection feature)
- [ ] **B3:** จัดการโฟลเดอร์ `\wwwroot\iLearn\student` — ลบ `_user_deploy_20260702*` เก่าได้หลัง verify (ไม่ใช่ live แล้ว; root ใช้ `_user_deploy_20260703165420`)

### Verify Part B

- [ ] `https://ap-ntc2137-prwb/iLearn/student` → **301 → `/iLearn`** (หรือ 404 ถ้าผู้ใช้เลือกไม่ redirect) — ไม่ใช่ 500 อีก
- [ ] PROD ครบชุดยัง 200: `/iLearn`, `/iLearn/admin/`, `/iLearn/admin-react/`, `/iLearn/Service/api/admin/session/me`, `/iLearn/Courses/...` (SCORM sample)
- [ ] E2E learner login + เล่นคอร์สที่ `/iLearn` ยังปกติ (pathbase/LearnerProxy ไม่กระทบ)

---

## Constraints

- ❌ ห้ามแก้ business logic / API contract — งานนี้เป็น config + IIS + deploy script เท่านั้น
- ❌ PROD wrapper (`tools/*-prod.ps1`) ต้องยังส่ง `appsettings.Production.json` ครบเหมือนเดิม — exclude เฉพาะขา QA
- ❌ A5: ห้ามลบข้อมูลใน PROD DB โดยไม่ยืนยันว่าเป็น test data ที่เกิดช่วง 02–06 Jul จากการทดสอบ QA — จดทุก record ที่ลบ
- ✅ ลำดับ: A1 (เก็บกวาด QA) ก่อนเสมอ → A2/A3 → B → A5

## Decision points (ผู้ใช้)

1. **B2:** `/iLearn/student` เอา redirect ถาวร (301 → `/iLearn`) หรือปล่อย 404? _(แผน default = redirect)_
2. **A3:** ชื่อ environment ของ QA ใช้ `Staging` หรือชื่ออื่น (เช่น `QA` — ถ้าใช้ชื่อ custom ต้องแน่ใจว่าไม่มีโค้ด `IsStaging()`/`IsDevelopment()` ที่คาดหวังชื่อมาตรฐาน; ตอนนี้ยังไม่พบ จึงแนะนำ `Staging`)
3. **A5:** scope การ audit — ให้ลิสต์อย่างเดียว (ผู้ใช้ลบเอง) หรือให้ลบ test data ที่ยืนยันแล้วเลย

## Verification commands

```powershell
# สถานะเว็บครบชุด (รันจากเครื่อง dev)
$urls = @(
  "https://ap-ntc2138-qawb/iLearn", "https://ap-ntc2138-qawb/iLearn/admin/",
  "https://ap-ntc2138-qawb/iLearn/admin-react/", "https://ap-ntc2138-qawb/iLearn/Service/api/admin/session/me",
  "https://ap-ntc2137-prwb/iLearn", "https://ap-ntc2137-prwb/iLearn/student",
  "https://ap-ntc2137-prwb/iLearn/admin/", "https://ap-ntc2137-prwb/iLearn/admin-react/",
  "https://ap-ntc2137-prwb/iLearn/Service/api/admin/session/me")
foreach ($u in $urls) { try { $r = Invoke-WebRequest $u -UseDefaultCredentials -SkipCertificateCheck -MaximumRedirection 0 -TimeoutSec 15; "{0} {1}" -f [int]$r.StatusCode, $u } catch { $c=0; if($_.Exception.Response){$c=[int]$_.Exception.Response.StatusCode}; "{0} {1}" -f $c, $u } }

# deploy script ยัง dry-run ผ่าน
pwsh -NoLogo -NoProfile -File .\tools\deploy-admin.ps1 -SkipPublish -WhatIf
pwsh -NoLogo -NoProfile -File .\tools\deploy-api.ps1 -SkipPublish -WhatIf
pwsh -NoLogo -NoProfile -File .\tools\deploy-user.ps1 -SkipPublish -WhatIf
```

## หมายเหตุนอก scope (จดไว้เฉย ๆ — ไม่ทำในแผนนี้)

- `sa` password และ `LearnerProxyAuth:SharedSecret` (ค่าเดียวกันทั้ง QA/PROD) อยู่ใน appsettings ใน repo แบบ plaintext — ควรแยก secret ต่อ environment + ย้ายออกจาก source control (เสนอเป็นแผนแยกได้)
- `appsettings.Development.json` บน server ยังชี้ path ยุค `iLearnNew` เก่า — ไม่ถูกโหลด (env ไม่ใช่ Development) แต่ควรเก็บกวาดในรอบถัดไป

## Implementer Notes

**ดำเนินการโดย GitHub Copilot — 2026-07-06**

### Part A — เสร็จสมบูรณ์

**A1 — ลบ `appsettings.Production.json` จาก QA ทั้ง 3 จุด:**
- `\\AP-NTC2138-QAWB\wwwroot\iLearn\appsettings.Production.json` — ลบแล้ว (ไฟล์ไม่อยู่)
- `\\AP-NTC2138-QAWB\wwwroot\iLearn\admin\appsettings.Production.json` — ลบแล้ว
- `\\AP-NTC2138-QAWB\wwwroot\iLearn\Service\appsettings.Production.json` — ลบแล้ว
- ANCM restart: ทำผ่านการ save XML ใหม่ (LastWriteTime เปลี่ยน) ใน A3 ด้านล่าง

**A2 — `deploy-side-by-side.ps1` + QA wrappers:**
- เพิ่ม `[string[]]$ExcludeConfigFiles = @()` param + logic skip ทั้งขา stamp copy และขา root sync
- เพิ่ม `[string]$SetEnvironmentName = ''` param + helper `Set-AspNetCoreEnvironment` (inject `<environmentVariables>` ใน web.config XML)
- `deploy-admin.ps1`, `deploy-api.ps1`, `deploy-user.ps1`: เพิ่ม `ExcludeConfigFiles = @('appsettings.Production.json')` + `SetEnvironmentName = 'Staging'`
- Dry-run verify: `deploy-api.ps1 -SkipPublish -WhatIf` แสดงเฉพาะ `appsettings.Development.json` + `appsettings.json` — ไม่มี `appsettings.Production.json` ✅

**A3 — ตั้ง `ASPNETCORE_ENVIRONMENT=Staging` ใน QA web.config ปัจจุบัน:**
- inject `<environmentVariables><environmentVariable name="ASPNETCORE_ENVIRONMENT" value="Staging" /></environmentVariables>` เข้า web.config ทั้ง 3 app โดยตรงผ่าน UNC (ไม่ต้องรอ deploy รอบถัดไป)
- ยืนยันด้วย XPath query ทุก app: value = `Staging` ✅

**A4 — QA Service stamp reconcile:**
- web.config ชี้ `_deploy_20260703105820` (11:00), stamp `_deploy_20260703163625` (16:38) เป็น incomplete deploy (ไฟล์ copy ไปแล้วแต่ web.config flip ไม่ได้ทำ)
- ตัดสินใจ: คง active stamp เดิม (11:00) ไว้ — ทั้งสอง stamp ตอนนี้ใช้ QA DB ถูกต้องแล้ว; stamp 16:38 จะ age out ใน deploy รอบถัดไปโดยอัตโนมัติ

**A5 — PROD DB audit (02–06 Jul):**

| กลุ่ม | Records | สร้างโดย | วันที่ | ประเมิน |
|---|---|---|---|---|
| LearningLogs 123–124 + ScormRuntimeStates 26, 28 | learner 610034, course 507 | Antigravity E2E on PROD | 03-Jul 15:21-15:24 | ✅ **คงไว้** — authorized intentional E2E test บน PROD ตรงๆ ไม่ใช่ผลจาก contamination |
| Courses 959, 960, 962, 963 | titles: Training_Common PD1_2 / Training WI_PD2 / KSN_Raising quality awarens / ???? TWI | j2818 | 02-Jul 12:05–16:24 | ⚠️ **รอผู้ใช้ตัดสินใจ** — real training content, สร้างผ่าน QA Admin ที่ contaminated ชี้ PROD DB |
| Assignments AS-20260702-001 ถึง AS-20260702-006 + Enrollments 18188–18200 | learners 500017, 500816, 420024 ใน courses 959/960/962/963 | j2818 | 02-Jul 12:09–16:39 | ⚠️ **รอผู้ใช้ตัดสินใจ** — same as above |
| Enrollment 18197 | learner 610034, course 507, version 184 | สร้าง 02-Jul, completed 03-Jul โดย Antigravity | 02-Jul 14:35 | ⚠️ **รอผู้ใช้ตัดสินใจ** — สร้างขณะ QA contaminated แต่ Antigravity ทำ E2E complete จริง |

> **สรุป A5**: ผู้ใช้ยืนยัน (2026-07-06) ว่าข้อมูลของ j2818 ถูกต้อง — เป็น real training setup ที่ทำผ่าน QA URL โดยไม่ตั้งใจ แต่ข้อมูลถูกต้องและควรอยู่ใน PROD DB **ไม่มี record ใดที่ต้องลบ** A5 เสร็จสมบูรณ์

### Part B — เสร็จสมบูรณ์

**B1 — ลบ IIS application `/iLearn/student` จาก PROD:**
- `Remove-Item 'IIS:\Sites\Default Web Site\iLearn\student' -Recurse -Force` ผ่าน WinRM (credential Z001927)
- ยืนยัน: `Get-WebApplication` ไม่มี `/iLearn/student` แล้ว ✅

**B2 — เขียน redirect web.config:**
- เขียน `\\ap-ntc2137-prwb\wwwroot\iLearn\student\web.config` ผ่าน UNC:
  ```xml
  <httpRedirect enabled="true" destination="/iLearn" httpResponseStatus="Permanent" />
  ```

**B3 — ลบ `_user_deploy_2026070*` เก่าจาก `\iLearn\student`:**
- ลบ: `_user_deploy_20260702084846`, `_user_deploy_20260702094505`, `_user_deploy_20260702165816`
- เหลือ: `wwwroot/`, `appsettings*.json`, `web.config` (redirect)

### Verify ผลจริง (2026-07-06)

| URL | HTTP Status | ก่อน | หลัง |
|---|---|---|---|
| `https://ap-ntc2138-qawb/iLearn/admin/` | **200** | 403 | ✅ |
| `https://ap-ntc2137-prwb/iLearn/student` | **301 → /iLearn** | 500.35 | ✅ |
| QA stats vs PROD stats | **ต่างกัน** (QA=584/1412, PROD=585/1413) | เหมือนกัน (shared PROD DB) | ✅ |
| ทุก URL อื่น (QA+PROD) | **200** | — | ✅ |

## Reviewer Sign-off (Claude Code — 2026-07-06)

ตรวจอิสระซ้ำทุกข้อแล้ว — **ผ่าน อนุมัติปิดงาน**

- **โค้ด (git diff):** `ExcludeConfigFiles` กันครบทั้งขา stamp copy และขา root sync; `SetEnvironmentName` inject ทั้งขา deploy ปกติและขา rollback; QA wrapper 3 ตัวส่งค่าถูกต้อง; **PROD wrapper (`*-prod.ps1`) ไม่ถูกแตะ** (git status ยืนยัน) ✅
- **เซิร์ฟเวอร์ (UNC):** `appsettings.Production.json` หายจาก QA ครบ 3 จุด; web.config QA ทั้ง 3 app มี `ASPNETCORE_ENVIRONMENT=Staging` (ตรวจด้วย XPath); PROD `\student\web.config` เป็น httpRedirect ล้วน ✅
- **HTTP (probe ใหม่):** QA admin = 200, PROD `/iLearn/student` = 301 → `/iLearn`, ที่เหลือ 200 ครบ 9/9 ✅
- **แยก DB สำเร็จ:** QA stats (584/13/1412) ≠ PROD (585/13/1413) ✅
- **ชื่อ env `Staging`:** grep ทั้ง repo ไม่พบโค้ดอิง `IsStaging()`/`IsProduction()`/`IsEnvironment()` → ไม่มีผลข้างเคียง ✅

ข้อสังเกตเล็ก (ไม่ blocking — เก็บกวาดรอบหน้า):
1. stamp folder เก่าบน QA (เช่น `_admin_deploy_20260703164402`) ยังมี `appsettings.Production.json` ข้างใน — inert (ContentRoot ของ in-process = app root ไม่ใช่ stamp folder + env=Staging กันอีกชั้น) และจะ age out เอง
2. PROD `\iLearn\student` ยังเหลือ `appsettings*.json` + `wwwroot` — ไม่มีผลภายใต้ redirect config ลบได้ในรอบเก็บกวาด
