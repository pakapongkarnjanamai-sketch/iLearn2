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

## [2026-06-12 18:57] GitHub Copilot (GPT-5.3-Codex) — ปิด PLAN-009 เป็น DONE และยืนยันผลทดสอบรอบสุดท้าย
- ทำอะไร: ตรวจเทียบโค้ดจริงกับขอบเขต PLAN-009 ซ้ำทั้งหมด (DetailPageHeader/backTo/backLabel ถูกถอด, sidebar เป็น action-only, Assignment และ Learner Group เป็นแท็บตามสเปค), อัปเดตสถานะ `PLAN-009-refine-detail-pages-design.md` จาก READY เป็น DONE พร้อม acceptance และ implementer notes, แล้วยืนยันว่า scope ต่อเนื่องของ PLAN-008 อยู่ในสถานะ DONE แล้ว
- ไฟล์หลักที่แตะ: `DOC/PLANS/PLAN-009-refine-detail-pages-design.md`, `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี (เป็นการปิดแผนและยืนยันผล)
- Verified: `npm run lint` ผ่าน (0 errors, 11 warnings baseline), `npm run build` ผ่าน

## [2026-06-12 18:54] GitHub Copilot (GPT-5.3-Codex) — ปิดช่องว่าง LearnerGroup tabs + บังคับ action-only sidebar
- ทำอะไร: รีแฟกเตอร์ `LearnerGroupDetailPage` ให้มีแท็บ `Overview` และ `Members`; ย้ายข้อมูล `LMS Category` และ `Owner / Creator` ออกจาก `ControlsSidebar` ไปอยู่ในการ์ด Overview (`DetailCard` + `FactGrid`); ลบการใช้ `ControlsDivider`; ปรับ `ControlsSidebar` ให้เป็น action-only API ด้วยการลบ `ControlsDivider` helper ออกจากคอมโพเนนต์กลาง
- ไฟล์หลักที่แตะ: `iLearn.Admin.React/src/pages/learner-groups/LearnerGroupDetailPage.tsx`, `iLearn.Admin.React/src/components/ui/ControlsSidebar.tsx`, `DOC/PLANS/PLAN-008-detail-pages-migration.md`, `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน (API shape / props / DB): ปรับ UI helper contract ภายใน (`ControlsDivider` ถูกถอดออก); API/DB ไม่เปลี่ยน
- Verified: `npm run lint` ผ่าน (0 errors, 11 warnings baseline), `npm run build` ผ่าน; manual browser ที่ `/learner-groups/22` ยืนยันแท็บทำงานครบและ sidebar เหลือเฉพาะปุ่ม

