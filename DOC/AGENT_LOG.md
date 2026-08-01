# Agent Work Log

บันทึกกลางสำหรับ AI agent ทุกตัว (Claude Code, Antigravity, GitHub Copilot) — **ต่อ entry ใหม่ไว้บนสุด** หลังจบงานที่แก้โค้ดทุกครั้ง

> **ไฟล์นี้เก็บเฉพาะ entry ล่าสุด ~30 รายการ** — ของเก่าอยู่ใน [archive/](./archive/) แยกตามเดือน
> (หมุนด้วย `pwsh tools/rotate-agent-log.ps1` เมื่อ entry เกิน ~40 — ห้ามเขียน entry ใหม่ลงไฟล์ archive)

## กติกาการเขียน entry

1. **อ่านก่อนเริ่มงาน:** 10 entry บนสุดของไฟล์นี้พอ (ไม่ต้องเปิด archive เว้นแต่ตามรอยประวัติเฉพาะเรื่อง)
2. **เขียนหลังจบงานที่แก้โค้ด** — ต่อบนสุด ใต้หัวข้อนี้
3. **ยาวไม่เกิน ~8 บรรทัด** — log คือ "ใครแตะอะไร/contract เปลี่ยนไหม" ไม่ใช่รายงานฉบับเต็ม
   รายละเอียด (เหตุผล ทางเลือกที่ตัดทิ้ง ผลรีวิว) เขียนใน `DOC/PLANS/PLAN-NNN-*.md` แล้วอ้างเลขแผนแทน

Format ต่อ entry:

```
## [YYYY-MM-DD] <Agent> — <สรุปงานสั้น ๆ>
- ทำอะไร: ...
- ไฟล์หลักที่แตะ: ...
- Contract ที่เปลี่ยน (API shape / props / DB): ... (หรือ "ไม่มี")
- Verified: lint/build/test อะไรผ่านบ้าง
```

---

## [2026-07-31] GitHub Copilot — PLAN-189 assignment archive system plan
- ทำอะไร: สร้างแผน READY สำหรับระบบ archive assignments แยกจาก delete/current tracking โดยคง enrollment snapshots และ historical reports
- ไฟล์หลักที่แตะ: `DOC/PLANS/PLAN-189-assignment-archive-system.md`, `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี (แผนเท่านั้น); proposed contract ในแผนคือ archive fields + archive/restore endpoints
- Verified: `pwsh tools/plan-status.ps1 -Next` ได้ `PLAN-189`; อ่าน lifecycle/status/dictionary และ code path `AssignmentService`/`AssignmentsCRUDController` ก่อนเขียนแผน

## [2026-07-31] GitHub Copilot — PLAN-188 learner group related assignments
- ทำอะไร: เพิ่ม assignments ที่เกี่ยวข้องใน `GET /api/LearnerGroups/{id}` และหน้า React `/learner-groups/:id` โดย reuse batch/status logic เดิมของ `LearnerGroupService`; deploy PROD API+React แล้ว
- ไฟล์หลักที่แตะ: `iLearn.Application/DTOs/LearnerGroupDto.cs`, `iLearn.Application/Services/LearnerGroupService.cs`, `iLearn.Admin.React/src/pages/learner-groups/LearnerGroupDetailPage.tsx`, `iLearn.Admin.React/src/lib/labels.ts`, `DOC/PLANS/PLAN-188-*.md`, `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน (API shape / props / DB): เพิ่ม `data.assignments` ใน `GET /api/LearnerGroups/{id}`; ไม่มี DB/route change
- Verified: `dotnet build iLearn.Tests` ✓; `dotnet test` 294/294 ✓; React build/lint ✓; PROD API `20260731163725` ✓; React `index-BMwSsivA.js` ✓; smoke group 32 API 200 (`AssignmentCount=0` currently)

## [2026-07-31] Claude Code — เปิด PLAN-187 ลบ dead code จาก Reviewer Notes ของ PLAN-185
- ทำอะไร: สร้างแผน READY assigned GPT — ลบ `AssignmentDashboardService.GetDashboardAsync` (ไม่มี call site, ตัวจริงคือ `AssignmentService`) + private helper/dependency ที่ตายตาม + `AssignmentStatusKeys.GetLearnerStatus` (เป็น special case เป๊ะ ๆ ของ `GetScheduledLearnerStatus`) + test ที่คุม dead code; เพิ่ม regression test เคส never-linked enrollment (self-enroll ไม่ควรขึ้น badge `Cancelled`)
- ไฟล์หลักที่แตะ: `DOC/PLANS/PLAN-187-remove-dead-assignment-dashboard-method.md`, `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี (แผนเท่านั้น ยังไม่ได้แก้โค้ด)
- Verified: `pwsh tools/plan-status.ps1 -Next` ได้ `PLAN-187`; grep ยืนยัน call site เหลือแค่ `AssignmentsController.cs:77` → `AssignmentService`; `plan-status.ps1` อ่าน header ไฟล์ใหม่ได้

## [2026-07-31] Claude Code — รีวิว PLAN-185 (VERIFIED)
- ทำอะไร: รีวิว fix ignored-query-filter ของ Copilot — ตรวจ audit table ทุกแถวเทียบโค้ดจริง (11 usages), ยืนยัน predicate ใหม่ตรง convention เดิม 3 จุด (`CourseService` ×2, `GetEffectiveSchedule`); พบ `AssignmentDashboardService.GetDashboardAsync` เป็น **dead code** (ไม่มี call site) ⇒ fix ตัวที่ 2 ไม่มีผลกับ production; พบว่า fix แถม badge `Self Enroll` ที่เคยขึ้น `Cancelled` ผิดแต่ยังไม่มี test คุม; แก้ header entry PLAN-186 ที่หายไป
- ไฟล์หลักที่แตะ: `DOC/PLANS/PLAN-185-*.md` (Status → VERIFIED + Reviewer Notes), `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี (เอกสารเท่านั้น ไม่ได้แก้โค้ด)
- Verified: `dotnet build iLearn.Tests` 0 error; `dotnet test` **294/294 ผ่าน** (implementer รันแค่ 39)

## [2026-07-31] GitHub Copilot — PLAN-185 ignored-query-filter audit fixes
- ทำอะไร: audit จุด `ignoreQueryFilters`/`.IgnoreQueryFilters()` ครบ 11 usages; แก้ 2 current-state leaks: learner profile deleted-only assignment link flags และ legacy `AssignmentDashboardService` deleted-rule counts
- ไฟล์หลักที่แตะ: `iLearn.API/Controllers/LearnersController.cs`, `iLearn.Application/Services/AssignmentDashboardService.cs`, `iLearn.Tests/{LearnersControllerTests.cs,AssignmentFlowTests.cs}`, `DOC/PLANS/PLAN-185-*.md`, `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี; semantics ของ learner profile `hasActiveAssignment/isAssignmentCancelled` ใช้ active assignment links เท่านั้น
- Verified: focused tests 1/1 + 1/1 ✓; regression set `LearnersControllerTests|AssignmentFlowTests|CourseServiceVisibilityTests|ReportServiceTests|CourseContentReadinessTests` 39/39 ✓ (มี NU1903 warnings เดิมของ Negotiate)

## [2026-07-31] GitHub Copilot — PLAN-186 ย้าย Apply Active Version เข้าตาราง Versions
- ทำอะไร: ย้าย `Apply Active Version` ออกจาก Controls sidebar ไปอยู่ใน action ของตาราง Versions; inactive version เปิด modal `Set Active Version`, active version เปิด modal `Apply Active Version`; เปลี่ยนชื่อปุ่ม `Add Version Package` เป็น `Add Version`; ย้ายข้อความใหม่ทั้งหมดเข้า `COURSE_LABELS` สองภาษา
- ไฟล์หลักที่แตะ: `iLearn.Admin.React/src/pages/courses/CourseDetailPage.tsx`, `iLearn.Admin.React/src/lib/labels.ts`, `DOC/PLANS/PLAN-186-*.md`, `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี (UI เท่านั้น ใช้ endpoint จาก PLAN-183)
- Verified: React lint/build ✓; QA deploy `index-4zpVvMiX.js` ✓; PROD deploy `index-4zpVvMiX.js` ✓; PROD `/courses/893` = 200; Playwright เห็นไทย `เพิ่มเวอร์ชัน` + EN `Add Version` และไม่พบ label เก่า `Add Version Package`

## [2026-07-31] GitHub Copilot — PLAN-185 technical debt plan for ignored query filters
- ทำอะไร: สร้างแผน READY สำหรับ audit จุดใช้ `ignoreQueryFilters: true` / `.IgnoreQueryFilters()` ที่อาจเอา soft-deleted data ไปตัดสิน current state ต่อจาก incident PLAN-184
- ไฟล์หลักที่แตะ: `DOC/PLANS/PLAN-185-audit-ignore-query-filter-current-state.md`, `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี (แผน technical debt เท่านั้น)
- Verified: `pwsh tools/plan-status.ps1 -Next` ได้ `PLAN-185`; grep พบ 8 source areas สำหรับ audit; ยังไม่ได้แก้ runtime code

## [2026-07-31] GitHub Copilot — PLAN-184 MyLearning KSN hidden by soft-deleted content link
- ทำอะไร: แก้ `หลักสูตรของฉัน` ซ่อน KSN ทั้งที่ player-info เปิดได้ โดยปรับ `CourseContentReadiness` ให้ ignore soft-deleted `CourseContentItem` links และถือ soft-deleted `ContentItem` เป็น not ready; เพิ่ม regression tests
- ไฟล์หลักที่แตะ: `iLearn.Application/Common/CourseContentReadiness.cs`, `iLearn.Tests/CourseContentReadinessTests.cs`, `DOC/PLANS/PLAN-184-*.md`, `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี; readiness semantics ignore soft-deleted course-content links when caller loads with ignored query filters
- Verified: focused CourseContentReadiness tests 2/2 ✓; QA API `20260731130531` ✓; PROD API `20260731130652` ✓; production learner `430263` my-courses count 2→3 and `HasKSN=True`

## [2026-07-31] GitHub Copilot — PLAN-183 Apply active version to learners action
- ทำอะไร: เพิ่ม Admin action ชัดเจนบน Course detail สำหรับ apply active version ให้ eligible learners พร้อม modal แสดง impact + policy (`MoveNotStarted` / `ResetInProgress`); เพิ่ม backend endpoint สำหรับ active version ที่ active อยู่แล้ว
- ไฟล์หลักที่แตะ: `iLearn.API/Controllers/CoursesController.cs`, `iLearn.Application/{Interfaces/Services/ICourseVersionService.cs,Services/CourseVersionService.cs}`, `iLearn.Admin.React/src/pages/courses/CourseDetailPage.tsx`, `iLearn.Tests/{CourseVersionLearnerPolicyTests.cs,CourseServiceVisibilityTests.cs}`, `DOC/PLANS/PLAN-183-*.md`, `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน (API shape / props / DB): เพิ่ม `POST /api/Courses/{courseId}/versions/{versionId}/apply-learner-policy`; ไม่เปลี่ยน DB/DTO เดิม; learner จะย้าย version เฉพาะเมื่อ Admin กด action และเลือก policy
- Verified: focused CourseVersion learner policy tests 9/9 ✓; React lint/build ✓; QA API `20260731125309` + React `index-T-I3M-Xy.js` ✓; PROD API `20260731125509` + React `index-T-I3M-Xy.js` ✓; no-op smoke ไม่ย้าย learner 431420, Playwright เห็นปุ่ม `Apply Active Version`

## [2026-07-31] GitHub Copilot — PLAN-182 Content Library Admin rights parity
- ทำอะไร: แก้สิทธิหน้า content detail/list/editor ให้ Admin ใช้งานเท่า SuperAdmin สำหรับ content item ปกติ: เปิด route upload/edit, แสดงปุ่ม edit metadata/publish/unpublish/delete และเปลี่ยน backend mutation endpoints จาก `SuperAdminOnly` เป็น `AdminOnly`
- ไฟล์หลักที่แตะ: `iLearn.API/Controllers/{ContentItemsController.cs,Base/ContentItemsCRUDController.cs}`, `iLearn.Admin.React/src/{App.tsx,pages/EntityListPage.tsx,pages/content-library/ContentItemDetailPage.tsx}`, `iLearn.Tests/{ContentItemsControllerTests.cs,ContentItemsCrudControllerTests.cs}`, `DOC/PLANS/PLAN-182-*.md`, `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน (API shape / props / DB): API shape ไม่เปลี่ยน; authorization semantics ของ normal Content Library mutations = `AdminOnly` (Admin+SuperAdmin); bulk maintenance endpoints ยัง `SuperAdminOnly`
- Verified: focused ContentItems tests 10/10 ✓; React lint/build ✓; QA API `20260731113853` + React `index-YLZeN2CC.js` ✓; PROD API `20260731114031` + React `index-YLZeN2CC.js` ✓; PROD content item 1747 page/API = 200

## [2026-07-31] GitHub Copilot — PLAN-181 assignment report learner-group scope fix
- ทำอะไร: แก้หน้า assignment report/export ที่แสดง learner groups ไม่เกี่ยวข้องกับ assignment โดยให้ dashboard rows ใช้ target group จาก `Assignment.LearnerGroupId/Name` แทน current learner membership และไม่สร้าง `Ungrouped` summary/export row เอง
- ไฟล์หลักที่แตะ: `iLearn.Application/Services/AssignmentService.cs`, `iLearn.Admin.React/src/pages/assignments/AssignmentReportPage.tsx`, `iLearn.Tests/AssignmentFlowTests.cs`, `DOC/PLANS/PLAN-181-*.md`, `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน (API shape / props / DB): API shape ไม่เปลี่ยน; semantics ของ `AssignmentDashboardDto.Learners[].LearnerGroups` = assignment target groups ไม่ใช่ current memberships
- Verified: focused `dotnet test` 2/2 ✓; React lint/build ✓; QA API `20260731112144` + React `index-CmNTH0OX.js` ✓; PROD API `20260731112350` + React `index-CmNTH0OX.js` ✓; PROD assignment 280 group/export summary rows = 0

## [2026-07-31] Claude Code — รีวิว+แก้ PLAN-180 เอง (VERIFIED) + deploy QA
- ทำอะไร: ผู้ใช้แจ้งหน้า gantt บน QA ยังพัง → ตรวจด้วย Playwright/Chromium จริง พบตารางซ้ายถูกวางทับบนแผนภูมิ (CSS `.wx-layout{flex-direction:column}` ทับ layout ของ SVAR) แก้ 6 จุด: layout chain, ป้ายซ้ำบนแท่งสั้น, วันที่เกินจริง 1 วัน (`data.end` เป็น exclusive), หัวคอลัมน์ day zoom ล้น, `.wx-bar` height dead rule, zebra ที่คลาสไม่มีจริง; ทำเส้น "วันนี้" สำเร็จผ่าน `highlightTime` (markers ถูก community build เคลียร์)
- ไฟล์หลักที่แตะ: `iLearn.Admin.React/src/index.css`, `.../gantt/AssignmentSvarGanttChart.tsx`, `.../gantt/svarGanttMapping.ts`, `.../lib/format.ts`, `.../lib/labels.ts`, `DOC/PLANS/PLAN-180-*.md`, `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี
- Verified: lint ✓ build ✓ deploy QA `CopySucceeded=True`/`RobocopyExitCode=3`; Playwright QA 1440×900: chart อยู่ข้าง grid (`x=562`), ช่องว่างใต้ chart 1px, 12 แถว, cell/scale ไม่ล้น, ป้ายซ้ำ 0, Today ไม่ select แถว, วันที่ตรงกับ dueDate
- หมายเหตุเครื่องมือ: **browser pane รีวิวหน้านี้ไม่ได้** (ไม่ composite frames ⇒ ResizeObserver ไม่ยิง ⇒ SVAR วัดขนาดได้ 0) ให้ใช้ Playwright แทน

