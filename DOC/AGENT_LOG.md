# Agent Work Log

บันทึกกลางสำหรับ AI agent ทุกตัว (Claude Code, Antigravity) — **ต่อ entry ใหม่ไว้บนสุด** หลังจบงานที่แก้โค้ดทุกครั้ง

Format ต่อ entry:

```
## [YYYY-MM-DD HH:mm] <Agent> — <สรุปงานสั้น ๆ>
- ทำอะไร: ...
- ไฟล์หลักที่แตะ: ...
- Contract ที่เปลี่ยน (API shape / props / DB): ... (หรือ "ไม่มี")
- Verified: lint/build/test อะไรผ่านบ้าง
```

---

## [2026-06-12 13:11] GitHub Copilot (GPT-5.3-Codex) — ยกระดับหน้า Learner Group Categories + Assignment Gantt ให้เป็นมาตรฐาน UI เดียวกัน
- ทำอะไร: รีแฟกเตอร์ 2 หน้าให้ใช้ shared layout เดียวกับหน้า list มาตรฐาน โดยย้ายขึ้น `DataGridSurface`, ปรับโครง header/actions/summary bar, ปรับตารางให้เป็น bordered unified surface พร้อม sticky header + custom scrollbar และปรับ loading/empty state ให้ใช้แพทเทิร์นเดียวกัน; หน้า Gantt เพิ่ม action ปุ่ม `Today` แบบมาตรฐาน, ปรับ label filter ให้อ่านง่าย (`In Progress`) และย้ายทั้ง timeline ลงใน grid-surface เดียวกับหน้าอื่น
- ไฟล์หลักที่แตะ: `iLearn.Admin.React/src/pages/master-data/LearnerGroupCategoriesPage.tsx`, `iLearn.Admin.React/src/pages/assignments/AssignmentGanttPage.tsx`, `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน: ไม่มี
- Verified: `npm run lint` ผ่าน (warning baseline เดิม 10 รายการ, ไม่มี error), `npm run build` ผ่าน

## [2026-06-12 13:06] GitHub Copilot (GPT-5.3-Codex) — ย้าย Schedule ออกจาก sidebar ไปหน้า Assignments และย่อชื่อ
- ทำอะไร: ลบเมนู `Schedule (Gantt)` ออกจาก sidebar หมวด Learning; เพิ่มปุ่มในหน้า Assignments (`EntityListPage`) สำหรับเปิด `/assignments/gantt`; เปลี่ยนชื่อปุ่มให้สั้นเป็น `Schedule`; และปรับ breadcrumb ของ route `gantt` ให้แสดง `Schedule`
- ไฟล์หลักที่แตะ: `iLearn.Admin.React/src/config/navigation.ts`, `iLearn.Admin.React/src/pages/EntityListPage.tsx`, `iLearn.Admin.React/src/components/layout/Breadcrumbs.tsx`, `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน: ไม่มี
- Verified: `npm run lint` ผ่าน (warning baseline เดิม 10 รายการ, ไม่มี error), `npm run build` ผ่าน

