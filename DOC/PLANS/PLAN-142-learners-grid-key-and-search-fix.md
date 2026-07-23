# PLAN-142 — Learners grid: fix broken infinite scroll (key=id=0) + ค้นหา NID/ชื่อไทย

- **สถานะ:** DONE
- **Assigned:** Claude Code (ผู้ใช้สั่งให้ Claude แก้ตรงในเซสชันเดียวกับที่วินิจฉัย — ข้ามขั้น implementer)
- **วันที่:** 2026-07-23
- **หน้าที่กระทบ:** `/admin-react/learners` (PROD: `https://ap-ntc2137-prwb.nikonoa.net/iLearn/admin-react/learners`)

## อาการที่ผู้ใช้รายงาน (PROD)

1. ตาราง Learner แสดงข้อมูลไม่ปกติ
2. Scroll ลงล่างแล้วไม่โหลดหน้าถัดไป
3. ค้นหาข้อมูลได้ไม่ครบ

## Root cause (ยืนยันจากโค้ดแล้วทั้งหมด)

### อาการ 1+2 — root เดียวกัน: `Id = 0` ทุกแถว + `key: 'id'`

- PROD ใช้ Provider `EmployeeHub` (`appsettings.Production.json`) ซึ่ง map ทุกแถวเป็น `Id = 0` ตายตัว
  (`EmployeeHubLearnerApiService.GetLearnersDxGridAsync`) — Legacy provider เดิมมี Id จริง อาการนี้จึงโผล่หลัง cutover PLAN-058
- แต่ `moduleConfigs.learners` ตั้ง `key: 'id'` ⇒ ใน `AppTable`:
  - dedupe ตอน page>1 (`existingKeys.has(x[store.key])`) เห็นทุกแถว key = `0` ⇒ แถวหน้าใหม่ถูกกรองทิ้งทั้งหมด
    ⇒ infinite scroll ไม่ไปไหน + effect auto-fill ยิง request วนเปล่า
  - React row key ซ้ำ (`"0"` ทุกแถว) ⇒ render เพี้ยน

### อาการ 3 — สองสาเหตุซ้อน

1. ผลค้นหาที่เกิน 1 หน้า โหลดต่อไม่ได้ (บั๊กเดียวกับข้อบน)
2. `searchExpr` มีแค่ `englishFirstName/englishLastName/eId` — คอมเมนต์เดิมอ้างว่า NID filter ทำ external endpoint 500
   ซึ่งเป็นข้อจำกัดของ **Legacy provider เท่านั้น**; EmployeeHub รัน `DataSourceLoader` บน in-memory cache ⇒ filter NID/ชื่อไทยได้ปกติ

## สิ่งที่แก้

**Backend**
- `iLearn.Application/DTOs/ExternalLearnerDto.cs` — เพิ่ม `ThaiFirstName`/`ThaiLastName` ใน `LearnerGridRowDto`
- `iLearn.Infrastructure/Services/EmployeeHubLearnerApiService.cs` — map สอง field ใหม่ (ผ่าน `NameHelper.StripGenderPrefix` ฝั่ง first name)
- `iLearn.API/Controllers/LearnersController.cs` — เพิ่ม `thaiFirstName`/`thaiLastName` ใน `FieldMapping` + regex ของ `MapFilterFieldNames`

**Frontend (`iLearn.Admin.React`)**
- `src/pages/moduleConfigs.ts` (learners) —
  - `key: 'id'` → **`key: 'eId'`** (รหัสพนักงาน unique) — แก้ infinite scroll + duplicate React keys
  - `searchExpr` → `['thaiFirstName','thaiLastName','englishFirstName','englishLastName','eId','nid']`
  - เพิ่มคอลัมน์ `thaiFirstName`/`thaiLastName` (ก่อนคอลัมน์ชื่ออังกฤษ)
- `src/lib/labels.ts` — คีย์ใหม่ `thaiFirstName`/`thaiLastName` + ปรับ `searchNameOrEmployeeId` ให้บอกว่าค้น NID/ชื่อไทยได้

**Tests (เพิ่ม 2 cases — รวมเป็น 274)**
- `LearnersControllerTests.MapFilterFieldNames_ThaiNameFields_MapsToPascalCase`
- `EmployeeHubLearnerApiServiceTests.GetLearnersDxGridAsync_ExposesThaiNamesAndFiltersOnThem`

## Verification

- `npm run lint` ✓ · `npm run build` ✓
- `dotnet build iLearn.Tests -o artifacts\verify-test` ✓ · `dotnet test` → **274/274 passed** ✓

## ข้อจำกัด / หมายเหตุ

- **Legacy provider (dev default):** legacy external grid ไม่มี field ชื่อไทย ⇒ คอลัมน์ใหม่จะว่าง และ filter `NID`/`ThaiFirstName` บน legacy proxy อาจ 500 (ข้อจำกัดเดิม) — กระทบเฉพาะ dev ที่รัน `Provider: "Legacy"`; QA/PROD เป็น EmployeeHub ทั้งคู่
- ยังไม่ได้ smoke บน browser จริง — บั๊ก scroll reproduce ได้เฉพาะ data จาก EmployeeHub (dev local เป็น Legacy ที่มี Id จริง) ⇒ **ต้อง deploy (full API publish + admin-react) แล้ว smoke บน QA→PROD**: เปิด `/admin-react/learners`, scroll จนแถวเพิ่มเกิน page แรก (footer X of Y ขยับ), ค้นหาชื่อไทย/NID, ตรวจ console 0 errors
- ไม่มี migration ใหม่