## [2026-06-12 18:43] GitHub Copilot (GPT-5.3-Codex) — ปิดงาน DETAIL redesign ใหม่ + Assignment tabs
- ทำอะไร: ปรับ `AssignmentDetailPage` ให้เลิกใช้ KPI/Metric strip ด้านบน แล้วเปลี่ยนเป็นแท็บ `Overview`, `Courses`, `Learners` โดยย้าย metrics + schedule ไปอยู่ใน `Overview` ด้วย `DetailCard` + `FactGrid`; ยืนยันมาตรฐานใหม่ทั้งระบบ detail ว่าไม่มี `DetailPageHeader` และไม่มี Back link ใน `ControlsSidebar`; อัปเดต `PLAN-008` acceptance/notes ให้ตรงผลงานล่าสุด
- ไฟล์หลักที่แตะ: `iLearn.Admin.React/src/pages/assignments/AssignmentDetailPage.tsx`, `DOC/PLANS/PLAN-008-detail-pages-migration.md`, `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มีเพิ่ม (คงผลจากรอบก่อนที่ตัด `ControlsSidebar` props `backTo`/`backLabel`)
- Verified: `npm run lint` ผ่าน (0 errors, 11 warnings baseline), `npm run build` ผ่าน; grep ผ่านสำหรับ `DetailPageHeader`, `<ControlsSidebar ... backTo=...>`, `backTo?`/`backLabel?`/`ArrowLeft`, และ `auto-cols-fr grid-flow-col` ใน `AssignmentDetailPage.tsx`

## [2026-06-12 18:40] GitHub Copilot (GPT-5.3-Codex) — ปรับ Detail Pages ตามดีไซน์ใหม่ (ตัด Page Header + ตัด Back action ใน ControlsSidebar)
- ทำอะไร: ปรับมาตรฐานหน้า Detail ตามทิศทางใหม่โดยลบ `DetailPageHeader` ออกจาก shared detail primitives (`src/components/ui/detail/index.tsx`), ลบพร็อพ `backTo`/`backLabel` และลบการเรนเดอร์ปุ่ม Back ออกจาก `ControlsSidebar`; migrate หน้า detail ทั้ง 7 หน้าให้เลิกใช้ header และเลิกส่ง back props (`UserDetailPage`, `ContentItemDetailPage`, `CourseDetailPage`, `AssignmentDetailPage`, `LearnerGroupDetailPage`, `MasterDataDetailPage`, `LearnerProfilePage`) พร้อมคง logic เดิมของ tabs/modals/forms/actions
- ไฟล์หลักที่แตะ: `iLearn.Admin.React/src/components/ui/detail/index.tsx`, `iLearn.Admin.React/src/components/ui/ControlsSidebar.tsx`, `iLearn.Admin.React/src/pages/users/UserDetailPage.tsx`, `iLearn.Admin.React/src/pages/content-library/ContentItemDetailPage.tsx`, `iLearn.Admin.React/src/pages/courses/CourseDetailPage.tsx`, `iLearn.Admin.React/src/pages/assignments/AssignmentDetailPage.tsx`, `iLearn.Admin.React/src/pages/learner-groups/LearnerGroupDetailPage.tsx`, `iLearn.Admin.React/src/pages/master-data/MasterDataDetailPage.tsx`, `iLearn.Admin.React/src/pages/learners/LearnerProfilePage.tsx`, `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน (API shape / props / DB): เปลี่ยน UI props contract ของ `ControlsSidebar` โดยตัด `backTo`/`backLabel` ออก (API/DB ไม่เปลี่ยน)
- Verified: `npm run lint` ผ่าน (0 errors, 11 warnings baseline), `npm run build` ผ่าน; grep ยืนยันไม่มี `DetailPageHeader`, ไม่มี `<ControlsSidebar ... backTo=...>` และไม่มี `backTo?`/`backLabel?` ใน `ControlsSidebar.tsx`

