# PLAN-058: EmployeeHub Provider — client + translation layer หลัง `ILearnerApiService` (feature-flag, ยังไม่เปิดใช้)

- **Status:** VERIFIED — implement รอบแรก + FIX-1 (รอบ 2) เสร็จ, reviewer ตรวจซ้ำ + รัน `dotnet test` 128/128 ผ่าน (ดู "Reviewer Findings รอบ 2" ท้ายไฟล์); default ยัง Legacy ทุก env → cutover จริงคือ PLAN-060
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

- ขยาย `EmployeeServiceSettings` และตั้งค่า config สลับ Provider (Legacy/EmployeeHub) ใน `appsettings*.json` ครบทุก env
- สำหรับ Production แก้ไขค่า `BaseEmployeeCsvUrl` และ API URLs ให้ชี้เซิร์ฟเวอร์ PROD (`AP-NTC2137-PRWB`) ทั้งหมดเพื่อปิด gap 3
- สร้าง `EmployeeHubClient` สำหรับยิงหา EmployeeHub (GET paged active, GET code, POST find-by-nids, GET health)
- สร้าง `EmployeeHubLearnerApiService` เพื่อรับข้อมูลจาก `EmployeeHubClient` แล้ว translate/map ข้อมูลให้เป็น shape เดิมของ `ILearnerApiService`
  - ทำ Directory Cache เก็บข้อมูลพนักงาน active ทั้งหมดใน memory (IMemoryCache TTL 30 นาที)
  - นำ `DataSourceLoader.Load` มารันบน cache ใน memory เพื่อประมวลผล filter/sort/paging/search สำหรับ DevExtreme Grid
  - ย้ายการประกาศ `LearnersGridResponse` และ `LearnerGridRowDto` ไปที่ `ExternalLearnerDto.cs` ของ Application layer เพื่อแก้ปัญหา cross-assembly reference ระหว่าง Infrastructure และ API
  - จัดการ mapping `Position = Grade` และ rules แปลง Division ตามที่ระบุใน PLAN-061
  - ย้ายการคำนวณ distinct divisions/departments/sections/positions ไปทำบน directory cache ทั้งหมดและครอบด้วย `DataSourceLoader`
- เพิ่ม smoke test check สำหรับ `employeeDirectory` ตรวจจับสถานะของ Provider ที่ทำงานอยู่ (EmployeeHub/Legacy)
- เพิ่ม Unit Tests ใน `EmployeeHubLearnerApiServiceTests.cs` ตรวจสอบความถูกต้องของการดึง/เซต cache, mapping properties, batch chunking, division logic, และ DevExtreme query parsing โดย mock HttpMessageHandler
- ทดสอบ build และ test suite ทั้งหมด (126 tests) ผ่านฉลุย 100%

## Reviewer Findings (รอบ 1 — Claude Code, 2026-07-09)

