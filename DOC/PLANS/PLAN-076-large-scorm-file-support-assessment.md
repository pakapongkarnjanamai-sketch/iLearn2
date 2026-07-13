# PLAN-076: ประเมินการรองรับ SCORM ไฟล์ใหญ่ (>1GB) — สถาปัตยกรรม, ข้อจำกัด, ตัวเลือก, ข้อเสนอ

- **Status:** ON HOLD — รอผู้ใช้ตัดสินใจ (2026-07-13 ผู้ใช้ขอยังไม่เลือกทาง; สนใจแนว "แบ่งเป็นตอนสั้น ๆ" — ดู §3.1 ด้านล่าง). ยังไม่แตกเป็นแผน implement
- **Assigned:** — (ยังไม่มอบ implementer — เป็นการประเมิน/ตัดสินใจ)
- **Author:** Claude Code (planner)
- **Reviewer:** —
- **สร้างเมื่อ:** 2026-07-13
- **อ้างอิง:** [PLAN-041](PLAN-041-scorm-upload-413-hosting-limit.md) (413 hosting limit), [PLAN-042](PLAN-042-scorm-upload-envelope-limit-separation.md) (แยก envelope/content limit), [PLAN-045](PLAN-045-production-cutover-ilearn2.md) ข้อ 11/42 (SCORM เก็บ 2 ที่)

> มาจากคำถามผู้ใช้ (2026-07-13): "SCORM content ที่มีขนาดมากกว่า 1GB จะมีปัญหาอะไรไหม" — คำตอบสั้น: **อัพโหลดไม่ได้เลยในสถาปัตยกรรมปัจจุบัน** (ถูกปฏิเสธที่ ~100–110MB) และการยกลิมิตเฉย ๆ มีความเสี่ยงสูงเพราะโมเดลจัดเก็บโหลดไฟล์ทั้งก้อนเข้าหน่วยความจำ เอกสารนี้ประเมินทางเลือกก่อนตัดสินใจ

---

## 1. สถาปัตยกรรมปัจจุบัน (ยืนยันจากโค้ดจริง)

### 1.1 SCORM ถูกเก็บ **2 ที่** (สำคัญที่สุดต่อการตัดสินใจ)

| ที่เก็บ | อะไร | ใช้ทำอะไร | โค้ด |
|---|---|---|---|
| **DB `FileStorages.Data`** (`byte[]`) | ZIP ต้นฉบับทั้งก้อน | เก็บไว้ re-extract ตอน activate/auto-prepare | [FileStorage.cs:10](../iLearn.Domain/Entities/FileStorage.cs) — column ไม่ระบุ type → EF default = **`varbinary(max)`** |
| **Disk/UNC `{FileUnc}\{folderName-guid}\...`** | ไฟล์ที่แตกจาก ZIP แล้ว | **ตัวที่ learner เล่นจริง** (เสิร์ฟเป็น static file ผ่าน URL) | [ScormService.cs:118](../iLearn.Infrastructure/Services/ScormService.cs) `ZipFile.ExtractToDirectory` |

- **การเล่นคอร์สของ learner ไม่แตะ DB byte[] เลย** — เสิร์ฟจาก static files บน disk (`_settings.FileUrl`) ⇒ ปัญหาไฟล์ใหญ่อยู่ที่ **ขา upload/storage เท่านั้น** ไม่ใช่ขา playback
- byte[] ใน DB ใช้ตอนเดียว: `TryPrepareContentItemForActivationAsync` โหลด `fileStorage.Data` กลับมา re-extract ([CourseVersionService.cs:732-744](../iLearn.Application/Services/CourseVersionService.cs))

### 1.2 Upload flow (จุดที่โหลดทั้งก้อนเข้า RAM)

`ProcessNewContentItemAsync` ([CourseVersionService.cs:775-833](../iLearn.Application/Services/CourseVersionService.cs)) — เหมือนกันใน `ContentItemsController` ([ContentItemsController.cs:248-251](../iLearn.API/Controllers/ContentItemsController.cs)):

```
IFormFile → new MemoryStream() → file.CopyToAsync(ms) → ms.ToArray()  // (1) buffer เข้า RAM แล้ว copy เป็น byte[] อีกชุด
         → fileStorage.Data = byte[]  → _fileStorageRepository.AddAsync   // (2) EF track byte[] ทั้งก้อน → INSERT varbinary(max)
         → ScormService.ExtractAndParseScormAsync(byte[], folder)         // (3) File.WriteAllBytesAsync(temp, byte[]) → unzip ลง disk
```

