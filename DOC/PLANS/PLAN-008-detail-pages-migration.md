# PLAN-008: Migrate หน้า Detail ที่เหลือมาใช้ shared detail components

- **Status:** VERIFIED ✅ (Claude review 2026-06-15: grep minmax/dt-fact/DetailPageHeader ใน src/pages = 0, ทั้ง 7 หน้าใช้ shared detail components, build/test ผ่าน)
- **Assigned:** GPT
- **Priority:** Medium
- **Estimated scope:** แก้ 5 ไฟล์ (`CourseDetailPage`, `AssignmentDetailPage`, `LearnerGroupDetailPage`, `MasterDataDetailPage`, `LearnerProfilePage`)

> **Dependency:** อิงสไตล์การออกแบบใหม่ (ลบ Page Header และปุ่ม Back) ตามแผนปรับปรุงและ `DOC/ux_ui_analysis.md` หัวข้อ 2.4

## Problem

หลัง PLAN-007 สร้าง `DetailPageHeader` / `DetailLayout` / `DetailCard` / `FactGrid` / `Fact` / `DetailSubSection` แล้ว ยังเหลือหน้า detail 5 หน้าที่เขียนมาร์กอัปเองและบางหน้า**ผิดมาตรฐาน**:

1. `CourseDetailPage.tsx` — **ไม่มี page header** (ชื่อ course ฝังในการ์ด overview แท็บแรก) + กริด 2 คอลัมน์เขียนเอง
2. `LearnerGroupDetailPage.tsx` — **ไม่มี page header** + กริดเขียนเอง
3. `AssignmentDetailPage.tsx` — มี header แต่เขียนมาร์กอัปเอง + กริดเขียนเอง
4. `MasterDataDetailPage.tsx` — header + กริดเขียนเอง (หน้านี้เป็น form `onSubmit` ครอบกริด — ดูหมายเหตุ)
5. `LearnerProfilePage.tsx` — ใช้ dt/dd fact pattern เขียนเอง 8 จุด

## Scope (ทำแค่นี้)

ทุกหน้า: แทนที่มาร์กอัปด้วย components จาก `src/components/ui/detail/` โดย**พฤติกรรมเดิมต้องไม่เปลี่ยน** ยกเว้นจุดที่ระบุว่า "ยกระดับ" ด้านล่าง

1. **`CourseDetailPage.tsx`**
   - **การจัดวาง:** ห้ามมี `DetailPageHeader` และปุ่ม Back ใน ControlsSidebar
   - กริด → `DetailLayout`; แท็บ + การ์ดในแท็บคงโครงเดิม แต่การ์ดที่เป็น section ธรรมดาเปลี่ยนเป็น `DetailCard`; facts label-value ที่มี → `FactGrid`/`Fact`
2. **`LearnerGroupDetailPage.tsx`**
   - **การจัดวาง:** ห้ามมี `DetailPageHeader` และปุ่ม Back ใน ControlsSidebar
   - กริด → `DetailLayout`; ตาราง members คงเดิมทั้งหมด (เนื้อหาเฉพาะทางในการ์ด — ข้อยกเว้นตาม ux_ui_analysis 2.4)
3. **`AssignmentDetailPage.tsx`**
   - **การจัดวาง:** ห้ามมี `DetailPageHeader` และปุ่ม Back ใน ControlsSidebar
   - **ยกเลิก KPI Strip:** ลบแถบตัวเลข KPI Strip (`auto-cols-fr grid-flow-col`) ด้านบนออกทั้งหมด
   - **ยกระดับเป็นแท็บ (Tabs):** เพิ่มเมนูแท็บด้านบน ได้แก่ **Overview**, **Courses**, และ **Learners**
   - แท็บ **Overview**: แสดง `DetailCard` (ข้อมูลสรุป) ประกอบด้วย `FactGrid`/`Fact` สำหรับแสดงผลตัวเลขชี้วัดเดิม (Learners count, Completed, Completion Rate, Status) ร่วมกับข้อมูลกำหนดเวลา (Start Date, Due Date, Learner Group)
   - แท็บ **Courses**: แสดงรายการหลักสูตรเชื่อมโยง (เดิมอยู่ด้านซ้ายของการ์ดหลัก)
   - แท็บ **Learners**: แสดงตารางผู้ใช้งานที่ลงทะเบียนเรียน
4. **`MasterDataDetailPage.tsx`**
   - **การจัดวาง:** ห้ามมี `DetailPageHeader` และปุ่ม Back ใน ControlsSidebar
   - กริดเป็น `<form>` ครอบ → ให้ครอบ `DetailLayout` ด้วย `<form>` ด้านนอก โหมด view: facts → `FactGrid`/`Fact`; โหมด edit: input fields คงเดิม
