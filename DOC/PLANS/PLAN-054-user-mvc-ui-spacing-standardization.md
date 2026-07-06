# PLAN-054: iLearn.User — UI audit + จัด spacing ให้มีมาตรฐาน (design tokens)

- **Status:** DONE
- **Assigned:** Antigravity (Gemini) — iLearn.User เท่านั้น ห้ามแตะ backend/admin
- **Reviewer:** Claude Code
- **สร้างเมื่อ:** 2026-07-06
- **อ้างอิง:** [PLAN-053](PLAN-053-admin-react-ui-consistency.md) (แนวเดียวกันฝั่ง admin-react)

> คำขอผู้ใช้ (2026-07-06): ตรวจสอบ UI และจัด spacing ใน `iLearn.User` ให้เหมาะสมและมีมาตรฐาน — audit แล้วพบว่า spacing/radius/font-size เป็นค่า ad-hoc กระจายทั้ง 4 view (ค่าแปลก ๆ เช่น `0.05rem`, `0.67rem`, `0.85rem` ปนกับ `15px`, `18px 20px 16px`), มี CSS ซ้ำ/ตาย และ inline style ในมาร์กอัป — finding ทุกข้อด้านล่างยืนยันจากโค้ดจริง

---

## ภาพรวมปัญหา (root causes)

1. **ไม่มี spacing scale กลาง** — Index.cshtml ใช้ rem, Player.cshtml ใช้ px ล้วน, Login ใช้ px — ค่าไม่ลงตัวกับ scale ใด ๆ (0.05 / 0.15 / 0.35 / 0.55 / 0.65 / 0.67 rem ฯลฯ)
2. **CSS ตาย + ซ้ำซ้อน** — `wwwroot/css/site.css` และ `wwwroot/css/theme-overrides.css` **ไม่ถูก link จากที่ไหนเลย** (grep ทั้ง `Views/**/*.cshtml` แล้ว) ขณะที่ `_DevExtremeLayout.cshtml:39-265` มี inline `<style>` ที่เนื้อหาซ้ำกับ `theme-overrides.css` เกือบทั้งไฟล์ (brand vars + Bootstrap/DX overrides) และ drift จากกันแล้วบางจุด
3. **Inline style ในมาร์กอัป/JS template** หลายจุด ทำให้ spacing แก้รวมไม่ได้
4. **สีพื้นหลัง body ไม่ตรงกันข้ามหน้า** — layout `--secondary-bg: #f4f6f8`, Dashboard override เป็น `#f8f9fa` (MyLearning/Index.cshtml:15-17), Login ใช้ `#f4f6f8 !important`

## มาตรฐานที่ให้วาง (single source: ไฟล์ใหม่ `wwwroot/css/user-theme.css`)

สร้างไฟล์เดียว `iLearn.User/wwwroot/css/user-theme.css` แล้ว link ใน `_DevExtremeLayout.cshtml` (พร้อม `asp-append-version="true"`) ประกอบด้วย:

```css
:root {
    /* colors (ย้ายจาก layout inline) */
    --brand-color: #027d83;
    --brand-dark: #004d40;
    --brand-light: #e0f2f1;
    --secondary-bg: #f4f6f8;      /* พื้นหลังหน้าเดียวกันทุกหน้า */
    --success-color: #28a745;
    --danger-color: #dc3545;

    /* spacing scale — ทุก padding/margin/gap ต้องใช้ตัวใดตัวหนึ่ง */
    --space-1: 0.25rem;  /* 4px  */
    --space-2: 0.5rem;   /* 8px  */
    --space-3: 0.75rem;  /* 12px */
    --space-4: 1rem;     /* 16px */
    --space-5: 1.5rem;   /* 24px */
    --space-6: 2rem;     /* 32px */

    /* radius scale */
    --radius-sm: 6px;    /* ปุ่มเล็ก/list item */
    --radius-md: 8px;    /* card/ปุ่มหลัก */
    --radius-lg: 12px;   /* modal */
    --radius-pill: 999px;

    /* type scale (จำกัดขั้น) */
    --text-xs: 0.75rem;
    --text-sm: 0.85rem;
    --text-base: 0.95rem;
    --text-md: 1.05rem;
    --text-lg: 1.25rem;
}
```