**Upload surfaces ที่มี pattern นี้ (2 จุด):**
- `POST Courses/{courseId}/versions` + `PUT Courses/versions/{versionId}` ([CoursesController.cs:358, 416](../iLearn.API/Controllers/CoursesController.cs))
- `ContentItemsController` upload ([ContentItemsController.cs:223+](../iLearn.API/Controllers/ContentItemsController.cs))

### 1.3 ชั้นของ limit ที่บล็อกไฟล์ใหญ่ (ทุกชั้น sync กันที่ 100–110MB)

| # | ชั้น | ค่า | โค้ด | ผลเมื่อเกิน |
|---|---|---|---|---|
| 1 | IIS request filtering | `maxAllowedContentLength=115343360` (**110MB**) | [web.config:7](../iLearn.API/web.config) | HTTP 413 (ก่อนถึง app) |
| 2 | Kestrel + IISServerOptions | `MaxRequestBodySize=110MB` | [Program.cs:36-41](../iLearn.API/Program.cs) | HTTP 413 |
| 3 | per-endpoint attribute | `[RequestSizeLimit/RequestFormLimits]=110MB` | CoursesController ×3, ContentItemsController ×1 | HTTP 413 |
| 4 | validation ก่อนบันทึก | `file.Length > 100MB` | [ScormUploadValidation.cs:35](../iLearn.Application/Common/ScormUploadValidation.cs) | 400 InvalidScormPackage |
| 5 | ScormService (แตก ZIP) | compressed 100MB / single entry 100MB / **total uncompressed 250MB** / entries 1000 | [ScormPackageLimits.cs](../iLearn.Application/Common/ScormPackageLimits.cs) | 400 InvalidScormPackage |

ค่ากลางทั้งหมดอยู่ที่ [ScormPackageLimits.cs](../iLearn.Application/Common/ScormPackageLimits.cs):
```
MaxCompressedPackageBytes      = 100 MB   // ขนาด ZIP
MaxRequestEnvelopeBytes        = 110 MB   // request ทั้ง envelope (ZIP + form overhead)
MaxSingleEntryUncompressedBytes= 100 MB   // ไฟล์เดี่ยวหลังแตก
MaxTotalUncompressedBytes      = 250 MB   // รวมหลังแตกทั้ง package (กัน zip-bomb)
MaxArchiveEntries              = 1000
```

⇒ ไฟล์ 1GB โดนบล็อกที่ **ชั้น 1 (IIS 413)** ก่อนเลย ต่อให้ทะลุมาได้ก็ตายที่ชั้น 4/5

---

## 2. ทำไม "แค่ยกตัวเลข limit" ไม่ปลอดภัย

การยก 5 ชั้นให้รับ 1GB ทำได้ทางเทคนิค แต่โมเดลจัดเก็บปัจจุบันจะพังเชิง resource:

1. **Memory blow-up ต่อ 1 upload** — `MemoryStream` + `ms.ToArray()` = ไฟล์ค้างใน RAM **~2 เท่า** (stream buffer + array copy) แล้ว EF ยัง track byte[] อีกชุด → ไฟล์ 1GB กิน **~2–3GB RAM ต่อ 1 request** อัพพร้อมกัน 2–3 คน = OOM/รีไซเคิล w3wp
2. **`varbinary(max)` 1GB/row** — DB บวมเร็ว, backup/restore ช้า, transaction log พอง, และ EF ไม่ได้ stream (โหลดทั้ง row เข้า memory ตอน re-extract ที่ [CourseVersionService.cs:732](../iLearn.Application/Services/CourseVersionService.cs))
3. **LOH / GC pressure** — byte[] ขนาด GB ไปอยู่ Large Object Heap → GC สะดุด, fragmentation
4. **Double storage** — เก็บทั้ง DB (1GB) + disk ที่แตกแล้ว (อาจ >1GB) ต่อ 1 version ⇒ สิ้นเปลืองสองเท่า
5. **Timeout** — upload 1GB + unzip + INSERT ใช้เวลานานเกิน request timeout / connection ของ IIS/Kestrel ปกติ

> ข้อ 4 คือ leverage สำคัญ: เนื่องจาก **playback เสิร์ฟจาก disk ล้วน** DB byte[] จึงเป็นแค่สำเนา archive — ทางเลือกที่ดีที่สุดหลายทางคือ "เลิกเก็บก้อนใหญ่ใน DB"

---

## 3. ตัวเลือก (พร้อม trade-off)

### 3.1 Option D — แบ่งเป็นตอนสั้น ๆ (หลาย content item ต่อ 1 version) — **ทำได้เลยวันนี้ ไม่ต้องแก้โค้ด**

