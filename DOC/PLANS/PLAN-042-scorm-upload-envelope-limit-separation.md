# PLAN-042: แยก request-envelope limit ออกจาก content-size limit (ปิด AC#6 ของ PLAN-041)

- **Status:** VERIFIED (รีวิวโดย Claude Code 2026-06-30 — ดู Review Notes ท้ายไฟล์)
- **Assigned:** GPT (GPT-5.3-Codex)
- **Priority:** Medium — ไม่บล็อกผู้ใช้ (บั๊ก 413 ที่รายงานถูกแก้แล้วใน PLAN-041) แต่ปิด edge case ที่เหลือให้ behavior ตรงตามดีไซน์
- **Estimated scope:** เพิ่ม constant ใหม่ 1 ตัว + แก้ attribute 4 จุด + แก้ Program.cs ให้ใช้ constant เดียวกัน (ไม่มี prod deploy ใหม่จำเป็นจนกว่าจะ verify — แต่ endpoint behavior จะไม่เปลี่ยนจริงจนกว่าจะ deploy)

## Background

[PLAN-041](./PLAN-041-scorm-upload-413-hosting-limit.md) แก้บั๊กที่ผู้ใช้รายงาน (อัพไฟล์ 28.1 MB ได้ HTTP 413 เพราะเพดาน hosting ติด default ~28.6 MB) สำเร็จแล้วด้วยการเพิ่ม `iLearn.API/web.config` (`maxAllowedContentLength=115343360`) + ตั้ง global `MaxRequestBodySize` ใน `Program.cs` (Kestrel + `IISServerOptions`) เป็น `MaxCompressedPackageBytes + 10MB` (~110 MB) และ deploy ขึ้น production แล้ว

ระหว่างรีวิว implementation พบว่า **Acceptance Criteria ข้อ 6 ของ PLAN-041 จะไม่มีวันผ่าน** ด้วยโครงสร้างปัจจุบัน:

> "อัพโหลดไฟล์ >100 MB ได้ error เชิง business (`exceeds the maximum allowed size of 100 MB`) ไม่ใช่ 413 ดิบ"

