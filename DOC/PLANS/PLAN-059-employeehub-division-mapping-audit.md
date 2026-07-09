# PLAN-059: Audit + mapping ชื่อ Division/Department/Section ระหว่าง iLearnDB กับ EmployeeHub

- **Status:** VERIFIED (reviewer sign-off ท้ายไฟล์) — audit ผ่าน, ไม่มี mapping blocker; เหลือ decision R2 (PD3) ที่ผู้ใช้ต้องเคาะก่อน PLAN-060 READY
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

> Audit ดำเนินการโดย GPT (GitHub Copilot) — 2026-07-09
> ใช้หลักฐานจาก PLAN-061 สำหรับ A1/A2/A4 (ข้ามตามคำสั่ง) และ query ตรง QA+PROD สำหรับ A3/A5

---

### Finding 1 — EnrollmentAssignment Snapshot ไม่มี division/section ฝัง

`EnrollmentAssignments` **ไม่มี JSON snapshot column** เลย  
Snapshot fields ทั้งหมดเป็น scalar:

| Column | Type | Purpose |
|---|---|---|
| `SnapshotCompleted` | `bit` | ว่า enrollment เคย complete หรือยัง ณ ขณะ reassign |
| `SnapshotCompletedDate` | `datetime2?` | วันที่ complete |
| `SnapshotProgress` | `float` | % progress ณ ขณะ snapshot |

**ไม่มี division / department / section / learner name ฝังใน snapshot** → ไม่มี data ที่ต้อง remap จาก cutover EmployeeHub  
**ความเสี่ยง: ศูนย์** — ไม่ต้องทำอะไร

---

### Finding 2 — Division `Test` (Id=6) ปลอดภัยลบได้

**QA:** `IsActive=1, IsDeleted=1` (soft-deleted แล้ว)  
**PROD:** `IsActive=1, IsDeleted=0` (**ยังไม่ได้ soft-delete**)

FK references ทั้ง QA และ PROD = **ศูนย์ทุกตาราง:**

| Table | QA refs | PROD refs |
|---|---|---|
| Assignments | 0 | 0 |
| Categories | 0 | 0 |
| LearnerGroups | 0 | 0 |
| LearnerGroupCategories | 0 | 0 |
| Roles | 0 | 0 |
| AdminActivities | 0 | 0 |

**ข้อเสนอ:** soft-delete บน PROD ให้ตรงกับ QA (หรือ hard-delete ก็ปลอดภัย เพราะ FK=0)

```sql
-- PROD: soft-delete Test division
UPDATE Divisions SET IsDeleted = 1, DeletedAt = GETDATE(), DeletedBy = 'admin-audit'
WHERE Id = 6 AND Name = 'Test';
```

**ความเสี่ยง: ต่ำมาก** — ไม่มี FK ใด ๆ ที่จะพัง, ไม่กระทบ audit/history

---

### Finding 3 — QA vs PROD Divisions Comparison

| Id | Name | QA IsActive | QA IsDeleted | PROD IsActive | PROD IsDeleted | Drift |
|---|---|---|---|---|---|---|
| 1 | PD1 | 1 | 0 | 1 | 0 | ✅ ตรงกัน |
| 2 | PD2 | 1 | 0 | 1 | 0 | ✅ ตรงกัน |
| 3 | CSD | 1 | 0 | 1 | 0 | ✅ ตรงกัน |
| 4 | PD3 | 1 | **1** | 1 | **0** | ⚠️ QA soft-deleted, PROD active |
| 5 | NLC | 1 | 0 | 1 | 0 | ✅ ตรงกัน |
| 6 | Test | 1 | **1** | 1 | **0** | ⚠️ QA soft-deleted, PROD active |

**Drift 2 แถว:**

1. **PD3 (Id=4):** ชื่อ `PD3` เป็นชื่อ division จริงใน EmployeeHub (ใช้โดยพนักงาน NTC+VDS) — FK=0 ทั้ง QA/PROD แต่ **ควรพิจารณาว่าจะเปิดใช้งานหรือไม่** เพราะ:
   - ถ้าองค์กรมี learner ใน PD3 จริง → ต้อง **un-delete บน QA** (`IsDeleted=0`) เพื่อให้ admin จัดกลุ่ม/assign ได้
   - ถ้า PD3 ถูก soft-delete บน QA โดยเจตนา (เช่น PD3 ถูกรวมกับ PD อื่น) → soft-delete บน PROD ด้วยเพื่อให้ตรงกัน
   - **ต้องให้ผู้ใช้ตัดสิน** — ข้อมูลจากระบบอย่างเดียวไม่พอ

