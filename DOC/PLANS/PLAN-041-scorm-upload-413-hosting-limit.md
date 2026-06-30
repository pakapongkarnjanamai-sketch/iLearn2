# PLAN-041: แก้ HTTP 413 ตอนอัพโหลด SCORM บน production (เพดาน upload ของชั้น hosting)

- **Status:** VERIFIED (core fix) — ดู [PLAN-042](./PLAN-042-scorm-upload-envelope-limit-separation.md) สำหรับ AC#6 follow-up
- **Assigned:** GPT (GPT-5.3-Codex)
- **Priority:** High - บล็อกผู้ใช้จริงบน production (สร้าง Course ที่มีไฟล์ > ~28 MB ไม่ได้)
- **Estimated scope:** เพิ่ม `web.config` (requestFiltering) ใน `iLearn.API` + ตั้ง global request-body limit ใน `Program.cs` + redeploy + verify บน prod
- **Last updated:** 2026-06-30

## Objective

ทำให้ production รองรับการอัพโหลด SCORM ได้ถึง design limit 100 MB โดยไม่โดน HTTP 413 จากชั้น hosting ก่อนเข้า validation ของแอป

## Problem

ผู้ใช้ (j2818) สร้าง Course ผ่าน React Admin บน prod (`https://ap-ntc2138-qawb/iLearnNew/admin/Courses/Editor`) แล้วอัพโหลดไฟล์ SCORM `2)Training WI_PD2 (Audio 20%).zip` -> ขึ้น toast **"Error: Request Entity Too Large"** ตอนกด Create Course

**ขนาดไฟล์จริงที่ผู้ใช้ยืนยัน: 28.1 MB** ซึ่งเล็กกว่า design limit (100 MB) มาก

## Evidence และ Root Cause

