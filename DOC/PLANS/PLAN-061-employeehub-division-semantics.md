# PLAN-061: Division semantics สำหรับ EmployeeHub migration — กติกา NLC = company, อื่น ๆ = division name

- **Status:** READY (เอกสารกติกา + ปรับ spec ของ PLAN-058/059 — ไม่มีงานโค้ดของตัวเอง)
- **Assigned:** — (เป็น addendum ที่ Gemini ต้องอ่านคู่กับ PLAN-058 และ GPT อ่านคู่กับ PLAN-059)
- **Reviewer:** Claude Code
- **สร้างเมื่อ:** 2026-07-09
- **อ้างอิง:** [PLAN-058](PLAN-058-employeehub-provider-foundation.md), [PLAN-059](PLAN-059-employeehub-division-mapping-audit.md)

> คำอธิบายผู้ใช้ (2026-07-09): ระบบเดิมแบ่งตาม Division ตรง ๆ แต่ระบบใหม่ Division ชื่อ **'NLC'** หมายถึงพนักงานทุกคนใน company `NLC` ส่วน Division อื่นคือ division ที่อยู่ภายใต้ NTC (คน VDS ใช้ชื่อ division ร่วมกับ NTC)

## EmployeeHub base URL ต่อ environment (ผู้ใช้ยืนยัน 2026-07-09)

| Env | API base (endpoints ต่อท้าย `/api/...`) | Explorer |
|---|---|---|
| QA / dev | `http://10.10.143.39/Tools/EmployeeHub/Service` | `.../scalar` |
| PROD | `http://AP-NTC2137-PRWB/Tools/EmployeeHub/Service` | `.../scalar` |

> ⚠️ `EmployeeHubBaseUrl` ใน config = base **ไม่รวม `/scalar`** (นั่นคือหน้า UI explorer เท่านั้น); เป็น `http://` ไม่ใช่ `https://`; PROD อยู่บนเว็บเซิร์ฟเวอร์ PROD ตัวเดียวกับ iLearn.API (`AP-NTC2137-PRWB`) — latency ต่ำ

---

## กติกา (Business rule — source of truth สำหรับทุกแผน EmployeeHub)

การตีความค่า "division" ใน iLearn (ค่าจาก `Divisions` master data / ตัวเลือกใน Bulk Assign / claim isolation):

| ค่า division ฝั่ง iLearn | เงื่อนไข filter บน EmployeeHub |
|---|---|
| `NLC` | `EmployeeDto.Company == "NLC"` (ครอบ OrgCode prefix 640 + 650 ทั้งหมด) — **ห้าม** match field `Division` (free-text ของพนักงาน NLC คือ `PD`/`AD` ไม่ใช่ `NLC`) |
| ค่าอื่น (เช่น `PD1`, `CSD`) | `EmployeeDto.Division == ค่า` (case-insensitive) โดย**ไม่กรอง company** → ครอบพนักงาน NTC + VDS ที่ใช้ชื่อ division ร่วมกัน — ตรงพฤติกรรม upstream เดิม (ยืนยันจากข้อมูล: VDS ใช้ `PD3`/`PD4` ฯลฯ ชุดเดียวกับ NTC) |

รายการ divisions ที่ระบบแสดง (แทน `GetDistinctDivisions` เดิม) = `["NLC"] ∪ distinct(Division ของพนักงานที่ Company != "NLC")` คำนวณจาก directory cache ใน provider — **ห้ามใช้ `GET /api/lookups/divisions` ตรง ๆ** เพราะ (1) ไม่มีค่า NLC (2) ปนค่า `AD`, `PD`, `AD Division`, `PD Division` ของฝั่งลาวซึ่งระบบเดิมไม่เคยแสดง (3) param `?company=` ของ dimension นี้ยังไม่ทำงาน

## หลักฐาน (lookup จริง 2026-07-09 โดย Claude)

- `GET /api/lookups/companies` → `NLC, NTC, VDS`
- `GET /api/lookups/divisions` → 18 ค่า: `AD, AD Division, CSD, DP-CGA, DP-SHD, ECD, ELD, FAD, MED, PCD, PD, PD Division, PD1, PD2, PD3, PD4, PNP, QAD` (ไม่มี NLC)
- Upstream เดิม `GetDistinctDivisions` → 15 ค่า: `NLC` + 14 division ของ NTC
- ตัวอย่างพนักงาน: NLC → `division='PD'|'AD'`; VDS → `division='PD3'|'PD4'` (ชื่อร่วมกับ NTC); NTC → `division='CSD'|'ECD'|...`
- **iLearnDB (QA) `Divisions`**: `CSD, NLC, PD1, PD2, PD3, Test` — ทุกค่า (ยกเว้น `Test`) ตรงกับค่าที่กติกาข้างบน resolve ได้ → ไม่ต้อง remap master data
- **`Assignments.Division` (string legacy) = NULL ทั้งตาราง** → ไม่มี data ต้อง migrate ในคอลัมน์นี้
- org tree ฝั่ง NLC มี 2 company roots: `640...` (AD=29, PD=1026 คน) และ `650...` (ต้นไม้ว่าง + root กำพร้า prefix 650 ถือพนักงาน ~31 คน) — ดู "ประเด็นฝั่ง EmployeeHub" ด้านล่าง

### ✅ Validation สำคัญ — กติกา reproduce รายการเดิมเป๊ะ

