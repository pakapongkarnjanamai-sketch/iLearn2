# PLAN-109: Player สถานะ — เหลือ 3 คำ + เลิก pill เป็นข้อความมีสี + สลับปุ่ม toolbar

- **Status:** VERIFIED — deployed QA + PROD, smoke tested both (Playwright), ผู้ใช้อนุมัติ PROD ในแชท (2026-07-22)
- **Assigned:** GitHub Copilot
- **Reviewer:** Claude Code
- **สร้างเมื่อ:** 2026-07-22
- **ที่มา:** ผู้ใช้รีวิว UI Player บน iPad — ขอ (1) ปรับคำสถานะให้เหมาะสม (ตัด `ดูอย่างเดียว`, `พร้อมแสดงผล`) (2) เลิกใช้ pill ให้เป็นข้อความเหมือน `course-meta-value` แต่มีสีตามสถานะ (เลือกแบบ B) (3) สลับตำแหน่งปุ่ม เต็มจอ ↔ พับแถบข้อมูล
- **QA deploy: ผู้ใช้อนุมัติล่วงหน้าแล้ว** — implement เสร็จ deploy QA ได้เลย (ยังห้าม PROD)
- แตะเฉพาะ **`Player.cshtml`** (+ inline CSS ในไฟล์เดียวกัน) — งานเล็ก ไม่มี migration

---

## บริบท (ยืนยันจากโค้ด)

**คำสถานะที่ใช้จริง** (`setCourseStatusDisplay(label, variant)`):

| คำ | variant | แสดงเมื่อ | จะทำอย่างไร |
| --- | --- | --- | --- |
| `กำลังเรียน` | muted | กำลังเรียน | คงไว้ |
| `เรียนจบแล้ว` | success | จบ (server / allPassed) | คงไว้ |
| `ไม่ผ่านเกณฑ์` | danger | สอบตก | คงไว้ |
| `พร้อมแสดงผล` | success | ผ่านครบทุก item (live) | **→ เปลี่ยนเป็น `เรียนจบแล้ว`** |
| `ดูอย่างเดียว` | warning | browse-only (ไม่มี enrollment) | **ลบ** — PLAN-107 ซ่อนแถวสถานะเคสนี้อยู่แล้ว ไม่มีทางแสดง |

- `status-*` classes (`.status-muted/success/danger/warning` + `.course-status-pill`) ใช้**เฉพาะ `#courseStatusDisplay` เท่านั้น** (grep ยืนยัน — markup บรรทัด 683 + `removeClass` บรรทัด 1023) ⇒ แก้ได้ปลอดภัย
- `setCourseStatusDisplay` ยัง toggle `status-*` + `.text()` เหมือนเดิม — **ห้ามแตะ signature/logic ตัวนี้**

## Scope

### §1 — คำสถานะเหลือ 3 คำ

แก้ **string เท่านั้น** ที่จุด `setCourseStatusDisplay(...)`:

**`พร้อมแสดงผล` → `เรียนจบแล้ว`** (1 จุด — `recalcTotalProgress` สาขา `allPassed` ~บรรทัด 1914)
- ⚠️ บรรทัดถัดไปเป็นตัว **enable `#btnPreSave`** ("แสดงผลการเรียน") — **แก้แค่ข้อความ ห้ามแตะ `.prop("disabled", ...)`**

**`ดูอย่างเดียว` → ลบทิ้ง** (3 จุด: `renderCourseDetails` ~1049, `setupReadOnlyMode` ~1509, `recalcTotalProgress` ~1934)
- ทั้ง 3 อยู่ในสาขา read-only-ไม่-completed ซึ่ง 107 ซ่อนแถวสถานะไปแล้ว ⇒ ลบ call `setCourseStatusDisplay("ดูอย่างเดียว", "warning")` ออก (สาขานั้นไม่ต้อง set อะไร)
- ตรวจว่าโครง if/else ยังถูกต้องหลังลบ (สาขา isCompleted → `เรียนจบแล้ว` ยังอยู่)

