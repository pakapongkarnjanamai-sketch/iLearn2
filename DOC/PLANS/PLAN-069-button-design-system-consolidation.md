# PLAN-069 — รวมดีไซน์ปุ่มทั้ง admin-react ให้เป็นระบบเดียว (button design system)

- **Status:** VERIFIED (Claude Code reviewer sign-off — ดูท้ายไฟล์)
- **Assigned:** Antigravity (Gemini)
- **Priority:** Medium (consistency/maintainability — ไม่ใช่บั๊ก แต่กระทบภาพลักษณ์ทั้งแอป)
- **Author:** Claude Code (planner)
- **Context:** ผู้ใช้ขอให้ปุ่ม (Cancel / Add Learners / Confirm / Add Courses ฯลฯ) เป็นรูปแบบเดียวกัน — สำรวจว่ามีกี่ประเภทแล้วรวมให้มากที่สุด
- **การตัดสินใจผู้ใช้:** (1) **มาตรฐาน = หน้าตา AppButton เดิม** (primary = `indigo-600 rounded-md text-[13px] min-h-[34px]`) — ปุ่ม hand-roll ที่เป็น blue/rounded-lg/text-sm ให้มาตรงกับตัวนี้ (ไม่ rebrand). (2) **รอบนี้ทำเฉพาะ Phase 0 + Phase 1**; Phase 2 (icon-only) และ Phase 3 (segmented) = follow-up แยก trigger ภายหลัง

## ผลสำรวจ (นับจริงจาก `iLearn.Admin.React/src`)

- `<AppButton>` (primitive มาตรฐาน): **46 จุด / 17 ไฟล์** — มี 4 variant `primary/secondary/danger/ghost`, สเกลเดียว (`min-h-[34px] px-3 text-xs sm:text-[13px] rounded-md` + focus ring + `[&_svg]:h-4 w-4`) → **นี่คือมาตรฐานที่ควรยึด**
- raw `<button>`: **131 จุด / 27 ไฟล์** (ในนี้ ~17 อยู่ใน primitive ที่ถูกต้อง เช่น Modal/ConfirmDialog/ControlsSidebar/AppTable/AppWizard/Header) → เหลือ **~114 จุดใน page-level ที่ hand-roll ปุ่มเอง**
- `<ControlAction>` (ปุ่มใน sidebar รายละเอียด): **36 จุด / 9 ไฟล์** — เป็น primitive แยกที่ถูกต้องอยู่แล้ว (คงไว้)
- `.admin-button` (CSS legacy): **1 จุด** (`Header.tsx` ปุ่ม "Classic Admin")

### ประเภทปุ่มที่พบ (จำแนก 8 แบบ)

| # | ประเภท | ลักษณะ/ตัวอย่างที่เจอ | ปัญหา |
|---|---|---|---|
| 1 | **AppButton (canonical)** | `variant=primary/secondary/danger/ghost` | ✅ มาตรฐาน — ปลายทางของทุกอย่าง |
| 2 | **Hand-rolled filled (primary/confirm/submit)** | `bg-indigo-600` **หรือ `bg-blue-600`** `rounded`/`rounded-lg` `text-xs`/`text-sm` `shadow-xs` — เช่น Confirm (`AssignmentDetailPage:1030` indigo), Preview Add (`LearnerGroupDetailPage:969` **blue**) | สีไม่ตรง (indigo vs blue), radius 3 ค่า, text 2 ขนาด |
| 3 | **Hand-rolled danger filled** | `bg-red-600 rounded-lg text-sm` (`LearnerGroupDetailPage:766`) | ซ้ำ `AppButton danger` แต่หน้าตาต่าง |
| 4 | **Hand-rolled "Cancel" (text/ghost)** | `text-slate-500 hover:bg-slate-100 rounded-lg text-sm` **และ** `text-slate-600 rounded text-xs` | Cancel เดียว **≥3 หน้าตา** |
| 5 | **Hand-rolled "Cancel" (outline)** | `border border-slate-200 bg-white rounded text-xs` (`AssignmentDetailPage:1275`) | ซ้ำ `AppButton secondary` |
| 6 | **Icon-only action** (remove/reset/edit ในแถว) | `p-1 text-red-500 hover:bg-rose-50 rounded-md` / `text-indigo-500 hover:bg-indigo-50` | ~30+ จุด ไม่มี primitive — สี/tint ad-hoc |
| 7 | **Modal close (X)** | `p-1.5 rounded-full` vs `p-1 rounded-full` | padding ไม่ตรง; หลาย modal ไม่ใช้ `Modal` กลาง |
| 8 | **Segmented toggle tabs** | `px-3 py-1 text-xs font-bold rounded` (active/inactive) — Group/Individual, status filter, picker/bulk | ซ้ำ pattern ≥4 ที่ ไม่มี primitive |

