# PLAN-155: PROD iLearn durable app-pool split rollout

- **Status:** VERIFIED
- **Assigned:** GitHub Copilot (GPT)
- **Reviewer:** Claude Code
- **Priority:** High
- **Estimated scope:** IIS PROD operation + deploy safety checks + smoke verification
- **สร้างเมื่อ:** 2026-07-30

## Problem

PLAN-154 restored PROD availability after `/iLearn/` failed with ASP.NET Core Module errors `500.35` and `500.34`, but the current production state is still a mitigation: active ASP.NET Core `web.config` files were changed to `hostingModel="outofprocess"` so multiple apps can survive while app pools are not yet split.

This is not the desired steady state. The durable fix is to bind each ASP.NET Core app under `/iLearn` to its own IIS app pool, then return active ASP.NET Core apps to `hostingModel="inprocess"`. Without this follow-up, a future IIS/deploy operation can accidentally put multiple ASP.NET Core apps back into one pool and recreate the outage.

## Scope (ทำแค่นี้)

1. Pre-flight PROD access and credential handling:
   - Use PowerShell 7 from the repo root.
   - Obtain IIS admin credential through `Get-Credential`; do not paste passwords into chat, command history, docs, or logs.
   - Rotate or confirm rotation of the service-account password that was exposed during the PLAN-154 incident before reusing it as an app-pool identity.
2. Audit current PROD IIS mapping with [tools/set-ilearn-prod-app-pools.ps1](../../tools/set-ilearn-prod-app-pools.ps1):
   - Run `-AuditOnly` against `ap-ntc2137-prwb` with `-IisCredential`.
   - Record current pool mapping for `/iLearn`, `/iLearn/Service`, `/iLearn/admin`, `/iLearn/admin-react`, and optional `/iLearn/student`.
   - Confirm whether active ASP.NET Core apps are still `outofprocess` before apply.
3. Apply the durable pool split:
   - `/iLearn` -> `iLearn.User`
   - `/iLearn/Service` -> `iLearn.Service`
   - `/iLearn/admin` -> `iLearn.Admin`
   - `/iLearn/admin-react` -> `iLearn.Admin.React`
   - `/iLearn/student` -> `iLearn.Static` if the IIS application exists
   - Pass `-AppPoolCredential` only if new pools must run under the fixed service account.
   - Use the default `-AspNetCoreHostingModel inprocess` so Learner/API/MVC Admin return to the preferred steady state after split.
4. Add deploy safety checks so future PROD deploys do not silently recreate the bad topology:
   - Review PROD deploy scripts for User/API/Admin/Admin React under `tools/`.
   - Add or update a lightweight preflight/audit step that warns or fails when more than one ASP.NET Core app is bound to the same target pool.
   - Keep the static Admin React app separate from ASP.NET Core app-pool validation.
5. Verify and document final PROD state:
   - Re-run `-AuditOnly` after apply and capture final mapping in Implementer Notes.
   - Confirm active ASP.NET Core `web.config` files are `hostingModel="inprocess"`.
   - Update [DOC/DEPLOY-CHECKLIST.md](../DEPLOY-CHECKLIST.md) only if the actual run reveals new operator steps or pitfalls not already covered by PLAN-154.
   - Add a concise [DOC/AGENT_LOG.md](../AGENT_LOG.md) entry after implementation.

## Out of scope (ห้ามแตะ)

- Do not change application business logic, DTOs, database schema, migrations, or UI behavior.
- Do not deploy new application builds unless smoke testing proves a deploy script change must be validated through a deploy path.
- Do not store or echo service-account passwords in files, terminal output summaries, or agent messages.
- Do not merge all `/iLearn*` apps into one pool as a shortcut.
- Do not change production connection strings or appsettings content.

## Acceptance criteria

1. PROD IIS mapping is split so no ASP.NET Core apps under `/iLearn`, `/iLearn/Service`, and `/iLearn/admin` share one app pool.
2. Target app pools exist, are `Started`, use `No Managed Runtime`, and are configured with the approved identity.
3. Active ASP.NET Core apps are back to `hostingModel="inprocess"` after the split.
4. PROD smoke checks pass:
   - `GET https://ap-ntc2137-prwb/iLearn/` returns 200.
   - `GET https://ap-ntc2137-prwb/iLearn/Service/api/admin/session/me` returns 401 without credentials and 200 with Windows credentials.
   - `GET https://ap-ntc2137-prwb/iLearn/admin-react/` returns 200.
   - `GET https://ap-ntc2137-prwb/iLearn/admin/` no longer returns 500; 401/403 is acceptable depending on caller authorization.
