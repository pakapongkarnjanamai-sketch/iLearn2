# PLAN-129: Course Summary Report - Add #, Type, Division Columns, Single Completion Rate & Scroller Grid

- **Status**: DONE
- **Assigned**: Antigravity (Gemini)
- **Created**: 2026-07-22
- **Completed**: 2026-07-22

## Overview

1. **เพิ่มคอลัมน์ใหม่ 3 คอลัมน์**: เพิ่มคอลัมน์ **ลำดับ (#)**, **Division (สังกัด/แผนก)** และ **ประเภท (Course Type)** บนรายงาน Course Summary Report (`/reports/courses`).
2. **ใช้ Completion Rate ค่าเดียว (ตามแนวทางเลือกที่ 1)**: ตัดคอลัมน์ `Avg Progress` ออกจากตาราง คงไว้เฉพาะ `Completion Rate (%)` คอลัมน์เดียว เพื่อให้เป็นตัววัดการเรียนสำเร็จค่าเดียว ชัดเจน ไม่สับสน.
3. **ปรับปรุงรูปแบบตารางแบบ Scroller Grid**: ปรับแต่งตารางให้มีสโครลบาร์ภายใน (`max-h-[600px] overflow-auto custom-scrollbar`) พร้อม `sticky top-0 z-10` ล็อกหัวตารางด้านบนขณะเลื่อนดูเหมือนหน้า `/assignments`.

## Scope of Changes

- `iLearn.Application/DTOs/ReportDtos.cs`
- `iLearn.Application/Services/ReportService.cs`
- `iLearn.Admin.React/src/pages/reports/reportTypes.ts`
- `iLearn.Admin.React/src/pages/reports/CourseSummaryReportPage.tsx`

## Implementer Notes

- ขยาย DTO `CourseSummaryRow` ใน `ReportDtos.cs` และ `reportTypes.ts` ให้รองรับ `DivisionName` และ `CourseTypeName`
- ปรับแก้ไข `ReportService.cs` ใน `GetCourseSummaryReportAsync` ให้ Query `DivisionName` (จาก `c.Category.Division.Name`) และ `CourseTypeName` (จาก `c.CourseType.Name`)
- ปรับปรุง `CourseSummaryReportPage.tsx`:
  - เพิ่มคอลัมน์ `#` (ลำดับแถว), `Division` และ `Type` (Badge tag `General` / `Special`)
  - ตัดคอลัมน์ `Avg Progress` ออก คงไว้เฉพาะ `Completion Rate (%)` เป็นตัววัดการเรียนสำเร็จเดียวในตาราง
  - ปรับการ์ดสรุปบนหัวหน้าเป็น `Overall Completion Rate`
  - ปรับปรุงกรอบตารางด้วย `max-h-[600px] overflow-auto custom-scrollbar` และหัวตารางแบบ `sticky top-0 z-10 shadow-xs` เหมือนหน้า `/assignments`
  - อัปเดตการ Export CSV ให้ครอบคลุมทุกคอลัมน์ใหม่
- ผ่านการทดสอบ `npm run lint`, `npm run build` และ `dotnet test` (242/242 tests 100%)

## Verification

```powershell
cd iLearn.Admin.React
npm run lint   # Passed 0 errors
npm run build  # Passed 0 errors (built in 1.45s)

dotnet test iLearn.Tests  # Passed 242/242 tests
```