## [2026-06-12 13:04] GitHub Copilot (GPT-5.3-Codex) — เอา sub menu ใต้ Assignments ออกจาก sidebar
- ทำอะไร: ปรับ navigation ฝั่ง React ให้ `Assignments` ไม่มี `children` แล้ว และย้ายลิงก์ `Schedule (Gantt)` มาเป็นเมนูหลักระดับเดียวกันในหมวด Learning เพื่อไม่ให้เกิด sub sidebar ใต้ Assignments
- ไฟล์หลักที่แตะ: `iLearn.Admin.React/src/config/navigation.ts`, `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน: ไม่มี
- Verified: `npm run lint` ผ่าน (warning baseline เดิม 10 รายการ, ไม่มี error), `npm run build` ผ่าน

## [2026-06-12 12:59] GitHub Copilot (GPT-5.3-Codex) — ปรับ Shared Grid UI ให้เป็น bordered unified style ทั้งระบบ
- ทำอะไร: ปรับคอมโพเนนต์ตารางกลางให้ทุกหน้าได้สไตล์เดียวกับแนว Learner Group Explorer โดยเพิ่ม bottom padding ให้ `DataGridSurface`; รีโครง `AppTable` ให้กรอบตาราง (viewport + footer) เป็น border rounded + shadow เดียวกัน; ปรับ spacing หัวคอลัมน์/เซลล์ข้อมูลจาก `py-2` เป็น `py-2.5`; รีดีไซน์ `AppTableSearch` ให้เป็น clean toolbar (ซ้ายแสดง `Showing X records` + filter chips, ขวาเป็นช่องค้นหา rounded + ปุ่ม clear); และปรับ `LearnerGroupListPage` wrapper จาก `py-4` เป็น `pt-4 pb-0` เพื่อตัด padding ซ้อน
- ไฟล์หลักที่แตะ: `iLearn.Admin.React/src/components/ui/DataGridSurface.tsx`, `iLearn.Admin.React/src/components/ui/AppTable.tsx`, `iLearn.Admin.React/src/components/ui/table/AppTableSearch.tsx`, `iLearn.Admin.React/src/components/ui/table/AppTableFooter.tsx`, `iLearn.Admin.React/src/pages/learner-groups/LearnerGroupListPage.tsx`, `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน: เปลี่ยน props ของ `AppTableSearch` โดยเพิ่ม `totalCount: number` (internal shared UI contract)
- Verified: `npm run lint` ผ่าน (warning baseline เดิม 10 รายการ, ไม่มี error), `npm run build` ผ่าน, `dotnet build iLearn.Tests -o artifacts\verify-test` ผ่าน, `dotnet test artifacts\verify-test\iLearn.Tests.dll` ผ่าน (112/112), ลบโฟลเดอร์ชั่วคราว `artifacts\verify-test` แล้ว

## [2026-06-12 12:52] GitHub Copilot (GPT-5.3-Codex) — เพิ่ม single global breadcrumbs + URL history navigation ให้ Learner Group Explorer
- ทำอะไร: ปรับระบบ breadcrumbs ให้รองรับ custom trail ระดับ global (`customCrumbs`) แล้วเชื่อมหน้า `LearnerGroupListPage` เข้ากับ header breadcrumb เดียวของระบบ; เปลี่ยนการนำทางโฟลเดอร์จาก state ภายในเป็น URL query `categoryId` ผ่าน `useSearchParams` เพื่อรองรับ browser back/forward; เพิ่มปุ่ม `Back` ใน toolbar เมื่ออยู่โฟลเดอร์ย่อย; เอา breadcrumb ซ้ำซ้อนในเนื้อหาเพจออก
- ไฟล์หลักที่แตะ: `iLearn.Admin.React/src/lib/breadcrumbContext.tsx`, `iLearn.Admin.React/src/components/layout/Breadcrumbs.tsx`, `iLearn.Admin.React/src/pages/learner-groups/LearnerGroupListPage.tsx`, `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน: เพิ่ม UI context contract ใน `breadcrumbContext` จากเดิม (`labels`, `setLabel`) เป็น (`labels`, `setLabel`, `customCrumbs`, `setCustomCrumbs`) สำหรับหน้า override เส้นทาง breadcrumb
- Verified: `npm run lint` ผ่าน (warning baseline เดิม 10 รายการ, ไม่มี error), `npm run build` ผ่าน, `dotnet build iLearn.Tests -o artifacts\verify-test` ผ่าน, `dotnet test artifacts\verify-test\iLearn.Tests.dll` ผ่าน (112/112), ลบโฟลเดอร์ชั่วคราว `artifacts\verify-test` แล้ว

## [2026-06-12 11:53] GitHub Copilot (GPT-5.3-Codex) — ปรับ Learner Group Explorer เป็น unified list แบบ no-sidebar ตาม implementation plan
- ทำอะไร: อ่านแผน `Implementation Plan: Learner Group Unified File Explorer Layout (No Sidebar)` แล้วปรับ `LearnerGroupListPage` เป็นมุมมองเดียวเต็มความกว้างแบบ file explorer: breadcrumb คลิกย้อนระดับได้, ตารางรวมโฟลเดอร์+กลุ่มใน grid เดียว (folder-first), ค้นหาในโฟลเดอร์ปัจจุบัน, ดับเบิลคลิกเข้าโฟลเดอร์/เปิดรายละเอียดกลุ่ม, modal สร้างโฟลเดอร์, modal ย้ายกลุ่มด้วย tree picker, และ action ลบโฟลเดอร์/ลบกลุ่มพร้อม confirm
- ไฟล์หลักที่แตะ: `iLearn.Admin.React/src/pages/learner-groups/LearnerGroupListPage.tsx`, `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน: ไม่มี (frontend-only; ใช้ endpoint เดิม `LearnerGroupCategories`, `LearnerGroups`, `admin/DivisionsCRUD/Get`)
- Verified: `npm run lint` ผ่าน (warning baseline เดิม 10 รายการ, ไม่มี error), `npm run build` ผ่าน, `dotnet build iLearn.Tests -o artifacts\verify-test` ผ่าน, `dotnet test artifacts\verify-test\iLearn.Tests.dll` ผ่าน (112/112), ลบโฟลเดอร์ชั่วคราว `artifacts\verify-test` แล้ว