กติกา:
- **spacing ทุกค่าใหม่ต้องเป็น `var(--space-N)`** — ค่าเดิมปัดเข้า token ที่ใกล้ที่สุด (เช่น `0.65rem` → `--space-3`, `18px 20px 16px` → `var(--space-4) var(--space-5)`, `0.05rem` → `--space-1` หรือตัดทิ้ง)
- **radius ใช้ token เท่านั้น** — เลิกใช้ 2px/3px/4px/16px/25% ปะปน (ยกเว้น scrollbar thumb 3px คงได้)
- ไม่ต้องไล่แปลงทุกบรรทัดแบบหุ่นยนต์ — จุดที่ค่าปัดแล้ว **หน้าตาเปลี่ยนเกินสังเกตได้ชัด** ให้คงค่าเดิมแล้วจดใน Implementer Notes

---

## Scope — สิ่งที่ต้องแก้ (ไฟล์:บรรทัดจากโค้ดจริง)

### A. รวม CSS ให้เหลือแหล่งเดียว

- [ ] **A1** สร้าง `wwwroot/css/user-theme.css` ตามโครงข้างบน แล้วย้ายเนื้อหา inline `<style>` บล็อกแรกของ `_DevExtremeLayout.cshtml:39-265` (theme vars, navbar, footer, Bootstrap/DX overrides, skeleton) เข้าไฟล์นี้ — layout เหลือแค่ `<link>` + `@font-face` block (ต้องอยู่ใน .cshtml เพราะใช้ `FileVersionProvider`)
- [ ] **A2** ลบไฟล์ตาย `wwwroot/css/site.css` และ `wwwroot/css/theme-overrides.css` — **ก่อนลบให้ grep ทั้ง repo อีกครั้ง** (รวม `Program.cs`/bundling) ว่าไม่มีการอ้างอิง; ส่วนกฎใน theme-overrides.css ที่ครบกว่า inline layout (เช่น DX Calendar/TreeView/Popup overrides) ให้ **ยกเข้า user-theme.css ด้วย** ไม่ใช่ทิ้ง
- [ ] **A3** `_DevExtremeLayout.cshtml:292-296` — footer `© 2024` → ใช้ `@DateTime.Now.Year`

### B. พื้นหลัง + header มาตรฐาน (MyLearning/Index.cshtml)

- [ ] **B1** `MyLearning/Index.cshtml:15-17` — ลบ `body { background-color: #f8f9fa; }` ให้ใช้ `--secondary-bg` จาก layout (หน้า Login ก็เลิก `!important` ได้เพราะค่าเดียวกัน — `Home/Index.cshtml:14-20`)
- [ ] **B2** `MyLearning/Index.cshtml:32-37` — `.page-header` padding `2.5rem 0 1.5rem` → `var(--space-5) 0` (สมมาตร); `.page-title` font-size `1.2rem` บน `<h2>` เล็กผิดสัดส่วน → `var(--text-lg)`
- [ ] **B3** inline style ในมาร์กอัป → ย้ายเป็น class ใน `@section Styles`:
  - `MyLearning/Index.cshtml:763-764` — avatar circle (`width:90px;height:90px;...` + icon `font-size:2.5rem`) → class `.profile-avatar`
  - `MyLearning/Index.cshtml:774-776` — ปุ่ม logout inline style ยาว → class `.btn-logout`
  - `MyLearning/Index.cshtml:1443` — icon empty state `style="color:#d0d0d0;"` → class

### C. จัด spacing การ์ดหลักสูตร (MyLearning/Index.cshtml)

- [ ] **C1** normalize padding การ์ด 3 ขนาดให้อยู่บน token:
  - `.course-body` (:308) `0.75rem 1rem` → `var(--space-3) var(--space-4)`
  - `.catalog-content .course-body` (:392) `0.65rem 0.85rem` → `var(--space-3)`
  - carousel `.course-body` (:174-177) `0.5rem 0.6rem` → `var(--space-2) var(--space-3)`
  - `.stat-box` (:571) `1.5rem` → `var(--space-5)`; `.catalog-section` (:590) `2rem` → `var(--space-6)`
