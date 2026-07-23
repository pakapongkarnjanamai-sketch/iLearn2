# PLAN-136: เฟสสองภาษาเต็มรูปแบบ (TH/EN) — iLearn.Admin.React ทั้งแอป

- **Status**: IN_PROGRESS
- **Assigned**: Claude Code (โซน P0/A/B/C — เสร็จแล้ว) → **โซน D/E/F มอบ GitHub Copilot ตาม PLAN-138** (ผู้ใช้สั่งเปลี่ยน 2026-07-23; Claude เปลี่ยนบทบาทเป็น reviewer)
- **Created**: 2026-07-23

## การตัดสินใจของผู้ใช้ (ยืนยันแล้ว)

1. **ขอบเขต**: UI ฝั่ง `iLearn.Admin.React` ทั้งหมด — ข้อความจาก backend (error/notification) ยังเดิม, ฝั่ง Learner ไม่รวม
2. **Dictionary**: **ไฟล์เดียว** — ขยาย `src/lib/labels.ts` (จัด section ตามโซน ไม่แตกไฟล์)
3. **Persistence**: `localStorage` (key `ilearn-admin-lang`, default `th`)
4. **ผู้ทำ**: Claude ทั้งหมด — ไล่ทีละโซน แอปต้อง lint/build ผ่านและใช้งานได้จริงหลังจบทุกโซน (ข้อความที่ยังไม่ migrate แสดงค่า hardcode เดิม ไม่พัง)

## สถาปัตยกรรม (Phase 0)

- `labels.ts` เพิ่ม language store: module-level `currentLang` อ่านจาก localStorage + `getLang()`/`setLang()`/`subscribeLang()` + hook `useLang()` (ผ่าน `useSyncExternalStore`)
- `t(pair)` คงเป็น plain function เดิม — ทุก call site resolve ตอน render อยู่แล้ว (บังคับตั้งแต่ PLAN-133/134)
- **Re-render strategy**: `AppLayoutInner` ใส่ `key={lang}` ที่ root div → สลับภาษา = remount ทั้ง tree ใต้ layout — การันตีไม่มีข้อความค้าง ยอมแลก transient state (search/filter ที่ยังไม่ save) หายตอนสลับ ซึ่งเกิดนาน ๆ ครั้ง
- **Switcher**: `SegmentedToggle` (variant segment) ใน Header ข้าง NotificationBell — ตัวเลือก `TH`/`EN`
- ข้อความมี parameter ใช้ `tf(pair, ...values)` แทนที่ `{0}`,`{1}` ใน string

## โซน migrate (อัปเดตสถานะทุกครั้งที่จบโซน)

