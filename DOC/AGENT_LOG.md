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

## [2026-06-16 09:09] GitHub Copilot (GPT-5.3-Codex) — Standardize Overview title and make Fact <dd> plain text
- ทำอะไร: ปรับหัวกล่อง section หลักให้ใช้ชื่อ `Overview` เหมือนกันทุกหน้า detail ที่เคยใช้ชื่อเฉพาะ (`Course/User/Content/Version Overview`) และปรับ shared detail primitive `Fact` ให้ `<dd>` เป็นข้อความปกติแบบไม่ผูก CSS class แล้ว
- ไฟล์หลักที่แตะ: `iLearn.Admin.React/src/components/ui/detail/index.tsx`, `iLearn.Admin.React/src/pages/courses/CourseDetailPage.tsx`, `iLearn.Admin.React/src/pages/content-library/ContentItemDetailPage.tsx`, `iLearn.Admin.React/src/pages/courses/VersionDetailPage.tsx`, `iLearn.Admin.React/src/pages/users/UserDetailPage.tsx`, `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี
- Verified: `npm run lint` ผ่าน, `npm run build` ผ่าน, grep ยืนยันไม่เหลือข้อความ `Course/Content/Version/User Overview`

## [2026-06-16 08:56] GitHub Copilot (GPT-5.3-Codex) — Refactor ControlsSidebar top-right alignment and remove Controls header
- ทำอะไร: ปรับคอมโพเนนต์กลาง `ControlsSidebar` ให้ใช้งานแบบชิดขวาบนเหมือนกันทุกหน้าที่ใช้ sidebar (`lg:sticky lg:top-0`) และเอา header ที่มีคำว่า `Controls` ออกทั้งหมดจากกล่อง controls; เก็บจุดเรียกใช้ที่ส่ง prop เดิม `stickyTopClass` ให้ตรงกับ API ใหม่
- ไฟล์หลักที่แตะ: `iLearn.Admin.React/src/components/ui/ControlsSidebar.tsx`, `iLearn.Admin.React/src/pages/assignments/AssignmentDetailPage.tsx`, `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี API/DB change; UI component props เปลี่ยนโดยถอด `title/icon/stickyTopClass` และใช้ `className?` แทน
- Verified: `npm run lint` ผ่าน, `npm run build` ผ่าน

## [2026-06-16 08:46] GitHub Copilot (GPT-5.3-Codex) — Implement PLAN-034 collapse course status to 3 states
- ทำอะไร: ทำตาม Scope ของ PLAN-034 โดยยุบ `CourseStatus` เหลือ `Draft/Open/Closed` (ลบ `Retired=3`), ลบ reference `CourseStatus.Retired` ใน backend ทั้งหมด (`CourseService`, `CourseVersionService`, `CoursesCRUDController`, `Course` entity), ปรับ test lifecycle ที่อ้าง Retired เป็น Closed, และ sync frontend ให้สถานะ `2` แปลว่า Closed จริง (`CourseDetailPage` เปลี่ยน `isRetired` เป็น `isClosed` + ปุ่ม `Close Course`; `CourseStatusBadge` เปลี่ยน tone ของ status 2 เป็น neutral)
- ไฟล์หลักที่แตะ: `iLearn.Domain/Enums/CourseStatus.cs`, `iLearn.Domain/Entities/Course.cs`, `iLearn.Application/Services/CourseService.cs`, `iLearn.Application/Services/CourseVersionService.cs`, `iLearn.API/Controllers/Base/CoursesCRUDController.cs`, `iLearn.Tests/LifecycleContractDtoTests.cs`, `iLearn.Admin.React/src/pages/courses/CourseDetailPage.tsx`, `iLearn.Admin.React/src/components/ui/CourseStatusBadge.tsx`, `DOC/PLANS/PLAN-034-course-status-collapse-3-states.md`, `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มีการเปลี่ยน shape API; เปลี่ยน semantic status ฝั่งโค้ดให้เหลือ 3 สถานะตามแผน (`Retired` ถูกถอดออก)
- Verified: `dotnet build iLearn.Tests -o artifacts/verify-test` ผ่าน, `dotnet test artifacts/verify-test/iLearn.Tests.dll` ผ่าน (Passed 118, Failed 0), `npm run lint` ผ่าน, `npm run build` ผ่าน

## [2026-06-16 08:42] GitHub Copilot (GPT-5.3-Codex) — Implement PLAN-033 remove tabs from detail pages
- ทำอะไร: แปลงหน้า detail 3 หน้าเป็น stacked sections ตาม scope ของ PLAN-033 โดยเอา `DetailTabs` และ state tab ออกทั้งหมดจาก `CourseDetailPage`, `AssignmentDetailPage`, `LearnerGroupDetailPage`; ปรับ `CourseDetailPage` จาก lazy-per-tab เป็น load-on-mount สำหรับ learners/assignments และคง section-level loading state; อัปเดต `DOC/ux_ui_analysis.md` §2.4 ให้ตรงมาตรฐานใหม่ (ไม่ใช้ tab ใน detail pages)
- ไฟล์หลักที่แตะ: `iLearn.Admin.React/src/pages/courses/CourseDetailPage.tsx`, `iLearn.Admin.React/src/pages/assignments/AssignmentDetailPage.tsx`, `iLearn.Admin.React/src/pages/learner-groups/LearnerGroupDetailPage.tsx`, `DOC/ux_ui_analysis.md`, `DOC/PLANS/PLAN-033-detail-pages-remove-tabs.md`, `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี
- Verified: `npm run lint` ผ่าน, `npm run build` ผ่าน