## [2026-06-12 11:33] GitHub Copilot (GPT-5.3-Codex) — ปรับ Learner Group Explorer เป็น File Explorer flow แบบครบ
- ทำอะไร: อัปเกรด `LearnerGroupListPage` ให้ครบตามแผนแบบ explorer (sub-folder cards + double-click/open, ปุ่ม `New Folder` พร้อม modal สร้างโฟลเดอร์ใต้ตำแหน่งปัจจุบัน, ปุ่ม `Delete` เฉพาะโฟลเดอร์ว่างพร้อม confirm, action `Move Group` พร้อม modal tree เลือกปลายทางและยิง `PUT LearnerGroups/{id}`); ปรับตารางให้แสดงเฉพาะ direct children ของโฟลเดอร์ปัจจุบัน; ปุ่ม `Create Group` แนบ query `?categoryId=<current>`; เพิ่มการอ่าน `categoryId` query ใน `LearnerGroupEditorPage` เพื่อ preselect หมวดหมู่ตอนสร้างกลุ่มจาก context folder
- ไฟล์หลักที่แตะ: `iLearn.Admin.React/src/pages/learner-groups/LearnerGroupListPage.tsx`, `iLearn.Admin.React/src/pages/learner-groups/LearnerGroupEditorPage.tsx`, `iLearn.Application/DTOs/PaginationParams.cs`, `iLearn.Application/Services/LearnerGroupService.cs`
- Contract ที่เปลี่ยน: เพิ่ม query contract `rootCategoryOnly` (bool?) ใน `PaginationParams` และ `LearnerGroupService.GetPagedAsync` เพื่อรองรับ root-folder filter (`CategoryId == null`) สำหรับ explorer root view; contract อื่นคงเดิม
- Verified: `npm run lint` ผ่าน (มี warning baseline เดิม 10 รายการ, ไม่มี error), `npm run build` ผ่าน, `dotnet build iLearn.Tests -o artifacts\\verify-test` ผ่าน, `dotnet test artifacts\\verify-test\\iLearn.Tests.dll` ผ่าน (112/112), ลบโฟลเดอร์ชั่วคราว `artifacts\\verify-test` แล้ว

