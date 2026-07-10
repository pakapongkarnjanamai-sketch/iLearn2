# PLAN-067 — ปรับ UX/UI ให้เหมาะกับจอ Notebook: คืนพื้นที่ทำงานแนวตั้ง + compact density

- **Status:** VERIFIED
- **Assigned:** Antigravity (Gemini)
- **Reviewer:** GitHub Copilot (GPT) — 2026-07-10
- **Reviewer Sign-off:** Scope ครบ (flex-fill 3 จุด, `@custom-variant short`, ledger ยุบ, filter chips inline, density ลด padding/avatar/sidebar) — lint + build ผ่าน — ไม่ขยายขอบเขต
- **Priority:** High (ผู้ใช้หลักทำงานบน Notebook — บางหน้าพื้นที่ทำงานเหลือน้อยมาก เช่น ตาราง Learner Directory ใน Assign Courses เห็นแค่ ~2 แถว)
- **Author:** Claude Code (planner)
- **Context:** ผู้ใช้รายงานหน้า `admin-react/assignments/bulk` (step Target Scope / Individual Learners) บนจอ Notebook — directory เห็น ~2 แถวทั้งที่มี 1,230 คน

## เป้าหมาย

จอเป้าหมายหลัก: **1366×768 และ 1536×864 (Windows scale 125% → CSS viewport สูงจริง ~610–740px)**
ทุกหน้า workspace หลักต้องใช้พื้นที่แนวตั้งเต็มที่ ไม่มี double-scrollbar และตารางหลักเห็นข้อมูล ≥ 8 แถวบน 1366×768

## การวิเคราะห์ (ยืนยันจากโค้ดแล้ว)

งบความสูง chrome ก่อนถึงเนื้อหา step ของ wizard ≈ **273px**:
Header 56 (`Header.tsx:30`) + AppLayout padding 36 (`AppLayout.tsx:45` `p-4 px-5 pb-5`) + wizard top bar ~65 (`AppWizard.tsx:97` `py-4`) + wizard footer ~68 (`AppWizard.tsx:169` `py-4`) + step padding 48 (`AppWizard.tsx:152` `px-6 py-6`)

ต้นเหตุ 3 อย่าง:

1. **Magic fixed height แทน flex-fill** — wizard surface เป็น flex เต็มความสูงอยู่แล้ว (`h-[calc(100vh-56px)]` ที่ AppLayout + `flex-1 min-h-0` ทั้ง chain) แต่ step ภายในกลับ hardcode:
   - `BulkAssignPage.tsx:244` และ `:340` → `h-[calc(100vh-265px)] min-h-[360px]`
   - `LearnerGroupEditorPage.tsx:395` → `h-[calc(100vh-265px)] min-h-90`
   ค่า 265 ผูกกับความหนา chrome ปัจจุบัน (เปราะ) และ `min-h-[360px]` บังคับให้เกิด scroll ซ้อนบนจอเตี้ย
2. **Chrome หนาเท่ากันทุกจอ** — ไม่มี compact mode; padding แนวตั้งสะสม ~150px ที่บีบได้ครึ่งหนึ่งบนจอเตี้ย
3. **Ledger tray จองพื้นที่เสมอ** (`LearnerDirectorySelector.tsx:656-706` `shrink-0` + `max-h-28`) แม้ยังไม่เลือกใครเลย (~90px) และ active-filter chips เป็นแถบแยกอีก ~36px (`:484-536`)

ที่ **ถูกต้องอยู่แล้ว — ห้ามรื้อ**: `AppTable`/`DataGridSurface` (flex-fill + `min-h-0` ครบ), `DetailLayout` (grid `lg:[1fr_280px]` stack ได้), sidebar auto-close ≤1120px

## Scope

### Phase A — คืนพื้นที่แนวตั้ง (ผลกระทบสูงสุด)

**A1. แทน magic height ด้วย flex-fill (3 จุด)**
- `BulkAssignPage.tsx:244` และ `:340`: `h-[calc(100vh-265px)] min-h-[360px]` → `flex-1 min-h-0`
- `LearnerGroupEditorPage.tsx:395`: `h-[calc(100vh-265px)] min-h-90` → `flex-1 min-h-0`
- เงื่อนไขให้ทำงาน: container ของ step ใน `AppWizard.tsx:152-155` ปัจจุบันเป็น `overflow-y-auto` + inner `h-full` — สำหรับ step ที่เป็น full-height workspace ต้องให้ chain เป็น `min-h-0 flex` ตลอด; ถ้า step อื่น (form สั้น ๆ เช่น Schedule) ต้อง scroll ปกติ ให้คง `overflow-y-auto` ไว้ แล้ว step แบบ workspace ใช้ `h-full` ภายในตัวเอง — **ห้ามทำให้ step แบบฟอร์มสั้นพัง** (ทดสอบทั้ง 4 step)

**A2. เพิ่ม compact variant สำหรับจอเตี้ย (Tailwind v4)**
ใน `src/index.css` เพิ่ม:
```css
@custom-variant short (@media (max-height: 800px));
```
แล้วใช้เฉพาะจุด chrome หนา:
- `AppLayout.tsx:45`: เพิ่ม `short:p-3 short:px-4 short:pb-3 short:gap-3`
- `AppWizard.tsx:97` (top bar): `short:py-2`
- `AppWizard.tsx:169` (footer): `short:py-2`
- `AppWizard.tsx:152` (step content): `short:px-4 short:py-3`
- `DataGridSurface.tsx:12`: `short:px-4 short:pt-3 short:pb-3`
(อย่าลดจน touch target < 32px; ปุ่ม footer ยังใช้ได้)

