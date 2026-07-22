# PLAN-128: Show All Courses in Course Summary Report & Add Metrics Guide Help Modal

- **Status**: DONE
- **Assigned**: Antigravity (Gemini)
- **Created**: 2026-07-22
- **Completed**: 2026-07-22

## Overview

1. **แสดง Courses ทั้งหมดในระบบ**: ปรับปรุง `ReportService.GetCourseSummaryReportAsync` ให้เริ่ม query จาก `Courses` หลัก (กรองตาม `divisionId` ถ้ามี) แล้ว LEFT JOIN กับสถิติการเรียน เพื่อให้คอร์สทั้งหมดในแคตตาล็อก (รวมคอร์สที่มี 0 enrollments) ปรากฏในรายงานสรุป
2. **สร้าง Popup อธิบายสถิติ (Metrics Guide Modal)**: เพิ่มปุ่ม "Metrics Guide / คู่มือตัววัด" และไอคอน Info บนหน้า `CourseSummaryReportPage.tsx` เมื่อคลิกจะเปิด `Modal` ป๊อปอัปแสดงคำอธิบาย, สูตรคำนวณ, และตัวอย่างเปรียบเทียบระหว่าง **Completion Rate (%)** กับ **Avg Progress (%)** ชัดเจน

## Scope of Changes

- `iLearn.Application/Services/ReportService.cs`
- `iLearn.Admin.React/src/pages/reports/CourseSummaryReportPage.tsx`

## Implementer Notes

- ปรับแก้ไข `ReportService.cs` ใน `GetCourseSummaryReportAsync`: ดึงรายการ `_courseRepo` ทั้งหมดเป็น Query หลัก และดึงสถิติ `courseGroups` มา Left-Join ทำให้คอร์สที่เพิ่งสร้างหรือยังไม่มีผู้เรียน (0 enrollments) ไม่หลุดจากตารางรายงานอีกต่อไป
- เพิ่มปุ่ม "Metrics Guide" และ `Modal` Popup ใน `CourseSummaryReportPage.tsx`: พร้อมตัวอย่างสถานการณ์จริง อธิบายข้อแตกต่างระหว่าง **Completion Rate (%)** (วัดผู้เรียนจบ 100%) และ **Avg Progress (%)** (วัดเนื้อหาบทเรียนสะสมเฉลี่ย)
- ผ่านการทดสอบ `npm run lint`, `npm run build` และ `dotnet test` 214/214 tests 100%

## Verification

```powershell
cd iLearn.Admin.React
npm run lint   # Passed 0 errors
npm run build  # Passed 0 errors (built in 1.24s)

dotnet test iLearn.Tests  # Passed 214/214 tests
```