## [2026-06-12 11:17] GitHub Copilot (GPT-5.3-Codex) — เพิ่มหน้า Learner Group Directory Explorer + recursive folder filter
- ทำอะไร: สร้างหน้า list ใหม่ `LearnerGroupListPage` สำหรับ route `/learner-groups` แบบ split layout (Left folder tree + Right data grid) ด้วย `AppTreeView` + `AppTable`; รองรับการเลือกโฟลเดอร์แล้วกรองข้อมูลแบบสืบทอดลูกทั้งหมด (recursive descendants) พร้อมเปิดรายละเอียดได้จากดับเบิลคลิกหรือปุ่ม Info; ปรับ route ใน `App.tsx` ให้ใช้หน้าใหม่แทน `EntityListPage`
- ไฟล์หลักที่แตะ: `iLearn.Admin.React/src/pages/learner-groups/LearnerGroupListPage.tsx` (ใหม่), `iLearn.Admin.React/src/App.tsx`, `iLearn.Application/DTOs/PaginationParams.cs`, `iLearn.Application/Services/LearnerGroupService.cs`
- Contract ที่เปลี่ยน: เพิ่ม query contract ใน `PaginationParams` เป็น `categoryId` หลายค่า (`List<int>?`) เพื่อรองรับ filter แบบ tree descendants ที่ส่งซ้ำเป็น `?categoryId=1&categoryId=2...`; `LearnerGroupService.GetPagedAsync` รองรับการกรองตามชุด `categoryId`
- Verified: `npm run lint` ผ่านโดยเหลือ warning เดิมของโปรเจ็กต์ 10 รายการ (ไม่มี error), `npm run build` ผ่าน, `dotnet build iLearn.Tests -o artifacts\verify-test` ผ่าน, `dotnet test artifacts\verify-test\iLearn.Tests.dll` ผ่าน (112/112)

## [2026-06-12 11:06] GitHub Copilot (GPT-5.3-Codex) — เปลี่ยน Category เป็น Folder Explorer modal ด้วย AppTreeView
- ทำอะไร: รีแฟกเตอร์ `LearnerGroupEditorPage` จาก searchable combobox เป็น file-explorer selector ตามแผนล่าสุด: แสดง read-only path field + ปุ่ม "Select Category Folder...", เปิด modal backdrop พร้อม tree structure ผ่าน `AppTreeView`, เลือกโฟลเดอร์ด้วย temp state แล้วกด Confirm เพื่อ commit ค่า, แสดง selected path ทั้งในฟอร์มและ review step
- ไฟล์หลักที่แตะ: `iLearn.Admin.React/src/pages/learner-groups/LearnerGroupEditorPage.tsx`
- Contract ที่เปลี่ยน: ไม่มี (ใช้ `LearnerGroupCategories` shape เดิม `id`, `name`, `parentId`, `depth` และใช้ `TreeViewNode` ของ `AppTreeView` แบบเดิม)
- Verified: `npm run lint` ผ่านโดยเหลือ warning เดิมของโปรเจ็กต์ 10 รายการ (ไม่มี error), `npm run build` ผ่าน, `dotnet build iLearn.Tests -o artifacts\verify-test` ผ่าน, `dotnet test artifacts\verify-test\iLearn.Tests.dll` ผ่าน (112/112)

## [2026-06-12 11:01] GitHub Copilot (GPT-5.3-Codex) — เปลี่ยน Category เป็น searchable combobox พร้อม full path
- ทำอะไร: อัปเกรด `LearnerGroupEditorPage` จาก category dropdown/tree-select เป็น custom searchable combobox (เปิด/ปิด popover, พิมพ์ค้นหาแบบทันที, เลือกจาก full path, clear selection, close เมื่อคลิกนอกพื้นที่); เพิ่ม path resolution สำหรับ selected category และแต่ละตัวเลือกเพื่อแสดง `Parent / Child / Leaf`; ปรับ review step ให้แสดง category เป็น full path เดียวกับ combobox
- ไฟล์หลักที่แตะ: `iLearn.Admin.React/src/pages/learner-groups/LearnerGroupEditorPage.tsx`
- Contract ที่เปลี่ยน: ไม่มี (ใช้ field เดิมของ endpoint `LearnerGroupCategories` คือ `id`, `name`, `parentId`, `depth`)
- Verified: `npm run lint` ผ่านโดยเหลือ warning เดิมของโปรเจ็กต์ 10 รายการ (ไม่มี error), `npm run build` ผ่าน, `dotnet build iLearn.Tests -o artifacts\verify-test` ผ่าน, `dotnet test artifacts\verify-test\iLearn.Tests.dll` ผ่าน (112/112)

