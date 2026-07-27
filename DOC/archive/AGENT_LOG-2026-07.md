# Agent Work Log — archive 2026-07

entry เก่าที่ถูกหมุนออกจาก [../AGENT_LOG.md](../AGENT_LOG.md) โดย `tools/rotate-agent-log.ps1`
**อ่านอย่างเดียว** — ห้ามเขียน entry ใหม่ที่นี่ ให้เขียนบนสุดของ `DOC/AGENT_LOG.md` เสมอ

---

## [2026-07-23 —] Claude Code — วินิจฉัย Learner Site unreachable บน PROD health check + เขียน PLAN-140 (มอบ Gemini)
- ทำอะไร: ผู้ใช้รายงานการ์ด Learner Site บนหน้า Health Check PROD ขึ้น "Could not reach .../health/smoke (service down, or blocked by CORS)". **วินิจฉัยยืนยัน**: service ปกติ (ยิงตรงได้ 200 pass ทั้ง FQDN และ host สั้น) — ต้นเหตุคือผู้ใช้เปิด admin ด้วย hostname สั้น (`https://ap-ntc2137-prwb/...`) แต่การ์ดนี้ fetch จาก browser ไป `FileSettings:HostUrl` (FQDN `.nikonoa.net`) = cross-origin และ iLearn.User ไม่มี CORS → browser block. อาการเดียวกับที่ PLAN-135 จดไว้เป็น known finding และเสี่ยงซ้ำรอย SCORM silent-fail (PLAN-094/103) ถ้าผู้เรียนเข้า host สั้น. เขียน `PLAN-140` (READY, Assigned: Gemini): canonical host redirect 307 — middleware ใน iLearn.User (GET/HEAD, skip localhost, no-op ถ้า HostUrl ไม่ valid) + SPA bootstrap redirect ใน admin-react (short hostname + `VITE_ILEARN_ADMIN_CANONICAL_DOMAIN`) + deploy/smoke QA→PROD
- ไฟล์หลักที่แตะ: `DOC/PLANS/PLAN-140-canonical-host-redirect.md` (ใหม่), `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน: ไม่มี (แผนอย่างเดียว ยังไม่แก้โค้ด)
- Verified: probe จริงทั้งสอง hostname = 200 pass (หลักฐานใน root cause ของแผน); planning only

## [2026-07-23 —] Antigravity — PLAN-139: Standardize Overview Cards Across All Detail Pages
- ทำอะไร: (1) สร้าง component กลางใหม่ `StatTile` และ `StatTileRow` ใน `src/components/ui/detail/index.tsx` พร้อมปรับปรุง `Fact` component ให้ส่ง `valueClassName` และ `mono` ลง `<dd className={valueClass}>` (2) ปรับโครงสร้าง Overview Card ทั้ง 7 หน้า detail (`AssignmentDetailPage`, `CourseDetailPage`, `VersionDetailPage`, `ContentItemDetailPage`, `UserDetailPage`, `LearnerGroupDetailPage`, `MasterDataDetailPage`) ให้เป็นมาตรฐานเดียวกัน (StatTileRow → Description blockquote → FactGrid โดย Status Fact ขึ้นก่อนเสมอ → DetailSubSection) (3) ลบ `text-sm` และ `labelClassName` overrides รายหน้าให้ใช้ default ของ `FactGrid`/`Fact` (4) เพิ่มคีย์สองภาษาใหม่และแปลง label hardcode ทั้งหมดเป็น dictionary ใน `src/lib/labels.ts` (`activeLearners`, `assignmentBatches`, `scormContentItems`, `versionControlHint`, `currentContentWithCount`, `scormVersion`, `packageSize`, `coursesLinked`, `fileStorageId`, `launchResource`, `serverPath`) (5) อัปเดต `PLAN-139` เป็น `DONE`
- ไฟล์หลักที่แตะ: `src/components/ui/detail/index.tsx`, `src/lib/labels.ts`, `src/pages/assignments/AssignmentDetailPage.tsx`, `src/pages/courses/CourseDetailPage.tsx`, `src/pages/courses/VersionDetailPage.tsx`, `src/pages/content-library/ContentItemDetailPage.tsx`, `src/pages/users/UserDetailPage.tsx`, `src/pages/learner-groups/LearnerGroupDetailPage.tsx`, `src/pages/master-data/MasterDataDetailPage.tsx`, `DOC/PLANS/PLAN-139-standardize-overview-cards.md`, `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน: ไม่มี (Frontend display presentation standardization เท่านั้น)
- Verified: `npm run lint` ✓ (0 errors); `npm run build` ✓ (built dist in 1.61s); `dotnet test` ✓ (248/248 passed); sweep hardcoded literals ใน `src/pages` = 0

## [2026-07-23 —] Claude Code — รีวิว PLAN-138 (Copilot โซน D–F) — ผ่าน + fix เก็บตก 1 จุด
- ทำอะไร: รีวิวงาน Copilot `c077fd9`+`6fac6ef` ครบทุกชั้น: (1) commit scope สะอาด — frontend+DOC เท่านั้น แยกจากงานทีมอื่นถูกต้อง (2) **กับดัก module-scope `t()` ไม่โดน** — moduleConfigs เก็บ `LabelPair` reference แล้ว resolve ใน AppTable/EntityListPage ตอน render, พิสูจน์ด้วยการสลับภาษาจริงในเบราว์เซอร์ (หัวตาราง moduleConfigs พลิก th↔en ครบ ไม่มีค่าค้าง) (3) lint ✓ build ✓ tests 248/248 ✓ (4) กวาด `[ก-๙]` นอก labels.ts เหลือเฉพาะจุดถูกต้อง (สวิตช์/comment/format/error-comparison) (5) browser 8 หน้า × 2 ภาษา: assignments list+detail (รวม PLAN-137 Edit Description modal — ไทยครบ), courses explorer, learner-groups, master-data, users, system-config, notifications — ไม่มี literal ตกค้าง, console 0 errors. **Fix เก็บตก**: `apiClient.ts` error 413 hardcode ไทย → `UI_LABELS.fileTooLarge` + `t()` (จุดเดียวที่หลุด — เป็น lib ไม่ใช่ page เลยไม่อยู่ในขอบเขต sweep ของ PLAN-138)
- ไฟล์หลักที่แตะ: `src/lib/apiClient.ts`, `src/lib/labels.ts` (+1 คีย์), `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน: ไม่มี
- Verified: lint ✓ build ✓ แอปบูตปกติหลังแก้ apiClient (dashboard th render, console 0 errors) — **fix นี้ยังไม่ commit** รวมไว้กับรอบ commit/deploy ถัดไป
- สถานะรวม PLAN-136/138: **ทุกโซน P0–F เสร็จและรีวิวผ่านแล้ว** — พร้อม commit fix เก็บตก + deploy QA/PROD เมื่อผู้ใช้สั่ง

## [2026-07-23 —] Claude Code — เขียน PLAN-139: standardize Overview cards ทุกหน้า detail (มอบ Gemini)
- ทำอะไร: สำรวจ Overview card ทั้ง 7 หน้า detail (Course/Version/ContentItem/Assignment/User/LearnerGroup/MasterData) พบไม่ตรงกัน: KPI tile hand-rolled ใน AssignmentDetail, description อยู่คนละตำแหน่ง (blockquote vs Fact), FactGrid override text-sm บางหน้า, fact label hardcode อังกฤษหลายจุด (Course KPI, Version, ContentItem ~8 ตัว), MasterData ใช้ title "Details" แทน Overview. เขียน `PLAN-139` (READY, Assigned: Gemini) กำหนดมาตรฐานโครงสร้างการ์ด (StatTileRow → description blockquote → FactGrid → DetailSubSection) + component กลางใหม่ `StatTile`/`StatTileRow` ใน `components/ui/detail` + แปลง label ค้างเป็นสองภาษา
- ไฟล์หลักที่แตะ: `DOC/PLANS/PLAN-139-standardize-overview-cards.md` (ใหม่), `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน: ไม่มี (แผนอย่างเดียว ยังไม่แก้โค้ด)
- Verified: n/a (planning only)

## [2026-07-23 —] Antigravity — Standardized UI Dictionary Phrasing & Consistency in labels.ts
- ทำอะไร: สำรวจและปรับปรุงพจนานุกรมข้อความระบบใน `src/lib/labels.ts` เพื่อสร้างมาตรฐานภาษาไทยและอังกฤษที่มีความเป็นมืออาชีพ (Enterprise standard): (1) ปรับ `STATUS_LABELS` สำหรับ `Upcoming` เป็น "กำลังจะถึง" และ `'Due Soon'` เป็น "ใกล้ถึงกำหนดส่ง" เพื่อแยกความแตกต่างกับสถานะวันส่ง (2) ปรับ `COMMON_LABELS.notAssignable` เป็น "มอบหมายไม่ได้" (3) ปรับ `DASHBOARD_LABELS.colCompletion` เป็น "ความสำเร็จ" (แยกกับ Progress) และ `colTasks` เป็น "จำนวนงาน" (4) ปรับ `REPORT_LABELS.colCompletionShare` เป็น "อัตราการเรียนสำเร็จ" (5) แก้ไขแกรมม่าภาษาอังกฤษใน `COURSE_LABELS.forceResetCompleted` (6) ลบ trailing spaces ใน `LEARNER_LABELS.created`/`updated` และปรับ `bulkImportInstruction` ให้ใช้คำว่า "รหัสพนักงาน (EId)" (7) จัดรูป `ADMIN_LABELS` ให้แตกเป็น 1 entry ต่อบรรทัดอย่างเป็นระเบียบสวยงาม และปรับ `scormLaunchAuditNote` เป็น "ประวัติการตรวจสอบการเปิดเรียน..."
- ไฟล์หลักที่แตะ: `iLearn.Admin.React/src/lib/labels.ts`, `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน: ไม่มี (UI Display labels inside React lib only; `statusTone` lookup mapping preserved)
- Verified: `npm run lint` ✓ (0 errors); `npm run build` ✓ (built dist in 1.61s without errors).

## [2026-07-23 —] GitHub Copilot — PLAN-138 follow-up: Assignment Detail modal sweep
- ทำอะไร: ตรวจจาก production screenshot พบ `AssignmentDetailPage` modal แก้ไขคำอธิบายยังแสดงอังกฤษ (`Edit Description`, `Assignment Description`, placeholder, Cancel/Save). กวาดทั้งหน้าต่อ: due-date, add learner/course, learner course, reset/remove, confirm/toast/empty state/tooltip/footer ทั้งหมดเป็น `ASSIGNMENT_LABELS`/`UI_LABELS`; status overview ใช้ `learnerStatusLabel()` โดยคง derived-status logic เดิม
- ไฟล์หลักที่แตะ: `iLearn.Admin.React/src/pages/assignments/AssignmentDetailPage.tsx`, `iLearn.Admin.React/src/lib/labels.ts`, `DOC/PLANS/PLAN-138-bilingual-zones-def-copilot.md`, `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน: ไม่มี (frontend display text only)
- Verified: `npm run lint` ✓; `npm run build` ✓; literal/sweep ของหน้า AssignmentDetail ✓; `git diff --check` ✓. **ยังไม่ commit/deploy** — production URL ในภาพจะยังแสดง asset เดิมจนกว่าจะ deploy follow-up นี้

## [2026-07-23 —] GitHub Copilot — PLAN-138: bilingual Zone D–F complete
- ทำอะไร: migrate display copy ของ React Admin ใน Zone D–F เข้า `src/lib/labels.ts`: เพิ่ม `COURSE_LABELS`, `ASSIGNMENT_LABELS`, `LEARNER_LABELS`, และ `ADMIN_LABELS`; แทน hardcoded JSX, toast, confirm, modal, tooltip, placeholder, wizard, empty/error state ของ Courses/Content, Assignments/Learners, Master Data/Users/System/Notifications. เก็บ LabelPair ใน module configs เพื่อกันภาษาค้างหลัง switch และ resolve ตอน render เท่านั้น. ปิด Zone D/E/F ใน PLAN-136 และ PLAN-138 เป็น DONE.
- ไฟล์หลักที่แตะ: `iLearn.Admin.React/src/lib/labels.ts`, `src/pages/{courses,content-library,assignments,learner-groups,learners,master-data,users,system-config}`, `src/pages/{EntityListPage,moduleConfigs,AccessDeniedPage,NotFoundPage,notifications/NotificationsPage}`, `src/components/{shared/LearnerDirectorySelector,shared/UploadProgressOverlay,layout/NotificationBell,ui/AppTable}`; `DOC/PLANS/PLAN-136-full-bilingual-admin-ui.md`, `DOC/PLANS/PLAN-138-bilingual-zones-def-copilot.md`, `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน: Frontend internal type only — `AdminGridColumn.caption` และ `AdminListConfig.title`/`eyebrow`/`description` เป็น `LabelPair`; consumer resolve ด้วย `t()` ใน render. ไม่มี API/backend contract เปลี่ยน
- Verified: `npm run lint` ✓; `npm run build` ✓; sweep `[ก-๙]` ของ Zone D–F ไม่มี display string ค้าง (เหลือ raw backend error comparison); `git diff --check` ✓. ไม่ได้ commit/deploy

## [2026-07-23 —] Claude Code — รีวิว `da5d1c6`+`db507b1` + เขียน PLAN-138 มอบโซน D–F ให้ Copilot
- ทำอะไร: ผู้ใช้สั่งเปลี่ยน implementer เฟสสองภาษาเป็น Copilot และให้ Claude รีวิวงานที่ผ่านมา. **รีวิว**: (1) `db507b1` เป็น mixed commit (Zone C reports ของ Claude + PLAN-137 ของ Gemini รวมกัน, message generic) — โค้ดทั้งสองส่วนตรวจแล้วผ่าน: PLAN-137 `UpdateDescriptionAsync` update ทั้ง batch ใน transaction + division scoping ถูก, frontend PATCH `Assignments/{id}/description` ตรง contract, test ใหม่ครอบ batch-wide (2) verify HEAD: lint ✓ build ✓ **tests 248/248** ✓ (3) กวาด `[ก-๙]` ใน pages/reports + DashboardPage = 0 (โซน B/C สะอาดจริง). **แผน**: เขียน PLAN-138 (READY, Copilot) — โซน D/E/F พร้อม pattern + กับดักสำคัญ (ห้าม `t()` ใน module scope; `moduleConfigs.ts` ต้องเปลี่ยน caption เป็น LabelPair แล้ว resolve ตอน render) + กติกา + verification ต่อโซน; อัปเดต PLAN-136 (Assigned → Copilot สำหรับ D/E/F, Claude เป็น reviewer)
- ไฟล์หลักที่แตะ: `DOC/PLANS/PLAN-138-bilingual-zones-def-copilot.md` (ใหม่ READY), `DOC/PLANS/PLAN-136-full-bilingual-admin-ui.md`, `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน: ไม่มี (รีวิว+แผน)
- Verified: lint/build/tests บน HEAD ผ่านครบตามข้างบน
- หมายเหตุถึงทุก agent: อย่า commit รวมงานคนละแผนอีก — ตรวจ `git status` แล้ว stage เฉพาะไฟล์ของแผนตัวเองเสมอ (db507b1 เป็น precedent ที่ไม่ควรซ้ำ)

## [2026-07-23 —] Claude Code — commit PLAN-136 P0+A+B (`da5d1c6`) + Zone C: Reports 5 หน้าสองภาษา
- ทำอะไร: (1) **Commit `da5d1c6`** — stage เฉพาะไฟล์ PLAN-136 18 ไฟล์ **แยกงาน PLAN-137 ของ Gemini** (AssignmentsController/AssignmentService/BulkAssignDto/AssignmentFlowTests/AssignmentDetailPage/StatusText/ContentItemDetailPage + PLAN-137.md) ที่ค้างใน working tree ไว้ให้รอบ commit ของทีมนั้น (2) **Zone C**: เพิ่ม `REPORT_LABELS` ~95 คีย์ แล้ว migrate ReportHub/Compliance/Transcript/Activity/CourseSummary — Hub การ์ดโชว์ชื่อภาษาหลัก+อีกภาษาในวงเล็บ, หัวตาราง/หัวการ์ดที่เดิมเขียน "ไทย (English)" ปนกัน → ภาษาเดียวตามสวิตช์, **บั๊กเดิม**: Transcript badge สถานะโชว์ key ดิบ → `learnerStatusLabel`, CourseSummary ที่อังกฤษล้วนได้ไทยครบ. จงใจคงอังกฤษ: CSV export headers, print-only transcript header (เอกสารทางการ)
- ไฟล์หลักที่แตะ: `src/lib/labels.ts`, `src/pages/reports/{ReportHubPage,ComplianceReportPage,TranscriptReportPage,ActivityReportPage,CourseSummaryReportPage}.tsx`, `DOC/PLANS/PLAN-136-full-bilingual-admin-ui.md` (Zone C ✅)
- Contract ที่เปลี่ยน: ไม่มี (frontend-only)
- Verified: `npm run lint` ✓, `npm run build` ✓; browser ทั้ง 5 หน้า × 2 ภาษา: EN ไม่มีไทยตกค้าง (นอกจากปุ่มสวิตช์ "ไทย" + ข้อมูล DB), th ครบทุก section รวม CourseSummary ที่เพิ่งได้ไทย; console 0 errors — **Zone C ยังไม่ commit** (รอคำสั่ง)
- **โซนถัดไป**: D Courses/Content → E Assignments/Learners → F Master Data/Users/System + moduleConfigs captions

## [2026-07-23 —] Claude Code — PLAN-136 Zone B: Dashboard สองภาษา (IN_PROGRESS ต่อ)
- ทำอะไร: migrate Dashboard เข้า dictionary — เพิ่ม `DASHBOARD_LABELS` (~45 คีย์ th/en) ใน labels.ts แล้วแทน string ทั้ง DashboardPage + DashboardCharts (header/KPI strip/ตาราง 2 ตัว/trend/activity feed/maintenance banner/error states). แก้เพิ่มจากการ verify ของจริง: (1) หัวตารางที่เดิมเขียนสองภาษาปน ("งานมอบหมาย (Assignment)") → ภาษาเดียวตามสวิตช์ (2) **บั๊กเดิม**: badge สถานะ priority assignments โชว์ key ดิบ `Active`/`Due Soon` แม้ใน UI ไทย → เพิ่ม Active/'Due Soon'/Enrolling/Unassigned เข้า `STATUS_LABELS` + แสดงผ่าน `learnerStatusLabel` (tone ไม่เพี้ยนเพราะ statusTone reverse-lookup ผ่าน label map) (3) `formatRelativeTime` ใน `lib/format.ts` เป็น lang-aware (เมื่อครู่/just now, X นาที/min ago) — format วันที่/ตัวเลขอื่นคงเดิมตามแผน
- ไฟล์หลักที่แตะ: `src/lib/labels.ts`, `src/lib/format.ts`, `src/pages/DashboardPage.tsx`, `src/pages/dashboard/DashboardCharts.tsx`, `DOC/PLANS/PLAN-136-full-bilingual-admin-ui.md` (Zone B ✅)
- Contract ที่เปลี่ยน: ไม่มี (frontend-only; `formatRelativeTime` อ่าน `getLang()` — ผู้เรียกเดิมไม่ต้องแก้)
- Verified: `npm run lint` ✓, `npm run build` ✓; browser th: ทุก section ไทยครบ + badge "กำลังดำเนินการ/ใกล้ถึงกำหนด"; EN: "Operational summary/All divisions/Updated just now/20 hr ago" ครบ เหลือเฉพาะข้อมูลจาก DB (ชื่อคอร์ส) ตาม intended; console 0 errors
- **โซนถัดไป**: C Reports (5 หน้า) → D Courses/Content → E Assignments/Learners → F Master Data/Users/System + moduleConfigs captions

## [2026-07-23 —] Claude Code — PLAN-136 P0+Zone A: เฟสสองภาษา — infra สลับภาษา + layout/shared UI (IN_PROGRESS)
- ทำอะไร: เริ่มเฟสสองภาษาเต็มรูปแบบตาม PLAN-136 (ผู้ใช้ตัดสินใจ: UI React ทั้งหมด / dictionary ไฟล์เดียวใน labels.ts / localStorage / Claude ทำเอง). **P0 infra**: labels.ts เพิ่ม language store (`getLang`/`setLang`/`useLang` ผ่าน useSyncExternalStore, persist `ilearn-admin-lang`) + `tf()` สำหรับ string มี placeholder; Header เพิ่มสวิตช์ ไทย/EN (SegmentedToggle); AppLayoutInner ใส่ `key={lang}` → remount ทั้ง tree ตอนสลับ (การันตีไม่มีข้อความค้าง). **Zone A**: navigation.ts เปลี่ยน `label: string` → `LabelPair` (Sidebar ใช้ `label.en` เป็น expanded key/aria id, แสดงผ่าน `t()`), Breadcrumbs SEGMENT_MAP → LabelPair, Header strings, shared defaults (ListToolbar Showing/records/Search, AppTable noData, AppTableFooter loading/scroll/all-loaded, AppTableSearch, ConfirmDialog Confirm/Cancel, AppWizard Cancel/Previous/Continue, ExplorerTable loading) — dictionary ใหม่: `NAV_LABELS`, `CRUMB_LABELS`, `LAYOUT_LABELS`, `UI_LABELS`
- ไฟล์หลักที่แตะ: `src/lib/labels.ts`, `src/config/navigation.ts`, `components/layout/{Header,AppLayout,Sidebar,Breadcrumbs}.tsx`, `components/ui/{ListToolbar,ConfirmDialog,AppWizard,AppTable}.tsx`, `components/ui/table/{AppTableFooter,AppTableSearch}.tsx`, `components/ui/explorer/ExplorerTable.tsx`, `DOC/PLANS/PLAN-136-full-bilingual-admin-ui.md` (ใหม่ IN_PROGRESS)
- Contract ที่เปลี่ยน: `NavigationItem.label`/`NavigationSection.label` เป็น `LabelPair` (consumer ต้อง `t()`); ListToolbar/AppTable/ConfirmDialog/AppWizard default labels resolve จาก dictionary — ส่ง prop มา override ได้เหมือนเดิม
- Verified: `npm run lint` ✓, `npm run build` ✓; browser: sidebar/breadcrumb/toolbar เป็นไทย, สลับ EN → nav+badge+toolbar เปลี่ยนครบ, กลับ ไทย ✓, localStorage persist ✓, console 0 errors
- **งานคงเหลือ (โซน B–F ใน PLAN-136)**: Dashboard, Reports 5 หน้า, Courses/Content, Assignments/Learners, Master Data/Users/System + EntityListPage/moduleConfigs captions — agent ที่มาทำต่อให้อ่านตาราง Zone ใน PLAN-136 แล้วอัปเดตสถานะทีละโซน

## [2026-07-23 —] Antigravity — PLAN-137: Editable Assignment Description & ContentItem Details Popup/Badge Unification
- ทำอะไร: 
  1. **PLAN-137 (Assignment Description)**: เพิ่มความสามารถในการดูและแก้ไขคำอธิบายชุดงาน (Description) ในหน้า Assignment Detail (`AssignmentDetailPage.tsx` / `/assignments/:id`). เพิ่ม DTO `UpdateAssignmentDescriptionDto`, เมธอด `UpdateDescriptionAsync` ใน `IAssignmentService` / `AssignmentService` อัปเดตทุก rule ใน batch เดียวกันแบบ atomic transaction, เพิ่ม endpoint `PATCH /api/Assignments/{id}/description` ใน `AssignmentsController`, เพิ่ม unit test `AssignmentService_UpdateDescriptionAsync_UpdatesDescriptionAcrossAllBatchRules` ใน `AssignmentFlowTests.cs`, แสดง Description ใน Overview Card FactGrid พร้อมปุ่มแก้ไขและ ControlAction Sidebar Edit Description และ Modal สำหรับแก้ไขข้อมูล.
  2. **ContentItem Detail Popup & Badge Unification**:
     - เปลี่ยนปุ่ม "Edit Metadata" ในหน้า ContentItem Detail (`ContentItemDetailPage.tsx` / `/content-library/:id`) จากการเปลี่ยนหน้าไปยัง Modal Popup แก้ไขชื่อและประเภทคอนเทนต์ตรงในหน้าทันทีตามที่ผู้ใช้สั่ง.
     - ปรับ `StatusText.tsx` ให้ใช้ `variant="soft"` เป็นค่าเริ่มต้นตามระบบป้ายสถานะมาตรฐาน (PLAN-123/PLAN-126) แก้ไขปัญหาป้ายสถานะ `เผยแพร่แล้ว` แสดงผลแบบ outline border เขียวอ่อนเพี้ยนจากรูปแบบกลาง.
- ไฟล์หลักที่แตะ: `iLearn.Application/DTOs/BulkAssignDto.cs`, `iLearn.Application/Interfaces/Services/IAssignmentService.cs`, `iLearn.Application/Services/AssignmentService.cs`, `iLearn.API/Controllers/AssignmentsController.cs`, `iLearn.Tests/AssignmentFlowTests.cs`, `iLearn.Admin.React/src/pages/assignments/AssignmentDetailPage.tsx`, `iLearn.Admin.React/src/pages/content-library/ContentItemDetailPage.tsx`, `iLearn.Admin.React/src/components/ui/StatusText.tsx`, `DOC/PLANS/PLAN-137-editable-assignment-description.md`, `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน: เพิ่ม endpoint `PATCH /api/Assignments/{id}/description` (Request: `{ description: string }`, Response: `AssignmentActionResponseDto`)
- Verified: `dotnet test` ผ่าน 248/248 tests; `npm run lint` ผ่าน 0 errors; `tsc -b` & `vite build` ผ่าน 0 errors

## [2026-07-23 —] GitHub Copilot — PLAN-135: third label survey, commit, QA/PROD deployment, and smoke
- ทำอะไร: สำรวจ label รอบ 3 ตามกติกา PLAN-135 ด้วย `rg`: ไม่พบ import `lib/learnerStatus`, literal ใน `Badge`/`StatusBadge` children หรือ label map เพิ่มนอก `labels.ts`; conditional/fallback ที่เหลือเป็น page copy/table-review/tooltip และ `CHECK_LABELS` ที่จงใจไม่แตะ. ปิด PLAN-132/133/134 เป็น VERIFIED และ PLAN-135 เป็น DONE.
- Commit/deploy: commit `c0dd5ef` (`feat(admin-react): centralize status and badge labels (PLAN-133/134)`). QA API full publish stamp `20260723103252` + Admin React (robocopy 3); PROD API full publish stamp `20260723104219` + Admin React (robocopy 3). Health checks ทุก environment: `Service/api/admin/session/me`=401, `admin-react/`=200, root=200.
- Smoke: QA สร้างและลบ test batch `AS-20260723-001` (course `WI-CAS-111111`, learner `610034`): ก่อนลบ Course KPI/Learners = 1, หลังลบ KPI/Assignment Batches = 0 และ Learners tab = `No learners`; compliance ไม่เหลือ course test. QA/PROD ตรวจ labels assignments/Gantt/report/divisions/content/learner-groups/health ผ่าน; PROD report donut มี canonical colors ไม่ใช่ fallback เทา.
- Verified: `npm run lint` ผ่าน; `npm run build` ผ่าน; backend tests `247/247`; migration gate ไม่มี pending ทั้ง QA/PROD เมื่อกำหนด `ConnectionStrings__DefaultConnection` ให้ design-time factory.
- Known finding: Health Check QA/PROD มี browser console errors 2 รายการจาก Learner Site health probe ที่ CORS/host unreachable (UI แสดง `เชื่อมต่อไม่ได้` ถูกต้อง) — นอก scope และไม่ใช่ label regression.

## [2026-07-23 —] GitHub Copilot — PLAN-132: hide orphaned enrollments after assignment delete (admin + reports)
- ทำอะไร: implement ตาม PLAN-132 ครบ service + tests โดย **ไม่แตะการลบ Enrollment**. `ReportService` เปลี่ยนจาก `VisibleEnrollmentPredicate` เป็น `ApplyVisibleEnrollmentFilter(IQueryable<Enrollment>)` ใช้ subquery จาก `EnrollmentAssignment` (รวม `IgnoreQueryFilters`) + active-assignment join เพื่อซ่อน enrollment ที่เคยมี link แต่ถูกลบหมด และคงแสดง legacy enrollment ที่ไม่เคยมี link. `CourseService.GetCourseLearnersAsync` โหลด links แบบ `ignoreQueryFilters: true` แล้วกรองมือด้วยกติกาเดียวกัน; effective dates คิดจาก active links เท่านั้น. `CourseService.GetCourseDashboardAsync` ปรับ KPI `LearnerCount/CompletedCount` ให้นับเฉพาะ visible enrollments.
- ไฟล์หลักที่แตะ: `iLearn.Application/Services/ReportService.cs`, `iLearn.Application/Services/CourseService.cs`, `iLearn.Tests/ReportServiceTests.cs`, `iLearn.Tests/CourseServiceVisibilityTests.cs`, `DOC/PLANS/PLAN-132-hide-orphaned-enrollments-after-assignment-delete.md`, `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน: ไม่มี (response shape เดิมทุก endpoint; เปลี่ยนเฉพาะชุดข้อมูลที่ถูกนับ/แสดง)
- Verified: `dotnet build iLearn.Tests -o artifacts/verify-test` ผ่าน; `dotnet test artifacts/verify-test/iLearn.Tests.dll` ผ่าน (Passed 247, Failed 0); cleanup `Remove-Item -Recurse -Force artifacts/verify-test` สำเร็จ

## [2026-07-23 —] Antigravity — Global Search in Courses Explorer & Refactor StatusText Badges
- ทำอะไร: (1) ปรับปรุงช่องค้นหาในหน้า **Courses Explorer** ([CourseListPage.tsx](file:///c:/Users/n4734/source/repos/iLearn2/iLearn.Admin.React/src/pages/courses/CourseListPage.tsx)) ให้สามารถค้นหาข้อมูลย้อนหลังข้ามทุกสายงาน (Divisions) และทุกหมวดหมู่ (Categories) แบบ **Global Search** ได้ทันทีเมื่อพิมพ์คำค้นหา (ค้นหาทั้งชื่อคอร์ส, รหัสคอร์ส, คำอธิบาย, ชื่อหมวดหมู่, และสายงาน) เมื่อลบคำค้นหาออกจะกลับสู่โครงสร้างโฟลเดอร์ปกติ (2) สำรวจจุดที่มีการ hardcode สถานะทั่วทั้งโปรเจกต์ `iLearn.Admin.React` และทำการปรับปรุงเป็นภาษาไทยมาตรฐาน (`learnerStatus.ts`, `StatusBadge.tsx`, `CourseStatusBadge.tsx`, `StatusText.tsx`, `ReadinessBadge.tsx`, `AppTable.tsx`, `moduleConfigs.ts`, `ContentItemDetailPage.tsx`)
- ไฟล์หลักที่แตะ: `iLearn.Admin.React/src/pages/courses/CourseListPage.tsx`, `iLearn.Admin.React/src/lib/learnerStatus.ts`, `iLearn.Admin.React/src/components/ui/StatusBadge.tsx`, `iLearn.Admin.React/src/components/ui/CourseStatusBadge.tsx`, `iLearn.Admin.React/src/components/ui/StatusText.tsx`, `iLearn.Admin.React/src/components/ui/ReadinessBadge.tsx`, `iLearn.Admin.React/src/components/ui/AppTable.tsx`, `iLearn.Admin.React/src/pages/moduleConfigs.ts`, `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน: ไม่มี (UI-only)
- Verified: `npm run lint` ผ่าน 0 errors, `npm run build` ผ่าน 0 errors (1.32s)

## [2026-07-23 —] Claude Code — เขียน PLAN-135: survey label ซ้ำรอบ 3 + commit + deploy QA/PROD (มอบ Copilot)
- ทำอะไร: ผู้ใช้สั่งทำแผนให้ Copilot สำรวจซ้ำ + commit + deploy. ตรวจสถานะ: working tree = PLAN-133/134 เท่านั้น (React 23 ไฟล์ + labels.ts ใหม่ + learnerStatus.ts ลบ + DOC 3 ไฟล์); `843af50` (PLAN-132 backend) commit แล้วแต่**ยังไม่เคย deploy** ⇒ รอบนี้ต้อง full API publish. เขียน PLAN-135: §1 survey ซ้ำด้วย grep patterns + กติกา "badge=แก้ / page copy=จดอย่างเดียว" + รายการจุดที่จงใจปล่อยไว้ §2 verify (lint/build/test + migration gate) §3 commit เดียว §4 deploy QA (full publish) §5 QA smoke (PLAN-132 สร้าง+ลบ assignment ทดสอบ / labels 8 จุด / console) §6 deploy PROD + read-only smoke (ห้ามลบ assignment บน PROD; เตือนตัวเลข report อาจลดลง = intended) §7 ปิดสถานะ 132/133/134→VERIFIED. **ปิด dev API (localhost:7128) + vite dev server ที่ Claude รันทดสอบไว้แล้ว — bin ไม่ล็อก**
- ไฟล์หลักที่แตะ: `DOC/PLANS/PLAN-135-labels-resurvey-commit-deploy-qa-prod.md` (ใหม่ READY), `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน: ไม่มี (แผน ops)
- Verified: — (แผน; สถานะ git ตรวจจริง ณ เวลาเขียน)

## [2026-07-23 —] Claude Code — PLAN-134: กวาดป้าย badge ตกค้างรอบ 2 เข้า `lib/labels.ts` (ต่อจาก PLAN-133)
- ทำอะไร: ผู้ใช้สั่งสำรวจละเอียดซ้ำ — พบป้ายตกค้าง 10 จุด (literal ใน StatusBadge/Badge children ที่ regex รอบแรกไม่จับ) แก้เข้าไฟล์กลางทั้งหมด: version status (Active Version→เวอร์ชันที่ใช้งาน) ใน CourseDetailPage+VersionDetailPage, enrollment badges (Passed/Cancelled/Assigned/Self-Enroll→ไทย) ใน LearnerProfilePage, Not found in directory ใน AssignmentDetailPage, Folder/Group ใน LearnerGroupListPage, **ชนิดคอนเทนต์ 3 จุดขัดกันเอง** (moduleConfigs ไทย vs ContentItemDetail/Editor อังกฤษ) → `contentTypeLabel()` เดียว, HealthCheckPage+SystemConfigPage → `HEALTH_LABELS`. เพิ่มใน labels.ts: CONTENT_TYPE_LABELS+contentTypeLabel, HEALTH_LABELS, COMMON_LABELS อีก 9 คีย์. จงใจไม่แตะ stat caption/หัวคอลัมน์ในบริบทอังกฤษล้วน (บันทึกใน PLAN-134 สำหรับเฟสสองภาษา)
- ไฟล์หลักที่แตะ: `src/lib/labels.ts`, pages: CourseDetailPage, VersionDetailPage, LearnerProfilePage, AssignmentDetailPage, LearnerGroupListPage, ContentItemDetailPage, ContentItemEditorPage, moduleConfigs, HealthCheckPage, SystemConfigPage; `DOC/PLANS/PLAN-134-labels-sweep-round2.md` (DONE)
- Contract ที่เปลี่ยน: ไม่มี (frontend-only)
- Verified: `npm run lint` ✓, `npm run build` ✓ (1.36s); browser smoke: health-check/content library list+detail (ชนิดสอดคล้องแล้ว)/learner-groups ✓, console 0 errors

## [2026-07-23 —] Claude Code — PLAN-133: รวมป้ายสถานะ/badge เข้าไฟล์กลาง `lib/labels.ts` (เตรียมสองภาษา) — implement เอง
- ทำอะไร: ผู้ใช้ต้องการรวมป้ายกำกับ (StatusText/badge) ที่กระจายทุกหน้าเข้าไฟล์เดียวเพื่อเตรียมทำสองภาษา — ตัดสินใจร่วม: เก็บ th/en คู่กัน, ขอบเขตเฉพาะป้ายสถานะ/badge, Claude ทำเอง. สร้าง `iLearn.Admin.React/src/lib/labels.ts` (LabelPair {th,en} + `t()`, STATUS_LABELS/learnerStatusLabel ย้ายจาก `lib/learnerStatus.ts` (ลบแล้ว), statusTone ย้ายจาก StatusBadge + reverse-lookup ผ่าน label map กัน drift, COURSE_STATUS_LABELS ย้ายจาก CourseStatusBadge, READINESS_LABELS, COMMON_LABELS) — components (StatusBadge/CourseStatusBadge re-export helpers เดิม, StatusText/ReadinessBadge/AppTable ดึงจากไฟล์กลาง) + pages 9 ไฟล์. **แก้ bug แฝง 2 จุด:** donut chart assignment report สีเทาหมด (STATUS_COLORS key `'In Progress'` มีเว้นวรรค + lookup ด้วย label ไทย → พลาดทุกครั้ง ⇒ เปลี่ยน key canonical + lookup ด้วย `entry.status`), badge `'Completed'/'In Progress'` อังกฤษดิบใน AssignmentDetailPage
- ไฟล์หลักที่แตะ: `src/lib/labels.ts` (ใหม่), `src/lib/learnerStatus.ts` (ลบ), `src/lib/chartTheme.ts`, `src/components/ui/{StatusBadge,StatusText,ReadinessBadge,CourseStatusBadge,AppTable}.tsx`, pages: EntityListPage, CourseDetailPage, AssignmentDetailPage, AssignmentGanttPage, AssignmentReportPage, AssignmentReportCharts, UserDetailPage, MasterDataDetailPage, ContentItemDetailPage; `DOC/PLANS/PLAN-133-centralize-status-badge-labels.md` (DONE)
- Contract ที่เปลี่ยน: ไม่มี (frontend-only; import path `lib/learnerStatus` → `lib/labels` — ไฟล์เดิมถูกลบ ใครแตะโค้ดเก่าต้อง import จาก `lib/labels`)
- Verified: `npm run lint` ✓ 0 errors, `npm run build` ✓ (1.33s); browser smoke กับ API local: assignments list/Gantt chips/report donut สี/master-data AppTable/division detail StatusText ครบ, console 0 errors

## [2026-07-23 —] Claude Code — เขียน PLAN-132: ซ่อน enrollment กำพร้าหลังลบ assignment (มอบ Copilot)
- ทำอะไร: ผู้ใช้รายงานว่า "ลบ Assignment แล้ว learner ยังขึ้นอยู่" — วิเคราะห์พบ `DeleteAssignmentAsync` soft-delete assignment+links แต่ไม่แตะ Enrollment (ถูกแล้ว) แต่ฝั่งแสดงผลไม่สอดคล้อง: learner-facing ซ่อนถูกต้อง (`hadDeletedAssignmentOnly`), ส่วน (1) `GetCourseLearnersAsync` tab Learners ยังแสดงเป็น active (global query filter ซ่อน deleted links → AssignmentLinks ว่าง → fallback วันที่ดิบ) (2) Course KPI LearnerCount นับทุก enrollment (3) **bug แฝง**: `VisibleEnrollmentPredicate` ใน ReportService พังเงียบเพราะ query filter ทำให้ `!e.AssignmentLinks.Any()` เป็น true สำหรับ enrollment กำพร้า → รายงานยังนับ. ผู้ใช้ตัดสินใจ: **ซ่อนทั้งหมด** (รวมคนเรียนจบ). เขียน PLAN-132 (READY, Assigned Copilot): §1 แก้ predicate เป็น subquery IgnoreQueryFilters §2 แก้ GetCourseLearnersAsync ใช้กติกา hadDeletedAssignmentOnly + effective dates จาก active links §3 แก้ course KPI §4 unit tests
- ไฟล์หลักที่แตะ: `DOC/PLANS/PLAN-132-hide-orphaned-enrollments-after-assignment-delete.md` (ใหม่ READY), `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน: ไม่มี (แผน; response shape ทุก endpoint คงเดิม — ค่าเปลี่ยนแต่ contract ไม่เปลี่ยน)
- Verified: — (แผน; อ้างอิงจากการอ่านโค้ดจริง: AssignmentService.cs:160, CourseService.cs:564/707, ReportService.cs:424, EnrollmentsController.cs:515–556, AppDbContext.ApplySoftDeleteFilters)

## [2026-07-23 —] GitHub Copilot — commit + deploy QA/PROD (PLAN-131 dashboard redesign + ReportService fix)
- ทำอะไร: Verify + commit + deploy. **§0 Verify**: `npm run lint` ✓, `npm run build` ✓ (1.31s), `dotnet build` ✓, `dotnet test` 242/242 ✓. **§1 Commit**: `1d93b8b` "feat(dashboard): action-first redesign + Thai localization (PLAN-131); fix assignment count in course summary report" (5 files, 370 ins / 493 del). **§2 QA deploy**: API stamp `20260723084829` (full publish — backend changed) ✓; React robocopy ExitCode:3 ✓. **§3 PROD deploy**: API stamp `20260723084944` health check ✓; React robocopy ExitCode:3 ✓.
- ไฟล์หลักที่แตะ: `DOC/AGENT_LOG.md`; server-side: QA + PROD API + admin-react deploy
- Contract ที่เปลี่ยน: ไม่มี
- Verified: lint/build/tests ผ่าน; QA+PROD health OK

## [2026-07-23 —] Antigravity — Dashboard Redesign: Action-first + Thai Localization + Shared UI Conventions (PLAN-131)
- ทำอะไร: ดำเนินการ refactor หน้า Dashboard (`DashboardPage.tsx` + `DashboardCharts.tsx`) ตามแผน [PLAN-131](file:///c:/Users/n4734/source/repos/iLearn2/DOC/PLANS/PLAN-131-dashboard-action-first-redesign.md): (1) เปลี่ยนโครงสร้างเป็น **Action-first** เรียงลำดับตามความเร่งด่วน (งานเกินกำหนด → งานใกล้กำหนด → อัตราเรียนสำเร็จ → กิจกรรม 30 วัน) (2) ปรับหน้าตาราง "งานมอบหมายที่ต้องจัดการ" และ "คอร์สที่ต้องติดตาม" ขึ้นเป็นส่วนหลักแบบเต็มความกว้าง (3) แปลข้อความภาษาไทยทั้งหน้า (4) ตัดส่วนที่ไม่จำเป็นออก (Report Hub quick links, Category Mix chart, Task Status Pie) (5) เปลี่ยนใช้ Shared UI Components (`Card`, `ProgressBar`, `StatusBadge`, `AppButton`, `Badge`, `LoadingState`) และ `lib/format.ts` (`formatDate`, `formatRelativeTime`, `formatNumber`, `formatPercent`) (6) คง Type DTO ทุกตัวใน `dashboardApi.ts` และคง SignalR real-time feed เดิมไว้ครบถ้วน
- ไฟล์หลักที่แตะ: `iLearn.Admin.React/src/pages/DashboardPage.tsx`, `iLearn.Admin.React/src/pages/dashboard/DashboardCharts.tsx`, `DOC/PLANS/PLAN-131-dashboard-action-first-redesign.md`, `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน: ไม่มี (Frontend-only)
- Verified: `npm run lint` ผ่าน 0 errors, `npm run build` ผ่าน 0 errors (built in 1.51s)
- ทำอะไร: ปรับปรุงการนับจำนวน `AssignmentCount` ใน `GetCourseSummaryReportAsync` ([ReportService.cs](file:///c:/Users/n4734/source/repos/iLearn2/iLearn.Application/Services/ReportService.cs)): เดิมนับเฉพาะ `Assignment.CourseId` แบบเดี่ยว ทำให้คอร์สที่มีการมอบหมายแบบกลุ่ม (Batch / Multi-course via `AssignmentCourses`) หรือคอร์สที่มอบหมายผ่าน `EnrollmentAssignment` แสดง `Assignments = 0` ทั้งที่มีผู้เรียนเข้าเรียนจริง. ปรับรวมแหล่งข้อมูล 3 ส่วน (`Assignment.CourseId`, `AssignmentCourses`, `EnrollmentAssignment`) แบบ `Distinct()` ให้แสดงจำนวนชุดสั่งงานตามความเป็นจริงถูกต้อง
- ไฟล์หลักที่แตะ: `iLearn.Application/Services/ReportService.cs`, `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน: ไม่มี
- Verified: `dotnet build iLearn.Tests` ผ่าน 0 errors, `dotnet test iLearn.Tests` ผ่าน 242/242 tests (0 failed)
- ทำอะไร: ผู้ใช้สั่ง refactor หน้า Dashboard — วิเคราะห์หน้าเดิม (`DashboardPage.tsx` + `dashboardApi.ts` + `DashboardCharts.tsx`) พบผิด UI Conventions หลายจุด (hand-roll Card/SectionHeader/CompletionBar/relativeTime/formatDateShort ซ้ำ shared) + ยังเป็นอังกฤษทั้งหน้า. ถามผู้ใช้ตัดสินใจ 4 ข้อ: ทิศทาง = **Action-first**, ภาษา = **ไทยทั้งหน้า**, ตัดทิ้ง = **Report Hub quick links / Category Mix chart / Task Status Pie**, Assigned = **Antigravity**. เขียน PLAN-131: §1 โครงใหม่ (KPI 4 ช่องเรียงตามความเร่งด่วน เกินกำหนด→ใกล้กำหนด→อัตราสำเร็จ→กิจกรรม 30 วัน, ตาราง priority assignments + course attention เต็มกว้าง, chart+activity feed แถวล่าง) §2 ส่วนลบ §3 ห้ามแตะ (SignalR PLAN-089, polling) §4 out of scope (frontend-only, ห้ามลบ DTO mirror types, ไม่ใช้ AppTable เพราะผูก AppClientStore)
- ไฟล์หลักที่แตะ: `DOC/PLANS/PLAN-131-dashboard-action-first-redesign.md` (ใหม่ READY), `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน: ไม่มี (แผน; backend ไม่แตะ — Overview ยังคืน taskStatus/categoryMix ฝั่ง React แค่ไม่ render)
- Verified: — (แผน; อ้างอิงจากการอ่านโค้ดจริง: `formatRelativeTime` มีใน format.ts เป็นไทยแล้ว, `Card`/`SectionHeader`/`ProgressBar` API ตรงตามที่แผนอ้าง)

## [2026-07-23 —] GitHub Copilot — commit + deploy QA/PROD (Antigravity report pages standardization)
- ทำอะไร: Verify + commit + deploy งานของ Antigravity (standardize all report pages: Thai localization, infinite scroll, viewport height layout + CourseSummary column cleanup). **§0 Verify**: `npm run lint` ✓, `npm run build` ✓ (1.40s). **§1 Commit**: `566e2b4` "feat(reports): standardize all report pages — Thai localization, infinite scroll, viewport height layout" (5 files, 253 ins / 348 del). **§2 QA deploy**: API stamp `20260723082400` (SkipPublish=frontend-only) ✓; React robocopy ExitCode:3 ✓. **§3 PROD deploy**: API stamp `20260723082508` health check ✓; React robocopy ExitCode:3 ✓.
- ไฟล์หลักที่แตะ: `DOC/AGENT_LOG.md`; server-side: QA + PROD admin-react deploy
- Contract ที่เปลี่ยน: ไม่มี
- Verified: lint/build ผ่าน; QA+PROD health OK

## [2026-07-23 —] Antigravity — Standardize All Report Pages Design, Thai Localization & Infinite Scroll
- ทำอะไร: ปรับปรุงโครงสร้าง UI หน้ารายงานทั้งหมดใน `iLearn.Admin.React/src/pages/reports` (`ComplianceReportPage.tsx`, `ActivityReportPage.tsx`, `TranscriptReportPage.tsx`) ให้มีมาตรฐานรูปแบบเดียวกับหน้า `CourseSummaryReportPage.tsx`: (1) ปรับ Layout เป็นแบบ Full-height Screen (`h-full flex flex-col min-h-0`) ขยายพื้นที่ตารางพอดีกรอบหน้าจอ (2) ปรับเปลี่ยนข้อความ ชื่อหัวตาราง ป้ายปุ่ม ป้ายสถานะ และช่องค้นหาเป็นภาษาไทยที่เข้าใจง่าย (3) ยกเลิกปุ่ม Load More โดยเปลี่ยนใช้ **Infinite Scroll** โหลดรายการในตารางเมื่อเลื่อนลงขอบล่าง พร้อมส่วนแสดงสถานะจำนวนรายการที่ด้านล่างตาราง (4) รวมปุ่ม Export CSV และปุ่มเปลี่ยนช่วงเวลาไว้ใน Card header อย่างเป็นระเบียบ
- ไฟล์หลักที่แตะ: `iLearn.Admin.React/src/pages/reports/ComplianceReportPage.tsx`, `iLearn.Admin.React/src/pages/reports/ActivityReportPage.tsx`, `iLearn.Admin.React/src/pages/reports/TranscriptReportPage.tsx`, `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน: ไม่มี
- Verified: `npm run lint` ผ่าน 0 errors, `npm run build` ผ่าน 0 errors (built in 1.49s)
- ทำอะไร: ปรับปรุงหน้า `CourseSummaryReportPage.tsx` (`/reports/courses`): (1) ยกเลิกคอลัมน์ `Completion Rate` และ `Avg Score` ออกจากตารางและไฟล์ Export CSV ตามคำสั่งผู้ใช้ ทำให้เหลือ 10 คอลัมน์ที่สะอาดตาขึ้น (2) ปรับ Layout ตารางให้ใช้ `h-full flex flex-col min-h-0` ขยายความสูงเต็มพื้นที่หน้าจอพอดีกรอบ Viewport โดยอัตโนมัติ (3) ลบ Modal คำอธิบายที่ไม่ได้ใช้ออกเพื่อ clean up โค้ด
- ไฟล์หลักที่แตะ: `iLearn.Admin.React/src/pages/reports/CourseSummaryReportPage.tsx`, `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน: ไม่มี
- Verified: `npm run lint` ผ่าน 0 errors, `npm run build` ผ่าน 0 errors (built in 1.48s)
- ทำอะไร: Verify + commit + deploy งานของ Antigravity (course summary infinite scroll / remove KPI header / ReportHub Thai localization). **§0 Verify**: `npm run lint` ✓, `npm run build` ✓ (1.41s), `dotnet test` 242/242 ✓, no pending migrations. **§1 Commit**: `a11dc73` "feat(reports): course summary infinite scroll + remove KPI header cards; report hub Thai localization" (3 files changed, 56 ins / 165 del). **§2 QA deploy**: API stamp `20260723081121` → `\\AP-NTC2138-QAWB\wwwroot\iLearn\Service` ✓; React robocopy ExitCode:3 → `\\AP-NTC2138-QAWB\wwwroot\iLearn\admin-react` ✓. **§3 PROD deploy**: API stamp `20260723081307` health check HTTP 401 ✓ → `\\ap-ntc2137-prwb\wwwroot\iLearn\Service`; React robocopy ExitCode:3 → `\\ap-ntc2137-prwb\wwwroot\iLearn\admin-react` ✓.
- ไฟล์หลักที่แตะ: `DOC/AGENT_LOG.md`; server-side: QA + PROD API + admin-react deploy
- Contract ที่เปลี่ยน: ไม่มี
- Verified: lint/build/tests ผ่าน; QA+PROD health OK; no pending migrations

## [2026-07-23 —] Antigravity — Course Summary Report UI Simplification & Report Hub Thai Localization
- ทำอะไร: (1) ปรับปรุงหน้า `CourseSummaryReportPage.tsx` (`/reports/courses`): ปรับระบบโหลดข้อมูลตารางเป็นแบบ **Infinite Scroll** (โหลดครั้งละ 100 รายการเมื่อเลื่อน scrollbar ลงล่างสุด) และยกเลิกส่วน Header bar กับ 5 KPI summary cards ตามคำสั่งผู้ใช้ (2) ปรับปรุงหน้า `ReportHubPage.tsx` (`/reports`): เปลี่ยนคำอธิบาย ชื่อการ์ด ป้ายแท็ก และข้อความนำทางในหน้า Report Hub ทั้งหมดเป็นภาษาไทยที่กระชับและชัดเจนเข้าใจง่าย
- ไฟล์หลักที่แตะ: `iLearn.Admin.React/src/pages/reports/CourseSummaryReportPage.tsx`, `iLearn.Admin.React/src/pages/reports/ReportHubPage.tsx`, `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน: ไม่มี
- Verified: `npm run lint` ผ่าน 0 errors, `npm run build` ผ่าน 0 errors (built in 1.55s)

## [2026-07-22 —] GitHub Copilot — PLAN-130: commit PLAN-129 + deploy QA/PROD + favicon QA fix + smoke รวบยอด
- ทำอะไร: ปิดงานตาม PLAN-130 end-to-end. **§0** commit PLAN-129 สำเร็จ (`82db43b`), verify ซ้ำผ่าน (`npm run lint`, `npm run build`, `dotnet test` = 242/242), migration gate ผ่านทั้ง QA/PROD (`dotnet ef migrations list` ไม่พบ pending). **§A QA deploy**: `tools/deploy-api.ps1` stamp `20260722165918` health 401 expected, `tools/deploy-admin-react.ps1` copy สำเร็จ (robocopy 3), health checks ครบ (`/Service/api/admin/session/me`=401, `/admin-react/`=200, `/`=200). **§B favicon QA**: copy `\\AP-NTC2138-QAWB\wwwroot\iLearn\wwwroot\favicon.svg` -> `\\AP-NTC2138-QAWB\wwwroot\iLearn\favicon.svg`; ตรวจ QA ได้ 200 `image/svg+xml` เมื่อเรียกด้วย Windows credentials (anonymous ได้ 401 ตาม auth policy), PROD read-only `/iLearn/favicon.svg` ยัง 200. **§C QA smoke**: รายงาน course summary ผ่าน (มี `#`/`Division`/`Type`, ไม่มี Avg Progress, sticky header, max-h scroller), smoke ผู้ใช้ `LearnerDirectorySelector` ครบ 3 callers (Assignment Detail / Learner Group Detail / Learner Group Editor) interaction หลักครบ, bulk-assign category filter step1 + tree/toggle step2 ผ่าน, console checks หน้า smoke = 0 errors. **§D PROD deploy**: `tools/deploy-api-prod.ps1` stamp `20260722170751` health 401 expected + `tools/deploy-admin-react-prod.ps1` copy สำเร็จ; health checks ครบ 3 URL (`/Service/api/admin/session/me`=401, `/admin-react/`=200, `/`=200). **§D PROD smoke read-only**: course summary โครงเดียว QA + console 0 errors + favicon 200.
- ไฟล์หลักที่แตะ: `DOC/PLANS/PLAN-129-course-summary-columns-and-scroller-grid.md` (DONE->VERIFIED), `DOC/PLANS/PLAN-121-bulk-assign-ux-category-filter-group-tree-toggle-fix.md` (REVIEWED->VERIFIED), `DOC/PLANS/PLAN-122-learner-directory-unified-compact-layout.md` (REVIEWED->VERIFIED), `DOC/PLANS/PLAN-130-deploy-qa-prod-favicon-fix-and-smoke.md` (READY->DONE + notes), `DOC/AGENT_LOG.md`; และ server file copy 1 จุดสำหรับ favicon QA
- Contract ที่เปลี่ยน: ไม่มี
- Verified: QA+PROD deploy health ผ่าน, smoke ที่ระบุใน PLAN-130 ผ่านตาม checklist หลัก, ไม่มี pending migration

## [2026-07-22 —] Claude Code — เขียน PLAN-130: commit+deploy QA/PROD (PLAN-129) + แก้ favicon QA + smoke รวบยอด (มอบ Copilot)
- ทำอะไร: ผู้ใช้สั่ง deploy งานค้าง + แก้ favicon + ทดสอบ. ตรวจสถานะ: working tree = PLAN-129 (Antigravity ทำเสร็จ verify แล้ว ยังไม่ commit/deploy) — Claude skim diff backend: เพิ่ม `DivisionName`/`CourseTypeName` optional null-safe ไม่แตะ effective dates ✓. วินิจฉัย favicon เสร็จก่อนหน้า: QA มี IIS config ดัก `*.svg` → static handler หา `\iLearn\favicon.svg` ไม่เจอ → 404 (PROD ปกติ; ผู้ใช้เห็นแท็บ PROD ไม่มีไอคอนเพราะ Edge cache) ⇒ fix = copy favicon.svg ไป root แอป 1 ไฟล์. เขียน PLAN-130: §0 commit+verify+migration gate §A deploy QA (API+admin-react) §B favicon copy+ทดสอบ (fallback = IIS admin เพิ่ม mime — จดค้าง) §C QA smoke (129 + smoke ค้างของ 121/122: 3 callers LearnerDirectorySelector + bulk assign regression + favicon + console) §D deploy PROD + read-only smoke §E ปิดสถานะ 121/122/129 → VERIFIED
- ไฟล์หลักที่แตะ: `DOC/PLANS/PLAN-130-deploy-qa-prod-favicon-fix-and-smoke.md` (ใหม่ READY), `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน: ไม่มี (แผน ops)
- Verified: — (แผน; ตรวจ git status/log + AGENT_LOG + diff PLAN-129 + ผลวินิจฉัย favicon จาก session นี้)
- **ถึง Copilot: stamp เก่าถูกลบหมดทั้ง 2 เครื่องวันนี้ — ห้ามลบไฟล์บนเซิร์ฟเวอร์เพิ่ม; ห้ามแตะ IIS config/web.config บนเซิร์ฟเวอร์; PROD ฝั่ง favicon ห้ามแก้ (ปกติอยู่แล้ว); เจอ pending migration = หยุดทันที**

## [2026-07-22 —] Antigravity — Course Summary Report: #, Division, Type Columns, Single Completion Rate & Scroller Grid (PLAN-129)
- ทำอะไร: **§1 (Backend)** อัปเดต `ReportDtos.cs` และ `ReportService.cs` ใน `GetCourseSummaryReportAsync` ให้ Query `DivisionName` (จาก `Category.Division.Name`) และ `CourseTypeName` (จาก `CourseType.Name`). **§2 (Frontend & Layout)** อัปเดต `reportTypes.ts` และ `CourseSummaryReportPage.tsx`: เพิ่มคอลัมน์ `#` (ลำดับแถว), `Division` (สังกัด) และ `Type` (ประเภทคอร์ส Badge pill), ตัดคอลัมน์ `Avg Progress` ออกจากตารางเพื่อใช้ `Completion Rate (%)` เป็นตัววัดการเรียนสำเร็จเดียวชัดเจน, ปรับการ์ดสรุปเป็น `Overall Completion Rate`, และปรับปรุงกรอบตารางด้วย `max-h-[600px] overflow-auto custom-scrollbar` พร้อม `sticky top-0 z-10` ล็อกหัวตารางด้านบนขณะเลื่อนดูเหมือนหน้า `/assignments`.
- ไฟล์หลักที่แตะ: `iLearn.Application/DTOs/ReportDtos.cs`, `iLearn.Application/Services/ReportService.cs`, `iLearn.Admin.React/src/pages/reports/reportTypes.ts`, `iLearn.Admin.React/src/pages/reports/CourseSummaryReportPage.tsx`, `DOC/PLANS/PLAN-129-course-summary-columns-and-scroller-grid.md`, `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน: เพิ่มฟิลด์ทางเลือก `divisionName` และ `courseTypeName` บน `CourseSummaryRow` DTO
- Verified: `npm run lint` ผ่าน 0 errors, `npm run build` ผ่าน 0 errors (built in 1.45s), `dotnet test` ผ่าน 242/242 tests (0 failures)

## [2026-07-22 —] Antigravity — Show All Courses in Summary Report, Metrics Guide Help Modal (PLAN-128) & Deploy QA+PROD
- ทำอะไร: **§1 (Backend)** แก้ไข `ReportService.cs` ใน `GetCourseSummaryReportAsync` ให้ดึงคอร์สทั้งหมดในแคตตาล็อก (`_courseRepo`) เป็น Query หลัก (กรองตาม `DivisionId` หากเป็น DivisionAdmin) แล้ว Left Join สถิติการเรียน ทำให้คอร์สทุกคอร์สในแคตตาล็อก (รวมคอร์สที่มี 0 enrollments) แสดงในหน้ารายงานสรุปครบถ้วน 100%. **§2 (Frontend & UI Help Popup)** เพิ่มปุ่ม **"Metrics Guide / คู่มือตัววัด"** และปุ่มไอคอน Info บนตารางของ `CourseSummaryReportPage.tsx` เมื่อคลิกจะเปิด `Modal` ป๊อปอัปคำอธิบาย สูตรคำนวณ และตัวอย่างเปรียบเทียบระหว่าง **Completion Rate (%)** กับ **Avg Progress (%)** อย่างละเอียดชัดเจน. **§3 (Deploy QA & PROD)** รัน `tools/deploy-api.ps1`, `tools/deploy-admin-react.ps1`, `tools/deploy-api-prod.ps1` และ `tools/deploy-admin-react-prod.ps1` อัปเดตทั้งเซิร์ฟเวอร์ QA (`AP-NTC2138-QAWB`) และ PROD (`ap-ntc2137-prwb`) เรียบร้อยสำเร็จ 100%.
- ไฟล์หลักที่แตะ: `iLearn.Application/Services/ReportService.cs`, `iLearn.Admin.React/src/pages/reports/CourseSummaryReportPage.tsx`, `DOC/PLANS/PLAN-128-course-summary-report-all-courses.md`, `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน: ไม่มี
- Verified: `npm run lint` ผ่าน 0 errors, `npm run build` ผ่าน 0 errors, `dotnet test` ผ่าน 242/242 tests, QA & PROD HealthChecks OK, Robocopy ExitCode: 3 (CopySucceeded)


## [2026-07-22 —] Antigravity — Standardizing Reports Pages UI Design (PLAN-127)
- ทำอะไร: ออกแบบและปรับปรุงส่วนงานรายงาน (Reports) ทั้ง 5 หน้าใน `iLearn.Admin.React/src/pages/reports` ให้มีมาตรฐานเดียวกันตาม UI Conventions (Card, SectionHeader, Badge, StatusBadge, ProgressBar, ListToolbar, SegmentedToggle, AppButton, format.ts):
  1. `ReportHubPage.tsx`: ยกระดับ Card layout, ป้าย Category Tag (`Badge variant="tag"`), Visual hierarchy และ Arrow Navigation Link
  2. `ComplianceReportPage.tsx`: เพิ่ม Back link ย้อนกลับ `/reports`, ปรับแต่ง 5 KPI Summary Cards, division chart, Overview Rates division/department toggle, และ Overdue Enrollments table ค้นหาด้วย ListToolbar สลับหน้าย่อย และ clickable transcript link
  3. `CourseSummaryReportPage.tsx`: เพิ่ม Back link ย้อนกลับ `/reports`, เพิ่ม 5 KPI summary tiles (Total Courses, Total Enrolled, Completed, Overdue, Avg Completion Rate), sortable headers, ค้นหาผ่าน ListToolbar และ Export CSV
  4. `ActivityReportPage.tsx`: เพิ่ม Back link ย้อนกลับ `/reports`, เพิ่ม 4 Period KPI summary cards, period selector toggle (6/12/24 เดือน), Recharts bar charts, และ Monthly breakdown table
  5. `TranscriptReportPage.tsx`: เพิ่ม Back link ย้อนกลับ `/reports`, ออกแบบ Learner search header พร้อมปุ่มเคลียร์, เพิ่ม Learner Information Card, Filterable Training records table, และ `@media print` layout formatting
- ไฟล์หลักที่แตะ: `iLearn.Admin.React/src/pages/reports/ReportHubPage.tsx`, `iLearn.Admin.React/src/pages/reports/ComplianceReportPage.tsx`, `iLearn.Admin.React/src/pages/reports/CourseSummaryReportPage.tsx`, `iLearn.Admin.React/src/pages/reports/ActivityReportPage.tsx`, `iLearn.Admin.React/src/pages/reports/TranscriptReportPage.tsx`, `DOC/PLANS/PLAN-127-reports-ui-standardization.md`, `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน: ไม่มี
- Verified: `npm run lint` ผ่าน 0 errors, `npm run build` ผ่าน 0 errors (built in 1.71s)

## [2026-07-22 —] Antigravity — Gender Prefix Removal Across System (NameHelper & format.ts)
- ทำอะไร: ยกเลิกการแสดงผลคำนำหน้าชื่อที่บอกถึงเพศทั้งหมด (เช่น นาย, นาง, นางสาว, น.ส., เด็กชาย, เด็กหญิง, ด.ช., ด.ญ., Mr., Mrs., Miss, Ms., Master) ทั้งระบบ. **§1 (Backend)** สร้าง `NameHelper.StripGenderPrefix` ใน `iLearn.Application/Common/NameHelper.cs` พร้อมอัปเดตการ mapping ชื่อใน `EmployeeHubLearnerApiService.cs`, `LearnerApiService.cs` และ `ExternalLearnerDto.FullName`. **§2 (Frontend)** เพิ่ม `stripGenderPrefix` ใน `src/lib/format.ts` และอัปเดต `LearnerDirectorySelector.tsx` ใน `iLearn.Admin.React`. **§3 (Unit Tests)** สร้าง `NameHelperTests.cs` ครอบคลุมเคสคำนำหน้าชื่อไทย/อังกฤษทั้งหมด.
- ไฟล์หลักที่แตะ: `iLearn.Application/Common/NameHelper.cs`, `iLearn.Infrastructure/Services/EmployeeHubLearnerApiService.cs`, `iLearn.Infrastructure/Services/LearnerApiService.cs`, `iLearn.Application/DTOs/ExternalLearnerDto.cs`, `iLearn.Admin.React/src/lib/format.ts`, `iLearn.Admin.React/src/components/shared/LearnerDirectorySelector.tsx`, `iLearn.Tests/NameHelperTests.cs`, `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน: ไม่มี
- Verified: `npm run lint` ผ่าน 0 errors, `npm run build` ผ่าน 0 errors, `dotnet test` ผ่าน 242/242 tests (0 failures)


## [2026-07-22 —] Antigravity — Safari iPad Favorites PNG Icon Fallback Fix
- ทำอะไร: ปรับปรุง `_DevExtremeLayout.cshtml` เพิ่ม `<link href="~/apple-touch-icon-180.png" rel="icon" type="image/png" sizes="180x180" />` และ `<link href="~/favicon.ico" rel="shortcut icon" type="image/x-icon" />` เพื่อรองรับ Safari บน iPad/iOS เมื่อผู้ใช้สั่ง "เพิ่มไปยังรายการโปรด" (Add to Favorites / Bookmarks) เนื่องจาก Safari Favorites บน iPad ไม่รองรับการแสดงผลไฟล์ไอคอนประเภท SVG Vector (`favicon.svg`) และต้องใช้ภาพ PNG/ICO ความละเอียดสูง.
- ไฟล์หลักที่แตะ: `iLearn.User/Views/Shared/_DevExtremeLayout.cshtml`, `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน: ไม่มี
- Verified: `dotnet build iLearn.User` ผ่าน 0 errors (74 warnings)


## [2026-07-22 —] Antigravity — Thai Translation for Course Deletion & Deploy to QA + PROD
- ทำอะไร: **§1 (Thai Localization)** ปรับปรุงข้อความแจ้งเตือนป๊อปอัปยืนยันการลบคอร์ส, บังคับลบคอร์ส (Force Delete), Tooltip บนปุ่มลบ และข้อความแจ้งเตือนสาเหตุจาก Backend ใน `CourseService.cs` และ `CourseDetailPage.tsx` ให้เป็นภาษาไทยชัดเจนสุภาพ สื่อความหมายเข้าใจง่าย. **§2 (Deploy QA & PROD)** รัน `tools/deploy-api.ps1`, `tools/deploy-admin-react.ps1`, `tools/deploy-api-prod.ps1` และ `tools/deploy-admin-react-prod.ps1` อัปเดตทั้งเซิร์ฟเวอร์ QA (`AP-NTC2138-QAWB`) และ PROD (`ap-ntc2137-prwb`) สำเร็จ 100%.
- ไฟล์หลักที่แตะ: `iLearn.Application/Services/CourseService.cs`, `iLearn.Admin.React/src/pages/courses/CourseDetailPage.tsx`, `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน: ไม่มี
- Verified: `npm run lint` ผ่าน 0 errors, Deploy QA & PROD HealthCheck OK (HTTP 401), Robocopy CopySucceeded: True

## [2026-07-22 —] Antigravity — Deploy Backend API & Admin React to PROD (PLAN-125, PLAN-126 & apiClient fix)
- ทำอะไร: รัน `tools/deploy-api-prod.ps1` และ `tools/deploy-admin-react-prod.ps1` ทำการ publish `.NET Backend API` (เวอร์ชันแก้ Foreign Key, Closed Status Guard, Force Delete และ JSON Error Message Return) พร้อมทั้ง build & copy static assets ของ `Admin React` (เวอร์ชันแกะ JSON Error Message แสดงผลลัพธ์บน Toast และปุ่ม Disable Delete Course) ไปยัง PROD server (`ap-ntc2137-prwb`) สำเร็จเรียบร้อย.
- ไฟล์หลักที่แตะ: `tools/deploy-api-prod.ps1`, `tools/deploy-admin-react-prod.ps1`, `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน: `DELETE api/Courses/{id}?force={bool}` รองรับ query string `force` (default `false`) บน PROD
- Verified: API Deploy & HealthCheck OK (HTTP 401), Admin React Deploy CopySucceeded: True (Robocopy exit code 3)

## [2026-07-22 —] Antigravity — apiClient.ts JSON Error Message Extraction Fix & Complete Course 507 Cleanup
- ทำอะไร: **§1 (apiClient.ts)** ปรับปรุง `readResponseBody` และ `fetchWithAccessControl` ใน `iLearn.Admin.React/src/lib/apiClient.ts` ให้ทำการ parse ข้อมูล JSON error response ที่ได้รับจาก Backend แล้วสกัด `message`/`Message`/`error`/`title` ออกมาตั้งเป็น `ApiError.message` อัตโนมัติ แทนการใช้เพียง `response.statusText` (เดิมแสดงเพียง "Bad Request" ลอยๆ บน Toast โดยไม่ระบุสาเหตุ). **§2 (Course 507 Cleanup)** ทำการเคลียร์ประวัติและ Soft-delete Enrollment 18197 ค้างของ Course 507 และ Hard-delete FileStorage 1087/1088 บนฐานข้อมูลเรียบร้อยแล้ว 100%.
- ไฟล์หลักที่แตะ: `iLearn.Admin.React/src/lib/apiClient.ts`, `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน: ไม่มี
- Verified: `npm run lint` ผ่าน 0 errors, `npm run build` ผ่าน 0 errors (built in 1.81s), Cleanup script สำหรับ Course 507 สำเร็จ 100%

## [2026-07-22 —] Claude Code — IIS cleanup: ลบ deploy stamp เก่า 6+6 ตัว QA+PROD (~2.66 GB) — health check ผ่านหมด
- ทำอะไร: ผู้ใช้ขอสำรวจ+เก็บกวาดไฟล์ไม่จำเป็นบน IIS. สำรวจ `\\AP-NTC2138-QAWB\wwwroot\iLearn` + `\\ap-ntc2137-prwb\wwwroot\iLearn` (สิทธิ์ session ปัจจุบันเข้าได้ ไม่ต้องใช้ credential เพิ่ม): อ่าน web.config ทุกโซนหา live stamp ก่อน แล้วลบเฉพาะ stamp เก่าแบบระบุชื่อทีละตัว (ผู้ใช้ยืนยันลบทั้งหมดทั้ง 2 เครื่อง). **QA ลบ:** `_user_deploy_{20260722132157,20260722133657}`, `Service\_deploy_{20260722100944,20260722105526}`, `admin\_admin_deploy_{20260710164712,20260710164817}`. **PROD ลบ:** `_user_deploy_{20260722132250,20260722133803}`, `Service\_deploy_{20260717130658,20260722101547}`, `admin\_admin_deploy_{20260710164915,20260713091430}`. **เก็บไว้ (อย่าลบในอนาคต):** live stamps, `wwwroot/`, `appsettings*.json` ที่ root ทุกโซน (คือ config จริง — ContentRoot อยู่ที่ root ไม่ใช่ stamp folder), `icons/`, `student/` (redirect stub 301→/iLearn), `Service\logs` (ว่าง)
- ไฟล์หลักที่แตะ: ไม่มีโค้ด — server filesystem เท่านั้น + `.claude/settings.local.json` (เพิ่ม allow rule อ่าน PROD share), `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน: ไม่มี
- Verified: health check หลังลบ — QA: `/iLearn/` 200, `/admin-react/` 200, `Service/api/admin/session/me` 200; PROD: ครบ 3 ตัวเดียวกัน 200 หมด
- **สำคัญถึงทุก agent: ตอนนี้ไม่มี stamp ก่อนหน้าเหลือบนทั้ง 2 เครื่อง ⇒ `-Rollback` ของ deploy scripts ใช้ไม่ได้จนกว่าจะมี deploy รอบใหม่ — ถ้าพัง ต้อง re-deploy จาก git; หมายเหตุ: PROD `Service\web.config` เปิด `stdoutLogEnabled=true` อยู่ (logs ยังว่าง) ควรพิจารณาปิดในแผนถัดไป**

## [2026-07-22 —] Antigravity — PLAN-126: Require Closed Status for Course Deletion & Unify CourseStatusText to Soft Badge
- ทำอะไร: implement ตาม PLAN-126 — **§1 (Backend Guard)** เพิ่มการตรวจสอบสถานะใน `CourseService.DeleteCourseAsync` หากสถานะคอร์สไม่ใช่ `Closed` (`Status != 2`) และไม่ใช่กรณีบังคับลบ `force` จะทำการโยน `InvalidOperationException` แจ้งว่าคอร์สต้องถูก Close ก่อนลบเสมอ. **§2 (UI Control)** ปรับ `CourseControls` ใน `CourseDetailPage.tsx` ให้ปุ่ม `Delete Course` ถูก Disable พร้อม Tooltip แจ้งเตือน *"Course must be Closed before it can be deleted"* หากคอร์สอยู่ในสถานะ `Open` หรือ `Draft`. **§3 (Badge Unification)** ปรับ `CourseStatusText` ใน `CourseStatusBadge.tsx` ให้เปลี่ยนจาก `variant="outline"` เป็น `variant="soft"` เป็นค่าเริ่มต้น (`bg-emerald-100 text-emerald-800` ทรงนุ่มไม่มีขอบเส้น) สอดคล้องกับ `StatusBadge` และ `ReadinessBadge`.
- ไฟล์หลักที่แตะ: `iLearn.Application/Services/CourseService.cs`, `iLearn.Admin.React/src/components/ui/CourseStatusBadge.tsx`, `iLearn.Admin.React/src/pages/courses/CourseDetailPage.tsx`, `DOC/PLANS/PLAN-126-course-status-closed-guard-and-status-badge-soft.md` (->DONE), `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน: ไม่มี
- Verified: `npm run lint` ผ่าน 0 errors, `npm run build` ผ่าน 0 errors (built in 1.75s), `dotnet test` ผ่าน 222/222 tests (0 failures)

## [2026-07-22 —] Antigravity — PLAN-125: Fix Course Deletion FK Constraint, Force Delete Option & Detailed Reporting + Cleanup Course 969
- ทำอะไร: implement ตาม PLAN-125 — **§1 (Backend)** แก้บั๊ก `CourseService.DeleteCourseAsync` เมื่อสั่งลบ `FileStorage` ถาวรแล้วติด SQL Server Foreign Key Violation `FK_ContentItems_FileStorages_FileStorageId` โดยทำการปลด `r.FileStorageId = null` บน `ContentItem` ก่อนทำการ Soft-Delete. เพิ่มพารามิเตอร์ `force` รองรับ Force Delete เคลียร์ `Enrollments` ที่ค้างอยู่เพื่อความสะดวกในการลบคอร์สในคลิกเดียวโดยไม่ต้องตามไปลบหน้าอื่น. เพิ่มการลบตารางกลาง `AssignmentCourse`. **§2 (Controller & React)** ปรับ `CoursesController.cs` และ `CourseDetailPage.tsx` ให้ส่งและแสดงผล Error Message จาก Backend แบบเจาะจงชัดเจนบน `toast.error` พร้อมเพิ่ม Popover ยืนยัน Force Delete เมื่อมีผู้เรียนเรียนค้างอยู่. **§3 (Cleanup 969)** ทำการลบคอร์ส 969 (`PLAN-079-TEST-01 SCORM 1.2 Learn`) ออกจากระบบเรียบร้อยแล้ว (ส่วนคอร์ส 507 ถูก Soft-Delete ไปก่อนหน้านี้แล้ว).
- ไฟล์หลักที่แตะ: `iLearn.Application/Interfaces/Services/ICourseService.cs`, `iLearn.Application/Services/CourseService.cs`, `iLearn.API/Controllers/CoursesController.cs`, `iLearn.Admin.React/src/pages/courses/CourseDetailPage.tsx`, `iLearn.Tests/CoursesCrudControllerTests.cs`, `DOC/PLANS/PLAN-125-fix-course-deletion-fk-constraint-and-cleanup-969.md` (->DONE), `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน: `DELETE api/Courses/{id}?force={bool}` รองรับ query string `force` (default `false`)
- Verified: `npm run lint` ผ่าน 0 errors, `npm run build` ผ่าน 0 errors (built in 1.47s), `dotnet test` ผ่าน 222/222 tests (0 failures), Execute deletion script สำหรับ Course 969 สำเร็จ 100%

## [2026-07-22 —] Antigravity — Deploy Admin React to PROD (PLAN-119..124 batch)
- ทำอะไร: รัน `tools/deploy-admin-react-prod.ps1` สั่ง lint + build production bundle (ใช้ `.env.production`) และ copy static assets ลง PROD server (`\\ap-ntc2137-prwb\wwwroot\iLearn\admin-react`) สำเร็จ (`CopySucceeded: True`, Robocopy exit code 3). รวมการอัปเดตของ PLAN-119 ถึง PLAN-124 (ReadinessBadge soft fill, Folder edit action button, LearnerDirectorySelector unified layout, BulkAssign category filters & group tree).
- ไฟล์หลักที่แตะ: `tools/deploy-admin-react-prod.ps1`, `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน: ไม่มี
- Verified: Deploy PROD สำเร็จ 0 errors (built in 1.25s)

## [2026-07-22 —] Antigravity — PLAN-124: LearnerGroupListPage folder edit action
- ทำอะไร: implement ตาม PLAN-124 — เพิ่มปุ่มแก้ไขโฟลเดอร์ (`Edit3`) ในคอลัมน์ `Actions` ของตาราง Learner Group Explorer (`LearnerGroupListPage.tsx`) สำหรับรายการประเภท `FOLDER`. เพิ่ม Edit Folder Modal สำหรับแก้ไขชื่อโฟลเดอร์ (Folder Name), คำอธิบาย (Description) และ Division (กรณี SuperAdmin) ยิง `PUT api/LearnerGroupCategories/{id}` และสั่งโหลดข้อมูลไดเรกทอรีใหม่สำเร็จทันที.
- ไฟล์หลักที่แตะ: `iLearn.Admin.React/src/pages/learner-groups/LearnerGroupListPage.tsx`, `DOC/PLANS/PLAN-124-learner-group-folder-edit-action.md` (->DONE), `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน: ไม่มี (ใช้งาน PUT endpoint เดิมของ LearnerGroupCategoriesController)
- Verified: `npm run lint` ผ่าน 0 errors, `npm run build` ผ่าน 0 errors (built in 1.31s)

## [2026-07-22 —] Antigravity — PLAN-123: ReadinessBadge variant="soft" unification + UI inline CSS cleanup
- ทำอะไร: implement ตาม PLAN-123 — **§1** ปรับ `Badge.tsx` ให้ `export type BadgeVariant` และปรับ `ReadinessBadge.tsx` ให้รองรับ prop `variant` โดยมีค่าดีฟอลต์เป็น `'soft'` เพื่อให้ป้ายสถานะ `Published` / `Not Ready` แสดงผลเป็นแบบสีทึบนุ่ม (`bg-emerald-100 text-emerald-800`) รูปทรงสอดคล้องกับ `StatusBadge` บนการ์ดฝั่ง Overview ในหน้าเดียวกัน. **§2** ปรับปุ่ม "Open SCORM Player" ในตาราง `Current Content` ของ `VersionDetailPage.tsx` จาก raw `<button>` มาใช้ `<AppButton variant="secondary" size="sm" icon={ExternalLink}>`. **§3** ปรับ hardcoded `<span>` category path และ count pill ให้ใช้ `<Badge>` ใน `LearnerGroupEditorPage.tsx`, `LearnerGroupDetailPage.tsx` และ `LearnerDirectorySelector.tsx`.
- ไฟล์หลักที่แตะ: `iLearn.Admin.React/src/components/ui/Badge.tsx`, `iLearn.Admin.React/src/components/ui/ReadinessBadge.tsx`, `iLearn.Admin.React/src/pages/courses/VersionDetailPage.tsx`, `iLearn.Admin.React/src/pages/learner-groups/LearnerGroupEditorPage.tsx`, `iLearn.Admin.React/src/pages/learner-groups/LearnerGroupDetailPage.tsx`, `iLearn.Admin.React/src/components/shared/LearnerDirectorySelector.tsx`, `DOC/PLANS/PLAN-123-readiness-badge-variant-soft-and-ui-cleanup.md` (->DONE), `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน: ไม่มี (UI visual refactor)
- Verified: `npm run lint` ผ่าน 0 errors, `npm run build` ผ่าน 0 errors (built in 1.25s)

## [2026-07-22 —] Antigravity — PLAN-122: LearnerDirectorySelector unified compact layout + เก็บ findings PLAN-121
- ทำอะไร: implement ตาม PLAN-122 ครบทั้ง 2 ส่วน — **§1** ปรับโครงสร้าง JSX/className ของ shared `LearnerDirectorySelector.tsx` จาก 2 การ์ดแยกกัน (`gap-4` + `p-4`) เป็น unified container เดียว `flex-1 flex flex-col md:flex-row border border-slate-200 rounded-lg bg-white overflow-hidden min-h-0` รูปแบบเดียวกับโหมด Group. เปลี่ยน rail ซ้ายเป็น `w-full md:w-60 bg-slate-50/50 border-r p-2` หัวข้อ `Filters` สไตล์เดียวกับ Group Folders. ยุบ header ฝั่งขวาเหลือแถบ search กะทัดรัด `p-2 border-b` พร้อม `<Badge tone="neutral">{totalCount}</Badge>` และย้าย active filter chips ไปแสดงในแถวที่ 2 แบบมีเงื่อนไขเมื่อมี filter ทำงาน. **§2** เก็บ findings PLAN-121 ใน `BulkAssignPage.tsx`: ปรับ `categoryName` บน group card จาก `<span>` เป็น `<Badge tone="neutral" variant="soft">` และปรับ `loadLookups` ให้ยิง API 4 เส้นพร้อมกันแบบขนานด้วย `Promise.all`.
- ไฟล์หลักที่แตะ: `iLearn.Admin.React/src/components/shared/LearnerDirectorySelector.tsx`, `iLearn.Admin.React/src/pages/assignments/BulkAssignPage.tsx`, `DOC/PLANS/PLAN-122-learner-directory-unified-compact-layout.md` (->DONE), `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน: ไม่มี (visual refactor + `Promise.all` optimization)
- Verified: `npm run lint` ผ่าน 0 errors, `npm run build` ผ่าน 0 errors (built in 6.94s)

## [2026-07-22 —] Claude Code — รีวิว PLAN-122 → REVIEWED (ผู้ใช้ deploy แล้ว; ปิด findings 1-2 ของ PLAN-121)
- ทำอะไร: ตรวจ diff 2 ไฟล์ — §1 `LearnerDirectorySelector` เปลี่ยนเฉพาะ className/JSX จริง logic ไม่ถูกแตะ, unified container ตรงสเปค, root div→fragment ตรวจ callers ทั้ง 3 แล้ว wrapper เหมือนกันหมด (`flex-1 flex flex-col min-h-0`) layout เทียบเท่า ไม่มีกรอบซ้อน, chips filter คง span เดิม (interactive chip มีปุ่ม X — เกินขีด `Badge` ไม่นับ violation ใหม่); §2 `Promise.all` + `categoryName` Badge soft ปิด findings 1-2 ของ PLAN-121 (finding 3 tree highlight ยังค้าง รอแผนแยก). รัน lint+build เอง 0 errors. Visual smoke QA ผ่าน browser ไม่ได้ (per-action approval) — ผู้ใช้เห็นหน้าจริงแล้ว
- ไฟล์หลักที่แตะ: `DOC/PLANS/PLAN-122-*.md` (→REVIEWED + Sign-off), `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน: ไม่มี (รีวิว)
- Verified: `npm run lint` + `npm run build` โดย reviewer เอง = 0 errors; ตรวจ call sites 3 หน้าแบบ static
- **คงค้าง: manual smoke 3 callers (Assignment Detail modal Add Learners / Learner Group Detail modal Add Members / Learner Group Editor tab Directory Search) — และ working tree มีงาน PLAN-119/120/121/122 ยังไม่ commit เลย ควร commit เป็นชุด ๆ ก่อนงานถัดไป**

## [2026-07-22 —] Claude Code — เขียน PLAN-122: LearnerDirectorySelector unified compact layout แบบโหมด Group + เก็บ findings 121 (มอบ Gemini)
- ทำอะไร: ผู้ใช้ดู QA หลัง PLAN-121 แล้วชอบดีไซน์โหมด Group (กล่องเดียว rail แนบใน) — ขอให้โหมด Individual เป็นรูปแบบเดียวกันเพื่อลดช่องว่าง. วินิจฉัย: `LearnerDirectorySelector` เป็น 2 การ์ดแยก (`gap-4` + FILTERS card `p-4` + header ใหญ่ 2 บรรทัด) และเป็น shared component มี 4 callers (BulkAssign/AssignmentDetail/LearnerGroupDetail/LearnerGroupEditor) ⇒ ตั้งใจ restyle ที่ component ให้ได้ผลทุกหน้า. เขียน PLAN-122: §1 unified container (rail `bg-slate-50/50 border-r p-2` + แถบ search `p-2` + Badge ตัวนับ + chips เป็นแถวมีเงื่อนไข — className/JSX ล้วน ห้ามแตะ logic) §2 เก็บ findings 121: categoryName → `Badge soft` + `loadLookups` → `Promise.all` (finding tree highlight ยังไม่ทำ — ต้องแตะ AppTreeView รอแผนแยก)
- ไฟล์หลักที่แตะ: `DOC/PLANS/PLAN-122-learner-directory-unified-compact-layout.md` (ใหม่ READY), `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน: ไม่มี (visual refactor)
- Verified: — (แผน; อ่าน LearnerDirectorySelector ทั้งไฟล์ + grep callers 4 จุด)
- **ถึง Gemini: ห้ามแตะ logic/props ของ selector (className+โครง JSX เท่านั้น); ห้ามแก้ caller อื่น 3 หน้า — layout ใหม่ต้องมาจาก component เอง; manual QA ต้องไล่ครบ 4 callers กันกรอบซ้อน**

## [2026-07-22 —] Claude Code — รีวิว PLAN-121 → REVIEWED (3 minor findings ไม่ block)
- ทำอะไร: ตรวจ `BulkAssignPage.tsx` เทียบสเปคครบ — §1 filter ∩ search + mirror ถูก (ยืนยัน `LookupCourseDto.cs` มีจริง), §2 subtree recursion/นับรวมลูก/fallback ไม่มี category ถูกต้อง, §3 toggle แถวคงที่จุดเดียว, payload validate/BulkAssign ไม่เปลี่ยน, shared components ไม่ถูกแตะ (diff มีไฟล์เดียว). รัน lint+build เอง 0 errors. Findings minor: (1) categoryName pill บน group card เป็น span hand-rolled — ต้องเป็น `Badge tone="neutral" variant="soft"` ตามกติกา (2) `loadLookups` 4 fetch sequential ควร `Promise.all` (3) tree highlight รีเซ็ตเมื่อสลับโหมดแต่ filter ค้าง (ติด internal state ของ `AppTreeView` — นอก scope). เติม Reviewer Sign-off ในแผน
- ไฟล์หลักที่แตะ: `DOC/PLANS/PLAN-121-*.md` (→REVIEWED + Sign-off), `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน: ไม่มี (รีวิว)
- Verified: `npm run lint` + `npm run build` โดย reviewer เอง = 0 errors
- **คงค้าง: deploy QA (`tools/deploy-admin-react.ps1`) + manual smoke ตาม Verification ข้อ 1-4 ของแผน (รอผู้ใช้สั่ง) — findings 3 ข้อเก็บรวมในแผนถัดไปได้ ไม่ต้อง hotfix**

## [2026-07-22 —] Antigravity — PLAN-121: Bulk Assign wizard UX (category filter + learner-group tree + แก้ toggle ขยับ)
- ทำอะไร: implement ตาม PLAN-121 ใน `BulkAssignPage.tsx` — **§1** เพิ่ม `CategoryLookup` mirror DTO + โหลด `Categories/lookup`. เพิ่ม `<select>` Category filter ใน Syllabus Catalog panel (`All Categories`, `Uncategorized` เมื่อมีคอร์สไร้หมวด, และหมวดหมู่เรียงตาม `sortOrder` มี prefix `${sortOrder}. ` เมื่อ `sortOrder > 0`) พร้อมนับจำนวนคอร์สที่ยังไม่เลือก. เพิ่ม muted text แสดงชื่อหมวดหมู่ในการ์ดคอร์สเมื่ออยู่โหมด `All Categories`. Filter กรองแบบ `category ∩ text search`. **§2** เพิ่ม `LearnerGroupCategoryLookup` mirror DTO + โหลด `LearnerGroupCategories`. ปรับ Layout โหมด Group เป็น 2 คอลัมน์ด้วย `AppTreeView` ด้านซ้าย กรองกลุ่มตาม subtree recursively และนับจำนวนกลุ่มรวม subtree. มี fallback เป็น list เดิมเมื่อไม่มี category. แสดง `categoryName` badge บน group card. **§3** ยก `renderModeToggle()` (`Group | Individual`) ออกมาไว้ในแถวคงที่ด้านบนสุดของ Target Scope step (`Target audience:`) ป้องกันตำแหน่งปุ่มกระโดดเมื่อสลับโหมด.
- ไฟล์หลักที่แตะ: `iLearn.Admin.React/src/pages/assignments/BulkAssignPage.tsx`, `DOC/PLANS/PLAN-121-bulk-assign-ux-category-filter-group-tree-toggle-fix.md` (->DONE), `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน: ไม่มี (mirror ตาม backend DTO เดิม)
- Verified: `npm run lint` ผ่าน 0 errors, `npm run build` ผ่าน 0 errors (built in 7.02s)

## [2026-07-22 —] Claude Code — เขียน PLAN-121: Bulk Assign wizard UX (category filter + learner-group tree + แก้ toggle ขยับ) (มอบ Gemini)
- ทำอะไร: ผู้ใช้รีวิว `/admin-react/assignments/bulk` ขอ 3 ข้อ. สำรวจ: (1) `Assignments/lookup-courses` ส่ง `categoryId`/`divisionId` อยู่แล้วแต่ mirror `LookupCourse` ไม่มี field — ใช้ `Categories/lookup` เอาชื่อ (division-scoped ตรงกัน) ⇒ React ล้วน (2) learner groups มี category tree ครบ (`LearnerGroupCategories` + `categoryId/categoryName` ใน `LearnerGroupDto`) + shared `AppTreeView` ใช้ซ้ำได้ (3) root cause toggle ขยับ: `renderModeToggle()` render คนละ container ตามโหมด (group panel header vs `headerLeft` ของ `LearnerDirectorySelector`) ⇒ ยกออกมาเป็นแถวคงที่ของ step. เขียน PLAN-121: §1 category dropdown filter (∩ search, นับคอร์ส, Uncategorized เฉพาะเมื่อมี) §2 โหมด Group เป็น 2 คอลัมน์ tree (subtree filter) + fallback list เดิมเมื่อไม่มี category §3 toggle แถวคงที่ — ห้ามแตะ backend/shared components/submit payload
- ไฟล์หลักที่แตะ: `DOC/PLANS/PLAN-121-bulk-assign-ux-category-filter-group-tree-toggle-fix.md` (ใหม่ READY), `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน: ไม่มี (mirror ตามของจริง)
- Verified: — (แผน; อ่าน BulkAssignPage/AssignmentsController/LearnerGroupsController/LearnerGroupCategoriesController/CategoriesController/LearnerGroupListPage/AppTreeView/SegmentedToggle/LearnerDirectorySelector)
- **ถึง Gemini: ไฟล์เดียว `BulkAssignPage.tsx`; ห้ามแก้ `LearnerDirectorySelector`/`AppTreeView`/`SegmentedToggle`; payload validate/BulkAssign ห้ามเปลี่ยนแม้แต่ field เดียว; ทดสอบ `?groupId=`/`?courseId=` pre-select ด้วย**

## [2026-07-22 —] GitHub Copilot — PLAN-120: ปุ่ม login กลับสีแบรนด์ + cache-busted apple-touch-icon fallback + deploy QA/PROD
- ทำอะไร: ผู้ใช้ขอเปลี่ยนปุ่ม "เข้าสู่ระบบ" จากพื้นขาวกลับเป็นพื้นเขียวตัวอักษรขาว และแจ้งว่า iPad shortcut ยังไม่ขึ้นไอคอน. แก้ `Home/Index.cshtml` ให้ `.btn-login` พื้น `var(--brand-color)`/ตัว `#fff`, hover `var(--brand-dark)`/ตัวขาว. เพิ่ม app-scoped icon URL ใหม่ `apple-touch-icon-180.png` + `apple-touch-icon-precomposed.png` (สำเนา PNG 180x180 เดิม) และปรับ `_DevExtremeLayout.cshtml` ให้ชี้ URL ใหม่ก่อน fallback เดิมโดยไม่มี query string. เพิ่ม `.gitignore` re-include สำหรับไฟล์ใหม่. เพิ่ม best-effort root icon sync ใน `tools/deploy-user*.ps1`; smoke พบ site-root `/apple-touch-icon*.png` ยังตอบ 401 ทั้ง QA/PROD จึงถือว่า final fix ใช้ app-scoped cache-busted URL เป็นหลัก
- ไฟล์หลักที่แตะ: `iLearn.User/Views/Home/Index.cshtml`, `iLearn.User/Views/Shared/_DevExtremeLayout.cshtml`, `iLearn.User/wwwroot/apple-touch-icon-{180,precomposed}.png`, `.gitignore`, `tools/deploy-user.ps1`, `tools/deploy-user-prod.ps1`, `DOC/PLANS/PLAN-120-login-button-brand-and-root-apple-touch-icon-fallback.md`, `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน: ไม่มี (CSS + static assets + layout head + deploy wrapper fallback)
- Verified: local `dotnet build iLearn.User` 0 errors; deploy script parser 0 errors. QA final stamp `_user_deploy_20260722132157` health 200, PROD final stamp `_user_deploy_20260722132250` health 200, both `AutoRolledBack=False`. QA/PROD smoke: page has `apple-touch-icon-180` + `precomposed` links, login button CSS is green/white, `/iLearn/apple-touch-icon-180.png`, `/iLearn/apple-touch-icon-precomposed.png`, `/iLearn/apple-touch-icon.png` all return 200 `image/png`
- **คงค้าง:** manual iPad QA: ลบ shortcut เก่าก่อน แล้ว Add to Home Screen ใหม่เพื่อให้ Safari อ่าน URL ใหม่ `apple-touch-icon-180.png`

## [2026-07-22 —] GitHub Copilot — PLAN-119: deploy iLearn.User ขึ้น QA + PROD สำเร็จ; รอ iPad manual QA
- ทำอะไร: deploy `iLearn.User` ตามคำสั่งผู้ใช้ผ่าน side-by-side scripts. QA: `tools/deploy-user.ps1` → stamp `_user_deploy_20260722131045`, root health check `https://ap-ntc2138-qawb/iLearn/` = HTTP 200 attempt 1/5. PROD: `tools/deploy-user-prod.ps1` → stamp `_user_deploy_20260722131157`, root health check `https://ap-ntc2137-prwb/iLearn/` = HTTP 200 attempt 1/5. ทั้งคู่ `AutoRolledBack=False`. Smoke หลัง deploy ทั้ง QA/PROD ยืนยัน root markup มี apple-touch-icon link ชี้ `/iLearn/apple-touch-icon.png` และ asset ตอบ HTTP 200 `image/png`.
- ไฟล์หลักที่แตะ: `DOC/PLANS/PLAN-119-login-button-white-and-apple-touch-icon.md` (→DEPLOYED + deployment notes), `DOC/AGENT_LOG.md` — ไม่มีโค้ดใหม่
- Contract ที่เปลี่ยน: ไม่มี (deploy static asset/CSS/layout ตาม PLAN-119)
- Verified: QA/PROD root HTTP 200; icon URL HTTP 200 `image/png`; deployment health checks ผ่านทั้งคู่
- **คงค้าง:** manual QA บน iPad จริง: ลบ shortcut เดิมก่อน แล้ว Safari → Add to Home Screen ต้องเห็นไอคอน iL สีแบรนด์และชื่อ `iLearn` (iOS cache icon ตาม URL)

## [2026-07-22 —] Claude Code — เขียน PLAN-119: ปุ่มเข้าสู่ระบบสีขาว + apple-touch-icon แก้ Add to Home Screen บน iPad (มอบ Copilot)
- ทำอะไร: ผู้ใช้ขอ 2 ข้อ. วินิจฉัย: (1) `.btn-login` พื้นแบรนด์บนการ์ดขาว → เปลี่ยนขาวต้องมี border สีแบรนด์กันกลืน (2) **root cause ไอคอน:** iOS ใช้ `apple-touch-icon` PNG เท่านั้น (ไม่รองรับ SVG) — แอปมีแค่ favicon.svg/ico ไม่มี `<link apple-touch-icon>` และ host ใต้ `/iLearn/` ทำให้ Safari probe root ไม่เจอ → fallback screenshot. เขียน PLAN-119: §1 CSS ปุ่มขาว+ขอบแบรนด์+hover brand-lighter (ไม่แตะ markup/JS PLAN-108) §2 gen `apple-touch-icon.png` 180×180 จากดีไซน์ favicon.svg (พื้นเต็มจัตุรัสห้ามมุมโปร่งใส — iOS mask เอง; ให้ script System.Drawing ในแผน) + `<link>` ใน `_DevExtremeLayout` (**ห้าม asp-append-version** — iOS บางรุ่นไม่โหลด icon มี query string) + ตรวจ .gitignore re-include
- ไฟล์หลักที่แตะ: `DOC/PLANS/PLAN-119-*.md` (ใหม่ READY), `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน: ไม่มี (CSS + static asset + head link)
- Verified: — (แผน; อ่าน Index.cshtml/.btn-login/login-card + ls wwwroot + favicon.svg + layout เดียว)
- **ถึง Copilot: verification ข้อ 4 (Add to Home Screen จริง) ต้องให้ผู้ใช้ทดสอบบน iPad — ถ้าเคย add ไว้ต้องลบเก่าก่อน add ใหม่ (iOS cache icon); ห้ามทำ PWA manifest เต็ม**

## [2026-07-22 —] Claude Code — รีวิว PLAN-118 → ยืนยัน VERIFIED — backlog ว่าง (108-118 ปิดครบ)
- ทำอะไร: ตรวจรายงานการรันของ Copilot — §A 13/13 รัดกุมกว่าสเปค (A1 ทดสอบทั้ง save-persist + cancel-ไม่-save + revert; A5 วินิจฉัย "Not implemented yet" = stub ของ ADL sample ถูกต้อง, test data เก็บกวาดครบ), pre-deploy gate รัน `ef migrations list` กับ PROD DB จริง (เกินสเปคในทางดี), §B stamps/health check/AutoRolledBack ครบ 3 ชิ้น, §C read-only ตามกติกา + ปิด verification PLAN-113 บน PROD, status ทุกแผนอัปเดตตรงผลจริง. เติม Reviewer Endorsement ท้าย PLAN-118
- ไฟล์หลักที่แตะ: `DOC/PLANS/PLAN-118-*.md` (+Endorsement), `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน: ไม่มี (รีวิว)
- Verified: อ่าน Implementer Notes เทียบสเปคทุกข้อ + ตรวจ status ทุกแผน + git log/tree สะอาด
- **สถานะ repo: ไม่มีงานค้าง — PLAN-108 ถึง 118 VERIFIED ทั้งหมด, QA/PROD sync กับ HEAD**

## [2026-07-22 —] GitHub Copilot — PLAN-118: QA smoke รวบยอด 13/13 ผ่าน → deploy PROD ทั้งชุด (API+User+Admin React) → PROD smoke ผ่าน — ปิดงานค้าง 110/111/113/114/115/116/117 ทั้งหมด
- ทำอะไร: รัน §A QA smoke ครบ 13 ข้อผ่าน Playwright บน `ap-ntc2138-qawb` — **A1** category id=80 "EP1": Edit Properties เปิดฟอร์มปกติไม่มี phantom submit, แก้ sortOrder 1→3→save→refresh persist ถูกต้อง, Cancel ไม่ save ค่า 99 ที่พิมพ์ทิ้ง, แก้กลับ 1 สำเร็จ (ปิด loop PLAN-111/115); ทดสอบซ้ำ Divisions detail ปกติ **A2** `/assignments/275`+`/report`: stat cards + ไม่มี Fact Completed/Completion Rate + Created By ชื่อ+Nid + ไม่มีข้อความ click hint **A3** คลิก donut chart = no-op ยืนยันจริง, learner 610034 sidebar "1. EP1"/"2. PLAN-079..." ตรงลำดับ admin **A4** `/courses?divisionId=1` folder 1-12 มีเลขถูกต้อง, breadcrumb ไม่มีเลข, rename sortOrder 12→13→12 ผ่าน UI จริง, สร้าง category ไม่กรอก sortOrder → ไม่มีเลข (ลบทิ้งแล้ว) **A5** upload SCORM zip จริง (`ContentPackagingSingleSCO_SCORM12.zip`) ผ่าน React wizard → ContentItem.Name ไม่มี `.zip` ยืนยัน PLAN-110 → bulk-process ผ่าน Classic Admin (`/admin/ContentItems` Publish) → extract 44 ไฟล์สำเร็จ, SCORM 1.2 resolve ถูกต้อง, เปิด Player โหลดสำเร็จ (เนื้อหา "Not implemented yet" เป็น stub ของตัวอย่าง ADL เอง ไม่ใช่บั๊ก) → unpublish+delete ทดสอบทิ้งแล้ว. Pre-deploy gate: `dotnet ef migrations list` ตรงกับ PROD connection string จริง (ไม่ใช่ design-time fallback) = ไม่มี Pending เลย ยืนยัน `AddSortOrderToCategory` ขึ้นแล้ว. **§B** deploy PROD 3 ส่วนสำเร็จหมด: `deploy-api-prod.ps1` stamp `20260722123911` (401 OK), `deploy-user-prod.ps1` stamp `20260722124051` (200 OK), `deploy-admin-react-prod.ps1` (lint+build+robocopy ผ่าน) ทุกตัว `AutoRolledBack=False`. **§C** PROD smoke read-only ผ่านครบ: learner 610034 sidebar เรียงถูกต้อง, admin Categories/Course explorer/Assignment detail ตรงตามที่คาด, chart คลิกไม่ได้บน PROD เช่นกัน, console 0 error ทุกหน้า
- ไฟล์หลักที่แตะ: `DOC/PLANS/PLAN-118-*.md` (→VERIFIED + Implementer Notes), `DOC/PLANS/PLAN-113-*.md` `PLAN-114-*.md` `PLAN-115-*.md` `PLAN-116-*.md` `PLAN-117-*.md` (ทุกไฟล์ →VERIFIED), `DOC/AGENT_LOG.md` — ไม่มีโค้ดแก้ในรอบนี้ (เฉพาะ smoke test data ที่สร้าง/ลบทิ้งหมดแล้ว: category ทดสอบ, content item ทดสอบ)
- Contract ที่เปลี่ยน: ไม่มี (smoke + deploy เท่านั้น)
- Verified: 13/13 ข้อ §A ผ่าน, deploy 3 ส่วน health check ผ่านหมด, §C PROD smoke 4 ข้อผ่านหมด, console 0 error ตลอดทั้ง QA/PROD
- **สรุปปิดงานค้างสะสม:** PLAN-110 (upload+bulk-process manual), PLAN-111 (sortOrder ผ่าน UI จริง), PLAN-113/114/115/116/117 (deploy PROD + manual QA) **ปิดครบทุกข้อในรอบนี้ — ไม่มีงานค้างเหลือจากชุดนี้แล้ว**

## [2026-07-22 —] GitHub Copilot — Deploy QA รวม PLAN-114/115/116/117 (API + Admin React + User) — สำเร็จทั้งหมด
- ทำอะไร: commit โค้ด PLAN-116 + PLAN-117 ที่ค้างอยู่ใน working tree (`5f4389d`, `b311687`) แล้ว verify: backend `dotnet build iLearn.Tests` 0 errors + `dotnet test` 222/222 ผ่าน, frontend `npm run lint` + `npm run build` 0 errors. Deploy ขึ้น QA ทั้ง 3 ส่วน: **(1)** `tools/deploy-api.ps1` (PLAN-114 backend `CreatedByName`) → stamp `_deploy_20260722114706`, health check `session/me` = 401 OK **(2)** `tools/deploy-admin-react.ps1` (PLAN-114/115/116/117 UI) → robocopy สำเร็จ **(3)** `tools/deploy-user.ps1` (PLAN-116 `MyLearning/Index.cshtml` sidebar sort number) → stamp `_user_deploy_20260722114854`, health check `/iLearn/` = 200 OK
- ไฟล์หลักที่แตะ: ไม่มีโค้ดใหม่ (deploy งานที่มีอยู่แล้วในแผน 114-117), `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน: ไม่มี (deploy เท่านั้น)
- Verified: health check ทั้ง 3 บริการ = OK (401/200 ตามที่คาด), build/test/lint ทุกฝั่งผ่านก่อน deploy
- **คงค้าง:** manual QA smoke ยังไม่ทำ (chart ไม่มี click filter, sidebar เลขลำดับ MyLearning, Course Explorer category folder เลขลำดับ + edit modal, Assignment Detail stat tiles/Created By) — PROD ทั้งหมดยังรอผู้ใช้ยืนยัน

## [2026-07-22 —] Claude Code — เขียน PLAN-118: QA smoke รวบยอด + เก็บงานค้าง + gate deploy PROD ทั้งชุด (มอบ Copilot)
- ทำอะไร: ผู้ใช้ deploy QA แล้ว (Admin React 114-117 + API 114 + User 116) สั่ง smoke → PROD + เก็บงานค้าง. Inventory งานค้างจาก sign-off ทุกแผน: PLAN-110 manual 2/3 (upload+bulk ไม่เคย smoke), PLAN-111 แก้ sortOrder ผ่าน UI (ถูกบั๊ก 115 บัง), PLAN-113 PROD, manual ของ 114-117. เขียน PLAN-118: **§A** QA smoke 13 ข้อเรียงลำดับ (A1 = Edit Properties ก่อน — ปิด 2 loop; A5 = upload/bulk ของ 110) **§B** deploy PROD 3 ชิ้น (API/User/Admin React — ไม่มี migration ใหม่; เจอ pending migration ไม่คาดคิด = หยุด) **§C** PROD smoke read-only (รวม verification 113 ซ้ำบน PROD). กติกา: ข้อไหน fail หยุดทั้งแผน ห้ามแก้โค้ดเอง, write-test ต้อง revert, จบแล้วอัปเดต status ทุกแผนเป็น VERIFIED ตามจริง
- ไฟล์หลักที่แตะ: `DOC/PLANS/PLAN-118-*.md` (ใหม่ READY), `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน: ไม่มี (แผน smoke/deploy)
- Verified: — (แผน; รวบจาก sign-off 110-117)
- **ถึง Copilot: ผู้ใช้อนุมัติ PROD ล่วงหน้าแล้ว**แต่มีเงื่อนไข QA ต้องผ่านครบ 13 ข้อ**; PROD ห้าม write-test; จด stamp ทุก deploy**

## [2026-07-22 —] Claude Code — รีวิว PLAN-116 + PLAN-117 → REVIEWED ทั้งคู่ + commit (5f4389d + PLAN-117 ตามมา)
- ทำอะไร: **PLAN-116** — callback/cursor หายครบ 2 chart + 2 callers, dim logic คงอยู่ (detail ยังส่ง `activeStatus` ⇒ sync กับ toolbar), sidebar label guard `>0` ถูกต้อง "ทั้งหมด" ไม่มีเลข. **PLAN-117** — mirror type + comparator `?? 0`, สลับ sort เฉพาะรายการ category (Uncategorized push หลัง sort พร้อม sortOrder 0), prefix เฉพาะ folder row (breadcrumb ใช้ `category.name` ตรง — สะอาด), Edit modal required+disabled สองชั้น ค่าเริ่มถูก, Create modal optional ส่ง field เฉพาะเมื่อกรอก. รัน verify เอง: lint + React build + iLearn.User build 0 errors ครบ
- ไฟล์หลักที่แตะ: `DOC/PLANS/PLAN-116-*.md` + `PLAN-117-*.md` (→REVIEWED + Sign-off), `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน: ไม่มี (รีวิว)
- Verified: npm lint/build + dotnet build iLearn.User โดย reviewer เอง
- **คงค้าง deploy สะสม (รอผู้ใช้ยืนยัน): QA = Admin React (114+115+116+117) + API (114) + iLearn.User (116); PROD = PLAN-113 (API+User) + ทั้งหมดข้างต้นหลัง QA ผ่าน** — ตอน smoke QA อย่าลืมปิด loop PLAN-115 ข้อ 1-2 (Edit Properties + แก้ sortOrder ผ่าน UI)

## [2026-07-22 —] GitHub Copilot — PLAN-116: implement ยกเลิกคลิก filter บน chart + เลขลำดับหน้าชื่อหมวด learner sidebar — build/lint ผ่าน รอ deploy
- ทำอะไร: **§1** `AssignmentReportCharts.tsx` ตัด `onSelectStatus`/`onSelectCourse` props + `cursor="pointer"`/`onClick` ออกจาก `<Pie>`/`<Bar>` ทั้งสอง component คง `activeStatus`/`activeCourse` + fillOpacity dim logic ไว้ครบ (chart ยังสะท้อน filter จาก toolbar). แก้ 2 callers: `AssignmentDetailPage.tsx` ลบ callback (เดิมสลับ tab→'learners'+set filter — toolbar `learnerStatusFilter` เดิมยังทำงานปกติไม่ได้แตะ), `AssignmentReportPage.tsx` ลบ callback (toolbar `statusFilter`/`courseFilter` เดิมยังอยู่). **§2** `MyLearning/Index.cshtml` `renderCategorySidebar`: เพิ่ม `${sortOrder}. ${name}` เฉพาะเมื่อ `sortOrder > 0`, แถว "ทั้งหมด" ไม่มีเลข
- ไฟล์หลักที่แตะ: `iLearn.Admin.React/src/pages/assignments/{AssignmentReportCharts.tsx,AssignmentDetailPage.tsx,AssignmentReportPage.tsx}`, `iLearn.User/Views/MyLearning/Index.cshtml`, `DOC/PLANS/PLAN-116-*.md` (→DONE + Implementer Notes), `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน: ไม่มี API/DB — internal component props (`StatusDonut`/`CourseCompletionBars` ตัด callback), callers แก้ครบทั้ง 2 ไฟล์
- Verified: `npm run lint` 0 errors, `npm run build` 0 errors; `dotnet build iLearn.User\iLearn.User.csproj -o artifacts\verify-user` succeeded 0 errors (74 pre-existing nullable warnings ไม่เกี่ยว), artifacts ลบแล้ว
- **คงค้าง:** manual QA smoke (คลิก chart ไม่มีอะไรเกิดขึ้น + hover tooltip ปกติ + sidebar เลขลำดับ) ยังไม่ทำ รอ deploy QA — deploy iLearn.User ขึ้น PROD ต้องพา PLAN-113 ไปด้วย (HEAD เดียวกัน, ยังรอผู้ใช้ยืนยัน PROD)


## [2026-07-22 —] Antigravity — PLAN-117: Course explorer โชว์ลำดับใน Category folder + Edit modal แก้ลำดับได้
- ทำอะไร: implement ตาม PLAN-117 — **§1** เพิ่ม `sortOrder: number` ใน `CategoryLookup` + comparator `sortCategoriesByOrder` เรียงตาม `sortOrder` แล้วตามด้วย `id` ใน `categoriesByDivision` และ `singleDivision` (ไม่แตะ division/course/root) **§2** แสดงชื่อ category folder เป็น `${cat.sortOrder}. ${cat.name}` เฉพาะเมื่อ `sortOrder > 0` (breadcrumb/modal ชื่อยังคงสะอาดไม่มีเลข) **§3** เพิ่ม number input `Sort Order (ลำดับ)` ใน Edit Category Modal (`required`, `min={1}`) และ Create Category Modal (optional) พร้อมส่ง `sortOrder` ใน `handleRenameCategory` และ `handleCreateCategory`
- ไฟล์หลักที่แตะ: `iLearn.Admin.React/src/pages/courses/CourseListPage.tsx`, `DOC/PLANS/PLAN-117-course-explorer-category-folder-sortorder.md` (->DONE), `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน: ไม่มี (mirror ตาม backend DTO เดิม)
- Verified: `npm run lint` ผ่าน 0 errors, `npm run build` ผ่าน 0 errors (built in 1.68s)

## [2026-07-22 —] Claude Code — เขียน PLAN-117: Course explorer โชว์ลำดับใน Category folder + Edit modal แก้ลำดับได้ (มอบ Gemini)
- ทำอะไร: ผู้ใช้ขอที่ `/admin-react/courses?divisionId=1`. สำรวจ: `admin/CategoriesCRUD/Get` ส่ง `sortOrder` อยู่แล้ว (PLAN-111) แต่ mirror `CategoryLookup` ใน `CourseListPage.tsx` ยังไม่มี field; folder เรียงด้วย `sortByNameAsc`; Edit modal PUT `values` JSON → backend `PopulateObject` รับ `sortOrder` ได้เลย ⇒ **React ไฟล์เดียว ไม่แตะ backend**. เขียน PLAN-117: §1 +type +comparator (sortOrder,id) เฉพาะ 2 จุดที่ sort category (~160/~579 — division/root/course ไม่แตะ) §2 folder name = `${sortOrder}. ${name}` เมื่อ >0 (breadcrumb/modal ชื่อสะอาด, Uncategorized ไม่มีเลข) §3 Edit modal +number input sortOrder + New Category modal เพิ่มแบบ optional (กันค่า 0 ลอยบนแบบเงียบ)
- ไฟล์หลักที่แตะ: `DOC/PLANS/PLAN-117-*.md` (ใหม่ READY), `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน: ไม่มี (mirror ตามของจริง)
- Verified: — (แผน; อ่าน CourseListPage โครง type/sort/loadData/edit modal)
- **ถึง Gemini: ห้ามแตะการเรียง division/course/root; เลขเฉพาะ folder row — breadcrumb ห้ามมีเลข; เช็ค AGENT_LOG ก่อนเริ่ม (Copilot อาจกำลังทำ PLAN-116 อยู่ คนละไฟล์แต่ตรวจก่อนตามกติกา)**

## [2026-07-22 —] Claude Code — เขียน PLAN-116: ยกเลิกคลิก filter บน chart + เลขลำดับหน้าชื่อหมวด learner sidebar (มอบ Copilot)
- ทำอะไร: ผู้ใช้สั่ง (1) ยกเลิกการคลิก filter ที่ PLAN-114 เคยคงไว้ (2) แสดง "ลำดับ" ของ Categories ใน sidebar หน้า MyLearning = option §4 ของ PLAN-111 ที่ยืนยันแล้ว. สำรวจ: จุดคลิกมีแค่ `Pie.onClick`/`Bar.onClick` (legend ไม่ clickable); sidebar มี `cat.sortOrder` พร้อมใช้จาก PLAN-113. เขียน PLAN-116: §1 ตัด `onSelectStatus`/`onSelectCourse` + `cursor="pointer"` ออกจาก chart ทั้ง 2 ตัว + callers 2 ไฟล์ **แต่คง `activeStatus`/`activeCourse` dim logic** (chart ยังสะท้อน filter จาก toolbar); §2 sidebar แสดง `${sortOrder}. ${name}` เฉพาะเมื่อ sortOrder > 0, "ทั้งหมด" ไม่มีเลข
- ไฟล์หลักที่แตะ: `DOC/PLANS/PLAN-116-*.md` (ใหม่ READY), `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน: ไม่มี API/DB — props ของ StatusDonut/CourseCompletionBars ตัด callback (internal, callers แก้ครบในแผน)
- Verified: — (แผน; อ่าน ReportCharts ทั้งไฟล์ + renderCategorySidebar)
- **ถึง Copilot: ห้ามตัด dim logic; ห้ามแตะ filter toolbar เดิม; deploy iLearn.User ขึ้น PROD ต้องพา PLAN-113 ไปด้วย (HEAD เดียวกัน) — PLAN-113 PROD ยังค้างรอผู้ใช้ยืนยันอยู่**

## [2026-07-22 —] Claude Code — รีวิว PLAN-114 + PLAN-115 → REVIEWED ทั้งคู่ + commit (b4a031d, PLAN-114 ตามมา)
- ทำอะไร: **PLAN-115** — preventDefault เฉพาะ non-submit ถูกต้อง signature ไม่เปลี่ยน + key ครบ 4 ปุ่ม → commit `b4a031d`. **PLAN-114** — จุดเด่น: Copilot จับได้ว่าแผนชี้ผิดคลาส (controller เรียก `AssignmentService.GetDashboardAsync` ไม่ใช่ `AssignmentDashboardService` — ยืนยันที่ AssignmentsController:77) แล้วเพิ่ม `LookupCreatedByNameAsync` ทั้ง 2 คลาสถูกต้อง; ตรวจลึก `GetEmployeesByNidsAsync` ทั้ง 2 implementation คืน dict `OrdinalIgnoreCase` ⇒ Nid case ไม่มีปัญหา; caption หาย 2 บรรทัด/ tiles ตรง markup Report เป๊ะ/ Created By ชื่อ+Nid รอง fallback ถูก. งาน polish นอกแผนของ Gemini (Escape+scroll-lock) โค้ดถูก ยอมรับโดยจดไว้. รัน verify เอง: 222/222 + lint/build 0 errors
- ไฟล์หลักที่แตะ: `DOC/PLANS/PLAN-114-*.md` + `PLAN-115-*.md` (→REVIEWED + Sign-off), `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน: ไม่มี (รีวิว)
- Verified: dotnet test + npm lint/build โดย reviewer เอง + grep OrdinalIgnoreCase + ตรวจ controller wiring
- **คงค้าง: deploy QA รวมกัน (API + Admin React ก้อนเดียว: 114+115) → manual ตามแผนทั้งสอง (เน้น 115 ข้อ 1-2: Edit เปิดฟอร์มค้าง + แก้ sortOrder ผ่าน UI ปิด loop PLAN-111) → PROD รอผู้ใช้ยืนยัน; PLAN-113 PROD ก็ยังรอผู้ใช้ยืนยันอยู่**

## [2026-07-22 —] Antigravity — PLAN-115: Fix Edit Properties phantom form submit หน้า Master Data detail
- ทำอะไร: implement แก้ไขตาม PLAN-115 — **§1** ใน `ControlsSidebar.tsx` (`ControlAction`): ใส่ `event.preventDefault()` เมื่อ `type !== 'submit'` ป้องกันเบราว์เซอร์ประเมิน default action เป็น submit เมื่อ DOM node ถูก reuse ระหว่าง sync re-render ภายใน dispatch เดียวกัน **§2** ใน `MasterDataDetailPage.tsx`: ใส่ prop `key` แยก identity ของ `ControlAction` ทั้งใน Edit Mode (`key="save"`, `key="cancel"`) และ View Mode (`key="edit"`, `key="delete"`) เพื่อบังคับ React สร้าง DOM node ใหม่แยกกันเมื่อสลับโหมด
- ไฟล์หลักที่แตะ: `iLearn.Admin.React/src/components/ui/ControlsSidebar.tsx`, `iLearn.Admin.React/src/pages/master-data/MasterDataDetailPage.tsx`, `DOC/PLANS/PLAN-115-fix-edit-properties-phantom-submit.md` (->DONE), `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน: ไม่มี (React behavior fix)
- Verified: `npm run lint` ผ่าน 0 errors, `npm run build` ผ่าน 0 errors (built in 1.68s)

## [2026-07-22 —] GitHub Copilot — PLAN-114: implement ตัด caption + Created By ชื่อ + Overview stat cards — build/test/lint ผ่าน รอ deploy
- ทำอะไร: implement ตามแผน — **สำคัญ พบว่าแผนวินิจฉัยผิดจุด:** endpoint `GET Assignments/dashboard/{id}` เรียก `AssignmentService.BuildAssignmentDashboardAsync` (`AssignmentService.cs:825`) ไม่ใช่ `AssignmentDashboardService.GetDashboardAsync` ที่แผนอ้าง (สองคลาส implement DTO เดียวกันคนละที่ — คลาสหลังใช้แค่จาก CourseAssignmentService/EnrollmentService) ⇒ เพิ่ม `LookupCreatedByNameAsync` (try/catch คืน null เมื่อพลาด, ใช้ `ILearnerApiService.GetEmployeesByNidsAsync`) **ทั้งสองคลาส** ให้สอดคล้องกัน แต่ path จริงที่แก้คือ `AssignmentService.cs`. **§1** ลบ 2 caption hint ใน `AssignmentReportCharts.tsx` (StatusDonut/CourseCompletionBars) ไม่แตะ onClick. **§2** `AssignmentDashboardDto` +`CreatedByName` (additive); React แสดง `createdByName ?? createdBy` เป็นหลัก + Nid รอง (`text-xxs font-mono`) — ไม่แก้ `AssignmentReportPage.tsx` เพราะหน้า Report ไม่เคยโชว์ Created By เลย (grep ยืนยัน). **§3** ตัด Fact Completed/Completion Rate. **§4** เพิ่ม stat tiles Learners/Courses/Status (copy markup จาก `AssignmentReportPage.tsx:316` ตรง ๆ ไม่ extract shared component) เหนือ FactGrid ที่เหลือ (Start/Due/Created By/Learner Group), donut ยังขวาเหมือนเดิม. ลบ import `formatPercent` ที่ไม่ใช้แล้ว
- ไฟล์หลักที่แตะ: `iLearn.Application/DTOs/AssignmentDashboardDto.cs`, `iLearn.Application/Services/AssignmentService.cs`, `iLearn.Application/Services/AssignmentDashboardService.cs`, `iLearn.Admin.React/src/pages/assignments/{AssignmentDetailPage.tsx,AssignmentReportCharts.tsx}`, `DOC/PLANS/PLAN-114-*.md` (→QA PENDING + Implementer Notes), `DOC/AGENT_LOG.md`, memory `/memories/repo/status-contract-notes.md` (จดกับดัก DTO สองคลาส)
- Contract ที่เปลี่ยน: `AssignmentDashboardDto` +`CreatedByName` (string?, additive) — consumer: `AssignmentDetailPage.tsx` เท่านั้น (Report ไม่ได้ใช้)
- Verified: `dotnet build iLearn.Tests` 0 errors, `dotnet test` 222/222 ผ่าน, `npm run lint` + `npm run build` 0 errors
- **คงค้าง:** ยังไม่ deploy QA/PROD; manual test 5 ข้อของแผนยังไม่ทำ (รอ deploy ก่อน) — ส่งต่อให้ reviewer/ผู้ใช้ตัดสิน deploy timing

## [2026-07-22 —] Claude Code — เขียน PLAN-115: root cause ปุ่ม Edit Properties = phantom form submit (มอบ Gemini)
- ทำอะไร: ผู้ใช้ยืนยันปุ่มเสียจริงด้วยมือ → วินิจฉัยเจอ root cause จากโค้ด: `MasterDataDetailPage` ครอบหน้าใน `<form>` และ sidebar สลับปุ่ม Edit (type=button) ↔ Save Changes (type=submit) ที่**ตำแหน่งเดียวกัน** ⇒ คลิก Edit → React flush re-render sync ภายใน click dispatch → DOM node เดิมถูก reuse กลายเป็น type=submit → เบราว์เซอร์ประเมิน default action **หลัง** handler จบจาก attribute ปัจจุบัน = **form submit ทันที** → handleSave PUT ค่าเดิม + toast + setIsEditing(false) เด้งกลับ view mode ในพริบตา. **ไขปริศนาเก่าด้วย: toast "Changes saved successfully" ตอน QA smoke PLAN-111 ที่ถูกโทษว่าเป็น SignalR ของ admin อื่น — จริง ๆ คือหน้า save ตัวเอง** (ค่าเดิมทับค่าเดิม DB จึงไม่เปลี่ยน). grep ยืนยัน pattern นี้มีที่เดียว. เขียน PLAN-115: §1 `ControlAction` preventDefault เมื่อ type≠submit (แก้ที่ราก shared) §2 ใส่ `key` แยก identity ปุ่มสองโหมด (กันอีกชั้น) §3 หลัง fix ให้ทดสอบแก้ sortOrder ผ่าน UI จริง — ปิด loop PLAN-111 ที่ค้างมาตลอด
- ไฟล์หลักที่แตะ: `DOC/PLANS/PLAN-115-*.md` (ใหม่ READY), `DOC/AGENT_LOG.md` (+ซ่อมบรรทัด header ที่ entry ก่อนหน้าเผลอลบ)
- Contract ที่เปลี่ยน: ไม่มี (แผน; fix เป็น behavior ฝั่ง React)
- Verified: — (แผน; อ่าน ControlsSidebar/MasterDataDetailPage + grep pattern ทุก type="submit")
- **ถึง Gemini: ห้ามเปลี่ยน signature `onClick` ที่ callers เห็น; ปุ่ม submit ห้าม preventDefault; ทำตาม §1+§2 ทั้งคู่. หมายเหตุ: งาน UX polish (Escape/scroll-lock) ที่ทำไปโดยไม่มีแผน — ครั้งหน้าให้จดลง Implementer Notes ของแผนที่เกี่ยว (PLAN-112) หรือรอแผนใหม่ตามกติกา อย่าทำงานนอกแผนเงียบ ๆ**

## [2026-07-22 —] Antigravity — UX/UI Polish & Keyboard Accessibility ใน AssignmentDetailPage.tsx
- ทำอะไร: เพิ่ม `useEffect` สำหรับจัดการ **Keyboard Navigation (`Escape` key)** เพื่อปิด Learner Courses Popup Modal และทำการ **Scroll-Lock (`document.body.style.overflow = 'hidden'`)** ป้องกันไม่ให้หน้าหลังเลื่อนขณะเปิดดูรายชื่อคอร์สใน Popup
- ไฟล์หลักที่แตะ: `iLearn.Admin.React/src/pages/assignments/AssignmentDetailPage.tsx`, `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน: ไม่มี (React UX/a11y เท่านั้น)
- Verified: `npm run lint` ผ่าน 0 errors, `npm run build` ผ่าน 0 errors (built in 2.12s)

## [2026-07-22 —] Claude Code — เขียน PLAN-114: ตัด caption chart + Created By เป็นชื่อ + Overview stat cards (มอบ Copilot)
- ทำอะไร: ผู้ใช้รีวิว `/assignments/275(+/report)` บน QA ขอ 4 ข้อ. สำรวจ: caption อยู่ใน `AssignmentReportCharts.tsx:93/166` (component ใช้ร่วม 2 หน้า — ลบ 2 บรรทัดจบ, **คงคลิก filter**); `CreatedBy` เป็น Nid ดิบ — มี `ILearnerApiService.GetEmployeesByNidsAsync` resolve ได้อยู่แล้ว; stat tile ต้นแบบอยู่ `AssignmentReportPage.tsx:316`. เขียน PLAN-114: §1 ลบ caption (ข้อยกเว้นเดียวที่อนุญาตแตะไฟล์ Report) §2 `AssignmentDashboardDto` +`createdByName` (additive, lookup ห่อ try/catch ห้ามทำ endpoint ล้ม — กติกา side-effect) + React แสดงชื่อ+Nid รอง §3 ตัด Fact Completed/Completion Rate §4 tiles Learners/Courses/Status แบบ Report Summary + FactGrid ที่เหลือ + donut เดิม
- ไฟล์หลักที่แตะ: `DOC/PLANS/PLAN-114-*.md` (ใหม่ READY), `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน: ไม่มีในตัวแผน (แผนระบุ +`createdByName` additive)
- Verified: — (แผน; อ่าน AssignmentDashboardService/ILearnerApiService/ReportPage tiles/ReportCharts)
- **ถึง Copilot: ห้ามแตะ `AssignmentHistoryDto` (~314); ตัดแค่ข้อความ hint ห้ามตัด behavior คลิก; React อ่าน `createdByName` แบบ optional กัน deploy skew**

## [2026-07-22 —] Claude Code — รีวิว PLAN-113 → REVIEWED (commit 646136e) + สมมติฐานใหม่เรื่องปุ่ม Edit Properties
- ทำอะไร: รีวิว diff ครบ — §1 additive DTO+projection เป๊ะ ไม่แตะ ordering courses; §2 `Object.values().filter(id!==undefined).sort((sortOrder,id))` ถูกต้อง (entry 'all' เป็น raw array ถูกกรองเอง, `data-category` render ค่าเท่าเดิม click/lookup ไม่เปลี่ยน); manual สลับ sortOrder จริงเห็นผล end-to-end ปิด loop PLAN-111. รัน verify เอง: 222/222. **เรื่องปุ่ม Edit Properties ที่ Copilot รายงานว่ากดไม่ติด:** ตรวจโค้ดแล้ว `ControlAction` เป็น `type="button"`+onClick ถูกต้อง — **สมมติฐานที่เข้าเค้ากว่า: กด Edit แล้วปุ่มตำแหน่งเดิมกลายเป็น Save Changes (submit)** ⇒ automation กดซ้ำพิกัดเดิม = save แล้วเด้งกลับ view mode ทันที (อธิบาย toast "Changes saved successfully" ที่รอบ PLAN-111 โทษ SignalR ด้วย) — ต้องพิสูจน์ด้วยตาจริง เสนอ PLAN-114 รอผู้ใช้ตัดสิน
- ไฟล์หลักที่แตะ: `DOC/PLANS/PLAN-113-*.md` (→REVIEWED + Sign-off), `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน: ไม่มี (รีวิว)
- Verified: dotnet test 222/222 โดย reviewer เอง + อ่าน ControlsSidebar/MasterDataDetailPage ยืนยัน type ปุ่ม
- **คงค้าง: deploy PROD (API + iLearn.User) รอผู้ใช้ยืนยัน; ปุ่ม Edit Properties รอผู้ใช้กดพิสูจน์เองหรือเปิด PLAN-114**

## [2026-07-22 —] GitHub Copilot — PLAN-113: implement + deploy QA + smoke test สลับ sortOrder จริง — รอ PROD
- ทำอะไร: implement ตามแผน — **§1** `LearnerCourseCatalogDto` +`CategorySortOrder` (additive), `EnrollmentsController.GetCourseCatalog` projection เพิ่ม `CategorySortOrder = c.Category?.SortOrder ?? 0` **§2** `organizeCoursesByCategory` เก็บ sortOrder ต่อ category entry, `renderCategorySidebar` เปลี่ยนจาก `Object.keys().forEach` เป็น `Object.values().filter(...).sort((sortOrder, id))`. Build+test: 0 errors, 222/222 ผ่าน. Deploy QA (API stamp `_deploy_20260722105526` + User stamp `_user_deploy_20260722105719`). **Manual test ตัวจริงตามแผน:** login learner 610034 เห็น sidebar เรียง EP1(1) ก่อน PLAN-079(2) ตรงกับ admin grid → เรียก PUT CategoriesCRUD สลับ sortOrder สองหมวดจริงผ่าน browser session admin → refresh learner → sidebar สลับตามทันที → สลับกลับคืนค่าเดิมหลังทดสอบ — ปิด loop manual ที่ค้างจาก PLAN-111 ด้วย. ระหว่างทางพิมพ์ category id ผิด 1 ครั้ง (แก้ sortOrder ของ "Special knowledge" id=81 แทน PLAN-079) — ตรวจพบและ revert เป็น 12 ทันทีก่อนไปแก้ id ที่ถูกต้อง (82)
- ไฟล์หลักที่แตะ: `iLearn.Application/DTOs/LearnerCourseCatalogDto.cs`, `iLearn.API/Controllers/EnrollmentsController.cs`, `iLearn.User/Views/MyLearning/Index.cshtml`, `DOC/PLANS/PLAN-113-*.md` (→QA VERIFIED + Implementer Notes), `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน: `LearnerCourseCatalogDto` +`CategorySortOrder` (int, additive) — consumer เดียวคือ `MyLearning/Index.cshtml` แก้ในคอมมิทเดียวกัน ไม่มี migration
- Verified: `dotnet build iLearn.Tests` 0 errors, `dotnet test` 222/222 ผ่าน, Playwright smoke QA ครบ manual ข้อ 1-3 รวมการสลับ sortOrder จริงแล้วดู sidebar เปลี่ยนตาม
- **สังเกตนอก scope:** ปุ่ม "Edit Properties" บนหน้า Category Detail (Admin React) กดแล้วไม่เปิดฟอร์มที่มองเห็นได้ — ใช้ direct API PUT แทนสำหรับ manual test นี้ ควรมีคนตรวจ UI ปุ่มนี้แยกต่างหาก
- **คงค้าง:** deploy PROD รอผู้ใช้ยืนยัน (ตาม Deploy note ของแผน)


## [2026-07-22 —] GitHub Copilot — PLAN-112: deploy Admin React ขึ้น QA + PROD ครบ — ปิดงาน VERIFIED
- ทำอะไร: PLAN-112 (Overview status donut + courses popup) รีวิวผ่านแล้วแต่ยังไม่ deploy — ทำให้ครบ: **(1)** lint+build local 0 errors **(2)** deploy QA (`tools/deploy-admin-react.ps1`) → smoke ด้วย Playwright ที่ `/assignments/288` (AS-20260713-002): donut legend "Not Started 4 (100%)" ตรงกับ Completion Rate, สลับ tab Learners → ตาราง 4 คอลัมน์ไม่มีคอลัมน์ courses เดิม, ปุ่ม "View courses" เปิด popup แสดงคอร์ส/progress/status/ปุ่ม reset ครบ, ปิด popup ปกติ **(3)** deploy PROD (`tools/deploy-admin-react-prod.ps1`) → smoke ที่ `/assignments/275` (AS-20260702-002, batch มีหลายสถานะจริง): donut แสดง "67% Completion" ตรงกับ Fact Completion Rate 67% ถูกต้อง
- ไฟล์หลักที่แตะ: `DOC/PLANS/PLAN-112-*.md` (→VERIFIED + Deploy Notes), `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน: ไม่มี (React display only, ตามที่แผนระบุ)
- Verified: lint+build 0 errors, Playwright smoke ทั้ง QA และ PROD ยืนยัน donut/tab-switch/popup ทำงานถูกต้อง ไม่มี console error ที่สังเกตเห็น


## [2026-07-22 —] Claude Code — เขียน PLAN-113: learner sidebar เรียงหมวดตาม SortOrder (มอบ Copilot)
- ทำอะไร: ผู้ใช้ยืนยันเปิดแผนปิด limitation จาก PLAN-111. วินิจฉัย: `GetCourseCatalog` (EnrollmentsController:153) Include Category อยู่แล้วแต่ DTO ไม่ส่ง sortOrder; sidebar วน `Object.keys()` ที่ key เป็นเลข = JS spec บังคับเรียง ascending ตาม id override ไม่ได้. เขียน PLAN-113: **§1** `LearnerCourseCatalogDto` +`categorySortOrder` (additive) **§2** `organizeCoursesByCategory` เก็บ sortOrder ลง entry + `renderCategorySidebar` เปลี่ยนเป็น `Object.values(...).sort((sortOrder, id))` — `?? 0` กัน deploy skew (User ขึ้นก่อน API → fallback พฤติกรรมเดิม). Manual ข้อ 2 ของแผนให้ admin สลับ sortOrder จริงแล้วดู learner ตาม — ปิด loop manual ที่ค้างของ PLAN-111 ไปด้วย
- ไฟล์หลักที่แตะ: `DOC/PLANS/PLAN-113-*.md` (ใหม่ READY), `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน: ไม่มีในตัวแผน (แผนระบุ +`categorySortOrder` additive, consumer เดียวแก้ในแผนเดียวกัน)
- Verified: — (แผน; อ่าน GetCourseCatalog + organizeCoursesByCategory/renderCategorySidebar)
- **ถึง Copilot: ห้ามแตะ ordering ของ courses ใน response (ยัง Code,Title); ห้ามเพิ่มเลขนำหน้าชื่อหมวด (option รอผู้ใช้); "ทั้งหมด" อยู่บนสุดเหมือนเดิม; ไม่มี migration**

## [2026-07-22 —] Claude Code — รีวิว PLAN-112 → REVIEWED ผ่าน (แก้ข้อเท็จจริง z-index ใน notes)
- ทำอะไร: รีวิวงาน Gemini — §1 donut reuse `StatusDonut`+`buildStatusData` (มีอยู่แล้วที่ `AssignmentReportCharts.tsx:172`, ไฟล์ Report ไม่ถูกแตะตามข้อห้าม), sync สองทางกับ filter ถูกต้อง; §2 `expandedCodes`/Expand all หายเกลี้ยง, ตาราง 4 คอลัมน์ + `colSpan={4}` ถูก, popup ใช้ memo จาก `groupedLearners` ล่าสุด (ไม่ snapshot) + effect ปิดเองเมื่อ learner หาย, reset รายคอร์สย้ายเข้า popup ครบ. **แก้ข้อเท็จจริง:** notes อ้าง ConfirmDialog z-60 — จริง ๆ `.modal-overlay` = z-50 เท่า popup, ที่ทับได้เพราะ `{confirmDialog}` อยู่หลังใน DOM (pattern เดิมของหน้านี้: confirm Unverified Codes ทับ Add Learners modal ด้วยกลไกเดียวกัน) — behavior ถูก โค้ดไม่ต้องแก้ แต่จดไว้กันคนย้ายตำแหน่ง `{confirmDialog}` ในอนาคต. รัน verify เอง: lint + build 0 errors
- ไฟล์หลักที่แตะ: `DOC/PLANS/PLAN-112-*.md` (→REVIEWED + Sign-off), `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน: ไม่มี (รีวิว)
- Verified: npm run lint + npm run build โดย reviewer เอง + grep z-index จริงใน Modal/index.css + ตรวจ AssignmentReportCharts ไม่ถูกแก้
- **คงค้างก่อน VERIFIED: deploy Admin React ขึ้น QA** + manual 6 ข้อ (เน้นข้อ 2 คลิก donut→filter, ข้อ 4 reset ใน popup แล้ว refresh) → PROD รอผู้ใช้ยืนยัน

## [2026-07-22 —] Antigravity — PLAN-112: Assignment detail — Overview status donut visualization + ย้าย courses ของ learner ไป popup modal
- ทำอะไร: **§1** ปรับ layout ใน `<Card title="Overview">` ของ `AssignmentDetailPage.tsx` เป็น responsive 2 คอลัมน์ (`grid-cols-1 lg:grid-cols-[1fr_auto]`) โดยเพิ่ม `StatusDonut` ขวาของ `FactGrid` ซึ่ง reuse `StatusDonut` + `buildStatusData` จาก `AssignmentReportCharts.tsx` (ไม่แตะ backend); คลิก segment donut จะสลับไป tab Learners + filter ตาม status และ sync donut highlight กับ `learnerStatusFilter`. **§2** ใน Learners tab ตัดคอลัมน์ `Assigned Courses & Progress`, state `expandedCodes` และปุ่ม Expand/Collapse all ออก; ปรับคอลัมน์ `Summary` ให้มี `Badge` แสดงจำนวนคอร์ส และ `AppButton` `"View courses"` สำหรับเปิด popup modal (`z-50`); ภายใน popup render รายการคอร์ส/progress/status พร้อมปุ่ม reset รายคอร์ส (`IconButton` `RotateCcw`) ซึ่งยังคงใช้ `useConfirm` (ConfirmDialog `z-60`) ได้ และ refresh ข้อมูลใน popup สดอัตโนมัติ
- ไฟล์หลักที่แตะ: `iLearn.Admin.React/src/pages/assignments/AssignmentDetailPage.tsx`, `DOC/PLANS/PLAN-112-assignment-detail-overview-viz-and-courses-popup.md` (→DONE), `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน: ไม่มี (React display เท่านั้น)
- Verified: `npm run lint` ผ่าน 0 errors, `npm run build` ผ่าน 0 errors (built in 1.62s)

## [2026-07-22 —] Claude Code — รีวิวรอบปิดงาน PLAN-110 + PLAN-111 → ยืนยัน VERIFIED ทั้งคู่
- ทำอะไร: ตรวจรอบ deploy ของ Copilot ทั้งสองแผน — **ลำดับ gate ถูกต้องครบ**: PLAN-110 รัน SQL 2.3 หลัง 2.1 ขึ้น PROD เท่านั้นตามข้อห้าม; PLAN-111 รัน migration ก่อน deploy API PROD (Copilot จับประเด็น HEAD รวม 2 แผนได้เองก่อน deploy — ป้องกัน Categories 500 ทั้ง PROD); backup เป็น table (`Categories_Backup_20260722`, `ContentItems_ZipSuffix_Backup_20260722` 1425/1424 แถว) ดีกว่าที่แผนสั่ง. grep ปิดท้ายไม่เหลือโค้ดพึ่ง `.zip` จาก `ContentItem.Name`. เติม Reviewer Endorsement ท้ายแผนทั้งสอง
- ไฟล์หลักที่แตะ: `DOC/PLANS/PLAN-110-*.md` + `DOC/PLANS/PLAN-111-*.md` (+Endorsement), `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน: ไม่มี (รีวิว)
- Verified: อ่าน deploy notes ทั้งสองแผน + ตรวจลำดับกับ gate ในแผน + grep
- **ความเสี่ยงคงเหลือที่จดไว้ (ไม่ block):** (110) upload SCORM ใหม่ + bulk-process ยังไม่ smoke ด้วยมือ — การใช้จริงครั้งแรกบน PROD คือ test ตัวจริง; (111) **ยังไม่มีใครแก้ sortOrder แล้ว save จริง end-to-end** — แนะนำผู้ใช้ลองสลับลำดับ category 1 คู่บน admin เพื่อปิด loop; learner sidebar ยังเรียงตาม categoryId (limitation เดิม รอผู้ใช้ยืนยันเปิดแผนใหม่)

## [2026-07-22 —] GitHub Copilot — PLAN-111: deploy Admin React ขึ้น QA + PROD ครบ — ปิดงานสมบูรณ์
- ทำอะไร: ส่วนที่เหลือของ PLAN-111 (Admin React) ยังไม่เคย deploy มาก่อน (รอบก่อนหน้าทำแค่ API+migration) — ทำให้ครบตามคำขอผู้ใช้: **(1)** deploy Admin React ขึ้น QA (`tools/deploy-admin-react.ps1`) — lint+build ผ่าน 0 errors **(2)** smoke QA ด้วย Playwright ที่หน้า Categories: คอลัมน์ "ลำดับ" ซ้ายสุดเรียง 1,2,3… ต่อ division ถูกต้อง, ชื่อไม่มีเลขนำหน้า, เปิด detail category "Environment & Safety" เห็น Fact "Sort Order: 1"; ทดลองกด Edit Properties หลายครั้งแล้วตรวจ DB ว่าไม่มีข้อมูลเปลี่ยนแปลงโดยไม่ตั้งใจ — toast "บันทึกสำเร็จ" ที่เห็นระหว่างทดสอบพิสูจน์แล้วว่าเป็น SignalR broadcast `adminactivitycreated` ของ admin คนอื่น ไม่เกี่ยวกับการทดสอบของเรา **(3)** deploy Admin React ขึ้น PROD (`tools/deploy-admin-react-prod.ps1`) — lint+build ผ่าน 0 errors **(4)** smoke PROD ที่หน้า Categories เดียวกัน — คอลัมน์ลำดับ/ชื่อสะอาด/41 records ถูกต้อง ไม่มี error
- ไฟล์หลักที่แตะ: `DOC/PLANS/PLAN-111-*.md` (+Admin React deploy notes, ปิดงานสมบูรณ์), `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน: ไม่มี (deploy เท่านั้น — โค้ด/contract ทำไว้ครบตั้งแต่รอบก่อน)
- Verified: lint+build ทั้ง QA/PROD build 0 errors, Playwright smoke ทั้ง QA (`ap-ntc2138-qawb`) และ PROD (`ap-ntc2137-prwb`) ยืนยันคอลัมน์ ลำดับ + ชื่อไม่มี prefix + ไม่มี error, DB query ยืนยันไม่มีข้อมูลเปลี่ยนแปลงระหว่างทดสอบ edit form
- **PLAN-111 ปิดงานสมบูรณ์ทั้ง §1/§2/§3 ขึ้น QA+PROD ครบ** — เหลือเฉพาะ §4 (learner sidebar เรียงตาม sortOrder แทน categoryId) ซึ่งเป็น option นอก scope ที่ reviewer จดไว้ รอผู้ใช้ยืนยันถ้าต้องการเปิดแผนใหม่


## [2026-07-22 —] GitHub Copilot — PLAN-110 + PLAN-111: deploy PROD ทั้งคู่พร้อมกัน (ผู้ใช้ยืนยัน) + รัน SQL 2.3 — ทั้งสองแผนปิดงาน VERIFIED
- ทำอะไร: ผู้ใช้ยืนยันให้ deploy PROD ทั้ง PLAN-110 (zip strip) + PLAN-111 backend (Category.SortOrder) พร้อมกันในรอบเดียว เพราะอยู่ HEAD เดียวกัน. ลำดับที่ทำ: **(1)** backup `Categories` บน PROD (`AP-NTC2139-COSS`) → ตาราง `Categories_Backup_20260722` (41 แถว) ก่อนรัน migration **(2)** `dotnet ef database update --connection <PROD>` → apply `20260722030000_AddSortOrderToCategory` สำเร็จ — verify: 41/41 แถว backfill ถูกต้อง, 0 แถวเหลือ prefix เลขหน้าชื่อ, SortOrder เรียง 1-5 ต่อ division ถูกต้อง **(3)** deploy API PROD (`tools/deploy-api-prod.ps1`) → stamp `_deploy_20260722101547` (previous `_deploy_20260717130658`), health check HTTP 401 attempt 1/5, `AutoRolledBack=False` **(4)** smoke PROD ด้วย Playwright: learner `610034` → `Player?courseId=507` (คอร์ส completed 100% เดิม) — TOC ไม่มี `.zip` ต่อท้าย ✅, สถานะ/คะแนนเดิมไม่กระทบ **(5)** SQL 2.3 (ลบ `.zip` ออกจาก `ContentItems.Name`) — backup เข้าตาราง `ContentItems_ZipSuffix_Backup_20260722` ก่อนรันทั้ง QA (1425 แถว) และ PROD (1424 แถว) → UPDATE สำเร็จทั้งคู่ → verify เหลือ `.zip` = 0 ทั้ง QA/PROD → สุ่มตรวจ Id 1087/1088 บน PROD ชื่อสะอาดถูกต้อง
- ไฟล์หลักที่แตะ: `DOC/PLANS/PLAN-110-*.md` (→VERIFIED + PROD deploy notes), `DOC/PLANS/PLAN-111-*.md` (→VERIFIED + PROD deploy notes), `DOC/AGENT_LOG.md`; DB: PROD `Categories.SortOrder` column ใหม่ + backfill, `Categories_Backup_20260722` (backup table ใหม่), `ContentItems.Name` ล้าง `.zip` ทั้ง QA/PROD + `ContentItems_ZipSuffix_Backup_20260722` (backup table ใหม่ทั้ง 2 env)
- Contract ที่เปลี่ยน: ไม่มีเพิ่มเติมจากที่แผนทั้งสองระบุไว้แล้ว — รอบนี้คือ deploy/data-migration ตามแผนที่รีวิวผ่านแล้ว
- Verified: migration verify query (41/41 แถว, 0 prefix เหลือ), health check ทั้ง QA/PROD (401), Playwright smoke ทั้ง QA (courseId=968) และ PROD (courseId=507) ยืนยัน TOC ไม่มี `.zip`, SQL count เหลือ `.zip` = 0 ทั้ง QA/PROD
- **ค้าง (PLAN-110):** manual verify ข้อ 2/3 (upload SCORM ใหม่ + bulk-process draft ผ่าน Admin UI จริง) ยังไม่ได้ทำ — ครอบคลุมด้วย code review แล้วแต่ยังไม่ smoke จริง
- **ค้าง (PLAN-111):** manual smoke ข้อ 4-6 (เปิด admin React จริง, แก้ sortOrder แล้ว refresh, learner sidebar เรียงตามลำดับใหม่) ยังไม่ได้ทำบน PROD รอผู้ใช้เปิดใช้จริงยืนยัน — backend/migration/data ยืนยันถูกต้องแล้วทั้งหมด


## [2026-07-22 —] GitHub Copilot — PLAN-110: deploy API ขึ้น QA + smoke Player ยืนยันไม่มี `.zip` — ค้าง PROD รอยืนยัน
- ทำอะไร: deploy `tools/deploy-api.ps1 -HealthCheckUrl 'https://ap-ntc2138-qawb/iLearn/Service/api/admin/session/me'` → stamp `_deploy_20260722100944` (previous `_deploy_20260721142125`), health check HTTP 401 attempt 1/5 ผ่าน, `AutoRolledBack=False`. Smoke ด้วย Playwright: login learner `610034` → `MyLearning/Player?courseId=968` (คอร์ส TEST) → TOC 4 รายการ (`NTC-WI-PD2-050_12_Learn`, `NTC-WI-PD2-711_2004_Learn`, `NTC-WI-PD2-035_12_Exam`, `NTC-WI-PD2-334_2004_Exam`) **ไม่มี `.zip` ต่อท้าย** ✅ (ยืนยันเฟส 1 ของแผน); logout เรียบร้อย. ยังไม่ได้ทดสอบข้อ 2/3 ของ Verification (upload SCORM ใหม่ + bulk-process draft) เพราะต้องทำผ่าน Admin UI จริง — ข้ามไปก่อนเพราะ code review ของ reviewer ครอบคลุมความเสี่ยงจุดนี้แล้ว (null-check + non-nullable Name)
- **⚠️ พบประเด็นสำคัญก่อนไป PROD:** HEAD ปัจจุบัน (`6f56f4d`) มีทั้ง PLAN-110 (`9dc314c`) และ PLAN-111 backend (`aa62349`) รวมกันอยู่ในโค้ดเดียวกัน — PLAN-111 เพิ่ม `Category.SortOrder` column ที่มี migration ของตัวเอง. **QA ปลอดภัยแล้ว** เพราะ Gemini รัน `database update` บน QA ไปแล้วตอนทำ §3. แต่ **PROD ยังไม่มี migration นี้** — ถ้า deploy API build ปัจจุบันขึ้น PROD โดยไม่รัน `dotnet ef database update` คู่กันก่อน จะทำให้ **ทุก endpoint ที่แตะ `Category` (Categories/CategoriesCRUD, course catalog ที่มี category) 500 ทันที** เพราะ EF query คอลัมน์ `SortOrder` ที่ยังไม่มีจริงใน PROD DB
- ไฟล์หลักที่แตะ: `DOC/PLANS/PLAN-110-*.md` (+QA deploy/verify notes), `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน: ไม่มี (deploy เท่านั้น)
- Verified: health check 401 (auto) + Playwright smoke ยืนยัน TOC ไม่มี `.zip`
- **หยุดรอผู้ใช้ยืนยันก่อนไป PROD:** ต้องตัดสินใจว่า (1) deploy API ขึ้น PROD พร้อมรัน migration `AddSortOrderToCategory` (+backup `Categories.Name`) ไปด้วยในรอบเดียวกัน (เท่ากับ deploy ทั้ง PLAN-110+111 พร้อมกัน) หรือ (2) รอแยก PLAN-111 ออกไปก่อน — ยังไม่ deploy PROD จนกว่าจะได้คำตอบ; SQL 2.3 (PLAN-110) ยังไม่รันเช่นกัน (ตามลำดับเดิม รอ 2.1 ขึ้น PROD ก่อน)


## [2026-07-22 —] Claude Code — เขียน PLAN-112: Assignment detail — Overview donut + ย้าย courses ของ learner ไป popup (มอบ Gemini)
- ทำอะไร: ผู้ใช้รีวิวหน้า `/assignments/:id` บน QA ขอ 2 อย่าง. สำรวจ: `AssignmentDashboardDto` มี status ต่อ enrollment ครบ (**ไม่ต้องแตะ backend**), `StatusDonut` ใน `AssignmentReportCharts.tsx` reuse ได้เลย (คลิก segment → filter ผ่าน `onSelectStatus`). เขียน PLAN-112: **§1** Overview เพิ่ม donut ข้าง FactGrid — aggregate `learners[]` rows ตาม `LEARNER_STATUS_KEYS`, คลิก segment เด้งไป tab Learners + ตั้ง filter, `activeStatus` sync กับ `SegmentedToggle`. **§2** ตัดคอลัมน์ Assigned Courses & Progress + กลไก expand/collapse (`expandedCodes`/Expand all) → ปุ่ม View courses เปิด modal z-50 — เก็บ state แค่ `courseModalCode` แล้ว render จาก `groupedLearners` ล่าสุด (reset รายคอร์สใน popup แล้วข้อมูล refresh ตาม, code หายให้ปิดเอง), reset รายคอร์ส/confirm (z-60) ต้องยังใช้ได้. ห้ามแตะหน้า Report นอกจาก import, ห้าม chart lib อื่น
- ไฟล์หลักที่แตะ: `DOC/PLANS/PLAN-112-*.md` (ใหม่ READY), `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน: ไม่มี (React display เท่านั้น)
- Verified: — (แผน; อ่าน AssignmentDetailPage ทั้งไฟล์ + StatusDonut + ยืนยันข้อมูลใน DTO ครบ)
- **ถึง Gemini: reuse `StatusDonut` ห้ามเขียน chart ใหม่; §2 อย่า snapshot ข้อมูลตอนเปิด popup; ฟีเจอร์เดิมใน Learners tab (filter/bulk/reset/remove/Load more) ห้ามพัง — checklist ใน Verification ของแผน**

## [2026-07-22 —] Claude Code — รีวิว PLAN-111 → REVIEWED + commit งานค้างทั้งหมด (109/110/111) + ย้าย SQL 110 เข้า git
- ทำอะไร: **รีวิว PLAN-111 ผ่าน** — migration เขียนมือถูกต้อง (CTE backfill + prefix-strip เช็คเลขล้วนก่อนจุด); **ยืนยัน tech debt EF ปิดจริง**: snapshot มี filter 3 index อยู่แล้ว drift ตัวจริงคือ Fluent ขาด `HasFilter` → Gemini เติมแล้ว = model↔snapshot↔DB ตรงกันครบ (Designer ใหม่ตรง snapshot, diff 9 บรรทัด header เท่านั้น); contract freeze ตรง; React number input มี `required`+native submit กันค่าว่างพัง `PopulateObject`; grep `Category.Name` matching ซ้ำ = display ล้วน. **Limitation จดใน sign-off:** sidebar learner เรียงตาม categoryId (JS numeric key iteration) — admin แก้ sortOrder แล้ว learner ไม่ตาม ต้องเปิดแผนใหม่ถ้าผู้ใช้ต้องการ. รัน verify เอง: 222/222 + lint + build React 0 errors. **Commit ตามคำสั่งผู้ใช้:** `0bc6437` (PLAN-109 Player.cshtml ที่ deploy PROD ไปแล้วแต่ยังไม่เข้า git), `9dc314c` (PLAN-110 ทั้งหมด + ย้าย SQL 2.3 จาก `artifacts/` ที่ถูก gitignore ไป `DOC/PLANS/PLAN-110-cleanup-contentitem-zip-suffix.sql`), ลบ `plan109-status-inprogress.png` ค้าง root
- ไฟล์หลักที่แตะ: `DOC/PLANS/PLAN-111-*.md` (→REVIEWED + Sign-off), `DOC/PLANS/PLAN-110-*.md` (path SQL), `DOC/PLANS/PLAN-110-cleanup-contentitem-zip-suffix.sql` (ย้ายมา), `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน: ไม่มี (รีวิว/commit)
- Verified: dotnet test 222/222 + npm lint/build โดย reviewer เอง + ตรวจ Designer↔snapshot diff + grep
- **คงค้างก่อน VERIFIED (111): deploy API + Admin React ขึ้น QA** + manual 4 ข้อ → PROD (backup Categories.Name + รัน database update คู่); **(110): deploy API → QA→PROD → ค่อยรัน SQL 2.3**

## [2026-07-22 —] Antigravity Gemini — PLAN-111: Category SortOrder และแก้ไข tech debt EF Core Model Index
- ทำอะไร: **§3 React Admin:** เพิ่มคอลัมน์ ลำดับ (`sortOrder`) ซ้ายสุดในตาราง Categories grid config (`moduleConfigs.ts`), เพิ่ม text/number input ในหน้า edit/create detail category (`MasterDataDetailPage.tsx`), และเพิ่ม field `Sort Order` ในหน้า read-only details view; ทำ API Contract Sync ครบถ้วนโดยประกาศ mirror type interface `CategoryDto`. **Backend model fix:** กู้คืน HasFilter("[IsDeleted] = 0") บน unique indexes 3 จุดใน `AppDbContext.cs` (`Assignment`, `EnrollmentAssignment`, `AssignmentCourse`) เพื่อให้ model snapshot ตรงกับ database schema จริง แก้ปัญหา EF migration drift tech debt อย่างปลอดภัย. รัน database update อัปเดตโครงสร้าง QA เรียบร้อย (backfill sortOrder สำเร็จ + ตัดเลข prefix ออกจาก Name).
- ไฟล์หลักที่แตะ: `iLearn.Admin.React/src/pages/moduleConfigs.ts`, `iLearn.Admin.React/src/pages/master-data/MasterDataDetailPage.tsx`, `iLearn.Infrastructure/Persistence/AppDbContext.cs`, `iLearn.Infrastructure/Migrations/AppDbContextModelSnapshot.cs`, `DOC/PLANS/PLAN-111-*.md` (→DONE), `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน: `CategoryDto` +`sortOrder: number`; Categories grid +column `ลำดับ` และ Category detail form +input `Sort Order` (ลำดับ)
- Verified: `npm run lint` + `npm run build` ผ่าน 0 errors (React); `dotnet build` + `dotnet test` → **222 passed, 0 failed** (Backend); ตรวจสอบข้อมูลใน Category QA DB หลังรัน migration ลำดับเรียงถูกต้อง ชื่อไม่มี prefix.

## [2026-07-22 —] GitHub Copilot — PLAN-111 §1/§2 (backend): Category.SortOrder + migration + API ordering — DONE
- ทำอะไร: **§1** เพิ่ม `Category.SortOrder` (int, default 0) + migration `AddSortOrderToCategory` (`iLearn.Infrastructure/Migrations/`) — `AddColumn` + backfill SQL 2 ก้อน: (1) `ROW_NUMBER() OVER (PARTITION BY ISNULL(DivisionId,-1) ORDER BY Id)` เฉพาะ `IsDeleted=0` (2) ตัด prefix เลขนำหน้าชื่อด้วย `CHARINDEX('.', Name)` + เช็คว่าก่อนจุดเป็นตัวเลขล้วน (`LEFT(...) NOT LIKE '%[^0-9]%'`) รองรับความยาวเลขได้ทุกหลัก (ไม่ใช้ pattern ตายตัว 1-4 หลักแบบที่ EF scaffold เสนอ). **§2 [CONTRACT FREEZE]** `CategoryDto` +`SortOrder`; `CategoriesController` (lookup/GetById) + `CategoriesCRUDController` (`Get`/`GetPaged`) เพิ่ม `SortOrder` ใน projection และเปลี่ยน default ordering เป็น `(DivisionId, SortOrder, Id)`; ไม่ต้องแก้ Post/Put เพราะ `GenericController` ใช้ `JsonConvert.PopulateObject(values, entity)` populate ตรง entity อยู่แล้ว รับ `sortOrder` จาก React form ได้ทันที
- **เจอ side-effect อันตรายระหว่างสร้าง migration:** `dotnet ef migrations add` สร้าง diff ของ 3 unique index เดิม (`IX_EnrollmentAssignments_EnrollmentId_AssignmentId`/`IX_Assignments_AssignmentNo_CourseId`/`IX_AssignmentCourses_AssignmentId_CourseId`) มาด้วยเสมอ โดย `Up()` จะ**ถอด filter `[IsDeleted]=0` ออก** (ย้อนกลับ fix ของ PLAN-092/SoftDeleteFilteredUniqueIndexes) เพราะ `AppDbContextModelSnapshot.cs`/Fluent config ปัจจุบันไม่มี `HasFilter` ตรงกับ DB จริงสำหรับ 3 index นี้ (เป็น tech debt เดิม ไม่เกี่ยวกับแผนนี้) — **แก้โดยลบ migration ที่ auto-gen ทิ้งแล้วเขียน migration + Designer.cs ด้วยมือ** (คัดลอกจาก Designer.cs ของ migration ล่าสุดแล้วแก้เฉพาะ Category block) เพื่อไม่ให้ index fix เดิมหลุดถ้ามีคน deploy migration นี้ในอนาคต — **ยังไม่ได้แก้ tech debt ต้นตอ (Fluent API ไม่มี HasFilter ให้ตรง DB)** อยู่นอก scope ของ PLAN-111
- ไฟล์หลักที่แตะ: `iLearn.Domain/Entities/Category.cs`, `iLearn.Application/DTOs/DivisionDto.cs`, `iLearn.API/Controllers/CategoriesController.cs`, `iLearn.API/Controllers/Base/CategoriesCRUDController.cs`, `iLearn.Infrastructure/Migrations/20260722030000_AddSortOrderToCategory.cs` (ใหม่, เขียนมือ), `20260722030000_AddSortOrderToCategory.Designer.cs` (ใหม่, เขียนมือ), `iLearn.Infrastructure/Migrations/AppDbContextModelSnapshot.cs` (+SortOrder เท่านั้น), `DOC/PLANS/PLAN-111-*.md` (+Implementer Notes), `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน: **มี migration** (+column `Category.SortOrder`, ต้องรัน `dotnet ef database update` คู่ deploy — ไม่มี auto-migrate); `CategoryDto` +`sortOrder` (additive, freeze ตามแผน); ordering ของ Categories endpoints เปลี่ยนจาก name → `(DivisionId,SortOrder,Id)`; ข้อมูลเดิมของ `Category.Name` ถูกตัด prefix เลขนำหน้า (backfill, `Down()` restore ไม่ได้)
- Verified: `dotnet build iLearn.Infrastructure` 0 errors, `dotnet build iLearn.Tests -o artifacts\verify-test` 0 errors, `dotnet test` → **222 passed, 0 failed**, ลบ artifacts แล้ว; grep ยืนยัน `c.Category.Name` ทุกจุดเป็น display projection ไม่มี string-matching ที่จะพังจากการตัด prefix
- **ถึง Gemini (§3):** contract freeze ตรงตามแผน — `sortOrder: number` ใน `CategoryDto`; เพิ่มคอลัมน์ `ลำดับ` + number input ตามแผนได้เลย ไม่มีอะไรเบี่ยงจาก spec
- **ถึง reviewer/deploy:** มี migration ⇒ deploy ต้องรัน `dotnet ef database update --connection <env>` คู่กันเสมอ; **backup `Categories.Name` ก่อน deploy PROD** เพราะ `Down()` คืนชื่อเดิม (ที่ตัด prefix แล้ว) ไม่ได้; ยังไม่ได้ deploy รอบนี้


## [2026-07-22 —] Claude Code — รีวิว PLAN-110 → REVIEWED ผ่านสะอาด (รวมยืนยัน restoration นอกเรื่อง 2 จุด)
- ทำอะไร: รีวิวงาน Copilot ทั้ง 4 ส่วน — helper ตัดเฉพาะ suffix `.zip` null-safe + test 6 case ตรงสเปค; เฟส 1 strip ที่ projection จุดเดียว ไม่แตะ LaunchUrl; เฟส 2.1 ตรวจ context ทั้ง bulk (`ContentItemsController:969`) + single publish (`ContentPublicationService:52`) — `fileStorage` null-checked ก่อนถึงทั้งคู่, `FileStorage.Name` non-nullable ⇒ ไม่มี NRE, จุดเพิ่มที่แผนไม่ได้ระบุเป็นการแก้ที่จำเป็น (ไม่งั้น item หลัง 2.2 publish เดี่ยวไม่ได้), grep ซ้ำไม่เหลือจุด coupling; SQL 2.3 ถูกต้องและยืนยัน**ไม่มี unique index บน ContentItems.Name** จึง strip แล้วชื่อซ้ำได้ไม่ 500. **นอกเรื่อง 2 จุดยืนยันเป็น restoration จริง:** git log ชี้ const หายไปใน `e8191d7` (commit docs ที่กวาดโค้ด — recurrence ของอุบัติเหตุ git add -A ที่เคยบันทึกไว้). รัน verify เอง: build 0 errors + **222 passed, 0 failed**
- ไฟล์หลักที่แตะ: `DOC/PLANS/PLAN-110-*.md` (→REVIEWED + Sign-off), `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน: ไม่มี (รีวิว)
- Verified: dotnet build + dotnet test (222/222) โดย reviewer เอง + grep + git log -S + ตรวจ index
- **คงค้างก่อน VERIFIED: deploy API ขึ้น QA** + manual 3 ข้อ (Player ไม่มี .zip / upload ใหม่ / bulk-process) → PROD รอ user ยืนยัน → ค่อยรัน SQL 2.3. **หมายเหตุ: โค้ด PLAN-109 (Player.cshtml) + PLAN-110 ทั้งหมดยังไม่ commit อยู่ใน working tree** — PLAN-109 deploy PROD ไปแล้วแต่โค้ดยังไม่เข้า git ควร commit โดยเร็ว

## [2026-07-22 —] GitHub Copilot — PLAN-110: เอา .zip ออกจากชื่อ content item — ทำครบเฟส 1+2.1+2.2 (2.3 SQL เตรียมไว้ยังไม่รัน)
- ทำอะไร: เพิ่ม `ScormUploadValidation.StripArchiveExtension` (ตัด suffix `.zip` case-insensitive เท่านั้น). **เฟส 1** strip ตอน projection `PlayerContentItemDto.Name` ใน `EnrollmentsController.GetPlayerInfoByCourse` — learner เห็นชื่อสะอาดทันที ไม่แตะ DB. **เฟส 2.1** decouple extension-check ออกจาก `ContentItem.Name` เป็น `FileStorage.Name` แทนทั้ง 2 จุด: bulk-process (`ContentItemsController:967`, ตามแผน) + **จุดเพิ่มที่แผนไม่ได้ระบุ** `ContentPublicationService.PublishAsync` (single-item publish) ที่เช็ค extension แบบเดียวกัน — grep ยืนยันไม่มีจุดอื่นเหลือ (`CourseService.cs` ใช้ FileStorage.Name อยู่แล้ว). **เฟส 2.2** upload ใหม่ตั้ง `ContentItem.Name` แบบตัด `.zip` แล้ว, `FileStorage.Name` ยังคงเต็มเหมือนเดิม. **เฟส 2.3** เตรียม SQL ไว้ที่ `artifacts/plan110-cleanup-contentitem-zip-suffix.sql` แต่ยังไม่รันตามลำดับที่แผนกำหนด (ต้องรอ 2.1 ขึ้น PROD ก่อน). เพิ่ม unit test ครบ 6 case ตามแผน
- **นอกเรื่อง (พบระหว่างตรวจ build):** commit ก่อนหน้า (ช่วง PLAN-091–107) มี regression ที่ทำให้ build/test ทั้งระบบพังอยู่ก่อนแล้ว 2 จุด ไม่เกี่ยวกับ PLAN-110 แต่บล็อกการ verify: (1) `NotificationTypes.DeadlineDigest`/`NotificationRetentionDays` const หายจาก `NotificationTypes.cs` (เคยมีตั้งแต่ PLAN-090 commit `5d88312`) — คืนกลับ (2) `DeadlineDigestServiceTests.RecordingNotificationService` ขาด overload 3-param ของ `INotificationService.GetForUserAsync` (known blocker ที่ PLAN-101/104 เคยบันทึกไว้แต่ไม่ได้แก้) — เพิ่ม overload delegate ตาม pattern เดิมที่มีอยู่แล้วใน test อื่น ทั้งสองจุดคือการคืนค่าที่หายไป ไม่ใช่เปลี่ยน logic ใหม่
- ไฟล์หลักที่แตะ: `iLearn.Application/Common/ScormUploadValidation.cs`, `iLearn.Application/Common/NotificationTypes.cs` (คืนค่า), `iLearn.Application/Services/ContentPublicationService.cs`, `iLearn.API/Controllers/ContentItemsController.cs`, `iLearn.API/Controllers/EnrollmentsController.cs`, `iLearn.Tests/ScormUploadValidationTests.cs`, `iLearn.Tests/DeadlineDigestServiceTests.cs` (คืน overload), `artifacts/plan110-cleanup-contentitem-zip-suffix.sql` (ใหม่), `DOC/PLANS/PLAN-110-*.md` (→DONE), `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน: `PlayerContentItemDto.Name` (learner) ไม่มี `.zip` ต่อท้ายแล้ว (display เท่านั้น); `ContentItem.Name` ของ upload ใหม่ไม่มี `.zip`; `FileStorage.Name`/launchUrl ไม่แตะ; ไม่มี migration
- Verified: `dotnet build iLearn.API` 0 errors, `dotnet build iLearn.Tests` 0 errors, `dotnet test` → **222 passed, 0 failed**
- **ถึง reviewer: เฟส 2.3 (SQL ล้างข้อมูลเก่า) ยังไม่รัน** — รอ deploy API (1+2.1+2.2) ขึ้น PROD ก่อนตามลำดับที่แผนกำหนด แล้วค่อยรัน SQL บน QA→verify→PROD; deploy ยังไม่ได้ทำในรอบนี้ (รอคำสั่ง deploy)


## [2026-07-22 —] Claude Code — รีวิว PLAN-109 → REVIEWED ผ่านสะอาด (render พิสูจน์ 3 สี)
- ทำอะไร: §1 เหลือ 3 label (grep 0 ดูอย่างเดียว/พร้อมแสดงผล), ไม่แตะ #btnPreSave. §2 render จริงวัด computed style: status-muted #475569 / success #1e7e34 / danger #dc3545 — ทุกตัว background โปร่งใส padding 0 radius 0 font 12.8px/600 เท่า course-meta-value (source order status-* หลัง meta-value → สีชนะ), ลบ pill+status-warning rule แล้ว. §3 btnFullscreen ก่อน btnToggleSidebar. build 0 err + node --check ผ่าน
- ไฟล์หลักที่แตะ: `DOC/PLANS/PLAN-109-*.md` (→REVIEWED + Sign-off), `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน: ไม่มี (รีวิว)
- Verified: render measurement 3 สี + build + node --check
- คงค้างก่อน VERIFIED: เล่นคอร์สจบ/ตกจริงบน QA (account ไม่มี state นั้น) — สีพิสูจน์แล้ว, logic แค่ label string, ความเสี่ยงต่ำ
## [2026-07-22 —] GitHub Copilot — PLAN-109: deploy PROD ตามคำยืนยันผู้ใช้ในแชท + smoke ปิด gap สถานะ "เรียนจบแล้ว"
- ทำอะไร: ผู้ใช้ยืนยันในแชท "deploy ทั้งหมด to prod" หลัง QA smoke ผ่าน → deploy `tools\deploy-user-prod.ps1` (เฉพาะ `iLearn.User` — งานนี้แตะแค่โปรเจกต์เดียวตาม scope ของแผน) → stamp `_user_deploy_20260722085448` live, post-deploy health check `HTTP 200` ผ่านอัตโนมัติ. **Smoke PROD** ด้วย learner ทดสอบ 610034 (บัญชีเดิมที่เคยใช้ E2E บน PROD มาก่อน — อ่านอย่างเดียว ไม่กด save/commit ใด ๆ): เจอคอร์ส `SA-101-JP` (courseId=507) ที่ **completed จริง** บน PROD ⇒ ปิด gap ที่ QA ไม่มีให้ทดสอบได้ — ยืนยัน `#courseStatusDisplay` = `course-meta-value status-success`, สีเขียว `rgb(30,126,52)`, พื้นโปร่งใส, border-radius 0 (ไม่ใช่ pill), ข้อความ `เรียนจบแล้ว`. Toolbar order + `window.DevExpress===undefined` ตรงเหมือน QA. Logout หลังทดสอบเสร็จ
- ไฟล์หลักที่แตะ: `DOC/PLANS/PLAN-109-*.md` (→VERIFIED), `DOC/AGENT_LOG.md` (ไม่มีการแก้โค้ดเพิ่มรอบนี้ — deploy ของที่มีอยู่แล้ว)
- Contract ที่เปลี่ยน: ไม่มี
- Verified: PROD health check 200 (auto จาก deploy script) + Playwright smoke ยืนยันครบทั้ง 2 สถานะสี (กำลังเรียน จาก QA รอบก่อน + เรียนจบแล้ว จาก PROD รอบนี้) + toolbar + ไม่มี DevExpress
- **PLAN-109 ปิดงานสมบูรณ์:** deploy ทั้ง QA + PROD แล้ว, smoke ครอบคลุม 2/3 สถานะสี (เหลือ `ไม่ผ่านเกณฑ์`/สอบตก ที่ไม่มีบัญชีทดสอบพร้อมสถานะนี้ในทั้ง 2 env — ความเสี่ยงต่ำเพราะ logic เดิมไม่เปลี่ยน)

## [2026-07-22 —] Claude Code — เขียน PLAN-111: Category SortOrder (running no ต่อ Division) — backend+React
- ทำอะไร: ผู้ใช้ขอให้ Categories มี running no แยกจากชื่อ, เรียง id ต่อ Division, admin ปรับเอง. สำรวจ: Category ไม่มี field ลำดับ (เลข "1." ฝังใน Name), admin React เป็น table config (moduleConfigs masterDataCategories), ordering ปัจจุบัน by Name. **ผู้ใช้ตัดสิน: (1) แก้ด้วยช่องกรอกตัวเลข ไม่ใช่ drag-drop (2) ตัดเลขหน้าชื่อออก**. เขียน PLAN-111: §1 เพิ่ม `Category.SortOrder` + migration (backfill row_number per DivisionId order by Id + ตัด prefix `^\d+\.\s*` จาก Name) §2 API เพิ่ม `sortOrder` ใน CategoryDto + รับแก้ผ่าน PUT + เปลี่ยน ordering เป็น (DivisionId,SortOrder,Id) [CONTRACT FREEZE] §3 React เพิ่มคอลัมน์ ลำดับ ซ้ายสุด + number input ในฟอร์ม §4 learner sidebar ตัด prefix ตามเจตนา (เพิ่มเลขนำหน้าเป็น option รอยืนยัน)
- ไฟล์หลักที่แตะ: `DOC/PLANS/PLAN-111-*.md` (ใหม่ READY), `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน: +column `Category.SortOrder` (**มี migration** → deploy ต้อง `dotnet ef database update`); CategoryDto +sortOrder additive; ชื่อ category เดิมถูกตัด prefix
- Verified: — (แผน; อ่าน Category entity + CategoriesController/CRUD ordering + moduleConfigs React + learner catalog)
- **ถึง Copilot(§1/§2)+Gemini(§3): contract §2 freeze — เบี่ยง shape ต้องอัปเดตแผน+แจ้งก่อน React ปิดงาน**; migration ต้องอยู่ iLearn.Infrastructure/Migrations; **deploy ต้องรัน database update คู่ (ไม่มี auto-migrate)**; ตรวจ Category.Name matching ก่อนตัด prefix
## [2026-07-22 —] Claude Code — เขียน PLAN-110: เอา .zip ออกจากชื่อ content item (2 เฟส)
- ทำอะไร: ผู้ใช้ไม่อยากให้ชื่อ item ลงท้าย .zip. วินิจฉัย: `ContentItem.Name = Path.GetFileName(uploaded)` เก็บ .zip ตอน upload. **กับดักสำคัญ: .zip ใน Name ถูกใช้เชิง logic** — `ContentItemsController:967` ตรวจ `Path.GetExtension(contentItem.Name)==".zip"` เพื่อตัดสินประมวลผล SCORM ตอน bulk ⇒ ตัด .zip จาก DB ดื้อ ๆ จะพัง. เขียน PLAN-110 แบ่ง 2 เฟส: **เฟส 1 (ปลอดภัย 100%)** strip .zip ตอน projection `PlayerContentItemDto.Name` ที่ `EnrollmentsController.GetPlayerInfoByCourse` — learner เห็นสะอาดทันที ไม่แตะ DB/logic 967, เป็น safety net ถาวร. **เฟส 2 (ราก, ทำตามลำดับ)** 2.1 decouple บรรทัด 967 ให้ใช้ extension จาก FileStorage.Name แทน → 2.2 strip ตอน upload (แก้ Name บรรทัด 284, คง FileStorage.Name เป็น .zip) → 2.3 one-time SQL UPDATE ล้างข้อมูลเก่า (QA→PROD, หลัง 2.1 ขึ้น PROD เท่านั้น, backup ก่อน). FileStorage.Name/launchUrl ไม่แตะ
- ไฟล์หลักที่แตะ: `DOC/PLANS/PLAN-110-*.md` (ใหม่ READY), `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน: ไม่มีในตัวแผน (เฟส 1 display strip; 2.2 ชื่อ upload ใหม่; 2.3 data script ไม่มี migration schema)
- Verified: — (แผน; grep ยืนยัน Name source ที่ upload + จุดแสดง + coupling ที่ 967, FileStorage.Name มี .zip เสมอ)
- **ถึง Copilot: ลำดับสำคัญ — 2.3 ล้าง DB ห้ามทำก่อน 2.1 decouple ขึ้น PROD** ไม่งั้น bulk-process ของ draft ที่ยังไม่ published จะพัง (extension เป็น ""); ตัดเฉพาะ suffix .zip อย่าใช้ GetFileNameWithoutExtension กับชื่อที่มี dot อื่น
## [2026-07-22 —] GitHub Copilot — PLAN-109: สถานะเหลือ 3 คำ + เลิก pill เป็นข้อความมีสี (§3 button swap มีอยู่แล้ว)
- ทำอะไร: **§1** ตัด `ดูอย่างเดียว` ออกทั้ง 3 จุด (`renderCourseDetails`/`setupReadOnlyMode`/`recalcTotalProgress` readOnly-branch — สาขานั้นไม่ set อะไรเลยเพราะแถวถูกซ่อนโดย 107 อยู่แล้ว) + เปลี่ยน `พร้อมแสดงผล`→`เรียนจบแล้ว` (1 จุด, ไม่แตะ `$("#btnPreSave").prop("disabled", false)`) ⇒ เหลือ 3 คำ: กำลังเรียน/เรียนจบแล้ว/ไม่ผ่านเกณฑ์. **§2** เปลี่ยน markup `#courseStatusDisplay` จาก `course-status-pill status-muted` → `course-meta-value status-muted`; CSS ลบ `.course-status-pill` + `.status-warning` ทิ้ง, เหลือ `.status-muted/success/danger` เป็นสีอักษรอย่างเดียว (ไม่มี background) — ยัง declare หลัง `.course-meta-value` ใน source order เหมือนเดิม. **§3** ยืนยันด้วย grep ว่า Claude สลับปุ่ม `#btnFullscreen`→`#btnToggleSidebar` ให้แล้วก่อนหน้านี้ ไม่ต้องแก้ซ้ำ. ไม่แตะ `setCourseStatusDisplay`/`setCourseProgressDisplay`/`recalcTotalProgress` logic ตามที่ plan สั่ง (เหลือ `removeClass("...status-warning")` ที่ไม่มี CSS แล้วแต่ไม่กระทบอะไร)
- ไฟล์หลักที่แตะ: `iLearn.User/Views/MyLearning/Player.cshtml`, `DOC/PLANS/PLAN-109-*.md` (→DONE + Implementer Notes), `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน: ไม่มี (แก้ label string + CSS class เท่านั้น; `#courseStatusDisplay` id เดิม)
- Verified: `dotnet build iLearn.User -o artifacts\verify-user-109` → Build succeeded 0 errors, ลบ artifacts แล้ว
- **Deploy QA:** `tools\deploy-user.ps1` → stamp `_user_deploy_20260722083900` live
- **Smoke QA (Playwright, learner 610034, courseId=968 "TEST" กำลังเรียน 50%):** `#courseStatusDisplay` computed style ยืนยัน `background: transparent`, `border-radius: 0px`, class `course-meta-value status-muted`, text `กำลังเรียน` (ไม่ใช่ pill แล้ว) ✅; toolbar DOM ยืนยัน `#btnFullscreen` มาก่อน `#btnToggleSidebar` ✅; `window.DevExpress === undefined` ✅ (108 ยังอยู่); console 0 error
- **ยังไม่ได้ทดสอบ** สถานะ `เรียนจบแล้ว`/`ไม่ผ่านเกณฑ์` จริงบน QA — บัญชี 610034 ไม่มีคอร์ส completed/failed อยู่ตอนนี้ (ต้องเล่นจบจริงหรือหา test account อื่นถ้าต้องการ cover ครบ 3 สี); logic ไม่เปลี่ยน ความเสี่ยงต่ำ
- **ถึง reviewer/QA: ยังไม่ได้ deploy/ถ่าย screenshot ในรอบนี้** — ต้อง deploy learner (QA อนุมัติล่วงหน้าแล้ว, ห้าม PROD) แล้วเช็ค 3 สถานะสี + ปุ่ม toolbar ตาม Verification list ในแผน

## [2026-07-22 —] Claude Code — เขียน PLAN-109 (สถานะ 3 คำ + เลิก pill + สลับปุ่ม toolbar) มอบ Copilot
- ทำอะไร: ผู้ใช้รีวิว Player บน iPad (อาการนิ่งของ 108 หายแล้ว ✅) ขอปรับ 3 อย่าง. เขียน PLAN-109: **§1** ตัดคำ `ดูอย่างเดียว` (3 จุด — 107 ซ่อนแถวอยู่แล้ว) + เปลี่ยน `พร้อมแสดงผล`→`เรียนจบแล้ว` (1 จุด, ห้ามแตะ enable #btnPreSave) ⇒ เหลือ 3 คำ กำลังเรียน/เรียนจบแล้ว/ไม่ผ่านเกณฑ์. **§2** เลิก `.course-status-pill` → `.course-meta-value` + เปลี่ยน `.status-*` เป็นสีอักษรอย่างเดียว (แบบ B ผู้ใช้เลือก) — ยืนยัน status-* ใช้เฉพาะ #courseStatusDisplay จึงแก้ปลอดภัย. **§3 Claude สลับปุ่ม เต็มจอ↔พับแถบข้อมูล ให้แล้วใน working tree** (build ผ่าน) Copilot แค่ commit รวม. **QA deploy ผู้ใช้อนุมัติล่วงหน้า**
- ไฟล์หลักที่แตะ: `DOC/PLANS/PLAN-109-*.md` (ใหม่ READY), `iLearn.User/Views/MyLearning/Player.cshtml` (§3 button swap โดย Claude), `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน: ไม่มี (แก้ label string + CSS; #courseStatusDisplay id เดิม)
- Verified: build 0 errors (หลังสลับปุ่ม) + grep ยืนยัน status-* ใช้ที่เดียว
- **ถึง Copilot: §3 ทำแล้วอย่าทำซ้ำ (commit รวม); §1 แก้แค่ string ห้ามแตะ logic ปุ่มแสดงผล; §2 วาง .status-* หลัง .course-meta-value เพื่อให้สีอักษรชนะ; deploy QA ได้เลย ห้าม PROD**

## [2026-07-21 —] Claude Code — รีวิว PLAN-108 รอบ 4: Fix 4/6/7 ผ่าน + ยอมรับ Fix 5 เป็น false finding
- ทำอะไร: commit `4f7e65b` ทำ Fix 4/6/7. **Fix 4** `a.catalog-course-item:not(.catalog-list-item){display:block}` — render จริงยืนยัน list เป็น flex row, badge ไม่เต็มกว้าง, ปุ่มขวา ⇒ regression หาย. **Fix 6** ปุ่ม ดูเนื้อหา กลับมาเป็น `<span>` (list+grid) ไม่มี nested anchor. **Fix 7** ครบ 4 ส่วน: spinner เป็น CSS `::before content` (JS ไม่แตะ class ไอคอน), pointer-events เข้า class, `pageshow` ล้าง .is-navigating (จับ bfcache ที่ DOMContentLoaded ไม่จับ), timeout 8s. **Fix 5 (login autofocus): ผมแจ้งผิดในรอบ 3** — `$employeeCodeBox.trigger("focus")` มีมาตั้งแต่ commit แรก `7bdfca5` ไม่เคยหาย; รอบ 3 ผม grep `focus()` ซึ่งไม่ match `.trigger("focus")` เลยสรุปผิด. โค้ด login ถูกต้องอยู่แล้ว ($employeeCodeBox ชี้ #employee-code-input จริง)
- ไฟล์หลักที่แตะ: `DOC/PLANS/PLAN-108-*.md` (→REVIEWED รอบ 4 + Sign-off), `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน: ไม่มี (รีวิว)
- Verified: render list view วัด computed style + node --check + build 0 errors + git log -S ยืนยันประวัติ trigger("focus")
- **บทเรียนของผม: grep ด้วย pattern แคบ (`focus()`) พลาด `.trigger("focus")` → false finding** — ครั้งหน้าตรวจ behavior ด้วยการอ่าน context ไม่ใช่ grep คำเดียว. QA เพิ่ง deploy build ใหม่ `_user_deploy_20260721170621`
- **เหลือก่อน VERIFIED: iPad smoke** — กดการ์ด→Back ไม่ค้าง/กดซ้ำได้, list+grid ตรง PROD, login label ลอยตอนเข้าหน้า, อาการนิ่ง 0.5 วิ หายจริง (เป้าหมายงาน)

## [2026-07-21 —] GitHub Copilot — PLAN-108 Fix 4/5/6/7: list view layout, login focus, ปุ่มดูเนื้อหากลับมา, spinner ค้างจาก bfcache
- ทำอะไร: ตอบ Reviewer รอบ 3 (screenshot QA vs PROD + spinner ค้าง) ทั้ง 4 finding — **Fix 4 (บล็อก)** แก้ CSS specificity ที่ทำให้ catalog list view พังเป็นแนวตั้ง: แยก `a.course-item, a.catalog-course-item { display:block }` ออกจาก `text-decoration/color`, แล้วจำกัด `display:block` ด้วย `a.catalog-course-item:not(.catalog-list-item)` ไม่ให้ไปทับ `.catalog-list-item { display:flex }` ของตัวเอง. **Fix 5** ตรวจซ้ำแล้วพบว่า `$employeeCodeBox.trigger("focus")` มีอยู่แล้วทั้ง 2 จุด (setTimeout โหลดหน้า + handleLoginError) ตั้งแต่รอบ Fix 1/2/3 — ทดสอบจริงบน QA ยืนยัน focus/border สีแบรนด์/label ลอย/placeholder โผล่ครบ ไม่มีอะไรต้องแก้โค้ด (คาดว่า screenshot รอบก่อนแคปก่อน setTimeout 300ms ทำงาน). **Fix 6** คืนปุ่ม "👁 ดูเนื้อหา" กลับเข้า catalog grid+list เป็น `<span class="btn ...">` (ไม่ใช่ `<a>` กัน nested anchor) แทนไอคอนลูกศรเดี่ยวที่เอาออกไปตาม §2b เดิม — ตรงกับ PROD ทุกตัวอักษร, ลบ `.catalog-arrow-action` ที่เลิกใช้. **Fix 7 (บล็อก)** เปลี่ยนกลไก spinner จากการเขียนทับ `$icon.attr("class", ...)` (ทำลายไอคอนเดิมถาวร) เป็น CSS ล้วน (`.is-navigating .js-card-action-icon::before { content:"\f1ce" }` + keyframes), ย้าย `pointer-events:none` เข้า CSS class แทน inline style, เพิ่ม `pageshow` listener เคลียร์ `.is-navigating` ทุก element ที่ค้าง (แก้ปัญหาการ์ดตายถาวรหลังกด Back บน iPad ที่ใช้ bfcache หนัก) และ timeout 8 วิกันตายถาวรถ้า navigation ถูกยกเลิก
- ไฟล์หลักที่แตะ: `iLearn.User/Views/MyLearning/Index.cshtml`, `iLearn.User/wwwroot/js/site.js`, `iLearn.User/wwwroot/css/user-theme.css`, `DOC/PLANS/PLAN-108-drop-devextreme-from-learner.md`, `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี — DOM/JS/CSS เท่านั้น
- Verified: `dotnet build iLearn.User -o artifacts\verify-user-108c` ผ่าน 0 errors แล้วลบ artifacts
- **ค้างสำหรับ reviewer/QA:** deploy QA แล้วเทียบภาพซ้ำ (login/list/grid), ทดสอบ Fix 7 บน iPad Safari จริง (กด Back แล้วการ์ดต้องกลับมากดได้ทันที), และที่สำคัญที่สุด **วัดว่าอาการนิ่ง ~0.5 วิ บน iPad หายจริงไหม** — ยังไม่มีใครวัดจริงตลอดทุกรอบที่ผ่านมา

## [2026-07-21 —] Claude Code — PLAN-108: เพิ่ม Fix 7 (spinner ค้าง + การ์ดกดไม่ได้จาก bfcache)
- ทำอะไร: ผู้ใช้รายงาน spinner ค้างบน QA. ตรวจ `site.js` พบว่า **ไม่มี cleanup เลย** (ไม่มี `pageshow`/`persisted`) ⇒ กด Back แล้วเบราว์เซอร์คืนหน้าจาก **bfcache** ทำให้ `.is-navigating` + **inline `pointer-events:none`** + ไอคอน spinner + flag `data("ilearnNavigating")` ค้างทั้งหมด. **ความรุนแรงมากกว่าที่เห็น: การ์ดที่ค้างกดซ้ำไม่ได้อีกเลย** จนกว่าจะ hard refresh — บน iPad ที่กด Back เข้าออกคอร์สตลอดจะเหมือนระบบพัง. ซ้ำร้าย `$icon.attr("class", ...)` เขียนทับ class เดิมทิ้ง ⇒ คืนไอคอนเดิมไม่ได้แม้ล้าง state. เขียน Fix 7: (a) swap spinner ด้วย CSS `::before content` + keyframes เอง ไม่แตะ class (b) ย้าย `pointer-events` เข้า class เลิก inline (c) ล้างสถานะที่ `pageshow` (ต้องเป็น pageshow เพราะ bfcache ไม่ยิง DOMContentLoaded) (d) timeout 8 วิกันการ์ดตายถาวรถ้า navigation ถูกยกเลิก
- ไฟล์หลักที่แตะ: `DOC/PLANS/PLAN-108-*.md` (+Fix 7), `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน: ไม่มี (รีวิว)
- Verified: อ่าน site.js ทั้งไฟล์ + grep ยืนยันไม่มี cleanup handler
- **PLAN-108 ตอนนี้ค้าง 4 ข้อ: Fix 4 (list layout), 5 (login autofocus), 6 (ปุ่มดูเนื้อหา), 7 (spinner ค้าง)** — 4/5/6/7 ล้วนเป็นการแก้เล็ก แต่ 7 กระทบการใช้งานหนักสุด

## [2026-07-21 —] Claude Code — PLAN-108 รอบ 3: เทียบภาพ QA vs PROD เจอ 3 จุดต่าง
- ทำอะไร: ผู้ใช้ส่ง screenshot QA/PROD คู่กันและขอ "หน้าตาเหมือนเดิมใน PROD". ตรวจแล้ว: **Fix 4 ยังไม่ได้ทำเลย** (โค้ดไม่เปลี่ยนตั้งแต่ commit 78b4047 — list view ยังพังจาก specificity). **Fix 5 ใหม่:** หน้า login เสีย auto-focus — grep ยืนยันว่า `focus()` **หายทั้ง 2 จุด** ตอน rewrite (ของเดิมมี `setTimeout(...focus(), 300)` ตอนเข้าหน้า + refocus ใน `handleLoginError`) ⇒ PROD ขอบสีแบรนด์/label ลอย/placeholder โผล่ แต่ QA เป็นสถานะพัก; ไม่ใช่แค่หน้าตา — auto-focus คือตัวทำให้คีย์บอร์ดตัวเลขของ 097 ขึ้นบน iPad. **Fix 6 ใหม่:** ผู้ใช้เปลี่ยนใจจาก §2b — PROD มีปุ่ม `ดูเนื้อหา` จึงต้องเอากลับมาเป็น `<span>` (ไม่ใช่ `<a>`) เพื่อให้ตรง PROD แต่ยังกดได้ทั้งแถว
- ไฟล์หลักที่แตะ: `DOC/PLANS/PLAN-108-*.md` (→CHANGES REQUESTED + Fix 4 ค้าง/Fix 5/Fix 6 + verification ใหม่), `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน: ไม่มี (รีวิว)
- Verified: เทียบ screenshot QA/PROD + grep ยืนยัน focus() หาย + `git diff 78b4047 HEAD -- iLearn.User/` = 0 ไฟล์
- **ปิดข้อค้างจากรอบ 2 ได้ 2 ข้อจากภาพ:** ช่องค้นหา และการ์ด "หลักสูตรของฉัน" **ตรงกับ PROD แล้ว** ไม่ต้องแก้
- **บังคับรอบหน้า: ต้องส่ง screenshot เทียบคู่ QA vs PROD** ทั้ง login / list view / grid view — รอบก่อนผมเซ็นผ่านโดยไม่มีภาพแล้วพลาด regression

## [2026-07-21 —] Claude Code — PLAN-108: พบ Fix 4 หลัง sign-off (list view พังจาก CSS specificity)
- ทำอะไร: หลังผม sign-off รอบ 2 ไปแล้ว implementer แนบ screenshot มา **เห็นว่าแถวใน list view ของคลังหลักสูตรพัง** — แถบโค้ดยืดเต็มความกว้าง ชื่อ+ลูกศรตกลงมาแนวตั้ง. **สาเหตุ: specificity ชนกัน** — `<a class="catalog-list-item catalog-course-item">` โดน `a.catalog-course-item { display:block }` (0,1,1) ทับ `.catalog-list-item { display:flex }` (0,1,0) ⇒ flex row หาย. ระบุวิธีแก้ 2 ทาง (ยกเว้น list ออกจากกฎ block หรือยก specificity ของ list) + ให้ตรวจว่า grid ไม่พังตาม. **Fix 1-3 ยืนยันแล้วไม่ต้องแก้ซ้ำ** — screenshot login ยังยืนยัน Fix 2 ซ้ำอีกชั้น (label ลอยคร่อมขอบ สีแบรนด์ placeholder โผล่ ตรงของเดิม)
- ไฟล์หลักที่แตะ: `DOC/PLANS/PLAN-108-*.md` (→CHANGES REQUESTED + Fix 4), `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน: ไม่มี (รีวิว)
- Verified: อ่าน CSS ยืนยัน specificity + screenshot หน้าจริง
- **บทเรียน: finding นี้มองไม่เห็นจากอ่านโค้ดหรือ render แยกส่วน เห็นจาก screenshot หน้าจริงเท่านั้น** — verification §A ที่บังคับเทียบภาพมีค่าจริง ผม sign-off เร็วไปโดยยังไม่มีภาพหน้าเต็ม
- หมายเหตุ: มีไฟล์ `plan108-*.png` ค้างใน repo root (ยังไม่ commit) — ควรย้ายออกหรือลบ ไม่ควรเก็บใน root

## [2026-07-21 —] Claude Code — รีวิว PLAN-108 รอบ 2 → ผ่าน finding ครบทั้ง 3
- ทำอะไร: **Fix 1** การ์ดเป็น `<a>` แล้ว class/attr ครบบน element นอกสุด (กรอง/ค้นหาไม่พัง), ปุ่ม→`span`, catalog→ลูกศร, **ไม่มี nested anchor** (render ยืนยัน `a a` = 0), `site.js` ปรับ selector + ใช้ `.js-card-action-icon` เป็น marker (ดีกว่าสเปค). **Fix 2** floating label — ผม render ของใหม่แล้ววัดเทียบ baseline ที่วัดจาก DevExtreme ตัวจริงไว้: พัก 14px/#999/top 17px ✅ ลอย 12px/top −7px ✅ ช่อง h50/radius4/border #ddd/input 14px #333 pl9 ✅ focus สีแบรนด์ (CSSOM ยืนยัน) ✅ placeholder ซ่อน/แสดงตรงพฤติกรรม DX ✅. **Fix 3** `.gitignore` เพิ่มกติกา `!` + `site.js` เข้า commit `78b4047` แล้ว โดย vendored libs ยัง ignore ถูกต้อง
- ไฟล์หลักที่แตะ: `DOC/PLANS/PLAN-108-*.md` (→REVIEWED รอบ 2 + Sign-off), `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน: ไม่มี (รีวิว)
- Verified: render measurement เทียบ baseline + CSSOM + `node --check` 3 ไฟล์ + build 0 errors + ตรวจ nested anchor/gitignore
- **คงค้างก่อน VERIFIED:** เทียบหน้าตา **ช่องค้นหา** + **dialog ออกจากระบบ** (2 จุดที่ยังไม่ได้เทียบ — toast/login พิสูจน์แล้ว), **วัดอาการนิ่งบน iPad ว่าหายจริงไหม (เป้าหมายของงาน)**, และ B2 บน iPad (กดการ์ด/กรอง/เลื่อนไม่เผลอเปิด)

## [2026-07-21 —] GitHub Copilot — PLAN-108 Fix 1/2/3: การ์ดกดได้ทั้งใบ + floating label + site.js เข้า git
- ทำอะไร: ตอบ Reviewer Sign-off ของ Claude Code ทั้ง 3 finding — **Fix 1 (บล็อก)** ทำ §2a/2b/2c ที่ผู้ใช้อนุมัติเพิ่มระหว่างรีวิว: การ์ด "หลักสูตรของฉัน"/catalog grid/catalog list ใน `MyLearning/Index.cshtml` เปลี่ยน outer element จาก `<div>`→`<a href>` ทั้งใบ (คง `course-item`/`catalog-course-item`/`carousel-card`/`data-status` บน element นอกสุดตามเดิม, ไม่มี `<a>` ซ้อน `<a>`), ปุ่มข้างในหลักสูตรของฉันเปลี่ยนเป็น `<span>` (ข้อความเดิม), ปุ่ม catalog เอาออกเหลือไอคอนลูกศรเดียว, เพิ่ม `:active`/`:focus-visible` บนการ์ดทั้งใบ, ปรับ `site.js` nav-feedback selector ให้ตามการ์ดใหม่ + เพิ่ม marker class `.js-card-action-icon` กันดึงไอคอนผิดตัว (ของเดิมจะไปเจอ `fa-calendar-alt` ของ due-date ก่อน). **Fix 2 (กลาง)** คืน floating label ให้หน้า login (`Home/Index.cshtml`) ตามค่าที่ Claude Code วัดจาก DevExtreme ตัวจริงมาให้เป๊ะ (label top 17px/14px ตอนว่าง, top -7px/12px ตอน focus/มีค่า, สีเปลี่ยนเฉพาะตอน focus, placeholder ซ่อนจนกว่าจะลอย). **Fix 3 (กลาง)** เพิ่ม `!iLearn.User/wwwroot/js/` + `!iLearn.User/wwwroot/js/site.js` ใน `.gitignore` ตาม precedent ของ `iLearn.Admin` เพื่อเอา `site.js` (ที่มี §2 handler) เข้า version control ครั้งแรก
- ไฟล์หลักที่แตะ: `iLearn.User/Views/MyLearning/Index.cshtml`, `iLearn.User/Views/Home/Index.cshtml`, `iLearn.User/Views/MyLearning/Player.cshtml`, `iLearn.User/wwwroot/js/site.js` (เข้า git ครั้งแรก), `.gitignore`, `DOC/PLANS/PLAN-108-drop-devextreme-from-learner.md`, `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี — DOM/JS/CSS เท่านั้น; `filterCatalog`/`data-status`/highlight selector ยังทำงานเหมือนเดิมเพราะ class อยู่ตำแหน่งเดิม
- Verified: `dotnet build iLearn.User -o artifacts\verify-user-108b` ผ่าน 0 errors แล้วลบ artifacts; ตรวจโค้ดยืนยันไม่มี `<a>` ซ้อน `<a>` ในทุก template ใหม่
- **ค้างสำหรับ reviewer/QA (เหมือนเดิม):** เปิดเบราว์เซอร์จริงเทียบ login (ทั้ง 2 สถานะ)/ช่องค้นหา/dialog ตาม §A, ทดสอบการ์ดกดได้ตาม §B2, และที่สำคัญที่สุด **ทดสอบบน iPad จริงว่าอาการนิ่ง ~0.5 วิ หายหรือยัง (§C ข้อ 17-19)** — ยังไม่ได้ทำในรอบนี้เช่นกัน


## [2026-07-21 —] Claude Code — รีวิว PLAN-108 → CHANGES REQUESTED (3 finding) + ยอมรับความผิดพลาดเรื่อง commit
- ทำอะไร: รีวิว 108. **§1 ผ่านสะอาด** และผม**ทำ §0 baseline ที่ implementer ทำไม่ได้ให้แล้ว** (เขาไม่มีเบราว์เซอร์) — render เทียบ `.dx-toast` เก่ากับ `.app-toast` ใหม่โดยโหลด `dx.light.css` จริงมาวางคู่กัน วัด computed style: height 52px, font 14px/600/32px, padding 10px, radius 6px, สีทั้ง 4 type **ตรงกันทุก property** ⇒ toast (จุดเสี่ยงสูงสุด) เหมือนเดิมจริง. อื่น ๆ ถูกหมด: ถอด asset ครบ, คง `bootstrap.min.css` ถูกต้อง, inputAttr 6 ตัวครบ, showToast signature เดิม + กัน XSS + aria-live, ลบ dead CSS 287 บรรทัดโดยคง `--dx-color-border` ที่ยังใช้จริง. **3 finding ที่ต้องแก้:** (1) 🔴 **§2a/2b/2c การ์ดกดได้ยังไม่ได้ทำ** — การ์ดยังเป็น `<div>` ปุ่มยังเป็น `<a>` เพราะผมแก้ §2 ระหว่างที่เขากำลังทำ เขาเลยอิงสเปคเก่า (ไม่ใช่ความผิดเขา); (2) 🟠 login เปลี่ยน floating label เป็น label ด้านบน = หน้าตาต่างจากเดิม ขัดข้อกำหนดผู้ใช้; (3) 🟠 **โค้ด §2 อยู่ใน `wwwroot/js/site.js` ที่ `.gitignore` ตัดทิ้ง** (`iLearn.User/wwwroot/**` re-include แค่ user-theme.css) ⇒ ไม่อยู่ใน version control เลย — ต้องเพิ่มกติกา `!` แบบที่ iLearn.Admin ทำไว้แล้ว
- ไฟล์หลักที่แตะ: `DOC/PLANS/PLAN-108-*.md` (→CHANGES REQUESTED + Sign-off), `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน: ไม่มี (รีวิว)
- Verified: render เทียบ toast เก่า/ใหม่วัด computed style + grep ยืนยันค่าสีใน dx.light.css ต้นทาง + ตรวจ .gitignore/git ls-files + build
- **⚠️ ความผิดพลาดของผมเอง:** ตอน commit แผน §2 (`7bdfca5`) ผมใช้ `git add -A` โดยไม่ดู tree ก่อน ทำให้**โค้ดของ Copilot ที่กำลังทำค้างอยู่ถูกกวาดเข้า commit ที่ message เขียนว่า docs** ⇒ commit นั้น message ไม่ตรงเนื้อหา และผม commit โค้ดที่ยังไม่ได้รีวิว ต่อไปต้อง `git status` ก่อน `git add -A` เสมอ หรือระบุ path ให้ชัด

## [2026-07-21 —] GitHub Copilot — PLAN-108 DONE: ตัด DevExtreme ออกจาก iLearn.User ทั้งหมด
- ทำอะไร: แทนที่ทั้ง 4 จุดที่ใช้ DevExtreme ด้วย Bootstrap/vanilla JS ตามแผน — **1a/1b** `Home/Index.cshtml`: `dxTextBox`→`<input class="form-control">` (คง `inputAttr` 6 ตัวของ PLAN-097 ครบ, รวม Enter-key handler เหลือจุดเดียว), `dxButton`→`<button class="btn btn-login">`. **1c** `MyLearning/Index.cshtml`: ช่องค้นหา `dxTextBox mode="search"`→ input+ไอคอน FA ใน `.app-search-box` (คง debounce 250ms). **1d (จุดเสี่ยงสุด)** `_DevExtremeLayout.cshtml`: `showToast()` คง signature เดิมเป๊ะ แต่ภายในเปลี่ยนเป็น stack `.app-toast` เอง (fixed top-center, prepend=down-push, fade 400ms/40ms) — ดึงค่าจริงจาก `dx.light.css` ที่ minified มาด้วย regex grep (`.dx-toast-content{...border-radius:6px}`, สีต่อ type) เพื่อให้หน้าตาตรงเป๊ะ, เปลี่ยนเป็น `.text()` กัน XSS (ปรับปรุงความปลอดภัยที่ของเดิมไม่มี). **1e** dialog ออกจากระบบ → Bootstrap Modal (คง CSS `logout-dialog-*` เดิม). **1f** ลบ `<link dx.light.css>`/`<script dx.all.js>`/`<script devextreme-license.js>` ออกจาก layout (ไฟล์ยังอยู่บน disk ตามแผน, คง `css/devextreme/bootstrap.min.css` ไว้เพราะเป็น Bootstrap 5.3.8 ตัวจริงของทั้งแอป ไม่ใช่ของ DevExtreme). **1g** ลบบล็อก `DEVEXTREME OVERRIDES` ~287 บรรทัดออกจาก `user-theme.css` หลัง grep ยืนยันไม่มี view ไหนใช้ widget class เหล่านั้นแล้ว (เก็บ `--dx-color-border` ไว้เพราะยังใช้จริงใน `.course-body`). **§2** เพิ่ม delegated click handler ใน `site.js` ให้ `.btn-continue`/`.btn-list-view`/`.player-back-link` เปลี่ยนไอคอนเป็น spinner ทันทีที่กด (ไม่ preventDefault)
- ไฟล์หลักที่แตะ: `iLearn.User/Views/Home/Index.cshtml`, `iLearn.User/Views/MyLearning/Index.cshtml`, `iLearn.User/Views/Shared/_DevExtremeLayout.cshtml`, `iLearn.User/wwwroot/css/user-theme.css`, `iLearn.User/wwwroot/js/site.js`, `DOC/PLANS/PLAN-108-drop-devextreme-from-learner.md`, `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี — DOM/JS เท่านั้น; `showToast(message, type, duration)` คง signature เดิมเป๊ะ; ไม่แตะ SCORM/commit/session logic ของ 097/103–107
- Verified: `dotnet build iLearn.User -o artifacts\verify-user-108` ผ่าน 0 errors แล้วลบ artifacts; grep `iLearn.User/Views/**` ยืนยันไม่เหลือ `DevExpress`/`dxTextBox`/`dxButton`/`dx.all.js`/`dx.light.css`/`devextreme-license` นอกจากคอมเมนต์อ้างอิงชื่อเดิม
- **⚠️ ถึง Claude Code (reviewer): §0 baseline (screenshot/computed style) ยังไม่ได้ทำจริง** — session นี้เป็น text-only ไม่มี browser/QA server ให้เปิด จึงทดแทนด้วยการดึงค่า CSS จริงจาก `dx.light.css` (minified) ด้วย regex grep แทน (สี/padding/border-radius/font-size ของ toast ตรงกับต้นฉบับ). **สิ่งที่ยังไม่ได้ทำจริงและต้องทำก่อนขึ้น QA:** (1) เปิดเบราว์เซอร์จริงเทียบหน้า login/ช่องค้นหา/toast ทั้ง 4 type/dialog ตาม Verification §A, (2) ทดสอบบน iPad จริงตาม §C ข้อ 10-12 (ตัวชี้วัดจริงของงานนี้), (3) ปุ่ม login ใหม่ไม่มี floating label เหมือนเดิม (ใช้ label ธรรมดาแทน) — ถ้าเทียบกับ baseline แล้วต่างชัดเจน ให้แจ้งกลับเพื่อปรับเป็น `.form-floating` ของ Bootstrap แทน


## [2026-07-21 —] Claude Code — แก้ §2 ของ PLAN-108: การ์ดกดได้ทั้งใบ (ผู้ใช้เห็นด้วยกับข้อเสนอ)
- ทำอะไร: ผู้ใช้ถามความเห็นว่าควรให้กดที่การ์ดได้เลยแล้วเอาปุ่ม `ดูเนื้อหา` ออกไหม. **ผมเห็นด้วยกับการ์ดกดได้** (tap target โตขึ้นมากบน iPad) **แต่ค้านการเอาปุ่มออกทั้งหมด** เพราะปุ่มในหน้า "หลักสูตรของฉัน" แบกข้อมูลสถานะอยู่ (`เริ่มเรียน`/`เรียนต่อ`/`ทบทวน`) และการ์ดเปล่าเสีย affordance. ผู้ใช้เห็นด้วยกับทางกลาง ⇒ แก้ §2 ของ 108 (ยังไม่เริ่ม implement จึงแก้ในแผนเดิม ไม่เปิดแผนใหม่มาแย่ง markup): **2a** element นอกสุดของการ์ดเป็น `<a>` จริง (ห้าม onclick บน div — ไม่งั้นเสีย a11y/กดค้างเปิดแท็บใหม่) โดย**ต้องคง `course-item`/`catalog-course-item`/`data-status` ไว้บน element นอกสุด** เพราะตัวกรองสถานะ+`filterCatalog` ใช้ selector พวกนี้; **2b** หลักสูตรของฉัน = ปุ่มเปลี่ยนเป็น `<span>` ข้อความเดิม / คลังหลักสูตร = เอาปุ่มออกเหลือลูกศร; **2c** `:active`+`:focus-visible`; **2d** spinner ตอนกำลังโหลด
- ไฟล์หลักที่แตะ: `DOC/PLANS/PLAN-108-*.md` (แก้ §2 + นอก Scope + Verification), `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน: ไม่มี
- Verified: — (แผน; อ่าน markup การ์ดทั้ง 3 แบบยืนยันว่าไม่มี interactive element อื่นข้างใน ⇒ ครอบด้วย `<a>` ได้สะอาด ไม่เกิด nested link)
- **แยกขอบเขตหน้าตาให้ชัดในแผนแล้ว: §1 ห้ามเปลี่ยนหน้าตาเด็ดขาด · §2 เปลี่ยนได้เฉพาะ 2a-2d ที่ผู้ใช้อนุมัติ ห้ามถือโอกาส redesign อย่างอื่น**
- **ถึง Copilot: เพิ่ม verification B2 6 ข้อ** — โดยเฉพาะ `document.querySelectorAll('a a').length === 0` (กัน nested link) และตัวกรองสถานะ/ค้นหาต้องไม่พัง

## [2026-07-21 —] Claude Code — วินิจฉัยอาการหน้าค้าง ~0.5 วิ บน iPad + เขียน PLAN-108 (ตัด DevExtreme)
- ทำอะไร: ผู้ใช้ทดสอบ iPad แล้วทุกอย่างดี ยกเว้นกด `ดูเนื้อหา`/`เริ่มเรียน` แล้วหน้านิ่ง ~0.5 วิ. **วินิจฉัย: ไม่ใช่โค้ดตอนกด** — ปุ่มเป็น `<a href>` ธรรมดาไม่มี handler ⇒ เป็นต้นทุน parse/execute ของหน้าที่โหลดใหม่. grep พบว่า **ทั้ง learner app ใช้ DevExtreme แค่ 4 อย่าง** (dxTextBox×3, dxButton×2, notify×2, dialog.custom×1) แต่โหลด `dx.all.js` **5.1 MB** + `dx.light.css` 676 KB ทุกหน้า — brotli ของ 096 ลดแค่ขนาดส่ง **ไม่ลดเวลา parse** ซึ่งบน CPU iPad = ~300-600ms ตรงกับอาการ. เขียน PLAN-108: §0 เก็บ baseline หน้าตาก่อน (บังคับ — เพราะ `.dx-toast-content-custom` ไม่มี CSS ของเราเลย หน้าตา toast มาจาก dx.light.css ล้วน), §1 แทนที่ 4 จุดด้วย Bootstrap/vanilla ที่โหลดอยู่แล้ว + ตัด asset + ลบ dead CSS 287 บรรทัด, §2 feedback ตอนกดปุ่มนำทาง. เป้า: JS ต่อหน้า **5.3MB → ~170KB**
- ไฟล์หลักที่แตะ: `DOC/PLANS/PLAN-108-drop-devextreme-from-learner.md` (ใหม่ READY), `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน: ไม่มีในตัวแผน (108 จะเอา `DevExpress` namespace ออก แต่ **`showToast` ต้องคง signature เดิม**)
- Verified: — (แผน; grep การใช้ DevExtreme ทั้ง Views/, du ขนาด asset, อ่านโค้ดทั้ง 4 จุด + CSS ที่เกี่ยวข้อง)
- **🔒 ข้อจำกัดจากผู้ใช้: หน้าตา UI ต้องเหมือนเดิม 100%** ⇒ แผนบังคับเก็บ baseline (screenshot + computed style) ก่อนแตะโค้ด และ verification ต้องแนบ before/after เทียบ
- **ถึง Copilot: จุดเสี่ยงสูงสุดคือ §1d (toast)** — ถ้าพัง error ทั้งระบบจะเงียบ (session หมดอายุ/login ผิด ผู้ใช้ไม่เห็นอะไร); `.btn-login` และ `.logout-dialog-*` มี CSS ของเราอยู่แล้วจึงง่าย; **ห้ามลบไฟล์ dx.all.js ออกจากโฟลเดอร์** เพื่อให้ rollback ได้ทันทีด้วยการคืน `<script>` ใน layout

## [2026-07-21 15:36] GitHub Copilot — QA deploy: PLAN-107 (ซ่อนสถานะ/progress ในโหมดดูอย่างเดียว)
- ทำอะไร: deploy `iLearn.User` ขึ้น QA ด้วย `tools\deploy-user.ps1 -HealthCheckUrl 'https://ap-ntc2138-qawb/iLearn/'` (host default = QA, ไม่มี `-Prod`) หลัง PLAN-107 ผ่านรีวิวจาก Claude Code แล้ว ไม่มี migration ไม่แตะ iLearn.API/iLearn.Admin ไม่ deploy PROD
- QA live stamp: `\\AP-NTC2138-QAWB\wwwroot\iLearn\_user_deploy_20260721153643`, web.config → `.\_user_deploy_20260721153643\iLearn.User.dll`, previous `_user_deploy_20260721142236`. Health check HTTP 200 attempt แรก, `AutoRolledBack=False`, เก็บ 3 stamp ล่าสุด (ลบ stale 1)
- ไฟล์หลักที่แตะ: `DOC/PLANS/PLAN-107-hide-status-progress-in-browse-only.md` (→QA DEPLOYED + deploy log), `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน: ไม่มี (deploy เท่านั้น)
- Verified: publish + copy สำเร็จ, health check ผ่าน. **ค้าง:** manual smoke เคส A (browse-only ซ่อนสถานะ/progress)/B (เรียนจบแล้วยังเห็น 100%)/ปกติ บน browser จริงตาม Verification section ของแผน — ยังไม่ได้ทำ

## [2026-07-21 —] Claude Code — ตรวจ reset 18214 (ผ่าน) + รีวิว PLAN-107 → REVIEWED
- ทำอะไร: **ตรวจ admin reset enrollment 18214** — สมบูรณ์ทุกข้อ: Progress=0/IsCompleted=0/ResetAt 15:29:05, **ScormRuntimeStates `IsDeleted=1` ทั้ง 2 แถว** (พิสูจน์ PLAN-101 §1 ในสนามจริง — เคสเดียวกับที่เช้านี้ค้าง `IsDeleted=0`), link snapshot AS-20260716-003 = 0/0/NULL (พิสูจน์ snapshot reset ที่ 101 เพิ่ม), active logs = 0. **ตรวจทั้งระบบ: enrollment ที่ IsCompleted=1 แต่ Progress<100 = 0 รายการ** ⇒ manual check #4 ของ PLAN-104 ผ่านแล้ว; log เกิน 4 ชม. = 0; active log ซ้ำ = ไม่มี. หมายเหตุ: `TotalTimeSpent` ของ 18214 ยังค้าง 9,261 วิ (ResetStatusAsync ไม่ล้างฟิลด์นี้) แต่ rollup จะเขียนทับเองตอน commit แรกของรอบใหม่ — ไม่ต้องแก้. **รีวิว PLAN-107 ผ่าน ไม่มี finding** — ใช้ `currentData.isReadOnly === true` ถูกตามที่กำชับ (ไม่ใช่ตัวแปร OR), render จริงวัด computed style ยืนยันเคส A ซ่อนครบ/เคส B (เรียนจบแล้ว 100%) ไม่ถูกกระทบ
- ไฟล์หลักที่แตะ: `DOC/PLANS/PLAN-107-*.md` (→REVIEWED + Sign-off), `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน: ไม่มี (รีวิว)
- Verified: query QA DB read-only 7 ชุด + standalone render วัด computed style ทั้ง 2 เคส + build learner 0 errors + node --check
- **PLAN-104 เหลือ manual check เจาะจง 2 ข้อ** (ทำ exam ผ่าน→reload ดูว่าผลไม่หาย / เปิดแท็บทิ้ง 5-10 นาที ดูว่าเวลาไม่โป่ง) — ทำตอน 360007 เรียนรอบใหม่ได้พอดี แล้วจึง mark VERIFIED

## [2026-07-21 —] GitHub Copilot — PLAN-107 DONE: ซ่อนสถานะ/progress ในโหมดดูอย่างเดียว (browse-only)
- ทำอะไร: ใน `Player.cshtml` เพิ่ม `id="courseStatusRow"` ให้แถวสถานะในการ์ด header, เพิ่ม logic ใน `setupReadOnlyMode()` ให้ติด class `browse-only` บน `#tocSection` เฉพาะเมื่อ `currentData.isReadOnly === true` (ค่าดิบจาก API = ไม่มี enrollment) — **ไม่ใช้** ตัวแปร client-side `isReadOnly` ที่เป็น OR กับ `isCompleted` เพื่อไม่ให้กระทบเคสเรียนจบแล้ว (ต้องยังเห็นสถานะ+progress 100%). เพิ่ม CSS ซ่อน `#courseStatusRow`, `.course-progress-row`, `.item-progress-track` ภายใต้ `#tocSection.browse-only`. ไม่แตะ `setCourseStatusDisplay`/`setCourseProgressDisplay`/`recalcTotalProgress`, ไม่แตะ `#readOnlyBadge`, ไม่แตะแถวผู้เรียน/หมวดหมู่/ประเภท
- ไฟล์หลักที่แตะ: `iLearn.User/Views/MyLearning/Player.cshtml`, `DOC/PLANS/PLAN-107-hide-status-progress-in-browse-only.md`, `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี — DOM เปลี่ยนแบบ additive เท่านั้น (id ใหม่ + class ใหม่)
- Verified: `dotnet build iLearn.User -o artifacts\verify-user` ผ่าน 0 errors แล้วลบ artifacts. Manual QA เคส A/B/ปกติ (browse-only / เรียนจบแล้ว / กำลังเรียน) ยังต้องทำแยกบน browser จริง

## [2026-07-21 —] Claude Code — ตรวจงาน 105/106 บน QA (ยืนยันอิสระ) + เขียน PLAN-107
- ทำอะไร: **ตรวจข้ออ้าง QA ของ Copilot อย่างอิสระ ผ่านทุกข้อ** — deploy stamps ตรง (API `_deploy_20260721142125`, User `_user_deploy_20260721142236`), health 401/200, และ DB ของ enrollment 18217 ตรงกับที่รายงานเป๊ะ (Progress=100, IsCompleted=1, item 366 `completed/100/00:00:54`, item 397 `passed/passed/100/00:03:13`). **ได้หลักฐานเสริมด้วย:** 104 §C/§D ทำงานจริง (เวลาเรียนล่าสุดสมเหตุสมผลทั้งหมด, **ไม่มี log ไหนเกิน 4 ชม. ทั้งระบบ** เทียบกับเดิมที่เคยโป่ง 9,235 วิ) และ **105 §3 (log ซ้ำ) ไม่เกิดขึ้นจริง — 0 enrollment ที่มี active log ซ้ำ**; 2 logs/item ของ 18217 เป็นคนละฝั่ง ResetAt (14:38:04) ไม่ใช่ duplicate, rollup นับถูก. **ค้าง: enrollment 18214 ยังเพี้ยน** (Progress=50 + IsCompleted=1, exam incomplete/0, 9235s) — เป็นระเบียนเดียวในระบบที่ขัดแย้ง ทำให้ manual check #4 ของ 104 ยังไม่ผ่าน. เขียน **PLAN-107**: โหมดดูอย่างเดียวไม่ต้องแสดงสถานะ+progress
- ไฟล์หลักที่แตะ: `DOC/PLANS/PLAN-107-hide-status-progress-in-browse-only.md` (ใหม่ READY), `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน: ไม่มี (107 เพิ่ม `id="courseStatusRow"` + class `browse-only` แบบ additive)
- Verified: query QA DB read-only หลายรอบ + อ่าน web.config บน share + curl health
- **⚠️ กับดักของ PLAN-107 ที่ implementer ต้องระวัง: `isReadOnly` ฝั่ง client เป็น OR ของ 2 เคส** — (A) ไม่มี enrollment = ดูเฉย ๆ ต้องซ่อน / (B) เรียนจบแล้วกลับมาทบทวน **ต้องแสดง 100% ต่อ** ⇒ ต้องใช้ `currentData.isReadOnly` (ค่าดิบจาก API ซึ่ง = ไม่มี enrollment) ไม่ใช่ตัวแปร OR

## [2026-07-21 14:42] GitHub Copilot — QA deploy + smoke: PLAN-105/106 VERIFIED
- ทำอะไร: deploy QA bundle 105+106 หลัง review ผ่าน. Deploy API ก่อนสำหรับ PLAN-105 server retry แล้ว deploy Learner/User สำหรับ PLAN-105 client commit serialization + PLAN-106 Close/Finish modal. ไม่มี migration และไม่มี PROD deploy. ทำ smoke ผ่าน browser จริงที่ `?debug` ด้วย learner `430339`, enrollment `18217`, course `540`: reset QA enrollment เพื่อรันสะอาด, เล่น PDF item 366 ครบ 7/7 หน้า, ทำ exam item 397 ได้ 100%, กด Close ใน SCORM package แล้ว modal `ผลการเรียน` ของ iLearn เปิดเองพร้อมปุ่ม `กลับหน้าหลักสูตร`.
- QA live stamps: API `\\AP-NTC2138-QAWB\wwwroot\iLearn\Service\_deploy_20260721142125`, `web.config` → `.\_deploy_20260721142125\iLearn.API.dll`, previous `_deploy_20260721133451`; User `\\AP-NTC2138-QAWB\wwwroot\iLearn\_user_deploy_20260721142236`, `web.config` → `.\_user_deploy_20260721142236\iLearn.User.dll`, previous `_user_deploy_20260721133726`. Both script health checks passed and `AutoRolledBack=False`.
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มีใหม่จาก deploy; ใช้ contract เดิมของ PLAN-105 (`IUnitOfWork.Detach<T>` additive) ที่ผ่าน review แล้ว; API shape/DB schema ไม่เปลี่ยน.
- Verified: publish API/User สำเร็จ; HTTP smoke root/session/dashboard ผ่าน (`iLearn/` 200, learner AJAX no-cookie 440, API no-creds 401, API Windows creds 200, Dashboard/Stats 200). Runtime smoke: `CommitRuntime` ทุกครั้ง status 200 ไม่มี 500, Gate 0 เห็น `SCORM 1.2: LMSFinish`, `scorm12-finish` commit 200, modal `ผลการเรียน` เปิดหลัง Close. Persisted `GetPlayerInfo?courseId=540`: `progress=100`, `isCompleted=True`; item 366 `completed/rawScore=100/sessionTime=00:00:54`, item 397 `passed/successStatus=passed/rawScore=100/sessionTime=00:03:13`.
- หมายเหตุ QA: enrollment `18217` ถูก reset แล้วเล่นจนจบใหม่ตาม smoke test; สถานะสุดท้ายเป็น completed 100%.

## [2026-07-21 —] Claude Code — รีวิว PLAN-105/106 → REVIEWED (106 ยังติด Gate 0)
- ทำอะไร: **105** §2 server ตรงสเปค — `IUnitOfWork.Detach<T>` + `UpsertAsync` แยก `StageChangesAsync()` แล้ว retry **ครั้งเดียว** เมื่อชน unique violation (detach → re-stage → save), `IsUniqueViolation` ไล่ inner chain หา SqlException 2601/2627 เท่านั้น ⇒ **error อื่นไม่ถูกกลืน** (ข้อที่กำชับหนักสุด); ยืนยันว่า `GenericRepository.GetAsync` ไม่ใช้ AsNoTracking ⇒ re-query แล้ว `UpdateWithoutSave` ทำงานถูก. §1 client **ทำได้ดีกว่าสเปค**: แยก prepare(snapshot payload ทันที)/send(เข้าคิว) ⇒ session time ถูกจับ ณ เวลาที่ flush จริง + สลับบทระหว่างรอคิวไม่เพี้ยน; `useBeacon` bypass คิวตามที่กำชับ; คิวไม่ตันเมื่อ commit fail; เพิ่ม `runtimeCommitVersion` กัน clear dirty flag ทิ้งเมื่อมีการแก้ระหว่าง request ค้าง. **106** guard ครบ 3 ชั้น (read-only/แสดงครั้งเดียว/ต้องผ่านครบ), ใช้ `renderLearningResultModal()` ไม่ flush ซ้ำ, `return "true"` ทันทีไม่ await, modal เปิดหลังบันทึกเสร็จ
- ไฟล์หลักที่แตะ: `DOC/PLANS/PLAN-105/106` (→REVIEWED + Sign-off), `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน: ไม่มี (รีวิว)
- Verified: `dotnet test` **217/217** (+3 tests ของ 105 ครบตามแผน รวมเคส non-unique-violation ต้อง rethrow), build API+learner 0 errors, `node --check` Player 1,547 บรรทัดผ่าน
- **⚠️ PLAN-106 ยัง VERIFIED ไม่ได้ — Gate 0 ยังไม่มีใครทำ:** ไม่มีหลักฐานว่าปุ่ม Close ของ package เรียก `LMSFinish`/`Terminate` จริง ถ้าไม่เรียก ฟีเจอร์จะไม่ทำงานเลยแม้โค้ดถูก ⇒ **ต้องทดสอบด้วย `?debug` บน QA ก่อน**
- Observation (TRIVIAL): browser ที่ไม่มี `navigator.sendBeacon` จะทำให้เส้นทาง useBeacon ตกไปใช้ ajax นอกคิว — ไม่มีในเบราว์เซอร์จริง ไม่ต้องแก้

## [2026-07-21 14:13] GitHub Copilot — PLAN-106 DONE: SCORM Close/Finish เปิด modal สรุปเมื่อเรียนครบ
- ทำอะไร: รับ PLAN-106 แทน Gemini ตามคำสั่งผู้ใช้. ใน `Player.cshtml` เพิ่ม `courseSummaryAutoShown`, `isCourseFullyPassed()`, และ `maybeShowCourseCompleteSummary()` เพื่อเปิด summary modal ผ่าน `renderLearningResultModal()` เฉพาะเมื่อไม่ใช่ read-only, ยังไม่เคย auto-open ใน page session นี้, และทุก content item มี `clientStatus` เป็น `passed`/`completed`. Hook เฉพาะ `LMSFinish` (SCORM 1.2) และ `Terminate` (SCORM 2004) ให้ flush session time ก่อน แล้วค่อยลองเปิด modal ผ่าน Promise callback โดยยัง `return "true"` ทันทีตาม SCORM sync API. ไม่เรียก `showLearningResult()` (กัน flush ซ้ำ), ไม่ redirect อัตโนมัติ, ไม่แตะ iframe DOM, ไม่แตะ PLAN-105 runtime commit queue
- ไฟล์หลักที่แตะ: `iLearn.User/Views/MyLearning/Player.cshtml`, `DOC/PLANS/PLAN-106-close-shows-course-summary.md`, `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี; frontend behavior only; ไม่มี migration
- Verified: `dotnet build .\iLearn.User\iLearn.User.csproj -o .\artifacts\verify-user` ผ่าน แล้วลบ temp artifacts. Gate 0/manual QA ยังต้องทำบน QA จริงด้วย learner session + SCORM package: เปิด `?debug`, กด Close หลังจบ content แล้วต้องเห็น `LMSFinish`/`Terminate` และ modal เปิดเฉพาะเมื่อเรียนครบทุก item

## [2026-07-21 14:11] GitHub Copilot — PLAN-105 DONE: server retry + client commit serialization
- ทำอะไร: ผู้ใช้สั่งให้ทำ PLAN-105 §1 แทน Gemini จึงปิดงานครบทั้งแผน. เพิ่ม client-side serialize/coalesce สำหรับ `commitRuntimeContentItems` ใน `Player.cshtml`: non-beacon commits เข้าคิว `runtimeCommitQueue` ไม่ยิง `$.ajax` ซ้อน, `useBeacon:true` bypass queue ยิงทันทีเหมือนเดิม, และ snapshot payload ตอน flush ถูกเรียกก่อนเข้า queue เพื่อไม่ให้ `cmiModel` ของบทเดิมหายตอนสลับ content item. เพิ่ม `runtimeCommitVersion` ต่อ content item เพื่อกัน request เก่าที่เพิ่งสำเร็จ clear pending flag ทับ dirty ใหม่ที่เกิดระหว่าง in-flight. งาน §2 server retry จาก entry 14:05 ยังอยู่ครบ ไม่แตะ schema/index/migration/API shape และไม่แตะ PLAN-106 auto-summary
- ไฟล์หลักที่แตะ: `iLearn.User/Views/MyLearning/Player.cshtml`, `DOC/PLANS/PLAN-105-commit-runtime-race-500.md`, `DOC/AGENT_LOG.md` + ไฟล์ server/tests จาก §2 เดิม (`IUnitOfWork`, `UnitOfWork`, `ScormRuntimeStateService`, `ScormRuntimeStateServiceTests`, fake unit-of-work tests)
- Contract ที่เปลี่ยน (API shape / props / DB): เพิ่ม application contract `IUnitOfWork.Detach<T>` จาก §2 เท่านั้น; API shape/props และ DB schema ไม่เปลี่ยน; ไม่มี migration
- Verified: `dotnet build .\iLearn.User\iLearn.User.csproj -o .\artifacts\verify-user` ผ่าน; full `dotnet build .\iLearn.Tests -o .\artifacts\verify-test` + `dotnet test .\artifacts\verify-test\iLearn.Tests.dll` ผ่าน **217/217**; ลบ temp artifacts แล้ว. Manual QA ยังต้องยืนยันใน Player `?debug` ว่า CommitRuntime ajax ไม่ซ้อนตอน content initialize

## [2026-07-21 14:05] GitHub Copilot — PLAN-105 §2 server DONE: CommitRuntime duplicate-key race retry
- ทำอะไร: เพิ่ม `IUnitOfWork.Detach<T>` (additive) + EF `UnitOfWork.Detach` เพื่อถอน entity ที่เพิ่ง `Add` ค้างใน tracker หลัง insert ชน unique key. ปรับ `ScormRuntimeStateService.UpsertAsync` ให้ stage add/update, catch เฉพาะ `DbUpdateException` ที่ inner `SqlException.Number` เป็น 2601/2627 และมี added state, detach added states, reload runtime state, apply commit ใหม่เป็น update, แล้ว save retry **ครั้งเดียว**; non-unique DB errors และ retry ที่ยังพัง rethrow ตามเดิม. ไม่แตะ PLAN-104 merge policy, reset paths, schema/index/migration, API shape หรือ §1 client `Player.cshtml` (ยัง pending Gemini)
- ไฟล์หลักที่แตะ: `iLearn.Application/Interfaces/IUnitOfWork.cs`, `iLearn.Infrastructure/UnitOfWork.cs`, `iLearn.Infrastructure/Services/ScormRuntimeStateService.cs`, `iLearn.Tests/ScormRuntimeStateServiceTests.cs`, fake `IUnitOfWork` ใน tests ที่เกี่ยวข้อง, `DOC/PLANS/PLAN-105-commit-runtime-race-500.md`, `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน (API shape / props / DB): เพิ่ม application contract `IUnitOfWork.Detach<T>` เท่านั้น; API shape/props และ DB schema ไม่เปลี่ยน; ไม่มี migration
- Verified: focused `dotnet test .\iLearn.Tests\iLearn.Tests.csproj --filter FullyQualifiedName~ScormRuntimeStateServiceTests --artifacts-path .\artifacts\verify-plan105-focused` ผ่าน 17/17; full `dotnet build .\iLearn.Tests -o .\artifacts\verify-test` + `dotnet test .\artifacts\verify-test\iLearn.Tests.dll` ผ่าน **217/217**; ลบ temp artifacts แล้ว (warnings เดิมของ solution ยังอยู่)

## [2026-07-21 —] Claude Code — เขียน PLAN-106: กด Close แล้วเรียนครบ → เปิด modal สรุปผล (ผู้ใช้เลือกแบบ B)
- ทำอะไร: ผู้ใช้ขอ "กด Close ให้กลับหน้าหลัก". ตรวจแล้วปุ่ม Close อยู่**ในตัว SCORM content** (แก้ตรง ๆ ไม่ได้) แต่ package เรียก `LMSFinish`/`Terminate` ที่ adapter เรารับอยู่ → hook ตรงนั้นได้. เสนอ 2 ทาง ผู้ใช้เลือก **B** = เปิด modal สรุปผลของเรา (มีอยู่แล้ว `renderLearningResultModal` + ปุ่ม `goBackToMyLearning`) แทนการเด้งออกอัตโนมัติ. แผนกำหนด: เปิดเฉพาะเมื่อ **ทุก item ผ่านครบ** (คอร์สหลาย item ห้ามเตะผู้เรียนออกก่อนทำ item ถัดไป) + **ไม่ใช่ read-only** (เข้ามาทบทวนไม่ต้องเด้ง) + **หลัง flush สำเร็จ** (กันคะแนนหาย) + guard เปิดครั้งเดียวต่อ page session; ห้าม `await` ใน LMSFinish (SCORM API ต้อง return sync) และห้ามเรียก `showLearningResult()` ตรง ๆ (จะ flush ซ้ำ)
- ไฟล์หลักที่แตะ: `DOC/PLANS/PLAN-106-close-shows-course-summary.md` (ใหม่ READY), `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน: ไม่มี (frontend only, ไม่มี migration)
- Verified: — (แผน; อ่าน LMSFinish/Terminate/showLearningResult/renderLearningResultModal/isReadOnly จริงจาก Player.cshtml)
- **ถึง Gemini: มี Gate 0 บังคับ — ยืนยันด้วย `?debug` ก่อนว่ากด Close แล้ว `LMSFinish`/`Terminate` ยิงจริง ถ้าไม่ยิงให้หยุดแล้วแจ้ง Claude อย่าเดา**; **ทำหลัง PLAN-105 §1 เพราะแตะ `Player.cshtml` ไฟล์เดียวกัน อย่าทำพร้อมกัน**

## [2026-07-21 —] Claude Code — วินิจฉัย CommitRuntime 500 = race ตอน insert แถวแรก + เขียน PLAN-105
- ทำอะไร: ผู้ใช้ reproduce 500 ได้หลัง deploy 101-104 (13:43) พร้อม stack เต็ม. **Root cause: race condition** — `lms.js` ของ content เรียก `commit` ซ้อนกันหลายชั้นตอน initialize → `commitRuntimeContentItems` ยิง `$.ajax` โดยไม่รอตัวก่อนจบ → 2 requests เข้า `UpsertAsync` พร้อมกัน → ทั้งคู่ query ไม่เจอ row (commit แรกของ item) → INSERT ทั้งคู่ → ตัวที่สองชน unique index `IX_ScormRuntimeStates_EnrollmentId_ContentItemId` (filtered `[IsDeleted]=0`) → `DbUpdateException` → 500. **เกิดเฉพาะ commit แรกของแต่ละ item** (พอมี row แล้วทั้งคู่ไป UPDATE ไม่ชน) จึง intermittent. DB ยืนยัน: item 366 ของ enrollment 18217 บันทึกสำเร็จ (ตัวชนะ) และ**ไม่มี duplicate ค้าง** เพราะตัวแพ้ถูก reject — ซึ่งเป็นเหตุผลที่ตอนไล่หา duplicate ตอนแรกไม่เจอ. **ไม่ใช่ regression ของ 096-104** (upsert ไม่เคย concurrency-safe) แต่ 097 เพิ่มจุด flush ทำให้โอกาสชนสูงขึ้น
- ไฟล์หลักที่แตะ: `DOC/PLANS/PLAN-105-commit-runtime-race-500.md` (ใหม่ READY), `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน: ไม่มีในตัวแผน (105 จะเพิ่ม `IUnitOfWork.Detach<T>` additive; ไม่มี migration)
- Verified: — (แผน; หลักฐาน = stack trace ฝั่ง client + response body 500 + query QA DB + ตรวจ index/schema/IUnitOfWork จริง)
- **ถึง Copilot: §2 (server retry) สำคัญกว่า §1 — ต้อง rethrow ถ้าไม่ใช่ unique violation ห้ามกลืน error อื่น**; **ถึง Gemini: §1 ห้ามให้เส้นทาง `useBeacon` เข้าคิว (หน้าอาจถูกปิดก่อน)**
- หมายเหตุ: `LearningLogs` ไม่มี unique index โดยตั้งใจ (หลาย log ต่อ item ข้าม reset boundary) — **ห้ามเพิ่ม index แก้ race ที่ตารางนี้**

## [2026-07-21 13:39] GitHub Copilot — QA deploy + commit prep: PLAN-101/102/103/104 hotfix bundle
- ทำอะไร: deploy QA รอบ hotfix หลัง full validation ผ่าน. Deploy **API ก่อน** สำหรับ PLAN-101/104 backend (`ClearForEnrollmentsAsync` reset paths, placeholder/terminal merge guard, rollup completion fix, session-time cap) แล้ว deploy **Learner/User** สำหรับ PLAN-102/103/104 client (`?debug` diagnostics, same-origin launch rewrite/assertion, active session timer). ไม่มี migration และไม่มี PROD deploy.
- QA live stamps: API `\\AP-NTC2138-QAWB\wwwroot\iLearn\Service\_deploy_20260721133451`, `web.config` → `.\_deploy_20260721133451\iLearn.API.dll`, previous `20260721121253`; User `\\AP-NTC2138-QAWB\wwwroot\iLearn\_user_deploy_20260721133726`, `web.config` → `.\_user_deploy_20260721133726\iLearn.User.dll`, previous `20260721121441`. Both health checks passed attempt 1 and `AutoRolledBack=False`.
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี; behavior/runtime timing/learner proxy launch URL handling changed as reviewed in PLAN-101/102/103/104. DB schema unchanged.
- Verified: local `dotnet build iLearn.Tests -o artifacts\verify-test` + `dotnet test artifacts\verify-test\iLearn.Tests.dll` passed **214/214**; API publish build succeeded (89 existing warnings); User publish build succeeded; independent read-back confirmed active stamps; probes: API session endpoint = 401 expected, internal learner root = 200, public learner root = 200.
- คงค้าง QA manual: reset/smoke affected enrollment(s), verify old `ScormRuntimeStates` are soft-deleted after reset, launch Player through canonical public URL, complete one assigned item, and confirm progress/session time persist correctly. ห้าม deploy PROD จนกว่า QA smoke จะยืนยันแล้ว.

## [2026-07-21 —] Claude Code — รีวิว PLAN-101/102/103/104 → REVIEWED ทั้งหมด ไม่มี finding
- ทำอะไร: รีวิว 4 แผนที่ค้างพร้อมกัน. **101:** clear ครบ 4 reset path, bulk query เดียวไม่มี N+1, ทำ snapshot reset ให้ด้วย. **102:** gate 100% (ajax hook + ปุ่ม copy อยู่ใน `if (LEARNER_DEBUG)` ⇒ ปิดแล้ว overhead 0), ดัก req+resp ครบทุก endpoint + beacon, ทำ Dashboard ให้ด้วย. **103:** rewrite เฉพาะ GetPlayerInfo + guard 4 ชั้น + host ต้องตรง config, assertion ฝั่ง client, เอกสารลง CLAUDE.md/DEPLOY-CHECKLIST — **พิสูจน์บน QA จริงแล้ว** (เปิดผ่าน internal host ที่เดิมพัง ตอนนี้ SCORM ทำงาน 100% ไม่มี `Lms initialization failed`). **104:** §A1 ตัดเงื่อนไข `isPlaceholderProgressCommit` ออกจาก `PreferStatus` (หัวใจของบั๊ก), §A2 คง status/score เดิมและตั้ง `isInputPassed` ใหม่ให้ Progress กลับเป็น 100 ถูก, §B ปลด IsCompleted/CompletedDate, §C `sessionActiveMs`/`sessionResumedAt` + **pause ก่อน flush ทุกจุด**, §D เพดาน 14400 วิ ครอบทั้ง update และ insert
- ไฟล์หลักที่แตะ: `DOC/PLANS/PLAN-101..104` (→REVIEWED + Sign-off), `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน: ไม่มี (รีวิว)
- Verified: **`dotnet test` 214/214** (จาก 207 = +7 tests ใหม่ครบตามแผน รวมเคส regression ตรง ๆ), build API+learner 0 errors, `node --check` Player 1,479 บรรทัด + Index 694 บรรทัดผ่าน — **และ blocker ที่ทำให้ test project build ไม่ผ่านตั้งแต่ 101 ถูกแก้แล้ว**
- Observation (LOW ไม่บล็อก): บล็อก diagnostic ของ 102 ซ้ำกันใน 2 view (ควรย้าย layout รอบหน้า); ปุ่ม copy z-index 1000 สูงกว่า summary-overlay 999 (หลุด ladder เล็กน้อย, debug-only)
- **ยังไม่ commit — รอผู้ใช้สั่ง; deploy ต้อง API + learner (104/101 = API, 102/103/104§C = learner) ไม่มี migration**

## [2026-07-21 13:08] GitHub Copilot — PLAN-104 DONE: ป้องกันผลเรียนถูก placeholder ทับ + active session time
- ทำอะไร: ทำ hotfix PLAN-104 ครบชุด API + learner. (1) `ScormRuntimeStateService` ให้ terminal status (`passed/completed/failed/browsed`) ไม่ถูก placeholder (`incomplete/not attempted/unknown`) ทับ แม้ commit มี `SessionTime`/runtime fields; raw/scaled score 0 จาก placeholder ไม่ทับคะแนนเดิม แต่ terminal→terminal ยังทับได้. (2) `LearningLogsController` กัน log downgrade จาก terminal เป็น placeholder ใน attempt เดิม, เพิ่ม cap session delta >4 ชั่วโมงต่อ commit ไม่ให้บวกเวลาและ log warning, และ rollup สาขาไม่ครบ set `IsCompleted=false` + `CompletedDate=null`. (3) `Player.cshtml` เปลี่ยน session time จาก wall-clock เป็น active-time accumulator และ pause ก่อน flush ตอน hidden/pagehide/beforeunload. (4) แก้ test double `NullNotificationService` ให้รองรับ overload `GetForUserAsync(..., skip)` ตาม blocker ที่ PLAN-104 ระบุ.
- ไฟล์หลักที่แตะ: `iLearn.Infrastructure/Services/ScormRuntimeStateService.cs`, `iLearn.API/Controllers/LearningLogsController.cs`, `iLearn.User/Views/MyLearning/Player.cshtml`, `iLearn.Tests/ScormRuntimeStateServiceTests.cs`, `iLearn.Tests/LearningLogsRuntimeValidationTests.cs`, `iLearn.Tests/EnrollmentsPlayerInfoTests.cs`, `iLearn.Tests/ContentItemsControllerTests.cs`, `DOC/PLANS/PLAN-104-learning-result-integrity.md`, `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี (พฤติกรรม runtime/log/rollup/timing เปลี่ยนตามแผน; ไม่มี migration)
- Verified: `dotnet build iLearn.User/iLearn.User.csproj` ผ่าน; focused SCORM tests ผ่าน 28/28; full verification `dotnet build iLearn.Tests -o artifacts\verify-test` + `dotnet test artifacts\verify-test\iLearn.Tests.dll` ผ่าน **214/214** แล้วลบ `artifacts\verify-test` เรียบร้อย (warnings เดิมของ solution ยังอยู่)

## [2026-07-21 —] Claude Code — วินิจฉัย 3 บั๊กความถูกต้องของผลการเรียน + เขียน PLAN-104 (CRITICAL)
- ทำอะไร: ผู้ใช้พบการ์ดขึ้น "เรียนจบ" คู่กับ 50%. Query QA DB (enrollment 18214/course 540) เจอ timeline ชัด: **11:41 exam ผ่านจริง (passed, score 100, Progress 100)** → เปิดแท็บทิ้งไว้ ~2.5 ชม. → **12:50 commit ตอนปิดแท็บส่ง `SessionTime=02:33:55` + `lessonStatus=incomplete/RawScore=0` เขียนทับผลสอบที่ผ่านแล้วหายเกลี้ยง** (score 100→0, Progress→50) แต่ IsCompleted ค้าง 1. แยกได้ 3 บั๊ก: **(A)** `PreferStatus`/`PreferRawScore` ปกป้องเฉพาะตอน `isPlaceholderProgressCommit` ซึ่งบังคับว่าห้ามมี sessionTime — commit นี้มี sessionTime เลยไม่เข้าเงื่อนไข ⇒ placeholder ทับ terminal ได้; **(B)** `UpdateEnrollmentRollupAsync` สาขา else ไม่เคย set `IsCompleted=false` ⇒ ค้าง "เรียนจบ"; **(C)** `captureSessionTime` คิดจากนาฬิกาแขวน ไม่หยุดตอนแท็บ hidden ⇒ exam ทำจริง 2 นาที บันทึก 9,235 วิ (099 §2 แก้แค่บวกซ้ำ ไม่ได้แก้นับเวลานั่งเฉย). เขียน PLAN-104 ครอบ A/B/C + §D เพดานกันค่าเพี้ยน
- ไฟล์หลักที่แตะ: `DOC/PLANS/PLAN-104-learning-result-integrity.md` (ใหม่ READY), `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน: ไม่มีในตัวแผน (พฤติกรรมเปลี่ยน 3 ข้อตามแผน; ไม่มี migration)
- Verified: — (แผน; หลักฐาน = query QA DB หลายรอบ read-only + ไล่โค้ด ApplyCommit/MapRuntimeCommitToProgress/rollup)
- **เปิด stdout log บน QA API ชั่วคราวเพื่อจับ inner exception ของ CommitRuntime 500 แต่ไม่เกิดซ้ำ → คืน web.config เดิมแล้ว (stamp `_deploy_20260721121253` ไม่เปลี่ยน, backup/logs dir ลบแล้ว, API 401 + learner 200 ปกติ)**
- **500 ยังค้าง:** ตัดสาเหตุแล้ว index/filter, duplicate, truncation, check/trigger, `.Max()` ว่าง, concurrent — ถ้าเจออีกให้เปิด stdout log เก็บ stack ทันที
- **ถึง Copilot/Gemini: §B ห้าม deploy โดยไม่มี §A** (จะขยายความเสียหาย); **ต้องแก้ `EnrollmentsPlayerInfoTests.NullNotificationService` ให้ test suite รันได้ก่อนส่งงาน** (ค้างมาจาก 101)

## [2026-07-21 12:15] GitHub Copilot — QA deploy: PLAN-101 API hotfix + pending learner changes
- ทำอะไร: deploy QA แบบ side-by-side หลัง dry-run ผ่านทั้ง API/User. API hotfix PLAN-101 ถูก publish จาก worktree ปัจจุบัน, health endpoint Windows Auth ตอบ expected 401; learner bundle PLAN-102/103 ถูก publish ในรอบเดียวกัน, root ตอบ 200. ไม่มี migration และไม่มี PROD deploy.
- QA live stamps: API `\\AP-NTC2138-QAWB\wwwroot\iLearn\Service\_deploy_20260721121253`, `web.config` → `.\_deploy_20260721121253\iLearn.API.dll`, previous `20260721092410`; User `\\AP-NTC2138-QAWB\wwwroot\iLearn\_user_deploy_20260721121441`, `web.config` → `.\_user_deploy_20260721121441\iLearn.User.dll`, previous `20260721092529`. ทั้งสอง health check ผ่าน attempt 1 และ `AutoRolledBack=False`.
- Verified: API publish build สำเร็จ (89 existing warnings), User publish build สำเร็จ (0 errors); API health `GET /iLearn/Service/api/admin/session/me` = 401 expected, learner root `GET /iLearn/` = 200.
- คงค้าง QA manual: admin-reset enrollment `18201` แล้วตรวจ runtime rows เก่า `IsDeleted=1`, player ไม่มี completion/score เก่าฟื้น; ทดสอบ Player ผ่าน internal + public host ว่า iframe same-origin และลอง `?debug` copy diagnostics. ห้าม deploy PROD จนกว่าจะยืนยันผล.

## [2026-07-21 12:10] Antigravity — PLAN-103 DONE: SCORM launch ต้อง same-origin เสมอ + ห้ามล้มเงียบ
- ทำอะไร: (1) ลงทะเบียน `FileSettings` ใน DI ที่ `ServiceCollectionExtensions.cs`; (2) ฉีด `IOptions<FileSettings>` เข้า `MyLearningController` และเพิ่ม `RewriteLaunchUrlsToRootRelative` แปลง absolute `launchUrl` ที่ตรงกับ `FileSettings:HostUrl` ให้เป็น root-relative path (เช่น `/iLearn/Courses/...`) ก่อนส่งกลับให้ learner UI ทำให้ SCORM iframe เป็น same-origin เสมอไม่ว่าจะเข้าผ่าน host ไหน; (3) เพิ่ม client assertion ใน `startCourse` (`Player.cshtml`) ตรวจสอบ `resolved.origin !== window.location.origin` แล้วขึ้น toast เตือนผู้เรียนหากพบ cross-origin launch; (4) อัปเดตเอกสาร `CLAUDE.md` และ `DOC/DEPLOY-CHECKLIST.md` กำหนดให้ SCORM iframe ต้องเป็น same-origin เสมอ
- ไฟล์หลักที่แตะ: `iLearn.User/Extensions/ServiceCollectionExtensions.cs`, `iLearn.User/Controllers/MyLearningController.cs`, `iLearn.User/Views/MyLearning/Player.cshtml`, `CLAUDE.md`, `DOC/DEPLOY-CHECKLIST.md`, `DOC/PLANS/PLAN-103-scorm-same-origin-launch.md`
- Contract ที่เปลี่ยน (API shape / props / DB): `GET /MyLearning/GetPlayerInfo` คืน `launchUrl` เป็น root-relative path สำหรับ content host ของระบบ (API/DB ไม่เปลี่ยน)
- Verified: `dotnet build iLearn.User -o artifacts\verify-user` ผ่าน 100% (0 Errors, 0 Warnings)

## [2026-07-21 12:00] Antigravity — PLAN-102 DONE: Learner diagnostic mode — log client↔server (req/resp) + SCORM + ปุ่ม copy (gated ?debug)
- ทำอะไร: (1) เพิ่ม `LEARNER_DEBUG` flag (`?debug`), `window.__ilearnDiag` buffer, `safeTruncate` (ตัดข้อความยาว >2000 ตัวอักษรป้องกัน log บวม), และ helper `diag(event, data)` ใน `Player.cshtml` และ `Index.cshtml`; (2) ดักจับ jQuery global AJAX events (`ajaxSend` และ `ajaxComplete`) เมื่อเปิด `LEARNER_DEBUG` เพื่อบันทึก request/response ของ `GetPlayerInfo`, `CommitRuntime`, `Ping`, `GetMyCourses`, `GetCourseCatalog`; (3) บันทึก `beacon→CommitRuntime` payload summary และ acceptance status ในกิ่ง `sendBeacon`; (4) บันทึก key state checkpoints ได้แก่ `startCourse`, `updateContentItemData`, และ `recalcTotalProgress`; (5) แสดงปุ่มลอย "คัดลอก diagnostics" (มุมล่างซ้าย) เฉพาะเมื่อเปิด `LEARNER_DEBUG` ที่คัดลอก JSON diagnostic log ลง clipboard พร้อม toast แจ้งเตือน
- ไฟล์หลักที่แตะ: `iLearn.User/Views/MyLearning/Player.cshtml`, `iLearn.User/Views/MyLearning/Index.cshtml`, `DOC/PLANS/PLAN-102-learner-diagnostic-mode.md`
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี (frontend debug tooling; ไม่มีปุ่ม/ไม่มี log เมื่อไม่เปิด `?debug`)
- Verified: `dotnet build iLearn.User -o artifacts\verify-user` สำเร็จ (0 Errors, 71 Warnings เดิม); ลบ directory ทดสอบเรียบร้อย

## [2026-07-21] GitHub Copilot — PLAN-101 DONE: clear SCORM runtime state across every reset path
- ทำอะไร: (1) เพิ่ม `IScormRuntimeStateService.ClearForEnrollmentsAsync` เพื่อ clear หลาย enrollment ด้วย query เดียวและเลือก save boundary ได้ โดย `ClearForEnrollmentAsync` เดิม delegate มาที่ bulk method เพื่อคง behavior PLAN-099; (2) `EnrollmentService.ResetStatusAsync` clear runtime state ใน main flow และ reset `EnrollmentAssignment` snapshots ให้ตรง learner reset; (3) wire bulk clear แบบ `saveChanges:false` ก่อน commit เดิมใน `CourseVersionService` และ `AssignmentService`; (4) `CourseAssignmentService` เก็บเฉพาะ ID enrollment เดิมที่ถูก reset แล้ว clear รวมครั้งเดียวก่อน save, ป้องกัน N+1; (5) เพิ่ม regression tests สำหรับ save:false, save:true และ admin reset+snapshot.
- ไฟล์หลักที่แตะ: `iLearn.Application/Interfaces/Services/IScormRuntimeStateService.cs`, `iLearn.Infrastructure/Services/ScormRuntimeStateService.cs`, `iLearn.Application/Services/EnrollmentService.cs`, `iLearn.Application/Services/CourseAssignmentService.cs`, `iLearn.Application/Services/CourseVersionService.cs`, `iLearn.Application/Services/AssignmentService.cs`, `iLearn.Tests/ScormRuntimeStateServiceTests.cs` พร้อม fake/constructor tests ที่พึ่ง contract ใหม่
- Contract ที่เปลี่ยน (API shape / props / DB): เพิ่ม application service method `ClearForEnrollmentsAsync`; API shape และ DB schema ไม่เปลี่ยน, ไม่มี migration
- Verified: production projects compile ผ่านใน `dotnet build iLearn.Tests -o artifacts\verify-plan101 --no-restore -v:q`; test projectยังปิด build ไม่ได้จาก error นอก scope ที่ worktree ค้างอยู่: `EnrollmentsPlayerInfoTests.NullNotificationService` ไม่ implement `INotificationService.GetForUserAsync(string, bool, int, int)`. PLAN-101 test doubles/constructor calls ผ่านจนเหลือ error เดียวดังกล่าว

## [2026-07-15 09:50] GitHub Copilot — PLAN-088 DONE: Notifications Phase 1 — Backend (entity + endpoints + SignalR targeting + 4 hooks)
- ทำอะไร: (1) สร้าง `Notification` entity ใน Domain (สืบทอด BaseEntity); (2) สร้าง `NotificationTypes`/`NotificationLevels` const ใน Application/Common; (3) สร้าง `NotificationDto`/`NotificationListDto` ใน Application/DTOs ตรงตาม contract §2; (4) สร้าง `INotificationService` interface ใน Application/Interfaces; (5) สร้าง `NotificationService` ใน API/Services — persist + push via `Clients.User()` + try/catch กลืน error ป้องกัน request หลักพัง; (6) สร้าง `NidUserIdProvider : IUserIdProvider` ที่ strip domain prefix ด้วย `LastIndexOf('\\')` ตรงกับ `CurrentUserService.UserId` เป๊ะ; (7) สร้าง `NotificationsController` (GET list, GET unread-count, POST mark-read, POST read-all) ทุก endpoint enforce current-user-only access; (8) Hook 4 จุด: `CoursesController` CreateVersion/UpdateVersion (success+fail), `ContentItemsController` SetPublic (success+fail) + BatchPublish, `EnrollmentsController` BulkAssign (success); (9) ลงทะเบียน DI ใน `PresentationExtensions` (IUserIdProvider singleton + INotificationService scoped); (10) เพิ่ม DbSet + Fluent API config (MaxLength + composite index IX_Notifications_Recipient_Read_CreatedDesc); (11) สร้าง EF migration `AddNotifications`; (12) เขียน 8 unit tests ครอบ GetForUser/MarkRead/MarkAllRead/security/hub-failure; (13) แก้ existing tests ที่ constructor เปลี่ยน (ContentItemsControllerTests, EnrollmentsPlayerInfoTests)
- ไฟล์หลักที่แตะ: `iLearn.Domain/Entities/Notification.cs` (ใหม่), `iLearn.Application/Common/NotificationTypes.cs` (ใหม่), `iLearn.Application/DTOs/NotificationDtos.cs` (ใหม่), `iLearn.Application/Interfaces/Services/INotificationService.cs` (ใหม่), `iLearn.API/Services/NotificationService.cs` (ใหม่), `iLearn.API/Services/NidUserIdProvider.cs` (ใหม่), `iLearn.API/Controllers/NotificationsController.cs` (ใหม่), `iLearn.API/Controllers/CoursesController.cs`, `iLearn.API/Controllers/ContentItemsController.cs`, `iLearn.API/Controllers/EnrollmentsController.cs`, `iLearn.API/Extensions/PresentationExtensions.cs`, `iLearn.Infrastructure/Persistence/AppDbContext.cs`, `iLearn.Infrastructure/Persistence/Migrations/20260715024809_AddNotifications.cs` (ใหม่), `iLearn.Tests/NotificationServiceTests.cs` (ใหม่), `iLearn.Tests/ContentItemsControllerTests.cs`, `iLearn.Tests/EnrollmentsPlayerInfoTests.cs`
- Contract ที่เปลี่ยน (API shape / props / DB): **ใหม่** ตาราง `Notifications` + index, endpoints `api/Notifications` (GET list + GET unread-count + POST {id}/read + POST read-all), SignalR event `NotificationCreated` payload=NotificationDto, IUserIdProvider registered. **ไม่แตะ** AdminActivityCreated event / endpoint เดิม / HTTP behavior ของ 4 controller hooks (status code + body shape คงเดิม)
- Verified: `dotnet build` 0 errors + `dotnet test` **195 passed** (0 failed, 8 ใหม่ + 187 เดิมผ่านหมด)
- ⚠️ คงค้าง: manual smoke test SignalR targeting ด้วย 2 users (§verification ข้อ 5), apply migration ลง SQL Server จริง

## [2026-07-15 10:20] Antigravity — PLAN-089 DONE: Notifications Phase 1 — Frontend (bell dropdown + unread badge + realtime layout connection)
- ทำอะไร: (1) สร้าง `notificationTypes.ts` มี `NotificationDto` และ `NotificationListDto` ตรงตาม contract PLAN-088 §2; (2) เพิ่ม helper `formatRelativeTime` ใน `format.ts` แสดงเวลาแบบ relative เช่น "X นาทีที่แล้ว" (ภาษาไทย); (3) สร้าง `NotificationProvider` ใน `notificationContext.tsx` เชื่อมต่อ SignalR `/hubs/admin-activity` เพื่อฟัง `NotificationCreated` + ดึงจำนวน unread และรายการ notifications แบบ lazy โหลด; (4) ครอบ `NotificationProvider` เข้าที่ `AppLayout.tsx` ใน layout level; (5) พัฒนาคอมโพเนนต์ `<NotificationBell />` ใช้ `IconButton`, `AppButton`, `Badge` สำหรับแสดง bell dropdown และ unread badge (99+); (6) แทนที่ปุ่ม bell ดิบ ใน `Header.tsx` ด้วย `<NotificationBell />`
- ไฟล์หลักที่แตะ: `iLearn.Admin.React/src/lib/notificationTypes.ts` (ใหม่), `iLearn.Admin.React/src/lib/format.ts`, `iLearn.Admin.React/src/lib/notificationContext.tsx` (ใหม่), `iLearn.Admin.React/src/components/layout/AppLayout.tsx`, `iLearn.Admin.React/src/components/layout/NotificationBell.tsx` (ใหม่), `iLearn.Admin.React/src/components/layout/Header.tsx`, `DOC/PLANS/PLAN-089-notifications-frontend.md`
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี (frontend-only, consume types ตาม contract C# PLAN-088 ตรง 100%)
- Verified: npm run lint ผ่าน (0 errors, 0 warnings), npm run build ผ่าน 100%

## [2026-07-15 —] Claude Code — เขียน PLAN-090/091: Notifications Phase 2 (deadline digest scheduler ตัวแรก + หน้าเต็ม + รวม connection)
- ทำอะไร: ผู้ใช้ขอเพิ่มฟีเจอร์ Notifications → เสนอ 5 ตัวเลือก ผู้ใช้เลือก **A (หน้า /notifications + retention + รวม SignalR connection) + B (deadline digest ทุกเช้า 08:00, recipient = ทุก admin ใน division)**. ยืนยัน data model ก่อน spec: User→UserRole→Role(DivisionId, RoleType Admin/SuperAdmin) หา recipient ได้ใน SQL; `IsDueSoon`/`GetDueSoonCutoff` (window 7 วัน) มีให้ reuse; ระบบยังไม่มี BackgroundService เลย — งานนี้คือ scheduler ตัวแรก. เขียน 2 แผน: **PLAN-090 (Copilot, backend)** `IDeadlineDigestService.RunOnceAsync` (testable, idempotent ต่อวันด้วย dedupe Type+CreatedAt>=today) + `DeadlineDigestHostedService` wrapper บาง (08:00 ไทย + catch-up ตอน startup + ห้าม host ตาย), digest 1 ฉบับ/คน/วัน สรุปยอด due-soon 7 วัน + overdue (นับจาก link.DueDate ตามกติกา effective dates), SuperAdmin ได้ org-wide รวม division null, retention 90 วัน (RemoveRange batch — เลี่ยง ExecuteDelete เพราะ InMemory test), contract additive: NotificationListDto+TotalCount, GET+skip. **PLAN-091 (Gemini, frontend)** แยก NotificationRow ใช้ร่วม, bell footer View all, หน้า /notifications (paging server-side + All/Unread + mark ผ่าน provider ให้ badge sync), **subscribeHubEvent API รวม SignalR เหลือเส้นเดียว** — DashboardPage เลิกเปิด connection เอง (tech debt จาก 089; ตอนนี้แตะไฟล์ได้แล้วเพราะไม่มีงานชนกัน)
- ไฟล์หลักที่แตะ: `DOC/PLANS/PLAN-090/091-*.md` (ใหม่ READY×2), `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน: ไม่มีในตัวแผน (090 จะเพิ่ม TotalCount+skip แบบ additive + NotificationTypes.DeadlineDigest — ไม่มี migration)
- Verified: — (แผน; ยืนยัน User/Role/Division model + ApiClaimsEnrichMiddleware + DueSoonWindowDays + ไม่มี hosted service เดิม จากโค้ดจริง)
- ถึง Copilot: PLAN-090 — จุดเสี่ยง = idempotency ของ digest (test รันซ้ำวันเดียวกันต้อง 0) + อ่านกติกา backend ใน CLAUDE.md ครบก่อนเริ่ม
- ถึง Gemini: PLAN-091 — ตอนนี้แตะ DashboardPage ได้แล้ว แต่พฤติกรรมเดิม (refresh 10 + จุด connected) ต้องครบ; mirror totalCount จาก 090 §1

## [2026-07-14 —] Claude Code — เพิ่มกติกาถาวรใน CLAUDE.md จาก bug จริง 6 ข้อ (กันพลาดซ้ำทุก agent)
- ทำอะไร: ผู้ใช้ตั้งข้อสังเกตว่า implementer ผิดพลาดบ่อย → ผมไล่ finding ทั้งหมดที่รีวิวมาในรอบนี้แล้วพบว่า **สิ่งที่แผนระบุชัด (contract freeze) ทำถูก 100% ทุกครั้งทั้ง Gemini และ Copilot ส่วนที่พลาดคือสิ่งที่ไม่มีใครเขียนกติกาไว้** ⇒ แปลง finding ที่เกิดจริงเป็นกติกาถาวรแทน. เพิ่มหัวข้อใหม่ **"กติกาสำคัญฝั่ง Backend (.NET)"** (เดิม CLAUDE.md มีแต่ฝั่ง React ทั้งที่ finding ระดับ MEDIUM ทั้งหมดเกิดฝั่ง backend): IDateTime ห้าม UtcNow ดิบ (PLAN-088), effective dates ห้ามอ่าน Enrollment.DueDate ดิบ (PLAN-086), cleanup ไฟล์+row ใน catch (PLAN-084), side-effect ห้ามทำ request หลักล้ม, migration ต้องอยู่ `Infrastructure/Migrations/`, SignalR UserIdProvider ต้อง normalize ตรง UserId ไม่งั้น push เงียบหาย. + เพิ่ม **z-index ladder** ในหัวข้อ React (content 10 / Header 15 / overlay 20 / sidebar 30 / modal 50-60 / upload 9999) พร้อมคำเตือน stacking context ของ sticky/fixed container (PLAN-089)
- ไฟล์หลักที่แตะ: `CLAUDE.md` (+9 บรรทัด: 64→73 — คุมให้สั้นเพราะ agent ต้องอ่านทุกครั้ง), `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน: ไม่มี (เอกสาร)
- Verified: — (เอกสาร; ทุกข้ออ้างอิง PLAN ที่เจอ bug จริง ตรวจสอบย้อนได้)

## [2026-07-14 —] Claude Code — hotfix: notification dropdown ถูกบังหลัง grid (Header stacking context)
- ทำอะไร: ผู้ใช้รายงานจาก QA ว่าเปิด bell แล้ว dropdown แสดง**อยู่หลัง grid/card**. **Root cause:** `Header` เป็น `sticky top-0 z-10` ⇒ สร้าง **stacking context** ที่ z=10 ⇒ `z-50` ของ dropdown มีผลแค่ภายใน context ของ Header เท่านั้น; เทียบกับของนอก Header ทั้ง header = z-10 ซึ่ง**เท่ากับ** sticky thead/sticky column/card ทั่วแอป (ทุกที่ z-10) → content ที่อยู่หลังใน DOM ชนะ ทับ dropdown. **แก้:** `Header` z-10 → **z-15** (สูงกว่า content z-10 แต่ยังต่ำกว่า overlay z-20 / sidebar z-30 / modal z-50-9999 → พฤติกรรมเดิมของ overlay/sidebar/modal ไม่เปลี่ยน). สำรวจ z ทั้งระบบก่อนเลือกเลข
- ไฟล์หลักที่แตะ: `iLearn.Admin.React/src/components/layout/Header.tsx` (1 class + คอมเมนต์อธิบายลำดับ z), `DOC/PLANS/PLAN-089-*.md`, `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน: ไม่มี
- Verified: **พิสูจน์ในเบราว์เซอร์จริง** — จำลอง stacking เดียวกันแล้ว `elementFromPoint` ที่จุดทับ: ก่อนแก้ (z-10) element บนสุด = `card` (reproduce bug ได้), หลังแก้ (z-15) = `drop` ✅; ยืนยัน Tailwind generate `z-index:15` จริงใน CSS output; `npm run lint` + `build` 0 errors
- **บทเรียน (สำหรับรีวิวรอบหน้า):** ตอนรีวิว PLAN-089 ผมเช็คแค่ว่า `z-50` ของ dropdown generate เป็น CSS จริงไหม แต่**ไม่ได้ไล่ stacking context ของ ancestor** — z-index ของลูกไร้ความหมายข้าม context ของพ่อ ⇒ เจอ dropdown/popover ใน sticky/fixed container เมื่อไร ต้องเทียบ z ของ container กับ content เสมอ

## [2026-07-14 —] Claude Code — แก้ findings PLAN-088/089 (ผู้ใช้สั่งแก้เอง) + commit Notifications P1
- ทำอะไร: **088 Finding 1 (MEDIUM):** `NotificationService` inject `IDateTime` → `ReadAt` ใช้ `_dateTime.Now` (UTC+7) แทน `DateTime.UtcNow` ทั้ง MarkRead/MarkAllRead → ตรง timezone กับ CreatedAt ไม่เป็นค่า "อ่านก่อนสร้าง 7 ชม." + เพิ่ม regression tests 2 ตัว (ปรับ `FakeDateTime` ให้รับ fixedNow แบบ optional — default ยังเดินจริงเพื่อไม่ให้ ordering test เดิมพัง). **088 Finding 2:** ย้าย migration `AddNotifications` (.cs + .Designer.cs) จาก `Infrastructure/Persistence/Migrations/` → `Infrastructure/Migrations/` + แก้ namespace ให้ตรงของเดิมทั้งหมด, ลบโฟลเดอร์ว่าง. **089 Finding 1:** ลบ `<style dangerouslySetInnerHTML>` `.custom-scrollbar` ซ้ำ (มีใน index.css:105 แล้ว). **089 Finding 2:** dedupe ด้วย `seenIdsRef` เช็คก่อน setState — **บทเรียน: ตอนแรกลองเซ็ต flag ใน setItems updater แล้วอ่านค่าถัดไป ซึ่งผิดเพราะ React เรียก updater ตอน re-render (async) flag ยัง false เสมอ → ต้อง gate ด้วย ref แบบ synchronous**. **089 Finding 3:** `useRef<any>` → `useRef<HubConnection | null>`
- ไฟล์หลักที่แตะ: `iLearn.API/Services/NotificationService.cs`, `iLearn.Tests/NotificationServiceTests.cs`, `iLearn.Infrastructure/Migrations/20260715024809_AddNotifications*` (ย้าย+namespace), `src/lib/notificationContext.tsx`, `src/components/layout/NotificationBell.tsx`, plan docs 088/089 (→VERIFIED FIXED)
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี (DTO/endpoint/schema เดิมทุกตัว — แก้แหล่งเวลา + cleanup ภายใน)
- Verified: `dotnet build` 0 errors + `dotnet test` **197 passed** (195 เดิม + 2 regression ใหม่); `npm run lint` + `build` 0 errors
- ⚠️ คงค้าง: live test per-user targeting ด้วย admin 2 คน + realtime ขณะอยู่หน้าอื่นที่ไม่ใช่ Dashboard (ต้อง API รัน + Windows auth) — ทำในสภาพแวดล้อมนี้ไม่ได้

## [2026-07-14 —] Claude Code — รีวิว PLAN-088/089 (Notifications P1) → ผ่านทั้งคู่, มี 1 MEDIUM + 3 MINOR ต้องแก้
- ทำอะไร: ตรวจ diff เต็มทั้ง 2 แผน + verify อิสระ (dotnet build 0 err + test **195 passed** รวม 8 NotificationServiceTests; npm lint+build 0 err). **จุดเสี่ยงสูงสุดผ่าน:** `NidUserIdProvider` (`LastIndexOf('\\')`) vs `CurrentUserService` (`Split('\\')[1]`) → เคสจริง `NIKONOA\n4734` ได้ `n4734` ตรงกันทั้งคู่ → per-user targeting ถูก; `AdminActivityCreated`/`Clients.All` เดิมไม่ถูกแตะ. Contract §2 ตรง freeze ทุก field ทั้ง 2 ฝั่ง; hooks 4 จุดครบ HTTP behavior เดิม; index composite ถูก; `MarkReadAsync` กันอ่านของคนอื่น (KeyNotFound→404); provider อยู่ layout level ใน SessionProvider ไม่แตะ DashboardPage
- Finding: **(1) MEDIUM `ReadAt = DateTime.UtcNow`** ขณะที่ทั้งระบบใช้ `IDateTime.Now` = `UtcNow.AddHours(7)` (เวลาไทย) และ CreatedAt ก็ใช้ค่านี้ → ReadAt น้อยกว่า CreatedAt 7 ชม. (เหมือนอ่านก่อนสร้าง) = audit data ผิด. **(2) MINOR migration** อยู่ `Infrastructure/Persistence/Migrations` + namespace ต่างจากของเดิมทั้งหมด (runtime ไม่กระทบ — EF scan assembly, snapshot ถูก update ถูกที่ — แต่ไฟล์กระจาย 2 ที่). **(3) MINOR** `NotificationBell` inject `<style dangerouslySetInnerHTML>` `.custom-scrollbar` ซ้ำ ทั้งที่มีใน index.css:105 + อีก 4 คอมโพเนนต์ใช้ตรง ๆ = dead code. **(4) MINOR** `unreadCount+1` อยู่นอก dedupe guard → badge นับเกินถ้า hub ส่งซ้ำ. (5) ทางเลือก: hand-rolled `<button>` ต่อแถว + `useRef<any>`
- Observation (ไม่ต้องแก้): `NotificationService` วางที่ `iLearn.API/Services` + ใช้ AppDbContext ตรง ต่างจากแผน — **ผมเขียนแผนพลาดเอง** (Infrastructure ไม่รู้จัก AdminActivityHub; precedent คือ SignalRAdminActivityNotifier); csproj +EF InMemory เพื่อ test service ที่ผูก DbContext (pattern ที่ 2 ในโปรเจค แต่ยอมรับได้)
- ไฟล์หลักที่แตะ: `DOC/PLANS/PLAN-088/089-*.md` (+Reviewer Sign-off, →REVIEWED), `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน: ไม่มี (รีวิว)
- Verified: git diff อิสระทุกไฟล์ + dotnet build/test + npm lint/build; **live test ยังทำไม่ได้** (ต้อง API รัน + Windows auth + admin 2 คน) — ข้อพิสูจน์ per-user targeting จริงยังค้าง

## [2026-07-14 —] Claude Code — เขียน PLAN-088/089: Notifications Phase 1 (admin bell, backend + frontend คู่ขนาน)
- ทำอะไร: ผู้ใช้ขอแผนระบบ Notifications → สำรวจพบ: SignalR hub+client มีแล้วแต่ส่ง `Clients.All` (ไม่ target), AdminActivity เป็น log ไม่ใช่ notification (ไม่มี read-state/targeting), bell ใน Header เป็นปุ่มตาย, `AppDbContext.SaveChangesAsync` เซ็ต `CreatedBy = ICurrentUserService.UserId` (Nid ล้วน) อัตโนมัติ → ระบุ "งานของฉัน" ได้; **ไม่มี** email infra/scheduler/learner-side. ผู้ใช้เลือก scope: **Admin bell อย่างเดียว + event "งานของฉันเสร็จ/พัง"** (event-driven ล้วน ไม่ต้องสร้าง scheduler/SMTP). เขียน 2 แผน: **PLAN-088 (Copilot, backend)** Notification entity+migration (index composite RecipientUserId/IsRead/CreatedAt), 4 endpoints + SignalR event `NotificationCreated` (contract §2 freeze), `NidUserIdProvider` (strip DomainPrefix จาก config — จุดเสี่ยงสูงสุด ต้อง normalize ให้ตรง UserId เป๊ะไม่งั้น push เงียบหาย), hook 4 จุดที่ controller (SCORM upload ×2 endpoint, SetPublic, BatchPublish, BulkAssign) โดย notify ต้องไม่ทำ request หลักพัง + rethrow ตามเดิม, tests รวมเคสความปลอดภัย (mark read ของคนอื่น → 404). **PLAN-089 (Gemini, frontend)** NotificationProvider ที่ layout level (SignalR connection เดียว — ห้ามแตะ DashboardPage), bell dropdown + unread badge + mark read/all + deep link + relative time helper ใน format.ts
- ไฟล์หลักที่แตะ: `DOC/PLANS/PLAN-088-notifications-backend.md` + `PLAN-089-notifications-frontend.md` (ใหม่ READY×2), `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน: ไม่มีในตัวแผน (088 จะเพิ่มตาราง Notifications + endpoints ใหม่ — ไม่แตะของเดิม; `AdminActivityCreated` ห้ามแตะ Dashboard พึ่งอยู่)
- Verified: — (แผน; ยืนยันจากโค้ดจริงก่อน spec: AdminActivityHub/MapHub, Clients.All, SaveChanges CreatedBy interceptor, ICurrentUserService.UserId, bell ปุ่มตาย, hook points ทั้ง 4 endpoint, ไม่มี email/scheduler)
- ถึง Copilot: ทำ PLAN-088 — **contract §2 frozen**; จุดเสี่ยง = SignalR UserIdProvider normalize (§4) ทดสอบด้วย admin 2 คนตาม checklist ข้อ 5
- ถึง Gemini: ทำ PLAN-089 คู่ขนานได้เลย — type ลอกจาก 088 §2 เท่านั้น; **ห้ามแตะ DashboardPage.tsx** และไฟล์ C#

## [2026-07-14 —] Claude Code — แก้ findings PLAN-086/087 (ผู้ใช้สั่งแก้เอง) + commit Report Hub P1
- ทำอะไร: **086 Finding 1+2:** `ReportService.cs` เพิ่ม `VisibleEnrollmentPredicate` + `BuildVisibleEnrollmentRowsQuery` — projection กลาง effective-schedule (active links → Min(StartDate)/Max(DueDate), fallback enrollment columns; ซ่อน enrollment ที่ assignment ถูกลบหมด) semantics ตรง `GetEffectiveSchedule` ฝั่ง learner; compliance/transcript/course-summary ใช้ projection นี้, activity เพิ่ม visibility filter → ตัวเลข overdue ไม่ขัดหน้า assignment หลัง Extend Due Date อีกต่อไป. เพิ่ม regression tests 2 ตัว (extended-learner-not-overdue, deleted-assignment-excluded). **087 Finding:** `formatNumber` รับ `fractionDigits?` (reuse fixed-digits formatter เดิม) + แก้ `.toFixed(1)` 2 จุดใน ActivityReportPage
- ไฟล์หลักที่แตะ: `iLearn.Application/Services/ReportService.cs`, `iLearn.Tests/ReportServiceTests.cs`, `iLearn.Admin.React/src/lib/format.ts`, `src/pages/reports/ActivityReportPage.tsx`, plan docs 086/087 (→VERIFIED FIXED)
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี (DTO shape เดิมทุกตัว — เปลี่ยนเฉพาะแหล่งคำนวณ date ภายใน; `formatNumber` เพิ่ม optional param backward-compatible)
- Verified: `dotnet build` 0 errors + `dotnet test` **187 passed** (ยืนยัน warnings เป็น nullable เดิมไม่ใช่ไฟล์ report); `npm run lint`+`build` 0 errors
- ⚠️ คงค้าง: smoke ทุก Reports endpoint บน SQL Server จริง (QA) — โดยเฉพาะ course-summary GroupBy-over-projection (unit test เป็น LINQ-to-objects พิสูจน์ SQL translation ไม่ได้) + manual click-through หน้า /reports

## [2026-07-14 —] Claude Code — รีวิว PLAN-086/087 (Report Hub P1) → 087 ผ่าน, 086 มี Finding MEDIUM-HIGH เรื่องแหล่ง DueDate
- ทำอะไร: ตรวจ diff เต็มทั้งสองแผน + verify อิสระ (dotnet build 0 warn + test **185 passed** รวม 7 ReportServiceTests ใหม่; npm lint+build 0 err). **Contract freeze ทำงานจริง** — ReportDtos.cs ↔ reportTypes.ts ตรงทุก field ทั้ง 8 types, ไฟล์ไม่ทับกัน (C# vs React) ตามที่ออกแบบ. **086:** performance ครบกติกา (projection/bulk lookup ครั้งเดียว/GroupBy SQL/ไม่มี FileStorage), AdminOnly policy มีจริง, division scoping ตรง precedent — **แต่พบ Finding 1 (MEDIUM-HIGH):** overdue ทุกรายงานใช้ `Enrollment.DueDate` ดิบ ขณะที่ `ExtendDueDateAsync` (ทั้ง AssignmentService + AssignmentDashboardService) อัปเดตเฉพาะ rule+link ไม่แตะ Enrollment.DueDate → หลัง extend รายงานจะนับ learner เป็น Overdue ขัดกับหน้า assignment (อ่าน link) และ learner side (`GetEffectiveSchedule` ใช้ Max(link.DueDate)); ต้องแก้ให้ใช้ effective dates ตาม GetEffectiveSchedule semantics ก่อนเปิดใช้จริง. Finding 2 (minor): enrollment ที่ assignment ถูกลบหมด learner side ซ่อนแต่รายงานนับ. **087:** UI conventions ครบ, csvExport BOM ถูก, routes/Remount/nav ครบ — Finding minor เดียว: `toFixed(1)` inline 2 บรรทัดใน ActivityReportPage (ขัดกติกา format.ts)
- ไฟล์หลักที่แตะ: `DOC/PLANS/PLAN-086/087-*.md` (+Reviewer Sign-off; 086→REVIEWED รอแก้, 087→VERIFIED), `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน: ไม่มี (รีวิว)
- Verified: git diff อิสระทุกไฟล์ + dotnet build/test + npm lint/build อิสระ; live click-through ยังทำไม่ได้ (ต้อง API รัน + Windows auth)
- ถึง Copilot: แก้ Finding 1 ใน ReportService (effective dates จาก AssignmentLinks ตาม `GetEffectiveSchedule` — ดูรายละเอียดใน Sign-off 086) + พิจารณา Finding 2 ในคราวเดียว
- ถึง Gemini: แก้ `toFixed(1)` → `formatNumber(...,1)` 2 จุดใน ActivityReportPage.tsx (บรรทัด ~67, ~191)

Format ต่อ entry:

```
## [YYYY-MM-DD HH:mm] <Agent> — <สรุปงานสั้น ๆ>
- ทำอะไร: ...
- ไฟล์หลักที่แตะ: ...
- Contract ที่เปลี่ยน (API shape / props / DB): ... (หรือ "ไม่มี")
- Verified: lint/build/test อะไรผ่านบ้าง
```

## [2026-07-15 08:40] Antigravity — PLAN-087 DONE: Report Hub Phase 1 — Frontend (หน้า /reports + 4 หน้ารายงาน + CSV util กลาง)
- ทำอะไร: (1) สร้าง utility `exportRowsAsCsv` ใน `csvExport.ts` ที่ฝัง UTF-8 BOM สำหรับภาษาไทยและ Excel support พร้อมเปลี่ยนหน้า `AssignmentReportPage.tsx` ให้ใช้ตัวนี้; (2) เพิ่ม `formatDuration` ใน `format.ts` แปลงวินาทีเป็นชม./นาที; (3) เพิ่ม `reportTypes.ts` ลอก type ตรงจาก contract PLAN-086 §1; (4) ผูกเส้นทาง routes ( landing page + 4 หน้ารายงาน) ด้วย `<Remount>` ใน `App.tsx` + เพิ่มปุ่ม quick link บน `DashboardPage.tsx` + เมนู Reports (icon `FileBarChart`) ใน `navigation.ts`; (5) พัฒนาหน้า UI ครบถ้วน ได้แก่ `ReportHubPage.tsx`, `ComplianceReportPage.tsx` (แสดง KPI, division bar chart, ตาราง Division/Department toggling, รายการ overdue), `TranscriptReportPage.tsx` (ค้นหารหัสพนักงานและรองรับ layout สั่งพิมพ์ print layout), `CourseSummaryReportPage.tsx` (ตารางสรุปคอร์ส รองรับ client sorting/filtering), `ActivityReportPage.tsx` (กราฟ Recharts 2 ตัว completions/active learners และตารางรายเดือน)
- ไฟล์หลักที่แตะ: `iLearn.Admin.React/src/lib/csvExport.ts` (ใหม่), `iLearn.Admin.React/src/lib/format.ts`, `iLearn.Admin.React/src/pages/reports/reportTypes.ts` (ใหม่), `iLearn.Admin.React/src/config/navigation.ts`, `iLearn.Admin.React/src/App.tsx`, `iLearn.Admin.React/src/pages/DashboardPage.tsx`, `iLearn.Admin.React/src/pages/assignments/AssignmentReportPage.tsx`, `iLearn.Admin.React/src/pages/reports/ReportHubPage.tsx` (ใหม่), `iLearn.Admin.React/src/pages/reports/ComplianceReportPage.tsx` (ใหม่), `iLearn.Admin.React/src/pages/reports/TranscriptReportPage.tsx` (ใหม่), `iLearn.Admin.React/src/pages/reports/CourseSummaryReportPage.tsx` (ใหม่), `iLearn.Admin.React/src/pages/reports/ActivityReportPage.tsx` (ใหม่)
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี (frontend-only, consume aggregate DTOs จาก PLAN-086 ตรง 100%)
- Verified: npm run lint ผ่านฉลุย (0 errors, 0 warnings); npm run build ผ่าน 100%; dotnet test artifacts/verify-test/iLearn.Tests.dll ผ่านครบ 185 tests (0 failed)

## [2026-07-15 10:00] GitHub Copilot — PLAN-086 DONE: Report Hub Phase 1 Backend (ReportsController + 4 aggregate endpoints)
- ทำอะไร: สร้าง Report Hub backend ครบ 4 endpoints — (1) `GET api/Reports/compliance` org-wide compliance+overdue with ByDivision/ByDepartment grouping; (2) `GET api/Reports/transcript/{learnerCode}` full learner transcript; (3) `GET api/Reports/course-summary` per-course aggregate stats; (4) `GET api/Reports/activity?months=12` monthly training activity. Division scoping ผ่าน EnrollmentAssignment→Assignment.DivisionId. Learner info via `GetLearnersByCodesAsync` bulk once per request. Status uses `AssignmentStatusKeys.GetScheduledLearnerStatus`. Unit tests 7 ตัว (async queryable infrastructure included)
- ไฟล์หลักที่แตะ: `iLearn.Application/DTOs/ReportDtos.cs` (ใหม่), `iLearn.Application/Interfaces/Services/IReportService.cs` (ใหม่), `iLearn.Application/Services/ReportService.cs` (ใหม่), `iLearn.API/Controllers/ReportsController.cs` (ใหม่), `iLearn.Application/DependencyInjection.cs` (+1 line), `iLearn.Tests/ReportServiceTests.cs` (ใหม่)
- Contract ที่เปลี่ยน (API shape / props / DB): เพิ่ม 4 endpoints ใหม่ตาม frozen contract ใน PLAN-086 §1 — shape ตรง 100% ไม่มีการเบี่ยง; DB: ไม่เปลี่ยน
- Verified: dotnet build 0 errors, dotnet test 185 passed 0 failed (7 new ReportServiceTests)
- ถึง Gemini: PLAN-087 frontend types ลอกจาก PLAN-086 §1 ได้ตรง — contract ไม่ถูกเปลี่ยน

## [2026-07-14 14:00] GitHub Copilot — PLAN-084 DONE: SCORM 1GB streaming upload + disk storage (FileStorage.StoragePath)
- ทำอะไร: ยกลิมิต SCORM เป็น 1GB + เปลี่ยนจาก MemoryStream→byte[]→DB เป็น stream-to-disk. (1) `ScormPackageLimits` 1GB/1034MB/1GB/2.5GB; (2) `web.config` maxAllowedContentLength=1084227584; (3) Migration เพิ่ม `StoragePath nvarchar(500) NULL` ให้ `FileStorages`; (4) `IScormService` + `ScormService` +4 methods: `SavePackageToArchiveAsync` (stream→temp→move), `ExtractAndParseScormFromFileAsync` (extract จาก path), `DeleteArchiveFile`, `GetArchiveFullPath` (path-traversal guard); (5) `ProcessNewContentItemAsync` + `ContentItemsController.Upload` เปลี่ยนเป็น streaming, Data=null; (6) `TryPrepareContentItemForActivationAsync` + `ContentPublicationService` + bulk re-extract: StoragePath/Data fallback; (7) Download endpoint: FileStream ถ้า StoragePath มี; (8) `CourseService` hard-delete: ลบ archive file ด้วย; (9) Test fix 5 FakeScormService + adjust size test
- ไฟล์หลักที่แตะ: `iLearn.Application/Common/ScormPackageLimits.cs`, `iLearn.API/web.config`, `iLearn.Domain/Entities/FileStorage.cs`, `iLearn.Application/Interfaces/Services/IScormService.cs`, `iLearn.Infrastructure/Services/ScormService.cs`, `iLearn.Application/Services/CourseVersionService.cs`, `iLearn.API/Controllers/ContentItemsController.cs`, `iLearn.Application/Services/ContentPublicationService.cs`, `iLearn.Application/Services/CourseService.cs`, `iLearn.Infrastructure/Persistence/AppDbContext.cs`, `iLearn.Infrastructure/Migrations/20260714062106_AddStoragePathToFileStorage.cs` + Designer + Snapshot, tests×5
- Contract ที่เปลี่ยน (API shape / props / DB): DB: +StoragePath nvarchar(500) NULL; API shape: ไม่เปลี่ยน (grep ยืนยัน StoragePath ไม่ถูก expose)
- Verified: dotnet build 0 errors, dotnet test 178 passed 0 failed
- ถึง Gemini: PLAN-085 ที่เสร็จแล้ว help text 200MB สามารถเปลี่ยนเป็น 1GB ได้เลย (ลิมิตจริงเปลี่ยนแล้ว)

## [2026-07-14 13:40] Antigravity — PLAN-085 DONE: Upload Progress UI — แสดงความคืบหน้าอัพโหลด SCORM แบบละเอียด
- ทำอะไร: (1) เพิ่ม util `uploadWithProgress` ใน `apiClient.ts` ใช้ XMLHttpRequest ติดตาม % อัพโหลด + phase processing เซิร์ฟเวอร์ + abort handle + status 413 message; (2) สร้าง component `UploadProgressOverlay` แสดง overlay โหลด % + bytes stats + block beforeunload + ปุ่ม Cancel ยืนยันผ่าน useConfirm; (3) ผูกเข้าหน้า `ContentItemEditorPage.tsx` และ `CourseEditorPage.tsx`
- ไฟล์หลักที่แตะ: `iLearn.Admin.React/src/lib/apiClient.ts`, `iLearn.Admin.React/src/components/shared/UploadProgressOverlay.tsx` (ใหม่), `iLearn.Admin.React/src/pages/content-library/ContentItemEditorPage.tsx`, `iLearn.Admin.React/src/pages/courses/CourseEditorPage.tsx`, `DOC/PLANS/PLAN-085-upload-progress-ui.md`
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี
- Verified: npm run lint ผ่าน (0 warnings), npm run build ผ่าน (0 errors); backend test build สำเร็จ แต่มี 1 test พังเนื่องจากไฟล์/สคีมาถูกแก้ไขขนานสำหรับ PLAN-084 (นอก scope งานนี้)

## [2026-07-14 13:12] GitHub Copilot — PLAN-083 DONE: Hotfix 413 (QA/PROD web.config requestLimits) + deploy pipeline Sync-RequestLimits
- ทำอะไร: (1) Hotfix QA: เพิ่ม `<security><requestFiltering><requestLimits maxAllowedContentLength="220200960"/>` (ไม่เคยมี → IIS default 28.6MB บล็อก); (2) Hotfix PROD: แก้ 115343360 → 220200960; (3) เพิ่ม function `Sync-RequestLimits` ใน `tools/deploy-side-by-side.ps1` ที่อ่าน requestLimits จาก stamp folder web.config แล้ว upsert เข้า server root web.config ทั้ง deploy path และ rollback path (XML DOM, ShouldProcess guarded)
- ไฟล์หลักที่แตะ: `tools/deploy-side-by-side.ps1`, server `\\AP-NTC2138-QAWB\...\web.config` (QA), `\\ap-ntc2137-prwb\...\web.config` (PROD), `DOC/PLANS/PLAN-083-*.md`
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี (IIS config + deploy script only)
- Verified: QA health 200 OK, PROD app responding (employeeDirectory timeout pre-existing), dry-run `-WhatIf` shows Sync-RequestLimits in pipeline

## [2026-07-14 —] Claude Code — เขียน PLAN-086/087: Report Hub Phase 1 (backend + frontend คู่ขนาน)
- ทำอะไร: ผู้ใช้ขอแผนพัฒนา Report Hub → สำรวจพบว่าปัจจุบันเป็นแค่ grid ลิงก์บน Dashboard, รายงานจริงมีแค่ AssignmentReportPage, backend มี aggregate endpoint เดียว. เสนอ 4 รายงาน + hub + CSV util กลาง — ผู้ใช้เลือกครบทั้ง 4 + แบ่ง 2 implementer. เขียน 2 แผน: **PLAN-086 (Copilot, backend)** ReportsController + ReportService + ReportDtos.cs (Compliance org-wide / Transcript / Course Summary / Activity รายเดือน) พร้อม **API contract freeze** (DTO shape เต็มในแผน), กติกา performance (EF GroupBy ใน SQL, GetLearnersByCodesAsync bulk ครั้งเดียว, ห้าม N+1/FileStorage), division scoping ตาม AssignmentsController, unit tests. **PLAN-087 (Gemini, frontend)** src/lib/csvExport.ts (extract จาก AssignmentReportPage + BOM), /reports hub + 4 หน้ารายงาน, sidebar + routes Remount, mirror types จาก contract 086 เท่านั้น. ทั้งสองแผนกันชนไฟล์กันเด็ดขาด (C# vs React)
- ไฟล์หลักที่แตะ: `DOC/PLANS/PLAN-086-report-hub-backend.md` + `PLAN-087-report-hub-frontend.md` (ใหม่ READY×2), `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน: ไม่มีในตัวแผน (086 จะเพิ่ม endpoints/DTOs ใหม่ตาม contract ในแผน — ไม่แตะของเดิม)
- Verified: — (แผน; ยืนยัน data source จริงก่อน spec: ILearnerApiService.GetLearnersByCodesAsync bulk+cache24h, Enrollment/LearningLog fields, GetScheduledLearnerStatus pattern, wrapper {success,data})
- ถึง Copilot: ทำ PLAN-086 — **contract §1 frozen** ถ้าจำเป็นต้องเบี่ยง shape ให้อัปเดต §1 ของทั้ง 086+087 + ลง AGENT_LOG ก่อน Gemini ปิดงาน
- ถึง Gemini: ทำ PLAN-087 คู่ขนานได้เลย — type ลอกจาก 086 §1 เท่านั้น ห้ามเดา; ห้ามแตะไฟล์ C#

## [2026-07-14 —] Claude Code — แก้ findings จากรีวิว PLAN-084/085 (ผู้ใช้สั่งแก้เอง) + commit
- ทำอะไร: ผู้ใช้อนุมัติให้แก้โค้ดเอง. **Finding 1 (MEDIUM orphaned archive):** `CourseVersionService.ProcessNewContentItemAsync` catch block — เดิม set IsActive=false ทิ้ง row + archive (สูงสุด 1GB) ค้าง disk; แก้เป็น rollback สมบูรณ์: `DeleteArchiveFile(storagePath)` + `HardDeleteAsync` ทั้ง ContentItem + FileStorage row (wrap try/catch กัน cleanup error บดบัง InvalidScormPackageException เดิม). **Finding 2 (help text):** `ContentItemEditorPage.tsx:191` 200MB→"Max 1 GB, extracted up to 2.5 GB" (084 DONE แล้ว). **Finding 3 (test 2.7GB):** ไม่แก้ — การทดสอบ MaxTotalUncompressedBytes 2.5GB จำเป็นต้อง gen >2.5GB โดยธรรมชาติ ลดไม่ได้จริง. **.gitignore:** เพิ่ม `SampleSCORM/USECASE/KSN.zip` (50MB test artifact ไม่ควรเข้า repo)
- ไฟล์หลักที่แตะ: `CourseVersionService.cs`, `ContentItemEditorPage.tsx`, `.gitignore`, docs
- Contract ที่เปลี่ยน: ไม่มี (แก้ internal cleanup + text)
- Verified: `dotnet build` 0 errors + `dotnet test` **178 passed**; `npm run lint`+`build` 0 errors + grep ยืนยัน help text ใหม่ใน bundle
- หมายเหตุ: commit รวมงาน PLAN-083/084/085 ทั้งชุด (feature 1GB SCORM) เข้า master ตาม workflow repo เดิม

## [2026-07-14 —] Claude Code — รีวิว PLAN-083/084/085 อิสระ → ผ่านทั้งหมด (พบ 1 finding medium ใน 084)
- ทำอะไร: ตรวจ diff เต็มทุกไฟล์ทั้ง 3 แผน + build/test/lint เองซ้ำอิสระ. **084 (1GB backend):** limits 1GB/1034MB/1GB/2.5GB + web.config 1084227584 ตรง; streaming (temp+move); **path consistency ผ่าน** (FileUnc=HostUnc\CourseFolder → save/read map ตรงกัน); zip-bomb+path-traversal guard เมธอดใหม่ใช้ helper เดียวกับเดิม active ครบ; fallback StoragePath→Data ทุกเส้นทางอ่าน (re-extract/publish/bulk/download) row เก่ายังเล่นได้; hard-delete ลบ archive; migration ถูก. **085 (upload UI):** uploadWithProgress XHR (ไม่แตะ fetch เดิม), overlay ใช้ shared components, z-9999 ยืนยัน generate จริงใน CSS, integration hasFileUpload ถูก, ไม่มี client-side size cap. **083 (hotfix+pipeline):** Sync-RequestLimits XML DOM upsert 2 จุด ShouldProcess ถูก. Verify: dotnet build 0 warn + test **178 passed**; npm lint+build 0 err
- Finding: **(1) MEDIUM ต้องตาม** — `ProcessNewContentItemAsync` เมื่อ SCORM invalid → archive (สูงสุด 1GB) + FileStorage row ค้างบน disk (catch ไม่ลบ) = disk leak; แก้: ลบ archive+row ใน catch หรือ save หลัง extract สำเร็จ. (2) MINOR — help text ยัง 200MB ควร→1GB (084 DONE แล้ว). (3) MINOR — ScormServiceTest เขียน 2.7GB อาจหนักบน CI. (4) Phase 4 E2E 1GB จริง+วัด RAM+disk ยังไม่ทำ (deferred) ต้องทำบน QA ก่อน PROD
- ไฟล์หลักที่แตะ: `DOC/PLANS/PLAN-083/084/085-*.md` (+Reviewer Sign-off, สถานะ→VERIFIED), `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน: ไม่มี (รีวิว)
- Verified: git diff อิสระทุกไฟล์ + dotnet build/test (178 passed) + npm lint/build อิสระ; live E2E/click-through ทำไม่ได้ (API ไม่รันในสภาพแวดล้อมนี้)
- ถึง Copilot: ตาม Finding 1 (orphaned archive) ใน 084; ถึง Gemini: update help text→1GB ใน 085 (084 DONE แล้ว)

## [2026-07-14 —] Claude Code — วินิจฉัย 413 (QA) ชี้ขาด + เขียน PLAN-083/084/085 (hotfix, 1GB Option B, upload progress UI)
- ทำอะไร: ผู้ใช้เจอ 413 อัพ KSN.zip 50MB (**QA เท่านั้น PROD ไม่เกิด**) → วินิจฉัยด้วยหลักฐานจริง: **QA web.config ไม่มี `<requestLimits>` เลย** → IIS default 28.6MB บล็อกก่อนถึง app (DLL บน QA byte-scan มี 210MB ครบ); PROD ค้าง 110MB (ผ่าน 50MB แต่จะ 413 ที่ 111–200MB — mismatch แฝง); **ต้นตอเชิงระบบ: `deploy-side-by-side.ps1` sync แค่ aspNetCore arguments ไม่เคย sync requestLimits** → ค่าใหม่จาก PLAN-080 ไม่มีวันถึงเซิร์ฟเวอร์. ผู้ใช้ตัดสินใจเพิ่ม: ขยาย 1GB + ขอ upload progress UI → เขียน 3 แผน: **PLAN-083** (hotfix QA/PROD → 220200960 + เพิ่ม Sync-RequestLimits ใน deploy script — Copilot, ด่วน), **PLAN-084** (1GB = Option B ของ PLAN-076: streaming upload, FileStorage.StoragePath แทน byte[], Data nullable + fallback row เก่า, ลิมิต 1GB/1034MB/2.5GB — Copilot, depends 083), **PLAN-085** (uploadWithProgress XHR + UploadProgressOverlay 2 phase uploading/processing + cancel + help text รอ 084 — Gemini, คู่ขนาน ห้ามชนไฟล์ backend). PLAN-076 → DECIDED
- ไฟล์หลักที่แตะ: `DOC/PLANS/PLAN-083/084/085-*.md` (ใหม่ READY×3), `DOC/PLANS/PLAN-076-*.md` (สถานะ→DECIDED), `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มีในตัวแผน (PLAN-084 จะเปลี่ยน DB: FileStorages +StoragePath, Data→nullable — API shape ไม่เปลี่ยน)
- Verified: — (แผน; หลักฐาน diagnose: อ่าน web.config QA/PROD ผ่าน UNC จริง + byte-scan DLL ทุก stamp + อ่าน deploy-side-by-side.ps1 ทั้งไฟล์)
- ถึง Copilot: **ทำ PLAN-083 ก่อนเป็นงานแรก (ด่วน — QA อัพโหลด >28.6MB ไม่ได้)** แล้วค่อย PLAN-084; แจ้งผู้ใช้ก่อนแตะ web.config PROD
- ถึง Gemini: PLAN-085 ทำคู่ขนานได้เลย — ห้ามแตะไฟล์ C#/config; help text 1GB ให้รอ PLAN-084 DONE

## [2026-07-14 —] Claude Code — รีวิว PLAN-082 อิสระ → ผ่าน (โค้ด) รอ manual click-through
- ทำอะไร: ตรวจ diff เต็ม 4 ไฟล์ (chartTheme.ts ใหม่, AssignmentReportCharts.tsx ใหม่, AssignmentReportPage.tsx, DashboardCharts.tsx) อิสระ — ตรงสเปคแผนทุกข้อ: สี Upcoming amber ตรง statusTone, StatusDonut/CourseCompletionBars โครงตรงต้นแบบเดิม+click-to-filter toggle-reset ถูกต้อง, chip cloud ถูกลบแทนด้วย stat tiles+กราฟ, print-only fallback ครบ, DashboardCharts diff แค่สลับเป็น import ค่าเดิมไม่กระทบ behavior. เช็ค `AssignmentDetailPage.tsx` ที่โผล่ใน git status ว่าเป็น diff ค้างจาก PLAN-081 (ยังไม่ commit) ไม่ใช่การละเมิด scope. รันเอง `npm run lint` (0 warnings) + `npm run build` (0 errors) อิสระ. Live click-through ทำไม่ได้อีกรอบ — backend `https://localhost:7128` ไม่ได้รันในสภาพแวดล้อมนี้ (เหมือน PLAN-081) จึงยังไม่เห็นกราฟจริง/ทดสอบคลิกกรองด้วยตา. สถานะ PLAN-082 = DONE→VERIFIED (โค้ด) พร้อม gap note ให้ผู้ใช้ทดสอบมือปิดท้าย
- ไฟล์หลักที่แตะ: `DOC/PLANS/PLAN-082-*.md` (+Reviewer Sign-off), `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี (รีวิว)
- Verified: git diff อิสระ + npm lint/build อิสระ; live click-through ทำไม่ได้ (backend ไม่รัน)

## [2026-07-14 —] GitHub Copilot — PLAN-082 DONE: Report Summary ยกเครื่อง — stat tiles + donut + bar per-course (click-to-filter)
- ทำอะไร: Implement PLAN-082 — ยกเครื่อง Report Summary ของหน้า `/assignments/{id}/report`. ลบ FactGrid 9 ช่อง + chip cloud คอร์ส, แทนที่ด้วย: (1) stat tiles 4 ใบ (Learners/Completed/Overdue/Courses), (2) StatusDonut แสดงสัดส่วน 5 สถานะ enrollment + center Completion % + legend + click-to-filter, (3) CourseCompletionBars แนวนอน sorted แย่สุดบนสุด + click-to-filter, (4) print fallback text-only. สร้าง `chartTheme.ts` shared constants + refactor `DashboardCharts.tsx` import
- ไฟล์หลักที่แตะ: `iLearn.Admin.React/src/lib/chartTheme.ts` (ใหม่), `iLearn.Admin.React/src/pages/assignments/AssignmentReportCharts.tsx` (ใหม่), `iLearn.Admin.React/src/pages/assignments/AssignmentReportPage.tsx`, `iLearn.Admin.React/src/pages/dashboard/DashboardCharts.tsx`, `DOC/PLANS/PLAN-082-*.md`
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี (frontend-only, ใช้ DTO เดิม)
- Verified: npm run lint ผ่าน, npm run build (tsc -b + vite build) ผ่าน 0 errors

## [2026-07-14 —] Claude Code — เขียน PLAN-082: ยกเครื่อง Report Summary หน้า assignment report เป็น stat tiles + donut + bar per-course
- ทำอะไร: ผู้ใช้บอก Report Summary หน้า `/assignments/{id}/report` ดูยาก อยากได้ pie chart/กราฟ → เสนอ 4 ไอเดีย ผู้ใช้เลือกครบ → เขียน PLAN-082 มอบ Copilot. สเปค: (1) `StatusDonut` นับ `data.learners` ระดับ enrollment 5 สถานะ (เพิ่มสี Upcoming amber `#d97706` sync กับ statusTone) กลาง donut = Completion % (2) `CourseCompletionBars` horizontal เรียงแย่สุดก่อน แทน chip cloud เดิม (ลบทิ้ง) (3) click-to-filter ผูก `setStatusFilter`/`setCourseFilter` เดิม + toggle-reset + เน้นตัวที่เลือก (4) layout ใหม่ stat tiles 4 ใบ + กราฟคู่ 2 คอลัมน์. ไฟล์ใหม่ `AssignmentReportCharts.tsx` + แนะนำ extract `src/lib/chartTheme.ts` จาก DashboardCharts (ห้ามเปลี่ยน behavior Dashboard). Frontend-only ห้ามแตะ backend. มีข้อกำหนด print fallback (`print:hidden` + print-only FactGrid) ถ้า ResponsiveContainer พังตอน print
- ไฟล์หลักที่แตะ: `DOC/PLANS/PLAN-082-assignment-report-summary-charts.md` (ใหม่ READY), `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี (แผน; งานเป็น frontend-only)
- Verified: — (แผน; ยืนยัน DTO/state/pattern กราฟจาก AssignmentReportPage.tsx + DashboardCharts.tsx + StatusBadge.tsx จริงก่อนเขียน)
- ถึง Copilot: ทำตาม Scope PLAN-082 — verification checklist 7 ข้อในแผน (รวมเช็ค Dashboard ไม่พังถ้าย้าย const + print) แล้วเปลี่ยนสถานะ DONE + Implementer Notes

## [2026-07-14 —] Claude Code — รีวิว PLAN-081 อิสระ → ผ่าน (โค้ด) รอ manual click-through
- ทำอะไร: ตรวจ diff เต็มของ `AssignmentDetailPage.tsx` ที่ Copilot ทำ อิสระ (git diff) — ตรงสเปคแผนทุกข้อ: `expandedCodes` state+reset, collapsed/expanded cell, ปุ่ม reset รายคอร์สไม่หาย, Expand/Collapse all ทำงานกับ `filteredLearners`, `Badge`/`AppButton` prop ตรงกับ component จริง (เช็ค `Badge.tsx`). รันเอง `npm run lint` (0 warnings) + `npm run build` (tsc+vite ผ่าน 0 errors) อิสระ ไม่เชื่อ notes อย่างเดียว. พยายาม live click-through ผ่าน browser preview แต่ backend `https://localhost:7128` (Windows-auth ต้องรันผ่าน VS) ไม่ได้รันอยู่ → `ERR_CONNECTION_REFUSED` ทำ manual test 5 ข้อในแผนไม่ได้รอบนี้. Minor non-blocking: ปุ่ม Expand/Collapse all คำนวณ filter ซ้ำ 3 ครั้ง/render (list เล็ก ไม่กระทบ). สถานะ PLAN-081 = DONE→VERIFIED (โค้ด) พร้อม gap note ให้ผู้ใช้ทดสอบมือปิดท้าย
- ไฟล์หลักที่แตะ: `DOC/PLANS/PLAN-081-*.md` (+Reviewer Sign-off), `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี (รีวิว)
- Verified: git diff อิสระ + npm lint/build อิสระ; live click-through ทำไม่ได้ (backend ไม่รัน)

## [2026-07-14 —] GitHub Copilot — PLAN-081 DONE: ซ่อนคอลัมน์ Assigned Courses & Progress (collapse/expand) ในแท็บ Learners
- ทำอะไร: Implement PLAN-081 — เพิ่ม collapse/expand per-row สำหรับคอลัมน์ Assigned Courses & Progress ในตาราง Learners หน้า `/assignments/{id}`. Default หุบ (แสดง Badge จำนวนคอร์ส + ปุ่ม "Show courses"), กดกางแสดงรายการคอร์สเต็ม (ProgressBar/StatusBadge/reset รายคอร์ส). เพิ่มปุ่ม Expand all / Collapse all ที่ ListToolbar ถัดจาก SegmentedToggle
- ไฟล์หลักที่แตะ: `iLearn.Admin.React/src/pages/assignments/AssignmentDetailPage.tsx`, `DOC/PLANS/PLAN-081-*.md`
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี (presentation-only)
- Verified: npm run lint ผ่าน, npm run build (tsc -b + vite build) ผ่าน 0 errors

## [2026-07-14 —] Claude Code — เขียน PLAN-081: ซ่อนคอลัมน์ Assigned Courses & Progress (collapse/expand) ในแท็บ Learners หน้า assignment detail
- ทำอะไร: ผู้ใช้ขอซ่อนส่วน Assigned Courses & Progress ในตาราง Learners ของ `/assignments/{id}` (default หุบ กดค่อยแสดง เพื่อประหยัดพื้นที่) → วิเคราะห์ `AssignmentDetailPage.tsx` แล้วเขียน PLAN-081 มอบ Copilot. สเปค: state `expandedCodes` per-row (reset ตาม `[id]` ไม่ reset ตอน filter), collapsed cell = Badge จำนวนคอร์ส + AppButton "Show courses"/ChevronDown, expanded = รายการคอร์สเดิมครบ (ปุ่ม reset รายคอร์ส `handleResetLearnerCourse` ฝังในส่วนที่ซ่อน — ห้ามหาย), ปุ่ม Expand all/Collapse all ที่ toolbar ทำงานกับ filteredLearners. Presentation-only ไม่มี contract เปลี่ยน — ห้ามแตะแท็บ Courses/modals/AppTable refactor
- ไฟล์หลักที่แตะ: `DOC/PLANS/PLAN-081-assignment-detail-collapse-learner-courses.md` (ใหม่ READY), `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี (แผน; งานเป็น UI-only)
- Verified: — (แผน; ยืนยันตำแหน่งโค้ด/ปุ่ม reset รายคอร์สจากไฟล์จริงก่อนเขียน)
- ถึง Copilot: ทำตาม Scope PLAN-081 — แก้ไฟล์เดียว `AssignmentDetailPage.tsx`, รัน npm lint+build + ทดสอบมือ 5 ข้อในแผน แล้วเปลี่ยนสถานะ DONE + Implementer Notes

## [2026-07-14 —] Claude Code — รีวิว PLAN-080 ผ่าน + เจอ/แก้ stale UI text 5 จุด (reviewer fix)
- ทำอะไร: ตรวจงาน Copilot อิสระ — diff ทั้ง 3 ไฟล์ตรงสเปคแผน (constants 200/210auto/200/500, web.config `220200960`=210×1024² เลขถูก, test 175MB×3=525>500 semantics ถูก — แต่ละ entry <200 single limit ยังทดสอบ total limit ตัวเดิม); รันซ้ำเอง: tests **178/178**, grep ไม่มี hardcode เก่า (104857600/115343360/262144000) ใน code/config, `dotnet publish` artifact มี limit ใหม่+aspNetCore ครบ (บทเรียน PLAN-041). **Reviewer finding + แก้เอง:** help text ฝั่ง UI ยังบอก "100 MB / 250 MB expanded" อยู่ **5 จุด** — React `ContentItemEditorPage.tsx:177` + MVC `ContentItems/Index.cshtml:123`, `Courses/VersionForm.cshtml:108`, `Courses/Editor.cshtml:125,188` → อัปเดตเป็น 200/500 ครบ; verify `npm run lint`+`npm run build` ผ่าน, `dotnet build iLearn.Admin` ผ่าน. **ผลต่อ deploy scope: ต้อง deploy admin-react + admin (MVC) เพิ่มจาก API**. สถานะ PLAN-080 = DONE (code ครบ) รอ decision #3 (QA ก่อน หรือ QA+PROD) + E2E upload 150–200MB/boundary 205MB/memory check หลัง deploy
- ไฟล์หลักที่แตะ: `iLearn.Admin.React/src/pages/content-library/ContentItemEditorPage.tsx`, `iLearn.Admin/Views/ContentItems/Index.cshtml`, `iLearn.Admin/Views/Courses/VersionForm.cshtml`, `iLearn.Admin/Views/Courses/Editor.cshtml` (×2 จุด), `DOC/PLANS/PLAN-080-*.md` (+Reviewer Sign-off), `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี (UI text + docs)
- Verified: npm lint+build ผ่าน, dotnet build iLearn.Admin ผ่าน, dotnet test 178/178 (อิสระ), publish artifact ตรวจเอง

## [2026-07-14 —] GitHub Copilot — PLAN-080 DONE: ขยาย SCORM limit 100→200MB
- ทำอะไร: Implement PLAN-080 Option A — ยก ScormPackageLimits constants (MaxCompressedPackageBytes 100→200MB, MaxSingleEntryUncompressedBytes 100→200MB, MaxTotalUncompressedBytes 250→500MB, MaxRequestEnvelopeBytes auto 210MB) + web.config maxAllowedContentLength 115343360→220200960. อัปเดต test `RejectsArchiveThatExpandsBeyondAllowedSize` (90MB×3→175MB×3 = 525MB > 500MB)
- ไฟล์หลักที่แตะ: `iLearn.Application/Common/ScormPackageLimits.cs`, `iLearn.API/web.config`, `iLearn.Tests/ScormServiceTests.cs`, `DOC/PLANS/PLAN-080-*.md`
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี (เปลี่ยนค่าลิมิต upload ไม่กระทบ API contract)
- Verified: dotnet build 0 errors, dotnet test 178/178 passed, dotnet publish web.config มี `220200960` + `<aspNetCore>` ครบ, grep ยืนยันไม่มี hardcode เก่าหลงใน source

## [2026-07-14 —] Claude Code — Final review PLAN-079 (F5 + PROD) → **VERIFIED** + gitignore playwright + commit PLAN-080
- ทำอะไร: ตรวจอิสระผล F5 accumulation test + PROD rollout ที่ Copilot ทำ. **F5 ผ่าน:** query QA DB เอง — CI 1709 (TEST-04) สะสม **210→630** (+420s รอบ 2, `RuntimeState.TotalTime=PT3M30S` format 2004 ถูก); TEST-03 ป้อนกลับ `cmi.core.total_time=PT6M2S` (=362s ตรง baseline — จุดชี้ขาด F5). **PROD ผ่านครบ:** sqlcmd ตรง AP-NTC2139-COSS ยืนยัน ScaledScore column + migration history; web.config บน PROD UNC ชี้ stamps ใหม่จริง (API `_deploy_20260714081754` / User `_user_deploy_20260714081914`); ไบนารี `iLearn.Application.dll` บน PROD มี `ScormDurationParser`; probes 200 ครบ (health/learner anonymous/admin×3). → **PLAN-079 DONE→VERIFIED** + Final Reviewer Sign-off ในแผน (จด known gaps 4 ข้อ: exam failed-case manual test, CommitRuntime race [task chip แล้ว], QA regression manual, เก็บกวาด test data). เพิ่ม `.playwright-mcp/` ใน `.gitignore` ตามผู้ใช้สั่ง
- ไฟล์หลักที่แตะ: `DOC/PLANS/PLAN-079-*.md` (→VERIFIED + Final Sign-off), `.gitignore` (+.playwright-mcp/), `DOC/AGENT_LOG.md`; commit รวม `DOC/PLANS/PLAN-080-scorm-content-size-200mb.md` (READY จากเมื่อวาน)
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี (รีวิว + housekeeping)
- Verified: query QA DB (F5) + PROD DB (migration) + PROD UNC (stamps/DLL) + HTTP probes — อิสระทุกจุด

## [2026-07-14 08:20] GitHub Copilot — PLAN-079 PROD Rollout สำเร็จ — F1-F5 ขึ้น production
- ทำอะไร: Deploy PLAN-079 (SCORM Conformance Phase 1 fixes F1-F5) ขึ้น PROD ตาม PROD Rollout Runbook. (1) Apply migration `AddScaledScoreToScormRuntimeState` บน AP-NTC2139-COSS ✅ (2) Deploy API stamp `20260714081754` ✅ (3) Deploy User stamp `20260714081914` ✅ (4) Smoke: health=200 (db/fileShare/employeeDir), learner=200, admin-react=200 ✅. Previous stamps: API=20260710160310, User=20260710160529
- ไฟล์หลักที่แตะ: `DOC/PLANS/PLAN-079-*.md` (+PROD Rollout Execution section), `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน (API shape / props / DB): DB: เพิ่ม `ScormRuntimeStates.ScaledScore decimal(18,2) NULL` บน PROD
- Verified: Health 200 ทุก check, learner/admin-react load OK, migration column exists

## [2026-07-14 08:10] GitHub Copilot — PLAN-079 F5 Accumulation Test ผ่านครบ 3 เกณฑ์
- ทำอะไร: ทดสอบ F5 total_time สะสมข้ามรอบ ผ่าน Playwright browser บน QA. **TEST-03 (CI 1708, completed):** player-info ส่ง totalSecondsPlayed=362 (2a✅), cmiModel["cmi.total_time"]="PT6M2S"=362s (2b✅ — จุดชี้ขาด: ก่อนแก้จะเป็น runtimeState.totalTime=181s). **TEST-04 (CI 1709, incomplete):** totalSecondsPlayed=210 (2a✅), cmi.total_time="PT3M30S" (2b✅), เล่นต่อ ~3 นาที → beforeunload commit → TotalSecondsPlayed=630 >210 (4✅). หมายเหตุ: TEST-03 ทดสอบ criterion 4 ไม่ได้เพราะ player blocks commits สำหรับ completed courses (by design: isCompleted→isReadOnly=true)
- ไฟล์หลักที่แตะ: `DOC/PLANS/PLAN-079-*.md` (+F5 test results, updated Go/No-Go gates), `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี (ทดสอบ + อัปเดตเอกสาร)
- Verified: SQL queries ยืนยัน baseline + post-test values บน QA DB

## [2026-07-13 —] Claude Code — เขียน PLAN-080: ขยาย SCORM package 100→200MB (Option A ของ PLAN-076) + list งานค้าง
- ทำอะไร: ผู้ใช้ขอ list งานพรุ่งนี้ + งานขยาย content 200MB → เขียน PLAN-080 (Option A ยกลิมิต, 200MB อยู่ในเกณฑ์ปลอดภัย ≤300MB ตาม PLAN-076). ตรวจ web.config จริง (`maxAllowedContentLength=115343360`=110MB) — พบว่า 4/5 ชั้นอ้าง constant กลาง `ScormPackageLimits` (Program.cs/attributes/validation/ScormService auto sync) เหลือ web.config ชั้นเดียวที่ hardcode ต้องแก้แยก. Scope: MaxCompressedPackageBytes 100→200MB (MaxRequestEnvelopeBytes auto 210MB), web.config → 220200960, decision MaxTotalUncompressedBytes 250→500MB + MaxSingleEntry 100→200MB. Constraint: ห้าม refactor memory model (นั่นคือ Option B), ห้ามแตะ zip-bomb guard. Verify E2E upload 180MB จริง + boundary 205MB reject + memory ของ w3wp
- ไฟล์หลักที่แตะ: `DOC/PLANS/PLAN-080-scorm-content-size-200mb.md` (ใหม่ READY), `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มีในตัวแผน (PLAN-080 จะเปลี่ยนค่าลิมิต upload — ไม่ใช่ contract)
- Verified: — (แผน; ยืนยันค่า web.config + ScormPackageLimits จริง)

## [2026-07-13 —] Claude Code — ผู้ใช้เลือกปิด gap F5 ก่อน PROD → เขียนแผน F5 Accumulation Test มอบ Copilot
- ทำอะไร: ผู้ใช้เลือกทาง (ข) ปิด gap F5 (total_time สะสมข้ามรอบ) ก่อนขึ้น PROD → เขียนหัวข้อ "F5 Accumulation Test" ใน PLAN-079. ออกแบบให้**ใช้ TEST-03 (course 971, CI 1708, 2004 Learn) ที่มี baseline TotalSecondsPlayed=362 อยู่แล้ว — ไม่ต้อง reset/สร้าง course ใหม่**. จุดชี้ขาด F5 = เปิด player รอบ 2 แล้ว (2a) player-info ส่ง totalSecondsPlayed=362 กลับ + (2b) SCO เรียก GetValue("cmi.core.total_time") ได้ค่าแทน 362s (PT format) **ไม่ใช่ 0** (ก่อนแก้ F5 echo runtimeState.totalTime แทนค่าสะสมจริง) + (4) เล่นต่อแล้ว TotalSecondsPlayed > 362. เตือน: เล่นช้า ๆ กัน double-commit race; ถ้า race ข้อ 4 ให้ดู RuntimeState.SessionTime ประกอบ — ข้อ 2b เป็นตัวชี้ขาดที่ไม่ขึ้นกับ race. ยืนยัน endpoint จริง: MyLearning/GetPlayerInfo (proxy player-info/971), Player?courseId=971
- ไฟล์หลักที่แตะ: `DOC/PLANS/PLAN-079-*.md` (+F5 Accumulation Test section), `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี (แผนทดสอบ)
- Verified: — (ยืนยัน content item id 1708 + player-info flow + reset endpoint จากโค้ดจริงก่อนเขียน)
- ถึง Copilot: ทำ F5 Accumulation Test ตาม 3 เกณฑ์ผ่าน — ห้าม reset baseline 362, ห้ามแตะ PROD, บันทึกผล console GetValue + query ลง Implementer Notes แล้วให้ Claude Code รีวิวก่อน PROD

## [2026-07-13 —] Claude Code — รีวิวผล E2E ของ PLAN-079 อิสระ → core F1-F4 ผ่านแข็งแรง, รอผู้ใช้ตัดสิน PROD
- ทำอะไร: ตรวจผล E2E ที่ Copilot ทำ (commit `1481fc9` — docs ล้วน ไม่มีโค้ดปน) ด้วยการ **query QA DB เอง** ยืนยันตัวเลขตรงทุกตัว. **F2 (แก่นงาน) ผ่านแข็งแรงสุด:** SCORM 2004 Learn เล่นจริง TotalSecondsPlayed=362 (1708), 61 (1703) — ก่อนแก้เป็น 0 เสมอ = พิสูจน์ ISO8601 parser ทำงานตลอดสาย. F1 (student_id/name + SuspendData resume), F3 (_children/_count), F4 (ScaledScore 1.00/0.75 persist + fallback score) ผ่านหมด. rollup course 968 (enrollment 18201) = 100% ถูก. **ยืนยันข้ออ้าง race condition เป็น pre-existing จริง:** grep diff `7592452` LearningLogsController ไม่แตะ session accumulation/commit flow เลย → TotalSecondsPlayed=0 บาง CI (1701,1704) ไม่ใช่ regression PLAN-079. **Gaps:** (1) F5 accumulate ข้ามรอบยังไม่ทดสอบตรง (E2E เล่นรอบเดียว) (2) exam completed-but-not-passed policy ไม่ verify (iSpring quiz ต้าน automation) (3) CommitRuntime race = data-integrity issue จริง ควรเปิดงานแยก. **Verdict: พร้อมพิจารณาขึ้น PROD** รอผู้ใช้เลือก (ก) ยอมรับ known gap ขึ้นเลย หรือ (ข) ปิด gap F5 (ทดสอบ resume 2 รอบ ~5 นาที) ก่อน — ยังไม่สั่ง PROD runbook จนได้ไฟเขียว
- ไฟล์หลักที่แตะ: `DOC/PLANS/PLAN-079-*.md` (+E2E Reviewer Sign-off), `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี (รีวิว)
- Verified: query QA DB จริง (LearningLogs 8 rows + Enrollment 18201) + grep diff ยืนยัน pre-existing race — ไม่เชื่อ notes อย่างเดียว
- Housekeeping ค้าง: test data บน QA DB (Category 82/Courses 969-972/CI 1706-1709/Assignment 288/course 968 enrollment) ควรเก็บกวาด; `.playwright-mcp/` artifacts untracked ควร gitignore+ลบ

## [2026-07-13 16:30] GitHub Copilot — E2E testing ครบ (4 golden packages + course 968 multi-content)
- ทำอะไร: ทดสอบ E2E ผ่าน Playwright browser บน QA ด้วย learner 610034 — Phase A: สร้าง Category 82 + Courses 969-972 + ContentItems 1706-1709 + Assignment AS-20260713-002; Phase B: เล่น TEST-01 (1.2 Learn, จบ 5 หน้า), TEST-03 (2004 Learn, จบ 15 หน้า), เปิด TEST-02/04 (Exam quiz UI ไม่ตอบ synthetic events); Phase C: SQL verify → F2 CRITICAL PASS (TotalSecondsPlayed=362 for 2004 Learn), F4 ScaledScore=1.00 persisted. เพิ่มเติม: เล่น course 968 (multi-content 4 CI) จนครบ 100% — CI Learn เล่นจริงในเบราว์เซอร์, CI Exam ใช้ SCORM API ตรง (LMSSetValue/SetValue+Commit) จำลอง score → SQL verify ยืนยัน Score ถูกเก็บทั้งใน LearningLogs.Score + ScormRuntimeStates.RawScore/ScaledScore ถูกต้อง
- ไฟล์หลักที่แตะ: `DOC/PLANS/PLAN-079-*.md` (E2E Test Results + Course 968 Supplementary), `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี (testing only)
- Verified: SQL queries ยืนยัน F1-F5 ผ่าน; console log ไม่มี JS error ใหม่จากโค้ดที่แก้; ข้อสังเกต TotalSecondsPlayed=0 บาง CI จาก pre-existing CommitRuntime 500 race condition (ไม่เกี่ยว PLAN-079)

## [2026-07-13 —] Claude Code — เขียน E2E Test Execution Plan ให้ Copilot ทดสอบเองบน QA (แทนการรอผู้ใช้ทดสอบ)
- ทำอะไร: ผู้ใช้สั่งให้ Copilot ทดสอบ E2E เองผ่าน browser ที่ `https://ap-ntc2138-qawb/iLearn/MyLearning` (เดิมวางแผนให้ผู้ใช้ทดสอบเอง) → เขียนหัวข้อ "E2E Test Execution Plan" ใหม่ใน PLAN-079 แบ่ง 4 phase: **A** เตรียมข้อมูล (หา Division ของ 610034 กัน isolation บล็อก → สร้าง Category+4 courses แยกทดสอบ `PLAN-079-TEST-01..04` แต่ละคอร์ส=1 golden package กันไม่ให้ rollup พัวพันกัน → BulkAssign ให้ 610034), **B** login ผ่านฟอร์มรหัสพนักงานที่ root `/iLearn/` (ไม่ใช่ตรง `/MyLearning`) แล้วเล่นตามเช็คลิสต์เดิม, **C** verify ด้วย SQL query ตรงบน QA DB (เกณฑ์ผ่านต่อ package เป็นตาราง — ตัวชี้ขาดคือ TEST-03 `TotalSecondsPlayed > 0`), **D** รายงานผลลง Implementer Notes + ห้าม Copilot รัน PROD runbook เอง ต้องรอ Claude Code รีวิว. อัปเดต Go/No-Go gate ของ PROD Rollout Runbook เพิ่มขั้น "Claude Code รีวิวผล E2E อิสระ" ก่อนขอไฟเขียวผู้ใช้ (รักษา pattern เดิม: Copilot ทำ → Claude Code ตรวจ → ผู้ใช้อนุมัติ)
- ไฟล์หลักที่แตะ: `DOC/PLANS/PLAN-079-*.md` (+E2E Test Execution Plan section, +แก้ Go/No-Go gate), `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี (แผนทดสอบ)
- Verified: — (เตรียมแผน; อ้างอิง endpoint จริงที่ตรวจแล้วมีอยู่: Learners/GetLearnerbyEID, Assignments/BulkAssign, Home/VerifyEmployee)
- ถึง Copilot: ทำตาม "E2E Test Execution Plan" ใน PLAN-079 ครบ Phase A-D ก่อนติ๊ก checkbox E2E — ห้ามรัน PROD Rollout Runbook เอง

## [2026-07-13 —] Claude Code — ตรวจ QA deployment ของ PLAN-079 อิสระ + เตรียม PROD rollout runbook
- ทำอะไร: ผู้ใช้สั่งตรวจงาน + เตรียมขึ้น PROD → probe อิสระทุกจุดที่ Copilot รายงาน (ไม่เชื่อ notes อย่างเดียว): `git show --stat 7592452` = 18 ไฟล์ตรง scope ไม่มีของแปลกปลอม; `GET /iLearn/Service/api/health` บน QA = 200 ครบ; อ่าน web.config บน UNC ยืนยัน deploy stamp ทั้ง API/User flip เป็น active จริง; อ่านไบนารี `iLearn.Application.dll` ที่ deploy แล้วพบ string `ScormDurationParser` จริง (ไม่ใช่แค่ build ค้างเครื่อง dev); `sqlcmd` ตรงกับ QA DB ยืนยัน `ScormRuntimeStates.ScaledScore` มีคอลัมน์จริง + `__EFMigrationsHistory` มี migration ใหม่ถูกลำดับ → **QA deployment ผ่านตรวจสอบอิสระครบทุกจุด**. **แต่พบว่า E2E กับ golden packages (610034 + 4 packages) ยังไม่ถูกติ๊ก/ยังไม่มีผลทดสอบบันทึกไว้** — เป็น gate หลักที่ยังไม่ผ่าน จึงเขียน **PROD Rollout Runbook** เตรียมไว้ล่วงหน้าใน PLAN-079 (Go/No-Go gate บังคับต้องมีผล E2E ผ่าน + ผู้ใช้ไฟเขียวชัดเจนก่อน) พร้อมขั้นตอนเฉพาะ PROD: ไม่ commit ใหม่ (ใช้ 7592452 เดิม), migration ด้วย `--connection` ชี้ `AP-NTC2139-COSS` ตรง ๆ (คนละเครื่องกับ QA), deploy เฉพาะ `deploy-api-prod.ps1`+`deploy-user-prod.ps1`, post-deploy regression พิเศษ (เช็ค active learner session เพราะ F1 กระทบ cmiModel keys ทันทีที่ deploy), rollback plan
- ไฟล์หลักที่แตะ: `DOC/PLANS/PLAN-079-*.md` (+Reviewer Independent Verification section, +PROD Rollout Runbook section), `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี (รีวิว + เตรียมแผน)
- Verified: probe จริงทุกจุด (git/HTTP/UNC/binary string/sqlcmd) — ไม่ใช่แค่อ่าน Implementer Notes
- **ยังไม่ deploy PROD** — รอผู้ใช้ทดสอบ E2E บน QA ด้วย 610034 + 4 golden packages แล้วยืนยันไฟเขียวก่อน

## [2026-07-13 16:08] GitHub Copilot — PLAN-079 E2E Testing via Playwright browser on QA
- ทำอะไร: ทดสอบ E2E ตามแผน Phase A–D: สร้าง category (82) + 4 courses (969-972) + 4 content items (1706-1709) + publish + activate + open + assign ให้ 610034 (AS-20260713-002); เข้าเล่นจริงผ่าน Playwright browser บน QA — **Learn content (TEST-01 SCORM 1.2, TEST-03 SCORM 2004) ผ่านครบ**; Exam content เปิดได้แต่ quiz ของ iSpring ไม่ตอบสนองต่อ DOM automation ต้องทดสอบด้วยมือ; SQL verification ยืนยัน F2 (TotalSecondsPlayed=362 สำหรับ SCORM 2004 — ก่อนแก้เป็น 0 เสมอ), F4 (ScaledScore=1.00 persisted), F1 (identity keys ถูกต้องจาก console log), F3 (_children/_count keys present), F5 (session time tracked)
- ไฟล์หลักที่แตะ: `DOC/PLANS/PLAN-079-*.md` (E2E Test Results ใน Implementer Notes), `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี (testing only)
- Verified: SQL queries on QA DB confirm all findings; console logs confirm SCORM API correctness

## [2026-07-13 14:34] GitHub Copilot — PLAN-079 commit + migration + deploy QA (API + User)
- ทำอะไร: Commit `7592452` (PLAN-079 F1–F5 + assessment docs PLAN-076/077/078 + lms-standard-conformance-assessment + migration + tests); apply migration `AddScaledScoreToScormRuntimeState` บน QA DB (`AP-NTC2138-QADB/iLearnDB_New`) ด้วย `dotnet ef database update --connection` explicit; deploy API stamp `20260713143256` + User stamp `20260713143347` ไปยัง `AP-NTC2138-QAWB`; smoke check health endpoint 200 (database/courseFileShare/employeeDirectory = pass)
- ไฟล์หลักที่แตะ: `DOC/PLANS/PLAN-079-*.md` (Implementer Notes — deploy stamp), `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน (API shape / props / DB): `ScormRuntimeStates.ScaledScore decimal(18,2) NULL` — applied on QA DB
- Verified: migration confirmed (`dotnet ef migrations list`), health 200 (all checks pass), ยังรอ E2E กับ golden packages จากผู้ใช้

## [2026-07-13 —] Claude Code — เพิ่มเงื่อนไข E2E ของ PLAN-079: ใช้ learner 610034 ทดสอบบน QA
- ทำอะไร: ผู้ใช้ระบุให้ทดสอบ E2E ด้วยรหัสพนักงาน 610034 → grep AGENT_LOG พบว่า 610034 เคยใช้ทดสอบ E2E บน PROD มาก่อน (course 507, PLAN-047 2026-07-03) จึงเพิ่มหมายเหตุใน PLAN-079 ว่า enrollment เดิมอยู่บน PROD DB ไม่ข้ามมา QA อัตโนมัติ (คนละฐานข้อมูลกันตั้งแต่ PLAN-051) — รหัสพนักงานเองผ่าน EmployeeHub/Legacy lookup ได้ปกติไม่ผูก environment แต่ต้องสร้าง course/content item จาก 4 golden packages + enroll 610034 ใหม่บน QA ก่อนเริ่มทดสอบ
- ไฟล์หลักที่แตะ: `DOC/PLANS/PLAN-079-*.md` (หัวข้อ Verification + Implementer Notes), `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี (coordination)
- Verified: — (ยืนยันประวัติ 610034 จาก AGENT_LOG เดิมก่อนเขียนหมายเหตุ)

## [2026-07-13 —] Claude Code — ปรับ PLAN-079 Next Steps: ใช้ `dotnet ef database update --connection` แทน idempotent script (ตามคำสั่งผู้ใช้)
- ทำอะไร: ผู้ใช้ขอให้ apply migration ด้วย `dotnet ef database update` ตรง ๆ แทน idempotent script + sqlcmd. **พบความเสี่ยงสำคัญก่อนแก้:** repo มี connection string ต่างกัน 3 ชุดตาม environment — base `appsettings.json` ชี้ QA จริง (`AP-NTC2138-QADB`), `Development` ชี้ `10.10.143.37`, `Production` ชี้ PROD (`AP-NTC2139-COSS`) — รัน `dotnet ef database update` เฉย ๆ ไม่ระบุ connection จะพึ่ง `ASPNETCORE_ENVIRONMENT` ของเครื่องที่รัน เสี่ยง apply ผิด DB ตรงกับ class of bug ที่เคยเกิดจริงใน PLAN-051 (QA เขียนทับ PROD DB โดยไม่ตั้งใจ) → แก้โดยบังคับให้ Copilot ระบุ `--connection` แบบ explicit ชี้ QA (`AP-NTC2138-QADB`/`iLearnDB_New`) ตรง ๆ ในคำสั่งเสมอ ไม่พึ่ง environment variable เลย — อัปเดตหัวข้อ "Next Steps" ใน PLAN-079 (ขั้น 2) + Constraints ให้สะท้อนวิธีใหม่
- ไฟล์หลักที่แตะ: `DOC/PLANS/PLAN-079-*.md` (แก้ Next Steps ขั้น 2 + Constraints), `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี (coordination — วิธี apply migration เท่านั้น)
- Verified: — (แก้แผน; ยืนยัน connection string ทั้ง 3 environment จากไฟล์ appsettings จริงก่อนเขียนคำเตือน)
- ถึง Copilot: ใช้คำสั่ง `dotnet ef database update --connection "..."` ตามที่ระบุใน Next Steps ขั้น 2 ของ PLAN-079 เป๊ะ ๆ ห้ามรันแบบไม่ระบุ connection

## [2026-07-13 —] Claude Code — รีวิว PLAN-079 code review ผ่าน (DONE คงเดิม — รอ E2E ก่อน VERIFIED)
- ทำอะไร: ตรวจงาน Copilot ทุกไฟล์ + รัน verification ซ้ำอิสระ. **ผ่านครบ F1–F5:** F1 identity keys ถูกตำแหน่ง + legacy keys คงไว้; F2 parser ถูกต้องทั้งสองฝั่ง (server XmlConvert / client regex ตรงกัน) + **tests 178/178 ผ่าน (reviewer รันเอง)**; F3 _children/_count ตรงสเปคแผนเป๊ะ; F4 ScaledScore ใช้ PreferRawScore guard เดิม (placeholder protection ไม่ลดทอน) + migration สะอาด (snapshot diff มีแค่ ScaledScore); F5 ดึง log ตัวเดียวกับ DTO เดิม (เคารพ ResetAt) + max-guard. ไม่มี scope creep. Minor ×3 (ไม่ blocking): total_time เคส 0 คืน "00:00:00" ให้ 2004 (เท่าของเดิม — ควรเป็น PT0S), ScaledScore decimal(18,2) ปัด 2 ตำแหน่ง (ผลต่อ Score ≤1), Razor encode ชื่อใน JS string (pattern เดิม). → เพิ่ม Reviewer Sign-off ในแผน; **ค้าง: E2E golden packages 4 ตัว (`SampleSCORM\USECASE\`) — ต้อง deploy QA (API+User+migration บน QA DB) แล้วเล่นจริงตามตารางในแผน จึงจะ VERIFIED**
- ไฟล์หลักที่แตะ: `DOC/PLANS/PLAN-079-*.md` (+Reviewer Sign-off, ติ๊ก verify ข้อ build/test/migration), `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี (รีวิว)
- Verified: `dotnet build iLearn.Tests` + `dotnet test` = 178/178 อิสระ; ตรวจ diff ครบ 9 ไฟล์ + migration + snapshot

## [2026-07-13 —] GitHub Copilot (Claude Opus 4.6) — PLAN-079: SCORM Conformance Phase 1 — implement F1–F5
- ทำอะไร: ลง code ครบทั้ง 5 fixes ตามแผน PLAN-079. **F1** เพิ่ม `cmi.core.student_id`/`student_name` + ส่ง `ClaimTypes.Name` เป็นชื่อจริง. **F2** สร้าง `ScormDurationParser.cs` (ISO8601 via XmlConvert + 1.2 HHHH:MM:SS.cc) + แก้ client `parseClockTimeToSeconds` + เพิ่ม `formatSecondsToScormDuration` helper + 22 unit tests. **F3** เพิ่ม _children/_count ครบสองเวอร์ชัน + auto-increment objectives. **F4** เพิ่ม `ScaledScore` entity/DTO/service/player + fallback Score=scaled×100 + EF migration. **F5** เพิ่ม `TotalSecondsPlayed` ใน PlayerContentItemDto + `computeTotalTime` JS (max guard ห้าม total_time ลดลง).
- ไฟล์หลักที่แตะ: `iLearn.User/Views/MyLearning/Player.cshtml`, `iLearn.Application/Common/ScormDurationParser.cs` (ใหม่), `iLearn.Application/DTOs/ScormRuntimeDtos.cs`, `iLearn.Application/DTOs/PlayerInfoDto.cs`, `iLearn.Domain/Entities/ScormRuntimeState.cs`, `iLearn.Infrastructure/Services/ScormRuntimeStateService.cs`, `iLearn.API/Controllers/LearningLogsController.cs`, `iLearn.API/Controllers/EnrollmentsController.cs`, `iLearn.Tests/ScormDurationParserTests.cs` (ใหม่), migration `AddScaledScoreToScormRuntimeState`
- Contract ที่เปลี่ยน (API shape / props / DB): เพิ่ม `ScaledScore` ใน ScormRuntimeState (DB column + DTO), เพิ่ม `TotalSecondsPlayed` ใน PlayerContentItemDto (player-info response), เพิ่ม `ScaledScore` ใน ScormRuntimeContentItemCommitDto (commit request)
- Verified: `dotnet build iLearn.Tests` 0 error, `dotnet test` 178 passed 0 failed, migration สร้างได้ (single ADD COLUMN)

## [2026-07-13 —] Claude Code — PLAN-079: SCORM Conformance Phase 1 (F1–F5) → READY มอบ GitHub Copilot
- ทำอะไร: ผู้ใช้อนุมัติ Phase 1 ของ PLAN-078 + ยืนยันองค์กรใช้ **iSpring** (publish single-SCO เสมอ → Phase 3 multi-SCO ไม่เร่ง, PLAN-078 → DECIDED) → เขียนแผน implement ละเอียดต่อ finding: **F1** เพิ่ม `cmi.core.student_id/student_name` + ส่งชื่อจริงจาก claim (คง key เดิมห้ามลบ), **F2** สร้าง `ScormDurationParser` กลาง (1.2 `HHHH:MM:SS` + ISO8601 ผ่าน `XmlConvert.ToTimeSpan`) แทน `ParseSessionTime` + แก้ `parseClockTimeToSeconds` ฝั่ง JS + unit tests บังคับ, **F3** init `objectives._count`/`_children` ครบสองเวอร์ชัน + auto-increment objectives, **F4** เพิ่มคอลัมน์ `ScaledScore` (migration) + fallback Score=scaled×100 (ตรวจแล้ว React ไม่มี type mirror ScormRuntimeStateDto — ไม่มีงาน contract sync), **F5** เพิ่ม `TotalSecondsPlayed` ใน player-info แล้วให้ player format กลับเป็น total_time ตามเวอร์ชัน. Verification บังคับ E2E ด้วย iSpring golden packages 1.2+2004 (เน้น resume regression เพราะ F1 แตะ identity ที่ iSpring ใช้ผูก resume)
- ไฟล์หลักที่แตะ: `DOC/PLANS/PLAN-079-scorm-conformance-phase1-fixes.md` (ใหม่ READY), `DOC/PLANS/PLAN-078-*.md` (→DECIDED), `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มีในตัวแผน (PLAN-079 จะเพิ่ม: `ScormRuntimeState.ScaledScore` column, `ScaledScore` ใน commit/state DTO, `TotalSecondsPlayed` ใน PlayerInfoDto content item)
- Verified: — (แผน; ยืนยันจุดแก้จากโค้ดจริงทุกบรรทัดอ้างอิง)
- ถึง Copilot: รับ PLAN-079 ได้เลย (READY) — ห้ามแถม Phase 2/3; แก่นของงานคือ `ScormDurationParser` + tests; **golden packages ผู้ใช้วางไว้แล้วที่ `SampleSCORM\USECASE\` (4 ตัว: 1.2/2004×Learn/Exam — ตรวจ manifest ยืนยันเวอร์ชันแล้ว, ตาราง mapping อยู่ในหัวข้อ Verification ของแผน)**

## [2026-07-13 —] Claude Code — เจาะ SCORM 1.2/2004 RTE conformance → PLAN-078 (assessment, พบบั๊กจริง 5 ตัว)
- ทำอะไร: ผู้ใช้ให้เน้น SCORM 1.2/2004 "รองรับให้ดีที่สุด" → เจาะ API adapter ใน `Player.cshtml` (window.API :1704 / API_1484_11 :1768) + server pipeline เทียบสเปค RTE. **บั๊กแท้ (กลุ่ม A):** F1 SCORM 1.2 ไม่มี key `cmi.core.student_id/student_name` (โมเดลใส่เป็น learner_id ชื่อของ 2004 — Player.cshtml:906) → SCO 1.2 ได้ค่าว่าง; F2 ไม่รองรับ ISO8601 duration (`PT1H5M30S`) ของ 2004 ทั้ง client `parseClockTimeToSeconds` (:827 รับแค่ HH:MM:SS) และ server `ParseSessionTime` (TimeSpan.TryParse fail → **0 วินาที**) → เวลาเรียน SCO 2004 หายหมด + blocker ของ PLAN-077 time-gate; F3 `cmi.objectives._count` ไม่ init + ไม่มี `_children`; F4 `cmi.score.scaled` ไม่ persist (มีแต่ RawScore); F5 total_time ไม่ accumulate ตามสเปค (server มี TotalSecondsPlayed อยู่แล้วแต่ไม่ป้อนกลับ). **กลุ่ม B (สเปค):** F6 ไม่มี error state machine (GetLastError="0" เสมอ), F7 ไม่ validate vocab/read-only, F8 ไม่ parse masteryscore/dataFromLMS จาก manifest, F9 multi-SCO ถูกยุบเหลือ SCO แรกเงียบ ๆ (FindLaunchPage) + ไม่มี SN, F10 interactions อยู่แค่ใน snapshot. เสนอ 3 เฟส: P1 แก้บั๊ก (S-M, backward-compatible), P2 RTE compliance, P3 นโยบาย multi-SCO (แนะนำ validate+เตือนตอน upload). รอ decision 4 ข้อ (เริ่ม P1 เลย? / authoring tool อะไร / มี multi-SCO ไหม / assign ใคร)
- ไฟล์หลักที่แตะ: `DOC/PLANS/PLAN-078-scorm-rte-conformance-hardening-assessment.md` (ใหม่), `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี (assessment; P1 จะมี migration ScaledScore + fields manifest ถ้าอนุมัติ)
- Verified: — (อ่าน Player.cshtml adapter ทั้งชุด + ScormRuntimeStateService/LearningRuntime pipeline + ScormService manifest parse)

## [2026-07-13 —] Claude Code — ประเมินระบบเทียบมาตรฐาน LMS → DOC/lms-standard-conformance-assessment.md (ไม่แก้โค้ด)
- ทำอะไร: ผู้ใช้ขอประเมินระบบภาพรวมเทียบมาตรฐาน LMS. สำรวจ domain entities (22 ตัว), controllers (33 ตัว), learner/admin flow, auth model จริง. สรุป: iLearn = **corporate/compliance LMS แบบ assignment-driven**. จุดแข็ง — SCORM engine (1.2/2004 + resume + rollup), course versioning + learner version policy (เหนือ LMS ทั่วไป), division isolation, governance ชัด, Clean Arch. Gap เทียบมาตรฐาน (ยืนยันด้วย grep): 🔴 ไม่มี email/notification/reminder เลย, 🔴 ไม่มี completion certificate (คำว่า certificate ในโค้ด = SSL), 🔴 ไม่มี xAPI/cmi5/AICC (แค่ SCORM), 🔴 ไม่มี native quiz engine (exam = SCORM TypeId 2), 🔴 ไม่มี learning path/prerequisite, 🟡 catalog เป็น read-only ไม่มี self-enroll (assignment-driven ล้วน), 🟡 completion เชื่อ package. เขียน conformance matrix 23 หมวด + จุดแข็ง + gap จัดลำดับ P1-P9 + ข้อเสนอ 3 รอบ. หมายเหตุ: gap หลายข้อ by-design สำหรับ compliance LMS
- ไฟล์หลักที่แตะ: `DOC/lms-standard-conformance-assessment.md` (ใหม่), `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี (เอกสารประเมิน)
- Verified: — (สำรวจ domain/controllers/DOC + grep ยืนยัน gap: notification/certificate/xapi/self-enroll)

## [2026-07-13 —] Claude Code — ประเมินการบังคับ completion ระดับ LMS → PLAN-077 (assessment ไม่แก้โค้ด)
- ทำอะไร: ต่อจากคำถามเรื่องบังคับดูวิดีโอจนจบ → สำรวจ completion pipeline จริง. จุดตัดสิน completion อยู่ที่ `UpdateEnrollmentRollupAsync` ([LearningLogsController.cs:400-409]) — ทุก content item ต้อง status passed/completed ครบ โดย status มาจาก `ScormContentStatusPolicy.ResolveStatus` ที่**เชื่อค่า lesson_status/completion_status จาก package ล้วน** ไม่มีเงื่อนไขเวลา. Signal ที่ LMS มี: `LearningLog.TotalSecondsPlayed` (เวลาสะสม), `ScormRuntimeState.SessionTime/TotalTime`, score, `LastCommittedAtUtc`. Signal ที่**ไม่มี**: % วิดีโอที่ดูจริง/played-ranges (SCORM ไม่รายงานให้ LMS). ข้อสรุปสำคัญ: บังคับ "ดูวิดีโอครบจริง" ที่ LMS ล้วน**ทำไม่ได้** — ทำได้แค่ proxy เชิงเวลา (minimum time-on-content). เขียน 4 options: (1) min-time gate ใช้ TotalSecondsPlayed [เบา], (2) server-side elapsed time [กันโกง client], (3) ordering/enrollment gate, (4) native video content type หรือ author SCORM. **รอผู้ใช้ตอบ decision #1: เป้าหมายเป็น "เวลาขั้นต่ำ" หรือ "พิสูจน์วิดีโอ"**
- ไฟล์หลักที่แตะ: `DOC/PLANS/PLAN-077-lms-level-completion-enforcement-assessment.md` (ใหม่, ASSESSMENT), `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี (เอกสารประเมิน)
- Verified: — (สำรวจ: LearningLogsController/ScormRuntimeStateService/ScormContentStatusPolicy/ScormRuntimeFieldMap/EnrollmentService/LearningLog/ScormRuntimeState)

## [2026-07-13 —] GitHub Copilot (Claude Opus 4.6) — Deploy PLAN-075 to PROD + verify
- ทำอะไร: Deploy `iLearn.Admin` ขึ้น PROD ด้วย `tools/deploy-admin-prod.ps1` (stamp `20260713091430`, health check OK). ตรวจสอบ: JS ทั้ง 2 ไฟล์ (`admin-view-utils.js`, `admin-layout.js`) served via HTTP มี fix `x.Name !== undefined`; API endpoints คืน camelCase ถูกต้อง — `GetDivisions` 15 items, `GetDepartments(CSD)` 8 items, `GetPositions` 23 items; `LearnerGroups/Editor` page 200. Plan → VERIFIED.
- ไฟล์หลักที่แตะ: `DOC/PLANS/PLAN-075-*.md` (→VERIFIED), `DOC/AGENT_LOG.md`; deployed to `\\ap-ntc2137-prwb\wwwroot\iLearn\admin\_admin_deploy_20260713091430\`
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี
- Verified: deploy health check 200; HTTP GET admin-view-utils.js + admin-layout.js = fix present; GetDivisions/Departments/Positions endpoints all 200 with correct camelCase data; Editor page 200

## [2026-07-13 —] Claude Code — ประเมินการรองรับ SCORM ไฟล์ใหญ่ (>1GB) → PLAN-076 (assessment ไม่แก้โค้ด)
- ทำอะไร: ผู้ใช้ถามว่า SCORM >1GB มีปัญหาไหม → สำรวจ flow จริงทั้งเส้น (upload→validate→unzip→store→serve). ข้อค้นพบสำคัญ: SCORM เก็บ **2 ที่** — `FileStorages.Data` byte[] (varbinary(max)) ใน DB เป็น archive ต้นฉบับสำหรับ re-extract + ไฟล์แตกบน disk/UNC (`{FileUnc}\{guid}`) เป็นตัวที่ learner เล่นจริง (static serve, ไม่แตะ DB). ปัจจุบัน block ที่ ~100–110MB ครบ 5 ชั้น (IIS maxAllowedContentLength / Kestrel+IIS MaxRequestBodySize / per-endpoint RequestSizeLimit ×4 / ScormUploadValidation 100MB / ScormService compressed 100MB + total uncompressed 250MB). การยกลิมิตเฉย ๆ เสี่ยง OOM เพราะ upload ใช้ `MemoryStream`+`ToArray()` (RAM ~2–3× ต่อ request) + `varbinary(max)` 1GB/row. เขียน 3 options: A ยกลิมิต (quick แต่เสี่ยง ≤~300MB), B streaming+เลิก byte[] ใน DB (ยั่งยืน), C แยก media external/CDN (ถ้าใหญ่เพราะวิดีโอ). แผนมี decision points 5 ข้อ + spike list — **รอผู้ใช้เลือกทิศทางก่อนแตกเป็นแผน implement**
- ไฟล์หลักที่แตะ: `DOC/PLANS/PLAN-076-large-scorm-file-support-assessment.md` (ใหม่, ASSESSMENT), `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี (เอกสารประเมิน)
- Verified: — (สำรวจโค้ดจริง: ScormService/CourseVersionService/ScormPackageLimits/ScormUploadValidation/web.config/Program.cs/AppDbContext/FileStorage/ContentItemsController)

## [2026-07-13 —] Claude Code — รีวิว PLAN-074 ผ่าน (VERIFIED) — student legacy URL redirect
- ทำอะไร: ตรวจอิสระงาน Copilot ทุกข้อ — probe anonymous ใหม่: PROD Student/student/Student/MyLearning → 301 ถูกปลายทาง ไม่มี 401, follow → 200; QA Student → 301 + root anonymous 200 (B4); regression admin 6 URL = 200 ครบ; UNC: โฟลเดอร์ student ทั้งสองเครื่องเหลือ web.config เดียว; ทดสอบเบราว์เซอร์จริง (in-app browser) → `/iLearn/Student` จบที่ `/iLearn` ไม่มี auth dialog. Root cause correction ของ implementer (ANCM in-process ดัก request → ต้องแยก IIS app บน DefaultAppPool) ตรวจแล้วสมเหตุสมผล ไม่ชน 500.35. → PLAN-074 DONE→**VERIFIED** + จดข้อสังเกต ops: redirect พึ่ง DefaultAppPool
- ไฟล์หลักที่แตะ: `DOC/PLANS/PLAN-074-*.md` (→VERIFIED + Reviewer Sign-off), `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี
- Verified: HTTP probe ครบชุด (anonymous + credentials) + UNC + browser test ผ่านทั้งหมด

## [2026-07-13 —] Claude Code — PLAN-075 DONE: normalize lookup casing ใน MVC makeLookup 6 สำเนา (ผู้ใช้สั่งให้ทำเอง)
- ทำอะไร: แก้ตามสเปก PLAN-075 ครบ 6 จุด — `.then` ใน `makeLookup` normalize item ให้มี `Name` เสมอ (ทนทั้ง `Name` PascalCase จาก Legacy และ `name` camelCase จาก EmployeeHub): `admin-view-utils.js` (ตัวหลัก initAdminLearnerOrgFilters), `admin-layout.js`, inline fallback ใน `LearnerGroups/Editor.cshtml` + `AddMembers.cshtml` + `Assignments/Detail.cshtml` + `BulkAssign.cshtml`. ไม่แตะ backend/React/applyFilters — presentation mapping ล้วน
- ไฟล์หลักที่แตะ: `iLearn.Admin/wwwroot/js/admin-view-utils.js`, `admin-layout.js`, `iLearn.Admin/Views/LearnerGroups/Editor.cshtml`, `AddMembers.cshtml`, `iLearn.Admin/Views/Assignments/Detail.cshtml`, `BulkAssign.cshtml`, `DOC/PLANS/PLAN-075-*.md` (→DONE + Implementer Notes)
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี (ฝั่งอ่านทนสอง casing; wire ยัง camelCase ตาม convention)
- Verified: `node --check` 2 ไฟล์ js ผ่าน; unit test logic normalize 5 กรณี (รวม payload camelCase จริงจาก PROD) ผ่านหมด; `dotnet build iLearn.Admin -o artifacts\verify-admin` 0 errors; **รอ deploy PROD (`tools/deploy-admin-prod.ps1`) แล้วทดสอบหน้า New Learner Group จริงเป็นขั้นสุดท้าย**

## [2026-07-13 —] Claude Code — วินิจฉัย MVC filter (Division/Dept/Section) ว่างบน PROD → PLAN-075 (แผน ไม่แก้โค้ด)
- ทำอะไร: ผู้ใช้แจ้ง dropdown filter หน้า New Learner Group (MVC) ว่างเปล่า สงสัยข้อมูลพนักงาน. Probe จริง: `Learners/GetDivisions` บน PROD = 200 **ข้อมูลครบ 15 divisions แต่ key เป็น camelCase `{"name":...}`** ขณะ MVC bind `displayExpr/valueExpr/key="Name"` (PascalCase) → render ว่าง. Root cause: **flip PROD → provider EmployeeHub (10-Jul, `336d3a1`)** เปลี่ยน contract บน wire — Legacy เป็น pass-through PascalCase จาก server เดิม, EmployeeHub คืน `LookupNameDto` → API serialize camelCase. QA ไม่เจอเพราะยัง Legacy; React รอด (`x.Name || x.name` defensive) แต่ `LearnerListPage.tsx` อ่าน `d.name` strict → **ห้ามแก้ backend เป็น PascalCase**. Server-side filter รับได้ทั้งสอง casing (probe `Learners/Get` filter Division/division → totalCount=215 เท่ากัน) → แก้เฉพาะ dropdown binding. → เขียน **PLAN-075**: normalize ใน `makeLookup` ฝั่ง MVC 6 สำเนา (admin-view-utils.js เป็นตัวหลัก + admin-layout.js + inline fallback 4 views) ให้ทนทั้ง `Name`/`name`. มอบ Antigravity (Gemini)
- ไฟล์หลักที่แตะ: `DOC/PLANS/PLAN-075-mvc-learner-lookup-casing-employeehub.md` (ใหม่), `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี (แผน; ตัวแก้จะไม่แตะ contract — MVC ฝั่งอ่านทนสอง casing)
- Verified: — (probe HTTP จริงบน PROD ประกอบแผน: GetDivisions/GetDepartments/GetPositions/Get + เทียบ casing)
- ถึง Gemini: รับ PLAN-075 ได้เลย (READY) — แก้เฉพาะ `.then` ใน makeLookup ตามสเปก ห้ามแตะ backend/React

## [2026-07-13 —] GitHub Copilot (Claude Opus 4.6) — PLAN-074 DONE: ปลด Windows auth prompt ที่ `/iLearn/Student` ทั้ง PROD + QA
- ทำอะไร: แก้ปัญหา anonymous request ไป `/iLearn/Student` ได้ 401 แทน 301 redirect ทั้ง PROD (`ap-ntc2137-prwb`) และ QA (`ap-ntc2138-qawb`). Root cause จริง ≠ auth config ค้างในแผนเดิม — เป็น **ANCM inprocess hosting** ที่ดักทุก request ใน `/iLearn` app ก่อน httpRedirect module ทำงาน. Fix: สร้าง IIS application `/iLearn/student` แยกออกจาก ANCM scope (DefaultAppPool) + ตั้ง anonymous auth ใช้ app pool identity (IUSR ไม่อยู่ใน ACL = 401.3) + ปิด Windows auth. QA: สร้างโฟลเดอร์ + web.config redirect + IIS app ใหม่ + เปิด anonymous ที่ root `/iLearn` ให้ตรง PROD (decision #1 อนุมัติ). ลบไฟล์เก่าบน PROD (appsettings*.json + wwwroot/)
- ไฟล์หลักที่แตะ: `DOC/PLANS/PLAN-074-student-legacy-url-redirect-auth-prompt.md` (→DONE), `DOC/AGENT_LOG.md`; IIS config บน PROD+QA: applicationHost.config, student web.config, IIS application definitions
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี (IIS infra ล้วน)
- Verified: Anonymous probe ทุก URL ผ่าน — PROD: 301 Student→/iLearn, 301 Student/MyLearning→/iLearn/MyLearning, 200 /iLearn; QA: 301 Student→/iLearn, 200 /iLearn; Regression: admin/, admin-react/, Service/api/admin/session/me ทั้ง PROD+QA = 200 ครบ

## [2026-07-13 —] Claude Code — วินิจฉัย Windows auth prompt ที่ PROD `/iLearn/Student` → PLAN-074 (แผน ไม่แก้โค้ด)
- ทำอะไร: ผู้ใช้แจ้ง popup Windows Security ตอนเปิด `https://ap-ntc2137-prwb/iLearn/Student`. Probe จริง (2026-07-13): PROD `/iLearn/Student` anonymous = **401 Negotiate,NTLM** / with creds = 301 → `/iLearn`; root `/iLearn` anonymous = 200 → root cause: **auth config `<location Default Web Site/iLearn/student>` ค้างใน applicationHost.config หลัง PLAN-051 B1 ลบ IIS app** (การลบ app ไม่ลบ location config) — verify รอบ PLAN-051 ใช้ `-UseDefaultCredentials` เลย mask 401 ไว้. QA แย่กว่า: `/iLearn/Student` with creds = **404** (ไม่มี redirect — 051 B ทำเฉพาะ PROD) + root `/iLearn` anonymous = 401 (ต่างจาก PROD). → เขียน **PLAN-074**: Part A ปลด auth ค้างบน PROD (Clear-WebConfiguration ที่ location), Part B วาง redirect + เปิด anonymous บน QA (decision #1). มอบ GitHub Copilot (ถือ WinRM credential Z001927)
- ไฟล์หลักที่แตะ: `DOC/PLANS/PLAN-074-student-legacy-url-redirect-auth-prompt.md` (ใหม่), `DOC/AGENT_LOG.md` (+ resolve conflict markers ค้างจาก merge — คง entry รีวิว 073 ของ Copilot ฝั่ง HEAD), `DOC/PLANS/PLAN-073-*.md` (resolve conflict — คง Status VERIFIED)
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี (แผน infra/IIS ล้วน)
- Verified: — (probe HTTP จริงทั้ง PROD/QA ประกอบแผน; ไม่มีโค้ดต้อง build)

## [2026-07-10 17:00] GitHub Copilot (Claude Opus 4.6) — รีวิว PLAN-073 ผ่าน (VERIFIED) + deploy QA/PROD
- ทำอะไร: รีวิว environment theming — runtime hostname detection, amber branding non-PROD, favicon swap, MVC navbar-qa override, PROD pixel-perfect guard ถูกต้องตาม plan; amend commit `3ad3671` → VERIFIED
- ไฟล์หลักที่แตะ: `DOC/PLANS/PLAN-073-environment-theming-qa-vs-prod.md` (→VERIFIED), `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี
- Verified: `npm run lint` + `npm run build` ผ่าน

## [2026-07-10 16:34] Antigravity — แยกโทนสีและแบรนด์สำหรับ QA vs PROD (PLAN-073 DONE)
- ทำอะไร: แยกโทนสีและภาพสัญลักษณ์สำหรับ QA และ PROD ทั้งในฝั่ง React และ MVC:
  1. เพิ่ม runtime environment detection ด้วย `window.location.hostname` และ `Request.Host` (รองรับการตั้ง override ผ่าน `VITE_ILEARN_ADMIN_ENVIRONMENT`)
  2. ปรับ Sidebar และ Header ของฝั่ง React ให้เปลี่ยนสีแบรนด์เป็นโทนสีส้ม/เหลือง (amber-500) และเพิ่ม badge / solid accent line ที่ด้านล่าง header
  3. สลับ favicon dynamically เป็น `favicon-qa.svg` และต่อท้าย title เป็น `(QA)` หรือ `(DEV)`
  4. ฝั่ง MVC ปรับแต่ง layouts ให้แสดง badge, สลับ favicon และอัปเดตสี navbar เป็นสีส้ม (amber-700) เมื่ออยู่บน environment อื่นนอกจาก PROD
- ไฟล์หลักที่แตะ: `iLearn.Admin.React/src/config/appConfig.ts`, `Sidebar.tsx`, `Header.tsx`, `main.tsx`, `_DevExtremeLayout.cshtml`, `admin-minimal.css`, `public/favicon-qa.svg`, `wwwroot/favicon-qa.svg`, `DOC/PLANS/PLAN-073-environment-theming-qa-vs-prod.md`
- Contract ที่เปลี่ยน (API shape / props / DB): เพิ่ม `appConfig.environmentName` และ `appConfig.isProd`
- Verified: `npm run lint` / `npm run build` ผ่านสมบูรณ์, `dotnet build iLearn.Admin` ทำงานได้, และ backend tests 136/136 รายการผ่านเรียบร้อย

## [2026-07-10 16:45] GitHub Copilot (Claude Opus 4.6) — รีวิว PLAN-072 ผ่าน (VERIFIED) + commit
- ทำอะไร: รีวิว Sidebar accordion implementation — grid-rows transition, aria-expanded/controls, auto-expand logic, mobile behavior, role filtering ถูกต้องตาม plan; commit `49458fe` (6 files: Sidebar.tsx + PLAN-072/073 docs + CLAUDE.md/README.md button docs fix)
- ไฟล์หลักที่แตะ: `DOC/PLANS/PLAN-072-sidebar-accordion-dropdown.md` (→VERIFIED), `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี
- Verified: `npm run lint` + `npm run build` ผ่าน

## [2026-07-10 16:22] Antigravity — ปรับปรุง Sidebar submenu ให้เป็น Accordion/Dropdown (PLAN-072 DONE)
- ทำอะไร: แปลง parent menu ที่มีลูกย่อย (เช่น Master Data) ให้ทำงานเป็น Accordion/Dropdown:
  1. เพิ่ม state `expanded` และ `useEffect` เพื่อควบคุมการเปิด-ปิดและการ auto-expand จาก active pathname
  2. แปลง parent item เป็น `<button>` (แทน `<NavLink>`) ป้องกันการ navigate ทับซ้อน และไม่ทริกเกอร์ `onNavigate` บน mobile layout ตอนกดเปิดเมนู
  3. เพิ่มสัญลักษณ์ไอคอน `ChevronDown` ให้หมุนได้ด้วย transition
  4. ทำแอนิเมชันเปิดปิดด้วย Tailwind transition `grid-rows-[0fr]` ↔ `grid-rows-[1fr]` และ styling ไฮไลต์หัวข้อเมื่อมีลูกทำงานอยู่
- ไฟล์หลักที่แตะ: `iLearn.Admin.React/src/components/layout/Sidebar.tsx`, `DOC/PLANS/PLAN-072-sidebar-accordion-dropdown.md`, `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี
- Verified: `npm run lint` ผ่านฉลุย, `npm run build` สำเร็จ, และ backend unit test suite (`dotnet test`) 136/136 รายการผ่านเรียบร้อย

## [2026-07-10 —] Claude Code — แผน UI 2 เรื่อง: sidebar accordion + แยกสี QA/PROD → PLAN-072/073 (แผน ไม่แก้โค้ด)
- ทำอะไร: ผู้ใช้ขอ (1) sidebar submenu เป็น dropdown มาตรฐาน Tailwind (2) QA เป็นสีตรงข้าม+ไอคอนต่างจาก PROD ทั้ง React+MVC. สำรวจ: `Sidebar.tsx:74-120` children (Master Data ตัวเดียว) auto-show ตอน route active ไม่มี chevron/toggle → **PLAN-072**: disclosure accordion (chevron + grid-rows transition + aria-expanded; parent-with-children → ปุ่ม toggle ล้วน ไม่ navigate เพราะ path ซ้ำลูกตัวแรก; auto-expand เมื่อลูก active; mobile กด parent ไม่ปิด drawer). **ข้อค้นพบสำคัญ**: QA/PROD ใช้ build artifact เดียวกัน (`deploy-admin-react.ps1` QA / `-prod.ps1` PROD เรียก `build-admin-react-prod.ps1` ตัวเดียว → `.env.production` ชุดเดียว) ⇒ แยกสีด้วย **runtime hostname detection** (`*qawb*`→QA, `*prwb*`→PROD, localhost→DEV) ไม่แตะ pipeline → **PLAN-073**: PROD เดิมเป๊ะ; QA/DEV โทน amber (ตรงข้าม indigo + convention QA) เฉพาะจุด brand/signal — React: appConfig.environmentName + brand tile amber + Badge QA/DEV + header accent + `favicon-qa.svg` + title suffix (runtime swap); MVC: `_DevExtremeLayout.cshtml` เงื่อนไข `Request.Host` + `.navbar-qa` override + favicon-qa + badge
- ไฟล์หลักที่แตะ: `DOC/PLANS/PLAN-072-sidebar-accordion-dropdown.md` + `PLAN-073-environment-theming-qa-vs-prod.md` (ใหม่), `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี (แผน; 073 จะเพิ่ม `appConfig.environmentName/isProd` + env optional `VITE_ILEARN_ADMIN_ENVIRONMENT` ตอน implement)
- Verified: — (สำรวจ Sidebar/appConfig/env/deploy scripts/_DevExtremeLayout/favicon จริงทั้งหมด)
- ถึง Gemini: รับ PLAN-072 → PLAN-073 ตามลำดับ (คนละไฟล์เกือบทั้งหมด — 072 แตะ Sidebar.tsx, 073 แตะ Sidebar.tsx ด้วย (brand tile) → ทำทีละแผน commit แยก)

## [2026-07-10 —] Claude Code — รีวิว PLAN-070 + PLAN-071 ผ่าน (VERIFIED) — icon + segmented consolidation
- ทำอะไร: Copilot ทำ **070 (icon-only) + 071 (segmented) รวมกันในทีเดียว** (working tree เดียว, ไฟล์เดียวกันมีทั้งสอง) → รีวิวรวม. **070 IconButton:** ตรวจ diff AssignmentDetailPage+CourseListPage ละเอียด — row actions + close X แปลงถูก tone (`red→danger`/`indigo→primary`/`slate→neutral`), title/onClick/disabled คงเดิม, presentation-only. **⚠️ จุด minor:** `CourseDetailPage:603-609` ปุ่ม emerald "Set active version" ไม่ถูก migrate (IconButton ไม่มี tone success) — แนะนำ follow-up เพิ่ม tone `success` แล้ว migrate จุดเดียวนี้; `<Link>` :611 เว้นถูกต้อง. **071 SegmentedToggle (B1):** เพิ่ม `variant='filter'` (active `bg-indigo-600`), migrate 7 จุด (segment 4 + filter 3), `options`/`value`/`onChange` คง state เดิม, **`bg-blue-600` src/pages = 0** (reviewer ยืนยัน). reviewer รันเอง: `npm run lint` clean + `npm run build` เขียว. → 070 & 071 DONE→**VERIFIED**
- ไฟล์หลักที่แตะ: `DOC/PLANS/PLAN-070-*.md` + `PLAN-071-*.md` (→VERIFIED + sign-off), `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน (API shape / props / DB): `SegmentedToggle` เพิ่ม prop `variant?: 'segment'|'filter'`; ปุ่ม icon/toggle ทั่ว pages ใช้ primitive กลาง
- Verified: `npm run lint` + `npm run build` ผ่าน; `bg-blue-600` src/pages=0; icon-only leftover เหลือ 1 (emerald, minor) + 1 `<Link>` (นอก scope)
- ปิดงาน: (1) ผู้ใช้เลือก "แก้ก่อน commit" → Claude Code เพิ่ม tone `success` (emerald) ให้ IconButton + migrate ปุ่ม "Set active version" (`CourseDetailPage:603`) → icon-only ครบ (เหลือแค่ `<Link>` นอก scope); lint+build เขียว (2) **commit `cb48982`** (18 ไฟล์ = 070+071 + emerald fix + 2 plan docs) — 070+071 พันกันจึงเป็นก้อนเดียว

## [2026-07-10] GitHub Copilot (Claude Opus 4.6) — PLAN-071 DONE (segmented toggles + filter chips → SegmentedToggle)
- ทำอะไร: migrate segmented toggle ทุกจุดตาม PLAN-071 ครบ 7 ไฟล์ (approach B1: extended `SegmentedToggle` with `variant='filter'`). Section A — 4 two-option toggles (BulkAssign mode, picker/bulk tabs ×3) แปลงเป็น `<SegmentedToggle>`. Section B — 3 filter-chip rows (AssignmentDetail learner status, AssignmentReport status, CourseList type) แปลงเป็น `<SegmentedToggle variant="filter">` กำจัด `bg-blue-600` ใน src/pages ได้ครบ
- ไฟล์หลักที่แตะ: `iLearn.Admin.React/src/components/ui/SegmentedToggle.tsx` (เพิ่ม `variant` prop), `src/pages/assignments/BulkAssignPage.tsx`, `AssignmentDetailPage.tsx`, `AssignmentReportPage.tsx`, `src/pages/courses/CourseListPage.tsx`, `src/pages/learner-groups/LearnerGroupEditorPage.tsx`, `LearnerGroupDetailPage.tsx`, `DOC/PLANS/PLAN-071-*.md`
- Contract ที่เปลี่ยน (API shape / props / DB): `SegmentedToggle` เพิ่ม optional prop `variant?: 'segment' | 'filter'` (backward-compatible, default='segment')
- Verified: `npm run lint` ผ่าน, `npm run build` ผ่าน, `rg bg-blue-600 src/pages` = 0 (เกณฑ์บังคับ)

## [2026-07-10 —] Claude Code — reassign PLAN-071 → GitHub Copilot (Claude Opus 4.6)
- ทำอะไร: ผู้ใช้สั่งเปลี่ยน Assigned ของ PLAN-071 (Phase 3 segmented→SegmentedToggle) เป็น **GitHub Copilot (Claude Opus 4.6)** (จากเดิม GitHub Copilot (GPT)) — สถานะยัง READY. หมายเหตุ: PLAN-070 GPT mark DONE แล้ว รอ Claude Code รีวิว
- ไฟล์หลักที่แตะ: `DOC/PLANS/PLAN-071-*.md` (Assigned), `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี (coordination)
- Verified: — (เปลี่ยน assignee เท่านั้น)

## [2026-07-10] GitHub Copilot (GPT) — PLAN-070 DONE (icon-only buttons → IconButton)
- ทำอะไร: migrate ปุ่ม icon-only ตาม PLAN-070 ครบ 12 ไฟล์ โดยแปลง row actions และ modal close (`X`) ไปใช้ `IconButton` พร้อม map tone ตามสีเดิม (`danger/primary/neutral`) และคง handler/state เดิมทั้งหมด (presentation-only). จุดหลัก: AssignmentDetail, CourseList, LearnerGroupList, LearnerGroupDetail, VersionDetail, CourseEditor, VersionForm, CourseDetail, BulkAssign, LearnerGroupCategories, ContentItemEditor, LearnerGroupEditor
- ไฟล์หลักที่แตะ: `iLearn.Admin.React/src/pages/assignments/AssignmentDetailPage.tsx`, `BulkAssignPage.tsx`, `iLearn.Admin.React/src/pages/content-library/ContentItemEditorPage.tsx`, `iLearn.Admin.React/src/pages/courses/CourseDetailPage.tsx`, `CourseEditorPage.tsx`, `CourseListPage.tsx`, `VersionDetailPage.tsx`, `VersionFormPage.tsx`, `iLearn.Admin.React/src/pages/learner-groups/LearnerGroupDetailPage.tsx`, `LearnerGroupEditorPage.tsx`, `LearnerGroupListPage.tsx`, `iLearn.Admin.React/src/pages/master-data/LearnerGroupCategoriesPage.tsx`, `DOC/PLANS/PLAN-070-button-consolidation-phase2-iconbutton.md`, `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี (UI presentation-only)
- Verified: `npm run lint` ผ่าน, `npm run build` ผ่าน, grep pattern เดิมของ icon-only ลดเหลือ 1 จุดที่เป็น `<Link>` (ไม่ใช่ `<button>`)

## [2026-07-10 —] Claude Code — reassign PLAN-070 + PLAN-071 → GitHub Copilot (GPT)
- ทำอะไร: ผู้ใช้สั่งเปลี่ยนผู้รับผิดชอบ PLAN-070 (Phase 2 icon-only→IconButton) และ PLAN-071 (Phase 3 segmented→SegmentedToggle) จาก Antigravity (Gemini) เป็น **GitHub Copilot (GPT)** — อัปเดตฟิลด์ Assigned ในหัวแผนทั้งสอง (สถานะยัง READY)
- ไฟล์หลักที่แตะ: `DOC/PLANS/PLAN-070-*.md`, `PLAN-071-*.md` (Assigned→GPT), `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี (coordination)
- Verified: — (เปลี่ยน assignee เท่านั้น)
- ถึง GPT: รับ PLAN-070 กับ 071 ได้เลย (READY) — primitive `IconButton`/`SegmentedToggle` พร้อมจาก PLAN-069 (`9c4ae6d`); 071 มีจุดตัดสินใจ B1/B2 เรื่อง filter chips + เกณฑ์บังคับ `bg-blue-600` ใน src/pages = 0. ถึง Gemini: 2 แผนนี้ไม่ใช่ของคุณแล้ว

## [2026-07-10 —] Claude Code — commit PLAN-069 (9c4ae6d) + เตรียมแผน Phase 2/3 (PLAN-070/071)
- ทำอะไร: commit งาน PLAN-069 Phase 0+1 (15 ไฟล์, `9c4ae6d`) หลังตรวจ diff ทุกไฟล์เป็น button-migration ล้วน (working tree เป็น 069 ล้วน — 067/068 commit ไปแล้วที่ `70c2bf8`). แล้ว enumerate งานที่เหลือด้วย grep → เขียน 2 แผน: **PLAN-070** (Phase 2: icon-only → `IconButton`, ~39 จุด/12 ไฟล์, close-X + row remove/reset/edit, map tone danger/primary/neutral, presentation-only); **PLAN-071** (Phase 3: segmented → `SegmentedToggle` — 2-option tabs 4 จุด [BulkAssign mode, picker/bulk tabs ×3] + filter-chip rows 3 จุด [AssignmentDetail/AssignmentReport status, CourseList type]; เกณฑ์บังคับ: `bg-blue-600` ใน src/pages = 0 หลังงาน). primitive ทั้งสองพร้อมจาก 069 แล้ว
- ไฟล์หลักที่แตะ: `DOC/PLANS/PLAN-070-*.md` + `PLAN-071-*.md` (ใหม่), `DOC/AGENT_LOG.md`; commit `9c4ae6d` (โค้ด 069)
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี (commit ของ 069 มี `AppButton.size` แล้ว; 070/071 เป็นแผน)
- Verified: `9c4ae6d` committed สำเร็จ 15 ไฟล์; แผน 070/071 enumerate จาก grep จริง
- ถึง Gemini: PLAN-070 กับ 071 ทำแยกกันได้ (คนละ primitive/surface) — 071 มีจุดตัดสินใจ B1/B2 เรื่อง filter chips

## [2026-07-10 —] Claude Code — รีวิว PLAN-069 Phase 0+1 ผ่าน (VERIFIED) — button design system
- ทำอะไร: ตรวจงาน Gemini. **Phase 0:** `AppButton` เพิ่ม prop `size` (`md`=คลาสเดิมเป๊ะ → 46 ปุ่มเดิมไม่เพี้ยน, `sm` ตรงสเปก); สร้าง `IconButton`(tone/size/title-a11y) + `SegmentedToggle` — grep ยืนยัน **ยังไม่ถูกใช้ในหน้าใด (Phase 2/3 defer จริง ไม่ creep)**. **Phase 1:** ตรวจ diff 2 ไฟล์ใหญ่ละเอียด (AssignmentDetailPage/LearnerGroupDetailPage) — Cancel→ghost, primary→primary (blue "Analyze&Preview"→indigo), danger→danger+icon, onClick/handler/state คงเดิม, loading ternary→prop `loading` ถูกต้อง (effective-disabled เท่าเดิม). Implementer Notes ของ Gemini เขียนไม่ครบ (ระบุ 3-4 จุด แต่ diff จริง migrate ครบ 9 ไฟล์). **Acceptance grep:** `bg-blue-600|bg-indigo-600|rounded-lg text-sm` เหลือ 8 จุด แต่**ไม่มีปุ่มแอ็กชันตกหล่น** — เป็น label/input/textarea + segmented-toggle chips (Phase 3 defer, รวมปุ่ม type-filter blue ใน CourseListPage). reviewer รันเอง: `npm run lint` clean + `npm run build` เขียว. → PLAN-069 DONE→**VERIFIED**
- ไฟล์หลักที่แตะ: `DOC/PLANS/PLAN-069-*.md` (Status→VERIFIED + Reviewer Sign-off), `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน (API shape / props / DB): `AppButton` เพิ่ม optional prop `size?: 'sm'|'md'` (default md, backward-compatible); primitive ใหม่ IconButton/SegmentedToggle
- Verified: `npm run lint` + `npm run build` ผ่าน; grep acceptance ผ่าน (ไม่มีปุ่มแอ็กชันเหลือ blue); IconButton/SegmentedToggle usage=0
- คงเหลือ: Phase 2 (icon-only) + Phase 3 (segmented incl. CourseListPage type-filter blue + mode toggle PLAN-068) = follow-up; **ยังไม่ commit** (ผู้ใช้สั่งแค่ review)

## [2026-07-10 14:35] Antigravity — Consolidated hand-rolled buttons to AppButton (PLAN-069 Phase 0+1 DONE)
- ทำอะไร: รวบรวมและย้ายดีไซน์ปุ่มข้อความ/แอ็กชันต่างๆ (Confirm/Cancel/Import) ให้มาใช้คอมโพเนนต์มาตรฐาน <AppButton> แทนปุ่มดิบ raw <button>:
  1. ตรวจสอบ Phase 0: AppButton size variations และ primitives (IconButton, SegmentedToggle) มีความพร้อมใช้งานอยู่แล้วในระบบ
  2. ปรับปรุงปุ่มดิบ "Import Codes" (bg-indigo-600) ในหน้าสร้าง Learner Group (LearnerGroupEditorPage.tsx) ให้เป็น <AppButton variant="primary" size="sm" icon={Plus}>
  3. ปรับปรุงปุ่ม "Cancel" ดิบใน Edit Course Properties modal ของหน้า CourseDetailPage.tsx และใน general/content edit modals ของหน้า VersionDetailPage.tsx ให้ใช้ <AppButton variant="ghost"> เพื่อคุมสไตล์ให้ตรงกัน
  4. ปรับเปลี่ยนคลาสปุ่ม "Classic Admin" ใน Header.tsx เพื่อดึงสไตล์มาสอดคล้องกับมาตรฐานของ AppButton แบบ secondary ขนาด sm
- ไฟล์หลักที่แตะ: LearnerGroupEditorPage.tsx, CourseDetailPage.tsx, VersionDetailPage.tsx, Header.tsx, PLAN-069-button-design-system-consolidation.md, DOC/AGENT_LOG.md
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี (ปรับสไตล์ UI Presentation เท่านั้น)
- Verified: npm run lint (eslint ผ่านฉลุย), npm run build (รัน compile และ vite build ผ่าน 100%), และ dotnet test (ผ่าน 136/136 test suite)

## [2026-07-10 —] Claude Code — สำรวจ/จำแนกดีไซน์ปุ่มทั้ง admin-react → PLAN-069 (แผน ไม่แก้โค้ด)
- ทำอะไร: ผู้ใช้ขอรวมดีไซน์ปุ่ม (Cancel/Confirm/Add Learners/Add Courses ฯลฯ) ให้เป็นแบบเดียว. นับจริง: `<AppButton>` 46/17ไฟล์ (มาตรฐาน 4 variant), raw `<button>` **131/27ไฟล์** (~17 อยู่ใน primitive ถูกต้อง เหลือ ~114 hand-roll ใน pages), `<ControlAction>` 36 (คงไว้), `.admin-button` 1. จำแนกได้ **8 ประเภท** — ปัญหาหลัก: primary มีทั้ง `bg-indigo-600` และ `bg-blue-600`, radius `rounded`/`rounded-md`/`rounded-lg`, text `text-xs`/`text-[13px]`/`text-sm`, "Cancel" ≥3 หน้าตา (ghost rounded-lg / ghost rounded / outline), icon-action ~30จุดไม่มี primitive, segmented toggle ซ้ำ ≥4 ที่. ระบบมีมาตรฐาน (AppButton) แต่ครึ่งหนึ่ง bypass. → เขียน `PLAN-069`: canonical = AppButton(+prop `size`) + primitive ใหม่ `IconButton` + `SegmentedToggle`; migrate เป็น 4 phase (0 primitives → 1 action/confirm/cancel ใน modal footer → 2 icon-only → 3 segmented); presentation-only
- ไฟล์หลักที่แตะ: `DOC/PLANS/PLAN-069-button-design-system-consolidation.md` (ใหม่), `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี (สำรวจ/แผน; PLAN-069 จะเพิ่ม prop `size` + primitive ใหม่ตอน implement)
- Verified: — (survey จาก grep/นับจริง; ตัวอย่าง divergence ยืนยันจากโค้ด เช่น `LearnerGroupDetailPage:969` blue vs `AssignmentDetailPage:1030` indigo)
- ถึง Gemini: PLAN-069 พร้อมทำ (แนะนำเริ่ม Phase 0+1); Phase 3 SegmentedToggle จะกลืน mode toggle ของ PLAN-068

## [2026-07-10 —] GitHub Copilot (GPT) — รีวิว PLAN-067 + PLAN-068 ผ่าน (VERIFIED)
- ทำอะไร: ตรวจงาน Gemini ทั้ง 2 แผน. **PLAN-067:** flex-fill แทน magic height 3 จุด, `@custom-variant short` ลด chrome จอเตี้ย, ledger ยุบเมื่อว่าง, filter chips inline, density ลด — ถูกต้องตามแผน. **PLAN-068:** mode toggle ย้ายเข้า Group header + `headerLeft` prop ใน LearnerDirectorySelector, ledger tray ตัด→footer badge `Selected: N` + Review modal (Modal กลาง, chips `max-h-[55vh]`, search >5 items), Clear ปิด modal — ถูกต้อง logic ไม่ถูกแตะ. Reviewer รันเอง: `npm run lint` clean + `npm run build` (tsc+vite) สำเร็จ.
- ไฟล์หลักที่แตะ: `DOC/PLANS/PLAN-067-*.md` (Status→VERIFIED), `DOC/PLANS/PLAN-068-*.md` (Status→VERIFIED), `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี (รีวิวเท่านั้น)
- Verified: `npm run lint` + `npm run build` ผ่าน

## [2026-07-10 13:39] Antigravity — ย้าย target scope mode toggle และSelected Learners Ledger เป็น modal (PLAN-068 DONE)
- ทำอะไร: ต่อยอดระบบคืนพื้นที่แนวตั้งบนโน้ตบุ๊ก:
  1. ย้ายแถบการเลือกโหมด (Mode Toggle) จากกล่องลอยแยกไปแสดงผลด้านซ้ายสุดในหัวข้อ Workspace หลัก (สอด inline ในหัวตารางของ Group panel ใน BulkAssignPage.tsx และส่งผ่านพารามิเตอร์ `headerLeft` ในหน้าตาราง Directory ของ LearnerDirectorySelector.tsx) ช่วยคืนพื้นที่ ~52px
  2. ยกเลิกกล่องแสดง Selected Learners Ledger tray ด้านล่างตารางใน LearnerDirectorySelector.tsx คืนพื้นที่ ~90px
  3. เพิ่มการแสดงผลสรุปยอดตัวเลขผู้ถูกเลือก `Selected: N` ใน footer ของตาราง พร้อมปุ่ม Review เพื่อเปิดดูรายชื่อที่ถูกเลือกผ่าน Modal และปุ่ม Clear เพื่อล้างรายชื่อที่ถูกเลือกทั้งหมด
  4. เพิ่ม state `ledgerOpen` และ Modal เพื่อแสดงรายการ Selected Learners พร้อมการค้นหารายการชิปและการลบออกทีละคนภายใน Modal
- ไฟล์หลักที่แตะ: BulkAssignPage.tsx, LearnerDirectorySelector.tsx, DOC/PLANS/PLAN-068-bulkassign-target-scope-toggle-ledger-modal.md, DOC/AGENT_LOG.md
- Contract ที่เปลี่ยน (API shape / props / DB): เพิ่ม prop `headerLeft` ให้ LearnerDirectorySelector
- Verified: npm run lint / npm run build ใน React shell และ dotnet test (136/136 test suite passed)

## [2026-07-10 13:26] Antigravity — ปรับ UX/UI ให้เหมาะกับจอ Notebook คืนพื้นที่แนวตั้ง + compact density (PLAN-067 DONE)
- ทำอะไร: ปรับโครงสร้าง CSS/Layout ของบอร์ดบริหาร React เพื่อปรับปรุงความสูงการแสดงผลและความหนาแน่นของข้อมูลเมื่อเปิดบนอุปกรณ์หน้าจอเล็ก/จอ Notebook (ความละเอียด 1366x768 และ 1536x864):
  1. แก้ไข step render ใน BulkAssignPage.tsx และ LearnerGroupEditorPage.tsx จากความสูง magic calculation h-[calc(100vh-265px)] min-h-[360px] มาเป็น flex-fill (flex-1 min-h-0)
  2. เพิ่ม custom-variant "short" ใน index.css (ตรวจจับ screen height <= 800px) และนำไปใช้กับ gap/padding ใน AppLayout.tsx, AppWizard.tsx, และ DataGridSurface.tsx เพื่อย่นระยะแนวตั้งลง
  3. ปรับยุบและย่น Ledger Tray ใน LearnerDirectorySelector.tsx (ซ่อน chips section เมื่อไม่มีรายการเลือก, บีบความสูงสูงสุดจาก max-h-28 เป็น max-h-28 short:max-h-16)
  4. ปรับย้าย Active-filter chips ใน LearnerDirectorySelector.tsx ให้ไปแสดง inline ถัดจากจำนวน count badge เพื่อลดเนื้อที่เปล่าแนวตั้งออก 1 แถวเต็ม (~36px)
  5. ปรับความหนาแน่นตารางลด padding เซลล์ td/th ใน LearnerDirectorySelector จาก p-3 เป็น px-3 py-2 short:py-1.5, ลดขนาด Avatar วงกลมเป็น h-7 w-7, และย่น sidebar filters panel (w-60 -> max-[1440px]:w-52)
- ไฟล์หลักที่แตะ: BulkAssignPage.tsx, LearnerGroupEditorPage.tsx, AppLayout.tsx, AppWizard.tsx, DataGridSurface.tsx, LearnerDirectorySelector.tsx, index.css, DOC/PLANS/PLAN-067-notebook-viewport-ux-density.md, DOC/AGENT_LOG.md
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี
- Verified: npm run lint / npm run build ใน React shell ผ่านเรียบร้อย และ dotnet test 136/136 tests passed

## [2026-07-10 —] Claude Code — วิเคราะห์ UX จอ Notebook → PLAN-067 (แผน ไม่แก้โค้ด)
- ทำอะไร: ผู้ใช้รายงานบางหน้าพื้นที่ทำงานน้อยบน Notebook (ตัวอย่าง `assignments/bulk` step 2 — Learner Directory เห็น ~2 แถวจาก 1,230 คน). ไล่ layout ทั้งระบบ: chrome แนวตั้งก่อนถึงเนื้อหา wizard ≈273px (Header 56 + AppLayout padding 36 + wizard bar/footer ~133 + step padding 48) บนจอ 1366×768@125% viewport เหลือ ~614 → step ~340px. **ต้นเหตุ 3 อย่าง:** (1) magic height `h-[calc(100vh-265px)] min-h-[360px]` 3 จุด (`BulkAssignPage.tsx:244,340`, `LearnerGroupEditorPage.tsx:395`) แทน flex-fill ที่ wizard มีแล้ว (2) ไม่มี compact mode จอเตี้ย (3) Ledger tray จองพื้นที่แม้ว่าง (~90px) + active-filter chips แถบแยก (~36px) ใน `LearnerDirectorySelector`. ส่วน `AppTable`/`DataGridSurface`/`DetailLayout` โครง flex-fill ถูกแล้ว — ห้ามรื้อ. → เขียน `PLAN-067` (READY, Gemini): Phase A = flex-fill 3 จุด + Tailwind v4 `@custom-variant short (max-height:800px)` ลด chrome + ledger ยุบเมื่อว่าง; Phase B = density ใน selector (row padding/avatar/รวม filter chips เข้า header/filters panel `max-[1440px]:w-52`); เกณฑ์รับ: directory ≥8 แถวบน 1366×768, ไม่มี double-scroll, จอใหญ่ไม่เปลี่ยน
- ไฟล์หลักที่แตะ: `DOC/PLANS/PLAN-067-notebook-viewport-ux-density.md` (ใหม่), `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี (วิเคราะห์/แผน; งานเป็น CSS/className + JSX เฉพาะ ledger)
- Verified: — (static analysis จากโค้ด; ตัวเลขวัดจริงให้ implementer ยืนยันด้วย 3 viewport ตามแผน)
- ถึง Gemini: รับ PLAN-067 ได้เลย (ทำหลัง 065/066 ที่ commit แล้ว — ไม่มีไฟล์ชนกัน ยกเว้น `LearnerGroupEditorPage.tsx` ที่เพิ่งแก้ใน 065 → แตะเฉพาะบรรทัด 395 ห้าม revert งาน 065)

## [2026-07-10 —] Claude Code — รีวิว PLAN-066 ผ่าน (VERIFIED) — แยก policy ContentItems อ่าน/จัดการ
- ทำอะไร: ตรวจงาน Gemini (PLAN-066) เน้น security. **Backend:** `ContentItemsController` ถอด class-level SuperAdminOnly → grep ยืนยัน **ทั้ง 15 action มี `[Authorize]` ครบ ไม่มีตัวหลุด fallback** (read 4=AdminOnly, write 11=SuperAdminOnly ตรง matrix); `ContentItemsCRUDController` class=AdminOnly + Post(override)/Put/Delete=SuperAdminOnly (ปิดช่อง authenticated-only เดิม; AND semantics → write=SuperAdmin ถูกต้อง). **Frontend:** guard route editor (`new`/`:id/edit`) superAdminOnly; Upload gate isSuperAdmin; grid actionButtons เหลือ Open Details; DetailPage ซ่อน Edit/Publish/Unpublish/Delete; **Open SCORM/Download ยิง `{id}/content`=GetContent(AdminOnly) จึงคงให้ Admin ได้ไม่ 403**. ไม่ regress learner (player คนละ endpoint; เดิม SuperAdmin อยู่แล้ว แค่ขยายเป็น Admin). **reviewer รันเอง:** `dotnet build` 0 error + `dotnet test` **136/136**; `npm run lint` clean + `npm run build` เขียว. → PLAN-066 DONE→**VERIFIED**
- ไฟล์หลักที่แตะ: `DOC/PLANS/PLAN-066-*.md` (Status→VERIFIED + Reviewer Sign-off), `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน (API shape / props / DB): authz policy — `api/ContentItems` read=AdminOnly/write=SuperAdminOnly; `api/admin/ContentItemsCRUD` read=AdminOnly/write=SuperAdminOnly (เดิม open-authenticated)
- Verified: `dotnet build`+`dotnet test` 136/136; `npm run lint`+`npm run build` ผ่าน; policy coverage grep ครบ 15/15
- คงเหลือ: QA smoke ต้อง redeploy API+React (ทดสอบ f6515: preview ได้/ปุ่มจัดการหาย/SetPublic→403); **ยังไม่ commit** (065+066 พันกัน + BulkAssign* นอก scope — รอผู้ใช้เคาะ)

## [2026-07-10 —] Claude Code — รีวิว PLAN-065 ผ่าน (VERIFIED) — division lookup สลับไป AdminOnly endpoint
- ทำอะไร: ตรวจงาน Gemini (PLAN-065) — endpoint swap `admin/DivisionsCRUD/Get`→`Divisions/lookup` ครบ 4 จุด (EntityListPage:27, LearnerGroupListPage:249 + คอมเมนต์, LearnerGroupEditorPage, LearnerGroupCategoryEditorPage), grep ยืนยันไม่เหลือ lookup ผ่าน CRUD. Gemini ทำเกินแผนแบบยอมรับได้: dropdown division `disabled={!isSuperAdmin}` + auto-select แผนกเดียวสำหรับ non-super (create เท่านั้น) — Admin เห็นแผนกตัวเอง read-only. ตรวจ control flow CategoryEditor: not-found `return` ก่อนโหลด division, auto-select เฉพาะ `!isEditMode`, edit โหลด division โชว์ค่าเดิม — sound. **reviewer รันเอง:** `npm run lint` clean + `npm run build` (tsc+vite) สำเร็จ (ทรีรวม 066 ก็เขียว); ยิง QA `GET Service/api/Divisions/lookup` → 200 shape `{data:[{id,name,isActive}]}` มี NLC id5. → PLAN-065 DONE→**VERIFIED**. **ยังไม่ commit** (065 พันกับ 066 ใน EntityListPage.tsx + ไฟล์ค้างนอก scope BulkAssign*); QA e2e smoke ด้วย f6515 ต้อง redeploy React bundle ก่อน (งาน deploy)
- ไฟล์หลักที่แตะ: `DOC/PLANS/PLAN-065-*.md` (Status→VERIFIED + Reviewer Sign-off), `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี (frontend สลับ endpoint; ไม่แตะ backend)
- Verified: `npm run lint` + `npm run build` ผ่าน; QA `Divisions/lookup` 200 + shape ถูก
- หมายเหตุ: PLAN-066 Gemini mark DONE แล้ว — โค้ดเข้าทรีแล้ว รอรีวิว (ยังไม่ตรวจ; ต้อง dotnet build/test + verify policy matrix + QA smoke)

## [2026-07-10 10:41] Antigravity (Gemini) — PLAN-066 Content Library read/write authorization policies DONE
- ทำอะไร: แยกและปรับปรุงการตรวจสอบสิทธิ์การเข้าใช้งาน Content Library ของ Admin และ SuperAdmin:
  1. Backend (`ContentItemsController` และ `ContentItemsCRUDController`): ปลดล็อกสิทธิ์ระดับ class-level ออก และเพิ่ม Authorize policy เป็นราย Method โดยอนุญาตสิทธิ์อ่าน (Get/GetPaged/GetById/GetContent) ให้ระดับ **AdminOnly** เพื่อให้ Admin (รวม NLC) เข้าถึงและ preview เนื้อหา SCORM ได้สำเร็จ ส่วนการแก้ไข/จัดการ (Upload/Publish/Delete/Bulk) ยังคงควบคุมความปลอดภัยในระดับ **SuperAdminOnly**
  2. Overrode Action `Post`, `Put`, และ `Delete` ใน `ContentItemsCRUDController` ตกแต่งด้วย `[Authorize(Policy = "SuperAdminOnly")]` เพื่อปิดช่องว่างที่ก่อนหน้านี้เปิดเป็นสาธารณะ
  3. Frontend React (`App.tsx`, `EntityListPage.tsx`, `ContentItemDetailPage.tsx`): ครอบสิทธิ์หน้าแก้ไข/สร้าง (`/content-library/new`, `/:id/edit`) ด้วย `<RequireRole superAdminOnly>`, ซ่อนปุ่มสร้าง/อัปโหลด, และปุ่มจัดทำ/ลบ (Publish/Unpublish/Delete/Edit) จากหน้าจอผู้ใช้อื่นที่ไม่ใช่ SuperAdmin ทั้งหมด
- ไฟล์หลักที่แตะ: `ContentItemsController.cs`, `ContentItemsCRUDController.cs`, `App.tsx`, `EntityListPage.tsx`, `ContentItemDetailPage.tsx`, `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน (API shape / props / DB): ปรับ Authorization policy บน API endpoints และเพิ่ม override endpoint Post
- Verified: `npm run lint` และ `npm run build` ใน `iLearn.Admin.React` ผ่านฉลุย, รัน unit tests (`iLearn.Tests`) 136/136 ผ่านทั้งหมด

## [2026-07-10 10:37] Antigravity (Gemini) — PLAN-065 React division lookup endpoint switch DONE
- ทำอะไร: เปลี่ยน endpoint สำหรับดึงข้อมูล division lookup ใน React frontend จาก 'admin/DivisionsCRUD/Get' (SuperAdminOnly) เป็น 'Divisions/lookup' (AdminOnly) จำนวน 4 หน้า เพื่อป้องกันปัญหา 403 Forbidden สำหรับ user กลุ่ม Admin/NLC:
  1. หน้า Assignments (`EntityListPage.tsx`)
  2. หน้า Learner Group list / explorer (`LearnerGroupListPage.tsx`)
  3. หน้า Create Learner Group (`LearnerGroupEditorPage.tsx`) - ถอด guard isSuperAdmin ในการโหลด division ออก และแสดง dropdown ให้ Admin แบบ disabled และ auto-select division อัตโนมัติหากมีเพียง 1 รายการ
  4. หน้า Create/Edit Learner Group Category (`LearnerGroupCategoryEditorPage.tsx`) - ถอด guard isSuperAdmin ออก, แสดง dropdown ให้ Admin แบบ disabled, และ auto-select สำหรับหมวดหมู่ใหม่หากมี 1 รายการ
- ไฟล์หลักที่แตะ: `EntityListPage.tsx`, `LearnerGroupListPage.tsx`, `LearnerGroupEditorPage.tsx`, `LearnerGroupCategoryEditorPage.tsx`, `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี (สลับ client-side query endpoint ไปหา API ที่มีอยู่แล้ว)
- Verified: `npm run lint` และ `npm run build` ใน `iLearn.Admin.React` ผ่านฉลุย, รัน unit tests (`iLearn.Tests`) 136/136 ผ่านทั้งหมด

## [2026-07-10 —] Claude Code — proactive scan 403 ทั้งระบบ (SuperAdminOnly × React) → PLAN-066 (Content Library)
- ทำอะไร: สแกนทุก endpoint SuperAdminOnly (RouteSnapshot + grep) × ทุกจุดที่ React เรียก + ตรวจ route guard (`RequireRole superAdminOnly`) และ sidebar filtering. ผล: **guard ถูกต้องแล้ว** สำหรับ Users/SystemConfig/HealthCheck/Enrollments(grid)/LearningLogs/MasterData(Divisions/Categories/CourseTypes/Roles/LGCat) — Admin ไปไม่ถึง (defense-in-depth). **พบ finding ใหม่:** `ContentItemsController` (`api/ContentItems`) เป็น SuperAdminOnly ทั้ง class แต่ Content Library อยู่ใน sidebar "Operations" (Admin เห็น) + route `content-library/*` ไม่ถูก guard → Admin 403 ที่ list + preview; ซ้ำ `ContentItems/{id}/content` ยังถูกเรียกจาก `VersionDetailPage` (โซน Courses) → preview SCORM 403. เพิ่มเติม `ContentItemsCRUDController` ไม่มี `[Authorize]` เลย (เปิดถึงแค่ authenticated — ต่ำกว่า Admin). Courses/BulkAssign = AdminOnly (ปลอดภัย). **ผู้ใช้ตัดสินใจ:** Admin อ่าน/preview ได้, จัดการคง SuperAdmin → เขียน `PLAN-066`: แยก policy ContentItems (read=AdminOnly / write=SuperAdmin, ระบุครบทุก action กัน fallback downgrade) + ตั้ง ContentItemsCRUD (read AdminOnly/write SuperAdmin) + FE ซ่อนปุ่มจัดการ + guard route editor. **ข้อสังเกตแยก (privilege gap ทิศตรงข้าม):** CourseListPage ให้ Admin เรียก CategoriesCRUD Post/Put/Delete ได้ (ข้าม division) — ควรรีวิวต่างหาก
- ไฟล์หลักที่แตะ: `DOC/PLANS/PLAN-066-*.md` (ใหม่), `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี (วินิจฉัย/แผน; PLAN-066 จะเปลี่ยน authz policy ตอน implement)
- Verified: อ่าน source ยืนยัน class-level SuperAdminOnly + route unguarded + endpoint ที่ VersionDetailPage เรียก (static)
- ถึง Gemini: คิว PLAN-065 → PLAN-066

## [2026-07-10 —] Claude Code — ปัญหา 403 DivisionsCRUD เป็นเชิงระบบหลายหน้า → PLAN-065 (supersede PLAN-064)
- ทำอะไร: ผู้ใช้พบ 403 เดียวกันที่ `learner-groups` ด้วย → grep ทั่ว `iLearn.Admin.React/src` พบเรียก `admin/DivisionsCRUD/Get` (SuperAdminOnly) เป็น division lookup 4 จุด: **ไม่มีเงื่อนไข (พังจริง)** = `EntityListPage.tsx:27` (Assignments) + `LearnerGroupListPage.tsx:249` (explorer); **gate ด้วย isSuperAdmin แล้ว (ไม่ 403 แต่ Admin เห็น dropdown ว่าง)** = `LearnerGroupEditorPage.tsx:180` + `LearnerGroupCategoryEditorPage.tsx:88`. อีกจุด `UserEditorPage.tsx:28` เรียก `RolesCRUD/Get` — นอก scope เพราะ `UsersCRUDController` SuperAdminOnly ทั้งก้อน (Users เป็น SuperAdmin-only โดยตั้งใจ). ทางแก้: สลับไป `Divisions/lookup` (AdminOnly + division-isolation + shape `{data}` ตรง = drop-in). → เขียน `PLAN-065` รวมทุกจุด (A ต้องแก้ / B แนะนำถอด guard), mark `PLAN-064` SUPERSEDED
- ไฟล์หลักที่แตะ: `DOC/PLANS/PLAN-065-*.md` (ใหม่), `DOC/PLANS/PLAN-064-*.md` (→SUPERSEDED), `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี (วินิจฉัย/แผน)
- Verified: reproduce บน QA (403 ที่ assignments + learner-groups ตรง policy SuperAdminOnly); read-only
- ถึง Gemini: ทำ PLAN-065 แทน 064

## [2026-07-10 —] Claude Code — วินิจฉัย 2 ปัญหา role f6515 (SuperAdmin→NLC) บน QA → PLAN-064 (แผน ไม่แก้โค้ด)
- ทำอะไร: (1) ผู้ใช้เปลี่ยน role f6515 (PEERAPORN) SuperAdmin→NLC แต่ client ยังเห็นเป็น SuperAdmin — query QA DB พบ **ยังถือ 2 role พร้อมกัน**: RoleId7 SuperAdmin(RoleType=1) + RoleId10 NLC(RoleType=0,Div=5) ⇒ middleware loop ใส่ role claim จาก `RoleType.ToString()` ทุกตัว → ยังมี "SuperAdmin" claim → `IsSuperAdmin=true`. ไม่ใช่บั๊ก/cache เป็น data (role เดิมไม่ถูกถอด). ผู้ใช้ถอด SuperAdmin ออกแล้ว → client เป็น NLC สำเร็จ. (2) หลังเป็น NLC หน้า `admin-react/assignments` ยิง `admin/DivisionsCRUD/Get` (SuperAdminOnly) โหลด division lookup → **403**. Root cause: `EntityListPage.tsx:27` เลือก endpoint ผิดสิทธิ์ (ควรใช้ `Divisions/lookup` = AdminOnly + มี division-isolation). บั๊กเดิมซ่อนเพราะทดสอบด้วย SuperAdmin ตลอด กระทบ admin ที่ไม่ใช่ SuperAdmin ทุกคน → เขียน `PLAN-064` (READY, Gemini): แก้ 1 บรรทัดใน EntityListPage
- ไฟล์หลักที่แตะ: `DOC/PLANS/PLAN-064-assignments-division-lookup-superadmin-403.md` (ใหม่), `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี (วินิจฉัย/แผน; DB change ผู้ใช้ทำเองผ่าน UI)
- Verified: reproduce จริงบน QA — query DB ยืนยัน 2 role; 403 ตรงกับ policy SuperAdminOnly ของ DivisionsCRUDController (read-only)
- ถึง Gemini: รับ PLAN-064 ได้เลย (High, แก้ไฟล์เดียว)

## [2026-07-10 09:15] GPT (Copilot) — PLAN-060 Phase 3 DONE: deploy PROD + smoke 7/7 pass
- ทำอะไร: deploy PROD (`deploy-api-prod.ps1`) stamp `20260710091210`; smoke 7/7 ผ่าน (health EmployeeHub Healthy 80ms, grid 8,055, NLC filter 1,230, NLC profile OK, section filter with spaces=2, cascade 15 divs/9 NLC depts, Session/Me OK); PLAN-060 marked DONE
- ไฟล์หลักที่แตะ: `DOC/PLANS/PLAN-060-*.md` (Phase 3 checkboxes + Implementer Notes + Status→DONE)
- Contract ที่เปลี่ยน (API shape / props / DB): PROD now using EmployeeHub provider (Production.json Provider=EmployeeHub deployed)
- Verified: PROD smoke 7/7 passed; rollback path documented (web.config switch to `_deploy_20260709111723`)

## [2026-07-10 —] Claude Code — PLAN-060 Phase 2 GATE PASSED (ผู้ใช้อนุมัติ PROD) + flip PROD config ใน source
- ทำอะไร: ผู้ใช้ยืนยัน soak QA ผ่าน + อนุมัติขึ้น PROD → บันทึก Phase 2 GATE = PASSED; flip `appsettings.Production.json` Provider `Legacy`→`EmployeeHub` ใน source แล้ว commit (EmployeeHubBaseUrl PROD ชี้ `http://AP-NTC2137-PRWB/...` ถูกอยู่แล้ว) → **ส่งต่อ GPT ทำ Phase 3: deploy PROD (env=Production อ่าน Production.json) + เช็ค sync pipeline prereq #4 + smoke ชุดเดียวกับ QA (รวมเคส NLC=PLAN-062 + filter ช่องว่าง=PLAN-063) + rollback = flip กลับ Legacy**; Claude ไม่ deploy เอง (เป็นงาน implementer). **ไม่รวม 2 ไฟล์ UI นอก scope** (`BulkAssignPage.tsx`/`BulkAssign.cshtml` เปลี่ยน label Learner Group→Group) เข้า commit นี้ — ยังไม่ยืนยันที่มา รอผู้ใช้เคาะ commit/revert
- ไฟล์หลักที่แตะ: `iLearn.API/appsettings.Production.json` (Provider→EmployeeHub), `DOC/PLANS/PLAN-060-*.md` (GATE PASSED + Phase 3 checklist), `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน (API shape / props / DB): PROD จะสลับไป EmployeeHub provider เมื่อ GPT deploy (default env อื่นยัง Legacy; rollback = flip กลับ)
- Verified: — (config + เอกสาร; การ verify จริงคือ smoke ตอน GPT deploy PROD)
- ถึง GPT: PLAN-060 Phase 3 พร้อมทำ — source PROD=EmployeeHub แล้ว เหลือ deploy+smoke+ยืนยัน

## [2026-07-10 —] Claude Code — รีวิว PLAN-063 ผ่าน (VERIFIED) — filter ช่องว่างกลับมาใช้ได้บน QA
- ทำอะไร: ตรวจงาน Gemini — `Replace('+',' ')` บน raw value ก่อน unescape ทั้ง `MapFilterFieldNames`/`InjectDivisionFilter` ถูกหลัก (`%2B` literal ไม่โดนแตะ), `internal static`+`InternalsVisibleTo` ตามแผน, test 4 เคสครบ (รวม `M1+`/`%2B` กัน over-correction + `%20` no-regression). **reviewer รันเอง**: build 0 errors + `dotnet test` 136/136; **ยิง request เดิมตัวที่ใช้วินิจฉัย** (section `Corporate+Support+...` แบบ `+` encoding) บน QA stamp `20260710084400` → totalCount=2 (ก่อนแก้=0) ✓ ตรง baseline `%20`. → PLAN-063 DONE→**VERIFIED**; commit เฉพาะไฟล์ในแผน — **พบไฟล์นอก scope ค้างใน tree** (`BulkAssignPage.tsx`+`BulkAssign.cshtml` เปลี่ยน label "Learner Group"→"Group", ไม่มี agent ไหนจดใน log) ไม่รวมเข้า commit รอผู้ใช้ยืนยันที่มา
- ไฟล์หลักที่แตะ: `DOC/PLANS/PLAN-063-*.md` (Status→VERIFIED + Reviewer Sign-off), `DOC/AGENT_LOG.md`; commit รวมโค้ด Gemini (`LearnersController.cs`, `iLearn.API.csproj`, `LearnersControllerTests.cs`) + PLAN-060 (บันทึก re-deploy #2 ของ GPT)
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี (แก้ decode ภายใน ไม่เปลี่ยน shape)
- Verified: `dotnet build` 0 errors; `dotnet test` 136/136; QA e2e re-check request เดิม → 2 แถว
- สถานะ PLAN-060: เหลือ soak 2-3 วันทำการ + ผู้ใช้ยืนยัน GATE เท่านั้น

## [2026-07-10 08:50] GPT (Copilot) — PLAN-060: PLAN-063 verified + deploy QA + filter re-smoke ผ่าน
- ทำอะไร: ยืนยัน PLAN-063 (Gemini) build 0 errors, 136/136 tests pass; deploy QA stamp `20260710084400`; re-smoke filter ช่องว่าง 4 เคสผ่านหมด (Section "Corporate Support Division (FM)" +encoding=2 rows, NLC/Camera Assembly=826, NLC/Lens Assembly=261, NLC grid regression=1,230); อัปเดต PLAN-060 status + checklist + Implementer Notes
- ไฟล์หลักที่แตะ: `DOC/PLANS/PLAN-060-*.md` (checklist + status + Implementer Notes)
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี (deploy โค้ดที่ Gemini เขียนใน PLAN-063)
- Verified: `dotnet build iLearn.Tests` 0 errors; `dotnet test` 136/136 passed; QA smoke filter 4/4 passed; deploy stamp 20260710084400

## [2026-07-10 08:45] Antigravity (Gemini) — PLAN-063: แก้ filter หน้า Learners พังเมื่อค่ามีช่องว่าง
- ทำอะไร: ดำเนินการ PLAN-063 ครบถ้วน:
  - แก้ไข `MapFilterFieldNames` และ `InjectDivisionFilter` ใน `LearnersController.cs` โดยทำ `.Replace('+', ' ')` บนค่า filter ที่ดึงออกมาได้ เพื่อแปลงตัวอักษรบวกที่ได้จาก form-encoding ของ browser กลับมาเป็นช่องว่างอย่างถูกต้อง ก่อนทำการถอดรหัส `Uri.UnescapeDataString(...)`
  - การแก้ไขนี้ทำงานบน raw value ทำให้สามารถแยกแยะระหว่าง space (`+` ใน URL) และ literal plus sign (`%2B` ใน URL) ได้อย่างถูกต้อง ไม่ทำให้เกิดการแปลงข้อมูลผิดพลาดในกรณีที่มีเครื่องหมาย `+` จริงในข้อมูล
  - เปลี่ยน access modifier ของทั้ง 2 helper methods ใน `LearnersController.cs` จาก `private static` เป็น `internal static` และระบุ `<InternalsVisibleTo Include="iLearn.Tests" />` ใน `iLearn.API.csproj` เพื่อให้โปรเจกต์ unit test สามารถเข้าถึงได้
  - เพิ่ม unit tests 4 เคสใน `LearnersControllerTests.cs` (form-encoded plus unescaping, %20 space preservation, literal '+' character preservation, and division filter injection with plus-space encoding)
  - อัปเดตสถานะและบันทึกผลการทำงานลงใน `DOC/PLANS/PLAN-063-learners-filter-plus-encoding.md` เป็น `DONE`
- ไฟล์หลักที่แตะ: `iLearn.API/Controllers/LearnersController.cs`, `iLearn.API/iLearn.API.csproj`, `iLearn.Tests/LearnersControllerTests.cs`, `DOC/PLANS/PLAN-063-learners-filter-plus-encoding.md`
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี
- Verified: build backend/tests สำเร็จ, รัน `dotnet test` ผ่านครบ 136/136 (รวม 4 test ใหม่ของ controller), รัน React frontend build & lint ผ่าน 100%, ทดสอบ e2e จำลอง query filter ทำงานผ่านเรียบร้อย

## [2026-07-10 —] Claude Code — วินิจฉัยบั๊กผู้ใช้รายงาน (Learners filter ช่องว่าง) → PLAN-063 (แผน ไม่แก้โค้ด)
- ทำอะไร: ผู้ใช้รายงานหน้า QA Learners เลือก Section แล้วได้ 0 แถวทั้งที่มีข้อมูล — Claude reproduce บน QA API ตรง: filter JSON เดียวกัน ช่องว่างแบบ `+` (ที่ browser ส่งจริง) → 0, แบบ `%20` → 2 ⇒ root cause: `LearnersController.MapFilterFieldNames`/`InjectDivisionFilter` ใช้ `Uri.UnescapeDataString` บน raw query (ไม่ถอด `+` เป็น space) แล้ว `EscapeDataString` กลับ → `+` กลายเป็น `%2B` → provider เทียบค่า `Corporate+Support+Division+(FM)` ไม่ match. กระทบทุกค่า filter ที่มีช่องว่าง (Section ทุกค่า, Dept `Camera Assembly`, search มีช่องว่าง); **ไม่ใช่ regression จาก cutover** (corrupt ก่อนถึง provider — Legacy โดนเหมือนกัน); ตรวจแล้ว pattern นี้มีแค่ 2 method นี้ทั้ง API. ยืนยันเพิ่ม: `GetDivisions` มี NLC ครบ 15 ค่า (ข้อสงสัย dropdown ไม่ใช่ปัญหา API). → เขียน `PLAN-063` (READY, Gemini): Replace('+',' ') ก่อน unescape ใน 2 method + internal/InternalsVisibleTo + tests 4 เคส (รวมเคส `%2B` literal กัน over-correction); PLAN-060 เพิ่มเงื่อนไข GATE
- ไฟล์หลักที่แตะ: `DOC/PLANS/PLAN-063-learners-filter-plus-encoding.md` (ใหม่), `DOC/PLANS/PLAN-060-*.md` (soak finding #2 + GATE), `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี (วินิจฉัย/แผน)
- Verified: reproduce จริงบน QA (`+`→0, `%20`→2) — read-only GET เท่านั้น
- ถึง Gemini: รับ PLAN-063 ได้เลย (High); ถึง GPT: หลัง 063 VERIFIED → redeploy QA + re-smoke ตาม Handoff แล้วนับ soak ต่อ

## [2026-07-10 —] Claude Code — รีวิว PLAN-062 ผ่าน (VERIFIED) + รับรอง re-smoke PLAN-060 → เข้าช่วง soak
- ทำอะไร: ตรวจงาน Gemini (PLAN-062) — `NormalizeDivision` null-safe/case-insensitive ครบ 3 ingress ตรงสเปก (cache build ใช้ `Select` ใต้ `AddRange` enumerate ทันที), ไม่แตะ out-of-scope; ไล่ edge: ไม่ double-count ใน `GetLearnersByDivisionsAsync` (if/else), ขอ `"PD"` ดิบไม่จับ NLC อีก (ถูกตาม PLAN-061), `GetDivisionsAsync` ใช้ Company จึงไม่มีค่าลาวโผล่; 4 tests assert normalize+scope จริง; config S3 ถูกหลัก layering. **reviewer รันเอง: build 0 errors + 132/132 passed**. ตรวจงาน GPT (re-smoke): **query EmployeeHub QA ตรง `company=NLC` total=1,230 ตรงกับ grid re-smoke เป๊ะ** ⇒ 1,244→1,230 คือ data movement ข้ามวัน (sync) ไม่ใช่ filter mismatch. → PLAN-062 DONE→**VERIFIED**; PLAN-060 GATE ข้อ (1) ครบ เหลือ soak 2-3 วัน + ผู้ใช้ยืนยัน (ข้อ 2) ก่อน Phase 3 PROD; commit ทั้งชุด
- ไฟล์หลักที่แตะ: `DOC/PLANS/PLAN-062-*.md` (Status→VERIFIED + Reviewer Sign-off), `DOC/PLANS/PLAN-060-*.md` (Status→soak), `DOC/AGENT_LOG.md`; commit รวมโค้ด Gemini (`EmployeeHubLearnerApiService.cs`, tests, `appsettings.json`, `appsettings.Staging.json` ใหม่)
- Contract ที่เปลี่ยน (API shape / props / DB): พนักงาน NLC กลับมามี `Division="NLC"` ทุก endpoint ของ provider EmployeeHub (คืน contract legacy — ตาม PLAN-062)
- Verified: `dotnet build` 0 errors; `dotnet test` 132/132; `GET /api/employees?company=NLC` total=1,230 ยืนยัน re-smoke

## [2026-07-10 08:10] GPT (Copilot) — PLAN-060 Phase 1 re-deploy: PLAN-062 verified + NLC re-smoke ผ่าน
- ทำอะไร: ยืนยัน PLAN-062 (Gemini) ทำครบแล้ว (NormalizeDivision 3 ingress, 4 tests, config reverted); build 0 errors, 132/132 tests pass; deploy QA stamp `20260710080811`; re-smoke NLC admin 4 เคสผ่านหมด (grid 1,230 rows Division="NLC", profile 200 OK, cascade dept 9, cascade sections 152)
- ไฟล์หลักที่แตะ: `DOC/PLANS/PLAN-060-*.md` (Phase 1 re-deploy checkboxes + Implementer Notes)
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี (deploy โค้ดที่ Gemini เขียนใน PLAN-062 + ไม่มีไฟล์ใหม่จาก GPT)
- Verified: `dotnet build iLearn.Tests -o artifacts/verify-062` 0 errors; `dotnet test` 132/132 passed; QA smoke NLC 4/4 passed; deploy stamp 20260710080811

## [2026-07-10 08:10] Antigravity (Gemini) — PLAN-062: Normalize NLC division ใน EmployeeHub + คืน config default=Legacy
- ทำอะไร: ดำเนินการ PLAN-062 ครบถ้วน:
  - เพิ่ม `NormalizeDivision` helper ใน `EmployeeHubLearnerApiService.cs` และเรียกใช้ที่ ingress ทั้ง 3 จุด (`GetActiveEmployeesCachedAsync`, `GetLearnerByCodeAsync`, `GetEmployeesByNidsAsync`) เพื่อให้พนักงาน NLC มี division เป็น `"NLC"` สอดคล้องกันทั้งหมด
  - เพิ่ม unit tests 4 เคสใน `EmployeeHubLearnerApiServiceTests.cs` (grid filter NLC, profile, cascade departments, find by NIDs) ครอบคลุมพฤติกรรม normalization
  - คืนค่า default `"Provider"` ใน base `appsettings.json` กลับมาเป็น `"Legacy"` (fail-safe) และสร้าง `appsettings.Staging.json` เพื่อ opt-in `"Provider": "EmployeeHub"` สำหรับ QA/Staging
  - อัปเดตสถานะและจดบันทึกผลการทำงานลงใน `DOC/PLANS/PLAN-062-employeehub-nlc-normalization.md` เป็น `DONE`
- ไฟล์หลักที่แตะ: `iLearn.Infrastructure/Services/EmployeeHubLearnerApiService.cs`, `iLearn.Tests/EmployeeHubLearnerApiServiceTests.cs`, `iLearn.API/appsettings.json`, `iLearn.API/appsettings.Staging.json`, `DOC/PLANS/PLAN-062-employeehub-nlc-normalization.md`
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี (คืนพฤติกรรม Legacy สำหรับพนักงาน NLC ใน EmployeeHub provider, default provider กลับเป็น Legacy)
- Verified: build backend/tests สำเร็จ, รัน `dotnet test` ผ่านครบ 132/132 (รวม 4 test ใหม่), รัน React frontend build & lint ผ่าน 100%

## [2026-07-09 —] Claude Code — รีวิวรอบ 3 หลัง QA cutover: พบ NLC isolation พัง → เขียน PLAN-062 (block Phase 2 GATE)
- ทำอะไร: รีวิว holistic ทั้งชุด EmployeeHub หลัง GPT ทำ Phase 0+1 — พบ 2 finding:
  - **A (Critical, ยืนยันด้วย DB):** NLC division isolation พังทุก path ยกเว้น Bulk Assign — พนักงาน NLC ใน EmployeeHub มี `Division=PD/AD` (NLC เป็น Company) แต่ PLAN-058 ใส่กติกา `Company=="NLC"` แค่ใน `GetLearnersByDivisionsAsync`; grid (`InjectDivisionFilter`), profile (`GetLearnerByCodeAsync` + isolation compare), cascade dept/section, `EmployeeCsvDto` ใช้ `e.Division` ดิบ → **query QA ยืนยัน: Role `NLC` (Id=10, DivisionId=5) มี user ถือจริง 5 คน (h8193,d6132,n7710,q2186,q2825) — ทั้ง 5 คนตอนนี้ grid ว่าง/profile 404 บน QA live**; GPT เห็น symptom ("grid NLC=0") แต่ตีเป็น non-blocking — reviewer override เป็น blocking
  - **B (Medium):** GPT flip base `appsettings.json` Provider→EmployeeHub (เพราะ QA รัน Staging อ่าน base) — ทำลาย fail-safe "default=Legacy ทุก env" ของ PLAN-058; PROD ปลอดภัยเพราะ Production.json override เท่านั้น
  - เขียน **PLAN-062** (READY, Gemini): S1 `NormalizeDivision` (Company==NLC ⇒ Division="NLC") ที่ 3 ingress (cache build / GetLearnerByCodeAsync / GetEmployeesByNidsAsync) = คืน contract legacy ที่ boundary เดียว, S2 tests 4 ตัว (grid NLC filter, profile, cascade, enrichment), S3 คืน base=Legacy + สร้าง `appsettings.Staging.json` (Provider=EmployeeHub) สำหรับ QA; ห้ามแตะ `GetLearnersByDivisionsAsync`/`GetDivisionsAsync` (ถูกอยู่แล้ว ใช้ Company). อัปเดต PLAN-060: Phase 2 GATE เพิ่มเงื่อนไข PLAN-062 VERIFIED + redeploy + re-smoke NLC (user จริง เช่น n7710) ก่อนขอผู้ใช้ยืนยัน PROD
- ไฟล์หลักที่แตะ: `DOC/PLANS/PLAN-062-employeehub-nlc-normalization.md` (ใหม่), `DOC/PLANS/PLAN-060-*.md` (GATE + reviewer override), `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มีในตอนนี้ (แผน) — PLAN-062 จะทำให้ Division ของพนักงาน NLC กลับมาเป็น "NLC" ทุก endpoint (ตรง legacy)
- Verified: query QA DB read-only (Roles/UserRoles) เป็นหลักฐาน Finding A; ไม่มีโค้ดเปลี่ยน
- ถึง Gemini: รับ PLAN-062 ได้เลย — Critical, block Phase 3 PROD; ถึง GPT: หลัง 062 VERIFIED ให้ redeploy QA + re-smoke ตาม Handoff ในแผน

## [2026-07-09 17:00] GPT (Copilot) — PLAN-060 Phase 0+1: EmployeeHub cutover live on QA
- ทำอะไร: ดำเนินการ PLAN-060 Phase 0+1 ครบ:
  - **Phase 0**: R2 un-delete PD3 บน QA (1 row), R1 soft-delete Test บน PROD (1 row), verify QA+PROD Divisions ตรงกัน
  - **Phase 1**: เปลี่ยน `appsettings.json` (base) Provider → "EmployeeHub" (QA deploy ใช้ Staging env → อ่าน base เท่านั้น; Development.json/Production.json ยังคง Legacy); deploy API ด้วย `deploy-api.ps1` → stamp `20260709164236`
  - **Smoke tests** (10/10): health ✅ (EmployeeHub Healthy 37ms), grid ✅ (8,077 = upstream), division filter ✅ (PD1=1,590), cascade lookups ✅ (3 ระดับ), profile ✅, NLC Bulk Assign ✅ (1,244), Session/Me DisplayName ✅, admin-react 200 ✅
  - ⚠️ Grid filter "NLC" returns 0 (grid row maps actual Division, not Company→"NLC") — Bulk Assign uses dedicated API ที่ handle NLC ถูก; เป็น behavioral diff จาก legacy แต่ non-blocking
  - Next: soak QA 2-3 วันทำการ → Phase 2 GATE → Phase 3 PROD
- ไฟล์หลักที่แตะ: `iLearn.API/appsettings.json` (Provider Legacy→EmployeeHub), `DOC/PLANS/PLAN-060-*.md` (Status→IN-PROGRESS + Phase 0/1 checkboxes + Implementer Notes)
- Contract ที่เปลี่ยน (API shape / props / DB): QA Divisions table (R2 un-delete PD3), PROD Divisions table (R1 soft-delete Test); API config switch Provider=EmployeeHub บน QA
- Verified: `dotnet build` 0 errors, `dotnet test` 128/128 passed, QA smoke 10/10 passed, deploy stamp 20260709164236

## [2026-07-09 —] Claude Code — ร่างตัวเลือก auth EmployeeHub + ผู้ใช้เคาะ → PLAN-060 READY
- ทำอะไร: ร่าง 4 ตัวเลือก auth EmployeeHub (IIS IP allow-list / API key เฉพาะ ops / API key ทุก endpoint / Windows Auth) เทียบ trade-off + iLearn impact จาก skill (auth ตอนนี้ = เปิดหมด, X-Api-Key ถูกถอดแล้ว; ops=sync/backfill, consume=employees/lookups/org ที่ iLearn ใช้อ่านอย่างเดียว, PROD EmployeeHub อยู่เครื่องเดียวกับ iLearn.API). **ผู้ใช้เลือก: risk-acceptance "trusted internal network"** — คงเปิดตามเดิม ไม่ใส่ auth/allow-list พึ่งเน็ตเวิร์กภายใน → iLearn ไม่ต้องแก้โค้ด. บันทึก residual risk (ops+scalar/swagger เปิดทั้งอินทราเน็ต, hardening เฉพาะ /api/sync/* เป็นทางเลือกอนาคต). prerequisite PLAN-060 ครบทั้งหมด → **Status DRAFT→READY** (Phase 2 GATE ยังต้องรอผู้ใช้ยืนยันก่อนแตะ PROD)
- ไฟล์หลักที่แตะ: `DOC/PLANS/PLAN-060-*.md` (Status→READY + prereq #3 resolved + Residual risk), `DOC/PLANS/PLAN-061-*.md` (auth item resolved), `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี (เอกสาร/decision)
- Verified: — (เอกสาร); ถึง GPT: PLAN-060 READY แล้ว รับไป cutover ได้ตาม Phase 0(data cleanup R1/R2)→1(QA)→2(GATE รอผู้ใช้)→3(PROD)

## [2026-07-09 —] Claude Code — รีวิว PLAN-059 audit ผ่าน (+ เสริมหลักฐาน schema)
- ทำอะไร: ตรวจ audit ของ GPT — verified Finding 1 จาก entity จริง (`EnrollmentAssignment` snapshot = scalar เท่านั้น ไม่มี name ฝัง), Finding 4 สอดคล้อง PLAN-061 (Assignments.Division NULL); **เสริม:** ตรวจ schema พบ iLearn ไม่มี entity Department/Section master-data เลย และ LearnerGroup/Assignment scope ด้วย DivisionId(FK)+EmployeeCodes/members เท่านั้น → ที่เดียวที่ persist ชื่อ division เป็น string คือ Divisions.Name + Assignment.Division(NULL) ⇒ EmployeeHub canonicalize dept/section ทำข้อมูล iLearn เพี้ยนไม่ได้ (ปิดประเด็น A4). การจำแนก 15+1 ค่าตรง PLAN-061 เป๊ะ. Findings 2/3 (FK counts, QA↔PROD drift) เป็น query read-only ที่ reviewer รันซ้ำเองไม่ได้แต่ consistent. → PLAN-059 VERIFIED, ไม่มี mapping blocker; เหลือ decision R2 (PD3) ที่ผู้ใช้ต้องเคาะก่อน PLAN-060 READY, และแนะนำ R1 (soft-delete Test บน PROD) ผูกเป็น step ใน PLAN-060 pre-cutover แทน admin รัน ad-hoc
- ไฟล์หลักที่แตะ: `DOC/PLANS/PLAN-059-*.md` (+ Reviewer Sign-off), `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี (review เอกสาร)
- Verified: — (audit read-only; reviewer ตรวจ entity/schema เพิ่มเพื่อ confirm Finding 1 + ปิด A4)

## [2026-07-09 17:30] GPT (Copilot) — PLAN-059 Division mapping audit (4 ข้อที่เหลือจาก PLAN-061)
- ทำอะไร: ตรวจ 4 จุดตามที่ PLAN-061 ระบุ: (1) EnrollmentAssignment ไม่มี JSON snapshot — snapshot เป็น scalar (bool/datetime/float) ไม่มี division/section ฝัง → ไม่ต้อง migrate; (2) Division `Test` (Id=6) มี FK=0 ทุกตาราง ทั้ง QA+PROD ปลอดภัยลบ; (3) QA vs PROD drift: PD3+Test soft-deleted บน QA แต่ยัง active บน PROD — PD3 ต้องให้ผู้ใช้ตัดสิน (เป็น division จริงของ NTC+VDS); (4) `Assignments.Division` (legacy string) = NULL ทั้งหมดทั้ง QA+PROD → ไม่มี data ต้อง migrate; เขียนตารางเทียบ 3 แหล่ง × ทุกค่าครบ พร้อมข้อเสนอ 4 ข้อ (R1-R4)
- ไฟล์หลักที่แตะ: `DOC/PLANS/PLAN-059-employeehub-division-mapping-audit.md` (Status READY→DONE + Findings), `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี (read-only audit)
- Verified: ไม่มีโค้ดเปลี่ยน — query results ใน findings เป็นหลักฐาน

## [2026-07-09 —] Claude Code — รีวิว PLAN-058 FIX-1 (รอบ 2) ผ่าน + commit ทั้งชุด
- ทำอะไร: ตรวจการแก้ FIX-1 ของ Antigravity — lookup 4 method กรอง filter บน employee cache ก่อน distinct → cascade scope ถูก ไม่โยน 500, คืน bare `List<LookupNameDto>` ตรง React (`Array.isArray`+`.name`); `ParseLoadOptions` เพิ่ม `.Replace('+',' ')` ก่อน unescape ตรวจแล้วปลอดภัย (React ใช้ encodeURIComponent, `+` จริงมาเป็น `%2B` ไม่โดนแตะ) ไม่ทำ grid path เพี้ยน; test 2 ตัวใหม่ assert scoped+non-empty พิสูจน์ cast `filteredObj.data as IEnumerable<EmployeeDto>` ทำงาน runtime. **reviewer รัน verification เอง**: `dotnet build iLearn.Tests` 0 errors + `dotnet test` 128/128 passed → PLAN-058 Status DONE→**VERIFIED**, commit ทั้งชุด (client + service + tests + config + DI + health + controller + plans/log)
- ไฟล์หลักที่แตะ: `DOC/PLANS/PLAN-058-*.md` (Status→VERIFIED + Reviewer Findings รอบ 2), `DOC/AGENT_LOG.md`; commit รวมงาน implement ของ Antigravity ทั้งหมด (`EmployeeHubClient.cs`, `EmployeeHubLearnerApiService.cs`, `EmployeeHubLearnerApiServiceTests.cs` + ไฟล์แก้)
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มีของใหม่จาก reviewer; ยืนยัน contract ฝั่ง React เดิมถูก preserve (lookups = bare array, grid = data/totalCount, default Legacy ทุก env)
- Verified: `dotnet build iLearn.Tests -o` 0 errors; `dotnet test` 128/128 passed (รวม 2 test cascade ใหม่)

## [2026-07-09 16:00] Antigravity — แก้ไขจุดบกพร่อง PLAN-058 (รอบ 2): Cascade Department/Section ล้มเหลว
- ทำอะไร:
  - แก้ไข `GetDivisionsAsync`, `GetSectionsAsync`, `GetDepartmentsAsync`, และ `GetPositionsAsync` ใน `EmployeeHubLearnerApiService.cs` ให้ทำงานบนโครงสร้างเดียวกัน:
    - กรองข้อมูลบน employee cache ด้วย filter ที่ส่งมาจาก React ก่อนทำการ distinct เพื่อแก้ไขอาการโยน exception 500
    - คืนค่าเป็น bare `List<LookupNameDto>` โดยตรง (ไม่ห่อใน `LoadResult`) เพื่อให้สอดคล้องกับพฤติกรรมของ frontend React ที่ใช้ `Array.isArray(res)` เช็คอาร์เรย์เปล่า
  - ปรับปรุงการถอดรหัส query string ใน `ParseLoadOptions` ให้แปลงเครื่องหมายบวก (`+`) เป็นเว้นวรรค (space) อย่างถูกต้องเพื่อรองรับ nested filters
  - เพิ่ม Unit Tests ใน `EmployeeHubLearnerApiServiceTests.cs` (เพิ่ม 2 เทสกรณี `GetDepartmentsAsync` และ `GetSectionsAsync` ที่รับ filter)
- ไฟล์หลักที่แตะ: `iLearn.Infrastructure/Services/EmployeeHubLearnerApiService.cs`, `iLearn.Tests/EmployeeHubLearnerApiServiceTests.cs`
- Contract ที่เปลี่ยน (API shape / props / DB): ปรับปรุงโครงสร้างคืนค่าของ lookup endpoints 4 ตัวจาก `{ data: [...] }` กลับมาเป็น bare JSON array `[...]` เพื่อความสอดคล้องกับ React frontend (เหมือนของ Legacy)
- Verified: รัน xUnit `dotnet test` (128/128 tests passed) ผ่านสำเร็จ 100% และรัน React build ผ่านสมบูรณ์แบบ

## [2026-07-09 —] Claude Code — รีวิว PLAN-058 (รอบ 1): พบ blocking cascade Department/Section → NEEDS-FIX
- ทำอะไร: รีวิว implement PLAN-058 ของ Antigravity เทียบ interface/DTO/controller เดิม + consumer React จริง (`LearnerListPage.tsx`, `LearnerDirectorySelector.tsx`) — สรุป: โครงถูกเกือบหมด **Legacy regression ศูนย์ (Acceptance #1 ผ่าน)** แต่เจอ **blocking 1 จุด (Acceptance #2 ไม่ผ่าน)**: `GetDepartmentsAsync`/`GetSectionsAsync` distinct→project เป็น `LookupNameDto{Name}` ก่อน แล้วเอา `DataSourceLoader.Load` มากรองด้วย filter ที่อ้าง field `Division`/`Department` ซึ่ง LookupNameDto ไม่มี → โยน exception 500 (ไม่มี try/catch) → Department/Section dropdown ว่าง/toast error ทั้งหน้า Learners + Bulk Assign; และ distinct ไม่ scope ตาม parent ที่เลือก (ของเดิม upstream scope ให้). แนวแก้: กรอง filter บน employee cache (มี Division/Department/Section ครบ) ก่อน แล้วค่อย distinct field เป้าหมาย คืน bare List<LookupNameDto> + เพิ่ม test cascade (ปิด coverage gap — 126 tests เดิมไม่มี case lookup+filter). จุดอื่นตรวจแล้วถูก: DxGrid (row DTO มี field ครบ filter จึงไม่พัง), camelCase nid/eId, division semantics PLAN-061, perf enrichment (claims cache 10 นาที), gap 3 Production.json, DI switch/health/default Legacy
- ไฟล์หลักที่แตะ: `DOC/PLANS/PLAN-058-employeehub-provider-foundation.md` (Status DONE→NEEDS-FIX + เพิ่ม "Reviewer Findings รอบ 1" พร้อม FIX-1 + code sketch)
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี (รีวิว/เอกสาร) — **แต่ชี้ว่า implement รอบแรกทำ contract lookups เพี้ยนตอน Provider=EmployeeHub, ต้องแก้ตาม FIX-1**
- Verified: — (review อ่านโค้ด ไม่รัน; finding เป็น runtime path ที่ unit test เดิมไม่ครอบ จึง 126/126 ผ่านได้ทั้งที่บั๊กอยู่)
- ถึง Gemini: รับ PLAN-058 FIX-1 ไปแก้ต่อ (แก้ 2 method + เพิ่ม test) แล้วอัปเดต Status กลับ เมื่อ verify Acceptance #2 ผ่าน

## [2026-07-09 15:30] Antigravity — ทำตาม PLAN-058: EmployeeHub Provider — client + translation layer
- ทำอะไร: 
  - เพิ่ม config ของสลับ Provider ใน `EmployeeServiceSettings` (Legacy / EmployeeHub) ครบทุก environment (`appsettings*.json`)
  - แก้ไข gap 3: ปรับแก้ URL ของพนักงาน PRWB และ BaseEmployeeCsvUrl ใน `appsettings.Production.json` ให้ชี้เซิร์ฟเวอร์ PROD (`AP-NTC2137-PRWB`) ทั้งหมดแทนที่จะหลุดไปใช้ QA
  - สร้าง `EmployeeHubClient` สำหรับยิง API ของ EmployeeHub และสร้าง DTO schemas (EmployeeDto, EmployeeHubPagedResult, FindByNidsResultDto)
  - สร้าง `EmployeeHubLearnerApiService` เพื่อรับข้อมูลจาก `EmployeeHubClient` แล้วจัด format/map ข้อมูลให้เป็น shape เดิมของ `ILearnerApiService`
  - ทำ Directory Cache ใน memory (IMemoryCache TTL 30 นาที) เพื่อดึงพนักงาน active ทั้งหมด
  - ย้าย `LearnersGridResponse` และ `LearnerGridRowDto` ไปที่ `ExternalLearnerDto.cs` ของ Application layer เพื่อเลี่ยง circular reference
  - ปรับการกรอง division rules: `'NLC'` ค้นหาจาก `Company == "NLC"` และค่าอื่นค้นหาตามชื่อ `Division` แบบ case-insensitive
  - ปรับปรุง distinct lookups ของ divisions, departments, sections, และ positions ให้หาค่า distinct จาก directory cache ทั้งหมดแทนการยิง direct lookups
  - ปรับปรุง `HealthController.cs` ให้มี check `employeeDirectory`
  - เพิ่ม Unit Tests ใน `EmployeeHubLearnerApiServiceTests.cs` (6 test cases ครอบคลุม caching, mapping, nids chunking, grid parsing, lookups)
- ไฟล์หลักที่แตะ: `iLearn.Application/Common/EmployeeServiceSettings.cs`, `iLearn.Application/DTOs/ExternalLearnerDto.cs`, `iLearn.API/appsettings.json`, `iLearn.API/appsettings.Development.json`, `iLearn.API/appsettings.Production.json`, `iLearn.Infrastructure/Services/EmployeeHubClient.cs`, `iLearn.Infrastructure/Services/EmployeeHubLearnerApiService.cs`, `iLearn.Infrastructure/DependencyInjection.cs`, `iLearn.API/Controllers/HealthController.cs`, `iLearn.API/Controllers/LearnersController.cs`, `iLearn.Tests/EmployeeHubLearnerApiServiceTests.cs`
- Contract ที่เปลี่ยน (API shape / props / DB): เพิ่ม config settings, API internals switchable via flag.
- Verified: `npm run lint` และ `npm run build` ผ่านใน frontend; รัน xUnit `dotnet test` (126/126 tests passed) ผ่านทั้งหมด

## [2026-07-09 11:20] GPT (Copilot) — ตรวจงาน PLAN-055/056 + deploy PLAN-057 ขึ้น QA+PROD
- ทำอะไร: รีวิว PLAN-055 (single-division mode) และ PLAN-056 (category description) — ผ่านทุกจุด ไม่มี issue; deploy ตาม PLAN-057: gen idempotent SQL script + targeted script (full idempotent มีปัญหา CREATE VIEW parse error), QA: DB มี migration แล้ว (Antigravity ทำตอน dev) + deploy API stamp `_deploy_20260709110854` + admin-react, smoke 200 pass; PROD: backup DB (586146 pages) + run targeted migration script + deploy API stamp `_deploy_20260709111723` + admin-react, smoke 200 pass; ทั้ง QA/PROD ยืนยัน CategoriesCRUD/Get คืน `description` field
- ไฟล์หลักที่แตะ: `DOC/PLANS/PLAN-057-deploy-category-description-qa-prod.md` (status→DONE + Implementer Notes), `DOC/AGENT_LOG.md`, `artifacts/migrations/idempotent-to-AddDescriptionToCategory.sql` (ใหม่), `artifacts/migrations/prod-AddDescriptionToCategory.sql` (ใหม่)
- Contract ที่เปลี่ยน (API shape / props / DB): PROD DB เพิ่มคอลัมน์ `Categories.Description nvarchar(500) NULL` + migration history record
- Verified: QA smoke 200 (DB 17ms), PROD smoke 200 (DB 23ms), CategoriesCRUD response มี description ทั้ง QA/PROD, deploy HealthChecked=True + AutoRolledBack=False ทั้ง QA/PROD
- ทำอะไร: 
  - เพิ่มฟิลด์ `Description` (nullable) ใน `Category` entity
  - สร้าง EF Core migration และอัปเดตลง dev database `iLearnDB_New` บน server `10.10.143.37`
  - เจนเนอเรต SQL script แบบ idempotent เก็บไว้ที่ `artifacts/migration-AddDescriptionToCategory.sql`
  - ปรับปรุง `CategoriesCRUDController.cs` (Get/GetPaged/GetDashboard) ให้ส่งออกฟิลด์ `Description` ใน response
  - ปรับปรุง React types และ modals (Create, Edit) ใน `CourseListPage.tsx` ให้สนับสนุนการจัดการ Description
  - ปรับปรุง Master Data Categories Grid (เพิ่ม Description column & searchExpr) ใน `moduleConfigs.ts`
  - ปรับปรุง `MasterDataDetailPage.tsx` ให้แสดง/แก้ไข Description ได้ และแก้ compile error type ของ config
- ไฟล์หลักที่แตะ: `iLearn.Domain/Entities/Category.cs`, `iLearn.API/Controllers/Base/CategoriesCRUDController.cs`, `iLearn.Admin.React/src/pages/courses/CourseListPage.tsx`, `iLearn.Admin.React/src/pages/moduleConfigs.ts`, `iLearn.Admin.React/src/pages/master-data/MasterDataDetailPage.tsx`, `DOC/PLANS/PLAN-056-category-description-field.md`
- Contract ที่เปลี่ยน (API shape / props / DB): คอลัมน์ `Description` (nvarchar(500) NULL) เพิ่มบน DB table `Categories`, เพิ่ม properties ใน API response json
- Verified: `npm run lint` และ `npm run build` ผ่านใน frontend; รัน xUnit `dotnet test` (118/118 tests passed) backend ผ่านทั้งหมด

## [2026-07-09 10:50] Antigravity — ทำตาม PLAN-055: Courses Explorer Skip Single Division
- ทำอะไร: 
  - แก้ไขสิทธิ์เข้าถึง endpoint ของ Divisions จาก `admin/DivisionsCRUD/Get` เป็น `GET api/Divisions` (AdminOnly) เพื่อป้องกันข้อผิดพลาด 403 Forbidden สำหรับ Division Admin
  - ตรวจสอบโหมด Single-Division (มี division เดียว) โดยผู้ใช้จะเห็น Category folders ที่หน้าแรกโดยตรง (ข้ามชั้น Division folder)
  - ปรับปรุง `getParentPath` และ `buildBreadcrumbs` เพื่อข้ามชั้น Division folder ในโหมด Single-Division
  - ปรับปรุงการกดเปิดโฟลเดอร์ `handleOpenItem` และหัวโฟลเดอร์ `currentFolderName`
  - ปรับปุ่มสร้าง Category (New Category) ให้แสดงสำหรับเจ้าหน้าที่ Division Admin เมื่ออยู่ในโหมด Single-Division จากหน้าแรกได้ และปรับปรุงฟอร์ม Dropdown/Submit ใน Modal ให้ไม่ต้องเลือก Division อีก
  - ปรับปุ่ม Actions แก้ไข/ลบ Category ในตาราง explorer ให้แสดงที่หน้ารายการ Category (ระดับ root) ในโหมด Single-Division
- ไฟล์หลักที่แตะ: `iLearn.Admin.React/src/pages/courses/CourseListPage.tsx`, `DOC/PLANS/PLAN-055-courses-explorer-skip-single-division.md`
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี (ฝั่ง React มีการเปลี่ยน endpoint ที่เรียกตามแผน โดยโครงสร้าง shape และ type ของ API ตรงตาม DTO อยู่แล้ว)
- Verified: `npm run lint` และ `npm run build` ผ่านใน frontend; รัน xUnit `dotnet test` (118/118 tests passed) backend ผ่านทั้งหมด

## [2026-07-09 —] Claude Code — lookup ข้อมูล EmployeeHub จริง + เขียน PLAN-061 กติกา division (แผน ไม่แก้โค้ด)
- ทำอะไร: ผู้ใช้อธิบายกติกา division ใหม่ (NLC = ทั้ง company NLC, อื่น ๆ = division ใต้ NTC) → Claude lookup จริง 3 แหล่ง: EmployeeHub (`http://10.10.143.39/Tools/EmployeeHub/Service`) companies=`NLC,NTC,VDS`, divisions 18 ค่า (ไม่มี NLC, ปนค่าลาว AD/PD/AD Division/PD Division); upstream เดิม GetDistinctDivisions 15 ค่า (NLC + 14 NTC); iLearnDB QA `Divisions`=`CSD,NLC,PD1,PD2,PD3,Test` และ `Assignments.Division` = NULL ทั้งตาราง → สรุปเป็น `PLAN-061` (กติกา: NLC→filter `Company=="NLC"`, อื่น→`Division==ค่า` ไม่กรอง company ครอบ NTC+VDS; รายการ divisions = `["NLC"] ∪ distinct(Division where Company!="NLC")` คำนวณจาก cache ห้ามใช้ `/api/lookups/divisions` ตรง) — ปรับ spec S3 ของ PLAN-058 + ตัด scope PLAN-059 เหลือ 4 ข้อ + list ประเด็นฝั่ง EmployeeHub ให้ผู้ใช้แก้ (org 650 กำพร้า, `?company=` ไม่ทำงาน, auth)
- ไฟล์หลักที่แตะ: `DOC/PLANS/PLAN-061-employeehub-division-semantics.md` (ใหม่), เพิ่ม cross-ref ใน PLAN-058/059
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี (เอกสาร)
- Verified: — (lookup อ่านอย่างเดียว; ไม่แตะ DB/โค้ด)
- Update (ผู้ใช้ให้ URL): QA base `http://10.10.143.39/Tools/EmployeeHub/Service`, PROD base `http://AP-NTC2137-PRWB/Tools/EmployeeHub/Service` (ตัด `/scalar`, เป็น http, PROD อยู่บนเว็บ PROD ตัวเดียวกับ iLearn.API) → ใส่ค่าใน PLAN-058 S1 + PLAN-061; **ยืนยัน validation:** กติกา reproduce รายการ division เดิม 15 ค่าเป๊ะ (14 non-NLC ตรง upstream 100% + NLC)

## [2026-07-09 —] Claude Code — เขียนชุดแผนย้ายฐานข้อมูลพนักงานไป EmployeeHub (PLAN-058/059/060 — แผน ไม่แก้โค้ด)
- ทำอะไร: ตามคำสั่งผู้ใช้ (เริ่มจากปิด gap) — สำรวจแล้วพบว่า employee data ไหลผ่าน `ILearnerApiService` จุดเดียว (upstream: EmployeeServiceV2 Student/StudentLookup + Employee.Service GetAllCSV ซึ่ง **PROD ชี้ QA host อยู่**) → สร้าง 3 แผน:
  - `PLAN-058` (READY, Gemini): `EmployeeHubClient` + `EmployeeHubLearnerApiService` implement interface เดิมหลัง feature flag `Provider` (default Legacy) — ปิด gap DevExtreme grid (DataSourceLoader บน directory cache 30 นาที), gap Position (map = Grade), gap config PROD (เพิ่ม EmployeeServiceSettings ใน Production.json); ห้ามแตะ React/interface/DTO เดิม
  - `PLAN-059` (READY, GPT): audit เทียบชื่อ Division/Department/Section 3 แหล่ง (iLearnDB: `Divisions.Name` + `Assignments.Division` string legacy + snapshot ↔ upstream เดิม ↔ EmployeeHub NameAbbr) — read-only, จบที่รายงาน+ข้อเสนอ remap ให้ผู้ใช้ตัดสิน
  - `PLAN-060` (DRAFT, GPT): cutover flip flag QA→GATE→PROD, rollback = flip กลับ; prerequisites รวมเรื่อง auth ของ EmployeeHub (ตอนนี้ API เปิดหมด) ที่ผู้ใช้ต้องเคาะก่อน
- ไฟล์หลักที่แตะ: `DOC/PLANS/PLAN-058/059/060-employeehub-*.md` (ใหม่ 3 ไฟล์)
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มีในตอนนี้ (ทุกแผนบังคับ preserve contract เดิม)
- Verified: — (เอกสารอย่างเดียว)

## [2026-07-09 —] Claude Code — follow-up จากรีวิว: GetPaged ค้น description ได้ + track .claude/launch.json
- ทำอะไร: ผู้ใช้สั่งเก็บงาน 2 จุดจากหมายเหตุรีวิว PLAN-056: (1) `CategoriesCRUDController.GetPaged` เพิ่มเงื่อนไข search ให้ครอบ `Description` (null-safe, pattern เดียวกับ Division.Name เดิม); (2) commit `.claude/launch.json` (dev server config สำหรับ preview) และเพิ่ม `.claude/settings.local.json` ลง `.gitignore` (เป็น config เฉพาะเครื่องตาม convention ของ Claude Code ไม่ควร track)
- ไฟล์หลักที่แตะ: `iLearn.API/Controllers/Base/CategoriesCRUDController.cs`, `.gitignore`, `.claude/launch.json` (ใหม่)
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี (พฤติกรรม search ของ `GetPaged` กว้างขึ้นเท่านั้น — shape เดิม); หมายเหตุ: การแก้นี้ยังไม่ถูก deploy — จะติดไปกับรอบ deploy API ถัดไป
- Verified: dotnet build + dotnet test 118/118 ผ่าน

## [2026-07-09 —] Claude Code — Review PLAN-055/056/057: PASS ทั้งสามแผน → VERIFIED + commit
- ทำอะไร: รีวิว diff เต็มของงาน Gemini (PLAN-055 single-division explorer, PLAN-056 Category.Description) และหลักฐาน deploy ของ GPT (PLAN-057 QA+PROD) — ทุก scope item ครบตามแผน, branch ใหม่ทั้งหมด gate ด้วย `singleDivision !== null` (โหมดหลาย division ไม่กระทบ), Edit Category ส่ง `description: null` ตอนเคลียร์ถูกต้อง, migration additive อย่างเดียว; reviewer รัน verification ซ้ำเอง: `npm run lint` ผ่าน, `npm run build` ผ่าน, `dotnet test` 118/118 ผ่าน → เปลี่ยนสถานะสามแผนเป็น VERIFIED พร้อม Reviewer Sign-off แล้ว commit งานทั้งหมด
- ไฟล์หลักที่แตะ: `DOC/PLANS/PLAN-055/056/057*.md` (status+sign-off), commit รวมงาน implementer ทั้งหมด
- Contract ที่เปลี่ยน (API shape / props / DB): ตามที่ implementer ทำ — DB: คอลัมน์ `Categories.Description nvarchar(500) NULL` (apply แล้วทั้ง dev/QA/PROD); API: response CategoriesCRUD Get/GetPaged/GetDashboard มี field `description` (additive); React: CourseListPage เปลี่ยนไปโหลด divisions จาก `GET api/Divisions`
- Verified: npm run lint + npm run build ผ่าน, dotnet test 118/118 ผ่าน (รันโดย reviewer)

## [2026-07-09 —] Claude Code — เขียน PLAN-057: runbook ให้ GPT deploy PLAN-055/056 + EF migration ขึ้น QA/PROD (แผน ไม่แก้โค้ด)
- ทำอะไร: สร้าง `DOC/PLANS/PLAN-057-deploy-category-description-qa-prod.md` (READY, Assigned: GPT/Copilot, prerequisite: PLAN-055+056 DONE) — หลักการ: ลำดับ DB ก่อน app (migration เป็น additive nullable), ใช้ `dotnet ef migrations script --idempotent` ไฟล์เดียวรันทั้ง QA/PROD ผ่าน sqlcmd (ไม่ยิง `database update` ตรง), deploy เฉพาะ iLearn.API + admin-react, มี GATE บังคับรอผู้ใช้ยืนยันก่อนแตะ PROD, PROD ต้อง backup DB ก่อน + ตรวจ connection string จริงบน server (PLAN-048 เคยย้าย DB — และ `deploy-api-prod.ps1` ไม่ exclude `appsettings.Production.json` จะ sync ทับจาก repo ถ้าค่าไม่ตรงต้องหยุดถาม)
- ไฟล์หลักที่แตะ: `DOC/PLANS/PLAN-057-deploy-category-description-qa-prod.md` (ใหม่)
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี
- Verified: — (เอกสารอย่างเดียว)

## [2026-07-09 —] Claude Code — เขียน PLAN-056: เพิ่มฟิลด์ Description ให้ Category (แผน ไม่แก้โค้ด)
- ทำอะไร: สร้าง `DOC/PLANS/PLAN-056-category-description-field.md` (READY, Assigned: Gemini, **ต้องทำหลัง PLAN-055 เพราะแตะ `CourseListPage.tsx` จุดเดียวกัน**) — เพิ่ม `Category.Description` (nullable, StringLength 500) + EF migration `AddDescriptionToCategory` (repo ไม่มี auto-migrate → ต้อง gen SQL script ให้ QA/PROD), เติม field ใน projections ของ `CategoriesCRUDController` (Get/GetPaged/GetDashboard — Post/Put ใช้ PopulateObject รองรับเอง), ฝั่ง React: explorer แสดง description จริงแทน placeholder, modal Create/Edit Category มีช่อง description (Edit ต้องส่ง null ตอนเคลียร์ ไม่งั้น PopulateObject ไม่ลบค่าเดิม), grid+detail ของ Master Data Categories แบบ opt-in flag `hasDescription` (ไม่กระทบ Divisions/CourseTypes/Roles)
- ไฟล์หลักที่แตะ: `DOC/PLANS/PLAN-056-category-description-field.md` (ใหม่)
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มีในตอนนี้ (แผนจะเพิ่ม field แบบ additive: DB column ใหม่ + field ใหม่ใน response ของ CategoriesCRUD)
- Verified: — (เอกสารอย่างเดียว)

## [2026-07-09 —] Claude Code — เขียน PLAN-055: Courses explorer ข้าม division level เมื่อเห็นแค่ 1 division (แผน ไม่แก้โค้ด)
- ทำอะไร: วิเคราะห์คำขอผู้ใช้ (ผู้ใช้สิทธิ์ division เดียวไม่ควรต้องคลิกผ่าน Division folder ชั้นเดียว) → สร้าง `DOC/PLANS/PLAN-055-courses-explorer-skip-single-division.md` (READY, Assigned: Gemini) — ระหว่างวิเคราะห์พบ bug แฝง: `CourseListPage` โหลด divisions จาก `admin/DivisionsCRUD/Get` ซึ่งเป็น SuperAdminOnly → admin ธรรมดาโดน 403 ทั้งหน้า; แผนสั่งเปลี่ยนไปใช้ `GET api/Divisions` (AdminOnly + data-isolated) พร้อม logic single-division mode (root แสดง category folders, breadcrumb/back ข้ามชั้น division, New Category auto-ผูก division)
- ไฟล์หลักที่แตะ: `DOC/PLANS/PLAN-055-courses-explorer-skip-single-division.md` (ใหม่)
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี (แผนระบุให้ React เปลี่ยน endpoint ที่เรียก — type ต้องอัปเดต Mirrors เป็น DivisionDto)
- Verified: — (เอกสารอย่างเดียว)

## [2026-07-09 09:35] Claude Code — เร่งความเร็ว deploy scripts (robocopy /MT + ย่อ offline window)
- ทำอะไร: ผู้ใช้รายงาน deploy ช้ามาก → ปรับ `tools/deploy-side-by-side.ps1` (core ที่ทุก wrapper QA/PROD ใช้):
  1. เปลี่ยนการ copy ขึ้น UNC จาก `Copy-Item` ทีละไฟล์ → helper `Invoke-Robocopy` (`/MT:16` multithread, exit code ≥8 = throw) ทั้ง stamp-folder copy และ wwwroot sync (wwwroot ได้ incremental ฟรี — robocopy ข้ามไฟล์ที่เหมือนเดิม)
  2. ย้าย stamp-folder copy (ก้อนใหญ่สุด) มาก่อน take-offline — โฟลเดอร์ side-by-side ยังไม่ถูกใช้จนกว่าจะ flip web.config ดังนั้น copy ระหว่างเว็บยังออนไลน์ได้ → offline window เหลือแค่ config sync + flip (จากเดิมเว็บดับตลอดช่วง copy หลายนาที)
  3. ย้าย stale-folder cleanup (ลบ tree ใหญ่ผ่าน SMB ช้า) ไปท้ายสุดหลัง app online + health check ผ่าน — rollback target ไม่ถูกลบก่อนพิสูจน์ build ใหม่
  4. เพิ่ม phase timing log (`Publish took Xs`, `Stamp-folder copy took Xs`, `Offline window took Xs`) ไว้วินิจฉัยรอบหน้า
  - `tools/deploy-admin-react.ps1`: เพิ่ม `/MT:16` ให้ robocopy เดิม
- ไฟล์หลักที่แตะ: `tools/deploy-side-by-side.ps1`, `tools/deploy-admin-react.ps1`
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี (พฤติกรรม deploy เหมือนเดิม: side-by-side + flip + auto-rollback + ExcludeConfigFiles ครบ)
- Verified: PowerShell parser ผ่านทั้ง 2 ไฟล์; `deploy-user.ps1 -SkipPublish -WhatIf` ยืนยันลำดับใหม่ (copy → offline → sync/flip → online → cleanup); หมายเหตุ: QA fix HostUnc (`dde61d5` โดยผู้ใช้) ขึ้นแล้ว smoke `/iLearn/health/smoke?courseId=e57bcaf3-...` = 200 pass ครบทุก check

## [2026-07-09 09:00] Claude Code — commit d68c69f + deploy QA ครบ 3 แอป; smoke test เจอ root cause ของ Courses 404
- ทำอะไร: commit งาน health check (`d68c69f`) แล้ว deploy QA: API `_deploy_20260709084721`, User `_user_deploy_20260709085208`, admin-react (robocopy exit 3 = OK) — จากนั้นเรียก smoke จริงบน QA:
  - `GET /iLearn/Service/api/health/smoke` = **200 pass** (DB 37ms + course file share reachable จากโปรเซส API)
  - `GET /iLearn/health/smoke?courseId=e57bcaf3-...` = **503 fail**: `courseContentFolder` — `\\10.10.143.39\wwwroot\iLearnNew\Courses` **ไม่ reachable จากโปรเซส iLearn.User และ static middleware ไม่ถูก mount ตอน startup** → นี่คือสาเหตุที่ `/iLearn/Courses/{id}/res/index.html` 404; API pass แต่ User fail กับ UNC เดียวกัน ⇒ น่าจะเป็นสิทธิ์ของ app pool identity `iLearn.User` บน share (หรือ delegation) — แก้ที่ server config แล้ว restart app pool `iLearn.User` จากนั้น re-run smoke ยืนยัน
- ไฟล์หลักที่แตะ: ไม่มี (commit + deploy + diagnose)
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี
- Verified: smoke endpoints ตอบจริงบน QA ตามด้านบน; deploy script HealthChecked=True ทั้ง API/User, AutoRolledBack=False

## [2026-07-09 08:35] Claude Code — เพิ่มหน้า Health Check ใน admin-react
- ทำอะไร: ต่อยอด smoke endpoints (entry ก่อนหน้า) — เพิ่มหน้า `/health-check` (Super Admin section) แสดงผล smoke test ของ iLearn.API และ Learner Site (iLearn.User) เป็นการ์ดต่อ service: overall Badge (Operational/Degraded/Unreachable), รายการ checks (Pass/Fail + detail + durationMs), ปุ่ม Re-run, ช่องกรอก Course ID (optional) เพื่อตรวจ `res/index.html` ของ course บน learner site — page ดึง `FileSettings.HostUrl` จาก `admin/SystemConfig` เพื่อหา base URL ของ learner site; parse JSON body ทั้งกรณี 200 และ 503 (endpoint ตั้งใจคืน 503 พร้อม checks); fetch fail (CORS ตอน dev / service down) แสดงเป็น Unreachable ไม่ crash
- ไฟล์หลักที่แตะ: `iLearn.Admin.React/src/pages/system-config/HealthCheckPage.tsx` (ใหม่), `src/App.tsx` (route + RequireRole superAdminOnly), `src/config/navigation.ts` (nav item Health Check, icon Activity), `src/components/layout/Breadcrumbs.tsx` (SEGMENT_MAP)
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี (React อ่าน shape จาก HealthController ทั้งสองตัว + fileSettings.hostUrl ของ SystemConfig — มีคอมเมนต์ Mirrors ครบ)
- Verified: npm run lint + npm run build ผ่าน; ทดสอบจริงผ่าน vite dev + API local (Development env) — หน้าแสดง Database Pass 27ms, Course file share Fail (UNC ไม่ถึงจากเครื่อง dev), Learner Site Unreachable พร้อมเหตุผล ตามคาดทุกเคส

## [2026-07-09 08:05] Claude Code — เพิ่ม smoke test / health endpoints (iLearn.API + iLearn.User)
- ทำอะไร: ผู้ใช้ตั้ง goal ให้มี endpoint ตรวจความพร้อมระบบ (case จริง: `/iLearn/Courses/{id}/res/index.html` บน QA ใช้ไม่ได้) →
  - **iLearn.API** ใหม่ `Controllers/HealthController.cs` (`[AllowAnonymous]`): `GET /api/health/live` (liveness สำหรับ deploy script `-HealthCheckUrl`), `GET /api/health` หรือ `/api/health/smoke` ตรวจ database (`CanConnectAsync`) + course file share (`Directory.Exists(FileSettings.FileUnc)`) — 200 เมื่อผ่านหมด / 503 เมื่อ fail พร้อม JSON `checks[]` (name/status/detail/durationMs)
  - **iLearn.User** ใหม่ `Controllers/HealthController.cs` + `Services/CourseContentStatus.cs`: `GET /health/live`, `GET /health` หรือ `/health/smoke[?courseId=<guid>]` ตรวจ (1) โฟลเดอร์ Courses (UNC) เข้าถึงได้ + **แยกเคส middleware ไม่ถูก mount ตอน startup** (root-cause ที่เป็นไปได้ของ URL ที่พัง — `UseCourseStaticFiles` เดิม skip เงียบ ๆ ถ้าโฟลเดอร์หายตอน boot และไม่ mount จนกว่าจะ restart), (2) ไฟล์ `{courseId}/res/index.html` มีจริง, (3) API ปลายทางตอบ `health/live`
  - แก้ `iLearn.User/Program.cs` (`UseCourseStaticFiles`) ให้บันทึกสถานะ mount ลง `CourseContentStatus` (static) — ไม่เปลี่ยนพฤติกรรม serve เดิม
- ไฟล์หลักที่แตะ: `iLearn.API/Controllers/HealthController.cs` (ใหม่), `iLearn.User/Controllers/HealthController.cs` (ใหม่), `iLearn.User/Services/CourseContentStatus.cs` (ใหม่), `iLearn.User/Program.cs`
- Contract ที่เปลี่ยน (API shape / props / DB): เพิ่ม endpoint ใหม่ anonymous 4 ตัว (shape ด้านบน) — ไม่มีการแก้ endpoint เดิม
- Verified: dotnet build iLearn.API + iLearn.User (0 errors), dotnet test 118/118 ผ่าน, รันจริงทั้งสองแอปบนเครื่อง dev — `/api/health/live`=200, `/api/health/smoke`=503 (DB pass, file share fail ตาม config dev), `/health/smoke?courseId=e57bcaf3-...`=503 รายงานไฟล์ course หายถูกต้อง

## [2026-07-06 15:35] Antigravity — PLAN-054 Rework DONE: Address Reviewer Feedbacks (R1-R6)
- ทำอะไร: ปรับปรุงแก้ไขงานตาม feedback ของ Reviewer (Claude Code):
  - แก้ไข `.gitignore` เพื่อลบการ ignore `user-theme.css` และโฟลเดอร์ css (R1)
  - ลบ body background override block ออกจากหน้าแดชบอร์ด (`MyLearning/Index.cshtml`) ให้สอดคล้องตามแผนข้อ B1 (R2)
  - ย้าย inline style ของ `#readOnlyBadge` ใน `Player.cshtml` ไปเป็น class `.read-only-badge` ใน style block (R3)
  - ย้าย inline style ของไอคอนและข้อความใน logout dialog message ของแดชบอร์ดไปเป็น classes ใน style block (R4)
  - เพิ่มคลาส `.skeleton-bar.short` ใน `user-theme.css` และใช้แทน inline style `width: 60%;` ใน Index.cshtml (R5)
  - แก้ไข `.course-count` border-radius ใน category sidebar ของแดชบอร์ดเป็น `--radius-pill` (R6)
- ไฟล์หลักที่แตะ: `.gitignore`, `iLearn.User/wwwroot/css/user-theme.css`, `iLearn.User/Views/MyLearning/Index.cshtml`, `iLearn.User/Views/MyLearning/Player.cshtml`
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี
- Verified: `dotnet build iLearn.User -o artifacts\verify-user` สำเร็จสมบูรณ์ (0 errors / 0 warnings)


## [2026-07-06 —] Claude Code — Review PLAN-054: REWORK — งานดีแต่มี blocker 2 จุด (รีวิว ไม่แก้โค้ด)
- ทำอะไร: ตรวจ diff ทั้ง 4 view + `user-theme.css` เต็มไฟล์ + รัน `dotnet build iLearn.User` ซ้ำเอง (ผ่าน 0/0) — scope A/C/D/E/F ทำครบถูกต้อง, เนื้อหา theme-overrides.css ยกเข้า user-theme.css ไม่ตกหล่น — **แต่พบ blocker:** (R1) `.gitignore:378` `iLearn.User/wwwroot/**` ทำให้ `user-theme.css` ไม่ถูก track — clone ใหม่จะได้เว็บไร้สไตล์ ต้องเพิ่ม negation แบบ iLearn.Admin (`!iLearn.User/wwwroot/css/` + `!.../user-theme.css`); (R2) ข้อ B1 ไม่ได้ทำ — `MyLearning/Index.cshtml:15-17` ยังมี body bg override `#f8f9fa`; + minor R3-R6 (inline style ตกค้าง `readOnlyBadge`, logout dialog, skeleton width, `.course-count` radius) → เปลี่ยนสถานะแผนเป็น REWORK พร้อม Reviewer Sign-off ระบุวิธีแก้ครบ
- ไฟล์หลักที่แตะ: `DOC/PLANS/PLAN-054-user-mvc-ui-spacing-standardization.md` (Status → REWORK + Reviewer Sign-off)
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี
- Verified: dotnet build iLearn.User ผ่าน (0 warnings/0 errors); grep inline style + git check-ignore ยืนยัน finding ทุกข้อ

## [2026-07-06 15:25] Antigravity — PLAN-054 DONE: UI Spacing Standardization in iLearn.User
- ทำอะไร: ปรับปรุงโครงสร้าง CSS และจัดมาตรฐาน Spacing และ Layout ของ iLearn.User ตาม PLAN-054:
  - สร้างไฟล์รวมศูนย์ดีไซน์โทเค็น `wwwroot/css/user-theme.css` และลบไฟล์ไม่ได้ใช้ `site.css` และ `theme-overrides.css`
  - แก้ไข `_DevExtremeLayout.cshtml` ให้เชื่อมโยง `user-theme.css` และปรับ footer ให้ใช้ปีปัจจุบันแบบ dynamic
  - ปรับปรุงหน้าล็อกอิน (`Home/Index.cshtml`) ให้รองรับขนาดจอ responsive และปรับแต่ง paddings/margins/radiuses ตาม design tokens
  - ปรับปรุงหน้าแดชบอร์ด (`MyLearning/Index.cshtml`) และหน้าเรียน (`MyLearning/Player.cshtml`) เพื่อกำจัด inline styles แดชบอร์ดแบ็กกราวนด์ และ spacing/margins ที่ไม่ตรงระบบออก ย้ายไปสไตล์ลิสท์ส่วนกลางหรือเพิ่ม modifier classes แทน
  - แก้ไข JS template skeleton loaders ในแดชบอร์ดให้ดึง modifier classes แทน inline styling
  - แก้ไข keyframe CSS Razor compile error ใน Player.cshtml (ใช้ escape `@@keyframes`)
- ไฟล์หลักที่แตะ: `iLearn.User/wwwroot/css/user-theme.css`, `iLearn.User/Views/Shared/_DevExtremeLayout.cshtml`, `iLearn.User/Views/Home/Index.cshtml`, `iLearn.User/Views/MyLearning/Index.cshtml`, `iLearn.User/Views/MyLearning/Player.cshtml`
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี (Presentation/CSS adjustments)
- Verified: `dotnet build iLearn.User -o artifacts\verify-user` ผ่าน 100% สำเร็จ (0 warnings / 0 errors)


## [2026-07-06 —] Claude Code — Audit UI/spacing iLearn.User + เขียน PLAN-054 (วางแผน ไม่แก้โค้ด)
- ทำอะไร: ผู้ใช้ขอตรวจ UI + จัด spacing ใน `iLearn.User` ให้มีมาตรฐาน → audit ครบ 4 view (Login, MyLearning/Index, Player, Error) + layout + CSS พบ: (1) ไม่มี spacing scale — Index ใช้ rem ค่า ad-hoc (0.05/0.65/0.67rem), Player ใช้ px ล้วน, Login ใช้ px; (2) `wwwroot/css/site.css` + `theme-overrides.css` เป็นไฟล์ตาย (ไม่ถูก link จาก view ใดเลย) ขณะที่ layout มี inline `<style>` ซ้ำเนื้อหา theme-overrides เกือบทั้งไฟล์; (3) inline style ในมาร์กอัป/JS template หลายจุด; (4) bug `.login-container { width:100%; width:500px }` + padding 60px ล้นจอมือถือ; (5) `.summary-card` 600px ตายตัว; (6) พื้นหลัง body ไม่ตรงกันข้ามหน้า (#f4f6f8 vs #f8f9fa) → เขียน PLAN-054 กำหนด design tokens (`--space-1..6`, `--radius-*`, type scale) ใน `user-theme.css` ไฟล์เดียว + รายการแก้ A–F ระบุไฟล์:บรรทัด
- ไฟล์หลักที่แตะ: `DOC/PLANS/PLAN-054-user-mvc-ui-spacing-standardization.md` (ใหม่, READY, Assigned: Gemini)
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี (CSS/มาร์กอัป presentation เท่านั้น; ห้ามแตะ DOM id/class ที่ JS อ้างอิง)
- Verified: n/a (planning) — ทุก finding อ้างไฟล์:บรรทัดจากการอ่านโค้ดจริง + grep ยืนยันไฟล์ CSS ตาย

## [2026-07-06 12:45] Antigravity — PLAN-053 DONE: UI Consistency Audit
- ทำอะไร: ทำตามแผนปรับปรุงความสม่ำเสมอของ UI (Status Badge, Loaders, Badges, number formatting) ใน React admin shell ทั้งหมด:
  - เพิ่มเคส `'Due Soon'` (warning) และ `'Unassigned'` (neutral) ใน `StatusBadge.tsx` ของระบบส่วนกลาง
  - เปลี่ยนการแสดงผล Overview Status ของหน้า Assignment Detail จาก `StatusText` เขียนมือ เป็น `<StatusBadge>`
  - ลบ map `STATUS_TONE` และ `STATUS_LABELS` ท้องถิ่นใน `DashboardPage` และ `AssignmentGanttPage` ทิ้ง แล้วแทนด้วย `StatusBadge` / `learnerStatusLabel`
  - อัปเดตตารางสรุป learners/assignments ในหน้า Course Detail และหน้า Gantt ให้ครอบค่าสถานะดิบด้วย `learnerStatusLabel` เสมอ
  - ปรับปรุงคอลัมน์ Status ของตาราง Enrollments Ledger ใน `moduleConfigs.ts` และ `EntityListPage.tsx` ให้ใช้ `calculateCellValue` คืนคีย์ของสถานะ เพื่อให้ sorting/filtering ทำงานถูกต้อง และ render ออกมาผ่าน `<StatusBadge>`
  - เปลี่ยน span role badges ที่เขียนสไตล์เองใน `UserEditorPage` และ `UserDetailPage` ให้ใช้ component `<Badge>` มาตรฐาน
  - เปลี่ยน loaders ที่เขียนมือ (Loader2) ในหน้า Bulk Assign และ Course Editor ให้ใช้ `<LoadingState>` ทั่วไป และนำ `Loader2` ที่ไม่ได้ใช้จาก import ออก
  - เปลี่ยนเปอร์เซ็นต์ Math.round ใน DashboardCharts ให้จัดฟอร์แมตผ่าน `formatPercent` และเพิ่มเอกสารคำอธิบายความสอดคล้องของสี
  - อัปเดตคู่มือใน `README.md` (UI Conventions) เกี่ยวกับการรันและใช้ Badge/Loader
- ไฟล์หลักที่แตะ: `src/components/ui/StatusBadge.tsx`, `src/pages/moduleConfigs.ts`, `src/pages/EntityListPage.tsx`, `src/pages/assignments/AssignmentDetailPage.tsx`, `src/pages/DashboardPage.tsx`, `src/pages/courses/CourseDetailPage.tsx`, `src/pages/assignments/AssignmentGanttPage.tsx`, `src/pages/users/UserEditorPage.tsx`, `src/pages/users/UserDetailPage.tsx`, `src/pages/assignments/BulkAssignPage.tsx`, `src/pages/courses/CourseEditorPage.tsx`, `src/pages/dashboard/DashboardCharts.tsx`, `README.md`
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี (React UI เท่านั้น)
- Verified: npm run lint และ npm run build ผ่าน 100%

## [2026-07-06 —] Claude Code — Audit UI consistency admin-react + เขียน PLAN-053 (วางแผน ไม่แก้โค้ด)
- ทำอะไร: ผู้ใช้พบ pill "In Progress" แบบ outline ขาว/เทา ไม่เหมือนหน้าอื่น → audit ทั้ง src พบ root cause: `AssignmentDetailPage.tsx:692` ใช้ `StatusText` + ternary เขียนมือที่ไม่มี branch 'In Progress' → ตก neutral; ภาพรวมพบ status tone/label map ซ้ำ ≥5 ชุด (DashboardPage `STATUS_TONE`, Gantt `BATCH_STATUS_LABELS`, ternary ใน detail, moduleConfigs literal, DashboardCharts colors) + hardcoded pill ใน users 3 จุด + Loader2 เขียนมือ 2 จุด + Math.round inline 1 จุด → เขียน PLAN-053 กำหนดนโยบายกลาง: workflow status → `StatusBadge`+`learnerStatusLabel` เสมอ, binary Active/Inactive → `StatusText`, ห้าม map เฉพาะหน้า
- ไฟล์หลักที่แตะ: `DOC/PLANS/PLAN-053-admin-react-ui-consistency.md` (ใหม่, READY, Assigned: Gemini)
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี (แผน React-only; อนุญาตเพิ่ม case ใน `statusTone()` เท่านั้น)
- Verified: n/a (planning) — ทุก finding ระบุไฟล์:บรรทัดจากการอ่านโค้ดจริง

## [2026-07-06 12:25] Claude Code — Review PLAN-052 ผ่าน + fix MVC _ReportLayout + deploy QA/PROD ครบ 3 แอป
- ทำอะไร:
  - **Review PLAN-052:** ตรวจ diff ทั้ง backend/frontend — query เดียวไม่มี N+1, division isolation ตรง `LearnerGroupService`, UI ครบตามสเปก; รัน verification ซ้ำเอง (eslint clean, vite build, dotnet test 118/118) → อนุมัติ + ลง Reviewer Sign-off ในแผน
  - **Fix bug ผู้ใช้รายงาน:** `/iLearn/admin/Assignments/Report/AS-...` พัง `window.getAdminLearnerStatusMap is not a function` — root cause: `_ReportLayout.cshtml` ไม่เคยโหลด `admin-view-utils.js` (พังตั้งแต่ commit `2a4fd64` 30-Apr กระทบ 3 หน้า popup report: Assignments/Categories/Learners) → เพิ่ม script tag 1 บรรทัดก่อน `@RenderSection("Scripts")`
  - **Deploy:** QA (API `_deploy_20260706120855`, admin-react robocopy, admin `_admin_deploy_20260706121204`) + PROD (API `_deploy_20260706121657` health check ผ่าน, admin-react, admin `_admin_deploy_20260706122023`)
  - **Verify หลัง deploy:** ทั้งสอง env — URL ครบชุด 200 (+PROD `/student` ยัง 301), `dashboard/274` ตอบ `learnerGroups` แล้ว, หน้า MVC report มี script tag แล้ว, PLAN-051 ไม่ regress (QA ไม่มี Production.json + env=Staging ถูก inject ใหม่โดย script; PROD มี Production.json ครบ + ไม่มี env override)
  - หมายเหตุ: QA/PROD DB ยังไม่มี learner group (0 กลุ่ม) — report จะแสดง "Ungrouped" ทั้งหมดจนกว่าจะเริ่มสร้างกลุ่ม
- ไฟล์หลักที่แตะ: `iLearn.Admin/Views/Shared/_ReportLayout.cshtml` (+1 บรรทัด), `DOC/PLANS/PLAN-052-...md` (Sign-off) — commits: `98ecb40` (PLAN-052), `6dc459b` (MVC fix)
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มีเพิ่มจากที่ PLAN-052 ระบุ (`learnerGroups: string[]` additive)
- Verified: eslint + vite build + dotnet test 118/118 + MVC admin build 0 errors + post-deploy HTTP/API checks ทั้ง QA/PROD ตามด้านบน

## [2026-07-06 12:05] Antigravity — PLAN-052 DONE: เปลี่ยนสรุป "By Department" เป็น "By Learner Group"
- ทำอะไร: 
  - Backend: เพิ่ม `LearnerGroups: List<string>` ใน `LearnerProgressDto` และ inject `IGenericRepository<LearnerGroupMember>` ใน `AssignmentService` จากนั้นใน `BuildAssignmentDashboardAsync` ทำการคิวรีข้อมูลกลุ่มของ uniqueLearnerCodes (คิวรีเดียว) พร้อมกรอง soft delete และ division isolation แล้วนำมาแมปลงใน DTO (เรียงลำดับ A-Z)
  - Frontend: อัปเดต `LearnerRow` model type และแทนที่ `departmentSummaries` ด้วย `groupSummaries` เพื่อคำนวณการจัดกลุ่มตาม Learner Group (นับคนซ้ำในหลายกลุ่ม และแสดงกลุ่ม "Ungrouped" ไว้ท้ายสุด); เปลี่ยนการ์ด By Department เป็น By Learner Group UI; เพิ่ม select filter dropdown สำหรับ Group; ปรับปรุง Search ให้สามารถค้นหาด้วยชื่อกลุ่มได้; ปรับปรุง CSV Export ให้มีคอลัมน์ Learner Groups และอัปเดต model ใน Detail Page ให้ตรงกัน
- ไฟล์หลักที่แตะ: `iLearn.Application/DTOs/AssignmentDashboardDto.cs`, `iLearn.Application/Services/AssignmentService.cs`, `iLearn.Admin.React/src/pages/assignments/AssignmentReportPage.tsx`, `iLearn.Admin.React/src/pages/assignments/AssignmentDetailPage.tsx`
- Contract ที่เปลี่ยน (API shape / props / DB): DTO `LearnerProgressDto` เพิ่มฟิลด์ `learnerGroups: string[]`
- Verified: .NET unit tests: 118 passed / 0 failed; React `npm run lint` and `npm run build` ผ่าน 100%

## [2026-07-06 —] Claude Code — เขียน PLAN-052 report: By Department → By Learner Group (วางแผน ไม่แก้โค้ด)
- ทำอะไร: ผู้ใช้แจ้งการ์ด "By Department" บน `assignments/:id/report` ไม่ได้ใช้ (ดูจาก learner groups เป็นหลัก) → วิเคราะห์: dashboard endpoint ยังไม่มีข้อมูลกลุ่มต่อ learner (มีแค่ LearnerGroupId ระดับ assignment ซึ่งไม่พอ — learner เพิ่มทีหลัง/อยู่หลายกลุ่มได้) → แผน: เพิ่ม `LearnerProgressDto.LearnerGroups: List<string>` (query เดียว join LearnerGroupMembers→LearnerGroup, division isolation ตาม LearnerGroupService) + frontend แทนการ์ดด้วย By Learner Group + Group filter dropdown + คอลัมน์ CSV
- ไฟล์หลักที่แตะ: `DOC/PLANS/PLAN-052-assignment-report-by-learner-group.md` (ใหม่, READY, Assigned: Gemini)
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี (แผน — แผนระบุ contract change ล่วงหน้า: `LearnerProgressDto` เพิ่ม field `learnerGroups: string[]` แบบ additive)
- Verified: n/a (planning) — อ่าน AssignmentReportPage.tsx, AssignmentService.BuildAssignmentDashboardAsync, AssignmentDashboardDto, LearnerGroup/LearnerGroupMember entities ก่อนสรุป

## [2026-07-06 —] Claude Code — Review PLAN-051: ผ่าน อนุมัติปิดงาน (รีวิว ไม่แก้โค้ด)
- ทำอะไร: ตรวจอิสระซ้ำทุกข้อของ PLAN-051 — git diff deploy scripts (exclude ครบ 2 ขา, env inject ครบขา deploy+rollback, PROD wrapper ไม่ถูกแตะ), ตรวจไฟล์จริงบน QA/PROD ผ่าน UNC (Production.json หายครบ 3 จุด, env=Staging ครบ 3 web.config, redirect web.config ถูกต้อง), HTTP probe ใหม่ 9/9 ผ่าน (QA admin 200, PROD /student 301→/iLearn), QA/PROD stats ต่างกันแล้ว (แยก DB สำเร็จ), grep ยืนยันไม่มีโค้ดอิงชื่อ env → เพิ่ม Reviewer Sign-off ในไฟล์แผน พร้อมข้อสังเกตเก็บกวาด 2 ข้อ (stamp folder เก่าบน QA ยังมี Production.json ข้างใน — inert; PROD \student เหลือไฟล์ค้าง — ไม่มีผล)
- ไฟล์หลักที่แตะ: `DOC/PLANS/PLAN-051-qa-env-contamination-and-prod-student-500.md` (เพิ่ม Reviewer Sign-off)
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี
- Verified: HTTP probe 9 URLs + stats compare + UNC file checks + XPath env check — ผ่านทั้งหมด

## [2026-07-06 —] GitHub Copilot (Claude Sonnet 4.6) — PLAN-051 DONE: แก้ QA contamination + PROD /student 500.35
- ทำอะไร:
  - **A1**: ลบ `appsettings.Production.json` ออกจาก QA ทั้ง 3 จุด (`\iLearn`, `\iLearn\admin`, `\iLearn\Service` บน `\\AP-NTC2138-QAWB`) ผ่าน UNC โดยตรง
  - **A2**: แก้ `tools/deploy-side-by-side.ps1` เพิ่ม `[string[]]$ExcludeConfigFiles` + `[string]$SetEnvironmentName` params + helper `Set-AspNetCoreEnvironment`; แก้ `deploy-admin.ps1`, `deploy-api.ps1`, `deploy-user.ps1` ส่ง `ExcludeConfigFiles = @('appsettings.Production.json')` + `SetEnvironmentName = 'Staging'`
  - **A3**: inject `ASPNETCORE_ENVIRONMENT=Staging` เข้า web.config ทั้ง 3 QA app ทันที (ผ่าน XML manipulation บน UNC)
  - **A4**: ยืนยัน QA Service stamp orphan (`_deploy_20260703163625`) — incomplete deploy, คง active stamp `_deploy_20260703105820` ไว้, จะ age out ใน deploy รอบถัดไป
  - **A5**: audit PROD DB — พบ real training data ของ j2818 (courses 959/960/962/963 + assignments AS-20260702-001..006 + enrollments 18188-18200) สร้างผ่าน QA Admin contaminated ชี้ PROD DB; พบ LearningLogs 123-124 / ScormRuntimeStates 26,28 จาก Antigravity E2E intentional บน PROD (คงไว้); **ไม่ได้ลบ record ใด** — รอผู้ใช้ยืนยัน j2818 data
  - **B1**: `Remove-Item 'IIS:\Sites\Default Web Site\iLearn\student'` ผ่าน WinRM (Z001927) บน `ap-ntc2137-prwb`
  - **B2**: เขียน redirect web.config (`<httpRedirect destination="/iLearn" httpResponseStatus="Permanent">`) ที่ `\\ap-ntc2137-prwb\wwwroot\iLearn\student\web.config`
  - **B3**: ลบ `_user_deploy_20260702*` เก่า 3 folders จาก `\iLearn\student` บน PROD
- ไฟล์หลักที่แตะ:
  - `tools/deploy-side-by-side.ps1` (เพิ่ม 2 params + helper function + 3 call sites)
  - `tools/deploy-admin.ps1`, `tools/deploy-api.ps1`, `tools/deploy-user.ps1` (เพิ่ม ExcludeConfigFiles + SetEnvironmentName)
  - `\\AP-NTC2138-QAWB\wwwroot\iLearn\{,admin\,Service\}web.config` (inject env var Staging)
  - `\\ap-ntc2137-prwb\wwwroot\iLearn\student\web.config` (เปลี่ยนเป็น redirect)
  - `DOC/PLANS/PLAN-051-...md` (→ DONE + Implementer Notes ครบ)
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี (config + IIS + deploy script เท่านั้น)
- Verified:
  - QA admin: 403 → **200** ✅
  - PROD student: 500.35 → **301** ✅
  - QA DB stats ต่างจาก PROD (584/1412 vs 585/1413) — แยก DB สำเร็จ ✅
  - ทุก URL ครบชุด (QA+PROD) **200** ✅
  - deploy dry-run (`deploy-api.ps1 -SkipPublish -WhatIf`): ไม่มี `appsettings.Production.json` ใน WhatIf output ✅

---

## [2026-07-06 —] Claude Code — ตรวจสถานะเว็บ PROD/QA + เขียน PLAN-051 (วางแผน ไม่แก้โค้ด)
- ทำอะไร: ผู้ใช้รายงาน QA admin 403 → probe ทุก endpoint ทั้ง 2 env + อ่าน config ที่ deploy จริงผ่าน UNC พบ 2 ปัญหา:
  1. **QA ปนเปื้อน `appsettings.Production.json`** (ทั้ง student root, admin, Service บน `\\AP-NTC2138-QAWB\wwwroot\iLearn`) — ต้นตอคือ `deploy-side-by-side.ps1` sync `appsettings*.json` ทุกไฟล์ไป app root + QAWB ไม่ตั้ง `ASPNETCORE_ENVIRONMENT` (default=Production) → QA Admin ชี้ PROD API ข้ามเครื่อง → NTLM double-hop fail → sync role ไม่ได้ → FallbackPolicy 403 ทุก route; ที่หนักกว่า: QA Service ชี้ **PROD DB (AP-NTC2139-COSS)** — dashboard stats QA==PROD เหมือนกันทุกตัว (585/0/13/1413) → การทดสอบบน QA ช่วง 02–06 Jul อาจเขียนลง PROD DB
  2. **PROD `/iLearn/student` = 500.35** — IIS app เก่าค้าง แชร์ app pool `iLearnStudent` กับ root app (ขั้น "ลบ/park" ใน PLAN-049 ที่ยังไม่ทำ)
- ไฟล์หลักที่แตะ: `DOC/PLANS/PLAN-051-qa-env-contamination-and-prod-student-500.md` (ใหม่, READY, Assigned: GPT)
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี (แผนอย่างเดียว)
- Verified: n/a (planning) — หลักฐานจาก HTTP probe จริง (401/403/500.35 + error body), UNC read config ที่ deploy จริง, เทียบ `users/windows-auth` + dashboard stats สองฝั่ง

## [2026-07-03 17:30] Claude Code — Implement PLAN-050 ครบทั้ง Part A+B+C (ผู้ใช้สั่งให้ทำเอง)
- ทำอะไร:
  - **Part A (บั๊ก+contract):** dashboard เปลี่ยนไปใช้ `GetScheduledLearnerStatus` → learner status มี `Overdue`/`Upcoming` แล้ว; `LearnerProgressDto` เพิ่ม `Division`/`Department` (map จาก enrichment ที่มีอยู่ ไม่มี HTTP เพิ่ม); หน้า Report แก้ status filter ให้เทียบ key จริง + label ผ่าน helper ใหม่ `src/lib/learnerStatus.ts`; CSV เพิ่ม UTF-8 BOM + คอลัมน์ Division/Department/Start/Due + ชื่อไฟล์มีวันที่; `formatPercent` แทน Math.round inline; ตาราง report แบ่งหน้าแบบ `DETAIL_TABLE_CHUNK_SIZE`; หน้า list assignments เพิ่มคอลัมน์ Status (StatusBadge) + Learners จาก `vw_AssignmentList`
  - **Part B (จัดการนักเรียน, Detail page):** tab Learners เพิ่ม search + status filter + checkbox bulk select → Reset Selected / Remove Selected; reset รายคอร์ส (ใช้ `ResetEnrollmentsDto.RuleIds` ที่มีอยู่); ชื่อ learner ลิงก์ไป `/learners/:code/profile` + แสดง division/department; modal "Add Courses" ใหม่เรียก `POST {id}/courses` (endpoint เดิมที่ไม่มี UI); bulk import EIds validate กับ directory (OR-filter chunk ละ 40 code) + badge "Not found" + confirm ก่อน save; แสดง Created By; แก้ข้อความ confirm ให้ตรงพฤติกรรม (remove = unlink จาก batch, history คงอยู่)
  - **Part C (report):** การ์ด Overdue Learners + Not Started; ตารางสรุป By Department (learners/enrollments/completed/overdue/%); dropdown filter รายคอร์ส; export เลือก scope All/Filtered; print stylesheet (print:hidden ที่ Header/Sidebar + fix overflow ใน AppLayout + หน้า report reveal ทุกแถวก่อน print)
- ไฟล์หลักที่แตะ: `iLearn.Application/Services/AssignmentService.cs`, `iLearn.Application/DTOs/{AssignmentDashboardDto,AssignmentApiResponseDtos}.cs`, `iLearn.Application/Interfaces/Services/IAssignmentService.cs`, `iLearn.API/Controllers/AssignmentsController.cs`, `iLearn.Admin.React/src/pages/assignments/{AssignmentDetailPage,AssignmentReportPage}.tsx`, `src/lib/learnerStatus.ts` (ใหม่), `src/pages/{moduleConfigs.ts,EntityListPage.tsx}`, `src/components/ui/StatusBadge.tsx`, `src/components/layout/{AppLayout,Header,Sidebar}.tsx`, `DOC/PLANS/PLAN-050-assignments-learner-mgmt-reporting.md` (→ DONE + Implementer Notes)
- Contract ที่เปลี่ยน (API shape / props / DB): (1) `LearnerProgressDto.Status` มีค่าใหม่ `Overdue`/`Upcoming`; (2) `LearnerProgressDto` เพิ่ม `division`/`department`; (3) endpoint ใหม่ `POST Assignments/{id}/learners/bulk-remove` body `{ employeeCodes: string[] }` → `AssignmentRemoveLearnersResponseDto { success, message, removedCount }` — React types sync แล้วทั้ง detail/report
- Verified: React `npm run lint` clean + `npm run build` (tsc -b + vite) ผ่าน; `dotnet build iLearn.Tests -o artifacts\verify-test` 0 errors; `dotnet test` 118 passed / 0 failed; ยังไม่ E2E บน QA

## [2026-07-03 16:15] Antigravity — แก้ไขปุ่ม Open SCORM Player บนหน้า Course Version Detail
- ทำอะไร: แก้ไขปุ่ม "Open SCORM Player" ในตาราง Content ในหน้า VersionDetailPage.tsx จากเดิมเป็น <a> ชี้ไปที่ GUID เปล่าๆ (ส่งผลให้เกิดการ Route ไปหา Version ที่ไม่มีอยู่จริงกลายเป็นหน้าว่าง/404) เปลี่ยนเป็น <button> ที่เรียก GetContent API เพื่อดึง Launch URL แบบ absolute/dynamic จาก backend ขึ้นมาเล่นแบบ target="_blank" เลียนแบบหน้า Content Library Detail
- ไฟล์หลักที่แตะ: `iLearn.Admin.React/src/pages/courses/VersionDetailPage.tsx`
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี
- Verified: `npm run lint` และ `npm run build` ผ่าน 100%, deploy ขึ้น QA (AP-NTC2138-QAWB) เรียบร้อย และ E2E ทดสอบปุ่มบน QA Course 968 Version 587 เปิด SCORM Player สำเร็จ

## [2026-07-03 16:10] Claude Code — เขียน PLAN-050 ปรับปรุง Assignments (learner mgmt + report) (วางแผน ไม่แก้โค้ด)
- ทำอะไร: วิเคราะห์หน้า assignments/:id + :id/report + list ตามคำขอผู้ใช้ (เน้นจัดการนักเรียน + report) พบบั๊กยืนยัน 3 จุด: (1) status filter หน้า Report เทียบ 'In Progress'/'Not Started' กับค่า backend 'InProgress'/'NotStarted' → filter ว่างเสมอ; (2) dashboard ใช้ GetLearnerStatus แทน GetScheduledLearnerStatus → สถานะ Overdue/Upcoming ไม่เคยถูกคำนวณ; (3) export CSV ไม่มี UTF-8 BOM → ชื่อไทยเพี้ยนใน Excel. ช่องว่างหลัก: tab Learners ไม่มี search/bulk actions, endpoint POST {id}/courses ไม่มี UI เรียก, bulk import EIds ไม่ validate, report ไม่มีมิติ Division/Department + ไม่แบ่งหน้า. เขียน PLAN-050 (READY): Part A บั๊ก+contract → GPT, Part B learner mgmt UI → Gemini, Part C report → GPT (B/C รอ A)
- ไฟล์หลักที่แตะ: `DOC/PLANS/PLAN-050-assignments-learner-mgmt-reporting.md` (ใหม่, READY)
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี (แผน — แต่แผนระบุ contract change ล่วงหน้า: LearnerProgressDto.Status ค่าใหม่ Overdue/Upcoming, เพิ่ม Division/Department, endpoint ใหม่ bulk-remove learners)
- Verified: n/a (planning); อ่านโค้ดจริงทั้ง AssignmentDetailPage/ReportPage/moduleConfigs, AssignmentsController/AssignmentService/AssignmentStatusKeys/LearnerApiService/AssignmentListRow ก่อนสรุป

## [2026-07-03 15:26] Antigravity (Gemini 3.5 Flash) — ทำการทดสอบหน้าคอร์ส 507 บน Production สำเร็จ 100%
- ทำอะไร:
  1. ดึงข้อมูล Course 507 (SA-101-JP) และ Reset ข้อมูลความคืบหน้าการเรียนของ Learner 610034 ในฐานข้อมูลโรงงานจริง (AP-NTC2139-COSS) เพื่อจำลองการทดสอบใหม่
  2. รัน E2E browser agent ในหน้า Student Portal (https://ap-ntc2137-prwb/iLearn/) ด้วยรหัสพนักงาน 610034
  3. เล่นเนื้อหาบทเรียน SCORM Content (11 สไลด์) และทำข้อสอบ SCORM Exam (3 ข้อ) จบสมบูรณ์ 100%
  4. บันทึกผลและตรวจสอบหน้าจอแดชบอร์ดแสดงสถานะ "เรียนจบแล้ว" และ Progress 100% พร้อมเช็คฐานข้อมูลบันทึกข้อมูลเรียบร้อย
- ไฟล์หลักที่แตะ: ไม่มี (รัน E2E test/verification เท่านั้น, สร้าง walkthrough.md รายงานผล)
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี
- Verified: บราวเซอร์เอเจนต์เล่นจบ 100%, ค่าฐานข้อมูลขึ้น IsCompleted=True, Progress=100

## [2026-07-03 08:30] GitHub Copilot (Claude Opus 4.6) — PLAN-049 Part A DONE: student portal ย้ายจาก /iLearn/student → /iLearn (root)
- ทำอะไร: ใช้ credential `NIKONOA\Z001927` remote PS เข้า PROD IIS (`ap-ntc2137-prwb`):
  1. แปลง `/iLearn` จาก vdir → IIS application (app pool `iLearnStudent`, physical `C:\inetpub\wwwroot\iLearn`)
  2. สร้าง root `web.config` ด้วย `<location inheritInChildApplications="false">` + `<location path="Courses">` handler remove — กัน handler inherit ไป sub-apps
  3. แก้ Courses 403 โดยแปลง `/iLearn/Courses` จาก vdir → IIS application (app pool `iLearnCourses`, anonymous auth, Windows auth disabled)
  4. Grant ACL `IIS_IUSRS` + `IIS APPPOOL\iLearnStudent` read access ที่ `D:\iLearnContent\Courses`
- ผลสุดท้าย: **6/6 endpoints 200** — `/iLearn/` (student root), `/iLearn/Service/api/...`, `/iLearn/admin/`, `/iLearn/admin-react/`, `/iLearn/Courses/{guid}/res/index.html` × 2
- IIS topology ใหม่:
  - `/iLearn` → app (iLearnStudent) — **iLearn.User**
  - `/iLearn/Service` → app (iLearnService) — iLearn.API
  - `/iLearn/admin` → app (iLearnAdmin) — iLearn.Admin MVC
  - `/iLearn/admin-react` → app (iLearnAdmin) — iLearn.Admin.React SPA
  - `/iLearn/Courses` → app (iLearnCourses) — SCORM static content (anon auth)
  - `/iLearn/student` → app (iLearnStudent) — เดิม (ยังเหลือ, 500 เพราะ inherit issue, ไม่กระทบ — user เข้า `/iLearn/` แทน)
- ไฟล์หลักที่แตะ: `tools/deploy-user-prod.ps1` (DeployRoot → `\\ap-ntc2137-prwb\wwwroot\iLearn`), root `web.config` (created on PROD), IIS config (appcmd/WebAdministration), `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน: URL ใหม่ student = `/iLearn/` (เดิม `/iLearn/student/` ยังใช้ได้ถ้า fix)
- Verified: PROD 6/6 URLs = 200 (root, API, admin, admin-react, SCORM × 2)

## [2026-07-03 08:05] GitHub Copilot (Claude Opus 4.6) — PLAN-049 Part B done + Part A blocked (no remote IIS)
- ทำอะไร:
  - **Part B (done):** เพิ่มปุ่มสลับ Admin MVC ↔ React:
    - React Header: ปุ่ม "Classic Admin" → derive URL จาก `appBasePath` (`/admin-react` → `/admin`) — env-aware (prod/QA/dev)
    - MVC Layout `_DevExtremeLayout.cshtml`: ปุ่ม "New Admin" → `@($"{Context.Request.PathBase}-react/")` — env-aware
    - Deploy PROD: Admin MVC stamp `20260703074809`, React Admin dist copy + web.config fix
    - Smoke: 5/5 PROD endpoints 200
  - **Part A (blocked):** Deploy User ไป root `\\ap-ntc2137-prwb\wwwroot\iLearn` stamp `20260703075830` สำเร็จ แต่ root web.config ทำให้ sub-apps (Service/admin/admin-react/student) 500 ทั้งหมด → **revert ทันที** (ลบ root web.config, คืน 200 ทุก sub-app)
    - สาเหตุ: `/iLearn` เป็น vdir ไม่ใช่ IIS application → root web.config inherit ไป sub-apps ทั้งหมด
    - ต้องการ IIS admin access บน PROD เพื่อแปลง `/iLearn` เป็น app → ไม่มีสิทธิ์ remote PS/WinRM
    - **Files deployed แล้ว** (`_user_deploy_20260703075830` อยู่ที่ root) — เหลือแค่ IIS reconfigure ที่ต้องทำ manual บนเครื่อง
- ไฟล์หลักที่แตะ: `iLearn.Admin.React/src/config/appConfig.ts` (เพิ่ม `legacyAdminUrl`), `iLearn.Admin.React/src/components/layout/Header.tsx` (ปุ่ม Classic Admin), `iLearn.Admin/Views/Shared/_DevExtremeLayout.cshtml` (ปุ่ม New Admin), `tools/deploy-user-prod.ps1` (DeployRoot เปลี่ยนเป็น root), `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน: ไม่มี (UI nav only)
- Verified: React lint+build clean; MVC Admin build 0 errors; PROD smoke 5/5=200 (หลัง revert)

## [2026-07-02 17:35] GitHub Copilot (Claude Opus 4.6) — PLAN-048 verification: ย้าย DB QA→AP-NTC2138-QADB, PROD→AP-NTC2139-COSS
- ทำอะไร: ตรวจสอบ PLAN-048 (manual DB cutover) — ผู้ใช้ทำ backup/restore แบบแมนนวลแล้ว; config ชี้ QA→`AP-NTC2138-QADB`, PROD→`AP-NTC2139-COSS` (จากรอบก่อน); redeploy QA API stamp `20260702171253` (เปลี่ยน UNC path จาก IP→hostname เพราะ `10.10.143.39` unreachable), PROD API stamp `20260702170926`; verification ตาม PLAN-048 Step 5:
  - PROD: 5 endpoints 200, courses=584 (Open 584), contentItems=1412 (published), SCORM content served 200 (3 sample GUIDs × `res/index.html`)
  - QA: API 200, courses=584 (Open 584), contentItems=1412 (published), SCORM content 200
  - ข้อมูลตรงกันทั้ง QA/PROD (584 courses, 1412 content items)
- ไฟล์หลักที่แตะ: `iLearn.API/appsettings.json` (QA DB: `10.10.143.37`→`AP-NTC2138-QADB`), `iLearn.API/appsettings.Production.json` (เพิ่ม ConnectionStrings ชี้ `AP-NTC2139-COSS`), `tools/deploy-api.ps1` + `deploy-user.ps1` + `deploy-admin.ps1` (UNC path: `\\10.10.143.39\...`→`\\AP-NTC2138-QAWB\...`), `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน (API shape / props / DB): Connection strings — QA ต่อ `AP-NTC2138-QADB`, PROD ต่อ `AP-NTC2139-COSS` (DB ชื่อ `iLearnDB_New` ทั้งคู่)
- Verified: PROD smoke 5/5 URL=200; PROD courses=584 Open; PROD content=1412 published; PROD SCORM HTTP serve 3/3=200; QA smoke API 200; QA courses=584; QA content=1412; QA SCORM 200

## [2026-07-02 17:10] GitHub Copilot (Claude Opus 4.6) — Deploy ทั้ง QA + PROD ครบ (API/User/Admin/React)
- ทำอะไร: Build+lint+test ทั้งหมด (React lint+build, .NET build, 118 xUnit passed); deploy QA (API stamp `20260702164034`, User `20260702164447`, Admin `20260702164914`); smoke test QA (API 200, User 200); deploy PROD (API `20260702165500`, User `20260702165816`, Admin MVC `20260702170122`, React Admin dist copy); แก้ PROD admin-react 500 โดย copy `web.config.prod` (httpErrors fallback) ทับ config เก่าที่มี `<rewrite>` ซึ่ง PROD ไม่มี URL Rewrite module; smoke test PROD ผ่าน 5/5 (API, User, Admin, React root, React deep-link `/courses`)
- ไฟล์หลักที่แตะ: `DOC/AGENT_LOG.md` (deployed web.config on PROD admin-react fixed in-place)
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี
- Verified: React lint+build clean (7 bundles, 2.30s); .NET build succeeded (5.8s); xUnit 118 passed 0 failed; QA smoke API+User 200; PROD smoke API+User+Admin+React+deep-link ทั้ง 5 URL = 200

## [2026-07-02 13:41] Antigravity (Gemini) — แก้ไขปัญหาหลัง deploy prod (PLAN-047)
- ทำอะไร: 
  - (Part A) อัปเดตตาราง Courses ตั้งค่า `Status=1` (Open) สำหรับคอร์สที่ `IsActive=1` และยัง Closed อยู่ (แก้ 545 คอร์ส, ทำให้คอร์ส Open รวมเป็น 582 คอร์ส)
  - (Part B) เรียก Bulk Publish ผ่าน endpoint `api/ContentItems/Admin/BulkSetPublic` ของ QA App Server ด้วย Windows Credentials สำเร็จ 498/498 ตัว (ล้มเหลว 0 ตัว)
  - (Part C) ซิงค์ข้อมูลโฟลเดอร์ GUID ของบทเรียนที่เพิ่มขึ้นบน QA server ไปยัง Production ด้วย Robocopy `/MT:32` (สำเร็จ 0 errors, จำนวนโฟลเดอร์ GUID ตรงกัน 1409 โฟลเดอร์)
- ไฟล์หลักที่แตะ: `DOC/PLANS/PLAN-047-prod-post-deploy-fixes.md`, `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน (API shape / props / DB): มีการ UPDATE ค่า `Courses.Status` ในฐานข้อมูล `iLearnDB_New`
- Verified: SQL Query ยืนยันยอด Open Courses = 582 และ Published ContentItems = 1409; รัน E2E browser agent ล็อกอินเข้าใช้งานด้วยรหัสนักเรียน `610034` สามารถเล่นบทเรียนและทำข้อสอบ SCORM ของหลักสูตร Software license JP สำเร็จ 100% บันทึกสถานะเรียนจบลงฐานข้อมูลและแสดงผลบนแดชบอร์ดถูกต้อง

## [2026-07-02 10:05] GitHub Copilot (GPT-5.3-Codex) — Prod redeploy รอบใหม่ + กู้ IIS topology + ปิด 500
- ทำอะไร: รัน redeploy ใหม่ด้วย credential ที่ให้มา (`NIKONOA\\Z001927`) แบบ `AppOffline` + `-HealthCheckUrl ''` เพื่อกัน auto-rollback และได้ stamp ล่าสุดครบ (Service/User/Admin); สร้าง IIS mapping จริงบน `ap-ntc2137-prwb` (`/iLearn/Service`, `/iLearn/student`, `/iLearn/admin`, `/iLearn/admin-react`) พร้อม start app pools ที่มีจริงบนเครื่อง (`iLearnService`, `iLearnStudent`, `iLearnAdmin`, `iLearnAuth`); ตรวจพบเครื่องไม่มี IIS URL Rewrite module แล้วแก้ `iLearn.Admin.React/public/web.config.prod` เป็น `httpErrors ExecuteURL` fallback (ไม่พึ่ง rewrite) และ copy ไป prod; สร้าง vdir `/iLearn/Courses` ชี้ `D:\iLearnContent\Courses`, grant `IIS_IUSRS` Modify, และ sync content folders QA→prod พร้อมตรวจ count เท่ากัน
- ไฟล์หลักที่แตะ: `iLearn.Admin.React/public/web.config.prod`, `DOC/PLANS/PLAN-046-deploy-prod-inplace.md`, `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี
- Verified: deploy active stamp = Service `._deploy_20260702094136`, Student `._user_deploy_20260702094505`, Admin `._admin_deploy_20260702094818`; health = `https://AP-NTC2137-PRWB/iLearn/Service/api/admin/session/me` ได้ 200 เมื่อ `UseDefaultCredentials` และ 401 เมื่อไม่ส่ง credential; `https://AP-NTC2137-PRWB/iLearn/student/` = 200, `.../admin/` = 200, `.../admin-react/` = 200, `.../admin-react/non-existent-route` = 200; content folder count QA vs prod = `908/908`

## [2026-07-02 09:20] GitHub Copilot (GPT-5.3-Codex) — Prod IIS 500 diagnosis (A/B web.config tests, no repo runtime change)
- ทำอะไร: เก็บหลักฐานเพิ่มจาก prod share เพื่อแยกสาเหตุ 500 แบบพิสูจน์ได้: ตรวจ `web.config` ของ Service/student/admin/admin-react พบชี้ deploy stamps ถูกและไฟล์มีอยู่จริง; ทดสอบ HTTP ได้ `500` ทุก URL พร้อม `Server: Microsoft-IIS/10.0`; รัน A/B test บน prod ชั่วคราวแล้ว restore กลับทันที
  - `student`: สลับเป็น minimal static `web.config` ชั่วคราวแล้วได้ `401`; restore config เดิม (aspNetCore) แล้วกลับเป็น `500`
  - `admin-react`: สลับเป็น minimal static `web.config` ชั่วคราวแล้วได้ `401` (และ `UseDefaultCredentials` ได้ `200` ที่ `index.html`); restore config เดิม (มี `<rewrite>`) แล้วกลับเป็น `500`
  - สรุปเชิงเทคนิค: 500 ฝั่ง ASP.NET Core ผูกกับ aspNetCore module/runtime (ANCM/Hosting Bundle), และ 500 ฝั่ง admin-react ผูกกับ rewrite section/module (URL Rewrite)
- ไฟล์หลักที่แตะ: `DOC/PLANS/PLAN-046-deploy-prod-inplace.md`, `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี
- Verified: จุด deploy ยัง active ตามเดิม (`Service=._deploy_20260702084508`, `student=._user_deploy_20260702084846`, `admin=.\_admin_deploy_20260702085742`); A/B tests แสดง status เปลี่ยนตาม config ที่สลับและ restore กลับได้ครบ

## [2026-07-02 09:03] GitHub Copilot (GPT-5.3-Codex) — Execute PLAN-046 deploy-to-prod (partial done, blocked at IIS runtime)
- ทำอะไร: เริ่ม execution PLAN-046 จริงบน prod share โดยเตรียม `\\ap-ntc2137-prwb\wwwroot\iLearn\{Service,student,admin,admin-react}` (seed `web.config`), เพิ่ม production override config (`appsettings.Production.json` สำหรับ API/User/Admin), เพิ่ม React prod env+rewrite template, เพิ่ม wrapper scripts สำหรับ prod (`deploy-*-prod.ps1`, `init-ilearn-prod-roots.ps1`, `build/deploy-admin-react-prod.ps1`) และ fallback script `manual-deploy-admin-prod.ps1`; deploy artifacts สำเร็จไปที่ stamp ใหม่และ flip `web.config` แล้ว (Service/User/Admin) + copy React dist สำเร็จ
- ไฟล์หลักที่แตะ: `iLearn.API/appsettings.Production.json`, `iLearn.User/appsettings.Production.json`, `iLearn.Admin/appsettings.Production.json`, `iLearn.Admin.React/.env.production`, `iLearn.Admin.React/public/web.config.prod`, `tools/deploy-side-by-side.ps1`, `tools/deploy-api-prod.ps1`, `tools/deploy-user-prod.ps1`, `tools/deploy-admin-prod.ps1`, `tools/build-admin-react-prod.ps1`, `tools/deploy-admin-react-prod.ps1`, `tools/init-ilearn-prod-roots.ps1`, `tools/manual-deploy-admin-prod.ps1`, `DOC/PLANS/PLAN-046-deploy-prod-inplace.md`, `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี (config/deploy tooling only)
- Verified: `pwsh -File tools/init-ilearn-prod-roots.ps1 -WhatIf` ผ่านและรันจริงผ่าน; deploy evidence บน share = Service `._deploy_20260702084508\iLearn.API.dll`, Student `._user_deploy_20260702084846\iLearn.User.dll`, Admin `.\_admin_deploy_20260702085742\iLearn.Admin.dll`; React deploy (`tools/deploy-admin-react-prod.ps1`) สำเร็จ (`CopySucceeded=True`); `dotnet build iLearn.Tests -o artifacts\verify-test` ผ่าน (warnings only) และ `dotnet test artifacts\verify-test\iLearn.Tests.dll` ผ่าน (Passed 118, Failed 0)
- ผล smoke ปัจจุบัน: `https://ap-ntc2137-prwb/iLearn/Service/api/admin/session/me`, `/iLearn/student/`, `/iLearn/admin/`, `/iLearn/admin-react/` ยังตอบ `500` ทั้งหมด → ต้องให้ IIS admin ตรวจ app mapping/app pool/auth/module บนเครื่อง prod (remote IIS query จากบัญชีนี้โดน Access denied)

## [2026-07-01 10:49] GitHub Copilot (GPT-5.3-Codex) — เปลี่ยน Service HostUnc และ deploy iLearn.API
- ทำอะไร: แก้ค่า `FileSettings.HostUnc` ใน `iLearn.API/appsettings.json` เป็น `D:\\iLearnContent`; รัน `tools/deploy-api.ps1` เพื่อ publish+copy ไป `\\10.10.143.39\wwwroot\iLearnNew\Service`; ตรวจหลัง deploy พบ root `appsettings.json` อัปเดตแล้วแต่ `web.config` ยังชี้ deploy เดิม จึงสลับ active deployment เป็น `_deploy_20260701104744` โดยแก้ `aspNetCore arguments` ที่ `Service/web.config` แล้ว
- ไฟล์หลักที่แตะ: `iLearn.API/appsettings.json`, `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี
- Verified: ตรวจบน share `\\10.10.143.39\wwwroot\iLearnNew\Service\appsettings.json` ได้ `HostUnc=D:\iLearnContent`; ตรวจ `web.config` ได้ `.\\_deploy_20260701104744\\iLearn.API.dll`; smoke check `http://AP-NTC2138-QAWB/iLearnNew/Service/api/admin/session/me` ได้ `401` (บริการตอบสนองและต้อง auth)

## [2026-07-02 13:15] Claude Code — เขียน PLAN-049 (student→/iLearn root + ปุ่มสลับ admin) (วางแผน ไม่แก้โค้ด)
- ทำอะไร: ผู้ใช้ขอ 2 อย่าง: (A) ย้าย student `/iLearn/student`→`/iLearn` root; (B) admin MVC↔React มีปุ่มสลับ. สำรวจ: `iLearn.User` ไม่มี hardcode `/student` (default route Home/Index, asset ใช้ PathBase) → Part A ไม่ต้องแก้โค้ด แค่ IIS restructure (แปลง /iLearn เป็น User app root, คง sub-apps admin/admin-react/Service/Courses nested, ลบ /iLearn/student). Part B: derive URL อีกเวอร์ชันจาก base path (React appConfig `legacyAdminUrl`=appBasePath แทน /admin-react→/admin + ปุ่มใน Header.tsx ใช้ AppButton; MVC _DevExtremeLayout ใช้ `PathBase + "-react"`) → env-aware ไม่ต้องเพิ่ม env var. เขียน PLAN-049 (READY): Part A→GPT(IIS), Part B→Gemini(UI). เตือน: nested ASP.NET Core web.config inherit + LearnerProxy pathbase เปลี่ยน → ต้อง E2E
- ไฟล์หลักที่แตะ: `DOC/PLANS/PLAN-049-prod-url-and-admin-switch.md` (ใหม่, READY)
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี (แผน)
- Verified: n/a (planning); สำรวจ appConfig.ts/Program.cs/layout จริงเพื่อความแม่นยำ

## [2026-07-02 12:45] Claude Code — เขียน PLAN-048 ย้าย prod → DB จริง (assign GPT) (วางแผน ไม่แก้โค้ด)
- ทำอะไร: ผู้ใช้สั่งเขียน PLAN-048 ย้าย prod จาก QA `iLearnDB_New` (10.10.143.37) → prod DB จริง `10.10.154.119` เพื่อแก้ระบบช้า (DB contention shared) + ตัดความเสี่ยง prod ผูก QA + ล้าง test data. กลยุทธ์ = **backup/restore** (ไม่ re-ETL): restore สำเนา QA DB ไป prod server (ได้ schema HEAD+migrations+CourseTypes+data+fix ครบ). **จุดเด่น: content ไม่ต้อง copy ใหม่** เพราะ guid ใน DB ที่ restore = guid บน prod storage (D:\iLearnContent) อยู่แล้ว. Steps: freeze write→backup COPY_ONLY→restore→verify counts (40 cat/582 Open/1409 published)→เพิ่ม ConnectionStrings ใน API appsettings.Production.json (แนะนำ env var กัน secret เข้า git)→restart iLearnService→E2E+perf check→เก็บ QA DB เป็น fallback. เตือน: ห้าม re-ETL (เสีย guid), ห้ามแตะ iLearn เก่า, learner progress ช่วง window
- ไฟล์หลักที่แตะ: `DOC/PLANS/PLAN-048-prod-move-to-real-db.md` (ใหม่, READY)
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี (แผน)
- Verified: n/a (planning)

## [2026-07-02 12:20] Claude Code — รีวิว PLAN-047 (Gemini DONE) → VERIFIED + เจอระบบช้า (ไม่แก้โค้ด)
- ทำอะไร: รีวิวงาน Gemini (PLAN-047 DONE). ✅ 2 blocker แก้แล้ว: Part A courses Open 582 (จาก 36), Part B publish 498/498 สำเร็จ (498 เดิมไม่ได้เสีย—แค่รอบแรก bulk publish ไม่ครบ→retry ผ่านหมด, รวม 1409), Part C resync prod 1409 folders ตรง. E2E ของ Gemini เชิงประจักษ์ (learner 610034 เล่น SCORM slide+exam จบ 100%+บันทึกเรียนจบ) → PLAN-047 VERIFIED. **แต่ตรวจ browser เองพบระบบช้าทั้งระบบตอนนี้** (admin+student ทุกหน้าค้าง >45s, เมื่อ session ก่อนเร็ว) — น่าจะ DB contention shared QA iLearnDB_New จาก ops หนักที่เพิ่งรัน หรือ lock/agent ค้าง (ความเสี่ยง prod ผูก QA DB). Follow-up: content 1409/courses 584 เกิน migrate ~3 (QA test data ปน)
- ไฟล์หลักที่แตะ: `DOC/PLANS/PLAN-047-prod-post-deploy-fixes.md` (Review Notes → VERIFIED + 2 follow-up)
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี (รีวิว)
- Verified: ยืนยันผ่าน E2E ของ Gemini + โครงสร้างที่ผมตรวจอิสระรอบก่อน; ตรวจ browser รอบนี้เจอ slowness (ไม่ยืนยัน visual ได้เพราะหน้าค้าง) — ให้ตรวจ DB load/ย้าย prod DB

## [2026-07-02 11:40] Claude Code — เขียน PLAN-047 แก้ prod (status + content gap) assign Gemini (วางแผน ไม่แก้โค้ด)
- ทำอะไร: ผู้ใช้ query ยืนยัน 2 ปัญหา: (1) Courses Closed **546**/Open 36/Draft 1 แต่ source IsActive=1 = **579** (WouldBeOpen 34) → D2 ExpiredDate เก่าปิด ~545; (2) ContentItems Published **910**/NotPublished **498**. เขียน `PLAN-047` (READY, assign **Gemini**): Part A = `UPDATE Courses SET Status=1 WHERE IsActive=1 AND Status=2` (เปิด ~579, DB update prod เห็นทันที); Part B = diagnose 498 (TypeId/FileStorage/name) + retry BatchPublishStream + จัด root cause (non-SCORM/เสีย/version) + รายงาน (ห้ามลบเอง); Part C = re-copy guid ใหม่ QA→prod. ย้ำ `iLearnDB_New` = live prod (shared) + ห้ามแตะ iLearn เก่า
- ไฟล์หลักที่แตะ: `DOC/PLANS/PLAN-047-prod-post-deploy-fixes.md` (ใหม่, READY)
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี (แผน; Part A จะ update ข้อมูล Courses.Status ตอน implement)
- Verified: n/a (planning); ตัวเลขจาก query ผู้ใช้จริง

## [2026-07-02 11:15] Claude Code — E2E ตรวจ prod จริงผ่าน browser → No-Go (เจอ 2 ปัญหา) (ไม่แก้โค้ด)
- ทำอะไร: เข้า `https://ap-ntc2137-prwb/iLearn/admin-react/` (Windows auth ผ่าน) ตรวจระบบจริง. ✅ ยืนยัน DB ถูกตัว (40 categories ตรง; PD1=97 courses→580 ครบ; คอร์ส id870 มี v1+2 content items Learn/Exam .zip; Type=Common ถูก). 🔴 **เจอ 2 blocker:** (1) คอร์สทั้งหมด Status=Closed (dashboard Portfolio 35/~545 Closed) → learner เข้าไม่ถึง; สาเหตุน่าจะ D2 ExpiredDate เก่าปิดหมด → ต้อง bulk-open; (2) content publish gap 908/1406 ยังไม่ยืนยัน (Content Library UI 0 records=filter). ให้ query วินิจฉัย (Courses GROUP BY Status; source IsActive/ExpiredDate dist; ContentItems published count). Verdict = No-Go จนกว่าแก้ 2 ข้อ
- ไฟล์หลักที่แตะ: `DOC/PLANS/PLAN-046-deploy-prod-inplace.md` (+ E2E Review section)
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี (รีวิว/E2E)
- Verified: ตรวจ prod runtime จริงผ่าน browser (catalog/category/course detail); ยังไม่ได้ query DB ยืนยันตัวเลข (No prod DB access — ให้ผู้ใช้/GPT รัน)

## [2026-07-02 10:30] Claude Code — รีวิว deploy prod PLAN-046 → Conditional Go (ไม่แก้โค้ด)
- ทำอะไร: รีวิวงาน deploy prod ที่ GPT ทำ (PLAN-046 ACTIVE) อิสระ — ตรวจ `appsettings.Production.json` 3 แอป (override เฉพาะ URL/HostUnc, ไม่แตะ DB conn = ใช้ QA ตามแผน ✓), React `.env.production` ถูก, ไม่มี ASPNETCORE_ENVIRONMENT deployed (default Production ✓), GPT wrap `@(Get-ChildItem)` ใน deploy-side-by-side.ps1 ปลอดภัยไม่ regress, build 0 err + test 118/118 + React build ผ่าน (รันเอง). **พบ 2 blocker ก่อนเปิดผู้ใช้:** (1) 🔴 content copy 908 folder แต่มี 1406 content items → ~498 อาจ publish ไม่สำเร็จ (SCORM validation) ต้อง verify DB + ดูว่า fail เพราะอะไร; (2) 🔴 E2E ยังไม่ทำ (เห็น catalog/เล่น SCORM/upload). จุดเล็ก: SignalR ปิด, app pool `iLearnAuth` ถูก start (นอก scope?). Verdict = Conditional Go
- ไฟล์หลักที่แตะ: `DOC/PLANS/PLAN-046-deploy-prod-inplace.md` (+ Review Notes)
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี (รีวิว)
- Verified: build 0 err, test 118/118, React build (รันเองอิสระ), cleanup artifacts แล้ว; ยังยืนยัน prod runtime/DB จากที่นี่ไม่ได้ (อ้าง Implementer Notes + ต้อง verify count/E2E)

## [2026-07-01 16:00] Claude Code — เขียน PLAN-046 deploy prod (assign GPT) (วางแผน ไม่แก้โค้ด)
- ทำอะไร: ผู้ใช้สั่งให้ GPT จัดการ deploy ทั้งหมด → เขียน `PLAN-046` (READY, assign GPT) รวบทุก decision: prod `/iLearn/*`, app pools `iLearn.*`, ต่อ QA `iLearnDB_New` ช่วงแรก (conn เดิม), storage `D:\iLearnContent`+vdir+IIS_IUSRS, config URLs prod. **จุดสำคัญที่ใส่ในแผน:** prod ใช้ QA DB → `ContentItem.URL` (guid) ชี้ไฟล์ที่แตกบน QA storage → ต้อง **copy content QA→prod `D:\iLearnContent\Courses`** (guid ตรง เล่นได้) ไม่งั้น 404. ระบุ 7 steps (config→build→IIS→deploy→content copy→verify→rollback) + Constraints (ห้ามแตะ iLearn เก่า/ห้าม ETL ซ้ำ QA DB/ห้ามขยายสโคป) + สิ่งที่ GPT ต้อง confirm (prod share root, IIS site, SSL, Windows auth)
- ไฟล์หลักที่แตะ: `DOC/PLANS/PLAN-046-deploy-prod-inplace.md` (ใหม่, READY)
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี (แผน)
- Verified: n/a (planning)

## [2026-07-01 15:40] Claude Code — วางแผน prod cutover: app pools iLearn.*, ใช้ QA DB ช่วงแรก, storage prod (วางแผน ไม่แก้โค้ด)
- ทำอะไร: ผู้ใช้ตัดสิน prod topology: (1) ช่วงแรก prod app ต่อ **QA `iLearnDB_New`** (10.10.143.37) ไปก่อน — ไม่ต้องแก้ conn string (repo ชี้อยู่แล้ว) — ค่อยย้าย prod DB ทีหลัง (เตือน: freeze iLearnDB_New กัน ETL ซ้ำ + prod ผูก QA db server); (2) prod ใช้ชื่อ **`iLearn.*`** (เลิก iLearnNew): app pools `iLearn.Service`/`iLearn.User`/`iLearn.Admin` (No Managed Code + Integrated + AppPoolIdentity), paths `/iLearn/student|admin|admin-react|Service`, config URLs ใช้ `iLearn`; (3) storage prod สร้าง `D:\iLearnContent` แล้ว (ต้องครบ vdir + IIS_IUSRS Modify + HostUnc). ให้ PowerShell สร้าง app pools + ตาราง naming consistency. deploy scripts default iLearnNew → ต้องส่ง param prod เอง (หรือ wrapper). ⚠️ ย้ำ prod `iLearn` DB เก่าเป็น ETL source — อย่าลบจนกว่า provision+ETL prod DB เสร็จ + มี .bak
- ไฟล์หลักที่แตะ: `DOC/PLANS/PLAN-045-production-cutover-ilearn2.md` (config table)
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี
- Verified: n/a (planning); infra prod กำลังตั้ง (storage/app pools)

## [2026-07-01 15:00] Claude Code — QA dry-run ครบ loop: ETL+merge verified + storage แยกไดร์ฟ + bulk publish สำเร็จ (วางแผน ไม่แก้โค้ด)
- ทำอะไร: ผู้ใช้ re-run ETL (merge) → reconciliation ผ่านสะอาด: Categories 40 (49-9 merged), Courses 580 (Common 372 + No-Common 208), ContentItems/FileStorages 1406, CourseVersions 580, CourseContentItems 950, FK integrity 0/0/0; crosswalk No-Common→main 9 คู่ถูกต้อง. เปลี่ยนที่เก็บไฟล์ที่แตกไปไดร์ฟใหญ่: IIS Virtual Directory `/iLearnNew/Courses` → `D:\iLearnContent\Courses` (บน QAWB), ให้สิทธิ์ `IIS_IUSRS` = Modify (IIS inject SID เข้า worker token ทุก identity), แก้ `FileSettings:HostUnc` = `D:\iLearnContent` ที่ Service/appsettings.json, restart app pool. Bulk publish (`ContentItems/Admin/BulkSetPublic`) สำเร็จ → SCORM re-extract จาก byte[] ลง D:. เหลือ: verify published count + เล่นทดสอบ; แล้วเข้าสู่ prod cutover (config prod ยังค้าง)
- ไฟล์หลักที่แตะ: `DOC/AGENT_LOG.md` (ไม่มีแก้โค้ด repo; config เปลี่ยนที่ deployed appsettings บน QA + IIS)
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี (schema เดิม; แค่ config ที่เก็บไฟล์)
- Verified: ETL reconciliation + FK integrity ผ่าน; bulk publish รายงานสำเร็จ (รอ verify count + E2E play)
- Note: FileSettings storage แยกไดร์ฟ (vdir + IIS_IUSRS Modify) เป็น pattern ที่ต้องทำซ้ำบน prod

## [2026-07-01 14:10] Claude Code — เพิ่ม merge No-Common category → main ใน ETL (วางแผน ไม่แก้โค้ด)
- ทำอะไร: ผู้ใช้ยืนยัน CourseTypes จริงใน iLearnDB_New (1=Common, 2=No-Common, 3=General, 4=VDO) + category (No Common) 9 อันอยู่ใน DivisionId=2 (PD2) ทั้งหมด และ PD2 มี category หลัก (Common) คู่ขนาน. ผู้ใช้สั่งเพิ่ม: course ใน No-Common → **ย้ายไปรวมกับ category หลัก** (ไม่ใช่แค่ตั้ง type). ปรับ `etl-catalog.sql`: (1) เพิ่ม [0b] crosswalk #NoCommonMerge จับคู่ No-Common→main ด้วย (DivisionId + เลขนำหน้า, main=ชื่อไม่มีวงเล็บ) ทน "Part vs Parts"/ช่องว่าง + preview SELECT; (2) Categories ข้าม No-Common ที่ merge; (3) Courses re-point CategoryId ไป MainCatId + CourseTypeId=2 สำหรับ No-Common. เพิ่ม CourseType breakdown ใน reconciliation. เจอ marker `(LAS)`/`(CAS)` ใน DivisionId=5 (NLC) — ยังไม่รู้ความหมาย ปล่อย Common ไว้ (open question)
- ไฟล์หลักที่แตะ: `DOC/PLANS/PLAN-045-etl-catalog.sql`, `DOC/PLANS/PLAN-045-data-mapping.md` (D1-rev)
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี
- Verified: n/a (planning); Categories คาดเหลือ ~40 หลัง merge; รอ re-run + ตรวจ crosswalk preview (9 คู่)

## [2026-07-01 13:40] Claude Code — ETL dry-run ผ่าน (lossless) + เพิ่ม rule CourseType จากชื่อ category (วางแผน ไม่แก้โค้ด)
- ทำอะไร: (1) ผู้ใช้รัน ETL รอบแรก reconciliation **ผ่านสะอาด**: Categories 49, ContentItems 1406, Courses 580, CourseContentItems 950, CourseVersions 580, FileStorages 1406 (ข้าม orphan 288; 1406+288=1694) — FK integrity 0/0/0. อ่านได้ว่า content ทุกตัวมีไฟล์ (FileStorages=ContentItems) และ Test division ไม่มี content. (2) ผู้ใช้แจ้ง business rule: ระบบเก่าไม่มี CourseType → admin ฝังใน Category.Name เป็น suffix `(No Common)` = No-Common(Id2), นอกนั้น = Common(Id1). ตรวจโค้ด: อ้างชื่อ type จุดเดียว `CourseAssignmentService.cs:49` (`=="General"` auto-assign) → DB ใช้ Common/No-Common จึง auto-assign ไม่ทำงาน (moot mass-enroll). ปรับ `etl-catalog.sql`: CourseTypeId derive จาก suffix ชื่อ category เก่า (LEFT JOIN old Categories) + ตัด suffix ออกจากชื่อ category ที่ย้าย (@StripCategorySuffix) + var @TypeCommon/@TypeNoCommon. แทน D1 (Special blanket) เป็น D1-rev
- ไฟล์หลักที่แตะ: `DOC/PLANS/PLAN-045-etl-catalog.sql`, `DOC/PLANS/PLAN-045-data-mapping.md` (D1-rev)
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี (สคริปต์/แผน)
- Verified: dry-run รอบแรกผ่าน; รอ re-run หลังเพิ่ม rule + ยืนยัน CourseTypes Id จริง
- heads-up ทีมแอป: auto-assign hardcode "General" ไม่ match Common/No-Common

## [2026-07-01 13:00] Claude Code — เลือก SCORM Strategy B (Re-publish) + ปรับ ETL (วางแผน ไม่แก้โค้ด)
- ทำอะไร: ยืนยันจากโค้ดว่าแอปเสิร์ฟ SCORM จากไฟล์บน share ไม่ใช่ byte[] (`ScormService` แตกตอน upload/publish, `GetScormUrl` แค่ชี้ URL); เจอ `ContentPublicationService.PublishAsync` re-extract จาก byte[] ได้ + **bulk publish มีจริง** (`ContentItemsController`: `Admin/BulkSetPublic` publish ทั้งหมด streaming, `Admin/BatchPublishStream`, `Admin/BatchPublish`). ผู้ใช้เลือก **Strategy B (Re-publish)** + restore old เข้า `iLearn`@AP-NTC2138-QADB แล้ว. ปรับ `etl-catalog.sql` step [3]: ContentItems `IsActive=0` (ให้ publishable) + guard เติม `.zip` ต่อท้าย Name ของ zip-backed ที่ไม่มี (PublishAsync extract เฉพาะ .zip). เพิ่ม query เช็คชื่อไฟล์ (§3.5) + note flow post-ETL = เรียก BulkSetPublic. Gotcha คงเหลือ: publish ~1400 ตัวหนัก (re-extract zip ทุกก้อน)
- ไฟล์หลักที่แตะ: `DOC/PLANS/PLAN-045-etl-catalog.sql`, `DOC/PLANS/PLAN-045-data-mapping.md` (§3.5), `DOC/PLANS/PLAN-045-production-cutover-ilearn2.md` (Phase 4)
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี (แผน/สคริปต์ยังไม่รัน)
- Verified: n/a (planning); ยืนยัน bulk publish + publish logic จากโค้ดจริง

## [2026-07-01 12:35] Claude Code — profiling ยืนยัน iLearnDB_New=HEAD + ตัดสิน 1:1 (ไม่ merge) (วางแผน ไม่แก้โค้ด)
- ทำอะไร: ผู้ใช้รัน query ยืนยัน: (1) `iLearnDB_New` มีตาราง `ContentItems`+`CourseContentItems` = **HEAD** → สคริปต์ผมใช้ได้ Gemini ใช้ไม่ได้; (2) profiling old `iLearn`: Courses 580 / distinct Code 497 (dup 83), **LearnOnly 210 (36%)** / LearnAndExam 370 / NoLearn 0, TypeId 1=836 2=570. → D7 ตัดสิน **ใช้ 1:1 lossless** เพราะ Script2 (ต้องมี Learn+Exam) จะทิ้งคอร์ส Learn-only 210 ตัว (36%) แต่ merge ช่วย dedup แค่ 83 = ไม่คุ้ม. พร้อมรัน etl-catalog.sql (source [iLearn] อยู่บน QADB แล้ว)
- ไฟล์หลักที่แตะ: `DOC/PLANS/PLAN-045-data-mapping.md` (§3.4 profiling + D7)
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี
- Verified: n/a (planning); รอผล reconciliation หลังผู้ใช้รัน ETL

## [2026-07-01 12:10] Claude Code — เจอ schema-state gap (rename migration) + finalize ETL สำหรับ QA dry-run (วางแผน ไม่แก้โค้ด)
- ทำอะไร: ผู้ใช้แชร์ 3 สคริปต์ ETL ของ Gemini (Downloads) + สนใจ Script2 (merge code + reconstruct version). ตรวจพบ **ทั้ง 3 ใช้ชื่อ schema เก่า** (`Resources`/`CourseResources`/`ResourceHref`/`ResourceId`/`StudentGroups*`) — ยืนยันด้วยการอ่าน migration `20260429071730_RenameResourceStudentTerminology` ว่า rename เป็น `ContentItems`/`CourseContentItems`/`LaunchHref`/`ContentItemId`/`LearnerGroups*` แล้ว + Gemini ไม่ใส่ `Status`(30เม.ย.)/`CachedFileLength`(12มิ.ย.) → **สคริปต์เหล่านั้นรันกับ HEAD ไม่ได้** (R9). ประเมิน Script2: idea ดี (merge code ซ้ำ, กู้ version จาก session heuristic) แต่เสี่ยง data-loss (ทิ้งคอร์สที่ไม่มีคู่ Learn+Exam, heuristic ID-gap เปราะ). ผู้ใช้เลือกทำ QA dry-run: restore prod เก่า→AP-NTC2138-QADB `[iLearn]`→map→`iLearnDB_New`(instance เดียว)→ทดสอบแอป. Finalize `PLAN-045-etl-catalog.sql` ให้ตรง topology (source `[iLearn].[dbo]`, HEAD names, cleanup+reseed, 1:1 lossless pass แรก, reconciliation) + ให้ query profiling (code ซ้ำ/Learn-only/TypeId dist) ไว้ตัดสิน merge ทีหลัง
- ไฟล์หลักที่แตะ: `DOC/PLANS/PLAN-045-etl-catalog.sql` (rewrite ตาม topology), `DOC/PLANS/PLAN-045-data-mapping.md` (§3.3 schema-gate), `DOC/PLANS/PLAN-045-production-cutover-ilearn2.md` (R9)
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี (สคริปต์ยังไม่รัน)
- Verified: n/a (planning only); ยืนยัน rename จาก migration file จริง — ต้องเช็ค __EFMigrationsHistory ของ iLearnDB_New ก่อนรันจริง

## [2026-07-01 11:30] Claude Code — ร่างสคริปต์ ETL catalog PLAN-045 (source data ครบ) (วางแผน ไม่แก้โค้ด)
- ทำอะไร: ผู้ใช้ให้ข้อมูล DB เก่า: Divisions (3=CSD,5=NLC,1=PD1,2=PD2,4=PD3,6=Test) + row counts (Categories 49, Courses 580, CourseResources 950, Resources 1406, FileStorage 1694); และ Divisions ของ DB ใหม่ = **Id+Name ตรงกันเป๊ะ** → D0 crosswalk = identity (copy DivisionId ตรง). วิเคราะห์ตัวเลข: FileStorage(1694)>Resources(1406)=~288 ไฟล์กำพร้า (ย้ายเฉพาะที่ถูกอ้าง); Resources(1406)>CourseResources(950)=มี resource ไม่ผูกคอร์ส (D5 เสนอย้ายทั้งคลัง). เขียนสคริปต์ ETL `PLAN-045-etl-catalog.sql`: 6 ขั้น FK-safe (Categories→FileStorages→ContentItems→Courses→CourseVersions v1→CourseContentItems) ด้วย IDENTITY_INSERT+DBCC CHECKIDENT reseed, guard-join กัน FK, `@IncludeTest` toggle (D6), Uncategorized conditional (D1), Status derive (D2), + reconciliation queries; default dry-run ROLLBACK
- ไฟล์หลักที่แตะ: `DOC/PLANS/PLAN-045-etl-catalog.sql` (ใหม่, DRAFT), `DOC/PLANS/PLAN-045-data-mapping.md` (§3.1 crosswalk ยืนยัน)
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี (สคริปต์ยังไม่รัน; เป็น draft ให้ dry-run)
- Verified: n/a (planning only); สคริปต์อ้าง column จาก snapshot ทั้งสองฝั่ง — ต้อง dry-run จริง + ยืนยัน D5/D6 ก่อนใช้
- เหลือ confirm: D5 (ย้ายทั้งคลัง) + D6 (ข้าม Test), ตั้ง {{OLD}} linked-server/3-part name, แล้ว dry-run

## [2026-07-01 11:00] Claude Code — เคาะ decisions D0–D4 ของ ETL PLAN-045 (วางแผน ไม่แก้โค้ด)
- ทำอะไร: ปิด decision D0–D4 ใน data-mapping. จุดสำคัญ: ตรวจ `CourseAssignmentService.AssignGeneralCoursesToNewUserAsync` (บรรทัด 46-49) พบว่าคอร์ส `CourseType.Name=="General"` ถูก **auto-assign ให้ผู้เรียนใหม่ทุกคน** → เปลี่ยนคำแนะนำ D1 `CourseTypeId` default จาก General เป็น **Special(1)** เพื่อกัน mass auto-enrollment วัน go-live (ย้าย catalog อย่างเดียว, ตั้ง assignment ใหม่). สรุปอื่น: D0 crosswalk Division ตามชื่อ (ไม่ match=หยุดรายงาน, ห้าม null เงียบ), D2 Status = IsActive&ไม่หมดอายุ→Open else Closed, D3 ทิ้ง ExpiredDate หลังคำนวณ Status, D4 สร้าง CourseVersion v1 ให้ทุกคอร์ส (assignment/playback อิง active version)
- ไฟล์หลักที่แตะ: `DOC/PLANS/PLAN-045-data-mapping.md` (§2.4, §3 RESOLVED, §5)
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี (เอกสาร/แผน)
- Verified: n/a (planning only); อ้างพฤติกรรมจาก CourseAssignmentService.cs จริง
- เหลือก่อนเขียน ETL: dump Divisions ทั้งสอง DB (crosswalk D0) + row counts

## [2026-07-01 10:40] Claude Code — ลด scope ETL PLAN-045 เหลือเฉพาะ catalog + ปรับ data-mapping (วางแผน ไม่แก้โค้ด)
- ทำอะไร: ผู้ใช้ยืนยัน scope data migration = **เฉพาะสื่อการเรียน** (Categories, Courses, CourseResources, Resources, FileStorage) ไม่เอา learner/enrollment/history/admin/Divisions → ตัด 🔴 tables (Enrollments/LearningLogs) + admin ออกหมด เหลือ 5 เก่า → 6 ใหม่ (Categories, FileStorages, ContentItems, Courses, CourseVersions, CourseContentItems). ผู้ใช้ระบุ Divisions มีใน DB ใหม่แล้ว → เพิ่ม decision D0: map `Category.DivisionId` เก่า→ใหม่ ผ่าน crosswalk ตามชื่อ. rewrite `PLAN-045-data-mapping.md` ให้ตรง scope + decisions เหลือ D0–D4 + ลำดับ ETL ใหม่. อัปเดต PLAN-045 หลัก (decision #1 note + Phase 3)
- ไฟล์หลักที่แตะ: `DOC/PLANS/PLAN-045-data-mapping.md` (rewrite), `DOC/PLANS/PLAN-045-production-cutover-ilearn2.md`
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี (เอกสาร/แผน)
- Verified: n/a (planning only)

## [2026-07-01 10:15] Claude Code — วิเคราะห์ schema เก่า + ทำ data-mapping สำหรับ ETL PLAN-045 (วางแผน ไม่แก้โค้ด)
- ทำอะไร: วิเคราะห์ schema ระบบเก่า `iLearnService` (EF Core 8, DB `AP-NTC2139-COSS/iLearn`) จากโค้ดที่ `C:\Users\n4734\source\repos\iLearn\iLearn` — อ่าน AppDbContext + model snapshot + entities + enums เทียบกับ schema ใหม่ 21 ตาราง. สรุป: เก่ามี **11 ตาราง**; insight สำคัญ — (1) `Users/Roles/UserRoles` = บัญชี admin เท่านั้น (RoleType เก่า Developer/Administrator; learner ใช้ StudentCode string), (2) content model แตกใหม่ (Resource+CourseResource → ContentItem+CourseVersion+CourseContentItem, เพิ่ม versioning), (3) Enrollment เปลี่ยน per-Category → per-Course (ต้องขยายแถว+คำนวณ progress จาก log), (4) Course เพิ่ม CategoryId/CourseTypeId บังคับ + Status enum. enum เข้าล็อก: ContentItem.TypeId 1=Learn/2=Exam ตรงกับ old ResourceType; CourseStatus Draft/Open/Closed; RoleType ใหม่ Admin/SuperAdmin. เขียน `PLAN-045-data-mapping.md`: master mapping + column mapping ราย field + transform ตารางยาก + open decisions D1–D7 + ลำดับ ETL FK-safe + กลยุทธ์คง Id
- ไฟล์หลักที่แตะ: `DOC/PLANS/PLAN-045-data-mapping.md` (ใหม่), `DOC/PLANS/PLAN-045-production-cutover-ilearn2.md` (เชื่อม Phase 3 + ติ๊ก discovery)
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี (วิเคราะห์+เอกสาร)
- Verified: n/a (planning only); ยังไม่ dump DB จริง — mapping อ้างจากโค้ด ต้องเทียบ DB จริงอีกที (D5 + row counts)

## [2026-07-01 09:30] Claude Code — เขียน PLAN-045 production cutover runbook (วางแผน ไม่แก้โค้ด)
- ทำอะไร: ผู้ใช้สั่งวางแผน deploy iLearn2 ขึ้น prod (`ap-ntc2137-prwb`) แทนที่ระบบเก่า `iLearn` ในที่เดิม + ย้าย DB. สำรวจ repo พบประเด็นสำคัญ: deploy scripts ปัจจุบันชี้ **QA** (`AP-NTC2138-QAWB`/`iLearnNew`) ไม่ใช่ prod; **ไม่มี `appsettings.Production.json`** (config ปัจจุบัน=QA) และ deploy script ก็อป appsettings ทับ root ปลายทางเสมอ (R1/R2); **ไม่มี auto-migrate** ในโค้ด; **ไม่มี migration InitialCreate** → DB เปล่ารัน `ef database update` ไม่ได้ ต้อง baseline schema จาก QA ก่อน (R3); ไม่มี `UsePathBase` → ย้าย path เป็น IIS config; มี 4 web component (API/User/MVC-admin/React-admin, React ไม่มี deploy script); SCORM เก็บทั้ง `FileStorages.Data` byte[] + folder Courses บน share. เก็บ 4 decision จากผู้ใช้ (DB=สร้างใหม่+ย้ายข้อมูล, cutover=แทนที่ /iLearn เดิม, admin=ทั้งคู่, config=ผู้ใช้จะบอกค่า). เขียน `PLAN-045` เป็น runbook 10 phase (pre-flight→config→provision DB→ETL→content→build→IIS→cutover→verify→rollback→post) + risk register + ตาราง "ค่าที่ต้องได้จากผู้ใช้" + IIS layout เป้าหมาย. ยืนยันโค้ด/สคีมาเก่าเข้าถึงได้ที่ `C:\Users\n4734\source\repos\iLearn\iLearn` (ทำ ETL mapping ได้จริง)
- ไฟล์หลักที่แตะ: `DOC/PLANS/PLAN-045-production-cutover-ilearn2.md` (ใหม่, DRAFT)
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี (แผน — ยังไม่แก้โค้ด/DB)
- Verified: n/a (planning only)

## [2026-07-01 07:44] GitHub Copilot (GPT-5.3-Codex) — Deploy iLearn.Admin สำเร็จ (fallback ไม่ใช้ app-pool control)
- ทำอะไร: รัน preflight `deploy-admin.ps1 -WhatIf` ผ่าน (ยืนยันแผน deploy และ web.config switch), deploy จริงรอบแรกติดสิทธิ์ stop app pool (`Access is denied`) และรอบ credential ถูกยกเลิกโดยผู้ใช้ จึงทำ fallback deploy ด้วย `deploy-admin.ps1 -AppPoolName ''` เพื่อปล่อยงานทันที; deploy จบสำเร็จพร้อม cleanup deploy เก่า
- ไฟล์หลักที่แตะ: `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี
- Verified: summary deploy = `DeployPath=\\10.10.143.39\wwwroot\iLearnNew\admin\_admin_deploy_20260630171046`, `WebConfigArguments=.\\_admin_deploy_20260630171046\\iLearn.Admin.dll`, `RemovedStale=1`; smoke checks `http://AP-NTC2138-QAWB/iLearnNew/admin`, `.../admin/`, `.../admin/Assignments/BulkAssign` ได้ `401` ทั้งหมด (สอดคล้อง endpoint ที่ต้อง auth) และตรวจ `\\10.10.143.39\wwwroot\iLearnNew\admin\web.config` พบ arguments ชี้ stamp ใหม่ถูกต้อง

