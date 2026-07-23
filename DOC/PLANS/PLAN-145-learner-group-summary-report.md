# PLAN-145 — Learner Group Summary Report

- **สถานะ:** DONE
- **Assigned:** GitHub Copilot (GPT)
- **วันที่:** 2026-07-23
- **หน้าที่กระทบ:** `/admin-react/reports/learner-groups`, `/admin-react/reports/assignments`

## เป้าหมาย

เพิ่มหน้า report ใน Report Hub สำหรับข้อมูลแบบ Learner Group และยืนยันว่า Assignment report เข้าใช้งานจาก Report Hub ได้ครบ เพื่อให้ admin ดูภาพรวมจำนวนกลุ่ม สมาชิก งานมอบหมาย รายการเรียน และ completion/overdue ได้จากจุดเดียว

## Scope

- Backend: เพิ่ม endpoint `GET /api/Reports/learner-groups`
- Application: เพิ่ม DTO และ `IReportService.GetLearnerGroupSummaryReportAsync(...)`
- Frontend: เพิ่ม route `/reports/learner-groups`, card ใน Report Hub, หน้า `LearnerGroupSummaryReportPage`
- Tests: เพิ่ม regression test ใน `ReportServiceTests`

## Contract

Response shape:

```text
{ success: true, data: LearnerGroupSummaryReportDto }
```

`LearnerGroupSummaryReportDto` มี `generatedAt`, total counters และ `rows` ราย Learner Group โดย row ใช้ `learnerGroupId` สำหรับเปิด `/learner-groups/{id}`

## Verification

- Focused `ReportServiceTests.LearnerGroupSummary_ScopesGroups_AndCountsMemberEnrollments` ✓
- `npm run lint` ✓
- `npm run build` ✓ (Vite chunk-size warning เดิม)
- `dotnet build .\iLearn.Tests\iLearn.Tests.csproj -o .\artifacts\verify-test` ✓ (warnings เดิม)
- `dotnet test .\artifacts\verify-test\iLearn.Tests.dll` → **276/276 passed** ✓

## Notes

- Report ต้อง honor division scope ผ่าน `LearnerGroup.DivisionId`
- Completion/overdue นับจาก visible enrollments ของสมาชิกในกลุ่ม และใช้ effective schedule จาก report projection เดิม
- Assignment count นับ assignment batch ที่ผูก `LearnerGroupId` กับกลุ่มนั้น โดย distinct ตาม `AssignmentNo`
- Assignment report เดิม `/reports/assignments` ยังอยู่ใน Report Hub; งานนี้เพิ่ม Learner Group report เป็นหน้าคู่กัน