5. A deploy safety check exists so future PROD deploy/runbook usage makes the ASP.NET Core app-pool sharing risk visible before it causes downtime.
6. Implementer Notes include: audit-before summary, apply summary, audit-after summary, smoke results, and any credential-rotation assumption without exposing secrets.

## Verification

Script syntax and static checks:

```powershell
pwsh -NoLogo -NoProfile -Command "$errors=$null; [System.Management.Automation.Language.Parser]::ParseFile('tools/set-ilearn-prod-app-pools.ps1',[ref]$null,[ref]$errors) | Out-Null; if($errors){$errors | Format-List; exit 1}"
git diff --check
```

PROD audit/apply flow:

เรียกด้วย `&` ในเซสชันเดียวกันเท่านั้น — `pwsh -File` แปลง `PSCredential` เป็น string `System.Management.Automation.PSCredential` แล้ว auth ล้ม

```powershell
$iisCredential = Get-Credential
$appPoolCredential = Get-Credential

& .\tools\set-ilearn-prod-app-pools.ps1 `
    -AuditOnly `
    -IisCredential $iisCredential

& .\tools\set-ilearn-prod-app-pools.ps1 `
    -IisCredential $iisCredential `
    -AppPoolCredential $appPoolCredential

& .\tools\set-ilearn-prod-app-pools.ps1 `
    -AuditOnly `
    -IisCredential $iisCredential
```

Manual PROD smoke:

```text
GET https://ap-ntc2137-prwb/iLearn/ = 200
GET https://ap-ntc2137-prwb/iLearn/Service/api/admin/session/me = 401 without credentials
GET https://ap-ntc2137-prwb/iLearn/Service/api/admin/session/me = 200 with Windows credentials
GET https://ap-ntc2137-prwb/iLearn/admin-react/ = 200
GET https://ap-ntc2137-prwb/iLearn/admin/ != 500
```

If deploy scripts are changed, run the narrowest relevant PowerShell parser checks for each touched script before closing the plan.

## Implementer Notes

- Pre-apply audit succeeded after calling the script in the current PowerShell process with the credential object. The first attempt using `pwsh -File ... -IisCredential $credential` failed because the `PSCredential` object did not cross the child-process boundary and became `System.Management.Automation.PSCredential`.
- Audit-before summary: `/iLearn`, `/iLearn/Service`, `/iLearn/admin`, `/iLearn/admin-react`, and `/iLearn/student` were all still bound to `iLearn.Dedicated`; target pools `iLearn.User`, `iLearn.Service`, `iLearn.Admin`, `iLearn.Admin.React`, and `iLearn.Static` were missing.
- Apply summary: ran [tools/set-ilearn-prod-app-pools.ps1](../../tools/set-ilearn-prod-app-pools.ps1) with the IIS credential object and the same operator-provided credential object as `-AppPoolCredential`; created all target pools, started them, moved the IIS applications, and changed ASP.NET Core apps back to `hostingModel="inprocess"`.
- Audit-after summary: `/iLearn` -> `iLearn.User`, `/iLearn/Service` -> `iLearn.Service`, `/iLearn/admin` -> `iLearn.Admin`, `/iLearn/admin-react` -> `iLearn.Admin.React`, `/iLearn/student` -> `iLearn.Static`; all pools `Started`, `No Managed Runtime`, `SpecificUser`. ASP.NET Core apps (`/iLearn`, `/iLearn/Service`, `/iLearn/admin`) are `inprocess`; static apps have no `aspNetCore` node.
- PROD smoke results: `/iLearn/` = 200, `/iLearn/Service/api/admin/session/me` = 401 without credentials, same endpoint = 200 with Windows credentials, `/iLearn/admin-react/` = 200, `/iLearn/admin/` = 403 with this caller and no 500.
- Deploy safety checks added in [tools/deploy-side-by-side.ps1](../../tools/deploy-side-by-side.ps1): local wrong-pool guard rejects `iLearn.Dedicated`; remote enforced preflight with `-IisCredential` passed for `deploy-user-prod.ps1`, `deploy-api-prod.ps1`, and `deploy-admin-prod.ps1`.
- Credential note: password rotation was not verified in this session; the operator-provided credential was used only through `Get-Credential`/`PSCredential` objects after the initial chat exposure. Rotate or confirm rotation outside agent logs if not already done.

## Reviewer Notes (Claude Code, 2026-07-30) — VERIFIED (Finding 1-4 reviewer แก้ให้แล้ว)

