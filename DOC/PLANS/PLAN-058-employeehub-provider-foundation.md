# PLAN-058: EmployeeHub Provider — client + translation layer หลัง `ILearnerApiService` (feature-flag, ยังไม่เปิดใช้)

- **Status:** READY
- **Assigned:** Gemini (Antigravity)
- **Reviewer:** Claude Code
- **Priority:** High
- **Estimated scope:** backend ~5 ไฟล์ (ไฟล์ใหม่ 2) + config + tests — **ห้ามแตะ React**
- **สร้างเมื่อ:** 2026-07-09
- **อ้างอิง:** skill `C:\Users\n4734\.agents\skills\api-employeehub-api-reference\SKILL.md` (**อ่านก่อนเริ่ม — จำเป็น**), [PLAN-059](PLAN-059-employeehub-division-mapping-audit.md), [PLAN-060](PLAN-060-employeehub-cutover-qa-prod.md)

> คำขอผู้ใช้ (2026-07-09): ย้ายฐานข้อมูลพนักงานไปใช้ EmployeeHub ตัวใหม่ โดยเริ่มจากปิด gap ที่พบก่อน

> ⚠️ **ต้องอ่าน [PLAN-061](PLAN-061-employeehub-division-semantics.md) ก่อน implement** — มีกติกา division (NLC = company, อื่น ๆ = division name) + EmployeeHub URL จริง (`http://10.10.143.39/Tools/EmployeeHub/Service`) และ **ตาราง S3 ฉบับแก้** ที่ชนะสเปกในไฟล์นี้

---

## Problem / เป้าหมาย

ข้อมูลพนักงานของ iLearn ทั้งหมดไหลผ่าน `ILearnerApiService` จุดเดียว โดย implementation ปัจจุบัน ([LearnerApiService.cs](iLearn.Infrastructure/Services/LearnerApiService.cs)) ยิงไป `EmployeeServiceV2` (Student/StudentLookup) + `Employee.Service GetAllCSV` เราจะย้ายไป **EmployeeHub** โดยเขียน implementation ใหม่ของ interface เดิม สลับได้ด้วย config flag — แผนนี้สร้างของทั้งหมดแต่ **default ยังเป็น Legacy** (deploy แล้วพฤติกรรมไม่เปลี่ยนจนกว่า PLAN-060 จะสั่งเปิด)

Gap ที่แผนนี้ปิด: (1) Learners grid เป็น DevExtreme passthrough ที่ EmployeeHub ไม่มี, (2) EmployeeHub ไม่มี field `Position` (มี `Grade`/`GradeLevel`), (3) `BaseEmployeeCsvUrl` บน PROD ชี้ QA host

## หลักการออกแบบ (ยึดตามนี้)

1. **ห้ามแก้ `ILearnerApiService` interface และ DTO ที่ controller/React ใช้อยู่** (`ExternalLearnerDto`, `AllLearnersApiResponse`, `EmployeeCsvDto`, `DivisionApiResponse`) — contract ฝั่งหน้าบ้านต้องเหมือนเดิม byte-for-byte เท่าที่ทำได้ React แตะศูนย์ไฟล์
2. **Directory cache กลาง:** implementation ใหม่ดึงพนักงาน active ทั้งหมดจาก `GET /api/employees` (วนหน้า `pageSize=200` จนครบ) เก็บ `IMemoryCache` TTL 30 นาที (key เดียว) แล้วให้ทุก method ที่เป็น list/filter/grid ทำงานจาก cache นี้ใน memory — ยกเว้น single lookup (`GetLearnerByCodeAsync`) ที่ยิงตรง `GET /api/employees/{empCode}` เพื่อความสด (เคส login)
3. **DevExtreme grid (gap 1):** `GetLearnersDxGridAsync(queryString)` ห้ามส่งต่อ upstream — ให้ parse `DataSourceLoadOptions` จาก query string แล้วใช้ `DataSourceLoader.Load()` (library เดิมที่ backend ใช้อยู่ทั่ว) ประมวลผลบน directory cache ใน memory → ได้ filter/sort/paging/searchValue semantics ครบโดย EmployeeHub ไม่ต้องรู้จัก DevExtreme; shape ผลลัพธ์ต้องเท่าของเดิม (rows camelCase `nid`/`eId` ฯลฯ — ดู typed DTO ที่ `LearnersController` ใช้ deserialize อยู่ และเทียบกับ `LearnerListPage.tsx` ก่อนเขียน)
4. **Position (gap 2):** map `Position = Grade` (เช่น `M1M`) ในทุกจุดที่ DTO เดิมมี Position — จอเดิมแสดงต่อได้ทันที; **ห้าม**เปลี่ยน label ฝั่ง UI ในแผนนี้ (ถ้าผู้ใช้อยากได้คำว่า Grade ค่อยเปิดแผน UI แยก)

