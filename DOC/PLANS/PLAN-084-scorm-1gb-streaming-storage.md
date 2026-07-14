# PLAN-084: รองรับ SCORM 1GB — streaming upload + เลิกเก็บ byte[] ใน DB (Option B ของ PLAN-076)

- **Status:** DONE → VERIFIED → Finding 1 FIXED (Claude Code 2026-07-14: rollback archive+rows ใน catch)
- **Assigned:** GitHub Copilot
- **Reviewer:** Claude Code
- **สร้างเมื่อ:** 2026-07-14
- **อ้างอิง:** [PLAN-076](PLAN-076-large-scorm-file-support-assessment.md) (assessment — Option B), [PLAN-080](PLAN-080-scorm-content-size-200mb.md) (แบบอย่างการยกลิมิต 5 ชั้น), [PLAN-083](PLAN-083-qa-413-requestlimits-sync.md) (**ต้องเสร็จก่อน** — pipeline sync requestLimits)
- **Depends on:** PLAN-083 (ไม่งั้นค่า web.config ใหม่ไม่ถึงเซิร์ฟเวอร์)

> ผู้ใช้ตัดสินใจ (2026-07-14): ขยายลิมิต SCORM package เป็น **1GB** — ตาม PLAN-076 §5 ขนาดนี้ **ห้ามใช้ Option A (ยกตัวเลขเฉย ๆ)** เพราะ pipeline ปัจจุบัน `MemoryStream → ToArray()` + เก็บ `byte[]` ลง `varbinary(max)` จะกิน RAM ~2–3GB ต่อ upload และ DB บวม — ต้องทำ **Option B: stream ลง disk + FileStorage เก็บ path แทน byte[]**

---

## หลักการ (จาก PLAN-076 — ยืนยันจากโค้ดแล้ว)

- Playback ของ learner เสิร์ฟ static file จาก disk (`FileSettings.HostUnc`) — **ไม่แตะ DB byte[] เลย** ⇒ byte[] ใน DB เป็นแค่ archive ไว้ re-extract ตอน activate
- จุดที่ต้องแก้มี 2 ขา: **ขาเขียน** (`ProcessNewContentItemAsync` + `ContentItemsController` upload) และ **ขาอ่านกลับ** (`TryPrepareContentItemForActivationAsync` re-extract)

## Scope

### Phase 1 — ค่าลิมิตใหม่ (แก้จุดเดียว sync 4 ชั้น + web.config)

[ScormPackageLimits.cs](../iLearn.Application/Common/ScormPackageLimits.cs):

```csharp
public const long MaxCompressedPackageBytes = 1024L * 1024 * 1024;             // 1 GB (ZIP)
public const long MaxRequestEnvelopeBytes = MaxCompressedPackageBytes + (10L * 1024 * 1024);  // 1034 MB (auto)
public const int  MaxArchiveEntries = 1000;                                    // คงเดิม
public const long MaxSingleEntryUncompressedBytes = 1024L * 1024 * 1024;       // 1 GB (วิดีโอเดี่ยว)
public const long MaxTotalUncompressedBytes = 2560L * 1024 * 1024;             // 2.5 GB (กัน zip-bomb, อัตรา 2.5×)
```

[iLearn.API/web.config](../iLearn.API/web.config): `maxAllowedContentLength="1084227584"` (= 1034×1024² ต้องตรง `MaxRequestEnvelopeBytes` เป๊ะ — เลข **ไม่เกิน** uint limit 4,294,967,295 ของ IIS) + อัปเดตคอมเมนต์

อัปเดต test `ScormServiceTests` ที่ผูกกับค่าลิมิต (เช่น `RejectsArchiveThatExpandsBeyondAllowedSize`) ให้ semantics เดิมกับเพดานใหม่ — ระวังอย่าสร้าง test ที่ alloc หน่วยความจำระดับ GB จริง (ใช้ mock/stream ความยาวปลอมตามแนว test เดิม)

### Phase 2 — ขาเขียน: stream แทน buffer (หัวใจของงาน)

แก้ `ProcessNewContentItemAsync` ([CourseVersionService.cs](../iLearn.Application/Services/CourseVersionService.cs) ~775-833) และ upload path ใน `ContentItemsController` (~223+):