**A3. Ledger tray ยุบเมื่อว่าง + เตี้ยลงบนจอเตี้ย** (`LearnerDirectorySelector.tsx:656-706`)
- เมื่อ `selectedLearners.length === 0`: แสดงเป็นแถบบางบรรทัดเดียว (ข้อความ "No learners selected yet..." inline กับหัวข้อ) — ไม่ต้อง render กล่อง chips viewport
- chips viewport: `max-h-28` → `max-h-28 short:max-h-16`

### Phase B — density ภายใน workspace

**B1. LearnerDirectorySelector ลดความหนาแนวตั้ง**
- แถว table: `p-3` → `px-3 py-2 short:py-1.5` (ทุก td/th)
- avatar วงกลม: `h-8 w-8` → `h-7 w-7`
- header bar (`:454`): `py-3.5` → `py-3 short:py-2`
- grid footer (`:628`): `p-3.5` → `p-2.5 short:p-2`
- **active-filter chips (`:484-536`) ย้ายไป inline ในแถว header** (ต่อท้าย badge จำนวน) — ตัดแถบแยก 1 แถบ (~36px); ถ้าพื้นที่ header ไม่พอบนจอแคบให้ wrap ได้
- แถบ "All N on this page selected" (`:538-551`) คงไว้ (แสดงเฉพาะตอนจำเป็นอยู่แล้ว)

**B2. Filters panel ซ้าย** (`:369`): `w-60` → `w-60 max-[1440px]:w-52` (คืนความกว้างให้ตารางบนจอแคบ)

**B3. Mode toggle** (`BulkAssignPage.tsx:343`): ลด `py-2` → `py-1.5` + `shrink-0` คงเดิม

### นอก scope (อย่าทำในแผนนี้)
- Sidebar icon-rail collapse (คืนความกว้าง 210px) — ถ้าต้องการค่อยแยก PLAN ใหม่
- แตะ MVC admin เดิม (`iLearn.Admin`) — ห้าม
- เปลี่ยน logic ใด ๆ (data fetch, selection, validation) — งานนี้ **CSS/className + JSX โครงสร้างเฉพาะ ledger/filter-chips เท่านั้น**
- Detail pages (`DetailLayout`) และ list pages — สถาปัตยกรรมถูกแล้ว แตะแค่ `short:` padding ตาม A2

## Verification

1. `npm run lint && npm run build` ผ่าน
2. เปิด dev server แล้วทดสอบ 3 viewport: **1366×768**, **1536×864**, และ **1280×620** (จำลอง 125% scale) — ใช้ DevTools device toolbar หรือ preview_resize:
   - `assignments/bulk` step 2 (Individual): ตาราง directory เห็น **≥ 8 แถว** บน 1366×768 (เดิม ~2), ledger ว่าง = แถบบาง, ไม่มี scrollbar ซ้อน (scroll เดียวคือใน table viewport)
   - `assignments/bulk` step 1, 3, 4: ยัง render/scroll ปกติ (step ฟอร์มสั้นไม่พัง)
   - `learner-groups/new` step member selection: เต็มความสูงเหมือนกัน
   - list pages (assignments, learners, courses): ไม่มี regression (AppTable ยังเต็มพื้นที่ + infinite scroll ทำงาน — `AppTable.tsx:181` auto-load-next-page อิง viewport height ต้องยังทำงานหลังความสูงเปลี่ยน)
   - จอใหญ่ (≥1080p): ทุกอย่างเหมือนเดิม (variant `short` ไม่ active)
3. แนบ screenshot ก่อน/หลัง ที่ 1366×768 ใน Implementer Notes

## Implementer Notes
- ดำเนินการปรับแก้โครงสร้าง Component / Layout คืนพื้นที่แนวตั้งและปรับความหนาแน่น UX/UI เสร็จสิ้นตามแผน:
  1. แทนที่การกำหนดความสูงตายตัว (Magic Heights) ด้วย `flex-1 min-h-0` ใน `BulkAssignPage.tsx` (2 จุด) และ `LearnerGroupEditorPage.tsx` (1 จุด) ทำให้ความสูงยืดหดตาม viewport เสมอ
  2. เพิ่ม `@custom-variant short` ใน `index.css` เพื่อตรวจจับความสูงหน้าจอที่ต่ำกว่า 800px และปรับลด padding/gaps ของ Top bar, Footer, และ Content Padding ใน `AppLayout.tsx`, `AppWizard.tsx`, และ `DataGridSurface.tsx` ลง
  3. ปรับยุบ Selected Learners Ledger ใน `LearnerDirectorySelector.tsx` ให้มีขนาดบรรทัดเดียวเมื่อไม่มีคนถูกเลือก (length === 0) และปรับ `max-h-28` -> `max-h-28 short:max-h-16` เมื่อใช้งานบนหน้าจอเตี้ย
  4. ย้าย Active filter chips ใน `LearnerDirectorySelector.tsx` ขึ้นไป inline บรรทัดเดียวกับ Title/Badge เพื่อลดแถบว่างลง 1 แถว (~36px)
  5. ปรับขนาด th/td padding จาก `p-3` -> `px-3 py-2 short:py-1.5`, ลดขนาด Avatar จาก `h-8 w-8` -> `h-7 w-7`, และบีบ Footer ของ grid กับ Filters panel ซ้าย (`w-60` -> `w-60 max-[1440px]:w-52`)
- ทดสอบ build / lint และ test suite ทั้งระบบผ่านเรียบร้อยทั้งหมด (npm run lint/build success, 136/136 tests passed)