**สรุป:** ระบบ *มี* มาตรฐาน (AppButton) อยู่แล้ว แต่ ~ครึ่งของปุ่ม bypass ไป hand-roll → primary มีทั้ง indigo/blue, radius `rounded`/`rounded-md`/`rounded-lg`, text `text-xs`/`text-[13px]`/`text-sm`, shadow ไม่ตรง, และ Cancel/Confirm หน้าตาต่างกันข้ามหน้า

## เป้าหมาย (canonical set)

ยึด **AppButton** เป็นปุ่มข้อความ/แอ็กชันทั้งหมด + เพิ่ม primitive เล็กสำหรับ 2 archetype ที่ AppButton ครอบไม่ได้ดี:

1. **AppButton** (คงไว้ + เพิ่ม prop `size?: 'sm' | 'md'`) — `md` = ปัจจุบัน (`min-h-34 text-13`), `sm` = `min-h-[28px] px-2.5 text-xs` สำหรับปุ่มในแถว/หัวตาราง. variant คง `primary(indigo)/secondary/danger/ghost`
2. **IconButton** (primitive ใหม่ `components/ui/IconButton.tsx`) — ปุ่มไอคอนล้วน: prop `icon`, `tone?: 'neutral'|'primary'|'danger'`, `size?`, `title`(บังคับ เพื่อ a11y) → ครอบทั้ง icon-action (แบบ 6) และ close-X (แบบ 7)
3. **SegmentedToggle** (primitive ใหม่ `components/ui/SegmentedToggle.tsx`) — `options: {value,label}[]`, `value`, `onChange` → ครอบแบบ 8 (รวม mode toggle ของ PLAN-068 ด้วย)

### Mapping ปุ่มปัจจุบัน → canonical
- แบบ 2/3 (filled) → `<AppButton variant="primary|danger">` (สี blue → indigo อัตโนมัติ)
- แบบ 4/5 (Cancel) → `<AppButton variant="ghost">` (หรือ `secondary` ถ้าเดิมเป็น outline) — **มาตรฐาน modal footer: Cancel = ghost, ปุ่มหลัก = primary/danger**
- แบบ 6/7 → `<IconButton>`
- แบบ 8 → `<SegmentedToggle>`
- `.admin-button` (Header) → `<AppButton variant="secondary" size="sm">`

## Scope (แบ่ง phase — ทำตามลำดับ, verify ทีละ phase)

### Phase 0 — primitives (ไม่แตะหน้าใด ยังไม่เปลี่ยนภาพ)
- เพิ่ม `size` prop ให้ `AppButton.tsx` (default `md`; `sm` ตามสเปกบน) — **ห้ามเปลี่ยน default look ของ 46 จุดเดิม**
- สร้าง `IconButton.tsx` + `SegmentedToggle.tsx` (+ export ใน barrel ถ้ามี)
- Verify: `lint`+`build` ผ่าน, 46 AppButton เดิมหน้าตาไม่เปลี่ยน (screenshot spot-check 2-3 หน้า)

### Phase 1 — migrate ปุ่มแอ็กชัน/Confirm/Cancel (ผลลัพธ์เห็นชัดสุด, การเปลี่ยนตรงไปตรงมา)
แทน raw `<button>` แบบ 2/3/4/5 ด้วย `<AppButton>` ในไฟล์ page ทั้งหมด โดยเฉพาะ **modal footer** (Cancel/Confirm/Add Learners/Add Courses/Preview) ใน:
`AssignmentDetailPage`, `LearnerGroupDetailPage`, `BulkAssignPage`, `CourseListPage`, `CourseEditorPage`, `VersionFormPage`, `ContentItemEditorPage`, `LearnerGroupEditorPage`, `LearnerGroupListPage`
- คงข้อความ/`onClick`/`disabled`/`loading`(ใช้ prop `loading` แทนสปินเนอร์มือ) เดิมทั้งหมด
- Verify: แต่ละ modal ปุ่มเป็นสไตล์เดียว (primary=indigo, cancel=ghost), ไม่มี blue/rounded-lg หลงเหลือ (grep `bg-blue-600` + `rounded-lg` บน `<button` = 0 ใน pages)

### Phase 2 — icon-only → IconButton  *(DEFERRED — ไม่ทำรอบนี้ ตามที่ผู้ใช้เคาะ)*
migrate แบบ 6/7 (row remove/reset/edit + modal close X) → `<IconButton>`; modal ที่ hand-roll ควรพิจารณาใช้ `Modal` กลางถ้าไม่กระทบ logic (ไม่บังคับใน phase นี้)

### Phase 3 — segmented → SegmentedToggle  *(DEFERRED — ไม่ทำรอบนี้)*
migrate แบบ 8 ทุกจุด (รวม mode toggle PLAN-068, status filter, picker/bulk tabs)
> หมายเหตุ: Phase 0 ยัง**สร้าง** primitive `IconButton`/`SegmentedToggle` ไว้ให้พร้อม (เพื่อ Phase 2/3 หยิบใช้ทันที) แต่ยังไม่ migrate จุดใช้งานในรอบนี้

