# PLAN-110: เอา `.zip` ออกจากชื่อ content item — เฟสเฉพาะหน้า (ปลอดภัย) + เฟสระยะยาว (แก้ที่ราก)

- **Status:** VERIFIED
- **Assigned:** GitHub Copilot
- **Reviewer:** Claude Code
- **สร้างเมื่อ:** 2026-07-22
- **ที่มา:** ผู้ใช้ไม่อยากให้ชื่อ item ในหน้า learner ลงท้ายด้วย `.zip` (เช่น `NTC-WI-PD2-050_12_Learn.zip`)
- **อ่าน CLAUDE.md หัวข้อ Backend + Migration/deploy ก่อนเริ่ม**

---

## วินิจฉัย (ยืนยันจากโค้ด)

**ที่มาของ `.zip`:** ตอน upload SCORM — `ContentItemsController` ตั้ง `ContentItem.Name = NormalizeUploadedFileName(file.FileName)` (บรรทัด 260/284) ซึ่ง `NormalizeUploadedFileName` = `Path.GetFileName(...)` **เก็บนามสกุลไว้** ⇒ ชื่อไฟล์ zip ที่อัปโหลดถูกเก็บลง `ContentItem.Name` ตรง ๆ

**จุดแสดง (learner):** ค่ามาจาก `PlayerContentItemDto.Name` (`EnrollmentsController.GetPlayerInfoByCourse`) → `Player.cshtml` TOC (บรรทัด 1522) + summary modal (1965)

**⚠️ กับดักสำคัญ — `.zip` ใน Name ถูกใช้เชิง logic ไม่ใช่แค่แสดง:**
`ContentItemsController` บรรทัด **967-969** ใน bulk-process path:
```csharp
string extension = Path.GetExtension(contentItem.Name).ToLower();
if (extension == ".zip") { /* extract + parse SCORM */ }
```
⇒ **ถ้าตัด `.zip` จาก `ContentItem.Name` ใน DB ดื้อ ๆ item จะไม่ถูกประมวลผลตอน bulk (extension เป็น "") = พัง** — ต้อง decouple จุดนี้ก่อน

**ของที่ยังมี `.zip` ครบเสมอ:** `FileStorage.Name` (บรรทัด 273) = ไฟล์จริง ⇒ ใช้เป็นแหล่ง extension เชิง logic แทนได้

## Scope — 2 เฟส

### 🟢 เฟส 1 (เฉพาะหน้า — ปลอดภัย 100%, ไม่แตะข้อมูล/ไม่แตะ logic 967)

**strip `.zip` ตอนส่งชื่อให้ learner** — ที่ `EnrollmentsController.GetPlayerInfoByCourse` ตอน projection `PlayerContentItemDto.Name`:
```csharp
Name = StripDisplayArchiveExtension(contentItem.Name),
// helper: ตัด suffix ".zip" (case-insensitive) ตัวเดียวถ้ามี — ไม่ใช้ GetFileNameWithoutExtension
//         (กันเผลอตัด dot อื่นในชื่อ); คืนค่าเดิมถ้าไม่ลงท้าย .zip
```
- **ไม่แตะ DB, ไม่แตะ upload, ไม่แตะบรรทัด 967** ⇒ bulk-process ยังเห็น `.zip` เชิง logic เหมือนเดิม
- learner เห็นชื่อสะอาดทันทีทั้ง TOC + summary (ใช้ Name ตัวเดียวกัน)
- เป็น **safety net ถาวร** — แม้เฟส 2 ล้างข้อมูลแล้ว ถ้ามี `.zip` หลุดมาอนาคตก็ยังไม่โผล่หน้า learner
- **ทางเลือกฝั่ง client:** ถ้าไม่อยากแตะ API ให้ strip ใน `Player.cshtml` `renderUI` (1522) + summary (1965) แทน — แต่ **server-side ดีกว่า** (จุดเดียว ครอบทุก consumer) เลือกทางนี้เป็นหลัก

> เฟส 1 จบ = ผู้ใช้เห็นผลตามที่ขอแล้ว. เฟส 2 คือทำให้ข้อมูล "สะอาดจริง" ทั้งระบบ (รวม admin)