รีวิวเทียบ interface/DTO/controller เดิม + consumer ฝั่ง React จริง สรุป: โครงถูกเกือบทั้งหมด **regression ศูนย์ฝั่ง Legacy (Acceptance #1 ผ่าน)** แต่มี **blocking 1 จุดที่ทำให้ Acceptance #2 ยังไม่ผ่าน** — ต้องแก้ก่อนปิดงาน

### 🔴 FIX-1 (blocking) — cascade Department/Section พังเมื่อ Provider=EmployeeHub

**อาการ:** `GetDepartmentsAsync` / `GetSectionsAsync` ([EmployeeHubLearnerApiService.cs:238–266](../../iLearn.Infrastructure/Services/EmployeeHubLearnerApiService.cs)) คำนวณ distinct → project เป็น `LookupNameDto { Name }` **ก่อน** แล้วค่อยเอา `DataSourceLoader.Load(items, loadOptions)` มากรอง แต่ `loadOptions` มี filter ที่อ้าง field `Division`/`Department` ซึ่ง `LookupNameDto` ไม่มี

**ทำไมพัง:** consumer ทั้งสองส่ง filter บน field ที่ไม่มีใน `LookupNameDto`:
- `LearnerListPage.tsx:38` → `GetDepartments?filter=["Division","=",div]`
- `LearnerDirectorySelector.tsx:97,122` → `GetSections?filter=[["Division","=",..],"and",["Department","=",..]]`

DataSourceLoader สร้าง filter expression บน property ที่ไม่มี → **โยน exception → HTTP 500** (สอง method นี้ไม่มี try/catch, controller เช็คแค่ null); React `.catch` กลืน error → **Department/Section dropdown ว่างหรือ toast error** ทั้งหน้า Learners และ Bulk Assign directory selector และต่อให้ไม่ error, distinct ก็คำนวณจาก**ทั้ง directory** ไม่ได้ scope ตาม division/department ที่เลือก (ของเดิม upstream `GetDistinct*?filter=...` scope ให้) → cascade เพี้ยน

**วิธีแก้ (ทำแบบนี้):** กรอง filter บน **employee cache** (ซึ่งมี `Division`/`Department`/`Section` ครบ) **ก่อน** แล้วค่อย distinct field เป้าหมาย → คืน bare `List<LookupNameDto>` เหมือน consumer คาด (`Array.isArray(res)` + อ่าน `.name`):
```csharp
var emps = await GetActiveEmployeesCachedAsync();
var lo = ParseLoadOptions(queryString);
// กรอง employees ด้วย filter เดิม (DataSourceLoader จัดการ filter ซ้อน "and" ให้เอง;
// field "Division"/"Department" ที่ React ส่งมาเป็น PascalCase ตรงกับ property ของ EmployeeDto)
var filteredObj = DataSourceLoader.Load(emps, new DataSourceLoadOptions { Filter = lo.Filter });
var filtered = ((IEnumerable<EmployeeDto>)filteredObj) // filter-only → คืน bare enumerable
    ?? Enumerable.Empty<EmployeeDto>();
var departments = filtered
    .Where(e => !string.IsNullOrWhiteSpace(e.Department))
    .Select(e => e.Department!)
    .Distinct(StringComparer.OrdinalIgnoreCase)
    .OrderBy(d => d)
    .Select(d => new LookupNameDto { Name = d })
    .ToList();
return departments; // bare array — ไม่ต้อง DataSourceLoader.Load ซ้ำ
```
- ทำเหมือนกันกับ `GetSectionsAsync` (target field = Section)
- `GetDivisionsAsync` / `GetPositionsAsync` **ไม่ต้องแก้** — consumer ไม่ส่ง filter มา (แต่ถ้าจะกันเหนียว ให้ pattern เดียวกันได้)
- **เพิ่ม test** (ปิด coverage gap): `GetDepartments` + filter `["Division","=","CSD"]` → คืนเฉพาะ department ของคน division CSD; `GetSections` + filter ซ้อน Division+Department → scope ถูก

### ✅ จุดที่ตรวจแล้วถูก (อย่าแก้)
- **DxGrid** ([:172](../../iLearn.Infrastructure/Services/EmployeeHubLearnerApiService.cs)) ถูก — map เป็น `LearnerGridRowDto` ที่มี field ครบ, controller `Get()` deserialize→re-serialize normalize ทั้ง old/new เหมือนกัน, `MapFilterFieldNames`/`InjectDivisionFilter` ตรง property (นี่คือเหตุผลที่ grid ไม่พังแต่ lookup พัง — row DTO มี field ที่ filter อ้าง แต่ `LookupNameDto` ไม่มี)
- camelCase `nid`/`eId` ถูก (CamelCase policy แปลง `NID`→`nid`, `EId`→`eId`)
- Division semantics ตรง PLAN-061 เป๊ะ (NLC=Company, อื่น=Division case-insensitive, union)
- lookup shape (bare array + `.name`), perf enrichment (claims cache 10 นาที → find-by-nids เรียกเฉพาะ cache-miss ไม่ regression), gap 3 (Production.json ครบ), DI switch + health check + default Legacy — ผ่านหมด

### Implementer Notes (รอบ 2 — แก้ไขจุดบกพร่องตาม Reviewer Findings)
- ปรับปรุงการทำงานของ lookup methods ทั้ง 4 ตัว (`GetDivisionsAsync`, `GetSectionsAsync`, `GetDepartmentsAsync`, `GetPositionsAsync`) ให้ทำงานบนโครงสร้างเดียวกัน:
  - ใช้ `DataSourceLoader.Load` กรองข้อมูลบน employee directory cache ด้วย filter ที่ส่งมาจาก React ก่อนทำการ distinct เพื่อให้ cascade scope ทำงานได้ถูกต้องไม่โยน exception (HTTP 500)
  - คืนผลลัพธ์เป็น bare array/list ของ `LookupNameDto` โดยไม่ครอบด้วย `LoadResult` ซ้ำ เพื่อให้ตรงตามที่ React คาดหวังในการทำ `Array.isArray(res)`
  - ปรับปรุงตัวถอดรหัส query parameter ใน `ParseLoadOptions` ให้แปลงเครื่องหมายบวก (`+`) เป็นเว้นวรรค (space) อย่างถูกต้อง
- เพิ่ม unit tests 2 ชุดใหม่ใน `EmployeeHubLearnerApiServiceTests.cs` เพื่อทดสอบ:
  - `GetDepartmentsAsync` เมื่อส่ง filter `["Division", "=", "CSD"]` จะต้องคืนค่าเฉพาะแผนกที่อยู่ใน Division นั้น
  - `GetSectionsAsync` เมื่อส่ง nested filter (Division + Department) จะต้องกรองได้ผลลัพธ์ที่ถูกต้อง
- ผลการทดสอบ unit tests ทั้งหมด 128 ตัวผ่านสำเร็จ 100% และโปรเจกต์ React build ทำงานได้สมบูรณ์เรียบร้อยครับ

## Reviewer Findings (รอบ 2 — Claude Code, 2026-07-09) — ✅ FIX-1 RESOLVED, VERIFIED

ตรวจการแก้ FIX-1 แล้ว — **ผ่าน ปิดงานได้**:
- `GetDepartmentsAsync`/`GetSectionsAsync` (+ `GetDivisionsAsync`/`GetPositionsAsync` ทำ pattern เดียวกัน) กรอง filter บน employee cache ก่อน distinct → cascade scope ถูก, ไม่โยน 500 อีก; คืน bare `List<LookupNameDto>` → ตรง `Array.isArray(res)` + `.name` ของ React
- `ParseLoadOptions` เพิ่ม `.Replace('+',' ')` **ก่อน** `UnescapeDataString` — ปลอดภัย: React ใช้ `encodeURIComponent` (space→`%20`, `+`→`%2B`) จึงไม่มี bare `+` ใน production; ค่าที่มี `+` จริงมาเป็น `%2B` ซึ่ง Replace ไม่แตะ แล้ว unescape กลับเป็น `+` ถูกต้อง — ไม่ทำ grid path เพี้ยน
- test 2 ตัวใหม่ assert ผลลัพธ์ **scoped + non-empty** (ไม่ใช่แค่ "ไม่ throw") → พิสูจน์ว่า `filteredObj.data as IEnumerable<EmployeeDto>` cast ทำงานจริง runtime (คืน 2/1 รายการตามคาด)
- **reviewer รัน verification เอง** (ไม่เชื่อ report อย่างเดียว): `dotnet build iLearn.Tests` = 0 errors; `dotnet test` = **128/128 passed**
- Acceptance #1 (Legacy regression ศูนย์) + #2 (lookups/grid/enrichment ทำงานโดย React ไม่แก้ไฟล์) + #3 (Production.json ครบ) + #4 (tests ผ่าน) — ครบ

หมายเหตุ soak: unit + Legacy path ครบแล้ว; การทดสอบ Provider=EmployeeHub กับ instance จริง (login by EId, bulk assign เลือก division, admin users DisplayName) จะทำตอน **PLAN-060 Phase 1 (QA)** ตามที่ verification ของแผนระบุ

