# PLAN-108: ตัด DevExtreme ออกจาก learner (แก้อาการหน้าค้าง ~0.5 วิ บน iPad) + feedback ตอนกด

- **Status:** READY
- **Assigned:** GitHub Copilot
- **Reviewer:** Claude Code
- **สร้างเมื่อ:** 2026-07-21
- **ที่มา:** ผู้ใช้ทดสอบบน iPad จริง — ทุกอย่างทำงานดี **ยกเว้น**กด `ดูเนื้อหา`/`เริ่มเรียน` แล้วหน้า**นิ่งไม่ตอบสนอง ~ครึ่งวินาที** จนรู้สึกว่าระบบช้า
- **🔒 ข้อจำกัดจากผู้ใช้ (สำคัญที่สุด): หน้าตา UI ต้องเหมือนเดิมทุกจุด** — งานนี้คือเปลี่ยนเครื่องยนต์ ไม่ใช่เปลี่ยนดีไซน์

---

## วินิจฉัย (ยืนยันจากโค้ดแล้ว)

**ปุ่มที่กดไม่มี JS handler เลย** — เป็น `<a href>` ธรรมดา:
```html
<a href=".../Player?courseId=${course.id}" class="btn btn-outline-primary btn-continue">ดูเนื้อหา</a>
```
⇒ ครึ่งวินาทีที่นิ่ง = **ต้นทุน parse/execute ของหน้าที่กำลังโหลด** ไม่ใช่โค้ดตอนกด

**ทั้ง learner app ใช้ DevExtreme แค่ 4 อย่าง** (grep ทั้ง `Views/`):

| ใช้ | จำนวน | ที่ไหน |
| --- | --- | --- |
| `dxTextBox` | 3 | ช่องรหัสพนักงาน (login), ช่องค้นหาหลักสูตร |
| `dxButton` | 2 | ปุ่มเข้าสู่ระบบ |
| `DevExpress.ui.notify` | 2 | `showToast` ใน layout |
| `DevExpress.ui.dialog.custom` | 1 | ยืนยันออกจากระบบ |

แลกกับการโหลด **`dx.all.js` 5.1 MB + `dx.light.css` 676 KB ทุกหน้า**

**สำคัญ:** PLAN-096 เปิด brotli แล้ว (ส่งจริง ~2MB) แต่ **การบีบอัดไม่ลดเวลา parse/execute** — เบราว์เซอร์ยังต้องแตกและ parse JS 5.1 MB ทุกครั้งที่เปลี่ยนหน้า = ~300-600ms บน CPU ของ iPad ตรงกับอาการพอดี

**เป้าหมายเชิงตัวเลข:** JS ที่ต้อง parse ต่อหน้า **~5.3 MB → ~170 KB** (jquery.min 88K + bootstrap.bundle 80K + site.js 1K) = ลด ~97%

## บริบทที่ต้องรู้ก่อนแตะ (ผลจากการสำรวจ)

- **`.dx-toast-content-custom` ไม่มี CSS ของเราเลยสักบรรทัด** — หน้าตา toast (สีตาม type, ขนาด, มุมโค้ง, เงา) มาจาก `dx.light.css` ล้วน ⇒ **ถ้าไม่เก็บ baseline ก่อน จะทำให้เหมือนเดิมไม่ได้**
- `user-theme.css` มีบล็อก `DEVEXTREME OVERRIDES` ยาว **~287 บรรทัด** ที่จะกลายเป็น dead code
- ของที่โหลดอยู่แล้วและใช้แทนได้: **Bootstrap 5 bundle (80K — มี Toast + Modal)**, jQuery, Font Awesome (webfont)
- `.btn-login` มี CSS ของเราเองอยู่แล้ว (สีแบรนด์ + hover + height 50) ⇒ ปุ่ม login แทบไม่ต้องทำอะไร
- `.logout-dialog-icon/-title/-text` มี CSS ของเราอยู่แล้ว ⇒ เนื้อใน dialog ยกมาได้เลย เหลือแค่กรอบ modal

## Scope

### §0 (บังคับ ทำก่อนแตะโค้ดใด ๆ) — เก็บ baseline หน้าตาปัจจุบัน

ถ้าไม่มี baseline จะพิสูจน์ไม่ได้ว่า "เหมือนเดิม" — เก็บบน QA ปัจจุบัน (ยังมี DevExtreme):

1. **Screenshot** ที่ viewport เดียวกัน (desktop 1280 + iPad 1024): หน้า login, dashboard (ช่องค้นหา), toast **ครบทั้ง 4 type** (success/error/warning/info), dialog ออกจากระบบ
2. **Computed style** ของ element จริง — อย่างน้อย: `background-color`, `color`, `border-radius`, `box-shadow`, `font-size`, `height`, `min-width`, `padding` ของ `.dx-toast` (ทุก type), ช่อง input (ปกติ + focus), ปุ่ม login, กรอบ dialog + ปุ่มใน dialog
3. เก็บไว้ใน Implementer Notes — **reviewer จะใช้เทียบ**

