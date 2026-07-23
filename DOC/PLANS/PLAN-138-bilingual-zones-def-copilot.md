# PLAN-138: เฟสสองภาษา โซน D–F (ต่อจาก PLAN-136) — มอบ Copilot

- **Status**: DONE
- **Assigned**: GitHub Copilot (GPT)
- **Created**: 2026-07-23
- **อ้างอิง**: PLAN-136 (แผนแม่บท — โซน P0/A/B/C เสร็จแล้วโดย Claude, commit `da5d1c6` + `db507b1`)

## Overview

ทำเฟสสองภาษา (TH/EN) ต่อให้จบ 3 โซนที่เหลือ ตามสถาปัตยกรรมที่วางไว้แล้วใน PLAN-136 — **infra เสร็จหมดแล้ว** (language store + switcher + remount) งานที่เหลือคือ migrate string ของแต่ละหน้าเข้า dictionary `src/lib/labels.ts` เท่านั้น ห้ามแก้สถาปัตยกรรม

## Pattern ที่ต้องทำตาม (จากโซนที่เสร็จแล้ว — ดูตัวอย่างจริงใน `pages/reports/*` และ `DashboardPage.tsx`)

1. เพิ่ม section ต่อท้าย `labels.ts` ต่อโซน: `COURSE_LABELS` (D), `ASSIGNMENT_LABELS` + `LEARNER_LABELS` (E), `ADMIN_LABELS` (F) — ทุก entry เป็น `{ th, en }` + `satisfies Record<string, LabelPair>`
2. **Reuse ก่อนเพิ่มใหม่**: คีย์กลางมีแล้วเยอะ — `UI_LABELS` (search/showing/records/confirm/cancel), `NAV_LABELS`, `CRUMB_LABELS`, `REPORT_LABELS` (colLearner/colDivision/exportCsv/rowsShowing...), `DASHBOARD_LABELS` (colStatus/colDueDate/colCourse...), สถานะทุกตัวผ่าน `learnerStatusLabel()` / `courseStatusLabel()` / `COMMON_LABELS`
3. Render ด้วย `t(pair)`; string มี placeholder ใช้ `tf(pair, ...values)` (`{0}`, `{1}`); ต้องการภาษาปัจจุบันนอก JSX ใช้ `getLang()`
4. หัวข้อที่เดิมเขียนสองภาษาปนกัน เช่น `"สถานะ (Status)"` → เหลือภาษาเดียวผ่าน `t()`
5. Badge ที่รับ status key ดิบจาก API (`Completed`, `Active`, ...) → ห่อ `learnerStatusLabel(...)` เสมอ (เจอบั๊กแบบนี้แล้ว 3 จุดในโซนก่อน ๆ)

### ⚠️ กับดักสำคัญที่สุด: ห้ามเรียก `t()` ใน module scope

`t()` resolve ค่า ณ เวลาที่ถูกเรียก — ถ้าเรียกตอน module load ผลจะค้างเป็นภาษาแรกตลอด สลับภาษาแล้วไม่เปลี่ยน (remount ไม่ช่วย เพราะ object เก็บ string ที่ resolve ไปแล้ว) ⇒ เก็บ `LabelPair` ไว้ใน config/map แล้ว `t()` ตอน render เท่านั้น — ดู pattern `SEGMENT_MAP` ใน `Breadcrumbs.tsx` และ `navigation.ts`

**จุดที่โดนกับดักนี้แน่ ๆ = `moduleConfigs.ts`** (Zone F): `adminListConfigs` เป็น module-level object — ต้องเปลี่ยน type `AdminGridColumn.caption` และ `AdminListConfig.title/eyebrow/description` เป็น `LabelPair` (หรือ `LabelPair | string` ช่วง migrate) แล้วให้ `EntityListPage`/`AppTable` เรียก `t()` ตอน render — grep ผู้ใช้ `caption`/`title` ทุกจุดก่อนเปลี่ยน type

## ขอบเขตต่อโซน (จากตาราง PLAN-136 — อัปเดตสถานะที่นั่นทุกครั้งที่จบโซน)

