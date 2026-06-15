# PLAN-014: LearnerApiService เลิกกลืน exception + ใช้ ILogger + แยกชนิด error

- **Status:** VERIFIED ✅ (Claude review 2026-06-15: ILogger inject + Console ลบหมด, group A propagate (GetAsync แยก 4xx→ArgumentException/5xx→HttpRequestException), group B degrade graceful+LogWarning, middleware HttpRequestException→502, controller ลบ null-check, test ใหม่ 502 ProblemDetails ผ่าน, build/test 116 ผ่าน)
- **Assigned:** Gemini
- **Priority:** Medium
- **Estimated scope:** 1-2 ไฟล์ (`LearnerApiService.cs` + อาจปรับ `LearnersController.Get` ให้ส่ง status ที่สื่อความ)

## Problem

`iLearn.Infrastructure/Services/LearnerApiService.cs` ทุก method ใช้ `try { ... } catch (Exception) { Console.WriteLine(...); return null; }` ปัญหา:

1. **กลืน exception** → bypass `GlobalExceptionMiddleware` (ที่ทำ ProblemDetails + log แบบ structured + sanitize log-forging อยู่แล้ว)
2. **`Console.WriteLine` แทน `ILogger`** → log ไม่เข้าระบบ logging, ไม่มี severity/scope, หาย
3. **error กำกวม** — `GetLearnersDxGridAsync` ใช้ `GetStringAsync` ที่ throw `HttpRequestException` ทั้งกรณี "ต่อ external ไม่ติด" และ "external คืน 4xx/5xx" (เช่น filter ผิด) แล้ว controller แปลงเป็นข้อความเดียว "Failed to connect to the employee data source." → แยกไม่ออกว่าเชื่อมต่อพัง หรือ request ผิด (เป็นต้นเหตุที่ debug บั๊ก search ยาก — PLAN-009)

## หลักการแก้ (สำคัญ: แยก 2 กลุ่ม method)

แยกพฤติกรรมตามบทบาทของ method — **อย่าทำเหมือนกันหมด**:

**กลุ่ม A — primary fetch (ความล้มเหลวต้อง surface เป็น error จริง):**
`GetLearnersDxGridAsync`, `GetLearnerByCodeAsync`, `GetLearnersByDivisionsAsync`, `GetDivisionsAsync`, `GetDepartmentsAsync`, `GetSectionsAsync`, `GetPositionsAsync`, `GetLearnerAsync`
→ ข้อมูลหลักที่ผู้ใช้ร้องขอตรง ๆ ถ้าพังต้องบอกผู้ใช้

**กลุ่ม B — enrichment helper (ความล้มเหลวควร degrade graceful):**
`GetLearnersByCodesAsync`, `GetEmployeesByNidsAsync`
→ ใช้เติมชื่อ/แผนกให้ row ที่มีอยู่แล้ว (เช่น UsersCRUD.Get, profile) ถ้า external ล่ม **ยังควรแสดงข้อมูลหลักได้** (แค่ไม่มีชื่อ enrich) — คง fallback คืน dictionary ว่างไว้ แต่ **เปลี่ยนเป็น log ผ่าน `ILogger.LogWarning` (ไม่ใช่ Console)**

## Scope (ทำแค่นี้)

1. **Inject `ILogger<LearnerApiService>`** ผ่าน constructor — แทน `Console.WriteLine` ทุกจุดด้วย `_logger.LogWarning/LogError` (มี exception + context เช่น URL/queryString แบบ sanitize)

2. **กลุ่ม A:** เอา `catch ... return null` ออก ให้ exception propagate (หรือ throw typed exception) — ปล่อย `GlobalExceptionMiddleware` แปลงเป็น ProblemDetails
   - แนะนำเพิ่ม mapping ใน `GlobalExceptionMiddleware.MapException` (`iLearn.API/Middleware/GlobalExceptionMiddleware.cs:87`): `HttpRequestException => (502 Bad Gateway, "Upstream employee service error.")` เพื่อแยกจาก 500 ทั่วไป
   - `LearnersController.Get` (`iLearn.API/Controllers/LearnersController.cs:149`): เลิกเช็ค `resultJson == null` แล้วคืนข้อความ generic — ให้ service โยน exception แทน (ถ้าอยากแยก "external 4xx เพราะ filter" ออกจาก "ต่อไม่ติด" ให้เปลี่ยน `GetLearnersDxGridAsync` จาก `GetStringAsync` เป็น `GetAsync` + อ่าน `response.StatusCode`: non-success ที่เป็น 4xx → โยน `ArgumentException`/`InvalidOperationException` พร้อม body, ต่อไม่ติด/5xx → `HttpRequestException`)

3. **กลุ่ม B:** คงพฤติกรรม fallback (คืน dictionary ว่างเมื่อพัง) แต่เปลี่ยน `Console.WriteLine` → `_logger.LogWarning(ex, ...)` และคอมเมนต์กำกับว่า "graceful degradation โดยตั้งใจ — enrichment ล้มไม่ควรทำทั้งหน้าพัง"