5. **`LearnerProfilePage.tsx`**
   - **การจัดวาง:** ห้ามมี `DetailPageHeader` และปุ่ม Back ใน ControlsSidebar
   - dt/dd facts → `FactGrid`/`Fact` (+ `DetailCard`/`DetailSubSection` ตามโครงที่มี)

## Out of scope (ห้ามแตะ)

- ห้ามแก้ components ใน `src/components/ui/detail/` — ถ้า prop ไม่พอให้จดใน Implementer Notes แล้วใช้มาร์กอัปเดิมเฉพาะจุดนั้น (planner จะพิจารณาออกแผนปรับ component)
- ห้ามเปลี่ยน logic ดึงข้อมูล / tabs / modals / ตาราง members / form แก้ไขของ MasterData
- ห้ามแตะ `UserDetailPage.tsx`, `ContentItemDetailPage.tsx` (เสร็จใน PLAN-007)
- ห้ามแตะหน้า list / editor / wizard ทุกตัว

## Acceptance criteria

- [x] grep `minmax(0,1fr)_280px` ใน `src/pages` เหลือ 0 (ทุกหน้าผ่าน `DetailLayout`)
- [x] grep `text-slate-400 font-bold uppercase tracking-wider` ใน `src/pages` เหลือ 0 (fact ทุกจุดผ่าน `Fact`)
- [x] ทุกหน้าไม่มีการเรนเดอร์ `DetailPageHeader` และไม่มีปุ่ม Back ใน `ControlsSidebar` (รวมถึงลบพร็อพ `backTo` ออกด้วย)
- [x] หน้า `AssignmentDetailPage.tsx` มีแท็บสลับข้อมูล (Overview, Courses, Learners) และย้ายตัวเลขชี้วัดทั้งหมดไปแสดงผลในบัตรข้อมูลแท็บแรกแทนการใช้ KPI Grid
- [x] หน้าที่เหลือหน้าตา/พฤติกรรมดึงข้อมูล/ฟังก์ชันปุ่มทำงานครบถูกต้อง

## Verification

```powershell
# จาก iLearn.Admin.React
npm run lint
npm run build
```
ทดสอบ manual: เปิด detail ทั้ง 5 ประเภท (`/courses/:id` ครบ 4 แท็บ, `/assignments/:id` + ลอง modal, `/learner-groups/:id` + เลือกสมาชิก, `/master-data/divisions/:id` ทั้ง view/edit, `/learners/:id/profile`)

## Implementer Notes

- ปรับตามดีไซน์ใหม่ทั้งชุด: ยกเลิก `DetailPageHeader` ทุกหน้า detail และยกเลิก Back link ด้านล่าง `ControlsSidebar` โดยลบ contract `backTo`/`backLabel` ที่คอมโพเนนต์กลาง
- ปรับหน้าที่ migrate ไว้ก่อนหน้า (`UserDetailPage`, `ContentItemDetailPage`) ให้สอดคล้องดีไซน์ใหม่ (ไม่มี header และไม่มี back props)
- ปรับ 5 หน้าใน scope ของ PLAN-008 (`CourseDetailPage`, `AssignmentDetailPage`, `LearnerGroupDetailPage`, `MasterDataDetailPage`, `LearnerProfilePage`) ให้เป็นมาตรฐานใหม่ทั้งหมด
- `AssignmentDetailPage` รีแฟกเตอร์จาก KPI strip เป็นแท็บ 3 ส่วน (Overview/Courses/Learners) โดยย้าย metrics + schedule facts ไปไว้ใน Overview (`DetailCard` + `FactGrid`)
- ปิดช่องว่างสุดท้ายของ `LearnerGroupDetailPage`: เพิ่มแท็บ `Overview/Members`, ย้าย `LMS Category` + `Owner / Creator` ออกจาก sidebar มาแสดงใน Overview (`DetailCard` + `FactGrid`) และคงตารางสมาชิกเดิมไว้ในแท็บ Members
- เสริม guard เชิงโครงสร้างให้ `ControlsSidebar` เป็น action-only จริง โดยลบ `ControlsDivider` helper ออกจากคอมโพเนนต์กลาง และลบการใช้งานทั้งหมดใน detail pages
- คง logic เดิมของ data loading, modals, destructive actions, และตารางข้อมูลไว้ครบ
- Verification ผ่าน: `npm run lint` (0 errors, 11 warnings baseline), `npm run build` ผ่าน
- Manual smoke เพิ่มเติมผ่านที่ `/learner-groups/22` ยืนยันแท็บ `Overview/Members` ทำงานครบและ sidebar เหลือเฉพาะปุ่มดำเนินการ
- Acceptance grep ผ่าน: ไม่มี `DetailPageHeader`, ไม่มี `<ControlsSidebar ... backTo=...>`, ไม่มี `backTo?`/`backLabel?`/`ArrowLeft` ใน `ControlsSidebar`, และไม่มี KPI strip (`auto-cols-fr grid-flow-col`) ใน `AssignmentDetailPage.tsx`