- **Zone D — Courses + Content**: `CourseListPage`, `CourseDetailPage`, `CourseEditorPage`, `VersionDetailPage`, `VersionFormPage`, `ContentItemDetailPage`, `ContentItemEditorPage` (สองไฟล์หลังมีบางส่วนเข้าระบบแล้วจาก PLAN-134 — เก็บส่วนที่เหลือ: toast, confirm, ControlAction, modal, stat tiles)
- **Zone E — Assignments + Learners**: `AssignmentDetailPage` (ไฟล์ใหญ่สุด ~1400 บรรทัด — รวมข้อความ modal ของ PLAN-137 ที่เพิ่งเพิ่ม), `AssignmentReportPage`, `AssignmentGanttPage`, `BulkAssignPage`, `AssignmentReportCharts`, `LearnerGroupListPage`, `LearnerGroupDetailPage`, `LearnerGroupEditorPage`, `LearnerListPage`, `LearnerProfilePage`, `components/shared/LearnerDirectorySelector`
- **Zone F — Master Data + Users + System + เศษที่เหลือ**: `MasterDataDetailPage`, `LearnerGroupCategoriesPage`, `LearnerGroupCategoryEditorPage`, `AdminUsersPage`, `UserEditorPage`, `UserDetailPage`, `SystemConfigPage`, `HealthCheckPage` (`CHECK_LABELS` → LabelPair), `NotificationsPage`, `NotificationBell`, `NotFoundPage`, `AccessDeniedPage`, `EntityListPage` (ปุ่ม Create/Schedule/Assign Courses), `moduleConfigs.ts` (ดูกับดักด้านบน), `UploadProgressOverlay`, `SectionHeader`/`ControlsSidebar` ถ้ามี default string

## กติกาคงเดิม (ห้ามฝ่าฝืน)

- **ไม่แปล**: ข้อมูลจาก DB/API, ชื่อ technical (SCORM, NID, EId, CSV), CSV export headers, print-only transcript header
- toast ของ frontend เอง = แปล; error message จาก backend = แสดงตามเดิม
- ไม่เพิ่ม dependency; ไม่แตะ `lib/format.ts` เพิ่ม (formatRelativeTime ทำแล้ว); ไม่แตะ backend
- ห้ามแก้ logic ใด ๆ ระหว่าง migrate — เปลี่ยนเฉพาะ display text

## Verification ต่อโซน (ก่อนขยับโซนถัดไป)

```powershell
# จาก iLearn.Admin.React
npm run lint
npm run build
```

- เปิดเบราว์เซอร์หน้าที่แก้ **ทั้ง 2 ภาษา** (สวิตช์ ไทย/EN ใน Header) — EN ต้องไม่มีไทยตกค้าง (ยกเว้นข้อมูล DB), th ครบ, console 0 errors
- กวาดท้ายโซน: `Select-String -Path src -Pattern "[ก-๙]" -Recurse | Where-Object Path -NotMatch "labels.ts"` — ผลที่เหลือต้องเป็นไฟล์นอกโซนหรือข้อมูล ไม่ใช่ display string ของโซนที่เพิ่งจบ

## ปิดงาน

- อัปเดตตารางโซนใน PLAN-136 (D/E/F → ✅) + PLAN-138 → `DONE` + Implementer Notes
- ลง `AGENT_LOG.md` ต่อโซนหรือรวบยอดตอนจบ
- Commit แยกจากงานค้างอื่นของ agent อื่นถ้ามีใน working tree (ตรวจ `git status` ก่อน stage เสมอ — commit `db507b1` รอบก่อนรวมงานสองทีมไปแล้ว อย่าให้ซ้ำ)
- **ยังไม่ deploy** จนกว่าผู้ใช้สั่ง

## Implementer Notes

- เพิ่ม `COURSE_LABELS`, `ASSIGNMENT_LABELS`, `LEARNER_LABELS`, และ `ADMIN_LABELS` ใน `src/lib/labels.ts` แล้ว migrate display copy ของ Zone D–F ตามขอบเขตทั้งหมด รวม toast, confirm dialog, modal, tooltip, placeholder, wizard, และ error/empty state ที่สร้างจาก frontend
- `AdminGridColumn.caption` และ `AdminListConfig.title`/`eyebrow`/`description` เปลี่ยนเป็น `LabelPair`; `AppTable`, `EntityListPage`, และ `MasterDataDetailPage` resolve ผ่าน `t()` ใน render/handler เท่านั้น จึงไม่มี `t()` ใน module scope
- คง API/DB data, backend errors, SCORM/NID/EId/CSV, และ CSV export headers ตามข้อยกเว้นของแผน
- Verification: `npm run lint` ผ่าน, `npm run build` ผ่าน, sweep `[ก-๙]` ใน Zone D–F ไม่พบ display string ค้าง (เหลือเฉพาะ raw backend error comparison), และ `git diff --check` ผ่าน. ไม่ได้ deploy หรือ commit
