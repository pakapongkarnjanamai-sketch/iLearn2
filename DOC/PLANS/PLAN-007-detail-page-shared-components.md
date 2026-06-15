# PLAN-007: สร้างชุด shared components สำหรับหน้า Detail + migrate 2 หน้าอ้างอิง

- **Status:** VERIFIED ✅ (Claude review 2026-06-15: commit 0b16325; grep hand-rolled markup=0, build ผ่าน. หมายเหตุ: DetailPageHeader ถูกตัดออกตามทิศ PLAN-008/009-refine — final state ไม่มี header)
- **Assigned:** GPT
- **Priority:** High
- **Estimated scope:** 1 ไฟล์ใหม่ (`src/components/ui/detail/index.tsx` หรือแยกไฟล์ย่อย) + migrate 2 หน้า (`UserDetailPage.tsx`, `ContentItemDetailPage.tsx`)

> **เกี่ยวข้อง:** PLAN-008 (GPT) จะ migrate หน้า detail ที่เหลือโดยใช้ components จากแผนนี้ — **แผนนี้ต้อง DONE ก่อน PLAN-008 จึงเริ่มได้** มาตรฐาน UI ที่เป็นเป้าหมายถูกบันทึกไว้แล้วใน `DOC/ux_ui_analysis.md` หัวข้อ 2.4

## Problem

หน้า detail ทั้ง 6 หน้า (`ContentItemDetailPage`, `CourseDetailPage`, `AssignmentDetailPage`, `LearnerGroupDetailPage`, `UserDetailPage`, `MasterDataDetailPage` + `LearnerProfilePage`) เขียนมาร์กอัปโครงหน้าซ้ำกันเอง:

- กริด 2 คอลัมน์ `grid grid-cols-1 gap-5 lg:grid-cols-[minmax(0,1fr)_280px] lg:items-start` ซ้ำ 6 ไฟล์
- header (eyebrow + h1 `text-2xl font-extrabold text-slate-900`) ซ้ำ 4 ไฟล์ — และ**ไม่สม่ำเสมอ**: `CourseDetailPage` กับ `LearnerGroupDetailPage` ไม่มี page header (ชื่อไปฝังในการ์ดแทน)
- fact item `dt` (`text-slate-400 font-bold uppercase tracking-wider`) + `dd` ซ้ำ 23 จุดใน 4 ไฟล์
- การ์ด section `rounded-lg border border-slate-200 bg-white p-5 space-y-5` + ตัวคั่น sub-section (`hr border-slate-100` + ป้ายจิ๋ว) ซ้ำกระจัดกระจาย

ผิดกติกา "Shared Primitives" ใน `DOC/ux_ui_analysis.md` ข้อ 3.1 — ต้องแยกเป็น components ใน `src/components/ui`

## Scope (ทำแค่นี้)

### 1. สร้าง components ใหม่ใน `iLearn.Admin.React/src/components/ui/detail/`

สร้างตาม spec ใน `DOC/ux_ui_analysis.md` หัวข้อ 2.4 (คลาส Tailwind ลอกจากหน้า `UserDetailPage.tsx` ปัจจุบันซึ่งเป็นแบบที่ถูกต้องที่สุด):

```tsx
// DetailPageHeader — header ก่อนเข้ากริด
type DetailPageHeaderProps = {
  eyebrow: string            // ชื่อโมดูล เช่น "Admin Users"
  title: ReactNode           // ชื่อรายการ
  meta?: ReactNode           // แสดงต่อท้าย title เช่น <StatusBadge/>
}

// DetailLayout — กริด 2 คอลัมน์ + sidebar ขวา
type DetailLayoutProps = {
  sidebar: ReactNode         // ใส่ <ControlsSidebar> ทั้งก้อน
  children: ReactNode        // เนื้อหาหลัก (ถูกครอบ min-w-0 ให้)
}

// DetailCard — การ์ด section (rounded-lg border border-slate-200 bg-white p-5 space-y-5)
type DetailCardProps = { children: ReactNode; className?: string }

// FactGrid — <dl> กริด label-value
type FactGridProps = { cols?: 2 | 3; children: ReactNode }   // default 3 (grid-cols-2 sm:grid-cols-3)

// Fact — 1 ช่องใน FactGrid
type FactProps = {
  label: string
  children: ReactNode        // ค่า — ส่ง StatusBadge/StatusText ได้
  mono?: boolean             // font-mono + wrap-break-word สำหรับรหัส/path
  colSpan?: 1 | 2 | 'full'
}

// DetailSubSection — เส้นคั่น + ป้ายหัวข้อจิ๋ว + เนื้อหา
type DetailSubSectionProps = { title: string; children: ReactNode }
```

