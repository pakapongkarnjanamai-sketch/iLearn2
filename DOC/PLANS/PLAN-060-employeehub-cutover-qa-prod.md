# PLAN-060: Cutover ไป EmployeeHub บน QA → PROD (flip provider flag)

- **Status:** DRAFT — ห้ามเริ่มจนกว่า PLAN-058 = VERIFIED, PLAN-059 มี Findings และผู้ใช้เคาะเรื่อง remap + auth ของ EmployeeHub แล้ว (Claude จะปรับเป็น READY เอง)
- **Assigned:** GPT (GitHub Copilot)
- **Reviewer:** Claude Code
- **Priority:** Medium
- **สร้างเมื่อ:** 2026-07-09
- **อ้างอิง:** [PLAN-058](PLAN-058-employeehub-provider-foundation.md), [PLAN-059](PLAN-059-employeehub-division-mapping-audit.md), [PLAN-057](PLAN-057-deploy-category-description-qa-prod.md) (แบบอย่าง runbook + GATE)

---

## Prerequisites

1. ✅ **PLAN-058 VERIFIED** (2026-07-09) — โค้ดมี provider switch, default Legacy; ยังต้อง deploy ขึ้น env เป้าหมายก่อน flip
2. ✅ **PLAN-059 VERIFIED** (2026-07-09) — audit ยืนยันไม่มี division ต้อง remap; เหลือแค่ data-cleanup 2 จุด (R1/R2) ที่ย้ายมาเป็น Phase 0 ด้านล่าง (ผู้ใช้เคาะแล้ว)
3. ⏳ **ผู้ใช้ตัดสินใจเรื่อง auth/การป้องกัน EmployeeHub** (ตอนนี้ API เปิดหมดรวม ops endpoints — อย่างน้อยควรมี network/IIS-level control ก่อนให้ production พึ่ง) — **ยังค้าง = ตัว block หลักที่เหลือ**; URL ต่อ env ยืนยันแล้ว (PLAN-061)
4. EmployeeHub sync pipeline เดินปกติ (เช็ค `GET /api/sync/runs?take=5` — run ล่าสุด Succeeded ไม่เก่าเกิน 2 วัน)

> สถานะ prerequisite: #1,#2 ✅ + ผู้ใช้เคาะ R1/R2 แล้ว — **เหลือ #3 (auth) เป็น blocker เดียว** ก่อนปรับ PLAN-060 เป็น READY

## Scope (โครง — เติมรายละเอียดตอนปรับเป็น READY)

### Phase 0 — Data cleanup ก่อน flip (จาก PLAN-059 R1/R2 — ผู้ใช้เคาะแล้ว 2026-07-09)
ทำก่อน flip provider; เป็น write เล็ก ๆ FK=0 ทั้งคู่ ไม่ต้อง backup แต่ให้ verify count ก่อน/หลัง
- [ ] **R2 — un-delete PD3 บน QA** (ผู้ใช้เลือก "เปิดใช้ PD3" ให้ตรง PROD): 
  ```sql
  -- QA เท่านั้น (PROD PD3 active อยู่แล้ว)
  UPDATE Divisions SET IsDeleted = 0, DeletedAt = NULL, DeletedBy = NULL
  WHERE Id = 4 AND Name = 'PD3' AND IsDeleted = 1;   -- คาดกระทบ 1 แถว
  ```
- [ ] **R1 — soft-delete `Test` (Id=6) บน PROD** ให้ตรง QA (FK=0):
  ```sql
  -- PROD เท่านั้น (QA soft-deleted อยู่แล้ว)
  UPDATE Divisions SET IsDeleted = 1, DeletedAt = GETDATE(), DeletedBy = 'plan060-cutover'
  WHERE Id = 6 AND Name = 'Test' AND IsDeleted = 0;   -- คาดกระทบ 1 แถว
  ```
- [ ] verify หลังทำ: `SELECT Id,Name,IsActive,IsDeleted FROM Divisions ORDER BY Id` บนทั้ง 2 env ต้องได้ชุดเดียวกัน (PD1,PD2,CSD,PD3 active; NLC active; Test deleted)

### Phase 1 — QA
- [ ] แก้ `EmployeeServiceSettings.Provider` เป็น `EmployeeHub` (QA config) → deploy API → smoke:
  - `/api/health/smoke` check `employeeDirectory` ผ่าน
  - learner login ด้วย EId จริง 1 คนบน QA (iLearn.User)
  - หน้า Learners grid: filter/sort/search/paging + หน้า profile
  - Bulk Assign: เลือก division → รายชื่อขึ้นถูกต้อง (จุดเสี่ยงสุดจาก mapping)
  - Admin Users: DisplayName ขึ้น (แทน CSV เดิม)
  - เทียบจำนวนพนักงาน active รวมกับ upstream เดิม (sanity ±)
- [ ] soak QA อย่างน้อย 2-3 วันทำการ ผู้ใช้ยืนยัน
### Phase 2 — GATE: รอผู้ใช้ยืนยันเป็นข้อความก่อนแตะ PROD
### Phase 3 — PROD
- [ ] backup ไม่จำเป็น (ไม่แตะ DB) แต่ต้องมี rollback ชัด: flip config กลับ `Legacy` + recycle app pool = กลับสถานะเดิมทันที (LearnerApiService เดิมยังอยู่ในโค้ด)
- [ ] flip Provider บน PROD → deploy → smoke ชุดเดียวกับ QA
### Phase 4 — หลัง soak PROD (แผนแยกในอนาคต)
- ถอด `LearnerApiService` legacy + `BaseEmployeeCsvUrl`/`BaseLearnerUrl` เดิม + ปิด EmployeeServiceV2 dependency — **อย่าทำในแผนนี้**

## Out of scope
- ❌ ลบโค้ด legacy (Phase 4 = แผนอนาคต)
- ❌ แก้ข้อมูล DB
- ❌ ฝั่ง EmployeeHub เอง

## Implementer Notes

(เติมตอนดำเนินการ)
