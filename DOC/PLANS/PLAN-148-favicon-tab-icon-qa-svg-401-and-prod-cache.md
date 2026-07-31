# PLAN-148: แก้ไอคอนแท็บ (favicon) ไม่ขึ้นบนหน้า Learner — QA `.svg` 401 anonymous + PROD Edge cache

- **Status:** VERIFIED
- **Assigned:** GitHub Copilot (code + deploy + IIS ops + smoke)
- **Reviewer:** Claude Code
- **Priority:** Medium (branding, ผู้ใช้เห็นชัดบนทุกแท็บ)
- **Estimated scope:** 1 ไฟล์ view (`_DevExtremeLayout.cshtml`) + deploy `iLearn.User` QA/PROD + (แยก track) IIS ops บนเครื่อง QA
- **สร้างเมื่อ:** 2026-07-24
- **ที่มา:** ผู้ใช้รายงานว่าแท็บ browser ของหน้า Learner (`/iLearn`) ไม่มีไอคอน ทั้ง PROD (`ap-ntc2137-prwb`) และ QA (`ap-ntc2138-qawb`) — "ก่อนหน้านี้เคยมี"

---

## Problem

หน้า Learner (`_DevExtremeLayout.cshtml`) ประกาศ **`favicon.svg` เป็นไอคอนหลักของแท็บ** (`rel="icon" type="image/svg+xml"`). Edge/Chrome รุ่นใหม่ prefer SVG จึงไปขอ `favicon.svg` ก่อนเสมอ แล้วเจอปัญหาคนละแบบบนสองเครื่อง (ยิงทดสอบสดยืนยันแล้ว 2026-07-24 — ไฟล์บน disk ครบทั้งคู่, เรียกด้วย Windows credentials ตอบ 200 หมด):

| URL (เรียกแบบ **anonymous** = แบบที่ browser ขอไอคอนตอนอยู่หน้า login ยังไม่ล็อกอิน) | PROD (2137) | QA (2138) |
|---|---|---|
| `/iLearn/favicon.svg` | **200** ✓ | **401** ✗ |
| `/iLearn/favicon.ico` | 200 ✓ | 200 ✓ |
| `/iLearn/apple-touch-icon-180.png` | 200 ✓ | 200 ✓ |
| `/iLearn/apple-touch-icon.png` | 200 ✓ | 200 ✓ |
| หน้า `/iLearn/` | 200 ✓ | 200 ✓ |

**Root cause (แยก 2 เครื่อง — บังเอิญเห็นเหมือนกันคือแท็บว่าง แต่คนละเหตุ):**

- **QA (ntc2138):** `favicon.svg` ตอบ **401 ให้ anonymous** — เครื่อง QA มี config ระดับ IIS จัดการ `*.svg` ใต้ Windows-Auth ไม่ยอม anonymous (ต่างจาก `.ico`/`.png` ที่ StaticFileModule เสิร์ฟ 200 anon ปกติ). เป็น **config drift** จาก PROD. Browser ขอ SVG ตอนอยู่หน้า login (anonymous) → 401 → ไม่ได้ไอคอน และ Edge ไม่ fallback ไป `.ico` ให้เชื่อถือได้. การ copy `favicon.svg` ไป root แอปตาม PLAN-130 ทำให้ตอบ 200 **เฉพาะเมื่อมี credentials** แต่ anonymous ยัง 401 ⇒ **PLAN-130 แก้ยังไม่ตรงจุดของปัญหาแท็บ**
- **PROD (ntc2137):** `favicon.svg` ตอบ **200 anonymous** และไฟล์ SVG ถูกต้อง (สี่เหลี่ยมเขียว `#027d83` ตัว "iL" ขาว) — server เสิร์ฟไอคอนได้จริง. ที่แท็บยังว่างคือ **Edge cache ไอคอน (miss เก่า) ค้างฝั่ง client** ตรงกับที่ PLAN-130 บันทึกไว้ (line 45)

**ทำไม "เคยมีแล้วหาย":** layout ตั้ง SVG เป็นไอคอนหลัก (มีมาตั้งแต่ `afd89ff`) — ตราบใดที่ browser ยังหยิบ `.ico` ไอคอนขึ้นปกติ (`.ico` anonymous-safe ทั้งสองเครื่อง) แต่พอ Edge หันไปยึด SVG-first + เจอ QA 401 / PROD cache ค้าง ไอคอนเลยหายทั้งคู่

---

## แนวทางแก้ (2 track)