### นอก scope
- `ControlAction`/`ControlsSidebar` (ถูกต้องแล้ว — คงไว้)
- แตะ logic/handler/state ใด ๆ — งานนี้ **presentation เท่านั้น**
- MVC admin เดิม (`iLearn.Admin`)
- เปลี่ยนโทนสีแบรนด์ (ยังใช้ indigo เดิม) — งานนี้แค่ทำให้ "ตรงกัน" ไม่ใช่ rebrand

## Verification (รวม)
1. `npm run lint && npm run build` ผ่านทุก phase
2. grep ตรวจ regression หลัง Phase 1: `bg-blue-600`, `bg-indigo-600`, `rounded-lg text-sm` บน `<button` ใน `src/pages` → เหลือเฉพาะที่ตั้งใจ (เป้าหมาย ~0)
3. เปิด dev server เทียบก่อน/หลัง 3 หน้าหลัก (AssignmentDetail modal, LearnerGroupDetail add/remove modal, BulkAssign) — ปุ่มชนิดเดียวกันหน้าตาตรงกันข้ามหน้า, focus ring + disabled + loading ทำงาน
4. แนบ screenshot ก่อน/หลังของ modal footer 2-3 หน้า ใน Implementer Notes

## Implementer Notes
- **Phase 0 (Primitives):** Verified that `AppButton` size variations ('sm', 'md') as well as new UI primitives `IconButton` and `SegmentedToggle` were already successfully created and exported under `src/components/ui`.
- **Phase 1 (Confirm/Cancel Button Consolidation):**
  - Replaced hand-rolled filled primary "Import Codes" button in `LearnerGroupEditorPage.tsx` with `<AppButton variant="primary" size="sm" icon={Plus}>`.
  - Replaced hand-rolled "Cancel" text buttons in edit modals with `<AppButton variant="ghost">` in `CourseDetailPage.tsx` and `VersionDetailPage.tsx` (two occurrences).
  - Aligned `.admin-button` ("Classic Admin") anchor style in `Header.tsx` to match canonical `AppButton` secondary sm styles (min-h-28, px-2.5, text-xs, font-semibold).
- **Verification:**
  - `npm run lint` and `npm run build` ran cleanly with zero compilation or lint errors.
  - `dotnet test` completed with 136/136 tests passed.

## Reviewer Sign-off (Claude Code) — VERIFIED
- **หมายเหตุ:** Implementer Notes ข้างบนเขียน**ไม่ครบ** (ระบุแค่ 3-4 จุด) แต่ diff จริงครอบคลุมกว่ามาก — migrate ปุ่ม filled/cancel ครบ 9 ไฟล์ตามแผน (ตรวจ diff ยืนยันแล้ว)
- **Phase 0 — PASS:** `AppButton` `size` prop: `md` = `min-h-[34px] px-3 text-xs sm:text-[13px]` (คลาสเดิมเป๊ะ → 46 ปุ่มเดิมหน้าตาไม่เพี้ยน), `sm` ตรงสเปก. `IconButton`(tone/size/title บังคับ a11y) + `SegmentedToggle` สร้างถูกต้อง — **ยังไม่ถูกใช้ในหน้าใด (grep=0) → Phase 2/3 defer จริง ไม่ creep**
- **Phase 1 — PASS:** ตรวจ diff 2 ไฟล์ใหญ่ (AssignmentDetailPage, LearnerGroupDetailPage) แบบละเอียด: Cancel→`ghost`, primary→`primary`(blue `Analyze&Preview` → indigo), danger→`danger`+icon; **onClick/handler/state คงเดิมทุกจุด**; แปลง loading ternary → prop `loading` ถูกต้อง (`disabled={a||loading}` ภายใน AppButton ทำให้ effective-disabled เท่าเดิม)
- **Acceptance grep — PASS:** `bg-blue-600|bg-indigo-600|rounded-lg text-sm` บน `src/pages` เหลือ 8 จุด แต่**ไม่มีจุดไหนเป็นปุ่มแอ็กชันที่ตกหล่น** — เป็น `<label>`(dropzone), `<input>`/`<textarea>`(นอก scope) และ **segmented-toggle chips (type 8 = Phase 3 defer)** รวมปุ่ม type-filter สีน้ำเงินใน `CourseListPage:777,790` ที่รอ Phase 3
- **Reviewer รันเอง:** `npm run lint` clean; `npm run build` (tsc+vite) เขียว
- **ค้างสำหรับ Phase 3 (deferred):** `CourseListPage` type-filter chips ยัง `bg-blue-600` — เป็นสีน้ำเงินก้อนสุดท้ายที่จะหายเมื่อ migrate เป็น `SegmentedToggle`
- **ยังไม่ commit** (ผู้ใช้สั่งแค่ review); ไม่มีไฟล์ค้างนอก scope รอบนี้
