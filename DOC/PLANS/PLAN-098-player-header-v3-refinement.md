# PLAN-098: Player header v3 — คืน label ครบ + ย้าย pill สถานะ + ฟอนต์ TOC เข้า type scale

- **Status:** DONE → REVIEWED (code + runtime render ผ่านสะอาด — รอ commit + iPad smoke ก่อน VERIFIED)
- **Assigned:** Antigravity (Gemini)
- **Reviewer:** Claude Code
- **สร้างเมื่อ:** 2026-07-17
- **ต่อยอดจาก:** [PLAN-097](PLAN-097-player-ipad-ux.md) (REVIEWED, **ยังไม่ commit**) — งานนี้ปรับ `course-header-panel` ที่ 097 เพิ่งทำ ⇒ **พับเข้า commit เดียวกับ 097** (ไม่ใช่ deploy รอบใหม่)
- **ที่มา:** ผู้ใช้เห็นผลจริงบน QA (คอร์ส "Software back up (Re.3)") — header v1 ของ 097 กระชับเกินไป: บรรทัด meta ถูกตัดจน**รหัสผู้เรียนเหลือ "21…"** (ค่าห้ามตัดสุดบนเครื่องใช้ร่วม), label หายหมดจนไม่รู้ว่าค่าไหนคืออะไร, ชื่อคอร์สยาว clamp 2 บรรทัดโดน tooltip ช่วยไม่ได้บน touch. ผู้ใช้อนุมัติแบบ v3 ในแชท (2026-07-17)

---

## บริบท (สภาพปัจจุบันหลัง 097 — ยืนยันจากไฟล์)

โครง header ตอนนี้ ([Player.cshtml](../../iLearn.User/Views/MyLearning/Player.cshtml)):

```html
<div class="course-header-panel">
    <h5 class="course-title" id="courseTitleDisplay">กำลังโหลดข้อมูล...</h5>
    <div class="course-meta-line">
        <span class="course-status-pill status-muted" id="courseStatusDisplay">-</span>
        <span class="course-meta-text">
            <span id="categoryNameDisplay">-</span> · <span id="courseTypeNameDisplay">-</span> · <span id="learnerCodeDisplay">-</span>
        </span>
    </div>
    <div class="course-progress-row">
        <div class="course-progress-track"><div class="course-progress-fill" id="courseProgressFill"></div></div>
        <span class="course-progress-value" id="courseProgressDisplay">0%</span>
    </div>
</div>
```

ปัญหา: 3 ค่า + pill เบียดใน 1 บรรทัด ~288px → ค่าท้าย (รหัสผู้เรียน) โดนตัดเสมอ; ไม่มี label.

**JS ที่ผูกกับ header (ห้ามพัง — contract ภายใน):**
- IDs: `courseTitleDisplay, learnerCodeDisplay, categoryNameDisplay, courseTypeNameDisplay, courseStatusDisplay, courseProgressFill, courseProgressDisplay` (ครบ 7)
- `setCourseStatusDisplay(label, variant)` → `$("#courseStatusDisplay")` toggle class `status-muted/success/danger/warning` + `.text()`
- `renderCourseDetails` → `.text()` + `.attr("title", …)` บน 4 ID (title/category/type/learner)
- `setCourseProgressDisplay` → `#courseProgressDisplay` + `#courseProgressFill`

**ฟอนต์ TOC ปัจจุบัน (ไม่เข้ากับ scale):** template ใช้ Bootstrap utility inline — ชื่อบทเรียน `.fw-bold` = **1rem (16px)** ซึ่ง**ใหญ่กว่าชื่อคอร์ส 0.95rem** (ลำดับชั้นกลับหัว); ไอคอน `.fs-4` = **1.5rem**; `.player-status-icon` = 1.2rem. type scale ระบบอยู่ที่ `wwwroot/css/user-theme.css` (`--text-xs 0.75 / --text-sm 0.85 / --text-base 0.95 / --text-md 1.05`)

## Scope (แก้เฉพาะ `Player.cshtml` — CSS + template + 0 logic)

### 1. Header v3 — คืน label เป็นแถว + pill เป็นแถว "สถานะ"

เปลี่ยน markup `.course-header-panel` เป็น (คง IDs ครบ 7):

```html
<div class="course-header-panel">
    <h5 class="course-title" id="courseTitleDisplay">กำลังโหลดข้อมูล...</h5>
    <div class="course-meta-list">
        <div class="course-meta-row">
            <span class="course-meta-label">ผู้เรียน</span>
            <span class="course-meta-value" id="learnerCodeDisplay">-</span>
        </div>
        <div class="course-meta-row">
            <span class="course-meta-label">หมวดหมู่</span>
            <span class="course-meta-value" id="categoryNameDisplay">-</span>
        </div>
        <div class="course-meta-row">
            <span class="course-meta-label">ประเภท</span>
            <span class="course-meta-value" id="courseTypeNameDisplay">-</span>
        </div>
        <div class="course-meta-row">
            <span class="course-meta-label">สถานะ</span>
            <span class="course-status-pill status-muted" id="courseStatusDisplay">-</span>
        </div>
    </div>
    <div class="course-progress-row">
        <div class="course-progress-track"><div class="course-progress-fill" id="courseProgressFill"></div></div>
        <span class="course-progress-value" id="courseProgressDisplay">0%</span>
    </div>
</div>
```

