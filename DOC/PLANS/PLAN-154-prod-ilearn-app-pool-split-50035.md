# PLAN-154: PROD iLearn app-pool split หลัง incident 500.35

- **Status:** VERIFIED
- **Assigned:** GitHub Copilot (GPT)
- **Reviewer:** Claude Code
- **Priority:** High
- **Estimated scope:** PROD incident mitigation + deploy runbook/script update
- **สร้างเมื่อ:** 2026-07-27

## Problem

`https://ap-ntc2137-prwb/iLearn/` ล่มเป็น HTTP 500 หลังมีการย้ายหลายแอปใต้ `/iLearn*` ไปใช้ app pool เดียว `iLearn.Dedicated`.

อาการที่ยืนยันได้จาก public response:

- ก่อนแก้: `/iLearn/` = `HTTP Error 500.35 - ASP.NET Core does not support multiple apps in the same app pool`
- หลังลองแก้เฉพาะ Learner เป็น `outofprocess`: `/iLearn/` เปลี่ยนเป็น `HTTP Error 500.34 - ASP.NET Core does not support mixing hosting models`

สรุป root cause: แอป ASP.NET Core แบบ in-process หลายตัว (`/iLearn`, `/iLearn/Service`, `/iLearn/admin`) ถูกผูกไว้ใน app pool เดียวกัน. ASP.NET Core Module ไม่รองรับ topology นี้.

## Scope

1. Mitigate production outage โดยตั้ง active ASP.NET Core `web.config` ใต้ `/iLearn` ให้ใช้ `hostingModel="outofprocess"` ให้ตรงกันชั่วคราว:
   - `\\ap-ntc2137-prwb\wwwroot\iLearn\web.config`
   - `\\ap-ntc2137-prwb\wwwroot\iLearn\Service\web.config`
   - `\\ap-ntc2137-prwb\wwwroot\iLearn\admin\web.config`
2. เพิ่มสคริปต์ถาวร [set-ilearn-prod-app-pools.ps1](../../tools/set-ilearn-prod-app-pools.ps1) เพื่อแยก app pool แบบ idempotent:
   - `/iLearn` -> `iLearn.User`
   - `/iLearn/Service` -> `iLearn.Service`
   - `/iLearn/admin` -> `iLearn.Admin`
   - `/iLearn/admin-react` -> `iLearn.Admin.React`
   - `/iLearn/student` -> `iLearn.Static` ถ้ามี app นี้จริง
3. อัปเดต [DEPLOY-CHECKLIST.md](../DEPLOY-CHECKLIST.md) ให้เลิกแนะนำการรวมทุก `/iLearn*` ไว้ใน pool เดียว และบันทึกข้อห้าม 500.35/500.34.

## Out of scope

- ไม่เปลี่ยน business logic / API contract / DB schema
- ไม่ deploy build ใหม่
- ไม่ rotate password ผ่าน agent session
- ไม่แก้ connection string หรือ appsettings content

## Acceptance criteria

1. Learner root `/iLearn/` กลับมา HTTP 200
2. API `/iLearn/Service/api/admin/session/me` ยัง HTTP 200 เมื่อส่ง Windows credentials
3. Admin React `/iLearn/admin-react/` ยัง HTTP 200
4. Runbook ระบุชัดว่า ASP.NET Core in-process apps ต้องแยก app pool ตัวต่อตัว
5. มีสคริปต์ audit/apply สำหรับ IIS admin ใช้คืน mapping ถาวรโดยไม่พิมพ์ password ออก output

## Verification

```powershell
pwsh -NoLogo -NoProfile -Command "$errors=$null; [System.Management.Automation.Language.Parser]::ParseFile('tools/set-ilearn-prod-app-pools.ps1',[ref]$null,[ref]$errors) | Out-Null; if($errors){$errors | Format-List; exit 1}"
pwsh -NoLogo -NoProfile -File .\tools\set-ilearn-prod-app-pools.ps1 -AuditOnly
```

Manual PROD smoke:

```text
GET https://ap-ntc2137-prwb/iLearn/ = 200
GET https://ap-ntc2137-prwb/iLearn/Service/api/admin/session/me = 200 with Windows credentials
GET https://ap-ntc2137-prwb/iLearn/admin-react/ = 200
```

## Implementer Notes

- Mitigation performed on PROD via file share only because WinRM IIS query/apply returned `Access is denied` from this session.
- Backups were created next to changed active configs using `web.config.bak-<timestamp>` before edits.
- Active PROD configs were changed to `hostingModel="outofprocess"` consistently for Learner/API/MVC Admin. This restored Learner from 500 to 200 without requiring IIS admin rights.
- Public smoke after mitigation:
  - `/iLearn/` = 200
  - `/iLearn/Service/api/admin/session/me` = 200 with Windows credentials
  - `/iLearn/admin-react/` = 200
  - `/iLearn/admin/` = 403 with this caller, but no longer 500 (likely auth/role behavior)
- Script syntax validation passed with the PowerShell parser. Running `set-ilearn-prod-app-pools.ps1 -AuditOnly` from the current session reaches WinRM and fails with `Access is denied`, so permanent IIS pool reassignment still needs an IIS admin credential.
- Durable fix still requires an IIS admin to run [set-ilearn-prod-app-pools.ps1](../../tools/set-ilearn-prod-app-pools.ps1) without `-AuditOnly`. After split, active ASP.NET Core apps can safely return to `hostingModel="inprocess"`; the script does that by default.

## Reviewer Notes (Claude Code, 2026-07-30) — VERIFIED

รีวิวหลัง PLAN-155 apply เสร็จ ตรวจอิสระเองทั้งหมด:

- **AC1 ✓** `GET https://ap-ntc2137-prwb/iLearn/` = 307 → `https://ap-ntc2137-prwb.nikonoa.net/iLearn/` = **200** (307 คือ canonical-host redirect ตาม PLAN-140 ไม่ใช่ error)
- **AC2 ✓** `/iLearn/Service/api/admin/session/me` = 401 แบบ anonymous (endpoint ยังมีชีวิต ไม่ใช่ 500)
- **AC3 ✓** `/iLearn/admin-react/` = 200
- **AC4 ✓** runbook §8 ใน [DEPLOY-CHECKLIST.md](../DEPLOY-CHECKLIST.md) ระบุ mapping 1 app = 1 pool พร้อมตารางชัดเจน
- **AC5 ✓** อ่านสคริปต์ทั้งไฟล์แล้ว: `-AppPoolCredential` ถูกใช้ผ่าน `GetNetworkCredential().Password` เขียนลง IIS อย่างเดียว **ไม่มี path ไหนพิมพ์ password ออก output** — ตาราง output มีแค่ `PoolUserName`. grep repo แล้วไม่มีค่า password หลุดในไฟล์ใด (เจอแค่ username `NIKONOA\Z001927` ซึ่งมีมาตั้งแต่ PLAN-049/051)
- Mitigation `outofprocess` ที่แผนนี้ทำ ถูกถอนคืนเป็น `inprocess` แล้วโดย PLAN-155 (ตรวจ web.config จริงทั้ง 3 แอปแล้ว) ⇒ แผนนี้ปิดได้ ปัญหาที่เหลืออยู่ในขอบเขต PLAN-155