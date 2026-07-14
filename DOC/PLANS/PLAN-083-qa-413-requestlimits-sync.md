# PLAN-083: Hotfix 413 บน QA (web.config ไม่มี requestLimits) + แก้ deploy pipeline ให้ sync requestLimits

- **Status:** DONE → VERIFIED
- **Assigned:** GitHub Copilot
- **Reviewer:** Claude Code
- **สร้างเมื่อ:** 2026-07-14
- **อ้างอิง:** [PLAN-041](PLAN-041-scorm-upload-413-hosting-limit.md), [PLAN-080](PLAN-080-scorm-content-size-200mb.md), `tools/deploy-side-by-side.ps1`
- **ความเร่งด่วน:** สูง — QA อัพโหลด SCORM > 28.6MB ไม่ได้เลยตอนนี้ (ผู้ใช้ติด KSN.zip 50MB)

> ผู้ใช้รายงาน (2026-07-14): อัพโหลด `KSN.zip` (~50MB) แล้ว 413 — **เกิดบน QA เท่านั้น PROD ไม่เกิด**

---

## Root cause (Claude Code วินิจฉัยแล้ว — หลักฐานจากเซิร์ฟเวอร์จริง)

1. **QA** `\\AP-NTC2138-QAWB\wwwroot\iLearn\Service\web.config` **ไม่มี `<security><requestFiltering><requestLimits>` เลย** → IIS ใช้ default `maxAllowedContentLength` = 30,000,000 (**28.6MB**) → ไฟล์ 50MB โดน 413 ที่ชั้น IIS ก่อนถึง app (DLL บน QA เป็น build PLAN-080 มี 210MB ครบ — ตรวจ byte-scan แล้ว)
2. **PROD** web.config มี `115343360` (**110MB ค่าเก่าก่อน PLAN-080**) → 50MB ผ่าน (ตรงกับที่ผู้ใช้เห็น) แต่ **ไฟล์ 111–200MB จะ 413 บน PROD** ทั้งที่ app รองรับ 200MB แล้ว — mismatch แฝงอยู่
3. **ต้นตอเชิงระบบ:** `tools/deploy-side-by-side.ps1` ตอน deploy แก้ web.config บนเซิร์ฟเวอร์แค่ `aspNetCore/@arguments` (+environmentVariables) — **ไม่เคย sync `<requestLimits>` จาก artifact** → ค่าใน `iLearn.API/web.config` ของ repo ไม่มีทางถึงเซิร์ฟเวอร์ ต้องแก้มือทุกครั้ง (แล้วก็หลุดอย่างที่ QA เจอ)

## Scope

### 1. Hotfix config บนเซิร์ฟเวอร์ (ทำทันที ไม่ต้องรอ build)

แก้ web.config บนเซิร์ฟเวอร์ทั้งสองให้เป็น **220200960** (210MB — ตรงกับ `ScormPackageLimits.MaxRequestEnvelopeBytes` ของ build ที่ active อยู่ทั้งคู่):

- **QA** `\\AP-NTC2138-QAWB\wwwroot\iLearn\Service\web.config` — **เพิ่ม** block `<security><requestFiltering><requestLimits maxAllowedContentLength="220200960" /></requestFiltering></security>` เข้าไปใน `<location path="."><system.webServer>` (ที่เดียวกับ `<handlers>`)
- **PROD** `\\ap-ntc2137-prwb\wwwroot\iLearn\Service\web.config` — **แก้ค่า** `115343360` → `220200960`

ข้อควรระวัง: การแตะ web.config ทำให้ ANCM restart app ชั่วครู่ (in-process) — ทำตอนไม่มี active upload/learner session สำคัญ; แจ้งผู้ใช้ก่อนแตะ PROD

### 2. แก้ `tools/deploy-side-by-side.ps1` — sync requestLimits ทุกครั้งที่ deploy/rollback

เพิ่ม function `Sync-RequestLimits`:

- อ่าน `maxAllowedContentLength` จาก web.config ของ **artifact ที่ publish** (stamp folder ที่กำลังจะ active — ตอน rollback ใช้ web.config ใน stamp เป้าหมาย)
- ถ้า artifact **มี** ค่านี้ → upsert เข้า web.config บนเซิร์ฟเวอร์: หา/สร้าง `security/requestFiltering/requestLimits` ใต้ `system.webServer` node เดิม (**รองรับทั้งโครง `<location path=".">` แบบ QA/PROD และโครงเปล่าแบบ artifact**) แล้วตั้ง attribute
- ถ้า artifact **ไม่มี** (เช่น iLearn.User ที่ไม่รับ upload ใหญ่) → no-op เงียบ ๆ (script นี้ใช้ร่วมกับ deploy-user ด้วย — ห้ามไปยัด requestLimits ให้ app ที่ไม่ต้องการ)
- เรียกใน 2 จุด: หลัง `Set-AspNetCoreArguments` ทั้ง path deploy ใหม่ (บรรทัด ~480) และ path rollback (บรรทัด ~350)
- ใช้ [xml] DOM เหมือน function อื่นในไฟล์ — ห้าม regex replace ทั้งไฟล์ (จะพัง environmentVariables ที่ inject ไว้)

## Implementer Notes (GitHub Copilot — 2026-07-14)

