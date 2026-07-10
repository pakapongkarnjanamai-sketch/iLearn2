# PLAN-060: Cutover ไป EmployeeHub บน QA → PROD (flip provider flag)

- **Status:** IN-PROGRESS (soak) — Phase 0+1 done; [PLAN-062](PLAN-062-employeehub-nlc-normalization.md) **VERIFIED + redeploy stamp `20260710080811` + NLC re-smoke 4/4 ผ่าน (2026-07-10)** → เงื่อนไข GATE ข้อ (1) ครบแล้ว; เหลือ **soak QA 2-3 วันทำการ + ผู้ใช้ยืนยันเป็นข้อความ (GATE ข้อ 2) ก่อนแตะ PROD**
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
- [x] **R2 — un-delete PD3 บน QA** — 1 row affected ✅
- [x] **R1 — soft-delete `Test` (Id=6) บน PROD** — 1 row affected ✅
- [x] verify: ทั้ง QA+PROD ตรงกัน (PD1,PD2,CSD,PD3,NLC active; Test deleted) ✅

### Phase 1 — QA
- [x] แก้ `EmployeeServiceSettings.Provider` = `EmployeeHub` ใน `appsettings.json` (base) → QA (Staging env) picks up; `Development.json` + `Production.json` ยังคง Legacy
- [x] deploy API → stamp `_deploy_20260709164236` (previous: `_deploy_20260709110854`)
- [x] smoke tests ผ่านทั้งหมด:
  - ✅ `/api/health/smoke`: employeeDirectory = EmployeeHub (Healthy) 37ms
  - ✅ Learners grid: totalCount=8,077 (ตรง upstream), filter PD1=1,590, search "HIRO"=7
  - ✅ Profile: employee 000191 returns correct data
  - ✅ Cascade lookups: GetDivisions 15 divisions, GetDepartments PD1→3, GetSections PD1/CAE→5
  - ✅ Bulk Assign NLC path: 1,244 employees (filter by Company works)
  - ✅ Session/Me: DisplayName="PAKHAPONG KANCHANAMAI"
  - ✅ Admin React frontend: 200
  - ⚠️ Grid filter `division=NLC` returns 0 (NLC employees' grid row has actual Division, not "NLC") — Bulk Assign uses dedicated API ที่ handle NLC ถูก, grid row แสดง division จริงจาก EmployeeHub
  - 🔴 **Reviewer override (Claude, รอบ 3): ข้อ ⚠️ ข้างบนไม่ใช่ non-blocking** — QA DB มี Role `NLC` (Id=10, DivisionId=5) + user ถือจริง 5 คน (`h8193, d6132, n7710, q2186, q2825`) → ทั้ง 5 คนตอนนี้ grid ว่าง/profile 404/cascade ว่างบน QA; smoke 10 ข้อไม่ครอบเคส NLC-scoped admin จึงหลุด → แก้ที่ [PLAN-062](PLAN-062-employeehub-nlc-normalization.md)
- [x] **redeploy QA หลัง PLAN-062 VERIFIED + re-smoke 3 เคส NLC admin** — deploy stamp `20260710080811` + re-smoke ผ่านทั้ง 3 เคส:
  - ✅ Grid filter `Division=NLC`: totalCount=1,230 (rows แสดง division="NLC")
  - ✅ Profile NLC employee (N130058): Division="NLC", Department="Camera Assembly"
  - ✅ Cascade GetDepartments(Division=NLC): 9 departments
  - ✅ Cascade GetSections(Division=NLC, Dept=Camera Assembly): 152 sections
- [ ] soak QA อย่างน้อย 2-3 วันทำการ ผู้ใช้ยืนยัน
### Phase 2 — GATE: (1) PLAN-062 VERIFIED + NLC re-smoke ผ่าน (2) รอผู้ใช้ยืนยันเป็นข้อความก่อนแตะ PROD
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

### Phase 0 (2026-07-09 GPT)
- R2 (QA) + R1 (PROD) ทำสำเร็จ, verify ตรงกันทั้ง 2 env

### Phase 1 (2026-07-09 GPT)
- **Config strategy**: เปลี่ยน base `appsettings.json` Provider → "EmployeeHub" เพราะ QA deploy script (`deploy-api.ps1`) pin `SetEnvironmentName = 'Staging'` + exclude `appsettings.Production.json` → QA ใช้แค่ base config; `Development.json` ยัง Legacy (local dev safe); `Production.json` ยัง Legacy (PROD safe)
- **Deploy**: `deploy-api.ps1` → stamp `20260709164236`, publish Release, OfflineStrategy=AppOffline, cleaned 1 stale folder
- **Smoke**: 10/10 checks passed (ดูรายละเอียดใน Phase 1 checklist)
- **NLC grid note**: Grid filter "NLC" returns 0 เพราะ `LearnerGridRowDto.Division` map จาก `emp.Division` (e.g. "PD") ไม่ใช่ "NLC"; ข้อนี้เป็น behavior ที่ต่างจาก legacy (legacy แสดง NLC เป็น division ใน grid) แต่ Bulk Assign path ที่ business-critical (ใช้ `GetLearnersByDivisionsAsync` ซึ่ง filter by Company) ทำงานถูก
- **Rollback path**: flip `appsettings.json` Provider กลับ "Legacy" + redeploy หรือ web.config switch กลับ `_deploy_20260709110854`
- **Next**: soak QA 2-3 วันทำการ → ผู้ใช้ยืนยัน Phase 2 GATE → Phase 3 PROD

### Phase 1 re-deploy (2026-07-10 GPT — post PLAN-062)
- **PLAN-062 verified**: 132/132 tests pass (128 original + 4 new NLC normalization tests); `NormalizeDivision` applied at all 3 ingress points; base config reverted to Legacy; `appsettings.Staging.json` overrides to EmployeeHub for QA
- **Deploy**: `deploy-api.ps1` → stamp `20260710080811` (previous: `20260709164236`)
- **NLC re-smoke (all pass)**:
  - Grid filter `Division=NLC`: totalCount=1,230 — rows show `division="NLC"` correctly
  - Profile `N130058` (NLC employee): 200 OK, `division="NLC"`, `department="Camera Assembly"`
  - Cascade departments(Division=NLC): 9 departments returned
  - Cascade sections(Division=NLC, Camera Assembly): 152 sections returned
- **Rollback path**: web.config switch back to `_deploy_20260709164236` or flip Staging.json Provider→Legacy
- **Next**: soak QA (NLC isolation fixed) → ผู้ใช้ยืนยัน Phase 2 GATE → Phase 3 PROD