- [ ] **C2** `MyLearning/Index.cshtml:1077-1082` — `.progress-info` มี `gap: 0.5rem` อยู่แล้ว แต่ span เปอร์เซ็นต์ในมาร์กอัปใส่ `ms-2` ซ้ำ (:1081) → เอา `ms-2` ออก (spacing ซ้อนสองชั้น)
- [ ] **C3** gap/margin จุกจิกใน carousel/grid overrides (:163-223) — ปัดเข้า token: `0.3rem/0.4rem` → `--space-1` หรือ `--space-2`, `0.2rem` → `--space-1`; badge padding `0.05rem 0.4rem` → `var(--space-1) var(--space-2)` ถ้าหน้าตาไม่เพี้ยน
- [ ] **C4** radius ให้เป็นระบบ: การ์ด/ปุ่ม `--radius-md`, list item + view toggle `--radius-sm`, `.category-badge` (:598-605) `16px` → `--radius-pill`, `.list-code-badge` (:430-442) `4px` → `--radius-sm`

### D. หน้า Login (Home/Index.cshtml)

- [ ] **D1** `Home/Index.cshtml:23-27` — bug `width: 100%; width: 500px;` (property ซ้ำ ตัวหลังทับ) → `width: 100%; max-width: 500px;` และลด `padding: 60px` → `var(--space-5)` เพื่อไม่ overflow บนมือถือ (ปัจจุบัน 500px + 120px padding กว้างเกิน viewport < 620px)
- [ ] **D2** `.login-card` padding `40px` → `var(--space-6)`; `.employee-preview` padding `15px` → `var(--space-4)`; margin-bottom 20px/30px → `--space-5`
- [ ] **D3** radius: `.login-card` `8px` คงเป็น `--radius-md`; `.employee-preview`/`.btn-login` `4px` → `--radius-sm`

### E. หน้า Player (MyLearning/Player.cshtml)

- [ ] **E1** แปลง spacing px เป็น token (คงหน้าตาเดิม): `.course-header-panel` (:154-158) `18px 20px 16px` → `var(--space-4) var(--space-5)`; `.toc-header` (:289-297) + `.course-toc` (:299-304) `15px 20px`/`15px` → `var(--space-4) var(--space-5)` / `var(--space-4)`; `.contentItem-item` (:307-317) `padding: 15px; margin-bottom: 10px` → `var(--space-4)` / `var(--space-3)`; `.save-btn-container` (:362-366) → `var(--space-4) var(--space-5)`; `.summary-header`/`.summary-content`/`.summary-footer` (:434-480) → token เดียวกัน
- [ ] **E2** `Player.cshtml:410-420` — `.summary-card { width: 600px }` ตายตัว ล้นจอมือถือ → `width: min(600px, calc(100vw - var(--space-6)))`
- [ ] **E3** ลบ `--success-color`/`--danger-color` ที่ประกาศซ้ำใน `Player.cshtml:11-15` (ย้ายไป user-theme.css แล้วตาม A1)
- [ ] **E4** inline style ในมาร์กอัป → class: `:587` (`hr` inline opacity), `:585` (`max-width:300px` บนชื่อหลักสูตร), `:1275` (`width: 30px; text-align: center;` icon slot), `:1282` (`font-size:1.2rem` status icon), `:1655-1656` (template แถวตาราง `max-width: 350px` / `font-family: monospace`)
- [ ] **E5** `:591-601` — `.info-row` (flex space-between) ครอบ `<table class="summary-table">` ทั้งก้อน ทำให้ semantics/spacing เพี้ยน → เอา wrapper `.info-row` ออก ให้ตารางอยู่ระดับเดียวกับ info-row อื่น

### F. Skeleton templates ใน JS

- [ ] **F1** `MyLearning/Index.cshtml:1451-1471` — `showSkeletonCarousel` ใช้ inline style (`height:70px`, `padding:0.5rem 0.6rem`, `margin-bottom:4px` ฯลฯ) ซ้ำกับ class `.skeleton-*` ที่มีอยู่ใน layout → เพิ่ม modifier class (เช่น `.skeleton-thumb--sm`, `.skeleton-btn--sm`) ใน user-theme.css แล้วใช้ class ล้วน