- **เลิก** `new MemoryStream()` → `CopyToAsync` → `ToArray()`
- **แทนด้วย:** `file.OpenReadStream()` → `CopyToAsync(FileStream)` ตรงไปยัง**ตำแหน่งเก็บ ZIP ถาวร**บน UNC: `{FileSettings.HostUnc}\{CourseFolder}\_archives\{fileStorageGuid}.zip` (สร้างโฟลเดอร์ `_archives` ถ้ายังไม่มี; เขียนลง temp ชื่อ `.tmp` ก่อนแล้ว `File.Move` เพื่อกันไฟล์ครึ่ง ๆ กลาง ๆ)
- `ScormService.ExtractAndParseScormAsync` ปัจจุบันรับ `byte[]` แล้วเขียน temp เอง → เพิ่ม overload รับ **file path** (หรือ `Stream`) แล้วแตกจากไฟล์ตรง ๆ — ห้ามอ่านทั้งไฟล์เข้า RAM; zip-bomb guard เดิม (entries/single/total/ratio) ต้องทำงานเหมือนเดิมทุกข้อ
- `ContentItem.CachedFileLength` ยังต้อง set = ขนาดไฟล์ ZIP เหมือนเดิม (React ใช้แสดงขนาด)

หมายเหตุ memory: ASP.NET Core buffer `IFormFile` ลง temp disk อัตโนมัติ (FileBufferingReadStream) — RAM จะแบนราบ แต่**ดิสก์ temp ของ app pool identity ต้องมีที่ ≥ 1GB** (ดู Phase 4)

### Phase 3 — Schema: `FileStorage` เก็บ path แทน byte[]

- Migration `AddFileStorageStoragePath`: เพิ่มคอลัมน์ `StoragePath nvarchar(500) NULL` + เปลี่ยน `Data` เป็น **nullable**
- **ขาเขียนใหม่:** set `StoragePath` (เก็บ **relative path** จาก HostUnc เช่น `Courses\_archives\{guid}.zip` — กัน HostUnc เปลี่ยนใน config แล้ว path พัง), `Data = null`
- **ขาอ่าน (re-extract ตอน activate):** `TryPrepareContentItemForActivationAsync` (~732-744) — ถ้า `StoragePath` มี → เปิด FileStream จาก path; ถ้าไม่มี → fallback `Data` (รองรับ row เก่าทั้งหมด **ไม่ต้อง backfill ในแผนนี้**)
- **ขาลบ:** จุดที่ลบ `FileStorage` row (version/content ถูกลบ) ให้ลบไฟล์ `_archives\{guid}.zip` ด้วยแบบ best-effort (`try/catch` log warning — ไฟล์ค้างดีกว่า transaction พัง)
- ตรวจว่า**ไม่มี query ไหน SELECT `Data`** ในเส้นทาง list (กติกา CLAUDE.md เดิม) — การทำ `Data` nullable ไม่กระทบ `CachedFileLength`

### Phase 4 — Infra/timeout/พื้นที่ (ตรวจก่อน deploy)

- ตรวจ disk ว่าง: QA/PROD `D:\iLearnContent` (ZIP 1GB + extract 2.5GB ต่อ version) + temp ของ w3wp (ASP.NET buffering ~1GB/upload) — บันทึกตัวเลขจริงลง Implementer Notes
- Upload 1GB บน LAN ~1Gbps ≈ 10–30 วินาที — in-process IIS ไม่มี requestTimeout ของ ANCM มาเกี่ยว แต่ให้ยืนยันจริงด้วย E2E (ถ้าเจอ timeout ชั้นไหน จดและแก้เฉพาะชั้นนั้น)
- Deploy: QA ก่อน → ผ่าน E2E → รอผู้ใช้ไฟเขียว → PROD (pipeline PLAN-083 จะพา `maxAllowedContentLength` ใหม่ขึ้นไปเองตอน deploy)

## Contract ที่เปลี่ยน

- DB: `FileStorages` +`StoragePath nvarchar(500) NULL`, `Data` → NULL-able (backward compatible — row เก่ายังอ่านได้)
- API shape: **ไม่เปลี่ยน** (ไม่มี DTO ไหน expose Data/StoragePath ให้ React — ตรวจซ้ำด้วย grep ก่อนปิดงาน)
- React: **ไม่แตะในแผนนี้** — help text 200MB→1GB อยู่ใน [PLAN-085](PLAN-085-upload-progress-ui.md) (กันชนไฟล์กัน)

## นอก Scope (ห้ามทำ)

- ห้ามแตะขา playback / static file serving
- ห้ามลด/ข้าม zip-bomb + path-traversal guard ใน ScormService
- ห้าม backfill row เก่า (Data→StoragePath) — เปิดเป็นงานแยกถ้าผู้ใช้ต้องการ
- ห้ามแตะไฟล์ React (PLAN-085 ทำคู่ขนาน)

## Implementer Notes (GitHub Copilot — 2026-07-14)

**Phase 1 — Limits:**
- `ScormPackageLimits`: 1GB/1034MB/1GB/2.5GB ตามแผน
- `web.config`: `maxAllowedContentLength="1084227584"` (1034MB)
- Test `RejectsArchiveThatExpandsBeyondAllowedSize` ปรับ chunks 175MB→900MB (3×900=2700MB > 2560MB limit)

