# PLAN-080: ขยายลิมิตขนาด SCORM package 100MB → 200MB (Option A ของ PLAN-076)

- **Status:** DONE
- **Assigned:** GitHub Copilot
- **Reviewer:** Claude Code
- **สร้างเมื่อ:** 2026-07-13
- **อ้างอิง:** [PLAN-076](PLAN-076-large-scorm-file-support-assessment.md) (Option A — ยกลิมิต, ปลอดภัย ≤~300MB), [PLAN-041](PLAN-041-scorm-upload-413-hosting-limit.md) + [PLAN-042](PLAN-042-scorm-upload-envelope-limit-separation.md) (ต้นแบบการแก้ลิมิต — บทเรียน: ต้องแก้ครบทุกชั้น)

> ผู้ใช้สั่ง (2026-07-13): ขยายขนาด content เป็น 200MB — ตรงกับ **Option A ของ PLAN-076** (100MB → 200MB ยังอยู่ในเกณฑ์ยกลิมิตที่ปลอดภัย ไม่ต้อง refactor เป็น streaming/blob ตอนนี้)

---

## บทเรียนสำคัญ (จาก PLAN-041/042): ต้องแก้ให้ทุกชั้น sync กัน

ลิมิตปัจจุบันถูกล็อก 100–110MB ผ่าน 5 ชั้น แต่ **4 ใน 5 ชั้นอ้างอิงค่า constant กลางที่ [ScormPackageLimits.cs](../iLearn.Application/Common/ScormPackageLimits.cs)** จึงแก้จุดเดียว sync ทั้งหมด — **ยกเว้น web.config ที่ hardcode ตัวเลข ต้องแก้แยก**

| ชั้น | ไฟล์ | อ้างอิง constant? | ต้องแก้เอง? |
|---|---|---|---|
| IIS request filtering | `iLearn.API/web.config` | ❌ hardcode `115343360` | ✅ **ต้องแก้** |
| Kestrel + IISServerOptions | `iLearn.API/Program.cs` | ✅ ใช้ `ScormPackageLimits.MaxRequestEnvelopeBytes` | auto |
| per-endpoint attribute | `CoursesController` ×3, `ContentItemsController` ×1 | ✅ ใช้ `ScormPackageLimits.MaxRequestEnvelopeBytes` | auto |
| validation ก่อนบันทึก | `ScormUploadValidation.cs` | ✅ ใช้ `ScormPackageLimits.MaxCompressedPackageBytes` | auto |
| แตก ZIP (zip-bomb guard) | `ScormService.cs` | ✅ ใช้ constants ทั้งหมด | auto |

## Scope

### 1. แก้ [ScormPackageLimits.cs](../iLearn.Application/Common/ScormPackageLimits.cs) — ค่ากลาง

ปัจจุบัน:
```csharp
public const long MaxCompressedPackageBytes = 100L * 1024 * 1024;              // 100 MB
public const long MaxRequestEnvelopeBytes = MaxCompressedPackageBytes + (10L * 1024 * 1024);  // 110 MB
public const int  MaxArchiveEntries = 1000;
public const long MaxSingleEntryUncompressedBytes = 100L * 1024 * 1024;        // 100 MB
public const long MaxTotalUncompressedBytes = 250L * 1024 * 1024;              // 250 MB
```

เปลี่ยนเป็น:
```csharp
public const long MaxCompressedPackageBytes = 200L * 1024 * 1024;              // 200 MB (ZIP)
public const long MaxRequestEnvelopeBytes = MaxCompressedPackageBytes + (10L * 1024 * 1024);  // 210 MB (auto)
public const int  MaxArchiveEntries = 1000;                                   // คงเดิม (จำนวนไฟล์ไม่เกี่ยวขนาด)
public const long MaxSingleEntryUncompressedBytes = 200L * 1024 * 1024;        // 200 MB (decision #2)
public const long MaxTotalUncompressedBytes = 500L * 1024 * 1024;             // 500 MB (decision #1)
```