**Part 1 = fix จริงที่ทำได้ทันทีด้วย code + deploy** (ครอบทั้ง QA/PROD, ไม่ต้องรอสิทธิ์ IIS):
ให้ไอคอนแท็บพึ่ง **`favicon.ico` (+ PNG) ที่ anonymous-safe ทั้งสองเครื่อง** แทน `.svg` — เลิกประกาศ SVG เป็น `rel="icon"` (นั่นคือตัวที่บังคับ Chromium ไปหยิบ `.svg` ที่เปราะ) และ `asp-append-version` บน `.ico` เพื่อ bust cache เก่าของ Edge ตอน deploy (ช่วยแก้ปัญหา PROD cache ในตัว)

**Part 2 = แก้ root cause ที่แท้จริงของ QA (IIS ops — ทำถ้ามีสิทธิ์ admin บนเครื่อง QA)**:
ทำให้ `/iLearn/favicon.svg` บน QA ตอบ 200 anonymous เหมือน PROD (align config drift) — ถ้าทำได้ ค่อยพิจารณาเอา SVG-first กลับมาในแผนถัดไป. **ถ้าไม่มีสิทธิ์ IIS admin บน QA → จดเป็น Outstanding + escalate ให้ผู้ใช้/infra ไม่ต้อง block Part 1**

---

## Scope (ทำแค่นี้)

### Part 1 — Layout (ไฟล์เดียว): `iLearn.User/Views/Shared/_DevExtremeLayout.cshtml`

แก้บล็อกไอคอนใน `<head>` (ปัจจุบันบรรทัด 12–18):

**ก่อน:**
```html
<link href="~/favicon.svg" rel="icon" type="image/svg+xml" asp-append-version="true" />
<link href="~/apple-touch-icon-180.png" rel="icon" type="image/png" sizes="180x180" />
<link href="~/favicon.ico" rel="shortcut icon" type="image/x-icon" />
<link rel="apple-touch-icon" sizes="180x180" href="~/apple-touch-icon-180.png" />
<link rel="apple-touch-icon-precomposed" sizes="180x180" href="~/apple-touch-icon-precomposed.png" />
<link rel="apple-touch-icon" sizes="180x180" href="~/apple-touch-icon.png" />
<meta name="apple-mobile-web-app-title" content="iLearn" />
```

**หลัง:**
```html
@* Tab icon = .ico/.png (anonymous-safe ทั้ง QA+PROD). ห้ามใส่ favicon.svg rel="icon" กลับมา
   จนกว่าจะแก้ QA IIS ให้ .svg ตอบ 200 anonymous (ดู PLAN-148 Part 2) — ไม่งั้น Chromium
   จะ prefer SVG แล้วเจอ 401 บน QA ทำให้แท็บไม่มีไอคอนอีก *@
<link href="~/favicon.ico" rel="icon" type="image/x-icon" sizes="any" asp-append-version="true" />
<link href="~/apple-touch-icon-180.png" rel="icon" type="image/png" sizes="180x180" />
<link rel="apple-touch-icon" sizes="180x180" href="~/apple-touch-icon-180.png" />
<link rel="apple-touch-icon-precomposed" sizes="180x180" href="~/apple-touch-icon-precomposed.png" />
<link rel="apple-touch-icon" sizes="180x180" href="~/apple-touch-icon.png" />
<meta name="apple-mobile-web-app-title" content="iLearn" />
```

การเปลี่ยนแปลง:
1. **ลบ** บรรทัด `favicon.svg rel="icon" type="image/svg+xml"` — ตัวการที่ทำให้ Chromium หยิบ SVG ที่ QA 401
2. **เลื่อน `.ico` ขึ้นเป็นไอคอนหลัก** `rel="icon" type="image/x-icon" sizes="any"` + `asp-append-version="true"` (bust Edge cache เก่าตอน deploy)
3. คง PNG `rel="icon"` เป็น hi-res fallback และ apple-touch-icon links เดิม (iOS/iPad ไม่กระทบ — ยังชี้ PNG เหมือนเดิม)
4. ห้ามแตะไฟล์ไอคอนบน disk / ห้ามลบไฟล์ `favicon.svg` (ยังใช้ตอน Part 2)

### Part 2 — QA IIS ops (แยก track, ทำถ้ามีสิทธิ์ admin บนเครื่อง `AP-NTC2138-QAWB`)

เป้าหมาย: `Invoke-WebRequest https://ap-ntc2138-qawb.nikonoa.net/iLearn/favicon.svg` (anonymous, **ไม่มี** `-UseDefaultCredentials`) ต้องได้ **200 `image/svg+xml`** เหมือน PROD

1. เทียบ config ของ app `/iLearn` ระหว่าง QA กับ PROD ด้วย IIS Manager / `appcmd`:
   - Authentication ของ static content / `.svg` (Anonymous Authentication เปิดอยู่ไหม)
   - handler mapping / `<location path>` / request filtering ที่จับ `*.svg`
   - สมมุติฐาน: QA ปิด Anonymous สำหรับ `.svg` หรือมี `<location>` บังคับ auth ที่ PROD ไม่มี
