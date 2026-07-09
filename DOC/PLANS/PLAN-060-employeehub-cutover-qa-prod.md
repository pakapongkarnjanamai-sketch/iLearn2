# PLAN-060: Cutover ไป EmployeeHub บน QA → PROD (flip provider flag)

- **Status:** READY — prerequisite ครบทั้งหมด (PLAN-058/059 VERIFIED, ผู้ใช้เคาะ R1/R2 + auth แล้ว 2026-07-09); GPT รับไป cutover ตาม Phase 0→3 ได้ แต่ **Phase 2 GATE ต้องรอผู้ใช้ยืนยันเป็นข้อความก่อนแตะ PROD**
- **Assigned:** GPT (GitHub Copilot)
- **Reviewer:** Claude Code
- **Priority:** Medium
- **สร้างเมื่อ:** 2026-07-09
- **อ้างอิง:** [PLAN-058](PLAN-058-employeehub-provider-foundation.md), [PLAN-059](PLAN-059-employeehub-division-mapping-audit.md), [PLAN-057](PLAN-057-deploy-category-description-qa-prod.md) (แบบอย่าง runbook + GATE)

---

## Prerequisites

1. ✅ **PLAN-058 VERIFIED** (2026-07-09) — โค้ดมี provider switch, default Legacy; ยังต้อง deploy ขึ้น env เป้าหมายก่อน flip
2. ✅ **PLAN-059 VERIFIED** (2026-07-09) — audit ยืนยันไม่มี division ต้อง remap; เหลือแค่ data-cleanup 2 จุด (R1/R2) ที่ย้ายมาเป็น Phase 0 ด้านล่าง (ผู้ใช้เคาะแล้ว)
3. ✅ **Auth — ผู้ใช้ตัดสินใจแล้ว (2026-07-09): risk-acceptance "trusted internal network"** — คง EmployeeHub เปิดตามเดิม (ไม่มี app auth / ไม่มี IP allow-list) โดยพึ่งว่าเป็นเน็ตเวิร์กภายในที่ปลอดภัย ให้ทุกเครื่องเข้าถึงได้ไม่มี friction → iLearn ไม่ต้องแก้โค้ด (`EmployeeHubClient`/health ไม่ต้องส่ง credential/key); **cutover ไม่ถูก block ด้วยเรื่อง auth** — ดู "Residual risk (ยอมรับแล้ว)" ด้านล่าง
4. EmployeeHub sync pipeline เดินปกติ (เช็ค `GET /api/sync/runs?take=5` — run ล่าสุด Succeeded ไม่เก่าเกิน 2 วัน) — **เช็คตอนเริ่ม Phase 1**

> สถานะ prerequisite: **ครบทั้งหมด** — PLAN-060 = READY

### Residual risk (ผู้ใช้ยอมรับแล้ว — ไม่ block, บันทึกไว้เพื่อความโปร่งใส)
- ops endpoints (`POST /api/sync/run`, `POST /api/sync/backfill-terminations`) + `/scalar` `/swagger` เปิดให้ทุกเครื่อง/ทุกคนในอินทราเน็ต — ใครในเน็ตเวิร์กก็ trigger sync/backfill หรือเปิดดู PII ผ่าน explorer ได้
- iLearn ใช้เฉพาะ consume (อ่าน) จึงไม่ได้เพิ่มความเสี่ยงนี้; ความเสี่ยงมีอยู่ก่อน iLearn cutover แล้ว
- **hardening ทางเลือกในอนาคต (ไม่กระทบ consumer ใด ๆ):** firewall/IIS rule กันเฉพาะ `/api/sync/*` ให้เรียกได้จาก host ของ scheduler + localhost, และปิด `/scalar` `/swagger` บน PROD — เปิดเป็นแผนแยกได้เมื่อพร้อม

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