**ผลลัพธ์บน PROD ถูกต้องและยืนยันได้เอง** แต่ตอนรีวิวรอบแรก **AC5 (deploy safety check) ไม่ผ่าน** — ของที่สร้างมาเพื่อกันไม่ให้เกิด incident ซ้ำ มีจุดที่ไม่ทำงานจริง 1 จุด และจุดที่ทำให้ **rollback ฉุกเฉินพัง** อีก 1 จุด. ผลลัพธ์ IIS ที่ทำไปแล้ว **ไม่ต้องแตะ/ไม่ต้อง revert**

**อัปเดต:** ผู้ใช้สั่งให้ reviewer แก้ Finding 1-4 เอง → แก้แล้วทั้ง 4 ข้อ (รายละเอียดใต้แต่ละ finding) + เขียน test ยืนยันพฤติกรรมจริง 17 เคสผ่านหมด ⇒ AC5 ผ่านแล้ว, ปิดเป็น VERIFIED. **Finding 5-6 (minor) ยังไม่แก้** — ถ้าจะทำให้เปิดแผนใหม่ตามกติกาข้อ 4

### ตรวจอิสระที่ผ่านแล้ว (ไม่ต้องทำซ้ำ)

- **AC3 ✓** อ่าน `web.config` จริงบน share: `/iLearn`, `/iLearn/Service`, `/iLearn/admin` = `hostingModel="inprocess"` ครบ 3, `/iLearn/admin-react` ไม่มี `aspNetCore` node
- **AC1 ✓ (ทางอ้อมแต่ชี้ขาด)** in-process ASP.NET Core 3 แอปเสิร์ฟ 200 พร้อมกันได้ = pool แยกจริง (ถ้ายังรวม pool ต้องเป็น 500.35)
- **AC4 ✓** smoke เองรอบใหม่: `/iLearn/` 307→200 (canonical host), API anonymous **401**, `/iLearn/admin-react/` **200**, `/iLearn/admin/` **401 ไม่ใช่ 500**
- **AC6 ✓** Implementer Notes ครบทั้ง audit-before/apply/audit-after/smoke/credential
- parser ✓ ทั้ง 2 สคริปต์, `git diff --check` ✓, ไม่มี password หลุดในไฟล์ใด
- ตรวจแล้วว่า deploy script อ่าน/เขียน `web.config` ด้วย `[xml]` ทุกจุด ⇒ การที่ `Save-XmlDocument` re-indent ไฟล์ใหม่ **ไม่กระทบ** `Get-AspNetCoreArguments`/`Sync-RequestLimits`

### Finding 1 (ต้องแก้) — guard กัน pool ชนใน `set-ilearn-prod-app-pools.ps1` เป็น dead code

[tools/set-ilearn-prod-app-pools.ps1:227-235](../../tools/set-ilearn-prod-app-pools.ps1) `Group-Object TargetPool` — แต่ `TargetPool` มาจาก `$mappings` ที่ **hardcode ให้ไม่ซ้ำกันอยู่แล้ว** (`iLearn.User`/`iLearn.Service`/`iLearn.Admin`) ⇒ `Count -gt 1` เป็นไปไม่ได้ เงื่อนไขนี้ throw ไม่ได้เลยไม่ว่าสถานะจริงจะเป็นอย่างไร

พิสูจน์แล้วด้วยการรัน logic ก้อนนี้จริงกับข้อมูล audit-before ที่แผนนี้บันทึกไว้เอง (ทั้ง 5 app อยู่ `iLearn.Dedicated`): **ไม่ fire** — ทั้งที่นั่นคือสภาพที่มันควรจับได้พอดี. เปลี่ยนไป group `PreviousPool` กับข้อมูลชุดเดียวกัน → fire ทันที (`iLearn.Dedicated: 3 apps`)

**แก้แล้ว:** เพิ่มฟิลด์ `ActualPool` ที่ **re-read `applicationPool` จาก IIS จริง** หลัง `Set-ItemProperty` (ไม่เดาว่าเขียนสำเร็จ) แล้วย้ายการเช็คมาไว้**ฝั่ง local หลังพิมพ์ตาราง** — เดิม throw อยู่ใน scriptblock ก่อน `return $results` ซึ่งจะทำให้ `-AuditOnly` ตายก่อน operator เห็นสถานะ ทั้งที่ audit มีไว้ดูสถานะ. พฤติกรรมใหม่: `-AuditOnly` เจอ pool ชน = **warning** (พร้อมชื่อ app ที่ชน) ไม่ล้ม; ตอน apply = **throw** ถ้าแยกไม่สำเร็จจริง (post-apply verification)