หลังแก้: เหลือ `กำลังเรียน` / `เรียนจบแล้ว` / `ไม่ผ่านเกณฑ์` เท่านั้น (variant `warning` เลิกใช้ — ลบ `.status-warning` ทิ้งได้)

### §2 — เลิก pill → ข้อความมีสี (แบบ B)

**markup** (บรรทัด 683):
```html
<!-- เดิม -->
<span class="course-status-pill status-muted" id="courseStatusDisplay">-</span>
<!-- ใหม่ -->
<span class="course-meta-value status-muted" id="courseStatusDisplay">-</span>
```
- ใช้ `.course-meta-value` ให้ฟอนต์/ขนาด/น้ำหนักตรงกับแถวอื่น (ผู้เรียน/หมวดหมู่/ประเภท)
- คง `id="courseStatusDisplay"` + `status-muted` เริ่มต้น

**CSS** — เปลี่ยน `.status-*` จาก "พื้น pill + สีอักษร" → **สีอักษรอย่างเดียว** (ไม่มี background):
```css
.status-muted  { color: #475569; }
.status-success{ color: #1e7e34; }
.status-danger { color: var(--danger-color); }
```
- **ลบ** rule `.course-status-pill` (บรรทัด 220-230) และ `.status-warning` — ไม่มีใครใช้แล้ว
- `.course-meta-value` มี `color:#1f2937` อยู่ — ต้องให้ `.status-*` ชนะ: มันถูกประกาศ**หลัง** `.course-meta-value` ในไฟล์อยู่แล้ว (source order ชนะที่ specificity เท่ากัน) — **วาง `.status-*` ให้อยู่หลัง `.course-meta-value` เสมอ** (ถ้าย้ายบล็อกให้ระวังข้อนี้)
- `.course-meta-value` มี ellipsis (nowrap/overflow) — สถานะสั้น ไม่กระทบ

### §3 — สลับปุ่ม toolbar (**Claude ทำแล้วใน working tree**)

`scorm-toolbar-actions` (บรรทัด ~603): สลับลำดับเป็น **[เต็มจอ `#btnFullscreen`] [พับแถบข้อมูล `#btnToggleSidebar`]**
- สลับแค่ลำดับ 2 `<button>` — id/onclick/class/`d-none d-lg-inline-flex` เดิมครบ
- **Claude แก้ให้แล้ว** (build ผ่าน) — Copilot แค่ commit รวมกับ §1/§2 ไม่ต้องทำซ้ำ

## Contract ที่เปลี่ยน

- API / DB / migration: **ไม่มี**
- DOM: `#courseStatusDisplay` เปลี่ยน class จาก `course-status-pill` → `course-meta-value` (คง id + status-*); `setCourseStatusDisplay` เหมือนเดิม
- CSS class ที่หาย: `.course-status-pill`, `.status-warning`

## นอก Scope (ห้ามทำ)

- ห้ามแตะ `setCourseStatusDisplay`/`setCourseProgressDisplay`/`recalcTotalProgress` **logic** (แก้แค่ string label ใน §1)
- ห้ามแตะ `#btnPreSave` enable/disable
- ห้ามแตะงาน browse-only ของ 107, session timer 104, commit 105, auto-summary 106
- ห้ามเปลี่ยนสี/ฟอนต์ของแถวอื่น หรือ layout header

## Verification

```powershell
dotnet build iLearn.User -o artifacts\verify-user
Remove-Item -Recurse -Force artifacts\verify-user
```

Manual (QA):
1. คอร์สกำลังเรียน → สถานะ **`กำลังเรียน`** สีเทา, **ไม่ใช่ pill** (ข้อความเหมือนแถวอื่น)
2. เล่นผ่านครบทุก item → สถานะ **`เรียนจบแล้ว`** สีเขียว (ไม่ใช่ `พร้อมแสดงผล`); ปุ่ม "แสดงผลการเรียน" ยัง**เปิดใช้ได้** (logic ไม่พัง)
3. สอบตก → **`ไม่ผ่านเกณฑ์`** สีแดง
4. คอร์สที่ไม่ได้ assign (browse-only) → **ไม่มีแถวสถานะ** (107 ยังทำงาน) — ไม่มี `ดูอย่างเดียว` โผล่
5. คอร์สเรียนจบแล้ว (ทบทวน) → **`เรียนจบแล้ว`** สีเขียว + progress 100% (เคส B ของ 107 ไม่พัง)
6. toolbar: ปุ่ม **เต็มจออยู่ซ้าย พับแถบข้อมูลอยู่ขวา**; ทั้งคู่ยังทำงาน (fullscreen sync icon, พับ sidebar) — บนจอเล็กปุ่มพับยังถูกซ่อน
7. console 0 error; `window.DevExpress` = undefined (108 ยังอยู่)

