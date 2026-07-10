# PLAN-062: Normalize NLC division ใน EmployeeHub provider + คืน config default = Legacy

- **Status:** READY
- **Assigned:** Gemini (Antigravity)
- **Reviewer:** Claude Code
- **Priority:** **Critical — block PLAN-060 Phase 2 GATE** (ห้ามแตะ PROD จนแผนนี้ VERIFIED + re-smoke NLC บน QA ผ่าน)
- **สร้างเมื่อ:** 2026-07-09
- **อ้างอิง:** [PLAN-058](PLAN-058-employeehub-provider-foundation.md), [PLAN-060](PLAN-060-employeehub-cutover-qa-prod.md), [PLAN-061](PLAN-061-employeehub-division-semantics.md) (กติกา NLC = company)

> ที่มา: รีวิวรอบ 3 ของ Claude (2026-07-09) หลัง GPT cutover QA — พบ 2 finding: (A) NLC division isolation พังทุก path ยกเว้น Bulk Assign, (B) base config default ถูก flip เป็น EmployeeHub ทำลาย fail-safe. GPT เองก็เห็น symptom ("Grid filter NLC returns 0") แต่ตีเป็น non-blocking — **หลักฐานใหม่ยืนยันว่า blocking**: QA DB มี Role `NLC` (Id=10, DivisionId=5) และมี **user จริง 5 คนถืออยู่** (`h8193, d6132, n7710, q2186, q2825`) → ตอนนี้ทั้ง 5 คนเห็น Learners grid ว่าง + เปิด profile ใครไม่ได้บน QA

---

## Finding A — root cause

ระบบเดิม (EmployeeServiceV2) พนักงาน NLC มี `Division="NLC"` ตรง ๆ แต่ EmployeeHub ลดสถานะ NLC เป็น **Company** (พนักงาน NLC มี `Division` free-text = `PD`/`AD`) — PLAN-058 ใส่กติกา `Company=="NLC"` ไว้แค่ใน `GetLearnersByDivisionsAsync` (Bulk Assign) ที่เดียว path อื่นส่ง `e.Division` ดิบออกไป:

| Path | จุดโค้ด | ผลกับ admin ที่ claim `Division="NLC"` |
|---|---|---|
| Learners grid | `LearnersController.Get()` inject `["Division","=","NLC"]` → `GetLearnersDxGridAsync` map `Division = e.Division` | **0 แถวเสมอ** |
| Learner profile | `profile/{code}` เทียบ `learnerInfo.Division == DivisionName` โดย `GetLearnerByCodeAsync` map `Division = emp.Division` | **404 ทุกคน** (isolation ปฏิเสธหมด) |
| Cascade dept/section | `GetDepartments/Sections?filter=["Division","=","NLC"]` กรอง cache ด้วย field `Division` | **dropdown ว่าง** |
| Users enrichment | `GetEmployeesByNidsAsync` → `EmployeeCsvDto.Division = e.Division` | โชว์ `PD`/`AD` แทน `NLC` (แสดงผลเพี้ยน) |

**หลักการแก้ (normalize at the boundary):** คืน contract เดิมของระบบที่ชั้น provider — พนักงานที่ `Company=="NLC"` ให้ `Division="NLC"` **ทุกจุดที่ provider ปล่อยข้อมูลออก** → grid/profile/cascade/enrichment/isolation สอดคล้องกันเองหมดโดยไม่ต้องแก้ทีละ path และไม่แตะ controller/React

## Scope

- [ ] **S1 — Normalize helper ใน `EmployeeHubLearnerApiService`:**
  ```csharp
  private static EmployeeDto NormalizeDivision(EmployeeDto e)
  {
      if (string.Equals(e.Company, "NLC", StringComparison.OrdinalIgnoreCase))
          e.Division = "NLC";
      return e;
  }
  ```
  Apply ที่ **ทุก ingress ของ `EmployeeDto` เข้า service (3 จุด)**:
  1. `GetActiveEmployeesCachedAsync` — normalize ทุก item ตอนสะสมหน้าเข้า list ก่อน cache → grid, `/all`, by-codes, by-divisions, lookups ทั้งหมดได้ผลอัตโนมัติ (รวมถึง filter `["Division","=","NLC"]` ที่ DataSourceLoader รันบน cache)
  2. `GetLearnerByCodeAsync` — normalize ตัวที่ fetch สด (แก้ profile isolation)
  3. `GetEmployeesByNidsAsync` — normalize items จาก find-by-nids (แก้ `EmployeeCsvDto.Division`)