## Scope

- [ ] **S1 — Settings:** ขยาย `EmployeeServiceSettings` ([iLearn.Application/Common/EmployeeServiceSettings.cs](iLearn.Application/Common/EmployeeServiceSettings.cs)):
  ```csharp
  public string Provider { get; set; } = "Legacy";   // "Legacy" | "EmployeeHub"
  public string EmployeeHubBaseUrl { get; set; } = string.Empty;  // base ไม่รวม /scalar; endpoints ต่อ /api/...
  ```
  - `appsettings.json` + `appsettings.Development.json`: เพิ่ม `"Provider": "Legacy"` และ `"EmployeeHubBaseUrl": "http://10.10.143.39/Tools/EmployeeHub/Service"` (QA/dev — ผู้ใช้ยืนยันแล้ว ดู PLAN-061)
  - **`appsettings.Production.json`: เพิ่ม section `EmployeeServiceSettings` ทั้งก้อน** — `Provider=Legacy`, `EmployeeHubBaseUrl=http://AP-NTC2137-PRWB/Tools/EmployeeHub/Service`, **URL เดิมฝั่ง PRWB (`BaseLearnerLookupUrl`/`BaseLearnerUrl`)** และ `BaseEmployeeCsvUrl` ชี้ host PROD ไม่ใช่ QA — ปิด gap 3: ปัจจุบัน PROD ตกไปใช้ค่าจาก `appsettings.json` ที่ชี้ `ap-ntc2138-qawb`
  - ⚠️ base เป็น `http://` (ไม่ใช่ https) และ **ไม่ลงท้าย `/scalar`** (นั่นคือหน้า explorer); เวลาต่อ URL ให้ได้ `{base}/api/employees` ฯลฯ
- [ ] **S2 — `EmployeeHubClient`** (ไฟล์ใหม่ `iLearn.Infrastructure/Services/EmployeeHubClient.cs`): typed HttpClient ครอบ endpoint ที่ต้องใช้ — `GET /api/employees` (paged, ต้องดึงทั้ง directory เพื่อ cache), `GET /api/employees/{empCode}`, `POST /api/employees/find-by-nids` (chunk ละ ≤200), `GET /health` — DTO ภายในลอกจาก skill API reference (`EmployeeDto`, `PagedResult<T>`, `FindByNidsResultDto`) พร้อมคอมเมนต์ `// Mirrors EmployeeHub <DtoName> (see .agents/skills/api-employeehub-api-reference)`; **ไม่ต้องเรียก `/api/lookups/*`** — distinct ทั้งหมดคำนวณจาก directory cache ตาม PLAN-061
- [ ] **S3 — `EmployeeHubLearnerApiService : ILearnerApiService`** (ไฟล์ใหม่) — mapping ต่อ method:
  | Method เดิม | ทำอย่างไร |
  |---|---|
  | `GetLearnerByCodeAsync(code)` | `GET /api/employees/{code}` ตรง; 404 → คืน null (พฤติกรรม not-found เดิม); map → `ExternalLearnerDto` (`Code=EmpCode`, `Name=FullNameEn`, `Position=Grade`) |
  | `GetLearnerAsync()` (/all) | จาก directory cache → ประกอบ `AllLearnersApiResponse` shape เดิม (`EId=EmpCode`, ชื่อแยก First/Last, `NID=Nid`, `Position=Grade`) |
  | `GetLearnersByCodesAsync` | filter cache ด้วย EId เหมือน logic เดิม |
  | `GetEmployeesByNidsAsync` | `POST find-by-nids` (chunk 200) → map เป็น `EmployeeCsvDto`; **เลิกใช้ CSV endpoint** ใน provider นี้ |
  | `GetLearnersByDivisionsAsync(divisions[])` | **ใช้กติกา [PLAN-061](PLAN-061-employeehub-division-semantics.md) ตาราง S3 ฉบับแก้** — `'NLC'`→`Company=="NLC"`, อื่น→`Division==ค่า`; ห้าม string-match `Division` ตรง ๆ กับ `'NLC'` |
  | `GetLearnersDxGridAsync` | หลักการข้อ 3 (DataSourceLoader บน cache) |
  | `GetDivisions/Departments/Sections/PositionsAsync` | **คำนวณ distinct จาก directory cache** (ตาม PLAN-061) — `GetDivisions` = `["NLC"] ∪ distinct(Division where Company!="NLC")`; อื่น ๆ = distinct field ที่ตรงกัน (positions ใช้ `Grade`) → จัด shape ให้ตรง `GetDistinct*` เดิมที่ React ใช้; **ห้ามใช้ `/api/lookups/*` ตรง ๆ** |
  - error handling ตามธรรมเนียมไฟล์เดิม: enrichment fail → คืน dictionary/list ว่าง + LogWarning (ห้ามโยนให้หน้า list พัง); lookup เดี่ยว fail → โยนตามเดิม