**สาเหตุ:** endpoint attribute ทั้ง 4 จุดยังตั้งไว้ที่ `ScormPackageLimits.MaxCompressedPackageBytes` (100 MB เป๊ะ) — ตัวนี้คือ:
- [CoursesController.cs:229-230](file:///c:/Users/n4734/source/repos/iLearn2/iLearn.API/Controllers/CoursesController.cs#L229) (`create-scorm`)
- [CoursesController.cs:360-361](file:///c:/Users/n4734/source/repos/iLearn2/iLearn.API/Controllers/CoursesController.cs#L360) (`POST {courseId}/versions`)
- [CoursesController.cs:418-419](file:///c:/Users/n4734/source/repos/iLearn2/iLearn.API/Controllers/CoursesController.cs#L418) (`PUT versions/{versionId}`)
- [ContentItemsController.cs:215-216](file:///c:/Users/n4734/source/repos/iLearn2/iLearn.API/Controllers/ContentItemsController.cs#L215) (`upload`)

`[RequestSizeLimit]`/`[RequestFormLimits]` ทำงาน **ก่อน** model binding และ controller action — มัน abort request ที่ 100 MB เป๊ะด้วย **HTTP 413 ดิบ** เสมอ ไม่มีทางถึง `ScormUploadValidation`/`ScormService` ที่ให้ข้อความ friendly ได้ ผลคือ headroom 10 MB ที่ PLAN-041 เพิ่มใน global Kestrel/`IISServerOptions` **ถูกหักล้างพอดี** โดย attribute รายตัวที่ยังเป็น 100 MB — ไฟล์ที่ใกล้ 100 MB เป๊ะ (รวม multipart envelope) ยังเสี่ยงโดน 413 เหมือนเดิม

**ไม่กระทบบั๊กที่ผู้ใช้รายงาน** (28.1 MB ผ่านสบายเพราะเพดานจริงตอนนี้คือ ~110 MB ที่ hosting layer) — นี่เป็นแค่ edge case ของไฟล์ที่ใกล้/เกิน 100 MB ที่ควรได้ข้อความสวยแทน 413 ดิบ

## Scope (ทำแค่นี้)

### 1. เพิ่ม constant ใหม่ใน `ScormPackageLimits.cs`
ที่ [iLearn.Application/Common/ScormPackageLimits.cs](file:///c:/Users/n4734/source/repos/iLearn2/iLearn.Application/Common/ScormPackageLimits.cs):

```csharp
public const long MaxCompressedPackageBytes = 100L * 1024 * 1024;
public const long MaxRequestEnvelopeBytes = MaxCompressedPackageBytes + (10L * 1024 * 1024); // เผื่อ multipart overhead
```

- `MaxCompressedPackageBytes` ยังเป็น **single source of truth ของขนาดไฟล์เนื้อหา** (100 MB) — ห้ามแก้ค่า
- `MaxRequestEnvelopeBytes` คือเพดานของ "ทั้ง HTTP request body" (เผื่อ multipart boundary/header overhead) — ใช้ค่านี้แทนที่ magic number `+ 10MB` ที่กระจายอยู่หลายที่

### 2. แก้ attribute ทั้ง 4 endpoint ให้ใช้ `MaxRequestEnvelopeBytes`
เปลี่ยนทั้ง 4 จุดที่ระบุใน Background จาก:
```csharp
[RequestSizeLimit(ScormPackageLimits.MaxCompressedPackageBytes)]
[RequestFormLimits(MultipartBodyLengthLimit = ScormPackageLimits.MaxCompressedPackageBytes)]
```
เป็น:
```csharp
[RequestSizeLimit(ScormPackageLimits.MaxRequestEnvelopeBytes)]
[RequestFormLimits(MultipartBodyLengthLimit = ScormPackageLimits.MaxRequestEnvelopeBytes)]
```

ตอนนี้ request จะผ่าน gate ของ attribute ได้ถึง ~110 MB → ไฟล์เนื้อหาที่เกิน 100 MB จะเข้าไปถึง `ScormUploadValidation.EnsureValidScormPackageUpload` / `ScormService.ExtractAndParseScormAsync` ที่เช็ค `file.Length > MaxCompressedPackageBytes` แล้วโยน exception ที่มีข้อความ friendly จริงตาม AC#6

### 3. แก้ `iLearn.API/Program.cs` ให้ใช้ constant เดียวกัน (ลด duplication)
จาก:
```csharp
const long maxRequestBodyBytes = ScormPackageLimits.MaxCompressedPackageBytes + (10L * 1024 * 1024);
```
เป็น:
```csharp
const long maxRequestBodyBytes = ScormPackageLimits.MaxRequestEnvelopeBytes;
```
(ค่าไม่เปลี่ยน — แค่รวม single source of truth ไม่ให้เลข `10MB` กระจายซ้ำ 2 ที่)

### ขอบเขตที่ห้ามทำ
- ห้ามแก้ `MaxCompressedPackageBytes` (100 MB) — เป็น design limit ที่ทดสอบ/อ้างอิงไว้ในหลายที่ (`ScormServiceTests`, UI label "Max 100MB")
- ห้ามแก้ `web.config` (`maxAllowedContentLength=115343360` ของ PLAN-041 ใช้ค่าตรงกับ `MaxRequestEnvelopeBytes` อยู่แล้ว — ถ้าต้องการ sync ให้คอมเมนต์อ้างอิงค่านี้ แต่ไม่ต้องแก้ตัวเลข)
- ห้ามแตะ React / validation logic อื่นนอกจากที่ระบุ
- **ไม่ต้อง deploy ขึ้น production ในแผนนี้** — แค่ commit + verify ใน local/CI ก่อน ให้ผู้ใช้/ทีมตัดสินใจ deploy รอบถัดไปเอง (เพราะ PLAN-041 เพิ่ง deploy ไปสด ๆ ควรแยก deploy window)

## Verification

```powershell
# จาก repo root
dotnet build iLearn.API -o artifacts\verify-plan042
dotnet build iLearn.Tests -o artifacts\verify-test
dotnet test artifacts\verify-test\iLearn.Tests.dll
Remove-Item -Recurse -Force artifacts\verify-plan042, artifacts\verify-test

# ยืนยันไม่มี endpoint ไหนเหลือ MaxCompressedPackageBytes ใน RequestSizeLimit/RequestFormLimits
rg "RequestSizeLimit\(ScormPackageLimits.MaxCompressedPackageBytes\)|RequestFormLimits\(MultipartBodyLengthLimit = ScormPackageLimits.MaxCompressedPackageBytes\)" iLearn.API
# ควรไม่พบผลลัพธ์ (ทุกจุดต้องเปลี่ยนเป็น MaxRequestEnvelopeBytes แล้ว)
```

- Manual/integration (เมื่อมีโอกาส deploy): อัพไฟล์ทดสอบที่ขนาด ~105 MB → ต้องได้ JSON error message "SCORM package exceeds the maximum allowed size of 100 MB" (HTTP 400) ไม่ใช่ 413

## Implementer Notes

- แก้โค้ดตาม scope ครบ:
	- เพิ่ม `MaxRequestEnvelopeBytes` ใน `iLearn.Application/Common/ScormPackageLimits.cs`
	- เปลี่ยน request-limit attributes ทั้ง 4 endpoint เป็น `ScormPackageLimits.MaxRequestEnvelopeBytes`:
		- `iLearn.API/Controllers/CoursesController.cs` (`create-scorm`, `POST {courseId}/versions`, `PUT versions/{versionId}`)
		- `iLearn.API/Controllers/ContentItemsController.cs` (`POST upload`)
	- ปรับ `iLearn.API/Program.cs` ให้ใช้ `ScormPackageLimits.MaxRequestEnvelopeBytes` แทน magic number

- Verification ที่รันจริง (ผ่านทั้งหมด):
	- `dotnet build iLearn.API/iLearn.API.csproj -o artifacts/verify-plan042`
	- `dotnet build iLearn.Tests -o artifacts/verify-test`
	- `dotnet test artifacts/verify-test/iLearn.Tests.dll` -> Passed 118, Failed 0
	- `rg "RequestSizeLimit\(ScormPackageLimits.MaxCompressedPackageBytes\)|RequestFormLimits\(MultipartBodyLengthLimit = ScormPackageLimits.MaxCompressedPackageBytes\)" iLearn.API`
		- ผลลัพธ์: ไม่พบ pattern เดิม (no legacy request-limit attributes)
	- cleanup สำเร็จ: `artifacts/verify-plan042`, `artifacts/verify-test`

- หมายเหตุ: แผนนี้ไม่ deploy production ตามขอบเขตที่กำหนด

## Review Notes (Claude Code, 2026-06-30)

ตรวจอิสระทุกจุด ไม่ได้พึ่งแค่ Implementer Notes:
- `grep` ยืนยันทั้ง 4 endpoint (`CoursesController.cs` ×3, `ContentItemsController.cs` ×1) เปลี่ยนเป็น `MaxRequestEnvelopeBytes` ครบ และไม่เหลือ pattern เก่า (`MaxCompressedPackageBytes` ใน `RequestSizeLimit`/`RequestFormLimits`) หลงเหลือที่ไหนเลย
- `Program.cs` ใช้ constant เดียวกัน ลบ magic number ซ้ำสำเร็จ
- ยืนยัน `ScormUploadValidation.cs`/`ScormService.cs` ยังเช็คขนาดไฟล์เนื้อหากับ `MaxCompressedPackageBytes` (100MB) เหมือนเดิม — ไม่ถูกแตะ ตรงตามขอบเขต
- รัน `dotnet build iLearn.API` เอง → 0 errors; รัน `dotnet test` เอง → Passed 118, Failed 0 (ตรงกับที่ implementer รายงาน)
- ตรวจเลขสอดคล้องข้าม 3 ชั้น: `web.config` (`maxAllowedContentLength=115343360`) = global `IISServerOptions`/Kestrel = per-endpoint attribute ทั้งหมด `MaxRequestEnvelopeBytes` (110 MB) **sync กันสมบูรณ์**
- Logic หลังแก้ถูกต้อง: ไฟล์ >100MB (เช่น 105MB) จะผ่าน attribute gate (envelope < 110MB) แล้วไปติดที่ `file.Length` check ใน `ScormUploadValidation` แทน → ได้ friendly message ตาม AC#6 ของ PLAN-041 จริง — gap ปิดสมบูรณ์
- ไม่พบ regression หรือปัญหาเชิงตรรกะใด ๆ — **APPROVE**, ปรับสถานะเป็น VERIFIED