- [ ] **S2 — Tests ใหม่ (อย่างน้อย 4):**
  1. `GetLearnersDxGridAsync` + filter `["Division","=","NLC"]` (shape เดียวกับที่ `InjectDivisionFilter` สร้าง) → ได้เฉพาะพนักงาน `Company=="NLC"` และ row โชว์ division `NLC`
  2. `GetLearnerByCodeAsync` พนักงาน NLC → `result.Division == "NLC"`
  3. `GetDepartmentsAsync` + filter `["Division","=","NLC"]` → ได้ departments ของพนักงาน NLC (ไม่ว่าง)
  4. `GetEmployeesByNidsAsync` พนักงาน NLC → `EmployeeCsvDto.Division == "NLC"`
  - test เดิม 128 ตัวต้องเขียวหมด (หมายเหตุ: `GetDivisionsAsync_AppliesDistinctCompanySemantics` ใช้ NLC/`PD_LA` — ยังผ่านเพราะ `Where(Company != "NLC")` กรองด้วย Company ไม่ใช่ Division)
- [ ] **S3 — คืน config default = Legacy (Finding B):**
  1. `appsettings.json` (base): `"Provider": "EmployeeHub"` → กลับเป็น `"Legacy"` (fail-safe default ตามหลักการ PLAN-058)
  2. สร้าง **`iLearn.API/appsettings.Staging.json`** ใหม่ (QA รัน `ASPNETCORE_ENVIRONMENT=Staging` — ยืนยันจาก log GPT) — override เฉพาะ key ที่ต่าง:
     ```json
     {
       "EmployeeServiceSettings": {
         "Provider": "EmployeeHub"
       }
     }
     ```
     (`EmployeeHubBaseUrl` ไม่ต้องใส่ — config layering merge per-key, base ชี้ QA URL `10.10.143.39` ถูกอยู่แล้ว)
  3. ตรวจว่า publish output รวม `appsettings.Staging.json` (SDK copy `appsettings*.json` by default — ยืนยันใน `bin`/publish ก็พอ)
  - ผลลัพธ์สุทธิ: default ทุก env = Legacy, QA opt-in ผ่าน Staging.json, PROD ยัง Legacy (Production.json) จนกว่า Phase 3 ของ PLAN-060 จะสั่ง flip

## Out of scope (ห้ามแตะ)

- ❌ `GetLearnersByDivisionsAsync` และ `GetDivisionsAsync` — logic ปัจจุบันถูกแล้ว (ใช้ `Company` ซึ่ง normalization ไม่กระทบ) ผลลัพธ์เท่าเดิม
- ❌ DTO ใน `EmployeeHubClient.cs` — mirror ของ EmployeeHub ต้องคงตามต้นทาง (normalization เป็น semantics ชั้น provider ไม่ใช่ transport)
- ❌ React ทุกไฟล์ / controller / `appsettings.Production.json` (ห้าม flip PROD)
- ❌ deploy ขึ้น QA — เป็นงาน GPT ใน PLAN-060 หลังแผนนี้ VERIFIED

## หมายเหตุ display ที่ตั้งใจ (ไม่ใช่ bug)

หลัง normalize พนักงาน NLC จะโชว์ Division = `NLC` บน grid/CSV enrichment (แทน `PD`/`AD` ที่เห็นบน QA ช่วงนี้) — **ตรงกับพฤติกรรม legacy เดิม** sub-division ของ NLC ยังดูได้จาก Department/Section ที่ไม่ถูกแตะ

## Acceptance criteria

1. Unit: 4 test ใหม่ผ่าน + 128 เดิมเขียวหมด
2. `Provider` default = `Legacy` ใน base/Development/Production; `Staging.json` = `EmployeeHub` ไฟล์เดียว
3. รายการ divisions จาก `GetDivisionsAsync` ยัง 15 ค่าเดิมเป๊ะ (ไม่มีค่าลาว `AD`/`PD` โผล่)
4. ไม่มีการแก้ interface/DTO เดิม/React

## Verification (รันก่อนปิดงาน)

```powershell
dotnet build iLearn.Tests -o artifacts/verify-test
dotnet test artifacts/verify-test/iLearn.Tests.dll
Remove-Item -Recurse -Force artifacts/verify-test
```
(ใช้ forward slash — backslash ใน bash เคยทำ path เพี้ยนเป็น `artifactsverify-test` มาแล้ว)

## Handoff หลัง VERIFIED (งาน GPT — PLAN-060)

1. redeploy QA (stamp ใหม่) — ได้ base=Legacy + Staging.json=EmployeeHub → QA ยังวิ่ง EmployeeHub เหมือนเดิม
2. **re-smoke ด้วย user NLC จริง** (เช่น `n7710`): Learners grid มีข้อมูล / เปิด profile พนักงาน NLC ได้ / cascade dept-section ไม่ว่าง
3. ผ่านแล้วจึงเข้า Phase 2 GATE ได้

## Implementer Notes

(เติมหลังทำเสร็จ)
