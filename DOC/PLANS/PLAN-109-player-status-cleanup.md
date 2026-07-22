# PLAN-109: Player สถานะ — เหลือ 3 คำ + เลิก pill เป็นข้อความมีสี + สลับปุ่ม toolbar

- **Status:** READY
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

_(เติมโดย implementer — แนบ screenshot สถานะ + toolbar)_
