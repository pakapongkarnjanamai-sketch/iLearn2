# PLAN-135: สำรวจป้าย label ซ้ำรอบ 3 + commit + deploy QA/PROD (labels รวมศูนย์ + enrollment-visibility)

- **Status**: DONE
- **Assigned**: GitHub Copilot (GPT)
- **Created**: 2026-07-23

## Overview

ปิดรอบงาน label centralization (PLAN-133/134 — ยังไม่ commit) และ deploy ให้ครบ โดยรอบ deploy นี้**รวม backend ด้วย** เพราะ `843af50` (PLAN-132 enrollment-visibility fix) commit แล้วแต่**ยังไม่เคยขึ้น QA/PROD**

สภาพ ณ เวลาเขียนแผน:

- Working tree = งาน PLAN-133/134 เท่านั้น (React 23 ไฟล์ + `labels.ts` ใหม่ + `learnerStatus.ts` ถูกลบ + `DOC/*` 3 ไฟล์) — ดูรายการเต็มจาก `git status`
- Commit ล่าสุด `843af50` = PLAN-132 (backend: ReportService/CourseService ซ่อน enrollment กำพร้า) — **ยังไม่ deploy** ⇒ ต้อง full API publish
- ไม่มี migration ใหม่ทั้งสองงาน (ยืนยันด้วย gate ใน §2 อีกครั้ง)
- Claude ปิด dev API (`localhost:7128`) และ vite dev server ที่รันทดสอบไว้แล้ว — bin ไม่ล็อก

## §1 สำรวจป้าย label ซ้ำ (fresh eyes รอบ 3)

กติกาตัดสิน (ตาม PLAN-133/134 — **ห้ามขยายขอบเขตเอง**):

- ป้ายใน **badge/pill** (StatusBadge/StatusText/Badge/ReadinessBadge/tag) ที่เป็น status/type vocabulary → ต้องมาจาก `src/lib/labels.ts` ผ่าน `t()`/helper — เจอ literal หลุด = แก้เข้าไฟล์กลาง (เพิ่มคีย์ th/en ตาม pattern เดิม)
- ข้อความหน้าเพจ / หัวคอลัมน์ / stat caption / tooltip → **ไม่แก้** จดลง Implementer Notes เท่านั้น

คำสั่งตรวจ (รันจาก `iLearn.Admin.React`):

```powershell
# 1) import ไฟล์เก่าต้องเหลือ 0
Select-String -Path src -Pattern "lib/learnerStatus" -Recurse

# 2) literal ใน badge children (เดี่ยว + conditional) — รีวิวผลด้วยตาทีละรายการ
Select-String -Path src\pages,src\components -Pattern '<(Status)?Badge[^>]*>\s*[A-Z]' -Recurse
Select-String -Path src\pages,src\components -Pattern "\?\s*'(Active|Inactive|Passed|Draft|Published|Open|Closed|Ready|Pending|Folder|Group|Enabled|Disabled|Pass|Fail|Learn|Exam)" -Recurse

# 3) คำไทยที่เป็น status vocabulary นอก labels.ts (ผลที่เป็น copy หน้าเพจ = ปล่อยผ่าน)
Select-String -Path src -Pattern "เรียนจบแล้ว|กำลังเรียน|ยังไม่เริ่ม|ใกล้กำหนด|หมดอายุ|ใช้งานอยู่|ปิดใช้งาน|เปิดใช้งาน|ฉบับร่าง|เผยแพร่แล้ว|พร้อมใช้งาน|บทเรียน|แบบทดสอบ|โฟลเดอร์" -Recurse | Where-Object Path -NotMatch "labels.ts"

# 4) label map แปลกปลอมนอกไฟล์กลาง
Select-String -Path src -Pattern "_LABELS?\s*[:=]" -Recurse | Where-Object Path -NotMatch "labels.ts"
```