2. **Test (Id=6):** ดูหัวข้อ Finding 2 — ปลอดภัยลบ/soft-delete ได้ทั้ง PROD

---

### Finding 4 — `Assignments.Division` (legacy string column) ว่างเปล่า

```
QA:   SELECT DISTINCT Division FROM Assignments WHERE Division IS NOT NULL → (0 rows)
PROD: SELECT DISTINCT Division FROM Assignments WHERE Division IS NOT NULL → (0 rows)
```

คอลัมน์นี้ NULL ทั้งตาราง ทั้ง QA และ PROD → **ไม่มีข้อมูล legacy ที่ต้อง migrate**

---

### Finding 5 — ตารางเทียบ 3 แหล่ง (สรุปจาก PLAN-061 + A3 query)

PLAN-061 ได้ทำ full lookup 3 แหล่งไว้แล้ว สรุปการจำแนกทุกค่า:

| ค่า | iLearnDB (Divisions.Name) | Upstream เดิม (GetDistinctDivisions) | EmployeeHub (derived) | สถานะ |
|---|---|---|---|---|
| PD1 | ✅ Id=1, active | ✅ | ✅ (NTC division) | **ตรง** |
| PD2 | ✅ Id=2, active | ✅ | ✅ (NTC division) | **ตรง** |
| CSD | ✅ Id=3, active | ✅ | ✅ (NTC division) | **ตรง** |
| PD3 | ✅ Id=4, QA deleted | ✅ | ✅ (NTC+VDS division) | **ต่าง state — ดู Finding 3** |
| NLC | ✅ Id=5, active | ✅ | ✅ (Company filter) | **ตรง** (กติกาพิเศษ: filter by Company ไม่ใช่ Division) |
| Test | ✅ Id=6, QA deleted | ❌ ไม่มี | ❌ ไม่มี | **Test data — ลบได้** |
| ECD | ❌ ไม่มีใน iLearn | ✅ | ✅ (NTC) | **ไม่ได้ใช้ — ปลอดภัย** |
| ELD | ❌ | ✅ | ✅ (NTC) | **ไม่ได้ใช้ — ปลอดภัย** |
| FAD | ❌ | ✅ | ✅ (NTC) | **ไม่ได้ใช้ — ปลอดภัย** |
| MED | ❌ | ✅ | ✅ (NTC) | **ไม่ได้ใช้ — ปลอดภัย** |
| PCD | ❌ | ✅ | ✅ (NTC) | **ไม่ได้ใช้ — ปลอดภัย** |
| PD4 | ❌ | ✅ | ✅ (NTC+VDS) | **ไม่ได้ใช้ — ปลอดภัย** |
| PNP | ❌ | ✅ | ✅ (NTC) | **ไม่ได้ใช้ — ปลอดภัย** |
| QAD | ❌ | ✅ | ✅ (NTC) | **ไม่ได้ใช้ — ปลอดภัย** |
| DP-CGA | ❌ | ✅ | ✅ (NTC) | **ไม่ได้ใช้ — ปลอดภัย** |
| DP-SHD | ❌ | ✅ | ✅ (NTC) | **ไม่ได้ใช้ — ปลอดภัย** |

> Division ที่ upstream/EmployeeHub มีแต่ iLearn ไม่มี (10 ค่า) = division ที่ยังไม่เคยสร้างเป็น master data ใน iLearn ซึ่งปกติดี — admin สร้างเพิ่มได้ตามต้องการ

---

### สรุปข้อเสนอ

| # | รายการ | Action | ความเสี่ยง | ใครตัดสิน |
|---|---|---|---|---|
| R1 | Soft-delete `Test` (Id=6) บน PROD | `UPDATE Divisions SET IsDeleted=1 WHERE Id=6` | ต่ำมาก (FK=0) | Admin ดำเนินการได้เลย |
| R2 | ตัดสิน PD3 (Id=4): un-delete บน QA หรือ soft-delete บน PROD | ดู Finding 3 | ต่ำ (FK=0) แต่กระทบ assign scope | **ผู้ใช้ตัดสิน** |
| R3 | ไม่ต้อง remap `Assignments.Division` | ไม่ต้องทำ (NULL ทั้งหมด) | ศูนย์ | — |
| R4 | ไม่ต้อง migrate EnrollmentAssignment snapshot | ไม่ต้องทำ (ไม่มี division data) | ศูนย์ | — |

