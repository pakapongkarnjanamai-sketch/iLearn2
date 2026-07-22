# PLAN-127: Standardizing Reports Pages UI Design

- **Status**: DONE
- **Assigned**: Antigravity (Gemini)
- **Created**: 2026-07-22
- **Completed**: 2026-07-22

## Overview

ออกแบบและปรับปรุงส่วนงานรายงาน (Reports) ทั้งหมดใน `iLearn.Admin.React/src/pages/reports` ให้มีมาตรฐานเดียวกันตาม UI Conventions ของระบบ (Card, PageHeader/SectionHeader, Badge, StatusBadge, ListToolbar, SegmentedToggle, AppButton, IconButton, format.ts, tableStandards.ts):

1. `ReportHubPage.tsx` — Hub รวมรายงาน ปรับ Card design, category tags, visual hierarchy และ stats
2. `ComplianceReportPage.tsx` — Compliance & Overdue report, KPI summary cards, division chart, division/dept breakdown, overdue search & list table
3. `CourseSummaryReportPage.tsx` — Course completion summary, เพิ่ม KPI summary cards, sortable table, list toolbar search filter
4. `ActivityReportPage.tsx` — Training activity report, เพิ่ม period KPI summary cards, period selector, dual Recharts charts, monthly breakdown table
5. `TranscriptReportPage.tsx` — Learner transcript, redesign search header, learner info summary, filtered training records, print-ready CSS formatting

## Scope of Changes

- `iLearn.Admin.React/src/pages/reports/ReportHubPage.tsx`
- `iLearn.Admin.React/src/pages/reports/ComplianceReportPage.tsx`
- `iLearn.Admin.React/src/pages/reports/CourseSummaryReportPage.tsx`
- `iLearn.Admin.React/src/pages/reports/ActivityReportPage.tsx`
- `iLearn.Admin.React/src/pages/reports/TranscriptReportPage.tsx`

## Implementer Notes

- ปรับปรุงการแสดงผล visual hierarchy และ back navigation บนทุกหน้าย่อยของรายงาน ให้ย้อนกลับสู่ `/reports` ได้สะดวก
- ยกระดับ KPI Summary Cards บน Compliance, Course Summary, Activity และ Transcript ให้ใช้โครงสร้าง `Card` / Grid ที่สะอาด เรียบร้อย ปราศจาก inline tailwind drift
- เพิ่มการค้นหากรองข้อมูลภายในตาราง (ListToolbar) และปุ่มส่งออก CSV (AppButton)
- ตรวจสอบผ่าน `npm run lint` 0 errors และ `npm run build` 0 errors

## Verification

```powershell
cd iLearn.Admin.React
npm run lint   # Passed 0 errors
npm run build  # Passed 0 errors (built in 1.71s)
```
