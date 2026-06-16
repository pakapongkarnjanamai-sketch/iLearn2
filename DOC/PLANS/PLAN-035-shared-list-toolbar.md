# PLAN-035: สกัด shared `<ListToolbar>` — รวม toolbar (Showing + search + chips) ให้ spacing สม่ำเสมอทั้งระบบ

- **Status:** DONE
- **Assigned:** GPT (GPT-5.3 Codex)
- **Priority:** Medium
- **Estimated scope:** 1 component ใหม่ + refactor `AppTableSearch` + explorer/list pages ที่ hand-roll toolbar

## Problem (จากการสำรวจ spacing)

หน้า list ใช้ toolbar (แถว "Showing X / search / chips") **คนละ component → spacing ไม่เท่ากัน**:

| toolbar | top/bottom padding | search input | หน้า |
|---|---|---|---|
| **standard** `AppTableSearch` | `pt-3 pb-2` | `py-2` | /assignments, /learners, /learning-logs, /enrollments, /master-data×4, /users (ผ่าน `AppTable`) |
| **explorer hand-rolled** | `pt-4 pb-0` | `py-1.5` | `/courses`, `/learner-groups` |
| toolbar เขียนเอง (ต้องสำรวจ) | ? | ? | `LearnerGroupCategoriesPage`, `AssignmentGanttPage` |

ผลคือ /courses + /learner-groups (explorer) แถว toolbar อยู่ต่ำลง 4px + ช่องค้นหาเตี้ยกว่า เทียบกับ /assignments → ไม่สม่ำเสมอ

**เป้าหมาย:** มี toolbar **component เดียว** เป็น source of truth ทุกหน้า list ใช้ร่วม → spacing/หน้าตาเท่ากันถาวร

## Scope (ทำแค่นี้)

### 1. สร้าง `<ListToolbar>` — `src/components/ui/` (หรือ `ui/table/`)
- props:
  - `count?: number`, `countUnit?: string` (เช่น "records" / "items in this folder") + รองรับซ่อน count
  - `searchValue: string`, `onSearchChange: (v) => void`, `searchPlaceholder?: string`
  - `toolbarContent?: ReactNode` (slot สำหรับ chips/filter เช่น Course Type)
  - (optional) `onClearSearch`
- markup + spacing = **มาตรฐานเดียว** อิงจาก `AppTableSearch` ปัจจุบัน: `flex flex-col gap-3 pb-2 pt-3 lg:flex-row lg:items-center lg:justify-between`, count `text-xs font-semibold text-slate-500`, search input `py-2 pl-9 pr-9 ...` (ใช้ค่าของ AppTableSearch เป็นมาตรฐาน — **อย่าใช้ค่า explorer `pt-4`/`py-1.5`**)
- presentational ล้วน (controlled search ผ่าน props)

### 2. ให้ `AppTableSearch` ใช้ `<ListToolbar>` ภายใน (หรือกลายเป็น thin wrapper)
- เพื่อให้หน้าที่ผ่าน `AppTable` (assignments/learners/users ฯลฯ) ใช้ markup เดียวกับ explorer แบบรับประกัน — **พฤติกรรม/หน้าตาของ AppTable ต้องไม่เปลี่ยน** (ค่าเดิมเป็นมาตรฐานอยู่แล้ว)

### 3. Migrate หน้า explorer ให้ใช้ `<ListToolbar>`
- `CourseListPage.tsx`: แทน toolbar hand-rolled (`pt-4 pb-0` + chips + search `py-1.5`) ด้วย `<ListToolbar>` — count="items in this folder", toolbarContent = Course Type chips, search = searchTerm/setSearchTerm (จาก `useExplorer`)
- `LearnerGroupListPage.tsx`: แทน toolbar hand-rolled ด้วย `<ListToolbar>` — count="items in this folder", search = searchTerm
- ผลข้างเคียงที่ตั้งใจ: explorer toolbar จะใช้ `pt-3 pb-2` + search `py-2` = **เท่ากับ /assignments** (นี่คือ fix ที่ผู้ใช้ต้องการ)