### §1 (ระดับ 2 — แก้ต้นเหตุ) ตัด DevExtreme ออกทั้งหมด

**1a. ช่องรหัสพนักงาน (`Home/Index.cshtml`)** — `dxTextBox` floating label, outlined, height 50
→ `<input class="form-control">` + Bootstrap `.form-floating` (หรือ label ของเราเอง ถ้า floating ของ Bootstrap หน้าตาไม่ตรง)
- **ต้องคง `inputAttr` ทั้ง 6 ตัวของ PLAN-097** (`inputmode=numeric`, autocomplete/autocorrect/autocapitalize=off, spellcheck=false, `enterkeyhint=go`) — ตัวนี้แก้ปัญหาคีย์บอร์ด iPad ห้ามหาย
- คง `onEnterKey` → กด Enter แล้ว login (มี handler `keyup.login` อยู่แล้วด้วย ระวังยิงซ้ำ)
- focus ring: `user-theme.css` มี `.form-control:focus { border-color: var(--brand-color); box-shadow: ... }` อยู่แล้ว ⇒ ได้สีเดิม

**1b. ปุ่มเข้าสู่ระบบ (`Home/Index.cshtml`)** — `dxButton` + `elementAttr: {class:'btn-login'}`
→ `<button type="button" class="btn btn-login w-100">เข้าสู่ระบบ</button>` (CSS `.btn-login` เดิมมีครบ)
- คงพฤติกรรม disabled + เปลี่ยนข้อความเป็น "กำลังตรวจสอบ..." ตอน submit (โค้ดเดิมใช้ `loginButton.option(...)` ⇒ เปลี่ยนเป็น `prop('disabled')` + `.text()`)

**1c. ช่องค้นหาหลักสูตร (`MyLearning/Index.cshtml`)** — `dxTextBox` `mode:"search"`, width 300
→ input-group + ไอคอนแว่นขยาย Font Awesome ให้หน้าตาตรงของเดิม (ดู baseline §0)
- **คง debounce 250ms + `filterCatalog` เดิม** (งานของ PLAN-097 C5)

**1d. `showToast` (`_DevExtremeLayout.cshtml`) — จุดเสี่ยงสูงสุด**
`DevExpress.ui.notify` → toast ของเราเอง (หรือ Bootstrap Toast) โดย**ต้องตรงกับ baseline §0**:
- position **top center**, stack ลงล่าง (`down-push` = toast ใหม่ดันตัวเก่าลง)
- คงพารามิเตอร์เดิม: `displayTime` default 3500ms, height ~45px, min-width 220px, width auto
- fade in ~400ms / out ~40ms
- ไอคอน Font Awesome ตาม type เดิม (`fa-circle-check` / `fa-circle-xmark` / `fa-triangle-exclamation` / `fa-circle-info`)
- **สีพื้นหลังต่อ type ต้องเท่าของเดิม** (เก็บจาก §0 — success ถูก override เป็น `var(--brand-color)` ใน user-theme.css)
- **signature เดิมห้ามเปลี่ยน:** `showToast(message, type, duration)` — ถูกเรียกจากหลายไฟล์ (login error, session expired, cross-origin warning ของ 103, commit warning ของ 106)
- ⚠️ **ถ้า toast พัง = error ทั้งระบบเงียบหมด** (session หมดอายุ/login ผิด ผู้ใช้จะไม่เห็นอะไรเลย) — ทดสอบทุก type

**1e. dialog ออกจากระบบ (`MyLearning/Index.cshtml`)** — `DevExpress.ui.dialog.custom`
→ Bootstrap Modal (โหลดอยู่แล้ว) ใส่ `messageHtml` เดิมทั้งก้อน (คลาส `logout-dialog-*` มี CSS อยู่แล้ว)
- ปุ่ม: `ยกเลิก` (outlined) + `ออกจากระบบ` (danger, มีไอคอน) — เรียง/หน้าตาตาม baseline
- กด `ออกจากระบบ` → `window.location.href = logoutUrl` เหมือนเดิม

**1f. ตัด asset ออกจาก `_DevExtremeLayout.cshtml`**
ลบ `dx.all.js`, `dx.light.css`, `src/devextreme-license.js`
- **คงไว้:** jquery.min, bootstrap.bundle, Font Awesome CSS, Sarabun, `user-theme.css`, `site.js`
- **ก่อนลบ CSS: grep หา class `dx-` ที่เหลือใน markup/JS ทุกไฟล์** ถ้ายังมีที่ไหนใช้อยู่ ต้องจัดการก่อน

**1g. ลบ dead CSS**
ลบบล็อก `DEVEXTREME OVERRIDES` (~287 บรรทัด) ใน `user-theme.css` **หลังจาก 1f ผ่านแล้วเท่านั้น**
- ระวัง: บล็อก `BOOTSTRAP OVERRIDES` ด้านบน (`.btn-primary`, `.form-control:focus`, ฯลฯ) **ห้ามลบ** — ตอนนี้กลายเป็นตัวหลักที่ทำให้หน้าตาเหมือนเดิม

