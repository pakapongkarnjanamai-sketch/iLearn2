# PLAN-143 — Assignment Summary Report

- **สถานะ:** DONE
- **Assigned:** GitHub Copilot (GPT)
- **วันที่:** 2026-07-23
- **หน้าที่กระทบ:** `/admin-react/reports/assignments`

## เป้าหมาย

เพิ่มหน้า report ใน Report Hub สำหรับภาพรวมงานมอบหมาย (Assignment batches) เพื่อให้ admin ดูจำนวนงาน, ผู้เรียน, คอร์ส, completion, overdue และเปิดไปยังรายงานราย batch ได้จากจุดเดียว

## Scope

- Backend: เพิ่ม endpoint `GET /api/Reports/assignments`
- Application: เพิ่ม DTO และ `IReportService.GetAssignmentSummaryReportAsync(...)`
- Frontend: เพิ่ม route `/reports/assignments`, card ใน Report Hub, หน้า `AssignmentSummaryReportPage`
- Tests: เพิ่ม regression test ใน `ReportServiceTests`

## Contract

Response shape:

```text
{ success: true, data: AssignmentSummaryReportDto }
```

`AssignmentSummaryReportDto` มี `generatedAt`, total counters และ `rows` ราย batch โดย row ใช้ `assignmentId` สำหรับเปิด `/assignments/{id}/report`

## Verification

- `npm run lint` ✓
- `npm run build` ✓
- `dotnet build iLearn.Tests -o artifacts\verify-test` ✓
- `dotnet test artifacts\verify-test\iLearn.Tests.dll` → **275/275 passed** ✓

## Notes

- ใช้ assignment batch grouping ตาม `AssignmentNo`; ถ้าไม่มีเลข batch ให้ fallback เป็น assignment id เดี่ยว
- สถานะ batch ใช้ `AssignmentStatusKeys.GetBatchStatus(...)` เพื่อคง priority Completed → Upcoming → Expired → InProgress
- นับ completion ผ่าน `EnrollmentAssignment` โดย honor `SnapshotCompleted` ก่อน `Enrollment.IsCompleted`