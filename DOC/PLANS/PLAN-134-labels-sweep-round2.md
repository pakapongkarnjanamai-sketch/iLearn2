# PLAN-134: กวาดป้ายสถานะ/badge ตกค้างรอบ 2 เข้า `lib/labels.ts` (ต่อจาก PLAN-133)

- **Status**: VERIFIED
- **Assigned**: Claude Code (ผู้ใช้สั่งสำรวจละเอียดซ้ำ + ทำต่อใน session)
- **Created**: 2026-07-23
- **Completed**: 2026-07-23

## Overview

ผู้ใช้สั่ง "สำรวจอย่างละเอียดทั้งหมดอีกครั้ง" หลังปิด PLAN-133 — กวาดซ้ำทั้ง `iLearn.Admin.React/src` พบป้าย badge ตกค้างอีก 10 จุดที่รอบแรกไม่ครอบคลุม (ส่วนใหญ่เป็น literal อังกฤษใน `StatusBadge`/`Badge` children ที่ regex รอบแรกไม่จับ เพราะอยู่คนละบรรทัด/เป็น conditional expression)

**กติกาตัดสิน (คงเดิมจาก PLAN-133):** ป้ายใน badge = รวมเข้าไฟล์กลาง · ข้อความหน้าเพจ, หัวคอลัมน์ตาราง, stat caption = เฟสสองภาษาถัดไป

## สิ่งที่พบและแก้

เพิ่มใน `lib/labels.ts`: `CONTENT_TYPE_LABELS` + `contentTypeLabel()`, `HEALTH_LABELS` (checking/unreachable/operational/degraded/pass/fail/enabled/disabledSecure), และขยาย `COMMON_LABELS` (activeVersion/inactiveVersion/passed/cancelled/assigned/selfEnroll/notFoundInDirectory/folder/group)

| จุด | เดิม | ใหม่ |
|---|---|---|
| `CourseDetailPage` versions tab + `VersionDetailPage` | `Active Version`/`Inactive` อังกฤษดิบ | เวอร์ชันที่ใช้งาน/ไม่ได้ใช้งาน |
| `LearnerProfilePage` enrollment badges | `Passed`/`Cancelled`/`Assigned`/`Self-Enroll` | ผ่านแล้ว/ยกเลิกแล้ว/ได้รับมอบหมาย/ลงทะเบียนเอง |
| `AssignmentDetailPage:1281` | `Not found in directory` | ไม่พบข้อมูลพนักงาน |
| `LearnerGroupListPage` tags | `Folder`/`Group` | โฟลเดอร์/กลุ่ม |
| **ชนิดคอนเทนต์ 3 จุดขัดกันเอง**: `moduleConfigs` (บทเรียน/แบบทดสอบ) vs `ContentItemDetailPage` `TYPE_LABEL` (Learn/Exam) vs `ContentItemEditorPage` select (Learn/Exam) | ปนสองภาษา | `contentTypeLabel()` ที่เดียว = บทเรียน/แบบทดสอบ ทุกจุด |
| `ContentItemEditorPage:234` | `<ReadinessBadge label="Ready">` override อังกฤษ | ตัด override ใช้ default กลาง (พร้อมใช้งาน) |
| `HealthCheckPage` | Checking…/Unreachable/Operational/Degraded/Pass/Fail | กำลังตรวจสอบ…/เชื่อมต่อไม่ได้/ระบบปกติ/มีปัญหา/ผ่าน/ไม่ผ่าน |
| `SystemConfigPage` trustCert badge | Enabled/Disabled (Secure) | เปิดอยู่/ปิดอยู่ (ปลอดภัย) |

## จงใจไม่แตะ (ตกไปเฟสสองภาษาเต็มรูปแบบ)

- Stat tile caption: `VersionFormPage:528-540` (Not Started/In Progress/Completed/Other Open), `AssignmentReportPage:318-331` (Learners/Completed/Overdue/Courses), `LearnerProfilePage` Summary Facts (Completed/In Progress) — อยู่ในบริบทหน้า/แถวที่ยังเป็นอังกฤษล้วน แปลบางช่องจะปนภาษาในแถวเดียว
- หัวคอลัมน์ตาราง: `AssignmentReportPage:375-380`, caption ทั้งหมดใน `moduleConfigs.ts`
- `HealthCheckPage` `CHECK_LABELS` (ชื่อรายการตรวจ — ไม่ใช่ป้ายสถานะ), tooltip `title="Rule Deleted"`, ข้อความ copy อื่น ๆ

## Verification

- `npm run lint` ✓ 0 errors, `npm run build` ✓ (1.36s)
- Browser smoke (dev + API local): health-check "มีปัญหา/ผ่าน/ไม่ผ่าน/เชื่อมต่อไม่ได้" ✓, content library list "บทเรียน/แบบทดสอบ" + detail "บทเรียน/เผยแพร่แล้ว" **สอดคล้องกันแล้ว** ✓, learner-groups "โฟลเดอร์/กลุ่ม" ✓, console 0 errors ✓ (version badge + learner profile ใช้กลไกเดียวกัน ผ่าน type-check — ไม่ได้คลิกถึงใน explorer)
