# PLAN-009: แก้บั๊ก Search ในตาราง Learners ล้ม ("Failed to connect to the employee data source")

- **Status:** VERIFIED (casing fix โดย Gemini + hotfix ฟิลด์ NID โดย Claude — ดู note ท้ายไฟล์)
- **Assigned:** Gemini
- **Priority:** High
- **Estimated scope:** 1 ไฟล์ (`iLearn.API/Controllers/LearnersController.cs`) — backend ล้วน

## Problem

หน้า `/learners` โหลดตารางได้ปกติ แต่**พอพิมพ์ค้นหาในช่อง Search ตารางพัง** ขึ้น error:

```json
{"message":"Failed to connect to the employee data source."}
```

ตัวอย่าง request ที่ fail (search คำว่า "61"):
```
GET https://localhost:7128/api/Learners/Get?skip=0&take=19&requireTotalCount=true
&filter=[["nid","contains","61"],"or",["englishFirstName","contains","61"],"or",["englishLastName","contains","61"],"or",["eId","contains","61"]]
```

### Root cause (วินิจฉัยจากโค้ด — มั่นใจสูง)

**ชื่อฟิลด์ใน search filter เป็น camelCase แต่ external employee API (DevExtreme grid) ผูกกับ property แบบ PascalCase**

1. Frontend สร้าง filter ด้วยชื่อ camelCase จาก `searchExpr: ['nid', 'englishFirstName', 'englishLastName', 'eId']` (`iLearn.Admin.React/src/pages/moduleConfigs.ts` บรรทัด ~88)
2. `LearnersController.Get()` (`iLearn.API/Controllers/LearnersController.cs` บรรทัด ~149-180) **แค่ proxy ส่ง query string ดิบ** ต่อไปที่ `_learnerService.GetLearnersDxGridAsync(queryString)` → ยิงไป `{BaseLearnerUrl}{queryString}` (external: `https://AP-NTC2137-PRWB/.../api/Student`)
3. External DevExtreme endpoint ผูกกับ model PascalCase — ยืนยันจาก `LearnerGridRowDto` (บรรทัด ~302) ที่เป็น `EId, NID, EnglishFirstName, EnglishLastName` ทั้งหมด และจาก `InjectDivisionFilter` (บรรทัด ~276-293) ที่ฉีดฟิลด์ `"Division"` **PascalCase** ลง filter แล้วทำงานได้
4. เมื่อส่ง field `nid`/`englishFirstName`/`englishLastName`/`eId` (camelCase) ปลายทางหา property ไม่เจอ → throw → `GetLearnersDxGridAsync` catch กว้าง ๆ คืน `null` → controller คืนข้อความ `"Failed to connect to the employee data source."` (ข้อความนี้กำกวม จริง ๆ คือ filter ผิด ไม่ใช่ต่อไม่ติด)

**ทำไมโหลดปกติแต่ search พัง:** ตอนโหลดไม่มี field filter (SuperAdmin) หรือมีแค่ `Division` PascalCase ที่ถูก (division-admin); search เป็นจุดเดียวที่ส่ง camelCase fields

## Scope (ทำแค่นี้)

แก้ `iLearn.API/Controllers/LearnersController.cs` method `Get()` — เพิ่มขั้นตอน **แปลงชื่อฟิลด์ใน filter จาก camelCase → PascalCase ก่อน forward** ไปยัง external service (ทำก่อนหรือหลัง `InjectDivisionFilter` ก็ได้ ขอให้ผลลัพธ์ filter ที่ส่งออกเป็น PascalCase ทั้งหมด)

