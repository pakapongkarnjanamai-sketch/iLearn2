# PLAN-031: สกัด primitive ร่วมของหน้า Detail (Tabs / CourseStatusBadge / Modal) ลด class ซ้ำ

- **Status:** VERIFIED ✅ (Claude review 2026-06-16: DetailTabs + CourseStatusBadge + Modal สร้าง+ใช้ร่วม (A+B+C ครบ), Modal/CourseStatusBadge ใช้ ≥2 ไฟล์, build/lint 0/0 — หมายเหตุ: DetailTabs หลัง PLAN-033 เหลือผู้ใช้แค่ CourseEditor)
- **Assigned:** GPT (GPT-5.3 Codex)
- **Priority:** Low
- **Estimated scope:** 2-3 component ใหม่ใน `src/components/ui` + migrate หน้า detail ที่ใช้ pattern เหล่านั้น

## Problem / สำรวจ

หน้า Detail ทุกหน้า**ใช้ shared layout components แล้ว** (`DetailLayout`/`DetailCard`/`FactGrid`/`Fact` จาก PLAN-007/008) และแทบไม่มี `style={{}}` (inline style จริง = 1 จุดเดียวทั้งระบบ) — มาตรฐานโครงหน้าเสร็จแล้ว

สิ่งที่ยังเหลือคือ **class string Tailwind ยาว ๆ ที่ re-implement ซ้ำในหลายหน้า** (ผู้ใช้เรียก "CSS inline ที่ใช้เยอะ") — ควรสกัดเป็น component/helper ร่วม:

| pattern | ไฟล์ที่ซ้ำ |
|---|---|
| **A. Tab buttons** (`pb-3 px-3 ... text-indigo-600 border-b-2 border-indigo-500` สำหรับแท็บที่เลือก) | `AssignmentDetailPage`, `CourseDetailPage`, `LearnerGroupDetailPage`, `CourseEditorPage` |
| **B. Course status badge** (logic `isDraft`/`isOpen`/`isRetired` → `bg-amber/emerald/rose...`) | `CourseDetailPage`, `CourseListPage` (+ `VersionDetailPage` ถ้ามี) |
| **C. Centered modal shell** (`fixed inset-0 z-50 ... backdrop-blur` + `.modal-window`/`scale-in`) | ~3 ไฟล์ (เช่น `LearnerGroupCategoriesPage`, `AssignmentDetailPage`, `VersionDetailPage`) |

## Scope (ทำแค่นี้ — pure refactor, พฤติกรรม/หน้าตาเหมือนเดิม)

### A. `<DetailTabs>` (หรือ `<Tabs>`) — `src/components/ui/`
- props: `tabs: { key: string; label: string }[]`, `active: string`, `onChange: (key) => void`
- render แท็บปุ่มขอบล่างสีคราม ตามมาตรฐาน `ux_ui_analysis.md` §2.4 (active = `text-indigo-600 border-b-2 border-indigo-500`, inactive = `text-slate-400 hover:text-slate-700`)
- migrate 4 ไฟล์ให้ใช้ `<DetailTabs>` แทน markup แท็บที่เขียนเอง — **คงแท็บ/ลำดับ/พฤติกรรมเดิมทุกหน้า**

### B. Course status badge — รวม logic สี
- ทางเลือก: สร้าง `<CourseStatusBadge status={...} />` (wrapper map status → tone ของ `StatusBadge` ที่มีอยู่) **หรือ** export helper `courseStatusTone(status): tone` แล้วป้อนให้ `StatusBadge`
- map: Draft→amber/neutral, Open(Active)→emerald/success, Retired→rose/danger, Closed→slate/neutral (ให้ตรงสีที่ใช้อยู่เดิม)
- migrate `CourseDetailPage`, `CourseListPage` (+ VersionDetail ถ้ามี) ให้เลิก re-implement class สีเอง → ใช้ตัวร่วม **สีต้องเหมือนเดิม**