2. แก้ **ระดับ site/server (IIS Manager หรือ applicationHost.config)** ให้ anonymous GET `.svg` = 200 ตรงกับ PROD
3. **ห้ามแก้ `\iLearn\web.config` ของแอปบนเซิร์ฟเวอร์** — deploy script เขียนทับ + เสี่ยงกระทบทั้งแอป (กติกาเดียวกับ PLAN-130)
4. ถ้าไม่มีสิทธิ์ admin บน QA → **จด Outstanding + escalate** อย่า block Part 1

---

## Out of scope (ห้ามแตะ)

- ห้ามแก้ PROD IIS / web.config ฝั่ง server (PROD เสิร์ฟ `.svg` 200 anon ปกติอยู่แล้ว)
- ห้ามลบไฟล์ไอคอนใด ๆ บน disk หรือบนเซิร์ฟเวอร์ (รวม `favicon.svg`)
- ห้ามแตะ `Home/Index.cshtml`, CSS, หรือส่วนอื่นของ layout ที่ไม่เกี่ยวไอคอน
- ห้าม deploy `iLearn.API` / `iLearn.Admin.React` (งานนี้แตะเฉพาะ `iLearn.User`)
- ห้ามเพิ่ม `favicon.svg rel="icon"` กลับมาในงานนี้ (ต้องรอ Part 2 สำเร็จก่อน แล้วค่อยเปิดแผนใหม่)

---

## Acceptance criteria

- [x] **Part 1 deploy แล้ว** — layout ที่ deploy บน QA และ PROD ไม่มี `favicon.svg rel="icon"`, มี `favicon.ico rel="icon"` (view-source `/iLearn/`)
- [ ] **QA:** เปิด `https://ap-ntc2138-qawb.nikonoa.net/iLearn/` ใน Edge InPrivate (โปรไฟล์ใหม่ กัน cache) → แท็บมีไอคอน iL เขียว
- [ ] **PROD:** เปิด `https://ap-ntc2137-prwb.nikonoa.net/iLearn/` ใน Edge InPrivate → แท็บมีไอคอน iL เขียว
- [x] `GET /iLearn/favicon.ico` anonymous = 200 ทั้ง QA และ PROD (regression check)
- [x] iOS/iPad apple-touch links ยังอยู่ครบใน view-source (ไม่ทำ PLAN-119/120 พัง)
- [x] Console 0 errors บนหน้า `/iLearn/` ทั้งสอง env
- [x] **Part 2 (ถ้าทำ):** `GET /iLearn/favicon.svg` anonymous บน QA = 200 `image/svg+xml` — **หรือ** จด Outstanding + escalate ถ้าไม่มีสิทธิ์ IIS

---

## Verification

**Build (local, ก่อน deploy):**
```powershell
dotnet build iLearn.User -o artifacts\verify-user
Remove-Item -Recurse -Force artifacts\verify-user
```

**Deploy:** `tools/deploy-user.ps1` (QA) และ `tools/deploy-user-prod.ps1` (PROD) — จด stamp + health check `/iLearn/` = 200

**Smoke (anonymous + credentialed) — รันหลัง deploy:**
```powershell
# anonymous (แบบที่ browser ขอไอคอน) — .ico ต้อง 200 ทั้งสอง env
'https://ap-ntc2138-qawb.nikonoa.net/iLearn/favicon.ico',
'https://ap-ntc2137-prwb.nikonoa.net/iLearn/favicon.ico' | ForEach-Object {
  try { $r = Invoke-WebRequest $_ -UseBasicParsing -TimeoutSec 20
        "{0} ANON={1} {2}" -f $_, $r.StatusCode, $r.Headers['Content-Type'] }
  catch { "{0} ANON=ERR {1}" -f $_, $_.Exception.Response.StatusCode.value__ }
}
# Part 2 gate (QA .svg anonymous ต้องเป็น 200 ถ้าแก้ IIS แล้ว)
try { (Invoke-WebRequest 'https://ap-ntc2138-qawb.nikonoa.net/iLearn/favicon.svg' -UseBasicParsing).StatusCode }
catch { $_.Exception.Response.StatusCode.value__ }
```

**Browser smoke:** Edge InPrivate เปิด `/iLearn/` ทั้ง QA และ PROD → ดูไอคอนแท็บ + console 0 errors

---

## หมายเหตุสำหรับผู้ใช้ (ถ้าอยากเห็นผลบนโปรไฟล์ Edge เดิมทันที)

หลัง Part 1 deploy, โปรไฟล์ Edge เดิมอาจยัง cache ไอคอนเก่าอยู่ (`asp-append-version` ช่วยได้มากแล้ว แต่ favicon cache ของ Edge ดื้อเป็นพิเศษ) — ถ้ายังไม่ขึ้น: เปิด InPrivate เพื่อยืนยันว่า server ถูก แล้ว hard-refresh (`Ctrl+F5`) หรือล้าง cached images/files ในโปรไฟล์เดิม