1. เพิ่ม private helper เช่น `MapFilterFieldNames(string queryString)` ที่:
   - ดึงค่า `filter=` จาก query string (pattern เดียวกับ `InjectDivisionFilter` — `Regex.Match(queryString, @"([?&])filter=([^&]*)")`, `Uri.UnescapeDataString`)
   - แทนที่ชื่อฟิลด์ที่ frontend ใช้ ตาม mapping ชัดเจน (เฉพาะ field ที่ search/ใช้จริง):
     | camelCase (frontend) | PascalCase (external) |
     |---|---|
     | `nid` | `NID` |
     | `eId` | `EId` |
     | `englishFirstName` | `EnglishFirstName` |
     | `englishLastName` | `EnglishLastName` |
     | `division` | `Division` |
     | `department` | `Department` |
     | `section` | `Section` |
     | `position` | `Position` |
   - แทนที่แบบ token ของชื่อฟิลด์ใน JSON filter เท่านั้น — **ระวังอย่าไปแทนค่าที่ผู้ใช้พิมพ์ค้นหา** (เช่น search คำว่า "nid") วิธีปลอดภัย: match รูปแบบ `"<field>"` ที่อยู่ในตำแหน่ง field ของ DevExtreme filter — แนวทางที่แนะนำคือ replace เฉพาะ `["<camel>",` และ `"<camel>"]`-context หรือใช้ regex `(?<=\[")<field>(?=","(contains|=|startswith|...))`. ขั้นต่ำที่ยอมรับได้: replace `"nid"` → `"NID"` ฯลฯ **เฉพาะเมื่อ token ตามด้วย `,"` (operator position)** เพื่อไม่ชนค่าค้นหา
   - re-escape แล้วประกอบกลับ query string เหมือน `InjectDivisionFilter`
2. เรียก helper ใน `Get()` กับ query string ก่อนส่งเข้า `GetLearnersDxGridAsync`
3. (แนะนำ ไม่บังคับ) ปรับ comment/ข้อความให้สื่อว่า error อาจมาจาก filter ไม่ใช่แค่การเชื่อมต่อ — แต่**ห้ามเปลี่ยน HTTP status/shape ของ response** (frontend อาจ handle อยู่)

## Out of scope (ห้ามแตะ)

- ห้ามแก้ frontend (`moduleConfigs.ts`, searchExpr, columns) — ให้คง camelCase ทั้งระบบ
- ห้ามแก้ `LearnerApiService.GetLearnersDxGridAsync` (catch กว้างเป็นอีกเรื่อง — ถ้าจะปรับ logging ค่อยทำแผนแยก)
- ห้ามแตะ external API / appsettings
- ห้ามเปลี่ยน `InjectDivisionFilter` logic เดิม (แค่ทำงานร่วมกับ field mapping ให้ผลออกมา PascalCase ครบ)

## Acceptance criteria

- [ ] search ในตาราง `/learners` ด้วยข้อความใด ๆ คืนผลลัพธ์ปกติ ไม่ขึ้น error
- [ ] filter ที่ส่งออก external มีชื่อฟิลด์ PascalCase ครบ (`NID`, `EId`, `EnglishFirstName`, `EnglishLastName`)
- [ ] กรณี division-admin (มี `InjectDivisionFilter`) search ก็ยังทำงาน + ยัง isolate division ถูกต้อง
- [ ] **ค่าค้นหาที่บังเอิญตรงกับชื่อฟิลด์ไม่ถูกแปลง** — เทส search คำว่า `nid` หรือ `position` แล้วผลลัพธ์ถูก (ค่าใน operand ไม่โดน replace)
- [ ] โหลดตารางปกติ (ไม่ search) ยังทำงานเหมือนเดิม

## Verification

```powershell
# Backend (ถ้า API รันใน VS อยู่ bin ถูกล็อก ให้ build ออก artifacts)
dotnet build iLearn.Tests -o artifacts\verify-test
dotnet test artifacts\verify-test\iLearn.Tests.dll
Remove-Item -Recurse -Force artifacts\verify-test
```
ทดสอบ manual (ต้องรัน API + เข้าถึง external employee service ได้): เปิด `/learners` → พิมพ์ค้นหาตัวเลข/ตัวอักษร → เห็นผลลัพธ์; ลองพิมพ์ `nid` ดูว่าไม่พัง; เทสด้วย account ที่มี division (ถ้ามี) ว่ายัง isolate ถูก