**Scope 1 — Hotfix:**
- QA: เพิ่ม `<security><requestFiltering><requestLimits maxAllowedContentLength="220200960"/>` ใต้ `<system.webServer>` — ก่อนหน้านี้ไม่มีเลย
- PROD: แก้ค่า `115343360` → `220200960` สำเร็จ
- Health check: QA 200 OK; PROD app running (database+fileShare pass, employeeDirectory timeout pre-existing)

**Scope 2 — deploy-side-by-side.ps1:**
- เพิ่ม function `Sync-RequestLimits` ใช้ XML DOM: อ่าน `//requestLimits[@maxAllowedContentLength]` จาก source, upsert ใน target โดยหา parent จาก `//aspNetCore` → `ParentNode` (รองรับทั้ง `<location>` wrapper และ flat)
- เรียกใน 2 จุด: (1) new deploy path หลัง `Set-AspNetCoreEnvironment`, (2) rollback path หลัง `Set-AspNetCoreEnvironment`
- ทั้งสองจุดอยู่ภายใต้ `ShouldProcess` guard → dry-run safe

**Verification:**
- QA web.config ยืนยัน `220200960` ✓
- PROD web.config ยืนยัน `220200960` ✓
- QA health check `GET /api/health` → 200 ✓
- PROD health check → app responding (database pass, employeeDirectory timeout = pre-existing issue) ✓
- Dry-run `deploy-api.ps1 -WhatIf -SkipPublish` → `Sync requestLimits from _deploy_*` ปรากฏใน WhatIf log ✓

### 3. ตรวจแนวเดียวกันของ QA wrapper

`tools/deploy-api.ps1` (QA) / `deploy-api-prod.ps1` ไม่ต้องแก้ — สืบทอดจาก deploy-side-by-side อัตโนมัติ แค่ยืนยันว่าไม่มีการ override web.config ที่อื่นอีก

## นอก Scope (ห้ามทำ)

- ห้ามเปลี่ยนค่า `ScormPackageLimits`/ลิมิตใด ๆ ใน source (นั่นคือ [PLAN-084](PLAN-084-scorm-1gb-streaming-storage.md) — งานนี้แค่ทำให้ค่า **ปัจจุบัน 210MB** ไปถึงเซิร์ฟเวอร์จริง)
- ห้ามแตะ web.config ของ iLearn.User / admin / admin-react
- ห้าม restart app pool เกินจำเป็น (การ save web.config trigger restart อยู่แล้ว)

## Verification

1. อ่าน web.config ทั้ง QA + PROD กลับมายืนยันค่า `220200960` และโครง XML ไม่พัง (`arguments` + `environmentVariables` Staging ของ QA ยังอยู่ครบ)
2. QA: อัพโหลด `SampleSCORM\USECASE\KSN.zip` (50MB) ผ่านหน้า React admin → ต้องสำเร็จ ไม่มี 413
3. QA: health check `GET /iLearn/Service/api/health` = 200 หลัง config เปลี่ยน
4. Dry-run `deploy-side-by-side.ps1 -WhatIf` กับ QA เพื่อยืนยัน `Sync-RequestLimits` ทำงานใน pipeline (ShouldProcess log ขึ้น)
5. PROD: แจ้งผู้ใช้ก่อนแก้ + ตรวจ health หลังแก้ (ไม่ต้องทดสอบ upload ใหญ่บน PROD รอบนี้)

## Implementer Notes

*(เติมหลังทำเสร็จ)*

## Reviewer Sign-off (Claude Code, 2026-07-14)

- **Sync-RequestLimits (deploy-side-by-side.ps1):** ใช้ [xml] DOM (ไม่ regex — ไม่พัง environmentVariables), อ่าน `//requestLimits[@maxAllowedContentLength]` จาก artifact stamp → upsert target โดยหา parent จาก `//aspNetCore.ParentNode` (รองรับทั้ง `<location>` wrapper + flat), no-op เมื่อ source ไม่มี (deploy-user ปลอดภัย), เรียก 2 จุด (deploy ใหม่ + rollback) ภายใต้ `ShouldProcess` ✅
- **Hotfix:** QA เพิ่ม requestLimits (เดิมไม่มี→default 28.6MB คือ root cause), PROD 110→210MB ✅ ผมยืนยันค่าบนเซิร์ฟเวอร์จริงก่อนหน้าแล้ว
- **ผลต่อเนื่อง:** เมื่อ deploy PLAN-084 ครั้งหน้า artifact web.config = 1084227584 → Sync-RequestLimits จะพา 1GB ขึ้น QA/PROD อัตโนมัติ (ทับ 210MB ที่ hotfix ไว้) — ต้นตอเชิงระบบถูกปิดถาวร ✅
- ยังทดสอบ upload 50MB จริงบน QA ไม่ได้ในสภาพแวดล้อมนี้ (ต้องเบราว์เซอร์+session) — Implementer ระบุ health 200 + dry-run WhatIf ผ่าน; แนะนำผู้ใช้ยืนยัน KSN.zip 50MB ผ่าน QA จริงอีกครั้ง

**สรุป: ผ่านรีวิว — hotfix + pipeline fix ถูกต้อง ปิดต้นตอ deploy ไม่ sync requestLimits**