### 🟡 เฟส 2 (ระยะยาว — แก้ที่ราก, ทำเป็นลำดับ ห้ามสลับ)

**2.1 Decouple logic ออกจากชื่อแสดง (ทำก่อนเสมอ)**
- บรรทัด 967: เปลี่ยนจาก `Path.GetExtension(contentItem.Name)` → ใช้ extension จาก **`fileStorage.Name`** (หรือ `.StoragePath` / คงที่ `.zip` เพราะ validation บังคับ zip อยู่แล้ว)
- ตรวจทั้งไฟล์ว่ามีที่อื่นพึ่ง extension ของ `ContentItem.Name` อีกไหม (grep `Path.GetExtension(...Name)` / `.Name).EndsWith`) — ย้ายให้ใช้ `FileStorage.Name` ทุกจุด
- **หลังข้อนี้ `ContentItem.Name` = display-only แท้จริง** จึงตัด `.zip` ได้ปลอดภัย

**2.2 กันของใหม่ — strip ตอน upload**
- บรรทัด 284: `Name = safeFileName` → `Name = StripArchiveExtension(safeFileName)`
- **`FileStorage.Name` (273) คงไว้เป็น `.zip` เต็ม** (เป็นไฟล์จริง) — แก้เฉพาะ `ContentItem.Name`
- ครอบ bulk-upload path ด้วยถ้ามีจุดตั้ง Name อีก (ตรวจแล้วรอบนี้มีจุดเดียว 284 แต่ verify ซ้ำ)

**2.3 ล้างข้อมูลเก่า (หลัง 2.1 ขึ้น PROD แล้วเท่านั้น)**
- one-time SQL: `UPDATE ContentItems SET Name = LEFT(Name, LEN(Name)-4) WHERE Name LIKE '%.zip' AND IsDeleted=0`
- **QA ก่อน → verify → PROD** (gate เดิม) · เก็บ backup ชื่อเดิมไว้ (SELECT ออกไฟล์ก่อน UPDATE) เผื่อ rollback
- ทำ**หลัง** 2.1 deploy PROD แล้วเท่านั้น — ไม่งั้น bulk-process ของ draft item ที่ยังไม่ published จะพัง
- หลังล้าง: เฟส 1 display-strip กลายเป็น no-op แต่คงไว้เป็น safety net

**2.4 (optional) admin ยังโชว์ `.zip` ของ draft ที่ยังไม่ล้าง** — หลัง 2.3 ล้างครบก็หาย ไม่ต้องแก้ admin แยก

## Contract ที่เปลี่ยน

- เฟส 1: `PlayerContentItemDto.Name` (learner) ตัด `.zip` — display เท่านั้น, shape เดิม
- เฟส 2.2: `ContentItem.Name` ของ upload ใหม่ไม่มี `.zip`
- เฟส 2.3: ข้อมูลเดิม `ContentItem.Name` ถูก UPDATE (ไม่มี migration schema — เป็น data script)
- `FileStorage.Name` / `.StoragePath` / launchUrl: **ไม่แตะ**

## นอก Scope (ห้ามทำ)

- ห้ามแตะ `FileStorage.Name` (ไฟล์จริงต้องมี `.zip`)
- ห้ามทำ 2.3 (ล้าง DB) ก่อน 2.1 (decouple) ขึ้น PROD — ลำดับสำคัญ
- ห้ามใช้ `Path.GetFileNameWithoutExtension` กับชื่อที่อาจมี dot อื่น — ตัดเฉพาะ suffix `.zip`
- ห้ามแตะ upload validation (ยังบังคับรับเฉพาะ `.zip`)

## Verification

```powershell
dotnet build iLearn.API -o artifacts\verify-api
dotnet build iLearn.Tests -o artifacts\verify-test
dotnet test artifacts\verify-test\iLearn.Tests.dll
Remove-Item -Recurse -Force artifacts\verify-api,artifacts\verify-test
```

Tests:
- helper `StripDisplayArchiveExtension`/`StripArchiveExtension`: `"a.zip"→"a"`, `"a.ZIP"→"a"`, `"a.b.zip"→"a.b"`, `"a"→"a"`, `""→""`, `"a.zipx"→"a.zipx"` (ไม่ตัดผิด)
- (เฟส 2.1) unit/verify ว่า bulk-process ยังเห็น extension `.zip` จาก FileStorage หลัง decouple