### C. `<Modal>` centered shell — `src/components/ui/`
- props: `open: boolean`, `onClose: () => void`, `title?`, `children`, `size?: 'sm'|'md'|'lg'`
- render `fixed inset-0 z-50 ... backdrop-blur-xs` + กล่อง `.modal-window`/`scale-in` (reuse CSS class ที่มีใน index.css) + ปุ่มปิด (X) + click-outside ปิด
- migrate modal ที่ซ้ำให้ใช้ `<Modal>` — **คงเนื้อหา/ฟอร์ม/พฤติกรรมเดิม**
- **ถ้า C ใหญ่/เสี่ยงเกินไปในรอบเดียว** ให้ทำ A+B ก่อน แล้วจด C ไว้ใน Implementer Notes เป็น follow-up (อย่าฝืน)

## Out of scope (ห้ามแตะ)

- ห้ามเปลี่ยน UX/หน้าตา/สี/ลำดับแท็บ/พฤติกรรม (pure refactor — แค่ย้าย markup ซ้ำไป component ร่วม)
- ห้ามแตะ `DetailLayout`/`DetailCard`/`Fact*` (เสร็จแล้ว ใช้ดีอยู่)
- ห้ามแตะ Explorer pages (CourseListPage ใช้ status badge — แตะเฉพาะส่วน badge ได้ แต่ไม่แตะ explorer logic)
- ห้ามแตะ backend
- ห้ามสกัด primitive ที่ใช้แค่ที่เดียว (over-abstraction) — เฉพาะที่ซ้ำ ≥2 ที่จริง

## Acceptance criteria

- [x] มี `<DetailTabs>` ใน `src/components/ui` — 4 ไฟล์ใช้ร่วม (ไม่มี markup แท็บเขียนเองเหลือ)
- [x] course status badge รวมเป็นตัวเดียว — `CourseDetail`/`CourseList` เลิก re-implement สีเอง, **สีเหมือนเดิม**
- [x] (ถ้าทำ C) `<Modal>` ใช้ร่วม ≥2 ที่; ถ้าไม่ทำ จดเป็น follow-up
- [x] grep ยืนยัน class string ซ้ำลดลง (tab pattern / course-status color logic ไม่ก๊อปหลายที่)
- [x] ทุกหน้า detail หน้าตา/พฤติกรรมเหมือนเดิม (แท็บสลับได้, badge สีถูก, modal เปิด/ปิด/submit ได้)
- [x] `npm run lint` (0/0) + `npm run build` ผ่าน

## Verification

```powershell
npm run lint
npm run build
```
ทดสอบ manual: `/courses/:id` (แท็บ + status badge), `/assignments/:id` (แท็บ + modal), `/learner-groups/:id` (แท็บ), `/courses` (status badge), version detail (ถ้าแตะ) — ดูว่าหน้าตา/สี/แท็บ/modal เหมือนเดิมทุกจุด

## Implementer Notes

- ทำตาม Scope แบบ **pure refactor** ครบ A+B+C (ไม่เปลี่ยน API contract / backend):
	- เพิ่ม `iLearn.Admin.React/src/components/ui/DetailTabs.tsx` แล้ว migrate หน้าแท็บ 4 ไฟล์:
		- `AssignmentDetailPage.tsx`
		- `CourseDetailPage.tsx`
		- `LearnerGroupDetailPage.tsx`
		- `CourseEditorPage.tsx` (ใช้ `variant="compact"` เพื่อคงสไตล์ edit header เดิม)
	- เพิ่ม `iLearn.Admin.React/src/components/ui/CourseStatusBadge.tsx` (พร้อม `CourseStatusText` / `getCourseStatusTone`) แล้ว migrate:
		- `CourseListPage.tsx` ให้ใช้ `<CourseStatusBadge>`
		- `CourseDetailPage.tsx` ให้ใช้ `<CourseStatusText>`
	- เพิ่ม `iLearn.Admin.React/src/components/ui/Modal.tsx` (`open`, `onClose`, `title?`, `children`, `size`, รองรับ `as="div"|"form"`) และ migrate ใช้งานร่วมแล้วอย่างน้อย 2 จุด:
		- `CourseDetailPage.tsx` (Edit Course Properties modal)
		- `CourseListPage.tsx` (Create Category / Rename Category modals)
		- `ConfirmDialog.tsx` (shared confirm modal wrapper)
- Verification:
	- `npm run lint` ผ่าน
	- `npm run build` ผ่าน
	- grep ยืนยันว่า modal shell ถูกใช้ซ้ำหลายจุด และ class string แท็บซ้ำ/logic status tone แบบ inline ไม่เหลือในหน้าที่ migrate
