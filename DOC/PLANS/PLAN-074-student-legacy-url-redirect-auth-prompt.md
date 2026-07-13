# PLAN-074: ปลด Windows auth prompt ที่ `/iLearn/Student` (PROD) + ทำ redirect เดียวกันบน QA — ให้ URL เก่าเด้งไป `/iLearn` แบบเนียน ๆ

- **Status:** VERIFIED
- **Assigned:** GitHub Copilot — งาน infra/IIS ล้วน (คนเดิมที่ทำ PLAN-049/051 ผ่าน WinRM ด้วย credential `NIKONOA\Z001927`)
- **Reviewer:** Claude Code
- **สร้างเมื่อ:** 2026-07-13
- **อ้างอิง:** [PLAN-051 Part B](PLAN-051-qa-env-contamination-and-prod-student-500.md) (ลบ IIS app `/iLearn/student` + วาง redirect web.config)

> มาจาก user report (2026-07-13): ผู้ใช้เปิด `https://ap-ntc2137-prwb/iLearn/Student` แล้วเจอ popup **Windows Security "Sign in to access this site"** แทนที่จะเด้งไปหน้านักเรียน

---

## หลักฐานที่ probe แล้ว (2026-07-13 จากเครื่อง dev)

| URL | Anonymous (ไม่ส่ง credentials) | ส่ง Windows credentials |
|---|---|---|
| PROD `/iLearn/Student` | **401** `WWW-Authenticate: Negotiate, NTLM` | 301 → `/iLearn` |
| PROD `/iLearn/Student/MyLearning` | — | 301 → `/iLearn/MyLearning` (httpRedirect ต่อ path ลูกให้เอง — ถูกต้อง) |
| PROD `/iLearn` (หน้านักเรียนจริง) | **200** | — |
| QA `/iLearn/Student` | **401** | **404** (ไม่มี redirect — PLAN-051 B ทำเฉพาะ PROD; request ตกลง root app แล้วไม่มี route `Student`) |
| QA `/iLearn` | **401** | 200 |

## Root cause

**PROD:** PLAN-051 B1 ลบ IIS application `/iLearn/student` แล้ววาง web.config httpRedirect (301 → `/iLearn`) — แต่ **การตั้งค่า authentication ของ path นั้นที่ค้างอยู่ใน applicationHost.config (`<location path="Default Web Site/iLearn/student">` — Windows auth เปิด / Anonymous ปิด) ไม่ได้ถูกลบไปด้วย** เพราะการลบ application ไม่ลบ location-based config → IIS challenge 401 ก่อนถึงจะยอมส่ง 301

- ผู้ใช้ที่เบราว์เซอร์ตอบ 401 อัตโนมัติไม่ได้ (เครื่องไม่ join โดเมน / site ไม่อยู่ Intranet zone) จะเจอ popup; เครื่องในโดเมนตอบเงียบ ๆ เลยไม่มีใครเห็นปัญหา
- Verify ของ PLAN-051 ใช้ `Invoke-WebRequest -UseDefaultCredentials` ทุก URL → 401 ถูกตอบอัตโนมัติ เห็นเป็น 301 ผ่าน → **mask ปัญหานี้ตั้งแต่วันนั้น** (บทเรียน: URL ฝั่ง learner ต้อง verify แบบ anonymous)

**QA:** ไม่เคยมี redirect เลย (PLAN-051 B ทำเฉพาะ PROD) + root `/iLearn` ของ QA ปิด anonymous (ต่างจาก PROD ที่เปิด) — bookmark เก่าบน QA จึงเจอ 401 → 404

## เป้าหมาย

ผู้ใช้เปิด `/iLearn/Student` (รวม path ลึก เช่น `/iLearn/Student/MyLearning`) ทั้ง PROD และ QA → **เด้งไป `/iLearn` ทันทีโดยไม่มี popup ใด ๆ** — หน้านักเรียนจริงใช้ cookie auth (กรอกรหัสพนักงาน) อยู่แล้ว ไม่มีเหตุต้องใช้ Windows auth ที่ path นี้