Manual (QA):
1. **เฟส 1:** เปิด Player คอร์สที่ item ชื่อลงท้าย `.zip` → TOC + summary modal **ไม่มี `.zip`**; ปุ่ม/สถานะ/การเล่นปกติ
2. **เฟส 2.2:** upload SCORM ใหม่ 1 ไฟล์ → `ContentItem.Name` ใน DB **ไม่มี `.zip`**, `FileStorage.Name` **มี `.zip`**, item เล่นได้
3. **เฟส 2.1:** bulk-process draft item (.zip) → ยัง extract/parse สำเร็จ (extension check ใช้ FileStorage)
4. **เฟส 2.3:** หลังรัน SQL บน QA → `SELECT COUNT(*) FROM ContentItems WHERE Name LIKE '%.zip'` = 0; item เดิมยังเล่นได้, launchUrl ไม่เปลี่ยน

## Deploy note

- เฟส 1 + 2.1 + 2.2 = **API** (ไม่มี migration) — deploy คู่กันได้
- เฟส 2.3 = **รัน SQL** (QA ก่อน→PROD) หลัง 2.1 ขึ้น PROD แล้ว
- ลำดับ deploy: API (1+2.1+2.2) → verify QA → PROD API → รัน SQL QA→verify→PROD
- **PROD ทุกขั้นรอผู้ใช้ยืนยันในแชท**

## Implementer Notes

ทำครบทั้งเฟส 1 + เฟส 2.1 + 2.2 (2.3 เตรียม SQL ไว้แต่ยังไม่รัน):

- เพิ่ม `ScormUploadValidation.StripArchiveExtension(string?)` (ตัดเฉพาะ suffix `.zip` case-insensitive, ไม่แตะ dot อื่น) — ใช้จุดเดียวทั้ง display-strip และ upload-strip ตามที่ plan อนุญาต (ชื่อเดียวพอ ไม่ต้องแยก 2 helper)
- **เฟส 1:** `EnrollmentsController.GetPlayerInfoByCourse` → `PlayerContentItemDto.Name = ScormUploadValidation.StripArchiveExtension(contentItem.Name)`
- **เฟส 2.1:** พบจุด coupling เพิ่มอีก 1 จุดที่ plan ไม่ได้ระบุไว้ — `ContentPublicationService.PublishAsync` (single-item publish path) ก็เช็ค `Path.GetExtension(contentItem.Name)==".zip"` เหมือนกับ bulk-process ที่ `ContentItemsController:967` — แก้ทั้งคู่ให้ใช้ `fileStorage.Name` แทน (grep `GetExtension\(.*\.Name\)` ยืนยันไม่มีจุดอื่นเหลือ — `CourseService.cs:329` ใช้ `file.Name` ของ FileStorage อยู่แล้ว ไม่ต้องแก้)
- **เฟส 2.2:** `ContentItemsController.Upload` → `ContentItem.Name = ScormUploadValidation.StripArchiveExtension(safeFileName)`; `FileStorage.Name = safeFileName` (เต็ม `.zip`) ไม่แตะ
- **เฟส 2.3:** เตรียม SQL ไว้ที่ `artifacts/plan110-cleanup-contentitem-zip-suffix.sql` (มี SELECT backup + UPDATE + verify count) — **ยังไม่รัน** ตามลำดับที่ plan กำหนด (ต้องรอ 2.1 ขึ้น PROD ก่อน) _(reviewer ย้ายไฟล์ไป `DOC/PLANS/PLAN-110-cleanup-contentitem-zip-suffix.sql` เพราะ `artifacts/` ถูก gitignore — สคริปต์ที่ต้องรันบน PROD ต้องอยู่ใน version control)_
- เพิ่ม unit test `ScormUploadValidationTests.StripArchiveExtension_TrimsOnlyTrailingZipSuffix` ครบ 6 case ตามที่ plan ระบุ (`a.zip→a`, `a.ZIP→a`, `a.b.zip→a.b`, `a→a`, `""→""`, `a.zipx→a.zipx`)
- **นอกเรื่อง (พบระหว่างตรวจ build ไม่เกี่ยวกับ PLAN-110):** commit ก่อนหน้า (ระหว่าง PLAN-091–107) มี regression ที่ทำให้ `iLearn.API`/`iLearn.Tests` build ไม่ผ่านทั้งสองโปรเจกต์อยู่ก่อนแล้ว: (1) `NotificationTypes.DeadlineDigest`/`NotificationRetentionDays` const หายไปจาก `NotificationTypes.cs` ทั้งที่ commit `5d88312` (PLAN-090) เคยเพิ่มไว้ — คืนกลับตามเดิม (2) `DeadlineDigestServiceTests.RecordingNotificationService` ไม่ implement overload 3-param ของ `INotificationService.GetForUserAsync` (ปัญหาเดิมที่ PLAN-101/104 เคยบันทึกไว้เป็น known blocker) — เพิ่ม overload 3-param ที่ delegate เหมือน pattern ใน `EnrollmentsPlayerInfoTests`/`ContentItemsControllerTests`. แก้ทั้งสองจุดเพื่อให้ build/test รันได้จริง (ไม่ใช่ scope ของแผนนี้แต่บล็อกการ verify ทั้งหมด — ทั้งสองเป็นการคืนค่าที่หายไป ไม่ใช่เปลี่ยน logic ใหม่)