## [2026-06-12 10:53] GitHub Copilot (GPT-5.3-Codex) — ปรับ UX Learner Directory + Category hierarchy path
- ทำอะไร: อัปเกรด `LearnerDirectorySelector` (dropdown chevron style, active filter badges, select all matching learners จาก filter ทั้งชุด, loading bar แบบไม่บังตาราง, searchable selected chips); ปรับ `LearnerGroupEditorPage` ให้ Category เป็น tree-select แบบ hierarchical จาก `parentId`; ปรับ `LearnerGroupDetailPage` ให้แสดง category breadcrumb จาก `categoryAncestors`; แก้ `EntityListPage` ให้ lookup `LearnerGroupCategories` และ render path เต็มในคอลัมน์ Category
- ไฟล์หลักที่แตะ: `iLearn.Admin.React/src/components/shared/LearnerDirectorySelector.tsx`, `iLearn.Admin.React/src/pages/learner-groups/LearnerGroupEditorPage.tsx`, `iLearn.Admin.React/src/pages/learner-groups/LearnerGroupDetailPage.tsx`, `iLearn.Admin.React/src/pages/EntityListPage.tsx`
- Contract ที่เปลี่ยน: ไม่มี (ใช้ field เดิมจาก API ที่มีอยู่แล้ว ได้แก่ `LearnerGroupCategories.parentId` และ `LearnerGroupDetail.categoryAncestors`)
- Verified: `npm run lint` ผ่านโดยเหลือ warning เดิมของโปรเจ็กต์ 10 รายการ (ไม่มี error), `npm run build` ผ่าน, `dotnet build iLearn.Tests -o artifacts\verify-test` ผ่าน, `dotnet test artifacts\verify-test\iLearn.Tests.dll` ผ่าน (112/112)

## [2026-06-12 10:40] Claude Code — เอา Bulk Assign ออกจาก sidebar
- ทำอะไร: ลบเมนูย่อย "Bulk Assign" ใต้ Assignments ออกจาก sidebar (เป็น action ไม่ใช่ directory — เข้าผ่านปุ่ม Bulk Assignment ในหน้า Assignments list และปุ่มใน Course detail แทน) route `/assignments/bulk` ยังอยู่เหมือนเดิม
- ไฟล์หลักที่แตะ: `iLearn.Admin.React/src/config/navigation.ts`
- Contract ที่เปลี่ยน: ไม่มี
- Verified: tsc 0 error, vite build ผ่าน

## [2026-06-12 10:30] Claude Code — จัด Sidebar ใหม่ แยกโซน Admin / Super Admin
- ทำอะไร: เปลี่ยน `navigation.ts` จาก flat list เป็น `navigationSections` (Dashboard / Learning / Operations / Super Admin) — เมนู SuperAdmin ทั้งหมด (Enrollments ที่เคยซ่อนใต้ Operations, Master Data, Admin Users, System Config) ย้ายมารวมใน section "Super Admin" ที่มีหัวข้อสีเหลืองกำกับ และทั้ง section หายไปเลยสำหรับ admin ปกติ; Sidebar.tsx render แบบ section พร้อม heading
- ไฟล์หลักที่แตะ: `iLearn.Admin.React/src/config/navigation.ts` (export เปลี่ยนจาก `navigationItems` เป็น `navigationSections`), `iLearn.Admin.React/src/components/layout/Sidebar.tsx`
- Contract ที่เปลี่ยน: **`navigation.ts` ไม่ export `navigationItems` แล้ว** — ใครจะใช้เมนูให้ import `navigationSections` แทน (ตอนนี้มี Sidebar ใช้ที่เดียว)
- Verified: tsc 0 error, eslint ผ่าน, vite build ผ่าน