- **ข้อเท็จจริงจากโค้ด:** 1 course version รองรับ **หลาย content item** อยู่แล้ว — `BuildOrderedContentItemIdsAsync` วนรับหลายไฟล์ในคำขอเดียว ([CourseVersionService.cs:518-554](../iLearn.Application/Services/CourseVersionService.cs)) แต่ละไฟล์เป็น SCORM package แยก มี manifest/launch/progress ของตัวเอง เรียงตาม `Order`
- **ทำ:** ผู้สร้างคอร์ส author เนื้อหาก้อนใหญ่ให้เป็น **หลาย SCORM module ย่อย** (แต่ละอัน < 100MB) แล้วอัปเป็นหลาย content item ในเวอร์ชันเดียว — learner เรียนเรียงเป็นตอน
- **ข้อดี:** ใช้ความสามารถที่มีอยู่แล้ว **ไม่ต้องแก้โค้ด/ไม่ต้อง deploy** เลย, progress tracking ละเอียดขึ้น (จบทีละตอน), หลบทุกข้อจำกัด memory/DB ในข้อ 2
- **ข้อเสีย/ข้อควรระวัง:**
  - ต้อง author เป็นหลาย package จริง ๆ — **ไม่ใช่แค่หั่นไฟล์ ZIP เป็นชิ้น** (แต่ละตอนต้องมี `imsmanifest.xml` + launch page ของตัวเอง ไม่งั้นไม่ผ่าน validation)
  - ถ้าไฟล์ใหญ่เพราะ **วิดีโอเดี่ยวก้อนใหญ่** (เช่น 1 คลิป 800MB) การแบ่งตอนก็ยังติดเพดาน 100MB/ตอนอยู่ดี → เคสนี้ต้องไป Option C (แยก media)
- **เหมาะเมื่อ:** เนื้อหาหนักเพราะมี**หลายบท/หลายสื่อ** ที่แยกเป็นโมดูลได้ตามธรรมชาติ

> ⇒ ถ้าเคสของคุณแบ่งเป็นตอนได้ตามเนื้อหา นี่คือทางที่**เริ่มได้ทันทีโดยไม่มีความเสี่ยงทางเทคนิค** — แนะนำลองทางนี้ก่อน แล้วค่อยพิจารณา B/C ถ้ายังไม่พอ

### Option A — ยกลิมิตอย่างเดียว (คงสถาปัตยกรรมเดิม)
- **ทำ:** ขยาย 5 ชั้นเป็นค่าที่ต้องการ (เช่น 1–2GB) + ปรับ timeout
- **ข้อดี:** งานน้อย, เปลี่ยนแค่ค่าคงที่/config
- **ข้อเสีย:** ได้ความเสี่ยงข้อ 2 ครบทุกข้อ — **ไม่แนะนำเกิน ~300–500MB** และยังไม่แตะ memory model (ยัง `ToArray()` อยู่)
- **เหมาะเมื่อ:** ต้องการแค่ขยับเพดานเล็กน้อย (เช่น 100MB → 300MB) ชั่วคราว

### Option B — Streaming upload + เลิกเก็บ byte[] ใน DB (เก็บ ZIP บน disk/blob)
- **ทำ:**
  - เปลี่ยน `MemoryStream/ToArray` → stream ตรงจาก `file.OpenReadStream()` ลง temp file (ไม่ผ่าน RAM ทั้งก้อน) + ใช้ multipart streaming (`Request.Body` reader แทน `[FromForm]` buffering)
  - `FileStorage` เก็บ **path/reference** แทน `byte[]` (ZIP ต้นฉบับไปอยู่บน disk/UNC ข้าง ๆ folder ที่แตก หรือ blob store)
  - re-extract อ่านจาก path แทน DB
- **ข้อดี:** memory คงที่ไม่ว่าไฟล์ใหญ่แค่ไหน, DB ไม่บวม, รองรับ GB ได้จริง
- **ข้อเสีย:** งานกลาง–ใหญ่ (แตะ upload pipeline + schema `FileStorage` + migration + deploy script ต้อง sync ไฟล์ ZIP ต้นฉบับด้วย), ต้องคิด backfill ของเดิมใน DB
- **เหมาะเมื่อ:** ต้องรองรับ >500MB เป็นปกติ — **ตัวเลือกที่ยั่งยืนที่สุด**