### Finding 2 (ต้องแก้) — preflight บล็อก emergency rollback

[tools/deploy-side-by-side.ps1:517](../../tools/deploy-side-by-side.ps1) เรียก `Test-IlearnProdAppPoolIsolation` **ก่อน** สาขา `if ($Rollback)` ที่บรรทัด 522 ⇒ ถ้า topology พังและ operator ส่ง `-IisCredential` มาด้วย preflight จะ throw และ **rollback ไม่ได้รัน** ทั้งที่ rollback แค่ flip `arguments` ใน `web.config` ไม่แตะ app pool เลย (แก้ไม่ได้และทำให้แย่ลงก็ไม่ได้) — คือปิดทางหนีในนาทีที่ต้องใช้พอดี

**แก้แล้ว:** ครอบด้วย `if (-not $Rollback) { ... }` + คอมเมนต์อธิบายเหตุผล (rollback แค่ repoint `arguments` ไป stamp folder เดิม ไม่แตะ pool) ⇒ rollback ใช้ได้เสมอแม้ topology พัง ส่วน deploy ปกติยังโดน preflight เหมือนเดิม

### Finding 3 (ต้องแก้) — `catch` แยกไม่ออกระหว่าง "ต่อ WinRM ไม่ได้" กับ "เจอ topology พังจริง"

[tools/deploy-side-by-side.ps1:405-414](../../tools/deploy-side-by-side.ps1) เมื่อไม่มี `-IisCredential` ทุก exception ถูกลดชั้นเป็น `Write-Warning "Could not audit remote..."` แล้ว deploy เดินต่อ — รวมถึง `throw "Invalid PROD IIS topology..."` (บรรทัด 381) ที่เป็นการ **ตรวจเจอของจริง** ไม่ใช่ปัญหาสิทธิ์. และ wrapper ทั้ง 3 ตัว (`deploy-user-prod.ps1`/`deploy-api-prod.ps1`/`deploy-admin-prod.ps1`) ประกาศ `-IisCredential` เป็น optional **ไม่มี default** ⇒ **เส้นทาง default คือเส้นที่ไม่ enforce** และข้อความที่ operator เห็นจะชวนให้เข้าใจผิดว่าเป็นแค่ปัญหา access

**แก้แล้ว:** scriptblock ฝั่ง remote throw ด้วย tag `ILEARN-TOPOLOGY:` ทั้ง 3 จุด (shared pool / wrong mapping / current app mapping) แล้ว `catch` re-throw ทุกกรณีที่ message มี tag นี้ **โดยไม่สนว่ามี `-IisCredential` หรือไม่** (พร้อมตัด tag ออกก่อนแสดงผล) — ส่วน connection/permission error (`Access is denied` ฯลฯ) ยัง downgrade เป็น warning เมื่อไม่มี credential ตามเดิม. `Required IIS application not found` จงใจ**ไม่**ติด tag เพื่อไม่ให้การ deploy แอปหนึ่งล้มเพราะอีกแอปถูกถอดออกจาก IIS (ดู Finding 5 ย่อหน้าท้าย)

### Finding 4 (ต้องแก้ — เอกสารขัดกันเอง จะกัด operator คนต่อไปตอน incident)

[DOC/DEPLOY-CHECKLIST.md](../DEPLOY-CHECKLIST.md) §8 code block หลักสั่งให้รัน `pwsh -NoLogo -NoProfile -File .\tools\set-ilearn-prod-app-pools.ps1 -AuditOnly -IisCredential $iisCredential` แต่ย่อหน้าถัดลงมาอีก 2 บรรทัดเขียนเองว่า **ห้าม** ส่ง `PSCredential` ผ่าน `pwsh -File` เพราะจะกลายเป็น string `System.Management.Automation.PSCredential` — ซึ่งคือ bug ที่ Implementer Notes ข้อแรกเจอมาแล้วจริง ๆ. หัวข้อ Verification ของแผนนี้ (บรรทัด 76-91) ก็เขียนแบบผิดเหมือนกัน

**แก้แล้ว:** code block ทั้งใน [DEPLOY-CHECKLIST §8](../DEPLOY-CHECKLIST.md) และใน Verification ของแผนนี้ (บรรทัด 76-93) เปลี่ยนเป็น `& .\tools\set-ilearn-prod-app-pools.ps1 ...` ในเซสชันเดียวกัน และเลื่อนคำเตือนเรื่อง `-File` ขึ้นไป**ก่อน** code block (เดิมอยู่หลัง = อ่านเจอตอนที่พังไปแล้ว). เพิ่มหัวข้อ **Deploy preflight** ใน checklist อธิบายพฤติกรรม guard ทั้ง 4 กรณี (pool ผิด / topology พัง / audit ไม่ได้ / rollback)