> **หมายเหตุ:** ชื่อไฟล์ layout ยังเป็น `_DevExtremeLayout.cshtml` — **ห้ามเปลี่ยนชื่อในแผนนี้** (จะทำให้ diff ใหญ่และเสี่ยงพลาด) จดเป็นหนี้ไว้เปลี่ยนรอบหน้า

### §2 (ระดับ 1 — แก้ความรู้สึก) feedback ทันทีที่กด

ต่อให้ §1 เสร็จ การเปลี่ยนหน้าก็ยังมีดีเลย์บ้าง ⇒ ให้ผู้ใช้เห็นว่า "ระบบรับคำสั่งแล้ว":

- ใส่ handler กับปุ่มที่พาไปหน้าอื่น: `.btn-continue`, `.btn-list-view` (`ดูเนื้อหา`/`เรียนต่อ`/`ทบทวน`) และปุ่ม `กลับ` ใน Player
- กดแล้ว: เปลี่ยนไอคอนเป็น spinner + `pointer-events: none` กันกดซ้ำ (**ห้ามเปลี่ยนหน้าตาตอนปกติ** — เฉพาะสถานะ "กำลังโหลด")
- **ห้าม `preventDefault()`** — ต้องปล่อยให้ browser navigate ตามปกติ (แค่เพิ่ม visual state)
- ปุ่ม Play (`startCourse`) มี overlay อยู่แล้ว ถ้าจะเพิ่ม feedback ให้ทำเบา ๆ ไม่ต้องแตะ logic

## Contract ที่เปลี่ยน

- API / DB / migration: **ไม่มี**
- Global JS ที่หายไป: `DevExpress` ทั้ง namespace — **`showToast(message, type, duration)` ต้องคง signature เดิมเป๊ะ**
- ไม่แตะ SCORM adapter / commit / session timer / launchUrl (งานของ 097/103/104/105/106/107)

## นอก Scope (ห้ามทำ)

- **ห้ามเปลี่ยนดีไซน์/สี/ระยะ/ฟอนต์ใด ๆ** — เป้าหมายคือหน้าตาเหมือนเดิม 100%
- ห้ามเปลี่ยนชื่อไฟล์ layout
- ห้ามแตะ `dx.all.js` ในโฟลเดอร์ (ลบแค่ `<script>`/`<link>` ใน layout — เก็บไฟล์ไว้ก่อน เผื่อ rollback)
- ห้ามแตะ Player logic, Dashboard data flow, diagnostic mode ของ 102
- ห้ามแตะ `iLearn.Admin` / `iLearn.Admin.React` (ยังใช้ DevExtreme จริง)

## Verification

```powershell
dotnet build iLearn.User -o artifacts\verify-user
Remove-Item -Recurse -Force artifacts\verify-user
```

**A. หน้าตาเหมือนเดิม (ข้อบังคับจากผู้ใช้ — สำคัญที่สุด):**
1. Screenshot **before/after** ที่ viewport เดียวกัน วางเทียบกัน: หน้า login, dashboard (ช่องค้นหา + การ์ด), toast ทั้ง 4 type, dialog ออกจากระบบ
2. เทียบ computed style กับที่เก็บใน §0 — ค่าที่ต่างต้องอธิบายได้ทุกตัว
3. ตรวจ responsive: iPad landscape/portrait ไม่เพี้ยน

**B. ใช้งานได้ครบ:**
4. Login: กรอกรหัส → Enter ก็ได้ ปุ่มก็ได้; รหัสผิด → **เห็น toast error**; คีย์บอร์ด iPad ยังเป็น **numeric** + ปุ่ม Go
5. ค้นหาหลักสูตร: พิมพ์รัว ๆ ลื่น (debounce ยังทำงาน), highlight ถูก
6. ออกจากระบบ: dialog เด้ง → ยกเลิกได้ → ออกได้
7. toast ทุก type + **หลายอันซ้อนกันต้อง stack ลงล่างเหมือนเดิม**
8. session หมดอายุ (440) → ยังเห็น toast เตือนก่อน redirect
9. console 0 error ทุกหน้า; `window.DevExpress` ต้องเป็น `undefined`

**C. เร็วขึ้นจริง (เป้าหมายของงาน):**
10. วัด JS ที่โหลด/parse ต่อหน้า: **~5.3MB → ~170KB**
11. **บน iPad จริง:** กด `ดูเนื้อหา` → อาการนิ่ง ~0.5 วิ ต้องหายไปหรือลดลงชัดเจน ← ตัวชี้วัดจริงของงานนี้
12. §2: กดแล้วเห็น spinner ทันที ไม่รู้สึกว่าค้าง

## Deploy note

แตะเฉพาะ **iLearn.User** → deploy learner อย่างเดียว ไม่มี migration
**Rollback ง่าย:** ไฟล์ `dx.all.js`/`dx.light.css` ยังอยู่ในโฟลเดอร์ (ไม่ได้ลบ) — ถ้าพังคืน `<script>`/`<link>` ใน layout กลับได้ทันที

## Implementer Notes

_(เติมโดย implementer — **ต้องแนบ baseline §0 และ screenshot เทียบ before/after**)_