- `MaxRequestEnvelopeBytes` คำนวณอัตโนมัติ (+10MB overhead) = 210MB
- **decision #1 — `MaxTotalUncompressedBytes`:** compressed 200MB (iSpring media บีบอัดไม่มาก) แตกออกอาจ ~250–400MB → ตั้ง **500MB** ให้มี headroom แต่ยังกัน zip-bomb (อัตราส่วน 2.5× ของ compressed ยังสมเหตุสมผล) — ถ้าผู้ใช้อยากคุมแน่นกว่าปรับได้
- **decision #2 — `MaxSingleEntryUncompressedBytes`:** วิดีโอเดี่ยวในแพ็กเกจอาจใหญ่ขึ้น → ขยายเป็น 200MB ให้สอดคล้อง (ไฟล์เดี่ยวไม่ควรเกินขนาด compressed ทั้งก้อน)

### 2. แก้ [web.config](../iLearn.API/web.config) — IIS (ชั้นเดียวที่ hardcode)

```xml
<!-- 210 MB = allow multipart envelope overhead above 200 MB SCORM payload -->
<requestLimits maxAllowedContentLength="220200960" />
```
- `220200960` = 210 × 1024 × 1024 = ต้อง **ตรงกับ `MaxRequestEnvelopeBytes`** เป๊ะ (บทเรียน PLAN-042: ถ้าไม่ sync จะเจอ 413 ที่ชั้น IIS ทั้งที่ app ยอม)
- อัปเดตคอมเมนต์ให้ตรงตัวเลขใหม่

### 3. ตรวจ published web.config หลัง deploy (บทเรียน PLAN-041)

`dotnet publish` จะ merge `<aspNetCore>` เข้า web.config — ต้องยืนยันว่า `maxAllowedContentLength` ใหม่ยังอยู่ใน artifact หลัง publish (ไม่ถูก overwrite)

## Constraints

- ❌ **ห้าม refactor memory model** (MemoryStream/ToArray → streaming) ในแผนนี้ — นั่นคือ Option B ของ PLAN-076 (งานใหญ่แยกต่างหาก) แผนนี้เป็น Option A ล้วน (ยกตัวเลข)
- ❌ ห้ามแตะ path/zip-bomb security guard ใน `ScormService` (แค่ปรับ**ค่าเพดาน** ผ่าน constant ไม่แตะ logic ตรวจ)
- ❌ ห้ามแก้ตัวเลขในโค้ดแบบ hardcode — ต้องผ่าน constant ที่ `ScormPackageLimits` เท่านั้น (web.config เป็นข้อยกเว้นเดียวที่ hardcode ได้เพราะเป็น XML config)
- ⚠️ **Memory note:** upload 200MB ผ่าน `MemoryStream`+`ToArray()` ใช้ RAM ~400–600MB/request (buffer + array copy + EF tracking) — ที่ 200MB ยังรับได้ (PLAN-076 ประเมิน Option A ปลอดภัย ≤~300MB) แต่**ถ้าอนาคตต้องเกิน 300MB ต้องไป Option B** (streaming) ไม่ใช่ยกตัวเลขต่อ

## Decision points (ผู้ใช้)

1. **`MaxTotalUncompressedBytes`** — ตั้ง 500MB (แผน default, headroom 2.5×) หรืออยากคุมแน่นกว่า/หลวมกว่า?
2. **`MaxSingleEntryUncompressedBytes`** — ตั้ง 200MB (แผน default) หรือคง 100MB?
3. **deploy target** — ขึ้นทั้ง QA และ PROD เลย หรือ QA ก่อน? (แก้เฉพาะ `iLearn.API` — deploy `deploy-api.ps1` / `deploy-api-prod.ps1`)

## Verify