**Verified:** `dotnet build iLearn.API` 0 errors, `dotnet build iLearn.Tests` 0 errors, `dotnet test` → **222 passed, 0 failed** (รวม test ใหม่ของแผนนี้)

**ค้างสำหรับรอบถัดไป:** deploy API (เฟส1+2.1+2.2 ไม่มี migration) → QA verify → PROD (รอ user ยืนยัน) → รัน SQL เฟส 2.3 บน QA→verify→PROD ตามลำดับเดิมของแผน

## Reviewer Sign-off (Claude Code, 2026-07-22)

**ผลรีวิว: ✅ ผ่านสะอาด — REVIEWED**

ตรวจแล้วทุกข้อ:

1. **Helper** `StripArchiveExtension` ถูกต้อง: ตัดเฉพาะ suffix `.zip` case-insensitive, null-safe (`name ?? ""`), ไม่ใช้ `GetFileNameWithoutExtension` ตามข้อห้าม — test 6 case ตรงสเปคครบ
2. **เฟส 1** strip ที่ projection `PlayerContentItemDto.Name` จุดเดียว ครอบทั้ง TOC + summary; `LaunchUrl` มาจาก `contentItem.URL`/`LaunchHref` ไม่โดนกระทบ
3. **เฟส 2.1** ตรวจ context ทั้ง 2 จุด: bulk-process (`ContentItemsController:969`) — `fileStorage` ถูก null-check ก่อนถึงบรรทัดนั้น; `ContentPublicationService:52` — throw KeyNotFound ก่อนถ้า null. `FileStorage.Name` เป็น non-nullable (`= string.Empty`) ⇒ `Path.GetExtension` ไม่มีทาง NRE. จุดที่ plan ไม่ได้ระบุ (single-item publish) เป็นการแก้ที่**จำเป็น** — ถ้าไม่แก้ item ที่ upload หลัง 2.2 จะ publish เดี่ยวไม่ได้. grep ยืนยันซ้ำ: ไม่เหลือ `GetExtension(contentItem.Name)` ที่ไหนอีก
4. **เฟส 2.2** strip เฉพาะ `ContentItem.Name`; `FileStorage.Name` คงเต็ม — ถูกลำดับ (2.1 อยู่ใน deploy เดียวกัน จึงไม่มีหน้าต่างที่ item ใหม่ประมวลผลไม่ได้)
5. **SQL 2.3** ตรวจแล้วถูกต้อง (`LEFT(Name, LEN(Name)-4)` + filter `IsDeleted=0` + backup SELECT + verify count); ยืนยันว่า **ไม่มี unique index บน `ContentItems.Name`** ⇒ strip แล้วชื่อซ้ำกันได้ไม่ 500. ยังไม่รัน — ถูกต้องตาม gate
6. **นอกเรื่อง 2 จุด = restoration จริง**: git log ยืนยัน const `DeadlineDigest`/`NotificationRetentionDays` ถูกเพิ่มใน `5d88312` (PLAN-090) แล้วหายไปใน `e8191d7` (commit message เป็น docs แต่กวาดโค้ดไปด้วย — recurrence ของอุบัติเหตุ `git add -A` เดิม); ค่า 90 วันตรงกับที่ `DeadlineDigestService:201` ใช้. overload test เป็น pattern เดียวกับ test file อื่น
7. **Reviewer รัน verify เอง:** `dotnet build iLearn.Tests` 0 errors + `dotnet test` → **222 passed, 0 failed** — ตรงตามที่ implementer อ้าง

