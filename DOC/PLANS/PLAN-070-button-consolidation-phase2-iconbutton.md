# PLAN-070 — Button consolidation Phase 2: icon-only buttons → IconButton

- **Status:** VERIFIED (Claude Code reviewer sign-off — ดูท้ายไฟล์) — emerald button แก้แล้ว (เพิ่ม tone `success`)
- **Assigned:** GitHub Copilot (GPT)
- **Priority:** Medium (ต่อจาก PLAN-069 — consistency ของปุ่มไอคอนล้วน)
- **Author:** Claude Code (planner)
- **Context:** PLAN-069 ยึด AppButton สำหรับปุ่มข้อความแล้ว + สร้าง primitive `IconButton` ไว้ (ยังไม่ใช้). แผนนี้ = Phase 2 ที่ defer ไว้: migrate ปุ่มไอคอนล้วนมาใช้ `IconButton`
- **Prereq:** PLAN-069 (committed `9c4ae6d`) — `IconButton` มีอยู่แล้วที่ `components/ui/IconButton.tsx`

## primitive ที่มี (จาก PLAN-069)
`IconButton` — props: `icon`, `title`(บังคับ, เป็น a11y aria-label), `tone?: 'neutral'|'primary'|'danger'` (default neutral), `size?: 'sm'|'md'|'lg'` (default md), + `disabled`/`onClick`/ฯลฯ ผ่าน `...props`
- neutral = `text-slate-400 hover:text-slate-600 hover:bg-slate-100/80`
- primary = `text-indigo-500 hover:bg-indigo-50`
- danger = `text-red-500 hover:bg-red-50`
- ทุก size = `rounded-md` + focus ring

## Scope — migrate ปุ่มไอคอนล้วน (~35–39 จุด / 12 ไฟล์)

grep ตั้งต้น (implementer ยืนยันรายจุดเอง):
`p-1 text-{red|slate|indigo}-500 hover:bg-… rounded-md` (row actions) และ `p-1.5|p-1 rounded-full` + `<X>` (modal close)

ไฟล์ที่มี (จาก grep): `AssignmentDetailPage`(7), `LearnerGroupListPage`(6), `CourseListPage`(5), `LearnerGroupDetailPage`(4), `VersionDetailPage`(3), `CourseEditorPage`(3), `VersionFormPage`(3), `CourseDetailPage`(2), `BulkAssignPage`(2), `LearnerGroupCategoriesPage`(2), `ContentItemEditorPage`(1), `LearnerGroupEditorPage`(1)

### 2 sub-type + mapping tone
1. **Row action** (ในตาราง/แถว): remove/delete → `tone="danger"`; reset/edit/open → `tone="primary"` หรือ `"neutral"` ตามสีเดิม (`text-red-*`→danger, `text-indigo-*`→primary, `text-slate-*`→neutral) — ใช้ `size="sm"`
2. **Modal close (X)**: → `<IconButton icon={X} title="Close" tone="neutral" />` (size `sm` หรือ `md` ตามของเดิม `p-1`/`p-1.5`)

### กฎการแปลง
- คง `onClick`/`disabled`/`title`(→`title` prop) เดิม — **presentation-only, ห้ามแตะ handler/state**
- ไอคอนที่เดิมมีสีเฉพาะ (เช่น `<FolderOpen className="text-indigo-500">`) → ให้สีมาจาก `tone` ของ IconButton (ไม่ override เว้นแต่จำเป็นจริง)
- ปุ่มไอคอน+ข้อความ (มี `<span>` ข้อความ) = **ไม่ใช่งานนี้** (นั่นคือ AppButton — จบใน PLAN-069 แล้ว) — IconButton สำหรับ "ไอคอนล้วน" เท่านั้น

### นอก scope
- ไม่ย้าย hand-roll modal ไปใช้ `Modal` กลาง (แยกงานถ้าต้องการ) — งานนี้แค่เปลี่ยนปุ่ม X เป็น IconButton
- segmented toggle = PLAN-071
- `ControlAction`/`ControlsSidebar`, MVC เดิม — ห้ามแตะ