- [x] `dotnet build iLearn.Tests` + `dotnet test` ผ่าน — updated test `RejectsArchiveThatExpandsBeyondAllowedSize` (90MB×3→175MB×3 = 525MB > 500MB) — 178 tests pass
- [x] grep ยืนยันไม่มีที่ไหน hardcode `104857600` (100MB) / `115343360` (110MB) / `262144000` (250MB) หลงเหลือนอก `ScormPackageLimits` + web.config (เหลือเฉพาะ DOC/PLANS เอกสาร)
- [x] `dotnet publish` แล้วตรวจ `web.config` artifact มี `maxAllowedContentLength="220200960"` + `<aspNetCore>` ครบ
- [ ] **E2E บน QA:** upload SCORM package จริงขนาด **150–200MB** — รอ deploy ขึ้น QA
- [ ] ทดสอบ boundary: upload ไฟล์ ~205MB (เกิน 200MB) → ต้องถูกปฏิเสธ — รอ deploy ขึ้น QA
- [ ] ตรวจ memory ของ w3wp ระหว่าง upload 200MB — รอ deploy ขึ้น QA

## Implementer Notes

- **Test update:** `ScormServiceTests.ExtractAndParseScormAsync_RejectsArchiveThatExpandsBeyondAllowedSize` ใช้ 90MB×3=270MB (>250MB เดิม) ไม่เกิน 500MB ใหม่ → ปรับเป็น 175MB×3=525MB (>500MB)
- ไม่มีความเบี่ยงเบนจากแผน — แก้ค่า constant + web.config ตรงตาม Scope ทั้ง 2 จุด
- E2E test + memory check รอ deploy ขึ้น QA

## Reviewer Sign-off — Code Review (Claude Code — 2026-07-14)

**Code review: ผ่าน** — ตรวจ diff ทั้ง 3 ไฟล์ + รัน verification ซ้ำอิสระ:

- **Constants:** 200MB / envelope auto 210MB / single-entry 200MB / total 500MB — ตรงสเปคแผนเป๊ะ (ใช้ default ของ decision #1/#2) ✅
- **web.config:** `220200960` = 210×1024² ตรวจเลขแล้วถูกต้อง + คอมเมนต์อัปเดต ✅
- **Test update ถูก semantics:** 175MB×3 = 525MB > 500MB (ยังทดสอบ *total* limit) และแต่ละ entry 175MB < 200MB single-entry limit — ไม่เผลอเปลี่ยนไปทดสอบผิดตัว ✅
- **Independent verification:** tests **178/178 ผ่าน** (รันเอง); grep ไม่มี hardcode ค่าเก่า (104857600/115343360/262144000) หลงเหลือใน code/config; `dotnet publish` artifact มี `maxAllowedContentLength="220200960"` + `<aspNetCore>` ครบ (บทเรียน PLAN-041 — ตรวจเอง) ✅

### Reviewer finding (แก้แล้วโดย reviewer ระหว่างรีวิว)
**ข้อความ help text ฝั่ง UI ยังบอกลิมิตเก่า 5 จุด** (ไม่มี client-side validation บล็อกจริง — เป็น text ล้วน แต่ทำให้ admin เข้าใจผิดว่ายัง 100MB):
- `iLearn.Admin.React/src/pages/content-library/ContentItemEditorPage.tsx:177` — "Max 100MB" → **200MB**
- `iLearn.Admin/Views/ContentItems/Index.cshtml:123`, `Views/Courses/VersionForm.cshtml:108`, `Views/Courses/Editor.cshtml:125,188` — "100 MB ZIP … 250 MB expanded" → **"200 MB ZIP … 500 MB expanded"**
- Verify หลังแก้: `npm run lint` + `npm run build` (React) ผ่าน, `dotnet build iLearn.Admin` ผ่าน ✅
- **ผลต่อ deploy scope:** จากเดิม API อย่างเดียว → ต้อง deploy **admin-react + admin (MVC)** ด้วย (text ใหม่)

### สถานะ: DONE (code review ผ่าน + UI text ครบ) — รอ decision #3 (deploy QA ก่อน หรือ QA+PROD) + E2E upload จริงก่อน VERIFIED