> หมายเหตุ implementer: ผมยืนยัน root cause จากโค้ดเท่านั้น (ยิง external HR API ตรง ๆ ถูกบล็อกด้วยเหตุผลความปลอดภัย/PII) — ขั้น manual test ให้ทำในเครื่อง dev ที่ต่อ intranet ได้ ถ้าพบว่า external จริง ๆ ต้องการ casing อื่น (เช่นบาง field ไม่ตรงตาราง mapping) ให้ปรับ mapping แล้วจดใน Implementer Notes

## Implementer Notes

- แก้ไขใน [LearnersController.cs](file:///c:/Users/n4734/source/repos/iLearn2/iLearn.API/Controllers/LearnersController.cs) โดยการเพิ่ม private method `MapFilterFieldNames` และ dictionary `FieldMapping` เพื่อแปลงฟิลด์ค้นหาจาก camelCase ไปเป็น PascalCase ก่อนที่จะ forward query ไปยัง external API
- ใช้ Regex match เฉพาะส่วนที่เป็นชื่อฟิลด์ (ตัวแรกใน token array ของ filter) เช่น `(?<=\[\s*")\b(nid|eId|...)\b(?="\s*,)` เพื่อความปลอดภัยและไม่แก้ไขค่าของข้อมูลที่ผู้ใช้พิมพ์ค้นหา (เช่น คำค้นหาว่า "nid" หรือ "position")
- เพิ่ม unit test ครอบคลุม 3 กรณีการทดสอบใน [LearnersControllerTests.cs](file:///c:/Users/n4734/source/repos/iLearn2/iLearn.Tests/LearnersControllerTests.cs): (1) การแปลง camelCase → PascalCase ทำงานถูกต้อง, (2) ปกป้อง search term ของ user ไม่ให้โดน replace, (3) ทำงานร่วมกับ Division isolation filter ได้อย่างสมบูรณ์
- ได้ทำการรัน `dotnet test` ทั้งหมด 115 เคส ผ่านทั้งหมด (100% Pass)

---

## [Claude/planner+hotfix 2026-06-12] Root cause ที่แท้จริง = ฟิลด์ `NID` (casing ไม่ใช่ตัวปัญหา) — STATUS: VERIFIED

ผู้ใช้รายงานว่าแก้ casing แล้ว search ยัง 500 อยู่ ผมทดสอบ **ผ่าน API ตัวเองที่ localhost:7128 ดูเฉพาะ HTTP status (ไม่อ่าน PII)**:

| filter | ผล |
|---|---|
| 4 ฟิลด์ (มี nid) | **500** |
| ไม่มี filter | 200 |
| eId + ชื่อ (ไม่มี nid) | **200** |
| nid อย่างเดียว | **500** |

**สรุป:** external employee grid (`/api/Student`) **filter ฟิลด์ `NID` ไม่ได้** (DevExtreme ปลายทาง throw → service คืน null → error กำกวม) — ตรงกับระบบเก่า (`iLearn.Admin/Views/Learners/Index.cshtml`) ที่ search EId/ชื่อ/Division เท่านั้น ไม่เคยมี NID ใน searchExpr/columns

หมายเหตุ: external **คืนค่า `nid` มาในผลลัพธ์** (ตรวจแล้วมี key) — คอลัมน์ NID จึงมีข้อมูล **เก็บคอลัมน์ไว้** แค่ filter ไม่ได้

งานของ Gemini (map camelCase→PascalCase) **ถูกต้องและจำเป็น** — ฟิลด์ `englishFirstName`/`eId` ต้องเป็น PascalCase ปลายทางถึงรับ (test "eId + ชื่อ" ผ่านได้เพราะ mapping นี้) → **คงไว้**

**Hotfix ที่ผมทำเอง (ผู้ใช้อนุญาต):** เอา `'nid'` ออกจาก `searchExpr` ของ learners ใน `iLearn.Admin.React/src/pages/moduleConfigs.ts` (คงคอลัมน์ NID ไว้) — ทดสอบ filter จริง 3 ฟิลด์ผ่าน API → **HTTP 200**, `npm run build` ผ่าน