---

## Part A: PROD `ap-ntc2137-prwb` (ต้นเหตุที่ผู้ใช้แจ้ง — ทำก่อน)

- [x] **A1 — ตรวจ + บันทึกสถานะ auth config ปัจจุบันของ location** (จดค่า before ลง Implementer Notes):
  ```powershell
  # รันบน PROD ผ่าน WinRM (Import-Module WebAdministration)
  foreach ($s in 'anonymousAuthentication','windowsAuthentication','basicAuthentication') {
    $c = Get-WebConfiguration -PSPath 'MACHINE/WEBROOT/APPHOST' `
      -Location 'Default Web Site/iLearn/student' `
      -Filter "system.webServer/security/authentication/$s"
    '{0}: enabled={1}' -f $s, $c.enabled
  }
  # ดู <location> ดิบใน applicationHost.config ประกอบ
  Select-String -Path "$env:windir\System32\inetsrv\config\applicationHost.config" -Pattern 'iLearn/student'
  ```
- [x] **A2 — ลบ auth config ที่ค้างของ location นั้นทิ้ง** ให้ inherit จาก `/iLearn` (ซึ่งเปิด anonymous อยู่แล้ว — พิสูจน์จาก root ตอบ 200 anonymous):
  ```powershell
  Clear-WebConfiguration -PSPath 'MACHINE/WEBROOT/APPHOST' `
    -Location 'Default Web Site/iLearn/student' `
    -Filter 'system.webServer/security/authentication/windowsAuthentication'
  Clear-WebConfiguration -PSPath 'MACHINE/WEBROOT/APPHOST' `
    -Location 'Default Web Site/iLearn/student' `
    -Filter 'system.webServer/security/authentication/anonymousAuthentication'
  ```
  - Fallback ถ้า Clear แล้วอาการไม่หาย (config อยู่ที่อื่น): ตั้งค่าตรง ๆ ที่ location เดิมแทน —
    `Set-WebConfigurationProperty ... anonymousAuthentication -Name enabled -Value $true` + `... windowsAuthentication -Name enabled -Value $false`
  - ไม่ต้อง recycle app pool — httpRedirect + auth เป็น IIS-level มีผลทันที
- [x] **A3 (optional — เก็บกวาดตามข้อสังเกต reviewer PLAN-051):** ในโฟลเดอร์ `\\ap-ntc2137-prwb\wwwroot\iLearn\student` ลบ `appsettings*.json` + `wwwroot\` เก่าที่เหลืออยู่ — **คงไว้เฉพาะ `web.config` (redirect)**

### Verify Part A (⚠️ ห้ามใส่ `-UseDefaultCredentials` กับ URL ฝั่ง student)

- [x] Anonymous `GET /iLearn/Student` → **301 → `/iLearn`** (ไม่มี 401 คั่น) — ทั้งตัวพิมพ์ `Student` และ `student`
- [x] Anonymous `GET /iLearn/Student/MyLearning` → 301 → `/iLearn/MyLearning`
- [x] Anonymous follow redirect จนสุด → จบที่ 200 หน้า login นักเรียน
- [x] เบราว์เซอร์จริง: เปิด `/iLearn/Student` → **ไม่มี Windows Security popup** เด้งถึงหน้านักเรียน _(reviewer ทดสอบแล้ว 2026-07-13 ผ่าน in-app browser — จบที่ `https://ap-ntc2137-prwb/iLearn` title "iLearn" ไม่มี dialog คั่น)_
- [x] Regression PROD: `/iLearn` (anonymous) = 200, `/iLearn/admin/`, `/iLearn/admin-react/`, `/iLearn/Service/api/admin/session/me` (with credentials) ยังปกติ — **auth ของ app อื่นต้องไม่เปลี่ยน**

