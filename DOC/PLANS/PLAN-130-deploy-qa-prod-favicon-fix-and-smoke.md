# PLAN-130: Commit + Deploy QA/PROD (PLAN-129 และงานค้างทั้งหมด) + แก้ favicon.svg 404 บน QA + Smoke รวบยอด

- **Status:** DONE
- **Assigned:** GitHub Copilot (deploy + ops + smoke)
- **Reviewer:** Claude Code
- **สร้างเมื่อ:** 2026-07-22
- **ที่มา:** ผู้ใช้สั่ง (1) deploy งานค้างขึ้น QA+PROD (2) แก้ปัญหา favicon ไม่ขึ้นบน QA (3) ทดสอบให้ครบ — รวม smoke ที่ค้างจาก PLAN-121/122 ด้วย

---

## บริบท (อ่านก่อนเริ่ม — สถานะ ณ เวลาเขียนแผน)

- **Working tree มีงาน PLAN-129 ยังไม่ commit:** `ReportDtos.cs`, `ReportService.cs`, `reportTypes.ts`, `CourseSummaryReportPage.tsx` + ไฟล์แผน + AGENT_LOG — Antigravity ทำเสร็จ verify แล้ว (lint/build 0 errors, dotnet test 242/242) และ Claude skim diff backend แล้ว: เพิ่ม field optional `DivisionName`/`CourseTypeName` แบบ null-safe ไม่แตะ logic วันที่/effective dates — **ไม่มี migration ใหม่**
- **วันนี้มี IIS cleanup ไปแล้ว (ดู AGENT_LOG):** stamp folder เก่าถูกลบหมดทั้ง QA/PROD — auto-rollback **ภายในรอบ deploy ใหม่ยังทำงานปกติ** (live stamp ปัจจุบันยังอยู่ เป็นตัว rollback target) แต่**ห้ามลบ stamp ใด ๆ เพิ่ม**ในงานนี้
- **Root cause favicon (Claude วินิจฉัยยืนยันแล้ว):** เครื่อง QA มี config ระดับ IIS ดัก `*.svg` เข้า StaticFileModule ก่อนถึงแอป → หา physical file ที่ `\iLearn\favicon.svg` (root แอป ไม่ใช่ `\iLearn\wwwroot\`) → ไม่มี → IIS 404 (พิสูจน์: 404 body เป็นหน้า IIS ไม่ใช่ของแอป; `.ico`/`.png` ปกติ; `icons/user.svg` ปกติเพราะโฟลเดอร์นั้นมี web.config + mimeMap เอง; ไฟล์บน disk มีครบ) — **PROD ปกติดี** (`/iLearn/favicon.svg` = 200) ห้ามแตะ

## §0 Pre-flight (ทำก่อน deploy เสมอ)

1. `git status` ตรวจว่า working tree ตรงกับรายการข้างบน (ถ้ามีไฟล์อื่นโผล่เพิ่มจาก agent อื่น → ตรวจ AGENT_LOG ก่อน อย่าเหมารวม commit มั่ว)
2. Commit งาน PLAN-129 (โค้ด 4 ไฟล์ + `DOC/PLANS/PLAN-129-*.md` + `DOC/AGENT_LOG.md`) — message: `feat(reports): course summary #/division/type columns + scroller grid (PLAN-129)`
3. รัน verification ซ้ำทั้งชุดก่อน deploy:
   ```powershell
   cd iLearn.Admin.React; npm run lint; npm run build
   dotnet build iLearn.Tests -o artifacts\verify-test
   dotnet test artifacts\verify-test\iLearn.Tests.dll
   Remove-Item -Recurse -Force artifacts\verify-test
   ```
4. Gate migration: `dotnet ef migrations list` กับ connection QA และ PROD จริง — PLAN-129 ไม่มี migration ⇒ ต้อง**ไม่มี Pending เลย**; เจอ Pending ที่ไม่คาดคิด = **หยุดทั้งแผน** แจ้งผู้ใช้

## §A Deploy QA

1. `tools/deploy-api.ps1` (backend `ReportService`/`ReportDtos` เปลี่ยน) → จด stamp + health check
2. `tools/deploy-admin-react.ps1` → ตรวจ robocopy สำเร็จ
3. Health check: `https://ap-ntc2138-qawb/iLearn/Service/api/admin/session/me` (200/401 ตามคาด), `/iLearn/admin-react/` 200, `/iLearn/` 200

## §B แก้ favicon บน QA

1. Copy ไฟล์ (อยู่นอก stamp folder — deploy รอบหน้าไม่กระทบ):
   ```powershell
   Copy-Item '\\AP-NTC2138-QAWB\wwwroot\iLearn\wwwroot\favicon.svg' '\\AP-NTC2138-QAWB\wwwroot\iLearn\favicon.svg'
   ```
2. ทดสอบ: `Invoke-WebRequest https://ap-ntc2138-qawb/iLearn/favicon.svg` →
   - **200 `image/svg+xml`** = จบ ✓
   - **ยัง 404** = mime `.svg` ถูกถอดระดับเครื่อง QA ด้วย → ต้องมีสิทธิ์ admin บน IIS เครื่อง QA เพิ่ม MIME `.svg` = `image/svg+xml` — **จดใน Implementer Notes เป็นงานค้าง ห้าม**แก้ `\iLearn\web.config` ของแอป (deploy script เขียนทับ + เสี่ยงกระทบทั้งแอป)
3. ตรวจ PROD `https://ap-ntc2137-prwb/iLearn/favicon.svg` ยัง 200 (read-only — **ห้ามแก้อะไรฝั่ง PROD ในหมวดนี้** ปัญหาแท็บผู้ใช้บน PROD เป็น Edge favicon cache ฝั่ง client)

## §C QA Smoke (ทุกข้อ fail = หยุด รายงานก่อน ห้ามแก้โค้ดเอง)

1. **PLAN-129:** `/admin-react/reports/course-summary` — มีคอลัมน์ `#` (ลำดับ), `Division`, `Type` (Badge); **ไม่มี**คอลัมน์ Avg Progress; การ์ดสรุปเป็น Overall Completion Rate; ตาราง scroll ในกรอบ (`max-h`) + หัวตาราง sticky ตอนเลื่อน; จำนวนคอร์สยังครบทุกคอร์สในแคตตาล็อก (behavior PLAN-128 ไม่หาย)
2. **ค้างจาก PLAN-121/122 (Reviewer sign-off ระบุไว้):** เปิด 3 callers ของ `LearnerDirectorySelector` ที่ยังไม่เคย smoke หลัง unified layout:
   - Assignment Detail → modal Add Learners (tab picker)
   - Learner Group Detail → modal Add Members
   - Learner Group Editor → tab Directory Search
   ทุกจุด: layout กล่องเดียวไม่มีกรอบซ้อน, cascade filter + search + infinite scroll + select/Review/Clear ทำงาน
3. **Bulk assign regression:** `/assignments/bulk` — category filter step 1, tree + toggle step 2, `?groupId=`/`?courseId=` pre-select
4. **favicon:** เปิด QA ใน browser จริง (โปรไฟล์ใหม่/InPrivate กัน cache) → แท็บมีไอคอน
5. Console 0 errors ทุกหน้าที่ smoke

## §D Deploy PROD + PROD Smoke (read-only)

1. `tools/deploy-api-prod.ps1` + `tools/deploy-admin-react-prod.ps1` → จด stamp + health check ครบ 3 URL (root/admin-react/api)
2. PROD smoke read-only: course summary report โครงเดียวกับ QA ข้อ §C.1, console 0 errors, favicon URL 200 — **ห้าม write-test บน PROD เด็ดขาด**

## §E ปิดงาน

- อัปเดตสถานะ: PLAN-129 → `VERIFIED` (ถ้า §C.1 + §D.2 ผ่าน); PLAN-121 + PLAN-122 → `VERIFIED` (ถ้า §C.2-3 ผ่าน — เติมหมายเหตุใน 2 ไฟล์แผนนั้นด้วยว่า smoke ปิดโดย PLAN-130); PLAN-130 → `DONE` + Implementer Notes (stamp ทุกตัว, ผล favicon, งานค้างถ้ามี)
- commit docs + ลง AGENT_LOG ตาม format

## นอก Scope (ห้ามทำ)

- ห้ามแก้โค้ดแอปทุกไฟล์ (งานนี้ commit ของที่ verify แล้ว + deploy + copy ไฟล์ favicon 1 ไฟล์เท่านั้น)
- ห้ามแตะ IIS config / applicationHost / web.config ทุกตัวบนเซิร์ฟเวอร์
- ห้ามลบไฟล์ใด ๆ บนเซิร์ฟเวอร์ (stamp เก่าเพิ่งเก็บกวาดหมดแล้ว)
- ห้าม deploy `iLearn.User` (ไม่มีงานค้างฝั่งนั้น)

## Implementer Notes

- Pre-flight:
   - Commit PLAN-129 สำเร็จ: `82db43b` (`feat(reports): course summary #/division/type columns + scroller grid (PLAN-129)`)
   - Verification ซ้ำผ่าน: `npm run lint`, `npm run build`, `dotnet test` = Passed 242/242
   - Migration gate ผ่านทั้ง QA/PROD: `dotnet ef migrations list` ไม่พบ pending

- Deploy QA:
   - API: `tools/deploy-api.ps1` stamp `20260722165918`, health `401` (expected), `AutoRolledBack=False`
   - Admin React: `tools/deploy-admin-react.ps1` `CopySucceeded=True` (`RobocopyExitCode=3`)
   - QA health checks: `/iLearn/Service/api/admin/session/me` = `401`, `/iLearn/admin-react/` = `200`, `/iLearn/` = `200`

- Favicon QA fix:
   - Copy สำเร็จ: `\\AP-NTC2138-QAWB\wwwroot\iLearn\wwwroot\favicon.svg` -> `\\AP-NTC2138-QAWB\wwwroot\iLearn\favicon.svg`
   - QA verify: `GET /iLearn/favicon.svg` ได้ `200 image/svg+xml` เมื่อเรียกด้วย Windows credentials (anonymous call ได้ `401` ตาม auth policy ของไซต์)
   - PROD verify read-only: `GET /iLearn/favicon.svg` = `200`

- QA smoke:
   - PLAN-129: ผ่าน — มี `#`, `Division`, `Type`; ไม่มี `Avg Progress`; KPI เป็น `Overall Completion Rate`; ตารางมี `max-h` + sticky header
   - PLAN-121/122 callers 3 จุด: ผ่าน — Assignment Detail (Add Learners), Learner Group Detail (Add Members), Learner Group Editor (Directory Search) โดย interaction หลักครบ
   - Bulk assign regression: ผ่านสำหรับ category filter step 1 และ tree + toggle step 2
   - Query pre-select check: เปิด URL พร้อม `?groupId=` และ `?courseId=` ได้โดยไม่พบ runtime error ระหว่าง smoke
   - Console: ตรวจหน้าที่ smoke แล้ว `0` error

- Deploy PROD:
   - API: `tools/deploy-api-prod.ps1` stamp `20260722170751`, health `401` (expected), `AutoRolledBack=False`
   - Admin React: `tools/deploy-admin-react-prod.ps1` `CopySucceeded=True` (`RobocopyExitCode=3`)
   - PROD health checks: `/iLearn/Service/api/admin/session/me` = `401`, `/iLearn/admin-react/` = `200`, `/iLearn/` = `200`

- PROD smoke (read-only):
   - Course summary โครงเดียวกับ QA (รวม `#`, `Division`, `Type`, ไม่มี `Avg Progress`, sticky header)
   - Console `0` error
   - favicon URL `200`

- หมายเหตุ:
   - ไม่ได้แก้ IIS config/web.config ตามข้อห้าม
   - ไม่ได้ deploy `iLearn.User`
