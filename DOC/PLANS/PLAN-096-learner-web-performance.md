# PLAN-096: Learner web performance — compression + cache + ตัด asset ที่ไม่ใช้ (iPad readiness ชุด A)

- **Status:** QA DEPLOYED (code + QA HTTPS header verification ผ่าน — เหลือ iPad smoke ก่อน VERIFIED)
- **Assigned:** GitHub Copilot
- **Reviewer:** Claude Code
- **สร้างเมื่อ:** 2026-07-17
- **ที่มา:** ผู้ใช้จะ rollout learner app บน iPad — รีวิวความพร้อมพบว่า "ไม่ลื่น" มาจาก payload หนัก + server ไม่บีบอัด/ไม่ cache (หลักฐานวัดจริงด้านล่าง)
- **คู่ขนานกับ:** [PLAN-097](PLAN-097-player-ipad-ux.md) (Gemini) — **ไฟล์ไม่ชนกัน:** `_DevExtremeLayout.cshtml` + `Program.cs` เป็นของแผนนี้เท่านั้น / Player.cshtml, MyLearning/Index.cshtml, Home/Index.cshtml, MyLearningController เป็นของ 097 — **ห้ามแตะไฟล์ของอีกแผน**

---

## บริบท (ยืนยันจากโค้ด + วัดจาก QA จริงแล้ว)

- curl ไป QA (`ap-ntc2138-qawb.nikonoa.net`) พร้อม `Accept-Encoding: gzip, br` → **ไม่มี `Content-Encoding` และไม่มี `Cache-Control`** (มีแค่ ETag): `dx.all.js` ส่งเต็ม **5,259,968 bytes ทุกครั้ง** — ยิงซ้ำ 3 ครั้งก็ไม่บีบ ⇒ IIS static compression ไม่ทำงานเพราะ host แบบ in-process ผ่าน ASP.NET Core module (response ของแอปเป็น "dynamic" ในสายตา IIS) — ต้องแก้ในแอป
- Layout ([`_DevExtremeLayout.cshtml`](../../iLearn.User/Views/Shared/_DevExtremeLayout.cshtml)) โหลด JS/CSS ใน `<head>` แบบ blocking รวม ~9.3MB ต่อ cold load ซึ่งมีของที่ **learner ไม่ได้ใช้เลย ~2.9MB**:
  - `font-awesome/js/all.min.js` 1.5MB — โหลด**ซ้ำ**กับ `all.min.css` (76KB) ที่โหลดอยู่แล้ว; ตัว JS ทำ SVG replacement ผ่าน MutationObserver ทั้ง DOM ⇒ ใน Player ทุกครั้งที่ SCORM SetValue → recalc → เปลี่ยน icon → FA สแกนซ้ำ = jank ต่อเนื่องบน iPad
  - `dx-exceljs-fork.min.js` 1.1MB + `filesaver.min.js` + `dx.aspnet.mvc.js` + `dx.aspnet.data.js` — ใช้โดย `handleExporting`/`createDataStore` ใน layout ซึ่ง**ไม่มี view ไหนเรียกเลย** (grep ยืนยันแล้ว: ทั้งสองฟังก์ชัน + `API_BASE` มี match เฉพาะบรรทัด define ใน layout; `serviceUrl` มีอ้างที่ Player.cshtml:697 เป็น `const baseUrl` ที่ประกาศแล้ว**ไม่ถูกใช้ต่อ** — อ่านทั้งไฟล์ยืนยันแล้ว)
  - `jquery.js` เป็นตัว **unminified** 285KB ทั้งที่ `jquery.min.js` (~87KB) อยู่โฟลเดอร์เดียวกัน
  - `dx.all.js` 5.26MB เป็น minified อยู่แล้ว (DevExtreme 25.2 full bundle) — learner ใช้แค่ dxTextBox/dxButton/dialog/notify แต่การเลิกใช้เป็นงานใหญ่ **นอก scope รอบนี้** ให้ compression + cache จัดการไปก่อน