### 4. สำรวจ + align หน้าอื่นที่มี toolbar เขียนเอง
- `LearnerGroupCategoriesPage.tsx`, `AssignmentGanttPage.tsx` (+ `AssignmentReportPage.tsx` search) — ตรวจว่ามี "Showing/search" toolbar ที่ควรใช้ `<ListToolbar>` ไหม
  - ถ้าโครงสร้างเข้ากันได้ → migrate ให้ใช้ `<ListToolbar>` (สม่ำเสมอ)
  - ถ้าต่างมาก (เช่น Gantt มี control เฉพาะ) → อย่างน้อย **align ค่า spacing/search ให้ตรงมาตรฐาน** (`pt-3 pb-2`, `py-2`) แล้วจดใน Notes ว่าตัวไหน migrate ตัวไหนแค่ align

## Out of scope (ห้ามแตะ)

- ห้ามเปลี่ยน **ค่ามาตรฐาน** (ยึด AppTableSearch เดิม) — งานนี้คือทำให้ explorer/อื่น ๆ มาตรงมาตรฐาน ไม่ใช่เปลี่ยนมาตรฐาน
- ห้ามเปลี่ยนพฤติกรรม search/filter/chips/count logic (แค่ย้าย markup ไป component ร่วม)
- ห้ามแตะ `useExplorer`/`ExplorerTable`/`AppTable` core logic (แตะเฉพาะส่วน toolbar/search markup)
- ห้ามแตะ backend
- ห้ามสกัดของที่ใช้ที่เดียว (เฉพาะ toolbar ที่ซ้ำจริง)

## Acceptance criteria

- [x] มี `<ListToolbar>` ใน `src/components/ui` เป็น source of truth (presentational)
- [x] `AppTableSearch` + explorer toolbars (Course/LearnerGroup) ใช้ `<ListToolbar>` — ไม่มี toolbar markup ซ้ำ/ค่า `pt-4`/`py-1.5` เหลือใน explorer
- [x] `/courses`, `/assignments`, `/learner-groups` toolbar **spacing เท่ากัน** (top padding + ความสูง search ตรงกัน)
- [x] count/search/chips ทุกหน้าทำงานเหมือนเดิม (กรองได้, chips กรองได้, นับถูก)
- [x] หน้าอื่นที่สำรวจ (LearnerGroupCategories/Gantt/Report) migrate หรือ align ตามที่เหมาะ + จดใน Notes
- [x] `npm run lint` (0/0) + `npm run build` ผ่าน

## Verification

```powershell
npm run lint
npm run build
```
ทดสอบ manual: เปิด `/courses`, `/assignments`, `/learner-groups` วางทับ/สลับดู — แถว Showing+search อยู่ระดับเดียวกัน ช่องค้นหาสูงเท่ากัน; chips/search/count ยังทำงาน; เช็ค `/learners`, `/users` (AppTable) ว่าไม่เพี้ยน

## Implementer Notes

- ค่ามาตรฐานที่ยึด: ใช้ spacing/search ของ `AppTableSearch` เดิมเป็นแหล่งจริง (`pt-3 pb-2`, input `py-2 pl-9 pr-9`) ไม่เปลี่ยน behavior ของ search/filter/chips
- สิ่งที่ทำ:
  - เพิ่ม shared presentational `ListToolbar` ที่รองรับ `count`, `countUnit`, controlled search, `toolbarContent` slot, และ `onClearSearch`
  - ปรับ `AppTableSearch` เป็น thin wrapper ที่เรียก `ListToolbar` เพื่อให้หน้า `AppTable` ทั้งระบบใช้ markup ชุดเดียว
  - migrate explorer toolbar ใน `CourseListPage` (รวม Course Type chips) และ `LearnerGroupListPage` ให้ใช้ `ListToolbar`
  - migrate แถบ filter+search ใน `AssignmentReportPage` ให้ใช้ `ListToolbar` โดยคง logic status filter chips เดิม
- สำรวจหน้าอื่นใน scope:
  - `LearnerGroupCategoriesPage`: ไม่มี search toolbar (มีเฉพาะแถว count ใน table header) จึงไม่ migrate
  - `AssignmentGanttPage`: ไม่มี search toolbar (มี status chips + count header) จึงไม่ migrate
- Verification: `npm run lint` ผ่าน, `npm run build` ผ่าน