### Finding 5 (minor — ยังไม่แก้) — guard ผูกกับ short hostname ตัวเดียว

`Get-IlearnProdAppPoolExpectation` ([deploy-side-by-side.ps1:294](../../tools/deploy-side-by-side.ps1)) match เฉพาะ UNC ที่ขึ้นต้น `\\ap-ntc2137-prwb\` เป๊ะ ๆ. ทดสอบแล้ว: `\\ap-ntc2137-prwb.nikonoa.net\wwwroot\iLearn` และ `\\10.10.10.5\wwwroot\iLearn` → คืน `$null` = **ข้าม guard ทั้งหมดเงียบ ๆ** (ทั้ง local และ remote). ตอนนี้ยังไม่เจ็บเพราะ wrapper hardcode short name ไว้ แต่ PROD redirect ไป FQDN `ap-ntc2137-prwb.nikonoa.net` อยู่แล้ว โอกาสมีคนพิมพ์ FQDN มีจริง — พิจารณา match จาก leaf path (`...\wwwroot\iLearn[\Service|\admin]`) แทน full UNC

### Finding 6 (minor — ยังไม่แก้) — ไม่ส่ง `-AppPoolCredential` = pool ใหม่เป็น `ApplicationPoolIdentity` เงียบ ๆ

`Ensure-AppPool` ตั้ง `identityType`/`userName` เฉพาะเมื่อมี `$Credential`. ถ้า operator ลืมส่งตอน incident จะได้ pool ใหม่ 5 ตัวใต้ `ApplicationPoolIdentity` (เข้า DB/share ไม่ได้) โดยไม่มีคำเตือน — AC2 ข้อ "approved identity" ไม่มีอะไรบังคับ. แนะนำ `Write-Warning` เมื่อ **สร้าง pool ใหม่** โดยไม่มี `-AppPoolCredential`

### Housekeeping (ไม่บล็อก)

- บน PROD เหลือไฟล์สำรอง app root ละ 2-3 ไฟล์ (`web.config.bak-20260727*`, `web.config.bak-poolsplit-20260730*`) — เก็บไว้เป็น rollback material ได้ แต่ควรตัดสินใจลบให้ชัดตอนปิด incident
- `DOC/DEPLOY-CHECKLIST.md` ไม่มี newline ปิดท้ายไฟล์ → เติมแล้วตอนแก้ Finding 4

### Verification ของการแก้ Finding 1-4 (Claude Code, 2026-07-30)

- parser ✓ ทั้ง 2 สคริปต์, `git diff --check` ✓
- เขียน test ดึง logic ที่แก้แล้วมารันจริง **17 assertion ผ่านหมด** ครอบคลุม:
  - **F1** — audit ด้วยข้อมูล audit-before จริงของแผนนี้ (3 แอปบน `iLearn.Dedicated`) → **warn พร้อมชื่อ app ที่ชน** (เดิมเงียบ); audit สภาพ PROD ปัจจุบัน → clean; apply ที่ย้ายสำเร็จแค่บางตัว → **throw**; apply ที่สำเร็จ → ไม่ throw; static `admin-react` ไม่ถูกนับรวม
  - **F3** — violation จริง **บล็อก deploy แม้ไม่มี credential** (เดิมเป็นแค่ warning) + tag ถูกตัดออกจากข้อความ; `Access is denied` ยัง warn เมื่อไม่มี credential และ throw เมื่อมี
  - **F2** — การเรียก preflight ถูกครอบ `if (-not $Rollback)` และยังอยู่ก่อน deploy path
  - **F4** — ไม่เหลือ `pwsh -File ... -IisCredential` ที่เป็น invocation จริงในทั้ง 2 ไฟล์ (ข้อความที่ *อ้างถึง* รูปแบบผิดเพื่ออธิบายบั๊ก ไม่นับ)
- **ไม่ได้รันสคริปต์จริงกับ PROD IIS** (ไม่มีสิทธิ์ WinRM ในเซสชันนี้ และไม่ควรแตะ PROD ที่กำลังปกติเพื่อทดสอบ guard) — path ที่ยังไม่ผ่านการรันจริงคือ `Get-ItemProperty` re-read หลัง `Set-ItemProperty` ⇒ **ครั้งหน้าที่รัน apply จริง ให้ดูว่าคอลัมน์ `ActualPool` มีค่าตรงกับ `TargetPool` ทุกแถว**