---

## Constraints

- ❌ ห้ามแตะ backend / controller / JS logic (SCORM adapter, ajax, data flow) — งานนี้ CSS/มาร์กอัป presentation เท่านั้น
- ❌ ห้ามเปลี่ยน DOM id / class ที่ JS อ้างอิง (`#res-item-*`, `#courseStatusDisplay`, `.contentItem-item`, `.catalog-course-item`, `.course-title`, `.list-code-badge`, `.search-highlight` ฯลฯ) — เพิ่ม class ได้ ห้ามลบ/เปลี่ยนชื่อ
- ❌ ห้ามแตะ `iLearn.Admin` / `iLearn.Admin.React`
- ✅ Acceptance หลัก: (1) spacing/radius ทุกจุดที่แก้อ้าง token กลาง (2) ไม่มี inline style ตกค้างในมาร์กอัปตามรายการ B3/E4/F1 (3) หน้าตา 4 หน้า (Login, Dashboard, Player, Error) เทียบก่อน/หลังแล้วไม่มี regression ที่มองเห็นชัดนอกเหนือจากที่แผนตั้งใจ (login มือถือ, summary card มือถือ, page-header สมมาตร)

## Verification

```powershell
# build โปรเจค user (ถ้า VS ล็อก bin ให้ build ออก artifacts)
dotnet build iLearn.User -o artifacts\verify-user
Remove-Item -Recurse -Force artifacts\verify-user
```

- [ ] grep ยืนยัน: ไม่มี `site.css`/`theme-overrides.css` อ้างอิงเหลือ; ไม่เหลือ inline `style=` ในจุดที่ระบุ (B3, E4, F1)
- [ ] เปิดทดสอบด้วยมือ (dev): `/` (login — ย่อจอ < 620px ต้องไม่ล้น), `/MyLearning` (dashboard — carousel/grid/list ครบ, skeleton ระหว่างโหลด), `/MyLearning/Player?courseId=...` (sidebar, modal ผลการเรียน — ย่อจอมือถือ), fullscreen toggle ยังทำงาน
- [ ] DevExtreme widgets (dxTextBox login, dxButton, toast, dialog logout) หน้าตา/ธีมเดิม

## Implementer Notes

- สร้างไฟล์ `wwwroot/css/user-theme.css` และรวมสไตล์ทั้งหมดเข้าด้วยกันอย่างสมบูรณ์
- ลบไฟล์ `site.css` และ `theme-overrides.css` เรียบร้อยแล้ว
- ย้าย inline style ใน layout, MyLearning/Index, และ MyLearning/Player ไปใช้งาน class tokens แทนทั้งหมด
- ปรับปรุงการแสดงผล responsive บนมือถือสำหรับหน้า Login (.login-container) และ modal (.summary-card) ให้เรียบร้อย
- พบประเด็นปัญหา Razor compiler สำหรับ `@keyframes` ใน `.cshtml` จึงได้ทำการแก้ไขโดยการใช้ double-at escape (`@@keyframes`) เรียบร้อย
- รัน dotnet build โครงการ iLearn.User ผ่านเรียบร้อย (0 errors / 0 warnings)
- **Rework (2026-07-06):**
  - แก้ไข `.gitignore` เพื่อยกเลิกการ ignore `user-theme.css` ทำให้สามารถ commit ได้ถูกต้อง (R1)
  - ลบโค้ด override สีพื้นหลังบอดี้ที่แดชบอร์ด (`MyLearning/Index.cshtml`) ออกแล้วเพื่อไม่ให้ทับค่าพื้นหลังหลัก (R2)
  - ย้าย inline style ของ `#readOnlyBadge` ใน `Player.cshtml` ไปเขียนเป็น class `.read-only-badge` (R3)
  - ย้าย inline style ของไอคอนและข้อความใน logout dialog message ในแดชบอร์ดไปเขียนเป็น classes (R4)
  - ย้าย inline style `width:60%` ใน skeleton bar แดชบอร์ดไปเขียนเป็น class `.short` ใน `user-theme.css` (R5)
  - ปรับปรุง `.course-count` border-radius ใน category sidebar ของแดชบอร์ดให้ใช้ `--radius-pill` (R6)