จุดที่**รู้อยู่แล้วว่าปล่อยไว้** (อย่ารายงานซ้ำ/อย่าแก้ — รายละเอียดใน PLAN-134 §จงใจไม่แตะ): stat caption ใน VersionFormPage/AssignmentReportPage/LearnerProfilePage Summary, หัวคอลัมน์ตาราง + caption ทั้งหมดใน moduleConfigs, `CHECK_LABELS` ใน HealthCheckPage, tooltip `title="Rule Deleted"`

## §2 Verification gate

```powershell
# Frontend (จาก iLearn.Admin.React)
npm run lint
npm run build

# Backend (bin ไม่ล็อกแล้ว แต่ถ้าล็อกให้ใช้ artifacts pattern ตาม CLAUDE.md)
dotnet build iLearn.Tests -o artifacts\verify-test
dotnet test artifacts\verify-test\iLearn.Tests.dll
Remove-Item -Recurse -Force artifacts\verify-test

# Migration gate — ต้องไม่มี pending ทั้ง QA และ PROD ก่อน deploy
dotnet ef migrations list --project iLearn.Infrastructure --startup-project iLearn.API
```

## §3 Commit

- Commit เดียว รวม React + `DOC/PLANS/PLAN-133*.md`, `PLAN-134*.md`, `PLAN-135*.md`, `DOC/AGENT_LOG.md`
- ข้อความแนว: `feat(admin-react): centralize status/badge labels into lib/labels.ts with th/en pairs (PLAN-133/134)`
- ก่อน commit: `git status` ต้องมีแต่ไฟล์ในรายการข้างต้น — ถ้ามีไฟล์แปลกปลอมจาก agent อื่น ให้หยุดแล้วตรวจ AGENT_LOG ก่อน

## §4 Deploy QA

1. `tools/deploy-api.ps1` — **full publish** (backend เปลี่ยนจริงจาก `843af50` — อย่าใช้ SkipPublish)
2. `tools/deploy-admin-react.ps1`
3. Health checks ตาม convention: `/Service/api/admin/session/me` = 401, `/admin-react/` = 200, `/` = 200

## §5 QA smoke

**PLAN-132 (enrollment visibility — ยังไม่เคย smoke บนเซิร์ฟเวอร์):**

1. สร้าง assignment ทดสอบใหม่ (คอร์ส+learner ทดสอบ) → learner โผล่ใน Course → tab Learners
2. ลบ assignment นั้น → learner **หาย**จาก (a) tab Learners (b) KPI Active Learners (c) Compliance report
3. ข้อมูลเดิมที่ไม่เกี่ยว (enrollment ปกติ) ยังแสดงครบ — เทียบจำนวนคร่าว ๆ ก่อน/หลัง deploy ว่าไม่หายผิดปกติ

**PLAN-133/134 (labels):**

4. `/assignments` — คอลัมน์ Status เป็นภาษาไทย ("กำลังเรียน" ฯลฯ)
5. `/assignments/gantt` — ชิป "ทั้งหมด / กำลังเรียน / ใกล้กำหนด / เรียนจบแล้ว / หมดอายุ"
6. `/assignments/{id}/report` — donut มีสีตามสถานะ (**ไม่ใช่เทาทุกชิ้น**) + legend ไทย
7. `/master-data/divisions` — คอลัมน์ Active = "ใช้งานอยู่/ปิดใช้งาน"; เข้า detail 1 รายการ = "ใช้งานอยู่"
8. `/content-library` — list "บทเรียน/แบบทดสอบ" และ detail แสดง**คำเดียวกัน** (เดิม detail เป็น Learn/Exam)
9. `/learner-groups` — tag "โฟลเดอร์/กลุ่ม"
10. `/health-check` — ป้ายไทย (ระบบปกติ/ผ่าน/ไม่ผ่าน/เชื่อมต่อไม่ได้ ตามสถานะจริง)
11. Console = 0 errors บนหน้าที่ smoke