## [2026-06-16 10:30] Claude Code — Audit lifecycle เจาะลึก + เขียน PLAN-034 (ยุบ Course เหลือ 3 สถานะ)
- ทำอะไร: เจาะ 2 risk จาก lifecycle_analysis — (#4 ResetAt) ตรวจทุก query แล้ว **ไม่มีบั๊ก** (learner/player reads filter ResetAt ครบ, admin/dashboard นับทั้งหมด=ตั้งใจ); (#1 Course transition) **เจอบั๊ก: FE↔BE status code ไม่ตรง** — FE ใช้ 2=Retired แต่ BE Closed=2/Retired=3, ปุ่ม "Retire" ส่ง 2(Closed), Retired(3)+guard ตายสนิท, badge tone เพี้ยน; เจาะเพิ่มพบ Closed(2) ใช้จริง (CanLearnerAccess=Open||Closed) ส่วน Retired(3) ตาย — ผู้ใช้เลือกยุบเหลือ 3 สถานะ → เขียน **PLAN-034** (GPT): ตัด Retired(3), เก็บ Closed(2), แก้ FE label/badge ให้ตรง (learner-access เดิมไม่เปลี่ยน)
- ไฟล์หลักที่แตะ: `DOC/lifecycle_analysis.md` (audit), `DOC/PLANS/PLAN-034-*.md` (เอกสารล้วน)
- Contract ที่เปลี่ยน: ไม่มี (PLAN-034 ตอน implement จะลบ enum Retired=3)
- Verified: n/a (planner) — audit อ่านโค้ดจริง

## [2026-06-16 09:40] Claude Code — วิเคราะห์ Lifecycle ทั้งระบบ → DOC/lifecycle_analysis.md
- ทำอะไร: อ่าน enums + Application/Common policies + services เพื่อ map lifecycle ของ entity หลัก: **Course** (Draft→Open→Closed→Retired + guard readiness/enrollment), **Course Version** (active+readiness), **Content Item** (Draft⇄Published + impact preview), **Assignment** (computed status จาก date+progress, DueSoon 7วัน), **Enrollment** (created→progress→completed→reset ด้วย ResetAt เก็บ history), **SCORM runtime** (resolve status: exam ต้อง pass ไม่ใช่แค่ complete) + cross-cutting (AdminActivity log, division isolation, soft delete) + ข้อสังเกต/ความเสี่ยง 5 ข้อ
- ไฟล์หลักที่แตะ: `DOC/lifecycle_analysis.md` (เอกสารใหม่ — ไม่แตะโค้ด)
- Contract ที่เปลี่ยน: ไม่มี
- Verified: n/a (analysis)

## [2026-06-16 09:00] Claude Code — เขียน PLAN-033 (เอา Tab ออกจากหน้า Detail → stack section)
- ทำอะไร: ผู้ใช้เปลี่ยนทิศ — ไม่เอา tab ในหน้า detail (ตรงข้าม PLAN-031) สำรวจพบ 3 หน้ามี tab: CourseDetail (overview/versions/learners/assignments), AssignmentDetail (overview/courses/learners), LearnerGroupDetail (overview/members) — เขียน **PLAN-033** (GPT): แปลง tab→stacked section เรียงหน้าเดียว (ใช้ DetailCard/SectionHeader), เปลี่ยน lazy-per-tab → load-on-mount + section loading state, คง CourseEditor (editor) ที่ยังใช้ DetailTabs, sync ux_ui_analysis §2.4
- ไฟล์หลักที่แตะ: `DOC/PLANS/PLAN-033-*.md` (เอกสารล้วน)
- Contract ที่เปลี่ยน: ไม่มี
- Verified: n/a (planner) — หมายเหตุ: PLAN-031 (DetailTabs) ยัง DONE รอรีวิว; DetailTabs จะเหลือผู้ใช้แค่ CourseEditor หลัง PLAN-033

## [2026-06-16 08:22] GitHub Copilot (GPT-5.3-Codex) — Complete PLAN-031 Optional Scope C (shared Modal shell)
- ทำอะไร: สกัด modal shell กลางเป็น `Modal` component (รองรับ `open`, `onClose`, `title?`, `children`, `size`, และ `as="div"|"form"`) แล้ว migrate modal wrapper ที่ซ้ำให้ใช้ร่วม โดยคงเนื้อหา/ฟอร์ม/พฤติกรรมเดิม: `CourseDetailPage` (Edit Course Properties), `CourseListPage` (Create/Rename Category), และ `ConfirmDialog`
- ไฟล์หลักที่แตะ: `iLearn.Admin.React/src/components/ui/Modal.tsx`, `iLearn.Admin.React/src/pages/courses/CourseDetailPage.tsx`, `iLearn.Admin.React/src/pages/courses/CourseListPage.tsx`, `iLearn.Admin.React/src/components/ui/ConfirmDialog.tsx`, `DOC/PLANS/PLAN-031-detail-pages-shared-primitives.md`, `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี
- Verified: `npm run lint` ผ่าน (`LINT_OK`), `npm run build` ผ่าน, และอัปเดต PLAN-031 ให้ติ๊ก Optional Scope C แล้ว

## [2026-06-15 23:30] Claude Code — เขียน API style guide + PLAN-032 (refactor AssignmentsController)
- ทำอะไร: (1) เขียน `DOC/api_style_guide.md` — มาตรฐานกลาง: routing, auth, **response envelope (ApiResponse<T> ห้าม anonymous)**, error (ProblemDetails/throw), DTO (+ คำเตือน EF GroupBy DTO ctor), controller thinness, division isolation — อิง pattern จริง; (2) เขียน **PLAN-032** (GPT): refactor `AssignmentsController` (1316 บรรทัด, inject raw repo 4 ตัว, anonymous 9 จุด) → ดึง logic ลง AssignmentService + แปลง anonymous→DTO ตาม style guide, pure refactor contract-preserving (ถ้าใหญ่เกินทำ B ก่อน)
- ไฟล์หลักที่แตะ: `DOC/api_style_guide.md`, `DOC/PLANS/PLAN-032-*.md` (เอกสารล้วน)
- Contract ที่เปลี่ยน: ไม่มี
- Verified: n/a (planner)

## [2026-06-16 08:15] GitHub Copilot (GPT-5.3-Codex) — Implement PLAN-031 (DetailTabs + CourseStatus primitive, pure refactor)
- ทำอะไร: ทำตาม `PLAN-031` scope A+B แบบ pure refactor โดยเพิ่ม primitive ใหม่ `DetailTabs` แล้ว migrate หน้าแท็บ 4 ไฟล์ (`AssignmentDetailPage`, `CourseDetailPage`, `LearnerGroupDetailPage`, `CourseEditorPage`) และเพิ่ม primitive สถานะคอร์ส `CourseStatusBadge`/`CourseStatusText` เพื่อลดการ re-implement class สีใน `CourseListPage` + `CourseDetailPage`; Optional scope C (`<Modal>` shared shell) ยังไม่ทำและบันทึกไว้ใน Implementer Notes ของแผน
- ไฟล์หลักที่แตะ: `iLearn.Admin.React/src/components/ui/DetailTabs.tsx`, `iLearn.Admin.React/src/components/ui/CourseStatusBadge.tsx`, `iLearn.Admin.React/src/pages/assignments/AssignmentDetailPage.tsx`, `iLearn.Admin.React/src/pages/courses/CourseDetailPage.tsx`, `iLearn.Admin.React/src/pages/courses/CourseEditorPage.tsx`, `iLearn.Admin.React/src/pages/courses/CourseListPage.tsx`, `iLearn.Admin.React/src/pages/learner-groups/LearnerGroupDetailPage.tsx`, `DOC/PLANS/PLAN-031-detail-pages-shared-primitives.md`, `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี
- Verified: `npm run lint` ผ่าน, `npm run build` ผ่าน, grep ยืนยัน pattern แท็บ class ซ้ำและ inline status tone logic ที่ migrate ถูกย้ายไป primitive แล้ว

## [2026-06-15 22:50] Claude Code — สำรวจหน้า Detail + เขียน PLAN-031 (สกัด primitive ร่วม ลด class ซ้ำ)
- ทำอะไร: สำรวจหน้า detail ทั้ง 8 (Course/Version/ContentItem/LearnerGroup/User/Assignment/MasterData/LearnerProfile) — ทุกหน้าใช้ shared layout (DetailLayout/Fact*) แล้ว และแทบไม่มี `style={{}}` (1 จุด) มาตรฐานโครงหน้าเสร็จ; ที่เหลือซ้ำคือ class string Tailwind: **A. Tab buttons** (4 ไฟล์), **B. course status badge logic** (CourseDetail/CourseList re-implement สีเอง ทั้งที่มี StatusBadge), **C. centered modal shell** (3 ไฟล์) → เขียน **PLAN-031** (GPT): สกัด `<DetailTabs>` + course status badge ร่วม + (optional) `<Modal>` — pure refactor หน้าตา/พฤติกรรมเหมือนเดิม
- ไฟล์หลักที่แตะ: `DOC/PLANS/PLAN-031-*.md` (เอกสารล้วน)
- Contract ที่เปลี่ยน: ไม่มี
- Verified: n/a (planner)

## [2026-06-15 22:10] Claude Code — PLAN-030: สแกน + ลบ dead code ทั้งโปรเจกต์ (ทำเอง+verify)
- ทำอะไร: ผู้ใช้สั่งให้ทำเอง — สแกน dead component/CSS token/lib utility ทั้ง React project พบ dead ที่ลบได้ปลอดภัยมีแค่ **`PageHeader.tsx`** (13 บรรทัด, 0 refs, ไม่มี barrel) → ลบทิ้ง; **ไม่แตะ:** `--admin-*` CSS `@theme` tokens (utility=0 แต่เป็น design palette — design decision ไม่ใช่ dead ชัด), `useSyncExternalStoreWithSelectorShim` (CLAUDE.md ห้ามลบ ใช้ผ่าน vite alias recharts)
- ไฟล์หลักที่แตะ: ลบ `iLearn.Admin.React/src/components/ui/PageHeader.tsx`, `DOC/PLANS/PLAN-030-*.md`
- Contract ที่เปลี่ยน: ไม่มี
- Verified: grep PageHeader=0, `npm run build` ผ่าน, `npm run lint` 0/0 — PLAN-030 = VERIFIED

## [2026-06-15 21:40] Claude Code — Review + ปิด PLAN-028/029 เป็น VERIFIED (+ แก้ doc ค้าง)
- ทำอะไร: รีวิว 2 แผน — **028** (AppTable flicker): overlay condition = `loading && data.length === 0` ถูกต้อง (initial มี spinner, scroll/refetch ไม่แฟลช); **029** (dead CSS): 8 รายการลบหมด (=0) keep ครบ — **เจอ doc sync ไม่ครบ:** PLAN-029 แก้แค่ ux_ui_analysis §2.3 แต่ §2.1 (`.neon-glow-dot`) และ §2.5 (`.selected-floating-badge`/`badge-pulse`) ยังอ้าง class ที่ลบ → ผมแก้ doc ให้ครบเอง ปรับทั้ง 2 แผนเป็น VERIFIED
- ไฟล์หลักที่แตะ: `DOC/ux_ui_analysis.md` (แก้ §2.1/§2.5), `DOC/PLANS/PLAN-028/029-*.md` (สถานะ)
- Contract ที่เปลี่ยน: ไม่มี
- Verified: `npm run build` ผ่าน, `npm run lint` 0/0
- ค้าง: working tree มี Breadcrumbs.tsx + VersionDetailPage.tsx modified (WIP นอกแผน 028/029 — ยังไม่รีวิว) + งาน PLAN-030 scan (dead component: PageHeader.tsx 0 refs) ที่ผู้ใช้ขอ ยังไม่ได้เขียน

## [2026-06-16 07:55] GitHub Copilot (GPT-5.3-Codex) — Remove Current Content edit button and add Open SCORM Player action
- ทำอะไร: ปรับ section `Current Content` ใน `VersionDetailPage` ให้เอาปุ่ม `Edit Content` ออกจากหัว section และเพิ่มคอลัมน์ `Player` พร้อมปุ่ม `Open SCORM Player` รายแถว (เปิดลิงก์ launch ใหม่ในแท็บใหม่); ถ้าไม่มี launch URL จะแสดง `Unavailable`
- ไฟล์หลักที่แตะ: `iLearn.Admin.React/src/pages/courses/VersionDetailPage.tsx`, `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี
- Verified: `npm run lint` ผ่าน, `npm run build` ผ่าน, browser check ที่ `/courses/848/version/541` ยืนยันไม่มีปุ่ม `Edit Content` ใน `Current Content` และมีปุ่ม `Open SCORM Player` ต่อรายการ

## [2026-06-16 07:52] GitHub Copilot (GPT-5.3-Codex) — Show current content list on Version Detail
- ทำอะไร: เพิ่ม section ใหม่ `Current Content` ในหน้า `VersionDetailPage` เพื่อแสดงรายการ content ที่ผูกกับเวอร์ชันปัจจุบันแบบ read-only (Order / Content Name / Type / Status) และเพิ่มปุ่ม `Edit Content` ใน section นี้เพื่อเปิด popup จัดการ content ได้ทันที
- ไฟล์หลักที่แตะ: `iLearn.Admin.React/src/pages/courses/VersionDetailPage.tsx`, `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี
- Verified: `npm run lint` ผ่าน, `npm run build` ผ่าน, browser check ที่ `/courses/848/version/541` เห็น section `Current Content` พร้อมรายการ content ปัจจุบัน

## [2026-06-15 17:17] GitHub Copilot (GPT-5.3-Codex) — Make Version Detail Edit Content match Version/New CONTENT flow (with upload)
- ทำอะไร: ปรับ popup `Edit Content` ใน `VersionDetailPage` ให้ใช้รูปแบบเดียวกับหน้า `/courses/:courseId/version/new` แท็บ `Content` โดยเพิ่ม action cards `Upload New SCORM` + `Select Existing Content`, ตารางรายการ content แบบมี `Source`/`Content Type`/`Status`, และรองรับไฟล์ใหม่ในลำดับเดียวกันกับรายการเดิม
- ไฟล์หลักที่แตะ: `iLearn.Admin.React/src/pages/courses/VersionDetailPage.tsx`, `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี (ใช้ contract เดิม `PUT Courses/versions/{id}` ที่รองรับ `Files`, `ContentItemIds`, `ContentTypeIds`)
- Verified: `npm run lint` ผ่าน, `npm run build` ผ่าน, browser check ที่ `/courses/848/version/541` ยืนยัน popup `Edit Content` มี `Upload New SCORM` และเปิด `Select Existing Content` ได้

## [2026-06-15 17:10] GitHub Copilot (GPT-5.3-Codex) — Remove "SCORM" wording from Version breadcrumb
- ทำอะไร: ปรับ breadcrumb label ของ segment `version` จาก `SCORM Version` เป็น `Version` ตาม feedback ผู้ใช้ เพื่อให้เส้นทางอ่านง่ายขึ้นและไม่ใช้คำที่ไม่ต้องการ
- ไฟล์หลักที่แตะ: `iLearn.Admin.React/src/components/layout/Breadcrumbs.tsx`, `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี
- Verified: `npm run lint` ผ่าน, `npm run build` ผ่าน

## [2026-06-15 17:03] GitHub Copilot (GPT-5.3-Codex) — Fix broken Version Detail breadcrumb link
- ทำอะไร: ตรวจหน้า `/courses/848/version/541` แล้วพบว่า breadcrumb ชั้น `SCORM Version` ชี้ไป `/courses/:id/version` ซึ่งไม่มี route และพาไป Not Found; แก้ logic ใน `Breadcrumbs.tsx` ให้กรณีเส้นทาง `courses/:courseId/version/...` ลิงก์ชั้น `version` กลับไป `/courses/:courseId` แทน พร้อมแก้ key ของ breadcrumb item ให้ unique (`${to}-${index}`) เพื่อแก้ React warning key ซ้ำ
- ไฟล์หลักที่แตะ: `iLearn.Admin.React/src/components/layout/Breadcrumbs.tsx`, `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี
- Verified: `npm run lint` ผ่าน, `npm run build` ผ่าน, browser check ที่ `/courses/848/version/541` ยืนยัน breadcrumb `SCORM Version` ชี้ไป `/courses/848` (ไม่พาไป `/courses/848/version` แล้ว)

## [2026-06-15 16:59] GitHub Copilot (GPT-5.3-Codex) — Enlarge Version Detail Edit Content popup for high-density data
- ทำอะไร: ปรับ popup `Edit Content` ใน `VersionDetailPage` ให้ใหญ่ขึ้นทั้งกว้างและสูง (เปลี่ยนจาก `modal-window-lg` เดิมที่ถูกจำกัดด้วย `max-width: 780px !important` ไปเป็นขนาดเฉพาะหน้านี้ `width: min(95vw, 1320px)` + `maxHeight: 88vh`) พร้อมเพิ่มพื้นที่แสดงผลภายใน (`lg` side panel กว้างขึ้นและ list/table viewport สูงขึ้น) เพื่อรองรับข้อมูลจำนวนมาก
- ไฟล์หลักที่แตะ: `iLearn.Admin.React/src/pages/courses/VersionDetailPage.tsx`, `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี
- Verified: `npm run lint` ผ่าน, `npm run build` ผ่าน, browser measure ที่ `/courses/829/version/533` ยืนยัน modal กว้างขึ้นจากประมาณ 733px เป็นประมาณ 1109px (viewport 1280px)

## [2026-06-15 16:53] GitHub Copilot (GPT-5.3-Codex) — Refactor Version Detail to 2 popup editors (General + Content)
- ทำอะไร: ปรับ `VersionDetailPage` ให้เหลือเฉพาะ `Version Overview` และย้าย `SCORM Content` + `Content Library` ออกจากหน้า detail ไปอยู่ใน popup ตามปุ่ม Controls 2 ปุ่มคือ `Edit General Info` และ `Edit Content`; โดย `Edit Content` รองรับการจัดลำดับขึ้น/ลง, ลบรายการที่เลือก, และเพิ่มจากรายการ `SCORM Content` พร้อม search แล้วบันทึกผ่าน `PUT Courses/versions/{id}`
- ไฟล์หลักที่แตะ: `iLearn.Admin.React/src/pages/courses/VersionDetailPage.tsx`, `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี
- Verified: `npm run lint` ผ่าน, `npm run build` ผ่าน, browser smoke check ที่ `/courses/829/version/533` ยืนยันว่าไม่มี section `SCORM Content`/`Content Library` บนหน้าหลักแล้ว และ popup `Edit General Info` + `Edit Content` แสดงพร้อมฟังก์ชัน reorder/remove/add

## [2026-06-15 16:39] GitHub Copilot (GPT-5.3-Codex) — Add per-version detail page for SCORM Content & Content Library
- ทำอะไร: ปรับโครงสร้างหน้า `CourseDetailPage` ให้แท็บ `Versions` โฟกัสที่รายการเวอร์ชันและ action เท่านั้น (เพิ่มปุ่ม View ไอคอน Eye ต่อแถว) แล้วเพิ่มหน้าใหม่ `VersionDetailPage` สำหรับแสดงรายละเอียดรายเวอร์ชันโดยตรง ได้แก่ `Version Overview`, ตาราง `SCORM Content`, และ `Content Library` พร้อม search และสถานะว่า content ใดถูก attach อยู่ในเวอร์ชันนั้น; เชื่อม route ใหม่ `/courses/:courseId/version/:versionId` ใน `App.tsx`
- ไฟล์หลักที่แตะ: `iLearn.Admin.React/src/pages/courses/CourseDetailPage.tsx`, `iLearn.Admin.React/src/pages/courses/VersionDetailPage.tsx`, `iLearn.Admin.React/src/App.tsx`, `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี
- Verified: `npm run lint` ผ่าน, `npm run build` ผ่าน, browser smoke check ผ่านที่ `/courses/829/version/533` (หน้าใหม่โหลดข้อมูลจริงและแสดง SCORM Content + Content Library รายเวอร์ชัน)

## [2026-06-15 16:30] GitHub Copilot (GPT-5.3-Codex) — Move SCORM Content & Library under Versions tab on Course Detail
- ทำอะไร: ปรับ `CourseDetailPage` ให้ `SCORM Content` และ `Content Library` ไม่เป็นแท็บระดับบนแล้ว แต่ย้ายไปแสดงเป็น 2 sections ภายในแท็บ `Versions` แทน พร้อมตัดปุ่มแท็บบนออกให้เหลือ `Overview / Versions / Learners / Assignments`; ปรับ trigger โหลดข้อมูล content library ให้โหลดเมื่อเข้าแท็บ `Versions`
- ไฟล์หลักที่แตะ: `iLearn.Admin.React/src/pages/courses/CourseDetailPage.tsx`, `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี
- Verified: `npm run lint` ผ่าน, `npm run build` ผ่าน, browser smoke check ที่ `/courses/829` ยืนยันว่าแท็บบนเหลือ 4 รายการและทั้ง `SCORM Content` + `Content Library` แสดงภายในแท็บ `Versions` ตามที่ต้องการ

## [2026-06-15 16:27] GitHub Copilot (GPT-5.3-Codex) — Improve Course Detail UX (popup edit, split content tabs, icon actions)
- ทำอะไร: ปรับปรุง `CourseDetailPage` ตาม feedback ที่ `/courses/829` โดย (1) เปลี่ยนปุ่ม Control จาก `Bulk Assign` เป็น `Assign Courses` (2) เปลี่ยน `Edit Properties` จากลิงก์หน้าใหม่เป็น popup modal แก้ไขข้อมูลหลักของคอร์ส (Course Type/Code/Title/Division/Category/Description) พร้อมบันทึกผ่าน `PUT Courses/{id}` (3) แยกแท็บใหม่ `SCORM Content` และ `Content Library` พร้อมระบบค้นหา content library และปุ่มไอคอนสำหรับเปิด/เพิ่ม content (4) ปรับ Actions ในตาราง `Versions` เป็นไอคอนทั้งหมด (Set Active / Edit / Delete)
- ไฟล์หลักที่แตะ: `iLearn.Admin.React/src/pages/courses/CourseDetailPage.tsx`, `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี
- Verified: `npm run lint` ผ่าน, `npm run build` ผ่าน, browser smoke check ที่ `/courses/829` ยืนยันแท็บใหม่แสดงผล, popup เปิด/ปิดได้และโหลดข้อมูลเดิมถูกต้อง, Actions column เป็นไอคอน, ปุ่ม `Assign Courses` แสดงผลตามคำใหม่

## [2026-06-15 16:16] GitHub Copilot (GPT-5.3-Codex) — Implement PLAN-030 cleanup dead selectors in Legacy Admin wizard CSS
- ทำอะไร: ทำตาม `PLAN-030-repo-wide-dead-css-cleanup` โดยลบ high-confidence dead selectors 5 รายการจาก `iLearn.Admin/wwwroot/css/admin-wizard.css` (`.admin-sidebar-card`, `.admin-inline-actions`, `.admin-form-card--sm`, `.admin-form-card--md`, `.admin-form-card--lg`) หลังยืนยันด้วย `rg` ก่อนลบว่าไม่มี reference นอกไฟล์ define และตรวจซ้ำหลังลบว่าไม่พบ selector แล้ว
- ไฟล์หลักที่แตะ: `iLearn.Admin/wwwroot/css/admin-wizard.css`, `DOC/PLANS/PLAN-030-repo-wide-dead-css-cleanup.md`, `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี
- Verified: `rg -n "admin-sidebar-card|admin-inline-actions|admin-form-card--sm|admin-form-card--md|admin-form-card--lg" iLearn.Admin` ก่อนลบพบเฉพาะ define/หลังลบไม่พบผลลัพธ์, `dotnet build iLearn.Admin/iLearn.Admin.csproj` ผ่าน (warnings เดิม, ไม่มี error)

## [2026-06-15 16:09] GitHub Copilot (GPT-5.3-Codex) — สแกน dead CSS ทั้ง repo และสร้าง PLAN-030
- ทำอะไร: สแกนไฟล์ CSS ทั้งโปรเจกต์ (`iLearn.Admin.React/src/index.css`, `iLearn.Admin/wwwroot/css/admin-minimal.css`, `admin-wizard.css`, `admin-tokens.css`, `site.css`) พร้อม cross-reference ไปยัง TS/TSX/CSHTML/JS/C# เพื่อหา selector ที่ไม่ถูกใช้งาน; สรุปผู้ต้องสงสัยและสร้างแผนรอบถัดไป `PLAN-030` โดยโฟกัสการลบ high-confidence dead selectors ฝั่ง Legacy Admin
- ไฟล์หลักที่แตะ: `DOC/PLANS/PLAN-030-repo-wide-dead-css-cleanup.md`, `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี
- Verified: สแกนด้วย `rg` ครอบคลุมทั้ง repo (selector + reference check); แผนถูกสร้างสถานะ `READY` และ assign ให้ GPT

## [2026-06-15 16:02] GitHub Copilot (GPT-5.3-Codex) — Implement PLAN-029 cleanup dead CSS in React index.css
- ทำอะไร: ทำตาม `PLAN-029-cleanup-dead-css` โดยลบ dead CSS จาก `iLearn.Admin.React/src/index.css` ครบ 8 รายการ (classes: `.neon-glow-dot`, `.selected-floating-badge`, `.overflow-wrap-anywhere`, `.wiz-section`, `.wiz-section-title`; keyframes: `neon-glow`, `badge-pulse`, `badge-fade-slide-in`) และตรวจซ้ำว่า keyframe ที่ลบไม่ถูก class ที่เหลืออ้างผ่าน `animation`; sync เอกสาร `DOC/ux_ui_analysis.md` §2.3 ให้ลบมาตรฐานที่เลิกใช้ (`wiz-section`/`wiz-section-title`)
- ไฟล์หลักที่แตะ: `iLearn.Admin.React/src/index.css`, `DOC/ux_ui_analysis.md`, `DOC/PLANS/PLAN-029-cleanup-dead-css.md`, `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี
- Verified: grep ยืนยัน class ที่ลบไม่มี reference ใน `src/**/*.ts,tsx`; grep ใน `index.css` ไม่พบ keyframe/class ที่ลบแล้ว; `npm run lint` ผ่าน, `npm run build` ผ่าน

## [2026-06-15 21:00] Claude Code — เขียน PLAN-029 (clean code CSS — ลบ dead CSS)
- ทำอะไร: ผู้ใช้ขอ clean code CSS — สแกน `index.css` (324 บรรทัด) หา custom class/keyframe ที่ define แต่ไม่ถูกใช้ (grep className ใน src = 0): `.neon-glow-dot`+`@keyframes neon-glow`, `.selected-floating-badge`+`@keyframes badge-pulse`/`badge-fade-slide-in`, `.overflow-wrap-anywhere`, `.wiz-section`, `.wiz-section-title` (8 รายการ) — เขียน **PLAN-029** (GPT): ลบ dead CSS + verify keyframe ไม่ถูก animation อื่นอ้าง + grep ยืนยัน 0 ก่อนลบ + sync ux_ui_analysis §2.3 ถ้าลบ wiz-section ที่ doc ระบุเป็นมาตรฐาน
- ไฟล์หลักที่แตะ: `DOC/PLANS/PLAN-029-cleanup-dead-css.md` (เอกสารล้วน)
- Contract ที่เปลี่ยน: ไม่มี
- Verified: n/a (planner)

## [2026-06-15 15:55] GitHub Copilot (GPT-5.3-Codex) — Fix Controls card vertical jump when switching tabs in Assignment Detail
- ทำอะไร: reproduce บัคที่ `/assignments/252` แล้ววัดตำแหน่งกล่อง Controls พบว่า sticky offset (`lg:top-5`) ทำให้ตำแหน่งกล่องกระโดดต่างกันตามความสูงแท็บ (Overview = +20px, Courses/Learners = 0px); แก้โดยเพิ่ม prop `stickyTopClass` ใน `ControlsSidebar` และตั้งค่าเฉพาะหน้า Assignment Detail เป็น `lg:top-0` เพื่อให้ตำแหน่ง Controls คงที่ทุกแท็บ
- ไฟล์หลักที่แตะ: `iLearn.Admin.React/src/components/ui/ControlsSidebar.tsx`, `iLearn.Admin.React/src/pages/assignments/AssignmentDetailPage.tsx`, `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี API/DB change; เพิ่ม optional prop ฝั่ง UI component `stickyTopClass?: string` (default เดิมยังคง behavior `lg:top-5`)
- Verified: `npm run lint` ผ่าน, `npm run build` ผ่าน, browser measurement บน `/assignments/252` หลังแก้ได้ `deltaControlsTabs = 0` เท่ากันทุกแท็บ (Overview/Courses/Learners)

## [2026-06-15 15:51] GitHub Copilot (GPT-5.3-Codex) — Audit and align remaining list columns with live API payloads
- ทำอะไร: ไล่ตรวจ column mapping ของหน้า list อื่น ๆ ที่ใช้ `EntityListPage` เทียบ payload จริงจาก API แล้วแก้ `moduleConfigs` เพิ่มเติมให้ field ตรงจริงในหลายหน้า: `users` (`displayName/updatedAt` -> `fullName/createdAt`), `masterDataCategories` (`updatedAt` -> `createdAt` พร้อม `divisionName`/`courseCount`), `masterDataCourseTypes` (`updatedAt` -> `createdAt` พร้อม `description`/`courseCount`), `masterDataRoles` (`updatedAt` -> `createdAt` พร้อม `roleType`/`division`), และปรับ `courses` mapping ใน config ให้ใช้ semantic fields (`statusName`, `courseTypeName`, `categoryName`, `canAssign`) เพื่อหลีกเลี่ยงคอลัมน์ไม่ตรง schema
- ไฟล์หลักที่แตะ: `iLearn.Admin.React/src/pages/moduleConfigs.ts`, `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี
- Verified: `npm run lint` ผ่าน, `npm run build` ผ่าน, browser/API smoke check ผ่าน (ทุก config ที่ใช้ใน EntityListPage มี field ครบ ไม่มี missing)

## [2026-06-15 15:46] GitHub Copilot (GPT-5.3-Codex) — Fix blank columns in Enrollments and Divisions list pages
- ทำอะไร: ตรวจ payload จริงจาก API (`EnrollmentsCRUD/Get`, `DivisionsCRUD/Get`) แล้วปรับ `moduleConfigs` ให้ map กับฟิลด์ที่มีอยู่จริง แทนฟิลด์ที่ไม่ถูกส่ง (`assignmentId`, `status`, `updatedAt`) โดยหน้า Enrollments เปลี่ยนเป็นคอลัมน์ `courseCode`/`courseTitle` + คำนวณสถานะจาก `isCompleted`/`progress` และใช้ `dueDate`/`createdAt`; หน้า Divisions เปลี่ยนจาก `updatedAt` เป็น `createdAt` และเพิ่ม `categoryCount`/`roleCount`
- ไฟล์หลักที่แตะ: `iLearn.Admin.React/src/pages/moduleConfigs.ts`, `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี
- Verified: `npm run lint` ผ่าน, `npm run build` ผ่าน, browser check ผ่านที่ `/enrollments` และ `/master-data/divisions` (คอลัมน์ที่เคยขึ้น `—` แสดงข้อมูลจริงแล้ว)

## [2026-06-15 15:40] GitHub Copilot (GPT-5.3-Codex) — Standardize AppTable page-size policy to avoid two-pass loading
- ทำอะไร: ปรับมาตรฐานการโหลดของ `AppTable` ให้คำนวณ `pageSize` แบบ viewport-based + overscan พร้อม debounce ตอน resize และยก `minPageSize` เป็น 60 เพื่อให้ชุดข้อมูลขนาดกลางโหลดจบในรอบแรก ลดอาการที่ผู้ใช้รับรู้ว่า flicker จากการโหลดสองรอบติดกัน
- ไฟล์หลักที่แตะ: `iLearn.Admin.React/src/components/ui/AppTable.tsx`, `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี
- Verified: `npm run lint` ผ่าน (EXIT:0), `npm run build` ผ่าน, browser test ที่ `/learning-logs` หลังแก้พบโหลดคงที่ 53 แถวตั้งแต่รอบแรก (`All records loaded`) และไม่พบ overlay/no-data flash ระหว่าง sampling ต่อเนื่อง

## [2026-06-15 15:29] GitHub Copilot (GPT-5.3-Codex) — Implement PLAN-028 fix AppTable loading flicker
- ทำอะไร: ทำตาม `PLAN-028-apptable-loading-flicker` โดยแก้ `AppTable` ให้ loading overlay แสดงเฉพาะ initial load (ตอนยังไม่มีข้อมูล) เพื่อตัดอาการ blur/flash ทับแถวเดิมระหว่าง infinite scroll และ refetch จาก search/filter/sort โดยไม่เปลี่ยนกลไกหลักของ paging/infinite scroll/startTransition
- ไฟล์หลักที่แตะ: `iLearn.Admin.React/src/components/ui/AppTable.tsx`, `DOC/PLANS/PLAN-028-apptable-loading-flicker.md`, `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี
- Verified: `npm run lint` ผ่าน (0 errors, 0 warnings), `npm run build` ผ่าน, smoke check ผ่านที่ `/assignments` และ `/learners` (initial overlay ยังมี, และระหว่างมีข้อมูลไม่พบ overlay ทับแถว)

## [2026-06-15 20:30] Claude Code — เขียน PLAN-028 (แก้ AppTable กระพริบ)
- ทำอะไร: ผู้ใช้แจ้ง `/assignments` กระพริบ — วินิจฉัย AppTable: spinner overlay (bg-white/45 backdrop-blur) แสดงทุก fetch รวม infinite-scroll page>1 + refetch → blur แฟลชทับแถวเดิม = กระพริบ (footer "Loading more" ก็มีอยู่แล้ว ซ้ำซ้อน) + auto-pagesize/auto-next-page อาจ fetch ซ้ำ → เขียน **PLAN-028** (GPT): แสดง overlay เฉพาะ initial load (`loading && data.length===0`), page>1 ใช้ footer แทน, refetch อาศัย startTransition คงแถวเดิม + (เสริม) ลด fetch ซ้ำจาก ResizeObserver — verify ทุกหน้า list ที่ใช้ AppTable ร่วม
- ไฟล์หลักที่แตะ: `DOC/PLANS/PLAN-028-apptable-loading-flicker.md` (เอกสารล้วน)
- Contract ที่เปลี่ยน: ไม่มี
- Verified: n/a (planner)

## [2026-06-15 20:00] Claude Code — Review + ปิด clean code PLAN-024/025/026/027 เป็น VERIFIED
- ทำอะไร: รีวิวงาน clean code ทั้ง 4 (commit แล้ว git tree สะอาด) — **024** lint 0/0 (group A useCallback, group B steps→plain array); **025** console.log/debug=0 + เติม toast catch เงียบ; **026** สกัด useExplorer+ExplorerTable ใช้ทั้ง 2 หน้า ฟีเจอร์เฉพาะหน้าครบ + breadcrumb loop guard (crumbsKey ref compare) + deep-link guard ถูก; **027** CoursesController Ok(new{)=0 → ApiResponse<T> (shape-compatible) + delegate _courseService — ปรับทั้ง 4 เป็น VERIFIED
- ไฟล์หลักที่แตะ: `DOC/PLANS/PLAN-024..027-*.md` (สถานะ)
- Contract ที่เปลี่ยน: ไม่มี (PLAN-027 เปลี่ยน wrapper เป็น ApiResponse<T> แต่ shape ที่ React อ่าน (success/data) เหมือนเดิม)
- Verified: รันเอง `dotnet test` 118/118, `npm run build` ผ่าน, `npm run lint` 0/0

## [2026-06-15 15:05] GitHub Copilot (GPT-5.3-Codex) — Implement PLAN-026 extract shared explorer for courses + learner groups
- ทำอะไร: ทำตาม `PLAN-026-extract-shared-explorer` โดยสร้าง shared explorer primitives (`useExplorer` + `ExplorerTable`) แล้ว refactor `CourseListPage` และ `LearnerGroupListPage` ให้ใช้ logic กลางร่วมกันสำหรับ query path, deep-link guard, breadcrumb sync, drill/back, และ client-side search โดยคงฟีเจอร์เฉพาะหน้าเดิมครบ (course type chips/category CRUD และ learner-group folder create/move/delete/relocate flows)
- ไฟล์หลักที่แตะ: `iLearn.Admin.React/src/components/ui/explorer/useExplorer.ts`, `iLearn.Admin.React/src/components/ui/explorer/ExplorerTable.tsx`, `iLearn.Admin.React/src/pages/courses/CourseListPage.tsx`, `iLearn.Admin.React/src/pages/learner-groups/LearnerGroupListPage.tsx`, `DOC/PLANS/PLAN-026-extract-shared-explorer.md`, `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี
- Verified: `npm run lint` ผ่าน (EXIT:0), `npm run build` ผ่าน, `dotnet build iLearn.Tests -o artifacts/verify-test` ผ่าน, `dotnet test artifacts/verify-test/iLearn.Tests.dll` ผ่าน (118/118), manual smoke ผ่านที่ `/courses`, `/learner-groups`, และ deep-link `/learner-groups?categoryId=13` (invalid id fallback ตาม deep-link guard)

## [2026-06-15 14:52] GitHub Copilot (GPT-5.3-Codex) — Implement PLAN-027 refactor CoursesController + typed responses increment
- ทำอะไร: ทำตาม `PLAN-027-controller-refactor-dto-increment` โดยย้าย logic หนักของ `CoursesController` ลง service call ให้ controller เหลือ orchestration ใน endpoint `GET Courses/{courseId}/learners`, `GET Courses/{courseId}/assignments`, `GET Courses/{courseId}/dashboard`; เปลี่ยน response wrapper ของ courses/version/status-impact หลาย endpoint จาก anonymous object เป็น `ApiResponse<T>` โดยคง key ที่ frontend ใช้ (`success`, `data`, `message`) และคง `course-types-lookup` เป็น plain array เดิมเพื่อไม่กระทบ contract
- ไฟล์หลักที่แตะ: `iLearn.API/Controllers/CoursesController.cs`, `iLearn.Admin.React/src/pages/courses/CourseDetailPage.tsx`, `iLearn.Admin.React/src/pages/courses/CourseListPage.tsx`, `iLearn.Admin.React/src/pages/courses/CourseEditorPage.tsx`, `iLearn.Admin.React/src/pages/courses/VersionFormPage.tsx`, `DOC/PLANS/PLAN-027-controller-refactor-dto-increment.md`, `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี breaking change (shape ที่ React อ่านยังคงเดิม); เพิ่มคอมเมนต์ `// Mirrors <Dto>` ใน type ฝั่ง React ที่เกี่ยวข้อง
- Verified: `dotnet build iLearn.API/iLearn.API.csproj --artifacts-path artifacts/validate` ผ่าน, `dotnet build iLearn.Tests -o artifacts/verify-test` ผ่าน, `dotnet test artifacts/verify-test/iLearn.Tests.dll` ผ่าน (118/118), `npm run lint` ผ่าน (EXIT:0), `npm run build` ผ่าน (EXIT:0), manual smoke ผ่านที่ `/courses` และ `/courses/823`

## [2026-06-15 20:05] GitHub Copilot (GPT-5.3-Codex) — Implement PLAN-024 แก้ lint warnings react-hooks/exhaustive-deps เหลือ 0
- ทำอะไร: ทำตาม `PLAN-024-fix-lint-warnings` โดยแก้กลุ่ม A ด้วย `useCallback` + ปรับ `useEffect` deps ให้ถูกต้อง (`AssignmentDetailPage.loadAssignmentDetails`, `BulkAssignPage.loadLookups`, `VersionFormPage.loadContentLibrary/loadVersionImpact/loadVersionData`, `LearnerProfilePage.loadProfile`, `MasterDataDetailPage.loadItem`) และแก้กลุ่ม B โดยถอด `useMemo` เฉพาะตัวแปร `steps` ของ wizard 5 หน้าให้เป็น array ปกติ เพื่อล้าง warning ทั้งหมดโดยไม่เปลี่ยน flow
- ไฟล์หลักที่แตะ: `iLearn.Admin.React/src/pages/assignments/AssignmentDetailPage.tsx`, `iLearn.Admin.React/src/pages/assignments/BulkAssignPage.tsx`, `iLearn.Admin.React/src/pages/courses/VersionFormPage.tsx`, `iLearn.Admin.React/src/pages/learners/LearnerProfilePage.tsx`, `iLearn.Admin.React/src/pages/master-data/MasterDataDetailPage.tsx`, `iLearn.Admin.React/src/pages/content-library/ContentItemEditorPage.tsx`, `iLearn.Admin.React/src/pages/courses/CourseEditorPage.tsx`, `iLearn.Admin.React/src/pages/users/UserEditorPage.tsx`, `DOC/PLANS/PLAN-024-fix-lint-warnings.md`, `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี
- Verified: `npm run lint` ผ่าน (0 errors, 0 warnings), `npm run build` ผ่าน, `dotnet test artifacts\verify-test\iLearn.Tests.dll` ผ่าน (118/118), smoke route ผ่าน (`/assignments/bulk`, `/content-library/new`, `/courses/new`, `/users/new`, `/assignments/264`, `/master-data/divisions/1`)

## [2026-06-15 19:26] GitHub Copilot (GPT-5.3-Codex) — Implement PLAN-025 เก็บกวาด console.* ฝั่ง React
- ทำอะไร: ทำตาม `PLAN-025-cleanup-console-statements` โดยลบ `console.error` นอก catch ใน `DashboardPage` 1 จุด, เติม `toast.error` ให้ catch ที่เคยเงียบ 9 จุด (`LearnerDirectorySelector` 3, `EntityListPage` 1, `VersionFormPage` 2, `LearnerGroupCategoryEditorPage` 1, `MasterDataDetailPage` 2), และคง `console.error` ใน catch ที่มี user-facing feedback แล้วไว้ตาม scope
- ไฟล์หลักที่แตะ: `iLearn.Admin.React/src/components/shared/LearnerDirectorySelector.tsx`, `iLearn.Admin.React/src/pages/EntityListPage.tsx`, `iLearn.Admin.React/src/pages/courses/VersionFormPage.tsx`, `iLearn.Admin.React/src/pages/master-data/LearnerGroupCategoryEditorPage.tsx`, `iLearn.Admin.React/src/pages/master-data/MasterDataDetailPage.tsx`, `iLearn.Admin.React/src/pages/DashboardPage.tsx`, `DOC/PLANS/PLAN-025-cleanup-console-statements.md`, `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี
- Verified: `rg 'console\.(log|debug)' iLearn.Admin.React/src -n` = 0, `npm run lint` ผ่าน (0 errors, 11 warnings baseline), `npm run build` ผ่าน, `dotnet test artifacts\verify-test\iLearn.Tests.dll` ผ่าน (118/118)

## [2026-06-15 19:15] Claude Code — ย้าย plan READY ของ Gemini ไป GPT (Gemini เครดิตหมด)
- ทำอะไร: ผู้ใช้แจ้ง Gemini เครดิตหมด → ย้าย plan ที่ยัง READY+Gemini ไป GPT (GPT-5.3 Codex): **PLAN-026** (สกัด shared Explorer), **PLAN-027** (refactor CoursesController+DTO) — plan ที่ VERIFIED แล้ว (022/023) คง Assigned เดิมไว้เป็นประวัติว่า Gemini ทำจริง สรุป: ตอนนี้ READY ทั้งหมด (024/025/026/027) = GPT
- ไฟล์หลักที่แตะ: `DOC/PLANS/PLAN-026-*.md`, `DOC/PLANS/PLAN-027-*.md` (เปลี่ยน Assigned)
- Contract ที่เปลี่ยน: ไม่มี
- Verified: n/a

## [2026-06-15 19:00] Claude Code — เขียนแผน clean code 4 ใบ (PLAN-024..027)
- ทำอะไร: ผู้ใช้เลือก clean code 4 ด้าน — สแกนหาจุดจริง (lint 11 warnings ทั้งหมด exhaustive-deps, console.* 63 จุด/18 ไฟล์, explorer ซ้ำ 2 หน้า, controller/DTO backlog) แล้วเขียน 4 แผน: **PLAN-024** (GPT, แก้ 11 lint warnings เหลือ 0 — useCallback loaders/documented disable wizard steps, ห้ามเปลี่ยน behavior), **PLAN-025** (GPT, เก็บ console.* — ลบ debug log, คง error log + กัน catch เงียบ), **PLAN-026** (Gemini, สกัด shared Explorer hook/component de-dup Course+LearnerGroup, pure de-dup), **PLAN-027** (Gemini, increment ถัดไป refactor CoursesController→CourseService + typed response ใช้ ApiResponse<T>)
- ไฟล์หลักที่แตะ: `DOC/PLANS/PLAN-024..027-*.md` (เอกสารล้วน)
- Contract ที่เปลี่ยน: ไม่มี (แผน)
- Verified: n/a (planner)

## [2026-06-15 14:17] GitHub Copilot (GPT-5.3-Codex) — Smoke test แก้ช่องว่างขวาผิดปกติใน React Admin
- ทำอะไร: เปิด `http://localhost:5173/` ตรวจ layout ด้วย browser automation และพบว่าหน้าแอปมี right gap คงที่ 15px แม้ไม่มี horizontal overflow สาเหตุจาก CSS ที่ตั้ง `scrollbar-gutter: stable` บน `html`; แก้โดยปรับเป็น `scrollbar-gutter: auto` ใน `iLearn.Admin.React/src/index.css` แล้ว reload smoke test ซ้ำ
- ไฟล์หลักที่แตะ: `iLearn.Admin.React/src/index.css`, `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี
- Verified: ก่อนแก้วัดได้ `rightGap: 15` (`windowInnerWidth: 1014`, `scrollWidth: 999`, `scrollbarGutter: stable`), หลังแก้เป็น `rightGap: 0` (`windowInnerWidth: 1014`, `scrollWidth: 1014`, `scrollbarGutter: auto`) และภาพหน้า dashboard ไม่เหลือแถบว่างด้านขวา

## [2026-06-15 14:10] Antigravity — ปรับปรุงกล่อง Overview ในหน้า Detail ทุกหน้าให้ได้มาตรฐานของดีไซน์ระบบ
- ทำอะไร: ปรับปรุงการแสดงผลและโครงสร้างข้อมูลภายใต้กล่อง Overview ของทั้ง 6 หน้าจอหลัก (Assignment, Course, User, Content Item, Master Data, Learner Group) ให้สอดคล้องกันตามดีไซน์ระบบ:
  1. ใช้ `<StatusText>` (outlined pill) เสมอสำหรับการแสดงผลสถานะใน Overview (แทน `<StatusBadge>` ที่มีพื้นหลังทึบและเหมาะสมกับตาราง/KPI)
  2. ปรับขนาดตัวเลข KPI counts / stats ทั้งหมดให้ใช้ขนาดมาตรฐาน `font-bold text-slate-800` (เอา `text-lg` size override ออกในหน้ารายละเอียดของวิชา)
  3. ใส่ `mono` prop ให้กับฟิลด์รหัสผ่าน identifiers เช่น รหัสพนักงาน (NID) หรือ SCORM File Storage ID
  4. ทำความสะอาด grid ในหน้ารายละเอียด SCORM item โดยเปลี่ยน layout ของ Launch Resource และ Server Path เป็น `colSpan="full"`
  5. เอา `mono` ออกจากชื่อผู้สร้าง Owner/Creator ในกลุ่มผู้เรียน
  6. เอา `labelClassName` ที่กำหนดขนาดอักษรทับซ้อนออกใน Master Data เพื่อให้แสดงผลหัวข้อ/ป้ายกำกับขนาดเท่ากันทุกหน้า
- ไฟล์หลักที่แตะ: `iLearn.Admin.React/src/pages/assignments/AssignmentDetailPage.tsx`, `iLearn.Admin.React/src/pages/courses/CourseDetailPage.tsx`, `iLearn.Admin.React/src/pages/users/UserDetailPage.tsx`, `iLearn.Admin.React/src/pages/content-library/ContentItemDetailPage.tsx`, `iLearn.Admin.React/src/pages/master-data/MasterDataDetailPage.tsx`, `iLearn.Admin.React/src/pages/learner-groups/LearnerGroupDetailPage.tsx`
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี
- Verified: `npm run lint` ผ่าน (0 errors, 11 warnings baseline), `npm run build` ผ่าน, `dotnet test` (118/118 tests passed)

## [2026-06-15 14:05] Antigravity — ปรับปรุงมาตรฐานดีไซน์และโครงสร้างหน้าจอ Detail ในส่วนที่เหลือเพื่อแก้ปัญหา Layout Shift และจัดระเบียบหัวข้อ
- ทำอะไร: ปรับปรุงโครงสร้างหน้าจอแสดงรายละเอียด (Detail/Overview) ทั้ง 5 หน้าจอที่เหลือ (LearnerGroupDetailPage, CourseDetailPage, UserDetailPage, ContentItemDetailPage, MasterDataDetailPage) จากเดิมที่ใช้ `<DetailCard>` หรือโครงสร้าง Custom margin/padding ที่มีปัญหา visual shift/scrollbar alignment ต่างกันเมื่อสลับแท็บ ให้มาใช้โครงสร้างที่เป็นมาตรฐานเดียวกันทั้งหมด คือครอบด้วยกล่อง `<section className="overflow-hidden rounded-lg border border-slate-200 bg-white shadow-xs">` พร้อมใช้ `<SectionHeader variant="card">` เพื่อแสดงหัวข้อสไตล์ flush card และครอบเนื้อหาภายในด้วย padding `p-5` เพื่อความสวยงาม เป็นระเบียบเรียบร้อย และสม่ำเสมอ
- ไฟล์หลักที่แตะ: `iLearn.Admin.React/src/pages/learner-groups/LearnerGroupDetailPage.tsx`, `iLearn.Admin.React/src/pages/courses/CourseDetailPage.tsx`, `iLearn.Admin.React/src/pages/users/UserDetailPage.tsx`, `iLearn.Admin.React/src/pages/content-library/ContentItemDetailPage.tsx`, `iLearn.Admin.React/src/pages/master-data/MasterDataDetailPage.tsx`
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี
- Verified: `npm run lint` ผ่าน (0 errors, 11 warnings baseline), `npm run build` ผ่าน, `dotnet test` (118/118 tests passed)

## [2026-06-15 18:30] Claude Code — Review + ปิด PLAN-021/022/023 เป็น VERIFIED
- ทำอะไร: รีวิว diff ที่ commit แล้ว — **021** (acdfa2c): App.tsx ครอบ learning-logs ด้วย RequireRole superAdminOnly + nav item superAdminOnly; **022** (acdfa2c): CreateAsync group+category ใช้ `parent?DivisionId : (IsSuperAdmin?dto.DivisionId:currentUser.DivisionId)` กัน escalation+inherit parent; **023** (3373581): UpdateLearnerGroupCategoryDto+DivisionId, UpdateAsync มี parent-inherit + SuperAdmin-only + empty-check guard (กันเปลี่ยน division ของ category ที่มีลูก/group), frontend edit selector + explorer folder selector (useSession) — ปรับทั้ง 3 เป็น VERIFIED
- ไฟล์หลักที่แตะ: `DOC/PLANS/PLAN-021/022/023-*.md` (สถานะ)
- Contract ที่เปลี่ยน: ไม่มี (โค้ด commit แล้ว — เพิ่ม DivisionId ใน Create/Update LearnerGroupCategory DTO ตามแผน)
- Verified: `dotnet test` 118/118 ผ่าน, `npm run build` ผ่าน, `npm run lint` 0 errors (11 warnings baseline)
- ⚠️ ข้อสังเกต: working tree มี diff ค้าง ~20 ไฟล์ (App.tsx, AppTable, index.css, Breadcrumbs, หน้าต่าง ๆ) — **ไม่ใช่งานของ PLAN-021/022/023** (พวกนั้น commit แล้ว) ดูเหมือนรอบ refactor UI/button styles ที่ยังไม่ commit — build/lint/test ผ่านบน state นี้ แต่ยังไม่ได้รีวิวเนื้อหา

## [2026-06-15 13:58] Antigravity — ปรับปรุงมาตรฐานการแสดงผลหัวข้อและจัดระเบียบโครงสร้างแท็บในหน้า Assignment Detail
- ทำอะไร: ปรับปรุงโครงสร้างของแท็บ Overview ในหน้า `AssignmentDetailPage.tsx` จากเดิมที่ใช้ `<DetailCard>` และหัวข้อประเภท plain ให้เปลี่ยนมาใช้โครงสร้าง `<section>` แบบมีขอบมุมตัด (`overflow-hidden rounded-lg border`) และหัวข้อประเภท `card` (`variant="card"`) ให้สอดคล้องกันทุกแท็บ (Overview, Courses, Learners) ส่งผลให้ปุ่ม Controls ฝั่งขวาไม่ขยับสั่นตำแหน่งเดิม และขนาดตัวอักษรของหัวข้อ "Overview" มีความเหมาะสมและเป็นระเบียบเท่ากับแท็บอื่นๆ
- ไฟล์หลักที่แตะ: `iLearn.Admin.React/src/pages/assignments/AssignmentDetailPage.tsx`
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี
- Verified: `npm run lint` ผ่าน, `npm run build` ผ่าน

## [2026-06-15 13:56] Antigravity — ป้องกันการขยับของหน้าจอ (Layout Shift) เมื่อสลับแท็บข้อมูลที่มีความสูงต่างกัน
- ทำอะไร: เพิ่มกฎ CSS `scrollbar-gutter: stable` ให้กับอิลิเมนต์ `html` ในไฟล์ `index.css` เพื่อจองพื้นที่สำหรับแถบเลื่อนแนวตั้ง (Scrollbar) เสมอ ป้องกันปัญหาโครงสร้างหน้าจอขยับหรือสั่น (Scrollbar Layout Shift) เมื่อผู้ใช้งานสลับไปยังแท็บที่มีความสูงข้อมูลต่างกันอย่างรวดเร็ว
- ไฟล์หลักที่แตะ: `iLearn.Admin.React/src/index.css`
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี
- Verified: `npm run lint` ผ่าน, `npm run build` ผ่าน

## [2026-06-15 13:54] Antigravity — ปรับปรุงการจัดกลุ่มข้อมูลรายชื่อผู้เรียน (Learners) ในหน้าแสดงรายละเอียดของ Assignment
- ทำอะไร: อัปเดต `AssignmentDetailPage.tsx` ให้ประมวลผลข้อมูลในแท็บ Learners โดยทำการจัดกลุ่ม (Grouping) ข้อมูลตามตัวตนของผู้เรียนผ่านการทำ `useMemo` (แทนการวนลูปแบบแบนราบเดิมที่ทำให้ชื่อคนซ้ำกันตามจำนวนวิชาที่ได้รับมอบหมาย) โดยจัดให้แสดงผล 1 แถวต่อ 1 คน และรวมรายชื่อวิชาที่ได้รับมอบหมายเข้าไปเป็นรายการย่อยพร้อมความคืบหน้าและสถานะการเรียนรายวิชาภายในแถวเดียวกัน เพื่อให้อ่านและลบ/รีเซ็ตข้อมูลได้สะดวกและไม่สับสน
- ไฟล์หลักที่แตะ: `iLearn.Admin.React/src/pages/assignments/AssignmentDetailPage.tsx`
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี
- Verified: `npm run lint` ผ่าน, `npm run build` ผ่าน

## [2026-06-15 13:51] Antigravity — ปรับปรุงการแสดงผลคอลัมน์วิชาเรียน (Courses) ในตาราง Assignment Batches ให้กระชับและไม่ยาวเกินไป
- ทำอะไร: อัปเดต `EntityListPage.tsx` เพื่อปรับแต่ง `cellRender` ของคอลัมน์ `courseNames` ในกรณีที่มีหลายวิชาเรียน โดยจะนำวิชาแรกมาแสดงผลพร้อมตัดคำให้อยู่ในกรอบ และแสดง Badge ตัวเลขระบุจำนวนวิชาเพิ่มเติม (เช่น `+2`) พร้อมกำหนดคำแนะนำเมาส์ชี้ (Tooltip) แสดงรายชื่อวิชาทั้งหมดเมื่อนำเมาส์ไปชี้ เพื่อความสะอาดตาและเป็นระเบียบของตารางข้อมูล
- ไฟล์หลักที่แตะ: `iLearn.Admin.React/src/pages/EntityListPage.tsx`
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี
- Verified: `npm run lint` ผ่าน, `npm run build` ผ่าน

## [2026-06-15 13:43] Antigravity — ป้องกันการกดเบิ้ล (Double Click) เข้าหน้า Detail ของรายการ Enrollments และ Learning Logs ที่ไม่มีหน้าดีเทล
- ทำอะไร: ปิดความสามารถในระดับ UI โดยส่ง `onRowDblClick`เป็น `undefined` และปิด cursor pointer สำหรับหน้าแสดงผลของข้อมูลที่ไม่มีหน้าดีเทล (ได้แก่ `Enrollments` และ `Learning Logs` ซึ่งเก็บประวัติ/อ่านอย่างเดียว) เพื่อป้องกันการลิงก์ไปหน้าเพจที่ไม่มีอยู่จริงจนเกิด Not Found error
- ไฟล์หลักที่แตะ: `iLearn.Admin.React/src/pages/EntityListPage.tsx`, `iLearn.Admin.React/src/components/ui/AppTable.tsx`
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี
- Verified: `npm run lint` ผ่าน, `npm run build` ผ่าน

## [2026-06-15 13:39] Antigravity — แก้ไขข้อผิดพลาด SyntaxError จากการลบหมวดหมู่ (Delete Category) ในหน้า Courses Explorer
- ทำอะไร: แก้ไขฟังก์ชัน `fetchWithAccessControl` ใน `apiClient.ts` ให้สามารถรองรับและประมวลผล HTTP status 200 OK ที่ไม่มี Response Body (Empty Body) ได้อย่างปลอดภัย โดยอ่านข้อมูลดิบเป็นข้อความและตรวจสอบความว่างเปล่าก่อนส่งไปแปลง JSON เพื่อไม่ให้เกิด `SyntaxError: Unexpected end of JSON input`
- ไฟล์หลักที่แตะ: `iLearn.Admin.React/src/lib/apiClient.ts`
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี
- Verified: `npm run lint` ผ่าน, `npm run build` ผ่าน

## [2026-06-15 13:35] Antigravity — ปรับเปลี่ยนคำศัพท์หน้า Bulk Assignment เป็น Assign Courses
- ทำอะไร: ปรับปรุงคำศัพท์ในระบบจากเดิมคือ "Bulk Assignment" หรือ "Bulk Assign" ให้เรียบง่ายและเป็นมิตรกับผู้ใช้งานมากขึ้นเป็น "Assign Courses" ในส่วนของ breadcrumbs, ปุ่มเปิดหน้าจากตารางรายการ และหัวข้อของ wizard page
- ไฟล์หลักที่แตะ: `iLearn.Admin.React/src/components/layout/Breadcrumbs.tsx`, `iLearn.Admin.React/src/pages/EntityListPage.tsx`, `iLearn.Admin.React/src/pages/assignments/BulkAssignPage.tsx`
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี
- Verified: `npm run lint` ผ่าน, `npm run build` ผ่าน

## [2026-06-15 13:32] Antigravity — ปรับปรุง UI/UX การเพิ่มผู้เรียนในหน้า Assignment Detail ให้เหมือนหน้า Learner Group Detail
- ทำอะไร: ปรับปรุงฟอร์มการเพิ่มผู้เรียน (Add More Learners) ในหน้า AssignmentDetailPage จากแบบเดิมที่เป็น textarea ให้กลายเป็น popup modal แบบ 2 แท็บ (Directory Search / Bulk Import) และระบบคิวเหมือนในหน้าดีเทลของกลุ่มผู้เรียน โดยเรียกใช้งาน component LearnerDirectorySelector
- ไฟล์หลักที่แตะ: `iLearn.Admin.React/src/pages/assignments/AssignmentDetailPage.tsx`
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี
- Verified: `npm run lint` ผ่าน, `npm run build` ผ่าน

## [2026-06-15 13:28] Antigravity — ปรับปรุงระบบแก้ไขกลุ่มผู้เรียนให้ทำงานผ่าน Popup Modal ในหน้าดีเทล
- ทำอะไร: ย้ายฟอร์มการแก้ไขคุณสมบัติกลุ่มผู้เรียน (Edit Learner Group) จากหน้าเว็บ /edit แยกต่างหาก มาทำงานเป็นแบบ Popup Modal ในหน้าแสดงรายละเอียด (LearnerGroupDetailPage) แทนการเปิดหน้าใหม่ โดยปรับให้ดึงข้อมูลโฟลเดอร์ผ่าน Category Explorer Modal (z-index 60) ร่วมด้วย และลบเส้นทาง Route ที่ไม่ได้ใช้งาน รวมถึงลบส่วนโค้ดแก้ไขกลุ่มในหน้าสร้างกลุ่ม (LearnerGroupEditorPage) ออก
- ไฟล์หลักที่แตะ: `iLearn.Admin.React/src/pages/learner-groups/LearnerGroupDetailPage.tsx`, `iLearn.Admin.React/src/pages/learner-groups/LearnerGroupEditorPage.tsx`, `iLearn.Admin.React/src/App.tsx`
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี
- Verified: `npm run lint` ผ่าน, `npm run build` ผ่าน, `dotnet test` ผ่าน (118/118 tests passed)

## [2026-06-15 13:24] Antigravity — ปรับปรุงคำของหัวข้อ (Title) และคำอธิบาย (Description) ในหน้าฟอร์มแก้ไขข้อมูล (Editor/Form Pages)
- ทำอะไร: ปรับปรุงคำอธิบายและหัวข้อในหน้ากรอกฟอร์มแก้ไขข้อมูลทั้งหมด ได้แก่ CourseEditorPage, VersionFormPage, ContentItemEditorPage, LearnerGroupEditorPage, LearnerGroupCategoryEditorPage, UserEditorPage ให้มีความสั้น กระชับ เป็นมาตรฐานเดียวกัน และแก้ไขคำภาษาอังกฤษที่ฟุ่มเฟือย
- ไฟล์หลักที่แตะ: `iLearn.Admin.React/src/pages/courses/CourseEditorPage.tsx`, `iLearn.Admin.React/src/pages/courses/VersionFormPage.tsx`, `iLearn.Admin.React/src/pages/content-library/ContentItemEditorPage.tsx`, `iLearn.Admin.React/src/pages/learner-groups/LearnerGroupEditorPage.tsx`, `iLearn.Admin.React/src/pages/master-data/LearnerGroupCategoryEditorPage.tsx`, `iLearn.Admin.React/src/pages/users/UserEditorPage.tsx`
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี
- Verified: `npm run lint` ผ่าน, `npm run build` ผ่าน, `dotnet test` ผ่าน (118/118 tests passed)

## [2026-06-15 13:19] Antigravity — ปรับปรุงและปรับกระชับคำของหัวข้อเพจ (Title) และคำอธิบาย (Note) ทุกเพจ
- ทำอะไร: ปรับปรุงคำใน config และเพจต่าง ๆ ให้สั้น กระชับ และเป็นมาตรฐานเดียวกัน เช่น ปรับ Title/Note ใน config ของวิชา, SCORM, batch, ผู้เรียน, และ admin รวมถึงอัปเดตเพจ Custom list ที่มีข้อความ hardcoded (เช่น AdminUsersPage, LearnerGroupCategoriesPage, LearnerGroupListPage, CourseListPage, AssignmentGanttPage)
- ไฟล์หลักที่แตะ: `iLearn.Admin.React/src/pages/moduleConfigs.ts`, `iLearn.Admin.React/src/pages/users/AdminUsersPage.tsx`, `iLearn.Admin.React/src/pages/master-data/LearnerGroupCategoriesPage.tsx`, `iLearn.Admin.React/src/pages/learner-groups/LearnerGroupListPage.tsx`, `iLearn.Admin.React/src/pages/courses/CourseListPage.tsx`, `iLearn.Admin.React/src/pages/assignments/AssignmentGanttPage.tsx`
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี
- Verified: `npm run lint` ผ่าน, `npm run build` ผ่าน, `dotnet test` ผ่าน (118/118 tests passed)

## [2026-06-15 13:12] Antigravity — ปรับปรุงและปรับสไตล์ของปุ่มใน Action Column ให้ได้มาตรฐานเดียวกัน (ครอบคลุมครบทุกหน้า)
- ทำอะไร: ปรับปรุง `AppTable.tsx` ให้มีสิทธิ์เลือก variant ของปุ่ม action และเพิ่มระบบตรวจจับสีอัตโนมัติตาม Hint text (เช่น คำว่า delete, remove จะได้สีแดงทันที); ทำการแก้โค้ดและย้ายคลาสปุ่ม action ในตารางแบบ Custom ของหน้าต่าง ๆ ให้ใช้คลาสดีไซน์และขนาดไอคอน h-3.5 w-3.5 ที่เป็นอันหนึ่งอันเดียวกัน (ครอบคลุมหน้า CourseListPage, LearnerGroupListPage, LearnerGroupCategoriesPage, AssignmentDetailPage, BulkAssignPage, LearnerGroupDetailPage, ContentItemEditorPage, CourseEditorPage, VersionFormPage)
- ไฟล์หลักที่แตะ: `iLearn.Admin.React/src/components/ui/AppTable.tsx`, `iLearn.Admin.React/src/pages/courses/CourseListPage.tsx`, `iLearn.Admin.React/src/pages/learner-groups/LearnerGroupListPage.tsx`, `iLearn.Admin.React/src/pages/master-data/LearnerGroupCategoriesPage.tsx`, `iLearn.Admin.React/src/pages/assignments/AssignmentDetailPage.tsx`, `iLearn.Admin.React/src/pages/assignments/BulkAssignPage.tsx`, `iLearn.Admin.React/src/pages/learner-groups/LearnerGroupDetailPage.tsx`, `iLearn.Admin.React/src/pages/content-library/ContentItemEditorPage.tsx`, `iLearn.Admin.React/src/pages/courses/CourseEditorPage.tsx`, `iLearn.Admin.React/src/pages/courses/VersionFormPage.tsx`
- Contract ที่เปลี่ยน (API shape / props / DB): เพิ่ม `variant?: 'primary' | 'danger' | 'success' | 'ghost' | undefined` ใน `actionButtons` element ของ `AppTable`
- Verified: `npm run lint` ผ่าน, `npm run build` ผ่าน, `dotnet test` (118/118 tests passed)

## [2026-06-15 12:59] GitHub Copilot (GPT-5.3-Codex) — เอกสาร cleanup รอบสาม (ล้างไฟล์แผนเก่าชุดใหญ่)
- ทำอะไร: ลบไฟล์แผนใน `DOC/PLANS` ที่ปิดงานแล้วทั้งหมด (สถานะ VERIFIED/CANCELLED เดิม) จำนวน 21 ไฟล์ เพื่อเก็บเฉพาะแผนล่าสุดที่ยังเป็น `DONE` และ `README` สำหรับ workflow ปัจจุบัน
- ไฟล์หลักที่แตะ: `DOC/PLANS/PLAN-001-*.md` ถึง `DOC/PLANS/PLAN-020-*.md` (deleted เฉพาะไฟล์ที่มีอยู่จริง), `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี
- Verified: ตรวจโฟลเดอร์หลังลบแล้วเหลือเฉพาะ `README.md`, `PLAN-021-*.md`, `PLAN-022-*.md`, `PLAN-023-*.md`

## [2026-06-15 12:58] GitHub Copilot (GPT-5.3-Codex) — เอกสาร cleanup รอบสองแบบเข้มขึ้น (ลบ historical docs เพิ่ม)
- ทำอะไร: ลบเอกสาร historical/transition ที่ไม่ใช้งานแล้วและไม่ถูกอ้างอิงในเอกสาร active ได้แก่เอกสารแผนย้าย React เก่า, gap analysis เก่า, smoke report เก่า และไฟล์ function audit ระดับราก; ปรับ `DOC/ux_ui_analysis.md` เพื่อลบลิงก์ค้างไปไฟล์ที่ลบและชี้ไปคู่มือปัจจุบัน (`iLearn.Admin.React/README.md`) แทน
- ไฟล์หลักที่แตะ: `DOC/REACT-ADMIN-REBUILD-PLAN.md` (deleted), `DOC/REACT-ADMIN-GAP-ANALYSIS.md` (deleted), `DOC/REACT-ADMIN-SMOKETEST-REPORT.md` (deleted), `ilearn_function_audit.md` (deleted), `DOC/ux_ui_analysis.md`, `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี
- Verified: ตรวจด้วย `rg` แล้วไม่พบ reference ค้างถึงไฟล์ที่ถูกลบในชุดเอกสาร active (`DOC`, `AGENTS.md`, `CLAUDE.md`, `CONTRIBUTING.md`, `iLearn.Admin.React/README.md`, `.github/copilot-instructions.md`)

## [2026-06-15 12:54] GitHub Copilot (GPT-5.3-Codex) — ลบเอกสาร/ไฟล์ประกอบที่ไม่จำเป็นและไม่ถูกอ้างอิง
- ทำอะไร: ทำความสะอาดไฟล์เอกสาร/ไฟล์รายงานเก่าที่ไม่ใช้งาน โดยตรวจค้นทั้งรีโปก่อนลบด้วย `rg` แล้วยืนยันว่าไม่มีที่ใดอ้างอิง จากนั้นลบไฟล์ที่เป็น old snapshot และไฟล์ชั่วคราวจากการตรวจ API
- ไฟล์หลักที่แตะ: `DOC/AppDbContextModelSnapshot_OLD.cs` (deleted), `content-items-response.json` (deleted), `network-commit-check.txt` (deleted), `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี
- Verified: `rg` ไม่พบ reference ของทั้งสามไฟล์ก่อนลบ, และ `git ls-files` ยืนยันเป็นไฟล์ที่ tracked จริง

## [2026-06-15 12:49] GitHub Copilot (GPT-5.3-Codex) — Audit เอกสาร DOC และอัปเดตสถานะให้ตรงโค้ดล่าสุด
- ทำอะไร: ตรวจเอกสารในโฟลเดอร์ DOC แบบกว้าง (grep สัญญาณล้าสมัย + ยืนยันกับโค้ดจริง) แล้วอัปเดต 2 เอกสารหลักให้เป็นปัจจุบัน: `DOC/api_analysis.md` (แก้จากข้อมูลยุคก่อน PLAN-013/021/022/023 เป็นสถานะปัจจุบัน พร้อมตัวเลขจาก inventory ล่าสุด), `DOC/division_isolation_analysis.md` (เปลี่ยนจาก pending findings เป็นสถานะหลัง implement แผนแล้ว และสรุปประเด็นที่ยัง open decision); ยืนยันซ้ำว่า `FileStoragesCRUDController` ไม่มีแล้ว, Learning Logs ถูก gate แบบ SuperAdmin ใน UI route/menu, และ flow Division ของ Learner Group Category ตรงกับ implementation ปัจจุบัน
- ไฟล์หลักที่แตะ: `DOC/api_analysis.md`, `DOC/division_isolation_analysis.md`, `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี (เอกสารล้วน)
- Verified: ตรวจ consistency ด้วย grep/read หลักฐานจากโค้ดจริง (`iLearn.Application/Services/LearnerGroupService.cs`, `iLearn.Application/Services/LearnerGroupCategoryService.cs`, `iLearn.Admin.React/src/App.tsx`, `iLearn.Admin.React/src/config/navigation.ts`, `DOC/API-ENDPOINT-INVENTORY.md`) และ re-scan คีย์เวิร์ด stale ใน 2 เอกสารที่อัปเดตแล้วไม่พบข้อความขัดแย้งเดิม

## [2026-06-15 12:40] Antigravity — ปรับปรุงและปรับสไตล์ของปุ่มใน Action Column ให้ได้มาตรฐานเดียวกัน
- ทำอะไร: ปรับปรุง `AppTable.tsx` ให้มีสิทธิ์เลือก variant ของปุ่ม action และเพิ่มระบบตรวจจับสีอัตโนมัติตาม Hint text (เช่น คำว่า delete, remove จะได้สีแดงทันที); ทำการแก้โค้ดและย้ายคลาสปุ่ม action ในตารางแบบ Custom ของหน้าต่าง ๆ ให้ใช้คลาสดีไซน์และขนาดไอคอน h-3.5 w-3.5 ที่เป็นอันหนึ่งอันเดียวกัน
- ไฟล์หลักที่แตะ: `iLearn.Admin.React/src/components/ui/AppTable.tsx`, `iLearn.Admin.React/src/pages/courses/CourseListPage.tsx`, `iLearn.Admin.React/src/pages/learner-groups/LearnerGroupListPage.tsx`, `iLearn.Admin.React/src/pages/master-data/LearnerGroupCategoriesPage.tsx`, `iLearn.Admin.React/src/pages/assignments/AssignmentDetailPage.tsx`, `iLearn.Admin.React/src/pages/learner-groups/LearnerGroupDetailPage.tsx`, `iLearn.Admin.React/src/pages/content-library/ContentItemEditorPage.tsx`, `iLearn.Admin.React/src/pages/courses/CourseEditorPage.tsx`, `iLearn.Admin.React/src/pages/courses/VersionFormPage.tsx`
- Contract ที่เปลี่ยน (API shape / props / DB): เพิ่ม `variant?: 'primary' | 'danger' | 'success' | 'ghost' | undefined` ใน `actionButtons` element ของ `AppTable`
- Verified: `npm run lint` ผ่าน, `npm run build` ผ่าน, `dotnet test` (118/118 tests passed)

## [2026-06-15 12:13] GitHub Copilot (GPT-5.3-Codex) — สร้างรายงาน Inventory Endpoint ทั้งหมดเป็นไฟล์ Markdown
- ทำอะไร: สแกน controller ทั้งหมดใน `iLearn.API/Controllers` จาก `[Route]` + `[Http*]` attributes แล้วสร้างรายงาน inventory endpoint แบบครบถ้วน (verb/route/controller/action/policy/source) พร้อมสรุปนับตาม verb, route family, policy และเพิ่ม SignalR hub mapping จาก `Program.cs`
- ไฟล์หลักที่แตะ: `DOC/API-ENDPOINT-INVENTORY.md`, `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี (เอกสารล้วน)
- Verified: ตรวจผลในไฟล์รายงานแล้วมีสรุปครบ (`TotalEndpoints: 165`, `TotalControllersWithEndpoints: 30`, `TotalHubs: 1`) และมีตาราง inventory endpoint ครบ

## [2026-06-15 11:47] Antigravity — ปรับปรุงหน้า Assignment Report และหน้า Detail ให้ไม่แสดง Description และใช้โครงสร้างมาตรฐาน
- ทำอะไร: ปรับปรุง `AssignmentReportPage.tsx` ให้ใช้โครงสร้าง `DetailLayout` และมีแถบด้านขวา `ControlsSidebar` สำหรับเก็บปุ่ม Print และ Export CSV เหมือนหน้า Detail ทั่วไป; ยกเลิกการแสดงผลรายละเอียด Description และยกเลิก Page Header ซ้ำซ้อน (เนื่องจากมี Breadcrumb แสดงผลอยู่แล้ว) ทั้งในหน้า Report และหน้า Detail (`AssignmentDetailPage.tsx`) พร้อมปรับปรุงการเว้นระยะห่างหัวตาราง FactGrid ในการ์ด Overview/Report Summary; เพิ่มการแสดงผล Assignment No. แบบกว้างเต็มบรรทัด (colSpan="full") ที่หัวตาราง FactGrid ในการ์ด Overview ของหน้า Report; ตกแต่งเนื้อหาฝั่งซ้ายโดยใช้ `DetailCard`, `FactGrid`, `Fact`, และ `DetailSubSection` ร่วมกับไอคอนวิชาเรียน; ใช้ `StatusBadge` และ `ProgressBar` ในตารางแสดงความคืบหน้าของ Learners พร้อมกล่องกรองสถานะและช่องค้นหารูปแบบมาตรฐาน
- ไฟล์หลักที่แตะ: `iLearn.Admin.React/src/pages/assignments/AssignmentReportPage.tsx`, `iLearn.Admin.React/src/pages/assignments/AssignmentDetailPage.tsx`
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี
- Verified: `npm run lint` และ `npm run build` ผ่าน, `dotnet test` (118/118 tests passed)

## [2026-06-15 11:34] Antigravity — ปรับปรุงไอคอนวิชาเรียน (Courses) ในหน้า Courses Explorer ให้สอดคล้องกัน
- ทำอะไร: เปลี่ยนการใช้งานไอคอนวิชาเรียนจาก `Layers` เป็น `BookOpen` (สี `text-indigo-500`) เพื่อปรับปรุงความเหมาะสมและคงความสอดคล้องตาม design system ส่วนอื่น ๆ ในระบบที่ใช้ `BookOpen` เป็นตัวแทนของวิชาเรียน
- ไฟล์หลักที่แตะ: `iLearn.Admin.React/src/pages/courses/CourseListPage.tsx`
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี
- Verified: `npm run lint` และ `npm run build` ผ่าน, `dotnet test` (118/118 tests passed)


## [2026-06-15 11:32] Antigravity — ปรับปรุงไอคอนโฟลเดอร์ Division ในหน้า Courses Explorer
- ทำอะไร: เพิ่ม `isDivision: true` ให้กับรายการ Division ใน list mapping และอัปเดตไอคอนในตาราง `CourseListPage.tsx` ให้ใช้ `Building2` (สี `text-indigo-500`) สำหรับโฟลเดอร์ Division เพื่อแยกให้แตกต่างอย่างชัดเจนจากโฟลเดอร์ Category (ไอคอน `Folder` สี `text-amber-500`)
- ไฟล์หลักที่แตะ: `iLearn.Admin.React/src/pages/courses/CourseListPage.tsx`
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี
- Verified: `npm run lint` และ `npm run build` ผ่าน, `dotnet test` (118/118 tests passed)


## [2026-06-15 11:30] Antigravity — เพิ่มการสร้าง เปลี่ยนชื่อ และลบ Category ในหน้า Courses Explorer
- ทำอะไร: อัปเดต `CourseListPage.tsx` ให้มีปุ่ม "New Category" ใน actions bar (เมื่ออยู่ระดับ root สำหรับ SuperAdmin หรืออยู่ระดับ division สำหรับทุก admin) และปุ่ม Rename/Delete (ไอคอนดินสอและถังขยะ) ถัดจากโฟลเดอร์หมวดหมู่; สร้าง modals สำหรับกรอกชื่อหมวดหมู่ใหม่และการเปลี่ยนชื่อ; เช็ค `courseCount` เพื่อ block การลบหมวดหมู่ที่มีวิชาเรียนอยู่ภายใน และใช้ `useConfirm` ยืนยันการลบ; อัปเดต `CategoryLookup` structure ให้มี `courseCount`
- ไฟล์หลักที่แตะ: `iLearn.Admin.React/src/pages/courses/CourseListPage.tsx`
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี (เรียกใช้งาน endpoint CategoriesCRUD เดิมบน backend)
- Verified: `npm run lint` และ `npm run build` ผ่าน, `dotnet test` (118/118 tests passed)


## [2026-06-15 11:15] Antigravity — Implement PLAN-023 SuperAdmin เลือก Division ได้ตอน Edit Category + สร้าง folder ในหน้า Explorer
- ทำอะไร: อัปเดต `UpdateLearnerGroupCategoryDto` ให้รับ `DivisionId` และปรับปรุง `LearnerGroupCategoryService.UpdateAsync` ให้รองรับการเปลี่ยน/ระบุ `DivisionId` โดย SuperAdmin; เพิ่มการตรวจสอบป้องกันความเสี่ยง (Division Update Safety Check) ใน service เพื่อ block/ปฏิเสธการเปลี่ยน division หากหมวดหมู่ไม่ได้ว่าง (มี sub-categories หรือ learner groups ภายใน); ปรับปรุง UI `LearnerGroupCategoryEditorPage.tsx` ให้ SuperAdmin สามารถแก้ไข/เลือกแผนกในหน้าจอ edit หมวดหมู่ได้; ปรับปรุงหน้า explorer `LearnerGroupListPage.tsx` ให้แสดง dropdown เลือก Division ใน modal "Create Folder" เมื่อ SuperAdmin สร้างโฟลเดอร์ที่ root level (`currentCategoryId === 0`)
- ไฟล์หลักที่แตะ: `iLearn.Application/DTOs/LearnerGroupCategoryDto.cs`, `iLearn.Application/Services/LearnerGroupCategoryService.cs`, `iLearn.Admin.React/src/pages/master-data/LearnerGroupCategoryEditorPage.tsx`, `iLearn.Admin.React/src/pages/learner-groups/LearnerGroupListPage.tsx`
- Contract ที่เปลี่ยน (API shape / props / DB): เพิ่ม `DivisionId` ใน `UpdateLearnerGroupCategoryDto`
- Verified: `npm run lint` และ `npm run build` ผ่าน, `dotnet test` (118/118 tests passed)

## [2026-06-15 17:40] Claude Code — เขียน PLAN-023 (SuperAdmin เลือก division ตอน Edit category + folder ใน explorer)
- ทำอะไร: ผู้ใช้ขอให้ SuperAdmin เลือก division ได้ตอน New/Edit Category ทั้ง `/master-data/learner-group-categories` และ `/learner-groups` — ตรวจแล้ว PLAN-022 ทำ create-side ที่ master-data editor แล้ว แต่ยังขาด (1) Edit category (PLAN-022 กัน UpdateAsync ออก, UpdateDto ไม่มี DivisionId), (2) explorer `handleCreateFolder` POST ไม่ส่ง divisionId + ไม่มี selector (explorer ไม่มี edit-category) → เขียน **PLAN-023** (Gemini): backend เพิ่ม DivisionId ใน UpdateLearnerGroupCategoryDto + UpdateAsync (IsSuperAdmin only, inherit parent, ปลอดภัยต่อ isolation), frontend แสดง selector ตอน edit ที่ master-data + ตอนสร้าง folder ที่ explorer root (inherit ถ้าอยู่ใน sub-folder)
- ไฟล์หลักที่แตะ: `DOC/PLANS/PLAN-023-*.md` (เอกสารล้วน)
- Contract ที่เปลี่ยน: ไม่มี (ตอน implement จะเพิ่ม DivisionId ใน UpdateLearnerGroupCategoryDto)
- Verified: n/a (planner) — หมายเหตุ: PLAN-022 ยัง DONE รอรีวิว, PLAN-023 ต่อยอดจากมัน

## [2026-06-15 11:05] Antigravity — Implement PLAN-022 ให้ SuperAdmin ระบุ Division ตอนสร้าง Learner Group / Learner Group Category
- ทำอะไร: อัปเดต React UI `LearnerGroupCategoryEditorPage.tsx` ให้ SuperAdmin สามารถระบุ/เลือก Division เมื่อสร้างหมวดหมู่ได้ (และจะสืบทอด DivisionId อัตโนมัติจาก Parent Category เมื่อระบุหมวดหมู่หลัก พร้อมแสดงคำชี้แจงและปิดการใช้งาน selector); อัปเดต type definition `LearnerGroupCategory` ใน `LearnerGroupCategoriesPage.tsx` เพื่อให้ sync ตาม backend contract DTO; แก้ไขและนำ unused import `Sliders` ออกจาก `CourseListPage.tsx` เพื่อแก้ปัญหาการ build
- ไฟล์หลักที่แตะ: `iLearn.Admin.React/src/pages/master-data/LearnerGroupCategoryEditorPage.tsx`, `iLearn.Admin.React/src/pages/master-data/LearnerGroupCategoriesPage.tsx`, `iLearn.Admin.React/src/pages/courses/CourseListPage.tsx`
- Contract ที่เปลี่ยน (API shape / props / DB): เพิ่ม `divisionId` แบบ nullable ใน `LearnerGroupCategory` type ฝั่ง React
- Verified: `npm run lint` และ `npm run build` ผ่าน, `dotnet test` (118/118 tests passed)

## [2026-06-15 11:00] GitHub Copilot (GPT-5.3-Codex) — Implement PLAN-021 ทำให้ Learning Logs เป็น SuperAdmin-only สม่ำเสมอ (UI ตรงกับ API)
- ทำอะไร: ปรับ route `/learning-logs` ใน React ให้ครอบ `RequireRole superAdminOnly` และย้ายเมนู `Learning Logs` จาก section `Operations` ไป section `Super Admin` พร้อมตั้ง `superAdminOnly: true` เพื่อให้สิทธิ์ฝั่ง UI ตรงกับ policy ของ API (`LearningLogsCRUDController`)
- ไฟล์หลักที่แตะ: `iLearn.Admin.React/src/App.tsx`, `iLearn.Admin.React/src/config/navigation.ts`, `DOC/PLANS/PLAN-021-learning-logs-superadmin-consistency.md`, `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี (frontend access gating/navigation only)
- Verified: `npm run lint` ผ่าน (0 errors, 11 warnings baseline), `npm run build` ผ่าน, `dotnet build iLearn.Tests -o artifacts\verify-test` ผ่าน, `dotnet test artifacts\verify-test\iLearn.Tests.dll` ผ่าน (118/118)

## [2026-06-15 17:00] Claude Code — Audit division isolation + เอกสาร + PLAN-021/022
- ทำอะไร: ตรวจ division isolation ทั้งระบบ (entity ที่มี DivisionId, isolation รายโมดูล, พฤติกรรม LearnerGroup/Category) → เขียน `DOC/division_isolation_analysis.md` สรุป: security ไม่มีรั่วข้าม division (entity ที่มี division isolate ครบ, ที่ไม่มีกั้นด้วย SuperAdminOnly) เจอประเด็นควรเคลียร์ → ออก 2 แผน: **PLAN-021** (GPT, D1: Learning Logs API เป็น SuperAdminOnly แต่ UI route/menu ไม่ตรง → gate route + ย้ายเมนูไป Super Admin), **PLAN-022** (Gemini, D3: ให้ SuperAdmin ระบุ division ตอนสร้าง LearnerGroup/Category ได้ — เพิ่ม DivisionId ใน Create DTO + service ใช้เฉพาะเมื่อ IsSuperAdmin, division-admin ยัง auto ห้าม override + frontend แสดง selector เฉพาะ SuperAdmin)
- ไฟล์หลักที่แตะ: `DOC/division_isolation_analysis.md`, `DOC/PLANS/PLAN-021-*.md`, `DOC/PLANS/PLAN-022-*.md` (เอกสารล้วน)
- Contract ที่เปลี่ยน: ไม่มี (เป็นแผน — PLAN-022 จะเพิ่ม DivisionId ใน Create DTO ตอน implement)
- Verified: n/a (planner)

## [2026-06-15 16:00] Claude Code — Review PLAN-020 (Courses Explorer) + hotfix data bug → VERIFIED
- ทำอะไร: รีวิว CourseListPage ที่เขียนใหม่เป็น explorer (3-level Division→Category→Course) — โครงสร้าง/deep-link guard/breadcrumb/uncategorized/type-chip ทำถูกตาม pattern; **เจอ data bug:** แผนสั่ง `Courses?isActive=false` แต่ service คืนเฉพาะ Draft/Closed/Retired (isActive=true=Open only) → explorer ขาด course Open ทั้งหมด (ความผิดแผนเองที่ assume isActive=false=ทั้งหมด) **แก้:** ยิง 2 call (true+false) merge (disjoint, ไม่ dup) ใน `loadData`; เทส endpoint จริงทั้ง 5 = 200, build/lint ผ่าน → ปรับ VERIFIED
- ไฟล์หลักที่แตะ: `iLearn.Admin.React/src/pages/courses/CourseListPage.tsx` (hotfix), `DOC/PLANS/PLAN-020-*.md`
- Contract ที่เปลี่ยน: ไม่มี (ใช้ endpoint เดิม)
- Verified: endpoint จริง 5 ตัว = HTTP 200, `npm run build` ผ่าน, `npm run lint` 0 errors (11 warnings baseline)
- บทเรียน: ตรวจ semantics ของ service param (isActive) ก่อนใส่ในแผนเสมอ — ซ้ำรอย enrollments.status/roles.description ที่ assume แล้วพลาด

## [2026-06-15 15:35] Antigravity — Implement PLAN-020 เปลี่ยนหน้า Courses เป็น Explorer (Division → Category → Course) แบบ Learner Group
- ทำอะไร: เขียนหน้าจอ `CourseListPage.tsx` ใหม่ทั้งหมดตามรูปแบบ Explorer ใน `LearnerGroupListPage.tsx` โดยแสดงผลการจัดกลุ่มวิชาเรียนออกเป็น 3 ระดับตามโครงสร้างจริง (Root: Divisions Folder -> ใน Division: Categories Folder -> ใน Category: Courses Leaf Items); จัดการเคสวิชาที่ไม่มีหมวดหมู่ (Uncategorized) ให้แสดงผลเสมือนโฟลเดอร์ Uncategorized Division/Category อย่างสวยงาม; รองรับ deep-linking URL parameter validation (`divisionId`/`categoryId`), breadcrumb trail, client-side search, และ chip filter Course Types ที่ระดับ category; ตรวจสอบและแก้ไข eslint warnings/compiler errors สำเร็จ
- ไฟล์หลักที่แตะ: `iLearn.Admin.React/src/pages/courses/CourseListPage.tsx`, `DOC/PLANS/PLAN-020-courses-explorer.md`
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี (UI changes เท่านั้น)
- Verified: `npm run lint` และ `npm run build` ผ่านสำเร็จครบถ้วน

## [2026-06-15 15:20] Claude Code — เขียน PLAN-020 (Courses → Explorer แบบ Learner Group)
- ทำอะไร: ผู้ใช้ขอให้หน้า `/courses` เป็น Explorer แบบ Learner Group — สำรวจ CourseListPage เดิม (tree sidebar+grid), LearnerGroupListPage (ต้นแบบ explorer), entity (Category มี DivisionId ไม่มี ParentId → โครงสร้าง Division→Category→Course 2 ชั้น), backend (`GET api/Courses` โหลดทั้งหมด, `DivisionsCRUD/Get`, `CategoriesCRUD/Get`, course-types-lookup) → เขียน **PLAN-020** (Gemini): เขียน CourseListPage ใหม่เป็น explorer 3 ระดับ (root=divisions, ?divisionId=categories, ?categoryId=courses) ตาม pattern LearnerGroupListPage (URL deep-link, breadcrumb trail, drill-in/back, client-side search, deep-link guard) คงปุ่ม Create + chip Course Type — ไม่แตะ backend, ยังไม่สกัด shared Explorer (เป็น follow-up)
- ไฟล์หลักที่แตะ: `DOC/PLANS/PLAN-020-courses-explorer.md` (เอกสารล้วน)
- Contract ที่เปลี่ยน: ไม่มี
- Verified: n/a (planner ไม่แก้โค้ด)

## [2026-06-15 14:40] Claude Code — Hotfix regression จาก PLAN-018 (EF แปลง DTO-constructor ใน GroupBy ไม่ได้)
- ทำอะไร: ผู้ใช้รัน Dashboard แล้ว 500 — `InvalidOperationException: LINQ could not be translated` ที่ `DashboardController.GetOverview` categoryMix (บรรทัด ~165) เพราะ PLAN-018 เปลี่ยนเป็น `.Select(g => new DashboardCategoryMixPointDto(...))` **ในตัว EF query** (EF แปลง record constructor ใน GroupBy projection เป็น SQL ไม่ได้) — แก้: project เป็น anonymous ใน SQL แล้ว map เป็น DTO ใน memory หลัง `ToListAsync` (pattern มาตรฐานของ error นี้); audit ทั้ง controller แล้ว — จุดอื่น (courseAttention ใช้ taskRows in-memory, trends/stats จาก local list/CountAsync) ปลอดภัย จุดเดียวคือ categoryMix
- ไฟล์หลักที่แตะ: `iLearn.API/Controllers/DashboardController.cs`
- Contract ที่เปลี่ยน: ไม่มี (shape เดิม)
- Verified: `dotnet build` + `dotnet test` 118/118 ผ่าน — **หมายเหตุ:** ผู้ใช้ต้อง rebuild/restart API ใน VS ถึงจะเห็นผล (binary ที่รันอยู่เป็นตัวเก่า)
- บทเรียน: review PLAN-018 พลาดเพราะ `dotnet test` ไม่ได้ exercise dashboard query กับ SQL จริง (in-memory provider แปลง record constructor ได้ แต่ SQL Server แปลงไม่ได้) — ควรเพิ่ม integration test ที่ยิง dashboard กับ DB provider จริง หรือเทส query แบบ relational

## [2026-06-15 14:00] Claude Code — Review + ปิด PLAN-015..019 เป็น VERIFIED
- ทำอะไร: รีวิว diff ครบ 5 แผน — **015** (roles description ลบครบ column+editor), **016** (UsersCRUD enrich→DataSourceLoader บน enriched list, shape/isolation เดิม, fallback test in-memory), **017** (EnrollmentsController 624→491 + EnrollmentService 4 admin ops + DI, learner/HMAC คงใน controller, pure refactor), **018** (DashboardResponseDtos แทน anonymous, dashboardApi diff = comment-only), **019** (gitignore negation — check-ignore ยืนยัน source ไม่หลุด/vendored ยัง ignore) — ปรับทั้งหมดเป็น VERIFIED
- ไฟล์หลักที่แตะ: `DOC/PLANS/PLAN-015..019-*.md` (สถานะ — ไม่แตะโค้ด)
- Contract ที่เปลี่ยน: ไม่มี (โค้ดเปลี่ยนโดย implementer; response shape ทั้งหมดคงเดิม)
- Verified: รันเอง `dotnet test` 118/118 ผ่าน, `npm run build` ผ่าน, `npm run lint` 0 errors (11 warnings baseline), grep/check-ignore ผ่าน

## [2026-06-15 09:41] GitHub Copilot (GPT-5.3-Codex) — Implement PLAN-019 ปรับ .gitignore กัน wwwroot ignore กลืนไฟล์ source ที่ track
- ทำอะไร: เพิ่ม negation rules ใน `.gitignore` ใต้ `iLearn.Admin/wwwroot/**` เพื่อ re-include เฉพาะไฟล์ source ที่เขียนมือและ track อยู่เดิม 7 ไฟล์ (`css/admin-minimal.css`, `css/admin-tokens.css`, `css/admin-wizard.css`, `css/site.css`, `js/admin-layout.js`, `js/admin-view-utils.js`, `js/site.js`) โดยคงการ ignore vendored/generated เดิม (เช่น `js/devextreme/**`) ไว้
- ไฟล์หลักที่แตะ: `.gitignore`, `DOC/PLANS/PLAN-019-gitignore-wwwroot-tracked-source.md`, `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี
- Verified: `git check-ignore --no-index iLearn.Admin/wwwroot/js/admin-view-utils.js` ไม่ถูก ignore, `git check-ignore --no-index iLearn.Admin/wwwroot/js/devextreme/dx.all.js` ยังถูก ignore, `git ls-files` ยืนยัน 7 ไฟล์ยัง track ครบ, `git status --short` ไม่พบ vendored untracked flood, `dotnet build iLearn.Tests -o artifacts\verify-test` ผ่าน, `dotnet test artifacts\verify-test\iLearn.Tests.dll` ผ่าน (118/118)

## [2026-06-15 09:40] Antigravity — Implement PLAN-017 Refactor logic จาก EnrollmentsController ลง EnrollmentService ใหม่ (pilot)
- ทำอะไร: สร้าง `IEnrollmentService` + implementation `EnrollmentService` ย้าย logic ฝั่ง Admin ได้แก่ `ResetStatus`, `GetById`, `UpdateCompletion`, และ `BulkAssign` (พร้อม private helpers เฉพาะฝั่ง admin) ลง Application layer; ลงทะเบียน service ใน `DependencyInjection.cs`; ปรับปรุง `EnrollmentsController.cs` ให้ใช้ `IEnrollmentService` สำหรับ admin actions โดยยังคง flow ของ user/player และ helper methods ที่เกี่ยวเนื่องกับการ resolve user/schedule identities เอาไว้; อัปเดต `EnrollmentsPlayerInfoTests.cs` ให้ instantiate Controller โดย inject `FakeEnrollmentService` สำเร็จ
- ไฟล์หลักที่แตะ: `iLearn.Application/Interfaces/Services/IEnrollmentService.cs`, `iLearn.Application/Services/EnrollmentService.cs`, `iLearn.API/Controllers/EnrollmentsController.cs`, `iLearn.Application/DependencyInjection.cs`, `iLearn.Tests/EnrollmentsPlayerInfoTests.cs`, `DOC/PLANS/PLAN-017-refactor-large-controllers-pilot.md`
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี (พฤติกรรม response shape, status code, side-effects เหมือนเดิมทุกประการ)
- Verified: `dotnet build` + `dotnet test` (118/118 tests passed) สำเร็จเรียบร้อย

## [2026-06-15 09:38] GitHub Copilot (GPT-5.3-Codex) — Implement PLAN-018 แปลง DashboardController เป็น typed DTO responses (pilot)
- ทำอะไร: เพิ่ม DTO records ใหม่สำหรับ Dashboard responses ที่ `iLearn.Application/DTOs/DashboardResponseDtos.cs` แล้วเปลี่ยน `DashboardController` จาก `Ok(new { ... })` เป็น typed DTO responses ครบ endpoint ใน scope (`Overview`, `Stats`, `EnrollmentTrends`, `LearningActivityTrends`, `MaintenanceStatus`, `RecentAdminActivities`) โดยคง field names/shape เดิมที่ frontend ใช้; เปลี่ยน helper ที่คืน `IEnumerable<object>` เป็น typed DTO collections; เพิ่มคอมเมนต์ `Mirrors` ฝั่ง React ใน `dashboardApi.ts` ให้ชี้ DTO ใหม่
- ไฟล์หลักที่แตะ: `iLearn.Application/DTOs/DashboardResponseDtos.cs`, `iLearn.API/Controllers/DashboardController.cs`, `iLearn.Admin.React/src/pages/dashboard/dashboardApi.ts`, `DOC/PLANS/PLAN-018-typed-response-dtos-pilot.md`, `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี breaking change ใน payload shape (เปลี่ยนจาก anonymous object เป็น typed DTO โดยรักษาชื่อ/โครงสร้าง field เดิม)
- Verified: `dotnet build iLearn.Tests -o artifacts\verify-test` ผ่าน, `dotnet test artifacts\verify-test\iLearn.Tests.dll` ผ่าน (118/118), `npm run lint` ผ่าน (0 errors, 11 warnings baseline), `npm run build` ผ่าน

## [2026-06-15 09:35] Antigravity — Implement PLAN-016 ทำให้ค้นหา Admin Users ด้วยชื่อ/แผนก/ตำแหน่งได้ (enrich-before-filter)
- ทำอะไร: ปรับปรุงการทำงานใน `UsersCRUDController.Get` ให้ดึงข้อมูลผู้ใช้ทั้งหมดและทำการ batch lookup เพื่อเติมข้อมูลพนักงานก่อน แล้วจึงนำผลลัพธ์มาทำ filter/paging/sorting ใน memory ด้วย `DataSourceLoader.Load` เพื่อเปิดความสามารถในการกรอง/จัดลำดับบนฟิลด์พนักงาน (FullName, Division, ฯลฯ); เพิ่ม fallback query provider check เพื่อรองรับ sync testing; ปรับปรุง `AdminUsersPage.tsx` ในฝั่ง frontend เพื่อขยาย `searchExpr` ให้รองรับ `fullName` และ `division`; เพิ่ม unit tests ครอบคลุมการ enrich, in-memory filter, และ division isolation ใน `UsersCRUDControllerTests.cs`
- ไฟล์หลักที่แตะ: `iLearn.API/Controllers/Base/UsersCRUDController.cs`, `iLearn.Admin.React/src/pages/users/AdminUsersPage.tsx`, `iLearn.Tests/UsersCRUDControllerTests.cs`, `DOC/PLANS/PLAN-016-admin-users-enriched-search.md`
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี ( shape และฟิลด์ของ response เหมือนเดิม)
- Verified: `npm run lint` ผ่าน, `npm run build` ผ่าน, `dotnet test` (118/118 passed) ผ่านทั้งหมดแล้ว

## [2026-06-15 09:32] GitHub Copilot (GPT-5.3-Codex) — Implement PLAN-015 ลบ Description ของ Roles จาก UI
- ทำอะไร: ลบฟิลด์ Description ของ roles ออกจาก UI ครบ 3 จุดตามแผน: ลบคอลัมน์ `description` ใน `masterDataRoles.columns`, ลบ textarea Description ในโหมดแก้ไขของ `MasterDataDetailPage`, และลบ `Fact` Description ในโหมด read-only; คง `searchExpr: ['name']` สำหรับ roles และไม่แตะ master-data type อื่น
- ไฟล์หลักที่แตะ: `iLearn.Admin.React/src/pages/moduleConfigs.ts`, `iLearn.Admin.React/src/pages/master-data/MasterDataDetailPage.tsx`, `DOC/PLANS/PLAN-015-remove-roles-description.md`, `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี (UI cleanup only; ไม่แตะ backend/entity)
- Verified: `npm run lint` ผ่าน (0 errors, 11 warnings baseline), `npm run build` ผ่าน, รัน `dotnet build iLearn.Tests -o artifacts\verify-test` + `dotnet test artifacts\verify-test\iLearn.Tests.dll` แล้วแต่ล้มเหลวจาก compile error นอก scope ที่ `iLearn.Tests/UsersCRUDControllerTests.cs` (CS0118: `DevExtreme.AspNet.Data.ResponseModel` namespace used like a type)

## [2026-06-15 13:00] Claude Code — เขียนแผน backlog improvement ครบชุด (PLAN-015..019)
- ทำอะไร: แปลง backlog ที่เหลือทั้งหมดเป็นแผน — **PLAN-015** (GPT, ลบ Description ของ roles ออกครบ 3 จุด: column + editor edit + read-only เพราะ Role entity ไม่มี field นี้), **PLAN-016** (Gemini, ทำ Admin Users ค้นด้วยชื่อ/แผนกได้ ด้วย enrich-before-filter ใน UsersCRUD.Get — Learners NID เป็น won't-fix เพราะ external filter ไม่ได้), **PLAN-017** (Gemini, pilot refactor EnrollmentsController → EnrollmentService + วางมาตรฐานวิธีให้ controller อื่นทำตาม), **PLAN-018** (GPT, pilot แทน anonymous-object → DTO record ที่ DashboardController), **PLAN-019** (GPT, แก้ .gitignore กัน `wwwroot/**` กลืน 7 ไฟล์ source เขียนมือของ admin เก่า)
- ไฟล์หลักที่แตะ: `DOC/PLANS/PLAN-015..019-*.md` (เอกสารล้วน)
- Contract ที่เปลี่ยน: ไม่มี
- Verified: n/a (planner ไม่แก้โค้ด)

## [2026-06-15 12:30] Claude Code — Review + ปิด PLAN-013/014 เป็น VERIFIED
- ทำอะไร: รีวิว diff จริง — **PLAN-013** (GPT): `FileStoragesCRUDController.cs` ถูกลบ, grep ทั้ง solution 0 refs, repository/entity ไม่ถูกแตะ; **PLAN-014** (Gemini): LearnerApiService inject ILogger + ลบ Console ครบ, group A (primary) propagate exception โดย GetLearnersDxGridAsync ใช้ GetAsync แยก 4xx→ArgumentException/5xx→HttpRequestException, group B (enrichment) degrade graceful + LogWarning, GlobalExceptionMiddleware เพิ่ม HttpRequestException→502, LearnersController ลบ null-check generic message, + test ใหม่ GlobalExceptionMiddlewareTests (502 ProblemDetails ใน Production) — ปรับทั้งคู่เป็น VERIFIED
- ไฟล์หลักที่แตะ: `DOC/PLANS/PLAN-013-*.md`, `DOC/PLANS/PLAN-014-*.md` (อัปเดตสถานะ — ไม่แตะโค้ด)
- Contract ที่เปลี่ยน: ไม่มี (โค้ดเปลี่ยนโดย implementer แล้ว — error path เปลี่ยนเป็น ProblemDetails/502 ตามแผน)
- Verified: รันเอง `dotnet build` + `dotnet test` = 116/116 ผ่าน

## [2026-06-15 12:00] Claude Code — Hotfix search error /enrollments + /master-data/roles (searchExpr ผิดจาก PLAN-011)
- ทำอะไร: ผู้ใช้แจ้ง error ช่อง search 2 หน้า — เทส endpoint จริง (status code): enrollments `status`=500, roles `description`=500 (learnerCode/name = 200) สาเหตุ: **Role ไม่มี property `Description`** (มีแค่ Name/RoleType) และ **Enrollment ไม่มี `Status`** (EnrollmentsCRUD projection custom) → filter ฟิลด์ที่ไม่มีจริง 500 (พลาดตอน PLAN-011 ที่ผม prescribe โดยไม่ได้เทส 2 ฟิลด์นี้) แก้ `moduleConfigs.ts`: enrollments → `['learnerCode']`, masterDataRoles → `['name']` แล้วเทสซ้ำทุกตาราง (categories/courseTypes/assignments/divisions/learningLogs/learners) = 200 หมด
- ไฟล์หลักที่แตะ: `iLearn.Admin.React/src/pages/moduleConfigs.ts`
- Contract ที่เปลี่ยน: ไม่มี (frontend search config)
- Verified: endpoint จริง (learnerCode=200, name=200, ทุก config อื่น=200), `npm run build` ผ่าน, `npm run lint` 0 errors (11 warnings baseline)
- ข้อสังเกตค้าง (ยังไม่แก้): roles grid มี **คอลัมน์ `description` ที่ map กับ property ที่ไม่มีบน Role** → แสดงค่าว่างเสมอ (คนละเรื่องกับ search — ควรออกแผนลบคอลัมน์/เพิ่มฟิลด์ทีหลัง)

## [2026-06-15 09:10] Antigravity — Implement PLAN-014 ปรับปรุงการจัดการ Exception ใน LearnerApiService และ GlobalExceptionMiddleware
- ทำอะไร: Inject `ILogger` และนำการกลืน exception ออกในกลุ่ม primary fetch ของ `LearnerApiService` เพื่อให้ propagate ขึ้นไปหา `GlobalExceptionMiddleware`; ในกลุ่ม enrichment helper ปล่อยให้ fallback ว่างแต่แก้ไขเป็น LogWarning; ปรับปรุง `GetLearnersDxGridAsync` ให้โยน `ArgumentException` เมื่อเป็น 4xx client errors และโยน `HttpRequestException` เมื่อเป็น connection/5xx errors; ปรับปรุง `GlobalExceptionMiddleware` ให้แมป `HttpRequestException` เป็น `502 Bad Gateway` (Upstream employee service error); เพิ่ม unit test `GlobalExceptionMiddlewareTests.cs`
- ไฟล์หลักที่แตะ: `iLearn.Infrastructure/Services/LearnerApiService.cs`, `iLearn.API/Controllers/LearnersController.cs`, `iLearn.API/Middleware/GlobalExceptionMiddleware.cs`, `iLearn.Tests/GlobalExceptionMiddlewareTests.cs`, `DOC/PLANS/PLAN-014-learner-api-service-error-handling.md`
- Contract ที่เปลี่ยน (API shape / props / DB): คืน HTTP 502 ProblemDetails (Bad Gateway) เมื่อ upstream service ล้ม แทน 500
- Verified: `dotnet test` (116/116 passed) ผ่านการทดสอบทั้งหมดแล้ว

## [2026-06-15 09:06] GitHub Copilot (GPT-5.3-Codex) — Implement PLAN-013 ลบ FileStoragesCRUD endpoint ที่เสี่ยงดัมพ์ blob
- ทำอะไร: ลบ `FileStoragesCRUDController` ออกจาก `iLearn.API` เพื่อตัด endpoint `api/admin/FileStoragesCRUD/*` ที่ inherit generic CRUD และเสี่ยงโหลด/serialize `FileStorage.Data` (SCORM ZIP blob) แบบทั้งตาราง; ตรวจซ้ำว่า `IGenericRepository<FileStorage>` ยังใช้งานตามเดิมใน `ContentItemsController`, `ContentItemsCRUDController`, และ services ฝั่ง Application โดยไม่แตะ flow เสิร์ฟไฟล์จริง
- ไฟล์หลักที่แตะ: `iLearn.API/Controllers/Base/FileStoragesCRUDController.cs` (deleted), `DOC/PLANS/PLAN-013-remove-filestorages-crud-blob-exposure.md`, `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน (API shape / props / DB): เอา admin CRUD route สำหรับ FileStorage ออก (`api/admin/FileStoragesCRUD/*`); contract อื่นไม่เปลี่ยน
- Verified: `dotnet build iLearn.Tests -o artifacts\verify-test` ผ่าน, `dotnet test artifacts\verify-test\iLearn.Tests.dll` ผ่าน (115/115), ลบ `artifacts\verify-test` แล้ว

## [2026-06-15 11:20] Claude Code — เขียนแผนปรับปรุงจากผลวิเคราะห์ API (PLAN-013/014)
- ทำอะไร: แปลงข้อค้นพบใน api_analysis เป็นแผน 2 ใบ — **PLAN-013** (GPT, HIGH: ลบ `FileStoragesCRUDController` ที่ไม่มีใครใช้แต่ดัมพ์ SCORM blob ทั้งหมดผ่าน GetAllAsync — ยืนยัน grep client = 0), **PLAN-014** (Gemini, MED: `LearnerApiService` เลิกกลืน exception, ใช้ ILogger, แยก primary fetch (propagate→ProblemDetails/502) กับ enrichment helper (degrade graceful + log warning)) — ข้อ 5.3/5.4/5.5 (enrich-before-filter, refactor controller ใหญ่, anonymous→DTO) ยังไม่ออกแผน รอผู้ใช้เลือก
- ไฟล์หลักที่แตะ: `DOC/PLANS/PLAN-013-*.md`, `DOC/PLANS/PLAN-014-*.md` (เอกสารล้วน)
- Contract ที่เปลี่ยน: ไม่มี
- Verified: n/a (planner ไม่แก้โค้ด)

## [2026-06-15 11:00] Claude Code — วิเคราะห์ API ทั้งระบบ → DOC/api_analysis.md
- ทำอะไร: อ่านทั้ง backend (33 controllers, Program.cs, auth extensions, middleware, LearnerApiService, HMAC resolver) แล้วเขียนเอกสารวิเคราะห์รวม: สถาปัตยกรรม Clean Arch, โมเดล auth (policies + HMAC learner proxy), surface endpoint, pattern ที่ใช้ซ้ำ, และความเสี่ยงเรียงลำดับ — พบ landmine สำคัญ: **FileStoragesCRUDController ดัมพ์ SCORM blob ทั้งหมด** (HIGH), LearnerApiService กลืน exception (MED), DevExtreme filter ผูก projection (MED, กำลังแก้ PLAN-009/011/012), controller ใหญ่/anonymous-object response (LOW)
- ไฟล์หลักที่แตะ: `DOC/api_analysis.md` (เอกสารใหม่ — ไม่แตะโค้ด)
- Contract ที่เปลี่ยน: ไม่มี
- Verified: n/a (analysis ล้วน)

## [2026-06-15 10:30] Claude Code — Review รวบยอด: ปิด 9 แผนเป็น VERIFIED
- ทำอะไร: รีวิวงานค้างทั้งหมดแล้วปรับเป็น VERIFIED — PLAN-004 (Users wizard), PLAN-005 (LearnerGroupCategories wizard), PLAN-006 (Users detail+delete), PLAN-007 (shared detail components), PLAN-008 (detail migration), PLAN-009-refine (ลบ header/back, tabs), PLAN-010 (Reset→icon), PLAN-011 (search expr ทั้งระบบ), PLAN-012 (custom pages search). ตรวจด้วย: full build/lint/test เขียวหมด + grep acceptance (ใน src/pages: minmax 2-col grid=0, hand dt-fact=0, DetailPageHeader=0, ทั้ง 7 detail page ใช้ shared components) + spot-check MasterDataDetailPage (form ครอบ DetailLayout ถูก) + ยืนยัน createDataSource ลบ fallback title/code/name + searchExpr ทุกตัวตรงกับฟิลด์ที่เทส endpoint จริง=200 + ux_ui_analysis 2.4 ตรงกับ design จริง (action-only sidebar, ไม่มี header)
- ไฟล์หลักที่แตะ: `DOC/PLANS/PLAN-004..012*.md` (อัปเดตสถานะ — ไม่แตะโค้ด)
- Contract ที่เปลี่ยน: ไม่มี
- Verified: `npm run lint` (0 errors, 11 warnings baseline), `npm run build` ผ่าน, `dotnet test` 115/115 ผ่าน, grep checks ผ่าน

## [2026-06-15 08:40] Antigravity — Implement PLAN-011 แก้ search ตารางพังทั้งระบบและป้องกัน fallback
- ทำอะไร: กำหนด `searchExpr` ที่ใช้ฟิลด์สำหรับค้นหาได้จริงบน backend ใน config ของตารางทั้งหมด 8 ตาราง ได้แก่ `assignments`, `learningLogs`, `enrollments`, `masterDataDivisions`, `masterDataCategories`, `masterDataCourseTypes`, `masterDataRoles`, `masterData` และตัดการ fallback เป็น `['title', 'code', 'name']` ใน `createDataSource.ts` เมื่อไม่มี `searchExpr` เพื่อป้องกันการเกิด error ในฝั่ง backend EF Query
- ไฟล์หลักที่แตะ: `iLearn.Admin.React/src/pages/moduleConfigs.ts`, `iLearn.Admin.React/src/lib/createDataSource.ts`, `DOC/PLANS/PLAN-011-list-search-expr-fix.md`
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี
- Verified: `npm run lint` ผ่าน, `npm run build` ผ่าน, `dotnet test` (115/115 passed) ผ่านการทดสอบทั้งหมดแล้ว

## [2026-06-15 08:39] GitHub Copilot (GPT-5.3-Codex) — Implement PLAN-012 แก้ search หน้า Admin Users + ปิดแผนเป็น DONE
- ทำอะไร: แก้ `AdminUsersPage` ให้ search filter ฝั่ง server เหลือเฉพาะฟิลด์ที่ query ได้จริง (`nid`) โดยปรับ `searchExpr` จาก `['nid','fullName','division']` เป็น `['nid']`, ปรับ placeholder เป็น `Search by NID...`, และเติมคอมเมนต์อธิบายว่าฟิลด์ `fullName`/`division` ถูก enrich หลัง paging จึง filter server-side ไม่ได้; ตรวจ `CourseListPage` แล้ว `searchExpr={['title','code']}` ถูกต้องตาม property ของ Course จึงไม่ต้องแก้; อัปเดต `PLAN-012` เป็น DONE พร้อม Implementer Notes
- ไฟล์หลักที่แตะ: `iLearn.Admin.React/src/pages/users/AdminUsersPage.tsx`, `DOC/PLANS/PLAN-012-custom-pages-search-fix.md`, `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี
- Verified: `npm run lint` ผ่าน (0 errors, 11 warnings baseline), `npm run build` ผ่าน, `dotnet build iLearn.Tests -o artifacts\verify-test` ผ่าน, `dotnet test artifacts\verify-test\iLearn.Tests.dll` ผ่าน (115/115)

## [2026-06-15 10:00] Claude Code — ตรวจ search ทุกตาราง (เจอพังทั้งระบบ) + เขียน PLAN-011/012
- ทำอะไร: ต่อยอดจากบั๊ก Learners — ตรวจทุกตารางว่า search พังเหมือนกันไหม พบว่า `createDataSource.ts` มี fallback searchExpr `['title','code','name']` เมื่อ config ไม่ตั้ง searchExpr → DevExtreme filter บนฟิลด์ที่ไม่มีบน entity → throw ทั้งก้อน → ตารางที่ผ่าน createAdminDataSource ทั้งหมด search พัง ทดสอบยิง endpoint จริง (ดูแค่ HTTP status): Divisions default(title/code/name)=พัง, name-only=200; assignments `assignmentNo/description`=200, enrollments `learnerCode`=200, learningLogs `status`=200 — เขียน 2 แผน: **PLAN-011** (Gemini: เติม searchExpr ทุก config ใน moduleConfigs + กัน fallback อันตรายใน createDataSource), **PLAN-012** (GPT: AdminUsersPage searchExpr `fullName`/`division` filter ไม่ได้→เหลือ `nid`, verify CourseListPage) — contentLibrary (createRestDataSource param search) + LearnerGroupListPage (client-side filter) ไม่กระทบ
- ไฟล์หลักที่แตะ: `DOC/PLANS/PLAN-011-list-search-expr-fix.md`, `DOC/PLANS/PLAN-012-custom-pages-search-fix.md` (เอกสารล้วน)
- Contract ที่เปลี่ยน: ไม่มี
- Verified: n/a (planner; ทดสอบ endpoint จริงเพื่อยืนยันฟิลด์ที่ filter ได้ — ดูแค่ status code ไม่อ่าน PII)

## [2026-06-15 09:30] Claude Code — Hotfix บั๊ก Search Learners (root cause จริง = ฟิลด์ NID) + ปิด PLAN-009 VERIFIED
- ทำอะไร: ผู้ใช้แจ้งว่าแก้ casing (PLAN-009 ของ Gemini) แล้ว search ยัง 500 — ผู้ใช้อนุญาตให้ Claude ลงมือแก้+เทสเอง ทดสอบผ่าน API ตัวเองที่ localhost:7128 (ดูเฉพาะ HTTP status ไม่อ่าน PII): filter ที่มี `nid`=500, ไม่มี `nid`=200, `nid` อย่างเดียว=500 → สรุป external `/api/Student` filter ฟิลด์ NID ไม่ได้ (ตรงกับ grid ระบบเก่าที่ไม่เคยมี NID) — งาน casing ของ Gemini ถูกต้อง/จำเป็น (englishFirstName/eId ต้อง PascalCase ปลายทางถึงรับ) คงไว้; **Hotfix:** เอา `'nid'` ออกจาก `searchExpr` ของ learners ใน `moduleConfigs.ts` (คงคอลัมน์ NID ที่ external คืนค่ามาจริง) — ทดสอบ filter จริงผ่าน API = HTTP 200
- ไฟล์หลักที่แตะ: `iLearn.Admin.React/src/pages/moduleConfigs.ts`, `DOC/PLANS/PLAN-009-learners-search-field-casing.md`
- Contract ที่เปลี่ยน: ไม่มี (frontend search config เท่านั้น)
- Verified: เทส endpoint จริง (filter 3 ฟิลด์ = HTTP 200, baseline 200), `npm run lint` (0 errors, 11 warnings baseline), `npm run build` ผ่าน

## [2026-06-15 08:12] Antigravity — Implement PLAN-009 แปลง Filter Casing ของ Learners ตาราง Search
- ทำอะไร: แก้ไขบั๊กการค้นหาในตาราง Learners โดยการเพิ่มขั้นตอนการแปลงฟิลด์ filter จาก camelCase ไปเป็น PascalCase ด้วย Regex ใน `LearnersController.cs` ก่อนที่จะ proxy query string ไปยัง external employee API และสร้าง unit tests ใหม่ใน `LearnersControllerTests.cs`
- ไฟล์หลักที่แตะ: `iLearn.API/Controllers/LearnersController.cs`, `iLearn.Tests/LearnersControllerTests.cs`, `DOC/PLANS/PLAN-009-learners-search-field-casing.md`
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี (ทำงานเข้ากับ API contract เดิมทั้งหมด)
- Verified: `dotnet test` (115/115 passed) ผ่านการทดสอบทั้งหมดแล้ว

## [2026-06-12 19:10] Claude Code — วินิจฉัยบั๊ก Search ตาราง Learners + เขียน PLAN-009
- ทำอะไร: ผู้ใช้รายงาน search ตาราง `/learners` พัง ขึ้น "Failed to connect to the employee data source" — วินิจฉัยจากโค้ด: frontend ส่ง filter field camelCase (`nid`/`englishFirstName`/`englishLastName`/`eId`) แต่ external employee DevExtreme API ผูก property PascalCase (ยืนยันจาก `LearnerGridRowDto` PascalCase + `InjectDivisionFilter` ฉีด `"Division"` PascalCase) → `LearnersController.Get()` proxy ส่ง query string ดิบ → external throw → service catch คืน null → error กำกวม เขียน **PLAN-009** (Assigned: Gemini): แก้ backend แปลงชื่อฟิลด์ใน filter camelCase→PascalCase ก่อน forward (ระวังไม่แทนค่าค้นหาที่ผู้ใช้พิมพ์)
- ไฟล์หลักที่แตะ: `DOC/PLANS/PLAN-009-learners-search-field-casing.md` (เอกสารล้วน)
- Contract ที่เปลี่ยน: ไม่มี
- Verified: n/a (planner; ยิง external HR API ตรงถูกบล็อกด้วยเหตุผล PII — วินิจฉัยจากโค้ด)

## [2026-06-12 19:00] GitHub Copilot (GPT-5.3-Codex) — Implement PLAN-010 ปรับปุ่ม Reset ใน Assignment Learners เป็นไอคอน
- ทำอะไร: แก้ `AssignmentDetailPage.tsx` ให้ปุ่ม Reset ในตาราง Learners เปลี่ยนจาก text button เป็น icon button (`RotateCcw`) และใช้สไตล์เดียวกับ action icon อื่น (`p-1 text-slate-400 hover:text-indigo-600 rounded transition cursor-pointer`) เพื่อให้สมดุลกับปุ่มถังขยะ; ไม่แตะ logic ของการ reset/API
- ไฟล์หลักที่แตะ: `iLearn.Admin.React/src/pages/assignments/AssignmentDetailPage.tsx`, `DOC/PLANS/PLAN-010-refine-actions-to-icons.md`, `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี
- Verified: `npm run lint` ผ่าน (0 errors, 11 warnings baseline), `npm run build` ผ่าน

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