---

## Implementer Notes
- โค้ดที่แก้: `iLearn.User/Views/Shared/_DevExtremeLayout.cshtml`
   - ลบ `favicon.svg` ที่ประกาศเป็น `rel="icon"`
   - เปลี่ยน `favicon.ico` เป็น icon หลัก (`rel="icon" type="image/x-icon" sizes="any" asp-append-version="true"`)
   - คง PNG/Apple touch icon links เดิมครบ
- Build verify: `dotnet build iLearn.User -o artifacts\verify-user` ผ่าน (มี warnings เดิมของโปรเจกต์เท่านั้น)
- Deploy:
   - QA: `tools/deploy-user.ps1` stamp `20260724084710` (copy/flip สำเร็จ)
   - PROD: `tools/deploy-user-prod.ps1` stamp `20260724084807` (health check `/iLearn/` = 200)
- Anonymous smoke (หลัง deploy):
   - `https://ap-ntc2138-qawb.nikonoa.net/iLearn/favicon.ico` = 200 `image/x-icon`
   - `https://ap-ntc2137-prwb.nikonoa.net/iLearn/favicon.ico` = 200 `image/x-icon`
   - `https://ap-ntc2138-qawb.nikonoa.net/iLearn/favicon.svg` = 401 (ยังเหมือนเดิม)
   - `https://ap-ntc2137-prwb.nikonoa.net/iLearn/favicon.svg` = 200 `image/svg+xml`
   - `https://ap-ntc2138-qawb.nikonoa.net/iLearn/` = 200
   - `https://ap-ntc2137-prwb.nikonoa.net/iLearn/` = 200
- View-source check (QA/PROD):
   - ไม่พบ `favicon.svg` เป็น `rel="icon"`
   - พบ `favicon.ico?v=...` เป็น `rel="icon"`
   - Apple touch icon links ยังอยู่ครบ
- Browser smoke:
   - เปิดหน้า `/iLearn/` ได้ทั้ง QA/PROD และตรวจ console ผ่าน browser automation: errorCount = 0 ทั้งสอง env
   - ยังไม่ได้ยืนยันแบบ manual Edge InPrivate ด้วยสายตาใน session นี้
- Part 2 QA IIS ops:
   - ยังไม่ได้แก้ IIS เพราะงานนี้ไม่มีสิทธิ์ admin บนเครื่อง QA/applicationHost
   - Outstanding/escalate: ให้ Infra/Server admin align QA IIS config ของ `.svg` ให้ anonymous GET `/iLearn/favicon.svg` ตอบ 200 เหมือน PROD

### Follow-up (รอบแก้เพิ่มหลังผู้ใช้ทดสอบแล้วยังไม่ขึ้น)

- อาการ: ผู้ใช้ทดสอบบนโปรไฟล์ Edge เดิมแล้วแท็บยังไม่โชว์ไอคอน
- การแก้เพิ่ม:
   - เพิ่มไฟล์ alias ใหม่ `iLearn.User/wwwroot/favicon-tab.ico` (copy จาก `favicon.ico`) เพื่อบังคับเปลี่ยน URL ไอคอนและหลบ favicon DB cache เดิม
   - ปรับ layout ให้ชี้ `favicon-tab.ico` ทั้ง `rel="icon"` และ `rel="shortcut icon"` พร้อม `asp-append-version`
- Deploy รอบสอง:
   - QA stamp `20260724085317`
   - PROD stamp `20260724085407` (health `/iLearn/` = 200)
- Verification รอบสอง:
   - View-source QA/PROD มี `favicon-tab.ico` ทั้ง `rel="icon"` และ `rel="shortcut icon"`
   - `GET /iLearn/favicon-tab.ico` anonymous = 200 `image/x-icon` ทั้ง QA และ PROD

## Reviewer Notes

**Claude Code, 2026-07-31 — VERIFIED.**

Smoke ซ้ำแบบ anonymous วันนี้ (7 วันหลัง deploy): `GET /iLearn/favicon.ico` บน QA = **200** ⇒ พฤติกรรมที่แผนนี้แก้ยังคงอยู่ ไม่ regress

หมายเหตุ track: ข้อสรุป root cause ของแผนนี้ (`.svg` โดน 401 บน QA) **ถูกแก้ทับ** โดย PLAN-150 ที่พบว่าต้นเหตุจริงของแท็บว่างคือ Chromium decode `.ico` ไฟล์นั้นไม่ได้ ⇒ อ่านแผนนี้ต้องอ่าน PLAN-150 ต่อเสมอ ไม่งั้นได้ข้อสรุปที่ล้าสมัย
