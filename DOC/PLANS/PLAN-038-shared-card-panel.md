# PLAN-038: Shared `Card` (Panel) component — รวมการ์ดเนื้อหา detail pages

- **Status:** VERIFIED ✅ (Claude Code review 2026-06-16)
- **Assigned:** Gemini (Antigravity)
- **Priority:** Medium-High (payoff สูง: กระทบเกือบทุกหน้า detail)
- **Estimated scope:** เพิ่ม shared component 1 ตัว (`Card.tsx`) + refactor `<section>` การ์ดในหน้า detail ~10 ไฟล์

## Problem

หน้า detail แทบทุกหน้าใน `iLearn.Admin.React` เขียน "การ์ดเนื้อหา" ด้วยมือเป็นชุด class เดียวกันเป๊ะ และจับคู่กับ `<SectionHeader variant="card">` เสมอ:

```jsx
<section className="overflow-hidden rounded-lg border border-slate-200 bg-white shadow-xs">
  <SectionHeader icon={BookOpen} variant="card">Overview</SectionHeader>
  <div className="p-5 space-y-5">...</div>
</section>
```

พบรูปแบบนี้ **19 จุด ใน 10 ไฟล์**: CourseDetailPage (4), DashboardPage (3), AssignmentDetailPage (3), VersionDetailPage (2), LearnerGroupDetailPage (2), UserDetailPage, ContentItemDetailPage, MasterDataDetailPage, AssignmentReportPage, LearnerProfilePage — และ `SectionHeader variant="card"` ถูกเรียกคู่กันรวม 23 ครั้ง

