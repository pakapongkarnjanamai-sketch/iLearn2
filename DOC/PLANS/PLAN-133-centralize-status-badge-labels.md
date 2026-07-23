# PLAN-133: รวมป้ายสถานะ/badge ทั้งแอปเข้าไฟล์กลาง `lib/labels.ts` (เตรียมสองภาษา)

- **Status**: VERIFIED
- **Assigned**: Claude Code (ผู้ใช้สั่งให้ทำเองใน session)
- **Created**: 2026-07-23
- **Completed**: 2026-07-23

## Overview

ผู้ใช้รายงานว่าป้ายกำกับ (status/badge) กระจายอยู่ทุกหน้า แปลไทยแล้วบางจุด ยังอังกฤษบางจุด — ต้องการรวมเข้า**ไฟล์เดียว**เพื่อเตรียมแผนทำสองภาษาในอนาคต

**การตัดสินใจของผู้ใช้:** (1) เก็บ **th/en คู่กัน**ตั้งแต่ตอนนี้ (2) ขอบเขต = **เฉพาะป้ายสถานะ/badge** (ไม่รวม caption คอลัมน์/ข้อความหน้าเพจ — เป็นเฟสถัดไป) (3) Claude implement เอง

## สิ่งที่ทำ

### ไฟล์ใหม่: `iLearn.Admin.React/src/lib/labels.ts` (single source of truth)

- `LabelPair = { th, en }` + `t(pair)` — `currentLang` fix เป็น `'th'`; เฟสสองภาษาแค่ทำให้ dynamic
- `LEARNER_STATUS_KEYS` / `STATUS_LABELS` / `learnerStatusLabel()` — ย้ายมาจาก `lib/learnerStatus.ts` (ลบไฟล์เดิมแล้ว) ยังคง mirror `AssignmentStatusKeys` ฝั่ง backend
- `statusTone()` — ย้ายมาจาก `StatusBadge.tsx`; จับคู่ canonical key ตรง ๆ ก่อน แล้ว fallback reverse-lookup ผ่าน `STATUS_LABELS` (รับได้ทั้ง key และข้อความแปลแล้ว th/en) ⇒ tone map ไม่มีวัน drift จาก label map
- `COURSE_STATUS_LABELS` / `courseStatusLabel()` / `getCourseStatusTone()` — ย้ายมาจาก `CourseStatusBadge.tsx`
- `READINESS_LABELS` — ย้ายมาจาก `ReadinessBadge.tsx`
- `COMMON_LABELS` — active/inactive/published/draft/assignable/notAssignable/all

### ไฟล์ที่แก้ให้ดึงจากไฟล์กลาง

- `components/ui/StatusBadge.tsx`, `CourseStatusBadge.tsx` — import + re-export helpers (ผู้ import เดิมไม่ต้องแก้)
- `components/ui/StatusText.tsx` — default labels resolve ตอน render ผ่าน `t(COMMON_LABELS.*)`
- `components/ui/ReadinessBadge.tsx` — ใช้ `READINESS_LABELS`
- `components/ui/AppTable.tsx` — boolean cell (ใช้งานอยู่/ปิดใช้งาน/มอบหมายได้/ไม่อนุญาต) ใช้ `COMMON_LABELS`
- `lib/chartTheme.ts` — **แก้ bug**: `STATUS_COLORS` เปลี่ยน key เป็น canonical (`InProgress`/`NotStarted` — เดิม `'In Progress'`/`'Not Started'` มีเว้นวรรค)
- `pages/assignments/AssignmentReportCharts.tsx` — **แก้ bug donut สีเทาหมด**: lookup สีด้วย `entry.status` (canonical key) แทน `entry.label` (ข้อความไทย → lookup พลาดทุกครั้ง → fallback `#64748b`)
- `pages/assignments/AssignmentDetailPage.tsx` — badge `'Completed'/'In Progress'` อังกฤษดิบ → `learnerStatusLabel(...)`
- `pages/assignments/AssignmentGanttPage.tsx` + `AssignmentReportPage.tsx` — ชิปตัวกรอง `'All'` → `t(COMMON_LABELS.all)` = "ทั้งหมด"
- `pages/users/UserDetailPage.tsx` + `pages/master-data/MasterDataDetailPage.tsx` — ตัด `activeLabel`/`inactiveLabel` ที่ซ้ำกับ default กลาง
- `pages/content-library/ContentItemDetailPage.tsx` — เผยแพร่แล้ว/ฉบับร่าง ผ่าน `COMMON_LABELS`
- import path `lib/learnerStatus` → `lib/labels` ทั้งหมด 6 ไฟล์ (EntityListPage, CourseDetailPage, AssignmentDetailPage, AssignmentGanttPage, AssignmentReportPage, AssignmentReportCharts)

## Out of Scope (เฟสถัดไป — แผนสองภาษา)

- caption คอลัมน์ใน `moduleConfigs.ts`, ข้อความหน้าเพจ/ปุ่ม/toast/heading ทั้งแอป
- language switcher + ทำ `currentLang` เป็น user setting (ตอนนั้นค่อยพิจารณา re-render strategy เพราะบางจุด resolve label ใน module scope ไม่ได้แล้ว — ทุกจุดตอนนี้ resolve ตอน render แล้ว)
- ฝั่ง backend ส่ง canonical key อยู่แล้ว ไม่ต้องแตะ

## Verification

- `npm run lint` ✓ 0 errors, `npm run build` ✓ (tsc + vite, built in 1.33s)
- Browser smoke (dev server + API local จริง): assignments list badge "กำลังเรียน" ✓, Gantt chips "ทั้งหมด/กำลังเรียน/ใกล้กำหนด/เรียนจบแล้ว/หมดอายุ" ✓, assignment report donut legend "ยังไม่เริ่ม" ได้สี `#94a3b8` ถูกต้อง (เดิม fallback เทา) ✓, master-data list "ใช้งานอยู่" (AppTable) ✓, division detail "ใช้งานอยู่" (StatusText default) ✓, console 0 errors ✓

## Implementer Notes

- พบและแก้ bug แฝง 1 ตัวระหว่างรวมไฟล์: donut chart หน้า assignment report สีเทาหมดทุกชิ้น เพราะ `STATUS_COLORS` key ไม่ตรงกับ label ที่แปลไทยแล้ว (เกิดตั้งแต่ตอน localize `learnerStatusLabel` เป็นไทย)
- `labels.ts` import เฉพาะ **type** `BadgeTone` จาก `components/ui/Badge` (type-only, ถูก erase ตอน build — ไม่เกิด runtime cycle กับ components ที่ import labels กลับ)