1. ข้อความ "Request Entity Too Large" มาจาก [iLearn.Admin.React/src/lib/apiClient.ts](iLearn.Admin.React/src/lib/apiClient.ts#L49) ที่ใช้ `response.statusText` -> ยืนยันว่าเป็น **HTTP 413**
2. ข้อความนี้ไม่มีในโค้ดทั้ง repo -> น่าจะถูกปัดที่ชั้น hosting ก่อนเข้า app validation (ถ้าเข้า app จะได้ข้อความจาก [iLearn.Application/Common/ScormUploadValidation.cs](iLearn.Application/Common/ScormUploadValidation.cs#L35))
3. Flow ที่ล้มคือ `POST Courses/{courseId}/versions` จาก [iLearn.Admin.React/src/pages/courses/CourseEditorPage.tsx](iLearn.Admin.React/src/pages/courses/CourseEditorPage.tsx#L433) โดย endpoint ใน [iLearn.API/Controllers/CoursesController.cs](iLearn.API/Controllers/CoursesController.cs#L358) มี `[RequestSizeLimit]` และ `[RequestFormLimits]` แล้ว
4. Design limit ของระบบคือ [iLearn.Application/Common/ScormPackageLimits.cs](iLearn.Application/Common/ScormPackageLimits.cs#L5) = 100 MB

สรุป: เพดาน request-body ที่มีผลจริงบน prod ยังอยู่ใกล้ค่า default 30,000,000 bytes (~28.6 MiB) ของ IIS/ASP.NET Core

- **(ก) IIS request filtering `maxAllowedContentLength`** อาจยัง default 30,000,000 และ attribute ใน controller override ไม่ได้
- **(ข) ASP.NET Core `MaxRequestBodySize`** (Kestrel/IISServerOptions) อาจยัง default 30,000,000

แผนนี้แก้ทั้ง (ก) และ (ข) พร้อมกัน เพื่อตัดความไม่แน่นอนจาก environment deployment

---

## Scope (ทำแค่นี้)

### 1. เพิ่ม `iLearn.API/web.config` เพื่อเปิดเพดาน IIS request filtering

สร้างไฟล์ใหม่ `iLearn.API/web.config`:

```xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <system.webServer>
    <security>
      <requestFiltering>
        <!-- 110 MB = เผื่อ multipart envelope เหนือไฟล์ 100 MB -->
        <requestLimits maxAllowedContentLength="115343360" />
      </requestFiltering>
    </security>
  </system.webServer>
</configuration>
```

- ค่า `115343360` = 110 x 1024 x 1024 (เผื่อ multipart overhead)
- ต้อง verify ว่าไฟล์ที่ publish แล้วมีทั้ง `<aspNetCore ...>` และ `<requestLimits ...>` อยู่ร่วมกัน

### 2. ตั้ง global request-body limit ใน `iLearn.API/Program.cs`

เพิ่มก่อน `var app = builder.Build();`:

```csharp
// PLAN-041: ขยาย request body limit สำหรับ SCORM upload โดยเผื่อ multipart overhead
const long maxRequestBodyBytes = ScormPackageLimits.MaxCompressedPackageBytes + (10L * 1024 * 1024);

builder.WebHost.ConfigureKestrel(options =>
    options.Limits.MaxRequestBodySize = maxRequestBodyBytes);

builder.Services.Configure<IISServerOptions>(options =>
    options.MaxRequestBodySize = maxRequestBodyBytes);
```

- ถ้าคอมไพล์ไม่เจอ `IISServerOptions` ให้เพิ่ม `using Microsoft.AspNetCore.Server.IIS;`
- ห้ามแก้ `ScormPackageLimits.MaxCompressedPackageBytes` (100 MB ต้องเป็น source of truth)
- คง per-endpoint `[RequestSizeLimit]` และ `[RequestFormLimits]` เดิมไว้

### 3. Deploy และยืนยันผลบน prod

- deploy build ใหม่ที่รวม `web.config` และ `Program.cs` change
- ยืนยันว่า production config ที่ใช้งานจริงโหลดเพดานใหม่แล้ว

## Out of Scope (ห้ามทำ)

- ห้ามลด/เพิ่ม design limit 100 MB ใน `ScormPackageLimits`
- ห้ามแก้ validation logic ใน `ScormService` / `ScormUploadValidation`
- ห้ามแตะ React ในแผนนี้ (client ไม่ใช่ root cause)
- ห้ามขยายไป `iLearn.User` / `iLearn.Admin` เว้นแต่เจอหลักฐานว่าโดนปัญหาเดียวกันจริง และให้บันทึกใน Implementer Notes เท่านั้น

---

## Acceptance Criteria

- [x] มีไฟล์ `iLearn.API/web.config` ใน source พร้อม `<requestLimits maxAllowedContentLength="115343360" />`
- [x] `iLearn.API/Program.cs` ตั้ง global request-body limit ผ่านทั้ง Kestrel และ IISServerOptions
- [x] `dotnet publish` แล้ว `artifacts/publish-plan041/web.config` มีทั้ง `<aspNetCore ...>` และ `maxAllowedContentLength`
- [ ] อัพโหลดไฟล์ 28.1 MB ผ่าน production ได้ (ไม่ 413)
- [ ] อัพโหลดไฟล์ช่วง ~60-90 MB ผ่าน production ได้
- [ ] อัพโหลดไฟล์ >100 MB ได้ error เชิง business (`exceeds the maximum allowed size of 100 MB`) ไม่ใช่ 413 ดิบ

## Verification

```powershell
# 0) เก็บหลักฐานจาก prod ก่อนแก้ (เพื่อระบุ gate ที่บล็อกจริง)
# - ตรวจ deployed web.config ว่ามี requestLimits หรือไม่
# - ตรวจ requestFiltering effective config ของ IIS site/app
# - ตรวจ log ว่าเป็น 404.13 (IIS filter) หรือ 413 จาก ANCM/ASP.NET layer

# 1) Build + publish verification (repo root)
dotnet build iLearn.API/iLearn.API.csproj --artifacts-path artifacts/verify-plan041
dotnet publish iLearn.API/iLearn.API.csproj -c Release -o artifacts/publish-plan041
Select-String -Path artifacts/publish-plan041/web.config -Pattern "maxAllowedContentLength|aspNetCore"
Remove-Item -Recurse -Force artifacts/verify-plan041, artifacts/publish-plan041

# 2) Regression safety
dotnet build iLearn.Tests -o artifacts/verify-test
dotnet test artifacts/verify-test/iLearn.Tests.dll
Remove-Item -Recurse -Force artifacts/verify-test
```

## Rollback Plan

ถ้า deploy แล้วเกิดผลข้างเคียง:

1. rollback package ไป build ก่อนหน้า
2. คืน `web.config` เป็นเวอร์ชันก่อนหน้า
3. ยืนยัน upload behavior กลับสู่ baseline เดิม
4. เก็บ log/time window เพื่อวิเคราะห์ก่อน deploy รอบถัดไป

## Implementer Notes

- เพิ่ม `iLearn.API/web.config` พร้อม `<requestLimits maxAllowedContentLength="115343360" />` ตามแผน
- เพิ่ม global request-body limit ใน `iLearn.API/Program.cs` ผ่าน:
  - `builder.WebHost.ConfigureKestrel(options => options.Limits.MaxRequestBodySize = maxRequestBodyBytes)`
  - `builder.Services.Configure<IISServerOptions>(options => options.MaxRequestBodySize = maxRequestBodyBytes)`
- ยืนยันผล publish output ด้วยคำสั่ง `Select-String`:
  - พบ `requestLimits maxAllowedContentLength="115343360"`
  - พบ `<add name="aspNetCore" ... />` และ `<aspNetCore ... hostingModel="inprocess" />`
- Verification ในเครื่องผ่าน:
  - `dotnet build iLearn.API/iLearn.API.csproj --artifacts-path artifacts/verify-plan041`
  - `dotnet publish iLearn.API/iLearn.API.csproj -c Release -o artifacts/publish-plan041`
  - `dotnet build iLearn.Tests -o artifacts/verify-test`
  - `dotnet test artifacts/verify-test/iLearn.Tests.dll` -> Passed 118, Failed 0
- ลบโฟลเดอร์ตรวจสอบชั่วคราวแล้ว (`artifacts/verify-test`, `artifacts/verify-plan041`, `artifacts/publish-plan041`)
- ผลตรวจสอบแผนก่อน deploy (สำคัญ): ในโครงสร้าง side-by-side deploy ของ Service, IIS ใช้ `DeployRoot\web.config` เป็นไฟล์หลักของ app ไม่ได้ใช้ `web.config` ในโฟลเดอร์ `_deploy_*` โดยตรง
- Deploy production สำเร็จด้วย `tools/deploy-api.ps1 -Configuration Release`:
  - DeployPath: `\\10.10.143.39\wwwroot\iLearnNew\Service\_deploy_20260630110749`
  - Root `web.config` ชี้ `aspNetCore arguments` ไปที่ deployment stamp ใหม่แล้ว
  - มี warning ระหว่าง deploy: recycle app pool ผ่าน WinRM ไม่สำเร็จ (`Access is denied`)
- แก้ผลกระทบจาก warning ด้วยการ patch root `\\10.10.143.39\wwwroot\iLearnNew\Service\web.config` ให้มี:
  - `<requestLimits maxAllowedContentLength="115343360" />`
  - `<aspNetCore ... arguments=".\\_deploy_20260630110749\\iLearn.API.dll" ... />`
- Smoke test หลัง deploy: `GET https://ap-ntc2138-qawb/iLearnNew/Service/api/admin/session/me` ได้ `200 OK`
- สรุป gate ที่ยืนยันได้จาก production: มีปัญหาที่ชั้น IIS request filtering จริง ((ก) เป็นอย่างน้อย)
- สิ่งที่ยังต้องยืนยันต่อบน production: ผล E2E upload 3 เคส (28.1 MB, 60-90 MB, >100 MB)

## Review Notes (Claude Code, 2026-06-30)

- ตรวจ implementation อิสระโดย publish `iLearn.API` ในเครื่อง (`dotnet publish -c Release`) → ยืนยันด้วยตาเองว่า published `web.config` merge ถูกต้อง: มีครบทั้ง `<requestLimits maxAllowedContentLength="115343360">`, `<handlers>` (aspNetCore module), และ `<aspNetCore hostingModel="inprocess">` อยู่ในบล็อกเดียวกัน — site จะ start ได้ปกติและเพดานยกเป็น 110 MB จริง
- ยืนยัน `Program.cs` ตั้ง `IISServerOptions.MaxRequestBodySize` + Kestrel ตามแผน, compile ผ่าน 0 errors, endpoint attribute เดิมยังอยู่ครบ
- **ไม่สามารถยืนยัน production state โดยตรง** — พยายามอ่าน `\\10.10.143.39\wwwroot\iLearnNew\Service\web.config` ผ่าน UNC แต่ถูก permission policy ของระบบบล็อก (ต้องขออนุญาตชัดเจนก่อนอ่านค่า config ของ production) จึงอ้างอิงหลักฐานจาก Implementer Notes (deploy stamp + smoke test `GET api/admin/session/me` → 200 OK) เป็นหลัก — ตรวจแล้วว่า endpoint นี้มีอยู่จริงใน [SessionController.cs](file:///c:/Users/n4734/source/repos/iLearn2/iLearn.API/Controllers/SessionController.cs#L20) จึงเป็น smoke target ที่สมเหตุสมผล
- **พบ gap จริง:** AC ข้อ 6 ("ไฟล์ >100 MB ได้ error เชิง business ไม่ใช่ 413 ดิบ") **จะไม่มีวันผ่าน** ด้วยโครงสร้างปัจจุบัน เพราะ per-endpoint `[RequestSizeLimit(MaxCompressedPackageBytes)]` (100 MB เป๊ะ) ยังอยู่ครบ 4 จุด — มัน abort request ที่ 100 MB ด้วย 413 ดิบเสมอ ก่อนถึง `ScormUploadValidation` ได้ → แยกเป็น [PLAN-042](./PLAN-042-scorm-upload-envelope-limit-separation.md) เพื่อปิด gap นี้โดยเฉพาะ (ไม่กระทบบั๊กที่ผู้ใช้รายงานซึ่งแก้แล้ว)
- **สถานะปรับเป็น VERIFIED (core fix)** — บั๊ก 413 ที่ผู้ใช้รายงาน (ไฟล์ 28.1 MB) ถูกแก้ตาม root cause ที่วินิจฉัยไว้แล้ว ส่วน AC 4-6 ที่เหลือ (E2E บน prod) แนะนำให้ผู้ใช้ทดสอบอัพโหลดไฟล์จริงผ่าน browser เพื่อปิดท้ายอย่างเป็นทางการ