**Phase 2 — Streaming:**
- Added `IScormService.SavePackageToArchiveAsync(Stream, string)` → streams to `{FileUnc}\_archives\{guid}.zip` via temp+move
- Added `IScormService.ExtractAndParseScormFromFileAsync(string zipFilePath, string folderName)` → extracts directly from disk file
- Added `IScormService.DeleteArchiveFile(string)` + `GetArchiveFullPath(string)` with path-traversal guard
- `ProcessNewContentItemAsync` + `ContentItemsController.Upload`: replaced `MemoryStream→ToArray()` with `file.OpenReadStream()→SavePackageToArchiveAsync()`; sets `FileStorage.StoragePath`, `Data=null`
- `TryPrepareContentItemForActivationAsync`: uses `StoragePath` → `ExtractAndParseScormFromFileAsync` if available, else falls back to legacy `Data` blob
- `ContentPublicationService.PublishAsync`: same StoragePath/Data fallback
- `ContentItemsController` bulk re-extract: same StoragePath/Data fallback
- Download endpoint: serves from FileStream if StoragePath exists, else Data blob
- `CourseService` hard-delete: calls `DeleteArchiveFile` before removing FileStorage row

**Phase 3 — Schema:**
- Migration `20260714062106_AddStoragePathToFileStorage`: adds `StoragePath nvarchar(500) NULL`
- `FileStorage.Data` was already nullable (`byte[]?`) — no migration needed for that
- AppDbContext Fluent API: `HasMaxLength(500)` on StoragePath

**Phase 4 — Verification:**
- `dotnet build iLearn.sln` → 0 errors ✓
- `dotnet test` → 178 passed, 0 failed ✓
- grep DTOs/Mappings for StoragePath → not exposed in API responses ✓
- Disk space / timeout / E2E → deferred to QA deploy (requires running API + large test file)

## นอก Scope (ห้ามทำ)

- ห้ามแตะขา playback / static file serving
- ห้ามลด/ข้าม zip-bomb + path-traversal guard ใน ScormService
- ห้าม backfill row เก่า (Data→StoragePath) — เปิดเป็นงานแยกถ้าผู้ใช้ต้องการ
- ห้ามแตะไฟล์ React (PLAN-085 ทำคู่ขนาน)

## Verification

```powershell
dotnet build iLearn.Tests -o artifacts\verify-test
dotnet test artifacts\verify-test\iLearn.Tests.dll
Remove-Item -Recurse -Force artifacts\verify-test
```

E2E บน QA (หลัง deploy):