4. แก้คอมเมนต์ภาษาไทยที่เป็น `???` (mojibake) ใน method `GetLearnersByCodesAsync`/`GetSectionsAsync` ฯลฯ ให้อ่านออก (optional, ถ้าสะดวก)

## Out of scope (ห้ามแตะ)

- ห้ามแก้ frontend (search ฝั่ง React จัดการแล้วใน PLAN-009/011/012)
- ห้ามแก้ external API / appsettings / `EmployeeServiceSettings`
- ห้ามเปลี่ยน shape ของ response ที่ controller คืน (เฉพาะกรณี error ที่เปลี่ยนเป็น ProblemDetails)
- ห้ามแตะ HMAC learner proxy / endpoint ฝั่งผู้เรียน

## Acceptance criteria

- [ ] ไม่มี `Console.WriteLine` เหลือใน `LearnerApiService.cs` (ใช้ `ILogger` แทนทั้งหมด)
- [ ] กลุ่ม A: external ล่ม/คืน error → client ได้ ProblemDetails (มี status สื่อความ เช่น 502) ไม่ใช่ข้อความกลืน ๆ
- [ ] กลุ่ม B: external ล่ม → หน้า Users/profile ยังโหลด row หลักได้ (ชื่อ enrich หาย แต่ไม่ 500 ทั้งหน้า) + มี log warning
- [ ] search Learners ที่แก้ไปแล้ว (PLAN-009) ยังทำงานปกติ (HTTP 200)
- [ ] `dotnet build` + `dotnet test` ผ่านครบ (115/115); ถ้าเพิ่ม mapping ใน middleware ให้เพิ่ม/อัปเดต test ถ้ามี coverage ส่วนนั้น

## Verification

```powershell
dotnet build iLearn.Tests -o artifacts/verify-test
dotnet test artifacts/verify-test/iLearn.Tests.dll
Remove-Item -Recurse -Force artifacts/verify-test
```
ทดสอบ manual (ถ้ารัน API + ต่อ intranet ได้): `/learners` ค้นหาปกติ (200); จำลอง external ล่ม (เช่นปิด VPN/แก้ base URL ชั่วคราวในเครื่อง dev) → ดูว่าหน้า Users ยังโหลด row ได้ และ grid learners คืน error ที่สื่อความ

## Implementer Notes

- เพิ่มการ Inject `ILogger<LearnerApiService>` ใน constructor ของ [LearnerApiService.cs](file:///c:/Users/n4734/source/repos/iLearn2/iLearn.Infrastructure/Services/LearnerApiService.cs) และลบ `Console.WriteLine` ออกไปทั้งหมดโดยเปลี่ยนมาใช้ `_logger` แทน
- ปรับปรุงการจัดการ Exception แยกตามกลุ่ม:
  - **กลุ่ม A (หลัก):** นำ `try-catch` คืน `null` ออกเพื่อให้ Exception เด้งขึ้นไปยัง Middleware โดยตรง; สำหรับ `GetLearnersDxGridAsync` เปลี่ยนมาใช้ `_httpClient.GetAsync` แล้วประเมิน StatusCode: ถ้า 4xx (เช่น user search ผิดฟิลด์) จะโยน `ArgumentException` พร้อม response body, ถ้า 5xx/connection failure จะโยน `HttpRequestException`
  - **กลุ่ม B (Enrichment):** คง fallback คืน empty dictionary ไว้ แต่เปลี่ยน logging จาก Console เป็น `_logger.LogWarning` เพื่อรักษาระดับการทำงานแบบ Graceful Degradation ไม่ให้หน้าหลักพัง
- ลบ null check `resultJson == null` ออกจากหน้า `Get` ใน [LearnersController.cs](file:///c:/Users/n4734/source/repos/iLearn2/iLearn.API/Controllers/LearnersController.cs) เพื่อปล่อยให้ exception ตกไปยัง global middleware
- เพิ่มการแมป `System.Net.Http.HttpRequestException` เป็น `502 Bad Gateway` (Upstream employee service error) ใน `MapException` ของ [GlobalExceptionMiddleware.cs](file:///c:/Users/n4734/source/repos/iLearn2/iLearn.API/Middleware/GlobalExceptionMiddleware.cs) เพื่อแยก error จาก upstream service ออกจาก error ฝั่ง backend ตัวเอง
- เขียน Unit Test ใหม่ใน [GlobalExceptionMiddlewareTests.cs](file:///c:/Users/n4734/source/repos/iLearn2/iLearn.Tests/GlobalExceptionMiddlewareTests.cs) เพื่อตรวจสอบว่า `HttpRequestException` ถูก map เป็น 502 ProblemDetails อย่างถูกต้อง และรัน `dotnet test` ทั้งหมด 116 เคส ผ่านทั้งหมด (100% Pass)
- แก้ไข mojibake (ภาษาไทยที่อ่านไม่ออก) ใน comment ต่างๆ ของ `LearnerApiService.cs` ให้อ่านรู้เรื่องเรียบร้อย