CSS:
- **เพิ่ม** `.course-meta-list { display: flex; flex-direction: column; gap: 5px; margin-bottom: 10px; }`
- **เพิ่ม** `.course-meta-row { display: flex; align-items: center; gap: 8px; min-width: 0; }`
- **เพิ่ม** `.course-meta-label { flex: 0 0 60px; color: #94a3b8; font-size: var(--text-xs); }` (label คงที่ 60px — ผู้เรียน/หมวดหมู่/ประเภท/สถานะ พอดี)
- **เพิ่ม** `.course-meta-value { min-width: 0; color: #1f2937; font-size: 0.8rem; font-weight: 600; overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }` (ตัดเฉพาะค่ายาว เช่นหมวดหมู่ — รหัสผู้เรียนไม่มีวันโดนตัดเพราะได้บรรทัดของตัวเอง)
- **ลบ** `.course-meta-line`, `.course-meta-text` (ที่ 097 เพิ่ม — เลิกใช้)
- `.course-status-pill` คงเดิม (flex-shrink:0 + status-* variants) — อยู่เป็น value ของแถวสถานะ **ห้ามใส่ ellipsis กับ pill**
- `.course-title`: `-webkit-line-clamp: 2` → **3** (รองรับชื่อยาว); คง `.attr("title")` เดิมของ 097 ไว้

### 2. ฟอนต์ TOC เข้า type scale (`.contentItem-item`)

Template `renderUI` — แก้ 2 utility class:
- **ไอคอน:** `<div class="me-3 fs-4 player-icon-slot">` → ลบ `fs-4` (ไอคอนกลับเป็น 1rem/16px พอดีแถวกระชับ)
- **ชื่อบทเรียน:** `<div class="fw-bold text-truncate" …>` → `<div class="contentItem-name text-truncate" …>`

CSS:
- **เพิ่ม** `.contentItem-name { font-size: var(--text-sm); font-weight: 600; color: #172033; }` (0.85rem — เล็กกว่าชื่อคอร์ส 0.95rem ตามลำดับชั้น; คง `text-truncate` เดิมสำหรับ ellipsis)
- **แก้** `.player-status-icon { font-size: 1.2rem; }` → `1rem` (check/x icon พอดีแถว)
- แถว `.item-progress-track` / progress bar / active-bar ในแต่ละ item **คงเดิม**

## Contract ที่เปลี่ยน

- API / DB / iLearn.API / endpoint: **ไม่มี**
- DOM: IDs ทั้ง 7 คงเดิม — เปลี่ยนแค่ wrapper/ตำแหน่งของ `#courseStatusDisplay` (จากใน `.course-meta-line` ไปเป็น value ของแถว "สถานะ"); `setCourseStatusDisplay` ยัง target ID เดิม class เดิม
- CSS class ที่หาย: `.course-meta-line`, `.course-meta-text` (097 เพิ่ง add — ไม่มี JS อ้าง); ที่เพิ่ม: `.course-meta-list/.course-meta-row/.course-meta-label/.course-meta-value/.contentItem-name`

## นอก Scope (ห้ามทำ)

- ห้ามแตะ JS logic ใด ๆ — งานนี้ CSS + 2 template block เท่านั้น (`renderCourseDetails` ที่ set title attr ของ 097 **คงไว้ครบ ห้ามลบ**)
- ห้ามแตะ `_DevExtremeLayout.cshtml`, `Program.cs`, `MyLearningController.cs`, `Home/Index.cshtml`, `MyLearning/Index.cshtml`, appsettings (ของ 096/097 — จบแล้ว)
- ห้ามแตะ pseudo-fullscreen / sidebar-collapse / ping / lifecycle flush ของ 097
- ห้ามเพิ่มฟิลด์ใหม่ (กำหนดส่ง/คะแนน) — ผู้ใช้ยืนยัน 4 ค่าพอ (ผู้เรียน/หมวดหมู่/ประเภท/สถานะ)
- ห้ามแตะ type scale ใน `user-theme.css` — ใช้ตัวแปรที่มีอยู่

## Verification

```powershell
dotnet build iLearn.User -o artifacts\verify-user
Remove-Item -Recurse -Force artifacts\verify-user
```

Manual (desktop — เปิด Player 1 คอร์ส):