### Option C — ไม่รับไฟล์ใหญ่เข้าระบบ แต่แยก media ออก (external hosting / CDN)
- **ทำ:** ให้ SCORM package เล็ก (โค้ด/manifest) ส่วนวิดีโอ/สื่อหนักชี้ไป URL ภายนอก (media server/CDN/streaming)
- **ข้อดี:** ระบบไม่ต้องแบกไฟล์ GB เลย, playback ลื่นกว่า (streaming), ประหยัด storage
- **ข้อเสีย:** เปลี่ยน workflow ของคนทำคอร์ส (ต้อง author package แบบอ้าง external media), ต้องมี media host, offline ไม่ได้
- **เหมาะเมื่อ:** ไฟล์ใหญ่เพราะ **วิดีโอ** (เคสที่พบบ่อยสุดของ SCORM >1GB)

---

## 4. คำถามที่ต้องการคำตอบก่อนเลือกทาง (Decision points)

1. **ขนาดสูงสุดที่ต้องรองรับจริง?** — 300MB, 1GB, หรือ 5GB+ ? (กำหนดว่าพอ Option A ไหม หรือต้อง B/C)
2. **ไฟล์ใหญ่เพราะอะไร?** — ถ้าเป็นวิดีโอฝังใน package → Option C อาจตอบโจทย์กว่าและถูกกว่ามาก
3. **ความถี่/จำนวน concurrent upload?** — กำหนดความเสี่ยง memory ของ Option A
4. **นโยบายเก็บ ZIP ต้นฉบับ** — จำเป็นต้องเก็บ archive ไว้ re-extract ไหม หรือเก็บแค่ไฟล์ที่แตกแล้วพอ? (ถ้าไม่ต้องเก็บ archive เลย ยิ่งลดภาระ DB)
5. **Storage budget บน disk/UNC** — ปัจจุบัน QA/PROD ใช้ `D:\iLearnContent` (PLAN-046) — มีที่พอสำหรับไฟล์ GB × หลายคอร์สไหม?

## 5. ข้อเสนอของ planner (คร่าว ๆ — รอ decision)

- ถ้าเป้าหมายจริงคือ **"รองรับวิดีโอในคอร์ส"** → แนะนำ **Option C** (แยก media) เป็นหลัก + คง limit เดิมสำหรับตัว package
- ถ้าต้องรับ **ไฟล์ SCORM ก้อนใหญ่จริง ๆ 500MB–2GB** → แนะนำ **Option B** (streaming + เลิก byte[] ใน DB) เป็นการลงทุนที่ยั่งยืน
- **Option A ใช้เป็น quick unblock ชั่วคราวเท่านั้น** และไม่ควรเกิน ~300MB โดยต้องแก้ memory model (`ToArray` → stream to temp) ควบคู่ ไม่งั้นเสี่ยง OOM

## 6. ถ้าตัดสินใจทำ — งาน spike/POC ที่ควรมีก่อนแผน implement

- [ ] วัด memory จริงของ upload ปัจจุบันที่ 100MB (baseline) เทียบกับ prototype streaming
- [ ] POC `FileStorage` เก็บ path แทน byte[] + ทดสอบ re-extract จาก path (Option B)
- [ ] ตรวจ deploy script (`deploy-*.ps1`) ว่า sync ไฟล์ course/ZIP ต้นฉบับข้าม stamp ได้ถูกต้อง (ปัจจุบัน sync เฉพาะ app artifacts — ไฟล์ content อยู่บน share แยก)
- [ ] ทดสอบ IIS/Kestrel timeout กับ upload ขนาดใหญ่ (อาจต้อง `requestTimeout` ใน aspNetCore + Kestrel keep-alive)
- [ ] ประเมิน backward-compat: version เก่าที่มี byte[] ใน DB อยู่แล้ว จะ migrate/backfill อย่างไร

## Constraints (สำหรับแผน implement ในอนาคต)

- ❌ ห้ามแตะขา **playback** ของ learner (เสิร์ฟ static จาก disk — ทำงานถูกอยู่แล้ว)
- ❌ ห้ามลด security ของ ScormService (path traversal guard, zip-bomb guard, manifest validation) — ถ้าขยาย `MaxTotalUncompressedBytes` ต้องคง guard เชิงจำนวน entry/single-entry ไว้
- ⚠️ การแก้ limit ต้อง sync **ทั้ง 5 ชั้น** พร้อมกัน (บทเรียนจาก PLAN-041/042) — แก้ชั้นเดียวจะเจอ 413 ที่ชั้นอื่น

## หมายเหตุ

- เอกสารนี้เป็น **assessment** ตามที่ผู้ใช้ขอ ("ประเมินการรองรับไฟล์ใหญ่") — ยังไม่ใช่แผน implement และยังไม่มอบ implementer จนกว่าผู้ใช้จะเลือกทิศทางจาก §4