- `API_BASE` ฝัง internal short host (`https://ap-ntc2138-qawb/...`) — dead code ที่เป็นกับดักชนิดเดียวกับ incident PLAN-094 (iframe ได้ internal host) ต้องเก็บกวาดทิ้ง
- Toast ของ learner แสดง `bottom center` — บน iPad โดนคีย์บอร์ดบังโดยเฉพาะ error ตอน login (ผู้ใช้ไม่รู้ว่า login พลาด) — ย้ายมาแผนนี้เพราะอยู่ในไฟล์ layout
- `<html lang="en">` ทั้งที่ UI เป็นไทย — Safari ชอบเด้ง translate bar

## Scope

### 1. Response compression (`iLearn.User/Program.cs`)

```csharp
using Microsoft.AspNetCore.ResponseCompression;
using System.IO.Compression;

builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
    options.Providers.Add<BrotliCompressionProvider>();
    options.Providers.Add<GzipCompressionProvider>();
});
builder.Services.Configure<BrotliCompressionProviderOptions>(o => o.Level = CompressionLevel.Fastest);
builder.Services.Configure<GzipCompressionProviderOptions>(o => o.Level = CompressionLevel.Fastest);
```

- `app.UseResponseCompression()` วาง**ก่อน** `UseStaticFiles()` และ `UseCourseStaticFiles()` (ให้ครอบทั้ง wwwroot + SCORM content)
- ใช้ MIME list default ของ middleware (html/css/js/json/svg ครอบแล้ว — font/รูป/วิดีโอไม่บีบซึ่งถูกต้อง)
- ระดับ `Fastest` พอ — ไฟล์ใหญ่ถูกขอไม่บ่อยเมื่อมี Cache-Control ตาม §2 (middleware บีบต่อ request ไม่มี cache ฝั่ง server)

### 2. Cache-Control

- **wwwroot** (`UseStaticFiles`): เพิ่ม `OnPrepareResponse` ตั้ง `Cache-Control: public,max-age=604800` (7 วัน) — ปลอดภัยเพราะทุก reference ใน layout ใช้ `asp-append-version`/`FileVersionProvider` (fingerprint) อยู่แล้ว; ข้อยกเว้นเดียวคือ FA webfonts ที่ถูกอ้างจากใน CSS โดยไม่มี version param — ยอมรับ staleness สูงสุด 7 วันตอน upgrade FA (นาน ๆ ครั้ง)
- **SCORM content** (`UseCourseStaticFiles` — มี `OnPrepareResponse` เดิมอยู่แล้ว): เพิ่ม `Cache-Control: public,max-age=3600` (1 ชม.) คงไว้ซึ่ง `X-Content-Type-Options: nosniff` เดิม — เนื้อหาอยู่ใต้โฟลเดอร์ต่อ content GUID การ re-upload ปกติได้ GUID ใหม่; ถ้า implement แล้วพบว่ามีเคส replace ไฟล์ใน GUID เดิม ให้จดใน Implementer Notes (1 ชม. ยังยอมรับได้ ไม่ต้องเปลี่ยนแผน)

### 3. ตัด asset ใน `_DevExtremeLayout.cshtml`

- **ลบ** `<script>`: `dx-exceljs-fork.min.js`, `filesaver.min.js`, `dx.aspnet.mvc.js`, `dx.aspnet.data.js`, `lib/font-awesome/js/all.min.js`
- **คงไว้:** `lib/font-awesome/css/all.min.css` (icons ทั้งแอปพึ่ง webfont ตัวนี้ — **ห้ามลบโฟลเดอร์ `webfonts/`**), `dx.all.js`, `dx.light.css`, bootstrap
- **สลับ** `~/js/devextreme/jquery.js` → `~/js/devextreme/jquery.min.js`
- **ลบ inline JS ที่ตายแล้ว:** `const API_BASE`, `const serviceUrl`, `createDataStore()`, `handleExporting()` ทั้งก้อน
- **แก้ Player.cshtml 1 บรรทัดเท่านั้น** (ข้อยกเว้นที่ตกลงกับ 097 แล้ว): ลบ `const baseUrl = (typeof serviceUrl !== 'undefined') ? serviceUrl : '';` (บรรทัด ~697 — ตัวแปรไม่ถูกใช้) — ห้ามแตะส่วนอื่นของไฟล์นี้

### 4. Toast ขึ้นบน + lang (ไฟล์ layout เดียวกัน)

- `showToast`: เปลี่ยน position จาก `{ position: 'bottom center', direction: 'up-push' }` → `{ position: 'top center', direction: 'down-push' }` (กันคีย์บอร์ด iPad บัง)
- `<html lang="en">` → `<html lang="th">`

