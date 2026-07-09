# PLAN-060: Cutover ไป EmployeeHub บน QA → PROD (flip provider flag)

- **Status:** DRAFT — ห้ามเริ่มจนกว่า PLAN-058 = VERIFIED, PLAN-059 มี Findings และผู้ใช้เคาะเรื่อง remap + auth ของ EmployeeHub แล้ว (Claude จะปรับเป็น READY เอง)
- **Assigned:** GPT (GitHub Copilot)
- **Reviewer:** Claude Code
- **Priority:** Medium
- **สร้างเมื่อ:** 2026-07-09
- **อ้างอิง:** [PLAN-058](PLAN-058-employeehub-provider-foundation.md), [PLAN-059](PLAN-059-employeehub-division-mapping-audit.md), [PLAN-057](PLAN-057-deploy-category-description-qa-prod.md) (แบบอย่าง runbook + GATE)

---

## Prerequisites

1. PLAN-058 VERIFIED และ deploy ขึ้น env เป้าหมายแล้ว (โค้ดมี provider switch แต่ยังเป็น Legacy)
2. ผล audit PLAN-059 ถูก resolve — data remap (ถ้ามี) ทำเสร็จเป็นแผนแยกก่อน cutover
3. **ผู้ใช้ตัดสินใจเรื่อง auth/การป้องกัน EmployeeHub แล้ว** (ตอนนี้ API เปิดหมดรวม ops endpoints — อย่างน้อยควรมี network/IIS-level control ก่อนให้ production พึ่ง) + ยืนยัน URL ของ EmployeeHub ต่อ env
4. EmployeeHub sync pipeline เดินปกติ (เช็ค `GET /api/sync/runs?take=5` — run ล่าสุด Succeeded ไม่เก่าเกิน 2 วัน)

## Scope (โครง — เติมรายละเอียดตอนปรับเป็น READY)

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
