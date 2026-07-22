# PLAN-110: เอา `.zip` ออกจากชื่อ content item — เฟสเฉพาะหน้า (ปลอดภัย) + เฟสระยะยาว (แก้ที่ราก)

- **Status:** READY
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

_(เติมโดย implementer)_