**แนบ screenshot** สถานะทั้ง 3 แบบ (กำลังเรียน/จบ/ตก) + toolbar ให้ reviewer เทียบ

## Deploy note

deploy **learner** อย่างเดียว (`tools\deploy-user.ps1`, default QA) — ไม่มี migration · **QA อนุมัติแล้ว** · PROD รอผู้ใช้ยืนยันในแชท

## Implementer Notes

- §1: แก้ label string ครบ 4 จุด — `renderCourseDetails` (isCompleted→เรียนจบแล้ว, else if !isReadOnly→กำลังเรียน, ตัดสาขา isReadOnly ทิ้ง), `setupReadOnlyMode` (เหลือแค่ if isCompleted→เรียนจบแล้ว ไม่มี else), `recalcTotalProgress` ทั้ง 2 จุด (allPassed→เรียนจบแล้ว, readOnly branch เหลือแค่ if isCompleted). ไม่แตะ `.prop("disabled", ...)` ของ `#btnPreSave` ตามที่สั่ง
- §2: markup `#courseStatusDisplay` เปลี่ยน class `course-status-pill` → `course-meta-value` (คง id + status-muted เริ่มต้น). CSS ลบ `.course-status-pill` และ `.status-warning` ทิ้ง, เปลี่ยน `.status-muted/.status-success/.status-danger` เหลือแค่ `color` (ไม่มี background) — ยังอยู่หลัง `.course-meta-value` ใน source order เหมือนเดิม
- ไม่แตะ `removeClass("status-muted status-success status-danger status-warning")` ใน `setCourseStatusDisplay` — เหลือ reference ถึง class ที่ไม่มี CSS แล้วแต่ไม่กระทบ (ตามคำสั่งห้ามแตะ logic ของฟังก์ชันนี้)
- §3: ปุ่ม toolbar สลับไว้แล้วโดย Claude ก่อนหน้า (ยืนยันด้วย grep — `#btnFullscreen` มาก่อน `#btnToggleSidebar` ที่บรรทัด 604/607) — ไม่ต้องแก้ซ้ำ
- Verified: `dotnet build iLearn.User -o artifacts\verify-user-109` → Build succeeded 0 errors (74 warnings เดิม, ไม่เกี่ยวกับงานนี้), ลบ artifacts แล้ว
- **Deploy QA:** `tools\deploy-user.ps1` → stamp `_user_deploy_20260722083900` live (คำสั่งเดิม `_user_deploy_20260721170621`)
- **Smoke QA (Playwright, learner 610034, courseId=968 "TEST" กำลังเรียน 50%):**
  - `#courseStatusDisplay` → `className="course-meta-value status-muted"`, computed `color: rgb(71,85,105)`, `background: rgba(0,0,0,0)` (โปร่งใส, ไม่มี pill), `border-radius: 0px`, text = `กำลังเรียน` ✅ ตรง §1/§2
  - Toolbar: `#btnFullscreen` มาก่อน `#btnToggleSidebar` ในโครง DOM จริง ✅ ตรง §3
  - `window.DevExpress === undefined` ✅ (PLAN-108 ยังอยู่), console 0 error ระหว่างโหลด Player
  - **ไม่ได้ทดสอบบน QA** สถานะ `เรียนจบแล้ว`/`ไม่ผ่านเกณฑ์` จริง — บัญชีทดสอบ 610034 บน QA ไม่มีคอร์สที่ completed/failed อยู่ในตอนนี้ (KPI แสดง 0 เรียนจบแล้ว)