## §6 Deploy PROD + read-only smoke

1. `tools/deploy-api-prod.ps1` (full publish) + `tools/deploy-admin-react-prod.ps1` + health checks 3 URL
2. Smoke **read-only เท่านั้น** — ตรวจข้อ 4–11 ข้างต้น **ห้ามสร้าง/ลบ assignment บน PROD**
3. เทียบตัวเลข Compliance report ก่อน/หลังคร่าว ๆ — ตัวเลขอาจ**ลดลง**ได้ = intended (PLAN-132 เลิกนับ enrollment กำพร้า) แต่ถ้าลดฮวบผิดสังเกต (เกิน ~ครึ่ง) ให้จดใน Implementer Notes แล้วแจ้งผู้ใช้ก่อนปิดงาน

## §7 ปิดสถานะ

- PLAN-132, PLAN-133, PLAN-134: `DONE` → `VERIFIED`
- PLAN-135: `READY` → `DONE` + Implementer Notes (ผล survey รอบ 3 / สิ่งที่เจอ)
- ลง `DOC/AGENT_LOG.md` ตาม format (entry ใหม่บนสุด)

## Out of Scope

- ห้ามแตะ logic ใน `labels.ts` เกินการเพิ่มคีย์ที่ survey เจอ
- ห้ามแปลข้อความหน้าเพจ/หัวคอลัมน์เพิ่ม (เฟสสองภาษาแยกต่างหาก)
- ห้ามแก้ backend ใด ๆ (ถ้า smoke PLAN-132 พบปัญหา → จด Implementer Notes + แจ้งผู้ใช้ ไม่ hotfix เอง)

## Implementer Notes

- Survey รอบ 3: ไม่มี import `lib/learnerStatus`, ไม่มี literal ใน `Badge`/`StatusBadge` children และไม่มี label map เพิ่มนอก `labels.ts`. ผล conditional ที่ยังพบเป็น page copy, table/review fallback, tooltip หรือ `CHECK_LABELS` ซึ่งอยู่นอก scope ตามแผน จึงไม่แก้เพิ่ม.
- Verification: `npm run lint` และ `npm run build` ผ่าน; backend build/test ผ่าน `247/247`; migration gate ผ่านทั้ง QA และ PROD เมื่อส่ง `ConnectionStrings__DefaultConnection` ให้ `AppDbContextFactory` (factory ไม่อ่าน `appsettings.*` เอง และหากไม่ตั้งค่าจะ fallback LocalDB จึงแสดง pending เทียม).
- Commit: `c0dd5ef feat(admin-react): centralize status and badge labels (PLAN-133/134)`.
- QA deploy: API full publish stamp `20260723103252`; Admin React copy สำเร็จ (robocopy 3); health `401/200/200`. PLAN-132 smoke: สร้าง batch `AS-20260723-001` สำหรับ course `WI-CAS-111111` และ learner `610034`, ยืนยัน Learners tab/KPI = 1 ก่อนลบ, ลบ batch แล้ว Learners tab = `No learners`, Active Learners/KPI และ Assignment Batches = 0; Compliance report ไม่พบ course test หลังลบ.
- PROD deploy: API full publish stamp `20260723104219`; Admin React copy สำเร็จ (robocopy 3); health `401/200/200`. Smoke เป็น read-only: assignment/Gantt labels ไทย, assignment report donut มี canonical colors `#4f46e5`/`#94a3b8` และ legend ไทย, division/content list+detail และ learner-group tags ถูกต้อง.
- Known environment finding: QA/PROD Health Check มี console errors 2 รายการเฉพาะ Learner Site probe เพราะ browser ไปถึง `https://<host>.nikonoa.net/iLearn/health/smoke` ไม่ได้ (รายงานเป็น CORS/service down); page แสดง `เชื่อมต่อไม่ได้` ถูกต้อง. ไม่ใช่ regression จากงาน labels และอยู่นอก scope.