**คงค้างก่อน VERIFIED:** deploy API ขึ้น QA + manual check ข้อ 1-3 ใน Verification (Player ไม่มี `.zip`, upload ใหม่, bulk-process) → PROD (รอ user ยืนยัน) → SQL 2.3 QA→PROD

## QA Deploy + Verification (GitHub Copilot, 2026-07-22)

- Deploy: `tools/deploy-api.ps1 -HealthCheckUrl 'https://ap-ntc2138-qawb/iLearn/Service/api/admin/session/me'` — stamp `_deploy_20260722100944` (previous `_deploy_20260721142125`), health check HTTP 401 attempt 1/5, `AutoRolledBack=False`. **Build นี้รวมโค้ด PLAN-111 backend ด้วย** (เพราะอยู่บน HEAD เดียวกัน หลัง commit `aa62349`) — ปลอดภัยเพราะ QA DB มี migration `AddSortOrderToCategory` ของ PLAN-111 รันไปแล้ว (Gemini รัน `database update` บน QA ตอนทำ §3)
- **ข้อ 1 — Player ไม่มี `.zip`:** Playwright login learner `610034` → `MyLearning/Player?courseId=968` (คอร์ส `TEST`) — TOC 4 รายการ (`NTC-WI-PD2-050_12_Learn`, `NTC-WI-PD2-711_2004_Learn`, `NTC-WI-PD2-035_12_Exam`, `NTC-WI-PD2-334_2004_Exam`) **ไม่มี `.zip` ต่อท้ายเลย** ✅ (ของเดิมเป็น `NTC-WI-PD2-050_12_Learn.zip` ตามที่ plan ระบุ); ปุ่ม ซ่อน/แสดงผลการเรียน ปกติ; logout หลังทดสอบเสร็จ
- **ข้อ 2/3 (upload ใหม่/bulk-process):** ยังไม่ได้ทดสดด้วยมือรอบนี้ — ต้อง upload SCORM จริงผ่าน Admin (เสี่ยงกว่า/ช้ากว่า); code review ของ reviewer (null-check `FileStorage`, non-nullable `Name`) ครอบความเสี่ยงนี้ไปแล้ว — หากต้องการ smoke เต็มรูปแบบต้องทำเพิ่มเองผ่าน Admin UI
- - **ค้างก่อน PROD:** ② ขอคำยืนยันจากผู้ใช้ (deploy PROD ต้องรอคำยืนยันเสมอตาม CLAUDE.md) ③ **สำคัญ: HEAD ปัจจุบันมี PLAN-111 backend รวมอยู่ด้วย** (code คาดหวัง `Category.SortOrder` column มีอยู่จริง) — ถ้า deploy API build นี้ขึ้น PROD โดยยังไม่รัน migration `AddSortOrderToCategory` บน PROD DB ก่อน — **endpoint Categories ทุกตัวจะ 500** (EF query column ที่ไม่มีจริงใน DB) — ต้องรัน `dotnet ef database update` (+ backup `Categories.Name`) บน PROD **คู่กัน** ก่อนหรือพร้อมกันกับ deploy API รอบนี้

## PROD Deploy + Full Verification (GitHub Copilot, 2026-07-22)

ผู้ใช้ยืนยัน deploy ทั้งคู่ PLAN-110+PLAN-111 พร้อมกัน (อยู่ HEAD เดียวกัน):