## [2026-06-12 18:10] GitHub Copilot (GPT-5.3-Codex) — Implement PLAN-007 shared detail components และปิด PLAN-008 migration
- ทำอะไร: สร้าง shared detail primitives ใหม่ที่ `src/components/ui/detail/index.tsx` (`DetailPageHeader`, `DetailLayout`, `DetailCard`, `FactGrid`, `Fact`, `DetailSubSection`) แล้ว migrate หน้า detail ตามแผนครบ: PLAN-007 (`UserDetailPage`, `ContentItemDetailPage`) และ PLAN-008 (`CourseDetailPage`, `AssignmentDetailPage`, `LearnerGroupDetailPage`, `MasterDataDetailPage`, `LearnerProfilePage`); เพิ่ม `DetailPageHeader` ให้ `CourseDetailPage`/`LearnerGroupDetailPage` ตามเกณฑ์ยกระดับ; คง logic เดิมของ tabs, modals, members table, master-data edit form; อัปเดตสถานะแผน PLAN-008 เป็น DONE พร้อม acceptance checklist/notes
- ไฟล์หลักที่แตะ: `iLearn.Admin.React/src/components/ui/detail/index.tsx`, `iLearn.Admin.React/src/pages/users/UserDetailPage.tsx`, `iLearn.Admin.React/src/pages/content-library/ContentItemDetailPage.tsx`, `iLearn.Admin.React/src/pages/courses/CourseDetailPage.tsx`, `iLearn.Admin.React/src/pages/assignments/AssignmentDetailPage.tsx`, `iLearn.Admin.React/src/pages/learner-groups/LearnerGroupDetailPage.tsx`, `iLearn.Admin.React/src/pages/master-data/MasterDataDetailPage.tsx`, `iLearn.Admin.React/src/pages/learners/LearnerProfilePage.tsx`, `iLearn.Admin.React/src/pages/assignments/AssignmentReportPage.tsx`, `DOC/PLANS/PLAN-008-detail-pages-migration.md`, `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี
- Verified: `npm run lint` ผ่าน (0 errors, 11 warnings baseline), `npm run build` ผ่าน, grep acceptance ผ่าน (`minmax(0,1fr)_280px` = 0 และ `text-slate-400 font-bold uppercase tracking-wider` = 0 ใน `src/pages`), manual smoke ผ่านที่ `/courses/823`, `/assignments/248`, `/learner-groups/22`, `/master-data/divisions/1`, `/learners/n4734/profile`

## [2026-06-12 17:35] Antigravity — แก้ไขบั๊กการแสดงผลบทบาทผู้ใช้ (camelCase field mappings) และปัญหา key prop ใน UserEditorPage
- ทำอะไร: ปรับเปลี่ยนการเข้าถึงคุณสมบัติของออบเจกต์บทบาทผู้ใช้ (Role และ UserRole) จาก PascalCase เป็น camelCase ใน [UserEditorPage.tsx](file:///c:/Users/n4734/source/repos/iLearn2/iLearn.Admin.React/src/pages/users/UserEditorPage.tsx) และ [UserDetailPage.tsx](file:///c:/Users/n4734/source/repos/iLearn2/iLearn.Admin.React/src/pages/users/UserDetailPage.tsx) เพื่อให้สอดคล้องกับ DTO / API response JSON payload ที่ส่งมาจาก backend และแก้ปัญหา React warning เรื่อง unique key ในรายการบทบาท
- ไฟล์หลักที่แตะ: `iLearn.Admin.React/src/pages/users/UserEditorPage.tsx`, `iLearn.Admin.React/src/pages/users/UserDetailPage.tsx`, `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี (เป็นการแก้ฝั่ง Client/Frontend ให้ตรงตาม Contract ของ API ปัจจุบัน)
- Verified: `npm run lint` ผ่าน, `npm run build` ผ่าน, `dotnet test` (112/112 passed) ผ่านเรียบร้อย


