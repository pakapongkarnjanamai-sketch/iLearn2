# PLAN-059: Audit + mapping ชื่อ Division/Department/Section ระหว่าง iLearnDB กับ EmployeeHub

- **Status:** READY
- **Assigned:** GPT (GitHub Copilot)
- **Reviewer:** Claude Code
- **Priority:** High
- **Estimated scope:** runbook (อ่านอย่างเดียวเป็นหลัก) — ผลลัพธ์คือรายงาน mapping + ข้อเสนอแก้ data ให้ผู้ใช้ตัดสิน **ห้ามแก้ข้อมูลจริงในแผนนี้**
- **สร้างเมื่อ:** 2026-07-09
- **อ้างอิง:** skill `C:\Users\n4734\.agents\skills\api-employeehub-api-reference\SKILL.md`, [PLAN-058](PLAN-058-employeehub-provider-foundation.md), [PLAN-060](PLAN-060-employeehub-cutover-qa-prod.md)

> ⚠️ **[PLAN-061](PLAN-061-employeehub-division-semantics.md) ปิด audit ส่วนใหญ่ไปแล้วด้วยข้อมูลจริง** (Claude lookup 2026-07-09) — เหลือให้ GPT ทำแค่ 4 ข้อในหัวข้อ "แก้ scope PLAN-059" ของ PLAN-061 อ่านไฟล์นั้นก่อน แล้วข้าม A1/A2/A4 ด้านล่างได้

> ที่มา (2026-07-09): gap #2 ของการย้ายไป EmployeeHub — iLearn ผูกสิทธิ์/การมอบหมายกับ**ชื่อ division แบบ free-text จาก upstream เดิม** แต่ EmployeeHub คืนค่า `NameAbbr` ที่ reconcile จากตาราง Organizations ถ้าชื่อไม่ตรงกัน หลัง cutover การกรองพนักงานตาม division, data isolation และ assignment เดิมจะจับคู่ไม่เจอแบบเงียบ ๆ (ผลลัพธ์ว่างเปล่า ไม่ error)

---

## จุดที่ชื่อ division ฝังอยู่ใน iLearn (ตรวจแล้วจากโค้ด — audit ต้องครอบทุกข้อ)

1. `Divisions.Name` (master data, FK ของ Role/Category/LearnerGroup/Assignment + claim `Division` ใน `ApiClaimsEnrichMiddleware` ที่ใช้กรอง learner)
2. `Assignments.Division` — **คอลัมน์ string เก็บชื่อ division ตรง ๆ** (legacy field คู่กับ `DivisionId`)
3. Snapshot ใน `EnrollmentAssignments` (คอลัมน์ snapshot JSON — ตรวจว่ามี division/section ฝังไหม)
4. ค่า division ที่หน้า Bulk Assign / LearnerDirectorySelector ส่งไป `Learners/divisions` (runtime — เทียบจากข้อ 1)

## Scope (ทำตามลำดับ)

- [ ] **A1 — ดึงชุดค่าจาก EmployeeHub:** `GET /api/lookups/divisions`, `/departments`, `/sections` และ `GET /api/lookups/org-tree` (บันทึก JSON ลง `artifacts/employeehub-audit/` — โฟลเดอร์นี้ untracked อยู่แล้ว) — ถ้ายังไม่รู้ URL ของ EmployeeHub ให้ถามผู้ใช้ก่อนเริ่ม
- [ ] **A2 — ดึงชุดค่าจาก upstream เดิม** (`.../api/StudentLookup/GetDistinctDivisions` ฯลฯ — URL ใน `iLearn.API/appsettings.json`) เพื่อเป็น baseline ว่าปัจจุบันระบบเห็นค่าอะไร
- [ ] **A3 — ดึงค่าที่ฝังใน iLearnDB (QA):**
  ```sql
  SELECT Id, Name FROM Divisions;
  SELECT DISTINCT Division FROM Assignments WHERE Division IS NOT NULL;
  -- + ตรวจ schema EnrollmentAssignments: คอลัมน์ snapshot มี division/section หรือไม่ (ดู entity/migration ก่อน query)
  ```
- [ ] **A4 — เทียบสามชุด (A1 vs A2 vs A3)** case-insensitive + trim: จัดตาราง `ค่าใน iLearn | ค่า upstream เดิม | ค่า EmployeeHub | สถานะ (ตรง / ต่างแค่ format / ไม่พบ)` — แยกหมวด Divisions / Departments (ระวัง: EmployeeHub canonicalize เช่น `SA Dept.` → `SA`) / Sections
- [ ] **A5 — รายงาน + ข้อเสนอ:** เขียนสรุปท้ายไฟล์แผนนี้ (หัวข้อ Findings):
  - รายการที่ต้อง remap พร้อมข้อเสนอวิธี (เช่น rename `Divisions.Name` ใน master data / UPDATE `Assignments.Division` / เพิ่มตาราง alias) + ผลกระทบ (claims, isolation, รายงานย้อนหลัง)
  - ระบุชัดว่าข้อไหน "แก้ข้อมูลได้ปลอดภัย" vs "กระทบ audit/ประวัติ ต้องให้ผู้ใช้เลือก"
  - **หยุดที่รายงาน** — การแก้ข้อมูลจริงจะเป็นแผนถัดไปหลังผู้ใช้ตัดสิน

## Out of scope (ห้ามทำ)

- ❌ UPDATE/INSERT/DELETE ใด ๆ บน DB ทุก environment
- ❌ แก้โค้ด/config ใน repo (นอกจากเติมผลลงไฟล์แผนนี้ + AGENT_LOG)
- ❌ แตะ PROD DB — audit ใช้ QA พอ (master data เพิ่งถูก clone ตอน cutover PLAN-048; ถ้าพบว่า QA/PROD drift ให้จดไว้)

## Acceptance criteria

1. ตารางเทียบครบ 3 แหล่ง × 3 หมวด พร้อมไฟล์ดิบใน `artifacts/employeehub-audit/`
2. ทุกค่าใน `Divisions.Name` และ `DISTINCT Assignments.Division` ถูกจำแนกสถานะ ไม่มีค่า "ยังไม่ได้ตรวจ"
3. ข้อเสนอ remap ระบุ SQL/ขั้นตอนคร่าว ๆ ต่อรายการ + ระดับความเสี่ยง

## Implementer Notes / Findings

(เติมผล audit ที่นี่)