- **ผู้ใช้ยืนยัน deploy PROD ในแชท (2026-07-22)** — deploy ด้วย `tools\deploy-user-prod.ps1` → stamp `_user_deploy_20260722085448` live, post-deploy health check `HTTP 200` ผ่านอัตโนมัติ
- **Smoke PROD (Playwright, learner 610034 — บัญชีทดสอบเดิมที่เคยใช้ E2E บน PROD มาก่อน, อ่านอย่างเดียว ไม่กด commit/save ใด ๆ):**
  - courseId=507 "SA-101-JP" (คอร์สที่ completed จริงของบัญชีนี้) → `#courseStatusDisplay` computed: `className="course-meta-value status-success"`, `color: rgb(30,126,52)` (เขียว), `background: transparent`, `border-radius: 0px`, text = `เรียนจบแล้ว` ✅ **ปิด gap ที่ QA ไม่มีให้เทียบ** — ยืนยันสถานะสีเขียว/เรียนจบแล้วทำงานถูกต้องจริงบน PROD
  - Toolbar `#btnFullscreen` → `#btnToggleSidebar` ✅, `window.DevExpress === undefined` ✅, console 0 error
  - ยังไม่มีเคส `ไม่ผ่านเกณฑ์` (สีแดง) ให้ทดสอบจริงในบัญชีนี้ — logic ไม่เปลี่ยน (แก้แค่ label string) ความเสี่ยงต่ำ, ปิด session ด้วยการกด "ออกจากระบบ" เรียบร้อยหลังทดสอบ

## Reviewer Sign-off (Claude Code, 2026-07-22)

- **§1 ผ่าน:** เหลือ 3 label — `กำลังเรียน`/`เรียนจบแล้ว`/`ไม่ผ่านเกณฑ์` (grep ยืนยัน 0 `ดูอย่างเดียว`, 0 `พร้อมแสดงผล`); สาขา read-only-non-completed ตัด call ทิ้งครบ 3 จุด, โครง if/else เหลือ isCompleted→เรียนจบแล้ว ถูกต้อง; **ไม่แตะ `.prop("disabled")` ของ #btnPreSave** ✅
- **§2 ผ่าน — render พิสูจน์ทั้ง 3 สี:** markup `course-meta-value status-muted` (ไม่มี pill); `.status-*` เป็น **สีอักษรอย่างเดียว** วัดจริง: กำลังเรียน `#475569` · เรียนจบแล้ว `#1e7e34` · ไม่ผ่านเกณฑ์ `#dc3545` — ทุกตัว **background โปร่งใส, padding 0, radius 0, font 12.8px/600 เท่า `.course-meta-value` เป๊ะ** ✅ `.course-meta-value`(210) มาก่อน `.status-*`(220) → สีอักษรชนะ ✅ ลบ `.course-status-pill` + `.status-warning` rule แล้ว (`status-warning` เหลือแค่ใน `removeClass` — ไม่มี CSS แล้ว ไม่มีผล) ✅
- **§3 ผ่าน:** `#btnFullscreen`(584) มาก่อน `#btnToggleSidebar`(587); id/onclick/`d-none d-lg-inline-flex` เดิมครบ ✅
- **Verify อิสระ:** build 0 errors · `node --check` ผ่าน · render measurement เทียบ reference plain value
- **Copilot smoke QA:** สถานะ `กำลังเรียน` computed color rgb(71,85,105) = `#475569` ตรงกับที่ผมวัด · toolbar order · DevExpress undefined · 0 error
- **คงค้างก่อน VERIFIED:** ยังไม่ได้ทดสอบ `เรียนจบแล้ว`/`ไม่ผ่านเกณฑ์` บน state จริง (QA account ไม่มีคอร์ส completed/failed) — **แต่ผม render พิสูจน์สีทั้ง 2 แล้ว + logic เป็นแค่ label string ที่ setCourseStatusDisplay เดิม** ⇒ ความเสี่ยงต่ำมาก; ยืนยันเต็มเมื่อผู้ใช้เล่นคอร์สจบ/ตกจริง

**สรุป: ผ่านรีวิว ไม่มี finding**