| โซน | ไฟล์ | ทิศทาง | สถานะ |
|---|---|---|---|
| **P0** Infra + switcher | labels.ts, Header, AppLayout | — | ✅ 2026-07-23 (verified ในเบราว์เซอร์: th↔en + localStorage + badge เดิมสลับตาม) |
| **A** Layout + shared UI | navigation.ts (+Sidebar/Breadcrumbs), ListToolbar, ConfirmDialog, AppWizard, AppTable/Footer/Search, ExplorerTable, Header strings | en→+th | ✅ 2026-07-23 — **ยกเว้น** EntityListPage buttons + moduleConfigs (title/eyebrow/description/caption ~60 คอลัมน์ ผูกกับ type `AdminGridColumn` → ทำในรอบโซน list ถัดไป) |
| **B** Dashboard | DashboardPage, DashboardCharts | th→+en | ✅ 2026-07-23 — `DASHBOARD_LABELS` ~45 คีย์; แก้เพิ่ม: หัวตารางสองภาษาปนกันเดิม ("งานมอบหมาย (Assignment)") → ภาษาเดียวตามสวิตช์, badge สถานะที่เคยโชว์ key ดิบ (`Active`/`Due Soon` จาก DashboardService) → เพิ่มเข้า `STATUS_LABELS` + ผ่าน `learnerStatusLabel`, `formatRelativeTime` ใน format.ts ทำเป็น lang-aware (เมื่อครู่ ↔ just now) — verified ทั้ง th/en ในเบราว์เซอร์, console 0 errors |
| **C** Reports (5 หน้า) | ReportHub, Compliance, Transcript, Activity, CourseSummary | th→+en | ✅ 2026-07-23 — `REPORT_LABELS` ~95 คีย์; Hub การ์ดแสดงชื่อภาษาหลัก + อีกภาษาในวงเล็บ (สลับตาม lang); หัวตาราง "ไทย (English)" ปนกันเดิม → ภาษาเดียวทุกหน้า; **บั๊กเดิม**: Transcript badge สถานะโชว์ key ดิบ → `learnerStatusLabel`; CourseSummary (อังกฤษล้วนเดิม) ได้ไทยครบ; ยกเว้น: CSV export headers + print-only transcript header คงอังกฤษ (ไฟล์/เอกสารทางการ) — verified th/en ทั้ง 5 หน้า, console 0 errors |
| **D** Courses + Content | CourseList/Detail/Editor, VersionDetail/Form, ContentItemDetail/Editor | ผสม | ✅ 2026-07-23 — `COURSE_LABELS`; modal/toast/confirm/explorer/stat text migrated; force-delete and category-modal copy included |
| **E** Assignments + Learners | AssignmentDetail/Report/Gantt/Bulk, LearnerGroupList/Detail/Editor, LearnerList/Profile, LearnerDirectorySelector | en→+th | ✅ 2026-07-23 — `ASSIGNMENT_LABELS` + `LEARNER_LABELS`; assignment and learner-group workflows including PLAN-137 description modal migrated |
| **F** Master Data + Users + System + misc | MasterData*, AdminUsers/UserEditor/UserDetail, SystemConfig, HealthCheck (CHECK_LABELS), Notifications, NotFound/AccessDenied, EntityListPage, moduleConfigs captions | en→+th | ✅ 2026-07-23 — `ADMIN_LABELS`; module-level configs now retain `LabelPair` and resolve in `AppTable`/page renderers |

## กติกา migrate ต่อโซน

- คีย์เป็น camelCase อังกฤษ จัดกลุ่มเป็น exported object ต่อโซน (`NAV_LABELS`, `UI_LABELS`, `DASHBOARD_LABELS`, `REPORT_LABELS`, ...) — เรียกแบบ `t(DASHBOARD_LABELS.overdueTasks)` ให้ TS คุม typo
- ห้ามแปล: ค่า data จาก API, ชื่อ technical (SCORM, NID, CSV), format วันที่/ตัวเลข (`lib/format.ts` คงเดิมทั้งสองภาษา — ตัดสินใจแยกทีหลังถ้าจะทำ)
- toast ที่เป็นข้อความ frontend เอง = แปล; ข้อความ error จาก backend ที่ throw ต่อมา = แสดงตามเดิม
- `navigation.ts` เปลี่ยน `label: string` → `LabelPair` แล้ว Sidebar/Breadcrumbs `t()` ตอน render — เช็ค consumer ของ label ทุกตัว (รวม Breadcrumbs matching)
- จบทุกโซน: `npm run lint` + `npm run build` + สลับภาษาดูหน้าที่แก้ในเบราว์เซอร์ทั้ง 2 ภาษา

## Verification ปิดงานรวม

- lint/build ผ่าน, สลับ TH↔EN แล้วข้อความเปลี่ยนครบทุกหน้า, refresh แล้วค่าภาษาคงอยู่ (localStorage), ไม่มีข้อความปนสองภาษาในหน้าเดียว (ยกเว้นชื่อ technical), console 0 errors
- Badge/status เดิมจาก PLAN-133/134 ต้องสลับตามด้วย (มี en อยู่แล้ว — ได้ฟรีจาก switcher)

## Out of Scope

- Backend i18n, ฝั่ง Learner, locale ของ format วันที่/ตัวเลข, ภาษาที่สาม
- ไม่เพิ่ม dependency ใหม่ (ไม่ใช้ react-i18next — pattern เดิมพอ)