## [2026-06-12 17:20] Antigravity — Implement PLAN-006 เพิ่มหน้าแสดงรายละเอียดผู้ใช้ระบบและฟังก์ชันการลบผู้ใช้
- ทำอะไร: สร้างหน้าสำหรับแสดงข้อมูลโดยละเอียด of Admin User ([UserDetailPage.tsx](file:///c:/Users/n4734/source/repos/iLearn2/iLearn.Admin.React/src/pages/users/UserDetailPage.tsx)) พร้อมแสดง Metric, ข้อมูลสังกัดองค์กร, และ Administrative Roles; ติดตั้งฟังก์ชันการลบแอดมินใน ControlsSidebar ผ่าน `useConfirm` และยิง `DELETE admin/UsersCRUD/Delete`; ปรับ [AdminUsersPage.tsx](file:///c:/Users/n4734/source/repos/iLearn2/iLearn.Admin.React/src/pages/users/AdminUsersPage.tsx) ให้ดับเบิลคลิกเปิดหน้ารายละเอียด และเพิ่มปุ่มรูปตา (`Eye`) สำหรับนำทาง; อัปเดตไฟล์รูท [App.tsx](file:///c:/Users/n4734/source/repos/iLearn2/iLearn.Admin.React/src/App.tsx)
- ไฟล์หลักที่แตะ: `iLearn.Admin.React/src/pages/users/UserDetailPage.tsx`, `iLearn.Admin.React/src/pages/users/AdminUsersPage.tsx`, `iLearn.Admin.React/src/App.tsx`, `DOC/PLANS/PLAN-006-admin-users-detail-delete.md`, `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน: ไม่มี (การเรียกใช้ API endpoint ยังรักษา Contract รูปแบบเดิม)
- Verified: `npm run lint` ผ่าน, `npm run build` ผ่าน, `dotnet test` (112/112 passed) ผ่านเรียบร้อย

## [2026-06-12 17:15] Claude Code — เขียน PLAN-006 (Admin Users: หน้า Detail + ฟังก์ชันลบ)
- ทำอะไร: ผู้ใช้รายงานช่องว่างที่พบระหว่างทำ PLAN-004: module Users ไม่มีหน้า Detail (`/users/:id`) และไม่มีฟังก์ชันลบ — ตรวจ backend แล้ว `GenericController.Delete` (`DELETE admin/UsersCRUD/Delete`, FormData `key`) มีอยู่แล้วไม่ต้องแก้ → เขียน **PLAN-006** (Assigned: Gemini): ไฟล์ใหม่ `UserDetailPage.tsx` (การ์ด + ControlsSidebar: Edit Roles / Delete ผ่าน useConfirm), grid dblclick → detail, route `users/:id` (Remount + RequireRole), breadcrumb setLabel เป็นชื่อ user
- ไฟล์หลักที่แตะ: `DOC/PLANS/PLAN-006-admin-users-detail-delete.md` (เอกสารล้วน)
- Contract ที่เปลี่ยน: ไม่มี
- Verified: n/a (planner ไม่แก้โค้ด)

## [2026-06-12 15:59] GitHub Copilot (GPT-5.3-Codex) — Implement PLAN-005 เปลี่ยน Learner Group Categories จาก modal เป็น Wizard
- ทำอะไร: สร้างหน้าใหม่ `LearnerGroupCategoryEditorPage` แบบ `AppWizard` 2 steps (Details/Review) รองรับ create+edit; ตัด modal create/edit ออกจาก `LearnerGroupCategoriesPage` และเปลี่ยน New/Edit เป็น route navigate; export type `LearnerGroupCategory` + `ApiListResponse` เพื่อใช้ร่วม; เพิ่ม routes `/master-data/learner-group-categories/new` และ `/master-data/learner-group-categories/:id/edit` (RequireRole + Remount) วางก่อน generic master-data routes; อัปเดตสถานะไฟล์แผน PLAN-005 เป็น DONE พร้อม checklist และ Implementer Notes
- ไฟล์หลักที่แตะ: `iLearn.Admin.React/src/pages/master-data/LearnerGroupCategoryEditorPage.tsx`, `iLearn.Admin.React/src/pages/master-data/LearnerGroupCategoriesPage.tsx`, `iLearn.Admin.React/src/App.tsx`, `DOC/PLANS/PLAN-005-learner-group-categories-wizard.md`, `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน: ไม่มี (ใช้ endpoint เดิม GET/POST/PUT/DELETE ของ LearnerGroupCategories)
- Verified: `npm run lint` ผ่าน (11 warnings baseline, 0 errors), `npm run build` ผ่าน; manual verify ผ่าน (new/edit wizard flow, invalid id -> NotFoundState, `/master-data/divisions/new` ไม่ถูก route ใหม่ชน); ทดสอบสร้างข้อมูลชั่วคราว `PLAN005_TMP` แล้วลบทิ้งเรียบร้อย

## [2026-06-12 17:00] Claude Code — สำรวจหน้าที่ create/edit ไม่ตรงมาตรฐาน wizard + เขียน PLAN-005
- ทำอะไร: ไล่ตรวจทุก page ว่า create/edit ใช้ pattern ไหน — เจอ 1 หน้าที่เข้าเคสเดียวกับ Users: `LearnerGroupCategoriesPage` (modal "New/Edit Category" กลางจอในหน้า list) → เขียน **PLAN-005** (Assigned: GPT): ไฟล์ใหม่ `LearnerGroupCategoryEditorPage.tsx` (wizard 2 steps Details→Review), ตัด modal, เพิ่ม routes `/master-data/learner-group-categories/new|/:id/edit` (ระวังไม่ให้ชน generic `master-data/:type/*`) — ข้อสังเกตอื่น: `MasterDataDetailPage` เป็นหน้าแยก+edit-in-place (ไม่ใช่ wizard แต่ไม่ใช่ modal) = borderline ยังไม่ออกแผน, modal ใน `AssignmentDetailPage` (Extend Due Date/Add Learners) เป็น action เฉพาะกิจ ถือว่าโอเค
- ไฟล์หลักที่แตะ: `DOC/PLANS/PLAN-005-learner-group-categories-wizard.md` (เอกสารล้วน)
- Contract ที่เปลี่ยน: ไม่มี
- Verified: n/a (planner ไม่แก้โค้ด)

## [2026-06-12 16:50] Antigravity — Implement PLAN-004 เปลี่ยนหน้าจัดการผู้ใช้ระบบเป็นแบบ Wizard
- ทำอะไร: สร้างหน้าสำหรับเพิ่ม/แก้ไขผู้ใช้ระบบแบบ Wizard ([UserEditorPage.tsx](file:///c:/Users/n4734/source/repos/iLearn2/iLearn.Admin.React/src/pages/users/UserEditorPage.tsx)) โดยในโหมดสร้างผู้ใช้ใหม่จะมี 3 ขั้นตอน (ระบุ NID -> เลือก Roles -> รีวิว) และในโหมดแก้ไขจะมี 2 ขั้นตอน (เลือก Roles -> รีวิว); นำพาเนล slide-over เดิมออกจาก [AdminUsersPage.tsx](file:///c:/Users/n4734/source/repos/iLearn2/iLearn.Admin.React/src/pages/users/AdminUsersPage.tsx) เปลี่ยนเป็นการ navigate ไปยัง Wizard แทน; เพิ่มเส้นทางใน [App.tsx](file:///c:/Users/n4734/source/repos/iLearn2/iLearn.Admin.React/src/App.tsx) (พร้อมครอบ Remount และ RequireRole) และกำหนด Breadcrumbs segment สำหรับ users ใน [Breadcrumbs.tsx](file:///c:/Users/n4734/source/repos/iLearn2/iLearn.Admin.React/src/components/layout/Breadcrumbs.tsx)
- ไฟล์หลักที่แตะ: `iLearn.Admin.React/src/pages/users/UserEditorPage.tsx`, `iLearn.Admin.React/src/pages/users/AdminUsersPage.tsx`, `iLearn.Admin.React/src/App.tsx`, `iLearn.Admin.React/src/components/layout/Breadcrumbs.tsx`, `DOC/PLANS/PLAN-004-admin-users-wizard-editor.md`, `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน: ไม่มี (การเรียกใช้ API endpoint ยังรักษา Contract รูปแบบเดิมของ DevExtreme)
- Verified: `npm run lint` ผ่าน, `npm run build` ผ่าน, `dotnet test` (112/112 passed) ผ่านเรียบร้อย

## [2026-06-12 16:45] Claude Code — เขียน PLAN-004 (Admin Users new/edit → Wizard)
- ทำอะไร: ผู้ใช้ชี้ว่า `/users` ทำ create/edit ผ่าน slide-over panel ไม่ตรงมาตรฐาน wizard ของระบบ — สำรวจ `AdminUsersPage.tsx` + `UsersCRUDController.cs` (Post คืน entity พร้อม id → create แล้ว assign roles ต่อใน flow เดียวได้) แล้วเขียน **PLAN-004** (Assigned: Gemini): ไฟล์ใหม่ `UserEditorPage.tsx` (create 3 steps: User→Roles→Review, edit 2 steps: Roles→Review), ตัด panel ใน `AdminUsersPage`, เพิ่ม routes `/users/new` + `/users/:id/edit` (Remount + RequireRole), เติม `'users'` ใน Breadcrumbs SEGMENT_MAP
- ไฟล์หลักที่แตะ: `DOC/PLANS/PLAN-004-admin-users-wizard-editor.md` (เอกสารล้วน — ยังไม่แก้โค้ด)
- Contract ที่เปลี่ยน: ไม่มี
- Verified: n/a (planner ไม่แก้โค้ด)

## [2026-06-12 16:30] Claude Code — Review + ปิด PLAN-002 และ PLAN-003 เป็น VERIFIED
- ทำอะไร: รีวิว diff ทั้งสองแผน — **PLAN-002** (GPT, dead code cleanup): ลบ branch `LearnerGroupsCRUD` ครบทุกจุด, คง `ContentItemsCRUD`+`createRestDataSource` ตามข้อห้าม, import ไม่เหลือ dead, ป้ายวันที่ folder=Created/group=Updated; **PLAN-003** (Gemini, sidebar): Learning เหลือ 3 รายการ, ย้าย Content Library+Learners ไป Operations, path/icon เดิม ไม่แตะ App.tsx/Super Admin — ปรับสถานะทั้งคู่เป็น VERIFIED
- ไฟล์หลักที่แตะ: `DOC/PLANS/PLAN-002-*.md`, `DOC/PLANS/PLAN-003-*.md` (เอกสาร — code เปลี่ยนโดย implementer แล้ว)
- Contract ที่เปลี่ยน: ไม่มี
- Verified: รันซ้ำเอง `npm run lint` (0 error / 10 warning baseline), `npm run build` ผ่าน

## [2026-06-12 16:20] Antigravity — ปรับกลุ่ม Learning และ Operations ใน sidebar ตามแผน PLAN-003
- ทำอะไร: ย้ายเมนู Content Library และ Learners จากกลุ่ม Learning ไปไว้ในกลุ่ม Operations ใน sidebar เพื่อให้สอดคล้องกับโครงสร้างฟังก์ชันการทำงาน และเหลือเฉพาะแกนหลักของการเรียนในกลุ่ม Learning
- ไฟล์หลักที่แตะ: `iLearn.Admin.React/src/config/navigation.ts`, `DOC/PLANS/PLAN-003-sidebar-learning-group.md`, `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน: ไม่มี
- Verified: `npm run lint` ผ่าน, `npm run build` ผ่าน, `dotnet test` ผ่านเรียบร้อย

## [2026-06-12 16:15] Claude Code — ปิด PLAN-001 เป็น CANCELLED (ยืนยันโดยผู้ใช้)
- ทำอะไร: ผู้ใช้ยืนยันว่าไม่ต้องการ Metadata step ในโหมด create ของหน้า Upload SCORM — ปิด PLAN-001 ถาวรเป็น CANCELLED (Gemini ตั้งสถานะ + เขียน Implementer Notes ไว้แล้ว ผมเติมบรรทัดยืนยัน planner) บั๊กเลือก Content Type ตอน upload ถือว่าไม่แก้ตามความต้องการล่าสุด
- ไฟล์หลักที่แตะ: `DOC/PLANS/PLAN-001-content-upload-type-step.md` (เอกสารล้วน)
- Contract ที่เปลี่ยน: ไม่มี
- Verified: n/a

## [2026-06-12 16:10] Claude Code — เขียน PLAN-003 (ปรับกลุ่ม Learning ใน sidebar)
- ทำอะไร: ผู้ใช้ขอให้กลุ่ม Learning ใน sidebar เหลือแค่ Courses/Assignments/Learner Groups — เขียน **PLAN-003** (Assigned: Gemini) ให้ย้าย Content Library + Learners ไปกลุ่ม Operations (route เดิม ไม่ลบ) แก้ไฟล์เดียว `navigation.ts`
- ไฟล์หลักที่แตะ: `DOC/PLANS/PLAN-003-sidebar-learning-group.md` (เอกสารล้วน — ยังไม่แก้โค้ด)
- Contract ที่เปลี่ยน: ไม่มี
- Verified: n/a (planner ไม่แก้โค้ด)

## [2026-06-12 15:35] Antigravity — ยกเลิกขั้นตอน Metadata ในโหมด Create และลบ Dead Code ตามคำสั่งผู้ใช้
- ทำอะไร: ยกเลิกการเพิ่มขั้นตอน Metadata ในโหมด Create ของหน้า Upload SCORM กลับมาเป็น 2 ขั้นตอน (Package Upload -> Review) ตามเดิม พร้อมเคลียร์โค้ดที่ไม่ได้ใช้ (Dead Code) ใน `ContentItemEditorPage.tsx` เช่น การเรียกใช้ PUT endpoint `admin/ContentItemsCRUD/Put` เมื่ออัปโหลดไฟล์ และปรับปรุงเงื่อนไข `validateMetadata` กับ `renderMetadataStep` ให้คลีนยิ่งขึ้น
- ไฟล์หลักที่แตะ: `iLearn.Admin.React/src/pages/content-library/ContentItemEditorPage.tsx`, `DOC/PLANS/PLAN-001-content-upload-type-step.md`, `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน: ไม่มี
- Verified: `npm run lint` ผ่าน, `npm run build` ผ่าน, `dotnet test` ผ่านเรียบร้อย

## [2026-06-12 15:33] GitHub Copilot (GPT-5.3-Codex) — Implement PLAN-002 เก็บ dead code หลังย้าย Learner Groups ไป Explorer
- ทำอะไร: ลบ config ที่ไม่ถูกใช้งาน `adminListConfigs.learnerGroups`; ลบ branch dead code `LearnerGroupsCRUD` ใน `EntityListPage` ครบ (lookup categories, rest datasource branch, route prefix branch, grid action, crudControllers set); ปรับ Explorer คอลัมน์วันที่ให้สื่อความหมายตรง (`Created` สำหรับโฟลเดอร์, `Updated` สำหรับกลุ่ม); อัปเดตสถานะแผน `DOC/PLANS/PLAN-002-learner-group-explorer-cleanup.md` เป็น `DONE` พร้อม checklist/notes
- ไฟล์หลักที่แตะ: `iLearn.Admin.React/src/pages/moduleConfigs.ts`, `iLearn.Admin.React/src/pages/EntityListPage.tsx`, `iLearn.Admin.React/src/pages/learner-groups/LearnerGroupListPage.tsx`, `DOC/PLANS/PLAN-002-learner-group-explorer-cleanup.md`, `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน: ไม่มี
- Verified: `npm run lint` ผ่าน (warning baseline เดิม 10 รายการ, ไม่มี error), `npm run build` ผ่าน; smoke browser เปิด `/assignments`, `/content-library`, `/master-data/divisions`, `/learner-groups` ได้

## [2026-06-12 15:52] Antigravity — เพิ่ม Metadata step ในโหมด Create ของหน้า Upload SCORM ตามแผน PLAN-001
- ทำอะไร: ปรับปรุง `ContentItemEditorPage.tsx` ในโหมด Create ให้มีขั้นตอนเลือก Metadata (Display Name + Content Type Learn/Exam) เป็น Step แรก ก่อนอัปโหลดไฟล์ SCORM เพื่อสนับสนุนการระบุ Content Type ในตอนอัปโหลดโดยตรง และกรอกแบบฟอร์มตรวจสอบข้อมูลเสร็จสิ้นใน 3 ขั้นตอน
- ไฟล์หลักที่แตะ: `iLearn.Admin.React/src/pages/content-library/ContentItemEditorPage.tsx`, `DOC/PLANS/PLAN-001-content-upload-type-step.md`, `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน: ไม่มี
- Verified: `npm run lint` ผ่าน, `npm run build` ผ่าน, `dotnet test` (112/112 tests passed) ผ่านเรียบร้อย

## [2026-06-12 15:50] Claude Code — ตั้งระบบ Planner/Implementer + แผน 2 ใบแรก
- ทำอะไร: ผู้ใช้กำหนดบทบาทใหม่: Claude = planner/reviewer, Gemini + GPT = implementer — สร้าง `DOC/PLANS/` (README + template + workflow READY→DONE→VERIFIED), เขียนแผนพร้อมทำ 2 ใบ: **PLAN-001** (Assigned: Gemini — หน้า Upload SCORM โหมด create เลือก Content Type ไม่ได้เพราะ wizard ข้าม Metadata step), **PLAN-002** (Assigned: GPT — ลบ dead code `adminListConfigs.learnerGroups` + branch LearnerGroupsCRUD ใน EntityListPage + ป้ายวันที่โฟลเดอร์), อัปเดตบทบาทใน `CLAUDE.md`
- ไฟล์หลักที่แตะ: `CLAUDE.md`, `DOC/PLANS/README.md`, `DOC/PLANS/PLAN-001-*.md`, `DOC/PLANS/PLAN-002-*.md`
- Contract ที่เปลี่ยน: ไม่มี (เอกสารล้วน)
- Verified: n/a (ไม่มีโค้ดเปลี่ยน)

## [2026-06-12 15:30] Claude Code — Review งานชุด Learner Group Explorer + แก้บั๊ก deep-link
- ทำอะไร: รีวิว commits 75d79a7..e2c7a44 ทั้งชุด (Explorer แบบโฟลเดอร์, breadcrumb custom trail, AppTableSearch/Footer ใหม่, backend filter) — พบและแก้บั๊ก 1 ตัว: เปิด `/learner-groups?categoryId=N` ตรง ๆ หรือ refresh ระหว่างอยู่ในโฟลเดอร์จะถูกเด้งกลับ root เพราะ effect ที่ validate categoryId ทำงานก่อน categories โหลดเสร็จ → เพิ่ม guard `loading || categories.length === 0` (ยืนยันใน browser แล้วทั้ง id จริง=อยู่ต่อ และ id ปลอม=ถูกดีดตามเดิม)
- ไฟล์หลักที่แตะ: `iLearn.Admin.React/src/pages/learner-groups/LearnerGroupListPage.tsx`
- Contract ที่เปลี่ยน: ไม่มี
- ข้อสังเกตค้างไว้ (ยังไม่แก้): `adminListConfigs.learnerGroups` ใน moduleConfigs ไม่มีผู้ใช้แล้ว; `PaginationParams.CategoryId`/`RootCategoryOnly` ฝั่ง backend ยังไม่มี frontend เรียกใช้ (Explorer กรองแบบ client-side)
- Verified: tsc 0 error, eslint 0 error (warning baseline 10), vite build ผ่าน, dotnet test 112/112

## [2026-06-12 13:14] GitHub Copilot (GPT-5.3-Codex) — ลดจุดแสดงจำนวน record ใน grid ให้เหลือจุดเดียว
- ทำอะไร: ปรับ `AppTableFooter` ให้ไม่แสดงข้อความ `Showing X of Y records` ซ้ำกับส่วนบนของ grid อีกต่อไป โดยคงเฉพาะสถานะโหลดด้านล่าง (`Loading more...`, `Scroll down to load more`, `All records loaded`) ทำให้จุดแสดงจำนวน record เหลือจุดเดียวที่ toolbar/search ด้านบน
- ไฟล์หลักที่แตะ: `iLearn.Admin.React/src/components/ui/table/AppTableFooter.tsx`, `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน: ไม่มี
- Verified: `npm run lint` ผ่าน (warning baseline เดิม 10 รายการ, ไม่มี error), `npm run build` ผ่าน

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
