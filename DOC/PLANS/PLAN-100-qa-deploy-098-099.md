# PLAN-100: QA deploy runbook — PLAN-098 (learner header) + PLAN-099 (SCORM reset hotfix)

- **Status:** DONE (QA deploy complete; manual learner/iPad smoke pending)
- **Assigned:** GitHub Copilot
- **Reviewer:** Claude Code
- **สร้างเมื่อ:** 2026-07-21

> Deploy 2 commit ที่รีวิวผ่านแล้ว: `51ff047` (098 learner) + `f718938` (099 API+Infra+App). **ไม่มี migration.** PROD รอผู้ใช้ยืนยันผล QA ในแชท (gate เดิม)

## Gate 0 (ก่อนเริ่ม)

- `git log --oneline -2` เห็น `f718938` (099) บนสุด, `51ff047` (098) ถัดมา — ถ้าไม่เห็นแปลว่ายังไม่ commit อย่า deploy
- pwsh 7 จาก repo root; UNC `\\AP-NTC2138-QAWB\...` เข้าถึงได้
- **099 = การแก้จริง** (API); 098 = header cosmetic (learner) — **deploy API ก่อน learner**

## Local validation

```powershell
dotnet build .\iLearn.Tests\iLearn.Tests.csproj -c Debug --artifacts-path .\artifacts\validate\iLearn.Tests
dotnet test .\artifacts\validate\iLearn.Tests\bin\Debug\net9.0\iLearn.Tests.dll   # คาด 207/207
dotnet build .\iLearn.API\iLearn.API.csproj -c Debug --artifacts-path .\artifacts\validate\iLearn.API
dotnet build .\iLearn.User\iLearn.User.csproj -c Debug --artifacts-path .\artifacts\validate\iLearn.User
```

## Deploy (API → User)

```powershell
# 1) API ก่อน (099 — hotfix)
pwsh -NoLogo -NoProfile -File .\tools\deploy-api.ps1 -SkipPublish -WhatIf   # dry-run: ดู DeployPath/web.config switch
pwsh -NoLogo -NoProfile -File .\tools\deploy-api.ps1 -HealthCheckUrl 'https://ap-ntc2138-qawb/iLearn/Service/api/admin/session/me'

# 2) User ถัดมา (098 — header)
pwsh -NoLogo -NoProfile -File .\tools\deploy-user.ps1 -SkipPublish -WhatIf
pwsh -NoLogo -NoProfile -File .\tools\deploy-user.ps1 -HealthCheckUrl 'https://ap-ntc2138-qawb/iLearn/'
```

จดทุกครั้ง: `DeployPath`, `WebConfigArguments`, `Stamp` (API + User) — ไว้ verify/rollback
- API health คาด 401 (Windows auth) = ปกติ; `AutoRolledBack=False`
- User health คาด 200; `AutoRolledBack=False`

## Verify (ตามลำดับ — ข้อ 3 คือหัวใจของ 099)

1. **098 header:** เปิด player คอร์สใด ๆ → header 4 แถว label ครบ, **รหัสผู้เรียนไม่โดนตัด**, pill สถานะแยกแถว, TOC ชื่อบทเรียนเล็กกว่าชื่อคอร์ส; console 0 error
2. **099 remediation:** enrollment 18201 / courseId 968 → กด **Reset Progress 1 ครั้ง** → เปิด player: **ทุก item สถานะว่าง/ไม่ผ่าน, ไม่มีติ๊กเขียวค้าง, บาร์ Learn ไม่เต็ม** (ยืนยันว่า hotfix ล้าง state เก่าจริง)
3. **099 correctness:** ทำ Exam จริงให้ผ่าน → ผ่านเฉพาะที่ทำ; Learn ดูจบ → progress ขยับตามจริง; เล่น ~1 นาทีสลับแท็บ 2-3 ครั้ง → เวลาเรียนไม่โป่ง
4. (ถ้าสะดวก) ยืนยัน DB: `ScormRuntimeStates` ของ 18201 rows เก่า `IsDeleted=1`, rows ใหม่สะอาด

## Rollback (ถ้าพัง)

web.config switch กลับ stamp เดิม (ตาม DEPLOY-CHECKLIST §6) — API: `_deploy_<old>`, User: `_user_deploy_<old>`. ไม่มี migration จึงไม่ต้อง revert DB

## หยุดรอผู้ใช้

หลัง verify QA ผ่าน → **หยุด รอผู้ใช้ยืนยันในแชท + iPad smoke** ก่อนพิจารณา PROD (ห้าม deploy PROD เอง)

## Implementer Notes

- Gate 0 passed: `ee6102e` (PLAN-100 docs) at HEAD, then `f718938` (099 API) and `51ff047` (098 learner); working tree was clean.
- Local validation passed: Debug builds for Tests/API/User succeeded; test assembly from `artifacts\validate\iLearn.Tests\bin\iLearn.Tests\debug\iLearn.Tests.dll` passed **207/207**. (The SDK emitted this layout rather than the example `bin\Debug\net9.0` path in the runbook.)
- API deployed first: `DeployPath=\\AP-NTC2138-QAWB\wwwroot\iLearn\Service\_deploy_20260721092410`; `WebConfigArguments=.\_deploy_20260721092410\iLearn.API.dll`; previous API stamp `20260717131321`; health check returned expected HTTP 401 on attempt 1/5; `AutoRolledBack=False`.
- User deployed second: `DeployPath=\\AP-NTC2138-QAWB\wwwroot\iLearn\_user_deploy_20260721092529`; `WebConfigArguments=.\_user_deploy_20260721092529\iLearn.User.dll`; previous User stamp `20260717164531`; health check returned HTTP 200 on attempt 1/5; `AutoRolledBack=False`.
- Read-back confirmed both active `web.config` files reference the new stamps with `ASPNETCORE_ENVIRONMENT=Staging`; both active DLLs exist. Independent checks: API session endpoint = 401, learner root = 200.
- Remaining QA gate: reset enrollment 18201/course 968 and perform player/iPad smoke for header, clean runtime state, completion behavior, and non-inflated time. No PROD deploy was performed.