- export ทุกตัวจาก `src/components/ui/detail/index.ts(x)` — จะรวมไฟล์เดียวหรือแยกไฟล์ละ component ก็ได้ (แนะนำแยกแล้ว re-export)
- ห้ามใส่ logic ดึงข้อมูล/router ใน components เหล่านี้ — เป็น presentational ล้วน

### 2. Migrate `UserDetailPage.tsx` (หน้าอ้างอิงที่ 1 — ตรง spec อยู่แล้ว แค่เปลี่ยนมาใช้ component)

- header → `DetailPageHeader` (eyebrow "Admin Users", title ชื่อ/NID)
- กริด → `DetailLayout` (sidebar = `ControlsSidebar` เดิม)
- การ์ด → `DetailCard`, facts → `FactGrid`/`Fact`, กลุ่ม Organization Info + Administrative Roles → `DetailSubSection`
- **พฤติกรรม/หน้าตาเดิมต้องไม่เปลี่ยน** (pixel-equivalent — diff ควรเป็นการแทนที่มาร์กอัปเท่านั้น)

### 3. Migrate `ContentItemDetailPage.tsx` (หน้าอ้างอิงที่ 2 — มี fact เยอะสุด + เคส mono)

- header/กริด/การ์ด → เหมือนข้อ 2
- Quick facts (8 ช่อง) → `FactGrid`/`Fact`; Technical paths (Launch Resource, Server Path) → `Fact mono` ใน `FactGrid cols={2}` หรือ `DetailSubSection` ตามโครงเดิม
- ControlsSidebar + ปุ่มทั้งหมด (Publish/Unpublish/Download/Delete) — ไม่เปลี่ยน logic

## Out of scope (ห้ามแตะ)

- ห้ามแตะหน้า detail อื่น (`CourseDetailPage`, `AssignmentDetailPage`, `LearnerGroupDetailPage`, `MasterDataDetailPage`, `LearnerProfilePage`) — เป็นงาน PLAN-008
- ห้ามแก้ `ControlsSidebar`, `SectionHeader`, `StatusBadge` ฯลฯ — components ใหม่ต้อง compose ของเดิม ไม่ใช่แทนที่
- ห้ามเปลี่ยน logic ดึงข้อมูล / ปุ่ม / confirm flow ใด ๆ ในสองหน้าที่ migrate
- ห้ามแก้ `DOC/ux_ui_analysis.md` (planner อัปเดตแล้ว)

## Acceptance criteria

- [x] `src/components/ui/detail/` มีครบ: `DetailPageHeader`, `DetailLayout`, `DetailCard`, `FactGrid`, `Fact`, `DetailSubSection` (presentational ล้วน, มี JSDoc สั้น ๆ ต่อตัว)
- [x] `UserDetailPage.tsx` และ `ContentItemDetailPage.tsx` ไม่เหลือมาร์กอัปกริด 2 คอลัมน์ / header / dt-dd ที่เขียนเอง (grep `minmax(0,1fr)_280px` ในสองไฟล์นี้ = 0)
- [x] หน้าตาทั้งสองหน้าเหมือนเดิมทุกจุด (เทียบ browser ก่อน/หลัง)
- [x] ปุ่มทุกตัวใน ControlsSidebar ของทั้งสองหน้ายังทำงานครบ

## Verification

```powershell
# จาก iLearn.Admin.React
npm run lint
npm run build
```
ทดสอบ manual: เปิด `/users/:id` และ `/content-library/:id` เทียบหน้าตากับก่อนแก้ + กดปุ่มทุกตัว (Edit/Delete/Publish/Download)

## Implementer Notes

- สร้าง shared components ใหม่ที่ `src/components/ui/detail/index.tsx` ครบ 6 ตัวตามแผน (`DetailPageHeader`, `DetailLayout`, `DetailCard`, `FactGrid`, `Fact`, `DetailSubSection`) และทำเป็น presentational-only
- Migrate `UserDetailPage.tsx` ให้ใช้ shared components ทั้ง header/layout/facts/sub-sections โดยคง logic ดึงข้อมูล, breadcrumb, confirm delete, controls actions เดิม
- Migrate `ContentItemDetailPage.tsx` ให้ใช้ shared components ทั้ง header/layout/facts โดยคง logic publish/unpublish/open/download/delete เดิม
- Verification ผ่าน: `npm run lint` (0 errors, 11 warnings baseline), `npm run build` ผ่าน
- Manual smoke ผ่าน: เปิด `/users/1` และ `/content-library/1418` ได้ตามปกติ, controls sidebar/action buttons แสดงครบ และโครง detail แสดงผลตามมาตรฐานใหม่