- [ ] **S4 — DI switch** ([iLearn.Infrastructure/DependencyInjection.cs](iLearn.Infrastructure/DependencyInjection.cs)): อ่าน `EmployeeServiceSettings.Provider` — `"EmployeeHub"` → register client + implementation ใหม่, อื่น ๆ → `LearnerApiService` เดิม; log ตอน startup ว่าใช้ provider ไหน
- [ ] **S5 — Health check:** `iLearn.API/Controllers/HealthController.cs` เพิ่ม check `employeeDirectory` ใน `/api/health/smoke` — ยิง `/health` ของ EmployeeHub เมื่อ Provider=EmployeeHub / ยิง base ของ StudentLookup เมื่อ Legacy (best-effort, timeout สั้น ~5s, fail = check fail แต่รายงานชัดว่า upstream ไหน)
- [ ] **S6 — Tests (`iLearn.Tests`):** unit tests สำหรับ `EmployeeHubLearnerApiService` ผ่าน mocked `HttpMessageHandler` ตาม convention ที่มีในโปรเจ็กต์ — อย่างน้อย: mapping Position=Grade, find-by-nids chunk >200, DxGrid loadOptions (filter+paging) บน cache, 404 → null, enrichment degrade เป็น empty

## Out of scope (ห้ามแตะ)

- ❌ React ทุกไฟล์ / `ILearnerApiService` interface / DTO เดิมทุกตัว (เพิ่มไฟล์ DTO ใหม่ภายใน Infrastructure ได้)
- ❌ ห้ามเปิด `Provider=EmployeeHub` เป็น default ที่ env ใด — cutover คือ [PLAN-060](PLAN-060-employeehub-cutover-qa-prod.md)
- ❌ การแก้ข้อมูล Division ใน DB — เป็นงาน [PLAN-059](PLAN-059-employeehub-division-mapping-audit.md)
- ❌ ฝั่ง EmployeeHub เอง (auth, เพิ่ม field Position) — นอก repo นี้

## Acceptance criteria

1. `Provider=Legacy` (default): พฤติกรรมทุก endpoint เหมือนก่อนแก้ 100% (regression ศูนย์)
2. `Provider=EmployeeHub` + ชี้ EmployeeHub dev: หน้า Learners grid (filter/sort/paging/search), learner login by EId, หน้า Admin Users (DisplayName enrichment), lookups Divisions/Sections/Departments/Positions ใช้งานได้โดย React ไม่แก้สักไฟล์
3. PROD config มี `EmployeeServiceSettings` ครบใน `appsettings.Production.json` และไม่มี URL ที่ชี้ QA host
4. tests ใหม่ผ่านทั้งหมด + suite เดิม 118 ตัวผ่าน

## Verification (รันก่อนปิดงาน)

```powershell
dotnet build iLearn.Tests -o artifacts\verify-test
dotnet test artifacts\verify-test\iLearn.Tests.dll
Remove-Item -Recurse -Force artifacts\verify-test
```
- รัน API local 2 รอบ (Provider=Legacy / EmployeeHub) แล้วเปิด admin-react dev ทดสอบหน้า Learners + Users + Assignment (bulk assign เลือก division) ตาม Acceptance ข้อ 1-2 — ถ้า EmployeeHub ยังไม่มี instance ให้ต่อ ให้จดใน Implementer Notes ว่าเทสได้แค่ Legacy + unit tests แล้วให้ reviewer ตามผลตอน PLAN-060

## Implementer Notes

(เติมหลังทำเสร็จ)