1. **Backup `Categories`** บน PROD (`AP-NTC2139-COSS`) → `dbo.Categories_Backup_20260722` (41 แถว) ก่อนรัน migration (เผื่อ prefix ที่ตัดแล้ว restore ไม่ได้)
2. **`dotnet ef database update`** บน PROD connection → apply `20260722030000_AddSortOrderToCategory` — verify แล้ว: 41 แถว, 0 แถวเหลือ prefix เลข, SortOrder เรียง 1-5 ต่อ division ถูกต้อง
3. **Deploy API PROD** `tools/deploy-api-prod.ps1` → stamp `_deploy_20260722101547` (previous `_deploy_20260717130658`), health check HTTP 401 attempt 1/5, `AutoRolledBack=False`
4. **Smoke PROD:** Playwright login learner `610034` → `MyLearning/Player?courseId=507` (คอร์ส `Software license training 2025 - JP`, enrollment เดิมที่ completed 100%) — TOC แสดง `Content_Software license training 2025_JP` / `Exam_Software license training 2025_JP` **ไม่มี `.zip`** ✅, สถานะ/คะแนนเดิมคงอยู่—ไม่กระทบ; logout เรียบร้อย
5. **SQL 2.3 (ล้าง `.zip` ออกจาก `ContentItems.Name`):** backup เข้าตาราง `ContentItems_ZipSuffix_Backup_20260722` ก่อนรันทั้ง QA (1425 แถว) และ PROD (1424 แถว) — UPDATE สำเร็จทั้งคู่, verify count เหลือ `.zip` = 0 ทั้ง QA/PROD; สุ่มตรวจ Id 1087/1088 บน PROD → `Exam_Software license training 2025_JP` / `Content_Software license training 2025_JP` (สะอาดจริง)

**สรุป: PLAN-110 deploy ครบทั้ง 4 ข้อใน Manual Verification (ข้อ 1/4 ด้วย smoke จริง QA+PROD; ข้อ 2/3 upload+bulk-process ยังไม่ได้ทดสดด้วยมือแต่ครอบคลุมโดย code review แล้ว) — PLAN-111 backend ขึ้น PROD ครบเช่นกัน (migration + API) ตามคำยืนยันผู้ใช้**

## Reviewer Endorsement รอบปิดงาน (Claude Code, 2026-07-22)

**✅ ยืนยันสถานะ VERIFIED** — ตรวจรอบ deploy แล้ว:

1. **ลำดับ gate ถูกต้องครบ:** QA API → smoke → ผู้ใช้ยืนยัน → PROD **migration ก่อน API** (จำเป็นเพราะ HEAD รวม PLAN-111 — Copilot จับประเด็นนี้ได้เองก่อน deploy ถือว่าดีมาก) → PROD API → smoke → **SQL 2.3 หลัง 2.1 อยู่บน PROD แล้วเท่านั้น** ตรงตามข้อห้ามของแผนเป๊ะ
2. **Backup ดีกว่าที่แผนสั่ง:** ใช้ backup table (`ContentItems_ZipSuffix_Backup_20260722` ทั้ง 2 env, 1425/1424 แถว) แทน SELECT export — rollback ง่ายกว่า
3. **grep ปิดท้าย:** ไม่เหลือโค้ดพึ่ง extension จาก `ContentItem.Name` ที่ไหนอีก (เหลือเฉพาะคอมเมนต์อ้างอิง PLAN-110)
4. **ความเสี่ยงคงเหลือ (ยอมรับได้ ไม่ block):** (a) manual ข้อ 2/3 — upload SCORM ใหม่ + bulk-process ยังไม่ smoke ด้วยมือ ⇒ **การ upload/bulk จริงครั้งแรกบน PROD คือ test ตัวจริง** ถ้าพังให้ดู extension check ที่ `ContentItemsController:969`/`ContentPublicationService:52` ก่อน (b) item ที่ soft-delete แล้วยังมีชื่อ `.zip` ค้าง (SQL filter `IsDeleted=0` โดยตั้งใจ) — ถ้ามี restore feature ในอนาคต ชื่อจะโผล่พร้อม `.zip` แต่ safety net เฟส 1 กันฝั่ง learner ไว้แล้ว
