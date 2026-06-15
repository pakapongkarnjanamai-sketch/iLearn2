# PLAN-018: แทน anonymous-object response ด้วย DTO record (pilot: Dashboard)

- **Status:** VERIFIED ✅ (Claude review 2026-06-15: DashboardResponseDtos + DashboardController คืน DTO record แทน anonymous, dashboardApi.ts diff = comment-only (shape ไม่ drift), build/lint/test 118 ผ่าน)
- **Assigned:** GPT
- **Priority:** Low
- **Estimated scope:** pilot 1 controller (`DashboardController` responses → DTO records) — controller อื่นเป็นแผนต่อ ๆ ไป

## Problem

controller หลายตัวคืน `Ok(new {...})` (anonymous object) → OpenAPI generate type ไม่ได้ ฝั่ง React ต้องลอก shape ด้วยมือ (กติกา "Mirrors <Dto>" ใน CLAUDE.md ช่วยอยู่ แต่พึ่งวินัยคน เสี่ยง drift เวลา backend เปลี่ยน field)

แก้แบบ **ทยอย** ทีละ controller โดยเริ่มจาก endpoint ที่ React ใช้บ่อยและเป็น read-only (เสี่ยงต่ำ) — แผนนี้เป็น pilot + วางมาตรฐาน DTO record ให้ทำตามใน controller อื่นภายหลัง

## Scope (ทำแค่นี้ — pilot)

**`DashboardController` (766 บรรทัด, read-only aggregation, React dashboard บริโภค)**
1. สร้าง DTO record ใน `iLearn.Application/DTOs/` (เช่น `DashboardKpiDto`, `DashboardChartDto` ฯลฯ ตาม response จริงของแต่ละ action) — ชื่อ field **ตรงกับ shape ปัจจุบันเป๊ะ** (camelCase หลัง serialize)
2. เปลี่ยน action จาก `Ok(new {...})` → คืน DTO record ที่ map ค่าเดิม
3. **ห้ามเปลี่ยนชื่อ/โครงสร้าง field** ที่ React ใช้อยู่ — ไป grep endpoint ใน `iLearn.Admin.React/src` ก่อน เพื่อยืนยันว่า shape ใหม่ตรงกับที่ frontend อ่าน (ตามกติกา API Contract Sync) ถ้า React มี type mirror อยู่แล้ว ให้เทียบให้ตรง
4. ใส่คอมเมนต์ `// Mirrors <DtoName>` ฝั่ง React type ที่เกี่ยวข้อง (ถ้ามี) ให้ชี้มา DTO ใหม่

## Out of scope (ห้ามแตะ)

- ห้ามเปลี่ยน shape/ชื่อ field ของ response (ต้อง byte-compatible กับของเดิมที่ React อ่าน)
- ห้าม convert controller อื่น (เป็นแผนแยกต่อ ๆ ไป)
- ห้ามแตะ logic การคำนวณ/aggregation ของ dashboard (แค่ห่อผลด้วย type)
- ห้ามแตะ SignalR live activity flow

## Acceptance criteria

- [x] response ของ `DashboardController` คืน DTO record แทน anonymous object
- [x] shape ที่ React ได้รับ **ไม่เปลี่ยน** (หน้า Dashboard แสดงผลเหมือนเดิมทุกการ์ด/กราฟ)
- [x] DTO อยู่ใน `iLearn.Application/DTOs/` + React type มีคอมเมนต์ Mirrors ชี้มา
- [x] `dotnet test` ผ่าน + `npm run build`/`lint` ผ่าน

## Verification

```powershell
dotnet build iLearn.Tests -o artifacts/verify-test
dotnet test artifacts/verify-test/iLearn.Tests.dll
Remove-Item -Recurse -Force artifacts/verify-test
npm run lint; npm run build
```
ทดสอบ manual (ถ้ารัน API ได้): เปิด Dashboard → KPI/กราฟ/live activity แสดงครบเหมือนเดิม

## Implementer Notes

- เพิ่มไฟล์ DTO ใหม่ `iLearn.Application/DTOs/DashboardResponseDtos.cs` สำหรับ pilot ของ Dashboard โดยครอบคลุม response shape ที่ใช้จริงใน React:
	- wrapper `DashboardApiResponseDto<T>`
	- overview graph (`DashboardOverviewDto`, `DashboardScopeDto`, `DashboardKpiDto`, `DashboardTaskStatusPointDto`, `DashboardLearningActivityPointDto`, `DashboardCategoryMixPointDto`, `DashboardPriorityAssignmentDto`, `DashboardCourseAttentionDto`)
	- endpoint อื่นใน controller (`DashboardStatsDto`, `DashboardEnrollmentTrendPointDto`, `DashboardMaintenanceStatusDto`, `DashboardMaintenanceOperationDto`)
- ปรับ `iLearn.API/Controllers/DashboardController.cs` ให้เลิก `Ok(new { ... })` และเปลี่ยนเป็น DTO records ทั้ง endpoint ใน scope (`Overview`, `Stats`, `EnrollmentTrends`, `LearningActivityTrends`, `MaintenanceStatus`, `RecentAdminActivities`)
- ปรับ helper ภายใน controller ให้คืน typed collection แทน `IEnumerable<object>` (`BuildLearningActivityTrendAsync`, `BuildCourseAttentionAsync`) และเปลี่ยน `BuildPriorityAssignments` ให้คืน `List<DashboardPriorityAssignmentDto>`
- อัปเดตฝั่ง React `iLearn.Admin.React/src/pages/dashboard/dashboardApi.ts` โดยเพิ่มคอมเมนต์ `// Mirrors <DtoName>` ให้ type ที่แมปกับ backend DTO ใหม่ (ไม่เปลี่ยนชื่อ field หรือโครงสร้าง type ฝั่ง frontend)
- Verification ที่รันแล้ว:
	- `dotnet build iLearn.Tests -o artifacts\verify-test` ผ่าน (มี warning เดิม)
	- `dotnet test artifacts\verify-test\iLearn.Tests.dll` ผ่าน (118/118)
	- `npm run lint` ผ่าน (0 errors, 11 warnings baseline)
	- `npm run build` ผ่าน
	- ลบโฟลเดอร์ชั่วคราว `artifacts\verify-test` แล้ว
- **[Claude/hotfix 2026-06-15] Regression ที่ตามมา:** การ project เข้า `DashboardCategoryMixPointDto` (record constructor) **ในตัว EF GroupBy query** ของ `GetOverview` ทำให้ EF/SQL Server แปลงไม่ได้ → runtime 500 (`dotnet test` จับไม่ได้เพราะ in-memory provider แปลง record ctor ได้) แก้โดย project เป็น anonymous ใน SQL แล้ว map เป็น DTO ใน memory หลัง `ToListAsync` — **บทเรียนสำหรับงาน anonymous→DTO ครั้งต่อไป: อย่าใส่ DTO record constructor ในส่วนที่ยังเป็น IQueryable (โดยเฉพาะ GroupBy) ให้ materialize ก่อนเสมอ**