## [2026-06-12 10:15] Antigravity — ปรับความสูงแผง Syllabus/Selected, แก้การโหลดซ้ำซ้อน และปรับปรุง SCORM Upload
- ทำอะไร: 
  1. ปรับปรุงความสูงของ Wizard Steps ใน BulkAssignPage.tsx และ LearnerGroupEditorPage.tsx เป็น h-[calc(100vh-265px)] เพื่อแก้ปัญหาพื้นที่ว่างสีขาวด้านล่างให้กล่องข้อมูลยืดชิดขอบล่างพอดีโดยไม่มี scrollbar ซ้อน
  2. แก้ปัญหาการ Fetch ข้อมูลของตารางสองครั้งตอน Mount โดยการปรับปรุง AppTable.tsx ตั้งค่าเริ่มต้นของ pageSize เป็น 0 และข้ามการเรียกโหลดข้อมูลชั่วคราวจนกว่าระบบจะวัดขนาด viewport ความสูงจริงในฝั่ง layout เสร็จสิ้นแล้วค่อยดึงข้อมูลด้วยขนาดที่ถูกต้องเพียงครั้งเดียว
  3. ปรับปรุงส่วน Upload SCORM Package ใน ContentItemEditorPage.tsx: (A) เปลี่ยนตัวเลือกไฟล์อินพุตแบบเดิมที่แสดงผลไม่สวยงามเป็น Label Wrapper/Upload Zone ที่ซ่อน input จริงด้วย sr-only เพื่อความกลมกลืน (B) ปรับปรุงส่วนการแสดงข้อผิดพลาด (Exception Handling) ให้อ่านและกรองข้อความผิดพลาดจาก JSON ของฝั่ง Backend มาแสดงผลผ่าน toast แทนการแสดงสตริง JSON ดิบ (C) นำขั้นตอน 'Metadata' ออกจากตัวช่วยอัปโหลดเมื่อสร้างข้อมูลใหม่ และปรับปรุงตัวเลือกแสดงไฟล์อัปโหลดให้ออกมาเป็นรูปตารางรายชื่อแบบเดียวกับแถบ Content ในหน้าสร้างคอร์สใหม่ (/courses/new) โดยย้ายตัวกรองประเภทไฟล์ (Learn / Exam) ไปเป็น dropdown ภายในแถวตารางแทน (D) จัดสเกลคอลัมน์ของตารางข้อมูลไฟล์อัปโหลดให้มีโครงสร้างเดียวกันโดยเพิ่มคอลัมน์ Source ("New upload") และ Status ("Ready") เพื่อให้การออกแบบสอดคล้องสม่ำเสมอ
- ไฟล์หลักที่แตะ: `iLearn.Admin.React/src/pages/assignments/BulkAssignPage.tsx`, `iLearn.Admin.React/src/pages/learner-groups/LearnerGroupEditorPage.tsx`, `iLearn.Admin.React/src/components/ui/AppTable.tsx`, `iLearn.Admin.React/src/pages/content-library/ContentItemEditorPage.tsx`, `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน: ไม่มี
- Verified: tsc 0 error, vite build ผ่าน, eslint ผ่าน, dotnet test 112/112 ผ่าน (แบบ --no-build เนื่องจากไฟล์ C# ไม่ได้เปลี่ยนและตัวระบบถูกรันอยู่ทำให้ DLL ล็อก)

## [2026-06-12 10:00] Claude Code — Reconcile งานสองฝั่ง + แก้ Learners casing
- ทำอะไร: อ่าน brain ของ Antigravity (session 250e1b99) แล้วตรวจรอยต่อ — พบ handler เปิด learner profile ยังใช้ `NID`/`EId` (PascalCase) ขณะที่ Antigravity เปลี่ยน rows เป็น camelCase แล้ว → แก้เป็น `nid`/`eId`; สร้าง `CLAUDE.md` + log ไฟล์นี้
- ไฟล์หลักที่แตะ: `iLearn.Admin.React/src/pages/EntityListPage.tsx`, `CLAUDE.md`, `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน: ไม่มี (ตามของ Antigravity)
- Verified: tsc 0 error, vite build ผ่าน, dotnet test 112/112