`distinct(Division where Company != "NLC")` จาก EmployeeHub (ตัด 4 ค่าลาว `AD/PD/AD Division/PD Division` ที่เป็น Company=NLC ออก) = **14 ค่า**: `CSD, DP-CGA, DP-SHD, ECD, ELD, FAD, MED, PCD, PD1, PD2, PD3, PD4, PNP, QAD` — **ตรงกับ 14 division ที่ upstream เดิมคืน 100%** ⇒ รายการสุดท้าย `["NLC"] ∪ …` = 15 ค่า = ชุดเดียวกับ `GetDistinctDivisions` เดิมเป๊ะ ไม่มีค่าขาด/เกิน (นี่คือหลักฐานว่ากติกาไม่ทำให้ผู้ใช้เห็น division เปลี่ยนไปหลัง cutover)

หมายเหตุ prefix→company (จาก skill): `620→NTC, 630→VDS, 640→NLC, 650→NLC` — ทั้ง 640+650 = company NLC จึงถูกกวาดด้วยกฎ `Company=="NLC"` ครบ และ VDS (630) ⊆ ชุดชื่อ division ของ NTC จึงไม่มีค่าใหม่โผล่

## ผลต่อแผนอื่น

### แก้ spec PLAN-058 (S3) — Gemini ใช้ตารางนี้แทน 2 แถวเดิม

| Method | ทำอย่างไร (ฉบับแก้) |
|---|---|
| `GetLearnersByDivisionsAsync(divisions[])` | ต่อค่าใน `divisions[]`: `'NLC'` → filter cache ด้วย `Company == "NLC"`; ค่าอื่น → filter ด้วย `Division == ค่า` (case-insensitive); union ผลลัพธ์ + skip/take/total shape เดิม |
| `GetDivisionsAsync` | คืน `["NLC"] + distinct(Division where Company != "NLC")` จาก cache — จัด shape ให้ตรงของเดิม (`{ data: [{ Name: ... }] }`) |
| `GetSectionsAsync`/`GetDepartmentsAsync`/`GetPositionsAsync` | เปลี่ยนเป็นคำนวณ distinct จาก directory cache เช่นกัน (field Section/Department/Grade) เพื่อให้ประชากรสอดคล้องกับกติกา division ข้างบน — ไม่ใช้ `/api/lookups/*` แล้ว (ตัด dependency + semantics ตรงกว่า) |
| Unit test เพิ่ม | case `'NLC'` → ได้เฉพาะ company NLC; case `'PD3'` → ได้ทั้ง NTC+VDS; รายการ divisions ไม่มี `AD`/`PD`/`AD Division`/`PD Division` |

### แก้ scope PLAN-059 — งาน audit ส่วนใหญ่ปิดแล้วด้วยหลักฐานข้างบน เหลือให้ GPT ทำแค่:

1. ตรวจ snapshot ใน `EnrollmentAssignments` (คอลัมน์ snapshot JSON) ว่ามี division/section ฝังไหม + มีค่าที่ขัดกติกาใหม่ไหม
2. ตรวจ `Divisions` แถว `Test` (Id=6) — เสนอผู้ใช้ลบ/ปิด IsActive ถ้าไม่มี FK ใช้งาน
3. เทียบ `Divisions` บน **PROD DB** ว่าตรงกับ QA (read-only)
4. ข้าม A1/A2/A4 เดิม — ใช้หลักฐานในไฟล์นี้แทนได้เลย

## ประเด็นฝั่ง EmployeeHub (ผู้ใช้แก้เองได้ — เรียงตามความคุ้ม)

1. **ซ่อม org structure บริษัท 650:** company root `650000...` ไม่มีพนักงาน (มี `AD Division`/`PD Division` เปล่า) และมี orphan roots prefix 650 (~31 คน เช่น `Camera Assembly-OFFICE STAFF`) — org-chain/approval ของคนกลุ่มนี้ใช้ไม่ได้; ไม่ block iLearn (กติกา NLC ใช้ Company จึงครอบคนกลุ่มนี้อยู่แล้ว) แต่ควรซ่อม
2. `?company=` บน `/api/lookups/divisions` ยังถูกเมิน — ถ้าทำให้ทำงานจะเป็นประโยชน์กับผู้บริโภคbroader (iLearn ไม่รอ)
3. (ทางเลือก) เพิ่ม param `division` บน `GET /api/employees` — iLearn ไม่จำเป็น (ใช้ cache) แต่ลด full-directory pull สำหรับ consumer อื่น
4. ~~**Auth ก่อน PROD cutover**~~ **✅ ตัดสินแล้ว (2026-07-09): risk-acceptance "trusted internal network"** — คงเปิดตามเดิม ไม่ใส่ auth/allow-list พึ่งเน็ตเวิร์กภายใน; iLearn ไม่ต้องแก้โค้ด. Residual risk (ops endpoints + scalar/swagger เปิดทั้งอินทราเน็ต) ยอมรับแล้ว — hardening เฉพาะ `/api/sync/*` เป็นทางเลือกอนาคต ดู PLAN-060 "Residual risk"

## Acceptance

- Gemini implement PLAN-058 ตามตารางฉบับแก้ในไฟล์นี้ (ไฟล์นี้ชนะเมื่อขัดกับ PLAN-058 เดิม)
- GPT ทำ PLAN-059 เฉพาะ 4 ข้อที่เหลือ