---

## Part B: QA `ap-ntc2138-qawb` (ทำให้พฤติกรรมตรงกับ PROD)

- [x] **B1 — ตรวจว่า QA ยังมี IIS app `/iLearn/student` เก่าหรือไม่** (`Get-WebApplication -Site 'Default Web Site'`) — ถ้ามี ให้ลบแบบเดียวกับ PLAN-051 B1 (จดสถานะก่อนลบ)
- [x] **B2 — วาง redirect เหมือน PROD:** สร้างโฟลเดอร์ (ถ้ายังไม่มี) + เขียน `\\AP-NTC2138-QAWB\wwwroot\iLearn\student\web.config`:
  ```xml
  <?xml version="1.0" encoding="UTF-8"?>
  <configuration>
    <system.webServer>
      <httpRedirect enabled="true" destination="/iLearn" httpResponseStatus="Permanent" />
    </system.webServer>
  </configuration>
  ```
- [x] **B3 — เคลียร์ auth config ค้างของ location QA `/iLearn/student`** (ตรวจก่อนแบบ A1 แล้ว Clear แบบ A2 ถ้ามี)
- [x] **B4 (decision #1) — เปิด `anonymousAuthentication` ที่ QA root app `/iLearn`** ให้ตรงกับ PROD (ตอนนี้ QA root ตอบ 401 anonymous แต่ PROD ตอบ 200) — ถ้าไม่ทำ ข้อ B2/B3 จะช่วยเฉพาะเครื่องในโดเมน เพราะปลายทาง `/iLearn` บน QA ยัง challenge อยู่

### Verify Part B

- [x] Anonymous `GET https://ap-ntc2138-qawb/iLearn/Student` → 301 → `/iLearn` (ไม่มี 401)
- [x] Anonymous `GET https://ap-ntc2138-qawb/iLearn` → 200 (ถ้า decision #1 อนุมัติ)
- [x] Regression QA: `/iLearn/admin/`, `/iLearn/admin-react/`, `/iLearn/Service/api/admin/session/me` (with credentials) ยัง 200

---

## Constraints

- ❌ **ไม่มีการแก้โค้ดใน repo เลย** — งานนี้เป็น IIS config + ไฟล์บน server ล้วน (repo แตะเฉพาะไฟล์แผนนี้ + AGENT_LOG)
- ❌ ห้ามแตะ auth ของ app อื่น (`/iLearn` root บน PROD, `/iLearn/admin`, `/iLearn/admin-react`, `/iLearn/Service`) — scope เฉพาะ location `iLearn/student` ทั้งสองเครื่อง + QA root `/iLearn` เฉพาะ decision #1
- ❌ ห้ามลบ/แก้ `web.config` redirect ของ PROD (ใช้งานถูกต้องอยู่แล้ว)
- ✅ ลำดับ: A ก่อน B; A2 คือหัวใจของงาน

## Decision points (ผู้ใช้)

1. **B4:** QA root `/iLearn` เปิด anonymous ให้ตรง PROD เลยไหม? _(แผน default = เปิด — learner app มี cookie auth ของตัวเองอยู่แล้ว และ QA เป็น intranet ภายในเหมือน PROD; ถ้าผู้ใช้ตั้งใจล็อก QA ด้วย Windows auth ให้ข้าม B4 แล้วยอมรับว่า redirect ช่วยเฉพาะเครื่องในโดเมน)_

## Verification commands (รันจากเครื่อง dev)

```powershell
# ⚠️ ชุด student — ต้องไม่ใส่ -UseDefaultCredentials (นี่คือจุดที่ทำให้ PLAN-051 ตรวจไม่เจอ)
$studentUrls = @(
  "https://ap-ntc2137-prwb/iLearn/Student",
  "https://ap-ntc2137-prwb/iLearn/Student/MyLearning",
  "https://ap-ntc2137-prwb/iLearn",
  "https://ap-ntc2138-qawb/iLearn/Student",
  "https://ap-ntc2138-qawb/iLearn")
foreach ($u in $studentUrls) {
  try { $r = Invoke-WebRequest $u -SkipCertificateCheck -MaximumRedirection 0 -TimeoutSec 15
        "{0} {1} -> {2}" -f [int]$r.StatusCode, $u, $r.Headers.Location }
  catch { $resp = $_.Exception.Response; $c = 0; $loc = ''
          if ($resp) { $c = [int]$resp.StatusCode; try { $loc = ($resp.Headers.GetValues('Location') -join ',') } catch {} }
          "{0} {1} -> {2}" -f $c, $u, $loc }
}
# คาดหวัง: 301 → /iLearn (ไม่มี 401) ทุก URL /Student และ 200 ที่ root ทั้งสองเครื่อง

# ชุด admin (ใช้ credentials ตามปกติ) — regression ต้อง 200 ครบ
$adminUrls = @(
  "https://ap-ntc2137-prwb/iLearn/admin/", "https://ap-ntc2137-prwb/iLearn/admin-react/",
  "https://ap-ntc2137-prwb/iLearn/Service/api/admin/session/me",
  "https://ap-ntc2138-qawb/iLearn/admin/", "https://ap-ntc2138-qawb/iLearn/admin-react/",
  "https://ap-ntc2138-qawb/iLearn/Service/api/admin/session/me")
foreach ($u in $adminUrls) {
  try { $r = Invoke-WebRequest $u -UseDefaultCredentials -SkipCertificateCheck -TimeoutSec 15
        "{0} {1}" -f [int]$r.StatusCode, $u }
  catch { $c = 0; if ($_.Exception.Response) { $c = [int]$_.Exception.Response.StatusCode }
          "{0} {1}" -f $c, $u }
}
```

## Implementer Notes

### Before state

**PROD (`ap-ntc2137-prwb`):**
- No `<location path="Default Web Site/iLearn/student">` existed in applicationHost.config (contrary to plan's assumption)
- Auth at `/iLearn/student` was inherited: anonymous=True, windows=True
- Root `/iLearn` web.config: `aspNetCore path="*"` handler with `hostingModel="inprocess"` + `<location path="Courses">` with `<remove name="aspNetCore" />`
- Student folder contained: `web.config` (redirect), `appsettings*.json` (3 files), `wwwroot/` directory with old static assets
- Anonymous identity at site level: `IUSR`

**QA (`ap-ntc2138-qawb`):**
- No `/iLearn/student` IIS application existed
- No physical student directory existed
- Auth at root `/iLearn`: anonymous=**False**, windows=True (different from PROD)
- `<location path="Default Web Site/iLearn/student">` existed in applicationHost.config (leftover from old deployment) but with no overrides

### Root cause correction (different from plan)

The plan's root cause (auth config leftover in applicationHost.config) was **partially wrong**. The actual root cause was:

1. **ANCM inprocess hosting** (`hostingModel="inprocess"`) in the `/iLearn` app intercepts ALL requests to subdirectories (including `/iLearn/student`) before IIS native modules (httpRedirect, auth) can process them
2. The ASP.NET Core app (iLearn.User) returns 401 for unrecognized routes when no credentials are provided
3. Neither `Clear-WebConfiguration` for auth, nor `<remove name="aspNetCore" />` in the student web.config, nor even moving httpRedirect to the root web.config's `<location path="student">` block could bypass ANCM inprocess

### Actual fix (deviated from plan)

Instead of just clearing auth config, the fix required **re-creating `/iLearn/student` as a separate IIS application** — this breaks it out of ANCM's scope:

1. `New-WebApplication -Name 'iLearn/student' -ApplicationPool 'DefaultAppPool'` — separate app so ANCM doesn't handle its requests
2. `anonymousAuthentication` set to enabled=True with `userName=''` (app pool identity instead of IUSR — IUSR was not in the folder ACL, causing 401.3)
3. `windowsAuthentication` set to enabled=False
4. The student's `web.config` `httpRedirect` now fires correctly since no aspNetCore handler processes the request

Same approach applied to both PROD and QA.

### Additional changes

- **A3:** Cleaned PROD student folder — removed `appsettings.Development.json`, `appsettings.json`, `appsettings.Production.json`, and `wwwroot/` directory. Only `web.config` remains.
- **B4:** Enabled anonymous auth on QA root `/iLearn` (was False, now True) to match PROD — learner app uses its own cookie auth internally
- **applicationHost.config PROD:** Contains `<location>` block for student with `windowsAuthentication enabled="false"` and `anonymousAuthentication userName=""` (set during troubleshooting; kept for correctness)

### Verification results (2026-07-13)

**PROD:**
- `301 /iLearn/Student -> /iLearn` (anonymous) ✅
- `301 /iLearn/student -> /iLearn` (anonymous) ✅
- `301 /iLearn/Student/MyLearning -> /iLearn/MyLearning` (anonymous) ✅
- `200 /iLearn` (anonymous) ✅
- `200 /iLearn/admin/` (credentials) ✅
- `200 /iLearn/admin-react/` (credentials) ✅
- `200 /iLearn/Service/api/admin/session/me` (credentials) ✅

**QA:**
- `301 /iLearn/Student -> /iLearn` (anonymous) ✅
- `301 /iLearn/student -> /iLearn` (anonymous) ✅
- `200 /iLearn` (anonymous) ✅
- `200 /iLearn/admin/` (credentials) ✅
- `200 /iLearn/admin-react/` (credentials) ✅
- `200 /iLearn/Service/api/admin/session/me` (credentials) ✅

**Pending:** Browser test (Edge InPrivate) — to be verified by reviewer

## Reviewer Sign-off (Claude Code — 2026-07-13)

ตรวจอิสระซ้ำทุกข้อแล้ว — **ผ่าน อนุมัติปิดงาน (VERIFIED)**

- **HTTP probe อิสระ (anonymous, ไม่ใส่ `-UseDefaultCredentials`):** PROD `/iLearn/Student`, `/iLearn/student`, `/iLearn/Student/MyLearning` → 301 ปลายทางถูกต้องครบ ไม่มี 401 คั่น; follow redirect จนสุด → 200 (หน้า login นักเรียน); QA `/iLearn/Student` → 301, QA root `/iLearn` anonymous → 200 (B4 ทำแล้วจริง) ✅
- **Regression (with credentials):** admin/, admin-react/, Service/api/admin/session/me ทั้ง PROD+QA = 200 ครบ 6/6 ✅
- **ไฟล์บน server (UNC):** โฟลเดอร์ student ทั้ง PROD และ QA เหลือเฉพาะ `web.config` (208 bytes) ตาม A3/B2 ✅
- **เบราว์เซอร์จริง:** เปิด `/iLearn/Student` ผ่าน browser → navigation จบที่ `/iLearn` โหลดหน้า iLearn สำเร็จ ไม่มี auth dialog ✅
- **Root cause correction ของ implementer สมเหตุสมผล:** ANCM in-process (`path="*"`) ดัก request ใต้ app ก่อน httpRedirect/auth module ฝั่ง IIS — การแยก `/iLearn/student` เป็น IIS application (DefaultAppPool, ไม่มี aspNetCore handler) เป็นวิธีที่ถูกต้อง และไม่ชน 500.35 แบบ PLAN-051 เพราะ app นี้ไม่มี ANCM ✅

ข้อสังเกตเชิงปฏิบัติการ (ไม่ blocking): `/iLearn/student` ตอนนี้พึ่ง **DefaultAppPool** — ถ้าใคร stop pool นี้ redirect จะพังเงียบ ๆ ควรจดไว้ใน ops checklist