1. สร้าง SCORM package ทดสอบ ~800MB–1GB (เอา golden package เดิมยัดไฟล์ binary สุ่ม — ต้อง**สุ่มจริง** (incompressible) กันติด ratio guard; จด script ที่ใช้ลง Notes)
2. อัพโหลดผ่านหน้า React → สำเร็จ; **watch memory w3wp ระหว่างอัพโหลด** — ต้องไม่พุ่งตามขนาดไฟล์ (เกณฑ์: เพิ่ม < 300MB)
3. DB: row ใหม่มี `StoragePath` ถูกต้อง, `Data IS NULL`; ไฟล์ ZIP อยู่ใน `_archives\` จริง
4. Learner เปิดเล่นคอร์สได้ (playback ไม่กระทบ)
5. ทดสอบ re-extract: ลบโฟลเดอร์ extract บน disk แล้ว trigger activate ใหม่ → ระบบแตกจาก `StoragePath` สำเร็จ
6. Regression content เก่า (row ที่มี Data): activate/เล่น ยังทำงาน (fallback path)
7. Boundary: ไฟล์ > 1GB (เช่น 1.1GB) → reject ด้วย error ชัดเจน ไม่ใช่ timeout/พัง

## Implementer Notes

*(เติมหลังทำเสร็จ)*

## Reviewer Sign-off (Claude Code, 2026-07-14)

ตรวจ diff เต็มทุกไฟล์อิสระ + build/test เองซ้ำ (ไม่เชื่อ Implementer Notes อย่างเดียว):

- **Limits + web.config:** 1GB/1034MB/1GB/2.5GB ตรงแผน; `maxAllowedContentLength="1084227584"` = 1034×1024² เป๊ะ (< uint max) ✅
- **Streaming (`SavePackageToArchiveAsync`):** temp `.tmp` → `File.Move` atomic + cleanup temp on failure ✅ ไม่อ่านทั้งไฟล์เข้า RAM
- **Path consistency (จุดเสี่ยงสุด — ตรวจแล้วผ่าน):** `FileSettings.FileUnc = Path.Combine(HostUnc, CourseFolder)` → save เขียน `{FileUnc}\_archives\{guid}.zip`, return relative `{CourseFolder}\_archives\{guid}.zip`, `GetArchiveFullPath` อ่าน `{HostUnc}\{relative}` = ที่เดียวกันเป๊ะ ✅ (config ปัจจุบัน HostUnc=`D:\iLearnContent` local disk → `File.Move` atomic ได้จริง)
- **zip-bomb + path-traversal guard:** เมธอดใหม่ `ExtractAndParseScormFromFileAsync` เรียก `IsValidZipFile`/`FindManifestPath`/`EnsureArchiveEntriesStayUnderPackageRoot` ชุดเดียวกับเมธอด byte[] เดิมเป๊ะ — guard entries(1000)/single(1GB)/total(2.5GB)+overflow-check ยัง active ครบ (ScormService.cs:312-342) ✅ `GetArchiveFullPath` มี path-traversal guard ✅
- **Fallback StoragePath→Data ครบทุกเส้นทางอ่าน:** `TryPrepareContentItemForActivationAsync`, `ContentPublicationService.PublishAsync`, `ContentItemsController` bulk re-extract + download endpoint — row เก่า (Data blob) ยังอ่าน/เล่นได้ ✅ (download ใช้ `FileStreamResult` → ASP.NET dispose stream อัตโนมัติ ✅)
- **Delete:** `CourseService` hard-delete เรียก `DeleteArchiveFile` best-effort ก่อนลบ row ✅
- **CachedFileLength:** set = `file.Length` ทั้ง 2 ขาเขียน ✅ (ContentItemsController เดิม set จาก `savedFile.Length` → เปลี่ยนเป็น `file.Length` เหมือนกัน)
- **migration/schema:** `StoragePath nvarchar(500) NULL` + `HasMaxLength(500)`; `Data` nullable อยู่แล้ว — backward compatible ✅
- **Verify อิสระ:** `dotnet build iLearn.Tests` = 0 warn/0 err; `dotnet test` = **178 passed, 0 failed** (11s — ยืนยัน test ไม่ OOM); grep DTO/Mapping ไม่มี expose StoragePath/Data ✅

### ⚠️ Finding 1 (MEDIUM — ต้องตามแก้): orphaned archive เมื่อ SCORM invalid
`ProcessNewContentItemAsync` (CourseEditor upload+activate ทันที) — `SavePackageToArchiveAsync` เขียนไฟล์ archive **ถาวร**บน disk + สร้าง FileStorage row ก่อน แล้วค่อย `ExtractAndParseScormFromFileAsync`. ถ้า SCORM invalid (manifest เสีย/zip-bomb เกิน 2.5GB) → extract โยน `InvalidScormPackageException`, catch แค่ set `IsActive=false` + throw — **ไฟล์ archive (สูงสุด 1GB) + FileStorage row ค้างบน disk ไม่ถูกลบ** → disk leak สะสมทุกครั้งที่อัพ SCORM เสีย. เดิม (byte[]) ไม่มีไฟล์ disk orphan (เก็บใน DB row + temp ลบใน finally).
**แก้:** ใน catch เรียก `_scormService.DeleteArchiveFile(storagePath)` + ลบ FileStorage/ContentItem row ที่เพิ่งสร้าง (หรือ save archive ทีหลัง extract สำเร็จ). *(ContentItemsController.Upload ไม่โดน — มันเก็บไฟล์เฉย ๆ ไม่ extract inline)*

### Finding 2 (MINOR): ScormServiceTests เขียน ~2.7GB ผ่าน compressor
`RejectsArchiveThatExpandsBeyondAllowedSize` เพิ่ม chunks 175MB→900MB×3 = 2.7GB uncompressed. Test ผ่าน 11s (byte ซ้ำ compress ดี ไม่ OOM บนเครื่องนี้) แต่หนักกว่าเดิม (525MB) — อาจช้า/กิน RAM บน CI ที่ทรัพยากรต่ำ. พิจารณาลดขนาด chunk ให้แตะ threshold พอดี (เช่น 860MB×3=2580MB > 2560MB) เพื่อลดภาระ

### Phase 4 ยังไม่ทำ (deferred ตาม note): E2E upload 1GB จริง + วัด RAM w3wp + ตรวจ disk ว่าง QA/PROD + boundary >1GB — **ต้องทำบน QA ก่อน deploy PROD** (จุดที่โค้ด review แทนไม่ได้: RAM แบนราบจริงไหม, timeout ที่ 1GB, ANCM in-process buffering)

**สรุป: โค้ดผ่านรีวิว — สถาปัตยกรรม streaming/fallback/guard ถูกต้องครบ ไม่มี regression จาก build/test/diff. ต้องตาม Finding 1 (disk leak) ก่อนถือว่าปิดสมบูรณ์ + รัน Phase 4 E2E บน QA ก่อน PROD**