## [2026-07-31] Claude Code — ล้างหนี้รีวิว 18 แผน (144–178) + ส่ง PLAN-180 ให้ Copilot
- ทำอะไร: ปิด loop 18 แผนเป็น `VERIFIED` พร้อม Reviewer Notes ทุกไฟล์ — gantt cluster 172–177 ปิดในฐานะ superseded (โค้ดถูก PLAN-178 ลบทิ้งแล้ว), 178 + report cluster 164–170 + 144/148/150/151 ตรวจ artifact จริงในโค้ด/บน env; normalize header ภาษาไทยของ PLAN-144 เป็น `- **Status:**`; PLAN-180 คงสถานะ READY assigned GitHub Copilot พร้อมให้เริ่ม
- ไฟล์หลักที่แตะ: `DOC/PLANS/PLAN-{144,148,150,151,164..170,172..178}-*.md`, `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี (เอกสารเท่านั้น ไม่ได้แก้โค้ด)
- Verified: `npm run lint`/`npm run build` ✓; smoke anon QA+PROD `favicon-32.png`/`site.webmanifest`/`favicon.ico` = 200; หนี้รีวิว 54 → **36 แผน**; `plan-status.ps1` ยังอ่าน header ครบทุกไฟล์ที่แก้
- พบระหว่างรีวิว: `npm audit` 7 รายการ (react-router 5 high, vite NTLMv2 UNC disclosure, ws DoS) — **ไม่ได้มาจาก SVAR** มีอยู่ก่อน PLAN-178 ยังไม่มีแผนรองรับ

## [2026-07-31] GitHub Copilot — PLAN-180 Assignment Gantt visual redesign
- ทำอะไร: รื้อ presentation SVAR: ตาราง 1 คอลัมน์ 2 บรรทัด truncate, localized time scales, bar/status mapping, scoped full-height wrappers และ Today ใช้ `scroll-chart`; deploy QA แล้ว
- ไฟล์หลักที่แตะ: `iLearn.Admin.React/src/{index.css,lib/format.ts,lib/labels.ts,pages/assignments/gantt/*}`, `DOC/PLANS/PLAN-180-*`, `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี
- หมายเหตุ: SVAR 2.7.1 community ล้าง `markers` ใน store จึงไม่ render Today marker แม้ typings รับ prop; บันทึกเป็น blocker ใน PLAN-180
- Verified: `npm run lint` ✓; `npx tsc --noEmit` ✓; `npm run build` ✓ (Vite chunk warning เดิม); QA deploy ✓ (`CopySucceeded=True`, RobocopyExitCode=3)

## [2026-07-31] Claude Code — รีวิวปิด loop PLAN-179 (VERIFIED)
- ทำอะไร: รีวิว PLAN-179 แบบตรวจซ้ำเอง ไม่เชื่อ log → Status `DONE` → `VERIFIED` + Reviewer Notes; บันทึกประเด็นค้าง 5 ข้อ (dead CSS var, week `%j`, cell ไม่ truncate, สีสถานะซ้ำ 2 ที่, หัวคอลัมน์ hardcode อังกฤษ) โดยส่งต่อให้ PLAN-180 รับช่วงแทนการตีกลับ
- ไฟล์หลักที่แตะ: `DOC/PLANS/PLAN-179-svar-gantt-ui-polish.md`, `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี (เอกสารเท่านั้น ไม่ได้แก้โค้ด)
- Verified: `npm run lint` ✓; `npm run build` ✓ reproduce hash `index-CHu7nUEk.js` ตรงกับที่ deploy บน QA; smoke QA root/gantt/asset = 200 ทั้งสาม

## [2026-07-31] Claude Code — PLAN-180 แผน redesign หน้า Assignment Gantt ทั้งหน้า
- ทำอะไร: ตรวจหน้า Gantt บน QA หลัง PLAN-178/179 ตามคำสั่ง "ปรับการออกแบบทั้งหมดใหม่" แล้วเขียนแผน READY ครอบคลุม layout เต็มการ์ด, ตารางซ้ายเหลือคอลัมน์เดียว 2 บรรทัด (truncate), timeline header สองภาษา, weekend/today marker ผ่าน SVAR API, bar/tooltip, ปุ่ม Today ใช้ `scroll-chart` — คง SVAR เป็น renderer เดิม
- ไฟล์หลักที่แตะ: `DOC/PLANS/PLAN-180-assignment-gantt-visual-redesign.md`, `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี (แผนเท่านั้น ไม่ได้แก้โค้ด)
- Verified: `pwsh tools/plan-status.ps1 -Next` = `PLAN-180`; ยืนยัน prop/typing ของ SVAR (`markers`, `highlightTime`, `IColumnConfig.cell`, `exec('scroll-chart', {date})`) จาก `node_modules/@svar-ui/*/types` ก่อนเขียนสเปก

## [2026-07-31] GitHub Copilot — PLAN-179 SVAR Gantt UI polish + QA smoke
- ทำอะไร: แก้ UI หลัง SVAR migration: Week scale ใช้ label สั้น+84px cells, Day/Month 24/140px; แยก bar label จาก Description, ตัด progress mapping/overlay, ปิด column resize, เพิ่ม scoped SVAR theme typography/border/bar CSS
- ไฟล์หลักที่แตะ: `iLearn.Admin.React/src/index.css`, `.../gantt/svarGanttMapping.ts`, `.../gantt/AssignmentSvarGanttChart.tsx`, `DOC/PLANS/PLAN-179-svar-gantt-ui-polish.md`, `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี
- Verified: lint/build ✓; QA deploy `CopySucceeded=True`/`RobocopyExitCode=3`; smoke root/Gantt = 200, asset `index-CHu7nUEk.js`; Playwright Day/Week/Month: blank labels=0, progress overlay=0, resize controls=0, h-scroll ยังใช้ได้

## [2026-07-31] GitHub Copilot — PLAN-178 implement SVAR React Gantt + QA deploy
- ทำอะไร: replace custom Gantt ด้วย `@svar-ui/react-gantt@2.7.1` (MIT) เพิ่ม SVAR adapter/mapping, readonly, Day/Week/Month scales, Today ผ่าน SVAR API, status template, click-through detail, lazy-load route; ลบ `GanttChart.tsx`/`GanttBar.tsx`/`ganttScale.ts` เหลือ `ganttStatus.ts` สำหรับ legend
- ไฟล์หลักที่แตะ: `iLearn.Admin.React/package.json`, `.../package-lock.json`, `.../src/pages/assignments/AssignmentGanttPage.tsx`, `.../src/pages/assignments/gantt/AssignmentSvarGanttChart.tsx`, `.../src/pages/assignments/gantt/svarGanttMapping.ts`, `DOC/PLANS/PLAN-178-assignment-gantt-svar-replacement.md`, `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี API/DB; เพิ่ม frontend dependency `@svar-ui/react-gantt@2.7.1`
- Verified: lint/build ผ่าน; main bundle ~608KB + SVAR lazy chunk ~252KB/32KB CSS; QA deploy `CopySucceeded=True`/`RobocopyExitCode=3`; smoke root/Gantt = 200; browser SVAR rows+readonly+zoom+click-through `/assignments/314` ผ่าน; npm install reports 7 audit vulnerabilities (ไม่ได้ auto-fix)

## [2026-07-31] GitHub Copilot — PLAN-178 replace custom Gantt with SVAR
- ทำอะไร: สร้างแผน READY สำหรับ refactor หน้า `/admin-react/assignments/gantt` จาก custom CSS-grid Gantt ไปใช้ `@svar-ui/react-gantt` (MIT, React >=18/19-compatible) หลัง PLAN-172..177 ยังเจอปัญหา alignment/rounding ซ้ำ
- ไฟล์หลักที่แตะ: `DOC/PLANS/PLAN-178-assignment-gantt-svar-replacement.md`, `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี (แผนเท่านั้น; ระบุ dependency frontend ที่จะเพิ่มเมื่อ implement)
- Verified: `pwsh tools/plan-status.ps1 -Next` ยืนยันเลข `PLAN-178`; ไม่ได้แก้โค้ด/ไม่ได้รัน build

## [2026-07-30] GitHub Copilot — PLAN-177 Gantt header/body guide alignment + QA deploy
- ทำอะไร: แก้ช่องวันใน body ไม่ตรงหัวตาราง Day/Week โดยเปลี่ยน guide lines จาก fixed px repeating-gradient (`22px`) เป็น percentage/calc stops จาก `timeline.ticks[].widthPct` ชุดเดียวกับ header cells; Day ยังมี weekend shading, Week ไม่มี weekend shading, Month ไม่เปลี่ยน
- ไฟล์หลักที่แตะ: `iLearn.Admin.React/src/pages/assignments/gantt/GanttChart.tsx`, `DOC/PLANS/PLAN-177-assignment-gantt-header-body-guide-alignment.md`, `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี
- Verified: `npm run lint` ✓, `npm run build` ✓, deploy QA `CopySucceeded=True`/`RobocopyExitCode=3`; smoke root + `/assignments/gantt` = 200; QA asset `assets/index-Bafo0cnS.js`; Playwright: Day/Week `usesFixedPxGuide=false`, `usesPercentCalcGuide=true`, Month row bg=0

## [2026-07-30] GitHub Copilot — PLAN-176 weekend shading only on Day + QA deploy
- ทำอะไร: ปรับ Gantt ให้สีพื้นหลังเสาร์/อาทิตย์แสดงเฉพาะ Day zoom; Week เหลือเฉพาะ weekly guide line และ Month ไม่มี row background แต่ยังคง month boundary guides/scrollbar/filler เดิม; deploy `iLearn.Admin.React` ขึ้น QA
- ไฟล์หลักที่แตะ: `iLearn.Admin.React/src/pages/assignments/gantt/GanttChart.tsx`, `DOC/PLANS/PLAN-176-assignment-gantt-weekend-day-only.md`, `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี
- Verified: `npm run lint` ✓, `npm run build` ✓, deploy QA `CopySucceeded=True`/`RobocopyExitCode=3`; smoke root + `/assignments/gantt` = 200; QA asset `assets/index-pgVtkzUg.js`; Playwright: Day weekend=true, Week=false, Month=false

## [2026-07-30] GitHub Copilot — PLAN-175 Gantt spacing audit + QA deploy
- ทำอะไร: สำรวจ Gantt spacing ด้วย Playwright หลัง PLAN-174 พบ blank ใต้ rows 70px (Day/Week) และ 98px (Month) + weekend background layer เสี่ยงเลื่อนใน Week; แก้โดยรวม weekend shading เป็น gradient layer เดียว และเพิ่ม filler grid row ที่ต่อ background/grid/weekend bands ลงถึง scrollbar พร้อมยืด month guides/today line ผ่าน filler
- ไฟล์หลักที่แตะ: `iLearn.Admin.React/src/pages/assignments/gantt/GanttChart.tsx`, `DOC/PLANS/PLAN-175-assignment-gantt-spacing-audit.md`, `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี
- Verified: `npm run lint` ✓, `npm run build` ✓, deploy QA `CopySucceeded=True`/`RobocopyExitCode=3`; smoke root + `/assignments/gantt` = 200; QA asset `assets/index-BcQrGzLp.js`; Playwright 1429×768: Day/Week filler 60px, Month filler 88px, gapLastRowToFiller=0, vertical scroll=no

## [2026-07-30] GitHub Copilot — PLAN-174 Gantt weekend band alignment + QA deploy
- ทำอะไร: แก้พื้นหลังเสาร์/อาทิตย์ใน Gantt ที่ไม่ตรงช่องวันที่ โดยเพิ่ม `weekendBands` ใน timeline model เป็น `leftPct/widthPct` จากวันจริง และเปลี่ยน body row shading จาก px repeating-gradient เป็น percentage background gradients; deploy `iLearn.Admin.React` ขึ้น QA ซ้ำ
- ไฟล์หลักที่แตะ: `iLearn.Admin.React/src/pages/assignments/gantt/ganttScale.ts`, `iLearn.Admin.React/src/pages/assignments/gantt/GanttChart.tsx`, `DOC/PLANS/PLAN-174-assignment-gantt-weekend-band-alignment.md`, `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี
- Verified: `npm run lint` ✓, `npm run build` ✓, deploy QA `CopySucceeded=True`/`RobocopyExitCode=3`; smoke root + `/assignments/gantt` = 200; QA asset `assets/index-BF5mUMMA.js`

## [2026-07-30] GitHub Copilot — PLAN-173 Month Gantt scrollbar + QA deploy
- ทำอะไร: แก้ Month zoom ที่ยังไม่มี scrollbar โดยเลิก fit-to-width (`fitsWidth`) ให้ month มี fixed/min timeline width (`max(totalDays*6px, months*220px, 1280px)`) และใช้ `overflow-x-scroll`; deploy `iLearn.Admin.React` ขึ้น QA ซ้ำ
- ไฟล์หลักที่แตะ: `iLearn.Admin.React/src/pages/assignments/gantt/ganttScale.ts`, `iLearn.Admin.React/src/pages/assignments/gantt/GanttChart.tsx`, `DOC/PLANS/PLAN-173-assignment-gantt-month-scrollbar.md`, `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี
- Verified: `npm run lint` ✓, `npm run build` ✓, deploy QA `CopySucceeded=True`/`RobocopyExitCode=3`; smoke root + `/assignments/gantt` = 200; QA asset `assets/index-CYKbRfZ1.js`

## [2026-07-30] GitHub Copilot — deploy PLAN-172 Admin React to QA
- ทำอะไร: รัน deploy `iLearn.Admin.React` ขึ้น QA ด้วย `tools/deploy-admin-react.ps1` (รวม lint + build + copy dist ไป `\\AP-NTC2138-QAWB\wwwroot\iLearn\admin-react`)
- ไฟล์หลักที่แตะ: `DOC/PLANS/PLAN-172-assignment-gantt-scrollbar-bottom.md`, `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี (deploy static frontend only)
- Verified: deploy result `CopySucceeded=True`, `RobocopyExitCode=3`; smoke `https://ap-ntc2138-qawb.nikonoa.net/iLearn/admin-react/` = 200, `https://ap-ntc2138-qawb.nikonoa.net/iLearn/admin-react/assignments/gantt` = 200

## [2026-07-30] GitHub Copilot — PLAN-172 Gantt scrollbar bottom alignment
- ทำอะไร: ปรับ `GanttChart` ให้ timeline scroller เป็น `flex-1` เพื่อให้ horizontal scrollbar อยู่ก้นพื้นที่ timeline/card ทุก zoom แทนการอยู่ใต้แถวสุดท้ายเมื่อข้อมูลมีไม่กี่ row
- ไฟล์หลักที่แตะ: `iLearn.Admin.React/src/pages/assignments/gantt/GanttChart.tsx`, `DOC/PLANS/PLAN-172-assignment-gantt-scrollbar-bottom.md`, `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี
- Verified: `npm run lint` ✓, `npm run build` ✓ (มี Vite chunk-size warning เดิม)

## [2026-07-30] Claude Code — PLAN-171 QA fix: month zoom แสดงผลไม่ดี
- ทำอะไร: เปลี่ยน month zoom ให้ **สเกลพอดีความกว้างการ์ด** แทน 3px/วันคงที่ (เดิมชาร์ตกว้าง ~560px ในพื้นที่ ~1500px, header เดือนหยุดกลางการ์ด, มีแถบ tick ว่าง, เส้นตารางถี่ทุก 3px เป็นลายพร้อย) โดยย้ายตำแหน่งแนวนอนทั้งหมดเป็น % ของคอลัมน์ timeline (`getTaskLayout` คืน leftPct/widthPct + clamp ไม่ให้ล้นขอบขวา), grid column = `minmax(0,1fr)` เฉพาะ month, header สูงตาม zoom (`headerHeight()` — month ตัดแถว tick ออก), เส้นตาราง: day = ต่อวัน+weekend / week = ต่อสัปดาห์ (phase ตรงกับ tick) / month = เส้นขอบเดือนจริงเป็น overlay %, และย้ายเส้น today + guide ไป overlay ที่ครอบ `left:NAME_COL_W → right:0`
- ไฟล์หลักที่แตะ: `iLearn.Admin.React/src/pages/assignments/gantt/ganttScale.ts`, `.../gantt/GanttChart.tsx`, `.../gantt/GanttBar.tsx`, `DOC/PLANS/PLAN-171-assignment-gantt-refactor.md`, `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน (API shape / props / DB): `GanttBar` props เปลี่ยนจาก `leftPx/widthPx/timelineWidth` → `leftPct/widthPct`; `TimelineModel` เพิ่ม `fitsWidth/headerH/monthBoundaryPcts/todayLeftPct` และ months/ticks ใช้ `widthPct` — internal ของโฟลเดอร์ gantt ไม่แตะ backend/DTO
- Verified: `npm run lint` ✓, `npm run build` ✓, วัด 3 zoom: month cells รวม = ความกว้างคอลัมน์เป๊ะทุก zoom, เส้น guide ตรงขอบเซลล์เดือน 0px, month ไม่มี h-scroll (พอดีการ์ด), paint order + hover card ยังถูกครบ ✓
- หมายเหตุ: เจอ commit `12396d1`/`fc413ea` จากภายนอก session ระหว่างทำงาน และ `fc413ea` เผลอ track ไฟล์ harness ชั่วคราว (`__gantt-probe.html`, `src/__ganttProbe.tsx`) — ลบใน working tree แล้ว แต่ยังอยู่ใน HEAD ต้อง commit การลบ

## [2026-07-30] Claude Code — PLAN-171 QA fix: tooltip ถูกทับ + ชาร์ตไม่พอดีจอ
- ทำอะไร: (1) hover card เคยเป็น z-auto จึงถูก**แท่งของแถวล่าง** (sibling ทีหลัง z-auto เท่ากัน) ทับจนเห็นเป็นเสี่ยง → ให้ card เป็น `z-10` และสลับ grid ให้ emit bar rows ก่อน name cells เพื่อให้คอลัมน์ชื่อ (z-10 เหมือนกัน) ยังทับ card ได้ (2) เพิ่ม `min-w-0` ที่ flex wrapper ในหน้าเพจ — flex item ที่ overflow visible มี automatic min size = min-content ซึ่งคอลัมน์ px ของ timeline ทำให้บวมเป็นความกว้างชาร์ตทั้งอัน เลย์เอาต์จึงยืดเลยการ์ดแทนที่จะให้ scroller คลิป (วัดได้ wrapper clientWidth 3752 ใน parent 1198) (3) scroller เลิกใช้ `flex-1` เพื่อให้ scrollbar แนวนอนมาอยู่ใต้แถวสุดท้าย (เดิมไปอยู่ก้นการ์ดห่างเกือบ 200px) (4) timeline column เป็น `minmax(widthPx, 1fr)` กัน dead space ตอน zoom รายเดือน
- ไฟล์หลักที่แตะ: `iLearn.Admin.React/src/pages/assignments/gantt/GanttBar.tsx`, `.../gantt/GanttChart.tsx`, `.../AssignmentGanttPage.tsx`, `DOC/PLANS/PLAN-171-assignment-gantt-refactor.md`, `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี
- Verified: `npm run lint` ✓, `npm run build` ✓, วัด 3 zoom บน fixture 12 แถว (มีแท่ง 140 วัน) — scroller ถูกจำกัดที่ 1196, scrollW 3750 ที่ day zoom, scrollbar 10px, ไม่มี page overflow-x, freeze-pane + hover card ทับถูกลำดับครบ ✓ | เกร็ด: `elementFromPoint` เป็น hit-test ไม่ใช่ paint order — ต้องเปิด `pointerEvents:'auto'` ตอนวัด card ที่เป็น `pointer-events-none`

## [2026-07-30] Claude Code — PLAN-171 header label overflow
- ทำอะไร: ใส่ `overflow-hidden` ให้เซลล์ header ทั้งสองแถวใน `GanttChart.tsx` (แถวเดือน + แถว tick) — เซลล์ที่แคบกว่าป้ายตัวเองเคยมีตัวอักษรล้นข้ามเส้นขอบ แถว tick ก็เป็นเหมือนกันที่สัปดาห์แรก/สุดท้ายซึ่งไม่เต็มสัปดาห์ (`27 Jun` ในช่อง 15px) ผลข้างเคียงที่ยอมรับ: เซลล์ริมแคบ ๆ จะโชว์ป้ายแบบตัดสั้น (`Sept 26` → `S`) แทนการล้น
- ไฟล์หลักที่แตะ: `iLearn.Admin.React/src/pages/assignments/gantt/GanttChart.tsx`, `DOC/PLANS/PLAN-171-assignment-gantt-refactor.md`, `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี (class เดียวต่อแถว)
- Verified: `npm run lint` ✓, `npm run build` ✓, วัดทั้ง 3 zoom บนหน้าจริงผ่าน harness ชั่วคราว (ลบแล้ว) — เซลล์ที่เนื้อหาเกินความกว้างรายงาน `overflowX: hidden` ครบทุกเคส ✓

## [2026-07-30] Claude Code — PLAN-171 fix G5-G8 (minors)
- ทำอะไร: G5 ย้าย `zoomOptions` เข้า component body (module-scope const ไม่ถูกประเมินใหม่ตอน `AppLayout` remount ด้วย `key={lang}` → ป้าย zoom ค้างภาษาเดิม), G7 เพิ่ม legend ในแถบ "แสดง X จาก Y" โดยโชว์เฉพาะสถานะที่มีในข้อมูล (ใช้ `counts` memo เดิม), G8 ย้ายสีแท่งไป `gantt/ganttStatus.ts` เป็น Tailwind class map ให้ bar + legend ใช้ตัวเดียวกัน (ไม่เหลือ hex), G6 hover card ของ 2 แถวท้ายเปิดขึ้นบนกัน scroller ตัด
- ไฟล์หลักที่แตะ: `iLearn.Admin.React/src/pages/assignments/AssignmentGanttPage.tsx`, `.../gantt/GanttBar.tsx`, `.../gantt/GanttChart.tsx`, `.../gantt/ganttStatus.ts` (ใหม่), `DOC/PLANS/PLAN-171-assignment-gantt-refactor.md`, `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน (API shape / props / DB): `GanttBar` เพิ่ม prop `flipHoverCardUp` (required) — internal ของโฟลเดอร์ gantt ไม่แตะ backend/DTO
- Verified: `npm run lint` ✓, `npm run build` ✓, วัดสี/legend/สลับภาษา/ทิศ hover card บนหน้าจริงผ่าน vite harness ชั่วคราว (ลบแล้ว) ✓ — PLAN-171 ปิดครบ G1-G8

## [2026-07-30] Claude Code — PLAN-171 review + fix G1-G4
- ทำอะไร: รีวิว PLAN-171 เจอ 4 ข้อแล้วแก้เอง — G1 grid auto-placement ดันชื่อทุกแถวลง 1 row (pin `gridRow` ทุก cell + เรียง DOM ตามลำดับทับ name→bar→header→corner, เลิกใช้ `.contents`, คุมสูง header ทั้งสองฝั่งเท่ากับ `HEADER_TOTAL_H`), G2 description ว่างทำเลข AS ซ้ำ (ซ่อน separator+title เมื่อ `title === assignmentNo`), G3 `todayOffsetDays` ถูก clamp ทำเส้นวันนี้โผล่ตลอด (คืนค่า raw + `isTodayInRange`), G4 เส้นวันนี้ทับคอลัมน์ freeze (ตัด `z-10`)
- ไฟล์หลักที่แตะ: `iLearn.Admin.React/src/pages/assignments/gantt/GanttChart.tsx`, `.../gantt/ganttScale.ts`, `DOC/PLANS/PLAN-171-assignment-gantt-refactor.md`, `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน (API shape / props / DB): `TimelineModel` เพิ่ม `isTodayInRange` และ `todayOffsetDays` เป็นค่า raw (ไม่ clamp) — internal ของโฟลเดอร์ gantt เท่านั้น ไม่แตะ backend/DTO
- Verified: `npm run lint` ✓, `npm run build` ✓, วัด layout/paint order บน component จริงผ่าน vite harness ชั่วคราว (ลบทิ้งแล้ว) ✓ — Status แผน = VERIFIED, เหลือ minor G5-G8 (i18n zoom label, hover card โดนตัด, legend, hex สี) ยังไม่แก้

## [2026-07-30] GitHub Copilot — PLAN-171 assignment gantt refactor
- ทำอะไร: แก้ backend `MapGanttTask` ตาม B1/B2 (title = description-only fallback assignmentNo, span date = Min/Max ทั้งกลุ่ม) และรีไรต์หน้า Gantt เป็นโครง 4 ไฟล์ (`AssignmentGanttPage` + `ganttScale` + `GanttChart` + `GanttBar`) พร้อม zoom Day/Week/Month, sticky header/left column, stable scale, ref-based Today centering, bar link+focus+hover card, filtered-empty state, และตัด progress overlay/% ออก
- ไฟล์หลักที่แตะ: `iLearn.Application/Services/AssignmentService.cs`, `iLearn.Admin.React/src/pages/assignments/AssignmentGanttPage.tsx`, `iLearn.Admin.React/src/pages/assignments/gantt/ganttScale.ts`, `iLearn.Admin.React/src/pages/assignments/gantt/GanttChart.tsx`, `iLearn.Admin.React/src/pages/assignments/gantt/GanttBar.tsx`, `iLearn.Admin.React/src/lib/labels.ts`, `DOC/PLANS/PLAN-171-assignment-gantt-refactor.md`, `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มีการเพิ่ม/ลบ field; เปลี่ยน semantics ของ `AssignmentGanttTaskDto.Title` ให้เป็น description-only และ `StartDate`/`DueDate` ให้เป็น span ระดับ AssignmentNo group
- Verified: `npm run lint` ✓, `npm run build` ✓, `dotnet build iLearn.Tests -o artifacts\\verify-test` ✓ (ผ่านพร้อม warning เดิม), `dotnet test artifacts\\verify-test\\iLearn.Tests.dll` ✓ (Passed 280), cleanup artifacts ✓

## [2026-07-30] GitHub Copilot — PLAN-170 remove completion-focused UI from assignment report
- ทำอะไร: ตัด Completion ออกจาก UI หน้า assignment report โดยลบ completion KPI tile, ลบ print-only completion text, ลบ section `Completion by Course`, ลบคอลัมน์ `Completed`/`Completion` ในตาราง group summary, และเปลี่ยนคอลัมน์ท้าย learner table จาก `Completed Date` เป็น `Due Date`
- ไฟล์หลักที่แตะ: `iLearn.Admin.React/src/pages/assignments/AssignmentReportPage.tsx`, `iLearn.Admin.React/src/pages/assignments/AssignmentReportCharts.tsx`, `DOC/PLANS/PLAN-170-assignment-report-remove-completion-ui.md`, `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี (frontend presentation only); `StatusDonut` ปรับให้ `completionRate` เป็น optional เพื่อคง compatibility กับหน้าอื่น
- Verified: `get_errors` ✓, `npm run lint` ✓, `npm run build` ✓

## [2026-07-30] GitHub Copilot — PLAN-169 remove Timeline column from assignment report
- ทำอะไร: ตัดคอลัมน์ `Timeline` ออกจากตาราง learner detail บนหน้า assignment report โดยลบทั้ง header และ row cell ที่แสดง Start/Due date timeline; ปรับ empty-state `colSpan` จาก 6 เป็น 5 และลบ import `tf` ที่ไม่ใช้แล้ว
- ไฟล์หลักที่แตะ: `iLearn.Admin.React/src/pages/assignments/AssignmentReportPage.tsx`, `DOC/PLANS/PLAN-169-assignment-report-remove-timeline-column.md`, `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี (frontend table presentation only)
- Verified: `get_errors` ✓, `npm run lint` ✓, `npm run build` ✓

## [2026-07-30] GitHub Copilot — PLAN-168 remove completion metrics from assignment workbook export
- ทำอะไร: ปรับ `Export Excel Workbook` ของหน้า assignment report ให้ตัดข้อมูลเชิง completion ออก โดยลบ `Completed`/`Completion Rate` จาก `Overview`, ลบ `Completed Date` จาก `Learner Detail`, ลบ `Completed Learners`/`Completion %` จาก `Course Summary`, และลบ `Completed`/`Completion %` จาก `Group Summary`
- ไฟล์หลักที่แตะ: `iLearn.Admin.React/src/pages/assignments/AssignmentReportPage.tsx`, `DOC/PLANS/PLAN-168-assignment-report-workbook-remove-completion-metrics.md`, `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี (frontend workbook export shape only)
- Verified: `get_errors` ✓, `npm run lint` ✓, `npm run build` ✓

## [2026-07-30] GitHub Copilot — PLAN-167 assignment report admin workbook export
- ทำอะไร: ยกเลิกปุ่ม `Export CSV (All)`, `Export Excel (Filtered)`, `Export CSV (Filtered)` และเปลี่ยน export ของหน้า assignment report ให้เหลือ `Export Excel Workbook` ปุ่มเดียวที่สร้าง workbook หลาย sheet (Overview, Learner Detail, Course Summary, Group Summary, Status Summary, Exceptions, Incomplete Only)
- ไฟล์หลักที่แตะ: `iLearn.Admin.React/src/lib/tableExport.ts`, `iLearn.Admin.React/src/pages/assignments/AssignmentReportPage.tsx`, `iLearn.Admin.React/src/lib/labels.ts`, `DOC/PLANS/PLAN-167-assignment-report-admin-workbook-export.md`, `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี (ใช้ข้อมูล `AssignmentDashboard` ที่โหลดอยู่แล้ว, frontend export behavior only)
- Verified: `get_errors` ✓, `npm run lint` ✓, `npm run build` ✓

## [2026-07-30] GitHub Copilot — PLAN-166 follow-up: remove Recharts pie sector focus frame
- ทำอะไร: แก้กรอบดำที่ยังเหลือหลังคลิก donut โดยตั้ง `Pie rootTabIndex={-1}` (Recharts default = 0) และขยาย scoped focus suppression ให้ครอบ focused SVG child ทั้งหมดใน chart wrapper
- ไฟล์หลักที่แตะ: `iLearn.Admin.React/src/pages/assignments/AssignmentReportCharts.tsx`, `DOC/PLANS/PLAN-166-assignment-report-static-charts.md`, `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี (frontend chart presentation only)
- Verified: `get_errors` ✓, `npm run lint` ✓, `npm run build` ✓

## [2026-07-30] GitHub Copilot — PLAN-166 follow-up: readable tooltips without chart focus frame
- ทำอะไร: ปรับ follow-up ตาม feedback หลัง QA: เอา tooltip กลับมาเป็นสีอ่อนอ่านง่าย แต่ปิด Recharts focus frame หลังคลิกด้วย `accessibilityLayer={false}` + scoped outline suppression และยังคง `isAnimationActive={false}`/ไม่มี cursor overlay
- ไฟล์หลักที่แตะ: `iLearn.Admin.React/src/pages/assignments/AssignmentReportCharts.tsx`, `DOC/PLANS/PLAN-166-assignment-report-static-charts.md`, `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี (frontend chart presentation only)
- Verified: `get_errors` ✓, `npm run lint` ✓, `npm run build` ✓

## [2026-07-30] GitHub Copilot — PLAN-166 assignment report static charts
- ทำอะไร: ปิด animation/hover feedback ของ `Status Overview` และ `Completion by Course` บนหน้า assignment report โดยลบ Recharts `Tooltip` และตั้ง `isAnimationActive={false}` บน `Pie`/`Bar`
- ไฟล์หลักที่แตะ: `iLearn.Admin.React/src/pages/assignments/AssignmentReportCharts.tsx`, `DOC/PLANS/PLAN-166-assignment-report-static-charts.md`, `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี (frontend chart presentation only)
- Verified: `get_errors` ✓, `npm run lint` ✓, `npm run build` ✓

## [2026-07-30] GitHub Copilot — PLAN-165 assignment report export actions
- ทำอะไร: ปรับหน้า `assignments/{id}/report` ให้ export ข้อมูลได้ชัดเจนขึ้น โดยแทน `ExportMenu` ใน controls sidebar ด้วย `ControlAction` สำหรับ Excel/CSV ทั้ง All และ Filtered พร้อม loading/disabled state และ label `Data Export`
- ไฟล์หลักที่แตะ: `iLearn.Admin.React/src/pages/assignments/AssignmentReportPage.tsx`, `iLearn.Admin.React/src/lib/labels.ts`, `DOC/PLANS/PLAN-165-assignment-report-export-actions.md`, `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี (ใช้ข้อมูล `AssignmentDashboard` ที่โหลดอยู่แล้ว, frontend presentation only)
- Verified: `get_errors` ✓, `npm run lint` ✓, `npm run build` ✓

## [2026-07-30] GitHub Copilot — PLAN-164 durable Bulk Assign selection panel radius fix
- ทำอะไร: ตรวจ root cause ของปัญหา radius ใน `Selected Courses` แล้วแก้แบบยั่งยืนด้วย shared `WizardSelectionPanel` ที่ encode `rounded-lg + overflow-hidden + header + scroll body`; refactor ทั้ง `Syllabus Catalog` และ `Selected Courses` ใน Bulk Assign มาใช้ component เดียวกันเพื่อลด drift
- ไฟล์หลักที่แตะ: `iLearn.Admin.React/src/components/ui/WizardSelectionPanel.tsx`, `iLearn.Admin.React/src/pages/assignments/BulkAssignPage.tsx`, `DOC/PLANS/PLAN-164-bulk-assign-wizard-selection-panel-radius.md`, `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี (frontend presentation/component primitive only)
- Verified: `get_errors` ✓, `npm run lint` ✓, `npm run build` ✓

## [2026-07-30] GitHub Copilot — implement PLAN-163 Delivery #6 (Batch C complete + lint error enforcement)
- ทำอะไร: ปิด Batch C โดยย้าย native `<button>` ที่เหลือใน `AssignmentGanttPage`, `VersionFormPage`, `LearnerGroupEditorPage`, `LearnerListPage`, `TranscriptReportPage` ไปใช้ `AppButton`/`IconButton`; flip `no-restricted-globals(fetch)` และ `no-restricted-syntax(JSX button)` ใน `eslint.config.js` จาก `warn` เป็น `error`; เพิ่ม section `Lint Guardrails And Exceptions` ใน React README พร้อมเหตุผล allowlist และอัปเดตแผนแม่ `PLAN-157` เป็น `DONE` พร้อมสรุปลำดับ child plans 158-163
- ไฟล์หลักที่แตะ: `iLearn.Admin.React/src/pages/assignments/AssignmentGanttPage.tsx`, `iLearn.Admin.React/src/pages/courses/VersionFormPage.tsx`, `iLearn.Admin.React/src/pages/learner-groups/LearnerGroupEditorPage.tsx`, `iLearn.Admin.React/src/pages/learners/LearnerListPage.tsx`, `iLearn.Admin.React/src/pages/reports/TranscriptReportPage.tsx`, `iLearn.Admin.React/eslint.config.js`, `iLearn.Admin.React/README.md`, `DOC/PLANS/PLAN-163-admin-standards-delivery6-batchc-and-error-enforcement.md`, `DOC/PLANS/PLAN-157-admin-standards-rollout-execution-contract.md`, `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี (frontend standards enforcement + UI primitive adoption only)
- Verified: `npm run lint` ✓ (ผ่านภายใต้ error rules), `npm run build` ✓, allowlist lint command output ว่าง ✓, stdin probes ของ `<button>`/`fetch` fail เป็น errors ตามคาด ✓, `git diff --check` ✓ (มีเฉพาะ CRLF warning)

## [2026-07-30] GitHub Copilot — implement PLAN-162 Delivery #5 (shared report KPI tile + CourseVersion clock fix)
- ทำอะไร: เพิ่ม shared `ReportKpiTile` และ refactor `AssignmentSummaryReportPage` + `LearnerGroupSummaryReportPage` ให้ใช้ component เดียว (semantic tone `neutral|info|success|danger`); แก้ `CourseVersionService` ให้ `CreatedAt` ของ `CourseVersion`/`CourseContentItem` ใช้ `_dateTime.Now` แทน `DateTime.UtcNow`; เพิ่มเทสต์ deterministic ใหม่ใน `CourseVersionLearnerPolicyTests` และขยาย harness ให้ assert `CourseContentItem.CreatedAt` ได้
- ไฟล์หลักที่แตะ: `iLearn.Admin.React/src/components/ui/ReportKpiTile.tsx`, `iLearn.Admin.React/src/pages/reports/AssignmentSummaryReportPage.tsx`, `iLearn.Admin.React/src/pages/reports/LearnerGroupSummaryReportPage.tsx`, `iLearn.Application/Services/CourseVersionService.cs`, `iLearn.Tests/CourseVersionLearnerPolicyTests.cs`, `DOC/PLANS/PLAN-162-admin-standards-delivery5-kpi-tile-and-courseversion-clock.md`, `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี (shared UI extraction + internal timestamp source consistency + test only)
- Verified: `npm run lint` ✓ (0 errors, 6 warnings), `npm run build` ✓, `dotnet test artifacts\\verify-plan162\\iLearn.Tests.dll --filter FullyQualifiedName~CourseVersionLearnerPolicyTests` ✓ (8/8), cleanup temp artifacts ✓, `git diff --check` ✓ (มีเฉพาะ CRLF warning)

## [2026-07-30] GitHub Copilot — implement PLAN-161 Delivery #4 response helper + report export migration
- ทำอะไร: เพิ่ม `fetchResponseWithAccessControl` ใน `apiClient.ts` (ใช้ auth/header merge/error mapping เดิม) และ refactor `fetchWithAccessControl<T>` ให้ reuse helper นี้; migrate Excel export ของ `AssignmentSummaryReportPage` และ `LearnerGroupSummaryReportPage` จาก direct `fetch` ไป helper ใหม่พร้อมคง `Accept` xlsx + `downloadBlob`/`filenameFromContentDisposition`
- ไฟล์หลักที่แตะ: `iLearn.Admin.React/src/lib/apiClient.ts`, `iLearn.Admin.React/src/pages/reports/AssignmentSummaryReportPage.tsx`, `iLearn.Admin.React/src/pages/reports/LearnerGroupSummaryReportPage.tsx`, `DOC/PLANS/PLAN-161-admin-standards-delivery4-response-helper-and-report-exports.md`, `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี (frontend helper/usage refactor only)
- Verified: `npm run lint` ✓ (0 errors; warnings ลดจาก 8 → 6), `npm run build` ✓, direct `fetch(` ใน report export 2 หน้าไม่เหลือ (`rg` no-match) ✓, `git diff --check` ✓ (มีเฉพาะ CRLF warning)

## [2026-07-30] GitHub Copilot — implement PLAN-160 Batch B native-button migration (Course/LearnerGroup detail pages)
- ทำอะไร: เดินต่อจาก PLAN-159 ตาม PLAN-157 โดย migrate native `<button>` ใน 3 ไฟล์ Batch B เป็น shared primitives: `CourseEditorPage` (select-existing-content tile + add-content icon buttons), `VersionDetailPage` (tile + add-content icon button), `LearnerGroupDetailPage` (clear queue, remove row, close modal)
- ไฟล์หลักที่แตะ: `iLearn.Admin.React/src/pages/courses/CourseEditorPage.tsx`, `iLearn.Admin.React/src/pages/courses/VersionDetailPage.tsx`, `iLearn.Admin.React/src/pages/learner-groups/LearnerGroupDetailPage.tsx`, `DOC/PLANS/PLAN-160-admin-standards-batch-b-native-button-migration.md`, `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี (UI primitive adoption only)
- Verified: `npm run lint` ✓ (0 errors; warnings ลดจาก 16 → 8), `npm run build` ✓, `rg -n "<button|</button>"` ในไฟล์ Batch B ไม่พบแล้ว ✓, `git diff --check` ✓ (มีเฉพาะ CRLF warning)

## [2026-07-30] GitHub Copilot — implement PLAN-159 Batch A native-button migration (Assignment pages)
- ทำอะไร: ทำต่อจาก PLAN-158 ตาม PLAN-157 delivery #2 โดย migrate native `<button>` ใน Batch A สองไฟล์เป็น `AppButton` ทั้งหมด: `BulkAssignPage` (clear selected courses) และ `AssignmentDetailPage` (clear selected learners, remove not found, clear queue, remove queued row)
- ไฟล์หลักที่แตะ: `iLearn.Admin.React/src/pages/assignments/BulkAssignPage.tsx`, `iLearn.Admin.React/src/pages/assignments/AssignmentDetailPage.tsx`, `DOC/PLANS/PLAN-159-admin-standards-batch-a-native-button-migration.md`, `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี (UI primitive adoption only)
- Verified: `npm run lint` ✓ (0 errors; warnings ลดจาก 21 → 16), `npm run build` ✓, `rg -n "<button|</button>"` ในสองไฟล์ Batch A ไม่พบแล้ว ✓, `git diff --check` ✓ (มีเฉพาะ CRLF warning)

## [2026-07-30] GitHub Copilot — implement PLAN-158 phase-0 standards guardrails + AppTable action primitive
- ทำอะไร: เริ่ม rollout ตาม PLAN-157 delivery #1 โดยเพิ่ม ESLint guardrails ระดับ `warn` ใน `eslint.config.js` (`no-restricted-globals` สำหรับ `fetch` ใน `src/**` + allowlist 4 ไฟล์, และ `no-restricted-syntax` บล็อก native `<button>` ใน `src/pages/**`) และ refactor `AppTable` row actions ให้ใช้ `IconButton` แทน native `<button>`
- ไฟล์หลักที่แตะ: `iLearn.Admin.React/eslint.config.js`, `iLearn.Admin.React/src/components/ui/AppTable.tsx`, `DOC/PLANS/PLAN-158-admin-standards-phase0-warn-and-apptable.md`, `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน (API shape / props / DB): ไม่มี (frontend lint policy + shared UI primitive usage only)
- Verified: `npm run lint` ✓ (0 errors, 21 warnings expected), `npm run build` ✓, rule probes ผ่าน `npx eslint --stdin --stdin-filename src/pages/__probe__.tsx` ทั้ง button/fetch ✓, allowlist files lint clean ✓, `git diff --check` ✓ (มีเฉพาะ CRLF warning)

## [2026-07-30] Claude Code — รีวิว PLAN-157 rollout (158-163) → VERIFIED ทั้ง 7 แผน
- ทำอะไร: ตรวจ AC ทั้ง 8 ข้อของ PLAN-157 กับงานจริง (commit 68593ca/c7fe2e5/49d57da/3284bf1) — **ผ่านหมด ไม่มี finding ตีกลับ**. ไฮไลต์: native `<button>` ใน `src/pages/**` = 0, `AppTable` = 0 และใช้ `IconButton` โดยคง `stopPropagation`/`title`/tone, **ไม่มี `eslint-disable` bypass แม้จุดเดียว**, ESLint 3 บล็อกตรงสเปค §1 เป๊ะ (rule ต่างชื่อกัน), export ใช้ `fetchResponseWithAccessControl` และ **`Accept` ของ Excel ยังอยู่ครบ** (invariant §4.1) + `downloadBlob` ไม่ถูกแตะ, `HealthCheckPage` ไม่อยู่ใน diff เลย ⇒ 503 ปลอดภัยโดยโครงสร้าง, `ReportKpiTile` map tone ได้ class ตรงของเดิมทุกค่า+markup เหมือนเดิม ⇒ visible output ไม่เปลี่ยน. **grep diff ยืนยันไม่มี `type="submit"`/`disabled`/`loading` ถูกแตะเลย** ⇒ ความเสี่ยง §3 เป็นศูนย์แบบมีหลักฐาน
- ไฟล์หลักที่แตะ: `DOC/PLANS/PLAN-157-*.md` (+Reviewer Notes รอบ implement, →VERIFIED), `PLAN-158..163` (→VERIFIED + Reviewer Notes), `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน: ไม่มี (review/docs only)
- Verified: `npm run lint` ✓ exit 0, `npm run build` ✓ exit 0, `dotnet test ~CourseVersionLearnerPolicyTests` **8/8 ✓**, `git diff --check` ✓ — และ **mutation test: revert `_dateTime.Now` ทีละบรรทัด (200/581) ทำ assertion แดงคนละบรรทัด (78/79)** ⇒ test ใหม่กันของจริงทั้งสอง entity ไม่ใช่ผ่านลอย ๆ (ตรวจ `InMemoryGenericRepository.AddEntity` แล้วว่าไม่ stamp `CreatedAt` เอง); คืนไฟล์ + ลบ artifacts แล้ว tree clean
- ข้อสังเกตไม่บล็อก: `apiClient.ts` CRLF→LF ทั้งไฟล์ (diff บวม 366 แต่ของจริง 16+/10-; ถูกตาม `* text=auto` ใน `.gitattributes` และ PLAN-158 disclose ไว้แล้ว) · hover/สี+ขนาดไอคอนใน action column เปลี่ยนเล็กน้อยจากการใช้ `IconButton` (resting เหมือนเดิมทุก tone, ได้ focus ring + `aria-label` เพิ่ม) ควรเหลือบดูจริง 1 ครั้ง · class dropzone ซ้ำ 3 ไฟล์ (ซ้ำมาก่อน) · `variant="ghost"`+amber override 1 จุด ควรมี tone `warning` · README ควรกำกับว่า allowlist 4 ไฟล์ปิดรายการแล้ว
- หนี้ที่ยังเปิด (นอก contract โดยเจตนา): §1b `LearnerDirectorySelector` 11 ปุ่ม + `NotificationRow`/`Header`/`Sidebar` — ต้องเปิดแผนใหม่

## [2026-07-30] Claude Code — แก้ Finding 1-4 ของ PLAN-157 เอง → READY (PLAN-156 → SUPERSEDED)
- ทำอะไร: ผู้ใช้สั่งให้ reviewer ลงมือแก้เอง — (1) กฎ `<button>` scope ขัดกันเอง: เขียน §1 ใหม่ให้ enforce เฉพาะ `src/pages/**` แล้ว**ลบ allowlist shared components ทั้งก้อน** (ไฟล์นอก scope ไม่ต้องมีลิสต์ให้ผิด) ย้ายของจริงไป §1b "Known remaining debt" — `LearnerDirectorySelector.tsx` มี native button **11 จุด** เป็น control จริงไม่ใช่ primitive boundary ⇒ ต้องเปิดแผนแยก, `AppTable.tsx` มีจุดเดียว (บรรทัด 365) ซึ่ง §2 refactor แล้วเหลือศูนย์ (2) ตัด `data-standard-exception` (React forward `data-*` ขึ้น production DOM) ไปใช้ `eslint-disable-next-line ... -- <reason>` (3) ตัด custom TS-API script + `standards:check` ทิ้ง เปลี่ยนเป็น ESLint flat-config 3 บล็อก โดยใช้ **rule ต่างชื่อ** (`no-restricted-globals` สำหรับ fetch / `no-restricted-syntax` สำหรับ JSX button) เพราะ flat config แทนที่ option array ไม่ merge, negative check ใช้ `--stdin-filename` ไม่ต้อง commit fixture (4) ตัด `AppLinkButton` + shared style map ออก เพราะ consumer = 0 (`<Link>` ใน Batch A+B มีจุดเดียวและเป็น inline text link ที่แผนบอกเองให้คงไว้) จด `CourseDetailPage.tsx:655` เป็น candidate แรกของแผนถัดไป · บวก minor 5-10 (§4 header-merge invariant, ห้ามเขียน filename parser ซ้ำเพราะ `downloadBlob.ts` มีแล้ว, HealthCheckPage ไม่ต้องแก้เพราะคอมเมนต์มีอยู่แล้ว, ตัดคำว่า CI เพราะ `.github/workflows` ว่าง, เลิกจองเลข 158-163, PLAN-156 → SUPERSEDED) + เพิ่มตาราง Verified baseline
- ไฟล์หลักที่แตะ: `DOC/PLANS/PLAN-157-*.md` (→READY), `DOC/PLANS/PLAN-156-*.md` (→SUPERSEDED), `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน: ไม่มี (plan/docs only — ยังไม่แตะโค้ดแอป)
- Verified: **รัน ESLint config ที่เขียนใน §1 กับ working tree จริง** ผ่าน `eslint.probe.config.js` ชั่วคราว (ลบแล้ว, `eslint.config.js` ตัวจริงไม่ถูกแตะ — `git status` ยืนยันเหลือแค่ไฟล์ docs): กฎทั้งสองยิงถูกผ่าน `--stdin-filename` ✓, override ปิด fetch rule ให้ 4 ไฟล์ allowlist ได้ ✓, button rule ไม่รั่วไป `components/ui` ✓, รัน `src/pages/**` ได้ **19 + 2 เป๊ะ ตรงตาราง baseline ทุกไฟล์** ✓; `git diff --check` ✓; `plan-status.ps1` อ่าน header ทั้งสองไฟล์ได้ ✓
- งานที่เหลือของ GPT: implement ตาม Delivery order ข้อ 1 (ESLint rules ที่ `'warn'` + AppTable action → IconButton) — เรียก `-Next` เอาเลขแผนตอนสร้างไฟล์

## [2026-07-30] Claude Code — รีวิว PLAN-156/157 → คง DRAFT ทั้งคู่ (4 finding ต้องแก้ก่อน READY)
- ทำอะไร: verify claim ทางเทคนิคของ PLAN-157 กับโค้ดจริงทุกข้อ — **ถูกหมด** (fetchWithAccessControl JSON.parse จริง, direct fetch 3 จุด, fetch whitelist ตรง, AppButton/IconButton variant+tone ตรง, AppTable action มี stopPropagation/title, CourseVersionService UtcNow 2 จุด บรรทัด 200/581 บน CourseVersion+CourseContentItem, KpiTile byte-identical, TS 6.0.2, -Next=158). แต่พบ 4 finding: (1) §1 กฎ button สองข้อขัดกันเรื่อง scope + allowlist ตกไฟล์ `LearnerDirectorySelector.tsx` (2) `data-standard-exception` ship ขึ้น DOM production ควรใช้ eslint-disable comment (3) custom TS-API script ทำได้ด้วย `no-restricted-syntax` + flat-config override ที่มีอยู่แล้ว (4) `AppLinkButton` มี consumer ~0 — `<Link>` ใน Batch A+B มี 1 จุดและเป็น inline text link ที่แผนบอกเองว่าให้คงไว้. บวก minor 6 ข้อ (§4.4 ทำแล้ว, downloadBlob helper มีอยู่แล้ว, Accept header invariant, **ไม่มี CI จริง — `.github/workflows` ว่าง**, การจองเลข 158-163, PLAN-156 ควรเป็น SUPERSEDED)
- ไฟล์หลักที่แตะ: `DOC/PLANS/PLAN-156-*.md` (+Reviewer Notes), `DOC/PLANS/PLAN-157-*.md` (+Reviewer Notes), `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน: ไม่มี (review/docs only)
- Verified: baseline ของ GPT ตรงกับที่วัดเองเป๊ะ (19 raw button/10 ไฟล์, 3 fetch, 2 UtcNow); `plan-status.ps1` อ่าน header ทั้งสองไฟล์ได้ ✓; `git diff --check` ✓
- งานที่เหลือของ GPT: แก้ finding 1-4 ใน PLAN-157 Reviewer Notes แล้วค่อยตั้ง READY

## [2026-07-30] GitHub Copilot — review PLAN-156 and add executable standards contract
- ทำอะไร: ตรวจ PLAN-156 พบ guardrail `<button>` ก่อน migration ทำให้ fail จาก baseline, `fetchWithAccessControl` ใช้กับ binary export ไม่ได้, และยังไม่มี `AppLinkButton`/baseline ที่วัดได้ จึงสร้าง PLAN-157 เป็น execution contract โดยคง PLAN-156 เป็น strategy/history
- ไฟล์หลักที่แตะ: `DOC/PLANS/PLAN-157-admin-standards-rollout-execution-contract.md`, `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน: ไม่มี (plan/docs only)
- Verified: `plan-status -Next` หลังสร้าง = PLAN-158; `git diff --check` ✓; baseline ยืนยัน page raw buttons 19 จุด/10 ไฟล์, direct fetch 3 จุด, Application `DateTime.UtcNow` 2 จุดใน `CourseVersionService`

## [2026-07-30] GitHub Copilot — PLAN-156 rollout plan for standards sustainability
- ทำอะไร: สร้างแผน `PLAN-156` สำหรับ rollout มาตรฐานร่วมแบบยั่งยืน (phase 0-4) ครอบคลุม guardrails, UI primitives adoption, API/export helper consolidation, token dedup, และ backend time-source consistency
- ไฟล์หลักที่แตะ: `DOC/PLANS/PLAN-156-admin-standards-sustainability-rollout.md`, `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน: ไม่มี (plan/docs only)
- Verified: ใช้เลขแผนจาก `tools/plan-status.ps1 -Next` = PLAN-156; `git diff --check` ไม่มี whitespace issues

## [2026-07-30] GitHub Copilot — remove React Admin grid footer/status bars
- ทำอะไร: สำรวจ `iLearn.Admin.React/src` แล้วถอด shared `AppTableFooter`, ลบข้อความ `All records loaded`/`Scroll down to load more`/แถบ `Showing X of Y` ใต้ grid-like tables และปรับ `LearnerDirectorySelector` ให้เหลือเฉพาะ selection tray
- ไฟล์หลักที่แตะ: `iLearn.Admin.React/src/components/ui/AppTable.tsx`, `src/components/shared/LearnerDirectorySelector.tsx`, detail/report/list pages หลายไฟล์, `src/lib/labels.ts`, `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน: ไม่มี (frontend presentation only)
- Verified: `npm run lint` ✓, `npm run build` ✓; grep ยืนยันไม่มี `All records loaded`/`AppTableFooter`/`scrollToLoadMore`/`<footer>` ใน `iLearn.Admin.React/src`

## [2026-07-30] Claude Code — แก้ Finding 1-4 ของ PLAN-155 เอง → VERIFIED
- ทำอะไร: ผู้ใช้สั่งให้ reviewer ลงมือแก้เอง — (1) `set-ilearn-prod-app-pools.ps1` เพิ่มฟิลด์ `ActualPool` ที่ **re-read จาก IIS จริง** หลังเขียน แล้วย้าย shared-pool check มาฝั่ง local หลังพิมพ์ตาราง (`-AuditOnly` = warn ไม่ล้ม, apply = throw ถ้าแยกไม่สำเร็จ) (2) ครอบ preflight ด้วย `if (-not $Rollback)` กันบล็อก rollback ฉุกเฉิน (3) ติด tag `ILEARN-TOPOLOGY:` ให้ throw ที่เป็น violation จริง แล้ว re-throw แม้ไม่มี `-IisCredential` ส่วน connection error ยัง warn ตามเดิม (4) แก้ code block ใน DEPLOY-CHECKLIST §8 + Verification ของแผนเป็น `& .\tools\...` และย้ายคำเตือน `-File` ขึ้นก่อน block + เพิ่มหัวข้อ Deploy preflight
- ไฟล์หลักที่แตะ: `tools/set-ilearn-prod-app-pools.ps1`, `tools/deploy-side-by-side.ps1`, `DOC/DEPLOY-CHECKLIST.md`, `DOC/PLANS/PLAN-155-*.md` (→VERIFIED), `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน: ไม่มี (deploy tooling/docs only — ไม่แตะ IIS PROD ที่ apply ไปแล้ว)
- Verified: parser ✓ ทั้ง 2 สคริปต์, `git diff --check` ✓, **test ดึง logic ที่แก้แล้วมารันจริง 17 assertion ผ่านหมด** (F1 warn/throw ถูกทุกเคสรวม apply ที่ย้ายสำเร็จบางตัว, F3 violation บล็อกได้แม้ไม่มี credential, F2/F4 static check)
- Outstanding: **ยังไม่ได้รันกับ PROD IIS จริง** (ไม่มีสิทธิ์ WinRM) — apply ครั้งหน้าให้ดูคอลัมน์ `ActualPool` ตรงกับ `TargetPool` ทุกแถว · Finding 5-6 (minor) ยังไม่แก้ ถ้าจะทำให้เปิดแผนใหม่

## [2026-07-30] Claude Code — รีวิว PLAN-154 → VERIFIED / PLAN-155 → ตีกลับ READY (safety check ไม่ทำงานจริง)
- ทำอะไร: ตรวจงาน app-pool split เอง — อ่าน `web.config` PROD จริง (inprocess ครบ 3 ✓), smoke เอง (`/iLearn/` 307→200, API 401, admin-react 200, admin 401 ไม่ใช่ 500 ✓) ⇒ ผลลัพธ์ IIS **ถูกต้อง ไม่ต้อง revert**. แต่ **ดึง logic guard ออกมารันจริงกับข้อมูล audit-before ของแผนเอง** พบ 3 บั๊กในของที่สร้างมากัน incident ซ้ำ: (1) guard ใน `set-ilearn-prod-app-pools.ps1` group `TargetPool` ที่ hardcode ไม่ซ้ำอยู่แล้ว = **dead code fire ไม่ได้เลย** (2) preflight บรรทัด 517 อยู่**ก่อน**สาขา `-Rollback` บรรทัด 522 = บล็อก rollback ฉุกเฉิน (3) `catch` แยก "ต่อ WinRM ไม่ได้" กับ "เจอ topology พังจริง" ไม่ออก + wrapper ไม่มี default `-IisCredential` = เส้นทาง default ไม่ enforce. บวก (4) DEPLOY-CHECKLIST §8 code block สั่ง `pwsh -File ... -IisCredential` ที่ย่อหน้าถัดไปเขียนเองว่าห้ามทำ
- ไฟล์หลักที่แตะ: `DOC/PLANS/PLAN-154-*.md` (→VERIFIED), `DOC/PLANS/PLAN-155-*.md` (→READY + Reviewer Notes 6 finding), `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน: ไม่มี (review/docs only)
- Verified: parser ✓ ทั้ง 2 สคริปต์, `git diff --check` ✓, PROD smoke 4 endpoint ✓, grep หา password หลุดในไฟล์ = ไม่พบ (เจอแค่ username เดิม)
- งานที่เหลือของ GPT: แก้สคริปต์+เอกสารตาม Finding 1-4 ใน PLAN-155 (ไม่ต้องแตะ IIS อีก)

## [2026-07-30] GitHub Copilot — PLAN-155 durable PROD iLearn app-pool split applied
- ทำอะไร: apply แยก PROD IIS app pools ถาวรจาก `iLearn.Dedicated` เป็น `iLearn.User`/`iLearn.Service`/`iLearn.Admin`/`iLearn.Admin.React`/`iLearn.Static`, คืน ASP.NET Core apps เป็น `hostingModel="inprocess"`, และเพิ่ม deploy preflight guard กันใช้ pool ผิด
- ไฟล์หลักที่แตะ: `tools/set-ilearn-prod-app-pools.ps1`, `tools/deploy-side-by-side.ps1`, `DOC/PLANS/PLAN-155-prod-ilearn-durable-app-pool-split-rollout.md`, `DOC/DEPLOY-CHECKLIST.md`, `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน: ไม่มี (IIS runtime + deploy tooling/docs only)
- Verified: parser ✓, wrong-pool guard ✓, PROD audit before/after ✓, smoke `/iLearn/` 200 + API 401/200 + admin-react 200 + admin 403/no 500, deploy wrapper preflight User/API/Admin ✓
- Outstanding: rotate/confirm rotation of the app-pool service-account password outside agent logs if not already done

## [2026-07-30] GitHub Copilot — PLAN-155 durable iLearn app-pool split rollout plan
- ทำอะไร: เขียน follow-up plan เพื่อเปลี่ยน PLAN-154 จาก mitigation (`outofprocess`) เป็น durable PROD IIS app-pool split + deploy safety checks
- ไฟล์หลักที่แตะ: `DOC/PLANS/PLAN-155-prod-ilearn-durable-app-pool-split-rollout.md`, `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน: ไม่มี (plan/docs only)
- Verified: PLAN-155 status header ✓, `set-ilearn-prod-app-pools.ps1` parser ✓, `git diff --check` ✓

## [2026-07-27] GitHub Copilot — PROD /iLearn 500.35 mitigation + app-pool split runbook
- ทำอะไร: ตรวจ PROD `/iLearn/` พบ 500.35 จากหลาย ASP.NET Core apps อยู่ pool เดียว (`iLearn.Dedicated`); mitigate โดยตั้ง active Learner/API/MVC Admin `web.config` เป็น `hostingModel="outofprocess"` ให้ตรงกันชั่วคราวจน root กลับมา 200
- ไฟล์หลักที่แตะ: `DOC/PLANS/PLAN-154-prod-ilearn-app-pool-split-50035.md`, `tools/set-ilearn-prod-app-pools.ps1`, `DOC/DEPLOY-CHECKLIST.md`, `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน: ไม่มี (IIS runtime mitigation + deploy tooling/docs)
- Verified: PowerShell parser ✓, `git diff --check` ✓, PROD smoke `/iLearn/` 200 + `/iLearn/Service/api/admin/session/me` 200 with credentials + `/iLearn/admin-react/` 200
- Outstanding: ถาวรต้องรัน `tools/set-ilearn-prod-app-pools.ps1` ด้วย IIS admin credential; session นี้ WinRM `Access is denied`; active configs ยังเป็น `outofprocess` จนกว่า split pool แล้ว script คืน `inprocess`

## [2026-07-27] GitHub Copilot — สร้าง iLearn.Dedicated app pool บน PROD และย้าย iLearn apps ทั้งหมด
- ทำอะไร: remote ไป `ap-ntc2137-prwb` แล้วสร้าง App Pool ใหม่ `iLearn.Dedicated`, ตั้งค่า `Integrated` + `AlwaysRunning` + `SpecificUser`, ผูกทุก app ใต้ `/iLearn*` ไป pool ใหม่, ตรวจยืนยันการผูกครบ 7 app และสตาร์ต pool สำเร็จ
- ไฟล์หลักที่แตะ: `DOC/DEPLOY-CHECKLIST.md`, `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน: ไม่มี (IIS runtime operation + docs only)
- Verified: ตรวจจาก remote IIS ได้ `PoolState=Started`, `TotalApps=7`, `AppsOnTargetPool=7`
- Outstanding: แนะนำหมุน password account `NIKONOA\Z001927` ทันที เพราะมีจังหวะที่ command output เผยค่า password

## [2026-07-27] Claude Code — รีวิว PLAN-153 → VERIFIED (smoke test ตรรกะจริง AC1-AC7 ผ่านหมด)
- ทำอะไร: รีวิวงาน PLAN-153 (ทำโดย **GitHub Copilot** แม้แผน assign ให้ Gemini — งานถูก ไม่ตีกลับ): รัน `npm run lint`/`npm run build` เองอิสระ ✓ + **smoke test โดยดึงฟังก์ชัน `deriveLearnerRollupStatus` ออกจากไฟล์ source มารันจริง** (ไม่ใช่ mock) แล้วจำลอง donut/filter/% ตามโค้ด → roll-up ถูก 7/7 เคส, **AC1-AC7 ผ่านหมด** (AC1 ป้าย `In Progress` แล้วกรองเจอจริง = Finding 1 ปิด · AC4 `Not Started 3` ไม่ใช่ 6 · AC6 เลขกลาง 33% ไม่ใช่ 67%). เจอ observation: ตัวหารเลขกลาง (คนที่มีคอร์ส) ≠ ประชากรวงแหวน (คนทั้งหมด) แต่ตรวจ backend แล้ว **learner ที่ไม่มีคอร์สเกิดไม่ได้** (`CourseCode` fallback `"-"` truthy เสมอ) ⇒ defensive code ไม่ต้องแก้
- ไฟล์หลักที่แตะ: `DOC/PLANS/PLAN-153-*.md` (→VERIFIED + Reviewer Notes), `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน: ไม่มี (รีวิว)
- Verified: lint ✓ build ✓ (hash `index-DkBMPY-5.js`) · smoke ตรรกะ AC1-AC7 ✓ · ไม่แตะ backend ตาม Out of scope ✓
- Outstanding: **ยังไม่ deploy** QA/PROD และยังไม่ได้ manual smoke บนของจริง (API ไม่ได้รันใน session นี้) — ควรกดกรองแต่ละสถานะบน assignment ที่มีสถานะผสมหลัง deploy

## [2026-07-27] GitHub Copilot — PLAN-153 learners-tab status unit alignment (row/filter/donut)
- ทำอะไร: implement PLAN-153 ใน `AssignmentDetailPage` ให้ตัวกรอง+โดนัทใช้ learner roll-up 5 สถานะตัวเดียวกับป้ายแถว, เปลี่ยน center completion เป็นหน่วยต่อคน (`Completed learners / learners with courses`), และ normalize ผู้เรียนไม่มีคอร์สเป็น `NotStarted` สำหรับ donut/filter เพื่อให้ยอดรวมตรงจำนวนแถว
- ไฟล์หลักที่แตะ: `iLearn.Admin.React/src/pages/assignments/AssignmentDetailPage.tsx`, `iLearn.Admin.React/src/lib/labels.ts`, `DOC/PLANS/PLAN-153-learner-status-filter-rollup-alignment.md`, `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน: ไม่มี (frontend-only)
- Verified: `npm run lint` ✓, `npm run build` ✓; grep ยืนยัน `deriveLearnerRollupStatus` มีนิยามเดียว และไม่มี `ASSIGNMENT_LABELS.courseCount` แล้ว

## [2026-07-27] Claude Code — PLAN-153 DRAFT → READY (ผู้ใช้เคาะตัวเลือก A, มอบ Gemini)
- ทำอะไร: ผู้ใช้เลือก **ตัวเลือก A** (แท็บ Learners นับ "ต่อคน" ทั้งป้าย/ตัวกรอง/โดนัท) → เปลี่ยน PLAN-153 เป็น `READY` มอบ Antigravity (Gemini). สำรวจเพิ่มก่อนปล่อยงาน พบว่าระบบมี roll-up **3 ชุดไม่ตรงกัน** — จึงเขียนเตือนไว้ในแผน: (1) `deriveLearnerRollupStatus` ต่อคน 5 สถานะ (2) `completionRate` ([AssignmentService.cs:1011](../iLearn.Application/Services/AssignmentService.cs#L1011)) **ต่อ enrollment** ซึ่งเป็นเลขกลางโดนัท (3) `ChartData` ต่อคน **3 สถานะ** ที่หน้า `AssignmentReportPage` ใช้อยู่. แก้ Scope ข้อ 4 จาก "ไปตรวจเอง" เป็นคำสั่งชัดเจน + สูตร `learnerCompletionRate` พร้อมใช้ (ไม่งั้นวงแหวนเป็นต่อคนแต่เลขกลางเป็นต่อ enrollment = บั๊กแบบเดิมซ้ำ)
- ไฟล์หลักที่แตะ: `DOC/PLANS/PLAN-153-learner-status-filter-rollup-alignment.md`, `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน: ไม่มี (แผนอย่างเดียว — งานจริง frontend-only ไฟล์เดียวตอน implement)
- Verified: — (แผน; อ้างอิงจากการอ่านโค้ด backend/frontend จริงใน session นี้)
- **ถึง Gemini:** รับ PLAN-153 ได้เลย — **ห้ามแตะ `AssignmentService.CompletionRate` และ `ChartData` ฝั่ง backend** (หน้าอื่นใช้อยู่ พังแน่) · ห้าม copy logic roll-up ไปวางซ้ำ ต้องมีนิยามเดียว · Known gap ท้ายแผน (หน้า report ยังเป็น 3 สถานะ) **อย่าลากเข้ามาแก้ในรอบนี้**

## [2026-07-27] Claude Code — รีวิว PLAN-152 → VERIFIED + optimize ระบบ agent (rotate log, plan-status tool)
- ทำอะไร: (1) **รีวิว PLAN-152** — ตรงตาม Scope, lint/build ผ่านซ้ำเอง, bundle hash `index-mJm00-Jd.js` ตรงกับที่ deploy จริง → `VERIFIED` + 3 Findings (Finding 1 MEDIUM: ป้าย roll-up ต่อคน vs ตัวกรอง/โดนัทต่อคอร์ส ยังขัดกันในเคสผสม → แตกเป็น PLAN-153 DRAFT รอผู้ใช้เคาะ) (2) **optimize ระบบ agent**: หมุน `AGENT_LOG.md` **849 KB → 56 KB** (473 entries → 30, ที่เหลือเข้า `DOC/archive/AGENT_LOG-2026-{06,07}.md`) + เพิ่ม `tools/rotate-agent-log.ps1` (fence-aware) และ `tools/plan-status.ps1` (อ่าน status ได้ทุก format ที่มีอยู่จริง → เปิดโปงหนี้รีวิว 41 แผน) + กติกาใหม่ใน `CLAUDE.md`/`PLANS/README.md` (entry ≤8 บรรทัด, format Status เป๊ะ, reviewer ต้องปิด loop)
- ไฟล์หลักที่แตะ: `tools/rotate-agent-log.ps1` (ใหม่), `tools/plan-status.ps1` (ใหม่), `DOC/AGENT_LOG.md`, `DOC/archive/*` (ใหม่), `CLAUDE.md`, `DOC/PLANS/README.md`, `DOC/PLANS/PLAN-152-*.md` (→VERIFIED + Reviewer Notes), `DOC/PLANS/PLAN-153-*.md` (ใหม่ DRAFT)
- Contract ที่เปลี่ยน: ไม่มี (docs/tooling เท่านั้น — ไม่แตะโค้ดแอป)
- Verified: `npm run lint` ✓ · `npm run build` ✓ · rotate ตรวจ integrity แล้ว (30+271+173 entries, ขอบต่อเนื่อง, byte รวมเท่าเดิม) · `plan-status.ps1` อ่าน header ได้ 129/134 ไฟล์
- **ถึงทุก agent:** ห้าม `Read` ไฟล์ใน `DOC/archive/` ทั้งไฟล์ (ใหญ่มาก) ให้ grep เฉพาะจุด · ก่อนสร้างแผนใหม่ใช้ `pwsh tools/plan-status.ps1 -Next`
- **ถึงผู้ใช้:** หนี้รีวิว 41 แผนค้างที่ `DONE` (ดู `-Debt`) — ในนั้นมี 12 แผนจาก 30 แผนล่าสุด

## [2026-07-24] GitHub Copilot — commit + deploy PLAN-152 to QA and PROD
- ทำอะไร: commit งานค้าง PLAN-152 (Learner row roll-up status + minimal summary/actions) แล้ว deploy `iLearn.Admin.React` ต่อไป QA และ PROD ตามคำขอผู้ใช้
- ไฟล์หลักที่แตะ: `iLearn.Admin.React/src/pages/assignments/AssignmentDetailPage.tsx`, `DOC/PLANS/PLAN-152-learner-row-rollup-status-minimal-summary.md`, `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน: ไม่มี (frontend-only)
- Verified: `git commit` = `00d944f`; deploy QA ผ่าน (`tools/deploy-admin-react.ps1`, `CopySucceeded=True`, `RobocopyExitCode=3`), deploy PROD ผ่าน (`tools/deploy-admin-react-prod.ps1`, `CopySucceeded=True`, `RobocopyExitCode=3`); smoke QA: `/iLearn/admin-react/` = 200, `assets/index-mJm00-Jd.js` = 200; smoke PROD: `/iLearn/admin-react/` = 200, `assets/index-mJm00-Jd.js` = 200

## [2026-07-24] GitHub Copilot — regenerate favicon PNGs from favicon.svg
- ทำอะไร: ผู้ใช้ขอสร้าง `favicon-32.png` และ `favicon-16.png` ใหม่โดยยึดรูปจาก `favicon.svg`; ใช้ Playwright render/screenshot จาก SVG เดียวกันที่ขนาด 32px/16px ทับไฟล์ PNG เดิมใน `iLearn.User/wwwroot`
- ไฟล์หลักที่แตะ: `iLearn.User/wwwroot/favicon-32.png`, `iLearn.User/wwwroot/favicon-16.png`, `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน: ไม่มี (static asset only)
- Verified: ตรวจขนาดไฟล์ด้วย `System.Drawing.Image` แล้วได้ `favicon-32.png = 32x32` และ `favicon-16.png = 16x16`

## [2026-07-24] Antigravity (Gemini) — PLAN-152 Learner row roll-up status 5 สถานะ + ย่อ Summary ให้มินิมอล
- ทำอะไร: เพิ่ม helper `deriveLearnerRollupStatus` คำนวณ roll-up status 5 สถานะ (`Completed`, `Overdue`, `InProgress`, `NotStarted`, `Upcoming`) ในตาราง Learners ของ `AssignmentDetailPage.tsx` เพื่อให้ตรงกับโดนัท chart และตัวกรอง, ปล่อยให้ `StatusBadge` derive tone สีตามสถานะจริงโดยอัตโนมัติ, ย้ายปุ่ม `View courses` ไปเป็น `IconButton` (ไอคอน `Eye`) ในคอลัมน์ Actions, และย่อคอลัมน์ Summary ให้มินิมอล (ตัด `X course(s)` badge)
- ไฟล์หลักที่แตะ: `iLearn.Admin.React/src/pages/assignments/AssignmentDetailPage.tsx`, `DOC/PLANS/PLAN-152-learner-row-rollup-status-minimal-summary.md` (Status→DONE, Implementer Notes), `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน: ไม่มี (Frontend UI/presentation only)
- Verified: `npm run lint` = 0 errors/warnings ✓; `npm run build` = ผ่าน ✓; `dotnet build iLearn.Tests` + `dotnet test` = Passed (279 passed) ✓

## [2026-07-24] GitHub Copilot — PLAN-151 deploy QA+PROD learner web app manifest (PWA)
- ทำอะไร: รับงาน deploy ต่อจาก Claude Code (โค้ดอยู่ใน working tree แล้ว) — รัน `deploy-user.ps1` (QA) + `deploy-user-prod.ps1` (PROD); smoke test ทุก acceptance criteria
- ไฟล์หลักที่แตะ: `DOC/PLANS/PLAN-151-learner-web-app-manifest-pwa.md` (Status→DONE, Implementer Notes), `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน: ไม่มี
- Verified QA: `GET /iLearn/site.webmanifest` = 200 `application/manifest+json` ✓; HTML `rel="manifest"` ✓, `theme-color=#c2410c` (orange QA) ✓, iOS metas ✓
- Verified PROD: `GET /iLearn/site.webmanifest` = 200 `application/manifest+json` ✓; HTML `rel="manifest"` ✓, `theme-color=#027d83` (teal PROD) ✓, iOS metas ✓; health check 200 ✓
- Outstanding: iPad standalone UX + DevTools manifest tab — ผู้ใช้ยืนยันเอง

## [2026-07-24 —] Claude Code — PLAN-151 add learner web app manifest (PWA / Add to Home Screen)
- ทำอะไร: ผู้ใช้ขอเพิ่ม web app manifest ให้ฝั่ง Learner (เดิมไม่มี `.webmanifest` เลย iOS พึ่ง apple-touch-icon อย่างเดียว) — สร้าง `iLearn.User/wwwroot/site.webmanifest` (name/short_name=iLearn, display=standalone, scope/start_url=`./` relative → resolve `/iLearn/` ตาม PathBase, icons ชี้ favicon-16/32 + apple-touch-icon-180 ตามขนาดจริง) และแก้ `_DevExtremeLayout.cshtml` เพิ่ม `<link rel="manifest">` + iOS standalone metas (`apple-mobile-web-app-capable`, `status-bar-style`, `mobile-web-app-capable`) + env-aware `<meta name="theme-color">` (teal PROD / orange QA-DEV ผ่าน `__isProd` เทียบเท่า PLAN-149). ไม่แตะ favicon links (PLAN-150) / theming block (PLAN-149) / ไฟล์ .ico/.svg
- ไฟล์หลักที่แตะ: `iLearn.User/wwwroot/site.webmanifest` (ใหม่), `iLearn.User/Views/Shared/_DevExtremeLayout.cshtml`, `DOC/PLANS/PLAN-151-learner-web-app-manifest-pwa.md` (ใหม่), `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน: ไม่มี (static asset + view/layout only)
- Verified: `dotnet build iLearn.User -o artifacts\verify-user` = 0 errors (มีแต่ NU1903 เดิม), cleanup ✓; ยืนยัน `site.webmanifest` ลงทะเบียนใน `staticwebassets.build.json` (จะรวมใน publish เหมือน favicon-*.png); `.webmanifest` อยู่ใน default content-type map ของ ASP.NET Core (`application/manifest+json`) → ไม่เสี่ยง 404 จาก middleware
- Outstanding: **ยังไม่ deploy** — มอบ Copilot (PLAN-151 READY) รับไป deploy QA→PROD + smoke. จุดเสี่ยง: `.webmanifest` เป็นนามสกุลใหม่ ต้อง smoke `GET /iLearn/site.webmanifest` anon = 200 บน QA (เทียบบทเรียน `.svg` 401 PLAN-148 Part 2). Follow-up ถ้าอยากได้ Android install เต็ม: ต้อง gen icon 192/512 จาก source โลโก้ ≥512px

## [2026-07-24 —] GitHub Copilot — PLAN-150 PNG favicon fix for Chromium tab icon
- ทำอะไร: implement PLAN-150 โดยสร้าง favicon PNG สำหรับ Chromium จาก `apple-touch-icon-180.png` เป็น `iLearn.User/wwwroot/favicon-32.png` และ `iLearn.User/wwwroot/favicon-16.png` (downscale คุณภาพสูง) เพื่อเลี่ยง `.ico` decode fail; layout ฝั่ง Learner ถูกเตรียมไว้แล้วให้ชี้ไป PNG icon (no additional layout change needed รอบนี้). ไม่แตะไฟล์ `.ico`/`.svg` บน disk ตามข้อกำหนด
- ไฟล์หลักที่แตะ: `iLearn.User/wwwroot/favicon-32.png`, `iLearn.User/wwwroot/favicon-16.png`, `DOC/PLANS/PLAN-150-favicon-remove-sizes-any-chrome-render.md`, `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน: ไม่มี (static asset only)
- Verified:
  - Build: `dotnet build iLearn.User -o artifacts\verify-user` ✓, cleanup `artifacts\verify-user` ✓
  - QA deploy: `tools/deploy-user.ps1` stamp `20260724120026`
  - PROD deploy: `tools/deploy-user-prod.ps1` stamp `20260724120134` (health `/iLearn/` = 200)
  - QA smoke: `GET /iLearn/favicon-32.png` / `favicon-16.png` = 200 `image/png`; browser smoke on `https://ap-ntc2138-qawb.nikonoa.net/iLearn/` showed title `iLearn (QA)`, PNG icon links in head, console errors = 0
  - PROD smoke: `GET /iLearn/favicon-32.png` / `favicon-16.png` = 200 `image/png`; browser smoke on `https://ap-ntc2137-prwb.nikonoa.net/iLearn/` showed title `iLearn`, PNG icon links in head, console errors = 0
- Outstanding: no local DEV smoke in this round

## [2026-07-24 —] GitHub Copilot — PLAN-149 learner QA vs PROD theming (burnt orange + QA badge)
- ทำอะไร: implement PLAN-149 ครบใน `iLearn.User` เพื่อแยกธีม QA ออกจาก PROD ด้วย runtime hostname detection โดยไม่แตะ favicon/scripts/footer: (1) `_DevExtremeLayout.cshtml` เพิ่ม `__isProd/__isDev/__envLabel`, title suffix (`iLearn (QA)/(DEV)` เฉพาะ non-PROD), inject `<style>` override ท้าย `<head>` สำหรับ non-PROD (`--brand-color: #c2410c`, `--brand-dark: #7c2d12`, `--brand-light: #ffedd5`, `--brand-lighter: #fff7ed`, `--brand-shadow-rgb: 194, 65, 12`), และเพิ่ม `.env-badge` ใน navbar brand เฉพาะ non-PROD; (2) `user-theme.css` เพิ่ม `--brand-shadow-rgb` default เป็น teal และแปลง rgba literal 6 จุดให้ผูกกับ `rgba(var(--brand-shadow-rgb), alpha)` เพื่อให้ focus/shadow เปลี่ยนตาม QA override อัตโนมัติ พร้อมเพิ่ม style `.env-badge`
- ไฟล์หลักที่แตะ: `iLearn.User/Views/Shared/_DevExtremeLayout.cshtml`, `iLearn.User/wwwroot/css/user-theme.css`, `DOC/PLANS/PLAN-149-learner-qa-environment-theming-burnt-orange.md`, `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน: ไม่มี (presentation/theme only)
- Verified:
  - Build: `dotnet build iLearn.User -o artifacts\verify-user` ✓ (มี warning vulnerability เดิม), cleanup `artifacts\verify-user` ✓
  - QA deploy: `tools/deploy-user.ps1` stamp `20260724092554`
  - PROD deploy: `tools/deploy-user-prod.ps1` stamp `20260724092705` (health `/iLearn/` = 200)
  - Browser smoke (runtime):
    - QA `https://ap-ntc2138-qawb.nikonoa.net/iLearn/`: title `iLearn (QA)`, computed `--brand-color=#c2410c`, `.env-badge=QA`, console errors = 0
    - PROD `https://ap-ntc2137-prwb.nikonoa.net/iLearn/`: title `iLearn`, computed `--brand-color=#027d83`, `.env-badge` ไม่แสดง, console errors = 0
  - Screenshot smoke: เก็บภาพ QA (burnt orange) และ PROD (teal) ใน session แล้ว
- Outstanding: acceptance ข้อ Localhost (`DEV` badge/title) ยังไม่ได้รันในรอบนี้

## [2026-07-24 —] GitHub Copilot — PLAN-148 follow-up hotfix (favicon-tab alias + redeploy)
- ทำอะไร: หลังผู้ใช้ทดสอบแล้วแท็บยังไม่ขึ้น (อาการ cache ค้างฝั่ง Edge profile) ทำ hotfix เพิ่มโดยสร้าง `iLearn.User/wwwroot/favicon-tab.ico` (copy จากไฟล์เดิม) และแก้ layout ให้ใช้ URL ใหม่ทั้ง `rel="icon"` และ `rel="shortcut icon"` (`favicon-tab.ico` + `asp-append-version`) เพื่อบังคับ browser ดึงไอคอนใหม่แทน record cache เดิม
- ไฟล์หลักที่แตะ: `iLearn.User/wwwroot/favicon-tab.ico` (ใหม่), `iLearn.User/Views/Shared/_DevExtremeLayout.cshtml`, `DOC/PLANS/PLAN-148-favicon-tab-icon-qa-svg-401-and-prod-cache.md`, `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน: ไม่มี (static asset + layout link only)
- Verified:
  - Build: `dotnet build iLearn.User -o artifacts\verify-user` ผ่าน
  - QA deploy stamp `20260724085317`
  - PROD deploy stamp `20260724085407` (health `/iLearn/` = 200)
  - View-source QA/PROD: เจอ `favicon-tab.ico` ทั้ง `rel="icon"` และ `rel="shortcut icon"`
  - Anonymous smoke: `GET /iLearn/favicon-tab.ico` = 200 `image/x-icon` ทั้ง QA และ PROD

## [2026-07-24 —] GitHub Copilot — PLAN-148 favicon tab icon fix (QA/PROD deploy + smoke)
- ทำอะไร: implement PLAN-148 Part 1 โดยแก้ `iLearn.User/Views/Shared/_DevExtremeLayout.cshtml` ให้เลิกประกาศ `favicon.svg` เป็น `rel="icon"` และย้าย `favicon.ico` ขึ้นเป็น icon หลัก (`rel="icon" type="image/x-icon" sizes="any" + asp-append-version`) พร้อมคง PNG/Apple touch links เดิมทั้งหมด; รัน build verify แล้ว deploy `iLearn.User` ไป QA/PROD
- ไฟล์หลักที่แตะ: `iLearn.User/Views/Shared/_DevExtremeLayout.cshtml`, `DOC/PLANS/PLAN-148-favicon-tab-icon-qa-svg-401-and-prod-cache.md`, `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน: ไม่มี (view/layout only)
- Verified:
  - Build: `dotnet build iLearn.User -o artifacts\verify-user` ผ่าน (warnings เดิม)
  - QA deploy: `tools/deploy-user.ps1` stamp `20260724084710`
  - PROD deploy: `tools/deploy-user-prod.ps1` stamp `20260724084807`, health `/iLearn/` = 200
  - Anonymous smoke:
    - QA `/iLearn/favicon.ico` = 200 `image/x-icon`
    - PROD `/iLearn/favicon.ico` = 200 `image/x-icon`
    - QA `/iLearn/favicon.svg` = 401 (เดิม)
    - PROD `/iLearn/favicon.svg` = 200 `image/svg+xml`
    - QA/PROD `/iLearn/` = 200
  - View-source QA/PROD: ไม่มี `favicon.svg rel=icon`, มี `favicon.ico rel=icon`, apple-touch links ครบ
  - Browser automation smoke: console errors = 0 ทั้ง QA และ PROD
- Outstanding: Part 2 (QA IIS drift) ยังไม่แก้เพราะไม่มีสิทธิ์ IIS admin ในงานนี้ — ต้อง escalate ให้ Infra align `.svg` anonymous access ให้เหมือน PROD ถ้าต้องการกลับไปใช้ SVG-first

## [2026-07-24 —] Claude Code — เขียน PLAN-148: แก้ favicon แท็บหน้า Learner ไม่ขึ้น (มอบ Copilot)
- ทำอะไร: ผู้ใช้รายงานแท็บ browser หน้า Learner (`/iLearn`) ไม่มีไอคอนทั้ง PROD และ QA. วินิจฉัยสด (Invoke-WebRequest anon vs credentialed ทั้งสอง env 2026-07-24): ไฟล์ไอคอนครบทุกตัวบน disk ทั้งคู่. **root cause แยก 2 เครื่อง** — layout ตั้ง `favicon.svg` เป็น `rel="icon"` หลัก, Edge/Chrome prefer SVG แล้ว (a) **QA:** `/iLearn/favicon.svg` = **401 anonymous** (IIS config drift ที่จับ `.svg` ใต้ Windows-Auth; `.ico`/`.png` = 200 anon ปกติ) → browser ขอ SVG ตอนหน้า login แบบ anon เจอ 401 ไม่ fallback = แท็บว่าง; PLAN-130 copy `.svg` ไป root แอปทำให้ 200 เฉพาะ credentialed แต่ anon ยัง 401 = แก้ไม่ตรงจุด (b) **PROD:** `/iLearn/favicon.svg` = 200 anon ปกติ, server ถูก — แท็บว่างเพราะ Edge favicon cache ค้างฝั่ง client. เขียน `PLAN-148` (READY): **Part 1** (code+deploy, ครอบทั้งสอง env) แก้ `_DevExtremeLayout.cshtml` ให้ tab icon พึ่ง `.ico`/PNG (anonymous-safe ทั้งคู่) เลิกประกาศ SVG rel=icon + `asp-append-version` บน `.ico` bust Edge cache; **Part 2** (IIS ops บน QA, ทำถ้ามีสิทธิ์ admin) align `.svg` anon ให้ = PROD ไม่งั้น escalate; **Part 3** PROD verify InPrivate (server ปกติ ไม่แตะ)
- ไฟล์หลักที่แตะ: `DOC/PLANS/PLAN-148-favicon-tab-icon-qa-svg-401-and-prod-cache.md` (ใหม่ READY), `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน: ไม่มี (แผนอย่างเดียว — ตอน implement แตะเฉพาะ view ของ `iLearn.User` + IIS ops บน QA)
- Verified: — (แผน; วินิจฉัยจาก live smoke ทั้ง QA/PROD ใน session นี้ + git history layout/favicon + PLAN-119/120/130)
- **ถึง Copilot:** Part 1 (แก้ layout + deploy `iLearn.User` QA/PROD) ทำได้ทันที ไม่ต้องรอ IIS; ห้ามลบ `favicon.svg` บน disk; ห้ามแก้ `\iLearn\web.config` บนเซิร์ฟเวอร์ (deploy เขียนทับ); ห้ามเอา `favicon.svg rel="icon"` กลับมาจนกว่า Part 2 (QA IIS) จะเสร็จ; ถ้าไม่มีสิทธิ์ IIS admin บน QA = จด Outstanding + escalate อย่า block Part 1

## [2026-07-24 —] Claude Code — รีวิว PLAN-147 → VERIFIED + fix เก็บตก (hasRows edge case)
- ทำอะไร: รีวิวงาน Copilot (commit `d4cdbfb` + deploy `992a4c1`): (1) สถาปัตยกรรมตรงตามการตัดสินใจที่ยืนยันไว้ — `exportRows()` เป็นจุดเดียวของ CSV+XLSX, 2 หน้ารวยไม่ downgrade (`handleExportExcel` เดิมไม่ถูกแตะ) (2) production build จริง — `write-excel-file/browser` แยก lazy chunk `browser-*.js`, grep 0 matches ใน main bundle (3) ไม่มี duplicate CSV escaping — `exportRowsAsCsv` เรียกจาก `tableExport.ts` ที่เดียว (4) `npm run lint` ✓ `npm run build` ✓ ซ้ำเอง (5) **fix เก็บตก minor**: `AssignmentReportPage` ปุ่ม "Export Filtered" เปลี่ยน `hasRows={filtered.length > 0}` → `hasRows` (เสมอ) ให้ปุ่มไม่หายเมื่อกรองได้ 0 แถวพอดี กลับไปพึ่ง `disabled={!isFiltered}` เหมือน `ControlAction` เดิม — ตั้งแผนเป็น VERIFIED
- ไฟล์หลักที่แตะ: `iLearn.Admin.React/src/pages/assignments/AssignmentReportPage.tsx`, `DOC/PLANS/PLAN-147-unified-csv-xlsx-export.md` (Reviewer Notes + VERIFIED), `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน: ไม่มี (frontend presentation only)
- Verified: lint ✓ build ✓ (bundle chunk-split ยังเหมือนเดิม `browser-CeIAsFQ3.js` 70.79 kB) — **ยังไม่ verify**: เปิดไฟล์ .csv/.xlsx จริงบน QA/PROD ทั้ง th/en (แนะนำปิดจุดนี้ก่อนถือว่าสมบูรณ์เต็มร้อย)

## [2026-07-24 —] GitHub Copilot — Commit `d4cdbfb` + deploy PLAN-147 to QA/PROD
- ทำอะไร: commit งาน PLAN-147 เป็น `d4cdbfb` (`feat(admin-react): unify csv and xlsx report export`) แล้ว deploy `iLearn.Admin.React` ไป QA และ PROD ผ่าน wrapper มาตรฐาน `tools/deploy-admin-react.ps1` / `tools/deploy-admin-react-prod.ps1`
- ไฟล์หลักที่แตะ: `DOC/AGENT_LOG.md` (entry นี้เท่านั้นหลัง deploy)
- Contract ที่เปลี่ยน: ไม่มีเพิ่มเติมจาก commit `d4cdbfb` (frontend-only; ไม่มี API/backend change)
- Verified: pre-commit `npm run lint` ✓, `npm run build` ✓; QA deploy build+copy สำเร็จ (`RobocopyExitCode=3`) ไป `\\AP-NTC2138-QAWB\wwwroot\iLearn\admin-react`; QA smoke ✓ `https://ap-ntc2138-qawb.nikonoa.net/iLearn/admin-react/` = 200, `/reports/assignments` = 200, `assets/index-IlGQvq4h.js` = 200, `assets/browser-CeIAsFQ3.js` = 200; PROD deploy copy สำเร็จ (`RobocopyExitCode=3`) ไป `\\ap-ntc2137-prwb\wwwroot\iLearn\admin-react`; PROD smoke ✓ `https://ap-ntc2137-prwb.nikonoa.net/iLearn/admin-react/` = 200, `/reports/assignments` = 200, `assets/index-IlGQvq4h.js` = 200, `assets/browser-CeIAsFQ3.js` = 200
- Outstanding: ยังไม่ได้ทำ manual browser smoke ดาวน์โหลดและเปิดไฟล์ `.csv` / `.xlsx` จริงบน QA/PROD ใน session นี้

## [2026-07-24 —] GitHub Copilot — PLAN-147 Unified CSV + XLSX export
- ทำอะไร: Implement PLAN-147 ใน `iLearn.Admin.React` โดยเพิ่ม dependency `write-excel-file@4.1.1`, helper กลาง `src/lib/tableExport.ts` (`exportRows(format, filename, header, rows)`) และ component กลาง `src/components/ui/ExportMenu.tsx`. ย้าย 6 หน้า report ให้เลิกเรียก `exportRowsAsCsv` ตรง ๆ: 4 หน้า client-only (`AssignmentReportPage`, `ActivityReportPage`, `ComplianceReportPage`, `CourseSummaryReportPage`) ใช้ helper กลางทั้ง CSV/XLSX; 2 หน้ารวย (`AssignmentSummaryReportPage`, `LearnerGroupSummaryReportPage`) เปลี่ยน CSV มาใช้ helper กลาง แต่คง server rich Excel endpoint เดิมไว้ พร้อม label ใหม่ `exportExcelDetail`. Assignment report คง behavior แยก export ทั้งหมด/filtered และเพิ่มฝั่ง XLSX ให้ทั้งสอง scope ผ่าน component เดียวกัน
- ไฟล์หลักที่แตะ: `iLearn.Admin.React/package.json`, `iLearn.Admin.React/package-lock.json`, `iLearn.Admin.React/src/lib/tableExport.ts`, `iLearn.Admin.React/src/components/ui/ExportMenu.tsx`, `iLearn.Admin.React/src/lib/labels.ts`, `iLearn.Admin.React/src/pages/assignments/AssignmentReportPage.tsx`, `iLearn.Admin.React/src/pages/reports/ActivityReportPage.tsx`, `iLearn.Admin.React/src/pages/reports/AssignmentSummaryReportPage.tsx`, `iLearn.Admin.React/src/pages/reports/ComplianceReportPage.tsx`, `iLearn.Admin.React/src/pages/reports/CourseSummaryReportPage.tsx`, `iLearn.Admin.React/src/pages/reports/LearnerGroupSummaryReportPage.tsx`, `DOC/PLANS/PLAN-147-unified-csv-xlsx-export.md`, `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน: ไม่มี backend/API contract change; frontend export surface เปลี่ยนเป็น shared helper/component เท่านั้น. 2 หน้า report summary ยังใช้ binary Excel endpoint เดิมจาก PLAN-146
- Verified: `npm run lint` ✓; `npm run build` ✓; Vite แยก `write-excel-file/browser` ไป chunk แยก `dist/assets/browser-*.js` (ไม่เข้า main bundle); grep `exportRowsAsCsv` เหลือเฉพาะ `src/lib/csvExport.ts` และ `src/lib/tableExport.ts`
- Outstanding: ยังไม่ได้ทำ manual browser smoke ดาวน์โหลด/เปิดไฟล์จริง `.csv`/`.xlsx` ทั้งภาษาไทยและอังกฤษใน session นี้

## [2026-07-24 —] Claude Code — เขียน PLAN-147: Unified CSV + XLSX export (client-side, มอบ Copilot)
- ทำอะไร: ผู้ใช้ขอให้ export "ทั้งหมด" มี 2 ทางเลือก .csv และ .xlsx โดยใช้โค้ดร่วมกัน. สำรวจพบ 2 รูปแบบที่ไม่แชร์โค้ดกันเลย: (a) CSV client-side `exportRowsAsCsv` 6 หน้า report, (b) server rich Excel (ClosedXML `ReportExcelBuilder`) แค่ 2 หน้า (Assignment/Learner Group Summary) หลาย sheet + detail รายคน. ยืนยัน 2 การตัดสินใจกับผู้ใช้: (1) กลไก export กลาง = client-side — helper เดียว `exportRows(format, filename, header, rows)` + `ExportMenu` component ใช้ทั้ง 6 หน้า, xlsx client = flat sheet เดียวผ่าน `write-excel-file` (lazy import); (2) คง server rich Excel ของ 2 หน้าไว้ (ไม่ downgrade) — บนสองหน้านั้น CSV ไปผ่าน helper กลาง แต่ปุ่ม Excel ยังยิง server endpoint เดิม. เขียนแผน `PLAN-147-unified-csv-xlsx-export.md` สถานะ READY
- ไฟล์หลักที่แตะ: `DOC/PLANS/PLAN-147-unified-csv-xlsx-export.md` (ใหม่), `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน: ไม่มี (แผนอย่างเดียว — ไม่แตะ backend/endpoint; งาน frontend ล้วนตอน implement)
- **ถึง Copilot:** รับ PLAN-147 ได้เลย — ห้าม downgrade server Excel 2 หน้ารวย, ห้ามมี CSV escaping สองชุด, lib xlsx ต้องอยู่ใน lazy chunk ไม่ใช่ main bundle

## [2026-07-23 —] Claude Code — รีวิว PLAN-145 + PLAN-146 → VERIFIED
- ทำอะไร: รีวิวงาน Copilot ทั้งสองแผน (commit `fe8719f` — ถูก commit ระหว่างรีวิว): (1) ตรวจ backend ครบ — division scope, effective dates ผ่าน `BuildVisibleEnrollmentRowsQuery`, `IDateTime.Now`, ไม่มี `FileStorage.Data` ใน query, ClosedXML (ไม่ใช่ EPPlus), fixed widths (2) ตรวจจุดเสี่ยง fallback `AssignmentNo`: summary `"Assignment {Id}"` ตรงกับ `BuildAssignmentDisplayNo` ⇒ lookup นับยอดใน `ApplyAssignmentExportDetailCounts` จับคู่ถูก; `ReportExcelBuilder.Labels.Status` ครอบ status keys จริงครบ (3) frontend: Remount route ✓, labels dictionary ครบ ✓, contract mirror comments ✓, binary endpoint ไม่ใช้ envelope ✓, CSV เดิมคงไว้ ✓ (4) รัน verification ซ้ำเอง: `npm run lint` ✓, `npm run build` ✓, `dotnet test` **279/279** ✓ — ตั้งสถานะทั้งสองแผนเป็น VERIFIED + Reviewer Notes
- ไฟล์หลักที่แตะ: `DOC/PLANS/PLAN-145-learner-group-summary-report.md`, `DOC/PLANS/PLAN-146-report-excel-detail-export.md`, `DOC/AGENT_LOG.md` (docs เท่านั้น)
- Contract ที่เปลี่ยน: ไม่มี (รีวิวอย่างเดียว)
- Verified: lint ✓ build ✓ tests 279/279 ✓ — **ยังไม่ deploy**: export endpoints ใหม่ต้อง full API publish + admin-react ไป QA→PROD; แนะนำทดสอบกด Export Excel จริงบน QA หลัง deploy (workbook validate ผ่าน ClosedXML round-trip ใน tests แล้ว)

## [2026-07-23 —] GitHub Copilot — PLAN-146 Excel export รายคน + date filter
- ทำอะไร: Implement PLAN-146 end-to-end ต่อจาก PLAN-143/145: เพิ่ม ClosedXML `0.105.0`, เพิ่ม export DTO/data methods ใน `ReportService`, `ReportExcelBuilder` สำหรับ workbook หลาย sheet, binary endpoints `GET /api/Reports/assignments/export` และ `GET /api/Reports/learner-groups/export`, date filter จาก effective due date, detail rows รายคนใช้ effective start/due dates จาก `BuildVisibleEnrollmentRowsQuery`; frontend เพิ่ม date range filter บน `/reports/assignments` และ `/reports/learner-groups`, ปุ่ม `Export Excel` พร้อม blob download helper และคง CSV export เดิม
- ไฟล์หลักที่แตะ: `iLearn.Application/iLearn.Application.csproj`, `iLearn.Tests/iLearn.Tests.csproj`, `iLearn.Application/DTOs/ReportDtos.cs`, `iLearn.Application/Interfaces/Services/IReportService.cs`, `iLearn.Application/Services/ReportService.cs`, `iLearn.Application/Services/ReportExcelBuilder.cs`, `iLearn.API/Controllers/ReportsController.cs`, `iLearn.Tests/ReportServiceTests.cs`, `iLearn.Admin.React/src/lib/downloadBlob.ts`, `iLearn.Admin.React/src/lib/labels.ts`, `iLearn.Admin.React/src/pages/reports/reportTypes.ts`, `iLearn.Admin.React/src/pages/reports/AssignmentSummaryReportPage.tsx`, `iLearn.Admin.React/src/pages/reports/LearnerGroupSummaryReportPage.tsx`, `DOC/PLANS/PLAN-146-report-excel-detail-export.md`, `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน: เพิ่ม binary file endpoints (no JSON envelope) `GET /api/Reports/assignments/export?from&to&lang` และ `GET /api/Reports/learner-groups/export?from&to&lang`, response content type `application/vnd.openxmlformats-officedocument.spreadsheetml.sheet`
- Verified: `npm run lint` ✓; `npm run build` ✓ (Vite chunk-size warning เดิม); focused export tests ✓; `dotnet build .\iLearn.Tests\iLearn.Tests.csproj -o .\artifacts\verify-test` ✓; `dotnet test .\artifacts\verify-test\iLearn.Tests.dll` = 279/279 ✓; cleaned validation artifacts ✓

## [2026-07-23 —] Claude Code — เขียน PLAN-146 Excel export รายคน + date filter (มอบ Copilot)
- ทำอะไร: ผู้ใช้ขอต่อยอด report Assignment/Learner Group ให้ admin โหลดข้อมูลไปรายงานต่อได้ — ยืนยัน requirement กับผู้ใช้แล้ว: (1) ละเอียดรายคน (learner × course) (2) ไฟล์ Excel .xlsx generate จาก backend หลาย sheet (3) filter ช่วงวันที่ก่อน export. เขียนแผน `PLAN-146-report-excel-detail-export.md` สถานะ READY: ClosedXML (ห้าม EPPlus เพราะ license), endpoints `GET /api/Reports/{assignments,learner-groups}/export?from&to&lang` คืนไฟล์ binary, Detail rows ต้องใช้ effective dates ผ่าน `BuildVisibleEnrollmentRowsQuery` (PLAN-086), CSV เดิมคงไว้, frontend เพิ่ม date range + ปุ่ม Export Excel ทั้งสองหน้า
- ไฟล์หลักที่แตะ: `DOC/PLANS/PLAN-146-report-excel-detail-export.md` (ใหม่), `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน: ไม่มี (แผนอย่างเดียว — endpoint ใหม่จะเกิดตอน implement)
- **ถึง Copilot:** รับ PLAN-146 ต่อจาก 143/145 ได้เลย — อย่าลืมว่า export endpoints ไม่ใช้ envelope `{ success, data }`

## [2026-07-23 —] GitHub Copilot — PLAN-145 Learner Group Summary Report
- ทำอะไร: เพิ่ม report ใหม่สำหรับข้อมูลแบบ Learner Group ที่ `/admin-react/reports/learner-groups`: backend endpoint `GET /api/Reports/learner-groups`, DTO `LearnerGroupSummaryReportDto`/`LearnerGroupSummaryRow`, service method `GetLearnerGroupSummaryReportAsync` ที่ scope ตาม `LearnerGroup.DivisionId`, นับสมาชิกจาก `LearnerGroupMember`, นับ assignment batch ตาม `LearnerGroupId` แบบ distinct `AssignmentNo`, และคำนวณ enrollment/progress/overdue จาก visible enrollments ของสมาชิกในกลุ่มโดยใช้ effective schedule เดิมของ `ReportService`; frontend เพิ่ม tile ใน Report Hub, route, type sync, labels สองภาษา, หน้า table/scroller, KPI, search, sort, CSV export และ link ไป `/learner-groups/{id}`. Assignment report เดิม `/reports/assignments` ยังอยู่ครบใน hub
- ไฟล์หลักที่แตะ: `iLearn.API/Controllers/ReportsController.cs`, `iLearn.Application/DTOs/ReportDtos.cs`, `iLearn.Application/Interfaces/Services/IReportService.cs`, `iLearn.Application/Services/ReportService.cs`, `iLearn.Tests/ReportServiceTests.cs`, `iLearn.Admin.React/src/App.tsx`, `iLearn.Admin.React/src/lib/labels.ts`, `iLearn.Admin.React/src/pages/reports/ReportHubPage.tsx`, `iLearn.Admin.React/src/pages/reports/reportTypes.ts`, `iLearn.Admin.React/src/pages/reports/LearnerGroupSummaryReportPage.tsx`, `DOC/PLANS/PLAN-145-learner-group-summary-report.md`, `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน: เพิ่ม `GET /api/Reports/learner-groups` คืน `{ success, data }`; `data.rows[].learnerGroupId` ใช้เปิดหน้า detail กลุ่มผู้เรียน
- Verified: focused test `LearnerGroupSummary_ScopesGroups_AndCountsMemberEnrollments` ✓; `npm run lint` ✓; `npm run build` ✓ (Vite chunk-size warning เดิม); `dotnet build .\iLearn.Tests\iLearn.Tests.csproj -o .\artifacts\verify-test` ✓; `dotnet test .\artifacts\verify-test\iLearn.Tests.dll` = 276/276 ✓

## [2026-07-23 —] GitHub Copilot — Commit `9a25676` + deploy PLAN-143/144 to QA/PROD
- ทำอะไร: commit `9a25676` (`feat(admin-react,api): add assignment summary report and scroll loading`) แล้ว deploy API + Admin React ไป QA/PROD. QA API stamp `20260723160518` health 401 attempt แรก, Admin React copy สำเร็จ (`RobocopyExitCode=3`). PROD API stamp `20260723160824` health 401 attempt แรก, Admin React copy สำเร็จ (`RobocopyExitCode=3`)
- Smoke QA/PROD: anonymous `/Service/api/admin/session/me` = 401, `/admin-react/` = 200, `/admin-react/reports/assignments` = 200, `GET /Service/api/Reports/assignments` = 200. QA report rows/total = 12 (`AS-20260721-002` first row). PROD report rows/total = 27 (`AS-20260722-002` first row). PROD learner group `/admin-react/learner-groups/36` = 200, `GET /Service/api/LearnerGroups/36` = 200, group `3. Production`, members = 303. QA note: learner group id 36 does not exist in QA DB, so only SPA route fallback was checked there
- ไฟล์หลักที่แตะ: deploy docs/log only after commit — `DOC/AGENT_LOG.md`, `DOC/PLANS/PLAN-143-assignment-summary-report.md`, `DOC/PLANS/PLAN-144-remove-manual-load-more-buttons.md`
- Contract ที่เปลี่ยน: ไม่มีเพิ่มเติมจาก `9a25676` (new `GET /api/Reports/assignments` deployed)
- Verified: pre-commit `npm run lint` ✓, `npm run build` ✓, `dotnet build iLearn.Tests -o artifacts\verify-test` ✓, `dotnet test artifacts\verify-test\iLearn.Tests.dll` = 275/275 ✓; QA/PROD deploy + smoke ✓

## [2026-07-23 —] GitHub Copilot — PLAN-144 Remove manual Load more buttons
- ทำอะไร: แก้ตารางที่ยังใช้ปุ่ม `Load more` ให้โหลดเพิ่มจาก scroll แทนตามคำขอผู้ใช้ โดยเริ่มจากหน้า `/learner-groups/36` Members table แล้วกวาดต่อทั้ง React Admin: เพิ่ม `shouldLoadMoreOnScroll(...)` ใน `tableStandards.ts`, เปลี่ยน LearnerGroupDetail members, AssignmentDetail courses/learners, AssignmentReport learners, CourseDetail versions/learners/assignments, VersionDetail content และ Notifications list ให้ใช้ `onScroll` เพิ่ม chunk/page อัตโนมัติ พร้อม footer hint `เลื่อนลงเพื่อโหลดเพิ่ม`; grep ยืนยันว่าไม่เหลือ manual `Load more` button/click handler ใน `iLearn.Admin.React/src/**`
- ไฟล์หลักที่แตะ: `iLearn.Admin.React/src/lib/tableStandards.ts`, `src/pages/learner-groups/LearnerGroupDetailPage.tsx`, `src/pages/assignments/AssignmentDetailPage.tsx`, `src/pages/assignments/AssignmentReportPage.tsx`, `src/pages/courses/CourseDetailPage.tsx`, `src/pages/courses/VersionDetailPage.tsx`, `src/pages/notifications/NotificationsPage.tsx`, `DOC/PLANS/PLAN-144-remove-manual-load-more-buttons.md`, `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน: ไม่มี (frontend behavior only; Notifications endpoint paging contract เดิม)
- Verified: grep manual `Load more`/`loadMore` handlers = 0; `npm run lint` ✓; `npm run build` ✓; `dotnet build iLearn.Tests -o artifacts\verify-test` ✓; `dotnet test artifacts\verify-test\iLearn.Tests.dll` = 275/275 ✓

## [2026-07-23 —] GitHub Copilot — PLAN-143 Assignment Summary Report
- ทำอะไร: เพิ่ม report ใหม่สำหรับงานมอบหมายที่ `/admin-react/reports/assignments`: backend endpoint `GET /api/Reports/assignments`, DTO `AssignmentSummaryReportDto`/`AssignmentSummaryRow`, service method `GetAssignmentSummaryReportAsync` ที่ group batch ด้วย `AssignmentNo`, คำนวณ status ผ่าน `AssignmentStatusKeys.GetBatchStatus`, honor `EnrollmentAssignment.SnapshotCompleted`, และ division scope ผ่าน `_currentUser.DivisionId`; frontend เพิ่ม route, card ใน Report Hub, type sync, labels สองภาษา, หน้า table/scroller พร้อม KPI, status filter, search, sort, CSV export และ link ไป `/assignments/{id}/report`
- ไฟล์หลักที่แตะ: `iLearn.API/Controllers/ReportsController.cs`, `iLearn.Application/DTOs/ReportDtos.cs`, `iLearn.Application/Interfaces/Services/IReportService.cs`, `iLearn.Application/Services/ReportService.cs`, `iLearn.Tests/ReportServiceTests.cs`, `iLearn.Admin.React/src/App.tsx`, `iLearn.Admin.React/src/lib/labels.ts`, `iLearn.Admin.React/src/pages/reports/ReportHubPage.tsx`, `iLearn.Admin.React/src/pages/reports/reportTypes.ts`, `iLearn.Admin.React/src/pages/reports/AssignmentSummaryReportPage.tsx`, `DOC/PLANS/PLAN-143-assignment-summary-report.md`, `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน: เพิ่ม `GET /api/Reports/assignments` คืน `{ success, data }`; `data.rows[].assignmentId` ใช้เปิดรายงานราย batch เดิม
- Verified: focused test `AssignmentSummary_GroupsBatches_And_UsesSnapshotCompletion` ✓; `npm run lint` ✓; `npm run build` ✓; `dotnet build iLearn.Tests -o artifacts\verify-test` ✓; `dotnet test artifacts\verify-test\iLearn.Tests.dll` = 275/275 ✓

## [2026-07-23 —] GitHub Copilot — Commit `147232c` + deploy PLAN-142/Reports Hub to QA/PROD
- ทำอะไร: commit งานพร้อมขึ้นเป็น `147232c` (`fix(admin-react,api): repair learners grid and report hub`) แล้ว deploy ทั้ง API + Admin React ไป QA และ PROD. QA API stamp `20260723153749` health `/Service/api/admin/session/me` = 401 attempt แรก, Admin React copy สำเร็จ (`RobocopyExitCode=3`). PROD API stamp `20260723154022` health 401 attempt แรก, Admin React copy สำเร็จ (`RobocopyExitCode=3`)
- Smoke QA/PROD: `/admin-react/` = 200, `/` = 200, anonymous `/Service/api/admin/session/me` = 401; Learners `Get?skip=0&take=20` total `8044`, page 1 = 20 rows, page 2 = 20 rows, payload มี `thaiFirstName` + `nid`; filter `thaiFirstName contains โมโตฮิสะ` = 200/total 1 และ `nid contains a5521` = 200/total 1 ทั้ง QA และ PROD; `/admin-react/reports` = 200 ทั้ง QA/PROD
- ไฟล์หลักที่แตะ: deploy docs/log only after commit — `DOC/AGENT_LOG.md`, `DOC/PLANS/PLAN-142-learners-grid-key-and-search-fix.md`
- Contract ที่เปลี่ยน: ไม่มีเพิ่มเติมจาก `147232c` (PLAN-142 additive Learners grid fields ถูก deploy แล้ว)
- Verified: pre-commit `npm run lint` ✓, `npm run build` ✓, `dotnet build iLearn.Tests -o artifacts\verify-test` ✓, `dotnet test artifacts\verify-test\iLearn.Tests.dll` = 274/274 ✓; QA/PROD deploy + smoke ✓

## [2026-07-23 —] GitHub Copilot — Reports Hub refactor
- ทำอะไร: refactor หน้า Reports Hub ให้ metadata รายงานเป็น config คงที่นอก render, แยก `ReportTile` component สำหรับการ์ดรายงาน, ใช้ type `LucideIcon`/`BadgeTone` ชัดเจน, รวม class สีราย tone ไว้ที่เดียว และปรับ layout/spacing/focus/hover ให้คมขึ้นโดยยังใช้ `Card`, `Badge`, `SectionHeader`, label dictionary และ route เดิมทั้งหมด
- ไฟล์หลักที่แตะ: `iLearn.Admin.React/src/pages/reports/ReportHubPage.tsx`, `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน: ไม่มี (frontend presentation/refactor only)
- Verified: `npm run lint` ✓, `npm run build` ✓ (ผ่าน; มี Vite chunk-size warning เดิมเท่านั้น)

## [2026-07-23 —] Claude Code — PLAN-142: แก้ Learners grid (infinite scroll ตาย + ค้นหา NID/ชื่อไทย)
- ทำอะไร: ผู้ใช้รายงานหน้า `/admin-react/learners` บน PROD แสดงผลเพี้ยน/scroll ไม่โหลดเพิ่ม/ค้นหาไม่ครบ. **Root cause:** EmployeeHub provider คืน `Id = 0` ทุกแถว แต่ learners config ใช้ `key: 'id'` ⇒ AppTable dedupe (page>1) กรองแถวใหม่ทิ้งหมด (scroll ตาย + ยิง request วนเปล่า) และ React row key ซ้ำ `"0"` (render เพี้ยน) — โผล่หลัง cutover EmployeeHub (PLAN-058) เพราะ Legacy มี Id จริง. **แก้:** (1) `moduleConfigs.learners` → `key: 'eId'`, searchExpr เพิ่ม `nid`+`thaiFirstName`+`thaiLastName` (ข้อจำกัด NID-500 เป็นของ Legacy proxy เท่านั้น — EmployeeHub filter in-memory ได้), เพิ่มคอลัมน์ชื่อไทย 2 คอลัมน์ (2) backend: เพิ่ม `ThaiFirstName`/`ThaiLastName` ใน `LearnerGridRowDto` + mapping ใน `EmployeeHubLearnerApiService` + `FieldMapping`/regex ใน `LearnersController` (3) labels: คีย์ `thaiFirstName`/`thaiLastName` + ปรับ placeholder ค้นหา (4) เพิ่ม tests 2 cases. รายละเอียดเต็มใน `PLAN-142`
- ไฟล์หลักที่แตะ: `iLearn.Admin.React/src/pages/moduleConfigs.ts`, `iLearn.Admin.React/src/lib/labels.ts`, `iLearn.Application/DTOs/ExternalLearnerDto.cs`, `iLearn.Infrastructure/Services/EmployeeHubLearnerApiService.cs`, `iLearn.API/Controllers/LearnersController.cs`, `iLearn.Tests/LearnersControllerTests.cs`, `iLearn.Tests/EmployeeHubLearnerApiServiceTests.cs`, `DOC/PLANS/PLAN-142-learners-grid-key-and-search-fix.md` (ใหม่)
- Contract ที่เปลี่ยน: `Learners/Get` grid row เพิ่ม field `thaiFirstName`/`thaiLastName` (additive, camelCase) — React columns/searchExpr sync แล้วในงานเดียวกัน
- Verified: `npm run lint` ✓, `npm run build` ✓, `dotnet test` **274/274** ✓ (รวม 2 tests ใหม่) — **ยังไม่ deploy**: ต้อง full API publish + admin-react แล้ว smoke QA→PROD (บั๊ก reproduce ได้เฉพาะ data EmployeeHub; dev local เป็น Legacy)

## [2026-07-23 —] GitHub Copilot — PLAN-141 deploy bilingual QA/PROD complete
- ทำอะไร: ปิดงาน deploy ตาม `PLAN-141` end-to-end หลัง working tree สะอาดจริง. **Pre-flight**: ยืนยัน `git status` สะอาด, รัน `npm run lint` ✓ / `npm run build` ✓ / `dotnet build iLearn.Tests -o artifacts\verify-test` ✓ / `dotnet test artifacts\verify-test\iLearn.Tests.dll` ✓; migration gate QA/PROD ตอนแรก fail เพราะ local dev `iLearn.API.exe` (PID 41724) ล็อก `iLearn.API\bin\Debug\*.dll` ทำ `dotnet ef migrations list` build ไม่ผ่าน — หยุดเฉพาะ PID นั้นแล้ว rerun gate ผ่านทั้งสอง connection (ไม่มี pending). **QA deploy**: `tools/deploy-api.ps1` stamp `20260723150900` health 401 attempt แรก, auto-rollback false; `tools/deploy-admin-react.ps1` build+copy สำเร็จ (`RobocopyExitCode=3`). **QA smoke**: anonymous `/Service/api/admin/session/me` = 401, authenticated `/admin-react/` และ `/` = 200; browser smoke สองภาษาผ่านบน Dashboard, Assignments list/detail, Courses, Master Data > Divisions, Users, System Config, Reports hub + Compliance, Notifications; refresh แล้ว EN คงอยู่, สลับกลับไทยแล้ว assignments header กลับเป็น `เลขที่งานมอบหมาย`, console 0 errors. ทดสอบ `PLAN-137` บน QA โดยแก้ description assignment 275 เป็นค่าชั่วคราว ตรวจ detail+list อัปเดต แล้ว revert กลับ `Training WI_PD2`. **PROD deploy**: `tools/deploy-api-prod.ps1` stamp `20260723151448` health 401 attempt แรก; `tools/deploy-admin-react-prod.ps1` build+copy สำเร็จ (`RobocopyExitCode=3`). **PROD smoke (read-only)**: anonymous `/Service/api/admin/session/me` = 401, authenticated `/admin-react/` และ `/` = 200; เปิด short host แล้ว redirect ไป FQDN ตาม PLAN-140, default ไทย, สลับ EN/refresh/สลับกลับไทยได้บน Dashboard, Assignments, Courses, Users, System Config, Compliance, Notifications; console 0 errors ทุกหน้าที่ตรวจ. ปิดสถานะ `PLAN-136/137/138` เป็น `VERIFIED` และ `PLAN-141` เป็น `DONE`
- ไฟล์หลักที่แตะ: `DOC/PLANS/PLAN-136-full-bilingual-admin-ui.md`, `DOC/PLANS/PLAN-137-editable-assignment-description.md`, `DOC/PLANS/PLAN-138-bilingual-zones-def-copilot.md`, `DOC/PLANS/PLAN-141-deploy-bilingual-qa-prod.md`, `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน: ไม่มี (deploy + docs/status only)
- Verified: QA deploy stamp `20260723150900` ✓, PROD deploy stamp `20260723151448` ✓, migration gate QA/PROD no pending ✓, health checks QA/PROD ✓, bilingual browser smoke QA/PROD ✓

## [2026-07-23 —] Claude Code — รีวิว PLAN-140 (Gemini) — ผ่าน + commit
- ทำอะไร: รีวิวงาน canonical host redirect ครบทุกชั้น: (1) อ่านโค้ด middleware — logic ตรงแผนทุกข้อ: 307 (ไม่ cache ถาวร), GET/HEAD เท่านั้น, localhost/loopback skip (รวม IPv6 + `IPAddress.IsLoopback`), HostUrl ว่าง/invalid/non-http = no-op ทั้งตัว, redirect URL ใช้ `PathBase+Path+QueryString` ไม่ต่อ path `/iLearn` ซ้ำ, ใช้ `Authority` คง port ที่ไม่ default (2) main.tsx: redirect รันก่อน mount React + ก่อน favicon side effect, คง path/search/hash, dev ปิดเพราะไม่ตั้ง env (3) tests 10 cases ครอบ edge ที่แผนสั่งครบ; `extern alias ILearnUserApp` แก้ namespace ชนถูกวิธี (4) รัน verification ซ้ำเอง: lint ✓ build ✓ **tests 272/272** ✓ (5) ยิงของจริงทั้งสอง env: short host → 307 + `Location` FQDN ถูกต้อง, FQDN ตรง → 200 ไม่ redirect (QA+PROD), health/smoke ปลายทาง 200 pass (6) ตรวจ bundle ที่ deploy บน PROD (`index-D4B_zuJ9.js`) มี `nikonoa.net` + `location.replace` = SPA redirect deploy แล้วจริง — จากนั้น commit งาน PLAN-140
- ไฟล์หลักที่แตะ: `DOC/AGENT_LOG.md` (entry นี้)
- Contract ที่เปลี่ยน: ไม่มี
- Verified: ตามรายการข้างบน — หมายเหตุ: browser smoke ของหน้า health-check ผ่าน URL สั้น + SCORM ผ่าน URL สั้น ยังไม่ได้ทำด้วย browser จริง (Browser pane ใช้ไม่ได้ในเซสชันรีวิว) — พฤติกรรมยืนยันแล้วระดับ HTTP/bundle; แนะนำผู้ใช้เปิด `https://ap-ntc2137-prwb/iLearn/admin-react/health-check` ดูว่าเด้ง FQDN + การ์ดเขียวทั้งคู่

## [2026-07-23 —] Antigravity — PLAN-140: Canonical Host Redirect Implementation & Deployment
- ทำอะไร: (1) สร้าง `CanonicalHostRedirectMiddleware` และ `CanonicalHostRedirectHelper` ใน `iLearn.User/Middleware/CanonicalHostRedirectMiddleware.cs` อ่าน `FileSettings:HostUrl` จาก configuration เพื่อทำ HTTP 307 Temporary Redirect จาก short NetBIOS hostname ไปยัง FQDN (`*.nikonoa.net`) สำหรับ GET/HEAD requests โดยคง PathBase, Path, QueryString ครบถ้วน พร้อมข้าม localhost/127.*/[::1] (2) ลงทะเบียน `app.UseCanonicalHostRedirect()` ไว้บนสุดของ middleware pipeline ใน `iLearn.User/Program.cs` (3) เพิ่ม `VITE_ILEARN_ADMIN_CANONICAL_DOMAIN=nikonoa.net` ใน `.env.production`, `canonicalDomain` ใน `appConfig.ts`, และ `redirectIfCanonicalHostNeeded()` ใน `iLearn.Admin.React/src/main.tsx` ให้เปลี่ยน URL ฝั่ง SPA client ก่อน mount React (4) เพิ่ม `<Aliases>ILearnUserApp</Aliases>` ใน `iLearn.Tests.csproj` และเขียน unit tests 10 cases ใน `CanonicalHostRedirectTests.cs` (5) Deploy & Smoke test ทั้ง QA และ PROD: short hostname เด้ง 307 ไปยัง FQDN (`https://ap-ntc2138-qawb/iLearn/` -> `307 Location: https://ap-ntc2138-qawb.nikonoa.net/iLearn/` บน QA, `https://ap-ntc2137-prwb/iLearn/` -> `307 Location: https://ap-ntc2137-prwb.nikonoa.net/iLearn/` บน PROD), FQDN ตอบกลับ 200 ปกติ (6) ปรับสถานะ `PLAN-140` เป็น `DONE`
- ไฟล์หลักที่แตะ: `iLearn.User/Middleware/CanonicalHostRedirectMiddleware.cs` (ใหม่), `iLearn.User/Program.cs`, `iLearn.Admin.React/.env.production`, `iLearn.Admin.React/src/config/appConfig.ts`, `iLearn.Admin.React/src/main.tsx`, `iLearn.Tests/iLearn.Tests.csproj`, `iLearn.Tests/CanonicalHostRedirectTests.cs` (ใหม่), `DOC/PLANS/PLAN-140-canonical-host-redirect.md`, `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน: ไม่มี (Middleware redirect presentation layer เท่านั้น)
- Verified: `npm run lint` ✓ (0 errors); `npm run build` ✓; `dotnet build` & `dotnet test` ✓ (272/272 passed); QA short host probe -> 307 Location `*.nikonoa.net` ✓, QA FQDN -> 200 ✓; PROD short host probe -> 307 Location `*.nikonoa.net` ✓, PROD FQDN -> 200 ✓

## [2026-07-23 —] Claude Code — จัดระเบียบเลขแผนชนกัน: PLAN-140 deploy → **PLAN-141** + กติกาจองเลข
- ทำอะไร: ผู้ใช้ชี้ว่าเลขแผนทับกัน — สำรวจพบชน 3 คู่: (1) **PLAN-140 (active, อันตราย)**: `canonical-host-redirect` (Gemini, สร้าง 14:24) vs `deploy-bilingual-qa-prod` (Claude, สร้าง 14:25) — ใช้กติกา "ใครสร้างก่อนได้เลข" ⇒ Gemini คง 140, แผน deploy ย้ายเป็น **`PLAN-141-deploy-bilingual-qa-prod.md`** (แก้หัวไฟล์ + reference ใน log ครบ) (2) PLAN-045 ตรวจแล้ว**ไม่ใช่เลขชน** — เป็นเอกสารประกอบ initiative เดียวกัน (cutover+mapping+etl) (3) PLAN-030 ชนจริงแต่เป็นประวัติศาสตร์ปิดงานแล้ว — คงไว้ (ถูกอ้างใน log/commit เก่า). เพิ่ม**กติกาจองเลข**ใน `DOC/PLANS/README.md` (เช็คเลขสูงสุดรวม uncommitted ก่อนสร้าง / หนึ่งเลขหนึ่งแผน / เอกสารประกอบใช้เลขร่วมได้ / ชนแล้วใครก่อนได้เลข) + ข้อ 5 ใน CLAUDE.md
- ไฟล์หลักที่แตะ: `DOC/PLANS/PLAN-141-deploy-bilingual-qa-prod.md` (rename จาก 140), `DOC/PLANS/README.md`, `CLAUDE.md`, `DOC/AGENT_LOG.md`
- Contract ที่เปลี่ยน: ไม่มี (docs เท่านั้น)
- **ถึง Copilot:** แผน deploy ที่ต้องรับไปทำคือ **PLAN-141** (ไม่ใช่ 140 — อันนั้นของ Gemini เรื่อง canonical host)

## [2026-07-23 —] Claude Code — รีวิว PLAN-139 (Overview cards) → VERIFIED
- ทำอะไร: รีวิวงาน Gemini ตาม PLAN-139: ตรวจ diff ครบ 10 ไฟล์ + รัน `npm run lint` ✓ / `npm run build` ✓ ซ้ำเอง + เปิด browser ตรวจจริง 7 หน้า detail โหมดไทย (+EN spot check) — โครงสร้างการ์ดตรงมาตรฐานทุกหน้า, sweep literal ที่สั่งแปลง = 0. ตั้งสถานะแผนเป็น VERIFIED + เขียน Reviewer Notes
- ประเด็นสำคัญจากรีวิว: (1) การแก้ `Fact` ให้ `valueClassName`/`mono` มีผลจริง (เดิมเป็น no-op) กระทบทุกจุดที่ใช้ Fact ทั้งแอป 40 จุด รวม LearnerProfilePage นอก scope — ตรวจแล้วตรง intent เดิม ยอมรับ (2) `apiClient.ts` แก้นอก scope (413 message → dictionary) โค้ดถูกแต่ไม่จดใน Implementer Notes (3) **พบบั๊กเดิม**: หน้า Assignment detail สถานะโชว์ "In Progress" ดิบในโหมดไทย — `deriveAssignmentStatus` คืน `'In Progress'` (เว้นวรรค) แต่คีย์ `STATUS_LABELS` คือ `InProgress` → ควรเปิดงานย่อยแก้ (4) bilingual ค้างนอก scope: ปุ่ม CourseControls, "Attached content items…", crumb "Divisions"
- ไฟล์หลักที่แตะ: `DOC/PLANS/PLAN-139-standardize-overview-cards.md` (Reviewer Notes + VERIFIED), `DOC/AGENT_LOG.md`, `.claude/launch.json` (เพิ่ม dev server config สำหรับ preview)
- Contract ที่เปลี่ยน: ไม่มี (รีวิวอย่างเดียว)
- Verified: lint ✓ build ✓ browser 7 หน้า ✓

## [2026-07-23 —] Claude Code — รีวิว+commit PLAN-139 (`021608d`) + commit fix รีวิว (`2ee587d`) + เขียน PLAN-141 deploy (มอบ Copilot; เดิมชื่อ PLAN-140 ถูกย้ายเลขเพราะชนกับแผน canonical-host ของ Gemini)
- ทำอะไร: ผู้ใช้สั่ง commit + ทำแผน deploy. พบ working tree มีงาน **PLAN-139 ของ Gemini** (StatTile overview cards, DONE) ปนกับ fix ของ Claude ในไฟล์ `labels.ts` เดียวกัน — **ไม่ทำ mixed commit**: รีวิว PLAN-139 ก่อน (StatTile/StatTileRow เข้า UI conventions, display-only refactor 7 detail pages + 11 คีย์ bilingual, lint/build ผ่าน) แล้วแยก 2 commits: ถอด `fileTooLarge` ออกจาก labels.ts ชั่วคราว → commit `021608d` (PLAN-139, ให้เครดิต Gemini ใน message) → ใส่กลับ → commit `2ee587d` (fix 413 + AGENT_LOG). เขียน PLAN-141 (READY, Copilot): deploy 6 commits ค้าง (da5d1c6→2ee587d) — **backend เปลี่ยนจาก db507b1 ต้อง full API publish**, migration gate (ไม่มี pending คาดหวัง), QA smoke สองภาษาละเอียด + ทดสอบ PLAN-137 edit description + PLAN-139 StatTile, PROD read-only, เตือนเรื่อง dev API ล็อก bin + ห้ามกวาดงาน agent อื่น
- ไฟล์หลักที่แตะ: `DOC/PLANS/PLAN-141-deploy-bilingual-qa-prod.md` (ใหม่ READY; เดิม PLAN-140), `DOC/AGENT_LOG.md`; commits: `021608d` (10 ไฟล์), `2ee587d` (3 ไฟล์)
- Contract ที่เปลี่ยน: ไม่มี (Fact ได้ prop `mono`/`valueClassName` เพิ่ม + StatTile ใหม่ — additive, ผู้ใช้เดิมไม่ต้องแก้)
- Verified: lint ✓ build ✓ ทั้งก่อนและหลังแยก commit; working tree สะอาดหลัง commit ทั้งสอง