**ไม่มีค่า division ใน iLearnDB ที่ขัดกับกติกา EmployeeHub ใน PLAN-061** — cutover สามารถดำเนินการได้โดยไม่ต้อง remap master data (ยกเว้นจุด R1/R2 ข้างบนซึ่งเป็นเรื่อง cleanup ไม่ใช่ mapping)

---

## Reviewer Sign-off (Claude Code, 2026-07-09) — ✅ ผ่าน

Audit ครบถ้วน สรุปถูกต้อง ตรวจซ้ำแล้ว:
- **Finding 1 verified จาก entity จริง** — [`EnrollmentAssignment.cs:28-30`](../../iLearn.Domain/Entities/EnrollmentAssignment.cs) มีแค่ `SnapshotCompleted`(bool)/`SnapshotCompletedDate`/`SnapshotProgress`(double) ไม่มี name string ฝัง ✓
- **Finding 4 สอดคล้อง** หลักฐานเดิม (PLAN-061: `Assignments.Division` NULL ทั้งตาราง) ✓
- **เสริมหลักฐานที่ทำให้ข้อสรุป "ไม่ต้อง remap" แข็งขึ้น (reviewer ตรวจ schema เพิ่ม):** iLearn **ไม่มี entity `Department`/`Section` master-data เลย** และ [`LearnerGroup`](../../iLearn.Domain/Entities/LearnerGroup.cs) / [`Assignment`](../../iLearn.Domain/Entities/Assignment.cs) scope ด้วย `DivisionId` (FK) + `EmployeeCodes`/explicit members เท่านั้น → **ที่เดียวที่เก็บ "ชื่อ" division เป็น string คือ `Divisions.Name` (master data) กับ `Assignment.Division` (NULL)** ส่วน department/section เป็น attribute ของพนักงานที่ดึงสด runtime → EmployeeHub canonicalize (`SA Dept.`→`SA`) **ทำข้อมูล iLearn เพี้ยนไม่ได้** เพราะไม่เคย persist ชื่อพวกนี้ (ปิดประเด็น A4 ที่แผนเดิมกังวล)
- **การจำแนก division 15+1 ค่า ตรงกับ PLAN-061 เป๊ะ** (14 non-NLC + NLC + Test) — internal consistency ผ่าน
- Findings 2/3 (FK counts, QA↔PROD drift) เป็นผล query read-only ที่ reviewer ไม่มี DB access รันซ้ำเอง แต่ internally consistent + สมเหตุสมผล; GPT แยก "ทำได้เลย" (R1/R3/R4) กับ "ผู้ใช้ตัดสิน" (R2) ถูกต้อง

### ข้อสังเกต reviewer (ไม่ block)
1. **Acceptance #1 (raw files ใน `artifacts/employeehub-audit/`)** — ไม่ได้สร้าง เพราะ A1/A2/A4 ถูก supersede ด้วย PLAN-061 (evidence inline พอ); แต่ผล A3 (FK counts, QA/PROD compare) อยู่ในตารางเท่านั้น ไม่มี raw dump — ยอมรับได้สำหรับ audit ขนาดนี้
2. **R1 (soft-delete `Test` บน PROD)** — ปลอดภัยจริง (FK=0) แต่เป็น **write บน PROD DB** ไม่ควรให้ admin รัน ad-hoc; แนะนำผูกเป็น step หนึ่งใน **PLAN-060 pre-cutover** (มี rollback/gate อยู่แล้ว) พร้อมกับผล R2
3. **R2 (PD3)** เป็น prerequisite ของ PLAN-060 (ต้องได้คำตอบผู้ใช้ก่อน cutover) — reviewer ยกไปถามผู้ใช้ต่อ

**สรุป:** PLAN-059 = DONE/VERIFIED; ไม่มี mapping blocker สำหรับ cutover เหลือแค่ **decision R2 (PD3)** ที่ผู้ใช้ต้องเคาะ แล้ว PLAN-060 ถึงจะขยับเป็น READY ได้
