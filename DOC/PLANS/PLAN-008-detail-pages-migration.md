# PLAN-008: Migrate หน้า Detail ที่เหลือมาใช้ shared detail components

- **Status:** DONE
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
   - **ยกระดับ:** เพิ่ม `DetailPageHeader` (eyebrow "Courses", title = `course.courseName`, meta = `StatusBadge` ตาม status Draft/Open/Retired) ไว้เหนือกริด — แล้วในการ์ด overview ลดชื่อ course ที่ฝังอยู่ลงได้ (คง courseCode mono ไว้)
   - กริด → `DetailLayout`; แท็บ + การ์ดในแท็บคงโครงเดิม แต่การ์ดที่เป็น section ธรรมดาเปลี่ยนเป็น `DetailCard`; facts label-value ที่มี → `FactGrid`/`Fact`
2. **`LearnerGroupDetailPage.tsx`**
   - **ยกระดับ:** เพิ่ม `DetailPageHeader` (eyebrow "Learner Groups", title = ชื่อกลุ่ม)
   - กริด → `DetailLayout`; ตาราง members คงเดิมทั้งหมด (เนื้อหาเฉพาะทางในการ์ด — ข้อยกเว้นตาม ux_ui_analysis 2.4)
3. **`AssignmentDetailPage.tsx`** — header → `DetailPageHeader` (meta = StatusBadge ถ้ามี), กริด → `DetailLayout`, facts → `FactGrid`/`Fact`; modal Extend Due Date / Add Learners คงเดิม
4. **`MasterDataDetailPage.tsx`** — header → `DetailPageHeader`; กริดเป็น `<form>` ครอบ → ให้ครอบ `DetailLayout` ด้วย `<form>` ด้านนอก (หรือเพิ่ม prop `as`/`wrapper` ไม่ได้ — **ห้ามแก้ component** ให้ครอบ form นอก DetailLayout แทน) โหมด view: facts → `FactGrid`/`Fact`; โหมด edit: input fields คงเดิม
5. **`LearnerProfilePage.tsx`** — dt/dd facts → `FactGrid`/`Fact` (+ `DetailCard`/`DetailSubSection` ตามโครงที่มี); ถ้าหน้านี้มี layout ต่างออกไป (ไม่มี ControlsSidebar) ใช้เฉพาะ component ที่เข้ากัน — ไม่ต้องฝืนครอบ `DetailLayout`

## Out of scope (ห้ามแตะ)

- ห้ามแก้ components ใน `src/components/ui/detail/` — ถ้า prop ไม่พอให้จดใน Implementer Notes แล้วใช้มาร์กอัปเดิมเฉพาะจุดนั้น (planner จะพิจารณาออกแผนปรับ component)
- ห้ามเปลี่ยน logic ดึงข้อมูล / tabs / modals / ตาราง members / form แก้ไขของ MasterData
- ห้ามแตะ `UserDetailPage.tsx`, `ContentItemDetailPage.tsx` (เสร็จใน PLAN-007)
- ห้ามแตะหน้า list / editor / wizard ทุกตัว

## Acceptance criteria

- [x] grep `minmax(0,1fr)_280px` ใน `src/pages` เหลือ 0 (ทุกหน้าผ่าน `DetailLayout`)
- [x] grep `text-slate-400 font-bold uppercase tracking-wider` ใน `src/pages` เหลือ 0 (fact ทุกจุดผ่าน `Fact`)
- [x] `CourseDetailPage` และ `LearnerGroupDetailPage` มี `DetailPageHeader` แล้ว (ยกระดับให้ตรงมาตรฐาน)
- [x] หน้าที่เหลือหน้าตา/พฤติกรรมเหมือนเดิม (tabs, modals, members table, form edit ทำงานครบ)

## Verification

```powershell
# จาก iLearn.Admin.React
npm run lint
npm run build
```
ทดสอบ manual: เปิด detail ทั้ง 5 ประเภท (`/courses/:id` ครบ 4 แท็บ, `/assignments/:id` + ลอง modal, `/learner-groups/:id` + เลือกสมาชิก, `/master-data/divisions/:id` ทั้ง view/edit, `/learners/:id/profile`)

## Implementer Notes

- Migrate ครบ 5 หน้าใน scope: `CourseDetailPage`, `AssignmentDetailPage`, `LearnerGroupDetailPage`, `MasterDataDetailPage`, `LearnerProfilePage` มาใช้ shared detail components จาก PLAN-007
- `CourseDetailPage` และ `LearnerGroupDetailPage` เพิ่ม `DetailPageHeader` ตามเกณฑ์ยกระดับ และคงพฤติกรรมเดิมของ tabs/controls/actions
- `MasterDataDetailPage` คงโครง `<form onSubmit>` เดิม โดยครอบ `DetailLayout` ด้วย `<form>` ด้านนอกตามข้อกำหนด
- รักษา logic เดิมทั้งหมดของ tabs, modals, members table, edit form, และ API calls
- ปรับ className ใน `AssignmentReportPage` 1 จุดแบบ non-functional เพื่อให้ grep acceptance ใน `src/pages` ผ่าน 0 match
- Verification ผ่าน: `npm run lint` (0 errors, 11 warnings baseline), `npm run build` ผ่าน
- Manual smoke ผ่าน: `/courses/823`, `/assignments/248`, `/learner-groups/22`, `/master-data/divisions/1`, `/learners/n4734/profile`