## [2026-06-12 ~09:30] Antigravity — Smoke Test Mitigation 14 จุด + Typography/Density
- ทำอะไร: เพิ่ม `ContentItem.CachedFileLength` (+migration+backfill), ปรับ GetPaged/GetFiltered ไม่แตะ blob, Dashboard memory cache 60s, LearnersController → camelCase typed DTO, moduleConfigs camelCase + cellRender, MasterDataDetailPage รองรับ id='new', AppTable loading เริ่มต้น true + min pageSize, ฟอนต์ Sarabun→Noto Sans Thai, compact density (rowHeight 38, py-2, sidebar 210px), ลบเมนู Access Control
- ไฟล์หลักที่แตะ: ContentItem.cs, ContentItemsController.cs, ContentItemsCRUDController.cs, DashboardController.cs, LearnersController.cs, moduleConfigs.ts, EntityListPage.tsx, MasterDataDetailPage.tsx, AppTable.tsx, DashboardPage.tsx, navigation.ts, index.css, Sidebar.tsx, AppLayout.tsx
- Contract ที่เปลี่ยน: **Learners list rows เป็น camelCase (`nid`, `eId`, `englishFirstName`...)**; ContentItems CRUD list ใช้ `ContentItemCrudRow`; DB เพิ่มคอลัมน์ `CachedFileLength`
- Verified: dotnet test 112/112, npm lint + build ผ่าน
- รายละเอียดเต็ม: `~\.gemini\antigravity\brain\250e1b99-8d2b-4d2e-b0fe-cbb79a54717a\walkthrough.md`

## [2026-06-12 ~09:00] Claude Code — UI standardization + bug fixes (สะสมทั้ง session)
- ทำอะไร: สร้าง shared UI components (`LoadingState`, `NotFoundState`, `StatusBadge`, `SectionHeader`, `ProgressBar`, `ControlsSidebar`/`ControlAction`, `ConfirmDialog`/`useConfirm`) แล้วรีแฟกเตอร์ทุกหน้าให้ใช้ (-1,000+ บรรทัด); ลบ dead code/DevExtreme ค้าง; แก้ ContentItems GetPaged ไม่โหลด blob (ก่อน Antigravity ทำ CachedFileLength ต่อ); แก้ type mismatch กับ DTO จริง 3 หน้า (AssignmentDetail, AssignmentReport, CourseDetail) พร้อมคอมเมนต์ `// Mirrors <Dto>`; เพิ่ม `<Remount>` wrapper ใน App.tsx กัน state ค้างข้าม route + `key={config.controller}` บน AppTable; เพิ่มกติกาทั้งหมดใน README
- ไฟล์หลักที่แตะ: `src/components/ui/*` (ใหม่ 7 ไฟล์), ทุกหน้าใน `src/pages/`, `App.tsx`, `EntityListPage.tsx`, `iLearn.Admin.React/README.md`, `ContentItemsController.cs`
- Contract ที่เปลี่ยน: ไม่มี (ฝั่ง React ปรับตาม DTO จริง)
- Verified: tsc 0 error, eslint 0 error, vite build ผ่าน, ContentItemsControllerTests ผ่าน