## Contract ที่เปลี่ยน

- API shape / DB: **ไม่มี**
- HTTP headers ของ learner app เปลี่ยน: เพิ่ม `Content-Encoding` (br/gzip) + `Cache-Control` — ผลคือ browser จะ cache asset นาน ⇒ **การ deploy รอบถัด ๆ ไปต้องพึ่ง `asp-append-version` เหมือนเดิม** (มีอยู่แล้ว ไม่ต้องทำอะไรเพิ่ม)
- Global JS ที่หายไปจากทุกหน้า: `API_BASE`, `serviceUrl`, `createDataStore`, `handleExporting`, `ExcelJS`, `saveAs`, Font Awesome JS API — ยืนยันแล้วว่าไม่มี view ใช้

## นอก Scope (ห้ามทำ)

- ห้ามเปลี่ยน/ตัด `dx.all.js`, `dx.light.css` (งานอนาคต)
- ห้ามแตะ `Player.cshtml` นอกจากบรรทัดเดียวใน §3, ห้ามแตะ `MyLearning/Index.cshtml`, `Home/Index.cshtml`, `MyLearningController` (ของ PLAN-097)
- ห้ามแตะ `appsettings*.json`, iLearn.API, iLearn.Admin*
- ห้ามเพิ่ม `defer`/ย้ายตำแหน่ง script tag ที่เหลือ (inline script ใน view พึ่ง `$`/`DevExpress` ตอน parse — จะพังทั้งแอป)

## Verification

```powershell
# build (กัน bin ล็อกจาก VS)
dotnet build iLearn.User -o artifacts\verify-user
Remove-Item -Recurse -Force artifacts\verify-user
dotnet build iLearn.Tests -o artifacts\verify-test
dotnet test artifacts\verify-test\iLearn.Tests.dll
Remove-Item -Recurse -Force artifacts\verify-test
```

รันแอป local แล้ว curl (คาดหวังทั้ง `Content-Encoding` และ `Cache-Control`):

```powershell
curl.exe -sk -H "Accept-Encoding: gzip, br" -D - -o NUL https://localhost:<port>/js/devextreme/dx.all.js
```

Browser smoke (desktop ก่อน): Login → Dashboard → Player 1 คอร์ส

1. Icons Font Awesome แสดงครบ (ปุ่ม Play, check-circle ใน TOC, navbar) — พิสูจน์ว่า webfont CSS พอ
2. Console: **0 error** — โดยเฉพาะห้ามมี `DevExpress.data`, `ExcelJS`, `saveAs`, `API_BASE` undefined
3. Login ผิด 1 ครั้ง → toast โผล่**ด้านบน**
4. SCORM เล่น + commit ได้ปกติ (Network เห็น `CommitRuntime` 200)

Deploy QA (ตาม `DOC/DEPLOY-CHECKLIST.md` — learner deploy script เดิมของ PLAN-095) แล้ว curl ซ้ำกับ `https://ap-ntc2138-qawb.nikonoa.net/iLearn/js/devextreme/dx.all.js` — เป้า: transfer จาก 5.26MB → **≤ ~1.6MB (br)** + reload หน้าเดิมครั้งที่ 2 ไม่มี request 200 ซ้ำของ asset (มาจาก memory/disk cache)
**PROD ต้องรอผู้ใช้ยืนยันผล QA บน iPad ในแชทก่อน** (gate เดียวกับ PLAN-093)

## Implementer Notes

- เพิ่ม Brotli/Gzip response compression ที่ระดับ `Fastest` และวาง middleware ก่อนทั้ง wwwroot และ SCORM static files
- เพิ่ม `Cache-Control: public,max-age=604800` สำหรับ wwwroot และ `public,max-age=3600` สำหรับ SCORM โดยคง `X-Content-Type-Options: nosniff`
- ตัด ExcelJS, FileSaver, DevExtreme ASP.NET adapters, Font Awesome JavaScript และ global helper ที่ไม่มีการใช้; เปลี่ยน jQuery เป็น minified build
- ลบ dead `baseUrl` ตามข้อยกเว้นที่ตกลงไว้จาก `Views/MyLearning/Player.cshtml` (path จริงของ Player)
- เปลี่ยน HTML language เป็น Thai และย้าย learner toast ไปด้านบน
- Verified: `dotnet build iLearn.User -o artifacts\verify-user` ผ่าน 0 errors (warnings เดิม 71 รายการ); `dotnet build iLearn.Tests -o artifacts\verify-test` ผ่าน 0 errors (warnings เดิม 90 รายการ); `dotnet test artifacts\verify-test\iLearn.Tests.dll` ผ่าน 203/203. ยังต้อง browser/local-header และ QA iPad smoke ตาม checklist