ปัญหา drift ที่เกิดแล้ว:
- บางการ์ดมี `shadow-xs` บางอันไม่มี (เช่น [CourseDetailPage.tsx:560/658/734](file:///c:/Users/n4734/source/repos/iLearn2/iLearn.Admin.React/src/pages/courses/CourseDetailPage.tsx#L560) ไม่มี `shadow-xs`)
- ต้องจำว่าต้องใส่ `variant="card"` ที่ SectionHeader ทุกครั้ง (ลืม = หัวการ์ดผิด)

### Component ที่เกี่ยวข้อง (ของจริง)
- [SectionHeader.tsx](file:///c:/Users/n4734/source/repos/iLearn2/iLearn.Admin.React/src/components/ui/SectionHeader.tsx) — มี `variant: 'plain' | 'card'`, props `icon` (LucideIcon, **required**), `children`, `actions`

---

## Scope (ทำแค่นี้)

### 1. สร้าง `src/components/ui/Card.tsx`
รวม section wrapper + SectionHeader (card variant) ในตัว:

```tsx
import type { ReactNode } from 'react'
import type { LucideIcon } from 'lucide-react'
import { SectionHeader } from './SectionHeader'

type CardProps = {
  /** ถ้ามี title จะ render หัวการ์ด (SectionHeader variant="card"); ไม่มี = การ์ดเปล่า */
  title?: ReactNode
  icon?: LucideIcon
  actions?: ReactNode
  children: ReactNode
  /** เพิ่ม class ที่ <section> (เช่น lg:col-span-2, min-w-0) */
  className?: string
  /** เพิ่ม class ที่กล่อง body; ค่า default = ไม่มี padding (ให้ตารางชนขอบได้) */
  bodyClassName?: string
}
```
- `<section>` ใช้ class มาตรฐานตายตัว: `overflow-hidden rounded-lg border border-slate-200 bg-white shadow-xs` + `className` ที่ส่งมา
- ถ้ามี `title` → render `<SectionHeader icon={icon ?? <fallback>} variant="card" actions={actions}>{title}</SectionHeader>`
  - **หมายเหตุ:** ปัจจุบัน SectionHeader บังคับ `icon` (required) — ถ้าจะรองรับการ์ดไม่มีไอคอน ให้แก้ `SectionHeader` ให้ `icon?` เป็น optional (render เฉพาะเมื่อมี) **ในแผนนี้ได้** ถือเป็น scope เดียวกัน
- body = `<div className={bodyClassName}>{children}</div>` (ไม่ยัด padding เริ่มต้น เพราะบางการ์ดเป็นตารางเต็ม บางการ์ดเป็น `p-5`)

### 2. Refactor call site (19 จุด, 10 ไฟล์)
แทน pattern เดิมด้วย `<Card>`:
```jsx
// เดิม
<section className="overflow-hidden rounded-lg border border-slate-200 bg-white shadow-xs">
  <SectionHeader icon={BookOpen} variant="card">Overview</SectionHeader>
  <div className="p-5 space-y-5">{...}</div>
</section>
// ใหม่
<Card icon={BookOpen} title="Overview" bodyClassName="p-5 space-y-5">{...}</Card>
```
- การ์ดที่มี `lg:col-span-2` / `min-w-0` → ส่งผ่าน `className`
- การ์ดที่ไม่มีหัว (เช่น KPI strip ใน DashboardPage:289 ที่เป็น grid พิเศษ) — **ประเมินก่อน** ถ้าโครงสร้างต่างมาก (ไม่ใช่ header+body) ให้ข้ามไว้ แล้วจดใน Implementer Notes ห้ามฝืนยัด
- **ทำให้ shadow เป็นมาตรฐานเดียว** — การ์ดที่เคยไม่มี `shadow-xs` (CourseDetailPage:560/658/734) จะได้ `shadow-xs` ตามมาตรฐาน (ตั้งใจ ปรับให้สม่ำเสมอ)

### ขอบเขตที่ห้ามทำ
- ห้ามเปลี่ยนเนื้อหา/logic ภายในการ์ด — แก้แค่ wrapper
- ห้ามแตะ `<section>` ที่ไม่ใช่การ์ดเนื้อหา (เช่น layout grid ระดับหน้า, ControlsSidebar)
- หน้า list (`EntityListPage`/AppTable) ไม่เกี่ยว — ข้าม
- ห้ามแตะ `iLearn.Admin` (MVC)

---

## Verification
```powershell
# จาก iLearn.Admin.React
npm run lint
npm run build
# ยืนยันว่าไม่มี pattern การ์ดเดิมหลงเหลือ (นอก Card.tsx)
rg "rounded-lg border border-slate-200 bg-white shadow-xs" src/pages
```
- เปิดด้วยตา: Course detail, Version detail, Assignment detail, User detail, Learner-group detail, Content-item detail — หัวการ์ด/เงา/ขอบต้องเหมือนเดิมและสม่ำเสมอ

## Implementer Notes
- ได้แก้ไข `SectionHeader` ให้รองรับ optional `icon` (`icon?: LucideIcon | undefined` เพื่อให้ผ่าน tsconfig `exactOptionalPropertyTypes: true` ของโปรเจค)
- ทำการ refactor ทั้งหมด 16 จุด ใน 9 หน้า:
  - `UserDetailPage.tsx` (1 จุด - Overview)
  - `ContentItemDetailPage.tsx` (1 จุด - Overview)
  - `MasterDataDetailPage.tsx` (1 จุด - Details)
  - `LearnerProfilePage.tsx` (1 จุด - Transcript)
  - `AssignmentReportPage.tsx` (1 จุด - Raw learner table wrapper)
  - `LearnerGroupDetailPage.tsx` (2 จุด - Overview, Members)
  - `AssignmentDetailPage.tsx` (3 จุด - Overview, Courses, Learners)
  - `CourseDetailPage.tsx` (4 จุด - Overview, Versions, Learners, Assignments)
  - `VersionDetailPage.tsx` (2 จุด - Overview, Current Content)
- ส่วน `DashboardPage.tsx` (3 จุด) จากการประเมินพบว่าโครงสร้างและหัวข้อต่างมาก (ใช้ local `SectionHeader` ที่มี subtitle และปุ่ม trailing link, โครงสร้าง UI เป็น kpi grid หรือ widgets ทั่วไป) จึงตัดสินใจไม่แปลงเป็น `Card` เพื่อป้องกัน layout พังและไม่ฝืนยัด component
- ตรวจสอบ `npm run build` และ `npm run lint` ของ React UI ผ่าน 100% ไม่มี error
- ตรวจสอบ Unit test backend `iLearn.Tests` (118 test cases) ผ่านทั้งหมด 100%