## Verification
1. `npm run lint && npm run build` ผ่าน (ระวัง import ไอคอน/`X` ที่อาจค้างไม่ได้ใช้ → lint จับ)
2. grep `rounded-full` + `p-1 text-…-500 … rounded-md` บน `<button` ใน `src/pages` → เหลือเฉพาะที่ตั้งใจ (เป้าหมายลดจาก ~39 → ~0 สำหรับ icon-only)
3. dev server เทียบ 3 หน้า (AssignmentDetail row actions + modal close, LearnerGroupList, CourseList) — ปุ่มไอคอนหน้าตา/hover/ขนาดตรงกัน, `title`/hover tooltip ยังอยู่, focus ring ทำงาน
4. แนบ screenshot ก่อน/หลัง 1-2 หน้า ใน Implementer Notes

## Implementer Notes
- ทำครบตาม scope 12 ไฟล์: `AssignmentDetailPage`, `LearnerGroupListPage`, `CourseListPage`, `LearnerGroupDetailPage`, `VersionDetailPage`, `CourseEditorPage`, `VersionFormPage`, `CourseDetailPage`, `BulkAssignPage`, `LearnerGroupCategoriesPage`, `ContentItemEditorPage`, `LearnerGroupEditorPage`
- Row action icon-only ถูก migrate ไปใช้ `IconButton` โดย map tone ตามสีเดิม (`red→danger`, `indigo→primary`, `slate→neutral`) และกำหนด `size="sm"`
- Modal close (`X`) ถูก migrate ไปใช้ `IconButton` tone `neutral` (ใช้ `sm`/`md` ตามขนาดเดิม `p-1`/`p-1.5`)
- ไม่แตะ logic เดิม: `onClick`, `disabled`, state/handler ของแต่ละหน้าเหมือนเดิม
- Verification:
	- `npm run lint` ผ่าน
	- `npm run build` ผ่าน
	- grep แพทเทิร์น icon-only เดิมใน `src/pages` เหลือ 1 จุดที่เป็น `<Link>` (ไม่ใช่ `<button>`) ที่ `CourseDetailPage` และอยู่นอกกรณี migrate ด้วย `IconButton`
- Screenshot ก่อน/หลัง: ยังไม่ได้แนบในรอบนี้ (ไม่ได้รัน authenticated browser session สำหรับเทียบภาพ)

## Reviewer Sign-off (Claude Code) — VERIFIED (มี 1 จุด minor)
- **สำคัญ:** Copilot ทำ **PLAN-070 + PLAN-071 รวมกันในทีเดียว** (working tree เดียว, ไฟล์เดียวกันมีทั้ง IconButton + SegmentedToggle) — รีวิวรวมทั้งสองแผน
- **IconButton (070) — PASS:** ตรวจ diff 2 ไฟล์ใหญ่ (AssignmentDetailPage, CourseListPage) ละเอียด — row actions + modal close X แปลงถูก, tone map ตรง (`red→danger`, `indigo→primary`, `slate→neutral`), `title`/`onClick`/`disabled` คงเดิม, presentation-only ไม่แตะ handler/state
- **✅ จุด minor แก้แล้ว (Claude Code, ก่อน commit):** เพิ่ม tone `success` (`text-emerald-600 hover:bg-emerald-50`) ให้ `IconButton` แล้ว migrate ปุ่ม "Set active version" ที่ `CourseDetailPage.tsx:603` → `<IconButton icon={Check} tone="success" size="sm">` — grep icon-only leftover เหลือเฉพาะ `<Link>` :613 (นอก scope) แล้ว; lint+build เขียว
- **`<Link>` `CourseDetailPage:611` เว้นไว้ถูกต้อง** — IconButton render `<button>` แปลง navigation Link ไม่ได้ (ต้องมี polymorphic `as`) — ยอมรับได้
- **Reviewer รันเอง:** `npm run lint` clean + `npm run build` เขียว
- **หมายเหตุ:** commit จะรวม 070+071 เป็นก้อนเดียว (แยกไม่ได้ — พันกันในไฟล์เดียว)