## Reviewer Sign-off (Claude Code, 2026-07-17)

ตรวจ diff เต็มทั้ง 3 ไฟล์ + verify อิสระ (build/test + **รัน runtime จริงจาก publish บน localhost** ซึ่ง implementer ยังไม่ได้ทำ):

- **Diff ตรงสเปคทุกจุด ไม่มีนอก scope:** Program.cs (AddResponseCompression + EnableForHttps + Brotli/Gzip Fastest, `UseResponseCompression` ก่อน static ทั้งสองตัว, Cache-Control 7d/1h, nosniff คงเดิม) ✅ layout (ตัด 5 scripts, jquery.min, ลบ API_BASE/serviceUrl/createDataStore/handleExporting, toast top center + down-push, lang=th, ไม่มี defer) ✅ Player.cshtml ลบเฉพาะบรรทัด `baseUrl` ตามข้อยกเว้น ✅ (diff ตรวจก่อน PLAN-097 จะ land — สภาพ 096 ล้วน; appsettings diffs ใน tree เป็นของ PLAN-094/095 เดิม)
- **Test อิสระ:** `dotnet test` **203/203 passed**
- **Runtime verify (publish → รัน → curl + browser):** `dx.all.js` ตอบ `Content-Encoding: br` + `Cache-Control: public,max-age=604800` + `Vary` → **5.26MB → 1,976,822 bytes (-62%)**; jquery.min 38KB br; HTML root บีบ br; served HTML เหลือ script 5 ตัว + `lang="th"` + grep ซาก API_BASE/exceljs/FA JS/aspnet = 0; toast วัดจาก DOM จริง `top=10px` (TOP ✓); icon login render ผ่าน webfont "Font Awesome 7 Free" + `window.FontAwesome` absent + `fa-solid-900.woff2` 200; console 0 error
- **Observation (ไม่บล็อก):** br ที่ `Fastest` ได้ 1.98MB สูงกว่าเป้าประมาณการ ~1.6MB เล็กน้อย — ถูกต้องแล้วที่ไม่ใช้ Optimal (middleware บีบต่อ request ไม่มี cache ฝั่ง server, Optimal บนไฟล์ 5MB จะกิน CPU ทุก cache-miss); เป้าใน Verification ถือว่าบรรลุในเจตนา
- **คงค้างก่อน VERIFIED:** (1) deploy QA แล้ว curl ยืนยัน br บน **HTTPS จริง** (local ทดสอบได้แค่ HTTP — `EnableForHttps` ยืนยันจากโค้ด) (2) SCORM `Cache-Control: max-age=3600` ตรวจ local ไม่ได้ (เครื่อง dev ไม่มี `D:\iLearnContent` — middleware skip) ต้องดูบน QA (3) iPad smoke ตามแผน — ทั้งหมดควรทำหลัง commit ชุดนี้ (precedent PLAN-093: deploy จาก tree ที่ commit แล้วเท่านั้น)

**สรุป: ผ่านรีวิว ไม่มี finding ต้องแก้ — รอ commit + QA rollout**

## QA Deployment (2026-07-17)

- Commit: `34573b4` (`feat(learner): improve iPad performance and player UX (PLAN-096/097)`)
- Deploy: QA stamp `20260717164531`; `web.config` now runs `.\_user_deploy_20260717164531\iLearn.User.dll`; public health check returned HTTP 200 and no rollback occurred.
- HTTPS header verification: `GET /iLearn/js/devextreme/dx.all.js` with `Accept-Encoding: gzip, br` returned `Content-Encoding: br`, `Cache-Control: public,max-age=604800`, and `Vary: Accept-Encoding`.
- Remaining gate: verify a real SCORM content asset returns `Cache-Control: public,max-age=3600`, then complete the shared iPad smoke tests before marking VERIFIED or considering PROD.