1. header แสดง 4 แถว label ครบ (ผู้เรียน/หมวดหมู่/ประเภท/สถานะ) — **รหัสผู้เรียนเต็มไม่โดนตัด**
2. คอร์สที่ไม่ผ่าน → แถวสถานะเป็น pill "ไม่ผ่านเกณฑ์" สีแดง; เรียนจบ → เขียว; read-only → เหลือง (เทส `setCourseStatusDisplay` ทุก variant ผ่าน state จริง)
3. หมวดหมู่ยาว → ตัด ellipsis เฉพาะแถวนั้น (ค่าอื่นไม่กระทบ); hover เห็น title tooltip
4. ชื่อคอร์สยาว → เห็นได้ถึง 3 บรรทัด
5. TOC: ชื่อบทเรียน**เล็กกว่า**ชื่อคอร์ส, ไอคอน + check/x พอดีแถว ไม่ล้น
6. progress bar + % (ทั้ง header และ per-item) แสดงถูก; console 0 error

**iPad smoke (ผู้ใช้)** — ทำรวมกับ smoke ของ 097: header อ่านครบทุกค่าบนจอจริง, TOC ไม่แน่นเกิน

## Implementer Notes

- ปรับปรุงโครงสร้าง HTML ใน `.course-header-panel` ใน `Player.cshtml` เป็น 4 แถว label (ผู้เรียน/หมวดหมู่/ประเภท/สถานะ) โดยคง Element IDs ทั้ง 7 ตามเดิม
- ปรับเพิ่ม CSS `.course-meta-list`, `.course-meta-row`, `.course-meta-label`, `.course-meta-value`, `.contentItem-name` และเปลี่ยน `-webkit-line-clamp` ของ `.course-title` เป็น 3 บรรทัด
- ปรับขนาดฟอนต์ TOC (`.contentItem-item`): ใช้ `var(--text-sm)` (0.85rem) กับ `.contentItem-name`, เอา `fs-4` ออกจากไอคอน slot, ปรับขนาด `.player-status-icon` เป็น 1rem
- ผ่านการตรวจสอบ build ด้วย `dotnet build iLearn.User -o artifacts\verify-user` สำเร็จ 0 errors

## Reviewer Sign-off (Claude Code, 2026-07-21)

ตรวจ diff header/TOC (แยกจากงาน 097 ที่ปนไฟล์เดียวกัน) + verify อิสระเกินที่ implementer ทำ (Gemini รันแค่ build — ผมเพิ่ม runtime render จริงด้วย DOM measurement):

- **§1 Header:** markup 4 แถว label ตรง template ในแผนเป๊ะ, **IDs ครบ 7** (courseTitle/learner/category/courseType/courseStatus/progressFill/progress), `#courseStatusDisplay` เป็น value แถว "สถานะ" คง class `course-status-pill status-muted` (setCourseStatusDisplay ยัง target ถูก) ✅ CSS `.course-meta-list/row/label(flex 0 0 60px, --text-xs)/value(0.8rem ellipsis)` ตรงค่า ✅ ลบ `.course-meta-line/.course-meta-text` หมด ✅ course-title clamp 2→**3** ✅
- **§2 TOC font:** ลบ `fs-4` จากไอคอน slot ✅ `fw-bold`→`.contentItem-name`(--text-sm 0.85rem) ✅ `.player-status-icon` 1.2→**1rem** ✅
- **คง 097 ครบ:** `renderCourseDetails` ยังมี `.attr("title", …)` ทั้ง 3 จุด (title/category/type) — ไม่ถูกลบตามที่กำชับ ✅
- **Verify อิสระ:** `dotnet build` 0 errors; `node --check` inline JS ผ่าน; **runtime render** (publish → standalone page ประกอบ CSS จริง + user-theme.css + mock data) วัดจาก DOM:
  - ชื่อคอร์ส 15.2px (0.95rem) > ชื่อบทเรียน 13.6px (0.85rem) → **ลำดับชั้นถูกแก้แล้ว** (เดิมกลับหัว)
  - **รหัสผู้เรียน "610034" ไม่โดนตัด** (learnerTruncated=false) — เป้าหมายหลักของ v3 บรรลุ
  - หมวดหมู่ยาว → ellipsis เฉพาะแถวนั้น (catEllipsized=true); ชื่อคอร์สยาว → clamp 3 บรรทัดจริง (webkitLineClamp=3, สูง ~62px)
  - pill "ไม่ผ่านเกณฑ์" variant danger → พื้นแดงอ่อน + **ไม่โดน ellipsis** (pillEllipsized=false)
  - header สูง ~187px (จากเดิม ~300px) — คืนพื้นที่ TOC ตามเป้า
- **ไม่ล้ำเขต:** แตะเฉพาะ CSS+2 template block ใน Player.cshtml, 0 logic; ไม่แตะไฟล์ 096/097/099

**สรุป: ผ่านรีวิว ไม่มี finding — รอ commit (พับรวม 097) + iPad smoke**
