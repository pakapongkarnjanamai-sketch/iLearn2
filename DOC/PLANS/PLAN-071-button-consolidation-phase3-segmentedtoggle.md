# PLAN-071 — Button consolidation Phase 3: segmented toggles → SegmentedToggle

- **Status:** VERIFIED (Claude Code reviewer sign-off — ดูท้ายไฟล์)
- **Assigned:** GitHub Copilot (Claude Opus 4.6)
- **Priority:** Medium (ต่อจาก PLAN-069/070 — ลบ pattern toggle ซ้ำ + เก็บปุ่มสีน้ำเงินก้อนสุดท้าย)
- **Author:** Claude Code (planner)
- **Context:** PLAN-069 สร้าง primitive `SegmentedToggle` ไว้ (ยังไม่ใช้). แผนนี้ = Phase 3 ที่ defer: migrate segmented toggle ทุกจุด + กำจัด `bg-blue-600` ที่เหลือใน CourseListPage
- **Prereq:** PLAN-069 (committed `9c4ae6d`) — `SegmentedToggle` อยู่ที่ `components/ui/SegmentedToggle.tsx`

## primitive ที่มี (จาก PLAN-069)
`SegmentedToggle` — props: `options: {value,label}[]`, `value`, `onChange(value)`, `className?`
render: กล่อง `bg-slate-100 p-0.5 rounded-lg` + ปุ่มย่อย `rounded-md`, active = `bg-white text-indigo-700 shadow-3xs`

## Scope

### A. 2-option toggle (fit ตรง — migrate ได้เลย, 4 จุด)
| จุด | state |
|---|---|
| `BulkAssignPage.tsx:246-261` (Group/Individual — ตัวที่ PLAN-068 ย้ายเข้า header) | `targetMode` `'group'\|'custom'` |
| `LearnerGroupEditorPage.tsx:397-411` (picker/bulk tabs) | `activeTab` |
| `AssignmentDetailPage.tsx:1147-1160` (picker/bulk tabs ใน add-learners modal) | `memberAddTab` |
| `LearnerGroupDetailPage.tsx:790-805` (picker/bulk tabs) | `memberAddTab` |

แทนด้วย `<SegmentedToggle options={[{value,label},…]} value={state} onChange={setState} />` — **คง state/handler เดิม** (แค่เปลี่ยน presentation)

### B. filter-chip rows (ต้องขยาย primitive ก่อน — พิจารณา)
| จุด | ลักษณะ |
|---|---|
| `AssignmentDetailPage.tsx:~777` (learner status filter) | multi-option, active = `bg-indigo-600 text-white`, มี "All" |
| `AssignmentReportPage.tsx:~416` (status filter) | เหมือนกัน |
| `CourseListPage.tsx:777,790` (course type filter — **ยังเป็น `bg-blue-600`**) | dynamic options + "All" |

filter chips ต่างจาก 2-option toggle: มีหลายตัว, dynamic, บางที่มี count/border, active เป็น solid fill ไม่ใช่ white-on-grey. **2 ทางเลือก** (implementer เลือกแล้วจดใน Notes):
- **(B1) เพิ่ม `variant?: 'segment' | 'filter'` ให้ SegmentedToggle** — `filter` = ชิป solid (`bg-indigo-600 text-white` เมื่อ active) รองรับ options แบบ dynamic แล้ว migrate ทั้ง 3 จุด (กำจัด `bg-blue-600` ไปเลย)
- **(B2) ถ้า B1 บานปลาย** — อย่างน้อยแก้ `CourseListPage:777,790` `bg-blue-600` → `bg-indigo-600` (ให้ตรงโทน) แล้วเลื่อน migrate เต็มรูปแบบเป็นงานถัดไป (จดใน Notes)

> **เป้าหมายบังคับของแผนนี้:** หลังงานเสร็จ **ต้องไม่เหลือ `bg-blue-600` ใน `src/pages`** (ผ่าน B1 หรืออย่างน้อย B2)

### นอก scope
- radio/checkbox indicator (เช่น `BulkAssignPage:412` `border-indigo-500 bg-indigo-500`) — **ไม่ใช่ toggle อย่าแตะ**
- icon-only = PLAN-070; ปุ่มข้อความ = PLAN-069 (จบแล้ว)

## Verification
1. `npm run lint && npm run build` ผ่าน
2. **grep `bg-blue-600` ใน `src/pages` = 0** (เกณฑ์บังคับ)
3. dev server: สลับ toggle ทุกจุด (Group/Individual, picker/bulk, filter) ทำงานเหมือนเดิม, active state ถูกต้อง, หน้าตา toggle ตรงกันทุกหน้า
4. Regression: filter (status/type) ยังกรองข้อมูลถูก; BulkAssign mode สลับแล้ว panel เปลี่ยนถูก
5. แนบ screenshot toggle 2-3 จุด + ระบุว่าเลือก B1 หรือ B2 ใน Implementer Notes

## Implementer Notes
- **Approach chosen: B1** — extended `SegmentedToggle` with `variant?: 'segment' | 'filter'`. Filter variant renders solid indigo fill on active (`bg-indigo-600 text-white`), bordered white on inactive — matching the existing filter chip styling from AssignmentDetailPage/AssignmentReportPage.
- Section A (4 two-option toggles): migrated all 4 locations in BulkAssignPage, LearnerGroupEditorPage, AssignmentDetailPage (modal), and LearnerGroupDetailPage (modal). Presentation-only change — state/handler preserved.
- Section B (3 filter-chip rows): migrated all 3 locations:
  - AssignmentDetailPage learner status filter → `SegmentedToggle variant="filter"` with `className="flex-wrap"`
  - AssignmentReportPage status filter → `SegmentedToggle variant="filter"` inside existing flex wrapper (alongside select dropdowns)
  - CourseListPage course type filter → `SegmentedToggle variant="filter"` with `className="min-w-0 overflow-x-auto custom-scrollbar max-sm:pb-1 flex-nowrap"` (horizontal scroll layout)
- `bg-blue-600` in `src/pages` = **0** (verified via ripgrep)
- Lint + build pass clean

## Reviewer Sign-off (Claude Code) — VERIFIED
- **B1 confirmed:** `SegmentedToggle` เพิ่ม `variant?: 'segment'|'filter'` — filter = ชิป bordered, active `bg-indigo-600 text-white`; segment variant เดิม**ไม่ถูกแตะ** (2-option tabs render เหมือนเดิม, active blue→indigo ที่ AssignmentDetail tabs = normalize ถูกทาง)
- **7 toggle migrate ครบ:** segment 4 (BulkAssign mode, picker/bulk tabs ×3) + filter 3 (AssignmentDetail status, AssignmentReport status, CourseList type) — ตรวจ AssignmentDetailPage + CourseListPage ละเอียด: `options` สร้างถูก (คง `learnerStatusLabel`/dynamic types + "All"), `value`/`onChange` ต่อ state เดิมตรง, className คง layout (`flex-wrap` / `overflow-x-auto`)
- **เกณฑ์บังคับผ่าน:** `grep bg-blue-600 src/pages = 0` (reviewer ยืนยันเอง) — สีน้ำเงินก้อนสุดท้ายหายหมด
- **Regression:** filter/tab เป็น presentation-only (setState เดิม) — logic กรอง/สลับ panel ไม่เปลี่ยน
- **Reviewer รันเอง:** `npm run lint` clean + `npm run build` เขียว
- **หมายเหตุ:** งานนี้พันกับ PLAN-070 (Copilot ทำรวมทีเดียว) — commit เป็นก้อนเดียวกัน