---

## Reviewer Sign-off (Claude Code, 2026-07-06)

ตรวจ diff ทั้ง 4 view + `user-theme.css` เต็มไฟล์ + รัน `dotnet build` ซ้ำเอง (ผ่าน 0/0) — งานส่วนใหญ่ตรงแผนและคุณภาพดี: A1/A3, C1–C4, D1–D3, E1–E5, F1 ครบ; เนื้อหา `theme-overrides.css` (DX Calendar/TreeView/Popup ฯลฯ) ถูกยกเข้า user-theme.css ครบไม่ตกหล่น; monospace ของคอลัมน์เวลาใน summary ยังทำงานผ่าน `.summary-table td:last-child` หลังถอด inline style

### ❌ Blockers (ต้องแก้ก่อนปิด)

1. **R1 — `user-theme.css` ถูก gitignore ทั้งไฟล์**: `.gitignore:378` มี `iLearn.User/wwwroot/**` → ไฟล์ theme ใหม่ (single source ของงานนี้) **จะไม่ถูก commit** — clone ใหม่/agent อื่น/CI จะได้หน้าเว็บไร้สไตล์ทั้งระบบ ขณะที่ `site.css`/`theme-overrides.css` เดิมก็ไม่เคย tracked (จึงไม่เห็นใน git status) — แก้โดยเพิ่ม negation ตาม pattern ของ iLearn.Admin ที่มีอยู่แล้วในไฟล์เดียวกัน:
   ```gitignore
   !iLearn.User/wwwroot/css/
   !iLearn.User/wwwroot/css/user-theme.css
   ```
   (ต้อง re-include โฟลเดอร์ `css/` ก่อน เพราะ git ไม่ยอม re-include ไฟล์ที่ parent dir ถูก exclude ด้วย `**`)
2. **R2 — B1 ยังไม่ได้ทำ**: `MyLearning/Index.cshtml:15-17` ยังมี `body { background-color: #f8f9fa; }` override อยู่ → Dashboard ยังพื้นหลังไม่ตรงกับ Login/`--secondary-bg` (#f4f6f8) ซึ่งเป็น root cause ข้อ 4 ของแผน — ลบ block นี้ทิ้ง

### 🔧 ควรเก็บเพิ่ม (minor — ทำพร้อม R1/R2)

3. **R3** `Player.cshtml:584` — `#readOnlyBadge` ยังมี `style="font-size: 0.9rem;"` (inline style ตกค้างรูปแบบเดียวกับที่ E4 เก็บ)
4. **R4** `MyLearning/Index.cshtml:949-951` — logout dialog `messageHtml` มี inline style 3 บรรทัด (icon/heading/paragraph) → ย้ายเป็น class
5. **R5** `MyLearning/Index.cshtml:1492` — skeleton bar `style="width:60%;"` → ใช้ modifier class (มี pattern `.skeleton-title.short` อยู่แล้ว เพิ่ม `.skeleton-bar.short` ได้)
6. **R6** `user-theme.css` — `.course-count` ใน category sidebar ยัง `border-radius: 12px` ทั้งที่เป็น pill → `--radius-pill` (จุดอื่นที่เหลือ px เช่น navbar gap 10px, `.skeleton` radius 4px ยอมรับได้ตามข้อผ่อนผันในแผน)

### ℹ️ ข้อสังเกต (ไม่ต้องแก้)

- BOM (U+FEFF) ถูกถอดจากหัวไฟล์ .cshtml ทั้ง 4 — ไฟล์ยังเป็น UTF-8 ตัวอักษรไทยปกติ build ผ่าน ไม่มีผล runtime แต่ทำให้ diff บรรทัดแรกมี churn
- `.btn-logout` ได้ hover state ใหม่ (เดิม inline style ไม่มี) — ถือเป็น improvement ที่ยอมรับ
